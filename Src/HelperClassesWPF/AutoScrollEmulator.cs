using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

using Game.HelperClassesCore;

namespace Game.HelperClassesWPF
{
    /// <summary>
    /// This class will emulate an autoscroll.  It is meant for controls that implement their own scrolling (not derived
    /// from ScrollableControl) - I'm not sure what the WPF equivelent to this is
    /// 
    /// TODO:  Have an option for horizontal only/vertical only/horizontal-vertical
    /// TODO:  Make another class that takes in a UI element, listens to events, and does the scrolling (won't work with everything, but will work with most)
    /// </summary>
    /// <remarks>
    /// To use this, map:
    /// MiddleMouse Down -> this.StartAutoScroll
    /// MiddleMouse Up -> this.StopAutoScroll
    /// MouseMove -> this.MouseMove
    /// 
    /// Then just respond to this.AutoScroll and this.CursorChanged
    /// 
    /// NOTE:  You will need to manually paint this.CenterIconImage at this.InitialPoint (minus size/2) when this.IsAutoScrollActive
    /// is true
    /// </remarks>
    public class AutoScrollEmulator
    {
        #region Class: CenterImage

        private class CenterImage
        {
            #region Declaration Section

            /// <summary>
            /// This is a user control that displays the center image
            /// </summary>
            private Popup _popup = null;

            /// <summary>
            /// This is the center image
            /// </summary>
            private Canvas _canvas = null;

            // These arrows are here so I can set their colors as the user mouses around
            private Polygon _triangleTop = null;
            private Polygon _triangleRight = null;
            private Polygon _triangleBottom = null;
            private Polygon _triangleLeft = null;

            #endregion

            #region Constructor

            public CenterImage()
            {
                // Center Image
                _canvas = BuildImage(out _triangleTop, out _triangleRight, out _triangleBottom, out _triangleLeft);
                _canvas.HorizontalAlignment = HorizontalAlignment.Center;
                _canvas.VerticalAlignment = VerticalAlignment.Center;

                // Popup (it seems to work fine without a control owning it)
                _popup = new Popup();

                _popup.AllowsTransparency = true;
                _popup.Placement = PlacementMode.Absolute;
                _popup.Cursor = Cursors.ScrollAll;		// using this, since that's the icon in the dead zone anyway
                _popup.Focusable = false;
                _popup.IsHitTestVisible = false;

                _popup.HorizontalAlignment = HorizontalAlignment.Left;
                _popup.VerticalAlignment = VerticalAlignment.Top;

                _popup.Width = _canvas.Width + 2;
                _popup.Height = _canvas.Height + 2;

                double offsetX = _popup.Width / 2d;
                double offsetY = _popup.Height / 2d;
                _canvas.RenderTransform = new TranslateTransform(offsetX, offsetY);

                _popup.Child = _canvas;
            }

            #endregion

            #region Public Methods

            /// <summary>
            /// This sets the arrow colors back to normal
            /// </summary>
            public void ResetSpeed()
            {
                SetSpeed(0d, 0d);
            }
            /// <summary>
            /// This colors the arrows according to the speeds passed in
            /// </summary>
            public void SetSpeed(double x, double y)
            {
                LinearGradientBrush brush0 = GetArrowBrush(0d);
                LinearGradientBrush brushX = GetArrowBrush(Math.Abs(x));
                LinearGradientBrush brushY = GetArrowBrush(Math.Abs(y));

                if (x < 0d)
                {
                    _triangleLeft.Fill = brushX;
                    _triangleRight.Fill = brush0;
                }
                else
                {
                    _triangleLeft.Fill = brush0;
                    _triangleRight.Fill = brushX;
                }

                if (y < 0d)
                {
                    _triangleTop.Fill = brushY;
                    _triangleBottom.Fill = brush0;
                }
                else
                {
                    _triangleTop.Fill = brush0;
                    _triangleBottom.Fill = brushY;
                }
            }

            public void ShowCenterImage(Point centerPoint)
            {
                _popup.HorizontalOffset = centerPoint.X + 1;		// it's off by one pixel
                _popup.VerticalOffset = centerPoint.Y + 1;

                _popup.IsOpen = true;

                // There doesn't seem to be a way to make the popup straddle a screen edge, so if they click near the edge,
                // this will get shifted over.  According to MSDN, this is for "security reasons"
            }
            public void HideCenterImage()
            {
                _popup.IsOpen = false;
            }

