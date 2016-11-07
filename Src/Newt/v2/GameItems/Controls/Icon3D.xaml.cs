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
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Game.HelperClassesWPF;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.MapParts;
using Game.Newt.v2.GameItems.ShipEditor;

namespace Game.Newt.v2.GameItems.Controls
{
    /// <summary>
    /// This shows 3D objects like an icon
    /// </summary>
    public partial class Icon3D : UserControl
    {
        #region Declaration Section

        private const string TITLE = "Icon3D";

        /// <summary>
        /// This listens to the mouse/keyboard and controls the camera
        /// </summary>
        private TrackBallRoam _trackball = null;

        private AnimateRotation _rotateAnimate = null;
        private DispatcherTimer _rotateTimer = null;
        private QuaternionRotation3D _rotateTransform = null;
        private DateTime _lastTick;

        private PointLight _hoverLight = null;
        private AmbientLight _selectedLight = null;

        #endregion

        #region Constructor

        public Icon3D(string name, ShipDNA dna, NewtonDynamics.World world)
        {
            InitializeComponent();

            this.ItemName = name;
            this.ShipDNA = dna;

            lblName.Text = name;
            lblName.Visibility = _showName ? Visibility.Visible : Visibility.Collapsed;

            InitializeTrackball();

            // Load the ship asyncronously
            FinishLoadingShipAsync(world);
        }
        public Icon3D(MineralType mineralType)
        {
            InitializeComponent();

            this.ItemName = mineralType.ToString();
            this.MineralType = mineralType;

            lblName.Text = this.ItemName;
            lblName.Visibility = _showName ? Visibility.Visible : Visibility.Collapsed;

            InitializeTrackball();

            RenderMineral();
            InitializeLight();
        }
        public Icon3D(ShipPartDNA dna, EditorOptions options)
        {
            InitializeComponent();

            // Need to set position to zero, or the image won't be centered (part's model considers position/orientation)
            dna = ShipPartDNA.Clone(dna);
            dna.Position = new Point3D();
            dna.Orientation = Quaternion.Identity;

            PartDesignBase part = BotConstructor.GetPartDesign(dna, options);

            this.ItemName = part.PartType;
            this.Part = part;

            lblName.Text = this.ItemName;

            lblName.Visibility = _showName ? Visibility.Visible : Visibility.Collapsed;

            InitializeTrackball();

            RenderPart();
            InitializeLight();
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// This will always be set.  It is the name of the item that the icon is showing
        /// </summary>
        public string ItemName
        {
            get;
            private set;
        }

        /// <summary>
        /// This is set when the icon shows a ship
        /// </summary>
        public ShipDNA ShipDNA
        {
            get;
            private set;
        }
        /// <summary>
        /// This is set when the icon shows a mineral
        /// </summary>
        public MineralType MineralType
        {
            get;
            private set;
        }
        /// <summary>
        /// This is set when the icon shows a part (ship part)
        /// NOTE: Give this icon a unique part design to avoid visuals trying to share models
        /// </summary>
        public PartDesignBase Part
        {
            get;
            private set;
        }

        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set
            {
                if (_lightGroup != null)
                {
                    if (value)
                    {
                        if (!_lightGroup.Children.Contains(_selectedLight))
                        {
                            _lightGroup.Children.Add(_selectedLight);
                        }
                    }
                    else
                    {
                        if (_lightGroup.Children.Contains(_selectedLight))
                        {
                            _lightGroup.Children.Remove(_selectedLight);
                        }
                    }
                }

                SetValue(IsSelectedProperty, value);
            }
        }
        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register("IsSelected", typeof(bool), typeof(Icon3D), new UIPropertyMetadata(false));

        public bool ShowBorder
        {
            get { return (bool)GetValue(ShowBorderProperty); }
            set { SetValue(ShowBorderProperty, value); }
        }
        public static readonly DependencyProperty ShowBorderProperty = DependencyProperty.Register("ShowBorder", typeof(bool), typeof(Icon3D), new PropertyMetadata(true));

