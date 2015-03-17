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

using Game.HelperClassesWPF;
using Game.HelperClassesWPF.Primitives3D;
using Game.Newt.v2.NewtonDynamics;
using Game.HelperClassesCore;

namespace Game.Newt.Testers
{
    // Check this out to get color ideas
    // http://kuler.adobe.com
    public partial class WindTunnelWindow : Window
    {
        #region Class: FluidVisual

        /// <summary>
        /// This is a wrapper to a line that moves through the world at the speed of the fluid
        /// </summary>
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
            public Color TrackballAxisLine = Color.FromArgb(255, 117, 108, 97);
            public SpecularMaterial TrackballAxisSpecular = new SpecularMaterial(Brushes.White, 100d);

            public Color TrackballGrabberHoverLight = Color.FromArgb(255, 74, 37, 138);
        }

        #endregion

        #region Declaration Section

        private const int NUMFLUIDVISUALS = 50;
        private const double FLUIDVISUALMAXPOS = 25d;

        private ItemColors _colors = new ItemColors();
        private bool _isInitialized = false;

        private World _world = null;

        // Fluid hull's visuals
        private ModelVisual3D _model = null;
        private ScreenSpaceLines3D _modelWireframe = null;

        // This is the hull and body of what's being tested
        private FluidHull _fluidHull = null;
        private Body _body = null;

        private FluidFieldUniform _field = null;

        // These are other bodies used to anchor the body in place
        private List<Body> _anchorBodies = new List<Body>();
        private List<Body> _ropeBodies = new List<Body>();
        private List<JointBase> _ropeJoints = new List<JointBase>();

        private List<FluidVisual> _fluidVisuals = new List<FluidVisual>();

        /// <summary>
        /// This listens to the mouse/keyboard and controls the camera
        /// </summary>
        private TrackBallRoam _trackball = null;

        private List<ModelVisual3D> _modelOrientationVisuals = new List<ModelVisual3D>();
        private TrackballGrabber _modelOrientationTrackball = null;

        private List<ModelVisual3D> _flowOrientationVisuals = new List<ModelVisual3D>();
        private TrackballGrabber _flowOrientationTrackball = null;
        private double _flowViscosity = 1d;

        private ScreenSpaceLines3D _forceLines = null;

        /// <summary>
        /// When a model is created, this is the direction it starts out as
        /// </summary>
        private DoubleVector _defaultDirectionFacing = new DoubleVector(1, 0, 0, 0, 0, 1);

        #endregion

        #region Constructor

        public WindTunnelWindow()
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
                // Init World
                _world = new World();
                _world.Updating += new EventHandler<WorldUpdatingArgs>(World_Updating);
                _world.SetWorldSize(new Point3D(-100, -100, -100), new Point3D(100, 100, 100));
                _world.UnPause();

                // Camera Trackball
                _trackball = new TrackBallRoam(_camera);
                _trackball.EventSource = grdViewPort;		//NOTE:  If this control doesn't have a background color set, the trackball won't see events (I think transparent is ok, just not null)
                _trackball.AllowZoomOnMouseWheel = true;
                _trackball.Mappings.AddRange(TrackBallMapping.GetPrebuilt(TrackBallMapping.PrebuiltMapping.MouseComplete));
                //_trackball.GetOrbitRadius += new GetOrbitRadiusHandler(Trackball_GetOrbitRadius);

                // Trackball Controls
                SetupModelTrackball();
                SetupFlowTrackball();

                _field = new FluidFieldUniform()
                {
                    Viscosity = trkFlowViscosity.Value,
                    Flow = GetWorldFlow()
                };

                // Fluid lines
                AddFluidVisuals(Convert.ToInt32(NUMFLUIDVISUALS * _flowViscosity));

                // Force lines
                _forceLines = new ScreenSpaceLines3D(true);
                _forceLines.Color = _colors.ForceLine;
                _forceLines.Thickness = 2d;

                _viewport.Children.Add(_forceLines);
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
                RemoveCurrentBody();

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
            if (_body == null)
            {
                // When the body is null, the fluid object is static (if there is one at all).  If it's not null, this needs to be called from the
                // body's apply force/torque event
                UpdateFluidForces();
            }

