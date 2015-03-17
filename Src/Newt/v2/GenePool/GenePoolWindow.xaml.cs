using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using Game.Newt.v2.GameItems;
using Game.Newt.v2.NewtonDynamics;
using Game.Newt.v2.GenePool.MapParts;

namespace Game.Newt.v2.GenePool
{
    public partial class GenePoolWindow : Window
    {
        #region Class: ExplodingBot

        public class ExplodingBot : ExplosionWithVisual
        {
            public ExplodingBot(Swimbot bot, double waveSpeed, double forceAtCenter, double maxRadius, Viewport3D viewport, double visualStartRadius)
                : base(bot.PhysicsBody, waveSpeed, forceAtCenter, maxRadius, viewport, visualStartRadius)
            {
                this.Bot = bot;
            }

            public Swimbot Bot
            {
                get;
                private set;
            }
        }

        #endregion

        #region Declaration Section

        private const double BOUNDRYSIZE = 40; //400;
        private const double BOUNDRYSIZEHALF = BOUNDRYSIZE * .5d;

        //TODO: Surround the world in a spherical shell
        private Point3D _boundryMin;
        private Point3D _boundryMax;

        private EditorOptions _editorOptions = new EditorOptions();
        private ItemOptions _itemOptions = null;
        private SharedVisuals _sharedVisuals = new SharedVisuals();

        private World _world = null;
        private Map _map = null;

        private UpdateManager _updateManager = null;

        private CameraPool _cameraPool = null;

        //private GravityFieldUniform _gravity = null;
        private RadiationField _radiation = null;

        private MaterialManager _materialManager = null;
        private int _material_Wall = -1;
        private int _material_Bot = -1;
        private int _material_ExplodingBot = -1;		// there is no property on body to turn off collision detection, that's done by its current material
        private int _material_Food = -1;
        private int _material_Egg = -1;
        private int _material_Projectile = -1;

        private List<Swimbot> _bots = new List<Swimbot>();
        private List<ExplodingBot> _explosions = new List<ExplodingBot>();
        private List<Mineral> _food = new List<Mineral>();

        /// <summary>
        /// This listens to the mouse/keyboard and controls the camera
        /// </summary>
        private TrackBallRoam _trackball = null;

        private ItemSelectDragLogic _selectionLogic = null;

        #endregion

        #region Constructor

        public GenePoolWindow()
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

                #region Init World

                _boundryMin = new Point3D(-BOUNDRYSIZEHALF, -BOUNDRYSIZEHALF, -BOUNDRYSIZEHALF);
                _boundryMax = new Point3D(BOUNDRYSIZEHALF, BOUNDRYSIZEHALF, BOUNDRYSIZEHALF);

                _world = new World();
                _world.Updating += new EventHandler<WorldUpdatingArgs>(World_Updating);

                List<Point3D[]> innerLines, outerLines;
                _world.SetCollisionBoundry(out innerLines, out outerLines, _boundryMin, _boundryMax);

                //TODO: Only draw the boundry lines if options say to

                #endregion
                #region Materials

                _materialManager = new MaterialManager(_world);

                // Wall
                Game.Newt.v2.NewtonDynamics.Material material = new Game.Newt.v2.NewtonDynamics.Material();
                material.Elasticity = .1d;
                _material_Wall = _materialManager.AddMaterial(material);

                // Bot
                material = new Game.Newt.v2.NewtonDynamics.Material();
                _material_Bot = _materialManager.AddMaterial(material);

                // Exploding Bot
                material = new Game.Newt.v2.NewtonDynamics.Material();
                material.IsCollidable = false;
                _material_ExplodingBot = _materialManager.AddMaterial(material);

                // Food
                material = new Game.Newt.v2.NewtonDynamics.Material();
                material.Elasticity = .1d;
                _material_Food = _materialManager.AddMaterial(material);

                // Egg
                material = new Game.Newt.v2.NewtonDynamics.Material();
                material.Elasticity = .5d;
                _material_Egg = _materialManager.AddMaterial(material);

                material = new Game.Newt.v2.NewtonDynamics.Material();
                _material_Projectile = _materialManager.AddMaterial(material);

                // Collisions
                _materialManager.RegisterCollisionEvent(_material_Bot, _material_Bot, Collision_BotBot);
                _materialManager.RegisterCollisionEvent(_material_Bot, _material_Food, Collision_BotFood);
                //TODO: May want to listen to projectile collisions

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
                #region Camera Pool

                //TODO: Make the number of threads more configurable, look at how many processors there are
                //_cameraPool = new CameraPool(2, Colors.Black);
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
                    new Type[] { typeof(Swimbot) },
                    new Type[] { typeof(Swimbot) },
                    _map);

                #endregion
                #region Fields

                _radiation = new RadiationField();
                _radiation.AmbientRadiation = 0d;

