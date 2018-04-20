using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v1.NewtonDynamics1;

namespace Game.Newt.Testers
{
    /// <summary>
    /// Interaction logic for CameraTester.xaml
    /// </summary>
    public partial class CameraTester : Window
    {
        #region Class: TrackBallRoam

        public class TrackBallRoam_local
        {
            #region Events

            //public event GetOrbitRadiusHandler GetOrbitRadius = null;

            #endregion

            #region Declaration Section

            private FrameworkElement _eventSource;

            private Point _previousPosition2D;
            private Vector3D _previousPosition3D = new Vector3D(0, 0, 1);

            //private Transform3DGroup _transform;
            //private ScaleTransform3D _scale = new ScaleTransform3D();
            //private AxisAngleRotation3D _rotation = new AxisAngleRotation3D();
            //private TranslateTransform3D _translate = new TranslateTransform3D();

            /// <summary>
            /// This trackball class doesn't directly manipulate the camera.  I just need a reference to the camera so
            /// I can orient mouse movements to the camera's coords.
            /// </summary>
            private PerspectiveCamera _camera = null;

            /// <summary>
            /// This is used when they are orbiting the camera around a point (it gets set back to null in the mouse up)
            /// </summary>
            private double? _orbitRadius = null;

            private AutoScrollEmulator _autoscroll = null;

            #endregion

            #region Constructor

            public TrackBallRoam_local(PerspectiveCamera camera)
            {
                _camera = camera;

                _autoscroll = new AutoScrollEmulator();
                _autoscroll.AutoScroll += new AutoScrollHandler(Autoscroll_AutoScroll);
                _autoscroll.CursorChanged += new EventHandler(Autoscroll_CursorChanged);
            }

            #endregion

            #region Public Properties

            /// <summary>
            /// The FrameworkElement we listen to for mouse events.
            /// </summary>
            public FrameworkElement EventSource
            {
                get
                {
                    return _eventSource;
                }
                set
                {
                    if (_eventSource != null)
                    {
                        _eventSource.MouseWheel -= this.EventSource_MouseWheel;
                        _eventSource.MouseDown -= this.EventSource_MouseDown;
                        _eventSource.MouseUp -= this.EventSource_MouseUp;
                        _eventSource.MouseMove -= this.EventSource_MouseMove;
                    }

                    _eventSource = value;

                    if (_eventSource != null)
                    {
                        _eventSource.MouseWheel += this.EventSource_MouseWheel;
                        _eventSource.MouseDown += this.EventSource_MouseDown;
                        _eventSource.MouseUp += this.EventSource_MouseUp;
                        _eventSource.MouseMove += this.EventSource_MouseMove;
                    }
                }
            }

            #endregion

            #region Protected Methods

            protected virtual double OnGetOrbitRadius()
            {
                //TODO:  Don't just get the radius, by get a live point to follow

                //TODO:  Raise an event, requesting the radius



                // If execuation gets here, then the event never returned anything.  Figure out the radius myself
                return _camera.Position.ToVector().Length;

            }

            #endregion

            #region Event Listeners

            private void EventSource_MouseWheel(object sender, MouseWheelEventArgs e)
            {
                Vector3D delta = _camera.LookDirection;
                delta.Normalize();
                delta = delta * (e.Delta * .01d);

                _camera.Position = _camera.Position + delta;
            }

            private void EventSource_MouseDown(object sender, MouseEventArgs e)
            {
                // By capturing the mouse, mouse events will still come in even when they are moving the mouse
                // outside the element/form
                Mouse.Capture(_eventSource, CaptureMode.SubTree);		// I had a case where I used the grid as the event source.  If they clicked one of the 3D objects, the scene would jerk.  But by saying subtree, I still get the event

                _previousPosition2D = e.GetPosition(_eventSource);
                _previousPosition3D = TrackballTransform.ProjectToTrackball(_eventSource.ActualWidth, _eventSource.ActualHeight, _previousPosition2D);

                #region Detect Autoscroll

                if (e.LeftButton == MouseButtonState.Pressed)
                {
                }
                else if (e.MiddleButton == MouseButtonState.Pressed)
                {
                    if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                    {
                    }
                    else if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                    {
                    }
                    else
                    {
                        _autoscroll.StartAutoScroll(_previousPosition2D, UtilityWPF.TransformToScreen(_previousPosition2D, _eventSource));
                    }
                }

                #endregion
            }
            private void EventSource_MouseUp(object sender, MouseEventArgs e)
            {
                if (e.MiddleButton != MouseButtonState.Pressed)
                {
                    _autoscroll.StopAutoScroll();
                }

                Mouse.Capture(_eventSource, CaptureMode.None);
                _orbitRadius = null;
            }

