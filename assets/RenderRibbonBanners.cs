using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;

namespace RibbonBannerRenderer
{
    class Program
    {
        static void Main(string[] args)
        {
            string fontPath = @"D:\OneDrive\@ Personal Project\mepOS\assets\fonts\SpaceGrotesk-Bold.ttf";
            string outDir = @"D:\OneDrive\@ Personal Project\mepOS\assets\official_icons";
            string resDir = @"D:\OneDrive\@ Personal Project\mepOS\src\mepOS.Loader\Resources";
            string binResDir = @"D:\OneDrive\@ Personal Project\mepOS\bin\Debug\Resources";
            string releaseResDir = @"D:\OneDrive\@ Personal Project\mepOS\bin\Release\Resources";
            string artifactDir = @"C:\Users\2412n\.gemini\antigravity\brain\60591867-667c-4678-8243-090a84a63b0c\banner_concepts";

            Directory.CreateDirectory(outDir);
            Directory.CreateDirectory(resDir);
            Directory.CreateDirectory(binResDir);
            Directory.CreateDirectory(releaseResDir);
            Directory.CreateDirectory(artifactDir);

            using (var pfc = new PrivateFontCollection())
            {
                pfc.AddFontFile(fontPath);
                FontFamily family = pfc.Families[0];

                // Render Concept 1: DSCons Signature Style (Text Left: ACTIVE / @MEPOS + Icon Right)
                RenderDsconsStyleBanner(family, "banner_concept1_dscons_style.png", false, outDir, artifactDir);
                RenderDsconsStyleBanner(family, "banner_concept1_dscons_style_dark.png", true, outDir, artifactDir);

                // Render Concept 2: Pure Master Logo Typography (mepOS + v1.0.1 badge)
                RenderMasterLogoBanner(family, "banner_concept2_master_logo.png", false, outDir, artifactDir);
                RenderMasterLogoBanner(family, "banner_concept2_master_logo_dark.png", true, outDir, artifactDir);

                // Render Concept 3: Compact Architectural Tile + mepOS Text
                RenderSwissArchitecturalBanner(family, "banner_concept3_swiss_badge.png", false, outDir, artifactDir);
                RenderSwissArchitecturalBanner(family, "banner_concept3_swiss_badge_dark.png", true, outDir, artifactDir);

                // Deploy Concept 2 (Right-aligned mepOS master typography) as official logo_banner.png
                DeployBanner("banner_concept2_master_logo.png", outDir, resDir, binResDir, releaseResDir);
            }

            Console.WriteLine("RIBBON BANNERS GENERATED SUCCESSFULLY!");
        }

        static void DeployBanner(string fileName, string srcDir, string resDir, string binResDir, string releaseResDir)
        {
            string src = Path.Combine(srcDir, fileName);
            File.Copy(src, Path.Combine(resDir, "logo_banner.png"), true);
            File.Copy(src, Path.Combine(binResDir, "logo_banner.png"), true);
            File.Copy(src, Path.Combine(releaseResDir, "logo_banner.png"), true);
        }

        static void ConfigureGraphics(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        }

