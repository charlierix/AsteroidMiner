using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.HelperClassesWPF.Primitives3D;

namespace Game.Newt.Testers.Convolution
{
    public partial class ImageFilterPainter : Window
    {
        #region Class: Axis3DProps

        private class Axis3DProps
        {
            /// <summary>
            /// This is the x and y size of a bar
            /// </summary>
            public double BarSize { get; set; }

            public double HalfX { get; set; }
            public double HalfY { get; set; }

            /// <summary>
            /// This is the Z that a bar's value of zero is at
            /// </summary>
            public double AxisOffset { get; set; }

            /// <summary>
            /// This is how tall a bar can get
            /// </summary>
            public double ZHeight { get; set; }

            public Visual3D Visual { get; set; }
        }

        #endregion
        #region Class: Bar3DProps

        private class Bar3DProps
        {
            #region Constructor

            public Bar3DProps(Axis3DProps axis, AllBar3D parent, int index, int x, int y)
            {
                _axis = axis;
                _parent = parent;

                this.Index = index;
                this.X = x;
                this.Y = y;
            }

            #endregion

            private readonly Axis3DProps _axis;
            private readonly AllBar3D _parent;

            public readonly int Index;
            public readonly int X;
            public readonly int Y;

            private double _height = 0d;
            /// <summary>
            /// NOTE: This is from 0 to 1 (or -1 to 1).  The property set will take care of updating HeightTransform according to axis.ZHeight
            /// </summary>
            public double Height
            {
                get
                {
                    return _height;
                }
                set
                {
                    _height = value;

                    // Scale
                    this.HeightTransform.ScaleZ = GetScaledHeight(_height, _axis.ZHeight);

                    //NOTE: Not doing color here, because if this height is the max value, all bars need to change color (so it's up to the caller to adjust
                    //all colors at once
                }
            }

            public GeometryModel3D Model { get; set; }
            public ScaleTransform3D HeightTransform { get; set; }

            internal static double GetScaledHeight(double height, double zHeight)
            {
                double retVal = height * zHeight;

                if (retVal.IsNearZero())
                {
                    retVal = Math3D.GetNearZeroValue(.0001);
                }

                return retVal;
            }
        }

        #endregion
        #region Class: AllBar3D

        private class AllBar3D
        {
            public Bar3DProps[] Bars { get; set; }

            public Model3DGroup Models { get; set; }
            public Visual3D Visual { get; set; }

            private int _hoverIndex = -1;
            public int HoveredIndex { get { return _hoverIndex; } set { _hoverIndex = value; } }

            public Visual3D HoverVisual { get; set; }

            /// <summary>
            /// These are bars that are currently selected
            /// Item1=Index
            /// Item2=SelectionVisual
            /// </summary>
            public List<Tuple<int, Visual3D>> Selected = new List<Tuple<int, Visual3D>>();

            //private int _selectedIndex = -1;
            //public int SelectedIndex { get { return _selectedIndex; } set { _selectedIndex = value; } }

            //public Visual3D SelectedVisual { get; set; }

            public void UpdateColors(bool isZeroToOne, bool isNegativeRedBlue)
            {
                double min = this.Bars.Min(o => o.Height);
                double max = this.Bars.Max(o => o.Height);
                double absMax = Math.Max(Math.Abs(min), Math.Abs(max));

                foreach (var bar in this.Bars)
                {
                    Color color = Convolutions.GetKernelPixelColor(bar.Height, min, max, absMax, isZeroToOne, isNegativeRedBlue);

                    Material material = UtilityWPF.GetUnlitMaterial(color);
                    bar.Model.BackMaterial = material;
                    bar.Model.Material = material;
                }
            }
        }

        #endregion

        #region Events

        public event EventHandler<Convolution2D> SaveRequested = null;

        #endregion

        #region Declaration Section

        private int _width = -1;
        private int _height = -1;

        private double[] _values = null;

        private Axis3DProps _axis = null;
        private AllBar3D _bars = null;

        /// <summary>
        /// This listens to the mouse/keyboard and controls the camera
        /// </summary>
        private TrackBallRoam _trackball = null;

        private readonly DropShadowEffect _errorEffect;

        /// <summary>
        /// Stays true while they are left dragging
        /// </summary>
        private bool _isDraggingMouse = false;
        private bool _isDragRemoving = false;
        private bool _isCtrlPressed = false;
        private bool _isShiftPressed = false;

        private bool _even45ExtraClock = false;
        private bool _even45ExtraCounter = false;

        private bool _isProgramaticallyChangingSettings = false;

        private bool _initialized = false;

