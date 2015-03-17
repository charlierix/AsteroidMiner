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
using System.Windows.Media.Media3D;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.HelperClassesWPF.Primitives3D;
using Game.Newt.v1.NewtonDynamics1;

namespace Game.Newt.Testers
{
    public partial class CollisionShapes : Window
    {
        #region Declaration Section

        private const double CREATEOBJECTBOUNDRY = 12;
        private const double GRAVITATIONALCONSTANT = 15d;
        private const double FORCETOORIGIN = 10;		// 1;
        private const double MAXRANDVELOCITY = 10d;

        /// <summary>
        /// This listens to the mouse/keyboard and controls the camera
        /// </summary>
        private TrackBallRoam _trackball = null;

        /// <summary>
        /// These are the lines that show the boundries.  Whenever the camera moves, they need to recalculate
        /// so they appear as a 2D line on the screen (they are actually 3D meshes)
        /// </summary>
        private List<ScreenSpaceLines3D> _lines = new List<ScreenSpaceLines3D>();

        /// <summary>
        /// These are the cubes, spheres, etc that get added
        /// </summary>
        private List<ConvexBody3D> _bodies = new List<ConvexBody3D>();
        /// <summary>
        /// These are the forces that get applied to the bodies once per frame (due to gravity)
        /// </summary>
        private SortedList<Body, Vector3D> _bodyForces = new SortedList<Body, Vector3D>();

        #endregion

        #region Constructor

        public CollisionShapes()
        {
            InitializeComponent();

            grdPanel.Background = SystemColors.ControlBrush;

            #region Init World

            _world.InitialiseBodies();
            //_world.ShouldForce2D = true;       // the checkbox is defaulted to unchecked
            _world.Gravity = 0d;

            _world.UnPause();

            #endregion

            // Trackball
            _trackball = new TrackBallRoam(_camera);
            _trackball.EventSource = grdViewPort;
            _trackball.AllowZoomOnMouseWheel = true;
            _trackball.Mappings.AddRange(TrackBallMapping.GetPrebuilt(TrackBallMapping.PrebuiltMapping.MouseComplete));
            //_trackball.GetOrbitRadius += new GetOrbitRadiusHandler(Trackball_GetOrbitRadius);
            _trackball.GetOrbitRadius += new EventHandler<GetOrbitRadiusArgs>(Trackball_GetOrbitRadius);

            // Make the form look right
            trkSimulationSpeed_ValueChanged(this, new RoutedPropertyChangedEventArgs<double>(trkSimulationSpeed.Value, trkSimulationSpeed.Value));
            trkMapSize_ValueChanged(this, new RoutedPropertyChangedEventArgs<double>(trkMapSize.Value, trkMapSize.Value));
        }

        #endregion

        #region Event Listeners

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RecalculateLineGeometries();
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            if (_bodies != null)
            {
                _bodies = null;
            }

            if (_bodyForces != null)
            {
                _bodyForces = null;
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
                #region Gravity

                _bodyForces.Clear();

                // Init my forces list
                for (int cntr = 0; cntr < _bodies.Count; cntr++)
                {
                    _bodyForces.Add(_bodies[cntr], new Vector3D());
                }

                // Apply Gravity
                for (int outerCntr = 0; outerCntr < _bodies.Count - 1; outerCntr++)
                {
                    //Point3D centerMass1 = _bodies[outerCntr].VisualMatrix.Transform(_bodies[outerCntr].CenterOfMass);
                    Point3D centerMass1 = _bodies[outerCntr].PositionToWorld(_bodies[outerCntr].CenterOfMass);

                    for (int innerCntr = outerCntr + 1; innerCntr < _bodies.Count; innerCntr++)
                    {
                        #region Apply Gravity

                        //Point3D centerMass2 = _bodies[innerCntr].VisualMatrix.Transform(_bodies[innerCntr].CenterOfMass);
                        Point3D centerMass2 = _bodies[innerCntr].PositionToWorld(_bodies[innerCntr].CenterOfMass);

                        Vector3D gravityLink = centerMass1 - centerMass2;

                        double force = GRAVITATIONALCONSTANT * (_bodies[outerCntr].Mass * _bodies[innerCntr].Mass) / gravityLink.LengthSquared;

                        gravityLink.Normalize();
                        gravityLink = Vector3D.Multiply(force, gravityLink);

                        _bodyForces[_bodies[innerCntr]] += gravityLink;
                        _bodyForces[_bodies[outerCntr]] -= gravityLink;

                        #endregion
                    }
                }

                #endregion

                if (chkAttractedToOrigin.IsChecked.Value)
                {
                    #region Attract To Origin

                    for (int cntr = 0; cntr < _bodies.Count; cntr++)
                    {
                        //Point3D centerMass = _bodies[cntr].VisualMatrix.Transform(_bodies[cntr].CenterOfMass);
                        Point3D centerMass = _bodies[cntr].PositionToWorld(_bodies[cntr].CenterOfMass);

                        Vector3D originLink = centerMass.ToVector();		// subtracting minus origin gives the same location

                        originLink.Normalize();
                        originLink = Vector3D.Multiply(originLink, FORCETOORIGIN);

                        _bodyForces[_bodies[cntr]] -= originLink;
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
            // Apply Gravity
            if (!_bodyForces.ContainsKey(sender))
            {
                return;
            }

            e.AddForce(_bodyForces[sender]);
        }

        private void Trackball_GetOrbitRadius(object sender, GetOrbitRadiusArgs e)
        {
            //TODO:  e should also pass in the mouse click point, so there is camera position, and click position, this
            // listener can decide which they want (I can't reuse the same direction, because of the perspective)

            RayCastResult result = _world.CastRay(e.Position, e.Direction, 30d, World.BodyFilterType.ExcludeBodies);

            if (result != null)
            {
                e.Result = result.HitDistance;
            }

        }

        private void grdViewPort_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RecalculateLineGeometries();
        }
        private void Camera_Changed(object sender, EventArgs e)
        {
            RecalculateLineGeometries();
        }

        private void RadioShape_Checked(object sender, RoutedEventArgs e)
        {

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

        private void trkSimulationSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_world == null || trkSimulationSpeed == null || lblSimSpeed == null)
            {
                return;
            }

            double speed = 1d;

            double halfRange = trkSimulationSpeed.Minimum + ((trkSimulationSpeed.Maximum - trkSimulationSpeed.Minimum) / 2d);

            if (trkSimulationSpeed.Value >= halfRange)
            {
                speed = UtilityCore.GetScaledValue_Capped(1d, 10d, halfRange, trkSimulationSpeed.Maximum, trkSimulationSpeed.Value);
            }
            else
            {
                speed = UtilityCore.GetScaledValue_Capped(.1d, 1d, trkSimulationSpeed.Minimum, halfRange, trkSimulationSpeed.Value);
            }

            _world.SimulationSpeed = speed;

            speed = Math.Round(speed, 3);
            lblSimSpeed.Content = speed.ToString();
        }
        private void trkMapSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_world == null || trkMapSize == null)
            {
                return;
            }