            private void EventSource_MouseMove(object sender, MouseEventArgs e)
            {
                Point currentPosition = e.GetPosition(_eventSource);

                // avoid any zero axis conditions
                if (currentPosition == _previousPosition2D)
                {
                    return;
                }

                _autoscroll.MouseMove(currentPosition);

                Vector3D currentPosition3D = TrackballTransform.ProjectToTrackball(_eventSource.ActualWidth, _eventSource.ActualHeight, currentPosition);

                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    SlideCamera(currentPosition);
                }
                else if (e.MiddleButton == MouseButtonState.Pressed)
                {
                    if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                    {
                        RotateCameraAroundLookDir(currentPosition, currentPosition3D);
                    }
                    else if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                    {
                        ZoomCamera(currentPosition);
                    }
                    else
                    {
                        // Nothing to do here, this is taken care of in the mouse down and mouse up
                        //AutoscrollCamera(currentPosition);
                    }
                }
                else if (e.RightButton == MouseButtonState.Pressed)
                {
                    if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                    {
                        RotateCamera(currentPosition);
                    }
                    else
                    {
                        OrbitCamera(currentPosition, currentPosition3D);
                    }
                }

                _previousPosition2D = currentPosition;
                _previousPosition3D = currentPosition3D;
            }

            private void Autoscroll_AutoScroll(object sender, AutoScrollArgs e)
            {
                SlideCamera(e.XDelta / (-60d * 2d), e.YDelta / (-60d * 2d));
            }
            private void Autoscroll_CursorChanged(object sender, EventArgs e)
            {
                _eventSource.Cursor = _autoscroll.SuggestedCursor;
            }

            #endregion

            #region Private Methods

            private void SlideCamera(Point currentPosition)
            {
                // Figure out the change in mouse position
                double dX = currentPosition.X - _previousPosition2D.X;
                double dY = currentPosition.Y - _previousPosition2D.Y;

                dX /= 60;
                dY /= 60;

                // Call the other overload
                SlideCamera(dX, dY);
            }
            private void SlideCamera(double deltaX, double deltaY)
            {
                Vector3D camZ = _camera.LookDirection;
                camZ.Normalize();
                Vector3D camX = -Vector3D.CrossProduct(camZ, _camera.UpDirection);
                camX.Normalize();
                Vector3D camY = Vector3D.CrossProduct(camZ, camX);
                camY.Normalize();

                Vector3D vSlide = (camX * deltaX) + (camY * deltaY);

                Point3D cameraPos = _camera.Position;
                cameraPos += vSlide;
                _camera.Position = cameraPos;
            }

            private void ZoomCamera(Point currentPosition)
            {
                double yDelta = currentPosition.Y - _previousPosition2D.Y;

                yDelta /= 6;

                Point3D position = _camera.Position;

                position -= _camera.LookDirection * yDelta;

                _camera.Position = position;
            }

            private void RotateCameraAroundLookDir(Point currentPosition, Vector3D currentPosition3D)
            {
                Vector3D curPos3DFlat = currentPosition3D;
                curPos3DFlat.Z = 0;
                curPos3DFlat.Normalize();

                Vector3D prevPos3DFlat = _previousPosition3D;
                prevPos3DFlat.Z = 0;
                prevPos3DFlat.Normalize();

                Vector3D axis = Vector3D.CrossProduct(prevPos3DFlat, curPos3DFlat);
                double angle = Vector3D.AngleBetween(prevPos3DFlat, curPos3DFlat);

                // Quaterion will throw if this happens - sometimes we can get 3D positions that are very similar, so we
                // avoid the throw by doing this check and just ignoring the event 
                if (axis.Length == 0)
                {
                    return;
                }

                // Now need to rotate the axis into the camera's coords
                // Get the camera's current view matrix.
                Matrix3D viewMatrix = MathUtils.GetViewMatrix(_camera);
                viewMatrix.Invert();

                // Transform the trackball rotation axis relative to the camera orientation.
                axis = viewMatrix.Transform(axis);

                Quaternion deltaRotation = new Quaternion(axis, -angle);
                //Quaternion deltaRotation = new Quaternion(_camera.LookDirection, angle);

                Vector3D[] vectors = new Vector3D[] { _camera.UpDirection, _camera.LookDirection };

                deltaRotation.GetRotatedVector(vectors);

                // Apply the changes
                _camera.UpDirection = vectors[0];
                _camera.LookDirection = vectors[1];
            }

