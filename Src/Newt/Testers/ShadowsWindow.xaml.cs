using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

using Game.HelperClasses;
using Game.Newt.AsteroidMiner2;
using Game.Newt.AsteroidMiner2.ShipParts;
using Game.Newt.AsteroidMiner2.ShipEditor;
using Game.Newt.HelperClasses;
using Game.Newt.HelperClasses.Clipper;
using Game.Newt.HelperClasses.Primitives3D;
using Game.Newt.NewtonDynamics;

namespace Game.Newt.Testers
{
    public partial class ShadowsWindow : Window
    {
        #region Class: TriangleVisual

        private class TriangleVisual
        {
            public TriangleVisual(ITriangle triangle, SolidColorBrush diffuseBrush, SolidColorBrush specularBrush)
            {
                this.Triangle = triangle;
                _diffuseBrush = diffuseBrush;
                _specularBrush = specularBrush;
            }

            public readonly ITriangle Triangle;
            private readonly SolidColorBrush _diffuseBrush;
            private readonly SolidColorBrush _specularBrush;

            public Color Color
            {
                get
                {
                    return _diffuseBrush.Color;
                }
                set
                {
                    _diffuseBrush.Color = value;
                    _specularBrush.Color = UtilityWPF.AlphaBlend(value, Colors.White, .5d);
                }
            }
        }

        #endregion
        #region Class: HullTriangleIntersect

        private static class HullTriangleIntersect
        {
            //NOTE: This can return null
            public static Point3D[] GetIntersection(ITriangleIndexed[] hull, ITriangle triangle)
            {
                //TODO: It's possible that the test triangle is touching the hull, but not really intersecting with it.  If that happens, then only
                // one or two points will be shared, but no real intersection

                // See if any of the points are inside
                int[] insidePoints = GetInsidePoints(hull, triangle);

                Point3D[] retVal = null;

                if (insidePoints.Length == 0)
                {
                    // No points are inside the hull, but edges may punch all the way through
                    retVal = AllOutside(hull, triangle);
                }
                else if (insidePoints.Length == 1)
                {
                    // One point is inside the hull.  See where its edges intersect the hull
                    retVal = OneInside(hull, triangle, insidePoints[0]);
                }
                else if (insidePoints.Length == 2)
                {
                    // Two points are inside the hull.  See where their edges intersect the hull
                    retVal = TwoInside(hull, triangle, insidePoints[0], insidePoints[1]);
                }
                else if (insidePoints.Length == 3)
                {
                    // The triangle is completely inside the hull
                    retVal = new Point3D[] { triangle.Point0, triangle.Point1, triangle.Point2 };
                }
                else
                {
                    throw new ApplicationException("");
                }

                if (retVal != null)
                {
                    //TODO: Make sure the returned polygon's normal is the same direction as the triangle passed in
                }

                // Exit Function
                return retVal;
            }

            #region Private Methods

            private static int[] GetInsidePoints(ITriangleIndexed[] hull, ITriangle triangle)
            {
                List<int> retVal = new List<int>();       // the values will be 0,1,2 corresponding to the point0,point1,point2

                for (int cntr = 0; cntr < 3; cntr++)
                {
                    if (Math3D.IsInside_ConvexHull(hull, GetTrianglePoint(triangle, cntr)))
                    {
                        retVal.Add(cntr);
                    }
                }

                return retVal.ToArray();
            }

            private static Point3D[] AllOutside(ITriangleIndexed[] hull, ITriangle triangle)
            {
                return null;
            }

            private static Point3D[] OneInside(ITriangleIndexed[] hull, ITriangle triangle, int insidePointIndex)
            {
                #region Examine Index

                TriangleEdge edge1, edge2, edgeOutside;       // Edges that are intersecting the hull
                Point3D insidePoint, outsideCenterPoint;

                switch (insidePointIndex)
                {
                    case 0:
                        edge1 = TriangleEdge.Edge_01;
                        edge2 = TriangleEdge.Edge_20;
                        edgeOutside = TriangleEdge.Edge_12;
                        insidePoint = triangle.Point0;
                        outsideCenterPoint = triangle.GetEdgeMidpoint(edgeOutside);
                        break;

                    case 1:
                        edge1 = TriangleEdge.Edge_01;
                        edge2 = TriangleEdge.Edge_12;
                        edgeOutside = TriangleEdge.Edge_20;
                        insidePoint = triangle.Point1;
                        outsideCenterPoint = triangle.GetEdgeMidpoint(edgeOutside);
                        break;

                    case 2:
                        edge1 = TriangleEdge.Edge_12;
                        edge2 = TriangleEdge.Edge_20;
                        edgeOutside = TriangleEdge.Edge_01;
                        insidePoint = triangle.Point2;
                        outsideCenterPoint = triangle.GetEdgeMidpoint(edgeOutside);
                        break;

                    default:
                        throw new ApplicationException("Unexpected point: " + insidePointIndex.ToString());
                }

                // Make sure that outsideCenterPoint is outside the hull
                outsideCenterPoint = EnsureOutsidePointIsBeyondHull(hull[0].AllPoints, insidePoint, outsideCenterPoint);

                #endregion

                // Get edge/hull intersections
                var intersect1 = GetIntersectPoints(hull, triangle.GetPoint(edge1, true), triangle.GetPoint(edge1, false));
                if (intersect1.Length == 0)
                {
                    return null;        // triangle is only touching the hull, not really intersecting
                }
                else if (intersect1.Length > 1)
                {
                    throw new ApplicationException("Should never get more than one intersect point: " + intersect1.Length.ToString());
                }

                var intersect2 = GetIntersectPoints(hull, triangle.GetPoint(edge2, true), triangle.GetPoint(edge2, false));
                if (intersect2.Length == 0)
                {
                    return null;
                }
                else if (intersect2.Length > 1)
                {
                    throw new ApplicationException("Should never get more than one intersect point: " + intersect2.Length.ToString());
                }

                // Now that the two intersect points are found, find connecting lines
                Point3D[] hullEdge = GetConnectingLines(hull, intersect1[0], intersect2[0], outsideCenterPoint);

                // Clip poly
                List<Point3D> hullPoly = new List<Point3D>();
                hullPoly.Add(insidePoint);
                hullPoly.AddRange(hullEdge);
                Point3D[] retVal = Math2D.GetIntersection_Polygon_Triangle(hullPoly.ToArray(), triangle);

                // Exit Function
                return retVal;
            }

            private static Point3D[] TwoInside(ITriangleIndexed[] hull, ITriangle triangle, int insidePointIndex1, int insidePointIndex2)
            {
                if (insidePointIndex1 > insidePointIndex2)
                {
                    // Reverse them to simplify the switch statements
                    return TwoInside(hull, triangle, insidePointIndex2, insidePointIndex1);
                }

                #region Examine Indices

                TriangleEdge edgeLeft, edgeRight;
                Point3D insidePointLeft, insidePointRight, insidePointMiddle, outsideCenterPoint;

                switch (insidePointIndex1)
                {
                    case 0:
                        switch (insidePointIndex2)
                        {
                            case 1:
                                #region 0, 1

                                edgeLeft = TriangleEdge.Edge_20;
                                edgeRight = TriangleEdge.Edge_12;

                                insidePointLeft = triangle.Point0;
                                insidePointRight = triangle.Point1;
                                insidePointMiddle = triangle.GetEdgeMidpoint(TriangleEdge.Edge_01);
                                outsideCenterPoint = triangle.Point2;

                                #endregion
                                break;

                            case 2:
                                #region 0, 2

                                edgeLeft = TriangleEdge.Edge_01;
                                edgeRight = TriangleEdge.Edge_12;

                                insidePointLeft = triangle.Point0;
                                insidePointRight = triangle.Point2;
                                insidePointMiddle = triangle.GetEdgeMidpoint(TriangleEdge.Edge_20);
                                outsideCenterPoint = triangle.Point1;

                                #endregion
                                break;

                            default:
                                throw new ApplicationException("Unexpected point: " + insidePointIndex2.ToString());
                        }
                        break;

                    case 1:
                        #region 1, 2

                        if (insidePointIndex2 != 2)
                        {
                            throw new ApplicationException("Unexpected point: " + insidePointIndex2.ToString());
                        }

                        edgeLeft = TriangleEdge.Edge_01;
                        edgeRight = TriangleEdge.Edge_20;

                        insidePointLeft = triangle.Point1;
                        insidePointRight = triangle.Point2;
                        insidePointMiddle = triangle.GetEdgeMidpoint(TriangleEdge.Edge_12);
                        outsideCenterPoint = triangle.Point0;

                        #endregion
                        break;

                    default:
                        throw new ApplicationException("Unexpected point: " + insidePointIndex2.ToString());
                }

                // Make sure that outsideCenterPoint is outside the hull
                outsideCenterPoint = EnsureOutsidePointIsBeyondHull(hull[0].AllPoints, insidePointMiddle, outsideCenterPoint);

                #endregion

                // Get edge/hull intersections
                var intersectLeft = GetIntersectPoints(hull, triangle.GetPoint(edgeLeft, true), triangle.GetPoint(edgeLeft, false));
                if (intersectLeft.Length == 0)
                {
                    return null;        // triangle is only touching the hull, not really intersecting
                }

                var intersectRight = GetIntersectPoints(hull, triangle.GetPoint(edgeRight, true), triangle.GetPoint(edgeRight, false));
                if (intersectRight.Length == 0)
                {
                    return null;
                }

                // Now that the two intersect points are found, find connecting lines
                Point3D[] hullEdge = GetConnectingLines(hull, intersectLeft[0], intersectRight[0], outsideCenterPoint);

                // Clip poly
                List<Point3D> hullPoly = new List<Point3D>();
                hullPoly.Add(insidePointLeft);
                hullPoly.AddRange(hullEdge);
                hullPoly.Add(insidePointRight);
                Point3D[] retVal = Math2D.GetIntersection_Polygon_Triangle(hullPoly.ToArray(), triangle);

                // Exit Function
                return retVal;
            }

            private static Tuple<int, Point3D>[] GetIntersectPoints(ITriangleIndexed[] hull, Point3D point1, Point3D point2)
            {
                List<Tuple<int, Point3D>> retVal = new List<Tuple<int, Point3D>>();

                for (int cntr = 0; cntr < hull.Length; cntr++)
                {
                    Point3D? point = Math3D.GetIntersection_Triangle_LineSegment(hull[cntr], point1, point2);

                    if (point != null && !Math3D.IsNearValue(point.Value, point1) && !Math3D.IsNearValue(point.Value, point2))
                    {
                        retVal.Add(Tuple.Create(cntr, point.Value));
                    }
                }

                return retVal.ToArray();
            }

            private static Point3D[] GetConnectingLines(ITriangleIndexed[] hull, Tuple<int, Point3D> intersect1, Tuple<int, Point3D> intersect2, Point3D outsideCenterPoint)
            {
                if (intersect1.Item1 == intersect2.Item1)
                {
                    // They are intersecting the same triangle.  Just return a line between them
                    return new Point3D[] { intersect1.Item2, intersect2.Item2 };
                }

                // Get a line from intersect1 to the edge of its triangle
                var edgeIntersect1 = GetPointOnTriangleEdge(hull[intersect1.Item1], intersect1.Item2, outsideCenterPoint);

                // Get a line from intersect2 to the edge of its triangle
                var edgeIntersect2 = GetPointOnTriangleEdge(hull[intersect2.Item1], intersect2.Item2, outsideCenterPoint);

                if (edgeIntersect1 == null && edgeIntersect2 == null)
                {
                    // This should never happen.  The only thing I can think of is intersect1 and 2 are intersecting 2 triangles, but right on their edge
                    return new Point3D[] { intersect1.Item2, intersect2.Item2 };
                }
                else if (edgeIntersect1 != null && edgeIntersect2 == null)
                {
                    return new Point3D[] { intersect1.Item2, edgeIntersect1.Item2, intersect2.Item2 };
                }
                else if (edgeIntersect1 == null && edgeIntersect2 != null)
                {
                    return new Point3D[] { intersect1.Item2, edgeIntersect2.Item2, intersect2.Item2 };
                }

                // edgeIntersect1 and 2 are both nonnull at this point

                if (Math3D.IsNearValue(edgeIntersect1.Item2, edgeIntersect2.Item2))
                {
                    // intersect1 and 2 are intersecting neighboring triangles, so edgeIntersect1 and 2 are the same point
                    return new Point3D[] { intersect1.Item2, edgeIntersect1.Item2, intersect2.Item2 };
                }

                // If execution gets here, there are triangles between the two intersected triangles passed in

                // Convert the hull into linked triangles
                TriangleIndexedLinked[] hullLinked = hull.Select(o => new TriangleIndexedLinked(o.Index0, o.Index1, o.Index2, o.AllPoints)).ToArray();
                TriangleIndexedLinked.LinkTriangles_Edges(hullLinked.ToList(), true);

                return GetConnectingLinesSprtBetween(hullLinked, intersect1, intersect2, edgeIntersect1, edgeIntersect2);
            }
            private static Point3D[] GetConnectingLinesSprtBetween(TriangleIndexedLinked[] hull, Tuple<int, Point3D> intersect1, Tuple<int, Point3D> intersect2, Tuple<TriangleEdge, Point3D> edgeIntersect1, Tuple<TriangleEdge, Point3D> edgeIntersect2)
            {
                // Start building the return points
                List<Point3D> retVal = new List<Point3D>();

                retVal.Add(intersect1.Item2);
                retVal.Add(edgeIntersect1.Item2);

                // Walk neighbors until intersect2's triangle
                //TODO: To reduce mathmatical drift, go right one triangle from 1, then left one from 2, then right, etc and meet in the middle.  But the
                //drift should be very small, and the polygon clip logic that's done by the caller will transform into/out of 2D anyway.

                //TODO: If the line goes exactly through a corner, this loop could fail

                TriangleIndexedLinked currentTriangle = hull[intersect1.Item1];
                Tuple<TriangleEdge, Point3D> currentIntersect = edgeIntersect1;

                while (true)
                {
                    currentTriangle = currentTriangle.GetNeighbor(currentIntersect.Item1);
                    if (currentTriangle == null)
                    {
                        //TODO: Instead of choking here, go left from 2 as far as possible.  Then just draw a line from the farthest points (jumping
                        //over the hole)
                        throw new ApplicationException("The hull passed in has holes");
                    }

                    if (currentTriangle.Token == hull[intersect2.Item1].Token)
                    {
                        // This is the other side
                        break;
                    }

                    //NOTE: There are some cases where intersect2 will lead backward.  So an extra check is needed to make sure it's going
                    //away from prevTriangle
                    var attemptIntersect = GetPointOnTriangleEdge(currentTriangle, currentIntersect.Item2, intersect2.Item2);       // not using outsideCenterPoint, because that's only some midpoint (between intersect1 and 2).  Since this while loop is marching toward intersect2, then intersect2 is the best outside point to use
                    if (attemptIntersect == null)
                    {
                        #region Reverse guidePoint

                        // This is a copy of what GetPointOnTriangleEdge does
                        Vector3D guideDirection = (intersect2.Item2 - currentIntersect.Item2).GetProjectedVector(currentTriangle);
                        if (Math3D.IsInvalid(guideDirection))
                        {
                            // Guide guideDirection is perpendicular to the triangle
                            throw new ApplicationException("Couldn't find an intersect on the other side of the triangle");
                        }

                        // Now reverse the guide direction
                        attemptIntersect = GetPointOnTriangleEdge(currentTriangle, currentIntersect.Item2, currentIntersect.Item2 - guideDirection);
                        if (attemptIntersect == null)
                        {
                            throw new ApplicationException("Couldn't find an intersect on the other side of the triangle");
                        }

                        #endregion
                    }

                    currentIntersect = attemptIntersect;

                    retVal.Add(currentIntersect.Item2);
                }

                if (!Math3D.IsNearValue(retVal[retVal.Count - 1], edgeIntersect2.Item2))
                {
                    retVal.Add(edgeIntersect2.Item2);       // execution will only get here if hull[intersect1.Item1] is a neighbor to hull[intersect2.Item1] (basically, if the while loop didn't do anything)
                }

                retVal.Add(intersect2.Item2);

                return retVal.ToArray();
            }

            /// <summary>
            /// This draws a line from pointOnTriangle toward guidePoint, and stops at the edge of the triangle (returning that point on the edge).
            /// Returns null if the point passed in is already sitting on the desired edge, or if pointOnTriangle+guidePoint is orthogonal to the triangle
            /// </summary>
            internal static Tuple<TriangleEdge, Point3D> GetPointOnTriangleEdge(ITriangle triangle, Point3D pointOnTriangle, Point3D guidePoint)
            {
                // Make the guide line parallel to the triangle
                Vector3D guideDirection = (guidePoint - pointOnTriangle).GetProjectedVector(triangle);
                if (Math3D.IsInvalid(guideDirection))
                {
                    // Guide guideDirection is perpendicular to the triangle
                    return null;
                }

                guideDirection.Normalize();

                Point3D guidePoint2 = pointOnTriangle + guideDirection;

                Point3D? resultEdge, resultGuide;

                foreach (TriangleEdge edge in Enum.GetValues(typeof(TriangleEdge)))
                {
                    // Since the test points are coplanar, this will return a value that is also coplanar
                    if (!Math3D.GetClosestPoints_Line_Line(out resultEdge, out resultGuide, triangle.GetPoint(edge, true), triangle.GetPoint(edge, false), pointOnTriangle, guidePoint2))
                    {
                        continue;
                    }

                    // Make sure it's not outside the triangle
                    if (!Math3D.IsNearTriangle(triangle, resultEdge.Value, false))
                    {
                        continue;
                    }

                    // See if pointOnTriangle is already sitting on this edge
                    Vector3D towardResult = resultEdge.Value - pointOnTriangle;
                    if (Math3D.IsNearZero(towardResult))
                    {
                        continue;
                    }
                    towardResult.Normalize();

                    // Make sure it's along the guide direction
                    if (!Math3D.IsNearValue(Vector3D.DotProduct(towardResult, guideDirection), 1d))
                    {
                        continue;
                    }

                    // Exit Function
                    return Tuple.Create(edge, resultEdge.Value);
                }

                // The only way this should happen is if pointOnTriangle is sitting on the triangle's edge, and that's the edge that
                // the guide line is pointing toward (so it's basically saying, there is no point closer to the edge than pointOnTriangle)
                return null;
            }

            private static Point3D GetTrianglePoint(ITriangle triangle, int index)
            {
                //NOTE: Even if triangle is a TriangleIndexed, this index doesn't refer to .AllPoints, it is just point 0, 1, 2

                switch (index)
                {
                    case 0:
                        return triangle.Point0;

                    case 1:
                        return triangle.Point1;

                    case 2:
                        return triangle.Point2;

                    default:
                        throw new ApplicationException("Unexpected Index: " + index.ToString());
                }
            }

            private static Point3D EnsureOutsidePointIsBeyondHull(Point3D[] hullPoints, Point3D insidePoint, Point3D outsideCenterPoint)
            {
                var aabb = Math3D.GetAABB(hullPoints);
                double hullSize = (aabb.Item2 - aabb.Item1).Length;

                Vector3D outsideDirection = outsideCenterPoint - insidePoint;       // can't just multiply outsideDirection * 1000, because it could be a really skinny triangle inside of a huge hull

                return insidePoint + (outsideDirection.ToUnit() * hullSize * 10d);
            }

            #endregion
        }

        #endregion
        #region Class: HullTriangleIntersect2

        // This was copied to Math3D
        private static class HullTriangleIntersect2
        {
            //NOTE: This will only return a proper polygon, or null (will return null if the triangle is only touching the hull, but not intersecting)
            public static Point3D[] GetIntersection_Hull_Triangle(ITriangleIndexed[] hull, ITriangle triangle)
            {
                // Think of the triangle as a plane, and get a polygon of it slicing through the hull
                Point3D[] planePoly = GetIntersection_Hull_Plane(hull, triangle);
                if (planePoly == null || planePoly.Length < 3)
                {
                    return null;
                }

                // Now intersect that polygon with the triangle (could return null)
                return Math2D.GetIntersection_Polygon_Triangle(planePoly, triangle);
            }

            #region Private Methods

            private static Point3D[] GetIntersection_Hull_Plane(ITriangleIndexed[] hull, ITriangle plane)
            {
                // Shoot through all the triangles in the hull, and get line segment intersections
                List<Tuple<Point3D, Point3D>> lineSegments = null;

                if (hull.Length > 100)
                {
                    lineSegments = hull.
                        AsParallel().
                        Select(o => GetIntersection_Plane_Triangle(plane, o)).
                        Where(o => o != null).
                        ToList();
                }
                else
                {
                    lineSegments = hull.
                        Select(o => GetIntersection_Plane_Triangle(plane, o)).
                        Where(o => o != null).
                        ToList();
                }

                if (lineSegments.Count < 2)
                {
                    // length of 0 is a clear miss, 1 is just touching
                    //NOTE: All the points could be colinear, and just touching the hull, but deeper analysis is needed
                    return null;
                }

                // Stitch the line segments together
                Point3D[] retVal = GetIntersection_Hull_PlaneSprtStitchSegments(lineSegments);

                if (retVal == null)
                {
                    // In some cases, the above method fails, so call the more generic 2D convex hull method
                    //NOTE: This assumes the hull is convex
                    retVal = GetIntersection_Hull_PlaneSprtConvexHull(lineSegments);
                }

                // Exit Function
                return retVal;      // could still be null
            }
            private static Point3D[] GetIntersection_Hull_PlaneSprtStitchSegments(List<Tuple<Point3D, Point3D>> lineSegments)
            {
                // Need to remove single vertex matches, or the loop below will have problems
                var fixedSegments = lineSegments.Where(o => !Math3D.IsNearValue(o.Item1, o.Item2)).ToList();
                if (fixedSegments.Count == 0)
                {
                    return null;
                }

                List<Point3D> retVal = new List<Point3D>();

                // Stitch the segments together into a polygon
                retVal.Add(fixedSegments[0].Item1);
                Point3D currentPoint = fixedSegments[0].Item2;
                fixedSegments.RemoveAt(0);

                while (fixedSegments.Count > 0)
                {
                    var match = FindAndRemoveMatchingSegment(fixedSegments, currentPoint);
                    if (match == null)
                    {
                        //TODO: If this becomes an issue, make a method that builds fragments, then the final polygon will hop from fragment to fragment
                        //TODO: Or, use Math2D.GetConvexHull - make an overload that rotates the points into the xy plane
                        //throw new ApplicationException("The hull passed in has holes in it");
                        return null;
                    }

                    retVal.Add(match.Item1);
                    currentPoint = match.Item2;
                }

                if (!Math3D.IsNearValue(retVal[0], currentPoint))
                {
                    //throw new ApplicationException("The hull passed in has holes in it");
                    return null;
                }

                if (retVal.Count < 3)
                {
                    return null;
                }

                // Exit Function
                return retVal.ToArray();
            }
            private static Point3D[] GetIntersection_Hull_PlaneSprtConvexHull(List<Tuple<Point3D, Point3D>> lineSegments)
            {
                // Convert the line segments into single points (deduped)
                List<Point3D> points = new List<Point3D>();
                foreach (var segment in lineSegments)
                {
                    if (!points.Any(o => Math3D.IsNearValue(o, segment.Item1)))
                    {
                        points.Add(segment.Item1);
                    }

                    if (!points.Any(o => Math3D.IsNearValue(o, segment.Item2)))
                    {
                        points.Add(segment.Item2);
                    }
                }

                // Build a polygon out of the outermost points
                var hull2D = Math2D.GetConvexHull(points.ToArray());
                if (hull2D == null || hull2D.PerimiterLines.Length == 0)
                {
                    return null;
                }

                // Transform to 3D
                return hull2D.PerimiterLines.Select(o => hull2D.GetTransformedPoint(hull2D.Points[o])).ToArray();
            }

            private static Tuple<Point3D, Point3D> GetIntersection_Plane_Triangle(ITriangle plane, ITriangle triangle)
            {
                // Get the line of intersection of the two planes
                Point3D pointOnLine;
                Vector3D lineDirection;
                if (!Math3D.GetIntersection_Plane_Plane(out pointOnLine, out lineDirection, plane, triangle))
                {
                    return null;
                }

                List<Point3D> retval = new List<Point3D>();

                // Cap to the triangle
                foreach (TriangleEdge edge in Enum.GetValues(typeof(TriangleEdge)))
                {
                    Point3D[] resultsLine, resultsLineSegment;
                    if (!Math3D.GetClosestPoints_Line_LineSegment(out resultsLine, out resultsLineSegment, pointOnLine, lineDirection, triangle.GetPoint(edge, true), triangle.GetPoint(edge, false)))
                    {
                        continue;
                    }

                    if (resultsLine.Length != resultsLineSegment.Length)
                    {
                        throw new ApplicationException("The line vs line segments have a different number of matches");
                    }

                    // This method is dealing with lines that are in the same plane, so if the result point for the plane/plane line is different
                    // than the triangle edge, then throw out this match
                    bool allMatched = true;
                    for (int cntr = 0; cntr < resultsLine.Length; cntr++)
                    {
                        if (!Math3D.IsNearValue(resultsLine[cntr], resultsLineSegment[cntr]))
                        {
                            allMatched = false;
                            break;
                        }
                    }

                    if (!allMatched)
                    {
                        continue;
                    }

                    retval.AddRange(resultsLineSegment);

                    if (retval.Count >= 2)
                    {
                        // No need to keep looking, there will only be up to two points of intersection
                        break;
                    }
                }

                // Exit Function
                if (retval.Count == 0)
                {
                    return null;
                }
                else if (retval.Count == 1)
                {
                    // Only touched one vertex
                    return Tuple.Create(retval[0], retval[0]);
                }
                else if (retval.Count == 2)
                {
                    // Standard result
                    return Tuple.Create(retval[0], retval[1]);
                }
                else
                {
                    throw new ApplicationException("Found more than two intersection points");
                }
            }

            /// <summary>
            /// This compares the test point to each end of each line segment.  Then removes that matching segment from the list
            /// </summary>
            /// <returns>
            /// null, or:
            /// Item1=Common Point
            /// Item2=Other Point
            /// </returns>
            private static Tuple<Point3D, Point3D> FindAndRemoveMatchingSegment(List<Tuple<Point3D, Point3D>> lineSegments, Point3D testPoint)
            {
                for (int cntr = 0; cntr < lineSegments.Count; cntr++)
                {
                    if (Math3D.IsNearValue(lineSegments[cntr].Item1, testPoint))
                    {
                        Tuple<Point3D, Point3D> retVal = lineSegments[cntr];
                        lineSegments.RemoveAt(cntr);
                        return retVal;
                    }

                    if (Math3D.IsNearValue(lineSegments[cntr].Item2, testPoint))
                    {
                        Tuple<Point3D, Point3D> retVal = Tuple.Create(lineSegments[cntr].Item2, lineSegments[cntr].Item1);
                        lineSegments.RemoveAt(cntr);
                        return retVal;
                    }
                }

                // No match found
                return null;
            }

            #endregion
        }

        #endregion
        #region Class: PolyUnion

        private static class PolyUnion
        {
            #region Class: EdgeIntersection

            private class EdgeIntersection
            {
                public EdgeIntersection(int poly1Index, Edge2D poly1Edge, int poly2Index, Edge2D poly2Edge, Point intersectionPoint)
                {
                    this.Poly1Index = poly1Index;
                    this.Poly1Edge = poly1Edge;

                    this.Poly2Index = poly2Index;
                    this.Poly2Edge = poly2Edge;

                    this.IntersectionPoint = intersectionPoint;
                }

                public readonly int Poly1Index;
                public readonly Edge2D Poly1Edge;

                public readonly int Poly2Index;
                public readonly Edge2D Poly2Edge;

                public readonly Point IntersectionPoint;
            }

            #endregion
            #region Class: PolygonIsland

            private class PolygonIsland
            {
                public PolygonIsland(Point[][] polygons, EdgeIntersection[] intersections)
                {
                    this.Polygons = polygons;
                    this.Intersections = intersections;
                }

                public readonly Point[][] Polygons;
                public readonly EdgeIntersection[] Intersections;
            }

            #endregion

            public static Point[][] GetPolyUnion(Point[][] polygons)
            {
                //NOTE: This may not work if there are convex polys inside of concave
                // Remove polygons that are completely inside other polygons
                // See which points are inside of other polygons
                //var insidePoints = GetInsidePoints(polygons);

                // See which edges are intersecting
                EdgeIntersection[] intersections = GetIntersections(polygons);
                if (intersections.Length == 0)
                {
                    return polygons;
                }

                // Divide into islands of polygons
                var islands = GetIslands(polygons, intersections);

                Point[][] retVal = new Point[islands.Length][];

                // Union each island independently
                for (int cntr = 0; cntr < retVal.Length; cntr++)
                {
                    if (islands[cntr].Polygons.Length == 1)
                    {
                        retVal[cntr] = islands[cntr].Polygons[0];       // there is only one polygon in this island (no intersections)
                    }
                    else
                    {
                        retVal[cntr] = MergeIsland(islands[cntr]);
                    }
                }

                // Exit Function
                return retVal;
            }

            public static Point[][][] Test1(Point[][] polygons)
            {
                // See which edges are intersecting
                EdgeIntersection[] intersections = GetIntersections(polygons);

                // Divide into islands of polygons
                var islands = GetIslands(polygons, intersections);

                return islands.Select(o => o.Polygons).ToArray();
            }

            #region Private Methods

            private static EdgeIntersection[] GetIntersections(Point[][] polygons)
            {
                List<EdgeIntersection> retVal = new List<EdgeIntersection>();

                for (int outer = 0; outer < polygons.Length - 1; outer++)
                {
                    for (int inner = outer + 1; inner < polygons.Length; inner++)
                    {
                        foreach (var intersection in GetIntersections(polygons[outer], polygons[inner]))
                        {
                            retVal.Add(new EdgeIntersection(outer, intersection.Item1, inner, intersection.Item2, intersection.Item3));
                        }
                    }
                }

                return retVal.ToArray();
            }
            private static Tuple<Edge2D, Edge2D, Point>[] GetIntersections(Point[] poly1, Point[] poly2)
            {
                List<Tuple<Edge2D, Edge2D, Point>> retVal = new List<Tuple<Edge2D, Edge2D, Point>>();

                foreach (Edge2D edge1 in Math2D.IterateEdges(poly1, null))
                {
                    foreach (Edge2D edge2 in Math2D.IterateEdges(poly2, null))
                    {
                        Point? intersect = Math2D.GetIntersection_LineSegment_LineSegment(edge1.Point0, edge1.Point1.Value, edge2.Point0, edge2.Point1.Value);
                        if (intersect != null)
                        {
                            retVal.Add(Tuple.Create(edge1, edge2, intersect.Value));
                        }
                    }
                }

                return retVal.ToArray();
            }

