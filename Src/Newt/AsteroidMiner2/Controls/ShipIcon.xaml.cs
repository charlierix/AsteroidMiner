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
using Game.Newt.AsteroidMiner2;
using Game.Newt.HelperClasses;

namespace Game.Newt.AsteroidMiner2.Controls
{
    public partial class ShipIcon : UserControl
    {
        #region Declaration Section

        /// <summary>
        /// This listens to the mouse/keyboard and controls the camera
        /// </summary>
        private TrackBallRoam _trackball = null;

        private PointLight _hoverLight = null;
        private AmbientLight _selectedLight = null;

        #endregion

        #region Constructor

        public ShipIcon(string name, ShipDNA dna, NewtonDynamics.World world)
        {
            InitializeComponent();

            this.ShipName = name;
            this.ShipDNA = dna;

            lblName.Text = name;
            lblName.Visibility = _showShipName ? Visibility.Visible : Visibility.Collapsed;

            // Trackball
            _trackball = new TrackBallRoam(_camera);
            _trackball.KeyPanScale = 15d;
            _trackball.EventSource = pnlIconBorder;		//NOTE:  If this control doesn't have a background color set, the trackball won't see events (I think transparent is ok, just not null)
            _trackball.AllowZoomOnMouseWheel = false;
            _trackball.Mappings.Add(new TrackBallMapping(CameraMovement.Orbit, MouseButton.Left));
            _trackball.Mappings.Add(new TrackBallMapping(CameraMovement.Orbit, MouseButton.Right));
            _trackball.ShouldHitTestOnOrbit = false;

            // Load the ship asyncronously
            FinishLoadingAsync(world);
        }

        #endregion

        #region Public Properties

        public string ShipName
        {
            get;
            private set;
        }
        public ShipDNA ShipDNA
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
        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register("IsSelected", typeof(bool), typeof(ShipIcon), new UIPropertyMetadata(false));

        private bool _showShipName = true;
        public bool ShowShipName
        {
            get
            {
                return _showShipName;
            }
            set
            {
                _showShipName = value;

                if (lblName != null)
                {
                    lblName.Visibility = _showShipName ? Visibility.Visible : Visibility.Collapsed;
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
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "BeanIcon", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void pnlIconBorder_MouseLeave(object sender, MouseEventArgs e)
        {
            try
            {
                //_hoverLight.Color = Colors.Transparent;
                _lightGroup.Children.Remove(_hoverLight);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "BeanIcon", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(ex.ToString(), "BeanIcon", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private async Task FinishLoadingAsync(NewtonDynamics.World world)
        {
            // Show the ship
            await RenderShipAsync(world);

            // Lights
            _hoverLight = new PointLight(Colors.White, new Point3D(0, 0, 0));
            UtilityWPF.SetAttenuation(_hoverLight, _camera.Position.ToVector().Length * 2d, .95d);

            _selectedLight = new AmbientLight(UtilityWPF.ColorFromHex("808080"));
        }

        //TODO: Make a static method off of Ship, and don't rely on world: public static Visual3D CreateVisual(ShipDNA dna, bool isDesign)
        private async Task RenderShipAsync(NewtonDynamics.World world)
        {
            //using (Ship ship = new Ship(new AsteroidMiner2.ShipEditor.EditorOptions(), new ItemOptions(), this.ShipDNA, world, 0, null, null, null, false, false))
            using (Ship ship = await Ship.GetNewShipAsync(new AsteroidMiner2.ShipEditor.EditorOptions(), new ItemOptions(), this.ShipDNA, world, 0, null, null, null, false, false))
            {
                if (ship.PhysicsBody.Visuals != null)		// this will never be null
                {
                    // The model coords may not be centered, so move the ship so that it's centered on the origin
                    Point3D minPoint, maxPoint;
                    ship.PhysicsBody.GetAABB(out minPoint, out maxPoint);
                    Vector3D offset = (minPoint + ((maxPoint - minPoint) / 2d)).ToVector();

                    ship.PhysicsBody.Position = (-offset).ToPoint();

                    // Add the visuals
                    foreach (Visual3D visual in ship.PhysicsBody.Visuals)
                    {
                        _viewport.Children.Add(visual);
                    }

                    // Pull the camera back to a good distance
                    _camera.Position = (_camera.Position.ToVector().ToUnit() * (ship.Radius * 2.1d)).ToPoint();
                }
            }
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
