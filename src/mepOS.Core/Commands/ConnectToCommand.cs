using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Interop;
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
    public class ConnectToCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UpdateRibbonIcon(commandData.Application);

            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            if (uidoc == null || uidoc.Document == null)
            {
                message = "Please open a Revit document first.";
                return Result.Failed;
            }

            Document doc = uidoc.Document;
            ConnectToConfig config = new ConnectToConfig();

            // 1. Pre-selection check: If exactly 2 elements are selected
            ICollection<ElementId> selIds = uidoc.Selection.GetElementIds();
            if (selIds != null && selIds.Count == 2)
            {
                var it = selIds.GetEnumerator();
                it.MoveNext(); Element e1 = doc.GetElement(it.Current);
                it.MoveNext(); Element e2 = doc.GetElement(it.Current);

                var engine = new ConnectToEngine(doc, config);
                using (var t = new Transaction(doc, "mepOS // Connect To"))
                {
                    t.Start();
                    string msg;
                    if (engine.Connect(e1, null, e2, null, out msg))
                    {
                        t.Commit();
                        return Result.Succeeded;
                    }
                    t.RollBack();
                    SwissModal.ShowWarning(
                        msg,
                        "mepOS // KẾT NỐI THẤT BẠI",
                        commandData.Application.MainWindowHandle
                    );
                    return Result.Failed;
                }
            }

            // 3. Continuous Interactive Picking Loop
            var filter = new MEPSelectionFilter();
            var connectEngine = new ConnectToEngine(doc, config);

            while (true)
            {
                Reference ref1 = null;
                Reference ref2 = null;

                try
                {
                    ref1 = uidoc.Selection.PickObject(
                        ObjectType.Element,
                        filter,
                        "mepOS // Pick FIRST MEP element to connect (Press Esc to finish)"
                    );
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    // User pressed Esc -> Silent clean exit
                    break;
                }

                if (ref1 == null) break;

                try
                {
                    ref2 = uidoc.Selection.PickObject(
                        ObjectType.Element,
                        filter,
                        "mepOS // Pick SECOND MEP element to connect"
                    );
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    // User pressed Esc during 2nd pick -> continue or break
                    break;
                }

                if (ref2 == null) break;

                Element elem1 = doc.GetElement(ref1.ElementId);
                Element elem2 = doc.GetElement(ref2.ElementId);

                if (elem1 == null || elem2 == null) continue;

                using (var t = new Transaction(doc, "mepOS // Connect To"))
                {
                    t.Start();
                    string msg;
                    bool success = connectEngine.Connect(
                        elem1,
                        ref1.GlobalPoint,
                        elem2,
                        ref2.GlobalPoint,
                        out msg
                    );

                    if (success)
                    {
                        t.Commit();
                    }
                    else
                    {
                        t.RollBack();
                        // Only alert if there is a real error
                        if (!string.IsNullOrEmpty(msg))
                        {
                            SwissModal.ShowWarning(
                                msg,
                                "mepOS // KẾT NỐI CHƯA HOÀN TẤT",
                                commandData.Application.MainWindowHandle
                            );
                        }
                    }
                }
            }

            return Result.Succeeded;
        }

        private static void UpdateRibbonIcon(UIApplication app)
        {
            try
            {
                string baseDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                string img32 = System.IO.Path.Combine(baseDir, "Resources", "CT_32.png");
                if (!System.IO.File.Exists(img32))
                {
                    string fallback = @"D:\OneDrive\@ Personal Project\mepOS\bin\Debug\Resources\CT_32.png";
                    if (System.IO.File.Exists(fallback)) img32 = fallback;
                }

                if (System.IO.File.Exists(img32))
                {
                    foreach (var panel in app.GetRibbonPanels("mepOS"))
                    {
                        if (panel.Name == "Modeling")
                        {
                            foreach (var item in panel.GetItems())
                            {
                                var pb = item as PushButton;
                                if (item.Name == "btnConnectTo" && pb != null)
                                {
                                    pb.ItemText = "Connect To";
                                    var bmp = new System.Windows.Media.Imaging.BitmapImage();
                                    bmp.BeginInit();
                                    bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                                    bmp.UriSource = new Uri(img32, System.UriKind.Absolute);
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
