using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using Game.HelperClassesCore;
using Game.HelperClassesWPF.Controls3D;

namespace Game.HelperClassesWPF
{
    /// <summary>
    /// This trackball physically moves the camera
    /// </summary>
    /// <remarks>
    /// TODO:  Make a TrackBallTransform that doesn't change the camera's position/look/up, but manipulates
    /// a transform instead.  Put all common logic in TrackBallBase
    /// 
    /// TODO:  Support chasing something.  Add some stiffness params (that should be a different class, like chase camera or something)
    /// 
    /// TODO:  Support keyboard only (arrow keys, asdw)
    /// </remarks>
    public class TrackBallRoam
    {
        #region class: VelocityTracker_Log

        private class VelocityTracker_Log
        {
            #region Declaration Section

            private const int MAXCOUNT = 5000;
            private const int SAMPLEMILLISECONDS = 250;

            private List<(Vector3D delta, DateTime time)> _velocityHistory = new List<(Vector3D delta, DateTime time)>();
            private List<(AxisAngleRotation3D aarX, AxisAngleRotation3D aarY, DateTime time)> _angularVelocityHistory_InPlace = new List<(AxisAngleRotation3D aarX, AxisAngleRotation3D aarY, DateTime time)>();
            private List<(Quaternion delta, DateTime time)> _angularVelocityHistory_Oribit = new List<(Quaternion delta, DateTime time)>();

            #endregion

            #region Public Methods

            public void Clear()
            {
                _velocityHistory.Clear();
                _angularVelocityHistory_InPlace.Clear();
                _angularVelocityHistory_Oribit.Clear();
            }

            public void AddLinear(Vector3D delta)
            {
                _velocityHistory.Add((delta, DateTime.UtcNow));

                while (_velocityHistory.Count > MAXCOUNT)
                {
                    _velocityHistory.RemoveAt(0);
                }
            }
            public void AddAngular_InPlace(AxisAngleRotation3D aarX, AxisAngleRotation3D aarY)
            {
                _angularVelocityHistory_InPlace.Add((aarX, aarY, DateTime.UtcNow));

                while (_angularVelocityHistory_InPlace.Count > MAXCOUNT)
                {
                    _angularVelocityHistory_InPlace.RemoveAt(0);
                }
            }
            public void AddAngular_Orbit(Quaternion delta)
            {
                _angularVelocityHistory_Oribit.Add((delta, DateTime.UtcNow));

                while (_angularVelocityHistory_Oribit.Count > MAXCOUNT)
                {
                    _angularVelocityHistory_Oribit.RemoveAt(0);
                }
            }

            public ((Vector3D unit, double speed)? linear, (Vector3D axis, double speed, bool isInPlace)? angular) GetVelocity()
            {
                if (_velocityHistory.Count > 0)
                {
                    //DrawLinear();
                    return (GetLinear(), null);
                }
                else if (_angularVelocityHistory_Oribit.Count > 0)
                {
                    //DrawOrbit();
                    return (null, GetOrbit());
                }

                return (null, null);
            }

            #endregion

            #region Private Methods

            private void DrawLinear()
            {
                DateTime now = DateTime.UtcNow;

                double LINE = _velocityHistory.Max(o => o.delta.Length) * .03;

                Debug3DWindow window = new Debug3DWindow()
                {
                    Title = string.Format("{0} - {1} seconds", _velocityHistory.Count.ToString("N0"), (_velocityHistory[_velocityHistory.Count - 1].time - _velocityHistory[0].time).TotalSeconds.ToStringSignificantDigits(2)),
                };

                #region offset

                Vector3D offset;
                if (_velocityHistory.Count == 0)
                {
                    offset = new Vector3D(0, 0, 1);
                }
                else
                {
                    try
                    {
                        ITriangle plane = Math2D.GetPlane_Average(_velocityHistory.Select(o => o.delta.ToPoint()).ToArray());
                        offset = plane.Normal;
                    }
                    catch (Exception)
                    {
                        offset = new Vector3D(0, 0, 1);
                    }
                }

                offset = offset.ToUnit() * (LINE * 3);

                #endregion

                #region every 10 milliseconds

                Point3D currOffset = new Point3D();

                DateTime curNow = now;
                Color color = UtilityWPF.GetRandomColor(190, 240);

                for (int cntr = _velocityHistory.Count - 1; cntr >= 0; cntr--)
                {
                    var snapshot = _velocityHistory[cntr];

                    if ((curNow - snapshot.time).TotalMilliseconds > 10)
                    {
                        curNow = snapshot.time;
                        color = UtilityWPF.GetRandomColor(190, 240);
                    }

                    window.AddLine(currOffset, currOffset + snapshot.delta, LINE, color);
                    currOffset += offset;
                }

                #endregion

                #region sum per 50 milliseconds

                currOffset = new Point3D(LINE * 100, 0, 0);

                curNow = now;
                color = UtilityWPF.GetRandomColor(0, 128);

                Vector3D sum = new Vector3D();

                for (int cntr = _velocityHistory.Count - 1; cntr >= 0; cntr--)
                {
                    var snapshot = _velocityHistory[cntr];

                    if ((curNow - snapshot.time).TotalMilliseconds > 50)
                    {
                        window.AddLine(currOffset, currOffset + sum, LINE, color);
                        sum = new Vector3D();
                        currOffset += offset;

                        curNow = snapshot.time;
                        color = UtilityWPF.GetRandomColor(0, 128);
                    }

                    sum += snapshot.delta;
                }

                #endregion

                #region sum of last N milliseconds

                currOffset = new Point3D(LINE * 100, 0, 0) - offset;

                curNow = now;
                DateTime lastNow = now;
                color = UtilityWPF.GetRandomColor(0, 128);

                sum = new Vector3D();

                for (int cntr = _velocityHistory.Count - 1; cntr >= 0; cntr--)
                {
                    var snapshot = _velocityHistory[cntr];

                    if ((curNow - snapshot.time).TotalMilliseconds > SAMPLEMILLISECONDS)
                    {
                        break;
                    }

                    lastNow = snapshot.time;
                    sum += snapshot.delta;
                }

                window.AddLine(currOffset, currOffset + sum, LINE, Colors.White);

                #endregion

                window.Show();
            }
            private void DrawOrbit()
            {
                DateTime now = DateTime.UtcNow;

                Debug3DWindow window = new Debug3DWindow()
                {
                    Title = string.Format("{0} - {1} seconds", _angularVelocityHistory_Oribit.Count.ToString("N0"), (_angularVelocityHistory_Oribit[_angularVelocityHistory_Oribit.Count - 1].time - _angularVelocityHistory_Oribit[0].time).TotalSeconds.ToStringSignificantDigits(2)),
                };

                var axisAngles = _angularVelocityHistory_Oribit.
                    Select(o => new
                    {
                        axis = o.delta.Axis.ToUnit(),
                        angle = o.delta.Angle,
                        o.time,
                    }).
                    ToArray();

                double LINE = axisAngles.Max(o => o.angle) * .03;

                #region offset

                Vector3D offset;
                if (axisAngles.Length == 0)
                {
                    offset = new Vector3D(0, 0, 1);
                }
                else
                {
                    try
                    {
                        ITriangle plane = Math2D.GetPlane_Average(axisAngles.Select(o => o.axis.ToPoint()).ToArray());
                        offset = plane.Normal;
                    }
                    catch (Exception)
                    {
                        offset = new Vector3D(0, 0, 1);
                    }
                }

                offset = offset.ToUnit() * (LINE * 3);

                #endregion

                #region every 10 milliseconds

                Point3D currOffset = new Point3D();

                DateTime curNow = now;
                Color color = UtilityWPF.GetRandomColor(190, 240);

                for (int cntr = axisAngles.Length - 1; cntr >= 0; cntr--)
                {
                    var snapshot = axisAngles[cntr];

                    if ((curNow - snapshot.time).TotalMilliseconds > 10)
                    {
                        curNow = snapshot.time;
                        color = UtilityWPF.GetRandomColor(190, 240);
                    }

                    window.AddLine(currOffset, currOffset + (snapshot.axis * snapshot.angle), LINE, color);
                    currOffset += offset;
                }

                var avg_stddev = Math1D.Get_Average_StandardDeviation(axisAngles.Select(o => o.angle));
                var avgTime = Math1D.Get_Average_StandardDeviation(Enumerable.Range(0, axisAngles.Length - 1).Select(o => (axisAngles[o + 1].time - axisAngles[o].time).TotalSeconds));

                window.AddText($"avg individuals: {avg_stddev.Item1.ToStringSignificantDigits(3)}");
                window.AddText($"avg individuals scaled: {(avg_stddev.Item1 / avgTime.Item1).ToStringSignificantDigits(3)}");

                #endregion

                #region sum per 50 milliseconds

                currOffset = new Point3D(LINE * 100, 0, 0);

                curNow = now;
                color = UtilityWPF.GetRandomColor(0, 128);

                Vector3D sum = new Vector3D();

                List<double> fifties = new List<double>();

                for (int cntr = axisAngles.Length - 1; cntr >= 0; cntr--)
                {
                    var snapshot = axisAngles[cntr];

                    if ((curNow - snapshot.time).TotalMilliseconds > 50)
                    {
                        window.AddLine(currOffset, currOffset + sum, LINE, color);
                        sum = new Vector3D();
                        currOffset += offset;

                        fifties.Add(sum.Length);

                        curNow = snapshot.time;
                        color = UtilityWPF.GetRandomColor(0, 128);
                    }

                    sum += snapshot.axis * snapshot.angle;
                }

                avg_stddev = Math1D.Get_Average_StandardDeviation(fifties);
                window.AddText($"avg 50s: {avg_stddev.Item1.ToStringSignificantDigits(3)}");

                #endregion

                #region sum of last N milliseconds

                currOffset = new Point3D(LINE * 100, 0, 0) - offset;

                curNow = now;
                DateTime? lastNow = null;
                color = UtilityWPF.GetRandomColor(0, 128);

                sum = new Vector3D();

                for (int cntr = axisAngles.Length - 1; cntr >= 0; cntr--)
                {
                    var snapshot = axisAngles[cntr];

                    if ((curNow - snapshot.time).TotalMilliseconds > SAMPLEMILLISECONDS)
                    {
                        break;
                    }

                    lastNow = snapshot.time;
                    sum += snapshot.axis * snapshot.angle;
                }

                window.AddLine(currOffset, currOffset + sum, LINE, Colors.White);

                window.AddText($"final: {sum.Length.ToStringSignificantDigits(3)}");

                if (lastNow != null)
                {
                    double duration = (now - lastNow.Value).TotalSeconds;


                }

                #endregion

                window.Show();
            }

