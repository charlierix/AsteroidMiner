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
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.Controls;
using Game.Newt.v2.GameItems.ShipEditor;
using Game.Newt.v2.GameItems.ShipParts;
using Game.HelperClassesWPF;
using Game.HelperClassesWPF.Controls2D;
using Game.HelperClassesWPF.Primitives3D;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.Arcanorum
{
    /// <summary>
    /// This is based on a flash game called Arcanorum.  It's a really fun game, simple, fun.
    /// </summary>
    /// <remarks>
    /// It had a lot of promise, but they didn't make an open world mode.  They just made a few levels, and a survival mode with fixed enemies
    /// 
    /// http://www.bored.com/game/play/150743/Arcanorum.html
    /// http://www.kongregate.com/games/boredcom/arcanorum
    /// http://www.funny-games.biz/arcanorum.html
    /// 
    /// Make an open world that is a large cylinder, and lots of rooms to jump to, or bridges to other open worlds
    /// 
    /// Randomly generate weapons that can be picked up/purchased, but also give the user a weapon designer screen
    /// </remarks>
    public partial class ArcanorumWindow : Window
    {
        #region Class: CameraBall

        private class CameraBall : IMapObject
        {
            public CameraBall(World world)
            {
                using (CollisionHull hull = CollisionHull.CreateNull(world))
                {
                    this.PhysicsBody = new Body(hull, Matrix3D.Identity, 250, null);
                }

                this.CreationTime = DateTime.Now;
            }

            #region IMapObject Members

            public long Token
            {
                get
                {
                    return this.PhysicsBody.Token;
                }
            }

            public Body PhysicsBody
            {
                get;
                private set;
            }

            public Visual3D[] Visuals3D
            {
                get
                {
                    return this.PhysicsBody.Visuals;
                }
            }
            public Model3D Model
            {
                get;
                private set;
            }

            public Point3D PositionWorld
            {
                get
                {
                    return this.PhysicsBody.Position;
                }
            }
            public Vector3D VelocityWorld
            {
                get
                {
                    return this.PhysicsBody.Velocity;
                }
            }
            public Matrix3D OffsetMatrix
            {
                get
                {
                    return this.PhysicsBody.OffsetMatrix;
                }
            }

            public double Radius
            {
                get;
                private set;
            }

            public DateTime CreationTime
            {
                get;
                private set;
            }

            public int CompareTo(IMapObject other)
            {
                return MapObjectUtil.CompareToT(this, other);
            }

            public bool Equals(IMapObject other)
            {
                return MapObjectUtil.EqualsT(this, other);
            }
            public override bool Equals(object obj)
            {
                return MapObjectUtil.EqualsObj(this, obj);
            }

            public override int GetHashCode()
            {
                return MapObjectUtil.GetHashCode(this);
            }

            #endregion
        }

        #endregion
        #region Class: Visual2DIntermediate

        /// <summary>
        /// This holds 2D controls that are placed in front of the 3D viewport
        /// </summary>
        private class Visual2DIntermediate
        {
            protected Visual2DIntermediate() { }
            public Visual2DIntermediate(UIElement[] elements, DateTime removeTime)
            {
                this.Elements = elements;
                this.RemoveTime = removeTime;
            }

            public UIElement[] Elements;
            public DateTime RemoveTime;
        }

        #endregion
        #region Class: Visual2DHitPoint

        /// <summary>
        /// This is a health bar that gets superimposed above the item
        /// </summary>
        private class Visual2DHitPoint : Visual2DIntermediate
        {
            // These interfaces point the same object, but it's just easier to get at that object through the interfaces
            public readonly ITakesDamage TakesDamage;
            public readonly IMapObject MapObject;
            private readonly Viewport3D _viewport;

            public Visual2DHitPoint(ITakesDamage takesDamage, IMapObject mapObject, Viewport3D viewport, Color color, DateTime removeTime)
            {
                this.TakesDamage = takesDamage;
                this.MapObject = mapObject;
                _viewport = viewport;
                this.RemoveTime = removeTime;

                this.ProgressBar = new ProgressBarGame()
                {
                    RightLabelVisibility = Visibility.Collapsed,
                    ProgressColor = Color.FromRgb(color.R, color.G, color.B),       // using the full alpha here
                    Opacity = color.A / 255d,       // the alpha of the color affects the entire control's transparency

                    Minimum = 0,
                    Maximum = this.TakesDamage.HitPoints.QuantityMax,
                    Value = this.TakesDamage.HitPoints.QuantityCurrent,
                };

                this.Elements = new UIElement[] { this.ProgressBar };

                // Set position
                Update();
            }

            public ProgressBarGame ProgressBar;

            /// <summary>
            /// This sets the progress bar's value, and position
            /// </summary>
            public void Update()
            {
                // Progress bar value
                this.ProgressBar.Value = this.TakesDamage.HitPoints.QuantityCurrent;
                this.ProgressBar.Maximum = this.TakesDamage.HitPoints.QuantityMax;

                // Set position
                bool isInFront;
                var circle = UtilityWPF.Project3Dto2D(out isInFront, _viewport, this.MapObject.PositionWorld, this.MapObject.Radius * 1.5d);

                if (circle != null && isInFront)
                {
                    double width = circle.Item2 * 1.5d;
                    double height = 12;

                    this.ProgressBar.Width = width;
                    this.ProgressBar.Height = height;

                    Canvas.SetLeft(this.ProgressBar, circle.Item1.X - (width / 2d));
                    Canvas.SetTop(this.ProgressBar, circle.Item1.Y - circle.Item2 - (height / 2d));

                    this.ProgressBar.Visibility = Visibility.Visible;
                }
                else
                {
                    this.ProgressBar.Visibility = Visibility.Collapsed;
                }
            }
        }

        #endregion

        #region Declaration Section

        private const double BOUNDRYSIZE = 100;
        private const double BOUNDRYSIZEHALF = BOUNDRYSIZE * .5d;

        private const int MAX_TREASURE = 4;
        private const int MAX_BOXES = 30;

        private const double UNDERITEM_RADIUS = 1d;
        private const double UNDERITEM_Z = -5;

        private const string FILE = "Arcanorum Options.xml";

        private Point3D _boundryMin;
        private Point3D _boundryMax;

        private World _world = null;
        private Map _map = null;
        private GravityFieldUniform _gravity = null;

        private UpdateManager _updateManager = null;

        private EditorOptions _editorOptions = null;
        private ItemOptionsArco _itemOptions = null;

        private BackdropPanel _backdropPanel = null;
        private ShopPanel _shopPanel = null;
        private CharacterPanel _charPanel = null;

        private MaterialManager _materialManager = null;
        private MaterialIDs _materialIDs = null;

        private Bot _player = null;

        private DragHitShape _dragPlane = null;

        private CameraBall _cameraBall = null;
        private MapObject_ChasePoint_Velocity _draggingCamera = null;

        private KeepItems2D _keep2D = null;

        private CustomMouseCursor _mouseCursor = null;

        private Point? _mousePoint = null;
        private double _cameraZ;

        private bool _isShiftPressed = false;
        private bool _isAltPressed = false;
        private bool _isCtrlPressed = false;

        /// <summary>
        /// These are visuals that need to disapear once the time has elapsed
        /// They are added to pnlTextVisuals
        /// </summary>
        private List<Visual2DIntermediate> _visuals2D = new List<Visual2DIntermediate>();

        // If the user clicks on a bot, this will show its parts
        private BotNPC _selectedBot = null;
        private ShipViewerWindow _selectedBotViewer = null;

        private Brush _strikeBrushPlayer = new SolidColorBrush(UtilityWPF.ColorFromHex("FF4F55"));
        private Brush _strikeBrushPlayerOutline = new SolidColorBrush(UtilityWPF.ColorFromHex("40000000"));
        private Brush _strikeBrushOther = new SolidColorBrush(UtilityWPF.ColorFromHex("D9FF64"));
        private Brush _strikeBrushOtherOutline = new SolidColorBrush(UtilityWPF.ColorFromHex("70000000"));
        private Brush _coordBrush = new SolidColorBrush(UtilityWPF.ColorFromHex("FFFFFF"));
        private Brush _coordBrushOutline = new SolidColorBrush(UtilityWPF.ColorFromHex("70000000"));
        private Lazy<FontFamily> _strikeFont = new Lazy<FontFamily>(() => GetBestStrikeFont());

        private DateTime _nextCleanup = DateTime.Now + TimeSpan.FromSeconds(60);
        private int _checkPortalCountdown = -1;

        private DateTime? _clearStatusTime = DateTime.Now;

        private bool _use3DPanels;      // this comes from the config

        private bool _isInitialized = false;

        #endregion


        #region Player Vision TEST

        //private SensorVision _playerVision = null;
        //private SensorHoming _playerVision = null;      // this is named funny, but avoids other places to rename all over
        //private Ellipse[] _playerVisionNeurons = null;
        //private byte[] _neuronColor_Zero = new byte[] { 0, 0, 0, 255 };
        //private byte[] _neuronColor_One = new byte[] { 255, 0, 0, 255 };

        //private void PlayerVision_RequestWorldLocation(object sender, PartRequestWorldLocationArgs e)
        //{
        //    e.Orientation = _player.PhysicsBody.Rotation;
        //    e.Position = _player.PositionWorld;
        //}

        #endregion
        #region Fitness TEST

        //private FitnessRule_TooFar _fitnessFar = null;
        //private FitnessRule_TooStill _fitnessStill = null;
        //private FitnessRule_TooTwitchy _fitnessTwitchy = null;
        //private FitnessTracker _fitnessAll = null;

        //private TextBlock lblFitnessFarTotal = null;
        //private TextBlock lblFitnessFarRate = null;

        //private TextBlock lblFitnessStillTotal = null;
        //private TextBlock lblFitnessStillRate = null;

        //private TextBlock lblFitnessTwitchyTotal = null;
        //private TextBlock lblFitnessTwitchyRate = null;

        //private TextBlock lblFitnessAllTotal = null;
        //private TextBlock lblFitnessAllRate = null;

        #endregion
        #region Dream TEST

        //private OfflineWorld _dreamWorld;

        //private static void DreamABot(OfflineWorld dream)
        //{

        //    BotDNA dna = new BotDNA()
        //    {
        //        Parts = new PartDNA[]
        //        {
        //            new PartDNA() { PartType = SensorVision.PARTTYPE, Position = new Point3D(0, 0, 1.5), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1) },
        //            new PartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(0, 0, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1) },
        //            new PartDNA() { PartType = MotionController_Linear.PARTTYPE, Position = new Point3D(0, 0, -1.5), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1) },
        //        }
        //    };


        //    dream.Add(new OfflineWorld.AddBotArgs(Math3D.GetRandomVector(dream.Size).ToPoint(), null, null, dna));


        //    //BotNPC npc = new BotNPC(dna, center + Math3D.GetRandomVector_Circular(radius), _world, _map, _keep2D, _materialIDs, _viewport, _editorOptions, _itemOptions, _gravity, _dragPlane, true, true);


        //}
        //private static void DreamAWeapon(OfflineWorld dream)
        //{
        //    WeaponHandleDNA handle = WeaponHandleDNA.GetRandomDNA(UtilityHelper.GetRandomEnum<WeaponHandleMaterial>());

        //    dream.Add(new OfflineWorld.AddWeaponArgs(Math3D.GetRandomVector(dream.Size).ToPoint(), Math3D.GetRandomVector_Spherical(4), null, handle));
        //}

        #endregion


        #region Constructor

        public ArcanorumWindow()
        {
            InitializeComponent();

            LoadFromFile();
        }

        #endregion

        #region Event Listeners

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                #region Misc

                _itemOptions = new ItemOptionsArco();
                //_itemOptions.VisionSensorNeuronDensity = 80;

                _editorOptions = new EditorOptions();

                _gravity = new GravityFieldUniform() { Gravity = new Vector3D(0, -_itemOptions.Gravity, 0) };

                #endregion

                #region Init World

                _boundryMin = new Point3D(-BOUNDRYSIZEHALF, -BOUNDRYSIZEHALF, -BOUNDRYSIZEHALF);
                _boundryMax = new Point3D(BOUNDRYSIZEHALF, BOUNDRYSIZEHALF, BOUNDRYSIZEHALF);

                _world = new World();
                _world.Updating += new EventHandler<WorldUpdatingArgs>(World_Updating);

                List<Point3D[]> innerLines, outerLines;
                _world.SetCollisionBoundry(out innerLines, out outerLines, _boundryMin, _boundryMax);

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
                #region Materials

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
                #region Map

                _map = new Map(_viewport, null, _world);
                _map.ShouldBuildSnapshots = true;
                //_map.ShouldShowSnapshotLines = false;

                #endregion
                #region Keep 2D

                //TODO: drag plane should either be a plane or a large cylinder, based on the current (level|scene|stage|area|arena|map|place|region|zone)

                // This game is 3D emulating 2D, so always have the mouse go to the XY plane
                _dragPlane = new DragHitShape();
                _dragPlane.SetShape_Plane(new Triangle(new Point3D(-1, -1, 0), new Point3D(1, -1, 0), new Point3D(0, 1, 0)));

                // This will keep objects onto that plane using forces (not velocities)
                _keep2D = new KeepItems2D();
                _keep2D.SnapShape = _dragPlane;

                #endregion

                // Doing this here so transparency looks correct
                CreateShops(6);
                CreateNests(1);

                #region Player

                _player = new Bot(null, StaticRandom.Next(1, 30), new Point3D(0, 0, 0), _world, _map, _keep2D, _materialIDs, _viewport, _editorOptions, _itemOptions, _gravity, _dragPlane, new Point3D(0, 0, 0), 5, false, false);
                _player.Ram.SetWPFStuff(grdViewPort, pnlVisuals2D);

                _map.AddItem(_player);

                #region FAIL

                // When not holding a weapon, this did fairly well, but still moved a bit unnaturally.  When holding a weapon, the auto forces
                // tended to keep the bot in a tight orbit around the chase point, which kept the weapon swinging

                //// This is given the mouse position, and applies forces on the player to get to that position
                //// The goal is to accelerate the player pretty quickly up to a max velocity
                //_draggingPlayer = new MapObjectChaseForces(_player);

                //// Attraction Force
                //_draggingPlayer.Forces.Add(new ChaseForcesGradient<ChaseForcesConstant>(new[]
                //    {
                //        new ChaseForcesGradientStop<ChaseForcesConstant>(new ChaseDistance(true, 0d), new ChaseForcesConstant(ChaseDirectionType.Direction) { BaseAcceleration = 20d, ApplyWhenUnderSpeed = 100d }),
                //        new ChaseForcesGradientStop<ChaseForcesConstant>(new ChaseDistance(false, 1d), new ChaseForcesConstant(ChaseDirectionType.Direction) { BaseAcceleration = 500d, ApplyWhenUnderSpeed = 100d }),
                //        new ChaseForcesGradientStop<ChaseForcesConstant>(new ChaseDistance(true, double.MaxValue), new ChaseForcesConstant(ChaseDirectionType.Direction) { BaseAcceleration = 500d, ApplyWhenUnderSpeed = 100d })
                //    }));

                //// This keeps the bot from orbiting the chase point
                //_draggingPlayer.Forces.Add(new ChaseForcesGradient<ChaseForcesDrag>(new[]
                //    {
                //        new ChaseForcesGradientStop<ChaseForcesDrag>(new ChaseDistance(true, 0d), new ChaseForcesDrag(ChaseDirectionType.Velocity_Orth) { BaseAcceleration = 20d }),        // putting a dip near the center to avoid jitter
                //        new ChaseForcesGradientStop<ChaseForcesDrag>(new ChaseDistance(false, .5d), new ChaseForcesDrag(ChaseDirectionType.Velocity_Orth) { BaseAcceleration = 60d }),
                //        new ChaseForcesGradientStop<ChaseForcesDrag>(new ChaseDistance(false, 5d), new ChaseForcesDrag(ChaseDirectionType.Velocity_Orth) { BaseAcceleration = 40d }),
                //        new ChaseForcesGradientStop<ChaseForcesDrag>(new ChaseDistance(true, double.MaxValue), new ChaseForcesDrag(ChaseDirectionType.Velocity_Orth) { BaseAcceleration = 40d }),
                //    }));

                //// These act like a shock absorber
                //_draggingPlayer.Forces.Add(new ChaseForcesDrag(ChaseDirectionType.Velocity_AlongIfVelocityAway) { BaseAcceleration = 50d });

                //_draggingPlayer.Forces.Add(new ChaseForcesGradient<ChaseForcesDrag>(new[]
                //    {
                //        new ChaseForcesGradientStop<ChaseForcesDrag>(new ChaseDistance(true, 0d), new ChaseForcesDrag(ChaseDirectionType.Velocity_AlongIfVelocityToward) { BaseAcceleration = 100d }),
                //        new ChaseForcesGradientStop<ChaseForcesDrag>(new ChaseDistance(false, .75d), new ChaseForcesDrag(ChaseDirectionType.Velocity_AlongIfVelocityToward) { BaseAcceleration = 20d }),
                //        new ChaseForcesGradientStop<ChaseForcesDrag>(new ChaseDistance(false, 2d), new ChaseForcesDrag(ChaseDirectionType.Velocity_AlongIfVelocityToward) { BaseAcceleration = 0d }),
                //    }));

                #endregion

                #endregion
                #region Mouse Cursor

                #region POOR

                // This will set the mouse cursor to a custom image.  But that uses winforms drawing, and is static, and non transparent

                ////NOTE: Can't figure out how to make this work with semitransparency, so just making it really small
                ////Ellipse cursor = new Ellipse() { Fill = new SolidColorBrush(UtilityWPF.ColorFromHex("FC4C3C")) };
                //Border cursor = new Border() { Background = new SolidColorBrush(UtilityWPF.ColorFromHex("555343")) };       //FC4C3C
                //grdViewPort.Cursor = UtilityWPF.ConvertToCursor(UtilityWPF.RenderControl(cursor, 4, 4, false), new Point(0.5, 0.5));

                #endregion

                grdViewPort.Cursor = Cursors.None;

                _mouseCursor = new CustomMouseCursor(grdViewPort, _player);

                pnlVisuals2D.Children.Add(_mouseCursor);

                #endregion
                #region Camera Controller

                _cameraBall = new CameraBall(_world);
                _cameraBall.PhysicsBody.Position = _camera.Position;

                _draggingCamera = new MapObject_ChasePoint_Velocity(_cameraBall);
                _draggingCamera.MaxVelocity = _player.DraggingBot.MaxVelocity.Value * 1.5d;
                _draggingCamera.Multiplier = _player.DraggingBot.Multiplier * .05d;

                _cameraZ = _camera.Position.Z;

                #endregion
                #region Inventory Panel

                pnlInventoryQuick.SlotCount = 4;

                //TODO: Don't tie these directly together.  Give the raw list to the full inventory panel, and let it decide the quick list
                pnlInventoryQuick.Weapons = _player.Inventory.Weapons;

                #endregion

                //CreateCornerGraphics();       // not needed anymore, because the border lines are being shown
                CreateTreasureBoxes(20, new Point3D(0, 0, 0));
                //PlaceTestWeapons(new Point3D(-20, -8, 0), new Vector3D(1, 0, 0));
                CreateNPCs(3, new Point3D(0, 0, 0));

                #region Update Manager

                _updateManager = new UpdateManager(
                    new Type[] { typeof(Bot), typeof(BotNPC), typeof(NPCNest) },
                    new Type[] { typeof(Bot), typeof(BotNPC) },
                    _map);

                #endregion

                #region Player Vision TEST

                // Build the sensor
                //PartDNA visionDNA = new PartDNA() { PartType = SensorVision.PARTTYPE, Position = new Point3D(0, 0, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1) };

                //_playerVision = new SensorVision(_editorOptions, _itemOptions, visionDNA, _map, 10, typeof(TreasureBox));
                //_playerVision.BotToken = _player.Token;



                //PartDNA visionDNA = new PartDNA() { PartType = SensorHoming.PARTTYPE, Position = new Point3D(0, 0, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1) };

                //_playerVision = new SensorHoming(_editorOptions, _itemOptions, visionDNA, _map, 5);


                //_playerVision.RequestWorldLocation += PlayerVision_RequestWorldLocation;


                //// Show the neurons as 2D shapes (updated in update event)
                //_playerVisionNeurons = new Ellipse[_playerVision.NeuronWorldPositions.Length];

                //for (int cntr = 0; cntr < _playerVisionNeurons.Length; cntr++)
                //{
                //    _playerVisionNeurons[cntr] = new Ellipse()
                //    {
                //        Width = 12,
                //        Height = 12,
                //        Fill = Brushes.Transparent,
                //        Stroke = new SolidColorBrush(UtilityWPF.ColorFromHex("608080C0")),
                //        StrokeThickness = 1
                //    };

                //    pnlVisuals2D.Children.Add(_playerVisionNeurons[cntr]);
                //}

                #endregion
                #region Fitness TEST

                //_fitnessFar = new FitnessRule_TooFar(_player, new Point3D(0, 0, 0), 8, 16);
                //_fitnessStill = new FitnessRule_TooStill(_player, .1, 1);
                //_fitnessTwitchy = new FitnessRule_TooTwitchy(_player, -.8);
                //_fitnessAll = new FitnessTracker(_player, new Tuple<IFitnessRule, double>[] { new Tuple<IFitnessRule, double>(_fitnessFar, 1d), new Tuple<IFitnessRule, double>(_fitnessStill, 1d), new Tuple<IFitnessRule, double>(_fitnessTwitchy, 1d) });


                //Thickness margin = new Thickness(4, 0, 0, 0);

                //lblFitnessFarTotal = new TextBlock() { Margin = margin };
                //lblFitnessFarRate = new TextBlock() { Margin = margin };
                //StackPanel row = new StackPanel() { Orientation = Orientation.Horizontal };
                //row.Children.Add(new TextBlock { Text = "Far" });
                //row.Children.Add(lblFitnessFarTotal);
                //row.Children.Add(lblFitnessFarRate);
                //pnlDebugReport.Children.Add(row);

                //lblFitnessStillTotal = new TextBlock() { Margin = margin };
                //lblFitnessStillRate = new TextBlock() { Margin = margin };
                //row = new StackPanel() { Orientation = Orientation.Horizontal };
                //row.Children.Add(new TextBlock { Text = "Still" });
                //row.Children.Add(lblFitnessStillTotal);
                //row.Children.Add(lblFitnessStillRate);
                //pnlDebugReport.Children.Add(row);

                //lblFitnessTwitchyTotal = new TextBlock() { Margin = margin };
                //lblFitnessTwitchyRate = new TextBlock() { Margin = margin };
                //row = new StackPanel() { Orientation = Orientation.Horizontal };
                //row.Children.Add(new TextBlock { Text = "Twitchy" });
                //row.Children.Add(lblFitnessTwitchyTotal);
                //row.Children.Add(lblFitnessTwitchyRate);
                //pnlDebugReport.Children.Add(row);

                //lblFitnessAllTotal = new TextBlock() { Margin = margin };
                //lblFitnessAllRate = new TextBlock() { Margin = margin };
                //row = new StackPanel() { Orientation = Orientation.Horizontal };
                //row.Children.Add(new TextBlock { Text = "Total" });
                //row.Children.Add(lblFitnessAllTotal);
                //row.Children.Add(lblFitnessAllRate);
                //pnlDebugReport.Children.Add(row);

                #endregion
                #region Dream TEST

                //_dreamWorld = new OfflineWorld(BOUNDRYSIZE, _itemOptions);

                //DreamAWeapon(_dreamWorld);
                //DreamABot(_dreamWorld);

                #endregion

                _world.UnPause();

                _isInitialized = true;

                //WeaponRadio_Checked(this, new RoutedEventArgs());
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


                //_dreamWorld.Dispose();




                _world.Pause();

                _updateManager.Dispose();

                if (_selectedBotViewer != null)
                {
                    _selectedBotViewer.Close();
                    _selectedBotViewer = null;
                    _selectedBot = null;
                }

                SaveToFile();

                _keep2D.Dispose();

                _cameraBall.PhysicsBody.Dispose();

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

        private void BackdropPanel_ClosePanel(object sender, EventArgs e)
        {
            try
            {
                CloseBackdrop();
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

                if (_clearStatusTime != null && DateTime.Now > _clearStatusTime)
                {
                    statusMessage.Text = "";
                    _clearStatusTime = null;
                }

                #region Player Vision TEST

                //_playerVision.Update_MainThread(e.ElapsedTime);
                //_playerVision.Update_AnyThread(e.ElapsedTime);

                //Point3D playerPos1 = _player.PositionWorld;
                //INeuron[] playerNeurons = _playerVision.Neruons_All.ToArray();
                //Vector3D[] playerNeuronPositions = _playerVision.NeuronWorldPositions.Select(o => o.ToVector()).ToArray();

                //for (int cntr = 0; cntr < _playerVisionNeurons.Length; cntr++)
                //{
                //    bool isInFront;
                //    Point? position2D = UtilityWPF.Project3Dto2D(out isInFront, _viewport, playerPos1 + playerNeuronPositions[cntr]);

                //    if (position2D == null || !isInFront)
                //    {
                //        continue;
                //    }

                //    Ellipse ellipse = _playerVisionNeurons[cntr];

                //    byte[] color = UtilityWPF.AlphaBlend(_neuronColor_One, _neuronColor_Zero, playerNeurons[cntr].Value);
                //    ellipse.Fill = new SolidColorBrush(Color.FromArgb(color[0], color[1], color[2], color[3]));

                //    Canvas.SetLeft(ellipse, position2D.Value.X - (ellipse.ActualWidth / 2));
                //    Canvas.SetTop(ellipse, position2D.Value.Y - (ellipse.ActualHeight / 2));
                //}

                #endregion
                #region Fitness TEST

                //_fitnessAll.Update_AnyThread(e.ElapsedTime);

                ////TODO: Add Age to IMapObject
                //double age = (DateTime.Now - _player.CreationTime).TotalSeconds;

                //lblFitnessFarTotal.Text = Math.Round(_fitnessFar.Score, 3).ToString();
                //lblFitnessFarRate.Text = Math.Round(_fitnessFar.Score / age, 3).ToString();

                //lblFitnessStillTotal.Text = Math.Round(_fitnessStill.Score, 3).ToString();
                //lblFitnessStillRate.Text = Math.Round(_fitnessStill.Score / age, 3).ToString();

                //lblFitnessTwitchyTotal.Text = Math.Round(_fitnessTwitchy.Score, 3).ToString();
                //lblFitnessTwitchyRate.Text = Math.Round(_fitnessTwitchy.Score / age, 3).ToString();

                //lblFitnessAllTotal.Text = Math.Round(_fitnessAll.Score, 3).ToString();
                //lblFitnessAllRate.Text = Math.Round(_fitnessAll.Score / age, 3).ToString();

                #endregion

                #region Drag Player

                bool updatedMouseCursor = false;

                if (_mousePoint != null)
                {
                    RayHitTestParameters mouseRay = UtilityWPF.RayFromViewportPoint(_camera, _viewport, _mousePoint.Value);
                    Point3D? hitPoint = _dragPlane.CastRay(mouseRay);

                    if (hitPoint != null)
                    {
                        //NOTE: This is is setting the position of the end of the spring
                        //NOTE: _draggingPlayer is listening to Body.ApplyForceAndTorque, and updates each time that is called
                        _player.DraggingBot.SetPosition(hitPoint.Value);

                        // The mouse cursor knows where the player is as well, so can change its appearance
                        _mouseCursor.Update(hitPoint);
                        updatedMouseCursor = true;
                    }
                }

                if (!updatedMouseCursor)
                {
                    _mouseCursor.Update(null);
                }

                #endregion
                #region Drag Camera

                Point3D playerPos = _player.PositionWorld;
                _draggingCamera.SetPosition(new Point3D(playerPos.X, playerPos.Y, _cameraZ));

                Point3D position = _cameraBall.PositionWorld;
                _camera.Position = position;

                Vector3D? cameraNormal = _dragPlane.GetNormal(position);
                if (cameraNormal != null)       // it should never be null
                {
                    Vector3D desiredLook = cameraNormal.Value * -1;

                    _camera.LookDirection = desiredLook;

                    #region Attempt always on screen

                    //// Get the 2D position of the player if this look direction is used
                    //bool isInFront;
                    //Point? playerPos2D = UtilityWPF.Project3Dto2D(out isInFront, _viewport, _player.PositionWorld);



                    //TODO: If the player is off the edge of the screen, then come up with a 3D point to look at so that
                    //the player will stay on screen



                    //if (playerPos2D == null)
                    //{
                    //    int three = 2;
                    //}
                    //else if (playerPos2D.Value.X < 0 || playerPos2D.Value.Y < 0)
                    //{
                    //    int six = 4;
                    //}
                    //else if (playerPos2D.Value.X > grdViewPort.ActualWidth || playerPos2D.Value.Y > grdViewPort.ActualHeight)
                    //{
                    //    int twelve = 9;
                    //}

                    #endregion

                    #region FAIL
                    // If the camera always looks at the player, then the player will always be in the middle of
                    // the screen, which doesn't look very good
                    //_camera.LookDirection = _player.PositionWorld - _camera.Position;
                    #endregion
                }

                // Need to keep setting this, because the ram weapon can momentarily allow the player to go faster.  If the
                // camera doesn't stay caught up, the bot will get twitchy when it's over the mouse (it can still get twitchy,
                // but this helps)
                _draggingCamera.MaxVelocity = _player.DraggingBot.MaxVelocity.Value * 1.5d;
                _draggingCamera.Multiplier = _player.DraggingBot.Multiplier * .05d;

                #endregion

                _keep2D.Update();

                #region Over Portal

                _checkPortalCountdown--;

                if (_checkPortalCountdown < 0)
                {
                    _checkPortalCountdown = 10;

                    PortalVisual overPortal = GetOverPortal();

                    if (overPortal == null)
                    {
                        if (_clearStatusTime == null)       // if it's non null, there is some other message showing, so don't clear it
                        {
                            statusMessage.Text = "";
                        }
                    }
                    else
                    {
                        statusMessage.Text = "Press space to enter " + overPortal.PortalType.ToString().ToLower();
                        _clearStatusTime = null;        // make sure it doesn't get wiped early
                    }
                }

                #endregion

                #region Remove/Update temp 2D visuals

                int index = 0;

                DateTime now = DateTime.Now;

                while (index < _visuals2D.Count)
                {
                    bool isDead = false;
                    // Handle specific types of visuals
                    if (_visuals2D[index] is Visual2DHitPoint)
                    {
                        Visual2DHitPoint visualCast = (Visual2DHitPoint)_visuals2D[index];
                        visualCast.Update();
                        isDead = Math3D.IsNearZero(visualCast.TakesDamage.HitPoints.QuantityCurrent);
                    }

                    // See if it should be removed
                    if (isDead || _visuals2D[index].RemoveTime < now)
                    {
                        foreach (UIElement element in _visuals2D[index].Elements)
                        {
                            pnlVisuals2D.Children.Remove(element);
                        }

                        _visuals2D.RemoveAt(index);
                    }
                    else
                    {
                        index++;
                    }
                }

                #endregion

                if (DateTime.Now > _nextCleanup)
                {
                    CleanupIfTooMany();
                    _nextCleanup = DateTime.Now + TimeSpan.FromSeconds(30);
                }
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
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Collision_BotWeapon(object sender, MaterialCollisionArgs e)
        {
            try
            {
                Bot bot = _map.GetItem<Bot>(e.GetBody(_materialIDs.Bot));
                if (bot == null)
                {
                    return;
                }

                // See if this is attached or free floating
                Body weaponBody = e.GetBody(_materialIDs.Weapon);
                if (weaponBody == null)
                {
                    return;
                }

                Weapon weaponFreeFloating = _map.GetItem<Weapon>(weaponBody);
                if (weaponFreeFloating != null)
                {
                    #region Maybe pick up weapon

                    //TODO: Instead of always attaching, let user specified rules figure out what to do
                    //bot.CollidedWithTreasure(weaponFreeFloating);

                    if (bot.Token == _player.Token)
                    {
                        if (_isShiftPressed)
                        {
                            bot.AttachWeapon(weaponFreeFloating, Bot.ItemToFrom.Map, Bot.ItemToFrom.Inventory);
                        }
                        else if (_isCtrlPressed)
                        {
                            bot.AttachWeapon(weaponFreeFloating, Bot.ItemToFrom.Map, Bot.ItemToFrom.Map);
                        }
                        else if (_isAltPressed)
                        {
                            bot.AddToInventory(weaponFreeFloating, Bot.ItemToFrom.Map);
                        }
                        else
                        {
                            statusMessage.Text = "Hold in shift to pick up weapon";
                            _clearStatusTime = DateTime.Now + TimeSpan.FromSeconds(4);
                        }
                    }
                    else
                    {
                        // For now, always attach
                        bot.AttachWeapon(weaponFreeFloating, Bot.ItemToFrom.Map, Bot.ItemToFrom.Inventory);

                        //TODO: Expand this method, or make a different one, and let the bot decide
                        //bot.CollidedWithTreasure(
                    }

                    #endregion
                    return;
                }

                // It's not free floating, that means it's attached to a bot.  Do damage
                Weapon weaponSwinging = FindWeapon_Attached(weaponBody);
                if (weaponSwinging == null)
                {
                    // This should never happen
                    return;
                }

                #region Do damage

                WeaponDamage damage = weaponSwinging.CalculateDamage(e.Collisions);
                if (damage != null)
                {
                    var damageResult = bot.Damage(damage, weaponSwinging);
                    if (damageResult != null)
                    {
                        bool isPlayer = bot.Token == _player.Token;

                        AddStrikeDamage(damageResult.Item2, isPlayer);
                        ShowHealthBar(bot);

                        if (damageResult.Item1)
                        {
                            if (isPlayer)
                            {
                                //TODO: Kill the player
                                _player.HitPoints.QuantityCurrent = _player.HitPoints.QuantityMax;
                                MessageBox.Show("You just died, reseting health to full", this.Title, MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                            else
                            {
                                bot.AttachWeapon(null, Bot.ItemToFrom.Nowhere, Bot.ItemToFrom.Map);     // if they are holding a weapon, release it to the map

                                //TODO: Randomly spill some of its inventory
                                //bot.Inventory.Weapons
                                //bot.RemoveFromInventory

                                // The bot is now dead, remove it
                                _map.RemoveItem(bot);
                                bot.Dispose();
                            }
                        }
                    }
                }

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Collision_WeaponWeapon(object sender, MaterialCollisionArgs e)
        {
            try
            {
                Weapon weapon0 = FindWeapon_Attached(e.Body0);
                if (weapon0 == null)
                {
                    return;
                }

                Weapon weapon1 = FindWeapon_Attached(e.Body1);
                if (weapon1 == null)
                {
                    return;
                }

                // Both weapons are being swung by bots.  Damage the weapons (don't worry about the damage that they return)
                weapon0.CalculateDamage(e.Collisions);
                weapon1.CalculateDamage(e.Collisions);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Collision_BotWall(object sender, MaterialCollisionArgs e)
        {
            try
            {
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Collision_WeaponWall(object sender, MaterialCollisionArgs e)
        {
            try
            {
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Collision_WeaponTreasureBox(object sender, MaterialCollisionArgs e)
        {
            try
            {
                TreasureBox treasure = _map.GetItem<TreasureBox>(e.GetBody(_materialIDs.TreasureBox));
                if (treasure == null)
                {
                    // This should be very rare (world and map being out of sync by a frame)
                    return;
                }

                Weapon weaponSwinging = FindWeapon_Attached(e.GetBody(_materialIDs.Weapon));
                if (weaponSwinging == null)
                {
                    return;
                }

                WeaponDamage damage = weaponSwinging.CalculateDamage(e.Collisions);
                if (damage != null)
                {
                    var damageResult = treasure.Damage(damage);
                    if (damageResult != null)
                    {
                        AddStrikeDamage(damageResult.Item2, false);
                        ShowHealthBar(treasure);

                        if (damageResult.Item1)
                        {
                            Point3D position = treasure.PositionWorld;
                            Vector3D velocity = treasure.VelocityWorld;
                            double radius = treasure.Radius;
                            object[] contianedTreasure = treasure.ContainedTreasureDNA;

                            #region Destroy barrel

                            // Get rid of the box
                            _keep2D.Remove(treasure);
                            _map.RemoveItem(treasure, true);
                            treasure.Dispose();

                            // Add the objects
                            if (contianedTreasure != null)
                            {
                                foreach (object item in contianedTreasure)
                                {
                                    AddTreasure(item, position + Math3D.GetRandomVector_Spherical(radius * 1.25d), velocity + Math3D.GetRandomVector_Spherical(1d));
                                }
                            }

                            #endregion

                            // Make more (just for fun)
                            CreateTreasureBoxes(StaticRandom.Next(1, 2), position);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Collision_BotTreasureBox(object sender, MaterialCollisionArgs e)
        {
            try
            {
                TreasureBox treasure = _map.GetItem<TreasureBox>(e.GetBody(_materialIDs.TreasureBox));
                if (treasure == null)
                {
                    // This should be very rare (world and map being out of sync by a frame)
                    return;
                }

                Bot bot = _map.GetItem<Bot>(e.GetBody(_materialIDs.Bot));
                if (bot == null)
                {
                    return;
                }

                WeaponDamage damage = bot.CalculateDamage(e.Collisions);
                if (damage != null)
                {
                    var damageResult = treasure.Damage(damage);
                    if (damageResult != null)
                    {
                        AddStrikeDamage(damageResult.Item2, false);
                        ShowHealthBar(treasure);

                        if (damageResult.Item1)
                        {
                            Point3D position = treasure.PositionWorld;
                            Vector3D velocity = treasure.VelocityWorld;
                            double radius = treasure.Radius;
                            object[] contianedTreasure = treasure.ContainedTreasureDNA;

                            #region Destroy barrel

                            // Get rid of the box
                            _keep2D.Remove(treasure);
                            _map.RemoveItem(treasure, true);
                            treasure.Dispose();

                            // Add the objects
                            if (contianedTreasure != null)
                            {
                                foreach (object item in contianedTreasure)
                                {
                                    AddTreasure(item, position + Math3D.GetRandomVector_Spherical(radius * 1.25d), velocity + Math3D.GetRandomVector_Spherical(1d));
                                }
                            }

                            #endregion

                            // Make more (just for fun)
                            CreateTreasureBoxes(StaticRandom.Next(1, 2), position);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            const int MAXLEVEL = 40;

            try
            {
                if (pnlOverlay.IsVisible)
                {
                    // Otherwise, this method steals the keypress
                    return;
                }

                e.Handled = true;       // need to set this, or the key could steal focus (alt key does for sure)

                if (e.IsRepeat)
                {
                    return;
                }

                Key key = (e.Key == Key.System ? e.SystemKey : e.Key);		// windows treats alt special, but I just care if alt is pressed

                switch (key)
                {
                    case Key.OemTilde:
                        if (_isShiftPressed)
                        {
                            _player.AttachWeapon(null, Bot.ItemToFrom.Nowhere, Bot.ItemToFrom.Map);
                        }
                        else
                        {
                            _player.AttachWeapon(null, Bot.ItemToFrom.Nowhere, Bot.ItemToFrom.Inventory);
                        }
                        break;

                    case Key.D1:
                    case Key.NumPad1:
                        NumberKeyPressed(1);
                        break;

                    case Key.D2:
                    case Key.NumPad2:
                        NumberKeyPressed(2);
                        break;

                    case Key.D3:
                    case Key.NumPad3:
                        NumberKeyPressed(3);
                        break;

                    case Key.D4:
                    case Key.NumPad4:
                        NumberKeyPressed(4);
                        break;

                    case Key.Space:
                        PortalVisual overPortal = GetOverPortal();
                        if (overPortal != null)
                        {
                            EnterPortal(overPortal);
                        }
                        break;

                    case Key.C:
                        ShowCharacter(CharacterPanel.Tabs.CharacterStats);
                        break;

                    case Key.Tab:
                    case Key.I:
                        ShowCharacter(CharacterPanel.Tabs.Inventory);
                        break;

                    case Key.O:
                        ShowCoords();
                        break;

                    case Key.LeftShift:
                    case Key.RightShift:
                        _isShiftPressed = true;
                        break;

                    case Key.LeftAlt:
                    case Key.RightAlt:
                        _isAltPressed = true;
                        break;

                    case Key.LeftCtrl:
                    case Key.RightCtrl:
                        _isCtrlPressed = true;
                        break;

                    case Key.F1:
                        if (_isShiftPressed)
                        {
                            if (_player.Level > 1)
                            {
                                _player.Level--;
                            }
                        }
                        else
                        {
                            _player.Level = Convert.ToInt32(Math.Round(UtilityCore.GetScaledValue(1, MAXLEVEL, 1, 12, 1)));
                        }
                        break;
                    case Key.F2:
                        if (_isShiftPressed)
                        {
                            _player.Level++;
                        }
                        else
                        {
                            _player.Level = Convert.ToInt32(Math.Round(UtilityCore.GetScaledValue(1, MAXLEVEL, 1, 12, 2)));
                        }
                        break;
                    case Key.F3:
                        _player.Level = Convert.ToInt32(Math.Round(UtilityCore.GetScaledValue(1, MAXLEVEL, 1, 12, 3)));
                        break;
                    case Key.F4:
                        _player.Level = Convert.ToInt32(Math.Round(UtilityCore.GetScaledValue(1, MAXLEVEL, 1, 12, 4)));
                        break;
                    case Key.F5:
                        _player.Level = Convert.ToInt32(Math.Round(UtilityCore.GetScaledValue(1, MAXLEVEL, 1, 12, 5)));
                        break;
                    case Key.F6:
                        _player.Level = Convert.ToInt32(Math.Round(UtilityCore.GetScaledValue(1, MAXLEVEL, 1, 12, 6)));
                        break;
                    case Key.F7:
                        _player.Level = Convert.ToInt32(Math.Round(UtilityCore.GetScaledValue(1, MAXLEVEL, 1, 12, 7)));
                        break;
                    case Key.F8:
                        _player.Level = Convert.ToInt32(Math.Round(UtilityCore.GetScaledValue(1, MAXLEVEL, 1, 12, 8)));
                        break;
                    case Key.F9:
                        _player.Level = Convert.ToInt32(Math.Round(UtilityCore.GetScaledValue(1, MAXLEVEL, 1, 12, 9)));
                        break;
                    case Key.F10:
                        _player.Level = Convert.ToInt32(Math.Round(UtilityCore.GetScaledValue(1, MAXLEVEL, 1, 12, 10)));
                        break;
                    case Key.F11:
                        _player.Level = Convert.ToInt32(Math.Round(UtilityCore.GetScaledValue(1, MAXLEVEL, 1, 12, 11)));
                        break;
                    case Key.F12:
                        _player.Level = Convert.ToInt32(Math.Round(UtilityCore.GetScaledValue(1, MAXLEVEL, 1, 12, 12)));
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            try
            {
                if (pnlOverlay.IsVisible)
                {
                    // Otherwise, this method steals the keypress
                    return;
                }

                Key key = (e.Key == Key.System ? e.SystemKey : e.Key);		// windows treats alt special, but I just care if alt is pressed

                switch (key)
                {
                    case Key.LeftShift:
                    case Key.RightShift:
                        _isShiftPressed = false;
                        break;

                    case Key.LeftAlt:
                    case Key.RightAlt:
                        _isAltPressed = false;
                        break;

                    case Key.LeftCtrl:
                    case Key.RightCtrl:
                        _isCtrlPressed = false;
                        break;
                }

                e.Handled = true;       // need to set this, or the key could steal focus (alt key does for sure)
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
                _mousePoint = e.GetPosition(grdViewPort);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void grdViewPort_MouseLeave(object sender, MouseEventArgs e)
        {
            try
            {
                _mousePoint = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void grdViewPort_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            try
            {
                double newZ = _cameraZ - (e.Delta * .003d);
                //double newZ = _cameraZ - (e.Delta * .03d);

                if (newZ > 5 && newZ < BOUNDRYSIZEHALF)
                {
                    _cameraZ = newZ;

                    //TODO: This only works with a plane.  Ask the drag surface for a normal (directly under the current position), and project a distance of _cameraZ along that normal
                    Point3D newPoint = new Point3D(_camera.Position.X, _camera.Position.Y, _cameraZ);
                    _draggingCamera.SetPosition(newPoint);
                    _cameraBall.PhysicsBody.Position = newPoint;
                    _camera.Position = newPoint;
                }
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
                // Fire a ray at the mouse point
                Point clickPoint = e.GetPosition(grdViewPort);

                RayHitTestParameters clickRay;
                List<MyHitTestResult> hits = UtilityWPF.CastRay(out clickRay, clickPoint, grdViewPort, _camera, _viewport, true);

                // See if they clicked on something
                var clickedItem = GetHit(hits, new Type[] { typeof(BotNPC) });

                if (_selectedBot != null && clickedItem != null && _selectedBot.Equals(clickedItem.Item1))
                {
                    // This is already selected, don't do anything
                }
                else
                {
                    // Close any existing viewer
                    if (_selectedBotViewer != null)
                    {
                        _selectedBotViewer.Close();
                    }
                    _selectedBot = null;
                    _selectedBotViewer = null;

                    //TODO: Place a 2D graphic behind the selected bot
                    if (clickedItem != null)
                    {
                        _selectedBot = (BotNPC)clickedItem.Item1;

                        PartBase[] parts = _selectedBot.Parts;
                        if (parts != null)
                        {
                            #region Show Viewer

                            _selectedBotViewer = new ShipViewerWindow(parts, _selectedBot.NeuronLinks);
                            _selectedBotViewer.Owner = this;		// the other settings like topmost, showintaskbar, etc are already set in the window's xaml
                            _selectedBotViewer.PopupBorder = new SolidColorBrush(UtilityWPF.ColorFromHex("60000000"));
                            _selectedBotViewer.PopupBackground = new SolidColorBrush(UtilityWPF.ColorFromHex("30000000"));
                            _selectedBotViewer.ViewportBorder = new SolidColorBrush(UtilityWPF.ColorFromHex("E0000000"));

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

                            _selectedBotViewer.ViewportBackground = brush;

                            _selectedBotViewer.PanelBorder = new SolidColorBrush(UtilityWPF.ColorFromHex("8068736B"));
                            _selectedBotViewer.PanelBackground = new SolidColorBrush(UtilityWPF.ColorFromHex("80424F45"));
                            _selectedBotViewer.Foreground = new SolidColorBrush(UtilityWPF.ColorFromHex("F0F0F0"));

                            //Point windowClickPoint = UtilityWPF.TransformToScreen(clickPoint, grdViewPort);
                            //Point popupPoint = new Point(windowClickPoint.X + 50, windowClickPoint.Y - (_selectedBotViewer.Height / 3d));
                            //popupPoint = UtilityWPF.EnsureWindowIsOnScreen(popupPoint, new Size(_selectedBotViewer.Width, _selectedBotViewer.Height));		// don't let the popup straddle monitors
                            //_selectedBotViewer.Left = popupPoint.X;
                            //_selectedBotViewer.Top = popupPoint.Y;

                            ShipViewerWindow.PlaceViewerInCorner(_selectedBotViewer, UtilityWPF.TransformToScreen(clickPoint, grdViewPort));

                            _selectedBotViewer.Show();

                            #endregion
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            try
            {
                // Show this on the default browser
                System.Diagnostics.Process.Start(e.Uri.OriginalString);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private Weapon FindWeapon_Attached(Body body)
        {
            return _map.GetItems<Bot>(true).
                Where(o => o.Weapon != null && o.Weapon.PhysicsBody.Equals(body)).
                Select(o => o.Weapon).
                FirstOrDefault();
        }
        private PortalVisual GetOverPortal()
        {
            // Get the portal that they are over.  If they are over multiple, return the closest one

            //TODO: Handle column maps as well as planes
            //TODO: There are only a couple dozen portals, but if there are hundreds, get from the octree snapshot instead

            Point position = _player.PositionWorld.ToPoint2D();

            var retVal = _map.GetItems<PortalVisual>(true).
                Select(o => new { Portal = o, DistSquared = (o.PositionWorld.ToPoint2D() - position).LengthSquared }).
                Where(o => o.DistSquared <= (_player.Radius + o.Portal.Radius) * (_player.Radius + o.Portal.Radius)).
                OrderBy(o => o.DistSquared).
                FirstOrDefault();

            if (retVal == null)
            {
                return null;
            }
            else
            {
                return retVal.Portal;
            }
        }

        private void CleanupIfTooMany()
        {
            Point3D position = _player.PositionWorld;

            // Currently, weapons are the only type of treasure
            CleanupIfTooMany<Weapon>(MAX_TREASURE, position);

            CleanupIfTooMany<TreasureBox>(MAX_BOXES, position);
        }
        private void CleanupIfTooMany<T>(int maxAllowed, Point3D position) where T : IMapObject
        {
            DateTime minAge = DateTime.Now - TimeSpan.FromSeconds(30);

            // See how many items there are that are old enough to be deleted
            var items = _map.GetItems<T>(true)
                .Where(o => o.CreationTime < minAge).
                Select(o => new { Item = o, Distance = (o.PositionWorld - position).LengthSquared }).
                OrderByDescending(o => o.Distance).     // put the items farthest away first.  This makes the rand.pow easier to write (farther away items are more likely to be removed)
                ToList();

            if (items.Count <= maxAllowed)
            {
                return;
            }

            // These closest items will never be deleted
            int safetyCount = Convert.ToInt32(Math.Round(maxAllowed / 2d));

            Random rand = StaticRandom.GetRandomForThread();

            while (items.Count > maxAllowed)
            {
                // Give a higher chance of far away objects to be removed
                int candidateCount = items.Count - safetyCount;
                int index = Convert.ToInt32(rand.NextPow(3, candidateCount - 1, false));

                // Remove it
                if (_map.RemoveItem(items[index].Item))
                {
                    _keep2D.Remove(items[index].Item);

                    if (items[index].Item is IDisposable)
                    {
                        ((IDisposable)items[index].Item).Dispose();
                    }
                    else
                    {
                        items[index].Item.PhysicsBody.Dispose();
                    }
                }

                // Whether it was in the map or not, it's gone now
                items.RemoveAt(index);
            }
        }

        private void CreateTreasureBoxes(int count, Point3D center)
        {
            double radius = GetRandomDropRadius(center, .8);

            for (int cntr = 0; cntr < count; cntr++)
            {
                object[] treasure = null;
                if (StaticRandom.NextDouble() > .5d)
                {
                    treasure = new object[] { WeaponDNA.GetRandomDNA() };
                }

                TreasureBox box = new TreasureBox(center + Math3D.GetRandomVector_Circular(radius), 1000, 100, _materialIDs.TreasureBox, _world, new WeaponDamage(), treasure);
                box.PhysicsBody.AngularVelocity = Math3D.GetRandomVector_Spherical(6);

                _keep2D.Add(box, false);
                _map.AddItem(box);
            }
        }

        private void CreateNPCs(int count, Point3D center)
        {
            double radius = GetRandomDropRadius(center, .65);

            for (int cntr = 0; cntr < count; cntr++)
            {
                BotDNA dna = new BotDNA()
                {
                    UniqueID = Guid.NewGuid(),

                    DraggingMaxVelocity = _player.DraggingBot.MaxVelocity.Value * .5,
                    DraggingMultiplier = _player.DraggingBot.Multiplier * .5,

                    Parts = new PartDNA[]
                    {
                        new PartDNA() { PartType = SensorVision.PARTTYPE, Position = new Point3D(0, 0, 1.5), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1) },
                        new PartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(0, 0, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1) },
                        new PartDNA() { PartType = MotionController_Linear.PARTTYPE, Position = new Point3D(0, 0, -1.5), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1) },
                    }
                };

                BotNPC npc = new BotNPC(dna, StaticRandom.Next(1, 25), center + Math3D.GetRandomVector_Circular(radius), _world, _map, _keep2D, _materialIDs, _viewport, _editorOptions, _itemOptions, _gravity, _dragPlane, new Point3D(0, 0, 0), 5, true, true);

                _map.AddItem(npc);
            }
        }

        private double GetRandomDropRadius(Point3D center, double percentMapSize)
        {
            //TODO: take center into account
            double retVal = Math3D.Min(Math.Abs(_boundryMax.X - center.X), Math.Abs(_boundryMax.Y - center.Y), Math.Abs(_boundryMax.Z - center.Z),
                Math.Abs(_boundryMin.X - center.X), Math.Abs(_boundryMin.Y - center.Y), Math.Abs(_boundryMin.Z - center.Z))
                * percentMapSize;

            return retVal;
        }

        private void AddTreasure(object dna, Point3D position, Vector3D velocity)
        {
            IMapObject item = null;

            if (dna is WeaponDNA)
            {
                item = new Weapon((WeaponDNA)dna, position, _world, _materialIDs.Weapon);
                _map.AddItem((Weapon)item);     // there's no real harm in adding to the map here.  The alternative would be a delegate to add the cast type, but that's excessive
            }
            else
            {
                throw new ApplicationException("Unknown type of treasure");
            }

            item.PhysicsBody.Velocity = velocity;
            item.PhysicsBody.AngularVelocity = Math3D.GetRandomVector_Spherical(6);

            _keep2D.Add(item, false);
            //_map.AddItem(item);     // AddItem is a generic method, and needs the exact type (not IMapObject)
        }

        /// <summary>
        /// This is just for debugging to see all possible weapon handles
        /// </summary>
        private void PlaceTestWeapons(Point3D start, Vector3D direction)
        {
            Point3D next = start;

            foreach (WeaponHandleMaterial material in Enum.GetValues(typeof(WeaponHandleMaterial)))
            {
                WeaponHandleDNA handleDNA = WeaponHandleDNA.GetRandomDNA(material);

                WeaponSpikeBallDNA leftBallDNA = null;
                if (!Math3D.IsNearZero(handleDNA.AttachPointPercent))
                {
                    leftBallDNA = WeaponSpikeBallDNA.GetRandomDNA(handleDNA);
                }

                WeaponSpikeBallDNA rightBallDNA = null;
                if (!Math3D.IsNearValue(handleDNA.AttachPointPercent, 1d))
                {
                    rightBallDNA = WeaponSpikeBallDNA.GetRandomDNA(handleDNA);
                }

                WeaponDNA dna = new WeaponDNA()
                {
                    UniqueID = Guid.NewGuid(),
                    Handle = handleDNA,
                    HeadLeft = leftBallDNA,
                    HeadRight = rightBallDNA
                };

                Weapon weapon = new Weapon(dna, next, _world, _materialIDs.Weapon);
                weapon.PhysicsBody.AngularVelocity = Math3D.GetRandomVector_Spherical(4);

                _keep2D.Add(weapon, false);
                _map.AddItem(weapon);

                next += direction.ToUnit() * (weapon.Radius * 2.5d);
            }
        }

        /// <summary>
        /// My video card is crappy and only redraws dirty parts of the window, leaving artifacts.  By placing
        /// a small visual in each corner, it forces a proper redraw
        /// </summary>
        private void CreateCornerGraphics()
        {
            Model3DGroup geometries = new Model3DGroup();

            DiffuseMaterial material = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(1, 0, 0, 0)));

            double mult = 1.1d;
            Vector3D min = _boundryMin.ToVector() * mult;
            Vector3D max = _boundryMax.ToVector() * mult;

            Vector3D[] eightPoints = new Vector3D[]
            {
                new Vector3D(min.X ,min.Y ,min.Z),
                new Vector3D(max.X, min.Y, min.Z),
                new Vector3D(min.X, max.Y, min.Z),
                new Vector3D(max.X, max.Y, min.Z),

                new Vector3D(min.X ,min.Y, max.Z),
                new Vector3D(max.X, min.Y, max.Z),
                new Vector3D(min.X, max.Y, max.Z),
                new Vector3D(max.X, max.Y, max.Z),
            };

            foreach (Vector3D position in eightPoints)
            {
                GeometryModel3D geometry = new GeometryModel3D();

                geometry.Material = material;
                geometry.BackMaterial = material;

                geometry.Geometry = UtilityWPF.GetSquare2D(.1);
                geometry.Transform = new TranslateTransform3D(position);

                geometries.Children.Add(geometry);
            }

            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = geometries;
            _viewport.Children.Add(visual);
        }

        private void CreateShops(int count)
        {
            double minRadius = 10;
            double maxRadius = _boundryMax.X - (UNDERITEM_RADIUS * 4);
            if (minRadius < maxRadius)
            {
                minRadius = 0;
            }

            List<Point3D> points = new List<Point3D>();

            for (int cntr = 0; cntr < count; cntr++)
            {
                #region Create Shop

                Inventory inventory = new Inventory();
                foreach (Weapon weapon in Enumerable.Range(0, StaticRandom.Next(4, 20)).Select(o => new Weapon(WeaponDNA.GetRandomDNA(), new Point3D(), null, _materialIDs.Weapon)))
                {
                    inventory.Weapons.Add(weapon);
                }

                PortalVisual shop = new PortalVisual(new Shop(inventory), UNDERITEM_RADIUS);

                if (cntr == 0)
                {
                    // Always put this in the same spot so it's easy to find
                    shop.PositionWorld = new Point3D(-3, -3, UNDERITEM_Z);
                }
                else
                {
                    // Come up with a position that isn't too close to others
                    PortalVisual[] existing = _map.GetAllItems().
                        Where(o => o is PortalVisual).
                        Select(o => (PortalVisual)o).
                        ToArray();

                    while (true)
                    {
                        Vector3D vect = Math3D.GetRandomVector_Circular(minRadius, maxRadius);
                        Point3D pos = new Point3D(vect.X, vect.Y, UNDERITEM_Z);

                        if (!existing.Any(o => (o.PositionWorld - pos).Length < (UNDERITEM_RADIUS + o.Radius) * 7))
                        {
                            shop.PositionWorld = pos;
                            break;
                        }
                    }
                }

                points.Add(shop.PositionWorld);
                _map.AddItem(shop);

                #endregion
            }

            #region Create Roads

            double maxLength = (_boundryMax - _boundryMin).Length / 2d;

            BillboardLine3DSet roads = new BillboardLine3DSet(false, false, true);
            roads.Color = UtilityWPF.ColorFromHex("08808080");
            roads.IsReflectiveColor = false;
            roads.BeginAddingLines();

            if (points.Count > 3)
            {
                // Only show sensible roads
                //TriangleIndexed[] triangles = Math2D.GetDelaunayTriangulation(points.Select(o => o.ToPoint2D()).ToArray(), points.ToArray());
                //foreach (var line in TriangleIndexed.GetUniqueLines(triangles))
                foreach (var line in Math2D.GetDelaunayTriangulation(points.Select(o => o.ToPoint2D()).ToArray(), true))
                {
                    roads.AddLine(points[line.Item1], points[line.Item2], CreateShopsSprtRoadWidth(points[line.Item1], points[line.Item2], maxLength));
                }
            }
            else
            {
                // Show all paths
                for (int outer = 0; outer < points.Count - 1; outer++)
                {
                    for (int inner = outer + 1; inner < points.Count; inner++)
                    {
                        roads.AddLine(points[outer], points[inner], CreateShopsSprtRoadWidth(points[outer], points[inner], maxLength));
                    }
                }
            }

            roads.EndAddingLines();

            _viewport.Children.Add(roads);

            #endregion
        }
        private static double CreateShopsSprtRoadWidth(Point3D point1, Point3D point2, double maxLength)
        {
            double distance = (point2 - point1).Length;

            // Reverse disance
            if (distance > maxLength)
            {
                distance = maxLength;
            }

            return UtilityCore.GetScaledValue_Capped(.03, .3, 0, maxLength, maxLength - distance);        // shorter roads should be wider
        }

        private void CreateNests(int count)
        {
            double minRadius = 20;
            double maxRadius = _boundryMax.X - (UNDERITEM_RADIUS * 6);
            if (minRadius < maxRadius)
            {
                minRadius = 0;
            }

            for (int cntr = 0; cntr < count; cntr++)
            {
                #region Create Nest

                NPCNest nest = new NPCNest(null, UNDERITEM_RADIUS, _world, _map, _keep2D, _materialIDs, _viewport, _editorOptions, _itemOptions, _gravity, _dragPlane);



                //// Come up with a position that isn't too close to others
                //PortalVisual[] existing = _map.GetAllItems().
                //    Where(o => o is PortalVisual).
                //    Select(o => (PortalVisual)o).
                //    ToArray();

                //while (true)
                //{
                //    Vector3D vect = Math3D.GetRandomVector_Circular(minRadius, maxRadius);
                //    Point3D pos = new Point3D(vect.X, vect.Y, UNDERITEM_Z);

                //    if (!existing.Any(o => (o.PositionWorld - pos).Length < (UNDERITEM_RADIUS + o.Radius) * 7))
                //    {
                //        nest.PositionWorld = pos;
                //        break;
                //    }
                //}


                nest.PositionWorld = new Point3D(6, 6, UNDERITEM_Z);



                _map.AddItem(nest);

                #endregion
            }
        }

        // This adds text to the window
        //TODO: Make sounds
        private void AddStrikeDamage(WeaponDamage damage, bool isPlayer)
        {
            if(damage.Position == null)
            {
                return;
            }

            double total = damage.GetDamage();

            AddStrikeDamage(total, damage.Position.Value, isPlayer);
        }
        private void AddStrikeDamage(double damage, Point3D position, bool isPlayer)
        {
            const double MINDAMAGE = 1d;
            const double MAXDAMAGE = 250d;

            if (damage < MINDAMAGE)
            {
                // Minimal hit, not worth reporting
                return;
            }

            bool isInFront;
            Point? position2D = UtilityWPF.Project3Dto2D(out isInFront, _viewport, position);

            if (position2D == null || !isInFront)
            {
                return;
            }

            // Text
            OutlinedTextBlock text = new OutlinedTextBlock();
            text.Text = damage.ToString("N0");
            text.FontFamily = _strikeFont.Value;
            text.FontSize = UtilityCore.GetScaledValue_Capped(13d, 36d, MINDAMAGE, MAXDAMAGE, damage);
            int textWeight = Convert.ToInt32(Math.Round(UtilityCore.GetScaledValue_Capped(700, 998, MINDAMAGE, MAXDAMAGE, damage)));
            text.FontWeight = FontWeight.FromOpenTypeWeight(textWeight);
            text.StrokeThickness = UtilityCore.GetScaledValue_Capped(.75d, 1.6d, MINDAMAGE, MAXDAMAGE, damage);

            //text.FontSize = UtilityHelper.GetScaledValue_Capped(13d, 64d, MINDAMAGE, MAXDAMAGE, damage);
            //text.StrokeThickness = UtilityHelper.GetScaledValue_Capped(.75d, 2.5d, MINDAMAGE, MAXDAMAGE, damage);

            if (isPlayer)
            {
                text.Fill = _strikeBrushPlayer;
                text.Stroke = _strikeBrushPlayerOutline;
            }
            else
            {
                text.Fill = _strikeBrushOther;
                text.Stroke = _strikeBrushOtherOutline;
            }

            pnlVisuals2D.Children.Add(text);

            Canvas.SetLeft(text, position2D.Value.X - (text.ActualWidth / 2));
            Canvas.SetTop(text, position2D.Value.Y - (text.ActualHeight / 2));

            UIElement[] controls = new UIElement[] { text };

            double seconds = UtilityCore.GetScaledValue_Capped(.4, 4, MINDAMAGE, MAXDAMAGE * 3, damage);
            _visuals2D.Add(new Visual2DIntermediate(controls, DateTime.Now + TimeSpan.FromSeconds(seconds)));
        }
        private static FontFamily GetBestStrikeFont()
        {
            return UtilityWPF.GetFont(new string[] { "Lucida Console", "Verdana", "Microsoft Sans Serif", "Arial" });
        }

        private void ShowCoords()
        {
            for (int x = Convert.ToInt32(-BOUNDRYSIZE); x < BOUNDRYSIZE; x += 5)
            {
                for (int y = Convert.ToInt32(-BOUNDRYSIZE); y < BOUNDRYSIZE; y += 5)
                {
                    Point3D position = new Point3D(x, y, 0);

                    bool isInFront;
                    Point? position2D = UtilityWPF.Project3Dto2D(out isInFront, _viewport, position);

                    if (position2D == null || !isInFront)
                    {
                        continue;
                    }

                    // Text
                    OutlinedTextBlock text = new OutlinedTextBlock();
                    text.Text = position.ToString();
                    text.FontFamily = _strikeFont.Value;
                    text.FontSize = 15;
                    text.FontWeight = FontWeight.FromOpenTypeWeight(750);
                    text.StrokeThickness = .8d;

                    text.Fill = _coordBrush;
                    text.Stroke = _coordBrushOutline;

                    pnlVisuals2D.Children.Add(text);

                    Canvas.SetLeft(text, position2D.Value.X - (text.ActualWidth / 2));
                    Canvas.SetTop(text, position2D.Value.Y - (text.ActualHeight / 2));

                    UIElement[] controls = new UIElement[] { text };

                    double seconds = 15;
                    _visuals2D.Add(new Visual2DIntermediate(controls, DateTime.Now + TimeSpan.FromSeconds(seconds)));
                }
            }
        }

        private void ShowHealthBar(IMapObject item)
        {
            // See if this is already showing
            Visual2DHitPoint existing = _visuals2D.
                Where(o => o is Visual2DHitPoint).
                Select(o => (Visual2DHitPoint)o).
                Where(o => o.MapObject.Equals(item)).
                FirstOrDefault();

            DateTime removeTime = DateTime.Now + TimeSpan.FromSeconds(3d);

            if (existing == null)
            {
                Visual2DHitPoint visual = new Visual2DHitPoint((ITakesDamage)item, item, _viewport, UtilityWPF.ColorFromHex("60FF4040"), removeTime);

                _visuals2D.Add(visual);
                foreach (UIElement element in visual.Elements)
                {
                    pnlVisuals2D.Children.Add(element);
                }
            }
            else
            {
                // Just update the time
                existing.RemoveTime = removeTime;
            }
        }

        private void NumberKeyPressed(int number)
        {
            if (_player.Inventory.Weapons.Count < number)       // the number pressed is 1 based, but the list is 0 based
            {
                return;
            }

            if (_isShiftPressed)
            {
                // Eject from inventory to the map
                _player.RemoveFromInventory(_player.Inventory.Weapons[number - 1], Bot.ItemToFrom.Map);

                //TODO: Test these others
                //_player.RemoveFromInventory(_player.Inventory.Weapons[number - 1], Bot.ItemToFrom.Inventory);
                //_player.RemoveFromInventory(_player.Inventory.Weapons[number - 1], Bot.ItemToFrom.Nowhere);
            }
            else
            {
                // Equip from inventory
                _player.AttachWeapon(_player.Inventory.Weapons[number - 1], Bot.ItemToFrom.Inventory, Bot.ItemToFrom.Inventory);

                //TODO: Test these others
                //_player.AttachWeapon(_player.Inventory.Weapons[number - 1], Bot.ItemToFrom.Inventory, Bot.ItemToFrom.Map);
                //_player.AttachWeapon(_player.Inventory.Weapons[number - 1], Bot.ItemToFrom.Inventory, Bot.ItemToFrom.Nowhere);
            }
        }

        private void CloseBackdrop()
        {
            if (_backdropPanel.LeftPanel != null && _backdropPanel.LeftPanel is ShopPanel)
            {
                #region Reattach Weapon

                ShopPanel shopPanelCast = (ShopPanel)_backdropPanel.LeftPanel;

                // Before displaying the shop screen, the attached weapon was placed in inventory
                if (shopPanelCast.AttachedWeaponID != null)
                {
                    // See if it's still in inventory (they may have sold it)
                    Weapon weapon = _player.Inventory.Weapons.Where(o => o.DNA.UniqueID == shopPanelCast.AttachedWeaponID.Value).FirstOrDefault();
                    if (weapon != null)
                    {
                        _player.AttachWeapon(weapon, Bot.ItemToFrom.Inventory, Bot.ItemToFrom.Inventory);
                    }
                }

                #endregion
            }

            // Hide it
            pnlOverlay.Visibility = System.Windows.Visibility.Collapsed;
            _backdropPanel.ClearPanels();

            // Resume the world
            _world.SimulationSpeed = 1d;
            if (_world.IsPaused)
            {
                _world.UnPause();
            }
        }
        private void EnterPortal(PortalVisual portal)
        {
            // Pause the world
            _mousePoint = null;
            _world.Pause();

            // Prep the panel
            EnsureBackdropPanelCreated();
            _backdropPanel.ClearPanels();

            switch (portal.PortalType)
            {
                case PortalVisualType.Shop:
                    EnsureShopPanelCreated();
                    _backdropPanel.BackdropColors = portal.BackdropPanelColors;
                    _shopPanel.ShowInventories((Shop)portal.Item, _player);
                    _shopPanel.DetailPanel = _backdropPanel.DetailPanel;
                    _backdropPanel.LeftPanel = _shopPanel;
                    break;

                default:
                    throw new ApplicationException("Unknown PortalVisualType: " + portal.PortalType.ToString());
            }

            // Show the panel
            pnlOverlay.Visibility = Visibility.Visible;
            _backdropPanel.Focus();
            Keyboard.Focus(_backdropPanel);
        }
        private void ShowCharacter(CharacterPanel.Tabs tab)
        {
            _mousePoint = null;
            _world.SimulationSpeed = .05d;       // don't fully pause, that would be cheating :)

            EnsureBackdropPanelCreated();
            EnsureCharacterPanelCreated();

            _backdropPanel.ClearPanels();

            _backdropPanel.BackdropColors = _player.DNAPartial.ShellColors;

            _charPanel.SetCurrentTab(tab);
            _backdropPanel.LeftPanel = _charPanel;

            pnlOverlay.Visibility = Visibility.Visible;
            _backdropPanel.Focus();
            Keyboard.Focus(_backdropPanel);
        }

        private void EnsureBackdropPanelCreated()
        {
            if (_backdropPanel != null)
            {
                return;
            }

            _backdropPanel = new BackdropPanel();
            _backdropPanel.ClosePanelKeys = _backdropPanel.ClosePanelKeys.Concat(new Key[] { Key.Tab, Key.Space, Key.C, Key.I }).ToArray();        // C for character, I for inventory
            _backdropPanel.Is3DPanel = _use3DPanels;        // this comes from the config

            pnlOverlay.Content = _backdropPanel;

            _backdropPanel.ClosePanel += BackdropPanel_ClosePanel;
        }
        private void EnsureShopPanelCreated()
        {
            if (_shopPanel != null)
            {
                return;
            }

            _shopPanel = new ShopPanel();
        }
        private void EnsureCharacterPanelCreated()
        {
            if (_charPanel != null)
            {
                return;
            }

            _charPanel = new CharacterPanel();
            _charPanel.World = _world;
        }

        private Tuple<IMapObject, MyHitTestResult> GetHit(List<MyHitTestResult> hits, Type[] selectableTypes)
        {
            // This is copied from ItemSelectDragLogic

            // Get the list of selectable items
            IMapObject[] candidates = selectableTypes.
                SelectMany(o => _map.GetItems(o, true)).
                Where(o => o.Visuals3D != null).
                ToArray();

            foreach (var hit in hits)		// hits are sorted by distance, so this method will only return the closest match
            {
                Visual3D visualHit = hit.ModelHit.VisualHit;
                if (visualHit == null)
                {
                    continue;
                }

                // See if this visual is part of one of the candidates
                IMapObject item = candidates.Where(o => o.Visuals3D.Any(p => p == visualHit)).FirstOrDefault();

                if (item != null)
                {
                    return Tuple.Create(item, hit);
                }
            }

            return null;
        }

        private void LoadFromFile()
        {
            ArcanorumOptions options = UtilityCore.ReadOptions<ArcanorumOptions>(FILE) ?? new ArcanorumOptions();

            _use3DPanels = options.Use3DPanels ?? true;
        }
        private void SaveToFile()
        {
            ArcanorumOptions options = new ArcanorumOptions();

            if (_backdropPanel != null)
            {
                _use3DPanels = _backdropPanel.Is3DPanel;
            }

            options.Use3DPanels = _use3DPanels;

            UtilityCore.SaveOptions(options, FILE);
        }

        #endregion
    }

    #region Class: AsteroidFieldOptions

    /// <summary>
    /// This class gets saved to xaml in their appdata folder
    /// NOTE: All properties are nullable so that new ones can be added, and an old xml will still load
    /// NOTE: Once a property is added, it can never be removed (or an old config will bomb the deserialize)
    /// </summary>
    public class ArcanorumOptions
    {
        public bool? Use3DPanels
        {
            get;
            set;
        }
    }

    #endregion
}
