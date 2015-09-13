using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
using Game.Newt.v2.GameItems.Controls;
using Game.Newt.v2.GameItems.ShipEditor;
using Game.Newt.v2.GameItems.ShipParts;
using Game.Newt.v2.NewtonDynamics;
using Game.HelperClassesWPF.Controls2D;

namespace Game.Newt.v2.FlyingBeans
{
    /// <summary>
    /// This is meant to be a simple alife simulation.  The ships get selected for flying as high as they can
    /// </summary>
    public partial class FlyingBeansWindow : Window
    {
        #region Class: BoundryField

        /// <summary>
        /// This will be an inward pointing force near the boundry of the map.  It is a way to slow things down before
        /// hitting the edge of the map, and can be thought of as a cylindrical shell
        /// </summary>
        /// <remarks>
        /// This is a tweaked copy of Game.Newt.v2.GameItems.GravityField.BoundryField
        /// 
        /// May want to make a more generic class in the future
        /// </remarks>
        public class BoundryField
        {
            #region Declaration Section

            // These multiply a point by these ratios to get a spherical map
            private readonly double _ratioX;
            private readonly double _ratioY;
            //private readonly double _ratioZ;

            // The radius where the boundry starts and stops
            private readonly double _boundryStart;
            private readonly double _boundryStartSquared;
            private readonly double _boundryStop;

            // This is the c in: force = c * x^2
            private readonly double _equationConstant;

            // This is the center point between MapMin and MapMax
            private readonly Point3D _center;

            #endregion

            #region Constructor

            public BoundryField(double startPercent, double strengthHalf, double exponent, Point3D mapMin, Point3D mapMax)
            {
                this.StartPercent = startPercent;
                this.StrengthHalf = strengthHalf;
                this.Exponent = exponent;
                this.MapMin = mapMin;
                this.MapMax = mapMax;

                #region Initialize

                // Center point
                _center = new Point3D((this.MapMin.X + this.MapMax.X) / 2d, (this.MapMin.Y + this.MapMax.Y) / 2d, (this.MapMin.Z + this.MapMax.Z) / 2d);

                Vector3D offset = this.MapMax - _center;
                double maxValue = Math.Max(offset.X, Math.Max(offset.Y, offset.Z));

                // Boundries
                _boundryStop = maxValue;
                _boundryStart = _boundryStop * this.StartPercent;
                _boundryStartSquared = _boundryStart * _boundryStart;

                // force = c * x^2
                // c = force / x^2
                _equationConstant = this.StrengthHalf / Math.Pow((_boundryStop - _boundryStart) * .5d, this.Exponent);

                // Ratios
                if (Math1D.IsNearZero(offset.X))
                {
                    _ratioX = 1d;
                }
                else
                {
                    _ratioX = maxValue / offset.X;
                }

                if (Math1D.IsNearZero(offset.Y))
                {
                    _ratioY = 1d;
                }
                else
                {
                    _ratioY = maxValue / offset.Y;
                }

                //if (Math3D.IsNearZero(offset.Z))
                //{
                //    _ratioZ = 1d;
                //}
                //else
                //{
                //    _ratioZ = maxValue / offset.Z;
                //}

                #endregion
            }

            #endregion

            #region Public Properties

            /// <summary>
            /// This is the percent of the map's size where the boundry force starts to be felt (a good value is .85)
            /// </summary>
            public readonly double StartPercent;
            /// <summary>
            /// This is the strength of the force halfway between the start of the boundry and the map's edge (the strength doesn't climb linearly, it's quadratic)
            /// </summary>
            public readonly double StrengthHalf;
            /// <summary>
            /// This is how quickly strength should climb as distance goes past the boundry start.  This is the n in:
            ///		force = c * x^n
            /// </summary>
            public readonly double Exponent;

            public readonly Point3D MapMin;
            public readonly Point3D MapMax;

            #endregion

            #region Public Methods

            public Vector3D GetForce(Point3D point)
            {
                // I can't figure out how to do this with an elliptical map, so scale it to a sphere, do the calculations, then squash back to an ellipse
                Vector3D dirToCenter = point - _center;
                //dirToCenter.Z = 0d;		// Z is zero, because it's a cylinder
                dirToCenter.Z = (Math.Abs(dirToCenter.X) + Math.Abs(dirToCenter.Y)) / 2d;		// I decided to push down (to avoid cheating by pushing against the boundry)
                //Vector3D scaledDirToCenter = new Vector3D(dirToCenter.X * _ratioX, dirToCenter.Y * _ratioY, dirToCenter.Z * _ratioZ);
                Vector3D scaledDirToCenter = new Vector3D(dirToCenter.X * _ratioX, dirToCenter.Y * _ratioY, 0d);		// Z is zero, because it's a cylinder

                if (scaledDirToCenter.LengthSquared > _boundryStartSquared)
                {
                    double distFromBoundry = scaledDirToCenter.Length - _boundryStart;     // wait till now to do the expensive operation.

                    double force = _equationConstant * Math.Pow(distFromBoundry, this.Exponent);

                    // Notice that the magnitude of the force doesn't scale, but the direction is toward the center in the orig elliptical form
                    return dirToCenter.ToUnit() * -force;
                }
                else
                {
                    return new Vector3D(0, 0, 0);
                }
            }

            #endregion
        }

        #endregion
        #region Class: ExplodingBean

        private class ExplodingBean : ExplosionWithVisual
        {
            public ExplodingBean(Bean bean, double waveSpeed, double forceAtCenter, double maxRadius, Viewport3D viewport, double visualStartRadius)
                : base(bean.PhysicsBody, waveSpeed, forceAtCenter, maxRadius, viewport, visualStartRadius)
            {
                this.Bean = bean;
            }

            public Bean Bean
            {
                get;
                private set;
            }
        }

        #endregion
        #region Class: SelectedBean

        private class SelectedBean
        {
            #region Constructor

            public SelectedBean(Bean bean, ShipViewerWindow viewer, Vector3D cameraOffset)
            {
                this.Bean = bean;
                this.Viewer = viewer;
                this.CameraOffset = cameraOffset;

                this.Viewer.Closed += new EventHandler(Viewer_Closed);
            }

            #endregion

            #region Public Properties

            public readonly Bean Bean;
            public ShipViewerWindow Viewer;

            public Vector3D CameraOffset;

            #endregion

            #region Event Listeners

            private void Viewer_Closed(object sender, EventArgs e)
            {
                this.Viewer = null;
            }

            #endregion
        }

        #endregion

        #region Enum: ReportLineType

        private enum ReportLineType
        {
            Header,
            Final,
            Candidate,
            Live
        }

        #endregion

        #region Declaration Section

        public const string OPTIONSFOLDER = "Flying Beans";
        public const string SESSIONFOLDERPREFIX = "Session";
        public const string SAVEFOLDERPREFIX = "Save";
        public const string AUTOSAVESUFFIX = "(auto)";

        private const double TERRAINRADIUS = 100d;
        private const double STARTHEIGHT = 15d;

        private Point3D _boundryMin;
        private Point3D _boundryMax;

        private FlyingBeanSession _session = null;
        private FlyingBeanOptions _options = null;
        private BeanColors _colors = new BeanColors();
        private EditorOptions _editorOptions = new EditorOptions();
        private ItemOptions _itemOptions = null;

        private World _world = null;
        private Map _map = null;

        private UpdateManager _updateManager = null;

        //private GravityFieldUniform _gravity = null;		// this one is now in options
        private RadiationField _radiation = null;
        private BoundryField _boundryField = null;

        private MaterialManager _materialManager = null;
        private int _material_Terrain = -1;
        private int _material_Bean = -1;
        private int _material_ExplodingBean = -1;		// there is no property on body to turn off collision detection, that's done by its current material
        private int _material_Projectile = -1;

        private Body _terrain = null;
        private List<Bean> _beans = new List<Bean>();
        private List<ExplodingBean> _explosions = new List<ExplodingBean>();

        // These keep track of what's being created (the creation is done in another thread)
        private int _beansCreatingCount = 0;
        private List<string> _creatingCandidateWinnerList = new List<string>();
        private List<string> _creatingFinalWinnerList = new List<string>();

        /// <summary>
        /// If the user clicks on a bean, this will be non null
        /// </summary>
        /// <remarks>
        /// TODO: Draw a 2D circle around this (or some kind of 2D graphic to show that it's selected
        /// </remarks>
        private SelectedBean _selectedBean = null;

        private DateTime _lastWinnerScan = DateTime.UtcNow;
        private WinnerManager _winnerManager = null;

        private ScreenSpaceLines3D _boundryLines = null;

        /// <summary>
        /// This listens to the mouse/keyboard and controls the camera
        /// </summary>
        private TrackBallRoam _trackball = null;

        // These are to make the buttons along the top feel like a tab control
        private Border _pressedButton = null;
        private PanelFile _panelFile = null;
        private PanelBeanTypes _panelBeanTypes = null;
        private PanelBeanProps _panelBeanProps = null;
        private PanelMutation _panelMutation = null;
        private PanelTracking _panelTracking = null;
        private PanelSimulation _panelSimulation = null;

        private Brush _statsForegroundBright = new SolidColorBrush(UtilityWPF.ColorFromHex("C0FFFFFF"));
        private Brush _statsForegroundMed = new SolidColorBrush(UtilityWPF.ColorFromHex("90F8F8F8"));
        private Brush _statsForegroundDim = new SolidColorBrush(UtilityWPF.ColorFromHex("60F0F0F0"));

        // Height text on mouse over
        private Lazy<FontFamily> _heightFont = new Lazy<FontFamily>(() => GetBestHeightFont());
        private Brush _heightBrush = new SolidColorBrush(UtilityWPF.ColorFromHex("FFFFFF"));
        private Brush _heightBrushOutline = new SolidColorBrush(UtilityWPF.ColorFromHex("40000000"));

        /// <summary>
        /// This is the height text that's added to pnlVisuals2D
        /// </summary>
        /// <remarks>
        /// Item1 = OutlinedTextBlock
        /// Item2 = Remove time
        /// </remarks>
        private Tuple<UIElement, DateTime> _heightText = null;

        private DateTime _windowStartTime = DateTime.UtcNow;
        private DateTime? _lastAutosave = null;

        #endregion

        #region Constructor

        public FlyingBeansWindow()
        {
            InitializeComponent();
        }

        #endregion

        #region Public Properties

        public string TotalBeansText
        {
            get { return (string)GetValue(TotalBeansTextProperty); }
            set { SetValue(TotalBeansTextProperty, value); }
        }
        public static readonly DependencyProperty TotalBeansTextProperty = DependencyProperty.Register("TotalBeansText", typeof(string), typeof(FlyingBeansWindow), new UIPropertyMetadata("0"));

        #endregion

        #region Event Listeners

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                double terrainHeight = TERRAINRADIUS / 20d;

                #region Load last save

                _session = new FlyingBeanSession();
                _options = new FlyingBeanOptions();
                _itemOptions = new ItemOptions();

                _panelFile = new PanelFile(null, _session, _options, _itemOptions, GetDefaultBeans());
                _panelFile.SessionChanged += new EventHandler(PanelFile_SessionChanged);
                if (!_panelFile.TryLoadLastSave(false))
                {
                    _panelFile.New(false, false);		// by calling new, all of the options initialization is done by the file panel instead of doing it here
                }

                #endregion

                #region Winners

                _winnerManager = new WinnerManager(_options.WinnersLive, _options.WinnerCandidates, _options.WinnersFinal, _options.FinalistCount);

                #endregion
                #region Init World

                double boundryXY = TERRAINRADIUS * 1.25d;

                _boundryMin = new Point3D(-boundryXY, -boundryXY, terrainHeight * -2d);
                _boundryMax = new Point3D(boundryXY, boundryXY, TERRAINRADIUS * 25d);

                _world = new World();
                _world.Updating += new EventHandler<WorldUpdatingArgs>(World_Updating);

                List<Point3D[]> innerLines, outerLines;
                _world.SetCollisionBoundry(out innerLines, out outerLines, _boundryMin, _boundryMax);

                // Draw the lines
                _boundryLines = new ScreenSpaceLines3D(true);
                _boundryLines.Thickness = 1d;
                _boundryLines.Color = _colors.BoundryLines;
                _viewport.Children.Add(_boundryLines);