            private void RotateCamera(Point currentPosition)
            {
                Vector3D camZ = _camera.LookDirection;
                camZ.Normalize();
                Vector3D camX = -Vector3D.CrossProduct(camZ, _camera.UpDirection);
                camX.Normalize();
                Vector3D camY = Vector3D.CrossProduct(camZ, camX);
                camY.Normalize();

                double dX = currentPosition.X - _previousPosition2D.X;
                double dY = currentPosition.Y - _previousPosition2D.Y;

                dX *= .1;
                dY *= -.1;

                AxisAngleRotation3D aarY = new AxisAngleRotation3D(camY, dX);
                AxisAngleRotation3D aarX = new AxisAngleRotation3D(camX, dY);

                RotateTransform3D rotY = new RotateTransform3D(aarY);
                RotateTransform3D rotX = new RotateTransform3D(aarX);

                camZ = camZ * rotY.Value * rotX.Value;
                camZ.Normalize();
                camY = camY * rotX.Value * rotY.Value;
                camY.Normalize();

                _camera.LookDirection = camZ;
                _camera.UpDirection = camY;
            }

            private void OrbitCamera(Point currentPosition, Vector3D currentPosition3D)
            {
                #region Get Mouse Movement - Spherical

                // Figure out a rotation axis and angle
                Vector3D axis = Vector3D.CrossProduct(_previousPosition3D, currentPosition3D);
                double angle = Vector3D.AngleBetween(_previousPosition3D, currentPosition3D);

                // Quaterion will throw if this happens - sometimes we can get 3D positions that are very similar, so we
                // avoid the throw by doing this check and just ignoring the event 
                if (axis.Length == 0)
                {
                    return;
                }

                // Now need to rotate the axis into the camera's coords
                // Get the camera's current view matrix.
                Matrix3D viewMatrix = MathUtils.GetViewMatrix(_camera);
                viewMatrix.Invert();

                // Transform the trackball rotation axis relative to the camera orientation.
                axis = viewMatrix.Transform(axis);

                Quaternion deltaRotation = new Quaternion(axis, -angle);

                #endregion

                // This can't be calculated each mose move.  It causes a wobble when the look direction isn't pointed directly at the origin
                if (_orbitRadius == null)
                {
                    _orbitRadius = OnGetOrbitRadius();
                }

                // Figure out the offset in world coords
                Vector3D lookLine = _camera.LookDirection;
                lookLine.Normalize();
                lookLine = lookLine * _orbitRadius.Value;

                Point3D orbitPointWorld = _camera.Position + lookLine;

                // Get the opposite of the look line (the line from the orbit center to the camera's position)
                Vector3D lookLineOpposite = lookLine * -1d;

                // Rotate
                Vector3D[] vectors = new Vector3D[] { lookLineOpposite, _camera.UpDirection, _camera.LookDirection };

                deltaRotation.GetRotatedVector(vectors);

                // Apply the changes
                _camera.Position = orbitPointWorld + vectors[0];
                _camera.UpDirection = vectors[1];
                _camera.LookDirection = vectors[2];
            }

            #endregion
        }

        #endregion
        #region Class: TrackballTransform

        /// <summary>
        ///     Trackball is a utility class which observes the mouse events
        ///     on a specified FrameworkElement and produces a Transform3D
        ///     with the resultant rotation and scale.
        /// 
        ///     Example Usage:
        /// 
        ///         Trackball trackball = new Trackball();
        ///         trackball.EventSource = myElement;
        ///         myViewport3D.Camera.Transform = trackball.Transform;
        /// 
        ///     Because Viewport3Ds only raise events when the mouse is over the
        ///     rendered 3D geometry (as opposed to not when the mouse is within
        ///     the layout bounds) you usually want to use another element as 
        ///     your EventSource.  For example, a transparent border placed on
        ///     top of your Viewport3D works well:
        ///     
        ///         <Grid>
        ///           <ColumnDefinition />
        ///           <RowDefinition />
        ///           <Viewport3D Name="myViewport" ClipToBounds="True" Grid.Row="0" Grid.Column="0" />
        ///           <Border Name="myElement" Background="Transparent" Grid.Row="0" Grid.Column="0" />
        ///         </Grid>
        ///     
        ///     NOTE: The Transform property may be shared by multiple Cameras
        ///           if you want to have auxilary views following the trackball.
        /// 
        ///           It can also be useful to share the Transform property with
        ///           models in the scene that you want to move with the camera.
        ///           (For example, the Trackport3D's headlight is implemented
        ///           this way.)
        /// 
        ///           You may also use a Transform3DGroup to combine the
        ///           Transform property with additional Transforms.
        /// </summary> 
        public class TrackballTransform
        {
            #region Declaration Section

