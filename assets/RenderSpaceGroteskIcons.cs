using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;

namespace SpaceGroteskIconRenderer
{
    class Program
    {
        static void Main(string[] args)
        {
            string fontPath = @"D:\OneDrive\@ Personal Project\mepOS\assets\fonts\SpaceGrotesk-Bold.ttf";
            string outDir = @"D:\OneDrive\@ Personal Project\mepOS\assets\official_icons";
            string resDir = @"D:\OneDrive\@ Personal Project\mepOS\src\mepOS.Loader\Resources";
            string binResDir = @"D:\OneDrive\@ Personal Project\mepOS\bin\Debug\Resources";
            string artifactDir = @"C:\Users\2412n\.gemini\antigravity\brain\60591867-667c-4678-8243-090a84a63b0c\official_icons";

            Directory.CreateDirectory(outDir);
            Directory.CreateDirectory(resDir);
            Directory.CreateDirectory(binResDir);
            Directory.CreateDirectory(artifactDir);

            using (var pfc = new PrivateFontCollection())
            {
                pfc.AddFontFile(fontPath);
                FontFamily family = pfc.Families[0];
                Console.WriteLine("Loaded font: " + family.Name);

                // Render CT (Connect To)
                RenderIcon(family, "CT_Light", "C", "T", false, outDir, artifactDir);
                RenderIcon(family, "CT_Dark", "C", "T", true, outDir, artifactDir);
                DeployToResources("CT_Light", "CT", outDir, resDir, binResDir);

                // Render BL (Bloom)
                RenderIcon(family, "BL_Light", "B", "L", false, outDir, artifactDir);
                RenderIcon(family, "BL_Dark", "B", "L", true, outDir, artifactDir);
                DeployToResources("BL_Light", "BL", outDir, resDir, binResDir);

                // Also render Master Logo Banner (mepOS) using this exact font!
                RenderMasterLogo(family, Path.Combine(outDir, "mepOS_logo_master.png"));
                RenderMasterLogo(family, Path.Combine(artifactDir, "mepOS_logo_master.png"));
            }

            Console.WriteLine("ALL OFFICIAL ICONS & MASTER LOGO GENERATED SUCCESSFULLY!");
        }

        static void DeployToResources(string prefix, string toolCode, string srcDir, string resDir, string binResDir)
        {
            File.Copy(Path.Combine(srcDir, prefix + "_256.png"), Path.Combine(resDir, toolCode + "_256.png"), true);
            File.Copy(Path.Combine(srcDir, prefix + "_32.png"), Path.Combine(resDir, toolCode + "_32.png"), true);
            File.Copy(Path.Combine(srcDir, prefix + "_16.png"), Path.Combine(resDir, toolCode + "_16.png"), true);

            File.Copy(Path.Combine(srcDir, prefix + "_32.png"), Path.Combine(binResDir, toolCode + "_32.png"), true);
            File.Copy(Path.Combine(srcDir, prefix + "_16.png"), Path.Combine(binResDir, toolCode + "_16.png"), true);
        }

        static void RenderIcon(FontFamily family, string name, string letter1, string letter2, bool isDarkTile, string dir1, string dir2)
        {
            int[] sizes = new int[] { 512, 256, 96, 32, 16 };
            
            // Draw 512 master first
            using (var bmp512 = new Bitmap(512, 512, PixelFormat.Format32bppArgb))
            {
                using (var g = Graphics.FromImage(bmp512))
                {
                    ConfigureGraphics(g);
                    DrawTile(g, 512, family, letter1, letter2, isDarkTile);
                }

                foreach (int s in sizes)
                {
                    using (var bmpResized = new Bitmap(s, s, PixelFormat.Format32bppArgb))
                    {
                        using (var gr = Graphics.FromImage(bmpResized))
                        {
                            gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            gr.SmoothingMode = SmoothingMode.HighQuality;
                            gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
                            gr.DrawImage(bmp512, 0, 0, s, s);
                        }

                        string fn = string.Format("{0}_{1}.png", name, s);
                        bmpResized.Save(Path.Combine(dir1, fn), ImageFormat.Png);
                        bmpResized.Save(Path.Combine(dir2, fn), ImageFormat.Png);
                    }
                }
            }
        }

