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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.HelperClassesWPF.Primitives3D;
using Game.Newt.v2.AsteroidMiner.MapParts;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.Controls;
using Game.Newt.v2.GameItems.MapParts;
using Game.Newt.v2.GameItems.ShipEditor;
using Game.Newt.v2.GameItems.ShipParts;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.AsteroidMiner.AstMin2D
{
    /// <summary>
    /// This is the main window of this game
    /// </summary>
    /// <remarks>
    /// TODO: Emulate gravity/vector fields/jet streams
    /// TODO: Add a sun
    /// </remarks>
    public partial class MinerWindow : Window
    {
        #region Class: MineralPrice

        private class MineralPrice
        {
            public MineralPrice(MineralType mineralType, double price, double min, double max)
            {
                this.MineralType = mineralType;
                this.Price = price;
                this.Min = min;
                this.Max = max;
            }

            public readonly MineralType MineralType;
            public readonly double Price;
            public readonly double Min;
            public readonly double Max;
        }

        #endregion

        #region Declaration Section

        /// <summary>
        /// This is the folder that all saves for this game will go
        /// This will be a subfolder of AsteroidMiner
        /// </summary>
        private const string FOLDER = "Miner2D";

        private Point3D _boundryMin;
        private Point3D _boundryMax;

        private EditorOptions _editorOptions = new EditorOptions();
        private ItemOptions _itemOptions = null;
        private SharedVisuals _sharedVisuals = new SharedVisuals(.25);

        private World _world = null;
        private Map _map = null;

        private UpdateManager _updateManager = null;
        private MapPopulationManager _mapPopulationManager = null;
        private MapForcesManager _mapForcesManager = null;
        private BackImageManager _backImageManager = null;
        private MinimapHelper _miniMap = null;
        private CameraHelper _cameraHelper = null;

        private MaterialManager _materialManager = null;
        private int _material_Ship = -1;
        private int _material_ExplodingShip = -1;		// there is no property on body to turn off collision detection, that's done by its current material
        private int _material_SpaceStation = -1;
        private int _material_Mineral = -1;
        private int _material_Asteroid = -1;
        private int _material_Projectile = -1;

        private Player _player = null;
        private SpaceStation2D[] _stations = null;

        private SpaceStation2D _currentlyOverStation = null;

        private ScreenSpaceLines3D _boundryLines = null;
        private Visual3D _stars = null;

        /// <summary>
        /// This listens to the mouse/keyboard and controls the camera
        /// </summary>
        /// <remarks>
        /// I'll only create this when debuging
        /// </remarks>
        private TrackBallRoam _trackball = null;

        private ShipProgressBarManager _progressBars = null;

        private SpaceDockPanel _spaceDockPanel = null;      // this gets lazy loaded
        private InfoScreen _infoScreen = null;      // this gets lazy loaded

        /// <summary>
        /// This holds mineral types, prices from cheapest to most expensive
        /// This is used when deciding what minerals to create when an asteroid explodes
        /// </summary>
        private static Lazy<MineralPrice[]> _pricesByMineralType = new Lazy<MineralPrice[]>(() =>
            ((MineralType[])Enum.GetValues(typeof(MineralType))).
                Select(o => new { MineralType = o, Price = Convert.ToDouble(ItemOptionsAstMin2D.GetCredits_Mineral(o)) * ItemOptionsAstMin2D.MINERAL_AVGVOLUME }).
                Select(o => new MineralPrice(o.MineralType, o.Price, o.Price / 2d, o.Price * 2d)).
                OrderBy(o => o.Price).
                ToArray());

        private bool _initialized = false;

        #endregion

        #region Constructor

        public MinerWindow()
        {
            InitializeComponent();

            //TODO: Use a better font.  I searched for square font and found this (really cool, but it's an exe installer):
            //http://www.ffonts.net/Nova-Square.font
            //this.FontFamily = UtilityWPF.GetFont(new[] { "Century Gothic", "Segoe UI" });

            _initialized = true;
        }

        #endregion

        #region Event Listeners

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _itemOptions = new ItemOptions();
                _itemOptions.ThrusterStrengthRatio *= 1.5;
                _itemOptions.FuelToThrustRatio *= .1;
                _itemOptions.ProjectileColor = UtilityWPF.ColorFromHex("FFE330");        // using bee/wasp colors, because white looks too much like the stars

                _progressBars = new ShipProgressBarManager(pnlProgressBars);
                _progressBars.Foreground = new SolidColorBrush(UtilityWPF.ColorFromHex("BBB"));

                //TODO: Load scene from file

                #region Init World

                // Set the size of the world to something a bit random (gets boring when it's always the same size)
                double halfSize = 325 + StaticRandom.Next(500);
                //halfSize *= 2;
                _boundryMin = new Point3D(-halfSize, -halfSize, -35);
                _boundryMax = new Point3D(halfSize, halfSize, 35);

                _world = new World();
                _world.Updating += new EventHandler<WorldUpdatingArgs>(World_Updating);

                List<Point3D[]> innerLines, outerLines;
                _world.SetCollisionBoundry(out innerLines, out outerLines, _boundryMin, _boundryMax);

                // Draw the lines
                _boundryLines = new ScreenSpaceLines3D(true);
                _boundryLines.Thickness = 1d;
                _boundryLines.Color = WorldColors.BoundryLines;
                _viewport.Children.Add(_boundryLines);

                foreach (Point3D[] line in innerLines)
                {
                    _boundryLines.AddLine(line[0], line[1]);
                }

                #endregion
                #region Materials

                _materialManager = new MaterialManager(_world);

                // Ship
                Game.Newt.v2.NewtonDynamics.Material material = new Game.Newt.v2.NewtonDynamics.Material();
                _material_Ship = _materialManager.AddMaterial(material);

                // Exploding Ship
                material = new Game.Newt.v2.NewtonDynamics.Material();
                material.IsCollidable = false;
                _material_ExplodingShip = _materialManager.AddMaterial(material);

                // Space Station (force field)
                material = new Game.Newt.v2.NewtonDynamics.Material();
                material.IsCollidable = false;
                //material.Elasticity = .99d;       // uncomment these if it should be collidable (it's an ellipse, and briefly shows a force field)
                //material.StaticFriction = .02d;
                //material.KineticFriction = .01d;
                _material_SpaceStation = _materialManager.AddMaterial(material);

                //_materialManager.RegisterCollisionEvent(_material_SpaceStation, _material_Asteroid, Collision_SpaceStation);
                //_materialManager.RegisterCollisionEvent(_material_SpaceStation, _material_Mineral, Collision_SpaceStation);

                // Mineral
                material = new Game.Newt.v2.NewtonDynamics.Material();
                material.Elasticity = .5d;
                material.StaticFriction = .9d;
                material.KineticFriction = .4d;
                _material_Mineral = _materialManager.AddMaterial(material);

                // Asteroid
                material = new Game.Newt.v2.NewtonDynamics.Material();
                material.Elasticity = .25d;
                material.StaticFriction = .9d;
                material.KineticFriction = .75d;
                _material_Asteroid = _materialManager.AddMaterial(material);

                // Projectile
                material = new Game.Newt.v2.NewtonDynamics.Material();
                material.Elasticity = .95d;
                _material_Projectile = _materialManager.AddMaterial(material);

                // Collisions
                _materialManager.RegisterCollisionEvent(_material_Ship, _material_Mineral, Collision_ShipMineral);
                _materialManager.RegisterCollisionEvent(_material_Ship, _material_Asteroid, Collision_ShipAsteroid);
                _materialManager.RegisterCollisionEvent(_material_Ship, _material_Projectile, Collision_ShipProjectile);

                _materialManager.RegisterCollisionEvent(_material_Asteroid, _material_Projectile, Collision_AsteroidProjectile);
                _materialManager.RegisterCollisionEvent(_material_Asteroid, _material_Asteroid, Collision_AsteroidAsteroid);

                #endregion
                #region Trackball

                //TODO: Only use this when debugging the scene

                // Trackball
                _trackball = new TrackBallRoam(_camera);
                _trackball.KeyPanScale = 15d;
                _trackball.EventSource = grdViewPort;		//NOTE:  If this control doesn't have a background color set, the trackball won't see events (I think transparent is ok, just not null)
                _trackball.AllowZoomOnMouseWheel = true;
                _trackball.Mappings.AddRange(TrackBallMapping.GetPrebuilt(TrackBallMapping.PrebuiltMapping.MouseComplete_NoLeft));
                //_trackball.GetOrbitRadius += new GetOrbitRadiusHandler(Trackball_GetOrbitRadius);
                _trackball.ShouldHitTestOnOrbit = true;

                #endregion
                #region Map

                _map = new Map(_viewport, null, _world);
                //_map.SnapshotFequency_Milliseconds = 250;     // just use the map's default values
                //_map.SnapshotMaxItemsPerNode = 13;
                _map.ShouldBuildSnapshots = true;
                _map.ShouldShowSnapshotLines = false;
                _map.ShouldSnapshotCentersDrift = true;

                _map.ItemRemoved += new EventHandler<MapItemArgs>(Map_ItemRemoved);

                #endregion
                #region UpdateManager

                _updateManager = new UpdateManager(
                    new Type[] { typeof(ShipPlayer), typeof(SpaceStation2D), typeof(Projectile) },
                    new Type[] { typeof(ShipPlayer) },
                    _map);

                #endregion
                #region Player

                _player = new Player();
                _player.Credits = 10;

                _player.ShipChanged += new EventHandler<ShipChangedArgs>(Player_ShipChanged);

                #endregion
                #region Minimap

                _miniMap = new MinimapHelper(_map, _viewportMap);

                #endregion
                #region Camera Helper

                _cameraHelper = new CameraHelper(_player, _camera, _cameraMap, _miniMap);

                #endregion
                #region MapPopulationManager

                _mapPopulationManager = new MapPopulationManager(_map, _world, new Point3D(_boundryMin.X, _boundryMin.Y, 0), new Point3D(_boundryMax.X, _boundryMax.Y, 0), _material_Asteroid, _material_Mineral, GetAsteroidMassByRadius, GetMineralsFromDestroyedAsteroid, ItemOptionsAstMin2D.MINASTEROIDRADIUS);
                _updateManager.AddNonMapItem(_mapPopulationManager, TokenGenerator.NextToken());

                #endregion
                #region MapForcesManager

                _mapForcesManager = new MapForcesManager(_map, _boundryMin, _boundryMax);
                _updateManager.AddNonMapItem(_mapForcesManager, TokenGenerator.NextToken());

                #endregion
                #region BackImageManager

                _backImageManager = new BackImageManager(backgroundCanvas, _player);

                #endregion

                CreateStars();      //TODO: Move this to BackImageManager
                //CreateStarsGrid();
                //CreateShip(UtilityCore.GetRandomEnum<DefaultShipType>());
                CreateShip(DefaultShipType.Basic);
                //CreateShip_3DFlyer();
                CreateSpaceStations();
                CreateAsteroids();
                CreateMinerals();
                //CreateProjectile();

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
                _world.Pause();

                _updateManager.Dispose();

                _mapForcesManager.Dispose();

                _map.Dispose();		// this will dispose the physics bodies
                _map = null;

                _world.Dispose();
                _world = null;
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
                _updateManager.Update_MainThread(e.ElapsedTime);

                _backImageManager.Update();
                _cameraHelper.Update();
                _miniMap.Update();
                _progressBars.Update();

                Point3D position = _player.Ship.PositionWorld;

                #region Space Stations

                SpaceStation2D currentlyOverStation = null;

                foreach (SpaceStation2D station in _stations)
                {
                    // See if the ship is over the station
                    Point3D stationPosition = station.PositionWorld;
                    stationPosition.Z = 0;

                    if ((stationPosition - position).LengthSquared < station.Radius * station.Radius)
                    {
                        currentlyOverStation = station;
                    }
                }

                // Store the station that the ship is over
                if (currentlyOverStation == null)
                {
                    statusMessage.Content = "";
                }
                else
                {
                    statusMessage.Content = "Press space to enter station";

                    //TODO:  Play a sound if this is the first time they entered the station's range
                }

                _currentlyOverStation = currentlyOverStation;

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Map_ItemRemoved(object sender, MapItemArgs e)
        {
            if (e.Item is IDisposable)
            {
                ((IDisposable)e.Item).Dispose();
            }
            else if (e.Item.PhysicsBody != null)
            {
                e.Item.PhysicsBody.Dispose();
            }
        }

        private void Player_ShipChanged(object sender, ShipChangedArgs e)
        {
            try
            {
                _progressBars.Ship = e.NewShip;

                if (e.PreviousShip != null)
                {
                    _map.RemoveItem(e.PreviousShip);
                }

                if (e.NewShip != null)
                {
                    _map.AddItem(e.NewShip);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Collision_ShipMineral(object sender, MaterialCollisionArgs e)
        {
            try
            {
                Body shipBody = e.GetBody(_material_Ship);
                Body mineralBody = e.GetBody(_material_Mineral);

                Ship ship = _map.GetItem<Ship>(shipBody);
                if (ship == null)
                {
                    return;
                }

                // For now, only the player ship can take minerals
                if (!(ship is ShipPlayer))
                {
                    return;
                }

                Mineral mineral = _map.GetItem<Mineral>(mineralBody);
                if (mineral == null)
                {
                    return;
                }

                ((ShipPlayer)ship).CollidedMineral(mineral);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Collision_ShipAsteroid(object sender, MaterialCollisionArgs e)
        {
            try
            {
                //TODO: Damage ship (possibly asteroid if the ship is in some kind of ram mode)
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Collision_ShipProjectile(object sender, MaterialCollisionArgs e)
        {
            try
            {
                //TODO: Damage ship, remove projectile
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Collision_AsteroidProjectile(object sender, MaterialCollisionArgs e)
        {
            try
            {
                Body asteroidBody = e.GetBody(_material_Asteroid);
                Body projectileBody = e.GetBody(_material_Projectile);

                //NOTE: this.Map_ItemRemoved will dispose the projectile once the map removes it, so get these stats now
                double projectileMass = projectileBody.Mass;
                Point3D projectilePos = projectileBody.Position;
                Vector3D projectileVelocity = projectileBody.Velocity;

                // Find and remove the projectile
                Projectile projectile = _map.RemoveItem<Projectile>(projectileBody);
                if (projectile == null)
                {
                    // Already gone
                    return;
                }

                // Damage the asteroid
                Asteroid asteroid = _map.GetItem<Asteroid>(asteroidBody);
                if (asteroid != null)
                {
                    asteroid.TakeDamage_Projectile(projectileMass, projectilePos, projectileVelocity);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Collision_AsteroidAsteroid(object sender, MaterialCollisionArgs e)
        {
            try
            {
                //TODO: Damage asteroids
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
                //this.Title = "KeyDown: " + e.Key.ToString();

                if (_player.Ship != null)
                {
                    if (e.Key == Key.F1)
                    {
                        #region Save ship to file

                        // Hidden feature: save the current ship so it can be analyzed
                        ShipDNA dna = _player.Ship.GetNewDNA();

                        //C:\Users\<username>\AppData\Roaming\Asteroid Miner\Miner2D\
                        string foldername = UtilityCore.GetOptionsFolder();
                        foldername = System.IO.Path.Combine(foldername, FOLDER);
                        Directory.CreateDirectory(foldername);

                        string filename = dna.ShipName;
                        if (string.IsNullOrWhiteSpace(filename))
                        {
                            filename = dna.ShipLineage;
                        }

                        if (string.IsNullOrWhiteSpace(filename))
                        {
                            filename = "No Name";
                        }

                        filename = DateTime.Now.ToString("yyyyMMdd HHmmddssfff") + " - " + filename;

                        filename = System.IO.Path.Combine(foldername, filename);

                        UtilityCore.SerializeToFile(filename, dna);

                        #endregion
                    }
                    else if (e.Key == Key.F2)
                    {
                        _player.Ship.PhysicsBody.Velocity = new Vector3D(0, 0, 0);
                        _player.Ship.PhysicsBody.AngularVelocity = new Vector3D(0, 0, 0);
                        _player.Ship.PhysicsBody.Rotation = Quaternion.Identity;
                    }
                    else if (e.Key == Key.F3)
                    {
                        _player.Credits += 10000M;
                    }
                    else if (e.Key == Key.F4)
                    {
                        #region Refill Containers

                        if (_player.Ship.Ammo != null)
                        {
                            _player.Ship.Ammo.QuantityCurrent = _player.Ship.Ammo.QuantityMax;
                        }

                        if (_player.Ship.Energy != null)
                        {
                            _player.Ship.Energy.QuantityCurrent = _player.Ship.Energy.QuantityMax;
                        }

                        if (_player.Ship.Fuel != null)
                        {
                            _player.Ship.Fuel.QuantityCurrent = _player.Ship.Fuel.QuantityMax;
                        }

                        if (_player.Ship.Plasma != null)
                        {
                            _player.Ship.Plasma.QuantityCurrent = _player.Ship.Plasma.QuantityMax;
                        }

                        #endregion
                    }
                    else if (e.Key == Key.F5)
                    {
                        ApplyShipRotation(new Vector3D(1, 0, 0));
                    }
                    else if (e.Key == Key.F6)
                    {
                        ApplyShipRotation(new Vector3D(-1, 0, 0));
                    }
                    else if (e.Key == Key.F7)
                    {
                        ApplyShipRotation(new Vector3D(0, 1, 0));
                    }
                    else if (e.Key == Key.F8)
                    {
                        ApplyShipRotation(new Vector3D(0, -1, 0));
                    }
                    else if (e.Key == Key.F9)
                    {
                        ApplyShipRotation(new Vector3D(0, 0, 1));
                    }
                    else if (e.Key == Key.System && e.SystemKey == Key.F10)
                    {
                        ApplyShipRotation(new Vector3D(0, 0, -1));
                    }
                }

                if (e.Key == Key.Tab)
                {
                    ShowInfoScreen();
                }
                else if (_currentlyOverStation != null && e.Key == Key.Space)
                {
                    ShowSpaceDock(_currentlyOverStation);
                }
                else if (_player.Ship != null)
                {
                    _player.Ship.KeyDown(e.Key);
                }

                e.Handled = true;
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
                //this.Title = "KeyUp: " + e.Key.ToString();

                if (_player.Ship != null)
                {
                    _player.Ship.KeyUp(e.Key);
                }

                e.Handled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnFullMap_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowInfoScreen();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SpaceDockPanel_LaunchShip(object sender, EventArgs e)
        {
            try
            {
                HideDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void InfoScreen_CloseRequested(object sender, EventArgs e)
        {
            try
            {
                HideDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private void ApplyShipRotation(Vector3D axis)
        {
            Quaternion quat = new Quaternion(axis, 20);

            _player.Ship.PhysicsBody.Rotation = _player.Ship.PhysicsBody.Rotation.RotateBy(quat);
        }

        private void ShowSpaceDock(SpaceStation2D station)
        {
            PauseWorld();

            if (_spaceDockPanel == null)
            {
                _spaceDockPanel = new SpaceDockPanel(_editorOptions, _itemOptions, _map, _material_Ship, _material_Projectile);
                _spaceDockPanel.LaunchShip += new EventHandler(SpaceDockPanel_LaunchShip);
            }

            _spaceDockPanel.ShipDocking(_player, station, _world);

            statusMessage.Content = "";

            panelContainer.Child = _spaceDockPanel;
            panelContainer.Visibility = Visibility.Visible;
        }
        private void ShowInfoScreen()
        {
            PauseWorld();

            if (_infoScreen == null)
            {
                _infoScreen = new InfoScreen(_map, _boundryMin, _boundryMax);
                _infoScreen.CloseRequested += new EventHandler(InfoScreen_CloseRequested);
            }

            _infoScreen.UpdateScreen();

            panelContainer.Child = _infoScreen;
            panelContainer.Visibility = Visibility.Visible;
            _infoScreen.Focus();
            Keyboard.Focus(_infoScreen);
        }
        private void HideDialog()
        {
            panelContainer.Visibility = Visibility.Collapsed;
            panelContainer.Child = null;

            this.Focus();

            ResumeWorld();
        }

        private void PauseWorld()
        {
            _world.Pause();
            darkPlate.Visibility = Visibility.Visible;
        }
        private void ResumeWorld()
        {
            darkPlate.Visibility = Visibility.Collapsed;

            _world.UnPause();
        }

        private void CreateStars()
        {
            // Putting the stars in 3D gives a really cool parallax effect.  Not realistic for the small distances the ship travels, but still
            // really cool.  But you need so many stars, and its a real hit on graphics performance (especially the laptop)
            //
            // The stars should be a 2D image, and other debris should be 3D (or maybe just a few 2D sheets sliding at different rates)

            // When the size is 400 (doubled is 800, with margins is 880), the number of stars that looks good is 1000.  So I want to keep that same density
            //const double DENSITY = 500d / (880d * 880d);
            //const double DENSITY = 2500d / (880d * 880d);
            const double DENSITY = 200d / (880d * 880d);

            Vector3D starMin = new Vector3D(_boundryMin.X * 1.1, _boundryMin.Y * 1.1, _boundryMin.Z * 20);
            Vector3D starMax = new Vector3D(_boundryMax.X * 1.1, _boundryMax.Y * 1.1, _boundryMin.Z * 1.5);

            double width = 2 * Math.Abs(starMax.X);
            int numStars = Convert.ToInt32(DENSITY * width * width);

            // Material
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(Brushes.GhostWhite));
            materials.Children.Add(new SpecularMaterial(Brushes.White, 100d));

            Model3DGroup geometries = new Model3DGroup();

            for (int cntr = 0; cntr < numStars; cntr++)
            {
                Vector3D position = Math3D.GetRandomVector(starMin, starMax);
                double scale = UtilityCore.GetScaledValue(1, 1.5, starMax.Z, starMin.Z, position.Z);

                // Geometry Model
                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = materials;
                geometry.BackMaterial = materials;
                geometry.Geometry = _sharedVisuals.StarMesh;

                Transform3DGroup transform = new Transform3DGroup();
                transform.Children.Add(new ScaleTransform3D(scale, scale, scale));      // Making farther away stars slightly larger so that they are more visible
                transform.Children.Add(new TranslateTransform3D(position));

                geometry.Transform = transform;

                geometries.Children.Add(geometry);
            }

            // Visual
            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = geometries;

            _viewport.Children.Add(visual);
            _stars = visual;
        }
        private void CreateStarsGrid()
        {
            // Material
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(Brushes.GhostWhite));
            materials.Children.Add(new SpecularMaterial(Brushes.White, 100d));

            Model3DGroup geometries = new Model3DGroup();

            for (double x = _boundryMin.X * 1.1; x < _boundryMax.X * 1.1; x += 15)
            {
                for (double y = _boundryMin.Y * 1.1; y < _boundryMax.Y * 1.1; y += 15)
                {
                    double z = _boundryMin.Z * 1.5;

                    //for (double z = _boundryMin.Z * 1.5; z > _boundryMin.Z * 10; z -= 30)
                    //{
                    // Geometry Model
                    GeometryModel3D geometry = new GeometryModel3D();
                    geometry.Material = materials;
                    geometry.BackMaterial = materials;
                    geometry.Geometry = _sharedVisuals.StarMesh;
                    geometry.Transform = new TranslateTransform3D(x, y, z);

                    geometries.Children.Add(geometry);
                    //}
                }
            }

            // Visual
            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = geometries;

            _viewport.Children.Add(visual);
            _stars = visual;
        }

        private void CreateSpaceStations()
        {
            const double ZMIN = -20;
            const double ZMAX = -12;

            double xyCoord = _boundryMax.X * .7;
            double zCoord = StaticRandom.NextDouble(ZMIN, ZMAX);
            double minDistanceBetweenStationsSquared = 200d * 200d;

            List<SpaceStation2D> stations = new List<SpaceStation2D>();

            #region Home Station

            // Make one right next to the player at startup
            Point3D homeLocation = Math3D.GetRandomVector_Circular_Shell(12).ToPoint();

            stations.Add(CreateSpaceStations_Build(new Point3D(homeLocation.X, homeLocation.Y, zCoord)));

            #endregion

            // Make a few more
            for (int cntr = 0; cntr < 4; cntr++)
            {
                // Come up with a position that isn't too close to any other station
                Vector3D position = new Vector3D();
                while (true)
                {
                    #region Figure out position

                    position = Math3D.GetRandomVector_Circular(xyCoord);
                    position.Z = StaticRandom.NextDouble(ZMIN, ZMAX);

                    // See if this is too close to an existing station
                    bool isTooClose = false;
                    foreach (SpaceStation station in stations)
                    {
                        Vector3D offset = position - station.PositionWorld.ToVector();
                        if (offset.LengthSquared < minDistanceBetweenStationsSquared)
                        {
                            isTooClose = true;
                            break;
                        }
                    }

                    if (!isTooClose)
                    {
                        break;
                    }

                    #endregion
                }

                stations.Add(CreateSpaceStations_Build(position.ToPoint()));
            }

            _stations = stations.ToArray();
        }
        private SpaceStation2D CreateSpaceStations_Build(Point3D position)
        {
            Vector3D axis = new Vector3D(0, 0, 1);
            Quaternion rotation = Math3D.GetRotation(axis, Math3D.GetRandomVector_Cone(axis, 30));

            SpaceStation2D retVal = new SpaceStation2D(position, _world, _material_SpaceStation, rotation);

            retVal.SpinDegreesPerSecond = Math3D.GetNearZeroValue(.33, 1.1);

            retVal.RandomizeInventory(true);

            //TODO: Comment this out when finished testing
            //retVal.StationInventory.AddRange(UtilityCore.GetEnums<DefaultShipType>().Select(o => new Inventory(DefaultShips.GetDNA(o), 1d)));

            _map.AddItem(retVal);

            return retVal;
        }

        private void CreateAsteroids()
        {
            double minPos = 20;
            double maxPos = _boundryMax.X * .9d;     // this form will start pushing back at .85, so some of them will be inbound  :)
            double asteroidSize;
            Point3D position;

            Random rand = StaticRandom.GetRandomForThread();

            // Small
            //for (int cntr = 0; cntr < 90; cntr++)
            for (int cntr = 0; cntr < 30; cntr++)
            {
                asteroidSize = 1 + (rand.NextDouble() * 4);
                position = Math3D.GetRandomVector_Circular(minPos, maxPos).ToPoint();
                CreateAsteroids_Build(asteroidSize, position);
            }

            // Medium
            //for (int cntr = 0; cntr < 30; cntr++)
            for (int cntr = 0; cntr < 10; cntr++)
            {
                asteroidSize = 4 + (rand.NextDouble() * 6);
                position = Math3D.GetRandomVector_Circular(minPos, maxPos).ToPoint();
                CreateAsteroids_Build(asteroidSize, position);
            }

            // Large
            //for (int cntr = 0; cntr < 6; cntr++)
            for (int cntr = 0; cntr < 2; cntr++)
            {
                asteroidSize = 9 + (rand.NextDouble() * 10);
                position = Math3D.GetRandomVector_Circular(minPos, maxPos).ToPoint();
                CreateAsteroids_Build(asteroidSize, position);
            }
        }
        private void CreateAsteroids_Build(double radius, Point3D position)
        {
            Asteroid asteroid = new Asteroid(radius, GetAsteroidMassByRadius, position, _world, _map, _material_Asteroid, GetMineralsFromDestroyedAsteroid, _material_Mineral, ItemOptionsAstMin2D.MINASTEROIDRADIUS);

            asteroid.PhysicsBody.AngularVelocity = Math3D.GetRandomVector_Spherical(1d);
            asteroid.PhysicsBody.Velocity = Math3D.GetRandomVector_Circular(6d);
            //asteroid.PhysicsBody.Velocity = asteroid.PhysicsBody.Position.ToVector().ToUnit() * -10d;		// makes all the asteroids come toward the origin (this is just here for testing)
            //asteroid.PhysicsBody.Velocity = asteroid.PhysicsBody.Position.ToVector().ToUnit().GetRotatedVector(new Vector3D(0, 0, 1), -90) * (StaticRandom.NextDouble() * 10d);


            //asteroid.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(Asteroid_ApplyForceAndTorque);


            _map.AddItem(asteroid);

            // This works, but the asteroids end up being added after the space stations, and the transparency makes them invisible
            #region Multithread

            //// Come up with the hull on a separate thread (this should speed up the perceived load time a bit)
            //var task = Task.Factory.StartNew(() =>
            //{
            //    return Asteroid.GetHullTriangles(radius);
            //});

            //// Run this on the UI thread once the hull is built
            ////NOTE: This is not the same as a wait.  Execution will go out of this method, and this code will fire like an event later
            //task.ContinueWith(resultTask =>
            //    {
            //        double mass = 10d + (radius * 100d);		// this isn't as realistic as 4/3 pi r^3, but is more fun and stable

            //        Asteroid asteroid = new Asteroid(radius, mass, position, _world, _material_Asteroid, task.Result);

            //        asteroid.PhysicsBody.AngularVelocity = Math3D.GetRandomVectorSpherical(1d);
            //        asteroid.PhysicsBody.Velocity = Math3D.GetRandomVectorSpherical(6d);
            //        //asteroid.PhysicsBody.Velocity = asteroid.PhysicsBody.Position.ToVector().ToUnit() * -10d;		// makes all the asteroids come toward the origin (this is just here for testing)

            //        asteroid.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(Body_ApplyForceAndTorque);

            //        _map.AddItem(asteroid);
            //    }, TaskScheduler.FromCurrentSynchronizationContext());

            #endregion
        }

        private void CreateMinerals()
        {
            double minPos = 20;
            double maxPos = _boundryMax.X * .9d;     // this form will start pushing back at .85, so some of them will be inbound  :)
            Point3D position;

            for (int cntr = 0; cntr < 20; cntr++)
            {
                position = Math3D.GetRandomVector_Circular(minPos, maxPos).ToPoint();
                CreateMineralsSprtBuild(MineralType.Ice, position);
            }

            for (int cntr = 0; cntr < 15; cntr++)
            {
                position = Math3D.GetRandomVector_Circular(minPos, maxPos).ToPoint();
                CreateMineralsSprtBuild(MineralType.Graphite, position);
            }

            for (int cntr = 0; cntr < 10; cntr++)
            {
                position = Math3D.GetRandomVector_Circular(minPos, maxPos).ToPoint();
                CreateMineralsSprtBuild(MineralType.Diamond, position);
            }

            for (int cntr = 0; cntr < 3; cntr++)
            {
                position = Math3D.GetRandomVector_Circular(minPos, maxPos).ToPoint();
                CreateMineralsSprtBuild(MineralType.Emerald, position);
            }
        }
        private void CreateMineralsSprtBuild(MineralType mineralType, Point3D position)
        {
            double volume = StaticRandom.NextPercent(ItemOptionsAstMin2D.MINERAL_AVGVOLUME, 2);
            decimal credits = ItemOptionsAstMin2D.GetCredits_Mineral(mineralType, volume);
            double scale = volume / ItemOptionsAstMin2D.MINERAL_AVGVOLUME;

            Mineral mineral = new Mineral(mineralType, position, volume, _world, _material_Mineral, _sharedVisuals, ItemOptionsAstMin2D.MINERAL_DENSITYMULT, scale, credits);

            mineral.PhysicsBody.AngularVelocity = Math3D.GetRandomVector_Spherical(1d);
            mineral.PhysicsBody.Velocity = Math3D.GetRandomVector_Circular(6d);

            _map.AddItem(mineral);
        }

        private void CreateProjectile()
        {
            for (int cntr = 0; cntr < 30; cntr++)
            {
                Projectile projectile = new Projectile(StaticRandom.NextDouble(.1, .5), 1000, new Point3D(15, 0, 0) + Math3D.GetRandomVector_Circular(3), _world, _material_Projectile, _itemOptions.ProjectileColor);

                projectile.PhysicsBody.AngularVelocity = Math3D.GetRandomVector_Spherical(6);

                _map.AddItem(projectile);
            }
        }

        private async void CreateShip_3DFlyer()
        {
            List<ShipPartDNA> parts = new List<ShipPartDNA>();

            parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-1, 0, 0), Scale = new Vector3D(1, 1, 1) });
            parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(1, 0, 0), Scale = new Vector3D(1, 1, 1) });
            parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0, 0, -1), Scale = new Vector3D(1, 1, 1) });
            parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0, 0, 1), Scale = new Vector3D(1, 1, 1) });

            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-1, 0, -1), Scale = new Vector3D(0.386525690798199, 0.386525690798199, 0.386525690798199), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two_Two_Two), ThrusterType = ThrusterType.Two_Two_Two });
            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(1, 0, -1), Scale = new Vector3D(0.386525690798199, 0.386525690798199, 0.386525690798199), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two_Two_Two), ThrusterType = ThrusterType.Two_Two_Two });
            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(1, 0, 1), Scale = new Vector3D(0.386525690798199, 0.386525690798199, 0.386525690798199), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two_Two_Two), ThrusterType = ThrusterType.Two_Two_Two });
            parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-1, 0, 1), Scale = new Vector3D(0.386525690798199, 0.386525690798199, 0.386525690798199), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two_Two_Two), ThrusterType = ThrusterType.Two_Two_Two });

            ShipDNA dna = ShipDNA.Create(parts);
            dna.ShipName = "3D Flyer";
            dna.ShipLineage = Guid.NewGuid().ToString();

            await CreateShip(dna);
        }
        private async void CreateShip(DefaultShipType type)
        {
            await CreateShip(DefaultShips.GetDNA(type));
        }
        private async Task<ShipPlayer> CreateShip(ShipDNA dna, bool shouldRotate = true)
        {
            ShipPlayer ship = await ShipPlayer.GetNewShipAsync(_editorOptions, _itemOptions, dna, _world, _material_Ship, _material_Projectile, null, null, null, _map);

            //ship.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(Bot_ApplyForceAndTorque);

            if (ship.Energy != null)
            {
                ship.Energy.QuantityCurrent = ship.Energy.QuantityMax;
            }
            if (ship.Plasma != null)
            {
                ship.Plasma.QuantityCurrent = ship.Plasma.QuantityMax;
            }
            if (ship.Fuel != null)
            {
                ship.Fuel.QuantityCurrent = ship.Fuel.QuantityMax;
            }
            if (ship.Ammo != null)
            {
                ship.Ammo.QuantityCurrent = ship.Ammo.QuantityMax;
            }
            ship.RecalculateMass();

            _player.Ship = ship;        // the ship changed event listener will add the ship to the map

            return ship;
        }

        private static double GetAsteroidMassByRadius(double radius, ITriangleIndexed[] triangles)
        {
            // this isn't as realistic as 4/3 pi r^3, but is more fun and stable
            return 4000d * Math.Pow(radius, 1.5d);
        }
        /// <summary>
        /// This is a delagate that gets called whenever an asteroid blows up
        /// </summary>
        private static MineralDNA[] GetMineralsFromDestroyedAsteroid(double destroyedMass)
        {
            //const double MASS_MONEY_RATIO = 1d / 750d;
            const double MASS_MONEY_RATIO = 1d / 600d;

            Random rand = StaticRandom.GetRandomForThread();

            List<MineralDNA> retVal = new List<MineralDNA>();

            // Convert mass into money
            double credits = destroyedMass * MASS_MONEY_RATIO;
            credits *= rand.NextPercent(1, 1.5);

            double remaining = credits;

            #region Build minerals

            while (true)
            {
                // Figure out which materials can be created
                var candidates = _pricesByMineralType.Value.
                    Where(o => o.Price * .85 < remaining).
                    ToArray();

                if (candidates.Length == 0)
                {
                    candidates = _pricesByMineralType.Value.
                        Where(o => o.Min < remaining).
                        ToArray();

                    if (candidates.Length == 0)
                    {
                        break;
                    }
                }

                // Pick a material - give a strong preference to the more valuable ones
                double percentIntoList = rand.NextPow(.1, isPlusMinus: false);

                int index = UtilityCore.GetIndexIntoList(percentIntoList, candidates.Length);

                // Figure out how much money this mineral should be
                double minRange = candidates[index].Min;
                double maxRange = candidates[index].Max;
                if (maxRange > remaining) maxRange = remaining;

                double currentCredits = rand.NextDouble(minRange, maxRange);

                remaining -= currentCredits;

                // Create it
                retVal.Add(ItemOptionsAstMin2D.GetMineral(candidates[index].MineralType, Convert.ToDecimal(currentCredits)));

                if (remaining < credits * .33)
                {
                    // Don't want to make a bunch of tiny minerals
                    break;
                }
            }

            #endregion
            #region Distribute remainder money

            // Sort it so the smallest mineral will take from the remainder first.  Since the amount taken from the remainder is just rand(0 to remainder), the first
            // one will have the highest chance to take the most.
            retVal = retVal.OrderBy(o => o.Volume).ToList();

            for (int cntr = 0; cntr < retVal.Count; cntr++)
            {
                // Credits
                double currentCredits = cntr == retVal.Count - 1 ? remaining : rand.NextDouble(remaining);      // take some portion of remaining
                remaining -= currentCredits;
                currentCredits += Convert.ToDouble(retVal[cntr].Credits);       // add the mineral's current credit value

                // Recreate it
                retVal[cntr] = ItemOptionsAstMin2D.GetMineral(retVal[cntr].MineralType, Convert.ToDecimal(currentCredits));
            }

            #endregion

            return retVal.ToArray();
        }

        #endregion
    }
}