            private static PolygonIsland[] GetIslands(Point[][] polygons, EdgeIntersection[] intersections)
            {
                // Figure out which polygons are in which islands
                var islands = GetIslandsSprtIndices(polygons, intersections);

                if (islands.Length == 1)
                {
                    return new PolygonIsland[] { new PolygonIsland(polygons, intersections) };
                }

                PolygonIsland[] retVal = new PolygonIsland[islands.Length];

                // Make new intersections that only know about the polygons in their island
                for (int cntr = 0; cntr < retVal.Length; cntr++)
                {
                    List<Point[]> localPolys = new List<Point[]>();
                    SortedList<int, int> mapping = new SortedList<int, int>();

                    foreach (int index in islands[cntr].Item1)
                    {
                        mapping.Add(index, localPolys.Count);
                        localPolys.Add(polygons[index]);
                    }

                    EdgeIntersection[] localIntersections = islands[cntr].Item2.Select(o => new EdgeIntersection(mapping[o.Poly1Index], o.Poly1Edge, mapping[o.Poly2Index], o.Poly2Edge, o.IntersectionPoint)).ToArray();

                    retVal[cntr] = new PolygonIsland(localPolys.ToArray(), localIntersections);
                }

                // Exit Function
                return retVal;
            }

            private static Tuple<int[], EdgeIntersection[]>[] GetIslandsSprtIndices(Point[][] polygons, EdgeIntersection[] intersections)
            {
                List<Tuple<int[], EdgeIntersection[]>> retVal = new List<Tuple<int[], EdgeIntersection[]>>();
                List<int> remaining = Enumerable.Range(0, polygons.Length).ToList();

                while (remaining.Count > 0)
                {
                    // Find all the polygons that are touching this one
                    var currentIsland = GetIslandsSprtTouching(remaining[0], intersections);

                    // Remove these from the remaining list (after this, what's in remaining are polygons that aren't touching what's in retVal)
                    foreach (int index in currentIsland.Item1)
                    {
                        remaining.Remove(index);
                    }

                    // Store this island
                    retVal.Add(currentIsland);
                }

                // Exit Function
                return retVal.ToArray();
            }
            private static Tuple<int[], EdgeIntersection[]> GetIslandsSprtTouching(int index, EdgeIntersection[] intersections)
            {
                List<int> returnIndices = new List<int>();
                List<EdgeIntersection> returnIntersections = new List<EdgeIntersection>();

                Stack<int> newIndices = new Stack<int>();

                // Put the intersections in a list, so it can be removed from as matches are found
                List<EdgeIntersection> remainingIntersections = intersections.ToList();

                // Start the working stack off with the index passed in
                newIndices.Push(index);

                while (newIndices.Count > 0)
                {
                    // Pop off the stack, and look for matches
                    int currentIndex = newIndices.Pop();
                    returnIndices.Add(currentIndex);

                    // Find unique intersections that match this current polygon, and put them in the working stack if they haven't been encountered yet
                    foreach (int match in GetIslandsSprtTouchingSprtExamine(remainingIntersections, returnIntersections, currentIndex))
                    {
                        if (!returnIndices.Contains(match) && !newIndices.Contains(match))
                        {
                            newIndices.Push(match);
                        }
                    }
                }

                // Exit Function
                return Tuple.Create(returnIndices.ToArray(), returnIntersections.ToArray());
            }
            private static int[] GetIslandsSprtTouchingSprtExamine(List<EdgeIntersection> intersections, List<EdgeIntersection> matchedIntersections, int currentPoly)
            {
                List<int> retVal = new List<int>();

                int index = 0;
                while (index < intersections.Count)
                {
                    int otherPoly = -1;

                    if (intersections[index].Poly1Index == currentPoly)
                    {
                        otherPoly = intersections[index].Poly2Index;
                    }
                    else if (intersections[index].Poly2Index == currentPoly)
                    {
                        otherPoly = intersections[index].Poly1Index;
                    }
                    else
                    {
                        index++;
                        continue;
                    }

                    // This is a match.  Transfer it from the list of intersections so it's not looked at again
                    matchedIntersections.Add(intersections[index]);
                    intersections.RemoveAt(index);

                    // Add to return if unique
                    if (!retVal.Contains(otherPoly))
                    {
                        retVal.Add(otherPoly);
                    }
                }

                // Exit Function
                return retVal.ToArray();
            }

            private static Point[] MergeIsland(PolygonIsland island)
            {
                // Convert into line segments
                Tuple<Point, Point>[] allSegments = MergeIslandSprtSegments(island);

                List<Tuple<Point, bool>> mids;

                // Remove segments that are inside a polygon
                Tuple<Point, Point>[] segments = MergeIslandSprtRemoveInternal(out mids, allSegments, island.Polygons);

                // Stitch together the remaining segments
                Point[] retVal = StitchSegments(segments);
                if (retVal == null)
                {
                    //NOTE: This assumes that the segments form a convex hull, which is likely incorrect
                    //retVal = GetConvexHull(segments);
                    throw new ApplicationException("Fix this");
                }

                return retVal;
            }

            private static Tuple<Point, Point>[] MergeIslandSprtSegments(PolygonIsland island)
            {
                List<Tuple<Point, Point>> retVal = new List<Tuple<Point, Point>>();

                // Shoot through all the polygons in this island
                for (int polyCntr = 0; polyCntr < island.Polygons.Length; polyCntr++)
                {
                    // Shoot through each edge of this polygon
                    foreach (Edge2D edge in Math2D.IterateEdges(island.Polygons[polyCntr], null))
                    {
                        // Get all intersections of this edge
                        EdgeIntersection[] intersections = MergeIslandSprtSegmentsSprtIntersections(polyCntr, edge, island.Intersections);

                        if (intersections.Length == 0)
                        {
                            // Nothing is intersecting this edge, so add the entire edge
                            retVal.Add(Tuple.Create(edge.Point0, edge.Point1Ext));
                        }
                        else
                        {
                            // This edge has intersections.  Convert into segments
                            retVal.AddRange(MergeIslandSprtSegmentsSprtDivide(polyCntr, edge, intersections));
                        }
                    }
                }

                // Exit Function
                return retVal.ToArray();
            }

            private static EdgeIntersection[] MergeIslandSprtSegmentsSprtIntersections(int polygonIndex, Edge2D edge, EdgeIntersection[] intersections)
            {
                List<EdgeIntersection> retVal = new List<EdgeIntersection>();

                foreach (EdgeIntersection intersection in intersections)
                {
                    if (intersection.Poly1Index == polygonIndex)        //  polygon match (1)
                    {
                        if (MergeIslandSprtSegmentsSprtIntersectionsSprtIsSame(edge.Index0, edge.Index1.Value, intersection.Poly1Edge.Index0, intersection.Poly1Edge.Index1.Value))     // edge match (1)
                        {
                            retVal.Add(intersection);
                        }
                    }
                    else if (intersection.Poly2Index == polygonIndex)       //  polygon match (2)
                    {
                        if (MergeIslandSprtSegmentsSprtIntersectionsSprtIsSame(edge.Index0, edge.Index1.Value, intersection.Poly2Edge.Index0, intersection.Poly2Edge.Index1.Value))     // edge match (2)
                        {
                            retVal.Add(intersection);
                        }
                    }
                }

                return retVal.ToArray();
            }
            private static bool MergeIslandSprtSegmentsSprtIntersectionsSprtIsSame(int edge0Index0, int edge0Index1, int edge1Index0, int edge1Index1)
            {
                // 0[0] could be the same as 1[0] or 1[1], 0[1] could match the other.  So use math.min and math.max to
                // reduce the number of compares
                return Math.Min(edge0Index0, edge0Index1) == Math.Min(edge1Index0, edge1Index1) &&
                    Math.Max(edge0Index0, edge0Index1) == Math.Max(edge1Index0, edge1Index1);
            }

            private static IEnumerable<Tuple<Point, Point>> MergeIslandSprtSegmentsSprtDivide(int polyCntr, Edge2D edge, EdgeIntersection[] intersections)
            {
                // Build vectors from edge.p0 to each intersection, sort by length
                var distances = Enumerable.Range(0, intersections.Length).
                    Select(o => new { Point = intersections[o].IntersectionPoint, Distance = (intersections[o].IntersectionPoint - edge.Point0).LengthSquared }).
                    OrderBy(o => o.Distance).
                    ToArray();

                List<Tuple<Point, Point>> retVal = new List<Tuple<Point, Point>>();

                // Point0 to first intersect
                retVal.Add(Tuple.Create(
                    edge.Point0,
                    distances[0].Point));

                // Intersect to Intersect
                for (int cntr = 0; cntr < distances.Length - 1; cntr++)
                {
                    retVal.Add(Tuple.Create(
                        distances[cntr].Point,
                        distances[cntr + 1].Point));
                }

                // Last intersect to Point1
                retVal.Add(Tuple.Create(
                    distances[distances.Length - 1].Point,
                    edge.Point1Ext));

                return retVal;
            }
            private static IEnumerable<Tuple<Point, Point>> MergeIslandSprtSegmentsSprtDivide_OLD(int polyCntr, Edge2D edge, EdgeIntersection[] intersections)
            {
                // Build vectors from edge.p0 to each intersection, sort by length
                var distances = Enumerable.Range(0, intersections.Length).
                    Select(o => new { Index = o, Distance = (intersections[o].IntersectionPoint - edge.Point0).LengthSquared }).
                    OrderBy(o => o.Distance).
                    ToArray();

                List<Tuple<Point, Point>> retVal = new List<Tuple<Point, Point>>();

                // Point0 to first intersect
                retVal.Add(Tuple.Create(
                    edge.Point0,
                    intersections[distances[0].Index].IntersectionPoint));

                // Intersect to Intersect
                for (int cntr = 0; cntr < distances.Length - 1; cntr++)
                {
                    retVal.Add(Tuple.Create(
                        intersections[distances[cntr].Index].IntersectionPoint,
                        intersections[distances[cntr + 1].Index].IntersectionPoint));
                }

                // Last intersect to Point1
                retVal.Add(Tuple.Create(
                    intersections[distances[distances.Length - 1].Index].IntersectionPoint,
                    edge.Point1Ext));

                return retVal;
            }

            private static Tuple<Point, Point>[] MergeIslandSprtRemoveInternal(out List<Tuple<Point, bool>> mids, Tuple<Point, Point>[] segments, Point[][] polygons)
            {
                List<Tuple<Point, Point>> retVal = new List<Tuple<Point, Point>>();

                mids = new List<Tuple<Point, bool>>();

                foreach (var segment in segments)
                {
                    bool isInside = false;

                    // Since the from and to of this segment sits on the edge of polygons, it is difficult to test.  Using the midpoint
                    // takes out guesswork
                    Point midPoint = segment.Item1 + ((segment.Item2 - segment.Item1) * .5d);

                    // Test against each polygon
                    foreach (Point[] polygon in polygons)
                    {
                        if (Math2D.IsInsidePolygon(polygon, midPoint, false))
                        {
                            isInside = true;
                            break;
                        }
                    }

                    mids.Add(Tuple.Create(midPoint, isInside));

                    if (!isInside)
                    {
                        // This defines an outside edge
                        retVal.Add(segment);
                    }
                }

                // Exit Function
                return retVal.ToArray();
            }

            #region Copied from HullTriangleIntersect2

            private static Point[] StitchSegments(Tuple<Point, Point>[] lineSegments)
            {
                // Need to remove single vertex matches, or the loop below will have problems
                var fixedSegments = lineSegments.Where(o => !Math2D.IsNearValue(o.Item1, o.Item2)).ToList();
                if (fixedSegments.Count == 0)
                {
                    return null;
                }

                List<Point> retVal = new List<Point>();

                // Stitch the segments together into a polygon
                retVal.Add(fixedSegments[0].Item1);
                Point currentPoint = fixedSegments[0].Item2;
                fixedSegments.RemoveAt(0);

                while (fixedSegments.Count > 0)
                {
                    var match = FindAndRemoveMatchingSegment(fixedSegments, currentPoint);
                    if (match == null)
                    {
                        //TODO: If this becomes an issue, make a method that builds fragments, then the final polygon will hop from fragment to fragment
                        //TODO: Or, use Math2D.GetConvexHull - make an overload that rotates the points into the xy plane
                        //throw new ApplicationException("The hull passed in has holes in it");
                        return null;
                    }

                    retVal.Add(match.Item1);
                    currentPoint = match.Item2;
                }

                if (!Math2D.IsNearValue(retVal[0], currentPoint))
                {
                    //throw new ApplicationException("The hull passed in has holes in it");
                    return null;
                }

                if (retVal.Count < 3)
                {
                    return null;
                }

                // Exit Function
                return retVal.ToArray();
            }
            private static Point[] GetConvexHull(Tuple<Point, Point>[] lineSegments)
            {
                // Convert the line segments into single points (deduped)
                List<Point> points = new List<Point>();
                foreach (var segment in lineSegments)
                {
                    if (!points.Any(o => Math2D.IsNearValue(o, segment.Item1)))
                    {
                        points.Add(segment.Item1);
                    }

                    if (!points.Any(o => Math2D.IsNearValue(o, segment.Item2)))
                    {
                        points.Add(segment.Item2);
                    }
                }

                // Build a polygon out of the outermost points
                var hull2D = Math2D.GetConvexHull(points.ToArray());
                if (hull2D == null || hull2D.PerimiterLines.Length == 0)
                {
                    return null;
                }

                // Exit Function
                return hull2D.Points;
            }

            /// <summary>
            /// This compares the test point to each end of each line segment.  Then removes that matching segment from the list
            /// </summary>
            /// <returns>
            /// null, or:
            /// Item1=Common Point
            /// Item2=Other Point
            /// </returns>
            private static Tuple<Point, Point> FindAndRemoveMatchingSegment(List<Tuple<Point, Point>> lineSegments, Point testPoint)
            {
                for (int cntr = 0; cntr < lineSegments.Count; cntr++)
                {
                    if (Math2D.IsNearValue(lineSegments[cntr].Item1, testPoint))
                    {
                        Tuple<Point, Point> retVal = lineSegments[cntr];
                        lineSegments.RemoveAt(cntr);
                        return retVal;
                    }

                    if (Math2D.IsNearValue(lineSegments[cntr].Item2, testPoint))
                    {
                        Tuple<Point, Point> retVal = Tuple.Create(lineSegments[cntr].Item2, lineSegments[cntr].Item1);
                        lineSegments.RemoveAt(cntr);
                        return retVal;
                    }
                }

                // No match found
                return null;
            }

            #endregion

            #endregion
        }

        #endregion
        #region Class: PolyUnion2

        private static class PolyUnion2
        {
            #region Class: EdgeIntersection

            private class EdgeIntersection
            {
                public EdgeIntersection(int poly1Index, Edge2D poly1Edge, int poly2Index, Edge2D poly2Edge, Point intersectionPoint)
                {
                    this.Poly1Index = poly1Index;
                    this.Poly1Edge = poly1Edge;

                    this.Poly2Index = poly2Index;
                    this.Poly2Edge = poly2Edge;

                    this.IntersectionPoint = intersectionPoint;
                }

                public readonly int Poly1Index;
                public readonly Edge2D Poly1Edge;

                public readonly int Poly2Index;
                public readonly Edge2D Poly2Edge;

                public readonly Point IntersectionPoint;
            }

            #endregion
            #region Class: PolygonIsland

            private class PolygonIsland
            {
                public PolygonIsland(Point[][] polygons, EdgeIntersection[] intersections)
                {
                    this.Polygons = polygons;
                    this.Intersections = intersections;
                }

                public readonly Point[][] Polygons;
                public readonly EdgeIntersection[] Intersections;
            }

            #endregion
            #region Class: StitchSegments

            private static class StitchSegments
            {
                //TODO: Rework this method:
                //  Join the segments into chains (stop a chain at any T intersection, but remember which chains are intersecting)
                //      If there are no T intersections, then the chain will be the final polygon
                //      If there's one T intersection, there should be a corresponding one on the other side of the polygon
                //
                //  Actually, I think this turning into a rework of previous methods
                public static Point[][] Stitch(Tuple<Point, Point>[] lineSegments)
                {
                    // Clean up the line segments (dedupe points, etc)
                    var fixedSegments = StitchSegmentsSprtCleanup(lineSegments);
                    if (fixedSegments.Count == 0)
                    {
                        return null;
                    }

                    List<Point[]> retVal = new List<Point[]>();

                    while (fixedSegments.Count > 0)
                    {
                        Point[] polygon = StitchSegmentsSprtSingle(fixedSegments);
                        if (polygon == null)
                        {
                            // There was some kind of error.  Don't give out bad results
                            return null;
                        }

                        retVal.Add(polygon);
                    }

                    // Exit Function
                    return retVal.ToArray();
                }

                #region Private Methods

                private static List<Tuple<Point, Point>> StitchSegmentsSprtCleanup(Tuple<Point, Point>[] lineSegments)
                {
                    // Remove single vertex matches
                    return lineSegments.Where(o => !Math2D.IsNearValue(o.Item1, o.Item2)).ToList();
                }
                private static List<Tuple<Point, Point>> StitchSegmentsSprtCleanup_EXTENDCOLINEAR(Tuple<Point, Point>[] lineSegments)
                {
                    // Remove single vertex matches
                    var deduped = lineSegments.Where(o => !Math2D.IsNearValue(o.Item1, o.Item2)).ToList();



                    // Remove double T joints (happens when two polygons are butted up next to each other.  Need to get rid of that middle segment)



                    // Removing colinear should be optional, it's an expense that may not be desired
                    bool merged = false;
                    do
                    {
                        // Find segment pairs that are colienar, and convert to a single segment (but only if they have no other segments using the point
                        // to be deleted)






                    } while (merged);









                    return null;

                }
                private static List<Tuple<Point, Point>> StitchSegmentsSprtCleanup_TOOMUCH(Tuple<Point, Point>[] lineSegments)
                {
                    Tuple<Point, Point>[] cleaned = lineSegments.
                        Where(o => !Math2D.IsNearValue(o.Item1, o.Item2)).     // Remove single vertex matches
                        Select(o => o.Item1.X < o.Item2.X ? o : Tuple.Create(o.Item2, o.Item1)).     // Put the smaller x as item1
                        ToArray();

                    // Only return distinct segments
                    List<Tuple<Point, Point>> retVal = new List<Tuple<Point, Point>>();
                    foreach (var segment in cleaned)
                    {
                        if (!retVal.Any(o => Math2D.IsNearValue(segment.Item1, o.Item1) && Math2D.IsNearValue(segment.Item2, o.Item2)))
                        {
                            retVal.Add(segment);
                        }
                    }

                    // Exit Function
                    return retVal;
                }
                private static Point[] StitchSegmentsSprtSingle(List<Tuple<Point, Point>> segments)
                {
                    List<Point> retVal = new List<Point>();

                    // Stitch the segments together into a polygon
                    retVal.Add(segments[0].Item1);
                    Point currentPoint = segments[0].Item2;
                    segments.RemoveAt(0);

                    while (segments.Count > 0)
                    {
                        var match = FindAndRemoveMatchingSegment(segments, currentPoint);
                        if (match == null)
                        {
                            // The rest of the segments belong to a polygon independent of the one that is currently being built (a hole)
                            break;
                        }

                        retVal.Add(match.Item1);
                        currentPoint = match.Item2;
                    }

                    if (!Math2D.IsNearValue(retVal[0], currentPoint))
                    {
                        //throw new ApplicationException("There are gaps in this polygon");
                        return null;
                    }

                    if (retVal.Count < 3)
                    {
                        return null;
                    }

                    return retVal.ToArray();
                }

                private static Point[] GetConvexHull(Tuple<Point, Point>[] lineSegments)
                {
                    // Convert the line segments into single points (deduped)
                    List<Point> points = new List<Point>();
                    foreach (var segment in lineSegments)
                    {
                        if (!points.Any(o => Math2D.IsNearValue(o, segment.Item1)))
                        {
                            points.Add(segment.Item1);
                        }

                        if (!points.Any(o => Math2D.IsNearValue(o, segment.Item2)))
                        {
                            points.Add(segment.Item2);
                        }
                    }

                    // Build a polygon out of the outermost points
                    var hull2D = Math2D.GetConvexHull(points.ToArray());
                    if (hull2D == null || hull2D.PerimiterLines.Length == 0)
                    {
                        return null;
                    }

                    // Exit Function
                    return hull2D.Points;
                }

                /// <summary>
                /// This compares the test point to each end of each line segment.  Then removes that matching segment from the list
                /// </summary>
                /// <returns>
                /// null, or:
                /// Item1=Common Point
                /// Item2=Other Point
                /// </returns>
                private static Tuple<Point, Point> FindAndRemoveMatchingSegment(List<Tuple<Point, Point>> lineSegments, Point testPoint)
                {
                    for (int cntr = 0; cntr < lineSegments.Count; cntr++)
                    {
                        if (Math2D.IsNearValue(lineSegments[cntr].Item1, testPoint))
                        {
                            Tuple<Point, Point> retVal = lineSegments[cntr];
                            lineSegments.RemoveAt(cntr);
                            return retVal;
                        }

                        if (Math2D.IsNearValue(lineSegments[cntr].Item2, testPoint))
                        {
                            Tuple<Point, Point> retVal = Tuple.Create(lineSegments[cntr].Item2, lineSegments[cntr].Item1);
                            lineSegments.RemoveAt(cntr);
                            return retVal;
                        }
                    }

                    // No match found
                    return null;
                }

                #endregion
            }

            #endregion
            #region Class: StitchSegments2

            internal static class StitchSegments2
            {
                #region Class: Chain

                private class Chain
                {
                    public Chain(int[] points, bool isClosed)
                    {
                        this.Points = points;
                        this.IsClosed = isClosed;
                    }

                    public readonly int[] Points;
                    public readonly bool IsClosed;
                }

                #endregion
                #region Class: Junction

                private class Junction
                {
                    public Junction(int fromPoint, int toPoint, List<Chain> chains)
                    {
                        this.FromPoint = fromPoint;
                        this.ToPoint = toPoint;
                        this.Chains = chains;
                    }

                    public readonly int FromPoint;
                    public readonly int ToPoint;
                    public List<Chain> Chains;
                }

                #endregion

                public static Polygon2D[] Stitch(Tuple<Point, Point>[] lineSegments)
                {
                    // Get unique points, and store the segments as indices
                    Point[] points;
                    Tuple<int, int>[] segments;
                    GetUniquePoints(out points, out segments, lineSegments);

                    segments = RemoveTails(segments);

                    // Come up with chains and rings
                    List<Chain> chains = GetChains(segments);
                    List<Junction> junctions = GetJunctions(chains);
                    chains = chains.Where(o => o.IsClosed).ToList();       // only keep the rings (the junctions now own the fragment chains)

                    foreach (Junction junction in junctions)
                    {
                        // If a junction has more than 2 chains, it can be reduced to 2
                        ReduceJunction_Single(junction, points);
                    }

                    if (junctions.Count == 1)
                    {
                        // Only one junction, it is a simple chain
                        chains.Add(ConvertStandaloneJunctionToChain(junctions[0]));
                        junctions.Clear();
                    }
                    else if (junctions.Count > 1)
                    {
                        ReduceJunctions_Multiple(junctions, chains, points);
                    }

                    // All junctions are gone, the chains are loops.  See which chains are independent polygons, and which are holes inside other chains
                    return ConvertChainsToPolys(chains, points);
                }

                #region Private Methods

                private static void GetUniquePoints(out Point[] uniquePoints, out Tuple<int, int>[] segments, Tuple<Point, Point>[] lineSegments)
                {
                    // Find the unique points
                    List<Point> uniquePointList = new List<Point>();
                    foreach (Point point in UtilityHelper.Iterate(lineSegments.Select(o => o.Item1), lineSegments.Select(o => o.Item2)))
                    {
                        if (!uniquePointList.Any(o => Math2D.IsNearValue(point, o)))
                        {
                            uniquePointList.Add(point);
                        }
                    }

                    uniquePoints = uniquePointList.ToArray();

                    // Convert the line segments into indices
                    segments = lineSegments.
                        Select(o => Tuple.Create(IndexOfPoint(uniquePointList, o.Item1), IndexOfPoint(uniquePointList, o.Item2))).      // using the list, because an out param can't be used in a lambda, and I didn't feel like making a copy of the array
                        Where(o => o.Item1 != o.Item2).
                        Select(o => o.Item1 < o.Item2 ? o : Tuple.Create(o.Item2, o.Item1)).        // force the item 1 to be the smaller index (so that the distinct will work properly)
                        Distinct().     // get rid of duplicate segments
                        ToArray();

                    if (segments.Any(o => o.Item1 < 0 || o.Item2 < 0))
                    {
                        throw new ApplicationException("Unique point wasn't found");
                    }
                }

                private static int IndexOfPoint(List<Point> points, Point testPoint)
                {
                    for (int cntr = 0; cntr < points.Count; cntr++)
                    {
                        if (Math2D.IsNearValue(points[cntr], testPoint))
                        {
                            return cntr;
                        }
                    }

                    return -1;
                }

                private static Tuple<int, int>[] RemoveTails(Tuple<int, int>[] segments)
                {
                    List<Tuple<int, int>> segmentList = new List<Tuple<int, int>>(segments);

                    while (true)
                    {
                        // Find the points that only have one segment pointing to them
                        var pointCounts = UtilityHelper.Iterate(segmentList.Select(o => o.Item1), segmentList.Select(o => o.Item2)).
                            GroupBy(o => o).
                            Select(o => new { Point = o.Key, Count = o.Count() }).
                            Where(o => o.Count == 1).
                            ToArray();

                        if (pointCounts.Length == 0)
                        {
                            return segmentList.ToArray();
                        }

                        int pointToFind = pointCounts[0].Point;

                        // Remove the segment that contains this point
                        bool foundIt = false;
                        for (int cntr = 0; cntr < segmentList.Count; cntr++)
                        {
                            if (segmentList[cntr].Item1 == pointToFind || segmentList[cntr].Item2 == pointToFind)
                            {
                                segmentList.RemoveAt(cntr);
                                foundIt = true;
                                break;
                            }
                        }

                        if (!foundIt)
                        {
                            throw new ApplicationException("Didn't find point");
                        }
                    }

                    throw new ApplicationException("Execution should never get here");
                }

                private static List<Chain> GetChains(Tuple<int, int>[] segments)
                {
                    // Find the points that have more than two segments pointing to them
                    var pointCounts = UtilityHelper.Iterate(segments.Select(o => o.Item1), segments.Select(o => o.Item2)).
                        GroupBy(o => o).
                        Select(o => Tuple.Create(o.Key, o.Count())).
                        Where(o => o.Item2 > 2).
                        ToArray();

                    List<Tuple<int, int>> segmentList = segments.ToList();

                    if (pointCounts.Length == 0)
                    {
                        // There are no junctions, so just return the unique loops
                        return GetChainsSprtLoops(segmentList);
                    }

                    List<Chain> retVal = new List<Chain>();

                    retVal.AddRange(GetChainsSprtFragments(segmentList, pointCounts));

                    if (segmentList.Count > 0)
                    {
                        retVal.AddRange(GetChainsSprtLoops(segmentList));
                    }

                    return retVal;
                }
                private static List<Chain> GetChainsSprtLoops(List<Tuple<int, int>> segments)
                {
                    List<Chain> retVal = new List<Chain>();

                    int[] ends = new int[0];

                    while (segments.Count > 0)
                    {
                        Chain polygon = GetChainsSprtSingle(segments, segments[0].Item1, ends);

                        retVal.Add(polygon);
                    }

                    // Exit Function
                    return retVal;
                }
                private static Chain[] GetChainsSprtFragments(List<Tuple<int, int>> segments, Tuple<int, int>[] pointCounts)
                {
                    List<int> ends = pointCounts.SelectMany(o => Enumerable.Repeat(o.Item1, o.Item2)).ToList();

                    List<Chain> retVal = new List<Chain>();

                    while (ends.Count > 0)
                    {
                        Chain chain = GetChainsSprtSingle(segments, ends[0], ends.Where(o => o != ends[0]).ToArray());

                        ends.Remove(chain.Points[0]);
                        ends.Remove(chain.Points[chain.Points.Length - 1]);

                        retVal.Add(chain);
                    }

                    return retVal.ToArray();
                }
                private static Chain GetChainsSprtSingle(List<Tuple<int, int>> segments, int start, int[] ends)
                {
                    List<int> retVal = new List<int>();

                    #region Find the start

                    int currentPoint = -1;

                    for (int cntr = 0; cntr < segments.Count; cntr++)
                    {
                        if (segments[cntr].Item1 == start)
                        {
                            retVal.Add(segments[cntr].Item1);
                            currentPoint = segments[cntr].Item2;
                            segments.RemoveAt(cntr);
                            break;
                        }
                        else if (segments[cntr].Item2 == start)
                        {
                            retVal.Add(segments[cntr].Item2);
                            currentPoint = segments[cntr].Item1;
                            segments.RemoveAt(cntr);
                            break;
                        }
                    }

                    if (currentPoint < 0)
                    {
                        int[] points = UtilityHelper.Iterate(segments.Select(o => o.Item1), segments.Select(o => o.Item2)).Distinct().ToArray();
                        throw new ApplicationException(string.Format("Didn't find the start: {0}, in: {1}", start.ToString(), string.Join(" ", points)));
                    }

                    #endregion

                    // Stitch the segments together into a polygon
                    while (segments.Count > 0)
                    {
                        if (ends.Contains(currentPoint))
                        {
                            retVal.Add(currentPoint);
                            return new Chain(retVal.ToArray(), false);
                        }

                        var match = FindAndRemoveMatchingSegment(segments, currentPoint);
                        if (match == null)
                        {
                            // The rest of the segments belong to a polygon independent of the one that is currently being built (a hole)
                            break;
                        }

                        retVal.Add(match.Item1);
                        currentPoint = match.Item2;
                    }

                    if (ends.Length == 0)
                    {
                        if (retVal[0] != currentPoint)
                        {
                            throw new ApplicationException("There are gaps in this polygon");
                        }

                        if (retVal.Count < 3)
                        {
                            return null;
                        }

                        return new Chain(retVal.ToArray(), true);
                    }
                    else
                    {
                        if (!ends.Contains(currentPoint))
                        {
                            throw new ApplicationException("Didn't find the end point");
                        }

                        retVal.Add(currentPoint);
                        return new Chain(retVal.ToArray(), false);
                    }
                }

                /// <summary>
                /// This compares the test point to each end of each line segment.  Then removes that matching segment from the list
                /// </summary>
                /// <returns>
                /// null, or:
                /// Item1=Common Point
                /// Item2=Other Point
                /// </returns>
                private static Tuple<int, int> FindAndRemoveMatchingSegment(List<Tuple<int, int>> lineSegments, int testPoint)
                {
                    for (int cntr = 0; cntr < lineSegments.Count; cntr++)
                    {
                        if (lineSegments[cntr].Item1 == testPoint)
                        {
                            var retVal = lineSegments[cntr];
                            lineSegments.RemoveAt(cntr);
                            return retVal;
                        }

                        if (lineSegments[cntr].Item2 == testPoint)
                        {
                            var retVal = Tuple.Create(lineSegments[cntr].Item2, lineSegments[cntr].Item1);
                            lineSegments.RemoveAt(cntr);
                            return retVal;
                        }
                    }

                    // No match found
                    return null;
                }

