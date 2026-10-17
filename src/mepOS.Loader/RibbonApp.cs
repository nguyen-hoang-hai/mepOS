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

        private static bool _isAligning = false;
        private static bool _eventsHooked = false;
        private static bool _layoutHooked = false;

        private static void CreateBrandPanel(UIControlledApplication application, string assemblyPath, string resourcesDir)
        {
            try
            {
                RibbonPanel brandPanel = null;
                foreach (RibbonPanel p in application.GetRibbonPanels(TabName))
                {
                    if (p.Name == "mepOS" || p.Name == "\u200B")
                    {
                        brandPanel = p;
                        break;
                    }
                }

                if (brandPanel == null)
                {
                    try
                    {
                        // Use zero-width space so default panel title is invisible
                        brandPanel = application.CreateRibbonPanel(TabName, "\u200B");
                    }
                    catch
                    {
                        brandPanel = application.CreateRibbonPanel(TabName, "mepOS");
                    }
                }

                var btnBannerData = new PushButtonData(
                    "btnLogoBanner",
                    "mepOS", // Valid non-whitespace text so PushButtonData constructor never throws
                    assemblyPath,
                    "mepOS.Loader.AboutCommandProxy"
                )
                {
                    ToolTip = "mepOS // Swiss Architectural MEP Suite\nPhiên bản: Active (Auto-updating)\nNhấp để xem tài liệu & kho mã nguồn GitHub."
                };

                string bannerPath = Path.Combine(resourcesDir, "logo_banner.png");
                if (!File.Exists(bannerPath))
                {
                    string devAssets = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(assemblyPath), @"..\..\assets\official_icons\banner_concept2_master_logo.png"));
                    if (File.Exists(devAssets))
                    {
                        bannerPath = devAssets;
                    }
                }

                if (File.Exists(bannerPath))
                {
                    var bmp = LoadBitmap(bannerPath);
                    btnBannerData.LargeImage = bmp;
                    btnBannerData.Image = bmp;
                }

                brandPanel.AddItem(btnBannerData);

                // Schedule visual tree styling and far-right alignment
                ScheduleBrandAlignment(bannerPath);
            }
            catch
            {
                // Non-critical: failure to load banner should never break tool startup
            }
        }

        private static void ScheduleBrandAlignment(string bannerPath)
        {
            try
            {
                var dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
                if (dispatcher == null) return;

                dispatcher.BeginInvoke(
                    System.Windows.Threading.DispatcherPriority.Loaded,
                    new Action(delegate {
                        AlignBrandPanel(bannerPath);
                    })
                );

                dispatcher.BeginInvoke(
                    System.Windows.Threading.DispatcherPriority.Background,
                    new Action(delegate {
                        AlignBrandPanel(bannerPath);
                    })
                );
            }
            catch
            {
            }
        }

        private static void AlignBrandPanel(string bannerPath)
        {
            if (_isAligning) return;
            try
            {
                _isAligning = true;

                object ribbonObj = GetAdWindowsRibbon();
                var ribbonFe = ribbonObj as System.Windows.FrameworkElement;
                if (ribbonFe == null) return;

                // Stylize underlying AdWindows button
                StylizeAdWindowsButton(ribbonObj, bannerPath);

                // Hook window resize once
                if (!_eventsHooked)
                {
                    _eventsHooked = true;
                    ribbonFe.SizeChanged += delegate {
                        ScheduleBrandAlignment(bannerPath);
                    };
                }

                // Find panel list view in WPF visual tree
                var panelListView = FindVisualChild<System.Windows.Controls.Panel>(ribbonFe, "mItemsPanel");
                if (panelListView == null) return;

                if (!_layoutHooked)
                {
                    _layoutHooked = true;
                    panelListView.LayoutUpdated += delegate {
                        if (!_isAligning)
                        {
                            AlignBrandMarginOnly(panelListView, ribbonFe);
                        }
                    };
                }

                // Locate brand panel control
                System.Windows.FrameworkElement brandControl = null;
                int childCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(panelListView);
                for (int i = 0; i < childCount; i++)
                {
                    var child = System.Windows.Media.VisualTreeHelper.GetChild(panelListView, i) as System.Windows.FrameworkElement;
                    if (child == null) continue;
                    if (HasChildNamed(child, "btnLogoBanner") || i == childCount - 1)
                    {
                        brandControl = child;
                    }
                }

                if (brandControl == null) return;

                // Collapse bottom title bar
                var titleBar = FindVisualChild<System.Windows.FrameworkElement>(brandControl, "PART_PanelTitleBar");
                if (titleBar != null)
                {
                    titleBar.Visibility = System.Windows.Visibility.Collapsed;
                }

                // Expand button view to full vertical height
                var rpv = FindVisualChild<System.Windows.FrameworkElement>(brandControl, "mRibbonPanelView");
                if (rpv != null)
                {
                    rpv.Margin = new System.Windows.Thickness(0, 1, 0, 1);
                }

                // Collapse any text label
                var txt = FindVisualChild<System.Windows.FrameworkElement>(brandControl, "mText");
                if (txt != null)
                {
                    txt.Visibility = System.Windows.Visibility.Collapsed;
                }

                // Expand button containers to 204 width
                var btn = FindVisualChild<System.Windows.FrameworkElement>(brandControl, "mButton");
                if (btn != null)
                {
                    btn.Width = 204.0;
                    btn.MinWidth = 204.0;
                }

                var innerPanel = FindVisualChild<System.Windows.Controls.StackPanel>(brandControl, "Panel");
                if (innerPanel != null)
                {
                    innerPanel.Width = 204.0;
                }

                AlignBrandMarginOnly(panelListView, ribbonFe);
            }
            catch
            {
            }
            finally
            {
                _isAligning = false;
            }
        }

        private static void AlignBrandMarginOnly(System.Windows.Controls.Panel panelListView, System.Windows.FrameworkElement ribbonFe)
        {
            try
            {
                int childCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(panelListView);
                if (childCount <= 1) return;

                System.Windows.FrameworkElement brandControl = null;
                for (int i = 0; i < childCount; i++)
                {
                    var child = System.Windows.Media.VisualTreeHelper.GetChild(panelListView, i) as System.Windows.FrameworkElement;
                    if (child == null) continue;
                    if (HasChildNamed(child, "btnLogoBanner") || i == childCount - 1)
                    {
                        brandControl = child;
                    }
                }

                if (brandControl == null) return;

                var scrollViewer = FindVisualChild<System.Windows.Controls.ScrollViewer>(ribbonFe, "mPanelBarScrollViewer");
                double availableWidth = 0;
                if (scrollViewer != null && scrollViewer.ViewportWidth > 100)
                {
                    availableWidth = scrollViewer.ViewportWidth;
                }
                else if (scrollViewer != null && scrollViewer.ActualWidth > 100)
                {
                    availableWidth = scrollViewer.ActualWidth;
                }
                else
                {
                    availableWidth = ribbonFe.ActualWidth - 40;
                }

                double otherPanelsWidth = 0;
                for (int i = 0; i < childCount; i++)
                {
                    var child = System.Windows.Media.VisualTreeHelper.GetChild(panelListView, i) as System.Windows.FrameworkElement;
                    if (child == null || child == brandControl || child.Visibility != System.Windows.Visibility.Visible) continue;
                    otherPanelsWidth += child.ActualWidth + child.Margin.Left + child.Margin.Right;
                }

                double brandWidth = brandControl.ActualWidth > 50 ? brandControl.ActualWidth : 204.0;
                double targetMargin = Math.Max(0, availableWidth - otherPanelsWidth - brandWidth - 25.0);

                if (Math.Abs(brandControl.Margin.Left - targetMargin) > 2.0)
                {
                    brandControl.Margin = new System.Windows.Thickness(targetMargin, 0, 0, 0);
                }
            }
            catch
            {
            }
        }

        private static object GetAdWindowsRibbon()
        {
            try
            {
                var asm = Assembly.Load("AdWindows");
                if (asm == null) return null;

                var cmType = asm.GetType("Autodesk.Windows.ComponentManager");
                if (cmType == null) return null;

                var ribbonProp = cmType.GetProperty("Ribbon", BindingFlags.Public | BindingFlags.Static);
                if (ribbonProp == null) return null;

                return ribbonProp.GetValue(null, null);
            }
            catch
            {
                return null;
            }
        }

        private static void StylizeAdWindowsButton(object ribbon, string bannerPath)
        {
            try
            {
                if (ribbon == null) return;
                var tabsProp = ribbon.GetType().GetProperty("Tabs");
                if (tabsProp == null) return;

                var tabs = tabsProp.GetValue(ribbon, null) as System.Collections.IEnumerable;
                if (tabs == null) return;

                BitmapImage bannerBmp = null;
                if (File.Exists(bannerPath))
                {
                    bannerBmp = LoadBitmap(bannerPath);
                }

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
                                if (id != null && id.IndexOf("btnLogoBanner", StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    var showTextProp = item.GetType().GetProperty("ShowText");
                                    if (showTextProp != null) showTextProp.SetValue(item, false, null);

                                    var textProp = item.GetType().GetProperty("Text");
                                    if (textProp != null) textProp.SetValue(item, "", null);

                                    var widthProp = item.GetType().GetProperty("Width");
                                    if (widthProp != null) widthProp.SetValue(item, 204.0, null);

                                    var minWidthProp = item.GetType().GetProperty("MinWidth");
                                    if (minWidthProp != null) minWidthProp.SetValue(item, 204.0, null);

                                    var heightProp = item.GetType().GetProperty("Height");
                                    if (heightProp != null) heightProp.SetValue(item, 56.0, null);

                                    var showImgProp = item.GetType().GetProperty("ShowImage");
                                    if (showImgProp != null) showImgProp.SetValue(item, true, null);

                                    if (bannerBmp != null)
                                    {
                                        var lImgProp = item.GetType().GetProperty("LargeImage");
                                        if (lImgProp != null) lImgProp.SetValue(item, bannerBmp, null);

                                        var imgProp = item.GetType().GetProperty("Image");
                                        if (imgProp != null) imgProp.SetValue(item, bannerBmp, null);
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
            }
        }

        private static bool HasChildNamed(System.Windows.DependencyObject parent, string name)
        {
            if (parent == null) return false;
            var fe = parent as System.Windows.FrameworkElement;
            if (fe != null && fe.Name != null && fe.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
            int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                if (HasChildNamed(System.Windows.Media.VisualTreeHelper.GetChild(parent, i), name))
                {
                    return true;
                }
            }
            return false;
        }

        private static T FindVisualChild<T>(System.Windows.DependencyObject parent, string name) where T : System.Windows.FrameworkElement
        {
            if (parent == null) return null;
            int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                var fe = child as T;
                if (fe != null && (string.IsNullOrEmpty(name) || string.Equals(fe.Name, name, StringComparison.OrdinalIgnoreCase)))
                {
                    return fe;
                }
                var sub = FindVisualChild<T>(child, name);
                if (sub != null) return sub;
            }
            return null;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}