                //_gravity = new GravityFieldUniform();
                //_gravity.Gravity = new Vector3D(0, 0, 0);

                //TODO: Support a uniform fluid
                //FluidField

                #endregion
                #region ItemSelectDragLogic

                _selectionLogic = new ItemSelectDragLogic(_map, _camera, _viewport, grdViewPort);

                _selectionLogic.SelectableTypes.Add(typeof(Ship));
                _selectionLogic.SelectableTypes.Add(typeof(Mineral));
                _selectionLogic.SelectableTypes.Add(typeof(Egg));

                _selectionLogic.ShouldMoveItemWithSpring = true;
                _selectionLogic.ShouldSpringCauseTorque = false;
                _selectionLogic.SpringColor = null;  // Colors.Chartreuse;
                _selectionLogic.ShowDebugVisuals = false;// true;

                _selectionLogic.ItemSelected += new EventHandler<ItemSelectedArgs>(SelectionLogic_ItemSelected);

                #endregion

                _world.UnPause();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }
        private void Window_Closed(object sender, EventArgs e)
        {
            //TODO: Now that ship creation is async, this can run before ships are finished building.  Disposing world
            //early causes bad things.  See FlyingBeansWindow.Window_Closed for a hacked solution

            try
            {
                _world.Pause();

                _updateManager.Dispose();

                _selectionLogic.UnselectItem();
                _selectionLogic = null;

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
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Collision_BotBot(object sender, MaterialCollisionArgs e)
        {
            try
            {

                //TODO: If they are able to reproduce, create an egg


                //Bean bean = _beans.Where(o => o.PhysicsBody.Equals(e.Body0)).FirstOrDefault();
                //if (bean != null)
                //{
                //    bean.CollidedBean();
                //}

                //bean = _beans.Where(o => o.PhysicsBody.Equals(e.Body1)).FirstOrDefault();
                //if (bean != null)
                //{
                //    bean.CollidedBean();
                //}
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Collision_BotFood(object sender, MaterialCollisionArgs e)
        {
            try
            {
                Body shipBody = e.GetBody(_material_Bot);
                Body mineralBody = e.GetBody(_material_Food);

                Swimbot bot = _bots.Where(o => o.PhysicsBody.Equals(shipBody)).FirstOrDefault();
                if (bot == null)
                {
                    return;
                }

                Mineral mineral = _map.GetItem<Mineral>(mineralBody);
                if (mineral == null)
                {
                    return;
                }

                bot.CollidedMineral(mineral);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Food_ApplyForceAndTorque(object sender, BodyApplyForceAndTorqueArgs e)
        {
            //TODO: Fluid
        }
        private void Egg_ApplyForceAndTorque(object sender, BodyApplyForceAndTorqueArgs e)
        {
            //TODO: Fluid
        }
        private void Bot_ApplyForceAndTorque(object sender, BodyApplyForceAndTorqueArgs e)
        {
            //TODO: Fluid

            //if (e.Body.MaterialGroupID == _material_Bot && !e.Body.IsAsleep)
            //{
            //    // Apply gravity
            //    e.Body.AddForce(_options.GravityField.Gravity * e.Body.Mass);

            //    // Apply an in-facing cylindrical force
            //    e.Body.AddForce(_boundryField.GetForce(e.Body.Position));

            //    // Nearby explosions
            //    foreach (Explosion explosion in _explosions)
            //    {
            //        explosion.ApplyForceToBody(e.Body);
            //    }
            //}

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

                _selectionLogic.LeftMouseDown(e, null);
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
                _selectionLogic.MouseMove(e);
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

                _selectionLogic.LeftMouseUp(e);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SelectionLogic_ItemSelected(object sender, ItemSelectedArgs e)
        {
            try
            {
                ShipViewerWindow viewer = null;
                if (chkShowViewerOnSelect.IsChecked.Value && e.Item is Swimbot)
                {
                    viewer = ShowShipViewer((Swimbot)e.Item, e.ClickPoint);
                }

                e.Requested_SelectedItem_Instance = new SelectedItemSwimbots(e.Item, e.Offset, viewer, grdViewPort, _viewport, pnlSelectionVisuals, _camera, e.ShouldMoveItemWithSpring, e.ShouldSpringCauseTorque, e.SpringColor);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnAddFood_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Point3D position = Math3D.GetRandomVector_Spherical(BOUNDRYSIZEHALF * .75d).ToPoint();

                Mineral mineral = new Mineral(MineralType.Emerald, position, .15d, _world, _material_Food, _sharedVisuals);

                mineral.PhysicsBody.AngularVelocity = Math3D.GetRandomVector_Spherical(1d);
                mineral.PhysicsBody.Velocity = Math3D.GetRandomVector_Spherical(4d);

                mineral.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(Food_ApplyForceAndTorque);

                _map.AddItem(mineral);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnAddPoison_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Point3D position = Math3D.GetRandomVector_Spherical(BOUNDRYSIZEHALF * .75d).ToPoint();

                Mineral mineral = new Mineral(MineralType.Ruby, position, .15d, _world, _material_Food, _sharedVisuals);

                mineral.PhysicsBody.AngularVelocity = Math3D.GetRandomVector_Spherical(1d);
                mineral.PhysicsBody.Velocity = Math3D.GetRandomVector_Spherical(4d);

                mineral.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(Food_ApplyForceAndTorque);

                _map.AddItem(mineral);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void btnAddBot_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //TODO: Implement:
                //  ConverterMatterToFuel
                //  ConverterMatterToEnergy

                //_itemOptions.CameraColorRGBNeuronDensity = 160 * 3;

                #region DNA

                List<PartDNA> parts = new List<PartDNA>();

                parts.Add(new PartDNA() { PartType = EnergyTank.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0, 0, 1.43121944558768), Scale = new Vector3D(2.7969681248785, 2.7969681248785, 0.754175785992705) });

                parts.Add(new PartDNA() { PartType = FuelTank.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0.617709955450103, 0, -1.17934947097654), Scale = new Vector3D(1.27861383329322, 1.27861383329322, 1.2242161981366) });
                parts.Add(new PartDNA() { PartType = FuelTank.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-2.16134944924876E-05, -0.60985890545398, -1.17934947097654), Scale = new Vector3D(1.27861383329322, 1.27861383329322, 1.2242161981366) });
                parts.Add(new PartDNA() { PartType = FuelTank.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0.00387496389974934, 0.618396858731443, -1.17934947097654), Scale = new Vector3D(1.27861383329322, 1.27861383329322, 1.2242161981366) });
                parts.Add(new PartDNA() { PartType = FuelTank.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-0.624666220599071, 0.00100528924605325, -1.17934947097654), Scale = new Vector3D(1.27861383329322, 1.27861383329322, 1.2242161981366) });

                parts.Add(new PartDNA() { PartType = CargoBay.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0, 0, 0), Scale = new Vector3D(1, 1, 1) });

                parts.Add(new PartDNA() { PartType = Brain.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-0.850947345148112, -0.0019217800299223, -0.1657062616585), Scale = new Vector3D(0.730753808122994, 0.730753808122994, 0.730753808122994) });
                parts.Add(new PartDNA() { PartType = Brain.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-0.00589599969475301, 0.850929089078933, -0.1657062616585), Scale = new Vector3D(0.730753808122994, 0.730753808122994, 0.730753808122994) });
                parts.Add(new PartDNA() { PartType = Brain.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-0.00522521361558187, -0.850933472485232, -0.165706261658499), Scale = new Vector3D(0.730753808122994, 0.730753808122994, 0.730753808122994) });
                parts.Add(new PartDNA() { PartType = Brain.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0.850949515219971, 0, -0.165706261658499), Scale = new Vector3D(0.730753808122994, 0.730753808122994, 0.730753808122994) });

                parts.Add(new PartDNA() { PartType = ConverterMatterToFuel.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0.232899395582397, -0.238935114546265, 0.773100284172734), Scale = new Vector3D(0.903613735551713, 0.903613735551713, 0.903613735551713) });
                parts.Add(new PartDNA() { PartType = ConverterMatterToEnergy.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-0.22797553388197, 0.226717979419686, 0.775967026847012), Scale = new Vector3D(0.903613735551713, 0.903613735551713, 0.903613735551713) });

                parts.Add(new PartDNA() { PartType = SensorSpin.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0.262933776282848, 0.240405245709603, 0.755961022082205), Scale = new Vector3D(1, 1, 1) });
                parts.Add(new PartDNA() { PartType = EnergyTank.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-0.250217552102507, -0.280865739393283, 0.743561713316594), Scale = new Vector3D(0.399083346950047, 0.399083346950047, 0.236501125779081) });       // This is just a placeholder for a future sensor (need to put something here so mass is balanced)

                parts.Add(new PartDNA() { PartType = CameraColorRGB.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-0.00208559296997747, 0, 2.00222812584528), Scale = new Vector3D(1, 1, 1) });

                parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-0.00137913748707474, 0, -1.20661030017992), Scale = new Vector3D(1.29260597488229, 1.29260597488229, 1.29260597488229), ThrusterType = ThrusterType.One });

                parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = new Quaternion(-0.623025498564933, -0.775855164007238, -0.0622612340741654, 0.0775340657568128), Position = new Point3D(0.189965645143804, -0.859019870524953, 0.883258802478369), Scale = new Vector3D(0.522840753255495, 0.522840753255495, 0.522840753255495), ThrusterType = ThrusterType.One });
                parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = new Quaternion(0.948585651195026, -0.300495041099996, 0.0947956598959211, 0.0300295768554372), Position = new Point3D(-0.719304806310003, 0.506559650891123, 0.883258802478371), Scale = new Vector3D(0.522840753255495, 0.522840753255495, 0.522840753255495), ThrusterType = ThrusterType.One });
                parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = new Quaternion(0.304146575685315, -0.947421167104513, 0.0303944881633535, 0.0946792887093452), Position = new Point3D(0.715381156965589, 0.512085817563994, 0.883258802478369), Scale = new Vector3D(0.522840753255495, 0.522840753255495, 0.522840753255495), ThrusterType = ThrusterType.One });

                ShipDNA dna = ShipDNA.Create(parts);
                dna.ShipLineage = Guid.NewGuid().ToString();

                #endregion

                Swimbot bot = await Swimbot.GetNewSwimbotAsync(_editorOptions, _itemOptions, dna, _world, _material_Bot, _material_Projectile, _radiation, null, _cameraPool, _map);
                bot.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(Bot_ApplyForceAndTorque);

                bot.PhysicsBody.Rotation = Math3D.GetRandomRotation();
                bot.PhysicsBody.Position = Math3D.GetRandomVector_Spherical(BOUNDRYSIZEHALF * .5d).ToPoint();

                //bot.PhysicsBody.Position = new Point3D(0, 0, BOUNDRYSIZEHALF * -.75d);

                if (bot.Energy != null)
                {
                    bot.Energy.QuantityCurrent = bot.Energy.QuantityMax * .5d;
                }
                if (bot.Fuel != null)
                {
                    bot.Fuel.QuantityCurrent = bot.Fuel.QuantityMax * .5d;
                }
                bot.RecalculateMass();

                _map.AddItem(bot);
                _bots.Add(bot);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnAddEgg_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_bots.Count == 0)
                {
                    MessageBox.Show("Add a bot first", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Swimbot bot = _bots[StaticRandom.Next(_bots.Count)];

                Point3D position = bot.PositionWorld + Math3D.GetRandomVector_Spherical_Shell(bot.Radius * 1.5d);

                Egg egg = new Egg(position, _world, _material_Egg, _itemOptions, bot.GetNewDNA());

                egg.PhysicsBody.AngularVelocity = bot.PhysicsBody.AngularVelocity;
                egg.PhysicsBody.Velocity = bot.PhysicsBody.Velocity;

                egg.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(Egg_ApplyForceAndTorque);

                _map.AddItem(egg);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _selectionLogic.UnselectItem();

                foreach (IMapObject item in _map.GetAllItems().ToArray())
                {
                    //TODO: Map will blow up if the item is already removed, make it fail silently (have it return a bool)
                    _map.RemoveItem(item);
                }

                foreach (Swimbot bot in _bots)
                {
                    bot.Dispose();
                }
                _bots.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private ShipViewerWindow ShowShipViewer(Swimbot bot, Point clickPoint)
        {
            // Make the camera follow this bean
            //TODO: Place a 2D graphic behind the selected ship
            //TODO: Zoom in on it
            #region Create Viewer

            ShipViewerWindow retVal = new ShipViewerWindow(bot);
            retVal.Owner = this;		// the other settings like topmost, showintaskbar, etc are already set in the window's xaml
            retVal.PopupBorder = new SolidColorBrush(UtilityWPF.ColorFromHex("60000000"));
            retVal.PopupBackground = new SolidColorBrush(UtilityWPF.ColorFromHex("30000000"));
            retVal.ViewportBorder = new SolidColorBrush(UtilityWPF.ColorFromHex("E0000000"));

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

            retVal.ViewportBackground = brush;

            retVal.PanelBorder = new SolidColorBrush(UtilityWPF.ColorFromHex("8068736B"));
            retVal.PanelBackground = new SolidColorBrush(UtilityWPF.ColorFromHex("80424F45"));
            retVal.Foreground = new SolidColorBrush(UtilityWPF.ColorFromHex("F0F0F0"));

            #endregion

            // Place Viewer
            Point screenClickPoint = UtilityWPF.TransformToScreen(clickPoint, grdViewPort);
            ShipViewerWindow.PlaceViewerInCorner(retVal, screenClickPoint);

            // This places the viewer to the right of where they clicked - which is fine if they can't drag the item
            // around, but it's in the way if they try to move the object
            //Point popupPoint = new Point(windowClickPoint.X + 50, windowClickPoint.Y - (viewer.Height / 3d));
            //popupPoint = UtilityWPF.EnsureWindowIsOnScreen(popupPoint, new Size(viewer.Width, viewer.Height));		// don't let the popup straddle monitors
            //viewer.Left = popupPoint.X;
            //viewer.Top = popupPoint.Y;

            retVal.Show();      // it needs to be shown first to get the size

            return retVal;
        }

        #endregion
    }
}
