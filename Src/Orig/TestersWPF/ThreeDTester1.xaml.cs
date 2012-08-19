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
using System.Windows.Threading;

using Game.Orig.Math3D;
using Game.Orig.Map;
using Game.Orig.HelperClassesWPF;

namespace Game.Orig.TestersWPF
{
    /// <summary>
    /// Interaction logic for ThreeDTester1.xaml
    /// </summary>
    public partial class ThreeDTester1 : Window
    {
        #region Declaration Section
        
        // Global
        private const double BOUNDRY = 12d;
        private const double ELAPSEDTIME = 1d;
        private const double PENETRATIONTHRESHOLDPERCENT = .02d;
        
        // Ball
        private const double MAXVELOCITY = .04;
        private const double RADIUS = .5d;
        private const double MASS = .5d;
        private const double ELASTICITY = .75d;
        private const double KINETICFRICTION = .75d;
        private const double STATICFRICTION = 1d;

        private Random _rand = new Random();

        private List<GeometryModel3D> _geometries = new List<GeometryModel3D>();

        // For rotating the single ball
        private bool _isMouseDown = false;
        private Point _lastPos;

        // For running the multi balls
        DispatcherTimer _multiBallTimer = new DispatcherTimer();
        //private List<Ball> _multiBalls = new List<Ball>();
        private SimpleMap _map = new SimpleMap();
        private MyVector _boundryLower = new MyVector(-BOUNDRY, -BOUNDRY, -BOUNDRY);
        private MyVector _boundryUpper = new MyVector(BOUNDRY, BOUNDRY, BOUNDRY);
        //private GravityController _gravController = null;

		private Game.Newt.HelperClasses.TrackBallRoam _trackball = null;

        #endregion

        #region Constructor

        public ThreeDTester1()
        {
            InitializeComponent();

			//	Trackball
			_trackball = new Game.Newt.HelperClasses.TrackBallRoam(_camera);
			_trackball.EventSource = grid1;
			_trackball.AllowZoomOnMouseWheel = true;
			_trackball.Mappings.AddRange(Game.Newt.HelperClasses.TrackBallMapping.GetPrebuilt(Game.Newt.HelperClasses.TrackBallMapping.PrebuiltMapping.MouseComplete));

            //	Setup the map
            _map.CollisionHandler = new CollisionHandler();
            _map.CollisionHandler.PenetrationThresholdPercent = PENETRATIONTHRESHOLDPERCENT;
            _map.TimerPullApartType = PullApartType.Force;
            _map.CollisionHandler.PullApartSpringVelocity = MAXVELOCITY / 10d;

            _multiBallTimer.Interval = TimeSpan.FromMilliseconds(10);
            _multiBallTimer.Tick += new EventHandler(MultiBallTimer_Tick); //new EventHandler(dt_Tick);

            grid1.MouseWheel += new MouseWheelEventHandler(viewport3D1_MouseWheel);
            grid1.MouseDown += new MouseButtonEventHandler(viewport3D1_MouseDown);
            grid1.MouseUp += new MouseButtonEventHandler(viewport3D1_MouseUp);
            grid1.MouseLeave += new MouseEventHandler(viewport3D1_MouseLeave);
            grid1.MouseMove += new MouseEventHandler(viewport3D1_MouseMove);
        }

        #endregion

        #region Event Listeners

        private void viewport3D1_MouseWheel(object sender, MouseWheelEventArgs e)
        {
			if (!_trackball.IsActive)		//	if it's active, I'll let it do the zooming
			{
				_camera.Position = new Point3D(_camera.Position.X, _camera.Position.Y, _camera.Position.Z - e.Delta / 100D);
			}
        }

        #region Single Ball

        private void btnSingleBall_Click(object sender, RoutedEventArgs e)
        {
            ClearEverything();
			_trackball.IsActive = false;

            int separators;
            if (!int.TryParse(textBox1.Text, out separators))
            {
                MessageBox.Show("Couldn't parse the textbox", this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MeshGeometry3D trapazoid = UtilityWPF.GetCube(1);
            //MeshGeometry3D trapazoid = UtilityWPF.GetSphere(separators, 1);


            Material material = new DiffuseMaterial(Brushes.YellowGreen);
            //Material material = new SpecularMaterial(Brushes.YellowGreen, 1d);
            //Material material = new EmissiveMaterial(Brushes.YellowGreen);


            GeometryModel3D geometry = new GeometryModel3D(trapazoid, material);
            geometry.Transform = new Transform3DGroup();

            _geometries.Add(geometry);
            _modelGroup.Children.Add(geometry);
        }

        private void viewport3D1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            _isMouseDown = true;
            _lastPos = GetMousePositionRelativeToCenterOfViewPort();
        }
        private void viewport3D1_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isMouseDown = false;
        }
        private void viewport3D1_MouseLeave(object sender, MouseEventArgs e)
        {
            _isMouseDown = false;
        }
        private void viewport3D1_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isMouseDown || _geometries.Count != 1)        // for single ball
            {
                return;
            }

