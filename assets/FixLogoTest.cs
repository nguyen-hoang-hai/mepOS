using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;

class FixLogoTest
{
    static void Main()
    {
        string fontPath = @"D:\OneDrive\@ Personal Project\mepOS\assets\fonts\SpaceGrotesk-Bold.ttf";
        string outPath = @"C:\Users\2412n\.gemini\antigravity\brain\60591867-667c-4678-8243-090a84a63b0c\test_fixed_logo.png";

        using (var pfc = new PrivateFontCollection())
        {
            pfc.AddFontFile(fontPath);
            FontFamily family = pfc.Families[0];

            float fontSize = 72f;
            float strokeWidth = 7f;
            float gap = 6f; // Clean separation between 'p' and 'O'

            // Measure "mep"
            RectangleF bMep;
            using (var testMep = new GraphicsPath())
            {
                testMep.AddString("mep", family, (int)FontStyle.Bold, fontSize, new PointF(0, 0), StringFormat.GenericTypographic);
                bMep = testMep.GetBounds();
            }

            // Measure "OS"
            RectangleF bOS;
            using (var testOS = new GraphicsPath())
            {
                testOS.AddString("OS", family, (int)FontStyle.Bold, fontSize, new PointF(0, 0), StringFormat.GenericTypographic);
                bOS = testOS.GetBounds();
            }

            float padX = strokeWidth + 4f;
            float padY = strokeWidth + 4f;

            float mepDrawX = padX - bMep.Left;
            float osDrawX = padX + bMep.Width + gap + (strokeWidth / 2f) - bOS.Left;

            float minY = Math.Min(bMep.Top, bOS.Top);
            float maxY = Math.Max(bMep.Bottom, bOS.Bottom);
            float drawY = padY - minY;

            int bmpW = (int)Math.Ceiling(padX + bMep.Width + gap + strokeWidth + bOS.Width + padX);
            int bmpH = (int)Math.Ceiling((maxY - minY) + padY * 2);

            // 1. Dark Theme Master Logo (mep White, OS Yellow + Black Stroke)
            using (var bmpDark = new Bitmap(bmpW, bmpH, PixelFormat.Format32bppArgb))
            {
                using (var g = Graphics.FromImage(bmpDark))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                    g.Clear(Color.Transparent);

                    // Draw "mep"
                    using (var pMep = new GraphicsPath())
                    {
                        pMep.AddString("mep", family, (int)FontStyle.Bold, fontSize, new PointF(mepDrawX, drawY), StringFormat.GenericTypographic);
                        using (var b = new SolidBrush(Color.White)) g.FillPath(b, pMep);
                    }

                    // Draw "OS"
                    using (var pOS = new GraphicsPath())
                    {
                        pOS.AddString("OS", family, (int)FontStyle.Bold, fontSize, new PointF(osDrawX, drawY), StringFormat.GenericTypographic);
                        using (var p = new Pen(Color.FromArgb(255, 10, 10, 10), strokeWidth) { LineJoin = LineJoin.Round }) g.DrawPath(p, pOS);
                        using (var b = new SolidBrush(Color.FromArgb(255, 255, 221, 0))) g.FillPath(b, pOS);
                    }
                }

                string darkPath1 = @"D:\OneDrive\@ Personal Project\mepOS\src\mepOS.Loader\Resources\logo_banner_dark.png";
                string darkPath2 = @"D:\OneDrive\@ Personal Project\mepOS\bin\Release\Resources\logo_banner_dark.png";
                string darkPath3 = @"D:\OneDrive\@ Personal Project\mepOS\assets\official_icons\banner_concept2_master_logo_dark.png";
                bmpDark.Save(darkPath1, ImageFormat.Png);
                bmpDark.Save(darkPath2, ImageFormat.Png);
                bmpDark.Save(darkPath3, ImageFormat.Png);
                bmpDark.Save(outPath, ImageFormat.Png);
            }

            // 2. Light Theme Master Logo (mep Black, OS Yellow + Black Stroke)
            using (var bmpLight = new Bitmap(bmpW, bmpH, PixelFormat.Format32bppArgb))
            {
                using (var g = Graphics.FromImage(bmpLight))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                    g.Clear(Color.Transparent);

                    using (var pMep = new GraphicsPath())
                    {
                        pMep.AddString("mep", family, (int)FontStyle.Bold, fontSize, new PointF(mepDrawX, drawY), StringFormat.GenericTypographic);
                        using (var b = new SolidBrush(Color.FromArgb(255, 10, 10, 10))) g.FillPath(b, pMep);
                    }

                    using (var pOS = new GraphicsPath())
                    {
                        pOS.AddString("OS", family, (int)FontStyle.Bold, fontSize, new PointF(osDrawX, drawY), StringFormat.GenericTypographic);
                        using (var p = new Pen(Color.FromArgb(255, 10, 10, 10), strokeWidth) { LineJoin = LineJoin.Round }) g.DrawPath(p, pOS);
                        using (var b = new SolidBrush(Color.FromArgb(255, 255, 221, 0))) g.FillPath(b, pOS);
                    }
                }

                string lightPath1 = @"D:\OneDrive\@ Personal Project\mepOS\src\mepOS.Loader\Resources\logo_banner.png";
                string lightPath2 = @"D:\OneDrive\@ Personal Project\mepOS\bin\Release\Resources\logo_banner.png";
                string lightPath3 = @"D:\OneDrive\@ Personal Project\mepOS\assets\official_icons\banner_concept2_master_logo.png";
                bmpLight.Save(lightPath1, ImageFormat.Png);
                bmpLight.Save(lightPath2, ImageFormat.Png);
                bmpLight.Save(lightPath3, ImageFormat.Png);
            }
        }

        Console.WriteLine("Logo test generated!");
    }
}
