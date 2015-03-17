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

using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.HelperClassesWPF.Primitives3D;
using Game.Newt.v1.NewtonDynamics1;

namespace Game.Newt.Testers
{
    public partial class GravityCubes2 : Window
    {
        #region Declaration Section

        private const double GRAVITATIONALCONSTANT = 15d;
        private const double FORCETOORIGIN = 1;
        private const double MAXRANDVELOCITY = 10d;

        private TrackBallRoam _trackball = null;

        private List<ScreenSpaceLines3D> _lines = new List<ScreenSpaceLines3D>();

        private List<ConvexBody3D> _cubes = new List<ConvexBody3D>();
        private SortedList<Body, Vector3D> _cubeForces = new SortedList<Body, Vector3D>();

        private Vector3D _boundryMin = new Vector3D(-12, -12, -12);
        private Vector3D _boundryMax = new Vector3D(12, 12, 12);

        private double _randVelMultiplier = 1d;
        private bool _shouldRandomizeVelocities = false;
        private bool _didRandomizeVelocities = false;

        #endregion

        #region Constructor

        public GravityCubes2()
        {
            InitializeComponent();

            grdPanel.Background = SystemColors.ControlBrush;
            chkAttractedToOrigin.Content = "Attracted to\r\nOrigin";

            #region Init World

            _world.InitialiseBodies();
            _world.Gravity = 0d;

            List<Point3D[]> innerLines, outerLines;
            _world.SetCollisionBoundry(out innerLines, out outerLines, _viewport, new Vector3D(-15, -15, -15), new Vector3D(15, 15, 15));

            Color lineColor = UtilityWPF.AlphaBlend(Colors.White, Colors.Gray, .1d);
            foreach (Point3D[] line in innerLines)
            {
                // Need to wait until the window is loaded to call lineModel.CalculateGeometry
                ScreenSpaceLines3D lineModel = new ScreenSpaceLines3D(false);
                lineModel.Thickness = 1d;
                lineModel.Color = lineColor;
                lineModel.AddLine(line[0], line[1]);

                _viewport.Children.Add(lineModel);
                _lines.Add(lineModel);
            }

            _world.UnPause();

            #endregion

            _trackball = new TrackBallRoam(_camera);
            _trackball.EventSource = grdViewPort;
            _trackball.AllowZoomOnMouseWheel = true;
            _trackball.Mappings.AddRange(TrackBallMapping.GetPrebuilt(TrackBallMapping.PrebuiltMapping.MouseComplete));
        }

        #endregion

        #region Event Listeners

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RecalculateLineGeometries();
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            if (_cubes != null)
            {
                _cubes = null;
            }

            if (_cubeForces != null)
            {
                _cubeForces = null;
            }

            if (_world != null)
            {
                _world.Dispose();
                _world = null;
            }
        }

