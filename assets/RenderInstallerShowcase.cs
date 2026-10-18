using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;

class RenderInstallerShowcase
{
    static Color BgCanvas = Color.FromArgb(255, 13, 16, 21);
    static Color WindowBg = Color.FromArgb(255, 14, 17, 22);
    static Color HeaderBg = Color.FromArgb(255, 22, 27, 34);
    static Color CardBg = Color.FromArgb(255, 22, 27, 34);
    static Color BorderColor = Color.FromArgb(255, 48, 54, 61);
    static Color YellowCore = Color.FromArgb(255, 255, 221, 0);
    static Color TextMuted = Color.FromArgb(255, 139, 148, 158);
    static Color GreenActive = Color.FromArgb(255, 63, 185, 80);
    static Color BlueLink = Color.FromArgb(255, 88, 166, 255);

    static void Main()
    {
        int canvasW = 1200;
        int canvasH = 920;

        string outDir = @"C:\Users\2412n\.gemini\antigravity\brain\60591867-667c-4678-8243-090a84a63b0c";
        string outPath = Path.Combine(outDir, "mepos_installer_concept_demo.png");
        string localPath = @"D:\OneDrive\@ Personal Project\mepOS\assets\mepos_installer_concept_demo.png";

        using (var bmp = new Bitmap(canvasW, canvasH, PixelFormat.Format32bppArgb))
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            // 1. Studio Background
            using (var b = new SolidBrush(BgCanvas)) g.FillRectangle(b, 0, 0, canvasW, canvasH);

            // Subtle Architectural Grid lines
            using (var pGrid = new Pen(Color.FromArgb(255, 22, 27, 34), 1f))
            {
                for (int x = 0; x < canvasW; x += 40) g.DrawLine(pGrid, x, 0, x, canvasH);
                for (int y = 0; y < canvasH; y += 40) g.DrawLine(pGrid, 0, y, canvasW, y);
            }

            // Top Studio Header
            using (var fTop = new Font("Segoe UI", 11f, FontStyle.Bold))
            using (var fSub = new Font("Segoe UI", 8.5f, FontStyle.Regular))
            using (var bY = new SolidBrush(YellowCore))
            using (var bW = new SolidBrush(Color.White))
            using (var bM = new SolidBrush(TextMuted))
            {
                g.DrawString("mep", fTop, bW, 40, 24);
                float mW = g.MeasureString("mep", fTop).Width;
                g.DrawString("OS", fTop, bY, 40 + mW - 4, 24);
                g.DrawString("// GIAO DIỆN BỘ CÀI ĐẶT ĐỘC LẬP (STANDALONE ZERO-ADMIN GUI INSTALLER DEMO)", fSub, bM, 40, 50);
            }

            // Highlights Badges on top-right
            DrawBadge(g, canvasW - 540, 26, "ZERO ADMIN", YellowCore, Color.FromArgb(10, 10, 10));
            DrawBadge(g, canvasW - 410, 26, "OFFLINE STANDALONE", Color.FromArgb(30, 36, 46), Color.White);
            DrawBadge(g, canvasW - 240, 26, "REVIT 2019-2026", Color.FromArgb(30, 36, 46), BlueLink);

            // 2. Render Window Mockup
            int winW = 700;
            int winH = 680;
            int winX = (canvasW - winW) / 2;
            int winY = 96;

            // Draw Window Shadow
            for (int s = 12; s >= 1; s--)
            {
                int alpha = (13 - s) * 5;
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

            // Window Title Bar (Windows 11 style)
            int titleH = 34;
            using (var bTitle = new SolidBrush(Color.FromArgb(255, 18, 22, 28)))
            {
                g.FillRectangle(bTitle, winX, winY, winW, titleH);
                g.DrawLine(new Pen(BorderColor), winX, winY + titleH, winX + winW, winY + titleH);
            }

            using (var fWinTitle = new Font("Segoe UI", 9f))
            using (var bWinText = new SolidBrush(Color.FromArgb(255, 201, 209, 217)))
            {
                g.DrawString("mepOS // One-Click Revit Installer (Non-Admin)", fWinTitle, bWinText, winX + 16, winY + 8);
            }

            // Title Bar Buttons (Minimize, Maximize, Close)
            int btnCloseX = winX + winW - 36;
            using (var fClose = new Font("Segoe UI", 8.5f))
            using (var bM = new SolidBrush(TextMuted))
            {
                g.DrawString("─", fClose, bM, winX + winW - 90, winY + 8);
                g.DrawString("□", fClose, bM, winX + winW - 60, winY + 8);
                g.DrawString("✕", fClose, bM, btnCloseX, winY + 8);
            }

            // Inner Content
            int contentY = winY + titleH;

            // Header Section
            int headerH = 88;
            using (var bH = new SolidBrush(HeaderBg))
            {
                g.FillRectangle(bH, winX, contentY, winW, headerH);
                g.DrawLine(new Pen(BorderColor), winX, contentY + headerH, winX + winW, contentY + headerH);
            }

            using (var fLogo = new Font("Segoe UI", 28f, FontStyle.Bold))
            using (var fSub2 = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (var fMono = new Font("Consolas", 8.5f, FontStyle.Bold))
            using (var bW = new SolidBrush(Color.White))
            using (var bY = new SolidBrush(YellowCore))
            using (var bBlue = new SolidBrush(BlueLink))
            using (var bG = new SolidBrush(GreenActive))
            {
                g.DrawString("mep", fLogo, bW, winX + 26, contentY + 10);
                float mW = g.MeasureString("mep", fLogo).Width;
                g.DrawString("OS", fLogo, bY, winX + 26 + mW - 8, contentY + 10);

                g.DrawString("// BỘ CÀI ĐẶT ADD-IN REVIT (ZERO ADMIN • ZERO UAC • AUTO-UPDATE)", fSub2, bY, winX + 28, contentY + 58);

                g.DrawString("v1.0.5 [RELEASE]", fMono, bBlue, winX + winW - 150, contentY + 20);
                g.DrawString("● LIVE CDN", fMono, bG, winX + winW - 150, contentY + 44);
            }

            // Feature Tools Showcase
            int toolsY = contentY + headerH + 16;
            using (var fSec = new Font("Segoe UI", 8f, FontStyle.Bold))
            using (var bSec = new SolidBrush(TextMuted))
            {
                g.DrawString("CÁC CÔNG CỤ ĐƯỢC KÍCH HOẠT TRÊN TAB MEPOS:", fSec, bSec, winX + 26, toolsY);
            }

            string ctIconPath = @"D:\OneDrive\@ Personal Project\mepOS\src\mepOS.Loader\Resources\CT_32.png";
            string blIconPath = @"D:\OneDrive\@ Personal Project\mepOS\src\mepOS.Loader\Resources\BL_32.png";

            DrawToolCard(g, winX + 26, toolsY + 20, 316, 78, ctIconPath, "Connect To [CT]", "Nối thông minh 2 đầu connector", "Auto-Align Collinear & Elevation 3D");
            DrawToolCard(g, winX + 356, toolsY + 20, 316, 78, blIconPath, "Bloom [BL]", "Bung nhánh tức thì từ phụ kiện", "BFS 3-level Stub Generator cho MEP");

            // Revit Selection Section
            int revitY = toolsY + 112;
            using (var fSec = new Font("Segoe UI", 8f, FontStyle.Bold))
            using (var bSec = new SolidBrush(TextMuted))
            {
                g.DrawString("CHỌN PHIÊN BẢN REVIT ĐỂ KÍCH HOẠT ADD-IN:", fSec, bSec, winX + 26, revitY);
            }

            int clbX = winX + 26;
            int clbY = revitY + 20;
            int clbW = winW - 52;
            int clbH = 140;

            using (var bBox = new SolidBrush(CardBg))
            using (var pBox = new Pen(BorderColor))
            {
                g.FillRectangle(bBox, clbX, clbY, clbW, clbH);
                g.DrawRectangle(pBox, clbX, clbY, clbW, clbH);
            }

            // Draw Checkbox items
            string[] items = {
                "Autodesk Revit 2024   [ĐÃ PHÁT HIỆN TRÊN MÁY]",
                "Autodesk Revit 2023",
                "Autodesk Revit 2022   [ĐÃ PHÁT HIỆN TRÊN MÁY]",
                "Autodesk Revit 2021",
                "Autodesk Revit 2020",
                "Autodesk Revit 2019"
            };

            for (int i = 0; i < items.Length; i++)
            {
                int itemY = clbY + 8 + i * 21;
                // Checkbox square
                using (var bCheck = new SolidBrush(Color.FromArgb(255, 30, 36, 46)))
                using (var pCheck = new Pen(BorderColor))
                {
                    g.FillRectangle(bCheck, clbX + 12, itemY + 2, 13, 13);
                    g.DrawRectangle(pCheck, clbX + 12, itemY + 2, 13, 13);
                }
                // Check mark
                using (var pMark = new Pen(YellowCore, 1.8f))
                {
                    g.DrawLine(pMark, clbX + 14, itemY + 8, clbX + 17, itemY + 12);
                    g.DrawLine(pMark, clbX + 17, itemY + 12, clbX + 23, itemY + 5);
                }

                bool isDetected = items[i].Contains("[ĐÃ PHÁT HIỆN TRÊN MÁY]");
                using (var fItem = new Font("Segoe UI", 8.5f))
                using (var bItem = new SolidBrush(isDetected ? Color.White : Color.FromArgb(255, 180, 190, 200)))
                {
                    g.DrawString(items[i], fItem, bItem, clbX + 32, itemY);
                }
            }

            // Path label
            int pathY = clbY + clbH + 8;
            using (var fMono = new Font("Consolas", 8f))
            using (var bBlue = new SolidBrush(BlueLink))
            {
                g.DrawString("Đích cài đặt: C:\\Users\\...\\AppData\\Roaming\\mepOS   (User-Level / 100% Zero UAC)", fMono, bBlue, winX + 26, pathY);
            }

            // Progress Bar Section
            int progY = pathY + 24;
            int progW = winW - 52;
            using (var bBarBg = new SolidBrush(Color.FromArgb(255, 30, 36, 46)))
            using (var bBarFill = new SolidBrush(YellowCore))
            {
                g.FillRectangle(bBarBg, winX + 26, progY, progW, 10);
                g.FillRectangle(bBarFill, winX + 26, progY, progW * 0.75f, 10); // Simulated 75%
            }

            using (var fStatus = new Font("Segoe UI", 8.5f))
            using (var bStat = new SolidBrush(Color.FromArgb(255, 201, 209, 217)))
            {
                g.DrawString("Sẵn sàng cài đặt. Bộ cài chạy trực tiếp không yêu cầu quyền Quản trị viên (Admin).", fStatus, bStat, winX + 26, progY + 16);
            }

            // Action Buttons
            int btnY = progY + 44;
            int btnInstallW = 390;
            int btnH = 48;

            // CTA Install Button
            using (var bBtn = new SolidBrush(YellowCore))
            using (var fBtn = new Font("Segoe UI", 10.5f, FontStyle.Bold))
            using (var bText = new SolidBrush(Color.FromArgb(255, 10, 10, 10)))
            {
                g.FillRectangle(bBtn, winX + 26, btnY, btnInstallW, btnH);
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("CÀI ĐẶT NGAY // INSTALL", fBtn, bText, new RectangleF(winX + 26, btnY, btnInstallW, btnH), sf);
            }

            // Uninstall Button
            int btnUnX = winX + 26 + btnInstallW + 12;
            int btnUnW = 114;
            using (var bBtn = new SolidBrush(CardBg))
            using (var pBtn = new Pen(BorderColor))
            using (var fBtn = new Font("Segoe UI", 9f))
            using (var bRed = new SolidBrush(Color.FromArgb(255, 248, 81, 73)))
            {
                g.FillRectangle(bBtn, btnUnX, btnY, btnUnW, btnH);
                g.DrawRectangle(pBtn, btnUnX, btnY, btnUnW, btnH);
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("Gỡ cài đặt", fBtn, bRed, new RectangleF(btnUnX, btnY, btnUnW, btnH), sf);
            }

            // Open Folder Button
            int btnFoldX = btnUnX + btnUnW + 12;
            int btnFoldW = 108;
            using (var bBtn = new SolidBrush(CardBg))
            using (var pBtn = new Pen(BorderColor))
            using (var fBtn = new Font("Segoe UI", 9f))
            using (var bGray = new SolidBrush(TextMuted))
            {
                g.FillRectangle(bBtn, btnFoldX, btnY, btnFoldW, btnH);
                g.DrawRectangle(pBtn, btnFoldX, btnY, btnFoldW, btnH);
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("Mở Addin", fBtn, bGray, new RectangleF(btnFoldX, btnY, btnFoldW, btnH), sf);
            }

            // Bottom Footer
            int footY = btnY + btnH + 16;
            using (var fFoot = new Font("Segoe UI", 8f))
            using (var bFoot = new SolidBrush(TextMuted))
            {
                var sf = new StringFormat { Alignment = StringAlignment.Center };
                g.DrawString("• Tự động kiểm tra & cập nhật ngầm qua GitHub • 100% In-Memory Dynamic Load • Không khóa DLL", fFoot, bFoot, winX + winW / 2, footY, sf);
            }

            // 3. Annotations on the right / bottom
            DrawAnnotation(g, winX - 220, contentY + 24, "Master Header", "Space Grotesk + Segoe UI\nBản sắc thương hiệu mepOS");
            DrawAnnotation(g, winX - 220, toolsY + 24, "Option A Tool Icons", "Preview trực quan 2 tool CT & BL\nHiển thị ngay trong bộ cài");
            DrawAnnotation(g, winX - 220, revitY + 40, "Auto-Detect Revit", "Tự động phát hiện 2019-2026\nKích hoạt chọn lọc theo ý muốn");
            DrawAnnotation(g, winX - 220, btnY + 10, "1-Click Install", "Zero UAC • Không quyền Admin\nGhi thẳng %APPDATA% trong 1s");

            bmp.Save(outPath, ImageFormat.Png);
            bmp.Save(localPath, ImageFormat.Png);
        }

        Console.WriteLine("Installer Concept Demo Showcase generated successfully!");
    }

    static void DrawBadge(Graphics g, int x, int y, string text, Color bg, Color fg)
    {
        using (var f = new Font("Segoe UI", 8f, FontStyle.Bold))
        {
            var sz = g.MeasureString(text, f);
            int w = (int)sz.Width + 16;
            int h = 24;
            using (var b = new SolidBrush(bg))
            using (var p = new Pen(BorderColor))
            {
                g.FillRectangle(b, x, y, w, h);
                g.DrawRectangle(p, x, y, w, h);
            }
            using (var bFg = new SolidBrush(fg))
            {
                g.DrawString(text, f, bFg, x + 8, y + 4);
            }
        }
    }

    static void DrawToolCard(Graphics g, int x, int y, int w, int h, string iconPath, string title, string sub1, string sub2)
    {
        using (var b = new SolidBrush(CardBg))
        using (var p = new Pen(BorderColor))
        {
            g.FillRectangle(b, x, y, w, h);
            g.DrawRectangle(p, x, y, w, h);
        }

        if (File.Exists(iconPath))
        {
            using (var img = Image.FromFile(iconPath))
            {
                g.DrawImage(img, x + 16, y + (h - 32) / 2, 32, 32);
            }
        }

        using (var fT = new Font("Segoe UI", 9.5f, FontStyle.Bold))
        using (var fS1 = new Font("Segoe UI", 8f))
        using (var fS2 = new Font("Segoe UI", 7.5f))
        using (var bW = new SolidBrush(Color.White))
        using (var bY = new SolidBrush(YellowCore))
        using (var bM = new SolidBrush(TextMuted))
        {
            g.DrawString(title, fT, bW, x + 58, y + 10);
            g.DrawString(sub1, fS1, bY, x + 58, y + 30);
            g.DrawString(sub2, fS2, bM, x + 58, y + 48);
        }
    }

    static void DrawAnnotation(Graphics g, int x, int y, string title, string desc)
    {
        using (var fT = new Font("Segoe UI", 8.5f, FontStyle.Bold))
        using (var fD = new Font("Segoe UI", 7.5f))
        using (var bT = new SolidBrush(YellowCore))
        using (var bD = new SolidBrush(TextMuted))
        {
            g.DrawString("◆ " + title, fT, bT, x, y);
            g.DrawString(desc, fD, bD, x + 12, y + 16);
        }
    }
}