            // These are the visuals that fly along the fluid direction
            UpdateFlowLines(e.ElapsedTime);
        }
        private void Body_ApplyForceAndTorque(object sender, BodyApplyForceAndTorqueArgs e)
        {
            UpdateFluidForces();
        }

        private void Body_BodyMoved(object sender, EventArgs e)
        {
            if (_body != null && _fluidHull != null)
            {
                _fluidHull.Transform = new MatrixTransform3D(_body.OffsetMatrix);
            }
        }

        private void ModelOrientationTrackball_RotationChanged(object sender, EventArgs e)
        {
            if (_model != null)
            {
                // This should only be set directly when the model is non null (otherwise, it's tied to the body)
                if (_fluidHull != null)
                {
                    _fluidHull.Transform = _modelOrientationTrackball.Transform;		// I can set it directly, because the transform only has rotation, no translation
                }
                _model.Transform = _modelOrientationTrackball.Transform;
                _modelWireframe.Transform = _modelOrientationTrackball.Transform;
            }
        }
        private void FlowOrientationTrackball_RotationChanged(object sender, EventArgs e)
        {
            // Update the flow lines
            Vector3D worldFlow = GetWorldFlow();
            foreach (FluidVisual fluidLine in _fluidVisuals)
            {
                fluidLine.WorldFlow = worldFlow;
            }

            // Update the fluid field
            _field.Flow = worldFlow;
            _field.Viscosity = trkFlowViscosity.Value;
        }
        private void trkFlow_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isInitialized)
            {
                return;
            }

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

            _field.Flow = worldFlow;
            _field.Viscosity = trkFlowViscosity.Value;
        }

        private void btnStaticTriangle_Click(object sender, RoutedEventArgs e)
        {
            RemoveCurrentBody();

            #region WPF Model

            // Material
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(_colors.HullFace)));
            materials.Children.Add(_colors.HullFaceSpecular);

            // Geometry Mesh
            MeshGeometry3D mesh = new MeshGeometry3D();

            mesh.Positions.Add(new Point3D(1, 0, 0));
            mesh.Positions.Add(new Point3D(-1, 1, 0));
            mesh.Positions.Add(new Point3D(-1, -1, 0));

            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(2);

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;
            geometry.Geometry = mesh;

            // Transform
            //transform = new Transform3DGroup();		// rotate needs to be added before translate
            //rotation = _defaultDirectionFacing.GetAngleAroundAxis(directionFacing);
            //transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(rotation)));
            //transform.Children.Add(new TranslateTransform3D(position.ToVector()));

            // Model Visual
            _model = new ModelVisual3D();
            _model.Content = geometry;
            _model.Transform = _modelOrientationTrackball.Transform;

            #endregion

            // Hull
            _fluidHull = new FluidHull();
            _fluidHull.Triangles = FluidHull.FluidTriangle.GetTrianglesFromMesh(mesh);
            _fluidHull.IsClosedConvexInUniformField = false;
            _fluidHull.Transform = _modelOrientationTrackball.Transform;
            _fluidHull.Field = _field;

            // Wireframe
            _modelWireframe = GetModelWireframe(_fluidHull);
            _modelWireframe.Transform = _modelOrientationTrackball.Transform;
            _viewport.Children.Add(_modelWireframe);

            // Add to the viewport
            _viewport.Children.Add(_model);
        }
        private void btnStaticSphere1_Click(object sender, RoutedEventArgs e)
        {
            RemoveCurrentBody();

            #region WPF Model

            // Material
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(_colors.HullFace)));
            materials.Children.Add(_colors.HullFaceSpecular);

            // Geometry Mesh
            MeshGeometry3D mesh = UtilityWPF.GetSphere_LatLon(1, 1d);

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;
            geometry.Geometry = mesh;

            // Transform
            //transform = new Transform3DGroup();		// rotate needs to be added before translate
            //rotation = _defaultDirectionFacing.GetAngleAroundAxis(directionFacing);
            //transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(rotation)));
            //transform.Children.Add(new TranslateTransform3D(position.ToVector()));

            // Model Visual
            _model = new ModelVisual3D();
            _model.Content = geometry;
            _model.Transform = _modelOrientationTrackball.Transform;

            #endregion

            // Hull
            _fluidHull = new FluidHull();
            _fluidHull.Triangles = FluidHull.FluidTriangle.GetTrianglesFromMesh(mesh);
            _fluidHull.IsClosedConvexInUniformField = chkClosedHulls.IsChecked.Value;
            _fluidHull.Field = _field;

            // Wireframe
            _modelWireframe = GetModelWireframe(_fluidHull);
            _modelWireframe.Transform = _modelOrientationTrackball.Transform;
            _viewport.Children.Add(_modelWireframe);

            // Add to the viewport
            _viewport.Children.Add(_model);
        }
        private void btnStaticSphere2_Click(object sender, RoutedEventArgs e)
        {
            RemoveCurrentBody();

            #region WPF Model

            // Material
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(_colors.HullFace)));
            materials.Children.Add(_colors.HullFaceSpecular);

            // Geometry Mesh
            MeshGeometry3D mesh = UtilityWPF.GetSphere_LatLon(2, 1d);

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;
            geometry.Geometry = mesh;

            // Transform
            //transform = new Transform3DGroup();		// rotate needs to be added before translate
            //rotation = _defaultDirectionFacing.GetAngleAroundAxis(directionFacing);
            //transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(rotation)));
            //transform.Children.Add(new TranslateTransform3D(position.ToVector()));

            // Model Visual
            _model = new ModelVisual3D();
            _model.Content = geometry;
            _model.Transform = _modelOrientationTrackball.Transform;

            #endregion

            // Hull
            _fluidHull = new FluidHull();
            _fluidHull.Triangles = FluidHull.FluidTriangle.GetTrianglesFromMesh(mesh);
            _fluidHull.IsClosedConvexInUniformField = chkClosedHulls.IsChecked.Value;
            _fluidHull.Field = _field;

            // Wireframe
            _modelWireframe = GetModelWireframe(_fluidHull);
            _modelWireframe.Transform = _modelOrientationTrackball.Transform;
            _viewport.Children.Add(_modelWireframe);

            // Add to the viewport
            _viewport.Children.Add(_model);
        }
        private void btnStaticSphere3_Click(object sender, RoutedEventArgs e)
        {
            RemoveCurrentBody();

            #region WPF Model

            // Material
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(_colors.HullFace)));
            materials.Children.Add(_colors.HullFaceSpecular);

            // Geometry Mesh
            MeshGeometry3D mesh = UtilityWPF.GetSphere_LatLon(4, 1d);

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;
            geometry.Geometry = mesh;

            // Transform
            //transform = new Transform3DGroup();		// rotate needs to be added before translate
            //rotation = _defaultDirectionFacing.GetAngleAroundAxis(directionFacing);
            //transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(rotation)));
            //transform.Children.Add(new TranslateTransform3D(position.ToVector()));

            // Model Visual
            _model = new ModelVisual3D();
            _model.Content = geometry;
            _model.Transform = _modelOrientationTrackball.Transform;

            #endregion

            // Hull
            _fluidHull = new FluidHull();
            _fluidHull.Triangles = FluidHull.FluidTriangle.GetTrianglesFromMesh(mesh);
            _fluidHull.IsClosedConvexInUniformField = chkClosedHulls.IsChecked.Value;
            _fluidHull.Field = _field;

            // Wireframe
            _modelWireframe = GetModelWireframe(_fluidHull);
            _modelWireframe.Transform = _modelOrientationTrackball.Transform;
            _viewport.Children.Add(_modelWireframe);

            // Add to the viewport
            _viewport.Children.Add(_model);
        }

        private void btnBodyPlate_Click(object sender, RoutedEventArgs e)
        {
            RemoveCurrentBody();

            _world.Pause();

            #region Main Body

            #region WPF Model

            // Material
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(_colors.HullFace)));
            materials.Children.Add(_colors.HullFaceSpecular);

            // Geometry Mesh
            MeshGeometry3D mesh = UtilityWPF.GetCube_IndependentFaces(new Point3D(-1, -1, -.1), new Point3D(1, 1, .1));
            CollisionHull collisionHull = CollisionHull.CreateBox(_world, 0, new Vector3D(2, 2, .2), null);

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;
            geometry.Geometry = mesh;

            // Transform
            //transform = new Transform3DGroup();		// rotate needs to be added before translate
            //rotation = _defaultDirectionFacing.GetAngleAroundAxis(directionFacing);
            //transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(rotation)));
            //transform.Children.Add(new TranslateTransform3D(position.ToVector()));

            // Model Visual
            ModelVisual3D model = new ModelVisual3D();
            model.Content = geometry;
            //model.Transform = _modelOrientationTrackball.Transform;		// start this in that orientation, but let physics take over from there (_modelOrientationTrackball update won't touch this body)

            #endregion

            // Body
            _body = new Body(collisionHull, _modelOrientationTrackball.Transform.Value, 1, null);
            collisionHull.Dispose();

            // Hull
            _fluidHull = new FluidHull();
            _fluidHull.Triangles = FluidHull.FluidTriangle.GetTrianglesFromMesh(mesh);
            _fluidHull.IsClosedConvexInUniformField = chkClosedHulls.IsChecked.Value;
            _fluidHull.Field = _field;
            _fluidHull.Body = _body;

            // Wireframe
            ScreenSpaceLines3D modelWireframe = GetModelWireframe(_fluidHull);
            modelWireframe.Transform = _modelOrientationTrackball.Transform;
            _viewport.Children.Add(modelWireframe);

            #endregion

            // Rope1
            Point3D bodyAttachPoint = _modelOrientationTrackball.Transform.Transform(new Point3D(1, -1, 0));
            Point3D anchorPoint = _modelOrientationTrackball.Transform.Transform(new Point3D(2, -3, 0));
            AddRope(bodyAttachPoint, anchorPoint, null);

            if (StaticRandom.Next(2) == 0)
            {
                // Rope2
                bodyAttachPoint = _modelOrientationTrackball.Transform.Transform(new Point3D(1, 1, 0));
                anchorPoint = _modelOrientationTrackball.Transform.Transform(new Point3D(2, 3, 0));
                AddRope(bodyAttachPoint, anchorPoint, null);
            }

            // Add to the viewport
            _viewport.Children.Add(model);		// this must be last, or it won't appear transparent to models added after it

            // Finish setting up the body
            _body.Visuals = new Visual3D[] { model, modelWireframe };
            _body.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(Body_ApplyForceAndTorque);
            _body.BodyMoved += new EventHandler(Body_BodyMoved);

            _world.UnPause();
        }
        private void btnBodyPropeller2_Click(object sender, RoutedEventArgs e)
        {
            RemoveCurrentBody();

            _world.Pause();

            // Materials
            MaterialGroup materialsFluidBlade = new MaterialGroup();		// These blades are what the fluid model knows about, but newton will know about a more complex object
            materialsFluidBlade.Children.Add(new DiffuseMaterial(new SolidColorBrush(_colors.HullFace)));
            materialsFluidBlade.Children.Add(_colors.HullFaceSpecular);

            MaterialGroup materialsPhysics = new MaterialGroup();		// this is the material for the composite physics object
            materialsPhysics.Children.Add(new DiffuseMaterial(new SolidColorBrush(_colors.GhostBodyFace)));
            materialsPhysics.Children.Add(_colors.GhostBodySpecular);

            // Geometries
            MeshGeometry3D meshFluidBlade = new MeshGeometry3D();

            MeshGeometry3D meshPhysicsBlade1 = null;
            MeshGeometry3D meshPhysicsBlade2 = null;
            MeshGeometry3D meshPhysicsCone = null;

            // Centered blade positions (these are defined once, then copies will be transformed and commited)
            Point3D[] bladePositionsFluid = new Point3D[4];
            bladePositionsFluid[0] = new Point3D(-1d, .25d, 0d);
            bladePositionsFluid[1] = new Point3D(-1d, -.25d, 0d);
            bladePositionsFluid[2] = new Point3D(1d, -.25d, 0d);
            bladePositionsFluid[3] = new Point3D(1d, .25d, 0d);

            Point3D[] bladePositionsPhysics = new Point3D[2];
            bladePositionsPhysics[0] = new Point3D(-1d, -.25d, -.05d);
            bladePositionsPhysics[1] = new Point3D(1d, .25d, .05d);

            // This tranform is throw away.  It's just used to transform points
            Transform3DGroup transform = new Transform3DGroup();
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 90d + 10d)));		// rotar tilt (the flow defaults to -x, so face them into the wind)
            transform.Children.Add(new TranslateTransform3D(new Vector3D(1d + .33d, 0, 0)));		// pull away from the center a bit
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 90)));		// I don't want it along X, I want it along Y

            // Fluid blade 1
            meshFluidBlade.Positions.Add(transform.Transform(bladePositionsFluid[0]));
            meshFluidBlade.Positions.Add(transform.Transform(bladePositionsFluid[1]));
            meshFluidBlade.Positions.Add(transform.Transform(bladePositionsFluid[2]));
            meshFluidBlade.Positions.Add(transform.Transform(bladePositionsFluid[3]));

            meshFluidBlade.TriangleIndices.Add(0);
            meshFluidBlade.TriangleIndices.Add(1);
            meshFluidBlade.TriangleIndices.Add(2);
            meshFluidBlade.TriangleIndices.Add(2);
            meshFluidBlade.TriangleIndices.Add(3);
            meshFluidBlade.TriangleIndices.Add(0);

            // Physics blade 1
            meshPhysicsBlade1 = UtilityWPF.GetCube_IndependentFaces(bladePositionsPhysics[0], bladePositionsPhysics[1]);
            CollisionHull collisionPhysicsBlade1 = CollisionHull.CreateBox(_world, 0, bladePositionsPhysics[1] - bladePositionsPhysics[0], transform.Value);
            Transform3D transformPhysicsBlade1 = transform.Clone();

            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 180d)));		// rotate the whole thing 180 degrees

            // Fluid blade 2
            meshFluidBlade.Positions.Add(transform.Transform(bladePositionsFluid[0]));
            meshFluidBlade.Positions.Add(transform.Transform(bladePositionsFluid[1]));
            meshFluidBlade.Positions.Add(transform.Transform(bladePositionsFluid[2]));
            meshFluidBlade.Positions.Add(transform.Transform(bladePositionsFluid[3]));

            meshFluidBlade.TriangleIndices.Add(0 + 4);
            meshFluidBlade.TriangleIndices.Add(1 + 4);
            meshFluidBlade.TriangleIndices.Add(2 + 4);
            meshFluidBlade.TriangleIndices.Add(2 + 4);
            meshFluidBlade.TriangleIndices.Add(3 + 4);
            meshFluidBlade.TriangleIndices.Add(0 + 4);

            // Physics blade 2
            meshPhysicsBlade2 = UtilityWPF.GetCube_IndependentFaces(bladePositionsPhysics[0], bladePositionsPhysics[1]);
            CollisionHull collisionPhysicsBlade2 = CollisionHull.CreateBox(_world, 0, bladePositionsPhysics[1] - bladePositionsPhysics[0], transform.Value);
            Transform3D transformPhysicsBlade2 = transform.Clone();

            //TODO:  Make an overload on some of the geometry builders that take a tranform
            //meshPhysicsCone = UtilityWPF.GetCone_AlongX();

            // Geometry Models
            GeometryModel3D geometryFluidBlade = new GeometryModel3D();
            geometryFluidBlade.Material = materialsFluidBlade;
            geometryFluidBlade.BackMaterial = materialsFluidBlade;
            geometryFluidBlade.Geometry = meshFluidBlade;

            GeometryModel3D geometryPhysicsBlade1 = new GeometryModel3D();
            geometryPhysicsBlade1.Material = materialsPhysics;
            geometryPhysicsBlade1.BackMaterial = materialsPhysics;
            geometryPhysicsBlade1.Geometry = meshPhysicsBlade1;
            geometryPhysicsBlade1.Transform = transformPhysicsBlade1;

            GeometryModel3D geometryPhysicsBlade2 = new GeometryModel3D();
            geometryPhysicsBlade2.Material = materialsPhysics;
            geometryPhysicsBlade2.BackMaterial = materialsPhysics;
            geometryPhysicsBlade2.Geometry = meshPhysicsBlade2;
            geometryPhysicsBlade2.Transform = transformPhysicsBlade2;

            // Model Visual
            ModelVisual3D modelFluidBlade = new ModelVisual3D();
            modelFluidBlade.Content = geometryFluidBlade;
            modelFluidBlade.Transform = _modelOrientationTrackball.Transform;

            ModelVisual3D modelPysicsBlade1 = new ModelVisual3D();
            modelPysicsBlade1.Content = geometryPhysicsBlade1;
            modelPysicsBlade1.Transform = _modelOrientationTrackball.Transform;

            ModelVisual3D modelPysicsBlade2 = new ModelVisual3D();
            modelPysicsBlade2.Content = geometryPhysicsBlade2;
            modelPysicsBlade2.Transform = _modelOrientationTrackball.Transform;

            CollisionHull collisionHull = CollisionHull.CreateCompoundCollision(_world, 0, new CollisionHull[] { collisionPhysicsBlade1, collisionPhysicsBlade2 });

            // Body
            _body = new Body(collisionHull, _modelOrientationTrackball.Transform.Value, 1, null);
            collisionHull.Dispose();
            collisionPhysicsBlade1.Dispose();
            collisionPhysicsBlade2.Dispose();
            //_body.AngularVelocity = new Vector3D(10, 0, 0);

            // Hull
            _fluidHull = new FluidHull();
            _fluidHull.Triangles = FluidHull.FluidTriangle.GetTrianglesFromMesh(meshFluidBlade);     // meshPhysicsBlade1
            _fluidHull.IsClosedConvexInUniformField = false;
            _fluidHull.Transform = _modelOrientationTrackball.Transform;
            _fluidHull.Field = _field;
            _fluidHull.Body = _body;

            // Wireframe
            ScreenSpaceLines3D modelWireframe = GetModelWireframe(_fluidHull);
            modelWireframe.Transform = _modelOrientationTrackball.Transform;
            _viewport.Children.Add(modelWireframe);

            // Rope
            Point3D bodyAttachPoint = _modelOrientationTrackball.Transform.Transform(new Point3D(.25, 0, 0));
            Point3D anchorPoint = _modelOrientationTrackball.Transform.Transform(new Point3D(3, 0, 0));
            //Point3D bodyAttachPoint = _modelOrientationTrackball.Transform.Transform(new Point3D(.25, 0, 0));
            //Point3D anchorPoint = _modelOrientationTrackball.Transform.Transform(new Point3D(.5, 0, 0));
            AddRope(bodyAttachPoint, anchorPoint, Math3D.DegreesToRadians(1d));

            // Add to the viewport
            _viewport.Children.Add(modelFluidBlade);
            _viewport.Children.Add(modelPysicsBlade1);
            _viewport.Children.Add(modelPysicsBlade2);

            // Finish setting up the body
            _body.Visuals = new Visual3D[] { modelPysicsBlade1, modelPysicsBlade2, modelWireframe, modelFluidBlade };
            _body.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(Body_ApplyForceAndTorque);
            _body.BodyMoved += new EventHandler(Body_BodyMoved);

            _world.UnPause();
        }
        private void btnBodyPropeller3_Click(object sender, RoutedEventArgs e)
        {
            //TODO:  This is just a tweaked copy of 2 (I was feeling lazy).  They should really be merged

            RemoveCurrentBody();

            _world.Pause();

            // Materials
            MaterialGroup materialsFluidBlade = new MaterialGroup();		// These blades are what the fluid model knows about, but newton will know about a more complex object
            materialsFluidBlade.Children.Add(new DiffuseMaterial(new SolidColorBrush(_colors.HullFace)));
            materialsFluidBlade.Children.Add(_colors.HullFaceSpecular);

            MaterialGroup materialsPhysics = new MaterialGroup();		// this is the material for the composite physics object
            materialsPhysics.Children.Add(new DiffuseMaterial(new SolidColorBrush(_colors.GhostBodyFace)));
            materialsPhysics.Children.Add(_colors.GhostBodySpecular);

            // Geometries
            MeshGeometry3D meshFluidBlade = new MeshGeometry3D();

            MeshGeometry3D meshPhysicsBlade1 = null;
            MeshGeometry3D meshPhysicsBlade2 = null;
            MeshGeometry3D meshPhysicsBlade3 = null;
            MeshGeometry3D meshPhysicsCone = null;

            // Centered blade positions (these are defined once, then copies will be transformed and commited)
            Point3D[] bladePositionsFluid = new Point3D[4];
            bladePositionsFluid[0] = new Point3D(-1d, .25d, 0d);
            bladePositionsFluid[1] = new Point3D(-1d, -.25d, 0d);
            bladePositionsFluid[2] = new Point3D(1d, -.25d, 0d);
            bladePositionsFluid[3] = new Point3D(1d, .25d, 0d);

            Point3D[] bladePositionsPhysics = new Point3D[2];
            bladePositionsPhysics[0] = new Point3D(-1d, -.25d, -.05d);
            bladePositionsPhysics[1] = new Point3D(1d, .25d, .05d);

            // This tranform is throw away.  It's just used to transform points
            Transform3DGroup transform = new Transform3DGroup();
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 90d + 5d)));		// rotar tilt (the flow defaults to -x, so face them into the wind)
            transform.Children.Add(new TranslateTransform3D(new Vector3D(1d + .33d, 0, 0)));		// pull away from the center a bit
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 90)));		// I don't want it along X, I want it along Y

            // Fluid blade 1
            meshFluidBlade.Positions.Add(transform.Transform(bladePositionsFluid[0]));
            meshFluidBlade.Positions.Add(transform.Transform(bladePositionsFluid[1]));
            meshFluidBlade.Positions.Add(transform.Transform(bladePositionsFluid[2]));
            meshFluidBlade.Positions.Add(transform.Transform(bladePositionsFluid[3]));

            meshFluidBlade.TriangleIndices.Add(0);
            meshFluidBlade.TriangleIndices.Add(1);
            meshFluidBlade.TriangleIndices.Add(2);
            meshFluidBlade.TriangleIndices.Add(2);
            meshFluidBlade.TriangleIndices.Add(3);
            meshFluidBlade.TriangleIndices.Add(0);

            // Physics blade 1
            meshPhysicsBlade1 = UtilityWPF.GetCube_IndependentFaces(bladePositionsPhysics[0], bladePositionsPhysics[1]);
            CollisionHull collisionPhysicsBlade1 = CollisionHull.CreateBox(_world, 0, bladePositionsPhysics[1] - bladePositionsPhysics[0], transform.Value);
            Transform3D transformPhysicsBlade1 = transform.Clone();

            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 120d)));		// rotate the whole thing 120 degrees

            // Fluid blade 2
            meshFluidBlade.Positions.Add(transform.Transform(bladePositionsFluid[0]));
            meshFluidBlade.Positions.Add(transform.Transform(bladePositionsFluid[1]));
            meshFluidBlade.Positions.Add(transform.Transform(bladePositionsFluid[2]));
            meshFluidBlade.Positions.Add(transform.Transform(bladePositionsFluid[3]));

            meshFluidBlade.TriangleIndices.Add(0 + 4);
            meshFluidBlade.TriangleIndices.Add(1 + 4);
            meshFluidBlade.TriangleIndices.Add(2 + 4);
            meshFluidBlade.TriangleIndices.Add(2 + 4);
            meshFluidBlade.TriangleIndices.Add(3 + 4);
            meshFluidBlade.TriangleIndices.Add(0 + 4);

            // Physics blade 2
            meshPhysicsBlade2 = UtilityWPF.GetCube_IndependentFaces(bladePositionsPhysics[0], bladePositionsPhysics[1]);
            CollisionHull collisionPhysicsBlade2 = CollisionHull.CreateBox(_world, 0, bladePositionsPhysics[1] - bladePositionsPhysics[0], transform.Value);
            Transform3D transformPhysicsBlade2 = transform.Clone();

            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 120d)));		// rotate the whole thing 120 degrees

            // Fluid blade 3
            meshFluidBlade.Positions.Add(transform.Transform(bladePositionsFluid[0]));
            meshFluidBlade.Positions.Add(transform.Transform(bladePositionsFluid[1]));
            meshFluidBlade.Positions.Add(transform.Transform(bladePositionsFluid[2]));
            meshFluidBlade.Positions.Add(transform.Transform(bladePositionsFluid[3]));

            meshFluidBlade.TriangleIndices.Add(0 + 8);
            meshFluidBlade.TriangleIndices.Add(1 + 8);
            meshFluidBlade.TriangleIndices.Add(2 + 8);
            meshFluidBlade.TriangleIndices.Add(2 + 8);
            meshFluidBlade.TriangleIndices.Add(3 + 8);
            meshFluidBlade.TriangleIndices.Add(0 + 8);

            // Physics blade 3
            meshPhysicsBlade3 = UtilityWPF.GetCube_IndependentFaces(bladePositionsPhysics[0], bladePositionsPhysics[1]);
            CollisionHull collisionPhysicsBlade3 = CollisionHull.CreateBox(_world, 0, bladePositionsPhysics[1] - bladePositionsPhysics[0], transform.Value);
            Transform3D transformPhysicsBlade3 = transform.Clone();

            //TODO:  Make an overload on some of the geometry builders that take a tranform
            //meshPhysicsCone = UtilityWPF.GetCone_AlongX();

            // Geometry Models
            GeometryModel3D geometryFluidBlade = new GeometryModel3D();
            geometryFluidBlade.Material = materialsFluidBlade;
            geometryFluidBlade.BackMaterial = materialsFluidBlade;
            geometryFluidBlade.Geometry = meshFluidBlade;

            GeometryModel3D geometryPhysicsBlade1 = new GeometryModel3D();
            geometryPhysicsBlade1.Material = materialsPhysics;
            geometryPhysicsBlade1.BackMaterial = materialsPhysics;
            geometryPhysicsBlade1.Geometry = meshPhysicsBlade1;
            geometryPhysicsBlade1.Transform = transformPhysicsBlade1;

            GeometryModel3D geometryPhysicsBlade2 = new GeometryModel3D();
            geometryPhysicsBlade2.Material = materialsPhysics;
            geometryPhysicsBlade2.BackMaterial = materialsPhysics;
            geometryPhysicsBlade2.Geometry = meshPhysicsBlade2;
            geometryPhysicsBlade2.Transform = transformPhysicsBlade2;

            GeometryModel3D geometryPhysicsBlade3 = new GeometryModel3D();
            geometryPhysicsBlade3.Material = materialsPhysics;
            geometryPhysicsBlade3.BackMaterial = materialsPhysics;
            geometryPhysicsBlade3.Geometry = meshPhysicsBlade3;
            geometryPhysicsBlade3.Transform = transformPhysicsBlade3;

            // Model Visual
            ModelVisual3D modelFluidBlade = new ModelVisual3D();
            modelFluidBlade.Content = geometryFluidBlade;
            modelFluidBlade.Transform = _modelOrientationTrackball.Transform;

            ModelVisual3D modelPysicsBlade1 = new ModelVisual3D();
            modelPysicsBlade1.Content = geometryPhysicsBlade1;
            modelPysicsBlade1.Transform = _modelOrientationTrackball.Transform;

            ModelVisual3D modelPysicsBlade2 = new ModelVisual3D();
            modelPysicsBlade2.Content = geometryPhysicsBlade2;
            modelPysicsBlade2.Transform = _modelOrientationTrackball.Transform;

            ModelVisual3D modelPysicsBlade3 = new ModelVisual3D();
            modelPysicsBlade3.Content = geometryPhysicsBlade3;
            modelPysicsBlade3.Transform = _modelOrientationTrackball.Transform;

            CollisionHull collisionHull = CollisionHull.CreateCompoundCollision(_world, 0, new CollisionHull[] { collisionPhysicsBlade1, collisionPhysicsBlade2, collisionPhysicsBlade3 });

            // Body
            _body = new Body(collisionHull, _modelOrientationTrackball.Transform.Value, 1, null);
            collisionHull.Dispose();
            collisionPhysicsBlade1.Dispose();
            collisionPhysicsBlade2.Dispose();
            collisionPhysicsBlade3.Dispose();

            // Hull
            _fluidHull = new FluidHull();
            _fluidHull.Triangles = FluidHull.FluidTriangle.GetTrianglesFromMesh(meshFluidBlade);
            _fluidHull.IsClosedConvexInUniformField = false;
            _fluidHull.Transform = _modelOrientationTrackball.Transform;
            _fluidHull.Field = _field;
            _fluidHull.Body = _body;

            // Wireframe
            ScreenSpaceLines3D modelWireframe = GetModelWireframe(_fluidHull);
            modelWireframe.Transform = _modelOrientationTrackball.Transform;
            _viewport.Children.Add(modelWireframe);

            // Rope
            Point3D bodyAttachPoint = _modelOrientationTrackball.Transform.Transform(new Point3D(.25, 0, 0));
            Point3D anchorPoint = _modelOrientationTrackball.Transform.Transform(new Point3D(3, 0, 0));
            //Point3D bodyAttachPoint = _modelOrientationTrackball.Transform.Transform(new Point3D(0, 0, 0));
            //Point3D anchorPoint = _modelOrientationTrackball.Transform.Transform(new Point3D(.33, 0, 0));
            AddRope(bodyAttachPoint, anchorPoint, Math3D.DegreesToRadians(1d));

            // Add to the viewport
            _viewport.Children.Add(modelFluidBlade);
            _viewport.Children.Add(modelPysicsBlade1);
            _viewport.Children.Add(modelPysicsBlade2);
            _viewport.Children.Add(modelPysicsBlade3);

            // Finish setting up the body
            _body.Visuals = new Visual3D[] { modelPysicsBlade1, modelPysicsBlade2, modelPysicsBlade3, modelWireframe, modelFluidBlade };
            _body.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(Body_ApplyForceAndTorque);
            _body.BodyMoved += new EventHandler(Body_BodyMoved);

            _world.UnPause();
        }
        private void btnBodyPropellerPlates_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RemoveCurrentBody();

                _world.Pause();

                // Material
                MaterialGroup materials = new MaterialGroup();
                materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(_colors.HullFace)));
                materials.Children.Add(_colors.HullFaceSpecular);

                #region Templates

                // These positions are transformed for each blade
                Point3D[] bladePositions = new Point3D[2];
                bladePositions[0] = new Point3D(-1d, -.25d, -.05d);
                bladePositions[1] = new Point3D(1d, .25d, .05d);

                // This tranform is throw away.  It's just used to transform points
                Transform3DGroup transform = new Transform3DGroup();
                transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 90d + 15d)));		// rotar tilt (the flow defaults to -x, so face them into the wind)
                transform.Children.Add(new TranslateTransform3D(new Vector3D(1d + .5d, 0, 0)));		// pull away from the center a bit
                transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 90)));		// I don't want it along X, I want it along Y

                #endregion

                // Blades
                MeshGeometry3D[] meshBlades = new MeshGeometry3D[StaticRandom.Next(1, 8)];
                CollisionHull[] collisionBlades = new CollisionHull[meshBlades.Length];
                Transform3D[] transformBlades = new Transform3D[meshBlades.Length];
                GeometryModel3D[] geometryBlades = new GeometryModel3D[meshBlades.Length];
                ModelVisual3D[] modelBlades = new ModelVisual3D[meshBlades.Length];

                for (int cntr = 0; cntr < meshBlades.Length; cntr++)
                {
                    meshBlades[cntr] = UtilityWPF.GetCube_IndependentFaces(bladePositions[0], bladePositions[1]);
                    collisionBlades[cntr] = CollisionHull.CreateBox(_world, 0, bladePositions[1] - bladePositions[0], transform.Value);
                    transformBlades[cntr] = transform.Clone();

                    geometryBlades[cntr] = new GeometryModel3D();
                    geometryBlades[cntr].Material = materials;
                    geometryBlades[cntr].BackMaterial = materials;
                    geometryBlades[cntr].Geometry = meshBlades[cntr];
                    geometryBlades[cntr].Transform = transformBlades[cntr];

                    modelBlades[cntr] = new ModelVisual3D();
                    modelBlades[cntr].Content = geometryBlades[cntr];
                    modelBlades[cntr].Transform = _modelOrientationTrackball.Transform;

                    // Prep for the next blade
                    transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 360d / meshBlades.Length)));
                }

                // Body
                CollisionHull collisionHull = CollisionHull.CreateCompoundCollision(_world, 0, collisionBlades);

                _body = new Body(collisionHull, _modelOrientationTrackball.Transform.Value, 1, null);
                collisionHull.Dispose();
                foreach (CollisionHull hull in collisionBlades)
                {
                    hull.Dispose();
                }

                _fluidHull = new FluidHull();
                _fluidHull.Triangles = FluidHull.FluidTriangle.GetTrianglesFromMesh(meshBlades, transformBlades);
                _fluidHull.IsClosedConvexInUniformField = false;
                _fluidHull.Transform = _modelOrientationTrackball.Transform;
                _fluidHull.Field = _field;
                _fluidHull.Body = _body;

                // Wireframe
                ScreenSpaceLines3D modelWireframe = GetModelWireframe(_fluidHull);
                modelWireframe.Transform = _modelOrientationTrackball.Transform;
                _viewport.Children.Add(modelWireframe);

                // Rope
                Point3D bodyAttachPoint = _modelOrientationTrackball.Transform.Transform(new Point3D(.25, 0, 0));
                Point3D anchorPoint = _modelOrientationTrackball.Transform.Transform(new Point3D(3, 0, 0));
                AddRope(bodyAttachPoint, anchorPoint, Math3D.DegreesToRadians(1d));

                // Add to the viewport
                foreach (ModelVisual3D model in modelBlades)
                {
                    _viewport.Children.Add(model);
                }

                // Finish setting up the body
                Visual3D[] visuals = new Visual3D[meshBlades.Length + 1];
                for (int cntr = 0; cntr < meshBlades.Length; cntr++)
                {
                    visuals[cntr] = modelBlades[cntr];
                }
                visuals[visuals.Length - 1] = modelWireframe;

                _body.Visuals = visuals;
                _body.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(Body_ApplyForceAndTorque);
                _body.BodyMoved += new EventHandler(Body_BodyMoved);

                _world.UnPause();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCameraDefault_Click(object sender, RoutedEventArgs e)
        {
            _camera.Position = new Point3D(0, -7.071, 7.071);
            _camera.LookDirection = new Vector3D(0, 1, -1);
            _camera.UpDirection = new Vector3D(0, 1, 0);
        }
        private void btnCameraTopDown_Click(object sender, RoutedEventArgs e)
        {
            _camera.Position = new Point3D(0, 0, 10);
            _camera.LookDirection = new Vector3D(0, 0, -1);
            _camera.UpDirection = new Vector3D(0, 1, 0);
        }
        private void btnCameraSide_Click(object sender, RoutedEventArgs e)
        {
            _camera.Position = new Point3D(0, -10, 0);
            _camera.LookDirection = new Vector3D(0, 1, 0);
            _camera.UpDirection = new Vector3D(0, 0, 1);
        }
        private void btnCameraFront_Click(object sender, RoutedEventArgs e)
        {
            _camera.Position = new Point3D(10, 0, 0);
            _camera.LookDirection = new Vector3D(-1, 0, 0);
            _camera.UpDirection = new Vector3D(0, 0, 1);
        }

        #endregion

        #region Private Methods

        private void UpdateFluidForces()
        {
            _forceLines.Clear();

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
                        Point3D pointEnd = forcesAt[cntr] + forces[cntr];

                        _forceLines.AddLine(forcesAt[cntr], pointEnd);
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

        private void RemoveCurrentBody()
        {
            _forceLines.Clear();

            #region Rope Joints

            foreach (JointBase joint in _ropeJoints)
            {
                joint.Dispose();
            }

            _ropeJoints.Clear();

            #endregion

            #region Anchor Bodies

            foreach (Body body in _anchorBodies)
            {
                if (body.Visuals != null)
                {
                    foreach (Visual3D visual in body.Visuals)		// visuals may be stored here, or in _model/_modelWireframe.  Putting if statements all over to be safe
                    {
                        if (_viewport.Children.Contains(visual))
                        {
                            _viewport.Children.Remove(visual);
                        }
                    }
                }

                body.Dispose();
            }

            _anchorBodies.Clear();

            #endregion
            #region Rope Bodies

            foreach (Body body in _ropeBodies)
            {
                if (body.Visuals != null)
                {
                    foreach (Visual3D visual in body.Visuals)		// visuals may be stored here, or in _model/_modelWireframe.  Putting if statements all over to be safe
                    {
                        if (_viewport.Children.Contains(visual))
                        {
                            _viewport.Children.Remove(visual);
                        }
                    }
                }
            }

            _ropeBodies.Clear();

            #endregion
            #region Main Body

            if (_body != null)
            {
                if (_body.Visuals != null)
                {
                    foreach (Visual3D visual in _body.Visuals)		// visuals may be stored here, or in _model/_modelWireframe.  Putting if statements all over to be safe
                    {
                        if (_viewport.Children.Contains(visual))
                        {
                            _viewport.Children.Remove(visual);
                        }
                    }
                }

                _body.BodyMoved -= new EventHandler(Body_BodyMoved);
                _body.ApplyForceAndTorque -= new EventHandler<BodyApplyForceAndTorqueArgs>(Body_ApplyForceAndTorque);
                _body.Dispose();
                _body = null;
            }

            #endregion

            #region Model

            if (_model != null)
            {
                _viewport.Children.Remove(_model);
                _model = null;
            }

            #endregion
            #region Model Wireframe

            if (_modelWireframe != null)
            {
                _viewport.Children.Remove(_modelWireframe);
                _modelWireframe = null;
            }

            #endregion

            _fluidHull = null;
        }

        private void AddRope(Point3D bodyAttachPoint, Point3D anchorPoint, double? radianLimit)
        {
            const double SEGMENTLENGTH = .25d;

            if (_body == null)
            {
                throw new InvalidOperationException("The body must be created before this method is called");
            }

            // Figure out how many rope segments to make
            Vector3D dir = anchorPoint - bodyAttachPoint;
            int numSegments = Convert.ToInt32(dir.Length / SEGMENTLENGTH);
            if (numSegments == 0)
            {
                numSegments = 1;
            }

            double segmentLength = dir.Length / numSegments;

            DoubleVector dirDbl = new DoubleVector(dir, Math3D.GetArbitraryOrhonganal(dir));

            #region Anchor

            #region WPF Model

            // Material
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(_colors.Anchor)));
            materials.Children.Add(_colors.AnchorSpecular);

            // Geometry Mesh
            MeshGeometry3D mesh = UtilityWPF.GetSphere_LatLon(5, .1d);

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;
            geometry.Geometry = mesh;

            // Transform
            Transform3DGroup transform = new Transform3DGroup();		// rotate needs to be added before translate
            //rotation = _defaultDirectionFacing.GetAngleAroundAxis(dirDbl);
            //transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(rotation)));
            transform.Children.Add(new TranslateTransform3D(anchorPoint.ToVector()));

            // Model Visual
            ModelVisual3D model = new ModelVisual3D();
            model.Content = geometry;
            model.Transform = transform;

            #endregion

            // Body
            CollisionHull hull = CollisionHull.CreateNull(_world);
            Body body = new Body(hull, model.Transform.Value, 0, new Visual3D[] { model });
            hull.Dispose();

            _anchorBodies.Add(body);

            // Add to the viewport
            _viewport.Children.Add(model);

            #endregion

            #region Rope

            for (int cntr = 0; cntr < numSegments; cntr++)
            {
                #region WPF Model

                // Material
                materials = new MaterialGroup();
                materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(_colors.Rope)));
                materials.Children.Add(_colors.RopeSpecular);

                // Geometry Mesh
                mesh = UtilityWPF.GetCylinder_AlongX(7, .03d, segmentLength);
                CollisionHull ropeHull = CollisionHull.CreateCylinder(_world, 0, .03d, segmentLength, null);

                // Geometry Model
                geometry = new GeometryModel3D();
                geometry.Material = materials;
                geometry.BackMaterial = materials;
                geometry.Geometry = mesh;

                // Transform
                transform = new Transform3DGroup();		// rotate needs to be added before translate
                //Quaternion rotation = _defaultDirectionFacing.GetAngleAroundAxis(dirDbl);

                Vector3D axisStandard;
                double radiansStandard;
                Math3D.GetRotation(out axisStandard, out radiansStandard, _defaultDirectionFacing.Standard, dirDbl.Standard);
                Quaternion rotationStandard = new Quaternion(axisStandard, Math3D.RadiansToDegrees(radiansStandard));

                //Vector3D axisOrth;
                //double radiansOrth;
                //Math3D.GetRotation(out axisOrth, out radiansOrth, _defaultDirectionFacing.Orth, dirDbl.Orth);
                //Quaternion rotationOrth = new Quaternion(axisOrth, Math3D.RadiansToDegrees(radiansOrth));

                //Quaternion rotation = rotationStandard.ToUnit() * rotationOrth.ToUnit();
                //Quaternion rotation = rotationOrth;
                Quaternion rotation = rotationStandard;		// adding the orth in just messes it up

                transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(rotation)));
                Vector3D segmentPosition = new Vector3D((segmentLength / 2d) + (segmentLength * cntr), 0, 0);
                segmentPosition = rotation.GetRotatedVector(segmentPosition);
                segmentPosition += bodyAttachPoint.ToVector();
                transform.Children.Add(new TranslateTransform3D(segmentPosition));

                // Model Visual
                model = new ModelVisual3D();
                model.Content = geometry;
                model.Transform = transform;

                #endregion

                // Body
                body = new Body(ropeHull, model.Transform.Value, .1, new Visual3D[] { model });
                ropeHull.Dispose();

                #region Joint

                if (cntr == 0)
                {
                    _ropeJoints.Add(JointBallAndSocket.CreateBallAndSocket(_world, bodyAttachPoint, _body, body));
                }
                else
                {
                    Vector3D connectPosition2 = new Vector3D(segmentLength * cntr, 0, 0);
                    connectPosition2 = rotation.GetRotatedVector(connectPosition2);
                    connectPosition2 += bodyAttachPoint.ToVector();

                    _ropeJoints.Add(JointBallAndSocket.CreateBallAndSocket(_world, connectPosition2.ToPoint(), _ropeBodies[_ropeBodies.Count - 1], body));
                }

                if (cntr == numSegments - 1)
                {
                    Vector3D connectPosition1 = new Vector3D(segmentLength * numSegments, 0, 0);
                    connectPosition1 = rotation.GetRotatedVector(connectPosition1);
                    connectPosition1 += bodyAttachPoint.ToVector();

                    _ropeJoints.Add(JointBallAndSocket.CreateBallAndSocket(_world, connectPosition1.ToPoint(), body, _anchorBodies[_anchorBodies.Count - 1]));
                }

                #endregion

                _ropeBodies.Add(body);

                // Add to the viewport
                _viewport.Children.Add(model);
            }

            if (radianLimit != null)
            {
                for (int cntr = 0; cntr < _ropeJoints.Count - 1; cntr++)		// the connection between the anchor point and rope will never have a limit
                {
                    ((JointBallAndSocket)_ropeJoints[cntr]).SetConeLimits(dir, radianLimit.Value, null);
                }
            }

            #endregion
        }

        private void SetupModelTrackball()
        {
            Model3DGroup model = new Model3DGroup();

            // major arrow along x
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

        private ScreenSpaceLines3D GetModelWireframe(FluidHull hull)
        {
            ScreenSpaceLines3D retVal = new ScreenSpaceLines3D();
            retVal.Thickness = 1d;
            retVal.Color = _colors.HullWireFrame;

            FluidHull.FluidTriangle[] triangles = hull.Triangles;

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

        #endregion
    }
}
