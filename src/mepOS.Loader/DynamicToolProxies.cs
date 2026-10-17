using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace mepOS.Loader
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public abstract class BaseToolProxy : IExternalCommand
    {
        public abstract int Slot { get; }
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            return DynamicLoader.ExecuteSlot(Slot, commandData, ref message, elements);
        }
    }

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ToolProxy_01 : BaseToolProxy { public override int Slot { get { return 1; } } }

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ToolProxy_02 : BaseToolProxy { public override int Slot { get { return 2; } } }

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ToolProxy_03 : BaseToolProxy { public override int Slot { get { return 3; } } }

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ToolProxy_04 : BaseToolProxy { public override int Slot { get { return 4; } } }

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ToolProxy_05 : BaseToolProxy { public override int Slot { get { return 5; } } }

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ToolProxy_06 : BaseToolProxy { public override int Slot { get { return 6; } } }

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ToolProxy_07 : BaseToolProxy { public override int Slot { get { return 7; } } }

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ToolProxy_08 : BaseToolProxy { public override int Slot { get { return 8; } } }

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ToolProxy_09 : BaseToolProxy { public override int Slot { get { return 9; } } }

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ToolProxy_10 : BaseToolProxy { public override int Slot { get { return 10; } } }

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ToolProxy_11 : BaseToolProxy { public override int Slot { get { return 11; } } }

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ToolProxy_12 : BaseToolProxy { public override int Slot { get { return 12; } } }

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ToolProxy_13 : BaseToolProxy { public override int Slot { get { return 13; } } }

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ToolProxy_14 : BaseToolProxy { public override int Slot { get { return 14; } } }

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ToolProxy_15 : BaseToolProxy { public override int Slot { get { return 15; } } }

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ToolProxy_16 : BaseToolProxy { public override int Slot { get { return 16; } } }

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ToolProxy_17 : BaseToolProxy { public override int Slot { get { return 17; } } }

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ToolProxy_18 : BaseToolProxy { public override int Slot { get { return 18; } } }

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ToolProxy_19 : BaseToolProxy { public override int Slot { get { return 19; } } }

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ToolProxy_20 : BaseToolProxy { public override int Slot { get { return 20; } } }
}