                private static List<Junction> GetJunctions(IEnumerable<Chain> chains)
                {
                    SortedList<Tuple<int, int>, List<Chain>> junctions = new SortedList<Tuple<int, int>, List<Chain>>();

                    foreach (Chain openChain in chains.Where(o => !o.IsClosed))
                    {
                        // Get the from/to points
                        Tuple<int, int> fromTo = Tuple.Create(openChain.Points[0], openChain.Points[openChain.Points.Length - 1]);

                        // Make sure from is less than to (that way all the chains point the same direction)
                        Chain chain = openChain;
                        if (fromTo.Item2 < fromTo.Item1)
                        {
                            // Reverse the chain
                            chain = new Chain(chain.Points.Reverse().ToArray(), false);
                            fromTo = Tuple.Create(fromTo.Item2, fromTo.Item1);
                        }

                        // Add this to a junction
                        if (!junctions.ContainsKey(fromTo))
                        {
                            junctions.Add(fromTo, new List<Chain>());
                        }

                        junctions[fromTo].Add(chain);
                    }

                    // Finalize it
                    List<Junction> retVal = new List<Junction>();

                    foreach (var point in junctions.Keys)
                    {
                        retVal.Add(new Junction(point.Item1, point.Item2, junctions[point].ToList()));
                    }

                    return retVal;
                }

                private static void ReduceJunction_Single(Junction junction, Point[] points)
                {
                    if (junction.Chains.Count <= 2)
                    {
                        // This method is only meant for 3 or more chains (reducing down to two)
                        return;
                    }

                    int indexLeft = 0;
                    int indexRight = 1;
                    Point[] currentPoly = GetPolyFromTouchingChains(junction.Chains[indexLeft], junction.Chains[indexRight]).
                        Select(o => points[o]).
                        ToArray();

                    for (int cntr = 2; cntr < junction.Chains.Count; cntr++)
                    {
                        // Compare a point in this chain with the existing polygon
                        if (Math2D.IsInsidePolygon(currentPoly, GetPointInsideChain(junction.Chains[cntr], points), false))
                        {
                            continue;
                        }

                        // Try keeping left (swap right)
                        if (ReduceJunction_Single_Try(ref currentPoly, indexLeft, ref indexRight, cntr, junction.Chains, points))
                        {
                            continue;
                        }

                        // Try keeping right (swap left)
                        if (ReduceJunction_Single_Try(ref currentPoly, indexRight, ref indexLeft, cntr, junction.Chains, points))
                        {
                            continue;
                        }

                        throw new ApplicationException("Couldn't find a chain to remove");
                    }

                    #region Remove inside chains

                    Chain leftChain = junction.Chains[indexLeft];
                    Chain rightChain = junction.Chains[indexRight];

                    junction.Chains.Clear();

                    junction.Chains.Add(leftChain);
                    junction.Chains.Add(rightChain);

                    #endregion
                }
                private static bool ReduceJunction_Single_Try(ref Point[] currentPoly, int indexStatic, ref int indexSwap, int indexAttempt, List<Chain> chains, Point[] points)
                {
                    // Make a polygon out of the static chain and the attempt chain
                    Point[] polyAttempt = GetPolyFromTouchingChains(chains[indexStatic], chains[indexAttempt]).
                        Select(o => points[o]).
                        ToArray();

                    Point swapPoint = GetPointInsideChain(chains[indexSwap], points);

                    // See if the swap chain is inside this new polygon
                    if (Math2D.IsInsidePolygon(polyAttempt, swapPoint, false))
                    {
                        indexSwap = indexAttempt;
                        currentPoly = polyAttempt;
                        return true;
                    }

                    return false;
                }

                /// <summary>
                /// This handles cases where there are multiple junctions that form the final polygon.  Once this method finishes, all junctions
                /// will be converted into chains
                /// </summary>
                /// <remarks>
                /// The junctions are are result of the original input polygons touching each other, not intersecting
                /// </remarks>
                /// <param name="junctions">These will either contain 1 or 2 chains.  If 1, it is connecting other junctions.  If 2 it is two possible chains, only one of which should be kept</param>
                /// <param name="chains">These are standalone loops</param>
                private static void ReduceJunctions_Multiple(List<Junction> junctions, List<Chain> chains, Point[] points)
                {
                    #region asserts
#if DEBUG

                    if (junctions.Any(o => o.Chains.Count == 0 || o.Chains.Count > 2))
                    {
                        throw new ArgumentException("All junctions should contain only 1 or 2 chains");
                    }

                    if (chains.Any(o => !o.IsClosed))
                    {
                        throw new ArgumentException("All standalone chains should be closed loops");
                    }

#endif
                    #endregion

                    // Eliminate junctions that are inside of others
                    ReduceJunctions_Multiple_Interior(junctions, points);

                    if (junctions.Count == 1)
                    {
                        // There is nothing left for this method to do
                        chains.Add(new Chain(GetPolyFromTouchingChains(junctions[0].Chains[0], junctions[0].Chains[1]).ToArray(), true));
                        junctions.Clear();
                        return;
                    }

                    //TODO: Be able to handle clusters of junctions that don't form a clear loop

                    var chainGuide = GetChains(junctions.Select(o => Tuple.Create(o.FromPoint, o.ToPoint)).ToArray());


                    if (chainGuide.Any(o => !o.IsClosed))
                    {
                        ReduceJunctions_Multiple_Cluster(junctions, chains, points);
                        return;
                    }



                    foreach (Chain juncLoop in chainGuide)
                    {
                        #region asserts
#if DEBUG
                        if (!juncLoop.IsClosed)
                        {
                            throw new ApplicationException("Expected this to be closed");
                        }
#endif
                        #endregion

                        Junction[] juncsForLoop = ReduceJunctions_Multiple_GetChain(juncLoop.Points, junctions);

                        for (int cntr = 0; cntr < juncsForLoop.Length; cntr++)
                        {
                            if (juncsForLoop[cntr].Chains.Count < 2)
                            {
                                // Nothing to reduce
                                continue;
                            }

                            // Find the outermost chain (getting rid of inner chains)
                            ReduceJunctions_Multiple_Reduce(juncsForLoop, cntr, points);
                        }

                        // Convert these junctions into a chain
                        Chain chain = ReduceJunctions_Multiple_FinalChain(juncsForLoop);
                        chains.Add(chain);
                    }

                    junctions.Clear();
                }

                private static void ReduceJunctions_Multiple_Interior(List<Junction> junctions, Point[] points)
                {
                    List<Tuple<int, int>> triedJunctions = new List<Tuple<int, int>>();

                    bool hadRemoval = false;
                    do
                    {
                        hadRemoval = false;

                        for (int cntr = 0; cntr < junctions.Count; cntr++)
                        {
                            #region See if should skip

                            if (junctions[cntr].Chains.Count < 2)
                            {
                                continue;
                            }

                            // Create a key to remember that this junction was tried
                            Tuple<int, int> key = Tuple.Create(junctions[cntr].FromPoint, junctions[cntr].ToPoint);
                            if (key.Item1 > key.Item2)
                            {
                                key = Tuple.Create(key.Item2, key.Item1);
                            }

                            if (triedJunctions.Contains(key))
                            {
                                continue;
                            }

                            triedJunctions.Add(key);

                            #endregion

                            // Get a combination of junctions that go between this junction's two points
                            int[] fragment = ReduceJunctions_Multiple_InteriorSprtFindFragment(cntr, junctions);

                            // Create a polygon out of this junction
                            Point[] poly = GetPolyFromTouchingChains(junctions[cntr].Chains[0], junctions[cntr].Chains[1]).Select(o => points[o]).ToArray();

                            // See if all points of the fragment are inside this polygon
                            if (ReduceJunctions_Multiple_InteriorSprtIsInside(poly, junctions, fragment, points))
                            {
                                // Remove these
                                foreach (int index in fragment.OrderByDescending(o => o))       // go backward sorted so removeat doesn't mess up the other indexes
                                {
                                    junctions.RemoveAt(index);
                                }

                                hadRemoval = true;
                                break;
                            }
                        }
                    } while (hadRemoval);
                }
                private static int[] ReduceJunctions_Multiple_InteriorSprtFindFragment(int index, List<Junction> junctions)
                {
                    //NOTE: This is a tweaked copy of GetChainsSprtSingle.  That one returns the indices of actual points, but this returns the indices of junctions

                    List<int> retVal = new List<int>();

                    int currentPoint = junctions[index].FromPoint;

                    while (currentPoint != junctions[index].ToPoint)
                    {
                        bool foundOne = false;

                        // Find the next junction
                        for (int cntr = 0; cntr < junctions.Count; cntr++)
                        {
                            if (cntr == index || retVal.Contains(cntr))
                            {
                                continue;
                            }

                            if (junctions[cntr].FromPoint == currentPoint)
                            {
                                retVal.Add(cntr);
                                currentPoint = junctions[cntr].ToPoint;
                                foundOne = true;
                                break;
                            }
                            else if (junctions[cntr].ToPoint == currentPoint)
                            {
                                retVal.Add(cntr);
                                currentPoint = junctions[cntr].FromPoint;
                                foundOne = true;
                                break;
                            }
                        }

                        if (!foundOne)
                        {
                            int[] points = UtilityHelper.Iterate(junctions.Select(o => o.FromPoint), junctions.Select(o => o.ToPoint)).Distinct().ToArray();
                            throw new ApplicationException(string.Format("Didn't find the point: {0}, in: {1}", currentPoint.ToString(), string.Join(" ", points)));
                        }
                    }

                    return retVal.ToArray();
                }
                private static bool ReduceJunctions_Multiple_InteriorSprtIsInside(Point[] poly, List<Junction> junctions, int[] fragment, Point[] points)
                {
                    foreach (Chain chain in fragment.Select(o => junctions[o]).SelectMany(o => o.Chains))
                    {
                        if (!Math2D.IsInsidePolygon(poly, GetPointInsideChain(chain, points), false))
                        {
                            // This one is outside
                            return false;
                        }
                    }

                    // They are all inside
                    return true;
                }

                private static Junction[] ReduceJunctions_Multiple_GetChain(int[] points, List<Junction> junctions)
                {
                    Junction[] retVal = new Junction[points.Length];

                    for (int cntr = 0; cntr < points.Length - 1; cntr++)
                    {
                        retVal[cntr] = ReduceJunctions_Multiple_GetChainSprtFind(points[cntr], points[cntr + 1], junctions);
                    }

                    retVal[points.Length - 1] = ReduceJunctions_Multiple_GetChainSprtFind(points[points.Length - 1], points[0], junctions);

                    return retVal;
                }
                private static Junction ReduceJunctions_Multiple_GetChainSprtFind(int from, int to, List<Junction> junctions)
                {
                    // Find the junction
                    Junction retVal = junctions.Where(o => (o.FromPoint == from && o.ToPoint == to) || (o.FromPoint == to && o.ToPoint == from)).FirstOrDefault();
                    if (retVal == null)
                    {
                        throw new ApplicationException("Didn't find the junction");
                    }

                    if (retVal.Chains[0].Points[0] != from)
                    {
                        // Reverse the chains
                        Chain[] chains = retVal.Chains.Select(o => new Chain(o.Points.Reverse().ToArray(), o.IsClosed)).ToArray();

                        retVal.Chains.Clear();
                        retVal.Chains.AddRange(chains);
                    }

                    // Exit Function
                    return retVal;
                }

                private static void ReduceJunctions_Multiple_Reduce(Junction[] chainOfJuncs, int index, Point[] points)
                {
                    // Create a point chain with all junctions except index
                    Point[] chainOthers = ReduceJunctions_Multiple_ReduceSprtOthersChain(chainOfJuncs, index, points);

                    int winningChain = -1;

                    // Try each chain to find the one that isn't inside the others
                    for (int outer = 0; outer < chainOfJuncs[index].Chains.Count; outer++)
                    {
                        Point[] chainComplete = ReduceJunctions_Multiple_ReduceSprtCompleteChain(chainOthers, chainOfJuncs[index].Chains[outer].Points, points);

                        // See if any of the other chains in this junction are inside the loop
                        bool hasOutside = false;
                        for (int inner = 0; inner < chainOfJuncs[index].Chains.Count; inner++)
                        {
                            if (inner == outer)
                            {
                                continue;
                            }

                            Point testPoint = GetPointInsideChain(chainOfJuncs[index].Chains[inner], points);
                            if (!Math2D.IsInsidePolygon(chainComplete, testPoint, null))
                            {
                                hasOutside = true;
                                break;
                            }
                        }

                        if (!hasOutside)
                        {
                            // Probably the winner (I think the junction only has two chains anyway by the time this method is called)
                            winningChain = outer;
                            break;
                        }
                    }

                    // Only keep the winning chain
                    if (winningChain < 0)
                    {
                        // If this becomes a problem, they are probably sitting on top of each other
                        throw new ApplicationException("Didn't find outer chain");
                    }

                    Chain winner = chainOfJuncs[index].Chains[winningChain];
                    chainOfJuncs[index].Chains.Clear();
                    chainOfJuncs[index].Chains.Add(winner);
                }
                private static Point[] ReduceJunctions_Multiple_ReduceSprtOthersChain(Junction[] chainOfJuncs, int index, Point[] points)
                {
                    List<Point> retVal = new List<Point>();

                    for (int outer = index + 1; outer < chainOfJuncs.Length; outer++)
                    {
                        int[] chain = chainOfJuncs[outer].Chains[0].Points;     // it doesn't matter which chain is used
                        for (int inner = 0; inner < chain.Length - 1; inner++)      // not using the last point in each chain, because the individual chains duplicate the endpoints
                        {
                            retVal.Add(points[chain[inner]]);
                        }
                    }

                    for (int outer = 0; outer < index; outer++)
                    {
                        int[] chain = chainOfJuncs[outer].Chains[0].Points;
                        for (int inner = 0; inner < chain.Length - 1; inner++)
                        {
                            retVal.Add(points[chain[inner]]);
                        }
                    }

                    return retVal.ToArray();
                }
                private static Point[] ReduceJunctions_Multiple_ReduceSprtCompleteChain(Point[] chainOthers, int[] chain, Point[] points)
                {
                    List<Point> retVal = new List<Point>();

                    // chainOthers was built so that this chain can go in front, and the combination of the two will make a complete loop
                    for (int cntr = 0; cntr < chain.Length - 1; cntr++)     // not using the last point in this chain, because the individual chains duplicate the endpoints
                    {
                        retVal.Add(points[chain[cntr]]);
                    }

                    retVal.AddRange(chainOthers);

                    return retVal.ToArray();
                }

                private static Chain ReduceJunctions_Multiple_FinalChain(Junction[] loopOfJunctions)
                {
                    List<int> points = new List<int>();

                    foreach (Junction junc in loopOfJunctions)
                    {
                        #region asserts
#if DEBUG
                        if (junc.Chains.Count != 1)
                        {
                            throw new ArgumentException("This method expects the junctions passed in to have exactly one chain each");
                        }
#endif
                        #endregion

                        for (int cntr = 0; cntr < junc.Chains[0].Points.Length - 1; cntr++)     // not using the last point in each chain, because it is the same as the first point in the next chain
                        {
                            points.Add(junc.Chains[0].Points[cntr]);
                        }
                    }

                    return new Chain(points.ToArray(), true);
                }

                private static void ReduceJunctions_Multiple_Cluster(List<Junction> junctions, List<Chain> chains, Point[] points)
                {
                    #region asserts
#if DEBUG
                    if (junctions.Count == 0)
                    {
                        throw new ArgumentException("Expected at least one junction passed in");
                    }

                    if (junctions.Any(o => o.Chains.Count > 2))
                    {
                        throw new ArgumentException("None of the junctions should have more than two chains");
                    }
#endif
                    #endregion

                    List<Junction> remaining = new List<Junction>(junctions);


                    // Find a point with more than one junction pointing at it
                    Tuple<int, Junction[]> multi1 = ReduceJunctions_Multiple_ClusterSprtFindMulti(junctions);

                    // Find another
                    Tuple<int, Junction[]> multi2 = ReduceJunctions_Multiple_ClusterSprtFindMulti(junctions);









                }
                private static Tuple<int, Junction[]> ReduceJunctions_Multiple_ClusterSprtFindMulti(List<Junction> junctions)
                {
                    // Find the point that has the most junctions pointing to it
                    var multi = UtilityHelper.Iterate(junctions.Select(o => o.FromPoint), junctions.Select(o => o.ToPoint)).
                        GroupBy(o => o).
                        Select(o => new { Point = o.Key, Count = o.Count() }).
                        Where(o => o.Count > 1).
                        OrderByDescending(o => o.Count).
                        FirstOrDefault();

                    if (multi == null)
                    {
                        throw new ArgumentException("Didn't find a point with multiple pointing to it");
                    }

                    // Pop off those junctions
                    List<Junction> matches = new List<Junction>();

                    int index = 0;
                    while (index < junctions.Count)
                    {
                        if (junctions[index].FromPoint == multi.Point || junctions[index].ToPoint == multi.Point)
                        {
                            matches.Add(junctions[index]);
                            junctions.RemoveAt(index);
                        }
                        else
                        {
                            index++;
                        }
                    }

                    // Exit Function
                    return Tuple.Create(multi.Point, matches.ToArray());
                }









                private static Chain ConvertStandaloneJunctionToChain(Junction junction)
                {
                    #region asserts

#if DEBUG
                    if (junction.Chains.Count != 2)
                    {
                        throw new ArgumentException("This function expects a junction containing exactly two chains");
                    }
#endif

                    #endregion

                    return new Chain(GetPolyFromTouchingChains(junction.Chains[0], junction.Chains[1]).ToArray(), true);
                }

                private static IEnumerable<int> GetPolyFromTouchingChains(Chain chain1, Chain chain2)
                {
                    #region asserts

#if DEBUG
                    if (chain1.IsClosed || chain2.IsClosed)
                    {
                        throw new ArgumentException("This method isn't meant for closed chains");
                    }
                    else if (chain1.Points[0] != chain2.Points[0] || chain1.Points[chain1.Points.Length - 1] != chain2.Points[chain2.Points.Length - 1])
                    {
                        throw new ArgumentException("The two chains need to start and stop on the same points");
                    }
#endif

                    #endregion

                    List<int> retVal = new List<int>();

                    // Start with the left chain
                    retVal.AddRange(chain1.Points);

                    // Tack on the right chain in reverse (but need to skip the first and last point in the second chain)
                    for (int cntr = chain2.Points.Length - 2; cntr > 0; cntr--)
                    {
                        retVal.Add(chain2.Points[cntr]);
                    }

                    // Exit Function
                    return retVal;
                }

                private static Polygon2D[] ConvertChainsToPolys(List<Chain> chains, Point[] points)
                {
                    // Convert each chain into a polygon
                    List<Point[]> polys = chains.Select(o =>
                            o.Points.Select(p => points[p]).ToArray()
                        ).ToList();

                    #region Determine parents

                    // Figure out what is inside of what
                    SortedList<int, List<int>> holeContainers = new SortedList<int, List<int>>();

                    for (int outer = 0; outer < polys.Count; outer++)
                    {
                        for (int inner = 0; inner < polys.Count; inner++)
                        {
                            if (outer == inner || holeContainers.ContainsKey(inner))
                            {
                                continue;
                            }

                            // Just compare one of the points of the inner poly to the outer poly
                            if (Math2D.IsInsidePolygon(polys[outer], polys[inner][0], null))
                            {
                                if (!holeContainers.ContainsKey(outer))
                                {
                                    holeContainers.Add(outer, new List<int>());
                                }

                                holeContainers[outer].Add(inner);
                            }
                        }
                    }

                    #endregion

                    int[] holes = holeContainers.Values.SelectMany(o => o).ToArray();
                    int[] standalones = Enumerable.Range(0, polys.Count).Where(o => !holeContainers.ContainsKey(o) && !holes.Contains(o)).ToArray();

                    #region Build return

                    List<Polygon2D> retVal = new List<Polygon2D>();

                    foreach (int index in standalones)
                    {
                        retVal.Add(new Polygon2D(polys[index]));
                    }

                    foreach (int index in holeContainers.Keys)
                    {
                        retVal.Add(new Polygon2D(polys[index], holeContainers[index].Select(o => polys[o]).ToArray()));
                    }

                    #endregion

                    return retVal.ToArray();
                }

                private static Point GetPointInsideChain(Chain chain, Point[] points)
                {
                    if (chain.Points.Length > 2)
                    {
                        return points[chain.Points[1]];
                    }

                    if (chain.Points.Length < 2)
                    {
                        throw new ArgumentException("The chain should have at least two points");
                    }

                    Point from = points[chain.Points[0]];
                    Point to = points[chain.Points[1]];

                    return from + ((to - from) * .5d);
                }

                #endregion
            }

            #endregion

            public static Polygon2D[] GetPolyUnion(Point[][] polygons)
            {
                if (polygons.Length == 0)
                {
                    return new Polygon2D[] { new Polygon2D(new Point[0]) };
                }
                else if (polygons.Length == 1)
                {
                    return new Polygon2D[] { new Polygon2D(polygons[0]) };
                }

                // See which edges are intersecting
                EdgeIntersection[] intersections = GetIntersections(polygons);
                if (intersections.Length == 0)
                {
                    return polygons.Select(o => new Polygon2D(o)).ToArray();
                }

                // Divide into islands of polygons
                var islands = GetIslands(polygons, intersections);

                List<Polygon2D> retVal = new List<Polygon2D>();

                // Union each island independently
                foreach (PolygonIsland island in islands)
                {
                    if (island.Polygons.Length == 1)
                    {
                        retVal.Add(new Polygon2D(island.Polygons[0]));       // there is only one polygon in this island (no intersections)
                    }
                    else
                    {
                        retVal.AddRange(MergeIsland(island));       // the island might actually be several polygons butted up next to each other
                    }
                }

                // Exit Function
                return retVal.ToArray();
            }

            #region Private Methods

            private static EdgeIntersection[] GetIntersections(Point[][] polygons)
            {
                List<EdgeIntersection> retVal = new List<EdgeIntersection>();

                for (int outer = 0; outer < polygons.Length - 1; outer++)
                {
                    for (int inner = outer + 1; inner < polygons.Length; inner++)
                    {
                        foreach (var intersection in GetIntersections(polygons[outer], polygons[inner]))
                        {
                            retVal.Add(new EdgeIntersection(outer, intersection.Item1, inner, intersection.Item2, intersection.Item3));
                        }
                    }
                }

                return retVal.ToArray();
            }
            private static Tuple<Edge2D, Edge2D, Point>[] GetIntersections(Point[] poly1, Point[] poly2)
            {
                List<Tuple<Edge2D, Edge2D, Point>> retVal = new List<Tuple<Edge2D, Edge2D, Point>>();

                foreach (Edge2D edge1 in Math2D.IterateEdges(poly1, null))
                {
                    foreach (Edge2D edge2 in Math2D.IterateEdges(poly2, null))
                    {
                        Point? intersect = Math2D.GetIntersection_LineSegment_LineSegment(edge1.Point0, edge1.Point1.Value, edge2.Point0, edge2.Point1.Value);
                        if (intersect != null)
                        {
                            retVal.Add(Tuple.Create(edge1, edge2, intersect.Value));
                        }
                    }
                }

                return retVal.ToArray();
            }

            private static PolygonIsland[] GetIslands(Point[][] polygons, EdgeIntersection[] intersections)
            {
                // Figure out which polygons are in which islands
                var islands = GetIslandsSprtIndices(polygons, intersections);

                if (islands.Length == 1)
                {
                    return new PolygonIsland[] { new PolygonIsland(polygons, intersections) };
                }

                PolygonIsland[] retVal = new PolygonIsland[islands.Length];

                // Make new intersections that only know about the polygons in their island
                for (int cntr = 0; cntr < retVal.Length; cntr++)
                {
                    List<Point[]> localPolys = new List<Point[]>();
                    SortedList<int, int> mapping = new SortedList<int, int>();

                    foreach (int index in islands[cntr].Item1)
                    {
                        mapping.Add(index, localPolys.Count);
                        localPolys.Add(polygons[index]);
                    }

                    EdgeIntersection[] localIntersections = islands[cntr].Item2.Select(o => new EdgeIntersection(mapping[o.Poly1Index], o.Poly1Edge, mapping[o.Poly2Index], o.Poly2Edge, o.IntersectionPoint)).ToArray();

                    retVal[cntr] = new PolygonIsland(localPolys.ToArray(), localIntersections);
                }

                // Exit Function
                return retVal;
            }

            private static Tuple<int[], EdgeIntersection[]>[] GetIslandsSprtIndices(Point[][] polygons, EdgeIntersection[] intersections)
            {
                List<Tuple<int[], EdgeIntersection[]>> retVal = new List<Tuple<int[], EdgeIntersection[]>>();
                List<int> remaining = Enumerable.Range(0, polygons.Length).ToList();

                while (remaining.Count > 0)
                {
                    // Find all the polygons that are touching this one
                    var currentIsland = GetIslandsSprtTouching(remaining[0], intersections);

                    // Remove these from the remaining list (after this, what's in remaining are polygons that aren't touching what's in retVal)
                    foreach (int index in currentIsland.Item1)
                    {
                        remaining.Remove(index);
                    }

                    // Store this island
                    retVal.Add(currentIsland);
                }

                // Exit Function
                return retVal.ToArray();
            }
            private static Tuple<int[], EdgeIntersection[]> GetIslandsSprtTouching(int index, EdgeIntersection[] intersections)
            {
                List<int> returnIndices = new List<int>();
                List<EdgeIntersection> returnIntersections = new List<EdgeIntersection>();

                Stack<int> newIndices = new Stack<int>();

                // Put the intersections in a list, so it can be removed from as matches are found
                List<EdgeIntersection> remainingIntersections = intersections.ToList();

                // Start the working stack off with the index passed in
                newIndices.Push(index);

                while (newIndices.Count > 0)
                {
                    // Pop off the stack, and look for matches
                    int currentIndex = newIndices.Pop();
                    returnIndices.Add(currentIndex);

                    // Find unique intersections that match this current polygon, and put them in the working stack if they haven't been encountered yet
                    foreach (int match in GetIslandsSprtTouchingSprtExamine(remainingIntersections, returnIntersections, currentIndex))
                    {
                        if (!returnIndices.Contains(match) && !newIndices.Contains(match))
                        {
                            newIndices.Push(match);
                        }
                    }
                }

                // Exit Function
                return Tuple.Create(returnIndices.ToArray(), returnIntersections.ToArray());
            }
            private static int[] GetIslandsSprtTouchingSprtExamine(List<EdgeIntersection> intersections, List<EdgeIntersection> matchedIntersections, int currentPoly)
            {
                List<int> retVal = new List<int>();

                int index = 0;
                while (index < intersections.Count)
                {
                    int otherPoly = -1;

                    if (intersections[index].Poly1Index == currentPoly)
                    {
                        otherPoly = intersections[index].Poly2Index;
                    }
                    else if (intersections[index].Poly2Index == currentPoly)
                    {
                        otherPoly = intersections[index].Poly1Index;
                    }
                    else
                    {
                        index++;
                        continue;
                    }

                    // This is a match.  Transfer it from the list of intersections so it's not looked at again
                    matchedIntersections.Add(intersections[index]);
                    intersections.RemoveAt(index);

                    // Add to return if unique
                    if (!retVal.Contains(otherPoly))
                    {
                        retVal.Add(otherPoly);
                    }
                }

                // Exit Function
                return retVal.ToArray();
            }

            private static Polygon2D[] MergeIsland(PolygonIsland island)
            {
                // Convert into line segments
                Tuple<Point, Point>[] allSegments = MergeIslandSprtSegments(island);

                List<Tuple<Point, bool>> mids;

                // Remove segments that are inside a polygon
                Tuple<Point, Point>[] segments = MergeIslandSprtRemoveInternal(out mids, allSegments, island.Polygons);

                // Stitch together the remaining segments
                //Point[][] polys = StitchSegments2.Stitch(segments);
                return StitchSegments2.Stitch(segments);

                //OLD
                //if (polys == null)
                //{
                //    //NOTE: This assumes that the segments form a convex hull, which is likely incorrect
                //    //retVal = GetConvexHull(segments);
                //    throw new ApplicationException("Fix this");
                //}

                //if (polys.Length == 1)
                //{
                //    return new Polygon2D[] { new Polygon2D(polys[0]) };
                //}
                //else
                //{
                //    return MergeIslandSprtFindAHoles(polys);
                //}
            }

            private static Tuple<Point, Point>[] MergeIslandSprtSegments(PolygonIsland island)
            {
                List<Tuple<Point, Point>> retVal = new List<Tuple<Point, Point>>();

                // Shoot through all the polygons in this island
                for (int polyCntr = 0; polyCntr < island.Polygons.Length; polyCntr++)
                {
                    // Shoot through each edge of this polygon
                    foreach (Edge2D edge in Math2D.IterateEdges(island.Polygons[polyCntr], null))
                    {
                        // Get all intersections of this edge
                        EdgeIntersection[] intersections = MergeIslandSprtSegmentsSprtIntersections(polyCntr, edge, island.Intersections);

                        if (intersections.Length == 0)
                        {
                            // Nothing is intersecting this edge, so add the entire edge
                            retVal.Add(Tuple.Create(edge.Point0, edge.Point1Ext));
                        }
                        else
                        {
                            // This edge has intersections.  Convert into segments
                            retVal.AddRange(MergeIslandSprtSegmentsSprtDivide(polyCntr, edge, intersections));
                        }
                    }
                }

                // Exit Function
                return retVal.ToArray();
            }

            private static EdgeIntersection[] MergeIslandSprtSegmentsSprtIntersections(int polygonIndex, Edge2D edge, EdgeIntersection[] intersections)
            {
                List<EdgeIntersection> retVal = new List<EdgeIntersection>();

                foreach (EdgeIntersection intersection in intersections)
                {
                    if (intersection.Poly1Index == polygonIndex)        //  polygon match (1)
                    {
                        if (MergeIslandSprtSegmentsSprtIntersectionsSprtIsSame(edge.Index0, edge.Index1.Value, intersection.Poly1Edge.Index0, intersection.Poly1Edge.Index1.Value))     // edge match (1)
                        {
                            retVal.Add(intersection);
                        }
                    }
                    else if (intersection.Poly2Index == polygonIndex)       //  polygon match (2)
                    {
                        if (MergeIslandSprtSegmentsSprtIntersectionsSprtIsSame(edge.Index0, edge.Index1.Value, intersection.Poly2Edge.Index0, intersection.Poly2Edge.Index1.Value))     // edge match (2)
                        {
                            retVal.Add(intersection);
                        }
                    }
                }

                return retVal.ToArray();
            }
            private static bool MergeIslandSprtSegmentsSprtIntersectionsSprtIsSame(int edge0Index0, int edge0Index1, int edge1Index0, int edge1Index1)
            {
                // 0[0] could be the same as 1[0] or 1[1], 0[1] could match the other.  So use math.min and math.max to
                // reduce the number of compares
                return Math.Min(edge0Index0, edge0Index1) == Math.Min(edge1Index0, edge1Index1) &&
                    Math.Max(edge0Index0, edge0Index1) == Math.Max(edge1Index0, edge1Index1);
            }

