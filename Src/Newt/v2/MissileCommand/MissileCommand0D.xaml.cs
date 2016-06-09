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
using System.Windows.Shapes;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.Controls;
using Game.Newt.v2.GameItems.MapParts;
using Game.Newt.v2.GameItems.ShipEditor;
using Game.Newt.v2.GameItems.ShipParts;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.MissileCommand
{
    public partial class MissileCommand0D : Window
    {
        #region Declaration Section

        private const double BOUNDRYSIZE = 50;
        private const double BOUNDRYSIZEHALF = BOUNDRYSIZE / 2d;

        private const string FOLDER = "MissileCommand0D";

        private Point3D _boundryMin;
        private Point3D _boundryMax;

        private EditorOptions _editorOptions = new EditorOptions();
        private ItemOptions _itemOptions = null;
        private SharedVisuals _sharedVisuals = new SharedVisuals();

        private World _world = null;
        private Map _map = null;

        private UpdateManager _updateManager = null;

        private CameraPool _cameraPool = null;

        private MaterialManager _materialManager = null;
        private int _material_Wall = -1;
        private int _material_Bot = -1;
        private int _material_ExplodingItem = -1;		// there is no property on body to turn off collision detection, that's done by its current material
        private int _material_Item = -1;
        private int _material_Projectile = -1;

        private DefenseBot _bot = null;
        private ShipViewerWindow _viewer = null;
        private BrainRGBRecognizerViewer _brainViewer = null;
        private ShipProgressBarManager _progressBars = null;

        #endregion

        #region Constructor

        public MissileCommand0D()
        {
            InitializeComponent();
        }

        #endregion

        #region Event Listeners

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _itemOptions = new ItemOptions();
                _itemOptions.MatterToEnergyConversionRate *= .12;
                _itemOptions.MatterToEnergyAmountToDraw *= 1.25;        //NOTE: In bot, the matter converter group doesn't check if empty often enough.  So if you draw much more, then there will be noticable pulses of not refilling (I assume that's the cause anyway)

                #region Init World

                _boundryMin = new Point3D(-BOUNDRYSIZEHALF, -BOUNDRYSIZEHALF, -BOUNDRYSIZEHALF);
                _boundryMax = new Point3D(BOUNDRYSIZEHALF, BOUNDRYSIZEHALF, BOUNDRYSIZEHALF);

                _world = new World();
                _world.Updating += new EventHandler<WorldUpdatingArgs>(World_Updating);

                List<Point3D[]> innerLines, outerLines;
                _world.SetCollisionBoundry(out innerLines, out outerLines, _boundryMin, _boundryMax);

                //NOTE: No need to draw a boundry

                #endregion
                #region Materials

                _materialManager = new MaterialManager(_world);

                // Wall
                Game.Newt.v2.NewtonDynamics.Material material = new Game.Newt.v2.NewtonDynamics.Material();
                material.Elasticity = .1d;
                _material_Wall = _materialManager.AddMaterial(material);

                // Bot
                material = new Game.Newt.v2.NewtonDynamics.Material();
                material.Elasticity = .1d;
                _material_Bot = _materialManager.AddMaterial(material);

                // Item
                material = new Game.Newt.v2.NewtonDynamics.Material();
                material.Elasticity = .1d;
                _material_Item = _materialManager.AddMaterial(material);

                // Exploding Item
                material = new Game.Newt.v2.NewtonDynamics.Material();
                material.IsCollidable = false;
                _material_ExplodingItem = _materialManager.AddMaterial(material);

                // Projectile
                material = new Game.Newt.v2.NewtonDynamics.Material();
                _material_Projectile = _materialManager.AddMaterial(material);

                // Collisions
                //_materialManager.RegisterCollisionEvent(_material_Bot, _material_Bot, Collision_BotBot);
                //_materialManager.RegisterCollisionEvent(_material_Bot, _material_Item, Collision_BotItem);
                //TODO: projectile collisions

                #endregion
                #region Camera Pool

                //TODO: Make the number of threads more configurable, look at how many processors there are
                _cameraPool = new CameraPool(1, Colors.Black);

                #endregion
                #region Map

                _map = new Map(_viewport, _cameraPool, _world);
                _map.SnapshotFequency_Milliseconds = 250;// 125;
                _map.SnapshotMaxItemsPerNode = 10;
                _map.ShouldBuildSnapshots = true;
                _map.ShouldShowSnapshotLines = false;
                _map.ShouldSnapshotCentersDrift = true;

                _updateManager = new UpdateManager(
                    new Type[] { typeof(DefenseBot) },
                    new Type[] { typeof(DefenseBot) },
                    _map);

                #endregion

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

                if (_viewer != null)
                {
                    _viewer.Close();
                    _viewer = null;
                }

                _updateManager.Dispose();

                // Explosions
                //foreach (ExplodingBot explosion in _explosions.ToArray())
                //{
                //    DisposeExplosion(explosion);
                //}

                _map.Dispose();		// this will dispose the physics bodies
                _map = null;

                if (_cameraPool != null)
                {
                    _cameraPool.Dispose();
                    _cameraPool = null;
                }

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

                if (_progressBars != null)
                {
                    _progressBars.Update();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetBot_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                #region DNA

                List<ShipPartDNA> parts = new List<ShipPartDNA>();

                parts.Add(new ShipPartDNA() { PartType = EnergyTank.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0.44329742616787, -2.22044604925031E-16, -0.750606111984116), Scale = new Vector3D(.65, .65, .65) });

                parts.Add(new ShipPartDNA() { PartType = AmmoBox.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-0.44329742616787, -2.22044604925031E-16, -0.839035218662306), Scale = new Vector3D(1, 1, 1) });

                parts.Add(new ShipPartDNA() { PartType = ProjectileGun.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-0.139350859269598, -2.22044604925031E-16, 0.0736057604093046), Scale = new Vector3D(1, 1, 1) });

                parts.Add(new ShipPartDNA() { PartType = CameraColorRGB.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0.443930484830668, -2.22044604925031E-16, -0.0296267373083678), Scale = new Vector3D(1, 1, 1) });

                parts.Add(new ShipPartDNA() { PartType = Brain.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(1.17108888679796, 2.22044604925031E-16, -0.787190712968899), Scale = new Vector3D(1, 1, 1) });

                parts.Add(new BrainRGBRecognizerDNA() { PartType = BrainRGBRecognizer.PARTTYPE, Orientation = new Quaternion(3.91881768803066E-16, -0.264772715428398, 1.07599743798686E-16, 0.964310846752577), Position = new Point3D(0.977003177386146, 0, -0.204027736848604), Scale = new Vector3D(1, 1, 1), Extra = GetBrainRGBRecognizerExtra() });

                parts.Add(new ShipPartDNA() { PartType = CargoBay.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0.812435730367477, 0, -1.83945537515666), Scale = new Vector3D(1, 1, 1) });

                //parts.Add(new ShipPartDNA() { PartType = ConverterMatterToAmmo.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-0.724059461163413, 0, -1.57962039018549), Scale = new Vector3D(1, 1, 1) });
                parts.Add(new ShipPartDNA() { PartType = ConverterMatterToEnergy.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-0.127736487867665, 0, -1.58633876430099), Scale = new Vector3D(1, 1, 1) });

                parts.Add(new ShipPartDNA() { PartType = PlasmaTank.PARTTYPE, Orientation = new Quaternion(0, -0.706641960456376, 0, 0.707571296564784), Position = new Point3D(-0.406755692018177, 0, -2.20579025132833), Scale = new Vector3D(1, 1, 1) });

                ShipDNA dna = ShipDNA.Create(parts);
                dna.ShipLineage = Guid.NewGuid().ToString();

                #endregion

                ShipCoreArgs core = new ShipCoreArgs()
                {
                    Map = _map,
                    Material_Ship = _material_Bot,
                    World = _world,
                };

                ShipExtraArgs extra = new ShipExtraArgs()
                {
                    Options = _editorOptions,
                    ItemOptions = _itemOptions,
                    Material_Projectile = _material_Projectile,
                    CameraPool = _cameraPool,
                    IsPhysicsStatic = true,
                };

                BotConstruction_Result seed = BotConstructor.ConstructBot(dna, core, extra);
                DefenseBot bot = new DefenseBot(seed);
                bot.PhysicsBody.Position = new Point3D(0, 0, -12);

                bot.ShouldLearnFromLifeEvents = false;

                if (bot.Energy != null)
                {
                    bot.Energy.QuantityCurrent = bot.Energy.QuantityMax;
                }
                if (bot.Ammo != null)
                {
                    bot.Ammo.QuantityCurrent = bot.Ammo.QuantityMax;
                }
                if (bot.Plasma != null)
                {
                    bot.Plasma.QuantityCurrent = bot.Plasma.QuantityMax;
                }

                bot.ShouldLearnFromLifeEvents = true;

                SetBot(bot);

                #region save to file

                //dna = bot.GetNewDNA();

                ////C:\Users\<username>\AppData\Roaming\Asteroid Miner\MissileCommand0D\
                //string foldername = UtilityCore.GetOptionsFolder();
                //foldername = System.IO.Path.Combine(foldername, FOLDER);
                //System.IO.Directory.CreateDirectory(foldername);

                //string filename = DateTime.Now.ToString("yyyyMMdd HHmmssfff") + " - bot";
                //filename = System.IO.Path.Combine(foldername, filename);

                //UtilityCore.SerializeToFile(filename, dna);

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ClearBot_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RemoveBot();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void BotViewer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_bot == null)
                {
                    MessageBox.Show("Create a bot first", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_viewer != null)
                {
                    _viewer.Close();
                    _viewer = null;
                }

                _viewer = new ShipViewerWindow(_bot)
                {
                    Owner = this,		// the other settings like topmost, showintaskbar, etc are already set in the window's xaml

                    PopupBorder = new SolidColorBrush(UtilityWPF.ColorFromHex("60000000")),
                    PopupBackground = new SolidColorBrush(UtilityWPF.ColorFromHex("30000000")),
                    ViewportBorder = new SolidColorBrush(UtilityWPF.ColorFromHex("E0000000")),
                    ViewportBackground = new SolidColorBrush(UtilityWPF.ColorFromHex("E0E0E0E0")),
                    PanelBorder = new SolidColorBrush(UtilityWPF.ColorFromHex("8068736B")),
                    PanelBackground = new SolidColorBrush(UtilityWPF.ColorFromHex("80424F45")),
                    Foreground = new SolidColorBrush(UtilityWPF.ColorFromHex("F0F0F0")),
                };

                _viewer.Show();      // it needs to be shown first to get the size
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowAsteroid_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RemoveItem_Click(this, new RoutedEventArgs());

                Point3D position = new Point3D(0, 0, 0);

                Asteroid asteroid = new Asteroid(5, (r, t) => r * 600, position, _world, _map, _material_Item);

                asteroid.PhysicsBody.AngularVelocity = Math3D.GetRandomVector_Spherical(2d);

                _map.AddItem(asteroid);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ShowEmerald_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RemoveItem_Click(this, new RoutedEventArgs());

                Point3D position = new Point3D(0, 0, 0);

                Mineral mineral = new Mineral(MineralType.Emerald, position, 1d, _world, _material_Item, _sharedVisuals, scale: 6d);

                mineral.PhysicsBody.AngularVelocity = Math3D.GetRandomVector_Spherical(2d);
                //mineral.PhysicsBody.Velocity = Math3D.GetRandomVector_Spherical(4d);

                _map.AddItem(mineral);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ApplyItem_Click(object sender, RoutedEventArgs e)
        {
            const double PERCENT = .2;

            try
            {
                if (_bot != null)
                {
                    // Asteroids cause damage (currently using plasma tank as a hitpoint meter)
                    foreach (Asteroid asteroid in _map.GetItems<Asteroid>(false))
                    {
                        if (_bot.Plasma != null)
                        {
                            _bot.Plasma.RemoveQuantity(_bot.Plasma.QuantityMax * PERCENT, false);
                        }
                    }

                    // Minerals get added to the cargobay.  The bot has converters that turn minerals into energy and ammo
                    foreach (Mineral mineral in _map.GetItems<Mineral>(false))
                    {
                        if (_bot.CargoBays != null)
                        {
                            var volume = _bot.CargoBays.CargoVolume;
                            double addVol = Math.Min(volume.Item2 * PERCENT, volume.Item2 - volume.Item1);

                            if (addVol > 0 && !addVol.IsNearZero())
                            {
                                Cargo cargo = new Cargo_Mineral(mineral.MineralType, mineral.Density, addVol);
                                _bot.CargoBays.Add(cargo);
                            }
                        }
                    }
                }

                RemoveItem_Click(this, new RoutedEventArgs());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (Asteroid asteroid in _map.GetItems<Asteroid>(false))
                {
                    _map.RemoveItem(asteroid, true);
                    asteroid.PhysicsBody.Dispose();
                }

                foreach (Mineral mineral in _map.GetItems<Mineral>(false))
                {
                    _map.RemoveItem(mineral, true);
                    mineral.PhysicsBody.Dispose();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void SaveImages_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_bot == null)
                {
                    MessageBox.Show("Create a bot first", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                BrainRGBRecognizer brainRecog = _bot.Parts.FirstOrDefault(o => o is BrainRGBRecognizer) as BrainRGBRecognizer;
                if (brainRecog == null)
                {
                    MessageBox.Show("ERROR: Bot should have a recognizer", this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                //C:\Users\<username>\AppData\Roaming\Asteroid Miner\MissileCommand0D\
                string foldername = UtilityCore.GetOptionsFolder();
                foldername = System.IO.Path.Combine(foldername, FOLDER);
                foldername = System.IO.Path.Combine(foldername, "recog dump " + DateTime.Now.ToString("yyyyMMdd HHmmss"));

                brainRecog.SaveImages(foldername);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ShipBot1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearBot_Click(this, e);

                #region DNA

                List<ShipPartDNA> parts = new List<ShipPartDNA>();

                parts.Add(new ShipPartDNA() { PartType = EnergyTank.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0.44329742616787, -2.22044604925031E-16, -0.750606111984116), Scale = new Vector3D(1, 1, 1) });

                parts.Add(new ShipPartDNA() { PartType = AmmoBox.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-0.44329742616787, -2.22044604925031E-16, -0.839035218662306), Scale = new Vector3D(1, 1, 1) });

                parts.Add(new ShipPartDNA() { PartType = ProjectileGun.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-0.139350859269598, -2.22044604925031E-16, 0.0736057604093046), Scale = new Vector3D(1, 1, 1) });

                parts.Add(new ShipPartDNA() { PartType = CameraColorRGB.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0.443930484830668, -2.22044604925031E-16, -0.0296267373083678), Scale = new Vector3D(1, 1, 1) });

                parts.Add(new ShipPartDNA() { PartType = Brain.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(1.17108888679796, 2.22044604925031E-16, -0.787190712968899), Scale = new Vector3D(1, 1, 1) });

                parts.Add(new BrainRGBRecognizerDNA() { PartType = BrainRGBRecognizer.PARTTYPE, Orientation = new Quaternion(3.91881768803066E-16, -0.264772715428398, 1.07599743798686E-16, 0.964310846752577), Position = new Point3D(0.977003177386146, 0, -0.204027736848604), Scale = new Vector3D(1, 1, 1) });

                ShipDNA dna = ShipDNA.Create(parts);
                dna.ShipLineage = Guid.NewGuid().ToString();

                #endregion

                ShipCoreArgs core = new ShipCoreArgs()
                {
                    World = _world,
                    Material_Ship = _material_Bot,
                    Map = _map,
                };

                ShipExtraArgs extra = new ShipExtraArgs()
                {
                    Options = _editorOptions,
                    ItemOptions = _itemOptions,
                    Material_Projectile = _material_Projectile,
                    CameraPool = _cameraPool,
                    IsPhysicsStatic = true,
                };

                //DefenseBot bot = DefenseBot.GetNewDefenseBotAsync(dna, _world, _material_Bot, _map, extra).Result;        // can't directly call result.  it deadlocks this main thread
                //DefenseBot bot = await DefenseBot.GetNewDefenseBotAsync(dna, _world, _material_Bot, _map, extra);
                //bot.PhysicsBody.Position = new Point3D(0, 0, -12);

                //_map.AddItem(bot);
                //_bot = bot;




                BotConstruction_Result result = BotConstructor.ConstructBot(parts, core, extra);
                Bot botNew = new Bot(result);

                botNew.Dispose();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ConstructionTimings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                #region Boston

                List<ShipPartDNA> parts = new List<ShipPartDNA>();

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

                #endregion

                ShipDNA dna = ShipDNA.Create(parts);

                ShipCoreArgs core = new ShipCoreArgs()
                {
                    World = _world,
                    Material_Ship = _material_Bot,
                    Map = _map,
                };

                //var result = BotConstructor.ConstructBot_Profile(dna, core);
                //Bot botNew = new Bot(result.Item1);

                //botNew.Dispose();

                //MessageBox.Show(result.Item2, this.Title, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private void RemoveBot()
        {
            // Viewer
            if (_viewer != null)
            {
                _viewer.Close();
                _viewer = null;
            }

            // Brain Viewer
            panelCustom.Content = null;
            _brainViewer = null;

            //Progress Bars
            _progressBars = null;
            pnlProgressBars.Visibility = Visibility.Collapsed;

            // Bot
            if (_bot != null)
            {
                _map.RemoveItem(_bot);

                _bot.Dispose();
                _bot = null;
            }
        }
        private void SetBot(DefenseBot bot)
        {
            // Remove
            RemoveBot();

            // Set bot
            _map.AddItem(bot);
            _bot = bot;

            // Brain Viewer
            BrainRGBRecognizer brainRecog = _bot.Parts.FirstOrDefault(o => o is BrainRGBRecognizer) as BrainRGBRecognizer;
            if (brainRecog != null)
            {
                _brainViewer = new BrainRGBRecognizerViewer()
                {
                    Recognizer = brainRecog,
                };
                panelCustom.Content = _brainViewer;
            }

            // Progress bars
            _progressBars = new ShipProgressBarManager(pnlProgressBars);
            _progressBars.Bot = _bot;
            _progressBars.Foreground = this.Foreground;
            pnlProgressBars.Visibility = Visibility.Visible;
        }

        private BrainRGBRecognizerDNAExtra GetBrainRGBRecognizerExtra()
        {
            BrainRGBRecognizerDNAExtra retVal = BrainRGBRecognizerDNAExtra.GetDefaultDNA();

            retVal.IsColor = chkColorVision.IsChecked.Value;
            retVal.UseEdgeDetect = chkEdgeDetect.IsChecked.Value;

            int finalResolution;
            if (int.TryParse(txtFinalResolution.Text, out finalResolution))
            {
                retVal.FinalResolution = finalResolution;
            }

            retVal.ShouldSOMDiscardDupes = chkDiscardDupes.IsChecked.Value;

            return retVal;
        }

        #endregion

        #region estimate #links

        private void DelaunaySegmentEquation1a_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Kick them off
                var results = Enumerable.Range(0, 11).
                    Select(o =>
                    {
                        double ratio = o / 10d;
                        return Tuple.Create(ratio, GetDelaunayResults1a(ratio));
                    }).
                        ToArray();

                // Wait for them to finish
                var final = results.
                    Select(o => Tuple.Create(o.Item1, o.Item2.SelectMany(p => p.Result))).
                    ToArray();

                // Build report
                int min = final.SelectMany(o => o.Item2).Min(o => o.Item1 + o.Item2);
                int max = final.SelectMany(o => o.Item2).Max(o => o.Item1 + o.Item2);

                var reportPrep = Enumerable.Range(min, max - min).
                    Select(o => new
                    {
                        NodeCount = o,
                        AvgPerRatio = final.
                            Select(p => new
                            {
                                Ratio = p.Item1,
                                Avg = p.Item2.Where(q => q.Item1 + q.Item2 == o).Average(q => q.Item3),
                            }),
                    });

                string report = string.Join("\t", UtilityCore.Iterate<string>("", final.Select(o => o.Item1.ToString())));     // header
                report += "\r\n";
                report += string.Join("\r\n", reportPrep.
                    Select(o => string.Join("\t", UtilityCore.Iterate<string>(o.NodeCount.ToString(), o.AvgPerRatio.Select(p => p.Avg.ToString())))));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void DelaunaySegmentEquation1b_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Kick them off
                var results = Enumerable.Range(0, 11).
                    Select(o =>
                    {
                        double ratio = o / 10d;
                        return Tuple.Create(ratio, GetDelaunayResults1b(ratio));
                    }).
                    ToArray();

                // Wait for them to finish
                var final = results.
                    Select(o => Tuple.Create(o.Item1, o.Item2.SelectMany(p => p.Result))).
                    ToArray();

                // Build report
                //int min = final.SelectMany(o => o.Item2).Min(o => o.Item1 + o.Item2);
                //int max = final.SelectMany(o => o.Item2).Max(o => o.Item1 + o.Item2);

                var pointCounts = final.
                    SelectMany(o => o.Item2).
                    ToLookup(o => o.Item1 + o.Item2);

                var reportPrep = pointCounts.
                    Select(o => new
                    {
                        NodeCount = o.Key,
                        AvgPerRatio = final.
                            Select(p => new
                            {
                                Ratio = p.Item1,
                                Avg = p.Item2.Where(q => q.Item1 + q.Item2 == o.Key).Average(q => q.Item3),
                            }),
                    });

                string report = string.Join("\t", UtilityCore.Iterate<string>("", final.Select(o => o.Item1.ToString())));     // header
                report += "\r\n";
                report += string.Join("\r\n", reportPrep.
                    Select(o => string.Join("\t", UtilityCore.Iterate<string>(o.NodeCount.ToString(), o.AvgPerRatio.Select(p => p.Avg.ToString())))));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void DelaunaySegmentEquation2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StringBuilder report = new StringBuilder(4096);

                for (int cntr = 0; cntr < 150; cntr++)
                {
                    report.Append(cntr);

                    for (double percent = 0d; percent <= 1; percent += .1d)
                    {
                        double estimate = GetEstimatedNumberOfLinks_1(cntr, percent);

                        report.Append("\t");
                        report.Append(estimate);
                    }

                    report.AppendLine();
                }

                string text = report.ToString();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static Task<Tuple<int, int, int>[]>[] GetDelaunayResults1a(double brainIORatio)
        {
            const int MAXPOINTS = 150;
            const int NUMRUNSPER = 20;

            // For each of these shapes, create point clouds with different numbers of points, remembering how many segments were generated
            // (this is the pruned links, not raw delaunay)

            // Cube
            var cubes = GetDelaunayResults_FixedSet(MAXPOINTS, NUMRUNSPER, brainIORatio, () => Math3D.GetRandomVector(10).ToPoint());

            // Sphere
            var spheres = GetDelaunayResults_FixedSet(MAXPOINTS, NUMRUNSPER, brainIORatio, () => Math3D.GetRandomVector_Spherical(10).ToPoint());

            // Cylinder
            var cylinders = GetDelaunayResults_FixedSet(MAXPOINTS, NUMRUNSPER, brainIORatio, () => Math3D.GetRandomVector_Circular(10).ToPoint2D().ToPoint3D(StaticRandom.NextDouble(-5, 5)));

            // Cone
            Vector3D axis = new Vector3D(0, 0, 10);
            var cones = GetDelaunayResults_FixedSet(MAXPOINTS, NUMRUNSPER, brainIORatio, () =>
            {
                Vector3D rotatedAxis = Math3D.GetRandomVector_Cone(axis, 45);
                rotatedAxis *= StaticRandom.NextDouble();
                return rotatedAxis.ToPoint();
            });

            return UtilityCore.Iterate<Task<Tuple<int, int, int>[]>>(cubes, spheres, cylinders, cones).ToArray();
        }
        private static Task<Tuple<int, int, int>[]>[] GetDelaunayResults1b(double brainIORatio)
        {
            const int MAXPOINTS = 300;
            const int NUMRUNSPER = 20;

            // For each of these shapes, create point clouds with different numbers of points, remembering how many segments were generated
            // (this is the pruned links, not raw delaunay)

            // Cube
            var cubes = GetDelaunayResults_FixedSet_Power(MAXPOINTS, NUMRUNSPER, brainIORatio, () => Math3D.GetRandomVector(10).ToPoint());

            // Sphere
            var spheres = GetDelaunayResults_FixedSet_Power(MAXPOINTS, NUMRUNSPER, brainIORatio, () => Math3D.GetRandomVector_Spherical(10).ToPoint());

            // Cylinder
            var cylinders = GetDelaunayResults_FixedSet_Power(MAXPOINTS, NUMRUNSPER, brainIORatio, () => Math3D.GetRandomVector_Circular(10).ToPoint2D().ToPoint3D(StaticRandom.NextDouble(-5, 5)));

            // Cone
            Vector3D axis = new Vector3D(0, 0, 10);
            var cones = GetDelaunayResults_FixedSet_Power(MAXPOINTS, NUMRUNSPER, brainIORatio, () =>
            {
                Vector3D rotatedAxis = Math3D.GetRandomVector_Cone(axis, 45);
                rotatedAxis *= StaticRandom.NextDouble();
                return rotatedAxis.ToPoint();
            });

            return UtilityCore.Iterate<Task<Tuple<int, int, int>[]>>(cubes, spheres, cylinders, cones).ToArray();
        }

        private static Task<Tuple<int, int, int>[]> GetDelaunayResults_FixedSet(int maxPoints, int numRunsPerPointCount, double brainIORatio, Func<Point3D> getRandomPoint)
        {
            return Task.Run(() => Enumerable.Range(1, maxPoints).
                AsParallel().
                SelectMany(o => GetDelaunayResults_Run(o, numRunsPerPointCount, brainIORatio, getRandomPoint)).
                ToArray());
        }
        private static Task<Tuple<int, int, int>[]> GetDelaunayResults_FixedSet_Power(int maxPoints, int numRunsPerPointCount, double brainIORatio, Func<Point3D> getRandomPoint)
        {
            const int LINEARTO = 15;
            const int NONLINEARSAMPLES = 15;

            List<int> samplePoints = new List<int>();

            // The first set should increment by 1
            samplePoints.AddRange(Enumerable.Range(1, Math.Min(LINEARTO, maxPoints)));

            // The second set should increment faster and faster
            if (maxPoints > LINEARTO)
            {
                double step = 1d / NONLINEARSAMPLES;
                double current = step;

                while (current <= 1d)
                {
                    double percent = Math.Pow(current, 2);
                    current += step;

                    int samplePoint = LINEARTO + (percent * (maxPoints - LINEARTO)).ToInt_Round();

                    if (!samplePoints.Contains(samplePoint))
                    {
                        samplePoints.Add(samplePoint);
                    }
                }
            }

            return Task.Run(() => samplePoints.
                AsParallel().
                SelectMany(o => GetDelaunayResults_Run(o, numRunsPerPointCount, brainIORatio, getRandomPoint)).
                ToArray());
        }

        /// <returns>
        /// Item1=Num Brains
        /// Item2=Num IO
        /// Item3=Num Links
        /// </returns>
        private static Tuple<int, int, int>[] GetDelaunayResults_Run(int count, int numRuns, double brainIORatio, Func<Point3D> getRandomPoint)
        {
            return Enumerable.Range(0, numRuns).
                //AsParallel().
                Select(o =>
                {
                    //TODO: Randomize the counts a bit
                    int numBrains = (count * brainIORatio).ToInt_Round();
                    int numIO = count - numBrains;

                    Point3D[] brains = Enumerable.Range(0, numBrains).
                        Select(p => getRandomPoint()).
                        ToArray();

                    Point3D[] io = Enumerable.Range(0, numIO).
                        Select(p => getRandomPoint()).
                        ToArray();

                    int numLinks = GetDelaunayResults_SegmentCount(brains, io);

                    return Tuple.Create(numBrains, numIO, numLinks);
                }).
                ToArray();
        }

        private static int GetDelaunayResults_SegmentCount(Point3D[] brains, Point3D[] io)
        {
            LinkItem[] brainItems = brains.
                Select(o => new LinkItem(o, 1d)).
                ToArray();

            LinkItem[] ioItems = io.
                Select(o => new LinkItem(o, 1d)).
                ToArray();

            // Self
            SortedList<Tuple<int, int>, double> all;
            LinkSetPair[] final;
            ItemLinker.Link_Self(out all, out final, brainItems);

            // Brain-IO
            Tuple<int, int>[] oneTwo = ItemLinker.Link_1_2(brainItems, ioItems);

            return final.Length + oneTwo.Length;
        }


        //TODO: Give up on trying to find the best function.  Just take some significant sample points, and do a bezier mesh (maybe store the points in a Lazy<data>)
        private static double GetEstimatedNumberOfLinks_1(int count, double ratio)
        {
            double estimate = Math.Pow(count, 1.175);

            estimate *= (1.8 * ratio) + (2.8 * Math.Pow(ratio, 0.6));
            estimate *= .6;

            return estimate;
        }

        private static int GetEstimatedNumberOfLinks_Final(int brainCount, int ioCount, ItemLinker_ExtraArgs extra)
        {
            return -1;
        }

        #endregion
        #region bezier plot

        private void BezierMesh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double[] ratios = new double[] { 0, .1, .2, .3, .4, .5, .6, .7, .8, .9, 1 };        // x axis
                double[] pointCounts = new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 20, 26, 35, 47, 61, 77, 96, 118, 142, 168, 197, 229, 263, 300 };       // y axis
                double[] linkCounts = new double[]      // z values
                {
                    0, 	0, 	0, 	0, 	0, 	0, 	0, 	0, 	0, 	0, 	0, 
                    0, 	0, 	0, 	1, 	1, 	1, 	1, 	1, 	1, 	1, 	1, 
                    0, 	0, 	2, 	2, 	2, 	2, 	2, 	2, 	2, 	3, 	3, 
                    0, 	0, 	3, 	3, 	3, 	3, 	3, 	4, 	4, 	6, 	6, 
                    0, 	0, 	4, 	4, 	4, 	4, 	5, 	7, 	7, 	7, 	9.65, 
                    0, 	5, 	5, 	5, 	5, 	6, 	8, 	8, 	10.5875, 	10.7125, 	13.65, 
                    0, 	6, 	6, 	6, 	7, 	9, 	9, 	11.6, 	14.875, 	15, 	18.35, 
                    0, 	7, 	7, 	7, 	8, 	10, 	12.7, 	15.8875, 	15.8625, 	19.3125, 	22.9875, 
                    0, 	8, 	8, 	9, 	11, 	11, 	13.6375, 	16.925, 	20.2625, 	24.15, 	28.3125, 
                    0, 	9, 	9, 	10, 	12, 	14.725, 	17.85, 	21.4375, 	25.2125, 	29.4625, 	33.3125, 
                    0, 	10, 	10, 	11, 	13, 	18.7375, 	22.175, 	26.35, 	30.1625, 	34.4125, 	38.5875, 
                    0, 	11, 	11, 	14, 	16.7375, 	19.8, 	23.225, 	27.3875, 	35.8, 	39.7375, 	43.9125, 
                    0, 	12, 	13, 	15, 	17.775, 	20.8875, 	28.2375, 	32.6, 	36.3125, 	45.5, 	49.7, 
                    0, 	13, 	14, 	16, 	21.9125, 	25.2375, 	29.0625, 	37.275, 	41.55, 	51.1, 	55.6125, 
                    0, 	14, 	15, 	17, 	22.8875, 	30.0625, 	33.9125, 	38.4375, 	47.4875, 	56.9, 	61.3, 
                    0, 	15, 	16, 	20.65, 	23.9625, 	31.2625, 	39.4875, 	43.5, 	52.9625, 	57.75, 	67.4375, 
                    0, 	19, 	22, 	27.7125, 	35.4125, 	43.325, 	52.575, 	61.8875, 	70.9625, 	81.325, 	92.025, 
                    0, 	26, 	30.7125, 	41.3875, 	49.2375, 	62.7875, 	77.5875, 	87.5875, 	102.7875, 	112.9625, 	130.1625, 
                    0, 	37, 	46.2, 	58.05, 	77.1875, 	96.7, 	111.2375, 	127.1625, 	149.225, 	170.65, 	187.2375, 
                    0, 	51.725, 	66.25, 	88.4875, 	113.325, 	139.7, 	160.75, 	188.2125, 	216.375, 	239.225, 	268.4125, 
                    0, 	68.775, 	93.7875, 	122.475, 	154.125, 	185.6875, 	224.4375, 	259.775, 	294.8, 	330.8125, 	366.6625, 
                    0, 	92.0875, 	123.45, 	163.65, 	206.8625, 	246.5875, 	293.4875, 	338.775, 	386.3125, 	428.5, 	476.425, 
                    0, 	119.3875, 	162.0875, 	216.4, 	266.2375, 	324.325, 	381.125, 	435.9, 	496.625, 	550.9375, 	613.95, 
                    0, 	150.3875, 	210.2125, 	269.775, 	338.0125, 	409.425, 	482.075, 	554.5125, 	623.5125, 	696.275, 	769.2875, 
                    0, 	183.4875, 	256.6125, 	340.5875, 	422.725, 	507.1125, 	591.3125, 	675.8625, 	768.6375, 	856.35, 	942.5375, 
                    0, 	223.725, 	315.6625, 	407.325, 	507.8375, 	609.775, 	715.7, 	819.425, 	921.65, 	1026.95, 	1130.8125, 
                    0, 	268.2125, 	373.125, 	490.2375, 	608.125, 	726.0375, 	847.1875, 	972.9625, 	1100.25, 	1219.45, 	1345.4875, 
                    0, 	316.35, 	443.4625, 	581.4625, 	720.925, 	854.7875, 	1000.775, 	1144.6375, 	1290.3, 	1434.2875, 	1584.5125, 
                    0, 	365.9875, 	520.325, 	675.4625, 	834.6, 	1003.275, 	1164.6, 	1328.7, 	1492.9625, 	1665.35, 	1830.9875, 
                    0, 	424.6375, 	598.35, 	779.7, 	963.8, 	1151.2375, 	1340.875, 	1530.5, 	1722.0375, 	1914.4625, 	2106.5375
                };

                BezierMesh mesh = new BezierMesh(ratios, pointCounts, linkCounts);

                //double test = mesh.EstimateValue(.2, 8);
                double test = mesh.EstimateValue(.21, 7.9);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void BezierMeshFlipX_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double[] ratios = new double[] { .4, .3, .2, .1 };        // x axis
                double[] pointCounts = new double[] { 3, 4, 5, 6, 7, 8, 9 };       // y axis
                double[] linkCounts = new double[]      // z values
                {
                    2, 2, 2, 0,
                    3, 3, 3, 0,
                    4, 4, 4, 0,
                    5, 5, 5, 5,
                    7, 6, 6, 6,
                    8, 7, 7, 7,
                    11, 9, 8, 8,
                };

                BezierMesh mesh = new BezierMesh(ratios, pointCounts, linkCounts);

                double test = mesh.EstimateValue(.2, 8);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void BezierMeshFlipY_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double[] ratios = new double[] { .1, .2, .3, .4 };        // x axis
                double[] pointCounts = new double[] { 9, 8, 7, 6, 5, 4, 3 };       // y axis
                double[] linkCounts = new double[]      // z values
                {
                    8, 8, 9, 11,
                    7, 7, 7, 8,
                    6, 6, 6, 7,
                    5, 5, 5, 5,
                    0, 4, 4, 4,
                    0, 3, 3, 3,
                    0, 2, 2, 2,
                };

                BezierMesh mesh = new BezierMesh(ratios, pointCounts, linkCounts);

                double test = mesh.EstimateValue(.2, 8);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void BezierMeshFlipXY_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double[] ratios = new double[] { .4, .3, .2, .1 };        // x axis
                double[] pointCounts = new double[] { 9, 8, 7, 6, 5, 4, 3 };       // y axis
                double[] linkCounts = new double[]      // z values
                {
                    11, 9, 8, 8,
                    8, 7, 7, 7,
                    7, 6, 6, 6,
                    5, 5, 5, 5,
                    4, 4, 4, 0,
                    3, 3, 3, 0,
                    2, 2, 2, 0,
                };

                BezierMesh mesh = new BezierMesh(ratios, pointCounts, linkCounts);

                double test = mesh.EstimateValue(.2, 8);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
