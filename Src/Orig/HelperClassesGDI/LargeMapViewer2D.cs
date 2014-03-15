using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Data;
using System.Text;
using System.Windows.Forms;

using Game.Orig.Math3D;

namespace Game.Orig.HelperClassesGDI
{
    /// <summary>
    /// This class is used to view a large map.  It basically uses vector based graphics (the draw commands are in
    /// 100% zoom coords, but get drawn to the portion and zoom that the picturebox is currently viewing.  So
    /// no matter how far you zoom, circles and lines will appear clean (images would be scaled though)
    /// </summary>
    /// <remarks>
    /// At first, I was going to support two views:  Main and Map.  But as I was writing this, I realized that they each
    /// needed the same amount of control.  So if you need both a main and map view, intantiate two of me.
    /// </remarks>
    public partial class LargeMapViewer2D : PictureBox
    {
        #region Declaration Section

        // These tell me the limits of the universe
        private MyVector _boundryLower = new MyVector(-10000d, -10000d, 0);
        private MyVector _boundryUpper = new MyVector(10000d, 10000d, 0);

        /// <summary>
        /// This is the point that the view will be centered on (It is either actively controlled from the outside, or played
        /// with from me based on panning functions)
        /// </summary>
        private MyVector _centerPoint = null;

        /// <summary>
        /// This is the zoom factor (1 is 100%, 2 is 200%, .5 is 50%)
        /// </summary>
        private double _zoom = 1d;

        /// <summary>
        /// This is only set when the picturebox is resized.  This will force a rebuild of the background bitmaps
        /// on the next frame
        /// </summary>
        private bool _isResized = true;

        // Offscreen buffer (being drawn to)
        private bool _drawingScene = false;
        private Bitmap _bitmap = null;
        private Graphics _graphics = null;

        // Border
        private bool _shouldDrawBorder = false;
        private Color _borderColor = Color.White;
        private double _borderWidth = 1d;

        // Checker Background
        //TODO: If more backgrounds are added, change this to an enum
        private bool _shouldDrawCheckerBackground = false;
        private Color _checkerOtherColor = Color.White;
        private int _numCheckersPerSide = 10;

        /// <summary>
        /// This is used if they do a draw and request a selection outline
        /// </summary>
        private Pen _selectionPen = new Pen(new HatchBrush(HatchStyle.Percent25, Color.White, Color.Gray), 8f);

        /// <summary>
        /// This is which button will do panning when the user drags the scene.
        /// </summary>
        private MouseButtons _panningMouseButton = MouseButtons.Left;
        private Point _panningLastPoint;

        private bool _shouldAutoScroll = true;
        private AutoScrollEmulator _autoscroll = null;

        private bool _shouldZoomOnMouseWheel = true;

        #endregion

        #region Constructor

