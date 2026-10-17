using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace mepOS.Loader
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ConnectToCommandProxy : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            return DynamicLoader.ExecuteCommand("mepOS.Core.Commands.ConnectToCommand", commandData, ref message, elements);
        }
    }
}
