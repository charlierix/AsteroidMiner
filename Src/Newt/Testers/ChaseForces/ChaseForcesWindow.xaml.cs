using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Shapes;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.HelperClassesWPF.Primitives3D;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.ShipEditor;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.Testers.ChaseForces
{
    public partial class ChaseForcesWindow : Window
    {
        #region Enum: ChaseType

        private enum ChaseType
        {
            Linear_Velocity,
            Linear_Force,
            Orientation_Velocity,
            Orientation_Torque,
        }

        #endregion

        #region Declaration Section

        /// <summary>
        /// This is the folder that all saves for this game will go
        /// This will be a subfolder of AsteroidMiner
        /// </summary>
        private const string FOLDER = "ChaseForces";

        private const double BOUNDRYSIZE = 1000;
        private const double BOUNDRYSIZEHALF = BOUNDRYSIZE / 2d;

        private Point3D _boundryMin;
        private Point3D _boundryMax;

        private EditorOptions _editorOptions = new EditorOptions();
        private ItemOptions _itemOptions = null;
        private SharedVisuals _sharedVisuals = new SharedVisuals();

        private World _world = null;

        private MaterialManager _materialManager = null;
        private int _material_Wall = -1;
        private int _material_Ball = -1;

        /// <summary>
        /// This listens to the mouse/keyboard and controls the camera
        /// </summary>
        private TrackBallRoam _trackball = null;

        private ChasedBall _chasedBall = null;
        private TranslateTransform3D _chasedBallTransform = null;
        private Visual3D _chasedBallVisual = null;
        private BillboardLine3D _chasedDirectionModel = null;
        private Visual3D _chasedDirectionVisual = null;
        private Visual3D _boundryLines = null;

        private BodyBall _bodyBall = null;

        private LinearVelocity _panel_LinearVelocity = null;
        private ForceCollection _panel_LinearForce = null;
        private OrientationVelocity _panel_OrientationVelocity = null;
        private ForceCollection _panel_OrientationForce = null;

        private MapObject_ChasePoint_Velocity _object_LinearVelocity = null;
        private MapObject_ChasePoint_Forces _object_LinearForce = null;
        private MapObject_ChaseOrientation_Velocity _object_OrientationVelocity = null;
        private MapObject_ChaseOrientation_Torques _object_OrientationForce = null;

        private bool _initialized = false;

        #endregion

        #region Constructor

        public ChaseForcesWindow()
        {
            InitializeComponent();

            foreach (string name in Enum.GetNames(typeof(ChaseType)))
            {
                cboChaseType.Items.Add(name.Replace("_", " "));
            }

            _initialized = true;
        }

        #endregion

        #region Event Listeners

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _itemOptions = new ItemOptions();

                #region Init World

                _boundryMin = new Point3D(-BOUNDRYSIZEHALF, -BOUNDRYSIZEHALF, -BOUNDRYSIZEHALF);
                _boundryMax = new Point3D(BOUNDRYSIZEHALF, BOUNDRYSIZEHALF, BOUNDRYSIZEHALF);

                _world = new World();
                _world.Updating += new EventHandler<WorldUpdatingArgs>(World_Updating);

                List<Point3D[]> innerLines, outerLines;
                _world.SetCollisionBoundry(out innerLines, out outerLines, _boundryMin, _boundryMax);

                #endregion
                #region Materials

                _materialManager = new MaterialManager(_world);

                // Wall
                Game.Newt.v2.NewtonDynamics.Material material = new Game.Newt.v2.NewtonDynamics.Material();
                material.Elasticity = .1d;
                _material_Wall = _materialManager.AddMaterial(material);

                // Ball
                material = new Game.Newt.v2.NewtonDynamics.Material();
                _material_Ball = _materialManager.AddMaterial(material);

                #endregion
                #region Trackball

                // Trackball
                _trackball = new TrackBallRoam(_camera);
                _trackball.KeyPanScale = 15d;
                _trackball.EventSource = grdViewPort;		//NOTE:  If this control doesn't have a background color set, the trackball won't see events (I think transparent is ok, just not null)
                _trackball.AllowZoomOnMouseWheel = true;
                _trackball.Mappings.AddRange(TrackBallMapping.GetPrebuilt(TrackBallMapping.PrebuiltMapping.MouseComplete_NoLeft));
                _trackball.Mappings.AddRange(TrackBallMapping.GetPrebuilt(TrackBallMapping.PrebuiltMapping.Keyboard_ASDW_In));
                _trackball.ShouldHitTestOnOrbit = true;
                //_trackball.UserMovedCamera += new EventHandler<UserMovedCameraArgs>(Trackball_UserMovedCamera);
                //_trackball.GetOrbitRadius += new EventHandler<GetOrbitRadiusArgs>(Trackball_GetOrbitRadius);

                #endregion

                #region Chased Ball

                _chasedBall = new ChasedBall();

                _chasedBall.MotionType_Position = MotionType_Position.Stop;
                _chasedBall.MotionType_Orientation = MotionType_Orientation.Stop;

                _chasedBall.BoundrySizeChanged += new EventHandler(ChasedBall_BoundrySizeChanged);

                // Ball visual
                _chasedBallVisual = GetChaseBallVisual_Position();
                _chasedBallTransform = new TranslateTransform3D();
                _chasedBallVisual.Transform = _chasedBallTransform;
                _viewport.Children.Add(_chasedBallVisual);

                // Direction Visual
                var directionVisual = GetChaseBallVisual_Orientation();
                _chasedDirectionModel = directionVisual.Item1;
                _chasedDirectionVisual = directionVisual.Item2;
                _viewport.Children.Add(_chasedDirectionVisual);

                // Panels (the act of instantiating them will update the ball's properties)
                pnlChasePosition.Content = new ChasedPosition(_chasedBall)
                {
                    Foreground = Brushes.White,
                };

                pnlChaseOrientation.Content = new ChasedOrientation(_chasedBall)
                {
                    Foreground = Brushes.White,
                };

                #endregion
                #region Debug Visuals

                // Put these on the viewport before the ball so that it is propertly semitransparent

                //TODO: Draw the bounding box.  Use XYZ colors.  This will help the user stay oriented

                #endregion
                #region Body Ball

                _bodyBall = new BodyBall(_world);

                //_bodyBall.PhysicsBody.AngularDamping = new Vector3D(.0001, .0001, .0001);
                //_bodyBall.PhysicsBody.AngularVelocity = new Vector3D(0, 0, 4 * Math.PI);

                _viewport.Children.AddRange(_bodyBall.Visuals3D);

                #endregion

                RedrawBoundry();

                _world.UnPause();
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
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void World_Updating(object sender, WorldUpdatingArgs e)
        {
            try
            {
                // Chased ball
                _chasedBall.Update(e.ElapsedTime);

                _chasedBallTransform.OffsetX = _chasedBall.Position.X;
                _chasedBallTransform.OffsetY = _chasedBall.Position.Y;
                _chasedBallTransform.OffsetZ = _chasedBall.Position.Z;

                _chasedDirectionModel.SetPoints(_chasedBall.Position, _chasedBall.Position + _chasedBall.Direction.ToUnit() * 10);

                // Chasing Objects
                if (_object_LinearVelocity != null)
                {
                    _object_LinearVelocity.SetPosition(_chasedBall.Position);
                }

                if (_object_LinearForce != null)
                {
                    _object_LinearForce.SetPosition(_chasedBall.Position);
                }

                if (_object_OrientationVelocity != null)
                {
                    _object_OrientationVelocity.SetOrientation(_chasedBall.Direction);
                }

                if (_object_OrientationForce != null)
                {
                    _object_OrientationForce.SetOrientation(_chasedBall.Direction);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void cboChaseType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (!_initialized)
                {
                    return;
                }

                ClearChasePanels();

                if (cboChaseType.SelectedValue == null || cboChaseType.SelectedValue.ToString() == "")
                {
                    pnlChaseSettings.Child = null;

                    //TODO: Store a null chase object
                }
                else
                {
                    string text = cboChaseType.SelectedValue.ToString().Replace(" ", "_");

                    ChaseType chaseType;
                    if (Enum.TryParse(text, out chaseType))
                    {
                        switch (chaseType)
                        {
                            case ChaseType.Linear_Velocity:
                                #region Linear_Velocity

                                if (_panel_LinearVelocity == null)
                                {
                                    _panel_LinearVelocity = new LinearVelocity();
                                    _panel_LinearVelocity.Foreground = Brushes.White;
                                    _panel_LinearVelocity.ValueChanged += new EventHandler(PanelChaseSettings_ValueChanged);
                                }

                                pnlChaseSettings.Child = _panel_LinearVelocity;

                                expandChasePosition.IsExpanded = true;

                                #endregion
                                break;

                            case ChaseType.Linear_Force:
                                #region Linear_Force

                                if (_panel_LinearForce == null)
                                {
                                    _panel_LinearForce = new ForceCollection(true);
                                    _panel_LinearForce.Foreground = Brushes.White;
                                    _panel_LinearForce.ValueChanged += new EventHandler(PanelChaseSettings_ValueChanged);
                                }

                                pnlChaseSettings.Child = _panel_LinearForce;

                                expandChasePosition.IsExpanded = true;

                                #endregion
                                break;

                            case ChaseType.Orientation_Velocity:
                                #region Orientation_Velocity

                                if (_panel_OrientationVelocity == null)
                                {
                                    _panel_OrientationVelocity = new OrientationVelocity();
                                    _panel_OrientationVelocity.Foreground = Brushes.White;
                                    _panel_OrientationVelocity.ValueChanged += new EventHandler(PanelChaseSettings_ValueChanged);
                                }

                                pnlChaseSettings.Child = _panel_OrientationVelocity;

                                expandChaseOrientation.IsExpanded = true;

                                #endregion
                                break;

                            case ChaseType.Orientation_Torque:
                                #region Orientation_Torque

                                if (_panel_OrientationForce == null)
                                {
                                    _panel_OrientationForce = new ForceCollection(false);
                                    _panel_OrientationForce.Foreground = Brushes.White;
                                    _panel_OrientationForce.ValueChanged += new EventHandler(PanelChaseSettings_ValueChanged);
                                }

                                pnlChaseSettings.Child = _panel_OrientationForce;

                                expandChaseOrientation.IsExpanded = true;

                                #endregion
                                break;

                            default:
                                throw new ApplicationException("Unknown ChaseType: " + chaseType.ToString());
                        }

                        UpdateChasePanel();
                    }
                    else
                    {
                        throw new ApplicationException("Unknown ChaseType: " + text);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnClearSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_initialized)
                {
                    return;
                }

                cboChaseType.SelectedValue = "";

                // Need to explicitely raise the event, because the event raised above still has cboChaseType.SelectedValue as the old value
                cboChaseType_SelectionChanged(this, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void PanelChaseSettings_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                if (!_initialized)
                {
                    return;
                }

                UpdateChasePanel();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //C:\Users\<username>\AppData\Roaming\Asteroid Miner\Miner2D\
                string foldername = UtilityCore.GetOptionsFolder();
                foldername = System.IO.Path.Combine(foldername, FOLDER);
                Directory.CreateDirectory(foldername);

                string filename = DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss.fff - ");

                if (_object_LinearVelocity != null)
                {
                    filename += "LinearVelocity.xml";
                    filename = System.IO.Path.Combine(foldername, filename);

                    UtilityCore.SerializeToFile(filename, _object_LinearVelocity);
                }
                else if (_object_LinearForce != null)
                {
                    filename += "LinearForce.xml";
                    filename = System.IO.Path.Combine(foldername, filename);

                    UtilityCore.SerializeToFile(filename, _object_LinearForce);
                }
                else if (_object_OrientationVelocity != null)
                {
                    filename += "OrientationVelocity.xml";
                    filename = System.IO.Path.Combine(foldername, filename);

                    UtilityCore.SerializeToFile(filename, _object_OrientationVelocity);
                }
                else if (_object_OrientationForce != null)
                {
                    filename += "OrientationForce.xml";
                    filename = System.IO.Path.Combine(foldername, filename);

                    UtilityCore.SerializeToFile(filename, _object_OrientationForce);
                }
                else
                {
                    MessageBox.Show("There were no settings to export", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //C:\Users\<username>\AppData\Roaming\Asteroid Miner\Miner2D\
                string foldername = UtilityCore.GetOptionsFolder();
                foldername = System.IO.Path.Combine(foldername, FOLDER);
                Directory.CreateDirectory(foldername);

                System.Diagnostics.Process.Start(foldername);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ChasedBall_BoundrySizeChanged(object sender, EventArgs e)
        {
            try
            {
                RedrawBoundry();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void chkDebugVisuals_Checked(object sender, RoutedEventArgs e)
        {
            try
            {

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnResetBody_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _bodyBall.PhysicsBody.Velocity = new Vector3D(0, 0, 0);
                _bodyBall.PhysicsBody.AngularVelocity = new Vector3D(0, 0, 0);
                _bodyBall.PhysicsBody.Position = new Point3D(0, 0, 0);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private void ClearChasePanels()
        {
            if (_object_LinearVelocity != null)
            {
                _object_LinearVelocity.Dispose();
                _object_LinearVelocity = null;
            }

            if (_object_LinearForce != null)
            {
                _object_LinearForce.Dispose();
                _object_LinearForce = null;
            }

            if (_object_OrientationVelocity != null)
            {
                _object_OrientationVelocity.Dispose();
                _object_OrientationVelocity = null;
            }

            if (_object_OrientationForce != null)
            {
                _object_OrientationForce.Dispose();
                _object_OrientationForce = null;
            }
        }
        private void UpdateChasePanel()
        {
            try
            {
                lblError.Text = "";

                ClearChasePanels();

                ChaseType chaseType;
                if (Enum.TryParse(cboChaseType.SelectedValue.ToString().Replace(" ", "_"), out chaseType))
                {
                    switch (chaseType)
                    {
                        case ChaseType.Linear_Velocity:
                            if (_panel_LinearVelocity != null)
                            {
                                _object_LinearVelocity = _panel_LinearVelocity.GetChaseObject(_bodyBall);
                            }
                            break;

                        case ChaseType.Linear_Force:
                            if (_panel_LinearForce != null)
                            {
                                _object_LinearForce = _panel_LinearForce.GetChaseObject_Linear(_bodyBall);
                            }
                            break;

                        case ChaseType.Orientation_Velocity:
                            if (_panel_OrientationVelocity != null)
                            {
                                _object_OrientationVelocity = _panel_OrientationVelocity.GetChaseObject(_bodyBall);
                            }
                            break;

                        case ChaseType.Orientation_Torque:
                            if (_panel_OrientationForce != null)
                            {
                                _object_OrientationForce = _panel_OrientationForce.GetChaseObject_Orientation(_bodyBall);
                            }
                            break;

                        default:
                            throw new ApplicationException("Unknown ChaseType: " + chaseType.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                lblError.Text = ex.Message;
            }
        }

        private Visual3D GetChaseBallVisual_Position()
        {
            // Material
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("C03000"))));
            materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("A0E8BF43")), 10));

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;
            geometry.Geometry = UtilityWPF.GetSphere_Ico(1, 2, true);

            // Model Visual
            ModelVisual3D retVal = new ModelVisual3D();
            retVal.Content = geometry;

            return retVal;
        }
        private Tuple<BillboardLine3D, Visual3D> GetChaseBallVisual_Orientation()
        {
            BillboardLine3D line = new BillboardLine3D()
            {
                Color = UtilityWPF.ColorFromHex("C03000"),
                IsReflectiveColor = false,
                Thickness = .2,
            };

            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = line.Model;

            return new Tuple<BillboardLine3D, Visual3D>(line, visual);
        }

        private void RedrawBoundry()
        {
            if (_boundryLines != null)
            {
                _viewport.Children.Remove(_boundryLines);
                _boundryLines = null;
            }

            if (_chasedBall == null)
            {
                return;
            }

            Vector3D min = -_chasedBall.Boundry;
            Vector3D max = _chasedBall.Boundry;

            Point3D[] points = new Point3D[8];

            points[0] = new Point3D(min.X, min.Y, max.Z);
            points[1] = new Point3D(max.X, min.Y, max.Z);
            points[2] = new Point3D(max.X, max.Y, max.Z);
            points[3] = new Point3D(min.X, max.Y, max.Z);

            points[4] = new Point3D(min.X, min.Y, min.Z);
            points[5] = new Point3D(max.X, min.Y, min.Z);
            points[6] = new Point3D(max.X, max.Y, min.Z);
            points[7] = new Point3D(min.X, max.Y, min.Z);

            Color colorX = UtilityWPF.ColorFromHex("80" + ChaseColors.X);
            Color colorY = UtilityWPF.ColorFromHex("80" + ChaseColors.Y);
            Color colorZ = UtilityWPF.ColorFromHex("80" + ChaseColors.Z);
            double thickness = .1;

            Model3DGroup group = new Model3DGroup();

            // X
            group.Children.Add(new BillboardLine3D() { Color = colorX, IsReflectiveColor = false, Thickness = thickness, FromPoint = points[0], ToPoint = points[1] }.Model);
            group.Children.Add(new BillboardLine3D() { Color = colorX, IsReflectiveColor = false, Thickness = thickness, FromPoint = points[3], ToPoint = points[2] }.Model);
            group.Children.Add(new BillboardLine3D() { Color = colorX, IsReflectiveColor = false, Thickness = thickness, FromPoint = points[4], ToPoint = points[5] }.Model);
            group.Children.Add(new BillboardLine3D() { Color = colorX, IsReflectiveColor = false, Thickness = thickness, FromPoint = points[7], ToPoint = points[6] }.Model);

            // Y
            group.Children.Add(new BillboardLine3D() { Color = colorY, IsReflectiveColor = false, Thickness = thickness, FromPoint = points[0], ToPoint = points[3] }.Model);
            group.Children.Add(new BillboardLine3D() { Color = colorY, IsReflectiveColor = false, Thickness = thickness, FromPoint = points[1], ToPoint = points[2] }.Model);
            group.Children.Add(new BillboardLine3D() { Color = colorY, IsReflectiveColor = false, Thickness = thickness, FromPoint = points[4], ToPoint = points[7] }.Model);
            group.Children.Add(new BillboardLine3D() { Color = colorY, IsReflectiveColor = false, Thickness = thickness, FromPoint = points[5], ToPoint = points[6] }.Model);

            // Z
            group.Children.Add(new BillboardLine3D() { Color = colorZ, IsReflectiveColor = false, Thickness = thickness, FromPoint = points[0], ToPoint = points[4] }.Model);
            group.Children.Add(new BillboardLine3D() { Color = colorZ, IsReflectiveColor = false, Thickness = thickness, FromPoint = points[1], ToPoint = points[5] }.Model);
            group.Children.Add(new BillboardLine3D() { Color = colorZ, IsReflectiveColor = false, Thickness = thickness, FromPoint = points[2], ToPoint = points[6] }.Model);
            group.Children.Add(new BillboardLine3D() { Color = colorZ, IsReflectiveColor = false, Thickness = thickness, FromPoint = points[3], ToPoint = points[7] }.Model);

            // Visual
            _boundryLines = new ModelVisual3D()
            {
                Content = group,
            };

            _viewport.Children.Add(_boundryLines);
        }

        #endregion
    }
}
