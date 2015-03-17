using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

using Game.HelperClassesCore;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.ShipEditor;
using Game.Newt.v2.GameItems.ShipParts;
using Game.Newt.v2.NewtonDynamics;
using Game.HelperClassesWPF.Primitives3D;
using Game.HelperClassesWPF;
using System.Xaml;

//TODO: UtilityWPF.GetConvexHull fails with coplanar points
//TODO: The attempt to come up with a perfect thrust firing solution is a bit flawed.  It's probably possible, but incredibly expensive and difficult.  Instead, lookup "control theory"

namespace Game.Newt.Testers
{
    public partial class ShipPartTesterWindow : Window
    {
        #region Class: TempBody

        /// <summary>
        /// This is a body that shouldn't stay around forever
        /// </summary>
        private class TempBody
        {
            #region Constructor

            public TempBody(Body body, TimeSpan maxAge)
            {
                this.IsTimeLimited = true;

                this.PhysicsBody = body;

                this.CreateTime = DateTime.Now;
                this.MaxAge = maxAge;

                this.MaxDistance = 0d;
                this.MaxDistanceSquared = 0d;
            }
            public TempBody(Body body, double maxDistance)
            {
                this.IsTimeLimited = true;

                this.PhysicsBody = body;

                this.MaxDistance = maxDistance;
                this.MaxDistanceSquared = maxDistance * maxDistance;

                this.CreateTime = DateTime.MinValue;
                this.MaxAge = new TimeSpan();
            }

            #endregion

            #region Public Properties

            public readonly Body PhysicsBody;

            public readonly bool IsTimeLimited;

            // These have meaning if IsTimeLimited is true
            public readonly DateTime CreateTime;
            public readonly TimeSpan MaxAge;

            // This has meaning if IsTimeLimited is false
            public readonly double MaxDistance;
            public readonly double MaxDistanceSquared;

            #endregion

            #region Public Methods

            public bool ShouldDie()
            {
                if (this.IsTimeLimited)
                {
                    return DateTime.Now - this.CreateTime > MaxAge;
                }
                else
                {
                    return this.PhysicsBody.Position.ToVector().LengthSquared > this.MaxDistanceSquared;
                }
            }

            #endregion
        }

        #endregion
        #region Class: ItemColors

        private class ItemColors
        {
            //Color1
            //ctrllight: E5D5A4
            //ctrl: BFB289
            //ctrldark: 6B644C
            //brown: 473C33
            //red: 941300

            //Color2
            //ctrllight: F2EFDC
            //ctrl: D9D2B0
            //ctrldark: BFB28A
            //olive: 595139
            //burntorange: 732C03

            public Color BoundryLines = Colors.Silver;

            public Color MassBall = Colors.Orange;
            public Color MassBallReflect = Colors.Yellow;
            public double MassBallReflectIntensity = 50d;
        }

        #endregion
        #region Enum: BalanceTestType

        private enum BalanceTestType
        {
            Individual,
            ZeroTorque1,
            ZeroTorque2,
            ZeroTorque3
        }

        #endregion
        #region Class: BalanceVisualizer

        private class BalanceVisualizer : IDisposable
        {
            #region Enum: IsInsideResult

            private enum IsInsideResult
            {
                Neither,
                Inside,
                Intersects
            }

            #endregion
            #region Class: HullLineResult

            private class HullLineResult
            {
                public HullLineResult(TriangleIndexed[] hull, Vector3D intersectTest, IsInsideResult isInside, ITriangle intersectingTriangle, Point3D intersectingPoint)
                {
                    this.Hull = hull;
                    this.IntersectTest = intersectTest;
                    this.IsInside = isInside;
                    this.IntersectingTriangle = intersectingTriangle;
                    this.IntersectingPoint = intersectingPoint;
                }

                public readonly TriangleIndexed[] Hull;
                public readonly Vector3D IntersectTest;
                public readonly IsInsideResult IsInside;

                // These only have meaning if IsInside is Intersects
                public readonly ITriangle IntersectingTriangle;
                public readonly Point3D IntersectingPoint;
            }

            #endregion

            #region Declaration Section

            private const string LINE_GREEN = "50B13B";
            private const string LINE_GREEN_ANTI = "97D889";
            private const string LINE_RED = "D14343";
            private const string LINE_RED_ANTI = "E38181";
            private const string LINE_CYAN = "148561";
            private const string LINE_CYAN_ANTI = "69B89F";
            private const string LINE_BLUE = "6E87C7";		// there is no anti for blue, it's a secondary type of line

            private const string HULL_COLOR = "209F9B50";
            private const string HULL_SPECULAR = "A0EAE375";
            private const double HULL_SPECULAR_INTENSITY = 33d;

            private readonly Viewport3D _viewport;
            private readonly List<Visual3D> _visuals;

            private readonly Vector3D[] _vectors;

            private readonly bool _is3D;

            #endregion

            #region Constructor

            public BalanceVisualizer(Viewport3D viewport, int numVectors, bool is3D)
            {
                _viewport = viewport;
                _visuals = new List<Visual3D>();
                _is3D = is3D;

                _vectors = GenerateVectors(numVectors, is3D);

                Redraw();
            }

            #endregion

            #region IDisposable Members

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    if (_viewport != null)
                    {
                        foreach (Visual3D visual in _visuals)
                        {
                            _viewport.Children.Remove(visual);
                        }
                    }
                }
            }

            #endregion

            #region Public Properties

            private bool _showAxiis = true;
            public bool ShowAxiis
            {
                get
                {
                    return _showAxiis;
                }
                set
                {
                    _showAxiis = value;

                    Redraw();
                }
            }

            private BalanceTestType _testType = BalanceTestType.Individual;
            public BalanceTestType TestType
            {
                get
                {
                    return _testType;
                }
                set
                {
                    _testType = value;

                    Redraw();
                }
            }

            // These are used for the individual test
            private bool _showPossibilityLines = false;
            public bool ShowPossibilityLines
            {
                get
                {
                    return _showPossibilityLines;
                }
                set
                {
                    _showPossibilityLines = value;

                    Redraw();
                }
            }

            private bool _showPossibilityHull = false;
            public bool ShowPossibilityHull
            {
                get
                {
                    return _showPossibilityHull;
                }
                set
                {
                    _showPossibilityHull = value;

                    Redraw();
                }
            }

            private int _selectedIndex = -1;
            public int SelectedIndex
            {
                get
                {
                    return _selectedIndex;
                }
                set
                {
                    _selectedIndex = value;

                    Redraw();
                }
            }

            #endregion

            #region Private Methods

            private void Redraw()
            {
                foreach (Visual3D visual in _visuals)
                {
                    _viewport.Children.Remove(visual);
                }

                switch (_testType)
                {
                    case BalanceTestType.Individual:
                        DrawIndividual();
                        break;

                    case BalanceTestType.ZeroTorque1:
                        DrawZeroTorqueTest();
                        break;

                    case BalanceTestType.ZeroTorque2:
                        DrawZeroTorqueTest2();
                        break;

                    default:
                        throw new ApplicationException("Unknown BalanceTestType: " + _testType.ToString());
                }
            }

            private void DrawAxiis(Vector3D offset)
            {
                ScreenSpaceLines3D lines = new ScreenSpaceLines3D();
                lines.Thickness = 1d;
                lines.Color = UtilityWPF.ColorFromHex("C8C8C8");

                lines.AddLine(new Point3D(offset.X - 5, offset.Y, offset.Z), new Point3D(offset.X + 5, offset.Y, offset.Z));
                lines.AddLine(new Point3D(offset.X, offset.Y - 5, offset.Z), new Point3D(offset.X, offset.Y + 5, offset.Z));
                lines.AddLine(new Point3D(offset.X, offset.Y, offset.Z - 5), new Point3D(offset.X, offset.Y, offset.Z + 5));

                _visuals.Add(lines);
                _viewport.Children.Add(lines);
            }

            // These create a convex hull that represents all possible combinations of the vectors passed in (not including the ones to skip)
            private static TriangleIndexed[] GetHull(Vector3D[] vectors, int skipIndex)
            {
                return GetHull(vectors, new int[] { skipIndex });
            }
            private static TriangleIndexed[] GetHull(Vector3D[] vectors, int[] skipIndices)
            {
                Point3D[] remainExtremes = GetRemainingExtremes(vectors, skipIndices);

                // Build a convex hull out of them
                //NOTE: GetConvexHull shouldn't throw any more exceptions, I changed all the cases to return null
                TriangleIndexed[] retVal = null;
                try
                {
                    retVal = Math3D.GetConvexHull(remainExtremes.ToArray());
                }
                catch (Exception)
                {
                    retVal = null;
                }

                // Exit Function
                return retVal;
            }

            private static Point3D[] GetRemainingExtremes(Vector3D[] vectors, int[] skipIndices)
            {
                // Put the vectors to be worked with in a single list to make them easier to work with
                List<Vector3D> others = new List<Vector3D>();
                for (int cntr = 0; cntr < vectors.Length; cntr++)
                {
                    if (!skipIndices.Contains(cntr))
                    {
                        others.Add(vectors[cntr]);
                    }
                }

                // Add up all combos
                List<Point3D> remainExtremes = new List<Point3D>();

                remainExtremes.Add(new Point3D(0, 0, 0));

                foreach (int[] combo in UtilityCore.AllCombosEnumerator(others.Count))
                {
                    // Add up the vectors that this combo points to
                    Vector3D extremity = others[combo[0]];
                    for (int cntr = 1; cntr < combo.Length; cntr++)
                    {
                        extremity += others[combo[cntr]];
                    }

                    Point3D point = extremity.ToPoint();
                    if (!remainExtremes.Contains(point))
                    {
                        remainExtremes.Add(point);
                    }
                }

                // Exit Function
                return remainExtremes.ToArray();
            }

            private static Vector3D[] GenerateVectors(int numVectors, bool is3D)
            {
                const double MAXRADIUS = 5d;

                Vector3D[] retVal = new Vector3D[numVectors];

                for (int cntr = 0; cntr < numVectors; cntr++)
                {
                    if (is3D)
                    {
                        retVal[cntr] = Math3D.GetRandomVector_Spherical(MAXRADIUS);
                    }
                    else
                    {
                        retVal[cntr] = Math3D.GetRandomVector_Circular(MAXRADIUS);
                    }
                }

                return retVal;
            }

            private static bool IsNearZeroTorque(Vector3D testVect)
            {
                const double NEARZERO = .05d;

                return Math.Abs(testVect.X) <= NEARZERO && Math.Abs(testVect.Y) <= NEARZERO && Math.Abs(testVect.Z) <= NEARZERO;
            }
            private static bool IsNearValueTorquesDot(double testValue, double compareTo)
            {
                const double NEARZERO = .0005d;

                return testValue >= compareTo - NEARZERO && testValue <= compareTo + NEARZERO;
            }

            #endregion
            #region Private Methods - Individual

            private void DrawIndividual()
            {
                if (_showAxiis)
                {
                    DrawAxiis(new Vector3D(0, 0, 0));
                }

                DrawVectors();

                if (_selectedIndex >= 0 && (_showPossibilityLines || _showPossibilityHull))
                {
                    DrawAntiSelected();
                }

                if (_selectedIndex >= 0 && _showPossibilityLines)
                {
                    DrawPossibleLines();
                }

                // The hull needs to be done last (transparency)
                if (_selectedIndex >= 0 && _showPossibilityHull)
                {
                    DrawPossibleHull();
                }
            }

            /// <summary>
            /// This draws the basic vectors
            /// </summary>
            private void DrawVectors()
            {
                ScreenSpaceLines3D standard = new ScreenSpaceLines3D();
                standard.Thickness = 2d;
                standard.Color = UtilityWPF.ColorFromHex("13285D");

                ScreenSpaceLines3D selected = new ScreenSpaceLines3D();
                selected.Thickness = 3d;
                selected.Color = UtilityWPF.ColorFromHex(LINE_GREEN);

                for (int cntr = 0; cntr < _vectors.Length; cntr++)
                {
                    if (cntr == _selectedIndex)
                    {
                        selected.AddLine(new Point3D(0, 0, 0), _vectors[cntr].ToPoint());
                    }
                    else
                    {
                        standard.AddLine(new Point3D(0, 0, 0), _vectors[cntr].ToPoint());
                    }
                }

                _visuals.Add(standard);
                _viewport.Children.Add(standard);

                if (_selectedIndex >= 0)
                {
                    _visuals.Add(selected);
                    _viewport.Children.Add(selected);
                }
            }
            private void DrawVectors(double thickness, Color color)
            {
                ScreenSpaceLines3D lines = new ScreenSpaceLines3D();
                lines.Thickness = thickness;
                lines.Color = color;

                for (int cntr = 0; cntr < _vectors.Length; cntr++)
                {
                    lines.AddLine(new Point3D(0, 0, 0), _vectors[cntr].ToPoint());
                }

                _visuals.Add(lines);
                _viewport.Children.Add(lines);
            }

            private void DrawAntiSelected()
            {
                ScreenSpaceLines3D antiSelected = new ScreenSpaceLines3D();
                antiSelected.Thickness = 2d;
                antiSelected.Color = UtilityWPF.ColorFromHex(LINE_GREEN_ANTI);

                antiSelected.AddLine(new Point3D(0, 0, 0), (_vectors[_selectedIndex] * -1d).ToPoint());

                _visuals.Add(antiSelected);
                _viewport.Children.Add(antiSelected);
            }

            /// <summary>
            /// This draws all possible additions of the lines that aren't selected
            /// </summary>
            private void DrawPossibleLines()
            {
                // Put the vectors to be worked with in a single list to make them easier to work with
                List<Vector3D> others = new List<Vector3D>();
                for (int cntr = 0; cntr < _vectors.Length; cntr++)
                {
                    if (cntr != _selectedIndex)
                    {
                        others.Add(_vectors[cntr]);
                    }
                }

                // Get the lines
                ScreenSpaceLines3D lines = new ScreenSpaceLines3D();
                lines.Thickness = 1d;
                lines.Color = UtilityWPF.ColorFromHex(LINE_BLUE);

                foreach (int[] combo in UtilityCore.AllCombosEnumerator(others.Count))
                {
                    // Add up the vectors that this combo points to
                    Vector3D extremity = others[combo[0]];
                    for (int cntr = 1; cntr < combo.Length; cntr++)
                    {
                        lines.AddLine(extremity.ToPoint(), (extremity + others[combo[cntr]]).ToPoint());
                        extremity += others[combo[cntr]];
                    }
                }

                // Commit
                _visuals.Add(lines);
                _viewport.Children.Add(lines);
            }