        /// <summary>
        /// Concept 1: DSCons Signature Style
        /// Left: Two lines "ACTIVE" / "@mepOS" (or "SUITE" / "@mepOS")
        /// Right: Circular or Swiss raised brand icon
        /// Target dimensions: 204 x 53 (scaled @2x: 408 x 106)
        /// </summary>
        static void RenderDsconsStyleBanner(FontFamily family, string fileName, bool isDark, string dir1, string dir2)
        {
            int w = 408;
            int h = 106;

            using (var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb))
            {
                bmp.SetResolution(192, 192);
                using (var g = Graphics.FromImage(bmp))
                {
                    ConfigureGraphics(g);
                    g.Clear(Color.Transparent);

                    Color primaryTextColor = isDark ? Color.FromArgb(255, 255, 255, 255) : Color.FromArgb(255, 10, 10, 10);
                    Color accentYellow = Color.FromArgb(255, 255, 221, 0);
                    Color activeGreen = Color.FromArgb(255, 34, 197, 94); // #22C55E

                    // 1. Text Left: Two lines
                    // Line 1: "ACTIVE" or "SUITE" (with a clean architectural green dot or bold title)
                    float line1FontSize = 26f;
                    float line2FontSize = 24f;

                    // Draw green active dot
                    float dotX = 14;
                    float dotY = 24;
                    float dotR = 10;
                    using (var dotBrush = new SolidBrush(activeGreen))
                    using (var dotBorder = new Pen(Color.FromArgb(255, 10, 10, 10), 2.5f))
                    {
                        g.FillEllipse(dotBrush, dotX, dotY, dotR, dotR);
                        g.DrawEllipse(dotBorder, dotX, dotY, dotR, dotR);
                    }

                    // Line 1 Text: "ACTIVE"
                    using (GraphicsPath p1 = new GraphicsPath())
                    {
                        p1.AddString("ACTIVE", family, (int)FontStyle.Bold, line1FontSize, new PointF(dotX + 16, 14), StringFormat.GenericTypographic);
                        using (var brush = new SolidBrush(primaryTextColor))
                        {
                            g.FillPath(brush, p1);
                        }
                    }

                    // Line 2 Text: "@mepOS" (or "@MEPOS")
                    using (GraphicsPath p2 = new GraphicsPath())
                    {
                        p2.AddString("@mepOS", family, (int)FontStyle.Bold, line2FontSize, new PointF(dotX, 52), StringFormat.GenericTypographic);
                        
                        // Give "@" subtle grey, "mep" primary, "OS" yellow
                        using (var brush = new SolidBrush(Color.FromArgb(255, 113, 113, 122)))
                        {
                            g.FillPath(brush, p2);
                        }
                    }

                    // 2. Right Side: The mepOS Badge (Circular / Swiss Raised Shield with "OS")
                    float iconSize = 88;
                    float iconX = w - iconSize - 12;
                    float iconY = (h - iconSize) / 2.0f;

                    // Solid hard shadow (DSCons / Swiss standard)
                    float shadowOffset = 4;
                    using (var sBrush = new SolidBrush(Color.FromArgb(255, 10, 10, 10)))
                    {
                        g.FillEllipse(sBrush, iconX + shadowOffset, iconY + shadowOffset, iconSize, iconSize);
                    }

                    // Circle base plate
                    Color circleBg = isDark ? Color.FromArgb(255, 24, 24, 27) : Color.FromArgb(255, 255, 255, 255);
                    using (var cBrush = new SolidBrush(circleBg))
                    using (var cBorder = new Pen(Color.FromArgb(255, 10, 10, 10), 3.5f))
                    {
                        g.FillEllipse(cBrush, iconX, iconY, iconSize, iconSize);
                        g.DrawEllipse(cBorder, iconX, iconY, iconSize, iconSize);
                    }

                    // Arc accent: Top yellow crest
                    using (var crestPen = new Pen(accentYellow, 7f))
                    {
                        g.DrawArc(crestPen, iconX + 4, iconY + 4, iconSize - 8, iconSize - 8, 200, 140);
                    }

                    // Monogram "OS" inside circle
                    float osFontSize = 40f;
                    using (GraphicsPath pOS = new GraphicsPath())
                    {
                        pOS.AddString("OS", family, (int)FontStyle.Bold, osFontSize, new PointF(iconX + 16, iconY + 16), StringFormat.GenericTypographic);

                        // Stroke first (paint-order: stroke fill)
                        using (var penOS = new Pen(Color.FromArgb(255, 10, 10, 10), 6f))
                        {
                            penOS.LineJoin = LineJoin.Round;
                            g.DrawPath(penOS, pOS);
                        }
                        // Yellow fill
                        using (var brushOS = new SolidBrush(accentYellow))
                        {
                            g.FillPath(brushOS, pOS);
                        }
                    }
                }

                bmp.Save(Path.Combine(dir1, fileName), ImageFormat.Png);
                bmp.Save(Path.Combine(dir2, fileName), ImageFormat.Png);
            }
        }