            #endregion

            #region Private Methods

            private static Canvas BuildImage(out Polygon triangleTop, out Polygon triangleRight, out Polygon triangleBottom, out Polygon triangleLeft)
            {
                // Create a canvas that will hold the other shapes
                Canvas retVal = new Canvas();
                retVal.Width = 30;
                retVal.Height = 30;
                retVal.HorizontalAlignment = HorizontalAlignment.Left;
                retVal.VerticalAlignment = VerticalAlignment.Top;
                retVal.Background = Brushes.Transparent;

                // Outer Circle
                Ellipse outerCircle = new Ellipse();
                outerCircle.Fill = new SolidColorBrush(Color.FromArgb(128, 255, 255, 255));
                outerCircle.Stroke = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0));
                outerCircle.StrokeThickness = 1;
                outerCircle.Width = 28;
                outerCircle.Height = 28;
                outerCircle.HorizontalAlignment = HorizontalAlignment.Center;
                outerCircle.VerticalAlignment = VerticalAlignment.Center;
                outerCircle.RenderTransform = new TranslateTransform(outerCircle.Width / -2d, outerCircle.Height / -2d);

                // Center Dot
                Ellipse centerDot = new Ellipse();
                centerDot.Fill = new RadialGradientBrush(Color.FromArgb(96, 0, 0, 0), Color.FromArgb(32, 0, 0, 0));
                centerDot.Width = 4;
                centerDot.Height = 4;
                centerDot.HorizontalAlignment = HorizontalAlignment.Center;
                centerDot.VerticalAlignment = VerticalAlignment.Center;
                centerDot.RenderTransform = new TranslateTransform(-2, -2);

                // Triangle Points
                PointCollection trianglePoints = new PointCollection();
                trianglePoints.Add(new Point(-5, 5));
                trianglePoints.Add(new Point(5, 5));
                trianglePoints.Add(new Point(0, 0));

                // Triangle Brush
                LinearGradientBrush triangleBrush = GetArrowBrush(0);		// the inital image can reuse the same brush for all arrows.  As speeds change, the brushes will get swapped out

                // Triangle Top
                triangleTop = new Polygon();
                triangleTop.Fill = triangleBrush;
                triangleTop.Points = trianglePoints;
                triangleTop.HorizontalAlignment = HorizontalAlignment.Center;
                triangleTop.VerticalAlignment = VerticalAlignment.Center;
                triangleTop.RenderTransform = new TranslateTransform(0, -6 - 5);

                // Triangle Right
                triangleRight = new Polygon();
                triangleRight.Fill = triangleBrush;
                triangleRight.Points = trianglePoints;
                triangleRight.HorizontalAlignment = HorizontalAlignment.Center;
                triangleRight.VerticalAlignment = VerticalAlignment.Center;
                triangleRight.RenderTransform = new TransformGroup();
                ((TransformGroup)triangleRight.RenderTransform).Children.Add(new TranslateTransform(0, -6 - 5));
                ((TransformGroup)triangleRight.RenderTransform).Children.Add(new RotateTransform(90));

                // Triangle Bottom
                triangleBottom = new Polygon();
                triangleBottom.Fill = triangleBrush;
                triangleBottom.Points = trianglePoints;
                triangleBottom.HorizontalAlignment = HorizontalAlignment.Center;
                triangleBottom.VerticalAlignment = VerticalAlignment.Center;
                triangleBottom.RenderTransform = new TransformGroup();
                ((TransformGroup)triangleBottom.RenderTransform).Children.Add(new TranslateTransform(0, -6 - 5));
                ((TransformGroup)triangleBottom.RenderTransform).Children.Add(new RotateTransform(180));

                // Triangle Left
                triangleLeft = new Polygon();
                triangleLeft.Fill = triangleBrush;
                triangleLeft.Points = trianglePoints;
                triangleLeft.HorizontalAlignment = HorizontalAlignment.Center;
                triangleLeft.VerticalAlignment = VerticalAlignment.Center;
                triangleLeft.RenderTransform = new TransformGroup();
                ((TransformGroup)triangleLeft.RenderTransform).Children.Add(new TranslateTransform(0, -6 - 5));
                ((TransformGroup)triangleLeft.RenderTransform).Children.Add(new RotateTransform(270));