        public LargeMapViewer2D()
        {
            InitializeComponent();

            // Default to viewing everything
            _centerPoint = new MyVector();
            ZoomFit();

            // AutoScroll Emulator
            _autoscroll = new AutoScrollEmulator();
            _autoscroll.AutoScroll += new AutoScrollHandler(Autoscroll_AutoScroll);
            _autoscroll.CursorChanged += new EventHandler(Autoscroll_CursorChanged);
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// This is which button will do panning when the user drags the scene.
        /// </summary>
        [Description("This is which button will do panning when the user drags the scene."),
        DefaultValue(MouseButtons.Left)]
        public MouseButtons PanMouseButton
        {
            get
            {
                return _panningMouseButton;
            }
            set
            {
                _panningMouseButton = value;
            }
        }

        [Description("Controls whether middle mouse drag will autoscroll the scene.  NOTE:  There will not be any scrollbars"),
        DefaultValue(true)]
        public bool AutoScroll
        {
            get
            {
                return _shouldAutoScroll;
            }
            set
            {
                _shouldAutoScroll = value;
            }
        }

        [Description("This is whether the mouse wheel will affect the zoom"),
        DefaultValue(true)]
        public bool ZoomOnMouseWheel
        {
            get
            {
                return _shouldZoomOnMouseWheel;
            }
            set
            {
                _shouldZoomOnMouseWheel = value;
            }
        }

        // These are all gets, and are meant to assist with cloning
        public MyVector BoundryLower
        {
            get
            {
                return _boundryLower;
            }
        }
        public MyVector BoundryUpper
        {
            get
            {
                return _boundryUpper;
            }
        }

        public bool ShouldDrawBorder
        {
            get
            {
                return _shouldDrawBorder;
            }
        }
        public Color BorderColor
        {
            get
            {
                return _borderColor;
            }
        }
        public double BorderWidth
        {
            get
            {
                return _borderWidth;
            }
        }

        public bool ShouldDrawCheckerBackground
        {
            get
            {
                return _shouldDrawCheckerBackground;
            }
        }
        public Color CheckerOtherColor
        {
            get
            {
                return _checkerOtherColor;
            }
        }
        public int NumCheckersPerSide
        {
            get
            {
                return _numCheckersPerSide;
            }
        }

        #endregion

        #region Public Methods (Environment Setup)

        public void SetBorder(MyVector boundryLower, MyVector boundryUpper)
        {
            _boundryLower = boundryLower;
            _boundryUpper = boundryUpper;
        }

        public void ShowBorder(Color color, double lineWidth)
        {
            _shouldDrawBorder = true;
            _borderColor = color;
            _borderWidth = lineWidth;
        }
        public void HideBorder()
        {
            _shouldDrawBorder = false;
        }

        public void ShowCheckerBackground(Color otherColor, int numSquaresPerSide)
        {
            _shouldDrawCheckerBackground = true;
            _checkerOtherColor = otherColor;
            _numCheckersPerSide = numSquaresPerSide;
        }
        /// <summary>
        /// The screen will just be filled with the picturebox's background color
        /// </summary>
        public void HideBackground()
        {
            _shouldDrawCheckerBackground = false;
        }

        #endregion
        #region Public Methods (Camera Control)

        /// <summary>
        /// This function will keep the main view centered on the point you pass in (it is most likely a pointer to
        /// the position of some radar blip).  If the point moves, I will stay centered on it.
        /// </summary>
        public void ChasePoint(MyVector point)
        {
            _centerPoint = point;
        }

        /// <summary>
        /// This function tells me to use the last known position of pclsCenterPoint, and stare at that instead of actively
        /// tracking something
        /// </summary>
        public void StaticPoint()
        {
            _centerPoint = _centerPoint.Clone();
        }
        /// <summary>
        /// This function tells me to look at the point passed in, but keep looking at where it is now, not actively track it.
        /// I will store the clone of it, so you don't have to make a clone before you pass me the pointer to a position
        /// </summary>
        public void StaticPoint(MyVector point)
        {
            _centerPoint = point.Clone();
        }

        /// <summary>
        /// This will pan my view by the offset passed in.
        /// I will be in static view mode after the function call
        /// </summary>
        public void PanView(MyVector offset, bool shouldTransformViewToWorld, bool enforceBoundryCap)
        {
            // Put myself into static mode
            StaticPoint();

            // Either add what was passed in directly, or convert it from view coords to world coords
            if (shouldTransformViewToWorld)
            {
                _centerPoint.X += GetDistanceViewToWorld(offset.X);
                _centerPoint.Y += GetDistanceViewToWorld(offset.Y);
            }
            else
            {
                _centerPoint.Add(offset);
            }

            if (enforceBoundryCap)
            {
                #region Boundry Check

                //double scaledWidth = (_boundryUpper.X - _boundryLower.X) - (this.Width / _zoom);
                //double scaledHeight = (_boundryUpper.Y - _boundryLower.Y) - (this.Height / _zoom);
                double scaledWidth = this.Width / _zoom;
                double scaledHeight = this.Height / _zoom;

                // Check left edge
                //if (_centerPoint.X - _boundryLower.X > scaledWidth)
                //{
                //    _centerPoint.X = scaledWidth + _boundryLower.X;
                //}

                //// Check top edge
                //if (_centerPoint.Y - _boundryLower.Y > scaledHeight)
                //{
                //    _centerPoint.Y = scaledHeight + _boundryLower.Y;
                //}

                // Check right edge
                //if (_centerPoint.X + _srcRect.Width < 0)
                //{
                //    _centerPoint.X = 0 - _srcRect.Width;
                //}

                //// Check bottom edge
                //if (_centerPoint.Y + _srcRect.Height < 0)
                //{
                //    _centerPoint.Y = 0 - _srcRect.Height;
                //}

                #endregion
            }
        }

        /// <summary>
        /// This function sets the zoom to whatever is passed in
        /// </summary>
        /// <param name="zoom">
        /// 1 is 100%
        /// 2 is 200%
        /// .5 is 50%
        /// </param>
        public void ZoomSet(double zoom)
        {
            _zoom = zoom;
        }

        /// <summary>
        /// If the delta is positive, then I will multiply the zoom by 1 + delta.
        /// If it is negative, I will divide by 1 + abs(delta)
        /// That way you can pass in .1 or -.1 and expect the same ratio of zooming to be used (run the calculation in excel, it helps)
        /// </summary>
        public void ZoomRelative(double zoomDelta)
        {
            if (zoomDelta > 0)
            {
                _zoom *= 1d + zoomDelta;
            }
            else
            {
                _zoom /= 1d + Math.Abs(zoomDelta);
            }
        }

        public void ZoomFit()
        {
            ZoomRectangle(_boundryLower, _boundryUpper);
        }

        /// <summary>
        /// This function will set the center position to the center of the rectangle passed in, and set the zoom so that I can
        /// see everything inside the rectangle
        /// </summary>
        /// <remarks>
        /// I will be in static mode after this call (as opposed to chase mode)
        /// </remarks>
        public void ZoomRectangle(MyVector cornerLower, MyVector cornerUpper)
        {
            // Figure out the position.  By setting it to a new vector, I've gone into static mode
            _centerPoint = cornerLower + cornerUpper;
            _centerPoint.Divide(2);

            // Set temp X and Y to the zoom rectangle width and height
            double tempX = cornerUpper.X - cornerLower.X;
            double tempY = cornerUpper.Y - cornerLower.Y;

            // Now set temp X and Y to the corresponding zoom factors
            tempX = this.Width / tempX;
            tempY = this.Height / tempY;

            // Store the lower of the two
            if (tempX < tempY)
            {
                _zoom = tempX;
            }
            else
            {
                _zoom = tempY;
            }
        }

        #endregion
        #region Public Methods (Draw - Start/Stop Frame)

        /// <summary>
        /// Once per frame, call this before drawing
        /// </summary>
        public void PrepareForNewDraw()
        {
            _drawingScene = true;

            if (_isResized)  // resized will be true the first time this method is called
            {
                #region Create New _bitmapCurrentlyDrawingTo

                // Wipe previous if needed
                if (_bitmap != null && (_bitmap.Width != this.Width || _bitmap.Height != this.Height))
                {
                    _bitmap.Dispose();
                    _bitmap = null;

                    _graphics.Dispose();
                    _graphics = null;
                }

                // Build new if needed
                if (_bitmap == null)
                {
                    if (this.DisplayRectangle.Width <= 0 || this.DisplayRectangle.Height <= 0)
                    {
                        // The control is minimized.  Just build a tiny bitmap (because I don't want a ton of if null checks on
                        // _graphics in most of my functions)
                        _bitmap = new Bitmap(10, 10);
                    }
                    else
                    {
                        // Make a bitmap the same size as the control
                        _bitmap = new Bitmap(this.DisplayRectangle.Width, this.DisplayRectangle.Height);
                    }

                    // Build graphics class
                    _graphics = Graphics.FromImage(_bitmap);
                }

                #endregion
            }

            // Clear the image
            _graphics.Clear(this.BackColor);

            #region Draw Background

            if (_shouldDrawCheckerBackground)
            {
                DrawCheckers();
            }

            if (_shouldDrawBorder)
            {
                DrawRectangle(_borderColor, _borderWidth, _boundryLower, _boundryUpper);
            }

            #endregion
        }

        /// <summary>
        /// Once per frame, call this when you are done drawing.  The image will be blitted
        /// </summary>
        public void FinishedDrawing()
        {
            _drawingScene = false;

            // This is kind of a crappy way of drawing, but it's faster than letting OnPaint doing the drawing
            using (Graphics graphics = this.CreateGraphics())
            {
                graphics.DrawImageUnscaled(_bitmap, 0, 0);

                if (_autoscroll.IsAutoScrollActive)
                {
                    graphics.DrawImageUnscaled(_autoscroll.CenterIconImage, _autoscroll.InitialPoint_Offset);
                }
            }
        }

        #endregion
        #region Public Methods (Draw Objects - these transform the coords passed in from world to view)

        public void DrawCircle(Color penColor, double penWidth, MyVector centerPoint, double radius)
        {
            try
            {
                float widthAndHeight = DistWToV(radius * 2d);
                using (Pen pen = new Pen(penColor, DistWToV(penWidth)))
                {
                    _graphics.DrawEllipse(pen,
                        PosWToV_X(centerPoint.X - radius), PosWToV_Y(centerPoint.Y - radius),
                        widthAndHeight, widthAndHeight);
                }
            }
            catch (OverflowException)
            {
                // Oh well
            }
        }
        public void DrawCircle_Selected(MyVector centerPoint, double radius)
        {
            try
            {
                float widthAndHeight = DistWToV(radius * 2d);
                _graphics.DrawEllipse(_selectionPen,
                    PosWToV_X(centerPoint.X - radius), PosWToV_Y(centerPoint.Y - radius),
                    widthAndHeight, widthAndHeight);
            }
            catch (OverflowException)
            {
                // Oh well
            }
        }
        public void DrawRectangle(Color penColor, double penWidth, MyVector lowerCorner, MyVector upperCorner)
        {
            try
            {
                using (Pen pen = new Pen(penColor, DistWToV(penWidth)))
                {
                    _graphics.DrawRectangle(pen,
                        PosWToV_X(lowerCorner.X), PosWToV_Y(lowerCorner.Y),
                        DistWToV(upperCorner.X - lowerCorner.X), DistWToV(upperCorner.Y - lowerCorner.Y));
                }
            }
            catch (OverflowException)
            {
                // Oh well
            }
        }
        public void DrawRectangle_Selected(MyVector lowerCorner, MyVector upperCorner)
        {
            try
            {
                _graphics.DrawRectangle(_selectionPen,
                    PosWToV_X(lowerCorner.X), PosWToV_Y(lowerCorner.Y),
                    DistWToV(upperCorner.X - lowerCorner.X), DistWToV(upperCorner.Y - lowerCorner.Y));
            }
            catch (OverflowException)
            {
                // Oh well
            }
        }
        public void DrawRectangle(Color penColor, DashStyle dashStyle, double penWidth, MyVector lowerCorner, MyVector upperCorner)
        {
            try
            {
                using (Pen pen = new Pen(penColor, DistWToV(penWidth)))
                {
                    pen.DashStyle = dashStyle;

                    _graphics.DrawRectangle(pen,
                        PosWToV_X(lowerCorner.X), PosWToV_Y(lowerCorner.Y),
                        DistWToV(upperCorner.X - lowerCorner.X), DistWToV(upperCorner.Y - lowerCorner.Y));
                }
            }
            catch (OverflowException)
            {
                // Oh well
            }
        }
        public void DrawLine(Color penColor, double penWidth, MyVector from, MyVector to)
        {
            try
            {
                using (Pen pen = new Pen(penColor, DistWToV(penWidth)))
                {
                    _graphics.DrawLine(pen,
                        PosWToV_X(from.X), PosWToV_Y(from.Y),
                        PosWToV_X(to.X), PosWToV_Y(to.Y));
                }
            }
            catch (OverflowException)
            {
                // Oh well
            }
        }
        public void DrawLine_Selected(MyVector from, MyVector to)
        {
            try
            {
                _graphics.DrawLine(_selectionPen,
                    PosWToV_X(from.X), PosWToV_Y(from.Y),
                    PosWToV_X(to.X), PosWToV_Y(to.Y));
            }
            catch (OverflowException)
            {
                // Oh well
            }
        }
        public void DrawArrow(Color penColor, double penWidth, MyVector from, MyVector to)
        {
            try
            {
                using (Pen pen = new Pen(penColor, DistWToV(penWidth)))
                {
                    float arrowSize = Convert.ToSingle(penWidth * 3d);		// GDI seems to scale for me, so I don't need to call DistWToV
                    pen.CustomEndCap = new System.Drawing.Drawing2D.AdjustableArrowCap(arrowSize, arrowSize, true);

                    _graphics.DrawLine(pen,
                        PosWToV_X(from.X), PosWToV_Y(from.Y),
                        PosWToV_X(to.X), PosWToV_Y(to.Y));
                }
            }
            catch (OverflowException)
            {
                // Oh well
            }
        }
        public void DrawTriangle(Color penColor, double penWidth, MyVector point1, MyVector point2, MyVector point3)
        {
            try
            {
                using (Pen pen = new Pen(penColor, DistWToV(penWidth)))
                {
                    float x1 = PosWToV_X(point1.X);
                    float y1 = PosWToV_Y(point1.Y);
                    float x2 = PosWToV_X(point2.X);
                    float y2 = PosWToV_Y(point2.Y);
                    float x3 = PosWToV_X(point3.X);
                    float y3 = PosWToV_Y(point3.Y);

                    _graphics.DrawLine(pen, x1, y1, x2, y2);
                    _graphics.DrawLine(pen, x2, y2, x3, y3);
                    _graphics.DrawLine(pen, x3, y3, x1, y1);
                }
            }
            catch (OverflowException)
            {
                // Oh well
            }
        }
        public void DrawTriangle_Selected(MyVector point1, MyVector point2, MyVector point3)
        {
            try
            {
                float x1 = PosWToV_X(point1.X);
                float y1 = PosWToV_Y(point1.Y);
                float x2 = PosWToV_X(point2.X);
                float y2 = PosWToV_Y(point2.Y);
                float x3 = PosWToV_X(point3.X);
                float y3 = PosWToV_Y(point3.Y);

                _graphics.DrawLine(_selectionPen, x1, y1, x2, y2);
                _graphics.DrawLine(_selectionPen, x2, y2, x3, y3);
                _graphics.DrawLine(_selectionPen, x3, y3, x1, y1);
            }
            catch (OverflowException)
            {
                // Oh well
            }
        }
        public void DrawArc(Color penColor, double penWidth, MyVector[] points, bool closed)
        {
            try
            {
                using (Pen pen = new Pen(penColor, DistWToV(penWidth)))
                {
                    // Transform the points to view coords
                    PointF[] scaledPoints = new PointF[points.Length];
                    for (int cntr = 0; cntr < points.Length; cntr++)
                    {
                        scaledPoints[cntr] = new PointF(PosWToV_X(points[cntr].X), PosWToV_Y(points[cntr].Y));
                    }

                    // Draw the curve
                    if (closed)
                    {
                        _graphics.DrawClosedCurve(pen, scaledPoints);
                    }
                    else
                    {
                        _graphics.DrawCurve(pen, scaledPoints);
                    }
                }
            }
            catch (OverflowException)
            {
                // Oh well
            }
        }
        public void DrawArc_Selected(MyVector[] points, bool closed)
        {
            try
            {
                // Transform the points to view coords
                PointF[] scaledPoints = new PointF[points.Length];
                for (int cntr = 0; cntr < points.Length; cntr++)
                {
                    scaledPoints[cntr] = new PointF(PosWToV_X(points[cntr].X), PosWToV_Y(points[cntr].Y));
                }

                // Draw the curve
                if (closed)
                {
                    _graphics.DrawClosedCurve(_selectionPen, scaledPoints);
                }
                else
                {
                    _graphics.DrawCurve(_selectionPen, scaledPoints);
                }
            }
            catch (OverflowException)
            {
                // Oh well
            }
        }

        public void DrawString(string message, Font font, Brush brush, MyVector position, ContentAlignment textAlign)
        {
            try
            {
                if (textAlign == ContentAlignment.TopLeft)
                {
                    _graphics.DrawString(message, font, brush, PosWToV_X(position.X), PosWToV_Y(position.Y));
                }
                else
                {
                    SizeF textSize = _graphics.MeasureString(message, font);
                    float worldX = PosWToV_X(position.X);
                    float worldY = PosWToV_Y(position.Y);

                    switch (textAlign)
                    {
                        case ContentAlignment.TopCenter:
                            _graphics.DrawString(message, font, brush, worldX - (textSize.Width / 2f), worldY);
                            break;

                        case ContentAlignment.TopRight:
                            _graphics.DrawString(message, font, brush, worldX - textSize.Width, worldY);
                            break;

                        case ContentAlignment.MiddleLeft:
                            _graphics.DrawString(message, font, brush, worldX, worldY - (textSize.Height / 2f));
                            break;

                        case ContentAlignment.MiddleCenter:
                            _graphics.DrawString(message, font, brush, worldX - (textSize.Width / 2f), worldY - (textSize.Height / 2f));
                            break;

                        case ContentAlignment.MiddleRight:
                            _graphics.DrawString(message, font, brush, worldX - textSize.Width, worldY - (textSize.Height / 2f));
                            break;

                        case ContentAlignment.BottomLeft:
                            _graphics.DrawString(message, font, brush, worldX, worldY - textSize.Height);
                            break;

                        case ContentAlignment.BottomCenter:
                            _graphics.DrawString(message, font, brush, worldX - (textSize.Width / 2f), worldY - textSize.Height);
                            break;

                        case ContentAlignment.BottomRight:
                            _graphics.DrawString(message, font, brush, worldX - textSize.Width, worldY - textSize.Height);
                            break;

                        default:
                            // Oh well
                            return;
                    }
                }

            }
            catch (OverflowException)
            {
                // Oh well
            }
        }

        public void FillCircle(Color color, MyVector centerPoint, double radius)
        {
            using (SolidBrush brush = new SolidBrush(color))
            {
                FillCircle(brush, centerPoint, radius);
            }
        }
        public void FillCircle(Brush brush, MyVector centerPoint, double radius)
        {
            try
            {
                float widthAndHeight = DistWToV(radius * 2d);

                _graphics.FillEllipse(brush,
                    PosWToV_X(centerPoint.X - radius), PosWToV_Y(centerPoint.Y - radius),
                    widthAndHeight, widthAndHeight);
            }
            catch (OverflowException)
            {
                // Oh well
            }
        }
        public void FillRectangle(Color color, MyVector lower, MyVector upper)
        {
            using (SolidBrush brush = new SolidBrush(color))
            {
                FillRectangle(brush, lower, upper);
            }
        }
        public void FillRectangle(Brush brush, MyVector lower, MyVector upper)
        {
            try
            {
                _graphics.FillRectangle(brush,
                    PosWToV_X(lower.X), PosWToV_Y(lower.Y),
                    DistWToV(upper.X - lower.X), DistWToV(upper.Y - lower.Y));
            }
            catch (OverflowException)
            {
                // Oh well
            }
        }
        public void FillPie(Color color, MyVector centerPoint, double radius, MyVector centerLine, double sweepRadians)
        {
            using (SolidBrush brush = new SolidBrush(color))
            {
                FillPie(brush, centerPoint, radius, centerLine, sweepRadians);
            }
        }
        public void FillPie(Brush brush, MyVector centerPoint, double radius, MyVector centerLine, double sweepRadians)
        {
            // Turn the centerline and sweep radians into angles that GDI likes
            MyVector dummy = new MyVector(1, 0, 0);
            double startDegrees;
            dummy.GetAngleAroundAxis(out dummy, out startDegrees, centerLine);
            startDegrees = Utility3D.GetRadiansToDegrees(startDegrees);

            if (centerLine.Y < 0)
            {
                startDegrees *= -1;
            }

            double sweepDegrees = Utility3D.GetRadiansToDegrees(sweepRadians);

            startDegrees -= sweepDegrees / 2d;

            // Call my overload
            FillPie(brush, centerPoint, radius, startDegrees, sweepDegrees);
        }
        public void FillPie(Color color, MyVector centerPoint, double radius, double startDegrees, double sweepDegrees)
        {
            using (SolidBrush brush = new SolidBrush(color))
            {
                FillPie(brush, centerPoint, radius, startDegrees, sweepDegrees);
            }
        }
        public void FillPie(Brush brush, MyVector centerPoint, double radius, double startDegrees, double sweepDegrees)
        {
            try
            {
                float widthAndHeight = DistWToV(radius * 2d);

                _graphics.FillPie(brush,
                    PosWToV_X(centerPoint.X - radius), PosWToV_Y(centerPoint.Y - radius),
                    widthAndHeight, widthAndHeight,
                    Convert.ToSingle(startDegrees), Convert.ToSingle(sweepDegrees));
            }
            catch (OverflowException)
            {
                // Oh well
            }
        }
        public void FillPolygon(Color color1, Color color2, MyVector centerPoint, IMyPolygon polygon)
        {
            using (SolidBrush brush1 = new SolidBrush(color1))
            {
                using (SolidBrush brush2 = new SolidBrush(color2))
                {
                    FillPolygon(brush1, brush2, centerPoint, polygon);
                }
            }
        }
        public void FillPolygon(Brush brush1, Brush brush2, MyVector centerPoint, IMyPolygon polygon)
        {
            try
            {
                //TODO: sort the triangles by z

                // Children
                if (polygon.ChildPolygons != null)
                {
                    foreach (IMyPolygon childPoly in polygon.ChildPolygons)
                    {
                        FillPolygon(brush1, brush2, centerPoint, childPoly);
                    }
                }

                // Current
                if (polygon.Triangles != null)
                {
                    Triangle[] cachedTriangles = polygon.Triangles;

                    for (int triangleCntr = 0; triangleCntr < cachedTriangles.Length; triangleCntr++)
                    {
                        PointF[] points = new PointF[3];

                        points[0].X = PosWToV_X(cachedTriangles[triangleCntr].Vertex1.X + centerPoint.X);
                        points[0].Y = PosWToV_Y(cachedTriangles[triangleCntr].Vertex1.Y + centerPoint.Y);

                        points[1].X = PosWToV_X(cachedTriangles[triangleCntr].Vertex2.X + centerPoint.X);
                        points[1].Y = PosWToV_Y(cachedTriangles[triangleCntr].Vertex2.Y + centerPoint.Y);

                        points[2].X = PosWToV_X(cachedTriangles[triangleCntr].Vertex3.X + centerPoint.X);
                        points[2].Y = PosWToV_Y(cachedTriangles[triangleCntr].Vertex3.Y + centerPoint.Y);

                        if (triangleCntr % 2 == 0)
                        {
                            _graphics.FillPolygon(brush1, points);
                        }
                        else
                        {
                            _graphics.FillPolygon(brush2, points);
                        }
                    }
                }
            }
            catch (OverflowException)
            {
                // Oh well
            }
        }
        public void FillTriangle(Color color, MyVector point1, MyVector point2, MyVector point3)
        {
            using (SolidBrush brush = new SolidBrush(color))
            {
                FillTriangle(brush, point1, point2, point3);
            }
        }
        public void FillTriangle(Brush brush, MyVector point1, MyVector point2, MyVector point3)
        {
            try
            {
                PointF[] points = new PointF[3];

                points[0].X = PosWToV_X(point1.X);
                points[0].Y = PosWToV_Y(point1.Y);

                points[1].X = PosWToV_X(point2.X);
                points[1].Y = PosWToV_Y(point2.Y);

                points[2].X = PosWToV_X(point3.X);
                points[2].Y = PosWToV_Y(point3.Y);

                _graphics.FillPolygon(brush, points);
            }
            catch (OverflowException)
            {
                // Oh well
            }
        }

        #endregion
        #region Public Methods (Misc)

        /// <summary>
        /// If the user clicks on a picture box, you need to run those coords through this function to figure out where they
        /// clicked in world coords
        /// </summary>
        /// <param name="position">A point in PictureBox coords</param>
        /// <returns>The point in World Coords</returns>
        public MyVector GetPositionViewToWorld(MyVector position)
        {
            MyVector retVal = position.Clone();

            // Figure out the world coords that the top left of the picturebox represents
            double picLeft = _centerPoint.X - ((this.Width / 2d) / _zoom);
            double picTop = _centerPoint.Y - ((this.Height / 2d) / _zoom);

            // The point passed in is a distance from the top left of the picture box to where they clicked.  Turn this view
            // coords into world coords
            retVal.Divide(_zoom);

            // Add these world coords to the top left of the picture box
            retVal.X += picLeft;
            retVal.Y += picTop;

            // It's now what it needs to be
            return retVal;
        }

        public double GetDistanceViewToWorld(double distance)
        {
            return distance / _zoom;
        }

        #endregion

        #region Overrides

        protected override void OnMouseEnter(EventArgs e)
        {
            // MouseWheel will never fire unless I force the focus
            this.Focus();

            base.OnMouseEnter(e);
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            // Remember where they clicked so I can do panning
            _panningLastPoint = new Point(e.X, e.Y);

            if (_shouldAutoScroll && e.Button == MouseButtons.Middle && _panningMouseButton != MouseButtons.Middle)
            {
                _autoscroll.StartAutoScroll(_panningLastPoint);
                this.Invalidate(new Rectangle(_autoscroll.InitialPoint_Offset, _autoscroll.CenterIconImage.Size));      // draw the autoscroll icon
            }

            base.OnMouseDown(e);
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle && _autoscroll.IsAutoScrollActive)
            {
                this.Invalidate(new Rectangle(_autoscroll.InitialPoint_Offset, _autoscroll.CenterIconImage.Size));      // don't draw the autoscroll icon
                _autoscroll.StopAutoScroll();
            }

            base.OnMouseUp(e);
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            _autoscroll.MouseMove(new Point(e.X, e.Y));

            if (e.Button == _panningMouseButton && _panningMouseButton != MouseButtons.None)
            {
                // Drag Scene
                PanView(new MyVector(_panningLastPoint.X - e.X, _panningLastPoint.Y - e.Y, 0), true, true);
                _panningLastPoint = new Point(e.X, e.Y);
            }

            base.OnMouseMove(e);
        }
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (_shouldZoomOnMouseWheel)
            {
                ZoomRelative(e.Delta * 0.001d);
            }

