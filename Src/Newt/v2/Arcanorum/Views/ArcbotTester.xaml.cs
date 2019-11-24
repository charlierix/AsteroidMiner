using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.HelperClassesWPF.Controls3D;
using Game.Newt.v2.Arcanorum.MapObjects;
using Game.Newt.v2.Arcanorum.Parts;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.Controls;
using Game.Newt.v2.GameItems.ShipEditor;
using Game.Newt.v2.GameItems.ShipParts;
using Game.Newt.v2.NewtonDynamics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace Game.Newt.v2.Arcanorum.Views
{
    public partial class ArcbotTester : Window
    {
        #region class: ConeResult

        private class ConeResult
        {
            public int Count { get; set; }
            public double Angle { get; set; }
            public double RadiusMin { get; set; }
            public double RadiusMax { get; set; }
            public Vector3D[] Points { get; set; }
            public double AvgPairLength { get; set; }
            public double Volume { get; set; }

            public double Count_per_Volume => Count / Volume;
            public double Avg_per_Volume => AvgPairLength / Volume;

            public double Volume_per_Count => Volume / Count;
            public double Volume_per_Avg => Volume / AvgPairLength;
        }

        #endregion

        #region Declaration Section

        private const double BOUNDRYSIZE = 100;
        private const double BOUNDRYSIZEHALF = BOUNDRYSIZE * .5d;

        private const double HOMINGRADIUS = 15;

        private const string OPTIONSFOLDER = "ArcbotTester";

        private Point3D _boundryMin;
        private Point3D _boundryMax;

        /// <summary>
        /// This listens to the mouse/keyboard and controls the camera
        /// </summary>
        private TrackBallRoam _trackball = null;

        private World _world = null;
        private Map _map = null;
        private GravityFieldUniform _gravity = null;

        private UpdateManager _updateManager = null;

        private EditorOptions _editorOptions = null;
        private ItemOptionsArco _itemOptions = null;

        private MaterialManager _materialManager = null;
        private MaterialIDs _materialIDs = null;

        private DragHitShape _dragPlane = null;

        private KeepItems2D _keep2D = null;

        private Point? _viewerPos = null;
        private ShipViewerWindow _viewer = null;
        private List<SensorVisionViewer> _sensorViewers = new List<SensorVisionViewer>();

        private Point3D? _setPos_ChasePoint = null;

        #endregion

        #region Constructor

        public ArcbotTester()
        {
            InitializeComponent();

            _trackball = new TrackBallRoam(_camera);
            //_trackball.KeyPanScale = 15d;
            _trackball.EventSource = grdViewPort;       //NOTE:  If this control doesn't have a background color set, the trackball won't see events (I think transparent is ok, just not null)
            _trackball.AllowZoomOnMouseWheel = true;
            _trackball.Mappings.AddRange(TrackBallMapping.GetPrebuilt(TrackBallMapping.PrebuiltMapping.MouseComplete));
            //_trackball.GetOrbitRadius += new GetOrbitRadiusHandler(Trackball_GetOrbitRadius);
            _trackball.ShouldHitTestOnOrbit = false;

        }

        #endregion

        #region Event Listeners

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                #region misc

                _itemOptions = new ItemOptionsArco();
                //_itemOptions.VisionSensorNeuronDensity = 80;

                _editorOptions = new EditorOptions();

                _gravity = new GravityFieldUniform() { Gravity = new Vector3D(0, -_itemOptions.Gravity, 0) };

                #endregion

                #region init world

                _boundryMin = new Point3D(-BOUNDRYSIZEHALF, -BOUNDRYSIZEHALF, -BOUNDRYSIZEHALF);
                _boundryMax = new Point3D(BOUNDRYSIZEHALF, BOUNDRYSIZEHALF, BOUNDRYSIZEHALF);

                _world = new World();
                _world.Updating += new EventHandler<WorldUpdatingArgs>(World_Updating);

                _world.SetCollisionBoundry(_boundryMin, _boundryMax);

                //TODO: Only draw the boundry lines if options say to

                // Draw the lines
                ScreenSpaceLines3D boundryLines = new ScreenSpaceLines3D(true);
                boundryLines.Thickness = 1d;
                boundryLines.Color = Colors.Silver;

                boundryLines.AddLine(new Point3D(-BOUNDRYSIZEHALF, -BOUNDRYSIZEHALF, 0), new Point3D(BOUNDRYSIZEHALF, -BOUNDRYSIZEHALF, 0));
                boundryLines.AddLine(new Point3D(BOUNDRYSIZEHALF, -BOUNDRYSIZEHALF, 0), new Point3D(BOUNDRYSIZEHALF, BOUNDRYSIZEHALF, 0));
                boundryLines.AddLine(new Point3D(BOUNDRYSIZEHALF, BOUNDRYSIZEHALF, 0), new Point3D(-BOUNDRYSIZEHALF, BOUNDRYSIZEHALF, 0));
                boundryLines.AddLine(new Point3D(-BOUNDRYSIZEHALF, BOUNDRYSIZEHALF, 0), new Point3D(-BOUNDRYSIZEHALF, -BOUNDRYSIZEHALF, 0));

                _viewport.Children.Add(boundryLines);

                #endregion
                #region materials

                _materialManager = new MaterialManager(_world);
                _materialIDs = new MaterialIDs();

                // Wall
                Game.Newt.v2.NewtonDynamics.Material material = new Game.Newt.v2.NewtonDynamics.Material();
                material.Elasticity = ItemOptionsArco.ELASTICITY_WALL;
                _materialIDs.Wall = _materialManager.AddMaterial(material);

                // Bot
                material = new Game.Newt.v2.NewtonDynamics.Material();
                _materialIDs.Bot = _materialManager.AddMaterial(material);

                // Bot Ram
                material = new Game.Newt.v2.NewtonDynamics.Material();
                material.Elasticity = ItemOptionsArco.ELASTICITY_BOTRAM;
                _materialIDs.BotRam = _materialManager.AddMaterial(material);

                // Exploding Bot
                material = new Game.Newt.v2.NewtonDynamics.Material();
                material.IsCollidable = false;
                _materialIDs.ExplodingBot = _materialManager.AddMaterial(material);

                // Weapon
                material = new Game.Newt.v2.NewtonDynamics.Material();
                _materialIDs.Weapon = _materialManager.AddMaterial(material);

                // Treasure Box
                material = new Game.Newt.v2.NewtonDynamics.Material();
                _materialIDs.TreasureBox = _materialManager.AddMaterial(material);

                // Collisions
                _materialManager.RegisterCollisionEvent(_materialIDs.Bot, _materialIDs.Bot, Collision_BotBot);
                _materialManager.RegisterCollisionEvent(_materialIDs.Bot, _materialIDs.Weapon, Collision_BotWeapon);
                _materialManager.RegisterCollisionEvent(_materialIDs.Weapon, _materialIDs.Weapon, Collision_WeaponWeapon);
                //_materialManager.RegisterCollisionEvent(_materialIDs.Bot, _materialIDs.Wall, Collision_BotWall);
                _materialManager.RegisterCollisionEvent(_materialIDs.Weapon, _materialIDs.Wall, Collision_WeaponWall);
                _materialManager.RegisterCollisionEvent(_materialIDs.Weapon, _materialIDs.TreasureBox, Collision_WeaponTreasureBox);
                _materialManager.RegisterCollisionEvent(_materialIDs.Bot, _materialIDs.TreasureBox, Collision_BotTreasureBox);

                #endregion
                #region map

                _map = new Map(_viewport, null, _world)
                {
                    ShouldBuildSnapshots = true,
                    //ShouldShowSnapshotLines = false,
                };

                #endregion
                #region keep 2D

                //TODO: drag plane should either be a plane or a large cylinder, based on the current (level|scene|stage|area|arena|map|place|region|zone)

                // This game is 3D emulating 2D, so always have the mouse go to the XY plane
                _dragPlane = new DragHitShape();
                _dragPlane.SetShape_Plane(new Triangle(new Point3D(-1, -1, 0), new Point3D(1, -1, 0), new Point3D(0, 1, 0)));

                // This will keep objects onto that plane using forces (not velocities)
                _keep2D = new KeepItems2D
                {
                    SnapShape = _dragPlane,
                };

                #endregion

                #region update manager

                _updateManager = new UpdateManager(
                    new[] { typeof(Bot), typeof(ArcBot), typeof(ArcBotNPC), typeof(ArcBot2), typeof(NPCNest) },
                    new[] { typeof(Bot), typeof(ArcBot), typeof(ArcBotNPC), typeof(ArcBot2) },
                    _map);

                #endregion

                _world.UnPause();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Window_Closed(object sender, System.EventArgs e)
        {
            try
            {
                if (_viewer != null)
                {
                    _viewer.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Viewer_Closed(object sender, EventArgs e)
        {
            _viewer = null;
        }

        private void World_Updating(object sender, WorldUpdatingArgs e)
        {
            try
            {
                _updateManager.Update_MainThread(e.ElapsedTime);

                _keep2D.Update();

                foreach (var viewer in _sensorViewers)
                {
                    viewer.Update();
                }

                if (_setPos_ChasePoint != null)
                {
                    ArcBot2 bot = SetPosition_GetArcBot();

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Collision_BotTreasureBox(object sender, MaterialCollisionArgs e)
        {
        }
        private void Collision_WeaponTreasureBox(object sender, MaterialCollisionArgs e)
        {
        }
        private void Collision_WeaponWall(object sender, MaterialCollisionArgs e)
        {
        }
        private void Collision_WeaponWeapon(object sender, MaterialCollisionArgs e)
        {
        }
        private void Collision_BotWeapon(object sender, MaterialCollisionArgs e)
        {
        }
        private void Collision_BotBot(object sender, MaterialCollisionArgs e)
        {
        }

        private void Bot_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearScene();

                #region dna

                double halfSqrt2 = Math.Sqrt(2) / 2;

                double sensorXY = .13;//.13;
                double sensorZ = .9;

                // Create bot dna (remember the unique ID of the brainneat part)
                ShipPartDNA[] parts = new ShipPartDNA[]
                {
                    //new ShipPartDNA() { PartType = SensorGravity.PARTTYPE, Position = new Point3D(sensorXY, sensorXY, sensorZ), Orientation = new Quaternion(halfSqrt2,0,0,halfSqrt2), Scale = new Vector3D(1, 1, 1) },
                    //new ShipPartDNA() { PartType = SensorCollision.PARTTYPE, Position = new Point3D(-sensorXY, sensorXY, sensorZ), Orientation = new Quaternion(halfSqrt2,0,0,halfSqrt2), Scale = new Vector3D(1, 1, 1) },
                    new ShipPartDNA() { PartType = SensorHoming.PARTTYPE, Position = new Point3D(-sensorXY, -sensorXY, sensorZ), Orientation = new Quaternion(halfSqrt2,0,0,halfSqrt2), Scale = new Vector3D(1, 1, 1) },
                    //new ShipPartDNA() { PartType = SensorVelocity.PARTTYPE, Position = new Point3D(sensorXY, -sensorXY, sensorZ), Orientation = new Quaternion(halfSqrt2,0,0,halfSqrt2), Scale = new Vector3D(1, 1, 1) },

                    new ShipPartDNA() { PartType = PlasmaTank.PARTTYPE, Position = new Point3D(0,0,0.543528607254167), Orientation = Quaternion.Identity, Scale = new Vector3D(1.89209635028234,1.89209635028234,0.252586385520305) },
                    new ShipPartDNA() { PartType = EnergyTank.PARTTYPE, Position = new Point3D(0,0,0.307466813523783), Orientation = Quaternion.Identity, Scale = new Vector3D(1,1,0.149239910187544) },

                    //new CameraHardCodedDNA() { PartType = CameraHardCoded.PARTTYPE, Position = new Point3D(0.249826620626459,0.20802214411661,-0.00333407878894652), Orientation = new Quaternion(-0.663257774324101,-0.239984678742854,-0.666577701043659,0.241185918408767), Scale = new Vector3D(1,1,1) },
                    //new CameraHardCodedDNA() { PartType = CameraHardCoded.PARTTYPE, Position = new Point3D(0.262012369728025,-0.145863634157708,-0.00333407878894495), Orientation = new Quaternion(0.682710603553056,-0.177227969093181,0.686127901113979,0.178115081001736), Scale = new Vector3D(1,1,1) },
                    //new CameraHardCodedDNA() { PartType = CameraHardCoded.PARTTYPE, Position = new Point3D(-0.303779554831675,-0.0127691984878863,-0.00333407878894622), Orientation = new Quaternion(0.0148144542160312,-0.705183701725268,0.0148886077416918,0.708713488037898), Scale = new Vector3D(1,1,1) },
                    //new CameraHardCodedDNA() { PartType = CameraHardCoded.PARTTYPE, Position = new Point3D(-0.0661289759910267,-0.302046076168234,-0.00333407878894601), Orientation = new Quaternion(0.442211473505322,-0.549502078187993,0.444424956322137,0.552252602497621), Scale = new Vector3D(1,1,1) },
                    //new CameraHardCodedDNA() { PartType = CameraHardCoded.PARTTYPE, Position = new Point3D(-0.110459969641624,0.279162567591854,-0.00333407878894656), Orientation = new Quaternion(-0.396521198492132,-0.583330489841766,-0.398505979331054,0.586250341752333), Scale = new Vector3D(1,1,1) },

                    // If there are more than one, give each a unique guid to know which to train (only want to train one at a time)
                    new BrainNEATDNA() { PartType = BrainNEAT.PARTTYPE, Position = new Point3D(0,0,-0.612726700748439), Orientation = Quaternion.Identity, Scale = new Vector3D(1,1,1) },

                    new ImpulseEngineDNA() { PartType = ImpulseEngine.PARTTYPE, Position = new Point3D(0,0,-1.52174128035678), Orientation = Quaternion.Identity, Scale = new Vector3D(1,1,1), ImpulseEngineType = ImpulseEngineType.Translate },
                };

                ShipDNA dna = ShipDNA.Create("bot", parts);

                #endregion

                #region ship extra args

                ShipExtraArgs extra = new ShipExtraArgs()
                {
                    Options = new EditorOptions(),
                    ItemOptions = new ItemOptionsArco(),
                };
                extra.Gravity = new GravityFieldUniform() { Gravity = new Vector3D(0, -((ItemOptionsArco)extra.ItemOptions).Gravity, 0) };

                #endregion
                #region create bot

                // Need to create a bot that will wire up neurons, then save off that dna.  Otherwise, each room's bot will have a random
                // wiring, and the training will be meaningless

                ShipCoreArgs core = new ShipCoreArgs()
                {
                    World = _world,
                    Material_Ship = _materialIDs.Bot,
                    Map = _map,
                };

                //BotConstructor_Events events = new BotConstructor_Events();
                //events.

                // Create the bot
                BotConstruction_Result construction = BotConstructor.ConstructBot(dna, core, extra);
                Bot bot = new Bot(construction);

                dna = bot.GetNewDNA();

                BrainNEAT brainPart = bot.Parts.FirstOrDefault(o => o is BrainNEAT) as BrainNEAT;
                if (brainPart == null)
                {
                    throw new ApplicationException("There needs to be a brain part");
                }

                foreach (PartBase part in bot.Parts)
                {
                    if (part is SensorHoming partHoming)
                    {
                        partHoming.HomePoint = new Point3D(0, 0, 0);
                        partHoming.HomeRadius = HOMINGRADIUS;
                    }
                }

                int inputCount = brainPart.Neruons_Writeonly.Count();
                int outputCount = brainPart.Neruons_Readonly.Count();

                #endregion

                _keep2D.Add(bot, false);
                _map.AddItem(bot);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ArcBot1Player_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearScene();

                ArcBot player = new ArcBot(null, StaticRandom.Next(1, 30), new Point3D(0, 0, 0), _world, _map, _keep2D, _materialIDs, _viewport, _editorOptions, _itemOptions, _gravity, _dragPlane, new Point3D(0, 0, 0), 5, false, false);
                player.Ram.SetWPFStuff(grdViewPort, pnlVisuals2D);

                _map.AddItem(player);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ArcBot1NPC_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearScene();

                BotDNA dna = new BotDNA()
                {
                    UniqueID = Guid.NewGuid(),

                    //DraggingMaxVelocity = _player.DraggingBot.MaxVelocity.Value * .5,
                    //DraggingMultiplier = _player.DraggingBot.Multiplier * .5,

                    // this is from ArcBot.GetFinalDNA()
                    DraggingMaxVelocity = StaticRandom.NextPercent(10, .25),
                    DraggingMultiplier = StaticRandom.NextPercent(40, .25),

                    Parts = new ShipPartDNA[]
                    {
                        new SensorVisionDNA() { PartType = SensorVision.PARTTYPE, Position = new Point3D(0, 0, 1.5), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1), SearchRadius = _itemOptions.VisionSensor_SearchRadius },
                        new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(0, 0, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1) },
                        new ShipPartDNA() { PartType = MotionController2.PARTTYPE, Position = new Point3D(0, 0, -1.5), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1) },
                    }
                };

                ArcBotNPC npc = new ArcBotNPC(dna, StaticRandom.Next(1, 25), new Point3D(0, 0, 0), _world, _map, _keep2D, _materialIDs, _viewport, _editorOptions, _itemOptions, _gravity, _dragPlane, new Point3D(0, 0, 0), 5, true, true);

                _map.AddItem(npc);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void BotPrepFor2a_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearScene();

                #region dna

                // Create bot dna (remember the unique ID of the brainneat part)
                var parts = new List<ShipPartDNA>();

                // If there are more than one, give each a unique guid to know which to train (only want to train one at a time)
                parts.Add(new BrainNEATDNA()
                {
                    PartType = BrainNEAT.PARTTYPE,
                    Position = new Point3D(0, 0, 0),
                    Orientation = Quaternion.Identity,
                    Scale = new Vector3D(1, 1, 1)
                });

                parts.Add(new ImpulseEngineDNA()
                {
                    PartType = ImpulseEngine.PARTTYPE,
                    Position = new Point3D(0, 0, -.75),
                    Orientation = Quaternion.Identity,
                    Scale = new Vector3D(.4, .4, .4),
                    ImpulseEngineType = ImpulseEngineType.Translate
                });

                #region cargo parts

                // These go in a ring at a Z between the brain and the impulse

                const double RING_Z = -.6;
                const double CARGO_Z = -.4;     // since the cargo is so long, pull it up a bit
                Point3D pointTo = new Point3D(0, 0, -.52);      // this is used to rotate the parts slightly

                Point3D[] positions_cargo = Math3D.GetRandomVectors_Circular_EvenDist(3, .4).
                    Select(o => o.ToPoint3D(RING_Z)).
                    ToArray();

                // Make the cargo bay long, because it's storing pole arms
                Vector3D cargo_vect = new Point3D(0, 0, RING_Z) - positions_cargo[0];
                Vector3D cargo_vectX = Vector3D.CrossProduct(cargo_vect, new Vector3D(0, 0, 1));
                Vector3D cargo_vectY = pointTo - positions_cargo[0];

                parts.Add(new ShipPartDNA()
                {
                    PartType = CargoBay.PARTTYPE,
                    Position = new Point3D(positions_cargo[0].X * 1.4, positions_cargo[0].Y * 1.4, CARGO_Z),        // this stays at the same angle along the ring, but a larger radius
                    Orientation = Math3D.GetRotation(new DoubleVector(1, 0, 0, 0, 1, 0), new DoubleVector(cargo_vectX, cargo_vectY)),       // need a double vector because the cargo bay is a box.  The other two are cylinders, so it doesn't matter which way the orth vector is pointing
                    Scale = new Vector3D(.35, .15, 1.35)
                });

                parts.Add(new ShipPartDNA()
                {
                    PartType = EnergyTank.PARTTYPE,
                    Position = positions_cargo[1],
                    Orientation = Math3D.GetRotation(new Vector3D(0, 0, 1), pointTo - positions_cargo[1]),
                    Scale = new Vector3D(.6, .6, .15)
                });

                parts.Add(new ShipPartDNA()
                {
                    PartType = PlasmaTank.PARTTYPE,
                    Position = positions_cargo[2],
                    Orientation = Math3D.GetRotation(new Vector3D(0, 0, 1), pointTo - positions_cargo[2]),
                    Scale = new Vector3D(.6, .6, .15)
                });

                #endregion

                #region sensors

                Point3D[] positions_sensor = Math3D.GetRandomVectors_Cone_EvenDist(StaticRandom.Next(5, 10), new Vector3D(0, 0, 1), 40, 1.1, 1.1).
                    Select(o => o.ToPoint() + new Vector3D(0, 0, -.3)).
                    ToArray();

                foreach (Point3D pos in positions_sensor)
                {
                    parts.Add(new ShipPartDNA()
                    {
                        PartType = SensorGravity.PARTTYPE,
                        Position = pos,
                        Orientation = Quaternion.Identity,
                        Scale = new Vector3D(1, 1, 1)
                    });
                }

                //new ShipPartDNA() { PartType = SensorGravity.PARTTYPE, Position = new Point3D(sensorXY, sensorXY, sensorZ), Orientation = new Quaternion(halfSqrt2,0,0,halfSqrt2), Scale = new Vector3D(1, 1, 1) },
                //new ShipPartDNA() { PartType = SensorCollision.PARTTYPE, Position = new Point3D(-sensorXY, sensorXY, sensorZ), Orientation = new Quaternion(halfSqrt2,0,0,halfSqrt2), Scale = new Vector3D(1, 1, 1) },
                //new ShipPartDNA() { PartType = SensorHoming.PARTTYPE, Position = new Point3D(-sensorXY, -sensorXY, sensorZ), Orientation = new Quaternion(halfSqrt2,0,0,halfSqrt2), Scale = new Vector3D(1, 1, 1) },
                //new ShipPartDNA() { PartType = SensorVelocity.PARTTYPE, Position = new Point3D(sensorXY, -sensorXY, sensorZ), Orientation = new Quaternion(halfSqrt2,0,0,halfSqrt2), Scale = new Vector3D(1, 1, 1) },

                #endregion

                ShipDNA dna = ShipDNA.Create("bot", parts);

                #endregion

                #region ship extra args

                ShipExtraArgs extra = new ShipExtraArgs()
                {
                    Options = new EditorOptions(),
                    ItemOptions = new ItemOptionsArco(),
                };
                extra.Gravity = new GravityFieldUniform() { Gravity = new Vector3D(0, -((ItemOptionsArco)extra.ItemOptions).Gravity, 0) };



                // Old arcbot uses velocity.  Impulse engine currently uses force
                //
                // Impulse engine currently takes the max force from itemoptions, and it's readonly, set in the constructor
                // It need a way to be overridden, because each arcbot will have constant size parts, but need custem performance based on level

                //extra.ItemOptions.Impulse_LinearStrengthRatio = ;

                //retVal.DraggingMaxVelocity = retVal.DraggingMaxVelocity ?? StaticRandom.NextPercent(10, .25);
                //retVal.DraggingMultiplier = retVal.DraggingMultiplier ?? StaticRandom.NextPercent(40, .25);





                // So get rid of ImpulseEngine.  Keep using ArcBot's "MapObject_ChasePoint_Velocity DraggingBot" and ArcBotNPC's "AIMousePlate _aiMousePlate"
                // But instead of NPC being a derived class, do it all in one class.  If the player wants to take over a bot, that would be an override instead of a
                // hardcoded type change.  This would allow the player to hop from bot to bot instead of being permanently tied to one





                #endregion
                #region create bot

                // Need to create a bot that will wire up neurons, then save off that dna.  Otherwise, each room's bot will have a random
                // wiring, and the training will be meaningless

                ShipCoreArgs core = new ShipCoreArgs()
                {
                    World = _world,
                    Material_Ship = _materialIDs.Bot,
                    Map = _map,
                };

                //BotConstructor_Events events = new BotConstructor_Events();
                //events.

                // Create the bot
                BotConstruction_Result construction = BotConstructor.ConstructBot(dna, core, extra);
                Bot bot = new Bot(construction);

                dna = bot.GetNewDNA();

                BrainNEAT brainPart = bot.Parts.FirstOrDefault(o => o is BrainNEAT) as BrainNEAT;
                if (brainPart == null)
                {
                    throw new ApplicationException("There needs to be a brain part");
                }

                foreach (PartBase part in bot.Parts)
                {
                    if (part is SensorHoming partHoming)
                    {
                        partHoming.HomePoint = new Point3D(0, 0, 0);
                        partHoming.HomeRadius = HOMINGRADIUS;
                    }
                }

                int inputCount = brainPart.Neruons_Writeonly.Count();
                int outputCount = brainPart.Neruons_Readonly.Count();

                #endregion

                _keep2D.Add(bot, false);
                _map.AddItem(bot);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void BotPrepFor2b_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearScene();

                int level = StaticRandom.Next(1, 30);

                #region dna

                // Create bot dna (remember the unique ID of the brainneat part)
                var parts = new List<ShipPartDNA>();

                // If there are more than one, give each a unique guid to know which to train (only want to train one at a time)
                parts.Add(new BrainNEATDNA()
                {
                    PartType = BrainNEAT.PARTTYPE,
                    Position = new Point3D(0, 0, 0),
                    Orientation = Quaternion.Identity,
                    Scale = new Vector3D(1, 1, 1)
                });

                parts.Add(new ShipPartDNA()
                {
                    PartType = MotionController2.PARTTYPE,
                    Position = new Point3D(0, 0, -.65),
                    Orientation = Quaternion.Identity,
                    Scale = new Vector3D(.75, .75, .75)
                });

                #region cargo parts

                // These go in a ring around the brain

                const double RING_Z = 0;

                Point3D[] positions_cargo = Math3D.GetRandomVectors_Circular_EvenDist(3, .66).
                    Select(o => o.ToPoint3D(RING_Z)).
                    ToArray();

                Point3D pointTo = new Point3D(0, 0, RING_Z);

                // Make the cargo bay long, because it's storing pole arms
                Vector3D cargo_vect = pointTo - positions_cargo[0];
                Vector3D cargo_vectX = Vector3D.CrossProduct(cargo_vect, new Vector3D(0, 1, 0));
                Vector3D cargo_vectY = pointTo - positions_cargo[0];

                parts.Add(new ShipPartDNA()
                {
                    PartType = CargoBay.PARTTYPE,
                    Position = positions_cargo[0],
                    Orientation = Math3D.GetRotation(new DoubleVector(1, 0, 0, 0, 1, 0), new DoubleVector(cargo_vectX, cargo_vectY)),       // need a double vector because the cargo bay is a box.  The other two are cylinders, so it doesn't matter which way the orth vector is pointing
                    Scale = new Vector3D(.35, .15, 1.35)
                });

                parts.Add(new ShipPartDNA()
                {
                    PartType = EnergyTank.PARTTYPE,
                    Position = positions_cargo[1],
                    Orientation = Math3D.GetRotation(new Vector3D(0, 0, 1), pointTo - positions_cargo[1]),
                    Scale = new Vector3D(.6, .6, .15)
                });

                parts.Add(new ShipPartDNA()
                {
                    PartType = PlasmaTank.PARTTYPE,
                    Position = positions_cargo[2],
                    Orientation = Math3D.GetRotation(new Vector3D(0, 0, 1), pointTo - positions_cargo[2]),
                    Scale = new Vector3D(.6, .6, .15)
                });

                #endregion

                #region sensors

                Point3D[] positions_sensor = Math3D.GetRandomVectors_Cone_EvenDist(StaticRandom.Next(5, 10), new Vector3D(0, 0, 1), 40, 1.1, 1.1).
                    Select(o => o.ToPoint() + new Vector3D(0, 0, -.3)).
                    ToArray();

                foreach (Point3D pos in positions_sensor)
                {
                    parts.Add(new ShipPartDNA()
                    {
                        PartType = SensorGravity.PARTTYPE,
                        Position = pos,
                        Orientation = Quaternion.Identity,
                        Scale = new Vector3D(1, 1, 1)
                    });
                }

                //new ShipPartDNA() { PartType = SensorVision.PARTTYPE, Position = new Point3D(0, 0, 1.5), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1) },
                //new ShipPartDNA() { PartType = SensorGravity.PARTTYPE, Position = new Point3D(sensorXY, sensorXY, sensorZ), Orientation = new Quaternion(halfSqrt2,0,0,halfSqrt2), Scale = new Vector3D(1, 1, 1) },
                //new ShipPartDNA() { PartType = SensorCollision.PARTTYPE, Position = new Point3D(-sensorXY, sensorXY, sensorZ), Orientation = new Quaternion(halfSqrt2,0,0,halfSqrt2), Scale = new Vector3D(1, 1, 1) },
                //new ShipPartDNA() { PartType = SensorHoming.PARTTYPE, Position = new Point3D(-sensorXY, -sensorXY, sensorZ), Orientation = new Quaternion(halfSqrt2,0,0,halfSqrt2), Scale = new Vector3D(1, 1, 1) },
                //new ShipPartDNA() { PartType = SensorVelocity.PARTTYPE, Position = new Point3D(sensorXY, -sensorXY, sensorZ), Orientation = new Quaternion(halfSqrt2,0,0,halfSqrt2), Scale = new Vector3D(1, 1, 1) },

                #endregion

                ShipDNA dna = ShipDNA.Create("bot", parts);

                #endregion

                #region ship extra args

                ShipExtraArgs extra = new ShipExtraArgs()
                {
                    Options = new EditorOptions(),
                    ItemOptions = new ItemOptionsArco(),
                };
                extra.Gravity = new GravityFieldUniform() { Gravity = new Vector3D(0, -((ItemOptionsArco)extra.ItemOptions).Gravity, 0) };



                // Old arcbot uses velocity.  Impulse engine currently uses force
                //
                // Impulse engine currently takes the max force from itemoptions, and it's readonly, set in the constructor
                // It need a way to be overridden, because each arcbot will have constant size parts, but need custem performance based on level

                //extra.ItemOptions.Impulse_LinearStrengthRatio = ;

                //retVal.DraggingMaxVelocity = retVal.DraggingMaxVelocity ?? StaticRandom.NextPercent(10, .25);
                //retVal.DraggingMultiplier = retVal.DraggingMultiplier ?? StaticRandom.NextPercent(40, .25);





                // So get rid of ImpulseEngine.  Keep using ArcBot's "MapObject_ChasePoint_Velocity DraggingBot" and ArcBotNPC's "AIMousePlate _aiMousePlate"
                // But instead of NPC being a derived class, do it all in one class.  If the player wants to take over a bot, that would be an override instead of a
                // hardcoded type change.  This would allow the player to hop from bot to bot instead of being permanently tied to one





                #endregion
                #region create bot

                // Need to create a bot that will wire up neurons, then save off that dna.  Otherwise, each room's bot will have a random
                // wiring, and the training will be meaningless

                ShipCoreArgs core = new ShipCoreArgs()
                {
                    World = _world,
                    Material_Ship = _materialIDs.Bot,
                    Map = _map,
                };

                BotConstructor_Events events = new BotConstructor_Events
                {
                    InstantiateUnknownPart_Standard = ArcBot2.InstantiateUnknownPart_Standard,
                };

                object[] botObjects = new object[]
                {
                    new ArcBot2_ConstructionProps()
                    {
                        DragPlane = _dragPlane,
                        Level = level,
                    },
                };

                // Create the bot
                BotConstruction_Result construction = BotConstructor.ConstructBot(dna, core, extra, events, botObjects);
                Bot bot = new Bot(construction);

                dna = bot.GetNewDNA();

                BrainNEAT brainPart = bot.Parts.FirstOrDefault(o => o is BrainNEAT) as BrainNEAT;
                if (brainPart == null)
                {
                    throw new ApplicationException("There needs to be a brain part");
                }

                foreach (PartBase part in bot.Parts)
                {
                    if (part is SensorHoming partHoming)
                    {
                        partHoming.HomePoint = new Point3D(0, 0, 0);
                        partHoming.HomeRadius = HOMINGRADIUS;
                    }
                }

                int inputCount = brainPart.Neruons_Writeonly.Count();
                int outputCount = brainPart.Neruons_Readonly.Count();

                #endregion

                _keep2D.Add(bot, false);
                _map.AddItem(bot);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ArcBot2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearScene();

                int level = StaticRandom.Next(1, 30);

                var extra = ArcBot2.GetArgs_Extra();

                ShipCoreArgs core = new ShipCoreArgs()
                {
                    World = _world,
                    Material_Ship = _materialIDs.Bot,
                    Map = _map,
                };

                var construction = ArcBot2.GetConstruction(level, core, extra, _dragPlane, false);

                ArcBot2 bot = new ArcBot2(construction, _materialIDs, _keep2D, _viewport);

                _keep2D.Add(bot, false);
                _map.AddItem(bot);

                UpdateNeuralViewer(bot);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void LotsOfBots_Rand_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearScene();

                int level = StaticRandom.Next(1, 30);

                var extra = ArcBot2.GetArgs_Extra();

                ShipCoreArgs core = new ShipCoreArgs()
                {
                    World = _world,
                    Material_Ship = _materialIDs.Bot,
                    Map = _map,
                };

                foreach (var cell in Math2D.GetCells(3, 7, 7))
                {
                    var construction = ArcBot2.GetConstruction(level, core, extra, _dragPlane, false);

                    ArcBot2 bot = new ArcBot2(construction, _materialIDs, _keep2D, _viewport);
                    bot.PhysicsBody.Position = cell.center.ToPoint3D();

                    _keep2D.Add(bot, false);
                    _map.AddItem(bot);

                    //UpdateNeuralViewer(bot);
                }

                Background = new SolidColorBrush(UtilityWPF.GetRandomColor(120, 200));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void LotsOfBots_Same_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var cells = Math2D.GetCells(3, 7, 7);






            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PlaceParts_Click(object sender, RoutedEventArgs e)
        {
            const double LINE = .005;

            try
            {
                Debug3DWindow window = new Debug3DWindow();

                window.AddAxisLines(3, LINE);

                // Brain
                window.AddDot(new Point3D(0, 0, 0), .5, Colors.HotPink);

                // Impulse
                window.AddDot(new Point3D(0, 0, -.75), .2, Colors.DarkSlateGray);





                Point3D[] positions_cargo = Math3D.GetRandomVectors_Circular_EvenDist(3, .4).
                    Select(o => o.ToPoint3D(-.6)).
                    ToArray();

                Point3D pointTo = new Point3D(0, 0, -.45);

                // Cargo
                window.AddDot(positions_cargo[0], .075, Colors.DarkGreen);
                window.AddLine(positions_cargo[0], pointTo, LINE, Colors.DarkGreen);

                // Energy
                window.AddDot(positions_cargo[1], .075, Colors.DodgerBlue);
                window.AddLine(positions_cargo[1], pointTo, LINE, Colors.DodgerBlue);

                // Plasma
                window.AddDot(positions_cargo[2], .075, Colors.DarkOrchid);
                window.AddLine(positions_cargo[2], pointTo, LINE, Colors.DarkOrchid);




                // Sensors
                //Point3D[] positions_sensor = Math3D.GetRandomVectors_Cone_EvenDist(7, new Vector3D(0, 0, 1), 80, 1, 1).
                //    Select(o => o.ToPoint()).
                //    ToArray();

                //Point3D[] positions_sensor = Math3D.GetRandomVectors_Cone_EvenDist(7, new Vector3D(0, 0, 1), 90, .8, .8).
                //    Select(o => o.ToPoint() + new Vector3D(0,0,.2)).
                //    ToArray();

                Point3D[] positions_sensor = Math3D.GetRandomVectors_Cone_EvenDist(StaticRandom.Next(5, 10), new Vector3D(0, 0, 1), 40, 1.5, 1.5).
                    Select(o => o.ToPoint() + new Vector3D(0, 0, -.5)).
                    ToArray();

                window.AddDots(positions_sensor, .25, Colors.DimGray);





                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void VisionNeurons_Click(object sender, RoutedEventArgs e)
        {
            const double DOT = .01;
            const double LINE = .0035;

            try
            {
                Debug3DWindow window = null;

                bool hasHoleInMiddle = true;

                double radius = .5;

                int numLayers = 4;
                int neuronCount = 60;       //300;

                var neuronZs = GetNeuron_Zs(numLayers, radius);

                #region initial points 2D

                Point3D[] staticPositions = null;
                if (hasHoleInMiddle)
                {
                    staticPositions = new Point3D[] { new Point3D(0, 0, 0) };
                }

                Vector3D[] positions_initial2D = NeuralUtility.GetNeuronPositions_Circular_Even(null, staticPositions, 1d, neuronCount, radius);

                window = new Debug3DWindow()
                {
                    Title = "Initial Positions",
                };

                window.AddAxisLines(1, LINE);

                window.AddDots(positions_initial2D, DOT, Colors.White);

                window.Show();

                #endregion

                #region create layers

                Vector3D[][] positions_plates = neuronZs.z.
                    Select(o => positions_initial2D.Select(p => new Vector3D(p.X, p.Y, o)).ToArray()).
                    ToArray();

                window = new Debug3DWindow()
                {
                    Title = "Plates",
                };

                window.AddAxisLines(1, LINE);

                window.AddDots(positions_plates.SelectMany(o => o), DOT, Colors.White);

                window.Show();

                #endregion

                #region mutate points

                Vector3D[] allPositions_preMutate = positions_plates.
                    SelectMany(o => o).
                    ToArray();

                Vector3D[] allPositions_postMutate = MutateUtility.MutateList(allPositions_preMutate, new MutateUtility.MuateArgs(false, .5, .25));
                //Vector3D[] allPositions_postMutate = MutateUtility.MutateList(allPositions_preMutate, new MutateUtility.MuateArgs(false, 1, .75));

                window = new Debug3DWindow()
                {
                    Title = "Mutated",
                };

                window.AddAxisLines(1, LINE);

                window.AddDots(allPositions_preMutate, DOT, Colors.White);
                window.AddDots(allPositions_postMutate, DOT, Colors.Red);

                window.Show();

                #endregion

                #region divide into layers

                //var byLayer = SeparateNeuronsByLayers(allPositions_postMutate, neuronZs.z);
                var byLayer = NeuralUtility.DivideNeuronLayersIntoSheets(allPositions_postMutate.Select(o => o.ToPoint()).ToArray(), neuronZs.z, neuronCount);

                window = new Debug3DWindow()
                {
                    Title = "By Layer",
                };

                window.AddAxisLines(1, LINE);

                window.AddDots(byLayer.SelectMany(o => o), DOT, Colors.White);

                foreach (var layer in byLayer)
                {
                    window.AddText(layer.Length.ToString());
                }

                window.Show();

                #endregion

                #region repaired points 2D

                // just use the first layer's positions to re evenly distribute (or the first layer with the correct number of neurons - or closest number)
                // could try to find the nearest between each layer and use the average of each set, but that's a lot of expense with very little payoff
                var repair_pre_test = byLayer.
                    Where(o => o.Length == neuronCount).
                    Select(o => new
                    {
                        layer = o,
                        avg_stddev = Math1D.Get_Average_StandardDeviation(o.Select(p => p.Z)),
                    }).
                    OrderBy(o => o.avg_stddev.Item2).
                    ToArray();

                // Look for an exact count match
                //NOTE: NeuralUtility.DivideNeuronLayersIntoSheets redistributes so that each layer has count.  So while getting first layer with count will work, taking it a step farther by finding the layer with the lowest spread
                //var repair_pre = byLayer.FirstOrDefault(o => o.Length == neuronCount);
                var repair_pre = byLayer.
                    Where(o => o.Length == neuronCount).
                    Select(o => new
                    {
                        layer = o,
                        avg_stddev = Math1D.Get_Average_StandardDeviation(o.Select(p => p.Z)),
                    }).
                    OrderBy(o => o.avg_stddev.Item2).
                    Select(o => o.layer).
                    FirstOrDefault();

                if (repair_pre == null)
                {
                    // Look for the smallest layer with more than count
                    repair_pre = byLayer.
                        Where(o => o.Length > neuronCount).
                        OrderBy(o => o.Length).
                        FirstOrDefault();

                    if (repair_pre == null)
                    {
                        // Look for the largest layer with less than count
                        repair_pre = byLayer.
                            Where(o => o.Length < neuronCount).      // this where is unnecessary, just putting it here for completeness (at this point, all layers are less than count
                            OrderByDescending(o => o.Length).
                            FirstOrDefault();
                    }

                    if (repair_pre == null)
                    {
                        throw new ApplicationException("byLayer is empty (this should never happen)");
                    }
                }

                var positions_repaired2D = NeuralUtility.GetNeuronPositions_Circular_Even(repair_pre, staticPositions, 1d, neuronCount, radius);

                window = new Debug3DWindow()
                {
                    Title = "Repaired Positions",
                };

                window.AddAxisLines(1, LINE);

                window.AddDots(positions_initial2D, DOT, Colors.White);
                window.AddDots(repair_pre.Select(o => o.ToPoint2D().ToPoint3D()), DOT, Colors.HotPink);
                window.AddDots(positions_repaired2D, DOT, Colors.Chartreuse);

                window.Show();

                #endregion

                #region final layers

                Vector3D[][] positions_plates_final = neuronZs.z.
                    Select(o => positions_repaired2D.Select(p => new Vector3D(p.X, p.Y, o)).ToArray()).
                    ToArray();

                window = new Debug3DWindow()
                {
                    Title = "Plates - Final",
                };

                window.AddAxisLines(1, LINE);

                window.AddDots(positions_plates_final.SelectMany(o => o), DOT, Colors.White);

                window.Show();

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void VisionMutate_Click(object sender, RoutedEventArgs e)
        {
            // create a vision sensor with neurons

            // get the dna, mutate, create a child

            // repeat a few times.  sometimes with mild mutation, sometimes with high mutation
        }
        private void VisionFar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SensorVisionDNA part = new SensorVisionDNA()
                {
                    PartType = SensorVision.PARTTYPE,
                    Position = new Point3D(0, 0, 0),
                    Orientation = Quaternion.Identity,
                    Scale = new Vector3D(1, 1, 1),
                    SearchRadius = 20,
                };

                CreateVisionTest(part);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void VisionFar_Filters_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SensorVisionDNA part = new SensorVisionDNA()
                {
                    PartType = SensorVision.PARTTYPE,
                    Position = new Point3D(0, 0, 0),
                    Orientation = Quaternion.Identity,
                    Scale = new Vector3D(1, 1, 1),
                    SearchRadius = 20,
                    Filters = new[]
                    {
                        SensorVisionFilterType.Bot_Other,
                        SensorVisionFilterType.TreasureBox,
                        SensorVisionFilterType.Weapon_FreeFloating,
                        SensorVisionFilterType.Weapon_Attached_Other,
                        //SensorVisionFilterType.Weapon_Attached_Personal,
                    },
                };

                CreateVisionTest(part);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void VisionAttachedWeapon_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SensorVisionDNA part = new SensorVisionDNA()
                {
                    PartType = SensorVision.PARTTYPE,
                    Position = new Point3D(0, 0, 0),
                    Orientation = Quaternion.Identity,
                    Scale = new Vector3D(1, 1, 1),
                    SearchRadius = 20,
                    Filters = new[]
                    {
                        SensorVisionFilterType.Bot_Other,
                        SensorVisionFilterType.Weapon_Attached_Other,
                        SensorVisionFilterType.Weapon_Attached_Personal,
                    },
                };

                ArcBot2 bot = CreateVisionTest(part);

                Weapon weapon = new Weapon(WeaponDNA.GetRandomDNA(), new Point3D(), _world, _materialIDs.Weapon);

                bot.AttachWeapon(weapon);

                weapon.Gravity = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void VisionLayers_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //TODO: The dna should have neuron density property (or at least a multiplier)
                SensorVisionDNA partFar = new SensorVisionDNA()
                {
                    PartType = SensorVision.PARTTYPE,
                    Position = new Point3D(0, 0, 0),
                    Orientation = Quaternion.Identity,
                    Scale = new Vector3D(1, 1, 1),
                    SearchRadius = 60,
                    Filters = new[]
                    {
                        SensorVisionFilterType.Nest,
                        SensorVisionFilterType.Bot_Family,
                        SensorVisionFilterType.Bot_Other,
                        SensorVisionFilterType.TreasureBox,
                        SensorVisionFilterType.Weapon_FreeFloating,
                    },
                };

                SensorVisionDNA partNear = new SensorVisionDNA()
                {
                    PartType = SensorVision.PARTTYPE,
                    Position = new Point3D(0, 0, 0),
                    Orientation = Quaternion.Identity,
                    Scale = new Vector3D(1, 1, 1),
                    SearchRadius = 12,
                    Filters = new[]
                    {
                        SensorVisionFilterType.Bot_Family,
                        SensorVisionFilterType.Bot_Other,
                        SensorVisionFilterType.Weapon_Attached_Personal,
                        SensorVisionFilterType.Weapon_Attached_Other,
                    },
                };

                CreateVisionTest(new[] { partFar, partNear });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetPos_CreateBot_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearScene();

                int level = StaticRandom.Next(1, 30);

                var extra = ArcBot2.GetArgs_Extra();
                ((GravityFieldUniform)extra.Gravity).Gravity *= .03;

                ShipCoreArgs core = new ShipCoreArgs()
                {
                    World = _world,
                    Material_Ship = _materialIDs.Bot,
                    Map = _map,
                };

                var construction = ArcBot2.GetConstruction(level, core, extra, _dragPlane, false);
                ArcBot2 bot = new ArcBot2(construction, _materialIDs, _keep2D, _viewport);

                Weapon weapon = new Weapon(WeaponDNA.GetRandomDNA(), new Point3D(), _world, _materialIDs.Weapon);
                bot.AttachWeapon(weapon);

                _keep2D.Add(bot, false);
                _map.AddItem(bot);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void SetPos_SpinWeapon_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ArcBot2 bot = SetPosition_GetArcBot();
                if (bot == null)
                {
                    return;
                }

                bot.Weapon.PhysicsBody.AngularVelocity = new Vector3D(0, 0, StaticRandom.NextDouble(-10, 10));
                //bot.Weapon.PhysicsBody.Velocity = Math3D.GetRandomVector_Circular(10);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void SetPos_StopWeapon_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ArcBot2 bot = SetPosition_GetArcBot();
                if (bot == null)
                {
                    return;
                }

                // It takes several clicks before this stops it
                //bot.Weapon.PhysicsBody.AngularVelocity = new Vector3D();

                WeaponDNA wepDNA = bot.Weapon.DNA;

                bot.AttachWeapon(null);
                bot.AttachWeapon(new Weapon(wepDNA, new Point3D(), _world, _materialIDs.Weapon));

                bot.PhysicsBody.Velocity = new Vector3D();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void SetPos_Near_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ArcBot2 bot = SetPosition_GetArcBot();
                if (bot == null)
                {
                    return;
                }

                Vector3D offset = Math3D.GetRandomVector_Circular_Shell(2);

                if (chkSetPos_StopBot.IsChecked.Value)
                {
                    //SetPosition_StopBot_ResetWeapon(bot);
                    bot.SetPosition(bot.PositionWorld + offset, !chkSetPos_StopWeapon.IsChecked.Value);
                }
                else
                {
                    bot.PhysicsBody.Position += offset;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void SetPos_Far_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ArcBot2 bot = SetPosition_GetArcBot();
                if (bot == null)
                {
                    return;
                }

                Vector3D offset = Math3D.GetRandomVector_Circular_Shell(12);

                if (chkSetPos_StopBot.IsChecked.Value)
                {
                    //SetPosition_StopBot_ResetWeapon(bot);
                    bot.SetPosition(bot.PositionWorld + offset, !chkSetPos_StopWeapon.IsChecked.Value);
                }
                else
                {
                    bot.PhysicsBody.Position += offset;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //TODO: Fix StopRadiusPercent - make a different button for that that also varies the other variables
        private void BrainNEATNeuronPositions_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // When profiling LotsOfBots_Rand_Click the biggest expense was,
                //      BrainNEAT.GetNeuronPositions calling
                //      Math3D.GetRandomVectors_Cone_EvenDist

                int count = 230;
                Vector3D axis = new Vector3D(0, 0, 1);
                double maxAngle = 83;
                double radius = .5;
                int iterationCount = 1000;      // 1000 was 1800 milliseconds
                //int iterationCount = 24;        // 12 is the sweet spot for this case

                var sizes = Debug3DWindow.GetDrawSizes(2);

                //Math3D.GetRandomVectors_Cone_EvenDist(count, axis, maxAngle, radius, radius, stopIterationCount: 1);
                //return;


                // Test different values to see what's good enough
                while (iterationCount > 1)
                {
                    Debug3DWindow window = new Debug3DWindow()
                    {
                        Title = iterationCount.ToString(),
                        Background = Brushes.SlateGray,
                    };

                    foreach (var cell in Math2D.GetCells(2, 2, 2))
                    {
                        DateTime startTime = DateTime.UtcNow;

                        Vector3D[] points = Math3D.GetRandomVectors_Cone_EvenDist(count, axis, maxAngle, radius, radius, stopIterationCount: iterationCount);

                        double elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;

                        window.AddDots(points.Select(o => cell.center.ToPoint3D() + o), sizes.dot, Colors.White);
                        window.AddText(elapsed.ToStringSignificantDigits(3));
                    }

                    window.Show();

                    iterationCount /= 2;

                    break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ConeStopPercent_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //ideal values seem to be:
                //% = .18
                //count = 100

                Random rand = StaticRandom.GetRandomForThread();

                for (int cntr = 0; cntr < 6; cntr++)
                //for (int cntr = 0; cntr < 4; cntr++)
                {
                    int count = rand.Next(10, 300);
                    Vector3D axis = new Vector3D(0, 0, 1);
                    double maxAngle = rand.Next(10, 170);
                    double radius = .5;

                    //Vector3D[] points = Math3D.GetRandomVectors_Cone_EvenDist(count, axis, maxAngle, radius, radius, stopRadiusPercent: .02, stopIterationCount: 1000);
                    Vector3D[] points = Math3D.GetRandomVectors_Cone_EvenDist(count, axis, maxAngle, radius, radius, stopRadiusPercent: 0, stopIterationCount: 12);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ConeLinkDistances_Click(object sender, RoutedEventArgs e)
        {
            //TODO: also account for repulse multipliers (probably use a checkbox so that runs can be compared 1:1 vs repulse)

            // The purpose of this button is to make an estimate function.  Graph experimental values to come up with a good estimation function
            // estimateAvgPerVolume(int count, double volume)

            try
            {
                ConeResult[] results = Enumerable.Range(0, 50).
                    AsParallel().
                    Select(o => MakeCone()).
                    ToArray();

                #region getGraphs

                var getGraphs = new Func<ConeResult[], GraphResult[]>(r =>
                {
                    return new[]
                    {
                        Debug3DWindow.GetGraph(r.Select(o => o.RadiusMin).ToArray(), "RadiusMin"),
                        Debug3DWindow.GetGraph(r.Select(o => o.RadiusMax).ToArray(), "RadiusMax"),
                        Debug3DWindow.GetGraph(r.Select(o => o.RadiusMax - o.RadiusMin).ToArray(), "Radius Difference"),
                        Debug3DWindow.GetGraph(r.Select(o => o.Angle).ToArray(), "Angle"),

                        Debug3DWindow.GetGraph(r.Select(o => o.Volume).ToArray(), "Volume"),
                        Debug3DWindow.GetGraph(r.Select(o => o.Count.ToDouble()).ToArray(), "Count"),
                        Debug3DWindow.GetGraph(r.Select(o => o.AvgPairLength).ToArray(), "AvgPairLength"),

                        //Debug3DWindow.GetGraph(r.Select(o => o.Count_per_Volume).ToArray(), "Count_per_Volume"),
                        //Debug3DWindow.GetGraph(r.Select(o => o.Avg_per_Volume).ToArray(), "Avg_per_Volume"),

                        Debug3DWindow.GetGraph(r.Select(o => o.Volume_per_Count).ToArray(), "Volume_per_Count"),
                        //Debug3DWindow.GetGraph(r.Select(o => o.Volume_per_Avg).ToArray(), "Volume_per_Avg"),

                        // This gets pretty close, but it's still spiky compared to the computed average.  That spikiness lines up with radius difference.  So the constants seem to be different between a 2D surface area and a 3D volume
                        Debug3DWindow.GetGraph(r.Select(o => Math.Pow( o.Volume_per_Count, .3333)).ToArray(), "Volume_per_Count^1/3"),
                    };
                });

                #endregion

                #region Avg_per_Volume

                //var sorted = results.
                //    OrderBy(o => o.Avg_per_Volume).
                //    ToArray();

                //Debug3DWindow window = new Debug3DWindow()
                //{
                //    Title = "Avg_per_Volume",
                //};

                //window.AddGraphs(getGraphs(sorted), new Point3D(), 1);

                //window.Show();

                #endregion
                #region AvgPairLength

                var sorted = results.
                    OrderBy(o => o.AvgPairLength).
                    ToArray();

                Debug3DWindow window = new Debug3DWindow()
                {
                    Title = "AvgPairLength",
                };

                window.AddGraphs(getGraphs(sorted), new Point3D(), 1, showHotTrackLines: true);

                window.AddText($"Avg Min: {results.Min(o => o.AvgPairLength)}");
                window.AddText($"Avg Max: {results.Max(o => o.AvgPairLength)}");
                window.AddText("");
                window.AddText($"Vol_per_Count Min: {results.Min(o => o.Volume_per_Count)}");
                window.AddText($"Vol_per_Count Max: {results.Max(o => o.Volume_per_Count)}");

                window.Show();

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void GenerateConeData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_shouldStopGeneratingCones)
                {
                    MessageBox.Show("Need to stop the current run first", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_conestatsFile != null)
                {
                    _conestatsFile.Dispose();
                    throw new ApplicationException("File still open");
                }

                string filename = Path.Combine(UtilityCore.GetOptionsFolder(), OPTIONSFOLDER);
                Directory.CreateDirectory(filename);
                filename = Path.Combine(filename, $"{DateTime.Now.ToString("yyyyMMdd HHmmss")} - Cone Stats.txt");

                _conestatsFile = new StreamWriter(filename, false);

                StringBuilder line = new StringBuilder();

                line.Append("Count");
                line.Append("\t");
                line.Append("Angle");
                line.Append("\t");
                line.Append("RadiusMin");
                line.Append("\t");
                line.Append("RadiusMax");
                line.Append("\t");
                line.Append("RadiusMax - RadiusMin");
                line.Append("\t");
                line.Append("Volume");
                line.Append("\t");
                line.Append("Volume_per_Count");
                line.Append("\t");
                line.Append("Volume_per_Count^.33");
                line.Append("\t");
                line.Append("AvgPairLength");

                _conestatsFile.WriteLine(line.ToString());

                if (_coneTimer == null)
                {
                    _coneTimer = new DispatcherTimer();
                    _coneTimer.Interval = TimeSpan.FromMilliseconds(1);
                    _coneTimer.Tick += ConeTimer_Tick;
                }

                btnGenerateCones.IsEnabled = false;
                btnStopGeneratingCones.IsEnabled = true;

                _shouldStopGeneratingCones = false;

                _coneTimer.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void StopConeGeneration_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _shouldStopGeneratingCones = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ConeTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (_conestatsFile == null)
                {
                    return;     // should never happen
                }

                if (_shouldStopGeneratingCones)
                {
                    _coneTimer.IsEnabled = false;
                    _conestatsFile.Close();
                    _conestatsFile = null;
                    btnStopGeneratingCones.IsEnabled = false;
                    btnGenerateCones.IsEnabled = true;
                    return;
                }

                ConeResult results = MakeCone(10);

                StringBuilder line = new StringBuilder();

                line.Append(results.Count);
                line.Append("\t");
                line.Append(results.Angle);
                line.Append("\t");
                line.Append(results.RadiusMin);
                line.Append("\t");
                line.Append(results.RadiusMax);
                line.Append("\t");
                line.Append(results.RadiusMax - results.RadiusMin);
                line.Append("\t");
                line.Append(results.Volume);
                line.Append("\t");
                line.Append(results.Volume_per_Count);
                line.Append("\t");
                line.Append(Math.Pow(results.Volume_per_Count, .333333));
                line.Append("\t");
                line.Append(results.AvgPairLength);

                _conestatsFile.WriteLine(line.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        private bool _shouldStopGeneratingCones = true;
        private StreamWriter _conestatsFile = null;
        DispatcherTimer _coneTimer = null;

        #region Private Methods

        private void ClearScene()
        {
            _map.Clear();

            foreach (SensorVisionViewer viewer in _sensorViewers)
            {
                viewer.Dispose();
            }
            _sensorViewers.Clear();

            if (_viewer != null)
            {
                _viewerPos = new Point(_viewer.Left, _viewer.Top);
                _viewer.Close();
                _viewer = null;
            }
        }
        private void UpdateNeuralViewer(Bot bot)
        {
            if (_viewer != null)
            {
                _viewerPos = new Point(_viewer.Left, _viewer.Top);
                _viewer.Close();
            }

            _viewer = new ShipViewerWindow(bot, this);
            _viewer.SetColorTheme_Light();

            if (_viewerPos != null)
            {
                _viewer.Left = _viewerPos.Value.X;
                _viewer.Top = _viewerPos.Value.Y;
            }

            _viewer.Closed += Viewer_Closed;

            _viewer.Show();
        }
        private void UpdateVisionViewers(Bot bot)
        {
            foreach (SensorVisionViewer viewer in _sensorViewers)
            {
                viewer.Dispose();
            }
            _sensorViewers.Clear();

            foreach (SensorVision sensor in bot.Parts.Where(o => o is SensorVision))
            {
                _sensorViewers.Add(new SensorVisionViewer(sensor, _viewport, _map) { ShowTempVisuals = true });
            }
        }

        private ArcBot2 CreateVisionTest(SensorVisionDNA part)
        {
            return CreateVisionTest(new[] { part });
        }
        private ArcBot2 CreateVisionTest(SensorVisionDNA[] parts)
        {
            ClearScene();

            ShipDNA dna = ShipDNA.Create("vision", parts);

            ShipExtraArgs extra = ArcBot2.GetArgs_Extra();

            ShipCoreArgs core = new ShipCoreArgs()
            {
                World = _world,
                Material_Ship = _materialIDs.Bot,
                Map = _map,
            };

            BotConstructor_Events events = new BotConstructor_Events
            {
                InstantiateUnknownPart_Standard = ArcBot2.InstantiateUnknownPart_Standard,
            };

            object[] botObjects = new object[]
            {
                new ArcBot2_ConstructionProps()
                {
                    DragPlane = _dragPlane,
                    Level = 1,
                },
            };

            BotConstruction_Result construction = BotConstructor.ConstructBot(dna, core, extra, events, botObjects);
            ArcBot2 bot = new ArcBot2(construction, _materialIDs, _keep2D, _viewport);

            //NOTE: Setting the token is done in ArcBot, will need to be implemented in ArcBot2, but this test is just using Bot in order to keep things simple
            foreach (SensorVision vision in bot.Parts.Where(o => o is SensorVision))
            {
                vision.BotToken = bot.Token;
            }

            bot.PhysicsBody.Velocity = Math3D.GetRandomVector_Spherical(2);
            bot.PhysicsBody.AngularVelocity = Math3D.GetRandomVector_Spherical(6);

            _keep2D.Add(bot, false);
            _map.AddItem(bot);

            //UpdateNeuralViewer(bot);      // the shipviewer is just obnoxious and not a very good visualization
            UpdateVisionViewers(bot);

            // Place other items in the map

            double minRadius = 1.5;
            double maxRadius = 25;
            Point3D center = new Point3D();

            CreateTreasureBoxes(4, center, minRadius, maxRadius);
            CreateWeapons(4, center, minRadius, maxRadius);
            CreateNPCNests(1, center, minRadius, maxRadius);

            return bot;
        }

        // Copied from ArcanorumWindow
        private void CreateTreasureBoxes(int count, Point3D center, double minRadius, double maxRadius)
        {
            for (int cntr = 0; cntr < count; cntr++)
            {
                object[] treasure = null;
                if (StaticRandom.NextDouble() > .5d)
                {
                    treasure = new object[] { WeaponDNA.GetRandomDNA() };
                }

                TreasureBox box = new TreasureBox(center + Math3D.GetRandomVector_Circular(minRadius, maxRadius), 1000, 100, _materialIDs.TreasureBox, _world, new DamageProps(), treasure);
                box.PhysicsBody.AngularVelocity = Math3D.GetRandomVector_Spherical(6);

                _keep2D.Add(box, false);
                _map.AddItem(box);
            }
        }
        private void CreateWeapons(int count, Point3D center, double minRadius, double maxRadius)
        {
            for (int cntr = 0; cntr < count; cntr++)
            {
                Weapon weapon = new Weapon(WeaponDNA.GetRandomDNA(), center + Math3D.GetRandomVector_Circular(minRadius, maxRadius), _world, _materialIDs.Weapon);
                weapon.PhysicsBody.AngularVelocity = Math3D.GetRandomVector_Spherical(6);

                _keep2D.Add(weapon, false);
                _map.AddItem(weapon);
            }
        }
        private void CreateNPCNests(int count, Point3D center, double minRadius, double maxRadius)
        {
            const double UNDERITEM_RADIUS = 1d;
            const double UNDERITEM_Z = -5;

            for (int cntr = 0; cntr < count; cntr++)
            {
                #region create nest

                IMapObject[] existing = _map.GetAllItems().
                    Where(o => o is NPCNest || o is PortalVisual).
                    ToArray();

                // Come up with a position that isn't too close to others
                Point3D position;
                do
                {
                    Vector3D vect = Math3D.GetRandomVector_Circular(minRadius, maxRadius);
                    position = new Point3D(vect.X, vect.Y, UNDERITEM_Z);

                    if (!existing.Any(o => (o.PositionWorld - position).Length < (UNDERITEM_RADIUS + o.Radius) * 7))
                    {
                        break;
                    }
                } while (true);

                NPCNest nest = new NPCNest(null, UNDERITEM_RADIUS, _world, _map, _keep2D, _materialIDs, _viewport, _editorOptions, _itemOptions, _gravity, _dragPlane)
                {
                    PositionWorld = position,
                };

                _map.AddItem(nest);

                #endregion
            }
        }

        #endregion
        #region Private Methods - vision neurons

        private static List<Vector3D>[] SeparateNeuronsByLayers(Vector3D[] positions, double[] plateZs)
        {
            List<Vector3D>[] retVal = Enumerable.Range(0, plateZs.Length).
                Select(o => new List<Vector3D>()).
                ToArray();

            foreach (Vector3D position in positions)
            {
                // Get the plate that this is closest to
                int index = GetNearest(plateZs, position.Z);

                // Add to that plate
                retVal[index].Add(new Vector3D(position.X, position.Y, plateZs[index]));
            }

            return retVal;
        }
        private static int GetNearest(double[] values, double test)
        {
            int retVal = -1;
            double minDist = double.MaxValue;

            for (int cntr = 0; cntr < values.Length; cntr++)
            {
                double dist = Math.Abs(test - values[cntr]);

                if (dist < minDist)
                {
                    retVal = cntr;
                    minDist = dist;
                }
            }

            if (retVal < 0)
            {
                throw new ApplicationException("Didn't find an index");
            }

            return retVal;
        }

        private static (double[] z, double gap) GetNeuron_Zs(int numPlates, double radius)
        {
            if (numPlates == 1)
            {
                return (new double[] { 0d }, 0);
            }

            // I don't want the plate's Z to go all the way to the edge of radius, so suck it in a bit
            double max = radius * .75d;

            double gap = (max * 2d) / Convert.ToDouble(numPlates - 1);		// multiplying by 2 because radius is only half

            double[] retVal = new double[numPlates];
            double current = max * -1d;

            for (int cntr = 0; cntr < numPlates; cntr++)
            {
                retVal[cntr] = current;
                current += gap;
            }

            return (retVal, gap);
        }

        #endregion
        #region Private Methods - set position

        private ArcBot2 SetPosition_GetArcBot()
        {
            ArcBot2[] arcBots = _map.GetItems<ArcBot2>(true).ToArray();

            if (arcBots.Length == 0)
            {
                MessageBox.Show("Create a bot first", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }
            else if (arcBots.Length > 1)
            {
                MessageBox.Show("There should only be one bot", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }
            else if (arcBots[0].Weapon == null)
            {
                MessageBox.Show("Create a bot with a weapon first", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            return arcBots[0];
        }

        private void SetPosition_StopBot_ResetWeapon(ArcBot2 bot)
        {
            WeaponDNA wepDNA = bot.Weapon.DNA;

            bot.AttachWeapon(null);
            bot.AttachWeapon(new Weapon(wepDNA, new Point3D(), _world, _materialIDs.Weapon));

            bot.PhysicsBody.Velocity = new Vector3D();
            bot.PhysicsBody.AngularVelocity = new Vector3D();
        }

        #endregion
        #region Private Methods - cone

        private static ConeResult MakeCone(double maxRadius = 1)
        {
            Random rand = StaticRandom.GetRandomForThread();

            int count = rand.Next(10, 300);
            Vector3D axis = new Vector3D(0, 0, 1);
            double angle = rand.Next(10, 170);

            double radiusMin, radiusMax;
            double scenario = rand.NextDouble(10);
            if (scenario < 3)
            {
                radiusMin = 0;
                radiusMax = rand.NextDouble(maxRadius);
            }
            else if (scenario < 6)
            {
                radiusMin = rand.NextDouble(maxRadius);
                radiusMax = radiusMin;
            }
            else
            {
                radiusMin = rand.NextDouble(maxRadius);
                radiusMax = rand.NextDouble(maxRadius);
                UtilityCore.MinMax(ref radiusMin, ref radiusMax);
            }

            Vector3D[] points = Math3D.GetRandomVectors_Cone_EvenDist(count, axis, angle, radiusMin, radiusMax, stopRadiusPercent: 0, stopIterationCount: 100);

            var pairs = Math3D.EvenDistribution.GetShortestPair(points.Select(o => new Math3D.EvenDistribution.Dot(false, o, 1)).ToArray());

            return new ConeResult()
            {
                Count = count,
                Angle = angle,
                RadiusMin = radiusMin,
                RadiusMax = radiusMax,
                Points = points,
                AvgPairLength = pairs.
                    Select(o => o.Length).
                    Average(),
                Volume = Math3D.EvenDistribution.GetConeVolume(radiusMin, radiusMax, angle),
            };
        }

        #endregion
    }
}
