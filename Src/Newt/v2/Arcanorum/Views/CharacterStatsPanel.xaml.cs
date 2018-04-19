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
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.HelperClassesWPF.Controls3D;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.Arcanorum.Views
{
    public partial class CharacterStatsPanel : UserControl
    {
        #region Declaration Section

        /// <summary>
        /// This listens to the mouse/keyboard and controls the camera
        /// </summary>
        private readonly TrackBallRoam _trackball;

        private readonly DispatcherTimer _timer;

        private List<ModelVisual3D> _visuals = new List<ModelVisual3D>();

        private bool _isInitialized = false;

        #endregion

        #region Constructor

        public CharacterStatsPanel()
        {
            InitializeComponent();

            // SpikeBall
            foreach (string name in Enum.GetNames(typeof(WeaponSpikeBallMaterial)))
            {
                cboMaterial.Items.Add(name);
            }

            cboMaterial.SelectedIndex = 0;

            // Axe Head
            foreach (string name in Enum.GetNames(typeof(WeaponAxeType)))
            {
                cboAxeType.Items.Add(name);
            }

            cboAxeType.SelectedIndex = 0;

            // Trackball
            _trackball = new TrackBallRoam(_camera);
            _trackball.KeyPanScale = 15d;
            _trackball.EventSource = grdViewport;		//NOTE:  If this control doesn't have a background color set, the trackball won't see events (I think transparent is ok, just not null)
            _trackball.AllowZoomOnMouseWheel = true;
            _trackball.Mappings.Add(new TrackBallMapping(CameraMovement.Orbit, MouseButton.Left));
            _trackball.Mappings.Add(new TrackBallMapping(CameraMovement.Orbit, MouseButton.Right));
            _trackball.ShouldHitTestOnOrbit = false;

            // Timer
            //_timer = new DispatcherTimer();
            //_timer.Interval = TimeSpan.FromMilliseconds(100);
            //_timer.Tick += Timer_Tick;
            //_timer.IsEnabled = true;

            _isInitialized = true;
        }

        #endregion

        #region Public Properties

        public World World
        {
            get;
            set;
        }

        #endregion

        #region Event Listeners

        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "CharacterStatsPanel", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnGenerateSpikeBall_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _viewport.Children.RemoveAll(_visuals);

                string selectedItem = cboMaterial.SelectedItem as string;

                WeaponSpikeBallMaterial material;
                if (selectedItem == null || !Enum.TryParse(selectedItem, out material))
                {
                    MessageBox.Show("Unrecognized material", "CharacterStatsPanel", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                WeaponSpikeBallDNA dna = new WeaponSpikeBallDNA()
                {
                    Radius = 1d,
                    Material = material
                };

                ModelVisual3D visual = new ModelVisual3D();
                visual.Content = new WeaponSpikeBall(new WeaponMaterialCache(), dna).Model;

                _visuals.Add(visual);
                _viewport.Children.Add(visual);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "CharacterStatsPanel", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnGenerateAxe_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _viewport.Children.RemoveAll(_visuals);

                string selectedItem = cboAxeType.SelectedItem as string;

                WeaponAxeType axeType;
                if (selectedItem == null || !Enum.TryParse(selectedItem, out axeType))
                {
                    MessageBox.Show("Unrecognized axe type", "CharacterStatsPanel", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                WeaponAxeSides sides;
                if(chkAxeDouble.IsChecked.Value)
                {
                    sides = WeaponAxeSides.Double;
                }
                else if(StaticRandom.NextBool())
                {
                    sides = WeaponAxeSides.Single_BackSpike;
                }
                else
                {
                    sides = WeaponAxeSides.Single_BackFlat;
                }

                WeaponAxeDNA dna = new WeaponAxeDNA()
                {
                    SizeSingle = 1d,
                    AxeType = axeType,
                    Sides = sides,
                    Style = WeaponAxeStye.Basic
                };

                ModelVisual3D visual = new ModelVisual3D();
                visual.Content = new WeaponAxe(new WeaponMaterialCache(), dna, true).Model;

                _visuals.Add(visual);
                _viewport.Children.Add(visual);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "CharacterStatsPanel", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnFull_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Point3D moveTo = new Point3D();
                if (chkFullRandPos.IsChecked.Value)
                {
                    moveTo = Math3D.GetRandomVector(5).ToPoint();
                }

                _viewport.Children.RemoveAll(_visuals);

                WeaponDNA dna = WeaponDNA.GetRandomDNA();
                Weapon weapon = new Weapon(dna, moveTo, this.World, 0);

                ModelVisual3D visual = new ModelVisual3D();
                visual.Content = weapon.Model;

                //if (chkFullRandPos.IsChecked.Value)
                //{
                //    weapon.MoveToAttachPoint(moveTo);
                visual.Transform = new MatrixTransform3D(weapon.PhysicsBody.OffsetMatrix);
                //}

                _visuals.Add(visual);
                _viewport.Children.Add(visual);

                if (!weapon.IsGraphicsOnly)
                {
                    double dotRadius = weapon.DNA.Handle.Radius * 1.5;

                    // I don't think adding to moveTo is correct (it's doing a double add)
                    AddDot(weapon.AttachPoint + moveTo.ToVector(), dotRadius, Colors.Green);
                    AddDot(weapon.PhysicsBody.CenterOfMass + moveTo.ToVector(), dotRadius, Colors.Red);
                    AddDot(moveTo, dotRadius, Colors.Orange);

                    // Physics AABB
                    Point3D aabbMin, aabbMax;
                    //weapon.PhysicsBody.CollisionHull.CalculateAproximateAABB(out aabbMin, out aabbMax);
                    //AddAABBLines(aabbMin, aabbMax, Colors.Silver);

                    //TODO: Show the mass matrix as a rectangle
                    weapon.PhysicsBody.GetAABB(out aabbMin, out aabbMax);
                    AddAABBLines(aabbMin, aabbMax, Colors.White);

                    // Spray dots onto the collision mesh
                    var hullPoints = weapon.PhysicsBody.CollisionHull.GetVisualizationOfHull();

                    AddDots(hullPoints.Select(o => o.Item1), weapon.DNA.Handle.Radius * .1, Colors.Black);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "CharacterStatsPanel", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private void AddDot(Point3D position, double radius, Color color)
        {
            GeometryModel3D model = new GeometryModel3D();
            model.Material = new DiffuseMaterial(new SolidColorBrush(color));

            model.Geometry = UtilityWPF.GetSphere_Ico(radius, 2, true);

            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = model;
            visual.Transform = new TranslateTransform3D(position.ToVector());

            _visuals.Add(visual);
            _viewport.Children.Add(visual);
        }
        private void AddDots(IEnumerable<Point3D> positions, double radius, Color color)
        {
            Model3DGroup group = new Model3DGroup();

            DiffuseMaterial material = new DiffuseMaterial(new SolidColorBrush(color));

            foreach(Point3D position in positions)
            {
                GeometryModel3D model = new GeometryModel3D();
                model.Material = material;

                model.Geometry = UtilityWPF.GetSphere_Ico(radius, 2, true);

                model.Transform = new TranslateTransform3D(position.ToVector());

                group.Children.Add(model);
            }

            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = group;

            _visuals.Add(visual);
            _viewport.Children.Add(visual);
        }
        private void AddAABBLines(Point3D min, Point3D max, Color color)
        {
            ScreenSpaceLines3D lineVisual = new ScreenSpaceLines3D(true);
            lineVisual.Thickness = 1;
            lineVisual.Color = color;

            // Top
            lineVisual.AddLine(new Point3D(min.X, min.Y, min.Z), new Point3D(max.X, min.Y, min.Z));
            lineVisual.AddLine(new Point3D(max.X, min.Y, min.Z), new Point3D(max.X, max.Y, min.Z));
            lineVisual.AddLine(new Point3D(max.X, max.Y, min.Z), new Point3D(min.X, max.Y, min.Z));
            lineVisual.AddLine(new Point3D(min.X, max.Y, min.Z), new Point3D(min.X, min.Y, min.Z));

            // Bottom
            lineVisual.AddLine(new Point3D(min.X, min.Y, max.Z), new Point3D(max.X, min.Y, max.Z));
            lineVisual.AddLine(new Point3D(max.X, min.Y, max.Z), new Point3D(max.X, max.Y, max.Z));
            lineVisual.AddLine(new Point3D(max.X, max.Y, max.Z), new Point3D(min.X, max.Y, max.Z));
            lineVisual.AddLine(new Point3D(min.X, max.Y, max.Z), new Point3D(min.X, min.Y, max.Z));

            // Sides
            lineVisual.AddLine(new Point3D(min.X, min.Y, min.Z), new Point3D(min.X, min.Y, max.Z));
            lineVisual.AddLine(new Point3D(max.X, min.Y, min.Z), new Point3D(max.X, min.Y, max.Z));
            lineVisual.AddLine(new Point3D(max.X, max.Y, min.Z), new Point3D(max.X, max.Y, max.Z));
            lineVisual.AddLine(new Point3D(min.X, max.Y, min.Z), new Point3D(min.X, max.Y, max.Z));

            _viewport.Children.Add(lineVisual);
            _visuals.Add(lineVisual);
        }

        #endregion
    }
}
