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
using System.Windows.Shapes;
using System.Windows.Media.Media3D;

using Game.Newt.HelperClasses;
using Game.Newt.HelperClasses.Primitives3D;
using Game.HelperClasses;
using System.IO;

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

        #endregion

        #region Constructor

        public PotatoWindow()
        {
            InitializeComponent();

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
                _trackball.Mappings.AddRange(TrackBallMapping.GetPrebuilt(TrackBallMapping.PrebuiltMapping.MouseComplete));
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
                    AddDot(Math3D.GetRandomVectorSpherical2D(MAXRADIUS).ToPoint(), DOTRADIUS, _colors.MedSlate);
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
                    AddDot(Math3D.GetRandomVectorSpherical2D(MAXRADIUS * .7d, MAXRADIUS).ToPoint(), DOTRADIUS, _colors.MedSlate);
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
                    AddDot(Math3D.GetRandomVectorSphericalShell2D(MAXRADIUS).ToPoint(), DOTRADIUS, _colors.MedSlate);
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
                    AddDot(Math3D.GetRandomVectorSpherical(MAXRADIUS).ToPoint(), DOTRADIUS, _colors.MedSlate);
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
                    AddDot(Math3D.GetRandomVectorSpherical(.9d * MAXRADIUS, MAXRADIUS).ToPoint(), DOTRADIUS, _colors.MedSlate);
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
                    AddDot(Math3D.GetRandomVectorSphericalShell(MAXRADIUS).ToPoint(), DOTRADIUS, _colors.MedSlate);
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
                    points.Add(Math3D.GetRandomVectorSpherical2D(minRadius, MAXRADIUS).ToPoint());

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
                    points.Add(Math3D.GetRandomVectorSpherical(minRadius, MAXRADIUS).ToPoint());

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
                points[0] = Math3D.GetRandomVectorSpherical(MAXRADIUS).ToPoint();
                points[1] = Math3D.GetRandomVectorSpherical(MAXRADIUS).ToPoint();
                points[2] = Math3D.GetRandomVectorSpherical(MAXRADIUS).ToPoint();
                TriangleIndexed triangle = new TriangleIndexed(0, 1, 2, points);

                // Create random points within that triangle
                for (int cntr = 0; cntr < trkNumPoints.Value; cntr++)
                {
                    //Point3D insidePoint = Math3D.GetRandomPointInTriangle(_rand, triangle.Point0, triangle.Point1, triangle.Point2);
                    Point3D insidePoint = Math3D.GetRandomPointInTriangle(triangle);
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
                    allPoints[cntr * 3] = Math3D.GetRandomVectorSpherical(MAXRADIUS).ToPoint();
                    allPoints[(cntr * 3) + 1] = Math3D.GetRandomVectorSpherical(MAXRADIUS).ToPoint();
                    allPoints[(cntr * 3) + 2] = Math3D.GetRandomVectorSpherical(MAXRADIUS).ToPoint();

                    triangles[cntr] = new TriangleIndexed(cntr * 3, (cntr * 3) + 1, (cntr * 3) + 2, allPoints);
                }

                // Create random points within those triangles
                Point3D[] points = Math3D.GetRandomPointsOnHull(triangles, Convert.ToInt32(trkNumPoints.Value));

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
                Point3D[] points = Math3D.GetRandomPointsOnHull(triangles, Convert.ToInt32(trkNumPoints.Value));

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
                Point3D[] points = Math3D.GetRandomPointsOnHull(origTriangles, Convert.ToInt32(trkNumPoints.Value));

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
                SortedList<int, List<Point3D>> points = Math3D.GetRandomPointsOnHull_Structured(origTriangles, Convert.ToInt32(trkNumPoints.Value));



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
                Point3D[] points = Math3D.GetRandomPointsOnHull(origTriangles, Convert.ToInt32(trkNumPoints.Value));

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
                SortedList<int, List<Point3D>> points = Math3D.GetRandomPointsOnHull_Structured(origTriangles, Convert.ToInt32(trkNumPoints.Value));

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
                    allPoints[cntr] = Math3D.GetRandomVectorSpherical(MAXRADIUS).ToPoint();
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
                    points.Add(Math3D.GetRandomVectorSpherical(.6d * MAXRADIUS, MAXRADIUS).ToPoint());
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
                    AddHullTest(new TriangleIndexed[] { removedTriangle }, true, true, _colors.HullFaceRemoved, _colors.HullFaceSpecularRemoved);
                }

                if (otherRemovedTriangles != null)
                {
                    AddHullTest(otherRemovedTriangles, true, true, _colors.HullFaceOtherRemoved, _colors.HullFaceSpecularOtherRemoved);
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
                    points.Add(Math3D.GetRandomVectorSpherical(.6d * MAXRADIUS, MAXRADIUS).ToPoint());
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
                    points.Add(Math3D.GetRandomVectorSpherical(.6d * MAXRADIUS, MAXRADIUS).ToPoint());
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
                    seeds.Add(Math3D.GetRandomVectorSpherical(MAXRADIUS));
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

                foreach (int[] combo in UtilityHelper.AllCombosEnumerator(seeds.Count))
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
            foreach (ModelVisual3D visual in _visuals)
            {
                if (_viewport.Children.Contains(visual))
                {
                    _viewport.Children.Remove(visual);
                }
            }

            _visuals.Clear();
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
            geometry.Geometry = UtilityWPF.GetSphere(3, radius, radius, radius);

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
        private void AddHull(Triangle[] triangles, bool drawLines, bool drawNormals)
        {
            if (drawLines)
            {
                #region Lines

                // Draw the lines
                ScreenSpaceLines3D lineVisual = new ScreenSpaceLines3D(true);
                lineVisual.Thickness = LINETHICKNESS;
                lineVisual.Color = _colors.DarkSlate;

                // TODO:  Dedupe the lines (that would be a good static method off of triangle
                foreach (Triangle triangle in triangles)
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

                foreach (Triangle triangle in triangles)
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
        private void AddHull(TriangleIndexed[] triangles, bool drawFaces, bool drawLines, bool drawNormals, bool includeEveryOtherFace, bool nearlyTransparent, bool softFaces)
        {
            if (drawLines)
            {
                #region Lines

                // Draw the lines
                ScreenSpaceLines3D lineVisual = new ScreenSpaceLines3D(true);
                lineVisual.Thickness = nearlyTransparent ? LINETHICKNESS * .5d : LINETHICKNESS;
                lineVisual.Color = _colors.DarkSlate;

                // TODO:  Dedupe the lines (that would be a good static method off of triangle
                foreach (TriangleIndexed triangle in triangles)
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
                    List<TriangleIndexed> trianglesEveryOther = new List<TriangleIndexed>();
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
        private void AddHullTest(TriangleIndexed[] triangles, bool drawLines, bool drawNormals, Color faceColor, SpecularMaterial faceSpecular)
        {
            if (drawLines)
            {
                #region Lines

                // Draw the lines
                ScreenSpaceLines3D lineVisual = new ScreenSpaceLines3D(true);
                lineVisual.Thickness = LINETHICKNESS;
                lineVisual.Color = _colors.DarkSlate;

                // TODO:  Dedupe the lines (that would be a good static method off of triangle
                foreach (TriangleIndexed triangle in triangles)
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
                    mesh = UtilityWPF.GetSphere(5, GetRandomSize(MINRADIUS, MAXRADIUS));
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
                Point3D nearestPoint = Math3D.GetClosestPoint_Point_Line(pointOnLine, lineDirection, point);
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

            Point3D nearestPoint = Math3D.GetClosestPoint_Point_Line(pointOnLine, lineDirection, point);

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

            Point3D nearestPoint = Math3D.GetClosestPoint_Point_Line(pointOnLine, lineDirection, point);

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
                Point3D nearestPoint = Math3D.GetClosestPoint_Point_Line(startPoint, lineDirection, points[cntr]);
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

                double distance = Math3D.DistanceFromPlane(triangle, points[cntr].ToVector());
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
                Point3D nearestPoint = Math3D.GetClosestPoint_Point_Line(startPoint, lineDirection, points[cntr]);
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
            public TriangleWithPoints()
                : base()
            {
                this.OutsidePoints = new List<int>();
            }

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
                Point3D nearestPoint = Math3D.GetClosestPoint_Point_Line(startPoint, lineDirection, points[cntr]);
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
            public TriangleWithPoints()
                : base()
            {
                this.OutsidePoints = new List<int>();
            }

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

            double smallestAxis = Math3D.Min(maxValues[0] - minValues[0], maxValues[1] - minValues[1], maxValues[2] - minValues[2]);
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
                Point3D nearestPoint = Math3D.GetClosestPoint_Point_Line(startPoint, lineDirection, points[cntr]);
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
                        nearestPoint = Math3D.GetClosestPoint_Point_Line(triangle.Point0, triangle.Point1 - triangle.Point0, point);
                        break;

                    case TriangleEdge.Edge_12:
                        nearestPoint = Math3D.GetClosestPoint_Point_Line(triangle.Point1, triangle.Point2 - triangle.Point1, point);
                        break;

                    case TriangleEdge.Edge_20:
                        nearestPoint = Math3D.GetClosestPoint_Point_Line(triangle.Point0, triangle.Point2 - triangle.Point0, point);
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
            public TriangleWithPoints()
                : base()
            {
                this.OutsidePoints = new List<int>();
            }

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
                Point3D nearestPoint = Math3D.GetClosestPoint_Point_Line(startPoint, lineDirection, points[cntr]);
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
                else if (Math3D.IsNearZero(distance) && Math3D.IsNearZero(maxDistance))		// this is for a coplanar point that can have a very slightly negative distance
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
            if (dot >= 0d || Math3D.IsNearZero(dot))		// 0 is coplanar, -1 is the opposite side
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
                else if (Math3D.IsNearZero(res))
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
}