        #endregion

        #region Constructor

        public ImageFilterPainter()
        {
            InitializeComponent();

            this.Background = SystemColors.ControlBrush;

            _errorEffect = new DropShadowEffect()
            {
                Color = Colors.Red,
                BlurRadius = 15,
                Direction = 0,
                ShadowDepth = 0,
                Opacity = .8,
            };

            _width = Convert.ToInt32(txtWidth.Text);
            _height = Convert.ToInt32(txtHeight.Text);

            _values = new double[_width * _height];

            // Camera Trackball
            _trackball = new TrackBallRoam(_camera);
            _trackball.EventSource = grdViewPort;		//NOTE:  If this control doesn't have a background color set, the trackball won't see events (I think transparent is ok, just not null)
            _trackball.AllowZoomOnMouseWheel = true;
            _trackball.Mappings.AddRange(TrackBallMapping.GetPrebuilt(TrackBallMapping.PrebuiltMapping.MouseComplete_NoLeft));
            _trackball.MouseWheelScale *= .1;
            //_trackball.GetOrbitRadius += new GetOrbitRadiusHandler(Trackball_GetOrbitRadius);

            _initialized = true;

            RedrawAxis();
            RebuildBars();

            PixelValueChanged();
        }

        #endregion

        #region Public Methods

        public void EditKernel(Convolution2D kernel)
        {
            _isProgramaticallyChangingSettings = true;

            _width = kernel.Width;
            _height = kernel.Height;

            // The kernel passed in is probably unit.  Maximize the height so that it looks nice and 3D
            _values = Convolutions.ToMaximized(kernel.Values).ToArray();       // taking ToArray to ensure it's cloned

            if (kernel.IsNegPos)
            {
                radRangeNegPos.IsChecked = true;
            }
            else
            {
                radRangeZeroPos.IsChecked = true;
            }

            txtWidth.Text = _width.ToString();
            txtHeight.Text = _height.ToString();

            _isProgramaticallyChangingSettings = false;

            RedrawAxis();
            RebuildBars();
            PixelValueChanged();
        }

        #endregion

        #region Event Listeners

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                if (!_initialized)
                {
                    return;
                }