            private (Vector3D unit, double speed)? GetLinear()
            {
                DateTime now = DateTime.UtcNow;

                #region sum of last N milliseconds

                DateTime curNow = now;
                DateTime? lastNow = null;

                Vector3D sum = new Vector3D();

                for (int cntr = _velocityHistory.Count - 1; cntr >= 0; cntr--)
                {
                    var snapshot = _velocityHistory[cntr];

                    if ((curNow - snapshot.time).TotalMilliseconds > SAMPLEMILLISECONDS)
                    {
                        break;
                    }

                    lastNow = snapshot.time;
                    sum += snapshot.delta;
                }

                #endregion

                if (lastNow == null)
                {
                    return null;
                }

                // Speed will be per second.  Since only a quarter second is used, the final vector length will be larger than sum
                double duration = (now - lastNow.Value).TotalSeconds;

                return
                (
                    sum.ToUnit(),
                    sum.Length / duration
                );
            }
            private (Vector3D axis, double speed, bool isInPlace)? GetOrbit()
            {
                DateTime now = DateTime.UtcNow;

                #region sum of last N milliseconds

                DateTime curNow = now;
                DateTime? lastNow = null;

                Vector3D sum = new Vector3D();

                for (int cntr = _angularVelocityHistory_Oribit.Count - 1; cntr >= 0; cntr--)
                {
                    var snapshot = _angularVelocityHistory_Oribit[cntr];

                    if ((curNow - snapshot.time).TotalMilliseconds > SAMPLEMILLISECONDS)
                    {
                        break;
                    }

                    lastNow = snapshot.time;
                    sum += snapshot.delta.Axis.ToUnit() * snapshot.delta.Angle;
                }

                #endregion

                if (lastNow == null)
                {
                    return null;
                }

                // Speed will be per second.  Since only a quarter second is used, the final vector length will be larger than sum
                double duration = (now - lastNow.Value).TotalSeconds;

                return
                (
                    sum.ToUnit(),
                    sum.Length / duration,
                    false
                );
            }

            #endregion
        }

        #endregion
        #region class: VelocityTracker_Inertia

        private class VelocityTracker_Inertia
        {
            public VelocityTracker_Inertia(VelocityTracker_Inertia_Linear linear)
            {
                InertiaLastTick = DateTime.UtcNow;
                Linear = linear;
                Angular = null;
            }
            public VelocityTracker_Inertia(VelocityTracker_Inertia_Angular angular)
            {
                InertiaLastTick = DateTime.UtcNow;
                Angular = angular;
                Linear = null;
            }

            public DateTime InertiaLastTick { get; set; }

            // Only one of these should be populated
            public readonly VelocityTracker_Inertia_Linear Linear;
            public readonly VelocityTracker_Inertia_Angular Angular;
        }

        #endregion
        #region class: VelocityTracker_Inertia_Linear

        private class VelocityTracker_Inertia_Linear
        {
            public Vector3D VelocityUnit { get; set; }
            public double Speed { get; set; }
        }

        #endregion
        #region class: VelocityTracker_Inertia_Angular

        private class VelocityTracker_Inertia_Angular
        {
            public Vector3D Axis { get; set; }
            public double Speed { get; set; }

            public bool IsInPlace { get; set; }

            public double? OrbitRadius = null;
        }

        #endregion

        #region Events

        /// <summary>
        /// This gets raised when the user initiates an orbit rotation.  The listener would want to cast a ray
        /// to see if there is an object being looked at, and if so, return the distance to it.
        /// </summary>
        public event EventHandler<GetOrbitRadiusArgs> GetOrbitRadius = null;

        //NOTE: You could also just listen to Camera.Changed (but this custom event gives more info)
        public event EventHandler<UserMovedCameraArgs> UserMovedCamera = null;

        #endregion

        #region Declaration Section

        private FrameworkElement _eventSource;
        private Point _previousPosition2D;
        private Vector3D _previousPosition3D = new Vector3D(0, 0, 1);

        private PerspectiveCamera _camera = null;

        private AutoScrollEmulator _autoscroll = null;
        private CameraMovement? _currentAutoscrollAction = null;

        private DispatcherTimer _inertiaTimer = null;
        private double? _inertiaPercentRetainPerSecond_Linear = .03;
        private double? _inertiaPercentRetainPerSecond_Angular = .03;

        private VelocityTracker_Log _velocityLog = new VelocityTracker_Log();
        private VelocityTracker_Inertia _velocityInertia = null;

        private List<CameraMovement> _currentKeyboardActions = new List<CameraMovement>();
        private DispatcherTimer _timerKeyboard = null;
        /// <summary>
        /// The time of the last occurance of KeyboardTimer_Tick.  This is used to know how much actual time has elapsed between
        /// ticks, which is used to keep the scroll distance output normalized to time instead of ticks (in cases with low FPS)
        /// </summary>
        private DateTime _lastKeyboardTick = DateTime.UtcNow;

        private List<TrackBallMapping> _mappings = new List<TrackBallMapping>();

        /// <summary>
        /// This is used when they are orbiting the camera around a point (it gets set back to null in the mouse up)
        /// </summary>
        private double? _orbitRadius = null;

        private double _mouseWheelScale = .01;
        private double _panScale = .015;
        private double _zoomScale = .15;
        private double _rotateScale = .1;
        private double _keyPanScale = 5;		// this is per second

        private bool _allowZoomOnMouseWheel = true;

        private bool _shouldHitTestOnOrbit = false;

        private bool _isActive = true;

        #endregion

        #region Constructor

        public TrackBallRoam(PerspectiveCamera camera)
        {
            _camera = camera;

            _autoscroll = new AutoScrollEmulator();
            _autoscroll.AutoScroll += new AutoScrollHandler(Autoscroll_AutoScroll);
            _autoscroll.CursorChanged += new EventHandler(Autoscroll_CursorChanged);
        }

        #endregion

        #region Public Properties

        public bool IsActive
        {
            get
            {
                return _isActive;
            }
            set
            {
                _isActive = value;

                if (!_isActive)
                {
                    // This is a copy of what's in the MouseUp
                    Mouse.Capture(_eventSource, CaptureMode.None);
                    _orbitRadius = null;

                    if (_currentAutoscrollAction != null)
                    {
                        _autoscroll.StopAutoScroll();
                        _currentAutoscrollAction = null;
                    }
                }
            }
        }

        /// <summary>
        /// The FrameworkElement that is listened to for mouse events.
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
                    _eventSource.PreviewKeyDown -= this.EventSource_PreviewKeyDown;
                    _eventSource.PreviewKeyUp -= this.EventSource_PreviewKeyUp;
                    _eventSource.LostKeyboardFocus -= EventSource_LostKeyboardFocus;
                }

                _eventSource = value;