        /// <summary>
        /// Concept 2: Pure Horizontal Master Logo (Far Right Aligned, No Version Badge)
        /// "mep" in Black/White + "OS" in Yellow with #0A0A0A stroke
        /// Aligned to the far right with 16px right margin, matching user preference.
        /// </summary>
        static void RenderMasterLogoBanner(FontFamily family, string fileName, bool isDark, string dir1, string dir2)
        {
            int w = 408;
            int h = 106;

            using (var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb))
            {
                bmp.SetResolution(192, 192);
                using (var g = Graphics.FromImage(bmp))
                {
                    ConfigureGraphics(g);
                    g.Clear(Color.Transparent);

                    Color prefixColor = isDark ? Color.FromArgb(255, 255, 255, 255) : Color.FromArgb(255, 10, 10, 10);
                    Color coreYellow = Color.FromArgb(255, 255, 221, 0);
                    Color strokeColor = Color.FromArgb(255, 10, 10, 10);

                    float fontSize = 76f;
                    float strokeWidth = 7.5f;

                    // 1. Measure "mep"
                    float mepW = 0;
                    using (GraphicsPath pMepTest = new GraphicsPath())
                    {
                        pMepTest.AddString("mep", family, (int)FontStyle.Bold, fontSize, new PointF(0, 0), StringFormat.GenericTypographic);
                        mepW = pMepTest.GetBounds().Width;
                    }

                    // 2. Measure "OS"
                    float osW = 0;
                    using (GraphicsPath pOSTest = new GraphicsPath())
                    {
                        pOSTest.AddString("OS", family, (int)FontStyle.Bold, fontSize, new PointF(0, 0), StringFormat.GenericTypographic);
                        osW = pOSTest.GetBounds().Width;
                    }

                    // Letter spacing: -0.03em
                    float kerning = 4f;
                    float totalW = mepW + osW - kerning + (strokeWidth / 2f);

                    // 3. Right-align logo with 16px margin from right edge
                    float rightMargin = 16f;
                    float startX = w - rightMargin - totalW;
                    float baselineY = 78f;
                    float osX = startX + mepW - kerning;

                    // 4. Draw "mep"
                    using (GraphicsPath pathMep = new GraphicsPath())
                    {
                        pathMep.AddString("mep", family, (int)FontStyle.Bold, fontSize, new PointF(startX, baselineY - fontSize * 0.95f), StringFormat.GenericTypographic);
                        using (var brush = new SolidBrush(prefixColor))
                        {
                            g.FillPath(brush, pathMep);
                        }
                    }

                    // 5. Draw "OS" with paint-order: stroke fill
                    using (GraphicsPath pathOS = new GraphicsPath())
                    {
                        pathOS.AddString("OS", family, (int)FontStyle.Bold, fontSize, new PointF(osX, baselineY - fontSize * 0.95f), StringFormat.GenericTypographic);

                        using (var pen = new Pen(strokeColor, strokeWidth))
                        {
                            pen.LineJoin = LineJoin.Round;
                            g.DrawPath(pen, pathOS);
                        }

                        using (var brush = new SolidBrush(coreYellow))
                        {
                            g.FillPath(brush, pathOS);
                        }
                    }
                }

                bmp.Save(Path.Combine(dir1, fileName), ImageFormat.Png);
                bmp.Save(Path.Combine(dir2, fileName), ImageFormat.Png);
            }
        }

        /// <summary>
        /// Concept 3: Swiss Architectural Banner (Raised Card Tile Left + mepOS Text Right)
        /// </summary>
        static void RenderSwissArchitecturalBanner(FontFamily family, string fileName, bool isDark, string dir1, string dir2)
        {
            int w = 408;
            int h = 106;

            using (var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb))
            {
                bmp.SetResolution(192, 192);
                using (var g = Graphics.FromImage(bmp))
                {
                    ConfigureGraphics(g);
                    g.Clear(Color.Transparent);

                    Color primaryText = isDark ? Color.FromArgb(255, 255, 255, 255) : Color.FromArgb(255, 10, 10, 10);
                    Color accentYellow = Color.FromArgb(255, 255, 221, 0);

                    // Left: Small Swiss Card Tile (64x64)
                    float cardX = 14;
                    float cardY = 16;
                    float cardS = 74;
                    float shadow = 4;

                    using (var shBrush = new SolidBrush(Color.FromArgb(255, 10, 10, 10)))
                    {
                        FillRoundRect(g, shBrush, cardX + shadow, cardY + shadow, cardS, cardS, 10);
                    }

                    Color cardBg = isDark ? Color.FromArgb(255, 24, 24, 27) : Color.FromArgb(255, 255, 255, 255);
                    using (var cBrush = new SolidBrush(cardBg))
                    using (var cPen = new Pen(Color.FromArgb(255, 10, 10, 10), 3f))
                    {
                        FillRoundRect(g, cBrush, cardX, cardY, cardS, cardS, 10);
                        DrawRoundRect(g, cPen, cardX, cardY, cardS, cardS, 10);
                    }

                    // Yellow top accent
                    using (var yBrush = new SolidBrush(accentYellow))
                    using (var cPen = new Pen(Color.FromArgb(255, 10, 10, 10), 2.5f))
                    {
                        GraphicsPath tb = GetTopBandPath(cardX, cardY, cardS, 12, 10);
                        g.FillPath(yBrush, tb);
                        g.DrawPath(cPen, tb);
                    }

                    // Monogram "OS" inside tile
                    using (GraphicsPath pOS = new GraphicsPath())
                    {
                        pOS.AddString("OS", family, (int)FontStyle.Bold, 38f, new PointF(cardX + 11, cardY + 16), StringFormat.GenericTypographic);
                        using (var penOS = new Pen(Color.FromArgb(255, 10, 10, 10), 5.5f))
                        {
                            penOS.LineJoin = LineJoin.Round;
                            g.DrawPath(penOS, pOS);
                        }
                        using (var brushOS = new SolidBrush(accentYellow))
                        {
                            g.FillPath(brushOS, pOS);
                        }
                    }

                    // Right: "mepOS" + "SWISS ARCHITECTURE"
                    float textX = cardX + cardS + 20;
                    using (GraphicsPath pTitle = new GraphicsPath())
                    {
                        pTitle.AddString("mepOS", family, (int)FontStyle.Bold, 42f, new PointF(textX, 16), StringFormat.GenericTypographic);
                        using (var brush = new SolidBrush(primaryText))
                        {
                            g.FillPath(brush, pTitle);
                        }
                    }

                    using (GraphicsPath pSub = new GraphicsPath())
                    {
                        pSub.AddString("SWISS MEP SUITE", family, (int)FontStyle.Bold, 15f, new PointF(textX + 2, 64), StringFormat.GenericTypographic);
                        using (var brush = new SolidBrush(Color.FromArgb(255, 113, 113, 122)))
                        {
                            g.FillPath(brush, pSub);
                        }
                    }
                }

                bmp.Save(Path.Combine(dir1, fileName), ImageFormat.Png);
                bmp.Save(Path.Combine(dir2, fileName), ImageFormat.Png);
            }
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