            private static IEnumerable<Tuple<Point, Point>> MergeIslandSprtSegmentsSprtDivide(int polyCntr, Edge2D edge, EdgeIntersection[] intersections)
            {
                // Build vectors from edge.p0 to each intersection, sort by length
                var distances = Enumerable.Range(0, intersections.Length).
                    Select(o => new { Point = intersections[o].IntersectionPoint, Distance = (intersections[o].IntersectionPoint - edge.Point0).LengthSquared }).
                    OrderBy(o => o.Distance).
                    ToArray();

                List<Tuple<Point, Point>> retVal = new List<Tuple<Point, Point>>();

                // Point0 to first intersect
                retVal.Add(Tuple.Create(
                    edge.Point0,
                    distances[0].Point));

                // Intersect to Intersect
                for (int cntr = 0; cntr < distances.Length - 1; cntr++)
                {
                    retVal.Add(Tuple.Create(
                        distances[cntr].Point,
                        distances[cntr + 1].Point));
                }

                // Last intersect to Point1
                retVal.Add(Tuple.Create(
                    distances[distances.Length - 1].Point,
                    edge.Point1Ext));

                return retVal;
            }
            private static IEnumerable<Tuple<Point, Point>> MergeIslandSprtSegmentsSprtDivide_OLD(int polyCntr, Edge2D edge, EdgeIntersection[] intersections)
            {
                // Build vectors from edge.p0 to each intersection, sort by length
                var distances = Enumerable.Range(0, intersections.Length).
                    Select(o => new { Index = o, Distance = (intersections[o].IntersectionPoint - edge.Point0).LengthSquared }).
                    OrderBy(o => o.Distance).
                    ToArray();

                List<Tuple<Point, Point>> retVal = new List<Tuple<Point, Point>>();

                // Point0 to first intersect
                retVal.Add(Tuple.Create(
                    edge.Point0,
                    intersections[distances[0].Index].IntersectionPoint));

                // Intersect to Intersect
                for (int cntr = 0; cntr < distances.Length - 1; cntr++)
                {
                    retVal.Add(Tuple.Create(
                        intersections[distances[cntr].Index].IntersectionPoint,
                        intersections[distances[cntr + 1].Index].IntersectionPoint));
                }

                // Last intersect to Point1
                retVal.Add(Tuple.Create(
                    intersections[distances[distances.Length - 1].Index].IntersectionPoint,
                    edge.Point1Ext));

                return retVal;
            }

            private static Tuple<Point, Point>[] MergeIslandSprtRemoveInternal(out List<Tuple<Point, bool>> mids, Tuple<Point, Point>[] segments, Point[][] polygons)
            {
                List<Tuple<Point, Point>> retVal = new List<Tuple<Point, Point>>();

                mids = new List<Tuple<Point, bool>>();

                foreach (var segment in segments)
                {
                    bool isInside = false;

                    // Since the from and to of this segment sits on the edge of polygons, it is difficult to test.  Using the midpoint
                    // takes out guesswork
                    Point midPoint = segment.Item1 + ((segment.Item2 - segment.Item1) * .5d);

                    // Test against each polygon
                    foreach (Point[] polygon in polygons)
                    {
                        if (Math2D.IsInsidePolygon(polygon, midPoint, false))
                        {
                            isInside = true;
                            break;
                        }
                    }

                    mids.Add(Tuple.Create(midPoint, isInside));

                    if (!isInside)
                    {
                        // This defines an outside edge
                        retVal.Add(segment);
                    }
                }

                // Exit Function
                return retVal.ToArray();
            }

            //TODO: This method is too simple.  There could be multiple polygons each with their own holes
            private static Polygon2D[] MergeIslandSprtFindAHoles(Point[][] polygons)
            {
                if (polygons.Length < 2)
                {
                    throw new ArgumentException("At least two polygons need to be passed to this method: " + polygons.Length.ToString());
                }

                // Find the parent, the rest will be holes
                int parentIndex = MergeIslandSprtFindAHolesSprtParent(polygons);
                if (parentIndex < 0)
                {
                    // Didn't find a parent.  Return each as its own polygon
                    //TODO: These are likely touching.  Try to do a better job of coming up with a single polygon
                    return polygons.Select(o => new Polygon2D(o)).ToArray();
                }

                List<Point[]> holes = new List<Point[]>(polygons);
                holes.RemoveAt(parentIndex);

                return new Polygon2D[] { new Polygon2D(polygons[parentIndex], holes.ToArray()) };
            }
            private static int MergeIslandSprtFindAHolesSprtParent(Point[][] polygons)
            {
                for (int outer = 0; outer < polygons.Length; outer++)
                {
                    for (int inner = 0; inner < polygons.Length; inner++)
                    {
                        if (outer == inner)
                        {
                            continue;
                        }

                        // Just compare one of the points of the inner poly to the outer poly
                        if (Math2D.IsInsidePolygon(polygons[outer], polygons[inner][0], null))
                        {
                            // Found it
                            //NOTE: This method assumes there is only one parent, and the rest are inside it.  It also assumes that each of the holes
                            //are independent (don't touch/intersect each other)
                            return outer;
                        }
                    }
                }

                return -1;
            }

            #endregion
        }

        #endregion
        #region Class: Clipper

        // Got this here:
        // http://sourceforge.net/projects/polyclipping/

        private static class ClipperHelper
        {
            public static Polygon2D[] GetPolyUnion(Point[][] polygons)
            {
                if (polygons == null || polygons.Length == 0)
                {
                    return new Polygon2D[0];
                }

                double scale = GetScale(polygons);
                var convertedPolys = ConvertInput(polygons, scale);

                Clipper clipper = new Clipper();
                clipper.AddPolygons(convertedPolys, PolyType.ptSubject);        // when doing union, I don't think it matters what is subject and what is union
                //clipper.ForceSimple = true;

                // Here is a page describing PolyFillType (nonzero is what you intuitively think of for a union)
                // http://www.angusj.com/delphi/clipper/documentation/Docs/Units/ClipperLib/Types/PolyFillType.htm

                PolyTree solution = new PolyTree();
                if (!clipper.Execute(ClipType.ctUnion, solution, PolyFillType.pftNonZero, PolyFillType.pftNonZero))
                {
                    return new Polygon2D[0];
                }

                return ConvertOutput(solution, 1d / scale);
            }

            #region Private Methods

            private static double GetScale(Point[][] polygons)
            {
                // I was going to go with massively large scale to use most of the range of int64, but there was a comment that says he
                // caps at +- 1.5 billion

                //var aabb = Math2D.GetAABB(polygons.SelectMany(o => o));
                //double max = Math3D.Max(aabb.Item1.X, aabb.Item1.Y, aabb.Item2.X, aabb.Item2.Y);


                //TODO: Don't scale if aabb is larger than some value
                //TODO: Scale a lot if there are a lot of points less than .001


                return 10000d;
            }

            private static List<List<IntPoint>> ConvertInput(Point[][] polygons, double scale)
            {
                List<List<IntPoint>> retVal = new List<List<IntPoint>>();

                // The union method flakes out if the polygons have different windings (clockwise vs counter clockwise)
                Vector3D normal = Math2D.GetPolygonNormal(polygons[0], PolygonNormalLength.DoesntMatter);

                for (int cntr = 0; cntr < polygons.Length; cntr++)
                {
                    Point[] points = polygons[cntr];

                    if (cntr > 0)       // no need to compare the first poly with itself
                    {
                        Vector3D normal2 = Math2D.GetPolygonNormal(points, PolygonNormalLength.DoesntMatter);

                        if (Vector3D.DotProduct(normal, normal2) < 0)
                        {
                            // This is wound the wrong direction.  Reverse it
                            points = points.Reverse().ToArray();
                        }
                    }

                    // Convert into the custom poly format
                    retVal.Add(points.Select(o => new IntPoint(Convert.ToInt64(o.X * scale), Convert.ToInt64(o.Y * scale))).ToList());
                }

                return retVal;
            }

            /// <summary>
            /// Convert into an array of polygons
            /// </summary>
            /// <remarks>
            /// The polytree will nest deeply if solids are inside of holes.  But Polygon2D would treat the solids inside of holes as their own
            /// independent isntances
            /// 
            /// http://www.angusj.com/delphi/clipper/documentation/Docs/Units/ClipperLib/Classes/PolyTree/_Body.htm
            /// </remarks>
            private static Polygon2D[] ConvertOutput(PolyTree solution, double scaleInverse)
            {
                List<Polygon2D> retVal = new List<Polygon2D>();

                // Walk the tree, and get all parents (need to look at contour count so that the root gets skipped)
                foreach (PolyNode parent in ((PolyNode)solution).Descendants(o => o.Childs).Where(o => !o.IsHole && o.Contour.Count > 0))
                {
                    // Convert the parent polygon
                    Point[] points = parent.Contour.Select(o => new Point(o.X * scaleInverse, o.Y * scaleInverse)).ToArray();

                    if (parent.Childs.Count == 0)
                    {
                        // No holes
                        retVal.Add(new Polygon2D(points));
                    }
                    else
                    {
                        List<Point[]> holes = new List<Point[]>();

                        foreach (PolyNode child in parent.Childs)
                        {
                            if (!child.IsHole)
                            {
                                throw new ApplicationException("Expected the child of a non hole to be a hole");
                            }

                            // Convert the hole polygon
                            holes.Add(child.Contour.Select(o => new Point(o.X * scaleInverse, o.Y * scaleInverse)).ToArray());
                        }

                        // Store with holes
                        retVal.Add(new Polygon2D(points, holes.ToArray()));
                    }
                }

                // Exit Function
                return retVal.ToArray();
            }

            #endregion
        }

        #endregion

        #region Declaration Section

        private EditorOptions _editorOptions = new EditorOptions();
        private ItemOptions _itemOptions = new ItemOptions();

        private World _world = null;
        private RadiationField _radiation = null;
        private GravityFieldUniform _gravity = null;

        /// <summary>
        /// This listens to the mouse/keyboard and controls the camera
        /// </summary>
        private TrackBallRoam _trackball = null;

        // Initial triangles
        private Visual3D _initialVisual = null;
        private TriangleVisual[] _initialTriangles = null;

        // Collided triangles
        private Visual3D _collidedVisual = null;
        private TriangleVisual[] _collidedTriangles = null;

        private List<Visual3D> _debugVisuals = new List<Visual3D>();

        #endregion

        #region Constructor

        public ShadowsWindow()
        {
            InitializeComponent();

            //trkReflections_ValueChanged(this, new RoutedPropertyChangedEventArgs<double>(0, 0));
            //trkPercentAngle_ValueChanged(this, new RoutedPropertyChangedEventArgs<double>(0, 0));
        }

        #endregion

        #region Event Listeners

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                #region Trackball

                // Trackball
                _trackball = new TrackBallRoam(_camera);
                _trackball.KeyPanScale = 1d;
                _trackball.EventSource = grdViewPort;		//NOTE:  If this control doesn't have a background color set, the trackball won't see events (I think transparent is ok, just not null)
                _trackball.AllowZoomOnMouseWheel = true;

                #region copied from MouseComplete_NoLeft - middle button changed

                TrackBallMapping complexMapping = null;

                // Middle Button
                complexMapping = new TrackBallMapping(CameraMovement.RotateAroundLookDirection_AutoScroll);
                complexMapping.Add(MouseButton.Middle);
                complexMapping.Add(new Key[] { Key.LeftCtrl, Key.RightCtrl });
                complexMapping.Add(new Key[] { Key.LeftAlt, Key.RightAlt });
                _trackball.Mappings.Add(complexMapping);
                _trackball.Mappings.Add(new TrackBallMapping(CameraMovement.RotateAroundLookDirection, MouseButton.Middle, new Key[] { Key.LeftCtrl, Key.RightCtrl }));

                complexMapping = new TrackBallMapping(CameraMovement.Zoom_AutoScroll);
                complexMapping.Add(MouseButton.Middle);
                complexMapping.Add(new Key[] { Key.LeftShift, Key.RightShift });
                complexMapping.Add(new Key[] { Key.LeftAlt, Key.RightAlt });
                _trackball.Mappings.Add(complexMapping);
                _trackball.Mappings.Add(new TrackBallMapping(CameraMovement.Zoom, MouseButton.Middle, new Key[] { Key.LeftShift, Key.RightShift }));

                //retVal.Add(new TrackBallMapping(CameraMovement.Pan_AutoScroll, MouseButton.Middle));
                _trackball.Mappings.Add(new TrackBallMapping(CameraMovement.Pan, MouseButton.Middle));

                // Left+Right Buttons (emulate middle)
                complexMapping = new TrackBallMapping(CameraMovement.RotateAroundLookDirection_AutoScroll);
                complexMapping.Add(MouseButton.Left);
                complexMapping.Add(MouseButton.Right);
                complexMapping.Add(new Key[] { Key.LeftCtrl, Key.RightCtrl });
                complexMapping.Add(new Key[] { Key.LeftAlt, Key.RightAlt });
                _trackball.Mappings.Add(complexMapping);

                complexMapping = new TrackBallMapping(CameraMovement.RotateAroundLookDirection);
                complexMapping.Add(MouseButton.Left);
                complexMapping.Add(MouseButton.Right);
                complexMapping.Add(new Key[] { Key.LeftCtrl, Key.RightCtrl });
                _trackball.Mappings.Add(complexMapping);

                complexMapping = new TrackBallMapping(CameraMovement.Zoom_AutoScroll);
                complexMapping.Add(MouseButton.Left);
                complexMapping.Add(MouseButton.Right);
                complexMapping.Add(new Key[] { Key.LeftShift, Key.RightShift });
                complexMapping.Add(new Key[] { Key.LeftAlt, Key.RightAlt });
                _trackball.Mappings.Add(complexMapping);

                complexMapping = new TrackBallMapping(CameraMovement.Zoom);
                complexMapping.Add(MouseButton.Left);
                complexMapping.Add(MouseButton.Right);
                complexMapping.Add(new Key[] { Key.LeftShift, Key.RightShift });
                _trackball.Mappings.Add(complexMapping);

                //complexMapping = new TrackBallMapping(CameraMovement.Pan_AutoScroll);
                complexMapping = new TrackBallMapping(CameraMovement.Pan);
                complexMapping.Add(MouseButton.Left);
                complexMapping.Add(MouseButton.Right);
                _trackball.Mappings.Add(complexMapping);

                // Right Button
                complexMapping = new TrackBallMapping(CameraMovement.RotateInPlace_AutoScroll);
                complexMapping.Add(MouseButton.Right);
                complexMapping.Add(new Key[] { Key.LeftCtrl, Key.RightCtrl });
                complexMapping.Add(new Key[] { Key.LeftAlt, Key.RightAlt });
                _trackball.Mappings.Add(complexMapping);
                _trackball.Mappings.Add(new TrackBallMapping(CameraMovement.RotateInPlace, MouseButton.Right, new Key[] { Key.LeftCtrl, Key.RightCtrl }));

                _trackball.Mappings.Add(new TrackBallMapping(CameraMovement.Orbit_AutoScroll, MouseButton.Right, new Key[] { Key.LeftAlt, Key.RightAlt }));
                _trackball.Mappings.Add(new TrackBallMapping(CameraMovement.Orbit, MouseButton.Right));

                #endregion

                //_trackball.Mappings.AddRange(TrackBallMapping.GetPrebuilt(TrackBallMapping.PrebuiltMapping.Keyboard_ASDW_In));		// let the ship get asdw instead of the camera
                //_trackball.GetOrbitRadius += new GetOrbitRadiusHandler(Trackball_GetOrbitRadius);
                _trackball.ShouldHitTestOnOrbit = true;

                #endregion

                #region Init World

                _world = new World();

                double halfSize = 200d;
                List<Point3D[]> innerLines, outerLines;
                _world.SetCollisionBoundry(out innerLines, out outerLines, new Point3D(-halfSize, -halfSize, -halfSize), new Point3D(halfSize, halfSize, halfSize));

                #endregion
                #region Fields

                _radiation = new RadiationField();
                _radiation.AmbientRadiation = 1d;

                _gravity = new GravityFieldUniform();

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Window_Closed(object sender, EventArgs e)
        {

        }

        private void btnRandom5_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearAllVisuals();

                PartBase[] parts = GetRandomParts(5);

                #region Collided

                var partVisuals = BreakUpPartVisuals(parts);

                foreach (TriangleVisual triangle in partVisuals.Item2)
                {
                    triangle.Color = Colors.Transparent;
                }

                if (chkShowCollisions.IsChecked.Value)
                {
                    _viewport.Children.Add(partVisuals.Item1);
                }

                _collidedVisual = partVisuals.Item1;
                _collidedTriangles = partVisuals.Item2;

                #endregion

                #region Initial (shown after because of transparency)

                partVisuals = BreakUpPartVisuals(parts);

                foreach (TriangleVisual triangle in partVisuals.Item2)
                {
                    triangle.Color = UtilityWPF.ColorFromHex("10808080");
                }

                if (chkShowInitial.IsChecked.Value)
                {
                    _viewport.Children.Add(partVisuals.Item1);
                }

                _initialVisual = partVisuals.Item1;
                _initialTriangles = partVisuals.Item2;

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnSlice1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearAllVisuals();

                // Get some triangles
                //Triangle[] orig = GetRandomTrianglesOnion(10, 100, 5);        // this just makes a mess
                ITriangle[] orig = GetRandomTrianglesPlate(20d, 3);

                //Visual3D visual = GetTriangleVisual(orig, UtilityWPF.ColorFromHex("D0D0D0"));
                //_debugVisuals.Add(visual);
                //_viewport.Children.Add(visual);

                ITriangle[] sliced = Math3D.SliceLargeTriangles(orig, 8d);
                var slicedIndexed = TriangleIndexedThreadsafe.ConvertToIndexed(sliced, false);

                foreach (ITriangle triangle in slicedIndexed)
                {
                    Visual3D visual = GetVisual_Triangle(new ITriangle[] { triangle }, UtilityWPF.GetRandomColor(255, 128, 192));
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                //TODO: Draw lines of sliced triangles


                #region FLAWED

                // Find the smallest one
                //double smallestArea = orig.Min(o => o.NormalLength);

                // Divide thin triangles
                //ITriangle[] noTinStrips = SliceThinTriangles(orig);

                // Bisect or trisect any so that none are larger than X times the smallest

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnPlaneDist_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearAllVisuals();

                // Get some triangles
                TriangleIndexed[] triangles = TriangleIndexed.ConvertToIndexed(Math3D.SliceLargeTriangles(GetRandomTrianglesOnion(4, 30, 5), 8d));

                // Order by distance to plane
                ITriangleIndexed[] sorted = Math3D.SortByPlaneDistance(triangles, _camera.Position, _camera.LookDirection);

                // Draw them
                Color frontColor = Colors.Gold;
                Color backColor = Colors.DarkViolet;

                Visual3D visual;

                for (int cntr = 0; cntr < sorted.Length; cntr++)
                {
                    visual = GetVisual_Triangle(new ITriangle[] { sorted[cntr] }, UtilityWPF.AlphaBlend(backColor, frontColor, Convert.ToDouble(cntr) / Convert.ToDouble(sorted.Length)));
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                // Draw the plane
                visual = GetVisual_Triangle(GetPlane(_camera.Position, _camera.LookDirection, 60), UtilityWPF.ColorFromHex("20B0B0B0"));
                _debugVisuals.Add(visual);
                _viewport.Children.Add(visual);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnPointOnTriangleEdge_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearAllVisuals();

                // Make a random triangle
                Triangle triangle = GetRandomTrianglesOnion(4, 30, 1)[0];

                // Pick a random spot on that triangle
                Vector bary = new Vector(StaticRandom.NextDouble(), StaticRandom.NextDouble());
                if (bary.X + bary.Y > 1d)
                {
                    bary = new Vector(1d - bary.X, 1d - bary.Y);
                }
                Point3D pointOnTriangle = Math3D.FromBarycentric(triangle, bary);

                // Pick a random spot in space to tell what direction to go in
                Point3D guidePoint = Math3D.GetRandomVectorSpherical(30).ToPoint();
                //Point3D guidePoint = pointOnTriangle + triangle.Normal;       // used to test for a perpendicular guide direction

                // Find the point on the triangle's edge
                var pointOnEdge = HullTriangleIntersect.GetPointOnTriangleEdge(triangle, pointOnTriangle, guidePoint);

                #region Draw

                Visual3D visual;

                double dotSize = .1d;

                // Points
                visual = GetVisual_Dot(pointOnTriangle, dotSize, Colors.DodgerBlue);
                _debugVisuals.Add(visual);
                _viewport.Children.Add(visual);

                if (pointOnEdge != null)
                {
                    visual = GetVisual_Dot(pointOnEdge.Item2, dotSize, Colors.DodgerBlue);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                visual = GetVisual_Dot(guidePoint, dotSize, Colors.DimGray);
                _debugVisuals.Add(visual);
                _viewport.Children.Add(visual);


                // Lines
                ScreenSpaceLines3D line = new ScreenSpaceLines3D();
                line.Thickness = 2d;
                line.Color = Colors.Silver;
                line.AddLine(pointOnTriangle, guidePoint);
                _debugVisuals.Add(line);
                _viewport.Children.Add(line);

                line = new ScreenSpaceLines3D();
                line.Thickness = 2d;
                line.Color = Colors.DodgerBlue;

                if (pointOnEdge != null)
                {
                    line.AddLine(pointOnEdge.Item2, pointOnTriangle);
                }

                _debugVisuals.Add(line);
                _viewport.Children.Add(line);


                // Triangle
                Color color = UtilityWPF.ColorFromHex("80E0E0E0");

                visual = GetVisual_Triangle(new ITriangle[] { triangle }, color);
                _debugVisuals.Add(visual);
                _viewport.Children.Add(visual);

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnPointOnTriangleEdge2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearAllVisuals();

                // Make a triangle
                Triangle triangle = new Triangle(new Point3D(0.50, -0.50, 1.00), new Point3D(0.50, -0.50, -1.00), new Point3D(-0.10, 0.60, -1.00));

                // Pick a spot on that triangle
                Point3D pointOnTriangle = new Point3D(0.22727272727272729, 0, 0);

                // Pick a spot in space to tell what direction to go in
                Point3D guidePoint = new Point3D(0.5, 0.5, 0);

                // Find the point on the triangle's edge
                var pointOnEdge = HullTriangleIntersect.GetPointOnTriangleEdge(triangle, pointOnTriangle, guidePoint);



                Vector3D guideDirection = (guidePoint - pointOnTriangle).GetProjectedVector(triangle);
                if (Math3D.IsInvalid(guideDirection))
                {
                    // Guide guideDirection is perpendicular to the triangle
                    MessageBox.Show("perpendicular", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Point3D guidePoint2 = pointOnTriangle + guideDirection;





                #region Draw

                Visual3D visual;

                double dotSize = .01d;

                // Points
                visual = GetVisual_Dot(pointOnTriangle, dotSize, Colors.DodgerBlue);
                _debugVisuals.Add(visual);
                _viewport.Children.Add(visual);

                if (pointOnEdge != null)
                {
                    visual = GetVisual_Dot(pointOnEdge.Item2, dotSize, Colors.DodgerBlue);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                visual = GetVisual_Dot(guidePoint, dotSize, Colors.DimGray);
                _debugVisuals.Add(visual);
                _viewport.Children.Add(visual);

                visual = GetVisual_Dot(guidePoint2, dotSize, Colors.Olive);
                _debugVisuals.Add(visual);
                _viewport.Children.Add(visual);


                // Lines
                ScreenSpaceLines3D line = new ScreenSpaceLines3D();
                line.Thickness = 2d;
                line.Color = Colors.Silver;
                line.AddLine(pointOnTriangle, guidePoint);
                _debugVisuals.Add(line);
                _viewport.Children.Add(line);


                line = new ScreenSpaceLines3D();
                line.Thickness = 2d;
                line.Color = Colors.Olive;
                line.AddLine(pointOnTriangle, guidePoint2);
                _debugVisuals.Add(line);
                _viewport.Children.Add(line);


                line = new ScreenSpaceLines3D();
                line.Thickness = 2d;
                line.Color = Colors.DodgerBlue;

                if (pointOnEdge != null)
                {
                    line.AddLine(pointOnEdge.Item2, pointOnTriangle);
                }

                _debugVisuals.Add(line);
                _viewport.Children.Add(line);


                // Triangle
                Color color = UtilityWPF.ColorFromHex("80E0E0E0");

                visual = GetVisual_Triangle(new ITriangle[] { triangle }, color);
                _debugVisuals.Add(visual);
                _viewport.Children.Add(visual);

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnShadowSingleStep_Click(object sender, RoutedEventArgs e)
        {
            int debugStep = 0;

            try
            {
                debugStep++;

                ClearAllVisuals();

                debugStep++;

                Point3D cameraPos = _camera.Position;
                Vector3D cameraLook = _camera.LookDirection;

                debugStep++;

                // Get some triangles
                TriangleIndexed[] triangles = TriangleIndexed.ConvertToIndexed(Math3D.SliceLargeTriangles(GetRandomTrianglesOnion(4, 30, 5), 8d));

                debugStep++;

                // Order by distance to plane
                ITriangleIndexed[] sorted = Math3D.SortByPlaneDistance(triangles, cameraPos, cameraLook);

                debugStep++;

                // Create a volume out of the nearest triangle
                ITriangleIndexed[] hull = GetHullFromTriangle(sorted[0], cameraLook.ToUnit() * 50);

                debugStep++;

                List<Point3D[]> clipPolygons = new List<Point3D[]>();

                debugStep++;

                if (hull != null && hull.Length > 6)        // if the triangle is super thin, then a 2D hull will be created
                {
                    for (int cntr = 1; cntr < sorted.Length; cntr++)
                    {
                        Point3D[] poly = HullTriangleIntersect2.GetIntersection_Hull_Triangle(hull, sorted[cntr]);
                        if (poly != null)
                        {
                            clipPolygons.Add(poly);
                        }
                    }
                }

                debugStep++;

                #region Draw

                Visual3D visual;

                // Clip polygons
                foreach (Point3D[] polygon in clipPolygons)
                {
                    visual = GetVisual_PolygonHull(polygon, Colors.DodgerBlue, .005);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                debugStep++;

                // Triangles
                Color color = UtilityWPF.ColorFromHex("50E0E0E0");

                for (int cntr = 1; cntr < sorted.Length; cntr++)        // don't draw the triangle that is part of the hull
                {
                    visual = GetVisual_Triangle(new ITriangle[] { sorted[cntr] }, color);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                debugStep++;

                // Shadow hull
                if (hull != null)
                {
                    color = UtilityWPF.AlphaBlend(Colors.Transparent, Colors.DodgerBlue, .85d);

                    // Material
                    MaterialGroup materials = new MaterialGroup();
                    materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
                    materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(Colors.White, color, .5d)), 30d));

                    // Geometry Model
                    GeometryModel3D geometry = new GeometryModel3D();
                    geometry.Material = materials;
                    geometry.BackMaterial = materials;
                    geometry.Geometry = UtilityWPF.GetMeshFromTriangles(hull);

                    ModelVisual3D modelVisual = new ModelVisual3D();
                    modelVisual.Content = geometry;

                    visual = modelVisual;

                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }


                debugStep++;


                //// Draw the plane
                //visual = GetTriangleVisual(GetPlane(_camera.Position, _camera.LookDirection, 60), UtilityWPF.ColorFromHex("20B0B0B0"));
                //_debugVisuals.Add(visual);
                //_viewport.Children.Add(visual);

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnShadowMultiStep_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearAllVisuals();

                Point3D cameraPos = _camera.Position;
                Vector3D cameraLook = _camera.LookDirection;

                // Get some triangles
                TriangleIndexed[] triangles = TriangleIndexed.ConvertToIndexed(Math3D.SliceLargeTriangles(GetRandomTrianglesOnion(4, 30, 5), 8d));

                ShadowMultiStepSprtFinish(cameraPos, cameraLook, triangles);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnShadowMultiStep2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearAllVisuals();

                Point3D cameraPos = _camera.Position;
                Vector3D cameraLook = _camera.LookDirection;

                // Get some triangles
                TriangleIndexed[] triangles = TriangleIndexed.ConvertToIndexed(GetRandomTrianglesOnion(4, 30, 5));

                ShadowMultiStepSprtFinish(cameraPos, cameraLook, triangles);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnShadowFixed1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearAllVisuals();

                Triangle triangle = new Triangle(new Point3D(0, 0, 0), new Point3D(1, 0, 0), new Point3D(0, 1, 0));

                ITriangleIndexed[] hull = GetHullFromTriangle(
                    new Triangle(new Point3D(-.75, -.5, -1), new Point3D(.5, -.5, -1), new Point3D(-.1, .6, -1)),
                    new Vector3D(0, 0, 2));

                //Point3D[] clipPolygon = HullTriangleIntersect.GetIntersection(hull, triangle);
                Point3D[] clipPolygon = HullTriangleIntersect2.GetIntersection_Hull_Triangle(hull, triangle);

                #region Draw

                Visual3D visual;

                // Clip polygons
                if (clipPolygon != null)
                {
                    visual = GetVisual_PolygonHull(clipPolygon, Colors.DodgerBlue, .005);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                // Triangle
                Color color = UtilityWPF.ColorFromHex("50E0E0E0");

                visual = GetVisual_Triangle(new ITriangle[] { triangle }, color);
                _debugVisuals.Add(visual);
                _viewport.Children.Add(visual);

                // Shadow hull

                color = UtilityWPF.AlphaBlend(Colors.Transparent, Colors.DodgerBlue, .85d);

                // Material
                MaterialGroup materials = new MaterialGroup();
                materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
                materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(Colors.White, color, .5d)), 30d));

                // Geometry Model
                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = materials;
                geometry.BackMaterial = materials;
                geometry.Geometry = UtilityWPF.GetMeshFromTriangles(hull.ToArray());

                ModelVisual3D modelVisual = new ModelVisual3D();
                modelVisual.Content = geometry;

                visual = modelVisual;

                _debugVisuals.Add(visual);
                _viewport.Children.Add(visual);


                //// Draw the plane
                //visual = GetTriangleVisual(GetPlane(_camera.Position, _camera.LookDirection, 60), UtilityWPF.ColorFromHex("20B0B0B0"));
                //_debugVisuals.Add(visual);
                //_viewport.Children.Add(visual);

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnShadowFixed2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearAllVisuals();

                Triangle triangle = new Triangle(new Point3D(0, 0, 0), new Point3D(1, -.25, 0), new Point3D(-.6, .6, 0));

                ITriangleIndexed[] hull = GetHullFromTriangle(
                    new Triangle(new Point3D(-.75, -.5, -1), new Point3D(.5, -.5, -1), new Point3D(-.1, .6, -1)),
                    new Vector3D(0, 0, 2));

                //Point3D[] clipPolygon = HullTriangleIntersect.GetIntersection(hull, triangle);
                Point3D[] clipPolygon = HullTriangleIntersect2.GetIntersection_Hull_Triangle(hull, triangle);

                #region Draw

                Visual3D visual;

                // Clip polygons
                if (clipPolygon != null)
                {
                    visual = GetVisual_PolygonHull(clipPolygon, Colors.DodgerBlue, .005);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                // Triangle
                Color color = UtilityWPF.ColorFromHex("50E0E0E0");

                visual = GetVisual_Triangle(new ITriangle[] { triangle }, color);
                _debugVisuals.Add(visual);
                _viewport.Children.Add(visual);

                // Shadow hull

                color = UtilityWPF.AlphaBlend(Colors.Transparent, Colors.DodgerBlue, .85d);

                // Material
                MaterialGroup materials = new MaterialGroup();
                materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
                materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(Colors.White, color, .5d)), 30d));

                // Geometry Model
                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = materials;
                geometry.BackMaterial = materials;
                geometry.Geometry = UtilityWPF.GetMeshFromTriangles(hull.ToArray());

                ModelVisual3D modelVisual = new ModelVisual3D();
                modelVisual.Content = geometry;

                visual = modelVisual;

                _debugVisuals.Add(visual);
                _viewport.Children.Add(visual);


                //// Draw the plane
                //visual = GetTriangleVisual(GetPlane(_camera.Position, _camera.LookDirection, 60), UtilityWPF.ColorFromHex("20B0B0B0"));
                //_debugVisuals.Add(visual);
                //_viewport.Children.Add(visual);

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnShadowFixed2a_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearAllVisuals();

                Triangle triangle = new Triangle(new Point3D(0, 0, .1), new Point3D(1, -.25, .1), new Point3D(-.6, .6, .1));

                ITriangleIndexed[] hull = GetHullFromTriangle(
                    new Triangle(new Point3D(-.75, -.5, -1), new Point3D(.5, -.5, -1), new Point3D(-.1, .6, -1)),
                    new Vector3D(0, 0, 2));

                //hull = TriangleIndexed.ConvertToIndexed(Math3D.SliceLargeTriangles(hull, .5d));
                hull = TriangleIndexed.ConvertToIndexed(Math3D.SliceLargeTriangles(hull, .125d));

                //Point3D[] clipPolygon = HullTriangleIntersect.GetIntersection(hull, triangle);
                Point3D[] clipPolygon = HullTriangleIntersect2.GetIntersection_Hull_Triangle(hull, triangle);

                #region Draw

                Visual3D visual;

                // Clip polygons
                if (clipPolygon != null)
                {
                    visual = GetVisual_PolygonHull(clipPolygon, Colors.DodgerBlue, .005);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                // Hull lines
                visual = GetVisual_HullLines(hull, UtilityWPF.AlphaBlend(Colors.Transparent, Colors.DimGray, .5d), 1d);
                _debugVisuals.Add(visual);
                _viewport.Children.Add(visual);

                //// Normals
                //ScreenSpaceLines3D lines = new ScreenSpaceLines3D();
                //lines.Color = UtilityWPF.AlphaBlend(Colors.Transparent, Colors.White, .5d);
                //lines.Thickness = 1d;
                //foreach (ITriangle hullTriangle in hull)
                //{
                //    lines.AddLine(hullTriangle.GetCenterPoint(), hullTriangle.GetCenterPoint() + hullTriangle.Normal);
                //}
                //_debugVisuals.Add(lines);
                //_viewport.Children.Add(lines);

                // Triangle
                Color color = UtilityWPF.ColorFromHex("50E0E0E0");

                visual = GetVisual_Triangle(new ITriangle[] { triangle }, color);
                _debugVisuals.Add(visual);
                _viewport.Children.Add(visual);

                // Shadow hull

                color = UtilityWPF.AlphaBlend(Colors.Transparent, Colors.DodgerBlue, .85d);

                // Material
                MaterialGroup materials = new MaterialGroup();
                materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
                materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(Colors.White, color, .5d)), 30d));

                // Geometry Model
                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = materials;
                geometry.BackMaterial = materials;
                geometry.Geometry = UtilityWPF.GetMeshFromTriangles(hull.ToArray());

                ModelVisual3D modelVisual = new ModelVisual3D();
                modelVisual.Content = geometry;

                visual = modelVisual;

                _debugVisuals.Add(visual);
                _viewport.Children.Add(visual);

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnShadowFixed3_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearAllVisuals();

                Triangle triangle = new Triangle(new Point3D(0, -.4, 0), new Point3D(1, -.25, 0), new Point3D(-1, .4, 0));

                ITriangleIndexed[] hull = GetHullFromTriangle(
                    new Triangle(new Point3D(-.75, -.5, -1), new Point3D(.5, -.5, -1), new Point3D(-.1, .6, -1)),
                    new Vector3D(0, 0, 2));

                //Point3D[] clipPolygon = HullTriangleIntersect.GetIntersection(hull, triangle);
                Point3D[] clipPolygon = HullTriangleIntersect2.GetIntersection_Hull_Triangle(hull, triangle);

                #region Draw

                Visual3D visual;

                // Clip polygons
                if (clipPolygon != null)
                {
                    visual = GetVisual_PolygonHull(clipPolygon, Colors.DodgerBlue, .005);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                // Triangle
                Color color = UtilityWPF.ColorFromHex("50E0E0E0");

                visual = GetVisual_Triangle(new ITriangle[] { triangle }, color);
                _debugVisuals.Add(visual);
                _viewport.Children.Add(visual);

                // Shadow hull

                color = UtilityWPF.AlphaBlend(Colors.Transparent, Colors.DodgerBlue, .85d);

                // Material
                MaterialGroup materials = new MaterialGroup();
                materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
                materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(Colors.White, color, .5d)), 30d));