            // Calculate the change in position from the last mouse move
            Point pos = GetMousePositionRelativeToCenterOfViewPort();
            double dx = pos.X - _lastPos.X;
            double dy = pos.Y - _lastPos.Y;

            double mouseAngle = 0d;
            if (dx != 0d && dy != 0d)
            {
                mouseAngle = Math.Asin(Math.Abs(dy) / Math.Sqrt(Math.Pow(dx, 2) + Math.Pow(dy, 2)));
                if (dx < 0 && dy > 0) mouseAngle += Math.PI / 2;
                else if (dx < 0 && dy < 0) mouseAngle += Math.PI;
                else if (dx > 0 && dy < 0) mouseAngle += Math.PI * 1.5;
            }
            else if (dx == 0 && dy != 0) mouseAngle = Math.Sign(dy) > 0 ? Math.PI / 2 : Math.PI * 1.5;
            else if (dx != 0 && dy == 0) mouseAngle = Math.Sign(dx) > 0 ? 0 : Math.PI;

            double axisAngle = mouseAngle + Math.PI / 2;

            Vector3D axis = new Vector3D(Math.Cos(axisAngle) * 4, Math.Sin(axisAngle) * 4, 0);

            double rotation = 0.01 * Math.Sqrt(Math.Pow(dx, 2) + Math.Pow(dy, 2));

            Transform3DGroup group = _geometries[0].Transform as Transform3DGroup;
            QuaternionRotation3D r = new QuaternionRotation3D(new Quaternion(axis, rotation * 180 / Math.PI));
            //TODO:  Create a combined rotation, instead of continually adding new partial ones
            group.Children.Add(new RotateTransform3D(r));

