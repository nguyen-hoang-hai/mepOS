using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;

class RenderIconConcepts
{
    static Color MeYellow = Color.FromArgb(255, 255, 221, 0);
    static Color MeBlack = Color.FromArgb(255, 10, 10, 10);
    static Color PipeDark = Color.FromArgb(255, 45, 52, 64);
    static Color PipeFlange = Color.FromArgb(255, 75, 85, 102);

    static void Main()
    {
        string outDir = @"C:\Users\2412n\.gemini\antigravity\brain\60591867-667c-4678-8243-090a84a63b0c";
        string assetsDir = @"D:\OneDrive\@ Personal Project\mepOS\assets\icon_concepts";
        Directory.CreateDirectory(assetsDir);

        // 1. Render Individual 96x96 Icons for each Concept
        RenderConcept1(Path.Combine(assetsDir, "c1_ct.png"), Path.Combine(assetsDir, "c1_bl.png"));
        RenderConcept2(Path.Combine(assetsDir, "c2_ct.png"), Path.Combine(assetsDir, "c2_bl.png"));
        RenderConcept3(Path.Combine(assetsDir, "c3_ct.png"), Path.Combine(assetsDir, "c3_bl.png"));
        RenderConcept4(Path.Combine(assetsDir, "c4_ct.png"), Path.Combine(assetsDir, "c4_bl.png"));

        // Copy individual icons to artifact folder
        foreach (var f in Directory.GetFiles(assetsDir, "*.png"))
        {
            File.Copy(f, Path.Combine(outDir, Path.GetFileName(f)), true);
        }

        // 2. Render Master Showcase Comparison Board (1200 x 920 px)
        RenderMasterShowcaseBoard(assetsDir, Path.Combine(outDir, "mepos_icon_concepts_preview.png"));
        File.Copy(Path.Combine(outDir, "mepos_icon_concepts_preview.png"), Path.Combine(assetsDir, "mepos_icon_concepts_preview.png"), true);

        Console.WriteLine("All concepts and master preview rendered successfully!");
    }

