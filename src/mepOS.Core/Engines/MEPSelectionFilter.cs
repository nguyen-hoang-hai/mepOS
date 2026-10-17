using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI.Selection;

namespace mepOS.Core.Engines
{
    public class MEPSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            if (elem == null) return false;

            if (elem is Pipe || elem is Duct || elem is CableTray || elem is Conduit)
            {
                return true;
            }

            var fi = elem as FamilyInstance;
            if (fi != null && fi.MEPModel != null && fi.MEPModel.ConnectorManager != null)
            {
                return true;
            }

            return false;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return true;
        }
    }
}
