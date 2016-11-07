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
        private const string SAVENAME = "Miner2D";

        private const double STATION_BOUNDRY_PERCENT = .7;

        private Point3D _boundryMin;
        private Point3D _boundryMax;

        private EditorOptions _editorOptions = new EditorOptions();
        private ItemOptions _itemOptions = null;
        private SharedVisuals _sharedVisuals = new SharedVisuals(.25);

        private World _world = null;
        private Map _map = null;

        private RadiationField _radiation;

        private UpdateManager _updateManager = null;
        private MapPopulationManager _mapPopulationManager = null;
        private MapForcesManager _mapForcesManager = null;
        private BackImageManager _backImageManager = null;
        private MinimapHelper _miniMap = null;
        private CameraHelper _cameraHelper = null;
        private SwarmObjectiveStrokes _brushStrokes = null;

        private MaterialManager _materialManager = null;
        private int _material_Ship = -1;
        private int _material_ExplodingShip = -1;		// there is no property on body to turn off collision detection, that's done by its current material
        private int _material_SpaceStation = -1;
        private int _material_Mineral = -1;
        private int _material_Asteroid = -1;
        private int _material_Projectile = -1;
        private int _material_SwarmBot = -1;

        private readonly ITriangle _clickPlane = new Triangle(new Point3D(0, 0, 0), new Point3D(1, 0, 0), new Point3D(0, 1, 0));

        private ShipExtraArgs _shipExtra = null;

        private Miner2DSession _session = null;

        private Player _player = null;
        private SpaceStation2D[] _stations = null;

        private SpaceStation2D _currentlyOverStation = null;
        private EditShipTransfer _editShipTransfer = null;

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
        private Editor _shipEditor = null;      // this gets lazy loaded
        private Border _shipEditorBorder = null;
        private UIElement _shipEditorManagementControl = null;
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

        private DateTime _nextAutoSave = GetNextAutoSaveTime();

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
                _itemOptions.Thruster_StrengthRatio *= 4.5;
                _itemOptions.FuelToThrustRatio *= .03;
                _itemOptions.Projectile_Color = UtilityWPF.ColorFromHex("FFE330");        // using bee/wasp colors, because white looks too much like the stars

                _progressBars = new ShipProgressBarManager(pnlProgressBars);
                _progressBars.Foreground = new SolidColorBrush(UtilityWPF.ColorFromHex("BBB"));

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

                // Swarmbot
                material = new Game.Newt.v2.NewtonDynamics.Material();
                material.Elasticity = .95d;
                _material_SwarmBot = _materialManager.AddMaterial(material);

                // Collisions
                _materialManager.RegisterCollisionEvent(_material_Ship, _material_Mineral, Collision_BotMineral);
                _materialManager.RegisterCollisionEvent(_material_Ship, _material_Asteroid, Collision_ShipAsteroid);
                _materialManager.RegisterCollisionEvent(_material_Ship, _material_Projectile, Collision_ShipProjectile);

                _materialManager.RegisterCollisionEvent(_material_Asteroid, _material_Projectile, Collision_AsteroidProjectile);
                _materialManager.RegisterCollisionEvent(_material_Asteroid, _material_SwarmBot, Collision_AsteroidSwarmBot);
                _materialManager.RegisterCollisionEvent(_material_Asteroid, _material_Asteroid, Collision_AsteroidAsteroid);

                #endregion
                #region Trackball

                //TODO: Only use this when debugging the scene

                //// Trackball
                //_trackball = new TrackBallRoam(_camera);
                //_trackball.KeyPanScale = 15d;
                //_trackball.EventSource = grdViewPort;		//NOTE:  If this control doesn't have a background color set, the trackball won't see events (I think transparent is ok, just not null)
                //_trackball.AllowZoomOnMouseWheel = true;
                //_trackball.Mappings.AddRange(TrackBallMapping.GetPrebuilt(TrackBallMapping.PrebuiltMapping.MouseComplete_NoLeft));
                ////_trackball.GetOrbitRadius += new GetOrbitRadiusHandler(Trackball_GetOrbitRadius);
                //_trackball.ShouldHitTestOnOrbit = true;

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
                #region Radiation

                //TODO: Make radiation sources instead of a constant ambient -- sort of mini stars, or something manmade looking
                _radiation = new RadiationField()
                {
                    AmbientRadiation = 1,
                };

                #endregion
                #region UpdateManager

                //TODO: UpdateManager needs to inspect types as they are added to the map (map's ItemAdded event)
                _updateManager = new UpdateManager(
                    new Type[] { typeof(ShipPlayer), typeof(SpaceStation2D), typeof(Projectile), typeof(Asteroid), typeof(SwarmBot1b) },
                    new Type[] { typeof(ShipPlayer), typeof(SwarmBot1b) },
                    _map);

                #endregion
                #region Brush Strokes

                _brushStrokes = new SwarmObjectiveStrokes(_world.WorldClock, _itemOptions.SwarmBay_BirthSize * 4, 6);

                // This would be for drawing the strokes
                //_brushStrokes.PointsChanged += BrushStrokes_PointsChanged;

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

                #region Ship Extra

                _shipExtra = new ShipExtraArgs()
                {
                    Options = _editorOptions,
                    ItemOptions = _itemOptions,
                    Material_Projectile = _material_Projectile,
                    Material_SwarmBot = _material_SwarmBot,
                    SwarmObjectiveStrokes = _brushStrokes,
                    RunNeural = false,
                    Radiation = _radiation,
                };

                #endregion

                CreateStars3D();      //TODO: Move this to BackImageManager
                                      //CreateStars3DGrid();
                                      //CreateShip(UtilityCore.GetRandomEnum<DefaultShipType>());

                if (!LoadLatestSession())
                {
                    CreateNewSession();
                }

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
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                SaveSession(false, false);
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

                _map.Dispose();     // this will dispose the physics bodies
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

                _brushStrokes.Tick();

                _backImageManager.Update();
                _cameraHelper.Update();
                _miniMap.Update();
                _progressBars.Update();

                Point3D position = _player.Ship.PositionWorld;

                if (_stations != null)
                {
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

                #region Autosave

                if (DateTime.UtcNow > _nextAutoSave)
                {
                    SaveSession(true, true);
                    _nextAutoSave = GetNextAutoSaveTime();
                }

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
                _progressBars.Bot = e.NewShip;

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

        private void Collision_BotMineral(object sender, MaterialCollisionArgs e)
        {
            try
            {
                Body botBody = e.GetBody(_material_Ship);
                Body mineralBody = e.GetBody(_material_Mineral);

                Bot bot = _map.GetItem<Bot>(botBody);
                if (bot == null)
                {
                    return;
                }

                // For now, only the player ship can take minerals
                if (!(bot is ShipPlayer))
                {
                    return;
                }

                Mineral mineral = _map.GetItem<Mineral>(mineralBody);
                if (mineral == null)
                {
                    return;
                }

                ((ShipPlayer)bot).CollidedMineral(mineral, _world, _material_Mineral, _sharedVisuals);
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

                if (asteroidBody == null || projectileBody == null)
                {
                    return;
                }

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
        private void Collision_AsteroidSwarmBot(object sender, MaterialCollisionArgs e)
        {
            try
            {
                Body asteroidBody = e.GetBody(_material_Asteroid);
                Body botBody = e.GetBody(_material_SwarmBot);

                if (asteroidBody == null || botBody == null)
                {
                    return;
                }

                //NOTE: this.Map_ItemRemoved will dispose the projectile once the map removes it, so get these stats now
                double botMass = botBody.Mass;
                Point3D botPos = botBody.Position;
                Vector3D botVelocity = botBody.Velocity;

                // Damage the bot
                SwarmBot1b bot = _map.GetItem<SwarmBot1b>(botBody);
                if (bot != null)
                {
                    bot.TakeDamage(asteroidBody.Velocity);
                }
                else
                {
                    // Already gone
                    return;
                }

                // Damage the asteroid
                Asteroid asteroid = _map.GetItem<Asteroid>(asteroidBody);
                if (asteroid != null)
                {
                    //TODO: When swarmbots can grow and specialize, remove this mass/vel boost
                    asteroid.TakeDamage_Projectile(botMass * 5, botPos, botVelocity * 3);
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

                if(ShouldIgnoreKeyPress())
                {
                    return;
                }

                if (_player.Ship != null)
                {
                    if (e.Key == Key.F1)
                    {
                        #region Save ship to file

                        // Hidden feature: save the current ship so it can be analyzed
                        ShipDNA dna = _player.Ship.GetNewDNA();

                        //C:\Users\<username>\AppData\Roaming\Asteroid Miner\Miner2D\
                        string foldername = UtilityCore.GetOptionsFolder();
                        foldername = System.IO.Path.Combine(foldername, SAVENAME);
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

                    #region manual rotate
                    //else if (e.Key == Key.F5)
                    //{
                    //    ApplyShipRotation(new Vector3D(1, 0, 0));
                    //}
                    //else if (e.Key == Key.F6)
                    //{
                    //    ApplyShipRotation(new Vector3D(-1, 0, 0));
                    //}
                    //else if (e.Key == Key.F7)
                    //{
                    //    ApplyShipRotation(new Vector3D(0, 1, 0));
                    //}
                    //else if (e.Key == Key.F8)
                    //{
                    //    ApplyShipRotation(new Vector3D(0, -1, 0));
                    //}
                    //else if (e.Key == Key.F9)
                    //{
                    //    ApplyShipRotation(new Vector3D(0, 0, 1));
                    //}
                    //else if (e.Key == Key.System && e.SystemKey == Key.F10)     // not sure why F10 is a system key, none of the others are
                    //{
                    //    ApplyShipRotation(new Vector3D(0, 0, -1));
                    //}
                    #endregion

                    else if (e.Key == Key.F12)
                    {
                        DebugMethod_F12();
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

                if (ShouldIgnoreKeyPress())
                {
                    return;
                }

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

        /// <summary>
        /// This method is named from the perspective of the miner window intercepting the keypress.  So if the miner window ignores it, then
        /// the keypress will be handled normally by whatever control has focus
        /// </summary>
        private bool ShouldIgnoreKeyPress()
        {
            return (
                panelContainer.Child != null && panelContainer.Visibility == Visibility.Visible &&      // some panel is visible
                (_shipEditorBorder != null && panelContainer.Child == _shipEditorBorder) ||
                (_spaceDockPanel != null && panelContainer.Child == _spaceDockPanel)
                );
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

                // Fire a ray at the mouse point
                Point clickPoint2D = e.GetPosition(grdViewPort);
                var clickRay = UtilityWPF.RayFromViewportPoint(_camera, _viewport, clickPoint2D);

                // No need for ray casting, just project down to the z=0 plane
                //Point3D clickPoint3D = new Point3D(clickRay.Origin.X, clickRay.Origin.Y, 0);
                Point3D clickPoint3D = Math3D.GetIntersection_Plane_Line(_clickPlane, clickRay.Origin, clickRay.Direction).Value;

                // Store the point
                _brushStrokes.AddPointToStroke(clickPoint3D);
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
                if (e.LeftButton != MouseButtonState.Pressed)
                {
                    return;
                }

                Point clickPoint2D = e.GetPosition(grdViewPort);
                var clickRay = UtilityWPF.RayFromViewportPoint(_camera, _viewport, clickPoint2D);

                //Point3D clickPoint3D = new Point3D(clickRay.Origin.X, clickRay.Origin.Y, 0);
                Point3D clickPoint3D = Math3D.GetIntersection_Plane_Line(_clickPlane, clickRay.Origin, clickRay.Direction).Value;

                _brushStrokes.AddPointToStroke(clickPoint3D);
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
                if (e.ChangedButton != MouseButton.Left)
                {
                    // All the special logic in this method is for the left button
                    return;
                }

                _brushStrokes.StopStroke();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                #region unit tests

                //Func<ISessionOptions> getSession = new Func<ISessionOptions>(() => new Miner2DSession());
                //Func<ISessionOptions> getSession = null;

                //var result1 = SessionSaveLoad.Load(@"C:\Users\charlie.rix\AppData\Roaming\Asteroid Miner", SAVENAME, getSession);
                //var result2 = SessionSaveLoad.Load(@"C:\Users\charlie.rix\AppData\Roaming\Asteroid Miner\Miner2D", SAVENAME, getSession);
                //var result3 = SessionSaveLoad.Load(@"C:\Users\charlie.rix\AppData\Roaming\Asteroid Miner\Miner2D\Session 2016-10-23 21.17.36.641", SAVENAME, getSession);
                //var result4 = SessionSaveLoad.Load(@"C:\Users\charlie.rix\AppData\Roaming\Asteroid Miner\Miner2D\Session 2016-10-23 21.17.36.641\Save 2016-10-23 21.17.36.654", SAVENAME, getSession);
                //var result5 = SessionSaveLoad.Load(@"C:\Users\charlie.rix\AppData\Roaming\Asteroid Miner\Ships", SAVENAME, getSession);

                #endregion

                System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();

                if (_session != null && _session.LatestSessionFolder != null)
                {
                    dialog.SelectedPath = _session.LatestSessionFolder;
                }
                else
                {
                    string baseFolder = UtilityCore.GetOptionsFolder();

                    baseFolder = System.IO.Path.Combine(baseFolder, SAVENAME);
                    if (Directory.Exists(baseFolder))
                    {
                        dialog.SelectedPath = baseFolder;
                    }
                }

                dialog.Description = "Select save folder";
                if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }

                // Save the current session first
                SaveSession(false, false);

                SessionFolderResults results = SessionSaveLoad.Load(dialog.SelectedPath, SAVENAME, () => new Miner2DSession());

                if (!LoadSession(results))
                {
                    MessageBox.Show("Couldn't load the session", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void New_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CreateNewSession();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string help = @"Fly around, shoot asteroids, pick up minerals, buy/sell from space station, modify your ship

Eventually, there will be AI bots to watch/help/kill

Standard Keys:
    ASDW Arrows - fly ship
    Shift - 100% thrust
    Ctrl - fire gun
    Tab - map
    Space - dock with space station

Cheat Keys:
    F1 - save ship to file
    F2 - full stop
    F3 - free money
    F4 - refill containers
    F12 - private debug method";

                MessageBox.Show(help, this.Title, MessageBoxButton.OK, MessageBoxImage.Question);
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
                SaveSession(false, false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void SpaceDockPanel_EditShip(object sender, EventArgs e)
        {
            try
            {
                ShowShipEditor();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void CloseShipEditor_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CloseShipEditor();
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
                _spaceDockPanel = new SpaceDockPanel(_editorOptions, _itemOptions, _map, _material_Ship, _shipExtra);
                _spaceDockPanel.LaunchShip += SpaceDockPanel_LaunchShip;
                _spaceDockPanel.EditShip += SpaceDockPanel_EditShip;
            }

            _spaceDockPanel.ShipDocking(_player, station, _world);

            statusMessage.Content = "";

            panelContainer.Child = _spaceDockPanel;
            panelContainer.Visibility = Visibility.Visible;
        }

        private void ShowShipEditor()
        {
            // Spacedock is currently showing.  Need to keep it alive, but change out panels

            SaveSession(false, false);      // the editor can be unstable, so this is a good spot to save

            if (_shipEditor == null)
            {
                #region instantiate _shipEditor

                StackPanel panel = new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                };

                //TODO: Make Ok, Cancel, Apply instead

                Button button = new Button()
                {
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Padding = new Thickness(12, 4, 12, 4),
                    Content = "Close",
                };
                button.Click += CloseShipEditor_Click;
                panel.Children.Add(button);

                _shipEditorManagementControl = panel;

                _shipEditor = new Editor();

                var resouceDict = new ResourceDictionary()
                {
                    Source = new Uri("/Game.Newt.v2.AsteroidMiner;component/AstMin2D/Stylesheet.xaml", UriKind.RelativeOrAbsolute),
                };

                _shipEditorBorder = new Border()
                {
                    Style = resouceDict["dialogBorder"] as Style,
                    Child = _shipEditor,
                };

                #endregion
            }

            _editShipTransfer = new EditShipTransfer(_player, _spaceDockPanel, _shipEditor, _editorOptions, _world, _material_Ship, _map, _shipExtra);

            _editShipTransfer.EditShip(this.Title, _shipEditorManagementControl);


            //TODO: Uncomment to see contents of view model
            //TabDebugWindow debugWindow = new TabDebugWindow(_shipEditor.TabControl_DEBUG);
            //debugWindow.Owner = this;
            ////debugWindow.Top = this.Top + this.ActualHeight - 200;
            ////debugWindow.Left = this.Left + this.ActualWidth - 200;
            //debugWindow.Show();


            panelContainer.Child = _shipEditorBorder;
        }
        private void CloseShipEditor()
        {
            if (_spaceDockPanel == null)
            {
                // Ship editor can only be shown from space dock.  So this should never be null at this point
                throw new InvalidOperationException("SpaceDock can't be null if ship editor was showing");
            }

            if (!_editShipTransfer.ShipEdited())
            {
                //TODO: Only show for a few seconds
                statusMessage.Content = "Invalid Ship - ignoring changes";
            }

            _editShipTransfer = null;

            // Just swap out the controls (panelContainer is currently showing the editor)
            panelContainer.Child = _spaceDockPanel;
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

        private static DesignPart CreateDesignPart(ShipPartDNA dna, EditorOptions options)
        {
            DesignPart retVal = new DesignPart(options)
            {
                Part2D = null,      // setting 2D to null will tell the editor that the part can't be resized or copied, only moved around
                Part3D = BotConstructor.GetPartDesign(dna, options),
            };

            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = retVal.Part3D.Model;
            retVal.Model = visual;

            return retVal;
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

        private void CreateNewSession()
        {
            DefaultShipType[] shipTypes = new[]
            {
                DefaultShipType.Basic,
                DefaultShipType.Horseshoe,
                DefaultShipType.Pusher,
                DefaultShipType.Star,
            };

            CreateShip(shipTypes[StaticRandom.Next(shipTypes.Length)]);
            //CreateShip(DefaultShipType.Basic);
            //CreateShip_3DFlyer();
            //CreateShip_FromFile();

            _player.Credits = 100;

            CreateSpaceStations();

            _session = null;
        }
        private void SaveSession(bool async, bool pruneAutosavesAfter)
        {
            Miner2DSession session = _session ?? new Miner2DSession();
            _session = session;

            List<Tuple<string, object>> items = new List<Tuple<string, object>>();

            // Player
            items.Add(new Tuple<string, object>("Player", new PlayerDNA()
            {
                Ship = _player.Ship.GetNewDNA(),

                Energy = _player.Ship.Energy != null ? _player.Ship.Energy.QuantityCurrent : 0d,
                Plasma = _player.Ship.Plasma != null ? _player.Ship.Plasma.QuantityCurrent : 0d,
                Fuel = _player.Ship.Fuel != null ? _player.Ship.Fuel.QuantityCurrent : 0d,
                Ammo = _player.Ship.Ammo != null ? _player.Ship.Ammo.QuantityCurrent : 0d,

                Credits = _player.Credits,
            }));

            // CargoBay Contents
            if (_player.Ship != null && _player.Ship.CargoBays != null)
            {
                Cargo[] cargo = _player.Ship.CargoBays.GetCargoSnapshot();
                if (cargo != null && cargo.Length > 0)
                {
                    items.AddRange(cargo.Select((o, i) => new Tuple<string, object>("Cargo_" + i.ToString(), o.GetNewDNA())));
                }
            }

            // Stations
            items.AddRange(_stations.Select((o, i) => new Tuple<string, object>("Station_" + i.ToString(), o.GetNewDNA())));

            SessionSaveLoad.Save(SAVENAME, session, items, false);

            if (pruneAutosavesAfter)
            {
                SessionSaveLoad.PruneAutosaves(session.LatestSessionFolder);
            }
        }
        private bool LoadLatestSession()
        {
            SessionFolderResults results = SessionSaveLoad.Load(SAVENAME, () => new Miner2DSession());
            return LoadSession(results);
        }
        private bool LoadSession(SessionFolderResults results)
        {
            if (results == null || results.SavedFiles == null)
            {
                return false;
            }

            #region group classes

            SpaceStation2DDNA[] stations = results.SavedFiles.
                Select(o => o.Item2 as SpaceStation2DDNA).
                Where(o => o != null).
                ToArray();

            PlayerDNA player = results.SavedFiles.
                Select(o => o.Item2 as PlayerDNA).
                FirstOrDefault(o => o != null);

            CargoDNA[] cargo = results.SavedFiles.
                Select(o => o.Item2 as CargoDNA).
                Where(o => o != null).
                ToArray();

            #endregion

            if (player == null || player.Ship == null || stations == null || stations.Length == 0)
            {
                return false;
            }

            // Session
            _session = results.SessionFile as Miner2DSession;       // could be null

            // Stations
            CreateSpaceStations(stations);

            #region player

            CreateShip(player.Ship);        // this stores it in _player

            if (_player.Ship.Energy != null)
            {
                _player.Ship.Energy.QuantityCurrent = player.Energy;
            }
            if (_player.Ship.Plasma != null)
            {
                _player.Ship.Plasma.QuantityCurrent = player.Plasma;
            }
            if (_player.Ship.Fuel != null)
            {
                _player.Ship.Fuel.QuantityCurrent = player.Fuel;
            }
            if (_player.Ship.Ammo != null)
            {
                _player.Ship.Ammo.QuantityCurrent = player.Ammo;
            }

            if (_player.Ship.CargoBays != null && cargo != null && cargo.Length > 0)
            {
                foreach (CargoDNA cargoDNA in cargo)
                {
                    _player.Ship.CargoBays.Add(cargoDNA.ToCargo());
                }
            }

            _player.Ship.RecalculateMass();

            _player.Credits = player.Credits;

            #endregion

            return true;
        }

        private void CreateStars3D()
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
        private void CreateStars3DGrid()
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

            double xyCoord = _boundryMax.X * STATION_BOUNDRY_PERCENT;
            double zCoord = StaticRandom.NextDouble(ZMIN, ZMAX);
            double minDistanceBetweenStationsSquared = 200d * 200d;

            #region clear the old

            if (_stations != null)
            {
                foreach (var station in _stations)
                {
                    _map.RemoveItem(station);
                }
            }

            _currentlyOverStation = null;
            _stations = null;

            #endregion

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
        private void CreateSpaceStations(SpaceStation2DDNA[] dna)
        {
            #region clear the old

            if (_stations != null)
            {
                foreach (var station in _stations)
                {
                    _map.RemoveItem(station);
                }
            }

            _currentlyOverStation = null;
            _stations = null;

            #endregion

            if (dna == null || dna.Length == 0)
            {
                return;
            }

            #region get normalized positions

            var positionStats = dna.
                Select((o, i) =>
                {
                    Vector vect = o.Position.ToVector2D();

                    return new
                    {
                        Index = i,
                        Pos = o.Position,
                        Pos2D = vect,
                        LenSqr = vect.LengthSquared,
                    };
                }).
                OrderByDescending(o => o.LenSqr).
                ToArray();

            // Take the largest distance, scale to this
            double xyCoord = _boundryMax.X * STATION_BOUNDRY_PERCENT;
            xyCoord *= StaticRandom.NextDouble(.75, 1d);        // don't always want to run the farthest to the edge

            double multiplier = 1d;
            if (!positionStats[0].LenSqr.IsNearZero())
            {
                multiplier = xyCoord / Math.Sqrt(positionStats[0].LenSqr);
            }

            // Overwrite the positions
            foreach (var stat in positionStats)
            {
                dna[stat.Index].Position = (stat.Pos2D * multiplier).ToPoint3D(stat.Pos.Z);
            }

            #endregion

            List<SpaceStation2D> stations = new List<SpaceStation2D>();

            foreach (SpaceStation2DDNA stationDNA in dna)
            {
                Inventory[] inventory = null;
                if (stationDNA.PlayerInventory != null)
                {
                    inventory = stationDNA.PlayerInventory.
                        Select(o => o.ToInventory()).
                        ToArray();
                }

                stations.Add(CreateSpaceStations_Build(stationDNA.Position, stationDNA.Flag, stationDNA.PurchasedVolume, inventory));
            }

            _stations = stations.ToArray();
        }
        private SpaceStation2D CreateSpaceStations_Build(Point3D position, FlagProps flag = null, int? purchasedVolume = null, Inventory[] playerInventory = null)
        {
            Vector3D axis = new Vector3D(0, 0, 1);
            Quaternion rotation = Math3D.GetRotation(axis, Math3D.GetRandomVector_Cone(axis, 30));

            SpaceStation2D retVal = new SpaceStation2D(position, _world, _material_SpaceStation, rotation, flag);

            retVal.SpinDegreesPerSecond = Math1D.GetNearZeroValue(.33, 1.1);

            retVal.RandomizeInventory(true);

            if (purchasedVolume != null && purchasedVolume.Value > retVal.PurchasedVolume)
            {
                retVal.PurchasedVolume = purchasedVolume.Value;
            }

            if (playerInventory != null)
            {
                retVal.PlayerInventory.AddRange(playerInventory);
            }

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
            AsteroidExtra extra = new AsteroidExtra()
            {
                GetMineralsByDestroyedMass = GetMineralsFromDestroyedAsteroid,
                MineralMaterialID = _material_Mineral,
                MinChildRadius = ItemOptionsAstMin2D.MINASTEROIDRADIUS,
            };

            Asteroid asteroid = new Asteroid(radius, GetAsteroidMassByRadius, position, _world, _map, _material_Asteroid, extra);

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
                CreateMinerals_Build(MineralType.Ice, position);
            }

            for (int cntr = 0; cntr < 15; cntr++)
            {
                position = Math3D.GetRandomVector_Circular(minPos, maxPos).ToPoint();
                CreateMinerals_Build(MineralType.Graphite, position);
            }

            for (int cntr = 0; cntr < 10; cntr++)
            {
                position = Math3D.GetRandomVector_Circular(minPos, maxPos).ToPoint();
                CreateMinerals_Build(MineralType.Diamond, position);
            }

            for (int cntr = 0; cntr < 3; cntr++)
            {
                position = Math3D.GetRandomVector_Circular(minPos, maxPos).ToPoint();
                CreateMinerals_Build(MineralType.Emerald, position);
            }
        }
        private void CreateMinerals_Build(MineralType mineralType, Point3D position)
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
                Projectile projectile = new Projectile(StaticRandom.NextDouble(.1, .5), 1000, new Point3D(15, 0, 0) + Math3D.GetRandomVector_Circular(3), _world, _material_Projectile, _itemOptions.Projectile_Color);

                projectile.PhysicsBody.AngularVelocity = Math3D.GetRandomVector_Spherical(6);

                _map.AddItem(projectile);
            }
        }

        private void CreateShip(DefaultShipType type)
        {
            CreateShip(DefaultShips.GetDNA(type));
        }
        private void CreateShip_3DFlyer()
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

            CreateShip(dna);
        }
        private void CreateShip_FromFile()
        {
            string filename = @"C:\Users\charlie.rix\AppData\Roaming\Asteroid Miner\Miner2D\gun fail.xml";

            ShipDNA dna = UtilityCore.DeserializeFromFile<ShipDNA>(filename);

            CreateShip(dna);
        }
        private ShipPlayer CreateShip(ShipDNA dna)
        {
            ShipPlayer ship = ShipPlayer.GetNewShip(dna, _world, _material_Ship, _map, _shipExtra);

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
            //const double MASS_MONEY_RATIO = 1d / 600d;
            const double MASS_MONEY_RATIO = 1d / 100d;

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
                double percentIntoList = rand.NextPow(.1);

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

        /// <summary>
        /// This gets called when they push F12.  Use it to diagnose whatever problem you're having at the time
        /// </summary>
        private void DebugMethod_F12()
        {
            IMapObject[] all = _map.GetAllItems(true).
                ToArray();


            // Created a ghost mineral by bumping into it.  Looks like the ship tried to take it, but failed.  So a disposed mineral is now floating around the map
            // Actually, the ship may have successfully taken it, but the map didn't remove it
            IMapObject[] disposed = all.
                Where(o => o.IsDisposed).
                ToArray();

            Point3D shipPos = _player.Ship.PositionWorld;

            var energy = _player.Ship.Energy;
            var rad = _radiation;


        }

        private static DateTime GetNextAutoSaveTime()
        {
            return DateTime.UtcNow + TimeSpan.FromMinutes(StaticRandom.NextPercent(3, .2));
        }

        #endregion
    }
}