                if (this.SizeToContent == System.Windows.SizeToContent.Manual)
                {
                    // At design time, the window's SizeToContent is set to width and height, and these controls are set to a decent size.
                    //
                    // But if the user manually resizes the window, these now need to fill all available space
                    _viewport.Width = double.NaN;
                    _viewport.Height = double.NaN;

                    canvasInk.Width = double.NaN;
                    canvasInk.Height = double.NaN;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                for (int cntr = 0; cntr < _values.Length; cntr++)
                {
                    _values[cntr] = 0;
                }

                RebuildBars();
                PixelValueChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Random_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool isNegPos = radRangeNegPos.IsChecked.Value;

                Random rand = StaticRandom.GetRandomForThread();

                for (int cntr = 0; cntr < _values.Length; cntr++)
                {
                    if (isNegPos)
                    {
                        _values[cntr] = rand.NextDouble(-1, 1);
                    }
                    else
                    {
                        _values[cntr] = rand.NextDouble();
                    }
                }

                RebuildBars();
                PixelValueChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (this.SaveRequested == null)
                {
                    MessageBox.Show("There is no listener to the save event", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                double[] normalized = Convolutions.ToUnit(_values);

                Convolution2D kernel = new Convolution2D(normalized, _width, _height, radRangeNegPos.IsChecked.Value);

                this.SaveRequested(this, kernel);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void txtWidthHeight_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (!_initialized || _isProgramaticallyChangingSettings)
                {
                    return;
                }

                #region Parse

                // Width
                int width;
                if (!int.TryParse(txtWidth.Text, out width))
                {
                    width = -1;
                }

                if (width > 0)
                {
                    txtWidth.Effect = null;
                }
                else
                {
                    txtWidth.Effect = _errorEffect;
                }

                // Height
                int height;
                if (!int.TryParse(txtHeight.Text, out height))
                {
                    height = -1;
                }

                if (height > 0)
                {
                    txtHeight.Effect = null;
                }
                else
                {
                    txtHeight.Effect = _errorEffect;
                }

                #endregion

                // Get the final values
                if (width <= 0)
                {
                    width = _width;
                }

                if (height <= 0)
                {
                    height = _height;
                }

                if (_width != width || _height != height)
                {
                    #region Change

                    double[] values = new double[width * height];

                    // Copy existing values
                    for (int y = 0; y < Math.Min(_height, height); y++)
                    {
                        int offsetOld = y * _width;
                        int offsetNew = y * width;

                        for (int x = 0; x < Math.Min(_width, width); x++)
                        {
                            values[offsetNew + x] = _values[offsetOld + x];
                        }
                    }

                    // Update
                    _values = values;
                    _width = width;
                    _height = height;

                    RedrawAxis();
                    RebuildBars();

                    PixelValueChanged();

                    #endregion
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Range_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_initialized || _isProgramaticallyChangingSettings)
                {
                    return;
                }

                RedrawAxis();
                RebuildBars();

                PixelValueChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void chkIsRedBlue_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_initialized)
                {
                    return;
                }

                RebuildBars();

                PixelValueChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RotateClockwise90_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RotateValues(true, true, false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void RotateCounterClockwise90_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RotateValues(false, true, false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void RotateClockwise45_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_width != _height)
                {
                    MessageBox.Show("Can't do 45 degree rotation of a non square image", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                RotateValues(true, false, _even45ExtraClock);

                if (_width % 2 == 0)
                {
                    _even45ExtraClock = !_even45ExtraClock;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void RotateCounterClockwise45_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_width != _height)
                {
                    MessageBox.Show("Can't do 45 degree rotation of a non square image", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                RotateValues(false, false, _even45ExtraCounter);

                if (_width % 2 == 0)
                {
                    _even45ExtraCounter = !_even45ExtraCounter;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TranslateLeft_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //TODO: Use AxisFor

                for (int y = 0; y < _height; y++)
                {
                    int yIndex = y * _width;

                    for (int x = 0; x < _width - 1; x++)
                    {
                        _values[yIndex + x] = _values[yIndex + x + 1];
                    }

                    _values[yIndex + _width - 1] = 0;
                }

                RebuildBars();
                PixelValueChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void TranslateRight_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                for (int y = 0; y < _height; y++)
                {
                    int yIndex = y * _width;

                    for (int x = _width - 1; x > 0; x--)
                    {
                        _values[yIndex + x] = _values[yIndex + x - 1];
                    }

                    _values[yIndex] = 0;
                }

                RebuildBars();
                PixelValueChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void TranslateUp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                for (int y = 0; y < _height - 1; y++)
                {
                    int yIndex0 = y * _width;
                    int yIndex1 = (y + 1) * _width;

                    for (int x = 0; x < _width; x++)
                    {
                        _values[yIndex0 + x] = _values[yIndex1 + x];
                    }
                }

                int yIndex = (_height - 1) * _width;

                for (int x = 0; x < _width; x++)
                {
                    _values[yIndex + x] = 0;
                }

                RebuildBars();
                PixelValueChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void TranslateDown_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                for (int y = _height - 1; y > 0; y--)
                {
                    int yIndex0 = y * _width;
                    int yIndex1 = (y - 1) * _width;

                    for (int x = 0; x < _width; x++)
                    {
                        _values[yIndex0 + x] = _values[yIndex1 + x];
                    }
                }

                for (int x = 0; x < _width; x++)
                {
                    _values[x] = 0;
                }

                RebuildBars();
                PixelValueChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Invert_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool isNegPos = radRangeNegPos.IsChecked.Value;

                for (int cntr = 0; cntr < _values.Length; cntr++)
                {
                    if (isNegPos)
                    {
                        _values[cntr] *= -1d;
                    }
                    else
                    {
                        _values[cntr] = .5 - (_values[cntr] - .5);
                    }
                }

                RebuildBars();
                PixelValueChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ToUnit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _values = Convolutions.ToUnit(_values);

                RebuildBars();
                PixelValueChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _values = Convolutions.ToMaximized(_values);

                RebuildBars();
                PixelValueChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (!_initialized)
                {
                    return;
                }

                if (!(e.Source is TabControl))
                {
                    return;
                }

                if (tab3D.IsSelected)
                {
                    grdSettings3D.Visibility = Visibility.Visible;
                }
                else if (tabPainter.IsSelected)
                {
                    grdSettings3D.Visibility = Visibility.Collapsed;
                }
                else
                {
                    grdSettings3D.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                switch (e.Key)
                {
                    case Key.LeftCtrl:
                    case Key.RightCtrl:
                        _isCtrlPressed = true;
                        break;

                    case Key.LeftShift:
                    case Key.RightShift:
                        _isShiftPressed = true;
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            try
            {
                switch (e.Key)
                {
                    case Key.LeftCtrl:
                    case Key.RightCtrl:
                        _isCtrlPressed = false;
                        break;

                    case Key.LeftShift:
                    case Key.RightShift:
                        _isShiftPressed = false;
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void grdViewPort_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (_bars == null)
                {
                    return;
                }

                Bar3DProps overBar = GetMouseOverBar(e);

                if (_isDraggingMouse && overBar != null)
                {
                    #region Selected/Unselect Bar

                    bool isAlreadySelected = _bars.Selected.Any(o => o.Item1 == overBar.Index);

                    if (_isDragRemoving)
                    {
                        if (isAlreadySelected)
                        {
                            RemoveSelected(overBar);
                        }
                    }
                    else
                    {
                        if (!isAlreadySelected)
                        {
                            AddSelected(overBar);
                        }
                    }

                    #endregion
                }

                #region Hover Visual

                if (overBar != null && _bars.HoveredIndex == overBar.Index)
                {
                    // It's already hovered over
                    return;
                }

                // Clear existing
                if (_bars.HoverVisual != null)
                {
                    _viewport.Children.Remove(_bars.HoverVisual);
                    _bars.HoverVisual = null;
                }

                _bars.HoveredIndex = -1;

                if (overBar == null)
                {
                    return;
                }

                // Get a new visual
                Visual3D visual = GetHoverVisual(_axis, overBar, true);

                // Store it
                _viewport.Children.Add(visual);
                _bars.HoveredIndex = overBar.Index;
                _bars.HoverVisual = visual;

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void grdViewPort_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.LeftButton != MouseButtonState.Pressed)
                {
                    return;
                }

                _isDraggingMouse = true;
                _isDragRemoving = false;

                if (_bars == null)
                {
                    return;
                }

                Bar3DProps overBar = GetMouseOverBar(e);

                // Clear existing
                if (!_isShiftPressed && !_isCtrlPressed)
                {
                    ClearSelected();
                }

                if (overBar == null)
                {
                    return;
                }

                bool isAlreadySelected = _bars.Selected.Any(o => o.Item1 == overBar.Index);
                if (_isCtrlPressed && !_isShiftPressed && isAlreadySelected)
                {
                    RemoveSelected(overBar);
                    _isDragRemoving = true;
                }
                else if (!isAlreadySelected)
                {
                    AddSelected(overBar);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void grdViewPort_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.LeftButton != MouseButtonState.Pressed)
                {
                    _isDraggingMouse = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void txtHeight3D_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (_isProgramaticallyChangingSettings || _bars == null || _bars.Selected.Count == 0)
                {
                    return;
                }

                double value;
                if (!double.TryParse(txtHeight3D.Text, out value))
                {
                    txtHeight3D.Effect = _errorEffect;
                    return;
                }

                double min, max;
                if (radRangeNegPos.IsChecked.Value)
                {
                    min = -1;
                    max = 1;
                }
                else if (radRangeZeroPos.IsChecked.Value)
                {
                    min = 0;
                    max = 0;
                }
                else
                {
                    MessageBox.Show("Unknown range", this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (value < min || value > max)
                {
                    txtHeight3D.Effect = _errorEffect;
                    return;
                }

                txtHeight3D.Effect = null;

                UpdateBarHeight(value);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void trkHeight3D_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (_isProgramaticallyChangingSettings || _bars == null || _bars.Selected.Count == 0)
                {
                    return;
                }

                UpdateBarHeight(trkHeight3D.Value);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void UpdateBarHeight(double height)
        {
            foreach (var selected in _bars.Selected)
            {
                var bar = _bars.Bars[selected.Item1];

                bar.Height = height;
                _values[bar.Index] = height;
            }

            _bars.UpdateColors(radRangeZeroPos.IsChecked.Value, chkIsRedBlue.IsChecked.Value);

            PixelValueChanged();

            _isProgramaticallyChangingSettings = true;

            trkHeight3D.Value = height;
            txtHeight3D.Text = height.ToStringSignificantDigits(3);

            _isProgramaticallyChangingSettings = false;
        }

        #endregion

        #region Private Methods

        private void ClearSelected()
        {
            if (_bars != null)
            {
                _viewport.Children.RemoveAll(_bars.Selected.Select(o => o.Item2));
                _bars.Selected.Clear();
            }

            DisableSelectedControls();
        }
        private void AddSelected(Bar3DProps bar)
        {
            // Get a new visual
            Visual3D visual = GetHoverVisual(_axis, bar, false);

            // Store it
            _viewport.Children.Add(visual);
            _bars.Selected.Add(Tuple.Create(bar.Index, visual));

            #region Update controls

            _isProgramaticallyChangingSettings = true;

            // Textbox
            txtHeight3D.Text = bar.Height.ToStringSignificantDigits(3);

            // Slider
            if (radRangeZeroPos.IsChecked.Value)
            {
                trkHeight3D.Minimum = 0;
                trkHeight3D.Maximum = 1;
            }
            else
            {
                trkHeight3D.Minimum = -1;
                trkHeight3D.Maximum = 1;
            }

            trkHeight3D.Value = bar.Height;

            grdSettings3D.IsEnabled = true;

            _isProgramaticallyChangingSettings = false;

            #endregion
        }
        private void RemoveSelected(Bar3DProps bar)
        {
            for (int cntr = 0; cntr < _bars.Selected.Count; cntr++)
            {
                if (_bars.Selected[cntr].Item1 == bar.Index)
                {
                    _viewport.Children.Remove(_bars.Selected[cntr].Item2);
                    _bars.Selected.RemoveAt(cntr);
                    break;
                }
            }

            if (_bars.Selected.Count == 0)
            {
                DisableSelectedControls();
            }
        }
        private void DisableSelectedControls()
        {
            _isProgramaticallyChangingSettings = true;

            txtHeight3D.Text = "";
            trkHeight3D.Value = (trkHeight3D.Minimum + trkHeight3D.Maximum) / 2;
            grdSettings3D.IsEnabled = false;

            _isProgramaticallyChangingSettings = false;
        }

        private void RotateValues(bool isClockwise, bool is90, bool isEven45Extra)
        {
            if (is90)
            {
                _values = Convolutions.Rotate_90(_values, _width, _height, isClockwise);
            }
            else
            {
                if (_width != _height)
                {
                    throw new InvalidOperationException("Can't do 45 degree rotation of a non square image");
                }

                _values = Convolutions.Rotate_45(_values, _width, isClockwise, isEven45Extra);
            }

            UtilityCore.Swap(ref _width, ref _height);

            if (_width != _height)
            {
                _isProgramaticallyChangingSettings = true;

                txtWidth.Text = _width.ToString();
                txtHeight.Text = _height.ToString();

                _isProgramaticallyChangingSettings = false;

                RedrawAxis();
            }

            RebuildBars();
            PixelValueChanged();
        }

        private void PixelValueChanged()
        {
            Convolution2D kernel = new Convolution2D(_values, _width, _height, radRangeNegPos.IsChecked.Value);
            bool isRedBlue = chkIsRedBlue.IsChecked.Value;

            // Show Kernel
            BitmapSource kernelBitmap = Convolutions.GetKernelBitmap(kernel, isNegativeRedBlue: isRedBlue);
            imagePreview.Source = kernelBitmap;
            imagePreview.Width = kernelBitmap.PixelWidth;
            imagePreview.Height = kernelBitmap.PixelHeight;
        }

        private void RedrawAxis_WRONGY()
        {
            const double AXISRADIUS = .015;
            const double CONERADIUS = AXISRADIUS * 3;
            const double CONEHEIGHT = AXISRADIUS * 7;

            if (_axis != null)
            {
                _viewport.Children.Remove(_axis.Visual);
                _axis = null;
            }

            Axis3DProps axis = new Axis3DProps();

            #region Calculate sizes

            axis.BarSize = 1d;

            axis.HalfX = (_width * axis.BarSize) / 2d;
            axis.HalfY = (_height * axis.BarSize) / 2d;

            axis.ZHeight = Math.Min(Math3D.Avg(_width, _height), 2);

            if (radRangeNegPos.IsChecked.Value)
            {
                axis.AxisOffset = 0;
            }
            else
            {
                // It doesn't look better being double height
                axis.AxisOffset = axis.ZHeight * -.66666666;
                axis.ZHeight *= 1.3333333;
            }

            #endregion

            MaterialGroup material_Axis = new MaterialGroup();
            material_Axis.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("72BF77"))));
            material_Axis.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("50FFFFFF")), 2));

            Model3DGroup models = new Model3DGroup();

            #region X Axis

            GeometryModel3D model = new GeometryModel3D() { Material = material_Axis, BackMaterial = material_Axis, };

            model.Geometry = UtilityWPF.GetCylinder_AlongX(20, AXISRADIUS, axis.HalfX * 2);

            model.Transform = new TranslateTransform3D(0, -axis.HalfY, axis.AxisOffset);

            models.Children.Add(model);

            #endregion
            #region X Cone

            model = new GeometryModel3D() { Material = material_Axis, BackMaterial = material_Axis, };

            model.Geometry = UtilityWPF.GetCone_AlongX(20, CONERADIUS, CONEHEIGHT);

            model.Transform = new TranslateTransform3D(axis.HalfX + (CONEHEIGHT / 2), -axis.HalfY, axis.AxisOffset);

            models.Children.Add(model);

            #endregion

            #region Y Axis

            model = new GeometryModel3D() { Material = material_Axis, BackMaterial = material_Axis, };

            model.Geometry = UtilityWPF.GetCylinder_AlongX(20, AXISRADIUS, axis.HalfY * 2);

            Transform3DGroup transform = new Transform3DGroup();
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 90)));
            transform.Children.Add(new TranslateTransform3D(-axis.HalfX, 0, axis.AxisOffset));
            model.Transform = transform;

            models.Children.Add(model);

            #endregion
            #region Y Cone

            model = new GeometryModel3D() { Material = material_Axis, BackMaterial = material_Axis, };

            model.Geometry = UtilityWPF.GetCone_AlongX(20, CONERADIUS, CONEHEIGHT);

            transform = new Transform3DGroup();
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 90)));
            transform.Children.Add(new TranslateTransform3D(-axis.HalfX, axis.HalfY + (CONEHEIGHT / 2), axis.AxisOffset));
            model.Transform = transform;

            models.Children.Add(model);

            #endregion

            #region Z Axis

            model = new GeometryModel3D() { Material = material_Axis, BackMaterial = material_Axis, };

            model.Geometry = UtilityWPF.GetCylinder_AlongX(20, AXISRADIUS, axis.ZHeight);

            transform = new Transform3DGroup();
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90)));
            transform.Children.Add(new TranslateTransform3D(-axis.HalfX, -axis.HalfY, axis.AxisOffset + (axis.ZHeight / 2)));
            model.Transform = transform;

