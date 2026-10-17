using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;

namespace mepOS.Core.Engines
{
    public class ConnectToEngine
    {
        private readonly Document _doc;
        private readonly ConnectToConfig _config;

        public ConnectToEngine(Document doc, ConnectToConfig config = null)
        {
            if (doc == null) throw new ArgumentNullException("doc");
            _doc = doc;
            _config = config != null ? config : new ConnectToConfig();
        }

        public bool Connect(Element el1, XYZ pt1, Element el2, XYZ pt2, out string statusMessage)
        {
            statusMessage = string.Empty;
            if (el1 == null || el2 == null)
            {
                statusMessage = "Phần tử được chọn không hợp lệ.";
                return false;
            }

            if (el1.Id == el2.Id)
            {
                statusMessage = "Không thể nối một phần tử với chính nó.";
                return false;
            }

            // 1. Identify best pair of open connectors
            Connector c1;
            Connector c2;
            if (!GeometryUtils.GetBestConnectorPair(el1, pt1, el2, pt2, out c1, out c2) || c1 == null || c2 == null)
            {
                statusMessage = "Không tìm thấy đầu nối (connector) còn trống trên các phần tử đã chọn.";
                return false;
            }

            if (c1.Domain != c2.Domain)
            {
                statusMessage = "Không thể nối 2 hệ thống khác bộ môn (ví dụ Ống nước và Ống gió).";
                return false;
            }

            XYZ p1 = c1.Origin;
            XYZ p2 = c2.Origin;
            double dist = p1.DistanceTo(p2);

            XYZ d1 = c1.CoordinateSystem.BasisZ.Normalize();
            XYZ d2 = c2.CoordinateSystem.BasisZ.Normalize();
            double dotDir = d1.DotProduct(d2);

            // =========================================================================
            // TIER 1: DIRECT SNAP / TOUCHING (< 1.5 mm / 0.005 ft)
            // =========================================================================
            if (dist < 0.005)
            {
                if (GeometryUtils.JoinOrTransition(_doc, c1, c2, out statusMessage))
                {
                    return true;
                }
            }

            // =========================================================================
            // TIER 2: FACING / COLLINEAR RUNS (dotDir < -0.6)
            // Tự động căn chỉnh lệch trục (XY) và lệch cao độ (Z)
            // =========================================================================
            if (dotDir < -0.6)
            {
                XYZ v = p2 - p1;
                double proj = v.DotProduct(d1);
                XYZ pTargetOnLine1 = p1 + proj * d1;
                XYZ offsetVec = pTargetOnLine1 - p2; // Vector di chuyển p2 vào đúng trục của p1
                double offsetDist = offsetVec.GetLength();

                // Nếu có lệch trục (XY) hoặc lệch cao độ (Z) -> Tự động tịnh tiến phần tử tự do
                if (offsetDist > 0.003)
                {
                    Element moveElem = null;
                    XYZ trans = null;

                    if (GeometryUtils.IsElementStandalone(el2))
                    {
                        moveElem = el2;
                        trans = offsetVec;
                    }
                    else if (GeometryUtils.IsElementStandalone(el1))
                    {
                        moveElem = el1;
                        trans = -offsetVec;
                    }
                    else
                    {
                        // Mặc định di chuyển phần tử thứ 2
                        moveElem = el2;
                        trans = offsetVec;
                    }

                    try
                    {
                        ElementTransformUtils.MoveElement(_doc, moveElem.Id, trans);
                        _doc.Regenerate();

                        // Cập nhật lại vị trí connector sau khi di chuyển
                        if (GeometryUtils.GetBestConnectorPair(el1, pt1, el2, pt2, out c1, out c2) && c1 != null && c2 != null)
                        {
                            p1 = c1.Origin;
                            p2 = c2.Origin;
                            d1 = c1.CoordinateSystem.BasisZ.Normalize();
                            d2 = c2.CoordinateSystem.BasisZ.Normalize();
                            dist = p1.DistanceTo(p2);
                        }
                    }
                    catch { }
                }

                // 2A: Khớp trực tiếp nếu đã chạm vào nhau sau khi căn trục
                if (dist < 0.005)
                {
                    if (GeometryUtils.JoinOrTransition(_doc, c1, c2, out statusMessage))
                    {
                        statusMessage = "Căn thẳng trục & cao độ, khớp trực tiếp thành công.";
                        return true;
                    }
                }

                // 2B: Kéo dài el2 tới p1 (nếu el2 là MEPCurve thẳng)
                if (GeometryUtils.IsLinearCurve(el2))
                {
                    if (GeometryUtils.ExtendCurveToPoint(el2, p1, p2))
                    {
                        _doc.Regenerate();
                        Connector c2New = GeometryUtils.GetConnectorAtPoint(el2, p1, 0.15);
                        if (c2New != null && GeometryUtils.JoinOrTransition(_doc, c1, c2New, out statusMessage))
                        {
                            statusMessage = "Tự động căn trục/cao độ và kéo dài nối thẳng thành công.";
                            return true;
                        }
                    }
                }

                // 2C: Kéo dài el1 tới p2 (nếu el1 là MEPCurve thẳng)
                if (GeometryUtils.IsLinearCurve(el1))
                {
                    if (GeometryUtils.ExtendCurveToPoint(el1, p2, p1))
                    {
                        _doc.Regenerate();
                        Connector c1New = GeometryUtils.GetConnectorAtPoint(el1, p2, 0.15);
                        if (c1New != null && GeometryUtils.JoinOrTransition(_doc, c1New, c2, out statusMessage))
                        {
                            statusMessage = "Tự động căn trục/cao độ và kéo dài nối thẳng thành công.";
                            return true;
                        }
                    }
                }

                // 2D: Tạo đoạn ống/máng cầu nối nếu khoảng cách xa
                if (dist >= GeometryUtils.MinLineLengthFt)
                {
                    Element bridge = GeometryUtils.CreateBridgingMEPCurve(_doc, el1, c1, p1, p2);
                    if (bridge != null)
                    {
                        _doc.Regenerate();
                        Connector bC1 = GeometryUtils.GetConnectorAtPoint(bridge, p1, 0.15);
                        Connector bC2 = GeometryUtils.GetConnectorAtPoint(bridge, p2, 0.15);
                        if (bC1 != null && bC2 != null)
                        {
                            string msg1, msg2;
                            bool ok1 = GeometryUtils.JoinOrTransition(_doc, c1, bC1, out msg1);
                            bool ok2 = GeometryUtils.JoinOrTransition(_doc, bC2, c2, out msg2);
                            if (ok1 && ok2)
                            {
                                statusMessage = "Tạo đoạn ống/máng cầu nối thành công.";
                                return true;
                            }
                        }
                    }
                }
            }

            // =========================================================================
            // TIER 3: CORNER / ANGLED INTERSECTION (ELBOW FITTING)
            // Tự động đồng bộ cao độ Z nếu 2 tuyến bị lệch cao độ
            // =========================================================================
            double angleRad = d1.AngleTo(d2);
            double angleDeg = angleRad * 180.0 / Math.PI;

            if (angleDeg >= 15.0 && angleDeg <= 165.0)
            {
                // Tự động kiểm tra và đồng bộ cao độ Z (Elevation Sync)
                double dz = p1.Z - p2.Z;
                if (Math.Abs(dz) > 0.003)
                {
                    Element moveElem = null;
                    XYZ zTrans = null;

                    if (GeometryUtils.IsElementStandalone(el2))
                    {
                        moveElem = el2;
                        zTrans = new XYZ(0, 0, dz);
                    }
                    else if (GeometryUtils.IsElementStandalone(el1))
                    {
                        moveElem = el1;
                        zTrans = new XYZ(0, 0, -dz);
                    }
                    else
                    {
                        moveElem = el2;
                        zTrans = new XYZ(0, 0, dz);
                    }

                    try
                    {
                        ElementTransformUtils.MoveElement(_doc, moveElem.Id, zTrans);
                        _doc.Regenerate();

                        // Cập nhật lại vị trí connector sau khi đồng bộ cao độ
                        if (GeometryUtils.GetBestConnectorPair(el1, pt1, el2, pt2, out c1, out c2) && c1 != null && c2 != null)
                        {
                            p1 = c1.Origin;
                            p2 = c2.Origin;
                            d1 = c1.CoordinateSystem.BasisZ.Normalize();
                            d2 = c2.CoordinateSystem.BasisZ.Normalize();
                        }
                    }
                    catch { }
                }

                // Tính toán giao điểm 3D sau khi đã cùng mặt phẳng cao độ
                XYZ pInt;
                double lineDist;
                if (GeometryUtils.CalculateLineIntersection3D(p1, d1, p2, d2, 0.25, out pInt, out lineDist) && lineDist <= 0.25)
                {
                    XYZ v1ToInt = pInt - p1;
                    XYZ v2ToInt = pInt - p2;
                    double proj1 = v1ToInt.DotProduct(d1);
                    double proj2 = v2ToInt.DotProduct(d2);

                    if (proj1 > -0.05 && proj2 > -0.05)
                    {
                        bool extended1 = GeometryUtils.IsLinearCurve(el1) ? GeometryUtils.ExtendCurveToPoint(el1, pInt, p1) : false;
                        bool extended2 = GeometryUtils.IsLinearCurve(el2) ? GeometryUtils.ExtendCurveToPoint(el2, pInt, p2) : false;

                        if (extended1 || extended2 || (p1.DistanceTo(pInt) < 0.01 && p2.DistanceTo(pInt) < 0.01))
                        {
                            _doc.Regenerate();
                            Connector c1Int = GeometryUtils.GetConnectorAtPoint(el1, pInt, 0.15);
                            Connector c2Int = GeometryUtils.GetConnectorAtPoint(el2, pInt, 0.15);

                            if (c1Int != null && c2Int != null)
                            {
                                try
                                {
                                    FamilyInstance elbow = _doc.Create.NewElbowFitting(c1Int, c2Int);
                                    if (elbow != null)
                                    {
                                        statusMessage = string.Format("Đồng bộ cao độ & nối Co (Elbow {0:F1}°) thành công.", angleDeg);
                                        return true;
                                    }
                                }
                                catch { }

                                try
                                {
                                    FamilyInstance elbow = _doc.Create.NewElbowFitting(c2Int, c1Int);
                                    if (elbow != null)
                                    {
                                        statusMessage = string.Format("Đồng bộ cao độ & nối Co (Elbow {0:F1}°) thành công.", angleDeg);
                                        return true;
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                }
            }

            statusMessage = "Không thể tìm tuyến nối phù hợp. Vui lòng kiểm tra lại góc giao hoặc Routing Preferences của hệ thống.";
            return false;
        }
    }
}
