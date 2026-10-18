using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace mepOS.Installer
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args != null && args.Length > 0)
            {
                foreach (string arg in args)
                {
                    string a = arg.ToLowerInvariant().Trim('-', '/');
                    if (a == "quiet" || a == "silent" || a == "s" || a == "q")
                    {
                        InstallerCore.InstallAll(null);
                        return;
                    }
                    if (a == "uninstall" || a == "u")
                    {
                        InstallerCore.UninstallAll(null);
                        return;
                    }
                    if (a.StartsWith("snapshot:"))
                    {
                        string outPath = arg.Substring(arg.IndexOf(':') + 1).Trim('"', '\'');
                        using (var form = new InstallerForm())
                        {
                            form.StartPosition = FormStartPosition.Manual;
                            form.Location = new Point(-2000, -2000);
                            form.Show();
                            Application.DoEvents();
                            using (var bmp = new Bitmap(form.Width, form.Height))
                            {
                                form.DrawToBitmap(bmp, new Rectangle(0, 0, form.Width, form.Height));
                                bmp.Save(outPath, System.Drawing.Imaging.ImageFormat.Png);
                            }
                        }
                        return;
                    }
                }
            }

            Application.Run(new InstallerForm());
        }
    }

    public static class InstallerCore
    {
        public static readonly string[] SupportedYears = new string[] { "2019", "2020", "2021", "2022", "2023", "2024", "2025", "2026" };

        public static string GetTargetDir()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "mepOS");
        }

        public static string GetAddinsBaseDir()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Autodesk\Revit\Addins");
        }

        public static List<string> DetectInstalledRevitYears()
        {
            var detected = new List<string>();
            string addinsBase = GetAddinsBaseDir();
            foreach (var yr in SupportedYears)
            {
                string dir = Path.Combine(addinsBase, yr);
                if (Directory.Exists(dir))
                {
                    detected.Add(yr);
                }
            }
            return detected;
        }

        public static Image LoadEmbeddedImage(string resName)
        {
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                using (var stream = asm.GetManifestResourceStream(resName))
                {
                    if (stream != null) return Image.FromStream(stream);
                }
            }
            catch { }
            return null;
        }

        public static bool ExtractEmbeddedFile(string resName, string destPath)
        {
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                using (var stream = asm.GetManifestResourceStream(resName))
                {
                    if (stream == null) return false;
                    string parent = Path.GetDirectoryName(destPath);
                    if (!Directory.Exists(parent)) Directory.CreateDirectory(parent);

                    using (var fs = new FileStream(destPath, FileMode.Create, FileAccess.Write))
                    {
                        byte[] buffer = new byte[65536];
                        int bytesRead;
                        while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            fs.Write(buffer, 0, bytesRead);
                        }
                    }
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public static bool Install(IEnumerable<string> targetYears, Action<string, int> progressCallback)
        {
            string targetDir = GetTargetDir();
            string resDir = Path.Combine(targetDir, "Resources");
            if (!Directory.Exists(resDir)) Directory.CreateDirectory(resDir);

            if (progressCallback != null) progressCallback("Đang giải nén tập tin lõi mepOS...", 20);

            // 1. Extract Core Binaries
            ExtractEmbeddedFile("mepOS.Loader.dll", Path.Combine(targetDir, "mepOS.Loader.dll"));
            ExtractEmbeddedFile("mepOS.Core.dll", Path.Combine(targetDir, "mepOS.Core.dll"));
            ExtractEmbeddedFile("version.json", Path.Combine(targetDir, "version.json"));

            if (progressCallback != null) progressCallback("Đang cấu hình biểu tượng công cụ...", 40);

            // 2. Extract Resources
            ExtractEmbeddedFile("CT_32.png", Path.Combine(resDir, "CT_32.png"));
            ExtractEmbeddedFile("CT_16.png", Path.Combine(resDir, "CT_16.png"));
            ExtractEmbeddedFile("BL_32.png", Path.Combine(resDir, "BL_32.png"));
            ExtractEmbeddedFile("BL_16.png", Path.Combine(resDir, "BL_16.png"));
            ExtractEmbeddedFile("logo_banner.png", Path.Combine(resDir, "logo_banner.png"));

            if (progressCallback != null) progressCallback("Đang khởi tạo tệp cấu hình mepOS.addin...", 60);

            // 3. Create mepOS.addin
            string loaderDllPath = Path.Combine(targetDir, "mepOS.Loader.dll");
            string addinContent =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<RevitAddIns>
  <AddIn Type=""Application"">
    <Name>mepOS</Name>
    <Assembly><![CDATA[" + loaderDllPath + @"]]></Assembly>
    <AddInId>B7E84920-5621-4A59-8671-5085BC361902</AddInId>
    <FullClassName>mepOS.Loader.RibbonApp</FullClassName>
    <VendorId>MEPOS</VendorId>
    <VendorDescription>mepOS - Swiss Clean Architectural Revit Suite</VendorDescription>
  </AddIn>
</RevitAddIns>";

            string masterAddinPath = Path.Combine(targetDir, "mepOS.addin");
            File.WriteAllText(masterAddinPath, addinContent, Encoding.UTF8);

            // 4. Register to Revit versions
            if (progressCallback != null) progressCallback("Đang đăng ký add-in vào Autodesk Revit...", 80);
            string addinsBase = GetAddinsBaseDir();

            int count = 0;
            foreach (var yr in targetYears)
            {
                try
                {
                    string yrDir = Path.Combine(addinsBase, yr);
                    if (!Directory.Exists(yrDir)) Directory.CreateDirectory(yrDir);
                    string targetFile = Path.Combine(yrDir, "mepOS.addin");
                    File.Copy(masterAddinPath, targetFile, true);
                    count++;
                }
                catch { }
            }

            if (progressCallback != null) progressCallback(string.Format("Cài đặt thành công mepOS cho {0} phiên bản Revit!", count), 100);
            return true;
        }

        public static bool InstallAll(Action<string, int> progressCallback)
        {
            return Install(SupportedYears, progressCallback);
        }

        public static bool UninstallAll(Action<string, int> progressCallback)
        {
            if (progressCallback != null) progressCallback("Đang gỡ đăng ký mepOS khỏi Revit...", 30);
            string addinsBase = GetAddinsBaseDir();
            foreach (var yr in SupportedYears)
            {
                try
                {
                    string file = Path.Combine(addinsBase, yr, "mepOS.addin");
                    if (File.Exists(file)) File.Delete(file);
                }
                catch { }
            }

            if (progressCallback != null) progressCallback("Đang xóa tập tin mepOS trong %APPDATA%...", 70);
            try
            {
                string targetDir = GetTargetDir();
                if (Directory.Exists(targetDir))
                {
                    Directory.Delete(targetDir, true);
                }
            }
            catch { }

            if (progressCallback != null) progressCallback("Đã gỡ cài đặt mepOS sạch sẽ khỏi máy tính!", 100);
            return true;
        }
    }

    public class InstallerForm : Form
    {
        private Color BgDark = Color.FromArgb(14, 17, 22);
        private Color CardDark = Color.FromArgb(22, 27, 34);
        private Color BorderDark = Color.FromArgb(48, 54, 61);
        private Color YellowAccent = Color.FromArgb(255, 221, 0);
        private Color TextMuted = Color.FromArgb(139, 148, 158);

        private CheckedListBox clbYears;
        private Button btnInstall;
        private Button btnUninstall;
        private Button btnOpenFolder;
        private ProgressBar progressBar;
        private Label lblStatus;
        private Label lblPath;

        public InstallerForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "mepOS // One-Click Revit Installer (Non-Admin)";
            this.Size = new Size(680, 620);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = BgDark;
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            // =================================================================
            // 1. HEADER PANEL (Swiss Architecture Master Branding)
            // =================================================================
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 88,
                BackColor = CardDark
            };
            pnlHeader.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.DrawLine(new Pen(BorderDark), 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
            };

            var lblLogoMep = new Label
            {
                Text = "mep",
                Font = new Font("Segoe UI", 28f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(24, 10),
                AutoSize = true
            };
            var lblLogoOS = new Label
            {
                Text = "OS",
                Font = new Font("Segoe UI", 28f, FontStyle.Bold),
                ForeColor = YellowAccent,
                Location = new Point(114, 10),
                AutoSize = true
            };
            var lblSub = new Label
            {
                Text = "// BỘ CÀI ĐẶT ADD-IN REVIT (ZERO ADMIN • ZERO UAC • AUTO-UPDATE)",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = YellowAccent,
                Location = new Point(26, 58),
                AutoSize = true
            };

            var lblTagBadge = new Label
            {
                Text = "v1.0.5 [RELEASE]",
                Font = new Font("Consolas", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(88, 166, 255),
                Location = new Point(510, 20),
                Size = new Size(130, 24),
                TextAlign = ContentAlignment.TopRight
            };

            var lblDot = new Label
            {
                Text = "● LIVE CDN",
                Font = new Font("Consolas", 8.5f),
                ForeColor = Color.FromArgb(63, 185, 80),
                Location = new Point(510, 44),
                Size = new Size(130, 20),
                TextAlign = ContentAlignment.TopRight
            };

            pnlHeader.Controls.Add(lblLogoMep);
            pnlHeader.Controls.Add(lblLogoOS);
            pnlHeader.Controls.Add(lblSub);
            pnlHeader.Controls.Add(lblTagBadge);
            pnlHeader.Controls.Add(lblDot);
            this.Controls.Add(pnlHeader);

            // =================================================================
            // 2. FEATURE TOOL SHOWCASE CARDS (Visual Demo inside Installer)
            // =================================================================
            var lblToolsTitle = new Label
            {
                Text = "CÁC CÔNG CỤ ĐƯỢC KÍCH HOẠT TRÊN TAB MEPOS:",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = TextMuted,
                Location = new Point(24, 102),
                AutoSize = true
            };
            this.Controls.Add(lblToolsTitle);

            // Card CT (Connect To)
            var pnlCardCt = CreateToolCard(24, 122, 300, 72, "CT_32.png", "Connect To [CT]", "Nối thông minh 2 đầu connector", "Auto-Align Collinear & Elevation 3D");
            this.Controls.Add(pnlCardCt);

            // Card BL (Bloom)
            var pnlCardBl = CreateToolCard(338, 122, 302, 72, "BL_32.png", "Bloom [BL]", "Bung nhánh tức thì từ phụ kiện", "BFS 3-level Stub Generator cho MEP");
            this.Controls.Add(pnlCardBl);

            // =================================================================
            // 3. TARGET PATH & REVIT CHECKLIST
            // =================================================================
            var lblRevitTitle = new Label
            {
                Text = "CHỌN PHIÊN BẢN REVIT ĐỂ KÍCH HOẠT ADD-IN:",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = TextMuted,
                Location = new Point(24, 208),
                AutoSize = true
            };
            this.Controls.Add(lblRevitTitle);

            clbYears = new CheckedListBox
            {
                Location = new Point(24, 228),
                Size = new Size(616, 116),
                BackColor = CardDark,
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                CheckOnClick = true
            };

            var detectedYears = InstallerCore.DetectInstalledRevitYears();
            foreach (var yr in InstallerCore.SupportedYears)
            {
                bool isDetected = detectedYears.Contains(yr);
                string text = isDetected ? string.Format("Autodesk Revit {0}   [ĐÃ PHÁT HIỆN TRÊN MÁY]", yr) : string.Format("Autodesk Revit {0}", yr);
                clbYears.Items.Add(text, true);
            }
            this.Controls.Add(clbYears);

            lblPath = new Label
            {
                Text = "Đích cài đặt: " + InstallerCore.GetTargetDir() + "   (User-Level / 100% Zero UAC)",
                Font = new Font("Consolas", 8.5f),
                ForeColor = Color.FromArgb(88, 166, 255),
                Location = new Point(24, 354),
                Size = new Size(616, 20)
            };
            this.Controls.Add(lblPath);

            // =================================================================
            // 4. PROGRESS SECTION
            // =================================================================
            progressBar = new ProgressBar
            {
                Location = new Point(24, 380),
                Size = new Size(616, 12),
                Style = ProgressBarStyle.Continuous,
                Value = 0
            };
            this.Controls.Add(progressBar);

            lblStatus = new Label
            {
                Text = "Sẵn sàng cài đặt. Bộ cài chạy trực tiếp không yêu cầu quyền Quản trị viên (Admin).",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(201, 209, 217),
                Location = new Point(24, 400),
                Size = new Size(616, 22)
            };
            this.Controls.Add(lblStatus);

            // =================================================================
            // 5. ACTION BUTTONS (High Contrast Architectural CTA)
            // =================================================================
            btnInstall = new Button
            {
                Text = "CÀI ĐẶT NGAY // INSTALL",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Location = new Point(24, 434),
                Size = new Size(380, 50),
                BackColor = YellowAccent,
                ForeColor = Color.FromArgb(10, 10, 10),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnInstall.FlatAppearance.BorderSize = 0;
            btnInstall.Click += BtnInstall_Click;
            this.Controls.Add(btnInstall);

            btnUninstall = new Button
            {
                Text = "Gỡ cài đặt",
                Font = new Font("Segoe UI", 9f),
                Location = new Point(418, 434),
                Size = new Size(110, 50),
                BackColor = CardDark,
                ForeColor = Color.FromArgb(248, 81, 73),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnUninstall.FlatAppearance.BorderColor = Color.FromArgb(60, 68, 77);
            btnUninstall.Click += BtnUninstall_Click;
            this.Controls.Add(btnUninstall);

            btnOpenFolder = new Button
            {
                Text = "Mở Addin",
                Font = new Font("Segoe UI", 9f),
                Location = new Point(538, 434),
                Size = new Size(102, 50),
                BackColor = CardDark,
                ForeColor = TextMuted,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnOpenFolder.FlatAppearance.BorderColor = Color.FromArgb(60, 68, 77);
            btnOpenFolder.Click += (s, e) =>
            {
                string path = InstallerCore.GetAddinsBaseDir();
                if (Directory.Exists(path)) Process.Start("explorer.exe", path);
            };
            this.Controls.Add(btnOpenFolder);

            var lblAutoUpdateNote = new Label
            {
                Text = "• Tự động kiểm tra & cập nhật ngầm qua GitHub • 100% In-Memory Dynamic Load • Không khóa DLL",
                Font = new Font("Segoe UI", 8f),
                ForeColor = TextMuted,
                Location = new Point(24, 504),
                Size = new Size(616, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblAutoUpdateNote);
        }

        private Panel CreateToolCard(int x, int y, int w, int h, string iconRes, string title, string sub1, string sub2)
        {
            var pnl = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(w, h),
                BackColor = CardDark
            };
            pnl.Paint += (s, e) =>
            {
                e.Graphics.DrawRectangle(new Pen(BorderDark), 0, 0, w - 1, h - 1);
            };

            var pic = new PictureBox
            {
                Location = new Point(14, 18),
                Size = new Size(36, 36),
                SizeMode = PictureBoxSizeMode.CenterImage,
                BackColor = Color.Transparent
            };
            var img = InstallerCore.LoadEmbeddedImage(iconRes);
            if (img != null) pic.Image = img;
            pnl.Controls.Add(pic);

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(58, 12),
                AutoSize = true
            };
            pnl.Controls.Add(lblTitle);

            var lblSub1 = new Label
            {
                Text = sub1,
                Font = new Font("Segoe UI", 8f),
                ForeColor = YellowAccent,
                Location = new Point(58, 32),
                AutoSize = true
            };
            pnl.Controls.Add(lblSub1);

            var lblSub2 = new Label
            {
                Text = sub2,
                Font = new Font("Segoe UI", 7.5f),
                ForeColor = TextMuted,
                Location = new Point(58, 48),
                AutoSize = true
            };
            pnl.Controls.Add(lblSub2);

            return pnl;
        }

        private void BtnInstall_Click(object sender, EventArgs e)
        {
            var selectedYears = new List<string>();
            for (int i = 0; i < clbYears.Items.Count; i++)
            {
                if (clbYears.GetItemChecked(i))
                {
                    selectedYears.Add(InstallerCore.SupportedYears[i]);
                }
            }

            if (selectedYears.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn ít nhất một phiên bản Revit để cài đặt!", "mepOS Installer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnInstall.Enabled = false;
            btnUninstall.Enabled = false;

            try
            {
                bool ok = InstallerCore.Install(selectedYears, (msg, pct) =>
                {
                    lblStatus.Text = msg;
                    progressBar.Value = Math.Max(0, Math.Min(100, pct));
                    Application.DoEvents();
                });

                if (ok)
                {
                    lblStatus.ForeColor = Color.FromArgb(63, 185, 80);
                    btnInstall.Text = "HOÀN TẤT // KHỞI ĐỘNG REVIT";
                    btnInstall.Enabled = true;
                    btnInstall.Click -= BtnInstall_Click;
                    btnInstall.Click += (s, ev) => this.Close();

                    MessageBox.Show(
                        "Cài đặt mepOS thành công!\n\nTừ bây giờ, hãy mở Revit để sử dụng thanh công cụ mepOS.\nMọi bản cập nhật tính năng mới sẽ được đồng bộ ngầm tự động.",
                        "mepOS // Cài đặt thành công",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi cài đặt: " + ex.Message, "mepOS Installer Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnInstall.Enabled = true;
            }
            finally
            {
                btnUninstall.Enabled = true;
            }
        }

        private void BtnUninstall_Click(object sender, EventArgs e)
        {
            var dr = MessageBox.Show(
                "Bạn có chắc chắn muốn gỡ cài đặt mepOS khỏi máy tính không?",
                "mepOS // Xác nhận gỡ cài đặt",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (dr != DialogResult.Yes) return;

            btnInstall.Enabled = false;
            btnUninstall.Enabled = false;

            try
            {
                InstallerCore.UninstallAll((msg, pct) =>
                {
                    lblStatus.Text = msg;
                    progressBar.Value = Math.Max(0, Math.Min(100, pct));
                    Application.DoEvents();
                });

                lblStatus.ForeColor = YellowAccent;
                MessageBox.Show("Đã gỡ cài đặt mepOS hoàn tất!", "mepOS", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi gỡ cài đặt: " + ex.Message, "mepOS Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnInstall.Enabled = true;
                btnUninstall.Enabled = true;
            }
        }
    }
}