                foreach (Point3D[] line in innerLines)
                {
                    _boundryLines.AddLine(line[0], line[1]);
                }

                #endregion
                #region Materials

                _materialManager = new MaterialManager(_world);

                // Terrain
                Game.Newt.v2.NewtonDynamics.Material material = new Game.Newt.v2.NewtonDynamics.Material();
                material.Elasticity = .1d;
                _material_Terrain = _materialManager.AddMaterial(material);

                // Bean
                material = new Game.Newt.v2.NewtonDynamics.Material();
                _material_Bean = _materialManager.AddMaterial(material);

                // Exploding Bean
                material = new Game.Newt.v2.NewtonDynamics.Material();
                material.IsCollidable = false;
                _material_ExplodingBean = _materialManager.AddMaterial(material);

                // Projectile
                material = new Game.Newt.v2.NewtonDynamics.Material();
                _material_Projectile = _materialManager.AddMaterial(material);

                _materialManager.RegisterCollisionEvent(0, _material_Bean, Collision_BeanTerrain);		// zero should be the boundry (it should be the default material if no other is applied)
                _materialManager.RegisterCollisionEvent(_material_Terrain, _material_Bean, Collision_BeanTerrain);
                _materialManager.RegisterCollisionEvent(_material_Bean, _material_Bean, Collision_BeanBean);

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
                _trackball.UserMovedCamera += new EventHandler<UserMovedCameraArgs>(Trackball_UserMovedCamera);
                _trackball.GetOrbitRadius += new EventHandler<GetOrbitRadiusArgs>(Trackball_GetOrbitRadius);

                #endregion
                #region Map

                _map = new Map(_viewport, null, _world);
                _map.SnapshotFequency_Milliseconds = 125;
                _map.SnapshotMaxItemsPerNode = 10;
                _map.ShouldBuildSnapshots = false;// true;
                _map.ShouldShowSnapshotLines = false;
                _map.ShouldSnapshotCentersDrift = true;

                _updateManager = new UpdateManager(
                    new Type[] { typeof(Bean) },
                    new Type[] { typeof(Bean) },
                    _map);

                #endregion
                #region Terrain

                //TODO: Texture map this so it's not so boring
                #region WPF Model (plus collision hull)