            base.OnMouseWheel(e);
        }

        protected override void OnResize(EventArgs e)
        {
            _isResized = true;

            base.OnResize(e);
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            if (_bitmap != null && !_drawingScene)
            {
                // The blitfrom image should never be sized differently than this control (assuming OnResize is called before OnPaint)
                //pe.Graphics.DrawImageUnscaled(_bitmapBlitFrom, 0, 0);
                pe.Graphics.DrawImageUnscaled(_bitmap, 0, 0);
            }
            else
            {
                base.OnPaint(pe);
            }

            if (_autoscroll.IsAutoScrollActive)
            {
                pe.Graphics.DrawImageUnscaled(_autoscroll.CenterIconImage, _autoscroll.InitialPoint_Offset);
            }
        }

        #endregion
        #region Event Handlers

        private void Autoscroll_AutoScroll(object sender, AutoScrollArgs e)
        {
            // Emulate a drag scene
            PanView(new MyVector(e.XDeltaInt, e.YDeltaInt, 0), true, true);
        }
        private void Autoscroll_CursorChanged(object sender, EventArgs e)
        {
            this.Cursor = _autoscroll.SuggestedCursor;
        }

        #endregion

        #region Private Methods

        private float PosWToV_X(double posX)
        {
            // Figure out the world coords that the left of the picturebox represents
            double picLeft = _centerPoint.X - ((this.Width / 2d) / _zoom);

            // Find the distance between the point passed in, and the picturebox left (in world coords)
            double distance = posX - picLeft;

            // Convert this distance into picturebox units
            distance *= _zoom;

            // Exit Function
            return Convert.ToSingle(distance);
        }
        private float PosWToV_Y(double posY)
        {
            // Figure out the world coords that the left of the picturebox represents
            double picTop = _centerPoint.Y - ((this.Height / 2d) / _zoom);

            // Find the distance between the point passed in, and the picturebox left (in world coords)
            double distance = posY - picTop;

            // Convert this distance into picturebox units
            distance *= _zoom;

            // Exit Function
            return Convert.ToSingle(distance);
        }