            private FrameworkElement _eventSource;
            private Point _previousPosition2D;
            private Vector3D _previousPosition3D = new Vector3D(0, 0, 1);

            private Transform3DGroup _transform;
            private ScaleTransform3D _scale = new ScaleTransform3D();		// scale is BAAAAD.  Don't keep this
            private AxisAngleRotation3D _rotation = new AxisAngleRotation3D();
            private TranslateTransform3D _translate = new TranslateTransform3D();

            /// <summary>
            /// This trackball class doesn't directly manipulate the camera.  I just need a reference to the camera so
            /// I can orient mouse movements to the camera's coords.
            /// </summary>
            private Camera _camera = null;

            private Double _transScale = .01;

            #endregion

            #region Constructor

            public TrackballTransform(Camera camera)
            {
                _transform = new Transform3DGroup();
                _transform.Children.Add(_scale);
                _transform.Children.Add(new RotateTransform3D(_rotation));
                _transform.Children.Add(_translate);

                _camera = camera;
            }

            #endregion

            #region Public Properties

            /// <summary>
            /// A transform to move the camera or scene to the trackball's
            /// current orientation and scale.
            /// </summary>
            public Transform3D Transform
            {
                get { return _transform; }
            }

            /// <summary>
            /// The FrameworkElement we listen to for mouse events.
            /// </summary>
            public FrameworkElement EventSource
            {
                get
                {
                    return _eventSource;
                }
                set
                {
                    if (_eventSource != null)
                    {
                        _eventSource.MouseDown -= this.OnMouseDown;
                        _eventSource.MouseUp -= this.OnMouseUp;
                        _eventSource.MouseMove -= this.OnMouseMove;
                    }

                    _eventSource = value;

                    if (_eventSource != null)
                    {
                        _eventSource.MouseDown += this.OnMouseDown;
                        _eventSource.MouseUp += this.OnMouseUp;
                        _eventSource.MouseMove += this.OnMouseMove;
                    }
                }
            }

            #endregion

            #region Public Methods

            public static Vector3D ProjectToTrackball(double width, double height, Point point)
            {
                double x = point.X / (width / 2);    // Scale so bounds map to [0,0] - [2,2]
                double y = point.Y / (height / 2);

                x = x - 1;                           // Translate 0,0 to the center
                y = 1 - y;                           // Flip so +Y is up instead of down

                double z2 = 1 - x * x - y * y;       // z^2 = 1 - x^2 - y^2
                double z = z2 > 0 ? Math.Sqrt(z2) : 0;

                return new Vector3D(x, y, z);
            }

            #endregion

            #region Event Listeners

            private void OnMouseDown(object sender, MouseEventArgs e)
            {
                // By capturing the mouse, mouse events will still come in even when they are moving the mouse
                // outside the element/form
                if (Mouse.Captured == null)
                {
                    Mouse.Capture(EventSource, CaptureMode.SubTree);		// I had a case where I used the grid as the event source.  If they clicked one of the 3D objects, the scene would jerk.  But by saying subtree, I still get the event
                }

                _previousPosition2D = e.GetPosition(EventSource);
                _previousPosition3D = ProjectToTrackball(EventSource.ActualWidth, EventSource.ActualHeight, _previousPosition2D);
            }
            private void OnMouseUp(object sender, MouseEventArgs e)
            {
                Mouse.Capture(EventSource, CaptureMode.None);
            }

            private void OnMouseMove(object sender, MouseEventArgs e)
            {
                Point currentPosition = e.GetPosition(EventSource);

                // Prefer tracking to zooming if both buttons are pressed.
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    Translate(currentPosition);
                }
                else if (e.MiddleButton == MouseButtonState.Pressed)
                {
                    Zoom(currentPosition);
                }
                else if (e.RightButton == MouseButtonState.Pressed)
                {
                    Track(currentPosition);
                }

                _previousPosition2D = currentPosition;
            }

            #endregion

            #region Private Methods

