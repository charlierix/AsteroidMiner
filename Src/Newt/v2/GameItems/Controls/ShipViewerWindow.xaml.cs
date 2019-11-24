using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Game.Newt.v2.GameItems.Controls
{
    //NOTE: Some of this code was copied from the BrainTester window
    public partial class ShipViewerWindow : Window
    {
        #region class: NeuralContainerVisual

        private class NeuralContainerVisual
        {
            public INeuronContainer Container = null;
            public List<Tuple<INeuron, SolidColorBrush>> Neurons = new List<Tuple<INeuron, SolidColorBrush>>();
            public Visual3D NeuronVisual = null;
        }

        #endregion
        #region class: NeuralVisuals

        private class NeuralVisuals
        {
            public List<NeuralContainerVisual> Containers = new List<NeuralContainerVisual>();

            // This doesn't use a visual for each link, but they are separated by color
            //TODO: May want an option to set the opacity based on the current weight of the feeder neuron (so that it's nearly invisible if the neuron isn't firing)
            public List<Visual3D> Links = new List<Visual3D>();
        }

        #endregion

        #region class: PartVisuals

        private class PartVisuals
        {
            //TODO: If there is never going to be more than one property, then get rid of this class
            public Visual3D Visual = null;
        }

        #endregion

        #region class: ItemColors

        public class ItemColors
        {
            public Color Neuron_Zero_NegPos = UtilityWPF.ColorFromHex("20808080");
            public Color Neuron_Zero_ZerPos = UtilityWPF.ColorFromHex("205E88D1");

            public Color Neuron_One_ZerPos = UtilityWPF.ColorFromHex("326CD1");

            public Color Neuron_NegOne_NegPos = UtilityWPF.ColorFromHex("D63633");
            public Color Neuron_One_NegPos = UtilityWPF.ColorFromHex("25994A");

            public Color Link_Negative = UtilityWPF.ColorFromHex("40FC0300");
            public Color Link_Positive = UtilityWPF.ColorFromHex("4000B237");

            public Color GetNeuronColor(INeuron neuron)
            {
                if (neuron.IsPositiveOnly)
                {
                    return UtilityWPF.AlphaBlend(Neuron_One_ZerPos, Neuron_Zero_ZerPos, neuron.Value);
                }
                else
                {
                    double weight = neuron.Value;     // need to grab the value locally, because it could be modified from a different thread

                    if (weight < 0)
                    {
                        return UtilityWPF.AlphaBlend(Neuron_NegOne_NegPos, Neuron_Zero_NegPos, Math.Abs(weight));
                    }
                    else
                    {
                        return UtilityWPF.AlphaBlend(Neuron_One_NegPos, Neuron_Zero_NegPos, weight);
                    }
                }
            }
        }

        #endregion

        #region Declaration Section

        private const int MINSIZE = 40;

        private readonly Bot _bot;        // this could be null (if they use the parts overload)
        private readonly PartBase[] _parts;
        private readonly NeuralUtility.ContainerOutput[] _neuronLinks;

        private double _partsRadius;
        private Vector3D _centerOffset;

        private readonly ItemColors _itemColors = new ItemColors();

        private DispatcherTimer _timer;

        private ShipProgressBarManager _progressBars = null;

        private NeuralVisuals _neuronVisuals = null;
        private PartVisuals _partVisuals = null;

        private TrackBallRoam _trackball = null;

        private Visual3D[] _startingVisualsBack;
        private Visual3D[] _startingVisualsFore;

        private bool _isResizing = false;
        private Point _resizeStart;

        private bool _isInitialized = false;

        #endregion

        #region Constructor

        public ShipViewerWindow(Bot bot, Window owner)
        {
            InitializeComponent();

            _bot = bot;
            _parts = bot.Parts.ToArray();
            _neuronLinks = bot.NeuronLinks;

            // The model coords may not be centered, so this is how much to move the parts so they appear centered
            Point3D minPoint, maxPoint;
            bot.PhysicsBody.GetAABB(out minPoint, out maxPoint);
            minPoint = _bot.PhysicsBody.PositionFromWorld(minPoint);
            maxPoint = _bot.PhysicsBody.PositionFromWorld(maxPoint);

            Constructor_Finish((minPoint, maxPoint), owner);
        }
        /// <summary>
        /// This overload is for bots that don't derive from ship, but do contain parts.  This will show the parts, and won't show
        /// any ship specific visuals
        /// </summary>
        public ShipViewerWindow(PartBase[] parts, NeuralUtility.ContainerOutput[] neuronLinks, Window owner)
        {
            InitializeComponent();

            _bot = null;
            _parts = parts;
            _neuronLinks = neuronLinks;

            var aabb = PartBase.GetAABB(_parts);

            Constructor_Finish(aabb, owner);
        }

        private void Constructor_Finish((Point3D min, Point3D max) aabb, Window owner)
        {
            _centerOffset = (aabb.min + ((aabb.max - aabb.min) / 2d)).ToVector() * -1d;
            _partsRadius = (aabb.max - aabb.min).Length / 2d;

            _startingVisualsBack = _viewportBack.Children.ToArray();
            _startingVisualsFore = _viewport.Children.ToArray();

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(100);
            _timer.Tick += new EventHandler(Timer_Tick);

            _camera.Changed += new EventHandler(Camera_Changed);

            _progressBars = new ShipProgressBarManager(pnlProgressBars);
            _progressBars.Bot = _bot;

            // This makes this window stay over top of the parent, but leaving TopMost as false so it's not on top of everything
            Owner = owner;

            _isInitialized = true;
        }

        #endregion

        #region Public Properties

        // This is the outermost border
        public Brush PopupBackground
        {
            get { return (Brush)GetValue(PopupBackgroundProperty); }
            set { SetValue(PopupBackgroundProperty, value); }
        }
        public static readonly DependencyProperty PopupBackgroundProperty = DependencyProperty.Register("PopupBackground", typeof(Brush), typeof(ShipViewerWindow), new UIPropertyMetadata(new SolidColorBrush(Color.FromArgb(48, 0, 0, 0))));

        public Brush PopupBorder
        {
            get { return (Brush)GetValue(PopupBorderProperty); }
            set { SetValue(PopupBorderProperty, value); }
        }
        public static readonly DependencyProperty PopupBorderProperty = DependencyProperty.Register("PopupBorder", typeof(Brush), typeof(ShipViewerWindow), new UIPropertyMetadata(new SolidColorBrush(Color.FromArgb(96, 0, 0, 0))));

        // This is the 3D viewport
        public Brush ViewportBackground
        {
            get { return (Brush)GetValue(ViewportBackgroundProperty); }
            set { SetValue(ViewportBackgroundProperty, value); }
        }
        public static readonly DependencyProperty ViewportBackgroundProperty = DependencyProperty.Register("ViewportBackground", typeof(Brush), typeof(ShipViewerWindow), new UIPropertyMetadata(new SolidColorBrush(Color.FromArgb(48, 255, 255, 255))));

        public Brush ViewportBorder
        {
            get { return (Brush)GetValue(ViewportBorderProperty); }
            set { SetValue(ViewportBorderProperty, value); }
        }
        public static readonly DependencyProperty ViewportBorderProperty = DependencyProperty.Register("ViewportBorder", typeof(Brush), typeof(ShipViewerWindow), new UIPropertyMetadata(new SolidColorBrush(Color.FromArgb(96, 255, 255, 255))));

        // This is for the various options panels
        public Brush PanelBackground
        {
            get { return (Brush)GetValue(PanelBackgroundProperty); }
            set { SetValue(PanelBackgroundProperty, value); }
        }
        public static readonly DependencyProperty PanelBackgroundProperty = DependencyProperty.Register("PanelBackground", typeof(Brush), typeof(ShipViewerWindow), new UIPropertyMetadata(new SolidColorBrush(Color.FromArgb(128, 160, 160, 160))));

        public Brush PanelBorder
        {
            get { return (Brush)GetValue(PanelBorderProperty); }
            set { SetValue(PanelBorderProperty, value); }
        }
        public static readonly DependencyProperty PanelBorderProperty = DependencyProperty.Register("PanelBorder", typeof(Brush), typeof(ShipViewerWindow), new UIPropertyMetadata(new SolidColorBrush(Color.FromArgb(160, 128, 128, 128))));

        // Close Button
        public Brush CloseForegroud
        {
            get { return (Brush)GetValue(CloseForegroudProperty); }
            set { SetValue(CloseForegroudProperty, value); }
        }
        public static readonly DependencyProperty CloseForegroudProperty = DependencyProperty.Register("CloseForegroud", typeof(Brush), typeof(ShipViewerWindow), new UIPropertyMetadata(new SolidColorBrush(Color.FromArgb(80, 255, 255, 255))));

        public Brush CloseBackground
        {
            get { return (Brush)GetValue(CloseBackgroundProperty); }
            set { SetValue(CloseBackgroundProperty, value); }
        }
        public static readonly DependencyProperty CloseBackgroundProperty = DependencyProperty.Register("CloseBackground", typeof(Brush), typeof(ShipViewerWindow), new UIPropertyMetadata(new SolidColorBrush(Color.FromArgb(30, 128, 128, 128))));

        public Brush CloseBorder
        {
            get { return (Brush)GetValue(CloseBorderProperty); }
            set { SetValue(CloseBorderProperty, value); }
        }
        public static readonly DependencyProperty CloseBorderProperty = DependencyProperty.Register("CloseBorder", typeof(Brush), typeof(ShipViewerWindow), new UIPropertyMetadata(new SolidColorBrush(Color.FromArgb(50, 128, 128, 128))));

        public Brush CloseBackground_Highlight
        {
            get { return (Brush)GetValue(CloseBackground_HighlightProperty); }
            set { SetValue(CloseBackground_HighlightProperty, value); }
        }
        public static readonly DependencyProperty CloseBackground_HighlightProperty = DependencyProperty.Register("CloseBackground_Highlight", typeof(Brush), typeof(ShipViewerWindow), new UIPropertyMetadata(new SolidColorBrush(Color.FromArgb(128, 192, 192, 192))));

        public Brush CloseBorder_Highlight
        {
            get { return (Brush)GetValue(CloseBorder_HighlightProperty); }
            set { SetValue(CloseBorder_HighlightProperty, value); }
        }
        public static readonly DependencyProperty CloseBorder_HighlightProperty = DependencyProperty.Register("CloseBorder_Highlight", typeof(Brush), typeof(ShipViewerWindow), new UIPropertyMetadata(new SolidColorBrush(Color.FromArgb(192, 192, 192, 192))));

        #endregion

        #region Public Methods

        public void SetColorTheme_Light()
        {
            // This is the main window's background (this will be the window's border region)
            PopupBackground = UtilityWPF.BrushFromHex("C0B0B0B0");
            PopupBorder = UtilityWPF.BrushFromHex("B0989898");

            // The viewport will be the majority of what's visible
            ViewportBackground = UtilityWPF.BrushFromHex("F0F0F0F0");
            ViewportBorder = UtilityWPF.BrushFromHex("F0000000");

            PanelBackground = UtilityWPF.BrushFromHex("60D4D9D5");
            PanelBorder = UtilityWPF.BrushFromHex("80C7D1C9");

            Foreground = UtilityWPF.BrushFromHex("525252");

            CloseForegroud = UtilityWPF.BrushFromHex("A0EBEBEB");
            CloseBackground = UtilityWPF.BrushFromHex("20A8A8A8");
            CloseBorder = UtilityWPF.BrushFromHex("30A8A8A8");
            CloseBackground_Highlight = UtilityWPF.BrushFromHex("80858585");
            CloseBorder_Highlight = UtilityWPF.BrushFromHex("D0858585");
        }
        public void SetColorTheme_Dark()
        {
            PopupBackground = UtilityWPF.BrushFromHex("E0505050");
            PopupBorder = UtilityWPF.BrushFromHex("E8303030");

            ViewportBackground = UtilityWPF.BrushFromHex("C0E0E0E0");
            ViewportBorder = UtilityWPF.BrushFromHex("E0000000");

            PanelBackground = UtilityWPF.BrushFromHex("80424F45");
            PanelBorder = UtilityWPF.BrushFromHex("8068736B");

            Foreground = UtilityWPF.BrushFromHex("F0F0F0");
        }
        public void SetColorTheme_Dark_Gradient()
        {
            PopupBackground = UtilityWPF.BrushFromHex("E0505050");
            PopupBorder = UtilityWPF.BrushFromHex("E8303030");

            ViewportBackground = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(UtilityWPF.ColorFromHex("C0EBEDE4"), 0d),
                    new GradientStop(UtilityWPF.ColorFromHex("C0DFE0DA"), .1d),
                    new GradientStop(UtilityWPF.ColorFromHex("C0E0E0E0"), .6d),
                    new GradientStop(UtilityWPF.ColorFromHex("C0DADBD5"), .9d),
                    new GradientStop(UtilityWPF.ColorFromHex("C0D7DBCC"), 1d)
                },
            };

            ViewportBorder = UtilityWPF.BrushFromHex("E0000000");

            PanelBackground = UtilityWPF.BrushFromHex("80424F45");
            PanelBorder = UtilityWPF.BrushFromHex("8068736B");

            Foreground = UtilityWPF.BrushFromHex("F0F0F0");
        }

        /// <summary>
        /// This places the viewer in the corner of the screen that is farthest from the point passed in
        /// NOTE: Current monitor, not just the form
        /// </summary>
        public static void PlaceViewerInCorner(ShipViewerWindow viewer, Point screenPoint)
        {
            const double OFFSET = 50;

            Rect screenRect = UtilityWPF.GetCurrentScreen(screenPoint);

            //Size viewerSize = new Size(viewer.ActualWidth, viewer.ActualHeight);      // sometimes it's not shown yet, so actual size will be zero
            Size viewerSize = new Size(viewer.Width, viewer.Height);

            #region Get candidate locations

            List<Tuple<Rect, double>> positions = new List<Tuple<Rect, double>>();

            // TopLeft
            Rect rect = new Rect(screenRect.TopLeft, viewerSize);

            if (!rect.Contains(screenPoint))
            {
                //positions.Add(Tuple.Create(rect, (rect.BottomRight - screenPoint).Length));       // first attempt was to do the nearest corner, but that gave counterintuitive results
                positions.Add(Tuple.Create(rect, (rect.TopLeft - screenPoint).Length));
            }

            // TopRight
            rect = new Rect(new Point(screenRect.Right - viewerSize.Width, screenRect.Top), viewerSize);

            if (!rect.Contains(screenPoint))
            {
                //positions.Add(Tuple.Create(rect, (rect.BottomLeft - screenPoint).Length));
                positions.Add(Tuple.Create(rect, (rect.TopRight - screenPoint).Length));
            }

            // BottomLeft
            rect = new Rect(new Point(screenRect.Left, screenRect.Bottom - viewerSize.Height), viewerSize);

            if (!rect.Contains(screenPoint))
            {
                //positions.Add(Tuple.Create(rect, (rect.TopRight - screenPoint).Length));
                positions.Add(Tuple.Create(rect, (rect.BottomLeft - screenPoint).Length));
            }

            // BottomRight
            rect = new Rect(new Point(screenRect.Right - viewerSize.Width, screenRect.Bottom - viewerSize.Height), viewerSize);

            if (!rect.Contains(screenPoint))
            {
                //positions.Add(Tuple.Create(rect, (rect.TopLeft - screenPoint).Length));
                positions.Add(Tuple.Create(rect, (rect.BottomRight - screenPoint).Length));
            }

            #endregion

            if (positions.Count > 0)
            {
                // Find the position that places the viewer the farthest away from the point passed in
                var farthest = positions.OrderByDescending(o => o.Item2).First();

                viewer.Left = farthest.Item1.Left;
                viewer.Top = farthest.Item1.Top;

                return;
            }

            #region Overlap

            // There is overlap, push it left or right of the point
            double leftDistance = screenPoint.X - screenRect.Left;
            double rightDistance = screenRect.Right - screenPoint.X;

            double x;

            if (rightDistance < leftDistance)
            {
                // Place it to the left of the point
                x = screenPoint.X - OFFSET - viewerSize.Width;
            }
            else
            {
                // Place it to the right of the point
                x = screenPoint.X + OFFSET;
            }

            Point popupPoint = new Point(x, screenPoint.Y - (viewer.Height / 3d));
            popupPoint = UtilityWPF.EnsureWindowIsOnScreen(popupPoint, viewerSize);		// don't let the popup straddle monitors

            viewer.Left = x;        // Ignore what the straddle method tried to do to x
            viewer.Top = popupPoint.Y;      // Honor what the straddle method tried to do to y

            #endregion
        }

        #endregion

        #region Event Listeners

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                #region Trackball

                // Trackball
                _trackball = new TrackBallRoam(_camera);
                //_trackball.KeyPanScale = 15d;
                _trackball.EventSource = pnlViewport;       //NOTE:  If this control doesn't have a background color set, the trackball won't see events (I think transparent is ok, just not null)
                _trackball.AllowZoomOnMouseWheel = true;
                _trackball.MouseWheelScale *= .05d;
                _trackball.PanScale *= .2d;
                _trackball.Mappings.AddRange(TrackBallMapping.GetPrebuilt(TrackBallMapping.PrebuiltMapping.MouseComplete));
                //_trackball.GetOrbitRadius += new GetOrbitRadiusHandler(Trackball_GetOrbitRadius);
                _trackball.ShouldHitTestOnOrbit = true;

                #endregion

                // Make sure the appropriate tab is showing
                Overlay_Checked(this, new RoutedEventArgs());

                // Pull the camera back to a good distance
                _camera.Position = (_camera.Position.ToVector().ToUnit() * (_partsRadius * 2.3d)).ToPoint();
                //Camera_Changed(this, new EventArgs());

                _timer.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Window_Closed(object sender, EventArgs e)
        {

        }

        private void Close_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                // This is needed because the window does a dragmove on mousedown, so the button needs to intercept that when the mouse
                // is down over the button (without this, the button's mouseup never fires)
                e.Handled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Close_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void grip_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is Rectangle source)
                {
                    Mouse.Capture(source);
                    _resizeStart = e.GetPosition(source);
                    _isResizing = true;

                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void grip_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (_isResizing && sender is Rectangle source)
                {
                    _isResizing = false;
                    source.ReleaseMouseCapture();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void gripLeft_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                ChangeSize(sender, e, true, false, false, false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void gripRight_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                ChangeSize(sender, e, false, true, false, false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void gripTop_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                ChangeSize(sender, e, false, false, true, false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void gripBottom_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                ChangeSize(sender, e, false, false, false, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void gripTopLeft_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                ChangeSize(sender, e, true, false, true, false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void gripTopRight_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                ChangeSize(sender, e, false, true, true, false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void gripBottomLeft_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                ChangeSize(sender, e, true, false, false, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void gripBottomRight_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                // Don't really need the bottom right one, because ResizeMode="CanResizeWithGrip" implements bottom right, but it doesn't really hurt either
                ChangeSize(sender, e, false, true, false, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.OriginalSource == pnlOuterBorder)     // this event fires for any click anywhere in this panel, but only drag if they actually click in the outer border
                {
                    // This allows the window to be dragged around (the mouse down event is on the border, so any control
                    // above it will intercept the mouse down event)
                    this.DragMove();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Camera_Changed(object sender, EventArgs e)
        {
            try
            {
                // Slave the background's camera with the main camera
                _cameraBack.Position = _camera.Position;
                _cameraBack.LookDirection = _camera.LookDirection;
                _cameraBack.UpDirection = _camera.UpDirection;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Overlay_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_isInitialized)
                {
                    return;
                }

                #region Remove existing

                // Back Visuals
                foreach (Visual3D visual in _viewportBack.Children.ToArray())
                {
                    if (_startingVisualsBack.Contains(visual))
                    {
                        continue;
                    }

                    _viewportBack.Children.Remove(visual);
                }

                // Fore Visuals
                foreach (Visual3D visual in _viewport.Children.ToArray())
                {
                    if (_startingVisualsFore.Contains(visual))
                    {
                        continue;
                    }

                    _viewport.Children.Remove(visual);
                }

                // Options panels
                pnlNeuralOptions.Visibility = Visibility.Collapsed;

                #endregion

                if (radNeural.IsChecked.Value)
                {
                    #region Neural

                    if (_neuronVisuals == null)
                    {
                        _neuronVisuals = CreateNeuralVisuals();
                    }

                    foreach (NeuralContainerVisual container in _neuronVisuals.Containers)
                    {
                        _viewport.Children.Add(container.NeuronVisual);
                    }

                    pnlNeuralOptions.Visibility = Visibility.Visible;

                    // Show the optional visuals if requested
                    ShowHideLinks();
                    ShowHideParts_Background();

                    #endregion
                }
                //else if (radParts.IsChecked.Value)
                //{
                //}
                //else if (radMass.IsChecked.Value)
                //{
                //}
                //else if (radCargo.IsChecked.Value)
                //{
                //}
                //else if (radGraphs.IsChecked.Value)
                //{
                //}
                else
                {
                    throw new ApplicationException("Unknown selected tab");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NeuralOptions_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_neuronVisuals == null)
                {
                    return;
                }

                if (e.OriginalSource == chkNeuralShowLinks)
                {
                    ShowHideLinks();
                }
                else if (e.OriginalSource == chkNeuralShowParts)
                {
                    ShowHideParts_Background();
                }
                else
                {
                    throw new ApplicationException("Unknown neural option checkbox");
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
                _progressBars.Foreground = this.Foreground;
                _progressBars.Update();


                //TODO: Store consumption/update graph

                // Update neuron visuals
                if (radNeural.IsChecked.Value && _neuronVisuals != null)
                {
                    foreach (var neuron in _neuronVisuals.Containers.SelectMany(o => o.Neurons))
                    {
                        neuron.Item2.Color = _itemColors.GetNeuronColor(neuron.Item1);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private void ShowHideLinks()
        {
            if (chkNeuralShowLinks.IsChecked.Value)
            {
                // Add them
                foreach (Visual3D visual in _neuronVisuals.Links)
                {
                    if (!_viewport.Children.Contains(visual))
                    {
                        _viewport.Children.Add(visual);
                    }
                }
            }
            else
            {
                // Remove them
                foreach (Visual3D visual in _neuronVisuals.Links)
                {
                    if (_viewport.Children.Contains(visual))
                    {
                        _viewport.Children.Remove(visual);
                    }
                }
            }
        }
        private void ShowHideParts_Background()
        {
            if (chkNeuralShowParts.IsChecked.Value)
            {
                // Add them
                if (_partVisuals == null)
                {
                    _partVisuals = CreatePartVisuals();
                }

                if (!_viewportBack.Children.Contains(_partVisuals.Visual))
                {
                    _viewportBack.Children.Add(_partVisuals.Visual);
                }
            }
            else
            {
                // Remove them
                if (_partVisuals != null && _viewportBack.Children.Contains(_partVisuals.Visual))
                {
                    _viewportBack.Children.Remove(_partVisuals.Visual);
                }
            }
        }

        private PartVisuals CreatePartVisuals()
        {
            Model3DGroup geometries = new Model3DGroup();

            foreach (PartBase part in _parts)
            {
                geometries.Children.Add(part.Model);
            }

            geometries.Transform = new TranslateTransform3D(_centerOffset);

            ModelVisual3D model = new ModelVisual3D();
            model.Content = geometries;

            PartVisuals retVal = new PartVisuals();
            retVal.Visual = model;

            return retVal;
        }

        private NeuralVisuals CreateNeuralVisuals()
        {
            NeuralVisuals retVal = new NeuralVisuals();

            Dictionary<INeuronContainer, Transform3D> containerTransforms = new Dictionary<INeuronContainer, Transform3D>();

            #region Neurons

            foreach (PartBase part in _parts)
            {
                if (!(part is INeuronContainer))
                {
                    continue;
                }

                NeuralContainerVisual container = new NeuralContainerVisual();
                container.Container = (INeuronContainer)part;

                // Neurons
                ModelVisual3D model;
                BuildNeuronVisuals(out container.Neurons, out model, container.Container.Neruons_All, container.Container, containerTransforms, _itemColors, _centerOffset);
                container.NeuronVisual = model;

                retVal.Containers.Add(container);
            }

            #endregion
            #region Links

            if (_neuronLinks != null)
            {
                Model3DGroup posLines = null, negLines = null;
                DiffuseMaterial posDiffuse = null, negDiffuse = null;

                foreach (var output in _neuronLinks)
                {
                    Transform3D toTransform = GetContainerTransform(containerTransforms, output.Container, _centerOffset);

                    foreach (var link in UtilityCore.Iterate(output.InternalLinks, output.ExternalLinks))
                    {
                        Transform3D fromTransform = GetContainerTransform(containerTransforms, link.FromContainer, _centerOffset);

                        BuildLinkVisual(ref posLines, ref posDiffuse, ref negLines, ref negDiffuse, fromTransform.Transform(link.From.Position), toTransform.Transform(link.To.Position), link.Weight, link.BrainChemicalModifiers, _itemColors);
                    }
                }

                if (posLines != null)
                {
                    ModelVisual3D model = new ModelVisual3D();
                    model.Content = posLines;
                    retVal.Links.Add(model);
                }

                if (negLines != null)
                {
                    ModelVisual3D model = new ModelVisual3D();
                    model.Content = negLines;
                    retVal.Links.Add(model);
                }
            }

            #endregion

            // Exit Function
            return retVal;
        }

        private static void BuildNeuronVisuals(out List<Tuple<INeuron, SolidColorBrush>> outNeurons, out ModelVisual3D model, IEnumerable<INeuron> inNeurons, INeuronContainer container, Dictionary<INeuronContainer, Transform3D> containerTransforms, ItemColors colors, Vector3D centerOffset)
        {
            outNeurons = new List<Tuple<INeuron, SolidColorBrush>>();

            Model3DGroup geometries = new Model3DGroup();

            double neuronScale = Math.Sqrt(container.Neruons_All.Max(o => o.Position.ToVector().LengthSquared));

            int neuronCount = inNeurons.Count();
            double neuronRadius;
            if (neuronCount < 20)
            {
                neuronRadius = .03d;
            }
            else if (neuronCount > 100)
            {
                neuronRadius = .007d;
            }
            else
            {
                neuronRadius = UtilityCore.GetScaledValue(.03d, .007d, 20, 100, neuronCount);
            }

            if (!Math1D.IsNearZero(neuronScale) && !Math1D.IsInvalid(neuronScale))
            {
                neuronRadius *= neuronScale;
            }

            foreach (INeuron neuron in inNeurons)
            {
                MaterialGroup material = new MaterialGroup();
                SolidColorBrush brush = new SolidColorBrush(neuron.IsPositiveOnly ? colors.Neuron_Zero_ZerPos : colors.Neuron_Zero_NegPos);
                DiffuseMaterial diffuse = new DiffuseMaterial(brush);
                material.Children.Add(diffuse);
                material.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("80C0C0C0")), 50d));

                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = material;
                geometry.BackMaterial = material;
                geometry.Geometry = UtilityWPF.GetSphere_LatLon(2, neuronRadius);
                geometry.Transform = new TranslateTransform3D(neuron.Position.ToVector());

                geometries.Children.Add(geometry);

                outNeurons.Add(new Tuple<INeuron, SolidColorBrush>(neuron, brush));
            }

            model = new ModelVisual3D();
            model.Content = geometries;
            model.Transform = GetContainerTransform(containerTransforms, container, centerOffset);
        }
        private static void BuildLinkVisual(ref Model3DGroup posLines, ref DiffuseMaterial posDiffuse, ref Model3DGroup negLines, ref DiffuseMaterial negDiffuse, Point3D from, Point3D to, double weight, double[] brainChemicals, ItemColors colors)
        {
            const double GAP = .035d;
            const double CHEMSPACE = .01d;

            double thickness = Math.Abs(weight) * .003d;

            #region Shorten Line

            // Leave a little bit of gap between the from node and the line so the user knows what direction the link is
            Vector3D line = from - to;
            double length = line.Length;
            double newLength = length - GAP;
            if (newLength > length * .75d)		// don't shorten it if it's going to be too small
            {
                line = line.ToUnit() * newLength;
            }
            else
            {
                newLength = length;		// doing this here so the logic below doesn't need an if statement
            }

            Point3D fromActual = to + line;

            #endregion

            GeometryModel3D geometry = null;

            #region Draw Line

            if (weight > 0)
            {
                if (posLines == null)
                {
                    posLines = new Model3DGroup();
                    posDiffuse = new DiffuseMaterial(new SolidColorBrush(colors.Link_Positive));
                }

                geometry = new GeometryModel3D();
                geometry.Material = posDiffuse;
                geometry.BackMaterial = posDiffuse;
                geometry.Geometry = UtilityWPF.GetLine(fromActual, to, thickness);

                posLines.Children.Add(geometry);
            }
            else
            {
                if (negLines == null)
                {
                    negLines = new Model3DGroup();
                    negDiffuse = new DiffuseMaterial(new SolidColorBrush(colors.Link_Negative));
                }

                geometry = new GeometryModel3D();
                geometry.Material = negDiffuse;
                geometry.BackMaterial = negDiffuse;
                geometry.Geometry = UtilityWPF.GetLine(fromActual, to, thickness);

                negLines.Children.Add(geometry);
            }

            #endregion

            if (brainChemicals != null)
            {
                #region Draw Brain Chemicals

                #region Calculations

                double workingLength = newLength - GAP;
                if (Math1D.IsNearValue(length, newLength))
                {
                    // Logic above didn't use a gap, so don't do one here either
                    workingLength = length;
                }

                double totalChemSpace = (brainChemicals.Length - 1) * CHEMSPACE;

                double chemSpace = CHEMSPACE;
                if (totalChemSpace > workingLength)
                {
                    chemSpace = workingLength / (brainChemicals.Length - 1);		// shouldn't get divide by zero
                }

                Vector3D chemOffset = line.ToUnit() * (chemSpace * -1d);

                RotateTransform3D rotTrans = new RotateTransform3D(new QuaternionRotation3D(Math3D.GetRotation(new Vector3D(0, 0, 1), line)));

                #endregion

                // Place the chemicals
                for (int cntr = 0; cntr < brainChemicals.Length; cntr++)
                {
                    Transform3DGroup transform = new Transform3DGroup();
                    transform.Children.Add(rotTrans);
                    transform.Children.Add(new TranslateTransform3D((fromActual + (chemOffset * cntr)).ToVector()));

                    double scale = .0062d * Math.Abs(brainChemicals[cntr]);
                    ScaleTransform3D scaleTransform = new ScaleTransform3D(scale, scale, scale);

                    if (brainChemicals[cntr] > 0)
                    {
                        if (posLines == null)
                        {
                            posLines = new Model3DGroup();
                            posDiffuse = new DiffuseMaterial(new SolidColorBrush(colors.Link_Positive));
                        }

                        geometry = new GeometryModel3D();
                        geometry.Material = posDiffuse;
                        geometry.BackMaterial = posDiffuse;
                        geometry.Geometry = UtilityWPF.GetCircle2D(5, scaleTransform, Transform3D.Identity);
                        geometry.Transform = transform;

                        posLines.Children.Add(geometry);
                    }
                    else
                    {
                        if (negLines == null)
                        {
                            negLines = new Model3DGroup();
                            negDiffuse = new DiffuseMaterial(new SolidColorBrush(colors.Link_Negative));
                        }

                        geometry = new GeometryModel3D();
                        geometry.Material = negDiffuse;
                        geometry.BackMaterial = negDiffuse;
                        geometry.Geometry = UtilityWPF.GetCircle2D(5, scaleTransform, Transform3D.Identity);
                        geometry.Transform = transform;

                        negLines.Children.Add(geometry);
                    }
                }

                #endregion
            }
        }

        private static Transform3D GetContainerTransform(Dictionary<INeuronContainer, Transform3D> existing, INeuronContainer container, Vector3D centerOffset)
        {
            if (!existing.ContainsKey(container))
            {
                Transform3DGroup transform = new Transform3DGroup();

                // Get the largest neuron position, and create a scale so that it fits inside of the container's scale
                double maxRadius = Math.Sqrt(container.Neruons_All.Max(o => o.Position.ToVector().LengthSquared));

                if (maxRadius > container.Radius && !Math1D.IsNearZero(maxRadius))
                {
                    double scale = container.Radius / maxRadius;

                    transform.Children.Add(new ScaleTransform3D(new Vector3D(scale, scale, scale)));		// this needs to be added to the group before the others?
                }

                transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(container.Orientation)));
                transform.Children.Add(new TranslateTransform3D(centerOffset + container.Position.ToVector()));

                existing.Add(container, transform);
            }

            return existing[container];
        }

        private void ChangeSize(object sender, MouseEventArgs e, bool isLeft, bool isRight, bool isTop, bool isBottom)
        {
            Rectangle source = sender as Rectangle;

            if (!_isResizing || source == null)
            {
                return;
            }

            Point position = e.GetPosition(source);

            //TODO: Don't go less than MINSIZE

            if (isLeft)
            {
                var newSize = GetNewSize(Width, _resizeStart.X - position.X);

                Width = newSize.size;
                Left -= newSize.change;
            }
            else if (isRight)
            {
                var newSize = GetNewSize(Width, position.X - _resizeStart.X);

                Width = newSize.size;
            }

            if (isTop)
            {
                var newSize = GetNewSize(Height, _resizeStart.Y - position.Y);

                Height = newSize.size;
                Top -= newSize.change;
            }
            else if (isBottom)
            {
                var newSize = GetNewSize(Height, position.Y - _resizeStart.Y);

                Height = newSize.size;
            }
        }

        private static (double size, double change) GetNewSize(double oldSize, double change)
        {
            double newSize = oldSize + change;
            double newChange = change;

            if (newSize < MINSIZE)
            {
                newSize = MINSIZE;
                newChange = MINSIZE - oldSize;
            }

            return (newSize, newChange);
        }

        #endregion
    }
}