                if (_eventSource != null)
                {
                    _eventSource.MouseWheel += this.EventSource_MouseWheel;
                    _eventSource.MouseDown += this.EventSource_MouseDown;
                    _eventSource.MouseUp += this.EventSource_MouseUp;
                    _eventSource.MouseMove += this.EventSource_MouseMove;

                    _eventSource.Focusable = true;		// if these two aren't set, the control won't get keyboard events
                    _eventSource.Focus();
                    _eventSource.PreviewKeyDown += this.EventSource_PreviewKeyDown;
                    _eventSource.PreviewKeyUp += this.EventSource_PreviewKeyUp;
                    _eventSource.LostKeyboardFocus += EventSource_LostKeyboardFocus;
                }
            }
        }

        // Define what mouse/keyboard events do what
        public bool AllowZoomOnMouseWheel
        {
            get
            {
                return _allowZoomOnMouseWheel;
            }
            set
            {
                _allowZoomOnMouseWheel = value;
            }
        }
        public List<TrackBallMapping> Mappings
        {
            get
            {
                return _mappings;
            }
        }

        /// <summary>
        /// Default is .01
        /// </summary>
        public double MouseWheelScale
        {
            get
            {
                return _mouseWheelScale;
            }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException("MouseWheelScale must be greater than zero: " + value.ToString());
                }

                _mouseWheelScale = value;
            }
        }
        /// <summary>
        /// Default is .015
        /// </summary>
        public double PanScale
        {
            get
            {
                return _panScale;
            }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException("PanScale must be greater than zero: " + value.ToString());
                }

                _panScale = value;
            }
        }
        /// <summary>
        /// Default is .15
        /// </summary>
        public double ZoomScale
        {
            get
            {
                return _zoomScale;
            }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException("ZoomScale must be greater than zero: " + value.ToString());
                }

                _zoomScale = value;
            }
        }
        /// <summary>
        /// Default is .1
        /// </summary>
        public double RotateScale
        {
            get
            {
                return _rotateScale;
            }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException("RotateScale must be greater than zero: " + value.ToString());
                }

                _rotateScale = value;
            }
        }
        public double KeyPanScale
        {
            get
            {
                return _keyPanScale;
            }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException("KeyPanScale must be greater than zero: " + value.ToString());
                }

                _keyPanScale = value;
            }
        }

        public bool ShouldHitTestOnOrbit
        {
            get
            {
                return _shouldHitTestOnOrbit;
            }
            set
            {
                _shouldHitTestOnOrbit = value;
            }
        }

        /// <summary>
        /// If non null, this will cause the camera to keep moving after they release the mouse
        /// </summary>
        /// <remarks>
        /// Percent of 0 means it will be stopped after 1 second, no matter how fast it was going
        /// Percent of 1 means it will stay the same speed forever
        /// 
        /// .03 is a pretty good setting
        /// </remarks>
        public double? InertiaPercentRetainPerSecond_Linear
        {
            get
            {
                return _inertiaPercentRetainPerSecond_Linear;
            }
            set
            {
                _inertiaPercentRetainPerSecond_Linear = value;
            }
        }
        public double? InertiaPercentRetainPerSecond_Angular
        {
            get
            {
                return _inertiaPercentRetainPerSecond_Angular;
            }
            set
            {
                _inertiaPercentRetainPerSecond_Angular = value;
            }
        }

        #endregion

        #region Public Methods

        public string GetMappingReport()
        {
            return TrackBallMapping.GetReport(_mappings, _allowZoomOnMouseWheel);
        }

        #endregion
        #region Protected Methods

        protected virtual double OnGetOrbitRadius()
        {
            //TODO:  Don't just get the radius, get a live point to follow

            // Raise an event, requesting the radius
            if (GetOrbitRadius != null)
            {
                GetOrbitRadiusArgs args = new GetOrbitRadiusArgs(_camera.Position, _camera.LookDirection);
                GetOrbitRadius(this, args);

                if (args.Result != null)
                {
                    // They set the value, return it
                    return args.Result.Value;
                }
            }

            // If execution gets here, then the event never returned anything.  Figure out the radius myself

            if (!_shouldHitTestOnOrbit)
            {
                // This works great when looking at the origin, but is really weird when offset from the orign
                return _camera.Position.ToVector().Length;
            }

            #region ray cast

            // If there are no hits when fired from the dead center, fire off some more rays a random offset from the center
            //TODO: See if there is a more efficient way of getting all 3D objects on screen

            double width = _eventSource.ActualWidth;
            double height = _eventSource.ActualHeight;
            double halfWidth = width * .5d;
            double halfHeight = height * .5d;

            for (int cntr = 0; cntr < 50; cntr++)
            {
                Vector3D rayVector = new Vector3D(0, 0, 0);
                if (cntr > 0)		// the first attempt will be the center of the viewport
                {
                    rayVector = Math3D.GetRandomVector_Circular(.25d);
                }

                double? distance = OrbitGetObjectDistance(_eventSource, new Point(halfWidth + (rayVector.X * width), halfHeight + (rayVector.Y * height)));
                if (distance != null && (distance.Value * distance.Value) < _camera.Position.ToVector().LengthSquared)		// if it's greater than the distance to the origin, then I want to ignore it
                {
                    return distance.Value;
                }
            }

            #endregion

            return _camera.Position.ToVector().Length;

            #region WRONG IDEA

            //Vector3D position = _camera.Position.ToVector();

            //if (Vector3D.DotProduct(_camera.LookDirection, position) >= 0)
            //{
            //    // They are looking away from the origin, so just use the distance to the origin
            //    return position.Length;
            //}
            //else
            //{
            //    // Get a ray from the origin that intersects the look ray at 90 degrees

            //    // This first orthogonal is a reference
            //    Vector3D orth1 = Vector3D.CrossProduct(_camera.LookDirection, position);

            //    // This second orthogonal is orthogonal to the look direction, but in the plane of the pos/look dir
            //    Vector3D orth2 = Vector3D.CrossProduct(orth1, _camera.LookDirection);

            //    // Now I need to intersect look direction with the line coming from the origin in the direction of the second orthogonal
            //    Point3D? intersectPoint = Math3D.GetIntersectionOfTwoLines(new Point3D(0, 0, 0), orth2, _camera.Position, _camera.LookDirection);
            //    if (intersectPoint == null)
            //    {
            //        // This should never happen?  Only if math drift is too much?
            //        return position.Length;
            //    }
            //    else
            //    {
            //        double retVal = (_camera.Position - intersectPoint.Value).Length;
            //        double distToOrigin = position.Length;

            //        return Math.Min(retVal, distToOrigin);		// I can't just blindly return the calculated distance, because they could be looking nearly parallel to the line to the origin.  In that case the distance returned would be huge
            //    }
            //}

            #endregion
        }

        protected virtual void OnUserMovedCamera(UserMovedCameraArgs e)
        {
            UserMovedCamera?.Invoke(this, e);
        }

        #endregion

        #region Event Listeners

        private void EventSource_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!_isActive || !_allowZoomOnMouseWheel)
            {
                return;
            }

            _orbitRadius = null;

            Vector3D delta = _camera.LookDirection;
            delta.Normalize();
            delta = delta * (e.Delta * _mouseWheelScale);

            _camera.Position = _camera.Position + delta;

            OnUserMovedCamera(new UserMovedCameraArgs(CameraMovement.Zoom));
        }

        private void EventSource_MouseDown(object sender, MouseEventArgs e)
        {
            if (!_isActive)
            {
                return;
            }

            _inertiaTimer?.Stop();
            _velocityInertia = null;
            _velocityLog.Clear();

            _orbitRadius = null;

            // By capturing the mouse, mouse events will still come in even when they are moving the mouse
            // outside the element/form
            Mouse.Capture(_eventSource, CaptureMode.SubTree);		// I had a case where I used the grid as the event source.  If they clicked one of the 3D objects, the scene would jerk.  But by saying subtree, I still get the event

            _previousPosition2D = e.GetPosition(_eventSource);
            _previousPosition3D = ProjectToTrackball(_eventSource.ActualWidth, _eventSource.ActualHeight, _previousPosition2D);

            #region Detect Autoscroll

            if (_currentAutoscrollAction == null)
            {
                CameraMovement? action = GetAction(e);
                if (action != null && IsAutoScroll(action.Value))
                {
                    _autoscroll.StartAutoScroll(_previousPosition2D, UtilityWPF.TransformToScreen(_previousPosition2D, _eventSource));
                    _currentAutoscrollAction = action;
                }
            }

            #endregion
        }
        private void EventSource_MouseUp(object sender, MouseEventArgs e)
        {
            if (!_isActive)
            {
                return;
            }

            Mouse.Capture(_eventSource, CaptureMode.None);

            StartMomentum();

            //_orbitRadius = null;

            #region Detect Autoscroll

            if (_currentAutoscrollAction != null)
            {
                // Autoscroll is currently running.  See if it should be turned off
                if (GetAction(e) != _currentAutoscrollAction)
                {
                    _autoscroll.StopAutoScroll();
                    _currentAutoscrollAction = null;
                }
            }

            #endregion
        }

        private void EventSource_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isActive)
            {
                return;
            }

            Point currentPosition = e.GetPosition(_eventSource);

            // Avoid any zero axis conditions
            if (currentPosition == _previousPosition2D)
            {
                return;
            }

            // Project the 2D position onto a sphere
            Vector3D currentPosition3D = ProjectToTrackball(_eventSource.ActualWidth, _eventSource.ActualHeight, currentPosition);

            if (_currentAutoscrollAction != null)
            {
                // They are doing an autoscroll, let the autoscroll class's timer fire events
                _autoscroll.MouseMove(currentPosition);
            }
            else
            {
                // See if an action occured
                CameraMovement? action = GetAction(e);

                if (action != null && !IsAutoScroll(action.Value))		// if it's an autoscroll action, then they missed their chance.  They will have to release the mouse, and click again (this would only happen if they were in the middle of doing some other action, and pressed even more buttons to change actions)
                {
                    #region perform action

                    switch (action.Value)
                    {
                        case CameraMovement.Orbit:
                            OrbitCamera(currentPosition, currentPosition3D);
                            break;

                        case CameraMovement.Pan:
                            PanCamera(currentPosition);
                            break;

                        case CameraMovement.RotateAroundLookDirection:
                            RotateCameraAroundLookDir(currentPosition, currentPosition3D);
                            break;

                        case CameraMovement.RotateInPlace:
                            RotateCamera(currentPosition);
                            break;

                        case CameraMovement.Zoom:
                            ZoomCamera(currentPosition);
                            break;

                        default:
                            throw new ApplicationException("Unexpected CameraMovement: " + action.Value);
                    }

                    OnUserMovedCamera(new UserMovedCameraArgs(action.Value));

                    #endregion
                }
            }

            _previousPosition2D = currentPosition;
            _previousPosition3D = currentPosition3D;
        }

        private void Autoscroll_AutoScroll(object sender, AutoScrollArgs e)
        {
            const double SCALE = .5d;

            if (!_isActive || _currentAutoscrollAction == null)
            {
                return;
            }

            double scale = SCALE;
            if (_currentAutoscrollAction.Value == CameraMovement.Pan_AutoScroll)
            {
                // Pan autoscroll is reverse direction, this has just been the convention forever, so is intuitive.  But the
                // other actions feel more like automated versions of the standard action, so shouldn't be reversed.
                scale *= -1d;
            }

            // The autoscroll class calculates delta by comparing the current position (passed during mouse move)
            // to the position at mouse down.  All of these action methods compare what they think is the current
            // mouse position with the previous mouse position.  So I need to create a false point to hand to these
            // action methods.
            Point currentPosition = new Point(_previousPosition2D.X + (e.XDelta * scale), _previousPosition2D.Y + (e.YDelta * scale));

            // Do the action
            switch (_currentAutoscrollAction.Value)
            {
                case CameraMovement.Orbit_AutoScroll:
                    Vector3D currentPosition3Da = ProjectToTrackball(_eventSource.ActualWidth, _eventSource.ActualHeight, currentPosition);
                    OrbitCamera(currentPosition, currentPosition3Da);
                    break;

                case CameraMovement.Pan_AutoScroll:
                    PanCamera(currentPosition);
                    break;

                case CameraMovement.RotateAroundLookDirection_AutoScroll:
                    Vector3D currentPosition3Db = ProjectToTrackball(_eventSource.ActualWidth, _eventSource.ActualHeight, currentPosition);
                    RotateCameraAroundLookDir(currentPosition, currentPosition3Db);
                    break;

                case CameraMovement.RotateInPlace_AutoScroll:
                    RotateCamera(currentPosition);
                    break;

                case CameraMovement.Zoom_AutoScroll:
                    ZoomCamera(currentPosition);
                    break;

                default:
                    throw new ApplicationException("Unknown CameraMovement: " + _currentAutoscrollAction.Value.ToString());
            }

            OnUserMovedCamera(new UserMovedCameraArgs(_currentAutoscrollAction.Value));
        }
        private void Autoscroll_CursorChanged(object sender, EventArgs e)
        {
            _eventSource.Cursor = _autoscroll.SuggestedCursor;
        }

        private void InertiaTimer_Tick(object sender, EventArgs e)
        {
            const double MINSPEED = .001;

            if (_velocityInertia == null)
            {
                _inertiaTimer.Stop();
                return;
            }

            DateTime now = DateTime.UtcNow;
            double elapsed = (now - _velocityInertia.InertiaLastTick).TotalSeconds;

            if (_velocityInertia.Linear != null)
            {
                #region linear

                if (_inertiaPercentRetainPerSecond_Linear == null)
                {
                    _inertiaTimer.Stop();
                    return;
                }

                // Decay
                // Vt = Vo * (1/R)^-t
                _velocityInertia.Linear.Speed = _velocityInertia.Linear.Speed * (Math.Pow((1 / _inertiaPercentRetainPerSecond_Linear.Value), -elapsed));

                if (_velocityInertia.Linear.Speed < MINSPEED)
                {
                    _inertiaTimer.Stop();
                    return;
                }

                double displacement = _velocityInertia.Linear.Speed * elapsed;

                PanCamera(_velocityInertia.Linear.VelocityUnit * displacement);

                _velocityInertia.InertiaLastTick = now;

                #endregion
            }
            else if (_velocityInertia.Angular != null)
            {
                #region angular

                if (_velocityInertia.Angular.Axis.Length == 0 || _velocityInertia.Angular.OrbitRadius == null || _inertiaPercentRetainPerSecond_Angular == null)
                {
                    _inertiaTimer.Stop();
                    return;
                }

                _velocityInertia.Angular.Speed = _velocityInertia.Angular.Speed * (Math.Pow((1 / _inertiaPercentRetainPerSecond_Angular.Value), -elapsed));

                if (_velocityInertia.Angular.Speed < MINSPEED)
                {
                    _inertiaTimer.Stop();
                    return;
                }

                double displacement = _velocityInertia.Angular.Speed * elapsed;

                if (_velocityInertia.Angular.IsInPlace)
                {

                }
                else
                {
                    OrbitCamera(new Quaternion(_velocityInertia.Angular.Axis, displacement));
                }

                _velocityInertia.InertiaLastTick = now;

                #endregion
            }
            else
            {
                _inertiaTimer.Stop();
            }
        }

        private void EventSource_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.IsRepeat)
            {
                // If they hold in the key, this event keeps firing
                return;
            }

            _inertiaTimer?.Stop();
            _velocityInertia = null;
            _velocityLog.Clear();

            switch (e.Key)
            {
                case Key.LeftAlt:
                case Key.LeftCtrl:
                case Key.LeftShift:
                case Key.RightAlt:
                case Key.RightCtrl:
                case Key.RightShift:
                    // Ignore this (this is a modification key, not a primary driver for camera movement)
                    return;
            }

            // See if an action occured
            CameraMovement? action = GetAction(e);
            if (action == null)
            {
                return;
            }

            // Add this to the list of active actions
            if (!_currentKeyboardActions.Contains(action.Value))
            {
                _currentKeyboardActions.Add(action.Value);
            }

            // Set up the timer if this is the first time it's needed
            if (_timerKeyboard == null)
            {
                _timerKeyboard = new DispatcherTimer();
                _timerKeyboard.Interval = TimeSpan.FromMilliseconds(25);
                _timerKeyboard.Tick += new EventHandler(KeyboardTimer_Tick);
            }

            // Make sure the timer is running
            if (!_timerKeyboard.IsEnabled)
            {
                _lastKeyboardTick = DateTime.UtcNow;
                _timerKeyboard.Start();
            }
        }
        private void EventSource_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (_currentKeyboardActions.Count == 0)
            {
                // Nothing is currently firing
                return;
            }

            CameraMovement? action = GetAction(e);
            if (action == null)
            {
                return;
            }

            // Find and kill any actions that this equals (there should never be more than one, but I want to be safe)
            int index = 0;
            while (index < _currentKeyboardActions.Count)
            {
                if (_currentKeyboardActions[index] == action.Value)
                {
                    _currentKeyboardActions.RemoveAt(index);
                }
                else
                {
                    index++;
                }
            }

            // If there's nothing left, then turn off the timer
            if (_currentKeyboardActions.Count == 0)
            {
                _timerKeyboard.Stop();
            }
        }
        private void EventSource_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            // Make sure to stop any keyboard actions (the mouse down captures the mouse, but the keyboard doesn't, so I need this extra check)
            _currentKeyboardActions.Clear();
            if (_timerKeyboard != null)
            {
                _timerKeyboard.Stop();
            }
        }
        private void KeyboardTimer_Tick(object sender, EventArgs e)
        {
            // Account for slow machines
            DateTime now = DateTime.UtcNow;
            double elapsedTime = (now - _lastKeyboardTick).TotalMilliseconds;
            _lastKeyboardTick = now;

            foreach (CameraMovement movement in _currentKeyboardActions)
            {
                switch (movement)
                {
                    case CameraMovement.Pan_Keyboard_In:
                        ZoomCamera(new Point(_previousPosition2D.X, _previousPosition2D.Y - GetKeyboardDelta(_keyPanScale, _zoomScale, elapsedTime)));		// it's subtract instead of add, because it's the camera that's moving
                        break;

                    case CameraMovement.Pan_Keyboard_Out:
                        ZoomCamera(new Point(_previousPosition2D.X, _previousPosition2D.Y + GetKeyboardDelta(_keyPanScale, _zoomScale, elapsedTime)));
                        break;

                    case CameraMovement.Pan_Keyboard_Up:
                        PanCamera(new Point(_previousPosition2D.X, _previousPosition2D.Y - GetKeyboardDelta(_keyPanScale, _panScale, elapsedTime)));
                        break;

                    case CameraMovement.Pan_Keyboard_Down:
                        PanCamera(new Point(_previousPosition2D.X, _previousPosition2D.Y + GetKeyboardDelta(_keyPanScale, _panScale, elapsedTime)));
                        break;

                    case CameraMovement.Pan_Keyboard_Left:
                        PanCamera(new Point(_previousPosition2D.X + GetKeyboardDelta(_keyPanScale, _panScale, elapsedTime), _previousPosition2D.Y));
                        break;

                    case CameraMovement.Pan_Keyboard_Right:
                        PanCamera(new Point(_previousPosition2D.X - GetKeyboardDelta(_keyPanScale, _panScale, elapsedTime), _previousPosition2D.Y));
                        break;

                    default:
                        throw new ApplicationException("Unexpected CameraMovement: " + movement);
                }

                OnUserMovedCamera(new UserMovedCameraArgs(movement));
            }
        }

        #endregion

        #region Private Methods - do movement

        private void PanCamera(Point currentPosition)
        {
            Vector3D camZ = _camera.LookDirection.ToUnit();
            Vector3D camX = -Vector3D.CrossProduct(camZ, _camera.UpDirection).ToUnit();
            Vector3D camY = Vector3D.CrossProduct(camZ, camX).ToUnit();

            double dX = currentPosition.X - _previousPosition2D.X;
            double dY = currentPosition.Y - _previousPosition2D.Y;

            dX *= _panScale;
            dY *= _panScale;

            Vector3D vPan = (camX * dX) + (camY * dY);

            //Point3D cameraPos = _camera.Position;
            //cameraPos += vPan;
            //_camera.Position = cameraPos;
            _camera.Position += vPan;

            _velocityLog.AddLinear(vPan);
        }
        private void PanCamera(Vector3D delta)
        {
            _camera.Position += delta;
        }

        private void ZoomCamera(Point currentPosition)
        {
            double yDelta = currentPosition.Y - _previousPosition2D.Y;

            yDelta *= _zoomScale;

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

            Vector3D[] vectors = new Vector3D[] { _camera.UpDirection, _camera.LookDirection };

            deltaRotation.GetRotatedVector(vectors);

            // Apply the changes
            _camera.UpDirection = vectors[0];
            _camera.LookDirection = vectors[1];

            // This is so specific and rarely used, don't bother giving it momentum
        }

        private void RotateCamera(Point currentPosition)
        {
            Vector3D camZ = _camera.LookDirection.ToUnit();
            Vector3D camX = -Vector3D.CrossProduct(camZ, _camera.UpDirection).ToUnit();
            Vector3D camY = Vector3D.CrossProduct(camZ, camX).ToUnit();

            double dX = currentPosition.X - _previousPosition2D.X;
            double dY = currentPosition.Y - _previousPosition2D.Y;

            dX *= _rotateScale;
            dY *= -_rotateScale;

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

            _velocityLog.AddAngular_InPlace(aarX, aarY);
        }

        private void OrbitCamera(Point currentPosition, Vector3D currentPosition3D)
        {
            #region get mouse movement - spherical

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
            //if (_orbitRadius == null)
            //{
            //    _orbitRadius = OnGetOrbitRadius();
            //}

            #region DELETE
            //// Figure out the offset in world coords
            //Vector3D lookLine = _camera.LookDirection.ToUnit();
            //lookLine = lookLine * _orbitRadius.Value;

            //Point3D orbitPointWorld = _camera.Position + lookLine;

            //// Get the opposite of the look line (the line from the orbit center to the camera's position)
            //Vector3D lookLineOpposite = lookLine * -1d;

            //// Rotate
            //Vector3D[] vectors = new Vector3D[] { lookLineOpposite, _camera.UpDirection, _camera.LookDirection };

            //deltaRotation.GetRotatedVector(vectors);

            //// Apply the changes
            //_camera.Position = orbitPointWorld + vectors[0];
            //_camera.UpDirection = vectors[1];
            //_camera.LookDirection = vectors[2];
            #endregion

            OrbitCamera(deltaRotation);

            _velocityLog.AddAngular_Orbit(deltaRotation);
        }
        private void OrbitCamera(Quaternion deltaRotation)
        {
            // This can't be calculated each mose move.  It causes a wobble when the look direction isn't pointed directly at the origin
            if (_orbitRadius == null)
            {
                _orbitRadius = OnGetOrbitRadius();
            }

            // Figure out the offset in world coords
            Vector3D lookLine = _camera.LookDirection.ToUnit();
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
        #region Private Methods

        private void StartMomentum()
        {
            if (_inertiaPercentRetainPerSecond_Linear == null && _inertiaPercentRetainPerSecond_Angular == null)
            {
                return;
            }

            var (linear, angular) = _velocityLog.GetVelocity();
            _velocityLog.Clear();

            if (linear != null && _inertiaPercentRetainPerSecond_Linear != null)
            {
                _velocityInertia = new VelocityTracker_Inertia(new VelocityTracker_Inertia_Linear()
                {
                    VelocityUnit = linear.Value.unit,
                    Speed = linear.Value.speed,
                });
            }
            else if (angular != null && _inertiaPercentRetainPerSecond_Angular != null)
            {
                _velocityInertia = new VelocityTracker_Inertia(new VelocityTracker_Inertia_Angular()
                {
                    Axis = angular.Value.axis,
                    Speed = angular.Value.speed,
                    IsInPlace = angular.Value.isInPlace,
                    OrbitRadius = _orbitRadius,
                });
            }
            else
            {
                return;
            }

            if (_inertiaTimer == null)
            {
                _inertiaTimer = new DispatcherTimer();
                _inertiaTimer.Interval = TimeSpan.FromMilliseconds(20);
                _inertiaTimer.Tick += InertiaTimer_Tick;
            }

            _inertiaTimer.Start();
        }

        /// <summary>
        /// This figures out which action to perform
        /// </summary>
        private CameraMovement? GetAction(MouseEventArgs e)
        {
            foreach (TrackBallMapping mapping in _mappings)
            {
                if (mapping.IsMatch(e))
                {
                    return mapping.Movement;
                }
            }

            return null;
        }
        /// <summary>
        /// This figures out which action to perform
        /// </summary>
        private CameraMovement? GetAction(KeyEventArgs e)
        {
            foreach (TrackBallMapping mapping in _mappings)
            {
                if (mapping.IsMatch(e))
                {
                    return mapping.Movement;
                }
            }

            return null;
        }

        private static bool IsAutoScroll(CameraMovement action)
        {
            switch (action)
            {
                case CameraMovement.Orbit_AutoScroll:
                case CameraMovement.Pan_AutoScroll:
                case CameraMovement.RotateAroundLookDirection_AutoScroll:
                case CameraMovement.RotateInPlace_AutoScroll:
                case CameraMovement.Zoom_AutoScroll:
                    return true;

                default:
                    return false;
            }
        }

        private static Vector3D ProjectToTrackball(double width, double height, Point point)
        {
            bool shouldInvertZ = false;

            // Scale the inputs so -1 to 1 is the edge of the screen
            double x = point.X / (width / 2d);    // Scale so bounds map to [0,0] - [2,2]
            double y = point.Y / (height / 2d);

            x = x - 1d;                           // Translate 0,0 to the center
            y = 1d - y;                           // Flip so +Y is up instead of down

            // Wrap (otherwise, everything greater than 1 will map to the permiter of the sphere where z = 0)
            bool localInvert;
            x = ProjectToTrackball_Wrap(out localInvert, x);
            shouldInvertZ |= localInvert;

            y = ProjectToTrackball_Wrap(out localInvert, y);
            shouldInvertZ |= localInvert;

            // Project onto a sphere
            double z2 = 1d - (x * x) - (y * y);       // z^2 = 1 - x^2 - y^2
            double z = 0d;
            if (z2 > 0d)
            {
                z = Math.Sqrt(z2);
            }
            else
            {
                // NOTE:  The wrap logic above should make it so this never happens
                z = 0d;
            }

            if (shouldInvertZ)
            {
                z *= -1d;
            }

            // Exit Function
            return new Vector3D(x, y, z);
        }
        /// <summary>
        /// This wraps the value so it stays between -1 and 1
        /// </summary>
        private static double ProjectToTrackball_Wrap(out bool shouldInvertZ, double value)
        {
            // Everything starts over at 4 (4 becomes zero)
            double retVal = value % 4d;

            double absX = Math.Abs(retVal);
            bool isNegX = retVal < 0d;

            shouldInvertZ = false;

            if (absX >= 3d)
            {
                // Anything from 3 to 4 needs to be -1 to 0
                // Anything from -4 to -3 needs to be 0 to 1
                retVal = 4d - absX;

                if (!isNegX)
                {
                    retVal *= -1d;
                }
            }
            else if (absX > 1d)
            {
                // This is the back side of the sphere
                // Anything from 1 to 3 needs to be flipped (1 stays 1, 2 becomes 0, 3 becomes -1)
                // -1 stays -1, -2 becomes 0, -3 becomes 1
                retVal = 2d - absX;

                if (isNegX)
                {
                    retVal *= -1d;
                }

                shouldInvertZ = true;
            }

            // Exit Function
            return retVal;
        }

        private static Vector3D ProjectToTrackball_ORIG(double width, double height, Point point)
        {
            double x = point.X / (width / 2);    // Scale so bounds map to [0,0] - [2,2]
            double y = point.Y / (height / 2);

            x = x - 1;                           // Translate 0,0 to the center
            y = 1 - y;                           // Flip so +Y is up instead of down

            double z2 = 1 - x * x - y * y;       // z^2 = 1 - x^2 - y^2
            double z = z2 > 0 ? Math.Sqrt(z2) : 0;

            return new Vector3D(x, y, z);
        }

        private static double GetKeyboardDelta(double keyScale, double mouseScale, double elapsedMilliseconds)
        {
            return (keyScale * elapsedMilliseconds * .001d) / mouseScale;
        }

        /// <summary>
        /// When they start a camera orbit, this does a ray cast to see if there is an object.  If one is found, it returns the
        /// distance to that object (this will be the radius of the camera orbit)
        /// </summary>
        /// <param name="point">This is a point from 0,0 to eventSource.ActualWidth,eventSource.ActualHeight</param>
        private static double? OrbitGetObjectDistance(FrameworkElement eventSource, Point point)
        {
            double? retVal = null;

            // This gets called every time there is a hit
            HitTestResultCallback resultCallback = delegate (HitTestResult result)
            {
                if (result is RayHitTestResult)
                {
                    RayHitTestResult resultCast = (RayHitTestResult)result;

                    // Only consider the hit if it has a non transparent brush
                    if (resultCast.ModelHit is GeometryModel3D)
                    {
                        GeometryModel3D geometry = (GeometryModel3D)resultCast.ModelHit;

                        if (!OrbitGetObjectDistance_IsTransparent(geometry))
                        {
                            retVal = resultCast.DistanceToRayOrigin;
                            return HitTestResultBehavior.Stop;
                        }
                    }
                }

                return HitTestResultBehavior.Continue;
            };

            // Get hits against existing models (and the anonymous method above gets the callbacks)
            VisualTreeHelper.HitTest(eventSource, null, resultCallback, new PointHitTestParameters(point));//new Point(eventSource.ActualWidth / 2d, eventSource.ActualHeight / 2d)));

            return retVal;
        }
        private static bool OrbitGetObjectDistance_IsTransparent(GeometryModel3D geometry)
        {
            foreach (Material material in new Material[] { geometry.BackMaterial, geometry.Material })
            {
                if (material == null)
                {
                    continue;
                }

                if (!OrbitGetObjectDistance_IsTransparent_Material(material))
                {
                    return false;
                }
            }

            return true;
        }
        private static bool OrbitGetObjectDistance_IsTransparent_Material(Material material)
        {
            if (material is MaterialGroup)
            {
                #region MaterialGroup

                // Recurse
                foreach (Material childMaterial in ((MaterialGroup)material).Children)
                {
                    if (!OrbitGetObjectDistance_IsTransparent_Material(childMaterial))
                    {
                        return false;
                    }
                }

                #endregion
            }
            else if (material is DiffuseMaterial)
            {
                #region DiffuseMaterial

                DiffuseMaterial materialCast1 = (DiffuseMaterial)material;

                if (materialCast1.Brush == null)
                {
                    if (!UtilityWPF.IsTransparent(materialCast1.AmbientColor))		// this was all white when the brush was set.  Not sure what that means
                    {
                        return false;
                    }
                }
                else
                {
                    if (!UtilityWPF.IsTransparent(materialCast1.Brush))
                    {
                        return false;
                    }
                }

                // I don't think I want to include with this one
                //if (!UtilityWPF.IsTransparent(materialCast1.Color))
                //{
                //    return true;
                //}

                #endregion
            }
            else if (material is SpecularMaterial)
            {
                #region SpecularMaterial

                SpecularMaterial materialCast2 = (SpecularMaterial)material;

                if (!UtilityWPF.IsTransparent(materialCast2.Brush))
                {
                    return false;
                }

                #endregion
            }
            else if (material is EmissiveMaterial)
            {
                #region EmissiveMaterial

                EmissiveMaterial materialCast3 = (EmissiveMaterial)material;

                if (materialCast3.Brush == null)
                {
                    if (!UtilityWPF.IsTransparent(materialCast3.Color))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!UtilityWPF.IsTransparent(materialCast3.Brush))
                    {
                        return false;
                    }
                }

                #endregion
            }

            // It is transparent
            return true;
        }

        #endregion
    }

    #region class: GetOrbitRadiusArgs

    public class GetOrbitRadiusArgs : EventArgs
    {
        private Point3D _position;
        private Vector3D _direction;
        private double? _result = null;

        public GetOrbitRadiusArgs(Point3D position, Vector3D direction)
        {
            _position = position;
            _direction = direction;
        }

        /// <summary>
        /// The position is in world coords
        /// </summary>
        public Point3D Position
        {
            get
            {
                return _position;
            }
        }
        public Vector3D Direction
        {
            get
            {
                return _direction;
            }
        }
        /// <summary>
        /// This is the radius of the camera's orbit, if you leave it null, then the trackball will calculate the radius.
        /// (which is the distance to the origin)
        /// </summary>
        public double? Result
        {
            get
            {
                return _result;
            }
            set
            {
                _result = value;
            }
        }
    }

    #endregion
    #region class: UserMovedCameraArgs

    public class UserMovedCameraArgs : EventArgs
    {
        public UserMovedCameraArgs(CameraMovement movement)
        {
            this.Movement = movement;
        }

        public CameraMovement Movement
        {
            get;
            private set;
        }

        public bool IsPan
        {
            get
            {
                switch (this.Movement)
                {
                    case CameraMovement.Orbit:		// orbit is a combination of pan and rotate
                    case CameraMovement.Orbit_AutoScroll:

                    case CameraMovement.Pan:
                    case CameraMovement.Pan_AutoScroll:
                    case CameraMovement.Pan_Keyboard_Down:
                    case CameraMovement.Pan_Keyboard_In:
                    case CameraMovement.Pan_Keyboard_Left:
                    case CameraMovement.Pan_Keyboard_Out:
                    case CameraMovement.Pan_Keyboard_Right:
                    case CameraMovement.Pan_Keyboard_Up:
                    case CameraMovement.Zoom:
                    case CameraMovement.Zoom_AutoScroll:
                        return true;

                    default:
                        return false;
                }
            }
        }
        public bool IsRotate
        {
            get
            {
                switch (this.Movement)
                {
                    case CameraMovement.Orbit:		// orbit is a combination of pan and rotate
                    case CameraMovement.Orbit_AutoScroll:

                    case CameraMovement.RotateAroundLookDirection:
                    case CameraMovement.RotateAroundLookDirection_AutoScroll:
                    case CameraMovement.RotateInPlace:
                    case CameraMovement.RotateInPlace_AutoScroll:
                        return true;

                    default:
                        return false;
                }
            }
        }
    }

    #endregion

    #region enum: CameraMovement

    /// <summary>
    /// NOTE:  These are ordered for TrackBallMapping.GetReport()
    /// </summary>
    public enum CameraMovement
    {
        Pan = 0,
        Pan_AutoScroll,

        Pan_Keyboard_In,		// equivalent to zoom in, but tied to a key
        Pan_Keyboard_Out,
        Pan_Keyboard_Left,		// equivalent to pan, but tied to a key
        Pan_Keyboard_Right,
        Pan_Keyboard_Up,
        Pan_Keyboard_Down,

        //TODO:  Support these.  Default the axis to the camera's rotated x,y,z.  Call a delegate that requests an alternative axis
        //RotateInPlace_Keyboard_XPos,
        //RotateInPlace_Keyboard_XNeg,
        //RotateInPlace_Keyboard_YPos,
        //RotateInPlace_Keyboard_YNeg,
        //RotateInPlace_Keyboard_ZPos,
        //RotateInPlace_Keyboard_ZNeg,

        Orbit,		// this is around all three axiis.  Make another one (OrbitCylinder) that excludes the look direction
        Orbit_AutoScroll,
        RotateInPlace,
        RotateInPlace_AutoScroll,
        RotateAroundLookDirection,
        RotateAroundLookDirection_AutoScroll,
        Zoom,
        Zoom_AutoScroll
    }

    #endregion
    #region class: TrackBallMapping

    public class TrackBallMapping
    {
        #region enum: PrebuiltMapping

        public enum PrebuiltMapping
        {
            /// <summary>
            /// Supports all actions across all 3 buttons (middle button emulated by left+right)
            /// </summary>
            MouseComplete = 0,
            /// <summary>
            /// Same as Complete, but left button has no mapping - drops pan (left+right is still used)
            /// </summary>
            MouseComplete_NoLeft,
            /// <summary>
            /// Same as noleft, but right button function is swapped (ctrl+right is orbit)
            /// </summary>
            MouseComplete_NoLeft_RightRotateInPlace,
            /// <summary>
            /// Same as MouseComplete, but left rotates and right pans
            /// </summary>
            MouseComplete_SwapLeftRight,
            /// <summary>
            /// W maps to in
            /// </summary>
            Keyboard_ASDW_In,
            /// <summary>
            /// W maps to up
            /// </summary>
            Keyboard_ASDW_Up,
            /// <summary>
            /// Up maps to in
            /// </summary>
            Keyboard_Arrows_In,
            /// <summary>
            /// Up maps to up
            /// </summary>
            Keyboard_Arrows_Up

            //Sketchup,		// middle is cylindrical orbit, shift+middle is pan, wheel is zoom (but there are toggles that map those actions to the left mouse button)
            //Sims2
        }

        #endregion

        #region Declaration Section

        private CameraMovement _movement;

        private List<MouseButton[]> _mouseButtons = new List<MouseButton[]>();
        private List<Key[]> _keys = new List<Key[]>();

        #endregion

        #region Constructor

        /// <summary>
        /// Use this constructor if you are going to add more complex mouse/key combinations
        /// </summary>
        public TrackBallMapping(CameraMovement movement)
        {
            _movement = movement;
        }
        public TrackBallMapping(CameraMovement movement, MouseButton mouseButton)
            : this(movement)
        {
            this.Add(mouseButton);
        }
        public TrackBallMapping(CameraMovement movement, MouseButton mouseButton, Key key)
            : this(movement)
        {
            this.Add(mouseButton);
            this.Add(key);
        }
        public TrackBallMapping(CameraMovement movement, Key key)
            : this(movement)
        {
            this.Add(key);
        }
        public TrackBallMapping(CameraMovement movement, IEnumerable<Key> keys)
            : this(movement)
        {
            this.Add(keys);
        }
        /// <summary>
        /// The keys in this overload are an OR condition (LeftShift or RightShift)
        /// </summary>
        public TrackBallMapping(CameraMovement movement, MouseButton mouseButton, IEnumerable<Key> keys)
            : this(movement)
        {
            this.Add(mouseButton);
            this.Add(keys);
        }

        #endregion

        #region Public Properties

        public CameraMovement Movement
        {
            get
            {
                return _movement;
            }
        }

        #endregion

        #region Public Methods

        public bool IsMatch(MouseEventArgs e)
        {
            if (_mouseButtons.Count == 0)
            {
                return false;
            }

            #region Check for mouse match

            foreach (MouseButton[] buttons in _mouseButtons)
            {
                // Only one of buttons needs to match
                bool isPressed = false;
                for (int cntr = 0; cntr < buttons.Length; cntr++)
                {
                    #region Check if pressed

                    switch (buttons[cntr])
                    {
                        case MouseButton.Left:
                            if (e.LeftButton == MouseButtonState.Pressed)
                            {
                                isPressed = true;
                            }
                            break;

                        case MouseButton.Middle:
                            if (e.MiddleButton == MouseButtonState.Pressed)
                            {
                                isPressed = true;
                            }
                            break;

                        case MouseButton.Right:
                            if (e.RightButton == MouseButtonState.Pressed)
                            {
                                isPressed = true;
                            }
                            break;

                        case MouseButton.XButton1:
                            if (e.XButton1 == MouseButtonState.Pressed)
                            {
                                isPressed = true;
                            }
                            break;

                        case MouseButton.XButton2:
                            if (e.XButton2 == MouseButtonState.Pressed)
                            {
                                isPressed = true;
                            }
                            break;

                        default:
                            throw new ApplicationException("Unknown MouseButton: " + buttons[cntr]);
                    }

                    #endregion

                    if (isPressed)
                    {
                        break;
                    }
                }

                if (!isPressed)
                {
                    // Since the items in _mouseButtons are an AND condition, this mapping failed
                    return false;
                }
            }

            #endregion

            if (_keys.Count > 0)
            {
                #region Check for key match

                foreach (Key[] keys in _keys)
                {
                    // Only one of keys needs to match
                    bool isPressed = false;
                    for (int cntr = 0; cntr < keys.Length; cntr++)
                    {
                        if (Keyboard.IsKeyDown(keys[cntr]))
                        {
                            isPressed = true;
                            break;
                        }
                    }

                    if (!isPressed)
                    {
                        // Since the items in _keys are an AND condition, this mapping failed
                        return false;
                    }
                }

                #endregion
            }

            // Exit Function
            return true;
        }
        public bool IsMatch(KeyEventArgs e)
        {
            if (_mouseButtons.Count > 0)
            {
                // This overload cares about keyboard only actions
                return false;
            }

            if (_keys.Count > 0)
            {
                foreach (Key[] keys in _keys)
                {
                    int index = Array.IndexOf<Key>(keys, e.Key);
                    if (index < 0)
                    {
                        continue;
                    }

                    if (keys.Length == 1)
                    {
                        return true;
                    }

                    // The key is pressed, but it is conditional on the other modifiers being pressed as well (ctrl, shift, alt)
                    bool isPressed = true;
                    for (int cntr = 0; cntr < keys.Length; cntr++)
                    {
                        if (cntr == index)
                        {
                            continue;
                        }

                        if (!Keyboard.IsKeyDown(keys[cntr]))
                        {
                            isPressed = false;		// can't return false here, because _keys may have another combo that matches (ex: leftalt + uparrow, or rightalt + uparrow)
                            break;
                        }
                    }

                    if (isPressed)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Each time Add is called, it is an AND condition (if you add left mouse button, then add right mouse button, then both
        /// must be pressed for this to be a match)
        /// </summary>
        public void Add(MouseButton mouseButton)
        {
            _mouseButtons.Add(new MouseButton[] { mouseButton });
        }
        /// <summary>
        /// Each time Add is called, it is an AND condition (if you add left mouse button, then add right mouse button, then both
        /// must be pressed for this to be a match)
        /// </summary>
        public void Add(Key key)
        {
            _keys.Add(new Key[] { key });
        }
        /// <summary>
        /// Each time Add is called, it is an AND condition (if you add left mouse button, then add right mouse button, then both
        /// must be pressed for this to be a match)
        /// </summary>
        /// <param name="mouseButtons">The buttons added together are an OR condition</param>
        public void Add(IEnumerable<MouseButton> mouseButtons)
        {
            if (mouseButtons.Count<MouseButton>() == 0)
            {
                throw new ArgumentException("No mouse buttons were passed in");
            }

            _mouseButtons.Add(mouseButtons.ToArray<MouseButton>());
        }
        /// <summary>
        /// Each time Add is called, it is an AND condition (if you add left mouse button, then add right mouse button, then both
        /// must be pressed for this to be a match)
        /// </summary>
        /// <param name="keys">The keys added together are an OR condition</param>
        public void Add(IEnumerable<Key> keys)
        {
            if (keys.Count<Key>() == 0)
            {
                throw new ArgumentException("No keys were passed in");
            }

            _keys.Add(keys.ToArray<Key>());
        }

        public string DescribeMapping(bool includeAction)
        {
            StringBuilder retVal = new StringBuilder(100);

            if (includeAction)
            {
                retVal.Append(_movement.ToString());
                retVal.Append(": ");
            }

            // Keys First
            for (int cntr = 0; cntr < _keys.Count; cntr++)
            {
                if (cntr > 0)
                {
                    retVal.Append("+");
                }

                retVal.Append(DescribeKeys(_keys[cntr]));
            }

            // Put a plus between keys and buttons
            if (_keys.Count > 0 && _mouseButtons.Count > 0)
            {
                retVal.Append("+");
            }

            // Buttons
            for (int cntr = 0; cntr < _mouseButtons.Count; cntr++)
            {
                if (cntr > 0)
                {
                    retVal.Append("+");
                }

                retVal.Append(DescribeButtons(_mouseButtons[cntr]));
            }

            // Exit Function
            return retVal.ToString();
        }

        /// <summary>
        /// This converts the enums into a bunch of mappings
        /// NOTE:  It's ok to add a prebuilt mouse then a prebuilt keyboard
        /// </summary>
        public static TrackBallMapping[] GetPrebuilt(PrebuiltMapping mapping)
        {
            List<TrackBallMapping> retVal = new List<TrackBallMapping>();

            TrackBallMapping complexMapping = null;

            // NOTE: Order is important, the most complex mouse/button combo must be added before the more simple
            // mouse only (or the mouse only would win)

            switch (mapping)
            {
                case PrebuiltMapping.MouseComplete:
                    #region Complete

                    // Get everything except the left
                    retVal.AddRange(GetPrebuilt(PrebuiltMapping.MouseComplete_NoLeft));

                    // Left
                    retVal.Add(new TrackBallMapping(CameraMovement.Pan, MouseButton.Left));

                    #endregion
                    break;

                case PrebuiltMapping.MouseComplete_NoLeft:
                    #region Complete_NoLeft

                    // Middle Button
                    complexMapping = new TrackBallMapping(CameraMovement.RotateAroundLookDirection_AutoScroll);
                    complexMapping.Add(MouseButton.Middle);
                    complexMapping.Add(new Key[] { Key.LeftCtrl, Key.RightCtrl });
                    complexMapping.Add(new Key[] { Key.LeftAlt, Key.RightAlt });
                    retVal.Add(complexMapping);
                    retVal.Add(new TrackBallMapping(CameraMovement.RotateAroundLookDirection, MouseButton.Middle, new Key[] { Key.LeftCtrl, Key.RightCtrl }));

                    complexMapping = new TrackBallMapping(CameraMovement.Zoom_AutoScroll);
                    complexMapping.Add(MouseButton.Middle);
                    complexMapping.Add(new Key[] { Key.LeftShift, Key.RightShift });
                    complexMapping.Add(new Key[] { Key.LeftAlt, Key.RightAlt });
                    retVal.Add(complexMapping);
                    retVal.Add(new TrackBallMapping(CameraMovement.Zoom, MouseButton.Middle, new Key[] { Key.LeftShift, Key.RightShift }));

                    retVal.Add(new TrackBallMapping(CameraMovement.Pan_AutoScroll, MouseButton.Middle));

                    // Left+Right Buttons (emulate middle)
                    complexMapping = new TrackBallMapping(CameraMovement.RotateAroundLookDirection_AutoScroll);
                    complexMapping.Add(MouseButton.Left);
                    complexMapping.Add(MouseButton.Right);
                    complexMapping.Add(new Key[] { Key.LeftCtrl, Key.RightCtrl });
                    complexMapping.Add(new Key[] { Key.LeftAlt, Key.RightAlt });
                    retVal.Add(complexMapping);

                    complexMapping = new TrackBallMapping(CameraMovement.RotateAroundLookDirection);
                    complexMapping.Add(MouseButton.Left);
                    complexMapping.Add(MouseButton.Right);
                    complexMapping.Add(new Key[] { Key.LeftCtrl, Key.RightCtrl });
                    retVal.Add(complexMapping);

                    complexMapping = new TrackBallMapping(CameraMovement.Zoom_AutoScroll);
                    complexMapping.Add(MouseButton.Left);
                    complexMapping.Add(MouseButton.Right);
                    complexMapping.Add(new Key[] { Key.LeftShift, Key.RightShift });
                    complexMapping.Add(new Key[] { Key.LeftAlt, Key.RightAlt });
                    retVal.Add(complexMapping);

                    complexMapping = new TrackBallMapping(CameraMovement.Zoom);
                    complexMapping.Add(MouseButton.Left);
                    complexMapping.Add(MouseButton.Right);
                    complexMapping.Add(new Key[] { Key.LeftShift, Key.RightShift });
                    retVal.Add(complexMapping);

                    complexMapping = new TrackBallMapping(CameraMovement.Pan_AutoScroll);
                    complexMapping.Add(MouseButton.Left);
                    complexMapping.Add(MouseButton.Right);
                    retVal.Add(complexMapping);

                    // Right Button
                    complexMapping = new TrackBallMapping(CameraMovement.RotateInPlace_AutoScroll);
                    complexMapping.Add(MouseButton.Right);
                    complexMapping.Add(new Key[] { Key.LeftCtrl, Key.RightCtrl });
                    complexMapping.Add(new Key[] { Key.LeftAlt, Key.RightAlt });
                    retVal.Add(complexMapping);
                    retVal.Add(new TrackBallMapping(CameraMovement.RotateInPlace, MouseButton.Right, new Key[] { Key.LeftCtrl, Key.RightCtrl }));

                    retVal.Add(new TrackBallMapping(CameraMovement.Orbit_AutoScroll, MouseButton.Right, new Key[] { Key.LeftAlt, Key.RightAlt }));
                    retVal.Add(new TrackBallMapping(CameraMovement.Orbit, MouseButton.Right));

                    #endregion
                    break;

                case PrebuiltMapping.MouseComplete_NoLeft_RightRotateInPlace:
                    #region MouseComplete_NoLeft_RightRotateInPlace

                    // Middle Button
                    complexMapping = new TrackBallMapping(CameraMovement.RotateAroundLookDirection_AutoScroll);
                    complexMapping.Add(MouseButton.Middle);
                    complexMapping.Add(new Key[] { Key.LeftCtrl, Key.RightCtrl });
                    complexMapping.Add(new Key[] { Key.LeftAlt, Key.RightAlt });
                    retVal.Add(complexMapping);
                    retVal.Add(new TrackBallMapping(CameraMovement.RotateAroundLookDirection, MouseButton.Middle, new Key[] { Key.LeftCtrl, Key.RightCtrl }));

                    complexMapping = new TrackBallMapping(CameraMovement.Zoom_AutoScroll);
                    complexMapping.Add(MouseButton.Middle);
                    complexMapping.Add(new Key[] { Key.LeftShift, Key.RightShift });
                    complexMapping.Add(new Key[] { Key.LeftAlt, Key.RightAlt });
                    retVal.Add(complexMapping);
                    retVal.Add(new TrackBallMapping(CameraMovement.Zoom, MouseButton.Middle, new Key[] { Key.LeftShift, Key.RightShift }));

                    retVal.Add(new TrackBallMapping(CameraMovement.Pan_AutoScroll, MouseButton.Middle));

                    // Left+Right Buttons (emulate middle)
                    complexMapping = new TrackBallMapping(CameraMovement.RotateAroundLookDirection_AutoScroll);
                    complexMapping.Add(MouseButton.Left);
                    complexMapping.Add(MouseButton.Right);
                    complexMapping.Add(new Key[] { Key.LeftCtrl, Key.RightCtrl });
                    complexMapping.Add(new Key[] { Key.LeftAlt, Key.RightAlt });
                    retVal.Add(complexMapping);

                    complexMapping = new TrackBallMapping(CameraMovement.RotateAroundLookDirection);
                    complexMapping.Add(MouseButton.Left);
                    complexMapping.Add(MouseButton.Right);
                    complexMapping.Add(new Key[] { Key.LeftCtrl, Key.RightCtrl });
                    retVal.Add(complexMapping);

                    complexMapping = new TrackBallMapping(CameraMovement.Zoom_AutoScroll);
                    complexMapping.Add(MouseButton.Left);
                    complexMapping.Add(MouseButton.Right);
                    complexMapping.Add(new Key[] { Key.LeftShift, Key.RightShift });
                    complexMapping.Add(new Key[] { Key.LeftAlt, Key.RightAlt });
                    retVal.Add(complexMapping);

                    complexMapping = new TrackBallMapping(CameraMovement.Zoom);
                    complexMapping.Add(MouseButton.Left);
                    complexMapping.Add(MouseButton.Right);
                    complexMapping.Add(new Key[] { Key.LeftShift, Key.RightShift });
                    retVal.Add(complexMapping);

                    complexMapping = new TrackBallMapping(CameraMovement.Pan_AutoScroll);
                    complexMapping.Add(MouseButton.Left);
                    complexMapping.Add(MouseButton.Right);
                    retVal.Add(complexMapping);

                    // Right Button
                    complexMapping = new TrackBallMapping(CameraMovement.Orbit_AutoScroll);
                    complexMapping.Add(MouseButton.Right);
                    complexMapping.Add(new Key[] { Key.LeftCtrl, Key.RightCtrl });
                    complexMapping.Add(new Key[] { Key.LeftAlt, Key.RightAlt });
                    retVal.Add(complexMapping);
                    retVal.Add(new TrackBallMapping(CameraMovement.Orbit, MouseButton.Right, new Key[] { Key.LeftCtrl, Key.RightCtrl }));

                    retVal.Add(new TrackBallMapping(CameraMovement.RotateInPlace_AutoScroll, MouseButton.Right, new Key[] { Key.LeftAlt, Key.RightAlt }));
                    retVal.Add(new TrackBallMapping(CameraMovement.RotateInPlace, MouseButton.Right));

                    #endregion
                    break;

                case PrebuiltMapping.MouseComplete_SwapLeftRight:
                    #region MouseComplete_SwapLeftRight

                    // Right
                    retVal.Add(new TrackBallMapping(CameraMovement.Pan, MouseButton.Right));

                    // Middle Button
                    complexMapping = new TrackBallMapping(CameraMovement.RotateAroundLookDirection_AutoScroll);
                    complexMapping.Add(MouseButton.Middle);
                    complexMapping.Add(new Key[] { Key.LeftCtrl, Key.RightCtrl });
                    complexMapping.Add(new Key[] { Key.LeftAlt, Key.RightAlt });
                    retVal.Add(complexMapping);
                    retVal.Add(new TrackBallMapping(CameraMovement.RotateAroundLookDirection, MouseButton.Middle, new Key[] { Key.LeftCtrl, Key.RightCtrl }));

                    complexMapping = new TrackBallMapping(CameraMovement.Zoom_AutoScroll);
                    complexMapping.Add(MouseButton.Middle);
                    complexMapping.Add(new Key[] { Key.LeftShift, Key.RightShift });
                    complexMapping.Add(new Key[] { Key.LeftAlt, Key.RightAlt });
                    retVal.Add(complexMapping);
                    retVal.Add(new TrackBallMapping(CameraMovement.Zoom, MouseButton.Middle, new Key[] { Key.LeftShift, Key.RightShift }));

                    retVal.Add(new TrackBallMapping(CameraMovement.Pan_AutoScroll, MouseButton.Middle));

                    // Left+Right Buttons (emulate middle)
                    complexMapping = new TrackBallMapping(CameraMovement.RotateAroundLookDirection_AutoScroll);
                    complexMapping.Add(MouseButton.Left);
                    complexMapping.Add(MouseButton.Right);
                    complexMapping.Add(new Key[] { Key.LeftCtrl, Key.RightCtrl });
                    complexMapping.Add(new Key[] { Key.LeftAlt, Key.RightAlt });
                    retVal.Add(complexMapping);

                    complexMapping = new TrackBallMapping(CameraMovement.RotateAroundLookDirection);
                    complexMapping.Add(MouseButton.Left);
                    complexMapping.Add(MouseButton.Right);
                    complexMapping.Add(new Key[] { Key.LeftCtrl, Key.RightCtrl });
                    retVal.Add(complexMapping);

                    complexMapping = new TrackBallMapping(CameraMovement.Zoom_AutoScroll);
                    complexMapping.Add(MouseButton.Left);
                    complexMapping.Add(MouseButton.Right);
                    complexMapping.Add(new Key[] { Key.LeftShift, Key.RightShift });
                    complexMapping.Add(new Key[] { Key.LeftAlt, Key.RightAlt });
                    retVal.Add(complexMapping);

                    complexMapping = new TrackBallMapping(CameraMovement.Zoom);
                    complexMapping.Add(MouseButton.Left);
                    complexMapping.Add(MouseButton.Right);
                    complexMapping.Add(new Key[] { Key.LeftShift, Key.RightShift });
                    retVal.Add(complexMapping);

                    complexMapping = new TrackBallMapping(CameraMovement.Pan_AutoScroll);
                    complexMapping.Add(MouseButton.Left);
                    complexMapping.Add(MouseButton.Right);
                    retVal.Add(complexMapping);

                    // Left Button
                    complexMapping = new TrackBallMapping(CameraMovement.RotateInPlace_AutoScroll);
                    complexMapping.Add(MouseButton.Left);
                    complexMapping.Add(new Key[] { Key.LeftCtrl, Key.RightCtrl });
                    complexMapping.Add(new Key[] { Key.LeftAlt, Key.RightAlt });
                    retVal.Add(complexMapping);
                    retVal.Add(new TrackBallMapping(CameraMovement.RotateInPlace, MouseButton.Left, new Key[] { Key.LeftCtrl, Key.RightCtrl }));

                    retVal.Add(new TrackBallMapping(CameraMovement.Orbit_AutoScroll, MouseButton.Left, new Key[] { Key.LeftAlt, Key.RightAlt }));
                    retVal.Add(new TrackBallMapping(CameraMovement.Orbit, MouseButton.Left));

                    #endregion
                    break;

                case PrebuiltMapping.Keyboard_ASDW_In:
                    #region Keyboard_ASDW_In

                    retVal.Add(new TrackBallMapping(CameraMovement.Pan_Keyboard_In, Key.W));
                    retVal.Add(new TrackBallMapping(CameraMovement.Pan_Keyboard_Out, Key.S));
                    retVal.Add(new TrackBallMapping(CameraMovement.Pan_Keyboard_Left, Key.A));
                    retVal.Add(new TrackBallMapping(CameraMovement.Pan_Keyboard_Right, Key.D));

                    #endregion
                    break;

                case PrebuiltMapping.Keyboard_ASDW_Up:
                    #region Keyboard_ASDW_In

                    retVal.Add(new TrackBallMapping(CameraMovement.Pan_Keyboard_Up, Key.W));
                    retVal.Add(new TrackBallMapping(CameraMovement.Pan_Keyboard_Down, Key.S));
                    retVal.Add(new TrackBallMapping(CameraMovement.Pan_Keyboard_Left, Key.A));
                    retVal.Add(new TrackBallMapping(CameraMovement.Pan_Keyboard_Right, Key.D));

                    #endregion
                    break;

                case PrebuiltMapping.Keyboard_Arrows_In:
                    #region Keyboard_ASDW_In

                    retVal.Add(new TrackBallMapping(CameraMovement.Pan_Keyboard_In, Key.Up));
                    retVal.Add(new TrackBallMapping(CameraMovement.Pan_Keyboard_Out, Key.Down));
                    retVal.Add(new TrackBallMapping(CameraMovement.Pan_Keyboard_Left, Key.Left));
                    retVal.Add(new TrackBallMapping(CameraMovement.Pan_Keyboard_Right, Key.Right));

                    #endregion
                    break;

                case PrebuiltMapping.Keyboard_Arrows_Up:
                    #region Keyboard_ASDW_In

                    retVal.Add(new TrackBallMapping(CameraMovement.Pan_Keyboard_Up, Key.Up));
                    retVal.Add(new TrackBallMapping(CameraMovement.Pan_Keyboard_Down, Key.Down));
                    retVal.Add(new TrackBallMapping(CameraMovement.Pan_Keyboard_Left, Key.Left));
                    retVal.Add(new TrackBallMapping(CameraMovement.Pan_Keyboard_Right, Key.Right));

                    #endregion
                    break;

                default:
                    throw new ApplicationException("Unknown PrebuiltMapping: " + mapping.ToString());
            }

            // Exit Function
            return retVal.ToArray();
        }

        /// <summary>
        /// This describes the mappings, so this report could be placed in a help panel
        /// </summary>
        public static string GetReport(IEnumerable<TrackBallMapping> mappings, bool doesWheelZoom)
        {
            StringBuilder retVal = new StringBuilder(1024);

            // Divide this up into movements
            SortedList<CameraMovement, List<TrackBallMapping>> mappingsByMovement = GetMappingsByMovement(mappings);

            // I need all the movements, because of the wheelmouse not being one of the standard mappings
            CameraMovement[] allMovements = (CameraMovement[])Enum.GetValues(typeof(CameraMovement));

            foreach (CameraMovement movement in allMovements)
            {
                if (movement == CameraMovement.Zoom && doesWheelZoom)
                {
                    // Can't continue (it's just easier to write the if statement this way)
                }
                else if (!mappingsByMovement.ContainsKey(movement))
                {
                    continue;
                }

                // Put an extra line between actions
                if (retVal.Length > 0)
                {
                    retVal.AppendLine();
                }

                // Describe the action
                retVal.Append(ConvertReportMovement(movement));
                retVal.AppendLine(":");

                // Append mappings
                if (mappingsByMovement.ContainsKey(movement))
                {
                    retVal.Append(ReportMappingGroup(mappingsByMovement[movement]));		// it puts in its own line terminators in
                }

                if (movement == CameraMovement.Zoom && doesWheelZoom)
                {
                    retVal.AppendLine("Wheel");
                }
            }

            // Exit Function
            return retVal.ToString();
        }

        #endregion

        #region Private Methods

        private static SortedList<CameraMovement, List<TrackBallMapping>> GetMappingsByMovement(IEnumerable<TrackBallMapping> mappings)
        {
            SortedList<CameraMovement, List<TrackBallMapping>> retVal = new SortedList<CameraMovement, List<TrackBallMapping>>();

            foreach (TrackBallMapping mapping in mappings)
            {
                if (!retVal.ContainsKey(mapping.Movement))
                {
                    retVal.Add(mapping.Movement, new List<TrackBallMapping>());
                }

                retVal[mapping.Movement].Add(mapping);
            }

            // Exit Function
            return retVal;
        }

        private static string ConvertReportMovement(CameraMovement movement)
        {
            string retVal = null;

            retVal = movement.ToString();

            switch (movement)
            {
                case CameraMovement.Pan_Keyboard_In:
                    retVal += " (sometimes used as zoom in)";
                    break;

                case CameraMovement.Pan_Keyboard_Out:
                    retVal += " (sometimes used as zoom out)";
                    break;
            }

            retVal = retVal.Replace('_', ' ');

            return retVal;
        }
        private static string ReportMappingGroup(List<TrackBallMapping> mappings)
        {
            List<string> descriptions = new List<string>();
            foreach (TrackBallMapping mapping in mappings)
            {
                descriptions.Add(mapping.DescribeMapping(false));
            }

            StringBuilder retVal = new StringBuilder(512);
            foreach (string description in descriptions.Distinct())
            {
                retVal.AppendLine(description);
            }

            // Exit Function
            return retVal.ToString();
        }

        private string DescribeKeys(Key[] keys)
        {
            if (keys.Length == 0)
            {
                return "";
            }

            List<string> consolidated = DescribeKeys_Consilidate(keys);
            if (consolidated.Count == 1)
            {
                return consolidated[0];
            }

            StringBuilder retVal = new StringBuilder(100);

            retVal.Append("(");

            for (int cntr = 0; cntr < consolidated.Count; cntr++)
            {
                if (cntr > 0)
                {
                    retVal.Append(" | ");
                }

                retVal.Append(consolidated[cntr]);
            }

            retVal.Append(")");

            // Exit Function
            return retVal.ToString();
        }
        private List<string> DescribeKeys_Consilidate(Key[] keys)
        {
            List<string> retVal = new List<string>();

            List<Key> allKeys = new List<Key>(keys);

            int index = 0;
            while (index < allKeys.Count)
            {
                #region See if this is one of the left/right paired keys (or oem/numpad dupes)

                string keyDescription = null;
                Key? searchForPair = null;
                switch (allKeys[index])
                {
                    case Key.LeftAlt:
                        keyDescription = "Alt";
                        searchForPair = Key.RightAlt;
                        break;
                    case Key.RightAlt:
                        keyDescription = "Alt";
                        searchForPair = Key.LeftAlt;
                        break;

                    case Key.LeftCtrl:
                        keyDescription = "Ctrl";
                        searchForPair = Key.RightCtrl;
                        break;
                    case Key.RightCtrl:
                        keyDescription = "Ctrl";
                        searchForPair = Key.LeftCtrl;
                        break;

                    case Key.LeftShift:
                        keyDescription = "Shift";
                        searchForPair = Key.RightShift;
                        break;
                    case Key.RightShift:
                        keyDescription = "Shift";
                        searchForPair = Key.LeftShift;
                        break;

                    case Key.Add:
                        keyDescription = "Plus";
                        searchForPair = Key.OemPlus;
                        break;
                    case Key.OemPlus:
                        keyDescription = "Plus";
                        searchForPair = Key.Add;
                        break;

                    case Key.Subtract:
                        keyDescription = "Minus";
                        searchForPair = Key.OemMinus;
                        break;
                    case Key.OemMinus:
                        keyDescription = "Minus";
                        searchForPair = Key.Subtract;
                        break;

                        //TODO: Do the rest of the oem/numpad dupes
                        //case Key.Oem1
                        //case Key.Oem2
                        //case Key.Oem3
                        //case Key.Oem4
                        //case Key.Oem5
                        //case Key.Oem6
                        //case Key.Oem7
                        //case Key.Oem8
                        //case Key.Oem9		// where's 0?
                        //case Key.NumPad0
                        //case Key.NumPad1
                        //case Key.NumPad2
                        //case Key.NumPad3
                        //case Key.NumPad4
                        //case Key.NumPad5
                        //case Key.NumPad6
                        //case Key.NumPad7
                        //case Key.NumPad8
                        //case Key.NumPad9

                        //case Key.OemPeriod
                        //case Key.OemQuestion
                        //case Key.Decimal
                        //case Key.Divide

                        //etc
                }

                #endregion

                if (searchForPair != null)
                {
                    #region Search for pair

                    int foundIndex = allKeys.IndexOf(searchForPair.Value, index + 1);
                    if (foundIndex > index)
                    {
                        allKeys.RemoveAt(foundIndex);
                    }

                    #endregion
                }

                // Add description
                if (keyDescription == null)
                {
                    string description = allKeys[index].ToString();

                    // Remove Oem
                    description = Regex.Replace(description, "^Oem", "");

                    retVal.Add(allKeys[index].ToString());
                }
                else
                {
                    retVal.Add(keyDescription);
                }

                // Bump index
                index++;
            }

            // Exit Function
            return retVal;
        }

        private string DescribeButtons(MouseButton[] buttons)
        {
            if (buttons.Length == 0)
            {
                return "";
            }
            else if (buttons.Length == 1)
            {
                return buttons[0].ToString();
            }

            StringBuilder retVal = new StringBuilder(100);

            retVal.Append("(");

            for (int cntr = 0; cntr < buttons.Length; cntr++)
            {
                if (cntr > 0)
                {
                    retVal.Append(" | ");
                }

                retVal.Append(buttons[cntr].ToString());
            }

            retVal.Append(")");

            // Exit Function
            return retVal.ToString();
        }

        #endregion
    }

    #endregion
}
