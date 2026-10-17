using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;

namespace mepOS.Core.Engines
{
    public static class GeometryUtils
    {
        public const double MmToFt = 1.0 / 304.8;
        public const double FtToMm = 304.8;
        public const double MinLineLengthFt = 0.015; // ~4.5 mm

        public static double ToFeet(double mm)
        {
            return mm * MmToFt;
        }

        public static double ToMm(double feet)
        {
            return feet * FtToMm;
        }

        public static ConnectorManager GetConnectorManager(Element element)
        {
            if (element == null) return null;

            var mepCurve = element as MEPCurve;
            if (mepCurve != null)
            {
                return mepCurve.ConnectorManager;
            }

            var fi = element as FamilyInstance;
            if (fi != null && fi.MEPModel != null)
            {
                return fi.MEPModel.ConnectorManager;
            }

            return null;
        }

        public static List<Connector> GetOpenConnectors(Element element)
        {
            var list = new List<Connector>();
            if (element == null) return list;

            ConnectorManager manager = GetConnectorManager(element);
            if (manager == null) return list;

            foreach (Connector c in manager.Connectors)
            {
                if (c != null && !c.IsConnected)
                {
                    list.Add(c);
                }
            }
            return list;
        }

        public static Connector GetConnectorAtPoint(Element elem, XYZ targetPt, double tolerance)
        {
            ConnectorManager cm = GetConnectorManager(elem);
            if (cm == null) return null;

            foreach (Connector c in cm.Connectors)
            {
                if (c != null && c.Origin.DistanceTo(targetPt) < tolerance)
                {
                    return c;
                }
            }
            return null;
        }

        public static bool IsLinearCurve(Element elem)
        {
            if (elem == null) return false;
            var locCurve = elem.Location as LocationCurve;
            if (locCurve != null && locCurve.Curve is Line)
            {
                return true;
            }
            return false;
        }

        public static Line GetLocationLine(Element elem)
        {
            if (elem == null) return null;
            var locCurve = elem.Location as LocationCurve;
            if (locCurve != null)
            {
                return locCurve.Curve as Line;
            }
            return null;
        }

