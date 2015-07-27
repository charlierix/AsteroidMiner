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
using System.Windows.Shapes;
using System.Windows.Threading;

using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.HelperClassesWPF.Controls2D;

namespace Game.Newt.Testers.FluidFields
{
    public partial class FluidPainter2D : Window
    {
        #region Enum: RandColorType

        //TODO: Make a few more, don't go nuts
        //TODO: Let them create a custom pallete
        //TODO: Sample colors off of an image from file or clipboard or wallpaper
        private enum RandColorType
        {
            Any,
            Black_Green,
            Black_Orange,
            Black_Purple,
            Red_Tan_Green,
            Tans,
            Skittles,
            Unispew,
            Camo,
            Cold_Beach,
            Mono_Cyan,
        }

        #endregion
        #region Class: SliderSettings

        public class SliderSettings
        {
            public double Min = 0;
            public double Max = 1;
            public double Value = .5;
        }

        #endregion

        #region Declaration Section

        private FluidField2D _field = null;

        private DispatcherTimer _timer = null;

        /// <summary>
        /// This is the image source of the canvas's image
        /// </summary>
        private WriteableBitmap _bitmap = null;

        private bool _isLeftDown = false;
        private bool _isRightDown = false;
        //NOTE: X and Y of these are scaled from 0 to 1, so they are percent of the size field
        private List<Point> _mousePointHistory = new List<Point>();

        private VelocityVisualizer2DWindow _velocityVisualizerWindow = null;
        private VelocityVisualizerVisualHost _velocityVisualizerPanel = null;

        private SliderSettings _colorBrushSize = null;
        private SliderSettings _wallBrushSize = null;

        /// <summary>
        /// These bind slider bars to properties
        /// </summary>
        private List<SliderShowValues.PropSync> _propLinks = new List<SliderShowValues.PropSync>();

        private RandColorType _brushColorType;
        private RandColorType _randColorType;
        private SortedList<RandColorType, Color[]> _randColors = null;

        private bool _isInitialized = false;

        #endregion

        #region Constructor

        public FluidPainter2D()
        {
            InitializeComponent();

            this.Background = SystemColors.ControlBrush;

            //NOTE: RandColorType.Any is calculated on the fly isntead of preloaded
            _randColors = new SortedList<RandColorType, Color[]>();
            _randColors.Add(RandColorType.Black_Green, new Color[] { Colors.Black, Colors.Black, Colors.Black, Colors.Chartreuse });
            _randColors.Add(RandColorType.Black_Orange, new Color[] { Colors.Black, Colors.Black, Colors.Black, UtilityWPF.ColorFromHex("F76700") });
            _randColors.Add(RandColorType.Black_Purple, new Color[] { Colors.Black, Colors.Black, Colors.Black, UtilityWPF.ColorFromHex("811CD6") });
            _randColors.Add(RandColorType.Red_Tan_Green, new string[] { "65BA99", "59A386", "F1DDBB", "D6C4A6", "E74C3C", "C74134" }.Select(o => UtilityWPF.ColorFromHex(o)).ToArray());
            _randColors.Add(RandColorType.Tans, new string[] { "736753", "594832", "D9CFC7", "BFB6AE", "A68D77" }.Select(o => UtilityWPF.ColorFromHex(o)).ToArray());
            _randColors.Add(RandColorType.Skittles, new string[] { "AC1014", "E87011", "FDD526", "73C509", "0980BA", "65286B" }.Select(o => UtilityWPF.ColorFromHex(o)).ToArray());
            _randColors.Add(RandColorType.Unispew, new string[] { "FF009C", "FFA11F", "9BFF00", "00FFFD", "8E47FF" }.Select(o => UtilityWPF.ColorFromHex(o)).ToArray());
            _randColors.Add(RandColorType.Camo, new string[] { "244034", "5E744A", "9EA755", "0D0A00", "745515" }.Select(o => UtilityWPF.ColorFromHex(o)).ToArray());
            _randColors.Add(RandColorType.Cold_Beach, new string[] { "CCC8B1", "858068", "FFFCF6", "B7ECFF", "B1CCCC" }.Select(o => UtilityWPF.ColorFromHex(o)).ToArray());
            _randColors.Add(RandColorType.Mono_Cyan, UtilityCore.Iterate(new string[] { "037B8E" }, Enumerable.Range(0, 10).Select(o => new string[] { "99A3A4", "B8BCBB", "535855", "79756A" }).SelectMany(o => o)).Select(o => UtilityWPF.ColorFromHex(o)).ToArray());

            foreach (string colorType in Enum.GetNames(typeof(RandColorType)))
            {
                cboBrushColorType.Items.Add(colorType.Replace('_', ' '));
                cboRandColorType.Items.Add(colorType.Replace('_', ' '));
            }
            cboBrushColorType.SelectedIndex = StaticRandom.Next(cboBrushColorType.Items.Count);
            cboRandColorType.SelectedIndex = StaticRandom.Next(cboRandColorType.Items.Count);
        }