                // Material
                MaterialGroup materials = new MaterialGroup();
                materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(_colors.Terrain)));
                materials.Children.Add(_colors.TerrainSpecular);

                // Geometry Model
                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = materials;
                geometry.BackMaterial = materials;

                geometry.Geometry = UtilityWPF.GetCylinder_AlongX(100, TERRAINRADIUS, terrainHeight);
                CollisionHull hull = CollisionHull.CreateCylinder(_world, 0, TERRAINRADIUS, terrainHeight, null);

                // Transform
                Transform3DGroup transform = new Transform3DGroup();		// rotate needs to be added before translate
                transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), -90)));
                transform.Children.Add(new TranslateTransform3D(new Vector3D(0, 0, terrainHeight / -2d)));		// I want the objects to be able to add to z=0

                // Model Visual
                ModelVisual3D model = new ModelVisual3D();
                model.Content = geometry;
                model.Transform = transform;

                // Add to the viewport
                _viewport.Children.Add(model);

                #endregion

                // Make a physics body that represents this shape
                _terrain = new Body(hull, transform.Value, 0, new Visual3D[] { model });		// using zero mass tells newton it's static scenery (stuff bounces off of it, but it will never move)
                hull.Dispose();
                _terrain.MaterialGroupID = _material_Terrain;

                #endregion
                #region Fields

                // gravity was done by the file panel

                _radiation = new RadiationField();
                _radiation.AmbientRadiation = 0d;

                _boundryField = new BoundryField(.5d, 7500d, 2d, _boundryMin, _boundryMax);

                #endregion

                // Doing this so that if they hit the import button, it will call a method in beantypes (not the best design, but it works)
                _panelBeanTypes = new PanelBeanTypes(_options, _world);
                _panelFile.BeanTypesPanel = _panelBeanTypes;

                this.TotalBeansText = _options.TotalBeans.ToString("N0");

                _world.UnPause();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (_panelFile != null)
                {
                    _panelFile.Save(false, false, AUTOSAVESUFFIX);
                }
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
                _world.Pause();     // this will stop firing World_Updating, which will stop trying to create new ships

                // Wait until there is nothing left
                while (_beansCreatingCount > 0 || _creatingCandidateWinnerList.Count > 0 || _creatingFinalWinnerList.Count > 0)
                {
                    //TODO: Find an alternative to doevents.  Can't do a .ContinueWith on tasks, because this form close will finish before it should
                    //These approaches all hang the current thread:
                    //
                    //Thread.Sleep(0);
                    //
                    //Thread.Yield();
                    //
                    //Thread waitThread = new Thread(() => Thread.Sleep(100));
                    //waitThread.Start();
                    //waitThread.Join();
                    //
                    //  AutoResetEvent _waitHandle;     //waithandle is meant for cross thread pulses, not same thread
                    //  _waitHandle.Set();        // would get set within AddBeanAsync, etc
                    //_waitHandle.WaitOne(100);

                    // The problem is the design.  _map, _world, etc are members of this class.  Going forward, make them members of some
                    // central pool that keeps tasks for item creations/destructions, and does a continue with all to do the actual tear down of
                    // map, world, etc
                    System.Windows.Forms.Application.DoEvents();
                }

                // Explosions
                foreach (ExplodingBean explosion in _explosions.ToArray())
                {
                    DisposeExplosion(explosion);
                }

                _updateManager.Dispose();

                _map.Dispose();		// this will dispose the physics bodies
                _map = null;

                _terrain.Dispose();

                _world.Pause();
                _world.Dispose();
                _world = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PanelFile_SessionChanged(object sender, EventArgs e)
        {
            try
            {
                //NOTE: This method has many statements, and isn't very threadsafe, but even if there were several threads hitting these options there shouldn't
                //be any damage with the panels being momentarily out of sync

                // Kill all current
                if (_selectedBean != null && _selectedBean.Viewer != null)
                {
                    _selectedBean.Viewer.Close();
                }

                _selectedBean = null;
                foreach (Bean bean in _beans.ToArray())		// using toarray, because DisposeBean removes from the list
                {
                    DisposeBean(bean);
                }
                foreach (ExplodingBean explosion in _explosions.ToArray())
                {
                    DisposeExplosion(explosion);
                }

                // Get the new session
                _session = _panelFile.Session;
                _options = _panelFile.Options;
                _itemOptions = _panelFile.ItemOptions;

                #region Refresh options panels

                if (_panelBeanTypes != null)
                {
                    _panelBeanTypes.Options = _options;
                }

                if (_panelBeanProps != null)
                {
                    _panelBeanProps.Options = _options;
                    _panelBeanProps.ItemOptions = _itemOptions;
                }

                if (_panelMutation != null)
                {
                    _panelMutation.Options = _options;
                }

                if (_panelTracking != null)
                {
                    _panelTracking.Options = _options;
                }

                if (_panelSimulation != null)
                {
                    _panelSimulation.Options = _options;
                }

                #endregion

                // Refresh winner manager
                if (_winnerManager != null)
                {
                    _winnerManager.Live = _options.WinnersLive;
                    _winnerManager.Candidates = _options.WinnerCandidates;
                    _winnerManager.Final = _options.WinnersFinal;
                }

                RefreshStats();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void PanelTracking_WinnerListsRecreated(object sender, EventArgs e)
        {
            try
            {
                _winnerManager.Live = _options.WinnersLive;
                _winnerManager.Candidates = _options.WinnerCandidates;
                _winnerManager.Final = _options.WinnersFinal;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void PanelTracking_NumFinalistsChanged(object sender, EventArgs e)
        {
            try
            {
                _winnerManager.FinalistCount = _options.FinalistCount;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void PanelTracking_KillLivingBeans(object sender, EventArgs e)
        {
            try
            {
                foreach (Bean bean in _beans.ToArray())
                {
                    // There is no instant kill method, but this will kill it during the next update
                    bean.CollidedBean();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void World_Updating(object sender, WorldUpdatingArgs e)
        {
            DateTime now = DateTime.UtcNow;

            _updateManager.Update_MainThread(e.ElapsedTime);

            #region Explosions

            int index = 0;
            while (index < _explosions.Count)
            {
                ExplodingBean explosion = _explosions[index];

                // Tell this explosion to advance its shockwave
                if (explosion.Update(e.ElapsedTime))
                {
                    // It has expired, remove it
                    DisposeExplosion(explosion);
                }
                else
                {
                    index++;
                }
            }

            #endregion

            if (_beans.Count + _beansCreatingCount < _options.NumBeansAtATime)
            {
                // Add new bean
                AddBeanAsync();     // fire and forget
            }

            if (_selectedBean != null)
            {
                // Chase bean
                _camera.Position = _selectedBean.Bean.PositionWorld + _selectedBean.CameraOffset;
            }

            if ((now - _lastWinnerScan).TotalSeconds > _options.TrackingScanFrequencySeconds)
            {
                FindWinners();
                RefreshStats();

                _lastWinnerScan = now;
            }

            if (_heightText != null && now > _heightText.Item2)
            {
                pnlVisuals2D.Children.Remove(_heightText.Item1);
                _heightText = null;
            }

            #region Autosave

            double elapsedFromStart = (DateTime.UtcNow - _windowStartTime).TotalMinutes;

            if (_lastAutosave == null)
            {
                if (elapsedFromStart > 3)
                {
                    _panelFile.Save(true, true, AUTOSAVESUFFIX);
                    _lastAutosave = DateTime.UtcNow;
                }
            }
            else
            {
                double autosaveElapse;

                // Figure out the delay
                if (elapsedFromStart <= 15d)
                {
                    autosaveElapse = 3d;		// 3 minutes between saves for the first 15 minutes
                }
                else if (elapsedFromStart <= 30d)
                {
                    autosaveElapse = 5d;		// 5 between for next 15 minutes
                }
                else if (elapsedFromStart <= 60d)
                {
                    autosaveElapse = 10d;		// 10 between for next 30 minutes
                }
                else if (elapsedFromStart <= 120d)
                {
                    autosaveElapse = 20d;		// 20 between for next hour
                }
                else
                {
                    autosaveElapse = 30d;		// every half hour after that
                }

                if ((DateTime.UtcNow - _lastAutosave.Value).TotalMinutes > autosaveElapse)
                {
                    _panelFile.Save(true, true, AUTOSAVESUFFIX);
                    _lastAutosave = DateTime.UtcNow;
                }
            }

            #endregion
        }
        private void Bean_ApplyForceAndTorque(object sender, BodyApplyForceAndTorqueArgs e)
        {
            if (e.Body.MaterialGroupID == _material_Bean && !e.Body.IsAsleep)
            {
                // Apply gravity
                e.Body.AddForce(_options.GravityField.Gravity * e.Body.Mass);

                // Apply an in-facing cylindrical force
                e.Body.AddForce(_boundryField.GetForce(e.Body.Position));

                // Nearby explosions
                foreach (Explosion explosion in _explosions)
                {
                    explosion.ApplyForceToBody(e.Body);
                }
            }
        }

        private void Collision_BeanTerrain(object sender, MaterialCollisionArgs e)
        {
            try
            {
                // Find the affected bean
                Body beanBody = e.GetBody(_material_Bean);
                Bean bean = _beans.Where(o => o.PhysicsBody.Equals(beanBody)).FirstOrDefault();

                // Inform it
                if (bean != null)
                {
                    bean.CollidedGround();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Collision_BeanBean(object sender, MaterialCollisionArgs e)
        {
            try
            {
                Bean bean = _beans.Where(o => o.PhysicsBody.Equals(e.Body0)).FirstOrDefault();
                if (bean != null)
                {
                    bean.CollidedBean();
                }

                bean = _beans.Where(o => o.PhysicsBody.Equals(e.Body1)).FirstOrDefault();
                if (bean != null)
                {
                    bean.CollidedBean();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Bean_Dead(object sender, DeadBeanArgs e)
        {
            try
            {
                Bean senderCast = sender as Bean;
                if (senderCast == null)
                {
                    MessageBox.Show("Sender should be a bean", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!_beans.Contains(senderCast))
                {
                    // The update manager asks map for a list of updatable parts.  When explosion is used, the bean isn't removed from the map until the
                    // explosion finishes (to keep the visual around).  Which means that beans can "die" multiple times
                    return;
                }

                // Regardless whether it should explode or not, quit tracking it
                _winnerManager.ShipDied(senderCast);

                if (_options.ShowExplosions)
                {
                    //TODO: The simulation keeps hanging for half a second during explosions.  Figure out if it's a poor graphics card, or something inneficient
                    #region Explode

                    switch (e.Cause)
                    {
                        case CauseOfDeath.Spinning:
                        case CauseOfDeath.BeanCollision:
                        case CauseOfDeath.TerrainCollision:
                            // explode
                            ExplodeBean(senderCast, true, .005d, .0001d);		// can't have a strong force, or the explosion could push another bean straight up, and that bean will start being considered a winner
                            break;

                        case CauseOfDeath.ZeroEnergy:
                        case CauseOfDeath.ZeroFuel:
                        case CauseOfDeath.Old:
                            // for now, just implode
                            ExplodeBean(senderCast, false, .002d, .0001d);
                            break;

                        default:
                            throw new ApplicationException("Unknown CauseOfDeath: " + e.Cause.ToString());
                    }

                    // The rest of these are done when the explosion is finished
                    //_map.RemoveItem(senderCast);
                    _beans.Remove(senderCast);
                    //senderCast.Dispose();

                    #endregion
                }
                else
                {
                    // Remove immediately
                    DisposeBean(senderCast);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PanelButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                #region Get border/text

                string buttonText = "";
                Border border = null;

                // Figure out which button was pushed
                if (e.OriginalSource is TextBlock)
                {
                    TextBlock textblock = (TextBlock)e.OriginalSource;
                    buttonText = textblock.Text;
                    border = (Border)textblock.Parent;
                }
                else if (e.OriginalSource is Border)
                {
                    border = (Border)e.OriginalSource;
                    buttonText = ((TextBlock)border.Child).Text;
                }
                else
                {
                    MessageBox.Show("Unknown panel button", this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                #endregion

                pnlSettings.Children.Clear();

                switch (buttonText)
                {
                    case "File":
                        #region File

                        // This is now always done in Window_Loaded
                        //if (_panelFile == null)
                        //{
                        //    _panelFile = new PanelFile(null, _session, _options, _itemOptions);
                        //    _panelFile.SessionChanged += new EventHandler(PanelFile_SessionChanged);
                        //}

                        pnlSettings.Children.Add(_panelFile);

                        #endregion
                        break;

                    case "Bean Types":
                        #region Bean Types

                        //_panelBeanTypes = _panelBeanTypes ?? new PanelBeanTypes(_options, _world);		// this is now done in window load

                        pnlSettings.Children.Add(_panelBeanTypes);

                        #endregion
                        break;

                    case "Bean Props":
                        #region Bean Props

                        _panelBeanProps = _panelBeanProps ?? new PanelBeanProps(_options, _itemOptions);
                        pnlSettings.Children.Add(_panelBeanProps);

                        #endregion
                        break;

                    case "Mutation":
                        #region Mutation

                        _panelMutation = _panelMutation ?? new PanelMutation(_options);
                        pnlSettings.Children.Add(_panelMutation);

                        #endregion
                        break;

                    case "Tracking":
                        #region Tracking

                        if (_panelTracking == null)
                        {
                            _panelTracking = new PanelTracking(_options);
                            _panelTracking.WinnerListsRecreated += new EventHandler(PanelTracking_WinnerListsRecreated);
                            _panelTracking.NumFinalistsChanged += new EventHandler(PanelTracking_NumFinalistsChanged);
                            _panelTracking.KillLivingBeans += new EventHandler(PanelTracking_KillLivingBeans);
                        }

                        pnlSettings.Children.Add(_panelTracking);

                        #endregion
                        break;

                    case "Simulation":
                        #region Simulation

                        _panelSimulation = _panelSimulation ?? new PanelSimulation(_options);
                        pnlSettings.Children.Add(_panelSimulation);

                        #endregion
                        break;

                    default:
                        MessageBox.Show("", this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                        break;		// even though it's an error, don't return.  Flow through
                }

                _pressedButton = border;
                ColorPanelButtons();
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
                if (e.ChangedButton != MouseButton.Left)
                {
                    // All the special logic in this method is for the left button
                    return;
                }

                #region Tab Buttons

                if (_pressedButton != null)
                {
                    // Hide the currently showing options panel
                    pnlSettings.Children.Clear();
                    _pressedButton = null;
                    ColorPanelButtons();
                }

                #endregion
                #region Select Bean

                var clickedBean = GetMouseOverBean(e);

                if (_selectedBean != null && clickedBean != null && _selectedBean.Bean.Equals(clickedBean.Item1))
                {
                    // This is already selected, don't do anything
                }
                else
                {
                    // Close any existing viewer
                    if (_selectedBean != null && _selectedBean.Viewer != null)
                    {
                        _selectedBean.Viewer.Close();
                    }
                    _selectedBean = null;

                    // Make the camera follow this bean
                    //TODO: Place a 2D graphic behind the selected ship
                    //TODO: Zoom in on the bean
                    if (clickedBean != null)
                    {
                        #region Show Viewer

                        ShipViewerWindow viewer = new ShipViewerWindow(clickedBean.Item1);
                        viewer.Owner = this;		// the other settings like topmost, showintaskbar, etc are already set in the window's xaml
                        viewer.PopupBorder = new SolidColorBrush(UtilityWPF.ColorFromHex("60000000"));
                        viewer.PopupBackground = new SolidColorBrush(UtilityWPF.ColorFromHex("30000000"));
                        viewer.ViewportBorder = new SolidColorBrush(UtilityWPF.ColorFromHex("E0000000"));

                        LinearGradientBrush brush = new LinearGradientBrush();
                        brush.StartPoint = new Point(0, 0);
                        brush.EndPoint = new Point(1, 1);

                        GradientStopCollection gradients = new GradientStopCollection();
                        gradients.Add(new GradientStop(UtilityWPF.ColorFromHex("E0EBEDE4"), 0d));
                        gradients.Add(new GradientStop(UtilityWPF.ColorFromHex("E0DFE0DA"), .1d));
                        gradients.Add(new GradientStop(UtilityWPF.ColorFromHex("E0E0E0E0"), .6d));
                        gradients.Add(new GradientStop(UtilityWPF.ColorFromHex("E0DADBD5"), .9d));
                        gradients.Add(new GradientStop(UtilityWPF.ColorFromHex("E0D7DBCC"), 1d));
                        brush.GradientStops = gradients;

                        viewer.ViewportBackground = brush;

                        viewer.PanelBorder = new SolidColorBrush(UtilityWPF.ColorFromHex("8068736B"));
                        viewer.PanelBackground = new SolidColorBrush(UtilityWPF.ColorFromHex("80424F45"));
                        viewer.Foreground = new SolidColorBrush(UtilityWPF.ColorFromHex("F0F0F0"));

                        Point windowClickPoint = UtilityWPF.TransformToScreen(clickedBean.Item3, grdViewPort);
                        Point popupPoint = new Point(windowClickPoint.X + 50, windowClickPoint.Y - (viewer.Height / 3d));
                        popupPoint = UtilityWPF.EnsureWindowIsOnScreen(popupPoint, new Size(viewer.Width, viewer.Height));		// don't let the popup straddle monitors
                        viewer.Left = popupPoint.X;
                        viewer.Top = popupPoint.Y;

                        viewer.Show();

                        #endregion

                        _selectedBean = new SelectedBean(clickedBean.Item1, viewer, _camera.Position - clickedBean.Item1.PositionWorld);
                    }
                }

                #endregion
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
                var bean = GetMouseOverBean(e);
                if (bean == null)
                {
                    return;
                }

                // Height is just Z
                double height = bean.Item1.PositionWorld.Z;

                UpdateHeightOverlay(height, bean.Item3.X - 8, bean.Item3.Y + 3);      // Shift left a bit so the mouse cursor isn't covering the text
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Trackball_UserMovedCamera(object sender, UserMovedCameraArgs e)
        {
            if (_selectedBean != null)
            {
                // If this doesn't change, then the world update will snap the camera back
                _selectedBean.CameraOffset = _camera.Position - _selectedBean.Bean.PositionWorld;
            }
        }
        private void Trackball_GetOrbitRadius(object sender, GetOrbitRadiusArgs e)
        {
            //TODO: This isn't perfect.  The camera isn't rotating around the ship, but where they clicked.  It's subtle, but while the user is orbiting the camera,
            //the ship will drift relative to the camera
            if (_selectedBean != null)
            {
                e.Result = _selectedBean.CameraOffset.Length;
            }
        }

        #endregion

        #region Private Methods

        private async Task AddBeanAsync()
        {
            try
            {
                // First this is to bump this so the caller won't call this method too many times while beans are being created
                _beansCreatingCount++;

                #region Get creation options

                string newBeanType;
                ShipDNA newBeanDNA;

                lock (_options.Lock)
                {
                    int typeCount = _options.NewBeanList.Keys.Count;
                    if (typeCount == 0)
                    {
                        // they could have cleared the list
                        return;
                    }

                    newBeanType = _options.NewBeanList.Keys.ToArray()[StaticRandom.Next(typeCount)];		// can't get at the keys by position, so turn them into an array
                    newBeanDNA = _options.NewBeanList[newBeanType];
                }

                #endregion

                if (await MaybeAddCandidateAsync(newBeanType))
                {
                    return;
                }

                if (await MaybeAddWinnerAsync(newBeanType))
                {
                    return;
                }

                // Create a new one (not derived from a winner)
                newBeanDNA.ShipLineage = Guid.NewGuid().ToString();
                if (StaticRandom.NextDouble() > .5d)		// 50/50 chance of mutating right away
                {
                    newBeanDNA = MutateUtility.Mutate(newBeanDNA, _options.MutateArgs);
                    newBeanDNA.Generation--;
                }

                await AddBeanAsync(newBeanDNA);
            }
            finally
            {
                _beansCreatingCount--;
            }
        }
        private async Task<bool> MaybeAddCandidateAsync(string newBeanType)
        {
            //if (_creatingCandidateWinnerList.Contains(newBeanType) || !_winnerManager.HasLivingCandidate(newBeanType))
            if (_creatingCandidateWinnerList.Contains(newBeanType) || _winnerManager.HasLivingCandidate(newBeanType))
            {
                // Only spawn one candidate of each type at a time.  This is really important when first loading a previous session.  The candidate
                // list will be full, and if they are all spawned at once, only the top few will be tracked as winners.  You could loose entire lineages
                //
                // Plus, it's really fun to see the obvious winning candidate shoot away compared to the other random bots :)
                return false;
            }

            try
            {
                // Store this (or the above if statement would short out if this method is called many times before the bean creation finishes)
                _creatingCandidateWinnerList.Add(newBeanType);      // NOTE: This gets removed in the finally

                // Find a candidate finalist
                var dna = _winnerManager.GetCandidate(newBeanType);
                if (dna == null)
                {
                    return false;
                }

                // Create the bean
                Bean bean = await AddBeanAsync(dna.Item2);

                // Store it
                _winnerManager.ShipCreated(dna.Item1, bean);

                // Exit Function
                return true;
            }
            finally
            {
                _creatingCandidateWinnerList.Remove(newBeanType);
            }
        }
        private async Task<bool> MaybeAddWinnerAsync(string name)
        {
            // Only use the full NewBeanProbOfWinner if all the lineages are filled (I do this because the default is 95%, and once one lineage was filled, it
            // kept being selected almost all the time.  This way it would be 95% of 25%)
            int usedLineages = _options.WinnersFinal.GetNumUsedLineages(name);
            usedLineages += _creatingFinalWinnerList.Where(o => o == name).Count();

            double percentUsed = Convert.ToDouble(usedLineages) / (_options.WinnersFinal.MaxLineages);

            if (StaticRandom.NextDouble() > percentUsed * _options.NewBeanProbOfWinner)
            {
                return false;
            }

            // Get a random winner
            ShipDNA dna = _options.WinnersFinal.GetWinner(name);
            if (dna == null)
            {
                return false;
            }

            try
            {
                _creatingFinalWinnerList.Add(name);

                // Mutate it
                dna = MutateUtility.Mutate(dna, _options.MutateArgs);

                // Create it
                await AddBeanAsync(dna);

                return true;
            }
            finally
            {
                _creatingFinalWinnerList.Remove(name);      // this could have several of the same name.  Just remove one of them
            }
        }
        private async Task<Bean> AddBeanAsync(ShipDNA dna)
        {
            Bean retVal = await Bean.GetNewBeanAsync(_editorOptions, _itemOptions, _options, dna, _world, _material_Bean, _material_Projectile, _radiation, _options.GravityField, true, true);

            //retVal.PhysicsBody.AngularDamping = new Vector3D(1, 1, 1);
            retVal.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(Bean_ApplyForceAndTorque);
            retVal.Dead += new EventHandler<DeadBeanArgs>(Bean_Dead);

            if (_options.NewBeanRandomOrientation)
            {
                retVal.PhysicsBody.Rotation = Math3D.GetRandomRotation();
            }

            if (_options.NewBeanRandomSpin)
            {
                Vector3D angularVelocity = Math3D.GetRandomVector_Spherical_Shell(1d);
                angularVelocity *= _options.AngularVelocityDeath * .5d;

                retVal.PhysicsBody.AngularVelocity = angularVelocity;
            }

            if (retVal.Energy != null)
            {
                retVal.Energy.QuantityCurrent = retVal.Energy.QuantityMax;
            }
            if (retVal.Fuel != null)
            {
                retVal.Fuel.QuantityCurrent = retVal.Fuel.QuantityMax;
            }
            retVal.RecalculateMass();

            Vector3D position = Math3D.GetRandomVector_Circular(TERRAINRADIUS * .5d);
            retVal.PhysicsBody.Position = new Point3D(position.X, position.Y, STARTHEIGHT);

            _map.AddItem(retVal);
            _beans.Add(retVal);

            _options.TotalBeans++;
            this.TotalBeansText = _options.TotalBeans.ToString("N0");

            return retVal;
        }

        private ExplodingBean ExplodeBean(Bean bean, bool isExplode, double sizeMultiplier, double forceMultiplier)
        {
            double explodeStartRadius = 1d;

            // Don't remove visuals.  Wait until the explosion is complete

            // Turn it into a ghost
            bean.PhysicsBody.ApplyForceAndTorque -= new EventHandler<BodyApplyForceAndTorqueArgs>(Bean_ApplyForceAndTorque);
            bean.PhysicsBody.MaterialGroupID = _material_ExplodingBean;		// the body now won't collide with anything

            double mass = bean.PhysicsBody.Mass;

            // Create the explosion
            ExplodingBean retVal = null;
            if (isExplode)
            {
                // Explode
                retVal = new ExplodingBean(bean, mass * sizeMultiplier * 60d, mass * forceMultiplier * 4000d, mass * sizeMultiplier * 16d, _viewport, explodeStartRadius);
            }
            else
            {
                // Implode
                retVal = new ExplodingBean(bean, mass * sizeMultiplier * 60d, mass * forceMultiplier * -3d, mass * sizeMultiplier * 16d, _viewport, explodeStartRadius);
            }

            _explosions.Add(retVal);

            // Exit Function
            return retVal;
        }
        private void DisposeExplosion(ExplodingBean explosion)
        {
            if (_selectedBean != null && _selectedBean.Bean == explosion.Bean)
            {
                //TODO: Remove any other selection visuals (leave the popup though)

                if (_selectedBean.Viewer != null)
                {
                    //TODO: This will get very annoying
                    _selectedBean.Viewer.Close();
                }

                _selectedBean = null;
            }

            _explosions.Remove(explosion);

            explosion.Dispose();

            //_winnerManager.ShipDied(explosion.Bean);		// this was done when creating the explosion
            _map.RemoveItem(explosion.Bean, true);
            //_beans.Remove(explosion.Bean);		// this was done when creating the explosion

            explosion.Bean.Dispose();
        }
        private void DisposeBean(Bean bean)
        {
            //NOTE: This method should be called for an instant kill of a bean, not called by explosion

            if (_selectedBean != null && _selectedBean.Bean == bean)
            {
                if (_selectedBean.Viewer != null)
                {
                    //TODO: This will get very annoying
                    _selectedBean.Viewer.Close();
                }

                _selectedBean = null;
            }

            _map.RemoveItem(bean, true);
            _beans.Remove(bean);
            bean.Dispose();
        }

        private void FindWinners()
        {
            // Get the min heights to be considered
            double minHeight = STARTHEIGHT * 1.8d;
            var minsByType = _options.WinnersFinal.GetMinsByType();

            // Get the finalists that always need to be scored
            long[] finalists = _winnerManager.GetLivingCandidates();

            List<WinnerList.WinningBean> liveScores = new List<WinnerList.WinningBean>();
            List<WinnerList.WinningBean> finalistScores = new List<WinnerList.WinningBean>();

            // Find the beans that are over the min height
            foreach (Bean bean in _beans)
            {
                double height = bean.PositionWorld.Z;

                if (finalists.Contains(bean.PhysicsBody.Token))
                {
                    // Finalist
                    finalistScores.Add(new WinnerList.WinningBean(bean, height, bean.GetAgeSeconds()));
                }
                else
                {
                    // First spawn
                    double localMin = minHeight;
                    if (minsByType != null)
                    {
                        var min = minsByType.Where(o => o.Item1 == bean.Name && o.Item2 == bean.Lineage).FirstOrDefault();		// only comparing against the same lineage, otherwise the probability of a random bean getting to the height of an accomplished lineage will be slim
                        if (min != null)
                        {
                            localMin = min.Item3;
                        }
                    }

                    if (height > localMin)
                    {
                        liveScores.Add(new WinnerList.WinningBean(bean, height, bean.GetAgeSeconds()));
                    }
                }
            }

            // Call the winner manager
            _winnerManager.RefreshWinners(liveScores.ToArray(), finalistScores.ToArray());
        }

        private void RefreshStats()
        {
            //TODO: Instead of wiping out the textblocks every time, change the values of existing ones, add/remove only as needed (a little more logic, but less expensive)

            grdStats.Children.Clear();
            grdStats.RowDefinitions.Clear();

            var dumpLive = _options.WinnersLive.Current;
            var dumpCandidates = _winnerManager != null ? _winnerManager.GetCandidateScoreDump() : null;		// during first load, this is still null
            var dumpFinal = _options.WinnersFinal.Current;
            if (IsEmpty(dumpLive) && IsEmpty(dumpCandidates) && IsEmpty(dumpFinal))
            {
                return;
            }

            // Add the header row
            RefreshStatsSprtAddRow(ReportLineType.Header, "", "max\r\nheight", "max\r\ngen");

            RefreshStatsSprtDump(ReportLineType.Final, dumpFinal);

            if (!IsEmpty(dumpCandidates) && !IsEmpty(dumpFinal))
            {
                RefreshStatsSprtAddRow(ReportLineType.Live, "", "", "");
            }

            RefreshStatsSprtDump(dumpCandidates);

            if (!IsEmpty(dumpLive) && (!IsEmpty(dumpCandidates) || !IsEmpty(dumpFinal)))
            {
                RefreshStatsSprtAddRow(ReportLineType.Live, "", "", "");
            }

            RefreshStatsSprtDump(ReportLineType.Live, dumpLive);
        }
        private void RefreshStatsSprtDump(ReportLineType lineType, WinnerList.WinningSet[] dump)
        {
            if (dump == null || dump.Length == 0)
            {
                return;
            }

            //NOTE: dump is already sorted by highest score
            foreach (var row in dump)
            {
                // Get maxes for each lineage
                string[] heights = new string[row.BeansByLineage.Length];
                string[] gens = new string[heights.Length];
                for (int cntr = 0; cntr < heights.Length; cntr++)
                {
                    heights[cntr] = Math.Round(row.BeansByLineage[cntr].Item2.Max(o => o.Score), 1).ToString();
                    gens[cntr] = row.BeansByLineage[cntr].Item2.Max(o => o.Ship != null ? o.Ship.Generation : o.DNA.Generation).ToString();
                }

                // There's no need to show the lineage name, it's just a guid
                RefreshStatsSprtAddRow(lineType, row.ShipName, string.Join("\r\n", heights), string.Join("\r\n", gens));
            }
        }
        private void RefreshStatsSprtDump(Tuple<WinnerList.WinningBean, double[]>[] finalistDump)
        {
            if (finalistDump == null || finalistDump.Length == 0)
            {
                return;
            }

            // Report all of these
            foreach (var finalist in finalistDump)
            {
                bool isFirst = true;
                string name = finalist.Item1.Ship.Name + "*";
                string generation = finalist.Item1.Ship.Generation.ToString();

                // Previous scores
                if (finalist.Item2 != null)
                {
                    foreach (double prevScore in finalist.Item2)
                    {
                        RefreshStatsSprtAddRow(ReportLineType.Candidate, isFirst ? name : "", Math.Round(prevScore, 1).ToString(), isFirst ? generation : "");
                        isFirst = false;
                    }
                }

                // Current score
                RefreshStatsSprtAddRow(ReportLineType.Candidate, isFirst ? name : "", Math.Round(finalist.Item1.Score, 1).ToString(), isFirst ? generation : "");
            }
        }
        private void RefreshStatsSprtAddRow(ReportLineType lineType, string name, string maxHeight, string maxGeneration)
        {
            grdStats.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1d, GridUnitType.Auto) });

            int row = grdStats.RowDefinitions.Count - 1;
            VerticalAlignment vertAlign = lineType == ReportLineType.Header ? VerticalAlignment.Bottom : VerticalAlignment.Top;
            FontWeight fontWeight = lineType == ReportLineType.Header ? FontWeights.Bold : FontWeights.Regular;
            Brush foreground = null;
            switch (lineType)
            {
                case ReportLineType.Header:
                case ReportLineType.Final:
                    foreground = _statsForegroundBright;
                    break;

                case ReportLineType.Candidate:
                    foreground = _statsForegroundMed;
                    break;

                case ReportLineType.Live:
                    foreground = _statsForegroundDim;
                    break;
            }

            // Name
            TextBlock text = new TextBlock();
            text.Text = name;
            text.HorizontalAlignment = HorizontalAlignment.Right;
            text.VerticalAlignment = vertAlign;
            text.FontWeight = fontWeight;
            text.Foreground = foreground;
            Grid.SetColumn(text, 0);
            Grid.SetRow(text, row);
            grdStats.Children.Add(text);

            // Height
            text = new TextBlock();
            text.Text = maxHeight;
            text.VerticalAlignment = vertAlign;
            text.FontWeight = fontWeight;
            text.Foreground = foreground;
            Grid.SetColumn(text, 1);
            Grid.SetRow(text, row);
            grdStats.Children.Add(text);

            // Generation
            text = new TextBlock();
            text.Text = maxGeneration;
            text.VerticalAlignment = vertAlign;
            text.FontWeight = fontWeight;
            text.Foreground = foreground;
            Grid.SetColumn(text, 2);
            Grid.SetRow(text, row);
            grdStats.Children.Add(text);
        }

        /// <summary>
        /// This will temporarily show text at the position.  If there is existing text, it is removed.
        /// </summary>
        private void UpdateHeightOverlay(double height, double rightEdgeX, double centerY)
        {
            // Remove existing
            if (_heightText != null)
            {
                pnlVisuals2D.Children.Remove(_heightText.Item1);
                _heightText = null;
            }

            // Create text
            OutlinedTextBlock text = new OutlinedTextBlock()
            {
                Text = height.ToString("N0"),
                FontFamily = _heightFont.Value,
                FontSize = 20,
                FontWeight = FontWeight.FromOpenTypeWeight(800),
                StrokeThickness = 1,
                Fill = _heightBrush,
                Stroke = _heightBrushOutline,
            };

            pnlVisuals2D.Children.Add(text);

            // Force the text to calculate its size
            text.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            Size size = text.DesiredSize;

            Canvas.SetLeft(text, rightEdgeX - size.Width);
            Canvas.SetTop(text, centerY - (size.Height / 2));

            // Store it
            _heightText = new Tuple<UIElement, DateTime>(text, DateTime.UtcNow + TimeSpan.FromMilliseconds(500));
        }
        private static FontFamily GetBestHeightFont()
        {
            return UtilityWPF.GetFont(new string[] { "Verdana", "Lucida Console", "Microsoft Sans Serif", "Arial" });
        }

        private void ColorPanelButtons()
        {
            foreach (Border button in grdPanelButtons.Children)
            {
                if (_pressedButton != null && button == _pressedButton)
                {
                    button.Background = _colors.PanelButtonPressedBackground;		//NOTE: When this is explicitly set, the style's hottrack will be ignored for this button
                }
                else
                {
                    button.ClearValue(Border.BackgroundProperty);		// remove the explicitly set color, and let the style take back over
                }
            }
        }

        private static SortedList<string, ShipDNA> GetDefaultBeans()
        {
            SortedList<string, ShipDNA> retVal = new SortedList<string, ShipDNA>();

            List<ShipPartDNA> parts;
            string name;

            #region Jelly

            parts = new List<ShipPartDNA>();

            parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(0, 0, 0.136589202374517), Orientation = Quaternion.Identity, Scale = new Vector3D(3.34291459364178, 3.34291459364178, 1.35096591424002) });
            parts.Add(new ShipPartDNA() { PartType = EnergyTank.PARTTYPE, Position = new Point3D(0, 0, -0.710326934192006), Orientation = Quaternion.Identity, Scale = new Vector3D(1.65971480283752, 1.65971480283752, 0.296378283717326) });

            parts.Add(new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(0, 0, -1.23663771723587), Orientation = Quaternion.Identity, Scale = new Vector3D(0.970442970385369, 0.970442970385369, 0.970442970385369) });
            parts.Add(new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(0, 0, -1.90189448386525), Orientation = Quaternion.Identity, Scale = new Vector3D(0.589686853798246, 0.589686853798246, 0.589686853798246) });
            parts.Add(new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(0, 0, -2.36643280869577), Orientation = Quaternion.Identity, Scale = new Vector3D(0.385345692349387, 0.385345692349387, 0.385345692349387) });
            parts.Add(new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(0, 0, -2.65414283341088), Orientation = Quaternion.Identity, Scale = new Vector3D(0.194289301853716, 0.194289301853716, 0.194289301853716) });

            parts.Add(new ShipPartDNA() { PartType = SensorGravity.PARTTYPE, Position = new Point3D(-0.167279126352673, 0, 0.981003695761894), Orientation = new Quaternion(0, -0.030037329690099, 0, 0.999548777611723), Scale = new Vector3D(1.55, 1.55, 1.55) });
            parts.Add(new ShipPartDNA() { PartType = SensorSpin.PARTTYPE, Position = new Point3D(0.165492927033909, 0, 0.980874806924927), Orientation = new Quaternion(0, 0.028321963321618, 0, 0.999598852737241), Scale = new Vector3D(1.55, 1.55, 1.55) });

            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(1.3, 0, -.2), Orientation = Quaternion.Identity, Scale = new Vector3D(2.2, 2.2, 2.2), ThrusterType = ThrusterType.One });
            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-0.65, 1.125833025, -.2), Orientation = Quaternion.Identity, Scale = new Vector3D(2.2, 2.2, 2.2), ThrusterType = ThrusterType.One });
            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-0.65, -1.125833025, -.2), Orientation = Quaternion.Identity, Scale = new Vector3D(2.2, 2.2, 2.2), ThrusterType = ThrusterType.One });

            name = "jelly";
            retVal.Add(name, ShipDNA.Create(name, parts));

            #endregion
            #region Pinto

            parts = new List<ShipPartDNA>();

            parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(0, 0, -.019), Orientation = Quaternion.Identity, Scale = new Vector3D(2.317, 2.317, .528) });
            parts.Add(new ShipPartDNA() { PartType = EnergyTank.PARTTYPE, Position = new Point3D(0, 0, -.866), Orientation = Quaternion.Identity, Scale = new Vector3D(.593, .593, .58) });
            parts.Add(new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(0, 0, .633), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1) });
            parts.Add(new ShipPartDNA() { PartType = SensorGravity.PARTTYPE, Position = new Point3D(0, 0, -.436), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1) });

            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(0, .404, -.849), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1), ThrusterType = ThrusterType.One });
            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(0, -.404, -.849), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1), ThrusterType = ThrusterType.One });
            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(.404, 0, -.849), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1), ThrusterType = ThrusterType.One });
            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-.404, 0, -.849), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1), ThrusterType = ThrusterType.One });

            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(0, 0, 1.114), Orientation = new Quaternion(-0.638338294971619, -0.299524684420353, 0.296923852871428, 0.643929662456701), Scale = new Vector3D(0.6, 0.6, 0.6), ThrusterType = ThrusterType.Two_Two });

            name = "pinto";
            retVal.Add(name, ShipDNA.Create(name, parts));

            #endregion
            #region Stalk

            parts = new List<ShipPartDNA>();

            parts.Add(new ShipPartDNA() { PartType = EnergyTank.PARTTYPE, Position = new Point3D(0, 0, 0.288349786775089), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 0.340026791535698) });
            parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(0, 0, -0.74722433066891), Orientation = Quaternion.Identity, Scale = new Vector3D(1.87992287465541, 1.87992287465541, 0.734340412743098) });

            parts.Add(new ShipPartDNA() { PartType = SensorGravity.PARTTYPE, Position = new Point3D(0, 0, 1.1062619218064), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1) });

            parts.Add(new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(0, 0, -0.12531597276071), Orientation = Quaternion.Identity, Scale = new Vector3D(0.59208657689904, 0.59208657689904, 0.59208657689904) });
            parts.Add(new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(0, 0, 0.72498816823002), Orientation = Quaternion.Identity, Scale = new Vector3D(0.59208657689904, 0.59208657689904, 0.59208657689904) });

            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-0.0291614651537778, -0.782249002104148, -1.1062619218064), Orientation = new Quaternion(-0.0666999722512638, -0.0660771744021589, -0.707277569653588, 0.700673504700742), Scale = new Vector3D(1, 1, 1), ThrusterType = ThrusterType.One });
            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-0.638033777397299, 0.50322105060489, -1.1062619218064), Orientation = new Quaternion(0.0885653406256113, -0.0311650399512359, 0.939135006492266, 0.330469908320681), Scale = new Vector3D(1, 1, 1), ThrusterType = ThrusterType.One });
            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(0.638033777397299, 0.443570373185585, -1.1062619218064), Orientation = new Quaternion(0.0269955119951477, -0.0899239768216048, 0.286256792484956, 0.953541802691907), Scale = new Vector3D(1, 1, 1), ThrusterType = ThrusterType.One });

            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-0.44272300614711, -0.307077434080483, 0.268793789036541), Orientation = new Quaternion(-0.180086704883376, -0.053509523192716, -0.941511379827859, 0.279753161387993), Scale = new Vector3D(0.534560752176155, 0.534560752176155, 0.534560752176155), ThrusterType = ThrusterType.One });
            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(0.00416448323274551, 0.533504418882871, 0.268793789036541), Orientation = new Quaternion(0.12502830960747, -0.140222723353822, 0.653660559630295, 0.73309848072121), Scale = new Vector3D(0.534560752176155, 0.534560752176155, 0.534560752176155), ThrusterType = ThrusterType.One });
            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(0.407292033734503, -0.296016549133222, 0.268793789036541), Orientation = new Quaternion(-0.0628231270711854, -0.177052944208962, -0.328445617860712, 0.92565057433139), Scale = new Vector3D(0.534560752176155, 0.534560752176155, 0.534560752176155), ThrusterType = ThrusterType.One });

            name = "stalk";
            retVal.Add(name, ShipDNA.Create(name, parts));

            #endregion
            #region String

            parts = new List<ShipPartDNA>();

            parts.Add(new ShipPartDNA() { PartType = EnergyTank.PARTTYPE, Position = new Point3D(-0.065780376606976, 0.0206728174346873, -1.20374264985011), Orientation = Quaternion.Identity, Scale = new Vector3D(1.28811900111547, 1.28811900111547, 0.574227855242444) });
            parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(-0.0601891503638872, 0.0206728174346873, 0.223628372085138), Orientation = Quaternion.Identity, Scale = new Vector3D(2.10009387593931, 2.10009387593931, 2.16042111091004) });

            parts.Add(new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(-0.0666096766684205, 0.0206728174346873, 1.9504920488317), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1) });
            parts.Add(new ShipPartDNA() { PartType = SensorGravity.PARTTYPE, Position = new Point3D(-0.0550619064508398, 0.0206728174346873, -1.9504920488317), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1) });

            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-0.829012173165579, 0.729869992776589, -0.0234450754607382), Orientation = Quaternion.Identity, Scale = new Vector3D(1.47257164107535, 1.47257164107535, 1.47257164107535), ThrusterType = ThrusterType.Two });
            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-0.946756174735323, -0.539113028637484, -0.0234450754607411), Orientation = Quaternion.Identity, Scale = new Vector3D(1.47257164107535, 1.47257164107535, 1.47257164107535), ThrusterType = ThrusterType.Two });
            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(0.946756174735323, 0.0206728174346873, -0.0234450754607411), Orientation = Quaternion.Identity, Scale = new Vector3D(1.47257164107535, 1.47257164107535, 1.47257164107535), ThrusterType = ThrusterType.Two });
            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(0.333189292179663, 0.962411768488615, -0.0234450754607372), Orientation = Quaternion.Identity, Scale = new Vector3D(1.47257164107535, 1.47257164107535, 1.47257164107535), ThrusterType = ThrusterType.Two });
            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(0.222899318410499, -0.962411768488616, -0.0234450754607421), Orientation = Quaternion.Identity, Scale = new Vector3D(1.47257164107535, 1.47257164107535, 1.47257164107535), ThrusterType = ThrusterType.Two });

            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-0.0363630029284377, 0.0206728174346873, -1.68370006629563), Orientation = new Quaternion(-0.693304267869653, 0.142339165422304, -0.142627994982035, 0.69190028847227), Scale = new Vector3D(0.6, 0.6, 0.6), ThrusterType = ThrusterType.Two_Two });
            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-0.0371673214410038, 0.0206728174346873, 1.44079035638637), Orientation = new Quaternion(-0.690321597175748, -0.156110988896358, 0.156427763749278, 0.68892365785109), Scale = new Vector3D(0.6, 0.6, 0.6), ThrusterType = ThrusterType.Two_Two });

            name = "string";
            retVal.Add(name, ShipDNA.Create(name, parts));

            #endregion
            #region Lima

            parts = new List<ShipPartDNA>();

            parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(0.706889841793355, -0.294925810277785, -1.08252370524933), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 2.14594057176029) });
            parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(0.189430372248921, -0.742152690534958, -1.08252370524933), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 2.14594057176029) });
            parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(-0.468606376726823, 0.605873374303246, -1.08252370524933), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 2.14594057176029) });
            parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(-0.765946787968095, 0, -1.08252370524934), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 2.14594057176029) });
            parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(0.211696107083261, 0.736110888551741, -1.08252370524933), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 2.14594057176029) });
            parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(-0.448187263745845, -0.621130146277456, -1.08252370524933), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 2.14594057176029) });
            parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(0.703154718007737, 0.303723434298543, -1.08252370524933), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 2.14594057176029) });

            parts.Add(new ShipPartDNA() { PartType = EnergyTank.PARTTYPE, Position = new Point3D(-0.00609757721549364, 0, 0.0755817899704276), Orientation = Quaternion.Identity, Scale = new Vector3D(1.78789163279904, 1.78789163279904, 0.663842922328367) });

            parts.Add(new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(0.00270250008617912, 0, 1.18826557731702), Orientation = Quaternion.Identity, Scale = new Vector3D(1.98252384869672, 1.98252384869672, 1.98252384869672) });

            parts.Add(new ShipPartDNA() { PartType = SensorGravity.PARTTYPE, Position = new Point3D(-0.718499823094977, 0, 0.160964473685404), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1) });
            parts.Add(new ShipPartDNA() { PartType = SensorSpin.PARTTYPE, Position = new Point3D(0.694879472628954, 0, 0.160964473685404), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1) });

            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(6.93889390390723E-18, 0, -1.33939158158084), Orientation = Quaternion.Identity, Scale = new Vector3D(2.0983984532584, 2.0983984532584, 2.0983984532584), ThrusterType = ThrusterType.One });
            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-0.00163840313730601, 0, 2.03661652999272), Orientation = new Quaternion(-0.705390553869969, 1.11438393982076E-13, 1.12544401400464E-13, 0.708818853100719), Scale = new Vector3D(0.530599249742847, 0.530599249742847, 0.530599249742847), ThrusterType = ThrusterType.Two_Two_One });

            name = "lima";
            retVal.Add(name, ShipDNA.Create(name, parts));

            #endregion
            #region Beanie

            parts = new List<ShipPartDNA>();

            parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(0, 0, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(2.38879634397741, 2.38879634397741, 3.43631222687401) });
            parts.Add(new ShipPartDNA() { PartType = EnergyTank.PARTTYPE, Position = new Point3D(0.0118438276468016, -1.77635683940025E-15, 2.13393734846931), Orientation = Quaternion.Identity, Scale = new Vector3D(2.63023708895789, 2.63023708895789, 0.618287579224999) });

            parts.Add(new ShipPartDNA() { PartType = SensorGravity.PARTTYPE, Position = new Point3D(0.00590420938837788, 0, -2.01005067291908), Orientation = Quaternion.Identity, Scale = new Vector3D(1.6247878610426, 1.6247878610426, 1.6247878610426) });

            parts.Add(new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(-0.609698871896441, 0.600772790721528, -0.419555698809813), Orientation = Quaternion.Identity, Scale = new Vector3D(0.603736194561437, 0.603736194561437, 0.603736194561437) });
            parts.Add(new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(-0.587157254645341, -0.622821819447964, 0.591934658442033), Orientation = Quaternion.Identity, Scale = new Vector3D(0.603736194561437, 0.603736194561437, 0.603736194561437) });
            parts.Add(new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(0.582819935769687, 0.626882431507332, -0.419555698809812), Orientation = Quaternion.Identity, Scale = new Vector3D(0.603736194561437, 0.603736194561437, 0.603736194561437) });
            parts.Add(new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(-0.638403950415874, 0.570176338124034, 0.591934658442034), Orientation = Quaternion.Identity, Scale = new Vector3D(0.603736194561437, 0.603736194561437, 0.603736194561437) });
            parts.Add(new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(0.630206777735492, -0.579223685426775, 0.591934658442033), Orientation = Quaternion.Identity, Scale = new Vector3D(0.603736194561437, 0.603736194561437, 0.603736194561437) });
            parts.Add(new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(0.505315772917541, 0.690881053513464, 0.591934658442034), Orientation = Quaternion.Identity, Scale = new Vector3D(0.603736194561437, 0.603736194561437, 0.603736194561437) });
            parts.Add(new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(0.630206777735492, -0.579223685426775, -0.419555698809813), Orientation = Quaternion.Identity, Scale = new Vector3D(0.603736194561437, 0.603736194561437, 0.603736194561437) });
            parts.Add(new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(-0.587157254645341, -0.622821819447964, -0.419555698809813), Orientation = Quaternion.Identity, Scale = new Vector3D(0.603736194561437, 0.603736194561437, 0.603736194561437) });

            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-0.00420050748688497, 0.9055968002922, -0.637960430014019), Orientation = new Quaternion(-0.0816321856320731, 0.0812544221862661, 0.700769969991161, -0.704027949944244), Scale = new Vector3D(1, 1, 1), ThrusterType = ThrusterType.One });
            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-0.00420050748688497, 0.9055968002922, 0.69320062996933), Orientation = new Quaternion(-0.0816321856320731, 0.0812544221862661, 0.700769969991161, -0.704027949944244), Scale = new Vector3D(1, 1, 1), ThrusterType = ThrusterType.One });
            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-0.905606542027285, 0, -0.637960430014019), Orientation = new Quaternion(0.115178534701103, -1.46790302215357E-16, 9.96860965801194E-16, 0.993344806773613), Scale = new Vector3D(1, 1, 1), ThrusterType = ThrusterType.One });
            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-0.905606542027285, 0, 0.693200629969329), Orientation = new Quaternion(0.115178534701103, -1.46790302215357E-16, 9.96860965801194E-16, 0.993344806773613), Scale = new Vector3D(1, 1, 1), ThrusterType = ThrusterType.One });
            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-0.00452309582883892, -0.905595246546017, -0.637960430014019), Orientation = new Quaternion(0.0816466564243036, 0.0812398815276393, 0.700644565654573, 0.704152751725662), Scale = new Vector3D(1, 1, 1), ThrusterType = ThrusterType.One });
            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-0.00452309582883892, -0.905595246546017, 0.69320062996933), Orientation = new Quaternion(0.0816466564243036, 0.0812398815276393, 0.700644565654573, 0.704152751725662), Scale = new Vector3D(1, 1, 1), ThrusterType = ThrusterType.One });
            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(0.90397662467943, 0.0543090323592946, -0.637960430014019), Orientation = new Quaternion(-0.00345517116608783, 0.115126698242007, 0.992897748842395, -0.0297987497692547), Scale = new Vector3D(1, 1, 1), ThrusterType = ThrusterType.One });
            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(0.90397662467943, 0.0543090323592946, 0.69320062996933), Orientation = new Quaternion(-0.00345517116608783, 0.115126698242007, 0.992897748842395, -0.0297987497692547), Scale = new Vector3D(1, 1, 1), ThrusterType = ThrusterType.One });

            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(0.551646227305787, -0.520119683463821, 0.0816930546761547), Orientation = new Quaternion(0.260718903777035, 0.656573215269568, -0.657805138762094, -0.26120808873782), Scale = new Vector3D(0.581975018372359, 0.581975018372359, 0.581975018372359), ThrusterType = ThrusterType.Two });
            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-0.492022005383339, -0.576846939357201, 0.0816930546761551), Orientation = new Quaternion(-0.64145533598852, -0.295969231049399, 0.296524555939719, 0.642658893915454), Scale = new Vector3D(0.581975018372359, 0.581975018372359, 0.581975018372359), ThrusterType = ThrusterType.Two });
            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-0.551833197683662, 0.519921308624139, 0.0816930546761547), Orientation = new Quaternion(-0.656620074650258, 0.260600866004715, -0.261089829492096, 0.657852086064695), Scale = new Vector3D(0.581975018372359, 0.581975018372359, 0.581975018372359), ThrusterType = ThrusterType.Two });
            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(0.48919891987111, 0.579243007747312, 0.0816930546761548), Orientation = new Quaternion(-0.297534730087477, 0.640730690842798, -0.641932889123378, 0.298092992312123), Scale = new Vector3D(0.581975018372359, 0.581975018372359, 0.581975018372359), ThrusterType = ThrusterType.Two });

            name = "beanie";
            retVal.Add(name, ShipDNA.Create(name, parts));

            #endregion
            #region Sprout

            parts = new List<ShipPartDNA>();

            parts.Add(new ShipPartDNA() { PartType = EnergyTank.PARTTYPE, Position = new Point3D(2.56356833214327, 0, -0.0634755488556928), Orientation = new Quaternion(0, -0.708173862179761, 0, 0.706038087446704), Scale = new Vector3D(1, 1, 1.47164937305187) });

            parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(-2.23392920471221, 0, -0.0659844378246188), Orientation = new Quaternion(-1.42964347637312E-13, -0.706728672693754, -1.43636758894531E-13, 0.707484687602866), Scale = new Vector3D(1.13746864736113, 1.13746864736113, 3.31707014456383) });
            parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(1.263470100279, 0, -0.0659844378246188), Orientation = new Quaternion(-1.42964347637312E-13, -0.706728672693754, -1.43636758894531E-13, 0.707484687602866), Scale = new Vector3D(4.58604099483389, 4.58604099483389, 1.0928604403201) });
            parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(0.0616761255750133, 0, -0.0748261433689537), Orientation = new Quaternion(-1.42964347637312E-13, -0.706728672693754, -1.43636758894531E-13, 0.707484687602866), Scale = new Vector3D(2.82475188423883, 2.82475188423883, 1.23067070800144) });

            parts.Add(new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(3.57550282704805, 0, -0.0594906102814495), Orientation = Quaternion.Identity, Scale = new Vector3D(0.666343120339225, 0.666343120339225, 0.666343120339225) });
            parts.Add(new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(-4.04788408321676, 0, -0.0594906102814495), Orientation = Quaternion.Identity, Scale = new Vector3D(0.330071733005903, 0.330071733005903, 0.330071733005903) });

            parts.Add(new ShipPartDNA() { PartType = SensorGravity.PARTTYPE, Position = new Point3D(3.97747719762673, 0, -0.0620762870629098), Orientation = Quaternion.Identity, Scale = new Vector3D(1.20029917154606, 1.20029917154606, 1.20029917154606) });

            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(2.76985031871199, 0.442312358432813, -0.652673366679227), Orientation = new Quaternion(-0.670074198587975, -0.22318746517782, 0.672063821348867, 0.222526726053615), Scale = new Vector3D(1.94847340595734, 1.94847340595734, 1.94847340595734), ThrusterType = ThrusterType.One });
            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(2.76985031871199, -0.408013753062594, -0.67679408651013), Orientation = new Quaternion(0.676114523725139, -0.204041336007597, -0.678122081766091, 0.203437278367035), Scale = new Vector3D(1.94847340595734, 1.94847340595734, 1.94847340595734), ThrusterType = ThrusterType.One });
            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(2.76985031871199, -0.0598584163810272, 0.67679408651013), Orientation = new Quaternion(0.0286033997211282, -0.707572896786788, -0.0286883305770025, 0.705478150580053), Scale = new Vector3D(1.94847340595734, 1.94847340595734, 1.94847340595734), ThrusterType = ThrusterType.One });
            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(2.76985031871199, 0.685972649037431, 0.215766756663835), Orientation = new Quaternion(-0.395267807336434, -0.586784964030945, 0.396441459191322, 0.585047806512345), Scale = new Vector3D(1.94847340595734, 1.94847340595734, 1.94847340595734), ThrusterType = ThrusterType.One });
            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(2.76985031871199, -0.71988224035489, 0.108558299857738), Orientation = new Quaternion(0.438609217405511, -0.554941657313832, -0.439911561062356, 0.553298770853852), Scale = new Vector3D(1.94847340595734, 1.94847340595734, 1.94847340595734), ThrusterType = ThrusterType.One });

            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-4.39341008458799, 0, -0.0640098084714286), Orientation = new Quaternion(0.291951362101871, 0.291933510260586, -0.644045787514781, 0.644006406286661), Scale = new Vector3D(0.36843339762354, 0.36843339762354, 0.36843339762354), ThrusterType = ThrusterType.Two_Two });
            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-4.26491529170379, 0, -0.0640098084714286), Orientation = new Quaternion(0, 0, -0.707128400114637, 0.707085161597466), Scale = new Vector3D(0.36843339762354, 0.36843339762354, 0.36843339762354), ThrusterType = ThrusterType.Two_Two });

            name = "sprout";
            retVal.Add(name, ShipDNA.Create(name, parts));

            #endregion
            #region Boston

            parts = new List<ShipPartDNA>();

            parts.Add(new ShipPartDNA() { PartType = SensorGravity.PARTTYPE, Position = new Point3D(0.06262624381726, 0.0452319990776786, 0.0107575852705214), Orientation = Quaternion.Identity, Scale = new Vector3D(1.98055565775194, 1.98055565775194, 1.98055565775194) });

            parts.Add(new ShipPartDNA() { PartType = EnergyTank.PARTTYPE, Position = new Point3D(0.0640302221718942, 0.0452319990776786, -0.398262947326255), Orientation = Quaternion.Identity, Scale = new Vector3D(0.859573285557595, 0.859573285557595, 0.363133980141406) });
            parts.Add(new ShipPartDNA() { PartType = EnergyTank.PARTTYPE, Position = new Point3D(0.0640302221718961, -0.359505731862839, 0.00974082197643137), Orientation = new Quaternion(-0.707994401651682, 0, 0, -0.706218045103548), Scale = new Vector3D(0.859573285557595, 0.859573285557595, 0.363133980141406) });
            parts.Add(new ShipPartDNA() { PartType = EnergyTank.PARTTYPE, Position = new Point3D(0.0640302221718942, 0.0452319990776786, 0.415496593341741), Orientation = Quaternion.Identity, Scale = new Vector3D(0.859573285557595, 0.859573285557595, 0.363133980141406) });
            parts.Add(new ShipPartDNA() { PartType = EnergyTank.PARTTYPE, Position = new Point3D(0.0640302221718945, 0.449970182263215, 0.00994044015096636), Orientation = new Quaternion(-0.707820225525904, 0, 0, 0.706392616281101), Scale = new Vector3D(0.859573285557595, 0.859573285557595, 0.363133980141406) });

            parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(-0.633871040519167, 0.0452319990776786, 0.0105599048346808), Orientation = new Quaternion(-1.06613503606533E-14, 0.706983101400418, 1.04235729042214E-14, 0.707230439343675), Scale = new Vector3D(1.50569057774504, 1.50569057774504, 0.952322625051029) });
            parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(-0.40793184117455, -0.333915145708534, -0.399430190653673), Orientation = new Quaternion(0.0179820697650735, 0.410182924354628, 0.5149540512311, 0.752495142081016), Scale = new Vector3D(0.959781086082721, 0.959781086082721, 0.56790863228087) });
            parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(0.774629433149324, 0.0452319990776786, 0.0105599048346808), Orientation = new Quaternion(-1.06613503606533E-14, 0.706983101400418, 1.04235729042214E-14, 0.707230439343675), Scale = new Vector3D(1.50569057774504, 1.50569057774504, 0.952322625051029) });
            parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(0.315023390587194, 0.619221355357736, -0.505349024898043), Orientation = new Quaternion(0.290518532869616, 0.287747281934623, -0.74359105344961, 0.529030083292266), Scale = new Vector3D(0.829438489501124, 0.829438489501124, 0.737751084688027) });
            parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(0.309455393701602, -0.704869873678094, 0.553399487133765), Orientation = new Quaternion(0.54207375147106, 0.683170759778384, 0.237900525971097, -0.427594551757378), Scale = new Vector3D(0.959781086082721, 0.959781086082721, 0.56790863228087) });
            parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(-0.553193493827276, 0.478546505441548, 0.548071445321983), Orientation = new Quaternion(0.598130585181683, 0.578229996417228, 0.0768855317204965, 0.549525694872958), Scale = new Vector3D(0.959781086082721, 0.959781086082721, 0.56790863228087) });
            parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(-0.135795078147285, -0.112441024889166, 0.931542744345501), Orientation = new Quaternion(0.658120115364394, 0.744147750445601, 0.0233404096588955, 0.112148404074527), Scale = new Vector3D(0.959781086082721, 0.959781086082721, 0.56790863228087) });
            parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(0.318035997738273, 0.58888924058271, 0.53862326999682), Orientation = new Quaternion(0.727956183597861, 0.476918202992041, -0.358769433521041, 0.337510467967247), Scale = new Vector3D(0.959781086082721, 0.959781086082721, 0.56790863228087) });

            parts.Add(new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(-1.39207379925579, 0.31976381759203, -0.144977694320637), Orientation = Quaternion.Identity, Scale = new Vector3D(0.312935800748763, 0.312935800748763, 0.312935800748763) });
            parts.Add(new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(-0.95108934283864, 0.082154705114342, -1.07864171487276), Orientation = Quaternion.Identity, Scale = new Vector3D(0.312935800748763, 0.312935800748763, 0.312935800748763) });
            parts.Add(new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(-1.26237238850117, 0.137887583052503, 0.682744920802546), Orientation = Quaternion.Identity, Scale = new Vector3D(0.312935800748763, 0.312935800748763, 0.312935800748763) });
            parts.Add(new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(-0.209724903293652, 0.759958101622015, -1.26625645655205), Orientation = Quaternion.Identity, Scale = new Vector3D(0.312935800748763, 0.312935800748763, 0.312935800748763) });
            parts.Add(new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(-1.0437807852787, -0.703019537745404, -0.646324247998212), Orientation = Quaternion.Identity, Scale = new Vector3D(0.312935800748763, 0.312935800748763, 0.312935800748763) });
            parts.Add(new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(-0.804024155014174, 1.04164516079897, -0.676156753899043), Orientation = Quaternion.Identity, Scale = new Vector3D(0.312935800748763, 0.312935800748763, 0.312935800748763) });
            parts.Add(new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(0.770385334720267, -0.614876859274918, -1.12021844293319), Orientation = Quaternion.Identity, Scale = new Vector3D(0.312935800748763, 0.312935800748763, 0.312935800748763) });
            parts.Add(new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(0.826083478713381, 1.19816066586892, 0.561812892638671), Orientation = Quaternion.Identity, Scale = new Vector3D(0.312935800748763, 0.312935800748763, 0.312935800748763) });
            parts.Add(new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(0.010054612300213, 1.35065612899851, 0.724114071596212), Orientation = Quaternion.Identity, Scale = new Vector3D(0.312935800748763, 0.312935800748763, 0.312935800748763) });
            parts.Add(new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(0.714123702527216, 0.972122864502635, -0.95474632300179), Orientation = Quaternion.Identity, Scale = new Vector3D(0.312935800748763, 0.312935800748763, 0.312935800748763) });
            parts.Add(new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(-0.488119432659847, -1.33606569285772, -0.0560952239342334), Orientation = Quaternion.Identity, Scale = new Vector3D(0.312935800748763, 0.312935800748763, 0.312935800748763) });
            parts.Add(new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(1.27567102575173, -0.713124822170317, -0.400570386901382), Orientation = Quaternion.Identity, Scale = new Vector3D(0.312935800748763, 0.312935800748763, 0.312935800748763) });
            parts.Add(new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(-0.122574480585318, -1.19141979478037, 0.818325814234051), Orientation = Quaternion.Identity, Scale = new Vector3D(0.312935800748763, 0.312935800748763, 0.312935800748763) });
            parts.Add(new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(1.49619785350449, 0.0968818300980203, 0.40822223383338), Orientation = Quaternion.Identity, Scale = new Vector3D(0.312935800748763, 0.312935800748763, 0.312935800748763) });
            parts.Add(new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(0.591501938051828, -0.897487268188259, 1.03415599018248), Orientation = Quaternion.Identity, Scale = new Vector3D(0.312935800748763, 0.312935800748763, 0.312935800748763) });
            parts.Add(new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(0.602146634482277, 0.495637281658607, 1.32294135345344), Orientation = Quaternion.Identity, Scale = new Vector3D(0.312935800748763, 0.312935800748763, 0.312935800748763) });
            parts.Add(new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(-0.55718253942255, -0.516621575629162, 1.24198910079097), Orientation = Quaternion.Identity, Scale = new Vector3D(0.312935800748763, 0.312935800748763, 0.312935800748763) });
            parts.Add(new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(-0.506679104500458, 0.585267411438967, 1.2756791582447), Orientation = Quaternion.Identity, Scale = new Vector3D(0.312935800748763, 0.312935800748763, 0.312935800748763) });
            parts.Add(new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(-0.910122425235313, 1.08914531989134, 0.434760436071202), Orientation = Quaternion.Identity, Scale = new Vector3D(0.312935800748763, 0.312935800748763, 0.312935800748763) });
            parts.Add(new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(-0.125720513509375, -0.977047473173131, -1.0547205194421), Orientation = Quaternion.Identity, Scale = new Vector3D(0.312935800748763, 0.312935800748763, 0.312935800748763) });

            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-0.106944297856511, 0.516319596743188, -0.773690047891018), Orientation = new Quaternion(-0.580856706784479, -0.699755868826396, 0.0503555530721975, -0.412809312517655), Scale = new Vector3D(1.49381917197275, 1.49381917197275, 1.49381917197275), ThrusterType = ThrusterType.Two });
            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(0.0632171036029679, -0.325806646160234, -0.86582557639219), Orientation = new Quaternion(0.737785723862743, -0.233171415729761, -0.625831434735991, 0.0981750062245953), Scale = new Vector3D(1.49381917197275, 1.49381917197275, 1.49381917197275), ThrusterType = ThrusterType.Two });
            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-0.263418167319983, 0.910216337620467, 0.35234290788821), Orientation = new Quaternion(-0.709121607920553, -0.661587817411694, 0.0915158899314037, 0.22599324530318), Scale = new Vector3D(1.49381917197275, 1.49381917197275, 1.49381917197275), ThrusterType = ThrusterType.Two });
            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(0.715330638117321, 0.783214769802288, -0.289009839133333), Orientation = new Quaternion(-0.537212591502583, -0.0434864433563857, 0.354446845047803, 0.764119751616819), Scale = new Vector3D(1.49381917197275, 1.49381917197275, 1.49381917197275), ThrusterType = ThrusterType.Two });
            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-0.905756167892496, 0.0225358707746821, 0.86582557639219), Orientation = new Quaternion(0.0813043687631664, -0.919271965928561, 0.22938801712521, 0.309369988649878), Scale = new Vector3D(1.49381917197275, 1.49381917197275, 1.49381917197275), ThrusterType = ThrusterType.Two });
            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(0.628431968023481, 0.045231999077678, 0.865721193462417), Orientation = new Quaternion(-0.253918376332788, -0.427868930729674, 0.0132499643648232, 0.867339653608145), Scale = new Vector3D(1.49381917197275, 1.49381917197275, 1.49381917197275), ThrusterType = ThrusterType.Two });
            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-0.680853218475483, -0.550318077194731, 0.0397893092320309), Orientation = new Quaternion(-0.211451384238473, 0.291481640750684, -0.61282901311167, 0.703397018686815), Scale = new Vector3D(1.49381917197275, 1.49381917197275, 1.49381917197275), ThrusterType = ThrusterType.Two });
            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(0.206114029499385, -0.910216337620466, 0.0210624637402835), Orientation = new Quaternion(0.372907831130986, -0.397746694239423, -0.43263915221213, 0.718025543191859), Scale = new Vector3D(1.49381917197275, 1.49381917197275, 1.49381917197275), ThrusterType = ThrusterType.Two });

            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(0.928614909100397, 0.820167522520978, 0.509485111341641), Orientation = new Quaternion(-0.0154277148958784, -0.45422481961171, 0.34155915471622, -0.822665875487335), Scale = new Vector3D(0.505726283020722, 0.505726283020722, 0.505726283020722), ThrusterType = ThrusterType.Two_Two_Two });
            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-0.043681216997261, -0.632488255381253, 1.07310619077886), Orientation = new Quaternion(0.311918646345471, 0.258046065296276, 0.179708948696996, 0.896562145088303), Scale = new Vector3D(0.505726283020722, 0.505726283020722, 0.505726283020722), ThrusterType = ThrusterType.Two_Two_Two });
            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-0.96687180750838, 0.511321117195848, -0.556767987230636), Orientation = new Quaternion(0.186682618578054, -0.817045952667937, -0.304044497706209, 0.452926544336234), Scale = new Vector3D(0.505726283020722, 0.505726283020722, 0.505726283020722), ThrusterType = ThrusterType.Two_Two_Two });
            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-0.422755203622775, -0.961633929728401, -0.580695228607911), Orientation = new Quaternion(-0.160516892633012, -0.663450532470308, 0.686825586338298, -0.249676454827765), Scale = new Vector3D(0.505726283020722, 0.505726283020722, 0.505726283020722), ThrusterType = ThrusterType.Two_Two_Two });
            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(1.17798984223157, -0.367610877704125, -0.419027688531442), Orientation = new Quaternion(0.188774232171627, -0.546763929161965, 0.629041024236897, -0.519346594156906), Scale = new Vector3D(0.505726283020722, 0.505726283020722, 0.505726283020722), ThrusterType = ThrusterType.Two_Two_Two });

            name = "boston";
            retVal.Add(name, ShipDNA.Create(name, parts));

            #endregion
            #region Warped

            parts = new List<ShipPartDNA>();

            parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(-0.415779118372089, -4.44089209850063E-16, 2.65134167497394), Orientation = new Quaternion(0.00149654661772449, 0.299669578213728, -0.000470073231312593, 0.954041761807409), Scale = new Vector3D(13.6702030728099, 13.6702030728099, 0.758867474822861) });
            parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(1.86043981847842, -0.0225518651618495, -1.67056439341923), Orientation = new Quaternion(0.32639687930165, -0.327972279423376, -0.628109595428521, 0.625601788074969), Scale = new Vector3D(3.29508007636767, 3.29508007636767, 4.38477226335578) });
            parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(0.55696401787799, 0.00519891808597983, 0.631346849283141), Orientation = new Quaternion(-8.13989373564962E-14, 0.997651968317974, -1.00771101930825E-13, 0.0684875909291051), Scale = new Vector3D(1.85991620495662, 1.85991620495662, 1.42586750269767) });

            parts.Add(new ShipPartDNA() { PartType = EnergyTank.PARTTYPE, Position = new Point3D(4.16012439197799, 1.77938964266832, 0.0650451496718962), Orientation = new Quaternion(0.328778913970438, -0.32559504977355, -0.626866024295185, 0.626842306226795), Scale = new Vector3D(1.57437311733454, 1.57437311733454, 1.4396038848807) });
            parts.Add(new ShipPartDNA() { PartType = EnergyTank.PARTTYPE, Position = new Point3D(4.15582249249203, -1.78088132270355, 0.0588729125123382), Orientation = new Quaternion(0.328778913970438, -0.32559504977355, -0.626866024295185, 0.626842306226795), Scale = new Vector3D(1.57437311733454, 1.57437311733454, 1.4396038848807) });
            parts.Add(new ShipPartDNA() { PartType = EnergyTank.PARTTYPE, Position = new Point3D(3.14480320767759, -1.11950059104691, -0.834927128722355), Orientation = new Quaternion(0.891060007964131, -0.122482031664977, -0.381614920367472, -0.213026445961787), Scale = new Vector3D(1, 1, 1.9561364479251) });
            parts.Add(new ShipPartDNA() { PartType = EnergyTank.PARTTYPE, Position = new Point3D(3.14745397022101, 1.07428216706874, -0.831123893633974), Orientation = new Quaternion(0.897605527639395, 0.0491512619721194, -0.374145961531993, 0.227822891007583), Scale = new Vector3D(1, 1, 1.9561364479251) });

            parts.Add(new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(3.14725799425251, 1.760755933471, 0.770940447560668), Orientation = Quaternion.Identity, Scale = new Vector3D(1.25714812873173, 1.25714812873173, 1.25714812873173) });
            parts.Add(new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(3.14295609476655, -1.79951503190086, 0.764768210401107), Orientation = Quaternion.Identity, Scale = new Vector3D(1.25714812873173, 1.25714812873173, 1.25714812873173) });

            parts.Add(new ShipPartDNA() { PartType = SensorGravity.PARTTYPE, Position = new Point3D(-0.106368452387744, -0.00549251474389084, 3.102851296814), Orientation = new Quaternion(0.675322637315157, 0.211674643872845, -0.212122422711366, 0.673897068144732), Scale = new Vector3D(1.52019845656624, 1.52019845656624, 1.52019845656624) });
            parts.Add(new ShipPartDNA() { PartType = SensorSpin.PARTTYPE, Position = new Point3D(-0.0437618024319062, 0, -0.336295171364882), Orientation = new Quaternion(0, 0.300153062509542, 0, 0.953891051989766), Scale = new Vector3D(1.23523220708212, 1.23523220708212, 1.23523220708212) });

            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(5.89340081527952, 1.78660304258119, -1.14298786652744), Orientation = new Quaternion(0.329029583723567, -0.325344337176554, -0.626734489015828, 0.626972467960048), Scale = new Vector3D(2.71495967899503, 2.71495967899503, 2.71495967899503), ThrusterType = ThrusterType.One });
            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(5.88909891579355, -1.77366792279068, -1.149160103687), Orientation = new Quaternion(0.329029583723567, -0.325344337176554, -0.626734489015828, 0.626972467960048), Scale = new Vector3D(2.71495967899503, 2.71495967899503, 2.71495967899503), ThrusterType = ThrusterType.One });

            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-0.75112661908622, 0.00861927978558735, 2.16861260525792), Orientation = new Quaternion(0.703491036128367, 0.465641697790831, 0.0784662269657508, 0.53115084730354), Scale = new Vector3D(1.07281757676208, 1.07281757676208, 1.07281757676208), ThrusterType = ThrusterType.Two_Two_One });

            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-0.842320315681215, 0.445850141106038, 2.07560957873666), Orientation = new Quaternion(-1.22821396260792E-13, 0.317055676963852, 4.24122399542322E-14, 0.948406926221542), Scale = new Vector3D(0.6325613611558, 0.6325613611558, 0.6325613611558), ThrusterType = ThrusterType.One });
            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-0.481221186922412, 0.00812762670631822, 1.79759591293543), Orientation = new Quaternion(-1.22821396260792E-13, 0.317055676963852, 4.24122399542322E-14, 0.948406926221542), Scale = new Vector3D(0.6325613611558, 0.6325613611558, 0.6325613611558), ThrusterType = ThrusterType.One });
            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-0.821473199814943, -0.414179343127105, 2.04686572173193), Orientation = new Quaternion(-1.22821396260792E-13, 0.317055676963852, 4.24122399542322E-14, 0.948406926221542), Scale = new Vector3D(0.6325613611558, 0.6325613611558, 0.6325613611558), ThrusterType = ThrusterType.One });
            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-1.19231066794686, -0.0185574665950802, 2.28906066589528), Orientation = new Quaternion(-1.22821396260792E-13, 0.317055676963852, 4.24122399542322E-14, 0.948406926221542), Scale = new Vector3D(0.6325613611558, 0.6325613611558, 0.6325613611558), ThrusterType = ThrusterType.One });


            name = "warped";
            retVal.Add(name, ShipDNA.Create(name, parts));

            #endregion
            #region Soyuz

            parts = new List<ShipPartDNA>();

            parts.Add(new ShipPartDNA() { PartType = EnergyTank.PARTTYPE, Position = new Point3D(-3.5981163671539E-05, 0, 1.46781323761163), Orientation = Quaternion.Identity, Scale = new Vector3D(1.25782536782606, 1.25782536782606, 1) });

            parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(-0.000696780643698047, 0, 0.433067599812263), Orientation = Quaternion.Identity, Scale = new Vector3D(1.14551898143555, 1.14551898143555, 1.05954228295416) });
            parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(-0.000696780643698047, 0, -1.23071113206213), Orientation = Quaternion.Identity, Scale = new Vector3D(1.13367725010974, 1.13367725010974, 2.21137757461636) });
            parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(-0.00132223877485671, -4.44089209850063E-16, 2.71504259698219), Orientation = Quaternion.Identity, Scale = new Vector3D(0.27111218305702, 0.27111218305702, 1) });

            parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(-8.01656896171499E-06, -0.410135135068866, -1.90712591628512), Orientation = new Quaternion(-0.027816666141175, -0.0278287545157711, -0.706405686381807, 0.706712671284688), Scale = new Vector3D(0.428226882175253, 0.428226882175253, 0.585505021356475) });
            parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(0.00211042674311201, 0.410124907281646, -1.90712591628512), Orientation = new Quaternion(0.0277510343499158, -0.0278942034411887, 0.704738963622237, 0.708374750156686), Scale = new Vector3D(0.428226882175253, 0.428226882175253, 0.585505021356475) });
            parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(-0.410129746360129, -0.00210243496901485, -1.90712591628512), Orientation = new Quaternion(-0.0393471020619694, -0.0001097835762651, -0.999221707524377, 0.00278795964086462), Scale = new Vector3D(0.428226882175253, 0.428226882175253, 0.585505021356475) });
            parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(0.410130337178093, 0, -1.90712591628512), Orientation = new Quaternion(0, -0.0393472552169608, 0, 0.999225596903368), Scale = new Vector3D(0.428226882175253, 0.428226882175253, 0.585505021356475) });

            parts.Add(new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(-0.0941853729643634, -0.165902766724583, -0.118008571611), Orientation = Quaternion.Identity, Scale = new Vector3D(0.19126797194689, 0.19126797194689, 0.19126797194689) });
            parts.Add(new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(0.0719365360386521, -0.206572496770261, -0.124326008618846), Orientation = Quaternion.Identity, Scale = new Vector3D(0.19126797194689, 0.19126797194689, 0.19126797194689) });
            parts.Add(new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(-0.175364343854563, -0.00674379861582364, -0.121395184593942), Orientation = Quaternion.Identity, Scale = new Vector3D(0.19126797194689, 0.19126797194689, 0.19126797194689) });
            parts.Add(new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(-0.110202787320596, 0.152137165580384, -0.10787449335919), Orientation = Quaternion.Identity, Scale = new Vector3D(0.19126797194689, 0.19126797194689, 0.19126797194689) });
            parts.Add(new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(0.0702999539928238, 0.189744443725594, -0.112505163543386), Orientation = Quaternion.Identity, Scale = new Vector3D(0.19126797194689, 0.19126797194689, 0.19126797194689) });
            parts.Add(new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(0.173981154380022, 0.0626940266596867, -0.12763812492239), Orientation = Quaternion.Identity, Scale = new Vector3D(0.19126797194689, 0.19126797194689, 0.19126797194689) });
            parts.Add(new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(0.185616687683087, -0.0930973411077959, -0.121153634213154), Orientation = Quaternion.Identity, Scale = new Vector3D(0.19126797194689, 0.19126797194689, 0.19126797194689) });

            parts.Add(new ShipPartDNA() { PartType = SensorGravity.PARTTYPE, Position = new Point3D(0.000737522638707078, 0, 2.09506565283935), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1) });

            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(0.0011923801810507, 0, -2.75061800748678), Orientation = Quaternion.Identity, Scale = new Vector3D(0.796760869039552, 0.796760869039552, 0.796760869039552), ThrusterType = ThrusterType.One });

            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-8.52715551335982E-05, -0.394969335493998, -2.71504259698219), Orientation = new Quaternion(-0.0154348979217083, -0.0154388430316377, -0.706847919907537, 0.707028587947036), Scale = new Vector3D(1, 1, 1), ThrusterType = ThrusterType.One });
            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(0.00203238742178685, 0.394959315996708, -2.71504259698219), Orientation = new Quaternion(0.0153971022659106, -0.0154765367515536, 0.705117051273509, 0.708754788382656), Scale = new Vector3D(1, 1, 1), ThrusterType = ThrusterType.One });
            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-0.394964545106545, -0.00194714164572035, -2.71504259698219), Orientation = new Quaternion(-0.021830952153577, -5.89582677500096E-05, -0.999758028697669, 0.0027000197300844), Scale = new Vector3D(1, 1, 1), ThrusterType = ThrusterType.One });
            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(0.394964545106545, 0, -2.71504259698219), Orientation = new Quaternion(0, -0.021831031766939, 0, 0.999761674626504), Scale = new Vector3D(1, 1, 1), ThrusterType = ThrusterType.One });

            name = "soyuz";
            retVal.Add(name, ShipDNA.Create(name, parts));

            #endregion

            return retVal;
        }

        private Tuple<Bean, MyHitTestResult, Point> GetMouseOverBean(MouseEventArgs e)
        {
            // Fire a ray at the mouse point
            Point clickPoint = e.GetPosition(grdViewPort);

            Visual3D[] ignoreVisuals = UtilityCore.Iterate(_terrain.Visuals, _explosions.Select(o => o.Visual)).Where(o => o != null).ToArray();

            RayHitTestParameters clickRay;
            List<MyHitTestResult> hits = UtilityWPF.CastRay(out clickRay, clickPoint, grdViewPort, _camera, _viewport, true, ignoreVisuals);

            // See if they clicked on a bean
            var bean = GetBeanHit(hits);
            if (bean == null)
            {
                return null;
            }

            return Tuple.Create(bean.Item1, bean.Item2, clickPoint);
        }
        private Tuple<Bean, MyHitTestResult> GetBeanHit(List<MyHitTestResult> hits)
        {
            foreach (var hit in hits)		// hits are sorted by distance, so this method will only return the closest match
            {
                Visual3D visualHit = hit.ModelHit.VisualHit;
                if (visualHit == null)
                {
                    continue;
                }

                Bean bean = _beans.FirstOrDefault(o => o.Visuals3D != null && o.Visuals3D.Any(p => p == visualHit));

                if (bean != null)
                {
                    return Tuple.Create(bean, hit);
                }
            }

            return null;
        }

        private static bool IsEmpty(Array array)
        {
            return array == null || array.Length == 0;
        }

        #endregion
    }
}