                // Add them to the canvas
                retVal.Children.Add(outerCircle);
                retVal.Children.Add(centerDot);
                retVal.Children.Add(triangleTop);
                retVal.Children.Add(triangleRight);
                retVal.Children.Add(triangleBottom);
                retVal.Children.Add(triangleLeft);

                // Exit Function
                return retVal;
            }

            /// <summary>
            /// This returns a linear gradient brush to paint an arrow.  The arrow gets darker as the speed increases
            /// </summary>
            private static LinearGradientBrush GetArrowBrush(double speed)
            {
                // Since the speed is distance cubed, I want to take the cube root so the darkness grows linearly.  If I don't
                // then it stays light for most of the range, and spikes to dark.
                //double cubeRoot = Math.Pow(speed, .33333333333d);
                double cubeRoot = Math.Pow(speed, .5d);		// actually, I decided to take the square root, sort of splitting the difference between linear and extreme

                //double darkAlpha = UtilityHelper.GetScaledValue_Capped(45, 192, 0, 500, speed);
                //double darkAlpha = UtilityHelper.GetScaledValue_Capped(45, 192, 0, 7.9370052d, cubeRoot);
                double darkAlpha = UtilityCore.GetScaledValue_Capped(45, 192, 0, 22.3606798d, cubeRoot);
                double lightAlpha = darkAlpha / 1.4d;

                return new LinearGradientBrush(Color.FromArgb(Convert.ToByte(darkAlpha), 0, 0, 0), Color.FromArgb(Convert.ToByte(lightAlpha), 0, 0, 0), 90);
            }