            private void Track(Point currentPosition)
            {
                Vector3D currentPosition3D = ProjectToTrackball(EventSource.ActualWidth, EventSource.ActualHeight, currentPosition);

                Vector3D axis = Vector3D.CrossProduct(_previousPosition3D, currentPosition3D);
                double angle = Vector3D.AngleBetween(_previousPosition3D, currentPosition3D);

                _previousPosition3D = currentPosition3D;

                // Quaterion will throw if this happens - sometimes we can get 3D positions that are very similar, so we
                // avoid the throw by doing this check and just ignoring the event 
                if (axis.Length == 0)
                {
                    return;
                }

                // Now need to rotate the axis into the camera's coords
                // Get the camera's current view matrix.
                Matrix3D viewMatrix = MathUtils.GetViewMatrix(_camera);
                viewMatrix.Invert();

                // Transform the trackball rotation axis relative to the camera orientation.
                axis = viewMatrix.Transform(axis);

                Quaternion delta = new Quaternion(axis, -angle);

                // Get the current orientantion from the RotateTransform3D
                AxisAngleRotation3D r = _rotation;
                Quaternion q = new Quaternion(_rotation.Axis, _rotation.Angle);

                // Compose the delta with the previous orientation
                q *= delta;

                // Write the new orientation back to the Rotation3D
                _rotation.Axis = q.Axis;
                _rotation.Angle = q.Angle;
            }

            private void Zoom(Point currentPosition)
            {
                // Scale doesn't let you fly around, just zoom in to near zero

                double yDelta = currentPosition.Y - _previousPosition2D.Y;

                double scale = Math.Exp(yDelta / 100);    // e^(yDelta/100) is fairly arbitrary.

                _scale.ScaleX *= scale;
                _scale.ScaleY *= scale;
                _scale.ScaleZ *= scale;
            }

            private void Translate(Point currentPosition)
            {
                // Calculate the panning vector from screen(the vector component of the Quaternion
                // the division of the X and Y components scales the vector to the mouse movement
                double axisX = (_previousPosition2D.X - currentPosition.X) * _transScale;
                double axisY = (currentPosition.Y - _previousPosition2D.Y) * _transScale;
                Vector3D axis = new Vector3D(axisX, axisY, 0);

                // Now need to rotate the axis into the camera's coords
                // Get the camera's current view matrix.
                Matrix3D viewMatrix = MathUtils.GetViewMatrix(_camera);
                viewMatrix.Invert();

                // Transform the trackball rotation axis relative to the camera orientation.
                axis = viewMatrix.Transform(axis);

                Quaternion qV = new Quaternion(axis.X, axis.Y, axis.Z, 0);		// I need to explicitely set x,y,z, or it will be stored as an identity because the angle is zero

                // Get the current orientantion from the RotateTransform3D
                Quaternion q = new Quaternion(_rotation.Axis, _rotation.Angle);
                Quaternion qC = q;
                qC.Conjugate();

                // Here we rotate our panning vector about the the rotaion axis of any current rotation transform
                // and then sum the new translation with any exisiting translation
                qV = q * qV * qC;
                _translate.OffsetX += qV.X;
                _translate.OffsetY += qV.Y;
                _translate.OffsetZ += qV.Z;
            }