        /// <summary>
        /// This is raised by _world once per frame (this is raised, then it requests forces for bodies)
        /// </summary>
        private void World_Updating(object sender, EventArgs e)
        {
            try
            {
                #region Init Rand Velocity Bools

                if (_shouldRandomizeVelocities && _didRandomizeVelocities)
                {
                    _shouldRandomizeVelocities = false;
                    _didRandomizeVelocities = false;
                }

                #endregion

                #region Gravity

                _cubeForces.Clear();

                // Init my forces list
                for (int cntr = 0; cntr < _cubes.Count; cntr++)
                {
                    _cubeForces.Add(_cubes[cntr], new Vector3D());
                }

                // Apply Gravity
                for (int outerCntr = 0; outerCntr < _cubes.Count - 1; outerCntr++)
                {
                    //Point3D centerMass1 = _cubes[outerCntr].VisualMatrix.Transform(_cubes[outerCntr].CenterOfMass);
                    Point3D centerMass1 = _cubes[outerCntr].PositionToWorld(_cubes[outerCntr].CenterOfMass);

                    for (int innerCntr = outerCntr + 1; innerCntr < _cubes.Count; innerCntr++)
                    {
                        #region Apply Gravity

                        //Point3D centerMass2 = _cubes[innerCntr].VisualMatrix.Transform(_cubes[innerCntr].CenterOfMass);
                        Point3D centerMass2 = _cubes[innerCntr].PositionToWorld(_cubes[innerCntr].CenterOfMass);

                        Vector3D gravityLink = centerMass1 - centerMass2;

                        double force = GRAVITATIONALCONSTANT * (_cubes[outerCntr].Mass * _cubes[innerCntr].Mass) / gravityLink.LengthSquared;

                        gravityLink.Normalize();
                        gravityLink = Vector3D.Multiply(force, gravityLink);

                        _cubeForces[_cubes[innerCntr]] += gravityLink;
                        _cubeForces[_cubes[outerCntr]] -= gravityLink;

                        #endregion
                    }
                }

                #endregion

                if (chkAttractedToOrigin.IsChecked.Value)
                {
                    #region Attract To Origin

                    for (int cntr = 0; cntr < _cubes.Count; cntr++)
                    {
                        //Point3D centerMass = _cubes[cntr].VisualMatrix.Transform(_cubes[cntr].CenterOfMass);
                        Point3D centerMass = _cubes[cntr].PositionToWorld(_cubes[cntr].CenterOfMass);

                        Vector3D originLink = centerMass.ToVector();		// subtracting minus origin gives the same location

                        originLink.Normalize();
                        originLink = Vector3D.Multiply(originLink, FORCETOORIGIN);

                        _cubeForces[_cubes[cntr]] -= originLink;
                    }

                    #endregion
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Body_ApplyForce(Body sender, BodyForceEventArgs e)
        {
            if (_shouldRandomizeVelocities)
            {
                #region Set Velocities

                _didRandomizeVelocities = true;

                Vector3D newVelocity = Math3D.GetRandomVector_Spherical(MAXRANDVELOCITY * _randVelMultiplier);

                e.AddImpulse(newVelocity, sender.CenterOfMass.ToVector());

                #endregion
            }

            // Apply Gravity
            if (!_cubeForces.ContainsKey(sender))
            {
                return;
            }

            e.AddForce(_cubeForces[sender]);
        }

        private void grdViewPort_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RecalculateLineGeometries();
        }
        private void Camera_Changed(object sender, EventArgs e)
        {
            RecalculateLineGeometries();
        }

        private void chkRandomRatios_Checked(object sender, RoutedEventArgs e)
        {
            bool shouldEnable = !chkRandomRatios.IsChecked.Value;

            trkX.IsEnabled = shouldEnable;
            trkY.IsEnabled = shouldEnable;
            trkZ.IsEnabled = shouldEnable;

            lblX.IsEnabled = shouldEnable;
            lblY.IsEnabled = shouldEnable;
            lblZ.IsEnabled = shouldEnable;
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            const double MINRATIO = .25d;
            const double MAXRATIO = 2d;
            const double MINMASS = .9d;
            const double MAXMASS = 5d;

            try
            {
                double ratioX, ratioY, ratioZ;
                if (chkRandomRatios.IsChecked.Value)
                {
                    ratioX = UtilityCore.GetScaledValue(MINRATIO, MAXRATIO, 0d, 1d, StaticRandom.NextDouble());
                    ratioY = UtilityCore.GetScaledValue(MINRATIO, MAXRATIO, 0d, 1d, StaticRandom.NextDouble());
                    ratioZ = UtilityCore.GetScaledValue(MINRATIO, MAXRATIO, 0d, 1d, StaticRandom.NextDouble());
                }
                else
                {
                    ratioX = UtilityCore.GetScaledValue(MINRATIO, MAXRATIO, trkX.Minimum, trkX.Maximum, trkX.Value);
                    ratioY = UtilityCore.GetScaledValue(MINRATIO, MAXRATIO, trkY.Minimum, trkY.Maximum, trkY.Value);
                    ratioZ = UtilityCore.GetScaledValue(MINRATIO, MAXRATIO, trkZ.Minimum, trkZ.Maximum, trkZ.Value);
                }

                #region WPF Model

                // Material
                MaterialGroup materials = new MaterialGroup();
                materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.GetRandomColor(64, 192))));
                materials.Children.Add(new SpecularMaterial(Brushes.White, 100d));

                // Geometry Model
                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = materials;
                geometry.BackMaterial = materials;
                geometry.Geometry = UtilityWPF.GetCube(new Point3D(-ratioX, -ratioY, -ratioZ), new Point3D(ratioX, ratioY, ratioZ));
                //geometry.Geometry = UtilityWPF.GetRectangle3D()

                // Transform
                Transform3DGroup transform = new Transform3DGroup();		// rotate needs to be added before translate
                transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(Math3D.GetRandomVector_Spherical(10), Math3D.GetNearZeroValue(360d))));
                transform.Children.Add(new TranslateTransform3D(Math3D.GetRandomVector(_boundryMin, _boundryMax)));

                // Model Visual
                ModelVisual3D model = new ModelVisual3D();
                model.Content = geometry;
                model.Transform = transform;

                #endregion

                // Add to the viewport
                _viewport.Children.Add(model);

                // Make a physics body that represents this cube
                ConvexBody3D body = new ConvexBody3D(_world, model);

                if (chkConstantMass.IsChecked.Value)
                {
                    body.Mass = 1f;
                }
                else
                {
                    // If I try to be realistic, then it's boring, so I'll scale the result.  (density shrinks a bit as things get larger)
                    double mass = ratioX * ratioY * ratioZ;
                    mass = UtilityCore.GetScaledValue(MINMASS, MAXMASS, Math.Pow(MINRATIO, 3), Math.Pow(MAXRATIO, 3), mass);
                    body.Mass = Convert.ToSingle(mass);
                }

                body.LinearDamping = .01f;
                body.AngularDamping = new Vector3D(.01f, .01f, .01f);

                //TODO:  Make this work
                //body.Velocity = 

                //needed?
                //_world.AddBody(body);


                body.ApplyForce += new BodyForceEventHandler(Body_ApplyForce);




                _cubes.Add(body);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _cubeForces.Clear();

                while (_cubes.Count > 0)
                {
                    _world.RemoveBody(_cubes[0]);
                    _viewport.Children.Remove(_cubes[0].Visual);
                    _cubes[0].Dispose();
                    _cubes.RemoveAt(0);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            _shouldRandomizeVelocities = true;
            _didRandomizeVelocities = false;
            _randVelMultiplier = .75d;
        }
        private void button3_Click(object sender, RoutedEventArgs e)
        {
            _shouldRandomizeVelocities = true;
            _didRandomizeVelocities = false;
            _randVelMultiplier = 1.5d;
        }
        private void button4_Click(object sender, RoutedEventArgs e)
        {
            _shouldRandomizeVelocities = true;
            _didRandomizeVelocities = false;
            _randVelMultiplier = 5d;
        }
        private void button5_Click(object sender, RoutedEventArgs e)
        {
            _shouldRandomizeVelocities = true;
            _didRandomizeVelocities = false;
            _randVelMultiplier = 10d;
        }

        private void btnResetCamera_Click(object sender, RoutedEventArgs e)
        {
            _camera.Position = new Point3D(0, 0, 30);
            _camera.LookDirection = new Vector3D(0, 0, -1);
            _camera.UpDirection = new Vector3D(0, 1, 0);


            RecalculateLineGeometries();
        }

        #endregion

        #region Small Invisible Buttons

        // These buttons are an attempt to figure out why ScreenSpaceLines3D was killing the performance of
        // the animation.  Turns out the autoUpdate of true passed to the constructor is very expensive

        ScreenSpaceLines3D _lastLine = null;

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            for (int cntr = 1; cntr <= 1; cntr++)
            {
                Color lineColor = UtilityWPF.GetRandomColor(64, 192);

                ScreenSpaceLines3D lineModel = new ScreenSpaceLines3D(false);
                lineModel.Thickness = 1d;
                lineModel.Color = lineColor;


                Point3D fromPoint = Math3D.GetRandomVector(15d).ToPoint();
                Point3D toPoint = Math3D.GetRandomVector(15d).ToPoint();


                //lineModel.AddLine(fromPoint, toPoint);
                lineModel.Points.Add(fromPoint);
                lineModel.Points.Add(toPoint);


                _viewport.Children.Add(lineModel);


                lineModel.CalculateGeometry();



                _lastLine = lineModel;
            }
        }
        private void button8_Click(object sender, RoutedEventArgs e)
        {

            // This is an attempt to figure out why ScreenSpaceLines3D slows everything down so much.
            // When I create a ModelVisual3D with the same data, there is no slowdown



            // Material
            MaterialGroup materials = new MaterialGroup();
            Color color = Colors.Black;
            color.ScA = UtilityWPF.GetRandomColor(64, 192).ScA;
            materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
            materials.Children.Add(new EmissiveMaterial(new SolidColorBrush(color)));
            //materials.Children.Add(new SpecularMaterial(Brushes.White, 100d));


            // Mesh
            MeshGeometry3D mesh = new MeshGeometry3D();
            mesh.Positions.Add(new Point3D(-13.5490513704136, 13.6741932804255, -3.76539926918485));
            mesh.Positions.Add(new Point3D(-13.5174663612576, 13.7109705191471, -3.76539926918485));
            mesh.Positions.Add(new Point3D(-0.620117366790474, 2.03912470964928, 5.03956830130937));
            mesh.Positions.Add(new Point3D(-0.596768747173794, 2.06631158660155, 5.03956830130937));

            mesh.TriangleIndices.Add(2);
            mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(2);
            mesh.TriangleIndices.Add(3);
            mesh.TriangleIndices.Add(1);


            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;
            geometry.Geometry = mesh;
            //geometry.Geometry = UtilityWPF.GetRectangle3D()

            // Transform
            //Transform3DGroup transform = new Transform3DGroup();		// rotate needs to be added before translate
            //transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(Math3D.GetRandomVectorSpherical(_rand, 10), Math3D.GetNearZeroValue(_rand, 360d))));
            //transform.Children.Add(new TranslateTransform3D(Math3D.GetRandomVector(_rand, _boundryMin, _boundryMax)));

            // Model Visual
            ModelVisual3D model = new ModelVisual3D();
            model.Content = geometry;
            //model.Transform = transform;


            // Add to the viewport
            _viewport.Children.Add(model);








        }
        private void button9_Click(object sender, RoutedEventArgs e)
        {
            //Color lineColor = UtilityWPF.GetRandomColor(_rand, 255, 64, 192);

            //ScreenSpaceLines3D_mine lineModel = new ScreenSpaceLines3D_mine(lineColor, 1d);

            //Point3D fromPoint = Math3D.GetRandomVector(_rand, 15d).ToPoint();
            //Point3D toPoint = Math3D.GetRandomVector(_rand, 15d).ToPoint();

            //lineModel.AddPoint(fromPoint);
            //lineModel.AddPoint(toPoint);

            //_viewport.Children.Add(lineModel);

            //lineModel.CalculateGeometry();

            //_lastMyLine = lineModel;
        }
        private void button10_Click(object sender, RoutedEventArgs e)
        {
            //if (_lastMyLine == null)
            //{
            //    MessageBox.Show("No my lines added", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
            //    return;
            //}

            //_lastMyLine.CalculateGeometry();
        }

        private void button6_Click(object sender, RoutedEventArgs e)
        {
            for (int cntr = 1; cntr <= 1; cntr++)
            {


                // Material
                MaterialGroup materials = new MaterialGroup();
                materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.GetRandomColor(64, 192))));
                materials.Children.Add(new SpecularMaterial(Brushes.White, 100d));

                // Geometry Model
                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = materials;
                //geometry.Geometry = UtilityWPF.GetCube(.5d);
                //geometry.Geometry = UtilityWPF.GetRectangle3D()
                //geometry.Geometry = model.get

                // Transform
                Transform3DGroup transform = new Transform3DGroup();		// rotate needs to be added before translate
                transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(Math3D.GetRandomVector_Spherical(10), Math3D.GetNearZeroValue(360d))));
                transform.Children.Add(new TranslateTransform3D(Math3D.GetRandomVector(_boundryMin, _boundryMax)));

                // Model Visual
                //ModelVisual3D model = new ModelVisual3D();
                //model.Content = geometry;
                //model.Transform = transform;




                SphereVisual3D model = new SphereVisual3D();
                //model.Content = geometry;
                model.Transform = transform;



                _viewport.Children.Add(model);


            }
        }
        private void button7_Click(object sender, RoutedEventArgs e)
        {
            if (_lastLine == null)
            {
                MessageBox.Show("No lines added", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string report = _lastLine.ReportGeometry();

            Clipboard.SetText(report);

            MessageBox.Show(report, this.Title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Private Methods

        private void RecalculateLineGeometries()
        {
            foreach (ScreenSpaceLines3D line in _lines)
            {
                line.CalculateGeometry();
            }
        }

        #endregion
    }
}