        static void DrawTile(Graphics g, int s, FontFamily family, string l1, string l2, bool isDarkTile)
        {
            float scale = s / 256.0f;
            g.Clear(Color.Transparent);

            float pad = 18 * scale;
            float r = 28 * scale;
            float tw = s - 2 * pad;
            float th = s - 2 * pad;

            // 1. Solid Architectural Hard Shadow (2.5px / 6px scaled)
            float shadowOffset = 8 * scale;
            using (var shadowBrush = new SolidBrush(Color.FromArgb(255, 10, 10, 10)))
            {
                FillRoundRect(g, shadowBrush, pad + shadowOffset, pad + shadowOffset, tw, th, r);
            }

            // 2. Base Plate
            Color baseColor = isDarkTile ? Color.FromArgb(255, 24, 24, 27) : Color.FromArgb(255, 255, 255, 255);
            Color borderColor = Color.FromArgb(255, 10, 10, 10);
            float borderWidth = 5 * scale;

            using (var baseBrush = new SolidBrush(baseColor))
            using (var borderPen = new Pen(borderColor, borderWidth))
            {
                FillRoundRect(g, baseBrush, pad, pad, tw, th, r);
                DrawRoundRect(g, borderPen, pad, pad, tw, th, r);
            }

            // 3. Top yellow accent strip (mepOS signature brand element)
            float accentH = 22 * scale;
            using (var accentBrush = new SolidBrush(Color.FromArgb(255, 255, 221, 0)))
            using (var borderPen = new Pen(borderColor, 3.5f * scale))
            {
                GraphicsPath topBand = GetTopBandPath(pad, pad, tw, accentH, r);
                g.FillPath(accentBrush, topBand);
                g.DrawPath(borderPen, topBand);
            }

            // 4. Render Typography: letter1 and letter2 in Space Grotesk Bold 700
            float fontSize = 126 * scale;
            int fontStyle = (int)FontStyle.Bold;

            Color prefixColor = isDarkTile ? Color.FromArgb(255, 255, 255, 255) : Color.FromArgb(255, 10, 10, 10);
            Color coreYellow = Color.FromArgb(255, 255, 221, 0);
            Color strokeColor = Color.FromArgb(255, 10, 10, 10);
            float strokeWidth = 8.5f * scale; // Corresponds to 2.5px at display resolution

            float baselineY = pad + th * 0.72f;

            // Measure both paths at (0,0) to get exact dimensions and kerning
            using (GraphicsPath testP1 = new GraphicsPath())
            using (GraphicsPath testP2 = new GraphicsPath())
            {
                testP1.AddString(l1, family, fontStyle, fontSize, new PointF(0, 0), StringFormat.GenericTypographic);
                testP2.AddString(l2, family, fontStyle, fontSize, new PointF(0, 0), StringFormat.GenericTypographic);

                RectangleF b1 = testP1.GetBounds();
                RectangleF b2 = testP2.GetBounds();

                float gap = 6 * scale;
                float totalW = b1.Width + gap + b2.Width;
                float startX = pad + (tw - totalW) / 2.0f;
                float tX = startX + b1.Width + gap;

                // Adjust startX if bounds had an offset
                startX -= b1.Left;
                tX -= b2.Left;

                // Draw letter 1 (First letter: prefixColor)
                using (GraphicsPath path1 = new GraphicsPath())
                {
                    path1.AddString(l1, family, fontStyle, fontSize, new PointF(startX, baselineY - fontSize * 0.95f), StringFormat.GenericTypographic);
                    using (var brush1 = new SolidBrush(prefixColor))
                    {
                        g.FillPath(brush1, path1);
                    }
                }

                // Draw letter 2 with exact paint-order: stroke fill
                using (GraphicsPath path2 = new GraphicsPath())
                {
                    path2.AddString(l2, family, fontStyle, fontSize, new PointF(tX, baselineY - fontSize * 0.95f), StringFormat.GenericTypographic);

                    // Step 1: Draw stroke first (behind fill)
                    using (var pen2 = new Pen(strokeColor, strokeWidth))
                    {
                        pen2.LineJoin = LineJoin.Round;
                        g.DrawPath(pen2, path2);
                    }

                    // Step 2: Fill yellow on top
                    using (var brush2 = new SolidBrush(coreYellow))
                    {
                        g.FillPath(brush2, path2);
                    }
                }
            }
        }