            #endregion
        }

        #endregion

        #region Declaration Section

        private Random _rand = new Random();

        private List<ConvexBody3D> _cubes = new List<ConvexBody3D>();

        private Point _lastPos;

        private PerspectiveCamera _camera2 = new PerspectiveCamera();
        private TrackballTransform _trackball2 = null;

        private PerspectiveCamera _camera3 = new PerspectiveCamera();
        private TrackBallRoam_local _trackball3 = null;

        private PerspectiveCamera _camera4 = new PerspectiveCamera();
        private TrackBallRoam _trackball4 = null;

        #endregion

        #region Constructor

        public CameraTester()
        {
            InitializeComponent();

            grdPanel.Background = SystemColors.ControlBrush;

            #region Init World

            _world.InitialiseBodies();
            _world.Gravity = 0d;


            //TODO:  Implement this
            //_world.SetBoundry(new Vector3D(-15, -15, -15), new Vector3D(15, 15, 15));


            _world.UnPause();

            #endregion

            #region Make cubes

            CreateCube(Colors.White, new Point3D());

            CreateCube(Colors.Pink, new Point3D(-5, 0, 0));
            CreateCube(Colors.Red, new Point3D(5, 0, 0));

            CreateCube(Colors.Chartreuse, new Point3D(0, -5, 0));
            CreateCube(Colors.Green, new Point3D(0, 5, 0));

            CreateCube(Colors.PowderBlue, new Point3D(0, 0, -5));
            CreateCube(Colors.Blue, new Point3D(0, 0, 5));

            #endregion

            grdViewPort.MouseWheel += new MouseWheelEventHandler(viewport3D1_MouseWheel);
            grdViewPort.MouseDown += new MouseButtonEventHandler(viewport3D1_MouseDown);
            grdViewPort.MouseMove += new MouseEventHandler(viewport3D1_MouseMove);

            radCamera_Checked(this, new RoutedEventArgs());
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// This converts 2D movement into a spherical rotation.  This is used for converting mouse movement into rotating an object
        /// </summary>
        public static Quaternion GetSphericalMovement(double dx, double dy)
        {
            //TODO:  Put this in Math3D

            const double ROTATESPEED = .01d;

            // Get axis of rotation
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

            // Get angle
            double radians = ROTATESPEED * Math.Sqrt((dx * dx) + (dy * dy));
            double degrees = Math1D.RadiansToDegrees(radians);

            // Exit Function
            return new Quaternion(axis, degrees);
        }

        #endregion

        #region Event Listeners

        private void Window_Closed(object sender, EventArgs e)
        {
            if (_cubes != null)
            {
                _cubes = null;
            }

            if (_world != null)
            {
                _world.Dispose();
                _world = null;
            }
        }

        private void radCamera_Checked(object sender, RoutedEventArgs e)
        {
            if (_camera1 == null || _camera2 == null)
            {
                // This event is getting invoked from InitializeComponent
                return;
            }

            #region Disable Controls

            chkIsActive4.IsEnabled = false;
            button1.IsEnabled = false;

            #endregion

            #region Dispose Current Trackballs

            if (_trackball2 != null)
            {
                _trackball2.EventSource = null;
                _trackball2 = null;
            }

            if (_trackball3 != null)
            {
                _trackball3.EventSource = null;
                _trackball3 = null;
            }

            if (_trackball4 != null)
            {
                _trackball4.EventSource = null;
                _trackball4 = null;
            }

            #endregion

            if (radCamera1.IsChecked.Value)
            {
                #region Camera 1

                _camera1.Position = new Point3D(0, 0, 25);
                _camera1.LookDirection = new Vector3D(0, 0, -10);
                _camera1.UpDirection = new Vector3D(0, 1, 0);
                _camera1.FieldOfView = 45;

                _viewport.Camera = _camera1;

                #endregion
            }
            else if (radCamera2.IsChecked.Value)
            {
                #region Camera 2

                //_camera2.Position = new Point3D(0, 0, 25);
                //_camera2.LookDirection = new Vector3D(0, 0, -10);
                //_camera2.UpDirection = new Vector3D(0, 1, 0);
                //_camera2.FieldOfView = 45;

                _camera2.Position = new Point3D(0, 25, 0);		// this alternate location is to test placing the camera somewhere other than the z axis
                _camera2.LookDirection = new Vector3D(0, -1, 0);
                _camera2.UpDirection = new Vector3D(0, 0, -1);
                _camera2.FieldOfView = 45;

                _trackball2 = new TrackballTransform(_camera2);
                _trackball2.EventSource = grdViewPort;
                _camera2.Transform = _trackball2.Transform;

                _viewport.Camera = _camera2;

                #endregion
            }
            else if (radCamera3.IsChecked.Value)
            {
                #region Camera 3

                //_camera3.Position = new Point3D(0, 0, 25);
                //_camera3.LookDirection = new Vector3D(0, 0, -10);
                //_camera3.UpDirection = new Vector3D(0, 1, 0);
                //_camera3.FieldOfView = 45;

                _camera3.Position = new Point3D(0, 25, 0);		// this alternate location is to test placing the camera somewhere other than the z axis
                _camera3.LookDirection = new Vector3D(0, -1, 0);
                _camera3.UpDirection = new Vector3D(0, 0, -1);
                _camera3.FieldOfView = 45;

                _trackball3 = new TrackBallRoam_local(_camera3);
                _trackball3.EventSource = grdViewPort;

                _viewport.Camera = _camera3;

                #endregion
            }
            else if (radCamera4.IsChecked.Value)
            {
                #region Camera 4

                chkIsActive4.IsEnabled = true;
                button1.IsEnabled = true;

                _camera4.Position = new Point3D(0, 0, 25);
                _camera4.LookDirection = new Vector3D(0, 0, -10);
                _camera4.UpDirection = new Vector3D(0, 1, 0);
                _camera4.FieldOfView = 45;

                //_camera4.Position = new Point3D(0, 25, 0);		// this alternate location is to test placing the camera somewhere other than the z axis
                //_camera4.LookDirection = new Vector3D(0, -1, 0);
                //_camera4.UpDirection = new Vector3D(0, 0, -1);
                //_camera4.FieldOfView = 45;

                _trackball4 = new TrackBallRoam(_camera4);
                _trackball4.EventSource = grdViewPort;

                _trackball4.AllowZoomOnMouseWheel = true;
                _trackball4.Mappings.AddRange(TrackBallMapping.GetPrebuilt(TrackBallMapping.PrebuiltMapping.MouseComplete));

                _trackball4.IsActive = chkIsActive4.IsChecked.Value;

                _viewport.Camera = _camera4;

                #endregion
            }
            else
            {
                MessageBox.Show("Unknown camera selected", this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                _viewport.Camera = null;
            }
        }

        #region For Camera 1

        private void viewport3D1_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!radCamera1.IsChecked.Value)
            {
                return;
            }

            //_camera1.Position = new Point3D(_camera1.Position.X, _camera1.Position.Y, _camera1.Position.Z - e.Delta / 100D);

            Vector3D delta = _camera1.LookDirection;
            delta.Normalize();
            delta = Vector3D.Multiply(delta, e.Delta / 100d);

            _camera1.Position = Point3D.Add(_camera1.Position, delta);
        }

        private void viewport3D1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //if (e.LeftButton != MouseButtonState.Pressed)
            //{
            //    return;
            //}

            _lastPos = GetMousePositionRelativeToCenterOfViewPort();
        }
        private void viewport3D1_MouseMove(object sender, MouseEventArgs e)
        {
            // Calculate the change in position from the last mouse move
            Point pos = GetMousePositionRelativeToCenterOfViewPort();
            double dx = pos.X - _lastPos.X;
            double dy = pos.Y - _lastPos.Y;


            //this.Title = "pos=(" + pos.X.ToString() + ", " + pos.Y + "), delta=(" + dx.ToString() + ", " + dy.ToString() + ")";

            //Quaternion quat = GetSphericalMovement(dx, dy);
            //this.Title += ", axis=(" + quat.Axis.X.ToString() + ", " + quat.Axis.Y.ToString() + ", " + quat.Axis.Z.ToString() + ") ,degrees=" + quat.Angle.ToString();

            //if(Keyboard.IsKeyDown(

            //TODO: Let the user define the ratios

            //TODO: Let the user define mappings (mouse button + keyboard button)
            if (radCamera1.IsChecked.Value)
            {
                if (Mouse.LeftButton == MouseButtonState.Pressed)
                {
                    PanCameraPosition(dx, dy);       // this fails when trying to look down the y axis (probably one too many rotations)
                }
                else if (Mouse.MiddleButton == MouseButtonState.Pressed)
                {
                    //TODO: Let the user define a custom vector to twirl around
                    RotateCameraAroundLookDir(pos.X, pos.Y, dx, dy);
                }
                else if (Mouse.RightButton == MouseButtonState.Pressed)
                {
                    RotateCameraSpherical(dx, dy);
                }
            }



            // This one shouldn't be used
            //RotateCameraUsingTransform(dx, dy);



            _lastPos = pos;
        }

        #endregion

        #region For Camera 4

        private void chkIsActive4_Checked(object sender, RoutedEventArgs e)
        {
            if (_trackball4 != null)
            {
                _trackball4.IsActive = chkIsActive4.IsChecked.Value;
            }
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            if (_trackball4 == null)
            {
                MessageBox.Show("Trackball4 isn't active", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            //MessageBox.Show(TrackBallMapping.GetReport(_trackball4.Mappings, _trackball4.AllowZoomOnMouseWheel), this.Title, MessageBoxButton.OK, MessageBoxImage.Information);
            MessageBox.Show(_trackball4.GetMappingReport(), this.Title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #endregion

        #region Private Methods

        private void CreateCube(Color color, Point3D location)
        {
            #region WPF Model

            // Material
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
            materials.Children.Add(new SpecularMaterial(Brushes.White, 100d));

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.Geometry = UtilityWPF.GetCube(1d);
            //geometry.Geometry = UtilityWPF.GetRectangle3D()

            // Transform
            Transform3DGroup transform = new Transform3DGroup();		// rotate needs to be added before translate
            //transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(Math3D.GetRandomVectorSpherical(_rand, 10), Math3D.GetNearZeroValue(_rand, 360d))));
            transform.Children.Add(new TranslateTransform3D(location.ToVector()));

            // Model Visual
            ModelVisual3D model = new ModelVisual3D();
            model.Content = geometry;
            model.Transform = transform;

            #endregion

            // Add to the viewport
            _viewport.Children.Add(model);

            // Make a physics body that represents this cube
            ConvexBody3D body = new ConvexBody3D(_world, model);
            body.Mass = 1f;

            _cubes.Add(body);
        }

        private Point GetMousePositionRelativeToCenterOfViewPort()
        {
            Point pos = Mouse.GetPosition(_viewport);
            return new Point(pos.X - _viewport.ActualWidth / 2d, _viewport.ActualHeight / 2d - pos.Y);
        }

        /// <summary>
        /// This isn't a good way of manipulating the camera
        /// </summary>
        private void RotateCameraUsingTransform(double dx, double dy)
        {
            // Turn the mouse movement into 3D rotation
            Quaternion quat = GetSphericalMovement(dx, dy);

            Transform3DGroup transform = _camera1.Transform as Transform3DGroup;

            RotateTransform3D rotation = new RotateTransform3D(new AxisAngleRotation3D(quat.Axis, quat.Angle * -1d));
            transform.Children.Add(rotation);
        }

        private void PanCameraPosition(double dx, double dy)
        {
            const double PANSPEED = .01d;

            Vector3D delta = GetRotatedDelta(-dx * PANSPEED, -dy * PANSPEED);

            _camera1.Position = delta + _camera1.Position;
        }

        private void RotateCameraAroundLookDir(double curX, double curY, double dx, double dy)
        {
            const double ROTATESPEED = .005d;

            // Calculate angle
            //double radians = ROTATESPEED * Math.Sqrt(Math.Pow(dx, 2) + Math.Pow(dy, 2));
            double radians = ROTATESPEED * Math.Sqrt((dx * dx) + (dy * dy));
            double degrees = Math1D.RadiansToDegrees(radians);

            if (radians < 0)
            {
                MessageBox.Show("ya");
            }

            // See if I should negate the angle
            //NOTE:  This logic is flawed.  I fixed this later by taking curXY cross prevXY
            if (curX >= 0 && curY >= 0)
            {
                // Q1
                if (dx > 0 || dy < 0)
                {
                    degrees *= -1;
                }
            }
            else if (curX <= 0 && curY >= 0)
            {
                // Q2
                if (dx > 0 || dy > 0)
                {
                    degrees *= -1;
                }
            }
            else if (curX <= 0 && curY <= 0)
            {
                // Q3
                if (dx < 0 || dy > 0)
                {
                    degrees *= -1;
                }
            }
            else if (curX >= 0 && curY <= 0)
            {
                // Q4
                if (dx < 0 || dy < 0)
                {
                    degrees *= -1;
                }
            }

            // Create a matrix that will perform the rotation
            Matrix3D matrix = new Matrix3D();
            matrix.Rotate(new Quaternion(_camera1.LookDirection, degrees));

            // Rotate the camera
            _camera1.UpDirection = matrix.Transform(_camera1.UpDirection);
        }

        private void RotateCameraSpherical(double dx, double dy)
        {
            // Figure out how to rotate the screen into world coords
            Quaternion quatScreenToWorld = GetRotationForDelta();

            // Turn the mouse movement into 3D rotation
            Quaternion quat = GetSphericalMovement(dx, dy);

            // Rotate that
            quat = new Quaternion(quatScreenToWorld.GetRotatedVector(quat.Axis), quat.Angle * .1d);      // also, make it less twitchy
            //quat = new Quaternion(quat.Axis, quat.Angle * .1d);

            // Create a matrix that will perform the rotation
            Matrix3D matrix = new Matrix3D();
            matrix.Rotate(quat);

            // Store the camera's stats
            Vector3D[] vectors = new Vector3D[2];
            vectors[0] = _camera1.LookDirection;
            vectors[1] = _camera1.UpDirection;

            // Rotate those stats
            matrix.Transform(vectors);

            // Apply the new stats
            _camera1.LookDirection = vectors[0];
            _camera1.UpDirection = vectors[1];
        }

        private Quaternion GetRotationForDelta()
        {
            DoubleVector screenPlane = new DoubleVector(1, 0, 0, 0, 1, 0);
            DoubleVector cameraPlane = new DoubleVector(_camera1.LookDirection.GetRotatedVector(_camera1.UpDirection, -90), _camera1.UpDirection);

            return screenPlane.GetRotation(cameraPlane);
        }
        private Vector3D GetRotatedDelta(double dx, double dy)
        {
            // Rotate the xy plane to the camera's orientation
            Quaternion quaternion = GetRotationForDelta();

            // Now return a the vector passed in, but rotated into camera coords
            Vector3D retVal = new Vector3D(dx, dy, 0d);
            retVal = quaternion.GetRotatedVector(retVal);

            return retVal;
        }

        #endregion
    }
}
