using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

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
        public static readonly string[] SupportedYears = new string[] {
            "2027", "2026", "2025", "2024", "2023", "2022", "2021", "2020", "2019"
        };

        public static string GetTargetDir()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "mepOS");
        }

        public static string GetAddinsBaseDir()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Autodesk\Revit\Addins");
        }

        public static bool IsRevitInstalled(string year)
        {
            try
            {
                // 1. Check Program Files
                string pfDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Autodesk\Revit " + year);
                if (Directory.Exists(pfDir) && File.Exists(Path.Combine(pfDir, "Revit.exe"))) return true;

                // 2. Check 64-bit Registry
                using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                {
                    using (var key = hklm.OpenSubKey(@"SOFTWARE\Autodesk\Revit\" + year))
                    {
                        if (key != null) return true;
                    }
                    using (var key = hklm.OpenSubKey(@"SOFTWARE\Autodesk\Revit\Autodesk Revit " + year))
                    {
                        if (key != null) return true;
                    }
                }
            }
            catch { }

            // 3. Fallback: check User Data folder created by actual Revit runs
            try
            {
                string appDataRevit = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Autodesk\Revit\Autodesk Revit " + year);
                if (Directory.Exists(appDataRevit)) return true;
            }
            catch { }

            return false;
        }

        public static List<string> DetectInstalledRevitYears()
        {
            var detected = new List<string>();
            foreach (var yr in SupportedYears)
            {
                if (IsRevitInstalled(yr))
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
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        protected override CreateParams CreateParams
        {
            get
            {
                const int CS_DROPSHADOW = 0x00020000;
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= CS_DROPSHADOW;
                return cp;
            }
        }

        private Color BgDark = Color.FromArgb(16, 20, 26);
        private Color HeaderBg = Color.FromArgb(22, 27, 34);
        private Color BorderDark = Color.FromArgb(48, 54, 61);
        private Color YellowAccent = Color.FromArgb(255, 221, 0);
        private Color TextMuted = Color.FromArgb(139, 148, 158);
        private Color TextSubtle = Color.FromArgb(110, 118, 129);
        private Color GreenActive = Color.FromArgb(63, 185, 80);
        private Color BlueLink = Color.FromArgb(88, 166, 255);

        private Dictionary<string, CheckBox> versionCheckBoxes = new Dictionary<string, CheckBox>();
        private Panel pnlVersions;
        private Label lblRevitTitle;
        private Label lnkToggleAllVersions;
        private Label lnkSelectAll;
        private Label lnkDeselectAll;
        private Button btnInstall;
        private Button btnUninstall;
        private ProgressBar progressBar;
        private Label lblStatus;
        private Label lblPath;
        private bool showAllVersions = false;

        public InstallerForm()
        {
            InitializeComponent();
        }

        private void DragWindow(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.Handle, 0xA1, 0x2, 0);
            }
        }

        private void InitializeComponent()
        {
            this.Text = "mepOS Installer";
            this.ClientSize = new Size(540, 360);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = BgDark;
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            // =================================================================
            // 1. UNIFIED MODERN HEADER (Logo + Seamless Window Controls)
            // =================================================================
            var pnlHeader = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(540, 72),
                BackColor = HeaderBg
            };
            pnlHeader.MouseDown += (s, e) => DragWindow(e);
            pnlHeader.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
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

            var btnMin = new Button
            {
                Text = "—",
                Font = new Font("Segoe UI", 8.5f),
                Size = new Size(42, 34),
                Location = new Point(540 - 84, 0),
                FlatStyle = FlatStyle.Flat,
                ForeColor = TextMuted,
                BackColor = HeaderBg,
                Cursor = Cursors.Hand
            };
            btnMin.FlatAppearance.BorderSize = 0;
            btnMin.Click += (s, e) => this.WindowState = FormWindowState.Minimized;
            btnMin.MouseEnter += (s, e) => { btnMin.BackColor = Color.FromArgb(48, 54, 61); btnMin.ForeColor = Color.White; };
            btnMin.MouseLeave += (s, e) => { btnMin.BackColor = HeaderBg; btnMin.ForeColor = TextMuted; };
            pnlHeader.Controls.Add(btnMin);

            var btnClose = new Button
            {
                Text = "✕",
                Font = new Font("Segoe UI", 8.5f),
                Size = new Size(42, 34),
                Location = new Point(540 - 42, 0),
                FlatStyle = FlatStyle.Flat,
                ForeColor = TextMuted,
                BackColor = HeaderBg,
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Close();
            btnClose.MouseEnter += (s, e) => { btnClose.BackColor = Color.FromArgb(232, 17, 35); btnClose.ForeColor = Color.White; };
            btnClose.MouseLeave += (s, e) => { btnClose.BackColor = HeaderBg; btnClose.ForeColor = TextMuted; };
            pnlHeader.Controls.Add(btnClose);

            this.Controls.Add(pnlHeader);

            // =================================================================
            // 2. REVIT VERSION SELECTION (DiRoots-Style Auto-Detect & Toggles)
            // =================================================================
            int startY = 88;
            lblRevitTitle = new Label
            {
                Text = "PHIÊN BẢN REVIT ĐƯỢC PHÁT HIỆN:",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = TextMuted,
                Location = new Point(32, startY),
                AutoSize = true
            };
            this.Controls.Add(lblRevitTitle);

            lnkSelectAll = new Label
            {
                Text = "[ Chọn tất cả ]",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = BlueLink,
                Location = new Point(340, startY),
                AutoSize = true,
                Cursor = Cursors.Hand
            };
            lnkSelectAll.Click += (s, e) =>
            {
                foreach (var cb in versionCheckBoxes.Values) cb.Checked = true;
            };
            this.Controls.Add(lnkSelectAll);

            lnkDeselectAll = new Label
            {
                Text = "[ Bỏ chọn ]",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = TextMuted,
                Location = new Point(440, startY),
                AutoSize = true,
                Cursor = Cursors.Hand
            };
            lnkDeselectAll.Click += (s, e) =>
            {
                foreach (var cb in versionCheckBoxes.Values) cb.Checked = false;
            };
            this.Controls.Add(lnkDeselectAll);

            pnlVersions = new Panel
            {
                Location = new Point(32, startY + 22),
                Width = 476,
                BackColor = Color.Transparent
            };
            this.Controls.Add(pnlVersions);

            lnkToggleAllVersions = new Label
            {
                Font = new Font("Segoe UI", 8f),
                ForeColor = BlueLink,
                AutoSize = true,
                Cursor = Cursors.Hand
            };
            lnkToggleAllVersions.Click += (s, e) =>
            {
                showAllVersions = !showAllVersions;
                RenderVersionCheckboxes();
            };
            this.Controls.Add(lnkToggleAllVersions);

            // =================================================================
            // 3. TARGET PATH & PROGRESS
            // =================================================================
            lblPath = new Label
            {
                Text = "Đích cài đặt: %APPDATA%\\mepOS  (User Data • Không cần quyền Admin)",
                Font = new Font("Consolas", 8.5f),
                ForeColor = BlueLink,
                Size = new Size(480, 20)
            };
            this.Controls.Add(lblPath);

            progressBar = new ProgressBar
            {
                Size = new Size(476, 5),
                Style = ProgressBarStyle.Continuous,
                Value = 0
            };
            this.Controls.Add(progressBar);

            lblStatus = new Label
            {
                Text = "Sẵn sàng cài đặt add-in cho các phiên bản Revit được chọn.",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(201, 209, 217),
                Size = new Size(476, 20)
            };
            this.Controls.Add(lblStatus);

            // =================================================================
            // 4. ACTION BUTTONS (High-Contrast Clean CTA)
            // =================================================================
            btnInstall = new Button
            {
                Text = "CÀI ĐẶT NGAY // INSTALL",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
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
                Size = new Size(122, 46),
                BackColor = Color.FromArgb(24, 28, 36),
                ForeColor = Color.FromArgb(248, 81, 73),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnUninstall.FlatAppearance.BorderColor = BorderDark;
            btnUninstall.Click += BtnUninstall_Click;
            this.Controls.Add(btnUninstall);

            // 1px Border around Form
            this.Paint += (s, e) =>
            {
                e.Graphics.DrawRectangle(new Pen(BorderDark), 0, 0, this.ClientSize.Width - 1, this.ClientSize.Height - 1);
            };

            // Initial render of version checkboxes
            RenderVersionCheckboxes();
        }

        private void RenderVersionCheckboxes()
        {
            pnlVersions.Controls.Clear();
            versionCheckBoxes.Clear();

            var detected = InstallerCore.DetectInstalledRevitYears();
            bool displayAll = showAllVersions || (detected.Count == 0);

            if (displayAll)
            {
                lblRevitTitle.Text = "TẤT CẢ PHIÊN BẢN REVIT HỖ TRỢ (2019 – 2027):";
                lnkToggleAllVersions.Text = detected.Count > 0 ? "[-] Chỉ hiển thị các phiên bản đã cài trên máy" : "";

                int cols = 3;
                int colW = 158;
                for (int i = 0; i < InstallerCore.SupportedYears.Length; i++)
                {
                    string yr = InstallerCore.SupportedYears[i];
                    int c = i % cols;
                    int r = i / cols;
                    bool isDet = detected.Contains(yr);

                    var cb = new CheckBox
                    {
                        Text = "Revit " + yr + (isDet ? " ●" : ""),
                        Location = new Point(c * colW, r * 26),
                        Size = new Size(colW - 8, 22),
                        ForeColor = isDet ? Color.White : TextMuted,
                        Font = new Font("Segoe UI", 9f, isDet ? FontStyle.Bold : FontStyle.Regular),
                        Checked = isDet || (detected.Count == 0),
                        Cursor = Cursors.Hand
                    };
                    pnlVersions.Controls.Add(cb);
                    versionCheckBoxes[yr] = cb;
                }
                pnlVersions.Height = (int)Math.Ceiling(InstallerCore.SupportedYears.Length / 3.0) * 26;
            }
            else
            {
                lblRevitTitle.Text = string.Format("PHÁT HIỆN {0} PHIÊN BẢN REVIT TRÊN MÁY TÍNH:", detected.Count);
                lnkToggleAllVersions.Text = "[+] Hiển thị tất cả các phiên bản hỗ trợ (2019 – 2027)";

                int col1X = 0;
                int col2X = 246;
                for (int i = 0; i < detected.Count; i++)
                {
                    string yr = detected[i];
                    int c = i % 2;
                    int r = i / 2;

                    var cb = new CheckBox
                    {
                        Text = "Autodesk Revit " + yr,
                        Location = new Point(c == 0 ? col1X : col2X, r * 28),
                        Size = new Size(230, 24),
                        ForeColor = Color.White,
                        Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                        Checked = true,
                        Cursor = Cursors.Hand
                    };
                    pnlVersions.Controls.Add(cb);
                    versionCheckBoxes[yr] = cb;
                }
                pnlVersions.Height = Math.Max(28, (int)Math.Ceiling(detected.Count / 2.0) * 28);
            }

            UpdateLayoutPositions();
        }

        private void UpdateLayoutPositions()
        {
            int linkY = pnlVersions.Bottom + 4;
            lnkToggleAllVersions.Location = new Point(32, linkY);

            int pathY = lnkToggleAllVersions.Bottom + 12;
            lblPath.Location = new Point(32, pathY);

            int progY = pathY + 22;
            progressBar.Location = new Point(32, progY);

            int statY = progY + 10;
            lblStatus.Location = new Point(32, statY);

            int btnY = statY + 26;
            btnInstall.Location = new Point(32, btnY);
            btnUninstall.Location = new Point(386, btnY);

            this.ClientSize = new Size(540, btnY + 46 + 22);
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