    static void ConfigureHighQuality(Graphics g)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
    }

    // ==========================================
    // CONCEPT 1: TECHNICAL MEP DRAFTING
    // ==========================================
    static void RenderConcept1(string ctPath, string blPath)
    {
        // CT
        using (var bmp = new Bitmap(96, 96, PixelFormat.Format32bppArgb))
        using (var g = Graphics.FromImage(bmp))
        {
            ConfigureHighQuality(g);
            g.Clear(Color.Transparent);

            // Pipe 1 (Top Left)
            using (var b = new SolidBrush(PipeDark)) g.FillRectangle(b, 10, 16, 32, 20);
            using (var b = new SolidBrush(PipeFlange)) g.FillRectangle(b, 38, 12, 8, 28);
            using (var p = new Pen(MeBlack, 3.5f))
            {
                g.DrawRectangle(p, 10, 16, 32, 20);
                g.DrawRectangle(p, 38, 12, 8, 28);
            }

            // Pipe 2 (Bottom Right)
            using (var b = new SolidBrush(PipeDark)) g.FillRectangle(b, 54, 60, 32, 20);
            using (var b = new SolidBrush(PipeFlange)) g.FillRectangle(b, 50, 56, 8, 28);
            using (var p = new Pen(MeBlack, 3.5f))
            {
                g.DrawRectangle(p, 54, 60, 32, 20);
                g.DrawRectangle(p, 50, 56, 8, 28);
            }

            // Dynamic S-Curve Bridge
            using (var path = new GraphicsPath())
            {
                path.AddBezier(new PointF(42, 26), new PointF(52, 26), new PointF(50, 70), new PointF(54, 70));
                using (var penBlack = new Pen(MeBlack, 12f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    g.DrawPath(penBlack, path);
                }
                using (var penYellow = new Pen(MeYellow, 7f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    g.DrawPath(penYellow, path);
                }
            }

            // Snap Node Pulses
            using (var b = new SolidBrush(MeYellow))
            using (var p = new Pen(MeBlack, 3f))
            {
                g.FillEllipse(b, 37, 21, 10, 10);
                g.DrawEllipse(p, 37, 21, 10, 10);

                g.FillEllipse(b, 49, 65, 10, 10);
                g.DrawEllipse(p, 49, 65, 10, 10);
            }

            // Arrow Head
            PointF[] arrow = { new PointF(68, 54), new PointF(76, 70), new PointF(60, 70) };
            using (var b = new SolidBrush(MeYellow)) g.FillPolygon(b, arrow);
            using (var p = new Pen(MeBlack, 2.5f)) g.DrawPolygon(p, arrow);

            bmp.Save(ctPath, ImageFormat.Png);
        }

        // BL
        using (var bmp = new Bitmap(96, 96, PixelFormat.Format32bppArgb))
        using (var g = Graphics.FromImage(bmp))
        {
            ConfigureHighQuality(g);
            g.Clear(Color.Transparent);

            // Center Valve/Fitting Body
            using (var b = new SolidBrush(PipeDark)) g.FillRectangle(b, 34, 34, 28, 28);
            using (var p = new Pen(MeBlack, 3.5f)) g.DrawRectangle(p, 34, 34, 28, 28);

            // Left Sprout
            DrawSprout(g, new PointF(34, 48), new PointF(14, 48), true);
            // Top Sprout
            DrawSprout(g, new PointF(48, 34), new PointF(48, 14), false);
            // Right Sprout
            DrawSprout(g, new PointF(62, 48), new PointF(82, 48), true);

            // Center Core Accent
            using (var b = new SolidBrush(MeYellow))
            using (var p = new Pen(MeBlack, 3f))
            {
                g.FillEllipse(b, 43, 43, 10, 10);
                g.DrawEllipse(p, 43, 43, 10, 10);
            }

            bmp.Save(blPath, ImageFormat.Png);
        }
    }

    static void DrawSprout(Graphics g, PointF p1, PointF p2, bool isHoriz)
    {
        using (var pBlack = new Pen(MeBlack, 11f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
        using (var pYellow = new Pen(MeYellow, 6.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
        {
            g.DrawLine(pBlack, p1, p2);
            g.DrawLine(pYellow, p1, p2);
        }
        // Flange cap
        float sz = 9f;
        if (isHoriz)
        {
            using (var b = new SolidBrush(MeBlack)) g.FillRectangle(b, p2.X - 2, p2.Y - sz, 4, sz * 2);
        }
        else
        {
            using (var b = new SolidBrush(MeBlack)) g.FillRectangle(b, p2.X - sz, p2.Y - 2, sz * 2, 4);
        }
    }

    // ==========================================
    // CONCEPT 2: ISOMETRIC SOLID 3D
    // ==========================================
    static void RenderConcept2(string ctPath, string blPath)
    {
        // CT
        using (var bmp = new Bitmap(96, 96, PixelFormat.Format32bppArgb))
        using (var g = Graphics.FromImage(bmp))
        {
            ConfigureHighQuality(g);
            g.Clear(Color.Transparent);

            // Top Iso Pipe
            PointF[] topFace1 = { new PointF(12, 30), new PointF(36, 16), new PointF(48, 24), new PointF(24, 38) };
            PointF[] sideFace1 = { new PointF(12, 30), new PointF(24, 38), new PointF(24, 50), new PointF(12, 42) };
            using (var b = new SolidBrush(Color.FromArgb(255, 71, 85, 105))) g.FillPolygon(b, topFace1);
            using (var b = new SolidBrush(Color.FromArgb(255, 51, 65, 85))) g.FillPolygon(b, sideFace1);
            using (var p = new Pen(MeBlack, 3f))
            {
                g.DrawPolygon(p, topFace1);
                g.DrawPolygon(p, sideFace1);
            }

            // Yellow Iso Transition Elbow
            PointF[] isoElbowTop = { new PointF(36, 30), new PointF(60, 44), new PointF(72, 36), new PointF(48, 24) };
            PointF[] isoElbowSide = { new PointF(60, 44), new PointF(72, 36), new PointF(72, 64), new PointF(60, 72) };
            using (var b = new SolidBrush(MeYellow)) g.FillPolygon(b, isoElbowTop);
            using (var b = new SolidBrush(Color.FromArgb(255, 230, 198, 0))) g.FillPolygon(b, isoElbowSide);
            using (var p = new Pen(MeBlack, 3.5f))
            {
                g.DrawPolygon(p, isoElbowTop);
                g.DrawPolygon(p, isoElbowSide);
            }

            // Bottom Iso Pipe
            PointF[] botFace = { new PointF(60, 72), new PointF(84, 58), new PointF(84, 70), new PointF(60, 84) };
            PointF[] botSide = { new PointF(36, 58), new PointF(60, 72), new PointF(60, 84), new PointF(36, 70) };
            using (var b = new SolidBrush(Color.FromArgb(255, 51, 65, 85))) g.FillPolygon(b, botFace);
            using (var b = new SolidBrush(Color.FromArgb(255, 30, 41, 59))) g.FillPolygon(b, botSide);
            using (var p = new Pen(MeBlack, 3f))
            {
                g.DrawPolygon(p, botFace);
                g.DrawPolygon(p, botSide);
            }

            // Snap circles
            using (var b = new SolidBrush(Color.White))
            using (var p = new Pen(MeBlack, 2.5f))
            {
                g.FillEllipse(b, 45, 21, 8, 8);
                g.DrawEllipse(p, 45, 21, 8, 8);
                g.FillEllipse(b, 69, 61, 8, 8);
                g.DrawEllipse(p, 69, 61, 8, 8);
            }

            bmp.Save(ctPath, ImageFormat.Png);
        }

        // BL
        using (var bmp = new Bitmap(96, 96, PixelFormat.Format32bppArgb))
        using (var g = Graphics.FromImage(bmp))
        {
            ConfigureHighQuality(g);
            g.Clear(Color.Transparent);

            // Center Iso Manifold Cube
            PointF[] cTop = { new PointF(48, 30), new PointF(69, 42), new PointF(48, 54), new PointF(27, 42) };
            PointF[] cLeft = { new PointF(27, 42), new PointF(48, 54), new PointF(48, 75), new PointF(27, 63) };
            PointF[] cRight = { new PointF(48, 54), new PointF(69, 42), new PointF(69, 63), new PointF(48, 75) };

            using (var b = new SolidBrush(Color.FromArgb(255, 71, 85, 105))) g.FillPolygon(b, cTop);
            using (var b = new SolidBrush(Color.FromArgb(255, 30, 41, 59))) g.FillPolygon(b, cLeft);
            using (var b = new SolidBrush(Color.FromArgb(255, 51, 65, 85))) g.FillPolygon(b, cRight);
            using (var p = new Pen(MeBlack, 3.5f))
            {
                g.DrawPolygon(p, cTop);
                g.DrawPolygon(p, cLeft);
                g.DrawPolygon(p, cRight);
            }

            // Sprouting Iso Stubs
            // Top vertical
            DrawIsoStub(g, new PointF(48, 30), new PointF(48, 12));
            // Bottom Right
            DrawIsoStub(g, new PointF(69, 52), new PointF(86, 62));
            // Bottom Left
            DrawIsoStub(g, new PointF(27, 52), new PointF(10, 62));

            // Top cap spark
            using (var b = new SolidBrush(MeYellow))
            using (var p = new Pen(MeBlack, 2f))
            {
                g.FillEllipse(b, 44, 8, 8, 8);
                g.DrawEllipse(p, 44, 8, 8, 8);
            }

            bmp.Save(blPath, ImageFormat.Png);
        }
    }

    static void DrawIsoStub(Graphics g, PointF p1, PointF p2)
    {
        using (var pBlack = new Pen(MeBlack, 10f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
        using (var pYellow = new Pen(MeYellow, 6f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
        {
            g.DrawLine(pBlack, p1, p2);
            g.DrawLine(pYellow, p1, p2);
        }
    }

    // ==========================================
    // CONCEPT 3: SWISS MINIMALIST GLYPH
    // ==========================================
    static void RenderConcept3(string ctPath, string blPath)
    {
        // CT
        using (var bmp = new Bitmap(96, 96, PixelFormat.Format32bppArgb))
        using (var g = Graphics.FromImage(bmp))
        {
            ConfigureHighQuality(g);
            g.Clear(Color.Transparent);

            // Two solid black square nodes
            using (var b = new SolidBrush(MeBlack))
            {
                g.FillRectangle(b, 12, 24, 24, 24);
                g.FillRectangle(b, 60, 48, 24, 24);
            }

            // Dynamic lightning / bridge connector
            PointF[] bridge = { new PointF(32, 36), new PointF(52, 36), new PointF(44, 60), new PointF(64, 60) };
            using (var pBlack = new Pen(MeBlack, 14f) { StartCap = LineCap.Square, EndCap = LineCap.Square })
            using (var pYellow = new Pen(MeYellow, 8f) { StartCap = LineCap.Square, EndCap = LineCap.Square })
            {
                g.DrawLines(pBlack, bridge);
                g.DrawLines(pYellow, bridge);
            }

            // Center yellow target reticle dots
            using (var b = new SolidBrush(MeYellow))
            {
                g.FillEllipse(b, 21, 33, 6, 6);
                g.FillEllipse(b, 69, 57, 6, 6);
            }

            bmp.Save(ctPath, ImageFormat.Png);
        }

        // BL
        using (var bmp = new Bitmap(96, 96, PixelFormat.Format32bppArgb))
        using (var g = Graphics.FromImage(bmp))
        {
            ConfigureHighQuality(g);
            g.Clear(Color.Transparent);

            // 4 Radial Arms
            Action<PointF, PointF> drawArm = delegate(PointF p1, PointF p2) {
                using (var pBlack = new Pen(MeBlack, 14f) { StartCap = LineCap.Square, EndCap = LineCap.Square })
                using (var pYellow = new Pen(MeYellow, 8f) { StartCap = LineCap.Square, EndCap = LineCap.Square })
                {
                    g.DrawLine(pBlack, p1, p2);
                    g.DrawLine(pYellow, p1, p2);
                }
            };

            drawArm(new PointF(48, 33), new PointF(48, 12));  // North
            drawArm(new PointF(63, 48), new PointF(84, 48));  // East
            drawArm(new PointF(48, 63), new PointF(48, 84));  // South
            drawArm(new PointF(33, 48), new PointF(12, 48));  // West

            // Central Black Hub
            using (var b = new SolidBrush(MeBlack))
            {
                g.FillEllipse(b, 33, 33, 30, 30);
            }
            // Inner Core Accent
            using (var b = new SolidBrush(MeYellow))
            {
                g.FillEllipse(b, 44, 44, 8, 8);
            }

            bmp.Save(blPath, ImageFormat.Png);
        }
    }

    // ==========================================
    // CONCEPT 4: CAD BLUEPRINT & SNAP
    // ==========================================
    static void RenderConcept4(string ctPath, string blPath)
    {
        // CT
        using (var bmp = new Bitmap(96, 96, PixelFormat.Format32bppArgb))
        using (var g = Graphics.FromImage(bmp))
        {
            ConfigureHighQuality(g);
            g.Clear(Color.Transparent);

            // Pipe 1 CAD Double-line
            using (var b = new SolidBrush(Color.FromArgb(255, 230, 235, 242))) g.FillRectangle(b, 8, 20, 32, 18);
            using (var p = new Pen(MeBlack, 2.5f)) g.DrawRectangle(p, 8, 20, 32, 18);
            using (var pDash = new Pen(MeBlack, 1.5f) { DashPattern = new float[] { 4, 3 } })
            {
                g.DrawLine(pDash, 2, 29, 44, 29);
            }

            // Pipe 2 CAD Double-line
            using (var b = new SolidBrush(Color.FromArgb(255, 230, 235, 242))) g.FillRectangle(b, 56, 56, 32, 18);
            using (var p = new Pen(MeBlack, 2.5f)) g.DrawRectangle(p, 56, 56, 32, 18);
            using (var pDash = new Pen(MeBlack, 1.5f) { DashPattern = new float[] { 4, 3 } })
            {
                g.DrawLine(pDash, 50, 65, 92, 65);
            }

            // Transition Path
            using (var path = new GraphicsPath())
            {
                path.AddBezier(new PointF(40, 29), new PointF(54, 29), new PointF(50, 65), new PointF(56, 65));
                using (var pBlack = new Pen(MeBlack, 9f)) g.DrawPath(pBlack, path);
                using (var pYellow = new Pen(MeYellow, 5f)) g.DrawPath(pYellow, path);
            }

            // CAD Snap Crosshair Box
            using (var b = new SolidBrush(MeYellow)) g.FillRectangle(b, 52, 61, 8, 8);
            using (var p = new Pen(MeBlack, 2f))
            {
                g.DrawRectangle(p, 52, 61, 8, 8);
                g.DrawLine(p, 56, 53, 56, 77);
                g.DrawLine(p, 44, 65, 68, 65);
            }

            bmp.Save(ctPath, ImageFormat.Png);
        }

        // BL
        using (var bmp = new Bitmap(96, 96, PixelFormat.Format32bppArgb))
        using (var g = Graphics.FromImage(bmp))
        {
            ConfigureHighQuality(g);
            g.Clear(Color.Transparent);

            // Tee outline
            using (var b = new SolidBrush(Color.FromArgb(255, 230, 235, 242)))
            {
                g.FillRectangle(b, 30, 30, 36, 36);
                g.FillRectangle(b, 14, 38, 68, 20);
                g.FillRectangle(b, 38, 38, 20, 44);
            }
            using (var p = new Pen(MeBlack, 3f))
            {
                g.DrawLine(p, 14, 38, 38, 38);
                g.DrawLine(p, 58, 38, 82, 38);
                g.DrawLine(p, 82, 38, 82, 58);
                g.DrawLine(p, 82, 58, 58, 58);
                g.DrawLine(p, 58, 58, 58, 82);
                g.DrawLine(p, 58, 82, 38, 82);
                g.DrawLine(p, 38, 82, 38, 58);
                g.DrawLine(p, 38, 58, 14, 58);
                g.DrawLine(p, 14, 58, 14, 38);
            }

            // Outward sprout arrows
            Action<PointF, PointF, int> drawArrow = delegate(PointF p1, PointF p2, int dir) {
                using (var pY = new Pen(MeYellow, 5f)) g.DrawLine(pY, p1, p2);
                using (var pB = new Pen(MeBlack, 2f)) g.DrawLine(pB, p1, p2);
            };

            drawArrow(new PointF(14, 48), new PointF(4, 48), 0);
            drawArrow(new PointF(82, 48), new PointF(92, 48), 1);
            drawArrow(new PointF(48, 82), new PointF(48, 92), 2);

            // Center fitting snap
            using (var b = new SolidBrush(MeBlack)) g.FillEllipse(b, 44, 44, 8, 8);

            bmp.Save(blPath, ImageFormat.Png);
        }
    }

    // ==========================================
    // MASTER SHOWCASE BOARD
    // ==========================================
    static void RenderMasterShowcaseBoard(string assetsDir, string boardPath)
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
            using (var bYellow = new SolidBrush(MeYellow))
            using (var bGray = new SolidBrush(Color.FromArgb(255, 139, 148, 158)))
            {
                g.DrawString("mep", fBold, bWhite, 36, 18);
                float mepW = g.MeasureString("mep", fBold).Width;
                g.DrawString("OS", fBold, bYellow, 36 + mepW - 10, 18);

                g.DrawString("// BẢNG SO SÁNH TRỰC QUAN 4 CONCEPT ICON CHO REVIT RIBBON", fSub, bGray, 38, 58);
            }

            // 4 Concept Columns
            string[] titles = {
                "CONCEPT 1: Technical MEP (Đề xuất)",
                "CONCEPT 2: Isometric 3D Solid",
                "CONCEPT 3: Swiss Minimalist Glyph",
                "CONCEPT 4: CAD Blueprint & Snap"
            };

            string[] descriptions = {
                "Ống lệch trục + cầu nối vàng #FFDD00 + snap node.\nPhụ kiện bung tia ống & hướng dòng chảy.",
                "Hình học 3D Isometric 30° có đổ bóng sáng/tối.\nPhù hợp môi trường mô hình không gian BIM.",
                "Đồ họa phẳng phong cách Thụy Sĩ, nét to bản.\nTương phản cực cao, rõ nét ở kích thước nhỏ.",
                "Mô phỏng bản vẽ CAD 2D có đường tâm nét đứt.\nKèm tâm ngắm crosshair bắt điểm chính xác."
            };

            string[] prefixes = { "c1", "c2", "c3", "c4" };

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
                using (var p = new Pen(i == 0 ? MeYellow : Color.FromArgb(255, 48, 54, 61), i == 0 ? 2f : 1f))
                {
                    g.FillRectangle(b, x, y, colW, 810);
                    g.DrawRectangle(p, x, y, colW, 810);
                }

                // Card Header
                using (var bH = new SolidBrush(Color.FromArgb(255, 30, 36, 46))) g.FillRectangle(bH, x, y, colW, 46);
                using (var fT = new Font("Arial", 11, FontStyle.Bold))
                using (var bT = new SolidBrush(i == 0 ? MeYellow : Color.White))
                {
                    g.DrawString(titles[i], fT, bT, new RectangleF(x + 10, y + 6, colW - 20, 36));
                }

                // Description
                using (var fD = new Font("Arial", 9.5f))
                using (var bD = new SolidBrush(Color.FromArgb(255, 160, 170, 185)))
                {
                    g.DrawString(descriptions[i], fD, bD, new RectangleF(x + 12, y + 54, colW - 24, 60));
                }

                // --- 1. Large Preview Section (96x96) ---
                using (var fSec = new Font("Arial", 9, FontStyle.Bold))
                using (var bSec = new SolidBrush(Color.FromArgb(255, 110, 120, 135)))
                {
                    g.DrawString("CHI TIẾT HIỂN THỊ (96×96)", fSec, bSec, x + 14, y + 120);
                }

                string imgCtPath = Path.Combine(assetsDir, prefixes[i] + "_ct.png");
                string imgBlPath = Path.Combine(assetsDir, prefixes[i] + "_bl.png");

                // Icon Box CT
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

                // Labels under big icons
                using (var fLbl = new Font("Arial", 9.5f, FontStyle.Bold))
                using (var bLbl = new SolidBrush(Color.White))
                using (var bCmd = new SolidBrush(MeYellow))
                {
                    g.DrawString("Connect To", fLbl, bLbl, x + 28, y + 256);
                    g.DrawString("[CT]", fLbl, bCmd, x + 54, y + 274);

                    g.DrawString("Bloom", fLbl, bLbl, x + 172, y + 256);
                    g.DrawString("[BL]", fLbl, bCmd, x + 178, y + 274);
                }

                // --- 2. Revit Ribbon Simulation (Light Mode) ---
                using (var fSec = new Font("Arial", 9, FontStyle.Bold))
                using (var bSec = new SolidBrush(Color.FromArgb(255, 110, 120, 135)))
                {
                    g.DrawString("REVIT RIBBON LIGHT (32×32)", fSec, bSec, x + 14, y + 310);
                }

                DrawRevitRibbonSimulation(g, x + 14, y + 332, colW - 28, imgCtPath, imgBlPath, false);

                // --- 3. Revit Ribbon Simulation (Dark Mode) ---
                using (var fSec = new Font("Arial", 9, FontStyle.Bold))
                using (var bSec = new SolidBrush(Color.FromArgb(255, 110, 120, 135)))
                {
                    g.DrawString("REVIT RIBBON DARK (32×32)", fSec, bSec, x + 14, y + 550);
                }

                DrawRevitRibbonSimulation(g, x + 14, y + 572, colW - 28, imgCtPath, imgBlPath, true);
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

        // Ribbon container
        using (var b = new SolidBrush(bgBar))
        using (var p = new Pen(borderBar))
        {
            g.FillRectangle(b, x, y, w, h);
            g.DrawRectangle(p, x, y, w, h);
        }

        // Panel Modeling
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

        // Buttons
        int btnW = (pW - 16) / 2;
        int btnH = pH - 28;

        // Button 1: CT
        DrawRevitButton(g, pX + 6, pY + 6, btnW, btnH, ctPath, "Connect To", textPrimary, isDark);

        // Button 2: BL
        DrawRevitButton(g, pX + 10 + btnW, pY + 6, btnW, btnH, blPath, "Bloom", textPrimary, isDark);

        // Panel Title Bar at bottom
        using (var fPanel = new Font("Segoe UI", 8.5f, FontStyle.Regular))
        using (var bText = new SolidBrush(textMuted))
        {
            var sz = g.MeasureString("Modeling", fPanel);
            g.DrawString("Modeling", fPanel, bText, pX + (pW - sz.Width) / 2f, pY + pH - 20);
        }
    }

    static void DrawRevitButton(Graphics g, int bx, int by, int bw, int bh, string iconPath, string text, Color textColor, bool isDark)
    {
        // Hover border subtle
        Color btnBorder = isDark ? Color.FromArgb(255, 60, 60, 65) : Color.FromArgb(255, 225, 225, 225);
        using (var p = new Pen(btnBorder)) g.DrawRectangle(p, bx, by, bw, bh);

        // 32x32 Icon centered
        int iconX = bx + (bw - 32) / 2;
        int iconY = by + 14;

        if (File.Exists(iconPath))
        {
            using (var img = Image.FromFile(iconPath))
            {
                g.DrawImage(img, iconX, iconY, 32, 32);
            }
        }

        // Button Text
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
