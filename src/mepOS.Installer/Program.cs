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
        private Color CardDark = Color.FromArgb(24, 30, 39);
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
            this.Text = "mepOS // One-Click Revit Installer";
            this.Size = new Size(580, 520);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = BgDark;
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            // 1. Header Panel
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 84,
                BackColor = CardDark
            };
            pnlHeader.Paint += (s, e) =>
            {
                e.Graphics.DrawLine(new Pen(BorderDark), 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
            };

            var lblLogoMep = new Label
            {
                Text = "mep",
                Font = new Font("Segoe UI", 26f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(24, 12),
                AutoSize = true
            };
            var lblLogoOS = new Label
            {
                Text = "OS",
                Font = new Font("Segoe UI", 26f, FontStyle.Bold),
                ForeColor = YellowAccent,
                Location = new Point(106, 12),
                AutoSize = true
            };
            var lblSub = new Label
            {
                Text = "BỘ CÀI ĐẶT ADD-IN REVIT // USER-LEVEL (KHÔNG CẦN QUYỀN ADMIN)",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = YellowAccent,
                Location = new Point(26, 56),
                AutoSize = true
            };

            pnlHeader.Controls.Add(lblLogoMep);
            pnlHeader.Controls.Add(lblLogoOS);
            pnlHeader.Controls.Add(lblSub);
            this.Controls.Add(pnlHeader);

            // 2. Info Section
            var lblTargetTitle = new Label
            {
                Text = "THƯ MỤC CÀI ĐẶT (User Application Data):",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = TextMuted,
                Location = new Point(24, 98),
                AutoSize = true
            };
            this.Controls.Add(lblTargetTitle);

            lblPath = new Label
            {
                Text = InstallerCore.GetTargetDir(),
                Font = new Font("Consolas", 9f),
                ForeColor = Color.FromArgb(88, 166, 255),
                Location = new Point(24, 118),
                Size = new Size(520, 20)
            };
            this.Controls.Add(lblPath);

            // 3. Revit Versions Selection
            var lblRevitTitle = new Label
            {
                Text = "CHỌN PHIÊN BẢN REVIT CẦN KÍCH HOẠT MEPOS:",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = TextMuted,
                Location = new Point(24, 146),
                AutoSize = true
            };
            this.Controls.Add(lblRevitTitle);

            clbYears = new CheckedListBox
            {
                Location = new Point(24, 168),
                Size = new Size(516, 124),
                BackColor = CardDark,
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                CheckOnClick = true
            };

            var detectedYears = InstallerCore.DetectInstalledRevitYears();
            foreach (var yr in InstallerCore.SupportedYears)
            {
                bool isDetected = detectedYears.Contains(yr);
                string text = isDetected ? string.Format("Autodesk Revit {0}   [Đã phát hiện trên máy]", yr) : string.Format("Autodesk Revit {0}", yr);
                clbYears.Items.Add(text, true); // Check all by default
            }
            this.Controls.Add(clbYears);

            // 4. Progress Section
            progressBar = new ProgressBar
            {
                Location = new Point(24, 304),
                Size = new Size(516, 14),
                Style = ProgressBarStyle.Continuous,
                Value = 0
            };
            this.Controls.Add(progressBar);

            lblStatus = new Label
            {
                Text = "Sẵn sàng cài đặt. Quá trình không yêu cầu quyền Quản trị viên (Admin).",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(201, 209, 217),
                Location = new Point(24, 324),
                Size = new Size(516, 24)
            };
            this.Controls.Add(lblStatus);

            // 5. Action Buttons
            btnInstall = new Button
            {
                Text = "CÀI ĐẶT NGAY // INSTALL",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Location = new Point(24, 362),
                Size = new Size(320, 48),
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
                Location = new Point(356, 362),
                Size = new Size(184, 48),
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
                Text = "Mở thư mục Addin",
                Font = new Font("Segoe UI", 8.5f),
                Location = new Point(24, 424),
                Size = new Size(160, 32),
                BackColor = BgDark,
                ForeColor = TextMuted,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnOpenFolder.FlatAppearance.BorderColor = BorderDark;
            btnOpenFolder.Click += (s, e) =>
            {
                string path = InstallerCore.GetAddinsBaseDir();
                if (Directory.Exists(path)) Process.Start("explorer.exe", path);
            };
            this.Controls.Add(btnOpenFolder);

            var lblAutoUpdateNote = new Label
            {
                Text = "• Tự động cập nhật ngầm qua GitHub • 100% Native Ribbon • Zero UAC",
                Font = new Font("Segoe UI", 8f),
                ForeColor = TextMuted,
                Location = new Point(194, 432),
                Size = new Size(346, 20),
                TextAlign = ContentAlignment.TopRight
            };
            this.Controls.Add(lblAutoUpdateNote);
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
