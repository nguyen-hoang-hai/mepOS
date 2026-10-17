using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;

namespace mepOS.Core.Engines
{
    public class MEPBloomSelectionFilter : Autodesk.Revit.UI.Selection.ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            if (elem == null) return false;
            ConnectorManager cm = BloomEngine.GetConnectorManager(elem);
            return cm != null && cm.Connectors != null && cm.Connectors.Size > 0;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }

    public class BloomResult
    {
        public int Pipes { get; set; }
        public int Ducts { get; set; }
        public int CableTrays { get; set; }
        public int Conduits { get; set; }
        public int ElementsProcessed { get; set; }
        public int Total
        {
            get { return Pipes + Ducts + CableTrays + Conduits; }
        }
    }

    /// <summary>
    /// Automated MEP Stub-out & Connector Blooming Engine.
    /// Sprouts pipe, duct, cable tray, or conduit stubs from open connectors on fittings,
    /// accessories, and equipment to enable instant 3D routing and Trim/Extend connections.
    /// </summary>
    public class BloomEngine
    {
        private readonly Document _doc;
        private readonly double _stubLengthFt;

        // Default stub length: 300 mm = ~0.984252 feet
        public const double DefaultStubLengthMm = 300.0;
        private const double MmToFeet = 1.0 / 304.8;

        public BloomEngine(Document doc, double stubLengthMm = DefaultStubLengthMm)
        {
            if (doc == null) throw new ArgumentNullException("doc");
            _doc = doc;
            double lenMm = stubLengthMm > 10.0 ? stubLengthMm : DefaultStubLengthMm;
            _stubLengthFt = lenMm * MmToFeet;
        }

        public BloomResult BloomElements(IEnumerable<Element> elements)
        {
            var result = new BloomResult();
            if (elements == null) return result;

            foreach (Element elem in elements)
            {
                if (elem == null) continue;

                var openConns = GetOpenConnectors(elem);
                if (openConns.Count == 0) continue;

                bool hadStubs = false;
                foreach (Connector c in openConns)
                {
                    try
                    {
                        if (c.Domain == Domain.DomainPiping)
                        {
                            var pipe = BloomPipeConnector(elem, c);
                            if (pipe != null)
                            {
                                result.Pipes++;
                                hadStubs = true;
                            }
                        }
                        else if (c.Domain == Domain.DomainHvac)
                        {
                            var duct = BloomDuctConnector(elem, c);
                            if (duct != null)
                            {
                                result.Ducts++;
                                hadStubs = true;
                            }
                        }
                        else if (c.Domain == Domain.DomainCableTrayConduit)
                        {
                            if (IsCableTrayTarget(elem, c))
                            {
                                var tray = BloomCableTrayConnector(elem, c);
                                if (tray != null)
                                {
                                    result.CableTrays++;
                                    hadStubs = true;
                                }
                            }
                            else
                            {
                                var conduit = BloomConduitConnector(elem, c);
                                if (conduit != null)
                                {
                                    result.Conduits++;
                                    hadStubs = true;
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Safely continue blooming other connectors
                    }
                }

                if (hadStubs)
                {
                    result.ElementsProcessed++;
                }
            }

            return result;
        }

        #region Open Connector Discovery

        public static ConnectorManager GetConnectorManager(Element elem)
        {
            if (elem == null) return null;

            var fi = elem as FamilyInstance;
            if (fi != null && fi.MEPModel != null && fi.MEPModel.ConnectorManager != null)
            {
                return fi.MEPModel.ConnectorManager;
            }

            var curve = elem as MEPCurve;
            if (curve != null && curve.ConnectorManager != null)
            {
                return curve.ConnectorManager;
            }

            // Fallback reflection for special MEP fittings / accessories
            try
            {
                var prop = elem.GetType().GetProperty("ConnectorManager");
                if (prop != null)
                {
                    return prop.GetValue(elem, null) as ConnectorManager;
                }
            }
            catch { }

            return null;
        }

        public static List<Connector> GetOpenConnectors(Element elem)
        {
            var openList = new List<Connector>();
            var cm = GetConnectorManager(elem);
            if (cm == null || cm.Connectors == null) return openList;

            foreach (Connector c in cm.Connectors)
            {
                try
                {
                    if (c.IsConnected) continue;

                    // Only physical MEP domains
                    if (c.Domain != Domain.DomainPiping &&
                        c.Domain != Domain.DomainHvac &&
                        c.Domain != Domain.DomainCableTrayConduit)
                    {
                        continue;
                    }

                    // Filter out logical/reference connectors
                    if (c.ConnectorType == ConnectorType.Logical ||
                        c.ConnectorType.ToString().Contains("Reference"))
                    {
                        continue;
                    }

                    openList.Add(c);
                }
                catch { }
            }

            return openList;
        }

        #endregion

        #region Direction Vector & Level Utilities

        public static XYZ GetOutwardVector(Connector c, Element elem)
        {
            try
            {
                Transform cs = c.CoordinateSystem;
                if (cs != null && cs.BasisZ != null && cs.BasisZ.GetLength() > 1e-6)
                {
                    return cs.BasisZ.Normalize();
                }
            }
            catch { }

            // Fallback: Vector from element location to connector origin
            try
            {
                if (elem != null && elem.Location is LocationPoint)
                {
                    var lp = (LocationPoint)elem.Location;
                    if (lp.Point != null)
                    {
                        XYZ v = c.Origin - lp.Point;
                        if (v.GetLength() > 1e-6)
                        {
                            return v.Normalize();
                        }
                    }
                }
            }
            catch { }

            return XYZ.BasisX;
        }

        private ElementId GetElementLevelId(Element elem, double zCoord)
        {
            try
            {
                if (elem != null && elem.LevelId != null && elem.LevelId != ElementId.InvalidElementId)
                    return elem.LevelId;
            }
            catch { }

            try
            {
                if (elem != null)
                {
                    var pRefLvl = elem.get_Parameter(BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM) ??
                                  elem.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM) ??
                                  elem.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM);

                    if (pRefLvl != null && pRefLvl.HasValue && pRefLvl.AsElementId() != ElementId.InvalidElementId)
                        return pRefLvl.AsElementId();
                }
            }
            catch { }

            // Fallback: Closest Level by Elevation
            try
            {
                var levels = new FilteredElementCollector(_doc)
                    .OfClass(typeof(Level))
                    .WhereElementIsNotElementType()
                    .Cast<Level>()
                    .OrderBy(l => Math.Abs(l.Elevation - zCoord))
                    .FirstOrDefault();

                if (levels != null) return levels.Id;
            }
            catch { }

            return ElementId.InvalidElementId;
        }

        #endregion

        #region Adjacent Element MEP Info Inheritance

        private class AdjacentMEPInfo
        {
            public ElementId TypeId { get; set; }
            public ElementId SystemTypeId { get; set; }
            public ElementId LevelId { get; set; }
            public double? Width { get; set; }
            public double? Height { get; set; }
            public double? Diameter { get; set; }

            public AdjacentMEPInfo()
            {
                TypeId = ElementId.InvalidElementId;
                SystemTypeId = ElementId.InvalidElementId;
                LevelId = ElementId.InvalidElementId;
            }
        }

        private AdjacentMEPInfo FindAdjacentMEPInfo(Element elem, Domain domain)
        {
            var info = new AdjacentMEPInfo();
            if (elem == null) return info;

            // 1. Inspect target element itself
            try
            {
                var cmSelf = GetConnectorManager(elem);
                if (cmSelf != null && cmSelf.Connectors != null)
                {
                    foreach (Connector c in cmSelf.Connectors)
                    {
                        if (c.MEPSystem != null)
                        {
                            info.SystemTypeId = c.MEPSystem.GetTypeId();
                            break;
                        }
                    }
                }
            }
            catch { }

            if (info.SystemTypeId == ElementId.InvalidElementId)
            {
                try
                {
                    Parameter pSys = null;
                    if (domain == Domain.DomainPiping)
                        pSys = elem.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM);
                    else if (domain == Domain.DomainHvac)
                        pSys = elem.get_Parameter(BuiltInParameter.RBS_DUCT_SYSTEM_TYPE_PARAM);

                    if (pSys != null && pSys.HasValue && pSys.AsElementId() != ElementId.InvalidElementId)
                        info.SystemTypeId = pSys.AsElementId();
                }
                catch { }
            }

            var cm = GetConnectorManager(elem);
            if (cm == null || cm.Connectors == null) return info;

            // 2. Breadth-first traversal up to depth 3
            var visited = new HashSet<ElementId> { elem.Id };
            var queue = new List<Element> { elem };
            int depth = 0;

            while (queue.Count > 0 && depth < 3)
            {
                var nextQueue = new List<Element>();
                foreach (Element current in queue)
                {
                    var cMgr = GetConnectorManager(current);
                    if (cMgr == null || cMgr.Connectors == null) continue;

                    foreach (Connector c in cMgr.Connectors)
                    {
                        if (!c.IsConnected || c.Domain != domain) continue;

                        foreach (Connector refC in c.AllRefs)
                        {
                            Element owner = refC.Owner;
                            if (owner == null || visited.Contains(owner.Id)) continue;
                            visited.Add(owner.Id);

                            // Pipe
                            var pipe = owner as Pipe;
                            if (domain == Domain.DomainPiping && pipe != null)
                            {
                                info.TypeId = pipe.GetTypeId();
                                if (pipe.MEPSystem != null)
                                    info.SystemTypeId = pipe.MEPSystem.GetTypeId();

                                if (info.SystemTypeId == ElementId.InvalidElementId)
                                {
                                    var pSys = pipe.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM);
                                    if (pSys != null && pSys.HasValue) info.SystemTypeId = pSys.AsElementId();
                                }

                                if (pipe.ReferenceLevel != null)
                                    info.LevelId = pipe.ReferenceLevel.Id;

                                var pDiam = pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM) ??
                                            pipe.get_Parameter(BuiltInParameter.RBS_CURVE_DIAMETER_PARAM);
                                if (pDiam != null && pDiam.HasValue) info.Diameter = pDiam.AsDouble();

                                return info;
                            }

                            // Duct
                            var duct = owner as Duct;
                            if (domain == Domain.DomainHvac && duct != null)
                            {
                                info.TypeId = duct.GetTypeId();
                                if (duct.MEPSystem != null)
                                    info.SystemTypeId = duct.MEPSystem.GetTypeId();

                                if (info.SystemTypeId == ElementId.InvalidElementId)
                                {
                                    var pSys = duct.get_Parameter(BuiltInParameter.RBS_DUCT_SYSTEM_TYPE_PARAM);
                                    if (pSys != null && pSys.HasValue) info.SystemTypeId = pSys.AsElementId();
                                }

                                if (duct.ReferenceLevel != null)
                                    info.LevelId = duct.ReferenceLevel.Id;

                                var pW = duct.get_Parameter(BuiltInParameter.RBS_CURVE_WIDTH_PARAM);
                                var pH = duct.get_Parameter(BuiltInParameter.RBS_CURVE_HEIGHT_PARAM);
                                var pD = duct.get_Parameter(BuiltInParameter.RBS_CURVE_DIAMETER_PARAM);

                                if (pW != null && pW.HasValue) info.Width = pW.AsDouble();
                                if (pH != null && pH.HasValue) info.Height = pH.AsDouble();
                                if (pD != null && pD.HasValue) info.Diameter = pD.AsDouble();

                                return info;
                            }

                            // Cable Tray
                            var tray = owner as CableTray;
                            if (domain == Domain.DomainCableTrayConduit && tray != null)
                            {
                                info.TypeId = tray.GetTypeId();
                                if (tray.ReferenceLevel != null)
                                    info.LevelId = tray.ReferenceLevel.Id;
                                else if (tray.LevelId != null && tray.LevelId != ElementId.InvalidElementId)
                                    info.LevelId = tray.LevelId;

                                var pW = tray.get_Parameter(BuiltInParameter.RBS_CABLETRAY_WIDTH_PARAM) ??
                                         tray.get_Parameter(BuiltInParameter.RBS_CURVE_WIDTH_PARAM);
                                var pH = tray.get_Parameter(BuiltInParameter.RBS_CABLETRAY_HEIGHT_PARAM) ??
                                         tray.get_Parameter(BuiltInParameter.RBS_CURVE_HEIGHT_PARAM);

                                if (pW != null && pW.HasValue) info.Width = pW.AsDouble();
                                if (pH != null && pH.HasValue) info.Height = pH.AsDouble();

                                return info;
                            }

                            // Conduit
                            var conduit = owner as Conduit;
                            if (domain == Domain.DomainCableTrayConduit && conduit != null)
                            {
                                info.TypeId = conduit.GetTypeId();
                                if (conduit.ReferenceLevel != null)
                                    info.LevelId = conduit.ReferenceLevel.Id;
                                else if (conduit.LevelId != null && conduit.LevelId != ElementId.InvalidElementId)
                                    info.LevelId = conduit.LevelId;

                                var pD = conduit.get_Parameter(BuiltInParameter.RBS_CONDUIT_DIAMETER_PARAM) ??
                                         conduit.get_Parameter(BuiltInParameter.RBS_CURVE_DIAMETER_PARAM);
                                if (pD != null && pD.HasValue) info.Diameter = pD.AsDouble();

                                return info;
                            }

                            nextQueue.Add(owner);
                        }
                    }
                }
                queue = nextQueue;
                depth++;
            }

            return info;
        }

        #endregion

        #region Piping Stubs

        private Pipe BloomPipeConnector(Element elem, Connector connector)
        {
            XYZ p0 = connector.Origin;
            XYZ dir = GetOutwardVector(connector, elem);
            XYZ p1 = p0 + dir * _stubLengthFt;

            var adj = FindAdjacentMEPInfo(elem, Domain.DomainPiping);
            ElementId pipeTypeId = adj.TypeId != ElementId.InvalidElementId ? adj.TypeId : GetDefaultPipeTypeId();
            ElementId systemTypeId = adj.SystemTypeId != ElementId.InvalidElementId ? adj.SystemTypeId : GetPipingSystemTypeId(connector, elem);
            ElementId levelId = adj.LevelId != ElementId.InvalidElementId ? adj.LevelId : GetElementLevelId(elem, p0.Z);

            if (pipeTypeId == ElementId.InvalidElementId ||
                systemTypeId == ElementId.InvalidElementId ||
                levelId == ElementId.InvalidElementId)
            {
                return null;
            }

            Pipe pipe = null;

            // Tier 1: Connector-based creation (Native auto-connects)
            try
            {
                pipe = Pipe.Create(_doc, systemTypeId, pipeTypeId, connector, p1);
            }
            catch { }

            // Tier 2: Point-to-point creation fallback
            if (pipe == null)
            {
                try
                {
                    pipe = Pipe.Create(_doc, systemTypeId, pipeTypeId, levelId, p0, p1);
                }
                catch { }
            }

            if (pipe == null) return null;

            // Set diameter matching connector
            try
            {
                double? diameter = null;
                if (connector.Shape == ConnectorProfileType.Round)
                {
                    diameter = connector.Radius * 2.0;
                }
                if (!diameter.HasValue && adj.Diameter.HasValue)
                {
                    diameter = adj.Diameter.Value;
                }

                if (diameter.HasValue && diameter.Value > 0)
                {
                    var pDiam = pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM) ??
                                pipe.get_Parameter(BuiltInParameter.RBS_CURVE_DIAMETER_PARAM);
                    if (pDiam != null && !pDiam.IsReadOnly)
                    {
                        pDiam.Set(diameter.Value);
                    }
                }
            }
            catch { }

            _doc.Regenerate();

            // Auto-connect fallback if not connected
            if (!connector.IsConnected && pipe.ConnectorManager != null)
            {
                foreach (Connector pc in pipe.ConnectorManager.Connectors)
                {
                    if (pc.Origin.DistanceTo(p0) < 0.1)
                    {
                        try { connector.ConnectTo(pc); } catch { }
                        break;
                    }
                }
            }

            return pipe;
        }

        private ElementId GetDefaultPipeTypeId()
        {
            // Existing pipe in doc
            var p = new FilteredElementCollector(_doc)
                .OfClass(typeof(Pipe))
                .WhereElementIsNotElementType()
                .Cast<Pipe>()
                .FirstOrDefault();
            if (p != null) return p.GetTypeId();

            // PipeType
            var pt = new FilteredElementCollector(_doc)
                .OfClass(typeof(PipeType))
                .FirstOrDefault();
            if (pt != null) return pt.Id;

            var cat = new FilteredElementCollector(_doc)
                .OfCategory(BuiltInCategory.OST_PipeCurves)
                .WhereElementIsElementType()
                .FirstOrDefault();
            return cat != null ? cat.Id : ElementId.InvalidElementId;
        }

        private ElementId GetPipingSystemTypeId(Connector connector, Element elem)
        {
            if (connector.MEPSystem != null)
                return connector.MEPSystem.GetTypeId();

            if (elem != null)
            {
                var cmElem = GetConnectorManager(elem);
                if (cmElem != null && cmElem.Connectors != null)
                {
                    foreach (Connector c in cmElem.Connectors)
                    {
                        if (c.MEPSystem != null)
                            return c.MEPSystem.GetTypeId();
                    }
                }

                var pSys = elem.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM);
                if (pSys != null && pSys.HasValue && pSys.AsElementId() != ElementId.InvalidElementId)
                    return pSys.AsElementId();
            }

            // Try existing pipe's system
            var existingPipe = new FilteredElementCollector(_doc)
                .OfClass(typeof(Pipe))
                .WhereElementIsNotElementType()
                .Cast<Pipe>()
                .FirstOrDefault(x => x.MEPSystem != null);
            if (existingPipe != null) return existingPipe.MEPSystem.GetTypeId();

            // First PipingSystemType in doc
            var pst = new FilteredElementCollector(_doc)
                .OfClass(typeof(PipingSystemType))
                .FirstOrDefault();
            if (pst != null) return pst.Id;

            return ElementId.InvalidElementId;
        }

        #endregion

        #region HVAC Duct Stubs

        private Duct BloomDuctConnector(Element elem, Connector connector)
        {
            XYZ p0 = connector.Origin;
            XYZ dir = GetOutwardVector(connector, elem);
            XYZ p1 = p0 + dir * _stubLengthFt;

            var adj = FindAdjacentMEPInfo(elem, Domain.DomainHvac);
            ElementId ductTypeId = adj.TypeId != ElementId.InvalidElementId ? adj.TypeId : GetDefaultDuctTypeId(connector.Shape);
            ElementId systemTypeId = adj.SystemTypeId != ElementId.InvalidElementId ? adj.SystemTypeId : GetDuctSystemTypeId(connector, elem);
            ElementId levelId = adj.LevelId != ElementId.InvalidElementId ? adj.LevelId : GetElementLevelId(elem, p0.Z);

            if (ductTypeId == ElementId.InvalidElementId ||
                systemTypeId == ElementId.InvalidElementId ||
                levelId == ElementId.InvalidElementId)
            {
                return null;
            }

            Duct duct = null;

            // Tier 1: Connector-based creation
            try
            {
                duct = Duct.Create(_doc, systemTypeId, ductTypeId, connector, p1);
            }
            catch { }

            // Tier 2: Point-to-point fallback
            if (duct == null)
            {
                try
                {
                    duct = Duct.Create(_doc, systemTypeId, ductTypeId, levelId, p0, p1);
                }
                catch { }
            }

            if (duct == null) return null;

            // Apply dimensions
            try
            {
                if (connector.Shape == ConnectorProfileType.Round)
                {
                    double? diam = connector.Radius * 2.0;
                    if (diam <= 0 && adj.Diameter.HasValue) diam = adj.Diameter.Value;
                    if (diam.HasValue && diam.Value > 0)
                    {
                        var pD = duct.get_Parameter(BuiltInParameter.RBS_CURVE_DIAMETER_PARAM);
                        if (pD != null && !pD.IsReadOnly) pD.Set(diam.Value);
                    }
                }
                else
                {
                    double w = connector.Width;
                    double h = connector.Height;
                    if (w <= 0 && adj.Width.HasValue) w = adj.Width.Value;
                    if (h <= 0 && adj.Height.HasValue) h = adj.Height.Value;

                    if (w > 0 && h > 0)
                    {
                        // Orientation check
                        Transform cs = connector.CoordinateSystem;
                        double finalW = w;
                        double finalH = h;
                        if (cs != null && Math.Abs(cs.BasisX.Z) > 0.7 && Math.Abs(dir.Z) < 0.7)
                        {
                            finalW = h;
                            finalH = w;
                        }

                        var pW = duct.get_Parameter(BuiltInParameter.RBS_CURVE_WIDTH_PARAM);
                        var pH = duct.get_Parameter(BuiltInParameter.RBS_CURVE_HEIGHT_PARAM);
                        if (pW != null && !pW.IsReadOnly) pW.Set(finalW);
                        if (pH != null && !pH.IsReadOnly) pH.Set(finalH);
                    }
                }
            }
            catch { }

            _doc.Regenerate();

            // Auto-connect fallback
            if (!connector.IsConnected && duct.ConnectorManager != null)
            {
                foreach (Connector dc in duct.ConnectorManager.Connectors)
                {
                    if (dc.Origin.DistanceTo(p0) < 0.1)
                    {
                        try { connector.ConnectTo(dc); } catch { }
                        break;
                    }
                }
            }

            return duct;
        }

        private ElementId GetDefaultDuctTypeId(ConnectorProfileType shape)
        {
            var types = new FilteredElementCollector(_doc)
                .OfClass(typeof(DuctType))
                .Cast<DuctType>()
                .ToList();

            if (types.Count == 0)
            {
                var catTypes = new FilteredElementCollector(_doc)
                    .OfCategory(BuiltInCategory.OST_DuctCurves)
                    .WhereElementIsElementType()
                    .ToList();
                return catTypes.Count > 0 ? catTypes[0].Id : ElementId.InvalidElementId;
            }

            // Match shape by Shape property (returns ConnectorProfileType)
            foreach (var dt in types)
            {
                if (dt.Shape == shape)
                    return dt.Id;
            }

            // Match by name
            foreach (var dt in types)
            {
                string name = (dt.Name ?? "").ToLower();
                string fam = (dt.FamilyName ?? "").ToLower();
                if (shape == ConnectorProfileType.Round && (name.Contains("round") || fam.Contains("round")))
                    return dt.Id;
                if (shape == ConnectorProfileType.Rectangular && (name.Contains("rect") || fam.Contains("rect") || name.Contains("mitered")))
                    return dt.Id;
                if (shape == ConnectorProfileType.Oval && (name.Contains("oval") || fam.Contains("oval")))
                    return dt.Id;
            }

            return types[0].Id;
        }

        private ElementId GetDuctSystemTypeId(Connector connector, Element elem)
        {
            if (connector.MEPSystem != null)
                return connector.MEPSystem.GetTypeId();

            if (elem != null)
            {
                var cmElem = GetConnectorManager(elem);
                if (cmElem != null && cmElem.Connectors != null)
                {
                    foreach (Connector c in cmElem.Connectors)
                    {
                        if (c.MEPSystem != null)
                            return c.MEPSystem.GetTypeId();
                    }
                }

                var pSys = elem.get_Parameter(BuiltInParameter.RBS_DUCT_SYSTEM_TYPE_PARAM);
                if (pSys != null && pSys.HasValue && pSys.AsElementId() != ElementId.InvalidElementId)
                    return pSys.AsElementId();
            }

            var existingDuct = new FilteredElementCollector(_doc)
                .OfClass(typeof(Duct))
                .WhereElementIsNotElementType()
                .Cast<Duct>()
                .FirstOrDefault(x => x.MEPSystem != null);
            if (existingDuct != null) return existingDuct.MEPSystem.GetTypeId();

            var mst = new FilteredElementCollector(_doc)
                .OfClass(typeof(MechanicalSystemType))
                .FirstOrDefault();
            if (mst != null) return mst.Id;

            return ElementId.InvalidElementId;
        }

        #endregion

        #region Cable Tray & Conduit Stubs

        public static bool IsCableTrayTarget(Element elem, Connector connector)
        {
            try
            {
                if (elem != null && elem.Category != null)
                {
                    int catId = elem.Category.Id.IntegerValue;
                    if (catId == (int)BuiltInCategory.OST_CableTray ||
                        catId == (int)BuiltInCategory.OST_CableTrayFitting)
                    {
                        return true;
                    }
                    if (catId == (int)BuiltInCategory.OST_Conduit ||
                        catId == (int)BuiltInCategory.OST_ConduitFitting)
                    {
                        return false;
                    }
                }
            }
            catch { }

            try
            {
                if (connector.Shape == ConnectorProfileType.Rectangular ||
                    connector.Shape == ConnectorProfileType.Oval)
                {
                    return true;
                }
                if (connector.Shape == ConnectorProfileType.Round)
                {
                    return false;
                }
            }
            catch { }

            try
            {
                if (connector.Width > 0) return true;
            }
            catch { }

            return false;
        }

        private CableTray BloomCableTrayConnector(Element elem, Connector connector)
        {
            XYZ p0 = connector.Origin;
            XYZ dir = GetOutwardVector(connector, elem);
            XYZ p1 = p0 + dir * _stubLengthFt;

            var adj = FindAdjacentMEPInfo(elem, Domain.DomainCableTrayConduit);
            ElementId trayTypeId = adj.TypeId != ElementId.InvalidElementId ? adj.TypeId : GetDefaultCableTrayTypeId();
            ElementId levelId = adj.LevelId != ElementId.InvalidElementId ? adj.LevelId : GetElementLevelId(elem, p0.Z);

            if (trayTypeId == ElementId.InvalidElementId || levelId == ElementId.InvalidElementId)
                return null;

            CableTray tray = null;
            try
            {
                tray = CableTray.Create(_doc, trayTypeId, p0, p1, levelId);
            }
            catch { }

            if (tray == null) return null;

            // Set Width and Height
            try
            {
                double w = connector.Width;
                double h = connector.Height;
                if (w <= 0 && adj.Width.HasValue) w = adj.Width.Value;
                if (h <= 0 && adj.Height.HasValue) h = adj.Height.Value;

                if (w > 0 && h > 0)
                {
                    Transform cs = connector.CoordinateSystem;
                    double finalW = w;
                    double finalH = h;
                    if (cs != null && Math.Abs(cs.BasisX.Z) > 0.7 && Math.Abs(dir.Z) < 0.7)
                    {
                        finalW = h;
                        finalH = w;
                    }

                    var pW = tray.get_Parameter(BuiltInParameter.RBS_CABLETRAY_WIDTH_PARAM) ??
                             tray.get_Parameter(BuiltInParameter.RBS_CURVE_WIDTH_PARAM);
                    if (pW != null && !pW.IsReadOnly) pW.Set(finalW);

                    var pH = tray.get_Parameter(BuiltInParameter.RBS_CABLETRAY_HEIGHT_PARAM) ??
                             tray.get_Parameter(BuiltInParameter.RBS_CURVE_HEIGHT_PARAM);
                    if (pH != null && !pH.IsReadOnly) pH.Set(finalH);
                }
            }
            catch { }

            _doc.Regenerate();

            // Auto-connect
            if (!connector.IsConnected && tray.ConnectorManager != null)
            {
                foreach (Connector tc in tray.ConnectorManager.Connectors)
                {
                    if (tc.Origin.DistanceTo(p0) < 0.1)
                    {
                        try { connector.ConnectTo(tc); } catch { }
                        break;
                    }
                }
            }

            return tray;
        }

        private ElementId GetDefaultCableTrayTypeId()
        {
            var ct = new FilteredElementCollector(_doc)
                .OfClass(typeof(CableTray))
                .WhereElementIsNotElementType()
                .Cast<CableTray>()
                .FirstOrDefault();
            if (ct != null) return ct.GetTypeId();

            var ctt = new FilteredElementCollector(_doc)
                .OfClass(typeof(CableTrayType))
                .FirstOrDefault();
            if (ctt != null) return ctt.Id;

            var cat = new FilteredElementCollector(_doc)
                .OfCategory(BuiltInCategory.OST_CableTray)
                .WhereElementIsElementType()
                .FirstOrDefault();
            return cat != null ? cat.Id : ElementId.InvalidElementId;
        }

        private Conduit BloomConduitConnector(Element elem, Connector connector)
        {
            XYZ p0 = connector.Origin;
            XYZ dir = GetOutwardVector(connector, elem);
            XYZ p1 = p0 + dir * _stubLengthFt;

            var adj = FindAdjacentMEPInfo(elem, Domain.DomainCableTrayConduit);
            ElementId conduitTypeId = adj.TypeId != ElementId.InvalidElementId ? adj.TypeId : GetDefaultConduitTypeId();
            ElementId levelId = adj.LevelId != ElementId.InvalidElementId ? adj.LevelId : GetElementLevelId(elem, p0.Z);

            if (conduitTypeId == ElementId.InvalidElementId || levelId == ElementId.InvalidElementId)
                return null;

            Conduit conduit = null;
            try
            {
                conduit = Conduit.Create(_doc, conduitTypeId, p0, p1, levelId);
            }
            catch { }

            if (conduit == null) return null;

            // Set Diameter
            try
            {
                double? diam = connector.Radius * 2.0;
                if (diam <= 0 && adj.Diameter.HasValue) diam = adj.Diameter.Value;

                if (diam.HasValue && diam.Value > 0)
                {
                    var pD = conduit.get_Parameter(BuiltInParameter.RBS_CONDUIT_DIAMETER_PARAM) ??
                             conduit.get_Parameter(BuiltInParameter.RBS_CURVE_DIAMETER_PARAM);
                    if (pD != null && !pD.IsReadOnly) pD.Set(diam.Value);
                }
            }
            catch { }

            _doc.Regenerate();

            // Auto-connect
            if (!connector.IsConnected && conduit.ConnectorManager != null)
            {
                foreach (Connector cc in conduit.ConnectorManager.Connectors)
                {
                    if (cc.Origin.DistanceTo(p0) < 0.1)
                    {
                        try { connector.ConnectTo(cc); } catch { }
                        break;
                    }
                }
            }

            return conduit;
        }

        private ElementId GetDefaultConduitTypeId()
        {
            var c = new FilteredElementCollector(_doc)
                .OfClass(typeof(Conduit))
                .WhereElementIsNotElementType()
                .Cast<Conduit>()
                .FirstOrDefault();
            if (c != null) return c.GetTypeId();

            var ct = new FilteredElementCollector(_doc)
                .OfClass(typeof(ConduitType))
                .FirstOrDefault();
            if (ct != null) return ct.Id;

            var cat = new FilteredElementCollector(_doc)
                .OfCategory(BuiltInCategory.OST_Conduit)
                .WhereElementIsElementType()
                .FirstOrDefault();
            return cat != null ? cat.Id : ElementId.InvalidElementId;
        }

        #endregion
    }
}
