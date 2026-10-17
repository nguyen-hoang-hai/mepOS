using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;

namespace mepOS.Core
{
    /// <summary>
    /// Dynamic Ribbon Builder in mepOS.Core.
    /// Allows defining and adding new MEP tools to the Revit Ribbon dynamically
    /// without ever needing to recompile or reinstall mepOS.Loader on client machines!
    /// </summary>
    public static class RibbonBuilder
    {
        private static readonly Dictionary<int, string> SlotMap = new Dictionary<int, string>();

        public static string GetSlotCommandType(int slotIndex)
        {
            string cmd;
            if (SlotMap.TryGetValue(slotIndex, out cmd))
            {
                return cmd;
            }
            return null;
        }

        public static void BuildDynamicTools(UIControlledApplication application, RibbonPanel panel, string loaderAssemblyPath)
        {
            // Future dynamic tools beyond Connect To and Bloom can be registered here:
            // RegisterTool(panel, loaderAssemblyPath, 1, "CAD Wire", "CW_32.png", "mepOS.Core.Commands.CadWireCommand", "CAD Wire [CW]\nConvert 2D CAD linework to MEP runs.");
        }

        private static void RegisterTool(
            RibbonPanel panel, 
            string loaderAssemblyPath, 
            int slot, 
            string text, 
            string iconName, 
            string fullCommandName, 
            string toolTip)
        {
            SlotMap[slot] = fullCommandName;
            string proxyClassName = string.Format("mepOS.Loader.ToolProxy_{0:D2}", slot);

            var btnData = new PushButtonData("btnDyn_" + slot, text, loaderAssemblyPath, proxyClassName)
            {
                ToolTip = toolTip
            };

            string resDir = Path.Combine(Path.GetDirectoryName(loaderAssemblyPath), "Resources");
            string iconPath = Path.Combine(resDir, iconName);
            if (File.Exists(iconPath))
            {
                try
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.UriSource = new Uri(iconPath, UriKind.Absolute);
                    bmp.EndInit();
                    bmp.Freeze();
                    btnData.LargeImage = bmp;
                }
                catch { }
            }

            panel.AddItem(btnData);
        }
    }
}
