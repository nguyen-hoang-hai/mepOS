using System;
using System.Diagnostics;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using mepOS.Core.UI.Components;

namespace mepOS.Core.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class AboutCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/nguyen-hoang-hai/mepOS",
                    UseShellExecute = true
                });
            }
            catch
            {
                SwissModal.ShowSuccess(
                    "mepOS — Swiss Clean Architectural Revit Suite\n" +
                    "Phiên bản: v1.0.1 (Active)\n" +
                    "Tác giả: Nguyễn Hoàng Hải\n" +
                    "Repository: https://github.com/nguyen-hoang-hai/mepOS",
                    "mepOS // SUITE DETAILS",
                    commandData.Application.MainWindowHandle
                );
            }
            return Result.Succeeded;
        }
    }
}
