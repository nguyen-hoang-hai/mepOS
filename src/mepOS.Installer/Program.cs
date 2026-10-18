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
                            using (var bmp = new Bitmap(form.ClientSize.Width, form.ClientSize.Height))
                            {
                                form.DrawToBitmap(bmp, new Rectangle(0, 0, form.ClientSize.Width, form.ClientSize.Height));
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
        public static readonly string[] SupportedYears = new string[] { "2024", "2023", "2022", "2021", "2020", "2019" };

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
        private Color BgDark = Color.FromArgb(16, 20, 26);
        private Color HeaderBg = Color.FromArgb(22, 27, 34);
        private Color BorderDark = Color.FromArgb(48, 54, 61);
        private Color YellowAccent = Color.FromArgb(255, 221, 0);
        private Color TextMuted = Color.FromArgb(139, 148, 158);
        private Color GreenActive = Color.FromArgb(63, 185, 80);
        private Color BlueLink = Color.FromArgb(88, 166, 255);

        private Dictionary<string, CheckBox> versionCheckBoxes = new Dictionary<string, CheckBox>();
        private Button btnInstall;
        private Button btnUninstall;
        private ProgressBar progressBar;
        private Label lblStatus;
        private Label lblPath;

        public InstallerForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "mepOS Installer";
            this.ClientSize = new Size(540, 420);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = BgDark;
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            // =================================================================
            // 1. HEADER PANEL (Official Master Logo - Zero Clipping)
            // =================================================================
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 94,
                BackColor = HeaderBg
            };
            pnlHeader.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                e.Graphics.DrawLine(new Pen(BorderDark), 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);

                // Draw Master Logo with dynamic exact aspect ratio
                var imgLogo = InstallerCore.LoadEmbeddedImage("logo_banner_dark.png");
                if (imgLogo != null)
                {
                    int logoH = 44;
                    int logoW = (int)(logoH * ((float)imgLogo.Width / imgLogo.Height));
                    e.Graphics.DrawImage(imgLogo, 30, 14, logoW, logoH);
                }
            };

            var lblSub = new Label
            {
                Text = "// BỘ CÀI ĐẶT ADD-IN REVIT (ZERO ADMIN • ZERO UAC)",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = YellowAccent,
                Location = new Point(32, 64),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblSub);
            this.Controls.Add(pnlHeader);

            // =================================================================
            // 2. REVIT VERSION SELECTION (2 Columns, Clean, No Scrollbars!)
            // =================================================================
            int startY = 114;
            var lblRevitTitle = new Label
            {
                Text = "CHỌN PHIÊN BẢN REVIT ĐỂ KÍCH HOẠT:",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = TextMuted,
                Location = new Point(32, startY),
                AutoSize = true
            };
            this.Controls.Add(lblRevitTitle);

            var detectedYears = InstallerCore.DetectInstalledRevitYears();

            string[] col1Years = new string[] { "2024", "2022", "2020" };
            string[] col2Years = new string[] { "2023", "2021", "2019" };

            int gridTop = startY + 24;
            int col1X = 34;
            int col2X = 280;

            for (int i = 0; i < 3; i++)
            {
                int rowY = gridTop + i * 30;

                // Column 1
                CreateVersionItem(col1Years[i], col1X, rowY, detectedYears.Contains(col1Years[i]));

                // Column 2
                CreateVersionItem(col2Years[i], col2X, rowY, detectedYears.Contains(col2Years[i]));
            }

            // =================================================================
            // 3. TARGET PATH & PROGRESS
            // =================================================================
            int pathY = gridTop + 3 * 30 + 12;
            lblPath = new Label
            {
                Text = "Đích cài đặt: %APPDATA%\\mepOS  (User Application Data)",
                Font = new Font("Consolas", 8.5f),
                ForeColor = BlueLink,
                Location = new Point(32, pathY),
                Size = new Size(480, 20)
            };
            this.Controls.Add(lblPath);

            int progY = pathY + 24;
            progressBar = new ProgressBar
            {
                Location = new Point(32, progY),
                Size = new Size(476, 6),
                Style = ProgressBarStyle.Continuous,
                Value = 0
            };
            this.Controls.Add(progressBar);

            int statY = progY + 12;
            lblStatus = new Label
            {
                Text = "Sẵn sàng cài đặt. Bộ cài chạy trực tiếp không yêu cầu quyền Quản trị viên (Admin).",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(201, 209, 217),
                Location = new Point(32, statY),
                Size = new Size(476, 20)
            };
            this.Controls.Add(lblStatus);

            // =================================================================
            // 4. ACTION BUTTONS (High-Contrast Clean CTA)
            // =================================================================
            int btnY = statY + 30;
            btnInstall = new Button
            {
                Text = "CÀI ĐẶT NGAY // INSTALL",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Location = new Point(32, btnY),
                Size = new Size(340, 46),
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
                Location = new Point(386, btnY),
                Size = new Size(122, 46),
                BackColor = Color.FromArgb(24, 28, 36),
                ForeColor = Color.FromArgb(248, 81, 73),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnUninstall.FlatAppearance.BorderColor = BorderDark;
            btnUninstall.Click += BtnUninstall_Click;
            this.Controls.Add(btnUninstall);
        }

        private void CreateVersionItem(string year, int x, int y, bool isDetected)
        {
            var cb = new CheckBox
            {
                Text = "Autodesk Revit " + year,
                Location = new Point(x, y),
                Size = new Size(150, 24),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f),
                Checked = true,
                Cursor = Cursors.Hand
            };
            this.Controls.Add(cb);
            versionCheckBoxes[year] = cb;

            if (isDetected)
            {
                var lblDet = new Label
                {
                    Text = "● ĐÃ CÓ",
                    Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                    ForeColor = GreenActive,
                    Location = new Point(x + 152, y + 5),
                    AutoSize = true
                };
                this.Controls.Add(lblDet);
            }
        }

        private void BtnInstall_Click(object sender, EventArgs e)
        {
            var selectedYears = new List<string>();
            foreach (var kvp in versionCheckBoxes)
            {
                if (kvp.Value.Checked)
                {
                    selectedYears.Add(kvp.Key);
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
                    lblStatus.ForeColor = GreenActive;
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