        public static bool ExtendCurveToPoint(Element curveElem, XYZ targetPt, XYZ whichEndPt)
        {
            if (!IsLinearCurve(curveElem)) return false;

            var locCurve = curveElem.Location as LocationCurve;
            if (locCurve == null) return false;

            var line = locCurve.Curve as Line;
            if (line == null) return false;

            XYZ pt0 = line.GetEndPoint(0);
            XYZ pt1 = line.GetEndPoint(1);

            if (pt0.DistanceTo(targetPt) < 0.003 || pt1.DistanceTo(targetPt) < 0.003)
            {
                return true;
            }

            try
            {
                Line newLine;
                if (pt0.DistanceTo(whichEndPt) < pt1.DistanceTo(whichEndPt))
                {
                    if (targetPt.DistanceTo(pt1) < 0.005) return false;
                    newLine = Line.CreateBound(targetPt, pt1);
                }
                else
                {
                    if (pt0.DistanceTo(targetPt) < 0.005) return false;
                    newLine = Line.CreateBound(pt0, targetPt);
                }
                locCurve.Curve = newLine;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool CalculateLineIntersection3D(XYZ p1, XYZ dir1, XYZ p2, XYZ dir2, double tolerance, out XYZ pInt, out double distBetweenLines)
        {
            pInt = null;
            distBetweenLines = double.PositiveInfinity;

            XYZ u1 = dir1.Normalize();
            XYZ u2 = dir2.Normalize();

            XYZ w0 = p1 - p2;
            double a = u1.DotProduct(u1);
            double b = u1.DotProduct(u2);
            double c = u2.DotProduct(u2);
            double d = u1.DotProduct(w0);
            double e = u2.DotProduct(w0);

            double denom = a * c - b * b;
            if (Math.Abs(denom) < 1e-5)
            {
                return false; // Lines are parallel
            }

            double s = (b * e - c * d) / denom;
            double t = (a * e - b * d) / denom;

            XYZ q1 = p1 + u1 * s;
            XYZ q2 = p2 + u2 * t;
            distBetweenLines = q1.DistanceTo(q2);

            if (distBetweenLines <= tolerance)
            {
                pInt = (q1 + q2) * 0.5;
                return true;
            }
            return false;
        }

        public static bool GetBestConnectorPair(Element el1, XYZ pt1, Element el2, XYZ pt2, out Connector best1, out Connector best2)
        {
            best1 = null;
            best2 = null;

            List<Connector> open1 = GetOpenConnectors(el1);
            List<Connector> open2 = GetOpenConnectors(el2);

            if (open1.Count == 0 || open2.Count == 0) return false;

            if (open1.Count == 1 && open2.Count == 1)
            {
                best1 = open1[0];
                best2 = open2[0];
                return true;
            }

            double bestScore = double.MaxValue;

            for (int i = 0; i < open1.Count; i++)
            {
                Connector c1 = open1[i];
                XYZ p1 = c1.Origin;
                XYZ d1 = c1.CoordinateSystem.BasisZ;

                for (int j = 0; j < open2.Count; j++)
                {
                    Connector c2 = open2[j];
                    XYZ p2 = c2.Origin;
                    XYZ d2 = c2.CoordinateSystem.BasisZ;

                    double dist = p1.DistanceTo(p2);
                    double dotDir = d1.DotProduct(d2);

                    double pickDist = 0.0;
                    if (pt1 != null) pickDist += p1.DistanceTo(pt1);
                    if (pt2 != null) pickDist += p2.DistanceTo(pt2);

                    bool isViable = false;

                    // Case A: Touching
                    if (dist < 0.01)
                    {
                        isViable = true;
                    }
                    else
                    {
                        XYZ v = p2 - p1;

                        // Case B: Collinear / facing (allowing lateral and elevation offsets)
                        if (dotDir < -0.6)
                        {
                            if (dist < 0.1 || v.DotProduct(d1) > -0.1)
                            {
                                isViable = true;
                            }
                        }
                        // Case C: Corner elbow intersection (allowing elevation differences)
                        else if (Math.Abs(dotDir) <= 0.96)
                        {
                            double angleDeg = d1.AngleTo(d2) * 180.0 / Math.PI;
                            if (angleDeg >= 15.0 && angleDeg <= 165.0)
                            {
                                XYZ d1_2d = new XYZ(d1.X, d1.Y, 0);
                                XYZ d2_2d = new XYZ(d2.X, d2.Y, 0);
                                if (d1_2d.GetLength() > 0.3 && d2_2d.GetLength() > 0.3)
                                {
                                    d1_2d = d1_2d.Normalize();
                                    d2_2d = d2_2d.Normalize();
                                    XYZ pInt2d;
                                    double lineDist2d;
                                    if (CalculateLineIntersection3D(new XYZ(p1.X, p1.Y, 0), d1_2d, new XYZ(p2.X, p2.Y, 0), d2_2d, 0.05, out pInt2d, out lineDist2d))
                                    {
                                        if ((pInt2d - new XYZ(p1.X, p1.Y, 0)).DotProduct(d1_2d) > -0.1 &&
                                            (pInt2d - new XYZ(p2.X, p2.Y, 0)).DotProduct(d2_2d) > -0.1)
                                        {
                                            isViable = true;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    double score = dist + 0.2 * pickDist;
                    if (isViable) score -= 1000.0;

                    if (score < bestScore)
                    {
                        bestScore = score;
                        best1 = c1;
                        best2 = c2;
                    }
                }
            }

            if (best1 == null) best1 = open1[0];
            if (best2 == null) best2 = open2[0];
            return true;
        }

        public static bool IsElementStandalone(Element elem)
        {
            if (elem == null) return false;
            ConnectorManager cm = GetConnectorManager(elem);
            if (cm == null) return false;

            try
            {
                foreach (Connector c in cm.Connectors)
                {
                    if (c != null && c.IsConnected) return false;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool JoinOrTransition(Document doc, Connector c1, Connector c2, out string message)
        {
            try
            {
                c1.ConnectTo(c2);
                message = "Kết nối trực tiếp thành công.";
                return true;
            }
            catch { }

            try
            {
                c2.ConnectTo(c1);
                message = "Kết nối trực tiếp thành công.";
                return true;
            }
            catch { }

            try
            {
                FamilyInstance union = doc.Create.NewUnionFitting(c1, c2);
                if (union != null)
                {
                    message = "Kết nối thành công qua Union fitting.";
                    return true;
                }
            }
            catch { }

            try
            {
                FamilyInstance union = doc.Create.NewUnionFitting(c2, c1);
                if (union != null)
                {
                    message = "Kết nối thành công qua Union fitting.";
                    return true;
                }
            }
            catch { }

            try
            {
                FamilyInstance fitting = doc.Create.NewTransitionFitting(c1, c2);
                if (fitting != null)
                {
                    message = "Kết nối thành công qua Transition fitting.";
                    return true;
                }
            }
            catch { }

            try
            {
                FamilyInstance fitting = doc.Create.NewTransitionFitting(c2, c1);
                if (fitting != null)
                {
                    message = "Kết nối thành công qua Transition fitting.";
                    return true;
                }
            }
            catch { }

            message = "Không thể khớp trực tiếp hoặc chèn phụ kiện chuyển đổi.";
            return false;
        }

        public static ElementId GetElementLevelId(Document doc, Element elem, double z)
        {
            if (elem != null)
            {
                Parameter p = elem.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM);
                if (p != null && p.AsElementId() != ElementId.InvalidElementId)
                    return p.AsElementId();

                p = elem.get_Parameter(BuiltInParameter.LEVEL_PARAM);
                if (p != null && p.AsElementId() != ElementId.InvalidElementId)
                    return p.AsElementId();

                if (elem.LevelId != ElementId.InvalidElementId)
                    return elem.LevelId;
            }

            // Find closest level by Z
            var collector = new FilteredElementCollector(doc).OfClass(typeof(Level));
            Level closest = null;
            double minDiff = double.MaxValue;
            foreach (Level lvl in collector)
            {
                double diff = Math.Abs(lvl.Elevation - z);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    closest = lvl;
                }
            }
            return closest != null ? closest.Id : ElementId.InvalidElementId;
        }

        public static Element CreateBridgingMEPCurve(Document doc, Element refElem, Connector refConn, XYZ pStart, XYZ pEnd)
        {
            if (doc == null || refConn == null) return null;

            double midZ = (pStart.Z + pEnd.Z) * 0.5;
            ElementId levelId = GetElementLevelId(doc, refElem, midZ);
            Domain domain = refConn.Domain;

            // 1. CableTray / Conduit
            if (domain == Domain.DomainCableTrayConduit)
            {
                bool isCableTray = refElem is CableTray || (refConn.Shape == ConnectorProfileType.Rectangular);
                if (isCableTray)
                {
                    ElementId typeId = refElem is CableTray ? refElem.GetTypeId() : GetDefaultCableTrayTypeId(doc);
                    if (typeId != ElementId.InvalidElementId)
                    {
                        CableTray tray = CableTray.Create(doc, typeId, pStart, pEnd, levelId);
                        if (tray != null)
                        {
                            try
                            {
                                Parameter pw = tray.get_Parameter(BuiltInParameter.RBS_CABLETRAY_WIDTH_PARAM);
                                if (pw != null && !pw.IsReadOnly && refConn.Width > 0) pw.Set(refConn.Width);
                                Parameter ph = tray.get_Parameter(BuiltInParameter.RBS_CABLETRAY_HEIGHT_PARAM);
                                if (ph != null && !ph.IsReadOnly && refConn.Height > 0) ph.Set(refConn.Height);
                            }
                            catch { }
                            return tray;
                        }
                    }
                }
                else
                {
                    ElementId typeId = refElem is Conduit ? refElem.GetTypeId() : GetDefaultConduitTypeId(doc);
                    if (typeId != ElementId.InvalidElementId)
                    {
                        Conduit conduit = Conduit.Create(doc, typeId, pStart, pEnd, levelId);
                        if (conduit != null)
                        {
                            try
                            {
                                Parameter pd = conduit.get_Parameter(BuiltInParameter.RBS_CONDUIT_DIAMETER_PARAM);
                                if (pd != null && !pd.IsReadOnly)
                                {
                                    double diam = refConn.Radius * 2.0;
                                    if (diam > 0) pd.Set(diam);
                                }
                            }
                            catch { }
                            return conduit;
                        }
                    }
                }
            }
            // 2. Piping
            else if (domain == Domain.DomainPiping)
            {
                ElementId sysId = GetPipingSystemTypeId(doc, refConn);
                ElementId typeId = refElem is Pipe ? refElem.GetTypeId() : GetDefaultPipeTypeId(doc);
                if (sysId != ElementId.InvalidElementId && typeId != ElementId.InvalidElementId)
                {
                    Pipe pipe = Pipe.Create(doc, sysId, typeId, levelId, pStart, pEnd);
                    if (pipe != null)
                    {
                        try
                        {
                            Parameter pd = pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
                            if (pd != null && !pd.IsReadOnly)
                            {
                                double diam = refConn.Radius * 2.0;
                                if (diam > 0) pd.Set(diam);
                            }
                        }
                        catch { }
                        return pipe;
                    }
                }
            }
            // 3. Ducts
            else if (domain == Domain.DomainHvac)
            {
                ElementId sysId = GetDuctSystemTypeId(doc, refConn);
                ElementId typeId = refElem is Duct ? refElem.GetTypeId() : GetDefaultDuctTypeId(doc);
                if (sysId != ElementId.InvalidElementId && typeId != ElementId.InvalidElementId)
                {
                    Duct duct = Duct.Create(doc, sysId, typeId, levelId, pStart, pEnd);
                    if (duct != null)
                    {
                        try
                        {
                            if (refConn.Shape == ConnectorProfileType.Round)
                            {
                                Parameter pd = duct.get_Parameter(BuiltInParameter.RBS_CURVE_DIAMETER_PARAM);
                                if (pd != null && !pd.IsReadOnly)
                                {
                                    double diam = refConn.Radius * 2.0;
                                    if (diam > 0) pd.Set(diam);
                                }
                            }
                            else
                            {
                                Parameter pw = duct.get_Parameter(BuiltInParameter.RBS_CURVE_WIDTH_PARAM);
                                if (pw != null && !pw.IsReadOnly && refConn.Width > 0) pw.Set(refConn.Width);
                                Parameter ph = duct.get_Parameter(BuiltInParameter.RBS_CURVE_HEIGHT_PARAM);
                                if (ph != null && !ph.IsReadOnly && refConn.Height > 0) ph.Set(refConn.Height);
                            }
                        }
                        catch { }
                        return duct;
                    }
                }
            }

            return null;
        }

        private static ElementId GetDefaultCableTrayTypeId(Document doc)
        {
            var col = new FilteredElementCollector(doc).OfClass(typeof(CableTrayType));
            var first = col.FirstElement();
            return first != null ? first.Id : ElementId.InvalidElementId;
        }

        private static ElementId GetDefaultConduitTypeId(Document doc)
        {
            var col = new FilteredElementCollector(doc).OfClass(typeof(ConduitType));
            var first = col.FirstElement();
            return first != null ? first.Id : ElementId.InvalidElementId;
        }

        private static ElementId GetDefaultPipeTypeId(Document doc)
        {
            var col = new FilteredElementCollector(doc).OfClass(typeof(PipeType));
            var first = col.FirstElement();
            return first != null ? first.Id : ElementId.InvalidElementId;
        }

        private static ElementId GetDefaultDuctTypeId(Document doc)
        {
            var col = new FilteredElementCollector(doc).OfClass(typeof(DuctType));
            var first = col.FirstElement();
            return first != null ? first.Id : ElementId.InvalidElementId;
        }

        private static ElementId GetPipingSystemTypeId(Document doc, Connector conn)
        {
            if (conn != null && conn.MEPSystem != null)
            {
                return conn.MEPSystem.GetTypeId();
            }
            var col = new FilteredElementCollector(doc).OfClass(typeof(MEPSystemType));
            foreach (MEPSystemType st in col)
            {
                if (st.SystemClassification == MEPSystemClassification.OtherPipe ||
                    st.SystemClassification == MEPSystemClassification.Sanitary ||
                    st.SystemClassification == MEPSystemClassification.DomesticColdWater ||
                    st.SystemClassification == MEPSystemClassification.SupplyHydronic)
                {
                    return st.Id;
                }
            }
            var first = col.FirstElement();
            return first != null ? first.Id : ElementId.InvalidElementId;
        }

        private static ElementId GetDuctSystemTypeId(Document doc, Connector conn)
        {
            if (conn != null && conn.MEPSystem != null)
            {
                return conn.MEPSystem.GetTypeId();
            }
            var col = new FilteredElementCollector(doc).OfClass(typeof(MEPSystemType));
            foreach (MEPSystemType st in col)
            {
                if (st.SystemClassification == MEPSystemClassification.SupplyAir ||
                    st.SystemClassification == MEPSystemClassification.ReturnAir ||
                    st.SystemClassification == MEPSystemClassification.ExhaustAir)
                {
                    return st.Id;
                }
            }
            var first = col.FirstElement();
            return first != null ? first.Id : ElementId.InvalidElementId;
        }
    }
}
