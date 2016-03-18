using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Media.Media3D;

using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.HelperClassesWPF.Controls2D;
using Game.HelperClassesWPF.Primitives3D;
using System.Text.RegularExpressions;
using Game.Newt.v2.AsteroidMiner.MapParts;
using Game.Newt.v2.GameItems.MapParts;
using System.Windows.Threading;

namespace Game.Newt.Testers
{
    public partial class PotatoWindow : Window
    {
        #region Class: TrianglesInPlane

        private class TrianglesInPlane
        {
            #region Constructor

            public TrianglesInPlane()
            {
                this.Triangles = new List<TriangleIndexedLinked>();
                this.TriangleIndices = new List<int>();
            }

            #endregion

            #region Public Properties

            public List<TriangleIndexedLinked> Triangles
            {
                get;
                private set;
            }
            public List<int> TriangleIndices
            {
                get;
                private set;
            }

            public Vector3D NormalUnit
            {
                get;
                private set;
            }

            #endregion

            #region Public Methods

            public void AddTriangle(int index, TriangleIndexedLinked triangle)
            {
                if (this.Triangles.Count == 0)
                {
                    this.NormalUnit = triangle.NormalUnit;
                }

                this.Triangles.Add(triangle);
                this.TriangleIndices.Add(index);
            }

            public bool ShouldAdd(TriangleIndexedLinked triangle)
            {
                // This needs to share an edge with at least one of the triangles (groups of triangles can share the same plane, but not
                // be neighbors - not for convex hulls, but this class is more generic than convex)
                if (!DoesShareEdge(triangle))
                {
                    return false;
                }

                // Compare the normals
                double dot = Vector3D.DotProduct(this.NormalUnit, triangle.NormalUnit);

                double dotCloseness = Math.Abs(1d - dot);

                if (dotCloseness < .01)
                {
                    return true;
                }

                return false;
            }

            #endregion

            #region Private Methods

            private bool DoesShareEdge(TriangleIndexedLinked triangle)
            {
                foreach (TriangleIndexedLinked existing in this.Triangles)
                {
                    if (existing.Neighbor_01 != null && existing.Neighbor_01.Token == triangle.Token)
                    {
                        return true;
                    }
                    else if (existing.Neighbor_12 != null && existing.Neighbor_12.Token == triangle.Token)
                    {
                        return true;
                    }
                    else if (existing.Neighbor_20 != null && existing.Neighbor_20.Token == triangle.Token)
                    {
                        return true;
                    }
                }

                return false;
            }

            #endregion
        }

        #endregion
        #region Class: ItemColors

        private class ItemColors
        {
            public Color DarkGray = Color.FromRgb(31, 31, 30);		//#1F1F1E
            public Color MedGray = Color.FromRgb(85, 84, 85);		//#555455
            public Color LightGray = Color.FromRgb(149, 153, 147);		//#959993
            public Color LightLightGray = Color.FromRgb(184, 189, 181);		//#B8BDB5
            public Color MedSlate = Color.FromRgb(78, 85, 77);		//#4E554D
            public Color DarkSlate = Color.FromRgb(43, 52, 52);		//#2B3434

            public Color AxisZ = Color.FromRgb(106, 161, 98);

            public Color LightLightSlate = UtilityWPF.ColorFromHex("CFE1CE");

            public Color Normals = UtilityWPF.ColorFromHex("677066");

            public Color HullFaceLight = UtilityWPF.AlphaBlend(Colors.Ivory, Colors.Transparent, .2d);
            public Color HullFace = UtilityWPF.AlphaBlend(Colors.Ivory, Colors.Transparent, .9d);
            //public Color HullFace = Colors.Ivory;
            private SpecularMaterial _hullFaceSpecular = null;
            public SpecularMaterial HullFaceSpecular
            {
                get
                {
                    if (_hullFaceSpecular == null)
                    {
                        _hullFaceSpecular = new SpecularMaterial(new SolidColorBrush(this.MedSlate), 100d);
                    }

                    return _hullFaceSpecular;
                }
            }
            private SpecularMaterial _hullFaceSpecularSoft = null;
            public SpecularMaterial HullFaceSpecularSoft
            {
                get
                {
                    if (_hullFaceSpecularSoft == null)
                    {
                        _hullFaceSpecularSoft = new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(this.MedSlate, Colors.Transparent, .01d)), 5d);
                        //_hullFaceSpecularSoft = new SpecularMaterial(Brushes.Transparent, 0d);
                    }

                    return _hullFaceSpecularSoft;
                }
            }

            public Color HullFaceRemoved = Color.FromArgb(128, 255, 0, 0);
            //public Color HullFaceRemoved = Color.FromRgb(255, 0, 0);
            private SpecularMaterial _hullFaceSpecularRemoved = null;
            public SpecularMaterial HullFaceSpecularRemoved
            {
                get
                {
                    if (_hullFaceSpecularRemoved == null)
                    {
                        _hullFaceSpecularRemoved = new SpecularMaterial(new SolidColorBrush(Colors.Red), 100d);
                    }

                    return _hullFaceSpecularRemoved;
                }
            }

            public Color HullFaceOtherRemoved = Color.FromArgb(40, 255, 0, 0);
            //public Color HullFaceOtherRemoved = Color.FromRgb(255, 192, 192);
            private SpecularMaterial _hullFaceSpecularOtherRemoved = null;
            public SpecularMaterial HullFaceSpecularOtherRemoved
            {
                get
                {
                    if (_hullFaceSpecularOtherRemoved == null)
                    {
                        _hullFaceSpecularOtherRemoved = new SpecularMaterial(new SolidColorBrush(Color.FromArgb(64, 255, 0, 0)), 100d);
                    }

                    return _hullFaceSpecularOtherRemoved;
                }
            }

            public Color Spike = UtilityWPF.ColorFromHex("E3E0D5");
            private SpecularMaterial _spikeSpecular = null;
            public SpecularMaterial SpikeSpecular
            {
                get
                {
                    if (_spikeSpecular == null)
                    {
                        _spikeSpecular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("80C2BFB6")), 10d);
                    }

                    return _spikeSpecular;
                }
            }

            public Color SpikeBall = UtilityWPF.ColorFromHex("615E54");
            private SpecularMaterial _spikeBallSpecular = null;
            public SpecularMaterial SpikeBallSpecular
            {
                get
                {
                    if (_spikeBallSpecular == null)
                    {
                        _spikeBallSpecular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("40C2BFB6")), 3d);
                    }

                    return _spikeBallSpecular;
                }
            }

            //public readonly Brush LabelFill = new SolidColorBrush(UtilityWPF.ColorFromHex("FFFFFF"));
            //public readonly Brush LabelStroke = new SolidColorBrush(UtilityWPF.ColorFromHex("A0000000"));
            //public readonly Brush LabelFill = new SolidColorBrush(UtilityWPF.ColorFromHex("00FF4D"));
            public readonly Brush LabelFill = new SolidColorBrush(UtilityWPF.ColorFromHex("A0FF2E"));
            public readonly Brush LabelStroke = new SolidColorBrush(UtilityWPF.ColorFromHex("A0000000"));
        }

        #endregion
        #region Class: AsteroidShatter

        private class AsteroidShatter
        {
            public AsteroidShatter(ITriangleIndexed[] hull)
            {
                this.Hull = hull;

                if (hull == null)
                {
                    this.HullPoints = null;
                }
                else if (hull.Length == 0)
                {
                    this.HullPoints = new Point3D[0];
                }
                else
                {
                    this.HullPoints = hull[0].AllPoints;
                }
            }

            public readonly ITriangleIndexed[] Hull;
            public readonly Point3D[] HullPoints;

            /// <summary>
            /// This is optional, it emulates collisions that will be considered when splitting the asteroid
            /// </summary>
            public readonly List<Tuple<Point3D, Vector3D>> Collisions = new List<Tuple<Point3D, Vector3D>>();

            /// <summary>
            /// When a decision to make a voronoi occurs, this is the voronoi
            /// </summary>
            public VoronoiResult3D Voronoi = null;
            /// <summary>
            /// When a voronoi gets created, this is the result of clipping the parent asteroid with the voronoi
            /// NOTE: Each control point will have a corresponding shard in this list (the shard may be zero length if the control point is outside the asteroid
            /// </summary>
            public ITriangleIndexed[][] ShatteredHulls = null;
        }

        #endregion
        #region Class: SmoothHullDebug

        private static class SmoothHullDebug
        {
            public static ITriangleIndexed[] SmoothSlice(out long[] specialTokens, ITriangleIndexed[] triangles, double maxEdgeLength, int maxPasses = 3)
            {
                List<long> specialTokensList = new List<long>();

                if (maxPasses < 1)
                {
                    specialTokens = specialTokensList.ToArray();
                    return triangles;
                }

                List<ITriangle> sliced = new List<ITriangle>();

                bool foundLarger = false;

                SortedList<int, ITriangle> planes = new SortedList<int, ITriangle>();
                SortedList<Tuple<int, int>, BezierSegment3D> edgeBeziers = new SortedList<Tuple<int, int>, BezierSegment3D>();

                for (int cntr = 0; cntr < triangles.Length; cntr++)
                {
                    // Get the length of the longest edge
                    Tuple<TriangleEdge, double>[] edgeLengths = Triangle.Edges.
                        Select(o => Tuple.Create(o, triangles[cntr].GetEdgeLength(o))).
                        OrderBy(o => o.Item2).
                        ToArray();

                    double maxLength = edgeLengths.Max(o => o.Item2);

                    if (maxLength > maxEdgeLength)
                    {
                        // This is too big.  Cut it up
                        sliced.AddRange(Divide(triangles[cntr], edgeLengths, triangles, planes, edgeBeziers));
                        foundLarger = true;
                    }
                    else
                    {
                        // Keep as is
                        sliced.Add(triangles[cntr]);
                    }
                }

                if (!foundLarger)
                {
                    specialTokens = specialTokensList.ToArray();
                    return triangles;
                }

                foreach (ITriangleIndexed triangle in sliced.ToArray())
                {
                    specialTokensList.AddRange(DivideNeighbor(triangle, sliced, edgeBeziers));
                }





                specialTokens = specialTokensList.ToArray();
                return sliced.Select(o => (ITriangleIndexed)o).ToArray();





                // Convert to indexed
                TriangleIndexed[] slicedIndexed = TriangleIndexed.ConvertToIndexed(sliced.ToArray());

                if (maxPasses > 1)
                {
                    // Recurse
                    long[] dummy;
                    return SmoothSlice(out dummy, slicedIndexed, maxEdgeLength, maxPasses - 1);
                }
                else
                {
                    return slicedIndexed;
                }
            }

            #region Private Methods

            private static ITriangle[] Divide(ITriangleIndexed triangle, Tuple<TriangleEdge, double>[] edgeLengths, ITriangleIndexed[] triangles, SortedList<int, ITriangle> planes, SortedList<Tuple<int, int>, BezierSegment3D> edgeBeziers)
            {
                // Define beziers for the three edges
                var curves = Triangle.Edges.
                    Select(o => Tuple.Create(o, GetCurvedEdge(triangle, o, triangles, planes, edgeBeziers))).
                    ToArray();

                // Take the shortest length divided by the longest (the array is sorted)
                double ratio = edgeLengths[0].Item2 / edgeLengths[2].Item2;

                TriangleIndexed[] retVal = null;

                if (ratio > .75d)
                {
                    // The 3 egde lengths are roughly equal.  Divide into 4
                    retVal = Divide_4(triangle, curves);
                }
                else if (ratio < .33d)
                {
                    // Skinny base, and two large sides
                    retVal = Divide_SkinnyBase(triangle, edgeLengths, curves);
                }
                else
                {
                    // Wide base, and two smaller sides
                    retVal = Divide_WideBase(triangle, edgeLengths, curves);
                }

                // Make sure the normals point in the same direction
                for (int cntr = 0; cntr < retVal.Length; cntr++)
                {
                    if (Vector3D.DotProduct(retVal[cntr].Normal, triangle.Normal) < 0)
                    {
                        // Reverse it
                        retVal[cntr] = new TriangleIndexed(retVal[cntr].Index0, retVal[cntr].Index2, retVal[cntr].Index1, retVal[cntr].AllPoints);
                    }
                }

                return retVal;
            }

            private static TriangleIndexed[] Divide_4(ITriangleIndexed triangle, Tuple<TriangleEdge, BezierSegment3D>[] curves)
            {
                List<Point3D> points = new List<Point3D>();
                List<int> map = new List<int>();

                //NOTE: Reusing existing points, then just adding new.  The constructed hull will have patches of triangles with
                //differing point arrays, but there is a pass at the end that rebuilds the hull with a single unique point array
                points.AddRange(triangle.AllPoints);

                map.Add(triangle.Index0);      // 0
                map.Add(triangle.Index1);      // 1
                map.Add(triangle.Index2);      // 2

                points.Add(BezierUtil.GetPoint(.5, curves.First(o => o.Item1 == TriangleEdge.Edge_01).Item2));       // 3
                map.Add(points.Count - 1);

                points.Add(BezierUtil.GetPoint(.5, curves.First(o => o.Item1 == TriangleEdge.Edge_12).Item2));       // 4
                map.Add(points.Count - 1);

                points.Add(BezierUtil.GetPoint(.5, curves.First(o => o.Item1 == TriangleEdge.Edge_20).Item2));       // 5
                map.Add(points.Count - 1);

                Point3D[] pointArr = points.ToArray();

                List<TriangleIndexed> retVal = new List<TriangleIndexed>();

                retVal.Add(MatchNormal(new TriangleIndexed(map[0], map[3], map[5], pointArr), triangle));       // Bottom Left
                retVal.Add(MatchNormal(new TriangleIndexed(map[1], map[4], map[3], pointArr), triangle));       // Top
                retVal.Add(MatchNormal(new TriangleIndexed(map[2], map[5], map[4], pointArr), triangle));       // Bottom Right
                retVal.Add(MatchNormal(new TriangleIndexed(map[3], map[4], map[5], pointArr), triangle));       // Center

                return retVal.ToArray();
            }
            private static TriangleIndexed[] Divide_SkinnyBase(ITriangleIndexed triangle, Tuple<TriangleEdge, double>[] edgeLengths, Tuple<TriangleEdge, BezierSegment3D>[] curves)
            {
                List<Point3D> points = new List<Point3D>();
                List<int> map = new List<int>();

                points.AddRange(triangle.AllPoints);

                map.Add(triangle.GetCommonIndex(edgeLengths[0].Item1, edgeLengths[1].Item1));        // Bottom Left (0)
                map.Add(triangle.GetCommonIndex(edgeLengths[1].Item1, edgeLengths[2].Item1));        // Top (1)
                map.Add(triangle.GetCommonIndex(edgeLengths[0].Item1, edgeLengths[2].Item1));        // Bottom Right (2)

                points.Add(BezierUtil.GetPoint(.5, curves.First(o => o.Item1 == edgeLengths[1].Item1).Item2));        // Mid Left (3)
                map.Add(points.Count - 1);

                points.Add(BezierUtil.GetPoint(.5, curves.First(o => o.Item1 == edgeLengths[2].Item1).Item2));        // Mid Right (4)
                map.Add(points.Count - 1);

                Point3D[] pointArr = points.ToArray();

                List<TriangleIndexed> retVal = new List<TriangleIndexed>();

                retVal.Add(MatchNormal(new TriangleIndexed(map[1], map[4], map[3], pointArr), triangle));       // Top
                retVal.Add(MatchNormal(new TriangleIndexed(map[0], map[4], map[2], pointArr), triangle));       // Bottom Right
                retVal.Add(MatchNormal(new TriangleIndexed(map[0], map[3], map[4], pointArr), triangle));       // Middle Left

                return retVal.ToArray();
            }
            private static TriangleIndexed[] Divide_WideBase(ITriangleIndexed triangle, Tuple<TriangleEdge, double>[] edgeLengths, Tuple<TriangleEdge, BezierSegment3D>[] curves)
            {
                List<Point3D> points = new List<Point3D>();
                List<int> map = new List<int>();

                points.AddRange(triangle.AllPoints);

                map.Add(triangle.GetCommonIndex(edgeLengths[2].Item1, edgeLengths[0].Item1));        // Bottom Left (0)
                map.Add(triangle.GetCommonIndex(edgeLengths[0].Item1, edgeLengths[1].Item1));        // Top (1)
                map.Add(triangle.GetCommonIndex(edgeLengths[2].Item1, edgeLengths[1].Item1));        // Bottom Right (2)

                points.Add(BezierUtil.GetPoint(.5, curves.First(o => o.Item1 == edgeLengths[2].Item1).Item2));        // Mid Bottom (3)
                map.Add(points.Count - 1);

                Point3D[] pointArr = points.ToArray();

                List<TriangleIndexed> retVal = new List<TriangleIndexed>();

                retVal.Add(MatchNormal(new TriangleIndexed(map[0], map[1], map[3], pointArr), triangle));       // Left
                retVal.Add(MatchNormal(new TriangleIndexed(map[1], map[2], map[3], pointArr), triangle));       // Right

                return retVal.ToArray();
            }

            private static long[] DivideNeighbor(ITriangleIndexed triangle, List<ITriangle> returnList, SortedList<Tuple<int, int>, BezierSegment3D> edgeBeziers)
            {
                // Find beziers for the edges of this triangle (if a bezier was created, then that edge was sliced in half)
                var bisectedEdges = Triangle.Edges.
                    Select(o => new { Edge = o, From = triangle.GetIndex(o, true), To = triangle.GetIndex(o, false) }).
                    Select(o => new { Edge = o.Edge, Key = Tuple.Create(Math.Min(o.From, o.To), Math.Max(o.From, o.To)) }).
                    Select(o => new { Edge = o.Edge, Key = o.Key, Match = edgeBeziers.TryGetValue(o.Key) }).
                    Where(o => o.Match.Item1).
                    Select(o => Tuple.Create(o.Edge, o.Key, o.Match.Item2)).
                    ToArray();

                TriangleIndexed[] replacement;
                long[] retVal = new long[0];

                switch (bisectedEdges.Length)
                {
                    case 0:
                        return retVal;

                    case 1:
                        replacement = DivideNeighbor_1(triangle, bisectedEdges[0]);
                        break;

                    case 2:
                        replacement = DivideNeighbor_2(triangle, bisectedEdges);
                        break;

                    case 3:
                        replacement = DivideNeighbor_3(triangle, bisectedEdges);
                        retVal = replacement.Select(o => o.Token).ToArray();
                        break;

                    default:
                        throw new ApplicationException("Unexpected number of matches: " + bisectedEdges.Length.ToString());
                }

                // Swap them
                returnList.Remove(triangle);
                returnList.AddRange(replacement);

                return retVal;
            }

            private static TriangleIndexed[] DivideNeighbor_1(ITriangleIndexed triangle, Tuple<TriangleEdge, Tuple<int, int>, BezierSegment3D> bisectedEdge)
            {
                List<Point3D> points = new List<Point3D>();
                List<int> map = new List<int>();

                points.AddRange(triangle.AllPoints);

                map.Add(triangle.GetIndex(bisectedEdge.Item1, true));        // Bottom Left (0)
                map.Add(triangle.GetOppositeIndex(bisectedEdge.Item1));        // Top (1)
                map.Add(triangle.GetIndex(bisectedEdge.Item1, false));        // Bottom Right (2)

                points.Add(BezierUtil.GetPoint(.5, bisectedEdge.Item3));        // Mid Bottom (3)
                map.Add(points.Count - 1);

                Point3D[] pointArr = points.ToArray();

                List<TriangleIndexed> retVal = new List<TriangleIndexed>();

                retVal.Add(MatchNormal(new TriangleIndexed(map[0], map[1], map[3], pointArr), triangle));       // Left
                retVal.Add(MatchNormal(new TriangleIndexed(map[1], map[2], map[3], pointArr), triangle));       // Right

                return retVal.ToArray();
            }
            private static TriangleIndexed[] DivideNeighbor_2(ITriangleIndexed triangle, Tuple<TriangleEdge, Tuple<int, int>, BezierSegment3D>[] bisectedEdges)
            {
                List<Point3D> points = new List<Point3D>();
                List<int> map = new List<int>();

                points.AddRange(triangle.AllPoints);

                map.Add(triangle.GetUncommonIndex(bisectedEdges[0].Item1, bisectedEdges[1].Item1));        // Bottom Left (0)
                map.Add(triangle.GetCommonIndex(bisectedEdges[0].Item1, bisectedEdges[1].Item1));        // Top (1)
                map.Add(triangle.GetUncommonIndex(bisectedEdges[1].Item1, bisectedEdges[0].Item1));        // Bottom Right (2)

                points.Add(BezierUtil.GetPoint(.5, bisectedEdges[0].Item3));        // Mid Left (3)
                map.Add(points.Count - 1);

                points.Add(BezierUtil.GetPoint(.5, bisectedEdges[1].Item3));        // Mid Right (4)
                map.Add(points.Count - 1);

                Point3D[] pointArr = points.ToArray();

                List<TriangleIndexed> retVal = new List<TriangleIndexed>();

                retVal.Add(MatchNormal(new TriangleIndexed(map[1], map[4], map[3], pointArr), triangle));       // Top
                retVal.Add(MatchNormal(new TriangleIndexed(map[0], map[4], map[2], pointArr), triangle));       // Bottom Right
                retVal.Add(MatchNormal(new TriangleIndexed(map[0], map[3], map[4], pointArr), triangle));       // Middle Left

                return retVal.ToArray();
            }
            private static TriangleIndexed[] DivideNeighbor_3(ITriangleIndexed triangle, Tuple<TriangleEdge, Tuple<int, int>, BezierSegment3D>[] bisectedEdges)
            {
                List<Point3D> points = new List<Point3D>();
                List<int> map = new List<int>();

                points.AddRange(triangle.AllPoints);

                map.Add(triangle.Index0);      // 0
                map.Add(triangle.Index1);      // 1
                map.Add(triangle.Index2);      // 2

                points.Add(BezierUtil.GetPoint(.5, bisectedEdges.First(o => o.Item1 == TriangleEdge.Edge_01).Item3));       // 3
                map.Add(points.Count - 1);

                points.Add(BezierUtil.GetPoint(.5, bisectedEdges.First(o => o.Item1 == TriangleEdge.Edge_12).Item3));       // 4
                map.Add(points.Count - 1);

                points.Add(BezierUtil.GetPoint(.5, bisectedEdges.First(o => o.Item1 == TriangleEdge.Edge_20).Item3));       // 5
                map.Add(points.Count - 1);

                Point3D[] pointArr = points.ToArray();

                List<TriangleIndexed> retVal = new List<TriangleIndexed>();

                retVal.Add(MatchNormal(new TriangleIndexed(map[0], map[3], map[5], pointArr), triangle));       // Bottom Left
                retVal.Add(MatchNormal(new TriangleIndexed(map[1], map[4], map[3], pointArr), triangle));       // Top
                retVal.Add(MatchNormal(new TriangleIndexed(map[2], map[5], map[4], pointArr), triangle));       // Bottom Right
                retVal.Add(MatchNormal(new TriangleIndexed(map[3], map[4], map[5], pointArr), triangle));       // Center

                return retVal.ToArray();
            }

            private static TriangleIndexed MatchNormal(TriangleIndexed newTriangle, ITriangle compareTo)
            {
                if (Vector3D.DotProduct(newTriangle.Normal, compareTo.Normal) > 0)
                {
                    return newTriangle;
                }
                else
                {
                    return new TriangleIndexed(newTriangle.Index0, newTriangle.Index2, newTriangle.Index1, newTriangle.AllPoints);
                }
            }

            private static BezierSegment3D GetCurvedEdge(ITriangleIndexed triangle, TriangleEdge edge, ITriangleIndexed[] triangles, SortedList<int, ITriangle> planes, SortedList<Tuple<int, int>, BezierSegment3D> edges, double controlPercent = .25d)
            {
                int fromIndex = triangle.GetIndex(edge, true);
                int toIndex = triangle.GetIndex(edge, false);

                var key = Tuple.Create(Math.Min(fromIndex, toIndex), Math.Max(fromIndex, toIndex));

                BezierSegment3D retVal;
                if (edges.TryGetValue(key, out retVal))
                {
                    return retVal;
                }

                retVal = GetCurvedEdge(triangle, edge, triangles, planes, controlPercent);

                edges.Add(key, retVal);

                return retVal;
            }
            /// <summary>
            /// Each point on the hull will get its own tangent plane.  This will return a bezier with control points
            /// snapped to the coresponding planes.  That way, any beziers that meet at a point will have a smooth
            /// transition
            /// </summary>
            private static BezierSegment3D GetCurvedEdge(ITriangleIndexed triangle, TriangleEdge edge, ITriangleIndexed[] triangles, SortedList<int, ITriangle> planes, double controlPercent = .25d)
            {
                // Points
                int fromIndex = triangle.GetIndex(edge, true);
                int toIndex = triangle.GetIndex(edge, false);

                Point3D fromPoint = triangle.GetPoint(edge, true);
                Point3D toPoint = triangle.GetPoint(edge, false);

                // Snap planes
                ITriangle fromPlane = GetTangentPlane(fromIndex, triangles, planes);
                ITriangle toPlane = GetTangentPlane(toIndex, triangles, planes);

                // Edge line segment
                Vector3D dir = toPoint - fromPoint;

                // Control points
                Point3D fromControl = Math3D.GetClosestPoint_Plane_Point(fromPlane, fromPoint + (dir * controlPercent));
                Point3D toControl = Math3D.GetClosestPoint_Plane_Point(toPlane, toPoint + (-dir * controlPercent));

                return new BezierSegment3D(fromIndex, toIndex, new[] { fromControl, toControl }, triangle.AllPoints);
            }

            private static ITriangle GetTangentPlane(int index, ITriangleIndexed[] triangles, SortedList<int, ITriangle> planes)
            {
                ITriangle retVal;
                if (planes.TryGetValue(index, out retVal))
                {
                    return retVal;
                }

                retVal = GetTangentPlane(index, triangles);

                planes.Add(index, retVal);

                return retVal;
            }
            private static ITriangle GetTangentPlane(int index, ITriangleIndexed[] triangles)
            {
                // Find the triangles that touch this point
                ITriangleIndexed[] touching = triangles.
                    Where(o => o.IndexArray.Contains(index)).
                    ToArray();

                // Get all the points in those triangles that aren't the point passed in
                Point3D[] otherPoints = touching.
                    SelectMany(o => o.IndexArray).
                    Where(o => o != index).
                    Select(o => triangles[0].AllPoints[o]).
                    ToArray();

                // Use those points to define a plane
                ITriangle plane = Math2D.GetPlane_Average(otherPoints);

                // Translate the plane
                Point3D requestPoint = triangles[0].AllPoints[index];
                return new Triangle(requestPoint, requestPoint + (plane.Point1 - plane.Point0), requestPoint + (plane.Point2 - plane.Point0));
            }

            #endregion
        }

        #endregion
        #region Class: HullVoronoiIntersect

        private static class HullVoronoiIntersect
        {
            #region Class: TriangleIntersection 1,2,3

            private class TriInt_I
            {
                public TriInt_I(ITriangleIndexed triangle, int triangleIndex)
                {
                    this.Triangle = triangle;
                    this.TriangleIndex = triangleIndex;
                }

                public readonly ITriangleIndexed Triangle;
                public readonly int TriangleIndex;

                public readonly List<TriInt_II> ControlPoints = new List<TriInt_II>();
            }

            private class TriInt_II
            {
                public TriInt_II(int controlPointIndex)
                {
                    this.ControlPointIndex = controlPointIndex;
                }

                public readonly int ControlPointIndex;
                public readonly List<TriInt_III> Faces = new List<TriInt_III>();
            }

            private class TriInt_III
            {
                public TriInt_III(Face3D face, int faceIndex, Tuple<Point3D, Point3D> segment, int[] triangleIndices)
                {
                    this.Face = face;
                    this.FaceIndex = faceIndex;
                    this.Segment = segment;
                    this.TriangleIndices = triangleIndices;
                }

                // The face that the triangle intersected
                public readonly Face3D Face;
                public readonly int FaceIndex;

                // The points of intersection
                public readonly Tuple<Point3D, Point3D> Segment;

                // The indicies of the triangle that are part of the parent control point
                public readonly int[] TriangleIndices;
            }

            #endregion
            #region Class: PolyFragIntermediate

            private class PolyFragIntermediate
            {
                public PolyFragIntermediate(int controlPointIndex, int originalTriangleIndex, Tuple<TriangleEdge, int>[] faceIndices, int triangleIndex0, int triangleIndex1, int triangleIndex2)
                {
                    this.ControlPointIndex = controlPointIndex;
                    this.OriginalTriangleIndex = originalTriangleIndex;

                    this.FaceIndices = faceIndices;

                    this.TriangleIndex0 = triangleIndex0;
                    this.TriangleIndex1 = triangleIndex1;
                    this.TriangleIndex2 = triangleIndex2;
                }

                public readonly int ControlPointIndex;
                public readonly int OriginalTriangleIndex;

                public readonly Tuple<TriangleEdge, int>[] FaceIndices;

                public readonly int TriangleIndex0;
                public readonly int TriangleIndex1;
                public readonly int TriangleIndex2;
            }

            #endregion

            #region Class: FaceTriangulation

            public class FaceTriangulation
            {
                public FaceTriangulation(Face3D face, int faceIndex, FaceTriangulation2_Poly[] polys)
                {
                    this.Face = face;
                    this.FaceIndex = faceIndex;
                    this.Polys = polys;
                }

                public readonly Face3D Face;
                public readonly int FaceIndex;

                public readonly FaceTriangulation2_Poly[] Polys;
            }

            public class FaceTriangulation2_Poly
            {
                public FaceTriangulation2_Poly(Point3D[] points, ITriangleIndexed[] triangles, Tuple<Edge3D, int>[] faceEdgePoints)
                {
                    this.Points = points;
                    this.Triangles = triangles;
                    this.FaceEdgePoints = faceEdgePoints;
                }

                public readonly Point3D[] Points;
                public readonly ITriangleIndexed[] Triangles;

                public readonly Tuple<Edge3D, int>[] FaceEdgePoints;
            }

            #endregion

            #region Class: TestFaceResult

            public class TestFaceResult
            {
                public TestFaceResult(Face3D face, int faceIndex, ITriangleIndexed[] poly1, ITriangleIndexed[] poly2)
                {
                    this.Face = face;
                    this.FaceIndex = faceIndex;
                    this.Poly1 = poly1;
                    this.Poly2 = poly2;
                }

                public readonly Face3D Face;
                public readonly int FaceIndex;

                public readonly ITriangleIndexed[] Poly1;
                public readonly ITriangleIndexed[] Poly2;      // poly2 is alt, could be null.  The final result class shouldn't have an alt
            }

            #endregion

            #region Class: Fragment Results

            public class PatchFragment
            {
                public PatchFragment(TriangleFragment[] polygon, int controlPointIndex)
                {
                    this.Polygon = polygon;
                    this.ControlPointIndex = controlPointIndex;
                }

                public readonly TriangleFragment[] Polygon;

                public readonly int ControlPointIndex;
            }

            public class TriangleFragment
            {
                public TriangleFragment(ITriangleIndexed triangle, Tuple<TriangleEdge, int>[] faceIndices, int originalTriangleIndex)
                {
                    this.Triangle = triangle;
                    this.FaceIndices = faceIndices;
                    this.OriginalTriangleIndex = originalTriangleIndex;
                }

                public readonly ITriangleIndexed Triangle;
                public readonly Tuple<TriangleEdge, int>[] FaceIndices;
                public readonly int OriginalTriangleIndex;
            }

            #endregion

            #region OLD

            /// <summary>
            /// This divides the hull into a set of cells.  It includes the voronoi walls and interior cells, but not outside the hull
            /// </summary>
            /// <returns>
            /// Item1=Index of control point
            /// Item2=Cell for that control point
            /// </returns>
            //public static Tuple<int, ITriangleIndexed[]>[] SliceHull(ITriangleIndexed[] hull, VoronoiResult3D voronoi)
            //{
            //    // Intersect the voronoi and hull
            //    PatchFragment[] hullIntersects = GetFragments(hull, voronoi);

            //    Point3D hullCenter = Math3D.GetCenter(hull.SelectMany(o => new[] { o.Point0, o.Point1, o.Point2 }));

            //    // Convert each intersect patch into a sub hull
            //    var intersectSubs = hullIntersects.
            //        Select(o => Tuple.Create(o.ControlPointIndex, GetSubHull(o, hull, voronoi, hullCenter))).
            //        ToArray();


            //    //TODO: Detect any control points that are completely internal and return them as well



            //    return intersectSubs;
            //}

            #endregion

            /// <summary>
            /// This will return the triangles sliced by voronoi regions
            /// </summary>
            /// <param name="triangles">A list of triangles.  Doesn't need to be a closed hull</param>
            /// <returns>
            /// Item1=Index of control point
            /// Item2=Set of triangles inside that control point's region
            /// </returns>
            public static PatchFragment[] GetFragments(ITriangleIndexed[] triangles, VoronoiResult3D voronoi)
            {
                if (triangles == null || triangles.Length == 0)
                {
                    return new PatchFragment[0];
                }

                Point3D[] allPoints = triangles[0].AllPoints;

                // Figure out which control point owns each triangle point
                int[] controlPointsByTrianglePoint = GetNearestControlPoints(allPoints, voronoi);

                // Divide the triangles by control point
                return GetTrianglesByControlPoint(triangles, voronoi, controlPointsByTrianglePoint);
            }

            #region face polys 1

            public static TestFaceResult[] GetTestFacePolys(PatchFragment patch, ITriangleIndexed[] hull, VoronoiResult3D voronoi)
            {
                List<TestFaceResult> retVal = new List<TestFaceResult>();

                int[] faces = voronoi.FacesByControlPoint[patch.ControlPointIndex];

                var workLater = new List<Tuple<int, Point3D[], Tuple<TriangleFragment, Tuple<TriangleEdge, int>[]>[]>>();

                foreach (int faceIndex in faces)
                {
                    var intersects = patch.Polygon.
                        Select(o => new
                        {
                            Triangle = o,
                            FaceHits = o.FaceIndices.Where(p => p.Item2 == faceIndex).ToArray()
                        }).
                        Where(o => o.FaceHits.Length > 0).
                        ToArray();

                    Point3D[] points = intersects.
                        SelectMany(o => o.FaceHits.SelectMany(p => new[] { o.Triangle.Triangle.GetPoint(p.Item1, true), o.Triangle.Triangle.GetPoint(p.Item1, false) })).
                        ToArray();

                    if (points.Length == 0)
                    {
                        continue;
                    }



                    // Detect which of the points are on the face's edge, and extend along that edge
                    var faceEdgeHits = voronoi.Faces[faceIndex].Edges.
                        Select(o => new
                        {
                            Edge = o,
                            Points = points.Where(p => Math1D.IsNearZero(Math3D.GetClosestDistance_Line_Point(o.Point0, o.DirectionExt, p))).ToArray()        // Even though the edge is finite, there should never be a case where a face hit is off the edge segment.  So save some processing and just assume it's a line
                        }).
                        Where(o => o.Points.Length > 0).
                        ToArray();




                    if (points.Length < 3)
                    {
                        workLater.Add(Tuple.Create(faceIndex, points, intersects.Select(o => Tuple.Create(o.Triangle, o.FaceHits)).ToArray()));
                        continue;
                    }




                    // Triangulate these points
                    //TODO: Can't assume that the face intersects are convex
                    var hull2D = Math2D.GetConvexHull(points);
                    TriangleIndexed[] triangles = Math2D.GetTrianglesFromConvexPoly(hull2D.PerimiterLines.Select(o => points[o]).ToArray());


                    retVal.Add(new TestFaceResult(voronoi.Faces[faceIndex], faceIndex, triangles, null));
                }



                //TODO: Dedupe points across sets of triangles



                return retVal.ToArray();
            }

            #endregion
            #region face polys 2

            public static FaceTriangulation[] GetTestFacePolys2(PatchFragment patch, ITriangleIndexed[] hull, VoronoiResult3D voronoi)
            {
                FaceTriangulation[] faceTriangles = voronoi.FacesByControlPoint[patch.ControlPointIndex].
                    Select(o => GetFaceTriangles(o, patch, voronoi)).
                    Where(o => o != null).
                    ToArray();


                //TODO: Join faces that share edge points


                //TODO: Dedupe points across sets of triangles



                return faceTriangles;
            }

            #endregion
            #region face polys 3

            //NOTE: This version3 is too fragile.  Keep the part that extends the face, but then do a convex hull to
            //get the final triangles.  That will clean up any holes
            public static TestFaceResult[] GetTestFacePolys3(PatchFragment patch, ITriangleIndexed[] hull, VoronoiResult3D voronoi)
            {
                List<TestFaceResult> retVal = new List<TestFaceResult>();

                var hullAABB = Math3D.GetAABB(hull);
                double rayExtend = (hullAABB.Item2 - hullAABB.Item1).Length * 10;

                int[] faces = voronoi.FacesByControlPoint[patch.ControlPointIndex];

                var workLater = new List<Tuple<int, Point3D[], Tuple<TriangleFragment, Tuple<TriangleEdge, int>[]>[]>>();

                foreach (int faceIndex in faces)
                {
                    var intersects = patch.Polygon.
                        Select(o => new
                        {
                            Triangle = o,
                            FaceHits = o.FaceIndices.Where(p => p.Item2 == faceIndex).ToArray()
                        }).
                        Where(o => o.FaceHits.Length > 0).
                        ToArray();

                    Point3D[] points = intersects.
                        SelectMany(o => o.FaceHits.SelectMany(p => new[] { o.Triangle.Triangle.GetPoint(p.Item1, true), o.Triangle.Triangle.GetPoint(p.Item1, false) })).
                        ToArray();

                    if (points.Length == 0)
                    {
                        // Add the entire face
                        //NOTE: Open faces should be ignored, because the only ones we care about are the ones that intersect.  Imagine the hull intersecing
                        //the base of a Y.  The V portion of the Y should be ignored
                        if (voronoi.Faces[faceIndex].IsClosed)
                        {
                            //TODO: Need an additional filter to see if this face touches the hull.  Store this face for later inclusion once the hull is more fully worked
                            //TODO: For v4, don't include this.  Just let the convex hull method fill in holes
                            retVal.Add(new TestFaceResult(voronoi.Faces[faceIndex], faceIndex, voronoi.Faces[faceIndex].GetPolygonTriangles(), null));
                        }
                        continue;
                    }

                    Point3D[] newPoints = AddFaceVerticies3(points, voronoi.Faces[faceIndex], rayExtend);

                    if (newPoints.Length == points.Length)
                    {
                    }

                    points = newPoints;


                    if (points.Length < 3)
                    {
                        workLater.Add(Tuple.Create(faceIndex, points, intersects.Select(o => Tuple.Create(o.Triangle, o.FaceHits)).ToArray()));
                        continue;
                    }

                    // Triangulate these points
                    //TODO: Can't assume that the face intersects are convex
                    var hull2D = Math2D.GetConvexHull(points);
                    TriangleIndexed[] triangles = Math2D.GetTrianglesFromConvexPoly(hull2D.PerimiterLines.Select(o => points[o]).ToArray());

                    retVal.Add(new TestFaceResult(voronoi.Faces[faceIndex], faceIndex, triangles, null));
                }



                //TODO: Dedupe points across sets of triangles



                return retVal.ToArray();
            }
            //NOTE: This method will never work 100% of the time.  It is only looking at the part of the intersection with the face
            //Instead, it needs to look at the intersection of the faces that clip the hull, and see if those common face verticies should be part of the hull
            private static Point3D[] AddFaceVerticies3(Point3D[] points, Face3D face, double rayExtend)
            {
                // Detect which of the points are on the face's edge, and extend along that edge
                var faceEdgeHits = face.Edges.
                    Select(o => new
                    {
                        Edge = o,
                        Points = points.Where(p => Math1D.IsNearZero(Math3D.GetClosestDistance_Line_Point(o.Point0, o.DirectionExt, p))).ToArray()        // Even though the edge is finite, there should never be a case where a face hit is off the edge segment.  So save some processing and just assume it's a line
                    }).
                    Where(o => o.Points.Length > 0).
                    ToArray();

                // Choose one of the points that aren't in faceEdgeHits.  This will define a direction not to go.  Then go through all the
                // face's points, and keep the ones that are the opposite direction of that line

                Point3D[] edgePoints = faceEdgeHits.
                    SelectMany(o => o.Points).
                    Distinct((o1, o2) => Math3D.IsNearValue(o1, o2)).
                    ToArray();

                if (edgePoints.Length > 2)
                {
                    return points.
                        Concat(edgePoints).
                        Distinct((o1, o2) => Math3D.IsNearValue(o1, o2)).
                        ToArray();
                }

                if (edgePoints == null) return points;
                else if (edgePoints.Length != 2) return points;

                Point3D? otherPoint = points.FirstOrDefault(o => !edgePoints.Any(p => p.IsNearValue(o)));

                if (otherPoint == null) return points;

                Point3D otherPoint2 = Math3D.GetClosestPoint_Line_Point(edgePoints[0], edgePoints[1] - edgePoints[0], otherPoint.Value);

                // Detect thin triangle
                //TODO: Choose another point
                double distanceOther = (otherPoint.Value - otherPoint2).Length;
                //if (otherPoint.Value.IsNearValue(otherPoint2))
                if (distanceOther.IsNearZero())
                    return points;

                double distanceLine = (edgePoints[1] - edgePoints[0]).Length;

                if (distanceOther / distanceLine < .01)
                    return points;

                Vector3D otherLine = otherPoint.Value - otherPoint2;

                Point3D[] faceMatches = face.Edges.
                    Select(o => new[] { o.Point0, o.GetPoint1Ext(rayExtend) }).
                    SelectMany(o => o).
                    Where(o => Vector3D.DotProduct(o - otherPoint2, otherLine) < 0).
                    Distinct((o1, o2) => Math3D.IsNearValue(o1, o2)).
                    ToArray();

                if (faceMatches.Length == 0) return points;

                return points.
                    Concat(faceMatches).
                    ToArray();
            }

            #endregion
            #region face polys 4

            public static TriangleIndexed[] GetTestFacePolys4(PatchFragment patch, ITriangleIndexed[] hullPatch, ITriangleIndexed[] hull, VoronoiResult3D voronoi)
            {
                List<TestFaceResult> retVal = new List<TestFaceResult>();

                var hullAABB = Math3D.GetAABB(hull);
                double rayExtend = (hullAABB.Item2 - hullAABB.Item1).Length * 10;

                int[] faces = voronoi.FacesByControlPoint[patch.ControlPointIndex];

                List<int> potentialFullFaces = new List<int>();

                foreach (int faceIndex in faces)
                {
                    var intersects = patch.Polygon.
                        Select(o => new
                        {
                            Triangle = o,
                            FaceHits = o.FaceIndices.Where(p => p.Item2 == faceIndex).ToArray()
                        }).
                        Where(o => o.FaceHits.Length > 0).
                        ToArray();

                    Point3D[] points = intersects.
                        SelectMany(o => o.FaceHits.SelectMany(p => new[] { o.Triangle.Triangle.GetPoint(p.Item1, true), o.Triangle.Triangle.GetPoint(p.Item1, false) })).
                        Distinct((p1, p2) => Math3D.IsNearValue(p1, p2)).
                        ToArray();

                    if (points.Length == 0)
                    {
                        // Add the entire face
                        //NOTE: Open faces should be ignored, because the only ones we care about are the ones that intersect.  Imagine the hull intersecing
                        //the base of a Y.  The V portion of the Y should be ignored
                        if (voronoi.Faces[faceIndex].IsClosed)
                        {
                            //TODO: Need an additional filter to see if this face touches the hull.  Store this face for later inclusion once the hull is more fully worked
                            //TODO: For v4, don't include this.  Just let the convex hull method fill in holes
                            //retVal.Add(new TestFaceResult(voronoi.Faces[faceIndex], faceIndex, voronoi.Faces[faceIndex].GetPolygonTriangles(), null));
                            potentialFullFaces.Add(faceIndex);
                        }
                        continue;
                    }

                    Point3D[] newPoints = AddFaceVerticies4(points, voronoi.Faces[faceIndex], rayExtend);
                    if (newPoints.Length == points.Length)
                    {
                    }
                    points = newPoints;


                    if (points.Length < 3)
                    {
                        continue;
                    }

                    // Triangulate these points
                    //TODO: Can't assume that the face intersects are convex
                    var hull2D = Math2D.GetConvexHull(points);
                    TriangleIndexed[] triangles = Math2D.GetTrianglesFromConvexPoly(hull2D.PerimiterLines.Select(o => points[o]).ToArray());

                    retVal.Add(new TestFaceResult(voronoi.Faces[faceIndex], faceIndex, triangles, null));
                }


                //TODO: Add skipped whole faces


                // Convert to a convex hull.  This will clean up any small holes that were missed above
                List<Point3D> finalPoints = new List<Point3D>();
                finalPoints.AddRange(TriangleIndexed.GetUsedPoints(retVal.Select(o => UtilityCore.Iterate(o.Poly1, o.Poly2))));
                finalPoints.AddRangeUnique(TriangleIndexed.GetUsedPoints(hullPatch), (p1, p2) => Math3D.IsNearValue(p1, p2));

                var returnHull = Math3D.GetConvexHull(finalPoints.ToArray());

                Point3D[] hullDeduped = TriangleIndexed.GetUsedPoints(returnHull);

                return returnHull;
            }
            private static Point3D[] AddFaceVerticies4(Point3D[] points, Face3D face, double rayExtend)
            {
                // Detect which of the points are on the face's edge, and extend along that edge
                var faceEdgeHits = face.Edges.
                    Select(o => new
                    {
                        Edge = o,
                        Points = points.Where(p => Math1D.IsNearZero(Math3D.GetClosestDistance_Line_Point(o.Point0, o.DirectionExt, p))).ToArray()        // Even though the edge is finite, there should never be a case where a face hit is off the edge segment.  So save some processing and just assume it's a line
                    }).
                    Where(o => o.Points.Length > 0).
                    ToArray();

                // If the edge hits are on one edge, then it is a polygon on the face, and the face's verticies should't be added (they definately
                // outside the polygon)
                if (faceEdgeHits.Length == 1) return points;

                // Choose one of the points that aren't in faceEdgeHits.  This will define a direction not to go.  Then go through all the
                // face's points, and keep the ones that are the opposite direction of that line

                Point3D[] edgePoints = faceEdgeHits.
                    SelectMany(o => o.Points).
                    Distinct((o1, o2) => Math3D.IsNearValue(o1, o2)).
                    ToArray();

                if (edgePoints.Length > 2)
                {
                    return points.
                        Concat(edgePoints).
                        Distinct((o1, o2) => Math3D.IsNearValue(o1, o2)).
                        ToArray();
                }

                if (edgePoints == null) return points;
                else if (edgePoints.Length != 2) return points;

                // Find the point that is farthest from the intersects
                Vector3D line = edgePoints[1] - edgePoints[0];

                var otherPoint = points.
                    Where(o => !edgePoints.Any(p => p.IsNearValue(o))).
                    Select(o =>
                    {
                        Point3D intersect = Math3D.GetClosestPoint_Line_Point(edgePoints[0], line, o);
                        Vector3D heightLine = o - intersect;

                        return new
                        {
                            Intersect = intersect,
                            HeightLine = heightLine,
                            HeightLineLen = heightLine.Length,
                        };
                    }).
                    OrderByDescending(o => o.HeightLineLen).
                    FirstOrDefault();

                // Detect thin triangle
                if (otherPoint == null) return points;
                else if (otherPoint.HeightLine.IsNearZero()) return points;
                else if (otherPoint.HeightLineLen / line.Length < .015) return points;

                // Draw lines between the face verticies and that intersect points, and keep the ones with a negative dot product (the verticies that are away from that line)
                Point3D[] faceMatches = face.Edges.
                    Select(o => new[] { o.Point0, o.GetPoint1Ext(rayExtend) }).
                    SelectMany(o => o).
                    Where(o => Vector3D.DotProduct(o - otherPoint.Intersect, otherPoint.HeightLine) < 0).
                    Distinct((o1, o2) => Math3D.IsNearValue(o1, o2)).
                    ToArray();

                if (faceMatches.Length == 0) return points;

                return points.
                    Concat(faceMatches).
                    ToArray();
            }

            #endregion

            #region Private Methods - sub hulls

            private static ITriangleIndexed[] GetSubHull(PatchFragment patch, ITriangleIndexed[] hull, VoronoiResult3D voronoi, Point3D hullCenter)
            {
                // I think it's flawed thinking to see if the control point is interior
                //if (IsInteriorControlPoint(patch, voronoi, hullCenter))
                //{
                //    return GetSubHull_Interior(patch, hull, voronoi);
                //}
                //else
                //{
                //    return GetSubHull_Exterior(patch, hull, voronoi);
                //}




                var faces = voronoi.FacesByControlPoint[patch.ControlPointIndex].
                    Select(o => new
                    {
                        FaceIndex = o,
                        Triangles = patch.Polygon.
                            Select(p => new { Triangle = p, FaceHits = p.FaceIndices.Where(q => q.Item2 == o).ToArray() }).
                            Where(p => p.FaceHits.Length > 0).
                            ToArray()
                    }).
                    ToArray();





                //int[] faces = voronoi.FacesByControlPoint[patch.ControlPointIndex];

                //foreach (int faceIndex in faces)
                //{
                //    patch.Polygon.
                //        Select(o => new { Triangle = o, FaceHits = o.FaceIndices.Where(p => p.Item2 == faceIndex).ToArray() }).
                //        Where(o => o.FaceHits.Length > 0).
                //        ToArray();









                //}







                return null;
            }

            private static bool IsInteriorControlPoint(PatchFragment patch, VoronoiResult3D voronoi, Point3D hullCenter)
            {
                Point3D ctrlPoint = voronoi.ControlPoints[patch.ControlPointIndex];

                List<bool> results = new List<bool>();

                foreach (var triangle in patch.Polygon)
                {
                    double centerDot = Vector3D.DotProduct(hullCenter - triangle.Triangle.Point0, triangle.Triangle.Normal);
                    double ctrlDot = Vector3D.DotProduct(ctrlPoint - triangle.Triangle.Point0, triangle.Triangle.Normal);

                    if (Math1D.IsNearZero(centerDot) || Math1D.IsNearZero(centerDot))
                    {
                        continue;
                    }

                    // If they are both the same sign, then the control point is on the same side of the patch as the center (meaning: interior)
                    results.Add(Math1D.IsNearPositive(centerDot) == Math1D.IsNearPositive(ctrlDot));
                }

                if (results.Count == 0)
                {
                    throw new ApplicationException("Couldn't find a triangle to compare");
                }

                int trueCount = results.Where(o => o).Count();
                int falseCount = results.Where(o => !o).Count();

                bool retVal = trueCount > falseCount;

                double ratio = Convert.ToDouble(retVal ? trueCount : falseCount) / Convert.ToDouble(trueCount + falseCount);

                //if(ratio < 1d)
                //{
                //    int four = 15;
                //}


                //if(ratio < .75)
                //{
                //    //throw new ApplicationException("Couldn't get a clear enough answer");
                //    int seven = 12;
                //}

                return retVal;
            }

            #region Class: Segment_Face

            public class Segment_Face
            {
                public Segment_Face(int index0, int index1, IList<Point3D> allPoints)
                {
                    this.Index0 = index0;
                    this.Index1 = index1;

                    this.AllPoints = allPoints;

                    this.IsFaceEdge = false;
                    this.FaceEdge = null;
                }
                public Segment_Face(int index0, int index1, IList<Point3D> allPoints, Edge3D faceEdge, int? corner0, int? corner1)
                {
                    this.Index0 = index0;
                    this.Index1 = index1;

                    this.AllPoints = allPoints;

                    this.IsFaceEdge = true;
                    this.FaceEdge = faceEdge;

                    this.Corner0 = corner0;
                    this.Corner1 = corner1;
                }

                public readonly int Index0;
                public readonly int Index1;

                public Point3D Point0
                {
                    get
                    {
                        return this.AllPoints[this.Index0];
                    }
                }
                public Point3D Point1
                {
                    get
                    {
                        return this.AllPoints[this.Index1];
                    }
                }

                public readonly IList<Point3D> AllPoints;

                public readonly bool IsFaceEdge;
                public readonly Edge3D FaceEdge;

                // These are tough to name.  If point0 is a face corner, then Corner0 will hold 0 or 1 (which end of the edge the point came from)
                public readonly int? Corner0;
                public readonly int? Corner1;

                /// <summary>
                /// This is useful when looking at lists of edges in the quick watch
                /// </summary>
                public override string ToString()
                {
                    const string DELIM = "       |       ";

                    StringBuilder retVal = new StringBuilder(100);

                    retVal.Append(this.IsFaceEdge ? "face edge" : "trian edge");
                    retVal.Append(DELIM);

                    retVal.Append(string.Format("{0} - {1}{2}({3}) ({4})",
                        this.Index0,
                        this.Index1,
                        DELIM,
                        this.Point0.ToString(2),
                        this.Point1.ToString(2)));

                    return retVal.ToString();
                }
            }

            #endregion

            private static FaceTriangulation GetFaceTriangles(int faceIndex, PatchFragment patch, VoronoiResult3D voronoi)
            {
                // Get all the points that touch the face (stored as segments)
                var faceIntersects = GetFaceIntersects(faceIndex, patch);
                if (faceIntersects == null)
                {
                    return null;
                }

                List<Segment_Face> segments = new List<Segment_Face>(faceIntersects.Item1);
                List<Point3D> points = new List<Point3D>(faceIntersects.Item2);

                // Add the face's edges (broken up by the intersecting points)
                AddFaceEdges(faceIndex, voronoi, segments, points);

                // Get all possible loops of segments
                var chains = ChainAttempt1.GetChains_Multiple(
                    segments.Select(o => Tuple.Create(o.Index0, o.Index1, o)).ToArray(),        // these are the segments mapped to something the method can use
                    (o, p) => o == p,       // int compare
                    (o, p) => o.Index0 == p.Index0 && o.Index1 == p.Index1);        // face edge compare (can't use o.FaceEdge.Token, because FaceEdge could be null).  It's safe to always compare index0 with index0, because duplicate/reverse segments will never be created

                // Throw out chains
                var loops = chains.
                    Where(o => o.Item2).
                    ToArray();


                // Each loop is a polygon (could be concave).  Triangulate them



                return null;
            }

            private static Tuple<IEnumerable<Segment_Face>, IEnumerable<Point3D>> GetFaceIntersects(int faceIndex, PatchFragment patch)
            {
                // Get all the points that touch this face
                var intersects = patch.Polygon.
                    Select(o => new
                    {
                        Triangle = o,
                        FaceHits = o.FaceIndices.Where(p => p.Item2 == faceIndex).ToArray()
                    }).
                    Where(o => o.FaceHits.Length > 0);

                List<Point3D> points = intersects.
                    SelectMany(o => o.FaceHits.SelectMany(p => new[] { o.Triangle.Triangle.GetPoint(p.Item1, true), o.Triangle.Triangle.GetPoint(p.Item1, false) })).
                    Distinct((o, p) => Math3D.IsNearValue(o, p)).
                    ToList();

                if (points.Count == 0)
                {
                    return null;
                }

                Func<Point3D, Point3D, bool> pointCompare = (o, p) => Math3D.IsNearValue(o, p);

                // Store these as segments
                List<Segment_Face> segments = intersects.
                    SelectMany(o => o.FaceHits.Select(p => new Segment_Face(
                        points.IndexOf(o.Triangle.Triangle.GetPoint(p.Item1, true), pointCompare),
                        points.IndexOf(o.Triangle.Triangle.GetPoint(p.Item1, false), pointCompare),
                        points))).
                    ToList();

                #region Validate
#if DEBUG
                if (segments.Any(o => o.Index0 < 0 || o.Index1 < 0))
                {
                    throw new ApplicationException("fail");
                }
#endif
                #endregion

                return new Tuple<IEnumerable<Segment_Face>, IEnumerable<Point3D>>(segments, points);
            }

            private static void AddFaceEdges(int faceIndex, VoronoiResult3D voronoi, List<Segment_Face> segments, List<Point3D> points)
            {
                // Detect which of the points are on the face's edge
                var faceEdgeHits = voronoi.Faces[faceIndex].Edges.
                    Select(o => new
                    {
                        Edge = o,
                        Points = points.Where(p => Math1D.IsNearZero(Math3D.GetClosestDistance_Line_Point(o.Point0, o.DirectionExt, p))).ToArray()        // Even though the edge is finite, there should never be a case where a face hit is off the edge segment.  So save some processing and just assume it's a line
                    }).
                    Where(o => o.Points.Length > 0).
                    ToArray();

                if (faceEdgeHits.Length > 0)
                {
                    #region Add face edges

                    List<Segment_Face> faceSegments = new List<Segment_Face>();

                    // Extend edge hits to the corners, store all pieces as segments (that aren't rays)
                    foreach (var faceEdgeHit in faceEdgeHits)
                    {
                        faceSegments.AddRange(DivideFaceEdgeHit(faceEdgeHit.Edge, faceEdgeHit.Points, points));
                    }

                    // Merge into the common list
                    segments.AddRange(faceSegments);

                    // Also store face edges that haven't been touched, but are between extensions
                    for (int cntr = 0; cntr < voronoi.Faces[faceIndex].Edges.Length; cntr++)
                    {
                        Edge3D edge = voronoi.Faces[faceIndex].Edges[cntr];

                        if (edge.EdgeType != EdgeType.Segment)
                        {
                            // Rays that haven't been intersected won't make polygons
                            continue;
                        }

                        if (!faceSegments.Any(o => o.FaceEdge.Token == edge.Token))
                        {
                            // Just add it straight to segments.  That way it's not compared against by other face edges
                            segments.Add(new Segment_Face(
                                GetPointIndex(edge.Point0, points),
                                GetPointIndex(edge.Point1.Value, points),
                                points, edge, 0, 1));
                        }
                    }

                    #endregion
                }
            }


            private static IEnumerable<Segment_Face> DivideFaceEdgeHit(Edge3D edge, Point3D[] intersectPoints, List<Point3D> allPoints)
            {
                // Sort the points by distance from edge.0
                Point3D[] sorted = intersectPoints.
                    Select(o => new { Point = o, Distance = (o - edge.Point0).LengthSquared }).
                    OrderBy(o => o.Distance).
                    Select(o => o.Point).
                    ToArray();

                List<Segment_Face> retVal = new List<Segment_Face>();

                // edge.0 to first point
                retVal.Add(new Segment_Face(GetPointIndex(edge.Point0, allPoints), GetPointIndex(sorted[0], allPoints), allPoints, edge, 0, null));

                // points[n] to points[n + 1]
                for (int cntr = 0; cntr < sorted.Length - 1; cntr++)
                {
                    retVal.Add(new Segment_Face(GetPointIndex(sorted[cntr], allPoints), GetPointIndex(sorted[cntr + 1], allPoints), allPoints, edge, null, null));
                }

                // last point to edge.1 (unless it's a ray)
                if (edge.EdgeType == EdgeType.Segment)
                {
                    retVal.Add(new Segment_Face(GetPointIndex(sorted[sorted.Length - 1], allPoints), GetPointIndex(edge.Point1.Value, allPoints), allPoints, edge, null, 1));
                }

                return retVal;
            }


            #endregion
            #region Private Methods - fragments

            private static PatchFragment[] GetTrianglesByControlPoint(ITriangleIndexed[] triangles, VoronoiResult3D voronoi, int[] controlPointsByTrianglePoint)
            {
                List<PolyFragIntermediate> buildingFinals = new List<PolyFragIntermediate>();

                List<Point3D> newPoints = new List<Point3D>();

                List<Tuple<long, int>> triangleFaceMisses = new List<Tuple<long, int>>();
                List<TriInt_I> intermediate = new List<TriInt_I>();

                // Figure out how long rays should extend
                var aabb = Math3D.GetAABB(voronoi.EdgePoints);
                double rayLength = (aabb.Item2 - aabb.Item1).Length * 10;

                // Get the aabb of each face
                var faceAABBs = voronoi.Faces.
                    Select(o => Math3D.GetAABB(o.Edges, rayLength)).
                    ToArray();

                for (int triangleIndex = 0; triangleIndex < triangles.Length; triangleIndex++)
                {
                    ITriangleIndexed triangle = triangles[triangleIndex];

                    if (controlPointsByTrianglePoint[triangle.Index0] == controlPointsByTrianglePoint[triangle.Index1] && controlPointsByTrianglePoint[triangle.Index0] == controlPointsByTrianglePoint[triangle.Index2])
                    {
                        #region Interior Triangle

                        // The entire triangle is inside the control point's cell
                        int index0 = GetPointIndex(triangle.Point0, newPoints);
                        int index1 = GetPointIndex(triangle.Point1, newPoints);
                        int index2 = GetPointIndex(triangle.Point2, newPoints);

                        var faceIndices = new Tuple<TriangleEdge, int>[0];

                        buildingFinals.Add(new PolyFragIntermediate(controlPointsByTrianglePoint[triangle.Index0], triangleIndex, faceIndices, index0, index1, index2));

                        #endregion
                        continue;
                    }

                    var triangleAABB = Math3D.GetAABB(triangle);

                    for (int faceIndex = 0; faceIndex < voronoi.Faces.Length; faceIndex++)
                    {
                        if (!Math3D.IsIntersecting_AABB_AABB(triangleAABB.Item1, triangleAABB.Item2, faceAABBs[faceIndex].Item1, faceAABBs[faceIndex].Item2))
                        {
                            continue;
                        }

                        GetTrianglesByControlPoint_FaceTriangle(triangle, triangleIndex, faceIndex, voronoi, controlPointsByTrianglePoint, intermediate, triangleFaceMisses, rayLength);
                    }
                }

                // Convert each intersected triangle into smaller triangles
                buildingFinals.AddRange(ConvertToSmaller(newPoints, intermediate));

                Point3D[] allPoints = newPoints.ToArray();

                // Map to the output type
                return buildingFinals.
                    GroupBy(o => o.ControlPointIndex).
                    Select(o => new PatchFragment(o.Select(p => new TriangleFragment(
                        new TriangleIndexed(p.TriangleIndex0, p.TriangleIndex1, p.TriangleIndex2, allPoints),
                        p.FaceIndices,
                        p.OriginalTriangleIndex
                        )).ToArray(), o.Key)).
                    ToArray();
            }

            private static void GetTrianglesByControlPoint_FaceTriangle(ITriangleIndexed triangle, int triangleIndex, int faceIndex, VoronoiResult3D voronoi, int[] controlPointsByTrianglePoint, List<TriInt_I> intersections, List<Tuple<long, int>> triangleFaceMisses, double rayLength)
            {
                Tuple<long, int> key = Tuple.Create(triangle.Token, faceIndex);

                // See if this triangle and face have been intersected
                if (triangleFaceMisses.Contains(key) || HasIntersection(triangle, faceIndex, intersections))
                {
                    return;
                }

                // Intersect the face and triangle
                Tuple<Point3D, Point3D> segment = Math3D.GetIntersection_Face_Triangle(voronoi.Faces[faceIndex], triangle, rayLength);

                if (segment == null)
                {
                    triangleFaceMisses.Add(key);
                    return;
                }

                // Store this intersection segment for every control point that has this face
                foreach (int controlIndex in voronoi.GetControlPoints(faceIndex))
                {
                    // Figure out which corners of the triangle are in this control point's cell
                    int[] triangleIndices = triangle.IndexArray.
                        Where(o => controlPointsByTrianglePoint[o] == controlIndex).
                        ToArray();

                    AddIntersection(triangle, triangleIndex, triangleIndices, controlIndex, faceIndex, voronoi.Faces[faceIndex], segment, intersections);
                }
            }

            private static IEnumerable<PolyFragIntermediate> ConvertToSmaller(List<Point3D> newPoints, List<TriInt_I> intersections)
            {
                List<PolyFragIntermediate> retVal = new List<PolyFragIntermediate>();

                foreach (TriInt_I triangle in intersections)
                {
                    foreach (TriInt_II controlPoint in triangle.ControlPoints)
                    {
                        IEnumerable<ITriangle> smalls;

                        if (controlPoint.Faces.Count == 1)
                        {
                            smalls = ConvertToSmaller_Single(triangle, controlPoint, controlPoint.Faces[0]);
                        }
                        else
                        {
                            smalls = ConvertToSmaller_Multi(triangle, controlPoint, controlPoint.Faces);
                        }

                        foreach (ITriangle small in smalls)
                        {
                            ITriangle smallValidNormal = MatchNormal(small, triangle.Triangle);

                            // Convert small into a final triangle indexed
                            int index0 = GetPointIndex(smallValidNormal.Point0, newPoints);
                            int index1 = GetPointIndex(smallValidNormal.Point1, newPoints);
                            int index2 = GetPointIndex(smallValidNormal.Point2, newPoints);

                            Tuple<TriangleEdge, int>[] faceIndices = LinkSegmentsWithFaces(index0, index1, index2, newPoints, controlPoint.Faces);

                            retVal.Add(new PolyFragIntermediate(controlPoint.ControlPointIndex, triangle.TriangleIndex, faceIndices, index0, index1, index2));
                        }
                    }
                }

                return retVal;
            }
            private static IEnumerable<ITriangle> ConvertToSmaller_Single(TriInt_I triangle, TriInt_II controlPoint, TriInt_III face)
            {
                Point3D[] points3D = UtilityCore.Iterate(face.TriangleIndices.Select(o => triangle.Triangle.AllPoints[o]), new[] { face.Segment.Item1, face.Segment.Item2 }).ToArray();

                return ConvertToSmaller_Finish(triangle, points3D);
            }
            private static IEnumerable<ITriangle> ConvertToSmaller_Multi(TriInt_I triangle, TriInt_II controlPoint, List<TriInt_III> faces)
            {
                IEnumerable<Point3D> trianglePoints = faces.
                    SelectMany(o => o.TriangleIndices).
                    Distinct().
                    Select(o => triangle.Triangle.AllPoints[o]);

                var chains1 = UtilityCore.GetChains(faces.Select(o => o.Segment), (o, p) => Math3D.IsNearValue(o, p));
                var chains2 = chains1;
                if (chains2.Length == 0)
                {
                    return new ITriangle[0];
                }
                else if (chains2.Length > 1 && chains2.Any(o => o.Item2))
                {
                    return new ITriangle[0];
                }
                else if (chains2.Length > 1)
                {
                    // This sometimes happens.  Just combine the chains
                    chains2 = new[] { Tuple.Create(chains2.SelectMany(o => o.Item1).ToArray(), false) };
                }

                Point3D[] points3D = UtilityCore.Iterate(trianglePoints, chains2[0].Item1).ToArray();

                return ConvertToSmaller_Finish(triangle, points3D);
            }
            private static IEnumerable<ITriangle> ConvertToSmaller_Finish(TriInt_I triangle, Point3D[] points)
            {
                if (points.Length < 3)
                {
                    //throw new ApplicationException("handle less than 2");
                    return new ITriangle[0];
                }
                else if (points.Length == 3)
                {
                    return new[] { new Triangle(points[0], points[1], points[2]) };
                }

                // The points could come in any order, so convert into a polygon to figure out the order
                //NOTE: At first, I tried Math2D.GetDelaunayTriangulation(), which works in most cases.  But when two points are really close together, it weirds out and returns nothing
                var hull2D = Math2D.GetConvexHull(points);

                // Now triangulate it
                return Math2D.GetTrianglesFromConvexPoly(hull2D.PerimiterLines.Select(o => points[o]).ToArray());
            }

            private static Tuple<TriangleEdge, int>[] LinkSegmentsWithFaces(int index0, int index1, int index2, List<Point3D> newPoints, List<TriInt_III> faces)
            {
                var retVal = new List<Tuple<TriangleEdge, int>>();

                // Store the triangle's points as something queryable
                var points = new[]
                    {
                        new { Index = 0, Point = newPoints[index0]},
                        new { Index = 1, Point = newPoints[index1]},
                        new { Index = 2, Point = newPoints[index2]},
                    };

                foreach (var face in faces)
                {
                    // Find two points of the triangle that match this face intersect
                    var matches = new[] { face.Segment.Item1, face.Segment.Item2 }.
                        Select(o => points.FirstOrDefault(p => Math3D.IsNearValue(p.Point, o))).
                        Where(o => o != null).
                        OrderBy(o => o.Index).
                        ToArray();

                    if (matches.Length < 2)
                    {
                        continue;
                    }

                    TriangleEdge edge;

                    if (matches[0].Index == 0 && matches[1].Index == 1)
                    {
                        edge = TriangleEdge.Edge_01;
                    }
                    else if (matches[0].Index == 0 && matches[1].Index == 2)
                    {
                        edge = TriangleEdge.Edge_20;
                    }
                    else if (matches[0].Index == 1 && matches[1].Index == 2)
                    {
                        edge = TriangleEdge.Edge_12;
                    }
                    else
                    {
                        throw new ApplicationException("Unexpected pair: " + matches[0].Index.ToString() + ", " + matches[1].Index.ToString());
                    }

                    retVal.Add(Tuple.Create(edge, face.FaceIndex));
                }

                return retVal.ToArray();
            }

            private static void AddFinal(int controlPoint, ITriangleIndexed triangle, SortedList<int, List<ITriangleIndexed>> triangleByControlPoint)
            {
                List<ITriangleIndexed> list;
                if (!triangleByControlPoint.TryGetValue(controlPoint, out list))
                {
                    list = new List<ITriangleIndexed>();
                    triangleByControlPoint.Add(controlPoint, list);
                }

                list.Add(triangle);
            }

            private static bool HasIntersection(ITriangleIndexed triangle, int faceIndex, List<TriInt_I> intersections)
            {
                TriInt_I i = intersections.FirstOrDefault(o => o.Triangle.Token == triangle.Token);
                if (i == null)
                {
                    return false;
                }

                foreach (TriInt_II ii in i.ControlPoints)
                {
                    TriInt_III iii = ii.Faces.FirstOrDefault(o => o.FaceIndex == faceIndex);
                    if (iii != null)
                    {
                        return true;
                    }
                }

                return false;
            }
            private static void AddIntersection(ITriangleIndexed triangle, int triangleIndex, int[] triangleIndices, int controlPointIndex, int faceIndex, Face3D face, Tuple<Point3D, Point3D> segment, List<TriInt_I> intersections)
            {
                TriInt_I i = intersections.FirstOrDefault(o => o.Triangle.Token == triangle.Token);
                if (i == null)
                {
                    i = new TriInt_I(triangle, triangleIndex);
                    intersections.Add(i);
                }

                TriInt_II ii = i.ControlPoints.FirstOrDefault(o => o.ControlPointIndex == controlPointIndex);
                if (ii == null)
                {
                    ii = new TriInt_II(controlPointIndex);
                    i.ControlPoints.Add(ii);
                }

                TriInt_III iii = ii.Faces.FirstOrDefault(o => o.FaceIndex == faceIndex);
                if (iii == null)
                {
                    iii = new TriInt_III(face, faceIndex, segment, triangleIndices);
                    ii.Faces.Add(iii);
                }
            }

            private static void RemoveNulls(List<TriInt_I> intersections)
            {
                // ugly, but it works :)

                int i = 0;
                while (i < intersections.Count)
                {
                    #region i

                    int ii = 0;
                    while (ii < intersections[i].ControlPoints.Count)
                    {
                        #region ii

                        int iii = 0;
                        while (iii < intersections[i].ControlPoints[ii].Faces.Count)
                        {
                            #region iii

                            if (intersections[i].ControlPoints[ii].Faces[iii].Segment == null)
                            {
                                intersections[i].ControlPoints[ii].Faces.RemoveAt(iii);
                            }
                            else
                            {
                                iii++;
                            }

                            #endregion
                        }

                        if (intersections[i].ControlPoints[ii].Faces.Count == 0)
                        {
                            intersections[i].ControlPoints.RemoveAt(ii);
                        }
                        else
                        {
                            ii++;
                        }

                        #endregion
                    }

                    if (intersections[i].ControlPoints.Count == 0)
                    {
                        intersections.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }

                    #endregion
                }
            }

            private static int GetPointIndex(Point3D point, List<Point3D> points)
            {
                for (int cntr = 0; cntr < points.Count; cntr++)
                {
                    if (Math3D.IsNearValue(points[cntr], point))
                    {
                        return cntr;
                    }
                }

                points.Add(point);
                return points.Count - 1;
            }

            private static ITriangle MatchNormal(ITriangle toMatch, ITriangle matchAgainst)
            {
                if (Vector3D.DotProduct(toMatch.Normal, matchAgainst.Normal) < 0)
                {
                    return new Triangle(toMatch.Point0, toMatch.Point2, toMatch.Point1);
                }
                else
                {
                    return toMatch;
                }
            }

            /// <summary>
            /// This returns which control point each triangle point is closest to
            /// </summary>
            /// <returns>
            /// Index into return = Triangle point index
            /// Value of return = Voronoi control point index
            /// </returns>
            private static int[] GetNearestControlPoints(Point3D[] allPoints, VoronoiResult3D voronoi)
            {
                // Cache the index with the point
                var controlPoints = voronoi.ControlPoints.
                    Select((o, i) => new { Point = o, Index = i }).
                    ToArray();

                // Find the closest control point for each triangle point
                return allPoints.
                    Select(o =>
                        controlPoints.
                            OrderBy(p => (o - p.Point).LengthSquared).
                            First().
                            Index).
                    ToArray();
            }

            #endregion
        }

        #endregion
        #region Class: ChainAttempt1

        //TODO: Put in UtilityCore
        private static class ChainAttempt1
        {
            /// <remarks>
            /// NOTE: Lower methods take TKey?, which must be a struct.  If there are cases where key is a class, then make an with the
            /// same basic logic, but contrains to class
            /// </remarks>
            public static Tuple<TItem[], bool>[] GetChains_Multiple<TKey, TItem>(Tuple<TKey, TKey, TItem>[] segments, Func<TKey, TKey, bool> compareKey, Func<TItem, TItem, bool> compareItem) where TKey : struct
            {
                //This was copied from Math3D.DelaunayVoronoi3D.WalkChains

                var retVal = new List<Tuple<TItem[], bool>>();

                foreach (var segment in segments)
                {
                    retVal.AddRange(WalkChains(segment, segments, retVal, compareKey, compareItem));
                }

                return retVal.ToArray();
            }

            /// <summary>
            /// This takes a starting segment, and returns all chains/loops that contain that segment
            /// </summary>
            /// <remarks>
            /// A face can either begin and end with rays, or can be a loop of segments.  This method walks in one direction:
            ///     If it finds a ray, it walks the other direction to find the other ray
            ///     If it loops back to the original segment, then it stops with that face
            ///     
            /// While walking, there will be other segments and rays that branch (not coplanar with the currently walked chain).  Those
            /// are ignored.  This method will get called for all segments, so those other branches will get picked up in another run
            /// </remarks>
            private static Tuple<TItem[], bool>[] WalkChains<TKey, TItem>(Tuple<TKey, TKey, TItem> start, Tuple<TKey, TKey, TItem>[] allSegments, IEnumerable<Tuple<TItem[], bool>> finishedChains, Func<TKey, TKey, bool> compareKey, Func<TItem, TItem, bool> compareItem) where TKey : struct
            {
                var nextLeft = GetNext_Point_Edges(start, allSegments, compareKey, compareItem);

                var retVal = new List<Tuple<TItem[], bool>>();

                foreach (var nextLeftEdge in nextLeft.Item2)
                {
                    if (HasSeenSegments(start.Item3, nextLeftEdge.Item3, finishedChains, compareItem))
                    {
                        continue;
                    }

                    var combinedFinished = UtilityCore.Iterate(finishedChains, retVal).ToArray();

                    // Walk left
                    //Tuple<bool, Tuple<TKey, TKey, TItem>[]> partialLeft;
                    var partialLeft = WalkChain(start, nextLeftEdge, nextLeft.Item1, true, allSegments, combinedFinished, compareKey, compareItem, true);

                    if (partialLeft.Item1)
                    {
                        // Going left formed a loop
                        retVal.Add(BuildFinalChain(compareItem, partialLeft.Item2, start));
                    }
                    else
                    {
                        #region Walk right

                        // Going left ended in a ray.  Go right to find the other ray (but only the path with the same axis)
                        var partialRight = WalkChain(start, start, null, false, allSegments, combinedFinished, compareKey, compareItem, false);
                        if (partialRight == null)
                        {
                            // This happens when partialLeft is for a ray that belongs to a different control point
                            continue;
                        }

                        #region asserts
#if DEBUG
                        if (partialRight.Item1)
                        {
                            throw new ApplicationException("When walking right, there should never be a loop");
                        }
#endif
                        #endregion

                        // Stitch the two chains together (both end in a ray)
                        retVal.Add(BuildFinalChain(compareItem, partialLeft.Item2, start, partialRight.Item2));

                        #endregion
                    }
                }

                return retVal.ToArray();
            }

            /// <summary>
            /// This walks in one direction, and recurses for each segment it encounters.  It stops when it encounters a ray,
            /// or loops back to the original segment
            /// </summary>
            /// <remarks>
            /// Using the term ray to mean a segment with only one connect (the method this was copied from actually
            /// had rays
            /// </remarks>
            /// <returns>
            /// Item1 = Whether it is a loop (if false, the last element of Item2 is a ray)
            /// Item2 = Segments that make a chain or loop
            /// </returns>
            private static Tuple<bool, Tuple<TKey, TKey, TItem>[]> WalkChain<TKey, TItem>(Tuple<TKey, TKey, TItem> start, Tuple<TKey, TKey, TItem> current, TKey? commonPoint, bool isLeft, Tuple<TKey, TKey, TItem>[] allSegments, IEnumerable<Tuple<TItem[], bool>> finishedChains, Func<TKey, TKey, bool> compareKey, Func<TItem, TItem, bool> compareItem, bool includeCurrentInReturn = false) where TKey : struct
            {

                //TODO: When there is a fork, need to walk all paths, and return unique chains - maybe?

                var returnChain = new List<Tuple<TKey, TKey, TItem>>();
                bool? endedAsLoop = null;

                if (includeCurrentInReturn)
                {
                    returnChain.Add(current);
                }

                // Find segments tied to the part of current that isn't commonPoint
                var next = GetNext_Point_Edges(current, allSegments, compareKey, compareItem, isLeft, commonPoint);

                foreach (var nextSegment in next.Item2)
                {
                    if (compareItem(nextSegment.Item3, start.Item3))
                    {
                        // Looped around.  Don't add this to the return list
                        endedAsLoop = true;
                        continue;       // can't return yet, because there may still be branches - not sure this is valid anymore (probably written like this when it was gathering branches)
                    }

                    // See if this path has already been done
                    if (HasSeenSegments(current.Item3, nextSegment.Item3, finishedChains, compareItem))
                    {
                        continue;
                    }

                    returnChain.Add(nextSegment);

                    // Recurse
                    var recursedResult = WalkChain(start, nextSegment, next.Item1, isLeft, allSegments, finishedChains, compareKey, compareItem);

                    returnChain.AddRange(recursedResult.Item2);
                    endedAsLoop = recursedResult.Item1;

                    break;
                }




                //TODO: See if this if statment is still needed
                if (endedAsLoop == null)
                {
                    // This is sometimes a legitimate case.  When getting edges by control point, multirays sometimes have a ray that is for a different
                    // control point.  This if statement should only hit when walking right (because walking left found a ray)
                    if (isLeft)
                    {
                        throw new ApplicationException("Didn't find the next link in the chain");
                    }

                    return null;
                }




                return Tuple.Create(endedAsLoop.Value, returnChain.ToArray());
            }

            /// <summary>
            /// This returns edges that touch one of the ends of the segment passed in (current must be of type segment)
            /// NOTE: This only returns edges that touch one side of current, not both
            /// </summary>
            /// <remarks>
            /// This gets called from several places.  If it's called for the first time, then isLeft becomes important (left is index0, right is index1)
            /// If it's called while walking a chain, then commonPointIndex becomes important (returns for index that isn't commonPointIndex)
            /// </remarks>
            private static Tuple<TKey, Tuple<TKey, TKey, TItem>[]> GetNext_Point_Edges<TKey, TItem>(Tuple<TKey, TKey, TItem> current, Tuple<TKey, TKey, TItem>[] allSegments, Func<TKey, TKey, bool> compareKey, Func<TItem, TItem, bool> compareItem, bool isLeft = true, TKey? commonKey = null) where TKey : struct
            {
                TKey nextKey;
                if (commonKey == null)
                {
                    nextKey = isLeft ? current.Item1 : current.Item2;
                }
                else if (compareKey(current.Item1, commonKey.Value))
                {
                    nextKey = current.Item2;
                }
                else
                {
                    nextKey = current.Item1;
                }

                // Find the segments that have this key (not including this segment)
                var nextSegments = allSegments.
                    Where(o => !compareItem(o.Item3, current.Item3)).       // grab the segments that aren't current
                    Where(o => compareKey(o.Item1, nextKey) || compareKey(o.Item2, nextKey)).       // grab the segments that contain nextKey
                    ToArray();

                return Tuple.Create(nextKey, nextSegments);
            }

            private static bool HasSeenSegments<TItem>(TItem segment1, TItem segment2, IEnumerable<Tuple<TItem[], bool>> finishedChains, Func<TItem, TItem, bool> compareItem)
            {
                foreach (var chain in finishedChains)
                {
                    // See if this chain contains both segments
                    if (chain.Item1.Any(o => compareItem(o, segment1)) && chain.Item1.Any(o => compareItem(o, segment2)))
                    {
                        return true;
                    }
                }

                return false;
            }

            private static Tuple<TItem[], bool> BuildFinalChain<TKey, TItem>(Func<TItem, TItem, bool> compareItem, Tuple<TKey, TKey, TItem>[] chain1, Tuple<TKey, TKey, TItem> middle, Tuple<TKey, TKey, TItem>[] chain2 = null) where TKey : struct
            {
                TItem[] retVal;
                bool isClosed;

                if (chain2 == null)
                {
                    #region Left, Middle

                    retVal = UtilityCore.Iterate<TItem>(middle.Item3, chain1.Select(o => o.Item3)).ToArray();
                    isClosed = true;

                    #endregion
                }
                else
                {
                    #region Left, Middle, Right

                    // Merge the chains
                    retVal = UtilityCore.Iterate<TItem>(
                        chain1.Select(o => o.Item3).Reverse(),      // reversing, because the chain walks toward a ray.  Need to go from ray toward middle
                        middle.Item3,
                        chain2.Select(o => o.Item3)
                        ).ToArray();

                    // See if the chain is closed (a loop) - I don't think it ever will be
                    if (retVal.Length > 1 && compareItem(retVal[0], retVal[retVal.Length - 1]))
                    {
                        retVal = retVal.Take(retVal.Length - 1).ToArray();
                        isClosed = true;
                    }
                    else
                    {
                        isClosed = false;
                    }

                    #endregion
                }

                return Tuple.Create(retVal, isClosed);
            }
        }

        #endregion
        #region Class: ChainAttempt2

        private static class ChainAttempt2
        {

            //TODO: Copy over bits of ShadowsWindow.PolyUnion2.StitchSegments2


            public static Tuple<TItem[], bool>[] GetChains_Multiple<TKey, TItem>(Tuple<TKey, TKey, TItem>[] segments, Func<TKey, TKey, bool> compareKey, Func<TItem, TItem, bool> compareItem) where TKey : struct
            {
                // Get rid non loops
                Tuple<TKey, TKey, TItem>[] reducedSegments;
                Tuple<TItem[], bool>[] tails;
                RemoveTails(out reducedSegments, out tails, segments, compareKey);





                return null;
            }


            private static void RemoveTails<TKey, TItem>(out Tuple<TKey, TKey, TItem>[] reducedSegments, out Tuple<TItem[], bool>[] tails, Tuple<TKey, TKey, TItem>[] segments, Func<TKey, TKey, bool> compareKey) where TKey : struct
            {
                var segmentList = new List<Tuple<TKey, TKey, TItem>>(segments);

                var tailSegments = new List<Tuple<TKey, TKey, TItem>>();

                while (true)
                {
                    // Find the points that only have one segment pointing to them
                    var pointCounts = UtilityCore.Iterate(segmentList.Select(o => o.Item1), segmentList.Select(o => o.Item2)).
                        GroupBy(compareKey).
                        Select(o => new { Point = o.Key, Count = o.Count() }).
                        Where(o => o.Count == 1).
                        ToArray();

                    if (pointCounts.Length == 0)
                    {
                        break;
                    }

                    TKey pointToFind = pointCounts[0].Point;

                    // Remove the segment that contains this point
                    bool foundIt = false;
                    for (int cntr = 0; cntr < segmentList.Count; cntr++)
                    {
                        if (compareKey(segmentList[cntr].Item1, pointToFind) || compareKey(segmentList[cntr].Item2, pointToFind))
                        {
                            tailSegments.Add(segmentList[cntr]);
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

                tails = GetChains(tailSegments, compareKey);

                reducedSegments = segmentList.ToArray();
            }


            #region get chains

            //private static List<Chain> GetChains(Tuple<int, int>[] segments)
            //{
            //    // Find the points that have more than two segments pointing to them
            //    var pointCounts = UtilityCore.Iterate(segments.Select(o => o.Item1), segments.Select(o => o.Item2)).
            //        GroupBy(o => o).
            //        Select(o => Tuple.Create(o.Key, o.Count())).
            //        Where(o => o.Item2 > 2).
            //        ToArray();

            //    List<Tuple<int, int>> segmentList = segments.ToList();

            //    if (pointCounts.Length == 0)
            //    {
            //        // There are no junctions, so just return the unique loops
            //        return GetChainsSprtLoops(segmentList);
            //    }

            //    List<Chain> retVal = new List<Chain>();

            //    retVal.AddRange(GetChainsSprtFragments(segmentList, pointCounts));

            //    if (segmentList.Count > 0)
            //    {
            //        retVal.AddRange(GetChainsSprtLoops(segmentList));
            //    }

            //    return retVal;
            //}
            //private static List<Chain> GetChainsSprtLoops(List<Tuple<int, int>> segments)
            //{
            //    List<Chain> retVal = new List<Chain>();

            //    int[] ends = new int[0];

            //    while (segments.Count > 0)
            //    {
            //        Chain polygon = GetChainsSprtSingle(segments, segments[0].Item1, ends);

            //        retVal.Add(polygon);
            //    }

            //    // Exit Function
            //    return retVal;
            //}
            //private static Chain[] GetChainsSprtFragments(List<Tuple<int, int>> segments, Tuple<int, int>[] pointCounts)
            //{
            //    List<int> ends = pointCounts.SelectMany(o => Enumerable.Repeat(o.Item1, o.Item2)).ToList();

            //    List<Chain> retVal = new List<Chain>();

            //    while (ends.Count > 0)
            //    {
            //        Chain chain = GetChainsSprtSingle(segments, ends[0], ends.Where(o => o != ends[0]).ToArray());

            //        ends.Remove(chain.Points[0]);
            //        ends.Remove(chain.Points[chain.Points.Length - 1]);

            //        retVal.Add(chain);
            //    }

            //    return retVal.ToArray();
            //}
            //private static Chain GetChainsSprtSingle(List<Tuple<int, int>> segments, int start, int[] ends)
            //{
            //    List<int> retVal = new List<int>();

            //    #region Find the start

            //    int currentPoint = -1;

            //    for (int cntr = 0; cntr < segments.Count; cntr++)
            //    {
            //        if (segments[cntr].Item1 == start)
            //        {
            //            retVal.Add(segments[cntr].Item1);
            //            currentPoint = segments[cntr].Item2;
            //            segments.RemoveAt(cntr);
            //            break;
            //        }
            //        else if (segments[cntr].Item2 == start)
            //        {
            //            retVal.Add(segments[cntr].Item2);
            //            currentPoint = segments[cntr].Item1;
            //            segments.RemoveAt(cntr);
            //            break;
            //        }
            //    }

            //    if (currentPoint < 0)
            //    {
            //        int[] points = UtilityCore.Iterate(segments.Select(o => o.Item1), segments.Select(o => o.Item2)).Distinct().ToArray();
            //        throw new ApplicationException(string.Format("Didn't find the start: {0}, in: {1}", start.ToString(), string.Join(" ", points)));
            //    }

            //    #endregion

            //    // Stitch the segments together into a polygon
            //    while (segments.Count > 0)
            //    {
            //        if (ends.Contains(currentPoint))
            //        {
            //            retVal.Add(currentPoint);
            //            return new Chain(retVal.ToArray(), false);
            //        }

            //        var match = FindAndRemoveMatchingSegment(segments, currentPoint);
            //        if (match == null)
            //        {
            //            // The rest of the segments belong to a polygon independent of the one that is currently being built (a hole)
            //            break;
            //        }

            //        retVal.Add(match.Item1);
            //        currentPoint = match.Item2;
            //    }

            //    if (ends.Length == 0)
            //    {
            //        if (retVal[0] != currentPoint)
            //        {
            //            throw new ApplicationException("There are gaps in this polygon");
            //        }

            //        if (retVal.Count < 3)
            //        {
            //            return null;
            //        }

            //        return new Chain(retVal.ToArray(), true);
            //    }
            //    else
            //    {
            //        if (!ends.Contains(currentPoint))
            //        {
            //            throw new ApplicationException("Didn't find the end point");
            //        }

            //        retVal.Add(currentPoint);
            //        return new Chain(retVal.ToArray(), false);
            //    }
            //}

            #endregion

            #region get junctions

            //private static List<Junction> GetJunctions(IEnumerable<Chain> chains)
            //{
            //    SortedList<Tuple<int, int>, List<Chain>> junctions = new SortedList<Tuple<int, int>, List<Chain>>();

            //    foreach (Chain openChain in chains.Where(o => !o.IsClosed))
            //    {
            //        // Get the from/to points
            //        Tuple<int, int> fromTo = Tuple.Create(openChain.Points[0], openChain.Points[openChain.Points.Length - 1]);

            //        // Make sure from is less than to (that way all the chains point the same direction)
            //        Chain chain = openChain;
            //        if (fromTo.Item2 < fromTo.Item1)
            //        {
            //            // Reverse the chain
            //            chain = new Chain(chain.Points.Reverse().ToArray(), false);
            //            fromTo = Tuple.Create(fromTo.Item2, fromTo.Item1);
            //        }

            //        // Add this to a junction
            //        if (!junctions.ContainsKey(fromTo))
            //        {
            //            junctions.Add(fromTo, new List<Chain>());
            //        }

            //        junctions[fromTo].Add(chain);
            //    }

            //    // Finalize it
            //    List<Junction> retVal = new List<Junction>();

            //    foreach (var point in junctions.Keys)
            //    {
            //        retVal.Add(new Junction(point.Item1, point.Item2, junctions[point].ToList()));
            //    }

            //    return retVal;
            //}

            #endregion


            #region other get chains

            //TODO: Make sure that 8 becomes two loops

            ///// <summary>
            ///// This converts the set of segments into chains (good for making polygons out of line segments)
            ///// </summary>
            ///// <remarks>
            ///// NOTE: When there is a spoke (think of an octopus), individual chains are found and removed.  There could be multiple ways
            ///// those chains chains could be selected, this method arbitrarily chooses one.  In other words:
            /////     >---
            /////     could become
            /////     \---    and    /
            /////     or
            /////     /---    and    \
            ///// This method will only return one of those possibilities
            ///// </remarks>
            ///// <returns>
            ///// Item1=A chain or loop of items
            ///// Item2=True: Loop, False: Chain
            ///// </returns>
            //private static Tuple<TItem[], bool>[] GetChains<TKey, TItem>(IEnumerable<Tuple<TKey, TKey, TItem>> segments, Func<TKey, TKey, bool> compareKey, Func<TItem, TItem, bool> compareItem) where TKey : struct
            //{
            //    // Convert the segments into chains
            //    List<Tuple<TKey, TKey, TItem>[]> chains = segments.
            //        Select(o => new[] { o }).
            //        ToList();

            //    // Keep trying to merge the chains until no more merges are possible
            //    while (true)
            //    {
            //        #region Merge pass

            //        if (chains.Count == 1) break;

            //        bool hadJoin = false;

            //        for (int outer = 0; outer < chains.Count - 1; outer++)
            //        {
            //            for (int inner = outer + 1; inner < chains.Count; inner++)
            //            {
            //                // See if these two can be merged
            //                Tuple<TKey, TKey, TItem>[] newChain = TryJoinChains(chains[outer], chains[inner], compareKey, compareItem);

            //                if (newChain != null)
            //                {
            //                    // Swap the sub chains with the new combined one
            //                    chains.RemoveAt(inner);
            //                    chains.RemoveAt(outer);

            //                    chains.Add(newChain);

            //                    hadJoin = true;
            //                    break;
            //                }
            //            }

            //            if (hadJoin) break;
            //        }

            //        if (!hadJoin) break;        // compared all the mini chains, and there were no merges.  Quit looking

            //        #endregion
            //    }

            //    #region Detect loops

            //    List<Tuple<T[], bool>> retVal = new List<Tuple<T[], bool>>();

            //    foreach (T[] chain in chains)
            //    {
            //        if (compare(chain[0], chain[chain.Length - 1]))
            //        {
            //            T[] loop = chain.Skip(1).ToArray();
            //            retVal.Add(Tuple.Create(loop, true));
            //        }
            //        else
            //        {
            //            retVal.Add(Tuple.Create(chain, false));
            //        }
            //    }

            //    #endregion

            //    return retVal.ToArray();
            //}

            //private static Tuple<TKey, TKey, TItem>[] TryJoinChains(Tuple<TKey, TKey, TItem>[] tuple1, Tuple<TKey, TKey, TItem>[] tuple2, Func<TKey, TKey, bool> compareKey, Func<TItem, TItem, bool> compareItem)
            //{
            //}

            //private static T[] TryJoinChains<T>(T[] chain1, T[] chain2, Func<T, T, bool> compare)
            //{
            //    if (compare(chain1[0], chain2[0]))
            //    {
            //        return UtilityCore.Iterate(chain1.Reverse<T>(), chain2.Skip(1)).ToArray();
            //    }
            //    else if (compare(chain1[chain1.Length - 1], chain2[0]))
            //    {
            //        return UtilityCore.Iterate(chain1, chain2.Skip(1)).ToArray();
            //    }
            //    else if (compare(chain1[0], chain2[chain2.Length - 1]))
            //    {
            //        return UtilityCore.Iterate(chain2, chain1.Skip(1)).ToArray();
            //    }
            //    else if (compare(chain1[chain1.Length - 1], chain2[chain2.Length - 1]))
            //    {
            //        return UtilityCore.Iterate(chain2, chain1.Reverse<T>().Skip(1)).ToArray();
            //    }
            //    else
            //    {
            //        return null;
            //    }
            //}

            #endregion
            #region other get chains

            private static Tuple<TItem[], bool>[] GetChains<TKey, TItem>(IEnumerable<Tuple<TKey, TKey, TItem>> segments, Func<TKey, TKey, bool> compareKey) where TKey : struct
            {
                var retVal = new List<Tuple<TItem[], bool>>();

                var segmentList = segments.ToList();

                Tuple<TItem[], bool> current;

                while (ExtractChain(out current, segmentList, compareKey))
                {
                    retVal.Add(current);
                }

                #region asserts
#if DEBUG
                if (segmentList.Count > 0)
                {
                    throw new ApplicationException("segmentList should be empty at this point");
                }
#endif
                #endregion

                return retVal.ToArray();
            }

            private static bool ExtractChain<TKey, TItem>(out Tuple<TItem[], bool> chain, List<Tuple<TKey, TKey, TItem>> segmentList, Func<TKey, TKey, bool> compareKey) where TKey : struct
            {
                //TODO: this   Q   needs to be a chain   \   and a loop   O   not a single loop chain

                if (segmentList.Count == 0)
                {
                    chain = null;
                    return false;
                }

                List<Tuple<TKey, TKey, TItem>> retVal = new List<Tuple<TKey, TKey, TItem>>();
                List<TKey> usedKeys = new List<TKey>();

                // Pop the first segment from the list
                retVal.Add(segmentList[0]);
                segmentList.RemoveAt(0);

                bool? isLoop = null;

                while (true)
                {
                    if (retVal.Count > 2 && GetCommonKey(retVal[0], retVal[retVal.Count - 1], compareKey) != null)
                    {
                        isLoop = true;
                        break;
                    }

                    if (!TryExtractNextSegment(retVal, usedKeys, segmentList, compareKey))
                    {
                        isLoop = false;
                        break;
                    }
                }

                if (isLoop == null)
                {
                    throw new ApplicationException("The decision whether the return is a loop or not should have been made by now");
                }

                chain = Tuple.Create(retVal.Select(o => o.Item3).ToArray(), isLoop.Value);
                return true;
            }

            private static bool TryExtractNextSegment<TKey, TItem>(List<Tuple<TKey, TKey, TItem>> retVal, List<TKey> usedKeys, List<Tuple<TKey, TKey, TItem>> segmentList, Func<TKey, TKey, bool> compareKey) where TKey : struct
            {
                // Get the key from retVal[len-1] that isn't in common with retVal[len - 2] (if retVal is only one segment, then return Item2)
                foreach (TKey key in GetNextKeys(retVal, compareKey))
                {
                    if (usedKeys.Contains(key, compareKey))
                    {
                        // The next key to search for is already somewhere in the chain being built up.  Quit searching
                        return false;
                    }

                    // Find a segment that has the exposed key
                    for (int cntr = 0; cntr < segmentList.Count; cntr++)
                    {
                        if (compareKey(segmentList[cntr].Item1, key) || compareKey(segmentList[cntr].Item2, key))
                        {
                            if (retVal.Count == 1)
                            {
                                // Waiting until now to add these so the above contains check doesn't fail
                                usedKeys.Add(retVal[0].Item1);
                                usedKeys.Add(retVal[0].Item2);
                            }
                            else
                            {
                                usedKeys.Add(key);
                            }

                            retVal.Add(segmentList[cntr]);
                            segmentList.RemoveAt(cntr);
                            return true;
                        }
                    }
                }

                return false;
            }

            private static TKey? GetCommonKey<TKey, TItem>(Tuple<TKey, TKey, TItem> segment1, Tuple<TKey, TKey, TItem> segment2, Func<TKey, TKey, bool> compareKey) where TKey : struct
            {
                if (compareKey(segment1.Item1, segment2.Item1))
                {
                    return segment1.Item1;
                }
                else if (compareKey(segment1.Item1, segment2.Item2))
                {
                    return segment1.Item1;
                }
                else if (compareKey(segment1.Item2, segment2.Item1))
                {
                    return segment1.Item2;
                }
                else if (compareKey(segment1.Item2, segment2.Item2))
                {
                    return segment1.Item2;
                }
                else
                {
                    return null;
                }
            }

            private static TKey[] GetNextKeys<TKey, TItem>(List<Tuple<TKey, TKey, TItem>> segments, Func<TKey, TKey, bool> compareKey) where TKey : struct
            {
                if (segments.Count == 0)
                {
                    throw new ArgumentException("The list passed in can't be empty");
                }
                else if (segments.Count == 1)
                {
                    //NOTE: Need to return both, because one direction may have no connections
                    return new[] { segments[0].Item1, segments[0].Item2 };
                }

                var seg1 = segments[segments.Count - 2];
                var seg2 = segments[segments.Count - 1];

                if (compareKey(seg2.Item1, seg1.Item1) || compareKey(seg2.Item1, seg1.Item2))
                {
                    return new[] { seg2.Item2 };      // returning the uncommon one
                }
                else if (compareKey(seg2.Item2, seg1.Item1) || compareKey(seg2.Item2, seg1.Item2))
                {
                    return new[] { seg2.Item1 };
                }

                throw new ApplicationException("The last two segments in the list don't have a common key");
            }

            #endregion
        }

        #endregion
        #region Class: GetChains2D_1

        private static class GetChains2D_1
        {
            #region Class: EventPoint

            private class EventPoint
            {
                public EventPoint(Point point, Tuple<Point, Point> segment, bool? isLeftPoint)
                {
                    this.Point = point;
                    this.Segment = segment;
                    this.IsLeftPoint = isLeftPoint;
                }

                public readonly Point Point;
                public readonly Tuple<Point, Point> Segment;
                public readonly bool? IsLeftPoint;      // null means the point was generated from an intersection
            }

            #endregion

            //Bentley–Ottmann algorithm
            //http://geomalgorithms.com/a09-_intersect-3.html
            //http://stackoverflow.com/questions/4407493/existing-bentley-ottmann-algorithm-implementation
            public static Point[][] GetSubPolygons(Tuple<Point, Point>[] segments, bool couldEdgesOverlap)
            {
                if (segments == null)
                {
                    return null;
                }
                else if (segments.Length == 0)
                {
                    return new[] { new Point[0] };
                }

                // Initialize event queue EQ = all segment endpoints;
                // Sort EQ by increasing x and y;
                List<EventPoint> eventQueue = segments.
                    SelectMany(o => new[]
                    {
                        new EventPoint(o.Item1, o, true),
                        new EventPoint(o.Item2, o, false),
                    }).
                    OrderBy(o => o.Point.X).
                    ThenBy(o => o.Point.Y).
                    ToList();

                // Initialize sweep line SL to be empty;
                var sweepLine = new List<Tuple<Point, Point>>();

                // Initialize output intersection list IL to be empty;
                List<Point> retVal = new List<Point>();

                //While (EQ is nonempty) {
                while (eventQueue.Count > 0)
                {
                    //Let E = the next event from EQ;
                    EventPoint e = eventQueue[0];

                    if (e.IsLeftPoint == null)
                    {
                        #region Inserted Point

                        //Add E’s intersect point to the output list IL;
                        //NOTE: Only intersections are stored in the return, because this method only worries about polygons (tails sticking out the side are thrown out)
                        //retVal.Add(e.Point);

                        //Let segE1 above segE2 be E's intersecting segments in SL;
                        //Swap their positions so that segE2 is now above segE1;
                        //Let segA = the segment above segE2 in SL;
                        //Let segB = the segment below segE1 in SL;
                        //If (I = Intersect(segE2 with segA) exists)
                        //    If (I is not in EQ already) 
                        //        Insert I into EQ;
                        //If (I = Intersect(segE1 with segB) exists)
                        //    If (I is not in EQ already) 
                        //        Insert I into EQ;

                        #endregion
                    }
                    else if (e.IsLeftPoint.Value)
                    {
                        #region Orig Left

                        int sweepIndex = AddToSweepLine(sweepLine, e.Segment);

                        var segA = GetAbove(sweepLine, sweepIndex);
                        Point? intersect = Intersect(e.Segment, segA);
                        if (intersect != null)
                        {
                            AddToEventQueue(eventQueue, intersect.Value, e.Segment, segA);
                        }

                        var segB = GetBelow(sweepLine, sweepIndex);
                        intersect = Intersect(e.Segment, segB);
                        if (intersect != null)
                        {
                            AddToEventQueue(eventQueue, intersect.Value, e.Segment, segA);
                        }

                        #endregion
                    }
                    else
                    {
                        #region Orig Right

                        int sweepIndex = sweepLine.IndexOf(e.Segment, (o, p) => Math2D.IsNearValue(o.Item1, p.Item1) && Math2D.IsNearValue(o.Item2, p.Item2));

                        var segA = GetAbove(sweepLine, sweepIndex);
                        var segB = GetBelow(sweepLine, sweepIndex);

                        sweepLine.RemoveAt(sweepIndex);

                        Point? intersect = Intersect(segA, segB);

                        if (intersect != null)
                        {
                            int eqIndex = FindIndex(eventQueue, intersect.Value);

                            if (eqIndex < 0)
                            {
                                AddToEventQueue(eventQueue, intersect.Value, segA, segB);
                            }
                        }

                        #endregion
                    }

                    eventQueue.RemoveAt(FindIndex(eventQueue, e.Point));
                }

                //return IL;
                return null;
            }

            private static int AddToSweepLine(List<Tuple<Point, Point>> sweepLine, Tuple<Point, Point> segment)
            {
                //TODO: May need insert in the middle
                sweepLine.Add(segment);
                return sweepLine.Count - 1;
            }

            private static Tuple<Point, Point> GetAbove(List<Tuple<Point, Point>> sweepLine, int index)
            {
                if (index == 0)
                {
                    return null;
                }
                else
                {
                    return sweepLine[index - 1];
                }
            }
            private static Tuple<Point, Point> GetBelow(List<Tuple<Point, Point>> sweepLine, int index)
            {
                if (index == sweepLine.Count - 1)
                {
                    return null;
                }
                else
                {
                    return sweepLine[index + 1];
                }
            }

            private static int AddToEventQueue(List<EventPoint> eventQueue, Point point, Tuple<Point, Point> orig1, Tuple<Point, Point> orig2)
            {
                // Remove the two original segments, and create 4 new segments.  Store them in order

                throw new ApplicationException("finish this");
            }

            private static Point? Intersect(Tuple<Point, Point> segment1, Tuple<Point, Point> segment2)
            {
                if (segment1 == null || segment2 == null)
                {
                    return null;
                }

                //TODO: See if they intersect
                return null;
            }

            private static int FindIndex(List<EventPoint> eventQueue, Point point)
            {
                for (int cntr = 0; cntr < eventQueue.Count; cntr++)
                {
                    if (Math2D.IsNearValue(eventQueue[cntr].Point, point))
                    {
                        return cntr;
                    }
                }

                return -1;
            }
        }

        #endregion
        #region Class: GetChains2D_2

        private static class GetChains2D_2
        {
            #region Class: SweepPoly

            private class SweepPoly
            {
                private readonly List<int> _chain = new List<int>();

                public SweepPoly(Tuple<int, int> segment)
                {
                    _chain.Add(segment.Item1);
                    _chain.Add(segment.Item2);

                    this.IsComplete = false;
                }

                public bool IsComplete
                {
                    get;
                    private set;
                }

                public bool TryAdd(Tuple<int, int> segment)
                {
                    if (this.IsComplete)
                    {
                        throw new InvalidOperationException("Can't try to add a segment to a completed loop");
                    }

                    int first = _chain[0];
                    int last = _chain[_chain.Count - 1];

                    if ((segment.Item1 == first && segment.Item2 == last) || (segment.Item2 == first && segment.Item1 == last))
                    {
                        this.IsComplete = true;
                    }
                    else if (segment.Item1 == last)
                    {
                        _chain.Add(segment.Item2);
                    }
                    else if (segment.Item2 == last)
                    {
                        _chain.Add(segment.Item1);
                    }
                    else if (segment.Item1 == first)
                    {
                        _chain.Insert(0, segment.Item2);
                    }
                    else if (segment.Item2 == first)
                    {
                        _chain.Insert(0, segment.Item1);
                    }
                    else
                    {
                        // This is the only case that returns false
                        return false;
                    }

                    return true;
                }
                public bool WillClose(Tuple<int, int> segment)
                {
                    if (this.IsComplete)
                    {
                        throw new InvalidOperationException("Can't try to add a segment to a completed loop");
                    }

                    int first = _chain[0];
                    int last = _chain[_chain.Count - 1];

                    return (segment.Item1 == first && segment.Item2 == last) || (segment.Item2 == first && segment.Item1 == last);
                }

                public int[] GetChain()
                {
                    return _chain.ToArray();
                }
            }

            #endregion

            public static Tuple<int[][], Point[]> GetSubPolygons(Tuple<Point, Point>[] segments)
            {
                throw new ApplicationException("finish this");
            }
            /// <summary>
            /// This will find polygons within the segments.  It finds the most granular polygons (no unions)
            /// </summary>
            /// <remarks>
            /// http://programmers.stackexchange.com/questions/216255/find-all-lines-segments-intersections
            /// </remarks>
            public static Tuple<int[][], Point[]> GetSubPolygons(Tuple<int, int>[] segments, Point[] points, bool shouldDedupePoints = false, bool couldSegmentsIntersect = true)
            {
                if (segments == null)
                {
                    return null;
                }
                else if (segments.Length == 0)
                {
                    return Tuple.Create(new int[0][], new Point[0]);
                }

                //TODO: Dedupe points
                //TODO: Intersect segments
                //TODO: Remove tails


                return Tuple.Create(GetSubPolygons_Clean(segments, points), points);
            }

            #region Private Methods

            public static int[][] GetSubPolygons_Clean(Tuple<int, int>[] segments, Point[] points)
            {
                // Sort the points
                var sortedPoints = points.
                    Select((o, i) => new { Point = o, Index = i }).
                    OrderBy(o => o.Point.X).
                    ThenBy(o => o.Point.Y).
                    ToArray();

                // The angles are needed to know which segments that share a point are next to which (imagine 3 segments off of a point.
                // The angles are used to know what order to put them in)
                var angles = GetSegmentAngles(segments, points);

                List<SweepPoly> sweepLine = new List<SweepPoly>();
                List<int[]> retVal = new List<int[]>();

                bool[] segmentUsed = new bool[segments.Length];

                for (int ptCntr = 0; ptCntr < sortedPoints.Length; ptCntr++)
                {
                    // Find all segments that contain this point
                    var matchingSegments = FindSegments(sortedPoints[ptCntr].Index, segments, segmentUsed, angles);

                    for (int segCntr = 0; segCntr < matchingSegments.Length; segCntr++)
                    {
                        if (matchingSegments[segCntr].Item2)
                        {
                            // This segment has already been added (the reason for keeping it in the for loop is so shouldAddTwice will be accurate)
                            continue;
                        }

                        //NOTE: Interior segments need to be duplicated between the poly above and poly below
                        bool shouldAddTwice = segCntr > 0 && segCntr < matchingSegments.Length - 1;

                        AddToSweepLine(matchingSegments[segCntr].Item1, sweepLine, retVal, shouldAddTwice);
                    }
                }

                if (sweepLine.Count > 0)
                {
                    //throw new ApplicationException("There are still unfinished chains");
                    int sixty = 2;
                }

                return retVal.ToArray();
            }

            private static Tuple<Tuple<int, int>, bool>[] FindSegments_ORIG(int point, Tuple<int, int>[] segments, bool[] segmentsUsed, Tuple<int, int>[] sortedIndices)
            {
                var retVal = new List<Tuple<Tuple<int, int>, bool>>();

                // Find all the segments that contain the point passed in
                //NOTE: All segments in retVal will have the point as Item1
                for (int cntr = 0; cntr < segments.Length; cntr++)
                {
                    if (segments[cntr].Item1 == point)
                    {
                        retVal.Add(Tuple.Create(segments[cntr], segmentsUsed[cntr]));
                        segmentsUsed[cntr] = true;
                    }
                    else if (segments[cntr].Item2 == point)
                    {
                        // Reverse the order of the segment
                        retVal.Add(Tuple.Create(Tuple.Create(segments[cntr].Item2, segments[cntr].Item1), segmentsUsed[cntr]));
                        segmentsUsed[cntr] = true;
                    }
                }

                // Make the list the same order as sortedIndices
                return retVal.
                    Select(o => new
                    {
                        Segment = o,
                        Order = sortedIndices.
                            First(p => p.Item1 == o.Item1.Item2).     // Use the second point in the segment (o.Item2) to find the entry in sortedIndices (p.Item1)
                            Item2       //sortedIndices.Item2 is the sort order
                    }).
                    OrderBy(o => o.Order).
                    Select(o => o.Segment).
                    ToArray();
            }
            private static Tuple<Tuple<int, int>, bool>[] FindSegments(int point, Tuple<int, int>[] segments, bool[] segmentsUsed, Tuple<Tuple<int, int>, double, double>[] angles)
            {
                var retVal = new List<Tuple<Tuple<int, int>, bool>>();

                // Find all the segments that contain the point passed in
                //NOTE: All segments in retVal will have the point as Item1
                for (int cntr = 0; cntr < segments.Length; cntr++)
                {
                    if (segments[cntr].Item1 == point)
                    {
                        retVal.Add(Tuple.Create(segments[cntr], segmentsUsed[cntr]));
                        segmentsUsed[cntr] = true;
                    }
                    else if (segments[cntr].Item2 == point)
                    {
                        // Reverse the order of the segment
                        retVal.Add(Tuple.Create(Tuple.Create(segments[cntr].Item2, segments[cntr].Item1), segmentsUsed[cntr]));
                        segmentsUsed[cntr] = true;
                    }
                }

                // Sort them by angle
                return retVal.
                    Select(o => new
                    {
                        Segment = o,
                        Angle = GetAngle(o.Item1, angles)
                    }).
                    OrderBy(o => o.Angle).
                    Select(o => o.Segment).
                    ToArray();
            }

            private static void AddToSweepLine_ORIG(Tuple<int, int> segment, List<SweepPoly> sweepLine, List<int[]> finishedLoops, bool addTwice)
            {
                int index = 0;
                int addCount = 0;

                while (index < sweepLine.Count)
                {

                    //TODO: TryAdd is too greedy
                    //      Instead, get all that can take it, and give to the best ---- not sure about this approach
                    //      Instead, find any polys that this will finish.  If found, add it.  Then go back and add to any polys that could take it (assuming the add count hasn't been exceeded)

                    if (sweepLine[index].TryAdd(segment))
                    {
                        if (sweepLine[index].IsComplete)
                        {
                            finishedLoops.Add(sweepLine[index].GetChain());
                            sweepLine.RemoveAt(index);
                        }

                        addCount++;
                        if (addCount > (addTwice ? 1 : 0))
                        {
                            return;
                        }
                    }

                    index++;
                }

                sweepLine.Add(new SweepPoly(segment));

                if (addTwice && addCount < 1)
                {
                    throw new ApplicationException("Should have added twice");
                }
            }
            private static void AddToSweepLine(Tuple<int, int> segment, List<SweepPoly> sweepLine, List<int[]> finishedLoops, bool addTwice)
            {
                int addCount = 0;

                // Try to close figures
                int index = 0;
                while (index < sweepLine.Count)
                {
                    if (sweepLine[index].WillClose(segment))
                    {
                        sweepLine[index].TryAdd(segment);       // this will close it, so commit the add
                        finishedLoops.Add(sweepLine[index].GetChain());
                        sweepLine.RemoveAt(index);

                        addCount++;
                        if (addCount > (addTwice ? 1 : 0))
                        {
                            return;
                        }
                    }

                    index++;
                }

                // Standard add
                index = 0;
                while (index < sweepLine.Count)
                {
                    if (sweepLine[index].TryAdd(segment))
                    {
                        if (sweepLine[index].IsComplete)
                        {
                            finishedLoops.Add(sweepLine[index].GetChain());
                            sweepLine.RemoveAt(index);
                        }

                        addCount++;
                        if (addCount > (addTwice ? 1 : 0))
                        {
                            return;
                        }
                    }

                    index++;
                }

                sweepLine.Add(new SweepPoly(segment));
                addCount++;

                if (addTwice && addCount < 2)
                {
                    throw new ApplicationException("Should have added twice");
                }
            }

            /// <summary>
            /// This returns the angle to -1,0
            /// </summary>
            /// <returns>
            /// Item1=Angle when going from segment[n].Item1 to Item2
            /// Item2=Angle when going from segment[n].Item2 to Item1
            /// </returns>
            private static Tuple<Tuple<int, int>, double, double>[] GetSegmentAngles(Tuple<int, int>[] segments, Point[] points)
            {
                var retVal = new Tuple<Tuple<int, int>, double, double>[segments.Length];

                Vector vector = new Vector(-1, 0);

                for (int cntr = 0; cntr < segments.Length; cntr++)
                {
                    double angle = Vector.AngleBetween(vector, points[segments[cntr].Item2] - points[segments[cntr].Item1]);

                    if (angle < 0)
                    {
                        angle += 360d;
                    }

                    double opposite = angle + 180;
                    if (opposite >= 360d)
                    {
                        opposite -= 360d;
                    }

                    retVal[cntr] = Tuple.Create(segments[cntr], angle, opposite);
                }

                return retVal;
            }

            private static double GetAngle(Tuple<int, int> segment, Tuple<Tuple<int, int>, double, double>[] angles)
            {
                // Find this segment in the angles passed in
                foreach (var angle in angles)
                {
                    if (angle.Item1.Item1 == segment.Item1 && angle.Item1.Item2 == segment.Item2)
                    {
                        return angle.Item2;
                    }
                    else if (angle.Item1.Item1 == segment.Item2 && angle.Item1.Item2 == segment.Item1)
                    {
                        return angle.Item3;
                    }
                }

                throw new ArgumentException("Didn't find the segment");
            }

            #endregion

            //private static Point? GetIntersection(Tuple<Point, Point> segment1, Tuple<Point, Point> segment2)
            //{
            //    return Math2D.GetIntersection_LineSegment_LineSegment(segment1.Item1, segment1.Item2, segment2.Item1, segment2.Item2);
            //}
        }

        #endregion
        #region Class: GetChains2D_3

        private static class GetChains2D_3
        {
            public static Tuple<int[][], Point[]> GetSubPolygons(Tuple<int, int>[] segments, Point[] points, bool shouldDedupePoints = false, bool couldSegmentsIntersect = true)
            {
                if (segments == null)
                {
                    return null;
                }
                else if (segments.Length == 0)
                {
                    return Tuple.Create(new int[0][], new Point[0]);
                }

                //TODO: Dedupe points
                //TODO: Intersect segments
                //TODO: Remove tails


                return Tuple.Create(GetSubPolygons_Clean(segments, points), points);
            }

            #region Private Methods

            private static int[][] GetSubPolygons_Clean(Tuple<int, int>[] segments, Point[] points)
            {
                // Sort the points
                var sortedPoints = points.
                    Select((o, i) => new { Point = o, Index = i }).
                    OrderBy(o => o.Point.X).
                    ThenBy(o => o.Point.Y).
                    ToArray();

                //// Find the points that have more than two segments pointing to them
                //var pointCounts = UtilityCore.Iterate(segments.Select(o => o.Item1), segments.Select(o => o.Item2)).
                //    GroupBy(o => o).
                //    Select(o => Tuple.Create(o.Key, o.Count())).
                //    Where(o => o.Item2 > 2).
                //    ToArray();

                // Come up with chains and rings
                List<Chain> chains = GetChains(segments);
                List<Junction> junctions = GetJunctions(chains);
                chains = chains.Where(o => o.IsClosed).ToList();       // only keep the rings (the junctions now own the fragment chains)










                return new int[0][];
            }



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

                public override string ToString()
                {
                    const string DELIM = "       |       ";

                    StringBuilder retVal = new StringBuilder(100);

                    retVal.Append(this.IsClosed ? "closed" : "open");

                    retVal.Append(DELIM);

                    retVal.Append(string.Join(", ", this.Points));

                    return retVal.ToString();
                }
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

                public override string ToString()
                {
                    const string DELIM = "       |       ";

                    StringBuilder retVal = new StringBuilder(100);

                    retVal.Append(string.Format("{0} - {1}", this.FromPoint, this.ToPoint));

                    retVal.Append(DELIM);

                    //retVal.Append(string.Join(" ", this.Chains.Select(o => string.Format("{{0} {1}}", o.IsClosed ? "closed" : "open", string.Join(",", o.Points)))));
                    retVal.Append(string.Join(" ", this.Chains.Select(o => "(" + string.Join(",", o.Points) + ")")));

                    return retVal.ToString();
                }
            }

            #endregion


            #region get chains

            private static List<Chain> GetChains(Tuple<int, int>[] segments)
            {
                // Find the points that have more than two segments pointing to them
                var pointCounts = UtilityCore.Iterate(segments.Select(o => o.Item1), segments.Select(o => o.Item2)).
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
                    int[] points = UtilityCore.Iterate(segments.Select(o => o.Item1), segments.Select(o => o.Item2)).Distinct().ToArray();
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

            #endregion
            #region get junctions

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

            #endregion








            #endregion
        }

        #endregion

        #region Declaration Section

        private const double MAXRADIUS = 10d;
        private const double DOTRADIUS = .05d;
        private const double LINETHICKNESS = 2d;

        private ItemColors _colors = new ItemColors();
        private bool _isInitialized = false;

        /// <summary>
        /// This listens to the mouse/keyboard and controls the camera
        /// </summary>
        private TrackBallRoam _trackball = null;

        private List<ModelVisual3D> _visuals = new List<ModelVisual3D>();

        private string _prevAttempt6File = null;

        private Lazy<FontFamily> _font = new Lazy<FontFamily>(() => GetBestFont());

        private AsteroidShatter _shatter = null;
        private ITriangleIndexed[][] _multiHull = null;

        private readonly DispatcherTimer _timerExplodeShatterAsteroid;

        private double _explodePercent = 0;
        private bool _explodeIsUp = true;
        private DateTime _explodeNextTransition = DateTime.MinValue;
        private List<Tuple<TranslateTransform3D, Vector3D>> _explodingSubHulls = new List<Tuple<TranslateTransform3D, Vector3D>>();

        #endregion

        #region Constructor

        public PotatoWindow()
        {
            InitializeComponent();

            _timerExplodeShatterAsteroid = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(25),
                IsEnabled = false,
            };
            _timerExplodeShatterAsteroid.Tick += TimerExplodeShatterAsteroid_Tick;

            _isInitialized = true;
        }

        #endregion

        #region Event Listeners

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Camera Trackball
                _trackball = new TrackBallRoam(_camera);
                _trackball.EventSource = grdViewPort;		//NOTE:  If this control doesn't have a background color set, the trackball won't see events (I think transparent is ok, just not null)
                _trackball.AllowZoomOnMouseWheel = true;
                _trackball.Mappings.AddRange(TrackBallMapping.GetPrebuilt(TrackBallMapping.PrebuiltMapping.MouseComplete_NoLeft));
                //_trackball.GetOrbitRadius += new GetOrbitRadiusHandler(Trackball_GetOrbitRadius);

                //TODO:  Add a checkbox to make this conditional
                ScreenSpaceLines3D line = new ScreenSpaceLines3D(true);
                line.Thickness = 1d;
                line.Color = _colors.AxisZ;
                line.AddLine(new Point3D(0, 0, 0), new Point3D(0, 0, 12));

                _viewport.Children.Add(line);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Window_Closed(object sender, EventArgs e)
        {

        }

        private void TimerExplodeShatterAsteroid_Tick(object sender, EventArgs e)
        {
            const double INCREMENT = .03;

            try
            {
                if (_explodingSubHulls.Count == 0)
                    return;
                else if (DateTime.UtcNow < _explodeNextTransition)
                    return;

                //TODO: Increment/Decrement _explodePercent
                if (_explodeIsUp)
                    _explodePercent += INCREMENT;
                else
                    _explodePercent -= INCREMENT;

                if (_explodePercent < 0)
                {
                    _explodePercent = 0;
                    _explodeIsUp = true;
                    _explodeNextTransition = DateTime.UtcNow + TimeSpan.FromSeconds(2);
                }
                else if (_explodePercent > 1)
                {
                    _explodePercent = 1;
                    _explodeIsUp = false;
                    _explodeNextTransition = DateTime.UtcNow + TimeSpan.FromSeconds(3);
                }

                // Run the linear percent through a sine to get percent along the direction vector
                double finalPercent = Math.Cos(Math.PI * _explodePercent);
                finalPercent *= -.5;
                finalPercent += .5;

                foreach (var subHull in _explodingSubHulls)
                {
                    Vector3D offset = subHull.Item2 * finalPercent;

                    subHull.Item1.OffsetX = offset.X;
                    subHull.Item1.OffsetY = offset.Y;
                    subHull.Item1.OffsetZ = offset.Z;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void grdViewPort_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {









            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void trkNumPoints_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isInitialized)
            {
                return;
            }

            try
            {
                trkNumPoints.ToolTip = Convert.ToInt32(trkNumPoints.Value).ToString("N0");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void RadioRange_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_isInitialized)
                {
                    return;
                }

                //NOTE: If these change, the radio button tooltips also need to change
                if (radSmallRange.IsChecked.Value)
                {
                    trkNumPoints.Maximum = 100;
                }
                else if (radLargeRange.IsChecked.Value)
                {
                    trkNumPoints.Maximum = 2500;
                }
                else if (radHugeRange.IsChecked.Value)
                {
                    trkNumPoints.Maximum = 30000;
                }
                else if (radExtremeRange.IsChecked.Value)
                {
                    trkNumPoints.Maximum = 100000;
                }
                else
                {
                    MessageBox.Show("Unknown radio button checked", this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnPointCloudDisk_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RemoveCurrentBody();

                for (int cntr = 0; cntr < trkNumPoints.Value; cntr++)
                {
                    AddDot(Math3D.GetRandomVector_Circular(MAXRADIUS).ToPoint(), DOTRADIUS, _colors.MedSlate);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnPointCloudRingThick_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RemoveCurrentBody();

                for (int cntr = 0; cntr < trkNumPoints.Value; cntr++)
                {
                    AddDot(Math3D.GetRandomVector_Circular(MAXRADIUS * .7d, MAXRADIUS).ToPoint(), DOTRADIUS, _colors.MedSlate);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnPointCloudRing_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RemoveCurrentBody();

                for (int cntr = 0; cntr < trkNumPoints.Value; cntr++)
                {
                    AddDot(Math3D.GetRandomVector_Circular_Shell(MAXRADIUS).ToPoint(), DOTRADIUS, _colors.MedSlate);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnPointCloudSphere_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RemoveCurrentBody();

                for (int cntr = 0; cntr < trkNumPoints.Value; cntr++)
                {
                    AddDot(Math3D.GetRandomVector_Spherical(MAXRADIUS).ToPoint(), DOTRADIUS, _colors.MedSlate);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnPointCloudSphereShellThick_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RemoveCurrentBody();

                for (int cntr = 0; cntr < trkNumPoints.Value; cntr++)
                {
                    AddDot(Math3D.GetRandomVector_Spherical(.9d * MAXRADIUS, MAXRADIUS).ToPoint(), DOTRADIUS, _colors.MedSlate);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnPointCloudSphereShell_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RemoveCurrentBody();

                for (int cntr = 0; cntr < trkNumPoints.Value; cntr++)
                {
                    AddDot(Math3D.GetRandomVector_Spherical_Shell(MAXRADIUS).ToPoint(), DOTRADIUS, _colors.MedSlate);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnHullCircle2D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RemoveCurrentBody();

                List<Point3D> points = new List<Point3D>();

                double minRadius = StaticRandom.NextDouble() * MAXRADIUS;

                for (int cntr = 0; cntr < trkNumPoints.Value; cntr++)
                {
                    points.Add(Math3D.GetRandomVector_Circular(minRadius, MAXRADIUS).ToPoint());

                    if (chkDrawDots.IsChecked.Value)
                    {
                        AddDot(points[points.Count - 1], DOTRADIUS, _colors.MedSlate);
                    }
                }

                while (points.Count > 2)
                {
                    // Do a quickhull implementation
                    //int[] lines = QuickHull2a.GetQuickHull2D(points.ToArray());
                    //int[] lines = UtilityWPF.GetConvexHull2D(points.ToArray());
                    var result = Math2D.GetConvexHull(points.ToArray());

                    // Draw the lines
                    ScreenSpaceLines3D lineVisual = new ScreenSpaceLines3D(true);
                    lineVisual.Thickness = LINETHICKNESS;
                    lineVisual.Color = _colors.DarkSlate;

                    for (int cntr = 0; cntr < result.PerimiterLines.Length - 1; cntr++)
                    {
                        lineVisual.AddLine(points[result.PerimiterLines[cntr]], points[result.PerimiterLines[cntr + 1]]);
                    }

                    lineVisual.AddLine(points[result.PerimiterLines[result.PerimiterLines.Length - 1]], points[result.PerimiterLines[0]]);

                    _viewport.Children.Add(lineVisual);
                    _visuals.Add(lineVisual);

                    // Prep for the next run
                    if (!chkConcentricHulls.IsChecked.Value)
                    {
                        break;
                    }

                    int[] lines = result.PerimiterLines.ToArray();
                    Array.Sort(lines);
                    for (int cntr = lines.Length - 1; cntr >= 0; cntr--)		// going backward so the index stays lined up
                    {
                        points.RemoveAt(lines[cntr]);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }

            #region ORIG

            //try
            //{
            //    RemoveCurrentBody();

            //    List<Point3D> points = new List<Point3D>();

            //    for (int cntr = 0; cntr < trkNumPoints.Value; cntr++)
            //    {
            //        points.Add(Math3D.GetRandomVectorSpherical2D(_rand, .6d * MAXRADIUS, MAXRADIUS).ToPoint());

            //        if (chkDrawDots.IsChecked.Value)
            //        {
            //            AddDot(points[points.Count - 1], DOTRADIUS, _colors.MedSlate);
            //        }
            //    }

            //    while (points.Count > 2)
            //    {
            //        // Do a quickhull implementation
            //        List<Point3D> lines = QuickHull2.GetQuickHull2D(points);

            //        // Draw the lines
            //        ScreenSpaceLines3D lineVisual = new ScreenSpaceLines3D(true);
            //        lineVisual.Thickness = LINETHICKNESS;
            //        lineVisual.Color = _colors.DarkSlate;

            //        for (int cntr = 0; cntr < lines.Count - 1; cntr++)
            //        {
            //            lineVisual.AddLine(lines[cntr], lines[cntr + 1]);
            //        }

            //        lineVisual.AddLine(lines[lines.Count - 1], lines[0]);

            //        _viewport.Children.Add(lineVisual);
            //        _visuals.Add(lineVisual);

            //        // Prep for the next run
            //        if (!chkConcentricHulls.IsChecked.Value)
            //        {
            //            break;
            //        }

            //        foreach (Point3D point in lines)
            //        {
            //            points.Remove(point);
            //        }
            //    }
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            //}

            #endregion
        }
        private void btnHullSphere3D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RemoveCurrentBody();

                List<Point3D> points = new List<Point3D>();

                double minRadius = StaticRandom.NextDouble() * MAXRADIUS;

                for (int cntr = 0; cntr < trkNumPoints.Value; cntr++)
                {
                    points.Add(Math3D.GetRandomVector_Spherical(minRadius, MAXRADIUS).ToPoint());

                    if (chkDrawDots.IsChecked.Value)
                    {
                        AddDot(points[points.Count - 1], DOTRADIUS, _colors.MedSlate);
                    }
                }

                List<TriangleIndexed[]> hulls = new List<TriangleIndexed[]>();
                while (points.Count > 3)
                {
                    //TriangleIndexed[] hull = QuickHull6.GetQuickHull(points.ToArray());
                    TriangleIndexed[] hull = Math3D.GetConvexHull(points.ToArray());
                    if (hull == null)
                    {
                        break;
                    }

                    hulls.Add(hull);

                    // Prep for the next run
                    if (!chkConcentricHulls.IsChecked.Value)
                    {
                        break;
                    }

                    foreach (TriangleIndexed triangle in hull)
                    {
                        points.Remove(triangle.Point0);
                        points.Remove(triangle.Point1);
                        points.Remove(triangle.Point2);
                    }
                }

                // They must be added in reverse order so that the outermost one is added last (or the transparency fails)
                for (int cntr = hulls.Count - 1; cntr >= 0; cntr--)
                {
                    AddHull(hulls[cntr], true, chkDrawLines.IsChecked.Value, chkDrawNormals.IsChecked.Value, false, chkNearlyTransparent.IsChecked.Value, chkSoftFaces.IsChecked.Value);
                }

                //TriangleIndexed[] hull = QuickHull6.GetQuickHull(points.ToArray());
                //AddHull(hull, chkDrawLines.IsChecked.Value, chkDrawNormals.IsChecked.Value, false, chkNearlyTransparent.IsChecked.Value);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnPointsOnHullTriangle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RemoveCurrentBody();

                // Make a random triangle
                Point3D[] points = new Point3D[3];
                points[0] = Math3D.GetRandomVector_Spherical(MAXRADIUS).ToPoint();
                points[1] = Math3D.GetRandomVector_Spherical(MAXRADIUS).ToPoint();
                points[2] = Math3D.GetRandomVector_Spherical(MAXRADIUS).ToPoint();
                TriangleIndexed triangle = new TriangleIndexed(0, 1, 2, points);

                // Create random points within that triangle
                for (int cntr = 0; cntr < trkNumPoints.Value; cntr++)
                {
                    //Point3D insidePoint = Math3D.GetRandomPointInTriangle(_rand, triangle.Point0, triangle.Point1, triangle.Point2);
                    Point3D insidePoint = Math3D.GetRandomPoint_InTriangle(triangle);
                    AddDot(insidePoint, DOTRADIUS, _colors.MedSlate);
                }

                // Semitransparent must be added last
                AddHull(new TriangleIndexed[] { triangle }, chkPointsDrawFaces.IsChecked.Value, chkPointsDrawLines.IsChecked.Value, false, false, true, false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnPointsOnHullSeveralTriangles_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RemoveCurrentBody();

                int numTriangles = StaticRandom.Next(2, 10);
                Point3D[] allPoints = new Point3D[numTriangles * 3];
                TriangleIndexed[] triangles = new TriangleIndexed[numTriangles];

                // Make some random triangles
                for (int cntr = 0; cntr < triangles.Length; cntr++)
                {
                    allPoints[cntr * 3] = Math3D.GetRandomVector_Spherical(MAXRADIUS).ToPoint();
                    allPoints[(cntr * 3) + 1] = Math3D.GetRandomVector_Spherical(MAXRADIUS).ToPoint();
                    allPoints[(cntr * 3) + 2] = Math3D.GetRandomVector_Spherical(MAXRADIUS).ToPoint();

                    triangles[cntr] = new TriangleIndexed(cntr * 3, (cntr * 3) + 1, (cntr * 3) + 2, allPoints);
                }

                // Create random points within those triangles
                Point3D[] points = Math3D.GetRandomPoints_OnHull(triangles, Convert.ToInt32(trkNumPoints.Value));

                foreach (Point3D point in points)
                {
                    AddDot(point, DOTRADIUS, _colors.MedSlate);
                }

                //SortedList<int, List<Point3D>> points = Math3D.GetRandomPointsOnHull_Structured(_rand, triangles, Convert.ToInt32(trkNumPoints.Value));

                //foreach (Point3D point in points.SelectMany(o => o.Value))
                //{
                //    AddDot(point, DOTRADIUS, _colors.MedSlate);
                //}

                // Semitransparent must be added last
                AddHull(triangles, chkPointsDrawFaces.IsChecked.Value, chkPointsDrawLines.IsChecked.Value, false, false, true, false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnPointsOnHullRandom_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RemoveCurrentBody();

                // Pick a random hull
                TriangleIndexed[] triangles = GetRandomHull();

                // Create random points within those triangles
                Point3D[] points = Math3D.GetRandomPoints_OnHull(triangles, Convert.ToInt32(trkNumPoints.Value));

                foreach (Point3D point in points)
                {
                    AddDot(point, DOTRADIUS, _colors.MedSlate);
                }

                // Semitransparent must be added last
                AddHull(triangles, chkPointsDrawFaces.IsChecked.Value, chkPointsDrawLines.IsChecked.Value, false, false, true, false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnHPHOrigAttempt_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RemoveCurrentBody();

                // Get Orig Hull
                TriangleIndexed[] origTriangles = GetRandomHull();

                // Create random points within those triangles
                Point3D[] points = Math3D.GetRandomPoints_OnHull(origTriangles, Convert.ToInt32(trkNumPoints.Value));

                //TODO:  This fails a lot when points are coplanar.  Preprocess the points:
                //		? Pre-processes the input point cloud by converting it to a unit-normal cube. Duplicate vertices are removed based on a normalized tolerance level (i.e. 0.1 means collapse vertices within 1/10th the width/breadth/depth of any side. This is extremely useful in eliminating slivers. When cleaning up ?duplicates and/or nearby neighbors? it also keeps the one which is ?furthest away? from the centroid of the volume. 


                // Other thoughts on coplanar input:
                // Coplanar input might fail right away since the code wont find a starting tetrahedron/simplex. I’m not sure if there is a “fix” in the version John posted that will generate a small box at the “center” (avg of points) should this occur. 
                //John Ratcliff added a wrapper that might be scaling the input to be in the 0 to 1 range. If I remember correctly, this scaling might be non uniform. 
                //If you change this, then be warned about the hardcoded epsilon value found within the code. Yes, I do know that you cannot just assume epsilon is 0.00001. The code started out in an experimental 3dsmax plugin and got moved into a more serious production environment rather quick. 
                //Another issue: 
                //The algorithm uses a greedy iteration where it finds the vertex furthest out in a particular direction at each step. One issue with this approach is that its possible to pick a vertex that, while at the limit of the convex hull, can be interpolated using neighboring vertices. In other words, its lies along an edge (collinear), or within a face (coplanar) made up by other vertices. An example would be a tessellated box. You want to just pick the 8 corners and avoid picking any other the other vertices. To avoid this, after finding a candidate vertex, the code perturbs the support direction to see if it still selects the same vertex. If not, then it ignores this vertex. Its not a very elegant solution. I suspect this could cause the algorithm to be unable to find candidates on a highly tessellated sphere. 
                //A cleaner and more robust approach would be to just allow such interpolating extreme vertices initially, and then after generating a hull, successively measure the contribution of each vertex to the hull and remove those that add no value. That would require more processing, but this is usually an offline process anyways. I haven’t had the opportunity to go back and implement this improvement.



                // Get Convex Hull
                TriangleIndexed[] finalTriangles = QuickHull6.GetQuickHull(points.ToArray());

                #region Draw

                // Points
                if (chkHPHPoints.IsChecked.Value)
                {
                    foreach (Point3D point in points)
                    {
                        AddDot(point, DOTRADIUS, _colors.MedSlate);
                    }
                }

                // Semitransparent must be added last

                // Final Hull
                AddHull(finalTriangles, chkHPHFinalFaces.IsChecked.Value, chkHPHFinalLines.IsChecked.Value, false, false, false, chkHPHFinalSoftFaces.IsChecked.Value);

                // Orig Hull
                AddHull(origTriangles, chkHPHOrigFaces.IsChecked.Value, chkHPHOrigLines.IsChecked.Value, false, false, true, chkHPHOrigSoftFaces.IsChecked.Value);

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnHPHPreprocessPoints_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RemoveCurrentBody();

                // Get Orig Hull
                TriangleIndexed[] origTriangles = GetRandomHull();

                // Create random points within those triangles
                SortedList<int, List<Point3D>> points = Math3D.GetRandomPoints_OnHull_Structured(origTriangles, Convert.ToInt32(trkNumPoints.Value));



                // Quickhull6 fails with coplanar points.  It needs to be fixed eventually, but a quick fix is to do a 2D quickhull on
                // each triangle.  Then send only those outer points to the quickhull 3D method
                //
                // This still doesn't work as well as I'd like, because a lot of the random hulls have square plates (2 triangles per plane).
                // The points on those two triangles should be combined when doing the quickhull 2D.  But I'd rather put my effort
                // into just fixing the quickhull 3D algorithm


                //foreach (int triangleIndex in points.Keys)		// can't do a foreach.  Setting the list messes up the iterator
                for (int keyCntr = 0; keyCntr < points.Keys.Count; keyCntr++)
                {
                    int triangleIndex = points.Keys[keyCntr];
                    List<Point3D> localPoints = points[triangleIndex];
                    Point3D[] localPointsTransformed = localPoints.ToArray();

                    Quaternion rotation = Math3D.GetRotation(origTriangles[triangleIndex].Normal, new Vector3D(0, 0, 1));
                    Transform3D transform = new RotateTransform3D(new QuaternionRotation3D(rotation));
                    transform.Transform(localPointsTransformed);		// not worried about translating to z=0.  quickhull2D ignores z completely

                    var localOuterPoints = Math2D.GetConvexHull(localPointsTransformed);

                    // Swap out all the points with just the outer points for this triangle
                    points[triangleIndex] = localOuterPoints.PerimiterLines.Select(o => localPoints[o]).ToList();
                }



                // Get Convex Hull
                TriangleIndexed[] finalTriangles = QuickHull6.GetQuickHull(points.SelectMany(o => o.Value).ToArray());

                #region Draw

                // Points
                if (chkHPHPoints.IsChecked.Value)
                {
                    foreach (Point3D point in points.SelectMany(o => o.Value))
                    {
                        AddDot(point, DOTRADIUS, _colors.MedSlate);
                    }
                }

                // Semitransparent must be added last

                // Final Hull
                AddHull(finalTriangles, chkHPHFinalFaces.IsChecked.Value, chkHPHFinalLines.IsChecked.Value, false, false, false, chkHPHFinalSoftFaces.IsChecked.Value);

                // Orig Hull
                AddHull(origTriangles, chkHPHOrigFaces.IsChecked.Value, chkHPHOrigLines.IsChecked.Value, false, false, true, chkHPHOrigSoftFaces.IsChecked.Value);

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnHPHDetectCoplanar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RemoveCurrentBody();

                // Get Orig Hull
                TriangleIndexed[] origTriangles = GetRandomHull();

                // Create random points within those triangles
                Point3D[] points = Math3D.GetRandomPoints_OnHull(origTriangles, Convert.ToInt32(trkNumPoints.Value));

                // Get Convex Hull
                TriangleIndexed[] finalTriangles = QuickHull7.GetConvexHull(points.ToArray());

                #region Draw

                // Points
                if (chkHPHPoints.IsChecked.Value)
                {
                    foreach (Point3D point in points)
                    {
                        AddDot(point, DOTRADIUS, _colors.MedSlate);
                    }
                }

                // Semitransparent must be added last

                // Final Hull
                AddHull(finalTriangles, chkHPHFinalFaces.IsChecked.Value, chkHPHFinalLines.IsChecked.Value, false, false, false, chkHPHFinalSoftFaces.IsChecked.Value);

                // Orig Hull
                AddHull(origTriangles, chkHPHOrigFaces.IsChecked.Value, chkHPHOrigLines.IsChecked.Value, false, false, true, chkHPHOrigSoftFaces.IsChecked.Value);

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnHPHPreprocessPoints2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RemoveCurrentBody();


                //TODO:  GetCylinder/Cone seem to be making invalid triangles (2 of the vertices are the same point).  Not sure how they show ok

                // Get Orig Hull
                //TriangleIndexed[] origTriangles = GetRandomHull(_rand);
                //TriangleIndexed[] origTriangles = UtilityWPF.GetTrianglesFromMesh(UtilityWPF.GetCone_AlongX(3, MAXRADIUS, MAXRADIUS));
                TriangleIndexed[] origTriangles = UtilityWPF.GetTrianglesFromMesh(UtilityWPF.GetCylinder_AlongX(3, MAXRADIUS, MAXRADIUS));

                // Create random points within those triangles
                SortedList<int, List<Point3D>> points = Math3D.GetRandomPoints_OnHull_Structured(origTriangles, Convert.ToInt32(trkNumPoints.Value));

                #region Group coplanar triangles

                // Group the triangles together that sit on the same plane
                List<TriangleIndexedLinked> trianglesLinked = origTriangles.Select(o => new TriangleIndexedLinked(o.Index0, o.Index1, o.Index2, o.AllPoints)).ToList();
                TriangleIndexedLinked.LinkTriangles_Edges(trianglesLinked, true);

                List<TrianglesInPlane> groupedTriangles = new List<TrianglesInPlane>();

                for (int cntr = 0; cntr < trianglesLinked.Count; cntr++)
                {
                    GroupCoplanarTriangles(cntr, trianglesLinked[cntr], groupedTriangles);
                }

                #endregion

                #region Quickhull2D each group's points

                SortedList<int, List<Point3D>> groupPoints = new SortedList<int, List<Point3D>>();

                for (int cntr = 0; cntr < groupedTriangles.Count; cntr++)
                {
                    // Grab all the points for the triangles in this group
                    //List<Point3D> localPoints = groupedTriangles[cntr].TriangleIndices.SelectMany(o => points[o]).ToList();		// not all triangles will have points
                    List<Point3D> localPoints = new List<Point3D>();
                    foreach (int triangleIndex in groupedTriangles[cntr].TriangleIndices)
                    {
                        if (points.ContainsKey(triangleIndex))
                        {
                            localPoints.AddRange(points[triangleIndex]);
                        }
                    }

                    // Rotate a copy of these points onto the xy plane
                    Point3D[] localPointsTransformed = localPoints.ToArray();

                    Quaternion rotation = Math3D.GetRotation(groupedTriangles[cntr].NormalUnit, new Vector3D(0, 0, 1));
                    Transform3D transform = new RotateTransform3D(new QuaternionRotation3D(rotation));
                    transform.Transform(localPointsTransformed);		// not worried about translating to z=0.  quickhull2D ignores z completely

                    // Do a 2D quickhull on these points
                    var localOuterPoints = Math2D.GetConvexHull(localPointsTransformed);

                    // Store only the outer points
                    groupPoints.Add(cntr, localOuterPoints.PerimiterLines.Select(o => localPoints[o]).ToList());
                }

                #endregion

                // Get Convex Hull
                TriangleIndexed[] finalTriangles = QuickHull6.GetQuickHull(groupPoints.SelectMany(o => o.Value).ToArray());

                #region Draw

                // Points
                if (chkHPHPoints.IsChecked.Value)
                {
                    List<Point3D> usedPoints = groupPoints.SelectMany(o => o.Value).ToList();

                    foreach (Point3D point in points.SelectMany(o => o.Value))
                    {
                        if (usedPoints.Contains(point))
                        {
                            AddDot(point, DOTRADIUS, _colors.MedSlate);
                        }
                        else
                        {
                            AddDot(point, DOTRADIUS, _colors.LightLightSlate);
                        }
                    }
                }

                // Semitransparent must be added last

                // Final Hull
                AddHull(finalTriangles, chkHPHFinalFaces.IsChecked.Value, chkHPHFinalLines.IsChecked.Value, false, false, false, chkHPHFinalSoftFaces.IsChecked.Value);

                // Orig Hull
                AddHull(origTriangles, chkHPHOrigFaces.IsChecked.Value, chkHPHOrigLines.IsChecked.Value, false, false, true, chkHPHOrigSoftFaces.IsChecked.Value);

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnIcosahedron_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RemoveCurrentBody();

                int numRecursions = Convert.ToInt32(trkIcoRecurse.Value);

                TriangleIndexed[] triangles = UtilityWPF.GetIcosahedron(trkIcoRadius.Value, numRecursions);

                if (chkDrawDots.IsChecked.Value)
                {
                    foreach (Point3D point in triangles[0].AllPoints)
                    {
                        AddDot(point, DOTRADIUS, _colors.MedSlate);
                    }
                }

                AddHull(triangles, true, chkDrawLines.IsChecked.Value, chkDrawNormals.IsChecked.Value, false, chkNearlyTransparent.IsChecked.Value, chkSoftFaces.IsChecked.Value);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void trkIcoRecurse_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (!_isInitialized)
                {
                    return;
                }

                lblIcoRecurse.Text = trkIcoRecurse.Value.ToString("N0");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void trkIcoRadius_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (!_isInitialized)
                {
                    return;
                }

                lblIcoRadius.Text = trkIcoRadius.Value.ToString("N1");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnIcoSpike1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RemoveCurrentBody();

                int numRecursions = Convert.ToInt32(trkIcoRecurse.Value);

                #region Outer

                //TriangleIndexed[] triangles = IcoSpike1.GetIcosahedron(trkIcoRadius.Value, numRecursions);

                double[] radius = new double[numRecursions + 1];

                radius[0] = trkIcoRadius.Value;
                for (int cntr = 1; cntr < radius.Length; cntr++)
                {
                    radius[cntr] = trkIcoRadius.Value / Convert.ToDouble(cntr + 2);
                }

                TriangleIndexed[] triangles = UtilityWPF.GetIcosahedron(radius);

                if (chkDrawDots.IsChecked.Value)
                {
                    foreach (Point3D point in triangles[0].AllPoints)
                    {
                        AddDot(point, DOTRADIUS, _colors.MedSlate);
                    }
                }

                AddHull(triangles, true, chkDrawLines.IsChecked.Value, chkDrawNormals.IsChecked.Value, false, chkNearlyTransparent.IsChecked.Value, chkSoftFaces.IsChecked.Value);

                #endregion

                #region Inner

                if (numRecursions > 0)
                {
                    //triangles = IcoSpike1.GetIcosahedron(trkIcoRadius.Value * trkIcoSpikeRatio.Value, numRecursions - 1);

                    radius = new double[numRecursions];

                    radius[0] = trkIcoRadius.Value * trkIcoSpikeRatio.Value;
                    for (int cntr = 1; cntr < radius.Length; cntr++)
                    {
                        radius[cntr] = radius[0] / Convert.ToDouble(cntr + 2);
                    }

                    triangles = UtilityWPF.GetIcosahedron(radius);

                    if (chkDrawDots.IsChecked.Value)
                    {
                        foreach (Point3D point in triangles[0].AllPoints)
                        {
                            AddDot(point, DOTRADIUS, _colors.MedSlate);
                        }
                    }

                    AddHull(triangles, true, chkDrawLines.IsChecked.Value, chkDrawNormals.IsChecked.Value, false, chkNearlyTransparent.IsChecked.Value, chkSoftFaces.IsChecked.Value);
                }

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnIcoSpike2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RemoveCurrentBody();

                int numRecursions = Convert.ToInt32(trkIcoRecurse.Value);

                double[] radius = new double[numRecursions + 1];

                #region First only

                radius[0] = trkIcoRadius.Value;

                for (int cntr = 1; cntr < radius.Length; cntr++)
                {
                    radius[cntr] = trkIcoRadius.Value * trkIcoSpikeRatio.Value;
                }

                #endregion

                #region Every other - fail

                //for (int cntr = 0; cntr < radius.Length; cntr++)
                //{
                //    if (cntr % 2 == 0)
                //    {
                //        radius[cntr] = trkIcoRadius.Value;
                //    }
                //    else
                //    {
                //        radius[cntr] = trkIcoRadius.Value * trkIcoSpikeRatio.Value;
                //    }
                //}

                #endregion

                TriangleIndexed[] triangles = UtilityWPF.GetIcosahedron(radius);

                if (chkDrawDots.IsChecked.Value)
                {
                    foreach (Point3D point in triangles[0].AllPoints)
                    {
                        AddDot(point, DOTRADIUS, _colors.MedSlate);
                    }
                }

                AddHull(triangles, true, chkDrawLines.IsChecked.Value, chkDrawNormals.IsChecked.Value, false, chkNearlyTransparent.IsChecked.Value, chkSoftFaces.IsChecked.Value);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnIcoSpike3_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RemoveCurrentBody();

                int numRecursions = Convert.ToInt32(trkIcoRecurse.Value);

                // Spikes
                //double[] radius = Enumerable.Range(0, numRecursions + 1).Select(o => trkIcoRadius.Value * trkIcoSpikeRatio.Value).ToArray();
                //radius[0] = trkIcoRadius.Value;

                double[] radius = new double[] { trkIcoRadius.Value, trkIcoRadius.Value * trkIcoSpikeRatio.Value * trkIcoSpikeRatioUnder.Value };

                TriangleIndexed[] triangles = UtilityWPF.GetIcosahedron(radius);

                AddHullTest(triangles, chkDrawLines.IsChecked.Value, chkDrawNormals.IsChecked.Value, _colors.Spike, _colors.SpikeSpecular, chkSoftFaces.IsChecked.Value);

                // Ball
                //if (numRecursions > 0)
                //{
                triangles = UtilityWPF.GetIcosahedron(trkIcoRadius.Value * trkIcoSpikeRatio.Value, numRecursions);

                AddHullTest(triangles, chkDrawLines.IsChecked.Value, chkDrawNormals.IsChecked.Value, _colors.SpikeBall, _colors.SpikeBallSpecular, true);
                //}
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void trkIcoSpikeRatio_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (!_isInitialized)
                {
                    return;
                }

                lblIcoSpikeRatio.Text = trkIcoSpikeRatio.Value.ToString("N2");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void trkIcoSpikeRatioUnder_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (!_isInitialized)
                {
                    return;
                }

                lblIcoSpikeRatioUnder.Text = trkIcoSpikeRatioUnder.Value.ToString("N2");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnRhombicuboctahedron_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //http://en.wikipedia.org/wiki/Rhombicuboctahedron

                RemoveCurrentBody();

                Rhombicuboctahedron rhomb;
                if (chkPolyRandomSize.IsChecked.Value)
                {
                    rhomb = UtilityWPF.GetRhombicuboctahedron(StaticRandom.NextPercent(MAXRADIUS, .75), StaticRandom.NextPercent(MAXRADIUS, .75), StaticRandom.NextPercent(MAXRADIUS, .75));
                }
                else
                {
                    rhomb = UtilityWPF.GetRhombicuboctahedron(MAXRADIUS, MAXRADIUS, MAXRADIUS);
                }

                if (chkDrawDots.IsChecked.Value || chkPolyLabelPoints.IsChecked.Value)
                {
                    foreach (Point3D point in rhomb.AllPoints)
                    {
                        AddDot(point, DOTRADIUS, _colors.MedSlate);
                    }

                    if (chkPolyLabelPoints.IsChecked.Value)
                    {
                        AddPointLabels(rhomb.AllPoints);
                    }
                }

                if (chkPolyMajorLines.IsChecked.Value)
                {
                    AddLines(rhomb.GetUniqueLines(), rhomb.AllPoints);
                }

                AddHull(rhomb.AllTriangles, true, chkDrawLines.IsChecked.Value && !chkPolyMajorLines.IsChecked.Value, chkDrawNormals.IsChecked.Value, false, chkNearlyTransparent.IsChecked.Value, chkSoftFaces.IsChecked.Value);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnIcosidodecahedron_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RemoveCurrentBody();

                Icosidodecahedron ico;
                if (chkPolyRandomSize.IsChecked.Value)
                {
                    ico = UtilityWPF.GetIcosidodecahedron(StaticRandom.NextPercent(MAXRADIUS, .75));
                }
                else
                {
                    ico = UtilityWPF.GetIcosidodecahedron(MAXRADIUS);
                }

                if (chkDrawDots.IsChecked.Value || chkPolyLabelPoints.IsChecked.Value)
                {
                    foreach (Point3D point in ico.AllPoints)
                    {
                        AddDot(point, DOTRADIUS, _colors.MedSlate);
                    }

                    if (chkPolyLabelPoints.IsChecked.Value)
                    {
                        AddPointLabels(ico.AllPoints);
                    }
                }

                if (chkPolyMajorLines.IsChecked.Value)
                {
                    AddLines(ico.GetUniqueLines(), ico.AllPoints);
                }

                AddHull(ico.AllTriangles, true, chkDrawLines.IsChecked.Value && !chkPolyMajorLines.IsChecked.Value, chkDrawNormals.IsChecked.Value, false, chkNearlyTransparent.IsChecked.Value, chkSoftFaces.IsChecked.Value);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnTruncatedIcosidodecahedron_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RemoveCurrentBody();

                TruncatedIcosidodecahedron ico;
                if (chkPolyRandomSize.IsChecked.Value)
                {
                    ico = UtilityWPF.GetTruncatedIcosidodecahedron(StaticRandom.NextPercent(MAXRADIUS, .75));
                }
                else
                {
                    ico = UtilityWPF.GetTruncatedIcosidodecahedron(MAXRADIUS);
                }

                if (chkDrawDots.IsChecked.Value || chkPolyLabelPoints.IsChecked.Value)
                {
                    foreach (Point3D point in ico.AllPoints)
                    {
                        AddDot(point, DOTRADIUS, _colors.MedSlate);
                    }

                    if (chkPolyLabelPoints.IsChecked.Value)
                    {
                        AddPointLabels(ico.AllPoints);
                    }
                }

                if (chkPolyMajorLines.IsChecked.Value)
                {
                    AddLines(ico.GetUniqueLines(), ico.AllPoints);
                }

                AddHull(ico.AllTriangles, true, chkDrawLines.IsChecked.Value && !chkPolyMajorLines.IsChecked.Value, chkDrawNormals.IsChecked.Value, false, chkNearlyTransparent.IsChecked.Value, chkSoftFaces.IsChecked.Value);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnDodecahedron_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RemoveCurrentBody();

                Dodecahedron dodec;
                if (chkPolyRandomSize.IsChecked.Value)
                {
                    dodec = UtilityWPF.GetDodecahedron(StaticRandom.NextPercent(MAXRADIUS, .75));
                }
                else
                {
                    dodec = UtilityWPF.GetDodecahedron(MAXRADIUS);
                }

                if (chkDrawDots.IsChecked.Value || chkPolyLabelPoints.IsChecked.Value)
                {
                    foreach (Point3D point in dodec.AllPoints)
                    {
                        AddDot(point, DOTRADIUS, _colors.MedSlate);
                    }

                    if (chkPolyLabelPoints.IsChecked.Value)
                    {
                        AddPointLabels(dodec.AllPoints);
                    }
                }

                if (chkPolyMajorLines.IsChecked.Value)
                {
                    AddLines(dodec.GetUniqueLines(), dodec.AllPoints);
                }

                AddHull(dodec.AllTriangles, true, chkDrawLines.IsChecked.Value && !chkPolyMajorLines.IsChecked.Value, chkDrawNormals.IsChecked.Value, false, chkNearlyTransparent.IsChecked.Value, chkSoftFaces.IsChecked.Value);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnPentakisDodecahedron_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RemoveCurrentBody();

                TriangleIndexed[] triangles;
                if (chkPolyRandomSize.IsChecked.Value)
                {
                    triangles = UtilityWPF.GetPentakisDodecahedron(StaticRandom.NextPercent(MAXRADIUS, .75d), StaticRandom.NextPercent(MAXRADIUS, .75d));
                }
                else
                {
                    triangles = UtilityWPF.GetPentakisDodecahedron(MAXRADIUS);
                }

                if (chkDrawDots.IsChecked.Value || chkPolyLabelPoints.IsChecked.Value)
                {
                    foreach (Point3D point in triangles[0].AllPoints)
                    {
                        AddDot(point, DOTRADIUS, _colors.MedSlate);
                    }

                    if (chkPolyLabelPoints.IsChecked.Value)
                    {
                        AddPointLabels(triangles[0].AllPoints);
                    }
                }

                AddHull(triangles, true, chkDrawLines.IsChecked.Value, chkDrawNormals.IsChecked.Value, false, chkNearlyTransparent.IsChecked.Value, chkSoftFaces.IsChecked.Value);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnTruncatedIcosahedron_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RemoveCurrentBody();

                TruncatedIcosahedron soccer;
                if (chkPolyRandomSize.IsChecked.Value)
                {
                    soccer = UtilityWPF.GetTruncatedIcosahedron(StaticRandom.NextPercent(MAXRADIUS, .75));
                }
                else
                {
                    soccer = UtilityWPF.GetTruncatedIcosahedron(MAXRADIUS);
                }

                if (chkDrawDots.IsChecked.Value || chkPolyLabelPoints.IsChecked.Value)
                {
                    foreach (Point3D point in soccer.AllPoints)
                    {
                        AddDot(point, DOTRADIUS, _colors.MedSlate);
                    }

                    if (chkPolyLabelPoints.IsChecked.Value)
                    {
                        AddPointLabels(soccer.AllPoints);
                    }
                }

                if (chkPolyMajorLines.IsChecked.Value)
                {
                    AddLines(soccer.GetUniqueLines(), soccer.AllPoints);
                }

                AddHull(soccer.AllTriangles, true, chkDrawLines.IsChecked.Value && !chkPolyMajorLines.IsChecked.Value, chkDrawNormals.IsChecked.Value, false, chkNearlyTransparent.IsChecked.Value, chkSoftFaces.IsChecked.Value);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnNewAsteroid_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ITriangleIndexed[] hull = Asteroid.GetHullTriangles(MAXRADIUS);
                _shatter = new AsteroidShatter(hull);

                DrawShatterAsteroid();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnShatterTriangle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Point3D[] points = Enumerable.Range(0, 3).
                    Select(o => Math3D.GetRandomVector_Spherical(MAXRADIUS).ToPoint()).
                    ToArray();

                TriangleIndexed[] hull = new[] { new TriangleIndexed(0, 1, 2, points) };

                _shatter = new AsteroidShatter(hull);

                DrawShatterAsteroid();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnShatterTest_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RemoveCurrentBody();

                Point3D[] points = new[]
                    {
                        new Point3D(-3.18319174107365, -3.56900996180343, -2.25515040289206),
                        new Point3D(-3.20726473967357, -3.51925477841, -2.30208605004547),
                        new Point3D(-2.7862288060072, 1.62778045809187, -1.78801317097937),
                        new Point3D(-2.50952313051213, 1.89001863612309, -1.29104866754377),
                        new Point3D(-1.97791836600583, 0.946525873863232, -0.262483926147299),
                        new Point3D(-1.78521769084085, 0.403608941248565, 0.120603908182445),
                    };

                // transformed 2D:
                //{0.0675632217518369,-5.02738214164118}	System.Windows.Point
                //{0.140075582834728,-5.02738214164118}	System.Windows.Point
                //{3.19924698626505,-0.835129258923396}	System.Windows.Point
                //{2.96564850696943,-0.253974923988107}	System.Windows.Point
                //{1.47600830863918,-0.145809657635379}	System.Windows.Point
                //{0.791541697529613,-0.246577177285284}	System.Windows.Point


                var hull2D = Math2D.GetConvexHull(points.ToArray());

                var delaunay2D = Math2D.GetDelaunayTriangulation(hull2D.Points);

                //var hull3D = Math2D.GetTrianglesFromConvexPoly(hull2D.PerimiterLines, points);


                // Draw dots
                foreach (Point3D point in points)
                {
                    AddDot(point, DOTRADIUS, _colors.MedSlate);
                }

                // Draw lines
                ScreenSpaceLines3D lineVisual = new ScreenSpaceLines3D(true);
                lineVisual.Thickness = LINETHICKNESS;
                lineVisual.Color = _colors.DarkSlate;

                for (int cntr = 0; cntr < points.Length - 1; cntr++)
                {
                    lineVisual.AddLine(points[cntr], points[cntr + 1]);
                }

                lineVisual.AddLine(points[points.Length - 1], points[0]);

                _viewport.Children.Add(lineVisual);
                _visuals.Add(lineVisual);


                //ScreenSpaceLines3D lineVisual = new ScreenSpaceLines3D(true);
                //lineVisual.Thickness = LINETHICKNESS;
                //lineVisual.Color = _colors.DarkSlate;

                //for (int cntr = 0; cntr < hull2D.PerimiterLines.Length - 1; cntr++)
                //{
                //    lineVisual.AddLine(points[hull2D.PerimiterLines[cntr]], points[hull2D.PerimiterLines[cntr + 1]]);
                //}

                //lineVisual.AddLine(points[hull2D.PerimiterLines[hull2D.PerimiterLines.Length - 1]], points[hull2D.PerimiterLines[0]]);

                //_viewport.Children.Add(lineVisual);
                //_visuals.Add(lineVisual);



                //AddHull(hull3D, true, false);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnShatterTest2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RemoveCurrentBody();

                #region 1

                //// Chain 1
                //ScreenSpaceLines3D lineVisual = new ScreenSpaceLines3D(true);
                //lineVisual.Thickness = LINETHICKNESS;
                //lineVisual.Color = _colors.DarkSlate;

                //lineVisual.AddLine(new Point3D(2.41697644690408, -1.03434037222278, 5.34046307400328), new Point3D(2.42650988724898, -0.911816989366243, 5.46170717130378));

                //_viewport.Children.Add(lineVisual);
                //_visuals.Add(lineVisual);

                //// Chain 2
                //lineVisual = new ScreenSpaceLines3D(true);
                //lineVisual.Thickness = LINETHICKNESS;
                //lineVisual.Color = _colors.DarkSlate;

                //lineVisual.AddLine(new Point3D(6.19389534318238, 0.466722248575599, 2.2599287921911), new Point3D(6.11825541298441, 0.581162982933572, 2.47864203662865));

                //_viewport.Children.Add(lineVisual);
                //_visuals.Add(lineVisual);

                //// Chain 3
                //AddDot(new Point3D(6.00211993510427, -0.468334622855055, 1.48310916922626), DOTRADIUS, _colors.MedSlate);

                #endregion
                #region 2

                //// Chain 1
                //ScreenSpaceLines3D lineVisual = new ScreenSpaceLines3D(true);
                //lineVisual.Thickness = LINETHICKNESS;
                //lineVisual.Color = _colors.DarkSlate;

                //lineVisual.AddLine(new Point3D(-1.66751547623575, 2.43551353884396, -1.28733504900566), new Point3D(-1.91755736428278, 2.40730283120172, -1.05102811655317));

                //_viewport.Children.Add(lineVisual);
                //_visuals.Add(lineVisual);

                //// Chain 2
                //lineVisual = new ScreenSpaceLines3D(true);
                //lineVisual.Thickness = LINETHICKNESS;
                //lineVisual.Color = _colors.DarkSlate;

                //lineVisual.AddLine(new Point3D(-4.2862280355896, 1.18950573617112, -3.53012404289355), new Point3D(-4.32021145708624, 1.28199914726276, -3.01992835802973));

                //_viewport.Children.Add(lineVisual);
                //_visuals.Add(lineVisual);

                //// Chain 3
                //AddDot(new Point3D(-2.19325004824761, 1.64235592394038, -4.43257882249856), DOTRADIUS, _colors.MedSlate);

                #endregion
                #region 3

                //// Chain 1
                //ScreenSpaceLines3D lineVisual = new ScreenSpaceLines3D(true);
                //lineVisual.Thickness = LINETHICKNESS;
                //lineVisual.Color = _colors.DarkSlate;

                //lineVisual.AddLine(new Point3D(2.78469044162559, 3.1608896451211, 1.81549909443282), new Point3D(2.59850015567276, 3.32650840954175, 1.17271113857054));

                //_viewport.Children.Add(lineVisual);
                //_visuals.Add(lineVisual);

                //// Chain 2
                //lineVisual = new ScreenSpaceLines3D(true);
                //lineVisual.Thickness = LINETHICKNESS;
                //lineVisual.Color = _colors.DarkSlate;

                //lineVisual.AddLine(new Point3D(0.309180751879649, 3.01641280381582, 3.58860192317121), new Point3D(0.568794338660403, 2.90869763119882, 3.94299639878302));

                //_viewport.Children.Add(lineVisual);
                //_visuals.Add(lineVisual);

                //// Chain 3
                //AddDot(new Point3D(0.359346494303422, 3.32770287188034, 2.19655266643483), DOTRADIUS, _colors.MedSlate);

                #endregion
                #region 4

                // Chain 1
                ScreenSpaceLines3D lineVisual = new ScreenSpaceLines3D(true);
                lineVisual.Thickness = LINETHICKNESS;
                lineVisual.Color = _colors.DarkSlate;

                lineVisual.AddLine(new Point3D(0.556672739256096, -4.90598854986012, -2.45356798521325), new Point3D(0.995689896533931, -3.39201206426292, -4.1745580542808));

                _viewport.Children.Add(lineVisual);
                _visuals.Add(lineVisual);

                // Chain 2
                lineVisual = new ScreenSpaceLines3D(true);
                lineVisual.Thickness = LINETHICKNESS;
                lineVisual.Color = _colors.DarkSlate;

                lineVisual.AddLine(new Point3D(2.71027671852163, -3.29453604934813, -2.98697726757335), new Point3D(1.91777287426863, -3.20825352131349, -3.71451112307666));
                lineVisual.AddLine(new Point3D(1.91777287426863, -3.20825352131349, -3.71451112307666), new Point3D(1.31959862517835, -3.25975720550465, -4.10503104352338));

                _viewport.Children.Add(lineVisual);
                _visuals.Add(lineVisual);

                // Chain 3
                AddDot(new Point3D(0.581416715714739, -4.94202763952268, -2.38550307273868), DOTRADIUS, _colors.MedSlate);

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnNewAsteroidDivided1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ITriangleIndexed[] hull = Asteroid.GetHullTriangles(MAXRADIUS);

                Point3D[] hullPoints = hull[0].AllPoints;

                double avgEdge = TriangleIndexed.GetUniqueLines(hull).Average(o => (hullPoints[o.Item1] - hullPoints[o.Item2]).Length);

                // Make sure no triangle is larger than a certain size
                ITriangle[] slicedHull1 = Math3D.SliceLargeTriangles(hull, avgEdge * 1.5);
                TriangleIndexed[] slicedHull2 = TriangleIndexed.ConvertToIndexed(slicedHull1);

                _shatter = new AsteroidShatter(slicedHull2);

                DrawShatterAsteroid();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnNewAsteroidDivided2Debug_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //TODO: Remove excessively thin triangles


                // Make sure no triangle is larger than a certain size, and push the point out against a bezier

                ITriangleIndexed[] hull = Asteroid.GetHullTriangles(MAXRADIUS);

                Point3D[] hullPoints = hull[0].AllPoints;

                double avgEdge = TriangleIndexed.GetUniqueLines(hull).Average(o => (hullPoints[o.Item1] - hullPoints[o.Item2]).Length);

                Tuple<int, int, int>[] origKeys = hull.
                    Select(o => o.IndexArray.OrderBy().ToArray()).
                    Select(o => Tuple.Create(o[0], o[1], o[2])).
                    ToArray();

                long[] specialTokens;

                // Make sure no triangle is larger than a certain size
                ITriangleIndexed[] slicedHull = SmoothHullDebug.SmoothSlice(out specialTokens, hull, avgEdge * 2, 1);
                //ITriangleIndexed[] slicedHull = SmoothHull.SmoothSlice(out specialTokens, hull, avgEdge * .25, 1);

                _shatter = new AsteroidShatter(slicedHull);

                //DrawShatterAsteroid();

                var hullTriangles = _shatter.Hull.
                    Select(o => new { Triangle = o, Array = o.IndexArray.OrderBy().ToArray() }).
                    Select(o => new { Triangle = o.Triangle, Key = Tuple.Create(o.Array[0], o.Array[1], o.Array[2]) }).
                    Select(o => new { Triangle = o.Triangle, Key = o.Key, IsOrig = origKeys.Contains(o.Key) }).
                    ToArray();

                RemoveCurrentBody();

                ITriangleIndexed[] triangles = hullTriangles.Where(o => o.IsOrig && !specialTokens.Contains(o.Triangle.Token)).Select(o => o.Triangle).ToArray();
                AddHull_CustomColor(triangles, _colors.HullFace, true, chkDrawLines.IsChecked.Value, chkDrawNormals.IsChecked.Value, false, chkNearlyTransparent.IsChecked.Value, chkSoftFaces.IsChecked.Value);

                triangles = TriangleIndexed.ConvertToIndexed(hullTriangles.Where(o => !o.IsOrig && !specialTokens.Contains(o.Triangle.Token)).Select(o => o.Triangle).ToArray());
                AddHull_CustomColor(triangles, UtilityWPF.AlphaBlend(Colors.Tomato, Colors.Transparent, .8), true, chkDrawLines.IsChecked.Value, chkDrawNormals.IsChecked.Value, false, false, chkSoftFaces.IsChecked.Value);

                triangles = TriangleIndexed.ConvertToIndexed(hullTriangles.Where(o => specialTokens.Contains(o.Triangle.Token)).Select(o => o.Triangle).ToArray());
                AddHull_CustomColor(triangles, UtilityWPF.AlphaBlend(Colors.LightGreen, Colors.Transparent, .8), true, chkDrawLines.IsChecked.Value, chkDrawNormals.IsChecked.Value, false, false, chkSoftFaces.IsChecked.Value);

                return;
                #region Single point and plane


                var trianglesByPoint = Enumerable.Range(0, hull[0].AllPoints.Length).
                    Select(o => hull.Where(p => p.IndexArray.Contains(o)).ToArray()).
                    ToArray();


                int randIndex = StaticRandom.Next(trianglesByPoint.Length);


                hull = trianglesByPoint[randIndex];


                Point3D[] otherPoints = trianglesByPoint[randIndex].
                    SelectMany(o => o.IndexArray).
                    Where(o => o != randIndex).
                    Select(o => hull[0].AllPoints[o]).
                    ToArray();

                ITriangle plane = Math2D.GetPlane_Average(otherPoints);


                _shatter = new AsteroidShatter(hull);

                DrawShatterAsteroid();

                AddPlane(plane, Colors.Gray, Colors.DarkOliveGreen, hull[0].AllPoints[randIndex]);

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnNewAsteroidDivided2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //TODO: Remove excessively thin triangles


                // Make sure no triangle is larger than a certain size, and push the point out against a bezier

                ITriangleIndexed[] hull = Asteroid.GetHullTriangles(MAXRADIUS);

                Point3D[] hullPoints = hull[0].AllPoints;

                double avgEdge = TriangleIndexed.GetUniqueLines(hull).Average(o => (hullPoints[o.Item1] - hullPoints[o.Item2]).Length);

                // Make sure no triangle is larger than a certain size
                //ITriangleIndexed[] slicedHull = Math3D.SliceLargeTriangles_Smooth(hull, avgEdge * 1.5);
                ITriangleIndexed[] slicedHull = Math3D.SliceLargeTriangles_Smooth(hull, avgEdge);
                //ITriangleIndexed[] slicedHull = Math3D.SliceLargeTriangles_Smooth(hull, avgEdge * .25);

                _shatter = new AsteroidShatter(slicedHull);

                DrawShatterAsteroid();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnNewAsteroidRemoveSkinny3_Click(object sender, RoutedEventArgs e)
        {
            const double RATIO = .93;

            try
            {
                ITriangleIndexed[] hull = Asteroid.GetHullTriangles(MAXRADIUS);

                // Remove excessively thin triangles
                ITriangleIndexed[] slicedHull = Math3D.RemoveThinTriangles(hull, RATIO);

                //_shatter = new AsteroidShatter(slicedHull);
                //DrawShatterAsteroid();

                _shatter = null;
                _multiHull = new[] { hull, slicedHull };

                RemoveCurrentBody();
                DrawMultipleHulls(_multiHull, true, chkDrawLines.IsChecked.Value, chkDrawNormals.IsChecked.Value, false, chkNearlyTransparent.IsChecked.Value, chkSoftFaces.IsChecked.Value);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnNewAsteroidRemoveSkinnyJ_Click(object sender, RoutedEventArgs e)
        {
            const double RATIO = .93;

            try
            {
                ITriangleIndexed[] hull = Asteroid.GetHullTriangles(MAXRADIUS);

                // Remove excessively thin triangles
                ITriangleIndexed[] slicedHull = Math3D.RemoveThinTriangles(hull, RATIO);

                // Now round it out
                Point3D[] hullPoints = slicedHull[0].AllPoints;
                double avgEdge = TriangleIndexed.GetUniqueLines(slicedHull).Average(o => (hullPoints[o.Item1] - hullPoints[o.Item2]).Length);
                double[] edgeLengths = TriangleIndexed.GetUniqueLines(slicedHull).Select(o => (hullPoints[o.Item1] - hullPoints[o.Item2]).Length).ToArray();

                ITriangleIndexed[] smoothHull = Math3D.SliceLargeTriangles_Smooth(slicedHull, avgEdge);

                //_shatter = new AsteroidShatter(smoothHull);
                //DrawShatterAsteroid();

                _shatter = null;
                _multiHull = new[] { hull, slicedHull, smoothHull };

                RemoveCurrentBody();
                DrawMultipleHulls(_multiHull, true, chkDrawLines.IsChecked.Value, chkDrawNormals.IsChecked.Value, false, chkNearlyTransparent.IsChecked.Value, chkSoftFaces.IsChecked.Value);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnNewAsteroidRemoveAddRemove_Click(object sender, RoutedEventArgs e)
        {
            const double RATIO = .93;

            try
            {
                ITriangleIndexed[] hull = Asteroid.GetHullTriangles(MAXRADIUS);

                // Remove excessively thin triangles
                ITriangleIndexed[] slicedHull = Math3D.RemoveThinTriangles(hull, RATIO);

                // Now round it out
                Point3D[] hullPoints = slicedHull[0].AllPoints;
                double avgEdge = TriangleIndexed.GetUniqueLines(slicedHull).Average(o => (hullPoints[o.Item1] - hullPoints[o.Item2]).Length);
                ITriangleIndexed[] smoothHull = Math3D.SliceLargeTriangles_Smooth(slicedHull, avgEdge);

                ITriangleIndexed[] secondSlicedHull = Math3D.RemoveThinTriangles(smoothHull, RATIO);

                _shatter = null;
                _multiHull = new[] { hull, slicedHull, smoothHull, secondSlicedHull };

                RemoveCurrentBody();
                DrawMultipleHulls(_multiHull, true, chkDrawLines.IsChecked.Value, chkDrawNormals.IsChecked.Value, false, chkNearlyTransparent.IsChecked.Value, chkSoftFaces.IsChecked.Value);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnNewAsteroidFinal_Click(object sender, RoutedEventArgs e)
        {
            const double RATIO = .93;

            try
            {
                ITriangleIndexed[] hull = Asteroid.GetHullTriangles(MAXRADIUS);

                // Remove excessively thin triangles
                ITriangleIndexed[] slicedHull = Math3D.RemoveThinTriangles(hull, RATIO);

                // Now round it out
                Point3D[] hullPoints = slicedHull[0].AllPoints;

                double[] lengths = TriangleIndexed.GetUniqueLines(slicedHull).
                    Select(o => (hullPoints[o.Item1] - hullPoints[o.Item2]).Length).
                    ToArray();

                double avgLen = lengths.Average();
                double maxLen = lengths.Max();
                double sliceLen = Math1D.Avg(avgLen, maxLen);

                // Another thin triangle slice
                ITriangleIndexed[] smoothHull = Math3D.SliceLargeTriangles_Smooth(slicedHull, sliceLen);

                // Remove again
                ITriangleIndexed[] secondSlicedHull = Math3D.RemoveThinTriangles(smoothHull, RATIO);

                _shatter = new AsteroidShatter(secondSlicedHull);
                DrawShatterAsteroid();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnShatterRedraw_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_shatter != null)
                {
                    DrawShatterAsteroid();
                }
                else if (_multiHull != null)
                {
                    RemoveCurrentBody();
                    DrawMultipleHulls(_multiHull, true, chkDrawLines.IsChecked.Value, chkDrawNormals.IsChecked.Value, false, chkNearlyTransparent.IsChecked.Value, chkSoftFaces.IsChecked.Value);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnShatterRandVoronoi_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_shatter == null)
                {
                    MessageBox.Show("Need to create an asteroid first", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int numPoints;
                if (!int.TryParse(txtShatterVoronoiCount.Text, out numPoints))
                {
                    MessageBox.Show("Couldn't parse the number of points as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var aabb = Math3D.GetAABB(_shatter.HullPoints);

                double radius = Math.Sqrt(Math.Max(aabb.Item1.ToVector().LengthSquared, aabb.Item2.ToVector().LengthSquared));
                radius *= .33;

                Point3D[] controlPoints = Enumerable.Range(0, numPoints).
                    Select(o => Math3D.GetRandomVector(radius).ToPoint()).
                    ToArray();

                _shatter.Voronoi = Math3D.GetVoronoi(controlPoints, true);



                //TODO: Finish this
                _shatter.ShatteredHulls = null;



                DrawShatterAsteroid();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnShatterRandVoronoiFar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_shatter == null)
                {
                    MessageBox.Show("Need to create an asteroid first", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int numPoints;
                if (!int.TryParse(txtShatterVoronoiCount.Text, out numPoints))
                {
                    MessageBox.Show("Couldn't parse the number of points as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var aabb = Math3D.GetAABB(_shatter.HullPoints);

                double radius = Math.Sqrt(Math.Max(aabb.Item1.ToVector().LengthSquared, aabb.Item2.ToVector().LengthSquared));
                radius *= .75;

                Point3D[] controlPoints = Enumerable.Range(0, numPoints).
                    Select(o => Math3D.GetRandomVector(radius).ToPoint()).
                    ToArray();

                _shatter.Voronoi = Math3D.GetVoronoi(controlPoints, true);



                //TODO: Finish this
                _shatter.ShatteredHulls = null;



                DrawShatterAsteroid();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnShatterChains_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RemoveCurrentBody();

                Point3D[] points = new[]
                    {
                        new Point3D(-1.14, 1.52, -4.90),        // 0
                        new Point3D(-1.95, 1.65, -4.85),        // 1
                        new Point3D(-0.53, 1.42, -4.70),        // 2
                        new Point3D(-0.53, 1.44, -4.20),        // 3
                        new Point3D(-0.45, 1.12, -14.63),       // 4
                        new Point3D(-1.36, 1.16, -18.17),       // 5
                        new Point3D(-1.99, 1.69, -3.79),        // 6

                        new Point3D(-1.36, 1.1, -20),      // 7
                        new Point3D(-1.5, 1.3, -21),      // 8
                        new Point3D(-1.1, 1, -23),      // 9

                        new Point3D(-2, 3, -4),      // 10
                        new Point3D(-1, 4, -4),      // 11
                        new Point3D(-1.5, 3.5, -5),      // 12
                        new Point3D(0, 2, -3),      // 13
                    };

                var segments = new[]
                    {
                        new HullVoronoiIntersect.Segment_Face(0, 1, points),
                        new HullVoronoiIntersect.Segment_Face(2, 0, points),
                        new HullVoronoiIntersect.Segment_Face(3, 2, points),
                        new HullVoronoiIntersect.Segment_Face(2, 4, points),
                        new HullVoronoiIntersect.Segment_Face(5, 1, points),
                        new HullVoronoiIntersect.Segment_Face(1, 6, points),
                        new HullVoronoiIntersect.Segment_Face(3, 6, points),
                        new HullVoronoiIntersect.Segment_Face(5, 4, points),

                        // Extra chain 1
                        new HullVoronoiIntersect.Segment_Face(5, 7, points),
                        new HullVoronoiIntersect.Segment_Face(7, 8, points),
                        new HullVoronoiIntersect.Segment_Face(8, 9, points),

                        // Extra chain 2
                        new HullVoronoiIntersect.Segment_Face(10, 11, points),
                        new HullVoronoiIntersect.Segment_Face(12, 13, points),
                        new HullVoronoiIntersect.Segment_Face(10, 1, points),
                        new HullVoronoiIntersect.Segment_Face(11, 12, points),
                    };

                // Draw dots
                foreach (Point3D point in points)
                {
                    AddDot(point, DOTRADIUS, _colors.MedSlate);
                }

                // Draw lines
                ScreenSpaceLines3D lineVisual = new ScreenSpaceLines3D(true);
                lineVisual.Thickness = LINETHICKNESS;
                lineVisual.Color = _colors.DarkSlate;

                foreach (var segment in segments)
                {
                    lineVisual.AddLine(segment.Point0, segment.Point1);
                }

                _viewport.Children.Add(lineVisual);
                _visuals.Add(lineVisual);

                #region Attempt 1

                //var chains = ChainAttempt1.GetChains_Multiple(
                //    segments.Select(o => Tuple.Create(o.Index0, o.Index1, o)).ToArray(),        // these are the segments mapped to something the method can use
                //    (o, p) => o == p,       // int compare
                //    (o, p) => o.Index0 == p.Index0 && o.Index1 == p.Index1);        // face edge compare (can't use o.FaceEdge.Token, because FaceEdge could be null).  It's safe to always compare index0 with index0, because duplicate/reverse segments will never be created


                //foreach (var chain in chains)
                //{
                //    lineVisual = new ScreenSpaceLines3D(true);
                //    lineVisual.Thickness = LINETHICKNESS;
                //    lineVisual.Color = UtilityWPF.GetRandomColor(0, 255);

                //    foreach (var seg in chain.Item1)
                //    {
                //        lineVisual.AddLine(seg.Point0, seg.Point1);
                //    }

                //    _viewport.Children.Add(lineVisual);
                //    _visuals.Add(lineVisual);
                //}

                #endregion

                var chains = ChainAttempt2.GetChains_Multiple(
                    segments.Select(o => Tuple.Create(o.Index0, o.Index1, o)).ToArray(),        // these are the segments mapped to something the method can use
                    (o, p) => o == p,       // int compare
                    (o, p) => o.Index0 == p.Index0 && o.Index1 == p.Index1);        // face edge compare (can't use o.FaceEdge.Token, because FaceEdge could be null).  It's safe to always compare index0 with index0, because duplicate/reverse segments will never be created


                foreach (var chain in chains)
                {
                    lineVisual = new ScreenSpaceLines3D(true);
                    lineVisual.Thickness = LINETHICKNESS;
                    lineVisual.Color = UtilityWPF.GetRandomColor(0, 255);

                    foreach (var seg in chain.Item1)
                    {
                        lineVisual.AddLine(seg.Point0, seg.Point1);
                    }

                    _viewport.Children.Add(lineVisual);
                    _visuals.Add(lineVisual);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnShatterChains2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RemoveCurrentBody();

                // Define Segments
                var tests = new[]
                    {
                        #region tests
                        //new
                        //{
                        //    Points = new[]
                        //    {
                        //        new Point(0,0),     
                        //        new Point(-1,0),     
                        //        new Point(-1,-1),     
                        //        new Point(0,-1),     
                        //        new Point(1,-1),     
                        //        new Point(1,0),     
                        //        new Point(1,1),     
                        //        new Point(0,1),     
                        //        new Point(-1,1),     
                        //    },
                        //    Segments = Enumerable.Range(1,8).Select(o => Tuple.Create(0,o)).ToArray()
                        //},
                        #endregion

                        #region diamond - horz
                        new
                        {
                            Points = new[]
                            {
                                new Point(180,360),     //0
                                new Point(390,200),     //1
                                new Point(560,360),     //2
                                new Point(390,480),     //3
                            },
                            Segments = new[]
                            {
                                Tuple.Create(0,1),
                                Tuple.Create(1,2),
                                Tuple.Create(2,3),
                                Tuple.Create(0,3),
                                Tuple.Create(2,0)
                            },
                        },
                        #endregion
                        #region diamond - vert
                        new
                        {
                            Points = new[]
                            {
                                new Point(180,360),     //0
                                new Point(390,200),     //1
                                new Point(560,360),     //2
                                new Point(390,480),     //3
                            },
                            Segments = new[]
                            {
                                Tuple.Create(0,1),
                                Tuple.Create(1,2),
                                Tuple.Create(2,3),
                                Tuple.Create(0,3),
                                Tuple.Create(1,3)
                            },
                        },
                        #endregion
                        #region diamond - cross

                        //TODO: Build this

                        #endregion

                        #region complex 1
                        new
                        {
                            Points = new[]
                            {
                                new Point(95,390),     //0
                                new Point(95,500),     //1
                                new Point(335,220),     //2
                                new Point(272,490),     //3
                                new Point(200,390),     //4
                                new Point(200,438),     //5
                                new Point(350,330),     //6
                                new Point(350,231),     //7
                                new Point(525,375),     //8
                            },
                            Segments = new[]
                            {
                                Tuple.Create(0,1),
                                Tuple.Create(0,2),
                                Tuple.Create(0,5),
                                Tuple.Create(1,5),
                                Tuple.Create(4,5),
                                Tuple.Create(5,3),
                                Tuple.Create(4,6),
                                Tuple.Create(2,7),
                                Tuple.Create(6,7),
                                Tuple.Create(6,8),
                                Tuple.Create(3,8),
                                Tuple.Create(7,8),
                                Tuple.Create(4,3),
                            },
                        },
                        #endregion
                        #region complex 2
                        new
                        {
                            Points = new[]
                            {
                                new Point(85,340),     //0
                                new Point(190,410),     //1
                                new Point(190,355),     //2
                                new Point(335,225),     //3
                                new Point(390,460),     //4
                                new Point(525,320),     //5
                            },
                            Segments = new[]
                            {
                                Tuple.Create(0,1),
                                Tuple.Create(0,3),
                                Tuple.Create(1,2),
                                Tuple.Create(1,4),
                                Tuple.Create(2,4),
                                Tuple.Create(3,4),
                                Tuple.Create(3,5),
                                Tuple.Create(4,5),
                            },
                        },
                        #endregion
                        #region complex 3
                        new
                        {
                            Points = new[]
                            {
                                new Point(85,370),     //0
                                new Point(155,320),     //1
                                new Point(155,425),     //2
                                new Point(333,480),     //3
                                new Point(333,260),     //4
                                new Point(515,350),     //5
                                new Point(625,325),     //6
                                new Point(625,405),     //7
                            },
                            Segments = new[]
                            {
                                Tuple.Create(0,1),
                                Tuple.Create(0,2),
                                Tuple.Create(1,2),
                                Tuple.Create(1,4),
                                Tuple.Create(2,3),
                                Tuple.Create(3,4),
                                Tuple.Create(3,7),
                                Tuple.Create(4,5),
                                Tuple.Create(5,6),
                                Tuple.Create(6,7),
                            },
                        },
                        #endregion
                    };

                tests = tests.Skip(2).Take(1).ToArray();
                //tests = tests.Take(1).ToArray();

                // Turn into polygons
                var solved = tests.Select(o => new
                    {
                        Points = o.Points,
                        Segments = o.Segments,
                        Polygons = GetChains2D_3.GetSubPolygons(o.Segments, o.Points),
                    }).
                    ToArray();


                Transform3DGroup transform = new Transform3DGroup();
                transform.Children.Add(new ScaleTransform3D(.01, .01, .01));
                transform.Children.Add(new TranslateTransform3D(-18, -3.5, 0));

                ScreenSpaceLines3D lineVisual = new ScreenSpaceLines3D(true);
                lineVisual.Thickness = LINETHICKNESS;
                lineVisual.Color = _colors.DarkSlate;

                foreach (var set in solved)
                {
                    Point3D[] transformedPoints = set.Points.Select(o => transform.Transform(o.ToPoint3D())).ToArray();

                    // Draw lines
                    foreach (var segment in set.Segments)
                    {
                        lineVisual.AddLine(transformedPoints[segment.Item1], transformedPoints[segment.Item2]);
                    }

                    // Label the points
                    AddPointLabels(transformedPoints, .1);

                    // Draw polygons
                    foreach (var poly in set.Polygons.Item1)
                    {
                        Point[] polyPoints2D = poly.Select(o => set.Polygons.Item2[o]).ToArray();
                        Point3D[] polyPoints3D = polyPoints2D.Select(o => transform.Transform(o.ToPoint3D())).ToArray();        //NOTE: GetTrianglesFromConcavePoly only knows about the subset of triangles it's handed, so the 3D points need to be from that subset

                        ITriangleIndexed[] triangles = Math2D.GetTrianglesFromConcavePoly(polyPoints2D).
                            Select(o => new TriangleIndexed(o.Item1, o.Item2, o.Item3, polyPoints3D)).
                            ToArray();

                        AddHull_CustomColor(triangles, UtilityWPF.GetRandomColor(64, 0, 255), true, false, false, false, false, true);
                    }

                    transform.Children.Add(new TranslateTransform3D(7, 0, 0));
                }

                _viewport.Children.Add(lineVisual);
                _visuals.Add(lineVisual);





            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnVariousNormals_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RemoveCurrentBody();

                Point3D[] vertices = new Point3D[] { new Point3D(-1, -1, 0), new Point3D(1, -1, 0), new Point3D(0, 1, 0) };


                Triangle triangle1 = new Triangle(vertices[0], vertices[1], vertices[2]);
                TriangleIndexed triangle2 = new TriangleIndexed(0, 1, 2, vertices);


                Vector3D normal1 = triangle1.NormalUnit;
                Vector3D normal2 = triangle2.NormalUnit;
                //Vector3D normal3 = Math3D.Normal(new Vector3D[] { vertices[0].ToVector(), vertices[1].ToVector(), vertices[2].ToVector() });		// this one is left handed




            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnOutsideSet_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RemoveCurrentBody();

                // Create random points
                Point3D[] allPoints = new Point3D[(int)trkNumPoints.Value];
                for (int cntr = 0; cntr < allPoints.Length; cntr++)
                {
                    allPoints[cntr] = Math3D.GetRandomVector_Spherical(MAXRADIUS).ToPoint();
                }

                // Create Triangle
                // This part was copied from quickhull5
                TriangleIndexed triangle = new TriangleIndexed(0, 1, 2, allPoints);		// Make a triangle for 0,1,2
                if (Vector3D.DotProduct(allPoints[2].ToVector(), triangle.Normal) < 0d)		// See what side of the plane 3 is on
                {
                    triangle = new TriangleIndexed(0, 2, 1, allPoints);
                }

                // Get the outside set
                List<int> outsideSet = QuickHull5.GetOutsideSet(triangle, Enumerable.Range(0, allPoints.Length).ToList(), allPoints);

                // Get the farthest point from the triangle
                QuickHull5.TriangleWithPoints triangleWrapper = new QuickHull5.TriangleWithPoints(triangle);
                triangleWrapper.OutsidePoints.AddRange(outsideSet);
                int farthestIndex = QuickHull5.ProcessTriangleSprtFarthestPoint(triangleWrapper);

                // Draw the points (one color for inside, another for outside)
                for (int cntr = 0; cntr < allPoints.Length; cntr++)
                {
                    if (cntr == farthestIndex)
                    {
                        AddDot(allPoints[cntr], DOTRADIUS * 5d, Colors.Red);
                    }
                    else if (outsideSet.Contains(cntr))
                    {
                        AddDot(allPoints[cntr], DOTRADIUS * 2d, _colors.DarkSlate);
                    }
                    else
                    {
                        AddDot(allPoints[cntr], DOTRADIUS, _colors.MedSlate);
                    }
                }

                // Draw the triangle
                //Point3D centerPoint = ((triangle.Point0.ToVector() + triangle.Point1.ToVector() + triangle.Point2.ToVector()) / 3d).ToPoint();
                //AddLine(centerPoint, centerPoint + triangle.Normal, LINETHICKNESS, _colors.Normals);
                AddHull(new TriangleIndexed[] { triangle }, true, true, true, false, false, false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnStart3D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RemoveCurrentBody();

                List<Point3D> points = new List<Point3D>();

                for (int cntr = 0; cntr < trkNumPoints.Value; cntr++)
                {
                    points.Add(Math3D.GetRandomVector_Spherical(.6d * MAXRADIUS, MAXRADIUS).ToPoint());
                    AddDot(points[points.Count - 1], DOTRADIUS, _colors.MedSlate);
                }

                Point3D farthestPoint;
                TriangleIndexed removedTriangle;
                TriangleIndexed[] otherRemovedTriangles;
                TriangleIndexed[] hull = QuickHull5.GetQuickHullTest(out farthestPoint, out removedTriangle, out otherRemovedTriangles, points.ToArray());
                if (hull == null)
                {
                    return;
                }

                AddHull(hull, true, true, true, false, false, false);

                if (removedTriangle != null)
                {
                    AddHullTest(new TriangleIndexed[] { removedTriangle }, true, true, _colors.HullFaceRemoved, _colors.HullFaceSpecularRemoved, false);
                }

                if (otherRemovedTriangles != null)
                {
                    AddHullTest(otherRemovedTriangles, true, true, _colors.HullFaceOtherRemoved, _colors.HullFaceSpecularOtherRemoved, false);
                }

                AddDot(farthestPoint, DOTRADIUS * 5d, Colors.Red);

                //lblTriangleReport.Text = QuickHull5.TrianglePoint0.ToString(true) + "\r\n" + QuickHull5.TrianglePoint1.ToString(true) + "\r\n" + QuickHull5.TrianglePoint2.ToString(true);
                //lblTriangleReport.Text += string.Format("\r\nInvert 0={0}, 1={1}, 2={2}, 3={3}", QuickHull5.Inverted0.ToString(), QuickHull5.Inverted1.ToString(), QuickHull5.Inverted2.ToString(), QuickHull5.Inverted3.ToString());
                //lblTriangleReport.Visibility = Visibility.Visible;

                //if (!(QuickHull5.Inverted0 == QuickHull5.Inverted1 && QuickHull5.Inverted1 == QuickHull5.Inverted2 && QuickHull5.Inverted2 == QuickHull5.Inverted3))
                //{
                //    MessageBox.Show("check it");
                //}
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnFail3D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (trkNumPoints.Value > 60)
                {
                    if (MessageBox.Show("This will take a LONG time\r\n\r\nContinue?", this.Title, MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)
                    {
                        return;
                    }
                }

                RemoveCurrentBody();

                List<Point3D> points = new List<Point3D>();

                for (int cntr = 0; cntr < trkNumPoints.Value; cntr++)
                {
                    points.Add(Math3D.GetRandomVector_Spherical(.6d * MAXRADIUS, MAXRADIUS).ToPoint());
                    AddDot(points[points.Count - 1], DOTRADIUS, _colors.MedSlate);
                }

                List<TriangleIndexed[]> hulls = new List<TriangleIndexed[]>();
                while (points.Count > 3)
                {
                    TriangleIndexed[] hull = QuickHull5.GetQuickHull(points.ToArray());
                    if (hull == null)
                    {
                        break;
                    }

                    hulls.Add(hull);

                    // Prep for the next run
                    if (!chkConcentricHulls.IsChecked.Value)
                    {
                        lblTriangleReport.Text = string.Format("Points: {0}\r\nTriangles: {1}", points.Count.ToString("N0"), hull.Length.ToString("N0"));
                        lblTriangleReport.Visibility = Visibility.Visible;

                        break;
                    }

                    foreach (TriangleIndexed triangle in hull)
                    {
                        points.Remove(triangle.Point0);
                        points.Remove(triangle.Point1);
                        points.Remove(triangle.Point2);
                    }
                }

                // They must be added in reverse order so that the outermost one is added last (or the transparency fails)
                for (int cntr = hulls.Count - 1; cntr >= 0; cntr--)
                {
                    AddHull(hulls[cntr], true, true, false, true, false, false);

                    TriangleIndexed lastTriangle = hulls[cntr][hulls[cntr].Length - 1];
                    Point3D fromPoint = lastTriangle.GetCenterPoint();
                    Point3D toPoint = fromPoint + lastTriangle.Normal;
                    AddLine(fromPoint, toPoint, LINETHICKNESS, _colors.Normals);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btn3DAttempt6_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RemoveCurrentBody();

                List<Point3D> points = new List<Point3D>();

                for (int cntr = 0; cntr < trkNumPoints.Value; cntr++)
                {
                    points.Add(Math3D.GetRandomVector_Spherical(.6d * MAXRADIUS, MAXRADIUS).ToPoint());
                    AddDot(points[points.Count - 1], DOTRADIUS, _colors.MedSlate);
                }

                TriangleIndexed[] hull = QuickHull6.GetQuickHull(points.ToArray());

                AddHull(hull, true, false, false, false, false, false);


                #region Log the points

                string folderName = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                folderName = System.IO.Path.Combine(folderName, "Potato Tester");
                if (!System.IO.Directory.Exists(folderName))
                {
                    System.IO.Directory.CreateDirectory(folderName);
                }

                string fileName = System.IO.Path.Combine(folderName, DateTime.Now.ToString("yyyyMMdd hhmmssfff") + " - attempt6 points.txt");
                using (System.IO.StreamWriter writer = new System.IO.StreamWriter(fileName, false))
                {
                    foreach (Point3D point in points)
                    {
                        writer.WriteLine(point.ToString(true));
                    }
                }

                #endregion

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btn3DAttempt6FromFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                #region Get filename

                Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
                if (_prevAttempt6File == null)
                {
                    dialog.InitialDirectory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Potato Tester");
                }
                else
                {
                    dialog.FileName = _prevAttempt6File;
                }

                dialog.Filter = "Text Files|*.txt|All Files|*.*";

                bool? result = dialog.ShowDialog();
                if (result == null || !result.Value)
                {
                    return;
                }

                string filename = dialog.FileName;
                _prevAttempt6File = filename;

                #endregion

                RemoveCurrentBody();

                #region Read File

                List<Point3D> points = new List<Point3D>();

                using (System.IO.StreamReader reader = new System.IO.StreamReader(new System.IO.FileStream(filename, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite)))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.Trim() == "")
                        {
                            continue;
                        }

                        string[] lineSplit = line.Split(",".ToCharArray());

                        points.Add(new Point3D(double.Parse(lineSplit[0].Trim()), double.Parse(lineSplit[1].Trim()), double.Parse(lineSplit[2].Trim())));

                        AddDot(points[points.Count - 1], DOTRADIUS, _colors.MedSlate);
                    }
                }

                #endregion

                TriangleIndexed[] hull = QuickHull6.GetQuickHull(points.ToArray());

                AddHull(hull, true, true, true, false, false, false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnHullCoplanar3D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RemoveCurrentBody();

                #region Seeds

                List<Vector3D> seeds = new List<Vector3D>();

                for (int cntr = 0; cntr < 5; cntr++)
                {
                    seeds.Add(Math3D.GetRandomVector_Spherical(MAXRADIUS));
                }

                //string filename = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), Guid.NewGuid().ToString() + ".txt");
                //using (StreamWriter writer = new StreamWriter(filename, false))
                //{
                //    foreach (Vector3D seed in seeds)
                //    {
                //        writer.WriteLine(string.Format("seeds.Add(new Vector3D({0}, {1}, {2}));", seed.X.ToString(), seed.Y.ToString(), seed.Z.ToString()));
                //    }
                //}


                // Makes a convex shape
                //seeds.Add(new Vector3D(-6.13418541495184, 3.03904877007752, -3.33505554369484));
                //seeds.Add(new Vector3D(-5.26142042059921, 1.04813002910126, 8.15585398846667));
                //seeds.Add(new Vector3D(5.6147484615464, 5.38143218113028, 3.25425260401201));

                // Leaves an abondonded point
                //seeds.Add(new Vector3D(0.713522368731609, -3.27925607677954, -2.94485193678854));
                //seeds.Add(new Vector3D(-0.546270952731412, 5.95208652950101, -0.554781188177078));
                //seeds.Add(new Vector3D(-7.00070327046414, 2.23338317770599, -1.35619062070206));

                // Makes a convex shape and leaves an abandonded point
                //seeds.Add(new Vector3D(-7.57161225829881, 0.148610202190096, 0.616364243479363));
                //seeds.Add(new Vector3D(-1.13578481882369, 1.0110125692943, -8.07250656366576));
                //seeds.Add(new Vector3D(-4.51519718097258, 5.87249320352238, -3.15885908394965));

                // This was screwing up (some triangles were built backward)
                //seeds.Add(new Vector3D(-3.05681705374527, -1.02859045292147, 1.21525025154558));
                //seeds.Add(new Vector3D(-0.32665179497561, 0.526585142860568, 0.405072111091191));
                //seeds.Add(new Vector3D(-1.4057994914715, -0.342068911634135, -1.91853848540546));

                // This one can't find the farthest point (may not be an issue) - couldn't recreate using these points, not enough precision
                //seeds.Add(new Vector3D(-2.75148437489207, 3.11744323865764, 0.990357457098332));
                //seeds.Add(new Vector3D(2.00180062415458, 7.75232506225408, 1.91351276138827));
                //seeds.Add(new Vector3D(2.36452088846013, 1.27573207068764, 0.911452948072206));

                // This one missed a point
                //seeds.Add(new Vector3D(-2.78661683481391, 3.11224586772823, 6.96732377222608));
                //seeds.Add(new Vector3D(-5.10567180289431, -5.35921261948769, 4.94990438457728));
                //seeds.Add(new Vector3D(0.116756086702488, -0.139430820232176, -0.158165585474803));

                #endregion
                #region Generate Points

                List<Point3D> points = new List<Point3D>();

                // Make a nearly coplanar hull
                //points.Add(new Point3D(-1, -1, 0));
                //points.Add(new Point3D(1, -1, 0));
                //points.Add(new Point3D(0, 1, 0));
                //points.Add(new Point3D(0, 0, .00000000001));


                points.Add(new Point3D(0, 0, 0));

                foreach (int[] combo in UtilityCore.AllCombosEnumerator(seeds.Count))
                {
                    // Add up the vectors that this combo points to
                    Vector3D extremity = seeds[combo[0]];
                    for (int cntr = 1; cntr < combo.Length; cntr++)
                    {
                        extremity += seeds[combo[cntr]];
                    }

                    Point3D point = extremity.ToPoint();
                    if (!points.Contains(point))
                    {
                        points.Add(point);
                    }
                }

                #endregion

                if (chkDrawDots.IsChecked.Value)
                {
                    #region Add dots

                    for (int cntr = 0; cntr < points.Count; cntr++)
                    {
                        // Use 16 colors to help identify the dots.  Memories.......
                        //http://en.wikipedia.org/wiki/Enhanced_Graphics_Adapter
                        Color color;
                        switch (cntr)
                        {
                            case 0:
                                color = UtilityWPF.ColorFromHex("000000");
                                break;
                            case 1:
                                color = UtilityWPF.ColorFromHex("0000AA");
                                break;
                            case 2:
                                color = UtilityWPF.ColorFromHex("00AA00");
                                break;
                            case 3:
                                color = UtilityWPF.ColorFromHex("00AAAA");
                                break;
                            case 4:
                                color = UtilityWPF.ColorFromHex("AA0000");
                                break;
                            case 5:
                                color = UtilityWPF.ColorFromHex("AA00AA");
                                break;
                            case 6:
                                color = UtilityWPF.ColorFromHex("AA5500");
                                break;
                            case 7:
                                color = UtilityWPF.ColorFromHex("AAAAAA");
                                break;
                            case 8:
                                color = UtilityWPF.ColorFromHex("555555");
                                break;
                            case 9:
                                color = UtilityWPF.ColorFromHex("5555FF");
                                break;
                            case 10:
                                color = UtilityWPF.ColorFromHex("55FF55");
                                break;
                            case 11:
                                color = UtilityWPF.ColorFromHex("55FFFF");
                                break;
                            case 12:
                                color = UtilityWPF.ColorFromHex("FF5555");
                                break;
                            case 13:
                                color = UtilityWPF.ColorFromHex("FF55FF");
                                break;
                            case 14:
                                color = UtilityWPF.ColorFromHex("FFFF55");
                                break;
                            case 15:
                                color = UtilityWPF.ColorFromHex("FFFFFF");
                                break;
                            default:
                                color = _colors.MedSlate;
                                break;
                        }

                        AddDot(points[cntr], DOTRADIUS, color);
                    }

                    #endregion
                }

                List<TriangleIndexed[]> hulls = new List<TriangleIndexed[]>();
                while (points.Count > 3)
                {
                    //TriangleIndexed[] hull = QuickHull8.GetConvexHull(points.ToArray(), Convert.ToInt32(txtCoplanarMaxSteps.Text));
                    TriangleIndexed[] hull = Math3D.GetConvexHull(points.ToArray());
                    if (hull == null)
                    {
                        break;
                    }

                    hulls.Add(hull);

                    // Prep for the next run
                    if (!chkConcentricHulls.IsChecked.Value)
                    {
                        break;
                    }

                    foreach (TriangleIndexed triangle in hull)
                    {
                        points.Remove(triangle.Point0);
                        points.Remove(triangle.Point1);
                        points.Remove(triangle.Point2);
                    }
                }

                // They must be added in reverse order so that the outermost one is added last (or the transparency fails)
                for (int cntr = hulls.Count - 1; cntr >= 0; cntr--)
                {
                    AddHull(hulls[cntr], true, chkDrawLines.IsChecked.Value, chkDrawNormals.IsChecked.Value, false, chkNearlyTransparent.IsChecked.Value, chkSoftFaces.IsChecked.Value);
                }

                //TriangleIndexed[] hull = QuickHull6.GetQuickHull(points.ToArray());
                //AddHull(hull, chkDrawLines.IsChecked.Value, chkDrawNormals.IsChecked.Value, false, chkNearlyTransparent.IsChecked.Value);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private void RemoveCurrentBody()
        {
            _timerExplodeShatterAsteroid.Stop();
            _explodingSubHulls.Clear();

            foreach (ModelVisual3D visual in _visuals)
            {
                if (_viewport.Children.Contains(visual))
                {
                    _viewport.Children.Remove(visual);
                }
            }

            _visuals.Clear();

            pnlVisuals2D.Children.Clear();
        }

        private void AddDot(Point3D position, double radius, Color color)
        {
            // Material
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
            materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(color, Colors.White, .5d)), 50d));

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;
            geometry.Geometry = UtilityWPF.GetSphere_LatLon(3, radius, radius, radius);

            // Model Visual
            ModelVisual3D model = new ModelVisual3D();
            model.Content = geometry;
            model.Transform = new TranslateTransform3D(position.ToVector());

            // Temporarily add to the viewport
            _viewport.Children.Add(model);
            _visuals.Add(model);
        }
        private void AddLine(Point3D from, Point3D to, double thickness, Color color)
        {
            ScreenSpaceLines3D lineVisual = new ScreenSpaceLines3D(true);
            lineVisual.Thickness = thickness;
            lineVisual.Color = color;
            lineVisual.AddLine(from, to);

            _viewport.Children.Add(lineVisual);
            _visuals.Add(lineVisual);
        }
        private void AddLines(IEnumerable<Tuple<int, int>> lines, Point3D[] points)
        {
            // Draw the lines
            ScreenSpaceLines3D lineVisual = new ScreenSpaceLines3D(true);
            lineVisual.Thickness = LINETHICKNESS;
            lineVisual.Color = _colors.DarkSlate;

            foreach (var line in lines)
            {
                lineVisual.AddLine(points[line.Item1], points[line.Item2]);
            }

            _viewport.Children.Add(lineVisual);
            _visuals.Add(lineVisual);
        }
        private void AddLines(IEnumerable<Tuple<int, int>> lines, Point3D[] points, double thickness, Color color)
        {
            // Draw the lines
            ScreenSpaceLines3D lineVisual = new ScreenSpaceLines3D(true);
            lineVisual.Thickness = thickness;
            lineVisual.Color = color;

            foreach (var line in lines)
            {
                lineVisual.AddLine(points[line.Item1], points[line.Item2]);
            }

            _viewport.Children.Add(lineVisual);
            _visuals.Add(lineVisual);
        }
        private void AddHull(ITriangle[] triangles, bool drawLines, bool drawNormals)
        {
            if (drawLines)
            {
                #region Lines

                // Draw the lines
                ScreenSpaceLines3D lineVisual = new ScreenSpaceLines3D(true);
                lineVisual.Thickness = LINETHICKNESS;
                lineVisual.Color = _colors.DarkSlate;

                // TODO:  Dedupe the lines (that would be a good static method off of triangle
                foreach (ITriangle triangle in triangles)
                {
                    lineVisual.AddLine(triangle.Point0, triangle.Point1);
                    lineVisual.AddLine(triangle.Point1, triangle.Point2);
                    lineVisual.AddLine(triangle.Point2, triangle.Point0);
                }

                _viewport.Children.Add(lineVisual);
                _visuals.Add(lineVisual);

                #endregion
            }

            if (drawNormals)
            {
                #region Normals

                // Draw the lines
                ScreenSpaceLines3D lineVisual = new ScreenSpaceLines3D(true);
                lineVisual.Thickness = LINETHICKNESS;
                lineVisual.Color = _colors.Normals;

                foreach (ITriangle triangle in triangles)
                {
                    Point3D centerPoint = ((triangle.Point0.ToVector() + triangle.Point1.ToVector() + triangle.Point2.ToVector()) / 3d).ToPoint();
                    lineVisual.AddLine(centerPoint, centerPoint + triangle.Normal);
                }

                _viewport.Children.Add(lineVisual);
                _visuals.Add(lineVisual);

                #endregion
            }

            #region Faces

            // Material
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(_colors.HullFace)));
            materials.Children.Add(_colors.HullFaceSpecular);

            // Geometry Mesh
            MeshGeometry3D mesh = UtilityWPF.GetMeshFromTriangles_IndependentFaces(triangles);

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;
            geometry.Geometry = mesh;

            // Model Visual
            ModelVisual3D model = new ModelVisual3D();
            model.Content = geometry;

            _viewport.Children.Add(model);
            _visuals.Add(model);

            #endregion
        }
        private void AddHull(ITriangleIndexed[] triangles, bool drawFaces, bool drawLines, bool drawNormals, bool includeEveryOtherFace, bool nearlyTransparent, bool softFaces)
        {
            if (drawLines)
            {
                #region Lines

                // Draw the lines
                ScreenSpaceLines3D lineVisual = new ScreenSpaceLines3D(true);
                lineVisual.Thickness = nearlyTransparent ? LINETHICKNESS * .5d : LINETHICKNESS;
                lineVisual.Color = _colors.DarkSlate;

                Point3D[] points = triangles[0].AllPoints;

                foreach (var line in TriangleIndexed.GetUniqueLines(triangles))
                {
                    lineVisual.AddLine(points[line.Item1], points[line.Item2]);
                }

                _viewport.Children.Add(lineVisual);
                _visuals.Add(lineVisual);

                #endregion
            }

            if (drawNormals)
            {
                #region Normals

                // Draw the lines
                ScreenSpaceLines3D lineVisual = new ScreenSpaceLines3D(true);
                lineVisual.Thickness = nearlyTransparent ? LINETHICKNESS * .5d : LINETHICKNESS;
                lineVisual.Color = _colors.Normals;

                foreach (TriangleIndexed triangle in triangles)
                {
                    Point3D centerPoint = ((triangle.Point0.ToVector() + triangle.Point1.ToVector() + triangle.Point2.ToVector()) / 3d).ToPoint();
                    lineVisual.AddLine(centerPoint, centerPoint + triangle.Normal);
                }

                _viewport.Children.Add(lineVisual);
                _visuals.Add(lineVisual);

                #endregion
            }

            if (drawFaces)
            {
                #region Faces

                // Material
                MaterialGroup materials = new MaterialGroup();
                materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(nearlyTransparent ? _colors.HullFaceLight : _colors.HullFace)));
                if (softFaces)
                {
                    materials.Children.Add(_colors.HullFaceSpecularSoft);
                }
                else
                {
                    materials.Children.Add(_colors.HullFaceSpecular);
                }

                // Geometry Mesh
                MeshGeometry3D mesh = null;

                if (includeEveryOtherFace)
                {
                    List<ITriangleIndexed> trianglesEveryOther = new List<ITriangleIndexed>();
                    for (int cntr = 0; cntr < triangles.Length; cntr += 2)
                    {
                        trianglesEveryOther.Add(triangles[cntr]);
                    }

                    // Not supporting soft for every other (shared vertices with averaged normals)
                    mesh = UtilityWPF.GetMeshFromTriangles_IndependentFaces(trianglesEveryOther.ToArray());
                }
                else
                {
                    if (softFaces)
                    {
                        mesh = UtilityWPF.GetMeshFromTriangles(TriangleIndexed.Clone_CondensePoints(triangles));
                        //mesh = UtilityWPF.GetMeshFromTriangles(triangles);
                    }
                    else
                    {
                        mesh = UtilityWPF.GetMeshFromTriangles_IndependentFaces(triangles);
                    }
                }

                // Geometry Model
                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = materials;
                geometry.BackMaterial = materials;
                geometry.Geometry = mesh;

                // Model Visual
                ModelVisual3D visual = new ModelVisual3D();
                visual.Content = geometry;

                _viewport.Children.Add(visual);
                _visuals.Add(visual);

                #endregion
            }
        }
        private void AddHullTest(TriangleIndexed[] triangles, bool drawLines, bool drawNormals, Color faceColor, SpecularMaterial faceSpecular, bool softFaces)
        {
            if (drawLines)
            {
                #region Lines

                // Draw the lines
                ScreenSpaceLines3D lineVisual = new ScreenSpaceLines3D(true);
                lineVisual.Thickness = LINETHICKNESS;
                lineVisual.Color = _colors.DarkSlate;

                Point3D[] points = triangles[0].AllPoints;

                foreach (var line in TriangleIndexed.GetUniqueLines(triangles))
                {
                    lineVisual.AddLine(points[line.Item1], points[line.Item2]);
                }

                _viewport.Children.Add(lineVisual);
                _visuals.Add(lineVisual);

                #endregion
            }

            if (drawNormals)
            {
                #region Normals

                // Draw the lines
                ScreenSpaceLines3D lineVisual = new ScreenSpaceLines3D(true);
                lineVisual.Thickness = LINETHICKNESS;
                lineVisual.Color = _colors.Normals;

                foreach (TriangleIndexed triangle in triangles)
                {
                    Point3D centerPoint = ((triangle.Point0.ToVector() + triangle.Point1.ToVector() + triangle.Point2.ToVector()) / 3d).ToPoint();
                    lineVisual.AddLine(centerPoint, centerPoint + triangle.Normal);
                }

                _viewport.Children.Add(lineVisual);
                _visuals.Add(lineVisual);

                #endregion
            }

            #region Faces

            // Material
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(faceColor)));
            materials.Children.Add(faceSpecular);

            // Geometry Mesh
            MeshGeometry3D mesh;
            if (softFaces)
            {
                mesh = UtilityWPF.GetMeshFromTriangles(TriangleIndexed.Clone_CondensePoints(triangles));
            }
            else
            {
                mesh = UtilityWPF.GetMeshFromTriangles_IndependentFaces(triangles);
            }

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;
            geometry.Geometry = mesh;

            // Model Visual
            ModelVisual3D model = new ModelVisual3D();
            model.Content = geometry;

            _viewport.Children.Add(model);
            _visuals.Add(model);

            #endregion
        }
        private void AddHull_CustomColor(ITriangleIndexed[] triangles, Color color, bool drawFaces, bool drawLines, bool drawNormals, bool includeEveryOtherFace, bool nearlyTransparent, bool softFaces)
        {
            if (triangles.Length == 0)
            {
                return;
            }

            if (drawLines)
            {
                #region Lines

                // Draw the lines
                ScreenSpaceLines3D lineVisual = new ScreenSpaceLines3D(true);
                lineVisual.Thickness = nearlyTransparent ? LINETHICKNESS * .5d : LINETHICKNESS;
                lineVisual.Color = _colors.DarkSlate;

                Point3D[] points = triangles[0].AllPoints;

                foreach (var line in TriangleIndexed.GetUniqueLines(triangles))
                {
                    lineVisual.AddLine(points[line.Item1], points[line.Item2]);
                }

                _viewport.Children.Add(lineVisual);
                _visuals.Add(lineVisual);

                #endregion
            }

            if (drawNormals)
            {
                #region Normals

                // Draw the lines
                ScreenSpaceLines3D lineVisual = new ScreenSpaceLines3D(true);
                lineVisual.Thickness = nearlyTransparent ? LINETHICKNESS * .5d : LINETHICKNESS;
                lineVisual.Color = _colors.Normals;

                foreach (TriangleIndexed triangle in triangles)
                {
                    Point3D centerPoint = ((triangle.Point0.ToVector() + triangle.Point1.ToVector() + triangle.Point2.ToVector()) / 3d).ToPoint();
                    lineVisual.AddLine(centerPoint, centerPoint + triangle.Normal);
                }

                _viewport.Children.Add(lineVisual);
                _visuals.Add(lineVisual);

                #endregion
            }

            if (drawFaces)
            {
                #region Faces

                // Material
                MaterialGroup materials = new MaterialGroup();
                materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
                if (softFaces)
                {
                    materials.Children.Add(_colors.HullFaceSpecularSoft);
                }
                else
                {
                    materials.Children.Add(_colors.HullFaceSpecular);
                }

                // Geometry Mesh
                MeshGeometry3D mesh = null;

                if (includeEveryOtherFace)
                {
                    List<ITriangleIndexed> trianglesEveryOther = new List<ITriangleIndexed>();
                    for (int cntr = 0; cntr < triangles.Length; cntr += 2)
                    {
                        trianglesEveryOther.Add(triangles[cntr]);
                    }

                    // Not supporting soft for every other (shared vertices with averaged normals)
                    mesh = UtilityWPF.GetMeshFromTriangles_IndependentFaces(trianglesEveryOther.ToArray());
                }
                else
                {
                    if (softFaces)
                    {
                        mesh = UtilityWPF.GetMeshFromTriangles(TriangleIndexed.Clone_CondensePoints(triangles));
                        //mesh = UtilityWPF.GetMeshFromTriangles(triangles);
                    }
                    else
                    {
                        mesh = UtilityWPF.GetMeshFromTriangles_IndependentFaces(triangles);
                    }
                }

                // Geometry Model
                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = materials;
                geometry.BackMaterial = materials;
                geometry.Geometry = mesh;

                // Model Visual
                ModelVisual3D model = new ModelVisual3D();
                model.Content = geometry;

                _viewport.Children.Add(model);
                _visuals.Add(model);

                #endregion
            }
        }
        private void AddPlane(ITriangle plane, Color fillColor, Color reflectiveColor, Point3D? center = null, double size = MAXRADIUS * 2)
        {
            // Model Visual
            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = UtilityWPF.GetPlane(plane, size, fillColor, reflectiveColor, center: center);

            _viewport.Children.Add(visual);
            _visuals.Add(visual);
        }

        private void AddPointLabels(Point3D[] points, double depthScale = 1)
        {
            var ray = UtilityWPF.RayFromViewportPoint(_camera, _viewport, new Point(_viewport.ActualWidth * .5d, _viewport.ActualHeight * .5d));

            Vector3D planeNormalUnit = ray.Direction.ToUnit();
            double planeOriginDist = Math3D.GetPlaneOriginDistance(planeNormalUnit, ray.Origin);

            var distances = Enumerable.Range(0, points.Length).
                Select(o => new { Index = o, Point = points[o], Distance = Math3D.DistanceFromPlane(planeNormalUnit, planeOriginDist, points[o]) }).
                Where(o => o.Distance > 0d).
                ToArray();

            if (distances.Length == 0)
            {
                return;
            }

            double min = distances.Min(o => o.Distance);
            double max = distances.Max(o => o.Distance);

            // A value of zero will show no depth, a value of 1 will show maximum depth
            if (depthScale > 1) depthScale = 1;
            if (depthScale < 0) depthScale = 0;
            double halfDepthScale = depthScale / 2;
            double percentMin = .5 - halfDepthScale;
            double percentMax = .5 + halfDepthScale;

            foreach (var distance in distances)
            {
                //double percent = UtilityCore.GetScaledValue_Capped(0, 1, min, max, distance.Distance);
                double percent = UtilityCore.GetScaledValue_Capped(percentMin, percentMax, min, max, distance.Distance);

                percent = 1d - percent;     // min distance should have the largest percent (it's closest)

                AddLabel(distance.Point, distance.Index.ToString(), percent);
            }
        }
        private void AddLabel(Point3D position, string text)
        {
            bool isInFront;
            Point? position2D = UtilityWPF.Project3Dto2D(out isInFront, _viewport, position);

            if (position2D == null || !isInFront)
            {
                return;
            }

            // Text
            OutlinedTextBlock textblock = new OutlinedTextBlock();
            textblock.Text = text;
            textblock.FontFamily = _font.Value;
            textblock.FontSize = 24d;
            textblock.FontWeight = FontWeight.FromOpenTypeWeight(900);
            textblock.StrokeThickness = 1d;
            textblock.Fill = _colors.LabelFill;
            textblock.Stroke = _colors.LabelStroke;

            pnlVisuals2D.Children.Add(textblock);

            Canvas.SetLeft(textblock, position2D.Value.X - (textblock.ActualWidth / 2));
            Canvas.SetTop(textblock, position2D.Value.Y - (textblock.ActualHeight / 2));
        }
        private void AddLabel(Point3D position, string text, double percent)
        {
            bool isInFront;
            Point? position2D = UtilityWPF.Project3Dto2D(out isInFront, _viewport, position);

            if (position2D == null || !isInFront)
            {
                return;
            }

            // Text
            OutlinedTextBlock textblock = new OutlinedTextBlock();
            textblock.Text = text;
            textblock.FontFamily = _font.Value;

            textblock.FontSize = UtilityCore.GetScaledValue_Capped(13d, 30d, 0, 1, percent);

            int textWeight = Convert.ToInt32(Math.Round(UtilityCore.GetScaledValue_Capped(700, 998, 0, 1, percent)));
            textblock.FontWeight = FontWeight.FromOpenTypeWeight(textWeight);

            textblock.StrokeThickness = UtilityCore.GetScaledValue_Capped(.75d, 1.6d, 0, 1, percent);

            textblock.Fill = _colors.LabelFill;
            textblock.Stroke = _colors.LabelStroke;

            pnlVisuals2D.Children.Add(textblock);

            Canvas.SetLeft(textblock, position2D.Value.X - (textblock.ActualWidth / 2));
            Canvas.SetTop(textblock, position2D.Value.Y - (textblock.ActualHeight / 2));
        }
        private static FontFamily GetBestFont()
        {
            return UtilityWPF.GetFont(new string[] { "Lucida Console", "Verdana", "Microsoft Sans Serif", "Arial" });
        }

        private void DrawShatterAsteroid()
        {
            RemoveCurrentBody();

            if (_shatter == null)
            {
                return;
            }

            // Fragments
            var trisByCtrl = new Tuple<int, ITriangleIndexed[], Color, HullVoronoiIntersect.PatchFragment>[0];
            if (_shatter.Voronoi != null)
            {
                trisByCtrl = HullVoronoiIntersect.GetFragments(_shatter.Hull, _shatter.Voronoi).
                    Select(o => Tuple.Create(o.ControlPointIndex, o.Polygon.Select(p => p.Triangle).ToArray(), UtilityWPF.GetRandomColor(0, 255), o)).
                    ToArray();
            }

            #region Look at radios

            bool drawOrigFaces = true;
            bool drawTrianglesByCtrlPoint = false;
            bool drawHullsByCtrlPoint = false;
            bool explodeHullsByCtrlPoint = false;
            bool firstCtrlPointOnly3 = false;
            bool firstCtrlPointOnly4 = false;
            bool thinLines = false;
            bool drawVoronoi = true;
            if (_shatter.Voronoi == null || radShatterVoronoiLinesOnly.IsChecked.Value)
            {

            }
            else if (radShatterTriangleByCtrlPoint.IsChecked.Value)
            {
                drawOrigFaces = false;
                drawTrianglesByCtrlPoint = true;
            }
            else if (radShatterFirstCtrlPoint3.IsChecked.Value)
            {
                drawOrigFaces = false;
                drawTrianglesByCtrlPoint = true;
                firstCtrlPointOnly3 = true;
                thinLines = true;
            }
            else if (radShatterFirstMinimal3.IsChecked.Value)
            {
                drawVoronoi = false;
                drawOrigFaces = false;
                drawTrianglesByCtrlPoint = true;
                firstCtrlPointOnly3 = true;
                thinLines = true;
            }
            else if (radShatterFirstCtrlPoint4.IsChecked.Value)
            {
                drawOrigFaces = false;
                drawTrianglesByCtrlPoint = false;
                firstCtrlPointOnly4 = true;
                thinLines = true;
            }
            else if (radShatterFirstMinimal4.IsChecked.Value)
            {
                drawVoronoi = false;
                drawOrigFaces = false;
                drawTrianglesByCtrlPoint = false;
                firstCtrlPointOnly4 = true;
                thinLines = true;
            }
            else if (radShatterFull.IsChecked.Value)
            {
                drawOrigFaces = false;
                drawHullsByCtrlPoint = true;
            }
            else if (radShatterFullExploded.IsChecked.Value)
            {
                drawOrigFaces = false;
                drawVoronoi = false;
                explodeHullsByCtrlPoint = true;
                thinLines = true;
            }
            else
            {
                throw new ApplicationException("Unknown radio button selection");
            }

            #endregion

            #region Draw voronoi

            if (_shatter.Voronoi != null && drawVoronoi)
            {
                for (int cntr = 0; cntr < _shatter.Voronoi.ControlPoints.Length; cntr++)
                {
                    Color color = Colors.GhostWhite;

                    var colorMatch = trisByCtrl.FirstOrDefault(o => o.Item1 == cntr);
                    if (drawTrianglesByCtrlPoint && colorMatch != null)     // don't worry about firstonly flag here.  Color all affected dots
                    {
                        color = colorMatch.Item3;
                    }

                    AddDot(_shatter.Voronoi.ControlPoints[cntr], DOTRADIUS * 3, color);
                }

                // I think it's a bit overkill to get unique, because these already should be unique.  But the method also converts the
                // rays into segments
                var segments = Edge3D.GetUniqueLines(_shatter.Voronoi.Edges, 1000);

                AddLines(segments.Item1, segments.Item2, LINETHICKNESS, Colors.GhostWhite);

                // Probably don't need to draw the faces.  It would just get busy
                //_shatter.Voronoi.Faces
            }

            #endregion
            #region Draw dots

            if (chkDrawDots.IsChecked.Value)
            {
                foreach (Point3D point in _shatter.HullPoints)
                {
                    AddDot(point, DOTRADIUS, _colors.MedSlate);
                }
            }

            #endregion
            #region Draw hull

            AddHull(_shatter.Hull, drawOrigFaces, chkDrawLines.IsChecked.Value, chkDrawNormals.IsChecked.Value, false, thinLines || chkNearlyTransparent.IsChecked.Value, chkSoftFaces.IsChecked.Value);

            if (_shatter.Voronoi != null && drawTrianglesByCtrlPoint)
            {
                foreach (var set in trisByCtrl)
                {
                    AddHull_CustomColor(set.Item2, UtilityWPF.AlphaBlend(set.Item3, Colors.Transparent, .75), true, false, chkDrawNormals.IsChecked.Value, false, false, chkSoftFaces.IsChecked.Value);

                    if (firstCtrlPointOnly3 || firstCtrlPointOnly4)
                    {
                        break;
                    }
                }
            }

            if (_shatter.Voronoi != null && (drawHullsByCtrlPoint || explodeHullsByCtrlPoint))
            {
                Tuple<int, ITriangleIndexed[]>[] fullShatter = Math3D.GetIntersection_Hull_Voronoi_full(_shatter.Hull, _shatter.Voronoi);

                if (drawHullsByCtrlPoint)
                {
                    foreach (var subShatter in fullShatter)
                    {
                        var set = trisByCtrl.FirstOrDefault(o => o.Item1 == subShatter.Item1);
                        if(set == null)
                        {
                            set = Tuple.Create(subShatter.Item1, (ITriangleIndexed[])subShatter.Item2, UtilityWPF.GetRandomColor(0, 255), (HullVoronoiIntersect.PatchFragment)null);
                        }

                        AddHull_CustomColor(subShatter.Item2, UtilityWPF.AlphaBlend(set.Item3, Colors.Transparent, .75), true, false, chkDrawNormals.IsChecked.Value, false, false, chkSoftFaces.IsChecked.Value);
                    }
                }
                else if (explodeHullsByCtrlPoint)
                {
                    // This needs to add a translate transform to each sub hull.  So out of laziness, I copied parts of AddHull_CustomColor here

                    Model3DGroup group = new Model3DGroup();

                    Point3D hullCenter = Math3D.GetCenter(TriangleIndexed.GetUsedPoints(_shatter.Hull));

                    foreach (var subShatter in fullShatter)
                    {
                        var set = trisByCtrl.FirstOrDefault(o => o.Item1 == subShatter.Item1);
                        if (set == null)
                        {
                            set = Tuple.Create(subShatter.Item1, (ITriangleIndexed[])subShatter.Item2, UtilityWPF.GetRandomColor(0, 255), (HullVoronoiIntersect.PatchFragment)null);
                        }

                        // Material
                        MaterialGroup materials = new MaterialGroup();
                        materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(set.Item3, Colors.Transparent, .75))));
                        materials.Children.Add(_colors.HullFaceSpecular);

                        // Geometry Mesh
                        MeshGeometry3D mesh = null;
                        if (chkSoftFaces.IsChecked.Value)
                        {
                            mesh = UtilityWPF.GetMeshFromTriangles(TriangleIndexed.Clone_CondensePoints(subShatter.Item2));
                        }
                        else
                        {
                            mesh = UtilityWPF.GetMeshFromTriangles_IndependentFaces(subShatter.Item2);
                        }

                        // Geometry Model
                        GeometryModel3D geometry = new GeometryModel3D();
                        geometry.Material = materials;
                        geometry.BackMaterial = materials;
                        geometry.Geometry = mesh;

                        Point3D subCenter = Math3D.GetCenter(TriangleIndexed.GetUsedPoints(subShatter.Item2));
                        Vector3D direction = (subCenter - hullCenter) * 2.5;      // multiply the distance a bit

                        TranslateTransform3D transform = new TranslateTransform3D();
                        geometry.Transform = transform;

                        group.Children.Add(geometry);
                        _explodingSubHulls.Add(Tuple.Create(transform, direction));
                    }

                    // Model Visual
                    ModelVisual3D model = new ModelVisual3D();
                    model.Content = group;

                    _viewport.Children.Add(model);
                    _visuals.Add(model);

                    _explodePercent = 0;
                    _explodeIsUp = true;
                    _explodeNextTransition = DateTime.UtcNow + TimeSpan.FromSeconds(1);
                    _timerExplodeShatterAsteroid.Start();
                }
            }

            #endregion

            #region First Control Point 3

            if (firstCtrlPointOnly3)
            {
                //TODO: Draw some debug face polys (with less color)
                //var facePolys = HullVoronoiIntersect.GetTestFacePolys(trisByCtrl[0].Item4, _shatter.Hull, _shatter.Voronoi);
                var facePolys = HullVoronoiIntersect.GetTestFacePolys3(trisByCtrl[0].Item4, _shatter.Hull, _shatter.Voronoi);

                ITriangleIndexed[] faceTriangles = facePolys.SelectMany(o => o.Poly1).ToArray();

                //Color color = UtilityWPF.AlphaBlend(trisByCtrl[0].Item3, Colors.Transparent, .5);
                Color color = UtilityWPF.ColorFromHex("E8FFFFFF");

                AddHull_CustomColor(faceTriangles, color, true, false, chkDrawNormals.IsChecked.Value, false, false, chkSoftFaces.IsChecked.Value);

                //var facePolys2 = HullVoronoiIntersect.GetTestFacePolys2(trisByCtrl[0].Item4, _shatter.Hull, _shatter.Voronoi);
            }

            #endregion
            #region First Control Point 4

            if (firstCtrlPointOnly4)
            {
                TriangleIndexed[] hull4 = HullVoronoiIntersect.GetTestFacePolys4(trisByCtrl[0].Item4, trisByCtrl[0].Item2, _shatter.Hull, _shatter.Voronoi);

                AddHull_CustomColor(hull4, UtilityWPF.AlphaBlend(trisByCtrl[0].Item3, Colors.Transparent, .75), true, false, chkDrawNormals.IsChecked.Value, false, false, chkSoftFaces.IsChecked.Value);
            }

            #endregion
        }

        private void DrawMultipleHulls(ITriangleIndexed[][] hulls, bool drawFaces, bool drawLines, bool drawNormals, bool includeEveryOtherFace, bool nearlyTransparent, bool softFaces, bool newTrianglesAsDifferentColor = true)
        {
            var aabb = Math3D.GetAABB(hulls.SelectMany(o => o));

            double width = aabb.Item2.X - aabb.Item1.X;

            double gap = width * .125;

            double totalWidth = (width * hulls.Length) + (gap * (hulls.Length - 1));

            double offset = totalWidth / -4;        // half the half width

            long[] prevTokens = null;
            Color otherColor = UtilityWPF.AlphaBlend(Colors.LightGreen, Colors.Transparent, .8);

            for (int cntr = 0; cntr < hulls.Length; cntr++)
            {
                TranslateTransform3D transform = new TranslateTransform3D(offset, 0, 0);

                #region Detect null neighbors

                if (hulls[cntr][0] is TriangleIndexedLinked)
                {
                    var bads = hulls[cntr].
                        Select(o => (TriangleIndexedLinked)o).
                        Select(o => new
                        {
                            Triangle = o,
                            Edges = Triangle.Edges.Select(p => new
                                {
                                    Edge = p,
                                    Neighbor = o.GetNeighbor(p),
                                    Point1 = o.GetPoint(p, true),
                                    Point2 = o.GetPoint(p, false)
                                }).Where(p => p.Neighbor == null).ToArray()
                        }).
                        Where(o => o.Edges.Length > 0).
                        ToArray();

                    if (bads.Length > 0)
                    {
                        Point3D[] points = bads.
                            SelectMany(o => o.Edges).
                            SelectMany(o => new[] { transform.Transform(o.Point1), transform.Transform(o.Point2) }).
                            ToArray();

                        var lines = Enumerable.Range(0, points.Length / 2).
                            Select(o => Tuple.Create(o * 2, (o * 2) + 1));

                        AddLines(lines, points, LINETHICKNESS * 3, Colors.Red);
                    }
                }

                #endregion

                ITriangleIndexed[] translated = hulls[cntr].Select(o => new TriangleIndexed(o.Index0, o.Index1, o.Index2, o.AllPoints.Select(p => transform.Transform(p)).ToArray())).ToArray();       // inneficient, but easy

                if (newTrianglesAsDifferentColor)
                {
                    long[] curTokens = hulls[cntr].Select(o => o.Token).ToArray();

                    if (cntr > 0)
                    {
                        // New color
                        ITriangleIndexed[] subset = translated.Where((o, i) => !prevTokens.Contains(hulls[cntr][i].Token)).ToArray();
                        if (subset.Length > 0)
                        {
                            AddHull_CustomColor(subset, otherColor, drawFaces, drawLines, drawNormals, includeEveryOtherFace, nearlyTransparent, softFaces);
                        }

                        // Standard color
                        subset = translated.Where((o, i) => prevTokens.Contains(hulls[cntr][i].Token)).ToArray();
                        if (subset.Length > 0)
                        {
                            AddHull(subset, drawFaces, drawLines, drawNormals, includeEveryOtherFace, nearlyTransparent, softFaces);
                        }
                    }
                    else
                    {
                        // First one, so nothing to compare to
                        AddHull(translated, drawFaces, drawLines, drawNormals, includeEveryOtherFace, nearlyTransparent, softFaces);
                    }

                    prevTokens = curTokens;
                }
                else
                {
                    // Do all the same color
                    AddHull(translated, drawFaces, drawLines, drawNormals, includeEveryOtherFace, nearlyTransparent, softFaces);
                }

                offset += width + gap;
            }
        }

        private static void GroupCoplanarTriangles(int index, TriangleIndexedLinked triangle, List<TrianglesInPlane> groups)
        {
            // Find an existing group that this fits in
            foreach (TrianglesInPlane group in groups)
            {
                if (group.ShouldAdd(triangle))
                {
                    group.AddTriangle(index, triangle);
                    return;
                }
            }

            // This triangle needs to be in its own group
            TrianglesInPlane newGroup = new TrianglesInPlane();
            newGroup.AddTriangle(index, triangle);
            groups.Add(newGroup);
        }

        private static TriangleIndexed[] GetRandomHull()
        {
            const double MINRADIUS = MAXRADIUS * .5d;

            MeshGeometry3D mesh = null;

            switch (StaticRandom.Next(5))
            {
                case 0:
                    mesh = UtilityWPF.GetCube(GetRandomSize(MINRADIUS, MAXRADIUS));
                    break;

                case 1:
                    mesh = UtilityWPF.GetCylinder_AlongX(12, GetRandomSize(MINRADIUS, MAXRADIUS), GetRandomSize(MINRADIUS, MAXRADIUS));
                    break;

                case 2:
                    mesh = UtilityWPF.GetCone_AlongX(12, GetRandomSize(MINRADIUS, MAXRADIUS), GetRandomSize(MINRADIUS, MAXRADIUS));
                    break;

                case 3:
                    mesh = UtilityWPF.GetSphere_LatLon(5, GetRandomSize(MINRADIUS, MAXRADIUS));
                    break;

                case 4:
                    double innerRadius = GetRandomSize(MINRADIUS, MAXRADIUS);
                    double outerRadius = GetRandomSize(innerRadius, MAXRADIUS);
                    mesh = UtilityWPF.GetTorus(20, 20, innerRadius, outerRadius);
                    break;

                //case 5:
                //mesh = UtilityWPF.GetMultiRingedTube();
                //break;

                default:
                    throw new ApplicationException("Unexpected random number");
            }

            // Come up with a random rotation
            Transform3D transform = new RotateTransform3D(new QuaternionRotation3D(Math3D.GetRandomRotation()));

            // Exit Function
            return UtilityWPF.GetTrianglesFromMesh(mesh, transform);
        }

        private static double GetRandomSize(double minSize, double maxSize)
        {
            return minSize + (StaticRandom.NextDouble() * (maxSize - minSize));
        }

        #endregion
    }

    #region quickhull attempts

    #region Class: QuickHull1

    //PseudoCode
    //Locate leftmost, rightmost, lowest, and highest points
    //Connect these points with a quadrilateral
    //Run QuickHull on the four triangular regions exterior to the quadrilateral

    //function QuickHull(a,b,s)
    //    if S={a,b} then return(a,b)
    //    else
    //        c = index of right of (a,c)
    //        A = points right of (a,c)
    //        B = points right of (a,b)
    //        return QuickHull(a,c,A) concatenated with QuickHull(c,b,B)

    public static class QuickHull1
    {
        /// <summary>
        /// Hand this a set of points, and it will return a set of lines that define a convex hull around those points
        /// </summary>
        public static List<Point[]> GetConvexHull2D(List<Point> points)
        {
            if (points.Count < 2)
            {
                throw new ArgumentNullException("At least 2 points need to be passed to this method");
            }

            // Get the leftmost and rightmost point
            int leftmost = GetLeftmost(points);
            int rightmost = GetRightmost(points);

            if (leftmost == rightmost)
            {
                throw new ApplicationException("handle this:  leftmost and rightmost are the same");
            }







            throw new ApplicationException("finish this");




        }

        #region Private Methods

        private static int GetLeftmost(List<Point> points)
        {
            double minX = double.MaxValue;
            int retVal = -1;

            for (int cntr = 0; cntr < points.Count; cntr++)
            {
                if (points[cntr].X < minX)
                {
                    minX = points[cntr].X;
                    retVal = cntr;
                }
            }

            return retVal;
        }
        private static int GetRightmost(List<Point> points)
        {
            double maxX = double.MinValue;
            int retVal = -1;

            for (int cntr = 0; cntr < points.Count; cntr++)
            {
                if (points[cntr].X > maxX)
                {
                    maxX = points[cntr].X;
                    retVal = cntr;
                }
            }

            return retVal;
        }

        private static int GetFurthestFromLine(Point lineStart, Point lineStop, List<Point> points)
        {
            Point3D pointOnLine = new Point3D(lineStart.X, lineStart.Y, 0d);
            Vector3D lineDirection = new Point3D(lineStop.X, lineStop.Y, 0d) - pointOnLine;

            double longestDistance = double.MinValue;
            int retVal = -1;

            for (int cntr = 0; cntr < points.Count; cntr++)
            {
                Point3D point = new Point3D(points[cntr].X, points[cntr].Y, 0d);
                Point3D nearestPoint = Math3D.GetClosestPoint_Line_Point(pointOnLine, lineDirection, point);
                double lengthSquared = (point - nearestPoint).LengthSquared;

                if (lengthSquared > longestDistance)
                {
                    longestDistance = lengthSquared;
                    retVal = cntr;
                }
            }

            return retVal;
        }

        #endregion
    }

    #endregion
    #region Class: QuickHull2

    /// <remarks>
    /// Got this here (ported it from java):
    /// http://www.ahristov.com/tutorial/geometry-games/convex-hull.html
    /// </remarks>
    public static class QuickHull2
    {
        public static List<Point3D> GetQuickHull2D(List<Point3D> points)
        {
            // Convert to 2D
            List<Point> points2D = new List<Point>();
            foreach (Point3D point in points)
            {
                points2D.Add(new Point(point.X, point.Y));
            }

            // Call quickhull
            List<Point> returnList2D = GetQuickHull2D(points2D);

            // Convert to 3D
            List<Point3D> retVal = new List<Point3D>();
            foreach (Point point in returnList2D)
            {
                retVal.Add(new Point3D(point.X, point.Y, 0d));
            }

            // Exit Function
            return retVal;
        }
        public static List<Point> GetQuickHull2D(List<Point> points)
        {
            if (points.Count < 3)
            {
                return new List<Point>(points);		// clone it
            }

            List<Point> retVal = new List<Point>();

            #region Find two most extreme points

            int minIndex = -1;
            int maxIndex = -1;
            double minX = double.MaxValue;
            double maxX = double.MinValue;
            for (int cntr = 0; cntr < points.Count; cntr++)
            {
                if (points[cntr].X < minX)
                {
                    minX = points[cntr].X;
                    minIndex = cntr;
                }

                if (points[cntr].X > maxX)
                {
                    maxX = points[cntr].X;
                    maxIndex = cntr;
                }
            }

            #endregion

            #region Move points to return list

            Point minPoint = points[minIndex];
            Point maxPoint = points[maxIndex];
            retVal.Add(minPoint);
            retVal.Add(maxPoint);

            if (maxIndex > minIndex)
            {
                points.RemoveAt(maxIndex);		// need to remove the later index first so it doesn't shift
                points.RemoveAt(minIndex);
            }
            else
            {
                points.RemoveAt(minIndex);
                points.RemoveAt(maxIndex);
            }

            #endregion

            #region Divide the list left and right of the line

            List<Point> leftSet = new List<Point>();
            List<Point> rightSet = new List<Point>();

            for (int cntr = 0; cntr < points.Count; cntr++)
            {
                if (IsRightOfLine(minPoint, maxPoint, points[cntr]))
                {
                    rightSet.Add(points[cntr]);
                }
                else
                {
                    leftSet.Add(points[cntr]);
                }
            }

            #endregion

            // Process these sets recursively, adding to retVal
            HullSet(minPoint, maxPoint, rightSet, retVal);
            HullSet(maxPoint, minPoint, leftSet, retVal);

            // Exit Function
            return retVal;
        }

        private static void HullSet(Point A, Point B, List<Point> set, List<Point> hull)
        {
            int insertPosition = hull.IndexOf(B);  //TODO:  take in the index.  it's safer

            if (set.Count == 0)
            {
                return;
            }
            else if (set.Count == 1)
            {
                Point p = set[0];
                set.RemoveAt(0);
                hull.Insert(insertPosition, p);
                return;
            }

            #region Find most distant point

            double maxDistance = double.MinValue;
            int furthestIndex = -1;
            for (int i = 0; i < set.Count; i++)
            {
                Point p = set[i];
                double distance = GetDistanceFromLineSquared(A, B, p);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    furthestIndex = i;
                }
            }

            // Move the point to the hull
            Point P = set[furthestIndex];
            set.RemoveAt(furthestIndex);
            hull.Insert(insertPosition, P);

            #endregion

            // Determine who's to the left of AP
            List<Point> leftSetAP = new List<Point>();
            for (int i = 0; i < set.Count; i++)
            {
                Point M = set[i];
                if (IsRightOfLine(A, P, M))
                {
                    //set.remove(M);
                    leftSetAP.Add(M);
                }
            }

            // Determine who's to the left of PB
            List<Point> leftSetPB = new List<Point>();
            for (int i = 0; i < set.Count; i++)
            {
                Point M = set[i];
                if (IsRightOfLine(P, B, M))
                {
                    //set.remove(M);
                    leftSetPB.Add(M);
                }
            }

            // Recurse
            HullSet(A, P, leftSetAP, hull);
            HullSet(P, B, leftSetPB, hull);
        }

        private static bool IsRightOfLine(Point lineStart, Point lineStop, Point testPoint)
        {
            double cp1 = ((lineStop.X - lineStart.X) * (testPoint.Y - lineStart.Y)) - ((lineStop.Y - lineStart.Y) * (testPoint.X - lineStart.X));

            return cp1 > 0;
            //return (cp1 > 0) ? 1 : -1;
        }

        //TODO:  This is ineficient
        private static double GetDistanceFromLineSquared(Point lineStart, Point lineStop, Point testPoint)
        {
            Point3D pointOnLine = new Point3D(lineStart.X, lineStart.Y, 0d);
            Vector3D lineDirection = new Point3D(lineStop.X, lineStop.Y, 0d) - pointOnLine;
            Point3D point = new Point3D(testPoint.X, testPoint.Y, 0d);

            Point3D nearestPoint = Math3D.GetClosestPoint_Line_Point(pointOnLine, lineDirection, point);

            return (point - nearestPoint).LengthSquared;
        }
    }

    #endregion
    #region Class: QuickHull2a

    /// <remarks>
    /// Got this here (ported it from java):
    /// http://www.ahristov.com/tutorial/geometry-games/convex-hull.html
    /// </remarks>
    public static class QuickHull2a
    {
        public static int[] GetQuickHull2D(Point3D[] points)
        {
            // Convert to 2D
            Point[] points2D = new Point[points.Length];
            for (int cntr = 0; cntr < points.Length; cntr++)
            {
                points2D[cntr] = new Point(points[cntr].X, points[cntr].Y);
            }

            // Call quickhull
            return GetQuickHull2D(points2D);
        }
        public static int[] GetQuickHull2D(Point[] points)
        {
            if (points.Length < 3)
            {
                return Enumerable.Range(0, points.Length).ToArray();		// return all the points
            }

            List<int> retVal = new List<int>();
            List<int> remainingPoints = Enumerable.Range(0, points.Length).ToList();

            #region Find two most extreme points

            int minIndex = -1;
            int maxIndex = -1;
            double minX = double.MaxValue;
            double maxX = double.MinValue;
            for (int cntr = 0; cntr < points.Length; cntr++)
            {
                if (points[cntr].X < minX)
                {
                    minX = points[cntr].X;
                    minIndex = cntr;
                }

                if (points[cntr].X > maxX)
                {
                    maxX = points[cntr].X;
                    maxIndex = cntr;
                }
            }

            #endregion

            #region Move points to return list

            retVal.Add(minIndex);
            retVal.Add(maxIndex);

            if (maxIndex > minIndex)
            {
                remainingPoints.RemoveAt(maxIndex);		// need to remove the later index first so it doesn't shift
                remainingPoints.RemoveAt(minIndex);
            }
            else
            {
                remainingPoints.RemoveAt(minIndex);
                remainingPoints.RemoveAt(maxIndex);
            }

            #endregion

            #region Divide the list left and right of the line

            List<int> leftSet = new List<int>();
            List<int> rightSet = new List<int>();

            for (int cntr = 0; cntr < remainingPoints.Count; cntr++)
            {
                if (IsRightOfLine(minIndex, maxIndex, remainingPoints[cntr], points))
                {
                    rightSet.Add(remainingPoints[cntr]);
                }
                else
                {
                    leftSet.Add(remainingPoints[cntr]);
                }
            }

            #endregion

            // Process these sets recursively, adding to retVal
            HullSet(minIndex, maxIndex, rightSet, retVal, points);
            HullSet(maxIndex, minIndex, leftSet, retVal, points);

            // Exit Function
            return retVal.ToArray();
        }

        #region Private Methods

        private static void HullSet(int lineStart, int lineStop, List<int> set, List<int> hull, Point[] allPoints)
        {
            int insertPosition = hull.IndexOf(lineStop);

            if (set.Count == 0)
            {
                return;
            }
            else if (set.Count == 1)
            {
                hull.Insert(insertPosition, set[0]);
                set.RemoveAt(0);
                return;
            }

            #region Find most distant point

            double maxDistance = double.MinValue;
            int farIndexIndex = -1;
            for (int cntr = 0; cntr < set.Count; cntr++)
            {
                int point = set[cntr];
                double distance = GetDistanceFromLineSquared(allPoints[lineStart], allPoints[lineStop], allPoints[point]);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    farIndexIndex = cntr;
                }
            }

            // Move the point to the hull
            int farIndex = set[farIndexIndex];
            set.RemoveAt(farIndexIndex);
            hull.Insert(insertPosition, farIndex);

            #endregion

            #region Find everything left of (Start, Far)

            List<int> leftSet_Start_Far = new List<int>();
            for (int cntr = 0; cntr < set.Count; cntr++)
            {
                int pointIndex = set[cntr];
                if (IsRightOfLine(lineStart, farIndex, pointIndex, allPoints))
                {
                    leftSet_Start_Far.Add(pointIndex);
                }
            }

            #endregion

            #region Find everything right of (Far, Stop)

            List<int> leftSet_Far_Stop = new List<int>();
            for (int cntr = 0; cntr < set.Count; cntr++)
            {
                int pointIndex = set[cntr];
                if (IsRightOfLine(farIndex, lineStop, pointIndex, allPoints))
                {
                    leftSet_Far_Stop.Add(pointIndex);
                }
            }

            #endregion

            // Recurse
            //NOTE: The set passed in was split into these two sets
            HullSet(lineStart, farIndex, leftSet_Start_Far, hull, allPoints);
            HullSet(farIndex, lineStop, leftSet_Far_Stop, hull, allPoints);
        }

        private static bool IsRightOfLine(int lineStart, int lineStop, int testPoint, Point[] allPoints)
        {
            double cp1 = ((allPoints[lineStop].X - allPoints[lineStart].X) * (allPoints[testPoint].Y - allPoints[lineStart].Y)) -
                                  ((allPoints[lineStop].Y - allPoints[lineStart].Y) * (allPoints[testPoint].X - allPoints[lineStart].X));

            return cp1 > 0;
            //return (cp1 > 0) ? 1 : -1;
        }

        private static double GetDistanceFromLineSquared(Point lineStart, Point lineStop, Point testPoint)
        {
            Point3D pointOnLine = new Point3D(lineStart.X, lineStart.Y, 0d);
            Vector3D lineDirection = new Point3D(lineStop.X, lineStop.Y, 0d) - pointOnLine;
            Point3D point = new Point3D(testPoint.X, testPoint.Y, 0d);

            Point3D nearestPoint = Math3D.GetClosestPoint_Line_Point(pointOnLine, lineDirection, point);

            return (point - nearestPoint).LengthSquared;
        }

        #endregion
    }

    #endregion
    #region Class: QuickHull3

    //   //This came from:
    //   //http://www.cs.ubc.ca/~lloyd/java/quickhull3d.html

    //   /// <summary>
    //   /// 
    //   /// </summary>
    //   /// <remarks>
    //   /// 
    //   /// Copyright John E. Lloyd, 2004. All rights reserved. Permission to use,
    //   /// copy, modify and redistribute is granted, provided that this copyright
    //   /// notice is retained and the author is given credit whenever appropriate.
    //   /// 
    //   /// This  software is distributed "as is", without any warranty, including 
    //   /// any implied warranty of merchantability or fitness for a particular
    //   /// use. The author assumes no responsibility for, and shall not be liable
    //   /// for, any special, indirect, or consequential damages, or any damages
    //   /// whatsoever, arising out of or in connection with the use of this
    //   /// software.
    //   /// 
    //   /// 
    //   /// 
    //   /// 
    //   /// 
    //   /// 
    //   /// </remarks>
    //   public class QuickHull3
    //   {
    //       #region Declaration Section

    //       /**
    //                       * Precision of a double.
    //                       */
    //       static private readonly double DOUBLE_PREC = 2.2204460492503131e-16;

    //       /**
    //        * Specifies that (on output) vertex indices for a face should be
    //        * listed in clockwise order.
    //        */
    //       public static readonly int CLOCKWISE = 0x1;

    //       /**
    //        * Specifies that (on output) the vertex indices for a face should be
    //        * numbered starting from 1.
    //        */
    //       public static readonly int INDEXED_FROM_ONE = 0x2;

    //       /**
    //        * Specifies that (on output) the vertex indices for a face should be
    //        * numbered starting from 0.
    //        */
    //       public static readonly int INDEXED_FROM_ZERO = 0x4;

    //       /**
    //        * Specifies that (on output) the vertex indices for a face should be
    //        * numbered with respect to the original input points.
    //        */
    //       public static readonly int POINT_RELATIVE = 0x8;

    //       /**
    //        * Specifies that the distance tolerance should be
    //        * computed automatically from the input point data.
    //        */
    //       public static readonly double AUTOMATIC_TOLERANCE = -1;

    //       private static readonly int NONCONVEX_WRT_LARGER_FACE = 1;
    //       private static readonly int NONCONVEX = 2;

    //       protected int findIndex = -1;

    //       // estimated size of the point set
    //       protected double charLength;

    //       protected boolean debug = false;

    //       protected Vertex[] pointBuffer = new Vertex[0];
    //       protected int[] vertexPointIndices = new int[0];
    //       private Face[] discardedFaces = new Face[3];

    //       private Vertex[] maxVtxs = new Vertex[3];
    //       private Vertex[] minVtxs = new Vertex[3];

    //       protected Vector faces = new Vector(16);
    //       protected Vector horizon = new Vector(16);

    //       private FaceList newFaces = new FaceList();
    //       private VertexList unclaimed = new VertexList();
    //       private VertexList claimed = new VertexList();

    //       protected int numVertices;
    //       protected int numFaces;
    //       protected int numPoints;

    //       protected double explicitTolerance = AUTOMATIC_TOLERANCE;
    //       protected double tolerance;

    //       #endregion

    //       #region Constructor

    //       /**
    //                       * Creates an empty convex hull object.
    //                       */
    //       public QuickHull3()
    //       {
    //       }

    //       /**
    //        * Creates a convex hull object and initializes it to the convex hull
    //        * of a set of points whose coordinates are given by an
    //        * array of doubles.
    //        *
    //        * @param coords x, y, and z coordinates of each input
    //        * point. The length of this array will be three times
    //        * the the number of input points.
    //        * @throws IllegalArgumentException the number of input points is less
    //        * than four, or the points appear to be coincident, colinear, or
    //        * coplanar.
    //        */
    //       public QuickHull3(double[] coords)
    //       {
    //           build(coords, coords.Length / 3);
    //       }

    //       /**
    //        * Creates a convex hull object and initializes it to the convex hull
    //        * of a set of points.
    //        *
    //        * @param points input points.
    //        * @throws IllegalArgumentException the number of input points is less
    //        * than four, or the points appear to be coincident, colinear, or
    //        * coplanar.
    //        */
    //       public QuickHull3(Point3d[] points)
    //       {
    //           build(points, points.Length);
    //       }

    //       #endregion

    //       #region Public Properties

    //       /**
    //                       * Returns true if debugging is enabled.
    //                       *
    //                       * @return true is debugging is enabled
    //                       * @see QuickHull3D#setDebug
    //                       */
    //       public boolean getDebug()
    //       {
    //           return debug;
    //       }
    //       /**
    //        * Enables the printing of debugging diagnostics.
    //        *
    //        * @param enable if true, enables debugging
    //        */
    //       public void setDebug(boolean enable)
    //       {
    //           debug = enable;
    //       }

    //       /**
    //        * Returns the distance tolerance that was used for the most recently
    //        * computed hull. The distance tolerance is used to determine when
    //        * faces are unambiguously convex with respect to each other, and when
    //        * points are unambiguously above or below a face plane, in the
    //        * presence of <a href=#distTol>numerical imprecision</a>. Normally,
    //        * this tolerance is computed automatically for each set of input
    //        * points, but it can be set explicitly by the application.
    //        *
    //        * @return distance tolerance
    //        * @see QuickHull3D#setExplicitDistanceTolerance
    //        */
    //       public double getDistanceTolerance()
    //       {
    //           return tolerance;
    //       }

    //       /**
    //        * Returns the explicit distance tolerance.
    //        *
    //        * @return explicit tolerance
    //        * @see #setExplicitDistanceTolerance
    //        */
    //       public double getExplicitDistanceTolerance()
    //       {
    //           return explicitTolerance;
    //       }
    //       /**
    //        * Sets an explicit distance tolerance for convexity tests.
    //        * If {@link #AUTOMATIC_TOLERANCE AUTOMATIC_TOLERANCE}
    //        * is specified (the default), then the tolerance will be computed
    //        * automatically from the point data.
    //        *
    //        * @param tol explicit tolerance
    //        * @see #getDistanceTolerance
    //        */
    //       public void setExplicitDistanceTolerance(double tol)
    //       {
    //           explicitTolerance = tol;
    //       }

    //       #endregion

    //       #region Public Methods

    //       /**
    //                       * Constructs the convex hull of a set of points whose
    //                       * coordinates are given by an array of doubles.
    //                       *
    //                       * @param coords x, y, and z coordinates of each input
    //                       * point. The length of this array will be three times
    //                       * the number of input points.
    //                       * @throws IllegalArgumentException the number of input points is less
    //                       * than four, or the points appear to be coincident, colinear, or
    //                       * coplanar.
    //                       */
    //       public void build(double[] coords)
    //       {
    //           build(coords, coords.Length / 3);
    //       }
    //       /**
    //* Constructs the convex hull of a set of points whose
    //* coordinates are given by an array of doubles.
    //*
    //* @param coords x, y, and z coordinates of each input
    //* point. The length of this array must be at least three times
    //* <code>nump</code>.
    //* @param nump number of input points
    //* @throws IllegalArgumentException the number of input points is less
    //* than four or greater than 1/3 the length of <code>coords</code>,
    //* or the points appear to be coincident, colinear, or
    //* coplanar.
    //*/
    //       public void build(double[] coords, int nump)
    //       {
    //           if (nump < 4)
    //           {
    //               throw new ArgumentException("Less than four input points specified");
    //           }
    //           if (coords.Length / 3 < nump)
    //           {
    //               throw new ArgumentException("Coordinate array too small for specified number of points");
    //           }
    //           initBuffers(nump);
    //           setPoints(coords, nump);
    //           buildHull();
    //       }
    //       /**
    //        * Constructs the convex hull of a set of points.
    //        *
    //        * @param points input points
    //        * @throws IllegalArgumentException the number of input points is less
    //        * than four, or the points appear to be coincident, colinear, or
    //        * coplanar.
    //        */
    //       public void build(Point3d[] points)
    //       {
    //           build(points, points.Length);
    //       }
    //       /**
    //        * Constructs the convex hull of a set of points.
    //        *
    //        * @param points input points
    //        * @param nump number of input points
    //        * @throws IllegalArgumentException the number of input points is less
    //        * than four or greater then the length of <code>points</code>, or the
    //        * points appear to be coincident, colinear, or coplanar.
    //        */
    //       public void build(Point3d[] points, int nump)
    //       {
    //           if (nump < 4)
    //           {
    //               throw new ArgumentException("Less than four input points specified");
    //           }
    //           if (points.Length < nump)
    //           {
    //               throw new ArgumentException("Point array too small for specified number of points");
    //           }
    //           initBuffers(nump);
    //           setPoints(points, nump);
    //           buildHull();
    //       }

    //       /**
    //   * Triangulates any non-triangular hull faces. In some cases, due to
    //   * precision issues, the resulting triangles may be very thin or small,
    //   * and hence appear to be non-convex (this same limitation is present
    //   * in <a href=http://www.qhull.org>qhull</a>).
    //   */
    //       public void triangulate()
    //       {
    //           double minArea = 1000 * charLength * DOUBLE_PREC;
    //           newFaces.clear();
    //           for (Iterator it = faces.iterator(); it.hasNext(); )
    //           {
    //               Face face = (Face)it.next();
    //               if (face.mark == Face.VISIBLE)
    //               {
    //                   face.triangulate(newFaces, minArea);
    //                   // splitFace (face);
    //               }
    //           }
    //           for (Face face = newFaces.first(); face != null; face = face.next)
    //           {
    //               faces.add(face);
    //           }
    //       }

    //       /**
    //   * Returns the number of vertices in this hull.
    //   *
    //   * @return number of vertices
    //   */
    //       public int getNumVertices()
    //       {
    //           return numVertices;
    //       }

    //       /**
    //        * Returns the vertex points in this hull.
    //        *
    //        * @return array of vertex points
    //        * @see QuickHull3D#getVertices(double[])
    //        * @see QuickHull3D#getFaces()
    //        */
    //       public Point3d[] getVertices()
    //       {
    //           Point3d[] vtxs = new Point3d[numVertices];
    //           for (int i = 0; i < numVertices; i++)
    //           {
    //               vtxs[i] = pointBuffer[vertexPointIndices[i]].pnt;
    //           }
    //           return vtxs;
    //       }

    //       /**
    //        * Returns the coordinates of the vertex points of this hull.
    //        *
    //        * @param coords returns the x, y, z coordinates of each vertex.
    //        * This length of this array must be at least three times
    //        * the number of vertices.
    //        * @return the number of vertices
    //        * @see QuickHull3D#getVertices()
    //        * @see QuickHull3D#getFaces()
    //        */
    //       public int getVertices(double[] coords)
    //       {
    //           for (int i = 0; i < numVertices; i++)
    //           {
    //               Point3d pnt = pointBuffer[vertexPointIndices[i]].pnt;
    //               coords[i * 3 + 0] = pnt.x;
    //               coords[i * 3 + 1] = pnt.y;
    //               coords[i * 3 + 2] = pnt.z;
    //           }
    //           return numVertices;
    //       }

    //       /**
    //        * Returns an array specifing the index of each hull vertex
    //        * with respect to the original input points.
    //        *
    //        * @return vertex indices with respect to the original points
    //        */
    //       public int[] getVertexPointIndices()
    //       {
    //           int[] indices = new int[numVertices];
    //           for (int i = 0; i < numVertices; i++)
    //           {
    //               indices[i] = vertexPointIndices[i];
    //           }
    //           return indices;
    //       }

    //       /**
    //        * Returns the number of faces in this hull.
    //        *
    //        * @return number of faces
    //        */
    //       public int getNumFaces()
    //       {
    //           return faces.size();
    //       }

    //       /**
    //        * Returns the faces associated with this hull.
    //        *
    //        * <p>Each face is represented by an integer array which gives the
    //        * indices of the vertices. These indices are numbered
    //        * relative to the
    //        * hull vertices, are zero-based,
    //        * and are arranged counter-clockwise. More control
    //        * over the index format can be obtained using
    //        * {@link #getFaces(int) getFaces(indexFlags)}.
    //        *
    //        * @return array of integer arrays, giving the vertex
    //        * indices for each face.
    //        * @see QuickHull3D#getVertices()
    //        * @see QuickHull3D#getFaces(int)
    //        */
    //       public int[][] getFaces()
    //       {
    //           return getFaces(0);
    //       }

    //       /**
    //        * Returns the faces associated with this hull.
    //        *
    //        * <p>Each face is represented by an integer array which gives the
    //        * indices of the vertices. By default, these indices are numbered with
    //        * respect to the hull vertices (as opposed to the input points), are
    //        * zero-based, and are arranged counter-clockwise. However, this
    //        * can be changed by setting {@link #POINT_RELATIVE
    //        * POINT_RELATIVE}, {@link #INDEXED_FROM_ONE INDEXED_FROM_ONE}, or
    //        * {@link #CLOCKWISE CLOCKWISE} in the indexFlags parameter.
    //        *
    //        * @param indexFlags specifies index characteristics (0 results
    //        * in the default)
    //        * @return array of integer arrays, giving the vertex
    //        * indices for each face.
    //        * @see QuickHull3D#getVertices()
    //        */
    //       public int[][] getFaces(int indexFlags)
    //       {
    //           int[][] allFaces = new int[faces.size()][];
    //           int k = 0;
    //           for (Iterator it = faces.iterator(); it.hasNext(); )
    //           {
    //               Face face = (Face)it.next();
    //               allFaces[k] = new int[face.numVertices()];
    //               getFaceIndices(allFaces[k], face, indexFlags);
    //               k++;
    //           }
    //           return allFaces;
    //       }

    //       #endregion
    //       #region Protected Methods

    //       protected void buildHull()
    //                       {
    //                         int cnt = 0;
    //                         Vertex eyeVtx;

    //                         computeMaxAndMin ();
    //                         createInitialSimplex ();
    //                         while ((eyeVtx = nextPointToAdd()) != null)
    //                          { addPointToHull (eyeVtx);
    //                            cnt++;
    //                            if (debug)
    //                             { System.out.println ("iteration " + cnt + " done"); 
    //                             }
    //                          }
    //                         reindexFacesAndVertices();
    //                         if (debug)
    //                          { System.out.println ("hull done");
    //                          }
    //                       }

    //       protected void setHull(double[] coords, int nump, int[][] faceIndices, int numf)
    //       {
    //           initBuffers(nump);
    //           setPoints(coords, nump);
    //           computeMaxAndMin();
    //           for (int i = 0; i < numf; i++)
    //           {
    //               Face face = Face.create(pointBuffer, faceIndices[i]);
    //               HalfEdge he = face.he0;
    //               do
    //               {
    //                   HalfEdge heOpp = findHalfEdge(he.head(), he.tail());
    //                   if (heOpp != null)
    //                   {
    //                       he.setOpposite(heOpp);
    //                   }
    //                   he = he.next;
    //               }
    //               while (he != face.he0);
    //               faces.add(face);
    //           }
    //       }

    //       protected void setFromQhull(double[] coords, int nump, boolean triangulate)
    //                       {
    //                         String commandStr = "./qhull i";
    //                         if (triangulate)
    //                          { commandStr += " -Qt"; 
    //                          }
    //                         try
    //                          { 
    //                            Process proc = Runtime.getRuntime().exec (commandStr);
    //                            PrintStream ps = new PrintStream (proc.getOutputStream());
    //                            StreamTokenizer stok =
    //                           new StreamTokenizer (
    //                              new InputStreamReader (proc.getInputStream()));

    //                            ps.println ("3 " + nump);
    //                            for (int i=0; i<nump; i++)
    //                             { ps.println (
    //                              coords[i*3+0] + " " +
    //                              coords[i*3+1] + " " +  
    //                              coords[i*3+2]);
    //                             }
    //                            ps.flush();
    //                            ps.close();
    //                            Vector indexList = new Vector(3);
    //                            stok.eolIsSignificant(true);
    //                            printQhullErrors (proc);

    //                            do
    //                             { stok.nextToken();
    //                             }
    //                            while (stok.sval == null ||
    //                               !stok.sval.startsWith ("MERGEexact"));
    //                            for (int i=0; i<4; i++)
    //                             { stok.nextToken();
    //                             }
    //                            if (stok.ttype != StreamTokenizer.TT_NUMBER)
    //                             { System.out.println ("Expecting number of faces");
    //                           System.exit(1); 
    //                             }
    //                            int numf = (int)stok.nval;
    //                            stok.nextToken(); // clear EOL
    //                            int[][] faceIndices = new int[numf][];
    //                            for (int i=0; i<numf; i++)
    //                             { indexList.clear();
    //                           while (stok.nextToken() != StreamTokenizer.TT_EOL)
    //                            { if (stok.ttype != StreamTokenizer.TT_NUMBER)
    //                               { System.out.println ("Expecting face index");
    //                                 System.exit(1); 
    //                               }
    //                              indexList.add (0, new Integer((int)stok.nval));
    //                            }
    //                           faceIndices[i] = new int[indexList.size()];
    //                           int k = 0;
    //                           for (Iterator it=indexList.iterator(); it.hasNext(); ) 
    //                            { faceIndices[i][k++] = ((Integer)it.next()).intValue();
    //                            }
    //                             }
    //                            setHull (coords, nump, faceIndices, numf);
    //                          }
    //                         catch (Exception e) 
    //                          { e.printStackTrace();
    //                            System.exit(1); 
    //                          }
    //                       }

    //       protected void initBuffers(int nump)
    //       {
    //           if (pointBuffer.length < nump)
    //           {
    //               Vertex[] newBuffer = new Vertex[nump];
    //               vertexPointIndices = new int[nump];
    //               for (int i = 0; i < pointBuffer.length; i++)
    //               {
    //                   newBuffer[i] = pointBuffer[i];
    //               }
    //               for (int i = pointBuffer.length; i < nump; i++)
    //               {
    //                   newBuffer[i] = new Vertex();
    //               }
    //               pointBuffer = newBuffer;
    //           }
    //           faces.clear();
    //           claimed.clear();
    //           numFaces = 0;
    //           numPoints = nump;
    //       }

    //       protected void setPoints(double[] coords, int nump)
    //       {
    //           for (int i = 0; i < nump; i++)
    //           {
    //               Vertex vtx = pointBuffer[i];
    //               vtx.pnt.set(coords[i * 3 + 0], coords[i * 3 + 1], coords[i * 3 + 2]);
    //               vtx.index = i;
    //           }
    //       }

    //       protected void setPoints(Point3d[] pnts, int nump)
    //       {
    //           for (int i = 0; i < nump; i++)
    //           {
    //               Vertex vtx = pointBuffer[i];
    //               vtx.pnt.set(pnts[i]);
    //               vtx.index = i;
    //           }
    //       }

    //       protected void computeMaxAndMin()
    //       {
    //           Vector3d max = new Vector3d();
    //           Vector3d min = new Vector3d();

    //           for (int i = 0; i < 3; i++)
    //           {
    //               maxVtxs[i] = minVtxs[i] = pointBuffer[0];
    //           }
    //           max.set(pointBuffer[0].pnt);
    //           min.set(pointBuffer[0].pnt);

    //           for (int i = 1; i < numPoints; i++)
    //           {
    //               Point3d pnt = pointBuffer[i].pnt;
    //               if (pnt.x > max.x)
    //               {
    //                   max.x = pnt.x;
    //                   maxVtxs[0] = pointBuffer[i];
    //               }
    //               else if (pnt.x < min.x)
    //               {
    //                   min.x = pnt.x;
    //                   minVtxs[0] = pointBuffer[i];
    //               }
    //               if (pnt.y > max.y)
    //               {
    //                   max.y = pnt.y;
    //                   maxVtxs[1] = pointBuffer[i];
    //               }
    //               else if (pnt.y < min.y)
    //               {
    //                   min.y = pnt.y;
    //                   minVtxs[1] = pointBuffer[i];
    //               }
    //               if (pnt.z > max.z)
    //               {
    //                   max.z = pnt.z;
    //                   maxVtxs[2] = pointBuffer[i];
    //               }
    //               else if (pnt.z < min.z)
    //               {
    //                   min.z = pnt.z;
    //                   maxVtxs[2] = pointBuffer[i];
    //               }
    //           }

    //           // this epsilon formula comes from QuickHull, and I'm
    //           // not about to quibble.
    //           charLength = Math.max(max.x - min.x, max.y - min.y);
    //           charLength = Math.max(max.z - min.z, charLength);
    //           if (explicitTolerance == AUTOMATIC_TOLERANCE)
    //           {
    //               tolerance =
    //                   3 * DOUBLE_PREC * (Math.max(Math.abs(max.x), Math.abs(min.x)) +
    //                     Math.max(Math.abs(max.y), Math.abs(min.y)) +
    //                     Math.max(Math.abs(max.z), Math.abs(min.z)));
    //           }
    //           else
    //           {
    //               tolerance = explicitTolerance;
    //           }
    //       }

    //       /**
    //        * Creates the initial simplex from which the hull will be built.
    //        */
    //       protected void createInitialSimplex()
    //                       {
    //                         double max = 0;
    //                         int imax = 0;

    //                         for (int i=0; i<3; i++)
    //                          { double diff = maxVtxs[i].pnt.get(i)-minVtxs[i].pnt.get(i);
    //                            if (diff > max)
    //                             { max = diff;
    //                           imax = i;
    //                             }
    //                          }

    //                         if (max <= tolerance)
    //                          { throw new ArgumentException ("Input points appear to be coincident");
    //                          }



    //                         Vertex[] vtx = new Vertex[4];
    //                         // set first two vertices to be those with the greatest
    //                         // one dimensional separation

    //                         vtx[0] = maxVtxs[imax];
    //                         vtx[1] = minVtxs[imax];

    //                         // set third vertex to be the vertex farthest from
    //                         // the line between vtx0 and vtx1
    //                         Vector3d u01 = new Vector3d();
    //                         Vector3d diff02 = new Vector3d();
    //                         Vector3d nrml = new Vector3d();
    //                         Vector3d xprod = new Vector3d();
    //                         double maxSqr = 0;
    //                         u01.sub (vtx[1].pnt, vtx[0].pnt);
    //                         u01.normalize();
    //                         for (int i=0; i<numPoints; i++)
    //                          { diff02.sub (pointBuffer[i].pnt, vtx[0].pnt);
    //                            xprod.cross (u01, diff02);
    //                            double lenSqr = xprod.normSquared();
    //                            if (lenSqr > maxSqr &&
    //                            pointBuffer[i] != vtx[0] &&  // paranoid
    //                            pointBuffer[i] != vtx[1])
    //                             { maxSqr = lenSqr; 
    //                           vtx[2] = pointBuffer[i];
    //                           nrml.set (xprod);
    //                             }
    //                          }
    //                         if (Math.sqrt(maxSqr) <= 100*tolerance)
    //                          { throw new ArgumentException ("Input points appear to be colinear");
    //                          }
    //                         nrml.normalize();


    //                         double maxDist = 0;
    //                         double d0 = vtx[2].pnt.dot (nrml);
    //                         for (int i=0; i<numPoints; i++)
    //                          { double dist = Math.abs (pointBuffer[i].pnt.dot(nrml) - d0);
    //                            if (dist > maxDist &&
    //                            pointBuffer[i] != vtx[0] &&  // paranoid
    //                            pointBuffer[i] != vtx[1] &&
    //                            pointBuffer[i] != vtx[2])
    //                             { maxDist = dist;
    //                           vtx[3] = pointBuffer[i];
    //                             }
    //                          }
    //                         if (Math.abs(maxDist) <= 100*tolerance)
    //                          { throw new ArgumentException ("Input points appear to be coplanar"); 
    //                          }

    //                         if (debug)
    //                          { System.out.println ("initial vertices:");
    //                            System.out.println (vtx[0].index + ": " + vtx[0].pnt);
    //                            System.out.println (vtx[1].index + ": " + vtx[1].pnt);
    //                            System.out.println (vtx[2].index + ": " + vtx[2].pnt);
    //                            System.out.println (vtx[3].index + ": " + vtx[3].pnt);
    //                          }

    //                         Face[] tris = new Face[4];

    //                         if (vtx[3].pnt.dot (nrml) - d0 < 0)
    //                          { tris[0] = Face.createTriangle (vtx[0], vtx[1], vtx[2]);
    //                            tris[1] = Face.createTriangle (vtx[3], vtx[1], vtx[0]);
    //                            tris[2] = Face.createTriangle (vtx[3], vtx[2], vtx[1]);
    //                            tris[3] = Face.createTriangle (vtx[3], vtx[0], vtx[2]);

    //                            for (int i=0; i<3; i++)
    //                             { int k = (i+1)%3;
    //                           tris[i+1].getEdge(1).setOpposite (tris[k+1].getEdge(0));
    //                           tris[i+1].getEdge(2).setOpposite (tris[0].getEdge(k));
    //                             }
    //                          }
    //                         else
    //                          { tris[0] = Face.createTriangle (vtx[0], vtx[2], vtx[1]);
    //                            tris[1] = Face.createTriangle (vtx[3], vtx[0], vtx[1]);
    //                            tris[2] = Face.createTriangle (vtx[3], vtx[1], vtx[2]);
    //                            tris[3] = Face.createTriangle (vtx[3], vtx[2], vtx[0]);

    //                            for (int i=0; i<3; i++)
    //                             { int k = (i+1)%3;
    //                           tris[i+1].getEdge(0).setOpposite (tris[k+1].getEdge(1));
    //                           tris[i+1].getEdge(2).setOpposite (tris[0].getEdge((3-i)%3));
    //                             }
    //                          }


    //                         for (int i=0; i<4; i++)
    //                          { faces.add (tris[i]); 
    //                          }

    //                         for (int i=0; i<numPoints; i++)
    //                          { Vertex v = pointBuffer[i];

    //                            if (v == vtx[0] || v == vtx[1] || v == vtx[2] || v == vtx[3])
    //                             { continue;
    //                             }

    //                            maxDist = tolerance;
    //                            Face maxFace = null;
    //                            for (int k=0; k<4; k++)
    //                             { double dist = tris[k].distanceToPlane (v.pnt);
    //                           if (dist > maxDist)
    //                            { maxFace = tris[k];
    //                              maxDist = dist;
    //                            }
    //                             }
    //                            if (maxFace != null)
    //                             { addPointToFace (v, maxFace);
    //                             }	      
    //                          }
    //                       }

    //       protected void resolveUnclaimedPoints(FaceList newFaces)
    //           {
    //             Vertex vtxNext = unclaimed.first();
    //             for (Vertex vtx=vtxNext; vtx!=null; vtx=vtxNext)
    //              { vtxNext = vtx.next;

    //                double maxDist = tolerance;
    //                Face maxFace = null;
    //                for (Face newFace=newFaces.first(); newFace != null;
    //                 newFace=newFace.next)
    //                 { 
    //               if (newFace.mark == Face.VISIBLE)
    //                { double dist = newFace.distanceToPlane(vtx.pnt);
    //                  if (dist > maxDist)
    //                   { maxDist = dist;
    //                     maxFace = newFace;
    //                   }
    //                  if (maxDist > 1000*tolerance)
    //                   { break;
    //                   }
    //                }
    //                 }
    //                if (maxFace != null)
    //                 { 
    //               addPointToFace (vtx, maxFace);
    //               if (debug && vtx.index == findIndex)
    //                { System.out.println (findIndex + " CLAIMED BY " +
    //                   maxFace.getVertexString()); 
    //                }
    //                 }
    //                else
    //                 { if (debug && vtx.index == findIndex)
    //                { System.out.println (findIndex + " DISCARDED"); 
    //                } 
    //                 }
    //              }
    //           }

    //       protected void deleteFacePoints(Face face, Face absorbingFace)
    //       {
    //           Vertex faceVtxs = removeAllPointsFromFace(face);
    //           if (faceVtxs != null)
    //           {
    //               if (absorbingFace == null)
    //               {
    //                   unclaimed.addAll(faceVtxs);
    //               }
    //               else
    //               {
    //                   Vertex vtxNext = faceVtxs;
    //                   for (Vertex vtx = vtxNext; vtx != null; vtx = vtxNext)
    //                   {
    //                       vtxNext = vtx.next;
    //                       double dist = absorbingFace.distanceToPlane(vtx.pnt);
    //                       if (dist > tolerance)
    //                       {
    //                           addPointToFace(vtx, absorbingFace);
    //                       }
    //                       else
    //                       {
    //                           unclaimed.add(vtx);
    //                       }
    //                   }
    //               }
    //           }
    //       }

    //       protected double oppFaceDistance(HalfEdge he)
    //       {
    //           return he.face.distanceToPlane(he.opposite.face.getCentroid());
    //       }

    //       protected void calculateHorizon(Point3d eyePnt, HalfEdge edge0, Face face, Vector horizon)
    //           {
    //              //	   oldFaces.add (face);
    //             deleteFacePoints (face, null);
    //             face.mark = Face.DELETED;
    //             if (debug)
    //              { System.out.println ("  visiting face " + face.getVertexString());
    //              }
    //             HalfEdge edge;
    //             if (edge0 == null)
    //              { edge0 = face.getEdge(0);
    //                edge = edge0;
    //              }
    //             else
    //              { edge = edge0.getNext();
    //              }
    //             do
    //              { Face oppFace = edge.oppositeFace();
    //                if (oppFace.mark == Face.VISIBLE)
    //                 { if (oppFace.distanceToPlane (eyePnt) > tolerance)
    //                { calculateHorizon (eyePnt, edge.getOpposite(),
    //                            oppFace, horizon);
    //                }
    //               else
    //                { horizon.add (edge);
    //                  if (debug)
    //                   { System.out.println ("  adding horizon edge " +
    //                             edge.getVertexString());
    //                   }
    //                }
    //                 }
    //                edge = edge.getNext();
    //              }
    //             while (edge != edge0);
    //           }

    //       protected void addNewFaces(FaceList newFaces, Vertex eyeVtx, Vector horizon)
    //                       { 
    //                         newFaces.clear();

    //                         HalfEdge hedgeSidePrev = null;
    //                         HalfEdge hedgeSideBegin = null;

    //                         for (Iterator it=horizon.iterator(); it.hasNext(); ) 
    //                          { HalfEdge horizonHe = (HalfEdge)it.next();
    //                            HalfEdge hedgeSide = addAdjoiningFace (eyeVtx, horizonHe);
    //                            if (debug)
    //                             { System.out.println (
    //                              "new face: " + hedgeSide.face.getVertexString());
    //                             }
    //                            if (hedgeSidePrev != null)
    //                             { hedgeSide.next.setOpposite (hedgeSidePrev);		 
    //                             }
    //                            else
    //                             { hedgeSideBegin = hedgeSide; 
    //                             }
    //                            newFaces.add (hedgeSide.getFace());
    //                            hedgeSidePrev = hedgeSide;
    //                          }
    //                         hedgeSideBegin.next.setOpposite (hedgeSidePrev);
    //                       }

    //       protected Vertex nextPointToAdd()
    //       {
    //           if (!claimed.isEmpty())
    //           {
    //               Face eyeFace = claimed.first().face;
    //               Vertex eyeVtx = null;
    //               double maxDist = 0;
    //               for (Vertex vtx = eyeFace.outside;
    //                vtx != null && vtx.face == eyeFace;
    //                vtx = vtx.next)
    //               {
    //                   double dist = eyeFace.distanceToPlane(vtx.pnt);
    //                   if (dist > maxDist)
    //                   {
    //                       maxDist = dist;
    //                       eyeVtx = vtx;
    //                   }
    //               }
    //               return eyeVtx;
    //           }
    //           else
    //           {
    //               return null;
    //           }
    //       }

    //       protected void addPointToHull(Vertex eyeVtx)
    //           {
    //               horizon.clear();
    //               unclaimed.clear();

    //               if (debug)
    //                { System.out.println ("Adding point: " + eyeVtx.index);
    //              System.out.println (
    //                 " which is " + eyeVtx.face.distanceToPlane(eyeVtx.pnt) +
    //                 " above face " + eyeVtx.face.getVertexString());
    //                }
    //               removePointFromFace (eyeVtx, eyeVtx.face);
    //               calculateHorizon (eyeVtx.pnt, null, eyeVtx.face, horizon);
    //               newFaces.clear();
    //               addNewFaces (newFaces, eyeVtx, horizon);

    //               // first merge pass ... merge faces which are non-convex
    //               // as determined by the larger face

    //               for (Face face = newFaces.first(); face!=null; face=face.next)
    //                { 
    //              if (face.mark == Face.VISIBLE)
    //               { while (doAdjacentMerge(face, NONCONVEX_WRT_LARGER_FACE))
    //                    ;
    //               }
    //                }		 
    //               // second merge pass ... merge faces which are non-convex
    //               // wrt either face	     
    //               for (Face face = newFaces.first(); face!=null; face=face.next)
    //                { 
    //              if (face.mark == Face.NON_CONVEX)
    //               { face.mark = Face.VISIBLE;
    //                 while (doAdjacentMerge(face, NONCONVEX))
    //                    ;
    //               }
    //                }	
    //               resolveUnclaimedPoints(newFaces);
    //           }

    //       protected void reindexFacesAndVertices()
    //       {
    //           for (int i = 0; i < numPoints; i++)
    //           {
    //               pointBuffer[i].index = -1;
    //           }
    //           // remove inactive faces and mark active vertices
    //           numFaces = 0;
    //           for (Iterator it = faces.iterator(); it.hasNext(); )
    //           {
    //               Face face = (Face)it.next();
    //               if (face.mark != Face.VISIBLE)
    //               {
    //                   it.remove();
    //               }
    //               else
    //               {
    //                   markFaceVertices(face, 0);
    //                   numFaces++;
    //               }
    //           }
    //           // reindex vertices
    //           numVertices = 0;
    //           for (int i = 0; i < numPoints; i++)
    //           {
    //               Vertex vtx = pointBuffer[i];
    //               if (vtx.index == 0)
    //               {
    //                   vertexPointIndices[numVertices] = i;
    //                   vtx.index = numVertices++;
    //               }
    //           }
    //       }

    //       protected boolean checkFaceConvexity(Face face, double tol, PrintStream ps)
    //       {
    //           double dist;
    //           HalfEdge he = face.he0;
    //           do
    //           {
    //               face.checkConsistency();
    //               // make sure edge is convex
    //               dist = oppFaceDistance(he);
    //               if (dist > tol)
    //               {
    //                   if (ps != null)
    //                   {
    //                       ps.println("Edge " + he.getVertexString() +
    //                           " non-convex by " + dist);
    //                   }
    //                   return false;
    //               }
    //               dist = oppFaceDistance(he.opposite);
    //               if (dist > tol)
    //               {
    //                   if (ps != null)
    //                   {
    //                       ps.println("Opposite edge " +
    //                           he.opposite.getVertexString() +
    //                           " non-convex by " + dist);
    //                   }
    //                   return false;
    //               }
    //               if (he.next.oppositeFace() == he.oppositeFace())
    //               {
    //                   if (ps != null)
    //                   {
    //                       ps.println("Redundant vertex " + he.head().index +
    //                           " in face " + face.getVertexString());
    //                   }
    //                   return false;
    //               }
    //               he = he.next;
    //           }
    //           while (he != face.he0);
    //           return true;
    //       }

    //       protected boolean checkFaces(double tol, PrintStream ps)
    //       {
    //           // check edge convexity
    //           boolean convex = true;
    //           for (Iterator it = faces.iterator(); it.hasNext(); )
    //           {
    //               Face face = (Face)it.next();
    //               if (face.mark == Face.VISIBLE)
    //               {
    //                   if (!checkFaceConvexity(face, tol, ps))
    //                   {
    //                       convex = false;
    //                   }
    //               }
    //           }
    //           return convex;
    //       }

    //       #endregion

    //       #region Private Methods

    //       private void addPointToFace(Vertex vtx, Face face)
    //       {
    //           vtx.face = face;

    //           if (face.outside == null)
    //           {
    //               claimed.add(vtx);
    //           }
    //           else
    //           {
    //               claimed.insertBefore(vtx, face.outside);
    //           }
    //           face.outside = vtx;
    //       }

    //       private void removePointFromFace(Vertex vtx, Face face)
    //       {
    //           if (vtx == face.outside)
    //           {
    //               if (vtx.next != null && vtx.next.face == face)
    //               {
    //                   face.outside = vtx.next;
    //               }
    //               else
    //               {
    //                   face.outside = null;
    //               }
    //           }
    //           claimed.delete(vtx);
    //       }

    //       private Vertex removeAllPointsFromFace(Face face)
    //       {
    //           if (face.outside != null)
    //           {
    //               Vertex end = face.outside;
    //               while (end.next != null && end.next.face == face)
    //               {
    //                   end = end.next;
    //               }
    //               claimed.delete(face.outside, end);
    //               end.next = null;
    //               return face.outside;
    //           }
    //           else
    //           {
    //               return null;
    //           }
    //       }

    //       private HalfEdge findHalfEdge(Vertex tail, Vertex head)
    //       {
    //           // brute force ... OK, since setHull is not used much
    //           for (Iterator it = faces.iterator(); it.hasNext(); )
    //           {
    //               HalfEdge he = ((Face)it.next()).findEdge(tail, head);
    //               if (he != null)
    //               {
    //                   return he;
    //               }
    //           }
    //           return null;
    //       }

    //       private void printQhullErrors(Process proc)
    //                       {
    //                         boolean wrote = false;
    //                         InputStream es = proc.getErrorStream();
    //                         while (es.available() > 0)
    //                          { System.out.write (es.read());
    //                            wrote = true;
    //                          }
    //                         if (wrote)
    //                          { System.out.println("");
    //                          }
    //                       }

    //       private void getFaceIndices(int[] indices, Face face, int flags)
    //       {
    //           boolean ccw = ((flags & CLOCKWISE) == 0);
    //           boolean indexedFromOne = ((flags & INDEXED_FROM_ONE) != 0);
    //           boolean pointRelative = ((flags & POINT_RELATIVE) != 0);

    //           HalfEdge hedge = face.he0;
    //           int k = 0;
    //           do
    //           {
    //               int idx = hedge.head().index;
    //               if (pointRelative)
    //               {
    //                   idx = vertexPointIndices[idx];
    //               }
    //               if (indexedFromOne)
    //               {
    //                   idx++;
    //               }
    //               indices[k++] = idx;
    //               hedge = (ccw ? hedge.next : hedge.prev);
    //           }
    //           while (hedge != face.he0);
    //       }

    //       private boolean doAdjacentMerge(Face face, int mergeType)
    //                       {
    //                         HalfEdge hedge = face.he0;

    //                         boolean convex = true;
    //                         do
    //                          { Face oppFace = hedge.oppositeFace();
    //                            boolean merge = false;
    //                            double dist1, dist2;

    //                            if (mergeType == NONCONVEX)
    //                             { // then merge faces if they are definitively non-convex
    //                           if (oppFaceDistance (hedge) > -tolerance ||
    //                               oppFaceDistance (hedge.opposite) > -tolerance)
    //                            { merge = true;
    //                            }
    //                             }
    //                            else // mergeType == NONCONVEX_WRT_LARGER_FACE
    //                             { // merge faces if they are parallel or non-convex
    //                           // wrt to the larger face; otherwise, just mark
    //                           // the face non-convex for the second pass.
    //                           if (face.area > oppFace.area)
    //                            { if ((dist1 = oppFaceDistance (hedge)) > -tolerance) 
    //                               { merge = true;
    //                               }
    //                              else if (oppFaceDistance (hedge.opposite) > -tolerance)
    //                               { convex = false;
    //                               }
    //                            }
    //                           else
    //                            { if (oppFaceDistance (hedge.opposite) > -tolerance)
    //                               { merge = true;
    //                               }
    //                              else if (oppFaceDistance (hedge) > -tolerance) 
    //                               { convex = false;
    //                               }
    //                            }
    //                             }

    //                            if (merge)
    //                             { if (debug)
    //                            { System.out.println (
    //                              "  merging " + face.getVertexString() + "  and  " +
    //                              oppFace.getVertexString());
    //                            }

    //                           int numd = face.mergeAdjacentFace (hedge, discardedFaces);
    //                           for (int i=0; i<numd; i++)
    //                            { deleteFacePoints (discardedFaces[i], face);
    //                            }
    //                           if (debug)
    //                            { System.out.println (
    //                                 "  result: " + face.getVertexString());
    //                            }
    //                           return true;
    //                             }
    //                            hedge = hedge.next;
    //                          }
    //                         while (hedge != face.he0);
    //                         if (!convex)
    //                          { face.mark = Face.NON_CONVEX; 
    //                          }
    //                         return false;
    //                       }

    //       private HalfEdge addAdjoiningFace(Vertex eyeVtx, HalfEdge he)
    //       {
    //           Face face = Face.createTriangle(
    //              eyeVtx, he.tail(), he.head());
    //           faces.add(face);
    //           face.getEdge(-1).setOpposite(he.getOpposite());
    //           return face.getEdge(0);
    //       }

    //       // 	private void splitFace (Face face)
    //       // 	 {
    //       //  	   Face newFace = face.split();
    //       //  	   if (newFace != null)
    //       //  	    { newFaces.add (newFace);
    //       //  	      splitFace (newFace);
    //       //  	      splitFace (face);
    //       //  	    }
    //       // 	 }

    //       private void markFaceVertices(Face face, int mark)
    //       {
    //           HalfEdge he0 = face.getFirstEdge();
    //           HalfEdge he = he0;
    //           do
    //           {
    //               he.head().index = mark;
    //               he = he.next;
    //           }
    //           while (he != he0);
    //       }

    //       #endregion
    //   }


    /**
     * Computes the convex hull of a set of three dimensional points.
     *
     * <p>The algorithm is a three dimensional implementation of Quickhull, as
     * described in Barber, Dobkin, and Huhdanpaa, <a
     * href=http://citeseer.ist.psu.edu/barber96quickhull.html> ``The Quickhull
     * Algorithm for Convex Hulls''</a> (ACM Transactions on Mathematical Software,
     * Vol. 22, No. 4, December 1996), and has a complexity of O(n log(n)) with
     * respect to the number of points. A well-known C implementation of Quickhull
     * that works for arbitrary dimensions is provided by <a
     * href=http://www.qhull.org>qhull</a>.
     *
     * <p>A hull is constructed by providing a set of points
     * to either a constructor or a
     * {@link #build(Point3d[]) build} method. After
     * the hull is built, its vertices and faces can be retrieved
     * using {@link #getVertices()
     * getVertices} and {@link #getFaces() getFaces}.
     * A typical usage might look like this:
     * <pre>
     *   // x y z coordinates of 6 points
     *   Point3d[] points = new Point3d[] 
     *    { new Point3d (0.0,  0.0,  0.0),
     *      new Point3d (1.0,  0.5,  0.0),
     *      new Point3d (2.0,  0.0,  0.0),
     *      new Point3d (0.5,  0.5,  0.5),
     *      new Point3d (0.0,  0.0,  2.0),
     *      new Point3d (0.1,  0.2,  0.3),
     *      new Point3d (0.0,  2.0,  0.0),
     *    };
     *
     *   QuickHull3D hull = new QuickHull3D();
     *   hull.build (points);
     *
     *   System.out.println ("Vertices:");
     *   Point3d[] vertices = hull.getVertices();
     *   for (int i = 0; i < vertices.length; i++)
     *    { Point3d pnt = vertices[i];
     *      System.out.println (pnt.x + " " + pnt.y + " " + pnt.z);
     *    }
     *
     *   System.out.println ("Faces:");
     *   int[][] faceIndices = hull.getFaces();
     *   for (int i = 0; i < vertices.length; i++)
     *    { for (int k = 0; k < faceIndices[i].length; k++)
     *       { System.out.print (faceIndices[i][k] + " ");
     *       }
     *      System.out.println ("");
     *    }
     * </pre>
     * As a convenience, there are also {@link #build(double[]) build}
     * and {@link #getVertices(double[]) getVertex} methods which
     * pass point information using an array of doubles.
     *
     * <h3><a name=distTol>Robustness</h3> Because this algorithm uses floating
     * point arithmetic, it is potentially vulnerable to errors arising from
     * numerical imprecision.  We address this problem in the same way as <a
     * href=http://www.qhull.org>qhull</a>, by merging faces whose edges are not
     * clearly convex. A face is convex if its edges are convex, and an edge is
     * convex if the centroid of each adjacent plane is clearly <i>below</i> the
     * plane of the other face. The centroid is considered below a plane if its
     * distance to the plane is less than the negative of a {@link
     * #getDistanceTolerance() distance tolerance}.  This tolerance represents the
     * smallest distance that can be reliably computed within the available numeric
     * precision. It is normally computed automatically from the point data,
     * although an application may {@link #setExplicitDistanceTolerance set this
     * tolerance explicitly}.
     *
     * <p>Numerical problems are more likely to arise in situations where data
     * points lie on or within the faces or edges of the convex hull. We have
     * tested QuickHull3D for such situations by computing the convex hull of a
     * random point set, then adding additional randomly chosen points which lie
     * very close to the hull vertices and edges, and computing the convex
     * hull again. The hull is deemed correct if {@link #check check} returns
     * <code>true</code>.  These tests have been successful for a large number of
     * trials and so we are confident that QuickHull3D is reasonably robust.
     *
     * <h3>Merged Faces</h3> The merging of faces means that the faces returned by
     * QuickHull3D may be convex polygons instead of triangles. If triangles are
     * desired, the application may {@link #triangulate triangulate} the faces, but
     * it should be noted that this may result in triangles which are very small or
     * thin and hence difficult to perform reliable convexity tests on. In other
     * words, triangulating a merged face is likely to restore the numerical
     * problems which the merging process removed. Hence is it
     * possible that, after triangulation, {@link #check check} will fail (the same
     * behavior is observed with triangulated output from <a
     * href=http://www.qhull.org>qhull</a>).
     *
     * <h3>Degenerate Input</h3>It is assumed that the input points
     * are non-degenerate in that they are not coincident, colinear, or
     * colplanar, and thus the convex hull has a non-zero volume.
     * If the input points are detected to be degenerate within
     * the {@link #getDistanceTolerance() distance tolerance}, an
     * IllegalArgumentException will be thrown.
     *
     * @author John E. Lloyd, Fall 2004 */

    #endregion
    #region Class: QuickHull4

    public static class QuickHull4
    {
        #region Class: TriangleWithPoints

        /// <summary>
        /// This links a triangle with a set of points that sit "outside" the triangle
        /// </summary>
        private class TriangleWithPoints
        {
            public TriangleWithPoints(Triangle triangle)
            {
                this.Triangle = triangle;
                this.OutsidePoints = new List<Point3D>();
            }

            public Triangle Triangle
            {
                get;
                private set;
            }
            public List<Point3D> OutsidePoints
            {
                get;
                private set;
            }
        }

        #endregion

        public static Triangle[] GetQuickHull(List<Point3D> points)
        {
            try
            {
                if (points.Count < 4)
                {
                    throw new ArgumentException("There must be at least 4 points", "points");
                }

                // Pick 4 points
                Point3D[] startPoints = GetStartingTetrahedron(points);
                List<TriangleWithPoints> retVal = ConvertStartingPointsToTriangles(startPoints, points);



                //TODO:  See if there's a more efficient way of doing this
                while (true)
                {
                    bool foundOne = false;
                    int index = 0;
                    while (index < retVal.Count)
                    {
                        if (retVal[index].OutsidePoints.Count > 0)
                        {
                            foundOne = true;
                            ProcessTriangle(retVal, index);
                        }
                        else
                        {
                            index++;
                        }
                    }

                    if (!foundOne)
                    {
                        break;
                    }
                }




                // Exit Function
                return retVal.Select(o => o.Triangle).ToArray();
            }
            catch (Exception ex)
            {
                //TODO:  Figure out how to report an error
                //System.Diagnostics.EventLog.WriteEntry("Application Error", ex.ToString(), System.Diagnostics.EventLogEntryType.Warning);
                //Console.Write(ex.ToString());
                return null;
            }
        }

        #region Private Methods

        /// <summary>
        /// I've seen various literature call this initial tetrahedron a simplex
        /// </summary>
        public static Point3D[] GetStartingTetrahedron(List<Point3D> points)
        {
            Point3D[] retVal = new Point3D[4];

            // Validate the point cloud is 3D, also get the points that have the smallest and largest X (an exception is thrown if they are the same point)
            int minXIndex, maxXIndex;
            GetStartingTetrahedronSprtGetMinMaxX(out minXIndex, out maxXIndex, points);

            // The first two return points will be the ones with the min and max X values
            retVal[0] = points[minXIndex];
            retVal[1] = points[maxXIndex];

            // The third point will be the one that is farthest from the line defined by the first two
            int thirdIndex = GetStartingTetrahedronSprtFarthestFromLine(minXIndex, maxXIndex, points);
            retVal[2] = points[thirdIndex];

            // The fourth point will be the one that is farthest from the plane defined by the first three
            int fourthIndex = GetStartingTetrahedronSprtFarthestFromPlane(minXIndex, maxXIndex, thirdIndex, points);
            retVal[3] = points[fourthIndex];

            // Exit Function
            return retVal;
        }
        /// <summary>
        /// This does more than just look at X.  It looks at Y and Z as well to verify the point cloud has points in all 3 dimensions
        /// </summary>
        private static void GetStartingTetrahedronSprtGetMinMaxX(out int minXIndex, out int maxXIndex, List<Point3D> points)
        {
            // Create arrays to hold the min and max along each axis (0=X, 1=Y, 2=Z) - using the first point
            double[] minValues = new double[] { points[0].X, points[0].Y, points[0].Z };
            double[] maxValues = new double[] { points[0].X, points[0].Y, points[0].Z };
            int[] minIndicies = new int[] { 0, 0, 0 };
            int[] maxIndicies = new int[] { 0, 0, 0 };

            for (int cntr = 1; cntr < points.Count; cntr++)
            {
                #region Examine Point

                // Min
                if (points[cntr].X < minValues[0])
                {
                    minValues[0] = points[cntr].X;
                    minIndicies[0] = cntr;
                }

                if (points[cntr].Y < minValues[1])
                {
                    minValues[1] = points[cntr].Y;
                    minIndicies[1] = cntr;
                }

                if (points[cntr].Z < minValues[2])
                {
                    minValues[2] = points[cntr].Z;
                    minIndicies[2] = cntr;
                }

                // Max
                if (points[cntr].X > maxValues[0])
                {
                    maxValues[0] = points[cntr].X;
                    maxIndicies[0] = cntr;
                }

                if (points[cntr].Y > maxValues[1])
                {
                    maxValues[1] = points[cntr].Y;
                    maxIndicies[1] = cntr;
                }

                if (points[cntr].Z > maxValues[2])
                {
                    maxValues[2] = points[cntr].Z;
                    maxIndicies[2] = cntr;
                }

                #endregion
            }

            #region Validate

            for (int cntr = 0; cntr < 3; cntr++)
            {
                if (maxValues[cntr] == minValues[cntr])
                {
                    throw new ApplicationException("The points passed in aren't in 3D - they are either all on the same point (0D), or colinear (1D), or coplanar (2D)");
                }
            }

            #endregion

            // Return the two points that are the min/max X
            minXIndex = minIndicies[0];
            maxXIndex = maxIndicies[0];
        }
        /// <summary>
        /// Finds the point that is farthest from the line
        /// </summary>
        private static int GetStartingTetrahedronSprtFarthestFromLine(int index1, int index2, List<Point3D> points)
        {
            Vector3D lineDirection = points[index2] - points[index1];		// The nearest point method wants a vector, so calculate it once
            Point3D startPoint = points[index1];		// not sure if there is much of a penalty to referencing a list by index, but I figure I'll just cache this point once

            double maxDistance = -1d;
            int retVal = -1;

            for (int cntr = 0; cntr < points.Count; cntr++)
            {
                if (cntr == index1 || cntr == index2)
                {
                    continue;
                }

                // Calculate the distance from the line
                Point3D nearestPoint = Math3D.GetClosestPoint_Line_Point(startPoint, lineDirection, points[cntr]);
                double distanceSquared = (points[cntr] - nearestPoint).LengthSquared;

                if (distanceSquared > maxDistance)
                {
                    maxDistance = distanceSquared;
                    retVal = cntr;
                }
            }

            if (retVal < 0)
            {
                throw new ApplicationException("Didn't find a return point, this should never happen");
            }

            // Exit Function
            return retVal;
        }
        private static int GetStartingTetrahedronSprtFarthestFromPlane(int index1, int index2, int index3, List<Point3D> points)
        {
            //TODO:  The overload of DistanceFromPlane that I'm calling is inefficient when called over and over.  Use the other overload
            Vector3D[] triangle = new Vector3D[] { points[index1].ToVector(), points[index2].ToVector(), points[index3].ToVector() };

            double maxDistance = -1d;
            int retVal = -1;

            for (int cntr = 0; cntr < points.Count; cntr++)
            {
                if (cntr == index1 || cntr == index2 || cntr == index3)
                {
                    continue;
                }

                double distance = Math.Abs(Math3D.DistanceFromPlane(triangle, points[cntr].ToVector()));
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    retVal = cntr;
                }
            }

            if (retVal < 0)
            {
                throw new ApplicationException("Didn't find a return point, this should never happen");
            }

            // Exit Function
            return retVal;
        }

        private static List<TriangleWithPoints> ConvertStartingPointsToTriangles(Point3D[] startPoints, List<Point3D> allPoints)
        {
            if (startPoints.Length != 4)
            {
                throw new ArgumentException("This method expects exactly 4 points passed in");
            }

            List<TriangleWithPoints> retVal = new List<TriangleWithPoints>();

            #region Make triangles

            // Make a triangle for 0,1,2
            Triangle triangle = new Triangle(startPoints[0], startPoints[1], startPoints[2]);

            // See what side of the plane 3 is on
            if (Vector3D.DotProduct(startPoints[2].ToVector(), triangle.Normal) < 0d)
            {
                retVal.Add(new TriangleWithPoints(triangle));
                retVal.Add(new TriangleWithPoints(new Triangle(startPoints[3], startPoints[1], startPoints[0])));
                retVal.Add(new TriangleWithPoints(new Triangle(startPoints[3], startPoints[2], startPoints[1])));
                retVal.Add(new TriangleWithPoints(new Triangle(startPoints[3], startPoints[0], startPoints[2])));
            }
            else
            {
                retVal.Add(new TriangleWithPoints(new Triangle(startPoints[0], startPoints[2], startPoints[1])));
                retVal.Add(new TriangleWithPoints(new Triangle(startPoints[3], startPoints[0], startPoints[1])));
                retVal.Add(new TriangleWithPoints(new Triangle(startPoints[3], startPoints[1], startPoints[2])));
                retVal.Add(new TriangleWithPoints(new Triangle(startPoints[3], startPoints[2], startPoints[0])));
            }

            #endregion

            #region Calculate outside points

            // For every triangle, find the points that are outside the polygon (not the points behind the triangle)
            foreach (TriangleWithPoints triangleWrapper in retVal)
            {
                triangleWrapper.OutsidePoints.AddRange(GetOutsideSet(triangleWrapper.Triangle, allPoints));
            }

            #endregion

            // Exit Function
            return retVal;
        }

        private static void ProcessTriangle(List<TriangleWithPoints> hull, int index)
        {
            //TriangleWithPoints

            Triangle triangle = hull[index].Triangle;

            // Find the farthest point from this triangle
            int fartherstIndex = ProcessTriangleSprtFarthestPoint(hull[index].Triangle, hull[index].OutsidePoints);










        }
        private static int ProcessTriangleSprtFarthestPoint(Triangle triangle, List<Point3D> points)
        {
            //TODO:  The overload of DistanceFromPlane that I'm calling is inefficient when called over and over.  Use the other overload
            Vector3D[] plane = new Vector3D[] { triangle.Point0.ToVector(), triangle.Point1.ToVector(), triangle.Point2.ToVector() };

            double maxDistance = 0d;
            int retVal = -1;

            for (int cntr = 0; cntr < points.Count; cntr++)
            {
                double distance = Math3D.DistanceFromPlane(plane, points[cntr].ToVector());
                if (distance > maxDistance)     // distance should never be negative (or the point wouldn't be in the list of outside points).  If for some reason there is one with a negative distance, it shouldn't be considered, because it sits inside the hull
                {
                    maxDistance = distance;
                    retVal = cntr;
                }
            }

            if (retVal < 0)
            {
                throw new ApplicationException("Didn't find a return point, this should never happen");
            }

            // Exit Function
            return retVal;
        }

        /// <summary>
        /// This returns the subset of points that are on the outside facing side of the triangle
        /// </summary>
        private static List<Point3D> GetOutsideSet(Triangle triangle, List<Point3D> points)
        {
            List<Point3D> retVal = new List<Point3D>();

            foreach (Point3D point in points)
            {
                if (Vector3D.DotProduct(triangle.Normal, point.ToVector()) > 0d)		// 0 would be coplanar, -1 would be the opposite side
                {
                    retVal.Add(point);
                }
            }

            return retVal;
        }

        #endregion
    }

    #endregion
    #region Class: QuickHull5

    //TODO:  Make overloads in Math3D so I don't have so many calls to ToVector()

    public static class QuickHull5
    {
        #region Class: TriangleWithPoints

        /// <summary>
        /// This links a triangle with a set of points that sit "outside" the triangle
        /// </summary>
        public class TriangleWithPoints
        {
            public TriangleWithPoints(TriangleIndexed triangle)
            {
                this.Triangle = triangle;
                this.OutsidePoints = new List<int>();
            }

            public TriangleIndexed Triangle
            {
                get;
                private set;
            }
            public List<int> OutsidePoints
            {
                get;
                private set;
            }
        }

        #endregion

        #region Declaration Section

        //public static Point3D TrianglePoint0;
        //public static Point3D TrianglePoint1;
        //public static Point3D TrianglePoint2;

        //public static bool Inverted0 = false;
        //public static bool Inverted1 = false;
        //public static bool Inverted2 = false;
        //public static bool Inverted3 = false;

        #endregion

        public static TriangleIndexed[] GetQuickHull(Point3D[] points)
        {
            try
            {
                if (points.Length < 4)
                {
                    throw new ArgumentException("There must be at least 4 points", "points");
                }

                // Pick 4 points
                int[] startPoints = GetStartingTetrahedron(points);
                List<TriangleWithPoints> retVal = ConvertStartingPointsToTriangles(startPoints, points);

                // If any triangle has any points outside the hull, then remove that triangle, and replace it with triangles connected to the
                // farthest out point (relative to the triangle that got removed)
                //TODO:  See if there's a more efficient way of doing this
                while (true)
                {
                    bool foundOne = false;
                    int index = 0;
                    while (index < retVal.Count)
                    {
                        if (retVal[index].OutsidePoints.Count > 0)
                        {
                            foundOne = true;
                            ProcessTriangle(retVal, index);





                            index++;


                            //foundOne = false;
                            //break;


                        }
                        else
                        {
                            index++;
                        }
                    }






                    //break;



                    if (!foundOne)
                    {
                        break;
                    }
                }

                // Exit Function
                return retVal.Select(o => o.Triangle).ToArray();
            }
            catch (Exception ex)
            {
                //TODO:  Figure out how to report an error
                //System.Diagnostics.EventLog.WriteEntry("Application Error", ex.ToString(), System.Diagnostics.EventLogEntryType.Warning);
                //Console.Write(ex.ToString());
                return null;
            }
        }
        public static TriangleIndexed[] GetQuickHullTest(out Point3D farthestPoint, out TriangleIndexed removedTriangle, out TriangleIndexed[] otherRemovedTriangles, Point3D[] points)
        {
            try
            {
                if (points.Length < 4)
                {
                    throw new ArgumentException("There must be at least 4 points", "points");
                }

                // Pick 4 points
                int[] startPoints = GetStartingTetrahedron(points);
                List<TriangleWithPoints> retVal = ConvertStartingPointsToTriangles(startPoints, points);


                ProcessTriangleTest(out farthestPoint, out removedTriangle, out otherRemovedTriangles, retVal, 0);



                // Exit Function
                return retVal.Select(o => o.Triangle).ToArray();
            }
            catch (Exception ex)
            {
                //TODO:  Figure out how to report an error
                //System.Diagnostics.EventLog.WriteEntry("Application Error", ex.ToString(), System.Diagnostics.EventLogEntryType.Warning);
                //Console.Write(ex.ToString());
                farthestPoint = new Point3D(0, 0, 0);
                removedTriangle = null;
                otherRemovedTriangles = null;
                return null;
            }
        }

        #region Private Methods

        /// <summary>
        /// I've seen various literature call this initial tetrahedron a simplex
        /// </summary>
        public static int[] GetStartingTetrahedron(Point3D[] points)
        {
            int[] retVal = new int[4];

            // Validate the point cloud is 3D, also get the points that have the smallest and largest X (an exception is thrown if they are the same point)
            int minXIndex, maxXIndex;
            GetStartingTetrahedronSprtGetMinMaxX(out minXIndex, out maxXIndex, points);

            // The first two return points will be the ones with the min and max X values
            retVal[0] = minXIndex;
            retVal[1] = maxXIndex;

            // The third point will be the one that is farthest from the line defined by the first two
            int thirdIndex = GetStartingTetrahedronSprtFarthestFromLine(minXIndex, maxXIndex, points);
            retVal[2] = thirdIndex;

            // The fourth point will be the one that is farthest from the plane defined by the first three
            int fourthIndex = GetStartingTetrahedronSprtFarthestFromPlane(minXIndex, maxXIndex, thirdIndex, points);
            retVal[3] = fourthIndex;

            // Exit Function
            return retVal;
        }
        /// <summary>
        /// This does more than just look at X.  It looks at Y and Z as well to verify the point cloud has points in all 3 dimensions
        /// </summary>
        private static void GetStartingTetrahedronSprtGetMinMaxX(out int minXIndex, out int maxXIndex, Point3D[] points)
        {
            // Create arrays to hold the min and max along each axis (0=X, 1=Y, 2=Z) - using the first point
            double[] minValues = new double[] { points[0].X, points[0].Y, points[0].Z };
            double[] maxValues = new double[] { points[0].X, points[0].Y, points[0].Z };
            int[] minIndicies = new int[] { 0, 0, 0 };
            int[] maxIndicies = new int[] { 0, 0, 0 };

            for (int cntr = 1; cntr < points.Length; cntr++)
            {
                #region Examine Point

                // Min
                if (points[cntr].X < minValues[0])
                {
                    minValues[0] = points[cntr].X;
                    minIndicies[0] = cntr;
                }

                if (points[cntr].Y < minValues[1])
                {
                    minValues[1] = points[cntr].Y;
                    minIndicies[1] = cntr;
                }

                if (points[cntr].Z < minValues[2])
                {
                    minValues[2] = points[cntr].Z;
                    minIndicies[2] = cntr;
                }

                // Max
                if (points[cntr].X > maxValues[0])
                {
                    maxValues[0] = points[cntr].X;
                    maxIndicies[0] = cntr;
                }

                if (points[cntr].Y > maxValues[1])
                {
                    maxValues[1] = points[cntr].Y;
                    maxIndicies[1] = cntr;
                }

                if (points[cntr].Z > maxValues[2])
                {
                    maxValues[2] = points[cntr].Z;
                    maxIndicies[2] = cntr;
                }

                #endregion
            }

            #region Validate

            for (int cntr = 0; cntr < 3; cntr++)
            {
                if (maxValues[cntr] == minValues[cntr])
                {
                    throw new ApplicationException("The points passed in aren't in 3D - they are either all on the same point (0D), or colinear (1D), or coplanar (2D)");
                }
            }

            #endregion

            // Return the two points that are the min/max X
            minXIndex = minIndicies[0];
            maxXIndex = maxIndicies[0];
        }
        /// <summary>
        /// Finds the point that is farthest from the line
        /// </summary>
        private static int GetStartingTetrahedronSprtFarthestFromLine(int index1, int index2, Point3D[] points)
        {
            Vector3D lineDirection = points[index2] - points[index1];		// The nearest point method wants a vector, so calculate it once
            Point3D startPoint = points[index1];		// not sure if there is much of a penalty to referencing a list by index, but I figure I'll just cache this point once

            double maxDistance = -1d;
            int retVal = -1;

            for (int cntr = 0; cntr < points.Length; cntr++)
            {
                if (cntr == index1 || cntr == index2)
                {
                    continue;
                }

                // Calculate the distance from the line
                Point3D nearestPoint = Math3D.GetClosestPoint_Line_Point(startPoint, lineDirection, points[cntr]);
                double distanceSquared = (points[cntr] - nearestPoint).LengthSquared;

                if (distanceSquared > maxDistance)
                {
                    maxDistance = distanceSquared;
                    retVal = cntr;
                }
            }

            if (retVal < 0)
            {
                throw new ApplicationException("Didn't find a return point, this should never happen");
            }

            // Exit Function
            return retVal;
        }
        private static int GetStartingTetrahedronSprtFarthestFromPlane(int index1, int index2, int index3, Point3D[] points)
        {
            //NOTE:  I'm copying bits of Math3D.DistanceFromPlane here for optimization reasons
            //Vector3D[] triangle = new Vector3D[] { points[index1].ToVector(), points[index2].ToVector(), points[index3].ToVector() };
            //Vector3D normal = Math3D.Normal(triangle);
            //double originDistance = Math3D.GetPlaneOriginDistance(normal, triangle[0].ToPoint());
            Triangle triangle = new Triangle(points[index1], points[index2], points[index3]);
            Vector3D normal = triangle.NormalUnit;
            double originDistance = Math3D.GetPlaneOriginDistance(normal, triangle.Point0);

            double maxDistance = 0d;
            int retVal = -1;

            for (int cntr = 0; cntr < points.Length; cntr++)
            {
                if (cntr == index1 || cntr == index2 || cntr == index3)
                {
                    continue;
                }

                //NOTE:  This is Math3D.DistanceFromPlane copied inline to speed things up
                Point3D point = points[cntr];
                double distance = ((normal.X * point.X) +					// Ax +
                                                (normal.Y * point.Y) +					// Bx +
                                                (normal.Z * point.Z)) + originDistance;	// Cz + D

                // I don't care which side of the triangle the point is on for this initial tetrahedron
                distance = Math.Abs(distance);

                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    retVal = cntr;
                }
            }

            if (retVal < 0)
            {
                throw new ApplicationException("Didn't find a return point, this should never happen");
            }

            // Exit Function
            return retVal;
        }

        private static List<TriangleWithPoints> ConvertStartingPointsToTriangles(int[] startPoints, Point3D[] allPoints)
        {
            if (startPoints.Length != 4)
            {
                throw new ArgumentException("This method expects exactly 4 points passed in");
            }

            List<TriangleWithPoints> retVal = new List<TriangleWithPoints>();

            #region Make triangles

            //// Make a triangle for 0,1,2
            //TriangleIndexed triangle = new TriangleIndexed(startPoints[0], startPoints[1], startPoints[2], allPoints);

            //// See what side of the plane 3 is on
            //if (Vector3D.DotProduct(allPoints[startPoints[2]].ToVector(), triangle.Normal) > 0d)
            //{
            //    retVal.Add(new TriangleWithPoints(triangle));
            //    retVal.Add(new TriangleWithPoints(new TriangleIndexed(startPoints[3], startPoints[1], startPoints[0], allPoints)));
            //    retVal.Add(new TriangleWithPoints(new TriangleIndexed(startPoints[3], startPoints[2], startPoints[1], allPoints)));
            //    retVal.Add(new TriangleWithPoints(new TriangleIndexed(startPoints[3], startPoints[0], startPoints[2], allPoints)));
            //}
            //else
            //{
            //    retVal.Add(new TriangleWithPoints(new TriangleIndexed(startPoints[0], startPoints[2], startPoints[1], allPoints)));
            //    retVal.Add(new TriangleWithPoints(new TriangleIndexed(startPoints[3], startPoints[0], startPoints[1], allPoints)));
            //    retVal.Add(new TriangleWithPoints(new TriangleIndexed(startPoints[3], startPoints[1], startPoints[2], allPoints)));
            //    retVal.Add(new TriangleWithPoints(new TriangleIndexed(startPoints[3], startPoints[2], startPoints[0], allPoints)));
            //}


            //Inverted0 = false;
            //Inverted1 = false;
            //Inverted2 = false;
            //Inverted3 = false;


            //TODO:  This fails when the triangle is on the wrong side of the origin - compare the normal with the distance from the origin?


            //TriangleIndexed triangle = new TriangleIndexed(startPoints[0], startPoints[1], startPoints[2], allPoints);		// Make a triangle for 0,1,2
            //if (Vector3D.DotProduct(allPoints[startPoints[2]].ToVector(), triangle.Normal) < 0d)		// See what side of the plane 3 is on
            //{
            //    Inverted0 = true;
            //    triangle = new TriangleIndexed(startPoints[0], startPoints[2], startPoints[1], allPoints);
            //}
            //retVal.Add(new TriangleWithPoints(triangle));


            //triangle = new TriangleIndexed(startPoints[3], startPoints[1], startPoints[0], allPoints);
            //if (Vector3D.DotProduct(allPoints[startPoints[0]].ToVector(), triangle.Normal) < 0d)
            //{
            //    Inverted1 = true;
            //    triangle = new TriangleIndexed(startPoints[3], startPoints[0], startPoints[1], allPoints);
            //}
            //retVal.Add(new TriangleWithPoints(triangle));


            //triangle = new TriangleIndexed(startPoints[3], startPoints[2], startPoints[1], allPoints);
            //if (Vector3D.DotProduct(allPoints[startPoints[1]].ToVector(), triangle.Normal) < 0d)
            //{
            //    Inverted2 = true;
            //    triangle = new TriangleIndexed(startPoints[3], startPoints[1], startPoints[2], allPoints);
            //}
            //retVal.Add(new TriangleWithPoints(triangle));


            //triangle = new TriangleIndexed(startPoints[3], startPoints[0], startPoints[2], allPoints);
            //if (Vector3D.DotProduct(allPoints[startPoints[2]].ToVector(), triangle.Normal) < 0d)
            //{
            //    Inverted3 = true;
            //    triangle = new TriangleIndexed(startPoints[3], startPoints[2], startPoints[0], allPoints);
            //}
            //retVal.Add(new TriangleWithPoints(triangle));





            retVal.Add(new TriangleWithPoints(CreateTriangle(startPoints[0], startPoints[1], startPoints[2], startPoints[3], allPoints)));
            retVal.Add(new TriangleWithPoints(CreateTriangle(startPoints[3], startPoints[1], startPoints[0], startPoints[2], allPoints)));
            retVal.Add(new TriangleWithPoints(CreateTriangle(startPoints[3], startPoints[2], startPoints[1], startPoints[0], allPoints)));
            retVal.Add(new TriangleWithPoints(CreateTriangle(startPoints[3], startPoints[0], startPoints[2], startPoints[1], allPoints)));







            #endregion

            //TrianglePoint0 = retVal[0].Triangle.Point0;
            //TrianglePoint1 = retVal[0].Triangle.Point1;
            //TrianglePoint2 = retVal[0].Triangle.Point2;

            #region Calculate outside points

            // GetOutsideSet wants indicies to the points it needs to worry about.  This initial call needs all points
            List<int> allPointIndicies = Enumerable.Range(0, allPoints.Length).ToList();

            // For every triangle, find the points that are outside the polygon (not the points behind the triangle)
            foreach (TriangleWithPoints triangleWrapper in retVal)
            {
                triangleWrapper.OutsidePoints.AddRange(GetOutsideSet(triangleWrapper.Triangle, allPointIndicies, allPoints));
            }

            #endregion

            // Exit Function
            return retVal;
        }

        private static void ProcessTriangleTest(out Point3D farthestPoint, out TriangleIndexed removedTriangle, out TriangleIndexed[] otherRemovedTriangles, List<TriangleWithPoints> hull, int index)
        {
            List<TriangleWithPoints> removedTriangles = new List<TriangleWithPoints>();

            TriangleWithPoints removedTriangleWrapper = hull[index];
            removedTriangles.Add(removedTriangleWrapper);
            hull.RemoveAt(index);

            // Find the farthest point from this triangle
            int fartherstIndex = ProcessTriangleSprtFarthestPoint(removedTriangleWrapper);

            #region Remove visible triangles

            // Find triangles that are visible to this point (the front sides, not the back sides)
            int triangleIndex = 0;
            while (triangleIndex < hull.Count)
            {
                //TODO:  See if it's cheaper to do the dot product again, or to scan this triangle's list of outside points
                if (hull[triangleIndex].OutsidePoints.Contains(fartherstIndex))
                //if (Vector3D.DotProduct(hull[triangleIndex].Triangle.Normal, hull[triangleIndex].Triangle.AllPoints[fartherstIndex].ToVector()) > 0d)		// 0 would be coplanar, -1 would be the opposite side
                {
                    removedTriangles.Add(hull[triangleIndex]);
                    hull.RemoveAt(triangleIndex);
                }
                else
                {
                    triangleIndex++;
                }
            }

            #endregion

            // Get all of the outside points from all the removed triangles (deduped)
            List<int> allOutsidePoints = removedTriangles.SelectMany(o => o.OutsidePoints).Distinct().ToList();		// if you try to read what SelectMany does, it gets pretty wordy and unclear.  It just does a foreach across all the OutsidePoints lists from all the removedTriangles (treats the list of lists like a single list)

            // Find the rim of the bowl that's been created
            List<int[]> horizonRidge = ProcessTriangleSprtGetHorizon(removedTriangles, hull);

            // Create new triangles, and add them to the hull
            ProcessTriangleSprtAddNew(fartherstIndex, hull, horizonRidge, allOutsidePoints, removedTriangleWrapper.Triangle.AllPoints);




            removedTriangle = removedTriangles[0].Triangle;
            removedTriangles.RemoveAt(0);

            otherRemovedTriangles = removedTriangles.Select(o => o.Triangle).ToArray();

            farthestPoint = removedTriangle.AllPoints[fartherstIndex];


        }

        private static void ProcessTriangle(List<TriangleWithPoints> hull, int index)
        {
            List<TriangleWithPoints> removedTriangles = new List<TriangleWithPoints>();

            TriangleWithPoints removedTriangle = hull[index];
            removedTriangles.Add(removedTriangle);
            hull.RemoveAt(index);

            // Find the farthest point from this triangle
            int fartherstIndex = ProcessTriangleSprtFarthestPoint(removedTriangle);

            #region Remove visible triangles

            // Find triangles that are visible to this point (the front sides, not the back sides)
            int triangleIndex = 0;
            while (triangleIndex < hull.Count)
            {
                //TODO:  See if it's cheaper to do the dot product again, or to scan this triangle's list of outside points
                if (hull[triangleIndex].OutsidePoints.Contains(fartherstIndex))
                //if (Vector3D.DotProduct(hull[triangleIndex].Triangle.Normal, hull[triangleIndex].Triangle.AllPoints[fartherstIndex].ToVector()) > 0d)		// 0 would be coplanar, -1 would be the opposite side
                {
                    removedTriangles.Add(hull[triangleIndex]);
                    hull.RemoveAt(triangleIndex);
                }
                else
                {
                    triangleIndex++;
                }
            }

            #endregion

            // Get all of the outside points from all the removed triangles (deduped)
            List<int> allOutsidePoints = removedTriangles.SelectMany(o => o.OutsidePoints).Distinct().ToList();		// if you try to read what SelectMany does, it gets pretty wordy and unclear.  It just does a foreach across all the OutsidePoints lists from all the removedTriangles (treats the list of lists like a single list)

            // Find the rim of the bowl that's been created
            List<int[]> horizonRidge = ProcessTriangleSprtGetHorizon(removedTriangles, hull);

            // Create new triangles, and add them to the hull
            ProcessTriangleSprtAddNew(fartherstIndex, hull, horizonRidge, allOutsidePoints, removedTriangle.Triangle.AllPoints);
        }
        public static int ProcessTriangleSprtFarthestPoint(TriangleWithPoints triangle)
        {
            Vector3D[] polygon = new Vector3D[] { triangle.Triangle.Point0.ToVector(), triangle.Triangle.Point1.ToVector(), triangle.Triangle.Point2.ToVector() };

            List<int> pointIndicies = triangle.OutsidePoints;
            Point3D[] allPoints = triangle.Triangle.AllPoints;

            double maxDistance = 0d;
            int retVal = -1;

            for (int cntr = 0; cntr < pointIndicies.Count; cntr++)
            {
                double distance = Math3D.DistanceFromPlane(polygon, allPoints[pointIndicies[cntr]].ToVector());

                if (distance > maxDistance)		// distance should never be negative (or the point wouldn't be in the list of outside points).  If for some reason there is one with a negative distance, it shouldn't be considered, because it sits inside the hull
                {
                    maxDistance = distance;
                    retVal = pointIndicies[cntr];
                }
            }

            if (retVal < 0)
            {
                throw new ApplicationException("Didn't find a return point, this should never happen");
            }

            // Exit Function
            return retVal;
        }
        private static int ProcessTriangleSprtFarthestPoint_ORIG(TriangleWithPoints triangle)
        {
            //NOTE:  I'm copying bits of Math3D.DistanceFromPlane here for optimization reasons
            //NOTE:  Also note that this is an almost exact copy of GetStartingTetrahedronSprtFarthestFromPlane.  But I'm not worried about duplication and maintainability - once written, this code will never change

            Vector3D normal = triangle.Triangle.NormalUnit;
            //Vector3D normal = Math3D.Normal(new Vector3D[] { triangle.Triangle.Point0.ToVector(), triangle.Triangle.Point1.ToVector(), triangle.Triangle.Point2.ToVector() });

            double originDistance = Math3D.GetPlaneOriginDistance(normal, triangle.Triangle.Point0);

            double maxDistance = 0d;
            int retVal = -1;

            List<int> pointIndicies = triangle.OutsidePoints;
            Point3D[] allPoints = triangle.Triangle.AllPoints;

            for (int cntr = 0; cntr < pointIndicies.Count; cntr++)
            {
                //NOTE:  This is Math3D.DistanceFromPlane copied inline to speed things up
                Point3D point = allPoints[pointIndicies[cntr]];
                double distance = ((normal.X * point.X) +					// Ax +
                                                (normal.Y * point.Y) +					// Bx +
                                                (normal.Z * point.Z)) + originDistance;	// Cz + D

                if (distance > maxDistance)		// distance should never be negative (or the point wouldn't be in the list of outside points).  If for some reason there is one with a negative distance, it shouldn't be considered, because it sits inside the hull
                {
                    maxDistance = distance;
                    retVal = cntr;
                }
            }

            if (retVal < 0)
            {
                throw new ApplicationException("Didn't find a return point, this should never happen");
            }

            // Exit Function
            return retVal;
        }
        /// <summary>
        /// This returns a set of point pairs that define the rim of the hull as seen from the new point (each array in the return has 2 elements)
        /// </summary>
        /// <remarks>
        /// I looked all over for a geometric way of doing this, but I think the simplest is to find the verticies in the removed triangles list that
        /// are still in the live hull. (but I doubt it's the most efficient way)
        /// </remarks>
        private static List<int[]> ProcessTriangleSprtGetHorizon(List<TriangleWithPoints> removedTriangles, List<TriangleWithPoints> hull)
        {
            List<int[]> retVal = new List<int[]>();
            List<int> unusedHullPointers = Enumerable.Range(0, hull.Count).ToList();

            foreach (TriangleWithPoints removed in removedTriangles)
            {
                // Find triangles in the hull that share 2 points with this removed triangle, and grab those two points
                retVal.AddRange(ProcessTriangleSprtGetHorizonSprtFind(unusedHullPointers, removed, hull));
            }

            return retVal;
        }
        private static List<int[]> ProcessTriangleSprtGetHorizonSprtFind(List<int> unusedHullPointers, TriangleWithPoints removed, List<TriangleWithPoints> hull)
        {
            List<int[]> retVal = new List<int[]>();

            int[] removedPoints = removed.Triangle.IndexArray;

            //foreach (TriangleWithPoints triangle in hull)
            for (int cntr = 0; cntr < unusedHullPointers.Count; cntr++)
            {
                IEnumerable<int> sharedPoints = removedPoints.Intersect(hull[unusedHullPointers[cntr]].Triangle.IndexArray);

                //NOTE:  If there is one point in common, I don't care.  There will be another triangle that has two points in common (one of them being this one point)
                if (sharedPoints.Count() == 2)
                {
                    //unusedHullPointers.RemoveAt(cntr);		// this triangle won't be needed again (maybe)
                    retVal.Add(sharedPoints.ToArray());		// since there's only 2, I'll cheat
                }
            }

            // Exit Function
            return retVal;
        }
        private static void ProcessTriangleSprtAddNew(int newIndex, List<TriangleWithPoints> hull, List<int[]> horizonRidge, List<int> allOutsidePoints, Point3D[] allPoints)
        {
            Vector3D newPoint = allPoints[newIndex].ToVector();

            foreach (int[] ridgeSegment in horizonRidge)
            {


                //TriangleIndexed triangle = new TriangleIndexed(ridgeSegment[0], ridgeSegment[1], newIndex, allPoints);
                //if (Vector3D.DotProduct(newPoint, triangle.Normal) > 0d)
                //{
                //    triangle = new TriangleIndexed(ridgeSegment[0], newIndex, ridgeSegment[1], allPoints);
                //}






                // Find a triangle vertex in the hull that isn't one of these two ridge segments
                // Since every triangle has 3 points, and it only doesn't need to be these two, and there's always at least one triangle, I'll just pick
                // one of the vertices from hull[0]
                int indexWithinHull = -1;
                if (hull[0].Triangle.Index0 != ridgeSegment[0] && hull[0].Triangle.Index0 != ridgeSegment[1])
                {
                    indexWithinHull = hull[0].Triangle.Index0;
                }
                else if (hull[0].Triangle.Index1 != ridgeSegment[0] && hull[0].Triangle.Index1 != ridgeSegment[1])
                {
                    indexWithinHull = hull[0].Triangle.Index1;
                }
                else
                {
                    indexWithinHull = hull[0].Triangle.Index2;
                }



                TriangleIndexed triangle = CreateTriangle(ridgeSegment[0], ridgeSegment[1], newIndex, indexWithinHull, allPoints);





                TriangleWithPoints triangleWrapper = new TriangleWithPoints(triangle);

                triangleWrapper.OutsidePoints.AddRange(GetOutsideSet(triangle, allOutsidePoints, allPoints));

                hull.Add(triangleWrapper);
            }



            //TODO: Create a triangle between the first and last ridge item - but which points to choose? - look at the hull for a triangle that contains 2 of the 4 points
            //TODO: Move this piece into the other method



        }

        /// <summary>
        /// This takes in 3 points that belong to the triangle, and a point that is not on the triangle, but is toward the rest
        /// of the hull.  It then creates a triangle whose normal points away from the hull (right hand rule)
        /// </summary>
        private static TriangleIndexed CreateTriangle(int point0, int point1, int point2, int pointWithinHull, Point3D[] allPoints)
        {
            // Try an arbitrary orientation
            TriangleIndexed retVal = new TriangleIndexed(point0, point1, point2, allPoints);

            // Get a vector pointing from point0 to the inside point
            Vector3D towardHull = allPoints[pointWithinHull] - allPoints[point0];

            if (Vector3D.DotProduct(towardHull, retVal.Normal) > 0d)
            {
                // When the dot product is greater than zero, that means the normal points in the same direction as the vector that points
                // toward the hull.  So buid a triangle that points in the opposite direction
                retVal = new TriangleIndexed(point0, point2, point1, allPoints);
            }

            // Exit Function
            return retVal;
        }

        /// <summary>
        /// This returns the subset of points that are on the outside facing side of the triangle
        /// </summary>
        /// <remarks>
        /// I got these steps here:
        /// http://www.gamedev.net/topic/106765-determining-if-a-point-is-in-front-of-or-behind-a-plane/
        /// </remarks>
        public static List<int> GetOutsideSet(TriangleIndexed triangle, List<int> pointIndicies, Point3D[] allPoints)
        {
            List<int> retVal = new List<int>();

            // Compute D, using a arbitrary point P, that lies on the plane: D = - (Nx*Px + Ny*Py + Nz*Pz); Don't forget the inversion !
            double D = -((triangle.NormalUnit.X * triangle.Point0.X) + (triangle.NormalUnit.Y * triangle.Point0.Y) + (triangle.NormalUnit.Z * triangle.Point0.Z));

            foreach (int index in pointIndicies)
            {
                if (index == triangle.Index0 || index == triangle.Index1 || index == triangle.Index2)
                {
                    continue;
                }

                // You can test a point (T) with respect to the plane using the plane equation: res = Nx*Tx + Ny*Ty + Nz*Tz + D
                double res = (triangle.NormalUnit.X * allPoints[index].X) + (triangle.NormalUnit.Y * allPoints[index].Y) + (triangle.NormalUnit.Z * allPoints[index].Z) + D;

                if (res > 0d)		// anything greater than zero lies outside the plane
                {
                    retVal.Add(index);
                }
            }

            return retVal;
        }

        #endregion
    }

    #endregion
    #region Class: QuickHull6

    public static class QuickHull6
    {
        #region Class: TriangleWithPoints

        /// <summary>
        /// This links a triangle with a set of points that sit "outside" the triangle
        /// </summary>
        public class TriangleWithPoints : TriangleIndexedLinked
        {
            public TriangleWithPoints(int index0, int index1, int index2, Point3D[] allPoints)
                : base(index0, index1, index2, allPoints)
            {
                this.OutsidePoints = new List<int>();
            }

            public List<int> OutsidePoints
            {
                get;
                private set;
            }
        }

        #endregion

        public static TriangleIndexed[] GetQuickHull(Point3D[] points)
        {
            if (points.Length < 4)
            {
                throw new ArgumentException("There must be at least 4 points", "points");
            }

            // Pick 4 points
            int[] startPoints = GetStartingTetrahedron(points);
            List<TriangleWithPoints> retVal = ConvertStartingPointsToTriangles(startPoints, points);


            //for (int cntr = 0; cntr < 2; cntr++)
            //{
            //    if (retVal[0].OutsidePoints.Count > 0)
            //    {
            //        ProcessTriangle(retVal, 0, points);
            //    }
            //}


            // If any triangle has any points outside the hull, then remove that triangle, and replace it with triangles connected to the
            // farthest out point (relative to the triangle that got removed)
            bool foundOne;
            do
            {
                foundOne = false;
                int index = 0;
                while (index < retVal.Count)
                {
                    if (retVal[index].OutsidePoints.Count > 0)
                    {
                        foundOne = true;
                        ProcessTriangle(retVal, index, points);
                    }
                    else
                    {
                        index++;
                    }
                }
            } while (foundOne);







            //List<TriangleWithPoints> errors = retVal.Where(o => o.Neighbor_01 == null || o.Neighbor_12 == null || o.Neighbor_20 == null).ToList();



            // Exit Function
            return retVal.ToArray();
        }

        #region Private Methods

        /// <summary>
        /// I've seen various literature call this initial tetrahedron a simplex
        /// </summary>
        private static int[] GetStartingTetrahedron(Point3D[] points)
        {
            int[] retVal = new int[4];

            // Validate the point cloud is 3D, also get the points that have the smallest and largest X (an exception is thrown if they are the same point)
            int minXIndex, maxXIndex;
            GetStartingTetrahedronSprtGetMinMaxX(out minXIndex, out maxXIndex, points);

            // The first two return points will be the ones with the min and max X values
            retVal[0] = minXIndex;
            retVal[1] = maxXIndex;

            // The third point will be the one that is farthest from the line defined by the first two
            int thirdIndex = GetStartingTetrahedronSprtFarthestFromLine(minXIndex, maxXIndex, points);
            retVal[2] = thirdIndex;

            // The fourth point will be the one that is farthest from the plane defined by the first three
            int fourthIndex = GetStartingTetrahedronSprtFarthestFromPlane(minXIndex, maxXIndex, thirdIndex, points);
            retVal[3] = fourthIndex;

            // Exit Function
            return retVal;
        }
        /// <summary>
        /// This does more than just look at X.  It looks at Y and Z as well to verify the point cloud has points in all 3 dimensions
        /// </summary>
        private static void GetStartingTetrahedronSprtGetMinMaxX(out int minXIndex, out int maxXIndex, Point3D[] points)
        {
            // Create arrays to hold the min and max along each axis (0=X, 1=Y, 2=Z) - using the first point
            double[] minValues = new double[] { points[0].X, points[0].Y, points[0].Z };
            double[] maxValues = new double[] { points[0].X, points[0].Y, points[0].Z };
            int[] minIndicies = new int[] { 0, 0, 0 };
            int[] maxIndicies = new int[] { 0, 0, 0 };

            for (int cntr = 1; cntr < points.Length; cntr++)
            {
                #region Examine Point

                // Min
                if (points[cntr].X < minValues[0])
                {
                    minValues[0] = points[cntr].X;
                    minIndicies[0] = cntr;
                }

                if (points[cntr].Y < minValues[1])
                {
                    minValues[1] = points[cntr].Y;
                    minIndicies[1] = cntr;
                }

                if (points[cntr].Z < minValues[2])
                {
                    minValues[2] = points[cntr].Z;
                    minIndicies[2] = cntr;
                }

                // Max
                if (points[cntr].X > maxValues[0])
                {
                    maxValues[0] = points[cntr].X;
                    maxIndicies[0] = cntr;
                }

                if (points[cntr].Y > maxValues[1])
                {
                    maxValues[1] = points[cntr].Y;
                    maxIndicies[1] = cntr;
                }

                if (points[cntr].Z > maxValues[2])
                {
                    maxValues[2] = points[cntr].Z;
                    maxIndicies[2] = cntr;
                }

                #endregion
            }

            #region Validate

            for (int cntr = 0; cntr < 3; cntr++)
            {
                if (maxValues[cntr] == minValues[cntr])
                {
                    throw new ApplicationException("The points passed in aren't in 3D - they are either all on the same point (0D), or colinear (1D), or coplanar (2D)");
                }
            }

            #endregion

            // Return the two points that are the min/max X
            minXIndex = minIndicies[0];
            maxXIndex = maxIndicies[0];
        }
        /// <summary>
        /// Finds the point that is farthest from the line
        /// </summary>
        private static int GetStartingTetrahedronSprtFarthestFromLine(int index1, int index2, Point3D[] points)
        {
            Vector3D lineDirection = points[index2] - points[index1];		// The nearest point method wants a vector, so calculate it once
            Point3D startPoint = points[index1];		// not sure if there is much of a penalty to referencing a list by index, but I figure I'll just cache this point once

            double maxDistance = -1d;
            int retVal = -1;

            for (int cntr = 0; cntr < points.Length; cntr++)
            {
                if (cntr == index1 || cntr == index2)
                {
                    continue;
                }

                // Calculate the distance from the line
                Point3D nearestPoint = Math3D.GetClosestPoint_Line_Point(startPoint, lineDirection, points[cntr]);
                double distanceSquared = (points[cntr] - nearestPoint).LengthSquared;

                if (distanceSquared > maxDistance)
                {
                    maxDistance = distanceSquared;
                    retVal = cntr;
                }
            }

            if (retVal < 0)
            {
                throw new ApplicationException("Didn't find a return point, this should never happen");
            }

            // Exit Function
            return retVal;
        }
        private static int GetStartingTetrahedronSprtFarthestFromPlane(int index1, int index2, int index3, Point3D[] points)
        {
            //NOTE:  I'm copying bits of Math3D.DistanceFromPlane here for optimization reasons
            //Vector3D[] triangle = new Vector3D[] { points[index1].ToVector(), points[index2].ToVector(), points[index3].ToVector() };
            //Vector3D normal = Math3D.Normal(triangle);
            //double originDistance = Math3D.GetPlaneOriginDistance(normal, triangle[0].ToPoint());
            Triangle triangle = new Triangle(points[index1], points[index2], points[index3]);
            Vector3D normal = triangle.NormalUnit;
            double originDistance = Math3D.GetPlaneOriginDistance(normal, triangle.Point0);

            double maxDistance = 0d;
            int retVal = -1;

            for (int cntr = 0; cntr < points.Length; cntr++)
            {
                if (cntr == index1 || cntr == index2 || cntr == index3)
                {
                    continue;
                }

                //NOTE:  This is Math3D.DistanceFromPlane copied inline to speed things up
                Point3D point = points[cntr];
                double distance = ((normal.X * point.X) +					// Ax +
                                                (normal.Y * point.Y) +					// Bx +
                                                (normal.Z * point.Z)) + originDistance;	// Cz + D

                // I don't care which side of the triangle the point is on for this initial tetrahedron
                distance = Math.Abs(distance);

                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    retVal = cntr;
                }
            }

            if (retVal < 0)
            {
                throw new ApplicationException("Didn't find a return point, this should never happen");
            }

            // Exit Function
            return retVal;
        }

        private static List<TriangleWithPoints> ConvertStartingPointsToTriangles(int[] startPoints, Point3D[] allPoints)
        {
            if (startPoints.Length != 4)
            {
                throw new ArgumentException("This method expects exactly 4 points passed in");
            }

            List<TriangleWithPoints> retVal = new List<TriangleWithPoints>();

            // Make triangles
            retVal.Add(CreateTriangle(startPoints[0], startPoints[1], startPoints[2], allPoints[startPoints[3]], allPoints));
            retVal.Add(CreateTriangle(startPoints[3], startPoints[1], startPoints[0], allPoints[startPoints[2]], allPoints));
            retVal.Add(CreateTriangle(startPoints[3], startPoints[2], startPoints[1], allPoints[startPoints[0]], allPoints));
            retVal.Add(CreateTriangle(startPoints[3], startPoints[0], startPoints[2], allPoints[startPoints[1]], allPoints));

            // Link triangles together
            TriangleIndexedLinked.LinkTriangles_Edges(retVal.ConvertAll(o => (TriangleIndexedLinked)o).ToList(), true);

            #region Calculate outside points

            // GetOutsideSet wants indicies to the points it needs to worry about.  This initial call needs all points
            List<int> allPointIndicies = Enumerable.Range(0, allPoints.Length).ToList();

            // For every triangle, find the points that are outside the polygon (not the points behind the triangle)
            // Note that a point will never be shared between triangles
            foreach (TriangleWithPoints triangle in retVal)
            {
                triangle.OutsidePoints.AddRange(GetOutsideSet(triangle, allPointIndicies, allPoints));
            }

            #endregion

            // Exit Function
            return retVal;
        }

        /// <summary>
        /// This works on a triangle that contains outside points.  It removes the triangle, and creates new ones connected to
        /// the outermost outside point
        /// </summary>
        private static void ProcessTriangle(List<TriangleWithPoints> hull, int hullIndex, Point3D[] allPoints)
        {
            TriangleWithPoints removedTriangle = hull[hullIndex];

            // Find the farthest point from this triangle
            int fartherstIndex = ProcessTriangleSprtFarthestPoint(removedTriangle);
            if (fartherstIndex < 0)
            {
                // The outside points are on the same plane as this triangle.  Just wipe them and go away
                removedTriangle.OutsidePoints.Clear();
                return;
            }

            //Key=which triangles to remove from the hull
            //Value=meaningless, I just wanted a sorted list
            SortedList<TriangleIndexedLinked, int> removedTriangles = new SortedList<TriangleIndexedLinked, int>();

            //Key=triangle on the hull that is a boundry (the new triangles will be added to these boundry triangles)
            //Value=the key's edges that are exposed to the removed triangles
            SortedList<TriangleIndexedLinked, List<TriangleEdge>> removedRim = new SortedList<TriangleIndexedLinked, List<TriangleEdge>>();

            // Find all the triangles that can see this point (they will need to be removed from the hull)
            //NOTE:  This method is recursive
            ProcessTriangleSprtRemove(removedTriangle, removedTriangles, removedRim, allPoints[fartherstIndex].ToVector());

            // Remove these from the hull
            ProcessTriangleSprtRemoveFromHull(hull, removedTriangles.Keys, removedRim);

            // Get all the outside points
            //List<int> allOutsidePoints1 = removedTriangles.Keys.SelectMany(o => ((TriangleWithPoints)o).OutsidePoints).Distinct().ToList();		// if you try to read what SelectMany does, it gets pretty wordy and unclear.  It just does a foreach across all the OutsidePoints lists from all the removedTriangles (treats the list of lists like a single list)
            List<int> allOutsidePoints2 = removedTriangles.Keys.SelectMany(o => ((TriangleWithPoints)o).OutsidePoints).ToList();		// there's no need to call distinct, since the outside points aren't shared between triangles

            // Create new triangles
            ProcessTriangleSprtNew(hull, fartherstIndex, removedRim, allPoints, allOutsidePoints2);		// Note that after this method, allOutsidePoints will only be the points left over (the ones that are inside the hull)
        }
        private static int ProcessTriangleSprtFarthestPoint(TriangleWithPoints triangle)
        {
            Vector3D[] polygon = new Vector3D[] { triangle.Point0.ToVector(), triangle.Point1.ToVector(), triangle.Point2.ToVector() };

            List<int> pointIndicies = triangle.OutsidePoints;
            Point3D[] allPoints = triangle.AllPoints;

            double maxDistance = 0d;
            int retVal = -1;

            for (int cntr = 0; cntr < pointIndicies.Count; cntr++)
            {
                double distance = Math3D.DistanceFromPlane(polygon, allPoints[pointIndicies[cntr]].ToVector());

                if (distance > maxDistance)		// distance should never be negative (or the point wouldn't be in the list of outside points).  If for some reason there is one with a negative distance, it shouldn't be considered, because it sits inside the hull
                {
                    maxDistance = distance;
                    retVal = pointIndicies[cntr];
                }
            }

            // This can happen when the outside points are on the same plane as the triangle
            //if (retVal < 0)
            //{
            //    throw new ApplicationException("Didn't find a return point, this should never happen");
            //}

            // Exit Function
            return retVal;
        }

        private static void ProcessTriangleSprtRemove(TriangleIndexedLinked triangle, SortedList<TriangleIndexedLinked, int> removedTriangles, SortedList<TriangleIndexedLinked, List<TriangleEdge>> removedRim, Vector3D farPoint)
        {
            // This triangle will need to be removed.  Keep track of it so it's not processed again (the int means nothing)
            removedTriangles.Add(triangle, 0);

            // Try each neighbor
            ProcessTriangleSprtRemoveSprtNeighbor(triangle.Neighbor_01, removedTriangles, removedRim, farPoint, triangle);
            ProcessTriangleSprtRemoveSprtNeighbor(triangle.Neighbor_12, removedTriangles, removedRim, farPoint, triangle);
            ProcessTriangleSprtRemoveSprtNeighbor(triangle.Neighbor_20, removedTriangles, removedRim, farPoint, triangle);
        }
        private static void ProcessTriangleSprtRemoveSprtNeighbor(TriangleIndexedLinked triangle, SortedList<TriangleIndexedLinked, int> removedTriangles, SortedList<TriangleIndexedLinked, List<TriangleEdge>> removedRim, Vector3D farPoint, TriangleIndexedLinked fromTriangle)
        {
            if (removedTriangles.ContainsKey(triangle))
            {
                return;
            }

            if (removedRim.ContainsKey(triangle))
            {
                // This triangle is already recognized as part of the hull.  Add a link from it to the from triangle (because two of its edges
                // are part of the hull rim)
                removedRim[triangle].Add(triangle.WhichEdge(fromTriangle));
                return;
            }

            // Need to subtract the far point from some point on this triangle, so that it's a vector from the triangle to the
            // far point, and not from the origin
            if (Vector3D.DotProduct(triangle.Normal, (farPoint - triangle.Point0).ToVector()) > 0d)		// 0 would be coplanar, -1 would be the opposite side
            {
                // This triangle is visible to the point.  Remove it (recurse)
                ProcessTriangleSprtRemove(triangle, removedTriangles, removedRim, farPoint);
            }
            else
            {
                // This triangle is invisible to the point, so needs to stay part of the hull.  Store this boundry
                removedRim.Add(triangle, new List<TriangleEdge>());
                removedRim[triangle].Add(triangle.WhichEdge(fromTriangle));
            }
        }

        private static void ProcessTriangleSprtRemoveFromHull(List<TriangleWithPoints> hull, IList<TriangleIndexedLinked> trianglesToRemove, SortedList<TriangleIndexedLinked, List<TriangleEdge>> removedRim)
        {
            // Remove from the hull list
            foreach (TriangleIndexedLinked triangle in trianglesToRemove)
            {
                hull.Remove((TriangleWithPoints)triangle);
            }

            // Break the links from the hull to the removed triangles (there's no need to break links in the other direction.  The removed triangles
            // will become orphaned, and eventually garbage collected)
            foreach (TriangleIndexedLinked triangle in removedRim.Keys)
            {
                foreach (TriangleEdge edge in removedRim[triangle])
                {
                    triangle.SetNeighbor(edge, null);
                }
            }
        }

        private static void ProcessTriangleSprtNew(List<TriangleWithPoints> hull, int fartherstIndex, SortedList<TriangleIndexedLinked, List<TriangleEdge>> removedRim, Point3D[] allPoints, List<int> outsidePoints)
        {
            List<TriangleWithPoints> newTriangles = new List<TriangleWithPoints>();

            // Run around the rim, and build a triangle between the far point and each edge
            foreach (TriangleIndexedLinked rimTriangle in removedRim.Keys)
            {
                // Get a point that is toward the hull (the created triangle will be built so its normal points away from this)
                Point3D insidePoint = ProcessTriangleSprtNewSprtInsidePoint(rimTriangle, removedRim[rimTriangle]);

                foreach (TriangleEdge rimEdge in removedRim[rimTriangle])
                {
                    // Get the points for this edge
                    int index1, index2;
                    rimTriangle.GetIndices(out index1, out index2, rimEdge);

                    // Build the triangle
                    TriangleWithPoints triangle = CreateTriangle(fartherstIndex, index1, index2, insidePoint, allPoints);

                    // Now link this triangle with the boundry triangle (just the one edge, the other edges will be joined later)
                    TriangleIndexedLinked.LinkTriangles_Edges(triangle, rimTriangle);

                    // Store this triangle
                    newTriangles.Add(triangle);
                    hull.Add(triangle);
                }
            }

            // The new triangles are linked to the boundry already.  Now link the other two edges to each other (newTriangles forms a
            // triangle fan, but they aren't neccessarily consecutive)
            TriangleIndexedLinked.LinkTriangles_Edges(newTriangles.ConvertAll(o => (TriangleIndexedLinked)o).ToList(), false);

            // Distribute the outside points to these new triangles
            foreach (TriangleWithPoints triangle in newTriangles)
            {
                // Find the points that are outside the polygon (not the points behind the triangle)
                // Note that a point will never be shared between triangles
                triangle.OutsidePoints.AddRange(GetOutsideSet(triangle, outsidePoints, allPoints));
            }
        }
        private static Point3D ProcessTriangleSprtNewSprtInsidePoint(TriangleIndexedLinked rimTriangle, List<TriangleEdge> sharedEdges)
        {
            bool[] used = new bool[3];

            // Figure out which indices are used
            foreach (TriangleEdge edge in sharedEdges)
            {
                switch (edge)
                {
                    case TriangleEdge.Edge_01:
                        used[0] = true;
                        used[1] = true;
                        break;

                    case TriangleEdge.Edge_12:
                        used[1] = true;
                        used[2] = true;
                        break;

                    case TriangleEdge.Edge_20:
                        used[2] = true;
                        used[0] = true;
                        break;

                    default:
                        throw new ApplicationException("Unknown TriangleEdge: " + edge.ToString());
                }
            }

            // Find one that isn't used
            for (int cntr = 0; cntr < 3; cntr++)
            {
                if (!used[cntr])
                {
                    return rimTriangle[cntr];
                }
            }

            // Project a point away from this triangle
            //TODO:  If the hull ends up concave, this is a very likely culprit.  May need to come up with a better way
            return rimTriangle.GetCenterPoint() + (rimTriangle.Normal * -.001);		// by not using the unit normal, I'm keeping this scaled roughly to the size of the triangle
        }

        /// <summary>
        /// This takes in 3 points that belong to the triangle, and a point that is not on the triangle, but is toward the rest
        /// of the hull.  It then creates a triangle whose normal points away from the hull (right hand rule)
        /// </summary>
        private static TriangleWithPoints CreateTriangle(int point0, int point1, int point2, Point3D pointWithinHull, Point3D[] allPoints)
        {
            // Try an arbitrary orientation
            TriangleWithPoints retVal = new TriangleWithPoints(point0, point1, point2, allPoints);

            // Get a vector pointing from point0 to the inside point
            Vector3D towardHull = pointWithinHull - allPoints[point0];

            if (Vector3D.DotProduct(towardHull, retVal.Normal) > 0d)
            {
                // When the dot product is greater than zero, that means the normal points in the same direction as the vector that points
                // toward the hull.  So buid a triangle that points in the opposite direction
                retVal = new TriangleWithPoints(point0, point2, point1, allPoints);
            }

            // Exit Function
            return retVal;
        }

        /// <summary>
        /// This returns the subset of points that are on the outside facing side of the triangle
        /// </summary>
        /// <remarks>
        /// I got these steps here:
        /// http://www.gamedev.net/topic/106765-determining-if-a-point-is-in-front-of-or-behind-a-plane/
        /// </remarks>
        /// <param name="pointIndicies">
        /// This method will only look at the points in pointIndicies.
        /// NOTE: This method will also remove values from here if they are part of an existing triangle, or get added to a triangle's outside list.
        /// </param>
        private static List<int> GetOutsideSet(TriangleIndexed triangle, List<int> pointIndicies, Point3D[] allPoints)
        {
            List<int> retVal = new List<int>();

            // Compute D, using a arbitrary point P, that lies on the plane: D = - (Nx*Px + Ny*Py + Nz*Pz); Don't forget the inversion !
            double D = -((triangle.NormalUnit.X * triangle.Point0.X) + (triangle.NormalUnit.Y * triangle.Point0.Y) + (triangle.NormalUnit.Z * triangle.Point0.Z));

            int cntr = 0;
            while (cntr < pointIndicies.Count)
            {
                int index = pointIndicies[cntr];

                if (index == triangle.Index0 || index == triangle.Index1 || index == triangle.Index2)
                {
                    pointIndicies.Remove(index);		// no need to consider this for future calls
                    continue;
                }

                // You can test a point (T) with respect to the plane using the plane equation: res = Nx*Tx + Ny*Ty + Nz*Tz + D
                double res = (triangle.NormalUnit.X * allPoints[index].X) + (triangle.NormalUnit.Y * allPoints[index].Y) + (triangle.NormalUnit.Z * allPoints[index].Z) + D;

                if (res > 0d)		// anything greater than zero lies outside the plane
                {
                    retVal.Add(index);
                    pointIndicies.Remove(index);		// an outside point can only belong to one triangle
                }
                else
                {
                    cntr++;
                }
            }

            return retVal;
        }

        #endregion
    }

    #endregion
    #region Class: QuickHull7

    //TODO:  This still fails with lots of coplanar points.  Triangles overlap, not all resulting triangles have neighbors.
    //I think a preprocess is nessassary
    //
    // A preprocess wasn't nessassary (fixed in quickhull8) plane distance of 0 and dot products of 0 needed to be handled special

    public static class QuickHull7
    {
        #region Class: TriangleWithPoints

        /// <summary>
        /// This links a triangle with a set of points that sit "outside" the triangle
        /// </summary>
        public class TriangleWithPoints : TriangleIndexedLinked
        {
            public TriangleWithPoints(int index0, int index1, int index2, Point3D[] allPoints)
                : base(index0, index1, index2, allPoints)
            {
                this.OutsidePoints = new List<int>();
            }

            public List<int> OutsidePoints
            {
                get;
                private set;
            }
        }

        #endregion

        #region Declaration Section

        private const double COPLANARDOTPRODUCT = .01d;

        #endregion

        public static TriangleIndexed[] GetConvexHull(Point3D[] points)
        {
            if (points.Length < 4)
            {
                throw new ArgumentException("There must be at least 4 points", "points");
            }

            // Pick 4 points
            double coplanarDistance;
            int[] startPoints = GetStartingTetrahedron(out coplanarDistance, points);
            List<TriangleWithPoints> retVal = ConvertStartingPointsToTriangles(startPoints, points, coplanarDistance);

            // If any triangle has any points outside the hull, then remove that triangle, and replace it with triangles connected to the
            // farthest out point (relative to the triangle that got removed)
            bool foundOne;
            do
            {
                foundOne = false;
                int index = 0;
                while (index < retVal.Count)
                {
                    if (retVal[index].OutsidePoints.Count > 0)
                    {
                        foundOne = true;
                        ProcessTriangle(retVal, index, points, coplanarDistance);
                    }
                    else
                    {
                        index++;
                    }
                }
            } while (foundOne);



            // Merge sets of coplanar triangles?




            // Exit Function
            return retVal.ToArray();
        }

        #region Private Methods

        /// <summary>
        /// I've seen various literature call this initial tetrahedron a simplex
        /// </summary>
        private static int[] GetStartingTetrahedron(out double coplanarDistance, Point3D[] points)
        {
            int[] retVal = new int[4];

            // Validate the point cloud is 3D, also get the points that have the smallest and largest X (an exception is thrown if they are the same point)
            int minXIndex, maxXIndex;
            GetStartingTetrahedronSprtGetMinMaxX(out minXIndex, out maxXIndex, out coplanarDistance, points);

            // The first two return points will be the ones with the min and max X values
            retVal[0] = minXIndex;
            retVal[1] = maxXIndex;

            // The third point will be the one that is farthest from the line defined by the first two
            int thirdIndex = GetStartingTetrahedronSprtFarthestFromLine(minXIndex, maxXIndex, points);
            retVal[2] = thirdIndex;

            // The fourth point will be the one that is farthest from the plane defined by the first three
            int fourthIndex = GetStartingTetrahedronSprtFarthestFromPlane(minXIndex, maxXIndex, thirdIndex, points);
            retVal[3] = fourthIndex;

            // Exit Function
            return retVal;
        }
        /// <summary>
        /// This does more than just look at X.  It looks at Y and Z as well to verify the point cloud has points in all 3 dimensions
        /// </summary>
        private static void GetStartingTetrahedronSprtGetMinMaxX(out int minXIndex, out int maxXIndex, out double coplanarDistance, Point3D[] points)
        {
            // Create arrays to hold the min and max along each axis (0=X, 1=Y, 2=Z) - using the first point
            double[] minValues = new double[] { points[0].X, points[0].Y, points[0].Z };
            double[] maxValues = new double[] { points[0].X, points[0].Y, points[0].Z };
            int[] minIndicies = new int[] { 0, 0, 0 };
            int[] maxIndicies = new int[] { 0, 0, 0 };

            for (int cntr = 1; cntr < points.Length; cntr++)
            {
                #region Examine Point

                // Min
                if (points[cntr].X < minValues[0])
                {
                    minValues[0] = points[cntr].X;
                    minIndicies[0] = cntr;
                }

                if (points[cntr].Y < minValues[1])
                {
                    minValues[1] = points[cntr].Y;
                    minIndicies[1] = cntr;
                }

                if (points[cntr].Z < minValues[2])
                {
                    minValues[2] = points[cntr].Z;
                    minIndicies[2] = cntr;
                }

                // Max
                if (points[cntr].X > maxValues[0])
                {
                    maxValues[0] = points[cntr].X;
                    maxIndicies[0] = cntr;
                }

                if (points[cntr].Y > maxValues[1])
                {
                    maxValues[1] = points[cntr].Y;
                    maxIndicies[1] = cntr;
                }

                if (points[cntr].Z > maxValues[2])
                {
                    maxValues[2] = points[cntr].Z;
                    maxIndicies[2] = cntr;
                }

                #endregion
            }

            #region Validate

            for (int cntr = 0; cntr < 3; cntr++)
            {
                if (maxValues[cntr] == minValues[cntr])
                {
                    throw new ApplicationException("The points passed in aren't in 3D - they are either all on the same point (0D), or colinear (1D), or coplanar (2D)");
                }
            }

            #endregion

            #region Calculate error distance

            double smallestAxis = Math1D.Min(maxValues[0] - minValues[0], maxValues[1] - minValues[1], maxValues[2] - minValues[2]);
            coplanarDistance = smallestAxis * .001;

            #endregion

            // Return the two points that are the min/max X
            minXIndex = minIndicies[0];
            maxXIndex = maxIndicies[0];
        }
        /// <summary>
        /// Finds the point that is farthest from the line
        /// </summary>
        private static int GetStartingTetrahedronSprtFarthestFromLine(int index1, int index2, Point3D[] points)
        {
            Vector3D lineDirection = points[index2] - points[index1];		// The nearest point method wants a vector, so calculate it once
            Point3D startPoint = points[index1];		// not sure if there is much of a penalty to referencing a list by index, but I figure I'll just cache this point once

            double maxDistance = -1d;
            int retVal = -1;

            for (int cntr = 0; cntr < points.Length; cntr++)
            {
                if (cntr == index1 || cntr == index2)
                {
                    continue;
                }

                // Calculate the distance from the line
                Point3D nearestPoint = Math3D.GetClosestPoint_Line_Point(startPoint, lineDirection, points[cntr]);
                double distanceSquared = (points[cntr] - nearestPoint).LengthSquared;

                if (distanceSquared > maxDistance)
                {
                    maxDistance = distanceSquared;
                    retVal = cntr;
                }
            }

            if (retVal < 0)
            {
                throw new ApplicationException("Didn't find a return point, this should never happen");
            }

            // Exit Function
            return retVal;
        }
        private static int GetStartingTetrahedronSprtFarthestFromPlane(int index1, int index2, int index3, Point3D[] points)
        {
            //NOTE:  I'm copying bits of Math3D.DistanceFromPlane here for optimization reasons
            //Vector3D[] triangle = new Vector3D[] { points[index1].ToVector(), points[index2].ToVector(), points[index3].ToVector() };
            //Vector3D normal = Math3D.Normal(triangle);
            //double originDistance = Math3D.GetPlaneOriginDistance(normal, triangle[0].ToPoint());
            Triangle triangle = new Triangle(points[index1], points[index2], points[index3]);
            Vector3D normal = triangle.NormalUnit;
            double originDistance = Math3D.GetPlaneOriginDistance(normal, triangle.Point0);

            double maxDistance = 0d;
            int retVal = -1;

            for (int cntr = 0; cntr < points.Length; cntr++)
            {
                if (cntr == index1 || cntr == index2 || cntr == index3)
                {
                    continue;
                }

                //NOTE:  This is Math3D.DistanceFromPlane copied inline to speed things up
                Point3D point = points[cntr];
                double distance = ((normal.X * point.X) +					// Ax +
                                                (normal.Y * point.Y) +					// Bx +
                                                (normal.Z * point.Z)) + originDistance;	// Cz + D

                // I don't care which side of the triangle the point is on for this initial tetrahedron
                distance = Math.Abs(distance);

                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    retVal = cntr;
                }
            }

            if (retVal < 0)
            {
                throw new ApplicationException("Didn't find a return point, this should never happen");
            }

            // Exit Function
            return retVal;
        }

        private static List<TriangleWithPoints> ConvertStartingPointsToTriangles(int[] startPoints, Point3D[] allPoints, double coplanarDistance)
        {
            if (startPoints.Length != 4)
            {
                throw new ArgumentException("This method expects exactly 4 points passed in");
            }

            List<TriangleWithPoints> retVal = new List<TriangleWithPoints>();

            // Make triangles
            retVal.Add(CreateTriangle(startPoints[0], startPoints[1], startPoints[2], allPoints[startPoints[3]], allPoints));
            retVal.Add(CreateTriangle(startPoints[3], startPoints[1], startPoints[0], allPoints[startPoints[2]], allPoints));
            retVal.Add(CreateTriangle(startPoints[3], startPoints[2], startPoints[1], allPoints[startPoints[0]], allPoints));
            retVal.Add(CreateTriangle(startPoints[3], startPoints[0], startPoints[2], allPoints[startPoints[1]], allPoints));

            // Link triangles together
            TriangleIndexedLinked.LinkTriangles_Edges(retVal.ConvertAll(o => (TriangleIndexedLinked)o).ToList(), true);

            #region Calculate outside points

            // GetOutsideSet wants indicies to the points it needs to worry about.  This initial call needs all points
            List<int> allPointIndicies = Enumerable.Range(0, allPoints.Length).ToList();

            // For every triangle, find the points that are outside the polygon (not the points behind the triangle)
            // Note that a point will never be shared between triangles
            foreach (TriangleWithPoints triangle in retVal)
            {
                triangle.OutsidePoints.AddRange(GetOutsideSet(triangle, allPointIndicies, allPoints, coplanarDistance));
            }

            #endregion

            // Exit Function
            return retVal;
        }

        /// <summary>
        /// This works on a triangle that contains outside points.  It removes the triangle, and creates new ones connected to
        /// the outermost outside point
        /// </summary>
        private static void ProcessTriangle(List<TriangleWithPoints> hull, int hullIndex, Point3D[] allPoints, double coplanarDistance)
        {
            TriangleWithPoints removedTriangle = hull[hullIndex];

            // Find the farthest point from this triangle
            int fartherstIndex = ProcessTriangleSprtFarthestPoint(removedTriangle, coplanarDistance);
            if (fartherstIndex < 0)
            {
                // The outside points are on the same plane as this triangle, and inside that triangle.  Just wipe them and go away
                removedTriangle.OutsidePoints.Clear();
                return;
            }

            //Key=which triangles to remove from the hull
            //Value=meaningless, I just wanted a sorted list
            SortedList<TriangleIndexedLinked, int> removedTriangles = new SortedList<TriangleIndexedLinked, int>();

            //Key=triangle on the hull that is a boundry (the new triangles will be added to these boundry triangles)
            //Value=the key's edges that are exposed to the removed triangles
            SortedList<TriangleIndexedLinked, List<TriangleEdge>> removedRim = new SortedList<TriangleIndexedLinked, List<TriangleEdge>>();

            // Find all the triangles that can see this point (they will need to be removed from the hull)
            //NOTE:  This method is recursive
            ProcessTriangleSprtRemove(removedTriangle, removedTriangles, removedRim, allPoints[fartherstIndex].ToVector());

            // Remove these from the hull
            ProcessTriangleSprtRemoveFromHull(hull, removedTriangles.Keys, removedRim);

            // Get all the outside points
            //List<int> allOutsidePoints = removedTriangles.Keys.SelectMany(o => ((TriangleWithPoints)o).OutsidePoints).Distinct().ToList();		// if you try to read what SelectMany does, it gets pretty wordy and unclear.  It just does a foreach across all the OutsidePoints lists from all the removedTriangles (treats the list of lists like a single list)
            List<int> allOutsidePoints = removedTriangles.Keys.SelectMany(o => ((TriangleWithPoints)o).OutsidePoints).ToList();		// there's no need to call distinct, since the outside points aren't shared between triangles

            // Create new triangles
            ProcessTriangleSprtNew(hull, fartherstIndex, removedRim, allPoints, allOutsidePoints, coplanarDistance);		// Note that after this method, allOutsidePoints will only be the points left over (the ones that are inside the hull)
        }

        private static int ProcessTriangleSprtFarthestPoint(TriangleWithPoints triangle, double coplanarDistance)
        {
            Vector3D[] polygon = new Vector3D[] { triangle.Point0.ToVector(), triangle.Point1.ToVector(), triangle.Point2.ToVector() };

            List<int> pointIndicies = triangle.OutsidePoints;
            Point3D[] allPoints = triangle.AllPoints;

            double maxDistance = 0d;
            int retVal = -1;
            List<int> coplanarPoints = null;

            for (int cntr = 0; cntr < pointIndicies.Count; cntr++)
            {
                double distance = Math3D.DistanceFromPlane(polygon, allPoints[pointIndicies[cntr]].ToVector());

                if (Math.Abs(distance) < coplanarDistance)
                {
                    // This point has a distance near zero, so is considered on the same plane as this triangle
                    if (coplanarPoints == null)
                    {
                        coplanarPoints = new List<int>();
                    }

                    coplanarPoints.Add(pointIndicies[cntr]);
                }
                else if (distance > maxDistance)		// distance should never be negative (or the point wouldn't be in the list of outside points).  If for some reason there is one with a negative distance, it shouldn't be considered, because it sits inside the hull
                {
                    maxDistance = distance;
                    retVal = pointIndicies[cntr];
                }
            }

            if (retVal < 0 && coplanarPoints != null)
            {
                // All the rest of the points are on the same plane as this triangle

                // Only keep the coplanar points
                triangle.OutsidePoints.Clear();
                triangle.OutsidePoints.AddRange(coplanarPoints);

                retVal = ProcessTriangleSprtFarthestPointSprtCoplanar(triangle, triangle.OutsidePoints);		// note that the coplanar method will further reduce the outside point list for any points that are inside the triangle
            }

            // Exit Function
            return retVal;
        }
        private static int ProcessTriangleSprtFarthestPointSprtCoplanar(TriangleWithPoints triangle, List<int> coplanarPoints)
        {
            Point3D[] allPoints = triangle.AllPoints;

            #region Remove inside points

            // Remove all points that are inside the triangle

            List<TriangleEdge> nearestEdges = new List<TriangleEdge>();

            int index = 0;
            while (index < coplanarPoints.Count)
            {
                TriangleEdge? edge;
                if (ProcessTriangleSprtFarthestPointSprtCoplanarIsInside(out edge, triangle, allPoints[coplanarPoints[index]]))
                {
                    coplanarPoints.RemoveAt(index);
                }
                else
                {
                    nearestEdges.Add(edge.Value);
                    index++;
                }
            }

            if (coplanarPoints.Count == 0)
            {
                // Nothing left.  The triangle is already as big as it can be
                return -1;
            }

            #endregion

            // Now find the point that is farthest from the triangle

            double maxDistance = 0d;
            int retVal = -1;

            for (int cntr = 0; cntr < coplanarPoints.Count; cntr++)
            {
                Point3D point = allPoints[coplanarPoints[cntr]];

                Point3D nearestPoint;

                switch (nearestEdges[cntr])
                {
                    case TriangleEdge.Edge_01:
                        nearestPoint = Math3D.GetClosestPoint_Line_Point(triangle.Point0, triangle.Point1 - triangle.Point0, point);
                        break;

                    case TriangleEdge.Edge_12:
                        nearestPoint = Math3D.GetClosestPoint_Line_Point(triangle.Point1, triangle.Point2 - triangle.Point1, point);
                        break;

                    case TriangleEdge.Edge_20:
                        nearestPoint = Math3D.GetClosestPoint_Line_Point(triangle.Point0, triangle.Point2 - triangle.Point0, point);
                        break;

                    default:
                        throw new ApplicationException("Unknown TriangleEdge: " + nearestEdges[cntr].ToString());
                }

                double distance = (point - nearestPoint).LengthSquared;

                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    retVal = coplanarPoints[cntr];
                }
            }

            if (retVal < 0)
            {
                throw new ApplicationException("Didn't find a return point, this should never happen");
            }

            // Exit Function
            return retVal;
        }
        /// <summary>
        /// point is considered coplanar with triangle.  This returns whether the point is inside the triangle or not
        /// </summary>
        private static bool ProcessTriangleSprtFarthestPointSprtCoplanarIsInside(out TriangleEdge? nearestEdge, TriangleWithPoints triangle, Point3D point)
        {
            Vector bary = Math3D.ToBarycentric(triangle, point);

            // Check if point is in triangle
            bool retVal = (bary.X >= 0) && (bary.Y >= 0) && (bary.X + bary.Y <= 1);

            // Figure out which edge this point is closest to
            nearestEdge = null;
            if (!retVal)
            {
                if (bary.X < 0)
                {
                    nearestEdge = TriangleEdge.Edge_01;
                }
                else if (bary.Y < 0)
                {
                    nearestEdge = TriangleEdge.Edge_20;
                }
                else
                {
                    nearestEdge = TriangleEdge.Edge_12;
                }
            }

            return retVal;
        }

        private static void ProcessTriangleSprtRemove(TriangleIndexedLinked triangle, SortedList<TriangleIndexedLinked, int> removedTriangles, SortedList<TriangleIndexedLinked, List<TriangleEdge>> removedRim, Vector3D farPoint)
        {
            // This triangle will need to be removed.  Keep track of it so it's not processed again (the int means nothing)
            removedTriangles.Add(triangle, 0);

            // Try each neighbor
            ProcessTriangleSprtRemoveSprtNeighbor(triangle.Neighbor_01, removedTriangles, removedRim, farPoint, triangle);
            ProcessTriangleSprtRemoveSprtNeighbor(triangle.Neighbor_12, removedTriangles, removedRim, farPoint, triangle);
            ProcessTriangleSprtRemoveSprtNeighbor(triangle.Neighbor_20, removedTriangles, removedRim, farPoint, triangle);
        }
        private static void ProcessTriangleSprtRemoveSprtNeighbor(TriangleIndexedLinked triangle, SortedList<TriangleIndexedLinked, int> removedTriangles, SortedList<TriangleIndexedLinked, List<TriangleEdge>> removedRim, Vector3D farPoint, TriangleIndexedLinked fromTriangle)
        {
            if (removedTriangles.ContainsKey(triangle))
            {
                return;
            }

            if (removedRim.ContainsKey(triangle))
            {
                // This triangle is already recognized as part of the hull.  Add a link from it to the from triangle (because two of its edges
                // are part of the hull rim)
                removedRim[triangle].Add(triangle.WhichEdge(fromTriangle));
                return;
            }

            // Need to subtract the far point from some point on this triangle, so that it's a vector from the triangle to the
            // far point, and not from the origin
            double dot = Vector3D.DotProduct(triangle.NormalUnit, (farPoint - triangle.Point0).ToVector().ToUnit());

            // Need to allow coplanar, or there could end up with lots of coplanar triangles overlapping each other
            if (dot > 0d || Math.Abs(dot) < COPLANARDOTPRODUCT)		// 0 would be coplanar, -1 would be the opposite side
            {
                // This triangle is visible to the point.  Remove it (recurse)
                ProcessTriangleSprtRemove(triangle, removedTriangles, removedRim, farPoint);
            }
            else
            {
                // This triangle is invisible to the point, so needs to stay part of the hull.  Store this boundry
                removedRim.Add(triangle, new List<TriangleEdge>());
                removedRim[triangle].Add(triangle.WhichEdge(fromTriangle));
            }
        }

        private static void ProcessTriangleSprtRemoveFromHull(List<TriangleWithPoints> hull, IList<TriangleIndexedLinked> trianglesToRemove, SortedList<TriangleIndexedLinked, List<TriangleEdge>> removedRim)
        {
            // Remove from the hull list
            foreach (TriangleIndexedLinked triangle in trianglesToRemove)
            {
                hull.Remove((TriangleWithPoints)triangle);
            }

            // Break the links from the hull to the removed triangles (there's no need to break links in the other direction.  The removed triangles
            // will become orphaned, and eventually garbage collected)
            foreach (TriangleIndexedLinked triangle in removedRim.Keys)
            {
                foreach (TriangleEdge edge in removedRim[triangle])
                {
                    triangle.SetNeighbor(edge, null);
                }
            }
        }

        private static void ProcessTriangleSprtNew(List<TriangleWithPoints> hull, int fartherstIndex, SortedList<TriangleIndexedLinked, List<TriangleEdge>> removedRim, Point3D[] allPoints, List<int> outsidePoints, double coplanarDistance)
        {
            List<TriangleWithPoints> newTriangles = new List<TriangleWithPoints>();

            // Run around the rim, and build a triangle between the far point and each edge
            foreach (TriangleIndexedLinked rimTriangle in removedRim.Keys)
            {
                // Get a point that is toward the hull (the created triangle will be built so its normal points away from this)
                Point3D insidePoint = ProcessTriangleSprtNewSprtInsidePoint(rimTriangle, removedRim[rimTriangle]);

                foreach (TriangleEdge rimEdge in removedRim[rimTriangle])
                {
                    // Get the points for this edge
                    int index1, index2;
                    rimTriangle.GetIndices(out index1, out index2, rimEdge);

                    // Build the triangle
                    TriangleWithPoints triangle = CreateTriangle(fartherstIndex, index1, index2, insidePoint, allPoints);

                    // Now link this triangle with the boundry triangle (just the one edge, the other edges will be joined later)
                    TriangleIndexedLinked.LinkTriangles_Edges(triangle, rimTriangle);

                    // Store this triangle
                    newTriangles.Add(triangle);
                    hull.Add(triangle);
                }
            }

            // The new triangles are linked to the boundry already.  Now link the other two edges to each other (newTriangles forms a
            // triangle fan, but they aren't neccessarily consecutive)
            TriangleIndexedLinked.LinkTriangles_Edges(newTriangles.ConvertAll(o => (TriangleIndexedLinked)o).ToList(), false);

            // Distribute the outside points to these new triangles
            foreach (TriangleWithPoints triangle in newTriangles)
            {
                // Find the points that are outside the polygon (not the points behind the triangle)
                // Note that a point will never be shared between triangles
                triangle.OutsidePoints.AddRange(GetOutsideSet(triangle, outsidePoints, allPoints, coplanarDistance));
            }
        }
        private static Point3D ProcessTriangleSprtNewSprtInsidePoint(TriangleIndexedLinked rimTriangle, List<TriangleEdge> sharedEdges)
        {
            bool[] used = new bool[3];

            // Figure out which indices are used
            foreach (TriangleEdge edge in sharedEdges)
            {
                switch (edge)
                {
                    case TriangleEdge.Edge_01:
                        used[0] = true;
                        used[1] = true;
                        break;

                    case TriangleEdge.Edge_12:
                        used[1] = true;
                        used[2] = true;
                        break;

                    case TriangleEdge.Edge_20:
                        used[2] = true;
                        used[0] = true;
                        break;

                    default:
                        throw new ApplicationException("Unknown TriangleEdge: " + edge.ToString());
                }
            }

            // Find one that isn't used
            for (int cntr = 0; cntr < 3; cntr++)
            {
                if (!used[cntr])
                {
                    return rimTriangle[cntr];
                }
            }

            // Project a point away from this triangle
            //TODO:  If the hull ends up concave, this is a very likely culprit.  May need to come up with a better way
            //return rimTriangle.GetCenterPoint() + (rimTriangle.Normal * -.001);		// by not using the unit normal, I'm keeping this scaled roughly to the size of the triangle
            return rimTriangle.GetCenterPoint() + (rimTriangle.Normal * -.05);		// by not using the unit normal, I'm keeping this scaled roughly to the size of the triangle
        }

        /// <summary>
        /// This takes in 3 points that belong to the triangle, and a point that is not on the triangle, but is toward the rest
        /// of the hull.  It then creates a triangle whose normal points away from the hull (right hand rule)
        /// </summary>
        private static TriangleWithPoints CreateTriangle(int point0, int point1, int point2, Point3D pointWithinHull, Point3D[] allPoints)
        {
            // Try an arbitrary orientation
            TriangleWithPoints retVal = new TriangleWithPoints(point0, point1, point2, allPoints);

            // Get a vector pointing from point0 to the inside point
            Vector3D towardHull = pointWithinHull - allPoints[point0];

            if (Vector3D.DotProduct(towardHull, retVal.Normal) > 0d)
            {
                // When the dot product is greater than zero, that means the normal points in the same direction as the vector that points
                // toward the hull.  So buid a triangle that points in the opposite direction
                retVal = new TriangleWithPoints(point0, point2, point1, allPoints);
            }

            // Exit Function
            return retVal;
        }

        /// <summary>
        /// This returns the subset of points that are on the outside facing side of the triangle
        /// </summary>
        /// <remarks>
        /// I got these steps here:
        /// http://www.gamedev.net/topic/106765-determining-if-a-point-is-in-front-of-or-behind-a-plane/
        /// </remarks>
        /// <param name="pointIndicies">
        /// This method will only look at the points in pointIndicies.
        /// NOTE: This method will also remove values from here if they are part of an existing triangle, or get added to a triangle's outside list.
        /// </param>
        private static List<int> GetOutsideSet(TriangleIndexed triangle, List<int> pointIndicies, Point3D[] allPoints, double coplanarDistance)
        {
            List<int> retVal = new List<int>();

            // Compute D, using a arbitrary point P, that lies on the plane: D = - (Nx*Px + Ny*Py + Nz*Pz); Don't forget the inversion !
            double D = -((triangle.NormalUnit.X * triangle.Point0.X) + (triangle.NormalUnit.Y * triangle.Point0.Y) + (triangle.NormalUnit.Z * triangle.Point0.Z));

            int cntr = 0;
            while (cntr < pointIndicies.Count)
            {
                int index = pointIndicies[cntr];

                if (index == triangle.Index0 || index == triangle.Index1 || index == triangle.Index2)
                {
                    pointIndicies.Remove(index);		// no need to consider this for future calls
                    continue;
                }

                // You can test a point (T) with respect to the plane using the plane equation: res = Nx*Tx + Ny*Ty + Nz*Tz + D
                double distance = (triangle.NormalUnit.X * allPoints[index].X) + (triangle.NormalUnit.Y * allPoints[index].Y) + (triangle.NormalUnit.Z * allPoints[index].Z) + D;

                // Anything greater than zero lies outside the plane
                // Distances really close to zero are considered coplanar, and have special handling
                if (distance > 0d || Math.Abs(distance) < coplanarDistance)
                {
                    retVal.Add(index);
                    pointIndicies.Remove(index);		// an outside point can only belong to one triangle
                }
                else
                {
                    cntr++;
                }
            }

            return retVal;
        }

        #endregion
    }

    #endregion
    #region Class: QuickHull8

    //TODO: May want to make a custom IsNearZero that is more strict than Math3D

    public static class QuickHull8
    {
        #region Class: TriangleWithPoints

        /// <summary>
        /// This links a triangle with a set of points that sit "outside" the triangle
        /// </summary>
        public class TriangleWithPoints : TriangleIndexedLinked
        {
            public TriangleWithPoints(int index0, int index1, int index2, Point3D[] allPoints)
                : base(index0, index1, index2, allPoints)
            {
                this.OutsidePoints = new List<int>();
            }

            public List<int> OutsidePoints
            {
                get;
                private set;
            }
        }

        #endregion

        public static TriangleIndexed[] GetConvexHull(Point3D[] points, int maxSteps)
        {
            if (points.Length < 4)
            {
                throw new ArgumentException("There must be at least 4 points", "points");
            }

            // Pick 4 points
            int[] startPoints = GetStartingTetrahedron(points);
            List<TriangleWithPoints> retVal = ConvertStartingPointsToTriangles(startPoints, points);



            int stepCount = 0;




            // If any triangle has any points outside the hull, then remove that triangle, and replace it with triangles connected to the
            // farthest out point (relative to the triangle that got removed)
            bool foundOne;
            do
            {
                foundOne = false;
                int index = 0;
                while (index < retVal.Count)
                {


                    if (maxSteps >= 0 && stepCount >= maxSteps)
                    {
                        break;
                    }
                    stepCount++;



                    if (retVal[index].OutsidePoints.Count > 0)
                    {
                        foundOne = true;
                        ProcessTriangle(retVal, index, points);
                    }
                    else
                    {
                        index++;
                    }
                }
            } while (foundOne);

            // Exit Function
            return retVal.ToArray();
        }

        #region Private Methods

        /// <summary>
        /// I've seen various literature call this initial tetrahedron a simplex
        /// </summary>
        private static int[] GetStartingTetrahedron(Point3D[] points)
        {
            int[] retVal = new int[4];

            // Validate the point cloud is 3D, also get the points that have the smallest and largest X (an exception is thrown if they are the same point)
            int minXIndex, maxXIndex;
            GetStartingTetrahedronSprtGetMinMaxX(out minXIndex, out maxXIndex, points);

            // The first two return points will be the ones with the min and max X values
            retVal[0] = minXIndex;
            retVal[1] = maxXIndex;

            // The third point will be the one that is farthest from the line defined by the first two
            int thirdIndex = GetStartingTetrahedronSprtFarthestFromLine(minXIndex, maxXIndex, points);
            retVal[2] = thirdIndex;

            // The fourth point will be the one that is farthest from the plane defined by the first three
            int fourthIndex = GetStartingTetrahedronSprtFarthestFromPlane(minXIndex, maxXIndex, thirdIndex, points);
            retVal[3] = fourthIndex;

            // Exit Function
            return retVal;
        }
        /// <summary>
        /// This does more than just look at X.  It looks at Y and Z as well to verify the point cloud has points in all 3 dimensions
        /// </summary>
        private static void GetStartingTetrahedronSprtGetMinMaxX(out int minXIndex, out int maxXIndex, Point3D[] points)
        {
            // Create arrays to hold the min and max along each axis (0=X, 1=Y, 2=Z) - using the first point
            double[] minValues = new double[] { points[0].X, points[0].Y, points[0].Z };
            double[] maxValues = new double[] { points[0].X, points[0].Y, points[0].Z };
            int[] minIndicies = new int[] { 0, 0, 0 };
            int[] maxIndicies = new int[] { 0, 0, 0 };

            for (int cntr = 1; cntr < points.Length; cntr++)
            {
                #region Examine Point

                // Min
                if (points[cntr].X < minValues[0])
                {
                    minValues[0] = points[cntr].X;
                    minIndicies[0] = cntr;
                }

                if (points[cntr].Y < minValues[1])
                {
                    minValues[1] = points[cntr].Y;
                    minIndicies[1] = cntr;
                }

                if (points[cntr].Z < minValues[2])
                {
                    minValues[2] = points[cntr].Z;
                    minIndicies[2] = cntr;
                }

                // Max
                if (points[cntr].X > maxValues[0])
                {
                    maxValues[0] = points[cntr].X;
                    maxIndicies[0] = cntr;
                }

                if (points[cntr].Y > maxValues[1])
                {
                    maxValues[1] = points[cntr].Y;
                    maxIndicies[1] = cntr;
                }

                if (points[cntr].Z > maxValues[2])
                {
                    maxValues[2] = points[cntr].Z;
                    maxIndicies[2] = cntr;
                }

                #endregion
            }

            #region Validate

            for (int cntr = 0; cntr < 3; cntr++)
            {
                if (maxValues[cntr] == minValues[cntr])
                {
                    throw new ApplicationException("The points passed in aren't in 3D - they are either all on the same point (0D), or colinear (1D), or coplanar (2D)");
                }
            }

            #endregion

            // Return the two points that are the min/max X
            minXIndex = minIndicies[0];
            maxXIndex = maxIndicies[0];
        }
        /// <summary>
        /// Finds the point that is farthest from the line
        /// </summary>
        private static int GetStartingTetrahedronSprtFarthestFromLine(int index1, int index2, Point3D[] points)
        {
            Vector3D lineDirection = points[index2] - points[index1];		// The nearest point method wants a vector, so calculate it once
            Point3D startPoint = points[index1];		// not sure if there is much of a penalty to referencing a list by index, but I figure I'll just cache this point once

            double maxDistance = -1d;
            int retVal = -1;

            for (int cntr = 0; cntr < points.Length; cntr++)
            {
                if (cntr == index1 || cntr == index2)
                {
                    continue;
                }

                // Calculate the distance from the line
                Point3D nearestPoint = Math3D.GetClosestPoint_Line_Point(startPoint, lineDirection, points[cntr]);
                double distanceSquared = (points[cntr] - nearestPoint).LengthSquared;

                if (distanceSquared > maxDistance)
                {
                    maxDistance = distanceSquared;
                    retVal = cntr;
                }
            }

            if (retVal < 0)
            {
                throw new ApplicationException("Didn't find a return point, this should never happen");
            }

            // Exit Function
            return retVal;
        }
        private static int GetStartingTetrahedronSprtFarthestFromPlane(int index1, int index2, int index3, Point3D[] points)
        {
            //NOTE:  I'm copying bits of Math3D.DistanceFromPlane here for optimization reasons
            //Vector3D[] triangle = new Vector3D[] { points[index1].ToVector(), points[index2].ToVector(), points[index3].ToVector() };
            //Vector3D normal = Math3D.Normal(triangle);
            //double originDistance = Math3D.GetPlaneOriginDistance(normal, triangle[0].ToPoint());
            Triangle triangle = new Triangle(points[index1], points[index2], points[index3]);
            Vector3D normal = triangle.NormalUnit;
            double originDistance = Math3D.GetPlaneOriginDistance(normal, triangle.Point0);

            double maxDistance = 0d;
            int retVal = -1;

            for (int cntr = 0; cntr < points.Length; cntr++)
            {
                if (cntr == index1 || cntr == index2 || cntr == index3)
                {
                    continue;
                }

                //NOTE:  This is Math3D.DistanceFromPlane copied inline to speed things up
                Point3D point = points[cntr];
                double distance = ((normal.X * point.X) +					// Ax +
                                                (normal.Y * point.Y) +					// Bx +
                                                (normal.Z * point.Z)) + originDistance;	// Cz + D

                // I don't care which side of the triangle the point is on for this initial tetrahedron
                distance = Math.Abs(distance);

                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    retVal = cntr;
                }
            }

            if (retVal < 0)
            {
                throw new ApplicationException("Didn't find a return point, this should never happen");
            }

            // Exit Function
            return retVal;
        }

        private static List<TriangleWithPoints> ConvertStartingPointsToTriangles(int[] startPoints, Point3D[] allPoints)
        {
            if (startPoints.Length != 4)
            {
                throw new ArgumentException("This method expects exactly 4 points passed in");
            }

            List<TriangleWithPoints> retVal = new List<TriangleWithPoints>();

            // Make triangles
            retVal.Add(CreateTriangle(startPoints[0], startPoints[1], startPoints[2], allPoints[startPoints[3]], allPoints, null));
            retVal.Add(CreateTriangle(startPoints[3], startPoints[1], startPoints[0], allPoints[startPoints[2]], allPoints, null));
            retVal.Add(CreateTriangle(startPoints[3], startPoints[2], startPoints[1], allPoints[startPoints[0]], allPoints, null));
            retVal.Add(CreateTriangle(startPoints[3], startPoints[0], startPoints[2], allPoints[startPoints[1]], allPoints, null));

            // Link triangles together
            TriangleIndexedLinked.LinkTriangles_Edges(retVal.ConvertAll(o => (TriangleIndexedLinked)o).ToList(), true);

            #region Calculate outside points

            // GetOutsideSet wants indicies to the points it needs to worry about.  This initial call needs all points
            List<int> allPointIndicies = Enumerable.Range(0, allPoints.Length).ToList();

            // Remove the indicies that are in the return triangles (I ran into a case where 4 points were passed in, but they were nearly coplanar - enough
            // that GetOutsideSet's Math3D.IsNearZero included it)
            foreach (int index in retVal.SelectMany(o => o.IndexArray).Distinct())
            {
                allPointIndicies.Remove(index);
            }

            // For every triangle, find the points that are outside the polygon (not the points behind the triangle)
            // Note that a point will never be shared between triangles
            foreach (TriangleWithPoints triangle in retVal)
            {
                if (allPointIndicies.Count > 0)
                {
                    triangle.OutsidePoints.AddRange(GetOutsideSet(triangle, allPointIndicies, allPoints));
                }
            }

            #endregion

            // Exit Function
            return retVal;
        }

        /// <summary>
        /// This works on a triangle that contains outside points.  It removes the triangle, and creates new ones connected to
        /// the outermost outside point
        /// </summary>
        private static void ProcessTriangle(List<TriangleWithPoints> hull, int hullIndex, Point3D[] allPoints)
        {
            TriangleWithPoints removedTriangle = hull[hullIndex];

            // Find the farthest point from this triangle
            int fartherstIndex = ProcessTriangleSprtFarthestPoint(removedTriangle);
            if (fartherstIndex < 0)
            {
                // The outside points are on the same plane as this triangle (and sitting within the bounds of the triangle).
                // Just wipe the points and go away
                removedTriangle.OutsidePoints.Clear();
                return;
                //throw new ApplicationException(string.Format("Couldn't find a farthest point for triangle\r\n{0}\r\n\r\n{1}\r\n",
                //    removedTriangle.ToString(),
                //    string.Join("\r\n", removedTriangle.OutsidePoints.Select(o => o.Item1.ToString() + "   |   " + allPoints[o.Item1].ToString(true)).ToArray())));		// this should never happen
            }

            //Key=which triangles to remove from the hull
            //Value=meaningless, I just wanted a sorted list
            SortedList<TriangleIndexedLinked, int> removedTriangles = new SortedList<TriangleIndexedLinked, int>();

            //Key=triangle on the hull that is a boundry (the new triangles will be added to these boundry triangles)
            //Value=the key's edges that are exposed to the removed triangles
            SortedList<TriangleIndexedLinked, List<TriangleEdge>> removedRim = new SortedList<TriangleIndexedLinked, List<TriangleEdge>>();

            // Find all the triangles that can see this point (they will need to be removed from the hull)
            //NOTE:  This method is recursive
            ProcessTriangleSprtRemove(removedTriangle, removedTriangles, removedRim, allPoints[fartherstIndex].ToVector());

            // Remove these from the hull
            ProcessTriangleSprtRemoveFromHull(hull, removedTriangles.Keys, removedRim);

            // Get all the outside points
            //List<int> allOutsidePoints = removedTriangles.Keys.SelectMany(o => ((TriangleWithPoints)o).OutsidePoints).Distinct().ToList();		// if you try to read what SelectMany does, it gets pretty wordy and unclear.  It just does a foreach across all the OutsidePoints lists from all the removedTriangles (treats the list of lists like a single list)
            List<int> allOutsidePoints = removedTriangles.Keys.SelectMany(o => ((TriangleWithPoints)o).OutsidePoints).ToList();		// there's no need to call distinct, since the outside points aren't shared between triangles

            // Create new triangles
            ProcessTriangleSprtNew(hull, fartherstIndex, removedRim, allPoints, allOutsidePoints);		// Note that after this method, allOutsidePoints will only be the points left over (the ones that are inside the hull)
        }
        private static int ProcessTriangleSprtFarthestPoint(TriangleWithPoints triangle)
        {
            //NOTE: This method is nearly a copy of GetOutsideSet

            Vector3D[] polygon = new Vector3D[] { triangle.Point0.ToVector(), triangle.Point1.ToVector(), triangle.Point2.ToVector() };

            List<int> pointIndicies = triangle.OutsidePoints;
            Point3D[] allPoints = triangle.AllPoints;

            double maxDistance = 0d;
            int retVal = -1;

            for (int cntr = 0; cntr < pointIndicies.Count; cntr++)
            {
                double distance = Math3D.DistanceFromPlane(polygon, allPoints[pointIndicies[cntr]].ToVector());

                // Distance should never be negative (or the point wouldn't be in the list of outside points).  If for some reason there is one with a negative distance,
                // it shouldn't be considered, because it sits inside the hull
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    retVal = pointIndicies[cntr];
                }
                else if (Math1D.IsNearZero(distance) && Math1D.IsNearZero(maxDistance))		// this is for a coplanar point that can have a very slightly negative distance
                {
                    // Can't trust the previous bary check, need another one (maybe it's false because it never went through that first check?)
                    Vector bary = Math3D.ToBarycentric(triangle, allPoints[pointIndicies[cntr]]);
                    if (bary.X < 0d || bary.Y < 0d || bary.X + bary.Y > 1d)
                    {
                        maxDistance = 0d;
                        retVal = pointIndicies[cntr];
                    }
                }
            }

            // Exit Function
            return retVal;
        }

        private static void ProcessTriangleSprtRemove(TriangleIndexedLinked triangle, SortedList<TriangleIndexedLinked, int> removedTriangles, SortedList<TriangleIndexedLinked, List<TriangleEdge>> removedRim, Vector3D farPoint)
        {
            // This triangle will need to be removed.  Keep track of it so it's not processed again (the int means nothing)
            removedTriangles.Add(triangle, 0);

            // Try each neighbor
            ProcessTriangleSprtRemoveSprtNeighbor(triangle.Neighbor_01, removedTriangles, removedRim, farPoint, triangle);
            ProcessTriangleSprtRemoveSprtNeighbor(triangle.Neighbor_12, removedTriangles, removedRim, farPoint, triangle);
            ProcessTriangleSprtRemoveSprtNeighbor(triangle.Neighbor_20, removedTriangles, removedRim, farPoint, triangle);
        }
        private static void ProcessTriangleSprtRemoveSprtNeighbor(TriangleIndexedLinked triangle, SortedList<TriangleIndexedLinked, int> removedTriangles, SortedList<TriangleIndexedLinked, List<TriangleEdge>> removedRim, Vector3D farPoint, TriangleIndexedLinked fromTriangle)
        {
            if (removedTriangles.ContainsKey(triangle))
            {
                return;
            }

            if (removedRim.ContainsKey(triangle))
            {
                // This triangle is already recognized as part of the hull.  Add a link from it to the from triangle (because two of its edges
                // are part of the hull rim)
                removedRim[triangle].Add(triangle.WhichEdge(fromTriangle));
                return;
            }

            // Need to subtract the far point from some point on this triangle, so that it's a vector from the triangle to the
            // far point, and not from the origin
            double dot = Vector3D.DotProduct(triangle.Normal, (farPoint - triangle.Point0).ToVector());
            if (dot >= 0d || Math1D.IsNearZero(dot))		// 0 is coplanar, -1 is the opposite side
            {
                // This triangle is visible to the point.  Remove it (recurse)
                ProcessTriangleSprtRemove(triangle, removedTriangles, removedRim, farPoint);
            }
            else
            {
                // This triangle is invisible to the point, so needs to stay part of the hull.  Store this boundry
                removedRim.Add(triangle, new List<TriangleEdge>());
                removedRim[triangle].Add(triangle.WhichEdge(fromTriangle));
            }
        }

        private static void ProcessTriangleSprtRemoveFromHull(List<TriangleWithPoints> hull, IList<TriangleIndexedLinked> trianglesToRemove, SortedList<TriangleIndexedLinked, List<TriangleEdge>> removedRim)
        {
            // Remove from the hull list
            foreach (TriangleIndexedLinked triangle in trianglesToRemove)
            {
                hull.Remove((TriangleWithPoints)triangle);
            }

            // Break the links from the hull to the removed triangles (there's no need to break links in the other direction.  The removed triangles
            // will become orphaned, and eventually garbage collected)
            foreach (TriangleIndexedLinked triangle in removedRim.Keys)
            {
                foreach (TriangleEdge edge in removedRim[triangle])
                {
                    triangle.SetNeighbor(edge, null);
                }
            }
        }

        private static void ProcessTriangleSprtNew(List<TriangleWithPoints> hull, int fartherstIndex, SortedList<TriangleIndexedLinked, List<TriangleEdge>> removedRim, Point3D[] allPoints, List<int> outsidePoints)
        {
            List<TriangleWithPoints> newTriangles = new List<TriangleWithPoints>();

            // Run around the rim, and build a triangle between the far point and each edge
            foreach (TriangleIndexedLinked rimTriangle in removedRim.Keys)
            {
                // Get a point that is toward the hull (the created triangle will be built so its normal points away from this)
                Point3D insidePoint = ProcessTriangleSprtNewSprtInsidePoint(rimTriangle, removedRim[rimTriangle]);

                foreach (TriangleEdge rimEdge in removedRim[rimTriangle])
                {
                    // Get the points for this edge
                    int index1, index2;
                    rimTriangle.GetIndices(out index1, out index2, rimEdge);

                    // Build the triangle
                    TriangleWithPoints triangle = CreateTriangle(fartherstIndex, index1, index2, insidePoint, allPoints, rimTriangle);

                    // Now link this triangle with the boundry triangle (just the one edge, the other edges will be joined later)
                    TriangleIndexedLinked.LinkTriangles_Edges(triangle, rimTriangle);

                    // Store this triangle
                    newTriangles.Add(triangle);
                    hull.Add(triangle);
                }
            }

            // The new triangles are linked to the boundry already.  Now link the other two edges to each other (newTriangles forms a
            // triangle fan, but they aren't neccessarily consecutive)
            TriangleIndexedLinked.LinkTriangles_Edges(newTriangles.ConvertAll(o => (TriangleIndexedLinked)o).ToList(), false);

            // Distribute the outside points to these new triangles
            foreach (TriangleWithPoints triangle in newTriangles)
            {
                // Find the points that are outside the polygon (not the points behind the triangle)
                // Note that a point will never be shared between triangles
                triangle.OutsidePoints.AddRange(GetOutsideSet(triangle, outsidePoints, allPoints));
            }
        }
        private static Point3D ProcessTriangleSprtNewSprtInsidePoint(TriangleIndexedLinked rimTriangle, List<TriangleEdge> sharedEdges)
        {
            bool[] used = new bool[3];

            // Figure out which indices are used
            foreach (TriangleEdge edge in sharedEdges)
            {
                switch (edge)
                {
                    case TriangleEdge.Edge_01:
                        used[0] = true;
                        used[1] = true;
                        break;

                    case TriangleEdge.Edge_12:
                        used[1] = true;
                        used[2] = true;
                        break;

                    case TriangleEdge.Edge_20:
                        used[2] = true;
                        used[0] = true;
                        break;

                    default:
                        throw new ApplicationException("Unknown TriangleEdge: " + edge.ToString());
                }
            }

            // Find one that isn't used
            for (int cntr = 0; cntr < 3; cntr++)
            {
                if (!used[cntr])
                {
                    return rimTriangle[cntr];
                }
            }

            // Project a point away from this triangle
            //TODO:  If the hull ends up concave, this is a very likely culprit.  May need to come up with a better way
            return rimTriangle.GetCenterPoint() + (rimTriangle.Normal * -.001);		// by not using the unit normal, I'm keeping this scaled roughly to the size of the triangle
        }

        /// <summary>
        /// This takes in 3 points that belong to the triangle, and a point that is not on the triangle, but is toward the rest
        /// of the hull.  It then creates a triangle whose normal points away from the hull (right hand rule)
        /// </summary>
        private static TriangleWithPoints CreateTriangle(int point0, int point1, int point2, Point3D pointWithinHull, Point3D[] allPoints, ITriangle neighbor)
        {
            // Try an arbitrary orientation
            TriangleWithPoints retVal = new TriangleWithPoints(point0, point1, point2, allPoints);

            // Get a vector pointing from point0 to the inside point
            Vector3D towardHull = pointWithinHull - allPoints[point0];

            double dot = Vector3D.DotProduct(towardHull, retVal.Normal);
            if (dot > 0d)
            {
                // When the dot product is greater than zero, that means the normal points in the same direction as the vector that points
                // toward the hull.  So buid a triangle that points in the opposite direction
                retVal = new TriangleWithPoints(point0, point2, point1, allPoints);
            }
            else if (dot == 0d)
            {
                // This new triangle is coplanar with the neighbor triangle, so pointWithinHull can't be used to figure out if this return
                // triangle is facing the correct way.  Instead, make it point the same direction as the neighbor triangle
                dot = Vector3D.DotProduct(retVal.Normal, neighbor.Normal);
                if (dot < 0)
                {
                    retVal = new TriangleWithPoints(point0, point2, point1, allPoints);
                }
            }

            // Exit Function
            return retVal;
        }

        /// <summary>
        /// This returns the subset of points that are on the outside facing side of the triangle
        /// </summary>
        /// <remarks>
        /// I got these steps here:
        /// http://www.gamedev.net/topic/106765-determining-if-a-point-is-in-front-of-or-behind-a-plane/
        /// </remarks>
        /// <param name="pointIndicies">
        /// This method will only look at the points in pointIndicies.
        /// NOTE: This method will also remove values from here if they are part of an existing triangle, or get added to a triangle's outside list.
        /// </param>
        private static List<int> GetOutsideSet(TriangleIndexed triangle, List<int> pointIndicies, Point3D[] allPoints)
        {
            List<int> retVal = new List<int>();

            // Compute D, using a arbitrary point P, that lies on the plane: D = - (Nx*Px + Ny*Py + Nz*Pz); Don't forget the inversion !
            double D = -((triangle.NormalUnit.X * triangle.Point0.X) + (triangle.NormalUnit.Y * triangle.Point0.Y) + (triangle.NormalUnit.Z * triangle.Point0.Z));

            int cntr = 0;
            while (cntr < pointIndicies.Count)
            {
                int index = pointIndicies[cntr];

                if (index == triangle.Index0 || index == triangle.Index1 || index == triangle.Index2)
                {
                    pointIndicies.Remove(index);		// no need to consider this for future calls
                    continue;
                }

                // You can test a point (T) with respect to the plane using the plane equation: res = Nx*Tx + Ny*Ty + Nz*Tz + D
                double res = (triangle.NormalUnit.X * allPoints[index].X) + (triangle.NormalUnit.Y * allPoints[index].Y) + (triangle.NormalUnit.Z * allPoints[index].Z) + D;

                if (res > 0d)		// anything greater than zero lies outside the plane
                {
                    retVal.Add(index);
                    pointIndicies.Remove(index);		// an outside point can only belong to one triangle
                }
                else if (Math1D.IsNearZero(res))
                {
                    // This point is coplanar.  Only consider it an outside point if it is outside the bounds of this triangle
                    Vector bary = Math3D.ToBarycentric(triangle, allPoints[index]);
                    if (bary.X < 0d || bary.Y < 0d || bary.X + bary.Y > 1d)
                    {
                        retVal.Add(index);
                        pointIndicies.Remove(index);		// an outside point can only belong to one triangle
                    }
                    else
                    {
                        cntr++;
                    }
                }
                else
                {
                    cntr++;
                }
            }

            return retVal;
        }

        #endregion
    }

    #endregion

    #endregion
}