        static void RenderMasterLogo(FontFamily family, string outPath)
        {
            int w = 720;
            int h = 240;
            using (var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    ConfigureGraphics(g);
                    g.Clear(Color.Transparent);

                    float fontSize = 110;
                    float baselineY = 160;
                    float startX = 40;

                    // 1. Draw "mep"
                    using (GraphicsPath pathMep = new GraphicsPath())
                    {
                        pathMep.AddString("mep", family, (int)FontStyle.Bold, fontSize, new PointF(startX, baselineY - fontSize * 0.95f), StringFormat.GenericTypographic);
                        using (var brush = new SolidBrush(Color.FromArgb(255, 10, 10, 10)))
                        {
                            g.FillPath(brush, pathMep);
                        }
                    }

                    // 2. Measure "mep"
                    float mepWidth = 245;
                    float osX = startX + mepWidth - 6;

                    // 3. Draw "OS" with paint-order: stroke fill
                    using (GraphicsPath pathOS = new GraphicsPath())
                    {
                        pathOS.AddString("OS", family, (int)FontStyle.Bold, fontSize, new PointF(osX, baselineY - fontSize * 0.95f), StringFormat.GenericTypographic);

                        // Stroke first
                        using (var pen = new Pen(Color.FromArgb(255, 10, 10, 10), 10.0f))
                        {
                            pen.LineJoin = LineJoin.Round;
                            g.DrawPath(pen, pathOS);
                        }

                        // Fill yellow on top
                        using (var brush = new SolidBrush(Color.FromArgb(255, 255, 221, 0)))
                        {
                            g.FillPath(brush, pathOS);
                        }
                    }
                }

                bmp.Save(outPath, ImageFormat.Png);
            }
        }

        static void ConfigureGraphics(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        }

        static void FillRoundRect(Graphics g, Brush b, float x, float y, float w, float h, float r)
        {
            using (GraphicsPath gp = GetRoundRectPath(x, y, w, h, r))
            {
                g.FillPath(b, gp);
            }
        }

        static void DrawRoundRect(Graphics g, Pen p, float x, float y, float w, float h, float r)
        {
            using (GraphicsPath gp = GetRoundRectPath(x, y, w, h, r))
            {
                g.DrawPath(p, gp);
            }
        }

        static GraphicsPath GetRoundRectPath(float x, float y, float w, float h, float r)
        {
            GraphicsPath gp = new GraphicsPath();
            float d = r * 2;
            gp.AddArc(x, y, d, d, 180, 90);
            gp.AddArc(x + w - d, y, d, d, 270, 90);
            gp.AddArc(x + w - d, y + h - d, d, d, 0, 90);
            gp.AddArc(x, y + h - d, d, d, 90, 90);
            gp.CloseFigure();
            return gp;
        }

        static GraphicsPath GetTopBandPath(float x, float y, float w, float bh, float r)
        {
            GraphicsPath gp = new GraphicsPath();
            float d = r * 2;
            gp.AddArc(x, y, d, d, 180, 90);
            gp.AddArc(x + w - d, y, d, d, 270, 90);
            gp.AddLine(x + w, y + bh, x, y + bh);
            gp.CloseFigure();
            return gp;
        }
    }
}
