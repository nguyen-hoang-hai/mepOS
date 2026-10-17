using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;

class RenderPureLetterConcepts
{
    static Color YellowCore = Color.FromArgb(255, 255, 221, 0);
    static Color BlackDark = Color.FromArgb(255, 10, 10, 10);
    static Color WhiteLight = Color.FromArgb(255, 255, 255, 255);

    static void Main()
    {
        string fontPath = @"D:\OneDrive\@ Personal Project\mepOS\assets\fonts\SpaceGrotesk-Bold.ttf";
        string outDir = @"C:\Users\2412n\.gemini\antigravity\brain\60591867-667c-4678-8243-090a84a63b0c";
        string assetsDir = @"D:\OneDrive\@ Personal Project\mepOS\assets\letter_concepts";
        Directory.CreateDirectory(assetsDir);

        using (var pfc = new PrivateFontCollection())
        {
            pfc.AddFontFile(fontPath);
            FontFamily family = pfc.Families[0];
            Console.WriteLine("Loaded font: " + family.Name);

            // 1. Render Concept A: Pure Floating Horizontal (Chữ nổi ngang tự do, không viền chữ 1)
            RenderConceptA(family, assetsDir);

            // 2. Render Concept B: High-Contrast Contoured (Cả 2 chữ đều có viền đen sắc nét)
            RenderConceptB(family, assetsDir);

            // 3. Render Concept C: Stacked Diagonal Monogram (Chữ xếp so le 2 tầng)
            RenderConceptC(family, assetsDir);

            // 4. Render Concept D: Architectural Underline (Chữ ngang + vạch kẻ kỹ thuật)
            RenderConceptD(family, assetsDir);

            // Copy all generated PNGs to brain artifact directory
            foreach (var f in Directory.GetFiles(assetsDir, "*.png"))
            {
                File.Copy(f, Path.Combine(outDir, Path.GetFileName(f)), true);
            }

            // 5. Render Master Typo Comparison Board (1200 x 920 px)
            RenderMasterTypoBoard(family, assetsDir, Path.Combine(outDir, "mepos_pure_letter_showcase.png"));
            File.Copy(Path.Combine(outDir, "mepos_pure_letter_showcase.png"), Path.Combine(assetsDir, "mepos_pure_letter_showcase.png"), true);

            // 6. Deploy Concept A to Loader Resources and official_icons
            string loaderResDir = @"D:\OneDrive\@ Personal Project\mepOS\src\mepOS.Loader\Resources";
            string officialDir = @"D:\OneDrive\@ Personal Project\mepOS\assets\official_icons";
            Directory.CreateDirectory(loaderResDir);
            Directory.CreateDirectory(officialDir);

            File.Copy(Path.Combine(assetsDir, "cA_ct_32.png"), Path.Combine(loaderResDir, "CT_32.png"), true);
            File.Copy(Path.Combine(assetsDir, "cA_ct_16.png"), Path.Combine(loaderResDir, "CT_16.png"), true);
            File.Copy(Path.Combine(assetsDir, "cA_bl_32.png"), Path.Combine(loaderResDir, "BL_32.png"), true);
            File.Copy(Path.Combine(assetsDir, "cA_bl_16.png"), Path.Combine(loaderResDir, "BL_16.png"), true);

            File.Copy(Path.Combine(assetsDir, "cA_ct_256.png"), Path.Combine(officialDir, "CT_256.png"), true);
            File.Copy(Path.Combine(assetsDir, "cA_ct_32.png"), Path.Combine(officialDir, "CT_32.png"), true);
            File.Copy(Path.Combine(assetsDir, "cA_ct_16.png"), Path.Combine(officialDir, "CT_16.png"), true);
            File.Copy(Path.Combine(assetsDir, "cA_bl_256.png"), Path.Combine(officialDir, "BL_256.png"), true);
            File.Copy(Path.Combine(assetsDir, "cA_bl_32.png"), Path.Combine(officialDir, "BL_32.png"), true);
            File.Copy(Path.Combine(assetsDir, "cA_bl_16.png"), Path.Combine(officialDir, "BL_16.png"), true);

            Console.WriteLine("Deployed Concept A icons to Loader Resources and official_icons!");
        }

        Console.WriteLine("All pure letter concepts and showcase board generated successfully!");
    }