        #endregion

        #region Event Listeners

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {

                // Field
                _field = new FluidField2D(200, 200, 3);
                _field.UseCheapDiffusion = !chkUseStandardDiffusion.IsChecked.Value;

                _field.Vorticity = 0;       //this is currently broken

                // Sliders
                PropertyInfo[] propsOptions = typeof(FluidField2D).GetProperties();
                _propLinks.Add(new SliderShowValues.PropSync(trkDiffusion, propsOptions.Where(o => o.Name == "Diffusion").First(), _field, 0, .1));
                _propLinks.Add(new SliderShowValues.PropSync(trkViscocity, propsOptions.Where(o => o.Name == "Viscosity").First(), _field, 0, .1));
                _propLinks.Add(new SliderShowValues.PropSync(trkWallReflection, propsOptions.Where(o => o.Name == "WallReflectivity").First(), _field, 0, 1));
                _propLinks.Add(new SliderShowValues.PropSync(trkVorticity, propsOptions.Where(o => o.Name == "Vorticity").First(), _field, 0, 1));
                _propLinks.Add(new SliderShowValues.PropSync(trkTimestep, propsOptions.Where(o => o.Name == "TimeStep").First(), _field, 0, 100));
                _propLinks.Add(new SliderShowValues.PropSync(trkIterations, propsOptions.Where(o => o.Name == "Iterations").First(), _field, 0, 20));

                _colorBrushSize = new SliderSettings() { Min = 0, Max = .5, Value = .125 };
                _wallBrushSize = new SliderSettings() { Min = 0, Max = .1, Value = .033 };

                trkBrushSize.Minimum = _colorBrushSize.Min;
                trkBrushSize.Maximum = _colorBrushSize.Max;
                trkBrushSize.Value = _colorBrushSize.Value;

                trkVelocityMultiplier.Minimum = .1;
                trkVelocityMultiplier.Maximum = 100;        //NOTE: the right side scales from 1 to 100, but the left side scales from 1/10 to 1 (because large multipliers are more interesting)
                trkVelocityMultiplier.Value = 8;

                ResetField();

                // Create a new image
                Image img = new Image();
                RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.NearestNeighbor);
                RenderOptions.SetEdgeMode(img, EdgeMode.Aliased);

                // Add this image to the canvas
                grdFluid.Children.Add(img);

                // Create the bitmap, and set
                _bitmap = new WriteableBitmap(_field.XSize, _field.YSize, UtilityWPF.DPI, UtilityWPF.DPI, PixelFormats.Bgra32, null);

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
            const double ONEOVER256 = 1d / 256d;

