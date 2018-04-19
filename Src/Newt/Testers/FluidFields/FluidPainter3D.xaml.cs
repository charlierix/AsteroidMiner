using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
using System.Windows.Threading;

using Game.HelperClassesCore;
using Game.Newt.v2.GameItems;
using Game.HelperClassesWPF;
using Game.HelperClassesWPF.Controls2D;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.Testers.FluidFields
{
    public partial class FluidPainter3D : Window
    {
        #region enum: ViewDirection

        private enum ViewDirection
        {
            Front,
            Back,
            Left,
            Right,
            Top,
            Bottom
        }

        #endregion

        #region Declaration Section

        private FluidField3D _field = null;

        private DispatcherTimer _timer = null;

        /// <summary>
        /// This is the image source of the canvas's image
        /// </summary>
        private WriteableBitmap _bitmap = null;

        private bool _isLeftDown = false;
        private bool _isRightDown = false;
        //NOTE: X,Y,Z of these are scaled from 0 to 1, so they are percent of the size field
        private List<Point3D> _mousePointHistory = new List<Point3D>();

        private VelocityVisualizer3DWindow _velocityVisualizerWindow = null;

        /// <summary>
        /// These bind slider bars to properties
        /// </summary>
        private List<SliderShowValues.PropSync> _propLinks = new List<SliderShowValues.PropSync>();

        private ViewDirection _viewDirection = ViewDirection.Front;

        byte[] _colorZFront = new byte[] { 255, 255, 0, 0 };       //z=0
        byte[] _colorZBack = new byte[] { 255, 0, 0, 255 };        //z=size-1

        private bool _isInitialized = false;

        #endregion

        #region Constructor

        public FluidPainter3D()
        {
            InitializeComponent();

            this.Background = SystemColors.ControlBrush;
        }

        #endregion

        #region Event Listeners

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Field
                _field = new FluidField3D(40);
                _field.Diffusion = 0;
                _field.Damping = 0;

                // Sliders
                PropertyInfo[] propsOptions = typeof(FluidField3D).GetProperties();
                _propLinks.Add(new SliderShowValues.PropSync(trkDiffusion, propsOptions.Where(o => o.Name == "Diffusion").First(), _field, 0, .03));
                _propLinks.Add(new SliderShowValues.PropSync(trkDamping, propsOptions.Where(o => o.Name == "Damping").First(), _field, 0, .1));
                _propLinks.Add(new SliderShowValues.PropSync(trkTimestep, propsOptions.Where(o => o.Name == "TimeStep").First(), _field, 0, 100));
                _propLinks.Add(new SliderShowValues.PropSync(trkIterations, propsOptions.Where(o => o.Name == "Iterations").First(), _field, 0, 20));

                trkBrushSize.Minimum = 0;
                trkBrushSize.Maximum = .5;
                trkBrushSize.Value = .15;

                trkVelocityMultiplier.Minimum = .1;
                trkVelocityMultiplier.Maximum = 20;
                trkVelocityMultiplier.Value = 3;

                trkBrushDepth.Minimum = 0;
                trkBrushDepth.Maximum = 1;
                trkBrushDepth.Value = .5;

                // Create a new image
                Image img = new Image();
                RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.NearestNeighbor);
                RenderOptions.SetEdgeMode(img, EdgeMode.Aliased);

                // Add this image to the canvas
                grdFluid.Children.Add(img);

                // Create the bitmap, and set
                _bitmap = new WriteableBitmap(_field.Size, _field.Size, UtilityWPF.DPI, UtilityWPF.DPI, PixelFormats.Bgra32, null);

                img.Source = _bitmap;
                img.Stretch = Stretch.Fill;

                // Timer
                _timer = new DispatcherTimer();
                _timer.Interval = TimeSpan.FromMilliseconds(1);
                _timer.Tick += Timer_Tick;
                _timer.IsEnabled = true;

                // Need to manually set the scrollviewer's height (a binding can't be conditional - see Window_SizeChanged)
                expanderScrollViewer.MaxHeight = expanderRow.ActualHeight;

                _isInitialized = true;
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
                _timer.Stop();

                if (_velocityVisualizerWindow != null)
                {
                    _velocityVisualizerWindow.Close();        // the closed event will fire, and this form will unhook from the viewer there
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (_field == null)
                {
                    _timer.IsEnabled = false;
                    return;
                }

                if (_isLeftDown || _isRightDown)
                {
                    #region Dragging mouse

                    double radius = trkBrushSize.Value * .5d;
                    int[] clickFieldPoints = GetFieldSphere(_mousePointHistory[_mousePointHistory.Count - 1], radius, _field);

                    Vector3D dragVelocity = (GetDragVelocity() ?? new Vector3D(0, 0, 0)) * trkVelocityMultiplier.Value;
                    bool applyVelocity = _isRightDown || (_isLeftDown && chkVelocityOnLeftDrag.IsChecked.Value);

                    foreach (int point in clickFieldPoints)
                    {
                        if (_isLeftDown)
                        {
                            // Add color
                            _field.SetInk(point, 1);
                        }

                        if (applyVelocity)
                        {
                            // Add velocity
                            _field.AddVelocity(point, dragVelocity.X, dragVelocity.Y, dragVelocity.Z);
                        }
                    }

                    #endregion
                }

                _field.Update();

                DrawField(_bitmap, _field, _viewDirection, _colorZFront, _colorZBack);

                if (_velocityVisualizerWindow != null)
                {
                    _velocityVisualizerWindow.Update();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                if (!_isInitialized)
                {
                    return;
                }

                if (this.SizeToContent == System.Windows.SizeToContent.Manual)
                {
                    // At design time, the window's SizeToContent is set to width and height, and the grid is set to a decent size.  This
                    // way, when they expand the expanders, the grid will stay the same size (and the window will grow to accomodate it).
                    //
                    // But if the user manually resizes the window, the grid now needs to fill all available space
                    grid1.Width = double.NaN;
                    grid1.Height = double.NaN;

                    expanderScrollViewer.MaxHeight = double.PositiveInfinity;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void grid1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.ChangedButton == MouseButton.Left)
                {
                    _isLeftDown = true;
                }
                else if (e.ChangedButton == MouseButton.Right)
                {
                    _isRightDown = true;
                }

                if (_isLeftDown || _isRightDown)
                {
                    _mousePointHistory.Add(GetScaledPoint(e, grid1, trkBrushDepth.Value, _viewDirection));
                    grid1.CaptureMouse();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void grid1_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (!_isLeftDown && !_isRightDown)
                {
                    return;
                }

                Point3D clickPoint = GetScaledPoint(e, grid1, trkBrushDepth.Value, _viewDirection);
                _mousePointHistory.Add(clickPoint);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void grid1_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.ChangedButton == MouseButton.Left)
                {
                    _isLeftDown = false;
                }
                else if (e.ChangedButton == MouseButton.Right)
                {
                    _isRightDown = false;
                }

                if (!_isLeftDown && !_isRightDown)
                {
                    grid1.ReleaseMouseCapture();
                    _mousePointHistory.Clear();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void View_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_isInitialized)
                {
                    return;
                }

                if (radViewFront.IsChecked.Value)
                {
                    _viewDirection = ViewDirection.Front;
                }
                else if (radViewRight.IsChecked.Value)
                {
                    _viewDirection = ViewDirection.Right;
                }
                else if (radViewLeft.IsChecked.Value)
                {
                    _viewDirection = ViewDirection.Left;
                }
                else if (radViewTop.IsChecked.Value)
                {
                    _viewDirection = ViewDirection.Top;
                }
                else if (radViewBottom.IsChecked.Value)
                {
                    _viewDirection = ViewDirection.Bottom;
                }
                else if (radViewBack.IsChecked.Value)
                {
                    _viewDirection = ViewDirection.Back;
                }
                else
                {
                    throw new ApplicationException("Unknown view direction");
                }

                SetVelocityViewerCameraPosition();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void BackgroundColor_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_isInitialized)
                {
                    return;
                }

                if (radBackBlack.IsChecked.Value)
                {
                    grid1.Background = Brushes.Black;
                }
                else if (radBackGray.IsChecked.Value)
                {
                    grid1.Background = Brushes.Gray;
                }
                else if (radBackWhite.IsChecked.Value)
                {
                    grid1.Background = Brushes.White;
                }
                else if (radBackGreen.IsChecked.Value)
                {
                    grid1.Background = new SolidColorBrush(UtilityWPF.ColorFromHex("236130"));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void GasColor_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_isInitialized)
                {
                    return;
                }

                if (radGasRedBlue.IsChecked.Value)
                {
                    _colorZFront = new byte[] { 255, 255, 0, 0 };
                    _colorZBack = new byte[] { 255, 0, 0, 255 };
                }
                else if (radGasWhiteBlack.IsChecked.Value)
                {
                    _colorZFront = new byte[] { 255, 255, 255, 255 };
                    _colorZBack = new byte[] { 255, 0, 0, 0 };
                }
                else if (radGasBlack.IsChecked.Value)
                {
                    _colorZFront = new byte[] { 255, 0, 0, 0 };
                    _colorZBack = new byte[] { 255, 0, 0, 0 };
                }
                else if (radGasWhite.IsChecked.Value)
                {
                    _colorZFront = new byte[] { 255, 255, 255, 255 };
                    _colorZBack = new byte[] { 255, 255, 255, 255 };
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BoundryType_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_field == null)
                {
                    return;
                }

                if (radBoundryType_ClosedBox.IsChecked.Value)
                {
                    _field.BoundryType = FluidFieldBoundryType3D.Closed;
                }
                else if (radBoundryType_OpenBox.IsChecked.Value)
                {
                    _field.BoundryType = FluidFieldBoundryType3D.Open;
                }
                else if (radBoundryType_WrapBox.IsChecked.Value)
                {
                    _field.BoundryType = FluidFieldBoundryType3D.WrapAround;
                }
                else
                {
                    throw new ApplicationException("Unknown boundry type");
                }
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

        private void Reset0_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ResetField();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Reset1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ResetField();

                for (int x = 1; x < _field.Size - 1; x++)       // if I go to the edges, they just get blanked out
                {
                    for (int y = 1; y < _field.Size - 1; y++)
                    {
                        _field.SetInk(_field.Get1DIndex(x, y, 1 + StaticRandom.Next(_field.Size - 2)), 1);
                    }
                }

                //_field.Update();
                //DrawField(_bitmap, _field);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Reset2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ResetField();

                int depth = StaticRandom.Next(_field.Size);

                for (int x = 0; x < _field.Size; x++)
                {
                    for (int y = 0; y < _field.Size; y++)
                    {
                        _field.SetInk(_field.Get1DIndex(x, y, depth), 1);
                    }
                }

                //_field.Update();
                //DrawField(_bitmap, _field);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Reset2a_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ResetField();

                int depth = 2 + StaticRandom.Next(_field.Size - 4);

                for (int x = 0; x < _field.Size; x++)
                {
                    for (int y = 0; y < _field.Size; y++)
                    {
                        for (int z = depth - 2; z <= depth + 2; z++)
                        {
                            _field.SetInk(_field.Get1DIndex(x, y, z), .75);
                        }
                    }
                }

                //_field.Update();
                //DrawField(_bitmap, _field);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Reset3_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ResetField();

                for (int x = 0; x < _field.Size; x++)
                {
                    for (int y = 0; y < _field.Size; y++)
                    {
                        for (int z = 0; z < _field.Size; z++)
                        {
                            _field.SetInk(_field.Get1DIndex(x, y, z), .33);
                        }
                    }
                }

                //_field.Update();
                //DrawField(_bitmap, _field);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Block0_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double radius = trkBrushSize.Value * .5d;
                int[] clickFieldPoints = GetFieldCube(new Point3D(.5, .5, .5), radius, _field);
                _field.SetBlockedCells(clickFieldPoints, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Block1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double radius = trkBrushSize.Value * .5d;
                int[] clickFieldPoints = GetFieldSphere(new Point3D(.5, .5, .5), radius, _field);
                _field.SetBlockedCells(clickFieldPoints, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private void ResetField()
        {
            // Set everything to zero
            for (int index = 0; index < _field.Size1D; index++)
            {
                _field.SetInk(index, 0);
                _field.SetVelocity(index, 0, 0, 0);
            }

            _field.SetBlockedCells(Enumerable.Range(0, _field.Size1D), false);
        }

        /// <summary>
        /// This takes the click point, and returns a point from 0 to 1
        /// </summary>
        private static Point3D GetScaledPoint(MouseEventArgs e, FrameworkElement relativeTo, double depth, ViewDirection direction)
        {
            Point clickPoint = e.GetPosition(relativeTo);

            double width = relativeTo.ActualWidth;
            double height = relativeTo.ActualHeight;

            if (Math1D.IsNearZero(width) || Math1D.IsNearZero(height))
            {
                // The window has no size
                return new Point3D(0, 0, 0);
            }

            Point3D point = new Point3D(clickPoint.X / width, clickPoint.Y / height, depth);

            switch (direction)
            {
                case ViewDirection.Front:
                    return new Point3D(point.X, point.Y, point.Z);

                case ViewDirection.Right:
                    return new Point3D(1d - point.Z, point.Y, point.X);

                case ViewDirection.Left:
                    return new Point3D(point.Z, point.Y, 1d - point.X);

                case ViewDirection.Top:
                    return new Point3D(point.X, point.Z, 1d - point.Y);

                case ViewDirection.Bottom:
                    return new Point3D(point.X, 1d - point.Z, point.Y);

                case ViewDirection.Back:
                    return new Point3D(1d - point.X, point.Y, 1d - point.Z);

                default:
                    throw new ApplicationException("Unknown ViewDirection: " + direction.ToString());
            }
        }

        private static void DrawField(WriteableBitmap bitmap, FluidField3D field, ViewDirection viewDirection, byte[] colorZFront, byte[] colorZBack)
        {
            Int32Rect rect = new Int32Rect(0, 0, field.Size, field.Size);
            int size = rect.Width * rect.Height * 4;
            byte[] pixels = new byte[size];

            double[] ink = field.Ink;
            bool[] blocked = field.Blocked;

            switch (viewDirection)
            {
                case ViewDirection.Front:
                    #region Front

                    DrawFieldSprtDoIt(pixels, ink, blocked, field.Size, colorZFront, colorZBack,
                        new AxisFor(Axis.X, 0, field.Size - 1),
                        new AxisFor(Axis.Y, 0, field.Size - 1),
                        new AxisFor(Axis.Z, field.Size - 1, 0));        // pixel z needs to start at the back, because the colors are overlaid

                    #endregion
                    break;

                case ViewDirection.Right:
                    #region Right

                    DrawFieldSprtDoIt(pixels, ink, blocked, field.Size, colorZFront, colorZBack,
                        new AxisFor(Axis.Z, 0, field.Size - 1),
                        new AxisFor(Axis.Y, 0, field.Size - 1),
                        new AxisFor(Axis.X, 0, field.Size - 1));

                    #endregion
                    break;

                case ViewDirection.Left:
                    #region Left

                    DrawFieldSprtDoIt(pixels, ink, blocked, field.Size, colorZFront, colorZBack,
                        new AxisFor(Axis.Z, field.Size - 1, 0),
                        new AxisFor(Axis.Y, 0, field.Size - 1),
                        new AxisFor(Axis.X, field.Size - 1, 0));

                    #endregion
                    break;

                case ViewDirection.Top:
                    #region Top

                    DrawFieldSprtDoIt(pixels, ink, blocked, field.Size, colorZFront, colorZBack,
                        new AxisFor(Axis.X, 0, field.Size - 1),
                        new AxisFor(Axis.Z, field.Size - 1, 0),
                        new AxisFor(Axis.Y, field.Size - 1, 0));

                    #endregion
                    break;

                case ViewDirection.Bottom:
                    #region Bottom

                    DrawFieldSprtDoIt(pixels, ink, blocked, field.Size, colorZFront, colorZBack,
                        new AxisFor(Axis.X, 0, field.Size - 1),
                        new AxisFor(Axis.Z, 0, field.Size - 1),
                        new AxisFor(Axis.Y, 0, field.Size - 1));

                    #endregion
                    break;

                case ViewDirection.Back:
                    #region Back

                    //NOTE: This one is up for interpretation.  I will rotate left to right, because I think that is more intuitive.  If rotating
                    //top to bottom, it would be upside down from the way I am presenting
                    DrawFieldSprtDoIt(pixels, ink, blocked, field.Size, colorZFront, colorZBack,
                        new AxisFor(Axis.X, field.Size - 1, 0),
                        new AxisFor(Axis.Y, 0, field.Size - 1),
                        new AxisFor(Axis.Z, 0, field.Size - 1));

                    // The alternate
                    //DrawField_DoIt(pixels, ink, blocked, field.Size, colorZFront, colorZBack,
                    //    new AxisFor(Axis.X, 0, field.Size - 1),
                    //    new AxisFor(Axis.Y, field.Size - 1, 0),
                    //    new AxisFor(Axis.Z, 0, field.Size - 1));

                    #endregion
                    break;

                default:
                    throw new ApplicationException("Unknown ViewDirection: " + viewDirection.ToString());
            }

            bitmap.WritePixels(rect, pixels, rect.Width * 4, 0);
        }
        private static void DrawFieldSprtDoIt(byte[] pixels, double[] ink, bool[] blocked, int size, byte[] colorZFront, byte[] colorZBack, AxisFor pixelX, AxisFor pixelY, AxisFor pixelZ)
        {
            List<Mapping_2D_1D> flattened = new List<Mapping_2D_1D>();

            // Setup the pixel array
            //for (int y2D = pixelY.Start; pixelY.IsPos ? y2D <= pixelY.Stop : y2D >= pixelY.Stop; y2D += pixelY.Increment)
            foreach (int y2D in pixelY.Iterate())
            {
                int offsetY = pixelY.GetValueForOffset(y2D) * size;

                //for (int x2D = pixelX.Start; pixelX.IsPos ? x2D <= pixelX.Stop : x2D >= pixelX.Stop; x2D += pixelX.Increment)
                foreach (int x2D in pixelX.Iterate())
                {
                    int offset = offsetY + pixelX.GetValueForOffset(x2D);

                    flattened.Add(new Mapping_2D_1D(x2D, y2D, offset));
                }
            }

            // Each pixel of the output bitmap can be added up independently, so employ some threading
            flattened.AsParallel().ForAll(o =>
                {
                    //TODO: Color is 6.5 times slower than byte array
                    List<byte[]> colorColumn = new List<byte[]>();

                    for (int z2D = pixelZ.Start; pixelZ.IsPos ? z2D <= pixelZ.Stop : z2D >= pixelZ.Stop; z2D += pixelZ.Increment)
                    {
                        int x = -1, y = -1, z = -1;
                        pixelX.Set3DIndex(ref x, ref y, ref z, o.X);
                        pixelY.Set3DIndex(ref x, ref y, ref z, o.Y);
                        pixelZ.Set3DIndex(ref x, ref y, ref z, z2D);

                        int index = FluidField3D.Get1DIndex(x, y, z, size);
                        if (blocked[index])
                        {
                            // Blocked cells are all white, so save the overlay method a bit of work, and throw out everything behind this
                            colorColumn.Clear();
                            colorColumn.Add(new byte[] { 255, 255, 255, 255 });
                            continue;
                        }

                        double inkCell = ink[index];

                        if (Math1D.IsNearZero(inkCell))
                        {
                            continue;
                        }

                        byte[] depthColor = UtilityWPF.AlphaBlend(colorZBack, colorZFront, UtilityCore.GetScaledValue_Capped(0, 1, 0, size - 1, z));

                        int alpha = Convert.ToInt32(Math.Round(inkCell * 255));
                        if (alpha < 0)
                        {
                            alpha = 0;
                        }
                        else if (alpha > 255)
                        {
                            alpha = 255;
                        }

                        colorColumn.Add(new byte[] { Convert.ToByte(alpha), depthColor[1], depthColor[2], depthColor[3] });
                    }

                    byte[] color = colorColumn.Count > 0 ? UtilityWPF.OverlayColors(colorColumn) : new byte[] { 0, 0, 0, 0 };

                    pixels[o.Offset1D * 4 + 0] = color[3];   // Blue
                    pixels[o.Offset1D * 4 + 1] = color[2];     // Green
                    pixels[o.Offset1D * 4 + 2] = color[1];     // Red
                    pixels[o.Offset1D * 4 + 3] = color[0];   // Alpha
                });
        }

        private static int[] GetFieldCube(Point3D point, double radius, FluidField3D field)
        {
            // Get the box that contains the return circle
            var center = GetFieldPoint_XYZ(point, field);
            var min = GetFieldPoint_XYZ(new Point3D(point.X - radius, point.Y - radius, point.Z - radius), field);
            var max = GetFieldPoint_XYZ(new Point3D(point.X + radius, point.Y + radius, point.Z + radius), field);

            // Get points that are inside the circle
            List<int> retVal = new List<int>();
            double maxDistance = radius * field.Size;
            double maxDistanceSquared = maxDistance * maxDistance;

            for (int x = min.Item1; x <= max.Item1; x++)
            {
                for (int y = min.Item2; y <= max.Item2; y++)
                {
                    for (int z = min.Item3; z <= max.Item3; z++)
                    {
                        retVal.Add(field.Get1DIndex(x, y, z));
                    }
                }
            }

            // Exit Function
            return retVal.ToArray();
        }
        private static int[] GetFieldSphere(Point3D point, double radius, FluidField3D field)
        {
            // Get the box that contains the return circle
            var center = GetFieldPoint_XYZ(point, field);
            var min = GetFieldPoint_XYZ(new Point3D(point.X - radius, point.Y - radius, point.Z - radius), field);
            var max = GetFieldPoint_XYZ(new Point3D(point.X + radius, point.Y + radius, point.Z + radius), field);

            // Get points that are inside the circle
            List<int> retVal = new List<int>();
            double maxDistance = radius * field.Size;
            double maxDistanceSquared = maxDistance * maxDistance;

            for (int x = min.Item1; x <= max.Item1; x++)
            {
                for (int y = min.Item2; y <= max.Item2; y++)
                {
                    for (int z = min.Item3; z <= max.Item3; z++)
                    {
                        double dx = x - center.Item1;
                        double dy = y - center.Item2;
                        double dz = z - center.Item3;
                        double distanceSquared = (dx * dx) + (dy * dy) + (dz * dz);

                        if (distanceSquared <= maxDistanceSquared)
                        {
                            retVal.Add(field.Get1DIndex(x, y, z));
                        }
                    }
                }
            }

            // Exit Function
            return retVal.ToArray();
        }

        private static Tuple<int, int, int> GetFieldPoint_XYZ(Point3D point, FluidField3D field)
        {
            int x = Convert.ToInt32(Math.Round(point.X * field.Size));
            if (x < 0)
            {
                x = 0;
            }
            else if (x >= field.Size)
            {
                x = field.Size - 1;
            }

            int y = Convert.ToInt32(Math.Round(point.Y * field.Size));
            if (y < 0)
            {
                y = 0;
            }
            else if (y >= field.Size)
            {
                y = field.Size - 1;
            }

            int z = Convert.ToInt32(Math.Round(point.Z * field.Size));
            if (z < 0)
            {
                z = 0;
            }
            else if (z >= field.Size)
            {
                z = field.Size - 1;
            }

            return Tuple.Create(x, y, z);
        }

        private Vector3D? GetDragVelocity()
        {
            if (_mousePointHistory.Count > 6)
            {
                // Don't let the history get too big
                _mousePointHistory.RemoveAt(0);
            }

            if (_mousePointHistory.Count < 2)
            {
                // Not enough data
                return null;
            }

            Vector3D retVal = new Vector3D(0, 0, 0);

            // Add up all the instantaneous velocities
            for (int cntr = 0; cntr < _mousePointHistory.Count - 1; cntr++)
            {
                retVal += _mousePointHistory[cntr + 1] - _mousePointHistory[cntr];
            }

            // Take the average
            return retVal / (_mousePointHistory.Count - 1);
        }

        private void SetVelocityViewerCameraPosition()
        {
            if (_velocityVisualizerWindow == null)
            {
                return;
            }

            switch (_viewDirection)
            {
                case ViewDirection.Front:
                    _velocityVisualizerWindow.ViewChanged(new DoubleVector(0, 0, 1, 0, -1, 0));
                    break;

                case ViewDirection.Right:
                    _velocityVisualizerWindow.ViewChanged(new DoubleVector(-1, 0, 0, 0, -1, 0));
                    break;

                case ViewDirection.Left:
                    _velocityVisualizerWindow.ViewChanged(new DoubleVector(1, 0, 0, 0, -1, 0));
                    break;

                case ViewDirection.Top:
                    _velocityVisualizerWindow.ViewChanged(new DoubleVector(0, 1, 0, 0, 0, 1));
                    break;

                case ViewDirection.Bottom:
                    _velocityVisualizerWindow.ViewChanged(new DoubleVector(0, -1, 0, 0, 0, -1));
                    break;

                case ViewDirection.Back:
                    _velocityVisualizerWindow.ViewChanged(new DoubleVector(0, 0, -1, 0, -1, 0));
                    break;

                default:
                    throw new ApplicationException("Unknown ViewDirection: " + _viewDirection.ToString());
            }
        }

        #region OLD

        //private static void DrawField_Front(byte[] pixels, double[] ink, int size, Color colorZFront, Color colorZBack)
        //{
        //    List<Color> colorColumn = new List<Color>();

        //    // Setup the pixel array
        //    for (int y = 0; y < size; y++)
        //    {
        //        int offsetY = y * size;

        //        for (int x = 0; x < size; x++)
        //        {
        //            int offset = offsetY + x;
        //            colorColumn.Clear();

        //            for (int z = size - 1; z >= 0; z--)       // work from the bottom up
        //            {
        //                double inkCell = ink[FluidField3D.Get1DIndex(x, y, z, size)];

        //                if (Math3D.IsNearZero(inkCell))
        //                {
        //                    continue;
        //                }

        //                Color depthColor = UtilityWPF.AlphaBlend(colorZBack, colorZFront, UtilityHelper.GetScaledValue_Capped(0, 1, 0, size - 1, z));

        //                int alpha = Convert.ToInt32(Math.Round(inkCell * 255));
        //                if (alpha < 0)
        //                {
        //                    alpha = 0;
        //                }
        //                else if (alpha > 255)
        //                {
        //                    alpha = 255;
        //                }

        //                colorColumn.Add(Color.FromArgb(Convert.ToByte(alpha), depthColor.R, depthColor.G, depthColor.B));
        //            }

        //            Color color = colorColumn.Count > 0 ? UtilityWPF.OverlayColors(colorColumn) : Colors.Transparent;

        //            pixels[offset * 4 + 0] = color.B;   // Blue
        //            pixels[offset * 4 + 1] = color.G;     // Green
        //            pixels[offset * 4 + 2] = color.R;     // Red
        //            pixels[offset * 4 + 3] = color.A;   // Alpha
        //        }
        //    }
        //}
        //private static void DrawField_Right(byte[] pixels, double[] ink, int size, Color colorZFront, Color colorZBack)
        //{
        //    List<Color> colorColumn = new List<Color>();

        //    // Setup the pixel array
        //    for (int y = 0; y < size; y++)      // y maps to y in the bitmap
        //    {
        //        int offsetY = y * size;

        //        for (int z = 0; z < size; z++)      // z maps to x in the bitmap
        //        {
        //            int offset = offsetY + z;
        //            colorColumn.Clear();

        //            for (int x = 0; x < size; x++)       // x=0 is the deepest depth
        //            {
        //                double inkCell = ink[FluidField3D.Get1DIndex(x, y, z, size)];

        //                if (Math3D.IsNearZero(inkCell))
        //                {
        //                    continue;
        //                }

        //                Color depthColor = UtilityWPF.AlphaBlend(colorZBack, colorZFront, UtilityHelper.GetScaledValue_Capped(0, 1, 0, size - 1, z));

        //                int alpha = Convert.ToInt32(Math.Round(inkCell * 255));
        //                if (alpha < 0)
        //                {
        //                    alpha = 0;
        //                }
        //                else if (alpha > 255)
        //                {
        //                    alpha = 255;
        //                }

        //                colorColumn.Add(Color.FromArgb(Convert.ToByte(alpha), depthColor.R, depthColor.G, depthColor.B));
        //            }

        //            Color color = colorColumn.Count > 0 ? UtilityWPF.OverlayColors(colorColumn) : Colors.Transparent;

        //            pixels[offset * 4 + 0] = color.B;   // Blue
        //            pixels[offset * 4 + 1] = color.G;     // Green
        //            pixels[offset * 4 + 2] = color.R;     // Red
        //            pixels[offset * 4 + 3] = color.A;   // Alpha
        //        }
        //    }
        //}

        #endregion

        #endregion
    }
}