    static void ConfigureHighQuality(Graphics g)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
    }

    // =========================================================================
    // CONCEPT A: PURE FLOATING HORIZONTAL (Chuẩn theo mepOS: chữ 1 đen, chữ 2 vàng viền đen)
    // =========================================================================
    static void RenderConceptA(FontFamily family, string dir)
    {
        RenderPair(family, "CT", "C", "T", dir, "cA", 1);
        RenderPair(family, "BL", "B", "L", dir, "cA", 1);
    }

    // =========================================================================
    // CONCEPT B: HIGH-CONTRAST CONTOURED (Chữ 1 Trắng/Đen có viền đen, chữ 2 Vàng viền đen)
    // =========================================================================
    static void RenderConceptB(FontFamily family, string dir)
    {
        RenderPair(family, "CT", "C", "T", dir, "cB", 2);
        RenderPair(family, "BL", "B", "L", dir, "cB", 2);
    }

    // =========================================================================
    // CONCEPT C: STACKED DIAGONAL MONOGRAM (So le tầng: C trên trái, T dưới phải)
    // =========================================================================
    static void RenderConceptC(FontFamily family, string dir)
    {
        RenderPair(family, "CT", "C", "T", dir, "cC", 3);
        RenderPair(family, "BL", "B", "L", dir, "cC", 3);
    }

    // =========================================================================
    // CONCEPT D: ARCHITECTURAL UNDERLINE (Ngang + gạch chân kỹ thuật màu vàng)
    // =========================================================================
    static void RenderConceptD(FontFamily family, string dir)
    {
        RenderPair(family, "CT", "C", "T", dir, "cD", 4);
        RenderPair(family, "BL", "B", "L", dir, "cD", 4);
    }

    static void RenderPair(FontFamily family, string tool, string l1, string l2, string dir, string prefix, int mode)
    {
        int[] sizes = new int[] { 256, 96, 32, 16 };
        using (var bmpMaster = new Bitmap(256, 256, PixelFormat.Format32bppArgb))
        {
            using (var g = Graphics.FromImage(bmpMaster))
            {
                ConfigureHighQuality(g);
                g.Clear(Color.Transparent);

                switch (mode)
                {
                    case 1: // Concept A
                        DrawHorizontalPure(g, family, l1, l2, 256);
                        break;
                    case 2: // Concept B
                        DrawContouredPure(g, family, l1, l2, 256);
                        break;
                    case 3: // Concept C
                        DrawStackedDiagonal(g, family, l1, l2, 256);
                        break;
                    case 4: // Concept D
                        DrawWithUnderline(g, family, l1, l2, 256);
                        break;
                }
            }

            foreach (int s in sizes)
            {
                using (var resized = new Bitmap(s, s, PixelFormat.Format32bppArgb))
                {
                    using (var gr = Graphics.FromImage(resized))
                    {
                        gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        gr.SmoothingMode = SmoothingMode.HighQuality;
                        gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        gr.DrawImage(bmpMaster, 0, 0, s, s);
                    }
                    string path = Path.Combine(dir, string.Format("{0}_{1}_{2}.png", prefix, tool.ToLower(), s));
                    resized.Save(path, ImageFormat.Png);
                }
            }
        }
    }

    // =========================================================================
    // CONCEPT A: DUAL OUTLINED (Chữ 1 Trắng viền Đen, Chữ 2 Vàng viền Đen)
    // =========================================================================
    static void DrawHorizontalPure(Graphics g, FontFamily family, string l1, string l2, int s)
    {
        float fontSize = s * 0.58f;
        float strokeW = s * 0.055f; // Sắc nét và không nuốt nét chữ

        float w1 = 0, h1 = 0, w2 = 0;
        using (var p1 = new GraphicsPath())
        {
            p1.AddString(l1, family, (int)FontStyle.Bold, fontSize, new PointF(0, 0), StringFormat.GenericTypographic);
            var b1 = p1.GetBounds();
            w1 = b1.Width;
            h1 = b1.Height;
        }
        using (var p2 = new GraphicsPath())
        {
            p2.AddString(l2, family, (int)FontStyle.Bold, fontSize, new PointF(0, 0), StringFormat.GenericTypographic);
            w2 = p2.GetBounds().Width;
        }

        float kerning = s * 0.02f;
        float totalW = w1 + w2 - kerning;
        float startX = (s - totalW) / 2f;
        float startY = (s - h1) / 2f - s * 0.02f;

        // Letter 1: White with solid black stroke (paint-order: stroke fill)
        using (var path1 = new GraphicsPath())
        {
            path1.AddString(l1, family, (int)FontStyle.Bold, fontSize, new PointF(startX, startY), StringFormat.GenericTypographic);
            using (var p = new Pen(BlackDark, strokeW * 2f) { LineJoin = LineJoin.Round }) g.DrawPath(p, path1);
            using (var b = new SolidBrush(WhiteLight)) g.FillPath(b, path1);
        }

        // Letter 2: Yellow with solid black stroke
        float x2 = startX + w1 - kerning;
        using (var path2 = new GraphicsPath())
        {
            path2.AddString(l2, family, (int)FontStyle.Bold, fontSize, new PointF(x2, startY), StringFormat.GenericTypographic);
            using (var p = new Pen(BlackDark, strokeW * 2f) { LineJoin = LineJoin.Round }) g.DrawPath(p, path2);
            using (var b = new SolidBrush(YellowCore)) g.FillPath(b, path2);
        }
    }

    // =========================================================================
    // CONCEPT B: FULL ELECTRIC YELLOW (Cả 2 chữ Vàng Neon viền Đen kỹ thuật)
    // =========================================================================
    static void DrawContouredPure(Graphics g, FontFamily family, string l1, string l2, int s)
    {
        float fontSize = s * 0.58f;
        float strokeW = s * 0.055f;

        float w1 = 0, h1 = 0, w2 = 0;
        using (var p1 = new GraphicsPath())
        {
            p1.AddString(l1, family, (int)FontStyle.Bold, fontSize, new PointF(0, 0), StringFormat.GenericTypographic);
            var b1 = p1.GetBounds();
            w1 = b1.Width;
            h1 = b1.Height;
        }
        using (var p2 = new GraphicsPath())
        {
            p2.AddString(l2, family, (int)FontStyle.Bold, fontSize, new PointF(0, 0), StringFormat.GenericTypographic);
            w2 = p2.GetBounds().Width;
        }

        float kerning = s * 0.02f;
        float totalW = w1 + w2 - kerning;
        float startX = (s - totalW) / 2f;
        float startY = (s - h1) / 2f - s * 0.02f;

        // Letter 1: Electric Yellow with bold black stroke
        using (var path1 = new GraphicsPath())
        {
            path1.AddString(l1, family, (int)FontStyle.Bold, fontSize, new PointF(startX, startY), StringFormat.GenericTypographic);
            using (var p = new Pen(BlackDark, strokeW * 2f) { LineJoin = LineJoin.Round }) g.DrawPath(p, path1);
            using (var b = new SolidBrush(YellowCore)) g.FillPath(b, path1);
        }

        // Letter 2: Electric Yellow with bold black stroke
        float x2 = startX + w1 - kerning;
        using (var path2 = new GraphicsPath())
        {
            path2.AddString(l2, family, (int)FontStyle.Bold, fontSize, new PointF(x2, startY), StringFormat.GenericTypographic);
            using (var p = new Pen(BlackDark, strokeW * 2f) { LineJoin = LineJoin.Round }) g.DrawPath(p, path2);
            using (var b = new SolidBrush(YellowCore)) g.FillPath(b, path2);
        }
    }

    // =========================================================================
    // CONCEPT C: STACKED DIAGONAL MONOGRAM (Chữ 1 Trắng trên trái, Chữ 2 Vàng dưới phải đè nhẹ)
    // =========================================================================
    static void DrawStackedDiagonal(Graphics g, FontFamily family, string l1, string l2, int s)
    {
        float fontSize = s * 0.52f;
        float strokeW = s * 0.05f;

        // Letter 1 at top-left (White with black stroke)
        float x1 = s * 0.12f;
        float y1 = s * 0.08f;
        using (var path1 = new GraphicsPath())
        {
            path1.AddString(l1, family, (int)FontStyle.Bold, fontSize, new PointF(x1, y1), StringFormat.GenericTypographic);
            using (var p = new Pen(BlackDark, strokeW * 2f) { LineJoin = LineJoin.Round }) g.DrawPath(p, path1);
            using (var b = new SolidBrush(WhiteLight)) g.FillPath(b, path1);
        }

        // Letter 2 at bottom-right, overlapping with thick black stroke to mask
        float x2 = s * 0.44f;
        float y2 = s * 0.38f;
        using (var path2 = new GraphicsPath())
        {
            path2.AddString(l2, family, (int)FontStyle.Bold, fontSize, new PointF(x2, y2), StringFormat.GenericTypographic);
            using (var p = new Pen(BlackDark, strokeW * 2.6f) { LineJoin = LineJoin.Round }) g.DrawPath(p, path2);
            using (var b = new SolidBrush(YellowCore)) g.FillPath(b, path2);
        }
    }

    // =========================================================================
    // CONCEPT D: ARCHITECTURAL UNDERLINE (Chữ ngang Trắng/Vàng + vạch chân kỹ thuật)
    // =========================================================================
    static void DrawWithUnderline(Graphics g, FontFamily family, string l1, string l2, int s)
    {
        float fontSize = s * 0.50f;
        float strokeW = s * 0.045f;

        float w1 = 0, h1 = 0, w2 = 0;
        using (var p1 = new GraphicsPath())
        {
            p1.AddString(l1, family, (int)FontStyle.Bold, fontSize, new PointF(0, 0), StringFormat.GenericTypographic);
            var b1 = p1.GetBounds();
            w1 = b1.Width;
            h1 = b1.Height;
        }
        using (var p2 = new GraphicsPath())
        {
            p2.AddString(l2, family, (int)FontStyle.Bold, fontSize, new PointF(0, 0), StringFormat.GenericTypographic);
            w2 = p2.GetBounds().Width;
        }

        float kerning = s * 0.02f;
        float totalW = w1 + w2 - kerning;
        float startX = (s - totalW) / 2f;
        float startY = (s - h1) / 2f - s * 0.09f;

        // Letter 1: White with black stroke
        using (var path1 = new GraphicsPath())
        {
            path1.AddString(l1, family, (int)FontStyle.Bold, fontSize, new PointF(startX, startY), StringFormat.GenericTypographic);
            using (var p = new Pen(BlackDark, strokeW * 2f) { LineJoin = LineJoin.Round }) g.DrawPath(p, path1);
            using (var b = new SolidBrush(WhiteLight)) g.FillPath(b, path1);
        }

        // Letter 2: Yellow with black stroke
        float x2 = startX + w1 - kerning;
        using (var path2 = new GraphicsPath())
        {
            path2.AddString(l2, family, (int)FontStyle.Bold, fontSize, new PointF(x2, startY), StringFormat.GenericTypographic);
            using (var p = new Pen(BlackDark, strokeW * 2f) { LineJoin = LineJoin.Round }) g.DrawPath(p, path2);
            using (var b = new SolidBrush(YellowCore)) g.FillPath(b, path2);
        }

        // Architectural Underline bar
        float barY = startY + h1 + s * 0.07f;
        float barH = s * 0.06f;
        using (var bBar = new SolidBrush(YellowCore))
        using (var pBar = new Pen(BlackDark, strokeW * 1.5f))
        {
            g.FillRectangle(bBar, startX, barY, totalW, barH);
            g.DrawRectangle(pBar, startX, barY, totalW, barH);
        }
    }

    // =========================================================================
    // MASTER SHOWCASE COMPARISON BOARD
    // =========================================================================
    static void RenderMasterTypoBoard(FontFamily family, string assetsDir, string boardPath)
    {
        int width = 1200;
        int height = 960;

        using (var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb))
        using (var g = Graphics.FromImage(bmp))
        {
            ConfigureHighQuality(g);

            // Dark Studio Background
            using (var b = new SolidBrush(Color.FromArgb(255, 14, 17, 22))) g.FillRectangle(b, 0, 0, width, height);

            // Top Header Bar
            using (var b = new SolidBrush(Color.FromArgb(255, 22, 27, 34))) g.FillRectangle(b, 0, 0, width, 88);
            using (var p = new Pen(Color.FromArgb(255, 48, 54, 61), 1f)) g.DrawLine(p, 0, 88, width, 88);

            // Logo & Title
            using (var fBold = new Font("Arial", 28, FontStyle.Bold))
            using (var fSub = new Font("Arial", 11, FontStyle.Regular))
            using (var bWhite = new SolidBrush(Color.White))
            using (var bYellow = new SolidBrush(YellowCore))
            using (var bGray = new SolidBrush(Color.FromArgb(255, 139, 148, 158)))
            {
                g.DrawString("mep", fBold, bWhite, 36, 18);
                float mepW = g.MeasureString("mep", fBold).Width;
                g.DrawString("OS", fBold, bYellow, 36 + mepW - 10, 18);

                g.DrawString("// BẢNG SO SÁNH 4 PHƯƠNG ÁN CHỮ CÁI (PURE MONOGRAM / TYPOGRAPHY)", fSub, bGray, 38, 58);
            }

            string[] titles = {
                "PHƯƠNG ÁN A: Dual Outlined (Đề xuất)",
                "PHƯƠNG ÁN B: Full Electric Yellow",
                "PHƯƠNG ÁN C: Stacked Diagonal Monogram",
                "PHƯƠNG ÁN D: Architectural Underline"
            };

            string[] descriptions = {
                "Chữ 1 Trắng viền Đen, Chữ 2 Vàng viền Đen.\nTối giản, tinh gọn, tương phản sắc nét trên cả Ribbon Sáng & Tối.",
                "Cả 2 chữ cái đều là Vàng Neon viền Đen kỹ thuật.\nĐồng nhất sắc màu nhận diện mepOS, nổi bật nhất trên Ribbon.",
                "Bố cục chữ lồng 2 tầng (Trắng trên trái, Vàng dưới phải đè nhẹ).\nPhong cách monogram kiến trúc hiện đại, năng động.",
                "Chữ ngang Trắng/Vàng kết hợp vạch chân kỹ thuật màu vàng.\nTạo trật tự kỹ thuật và chân đế vững vàng cho công cụ."
            };

            string[] prefixes = { "cA", "cB", "cC", "cD" };

            int colW = 270;
            int colGap = 20;
            int startX = 36;
            int startY = 110;

            for (int i = 0; i < 4; i++)
            {
                int x = startX + i * (colW + colGap);
                int y = startY;

                // Card background
                using (var b = new SolidBrush(Color.FromArgb(255, 22, 27, 34)))
                using (var p = new Pen(i == 0 ? YellowCore : Color.FromArgb(255, 48, 54, 61), i == 0 ? 2f : 1f))
                {
                    g.FillRectangle(b, x, y, colW, 810);
                    g.DrawRectangle(p, x, y, colW, 810);
                }

                // Card Header
                using (var bH = new SolidBrush(Color.FromArgb(255, 30, 36, 46))) g.FillRectangle(bH, x, y, colW, 46);
                using (var fT = new Font("Arial", 10.5f, FontStyle.Bold))
                using (var bT = new SolidBrush(i == 0 ? YellowCore : Color.White))
                {
                    g.DrawString(titles[i], fT, bT, new RectangleF(x + 10, y + 6, colW - 20, 36));
                }

                // Description
                using (var fD = new Font("Arial", 9.5f))
                using (var bD = new SolidBrush(Color.FromArgb(255, 160, 170, 185)))
                {
                    g.DrawString(descriptions[i], fD, bD, new RectangleF(x + 12, y + 54, colW - 24, 60));
                }

                // Large Preview Section (96x96)
                using (var fSec = new Font("Arial", 9, FontStyle.Bold))
                using (var bSec = new SolidBrush(Color.FromArgb(255, 110, 120, 135)))
                {
                    g.DrawString("CHI TIẾT HIỂN THỊ (96×96)", fSec, bSec, x + 14, y + 120);
                }

                string imgCtPath = Path.Combine(assetsDir, string.Format("{0}_ct_96.png", prefixes[i]));
                string imgBlPath = Path.Combine(assetsDir, string.Format("{0}_bl_96.png", prefixes[i]));

                // Icon Box CT & BL
                using (var bBox = new SolidBrush(Color.FromArgb(255, 13, 17, 23)))
                using (var pBox = new Pen(Color.FromArgb(255, 48, 54, 61)))
                {
                    g.FillRectangle(bBox, x + 16, y + 142, 108, 108);
                    g.DrawRectangle(pBox, x + 16, y + 142, 108, 108);

                    g.FillRectangle(bBox, x + 144, y + 142, 108, 108);
                    g.DrawRectangle(pBox, x + 144, y + 142, 108, 108);
                }

                if (File.Exists(imgCtPath))
                {
                    using (var img = Image.FromFile(imgCtPath)) g.DrawImage(img, x + 22, y + 148, 96, 96);
                }
                if (File.Exists(imgBlPath))
                {
                    using (var img = Image.FromFile(imgBlPath)) g.DrawImage(img, x + 150, y + 148, 96, 96);
                }

                // Labels
                using (var fLbl = new Font("Arial", 9.5f, FontStyle.Bold))
                using (var bLbl = new SolidBrush(Color.White))
                using (var bCmd = new SolidBrush(YellowCore))
                {
                    g.DrawString("Connect To", fLbl, bLbl, x + 28, y + 256);
                    g.DrawString("[CT]", fLbl, bCmd, x + 54, y + 274);

                    g.DrawString("Bloom", fLbl, bLbl, x + 172, y + 256);
                    g.DrawString("[BL]", fLbl, bCmd, x + 178, y + 274);
                }

                // Revit Ribbon Simulation (Light Mode)
                using (var fSec = new Font("Arial", 9, FontStyle.Bold))
                using (var bSec = new SolidBrush(Color.FromArgb(255, 110, 120, 135)))
                {
                    g.DrawString("REVIT RIBBON LIGHT (32×32)", fSec, bSec, x + 14, y + 310);
                }
                string ct32 = Path.Combine(assetsDir, string.Format("{0}_ct_32.png", prefixes[i]));
                string bl32 = Path.Combine(assetsDir, string.Format("{0}_bl_32.png", prefixes[i]));
                DrawRevitRibbonSimulation(g, x + 14, y + 332, colW - 28, ct32, bl32, false);

                // Revit Ribbon Simulation (Dark Mode)
                using (var fSec = new Font("Arial", 9, FontStyle.Bold))
                using (var bSec = new SolidBrush(Color.FromArgb(255, 110, 120, 135)))
                {
                    g.DrawString("REVIT RIBBON DARK (32×32)", fSec, bSec, x + 14, y + 550);
                }
                DrawRevitRibbonSimulation(g, x + 14, y + 572, colW - 28, ct32, bl32, true);
            }

            bmp.Save(boardPath, ImageFormat.Png);
        }
    }

    static void DrawRevitRibbonSimulation(Graphics g, int x, int y, int w, string ctPath, string blPath, bool isDark)
    {
        int h = 180;
        Color bgBar = isDark ? Color.FromArgb(255, 37, 37, 38) : Color.FromArgb(255, 240, 240, 240);
        Color borderBar = isDark ? Color.FromArgb(255, 62, 62, 66) : Color.FromArgb(255, 208, 208, 208);
        Color bgPanel = isDark ? Color.FromArgb(255, 45, 45, 48) : Color.FromArgb(255, 247, 247, 247);
        Color textPrimary = isDark ? Color.FromArgb(255, 230, 230, 230) : Color.FromArgb(255, 30, 30, 30);
        Color textMuted = isDark ? Color.FromArgb(255, 150, 150, 150) : Color.FromArgb(255, 120, 120, 120);

        using (var b = new SolidBrush(bgBar))
        using (var p = new Pen(borderBar))
        {
            g.FillRectangle(b, x, y, w, h);
            g.DrawRectangle(p, x, y, w, h);
        }

        int pW = w - 16;
        int pX = x + 8;
        int pY = y + 8;
        int pH = h - 16;

        using (var b = new SolidBrush(bgPanel))
        using (var p = new Pen(borderBar))
        {
            g.FillRectangle(b, pX, pY, pW, pH);
            g.DrawRectangle(p, pX, pY, pW, pH);
        }

        int btnW = (pW - 16) / 2;
        int btnH = pH - 28;

        DrawRevitButton(g, pX + 6, pY + 6, btnW, btnH, ctPath, "Connect To", textPrimary, isDark);
        DrawRevitButton(g, pX + 10 + btnW, pY + 6, btnW, btnH, blPath, "Bloom", textPrimary, isDark);

        using (var fPanel = new Font("Segoe UI", 8.5f, FontStyle.Regular))
        using (var bText = new SolidBrush(textMuted))
        {
            var sz = g.MeasureString("Modeling", fPanel);
            g.DrawString("Modeling", fPanel, bText, pX + (pW - sz.Width) / 2f, pY + pH - 20);
        }
    }

    static void DrawRevitButton(Graphics g, int bx, int by, int bw, int bh, string iconPath, string text, Color textColor, bool isDark)
    {
        Color btnBorder = isDark ? Color.FromArgb(255, 60, 60, 65) : Color.FromArgb(255, 225, 225, 225);
        using (var p = new Pen(btnBorder)) g.DrawRectangle(p, bx, by, bw, bh);

        int iconX = bx + (bw - 32) / 2;
        int iconY = by + 14;

        if (File.Exists(iconPath))
        {
            using (var img = Image.FromFile(iconPath))
            {
                g.DrawImage(img, iconX, iconY, 32, 32);
            }
        }

        using (var f = new Font("Segoe UI", 8.5f))
        using (var b = new SolidBrush(textColor))
        {
            var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Near
            };
            g.DrawString(text, f, b, new RectangleF(bx, iconY + 36, bw, 36), sf);
        }
    }
}
