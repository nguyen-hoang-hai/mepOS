using System;
using System.Collections.Generic;
using System.IO;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.Exceptions;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using mepOS.Core.Engines;
using mepOS.Core.UI.Components;

namespace mepOS.Core.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class BloomCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UpdateRibbonIcons(commandData.Application);

            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            if (uidoc == null || uidoc.Document == null)
            {
                message = "Please open a Revit document first.";
                return Result.Failed;
            }

            Document doc = uidoc.Document;
            var engine = new BloomEngine(doc);

            // 1. Pre-Selection Mode: If user has elements selected prior to clicking
            ICollection<ElementId> selIds = uidoc.Selection.GetElementIds();
            if (selIds != null && selIds.Count > 0)
            {
                var elementsToBloom = new List<Element>();
                foreach (ElementId eid in selIds)
                {
                    Element el = doc.GetElement(eid);
                    if (el != null && BloomEngine.GetOpenConnectors(el).Count > 0)
                    {
                        elementsToBloom.Add(el);
                    }
                }

                if (elementsToBloom.Count > 0)
                {
                    using (var t = new Transaction(doc, "mepOS // Bloom"))
                    {
                        t.Start();
                        var res = engine.BloomElements(elementsToBloom);
                        if (res.Total > 0)
                        {
                            t.Commit();
                            return Result.Succeeded;
                        }
                        t.RollBack();
                    }
                }
            }

            // 2. Interactive Continuous Picking Loop (Esc to exit)
            var filter = new MEPBloomSelectionFilter();

            while (true)
            {
                Reference pickedRef = null;
                try
                {
                    pickedRef = uidoc.Selection.PickObject(
                        ObjectType.Element,
                        filter,
                        "mepOS // Pick MEP fitting, valve, tray, or equipment to bloom (Press Esc to finish)"
                    );
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    // Clean exit on Esc
                    break;
                }

                if (pickedRef == null) break;

                Element targetElem = doc.GetElement(pickedRef.ElementId);
                if (targetElem == null) continue;

                var openConns = BloomEngine.GetOpenConnectors(targetElem);
                if (openConns.Count == 0)
                {
                    SwissModal.ShowWarning(
                        "Phần tử vừa chọn không có đầu nối (connector) nào đang mở.",
                        "mepOS // KHÔNG CÓ ĐẦU NỐI MỞ",
                        commandData.Application.MainWindowHandle
                    );
                    continue;
                }

                using (var t = new Transaction(doc, "mepOS // Bloom"))
                {
                    t.Start();
                    var res = engine.BloomElements(new[] { targetElem });
                    if (res.Total > 0)
                    {
                        t.Commit();
                    }
                    else
                    {
                        t.RollBack();
                        SwissModal.ShowWarning(
                            "Không thể tạo đoạn ống/máng nối tương ứng.\nVui lòng kiểm tra đã load sẵn Type phù hợp trong dự án.",
                            "mepOS // CHƯA TẠO ĐƯỢC STUB",
                            commandData.Application.MainWindowHandle
                        );
                    }
                }
            }

            return Result.Succeeded;
        }

        private static void UpdateRibbonIcons(UIApplication app)
        {
            try
            {
                string baseDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                string img32 = Path.Combine(baseDir, "Resources", "BL_32.png");
                if (!File.Exists(img32))
                {
                    string fallback = @"D:\OneDrive\@ Personal Project\mepOS\bin\Debug\Resources\BL_32.png";
                    if (File.Exists(fallback)) img32 = fallback;
                }

                if (File.Exists(img32))
                {
                    foreach (var panel in app.GetRibbonPanels("mepOS"))
                    {
                        if (panel.Name == "Modeling")
                        {
                            foreach (var item in panel.GetItems())
                            {
                                var pb = item as PushButton;
                                if (item.Name == "btnBloom" && pb != null)
                                {
                                    pb.ItemText = "Bloom";
                                    var bmp = new System.Windows.Media.Imaging.BitmapImage();
                                    bmp.BeginInit();
                                    bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                                    bmp.UriSource = new Uri(img32, UriKind.Absolute);
                                    bmp.EndInit();
                                    bmp.Freeze();
                                    pb.LargeImage = bmp;
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }
    }
}