                // Geometry Model
                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = materials;
                geometry.BackMaterial = materials;
                geometry.Geometry = UtilityWPF.GetMeshFromTriangles(hull.ToArray());

                ModelVisual3D modelVisual = new ModelVisual3D();
                modelVisual.Content = geometry;

                visual = modelVisual;

                _debugVisuals.Add(visual);
                _viewport.Children.Add(visual);


                //// Draw the plane
                //visual = GetTriangleVisual(GetPlane(_camera.Position, _camera.LookDirection, 60), UtilityWPF.ColorFromHex("20B0B0B0"));
                //_debugVisuals.Add(visual);
                //_viewport.Children.Add(visual);

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnShadowFixed4_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearAllVisuals();

                Triangle triangle = new Triangle(new Point3D(0, 0, 0), new Point3D(1, 0, 0), new Point3D(0, 1, 0));

                List<Point3D> hullTop = new List<Point3D>();
                hullTop.Add(new Point3D(-.75, -.5, 1));     // 0
                hullTop.Add(new Point3D(.5, -.5, 1));       // 1
                hullTop.Add(new Point3D(.35, .3, 1));
                hullTop.Add(new Point3D(.25, .45, 1));
                hullTop.Add(new Point3D(-.1, .6, 1));       // 2

                ITriangleIndexed[] hull = GetHullFromPolygon(hullTop.ToArray(), new Vector3D(0, 0, -2));

                //Point3D[] clipPolygon = HullTriangleIntersect.GetIntersection(hull, triangle);
                Point3D[] clipPolygon = HullTriangleIntersect2.GetIntersection_Hull_Triangle(hull, triangle);

                #region Draw

                Visual3D visual;