            models.Children.Add(model);

            #endregion
            #region Z Cone

            model = new GeometryModel3D() { Material = material_Axis, BackMaterial = material_Axis, };

            model.Geometry = UtilityWPF.GetCone_AlongX(20, CONERADIUS, CONEHEIGHT);

            transform = new Transform3DGroup();
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), -90)));
            transform.Children.Add(new TranslateTransform3D(-axis.HalfX, -axis.HalfY, axis.AxisOffset + axis.ZHeight + (CONEHEIGHT / 2)));
            model.Transform = transform;

            models.Children.Add(model);

            #endregion

            #region Origin Dot

            model = new GeometryModel3D() { Material = material_Axis, BackMaterial = material_Axis, };

            model.Geometry = UtilityWPF.GetSphere_Ico(AXISRADIUS, 4, true);

            model.Transform = new TranslateTransform3D(-axis.HalfX, -axis.HalfY, axis.AxisOffset);

            models.Children.Add(model);

            #endregion

            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = models;
            axis.Visual = visual;

            _axis = axis;
            _viewport.Children.Add(_axis.Visual);
        }
        private void RebuildBars_WRONGY()
        {
            if (_axis == null)
            {
                return;
            }

            if (_bars != null)
            {
                _viewport.Children.Remove(_bars.Visual);
                _bars = null;
            }

            AllBar3D bars = new AllBar3D();

            bars.Models = new Model3DGroup();

            List<Bar3DProps> individualBars = new List<Bar3DProps>();

            for (int y = 0; y < _height; y++)
            {
                int indexOffset = y * _width;

                for (int x = 0; x < _width; x++)
                {
                    Bar3DProps bar = new Bar3DProps(_axis, bars, indexOffset + x, x, y);

                    bar.Model = new GeometryModel3D();

                    //NOTE: Material is updated by bar.Height set

                    double xPos = -_axis.HalfX + (x * _axis.BarSize);
                    double yPos = -_axis.HalfY + (y * _axis.BarSize);

                    bar.Model.Geometry = UtilityWPF.GetCube(new Point3D(xPos, yPos, 0), new Point3D(xPos + _axis.BarSize, yPos + _axis.BarSize, 1));

                    Transform3DGroup transform = new Transform3DGroup();
                    bar.HeightTransform = new ScaleTransform3D(1, 1, 1);
                    transform.Children.Add(bar.HeightTransform);
                    transform.Children.Add(new TranslateTransform3D(0, 0, _axis.AxisOffset));
                    bar.Model.Transform = transform;

                    bar.Height = _values[indexOffset + x];

                    bars.Models.Children.Add(bar.Model);
                    individualBars.Add(bar);
                }
            }

            bars.Bars = individualBars.ToArray();



            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = bars.Models;
            bars.Visual = visual;

            _bars = bars;
            _viewport.Children.Add(_bars.Visual);
        }

        //NOTE: Making Y go down instead of up to match the way the 2D image is drawn
        private void RedrawAxis()
        {
            const double AXISRADIUS = .015;
            const double CONERADIUS = AXISRADIUS * 3;
            const double CONEHEIGHT = AXISRADIUS * 7;

            if (_axis != null)
            {
                _viewport.Children.Remove(_axis.Visual);
                _axis = null;
            }

            Axis3DProps axis = new Axis3DProps();

            #region Calculate sizes

            axis.BarSize = 1d;

            axis.HalfX = (_width * axis.BarSize) / 2d;
            axis.HalfY = (_height * axis.BarSize) / 2d;

            axis.ZHeight = 1;

            if (radRangeNegPos.IsChecked.Value)
            {
                axis.AxisOffset = 0;
            }
            else
            {
                // It doesn't look better being double height
                axis.AxisOffset = axis.ZHeight * -.66666666;
                axis.ZHeight *= 1.3333333;
            }

            #endregion

            MaterialGroup material_Axis = new MaterialGroup();
            material_Axis.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("72BF77"))));
            material_Axis.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("50FFFFFF")), 2));

            Model3DGroup models = new Model3DGroup();

            #region X Axis

            GeometryModel3D model = new GeometryModel3D() { Material = material_Axis, BackMaterial = material_Axis, };

            model.Geometry = UtilityWPF.GetCylinder_AlongX(20, AXISRADIUS, axis.HalfX * 2);

            model.Transform = new TranslateTransform3D(0, axis.HalfY, axis.AxisOffset);

            models.Children.Add(model);

            #endregion
            #region X Cone

            model = new GeometryModel3D() { Material = material_Axis, BackMaterial = material_Axis, };

            model.Geometry = UtilityWPF.GetCone_AlongX(20, CONERADIUS, CONEHEIGHT);

            model.Transform = new TranslateTransform3D(axis.HalfX + (CONEHEIGHT / 2), axis.HalfY, axis.AxisOffset);

            models.Children.Add(model);

            #endregion

            #region Y Axis

            model = new GeometryModel3D() { Material = material_Axis, BackMaterial = material_Axis, };

            model.Geometry = UtilityWPF.GetCylinder_AlongX(20, AXISRADIUS, axis.HalfY * 2);

            Transform3DGroup transform = new Transform3DGroup();
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 90)));
            transform.Children.Add(new TranslateTransform3D(-axis.HalfX, 0, axis.AxisOffset));
            model.Transform = transform;

            models.Children.Add(model);

            #endregion
            #region Y Cone

            model = new GeometryModel3D() { Material = material_Axis, BackMaterial = material_Axis, };

            model.Geometry = UtilityWPF.GetCone_AlongX(20, CONERADIUS, CONEHEIGHT);

            transform = new Transform3DGroup();
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), -90)));
            transform.Children.Add(new TranslateTransform3D(-axis.HalfX, -axis.HalfY - (CONEHEIGHT / 2), axis.AxisOffset));
            model.Transform = transform;

            models.Children.Add(model);

            #endregion

            #region Z Axis

            model = new GeometryModel3D() { Material = material_Axis, BackMaterial = material_Axis, };

            model.Geometry = UtilityWPF.GetCylinder_AlongX(20, AXISRADIUS, axis.ZHeight);

            transform = new Transform3DGroup();
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90)));
            transform.Children.Add(new TranslateTransform3D(-axis.HalfX, axis.HalfY, axis.AxisOffset + (axis.ZHeight / 2)));
            model.Transform = transform;

            models.Children.Add(model);

            #endregion
            #region Z Cone

            model = new GeometryModel3D() { Material = material_Axis, BackMaterial = material_Axis, };

            model.Geometry = UtilityWPF.GetCone_AlongX(20, CONERADIUS, CONEHEIGHT);

            transform = new Transform3DGroup();
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), -90)));
            transform.Children.Add(new TranslateTransform3D(-axis.HalfX, axis.HalfY, axis.AxisOffset + axis.ZHeight + (CONEHEIGHT / 2)));
            model.Transform = transform;

            models.Children.Add(model);

            #endregion

            #region Origin Dot

            model = new GeometryModel3D() { Material = material_Axis, BackMaterial = material_Axis, };

            model.Geometry = UtilityWPF.GetSphere_Ico(AXISRADIUS, 4, true);

            model.Transform = new TranslateTransform3D(-axis.HalfX, axis.HalfY, axis.AxisOffset);

            models.Children.Add(model);

            #endregion

            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = models;
            axis.Visual = visual;

            _axis = axis;
            _viewport.Children.Add(_axis.Visual);
        }
        private void RebuildBars()
        {
            if (_axis == null)
            {
                return;
            }

            if (_bars != null)
            {
                _viewport.Children.Remove(_bars.Visual);

                if (_bars.HoverVisual != null)
                {
                    _viewport.Children.Remove(_bars.HoverVisual);
                }

                ClearSelected();

                _bars = null;
            }

            AllBar3D bars = new AllBar3D();

            bars.Models = new Model3DGroup();

            List<Bar3DProps> individualBars = new List<Bar3DProps>();

            for (int y = 0; y < _height; y++)
            {
                int indexOffset = y * _width;

                for (int x = 0; x < _width; x++)
                {
                    int index = indexOffset + x;

                    Bar3DProps bar = RebuildBars_Bar(_axis, bars, index, x, y, _values[index]);

                    bars.Models.Children.Add(bar.Model);
                    individualBars.Add(bar);
                }
            }

            bars.Bars = individualBars.ToArray();

            bars.UpdateColors(radRangeZeroPos.IsChecked.Value, chkIsRedBlue.IsChecked.Value);

            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = bars.Models;
            bars.Visual = visual;

            _bars = bars;
            _viewport.Children.Add(_bars.Visual);
        }
        private static Bar3DProps RebuildBars_Bar(Axis3DProps axis, AllBar3D bars, int index, int x, int y, double value)
        {
            Bar3DProps retVal = new Bar3DProps(axis, bars, index, x, y);

            retVal.Model = new GeometryModel3D();

            //NOTE: Material is updated by bars.UpdateColors

            double xPos = -axis.HalfX + (x * axis.BarSize);
            double yPos = axis.HalfY - (y * axis.BarSize);

            retVal.Model.Geometry = UtilityWPF.GetCube(new Point3D(xPos, yPos, 0), new Point3D(xPos + axis.BarSize, yPos - axis.BarSize, 1));

            Transform3DGroup transform = new Transform3DGroup();
            retVal.HeightTransform = new ScaleTransform3D(1, 1, 1);
            transform.Children.Add(retVal.HeightTransform);
            transform.Children.Add(new TranslateTransform3D(0, 0, axis.AxisOffset));
            retVal.Model.Transform = transform;

            retVal.Height = value;

            return retVal;
        }

        private static Visual3D GetHoverVisual(Axis3DProps axis, Bar3DProps bar, bool isHover)
        {
            //TODO:
            //  probably a flattened semitransparent dome over top and bottom of bar
            //  or maybe a semitransparent plate that hovers over top and bottom of bar

            double halfBarSize = axis.BarSize / 2d;

            double x = -axis.HalfX + (bar.X * axis.BarSize) + halfBarSize;
            double y = axis.HalfY - (bar.Y * axis.BarSize) - halfBarSize;

            double zPos = axis.AxisOffset + (axis.ZHeight * 1.5);
            double zNeg;
            if (axis.AxisOffset.IsNearZero())
            {
                zNeg = -zPos;
            }
            else
            {
                zNeg = axis.AxisOffset - (axis.ZHeight * .1);
            }

            ScreenSpaceLines3D retVal = new ScreenSpaceLines3D();

            if (isHover)
            {
                retVal.Color = UtilityWPF.AlphaBlend(Colors.LimeGreen, Colors.Gray, .33);
                retVal.Thickness = 1.5;
            }
            else
            {
                retVal.Color = Colors.LimeGreen;
                retVal.Thickness = 4;
            }

            retVal.AddLine(new Point3D(x, y, zNeg), new Point3D(x, y, zPos));

            return retVal;
        }

        private Bar3DProps GetMouseOverBar(MouseEventArgs e)
        {
            // Fire a ray at the mouse point
            Point clickPoint = e.GetPosition(grdViewPort);

            Visual3D[] ignoreVisuals = new[] { _axis.Visual };

            RayHitTestParameters clickRay;
            List<MyHitTestResult> hits = UtilityWPF.CastRay(out clickRay, clickPoint, grdViewPort, _camera, _viewport, true, ignoreVisuals);

            // See which bar it's intersecting (if any)
            //NOTE: Outer loop needs to be hits, because they are sorted by distance from camera
            foreach (var hit in hits)
            {
                Model3D hitModel = hit.ModelHit.ModelHit;

                foreach (var bar in _bars.Bars)
                {
                    if (bar.Model == hitModel)
                    {
                        return bar;
                    }
                }
            }

            return null;
        }

        #endregion
    }
}