            _lastPos = pos;
        }

        #endregion

        #region Multi Ball

        private void btnMultiBalls_Click(object sender, RoutedEventArgs e)
        {
            ClearEverything();
			_trackball.IsActive = true;

            int numBalls;
            if(!int.TryParse(textBox2.Text, out numBalls))
            {
                MessageBox.Show("Couldn't parse the textbox", this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            for (int cntr = 0; cntr < numBalls; cntr++)
            {
                double radius = RADIUS;
                double mass = MASS;
                if (chkRandSize.IsChecked != null && chkRandSize.IsChecked.Value)
                {
                    double multiplier = GetScaledValue(.2d, 5d, 0d, 1d, _rand.NextDouble());
                    radius = RADIUS * multiplier;
                    //mass = MASS * multiplier;
                    mass = (4d / 3d) * Math.PI * (radius * radius * radius);
                    //mass = radius * radius * radius;
                }

                AddMultiBall(radius, mass);
            }

            SetRandomVelocities(MAXVELOCITY);

            // Let it rip
            _multiBallTimer.Start();
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            foreach (BallBlip blip in _map.GetAllBlips())
            {
                blip.Ball.StopBall();
            }
        }
        private void btnRandLow_Click(object sender, RoutedEventArgs e)
        {
            SetRandomVelocities(MAXVELOCITY / 3d);
        }
        private void btnRandMed_Click(object sender, RoutedEventArgs e)
        {
            SetRandomVelocities(MAXVELOCITY);
        }
        private void btnRandHigh_Click(object sender, RoutedEventArgs e)
        {
            SetRandomVelocities(MAXVELOCITY * 3d);
        }

		private void btnResetCamera_Click(object sender, RoutedEventArgs e)
		{
			_camera.Position = new Point3D(0, 0, 5);
			_camera.UpDirection = new Vector3D(0, 1, 0);
			_camera.LookDirection = new Vector3D(0, 0, -10);
		}

        private void MultiBallTimer_Tick(object sender, EventArgs e)
        {
            // Physics
            #region OLD
            //for (int cntr = 0; cntr < _multiBalls.Count; cntr++)
            //{
            //    // Run the timer
            //    _multiBalls[cntr].PrepareForNewTimerCycle();
            //    _multiBalls[cntr].TimerTestPosition(ELAPSEDTIME);
            //    _multiBalls[cntr].TimerFinish();
            //}
            #endregion
            _map.PrepareForNewTimerCycle();

            if (chkGravity.IsChecked != null && chkGravity.IsChecked.Value)
            {
                DoGravity();
            }

            _map.Timer(ELAPSEDTIME);


            // Render
            RadarBlip[] blips = _map.GetAllBlips();
            for (int cntr = 0; cntr < blips.Length; cntr++)
            {
                Transform3DGroup group = _geometries[cntr].Transform as Transform3DGroup;
                group.Children.Clear();
                group.Children.Add(new TranslateTransform3D(blips[cntr].Sphere.Position.X, blips[cntr].Sphere.Position.Y, blips[cntr].Sphere.Position.Z));
            }
        }

        #endregion

        #endregion

        #region Private Methods

        private void ClearEverything()
        {
            _multiBallTimer.Stop();
            _map.Clear();

            if (_geometries.Count > 0)
            {
                foreach (GeometryModel3D geometry in _geometries)
                {
                    _modelGroup.Children.Remove(geometry);
                }
                _geometries.Clear();
            }
        }

        private Point GetMousePositionRelativeToCenterOfViewPort()
        {
            Point pos = Mouse.GetPosition(viewport3D1);
            return new Point(pos.X - viewport3D1.ActualWidth / 2d, viewport3D1.ActualHeight / 2d - pos.Y);
        }

        private void AddMultiBall(double radius, double mass)
        {
            // Physical Ball
            MyVector pos = Utility3D.GetRandomVector(_rand, BOUNDRY);
            DoubleVector dirFacing = new DoubleVector(1, 0, 0, 0, 1, 0);

            Ball ball = new Ball(pos, dirFacing, radius, mass, ELASTICITY, KINETICFRICTION, STATICFRICTION, _boundryLower, _boundryUpper);

            BallBlip blip = new BallBlip(ball, CollisionStyle.Standard, RadarBlipQual.BallUserDefined00, _map.GetNextToken());
            _map.Add(blip);

            // WPF Rendering
            Geometry3D geometry = UtilityWPF.GetSphere(5, radius);
            Material material = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(255, Convert.ToByte(_rand.Next(256)), Convert.ToByte(_rand.Next(256)), Convert.ToByte(_rand.Next(256)))));
            GeometryModel3D geometryModel = new GeometryModel3D(geometry, material);
            geometryModel.Transform = new Transform3DGroup();

            //TODO:  Tie this transform directly to the ball's velocity (the shpere class should take the transform group)
            Transform3DGroup group = geometryModel.Transform as Transform3DGroup;
            group.Children.Clear();
            group.Children.Add(new TranslateTransform3D(pos.X, pos.Y, pos.Z));

            _geometries.Add(geometryModel);
            _modelGroup.Children.Add(geometryModel);
        }

        private void DoGravity()
        {
            const double GRAVITATIONALCONSTANT = .001d; //500d;
            //double gravitationalConstantAdjusted = GRAVITATIONALCONSTANT * _gravityMultiplier;

            RadarBlip[] blips = _map.GetAllBlips();

            for (int outerCntr = 0; outerCntr < blips.Length - 1; outerCntr++)
            {
                Ball ball1 = blips[outerCntr].Sphere as Ball;

                for (int innerCntr = outerCntr + 1; innerCntr < blips.Length; innerCntr++)
                {
                    #region Apply Gravity

                    Ball ball2 = blips[innerCntr].Sphere as Ball;

                    MyVector centerMass1, centerMass2;
                    if (ball1 is TorqueBall)
                    {
                        centerMass1 = ball1.Position + ((TorqueBall)ball1).CenterOfMass;
                    }
                    else
                    {
                        centerMass1 = ball1.Position;
                    }

                    if (ball2 is TorqueBall)
                    {
                        centerMass2 = ball2.Position + ((TorqueBall)ball2).CenterOfMass;
                    }
                    else
                    {
                        centerMass2 = ball2.Position;
                    }

                    MyVector gravityLink = centerMass1 - centerMass2;

                    double force = GRAVITATIONALCONSTANT * (ball1.Mass * ball2.Mass) / gravityLink.GetMagnitudeSquared();

                    gravityLink.BecomeUnitVector();
                    gravityLink.Multiply(force);

                    if (blips[innerCntr].CollisionStyle == CollisionStyle.Standard)
                    {
                        ball2.ExternalForce.Add(gravityLink);
                    }

                    if (blips[outerCntr].CollisionStyle == CollisionStyle.Standard)
                    {
                        gravityLink.Multiply(-1d);
                        ball1.ExternalForce.Add(gravityLink);
                    }

                    #endregion
                }
            }
        }

        /// <summary>
        /// This is good for converting a trackbar into a double
        /// </summary>
        /// <remarks>
        /// From UtilityHelper
        /// </remarks>
        /// <param name="minReturn">This is the value that will be returned when valueRange == minRange</param>
        /// <param name="maxReturn">This is the value that will be returned with valueRange == maxRange</param>
        /// <param name="minRange">The lowest value that valueRange can be</param>
        /// <param name="maxRange">The highest value that valueRange can be</param>
        /// <param name="valueRange">The trackbar's value</param>
        /// <returns>Somewhere between minReturn and maxReturn</returns>
        public static double GetScaledValue(double minReturn, double maxReturn, double minRange, double maxRange, double valueRange)
        {
            //	Get the percent of value within the range
            double percent = (valueRange - minRange) / (maxRange - minRange);

            //	Get the lerp between the return range
            return minReturn + (percent * (maxReturn - minReturn));
        }

        private void SetRandomVelocities(double maxVelocity)
        {
            foreach (BallBlip blip in _map.GetAllBlips())
            {
                blip.Ball.Velocity.StoreNewValues(Utility3D.GetRandomVectorSpherical(_rand, maxVelocity));
            }
        }

        #endregion
    }
}
