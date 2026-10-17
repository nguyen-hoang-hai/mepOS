using System;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;

namespace mepOS.Loader
{
    public class RibbonApp : IExternalApplication
    {
        public const string TabName = "mepOS";

        static RibbonApp()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var requested = new AssemblyName(args.Name);
            if (string.Equals(requested.Name, "RevitAPI", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(requested.Name, "RevitAPIUI", StringComparison.OrdinalIgnoreCase))
            {
                foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (string.Equals(a.GetName().Name, requested.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        return a;
                    }
                }
            }
            return null;
        }

        private static BitmapImage LoadBitmap(string path)
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(path, UriKind.Absolute);
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch
            {
                return null;
            }
        }

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                // 1. Create Ribbon Tab "mepOS"
                try
                {
                    application.CreateRibbonTab(TabName);
                }
                catch
                {
                    // Tab might already exist if reloaded
                }

                // 2. Create Ribbon Panel "Modeling"
                RibbonPanel panel = null;
                foreach (RibbonPanel p in application.GetRibbonPanels(TabName))
                {
                    if (p.Name == "Modeling")
                    {
                        panel = p;
                        break;
                    }
                }
                if (panel == null)
                {
                    panel = application.CreateRibbonPanel(TabName, "Modeling");
                }

                // 3. Create PushButton for "Connect To"
                string thisAssemblyPath = Assembly.GetExecutingAssembly().Location;
                string resourcesDir = Path.Combine(Path.GetDirectoryName(thisAssemblyPath), "Resources");

                var btnData = new PushButtonData(
                    "btnConnectTo",
                    "Connect To",
                    thisAssemblyPath,
                    "mepOS.Loader.ConnectToCommandProxy"
                )
                {
                    ToolTip = "Connect To [CT]\nIntelligently connects pipes, ducts, cable trays, and conduits.\n• Auto-aligns lateral & elevation offsets.\n• Continuous picking mode (Esc to finish)."
                };

                // Load icons if present
                string img32 = Path.Combine(resourcesDir, "CT_32.png");
                if (File.Exists(img32))
                {
                    btnData.LargeImage = LoadBitmap(img32);
                }

                string img16 = Path.Combine(resourcesDir, "CT_16.png");
                if (File.Exists(img16))
                {
                    btnData.Image = LoadBitmap(img16);
                }

                panel.AddItem(btnData);

                // 4. Create PushButton for "Bloom"
                var btnBloomData = new PushButtonData(
                    "btnBloom",
                    "Bloom",
                    thisAssemblyPath,
                    "mepOS.Loader.BloomCommandProxy"
                )
                {
                    ToolTip = "Bloom [BL]\nAutomatically sprouts pipe, duct, cable tray, or conduit stubs from open connectors on fittings, accessories, and equipment.\n• Click to bloom selection or pick elements.\n• Continuous picking mode (Esc to finish)."
                };

                string bloom32 = Path.Combine(resourcesDir, "BL_32.png");
                if (File.Exists(bloom32))
                {
                    btnBloomData.LargeImage = LoadBitmap(bloom32);
                }

                string bloom16 = Path.Combine(resourcesDir, "BL_16.png");
                if (File.Exists(bloom16))
                {
                    btnBloomData.Image = LoadBitmap(bloom16);
                }

                panel.AddItem(btnBloomData);

                // 5. Let mepOS.Core dynamically register any additional new tools
                DynamicLoader.BuildCoreRibbon(application, panel, thisAssemblyPath);

                // 6. Check for updates silently on background thread
                string installDir = Path.GetDirectoryName(thisAssemblyPath);
                UpdateManager.CheckForUpdatesAsync(installDir);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("mepOS // RIBBON STARTUP ERROR", ex.Message);
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}