            private void DrawPossibleHull()
            {
                TriangleIndexed[] hull = GetHull(_vectors, _selectedIndex);

                if (hull != null)
                {
                    DrawPossibleHull(hull, new Vector3D(0, 0, 0));
                }
            }
            private void DrawPossibleHull(TriangleIndexed[] hull, Vector3D offset)
            {
                #region Lines

                ScreenSpaceLines3D lines = new ScreenSpaceLines3D();
                lines.Thickness = 1d;
                lines.Color = UtilityWPF.ColorFromHex("40373403");

                foreach (var triangle in hull)
                {
                    lines.AddLine(triangle.Point0 + offset, triangle.Point1 + offset);
                    lines.AddLine(triangle.Point0 + offset, triangle.Point2 + offset);
                    lines.AddLine(triangle.Point1 + offset, triangle.Point2 + offset);
                }

                _visuals.Add(lines);
                _viewport.Children.Add(lines);

                #endregion

                #region Hull

                // Material
                MaterialGroup materials = new MaterialGroup();
                materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex(HULL_COLOR))));
                materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex(HULL_SPECULAR)), HULL_SPECULAR_INTENSITY));

                // Geometry Model
                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = materials;
                geometry.BackMaterial = materials;
                geometry.Geometry = UtilityWPF.GetMeshFromTriangles_IndependentFaces(hull);

                ModelVisual3D model = new ModelVisual3D();
                model.Content = geometry;
                model.Transform = new TranslateTransform3D(offset);

                _visuals.Add(model);
                _viewport.Children.Add(model);

                #endregion
            }

            #endregion
            #region Private Methods - Zero Torque 1

            private void DrawZeroTorqueTest()
            {
                var hulls = DrawZeroTorqueTestSprtGetHulls();

                //bool isPossible = hulls.All(o => o.IsInside != IsInsideResult.Neither);

                if (_showAxiis)
                {
                    DrawAxiis(new Vector3D(0, 0, 0));
                }

                SortedList<IsInsideResult, Tuple<ScreenSpaceLines3D, ScreenSpaceLines3D>> lines = new SortedList<IsInsideResult, Tuple<ScreenSpaceLines3D, ScreenSpaceLines3D>>();
                List<Visual3D> intersectPlanes = new List<Visual3D>();

                for (int cntr = 0; cntr < _vectors.Length; cntr++)
                {
                    IsInsideResult isInside = hulls[cntr].IsInside;

                    if (!lines.ContainsKey(isInside))
                    {
                        #region Add line category

                        lines.Add(isInside, new Tuple<ScreenSpaceLines3D, ScreenSpaceLines3D>(new ScreenSpaceLines3D(), new ScreenSpaceLines3D()));

                        lines[isInside].Item1.Thickness = 3d;
                        lines[isInside].Item2.Thickness = 2d;

                        switch (hulls[cntr].IsInside)
                        {
                            case IsInsideResult.Inside:
                                lines[isInside].Item1.Color = UtilityWPF.ColorFromHex(LINE_GREEN);
                                lines[isInside].Item2.Color = UtilityWPF.ColorFromHex(LINE_GREEN_ANTI);
                                break;

                            case IsInsideResult.Intersects:
                                lines[isInside].Item1.Color = UtilityWPF.ColorFromHex(LINE_CYAN);
                                lines[isInside].Item2.Color = UtilityWPF.ColorFromHex(LINE_CYAN_ANTI);
                                break;

                            case IsInsideResult.Neither:
                                lines[isInside].Item1.Color = UtilityWPF.ColorFromHex(LINE_RED);
                                lines[isInside].Item2.Color = UtilityWPF.ColorFromHex(LINE_RED_ANTI);
                                break;

                            default:
                                throw new ApplicationException("Unknown IsInsideResult: " + hulls[cntr].IsInside.ToString());
                        }

                        #endregion
                    }

                    lines[isInside].Item1.AddLine(new Point3D(0, 0, 0), _vectors[cntr].ToPoint());
                    lines[isInside].Item2.AddLine(new Point3D(0, 0, 0), (_vectors[cntr] * -1d).ToPoint());

                    if (hulls[cntr].IsInside == IsInsideResult.Intersects)
                    {
                        #region Add intersection point

                        // Material
                        MaterialGroup materials = new MaterialGroup();
                        materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex(LINE_CYAN))));
                        materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex(LINE_CYAN_ANTI)), 50d));

                        // Geometry Model
                        GeometryModel3D geometry = new GeometryModel3D();
                        geometry.Material = materials;
                        geometry.BackMaterial = materials;
                        geometry.Geometry = UtilityWPF.GetSphere_LatLon(5, .05d);

                        // Model Visual
                        ModelVisual3D model = new ModelVisual3D();
                        model.Content = geometry;
                        model.Transform = new TranslateTransform3D(hulls[cntr].IntersectingPoint.ToVector());

                        _visuals.Add(model);
                        _viewport.Children.Add(model);

                        #endregion
                        #region Add intersection triangle

                        // Material
                        materials = new MaterialGroup();
                        materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex(HULL_COLOR))));
                        materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex(HULL_SPECULAR)), HULL_SPECULAR_INTENSITY));

                        // Geometry Model
                        geometry = new GeometryModel3D();
                        geometry.Material = materials;
                        geometry.BackMaterial = materials;
                        geometry.Geometry = UtilityWPF.GetMeshFromTriangles_IndependentFaces(new ITriangle[] { hulls[cntr].IntersectingTriangle });

                        model = new ModelVisual3D();
                        model.Content = geometry;

                        intersectPlanes.Add(model);

                        #endregion
                    }
                }

                foreach (var line in lines.Values)
                {
                    _visuals.Add(line.Item1);
                    _visuals.Add(line.Item2);
                    _viewport.Children.Add(line.Item1);
                    _viewport.Children.Add(line.Item2);
                }

                // These need to be done last because of transparency
                foreach (Visual3D model in intersectPlanes)
                {
                    // Not drawing them because they are just distracting
                    //_visuals.Add(model);
                    //_viewport.Children.Add(model);
                }
            }
            private HullLineResult[] DrawZeroTorqueTestSprtGetHulls()
            {
                HullLineResult[] retVal = new HullLineResult[_vectors.Length];
                for (int cntr = 0; cntr < _vectors.Length; cntr++)
                {
                    var hull = GetHull(_vectors, cntr);
                    Vector3D opposite = _vectors[cntr] * -1d;
                    Tuple<IsInsideResult, ITriangle, Point3D> isInside = null;
                    if (hull != null)
                    {
                        isInside = DrawZeroTorqueTestSprtIsInside(hull, opposite);
                    }

                    if (isInside == null)
                    {
                        retVal[cntr] = new HullLineResult(hull, opposite, IsInsideResult.Neither, null, new Point3D());
                    }
                    else
                    {
                        retVal[cntr] = new HullLineResult(hull, opposite, isInside.Item1, isInside.Item2, isInside.Item3);
                    }
                }

                return retVal;
            }
            private Tuple<IsInsideResult, ITriangle, Point3D> DrawZeroTorqueTestSprtIsInside(TriangleIndexed[] hull, Vector3D opposite)
            {
                if (Math3D.IsInside_Planes(hull, opposite.ToPoint()))
                {
                    return new Tuple<IsInsideResult, ITriangle, Point3D>(IsInsideResult.Inside, null, new Point3D());
                }

                Vector3D[] line = new Vector3D[] { opposite * .001d, opposite * .999d };

                foreach (ITriangle triangle in hull)
                {
                    Vector3D dummy1;
                    double dummy2;
                    Vector3D? intersectionPoint;
                    if (Math3D.IsIntersecting_Polygon2D_Line(new Vector3D[] { triangle.Point0.ToVector(), triangle.Point1.ToVector(), triangle.Point2.ToVector() }, line, 3, out dummy1, out dummy2, out intersectionPoint))
                    {
                        return new Tuple<IsInsideResult, ITriangle, Point3D>(IsInsideResult.Intersects, triangle, intersectionPoint.Value.ToPoint());
                    }
                }

                return new Tuple<IsInsideResult, ITriangle, Point3D>(IsInsideResult.Neither, null, new Point3D());
            }

            #endregion
            #region Private Methods - Zero Torque 2

            private void DrawZeroTorqueTest2()
            {
                //const double MINPERCENT = .005d;

                // Find 100% thrusters
                SortedList<int, List<int[]>> fullThrusts = DrawZeroTorqueTest2SprtGetFullThrusts(_vectors);

                if (fullThrusts.Count == 0)
                {
                    #region Draw them red

                    if (_showAxiis)
                    {
                        DrawAxiis(new Vector3D(0, 0, 0));
                    }

                    ScreenSpaceLines3D lines = new ScreenSpaceLines3D();
                    lines.Thickness = 3d;
                    lines.Color = UtilityWPF.ColorFromHex(LINE_RED);

                    for (int cntr = 0; cntr < _vectors.Length; cntr++)
                    {
                        lines.AddLine(new Point3D(0, 0, 0), _vectors[cntr].ToPoint());
                    }

                    _visuals.Add(lines);
                    _viewport.Children.Add(lines);

                    #endregion
                }
                else
                {
                    #region Draw all

                    double maxVectorLength = _vectors.Max(o => o.Length);
                    maxVectorLength *= 2.5d;

                    //double offsetX = (fullThrusts.Keys.Count * maxVectorLength) / -2d;
                    double offsetX = 0d;

                    ScreenSpaceLines3D linesFull = new ScreenSpaceLines3D();
                    linesFull.Thickness = 3d;
                    linesFull.Color = UtilityWPF.ColorFromHex(LINE_GREEN);
                    _visuals.Add(linesFull);
                    _viewport.Children.Add(linesFull);

                    ScreenSpaceLines3D linesCumulative = new ScreenSpaceLines3D();
                    linesCumulative.Thickness = 3d;
                    linesCumulative.Color = Colors.Chartreuse;
                    _visuals.Add(linesCumulative);
                    _viewport.Children.Add(linesCumulative);

                    ScreenSpaceLines3D linesCumulativeAnti = new ScreenSpaceLines3D();
                    linesCumulativeAnti.Thickness = 2d;
                    linesCumulativeAnti.Color = UtilityWPF.ColorFromHex(LINE_GREEN_ANTI);
                    _visuals.Add(linesCumulativeAnti);
                    _viewport.Children.Add(linesCumulativeAnti);

                    ScreenSpaceLines3D linesOther = new ScreenSpaceLines3D();
                    linesOther.Thickness = 3d;
                    linesOther.Color = UtilityWPF.ColorFromHex(LINE_BLUE);
                    _visuals.Add(linesOther);
                    _viewport.Children.Add(linesOther);

                    //NOTE: Only need to grab the key that represents the most number of vectors firing at 100%.  The lower keys are trumped
                    //by the higher key
                    int key = fullThrusts.Keys[fullThrusts.Keys.Count - 1];
                    //foreach (int key in fullThrusts.Keys)
                    //{

                    double offsetY = (fullThrusts[key].Count * maxVectorLength) / -2d;

                    foreach (int[] fulls in fullThrusts[key])
                    {
                        Vector3D cumulative = new Vector3D(0, 0, 0);
                        Vector3D offset = new Vector3D(offsetX, offsetY, 0);

                        for (int cntr = 0; cntr < _vectors.Length; cntr++)
                        {
                            if (_showAxiis)
                            {
                                DrawAxiis(offset);
                            }

                            if (fulls.Contains(cntr))
                            {
                                // Draw the 100%s as green lines (line and anti line)
                                linesFull.AddLine(offset.ToPoint(), (offset + _vectors[cntr]).ToPoint());
                                cumulative += _vectors[cntr];
                                //linesFullAnti.AddLine(offset.ToPoint(), (offset - _vectors[cntr]).ToPoint());
                            }
                            else
                            {
                                // Draw the remaining lines as blue
                                linesOther.AddLine(offset.ToPoint(), (offset + _vectors[cntr]).ToPoint());
                            }

                            // Draw the hull of the remaining lines
                            TriangleIndexed[] hull = GetHull(_vectors, fulls);
                            if (hull != null)
                            {
                                DrawPossibleHull(hull, offset);
                            }
                        }

                        // Draw the cumulative and anti (it's the anti that was compared to the hull)
                        linesCumulative.AddLine(offset.ToPoint(), (offset + cumulative).ToPoint());
                        linesCumulativeAnti.AddLine(offset.ToPoint(), (offset - cumulative).ToPoint());

                        offsetY += maxVectorLength;
                    }

                    //    offsetX += maxVectorLength;
                    //}

                    #endregion
                }
            }

            private static SortedList<int, List<int[]>> DrawZeroTorqueTest2SprtGetFullThrusts(Vector3D[] vectors)
            {
                SortedList<int, List<int[]>> retVal = new SortedList<int, List<int[]>>();

                //List<int[]> allCombos = new List<int[]>(UtilityHelper.AllCombosEnumerator(vectors.Length));

                // See which sets of thrusters can be fired at 100% - in other words, find the weakest link(s)
                foreach (int[] combo in UtilityCore.AllCombosEnumerator(vectors.Length))
                {
                    List<Vector3D> tests = new List<Vector3D>();
                    List<Vector3D> others = new List<Vector3D>();

                    // Split up the torques
                    for (int cntr = 0; cntr < vectors.Length; cntr++)
                    {
                        if (combo.Contains(cntr))
                        {
                            tests.Add(vectors[cntr]);
                        }
                        else
                        {
                            others.Add(vectors[cntr]);
                        }
                    }

                    // See if this combo can fire
                    if (CanCounterVectors2(tests.ToArray(), others.ToArray()))
                    {
                        if (!retVal.ContainsKey(combo.Length))
                        {
                            retVal.Add(combo.Length, new List<int[]>());
                        }

                        retVal[combo.Length].Add(combo);
                    }
                }

                // Exit Function
                return retVal;
            }
            private static bool CanCounterVectors2(Vector3D[] tests, Vector3D[] others)
            {
                Vector3D cumulative = new Vector3D();
                foreach (Vector3D test in tests)
                {
                    cumulative += test;
                }

                // Make the cumulative point the opposite way (that's what all the tests need)
                cumulative = cumulative * -1d;

                // Check for 0D, 1D, 2D, 3D opposition
                if (others.Length == 0)
                {
                    #region 0D

                    // Nothing to oppose.  The only thing that works is if the test self cancel
                    return Math3D.IsNearZero(cumulative);

                    #endregion
                }
                else if (others.Length == 1)
                {
                    #region 1D

                    // Opposition is a single line
                    // If cumulative's length is longer, then the other is overwhelmed
                    // If the dot product isn't one, then they aren't lined up (cumulative has already been negated)
                    return cumulative.LengthSquared <= others[0].LengthSquared && Math3D.IsNearValue(Vector3D.DotProduct(cumulative.ToUnit(), others[0].ToUnit()), 1d);

                    #endregion
                }
                else if (others.Length == 2)
                {
                    #region 2D

                    // Opposition forms a parallelagram
                    Triangle triangle = new Triangle(new Point3D(0, 0, 0), others[0].ToPoint(), others[1].ToPoint());
                    if (!Math3D.IsNearZero(Vector3D.DotProduct(triangle.Normal, cumulative)))
                    {
                        return false;
                    }

                    Vector bary = Math3D.ToBarycentric(triangle, cumulative.ToPoint());
                    return bary.X >= 0d && bary.Y >= 0d && bary.X + bary.Y <= 2d;

                    #endregion
                }
                else
                {
                    #region 3D

                    //NOTE: The reason there is no need to check for 4D, 5D, etc is because cumulative is a 3D vector

                    // Build the hull out of others
                    Point3D[] extremes = GetRemainingExtremes(others, new int[0]);

                    TriangleIndexed[] hull = Math3D.GetConvexHull(extremes);
                    if (hull != null)
                    {
                        return Math3D.IsInside_Planes(hull, cumulative.ToPoint());
                    }

                    // If hull is null, the points could be coplanar, so try 2D
                    var perimiter = Math2D.GetConvexHull(extremes);
                    if (perimiter != null)
                    {
                        // These are coplanar, see if the cumulative is inside
                        Point? cumulative2D = perimiter.GetTransformedPoint(cumulative.ToPoint());

                        if (cumulative2D == null)
                        {
                            return false;
                        }
                        else
                        {
                            return perimiter.IsInside(cumulative2D.Value);
                        }
                    }

                    return false;

                    #endregion
                }
            }

            #endregion
            #region Private Methods - Zero Torque 3

            private void DrawZeroTorqueTest3()
            {
                const double MINPERCENT = .005d;

                // Find 100% thrusters
                SortedList<int, List<int[]>> fullThrusts = DrawZeroTorqueTest3SprtGetFullThrusts(_vectors);

                if (fullThrusts.Count == 0)
                {
                    #region Draw them red

                    if (_showAxiis)
                    {
                        DrawAxiis(new Vector3D(0, 0, 0));
                    }

                    ScreenSpaceLines3D lines = new ScreenSpaceLines3D();
                    lines.Thickness = 3d;
                    lines.Color = UtilityWPF.ColorFromHex(LINE_RED);

                    for (int cntr = 0; cntr < _vectors.Length; cntr++)
                    {
                        lines.AddLine(new Point3D(0, 0, 0), _vectors[cntr].ToPoint());
                    }

                    _visuals.Add(lines);
                    _viewport.Children.Add(lines);

                    #endregion
                    return;
                }



                //TODO: Implement this
                // Find the next highest percent
                //DrawZeroTorqueTest3SprtNextHighest(_vectors, fullThrusts);




                #region Draw all

                double maxVectorLength = _vectors.Max(o => o.Length);
                maxVectorLength *= 2.5d;

                //double offsetX = (fullThrusts.Keys.Count * maxVectorLength) / -2d;
                double offsetX = 0d;

                ScreenSpaceLines3D linesFull = new ScreenSpaceLines3D();
                linesFull.Thickness = 3d;
                linesFull.Color = UtilityWPF.ColorFromHex(LINE_GREEN);
                _visuals.Add(linesFull);
                _viewport.Children.Add(linesFull);

                ScreenSpaceLines3D linesCumulative = new ScreenSpaceLines3D();
                linesCumulative.Thickness = 3d;
                linesCumulative.Color = Colors.Chartreuse;
                _visuals.Add(linesCumulative);
                _viewport.Children.Add(linesCumulative);

                ScreenSpaceLines3D linesCumulativeAnti = new ScreenSpaceLines3D();
                linesCumulativeAnti.Thickness = 2d;
                linesCumulativeAnti.Color = UtilityWPF.ColorFromHex(LINE_GREEN_ANTI);
                _visuals.Add(linesCumulativeAnti);
                _viewport.Children.Add(linesCumulativeAnti);

                ScreenSpaceLines3D linesOther = new ScreenSpaceLines3D();
                linesOther.Thickness = 3d;
                linesOther.Color = UtilityWPF.ColorFromHex(LINE_BLUE);
                _visuals.Add(linesOther);
                _viewport.Children.Add(linesOther);

                //NOTE: Only need to grab the key that represents the most number of vectors firing at 100%.  The lower keys are trumped
                //by the higher key
                int key = fullThrusts.Keys[fullThrusts.Keys.Count - 1];
                //foreach (int key in fullThrusts.Keys)
                //{

                double offsetY = (fullThrusts[key].Count * maxVectorLength) / -2d;

                foreach (int[] fulls in fullThrusts[key])
                {
                    Vector3D cumulative = new Vector3D(0, 0, 0);
                    Vector3D offset = new Vector3D(offsetX, offsetY, 0);

                    for (int cntr = 0; cntr < _vectors.Length; cntr++)
                    {
                        if (_showAxiis)
                        {
                            DrawAxiis(offset);
                        }

                        if (fulls.Contains(cntr))
                        {
                            // Draw the 100%s as green lines (line and anti line)
                            linesFull.AddLine(offset.ToPoint(), (offset + _vectors[cntr]).ToPoint());
                            cumulative += _vectors[cntr];
                            //linesFullAnti.AddLine(offset.ToPoint(), (offset - _vectors[cntr]).ToPoint());
                        }
                        else
                        {
                            // Draw the remaining lines as blue
                            linesOther.AddLine(offset.ToPoint(), (offset + _vectors[cntr]).ToPoint());
                        }

                        // Draw the hull of the remaining lines
                        TriangleIndexed[] hull = GetHull(_vectors, fulls);
                        if (hull != null)
                        {
                            DrawPossibleHull(hull, offset);
                        }
                    }

                    // Draw the cumulative and anti (it's the anti that was compared to the hull)
                    linesCumulative.AddLine(offset.ToPoint(), (offset + cumulative).ToPoint());
                    linesCumulativeAnti.AddLine(offset.ToPoint(), (offset - cumulative).ToPoint());

                    offsetY += maxVectorLength;
                }

                //    offsetX += maxVectorLength;
                //}

                #endregion
            }

            private static SortedList<int, List<int[]>> DrawZeroTorqueTest3SprtGetFullThrusts(Vector3D[] vectors)
            {
                SortedList<int, List<int[]>> retVal = new SortedList<int, List<int[]>>();

                //List<int[]> allCombos = new List<int[]>(UtilityHelper.AllCombosEnumerator(vectors.Length));

                // See which sets of thrusters can be fired at 100% - in other words, find the weakest link(s)
                foreach (int[] combo in UtilityCore.AllCombosEnumerator(vectors.Length))
                {
                    List<Vector3D> tests = new List<Vector3D>();
                    List<Vector3D> others = new List<Vector3D>();

                    // Split up the torques
                    for (int cntr = 0; cntr < vectors.Length; cntr++)
                    {
                        if (combo.Contains(cntr))
                        {
                            tests.Add(vectors[cntr]);
                        }
                        else
                        {
                            others.Add(vectors[cntr]);
                        }
                    }

                    // See if this combo can fire
                    if (CanCounterVectors3(tests.ToArray(), others.ToArray()))
                    {
                        if (!retVal.ContainsKey(combo.Length))
                        {
                            retVal.Add(combo.Length, new List<int[]>());
                        }

                        retVal[combo.Length].Add(combo);
                    }
                }

                // Exit Function
                return retVal;
            }
            private static bool CanCounterVectors3(Vector3D[] tests, Vector3D[] others)
            {
                Vector3D cumulative = new Vector3D();
                foreach (Vector3D test in tests)
                {
                    cumulative += test;
                }

                // Make the cumulative point the opposite way (that's what all the tests need)
                cumulative = cumulative * -1d;

                // Check for 0D, 1D, 2D, 3D opposition
                if (others.Length == 0)
                {
                    #region 0D

                    // Nothing to oppose.  The only thing that works is if the test self cancel
                    return Math3D.IsNearZero(cumulative);

                    #endregion
                }
                else if (others.Length == 1)
                {
                    #region 1D

                    // Opposition is a single line
                    // If cumulative's length is longer, then the other is overwhelmed
                    // If the dot product isn't one, then they aren't lined up (cumulative has already been negated)
                    return cumulative.LengthSquared <= others[0].LengthSquared && Math3D.IsNearValue(Vector3D.DotProduct(cumulative.ToUnit(), others[0].ToUnit()), 1d);

                    #endregion
                }
                else if (others.Length == 2)
                {
                    #region 2D

                    // Opposition forms a parallelagram
                    Triangle triangle = new Triangle(new Point3D(0, 0, 0), others[0].ToPoint(), others[1].ToPoint());
                    if (!Math3D.IsNearZero(Vector3D.DotProduct(triangle.Normal, cumulative)))
                    {
                        return false;
                    }

                    Vector bary = Math3D.ToBarycentric(triangle, cumulative.ToPoint());
                    return bary.X >= 0d && bary.Y >= 0d && bary.X + bary.Y <= 2d;

                    #endregion
                }
                else
                {
                    #region 3D

                    //NOTE: The reason there is no need to check for 4D, 5D, etc is because cumulative is a 3D vector

                    // Build the hull out of others
                    Point3D[] extremes = GetRemainingExtremes(others, new int[0]);

                    TriangleIndexed[] hull = Math3D.GetConvexHull(extremes);
                    if (hull != null)
                    {
                        return Math3D.IsInside_Planes(hull, cumulative.ToPoint());
                    }

                    // If hull is null, the points could be coplanar, so try 2D
                    var perimiter = Math2D.GetConvexHull(extremes);
                    if (perimiter != null)
                    {
                        // These are coplanar, see if the cumulative is inside
                        Point? cumulative2D = perimiter.GetTransformedPoint(cumulative.ToPoint());

                        if (cumulative2D == null)
                        {
                            return false;
                        }
                        else
                        {
                            return perimiter.IsInside(cumulative2D.Value);
                        }
                    }

                    return false;

                    #endregion
                }
            }

            #endregion
        }

        #endregion
        #region Class: ThrustController

        /// <summary>
        /// This is a proof of concept class.  The final version will be owned by the ship
        /// </summary>
        private class ThrustController : IDisposable
        {
            #region Class: ThrustContribution

            private class ThrustContribution
            {
                #region Constructor

                public ThrustContribution(Thruster thruster, int index, Vector3D translationForce, Vector3D torque)
                {
                    this.Thruster = thruster;
                    this.Index = index;
                    this.TranslationForce = translationForce;
                    this.Torque = torque;

                    //this.TranslationLength = translationForce.Length;
                    //this.TorqueLength = torque.Length;
                }

                #endregion

                public readonly Thruster Thruster;
                public readonly int Index;

                public readonly Vector3D TranslationForce;
                public readonly Vector3D Torque;

                //public readonly double TranslationLength;
                //public readonly double TorqueLength;
            }

            #endregion
            #region Class: ThrustSetting

            private class ThrustSetting
            {
                #region Constructor

                public ThrustSetting(Thruster thruster, int index, Vector3D translationForceFull, Vector3D torqueFull, Vector3D translationForceUnit, Vector3D torqueUnit, Vector3D translationForce, Vector3D torque, double percent)
                {
                    this.Thruster = thruster;
                    this.Index = index;

                    this.TranslationForceFull = translationForceFull;
                    this.TorqueFull = torqueFull;

                    this.TranslationForceUnit = translationForceUnit;
                    this.TorqueUnit = torqueUnit;

                    this.TranslationForce = translationForce;
                    this.Torque = torque;

                    this.Percent = percent;
                }

                #endregion

                public readonly Thruster Thruster;
                public readonly int Index;

                // These are what the translation and torque would be if percent were 1
                public readonly Vector3D TranslationForceFull;
                public readonly Vector3D TorqueFull;

                // These are the translation and torque as unit vectors
                public readonly Vector3D TranslationForceUnit;
                public readonly Vector3D TorqueUnit;

                // These are what the translation and torque are for the stored percent
                public readonly Vector3D TranslationForce;
                public readonly Vector3D Torque;

                // This is how much to fire this thruster
                public readonly double Percent;
            }

            #endregion
            #region Class: ThrustSet

            private class ThrustSet
            {
                #region Constructor

                public ThrustSet(ThrustSetting[] thrusters, Vector3D translation, Vector3D torque, double fuelUsage)
                {
                    this.Thrusters = thrusters;

                    this.Translation = translation;
                    this.Torque = torque;

                    this.TranslationLength = this.Translation.Length;
                    this.TorqueLength = this.Torque.Length;

                    //this.TranslationUnit = this.Translation.ToUnit();
                    //this.TorqueUnit = this.Torque.ToUnit();

                    this.FuelUsage = fuelUsage;
                }

                #endregion

                public readonly ThrustSetting[] Thrusters;

                public readonly Vector3D Translation;
                public readonly Vector3D Torque;

                public readonly double TranslationLength;
                public readonly double TorqueLength;

                //public readonly Vector3D TranslationUnit;
                //public readonly Vector3D TorqueUnit;

                public readonly double FuelUsage;
            }

            #endregion
            #region Class: FiringAttempt

            /// <summary>
            /// This holds percents, and what torque those produce.  It's used as an intermediate to hold a history of best performing attempts (I was just
            /// using a tuple at first, but kept adding properties)
            /// </summary>
            private class FiringAttempt
            {
                #region Constructor

                public FiringAttempt(double[] percents, Vector3D sumTorque)
                {
                    this.PercentsAbsolute = percents;
                    this.PercentsRelative = GetRelativePercents(percents);
                    this.SumTorque = sumTorque;
                    this.SumTorqueLength = sumTorque.Length;
                }

                #endregion

                public readonly double[] PercentsAbsolute;
                public readonly double[] PercentsRelative;
                public readonly Vector3D SumTorque;
                public readonly double SumTorqueLength;

                #region Public Methods

                public static double[] GetRelativePercents(double[] absolutePercents)
                {
                    double maxPercent = absolutePercents.Max();
                    return absolutePercents.Select(o => o / maxPercent).ToArray();
                }

                #endregion
            }

            #endregion

            #region Declaration Section

            private Ship _ship = null;
            private Thruster[] _thrusters = null;

            private Viewport3D _viewport = null;
            private ScreenSpaceLines3D _lines = null;
            private List<Visual3D> _debugVisuals = null;

            private ItemOptions _itemOptions = null;

            private bool _isUpPressed = false;
            private bool _isDownPressed = false;

            private DateTime? _lastTick = null;

            // These are pre calculated to help with figuring out which thrusters to fire when a certain direction is requested
            private Point3D _shipCenterMass;
            private MassMatrix _shipMassMatrix;
            private ThrustContribution[] _contributions = null;
            private ThrustSet[] _zeroTranslationSets = null;
            private ThrustSet[] _zeroTorqueSets = null;
            private bool _useSimple;

            #endregion

            #region Constructor

            public ThrustController(Ship ship, Viewport3D viewport, ItemOptions itemOptions)
            {
                _ship = ship;
                _thrusters = ship.Thrusters;
                _viewport = viewport;
                _itemOptions = itemOptions;

                _lines = new ScreenSpaceLines3D();
                _lines.Color = Colors.Orange;
                _lines.Thickness = 2d;
                _viewport.Children.Add(_lines);
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    if (_viewport != null && _lines != null)
                    {
                        _viewport.Children.Remove(_lines);
                        _lines = null;
                    }

                    if (_viewport != null && _debugVisuals != null)
                    {
                        _viewport.Children.RemoveAll(_debugVisuals);
                        _debugVisuals = null;
                    }

                    _viewport = null;
                }
            }

            #endregion

            #region Public Methods

            /// <summary>
            /// Call this whenever the ship's mass matrix changes
            /// </summary>
            public void MassChanged(bool useSimple)
            {
                _useSimple = useSimple;
                _contributions = null;
                _zeroTranslationSets = null;
                _zeroTorqueSets = null;


                //EnsureThrustSetsCalculated();


            }

            /// <summary>
            /// This fires every thruster at 100%
            /// </summary>
            public void ApplyForce1(BodyApplyForceAndTorqueArgs e)
            {
                _lines.Clear();

                if (!_isUpPressed && !_isDownPressed)
                {
                    return;
                }

                _ship.Fuel.QuantityCurrent = _ship.Fuel.QuantityMax;

                double elapsedTime = 1d;
                DateTime curTick = DateTime.Now;
                if (_lastTick != null)
                {
                    elapsedTime = (curTick - _lastTick.Value).TotalSeconds;
                }

                _lastTick = curTick;

                foreach (Thruster thruster in _thrusters)
                {
                    double percent = 1d;
                    Vector3D? force = thruster.Fire(ref percent, 0, elapsedTime);
                    if (force != null)
                    {
                        Vector3D bodyForce = e.Body.DirectionToWorld(force.Value);
                        Point3D bodyPoint = e.Body.PositionToWorld(thruster.Position);
                        e.Body.AddForceAtPoint(bodyForce, bodyPoint);

                        _lines.AddLine(bodyPoint, bodyPoint - bodyForce);		// subtracting, so the line looks like a flame
                    }
                    else
                    {
                        int seven = -2;
                    }
                }
            }
            public void ApplyForce2(BodyApplyForceAndTorqueArgs e)
            {
                _lines.Clear();

                if (!_isUpPressed && !_isDownPressed)
                {
                    return;
                }

                _ship.Fuel.QuantityCurrent = _ship.Fuel.QuantityMax;

                double elapsedTime = 1d;
                DateTime curTick = DateTime.Now;
                if (_lastTick != null)
                {
                    elapsedTime = (curTick - _lastTick.Value).TotalSeconds;
                }

                _lastTick = curTick;

                // Keeping each button's contribution in a set so they are easier to normalize
                List<Tuple<Thruster, int, double>[]> thrusterSets = new List<Tuple<Thruster, int, double>[]>();

                Vector3D direction;

                if (_isUpPressed)
                {
                    direction = new Vector3D(0, 0, 1);

                    if(_useSimple)
                    {
                        thrusterSets.Add(FireThrustLinear1(e, elapsedTime, direction).ToArray());
                    }
                    else
                    {
                        thrusterSets.Add(FireThrustLinear3(e, elapsedTime, direction).ToArray());
                    }
                }

                if (_isDownPressed)
                {
                    direction = new Vector3D(0, 0, -1);

                    if (_useSimple)
                    {
                        thrusterSets.Add(FireThrustLinear1(e, elapsedTime, direction).ToArray());
                    }
                    else
                    {
                        thrusterSets.Add(FireThrustLinear3(e, elapsedTime, direction).ToArray());
                    }
                }

                //TODO: Normalize these so no thruster will fire above 100% (keep the ratios though)

                #region Fire them

                foreach (var thruster in thrusterSets.SelectMany(o => o))
                {
                    double percent = thruster.Item3;
                    Vector3D? force = thruster.Item1.Fire(ref percent, thruster.Item2, elapsedTime);
                    if (force != null)
                    {
                        Vector3D bodyForce = e.Body.DirectionToWorld(force.Value);
                        Point3D bodyPoint = e.Body.PositionToWorld(thruster.Item1.Position);
                        e.Body.AddForceAtPoint(bodyForce, bodyPoint);

                        Vector3D lineVect = GetThrustLine(bodyForce, _itemOptions.ThrusterStrengthRatio);		// this returns a vector in the opposite direction, so the line looks like a flame
                        Point3D lineStart = bodyPoint + (lineVect.ToUnit() * thruster.Item1.ThrustVisualStartRadius);
                        _lines.AddLine(lineStart, lineStart + lineVect);
                    }
                    else
                    {
                        int seven = -2;
                    }
                }

                #endregion
            }

            public void KeyDown(KeyEventArgs e)
            {
                switch (e.Key)
                {
                    case Key.Up:
                        _isUpPressed = true;
                        break;

                    case Key.Down:
                        _isDownPressed = true;
                        break;
                }
            }
            public void KeyUp(KeyEventArgs e)
            {
                switch (e.Key)
                {
                    case Key.Up:
                        _isUpPressed = false;
                        break;

                    case Key.Down:
                        _isDownPressed = false;
                        break;
                }

                if (!_isDownPressed && !_isUpPressed)
                {
                    _lastTick = null;
                }
            }

            public void DrawDebugVisuals_Pre()
            {
                #region Clear existing

                if (_debugVisuals == null)
                {
                    _debugVisuals = new List<Visual3D>();
                }
                else
                {
                    foreach (Visual3D existing in _debugVisuals)
                    {
                        _viewport.Children.Remove(existing);
                    }
                    _debugVisuals.Clear();
                }

                #endregion

                //EnsureThrustSetsCalculated();
                Point3D centerMass = _ship.PhysicsBody.CenterOfMass;
                var contributions = GetThrusterContributions(_shipCenterMass);

                //bool canAddToZero = CanAddToZero(_contributions.Select(o => o.Torque));		// this method is flawed

                #region Center Mass

                // Material
                MaterialGroup materials = new MaterialGroup();
                materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("BA5E67"))));
                materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("CF6F68")), 75d));

                // Geometry Model
                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = materials;
                geometry.BackMaterial = materials;
                geometry.Geometry = UtilityWPF.GetSphere_LatLon(5, .1d, .1d, .1d);

                ModelVisual3D model = new ModelVisual3D();
                model.Content = geometry;
                model.Transform = new TranslateTransform3D(_ship.PhysicsBody.PositionToWorld(centerMass).ToVector());

                _debugVisuals.Add(model);
                _viewport.Children.Add(model);

                #endregion
                #region Torques

                ScreenSpaceLines3D lines = new ScreenSpaceLines3D();
                lines.Thickness = 3d;
                //if (canAddToZero)
                //{
                lines.Color = UtilityWPF.ColorFromHex("91B57F");
                //}
                //else
                //{
                //    lines.Color = UtilityWPF.ColorFromHex("EB9B73");
                //}

                ScreenSpaceLines3D opposites = new ScreenSpaceLines3D();
                opposites.Thickness = 1d;
                opposites.Color = UtilityWPF.ColorFromHex("FFDAA5");

                double scalePercent = GetScaledDebugTorquePercent(contributions.Select(o => o.Torque));

                foreach (var thruster in contributions)
                {
                    lines.AddLine(_ship.PhysicsBody.PositionToWorld(centerMass), _ship.PhysicsBody.PositionToWorld(centerMass) + _ship.PhysicsBody.DirectionToWorld(thruster.Torque * scalePercent));
                    opposites.AddLine(_ship.PhysicsBody.PositionToWorld(centerMass), _ship.PhysicsBody.PositionToWorld(centerMass) - _ship.PhysicsBody.DirectionToWorld(thruster.Torque * scalePercent));
                }

                _debugVisuals.Add(lines);
                _debugVisuals.Add(opposites);
                _viewport.Children.Add(lines);
                _viewport.Children.Add(opposites);

                #endregion

                // May want to do translations

            }
            public void DrawDebugVisuals_Post()
            {
            }

            #endregion

            #region Private Methods - fire

            //NOTE: direction must be a unit vector
            /// <summary>
            /// This fires any thruster that contributes to the direction at 100%
            /// </summary>
            private List<Tuple<Thruster, int, double>> FireThrustLinear1(BodyApplyForceAndTorqueArgs e, double elapsedTime, Vector3D direction)
            {
                #region Get contributing thrusters

                // Get a list of thrusters that will contribute to the direction

                List<Tuple<Thruster, int, Vector3D, double>> contributing = new List<Tuple<Thruster, int, Vector3D, double>>();

                foreach (Thruster thruster in _thrusters)
                {
                    for (int cntr = 0; cntr < thruster.ThrusterDirectionsShip.Length; cntr++)
                    {
                        Vector3D thrustDirUnit = thruster.ThrusterDirectionsShip[cntr].ToUnit();
                        double dot = Vector3D.DotProduct(thrustDirUnit, direction);

                        if (dot > 0d)
                        {
                            contributing.Add(new Tuple<Thruster, int, Vector3D, double>(thruster, cntr, thrustDirUnit, dot));
                        }
                    }
                }

                #endregion

                List<Tuple<Thruster, int, double>> retVal = new List<Tuple<Thruster, int, double>>();

                retVal.AddRange(contributing.Select(o => Tuple.Create(o.Item1, o.Item2, 1d)));      // 1 for 100%

                return retVal;

                //Point3D center = _ship.PhysicsBody.CenterOfMass;
                //MassMatrix massMatrix = _ship.PhysicsBody.MassMatrix;

                //// Figure out the drift


                //// See which thrusters can most reduce that drift

                //#region Fire them

                //foreach (var contribute in contributing)
                //{
                //    double percent = 1d;
                //    Vector3D? force = contribute.Item1.Fire(ref percent, contribute.Item2, elapsedTime);
                //    if (force != null)
                //    {
                //        Vector3D bodyForce = e.Body.DirectionToWorld(force.Value);
                //        Point3D bodyPoint = e.Body.PositionToWorld(contribute.Item1.Position);
                //        e.Body.AddForceAtPoint(bodyForce, bodyPoint);

                //        _lines.AddLine(bodyPoint, bodyPoint - bodyForce);		// subtracting, so the line looks like a flame
                //    }
                //    else
                //    {
                //        int seven = -2;
                //    }
                //}

                //#endregion
            }
            /// <summary>
            /// This is an attempt to fix 1, but I didn't get very far
            /// </summary>
            private List<Tuple<Thruster, int, double>> FireThrustLinear2(BodyApplyForceAndTorqueArgs e, double elapsedTime, Vector3D direction)
            {
                Point3D center = _ship.PhysicsBody.CenterOfMass;
                MassMatrix massMatrix = _ship.PhysicsBody.MassMatrix;

                ThrustContribution[] contributions = GetThrusterContributions(center);

                #region Find along and against

                List<Tuple<ThrustContribution, double>> along = new List<Tuple<ThrustContribution, double>>();
                List<Tuple<ThrustContribution, double>> against = new List<Tuple<ThrustContribution, double>>();

                foreach (ThrustContribution contribution in contributions)
                {
                    double dot = Vector3D.DotProduct(contribution.Thruster.ThrusterDirectionsShip[contribution.Index], direction);

                    // Don't include zero
                    if (dot > 0d)
                    {
                        along.Add(new Tuple<ThrustContribution, double>(contribution, dot));
                    }
                    else if (dot < 0d)
                    {
                        against.Add(new Tuple<ThrustContribution, double>(contribution, dot));
                    }
                }

                #endregion

                Tuple<Vector3D, Vector3D> alongCombinedForceAndTorque = GetCombinedForceAndTorque(along.Select(o => new Tuple<Vector3D, Vector3D>(o.Item1.TranslationForce, o.Item1.Torque)));


                // I think that fixing torque drif is much more important than linear drift


                if (!Math3D.IsNearZero(alongCombinedForceAndTorque.Item2.LengthSquared))
                {
                    // There is a net torque




                }



                if (!Math3D.IsNearValue(Vector3D.DotProduct(alongCombinedForceAndTorque.Item1.ToUnit(), direction), 1d))
                {
                    // There is a linear drift



                }







                // Exit Function
                return along.Select(o => new Tuple<Thruster, int, double>(o.Item1.Thruster, o.Item1.Index, 1d)).ToList();
            }
            private List<Tuple<Thruster, int, double>> FireThrustLinear3(BodyApplyForceAndTorqueArgs e, double elapsedTime, Vector3D direction)
            {
                //TODO: Expose these as properties of the class, or take as args to the method call
                const double PERFECTALIGN = .01;

                EnsureThrustSetsCalculated();

                // Find the best thruster set for the requested direction, may need to combine sets

                var matches = _zeroTorqueSets.Select(o => new Tuple<ThrustSet, double>(o, Vector3D.DotProduct(o.Translation, direction))).OrderByDescending(o => o.Item2).ToArray();

                if (matches.Length > 0)
                {
                    // Find set that are perfectly aligned (direction is a unit vector, so if they are perfectly aligned, the dot product will be the translation's length)
                    var perfectAlign = matches.Where(o => IsNearValue(o.Item2, o.Item1.TranslationLength, o.Item1.TranslationLength * PERFECTALIGN)).OrderByDescending(o => o.Item2).ToArray();

                    if (perfectAlign.Length > 0)
                    {
                        ThrustSet set = GetBestPerfectAlign(perfectAlign);

                        return set.Thrusters.Select(o => new Tuple<Thruster, int, double>(o.Thruster, o.Index, o.Percent)).ToList();
                    }


                    //TODO: Combine solutions to get the requested direction

                    ThrustSet set2 = matches.First().Item1;

                    return set2.Thrusters.Select(o => new Tuple<Thruster, int, double>(o.Thruster, o.Index, o.Percent)).ToList();



                }

                return new List<Tuple<Thruster, int, double>>();
            }

            private static ThrustSet GetBestPerfectAlign(Tuple<ThrustSet, double>[] sets)
            {
                // Find the most fuel efficient set
                var setsWithFuelEfficiency = sets.Select(o => new Tuple<ThrustSet, double, double>(o.Item1, o.Item2, o.Item2 / o.Item1.FuelUsage)).OrderByDescending(o => o.Item3).ToArray();

                return setsWithFuelEfficiency[0].Item1;
            }

            private static Tuple<Vector3D, Vector3D> GetCombinedForceAndTorque(IEnumerable<Tuple<Vector3D, Vector3D>> forcesAndTorques)
            {
                Vector3D force = new Vector3D(0, 0, 0);
                Vector3D torque = new Vector3D(0, 0, 0);

                foreach (var item in forcesAndTorques)
                {
                    force += item.Item1;
                    torque += item.Item2;
                }

                return new Tuple<Vector3D, Vector3D>(force, torque);
            }

            private static Vector3D GetThrustLine(Vector3D force, double strengthRatio)
            {
                const double MULT = -1d;		// the desired length when force.length / strengthRatio is one

                double ratio = MULT * force.Length / strengthRatio;

                return force.ToUnit() * MULT;
            }

            #endregion
            #region Private Methods - prep

            internal void EnsureThrustSetsCalculated()
            {
                if (_contributions != null)
                {
                    return;
                }

                _shipCenterMass = _ship.PhysicsBody.CenterOfMass;
                _shipMassMatrix = _ship.PhysicsBody.MassMatrix;

                _contributions = GetThrusterContributions(_shipCenterMass);

                List<ThrustSet> zeroTorques = new List<ThrustSet>();
                List<ThrustSet> zeroTranslations = new List<ThrustSet>();

                var illegalCombos = GetIllegalCombos(_contributions);

                double fuelToThrust = _itemOptions.FuelToThrustRatio;

                //var test = AllCombosEnumerator(_contributions.Length, illegalCombos).ToList();

                //NOTE: AllCombosEnumerator seems to only be called once, so I'm guessing AsParallel gets all enumerated values before
                //dividing up the work (I would hate for it to call AllCombosEnumerator many times - once for Count(), etc)
                //foreach (Tuple<ThrustSet[], ThrustSet[]> zeros in AllCombosEnumerator(_contributions.Length, illegalCombos).AsParallel().Select(o => GetZeros(_contributions, o, fuelToThrust)))
                foreach (Tuple<ThrustSet[], ThrustSet[]> zeros in AllCombosEnumerator(_contributions.Length, illegalCombos).Select(o => GetZeros(_contributions, o, fuelToThrust, _shipCenterMass)))
                {
                    if (zeros == null)
                    {
                        continue;
                    }

                    if (zeros.Item1 != null && zeros.Item1.Length > 0)
                    {
                        zeroTorques.AddRange(zeros.Item1);
                    }

                    if (zeros.Item2 != null && zeros.Item2.Length > 0)
                    {
                        zeroTranslations.AddRange(zeros.Item2);
                    }
                }

                _zeroTorqueSets = zeroTorques.ToArray();
                _zeroTranslationSets = zeroTranslations.ToArray();
            }

            private ThrustContribution[] GetThrusterContributions(Point3D center)
            {
                List<ThrustContribution> retVal = new List<ThrustContribution>();

                foreach (Thruster thruster in _thrusters)
                {
                    for (int cntr = 0; cntr < thruster.ThrusterDirectionsShip.Length; cntr++)
                    {
                        // This is copied from Body.AddForceAtPoint

                        Vector3D offsetFromMass = thruster.Position - center;		// this is ship's local coords
                        Vector3D force = thruster.ThrusterDirectionsShip[cntr] * thruster.ForceAtMax;

                        Vector3D translationForce, torque;
                        Math3D.SplitForceIntoTranslationAndTorque(out translationForce, out torque, offsetFromMass, force);

                        retVal.Add(new ThrustContribution(thruster, cntr, translationForce, torque));
                    }
                }

                // Exit Function
                return retVal.ToArray();
            }

            /// <summary>
            /// This taks a set of thrusters and tries to find combinations that will produce zero torque and combinations that will produce zero translation
            /// </summary>
            /// <returns>
            /// Item1=Torques
            /// Item2=Translations
            /// </returns>
            private static Tuple<ThrustSet[], ThrustSet[]> GetZeros(ThrustContribution[] all, int[] combo, double fuelToThrust, Point3D centerMass)
            {
                // Get an array of the referenced thrusters
                ThrustContribution[] used = new ThrustContribution[combo.Length];
                for (int cntr = 0; cntr < combo.Length; cntr++)
                {
                    used[cntr] = all[combo[cntr]];
                }

                //This is now done up front (more efficient)
                // Make sure this combo doesn't have the same thruster firing is opposite directions
                //if (!IsValidCombo(used))
                //{
                //    return null;
                //}

                // Try to find combinations of these thrusters that will produce zero torque and zero translation
                List<ThrustSet> torques = GetZeroTorques(used, fuelToThrust, centerMass);
                List<ThrustSet> translations = GetZeroTranslations(used, fuelToThrust);

                // Exit Function
                if (torques.Count == 0 && translations.Count == 0)
                {
                    return null;
                }
                else
                {
                    return new Tuple<ThrustSet[], ThrustSet[]>(torques.ToArray(), translations.ToArray());
                }
            }

            private static Tuple<int, int>[] GetIllegalCombos(ThrustContribution[] thrusters)
            {
                List<Tuple<int, int>> retVal = new List<Tuple<int, int>>();

                for (int outer = 0; outer < thrusters.Length - 1; outer++)
                {
                    for (int inner = outer + 1; inner < thrusters.Length; inner++)
                    {
                        if (thrusters[outer].Thruster == thrusters[inner].Thruster)
                        {
                            Vector3D dir1 = thrusters[outer].Thruster.ThrusterDirectionsModel[thrusters[outer].Index];
                            Vector3D dir2 = thrusters[inner].Thruster.ThrusterDirectionsModel[thrusters[inner].Index];

                            if (Math3D.IsNearValue(Vector3D.DotProduct(dir1, dir2), -1d))
                            {
                                retVal.Add(new Tuple<int, int>(outer, inner));
                            }
                        }
                    }
                }

                return retVal.ToArray();
            }

            //TODO: Finish these
            private static List<ThrustSet> GetZeroTorques(ThrustContribution[] used, double fuelToThrust, Point3D centerMass)
            {
                // Split the list into inline thrusters, and thrusters that produce torque.  So there could be 5 total, but if only 2
                // produce torque, only those 2 are used in the torque calculations
                List<ThrustContribution> noTorque = new List<ThrustContribution>();
                List<ThrustContribution> hasTorque = new List<ThrustContribution>();

                for (int cntr = 0; cntr < used.Length; cntr++)
                {
                    if (IsNearZeroTorque(used[cntr].Torque))
                    {
                        noTorque.Add(used[cntr]);
                    }
                    else
                    {
                        hasTorque.Add(used[cntr]);
                    }
                }

                List<ThrustSet> retVal = new List<ThrustSet>();

                if (hasTorque.Count == 0)
                {
                    // All zero torque
                    retVal.AddRange(GetZeroTorquesSprtZeroTorque(noTorque, fuelToThrust));
                }
                else if (hasTorque.Count == 2)
                {
                    // Two
                    retVal.AddRange(GetZeroTorquesSprtTwoTorque(noTorque, hasTorque, fuelToThrust));
                }
                else if (hasTorque.Count > 2)
                {
                    // Many
                    //retVal.AddRange(GetZeroTorquesSprtManyTorque1(noTorque, hasTorque, fuelToThrust));
                    retVal.AddRange(GetZeroTorquesSprtManyTorque5(noTorque, hasTorque, fuelToThrust, centerMass));
                }

                // Throw out any that have zero translation
                retVal = retVal.Where(o => !IsNearZeroTranslation(o.TranslationLength)).ToList();

                // Exit Function
                return retVal;
            }
            private static List<ThrustSet> GetZeroTorquesSprtZeroTorque(List<ThrustContribution> noTorque, double fuelToThrust)
            {
                Vector3D translation = new Vector3D(0, 0, 0);
                Vector3D torque = new Vector3D(0, 0, 0);
                double fuelUsed = 0d;

                ThrustSetting[] thrusters = new ThrustSetting[noTorque.Count];
                for (int cntr = 0; cntr < noTorque.Count; cntr++)
                {
                    thrusters[cntr] = new ThrustSetting(noTorque[cntr].Thruster, noTorque[cntr].Index, noTorque[cntr].TranslationForce, noTorque[cntr].Torque, noTorque[cntr].TranslationForce.ToUnit(), noTorque[cntr].Torque.ToUnit(), noTorque[cntr].TranslationForce, noTorque[cntr].Torque, 1d);

                    translation += noTorque[cntr].TranslationForce;
                    torque += noTorque[cntr].Torque;
                    fuelUsed += noTorque[cntr].Thruster.ForceAtMax * fuelToThrust;
                }

                List<ThrustSet> retVal = new List<ThrustSet>();
                retVal.Add(new ThrustSet(thrusters, translation, torque, fuelUsed));
                return retVal;
            }
            private static List<ThrustSet> GetZeroTorquesSprtTwoTorque(List<ThrustContribution> noTorque, List<ThrustContribution> hasTorque, double fuelToThrust)
            {
                List<ThrustSet> retVal = new List<ThrustSet>();

                // Both torques are non zero.  See if they're opposed
                Vector3D torque1Unit = hasTorque[0].Torque.ToUnit();
                Vector3D torque2Unit = hasTorque[1].Torque.ToUnit();

                double dot = Vector3D.DotProduct(torque1Unit, torque2Unit);

                if (!IsNearValueTorquesDot(dot, -1d))
                {
                    // Nothing to return
                    return retVal;
                }

                // They are opposed, now get the ratio between the two
                double[] lengths = new double[] { hasTorque[0].Torque.Length, hasTorque[1].Torque.Length };

                Vector3D translation = new Vector3D(0, 0, 0);
                Vector3D torque = new Vector3D(0, 0, 0);
                double fuelUsed = 0d;

                List<ThrustSetting> thrusters = new List<ThrustSetting>();

                // Add the two torque producing thrusters
                for (int cntr = 0; cntr < hasTorque.Count; cntr++)
                {
                    #region Add opposing torques

                    double lengthThis = lengths[cntr];
                    double lengthOther = cntr == 0 ? lengths[1] : lengths[0];

                    double percent = 1d;

                    // Figure out the percent.  The smaller one fires at 100%.  The other is a fraction of that
                    if (!Math3D.IsNearValue(lengthThis, lengthOther) && lengthThis > lengthOther)
                    {
                        percent = lengthOther / lengthThis;
                    }

                    thrusters.Add(new ThrustSetting(hasTorque[cntr].Thruster, hasTorque[cntr].Index, hasTorque[cntr].TranslationForce, hasTorque[cntr].Torque, hasTorque[cntr].TranslationForce.ToUnit(), hasTorque[cntr].Torque.ToUnit(), hasTorque[cntr].TranslationForce * percent, hasTorque[cntr].Torque * percent, percent));

                    translation += hasTorque[cntr].TranslationForce * percent;
                    torque += hasTorque[cntr].Torque * percent;
                    fuelUsed += hasTorque[cntr].Thruster.ForceAtMax * fuelToThrust * percent;

                    #endregion
                }

                // Add any inline thrusters each at 100%
                foreach (ThrustContribution thruster in noTorque)
                {
                    #region Add inline

                    thrusters.Add(new ThrustSetting(thruster.Thruster, thruster.Index, thruster.TranslationForce, thruster.Torque, thruster.TranslationForce.ToUnit(), thruster.Torque.ToUnit(), thruster.TranslationForce, thruster.Torque, 1d));

                    translation += thruster.TranslationForce;
                    torque += thruster.Torque;
                    fuelUsed += thruster.Thruster.ForceAtMax * fuelToThrust;

                    #endregion
                }

                retVal.Add(new ThrustSet(thrusters.ToArray(), translation, torque, fuelUsed));

                // Exit Function
                return retVal;
            }
            private static List<ThrustSet> GetZeroTorquesSprtManyTorque1(List<ThrustContribution> noTorque, List<ThrustContribution> hasTorque, double fuelToThrust)
            {
                List<ThrustSet> retVal = new List<ThrustSet>();

                // See if adds to zero
                if (!CanAddToZero(hasTorque.Select(o => o.Torque)))		//NOTE: This method has a lot of false positives
                {
                    // Nothing to return
                    return retVal;
                }

                #region OLD

                //bool[] foundPlusMinus = new bool[6];
                //foreach (ThrustContribution thrust in hasTorque)
                //{
                //    if (thrust.Torque.X > 0)
                //    {
                //        foundPlusMinus[0] = true;
                //    }
                //    else if (thrust.Torque.X < 0)
                //    {
                //        foundPlusMinus[1] = true;
                //    }

                //    if (thrust.Torque.Y > 0)
                //    {
                //        foundPlusMinus[2] = true;
                //    }
                //    else if (thrust.Torque.Y < 0)
                //    {
                //        foundPlusMinus[3] = true;
                //    }

                //    if (thrust.Torque.Z > 0)
                //    {
                //        foundPlusMinus[4] = true;
                //    }
                //    else if (thrust.Torque.Z < 0)
                //    {
                //        foundPlusMinus[5] = true;
                //    }
                //}

                //if (foundPlusMinus[0] != foundPlusMinus[1] || foundPlusMinus[2] != foundPlusMinus[3] || foundPlusMinus[4] != foundPlusMinus[5])
                //{
                //    // Nothing to return
                //    return retVal;
                //}

                //if (!foundPlusMinus.All(o => o))
                //{
                //    // Nothing to return
                //    return retVal;
                //}

                #endregion

                // Not exactly sure how to figure this out, but I'm guessing it's some combination of cross and dot products

                // May need lots of visualizations of thruster setup, and the debug lines

                // There could be stable sets within what was passed in, for example:
                //		4 thrusters in a cross pointing down.  Each opposite pair perfectly oppose each other, but all 4 can be fired at 100%
                //		

                // If the torque is along or against the translation, that should also be added, but marked (this would cause the
                // ship to spin like a football but at least it won't wobble)



                return new List<ThrustSet>();
            }
            private static List<ThrustSet> GetZeroTorquesSprtManyTorque2(List<ThrustContribution> noTorque, List<ThrustContribution> hasTorque, double fuelToThrust)
            {

                // This method is stable font/back, but won't stop roll around sumTorque


                List<ThrustSet> retVal = new List<ThrustSet>();

                // Add up all the torques
                Vector3D sumTorque = new Vector3D(0, 0, 0);
                foreach (ThrustContribution thrust in hasTorque)
                {
                    sumTorque += thrust.Torque;
                }

                if (IsNearZeroTorque(sumTorque))
                {
                    #region Fire all at 100%

                    List<ThrustSetting> thrusters1 = new List<ThrustSetting>();
                    Vector3D translation1 = new Vector3D();
                    Vector3D torque1 = new Vector3D();
                    double fuelUsed1 = 0d;

                    foreach (ThrustContribution thruster in new List<ThrustContribution>[] { noTorque, hasTorque }.SelectMany(o => o))
                    {
                        thrusters1.Add(new ThrustSetting(thruster.Thruster, thruster.Index, thruster.TranslationForce, thruster.Torque, thruster.TranslationForce.ToUnit(), thruster.Torque.ToUnit(), thruster.TranslationForce, thruster.Torque, 1d));

                        translation1 += thruster.TranslationForce;
                        torque1 += thruster.Torque;
                        fuelUsed1 += thruster.Thruster.ForceAtMax * fuelToThrust;
                    }

                    retVal.Add(new ThrustSet(thrusters1.ToArray(), translation1, torque1, fuelUsed1));

                    #endregion
                    return retVal;
                }

                Vector3D sumPositives = new Vector3D(0, 0, 0);
                Vector3D sumNegatives = new Vector3D(0, 0, 0);

                #region Dot with sum torque

                // Take the dots of all torques relative to that sum
                List<Tuple<ThrustContribution, double>> posDots = new List<Tuple<ThrustContribution, double>>();
                List<Tuple<ThrustContribution, double>> negDots = new List<Tuple<ThrustContribution, double>>();

                foreach (ThrustContribution thrust in hasTorque)
                {
                    double dot = Vector3D.DotProduct(thrust.Torque, sumTorque);
                    if (dot < 0)		// go ahead and put the zeros with the positives
                    {
                        negDots.Add(new Tuple<ThrustContribution, double>(thrust, dot));
                        sumNegatives += thrust.Torque;
                    }
                    else
                    {
                        posDots.Add(new Tuple<ThrustContribution, double>(thrust, dot));
                        sumPositives += thrust.Torque;
                    }
                }

                if (negDots.Count == 0)
                {
                    // There are no negative dots, so it's impossible to come up with a combination that adds to zero
                    return retVal;
                }

                #endregion

                // Attempt1: all negatives fire 100%, all positives can't exceed the total of the negatives
                //TODO: This is too simplistic.  It balances the torque along the sum, but not left/right of that vector
                double percent = sumNegatives.Length / sumPositives.Length;

                List<ThrustSetting> thrusters2 = new List<ThrustSetting>();
                Vector3D translation2 = new Vector3D();
                Vector3D torque2 = new Vector3D();
                double fuelUsed2 = 0d;

                // Positives
                foreach (var thruster in posDots)
                {
                    thrusters2.Add(new ThrustSetting(thruster.Item1.Thruster, thruster.Item1.Index,
                        thruster.Item1.TranslationForce, thruster.Item1.Torque,
                        thruster.Item1.TranslationForce.ToUnit(), thruster.Item1.Torque.ToUnit(),
                        thruster.Item1.TranslationForce * percent, thruster.Item1.Torque * percent,
                        percent));

                    translation2 += thruster.Item1.TranslationForce * percent;
                    torque2 += thruster.Item1.Torque * percent;
                    fuelUsed2 += thruster.Item1.Thruster.ForceAtMax * fuelToThrust * percent;
                }

                // Negatives
                foreach (var thruster in negDots)
                {
                    thrusters2.Add(new ThrustSetting(thruster.Item1.Thruster, thruster.Item1.Index, thruster.Item1.TranslationForce, thruster.Item1.Torque, thruster.Item1.TranslationForce.ToUnit(), thruster.Item1.Torque.ToUnit(), thruster.Item1.TranslationForce, thruster.Item1.Torque, 1d));

                    translation2 += thruster.Item1.TranslationForce;
                    torque2 += thruster.Item1.Torque;
                    fuelUsed2 += thruster.Item1.Thruster.ForceAtMax * fuelToThrust;
                }

                // Inlines
                foreach (ThrustContribution thruster in noTorque)
                {
                    thrusters2.Add(new ThrustSetting(thruster.Thruster, thruster.Index, thruster.TranslationForce, thruster.Torque, thruster.TranslationForce.ToUnit(), thruster.Torque.ToUnit(), thruster.TranslationForce, thruster.Torque, 1d));

                    translation2 += thruster.TranslationForce;
                    torque2 += thruster.Torque;
                    fuelUsed2 += thruster.Thruster.ForceAtMax * fuelToThrust;
                }


                retVal.Add(new ThrustSet(thrusters2.ToArray(), translation2, torque2, fuelUsed2));



                //return new List<ThrustSet>();
                return retVal;
            }
            private static List<ThrustSet> GetZeroTorquesSprtManyTorque3(List<ThrustContribution> noTorque, List<ThrustContribution> hasTorque, double fuelToThrust, Point3D centerMass)
            {
                List<ThrustSet> retVal = new List<ThrustSet>();

                // Add up all the torques
                Vector3D sumTorque = new Vector3D(0, 0, 0);
                foreach (ThrustContribution thrust in hasTorque)
                {
                    sumTorque += thrust.Torque;
                }

                if (IsNearZeroTorque(sumTorque))
                {
                    #region Fire all at 100%

                    List<ThrustSetting> thrusters1 = new List<ThrustSetting>();
                    Vector3D translation1 = new Vector3D();
                    Vector3D torque1 = new Vector3D();
                    double fuelUsed1 = 0d;

                    foreach (ThrustContribution thruster in new List<ThrustContribution>[] { noTorque, hasTorque }.SelectMany(o => o))
                    {
                        thrusters1.Add(new ThrustSetting(thruster.Thruster, thruster.Index, thruster.TranslationForce, thruster.Torque, thruster.TranslationForce.ToUnit(), thruster.Torque.ToUnit(), thruster.TranslationForce, thruster.Torque, 1d));

                        translation1 += thruster.TranslationForce;
                        torque1 += thruster.Torque;
                        fuelUsed1 += thruster.Thruster.ForceAtMax * fuelToThrust;
                    }

                    retVal.Add(new ThrustSet(thrusters1.ToArray(), translation1, torque1, fuelUsed1));

                    #endregion
                    return retVal;
                }

                Vector3D sumPositives = new Vector3D(0, 0, 0);
                Vector3D sumNegatives = new Vector3D(0, 0, 0);

                #region Dot with sum torque

                // Take the dots of all torques relative to that sum
                List<Tuple<ThrustContribution, double>> posDots = new List<Tuple<ThrustContribution, double>>();
                List<Tuple<ThrustContribution, double>> negDots = new List<Tuple<ThrustContribution, double>>();

                foreach (ThrustContribution thrust in hasTorque)
                {
                    double dot = Vector3D.DotProduct(thrust.Torque, sumTorque);
                    if (dot < 0)		// go ahead and put the zeros with the positives
                    {
                        negDots.Add(new Tuple<ThrustContribution, double>(thrust, dot));
                        sumNegatives += thrust.Torque;
                    }
                    else
                    {
                        posDots.Add(new Tuple<ThrustContribution, double>(thrust, dot));
                        sumPositives += thrust.Torque;
                    }
                }

                if (negDots.Count == 0)
                {
                    // There are no negative dots, so it's impossible to come up with a combination that adds to zero
                    return retVal;
                }

                #endregion

                // All negatives fire 100%, all positives can't exceed the total of the negatives
                double percentFrontBack = sumNegatives.Length / sumPositives.Length;

                #region Add up orths

                // The front/back is balanced, now balance the torque around that sum line
                List<Tuple<ThrustContribution, Vector3D>> posDistToLine = new List<Tuple<ThrustContribution, Vector3D>>();
                List<Tuple<ThrustContribution, Vector3D>> negDistToLine = new List<Tuple<ThrustContribution, Vector3D>>();
                Vector3D sumOrth = new Vector3D(0, 0, 0);

                foreach (var thrust in posDots)
                {
                    Point3D torquePoint = (thrust.Item1.Torque * percentFrontBack).ToPoint();
                    Point3D pointAlongLine = Math3D.GetClosestPoint_Line_Point(new Point3D(0, 0, 0), sumTorque, torquePoint);
                    Vector3D line = torquePoint - pointAlongLine;
                    sumOrth += line;
                    posDistToLine.Add(new Tuple<ThrustContribution, Vector3D>(thrust.Item1, line));
                }

                foreach (var thrust in negDots)
                {
                    Point3D torquePoint = thrust.Item1.Torque.ToPoint();		// the negatives fire at 100%
                    Point3D pointAlongLine = Math3D.GetClosestPoint_Line_Point(new Point3D(0, 0, 0), sumTorque, torquePoint);
                    Vector3D line = torquePoint - pointAlongLine;
                    sumOrth += line;
                    negDistToLine.Add(new Tuple<ThrustContribution, Vector3D>(thrust.Item1, line));
                }

                #endregion

                if (IsNearZeroTorque(sumOrth))
                {
                    #region Orth already balanced

                    List<ThrustSetting> thrusters2 = new List<ThrustSetting>();
                    Vector3D translation2 = new Vector3D();
                    Vector3D torque2 = new Vector3D();
                    double fuelUsed2 = 0d;

                    // Positives
                    foreach (var thruster in posDots)
                    {
                        thrusters2.Add(new ThrustSetting(thruster.Item1.Thruster, thruster.Item1.Index,
                            thruster.Item1.TranslationForce, thruster.Item1.Torque,
                            thruster.Item1.TranslationForce.ToUnit(), thruster.Item1.Torque.ToUnit(),
                            thruster.Item1.TranslationForce * percentFrontBack, thruster.Item1.Torque * percentFrontBack,
                            percentFrontBack));

                        translation2 += thruster.Item1.TranslationForce * percentFrontBack;
                        torque2 += thruster.Item1.Torque * percentFrontBack;
                        fuelUsed2 += thruster.Item1.Thruster.ForceAtMax * fuelToThrust * percentFrontBack;
                    }

                    // Negatives
                    foreach (var thruster in negDots)
                    {
                        thrusters2.Add(new ThrustSetting(thruster.Item1.Thruster, thruster.Item1.Index, thruster.Item1.TranslationForce, thruster.Item1.Torque, thruster.Item1.TranslationForce.ToUnit(), thruster.Item1.Torque.ToUnit(), thruster.Item1.TranslationForce, thruster.Item1.Torque, 1d));

                        translation2 += thruster.Item1.TranslationForce;
                        torque2 += thruster.Item1.Torque;
                        fuelUsed2 += thruster.Item1.Thruster.ForceAtMax * fuelToThrust;
                    }

                    // Inlines
                    foreach (ThrustContribution thruster in noTorque)
                    {
                        thrusters2.Add(new ThrustSetting(thruster.Thruster, thruster.Index, thruster.TranslationForce, thruster.Torque, thruster.TranslationForce.ToUnit(), thruster.Torque.ToUnit(), thruster.TranslationForce, thruster.Torque, 1d));

                        translation2 += thruster.TranslationForce;
                        torque2 += thruster.Torque;
                        fuelUsed2 += thruster.Thruster.ForceAtMax * fuelToThrust;
                    }

                    retVal.Add(new ThrustSet(thrusters2.ToArray(), translation2, torque2, fuelUsed2));

                    #endregion
                    return retVal;
                }

                #region Dot with sum orth

                // first double is the dot, second double is the front/back percent
                List<Tuple<ThrustContribution, double, Vector3D, double>> posOrthDots = new List<Tuple<ThrustContribution, double, Vector3D, double>>();
                List<Tuple<ThrustContribution, double, Vector3D, double>> negOrthDots = new List<Tuple<ThrustContribution, double, Vector3D, double>>();

                foreach (var thrust in posDistToLine)
                {
                    double dot = Vector3D.DotProduct(thrust.Item2, sumOrth);
                    if (dot < 0)		// go ahead and put the zeros with the positives
                    {
                        negOrthDots.Add(new Tuple<ThrustContribution, double, Vector3D, double>(thrust.Item1, dot, thrust.Item2, percentFrontBack));
                    }
                    else
                    {
                        posOrthDots.Add(new Tuple<ThrustContribution, double, Vector3D, double>(thrust.Item1, dot, thrust.Item2, percentFrontBack));
                    }
                }

                foreach (var thrust in negDistToLine)
                {
                    double dot = Vector3D.DotProduct(thrust.Item2, sumOrth);
                    if (dot < 0)		// go ahead and put the zeros with the positives
                    {
                        negOrthDots.Add(new Tuple<ThrustContribution, double, Vector3D, double>(thrust.Item1, dot, thrust.Item2, 1d));
                    }
                    else
                    {
                        posOrthDots.Add(new Tuple<ThrustContribution, double, Vector3D, double>(thrust.Item1, dot, thrust.Item2, 1d));
                    }
                }

                if (negOrthDots.Count == 0)
                {
                    // They all spin in one direction around that line, so can't return anything
                    return retVal;
                }

                Vector3D sumOrthPositives = new Vector3D(0, 0, 0);
                Vector3D sumOrthNegatives = new Vector3D(0, 0, 0);

                foreach (var thrust in posOrthDots)
                {
                    sumOrthPositives += thrust.Item3;
                }

                foreach (var thrust in negOrthDots)
                {
                    sumOrthNegatives += thrust.Item3;
                }

                #endregion

                double percentLeftRight = sumOrthNegatives.Length / sumOrthPositives.Length;



                // Find a combination of thruster firings that satisfy these two percents (probaly two linear equations, which if that's the case, do that in the begining)



                //return GetZeroTorquesSprtManyTorque3SprtReturnA(noTorque, posOrthDots, negOrthDots, percentLeftRight, fuelToThrust);
                return GetZeroTorquesSprtManyTorque3SprtReturnC(noTorque, posOrthDots, negOrthDots, percentLeftRight, fuelToThrust, centerMass);
            }
            private static List<ThrustSet> GetZeroTorquesSprtManyTorque4(List<ThrustContribution> noTorque, List<ThrustContribution> hasTorque, double fuelToThrust, Point3D centerMass)
            {
                const double MINPERCENT = .005d;



                //TODO: This won't catch all scenarios.  Imagine 3 thrusters forming a Y that has the two tips balanced, but the single part is longer.  The tips
                //will both need to fire at 100% equally, but will individually fail the CanCounterVector2 method
                //
                // So before calling the method one at a time, try with all three, then the three combos of two at a time, etc


                //SortedList<int, List<int>> fullThrusts = new SortedList<int, List<int>>();

                //foreach (int[] combo in AllCombosEnumerator(hasTorque.Count))
                //{






                //}




                List<ThrustSet> retVal = new List<ThrustSet>();

                // I'm going with the assumption that at least one of the thrusters can be fired at 100%.  I can't think of any arrangement
                // that breaks that.

                // See which thrusters can be fired at 100% - in other words, find the weakest link(s)
                List<int> fullThrusts = new List<int>();
                for (int cntr = 0; cntr < hasTorque.Count; cntr++)
                {
                    List<Tuple<Vector3D, Vector3D>> others = new List<Tuple<Vector3D, Vector3D>>();
                    for (int inner = 0; inner < hasTorque.Count; inner++)
                    {
                        if (inner != cntr)
                        {
                            others.Add(new Tuple<Vector3D, Vector3D>(hasTorque[inner].Torque, hasTorque[inner].Torque * MINPERCENT));
                        }
                    }

                    if (CanCounterVector2(hasTorque[cntr].Torque, others))
                    {
                        fullThrusts.Add(cntr);
                    }
                }

                if (fullThrusts.Count == 0)
                {
                    // There are no 100%'s, so exit with no solution
                    return retVal;
                }
                else if (fullThrusts.Count == hasTorque.Count)
                {
                    #region Fire all at 100%

                    List<ThrustSetting> thrusters1 = new List<ThrustSetting>();
                    Vector3D translation1 = new Vector3D();
                    Vector3D torque1 = new Vector3D();
                    double fuelUsed1 = 0d;

                    foreach (ThrustContribution thruster in new List<ThrustContribution>[] { noTorque, hasTorque }.SelectMany(o => o))
                    {
                        thrusters1.Add(new ThrustSetting(thruster.Thruster, thruster.Index, thruster.TranslationForce, thruster.Torque, thruster.TranslationForce.ToUnit(), thruster.Torque.ToUnit(), thruster.TranslationForce, thruster.Torque, 1d));

                        translation1 += thruster.TranslationForce;
                        torque1 += thruster.Torque;
                        fuelUsed1 += thruster.Thruster.ForceAtMax * fuelToThrust;
                    }

                    retVal.Add(new ThrustSet(thrusters1.ToArray(), translation1, torque1, fuelUsed1));

                    #endregion
                    return retVal;
                }




                // Now that the 100%s are found, find the next weakest link, and see what its max value can be

                // Repeat for all the thrusters




                return retVal;
            }
            private static List<ThrustSet> GetZeroTorquesSprtManyTorque5(List<ThrustContribution> noTorque, List<ThrustContribution> hasTorque, double fuelToThrust, Point3D centerMass)
            {
                const double MINPERCENT = .005d;

                //NOTE: There are cases where multiple thrusters will need to fire at 100% (Imagine 3 thrusters forming a Y that has the two
                //tips balanced, but the single part is longer.  The tips will both need to fire at 100% equally)

                // I'm going with the assumption that at least one of the thrusters can be fired at 100%.  I can't think of any arrangement
                // that breaks that.

                #region Find 100% thrusters

                SortedList<int, List<int[]>> fullThrusts = new SortedList<int, List<int[]>>();

                List<int[]> allCombos = new List<int[]>(UtilityCore.AllCombosEnumerator(hasTorque.Count));

                // See which sets of thrusters can be fired at 100% - in other words, find the weakest link(s)
                foreach (int[] combo in UtilityCore.AllCombosEnumerator(hasTorque.Count))
                {
                    List<Vector3D> tests = new List<Vector3D>();
                    List<Tuple<Vector3D, Vector3D>> others = new List<Tuple<Vector3D, Vector3D>>();

                    // Split up the torques
                    for (int cntr = 0; cntr < hasTorque.Count; cntr++)
                    {
                        if (combo.Contains(cntr))
                        {
                            tests.Add(hasTorque[cntr].Torque);
                        }
                        else
                        {
                            others.Add(new Tuple<Vector3D, Vector3D>(hasTorque[cntr].Torque, hasTorque[cntr].Torque * MINPERCENT));
                        }
                    }

                    // See if this combo can fire
                    if (CanCounterVector3(tests, others))
                    {
                        if (!fullThrusts.ContainsKey(combo.Length))
                        {
                            fullThrusts.Add(combo.Length, new List<int[]>());
                        }

                        fullThrusts[combo.Length].Add(combo);
                    }
                }

                #endregion

                List<ThrustSet> retVal = new List<ThrustSet>();

                if (fullThrusts.Count == 0)
                {
                    // There are no 100%'s, so exit with no solution
                    return retVal;
                }
                else if (fullThrusts.ContainsKey(hasTorque.Count))
                {
                    #region Fire all at 100%

                    List<ThrustSetting> thrusters1 = new List<ThrustSetting>();
                    Vector3D translation1 = new Vector3D();
                    Vector3D torque1 = new Vector3D();
                    double fuelUsed1 = 0d;

                    foreach (ThrustContribution thruster in new List<ThrustContribution>[] { noTorque, hasTorque }.SelectMany(o => o))
                    {
                        thrusters1.Add(new ThrustSetting(thruster.Thruster, thruster.Index, thruster.TranslationForce, thruster.Torque, thruster.TranslationForce.ToUnit(), thruster.Torque.ToUnit(), thruster.TranslationForce, thruster.Torque, 1d));

                        translation1 += thruster.TranslationForce;
                        torque1 += thruster.Torque;
                        fuelUsed1 += thruster.Thruster.ForceAtMax * fuelToThrust;
                    }

                    retVal.Add(new ThrustSet(thrusters1.ToArray(), translation1, torque1, fuelUsed1));

                    #endregion
                    return retVal;
                }

                #region Find best 100% set

                // Find the best thrusters to fire at 100% (the ones with the most cumulative translation force?)

                List<Tuple<int[], double>> thrusts100ByLength = new List<Tuple<int[], double>>();

                foreach (int[] temp in fullThrusts.Values.SelectMany(o => o))
                {
                    Vector3D sumTranslation = hasTorque[temp[0]].TranslationForce;
                    for (int cntr = 1; cntr < temp.Length; cntr++)
                    {
                        sumTranslation += hasTorque[temp[0]].TranslationForce;
                    }

                    thrusts100ByLength.Add(new Tuple<int[], double>(temp, sumTranslation.Length));
                }

                int[] thrusts100 = thrusts100ByLength.OrderByDescending(o => o.Item2).First().Item1;

                #endregion

                // Now that the 100%s are found, find the next weakest link, and see what its max value can be
                //FindWeakestLink();


                // Repeat for all the thrusters




                return retVal;
            }

            /// <summary>
            /// This returns true if the other torques can compensate for the test torque (some combination of others plus the test torque
            /// will add to zero)
            /// </summary>
            /// <param name="others">
            /// Item1 = The torque if firing at 100%
            /// Item2 = The torque if firing at the minimum allowed percent
            /// </param>
            private static bool CanCounterVector1(Vector3D test, IEnumerable<Tuple<Vector3D, Vector3D>> others)
            {
                bool foundNegative = false;
                Vector3D sumPositive = test;
                Vector3D sumNegative = new Vector3D(0, 0, 0);

                // Shoot through the other vectors to see if some combination of them can counter the test vector
                foreach (var other in others)
                {
                    double dot = Vector3D.DotProduct(test, other.Item1);
                    if (dot < 0d)
                    {
                        foundNegative = true;
                        sumNegative += other.Item1;		// adding the full contribution
                    }
                    else
                    {
                        sumPositive += other.Item2;		// add the minimum contribution
                    }
                }

                if (!foundNegative)
                {
                    return false;
                }

                //TODO: This is too simplistic, the neg side could be greater, but need to see if there is a balance left to right
                double sumDot = Vector3D.DotProduct(sumPositive, sumNegative);

                return Math.Abs(sumDot) > sumPositive.Length;
            }
            private static bool CanCounterVector2(Vector3D test, IEnumerable<Tuple<Vector3D, Vector3D>> others)
            {
                #region First division

                Vector3D sumPositive = test;
                Vector3D sumNegative = new Vector3D(0, 0, 0);
                List<Vector3D> negatives = new List<Vector3D>();

                // Shoot through the other vectors to see if some combination of them can counter the test vector
                foreach (var other in others)
                {
                    double dot = Vector3D.DotProduct(test, other.Item1);
                    if (dot < 0d)
                    {
                        negatives.Add(other.Item1);
                        sumNegative += other.Item1;
                    }
                    else
                    {
                        sumPositive += other.Item2;
                    }
                }

                if (negatives.Count == 0)
                {
                    // No negative dot products
                    return false;
                }

                double sumDot = Vector3D.DotProduct(sumPositive, sumNegative);
                double sumPosLen = sumPositive.Length;
                if (Math.Abs(sumDot) / sumPosLen < sumPosLen)
                {
                    // The sum of the negatives isn't enough
                    return false;
                }

                #endregion

                #region Get orth

                // Get the vectors that are orthogonal to the sumPositive line
                Vector3D sumOrths = new Vector3D(0, 0, 0);
                List<Vector3D> orths = new List<Vector3D>();

                foreach (Vector3D negative in negatives)
                {
                    Point3D negPoint = negative.ToPoint();
                    Point3D pointAlongLine = Math3D.GetClosestPoint_Line_Point(new Point3D(0, 0, 0), sumPositive, negPoint);
                    Vector3D line = negPoint - pointAlongLine;

                    sumOrths += line;
                    orths.Add(line);
                }

                if (Math3D.IsNearZero(sumOrths))
                {
                    // They balance out
                    return true;
                }

                if (orths.Count == 1)
                {
                    // There's only one negative, and it can't perfectly counter the positive
                    return false;
                }

                // Find negative dots against sumOrths
                foreach (Vector3D orth in orths)
                {
                    double orthDot = Vector3D.DotProduct(sumOrths, orth);
                    if (orthDot < 0d)
                    {
                        return true;
                    }
                }



                #endregion

                return false;
            }
            private static bool CanCounterVector3(IEnumerable<Vector3D> tests, IEnumerable<Tuple<Vector3D, Vector3D>> others)
            {
                #region Get Manditory

                Vector3D sumManditory = new Vector3D(0, 0, 0);

                foreach (Vector3D test in tests)
                {
                    sumManditory += test;
                }

                foreach (var other in others)
                {
                    sumManditory += other.Item2;
                }

                #endregion

                if (IsNearZeroTorque(sumManditory))
                {
                    return true;
                }

                // Shoot through others to see if some combination can get back to the origin
                List<Vector3D> remaining = others.Select(o => o.Item1 - o.Item2).ToList();

                if (remaining.Count == 0)
                {
                    return false;
                }
                else if (remaining.Count == 1)
                {
                    //TODO: Line
                    return false;
                }
                else if (remaining.Count == 2)
                {
                    //TODO: Parallelagram
                    return false;
                }

                // Add up all combos of the remaining points
                List<Point3D> remainExtremes = new List<Point3D>();

                remainExtremes.Add(new Point3D(0, 0, 0));

                foreach (int[] combo in UtilityCore.AllCombosEnumerator(remaining.Count))
                {
                    // Add up the vectors that this combo points to
                    Vector3D extremity = remaining[combo[0]];
                    for (int cntr = 1; cntr < combo.Length; cntr++)
                    {
                        extremity += remaining[combo[cntr]];
                    }

                    remainExtremes.Add(extremity.ToPoint());
                }

                // Build a convex hull out of them
                TriangleIndexed[] hull = null;
                try
                {
                    hull = Math3D.GetConvexHull(remainExtremes.ToArray());
                }
                catch (Exception)
                {
                    hull = null;
                }

                if (hull == null)
                {
                    //TODO: Couldn't build a hull out of the points, so they are probably coplanar/colinear
                    // Could try running them through the quickhull 2D, but they would need to be rotated so that Z drops out
                    return false;
                }

                // See if sumManditory is inside the hull
                if (Math3D.IsInside_Planes(hull, (sumManditory * -1d).ToPoint()))		// negating, because the hull represents all possibilities that can pull it back to zero
                {
                    return true;
                }

                return false;
            }

            private static List<ThrustSet> GetZeroTorquesSprtManyTorque3SprtReturnA(List<ThrustContribution> noTorque, List<Tuple<ThrustContribution, double, Vector3D, double>> posOrthDots, List<Tuple<ThrustContribution, double, Vector3D, double>> negOrthDots, double percentLeftRight, double fuelToThrust)
            {
                List<ThrustSetting> thrusters = new List<ThrustSetting>();
                Vector3D translation = new Vector3D();
                Vector3D torque = new Vector3D();
                double fuelUsed = 0d;

                // Positives
                foreach (var thruster in posOrthDots)
                {
                    double percent = thruster.Item4 * percentLeftRight;
                    thrusters.Add(new ThrustSetting(thruster.Item1.Thruster, thruster.Item1.Index,
                        thruster.Item1.TranslationForce, thruster.Item1.Torque,
                        thruster.Item1.TranslationForce.ToUnit(), thruster.Item1.Torque.ToUnit(),
                        thruster.Item1.TranslationForce * percent, thruster.Item1.Torque * percent,
                        percent));

                    translation += thruster.Item1.TranslationForce * percent;
                    torque += thruster.Item1.Torque * percent;
                    fuelUsed += thruster.Item1.Thruster.ForceAtMax * fuelToThrust * percent;
                }

                // Negatives
                foreach (var thruster in negOrthDots)
                {
                    double percent = thruster.Item4;
                    thrusters.Add(new ThrustSetting(thruster.Item1.Thruster, thruster.Item1.Index,
                        thruster.Item1.TranslationForce, thruster.Item1.Torque,
                        thruster.Item1.TranslationForce.ToUnit(), thruster.Item1.Torque.ToUnit(),
                        thruster.Item1.TranslationForce * percent, thruster.Item1.Torque * percent,
                        percent));

                    translation += thruster.Item1.TranslationForce * percent;
                    torque += thruster.Item1.Torque * percent;
                    fuelUsed += thruster.Item1.Thruster.ForceAtMax * fuelToThrust * percent;
                }

                // Inlines
                foreach (ThrustContribution thruster in noTorque)
                {
                    thrusters.Add(new ThrustSetting(thruster.Thruster, thruster.Index, thruster.TranslationForce, thruster.Torque, thruster.TranslationForce.ToUnit(), thruster.Torque.ToUnit(), thruster.TranslationForce, thruster.Torque, 1d));

                    translation += thruster.TranslationForce;
                    torque += thruster.Torque;
                    fuelUsed += thruster.Thruster.ForceAtMax * fuelToThrust;
                }

                // Exit Function
                List<ThrustSet> retVal = new List<ThrustSet>();
                retVal.Add(new ThrustSet(thrusters.ToArray(), translation, torque, fuelUsed));
                return retVal;
            }
            private static List<ThrustSet> GetZeroTorquesSprtManyTorque3SprtReturnB(List<ThrustContribution> noTorque, List<Tuple<ThrustContribution, double, Vector3D, double>> posOrthDots, List<Tuple<ThrustContribution, double, Vector3D, double>> negOrthDots, double percentLeftRight, double fuelToThrust, Point3D centerMass)
            {
                #region Consolidate candidates

                // First double is front/back %, second is left/right %
                Tuple<ThrustContribution, double, double>[] candidates = new Tuple<ThrustContribution, double, double>[posOrthDots.Count + negOrthDots.Count];

                for (int cntr = 0; cntr < posOrthDots.Count; cntr++)
                {
                    candidates[cntr] = new Tuple<ThrustContribution, double, double>(posOrthDots[cntr].Item1, posOrthDots[cntr].Item4, percentLeftRight);
                }

                for (int cntr = 0; cntr < negOrthDots.Count; cntr++)
                {
                    candidates[posOrthDots.Count + cntr] = new Tuple<ThrustContribution, double, double>(negOrthDots[cntr].Item1, negOrthDots[cntr].Item4, 1d);
                }

                //List<Tuple<ThrustContribution, double, double>> candidates = new List<Tuple<ThrustContribution, double, double>>();

                //foreach (var thruster in posOrthDots)
                //{
                //    candidates.Add(new Tuple<ThrustContribution, double, double>(thruster.Item1, thruster.Item4, percentLeftRight));
                //}

                //foreach (var thruster in negOrthDots)
                //{
                //    candidates.Add(new Tuple<ThrustContribution, double, double>(thruster.Item1, thruster.Item4, 1d));
                //}

                #endregion


                //TODO: Can't just multiply the percents together.  Keep trying random percents to see if a zero torque solution can be found
                //TODO: This is taking too many iterations.  Do 100 iterations, then just keep refining the smallest

                Random rand = StaticRandom.GetRandomForThread();

                //Tuple<double[], Vector3D, double> currentSmallest = null;		//TODO: Instead of 1, keep 100 of the smallest, always sorted, so I can see if I can spot a trend
                //SortedList<double, Tuple<double[], Vector3D, double>> currentSmallest = new SortedList<double, Tuple<double[], Vector3D, double>>();
                SortedList<double, FiringAttempt> currentSmallest = new SortedList<double, FiringAttempt>();
                double[] percents = new double[candidates.Length];
                while (true)
                {
                    #region Generate and test random percents

                    Vector3D sumTorque = new Vector3D(0, 0, 0);

                    for (int cntr = 0; cntr < candidates.Length; cntr++)
                    {
                        //percents[cntr] = rand.NextDouble() * candidates[cntr].Item2 * candidates[cntr].Item3;
                        percents[cntr] = rand.NextDouble();


                        // This is copied from Body.AddForceAtPoint
                        Vector3D offsetFromMass = candidates[cntr].Item1.Thruster.Position - centerMass;		// this is ship's local coords
                        Vector3D force = candidates[cntr].Item1.Thruster.ThrusterDirectionsShip[candidates[cntr].Item1.Index] * candidates[cntr].Item1.Thruster.ForceAtMax * percents[cntr];

                        Vector3D translationForce, testTorque;
                        Math3D.SplitForceIntoTranslationAndTorque(out translationForce, out testTorque, offsetFromMass, force);


                        sumTorque += testTorque;
                    }

                    double length = sumTorque.Length;

                    if (!currentSmallest.Keys.Contains(length) && (currentSmallest.Keys.Count < 100 || length < currentSmallest.Keys[currentSmallest.Keys.Count - 1]))
                    {
                        currentSmallest.Add(length, new FiringAttempt(percents.ToArray(), sumTorque));

                        while (currentSmallest.Keys.Count > 100)
                        {
                            currentSmallest.RemoveAt(currentSmallest.Keys.Count - 1);
                        }
                    }

                    if (rand.NextDouble() < 0d)
                    {
                        string report = ReportDump(candidates, currentSmallest);
                    }

                    if (IsNearZeroTorque(sumTorque))
                    {
                        string report = ReportDump(candidates, currentSmallest);
                        break;
                    }

                    #endregion
                }



                // Now compare these derived percents with the calculated ones to try to figure out what the equation should be






                List<ThrustSetting> thrusters = new List<ThrustSetting>();
                Vector3D translation = new Vector3D();
                Vector3D torque = new Vector3D();
                double fuelUsed = 0d;

                // Torque generating thrusters
                for (int cntr = 0; cntr < candidates.Length; cntr++)
                {
                    thrusters.Add(new ThrustSetting(candidates[cntr].Item1.Thruster, candidates[cntr].Item1.Index,
                        candidates[cntr].Item1.TranslationForce, candidates[cntr].Item1.Torque,
                        candidates[cntr].Item1.TranslationForce.ToUnit(), candidates[cntr].Item1.Torque.ToUnit(),
                        candidates[cntr].Item1.TranslationForce * percents[cntr], candidates[cntr].Item1.Torque * percents[cntr],
                        percents[cntr]));

                    translation += candidates[cntr].Item1.TranslationForce * percents[cntr];
                    torque += candidates[cntr].Item1.Torque * percents[cntr];
                    fuelUsed += candidates[cntr].Item1.Thruster.ForceAtMax * fuelToThrust * percents[cntr];
                }

                // Inlines
                foreach (ThrustContribution thruster in noTorque)
                {
                    thrusters.Add(new ThrustSetting(thruster.Thruster, thruster.Index, thruster.TranslationForce, thruster.Torque, thruster.TranslationForce.ToUnit(), thruster.Torque.ToUnit(), thruster.TranslationForce, thruster.Torque, 1d));

                    translation += thruster.TranslationForce;
                    torque += thruster.Torque;
                    fuelUsed += thruster.Thruster.ForceAtMax * fuelToThrust;
                }

                // Exit Function
                List<ThrustSet> retVal = new List<ThrustSet>();
                retVal.Add(new ThrustSet(thrusters.ToArray(), translation, torque, fuelUsed));
                return retVal;
            }
            private static List<ThrustSet> GetZeroTorquesSprtManyTorque3SprtReturnC(List<ThrustContribution> noTorque, List<Tuple<ThrustContribution, double, Vector3D, double>> posOrthDots, List<Tuple<ThrustContribution, double, Vector3D, double>> negOrthDots, double percentLeftRight, double fuelToThrust, Point3D centerMass)
            {
                #region Consolidate candidates

                // First double is front/back %, second is left/right %
                Tuple<ThrustContribution, double, double>[] candidates = new Tuple<ThrustContribution, double, double>[posOrthDots.Count + negOrthDots.Count];

                for (int cntr = 0; cntr < posOrthDots.Count; cntr++)
                {
                    candidates[cntr] = new Tuple<ThrustContribution, double, double>(posOrthDots[cntr].Item1, posOrthDots[cntr].Item4, percentLeftRight);
                }

                for (int cntr = 0; cntr < negOrthDots.Count; cntr++)
                {
                    candidates[posOrthDots.Count + cntr] = new Tuple<ThrustContribution, double, double>(negOrthDots[cntr].Item1, negOrthDots[cntr].Item4, 1d);
                }

                #endregion

                Random rand = StaticRandom.GetRandomForThread();

                SortedList<double, FiringAttempt> currentSmallest = new SortedList<double, FiringAttempt>();
                int thresholdHitCount = 0;

                while (true)
                {
                    #region Generate and test random percents

                    // Get some random percents to fire the thrusters at
                    var attempt = GeneratePercents2(candidates, rand, centerMass, currentSmallest);

                    // Add to the top 100
                    //TODO: Don't just reward the smallest output torque.  Reward the smallest ratio of input to output torque, only only stop when
                    //the output torque is really small
                    if (attempt.SumTorqueLength < 10d && !currentSmallest.Keys.Contains(attempt.SumTorqueLength) && (currentSmallest.Keys.Count < 100 || attempt.SumTorqueLength < currentSmallest.Keys[currentSmallest.Keys.Count - 1]))
                    {
                        currentSmallest.Add(attempt.SumTorqueLength, attempt);

                        while (currentSmallest.Keys.Count > 100)
                        {
                            currentSmallest.RemoveAt(currentSmallest.Keys.Count - 1);
                        }
                    }

                    if (rand.NextDouble() < 0d)
                    {
                        string report = ReportDump(candidates, currentSmallest);
                    }

                    if (IsNearZeroTorque(attempt.SumTorque))
                    {
                        thresholdHitCount++;
                        if (thresholdHitCount > 100)
                        {
                            string report = ReportDump(candidates, currentSmallest);
                            break;
                        }
                    }

                    #endregion
                }

                double[] percents = currentSmallest[currentSmallest.Keys[0]].PercentsAbsolute;

                List<ThrustSetting> thrusters = new List<ThrustSetting>();
                Vector3D translation = new Vector3D();
                Vector3D torque = new Vector3D();
                double fuelUsed = 0d;

                // Torque generating thrusters
                for (int cntr = 0; cntr < candidates.Length; cntr++)
                {
                    thrusters.Add(new ThrustSetting(candidates[cntr].Item1.Thruster, candidates[cntr].Item1.Index,
                        candidates[cntr].Item1.TranslationForce, candidates[cntr].Item1.Torque,
                        candidates[cntr].Item1.TranslationForce.ToUnit(), candidates[cntr].Item1.Torque.ToUnit(),
                        candidates[cntr].Item1.TranslationForce * percents[cntr], candidates[cntr].Item1.Torque * percents[cntr],
                        percents[cntr]));

                    translation += candidates[cntr].Item1.TranslationForce * percents[cntr];
                    torque += candidates[cntr].Item1.Torque * percents[cntr];
                    fuelUsed += candidates[cntr].Item1.Thruster.ForceAtMax * fuelToThrust * percents[cntr];
                }

                // Inlines
                foreach (ThrustContribution thruster in noTorque)
                {
                    thrusters.Add(new ThrustSetting(thruster.Thruster, thruster.Index, thruster.TranslationForce, thruster.Torque, thruster.TranslationForce.ToUnit(), thruster.Torque.ToUnit(), thruster.TranslationForce, thruster.Torque, 1d));

                    translation += thruster.TranslationForce;
                    torque += thruster.Torque;
                    fuelUsed += thruster.Thruster.ForceAtMax * fuelToThrust;
                }

                // Exit Function
                List<ThrustSet> retVal = new List<ThrustSet>();
                retVal.Add(new ThrustSet(thrusters.ToArray(), translation, torque, fuelUsed));
                return retVal;
            }

            /// <summary>
            /// This comes up with random percents to fire the thrusters at, and calculates what torque that would produce
            /// </summary>
            /// <returns>
            /// double[] = percents the same size as candidates (the percent to fire the corresponding thruster)
            /// Vector3D = the sum of the torque those thrusters produce
            /// double = length of the vector (so .length doesn't have to keep getting called)
            /// </returns>
            private static FiringAttempt GeneratePercents1(Tuple<ThrustContribution, double, double>[] candidates, Random rand, Point3D centerMass, SortedList<double, FiringAttempt> currentSmallest)
            {

                //NOTE: This GeneratePercents1 is only rewarding a near zero sum torque, so the easiest way to do that is to fire the thrusters at
                //near zero percents.  Version 2 needs to try to maximize the percents that the thrusters fire at and still get a near zero output (it's
                //the ratio of thruster firings that's important)


                Vector[] minMaxAbs = new Vector[candidates.Length];		// using X as the min, Y as the max
                Vector[] minMaxScaled = new Vector[candidates.Length];

                if (currentSmallest.Keys.Count > 10)
                {
                    #region Limit using currentSmallest

                    // Init arrays
                    for (int cntr = 0; cntr < candidates.Length; cntr++)
                    {
                        minMaxAbs[cntr] = new Vector(double.MaxValue, double.MinValue);
                        minMaxScaled[cntr] = new Vector(double.MaxValue, double.MinValue);
                    }

                    // Grab the min/max out of currentSmallest
                    foreach (FiringAttempt small in currentSmallest.Values)
                    {
                        for (int cntr = 0; cntr < small.PercentsAbsolute.Length; cntr++)
                        {
                            // Absolute
                            if (small.PercentsAbsolute[cntr] < minMaxAbs[cntr].X)
                            {
                                minMaxAbs[cntr].X = small.PercentsAbsolute[cntr];
                            }

                            if (small.PercentsAbsolute[cntr] > minMaxAbs[cntr].Y)
                            {
                                minMaxAbs[cntr].Y = small.PercentsAbsolute[cntr];
                            }

                            // Scaled
                            if (small.PercentsRelative[cntr] < minMaxScaled[cntr].X)
                            {
                                minMaxScaled[cntr].X = small.PercentsRelative[cntr];
                            }

                            if (small.PercentsRelative[cntr] > minMaxScaled[cntr].Y)
                            {
                                minMaxScaled[cntr].Y = small.PercentsRelative[cntr];
                            }
                        }
                    }

                    // Expand the values
                    for (int cntr = 0; cntr < candidates.Length; cntr++)
                    {
                        // Absolute +- 5%
                        minMaxAbs[cntr].X = minMaxAbs[cntr].X * .95d;
                        minMaxAbs[cntr].Y = minMaxAbs[cntr].Y * 1.05d;
                        if (minMaxAbs[cntr].Y > 1d)
                        {
                            minMaxAbs[cntr].Y = 1d;
                        }

                        // Scaled +- 1%
                        minMaxScaled[cntr].X = minMaxScaled[cntr].X * .99d;
                        minMaxScaled[cntr].Y = minMaxScaled[cntr].Y * 1.01d;
                        if (minMaxScaled[cntr].Y > 1d)
                        {
                            minMaxScaled[cntr].Y = 1d;
                        }
                    }

                    #endregion
                }
                else
                {
                    #region Full range

                    for (int cntr = 0; cntr < candidates.Length; cntr++)
                    {
                        minMaxAbs[cntr] = new Vector(0d, 1d);
                        minMaxScaled[cntr] = new Vector(0d, 1d);
                    }

                    #endregion
                }

                // Figure out which one represents the largest scale
                int maxIndex = 0;
                double maxY = minMaxScaled[0].Y;
                for (int cntr = 1; cntr < candidates.Length; cntr++)
                {
                    if (minMaxScaled[cntr].Y > maxY)
                    {
                        maxIndex = cntr;
                        maxY = minMaxScaled[cntr].Y;
                    }
                }

                Vector3D sumTorque = new Vector3D(0, 0, 0);
                double[] percents = new double[candidates.Length];

                int seedIndex = rand.Next(candidates.Length);
                percents[seedIndex] = UtilityCore.GetScaledValue(minMaxAbs[seedIndex].X, minMaxAbs[seedIndex].Y, 0d, 1d, rand.NextDouble());

                // Come up with the percents
                for (int cntr = 0; cntr < candidates.Length; cntr++)
                {
                    #region Figure out percent

                    //TODO: Use the scaled percents instead, should converge on the answer much sooner
                    //percents[cntr] = UtilityHelper.GetScaledValue(minMaxAbs[cntr].X, minMaxAbs[cntr].Y, 0d, 1d, rand.NextDouble());

                    if (cntr != seedIndex)
                    {
                        // Choose a percent that is a scaled relative to the percent at seed index
                        double? min = null;
                        double? max = null;
                        if (seedIndex == maxIndex)
                        {
                            min = percents[seedIndex] * minMaxScaled[cntr].X;
                            max = percents[seedIndex] * minMaxScaled[cntr].Y;
                        }
                        else if (!Math3D.IsNearZero(percents[seedIndex]))		// can't divide by zero
                        {
                            min = (minMaxScaled[cntr].X * minMaxScaled[maxIndex].X) / percents[seedIndex];
                            max = (minMaxScaled[cntr].Y * minMaxScaled[maxIndex].Y) / percents[seedIndex];
                        }

                        if (min == null)
                        {
                            // Couldn't get a relative range, so just pick a random absolute value
                            percents[cntr] = UtilityCore.GetScaledValue(minMaxAbs[cntr].X, minMaxAbs[cntr].Y, 0d, 1d, rand.NextDouble());
                        }
                        else
                        {
                            percents[cntr] = UtilityCore.GetScaledValue(min.Value, max.Value, 0d, 1d, rand.NextDouble());
                            if (percents[cntr] < minMaxAbs[cntr].X)
                            {
                                percents[cntr] = minMaxAbs[cntr].X;
                            }
                            else if (percents[cntr] > minMaxAbs[cntr].Y)
                            {
                                percents[cntr] = minMaxAbs[cntr].Y;
                            }
                        }
                    }

                    #endregion

                    #region Calculate torque

                    // This is copied from Body.AddForceAtPoint
                    Vector3D offsetFromMass = candidates[cntr].Item1.Thruster.Position - centerMass;		// this is ship's local coords
                    Vector3D force = candidates[cntr].Item1.Thruster.ThrusterDirectionsShip[candidates[cntr].Item1.Index] * candidates[cntr].Item1.Thruster.ForceAtMax * percents[cntr];

                    Vector3D translationForce, testTorque;
                    Math3D.SplitForceIntoTranslationAndTorque(out translationForce, out testTorque, offsetFromMass, force);

                    #endregion

                    sumTorque += testTorque;
                }

                // Exit Function
                return new FiringAttempt(percents, sumTorque);
            }
            private static FiringAttempt GeneratePercents2(Tuple<ThrustContribution, double, double>[] candidates, Random rand, Point3D centerMass, SortedList<double, FiringAttempt> currentSmallest)
            {
                // Look at past successful results to help limit the range of what random percents to pick
                Vector[] minMaxAbs, minMaxScaled;		// using X as the min, Y as the max
                GeneratePercents2SprtExamineHistory(out minMaxAbs, out minMaxScaled, candidates, currentSmallest);

                // Come up with random percents
                double[] percents = GeneratePercents2SprtGenerate(candidates, rand, minMaxAbs, minMaxScaled);
                //Vector3D testTorque = GeneratePercents2SprtTest(candidates, percents, centerMass);		// no need to do this here, it won't be used

                // Using these percents, max out the thrusters and return that torque
                //NOTE: This gets to the solution quickly for three thrusters.  But with 4, things keep bouncing around (one of the thrusters is max, then another.  Once that is
                //sorted out, the other 3 jockey for 2nd, and it takes minutes to try to find a solution)
                double[] maxPercents = FiringAttempt.GetRelativePercents(percents);
                Vector3D torque = GeneratePercents2SprtTest(candidates, maxPercents, centerMass);

                // Exit Function
                return new FiringAttempt(maxPercents, torque);
            }
            private static FiringAttempt GeneratePercents3(Tuple<ThrustContribution, double, double>[] candidates, Random rand, Point3D centerMass, SortedList<double, FiringAttempt> currentSmallest)
            {
                // Look at past successful results to help limit the range of what random percents to pick
                Vector[] minMaxAbs, minMaxScaled;		// using X as the min, Y as the max
                GeneratePercents2SprtExamineHistory(out minMaxAbs, out minMaxScaled, candidates, currentSmallest);

                // Come up with random percents
                double[] percents = GeneratePercents2SprtGenerate(candidates, rand, minMaxAbs, minMaxScaled);
                Vector3D testTorque = GeneratePercents2SprtTest(candidates, percents, centerMass);		// no need to do this here, it won't be used


                // Using these percents, max out the thrusters and return that torque
                //NOTE: This gets to the solution quickly for three thrusters.  But with 4, things keep bouncing around (one of the thrusters is max, then another.  Once that is
                //sorted out, the other 3 jockey for 2nd, and it takes minutes to try to find a solution)
                double[] maxPercents = FiringAttempt.GetRelativePercents(percents);
                Vector3D torque = GeneratePercents2SprtTest(candidates, maxPercents, centerMass);





                // Figure out a way to get the trend that gives a smaller torque


                // Do a few passes with slightly increased percents to see if a smaller output torque can be found
                // Only return the percents with the smallest result


                // Pick a thruster to increment.  Keep incrementing if the torque is reducing.  If it doesn't reduce, then pick a different thruster.  Stop when all thrusters have been tried






                // Exit Function
                return new FiringAttempt(maxPercents, torque);
            }
            private static void GeneratePercents2SprtExamineHistory(out Vector[] minMaxAbs, out Vector[] minMaxScaled, Tuple<ThrustContribution, double, double>[] candidates, SortedList<double, FiringAttempt> currentSmallest)
            {
                const double DIFFPERCENT = .025d;		// increases the range 5% (2.5% each direction)

                minMaxAbs = new Vector[candidates.Length];		// using X as the min, Y as the max
                minMaxScaled = new Vector[candidates.Length];

                if (currentSmallest.Keys.Count > 10)
                {
                    #region Limit using currentSmallest

                    // Init arrays
                    for (int cntr = 0; cntr < candidates.Length; cntr++)
                    {
                        minMaxAbs[cntr] = new Vector(double.MaxValue, double.MinValue);
                        minMaxScaled[cntr] = new Vector(double.MaxValue, double.MinValue);
                    }

                    // Grab the min/max out of currentSmallest
                    foreach (FiringAttempt small in currentSmallest.Values)
                    {
                        for (int cntr = 0; cntr < small.PercentsAbsolute.Length; cntr++)
                        {
                            // Absolute
                            if (small.PercentsAbsolute[cntr] < minMaxAbs[cntr].X)
                            {
                                minMaxAbs[cntr].X = small.PercentsAbsolute[cntr];
                            }

                            if (small.PercentsAbsolute[cntr] > minMaxAbs[cntr].Y)
                            {
                                minMaxAbs[cntr].Y = small.PercentsAbsolute[cntr];
                            }

                            // Scaled
                            if (small.PercentsRelative[cntr] < minMaxScaled[cntr].X)
                            {
                                minMaxScaled[cntr].X = small.PercentsRelative[cntr];
                            }

                            if (small.PercentsRelative[cntr] > minMaxScaled[cntr].Y)
                            {
                                minMaxScaled[cntr].Y = small.PercentsRelative[cntr];
                            }
                        }
                    }

                    // Expand the values
                    for (int cntr = 0; cntr < candidates.Length; cntr++)
                    {
                        // Absolute +- 5%
                        //minMaxAbs[cntr].X = minMaxAbs[cntr].X * .95d;
                        //minMaxAbs[cntr].Y = minMaxAbs[cntr].Y * 1.05d;

                        double diff = (minMaxAbs[cntr].Y - minMaxAbs[cntr].X) * DIFFPERCENT;
                        minMaxAbs[cntr].X = minMaxAbs[cntr].X - diff;
                        minMaxAbs[cntr].Y = minMaxAbs[cntr].Y + diff;

                        if (minMaxAbs[cntr].X < 0d)
                        {
                            minMaxAbs[cntr].X = 0d;
                        }

                        if (minMaxAbs[cntr].Y > 1d)
                        {
                            minMaxAbs[cntr].Y = 1d;
                        }

                        // Scaled +- 1%
                        //minMaxScaled[cntr].X = minMaxScaled[cntr].X * .99d;
                        //minMaxScaled[cntr].Y = minMaxScaled[cntr].Y * 1.01d;

                        diff = (minMaxAbs[cntr].Y - minMaxAbs[cntr].X) * DIFFPERCENT;

                        minMaxScaled[cntr].X = minMaxScaled[cntr].X - diff;
                        minMaxScaled[cntr].Y = minMaxScaled[cntr].Y + diff;

                        if (minMaxScaled[cntr].X < 0d)
                        {
                            minMaxScaled[cntr].X = 0d;
                        }

                        if (minMaxScaled[cntr].Y > 1d)
                        {
                            minMaxScaled[cntr].Y = 1d;
                        }
                    }

                    #endregion
                }
                else
                {
                    #region Full range

                    for (int cntr = 0; cntr < candidates.Length; cntr++)
                    {
                        minMaxAbs[cntr] = new Vector(0d, 1d);
                        minMaxScaled[cntr] = new Vector(0d, 1d);
                    }

                    #endregion
                }
            }
            private static double[] GeneratePercents2SprtGenerate(Tuple<ThrustContribution, double, double>[] candidates, Random rand, Vector[] minMaxAbs, Vector[] minMaxScaled)
            {
                // Figure out which one represents the largest scale
                int maxIndex = 0;
                double maxY = minMaxScaled[0].Y;
                for (int cntr = 1; cntr < candidates.Length; cntr++)
                {
                    if (minMaxScaled[cntr].Y > maxY)
                    {
                        maxIndex = cntr;
                        maxY = minMaxScaled[cntr].Y;
                    }
                }

                Vector3D sumTorque = new Vector3D(0, 0, 0);
                double[] retVal = new double[candidates.Length];

                int seedIndex = rand.Next(candidates.Length);
                retVal[seedIndex] = UtilityCore.GetScaledValue(minMaxAbs[seedIndex].X, minMaxAbs[seedIndex].Y, 0d, 1d, rand.NextDouble());

                // Come up with the percents
                for (int cntr = 0; cntr < candidates.Length; cntr++)
                {
                    #region Figure out percent

                    //TODO: Use the scaled percents instead, should converge on the answer much sooner
                    //percents[cntr] = UtilityHelper.GetScaledValue(minMaxAbs[cntr].X, minMaxAbs[cntr].Y, 0d, 1d, rand.NextDouble());

                    if (cntr != seedIndex)
                    {
                        // Choose a percent that is a scaled relative to the percent at seed index
                        double? min = null;
                        double? max = null;
                        if (seedIndex == maxIndex)
                        {
                            min = retVal[seedIndex] * minMaxScaled[cntr].X;
                            max = retVal[seedIndex] * minMaxScaled[cntr].Y;
                        }
                        else if (!Math3D.IsNearZero(retVal[seedIndex]))		// can't divide by zero
                        {
                            min = (minMaxScaled[cntr].X * minMaxScaled[maxIndex].X) / retVal[seedIndex];
                            max = (minMaxScaled[cntr].Y * minMaxScaled[maxIndex].Y) / retVal[seedIndex];
                        }

                        if (min == null)
                        {
                            // Couldn't get a relative range, so just pick a random absolute value
                            retVal[cntr] = UtilityCore.GetScaledValue(minMaxAbs[cntr].X, minMaxAbs[cntr].Y, 0d, 1d, rand.NextDouble());
                        }
                        else
                        {
                            retVal[cntr] = UtilityCore.GetScaledValue(min.Value, max.Value, 0d, 1d, rand.NextDouble());
                            if (retVal[cntr] < minMaxAbs[cntr].X)
                            {
                                retVal[cntr] = minMaxAbs[cntr].X;
                            }
                            else if (retVal[cntr] > minMaxAbs[cntr].Y)
                            {
                                retVal[cntr] = minMaxAbs[cntr].Y;
                            }
                        }
                    }

                    #endregion
                }

                // Exit Function
                return retVal;
            }
            private static double[] GeneratePercents2SprtIncrease()
            {

                return null;

            }
            private static Vector3D GeneratePercents2SprtTest(Tuple<ThrustContribution, double, double>[] candidates, double[] percents, Point3D centerMass)
            {
                Vector3D retVal = new Vector3D(0, 0, 0);

                for (int cntr = 0; cntr < candidates.Length; cntr++)
                {
                    // Calculate torque (this is copied from Body.AddForceAtPoint)
                    Vector3D offsetFromMass = candidates[cntr].Item1.Thruster.Position - centerMass;		// this is ship's local coords
                    Vector3D force = candidates[cntr].Item1.Thruster.ThrusterDirectionsShip[candidates[cntr].Item1.Index] * candidates[cntr].Item1.Thruster.ForceAtMax * percents[cntr];

                    Vector3D translationForce, testTorque;
                    Math3D.SplitForceIntoTranslationAndTorque(out translationForce, out testTorque, offsetFromMass, force);

                    retVal += testTorque;
                }

                return retVal;
            }

            private static string ReportDump(Tuple<ThrustContribution, double, double>[] candidates, SortedList<double, FiringAttempt> currentSmallest)
            {
                StringBuilder retVal = new StringBuilder();

                // Header
                retVal.Append("Initial Torque\tFrontBack %\tLeftRight %\t\tTorque X\tTorque Y\tTorque Z\t\tLength");
                for (int cntr = 1; cntr <= currentSmallest.First().Value.PercentsAbsolute.Length; cntr++)
                {
                    retVal.Append("\tAbs Percent ");
                    retVal.Append(cntr.ToString());
                }

                retVal.Append("\t");

                for (int cntr = 1; cntr <= currentSmallest.First().Value.PercentsRelative.Length; cntr++)
                {
                    retVal.Append("\tScaled Percent ");
                    retVal.Append(cntr.ToString());
                }

                retVal.AppendLine();

                int index = 0;

                // Rows
                foreach (double key in currentSmallest.Keys)
                {
                    // Thrusters
                    if (index < candidates.Length)
                    {
                        retVal.Append(candidates[index].Item1.Torque.ToString());
                        retVal.Append("\t");
                        retVal.Append(candidates[index].Item2.ToString());
                        retVal.Append("\t");
                        retVal.Append(candidates[index].Item3.ToString());
                        retVal.Append("\t\t");
                    }
                    else
                    {
                        retVal.Append("\t\t\t\t");
                    }

                    index++;

                    var current = currentSmallest[key];

                    // Torque
                    retVal.Append(current.SumTorque.X.ToString());
                    retVal.Append("\t");
                    retVal.Append(current.SumTorque.Y.ToString());
                    retVal.Append("\t");
                    retVal.Append(current.SumTorque.Z.ToString());

                    // Length
                    retVal.Append("\t");
                    retVal.Append("\t");
                    retVal.Append(key.ToString());

                    // Absolute Percents
                    foreach (double percent in current.PercentsAbsolute)
                    {
                        retVal.Append("\t");
                        retVal.Append(percent.ToString());
                    }

                    retVal.Append("\t");

                    // Scaled Percents
                    foreach (double percent in current.PercentsRelative)
                    {
                        retVal.Append("\t");
                        retVal.Append(percent.ToString());
                    }

                    retVal.AppendLine();
                }

                // Exit Function
                return retVal.ToString();
            }

            private static bool CanAddToZero(IEnumerable<Vector3D> vectors)
            {
                bool[] foundPlusMinus = new bool[6];
                foreach (Vector3D vector in vectors)
                {
                    if (vector.X > 0)
                    {
                        foundPlusMinus[0] = true;
                    }
                    else if (vector.X < 0)
                    {
                        foundPlusMinus[1] = true;
                    }

                    if (vector.Y > 0)
                    {
                        foundPlusMinus[2] = true;
                    }
                    else if (vector.Y < 0)
                    {
                        foundPlusMinus[3] = true;
                    }

                    if (vector.Z > 0)
                    {
                        foundPlusMinus[4] = true;
                    }
                    else if (vector.Z < 0)
                    {
                        foundPlusMinus[5] = true;
                    }
                }

                if (foundPlusMinus[0] != foundPlusMinus[1] || foundPlusMinus[2] != foundPlusMinus[3] || foundPlusMinus[4] != foundPlusMinus[5])
                {
                    // Nothing to return
                    return false;
                }

                //if (!foundPlusMinus.All(o => o))
                //{
                //    // Nothing to return
                //    return false;
                //}

                return true;
            }

            private static List<ThrustSet> GetZeroTranslations(ThrustContribution[] used, double fuelToThrust)
            {
                return new List<ThrustSet>();
            }

            private static bool IsNearZeroTorque(Vector3D testVect)
            {
                const double NEARZERO = .05d;

                return Math.Abs(testVect.X) <= NEARZERO && Math.Abs(testVect.Y) <= NEARZERO && Math.Abs(testVect.Z) <= NEARZERO;
            }
            private static bool IsNearValueTorquesDot(double testValue, double compareTo)
            {
                const double NEARZERO = .0005d;

                return testValue >= compareTo - NEARZERO && testValue <= compareTo + NEARZERO;
            }

            private static bool IsNearZeroTranslation(Vector3D testVect)
            {
                const double NEARZERO = .05d;

                return Math.Abs(testVect.X) <= NEARZERO && Math.Abs(testVect.Y) <= NEARZERO && Math.Abs(testVect.Z) <= NEARZERO;
            }
            private static bool IsNearZeroTranslation(double length)
            {
                const double NEARZERO = .05d;

                return Math.Abs(length) <= NEARZERO;
            }

            private static bool IsNearValue(double testValue, double compareTo, double threshold)
            {
                return testValue >= compareTo - threshold && testValue <= compareTo + threshold;
            }

            /// <summary>
            /// This uses UtilityHelper.AllCombosEnumerator, and skips any iterations that contain the illegal pairs
            /// </summary>
            private static IEnumerable<int[]> AllCombosEnumerator(int inputSize, Tuple<int, int>[] illegalPairs)
            {
                foreach (int[] retVal in UtilityCore.AllCombosEnumerator(inputSize))
                {
                    bool isValid = true;

                    for (int illegalCntr = 0; illegalCntr < illegalPairs.Length; illegalCntr++)
                    {
                        bool found1 = false;
                        bool found2 = false;

                        for (int returnCntr = 0; returnCntr < retVal.Length; returnCntr++)
                        {
                            if (retVal[returnCntr] == illegalPairs[illegalCntr].Item1)
                            {
                                found1 = true;
                            }
                            else if (retVal[returnCntr] == illegalPairs[illegalCntr].Item2)
                            {
                                found2 = true;
                            }
                        }

                        if (found1 && found2)
                        {
                            isValid = false;
                            break;
                        }
                    }

                    if (isValid)
                    {
                        yield return retVal;
                    }
                }
            }

            #endregion
            #region Private Methods

            /// <summary>
            /// 
            /// http://www.algebralab.org/lessons/lesson.aspx?file=Algebra_matrix_systems.xml
            /// </summary>
            /// <remarks>
            /// http://www.algebra.com/algebra/homework/coordinate/
            /// 
            /// This is a more robust solver
            /// http://www.bluebit.gr/matrix-calculator/linear_equations.aspx
            /// 
            /// More:
            /// http://www.khanacademy.org/math/algebra/algebra-matrices/v/matrices-to-solve-a-system-of-equations
            /// 
            /// this one has some code that looks like it works
            /// http://social.msdn.microsoft.com/Forums/en/Vsexpressvcs/thread/70408584-668d-49a0-b179-fabf101e71e9
            /// 
            /// this looks like the best bet
            /// http://www.mathdotnet.com/
            /// http://mathnetnumerics.codeplex.com/wikipage?title=Linear%20Algebra&referringTitle=Documentation
            /// </remarks>
            private static double[] SolveForZero(Vector3D[] vectors)
            {
                //if (vectors.Length != 3)
                //{
                //    return null;
                //}






                return null;



            }

            private static double GetScaledDebugTorquePercent(IEnumerable<Vector3D> torques)
            {
                const double RETURNLEN = 7d;

                double maxLength = torques.Max(o => o.Length);

                return RETURNLEN / maxLength;
            }

            #endregion
        }

        #endregion
        #region Class: EnsurePartsNotIntersecting

        private static class EnsurePartsNotIntersecting
        {
            /// <summary>
            /// This will move/rotate parts until they aren't intersecting
            /// </summary>
            /// <remarks>
            /// To save some processing, the final collision hulls are returned (so the caller doesn't have to recreate them)
            /// </remarks>
            public static CollisionHull[] Separate(out CollisionHull.IntersectionPoint[] allIntersections, PartBase[] parts, World world, double ignoreDepthPercent)
            {
                // Get collision hulls for all the parts
                CollisionHull[] retVal = parts.Select(o => o.CreateCollisionHull(world)).ToArray();

                List<CollisionHull.IntersectionPoint> all = new List<CollisionHull.IntersectionPoint>();
                List<int> changedParts = new List<int>();

                while (true)
                {
                    // See which parts are colliding
                    SortedList<int, double> sizes;
                    var intersections = GetIntersections(out sizes, retVal, ignoreDepthPercent);
                    if (intersections.Length == 0)
                    {
                        break;
                    }

                    all.AddRange(intersections.SelectMany(o => o.Item3));

                    DoStep1(parts, retVal, intersections, changedParts);
                    //DoStep2(parts, retVal, intersections, changedParts, sizes);		//Attempt2 is flawed.  Newton is returning collision normals that don't seem to be right.  They are perpendicular to part1's face, but don't seem to make sense with part2

                    //Maybe attempt3 should do some kind of jostling.  Do translations in the direction that attempt1 is using, but choose a couple random rotations to see if the parts can be separated without going the full translation distance (seems very inneficient though)


                    //TODO: Remove this when the intersections start taking in offsets
                    break;
                }

                // Recreate the final hulls for parts that changed
                foreach (int index in changedParts.Distinct())
                {
                    //TODO: Attempt a dispose on the old hull (newton reuses and shares hulls when the values are the same, so it's dangerous to dispose hulls, but not disposing will permanently consume memory)
                    //retVal[index].Dispose();
                    retVal[index] = parts[index].CreateCollisionHull(world);
                }

                // Exit Function
                allIntersections = all.ToArray();
                return retVal;
            }

            /// <summary>
            /// This finds intersections between all the hulls
            /// </summary>
            private static Tuple<int, int, CollisionHull.IntersectionPoint[]>[] GetIntersections(out SortedList<int, double> sizes, CollisionHull[] hulls, double ignoreDepthPercent)
            {
                List<Tuple<int, int, CollisionHull.IntersectionPoint[]>> retVal = new List<Tuple<int, int, CollisionHull.IntersectionPoint[]>>();

                sizes = new SortedList<int, double>();

                // Compare each hull to the others
                for (int outer = 0; outer < hulls.Length - 1; outer++)
                {
                    for (int inner = outer + 1; inner < hulls.Length; inner++)
                    {



                        //TODO: Use the overload that takes offsets
                        CollisionHull.IntersectionPoint[] points = hulls[outer].GetIntersectingPoints_HullToHull(100, hulls[inner], 0);
                        //CollisionHull.IntersectionPoint[] points = hulls[inner].GetIntersectingPoints_HullToHull(100, hulls[outer], 0);		// the normals seem to be the same when colliding from the other direction


                        if (points != null && points.Length > 0)
                        {
                            double sumSize = GetIntersectionsSprtSize(sizes, hulls, outer) + GetIntersectionsSprtSize(sizes, hulls, inner);
                            double minSize = sumSize * ignoreDepthPercent;

                            // Filter out the shallow penetrations
                            //TODO: May need to add the lost distance to the remaining intersections
                            points = points.Where(o => o.PenetrationDistance > minSize).ToArray();

                            if (points != null && points.Length > 0)
                            {
                                retVal.Add(Tuple.Create(outer, inner, points));
                            }
                        }
                    }
                }

                // Exit Function
                return retVal.ToArray();
            }
            private static double GetIntersectionsSprtSize(SortedList<int, double> sizes, CollisionHull[] hulls, int index)
            {
                if (sizes.ContainsKey(index))
                {
                    return sizes[index];
                }

                //NOTE: The returned AABB will be bigger than the actual object
                Point3D min, max;
                hulls[index].CalculateAproximateAABB(out min, out max);

                // Just get the average size of this box
                double size = ((max.X - min.X) + (max.Y - min.Y) + (max.Z - min.Z)) / 3d;

                sizes.Add(index, size);

                return size;
            }

            //Attempt1: Pull straight apart
            private static void DoStep1(PartBase[] parts, CollisionHull[] hulls, Tuple<int, int, CollisionHull.IntersectionPoint[]>[] intersections, List<int> changedParts)
            {
                SortedList<int, List<Tuple<Vector3D?, Quaternion?>>> moves = new SortedList<int, List<Tuple<Vector3D?, Quaternion?>>>();

                // Shoot through all the part pairs
                foreach (var intersection in intersections)
                {
                    double mass1 = parts[intersection.Item1].TotalMass;
                    double mass2 = parts[intersection.Item2].TotalMass;
                    double totalMass = mass1 + mass2;

                    double sumPenetration = intersection.Item3.Sum(o => o.PenetrationDistance);
                    double avgPenetration = sumPenetration / Convert.ToDouble(intersection.Item3.Length);

                    // Shoot through the intersecting points between these two parts
                    foreach (var intersectPoint in intersection.Item3)
                    {
                        // The sum of scaledDistance needs to add up to avgPenetration
                        double percentDistance = intersectPoint.PenetrationDistance / sumPenetration;
                        double scaledDistance = avgPenetration * percentDistance;

                        double distance1 = scaledDistance * ((totalMass - mass1) / totalMass);
                        double distance2 = scaledDistance * ((totalMass - mass2) / totalMass);

                        // Normal is pointing away from the first and toward the second
                        Vector3D normalUnit = intersectPoint.Normal.ToUnit();

                        DoStepSprtAddForce(moves, intersection.Item1, normalUnit * (-1d * distance1), null);
                        DoStepSprtAddForce(moves, intersection.Item2, normalUnit * distance2, null);
                    }
                }

                // Apply the movements
                DoStepSprtMove(parts, moves);

                // Remember which parts were modified (the list will be deduped later)
                changedParts.AddRange(moves.Keys);
            }

            //Attempt2: Include rotation
            private static void DoStep2(PartBase[] parts, CollisionHull[] hulls, Tuple<int, int, CollisionHull.IntersectionPoint[]>[] intersections, List<int> changedParts, SortedList<int, double> sizes)
            {
                SortedList<int, List<Tuple<Vector3D?, Quaternion?>>> moves = new SortedList<int, List<Tuple<Vector3D?, Quaternion?>>>();

                // Shoot through all the part pairs
                foreach (var intersection in intersections)
                {
                    double mass1 = parts[intersection.Item1].TotalMass;
                    double mass2 = parts[intersection.Item2].TotalMass;
                    double totalMass = mass1 + mass2;

                    double sumPenetration = intersection.Item3.Sum(o => o.PenetrationDistance);
                    double avgPenetration = sumPenetration / Convert.ToDouble(intersection.Item3.Length);

                    // Shoot through the intersecting points between these two parts
                    foreach (var intersectPoint in intersection.Item3)
                    {
                        // The sum of scaledDistance needs to add up to avgPenetration
                        double percentDistance = intersectPoint.PenetrationDistance / sumPenetration;
                        double scaledDistance = avgPenetration * percentDistance;

                        double distance1 = scaledDistance * ((totalMass - mass1) / totalMass);
                        double distance2 = scaledDistance * ((totalMass - mass2) / totalMass);


                        // Normal is pointing away from the first and toward the second
                        Vector3D normalUnit = intersectPoint.Normal.ToUnit();

                        Vector3D translation, torque;
                        Math3D.SplitForceIntoTranslationAndTorque(out translation, out torque, intersectPoint.ContactPoint - parts[intersection.Item1].Position, normalUnit * (-1d * distance1));
                        DoStepSprtAddForce(moves, intersection.Item1, translation, DoStep2SprtRotate(torque, sizes[intersection.Item1]));


                        Math3D.SplitForceIntoTranslationAndTorque(out translation, out torque, intersectPoint.ContactPoint - parts[intersection.Item2].Position, normalUnit * distance2);
                        DoStepSprtAddForce(moves, intersection.Item2, translation, DoStep2SprtRotate(torque, sizes[intersection.Item2]));


                    }
                }

                // Apply the movements
                DoStepSprtMove(parts, moves);

                // Remember which parts were modified (the list will be deduped later)
                changedParts.AddRange(moves.Keys);
            }
            private static Quaternion? DoStep2SprtRotate(Vector3D torque, double size)
            {
                const double MAXANGLE = 22.5d;

                if (Math3D.IsNearZero(torque))
                {
                    return null;
                }

                double length = torque.Length;
                Vector3D axis = torque / length;

                // Make the angle to be some proportion between the torque's length and the average size of the part
                //double angle = UtilityHelper.GetScaledValue_Capped(0d, MAXANGLE, 0d, size, length);
                double angle = UtilityCore.GetScaledValue_Capped(0d, MAXANGLE, 0d, size, length * 100);

                return new Quaternion(axis, angle);
            }

            private static void DoStepSprtAddForce(SortedList<int, List<Tuple<Vector3D?, Quaternion?>>> moves, int index, Vector3D? translation, Quaternion? rotation)
            {
                if (!moves.ContainsKey(index))
                {
                    moves.Add(index, new List<Tuple<Vector3D?, Quaternion?>>());
                }

                moves[index].Add(Tuple.Create(translation, rotation));
            }
            private static void DoStepSprtMove(PartBase[] parts, SortedList<int, List<Tuple<Vector3D?, Quaternion?>>> moves)
            {
                foreach (int partIndex in moves.Keys)
                {
                    foreach (var move in moves[partIndex])
                    {
                        if (move.Item1 != null)
                        {
                            parts[partIndex].Position += move.Item1.Value;
                        }

                        if (move.Item2 != null)
                        {
                            parts[partIndex].Orientation = parts[partIndex].Orientation.RotateBy(move.Item2.Value);
                        }
                    }
                }
            }
        }

        #endregion

        #region Declaration Section

        private Random _rand = new Random();

        private bool _isInitialized = false;

        private EditorOptions _editorOptions = new EditorOptions();
        private ItemOptions _itemOptions = new ItemOptions();
        private ItemColors _colors = new ItemColors();

        private Point3D _boundryMin;
        private Point3D _boundryMax;

        private World _world = null;
        private Map _map = null;
        private RadiationField _radiation = null;
        private GravityFieldUniform _gravity = null;

        private MaterialManager _materialManager = null;
        private int _material_Asteroid = -1;
        private int _material_Ship = -1;
        private int _material_Sand = -1;

        private ScreenSpaceLines3D _boundryLines = null;

        /// <summary>
        /// This listens to the mouse/keyboard and controls the camera
        /// </summary>
        /// <remarks>
        /// I'll only create this when debuging
        /// </remarks>
        private TrackBallRoam _trackball = null;

        private List<Visual3D> _currentVisuals = new List<Visual3D>();
        private Body _currentBody = null;
        private Ship _ship = null;
        private ShipDNA _shipDNA = null;		// eventually, ship should expose a method to create a dna.  But for now, just store it here
        private ThrustController _thrustController = null;

        private BalanceVisualizer _balanceVisualizer = null;

        private List<TempBody> _tempBodies = new List<TempBody>();
        private DateTime _lastSandAdd = DateTime.MinValue;

        /// <summary>
        /// When a model is created, this is the direction it starts out as
        /// </summary>
        private DoubleVector _defaultDirectionFacing = new DoubleVector(1, 0, 0, 0, 0, 1);

        #endregion

        #region Constructor

        public ShipPartTesterWindow()
        {
            InitializeComponent();

            _itemOptions.ThrusterStrengthRatio /= 3d;
            _itemOptions.FuelToThrustRatio /= 3d;

            _isInitialized = true;

            trkBalanceCount_ValueChanged(this, null);
        }

        #endregion

        #region Event Listeners

        private void Window_Loaded(object sender, RoutedEventArgs e)
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
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            try
            {
                // Temp Bodies
                foreach (TempBody body in _tempBodies)
                {
                    body.PhysicsBody.Dispose();
                }
                _tempBodies.Clear();

                // Current Body
                if (_currentBody != null)
                {
                    _currentBody.Dispose();
                }
                _currentBody = null;

                //TODO: Ship

                // Map
                if (_map != null)
                {
                    _map.Dispose();		// this will dispose the physics bodies
                    _map = null;
                }

                // World
                if (_world != null)
                {
                    _world.Pause();
                    _world.Dispose();
                    _world = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void World_Updating(object sender, WorldUpdatingArgs e)
        {
            try
            {
                #region Remove temp bodies

                int index = 0;
                while (index < _tempBodies.Count)
                {
                    if (_tempBodies[index].ShouldDie())
                    {
                        foreach (Visual3D visual in _tempBodies[index].PhysicsBody.Visuals)
                        {
                            _viewport.Children.Remove(visual);
                        }

                        _tempBodies[index].PhysicsBody.Dispose();

                        _tempBodies.RemoveAt(index);
                    }
                    else
                    {
                        index++;
                    }
                }

                #endregion

                if (radFallingSand.IsChecked.Value || (radFallingSandPlates.IsChecked.Value && (DateTime.Now - _lastSandAdd).TotalMilliseconds > 5000d))
                {
                    #region Falling Sand

                    bool isPlate = radFallingSandPlates.IsChecked.Value;
                    Color color = UtilityWPF.ColorFromHex("FFF5EE95");
                    Vector3D size = new Vector3D(.05, .05, .05);
                    double maxDist = 1.1 * size.LengthSquared;
                    double mass = trkMass.Value;
                    double radius = 1d;
                    double startHeight = 1d;
                    TimeSpan lifespan = TimeSpan.FromSeconds(15d);
                    Vector3D velocity = new Vector3D(0, 0, -trkSpeed.Value);
                    int numCubes = isPlate ? 150 : 1;

                    // Calculate positions (they must all be calculated up front, or some sand will be built later)
                    List<Vector3D> positions = new List<Vector3D>();
                    for (int cntr = 0; cntr < numCubes; cntr++)
                    {
                        Vector3D startPos;
                        while (true)
                        {
                            startPos = Math3D.GetRandomVector_Circular(radius);

                            if (!positions.Any(o => (o - startPos).LengthSquared < maxDist))
                            {
                                break;
                            }
                        }

                        positions.Add(startPos);
                    }

                    // Place the sand
                    for (int cntr = 0; cntr < numCubes; cntr++)
                    {
                        Vector3D startPos = new Vector3D(positions[cntr].X, positions[cntr].Y, startHeight);
                        Quaternion orientation = isPlate ? Quaternion.Identity : Math3D.GetRandomRotation();

                        Body body = GetNewBody(CollisionShapeType.Box, color, size, mass, startPos.ToPoint(), orientation, _material_Sand);
                        body.Velocity = velocity;
                        body.LinearDamping = 0d;
                        body.AngularDamping = new Vector3D(0d, 0d, 0d);

                        _tempBodies.Add(new TempBody(body, lifespan));
                    }

                    // Remember when this was done
                    _lastSandAdd = DateTime.Now;

                    #endregion
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Ship_ApplyForceAndTorque(object sender, BodyApplyForceAndTorqueArgs e)
        {
            if (_thrustController != null)
            {
                //_thrustController.ApplyForce1(e);
                _thrustController.ApplyForce2(e);
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (_thrustController != null)
            {
                _thrustController.KeyDown(e);
            }
        }
        private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (_thrustController != null)
            {
                _thrustController.KeyUp(e);
            }
        }

        private void Collision_Ship(object sender, MaterialCollisionArgs e)
        {
        }
        private void Collision_Sand(object sender, MaterialCollisionArgs e)
        {
            // When the sand hits the ship, it is staying alive too long and messing up other sand
            Body body = e.GetBody(_material_Sand);

            TempBody tempBody = _tempBodies.FirstOrDefault(o => o.PhysicsBody.Equals(body));
            if (tempBody != null)
            {
                _tempBodies.Remove(tempBody);
                _tempBodies.Add(new TempBody(tempBody.PhysicsBody, TimeSpan.FromSeconds(1)));
            }
        }

        private void btnAmmoBox_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PartDNA dna = GetDefaultDNA(AmmoBox.PARTTYPE);
                AmmoBox ammoBox = new AmmoBox(_editorOptions, _itemOptions, dna);
                ammoBox.RemovalMultiple = ammoBox.QuantityMax * .1d;

                double mass1 = ammoBox.TotalMass;

                double remainder = ammoBox.AddQuantity(.05d, false);
                double mass2 = ammoBox.TotalMass;

                remainder = ammoBox.AddQuantity(500d, false);
                double mass3 = ammoBox.TotalMass;

                double output = ammoBox.RemoveQuantity(.1d, false);
                double mass4 = ammoBox.TotalMass;

                output = ammoBox.RemoveQuantity(ammoBox.RemovalMultiple * 2d, false);
                double mass5 = ammoBox.TotalMass;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnSingleFuel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PartDNA dna = GetDefaultDNA(FuelTank.PARTTYPE);
                FuelTank fuelTank = new FuelTank(_editorOptions, _itemOptions, dna);

                double mass1 = fuelTank.TotalMass;

                double remainder = fuelTank.AddQuantity(.05d, false);
                double mass2 = fuelTank.TotalMass;

                remainder = fuelTank.AddQuantity(500d, false);
                double mass3 = fuelTank.TotalMass;

                double output = fuelTank.RemoveQuantity(.1d, false);
                double mass4 = fuelTank.TotalMass;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnSingleEnergy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PartDNA dna = GetDefaultDNA(EnergyTank.PARTTYPE);
                EnergyTank energyTank = new EnergyTank(_editorOptions, _itemOptions, dna);

                double mass1 = energyTank.TotalMass;

                double remainder = energyTank.AddQuantity(.05d, false);
                double mass2 = energyTank.TotalMass;

                remainder = energyTank.AddQuantity(500d, false);
                double mass3 = energyTank.TotalMass;

                double output = energyTank.RemoveQuantity(.1d, false);
                double mass4 = energyTank.TotalMass;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnMultiEnergy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<EnergyTank> tanks = new List<EnergyTank>();
                ContainerGroup group = new ContainerGroup();
                group.Ownership = ContainerGroup.ContainerOwnershipType.GroupIsSoleOwner;

                for (int cntr = 0; cntr < 3; cntr++)
                {
                    PartDNA dna = GetDefaultDNA(EnergyTank.PARTTYPE);
                    double xy = _rand.NextDouble() * 3d;
                    double z = _rand.NextDouble() * 3d;
                    dna.Scale = new Vector3D(xy, xy, z);

                    EnergyTank energyTank = new EnergyTank(_editorOptions, _itemOptions, dna);

                    tanks.Add(energyTank);
                    group.AddContainer(energyTank);
                }

                #region Test1

                double max = group.QuantityMax;

                double remainder1 = group.AddQuantity(max * .5d, false);
                double remainder2 = group.RemoveQuantity(max * .25d, false);
                double remainder3 = group.RemoveQuantity(max * .33d, true);		// should fail
                double remainder4 = group.AddQuantity(max, false);		// partial add

                group.QuantityCurrent *= .5d;

                group.RemoveContainer(tanks[0], false);
                group.AddContainer(tanks[0]);

                group.RemoveContainer(tanks[0], true);
                group.AddContainer(tanks[0]);

                #endregion

                //TODO: Finish these
                //TODO: Test setting max (with just regular containers)

                #region Test2

                group.QuantityCurrent = 0d;

                group.Ownership = ContainerGroup.ContainerOwnershipType.GroupIsSoleOwner;
                //group.Ownership = ContainerGroup.ContainerOwnershipType.QuantitiesCanChange;

                tanks[0].QuantityCurrent *= .5d;		// this will make sole owner fail, but the second enum will handle it

                group.QuantityCurrent = max * .75d;

                group.RemoveQuantity(max * .33d, false);
                group.AddQuantity(max * .5d, false);

                group.RemoveContainer(tanks[0], true);
                group.AddContainer(tanks[0]);

                #endregion

                #region Test3

                group.OnlyRemoveMultiples = true;
                group.RemovalMultiple = max * .25d;

                group.QuantityCurrent = group.QuantityMax;

                double rem1 = group.RemoveQuantity(max * .1d, false);
                double rem2 = group.RemoveQuantity(max * .1d, true);

                group.QuantityCurrent = group.QuantityMax;

                double rem3 = group.RemoveQuantity(max * .3d, false);
                double rem4 = group.RemoveQuantity(max * .3d, true);

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnEnergyToAmmo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PartDNA dna = GetDefaultDNA(EnergyTank.PARTTYPE);
                EnergyTank energyTank = new EnergyTank(_editorOptions, _itemOptions, dna);

                dna = GetDefaultDNA(AmmoBox.PARTTYPE);
                AmmoBox ammoBox = new AmmoBox(_editorOptions, _itemOptions, dna);

                dna = GetDefaultDNA(ConverterEnergyToAmmo.PARTTYPE);
                ConverterEnergyToAmmo converter = new ConverterEnergyToAmmo(_editorOptions, _itemOptions, dna, energyTank, ammoBox);

                energyTank.QuantityCurrent = energyTank.QuantityMax;

                double mass = converter.DryMass;
                mass = converter.TotalMass;

                converter.Transfer(1d, .5d);
                converter.Transfer(1d, 1d);
                converter.Transfer(1d, .1d);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnEnergyToFuel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PartDNA dna = GetDefaultDNA(EnergyTank.PARTTYPE);
                EnergyTank energyTank = new EnergyTank(_editorOptions, _itemOptions, dna);

                dna = GetDefaultDNA(FuelTank.PARTTYPE);
                FuelTank fuelTank = new FuelTank(_editorOptions, _itemOptions, dna);

                dna = GetDefaultDNA(ConverterEnergyToFuel.PARTTYPE);
                ConverterEnergyToFuel converter = new ConverterEnergyToFuel(_editorOptions, _itemOptions, dna, energyTank, fuelTank);

                energyTank.QuantityCurrent = energyTank.QuantityMax;

                double mass = converter.DryMass;
                mass = converter.TotalMass;

                converter.Transfer(1d, .5d);
                converter.Transfer(1d, 1d);
                converter.Transfer(1d, .1d);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnFuelToEnergy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PartDNA dna = GetDefaultDNA(FuelTank.PARTTYPE);
                FuelTank fuelTank = new FuelTank(_editorOptions, _itemOptions, dna);

                dna = GetDefaultDNA(EnergyTank.PARTTYPE);
                EnergyTank energyTank = new EnergyTank(_editorOptions, _itemOptions, dna);

                dna = GetDefaultDNA(ConverterFuelToEnergy.PARTTYPE);
                ConverterFuelToEnergy converter = new ConverterFuelToEnergy(_editorOptions, _itemOptions, dna, fuelTank, energyTank);

                fuelTank.QuantityCurrent = fuelTank.QuantityMax;

                double mass = converter.DryMass;
                mass = converter.TotalMass;

                converter.Transfer(1d, .5d);
                converter.Transfer(1d, 1d);
                converter.Transfer(1d, .1d);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnSolarPanel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PartDNA dna = GetDefaultDNA(EnergyTank.PARTTYPE);
                EnergyTank energyTank = new EnergyTank(_editorOptions, _itemOptions, dna);

                RadiationField radiation = new RadiationField();
                radiation.AmbientRadiation = 1d;

                ConverterRadiationToEnergyDNA dna2 = new ConverterRadiationToEnergyDNA()
                {
                    PartType = ConverterRadiationToEnergy.PARTTYPE,
                    Shape = UtilityCore.GetRandomEnum<SolarPanelShape>(),
                    Position = new Point3D(0, 0, 0),
                    Orientation = Quaternion.Identity,
                    Scale = new Vector3D(1, 1, 1)
                };
                ConverterRadiationToEnergy solar = new ConverterRadiationToEnergy(_editorOptions, _itemOptions, dna2, energyTank, radiation);

                solar.Transfer(1d, Transform3D.Identity);
                solar.Transfer(1d, Transform3D.Identity);
                solar.Transfer(1d, Transform3D.Identity);
                solar.Transfer(1d, Transform3D.Identity);
                solar.Transfer(1d, Transform3D.Identity);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnThruster_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PartDNA dna = GetDefaultDNA(FuelTank.PARTTYPE);
                FuelTank fuelTank = new FuelTank(_editorOptions, _itemOptions, dna);

                ThrusterDNA dna2 = new ThrusterDNA()
                {
                    PartType = Thruster.PARTTYPE,
                    ThrusterType = UtilityCore.GetRandomEnum<ThrusterType>(),
                    Position = new Point3D(0, 0, 0),
                    Orientation = Quaternion.Identity,
                    Scale = new Vector3D(1, 1, 1)
                };
                Thruster thruster = new Thruster(_editorOptions, _itemOptions, dna2, fuelTank);

                fuelTank.QuantityCurrent = fuelTank.QuantityMax;

                double percent;
                Vector3D? thrust;

                do
                {
                    percent = 1d;
                    thrust = thruster.Fire(ref percent, 0, 1d);
                } while (fuelTank.QuantityCurrent > 0d);

                percent = 1d;
                thrust = thruster.Fire(ref percent, 0, 1d);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnStandaloneAmmoEmpty_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PartDNA dna = GetDefaultDNA(AmmoBox.PARTTYPE);
                ModifyDNA(dna, chkStandaloneRandSize.IsChecked.Value, chkStandaloneRandOrientation.IsChecked.Value);

                AmmoBox ammoBox = new AmmoBox(_editorOptions, _itemOptions, dna);
                ammoBox.RemovalMultiple = ammoBox.QuantityMax * .1d;

                BuildStandalonePart(ammoBox);

                if (chkStandaloneShowMassBreakdown.IsChecked.Value)
                {
                    double cellSize = Math3D.Max(dna.Scale.X, dna.Scale.Y, dna.Scale.Z) * UtilityCore.GetScaledValue_Capped(.1d, .3d, 0d, 1d, _rand.NextDouble());
                    DrawMassBreakdown(ammoBox.GetMassBreakdown(cellSize), cellSize);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnStandaloneAmmoFull_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PartDNA dna = GetDefaultDNA(AmmoBox.PARTTYPE);
                ModifyDNA(dna, chkStandaloneRandSize.IsChecked.Value, chkStandaloneRandOrientation.IsChecked.Value);

                AmmoBox ammoBox = new AmmoBox(_editorOptions, _itemOptions, dna);
                ammoBox.RemovalMultiple = ammoBox.QuantityMax * .1d;
                ammoBox.QuantityCurrent = ammoBox.QuantityMax;

                BuildStandalonePart(ammoBox);

                if (chkStandaloneShowMassBreakdown.IsChecked.Value)
                {
                    double cellSize = Math3D.Max(dna.Scale.X, dna.Scale.Y, dna.Scale.Z) * UtilityCore.GetScaledValue_Capped(.1d, .3d, 0d, 1d, _rand.NextDouble());
                    DrawMassBreakdown(ammoBox.GetMassBreakdown(cellSize), cellSize);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnStandaloneFuelEmpty_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PartDNA dna = GetDefaultDNA(FuelTank.PARTTYPE);
                ModifyDNA(dna, chkStandaloneRandSize.IsChecked.Value, chkStandaloneRandOrientation.IsChecked.Value);
                double radius = (dna.Scale.X + dna.Scale.Y) * .5d;
                dna.Scale = new Vector3D(radius, radius, dna.Scale.Z);

                FuelTank fuelTank = new FuelTank(_editorOptions, _itemOptions, dna);

                BuildStandalonePart(fuelTank);

                if (chkStandaloneShowMassBreakdown.IsChecked.Value)
                {
                    double cellSize = Math3D.Max(dna.Scale.X, dna.Scale.Y, dna.Scale.Z) * UtilityCore.GetScaledValue_Capped(.1d, .3d, 0d, 1d, _rand.NextDouble());
                    DrawMassBreakdown(fuelTank.GetMassBreakdown(cellSize), cellSize);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnStandaloneFuelFull_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PartDNA dna = GetDefaultDNA(FuelTank.PARTTYPE);
                ModifyDNA(dna, chkStandaloneRandSize.IsChecked.Value, chkStandaloneRandOrientation.IsChecked.Value);
                double radius = (dna.Scale.X + dna.Scale.Y) * .5d;
                dna.Scale = new Vector3D(radius, radius, dna.Scale.Z);

                //dna.Scale = new Vector3D(2, 2, .9);
                //dna.Scale = new Vector3D(1.15, 1.15, 4);

                FuelTank fuelTank = new FuelTank(_editorOptions, _itemOptions, dna);
                fuelTank.QuantityCurrent = fuelTank.QuantityMax;

                BuildStandalonePart(fuelTank);

                if (chkStandaloneShowMassBreakdown.IsChecked.Value)
                {
                    double cellSize = Math3D.Max(dna.Scale.X, dna.Scale.Y, dna.Scale.Z) * UtilityCore.GetScaledValue_Capped(.1d, .3d, 0d, 1d, _rand.NextDouble());
                    DrawMassBreakdown(fuelTank.GetMassBreakdown(cellSize), cellSize);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnStandaloneEnergy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PartDNA dna = GetDefaultDNA(EnergyTank.PARTTYPE);
                ModifyDNA(dna, chkStandaloneRandSize.IsChecked.Value, chkStandaloneRandOrientation.IsChecked.Value);
                double radius = (dna.Scale.X + dna.Scale.Y) * .5d;
                dna.Scale = new Vector3D(radius, radius, dna.Scale.Z);

                EnergyTank energyTank = new EnergyTank(_editorOptions, _itemOptions, dna);

                BuildStandalonePart(energyTank);

                if (chkStandaloneShowMassBreakdown.IsChecked.Value)
                {
                    double cellSize = Math3D.Max(dna.Scale.X, dna.Scale.Y, dna.Scale.Z) * UtilityCore.GetScaledValue_Capped(.1d, .3d, 0d, 1d, _rand.NextDouble());
                    DrawMassBreakdown(energyTank.GetMassBreakdown(cellSize), cellSize);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnStandaloneEnergyToAmmo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PartDNA dna = GetDefaultDNA(ConverterEnergyToAmmo.PARTTYPE);
                ModifyDNA(dna, chkStandaloneRandSize.IsChecked.Value, chkStandaloneRandOrientation.IsChecked.Value);
                double size = (dna.Scale.X + dna.Scale.Y + dna.Scale.Z) / 3d;
                dna.Scale = new Vector3D(size, size, size);

                ConverterEnergyToAmmo converter = new ConverterEnergyToAmmo(_editorOptions, _itemOptions, dna, null, null);

                BuildStandalonePart(converter);

                if (chkStandaloneShowMassBreakdown.IsChecked.Value)
                {
                    double cellSize = Math3D.Max(dna.Scale.X, dna.Scale.Y, dna.Scale.Z) * UtilityCore.GetScaledValue_Capped(.1d, .3d, 0d, 1d, _rand.NextDouble());
                    DrawMassBreakdown(converter.GetMassBreakdown(cellSize), cellSize);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnStandaloneEnergyToFuel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PartDNA dna = GetDefaultDNA(ConverterEnergyToFuel.PARTTYPE);
                ModifyDNA(dna, chkStandaloneRandSize.IsChecked.Value, chkStandaloneRandOrientation.IsChecked.Value);
                double size = (dna.Scale.X + dna.Scale.Y + dna.Scale.Z) / 3d;
                dna.Scale = new Vector3D(size, size, size);

                ConverterEnergyToFuel converter = new ConverterEnergyToFuel(_editorOptions, _itemOptions, dna, null, null);

                BuildStandalonePart(converter);

                if (chkStandaloneShowMassBreakdown.IsChecked.Value)
                {
                    double cellSize = Math3D.Max(dna.Scale.X, dna.Scale.Y, dna.Scale.Z) * UtilityCore.GetScaledValue_Capped(.1d, .3d, 0d, 1d, _rand.NextDouble());
                    DrawMassBreakdown(converter.GetMassBreakdown(cellSize), cellSize);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnStandaloneFuelToEnergy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PartDNA dna = GetDefaultDNA(ConverterFuelToEnergy.PARTTYPE);
                ModifyDNA(dna, chkStandaloneRandSize.IsChecked.Value, chkStandaloneRandOrientation.IsChecked.Value);

                // It's inacurate to comment this out, but it tests the collision hull better
                double size = (dna.Scale.X + dna.Scale.Y + dna.Scale.Z) / 3d;
                dna.Scale = new Vector3D(size, size, size);

                ConverterFuelToEnergy converter = new ConverterFuelToEnergy(_editorOptions, _itemOptions, dna, null, null);

                BuildStandalonePart(converter);

                if (chkStandaloneShowMassBreakdown.IsChecked.Value)
                {
                    double cellSize = Math3D.Max(dna.Scale.X, dna.Scale.Y, dna.Scale.Z) * UtilityCore.GetScaledValue_Capped(.1d, .3d, 0d, 1d, _rand.NextDouble());
                    DrawMassBreakdown(converter.GetMassBreakdown(cellSize), cellSize);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnStandaloneSolarPanel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RadiationField radiation = new RadiationField();
                radiation.AmbientRadiation = 1d;

                ConverterRadiationToEnergyDNA dna = new ConverterRadiationToEnergyDNA()
                {
                    PartType = ConverterRadiationToEnergy.PARTTYPE,
                    Shape = UtilityCore.GetRandomEnum<SolarPanelShape>(),
                    Position = new Point3D(0, 0, 0),
                    Orientation = Quaternion.Identity,
                    Scale = new Vector3D(1, 1, 1)
                };
                ModifyDNA(dna, chkStandaloneRandSize.IsChecked.Value, chkStandaloneRandOrientation.IsChecked.Value);

                ConverterRadiationToEnergy solar = new ConverterRadiationToEnergy(_editorOptions, _itemOptions, dna, null, radiation);

                BuildStandalonePart(solar);

                if (chkStandaloneShowMassBreakdown.IsChecked.Value)
                {
                    double cellSize = Math3D.Max(dna.Scale.X, dna.Scale.Y, dna.Scale.Z) * UtilityCore.GetScaledValue_Capped(.1d, .3d, 0d, 1d, _rand.NextDouble());
                    DrawMassBreakdown(solar.GetMassBreakdown(cellSize), cellSize);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnStandaloneThruster_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ThrusterDNA dna = new ThrusterDNA()
                {
                    PartType = Thruster.PARTTYPE,
                    ThrusterType = UtilityCore.GetRandomEnum<ThrusterType>(ThrusterType.Custom),
                    Position = new Point3D(0, 0, 0),
                    Orientation = Quaternion.Identity,
                    Scale = new Vector3D(1, 1, 1)
                };
                ModifyDNA(dna, chkStandaloneRandSize.IsChecked.Value, chkStandaloneRandOrientation.IsChecked.Value);
                double size = (dna.Scale.X + dna.Scale.Y + dna.Scale.Z) / 3d;
                dna.Scale = new Vector3D(size, size, size);

                Thruster thruster = new Thruster(_editorOptions, _itemOptions, dna, null);

                BuildStandalonePart(thruster);

                if (chkStandaloneShowMassBreakdown.IsChecked.Value)
                {
                    double cellSize = Math3D.Max(dna.Scale.X, dna.Scale.Y, dna.Scale.Z) * UtilityCore.GetScaledValue_Capped(.1d, .3d, 0d, 1d, _rand.NextDouble());
                    DrawMassBreakdown(thruster.GetMassBreakdown(cellSize), cellSize);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnShipBasic_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureWorldStarted();
                ClearCurrent();

                List<PartDNA> parts = new List<PartDNA>();
                parts.Add(new PartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(-.75, 0, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1) });
                parts.Add(new PartDNA() { PartType = EnergyTank.PARTTYPE, Position = new Point3D(.75, 0, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1) });

                ShipDNA shipDNA = ShipDNA.Create(parts);

                Ship ship = await Ship.GetNewShipAsync(_editorOptions, _itemOptions, shipDNA, _world, _material_Ship, _material_Ship, _radiation, _gravity, null, _map, false, false);

                ClearCurrent();

                _shipDNA = shipDNA;
                _ship = ship;

                _map.AddItem(_ship);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void btnSimplestFlyer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureWorldStarted();
                ClearCurrent();

                List<PartDNA> parts = new List<PartDNA>();
                parts.Add(new PartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(0, 0, .5), Orientation = Quaternion.Identity, Scale = new Vector3D(2, 2, .9) });
                parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(0, 0, -.5), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.One), ThrusterType = ThrusterType.One });

                ShipDNA shipDNA = ShipDNA.Create(parts);

                Ship ship = await Ship.GetNewShipAsync(_editorOptions, _itemOptions, shipDNA, _world, _material_Ship, _material_Ship, _radiation, _gravity, null, _map, false, false);

                ClearCurrent();

                _shipDNA = shipDNA;
                _ship = ship;

                _ship.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(Ship_ApplyForceAndTorque);

                _thrustController = new ThrustController(_ship, _viewport, _itemOptions);

                _ship.Fuel.QuantityCurrent = _ship.Fuel.QuantityMax;
                _ship.RecalculateMass();
                _thrustController.MassChanged(chkShipSimple.IsChecked.Value);

                if (chkShipDebugVisuals.IsChecked.Value)
                {
                    _thrustController.DrawDebugVisuals_Pre();
                }

                _map.AddItem(_ship);

                grdViewPort.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void btnShip3Thrust_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureWorldStarted();
                ClearCurrent();

                List<PartDNA> parts = new List<PartDNA>();
                parts.Add(new PartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(0, 0, -1), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, .65) });

                parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(1.3, 0, 2), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.One), ThrusterType = ThrusterType.One });
                parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-0.65, 1.125833025, 2), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.One), ThrusterType = ThrusterType.One });
                parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-0.65, -1.125833025, 2), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.One), ThrusterType = ThrusterType.One });

                ShipDNA shipDNA = ShipDNA.Create(parts);

                Ship ship = await Ship.GetNewShipAsync(_editorOptions, _itemOptions, shipDNA, _world, _material_Ship, _material_Ship, _radiation, _gravity, null, _map, false, false);

                ClearCurrent();

                _shipDNA = shipDNA;
                _ship = ship;

                _ship.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(Ship_ApplyForceAndTorque);

                _thrustController = new ThrustController(_ship, _viewport, _itemOptions);

                _ship.Fuel.QuantityCurrent = _ship.Fuel.QuantityMax;
                _ship.RecalculateMass();
                _thrustController.MassChanged(chkShipSimple.IsChecked.Value);

                if (chkShipDebugVisuals.IsChecked.Value)
                {
                    _thrustController.DrawDebugVisuals_Pre();
                }

                _map.AddItem(_ship);

                grdViewPort.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void btnShip3ThrustY_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureWorldStarted();
                ClearCurrent();

                List<PartDNA> parts = new List<PartDNA>();
                parts.Add(new PartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(0, 0, -1), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, .65) });

                parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(3, 0, 2), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.One), ThrusterType = ThrusterType.One });
                parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-0.65, .75, 2), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.One), ThrusterType = ThrusterType.One });
                parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-0.65, -.75, 2), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.One), ThrusterType = ThrusterType.One });

                ShipDNA shipDNA = ShipDNA.Create(parts);

                Ship ship = await Ship.GetNewShipAsync(_editorOptions, _itemOptions, shipDNA, _world, _material_Ship, _material_Ship, _radiation, _gravity, null, _map, false, false);

                ClearCurrent();

                _shipDNA = shipDNA;
                _ship = ship;

                _ship.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(Ship_ApplyForceAndTorque);

                _thrustController = new ThrustController(_ship, _viewport, _itemOptions);

                _ship.Fuel.QuantityCurrent = _ship.Fuel.QuantityMax;
                _ship.RecalculateMass();
                _thrustController.MassChanged(chkShipSimple.IsChecked.Value);

                if (chkShipDebugVisuals.IsChecked.Value)
                {
                    _thrustController.DrawDebugVisuals_Pre();
                }

                _map.AddItem(_ship);

                grdViewPort.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void btnShip3ThrustRand_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureWorldStarted();
                ClearCurrent();

                List<PartDNA> parts = new List<PartDNA>();
                parts.Add(new PartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(0, 0, -1), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, .65) });

                parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = Math3D.GetRandomVector_Circular_Shell(1.3).ToPoint(), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.One), ThrusterType = ThrusterType.One });
                parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = Math3D.GetRandomVector_Circular_Shell(1.3).ToPoint(), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.One), ThrusterType = ThrusterType.One });
                parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = Math3D.GetRandomVector_Circular_Shell(1.3).ToPoint(), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.One), ThrusterType = ThrusterType.One });

                ShipDNA shipDNA = ShipDNA.Create(parts);

                Ship ship = await Ship.GetNewShipAsync(_editorOptions, _itemOptions, shipDNA, _world, _material_Ship, _material_Ship, _radiation, _gravity, null, _map, false, false);

                ClearCurrent();

                _shipDNA = shipDNA;
                _ship = ship;

                _ship.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(Ship_ApplyForceAndTorque);

                _thrustController = new ThrustController(_ship, _viewport, _itemOptions);

                _ship.Fuel.QuantityCurrent = _ship.Fuel.QuantityMax;
                _ship.RecalculateMass();
                _thrustController.MassChanged(chkShipSimple.IsChecked.Value);

                if (chkShipDebugVisuals.IsChecked.Value)
                {
                    _thrustController.DrawDebugVisuals_Pre();
                }

                _map.AddItem(_ship);

                grdViewPort.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void btnShip4ThrustRand_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureWorldStarted();
                ClearCurrent();

                List<PartDNA> parts = new List<PartDNA>();
                parts.Add(new PartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(0, 0, -1), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, .65) });

                parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = Math3D.GetRandomVector_Circular_Shell(1.3).ToPoint(), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.One), ThrusterType = ThrusterType.One });
                parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = Math3D.GetRandomVector_Circular_Shell(1.3).ToPoint(), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.One), ThrusterType = ThrusterType.One });
                parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = Math3D.GetRandomVector_Circular_Shell(1.3).ToPoint(), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.One), ThrusterType = ThrusterType.One });
                parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = Math3D.GetRandomVector_Circular_Shell(1.3).ToPoint(), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.One), ThrusterType = ThrusterType.One });

                ShipDNA shipDNA = ShipDNA.Create(parts);

                Ship ship = await Ship.GetNewShipAsync(_editorOptions, _itemOptions, shipDNA, _world, _material_Ship, _material_Ship, _radiation, _gravity, null, _map, false, false);

                ClearCurrent();

                _shipDNA = shipDNA;
                _ship = ship;

                _ship.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(Ship_ApplyForceAndTorque);

                _thrustController = new ThrustController(_ship, _viewport, _itemOptions);

                _ship.Fuel.QuantityCurrent = _ship.Fuel.QuantityMax;
                _ship.RecalculateMass();
                _thrustController.MassChanged(chkShipSimple.IsChecked.Value);

                if (chkShipDebugVisuals.IsChecked.Value)
                {
                    _thrustController.DrawDebugVisuals_Pre();
                }

                _map.AddItem(_ship);

                grdViewPort.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void btnShip4ThrustRand3D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureWorldStarted();
                ClearCurrent();

                List<PartDNA> parts = new List<PartDNA>();
                parts.Add(new PartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(0, 0, -1), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, .65) });

                Vector3D referenceVect = new Vector3D(0, 0, 1);

                parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = Math3D.GetRandomVector_Circular_Shell(1.3).ToPoint(), Orientation = Math3D.GetRotation(referenceVect, referenceVect + Math3D.GetRandomVector_Circular(.3d)), Scale = new Vector3D(1, 1, 1), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.One), ThrusterType = ThrusterType.One });
                parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = Math3D.GetRandomVector_Circular_Shell(1.3).ToPoint(), Orientation = Math3D.GetRotation(referenceVect, referenceVect + Math3D.GetRandomVector_Circular(.3d)), Scale = new Vector3D(1, 1, 1), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.One), ThrusterType = ThrusterType.One });
                parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = Math3D.GetRandomVector_Circular_Shell(1.3).ToPoint(), Orientation = Math3D.GetRotation(referenceVect, referenceVect + Math3D.GetRandomVector_Circular(.3d)), Scale = new Vector3D(1, 1, 1), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.One), ThrusterType = ThrusterType.One });
                parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = Math3D.GetRandomVector_Circular_Shell(1.3).ToPoint(), Orientation = Math3D.GetRotation(referenceVect, referenceVect + Math3D.GetRandomVector_Circular(.3d)), Scale = new Vector3D(1, 1, 1), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.One), ThrusterType = ThrusterType.One });

                ShipDNA shipDNA = ShipDNA.Create(parts);

                Ship ship = await Ship.GetNewShipAsync(_editorOptions, _itemOptions, shipDNA, _world, _material_Ship, _material_Ship, _radiation, _gravity, null, _map, false, false);

                ClearCurrent();

                _shipDNA = shipDNA;
                _ship = ship;

                _ship.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(Ship_ApplyForceAndTorque);

                _thrustController = new ThrustController(_ship, _viewport, _itemOptions);

                _ship.Fuel.QuantityCurrent = _ship.Fuel.QuantityMax;
                _ship.RecalculateMass();
                _thrustController.MassChanged(chkShipSimple.IsChecked.Value);

                if (chkShipDebugVisuals.IsChecked.Value)
                {
                    _thrustController.DrawDebugVisuals_Pre();
                }

                _map.AddItem(_ship);

                grdViewPort.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void btnShipSimpleFlyer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureWorldStarted();
                ClearCurrent();

                List<PartDNA> parts = new List<PartDNA>();
                parts.Add(new PartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(0, 0, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(3, 3, 1) });

                //parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(1, 0, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(.5d, .5d, .5d), ThrusterType = ThrusterType.Two });
                //parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-1, 0, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(.5d, .5d, .5d), ThrusterType = ThrusterType.Two });
                //parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(0, 1, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(.5d, .5d, .5d), ThrusterType = ThrusterType.Two });
                //parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(0, -1, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(.5d, .5d, .5d), ThrusterType = ThrusterType.Two });

                //parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(1, 0, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(.5d, .5d, .5d), ThrusterType = ThrusterType.One });
                //parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-1, 0, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(.5d, .5d, .5d), ThrusterType = ThrusterType.One });
                //parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(0, 1, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(.5d, .5d, .5d), ThrusterType = ThrusterType.One });
                //parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(0, -1, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(.5d, .5d, .5d), ThrusterType = ThrusterType.One });

                parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(1, 0, 0), Orientation = new Quaternion(new Vector3D(0, 0, 1), 90d), Scale = new Vector3D(.5d, .5d, .5d), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two_Two_One), ThrusterType = ThrusterType.Two_Two_One });
                parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-1, 0, 0), Orientation = new Quaternion(new Vector3D(0, 0, 1), 270d), Scale = new Vector3D(.5d, .5d, .5d), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two_Two_One), ThrusterType = ThrusterType.Two_Two_One });
                parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(0, 1, 0), Orientation = new Quaternion(new Vector3D(0, 0, 1), 180d), Scale = new Vector3D(.5d, .5d, .5d), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two_Two_One), ThrusterType = ThrusterType.Two_Two_One });
                parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(0, -1, 0), Orientation = new Quaternion(new Vector3D(0, 0, 1), 0d), Scale = new Vector3D(.5d, .5d, .5d), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two_Two_One), ThrusterType = ThrusterType.Two_Two_One });

                ShipDNA shipDNA = ShipDNA.Create(parts);

                Ship ship = await Ship.GetNewShipAsync(_editorOptions, _itemOptions, shipDNA, _world, _material_Ship, _material_Ship, _radiation, _gravity, null, _map, false, false);

                ClearCurrent();

                _shipDNA = shipDNA;
                _ship = ship;

                _ship.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(Ship_ApplyForceAndTorque);

                _thrustController = new ThrustController(_ship, _viewport, _itemOptions);

                //double mass = _ship.PhysicsBody.Mass;

                _ship.Fuel.QuantityCurrent = _ship.Fuel.QuantityMax;
                _ship.RecalculateMass();
                _thrustController.MassChanged(chkShipSimple.IsChecked.Value);

                if (chkShipDebugVisuals.IsChecked.Value)
                {
                    _thrustController.DrawDebugVisuals_Pre();
                }

                //mass = _ship.PhysicsBody.Mass;

                _map.AddItem(_ship);

                grdViewPort.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void btnShipWackyFlyer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureWorldStarted();
                ClearCurrent();

                List<PartDNA> parts = new List<PartDNA>();
                parts.Add(new PartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(0, 0, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(3, 3, 1) });

                ThrusterType thrustType = UtilityCore.GetRandomEnum<ThrusterType>(ThrusterType.Custom);
                parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(1, 0, 0), Orientation = Math3D.GetRandomRotation(), Scale = new Vector3D(.5d, .5d, .5d), ThrusterDirections = ThrusterDesign.GetThrusterDirections(thrustType), ThrusterType = thrustType });

                thrustType = UtilityCore.GetRandomEnum<ThrusterType>(ThrusterType.Custom);
                parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-1, 0, 0), Orientation = Math3D.GetRandomRotation(), Scale = new Vector3D(.5d, .5d, .5d), ThrusterDirections = ThrusterDesign.GetThrusterDirections(thrustType), ThrusterType = thrustType });

                thrustType = UtilityCore.GetRandomEnum<ThrusterType>(ThrusterType.Custom);
                parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(0, 1, 0), Orientation = Math3D.GetRandomRotation(), Scale = new Vector3D(.5d, .5d, .5d), ThrusterDirections = ThrusterDesign.GetThrusterDirections(thrustType), ThrusterType = thrustType });

                thrustType = UtilityCore.GetRandomEnum<ThrusterType>(ThrusterType.Custom);
                parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(0, -1, 0), Orientation = Math3D.GetRandomRotation(), Scale = new Vector3D(.5d, .5d, .5d), ThrusterDirections = ThrusterDesign.GetThrusterDirections(thrustType), ThrusterType = thrustType });

                ShipDNA shipDNA = ShipDNA.Create(parts);

                Ship ship = await Ship.GetNewShipAsync(_editorOptions, _itemOptions, shipDNA, _world, _material_Ship, _material_Ship, _radiation, _gravity, null, _map, false, false);

                ClearCurrent();

                _shipDNA = shipDNA;
                _ship = ship;

                _ship.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(Ship_ApplyForceAndTorque);

                _thrustController = new ThrustController(_ship, _viewport, _itemOptions);

                //double mass = _ship.PhysicsBody.Mass;

                _ship.Fuel.QuantityCurrent = _ship.Fuel.QuantityMax;
                _ship.RecalculateMass();
                _thrustController.MassChanged(chkShipSimple.IsChecked.Value);

                if (chkShipDebugVisuals.IsChecked.Value)
                {
                    _thrustController.DrawDebugVisuals_Pre();
                }

                //mass = _ship.PhysicsBody.Mass;

                _map.AddItem(_ship);

                grdViewPort.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void btnShipChallenge1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureWorldStarted();
                ClearCurrent();

                List<PartDNA> parts = new List<PartDNA>();
                parts.Add(new PartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(0.611527511599856, 0, 0), Orientation = new Quaternion(0, -0.706493084706277, 0, 0.707719945502605), Scale = new Vector3D(4.70545346938791, 4.70545346938791, 1.04748080326409) });
                parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-1.48216852903668, 0, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(1.65021551755816, 1.65021551755816, 1.65021551755816), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two), ThrusterType = ThrusterType.Two });
                parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-2.60730396412872, 0, 0), Orientation = new Quaternion(new Vector3D(0, 0, 1), 270), Scale = new Vector3D(0.71390056433019, 0.71390056433019, 0.71390056433019), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two), ThrusterType = ThrusterType.Two });

                ShipDNA shipDNA = ShipDNA.Create(parts);

                Ship ship = await Ship.GetNewShipAsync(_editorOptions, _itemOptions, shipDNA, _world, _material_Ship, _material_Ship, _radiation, _gravity, null, _map, false, false);

                ClearCurrent();

                _shipDNA = shipDNA;
                _ship = ship;

                _ship.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(Ship_ApplyForceAndTorque);

                _thrustController = new ThrustController(_ship, _viewport, _itemOptions);

                //double mass = _ship.PhysicsBody.Mass;

                _ship.Fuel.QuantityCurrent = _ship.Fuel.QuantityMax;
                _ship.RecalculateMass();
                _thrustController.MassChanged(chkShipSimple.IsChecked.Value);

                if (chkShipDebugVisuals.IsChecked.Value)
                {
                    _thrustController.DrawDebugVisuals_Pre();
                }

                //mass = _ship.PhysicsBody.Mass;

                _map.AddItem(_ship);

                grdViewPort.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void btnShipChallenge2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureWorldStarted();
                ClearCurrent();

                List<PartDNA> parts = new List<PartDNA>();
                parts.Add(new PartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(0.611527511599856, 0, 0.0153375982352619), Orientation = new Quaternion(0, -0.706493084706277, 0, 0.707719945502605), Scale = new Vector3D(4.70545346938791, 4.70545346938791, 1.04748080326409) });
                parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-1.48216852903668, 0, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(1.65021551755816, 1.65021551755816, 1.65021551755816), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two), ThrusterType = ThrusterType.Two });
                parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-2.60730396412872, 1.18811621237628, -0.0147591913688635), Orientation = new Quaternion(0, 0, -0.846976393198269, 0.531630500784946), Scale = new Vector3D(0.71390056433019, 0.71390056433019, 0.71390056433019), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two_Two_One), ThrusterType = ThrusterType.Two_Two_One });
                parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-2.60721450068017, -1.18828382189838, -0.0147591913688617), Orientation = new Quaternion(0, 0, -0.496864090131338, 0.867828367788216), Scale = new Vector3D(0.71390056433019, 0.71390056433019, 0.71390056433019), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two_Two_One), ThrusterType = ThrusterType.Two_Two_One });

                ShipDNA shipDNA = ShipDNA.Create(parts);

                Ship ship = await Ship.GetNewShipAsync(_editorOptions, _itemOptions, shipDNA, _world, _material_Ship, _material_Ship, _radiation, _gravity, null, _map, false, false);

                ClearCurrent();

                _shipDNA = shipDNA;
                _ship = ship;

                _ship.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(Ship_ApplyForceAndTorque);

                _thrustController = new ThrustController(_ship, _viewport, _itemOptions);

                //double mass = _ship.PhysicsBody.Mass;

                _ship.Fuel.QuantityCurrent = _ship.Fuel.QuantityMax;
                _ship.RecalculateMass();
                _thrustController.MassChanged(chkShipSimple.IsChecked.Value);

                if (chkShipDebugVisuals.IsChecked.Value)
                {
                    _thrustController.DrawDebugVisuals_Pre();
                }

                //mass = _ship.PhysicsBody.Mass;

                _map.AddItem(_ship);

                grdViewPort.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void btnShipBoston_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureWorldStarted();
                ClearCurrent();

                //TODO: Let the user choose a file
                ShipDNA shipDNA = (ShipDNA)XamlServices.Load(@"C:\Users\charlie.rix\AppData\Roaming\Asteroid Miner\Ships\Beans\2013-05-17 13.50.46.937 - boston - 137.7.xml");

                DateTime startTime = DateTime.Now;

                for (int cntr = 0; cntr < 100; cntr++)
                {
                    //using (Ship shipTest = await Ship.GetNewShipAsync(_editorOptions, _itemOptions, shipDNA, _world, _material_Ship, _radiation, _gravity, null, true, true)) { }
                    using (Ship shipTest = await Ship.GetNewShipAsync(_editorOptions, _itemOptions, shipDNA, _world, _material_Ship, _material_Ship, _radiation, _gravity, null, _map, true, false)) { }          // BAAAAAAAAAD
                    //using (Ship shipTest = await Ship.GetNewShipAsync(_editorOptions, _itemOptions, shipDNA, _world, _material_Ship, _radiation, _gravity, null, false, true)) { }        // GOOD
                }

                TimeSpan elapsed = DateTime.Now - startTime;

                //using (Ship shipTest = await Ship.GetNewShipAsync(_editorOptions, _itemOptions, shipDNA, _world, _material_Ship, _radiation, _gravity, null, true, false)) { }          // BAAAAAAAAAD

                ClearCurrent();

                //_shipDNA = shipDNA;
                //_ship = ship;

                //_ship.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(Ship_ApplyForceAndTorque);

                //_thrustController = new ThrustController(_ship, _viewport, _itemOptions);

                ////double mass = _ship.PhysicsBody.Mass;

                //_ship.Fuel.QuantityCurrent = _ship.Fuel.QuantityMax;
                //_ship.RecalculateMass();
                //_thrustController.MassChanged();

                //if (chkShipDebugVisuals.IsChecked.Value)
                //{
                //    _thrustController.DrawDebugVisuals_Pre();
                //}

                ////mass = _ship.PhysicsBody.Mass;

                //_map.AddItem(_ship);

                grdViewPort.Focus();

                MessageBox.Show(elapsed.TotalMilliseconds.ToString("N0"), this.Title, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void chkShipSimple_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_thrustController != null)
                {
                    _thrustController.MassChanged(chkShipSimple.IsChecked.Value);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCalcThrusts_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_thrustController == null)
                {
                    MessageBox.Show("There is no ship loaded", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _thrustController.EnsureThrustSetsCalculated();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnSaveShip_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_shipDNA == null)
                {
                    MessageBox.Show("There is no ship currently loaded", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ShipEditorWindow.SaveShip(_shipDNA);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void btnLoadShip_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string errMsg;
                ShipDNA shipDNA = ShipEditorWindow.LoadShip(out errMsg);
                if (shipDNA == null)
                {
                    if (!string.IsNullOrEmpty(errMsg))
                    {
                        MessageBox.Show(errMsg, this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    return;
                }

                EnsureWorldStarted();

                Ship ship = await Ship.GetNewShipAsync(_editorOptions, _itemOptions, shipDNA, _world, _material_Ship, _material_Ship, _radiation, _gravity, null, _map, false, chkLoadRepairPositions.IsChecked.Value);

                ClearCurrent();

                _shipDNA = shipDNA;
                _ship = ship;

                _ship.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(Ship_ApplyForceAndTorque);

                _thrustController = new ThrustController(_ship, _viewport, _itemOptions);

                if (_ship.Fuel != null)
                {
                    _ship.Fuel.QuantityCurrent = _ship.Fuel.QuantityMax;
                }
                _ship.RecalculateMass();
                _thrustController.MassChanged(chkShipSimple.IsChecked.Value);

                if (chkShipDebugVisuals.IsChecked.Value)
                {
                    _thrustController.DrawDebugVisuals_Pre();
                }

                _map.AddItem(_ship);

                grdViewPort.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void trkBalanceCount_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isInitialized)
            {
                return;
            }

            try
            {
                ClearBalanceVisualizer();

                trkBalanceCount.ToolTip = Convert.ToInt32(trkBalanceCount.Value).ToString();

                cboBalanceVector.Items.Clear();
                cboBalanceVector.Items.Add("None");
                for (int cntr = 1; cntr <= Convert.ToInt32(trkBalanceCount.Value); cntr++)
                {
                    cboBalanceVector.Items.Add("Line " + cntr.ToString());
                }
                cboBalanceVector.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void chkBalance3D_Checked(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized)
            {
                return;
            }

            try
            {
                ClearBalanceVisualizer();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void chkBalanceAxiis_Checked(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized)
            {
                return;
            }

            try
            {
                if (_balanceVisualizer == null)
                {
                    // If it's null then don't create it
                    //CreateBalanceVisualizer();
                }
                else
                {
                    _balanceVisualizer.ShowAxiis = chkBalanceAxiis.IsChecked.Value;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void radBalance_Checked(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized)
            {
                return;
            }

            try
            {
                if (_balanceVisualizer == null)
                {
                    // If it's null then don't create it
                    //CreateBalanceVisualizer();
                }
                else
                {
                    _balanceVisualizer.TestType = GetBalanceTestType();
                }

                if (radBalanceIndividual.IsChecked.Value)
                {
                    cboBalanceVector.Visibility = Visibility.Visible;
                    chkBalancePossLines.Visibility = Visibility.Visible;
                    chkBalancePossHull.Visibility = Visibility.Visible;
                }
                else
                {
                    cboBalanceVector.Visibility = Visibility.Collapsed;
                    chkBalancePossLines.Visibility = Visibility.Collapsed;
                    chkBalancePossHull.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void chkBalancePossLines_Checked(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized)
            {
                return;
            }

            try
            {
                if (_balanceVisualizer == null)
                {
                    // If it's null then don't create it
                    //CreateBalanceVisualizer();
                }
                else
                {
                    _balanceVisualizer.ShowPossibilityLines = chkBalancePossLines.IsChecked.Value;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void chkBalancePossHull_Checked(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized)
            {
                return;
            }

            try
            {
                if (_balanceVisualizer == null)
                {
                    // If it's null then don't create it
                    //CreateBalanceVisualizer();
                }
                else
                {
                    _balanceVisualizer.ShowPossibilityHull = chkBalancePossHull.IsChecked.Value;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void cboBalanceVector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized)
            {
                return;
            }

            try
            {
                if (_balanceVisualizer == null)
                {
                    // If it's null then don't create it
                    //CreateBalanceVisualizer();
                }
                else
                {
                    // Pull the selected index out of the combobox's text
                    int selectedIndex = -1;
                    string selectedText = cboBalanceVector.SelectedItem as string;
                    if (!string.IsNullOrEmpty(selectedText))
                    {
                        Match match = Regex.Match(selectedText, @"\d+");
                        if (match.Success)
                        {
                            selectedIndex = Convert.ToInt32(match.Value) - 1;		// the text is one based, but selected index is zero based
                        }
                    }

                    _balanceVisualizer.SelectedIndex = selectedIndex;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnBalanceGenerate_Click(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized)
            {
                return;
            }

            try
            {
                CreateBalanceVisualizer();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnBalanceTest_Click(object sender, RoutedEventArgs e)
        {
            //NOTE: The reason why this works but the other doesn't is because these points are probably only an approximation

            try
            {
                ClearCurrent();

                // Add up all combos
                List<Point3D> remainExtremes = new List<Point3D>();

                //remainExtremes.Add(new Point3D(0, 0, 0));
                //remainExtremes.Add(new Point3D(-3.40147377037991, -0.419781234591244, -2.17796212660181));
                //remainExtremes.Add(new Point3D(-1.49306669877188, -0.925708791939134, 2.13598931537263));
                //remainExtremes.Add(new Point3D(-5.69624263165113, -0.958436816589518, -3.37315759858873));
                //remainExtremes.Add(new Point3D(-1.76014897753647, -0.0930416619464117, -4.09343205865151));
                //remainExtremes.Add(new Point3D(-1.25496300318026, 0.717843566701332, -1.20328603793784));
                //remainExtremes.Add(new Point3D(-3.78783556004309, -1.46436437393741, 0.94079384338572));
                //remainExtremes.Add(new Point3D(0.14825809407156, -0.598969219294302, 0.220519383322938));
                //remainExtremes.Add(new Point3D(-4.05491783880769, -0.631697243944685, -5.28862753063842));
                //remainExtremes.Add(new Point3D(0.653444068427775, 0.211916009353442, 3.11066540403661));
                //remainExtremes.Add(new Point3D(-3.54973186445147, 0.179187984703058, -2.39848150992475));
                //remainExtremes.Add(new Point3D(0.38636178966318, 1.04458313934616, -3.11875596998753));
                //remainExtremes.Add(new Point3D(-2.14651076719965, -1.13762480129258, -0.974676088663974));
                //remainExtremes.Add(new Point3D(-1.64132479284344, -0.326739572644832, 1.91546993204969));
                //remainExtremes.Add(new Point3D(2.29476886127121, 0.538655581998274, 1.19519547198691));
                //remainExtremes.Add(new Point3D(-1.90840707160803, 0.50592755734789, -4.31395144197444));

                //remainExtremes.Add(new Point3D(0, 0, 0));
                //remainExtremes.Add(new Point3D(-5.31630744216994, 1.11425207033481, -4.47580708816051));
                //remainExtremes.Add(new Point3D(-2.13046936596623, -1.49053540671147, -3.00904096965914));
                //remainExtremes.Add(new Point3D(-4.57987376242305, 2.72713215596488, -0.802575639743833));
                //remainExtremes.Add(new Point3D(-3.92227175595059, 0.991907391416202, -5.13999756691806));
                //remainExtremes.Add(new Point3D(-1.39403568621935, 0.122344678918604, 0.664190478757545));
                //remainExtremes.Add(new Point3D(-0.736433679746885, -1.61288008563008, -3.67323144841668));
                //remainExtremes.Add(new Point3D(-3.18583807620371, 2.60478747704628, -1.46676611850138));


                remainExtremes.Add(new Point3D(0, 0, 0));
                remainExtremes.Add(new Point3D(0.435973208357982, -1.1377534775636, 5.70807586177137));
                remainExtremes.Add(new Point3D(1.95478111186171, -4.09634847660277, 2.29053968141151));
                remainExtremes.Add(new Point3D(1.18330074763286, 1.42930566755721, 2.31176372755387));
                remainExtremes.Add(new Point3D(-2.26613544277861, 0.391535853918364, 6.81384831457736));
                remainExtremes.Add(new Point3D(2.70210865113659, -1.52928933148196, -1.10577245280599));
                remainExtremes.Add(new Point3D(-0.747327539274875, -2.56705914512081, 3.3963121342175));
                remainExtremes.Add(new Point3D(-1.51880790350373, 2.95859499903917, 3.41753618035986));


                // Build a convex hull out of them
                TriangleIndexed[] hull = null;
                try
                {
                    hull = Math3D.GetConvexHull(remainExtremes.ToArray());
                }
                catch (Exception)
                {
                    hull = null;
                }

                if (hull != null)
                {
                    #region Lines

                    ScreenSpaceLines3D lines = new ScreenSpaceLines3D();
                    lines.Thickness = 1d;
                    lines.Color = UtilityWPF.ColorFromHex("373403");

                    foreach (var triangle in hull)
                    {
                        lines.AddLine(triangle.Point0, triangle.Point1);
                        lines.AddLine(triangle.Point0, triangle.Point2);
                        lines.AddLine(triangle.Point1, triangle.Point2);
                    }

                    _currentVisuals.Add(lines);
                    _viewport.Children.Add(lines);

                    #endregion

                    #region Hull

                    // Material
                    MaterialGroup materials = new MaterialGroup();
                    materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("209F9B50"))));
                    materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("A0EAE375")), 33d));

                    // Geometry Model
                    GeometryModel3D geometry = new GeometryModel3D();
                    geometry.Material = materials;
                    geometry.BackMaterial = materials;
                    geometry.Geometry = UtilityWPF.GetMeshFromTriangles_IndependentFaces(hull);

                    ModelVisual3D model = new ModelVisual3D();
                    model.Content = geometry;

                    _currentVisuals.Add(model);
                    _viewport.Children.Add(model);

                    #endregion
                }





            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnIntersectingCubes_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureWorldStarted();
                ClearCurrent();

                PartDNA dnaGrav = new PartDNA() { PartType = SensorGravity.PARTTYPE, Position = new Point3D(-.5, 0, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(10, 10, 10) };
                PartDNA dnaSpin = new PartDNA() { PartType = SensorSpin.PARTTYPE, Position = new Point3D(.5, 0, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(10, 10, 10) };
                //PartDNA dnaGrav = new PartDNA() { PartType = SensorGravity.PARTTYPE, Position = new Point3D(-1, 0, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(10, 10, 10) };
                //PartDNA dnaSpin = new PartDNA() { PartType = SensorSpin.PARTTYPE, Position = new Point3D(1, 0, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(10, 10, 10) };

                SensorGravity grav = new SensorGravity(_editorOptions, _itemOptions, dnaGrav, null, null);
                SensorSpin spin = new SensorSpin(_editorOptions, _itemOptions, dnaSpin, null);

                PartBase[] parts = new PartBase[] { grav, spin };

                // Draw before
                DrawParts(parts, new Vector3D(-5, 0, 0));

                // Separate them
                CollisionHull.IntersectionPoint[] intersections;
                EnsurePartsNotIntersecting.Separate(out intersections, parts, _world, .01d);

                DrawLines(intersections.Select(o => Tuple.Create(o.ContactPoint, o.Normal.ToUnit() * o.PenetrationDistance)).ToArray(), new Vector3D(-5, -5, 0), Colors.Red);

                // Draw after
                DrawParts(parts, new Vector3D(5, 0, 0));

                #region Test

                //CollisionHull hull1a, hull2a;
                //Transform3DGroup dummy2;
                //Quaternion dummy3;
                //DiffuseMaterial dummy4;
                //double size = 10d * .2d;
                //Visual3D model = GetWPFModel(out hull1a, out dummy2, out dummy3, out dummy4, CollisionShapeType.Box, Colors.Tan, Colors.Tan, 1d, new Vector3D(size, size, size), new Point3D(-.6, 5, 0), _defaultDirectionFacing, true);		// this doesn't use the offset in the collision hull
                //_currentVisuals.Add(model);
                //_viewport.Children.Add(model);

                //model = GetWPFModel(out hull2a, out dummy2, out dummy3, out dummy4, CollisionShapeType.Box, Colors.Chartreuse, Colors.Chartreuse, 1d, new Vector3D(size, size, size), new Point3D(.6, 5, 0), _defaultDirectionFacing, true);
                //_currentVisuals.Add(model);
                //_viewport.Children.Add(model);


                //TranslateTransform3D offset1 = new TranslateTransform3D(-.6, 0, 0);
                //TranslateTransform3D offset2 = new TranslateTransform3D(.6, 0, 0);


                //CollisionHull.IntersectionPoint[] points1a = hull1a.GetIntersectingPoints_HullToHull(100, hull2a, 0, offset1, offset2);
                //CollisionHull.IntersectionPoint[] points2a = hull2a.GetIntersectingPoints_HullToHull(100, hull1a, 0, offset2, offset1);

                //CollisionHull hull1b = CollisionHull.CreateBox(_world, 0, new Vector3D(size, size, size), offset1.Value);
                //CollisionHull hull2b = CollisionHull.CreateBox(_world, 0, new Vector3D(size, size, size), offset2.Value);

                //CollisionHull.IntersectionPoint[] points1b = hull1b.GetIntersectingPoints_HullToHull(100, hull2b, 0, null, null);
                //CollisionHull.IntersectionPoint[] points2b = hull2b.GetIntersectingPoints_HullToHull(100, hull1b, 0, null, null);

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnIntersectingCubes2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureWorldStarted();
                ClearCurrent();

                PartDNA dnaGrav = new PartDNA() { PartType = SensorGravity.PARTTYPE, Position = new Point3D(-.6, -.1, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(10, 10, 10) };
                PartDNA dnaSpin = new PartDNA() { PartType = SensorSpin.PARTTYPE, Position = new Point3D(.6, .1, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(10, 10, 10) };

                SensorGravity grav = new SensorGravity(_editorOptions, _itemOptions, dnaGrav, null, null);
                SensorSpin spin = new SensorSpin(_editorOptions, _itemOptions, dnaSpin, null);

                PartBase[] parts = new PartBase[] { grav, spin };

                // Draw before
                DrawParts(parts, new Vector3D(-5, 0, 0));

                // Separate them
                CollisionHull.IntersectionPoint[] intersections;
                EnsurePartsNotIntersecting.Separate(out intersections, parts, _world, .01d);

                DrawLines(intersections.Select(o => Tuple.Create(o.ContactPoint, o.Normal.ToUnit() * o.PenetrationDistance)).ToArray(), new Vector3D(-5, -5, 0), Colors.Red);

                // Draw after
                DrawParts(parts, new Vector3D(5, 0, 0));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnIntersectingCubes3_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureWorldStarted();
                ClearCurrent();

                //PartDNA dnaGrav = new PartDNA() { PartType = SensorGravity.PARTTYPE, Position = new Point3D(-1.01, 0, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(10, 10, 10) };
                //PartDNA dnaSpin = new PartDNA() { PartType = SensorSpin.PARTTYPE, Position = new Point3D(1.01, 0, 0), Orientation = new Quaternion(new Vector3D(0, 0, 1), 30), Scale = new Vector3D(10, 10, 10) };
                PartDNA dnaGrav = new PartDNA() { PartType = SensorGravity.PARTTYPE, Position = new Point3D(-1.01, 0, 0), Orientation = new Quaternion(new Vector3D(0, 0, -1), 15), Scale = new Vector3D(10, 10, 10) };
                PartDNA dnaSpin = new PartDNA() { PartType = SensorSpin.PARTTYPE, Position = new Point3D(1.01, 0, 0), Orientation = new Quaternion(new Vector3D(0, 0, 1), 45), Scale = new Vector3D(10, 10, 10) };

                SensorGravity grav = new SensorGravity(_editorOptions, _itemOptions, dnaGrav, null, null);
                SensorSpin spin = new SensorSpin(_editorOptions, _itemOptions, dnaSpin, null);

                PartBase[] parts = new PartBase[] { grav, spin };

                // Draw before
                DrawParts(parts, new Vector3D(-5, 0, 0));

                // Separate them
                CollisionHull.IntersectionPoint[] intersections;
                EnsurePartsNotIntersecting.Separate(out intersections, parts, _world, .01d);

                DrawLines(intersections.Select(o => Tuple.Create(o.ContactPoint, o.Normal.ToUnit() * o.PenetrationDistance)).ToArray(), new Vector3D(-5, -5, 0), Colors.Red);

                // Draw after
                DrawParts(parts, new Vector3D(5, 0, 0));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnBoxBreakdown_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearCurrent();
                Random rand = StaticRandom.GetRandomForThread();

                #region Unit Tests

                //var cube1 = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Box, UtilityNewt.MassDistribution.Uniform, new Vector3D(1, 1, 1), .3);
                //var cube2 = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Box, UtilityNewt.MassDistribution.Uniform, new Vector3D(1, 1, 1), .065);
                //var cube3 = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Box, UtilityNewt.MassDistribution.Uniform, new Vector3D(1, 1, 1), .5);

                //var rect1 = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Box, UtilityNewt.MassDistribution.Uniform, new Vector3D(2, 1, 1), .3);
                //var rect2 = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Box, UtilityNewt.MassDistribution.Uniform, new Vector3D(2, 1, 1), .065);
                //var rect3 = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Box, UtilityNewt.MassDistribution.Uniform, new Vector3D(2, 1, 1), .5);

                //for (int cntr = 0; cntr < 1000; cntr++)
                //{
                //    Vector3D size = new Vector3D(1 + (rand.NextDouble() * 2), 1 + (rand.NextDouble() * 2), 1 + (rand.NextDouble() * 2));
                //    double cellSize = .1 + (rand.NextDouble() * 1.2);
                //    var rect = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Box, UtilityNewt.MassDistribution.Uniform, size, cellSize);
                //}

                #endregion

                // Visual
                Vector3D size = new Vector3D(1 + (rand.NextDouble() * 2), 1 + (rand.NextDouble() * 2), 1 + (rand.NextDouble() * 2));
                double cellSize = .1 + (rand.NextDouble() * 1.2);
                var rect = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Box, UtilityNewt.MassDistribution.Uniform, size, cellSize);

                DrawMassBreakdown(rect, cellSize);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnCylinderBreakdown_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearCurrent();
                Random rand = StaticRandom.GetRandomForThread();



                // Break it down
                Vector3D size = new Vector3D(1 + (rand.NextDouble() * 2), 1 + (rand.NextDouble() * 2), 1 + (rand.NextDouble() * 2));
                double cellSize = .1 + (rand.NextDouble() * 1.2);

                //Vector3D size = new Vector3D(1, 1, 1);
                //double cellSize = .3d;

                //Vector3D size = new Vector3D(1, 1, 1);
                //double cellSize = .17d;

                //Vector3D size = new Vector3D(3, .2, .2);
                //double cellSize = .3d;

                var cylinder = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Cylinder, UtilityNewt.MassDistribution.Uniform, size, cellSize);

                DrawMassBreakdown(cylinder, cellSize);

                // Draw Cylinder
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnCapsuleBreakdown_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearCurrent();
                Random rand = StaticRandom.GetRandomForThread();


                Vector3D size = new Vector3D(1 + (rand.NextDouble() * 2), 1 + (rand.NextDouble() * 2), 1 + (rand.NextDouble() * 2));
                size.Z = size.Y;
                double cellSize = .1 + (rand.NextDouble() * 1.2);

                //Vector3D size = new Vector3D(3, .75, .75);
                //double cellSize = .25;


                Vector3D mainSize = new Vector3D(size.X * .75d, size.Y, size.Z);
                double mainVolume = Math.Pow((mainSize.Y + mainSize.Z) / 4d, 2d) * Math.PI * mainSize.X;
                var mainCylinder = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Cylinder, UtilityNewt.MassDistribution.Uniform, mainSize, cellSize);

                Vector3D capSize = new Vector3D(size.X * .125d, size.Y * .5d, size.Z * .5d);
                double capVolume = Math.Pow((capSize.Y + capSize.Z) / 4d, 2d) * Math.PI * capSize.X;
                var capCylinder = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Cylinder, UtilityNewt.MassDistribution.Uniform, capSize, cellSize);

                double offsetX = (mainSize.X * .5d) + (capSize.X * .5d);

                var objects = new Tuple<UtilityNewt.ObjectMassBreakdown, Point3D, Quaternion, double>[3];
                objects[0] = new Tuple<UtilityNewt.ObjectMassBreakdown, Point3D, Quaternion, double>(mainCylinder, new Point3D(0, 0, 0), Quaternion.Identity, mainVolume);
                objects[1] = new Tuple<UtilityNewt.ObjectMassBreakdown, Point3D, Quaternion, double>(capCylinder, new Point3D(offsetX, 0, 0), Quaternion.Identity, capVolume);
                objects[2] = new Tuple<UtilityNewt.ObjectMassBreakdown, Point3D, Quaternion, double>(capCylinder, new Point3D(-offsetX, 0, 0), Quaternion.Identity, capVolume);
                var combined = UtilityNewt.Combine(objects);

                DrawMassBreakdown(combined, cellSize);

                #region Draw Capsule

                // Material
                MaterialGroup materials = new MaterialGroup();
                materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("402A4A52"))));
                materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("602C8564")), .25d));

                // Geometry Model
                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = materials;
                geometry.BackMaterial = materials;

                //NOTE: This capsule will become a sphere if the height isn't enough to overcome twice the radius.  But the fuel tank will start off
                //as a capsule, then the scale along height will deform the caps
                geometry.Geometry = UtilityWPF.GetCapsule_AlongZ(20, 20, size.Y * .5d, size.X);

                geometry.Transform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90));

                ModelVisual3D visual = new ModelVisual3D();
                visual.Content = geometry;
                _viewport.Children.Add(visual);
                _currentVisuals.Add(visual);

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        /// <remarks>
        /// Good links:
        /// http://newtondynamics.com/wiki/index.php5?title=NewtonBodySetMassMatrix
        /// 
        /// http://en.wikipedia.org/wiki/List_of_moments_of_inertia
        /// 
        /// http://en.wikipedia.org/wiki/Moment_of_inertia
        /// 
        /// 
        /// Say there are 3 solid spheres, each with a different mass and radius.  Each individual sphere's moment of inertia is 2/5*m*r^2 for each x,y,z
        /// axis, because they are perfectly symetrical.  So now that each individual sphere inertia is known, the 3 as a rigid body can use the parallel axis
        /// theorem.
        /// 
        /// ------------
        /// 
        /// However, say there are 3 solid cylinders, each with a different mass height radius, and each with a random orientation.  Each individual cylinder's
        /// moment of inertia in its local coords is (mr^2)/2 along the axis, and (3mr^2 + 3mh^2)/12 along two poles perp to the axis.
        /// 
        /// BUT the combined body can't use those simplified equations because each cylinder as an arbitrary orienation relative to the whole body's axis, so
        /// those simplifications for each individual body along the whole's axiis can't be used.  The only way is to evenly divide the parts into many uniform
        /// pieces and add up all the mr^2 of each piece (r being the distance from the axis)
        /// 
        /// In other words:
        ///		Ix = sum(m* (dist from x)^2) 
        ///		Iy = sum(m* (dist from y)^2) 
        ///		Iz = sum(m* (dist from z)^2) 
        /// 
        /// Then use parallel axis theorem for each part because each part's center of mass is not along the whole's axiis (they may be in rare cases, but not likely)
        /// 
        /// -------------
        /// 
        /// Newt seems to want the mass separate from the vector, so I guess the vector is just all the r^2's
        /// 
        /// -------------
        /// 
        /// Rather than the ship knowing how to divide up each part, the part should have abstract methods:
        ///		Point3D GetCenterMass()
        ///		Vector3D GetInertia(Tuple[Vector3D,Vector3D,Vector3D] rays)		// each of the 3 rays is through the object's center of mass.  The rays are assumed to all be perp to each other
        ///		
        /// This way, if the part knows the object is a sphere, it can just return (2mr^2)/5, regardless of the vector passed in.
        /// Otherwise, it would need to break down the object however it decides to
        /// 
        /// As a side note, if those methods could be made static, and pass in a dna, then you could wrap them in a task, and spin
        /// up 3 tasks for each part (one for each axis).  Then await all the tasks. -- actually, keep the method an instance method,
        /// but make a static utility class for various solids.  Then the part can cache previous results for those rays.
        /// 
        /// </remarks>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnMassMatrix_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureWorldStarted();
                ClearCurrent();

                Body body = GetNewBody(CollisionShapeType.Box, Colors.Orange, new Vector3D(1, 1, 1), 1, new Point3D(0, 0, 0), Quaternion.Identity, _material_Ship);
                body.LinearDamping = .01f;
                body.AngularDamping = new Vector3D(.01f, .01f, .01f);

                var prevMatrix = body.MassMatrix;


                double x = 1;
                double y = 1;
                double z = 1;


                body.MassMatrix = new MassMatrix(10, new Vector3D(x, y, z));


                // Remember them
                _currentBody = body;
                _currentVisuals.AddRange(body.Visuals);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCoplanarTo2D_Click(object sender, RoutedEventArgs e)
        {
            const double MAXRADIUS = 15d;

            try
            {
                ClearCurrent();

                #region Generate a bunch of coplanar points

                Triangle triangle = new Triangle(Math3D.GetRandomVector(MAXRADIUS).ToPoint(), Math3D.GetRandomVector(MAXRADIUS).ToPoint(), Math3D.GetRandomVector(MAXRADIUS).ToPoint());

                Point3D[] points3D = new Point3D[200];
                for (int cntr = 0; cntr < points3D.Length; cntr++)
                {
                    points3D[cntr] = Math3D.FromBarycentric(triangle, new Vector(Math3D.GetNearZeroValue(2d), Math3D.GetNearZeroValue(2d)));
                }

                #endregion

                // Transform them onto the xy plane
                Point[] points2D = GetRotatedPoints(points3D);

                #region Draw Axiis

                ScreenSpaceLines3D axisLines = new ScreenSpaceLines3D();
                axisLines.Thickness = 3d;
                axisLines.Color = Colors.Black;

                axisLines.AddLine(new Point3D(-50, 0, 0), new Point3D(50, 0, 0));
                axisLines.AddLine(new Point3D(0, -50, 0), new Point3D(0, 50, 0));
                axisLines.AddLine(new Point3D(0, 0, -50), new Point3D(0, 0, 50));

                _currentVisuals.Add(axisLines);
                _viewport.Children.Add(axisLines);

                #endregion
                #region Draw Points

                // Material
                MaterialGroup material3D = new MaterialGroup();
                material3D.Children.Add(new DiffuseMaterial(Brushes.HotPink));
                material3D.Children.Add(new SpecularMaterial(Brushes.White, 25d));

                MaterialGroup material2D = new MaterialGroup();
                material2D.Children.Add(new DiffuseMaterial(Brushes.DarkSeaGreen));
                material2D.Children.Add(new SpecularMaterial(Brushes.Yellow, 25d));

                ScreenSpaceLines3D lines = new ScreenSpaceLines3D();
                lines.Thickness = 1d;
                lines.Color = UtilityWPF.ColorFromHex("C8C8C8");

                Model3DGroup geometries = new Model3DGroup();

                for (int cntr = 0; cntr < points3D.Length; cntr++)
                {
                    // 3D Point
                    GeometryModel3D geometry = new GeometryModel3D();
                    geometry.Material = material3D;
                    geometry.BackMaterial = material3D;
                    geometry.Geometry = UtilityWPF.GetSphere_LatLon(5, .2d);
                    geometry.Transform = new TranslateTransform3D(points3D[cntr].ToVector());
                    geometries.Children.Add(geometry);

                    if (points2D != null)
                    {
                        // 2D Point
                        geometry = new GeometryModel3D();
                        geometry.Material = material2D;
                        geometry.BackMaterial = material2D;
                        geometry.Geometry = UtilityWPF.GetSphere_LatLon(5, .2d);
                        geometry.Transform = new TranslateTransform3D(points2D[cntr].X, points2D[cntr].Y, 0d);
                        geometries.Children.Add(geometry);

                        // Connecting line
                        lines.AddLine(points3D[cntr], new Point3D(points2D[cntr].X, points2D[cntr].Y, 0));
                    }
                }

                ModelVisual3D visual = new ModelVisual3D();
                visual.Content = geometries;
                _viewport.Children.Add(visual);
                _currentVisuals.Add(visual);

                if (points2D != null)
                {
                    //_viewport.Children.Add(lines);
                    //_currentVisuals.Add(lines);
                }

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void radFallingSand_Checked(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized)
            {
                return;
            }

            try
            {
                EnsureWorldStarted();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private void ClearCurrent()
        {
            ClearBalanceVisualizer();

            foreach (Visual3D visual in _currentVisuals)
            {
                _viewport.Children.Remove(visual);
            }
            _currentVisuals.Clear();

            if (_currentBody != null)
            {
                _currentBody.Dispose();
                _currentBody = null;
            }

            if (_ship != null)
            {
                _map.RemoveItem(_ship, true);
                _ship.Dispose();
                _ship = null;
            }

            _shipDNA = null;

            if (_thrustController != null)
            {
                _thrustController.Dispose();
                _thrustController = null;
            }
        }

        private void EnsureWorldStarted()
        {
            if (_world != null)
            {
                return;
            }

            #region Init World

            // Set the size of the world to something a bit random (gets boring when it's always the same size)
            double halfSize = 50d;
            _boundryMin = new Point3D(-halfSize, -halfSize, -halfSize);
            _boundryMax = new Point3D(halfSize, halfSize, halfSize);

            _world = new World();
            _world.Updating += new EventHandler<WorldUpdatingArgs>(World_Updating);

            List<Point3D[]> innerLines, outerLines;
            _world.SetCollisionBoundry(out innerLines, out outerLines, _boundryMin, _boundryMax);

            // Draw the lines
            _boundryLines = new ScreenSpaceLines3D(true);
            _boundryLines.Thickness = 1d;
            _boundryLines.Color = _colors.BoundryLines;
            _viewport.Children.Add(_boundryLines);

            foreach (Point3D[] line in innerLines)
            {
                _boundryLines.AddLine(line[0], line[1]);
            }

            #endregion
            #region Materials

            _materialManager = new MaterialManager(_world);

            // Asteroid
            Game.Newt.v2.NewtonDynamics.Material material = new Game.Newt.v2.NewtonDynamics.Material();
            material.Elasticity = .25d;
            material.StaticFriction = .9d;
            material.KineticFriction = .75d;
            _material_Asteroid = _materialManager.AddMaterial(material);

            // Ship
            material = new Game.Newt.v2.NewtonDynamics.Material();
            material.Elasticity = .75d;
            material.StaticFriction = .5d;
            material.KineticFriction = .2d;
            _material_Ship = _materialManager.AddMaterial(material);

            // Sand
            material = new Game.Newt.v2.NewtonDynamics.Material();
            material.Elasticity = .5d;
            material.StaticFriction = .5d;
            material.KineticFriction = .33d;
            _material_Sand = _materialManager.AddMaterial(material);

            _materialManager.RegisterCollisionEvent(_material_Ship, _material_Asteroid, Collision_Ship);
            _materialManager.RegisterCollisionEvent(_material_Ship, _material_Sand, Collision_Sand);

            #endregion

            _map = new Map(_viewport, null, _world);
            //_map.SnapshotFequency_Milliseconds = 125;
            //_map.SnapshotMaxItemsPerNode = 10;
            _map.ShouldBuildSnapshots = false;
            //_map.ShouldShowSnapshotLines = true;
            //_map.ShouldSnapshotCentersDrift = false;

            _radiation = new RadiationField();
            _radiation.AmbientRadiation = 1d;

            _gravity = new GravityFieldUniform();

            _world.UnPause();
        }

        private void BuildStandalonePart(PartBase part)
        {
            EnsureWorldStarted();
            ClearCurrent();

            // WPF
            ModelVisual3D model = new ModelVisual3D();
            model.Content = part.Model;

            _viewport.Children.Add(model);
            _currentVisuals.Add(model);

            // Physics
            CollisionHull hull = part.CreateCollisionHull(_world);
            Body body = new Body(hull, Matrix3D.Identity, part.TotalMass, new Visual3D[] { model });
            body.MaterialGroupID = _material_Ship;
            body.LinearDamping = .01f;
            body.AngularDamping = new Vector3D(.01f, .01f, .01f);

            _currentBody = body;
        }

        private Body GetNewBody(CollisionShapeType shape, Color color, Vector3D size, double mass, Point3D position, Quaternion orientation, int materialID)
        {
            DoubleVector dirFacing = _defaultDirectionFacing.GetRotatedVector(orientation.Axis, orientation.Angle);

            // Get the wpf model
            CollisionHull hull;
            Transform3DGroup transform;
            Quaternion rotation;
            DiffuseMaterial bodyMaterial;
            ModelVisual3D model = GetWPFModel(out hull, out transform, out rotation, out bodyMaterial, shape, color, Colors.White, 100d, size, position, dirFacing, true);

            // Add to the viewport
            _viewport.Children.Add(model);

            // Make a physics body that represents this shape
            Body retVal = new Body(hull, transform.Value, mass, new Visual3D[] { model });
            retVal.AutoSleep = false;		// the falling sand was falling asleep, even though the velocity was non zero (the sand would suddenly stop)
            retVal.MaterialGroupID = materialID;

            return retVal;
        }
        private Model3D GetWPFGeometry(out CollisionHull hull, out Transform3DGroup transform, out Quaternion rotation, out DiffuseMaterial bodyMaterial, CollisionShapeType shape, Color color, Color reflectionColor, double reflectionIntensity, Vector3D size, Point3D position, DoubleVector directionFacing, bool createHull)
        {
            // Material
            MaterialGroup materials = new MaterialGroup();
            bodyMaterial = new DiffuseMaterial(new SolidColorBrush(color));
            materials.Children.Add(bodyMaterial);
            materials.Children.Add(new SpecularMaterial(new SolidColorBrush(reflectionColor), reflectionIntensity));

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;

            hull = null;

            switch (shape)
            {
                case CollisionShapeType.Box:
                    Vector3D halfSize = size / 2d;
                    geometry.Geometry = UtilityWPF.GetCube_IndependentFaces(new Point3D(-halfSize.X, -halfSize.Y, -halfSize.Z), new Point3D(halfSize.X, halfSize.Y, halfSize.Z));
                    if (createHull)
                    {
                        hull = CollisionHull.CreateBox(_world, 0, size, null);
                    }
                    break;

                case CollisionShapeType.Sphere:
                    geometry.Geometry = UtilityWPF.GetSphere_LatLon(5, size.X, size.Y, size.Z);
                    if (createHull)
                    {
                        hull = CollisionHull.CreateSphere(_world, 0, size, null);
                    }
                    break;

                case CollisionShapeType.Cylinder:
                    geometry.Geometry = UtilityWPF.GetCylinder_AlongX(20, size.X, size.Y);
                    if (createHull)
                    {
                        hull = CollisionHull.CreateCylinder(_world, 0, size.X, size.Y, null);
                    }
                    break;

                case CollisionShapeType.Cone:
                    geometry.Geometry = UtilityWPF.GetCone_AlongX(20, size.X, size.Y);
                    if (createHull)
                    {
                        hull = CollisionHull.CreateCone(_world, 0, size.X, size.Y, null);
                    }
                    break;

                default:
                    throw new ApplicationException("Unexpected CollisionShapeType: " + shape.ToString());
            }

            // Transform
            transform = new Transform3DGroup();		// rotate needs to be added before translate



            //rotation = _defaultDirectionFacing.GetAngleAroundAxis(directionFacing);		// can't use double vector, it over rotates (not anymore, but this is still isn't rotating correctly)

            rotation = Math3D.GetRotation(_defaultDirectionFacing.Standard, directionFacing.Standard);



            transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(rotation)));
            transform.Children.Add(new TranslateTransform3D(position.ToVector()));

            geometry.Transform = transform;

            //// Model Visual
            //ModelVisual3D retVal = new ModelVisual3D();
            //retVal.Content = geometry;
            //retVal.Transform = transform;

            // Exit Function
            //return retVal;
            return geometry;
        }
        private ModelVisual3D GetWPFModel(out CollisionHull hull, out Transform3DGroup transform, out Quaternion rotation, out DiffuseMaterial bodyMaterial, CollisionShapeType shape, Color color, Color reflectionColor, double reflectionIntensity, Vector3D size, Point3D position, DoubleVector directionFacing, bool createHull)
        {
            // Material
            MaterialGroup materials = new MaterialGroup();
            bodyMaterial = new DiffuseMaterial(new SolidColorBrush(color));
            materials.Children.Add(bodyMaterial);
            materials.Children.Add(new SpecularMaterial(new SolidColorBrush(reflectionColor), reflectionIntensity));

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;

            hull = null;

            switch (shape)
            {
                case CollisionShapeType.Box:
                    Vector3D halfSize = size / 2d;
                    geometry.Geometry = UtilityWPF.GetCube_IndependentFaces(new Point3D(-halfSize.X, -halfSize.Y, -halfSize.Z), new Point3D(halfSize.X, halfSize.Y, halfSize.Z));
                    if (createHull)
                    {
                        hull = CollisionHull.CreateBox(_world, 0, size, null);
                    }
                    break;

                case CollisionShapeType.Sphere:
                    geometry.Geometry = UtilityWPF.GetSphere_LatLon(5, size.X, size.Y, size.Z);
                    if (createHull)
                    {
                        hull = CollisionHull.CreateSphere(_world, 0, size, null);
                    }
                    break;

                case CollisionShapeType.Cylinder:
                    geometry.Geometry = UtilityWPF.GetCylinder_AlongX(20, size.X, size.Y);
                    if (createHull)
                    {
                        hull = CollisionHull.CreateCylinder(_world, 0, size.X, size.Y, null);
                    }
                    break;

                case CollisionShapeType.Cone:
                    geometry.Geometry = UtilityWPF.GetCone_AlongX(20, size.X, size.Y);
                    if (createHull)
                    {
                        hull = CollisionHull.CreateCone(_world, 0, size.X, size.Y, null);
                    }
                    break;

                default:
                    throw new ApplicationException("Unexpected CollisionShapeType: " + shape.ToString());
            }

            // Transform
            transform = new Transform3DGroup();		// rotate needs to be added before translate



            //rotation = _defaultDirectionFacing.GetAngleAroundAxis(directionFacing);		// can't use double vector, it over rotates (not anymore, but this is still isn't rotating correctly)

            rotation = Math3D.GetRotation(_defaultDirectionFacing.Standard, directionFacing.Standard);



            transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(rotation)));
            transform.Children.Add(new TranslateTransform3D(position.ToVector()));

            // Model Visual
            ModelVisual3D retVal = new ModelVisual3D();
            retVal.Content = geometry;
            retVal.Transform = transform;

            // Exit Function
            return retVal;
        }

        /// <summary>
        /// This draws an already created part
        /// </summary>
        private void DrawParts(PartBase[] parts, Vector3D offset)
        {
            Model3DGroup models = new Model3DGroup();

            foreach (PartBase part in parts)
            {
                models.Children.Add(part.Model);
            }

            ModelVisual3D model = new ModelVisual3D();
            model.Content = models;
            model.Transform = new TranslateTransform3D(offset);

            _currentVisuals.Add(model);
            _viewport.Children.Add(model);
        }
        private void DrawLines(Tuple<Point3D, Vector3D>[] lines, Vector3D offset, Color color)
        {
            ScreenSpaceLines3D lineVisual = new ScreenSpaceLines3D(true);
            lineVisual.Color = color;
            lineVisual.Thickness = 2d;

            foreach (var line in lines)
            {
                lineVisual.AddLine(line.Item1 + offset, line.Item1 + line.Item2 + offset);
            }

            _currentVisuals.Add(lineVisual);
            _viewport.Children.Add(lineVisual);
        }

        private static PartDNA GetDefaultDNA(string partType)
        {
            PartDNA retVal = new PartDNA();

            retVal.PartType = partType;

            retVal.Position = new Point3D(0, 0, 0);
            retVal.Orientation = Quaternion.Identity;
            retVal.Scale = new Vector3D(1, 1, 1);

            return retVal;
        }
        private static void ModifyDNA(PartDNA dna, bool randSize, bool randOrientation)
        {
            if (randSize)
            {
                dna.Scale = Math3D.GetRandomVector(new Vector3D(.25, .25, .25), new Vector3D(2.5, 2.5, 2.5));
            }

            if (randOrientation)
            {
                dna.Orientation = Math3D.GetRandomRotation();
            }
        }

        private void ClearBalanceVisualizer()
        {
            if (_balanceVisualizer != null)
            {
                _balanceVisualizer.Dispose();
            }

            _balanceVisualizer = null;
        }
        private void CreateBalanceVisualizer()
        {
            ClearBalanceVisualizer();

            // Pull the selected index out of the combobox's text
            int selectedIndex = -1;
            string selectedText = cboBalanceVector.SelectedItem as string;
            if (!string.IsNullOrEmpty(selectedText))
            {
                Match match = Regex.Match(selectedText, @"\d+");
                if (match.Success)
                {
                    selectedIndex = Convert.ToInt32(match.Value) - 1;		// the text is one based, but selected index is zero based
                }
            }

            _balanceVisualizer = new BalanceVisualizer(_viewport, Convert.ToInt32(trkBalanceCount.Value), chkBalance3D.IsChecked.Value);
            _balanceVisualizer.SelectedIndex = selectedIndex;
            _balanceVisualizer.ShowAxiis = chkBalanceAxiis.IsChecked.Value;
            _balanceVisualizer.TestType = GetBalanceTestType();
            _balanceVisualizer.ShowPossibilityLines = chkBalancePossLines.IsChecked.Value;
            _balanceVisualizer.ShowPossibilityHull = chkBalancePossHull.IsChecked.Value;
        }
        private BalanceTestType GetBalanceTestType()
        {
            if (radBalanceIndividual.IsChecked.Value)
            {
                return BalanceTestType.Individual;
            }
            else if (radBalanceZeroTorque1.IsChecked.Value)
            {
                return BalanceTestType.ZeroTorque1;
            }
            else if (radBalanceZeroTorque2.IsChecked.Value)
            {
                return BalanceTestType.ZeroTorque2;
            }
            else if (radBalanceZeroTorque3.IsChecked.Value)
            {
                return BalanceTestType.ZeroTorque3;
            }
            else
            {
                throw new ApplicationException("Unknown balance test type");
            }
        }

        private void DrawMassBreakdown(UtilityNewt.IObjectMassBreakdown breakdown, double cellSize)
        {
            #region Draw masses as cubes

            double radMult = (cellSize * .75d) / breakdown.Max(o => o.Item2);
            DoubleVector dirFacing = new DoubleVector(1, 0, 0, 0, 1, 0);

            Model3DGroup geometries = new Model3DGroup();

            foreach (var pointMass in breakdown)
            {
                double radius = pointMass.Item2 * radMult;

                CollisionHull dummy1; Transform3DGroup dummy2; Quaternion dummy3; DiffuseMaterial dummy4;
                geometries.Children.Add(GetWPFGeometry(out dummy1, out dummy2, out dummy3, out dummy4, CollisionShapeType.Box, _colors.MassBall, _colors.MassBallReflect, _colors.MassBallReflectIntensity, new Vector3D(radius, radius, radius), pointMass.Item1, dirFacing, false));
            }

            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = geometries;
            _viewport.Children.Add(visual);
            _currentVisuals.Add(visual);

            #endregion
            #region Draw Axiis

            ScreenSpaceLines3D line = new ScreenSpaceLines3D();
            line.Color = Colors.DimGray;
            line.Thickness = 2d;
            line.AddLine(new Point3D(0, 0, 0), new Point3D(10, 0, 0));
            _viewport.Children.Add(line);
            _currentVisuals.Add(line);

            line = new ScreenSpaceLines3D();
            line.Color = Colors.Silver;
            line.Thickness = 2d;
            line.AddLine(new Point3D(0, 0, 0), new Point3D(0, 10, 0));
            _viewport.Children.Add(line);
            _currentVisuals.Add(line);

            line = new ScreenSpaceLines3D();
            line.Color = Colors.White;
            line.Thickness = 2d;
            line.AddLine(new Point3D(0, 0, 0), new Point3D(0, 0, 10));
            _viewport.Children.Add(line);
            _currentVisuals.Add(line);

            #endregion
        }

        // These 3 methods are copied from QuickHull2D
        private static Point[] GetRotatedPoints(Point3D[] points)
        {
            // Make sure they are coplanar, and get a triangle that represents that plane
            ITriangle triangle = GetThreeCoplanarPoints(points);
            if (triangle == null)
            {
                return null;
            }

            // Figure out a transform that will make Z drop out
            Transform3D transform = GetTransformTo2D(triangle);

            // Transform them
            Point[] retVal = new Point[points.Length];
            for (int cntr = 0; cntr < points.Length; cntr++)
            {
                Point3D transformed = transform.Transform(points[cntr]);
                retVal[cntr] = new Point(transformed.X, transformed.Y);
            }

            // Exit Function
            return retVal;
        }
        //NOTE: This also makes sure that all the points lie in the same plane as the returned triangle (some of the points may still be
        //colinear or the same, but at least they are on the same plane)
        private static ITriangle GetThreeCoplanarPoints(Point3D[] points)
        {
            Vector3D? line1 = null;
            Vector3D? line1Unit = null;

            ITriangle retVal = null;

            for (int cntr = 1; cntr < points.Length; cntr++)
            {
                if (Math3D.IsNearValue(points[0], points[cntr]))
                {
                    // These points are sitting on top of each other
                    continue;
                }

                Vector3D line = points[cntr] - points[0];

                if (line1 == null)
                {
                    // Found the first line
                    line1 = line;
                    line1Unit = line.ToUnit();
                    continue;
                }

                if (retVal == null)
                {
                    if (!Math3D.IsNearValue(Math.Abs(Vector3D.DotProduct(line1Unit.Value, line.ToUnit())), 1d))
                    {
                        // These two lines aren't colinear.  Found the second line
                        retVal = new Triangle(points[0], points[0] + line1.Value, points[cntr]);
                    }

                    continue;
                }

                double dot = Vector3D.DotProduct(retVal.Normal, line);
                if (!Math3D.IsNearZero(dot))
                {
                    // This point isn't coplanar with the triangle
                    return null;
                }
            }

            // Exit Function
            return retVal;
        }
        private static Transform3D GetTransformTo2D(ITriangle triangle)
        {
            Vector3D line1 = triangle.Point1 - triangle.Point0;

            DoubleVector from = new DoubleVector(line1, Math3D.GetOrthogonal(line1, triangle.Point2 - triangle.Point0));
            DoubleVector to = new DoubleVector(new Vector3D(1, 0, 0), new Vector3D(0, 1, 0));

            Quaternion rotation = from.GetRotation(to);

            Transform3DGroup retVal = new Transform3DGroup();

            // Rotate
            retVal.Children.Add(new RotateTransform3D(new QuaternionRotation3D(rotation)));

            // Then translate
            retVal.Children.Add(new TranslateTransform3D(0, 0, -triangle.Point0.Z));

            return retVal;
        }

        #endregion
    }
}
