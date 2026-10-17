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

                // 6. Create Suite Brand Panel (DSCons-style wide logo banner on far right)
                CreateBrandPanel(application, thisAssemblyPath, resourcesDir);

                // 7. Check for updates silently on background thread
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

        private static void CreateBrandPanel(UIControlledApplication application, string assemblyPath, string resourcesDir)
        {
            try
            {
                RibbonPanel brandPanel = null;
                foreach (RibbonPanel p in application.GetRibbonPanels(TabName))
                {
                    if (p.Name == "mepOS" || p.Name == " ")
                    {
                        brandPanel = p;
                        break;
                    }
                }

                if (brandPanel == null)
                {
                    try
                    {
                        brandPanel = application.CreateRibbonPanel(TabName, " ");
                    }
                    catch
                    {
                        brandPanel = application.CreateRibbonPanel(TabName, "mepOS");
                    }
                }

                var btnBannerData = new PushButtonData(
                    "btnLogoBanner",
                    " ",
                    assemblyPath,
                    "mepOS.Loader.AboutCommandProxy"
                )
                {
                    ToolTip = "mepOS // Swiss Architectural MEP Suite\nVersion: Active (Auto-updating)\nClick to visit GitHub repository & documentation."
                };

                string bannerPath = Path.Combine(resourcesDir, "logo_banner.png");
                if (File.Exists(bannerPath))
                {
                    btnBannerData.LargeImage = LoadBitmap(bannerPath);
                }

                brandPanel.AddItem(btnBannerData);

                // Stylize banner button to hide text and ensure full-width banner display
                StylizeBannerButton("btnLogoBanner");
            }
            catch
            {
                // Non-critical: failure to load banner should never break tool startup
            }
        }

        private static void StylizeBannerButton(string buttonId)
        {
            try
            {
                var asm = Assembly.Load("AdWindows");
                if (asm == null) return;

                var cmType = asm.GetType("Autodesk.Windows.ComponentManager");
                if (cmType == null) return;

                var ribbonProp = cmType.GetProperty("Ribbon", BindingFlags.Public | BindingFlags.Static);
                if (ribbonProp == null) return;

                object ribbon = ribbonProp.GetValue(null, null);
                if (ribbon == null) return;

                var tabsProp = ribbon.GetType().GetProperty("Tabs");
                if (tabsProp == null) return;

                var tabs = tabsProp.GetValue(ribbon, null) as System.Collections.IEnumerable;
                if (tabs == null) return;

                foreach (object tab in tabs)
                {
                    var tabIdProp = tab.GetType().GetProperty("Id");
                    string tabId = tabIdProp != null ? tabIdProp.GetValue(tab, null) as string : null;
                    if (tabId != null && tabId.IndexOf(TabName, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        var panelsProp = tab.GetType().GetProperty("Panels");
                        var panels = panelsProp != null ? panelsProp.GetValue(tab, null) as System.Collections.IEnumerable : null;
                        if (panels == null) continue;

                        foreach (object panel in panels)
                        {
                            var sourceProp = panel.GetType().GetProperty("Source");
                            object source = sourceProp != null ? sourceProp.GetValue(panel, null) : null;
                            if (source == null) continue;

                            var itemsProp = source.GetType().GetProperty("Items");
                            var items = itemsProp != null ? itemsProp.GetValue(source, null) as System.Collections.IEnumerable : null;
                            if (items == null) continue;

                            foreach (object item in items)
                            {
                                var idProp = item.GetType().GetProperty("Id");
                                string id = idProp != null ? idProp.GetValue(item, null) as string : null;
                                if (id != null && id.IndexOf(buttonId, StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    var showTextProp = item.GetType().GetProperty("ShowText");
                                    if (showTextProp != null)
                                    {
                                        showTextProp.SetValue(item, false, null);
                                    }

                                    var widthProp = item.GetType().GetProperty("Width");
                                    if (widthProp != null)
                                    {
                                        widthProp.SetValue(item, 204.0, null);
                                    }
                                    return;
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // Graceful fallback
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}
