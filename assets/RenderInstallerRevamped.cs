using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;

class RenderInstallerRevamped
{
    static Color BgCanvas = Color.FromArgb(255, 13, 16, 21);
    static Color WindowBg = Color.FromArgb(255, 16, 20, 26);
    static Color HeaderBg = Color.FromArgb(255, 22, 27, 34);
    static Color BorderColor = Color.FromArgb(255, 48, 54, 61);
    static Color YellowCore = Color.FromArgb(255, 255, 221, 0);
    static Color TextMuted = Color.FromArgb(255, 139, 148, 158);
    static Color GreenActive = Color.FromArgb(255, 63, 185, 80);
    static Color BlueLink = Color.FromArgb(255, 88, 166, 255);
    static Color BlackDark = Color.FromArgb(255, 10, 10, 10);

    static void Main()
    {
        int canvasW = 960;
        int canvasH = 680;

        string fontPath = @"D:\OneDrive\@ Personal Project\mepOS\assets\fonts\SpaceGrotesk-Bold.ttf";
        string outDir = @"C:\Users\2412n\.gemini\antigravity\brain\60591867-667c-4678-8243-090a84a63b0c";
        string outPath = Path.Combine(outDir, "mepos_installer_revamped.png");
        string localPath = @"D:\OneDrive\@ Personal Project\mepOS\assets\mepos_installer_revamped.png";

        using (var pfc = new PrivateFontCollection())
        {
            pfc.AddFontFile(fontPath);
            FontFamily family = pfc.Families[0];

            using (var bmp = new Bitmap(canvasW, canvasH, PixelFormat.Format32bppArgb))
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                // Studio Canvas Background
                using (var b = new SolidBrush(BgCanvas)) g.FillRectangle(b, 0, 0, canvasW, canvasH);

                // Subtle Grid lines
                using (var pGrid = new Pen(Color.FromArgb(255, 22, 27, 34), 1f))
                {
                    for (int x = 0; x < canvasW; x += 40) g.DrawLine(pGrid, x, 0, x, canvasH);
                    for (int y = 0; y < canvasH; y += 40) g.DrawLine(pGrid, 0, y, canvasW, y);
                }

                // Window Dimensions
                int winW = 560;
                int winH = 460;
                int winX = (canvasW - winW) / 2;
                int winY = (canvasH - winH) / 2 - 10;

                // Window Shadow
                for (int s = 14; s >= 1; s--)
                {
                    int alpha = (15 - s) * 5;
                    using (var pShadow = new Pen(Color.FromArgb(alpha, 0, 0, 0), s * 2f))
                    {
                        g.DrawRectangle(pShadow, winX - s, winY - s, winW + s * 2, winH + s * 2);
                    }
                }

                // Window Base
                using (var bWin = new SolidBrush(WindowBg))
                using (var pWin = new Pen(BorderColor, 1.5f))
                {
                    g.FillRectangle(bWin, winX, winY, winW, winH);
                    g.DrawRectangle(pWin, winX, winY, winW, winH);
                }

                // 1. Title Bar
                int titleH = 34;
                using (var bTitle = new SolidBrush(Color.FromArgb(255, 18, 22, 28)))
                {
                    g.FillRectangle(bTitle, winX, winY, winW, titleH);
                    g.DrawLine(new Pen(BorderColor), winX, winY + titleH, winX + winW, winY + titleH);
                }

                using (var fWinTitle = new Font("Segoe UI", 9f))
                using (var bWinText = new SolidBrush(Color.FromArgb(255, 201, 209, 217)))
                {
                    g.DrawString("mepOS Installer", fWinTitle, bWinText, winX + 16, winY + 8);
                }

                // Window Controls
                using (var fClose = new Font("Segoe UI", 8.5f))
                using (var bM = new SolidBrush(TextMuted))
                {
                    g.DrawString("─", fClose, bM, winX + winW - 80, winY + 8);
                    g.DrawString("□", fClose, bM, winX + winW - 52, winY + 8);
                    g.DrawString("✕", fClose, bM, winX + winW - 26, winY + 8);
                }

                // 2. Header Panel
                int headerY = winY + titleH;
                int headerH = 100;
                using (var bH = new SolidBrush(HeaderBg))
                {
                    g.FillRectangle(bH, winX, headerY, winW, headerH);
                    g.DrawLine(new Pen(BorderColor), winX, headerY + headerH, winX + winW, headerY + headerH);
                }

                // Draw Master mepOS Logo using SpaceGrotesk (Continuous vector path - ZERO clipping!)
                // Draw Master mepOS Logo using official master logo image (100% genuine, ZERO clipping!)
                string logoMasterPath = @"D:\OneDrive\@ Personal Project\mepOS\assets\official_icons\banner_concept2_master_logo_dark.png";
                if (File.Exists(logoMasterPath))
                {
                    using (var imgLogo = Image.FromFile(logoMasterPath))
                    {
                        // Master logo has aspect ratio 408x106 (~3.85:1)
                        int logoH = 46;
                        int logoW = (int)(logoH * (408f / 106f));
                        g.DrawImage(imgLogo, winX + 32, headerY + 12, logoW, logoH);
                    }
                }

                // Subtitle: Clean architectural text without version or badges
                using (var fSub = new Font("Segoe UI", 9f, FontStyle.Bold))
                using (var bSub = new SolidBrush(YellowCore))
                {
                    g.DrawString("// BỘ CÀI ĐẶT ADD-IN REVIT (ZERO ADMIN • ZERO UAC)", fSub, bSub, winX + 34, headerY + 68);
                }

                // 3. Body Section
                int bodyY = headerY + headerH + 24;

                // Section Title
                using (var fSec = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                using (var bSec = new SolidBrush(TextMuted))
                {
                    g.DrawString("CHỌN PHIÊN BẢN REVIT ĐỂ KÍCH HOẠT:", fSec, bSec, winX + 34, bodyY);
                }

                // Revit Version Checkbox Grid (2 Columns, Clean, No Scrollbar!)
                string[] col1 = { "Autodesk Revit 2024", "Autodesk Revit 2022", "Autodesk Revit 2020" };
                string[] col2 = { "Autodesk Revit 2023", "Autodesk Revit 2021", "Autodesk Revit 2019" };
                bool[] col1Det = { true, true, false };
                bool[] col2Det = { false, false, false };

                int gridY = bodyY + 24;
                int col1X = winX + 34;
                int col2X = winX + 290;

                for (int i = 0; i < 3; i++)
                {
                    int rowY = gridY + i * 32;

                    // Col 1
                    DrawCheckboxRow(g, col1X, rowY, col1[i], col1Det[i]);

                    // Col 2
                    DrawCheckboxRow(g, col2X, rowY, col2[i], col2Det[i]);
                }

                // Destination Path
                int pathY = gridY + 3 * 32 + 10;
                using (var fMono = new Font("Consolas", 8.5f))
                using (var bPath = new SolidBrush(BlueLink))
                {
                    g.DrawString("Đích cài đặt: %APPDATA%\\mepOS  (User Application Data)", fMono, bPath, winX + 34, pathY);
                }

                // Progress Bar (Sleek thin line)
                int progY = pathY + 24;
                int progW = winW - 68;
                using (var bBarBg = new SolidBrush(Color.FromArgb(255, 30, 36, 46)))
                using (var bBarFill = new SolidBrush(YellowCore))
                {
                    g.FillRectangle(bBarBg, winX + 34, progY, progW, 4);
                }

                // Status text
                using (var fStat = new Font("Segoe UI", 8.5f))
                using (var bStat = new SolidBrush(Color.FromArgb(255, 180, 190, 200)))
                {
                    g.DrawString("Sẵn sàng cài đặt. Bộ cài chạy trực tiếp không yêu cầu quyền Quản trị viên (Admin).", fStat, bStat, winX + 34, progY + 12);
                }

                // 4. Action Buttons
                int btnY = progY + 38;
                int btnInstallW = 340;
                int btnH = 46;

                // Main CTA Button
                using (var bBtn = new SolidBrush(YellowCore))
                using (var fBtn = new Font("Segoe UI", 10.5f, FontStyle.Bold))
                using (var bText = new SolidBrush(Color.FromArgb(255, 10, 10, 10)))
                {
                    g.FillRectangle(bBtn, winX + 34, btnY, btnInstallW, btnH);
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString("CÀI ĐẶT NGAY // INSTALL", fBtn, bText, new RectangleF(winX + 34, btnY, btnInstallW, btnH), sf);
                }

                // Secondary Uninstall Button
                int btnUnX = winX + 34 + btnInstallW + 12;
                int btnUnW = 140;
                using (var bBtn = new SolidBrush(Color.FromArgb(255, 24, 28, 36)))
                using (var pBtn = new Pen(BorderColor))
                using (var fBtn = new Font("Segoe UI", 9f))
                using (var bRed = new SolidBrush(Color.FromArgb(255, 248, 81, 73)))
                {
                    g.FillRectangle(bBtn, btnUnX, btnY, btnUnW, btnH);
                    g.DrawRectangle(pBtn, btnUnX, btnY, btnUnW, btnH);
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString("Gỡ cài đặt", fBtn, bRed, new RectangleF(btnUnX, btnY, btnUnW, btnH), sf);
                }

                bmp.Save(outPath, ImageFormat.Png);
                bmp.Save(localPath, ImageFormat.Png);
            }
        }

        Console.WriteLine("Revamped installer demo generated successfully!");
    }