        private bool _showName = true;
        public bool ShowName
        {
            get
            {
                return _showName;
            }
            set
            {
                _showName = value;

                if (lblName != null)
                {
                    lblName.Visibility = _showName ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }

        private bool _autoRotateOnMouseHover = false;
        /// <summary>
        /// Set to true if you want the item to randomly rotate when the mouse is over the control
        /// </summary>
        public bool AutoRotateOnMouseHover
        {
            get
            {
                return _autoRotateOnMouseHover;
            }
            set
            {
                _autoRotateOnMouseHover = value;

                if (!_autoRotateOnMouseHover)
                {
                    _rotateAnimate = null;
                    if (_rotateTimer != null)
                    {
                        _rotateTimer.Tick -= RotateTimer_Tick;
                        _rotateTimer = null;
                    }
                }
            }
        }

        private FrameworkElement _autoRotateParent = null;
        /// <summary>
        /// If AutoRotateOnMouseHover is true, you can set this to a parent of the icon so the rotate happens when the mouse is anywhere over that
        /// control
        /// </summary>
        public FrameworkElement AutoRotateParent
        {
            get
            {
                return _autoRotateParent;
            }
            set
            {
                if (_autoRotateParent != null)
                {
                    _autoRotateParent.MouseEnter -= AutoRotateParent_MouseEnter;
                    _autoRotateParent.MouseLeave -= AutoRotateParent_MouseLeave;
                }

                _autoRotateParent = value;

                if (_autoRotateParent != null)
                {
                    _autoRotateParent.MouseEnter += AutoRotateParent_MouseEnter;
                    _autoRotateParent.MouseLeave += AutoRotateParent_MouseLeave;
                }
            }
        }

        #endregion

        #region Event Listeners

        private void pnlIconBorder_MouseEnter(object sender, MouseEventArgs e)
        {
            try
            {
                MoveHoverLight(e);
                _lightGroup.Children.Add(_hoverLight);

                if (_autoRotateParent == null)
                {
                    AutoRotate_MouseEnter();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void pnlIconBorder_MouseLeave(object sender, MouseEventArgs e)
        {
            try
            {
                //_hoverLight.Color = Colors.Transparent;
                _lightGroup.Children.Remove(_hoverLight);

                if (_autoRotateParent == null)
                {
                    AutoRotate_MouseLeave();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void pnlIconBorder_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                MoveHoverLight(e);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AutoRotateParent_MouseEnter(object sender, MouseEventArgs e)
        {
            try
            {
                AutoRotate_MouseEnter();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void AutoRotateParent_MouseLeave(object sender, MouseEventArgs e)
        {
            try
            {
                AutoRotate_MouseLeave();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RotateTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (_rotateAnimate != null)
                {
                    DateTime newTime = DateTime.UtcNow;
                    double elapsedTime = (newTime - _lastTick).TotalSeconds;
                    _lastTick = newTime;

                    _rotateAnimate.Tick(elapsedTime);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private async Task FinishLoadingShipAsync(NewtonDynamics.World world)
        {
            // Show the ship
            await RenderShipAsync(world);

            // Lights
            InitializeLight();
        }

        //TODO: Make a static method off of Ship, and don't rely on world: public static Visual3D CreateVisual(ShipDNA dna, bool isDesign)
        private async Task RenderShipAsync(NewtonDynamics.World world)
        {
            ShipExtraArgs args = new ShipExtraArgs()
            {
                RunNeural = false,
                RepairPartPositions = false,
            };

            //using (Ship ship = await Ship.GetNewShipAsync(this.ShipDNA, world, 0, null, args))
            using (Bot bot = new Bot(BotConstructor.ConstructBot(this.ShipDNA, new ShipCoreArgs() { World = world }, args)))
            {
                if (bot.PhysicsBody.Visuals != null)		// this will never be null
                {
                    // The model coords may not be centered, so move the ship so that it's centered on the origin
                    Point3D minPoint, maxPoint;
                    bot.PhysicsBody.GetAABB(out minPoint, out maxPoint);
                    Vector3D offset = (minPoint + ((maxPoint - minPoint) / 2d)).ToVector();

                    bot.PhysicsBody.Position = (-offset).ToPoint();

                    // Add the visuals
                    foreach (Visual3D visual in bot.PhysicsBody.Visuals)
                    {
                        _viewport.Children.Add(visual);
                    }

                    // Pull the camera back to a good distance
                    _camera.Position = (_camera.Position.ToVector().ToUnit() * (bot.Radius * 2.1d)).ToPoint();
                }
            }
        }

        private void RenderMineral()
        {
            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = Mineral.GetNewVisual(this.MineralType);
            _rotateTransform = new QuaternionRotation3D(Math3D.GetRandomRotation());
            visual.Transform = new RotateTransform3D(_rotateTransform);

            _viewport.Children.Add(visual);

            _camera.Position = (_camera.Position.ToVector().ToUnit() * 2.5d).ToPoint();
        }

        private void RenderPart()
        {
            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = this.Part.Model;
            _rotateTransform = new QuaternionRotation3D(Math3D.GetRandomRotation());
            visual.Transform = new RotateTransform3D(_rotateTransform);

            // Pull the camera back far enough to see the part
            double? maxDist = UtilityWPF.GetPointsFromMesh(this.Part.Model).
                Select(o => o.ToVector().LengthSquared).
                OrderByDescending(o => o).
                FirstOrDefault();

            double cameraDist = 2.1;
            if (maxDist != null)
            {
                maxDist = Math.Sqrt(maxDist.Value);

                cameraDist = maxDist.Value * 3;
            }

            _viewport.Children.Add(visual);

            _camera.Position = (_camera.Position.ToVector().ToUnit() * cameraDist).ToPoint();
        }

        private void InitializeTrackball()
        {
            // Trackball
            _trackball = new TrackBallRoam(_camera);
            _trackball.KeyPanScale = 15d;
            _trackball.EventSource = pnlIconBorder;		//NOTE:  If this control doesn't have a background color set, the trackball won't see events (I think transparent is ok, just not null)
            _trackball.AllowZoomOnMouseWheel = false;
            _trackball.Mappings.Add(new TrackBallMapping(CameraMovement.Orbit, MouseButton.Left));
            _trackball.Mappings.Add(new TrackBallMapping(CameraMovement.Orbit, MouseButton.Right));
            _trackball.ShouldHitTestOnOrbit = false;
        }
        private void InitializeLight()
        {
            _hoverLight = new PointLight(Colors.White, new Point3D(0, 0, 0));
            UtilityWPF.SetAttenuation(_hoverLight, _camera.Position.ToVector().Length * 2d, .95d);

            _selectedLight = new AmbientLight(UtilityWPF.ColorFromHex("808080"));
        }

        private void MoveHoverLight(MouseEventArgs e)
        {
            Point mousePos = e.GetPosition(pnlIconBorder);

            double width = pnlIconBorder.ActualWidth;
            double height = pnlIconBorder.ActualHeight;

            DoubleVector standard = new DoubleVector(
                ProjectToTrackball(width, height, new Point(width * .5d, height * .5d)),
                ProjectToTrackball(width, height, new Point(width * .5d, height)));

            Vector3D camPos = _camera.Position.ToVector();
            Vector3D cross1 = Vector3D.CrossProduct(camPos, _camera.UpDirection * -1d);
            Vector3D cross2 = Vector3D.CrossProduct(cross1, camPos);
            DoubleVector camera = new DoubleVector(camPos, cross2);		// can't use camera.up directly, because it's not nessassarily orthogonal

            // Attach the point to a sphere around the ship
            Vector3D projectedPos = ProjectToTrackball(width, height, mousePos);

            // Rotate this so that it's relative to the camera
            Quaternion rotation = standard.GetRotation(camera);

            _hoverLight.Position = rotation.GetRotatedVector(projectedPos * 2d).ToPoint();
        }

        private void AutoRotate_MouseEnter()
        {
            if (_autoRotateOnMouseHover && _rotateTimer == null && _rotateTransform != null)
            {
                _rotateTimer = new DispatcherTimer();
                _rotateTimer.Interval = TimeSpan.FromMilliseconds(25);
                _rotateTimer.Tick += RotateTimer_Tick;

                _rotateAnimate = AnimateRotation.Create_AnyOrientation(_rotateTransform, 45d);
            }

            if (_autoRotateOnMouseHover && _rotateTimer != null)
            {
                _lastTick = DateTime.UtcNow;
                _rotateTimer.IsEnabled = true;
            }
        }
        private void AutoRotate_MouseLeave()
        {
            if (_rotateTimer != null)
            {
                _rotateTimer.IsEnabled = false;
            }
        }

        // Copied from trackball
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
            x = ProjectToTrackballSprtWrap(out localInvert, x);
            shouldInvertZ |= localInvert;

            y = ProjectToTrackballSprtWrap(out localInvert, y);
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
        private static double ProjectToTrackballSprtWrap(out bool shouldInvertZ, double value)
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

        #endregion
    }
}
