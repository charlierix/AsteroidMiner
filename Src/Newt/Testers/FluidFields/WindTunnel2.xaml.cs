using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
using Game.HelperClassesWPF;
using Game.HelperClassesWPF.Primitives3D;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.Testers.FluidFields
{
    //TODO:
    // After this is tester is complete, make an octree of fluid maps.  This way, high resolution can be used around bodies, and low resolution will
    // preserve some of the state of the fluid (and when a new high resolution block is needed, it can be initialized from the low resolution parents)

    public partial class WindTunnel2 : Window
    {
        #region Class: FluidVisual

        /// <summary>
        /// This is a wrapper to a line that moves through the world at the speed of the fluid
        /// </summary>
        /// <remarks>
        /// This is a copy of WindTunnelWindow.FluidVisual.  May want to make a subfolder of useful classes like this
        /// </remarks>
        private class FluidVisual : IDisposable
        {
            #region Declaration Section

            private Viewport3D _viewport = null;
            private ScreenSpaceLines3D _line = null;

            private RotateTransform3D _worldFlowRotation = null;

            #endregion

            #region Constructor

            /// <summary>
            /// NOTE:  Keep the model coords along X.  The line will be rotated into world coords
            /// </summary>
            public FluidVisual(Viewport3D viewport, Point3D modelFromPoint, Point3D modelToPoint, Point3D worldStartPoint, Vector3D worldFlow, Color color, double maxDistance)
            {
                _viewport = viewport;
                this.Position = worldStartPoint;
                this.WorldFlow = worldFlow;
                this.MaxDistance = maxDistance;

                _line = new ScreenSpaceLines3D(false);
                _line.Thickness = 1d;
                _line.Color = color;
                _line.AddLine(modelFromPoint, modelToPoint);

                UpdateTransform();

                _viewport.Children.Add(_line);
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
                    if (_viewport != null && _line != null)
                    {
                        _viewport.Children.Remove(_line);
                        _viewport = null;
                        _line = null;
                    }
                }
            }

            #endregion

            #region Public Properties

            private Vector3D _worldFlow;
            public Vector3D WorldFlow
            {
                get
                {
                    return _worldFlow;
                }
                set
                {
                    _worldFlow = value;

                    Vector3D axis;
                    double radians;
                    Math3D.GetRotation(out axis, out radians, new Vector3D(-1, 0, 0), _worldFlow);
                    _worldFlowRotation = new RotateTransform3D(new AxisAngleRotation3D(axis, Math3D.RadiansToDegrees(radians)));
                }
            }

            public Point3D Position
            {
                get;
                private set;
            }

            /// <summary>
            /// When the line gets farther from the center than this, then it should be removed
            /// </summary>
            public double MaxDistance
            {
                get;
                private set;
            }

            #endregion

            #region Public Methods

            /// <summary>
            /// Moves the line through the world
            /// </summary>
            public void Update(double elapsedTime)
            {
                this.Position += this.WorldFlow * elapsedTime;

                UpdateTransform();
            }

            #endregion

            #region Private Methods

            private void UpdateTransform()
            {
                Transform3DGroup transform = new Transform3DGroup();
                transform.Children.Add(_worldFlowRotation);
                transform.Children.Add(new TranslateTransform3D(this.Position.ToVector()));

                _line.Transform = transform;

                _line.CalculateGeometry();
            }

            #endregion
        }

        #endregion
        #region Class: ItemColors

        private class ItemColors
        {
            public Color ForceLine = UtilityWPF.AlphaBlend(Colors.HotPink, Colors.Plum, .25d);

            public Color HullFace = UtilityWPF.AlphaBlend(Colors.Ivory, Colors.Transparent, .2d);
            public SpecularMaterial HullFaceSpecular = new SpecularMaterial(new SolidColorBrush(Color.FromArgb(255, 86, 68, 226)), 100d);
            public Color HullWireFrame = UtilityWPF.AlphaBlend(Colors.Ivory, Colors.Transparent, .3d);

            public Color GhostBodyFace = Color.FromArgb(40, 192, 192, 192);
            public SpecularMaterial GhostBodySpecular = new SpecularMaterial(new SolidColorBrush(Color.FromArgb(96, 86, 68, 226)), 25);

            public Color Anchor = Colors.Gray;
            public SpecularMaterial AnchorSpecular = new SpecularMaterial(new SolidColorBrush(Colors.Silver), 50d);
            public Color Rope = Colors.Silver;
            public SpecularMaterial RopeSpecular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(Colors.Silver, Colors.White, .5d)), 25d);

            public Color FluidLine
            {
                get
                {
                    return UtilityWPF.GetRandomColor(255, 153, 168, 186, 191, 149, 166);
                }
            }

            public DiffuseMaterial TrackballAxisMajor = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(255, 147, 98, 229)));
            public DiffuseMaterial TrackballAxisMinor = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(255, 127, 112, 153)));
            public Color TrackballAxisLine = Color.FromArgb(96, 117, 108, 97);
            public SpecularMaterial TrackballAxisSpecular = new SpecularMaterial(Brushes.White, 100d);

            public Color TrackballGrabberHoverLight = Color.FromArgb(255, 74, 37, 138);

            public Color BlockedCell = UtilityWPF.ColorFromHex("60BAE5B1");
            public Color FieldBoundry = UtilityWPF.ColorFromHex("40B5C9B1");
        }

        #endregion
        #region Class: BlockedCellCalc1

        private static class BlockedCellCalc1
        {
            public static int[] GetBlockedCells(FluidField3D field, ITriangleIndexed[] hull)
            {
                return GetBlockedCells(field, new ITriangleIndexed[][] { hull });
            }
            public static int[] GetBlockedCells(FluidField3D field, IEnumerable<ITriangleIndexed[]> hulls)
            {
                //NOTE: The hulls need to be in model coords

                // Get populated hulls
                ITriangleIndexed[][] hulls1 = hulls.Where(o => o.Length > 0).ToArray();
                if (hulls1.Length == 0)
                {
                    return new int[0];      // no hulls were passed in
                }

                // Get the location of all the cells
                Rectangle3DIndexedMapped[] cells1 = field.GetCells();

                #region Filter AABB

                // Remove non aabb matches
                Rect3D[] aabbs = hulls1.Select(o => Math3D.GetAABB_Rect(o[0].AllPoints)).ToArray();

                Rectangle3DIndexedMapped[] cells2 = cells1.Where(o =>
                {
                    Rect3D cellRect = o.ToRect3D();
                    return aabbs.Any(p => p.OverlapsWith(cellRect));
                }).ToArray();

                if (cells2.Length == 0)
                {
                    return new int[0];      // this should never happen
                }

                #endregion

                //TODO: When multiple hulls are passed in, ignore cells that are outside of a particular hull's aabb

                #region Test points

                // Process the corners of the cells
                int[] cornerMatches = GetBlockedCellsSprtPointMap(cells2).
                    //AsParallel().
                    Where(o => hulls1.Any(p => Math3D.IsInside_ConcaveHull(p, o.Item1))).
                    SelectMany(o => o.Item2.Select(p => cells2[p].Mapping.Offset1D)).       // add all the cells that neighbor this corner point
                    Distinct().     // need distinct, since cells were added multiple times
                    ToArray();

                // Process the centers of the cells that didn't have a corner match
                int[] centerMatches = cells2.
                    Where(o => !cornerMatches.Contains(o.Mapping.Offset1D)).
                    AsParallel().
                    Where(o =>
                    {
                        Point3D centerPoint = o.AABBMin + ((o.AABBMax - o.AABBMin) * .5d);
                        return hulls1.Any(p => Math3D.IsInside_ConcaveHull(p, centerPoint));        //NOTE: it's faster to use the single threaded overload in this case
                    }).
                    Select(o => o.Mapping.Offset1D).
                    ToArray();

                #endregion

                #region Test triangles

                // Look for intersecting edges
                Rectangle3DIndexedMapped[] cells3 = cells2.
                    Where(o => !cornerMatches.Contains(o.Mapping.Offset1D) && !centerMatches.Contains(o.Mapping.Offset1D)).
                    ToArray();

                int[] edgeMatches = null;
                if (cells3.Length > 0)
                {
                    // aabbs is for entire hulls.  This needs to be for each triangle in each hull
                    Tuple<ITriangle, Rect3D>[] hullTrianglesAABBs = hulls.SelectMany(o => o.Select(p => new Tuple<ITriangle, Rect3D>(p, Math3D.GetAABB_Rect(p)))).ToArray();

                    // Now compare each remaining candidate cell to each triangle of each hull
                    edgeMatches = GetBlockedCellsSprtEdgeMap(cells3).
                        AsParallel().
                        Where(o => hullTrianglesAABBs.Any(p => IsEdgeMatch(p, o.Item1))).
                        SelectMany(o => o.Item2.Select(p => cells3[p].Mapping.Offset1D)).       // add all the cells that neighbor this corner point
                        Distinct().     // need distinct, since cells were added multiple times
                        ToArray();
                }

                #endregion

                // Exit Function
                return UtilityCore.Iterate(cornerMatches, centerMatches, edgeMatches).ToArray();
            }

            #region Private Methods

            /// <summary>
            /// This returns all the unique corner points of the cells passed in, and which cells share each of those points
            /// </summary>
            private static Tuple<Point3D, int[]>[] GetBlockedCellsSprtPointMap(Rectangle3DIndexedMapped[] cells)
            {
                if (cells.Length == 0)
                {
                    return new Tuple<Point3D, int[]>[0];
                }

                // Build an intermediate map
                SortedList<int, List<int>> points_cells = new SortedList<int, List<int>>();

                for (int cntr = 0; cntr < cells.Length; cntr++)
                {
                    foreach (int index in cells[cntr].Indices)
                    {
                        if (!points_cells.ContainsKey(index))
                        {
                            points_cells.Add(index, new List<int>());
                        }

                        points_cells[index].Add(cntr);
                    }
                }

                Point3D[] allPoints = cells[0].AllPoints;

                // Build the final map
                List<Tuple<Point3D, int[]>> retVal = new List<Tuple<Point3D, int[]>>();

                foreach (int pointIndex in points_cells.Keys)
                {
                    retVal.Add(Tuple.Create(allPoints[pointIndex], points_cells[pointIndex].ToArray()));
                }

                // Exit Function
                return retVal.ToArray();
            }
            /// <summary>
            /// This returns all the unique edge triangles of the cells passed in (each face of the cell cube has two triangles), and which
            /// cells share each of those triangles
            /// </summary>
            private static Tuple<ITriangle, int[]>[] GetBlockedCellsSprtEdgeMap(Rectangle3DIndexedMapped[] cells)
            {
                if (cells.Length == 0)
                {
                    return new Tuple<ITriangle, int[]>[0];
                }

                // Get the triangles for each cell
                List<Tuple<int, Tuple<int, int, int>>> trianglesPerCell = new List<Tuple<int, Tuple<int, int, int>>>();

                for (int cntr = 0; cntr < cells.Length; cntr++)
                {
                    Tuple<int, int, int>[] trianglesOrdered = cells[cntr].GetEdgeTriangles().Select(o =>
                    {
                        int[] indices = o.IndexArray.OrderBy(p => p).ToArray();     // sorting them so they can be easily compared to other cell's triangles
                        return Tuple.Create(indices[0], indices[1], indices[2]);
                    }).
                    ToArray();

                    trianglesPerCell.AddRange(trianglesOrdered.Select(o => Tuple.Create(cntr, o)));
                }

                Point3D[] points = cells[0].AllPoints;

                // Now group by triangle
                var retVal = trianglesPerCell.GroupBy(o => o.Item2).
                    Select(o =>
                    {
                        ITriangle triangle = new TriangleIndexed(o.Key.Item1, o.Key.Item2, o.Key.Item3, points);

                        int[] correspondingCells = o.Select(p => p.Item1).Distinct().ToArray();

                        return Tuple.Create(triangle, correspondingCells);
                    }).
                    ToArray();

                // Exit Function
                return retVal;
            }

            private static bool IsEdgeMatch(Tuple<ITriangle, Rect3D> triangle1, ITriangle triangle2)
            {
                // AABB
                if (!triangle1.Item2.OverlapsWith(Math3D.GetAABB_Rect(triangle2)))
                {
                    return false;
                }

                // Triangle
                return Math3D.GetIntersection_Triangle_Triangle(triangle1.Item1, triangle2) != null;
            }

            #endregion
        }

        #endregion
        #region Class: BlockedCellCalc2

        // This gets rid of point matching and just does edge mapping.  But appears to run slower
        private static class BlockedCellCalc2
        {
            public static int[] GetBlockedCells(FluidField3D field, ITriangleIndexed[] hull)
            {
                return GetBlockedCells(field, new ITriangleIndexed[][] { hull });
            }
            public static int[] GetBlockedCells(FluidField3D field, IEnumerable<ITriangleIndexed[]> hulls)
            {
                //NOTE: The hulls need to be in model coords

                // Get populated hulls
                ITriangleIndexed[][] hulls1 = hulls.Where(o => o.Length > 0).ToArray();
                if (hulls1.Length == 0)
                {
                    return new int[0];      // no hulls were passed in
                }

                // Get the location of all the cells
                Rectangle3DIndexedMapped[] cells1 = field.GetCells();

                #region Filter AABB

                // Remove non aabb matches
                Rect3D[] aabbs = hulls1.Select(o => Math3D.GetAABB_Rect(o[0].AllPoints)).ToArray();

                Rectangle3DIndexedMapped[] cells2 = cells1.Where(o =>
                {
                    Rect3D cellRect = o.ToRect3D();
                    return aabbs.Any(p => p.OverlapsWith(cellRect));
                }).ToArray();

                if (cells2.Length == 0)
                {
                    return new int[0];      // this should never happen
                }

                #endregion

                //TODO: When multiple hulls are passed in, ignore cells that are outside of a particular hull's aabb

                #region Test triangles

                // Look for intersecting edges
                int[] edgeMatches = new int[0];
                if (cells2.Length > 0)
                {
                    // aabbs is for entire hulls.  This needs to be for each triangle in each hull
                    Tuple<ITriangle, Rect3D>[] hullTrianglesAABBs = hulls.SelectMany(o => o.Select(p => new Tuple<ITriangle, Rect3D>(p, Math3D.GetAABB_Rect(p)))).ToArray();

                    // Now compare each remaining candidate cell to each triangle of each hull
                    edgeMatches = GetBlockedCellsSprtEdgeMap(cells2).
                        AsParallel().
                        Where(o => hullTrianglesAABBs.Any(p => IsEdgeMatch(p, o.Item1))).
                        SelectMany(o => o.Item2.Select(p => cells2[p].Mapping.Offset1D)).       // add all the cells that neighbor this corner point
                        Distinct().     // need distinct, since cells were added multiple times
                        ToArray();
                }

                #endregion

                // Exit Function
                return edgeMatches;
            }

            #region Private Methods

            /// <summary>
            /// This returns all the unique corner points of the cells passed in, and which cells share each of those points
            /// </summary>
            private static Tuple<Point3D, int[]>[] GetBlockedCellsSprtPointMap(Rectangle3DIndexedMapped[] cells)
            {
                if (cells.Length == 0)
                {
                    return new Tuple<Point3D, int[]>[0];
                }

                // Build an intermediate map
                SortedList<int, List<int>> points_cells = new SortedList<int, List<int>>();

                for (int cntr = 0; cntr < cells.Length; cntr++)
                {
                    foreach (int index in cells[cntr].Indices)
                    {
                        if (!points_cells.ContainsKey(index))
                        {
                            points_cells.Add(index, new List<int>());
                        }

                        points_cells[index].Add(cntr);
                    }
                }

                Point3D[] allPoints = cells[0].AllPoints;

                // Build the final map
                List<Tuple<Point3D, int[]>> retVal = new List<Tuple<Point3D, int[]>>();

                foreach (int pointIndex in points_cells.Keys)
                {
                    retVal.Add(Tuple.Create(allPoints[pointIndex], points_cells[pointIndex].ToArray()));
                }

                // Exit Function
                return retVal.ToArray();
            }
            /// <summary>
            /// This returns all the unique edge triangles of the cells passed in (each face of the cell cube has two triangles), and which
            /// cells share each of those triangles
            /// </summary>
            private static Tuple<ITriangle, int[]>[] GetBlockedCellsSprtEdgeMap(Rectangle3DIndexedMapped[] cells)
            {
                if (cells.Length == 0)
                {
                    return new Tuple<ITriangle, int[]>[0];
                }

                // Get the triangles for each cell
                List<Tuple<int, Tuple<int, int, int>>> trianglesPerCell = new List<Tuple<int, Tuple<int, int, int>>>();

                for (int cntr = 0; cntr < cells.Length; cntr++)
                {
                    Tuple<int, int, int>[] trianglesOrdered = cells[cntr].GetEdgeTriangles().Select(o =>
                    {
                        int[] indices = o.IndexArray.OrderBy(p => p).ToArray();     // sorting them so they can be easily compared to other cell's triangles
                        return Tuple.Create(indices[0], indices[1], indices[2]);
                    }).
                    ToArray();

                    trianglesPerCell.AddRange(trianglesOrdered.Select(o => Tuple.Create(cntr, o)));
                }

                Point3D[] points = cells[0].AllPoints;

                // Now group by triangle
                var retVal = trianglesPerCell.GroupBy(o => o.Item2).
                    Select(o =>
                    {
                        ITriangle triangle = new TriangleIndexed(o.Key.Item1, o.Key.Item2, o.Key.Item3, points);

                        int[] correspondingCells = o.Select(p => p.Item1).Distinct().ToArray();

                        return Tuple.Create(triangle, correspondingCells);
                    }).
                    ToArray();

                // Exit Function
                return retVal;
            }

            private static bool IsEdgeMatch(Tuple<ITriangle, Rect3D> triangle1, ITriangle triangle2)
            {
                // AABB
                if (!triangle1.Item2.OverlapsWith(Math3D.GetAABB_Rect(triangle2)))
                {
                    return false;
                }

                // Triangle
                return Math3D.GetIntersection_Triangle_Triangle(triangle1.Item1, triangle2) != null;
            }

            #endregion
        }

        #endregion

        #region Declaration Section

        private const int NUMFLUIDVISUALS = 50;
        private const double FLUIDVISUALMAXPOS = 25d;

        private ItemColors _colors = new ItemColors();

        private World _world = null;

        private FluidField3D _field = null;
        private Quaternion _fieldRotation = Quaternion.Identity;
        private Rectangle3DIndexedMapped[] _fieldCells = null;
        private FluidFieldUniform _fieldUniform = null;
        private FluidFieldField _fieldField = null;

        private VelocityVisualizer3DWindow _velocityVisualizerWindow = null;

        // This is the hull and body of what's being tested
        private FluidHull _fluidHull = null;
        //private Body _body = null;

        // Visual of the body that is placed in the fluid
        private MeshGeometry3D _bodyMesh = null;
        private ModelVisual3D _bodyVisual = null;
        private ScreenSpaceLines3D _bodyWireframe = null;

        private ScreenSpaceLines3D _blockedCellsWireframe = null;
        private ScreenSpaceLines3D _fieldBoundryWireframe = null;

        private ScreenSpaceLines3D _fluidHullForceLines = null;

        private List<FluidVisual> _fluidVisuals = new List<FluidVisual>();

        private List<Visual3D> _debugVisuals = new List<Visual3D>();

        /// <summary>
        /// This listens to the mouse/keyboard and controls the camera
        /// </summary>
        private TrackBallRoam _trackball = null;

        private List<ModelVisual3D> _modelOrientationVisuals = new List<ModelVisual3D>();
        private TrackballGrabber _modelOrientationTrackball = null;

        private List<ModelVisual3D> _flowOrientationVisuals = new List<ModelVisual3D>();
        private TrackballGrabber _flowOrientationTrackball = null;
        private double _flowViscosity = 1d;

        private bool _isInitialized = false;

        #endregion

        #region Constructor

        public WindTunnel2()
        {
            InitializeComponent();
        }

        #endregion

        #region Event Listeners

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Init World
                _world = new World();
                _world.Updating += new EventHandler<WorldUpdatingArgs>(World_Updating);
                _world.SetWorldSize(new Point3D(-100, -100, -100), new Point3D(100, 100, 100));
                _world.UnPause();

                // Camera Trackball
                _trackball = new TrackBallRoam(_camera);
                //_trackball.ShouldHitTestOnOrbit = true;
                _trackball.EventSource = grdViewPort;		//NOTE:  If this control doesn't have a background color set, the trackball won't see events (I think transparent is ok, just not null)
                _trackball.AllowZoomOnMouseWheel = true;
                _trackball.Mappings.AddRange(TrackBallMapping.GetPrebuilt(TrackBallMapping.PrebuiltMapping.MouseComplete));
                //_trackball.GetOrbitRadius += new GetOrbitRadiusHandler(Trackball_GetOrbitRadius);

                // Trackball Controls
                SetupModelTrackball();
                SetupFlowTrackball();

                // Fluid lines
                AddFluidVisuals(Convert.ToInt32(NUMFLUIDVISUALS * _flowViscosity));

                // Force lines
                _fluidHullForceLines = new ScreenSpaceLines3D(true);
                _fluidHullForceLines.Color = _colors.ForceLine;
                _fluidHullForceLines.Thickness = 2d;

                _viewport.Children.Add(_fluidHullForceLines);

                // Field - may want to only add this when a body is added
                _field = new FluidField3D(20);
                _field.Diffusion = 0;
                _field.Damping = 0; //GetFluidViscocity();        // this stays zero (see comments in VelocityViscocityChanged)

                _field.BoundryType = FluidFieldBoundryType3D.Open_Slaved;
                _fieldUniform = new FluidFieldUniform();
                _field.OpenBoundryParent = _fieldUniform;
                _field.SizeWorld = 10d;     // This changes whenever a body is swapped out

                _fieldCells = _field.GetCells();

                _fieldField = new FluidFieldField(_field);
                _fieldField.Viscosity = GetFluidViscocity();

                _isInitialized = true;

                ShowHideFluidBoundry();
                ShowHideBlockedCells();

                VelocityViscocityChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            try
            {
                if (_velocityVisualizerWindow != null)
                {
                    _velocityVisualizerWindow.Close();        // the closed event will fire, and this form will unhook from the viewer there
                }

                _world.Pause();
                _world.Dispose();
                _world = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void World_Updating(object sender, WorldUpdatingArgs e)
        {
            if (_field != null)
            {
                _field.Update();
            }

            if (_velocityVisualizerWindow != null)
            {
                _velocityVisualizerWindow.Update();
            }

            //if (_body == null)
            //{
            // When the body is null, the fluid object is static (if there is one at all).  If it's not null, this needs to be called from the
            // body's apply force/torque event
            UpdateFluidForces();
            //}

            // These are the visuals that fly along the fluid direction
            UpdateFlowLines(e.ElapsedTime);
        }
        private void Body_ApplyForceAndTorque(object sender, BodyApplyForceAndTorqueArgs e)
        {
        }

        private void ModelOrientationTrackball_RotationChanged(object sender, EventArgs e)
        {
            try
            {
                if (_bodyMesh != null)
                {
                    //var backup = _bodyMesh;
                    //AddNewBody(backup, false);      //TODO: This is overkill, make a separate method that just updates the trasnforms/blocked cells
                    //_bodyMesh = backup;     // Add removes _bodyMesh, so put it back

                    RotateModel(_bodyMesh);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void FlowOrientationTrackball_RotationChanged(object sender, EventArgs e)
        {
            try
            {
                VelocityViscocityChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void trkFlow_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (!_isInitialized)
                {
                    return;
                }

                VelocityViscocityChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Hyperlink_Velocity(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_velocityVisualizerWindow != null)
                {
                    _velocityVisualizerWindow.Focus();
                    return;
                }

                _velocityVisualizerWindow = new VelocityVisualizer3DWindow();
                _velocityVisualizerWindow.Closed += VelocityVisualizer_Closed;
                _velocityVisualizerWindow.Field = _field;
                _velocityVisualizerWindow.LinePlacement = VelocityVisualizer3DWindow.LinePlacementType.PlateXY;
                _velocityVisualizerWindow.Show();

                SetVelocityViewerCameraPosition();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void VelocityVisualizer_Closed(object sender, EventArgs e)
        {
            try
            {
                _velocityVisualizerWindow.Closed -= VelocityVisualizer_Closed;
                _velocityVisualizerWindow = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Camera_Changed(object sender, EventArgs e)
        {
            try
            {
                SetVelocityViewerCameraPosition();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void chkShowBlockedCells_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowHideBlockedCells();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void chkShowFluidBoundry_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowHideFluidBoundry();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnStaticCube_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double cubeSizeHalf = 1d;

                MeshGeometry3D mesh = UtilityWPF.GetCube_IndependentFaces(new Point3D(-cubeSizeHalf, -cubeSizeHalf, -cubeSizeHalf), new Point3D(cubeSizeHalf, cubeSizeHalf, cubeSizeHalf));

                AddNewBody(mesh, true);

                _bodyMesh = mesh;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnStaticSphere_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MeshGeometry3D mesh = UtilityWPF.GetSphere_LatLon(3, StaticRandom.NextDouble(.25, 1), StaticRandom.NextDouble(.25, 1), StaticRandom.NextDouble(.25, 1));

                AddNewBody(mesh, true);

                _bodyMesh = mesh;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnStaticTorus_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double radius = StaticRandom.NextDouble(.5, 1);

                MeshGeometry3D mesh = UtilityWPF.GetTorus(18, 6, radius * StaticRandom.NextDouble(.1, .95), radius);
                //MeshGeometry3D mesh = UtilityWPF.GetTorus(30, 10, radius * StaticRandom.NextDouble(.1, .95), radius);

                AddNewBody(mesh, true);

                _bodyMesh = mesh;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnStaticPlate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Vector3D half = new Vector3D(StaticRandom.NextDouble(.25, 1), StaticRandom.NextDouble(.25, 1), .01);

                MeshGeometry3D mesh = UtilityWPF.GetCube_IndependentFaces(-half, half);

                AddNewBody(mesh, true);

                _bodyMesh = mesh;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnStaticCone_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //MeshGeometry3D mesh = UtilityWPF.GetCone_AlongX(12, StaticRandom.NextDouble(.25, 1), StaticRandom.NextDouble(.25, 1));
                MeshGeometry3D mesh = UtilityWPF.GetCone_AlongX(7, StaticRandom.NextDouble(.25, 1), StaticRandom.NextDouble(.25, 1));

                AddNewBody(mesh, true);

                _bodyMesh = mesh;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnStaticCylinder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MeshGeometry3D mesh = UtilityWPF.GetCylinder_AlongX(12, StaticRandom.NextDouble(.25, 1), StaticRandom.NextDouble(.25, 1));

                AddNewBody(mesh, true);

                _bodyMesh = mesh;
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
            _bodyMesh = null;
            _fluidHull = null;

            _fluidHullForceLines.Clear();

            if (_bodyWireframe != null)
            {
                _viewport.Children.Remove(_bodyWireframe);
                _bodyWireframe = null;
            }

            if (_bodyVisual != null)
            {
                _viewport.Children.Remove(_bodyVisual);
                _bodyVisual = null;
            }

            foreach (Visual3D visual in _debugVisuals)
            {
                _viewport.Children.Remove(visual);
            }
            _debugVisuals.Clear();

            if (_field != null)
            {
                _field.SetBlockedCells(false);
            }

            ShowHideBlockedCells();
        }

        private void AddNewBody(MeshGeometry3D mesh, bool shouldChangeFieldSize)
        {
            RemoveCurrentBody();

            #region WPF Model

            // Material
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(_colors.HullFace)));
            materials.Children.Add(_colors.HullFaceSpecular);

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;
            geometry.Geometry = mesh;

            // Model Visual
            _bodyVisual = new ModelVisual3D();
            _bodyVisual.Content = geometry;
            _bodyVisual.Transform = _modelOrientationTrackball.Transform;

            #endregion

            #region Fluid Hull

            _fluidHull = new FluidHull();
            _fluidHull.Triangles = FluidHull.FluidTriangle.GetTrianglesFromMesh(mesh);
            _fluidHull.Transform = _modelOrientationTrackball.Transform;

            //_fluidHull.Field = _fieldUniform;
            _fluidHull.Field = _fieldField;

            #endregion

            ITriangleIndexed[] modelHull = UtilityWPF.GetTrianglesFromMesh(mesh);

            double farthestPosition = mesh.Positions.AsEnumerable().Max(o => Math3D.Max(Math.Abs(o.X), Math.Abs(o.Y), Math.Abs(o.Z)));

            if (shouldChangeFieldSize)
            {
                _field.SizeWorld = farthestPosition * 2d * (1.5d + (StaticRandom.NextDouble() * 1.5d));
                _fieldCells = _field.GetCells(false);       // need to rebuild this, because the world's size changed
            }

            // Wireframe
            _bodyWireframe = GetModelWireframe(modelHull);
            _bodyWireframe.Transform = _modelOrientationTrackball.Transform;
            _viewport.Children.Add(_bodyWireframe);

            // Add this last so the semi transparency looks right
            _viewport.Children.Add(_bodyVisual);

            RotateModel(mesh);

            ShowHideBlockedCells();
            ShowHideFluidBoundry();
        }

        private void RotateModel(MeshGeometry3D mesh)
        {
            _fluidHullForceLines.Clear();       // these hang while the blocked cells are recalculating, so just get rid of them

            if (_bodyVisual != null)
            {
                _bodyVisual.Transform = _modelOrientationTrackball.Transform;
            }

            if (_fluidHull != null)
            {
                _fluidHull.Transform = _modelOrientationTrackball.Transform;
            }

            if (_bodyWireframe != null)
            {
                _bodyWireframe.Transform = _modelOrientationTrackball.Transform;
            }

            if (_field != null)
            {
                //TODO: Just create a new set
                ITriangleIndexed[] modelHull = UtilityWPF.GetTrianglesFromMesh(mesh, _modelOrientationTrackball.Transform, false);

                _field.SetBlockedCells(false);
                _field.SetBlockedCells(BlockedCellCalc1.GetBlockedCells(_field, modelHull), true);
            }

            ShowHideBlockedCells();
        }

        private void UpdateFluidForces()
        {
            const double FLOWMULT = 15d;

            _fluidHullForceLines.Clear();

            if (_fluidHull != null)
            {
                _fluidHull.Update();

                foreach (FluidHull.FluidTriangle triangle in _fluidHull.Triangles)
                {
                    Point3D[] forcesAt;
                    Vector3D[] forces;
                    triangle.GetForces(out forcesAt, out forces);
                    if (forcesAt == null)
                    {
                        continue;
                    }

                    for (int cntr = 0; cntr < forcesAt.Length; cntr++)
                    {
                        Point3D pointEnd = forcesAt[cntr] + (forces[cntr] * FLOWMULT);      // artificially lengthening it so it's more visible

                        _fluidHullForceLines.AddLine(forcesAt[cntr], pointEnd);
                    }
                }
            }
        }

        private void UpdateFlowLines(double elapsedTime)
        {
            int index = 0;
            while (index < _fluidVisuals.Count)
            {
                _fluidVisuals[index].Update(elapsedTime);

                if (_fluidVisuals[index].Position.ToVector().LengthSquared > Math.Pow(_fluidVisuals[index].MaxDistance, 2d))
                {
                    _fluidVisuals[index].Dispose();
                    _fluidVisuals.RemoveAt(index);
                }
                else
                {
                    index++;
                }
            }

            int maxFluidVisuals = Convert.ToInt32(NUMFLUIDVISUALS * _flowViscosity);
            if (_fluidVisuals.Count < maxFluidVisuals)
            {
                AddFluidVisuals(maxFluidVisuals - _fluidVisuals.Count);
            }
        }

        private void SetupModelTrackball()
        {
            Model3DGroup model = new Model3DGroup();

            // Major arrow along x
            model.Children.Add(TrackballGrabber.GetMajorArrow(Axis.X, true, _colors.TrackballAxisMajor, _colors.TrackballAxisSpecular));

            // Minor arrow along z
            model.Children.Add(TrackballGrabber.GetMinorArrow(Axis.Z, true, _colors.TrackballAxisMinor, _colors.TrackballAxisSpecular));

            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = model;

            _viewportModelRotate.Children.Add(visual);
            _modelOrientationVisuals.Add(visual);

            // Create the trackball
            _modelOrientationTrackball = new TrackballGrabber(grdModelRotateViewport, _viewportModelRotate, 1d, _colors.TrackballGrabberHoverLight);
            _modelOrientationTrackball.SyncedLights.Add(_lightModel1);
            _modelOrientationTrackball.RotationChanged += new EventHandler(ModelOrientationTrackball_RotationChanged);

            // Faint lines
            _modelOrientationTrackball.HoverVisuals.Add(TrackballGrabber.GetGuideLine(Axis.X, false, _colors.TrackballAxisLine));
            _modelOrientationTrackball.HoverVisuals.Add(TrackballGrabber.GetGuideLineDouble(Axis.Y, _colors.TrackballAxisLine));
            _modelOrientationTrackball.HoverVisuals.Add(TrackballGrabber.GetGuideLine(Axis.Z, false, _colors.TrackballAxisLine));
        }
        private void SetupFlowTrackball()
        {
            // Major arrow along x
            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = TrackballGrabber.GetMajorArrow(Axis.X, false, _colors.TrackballAxisMajor, _colors.TrackballAxisSpecular);

            _viewportFlowRotate.Children.Add(visual);
            _flowOrientationVisuals.Add(visual);

            // Create the trackball
            _flowOrientationTrackball = new TrackballGrabber(grdFlowRotateViewport, _viewportFlowRotate, 1d, _colors.TrackballGrabberHoverLight);
            _flowOrientationTrackball.SyncedLights.Add(_lightFlow1);
            _flowOrientationTrackball.RotationChanged += new EventHandler(FlowOrientationTrackball_RotationChanged);

            // Faint lines
            _flowOrientationTrackball.HoverVisuals.Add(TrackballGrabber.GetGuideLine(Axis.X, true, _colors.TrackballAxisLine));
            _flowOrientationTrackball.HoverVisuals.Add(TrackballGrabber.GetGuideLineDouble(Axis.Y, _colors.TrackballAxisLine));
            _flowOrientationTrackball.HoverVisuals.Add(TrackballGrabber.GetGuideLineDouble(Axis.Z, _colors.TrackballAxisLine));
        }

        private void VelocityViscocityChanged()
        {
            #region Adjust number of flow lines based on viscosity

            _flowViscosity = trkFlowViscosity.Value;

            int maxFluidVisuals = Convert.ToInt32(NUMFLUIDVISUALS * _flowViscosity);
            if (_fluidVisuals.Count < maxFluidVisuals)
            {
                AddFluidVisuals(maxFluidVisuals - _fluidVisuals.Count);
            }
            else if (_fluidVisuals.Count > maxFluidVisuals)
            {
                int numToRemove = _fluidVisuals.Count - maxFluidVisuals;
                for (int cntr = 1; cntr <= numToRemove; cntr++)
                {
                    int removeIndex = StaticRandom.Next(_fluidVisuals.Count);
                    _fluidVisuals[removeIndex].Dispose();
                    _fluidVisuals.RemoveAt(removeIndex);
                }
            }

            #endregion

            // Update the flow line speed
            Vector3D worldFlow = GetWorldFlow();
            foreach (FluidVisual fluidLine in _fluidVisuals)
            {
                fluidLine.WorldFlow = worldFlow;
            }

            if (_field != null)
            {
                _fieldUniform.Flow = worldFlow * .05;
                double viscocity = GetFluidViscocity();

                //TODO: This field's viscocity isn't correctly implemented.  It's more of a slowdown than a viscocity.  Probably need to play with _field.TimeStep, _field.Iterations instead (or something along those lines
                //TODO: The field should use the average viscocity from calls to _fieldUniform
                _field.Damping = 0;       // viscocity;

                _fieldUniform.Viscosity = viscocity;
                _fieldField.Viscosity = viscocity;
            }
        }

        private void ShowHideBlockedCells()
        {
            // Wipe out the old one
            if (_blockedCellsWireframe != null)
            {
                _viewport.Children.Remove(_blockedCellsWireframe);
                _blockedCellsWireframe = null;
            }

            // See if a new one needs to be created
            if (!chkShowBlockedCells.IsChecked.Value || _field == null)
            {
                return;
            }

            // Get a deduped list of blocked cell's edge lines
            var lines = Rectangle3DIndexed.GetEdgeLinesDeduped(Enumerable.Range(0, _field.Size1D).Where(o => _field.Blocked[o]).Select(o => _fieldCells[o]));
            if (lines.Length == 0)
            {
                return;
            }

            // Create the visual
            _blockedCellsWireframe = new ScreenSpaceLines3D();
            _blockedCellsWireframe.Color = _colors.BlockedCell;
            _blockedCellsWireframe.Thickness = 1;

            Point3D[] allPoints = _fieldCells[0].AllPoints;

            foreach (var line in lines)
            {
                _blockedCellsWireframe.AddLine(allPoints[line.Item1], allPoints[line.Item2]);
            }

            if (_bodyVisual != null) _viewport.Children.Remove(_bodyVisual);
            _viewport.Children.Add(_blockedCellsWireframe);
            if (_bodyVisual != null) _viewport.Children.Add(_bodyVisual);       // this needs to be last so that semi transparency works
        }
        private void ShowHideFluidBoundry()
        {
            // Wipe out the old one
            if (_fieldBoundryWireframe != null)
            {
                _viewport.Children.Remove(_fieldBoundryWireframe);
                _fieldBoundryWireframe = null;
            }

            // See if a new one needs to be created
            if (!chkShowFluidBoundry.IsChecked.Value || _field == null)
            {
                return;
            }

            _fieldBoundryWireframe = new ScreenSpaceLines3D();
            _fieldBoundryWireframe.Color = _colors.FieldBoundry;
            _fieldBoundryWireframe.Thickness = 3;

            double half = _field.SizeWorld / 2d;

            _fieldBoundryWireframe.AddLine(new Point3D(-half, -half, -half), new Point3D(-half, -half, -half));

            // Top (z=0)
            _fieldBoundryWireframe.AddLine(new Point3D(-half, -half, -half), new Point3D(half, -half, -half));
            _fieldBoundryWireframe.AddLine(new Point3D(half, -half, -half), new Point3D(half, half, -half));
            _fieldBoundryWireframe.AddLine(new Point3D(half, half, -half), new Point3D(-half, half, -half));
            _fieldBoundryWireframe.AddLine(new Point3D(-half, half, -half), new Point3D(-half, -half, -half));

            // Bottom (z=1)
            _fieldBoundryWireframe.AddLine(new Point3D(-half, -half, half), new Point3D(half, -half, half));
            _fieldBoundryWireframe.AddLine(new Point3D(half, -half, half), new Point3D(half, half, half));
            _fieldBoundryWireframe.AddLine(new Point3D(half, half, half), new Point3D(-half, half, half));
            _fieldBoundryWireframe.AddLine(new Point3D(-half, half, half), new Point3D(-half, -half, half));

            // Sides
            _fieldBoundryWireframe.AddLine(new Point3D(-half, -half, -half), new Point3D(-half, -half, half));
            _fieldBoundryWireframe.AddLine(new Point3D(half, -half, -half), new Point3D(half, -half, half));
            _fieldBoundryWireframe.AddLine(new Point3D(half, half, -half), new Point3D(half, half, half));
            _fieldBoundryWireframe.AddLine(new Point3D(-half, half, -half), new Point3D(-half, half, half));

            if (_bodyVisual != null) _viewport.Children.Remove(_bodyVisual);
            _viewport.Children.Add(_fieldBoundryWireframe);
            if (_bodyVisual != null) _viewport.Children.Add(_bodyVisual);       // this needs to be last so that semi transparency works
        }

        private ScreenSpaceLines3D GetModelWireframe(ITriangleIndexed[] triangles)
        {
            ScreenSpaceLines3D retVal = new ScreenSpaceLines3D();
            retVal.Thickness = 1d;
            retVal.Color = _colors.HullWireFrame;

            if (triangles.Length > 0)
            {
                Point3D[] allPoints = triangles[0].AllPoints;

                foreach (Tuple<int, int> line in TriangleIndexed.GetUniqueLines(triangles))
                {
                    retVal.AddLine(allPoints[line.Item1], allPoints[line.Item2]);
                }
            }

            return retVal;
        }
        private void AddFluidVisuals(int count)
        {
            for (int cntr = 0; cntr < count; cntr++)
            {
                double length = .5d + Math.Abs(Math3D.GetNearZeroValue(3d));
                Point3D modelFrom = new Point3D(-length, 0, 0);
                Point3D modelTo = new Point3D(length, 0, 0);

                Color color = _colors.FluidLine;		// the property get returns a random color each time

                Point3D position = Math3D.GetRandomVector_Spherical(FLUIDVISUALMAXPOS).ToPoint();

                double maxDistance = position.ToVector().Length;
                maxDistance = UtilityCore.GetScaledValue_Capped(maxDistance, FLUIDVISUALMAXPOS, 0d, 1d, StaticRandom.NextDouble());

                _fluidVisuals.Add(new FluidVisual(_viewport, modelFrom, modelTo, position, GetWorldFlow(), color, maxDistance));
            }
        }

        private Vector3D GetWorldFlow()
        {
            return _flowOrientationTrackball.Transform.Transform(new Vector3D(-trkFlowSpeed.Value, 0, 0));
        }

        private double GetFluidViscocity()
        {
            //return UtilityHelper.GetScaledValue(0d, .1d, trkFlowViscosity.Minimum, trkFlowViscosity.Maximum, trkFlowViscosity.Value);
            //return 0d;
            return trkFlowViscosity.Value;
        }

        private void SetVelocityViewerCameraPosition()
        {
            if (_velocityVisualizerWindow == null)
            {
                return;
            }

            // The camera wasn't set up with an accurate orth, so it needs to be fixed
            _velocityVisualizerWindow.ViewChanged(new DoubleVector(_camera.LookDirection, Math3D.GetOrthogonal(_camera.LookDirection, _camera.UpDirection)));
        }

        #endregion
    }
}