        private float DistWToV(double distance)
        {
            return Convert.ToSingle(distance * _zoom);
        }

        private void DrawCheckers()
        {
            double checkerSizeX = (_boundryUpper.X - _boundryLower.X) / _numCheckersPerSide;
            double checkerSizeY = (_boundryUpper.Y - _boundryLower.Y) / _numCheckersPerSide;

            SolidBrush checkerBrush = new SolidBrush(_checkerOtherColor);

            MyVector checkerLowerCorner = new MyVector();
            MyVector checkerUpperCorner = new MyVector();

            //TODO: Do this more efficiently
            #region First Half

            for (double horizontalCntr = _boundryLower.X; horizontalCntr < _boundryUpper.X; horizontalCntr += checkerSizeX * 2)
            {
                for (double verticalCntr = _boundryLower.Y + checkerSizeY; verticalCntr < _boundryUpper.Y - checkerSizeY; verticalCntr += checkerSizeY * 2)
                {
                    checkerLowerCorner.X = horizontalCntr;
                    checkerLowerCorner.Y = verticalCntr;

                    checkerUpperCorner.X = horizontalCntr + checkerSizeX;
                    checkerUpperCorner.Y = verticalCntr + checkerSizeY;

                    FillRectangle(checkerBrush, checkerLowerCorner, checkerUpperCorner);
                }
            }

            #endregion

            #region Second Half

            for (double horizontalCntr = _boundryLower.X + checkerSizeX; horizontalCntr < _boundryUpper.X - checkerSizeX; horizontalCntr += checkerSizeX * 2)
            {
                for (double verticalCntr = _boundryLower.Y; verticalCntr < _boundryUpper.Y; verticalCntr += checkerSizeY * 2)
                {
                    checkerLowerCorner.X = horizontalCntr;
                    checkerLowerCorner.Y = verticalCntr;

                    checkerUpperCorner.X = horizontalCntr + checkerSizeX;
                    checkerUpperCorner.Y = verticalCntr + checkerSizeY;

                    FillRectangle(checkerBrush, checkerLowerCorner, checkerUpperCorner);
                }
            }

            #endregion

            if (_numCheckersPerSide % 2 == 0)
            {
                #region Bottom Edge

                checkerLowerCorner.Y = _boundryUpper.Y - checkerSizeY;
                checkerUpperCorner.Y = _boundryUpper.Y;
                for (double horizontalCntr = _boundryLower.X; horizontalCntr < _boundryUpper.X; horizontalCntr += checkerSizeX * 2)
                {
                    checkerLowerCorner.X = horizontalCntr;
                    checkerUpperCorner.X = horizontalCntr + checkerSizeX;

                    FillRectangle(checkerBrush, checkerLowerCorner, checkerUpperCorner);
                }

                #endregion

                #region Right Edge

                checkerLowerCorner.X = _boundryUpper.X - checkerSizeX;
                checkerUpperCorner.X = _boundryUpper.X;
                for (double verticalCntr = _boundryLower.Y; verticalCntr < _boundryUpper.Y - checkerSizeY; verticalCntr += checkerSizeY * 2)
                {
                    checkerLowerCorner.Y = verticalCntr;
                    checkerUpperCorner.Y = verticalCntr + checkerSizeY;

                    FillRectangle(checkerBrush, checkerLowerCorner, checkerUpperCorner);
                }

                #endregion
            }
        }

        #endregion
    }
}
