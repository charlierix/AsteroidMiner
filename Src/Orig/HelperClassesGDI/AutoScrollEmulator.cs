using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Windows.Forms;

namespace Game.Orig.HelperClassesGDI
{
    /// <summary>
    /// This class will emulate an autoscroll.  It is meant for controls that implement their own scrolling (not derived
    /// from ScrollableControl)
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
        private Timer _timer = null;

        /// <summary>
        /// The point passed into this.StartAutoScroll
        /// </summary>
        private Point _initialPoint = new Point(0, 0);
        /// <summary>
        /// The point passed into this.MouseMove
        /// </summary>
        private Point _currentPoint = new Point(0, 0);

        // When the distance between current and initial is <= these values, there is no movement
        private int _deadDistX = 10;
        private int _deadDistY = 10;		// there isn't much point in having the y be different than x, but the capability is there

        // This is part of the x squared function.  The smaller the number, the higher the acceleration
        private double _accelX = 40d;
        private double _accelY = 40d;

        /// <summary>
        /// This is whether the AutoScroll event only exposes whole integer values or whether it exposes double values (partial pixel)
        /// </summary>
        private bool _isIntegerMode = true;

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
        /// This stays null until the first request, and is then cached forever
        /// </remarks>
        private Bitmap _centerIconImage = null;
        /// <summary>
        /// This is the size of _centerIconImage divided by 2
        /// </summary>
        private Size _centerIconOffset = new Size(0, 0);

        /// <summary>
        /// The Environment.TickCount of the last occurance of Timer_Tick.  This is used to know how much actual
        /// time has elapsed between ticks, which is used to keep the scroll distance output normalized to time instead
        /// of ticks (in cases with low FPS)
        /// </summary>
        private int _lastTick = 0;

        #endregion

        #region Constructor