            // Clear old
            foreach (ScreenSpaceLines3D line in _lines)
            {
                _viewport.Children.Remove(line);
                line.Dispose();
            }
            _lines.Clear();

            double boundry = trkMapSize.Value;
            lblMapSize.Content = boundry.ToString("N0");

            // Set the new boundry
            List<Point3D[]> innerLines, outerLines;
            _world.SetCollisionBoundry(out innerLines, out outerLines, _viewport, new Vector3D(-boundry, -boundry, -boundry), new Vector3D(boundry, boundry, boundry));

            // Draw the lines
            Color lineColor = UtilityWPF.AlphaBlend(Colors.White, Colors.Gray, .1d);
            foreach (Point3D[] line in innerLines)
            {
                ScreenSpaceLines3D lineModel = new ScreenSpaceLines3D(false);
                lineModel.Thickness = 1d;
                lineModel.Color = lineColor;
                lineModel.AddLine(line[0], line[1]);

                _viewport.Children.Add(lineModel);
                _lines.Add(lineModel);
            }

            // NOTE:  If this is called from the constructor, then recalculating doesn't do any good.  They must be recalculated
            // in the window load
            RecalculateLineGeometries();
        }

        private void btnZeroVel_Click(object sender, RoutedEventArgs e)
        {
            SetVelocites(MAXRANDVELOCITY * 0d);
        }
        private void btnRandVelLow_Click(object sender, RoutedEventArgs e)
        {
            SetVelocites(MAXRANDVELOCITY * .33d);
        }
        private void btnRandVelMed_Click(object sender, RoutedEventArgs e)
        {
            SetVelocites(MAXRANDVELOCITY * 1d);
        }
        private void btnRandVelHigh_Click(object sender, RoutedEventArgs e)
        {
            SetVelocites(MAXRANDVELOCITY * 5d);
        }
        private void btnRandVelExtreme_Click(object sender, RoutedEventArgs e)
        {
            SetVelocites(MAXRANDVELOCITY * 10d);
        }

        private void chkShouldForce2D_Checked(object sender, RoutedEventArgs e)
        {
            _world.ShouldForce2D = chkShouldForce2D.IsChecked.Value;
        }

        private void btnResetCamera_Click(object sender, RoutedEventArgs e)
        {
            _camera.Position = new Point3D(0, 0, 40);
            _camera.LookDirection = new Vector3D(0, 0, -1);
            _camera.UpDirection = new Vector3D(0, 1, 0);
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Figure out the ratios and mass of this body
                ConvexBody3D.CollisionShape shape;
                double ratioX, ratioY, ratioZ, mass;
                GetRatiosMass(out shape, out ratioX, out ratioY, out ratioZ, out mass);

                #region WPF Model

                // Material
                MaterialGroup materials = new MaterialGroup();
                materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.GetRandomColor(64, 192))));
                materials.Children.Add(new SpecularMaterial(Brushes.White, 100d));

                // Geometry Model
                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = materials;
                geometry.BackMaterial = materials;

                switch (shape)
                {
                    case ConvexBody3D.CollisionShape.Cube:
                        geometry.Geometry = UtilityWPF.GetCube(new Point3D(-ratioX, -ratioY, -ratioZ), new Point3D(ratioX, ratioY, ratioZ));
                        break;

                    case ConvexBody3D.CollisionShape.Sphere:
                        geometry.Geometry = UtilityWPF.GetSphere_LatLon(5, ratioX, ratioY, ratioZ);
                        //geometry.Geometry = UtilityWPF.GetTorus(30, 10, ratioX * .2, ratioX);    // 
                        break;

                    case ConvexBody3D.CollisionShape.Capsule:
                    case ConvexBody3D.CollisionShape.ChamferCylinder:
                    case ConvexBody3D.CollisionShape.Cone:
                    case ConvexBody3D.CollisionShape.Cylinder:
                    default:
                        throw new ApplicationException("Unknown ConvexBody3D.CollisionShape: " + shape.ToString());
                }

                // Transform
                Transform3DGroup transform = new Transform3DGroup();		// rotate needs to be added before translate
                transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(Math3D.GetRandomVector_Spherical(10), Math3D.GetNearZeroValue(360d))));
                transform.Children.Add(new TranslateTransform3D(Math3D.GetRandomVector(CREATEOBJECTBOUNDRY)));

                // Model Visual
                ModelVisual3D model = new ModelVisual3D();
                model.Content = geometry;
                model.Transform = transform;

                #endregion

                // Add to the viewport
                _viewport.Children.Add(model);

                // Make a physics body that represents this shape
                ConvexBody3D body = new ConvexBody3D(_world, model);

                body.Mass = Convert.ToSingle(mass);

                body.LinearDamping = .01f;
                body.AngularDamping = new Vector3D(.01f, .01f, .01f);

                body.ApplyForce += new BodyForceEventHandler(Body_ApplyForce);

                _bodies.Add(body);
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
                _bodyForces.Clear();

                while (_bodies.Count > 0)
                {
                    _world.RemoveBody(_bodies[0]);
                    _viewport.Children.Remove(_bodies[0].Visual);
                    _bodies[0].Dispose();
                    _bodies.RemoveAt(0);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private void SetVelocites(double speed)
        {
            foreach (ConvexBody3D body in _bodies)
            {
                body.Velocity = Math3D.GetRandomVector_Spherical(speed);
            }
        }

        private void RecalculateLineGeometries()
        {
            foreach (ScreenSpaceLines3D line in _lines)
            {
                line.CalculateGeometry();
            }
        }

        private void GetRatiosMass(out ConvexBody3D.CollisionShape shape, out double ratioX, out double ratioY, out double ratioZ, out double mass)
        {
            const double MINRATIO = .25d;
            const double MAXRATIO = 2d;
            const double MINMASS = .9d;
            const double MAXMASS = 5d;

            // Ratios
            if (chkRandomRatios.IsChecked.Value)
            {
                ratioX = UtilityCore.GetScaledValue(MINRATIO, MAXRATIO, 0d, 1d, StaticRandom.NextDouble());		// reused as radius
                ratioY = UtilityCore.GetScaledValue(MINRATIO, MAXRATIO, 0d, 1d, StaticRandom.NextDouble());		// reused as height
                ratioZ = UtilityCore.GetScaledValue(MINRATIO, MAXRATIO, 0d, 1d, StaticRandom.NextDouble());
            }
            else
            {
                ratioX = UtilityCore.GetScaledValue(MINRATIO, MAXRATIO, trkX.Minimum, trkX.Maximum, trkX.Value);
                ratioY = UtilityCore.GetScaledValue(MINRATIO, MAXRATIO, trkY.Minimum, trkY.Maximum, trkY.Value);
                ratioZ = UtilityCore.GetScaledValue(MINRATIO, MAXRATIO, trkZ.Minimum, trkZ.Maximum, trkZ.Value);
            }

            if (radCube.IsChecked.Value)
            {
                shape = ConvexBody3D.CollisionShape.Cube;
                mass = ratioX * ratioY * ratioZ;
            }
            else if (radSphere.IsChecked.Value)
            {
                shape = ConvexBody3D.CollisionShape.Sphere;
                mass = (4d / 3d) * Math.PI * ratioX * ratioX * ratioX;
            }
            else if (radPill.IsChecked.Value)
            {
                shape = ConvexBody3D.CollisionShape.Capsule;
                mass = Math.PI * ratioX * ratioX * ratioY;
            }
            else if (radCylinder.IsChecked.Value)
            {
                shape = ConvexBody3D.CollisionShape.Cylinder;
                mass = Math.PI * ratioX * ratioX * ratioY;
            }
            else if (radCone.IsChecked.Value)
            {
                shape = ConvexBody3D.CollisionShape.Cone;
                mass = (1d / 3d) * Math.PI * ratioX * ratioX * ratioY;
            }
            else if (radChamferCylinder.IsChecked.Value)
            {
                shape = ConvexBody3D.CollisionShape.ChamferCylinder;
                mass = Math.PI * ratioX * ratioX * ratioY;
            }
            else
            {
                throw new ApplicationException("Unknown shape");
            }

            // Mass
            if (chkConstantMass.IsChecked.Value)
            {
                //mass = 1d;		// for some reason, setting them to exactly one makes them jittery when they collide
                mass = 1.01d;
            }
            else
            {
                // If I try to be realistic, then it's boring, so I'll scale the result.  (density shrinks a bit as things get larger)
                mass = UtilityCore.GetScaledValue(MINMASS, MAXMASS, Math.Pow(MINRATIO, 3), Math.Pow(MAXRATIO, 3), mass);
            }
        }

        #endregion

        #region OLD (world.setboundry)

        /*
		private List<TerrianBody3D> _boundryCubes = new List<TerrianBody3D>();

		private void button1_Click(object sender, RoutedEventArgs e)
		{
			//_boundryCubes.AddRange(GetBoundry());

			//_boundryCubes.AddRange(GetBoundry1(new Point3D(-15, -15, -15), new Point3D(15, 15, 15)));
			//_boundryCubes.AddRange(GetBoundry1(new Point3D(-150, -150, -150), new Point3D(150, 150, 150)));
			//_boundryCubes.AddRange(GetBoundry1(new Point3D(-150, -150, -150), new Point3D(-100, 150, 150)));

			//_boundryCubes.AddRange(GetBoundry2(new Point3D(-50, -50, -50), new Point3D(50, 50, 50)));
			//_boundryCubes.AddRange(GetBoundry2(new Point3D(-150, -150, -150), new Point3D(-100, 150, 150)));

			//_boundryCubes.AddRange(GetBoundry3(new Point3D(-50, -50, -50), new Point3D(50, 50, 50)));

			_boundryCubes.AddRange(GetBoundry4(new Point3D(-50, -50, -50), new Point3D(50, 50, 50), true, true, true));





		}
		private void button2_Click(object sender, RoutedEventArgs e)
		{
			ScreenSpaceLines3D line = new ScreenSpaceLines3D();
			line.Color = Colors.Gold;
			line.AddLine(new Point3D(-10, -10, -10), new Point3D(10, 10, 10));
			line.Thickness = 1d;

			_viewport.Children.Add(line);
		}

		private TerrianBody3D[] GetBoundry()
		{
			List<TerrianBody3D> retVal = new List<TerrianBody3D>();

			// Left
			ModelVisual3D model = GetWPFCube(new Point3D(-15 - 30, -15, -15), new Point3D(-15, 15, 15), Colors.Pink, true);
			_viewport.Children.Add(model);
			retVal.Add(new TerrianBody3D(_world, model));

			// Right
			model = GetWPFCube(new Point3D(15, -15, -15), new Point3D(15 + 30, 15, 15), Colors.Red, true);
			_viewport.Children.Add(model);
			retVal.Add(new TerrianBody3D(_world, model));

			// Top
			model = GetWPFCube(new Point3D(-15, 15, -15), new Point3D(15, 15 + 30, 15), Colors.Green, true);
			_viewport.Children.Add(model);
			retVal.Add(new TerrianBody3D(_world, model));

			// Bottom
			model = GetWPFCube(new Point3D(-15, -15 - 30, -15), new Point3D(15, -15, 15), Colors.Chartreuse, true);
			_viewport.Children.Add(model);
			retVal.Add(new TerrianBody3D(_world, model));

			// Far
			model = GetWPFCube(new Point3D(-15, -15, -15 - 30), new Point3D(15, 15, -15), Colors.PowderBlue, true);
			_viewport.Children.Add(model);
			retVal.Add(new TerrianBody3D(_world, model));

			// Near
			model = GetWPFCube(new Point3D(-15, -15, 15), new Point3D(15, 15, 15 + 30), Colors.Blue, true);
			_viewport.Children.Add(model);
			retVal.Add(new TerrianBody3D(_world, model));

			// Exit Function
			return retVal.ToArray();
		}
		private TerrianBody3D[] GetBoundry1(Point3D min, Point3D max)
		{
			List<TerrianBody3D> retVal = new List<TerrianBody3D>();

			double depthX = ((max.Y - min.Y) + (max.Z - min.Z)) / 2d;
			double depthY = ((max.X - min.X) + (max.Z - min.Z)) / 2d;
			double depthZ = ((max.X - min.X) + (max.Y - min.Y)) / 2d;

			// Left
			ModelVisual3D model = GetWPFCube(new Point3D(min.X - depthX, min.Y, min.Z), new Point3D(min.X, max.Y, max.Z), Colors.Pink, true);
			_viewport.Children.Add(model);
			retVal.Add(new TerrianBody3D(_world, model));

			// Right
			model = GetWPFCube(new Point3D(max.X, min.Y, min.Z), new Point3D(max.X + depthX, max.Y, max.Z), Colors.Red, true);
			_viewport.Children.Add(model);
			retVal.Add(new TerrianBody3D(_world, model));


			// Top
			model = GetWPFCube(new Point3D(min.X, max.Y, min.Z), new Point3D(max.X, max.Y + depthY, max.Z), Colors.Green, true);
			_viewport.Children.Add(model);
			retVal.Add(new TerrianBody3D(_world, model));

			// Bottom
			model = GetWPFCube(new Point3D(min.X, min.Y - depthY, min.Z), new Point3D(max.X, min.Y, max.Z), Colors.Chartreuse, true);
			_viewport.Children.Add(model);
			retVal.Add(new TerrianBody3D(_world, model));


			// Far
			model = GetWPFCube(new Point3D(min.X, min.Y, min.Z - depthZ), new Point3D(max.X, max.Y, min.Z), Colors.PowderBlue, true);
			_viewport.Children.Add(model);
			retVal.Add(new TerrianBody3D(_world, model));

			// Near
			model = GetWPFCube(new Point3D(min.X, min.Y, max.Z), new Point3D(max.X, max.Y, max.Z + depthZ), Colors.Blue, true);
			_viewport.Children.Add(model);
			retVal.Add(new TerrianBody3D(_world, model));

			// Exit Function
			return retVal.ToArray();
		}
		private TerrianBody3D[] GetBoundry2(Point3D min, Point3D max)
		{
			List<TerrianBody3D> retVal = new List<TerrianBody3D>();

			double depthX = ((max.Y - min.Y) + (max.Z - min.Z)) / 2d;
			double depthY = ((max.X - min.X) + (max.Z - min.Z)) / 2d;
			double depthZ = ((max.X - min.X) + (max.Y - min.Y)) / 2d;

			// Left
			ModelVisual3D model = GetWPFCube(new Point3D(min.X - depthX, min.Y - depthY, min.Z - depthZ), new Point3D(min.X, max.Y + depthY, max.Z), Colors.Pink, true);
			_viewport.Children.Add(model);
			retVal.Add(new TerrianBody3D(_world, model));

			// Right
			model = GetWPFCube(new Point3D(max.X, min.Y - depthY, min.Z), new Point3D(max.X + depthX, max.Y + depthY, max.Z + depthZ), Colors.Red, true);
			_viewport.Children.Add(model);
			retVal.Add(new TerrianBody3D(_world, model));


			// Top
			model = GetWPFCube(new Point3D(min.X, max.Y, min.Z), new Point3D(max.X, max.Y + depthY, max.Z), Colors.Green, true);
			_viewport.Children.Add(model);
			retVal.Add(new TerrianBody3D(_world, model));

			// Bottom
			model = GetWPFCube(new Point3D(min.X, min.Y - depthY, min.Z), new Point3D(max.X, min.Y, max.Z), Colors.Chartreuse, true);
			_viewport.Children.Add(model);
			retVal.Add(new TerrianBody3D(_world, model));


			// Far
			model = GetWPFCube(new Point3D(min.X, min.Y - depthY, min.Z - depthZ), new Point3D(max.X + depthX, max.Y + depthY, min.Z), Colors.PowderBlue, true);
			_viewport.Children.Add(model);
			retVal.Add(new TerrianBody3D(_world, model));

			// Near
			model = GetWPFCube(new Point3D(min.X - depthX, min.Y - depthY, max.Z), new Point3D(max.X, max.Y + depthY, max.Z + depthZ), Colors.Blue, true);
			_viewport.Children.Add(model);
			retVal.Add(new TerrianBody3D(_world, model));

			// Exit Function
			return retVal.ToArray();
		}
		private TerrianBody3D[] GetBoundry3(Point3D min, Point3D max)
		{
			List<TerrianBody3D> retVal = new List<TerrianBody3D>();

			double depthX = ((max.Y - min.Y) + (max.Z - min.Z)) / 2d;
			double depthY = ((max.X - min.X) + (max.Z - min.Z)) / 2d;
			double depthZ = ((max.X - min.X) + (max.Y - min.Y)) / 2d;

			// Left
			ModelVisual3D model = GetWPFCubeTransparent(new Point3D(min.X - depthX, min.Y - depthY, min.Z - depthZ), new Point3D(min.X, max.Y + depthY, max.Z));
			_viewport.Children.Add(model);
			retVal.Add(new TerrianBody3D(_world, model));

			// Right
			model = GetWPFCubeTransparent(new Point3D(max.X, min.Y - depthY, min.Z), new Point3D(max.X + depthX, max.Y + depthY, max.Z + depthZ));
			_viewport.Children.Add(model);
			retVal.Add(new TerrianBody3D(_world, model));


			// Top
			model = GetWPFCubeTransparent(new Point3D(min.X, max.Y, min.Z), new Point3D(max.X, max.Y + depthY, max.Z));
			_viewport.Children.Add(model);
			retVal.Add(new TerrianBody3D(_world, model));

			// Bottom
			model = GetWPFCubeTransparent(new Point3D(min.X, min.Y - depthY, min.Z), new Point3D(max.X, min.Y, max.Z));
			_viewport.Children.Add(model);
			retVal.Add(new TerrianBody3D(_world, model));


			// Far
			model = GetWPFCubeTransparent(new Point3D(min.X, min.Y - depthY, min.Z - depthZ), new Point3D(max.X + depthX, max.Y + depthY, min.Z));
			_viewport.Children.Add(model);
			retVal.Add(new TerrianBody3D(_world, model));

			// Near
			model = GetWPFCubeTransparent(new Point3D(min.X - depthX, min.Y - depthY, max.Z), new Point3D(max.X, max.Y + depthY, max.Z + depthZ));
			_viewport.Children.Add(model);
			retVal.Add(new TerrianBody3D(_world, model));

			// Exit Function
			return retVal.ToArray();
		}
		private TerrianBody3D[] GetBoundry4(Point3D min, Point3D max, bool useColor, bool drawInnerLines, bool drawOuterLines)
		{
			const double MAXDEPTH = 1000d;

			List<TerrianBody3D> retVal = new List<TerrianBody3D>();

			double depthX = ((max.Y - min.Y) + (max.Z - min.Z)) / 2d;
			double depthY = ((max.X - min.X) + (max.Z - min.Z)) / 2d;
			double depthZ = ((max.X - min.X) + (max.Y - min.Y)) / 2d;

			if (depthX > MAXDEPTH)
			{
				depthX = MAXDEPTH;
			}
			if (depthY > MAXDEPTH)
			{
				depthY = MAXDEPTH;
			}
			if (depthZ > MAXDEPTH)
			{
				depthZ = MAXDEPTH;
			}

			//Color colorX = Color.FromArgb(255, 128, 128, 128);
			//Color colorY = Color.FromArgb(255, 96, 96, 96);
			//Color colorZ = Color.FromArgb(255, 160, 160, 160);
			Color colorX = Colors.Gray;
			Color colorY = Colors.Gray;
			Color colorZ = Colors.Gray;

			#region Terrains

			// Left
			ModelVisual3D model = GetWPFCube4(new Point3D(min.X - depthX, min.Y - depthY, min.Z - depthZ), new Point3D(min.X, max.Y + depthY, max.Z), colorX, useColor);
			_viewport.Children.Add(model);
			retVal.Add(new TerrianBody3D(_world, model));

			// Right
			model = GetWPFCube4(new Point3D(max.X, min.Y - depthY, min.Z), new Point3D(max.X + depthX, max.Y + depthY, max.Z + depthZ), colorX, useColor);
			_viewport.Children.Add(model);
			retVal.Add(new TerrianBody3D(_world, model));


			// Top
			model = GetWPFCube4(new Point3D(min.X, max.Y, min.Z), new Point3D(max.X, max.Y + depthY, max.Z), colorY, useColor);
			_viewport.Children.Add(model);
			retVal.Add(new TerrianBody3D(_world, model));

			// Bottom
			model = GetWPFCube4(new Point3D(min.X, min.Y - depthY, min.Z), new Point3D(max.X, min.Y, max.Z), colorY, useColor);
			_viewport.Children.Add(model);
			retVal.Add(new TerrianBody3D(_world, model));


			// Far
			model = GetWPFCube4(new Point3D(min.X, min.Y - depthY, min.Z - depthZ), new Point3D(max.X + depthX, max.Y + depthY, min.Z), colorZ, useColor);
			_viewport.Children.Add(model);
			retVal.Add(new TerrianBody3D(_world, model));

			// Near
			model = GetWPFCube4(new Point3D(min.X - depthX, min.Y - depthY, max.Z), new Point3D(max.X, max.Y + depthY, max.Z + depthZ), colorZ, useColor);
			_viewport.Children.Add(model);
			retVal.Add(new TerrianBody3D(_world, model));

			#endregion

			Color colorInner = Colors.WhiteSmoke;
			Color colorOuter = Colors.Gold;
			double lineThickness = 1d;
			//if (useColor)
			//{
			//    lineThickness = 2d;
			//}
			ScreenSpaceLines3D line = null;

			#region Inner Lines

			if (drawInnerLines)
			{
				// Far
				line = new ScreenSpaceLines3D();
				line.Thickness = lineThickness;
				line.Color = colorInner;
				line.AddLine(new Point3D(min.X, min.Y, min.Z), new Point3D(max.X, min.Y, min.Z));
				_viewport.Children.Add(line);

				line = new ScreenSpaceLines3D();
				line.Thickness = lineThickness;
				line.Color = colorInner;
				line.AddLine(new Point3D(min.X, max.Y, min.Z), new Point3D(max.X, max.Y, min.Z));
				_viewport.Children.Add(line);

				line = new ScreenSpaceLines3D();
				line.Thickness = lineThickness;
				line.Color = colorInner;
				line.AddLine(new Point3D(min.X, min.Y, min.Z), new Point3D(min.X, max.Y, min.Z));
				_viewport.Children.Add(line);

				line = new ScreenSpaceLines3D();
				line.Thickness = lineThickness;
				line.Color = colorInner;
				line.AddLine(new Point3D(max.X, min.Y, min.Z), new Point3D(max.X, max.Y, min.Z));
				_viewport.Children.Add(line);

				// Near
				line = new ScreenSpaceLines3D();
				line.Thickness = lineThickness;
				line.Color = colorInner;
				line.AddLine(new Point3D(min.X, min.Y, max.Z), new Point3D(max.X, min.Y, max.Z));
				_viewport.Children.Add(line);

				line = new ScreenSpaceLines3D();
				line.Thickness = lineThickness;
				line.Color = colorInner;
				line.AddLine(new Point3D(min.X, max.Y, max.Z), new Point3D(max.X, max.Y, max.Z));
				_viewport.Children.Add(line);

				line = new ScreenSpaceLines3D();
				line.Thickness = lineThickness;
				line.Color = colorInner;
				line.AddLine(new Point3D(min.X, min.Y, max.Z), new Point3D(min.X, max.Y, max.Z));
				_viewport.Children.Add(line);

				line = new ScreenSpaceLines3D();
				line.Thickness = lineThickness;
				line.Color = colorInner;
				line.AddLine(new Point3D(max.X, min.Y, max.Z), new Point3D(max.X, max.Y, max.Z));
				_viewport.Children.Add(line);

				// Connecting Z's
				line = new ScreenSpaceLines3D();
				line.Thickness = lineThickness;
				line.Color = colorInner;
				line.AddLine(new Point3D(min.X, min.Y, min.Z), new Point3D(min.X, min.Y, max.Z));
				_viewport.Children.Add(line);

				line = new ScreenSpaceLines3D();
				line.Thickness = lineThickness;
				line.Color = colorInner;
				line.AddLine(new Point3D(min.X, max.Y, min.Z), new Point3D(min.X, max.Y, max.Z));
				_viewport.Children.Add(line);

				line = new ScreenSpaceLines3D();
				line.Thickness = lineThickness;
				line.Color = colorInner;
				line.AddLine(new Point3D(max.X, min.Y, min.Z), new Point3D(max.X, min.Y, max.Z));
				_viewport.Children.Add(line);

				line = new ScreenSpaceLines3D();
				line.Thickness = lineThickness;
				line.Color = colorInner;
				line.AddLine(new Point3D(max.X, max.Y, min.Z), new Point3D(max.X, max.Y, max.Z));
				_viewport.Children.Add(line);
			}

			#endregion
			#region Outer Lines

			if (drawOuterLines)
			{
				// Far
				line = new ScreenSpaceLines3D();
				line.Thickness = lineThickness;
				line.Color = colorOuter;
				line.AddLine(new Point3D(min.X - depthX, min.Y - depthY, min.Z - depthZ), new Point3D(max.X + depthX, min.Y - depthY, min.Z - depthZ));
				_viewport.Children.Add(line);

				line = new ScreenSpaceLines3D();
				line.Thickness = lineThickness;
				line.Color = colorOuter;
				line.AddLine(new Point3D(min.X - depthX, max.Y + depthY, min.Z - depthZ), new Point3D(max.X + depthX, max.Y + depthY, min.Z - depthZ));
				_viewport.Children.Add(line);

				line = new ScreenSpaceLines3D();
				line.Thickness = lineThickness;
				line.Color = colorOuter;
				line.AddLine(new Point3D(min.X - depthX, min.Y - depthY, min.Z - depthZ), new Point3D(min.X - depthX, max.Y + depthY, min.Z - depthZ));
				_viewport.Children.Add(line);

				line = new ScreenSpaceLines3D();
				line.Thickness = lineThickness;
				line.Color = colorOuter;
				line.AddLine(new Point3D(max.X + depthX, min.Y - depthY, min.Z - depthZ), new Point3D(max.X + depthX, max.Y + depthY, min.Z - depthZ));
				_viewport.Children.Add(line);

				// Near
				line = new ScreenSpaceLines3D();
				line.Thickness = lineThickness;
				line.Color = colorOuter;
				line.AddLine(new Point3D(min.X - depthX, min.Y - depthY, max.Z + depthZ), new Point3D(max.X + depthX, min.Y - depthY, max.Z + depthZ));
				_viewport.Children.Add(line);

				line = new ScreenSpaceLines3D();
				line.Thickness = lineThickness;
				line.Color = colorOuter;
				line.AddLine(new Point3D(min.X - depthX, max.Y + depthY, max.Z + depthZ), new Point3D(max.X + depthX, max.Y + depthY, max.Z + depthZ));
				_viewport.Children.Add(line);

				line = new ScreenSpaceLines3D();
				line.Thickness = lineThickness;
				line.Color = colorOuter;
				line.AddLine(new Point3D(min.X - depthX, min.Y - depthY, max.Z + depthZ), new Point3D(min.X - depthX, max.Y + depthY, max.Z + depthZ));
				_viewport.Children.Add(line);

				line = new ScreenSpaceLines3D();
				line.Thickness = lineThickness;
				line.Color = colorOuter;
				line.AddLine(new Point3D(max.X + depthX, min.Y - depthY, max.Z + depthZ), new Point3D(max.X + depthX, max.Y + depthY, max.Z + depthZ));
				_viewport.Children.Add(line);

				// Connecting Z's
				line = new ScreenSpaceLines3D();
				line.Thickness = lineThickness;
				line.Color = colorOuter;
				line.AddLine(new Point3D(min.X - depthX, min.Y - depthY, min.Z - depthZ), new Point3D(min.X - depthX, min.Y - depthY, max.Z + depthZ));
				_viewport.Children.Add(line);

				line = new ScreenSpaceLines3D();
				line.Thickness = lineThickness;
				line.Color = colorOuter;
				line.AddLine(new Point3D(min.X - depthX, max.Y + depthY, min.Z - depthZ), new Point3D(min.X - depthX, max.Y + depthY, max.Z + depthZ));
				_viewport.Children.Add(line);

				line = new ScreenSpaceLines3D();
				line.Thickness = lineThickness;
				line.Color = colorOuter;
				line.AddLine(new Point3D(max.X + depthX, min.Y - depthY, min.Z - depthZ), new Point3D(max.X + depthX, min.Y - depthY, max.Z + depthZ));
				_viewport.Children.Add(line);

				line = new ScreenSpaceLines3D();
				line.Thickness = lineThickness;
				line.Color = colorOuter;
				line.AddLine(new Point3D(max.X + depthX, max.Y + depthY, min.Z - depthZ), new Point3D(max.X + depthX, max.Y + depthY, max.Z + depthZ));
				_viewport.Children.Add(line);
			}

			#endregion

			// Exit Function
			return retVal.ToArray();
		}

		private ModelVisual3D GetWPFCube(Point3D min, Point3D max, Color color, bool isSpecular)
		{
			// Material
			MaterialGroup materials = new MaterialGroup();
			materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
			if (isSpecular)
			{
				materials.Children.Add(new SpecularMaterial(Brushes.White, 100d));
			}

			// Geometry Model
			GeometryModel3D geometry = new GeometryModel3D();
			geometry.Material = materials;
			geometry.Geometry = UtilityWPF.GetCube(min, max);

			// Transform
			Transform3DGroup transform = new Transform3DGroup();		// rotate needs to be added before translate
			//transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(Math3D.GetRandomVectorSpherical(_rand, 10), Math3D.GetNearZeroValue(_rand, 360d))));
			//transform.Children.Add(new TranslateTransform3D(Math3D.GetRandomVector(_rand, _boundryMin, _boundryMax)));

			// Model Visual
			ModelVisual3D retVal = new ModelVisual3D();
			retVal.Content = geometry;
			retVal.Transform = transform;

			// Exit Function
			return retVal;
		}
		private ModelVisual3D GetWPFCubeTransparent(Point3D min, Point3D max)
		{
			// Material
			MaterialGroup materials = new MaterialGroup();
			//materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(128, 128, 128, 128))));
			//if (isSpecular)
			//{
			materials.Children.Add(new SpecularMaterial(Brushes.White, 100d));
			//}

			// Geometry Model
			GeometryModel3D geometry = new GeometryModel3D();
			geometry.Material = materials;
			geometry.Geometry = UtilityWPF.GetCube(min, max);

			// Transform
			Transform3DGroup transform = new Transform3DGroup();		// rotate needs to be added before translate
			//transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(Math3D.GetRandomVectorSpherical(_rand, 10), Math3D.GetNearZeroValue(_rand, 360d))));
			//transform.Children.Add(new TranslateTransform3D(Math3D.GetRandomVector(_rand, _boundryMin, _boundryMax)));

			// Model Visual
			ModelVisual3D retVal = new ModelVisual3D();
			retVal.Content = geometry;
			retVal.Transform = transform;

			// Exit Function
			return retVal;
		}
		private ModelVisual3D GetWPFCube4(Point3D min, Point3D max, Color color, bool useColor)
		{
			// Material
			MaterialGroup materials = new MaterialGroup();
			if (useColor)
			{
				materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
			}

			// Geometry Model
			GeometryModel3D geometry = new GeometryModel3D();
			geometry.Material = materials;
			geometry.Geometry = UtilityWPF.GetCube(min, max);

			// Transform
			Transform3DGroup transform = new Transform3DGroup();		// rotate needs to be added before translate
			//transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(Math3D.GetRandomVectorSpherical(_rand, 10), Math3D.GetNearZeroValue(_rand, 360d))));
			//transform.Children.Add(new TranslateTransform3D(Math3D.GetRandomVector(_rand, _boundryMin, _boundryMax)));

			// Model Visual
			ModelVisual3D retVal = new ModelVisual3D();
			retVal.Content = geometry;
			retVal.Transform = transform;

			// Exit Function
			return retVal;
		}

		*/

        #endregion
    }
}