            #endregion
        }

        #endregion

        #region Events

        /// <summary>
        /// This is raised when the control needs to scroll (similiar in concept to a scrollbar's scroll event, or the mousewheel event)
        /// </summary>
        public event AutoScrollHandler AutoScroll = null;

        /// <summary>
        /// This is raised when the control needs to change the mouse cursor.  Use this.SuggestedCursor to get the cursor
        /// </summary>
        public event EventHandler CursorChanged = null;

        #endregion

        #region Declaration Section

        private const double NEARZERO = .15d;		// 15% pixel per tick

        /// <summary>
        /// This is active between calls to this.StartAutoScroll and this.StopAutoScroll
        /// </summary>
        private DispatcherTimer _timer = null;

        /// <summary>
        /// The point passed into this.StartAutoScroll
        /// </summary>
        private Point _initialPoint = new Point(0, 0);
        /// <summary>
        /// The point passed into this.MouseMove
        /// </summary>
        private Point _currentPoint = new Point(0, 0);

        // When the distance between current and initial is <= these values, there is no movement
        private double _deadDistX = 10d;
        private double _deadDistY = 10d;		// there isn't much point in having the y be different than x, but the capability is there

        // This is part of the x cubed function.  The smaller the number, the higher the acceleration
        private double _accelX = 40d;
        private double _accelY = 40d;

        // This is how many pixels to move between the last update event.  They are double to support fractions of pixels per tick.  The
        // update event is integer, but this provides a much more accurate scroll rate (noticable at very slow speeds)
        private double _xDelta = 0d;
        private double _yDelta = 0d;

        /// <summary>
        /// This is the current suggested mouse cursor
        /// </summary>
        private Cursor _cursor = Cursors.Arrow;

        /// <summary>
        /// This is the image that should be drawn at this.InitialPoint_Offset
        /// </summary>
        /// <remarks>
        /// I have this in a wrapper class, so I can manipulate the arrows while they are mousing around
        /// </remarks>
        private CenterImage _centerImage = null;

        /// <summary>
        /// The time of the last occurance of Timer_Tick.  This is used to know how much actual time has elapsed between ticks,
        /// which is used to keep the scroll distance output normalized to time instead of ticks (in cases with low FPS)
        /// </summary>
        private DateTime _lastTick = DateTime.UtcNow;

        #endregion

        #region Constructor

        /// <summary>
        /// This class will add a popup control to the panel passed in.  The popup shouldn't affect any other controls in that panel.
        /// </summary>
        public AutoScrollEmulator()
        {
            // Set up the timer
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(10);
            _timer.Tick += new EventHandler(Timer_Tick);

            // Set up the center image
            _centerImage = new CenterImage();
        }

        #endregion

        #region Public Properties

        public double DeadDistanceX
        {
            get
            {
                return _deadDistX;
            }
            set
            {
                _deadDistX = value;
            }
        }
        public double DeadDistanceY
        {
            get
            {
                return _deadDistY;
            }
            set
            {
                _deadDistY = value;
            }
        }

        public double AccelX
        {
            get
            {
                return _accelX;
            }
            set
            {
                _accelX = value;
            }
        }
        public double AccelY
        {
            get
            {
                return _accelY;
            }
            set
            {
                _accelY = value;
            }
        }

        /// <summary>
        /// This is the mouse cursor that controls should use while autoscrolling (listen to this.CursorChanged event)
        /// </summary>
        public Cursor SuggestedCursor
        {
            get
            {
                return _cursor;
            }
        }

        /// <summary>
        /// This tells whether the class is currently autoscrolling
        /// </summary>
        public bool IsAutoScrollActive
        {
            get
            {
                return _timer.IsEnabled;
            }
        }

        /// <summary>
        /// This is the point that is the center of the autoscroll
        /// NOTE:  This only has meaning when IsAutoScrollActive is true
        /// </summary>
        public Point InitialPoint
        {
            get
            {
                return _initialPoint;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Call this when the user initiates an autoscroll (middle mouse down)
        /// </summary>
        public void StartAutoScroll(Point mouseDownPoint, Point mouseDownPointScreen)
        {
            _initialPoint = mouseDownPoint;
            _currentPoint = mouseDownPoint;

            _xDelta = 0d;
            _yDelta = 0d;

            _centerImage.ResetSpeed();

            Point placePoint = _initialPoint;
            //_centerImage.Image.RenderTransform = new TranslateTransform(placePoint.X, placePoint.Y);
            _centerImage.ShowCenterImage(mouseDownPointScreen);

            _lastTick = DateTime.UtcNow;
            _timer.Start();
        }

        /// <summary>
        /// Call this when the user finishes an autoscroll (middle mouse up)
        /// </summary>
        public void StopAutoScroll()
        {
            _timer.Stop();

            _centerImage.ResetSpeed();
            _centerImage.HideCenterImage();

            //TODO:  remember the cursor that was being used before, and/or request the current cursor to use
            ApplyCursor(Cursors.Arrow);
        }

        /// <summary>
        /// Call this whenever the mouse moves
        /// </summary>
        /// <remarks>
        /// It is safe to call this method even when not in autoscroll mode
        /// </remarks>
        public void MouseMove(Point mousePosition)
        {
            _currentPoint = mousePosition;
        }

        #endregion
        #region Protected Methods

        protected virtual void OnAutoScroll(double xDelta, double yDelta)
        {
            // Set the arrow color
            _centerImage.SetSpeed(xDelta, yDelta);

            if (this.AutoScroll != null)
            {
                this.AutoScroll(this, new AutoScrollArgs(xDelta, yDelta));
            }
        }

        protected virtual void OnCursorChanged()
        {
            if (this.CursorChanged != null)
            {
                this.CursorChanged(this, new EventArgs());
            }
        }

        #endregion

        #region Event Listeners

        private void Timer_Tick(object sender, EventArgs e)
        {
            // Account for slow machines
            double elapsedTime = (DateTime.UtcNow - _lastTick).TotalMilliseconds;
            double timeStretch = elapsedTime / _timer.Interval.TotalMilliseconds;

            // Calculate Delta
            double xSpeed = GetDelta(_currentPoint.X, _initialPoint.X, _deadDistX, _accelX, timeStretch);
            double ySpeed = GetDelta(_currentPoint.Y, _initialPoint.Y, _deadDistY, _accelY, timeStretch);

            _xDelta += xSpeed;
            _yDelta += ySpeed;

            // Figure out the mouse cursor
            ApplyCursor(GetActiveCursor(xSpeed, ySpeed));		// I have to pass it speed, and not delta.  If I pass it delta then the cursor will alternate between no movement and movement as the delta exceeds the threshold (but speed would continously stay under that threshold) (this only matters when the speed is less than threshold (NEARZERO))

            // Raise scroll event
            #region Double

            // Remember the current delta
            double xDelta = _xDelta;
            double yDelta = _yDelta;

            // Set delta to zero before raising the event (this probably isn't nessassary, but I want to be completely
            // consistent with the way the integer mode works)
            _xDelta = 0d;
            _yDelta = 0d;

            // Raise event
            OnAutoScroll(xDelta, yDelta);

            #endregion

            // Remember the current time
            _lastTick = DateTime.UtcNow;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// This function converts the distance between the current and initial into a scroll delta
        /// </summary>
        private double GetDelta(double current, double initial, double deadDistance, double accel, double timeStretch)
        {
            // See if it's in the dead zone
            if (Math.Abs(current - initial) <= deadDistance)
            {
                return 0d;
            }

            double distance = current - initial;

            // First part is just integer division (speed increases as distance from center increases)
            //double retVal = distance / deadDistance;
            double retVal = 0d;		// I don't like this linear part (too fast, too soon)

            #region Second part is an acceleration factor

            // This second part adds an X cubed to the return value.  This provides a non linear increase in speed based on
            // distance from center.

            // Start at zero distance when outside the dead zone (otherwise, it's already accelerating too fast)
            double distanceBeyondDead = distance;

            if (distance > 0)
            {
                distanceBeyondDead -= deadDistance;
            }
            else
            {
                distanceBeyondDead += deadDistance;
            }

            // Add Distance Cubed
            retVal += Math.Pow(distanceBeyondDead / accel, 3d);		//NOTE:  the power can't be even, because I need negative values too

            #endregion

            // If the timer is ticking irregularly, this will keep the scrolling regular
            retVal *= timeStretch;

            // Exit Function
            return retVal;
        }

        private static Cursor GetActiveCursor(double deltaX, double deltaY)
        {
            if (IsEQZero(deltaX) && IsEQZero(deltaY))
            {
                return Cursors.ScrollAll;
            }
            else if (IsGTZero(deltaX) && IsEQZero(deltaY))
            {
                return Cursors.ScrollE;
            }
            else if (IsGTZero(deltaX) && IsGTZero(deltaY))
            {
                return Cursors.ScrollSE;
            }
            else if (IsEQZero(deltaX) && IsGTZero(deltaY))
            {
                return Cursors.ScrollS;
            }
            else if (IsLTZero(deltaX) && IsGTZero(deltaY))
            {
                return Cursors.ScrollSW;
            }
            else if (IsLTZero(deltaX) && IsEQZero(deltaY))
            {
                return Cursors.ScrollW;
            }
            else if (IsLTZero(deltaX) && IsLTZero(deltaY))
            {
                return Cursors.ScrollNW;
            }
            else if (IsEQZero(deltaX) && IsLTZero(deltaY))
            {
                return Cursors.ScrollN;
            }
            else if (IsGTZero(deltaX) && IsLTZero(deltaY))
            {
                return Cursors.ScrollNE;
            }

            throw new ApplicationException("Execution shouldn't get here");
        }
        private static bool IsEQZero(double testValue)
        {
            return Math.Abs(testValue) <= NEARZERO;
        }
        private static bool IsLTZero(double testValue)
        {
            return testValue < 0d - NEARZERO;
        }
        private static bool IsGTZero(double testValue)
        {
            return testValue > NEARZERO;
        }

        /// <summary>
        /// This function will compare the current cursor with the one passed in.  If there is a change, then the event will be raised
        /// </summary>
        private void ApplyCursor(Cursor cursor)
        {
            if (cursor != _cursor)
            {
                _cursor = cursor;
                OnCursorChanged();
            }
        }

        #endregion
    }

    #region AutoScroll Delegate/Args

    public delegate void AutoScrollHandler(object sender, AutoScrollArgs e);

    public class AutoScrollArgs : EventArgs
    {
        #region Declaration Section

        private double _xDelta = 0d;
        private double _yDelta = 0d;

        #endregion

        #region Constructor

        public AutoScrollArgs(double xDelta, double yDelta)
        {
            _xDelta = xDelta;
            _yDelta = yDelta;
        }

        #endregion

        #region Public Properties

        public double XDelta
        {
            get
            {
                return _xDelta;
            }
        }
        public double YDelta
        {
            get
            {
                return _yDelta;
            }
        }

        #endregion
    }

    #endregion
}
