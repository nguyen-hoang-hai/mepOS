using System;
using System.IO;
using System.Reflection;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace mepOS.Loader
{
    /// <summary>
    /// Dynamic In-Memory Assembly Loader.
    /// Reads DLL bytes into RAM before loading so Revit NEVER locks the DLL file on disk!
    /// Supports dynamic Ribbon tool mapping and slot execution.
    /// </summary>
    public static class DynamicLoader
    {
        private static Assembly _lastLoadedCore;

        static DynamicLoader()
        {
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
        }

        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (string.IsNullOrEmpty(args.Name)) return null;

            // Never return the main assembly for satellite resource requests (.resources)!
            if (args.Name.Contains(".resources"))
            {
                return null;
            }

            var requestedName = new AssemblyName(args.Name);
            if (string.Equals(requestedName.Name, "mepOS.Core", StringComparison.OrdinalIgnoreCase))
            {
                return _lastLoadedCore;
            }

            return null;
        }

        public static string FindCoreDllPath()
        {
            string loaderDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // 1. Check local directory next to mepOS.Loader.dll
            string coreDllPath = Path.Combine(loaderDir, "mepOS.Core.dll");
            if (File.Exists(coreDllPath)) return coreDllPath;

            // 2. Check %APPDATA%\mepOS\mepOS.Core.dll
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                "mepOS", 
                "mepOS.Core.dll"
            );
            if (File.Exists(appDataPath)) return appDataPath;

            // 3. Fallback to solution bin folder (dev mode)
            try
            {
                string devPath = Path.GetFullPath(Path.Combine(loaderDir, "..", "..", "bin", "Debug", "mepOS.Core.dll"));
                if (File.Exists(devPath)) return devPath;
            }
            catch { }

            return coreDllPath;
        }

        public static Assembly LoadCoreAssembly()
        {
            string coreDllPath = FindCoreDllPath();
            if (string.IsNullOrEmpty(coreDllPath) || !File.Exists(coreDllPath))
            {
                return null;
            }

            string pdbPath = Path.ChangeExtension(coreDllPath, ".pdb");
            byte[] assemblyBytes = File.ReadAllBytes(coreDllPath);
            byte[] pdbBytes = File.Exists(pdbPath) ? File.ReadAllBytes(pdbPath) : null;

            Assembly coreAssembly = Assembly.Load(assemblyBytes, pdbBytes);
            _lastLoadedCore = coreAssembly;
            return coreAssembly;
        }

        public static Result ExecuteCommand(string fullTypeName, ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                Assembly coreAssembly = LoadCoreAssembly();
                if (coreAssembly == null)
                {
                    message = string.Format("mepOS.Core.dll not found at: {0}", FindCoreDllPath());
                    TaskDialog.Show("mepOS // LOADER ERROR", message);
                    return Result.Failed;
                }

                // Resolve command type and invoke Execute
                Type cmdType = coreAssembly.GetType(fullTypeName);
                if (cmdType == null)
                {
                    message = string.Format("Command type '{0}' was not found in mepOS.Core.dll.", fullTypeName);
                    TaskDialog.Show("mepOS // LOADER ERROR", message);
                    return Result.Failed;
                }

                var commandInstance = Activator.CreateInstance(cmdType) as IExternalCommand;
                if (commandInstance == null)
                {
                    message = string.Format("Type '{0}' does not implement IExternalCommand.", fullTypeName);
                    TaskDialog.Show("mepOS // LOADER ERROR", message);
                    return Result.Failed;
                }

                return commandInstance.Execute(commandData, ref message, elements);
            }
            catch (Exception ex)
            {
                message = string.Format("mepOS Loader Exception: {0}\n{1}", ex.Message, ex.StackTrace);
                TaskDialog.Show("mepOS // EXCEPTION", message);
                return Result.Failed;
            }
        }

        public static Result ExecuteSlot(int slotIndex, ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                Assembly coreAssembly = LoadCoreAssembly();
                if (coreAssembly == null)
                {
                    message = "mepOS.Core.dll could not be loaded into memory.";
                    return Result.Failed;
                }

                Type builderType = coreAssembly.GetType("mepOS.Core.RibbonBuilder");
                if (builderType == null)
                {
                    message = "mepOS.Core.RibbonBuilder type not found.";
                    return Result.Failed;
                }

                MethodInfo mi = builderType.GetMethod("GetSlotCommandType", BindingFlags.Public | BindingFlags.Static);
                if (mi == null)
                {
                    message = "GetSlotCommandType method not found.";
                    return Result.Failed;
                }

                string fullTypeName = mi.Invoke(null, new object[] { slotIndex }) as string;
                if (string.IsNullOrEmpty(fullTypeName))
                {
                    message = string.Format("No dynamic tool is mapped to Slot {0}.", slotIndex);
                    return Result.Failed;
                }

                return ExecuteCommand(fullTypeName, commandData, ref message, elements);
            }
            catch (Exception ex)
            {
                message = string.Format("Slot {0} Execution Exception: {1}", slotIndex, ex.Message);
                return Result.Failed;
            }
        }

        public static void BuildCoreRibbon(UIControlledApplication application, RibbonPanel panel, string thisAssemblyPath)
        {
            try
            {
                Assembly coreAssembly = LoadCoreAssembly();
                if (coreAssembly == null) return;

                Type builderType = coreAssembly.GetType("mepOS.Core.RibbonBuilder");
                if (builderType != null)
                {
                    MethodInfo mi = builderType.GetMethod("BuildDynamicTools", BindingFlags.Public | BindingFlags.Static);
                    if (mi != null)
                    {
                        mi.Invoke(null, new object[] { application, panel, thisAssemblyPath });
                    }
                }
            }
            catch
            {
                // Non-fatal - built-in buttons remain intact
            }
        }
    }
}