            try
            {
                if (_field == null)
                {
                    _timer.IsEnabled = false;
                    return;
                }

                //TODO: Populate the space between mouse locations from the last tick (instead of drawing spots)

                //TODO: Allow for some keyboard modifiers, brush buttons, etc for more options:
                // Shape
                // Fade/Grow

                if (_isLeftDown || _isRightDown)
                {
                    double radius = trkBrushSize.Value * .5d;
                    int[] clickFieldPoints = GetFieldCircle(_mousePointHistory[_mousePointHistory.Count - 1], radius, radius, _field);

                    if (radColorBrush.IsChecked.Value)
                    {
                        #region Color Brush

                        Random rand = StaticRandom.GetRandomForThread();

                        Color color = GetRandomColor(rand, _brushColorType);
                        double colorR = color.R * ONEOVER256;
                        double colorG = color.G * ONEOVER256;
                        double colorB = color.B * ONEOVER256;

                        double velScale = trkVelocityMultiplier.Value;
                        Vector dragVelocity = GetDragVelocity() ?? new Vector(0, 0);
                        double velX = dragVelocity.X * velScale;
                        double velY = dragVelocity.Y * velScale;
                        bool applyVelocity = _isRightDown || (_isLeftDown && chkVelocityOnLeftDrag.IsChecked.Value);
                        bool randPerPixel = chkRandColorPerPixel.IsChecked.Value;

                        foreach (int point in clickFieldPoints)
                        {
                            if (_isLeftDown)
                            {
                                if (randPerPixel)
                                {
                                    color = GetRandomColor(rand, _brushColorType);
                                    colorR = color.R * ONEOVER256;
                                    colorG = color.G * ONEOVER256;
                                    colorB = color.B * ONEOVER256;
                                }

                                // Add color
                                _field.SetInk(0, point, colorR);
                                _field.SetInk(1, point, colorG);
                                _field.SetInk(2, point, colorB);
                            }

                            if (applyVelocity)
                            {
                                // Add velocity
                                _field.AddVel(point, velX, velY);
                            }
                        }

                        #endregion
                    }
                    else if (radWall.IsChecked.Value)
                    {
                        #region Wall

                        foreach (int point in clickFieldPoints)
                        {
                            _field.SetBlockedCell(point, _isLeftDown);      // if both are down, left wins (execution only gets here if left or right are down, so if left is up, right must be down - or both down, which let left win)
                        }

                        #endregion
                    }
                    else
                    {
                        throw new ApplicationException("Unknown tool brush");
                    }
                }

                if (chkPauseResume.IsChecked.Value)
                {
                    _field.Update();
                }

                DrawField(_bitmap, _field, chkShowWalls.IsChecked.Value);

                // Velocity Viewer
                if (chkVelocityOverlay.IsChecked.Value)
                {
                    _velocityVisualizerPanel.Update();
                }

                if (_velocityVisualizerWindow != null)
                {
                    _velocityVisualizerWindow.Update();
                }

                #region Analyze Field

                // Colors > 1
                if (chkFlagColor.IsChecked.Value)
                {
                    if (_field.Layers.SelectMany(o => o).Any(o => Math.Abs(o) > 1))
                    {
                        chkFlagColor.Background = Brushes.Tomato;
                        chkFlagColor.Foreground = Brushes.Tomato;
                    }
                    else
                    {
                        chkFlagColor.Background = null;
                        chkFlagColor.Foreground = SystemColors.WindowTextBrush;
                    }
                }

                // Velocities > 1
                if (chkFlagVelocity.IsChecked.Value)
                {
                    if (Enumerable.Range(0, _field.KSize).Any(o => _field.XVel[o] * _field.XVel[o] + _field.YVel[o] * _field.YVel[o] > 1))
                    {
                        chkFlagVelocity.Background = Brushes.Tomato;
                        chkFlagVelocity.Foreground = Brushes.Tomato;
                    }
                    else
                    {
                        chkFlagVelocity.Background = null;
                        chkFlagVelocity.Foreground = SystemColors.WindowTextBrush;
                    }
                }

                #endregion
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

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            try
            {
                // Show this on the default browser
                System.Diagnostics.Process.Start(e.Uri.OriginalString);
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

                _velocityVisualizerWindow = new VelocityVisualizer2DWindow();
                _velocityVisualizerWindow.Closed += VelocityVisualizer_Closed;
                _velocityVisualizerWindow.Field = _field;
                _velocityVisualizerWindow.Show();
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
        private void chkVelocityOverlay_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!chkVelocityOverlay.IsChecked.Value)
                {
                    grdVelocities.Visibility = Visibility.Collapsed;
                    return;
                }

                if (_velocityVisualizerPanel == null)
                {
                    _velocityVisualizerPanel = new VelocityVisualizerVisualHost(_field);
                    grdVelocities.Children.Add(_velocityVisualizerPanel);
                }

                grdVelocities.Visibility = Visibility.Visible;
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
                    _mousePointHistory.Add(GetScaledPoint(e, grid1));
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

                Point clickPoint = GetScaledPoint(e, grid1);

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

        private void ToolButton_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_isInitialized)
                {
                    return;
                }