                // Clip polygons
                if (clipPolygon != null)
                {
                    visual = GetVisual_PolygonHull(clipPolygon, Colors.DodgerBlue, .005);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                // Hull lines
                visual = GetVisual_HullLines(hull, UtilityWPF.AlphaBlend(Colors.Transparent, Colors.DimGray, .5d), 1d);
                _debugVisuals.Add(visual);
                _viewport.Children.Add(visual);

                // Triangle
                Color color = UtilityWPF.ColorFromHex("50E0E0E0");

                visual = GetVisual_Triangle(new ITriangle[] { triangle }, color);
                _debugVisuals.Add(visual);
                _viewport.Children.Add(visual);

                // Shadow hull

                color = UtilityWPF.AlphaBlend(Colors.Transparent, Colors.DodgerBlue, .85d);

                // Material
                MaterialGroup materials = new MaterialGroup();
                materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
                materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(Colors.White, color, .5d)), 30d));

                // Geometry Model
                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = materials;
                geometry.BackMaterial = materials;
                geometry.Geometry = UtilityWPF.GetMeshFromTriangles(hull.ToArray());

                ModelVisual3D modelVisual = new ModelVisual3D();
                modelVisual.Content = geometry;

                visual = modelVisual;

                _debugVisuals.Add(visual);
                _viewport.Children.Add(visual);


                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnShadowFixed5_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearAllVisuals();

                Triangle triangle = new Triangle(new Point3D(-5, -5, -1), new Point3D(5, -5, 0), new Point3D(0, 5, 0));

                ITriangleIndexed[] hull = GetHullFromTriangle(
                    new Triangle(new Point3D(-.75, -.5, -.3), new Point3D(.5, -.5, -.3), new Point3D(-.1, .6, -.3)),
                    new Vector3D(0, 0, .075));

                //Point3D[] clipPolygon = HullTriangleIntersect.GetIntersection(hull, triangle);
                Point3D[] clipPolygon = HullTriangleIntersect2.GetIntersection_Hull_Triangle(hull, triangle);

                #region Draw

                Visual3D visual;

                // Clip polygons
                if (clipPolygon != null)
                {
                    visual = GetVisual_PolygonHull(clipPolygon, Colors.DodgerBlue, .005);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                // Shadow hull

                Color color = UtilityWPF.AlphaBlend(Colors.Transparent, Colors.DodgerBlue, .85d);

                // Material
                MaterialGroup materials = new MaterialGroup();
                materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
                materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(Colors.White, color, .5d)), 30d));

                // Geometry Model
                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = materials;
                geometry.BackMaterial = materials;
                geometry.Geometry = UtilityWPF.GetMeshFromTriangles(hull.ToArray());

                ModelVisual3D modelVisual = new ModelVisual3D();
                modelVisual.Content = geometry;

                visual = modelVisual;

                _debugVisuals.Add(visual);
                _viewport.Children.Add(visual);


                // Triangle
                color = UtilityWPF.ColorFromHex("50E0E0E0");

                visual = GetVisual_Triangle(new ITriangle[] { triangle }, color);
                _debugVisuals.Add(visual);
                _viewport.Children.Add(visual);



                //// Draw the plane
                //visual = GetTriangleVisual(GetPlane(_camera.Position, _camera.LookDirection, 60), UtilityWPF.ColorFromHex("20B0B0B0"));
                //_debugVisuals.Add(visual);
                //_viewport.Children.Add(visual);

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnShadowFixed5a_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearAllVisuals();

                Triangle triangle = new Triangle(new Point3D(-5, -5, -1), new Point3D(5, -5, 0), new Point3D(0, 5, 0));

                ITriangleIndexed[] hull = GetHullFromTriangle(
                    new Triangle(new Point3D(-.75, -.5, -.32), new Point3D(.5, -.5, -.32), new Point3D(-.1, .6, -.32)),
                    new Vector3D(0, 0, .07));

                //Point3D[] clipPolygon = HullTriangleIntersect.GetIntersection(hull, triangle);
                Point3D[] clipPolygon = HullTriangleIntersect2.GetIntersection_Hull_Triangle(hull, triangle);

                #region Draw

                Visual3D visual;

                // Clip polygons
                if (clipPolygon != null)
                {
                    visual = GetVisual_PolygonHull(clipPolygon, Colors.DodgerBlue, .005);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                // Shadow hull

                Color color = UtilityWPF.AlphaBlend(Colors.Transparent, Colors.DodgerBlue, .85d);

                // Material
                MaterialGroup materials = new MaterialGroup();
                materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
                materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(Colors.White, color, .5d)), 30d));

                // Geometry Model
                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = materials;
                geometry.BackMaterial = materials;
                geometry.Geometry = UtilityWPF.GetMeshFromTriangles(hull.ToArray());

                ModelVisual3D modelVisual = new ModelVisual3D();
                modelVisual.Content = geometry;

                visual = modelVisual;

                _debugVisuals.Add(visual);
                _viewport.Children.Add(visual);


                // Triangle
                color = UtilityWPF.ColorFromHex("50E0E0E0");

                visual = GetVisual_Triangle(new ITriangle[] { triangle }, color);
                _debugVisuals.Add(visual);
                _viewport.Children.Add(visual);


                //// Draw the plane
                //visual = GetTriangleVisual(GetPlane(_camera.Position, _camera.LookDirection, 60), UtilityWPF.ColorFromHex("20B0B0B0"));
                //_debugVisuals.Add(visual);
                //_viewport.Children.Add(visual);

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnShadowFixed6_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearAllVisuals();

                Triangle triangle = new Triangle(new Point3D(-2.39, -3.17, 12.44), new Point3D(-0.44, 1.28, 12.15), new Point3D(-4.24, 3.63, 9.62));

                Point3D[] allPoints = new Point3D[6];
                allPoints[0] = new Point3D(3.35, -1.07, 14.69);
                allPoints[1] = new Point3D(-0.44, 1.28, 12.15);
                allPoints[2] = new Point3D(-2.39, -3.17, 12.44);
                allPoints[3] = new Point3D(3.35, -1.07, -35.31);
                allPoints[4] = new Point3D(-0.44, 1.28, -37.85);
                allPoints[5] = new Point3D(-2.39, -3.17, -37.56);

                TriangleIndexed[] hull = new TriangleIndexed[1];
                hull[0] = new TriangleIndexed(1, 2, 0, allPoints);
                //hull[1] = new TriangleIndexed(3, 4, 1, allPoints);
                //hull[2] = new TriangleIndexed(3, 1, 0, allPoints);
                //hull[3] = new TriangleIndexed(5, 0, 2, allPoints);
                //hull[4] = new TriangleIndexed(5, 2, 1, allPoints);
                //hull[5] = new TriangleIndexed(5, 3, 0, allPoints);
                //hull[6] = new TriangleIndexed(5, 1, 4, allPoints);
                //hull[7] = new TriangleIndexed(5, 4, 3, allPoints);

                Point3D[] clipPolygon = HullTriangleIntersect2.GetIntersection_Hull_Triangle(hull, triangle);
                //Point3D[] clipPolygon = null;

                #region Draw

                Visual3D visual;

                // Dots
                double dotRadius = .05d;

                visual = GetVisual_Dot(new Point3D(3.34828755135393, -1.07062650560222, 14.6744119439448), dotRadius, Colors.Black);
                _debugVisuals.Add(visual);
                _viewport.Children.Add(visual);

                visual = GetVisual_Dot(new Point3D(3.35, -1.07, 14.6750787484006), dotRadius, Colors.White);
                _debugVisuals.Add(visual);
                _viewport.Children.Add(visual);

                visual = GetVisual_Dot(new Point3D(-2.39, -3.17, 12.44), dotRadius, Colors.Green);
                _debugVisuals.Add(visual);
                _viewport.Children.Add(visual);

                visual = GetVisual_Dot(new Point3D(-0.548204828380986, 1.03307103266903, 12.1660920001182), dotRadius, Colors.Yellow);
                _debugVisuals.Add(visual);
                _viewport.Children.Add(visual);


                // Clip polygons
                if (clipPolygon != null)
                {
                    visual = GetVisual_PolygonHull(clipPolygon, Colors.DodgerBlue, .005);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }


                // Shadow hull

                Color color = UtilityWPF.AlphaBlend(Colors.Transparent, Colors.DodgerBlue, .85d);

                // Material
                MaterialGroup materials = new MaterialGroup();
                materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
                materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(Colors.White, color, .5d)), 30d));

                // Geometry Model
                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = materials;
                geometry.BackMaterial = materials;
                geometry.Geometry = UtilityWPF.GetMeshFromTriangles(hull.ToArray());

                ModelVisual3D modelVisual = new ModelVisual3D();
                modelVisual.Content = geometry;

                visual = modelVisual;

                _debugVisuals.Add(visual);
                _viewport.Children.Add(visual);


                // Triangle
                color = UtilityWPF.ColorFromHex("50E0E0E0");

                visual = GetVisual_Triangle(new ITriangle[] { triangle }, color);
                _debugVisuals.Add(visual);
                _viewport.Children.Add(visual);


                //// Draw the plane
                //visual = GetTriangleVisual(GetPlane(_camera.Position, _camera.LookDirection, 60), UtilityWPF.ColorFromHex("20B0B0B0"));
                //_debugVisuals.Add(visual);
                //_viewport.Children.Add(visual);

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnClipPolyFixed1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearAllVisuals();

                //List<Point> subjectPoly = new List<Point>();
                //subjectPoly.Add(new Point(0, 0));
                //subjectPoly.Add(new Point(1, 0));
                //subjectPoly.Add(new Point(2, 0));
                //subjectPoly.Add(new Point(2, 2));

                //List<Point> clipPoly = new List<Point>();
                //clipPoly.Add(new Point(-.5, 3.5));
                //clipPoly.Add(new Point(-.5, -1));
                //clipPoly.Add(new Point(3, 0));

                List<Point> subjectPoly = new List<Point>();
                subjectPoly.Add(new Point(0, 0));
                subjectPoly.Add(new Point(0.227272727272727, 0));
                subjectPoly.Add(new Point(0.2, 0.0499999999999999));
                subjectPoly.Add(new Point(-0.1, 0.6));
                subjectPoly.Add(new Point(0, 0.416666666666667));

                List<Point> clipPoly = new List<Point>();
                clipPoly.Add(new Point(0, 0));
                clipPoly.Add(new Point(1, 0));
                clipPoly.Add(new Point(0, 1));

                //Point[] intersection = null;
                Point[] intersection = Math2D.GetIntersection_Polygon_Polygon(subjectPoly.ToArray(), clipPoly.ToArray());

                #region Draw

                Visual3D visual;

                double dotSize = .05d;

                // Subject points
                foreach (Point point in subjectPoly)
                {
                    visual = GetVisual_Dot(point.ToPoint3D(), dotSize, Colors.DarkCyan);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                // Clip points
                foreach (Point point in clipPoly)
                {
                    visual = GetVisual_Dot(point.ToPoint3D(), dotSize, Colors.DarkGreen);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                // Intersect points
                if (intersection != null)
                {
                    foreach (Point point in intersection)
                    {
                        visual = GetVisual_Dot(point.ToPoint3D(), dotSize * 1.5, Colors.HotPink);
                        _debugVisuals.Add(visual);
                        _viewport.Children.Add(visual);
                    }
                }

                // Subject lines
                visual = GetVisual_PolygonLines(subjectPoly.Select(o => o.ToPoint3D()).ToArray(), Colors.DarkCyan, 1d);
                _debugVisuals.Add(visual);
                _viewport.Children.Add(visual);

                // Clip lines
                visual = GetVisual_PolygonLines(clipPoly.Select(o => o.ToPoint3D()).ToArray(), Colors.DarkGreen, 1d);
                _debugVisuals.Add(visual);
                _viewport.Children.Add(visual);

                // Intersect poly
                if (intersection != null)
                {
                    visual = GetVisual_Polygon_Convex(intersection.Select(o => o.ToPoint3D()).ToArray(), UtilityWPF.AlphaBlend(Colors.HotPink, Colors.Transparent, .5d));
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnClipPolyFixed2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearAllVisuals();

                List<Point> subjectPoly = new List<Point>();
                subjectPoly.Add(new Point(-1.84772209054484, -21.473079422276));
                subjectPoly.Add(new Point(-2.09948981452815, -21.7529431764434));
                subjectPoly.Add(new Point(-4.92678945436559, -24.8957554434106));
                subjectPoly.Add(new Point(-1.25356748962809, -22.0540658765885));

                List<Point> clipPoly = new List<Point>();
                clipPoly.Add(new Point(-3.0360312923783, -20.3111065136509));
                clipPoly.Add(new Point(-4.92678945436552, -24.8957554434106));
                clipPoly.Add(new Point(-1.84772209054478, -21.4730794222761));

                //Point[] intersection = null;
                Point[] intersection = Math2D.GetIntersection_Polygon_Polygon(subjectPoly.ToArray(), clipPoly.ToArray());

                #region Draw

                Visual3D visual;

                double dotSize = .05d;

                // Subject points
                foreach (Point point in subjectPoly)
                {
                    visual = GetVisual_Dot(point.ToPoint3D(), dotSize, Colors.DarkCyan);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                // Clip points
                foreach (Point point in clipPoly)
                {
                    visual = GetVisual_Dot(point.ToPoint3D(), dotSize, Colors.DarkGreen);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                // Intersect points
                if (intersection != null)
                {
                    foreach (Point point in intersection)
                    {
                        visual = GetVisual_Dot(point.ToPoint3D(), dotSize * 1.5, Colors.HotPink);
                        _debugVisuals.Add(visual);
                        _viewport.Children.Add(visual);
                    }
                }

                // Subject lines
                visual = GetVisual_PolygonLines(subjectPoly.Select(o => o.ToPoint3D()).ToArray(), Colors.DarkCyan, 1d);
                _debugVisuals.Add(visual);
                _viewport.Children.Add(visual);

                // Clip lines
                visual = GetVisual_PolygonLines(clipPoly.Select(o => o.ToPoint3D()).ToArray(), Colors.DarkGreen, 1d);
                _debugVisuals.Add(visual);
                _viewport.Children.Add(visual);

                // Intersect poly
                if (intersection != null)
                {
                    visual = GetVisual_Polygon_Convex(intersection.Select(o => o.ToPoint3D()).ToArray(), UtilityWPF.AlphaBlend(Colors.HotPink, Colors.Transparent, .5d));
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnClipPolyFixed3_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearAllVisuals();

                List<Point> subjectPoly = new List<Point>();
                subjectPoly.Add(new Point(-8.82900018793393, -19.6521705795618));
                subjectPoly.Add(new Point(-6.41601459766629, -17.8752265005177));
                subjectPoly.Add(new Point(-8.49142505485237, -23.1023703425934));

                List<Point> clipPoly = new List<Point>();
                clipPoly.Add(new Point(-6.41601459766631, -17.8752265005179));
                clipPoly.Add(new Point(-9.52612617151692, -12.5271673798134));
                clipPoly.Add(new Point(-9.0087756131846, -17.8147688612034));

                //Point[] intersection = null;
                Point[] intersection = Math2D.GetIntersection_Polygon_Polygon(subjectPoly.ToArray(), clipPoly.ToArray());

                #region Draw

                Visual3D visual;

                double dotSize = .05d;

                // Subject points
                foreach (Point point in subjectPoly)
                {
                    visual = GetVisual_Dot(point.ToPoint3D(), dotSize, Colors.DarkCyan);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                // Clip points
                foreach (Point point in clipPoly)
                {
                    visual = GetVisual_Dot(point.ToPoint3D(), dotSize, Colors.DarkGreen);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                // Intersect points
                if (intersection != null)
                {
                    foreach (Point point in intersection)
                    {
                        visual = GetVisual_Dot(point.ToPoint3D(), dotSize * 1.5, Colors.HotPink);
                        _debugVisuals.Add(visual);
                        _viewport.Children.Add(visual);
                    }
                }

                // Subject lines
                visual = GetVisual_PolygonLines(subjectPoly.Select(o => o.ToPoint3D()).ToArray(), Colors.DarkCyan, 1d);
                _debugVisuals.Add(visual);
                _viewport.Children.Add(visual);

                // Clip lines
                visual = GetVisual_PolygonLines(clipPoly.Select(o => o.ToPoint3D()).ToArray(), Colors.DarkGreen, 1d);
                _debugVisuals.Add(visual);
                _viewport.Children.Add(visual);

                // Intersect poly
                if (intersection != null)
                {
                    visual = GetVisual_Polygon_Convex(intersection.Select(o => o.ToPoint3D()).ToArray(), UtilityWPF.AlphaBlend(Colors.HotPink, Colors.Transparent, .5d));
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnPolyUnion_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearAllVisuals();

                // Create some random triangles
                Point[][] initialPolys = new Point[5][];

                for (int cntr = 0; cntr < initialPolys.Length; cntr++)
                {
                    Point[] triangle = Enumerable.Range(0, 3).
                        Select(o => Math3D.GetRandomVectorSpherical2D(20d)).
                        Select(o => new Point(o.X, o.Y)).
                        ToArray();

                    initialPolys[cntr] = triangle;
                }

                // Separate into islands
                Polygon2D[] finalPolys = null;
                try
                {
                    finalPolys = GetPolyUnion(initialPolys);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                    finalPolys = new Polygon2D[0];
                }

                #region Draw

                Visual3D visual;

                // Lines
                foreach (Point[] polygon in initialPolys)
                {
                    visual = GetVisual_PolygonLines(polygon.Select(o => o.ToPoint3D(-2)).ToArray(), Colors.Gray, 1);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                foreach (Polygon2D polygon in finalPolys)
                {
                    visual = GetVisual_PolygonLines(polygon.Polygon.Select(o => o.ToPoint3D(2)).ToArray(), Colors.Black, 2);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);

                    foreach (Point[] hole in polygon.Holes)
                    {
                        visual = GetVisual_PolygonLines(hole.Select(o => o.ToPoint3D(2)).ToArray(), Colors.Red, 2);
                        _debugVisuals.Add(visual);
                        _viewport.Children.Add(visual);
                    }
                }

                // Polygons
                //foreach (Point[] polygon in finalPolys)       // can't do these, they are concave
                foreach (Point[] polygon in initialPolys)
                {
                    Color color = Color.FromArgb(10, Convert.ToByte(StaticRandom.Next(256)), Convert.ToByte(StaticRandom.Next(256)), Convert.ToByte(StaticRandom.Next(256)));

                    visual = GetVisual_Polygon_Convex(polygon.Select(o => o.ToPoint3D()).ToArray(), color);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnPolyUnionTest1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearAllVisuals();

                // Create some random triangles
                //ITriangle[] polys = new ITriangle[5];
                Point[][] polys = new Point[5][];

                for (int cntr = 0; cntr < polys.Length; cntr++)
                {
                    Point[] triangle = Enumerable.Range(0, 3).
                        Select(o => Math3D.GetRandomVectorSpherical2D(20d)).
                        Select(o => new Point(o.X, o.Y)).
                        ToArray();

                    //polys[cntr] = new Triangle(triangle[0].ToPoint3D(), triangle[1].ToPoint3D(), triangle[2].ToPoint3D());
                    polys[cntr] = triangle;
                }

                // Separate into islands
                var islands = PolyUnion.Test1(polys);

                // Draw them
                Visual3D visual;

                for (int islandCntr = 0; islandCntr < islands.Length; islandCntr++)
                {
                    // Choose a color for this island
                    //int colorBase = StaticRandom.Next(128, 192);
                    Color colorBase = Color.FromRgb(Convert.ToByte(StaticRandom.Next(10, 246)), Convert.ToByte(StaticRandom.Next(10, 246)), Convert.ToByte(StaticRandom.Next(10, 246)));

                    // Draw each polygon in this island
                    for (int cntr = 0; cntr < islands[islandCntr].Length; cntr++)
                    {
                        int offset = -10 + StaticRandom.Next(21);
                        Color color = Color.FromRgb(Convert.ToByte(colorBase.R + offset), Convert.ToByte(colorBase.G + offset), Convert.ToByte(colorBase.B + offset));

                        //Color color = UtilityWPF.GetRandomColor(255, Convert.ToByte(colorBase.R - 5), Convert.ToByte(colorBase.R + 5), Convert.ToByte(colorBase.G - 5), Convert.ToByte(colorBase.G + 5), Convert.ToByte(colorBase.B - 5), Convert.ToByte(colorBase.B + 5));

                        visual = GetVisual_Polygon_Convex(islands[islandCntr][cntr].Select(o => o.ToPoint3D()).ToArray(), color);
                        _debugVisuals.Add(visual);
                        _viewport.Children.Add(visual);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnPolyUnionTest2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearAllVisuals();

                // Create some random triangles
                Point[][] initialPolys = new Point[2][];

                initialPolys[0] = new Point[] {
                    new Point(1.18835849523989,12.0923913811283),
                    new Point(17.1450026099927,-0.446056243262942),
                    new Point(12.8603228459465,-14.4021227601447) };

                initialPolys[1] = new Point[] {
                    new Point(-6.93311959776908,-17.9361101452111),
                    new Point(13.8351851965873,1.20534910571344),
                    new Point(1.04225300790695,-15.6208477667981) };

                // Separate into islands
                Polygon2D[] finalPolys = GetPolyUnion(initialPolys);

                #region Draw

                Visual3D visual;

                // Lines
                foreach (Point[] polygon in initialPolys)
                {
                    visual = GetVisual_PolygonLines(polygon.Select(o => o.ToPoint3D(-2)).ToArray(), Colors.Gray, 1);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                foreach (Polygon2D polygon in finalPolys)
                {
                    visual = GetVisual_PolygonLines(polygon.Polygon.Select(o => o.ToPoint3D(2)).ToArray(), Colors.Black, 2);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);

                    foreach (Point[] hole in polygon.Holes)
                    {
                        visual = GetVisual_PolygonLines(hole.Select(o => o.ToPoint3D(2)).ToArray(), Colors.Red, 2);
                        _debugVisuals.Add(visual);
                        _viewport.Children.Add(visual);
                    }
                }

                // Polygons
                //foreach (Point[] polygon in finalPolys)       // can't do these, they are concave
                foreach (Point[] polygon in initialPolys)
                {
                    Color color = Color.FromArgb(128, Convert.ToByte(StaticRandom.Next(256)), Convert.ToByte(StaticRandom.Next(256)), Convert.ToByte(StaticRandom.Next(256)));

                    visual = GetVisual_Polygon_Convex(polygon.Select(o => o.ToPoint3D()).ToArray(), color);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnPolyUnion2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearAllVisuals();

                // Create some random triangles
                Point[][] initialPolys = new Point[5][];

                for (int cntr = 0; cntr < initialPolys.Length; cntr++)
                {
                    Point[] triangle = Enumerable.Range(0, 3).
                        Select(o => Math3D.GetRandomVectorSpherical2D(20d)).
                        Select(o => new Point(o.X, o.Y)).
                        ToArray();

                    initialPolys[cntr] = triangle;
                }

                // Separate into islands
                Polygon2D[] finalPolys = null;
                try
                {
                    finalPolys = GetPolyUnion(initialPolys);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                    finalPolys = new Polygon2D[0];
                }

                #region Draw

                Visual3D visual;

                // Lines
                foreach (Point[] polygon in initialPolys)
                {
                    visual = GetVisual_PolygonLines(polygon.Select(o => o.ToPoint3D(-2)).ToArray(), Colors.Gray, 1);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                foreach (Polygon2D polygon in finalPolys)
                {
                    visual = GetVisual_PolygonLines(polygon.Polygon.Select(o => o.ToPoint3D(2)).ToArray(), Colors.Black, 2);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);

                    foreach (Point[] hole in polygon.Holes)
                    {
                        visual = GetVisual_PolygonLines(hole.Select(o => o.ToPoint3D(2)).ToArray(), Colors.Red, 2);
                        _debugVisuals.Add(visual);
                        _viewport.Children.Add(visual);
                    }
                }

                // Polygons
                //foreach (Point[] polygon in finalPolys)       // can't do these, they are concave
                foreach (Point[] polygon in initialPolys)
                {
                    Color color = Color.FromArgb(10, Convert.ToByte(StaticRandom.Next(256)), Convert.ToByte(StaticRandom.Next(256)), Convert.ToByte(StaticRandom.Next(256)));

                    visual = GetVisual_Polygon_Convex(polygon.Select(o => o.ToPoint3D()).ToArray(), color);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnPolyUnionTest3a_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearAllVisuals();

                // Create some random triangles
                Point[][] initialPolys = new Point[3][];

                initialPolys[0] = new Point[] {
                    new Point(-8.00822498773053,15.724908636816),
                    new Point(-10.9888146681928,9.99396633672174),
                    new Point(-1.35614945481762,1.2745135259889) };

                initialPolys[1] = new Point[] {
                    new Point(17.6394906227247,-1.78969639822036),
                    new Point(-0.944559908696264,8.15715451007008),
                    new Point(-16.1622843293672,0.442278789958519) };

                initialPolys[2] = new Point[] {
                    new Point(-13.3366344395237,10.1599123251435),
                    new Point(4.03979352867374,6.13141576520923),
                    new Point(16.079651596552,9.28399147918985) };

                // Separate into islands
                Polygon2D[] finalPolys = null;
                try
                {
                    finalPolys = GetPolyUnion(initialPolys);
                }
                catch (Exception ex)
                {
                    //MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                    finalPolys = new Polygon2D[0];
                }

                #region Draw

                Visual3D visual;

                // Lines
                foreach (Point[] polygon in initialPolys)
                {
                    visual = GetVisual_PolygonLines(polygon.Select(o => o.ToPoint3D(-2)).ToArray(), Colors.Gray, 1);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                foreach (Polygon2D polygon in finalPolys)
                {
                    visual = GetVisual_PolygonLines(polygon.Polygon.Select(o => o.ToPoint3D(2)).ToArray(), Colors.Black, 2);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);

                    foreach (Point[] hole in polygon.Holes)
                    {
                        visual = GetVisual_PolygonLines(hole.Select(o => o.ToPoint3D(2)).ToArray(), Colors.Red, 2);
                        _debugVisuals.Add(visual);
                        _viewport.Children.Add(visual);
                    }
                }

                // Polygons
                //foreach (Point[] polygon in finalPolys)       // can't do these, they are concave
                //foreach (Point[] polygon in initialPolys)
                for (int cntr = 0; cntr < initialPolys.Length; cntr++)
                {
                    Color color = UtilityWPF.GetColorEGA(10, cntr);
                    //Color color = Color.FromArgb(10, Convert.ToByte(StaticRandom.Next(256)), Convert.ToByte(StaticRandom.Next(256)), Convert.ToByte(StaticRandom.Next(256)));

                    visual = GetVisual_Polygon_Convex(initialPolys[cntr].Select(o => o.ToPoint3D()).ToArray(), color);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnPolyUnionTest3b_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearAllVisuals();

                // Create some random triangles
                Point[][] initialPolys = new Point[3][];

                initialPolys[0] = new Point[] {
                    new Point(-8.00822498773053,15.724908636816),
                    new Point(-10.9888146681928,9.99396633672174),
                    new Point(-1.35614945481762,1.2745135259889) };

                initialPolys[1] = new Point[] {
                    new Point(17.6394906227247,-1.78969639822036),
                    new Point(-0.944559908696264,8.15715451007008),
                    new Point(-16.1622843293672,0.442278789958519) }.Reverse().ToArray();

                initialPolys[2] = new Point[] {
                    new Point(-13.3366344395237,10.1599123251435),
                    new Point(4.03979352867374,6.13141576520923),
                    new Point(16.079651596552,9.28399147918985) };

                // Separate into islands
                Polygon2D[] finalPolys = null;
                try
                {
                    finalPolys = GetPolyUnion(initialPolys);
                }
                catch (Exception ex)
                {
                    //MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                    finalPolys = new Polygon2D[0];
                }

                #region Draw

                Visual3D visual;

                // Lines
                foreach (Point[] polygon in initialPolys)
                {
                    visual = GetVisual_PolygonLines(polygon.Select(o => o.ToPoint3D(-2)).ToArray(), Colors.Gray, 1);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                foreach (Polygon2D polygon in finalPolys)
                {
                    visual = GetVisual_PolygonLines(polygon.Polygon.Select(o => o.ToPoint3D(2)).ToArray(), Colors.Black, 2);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);

                    foreach (Point[] hole in polygon.Holes)
                    {
                        visual = GetVisual_PolygonLines(hole.Select(o => o.ToPoint3D(2)).ToArray(), Colors.Red, 2);
                        _debugVisuals.Add(visual);
                        _viewport.Children.Add(visual);
                    }
                }

                // Polygons
                //foreach (Point[] polygon in finalPolys)       // can't do these, they are concave
                //foreach (Point[] polygon in initialPolys)
                for (int cntr = 0; cntr < initialPolys.Length; cntr++)
                {
                    Color color = UtilityWPF.GetColorEGA(10, cntr);
                    //Color color = Color.FromArgb(10, Convert.ToByte(StaticRandom.Next(256)), Convert.ToByte(StaticRandom.Next(256)), Convert.ToByte(StaticRandom.Next(256)));

                    visual = GetVisual_Polygon_Convex(initialPolys[cntr].Select(o => o.ToPoint3D()).ToArray(), color);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnPolyUnionTest4_Click(object sender, RoutedEventArgs e)
        {

            // The main problem is that lines are slicing through the entire polygon.  Need to detect that, also get rid of colinear points

            try
            {
                ClearAllVisuals();

                Point3D[][] initial3D = new Point3D[3][];

                initial3D[0] = new Point3D[] {
                    new Point3D(-16.1947246550931,-0.9149519332283,2.55976103387392),
                    new Point3D(-17.1107881600685,0.515445435128346,3.40133335668073),
                    new Point3D(-17.3596515210729,-0.829632679480615,4.42103762371103),
                    new Point3D(-17.3450427909807,-2.19357445878095,5.01957844043668),
                    new Point3D(-17.3283385073944,-2.17500700982897,4.98385833288751) };

                initial3D[1] = new Point3D[] {
                    new Point3D(-16.1947246550931,-0.914951933228302,2.55976103387392),
                    new Point3D(-17.3283385073944,-2.17500700982897,4.9838583328875),
                    new Point3D(-17.3450427909807,-2.19357445878095,5.01957844043668),
                    new Point3D(-17.341448388578,-2.52916527179157,5.16684630945675),
                    new Point3D(-15.3066235822732,-2.3016870563904,1.74387733292857) };

                initial3D[2] = new Point3D[] {
                    new Point3D(-17.3204018787836,-4.49416933689273,6.02915227870288),
                    new Point3D(-16.4203453178512,-4.5492595275131,4.58613381891047),
                    new Point3D(-14.928909934653,-2.89147215820253,1.39687808008519),
                    new Point3D(-15.3066235822732,-2.3016870563904,1.74387733292857),
                    new Point3D(-17.341448388578,-2.52916527179157,5.16684630945675) };

                // Create some random triangles
                Point[][] initialPolys = new Point[3][];

                initialPolys[0] = new Point[] {
                    new Point(-1.70753174254516,-10.6978382660948),
                    new Point(-0.726945738693094,-12.3201526746214),
                    new Point(-2.36588629353582,-12.7943243278674),
                    new Point(-3.85545055921974,-12.7943243278674),
                    new Point(-3.82425967044327,-12.7638803171038) };

                initialPolys[1] = new Point[] {
                    new Point(-1.70753174254517,-10.6978382660948),
                    new Point(-3.82425967044327,-12.7638803171038),
                    new Point(-3.85545055921974,-12.7943243278674),
                    new Point(-4.22195013459667,-12.7943243278674),
                    new Point(-2.65818579358351,-9.1250442852861) };

                initialPolys[2] = new Point[] {
                    new Point(-6.36793604229414,-12.7943243278674),
                    new Point(-5.84736974693176,-11.1743089731826),
                    new Point(-3.06250352298613,-8.45612745855601),
                    new Point(-2.65818579358351,-9.1250442852861),
                    new Point(-4.22195013459667,-12.7943243278674) };



                var feedsIntermediate = new Tuple<double, double, double, double>[]
                {
                    Tuple.Create(-1.70753174254516, -10.6978382660948, -0.726945738693094, -12.3201526746214),      //0
                    Tuple.Create(-0.726945738693094, -12.3201526746214, -2.36588629353582, -12.7943243278674),      //1
                    Tuple.Create(-2.36588629353582, -12.7943243278674, -3.85545055921974, -12.7943243278674),       //2
                    Tuple.Create(-3.85545055921974, -12.7943243278674, -3.82425967044327, -12.7638803171038),       //3
                    Tuple.Create(-3.82425967044327, -12.7638803171038, -1.70753174254516, -10.6978382660948),       //4
                    Tuple.Create(-1.70753174254517, -10.6978382660948, -3.82425967044327, -12.7638803171038),       //5
                    Tuple.Create(-3.82425967044327, -12.7638803171038, -3.85545055921974, -12.7943243278674),       //6
                    Tuple.Create(-3.85545055921974, -12.7943243278674, -4.22195013459667, -12.7943243278674),       //7
                    Tuple.Create(-4.22195013459667, -12.7943243278674, -2.65818579358351, -9.1250442852861),       //8
                    Tuple.Create(-2.65818579358351, -9.1250442852861, -1.70753174254517, -10.6978382660948),       //9
                    Tuple.Create(-6.36793604229414, -12.7943243278674, -5.84736974693176, -11.1743089731826),       //10
                    Tuple.Create(-5.84736974693176, -11.1743089731826, -3.06250352298613, -8.45612745855601),       //11
                    Tuple.Create(-3.06250352298613, -8.45612745855601, -2.65818579358351, -9.1250442852861),       //12
                    Tuple.Create(-2.65818579358351, -9.1250442852861, -4.22195013459667, -12.7943243278674),       //13
                    Tuple.Create(-4.22195013459667, -12.7943243278674, -6.36793604229414, -12.7943243278674)       //14
                }.Select(o => Tuple.Create(new Point(o.Item1, o.Item2), new Point(o.Item3, o.Item4))).ToArray();

                #region feeds intermediate dupes

                // Simplify duplication chains:
                //      Any point should only have one other match in the list
                //      If there is more than one match, the list should be able to be simplified

                List<Point> uniquePoints = new List<Point>();
                foreach (Point point in UtilityHelper.Iterate(feedsIntermediate.Select(o => o.Item1), feedsIntermediate.Select(o => o.Item2)))
                {
                    if (!uniquePoints.Any(o => Math2D.IsNearValue(point, o)))
                    {
                        uniquePoints.Add(point);
                    }
                }

                var matchAttempt = uniquePoints
                    .Select(o => new { Point = o, Matches = feedsIntermediate.Where(p => Math2D.IsNearValue(o, p.Item1) || Math2D.IsNearValue(o, p.Item2)).ToArray() }).
                    OrderByDescending(o => o.Matches.Length).
                    ToArray();


                Tuple<Point, Point>[] feeds2 = feedsIntermediate.Select(o => o.Item1.X < o.Item2.X ? o : Tuple.Create(o.Item2, o.Item1)).ToArray();

                List<Tuple<Point, Point>> feeds3 = new List<Tuple<Point, Point>>();
                foreach (var feed in feeds2)
                {
                    if (!feeds3.Any(o => Math2D.IsNearValue(feed.Item1, o.Item1) && Math2D.IsNearValue(feed.Item2, o.Item2)))
                    {
                        feeds3.Add(feed);
                    }
                }


                var matchAttempt2 = uniquePoints
                    .Select(o => new { Point = o, Matches = feeds3.Where(p => Math2D.IsNearValue(o, p.Item1) || Math2D.IsNearValue(o, p.Item2)).ToArray() }).
                    OrderByDescending(o => o.Matches.Length).
                    ToArray();


                #endregion

                // This is what StitchSegments generates (should have just come up with one poly)
                Point[][] intermediate = new Point[1][];

                intermediate[0] = new Point[] {
                    new Point(-1.70753174254516,-10.6978382660948),     //0
                    new Point(-0.726945738693094,-12.3201526746214),        //1
                    new Point(-2.36588629353582,-12.7943243278674),     //2
                    new Point(-3.85545055921974,-12.7943243278674),     //3
                    new Point(-3.82425967044327,-12.7638803171038),     //4
                    new Point(-1.70753174254517,-10.6978382660948),     //5
                    new Point(-3.82425967044327,-12.7638803171038),     //6
                    new Point(-3.85545055921974,-12.7943243278674),     //7
                    new Point(-4.22195013459667,-12.7943243278674),     //8
                    new Point(-2.65818579358351,-9.1250442852861) };        //9

                //intermediate[1] = new Point[] {
                //    new Point(-6.36793604229414,-12.7943243278674),
                //    new Point(-5.84736974693176,-11.1743089731826),
                //    new Point(-3.06250352298613,-8.45612745855601),
                //    new Point(-2.65818579358351,-9.1250442852861),
                //    new Point(-4.22195013459667,-12.7943243278674) };

                #region intermediate distances

                List<Tuple<int, int, double, bool>> distances = new List<Tuple<int, int, double, bool>>();
                for (int outer = 0; outer < intermediate[0].Length - 1; outer++)
                {
                    for (int inner = outer + 1; inner < intermediate[0].Length; inner++)
                    {
                        double distance = (intermediate[0][outer] - intermediate[0][inner]).Length;

                        distances.Add(Tuple.Create(outer, inner, distance, Math3D.IsNearZero(distance)));
                    }
                }

                distances = distances.OrderBy(o => o.Item3).ToList();

                #endregion

                // Separate into islands
                Polygon2D[] finalPolys = null;
                try
                {
                    finalPolys = GetPolyUnion(initialPolys);
                }
                catch (Exception ex)
                {
                    //MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                    finalPolys = new Polygon2D[0];
                }

                #region Draw

                Visual3D visual;

                // Lines
                foreach (Point[] polygon in initialPolys)
                {
                    visual = GetVisual_PolygonLines(polygon.Select(o => o.ToPoint3D(-2)).ToArray(), Colors.Gray, 1);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }


                foreach (Point[] polygon in intermediate)
                {
                    visual = GetVisual_PolygonLines(polygon.Select(o => o.ToPoint3D(0)).ToArray(), Colors.Gray, 2);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }


                for (int cntr = 0; cntr < intermediate[0].Length; cntr++)
                {
                    visual = GetVisual_Dot(intermediate[0][cntr].ToPoint3D(2), .02 + (cntr * .01), UtilityWPF.GetColorEGA(64, cntr));
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }


                //foreach (Point3D[] poly in initial3D)
                //{
                //    visual = GetPolygonLinesVisual(poly, Colors.Black, 1);
                //    _debugVisuals.Add(visual);
                //    _viewport.Children.Add(visual);
                //}


                foreach (Polygon2D polygon in finalPolys)
                {
                    visual = GetVisual_PolygonLines(polygon.Polygon.Select(o => o.ToPoint3D(2)).ToArray(), Colors.Black, 2);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);

                    foreach (Point[] hole in polygon.Holes)
                    {
                        visual = GetVisual_PolygonLines(hole.Select(o => o.ToPoint3D(2)).ToArray(), Colors.Red, 2);
                        _debugVisuals.Add(visual);
                        _viewport.Children.Add(visual);
                    }
                }

                // Polygons
                //foreach (Point[] polygon in finalPolys)       // can't do these, they are concave
                //foreach (Point[] polygon in initialPolys)
                for (int cntr = 0; cntr < initialPolys.Length; cntr++)
                {
                    Color color = UtilityWPF.GetColorEGA(10, cntr);
                    //Color color = Color.FromArgb(10, Convert.ToByte(StaticRandom.Next(256)), Convert.ToByte(StaticRandom.Next(256)), Convert.ToByte(StaticRandom.Next(256)));

                    visual = GetVisual_Polygon_Convex(initialPolys[cntr].Select(o => o.ToPoint3D()).ToArray(), color);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnPolyUnionTest5_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearAllVisuals();

                // Create some random triangles
                Point[][] initialPolys = new Point[4][];

                initialPolys[0] = new Point[] {
                    new Point(7.63258002821866,-1.71657720938275),
                    new Point(7.76145635305171,-1.67761318154907),
                    new Point(9.43425936393152,-1.17186363320799),
                    new Point(10.5549080800541,-1.71657720938275) };

                initialPolys[1] = new Point[] {
                    new Point(10.3058611604882,-1.59552300547356),
                    new Point(10.3269392559373,-1.71657720938275),
                    new Point(9.54274292483309,-1.71657720938275),
                    new Point(8.87261465785801,-0.898865063733471) };

                initialPolys[2] = new Point[] {
                    new Point(9.43425936393151,-1.17186363320798),
                    new Point(7.7614563530517,-1.67761318154907),
                    new Point(7.63258002821865,-1.71657720938275),
                    new Point(7.00040640990406,-1.71657720938275),
                    new Point(6.87244607297693,0.029306274277618),
                    new Point(6.96135493605682,0.0301407014836736) };

                initialPolys[3] = new Point[] {
                    new Point(6.87244607297693,0.0293062742776171),
                    new Point(6.96135493605682,0.0301407014836719),
                    new Point(6.86909824100712,0.0749839012871441) };



                var intersections = new Tuple<Point, Point, Point, Point, Point>[] {
                    Tuple.Create(new Point(9.16344177297135,-1.2537416985989), new Point(7.76145635305171,-1.67761318154907), new Point(9.43425936393152,-1.17186363320799), new Point(9.54274292483309,-1.71657720938275), new Point(8.87261465785801,-0.898865063733471)),
                    Tuple.Create(new Point(10.3058611604882,-1.59552300547359), new Point(9.43425936393152,-1.17186363320799), new Point(10.5549080800541,-1.71657720938275), new Point(10.3058611604882,-1.59552300547356), new Point(10.3269392559373,-1.71657720938275)),
                    Tuple.Create(new Point(10.3269392559373,-1.71657720938275), new Point(10.5549080800541,-1.71657720938275), new Point(7.63258002821866,-1.71657720938275), new Point(10.3058611604882,-1.59552300547356), new Point(10.3269392559373,-1.71657720938275)),
                    Tuple.Create(new Point(9.54274292483309,-1.71657720938275), new Point(10.5549080800541,-1.71657720938275), new Point(7.63258002821866,-1.71657720938275), new Point(9.54274292483309,-1.71657720938275), new Point(8.87261465785801,-0.898865063733471)),
                    Tuple.Create(new Point(9.16344177297134,-1.25374169859889), new Point(9.54274292483309,-1.71657720938275), new Point(8.87261465785801,-0.898865063733471), new Point(9.43425936393151,-1.17186363320798), new Point(7.7614563530517,-1.67761318154907)),
                    Tuple.Create(new Point(8.87261465785801,-0.898865063733473), new Point(9.54274292483309,-1.71657720938275), new Point(8.87261465785801,-0.898865063733471), new Point(6.96135493605682,0.0301407014836736), new Point(9.43425936393151,-1.17186363320798)),
                    Tuple.Create(new Point(6.87244607297693,0.0293062742776171), new Point(7.00040640990406,-1.71657720938275), new Point(6.87244607297693,0.029306274277618), new Point(6.87244607297693,0.0293062742776171), new Point(6.96135493605682,0.0301407014836719)),
                    Tuple.Create(new Point(6.96135493605682,0.0301407014836736), new Point(6.87244607297693,0.029306274277618), new Point(6.96135493605682,0.0301407014836736), new Point(6.96135493605682,0.0301407014836719), new Point(6.86909824100712,0.0749839012871441)) }.
                    Select(o => new { Intersect = o.Item1, Segment1 = Tuple.Create(o.Item2, o.Item3), Segment2 = Tuple.Create(o.Item4, o.Item5) }).ToArray();


                // Separate into islands
                Polygon2D[] finalPolys = null;
                try
                {
                    finalPolys = GetPolyUnion(initialPolys);
                }
                catch (Exception ex)
                {
                    //MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                    finalPolys = new Polygon2D[0];
                }

                #region Draw

                Visual3D visual;

                // Lines
                for (int cntr = 0; cntr < initialPolys.Length; cntr++)
                {
                    double z = -2 - (cntr * .1);

                    visual = GetVisual_PolygonLines(initialPolys[cntr].Select(o => o.ToPoint3D(z)).ToArray(), Colors.Gray, 1);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);

                    foreach (Point point in initialPolys[cntr])
                    {
                        visual = GetVisual_Dot(point.ToPoint3D(z), .01d, Colors.Gray);
                        _debugVisuals.Add(visual);
                        _viewport.Children.Add(visual);
                    }
                }


                for (int cntr = 0; cntr < intersections.Length; cntr++)
                {
                    double z = cntr * .1d;

                    visual = GetVisual_Dot(intersections[cntr].Intersect.ToPoint3D(z), .01d, Colors.Gray);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);

                    visual = GetVisual_Line(intersections[cntr].Segment1.Item1.ToPoint3D(z), intersections[cntr].Segment1.Item2.ToPoint3D(z), Colors.Gray, 2d);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);

                    visual = GetVisual_Line(intersections[cntr].Segment2.Item1.ToPoint3D(z), intersections[cntr].Segment2.Item2.ToPoint3D(z), Colors.Gray, 2d);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }


                foreach (Polygon2D polygon in finalPolys)
                {
                    visual = GetVisual_PolygonLines(polygon.Polygon.Select(o => o.ToPoint3D(2)).ToArray(), Colors.Black, 2);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);

                    foreach (Point[] hole in polygon.Holes)
                    {
                        visual = GetVisual_PolygonLines(hole.Select(o => o.ToPoint3D(2)).ToArray(), Colors.Red, 2);
                        _debugVisuals.Add(visual);
                        _viewport.Children.Add(visual);
                    }
                }

                // Polygons
                //foreach (Point[] polygon in finalPolys)       // can't do these, they are concave
                //foreach (Point[] polygon in initialPolys)
                for (int cntr = 0; cntr < initialPolys.Length; cntr++)
                {
                    Color color = UtilityWPF.GetColorEGA(10, cntr);
                    //Color color = Color.FromArgb(10, Convert.ToByte(StaticRandom.Next(256)), Convert.ToByte(StaticRandom.Next(256)), Convert.ToByte(StaticRandom.Next(256)));

                    visual = GetVisual_Polygon_Convex(initialPolys[cntr].Select(o => o.ToPoint3D(-2 - (cntr * .1))).ToArray(), color);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnChainTest1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearAllVisuals();

                List<Tuple<Point, Point>> segments1 = new List<Tuple<Point, Point>>();

                segments1.Add(Tuple.Create(new Point(-1, -1), new Point(0, -1)));        //0
                segments1.Add(Tuple.Create(new Point(0, 1), new Point(1, -1)));      //1
                segments1.Add(Tuple.Create(new Point(0, 1), new Point(-1, -1)));     //2
                segments1.Add(Tuple.Create(new Point(0, -1), new Point(0, 1)));        //3
                segments1.Add(Tuple.Create(new Point(1, -1), new Point(0, -1)));       //4

                //var segments = segments1;
                var segments = UtilityHelper.RandomRange(0, segments1.Count).Select(o => segments1[o]).ToList();

                Polygon2D[] final = PolyUnion2.StitchSegments2.Stitch(segments.ToArray());

                #region Draw

                Visual3D visual;

                // Initial
                for (int cntr = 0; cntr < segments.Count; cntr++)
                {
                    double z = -2 - (cntr * .1);

                    visual = GetVisual_Line(segments[cntr].Item1.ToPoint3D(z), segments[cntr].Item2.ToPoint3D(z), Colors.Gray, 1);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                // Final
                foreach (Polygon2D polygon in final)
                {
                    visual = GetVisual_PolygonLines(polygon.Polygon.Select(o => o.ToPoint3D(0)).ToArray(), Colors.Black, 2);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);

                    foreach (Point[] hole in polygon.Holes)
                    {
                        visual = GetVisual_PolygonLines(hole.Select(o => o.ToPoint3D(0)).ToArray(), Colors.Red, 2);
                        _debugVisuals.Add(visual);
                        _viewport.Children.Add(visual);
                    }
                }

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnChainTest2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearAllVisuals();

                List<Tuple<Point, Point>> segments1 = new List<Tuple<Point, Point>>();

                segments1.Add(Tuple.Create(new Point(0, 1), new Point(1, -1)));        //0
                segments1.Add(Tuple.Create(new Point(1, -1), new Point(0, -1)));      //1
                segments1.Add(Tuple.Create(new Point(0, -1), new Point(0, 0)));      //2
                segments1.Add(Tuple.Create(new Point(0, 0), new Point(0, 1)));      //3
                segments1.Add(Tuple.Create(new Point(0, 1), new Point(-1, -1)));      //4
                segments1.Add(Tuple.Create(new Point(-1, -1), new Point(0, -1)));      //5

                //var segments = segments1;
                var segments = UtilityHelper.RandomRange(0, segments1.Count).Select(o => segments1[o]).ToList();

                Polygon2D[] final = PolyUnion2.StitchSegments2.Stitch(segments.ToArray());

                #region Draw

                Visual3D visual;

                // Initial
                for (int cntr = 0; cntr < segments.Count; cntr++)
                {
                    double z = -2 - (cntr * .1);

                    visual = GetVisual_Line(segments[cntr].Item1.ToPoint3D(z), segments[cntr].Item2.ToPoint3D(z), Colors.Gray, 1);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                // Final
                foreach (Polygon2D polygon in final)
                {
                    visual = GetVisual_PolygonLines(polygon.Polygon.Select(o => o.ToPoint3D(0)).ToArray(), Colors.Black, 2);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);

                    foreach (Point[] hole in polygon.Holes)
                    {
                        visual = GetVisual_PolygonLines(hole.Select(o => o.ToPoint3D(0)).ToArray(), Colors.Red, 2);
                        _debugVisuals.Add(visual);
                        _viewport.Children.Add(visual);
                    }
                }

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnChainTest3_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearAllVisuals();

                List<Tuple<Point, Point>> segments1 = new List<Tuple<Point, Point>>();

                segments1.Add(Tuple.Create(new Point(-2, -2), new Point(2, -2)));
                segments1.Add(Tuple.Create(new Point(2, -2), new Point(2, 2)));
                segments1.Add(Tuple.Create(new Point(2, 2), new Point(-2, 2)));
                segments1.Add(Tuple.Create(new Point(-2, 2), new Point(-2, -2)));

                segments1.Add(Tuple.Create(new Point(-1, -1), new Point(1, -1)));
                segments1.Add(Tuple.Create(new Point(1, -1), new Point(0, 1)));
                segments1.Add(Tuple.Create(new Point(0, 1), new Point(-1, -1)));

                //var segments = segments1;
                var segments = UtilityHelper.RandomRange(0, segments1.Count).Select(o => segments1[o]).ToList();

                Polygon2D[] final = PolyUnion2.StitchSegments2.Stitch(segments.ToArray());

                #region Draw

                Visual3D visual;

                // Initial
                for (int cntr = 0; cntr < segments.Count; cntr++)
                {
                    double z = -2 - (cntr * .1);

                    visual = GetVisual_Line(segments[cntr].Item1.ToPoint3D(z), segments[cntr].Item2.ToPoint3D(z), Colors.Gray, 1);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                // Final
                foreach (Polygon2D polygon in final)
                {
                    visual = GetVisual_PolygonLines(polygon.Polygon.Select(o => o.ToPoint3D(0)).ToArray(), Colors.Black, 2);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);

                    foreach (Point[] hole in polygon.Holes)
                    {
                        visual = GetVisual_PolygonLines(hole.Select(o => o.ToPoint3D(0)).ToArray(), Colors.Red, 2);
                        _debugVisuals.Add(visual);
                        _viewport.Children.Add(visual);
                    }
                }

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnChainTest4_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearAllVisuals();

                List<Tuple<Point, Point>> segments1 = new List<Tuple<Point, Point>>();

                // Square contains triangle
                segments1.Add(Tuple.Create(new Point(-2, -2), new Point(2, -2)));
                segments1.Add(Tuple.Create(new Point(2, -2), new Point(2, 2)));
                segments1.Add(Tuple.Create(new Point(2, 2), new Point(-2, 2)));
                segments1.Add(Tuple.Create(new Point(-2, 2), new Point(-2, -2)));

                segments1.Add(Tuple.Create(new Point(-1, -1), new Point(1, -1)));
                segments1.Add(Tuple.Create(new Point(1, -1), new Point(0, 1)));
                segments1.Add(Tuple.Create(new Point(0, 1), new Point(-1, -1)));

                // Square contains 2 triangles
                double y = 5;
                segments1.Add(Tuple.Create(new Point(-2, -2 + y), new Point(2, -2 + y)));
                segments1.Add(Tuple.Create(new Point(2, -2 + y), new Point(2, 2 + y)));
                segments1.Add(Tuple.Create(new Point(2, 2 + y), new Point(-2, 2 + y)));
                segments1.Add(Tuple.Create(new Point(-2, 2 + y), new Point(-2, -2 + y)));

                y = 4.8;
                segments1.Add(Tuple.Create(new Point(-.1, -.1 + y), new Point(.1, -.1 + y)));
                segments1.Add(Tuple.Create(new Point(.1, -.1 + y), new Point(0, .1 + y)));
                segments1.Add(Tuple.Create(new Point(0, .1 + y), new Point(-.1, -.1 + y)));

                y = 5.2;
                segments1.Add(Tuple.Create(new Point(-.1, -.1 + y), new Point(.1, -.1 + y)));
                segments1.Add(Tuple.Create(new Point(.1, -.1 + y), new Point(0, .1 + y)));
                segments1.Add(Tuple.Create(new Point(0, .1 + y), new Point(-.1, -.1 + y)));

                // Standalone square
                double x = 5;
                segments1.Add(Tuple.Create(new Point(-2 + x, -2), new Point(2 + x, -2)));
                segments1.Add(Tuple.Create(new Point(2 + x, -2), new Point(2 + x, 2)));
                segments1.Add(Tuple.Create(new Point(2 + x, 2), new Point(-2 + x, 2)));
                segments1.Add(Tuple.Create(new Point(-2 + x, 2), new Point(-2 + x, -2)));


                //var segments = segments1;
                var segments = UtilityHelper.RandomRange(0, segments1.Count).Select(o => segments1[o]).ToList();

                Polygon2D[] final = PolyUnion2.StitchSegments2.Stitch(segments.ToArray());

                #region Draw

                Visual3D visual;

                // Initial
                for (int cntr = 0; cntr < segments.Count; cntr++)
                {
                    double z = -2 - (cntr * .1);

                    visual = GetVisual_Line(segments[cntr].Item1.ToPoint3D(z), segments[cntr].Item2.ToPoint3D(z), Colors.Gray, 1);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                // Final
                foreach (Polygon2D polygon in final)
                {
                    visual = GetVisual_PolygonLines(polygon.Polygon.Select(o => o.ToPoint3D(0)).ToArray(), Colors.Black, 2);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);

                    foreach (Point[] hole in polygon.Holes)
                    {
                        visual = GetVisual_PolygonLines(hole.Select(o => o.ToPoint3D(0)).ToArray(), Colors.Red, 2);
                        _debugVisuals.Add(visual);
                        _viewport.Children.Add(visual);
                    }
                }

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnChainTest5a_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearAllVisuals();

                List<Tuple<Point, Point>> segments1 = new List<Tuple<Point, Point>>();

                segments1.Add(Tuple.Create(new Point(0, -2), new Point(.5, -1)));
                segments1.Add(Tuple.Create(new Point(.5, -1), new Point(0, 0)));
                segments1.Add(Tuple.Create(new Point(0, 0), new Point(-.5, -1)));
                segments1.Add(Tuple.Create(new Point(-.5, -1), new Point(0, -2)));
                segments1.Add(Tuple.Create(new Point(0, -2), new Point(0, 0)));

                segments1.Add(Tuple.Create(new Point(0, 2), new Point(.5, 1)));
                segments1.Add(Tuple.Create(new Point(.5, 1), new Point(0, 0)));
                segments1.Add(Tuple.Create(new Point(0, 0), new Point(-.5, 1)));
                segments1.Add(Tuple.Create(new Point(-.5, 1), new Point(0, 2)));
                segments1.Add(Tuple.Create(new Point(0, 2), new Point(0, 0)));

                segments1.Add(Tuple.Create(new Point(0, -2), new Point(-1, -1.75)));
                segments1.Add(Tuple.Create(new Point(-1, -1.75), new Point(-1, 1.75)));
                segments1.Add(Tuple.Create(new Point(-1, 1.75), new Point(0, 2)));

                var segments = segments1;
                //var segments = UtilityHelper.RandomRange(0, segments1.Count).Select(o => segments1[o]).ToList();

                Polygon2D[] final = PolyUnion2.StitchSegments2.Stitch(segments.ToArray());

                #region Draw

                Visual3D visual;

                // Initial
                for (int cntr = 0; cntr < segments.Count; cntr++)
                {
                    double z = -2 - (cntr * .1);

                    visual = GetVisual_Line(segments[cntr].Item1.ToPoint3D(z), segments[cntr].Item2.ToPoint3D(z), Colors.Gray, 1);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                // Final
                foreach (Polygon2D polygon in final)
                {
                    visual = GetVisual_PolygonLines(polygon.Polygon.Select(o => o.ToPoint3D(0)).ToArray(), Colors.Black, 2);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);

                    foreach (Point[] hole in polygon.Holes)
                    {
                        visual = GetVisual_PolygonLines(hole.Select(o => o.ToPoint3D(0)).ToArray(), Colors.Red, 2);
                        _debugVisuals.Add(visual);
                        _viewport.Children.Add(visual);
                    }
                }

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnChainTest5b_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearAllVisuals();

                List<Tuple<Point, Point>> segments1 = new List<Tuple<Point, Point>>();

                segments1.Add(Tuple.Create(new Point(0, -2), new Point(.5, -1)));
                segments1.Add(Tuple.Create(new Point(.5, -1), new Point(0, -.25)));
                segments1.Add(Tuple.Create(new Point(0, -.25), new Point(-.5, -1)));
                segments1.Add(Tuple.Create(new Point(-.5, -1), new Point(0, -2)));
                segments1.Add(Tuple.Create(new Point(0, -2), new Point(0, -.25)));

                segments1.Add(Tuple.Create(new Point(0, -.25), new Point(0, .25)));

                segments1.Add(Tuple.Create(new Point(0, 2), new Point(.5, 1)));
                segments1.Add(Tuple.Create(new Point(.5, 1), new Point(0, .25)));
                segments1.Add(Tuple.Create(new Point(0, .25), new Point(-.5, 1)));
                segments1.Add(Tuple.Create(new Point(-.5, 1), new Point(0, 2)));
                segments1.Add(Tuple.Create(new Point(0, 2), new Point(0, .25)));

                segments1.Add(Tuple.Create(new Point(0, -2), new Point(-1, -1.75)));
                segments1.Add(Tuple.Create(new Point(-1, -1.75), new Point(-1, 1.75)));
                segments1.Add(Tuple.Create(new Point(-1, 1.75), new Point(0, 2)));

                var segments = segments1;
                //var segments = UtilityHelper.RandomRange(0, segments1.Count).Select(o => segments1[o]).ToList();

                Polygon2D[] final = PolyUnion2.StitchSegments2.Stitch(segments.ToArray());

                #region Draw

                Visual3D visual;

                // Initial
                for (int cntr = 0; cntr < segments.Count; cntr++)
                {
                    double z = -2 - (cntr * .1);

                    visual = GetVisual_Line(segments[cntr].Item1.ToPoint3D(z), segments[cntr].Item2.ToPoint3D(z), Colors.Gray, 1);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                // Final
                foreach (Polygon2D polygon in final)
                {
                    visual = GetVisual_PolygonLines(polygon.Polygon.Select(o => o.ToPoint3D(0)).ToArray(), Colors.Black, 2);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);

                    foreach (Point[] hole in polygon.Holes)
                    {
                        visual = GetVisual_PolygonLines(hole.Select(o => o.ToPoint3D(0)).ToArray(), Colors.Red, 2);
                        _debugVisuals.Add(visual);
                        _viewport.Children.Add(visual);
                    }
                }

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnChainTest5c_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearAllVisuals();

                List<Tuple<Point, Point>> segments1 = new List<Tuple<Point, Point>>();

                segments1.Add(Tuple.Create(new Point(0, -2), new Point(.5, -1)));
                segments1.Add(Tuple.Create(new Point(.5, -1), new Point(0, -.25)));
                segments1.Add(Tuple.Create(new Point(0, -.25), new Point(-.5, -1)));
                segments1.Add(Tuple.Create(new Point(-.5, -1), new Point(0, -2)));
                segments1.Add(Tuple.Create(new Point(0, -2), new Point(0, -.25)));

                segments1.Add(Tuple.Create(new Point(0, -.25), new Point(0, .25)));

                segments1.Add(Tuple.Create(new Point(0, 2), new Point(.5, 1)));
                segments1.Add(Tuple.Create(new Point(.5, 1), new Point(0, .25)));
                segments1.Add(Tuple.Create(new Point(0, .25), new Point(-.5, 1)));
                segments1.Add(Tuple.Create(new Point(-.5, 1), new Point(0, 2)));
                segments1.Add(Tuple.Create(new Point(0, 2), new Point(0, .25)));

                segments1.Add(Tuple.Create(new Point(0, -2), new Point(-1, -1.75)));
                segments1.Add(Tuple.Create(new Point(-1, -1.75), new Point(-1, 1.75)));
                segments1.Add(Tuple.Create(new Point(-1, 1.75), new Point(0, 2)));

                segments1.Add(Tuple.Create(new Point(0, -2), new Point(1, -1.75)));
                segments1.Add(Tuple.Create(new Point(1, -1.75), new Point(1, 1.75)));
                segments1.Add(Tuple.Create(new Point(1, 1.75), new Point(0, 2)));

                segments1.Add(Tuple.Create(new Point(0, -2), new Point(2, -1.75)));
                segments1.Add(Tuple.Create(new Point(2, -1.75), new Point(2, 1.75)));
                segments1.Add(Tuple.Create(new Point(2, 1.75), new Point(0, 2)));

                var segments = segments1;
                //var segments = UtilityHelper.RandomRange(0, segments1.Count).Select(o => segments1[o]).ToList();

                Polygon2D[] final = PolyUnion2.StitchSegments2.Stitch(segments.ToArray());

                #region Draw

                Visual3D visual;

                // Initial
                for (int cntr = 0; cntr < segments.Count; cntr++)
                {
                    double z = -2 - (cntr * .1);

                    visual = GetVisual_Line(segments[cntr].Item1.ToPoint3D(z), segments[cntr].Item2.ToPoint3D(z), Colors.Gray, 1);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                // Final
                foreach (Polygon2D polygon in final)
                {
                    visual = GetVisual_PolygonLines(polygon.Polygon.Select(o => o.ToPoint3D(0)).ToArray(), Colors.Black, 2);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);

                    foreach (Point[] hole in polygon.Holes)
                    {
                        visual = GetVisual_PolygonLines(hole.Select(o => o.ToPoint3D(0)).ToArray(), Colors.Red, 2);
                        _debugVisuals.Add(visual);
                        _viewport.Children.Add(visual);
                    }
                }

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnChainTest6_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearAllVisuals();

                List<Tuple<Point, Point>> segments1 = new List<Tuple<Point, Point>>();

                // Left triangle
                segments1.Add(Tuple.Create(new Point(-2, -1), new Point(0, -1)));
                segments1.Add(Tuple.Create(new Point(0, -1), new Point(-1, 1)));
                segments1.Add(Tuple.Create(new Point(-1, 1), new Point(-2, -1)));

                // Right triangle
                segments1.Add(Tuple.Create(new Point(2, -1), new Point(0, -1)));
                segments1.Add(Tuple.Create(new Point(0, -1), new Point(1, 1)));
                segments1.Add(Tuple.Create(new Point(1, 1), new Point(2, -1)));

                // Middle triangle
                segments1.Add(Tuple.Create(new Point(0, -1), new Point(-1, 1)));
                segments1.Add(Tuple.Create(new Point(-1, 1), new Point(1, 1)));
                segments1.Add(Tuple.Create(new Point(1, 1), new Point(0, -1)));

                var segments = segments1;
                //var segments = UtilityHelper.RandomRange(0, segments1.Count).Select(o => segments1[o]).ToList();

                Polygon2D[] final = PolyUnion2.StitchSegments2.Stitch(segments.ToArray());

                #region Draw

                Visual3D visual;

                // Initial
                for (int cntr = 0; cntr < segments.Count; cntr++)
                {
                    double z = -2 - (cntr * .1);

                    visual = GetVisual_Line(segments[cntr].Item1.ToPoint3D(z), segments[cntr].Item2.ToPoint3D(z), Colors.Gray, 1);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                // Final
                foreach (Polygon2D polygon in final)
                {
                    visual = GetVisual_PolygonLines(polygon.Polygon.Select(o => o.ToPoint3D(0)).ToArray(), Colors.Black, 2);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);

                    foreach (Point[] hole in polygon.Holes)
                    {
                        visual = GetVisual_PolygonLines(hole.Select(o => o.ToPoint3D(0)).ToArray(), Colors.Red, 2);
                        _debugVisuals.Add(visual);
                        _viewport.Children.Add(visual);
                    }
                }

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnChainTest7a_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearAllVisuals();

                List<Tuple<Point, Point>> segments1 = new List<Tuple<Point, Point>>();

                segments1.Add(Tuple.Create(new Point(-2, -2), new Point(2, -2)));
                segments1.Add(Tuple.Create(new Point(2, -2), new Point(2, 2)));
                segments1.Add(Tuple.Create(new Point(2, 2), new Point(-2, 2)));
                segments1.Add(Tuple.Create(new Point(-2, 2), new Point(-2, -2)));

                segments1.Add(Tuple.Create(new Point(0, -1), new Point(1, 0)));
                segments1.Add(Tuple.Create(new Point(1, 0), new Point(0, 1)));
                segments1.Add(Tuple.Create(new Point(0, 1), new Point(-1, 0)));
                segments1.Add(Tuple.Create(new Point(-1, 0), new Point(0, -1)));
                segments1.Add(Tuple.Create(new Point(0, -1), new Point(0, 1)));

                //var segments = segments1;
                var segments = UtilityHelper.RandomRange(0, segments1.Count).Select(o => segments1[o]).ToList();

                Polygon2D[] final = PolyUnion2.StitchSegments2.Stitch(segments.ToArray());

                #region Draw

                Visual3D visual;

                // Initial
                for (int cntr = 0; cntr < segments.Count; cntr++)
                {
                    double z = -2 - (cntr * .1);

                    visual = GetVisual_Line(segments[cntr].Item1.ToPoint3D(z), segments[cntr].Item2.ToPoint3D(z), Colors.Gray, 1);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                // Final
                foreach (Polygon2D polygon in final)
                {
                    visual = GetVisual_PolygonLines(polygon.Polygon.Select(o => o.ToPoint3D(0)).ToArray(), Colors.Black, 2);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);

                    foreach (Point[] hole in polygon.Holes)
                    {
                        visual = GetVisual_PolygonLines(hole.Select(o => o.ToPoint3D(0)).ToArray(), Colors.Red, 2);
                        _debugVisuals.Add(visual);
                        _viewport.Children.Add(visual);
                    }
                }

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnChainTest7b_Click(object sender, RoutedEventArgs e)
        {
            //NOTE: I don't think 7b and 7c will occur, so I'm not going to bother fixing them

            try
            {
                ClearAllVisuals();

                List<Tuple<Point, Point>> segments1 = new List<Tuple<Point, Point>>();

                segments1.Add(Tuple.Create(new Point(-2, -2), new Point(2, -2)));
                segments1.Add(Tuple.Create(new Point(2, -2), new Point(2, 0)));
                segments1.Add(Tuple.Create(new Point(2, 0), new Point(2, 2)));
                segments1.Add(Tuple.Create(new Point(2, 2), new Point(-2, 2)));
                segments1.Add(Tuple.Create(new Point(-2, 2), new Point(-2, -2)));

                segments1.Add(Tuple.Create(new Point(0, -1), new Point(1, 0)));
                segments1.Add(Tuple.Create(new Point(1, 0), new Point(0, 1)));
                segments1.Add(Tuple.Create(new Point(0, 1), new Point(-1, 0)));
                segments1.Add(Tuple.Create(new Point(-1, 0), new Point(0, -1)));
                segments1.Add(Tuple.Create(new Point(0, -1), new Point(0, 1)));

                segments1.Add(Tuple.Create(new Point(1, 0), new Point(2, 0)));

                //var segments = segments1;
                var segments = UtilityHelper.RandomRange(0, segments1.Count).Select(o => segments1[o]).ToList();

                Polygon2D[] final = null;
                try
                {
                    final = PolyUnion2.StitchSegments2.Stitch(segments.ToArray());
                }
                catch (Exception)
                {
                    final = new Polygon2D[0];
                }

                #region Draw

                Visual3D visual;

                // Initial
                for (int cntr = 0; cntr < segments.Count; cntr++)
                {
                    double z = -2 - (cntr * .1);

                    visual = GetVisual_Line(segments[cntr].Item1.ToPoint3D(z), segments[cntr].Item2.ToPoint3D(z), Colors.Gray, 1);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                // Final
                foreach (Polygon2D polygon in final)
                {
                    visual = GetVisual_PolygonLines(polygon.Polygon.Select(o => o.ToPoint3D(0)).ToArray(), Colors.Black, 2);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);

                    foreach (Point[] hole in polygon.Holes)
                    {
                        visual = GetVisual_PolygonLines(hole.Select(o => o.ToPoint3D(0)).ToArray(), Colors.Red, 2);
                        _debugVisuals.Add(visual);
                        _viewport.Children.Add(visual);
                    }
                }

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnChainTest7c_Click(object sender, RoutedEventArgs e)
        {
            //NOTE: I don't think 7b and 7c will occur, so I'm not going to bother fixing them

            try
            {
                ClearAllVisuals();

                List<Tuple<Point, Point>> segments1 = new List<Tuple<Point, Point>>();

                segments1.Add(Tuple.Create(new Point(-2, -2), new Point(2, -2)));
                segments1.Add(Tuple.Create(new Point(2, -2), new Point(2, 0)));
                segments1.Add(Tuple.Create(new Point(2, 0), new Point(2, 2)));
                segments1.Add(Tuple.Create(new Point(2, 2), new Point(-2, 2)));
                segments1.Add(Tuple.Create(new Point(-2, 2), new Point(-2, -2)));

                segments1.Add(Tuple.Create(new Point(4, -1), new Point(5, 0)));
                segments1.Add(Tuple.Create(new Point(5, 0), new Point(4, 1)));
                segments1.Add(Tuple.Create(new Point(4, 1), new Point(3, 0)));
                segments1.Add(Tuple.Create(new Point(3, 0), new Point(4, -1)));
                segments1.Add(Tuple.Create(new Point(4, -1), new Point(4, 1)));

                segments1.Add(Tuple.Create(new Point(3, 0), new Point(2, 0)));

                //var segments = segments1;
                var segments = UtilityHelper.RandomRange(0, segments1.Count).Select(o => segments1[o]).ToList();

                Polygon2D[] final = null;
                try
                {
                    final = PolyUnion2.StitchSegments2.Stitch(segments.ToArray());
                }
                catch (Exception)
                {
                    final = new Polygon2D[0];
                }

                #region Draw

                Visual3D visual;

                // Initial
                for (int cntr = 0; cntr < segments.Count; cntr++)
                {
                    double z = -2 - (cntr * .1);

                    visual = GetVisual_Line(segments[cntr].Item1.ToPoint3D(z), segments[cntr].Item2.ToPoint3D(z), Colors.Gray, 1);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                // Final
                foreach (Polygon2D polygon in final)
                {
                    visual = GetVisual_PolygonLines(polygon.Polygon.Select(o => o.ToPoint3D(0)).ToArray(), Colors.Black, 2);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);

                    foreach (Point[] hole in polygon.Holes)
                    {
                        visual = GetVisual_PolygonLines(hole.Select(o => o.ToPoint3D(0)).ToArray(), Colors.Red, 2);
                        _debugVisuals.Add(visual);
                        _viewport.Children.Add(visual);
                    }
                }

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnChainTest7d_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearAllVisuals();

                List<Tuple<Point, Point>> segments1 = new List<Tuple<Point, Point>>();

                segments1.Add(Tuple.Create(new Point(-2, -2), new Point(2, -2)));
                segments1.Add(Tuple.Create(new Point(2, -2), new Point(2, 2)));
                segments1.Add(Tuple.Create(new Point(2, 2), new Point(-2, 2)));
                segments1.Add(Tuple.Create(new Point(-2, 2), new Point(-2, -2)));

                segments1.Add(Tuple.Create(new Point(0, -1), new Point(1, 0)));
                segments1.Add(Tuple.Create(new Point(1, 0), new Point(0, 1)));
                segments1.Add(Tuple.Create(new Point(0, 1), new Point(-1, 0)));
                segments1.Add(Tuple.Create(new Point(-1, 0), new Point(0, -1)));
                segments1.Add(Tuple.Create(new Point(0, -1), new Point(0, 1)));

                segments1.Add(Tuple.Create(new Point(1, 0), new Point(1.5, .5)));

                //var segments = segments1;
                var segments = UtilityHelper.RandomRange(0, segments1.Count).Select(o => segments1[o]).ToList();

                Polygon2D[] final = PolyUnion2.StitchSegments2.Stitch(segments.ToArray());

                #region Draw

                Visual3D visual;

                // Initial
                for (int cntr = 0; cntr < segments.Count; cntr++)
                {
                    double z = -2 - (cntr * .1);

                    visual = GetVisual_Line(segments[cntr].Item1.ToPoint3D(z), segments[cntr].Item2.ToPoint3D(z), Colors.Gray, 1);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                // Final
                foreach (Polygon2D polygon in final)
                {
                    visual = GetVisual_PolygonLines(polygon.Polygon.Select(o => o.ToPoint3D(0)).ToArray(), Colors.Black, 2);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);

                    foreach (Point[] hole in polygon.Holes)
                    {
                        visual = GetVisual_PolygonLines(hole.Select(o => o.ToPoint3D(0)).ToArray(), Colors.Red, 2);
                        _debugVisuals.Add(visual);
                        _viewport.Children.Add(visual);
                    }
                }

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnChainTest8a_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearAllVisuals();

                List<Tuple<Point, Point>> segments1 = new List<Tuple<Point, Point>>();

                segments1.Add(Tuple.Create(new Point(-3, 2), new Point(-2, 1)));
                segments1.Add(Tuple.Create(new Point(-2, 1), new Point(-1, 0)));
                segments1.Add(Tuple.Create(new Point(-1, 0), new Point(-2, -1)));
                segments1.Add(Tuple.Create(new Point(-2, -1), new Point(-3, -2)));
                segments1.Add(Tuple.Create(new Point(-3, -2), new Point(-4, -1)));
                segments1.Add(Tuple.Create(new Point(-4, -1), new Point(-5, 0)));
                segments1.Add(Tuple.Create(new Point(-5, 0), new Point(-4, 1)));
                segments1.Add(Tuple.Create(new Point(-4, 1), new Point(-3, 2)));
                segments1.Add(Tuple.Create(new Point(-3, 2), new Point(-3, 0)));
                segments1.Add(Tuple.Create(new Point(-3, 0), new Point(-3, -2)));
                segments1.Add(Tuple.Create(new Point(-2, 1), new Point(-2, -1)));
                segments1.Add(Tuple.Create(new Point(-4, 1), new Point(-4, -1)));
                segments1.Add(Tuple.Create(new Point(-2, 1), new Point(-3, 0)));
                segments1.Add(Tuple.Create(new Point(-2, -1), new Point(-3, 0)));
                segments1.Add(Tuple.Create(new Point(-4, 1), new Point(-3, 0)));
                segments1.Add(Tuple.Create(new Point(-4, -1), new Point(-3, 0)));

                var segments = segments1;
                //var segments = UtilityHelper.RandomRange(0, segments1.Count).Select(o => segments1[o]).ToList();

                Polygon2D[] final = null;
                try
                {
                    final = PolyUnion2.StitchSegments2.Stitch(segments.ToArray());
                }
                catch (Exception)
                {
                    final = new Polygon2D[0];
                }

                #region Draw

                Visual3D visual;

                // Initial
                for (int cntr = 0; cntr < segments.Count; cntr++)
                {
                    double z = -2 - (cntr * .1);

                    visual = GetVisual_Line(segments[cntr].Item1.ToPoint3D(z), segments[cntr].Item2.ToPoint3D(z), Colors.Gray, 1);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                // Final
                foreach (Polygon2D polygon in final)
                {
                    visual = GetVisual_PolygonLines(polygon.Polygon.Select(o => o.ToPoint3D(0)).ToArray(), Colors.Black, 2);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);

                    foreach (Point[] hole in polygon.Holes)
                    {
                        visual = GetVisual_PolygonLines(hole.Select(o => o.ToPoint3D(0)).ToArray(), Colors.Red, 2);
                        _debugVisuals.Add(visual);
                        _viewport.Children.Add(visual);
                    }
                }

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnChainTest8b_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearAllVisuals();

                List<Tuple<Point, Point>> segments1 = new List<Tuple<Point, Point>>();

                segments1.Add(Tuple.Create(new Point(0, -5), new Point(4, -1)));

                segments1.Add(Tuple.Create(new Point(4, -1), new Point(5, 0)));
                segments1.Add(Tuple.Create(new Point(5, 0), new Point(4, 1)));
                segments1.Add(Tuple.Create(new Point(4, 1), new Point(3, 0)));
                segments1.Add(Tuple.Create(new Point(3, 0), new Point(4, -1)));
                segments1.Add(Tuple.Create(new Point(4, -1), new Point(4, 1)));

                segments1.Add(Tuple.Create(new Point(4, 1), new Point(0, 5)));
                segments1.Add(Tuple.Create(new Point(0, 5), new Point(-3, 2)));

                segments1.Add(Tuple.Create(new Point(-3, 2), new Point(-2, 1)));
                segments1.Add(Tuple.Create(new Point(-2, 1), new Point(-1, 0)));
                segments1.Add(Tuple.Create(new Point(-1, 0), new Point(-2, -1)));
                segments1.Add(Tuple.Create(new Point(-2, -1), new Point(-3, -2)));
                segments1.Add(Tuple.Create(new Point(-3, -2), new Point(-4, -1)));
                segments1.Add(Tuple.Create(new Point(-4, -1), new Point(-5, 0)));
                segments1.Add(Tuple.Create(new Point(-5, 0), new Point(-4, 1)));
                segments1.Add(Tuple.Create(new Point(-4, 1), new Point(-3, 2)));
                segments1.Add(Tuple.Create(new Point(-3, 2), new Point(-3, 0)));
                segments1.Add(Tuple.Create(new Point(-3, 0), new Point(-3, -2)));
                segments1.Add(Tuple.Create(new Point(-2, 1), new Point(-2, -1)));
                segments1.Add(Tuple.Create(new Point(-4, 1), new Point(-4, -1)));
                segments1.Add(Tuple.Create(new Point(-2, 1), new Point(-3, 0)));
                segments1.Add(Tuple.Create(new Point(-2, -1), new Point(-3, 0)));
                segments1.Add(Tuple.Create(new Point(-4, 1), new Point(-3, 0)));
                segments1.Add(Tuple.Create(new Point(-4, -1), new Point(-3, 0)));

                segments1.Add(Tuple.Create(new Point(-3, -2), new Point(0, -5)));

                //var segments = segments1;
                var segments = UtilityHelper.RandomRange(0, segments1.Count).Select(o => segments1[o]).ToList();

                Polygon2D[] final = null;
                try
                {
                    final = PolyUnion2.StitchSegments2.Stitch(segments.ToArray());
                }
                catch (Exception)
                {
                    final = new Polygon2D[0];
                }

                #region Draw

                Visual3D visual;

                // Initial
                for (int cntr = 0; cntr < segments.Count; cntr++)
                {
                    double z = -2 - (cntr * .1);

                    visual = GetVisual_Line(segments[cntr].Item1.ToPoint3D(z), segments[cntr].Item2.ToPoint3D(z), Colors.Gray, 1);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                // Final
                foreach (Polygon2D polygon in final)
                {
                    visual = GetVisual_PolygonLines(polygon.Polygon.Select(o => o.ToPoint3D(0)).ToArray(), Colors.Black, 2);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);

                    foreach (Point[] hole in polygon.Holes)
                    {
                        visual = GetVisual_PolygonLines(hole.Select(o => o.ToPoint3D(0)).ToArray(), Colors.Red, 2);
                        _debugVisuals.Add(visual);
                        _viewport.Children.Add(visual);
                    }
                }

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnChainTest9_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearAllVisuals();

                List<Tuple<Point, Point>> segments1 = new List<Tuple<Point, Point>>();

                segments1.Add(Tuple.Create(new Point(-1, -1), new Point(1, -1)));
                segments1.Add(Tuple.Create(new Point(1, -1), new Point(0, 1)));
                segments1.Add(Tuple.Create(new Point(0, 1), new Point(-1, -1)));

                segments1.Add(Tuple.Create(new Point(1, -1), new Point(0, 0)));

                //var segments = segments1;
                var segments = UtilityHelper.RandomRange(0, segments1.Count).Select(o => segments1[o]).ToList();

                Polygon2D[] final = PolyUnion2.StitchSegments2.Stitch(segments.ToArray());

                #region Draw

                Visual3D visual;

                // Initial
                for (int cntr = 0; cntr < segments.Count; cntr++)
                {
                    double z = -2 - (cntr * .1);

                    visual = GetVisual_Line(segments[cntr].Item1.ToPoint3D(z), segments[cntr].Item2.ToPoint3D(z), Colors.Gray, 1);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                // Final
                foreach (Polygon2D polygon in final)
                {
                    visual = GetVisual_PolygonLines(polygon.Polygon.Select(o => o.ToPoint3D(0)).ToArray(), Colors.Black, 2);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);

                    foreach (Point[] hole in polygon.Holes)
                    {
                        visual = GetVisual_PolygonLines(hole.Select(o => o.ToPoint3D(0)).ToArray(), Colors.Red, 2);
                        _debugVisuals.Add(visual);
                        _viewport.Children.Add(visual);
                    }
                }

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnIsInsidePolygon1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearAllVisuals();

                List<Point> poly1 = new List<Point>();
                poly1.Add(new Point(5.51472950351997, -12.3950481070757));
                poly1.Add(new Point(7.72718275249385, -12.0511137017887));
                poly1.Add(new Point(-11.1130107782599, -8.39494922109216));

                List<Point> poly2 = new List<Point>();
                poly2.Add(new Point(-9.98428706863476, 9.78867672251086));
                poly2.Add(new Point(-2.93446737282418, -8.42832213770597));
                poly2.Add(new Point(0.0415494140861985, -11.7432056755654));

                List<Point> testPoints = new List<Point>();
                testPoints.Add(new Point(6.62095612800691, -12.2230809044322));
                testPoints.Add(new Point(3.58143956898125, -11.2465827425442));
                testPoints.Add(new Point(-0.904764693031445, -10.3759812463788));
                testPoints.Add(new Point(-6.17911827489571, -9.352429965275));
                testPoints.Add(new Point(-5.91637302922953, -9.64509292784312));
                testPoints.Add(new Point(-0.513399032570193, -10.9448744915904));
                testPoints.Add(new Point(2.60383335928938, -11.6947802278312));
                testPoints.Add(new Point(-6.45937722072947, 0.680177292402444));
                testPoints.Add(new Point(-2.08984657217786, -9.3691164235819));
                testPoints.Add(new Point(-0.98248052586536, -10.602573672026));
                testPoints.Add(new Point(-0.33909293305649, -11.3192211550797));
                testPoints.Add(new Point(-0.132756685427504, -11.3688590120761));
                testPoints.Add(new Point(-0.435683199736278, -10.7182820659432));
                testPoints.Add(new Point(-5.27429534158305, -0.326687530394448));

                //bool? includeEdgeHits = true;
                bool? includeEdgeHits = false;
                //bool? includeEdgeHits = null;

                var isInside = testPoints.Select(o => new
                    {
                        Point = o,
                        IsInside1 = Math2D.IsInsidePolygon(poly1.ToArray(), o, includeEdgeHits),
                        IsInside2 = Math2D.IsInsidePolygon(poly2.ToArray(), o, includeEdgeHits)
                    }).ToArray();

                #region Draw

                Visual3D visual;

                // Points
                foreach (var testPoint in isInside)
                {
                    Color color = Colors.Gold;

                    if (testPoint.IsInside1 && testPoint.IsInside2)
                    {
                        color = Colors.Green;
                    }
                    else if (testPoint.IsInside1)
                    {
                        color = Colors.DimGray;
                    }
                    else if (testPoint.IsInside2)
                    {
                        color = Colors.Orange;
                    }
                    else
                    {
                        color = Colors.White;
                    }

                    visual = GetVisual_Dot(testPoint.Point.ToPoint3D(), .05d, color);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                // Polygon 1
                visual = GetVisual_Polygon_Convex(poly1.Select(o => o.ToPoint3D()).ToArray(), Colors.Silver);
                _debugVisuals.Add(visual);
                _viewport.Children.Add(visual);

                // Polygon 2
                visual = GetVisual_Polygon_Convex(poly2.Select(o => o.ToPoint3D()).ToArray(), Colors.LightYellow);
                _debugVisuals.Add(visual);
                _viewport.Children.Add(visual);

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnIsInsidePolygon2_Click(object sender, RoutedEventArgs e)
        {
            const double RADIUS = 20d;

            try
            {
                ClearAllVisuals();

                Point[] triangle = Enumerable.Range(0, 3).
                    Select(o => Math3D.GetRandomVectorSpherical2D(RADIUS)).
                    Select(o => new Point(o.X, o.Y)).
                    ToArray();

                var isInside = Enumerable.Range(0, 200).
                    Select(o => Math3D.GetRandomVectorSpherical2D(RADIUS)).
                    Select(o => new Point(o.X, o.Y)).
                    Select(o => new
                        {
                            Point = o,
                            IsInside = Math2D.IsInsidePolygon(triangle, o, false),
                        }).
                        ToArray();

                #region Draw

                Visual3D visual;

                // Points
                foreach (var testPoint in isInside)
                {
                    visual = GetVisual_Dot(testPoint.Point.ToPoint3D(), .05d, testPoint.IsInside ? Colors.DarkSeaGreen : Colors.Tomato);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                // Polygon
                visual = GetVisual_Polygon_Convex(triangle.Select(o => o.ToPoint3D()).ToArray(), Colors.Silver);
                _debugVisuals.Add(visual);
                _viewport.Children.Add(visual);

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnIntersectTriangles1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearAllVisuals();

                //Triangle[] triangles = GetRandomTrianglesPlate(10, 2);
                Triangle[] triangles = GetRandomTrianglesOnion(0, 5, 2);

                Point3D planeIntersectPoint;
                Vector3D planeIntersectDir;
                bool hasPlaneIntersect = Math3D.GetIntersection_Plane_Plane(out planeIntersectPoint, out planeIntersectDir, triangles[0], triangles[1]);

                Tuple<Point3D, Point3D> triangleIntersect = Math3D.GetIntersection_Triangle_Triangle(triangles[0], triangles[1]);

                #region Draw

                Visual3D visual;

                // Plane intersect
                if (hasPlaneIntersect)
                {
                    visual = GetVisual_Line(planeIntersectPoint + (planeIntersectDir * -100), planeIntersectPoint + (planeIntersectDir * 100), Colors.Gray, .5);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                // Triangle intersect
                if (triangleIntersect != null)
                {
                    visual = GetVisual_Line(triangleIntersect.Item1, triangleIntersect.Item2, Colors.HotPink, 3);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                // Triangles
                visual = GetVisual_Triangle(triangles, UtilityWPF.ColorFromHex("80E0E0E0"));
                _debugVisuals.Add(visual);
                _viewport.Children.Add(visual);

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnIntersectTriangles2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearAllVisuals();

                //Triangle[] triangles = GetRandomTrianglesPlate(10, 2);
                Triangle[] triangles = new Triangle[]
                {
                    new Triangle(new Point3D(-1,0,-1), new Point3D(1,0,-1), new Point3D(0,0,1)),
                    new Triangle(new Point3D(0,-1,-1), new Point3D(0,1,-1), new Point3D(0,0,1)),
                };

                Point3D planeIntersectPoint;
                Vector3D planeIntersectDir;
                bool hasPlaneIntersect = Math3D.GetIntersection_Plane_Plane(out planeIntersectPoint, out planeIntersectDir, triangles[0], triangles[1]);

                Tuple<Point3D, Point3D> triangleIntersect = Math3D.GetIntersection_Triangle_Triangle(triangles[0], triangles[1]);

                #region Draw

                Visual3D visual;

                // Plane intersect
                if (hasPlaneIntersect)
                {
                    visual = GetVisual_Line(planeIntersectPoint + (planeIntersectDir * -100), planeIntersectPoint + (planeIntersectDir * 100), Colors.Gray, .5);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                // Triangle intersect
                if (triangleIntersect != null)
                {
                    visual = GetVisual_Line(triangleIntersect.Item1, triangleIntersect.Item2, Colors.HotPink, 3);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                // Triangles
                visual = GetVisual_Triangle(triangles, UtilityWPF.ColorFromHex("80E0E0E0"));
                _debugVisuals.Add(visual);
                _viewport.Children.Add(visual);

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnIntersectTriangles3_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearAllVisuals();

                Triangle[] hull1 = new Triangle[]
                {
                    new Triangle(new Point3D(-0.60, -0.45, 0.01) , new Point3D(0.60, -0.45, 0.01) , new Point3D(0.60, 0.45, 0.01)),
                    new Triangle(new Point3D(0.60, 0.45, 0.01) , new Point3D(-0.60, 0.45, 0.01) , new Point3D(-0.60, -0.45, 0.01)),
                    new Triangle(new Point3D(0.60, 0.45, -0.01) , new Point3D(0.60, -0.45, -0.01) , new Point3D(-0.60, -0.45, -0.01)),
                    new Triangle(new Point3D(-0.60, -0.45, -0.01) , new Point3D(-0.60, 0.45, -0.01) , new Point3D(0.60, 0.45, -0.01)),
                    new Triangle(new Point3D(0.60, -0.45, 0.01) , new Point3D(0.60, -0.45, -0.01) , new Point3D(0.60, 0.45, 0.01)),
                    new Triangle(new Point3D(0.60, -0.45, -0.01) , new Point3D(0.60, 0.45, -0.01) , new Point3D(0.60, 0.45, 0.01)),
                    new Triangle(new Point3D(0.60, 0.45, 0.01) , new Point3D(0.60, 0.45, -0.01) , new Point3D(-0.60, 0.45, 0.01)),
                    new Triangle(new Point3D(-0.60, 0.45, 0.01) , new Point3D(0.60, 0.45, -0.01) , new Point3D(-0.60, 0.45, -0.01)),
                    new Triangle(new Point3D(0.60, -0.45, -0.01) , new Point3D(0.60, -0.45, 0.01) , new Point3D(-0.60, -0.45, 0.01)),
                    new Triangle(new Point3D(-0.60, -0.45, 0.01) , new Point3D(-0.60, -0.45, -0.01) , new Point3D(0.60, -0.45, -0.01)),
                    new Triangle(new Point3D(-0.60, -0.45, -0.01) , new Point3D(-0.60, -0.45, 0.01) , new Point3D(-0.60, 0.45, 0.01)),
                    new Triangle(new Point3D(-0.60, 0.45, 0.01) , new Point3D(-0.60, 0.45, -0.01) , new Point3D(-0.60, -0.45, -0.01)),
                };

                Triangle[] hull2 = new Triangle[]
                {
                    new Triangle(new Point3D(-0.60, -0.36, -0.12) , new Point3D(-0.48, -0.24, -0.12) , new Point3D(-0.48, -0.36, -0.12)),
                    new Triangle(new Point3D(-0.60, -0.36, -0.12) , new Point3D(-0.60, -0.24, -0.12) , new Point3D(-0.48, -0.24, -0.12)),
                    new Triangle(new Point3D(-0.60, -0.36, 0.00) , new Point3D(-0.48, -0.36, 0.00) , new Point3D(-0.48, -0.24, 0.00)),
                    new Triangle(new Point3D(-0.60, -0.36, 0.00) , new Point3D(-0.48, -0.24, 0.00) , new Point3D(-0.60, -0.24, 0.00)),
                    new Triangle(new Point3D(-0.60, -0.36, -0.12) , new Point3D(-0.48, -0.36, -0.12) , new Point3D(-0.60, -0.36, 0.00)),
                    new Triangle(new Point3D(-0.60, -0.36, 0.00) , new Point3D(-0.48, -0.36, -0.12) , new Point3D(-0.48, -0.36, 0.00)),
                    new Triangle(new Point3D(-0.60, -0.24, -0.12) , new Point3D(-0.60, -0.24, 0.00) , new Point3D(-0.48, -0.24, -0.12)),
                    new Triangle(new Point3D(-0.60, -0.24, 0.00) , new Point3D(-0.48, -0.24, 0.00) , new Point3D(-0.48, -0.24, -0.12)),
                    new Triangle(new Point3D(-0.60, -0.36, 0.00) , new Point3D(-0.60, -0.24, -0.12) , new Point3D(-0.60, -0.36, -0.12)),
                    new Triangle(new Point3D(-0.60, -0.36, 0.00) , new Point3D(-0.60, -0.24, 0.00) , new Point3D(-0.60, -0.24, -0.12)),
                    new Triangle(new Point3D(-0.48, -0.36, 0.00) , new Point3D(-0.48, -0.36, -0.12) , new Point3D(-0.48, -0.24, -0.12)),
                    new Triangle(new Point3D(-0.48, -0.36, 0.00) , new Point3D(-0.48, -0.24, -0.12) , new Point3D(-0.48, -0.24, 0.00)),
                };

                #region Draw

                Visual3D visual;

                // Hull1
                visual = GetVisual_HullLines(TriangleIndexed.ConvertToIndexed(hull1), UtilityWPF.ColorFromHex("80E0E0E0"), 1);
                _debugVisuals.Add(visual);
                _viewport.Children.Add(visual);


                //visual = GetVisual_Triangle(hull1, UtilityWPF.ColorFromHex("80E0E0E0"));
                //_debugVisuals.Add(visual);
                //_viewport.Children.Add(visual);

                for (int cntr = 0; cntr < hull1.Length; cntr++)
                {
                    visual = GetVisual_Triangle(new ITriangle[] { hull1[cntr] }, UtilityWPF.GetColorEGA(192, cntr));
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                // Hull2
                visual = GetVisual_HullLines(TriangleIndexed.ConvertToIndexed(hull2), UtilityWPF.ColorFromHex("80E0E0E0"), 1);
                _debugVisuals.Add(visual);
                _viewport.Children.Add(visual);

                //visual = GetVisual_Triangle(hull2, UtilityWPF.ColorFromHex("80E0E0E0"));
                //_debugVisuals.Add(visual);
                //_viewport.Children.Add(visual);

                for (int cntr = 0; cntr < hull2.Length; cntr++)
                {
                    visual = GetVisual_Triangle(new ITriangle[] { hull2[cntr] }, UtilityWPF.GetColorEGA(192, cntr));
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnIntersectTriangles3a_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearAllVisuals();

                //Triangle[] triangles = GetRandomTrianglesPlate(10, 2);
                Triangle[] triangles = new Triangle[]
                {
                    new Triangle(new Point3D(-0.60, -0.45, -0.01) , new Point3D(-0.60, 0.45, -0.01) , new Point3D(0.60, 0.45, -0.01)),
                    new Triangle(new Point3D(-0.48, -0.36, 0.00) , new Point3D(-0.48, -0.24, -0.12) , new Point3D(-0.48, -0.24, 0.00)),
                };

                Point3D planeIntersectPoint;
                Vector3D planeIntersectDir;
                bool hasPlaneIntersect = Math3D.GetIntersection_Plane_Plane(out planeIntersectPoint, out planeIntersectDir, triangles[0], triangles[1]);

                Tuple<Point3D, Point3D> triangleIntersect = Math3D.GetIntersection_Triangle_Triangle(triangles[0], triangles[1]);

                #region Draw

                Visual3D visual;

                // Plane intersect
                if (hasPlaneIntersect)
                {
                    visual = GetVisual_Line(planeIntersectPoint + (planeIntersectDir * -100), planeIntersectPoint + (planeIntersectDir * 100), Colors.Gray, .1);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                // Triangle intersect
                if (triangleIntersect != null)
                {
                    visual = GetVisual_Line(triangleIntersect.Item1, triangleIntersect.Item2, Colors.HotPink, 3);
                    _debugVisuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                // Triangles
                visual = GetVisual_Triangle(triangles, UtilityWPF.ColorFromHex("80E0E0E0"));
                _debugVisuals.Add(visual);
                _viewport.Children.Add(visual);

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private void ClearAllVisuals()
        {
            ClearDebugVisuals();

            if (_initialVisual != null)
            {
                _viewport.Children.Remove(_initialVisual);
            }
            _initialVisual = null;
            _initialTriangles = null;

            if (_collidedVisual != null)
            {
                _viewport.Children.Remove(_collidedVisual);
            }
            _collidedVisual = null;
            _collidedTriangles = null;
        }
        private void ClearDebugVisuals()
        {
            foreach (Visual3D visual in _debugVisuals)
            {
                _viewport.Children.Remove(visual);
            }

            _debugVisuals.Clear();
        }

        private Polygon2D[] GetPolyUnion(Point[][] polygons)
        {
            if (radUnion1.IsChecked.Value)
            {
                Point[][] result1 = PolyUnion.GetPolyUnion(polygons);
                if (result1 == null)
                {
                    return new Polygon2D[0];
                }
                else
                {
                    return result1.Select(o => new Polygon2D(o)).ToArray();
                }
            }
            else if (radUnion2.IsChecked.Value)
            {
                return PolyUnion2.GetPolyUnion(polygons);
            }
            else if (radUnionFinal.IsChecked.Value)
            {
                //return ClipperHelper.GetPolyUnion(polygons);
                return Math2D.GetUnion_Polygons(polygons);
            }
            else
            {
                throw new ApplicationException("Unknown union algorithm to use");
            }
        }

        private void ShadowMultiStepSprtFinish(Point3D cameraPos, Vector3D cameraLook, TriangleIndexed[] triangles)
        {
            // Order by distance to plane
            ITriangleIndexed[] sorted = Math3D.SortByPlaneDistance(triangles, cameraPos, cameraLook);

            SortedList<int, List<Point3D[]>> clipPolygons = new SortedList<int, List<Point3D[]>>();

            Vector3D hullDepth = cameraLook.ToUnit() * 50d;

            //for (int outer = 0; outer < sorted.Length - 1; outer++)
            for (int outer = 0; outer < sorted.Length; outer++)
            {
                // Create a volume out of the this triangle
                ITriangleIndexed[] hull = GetHullFromTriangle(sorted[outer], hullDepth);

                if (hull != null && hull.Length > 6)        // if the triangle is super thin, then a 2D hull will be created
                {
                    //for (int inner = outer + 1; inner < sorted.Length; inner++)
                    for (int inner = 0; inner < sorted.Length; inner++)
                    {
                        if (outer == inner)
                        {
                            continue;
                        }

                        Point3D[] poly = HullTriangleIntersect2.GetIntersection_Hull_Triangle(hull, sorted[inner]);
                        if (poly != null)
                        {
                            if (!clipPolygons.ContainsKey(inner))
                            {
                                clipPolygons.Add(inner, new List<Point3D[]>());
                            }

                            clipPolygons[inner].Add(poly);
                        }
                    }
                }
            }

            double[] percents = new double[sorted.Length];
            for (int cntr = 0; cntr < sorted.Length; cntr++)
            {
                if (clipPolygons.ContainsKey(cntr))
                {
                    percents[cntr] = GetPercentVisible(new Point3D[] { sorted[cntr].Point0, sorted[cntr].Point1, sorted[cntr].Point2 }, clipPolygons[cntr].ToArray());
                }
                else
                {
                    percents[cntr] = 1d;
                }
            }

            #region Draw

            Visual3D visual;
            Color color;

            // % in shadow
            for (int cntr = 0; cntr < sorted.Length; cntr++)
            {
                color = UtilityWPF.AlphaBlend(Colors.White, Colors.Black, percents[cntr]);

                visual = GetVisual_PolygonLines(new Point3D[] { sorted[cntr].Point0, sorted[cntr].Point1, sorted[cntr].Point2 }, color, 2d);
                _debugVisuals.Add(visual);
                _viewport.Children.Add(visual);
            }

            // Clip polygons
            foreach (Point3D[] polygon in clipPolygons.Values.SelectMany(o => o))
            {
                visual = GetVisual_PolygonHull(polygon, Colors.DodgerBlue, .005);
                _debugVisuals.Add(visual);
                _viewport.Children.Add(visual);
            }

            // Triangles
            color = UtilityWPF.ColorFromHex("50E0E0E0");

            for (int cntr = 0; cntr < sorted.Length; cntr++)
            {
                visual = GetVisual_Triangle(new ITriangle[] { sorted[cntr] }, color);
                _debugVisuals.Add(visual);
                _viewport.Children.Add(visual);
            }


            //// Draw the plane
            //visual = GetTriangleVisual(GetPlane(_camera.Position, _camera.LookDirection, 60), UtilityWPF.ColorFromHex("20B0B0B0"));
            //_debugVisuals.Add(visual);
            //_viewport.Children.Add(visual);

            #endregion



        }

        private PartBase[] GetRandomParts(int count)
        {
            PartBase[] parts = Enumerable.Range(0, 5).Select(o => GetRandomPart()).ToArray();

            bool changed1, changed2;
            Ship.PartSeparator.PullInCrude(out changed1, parts);
            Ship.PartSeparator.Separate(out changed2, parts, _world);

            return parts;
        }
        private PartBase GetRandomPart()
        {
            // This was copied from OverlappingPartsWindow

            Point3D position = Math3D.GetRandomVector(2d).ToPoint();
            Quaternion orientation = Math3D.GetRandomRotation();
            double radius = 1d + StaticRandom.NextDouble() * 4d;
            double height = 1d + StaticRandom.NextDouble() * 4d;

            switch (StaticRandom.Next(8))
            {
                case 0:
                    #region Spin

                    double spinSize = 5d + (StaticRandom.NextDouble() * 8d);
                    PartNeuralDNA dnaSpin = new PartNeuralDNA() { PartType = SensorSpin.PARTTYPE, Position = position, Orientation = orientation, Scale = new Vector3D(spinSize, spinSize, spinSize) };
                    return new SensorSpin(_editorOptions, _itemOptions, dnaSpin, null);

                    #endregion

                case 1:
                    #region Fuel

                    PartDNA dnaFuel = new PartDNA() { PartType = FuelTank.PARTTYPE, Position = position, Orientation = orientation, Scale = new Vector3D(radius, radius, height) };
                    FuelTank fuel = new FuelTank(_editorOptions, _itemOptions, dnaFuel);
                    fuel.QuantityCurrent = fuel.QuantityMax;		// without this, the fuel tank gets tossed around because it's so light
                    return fuel;

                    #endregion

                case 2:
                    #region Energy

                    PartDNA dnaEnergy = new PartDNA() { PartType = EnergyTank.PARTTYPE, Position = position, Orientation = orientation, Scale = new Vector3D(radius, radius, height) };
                    return new EnergyTank(_editorOptions, _itemOptions, dnaEnergy);

                    #endregion

                case 3:
                    #region Brain

                    PartNeuralDNA dnaBrain = new PartNeuralDNA() { PartType = Brain.PARTTYPE, Position = position, Orientation = orientation, Scale = new Vector3D(radius, radius, radius) };
                    return new Brain(_editorOptions, _itemOptions, dnaBrain, null);

                    #endregion

                case 4:
                    #region Thruster

                    ThrusterDNA dnaThruster1 = new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = position, Orientation = orientation, Scale = new Vector3D(height, height, height), ThrusterType = UtilityHelper.GetRandomEnum(ThrusterType.Custom) };
                    return new Thruster(_editorOptions, _itemOptions, dnaThruster1, null);

                    #endregion

                case 5:
                    #region Solar

                    ConverterRadiationToEnergyDNA dnaSolar = new ConverterRadiationToEnergyDNA() { PartType = ConverterRadiationToEnergy.PARTTYPE, Position = position, Orientation = orientation, Scale = new Vector3D(height, 1d + StaticRandom.NextDouble() * 4d, 1d), Shape = UtilityHelper.GetRandomEnum<SolarPanelShape>() };
                    return new ConverterRadiationToEnergy(_editorOptions, _itemOptions, dnaSolar, null, _radiation);

                    #endregion

                case 6:
                    #region Fuel->Energy

                    PartDNA dnaBurner = new PartDNA() { PartType = ConverterFuelToEnergy.PARTTYPE, Position = position, Orientation = orientation, Scale = new Vector3D(radius, radius, height) };
                    return new ConverterFuelToEnergy(_editorOptions, _itemOptions, dnaBurner, null, null);

                    #endregion

                case 7:
                    #region Energy->Ammo

                    PartDNA dnaReplicator = new PartDNA() { PartType = ConverterEnergyToAmmo.PARTTYPE, Position = position, Orientation = orientation, Scale = new Vector3D(radius, radius, height) };
                    return new ConverterEnergyToAmmo(_editorOptions, _itemOptions, dnaReplicator, null, null);

                    #endregion

                default:
                    throw new ApplicationException("Unexpected integer");
            }
        }

        private Tuple<Visual3D, TriangleVisual[]> BreakUpPartVisuals(PartBase[] parts)
        {
            List<Tuple<MeshGeometry3D, Transform3D>> meshes = new List<Tuple<MeshGeometry3D, Transform3D>>();

            // Grab all the meshes out of the parts (doesn't matter which mesh came from which part)
            foreach (PartBase part in parts)
            {
                Transform3DGroup transform = new Transform3DGroup();
                transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(part.Orientation)));
                transform.Children.Add(new TranslateTransform3D(part.Position.ToVector()));

                foreach (MeshGeometry3D mesh in GetMeshesFromModel(part.Model))
                {
                    meshes.Add(Tuple.Create(mesh, (Transform3D)transform));
                }
            }

            // Call the overload
            return BreakUpMeshes(meshes);
        }
        private Tuple<Visual3D, TriangleVisual[]> BreakUpMeshes(IEnumerable<Tuple<MeshGeometry3D, Transform3D>> meshes)
        {
            List<TriangleVisual> triangles = new List<TriangleVisual>();
            Model3DGroup triangleGroup = new Model3DGroup();

            // Break the meshes into individual triangles, and make each triangle a unique model
            foreach (ITriangle triangle in meshes.Select(o => UtilityWPF.GetTrianglesFromMesh(o.Item1, o.Item2)).SelectMany(o => o))
            {
                SolidColorBrush diffuseBrush = new SolidColorBrush(Colors.White);
                SolidColorBrush specularBrush = new SolidColorBrush(Colors.White);

                // Material
                MaterialGroup materials = new MaterialGroup();
                materials.Children.Add(new DiffuseMaterial(diffuseBrush));
                materials.Children.Add(new SpecularMaterial(specularBrush, 30d));

                // Geometry Model
                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = materials;
                geometry.BackMaterial = materials;
                geometry.Geometry = UtilityWPF.GetMeshFromTriangles_IndependentFaces(new ITriangle[] { triangle });

                triangleGroup.Children.Add(geometry);

                // Store the necessary bits
                triangles.Add(new TriangleVisual(triangle, diffuseBrush, specularBrush));
            }

            // Model Visual
            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = triangleGroup;

            return Tuple.Create((Visual3D)visual, triangles.ToArray());
        }

        private static MeshGeometry3D[] GetMeshesFromModel(Model3D model)
        {
            List<MeshGeometry3D> retVal = new List<MeshGeometry3D>();

            if (model is Model3DGroup)
            {
                foreach (Model3D child in ((Model3DGroup)model).Children)
                {
                    retVal.AddRange(GetMeshesFromModel(child));
                }
            }
            else if (model is GeometryModel3D)
            {
                GeometryModel3D modelCast = (GeometryModel3D)model;

                if (modelCast.Geometry is MeshGeometry3D)
                {
                    retVal.Add((MeshGeometry3D)modelCast.Geometry);
                }
            }

            // else unknown, just skip it

            return retVal.ToArray();
        }

        private static Visual3D GetVisual_Polygon_Convex(Point3D[] polygon, Color color)
        {
            // Material
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
            materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(Colors.White, color, .5d)), 30d));

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;
            geometry.Geometry = UtilityWPF.GetMeshFromTriangles_IndependentFaces(Math2D.GetTrianglesFromConvexPoly(polygon));

            ModelVisual3D retVal = new ModelVisual3D();
            retVal.Content = geometry;

            return retVal;
        }
        private static Visual3D GetVisual_PolygonHull(Point3D[] polygon, Color color, double thickness)
        {
            if (polygon.Length < 3)
            {
                throw new ApplicationException("Need at least 3 points for a polygon");
            }

            Vector3D normal = Math2D.GetPolygonNormal(polygon, PolygonNormalLength.Unit) * (thickness * .5d);

            Point3D[] top = polygon.Select(o => o + normal).ToArray();
            Point3D[] bottom = polygon.Select(o => o - normal).ToArray();

            TriangleIndexed[] topTriangles = Math2D.GetTrianglesFromConvexPoly(top);
            TriangleIndexed[] bottomTriangles = Math2D.GetTrianglesFromConvexPoly(bottom);

            //TODO: make triangles out of the sides


            // Material
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
            materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(Colors.White, color, .5d)), 30d));

            Model3DGroup geometries = new Model3DGroup();

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;
            geometry.Geometry = UtilityWPF.GetMeshFromTriangles_IndependentFaces(topTriangles);

            geometries.Children.Add(geometry);

            geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;
            geometry.Geometry = UtilityWPF.GetMeshFromTriangles_IndependentFaces(bottomTriangles);

            geometries.Children.Add(geometry);

            // Visual
            ModelVisual3D retVal = new ModelVisual3D();
            retVal.Content = geometries;

            return retVal;
        }
        private static Visual3D GetVisual_Triangle(ITriangle[] triangles, Color color)
        {
            // Material
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
            materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(Colors.White, color, .5d)), 30d));

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;
            geometry.Geometry = UtilityWPF.GetMeshFromTriangles_IndependentFaces(triangles);

            ModelVisual3D retVal = new ModelVisual3D();
            retVal.Content = geometry;

            return retVal;
        }
        private static Visual3D GetVisual_Dot(Point3D position, double radius, Color color)
        {
            // Material
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
            materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(color, Colors.White, .5d)), 50d));

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;
            geometry.Geometry = UtilityWPF.GetSphere(3, radius, radius, radius);

            // Model Visual
            ModelVisual3D retVal = new ModelVisual3D();
            retVal.Content = geometry;
            retVal.Transform = new TranslateTransform3D(position.ToVector());

            return retVal;
        }
        private static Visual3D GetVisual_HullLines(ITriangleIndexed[] hull, Color color, double thickness)
        {
            Point3D[] allPoints = hull[0].AllPoints;

            ScreenSpaceLines3D retVal = new ScreenSpaceLines3D();
            retVal.Color = color;
            retVal.Thickness = thickness;

            foreach (var line in TriangleIndexed.GetUniqueLines(hull))
            {
                retVal.AddLine(allPoints[line.Item1], allPoints[line.Item2]);
            }

            return retVal;
        }
        private static Visual3D GetVisual_PolygonLines(Point3D[] polygon, Color color, double thickness)
        {
            ScreenSpaceLines3D retVal = new ScreenSpaceLines3D();
            retVal.Color = color;
            retVal.Thickness = thickness;

            for (int cntr = 0; cntr < polygon.Length - 1; cntr++)
            {
                retVal.AddLine(polygon[cntr], polygon[cntr + 1]);
            }

            retVal.AddLine(polygon[polygon.Length - 1], polygon[0]);

            return retVal;
        }
        private static Visual3D GetVisual_Line(Point3D from, Point3D to, Color color, double thickness)
        {
            ScreenSpaceLines3D retVal = new ScreenSpaceLines3D();
            retVal.Color = color;
            retVal.Thickness = thickness;

            retVal.AddLine(from, to);

            return retVal;
        }

        private static Triangle[] GetRandomTrianglesOnion(double minRadius, double maxRadius, int count)
        {
            Triangle[] retVal = new Triangle[count];

            double step = (maxRadius - minRadius) / count;

            for (int cntr = 0; cntr < count; cntr++)
            {
                retVal[cntr] = GetRandomTriangle(cntr * step, (cntr + 1) * step);
            }

            return retVal;
        }
        private static Triangle GetRandomTriangle(double minRadius, double maxRadius)
        {
            return new Triangle(
                Math3D.GetRandomVectorSpherical(minRadius, maxRadius).ToPoint(),
                Math3D.GetRandomVectorSpherical(minRadius, maxRadius).ToPoint(),
                Math3D.GetRandomVectorSpherical(minRadius, maxRadius).ToPoint());
        }

        private static Triangle[] GetRandomTrianglesPlate(double radius, int count)
        {
            List<Point[]> retVal = new List<Point[]>();

            for (int cntr = 0; cntr < count; cntr++)
            {
                while (true)
                {
                    // Make a random triangle
                    Point[] triangle = Enumerable.Range(0, 3).Select(o => Math3D.GetRandomVectorSpherical2D(radius)).Select(o => new Point(o.X, o.Y)).ToArray();

                    // Dupe check
                    bool intersected = false;
                    for (int inner = 0; inner < cntr; inner++)
                    {
                        Point[] intersect = Math2D.GetIntersection_Polygon_Polygon(retVal[inner], triangle);

                        if (intersect != null && intersect.Length > 0)
                        {
                            intersected = true;
                            break;
                        }
                    }

                    // Add if unique
                    if (!intersected)
                    {
                        retVal.Add(triangle);
                        break;
                    }
                }
            }

            // Exit Function
            return retVal.Select(o => new Triangle(o[0].ToPoint3D(), o[1].ToPoint3D(), o[2].ToPoint3D())).ToArray();
        }

        private static ITriangle[] GetPlane(Point3D point, Vector3D normal, double size)
        {
            double halfSize = size / 2d;

            Vector3D orth1 = Math3D.GetArbitraryOrhonganal(normal).ToUnit() * halfSize;
            Vector3D orth2 = Vector3D.CrossProduct(orth1, normal).ToUnit() * halfSize;

            Point3D[] points = new Point3D[4];
            points[0] = point - orth1 - orth2;
            points[1] = point + orth1 - orth2;
            points[2] = point + orth1 + orth2;
            points[3] = point - orth1 + orth2;

            ITriangle[] retVal = new ITriangle[2];
            retVal[0] = new TriangleIndexed(0, 1, 2, points);
            retVal[1] = new TriangleIndexed(2, 3, 0, points);

            return retVal;
        }

        private static ITriangleIndexed[] GetHullFromTriangle(ITriangle triangle, Vector3D direction)
        {
            Point3D[] points = new Point3D[6];

            points[0] = triangle.Point0;
            points[1] = triangle.Point1;
            points[2] = triangle.Point2;
            points[3] = triangle.Point0 + direction;
            points[4] = triangle.Point1 + direction;
            points[5] = triangle.Point2 + direction;


            // This doesn't always include all 6 points when the triangle is really thin
            return Math3D.GetConvexHull(points);
        }
        private static ITriangleIndexed[] GetHullFromPolygon(Point3D[] polygon, Vector3D direction)
        {
            Point3D[] points = new Point3D[polygon.Length * 2];

            for (int cntr = 0; cntr < polygon.Length; cntr++)
            {
                points[cntr] = polygon[cntr];
                points[polygon.Length + cntr] = polygon[cntr] + direction;
            }

            return Math3D.GetConvexHull(points);
        }

        private double GetPercentVisible(Point3D[] polygon, Point3D[][] clipPolygons)
        {
            var transforms = Math2D.GetTransformTo2D(polygon);

            Point[][] clip2D = clipPolygons.Select(o => o.Select(p => transforms.Item1.Transform(p).ToPoint2D()).ToArray()).ToArray();

            // Merge the polygons
            Polygon2D[] unions = null;
            try
            {
                unions = GetPolyUnion(clip2D);
            }
            catch (Exception ex)
            {
                throw;
            }

            // Get the area of the outer polygon
            double outerArea = Math2D.GetAreaPolygon(polygon.Select(o => transforms.Item1.Transform(o)).ToArray());

            // Get the area of the inner polygons
            double innerArea = 0d;
            foreach (Polygon2D inner in unions)
            {
                double innerPoly = Math2D.GetAreaPolygon(inner.Polygon);
                double innerHoles = inner.Holes.Sum(o => Math2D.GetAreaPolygon(o));

                // Add to the total
                innerArea += innerPoly - innerHoles;
            }

            return (outerArea - innerArea) / outerArea;
        }

        #endregion
    }
}