    static void DrawCheckboxRow(Graphics g, int x, int y, string name, bool isDetected)
    {
        // Custom Checkbox
        int boxSize = 16;
        using (var bBox = new SolidBrush(Color.FromArgb(255, 24, 30, 40)))
        using (var pBox = new Pen(BorderColor, 1.2f))
        {
            g.FillRectangle(bBox, x, y + 2, boxSize, boxSize);
            g.DrawRectangle(pBox, x, y + 2, boxSize, boxSize);
        }

        // Yellow Checkmark
        using (var pCheck = new Pen(YellowCore, 2f))
        {
            g.DrawLine(pCheck, x + 3, y + 10, x + 6, y + 14);
            g.DrawLine(pCheck, x + 6, y + 14, x + 13, y + 5);
        }

        // Text
        using (var fText = new Font("Segoe UI", 9f, FontStyle.Regular))
        using (var bText = new SolidBrush(Color.White))
        {
            g.DrawString(name, fText, bText, x + 24, y);
        }

        // Green Tag if detected
        if (isDetected)
        {
            float nameW = g.MeasureString(name, new Font("Segoe UI", 9f)).Width;
            using (var fTag = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            using (var bTag = new SolidBrush(GreenActive))
            {
                g.DrawString("● ĐÃ CÓ", fTag, bTag, x + 24 + nameW + 8, y + 2);
            }
        }
    }
}