                if (e.OriginalSource == radColorBrush)
                {
                    _colorBrushSize.Min = trkBrushSize.Minimum;
                    _colorBrushSize.Max = trkBrushSize.Maximum;
                    _colorBrushSize.Value = trkBrushSize.Value;
                }
                else if (e.OriginalSource == radWall)
                {
                    _wallBrushSize.Min = trkBrushSize.Minimum;
                    _wallBrushSize.Max = trkBrushSize.Maximum;
                    _wallBrushSize.Value = trkBrushSize.Value;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ToolButton_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_isInitialized)
                {
                    return;
                }

                // Clear
                lblVelocityMultiplier.Visibility = Visibility.Collapsed;
                trkVelocityMultiplier.Visibility = Visibility.Collapsed;
                chkVelocityOnLeftDrag.Visibility = Visibility.Collapsed;
                chkRandColorPerPixel.Visibility = Visibility.Collapsed;
                cboBrushColorType.Visibility = Visibility.Collapsed;

                // Populate brush specific
                if (radColorBrush.IsChecked.Value)
                {
                    trkBrushSize.Minimum = _colorBrushSize.Min;
                    trkBrushSize.Maximum = _colorBrushSize.Max;
                    trkBrushSize.Value = _colorBrushSize.Value;

                    lblVelocityMultiplier.Visibility = Visibility.Visible;
                    trkVelocityMultiplier.Visibility = Visibility.Visible;
                    chkVelocityOnLeftDrag.Visibility = Visibility.Visible;
                    chkRandColorPerPixel.Visibility = Visibility.Visible;
                    cboBrushColorType.Visibility = Visibility.Visible;
                }
                else if (radWall.IsChecked.Value)
                {
                    trkBrushSize.Minimum = _wallBrushSize.Min;
                    trkBrushSize.Maximum = _wallBrushSize.Max;
                    trkBrushSize.Value = _wallBrushSize.Value;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void chkUseStandardDiffusion_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                _field.UseCheapDiffusion = !chkUseStandardDiffusion.IsChecked.Value;
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
                    _field.BoundryType = FluidFieldBoundryType2D.Closed;
                }
                else if (radBoundryType_OpenBox.IsChecked.Value)
                {
                    _field.BoundryType = FluidFieldBoundryType2D.Open;
                }
                else if (radBoundryType_WrapBox.IsChecked.Value)
                {
                    _field.BoundryType = FluidFieldBoundryType2D.WrapAround;
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

        private void ColorType_SelectionChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                string selectedType;

                if (cboBrushColorType.SelectedItem != null)
                {
                    selectedType = cboBrushColorType.SelectedItem.ToString().Replace(' ', '_');
                    _brushColorType = (RandColorType)Enum.Parse(typeof(RandColorType), selectedType);
                }

                if (cboRandColorType.SelectedItem != null)      // this was null when setting selected item of cboBrushColorType in the constructor
                {
                    selectedType = cboRandColorType.SelectedItem.ToString().Replace(' ', '_');
                    _randColorType = (RandColorType)Enum.Parse(typeof(RandColorType), selectedType);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
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

        #endregion

        #region Private Methods

        private void ResetField()
        {
            Random rand = StaticRandom.GetRandomForThread();

            // Randomize the colors
            for (int k = 0; k < _field.KSize; k++)      // k = x + y * _xSize
            {
                Color color = GetRandomColor(rand, _randColorType);
                double colorR = color.R / 256d;
                double colorG = color.G / 256d;
                double colorB = color.B / 256d;

                _field.SetInk(0, k, colorR);
                _field.SetInk(1, k, colorG);
                _field.SetInk(2, k, colorB);
            }

            // Set velocity to zero
            for (int k = 0; k < _field.KSize; k++)
            {
                _field.SetVel(k, 0, 0);
                _field.SetBlockedCell(k, false);
            }
        }

        private static void DrawField(WriteableBitmap bitmap, FluidField2D field, bool showBlockedCells)
        {
            Int32Rect rect = new Int32Rect(0, 0, field.XSize, field.YSize);
            int size = rect.Width * rect.Height * 4;
            byte[] pixels = new byte[size];

            double[] reds = field.GetLayer(0);
            double[] greens = field.GetLayer(1);
            double[] blues = field.GetLayer(2);

            var blockedCells = field.Blocked;

            //NOTE: The field's arrays happen to be the same layout as the rect, so there's no need to call field.GetK

            // Setup the pixel array
            for (int i = 0; i < rect.Height * rect.Width; i++)
            {
                if (blockedCells[i] && showBlockedCells)
                {
                    pixels[i * 4 + 0] = 255;   // Blue
                    pixels[i * 4 + 1] = 255;     // Green
                    pixels[i * 4 + 2] = 255;     // Red
                    pixels[i * 4 + 3] = 255;   // Alpha
                }
                else
                {
                    pixels[i * 4 + 0] = ConvertToByteCapped(blues[i] * 256);   // Blue
                    pixels[i * 4 + 1] = ConvertToByteCapped(greens[i] * 256);     // Green
                    pixels[i * 4 + 2] = ConvertToByteCapped(reds[i] * 256);     // Red
                    pixels[i * 4 + 3] = 255;   // Alpha
                }
            }

            bitmap.WritePixels(rect, pixels, rect.Width * 4, 0);
        }

        private static byte ConvertToByteCapped(double value)
        {
            if (value < 0d)
            {
                return 0;
            }
            else if (value > 255d)
            {
                return 255;
            }
            else
            {
                return Convert.ToByte(value);
            }
        }

        /// <summary>
        /// This takes the click point, and returns a point from 0 to 1
        /// </summary>
        private static Point GetScaledPoint(MouseEventArgs e, FrameworkElement relativeTo)
        {
            Point clickPoint = e.GetPosition(relativeTo);

            double width = relativeTo.ActualWidth;
            double height = relativeTo.ActualHeight;

            if (Math3D.IsNearZero(width) || Math3D.IsNearZero(height))
            {
                return new Point(0, 0);
            }

            return new Point(clickPoint.X / width, clickPoint.Y / height);
        }

        /// <summary>
        /// This takes a point from 0 to 1, and returns the corresponding position in the field (what the field calls k)
        /// </summary>
        private static int GetFieldPoint(Point point, FluidField2D field)
        {
            var xy = GetFieldPoint_XY(point, field);

            return field.GetK(xy.Item1, xy.Item2);
        }
        private static Tuple<int, int> GetFieldPoint_XY(Point point, FluidField2D field)
        {
            int x = Convert.ToInt32(Math.Round(point.X * field.XSize));
            if (x < 0)
            {
                x = 0;
            }
            else if (x >= field.XSize)
            {
                x = field.XSize - 1;
            }

            int y = Convert.ToInt32(Math.Round(point.Y * field.XSize));
            if (y < 0)
            {
                y = 0;
            }
            else if (y >= field.YSize)
            {
                y = field.YSize - 1;
            }

            return Tuple.Create(x, y);
        }

        private static int[] GetFieldCircle(Point point, double radiusX, double radiusY, FluidField2D field)
        {
            // Get the box that contains the return circle
            var center = GetFieldPoint_XY(point, field);
            var min = GetFieldPoint_XY(new Point(point.X - radiusX, point.Y - radiusY), field);
            var max = GetFieldPoint_XY(new Point(point.X + radiusX, point.Y + radiusY), field);

            // Get points that are inside the circle
            List<int> retVal = new List<int>();
            double maxDistance = ((radiusX * field.XSize) + (radiusY * field.YSize)) * .5d;     // just take the average.  TODO: see if inside the ellipse
            double maxDistanceSquared = maxDistance * maxDistance;

            for (int x = min.Item1; x <= max.Item1; x++)
            {
                for (int y = min.Item2; y <= max.Item2; y++)
                {
                    double dx = x - center.Item1;
                    double dy = y - center.Item2;
                    double distanceSquared = (dx * dx) + (dy * dy);

                    if (distanceSquared <= maxDistanceSquared)
                    {
                        retVal.Add(field.GetK(x, y));
                    }
                }
            }

            // Exit Function
            return retVal.ToArray();
        }
        private static Tuple<int, double>[] GetFieldCircle(Point point, double radiusX, double radiusY, double valueCenter, double valueEdge, FluidField2D field)
        {
            // Get the box that contains the return circle
            var center = GetFieldPoint_XY(new Point(point.X, point.Y), field);
            var min = GetFieldPoint_XY(new Point(point.X - radiusX, point.Y - radiusY), field);
            var max = GetFieldPoint_XY(new Point(point.X + radiusX, point.Y + radiusY), field);

            // Get points that are inside the circle
            List<Tuple<int, double>> retVal = new List<Tuple<int, double>>();
            Vector radius = new Vector(radiusX * field.XSize, radiusY * field.YSize);
            double maxDistance = ((radiusX * field.XSize) + (radiusY * field.YSize)) * .5d;     // just take the average.  TODO: see if inside the ellipse
            double maxDistanceSquared = maxDistance * maxDistance;

            for (int x = min.Item1; x <= max.Item1; x++)
            {
                for (int y = min.Item2; y <= max.Item2; y++)
                {
                    double dx = x - center.Item1;
                    double dy = y - center.Item2;
                    double distanceSquared = (dx * dx) + (dy * dy);

                    if (distanceSquared <= maxDistanceSquared)
                    {
                        double distance = Math.Sqrt(distanceSquared);

                        retVal.Add(Tuple.Create(
                            field.GetK(x, y),       // coordinates that the field wants
                            UtilityCore.GetScaledValue(valueCenter, valueEdge, 0, maxDistance, distance)      // LERP
                            ));
                    }
                }
            }

            // Exit Function
            return retVal.ToArray();
        }

        private Color GetRandomColor(Random rand, RandColorType colorType)
        {
            int drift = 0;
            Color[] colors = null;
            bool useMap = true;

            switch (colorType)
            {
                case RandColorType.Any:
                    colors = new Color[] { Color.FromRgb(128, 128, 128) };
                    drift = 128;
                    useMap = false;
                    break;

                case RandColorType.Black_Green:
                    drift = 15;
                    break;

                case RandColorType.Black_Orange:
                    drift = 6;
                    break;

                case RandColorType.Black_Purple:
                    drift = 10;
                    break;

                case RandColorType.Red_Tan_Green:
                    drift = 5;
                    break;

                case RandColorType.Tans:
                    drift = 4;
                    break;

                case RandColorType.Skittles:
                    drift = 2;
                    break;

                case RandColorType.Unispew:
                    drift = 2;
                    break;

                case RandColorType.Camo:
                    drift = 5;
                    break;

                case RandColorType.Cold_Beach:
                    drift = 7;
                    break;

                case RandColorType.Mono_Cyan:
                    drift = 0;
                    break;

                default: throw new ApplicationException("Unknown RandColorType: " + colorType.ToString());
            }

            if (useMap)
            {
                colors = _randColors[colorType];
            }

            Color retVal = colors[rand.Next(colors.Length)];

            if (drift > 0)
            {
                retVal = DriftColor(rand, retVal, drift);
            }

            return retVal;
        }
        private static Color DriftColor(Random rand, Color baseColor, int amount)
        {
            int r = rand.Next(baseColor.R - amount, baseColor.R + amount + 1);
            int g = rand.Next(baseColor.G - amount, baseColor.G + amount + 1);
            int b = rand.Next(baseColor.B - amount, baseColor.B + amount + 1);

            if (r < 0) r = 0;
            if (g < 0) g = 0;
            if (b < 0) b = 0;

            if (r > 255) r = 255;
            if (g > 255) g = 255;
            if (b > 255) b = 255;

            return Color.FromArgb(baseColor.A, Convert.ToByte(r), Convert.ToByte(g), Convert.ToByte(b));
        }

        private Vector? GetDragVelocity()
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

            Vector retVal = new Vector(0, 0);

            // Add up all the instantaneous velocities
            for (int cntr = 0; cntr < _mousePointHistory.Count - 1; cntr++)
            {
                retVal += _mousePointHistory[cntr + 1] - _mousePointHistory[cntr];
            }

            // Take the average
            return retVal / (_mousePointHistory.Count - 1);
        }

        #endregion
    }
}