        public AutoScrollEmulator()
        {
            // Set up the timer
            _timer = new Timer();
            _timer.Interval = 10;
            _timer.Enabled = false;
            _timer.Tick += new EventHandler(Timer_Tick);
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// True: AutoScroll event exposes whole integer values
        /// False: AutoScroll event exposes double values
        /// </summary>
        /// <remarks>
        /// The scroll speed is always tracked as a double internally, so this only affects the precision of the
        /// output (if in integer mode, when the event is raised, the decimal portion is retained internally,
        /// so no information is lost)
        /// 
        /// This should only be set to false if you are implementing something that supports zooming in
        /// </remarks>
        public bool IsIntegerMode
        {
            get
            {
                return _isIntegerMode;
            }
            set
            {
                _isIntegerMode = value;
            }
        }

        public int DeadDistanceX
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
        public int DeadDistanceY
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
        /// This is the mouse cursor that control should use while autoscrolling (listen to this.CursorChanged event)
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
                return _timer.Enabled;
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

        /// <summary>
        /// When drawing this.CenterIconImage, this is the location to pass into Graphics.DrawImageUnscaled so that
        /// the image is centered over this.InitialPoint
        /// </summary>
        public Point InitialPoint_Offset
        {
            get
            {
                // Make sure the image is built
                if (_centerIconImage == null)
                {
                    Image image = this.CenterIconImage;
                }

                // Exit Function
                return new Point(_initialPoint.X - _centerIconOffset.Width, _initialPoint.Y - _centerIconOffset.Height);
            }
        }

        /// <summary>
        /// This is the image that should be drawn at this.InitialPoint_Offset
        /// </summary>
        /// <remarks>
        /// There doesn't seem to be much of a standard for the autoscroll icon.  A lot of apps use Cursors.NoMove2D, which is what
        /// I'm using (slightly transparent).  IE uses a different icon completely.  I haven't played with Vista yet.
        /// </remarks>
        public Image CenterIconImage
        {
            get
            {
                if (_centerIconImage == null)
                {
                    #region Draw Image

                    // In order to get the 50% opacity, I have to use a temp bitmap (because the cursors draw themselves)
                    Bitmap tempBitmap = new Bitmap(Cursors.NoMove2D.Size.Width, Cursors.NoMove2D.Size.Height);
                    using (Graphics graphics = Graphics.FromImage(tempBitmap))
                    {
                        graphics.Clear(Color.Transparent);

                        Cursors.NoMove2D.Draw(graphics, new Rectangle(0, 0, tempBitmap.Width, tempBitmap.Height));
                    }

                    // Now blit the temp image onto the real image at 50% opacity
                    _centerIconImage = new Bitmap(tempBitmap.Width, tempBitmap.Height);
                    using (Graphics graphics = Graphics.FromImage(_centerIconImage))
                    {
                        graphics.Clear(Color.Transparent);

                        // Create a new color matrix and set the alpha value to 0.5
                        ColorMatrix matrix = new ColorMatrix();
                        matrix.Matrix00 = matrix.Matrix11 = matrix.Matrix22 = matrix.Matrix44 = 1f;
                        matrix.Matrix33 = 0.4f;		// I like 40% better

                        // Create a new image attribute object and set the color matrix to the one just created
                        ImageAttributes imageAttrib = new ImageAttributes();
                        imageAttrib.SetColorMatrix(matrix);

                        // Blit the image at 50%
                        graphics.DrawImage(tempBitmap,
                            new Rectangle(0, 0, tempBitmap.Width, tempBitmap.Height),
                            0, 0, tempBitmap.Width, tempBitmap.Height, GraphicsUnit.Pixel,
                            imageAttrib);
                    }

                    #endregion

                    _centerIconOffset = new Size(_centerIconImage.Width / 2, _centerIconImage.Height / 2);
                }

                return _centerIconImage;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Call this when the user initiates an autoscroll (middle mouse down)
        /// </summary>
        public void StartAutoScroll(Point mouseDownPoint)
        {
            _initialPoint = mouseDownPoint;
            _currentPoint = mouseDownPoint;

            _xDelta = 0d;
            _yDelta = 0d;

            _lastTick = Environment.TickCount;
            _timer.Enabled = true;
        }
        /// <summary>
        /// Call this when the user finishes an autoscroll (middle mouse up)
        /// </summary>
        public void StopAutoScroll()
        {
            _timer.Enabled = false;

            ApplyCursor(Cursors.Default);
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

        protected virtual void OnAutoScroll(int xDelta, int yDelta)
        {
            if (this.AutoScroll != null)
            {
                this.AutoScroll(this, new AutoScrollArgs(xDelta, yDelta));
            }
        }
        protected virtual void OnAutoScroll(double xDelta, double yDelta)
        {
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

        #region Event Handlers

        private void Timer_Tick(object sender, EventArgs e)
        {
            // Account for slow machines
            int elapsedTime = Environment.TickCount - _lastTick;
            double timeStretch = Convert.ToDouble(elapsedTime) / Convert.ToDouble(_timer.Interval);

            // Calculate Delta
            double xSpeed = GetDelta(_currentPoint.X, _initialPoint.X, _deadDistX, _accelX, timeStretch);
            double ySpeed = GetDelta(_currentPoint.Y, _initialPoint.Y, _deadDistY, _accelY, timeStretch);

            _xDelta += xSpeed;
            _yDelta += ySpeed;

            // Figure out the mouse cursor
            ApplyCursor(GetActiveCursor(xSpeed, ySpeed));		// I have to pass it speed, and not delta.  If I pass it delta then the cursor will alternate between no movement and movement as the delta exceeds the threshold (but speed would continously stay under that threshold) (this only matters when the speed is less than threshold (NEARZERO))

            // Raise scroll event
            if (_isIntegerMode)
            {
                #region Integer

                // Make sure there is movement of at least 1 whole pixel (it may take several ticks to add up to a pixel)
                if (Math.Abs(_xDelta) >= 1d || Math.Abs(_yDelta) >= 1d)
                {
                    // Get the int equivalents
                    int xDeltaInt = Convert.ToInt32(Math.Truncate(_xDelta));
                    int yDeltaInt = Convert.ToInt32(Math.Truncate(_yDelta));

                    // Remove what I'm about to report (the int part)
                    _xDelta -= xDeltaInt;
                    _yDelta -= yDeltaInt;

                    // Raise event
                    OnAutoScroll(xDeltaInt, yDeltaInt);
                }

                #endregion
            }
            else
            {
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
            }

            // Remember the current time
            _lastTick = Environment.TickCount;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// This function converts the distance between the current and initial into a scroll delta
        /// </summary>
        private double GetDelta(int current, int initial, int deadDistance, double accel, double timeStretch)
        {
            // See if it's in the dead zone
            if (Math.Abs(current - initial) <= deadDistance)
            {
                return 0d;
            }

            int distance = current - initial;

            // First part is just integer division (speed increases as distance from center increases)
            //double retVal = distance / deadDistance;
            double retVal = 0d;		// I don't like this linear part (too fast, too soon)

            #region Second part is an acceleration factor

            // This second part adds an X squared to the return value.  This provides a non linear increase in speed based on
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

            // Add Distance Squared
            double power = Math.Pow(distanceBeyondDead / accel, 2d);
            if (distanceBeyondDead < 0)
            {
                retVal -= power;
            }
            else
            {
                retVal += power;
            }

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
                return Cursors.NoMove2D;
            }
            else if (IsGTZero(deltaX) && IsEQZero(deltaY))
            {
                return Cursors.PanEast;
            }
            else if (IsGTZero(deltaX) && IsGTZero(deltaY))
            {
                return Cursors.PanSE;
            }
            else if (IsEQZero(deltaX) && IsGTZero(deltaY))
            {
                return Cursors.PanSouth;
            }
            else if (IsLTZero(deltaX) && IsGTZero(deltaY))
            {
                return Cursors.PanSW;
            }
            else if (IsLTZero(deltaX) && IsEQZero(deltaY))
            {
                return Cursors.PanWest;
            }
            else if (IsLTZero(deltaX) && IsLTZero(deltaY))
            {
                return Cursors.PanNW;
            }
            else if (IsEQZero(deltaX) && IsLTZero(deltaY))
            {
                return Cursors.PanNorth;
            }
            else if (IsGTZero(deltaX) && IsLTZero(deltaY))
            {
                return Cursors.PanNE;
            }

            throw new ApplicationException("Execution shouldn't get here");
        }
        private static bool IsEQZero(double testValue)
        {
            return Math.Abs(testValue) < NEARZERO;
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

        private bool _isIntegerMode = true;

        private int _xDeltaInt = 0;
        private int _yDeltaInt = 0;

        private double _xDeltaDbl = 0d;
        private double _yDeltaDbl = 0d;

        #endregion

        #region Constructor

        public AutoScrollArgs(int xDelta, int yDelta)
        {
            _isIntegerMode = true;
            _xDeltaInt = xDelta;
            _yDeltaInt = yDelta;
        }
        public AutoScrollArgs(double xDelta, double yDelta)
        {
            _isIntegerMode = false;
            _xDeltaDbl = xDelta;
            _yDeltaDbl = yDelta;
        }

        #endregion

        #region Properties

        public bool IsIntegerMode
        {
            get
            {
                return _isIntegerMode;
            }
        }

        public int XDeltaInt
        {
            get
            {
                if (!_isIntegerMode)
                {
                    throw new InvalidOperationException("Cannot call an integer property when in double mode");
                }

                return _xDeltaInt;
            }
        }
        public int YDeltaInt
        {
            get
            {
                if (!_isIntegerMode)
                {
                    throw new InvalidOperationException("Cannot call an integer property when in double mode");
                }

                return _yDeltaInt;
            }
        }

        public double XDeltaDbl
        {
            get
            {
                if (_isIntegerMode)
                {
                    throw new InvalidOperationException("Cannot call a double property when in integer mode");
                }

                return _xDeltaDbl;
            }
        }
        public double YDeltaDbl
        {
            get
            {
                if (_isIntegerMode)
                {
                    throw new InvalidOperationException("Cannot call a double property when in integer mode");
                }

                return _yDeltaDbl;
            }
        }

        #endregion
    }

    #endregion
}
