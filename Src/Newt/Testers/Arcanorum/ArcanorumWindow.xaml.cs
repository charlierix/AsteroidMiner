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
using Game.HelperClasses;
using Game.Newt.AsteroidMiner2;
using Game.Newt.AsteroidMiner2.Controls;
using Game.Newt.AsteroidMiner2.ShipEditor;
using Game.Newt.AsteroidMiner2.ShipParts;
using Game.Newt.HelperClasses;
using Game.Newt.HelperClasses.Controls2D;
using Game.Newt.HelperClasses.Primitives3D;
using Game.Newt.NewtonDynamics;

namespace Game.Newt.Testers.Arcanorum
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

        private const string FILE = "Arcanorum Options.xml";

        private Point3D _boundryMin;
        private Point3D _boundryMax;

        private World _world = null;
        private Map _map = null;
        private GravityFieldUniform _gravity = null;

        private EditorOptions _editorOptions = null;
        private ItemOptionsArco _itemOptions = null;

        private BackdropPanel _backdropPanel = null;
        private ShopPanel _shopPanel = null;

        private MaterialManager _materialManager = null;
        private MaterialIDs _materialIDs = null;

        private Bot _player = null;

        private DragHitShape _dragPlane = null;

        private CameraBall _cameraBall = null;
        private MapObjectChaseVelocity _draggingCamera = null;

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

        private Brush _strikeBrushPlayer = new SolidColorBrush(UtilityWPF.ColorFromHex("FF4F55"));
        private Brush _strikeBrushPlayerOutline = new SolidColorBrush(UtilityWPF.ColorFromHex("40000000"));
        private Brush _strikeBrushOther = new SolidColorBrush(UtilityWPF.ColorFromHex("D9FF64"));
        private Brush _strikeBrushOtherOutline = new SolidColorBrush(UtilityWPF.ColorFromHex("70000000"));
        private Lazy<FontFamily> _strikeFont = new Lazy<FontFamily>(() => GetBestStrikeFont());

        private DateTime _nextCleanup = DateTime.Now + TimeSpan.FromSeconds(60);
        private int _checkPortalCountdown = -1;

        private DateTime? _clearStatusTime = DateTime.Now;

        private bool _use3DPanels;      // this comes from the config

        private bool _isInitialized = false;

        #endregion


        #region Player Vision TEST

        //private SensorVision _playerVision = null;
        //private Ellipse[] _playerVisionNeurons = null;
        //private byte[] _neuronColor_Zero = new byte[] { 0, 0, 0, 255 };
        //private byte[] _neuronColor_One = new byte[] { 255, 0, 0, 255 };

        //private void PlayerVision_RequestWorldLocation(object sender, PartRequestWorldLocationArgs e)
        //{
        //    e.Orientation = _player.PhysicsBody.Rotation;
        //    e.Position = _player.PositionWorld;
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

                _gravity = new GravityFieldUniform() { Gravity = new Vector3D(0, -10, 0) };

                _editorOptions = new EditorOptions();

                _itemOptions = new ItemOptionsArco();
                //_itemOptions.VisionSensorNeuronDensity = 80;

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
                Game.Newt.NewtonDynamics.Material material = new Game.Newt.NewtonDynamics.Material();
                material.Elasticity = .1d;
                _materialIDs.Wall = _materialManager.AddMaterial(material);

                // Bot
                material = new Game.Newt.NewtonDynamics.Material();
                _materialIDs.Bot = _materialManager.AddMaterial(material);

                // Bot Ram
                material = new Game.Newt.NewtonDynamics.Material();
                material.Elasticity = .95d;
                _materialIDs.BotRam = _materialManager.AddMaterial(material);

                // Exploding Bot
                material = new Game.Newt.NewtonDynamics.Material();
                material.IsCollidable = false;
                _materialIDs.ExplodingBot = _materialManager.AddMaterial(material);

                // Weapon
                material = new Game.Newt.NewtonDynamics.Material();
                _materialIDs.Weapon = _materialManager.AddMaterial(material);

                // Treasure Box
                material = new Game.Newt.NewtonDynamics.Material();
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

                // Doing this here so transparency looks correct
                CreateShops(6);

                #region Keep 2D

                //TODO: drag plane should either be a plane or a large cylinder, based on the current (level|scene|stage|area|arena|map|place|region|zone)

                // This game is 3D emulating 2D, so always have the mouse go to the XY plane
                _dragPlane = new DragHitShape();
                _dragPlane.SetShape_Plane(new Triangle(new Point3D(-1, -1, 0), new Point3D(1, -1, 0), new Point3D(0, 1, 0)));

                // This will keep objects onto that plane using forces (not velocities)
                _keep2D = new KeepItems2D();
                _keep2D.SnapShape = _dragPlane;

                #endregion
                #region Player

                _player = new Bot(null, new Point3D(0, 0, 0), _world, _map, _keep2D, _materialIDs, _viewport, _editorOptions, _itemOptions, _gravity, false, false);
                _player.Ram.SetWPFStuff(grdViewPort, pnlVisuals2D);

                _player.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(Player_ApplyForceAndTorque);

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

                _draggingCamera = new MapObjectChaseVelocity(_cameraBall);
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
                //PlaceWeaponHandles(new Point3D(-20, 8, 0), new Vector3D(1, 0, 0));
                CreateNPCs(6, new Point3D(0, 0, 0));


                #region Player Vision TEST

                //// Build the sensor
                //PartNeuralDNA visionDNA = new PartNeuralDNA() { PartType = SensorVision.PARTTYPE, Position = new Point3D(0, 0, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1) };

                //_playerVision = new SensorVision(_editorOptions, _itemOptions, visionDNA, _map, 10, typeof(TreasureBox));
                //_playerVision.BotToken = _player.Token;

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
                _world.Pause();

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
                if (_clearStatusTime != null && DateTime.Now > _clearStatusTime)
                {
                    statusMessage.Text = "";
                    _clearStatusTime = null;
                }

                #region Player Vision TEST

                //_playerVision.Update(e.ElapsedTime);

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


                _player.Update(e.ElapsedTime);

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
                                //TODO: Spill some of its inventory
                                //TODO: Create a new one

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
                            _map.RemoveItem(treasure);
                            treasure.Dispose();

                            // Add the objects
                            if (contianedTreasure != null)
                            {
                                foreach (object item in contianedTreasure)
                                {
                                    AddTreasure(item, position + Math3D.GetRandomVectorSpherical(radius * 1.25d), velocity + Math3D.GetRandomVectorSpherical(1d));
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
                            _map.RemoveItem(treasure);
                            treasure.Dispose();

                            // Add the objects
                            if (contianedTreasure != null)
                            {
                                foreach (object item in contianedTreasure)
                                {
                                    AddTreasure(item, position + Math3D.GetRandomVectorSpherical(radius * 1.25d), velocity + Math3D.GetRandomVectorSpherical(1d));
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

        private void Player_ApplyForceAndTorque(object sender, BodyApplyForceAndTorqueArgs e)
        {
            try
            {
                //NOTE: _draggingPlayer is handling the force toward the mouse


                //TODO: Make a class that does this
                Vector3D angularVelocity = _player.PhysicsBody.AngularVelocity;

                if (angularVelocity.LengthSquared > 1)
                {
                    _player.PhysicsBody.AngularVelocity = angularVelocity * .9d;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
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

                    #region OLD
                    //case Key.D0:
                    //case Key.NumPad0:
                    //    radWeaponEmpty.IsChecked = true;
                    //    break;

                    //case Key.D1:
                    //case Key.NumPad1:
                    //    radWeaponHandle.IsChecked = true;
                    //    break;

                    //case Key.D2:
                    //case Key.NumPad2:
                    //    radWeaponHandleHeavy.IsChecked = true;
                    //    break;
                    #endregion

                    case Key.Space:
                        PortalVisual overPortal = GetOverPortal();
                        if (overPortal != null)
                        {
                            EnterPortal(overPortal);
                        }
                        break;

                    case Key.Tab:
                    case Key.C:
                    case Key.I:
                        ShowInventory();
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

        private void WeaponRadio_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_isInitialized)
                {
                    return;
                }

                // Create the desired weapon

                Weapon newWeapon = null;

                WeaponHandleMaterial[] materials = new WeaponHandleMaterial[] { WeaponHandleMaterial.Soft_Wood, WeaponHandleMaterial.Hard_Wood, WeaponHandleMaterial.Composite };

                if (radWeaponEmpty.IsChecked.Value)
                {
                }
                else if (radWeaponHandle.IsChecked.Value)
                {
                    #region Small Handle

                    WeaponHandleDNA handle = WeaponHandleDNA.GetRandomDNA(materials[StaticRandom.Next(materials.Length)], WeaponHandleType.Rod, false, 2d, .05d);

                    newWeapon = new Weapon(handle, new Point3D(0, 0, 0), _world, _materialIDs.Weapon);

                    #endregion
                }
                else if (radWeaponHandleHeavy.IsChecked.Value)
                {
                    #region Large Handle

                    WeaponHandleDNA handle = WeaponHandleDNA.GetRandomDNA(materials[StaticRandom.Next(materials.Length)], WeaponHandleType.Rod, false, 3d, .15d);

                    newWeapon = new Weapon(handle, new Point3D(0, 0, 0), _world, _materialIDs.Weapon);

                    #endregion
                }
                else
                {
                    throw new ApplicationException("Unknown weapon type");
                }

                // Swap weapons
                _player.AttachWeapon(newWeapon, Bot.ItemToFrom.Nowhere, Bot.ItemToFrom.Map);
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
                    treasure = new object[] { WeaponHandleDNA.GetRandomDNA() };
                }

                TreasureBox box = new TreasureBox(center + Math3D.GetRandomVectorSpherical2D(radius), 1000, 100, _materialIDs.TreasureBox, _world, new WeaponDamage(), treasure);
                box.PhysicsBody.AngularVelocity = Math3D.GetRandomVectorSpherical(6);

                _keep2D.Add(box);
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
                    DraggingMaxVelocity = _player.DraggingBot.MaxVelocity.Value * .5,
                    DraggingMultiplier = _player.DraggingBot.Multiplier * .5,

                    Parts = new PartDNA[]
                    {
                        new PartNeuralDNA() { PartType = SensorVision.PARTTYPE, Position = new Point3D(0, 0, -1.5), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1) },
                        new PartNeuralDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(0, 0, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1) }
                    }
                };

                BotNPC npc = new BotNPC(dna, center + Math3D.GetRandomVectorSpherical2D(radius), _world, _map, _keep2D, _materialIDs, _viewport, _editorOptions, _itemOptions, _gravity, _dragPlane, true, true);

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

            if (dna is WeaponHandleDNA)
            {
                item = new Weapon((WeaponHandleDNA)dna, position, _world, _materialIDs.Weapon);
                _map.AddItem((Weapon)item);     // there's no real harm in adding to the map here.  The alternative would be a delegate to add the cast type, but that's excessive
            }
            else
            {
                throw new ApplicationException("Unknown type of treasure");
            }

            item.PhysicsBody.Velocity = velocity;
            item.PhysicsBody.AngularVelocity = Math3D.GetRandomVectorSpherical(6);

            _keep2D.Add(item);
            //_map.AddItem(item);     // AddItem is a generic method, and needs the exact type (not IMapObject)
        }

        /// <summary>
        /// This is just for debuggings to see all possible weapon handles
        /// </summary>
        private void PlaceWeaponHandles(Point3D start, Vector3D direction)
        {
            Point3D next = start;

            foreach (WeaponHandleMaterial material in Enum.GetValues(typeof(WeaponHandleMaterial)))
            {
                WeaponHandleDNA handle = WeaponHandleDNA.GetRandomDNA(material);

                Weapon weapon = new Weapon(handle, next, _world, _materialIDs.Weapon);
                weapon.PhysicsBody.AngularVelocity = Math3D.GetRandomVectorSpherical(4);

                _keep2D.Add(weapon);
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
            const double VISUALRADIUS = 1d;
            const double Z = -5;

            double minRadius = 10;
            double maxRadius = _boundryMax.X - (VISUALRADIUS * 4);
            if (minRadius < maxRadius)
            {
                minRadius = 0;
            }

            List<Point3D> points = new List<Point3D>();

            for (int cntr = 0; cntr < count; cntr++)
            {
                #region Create Shop

                Inventory inventory = new Inventory();
                foreach (Weapon weapon in Enumerable.Range(0, StaticRandom.Next(4, 20)).Select(o => new Weapon(WeaponHandleDNA.GetRandomDNA(), new Point3D(), null, _materialIDs.Weapon)))
                {
                    inventory.Weapons.Add(weapon);
                }

                PortalVisual shop = new PortalVisual(new Shop(inventory), VISUALRADIUS);

                if (cntr == 0)
                {
                    // Always put this in the same spot so it's easy to find
                    shop.PositionWorld = new Point3D(-3, -3, Z);
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
                        Vector3D vect = Math3D.GetRandomVectorSpherical2D(minRadius, maxRadius);
                        Point3D pos = new Point3D(vect.X, vect.Y, Z);

                        if (!existing.Any(o => (o.PositionWorld - pos).Length < (VISUALRADIUS + o.Radius) * 7))
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
                TriangleIndexed[] triangles = Math2D.GetDelaunayTriangulation(points.Select(o => o.ToPoint2D()).ToArray(), points.ToArray());
                foreach (var line in TriangleIndexed.GetUniqueLines(triangles))
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

            return UtilityHelper.GetScaledValue_Capped(.03, .3, 0, maxLength, maxLength - distance);        // shorter roads should be wider
        }

        // This adds text to the window
        //TODO: Make sounds
        private void AddStrikeDamage(WeaponDamage damage, bool isPlayer)
        {
            double total = damage.GetDamage();

            AddStrikeDamage(total, damage.Position, isPlayer);
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
            text.FontSize = UtilityHelper.GetScaledValue_Capped(13d, 36d, MINDAMAGE, MAXDAMAGE, damage);
            int textWeight = Convert.ToInt32(Math.Round(UtilityHelper.GetScaledValue_Capped(700, 998, MINDAMAGE, MAXDAMAGE, damage)));
            text.FontWeight = FontWeight.FromOpenTypeWeight(textWeight);
            text.StrokeThickness = UtilityHelper.GetScaledValue_Capped(.75d, 1.6d, MINDAMAGE, MAXDAMAGE, damage);

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

            double seconds = UtilityHelper.GetScaledValue_Capped(.4, 4, MINDAMAGE, MAXDAMAGE * 3, damage);
            _visuals2D.Add(new Visual2DIntermediate(controls, DateTime.Now + TimeSpan.FromSeconds(seconds)));
        }
        private static FontFamily GetBestStrikeFont()
        {
            return UtilityWPF.GetFont(new string[] { "Lucida Console", "Verdana", "Microsoft Sans Serif", "Arial" });
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
        private void ShowInventory()
        {
            _mousePoint = null;
            _world.SimulationSpeed = .05d;       // don't fully pause, that would be cheating :)

            EnsureBackdropPanelCreated();

            _backdropPanel.ClearPanels();

            _backdropPanel.BackdropColors = _player.DNA.ShellColors;

            //TODO: Set appropriate panels




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

        private void LoadFromFile()
        {
            ArcanorumOptions options = UtilityHelper.ReadOptions<ArcanorumOptions>(FILE) ?? new ArcanorumOptions();

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

            UtilityHelper.SaveOptions(options, FILE);
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
