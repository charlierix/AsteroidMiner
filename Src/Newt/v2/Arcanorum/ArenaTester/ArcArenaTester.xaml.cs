using Game.HelperClassesAI;
using Game.HelperClassesCore;
using Game.HelperClassesCore.Threads;
using Game.HelperClassesWPF;
using Game.HelperClassesWPF.Controls3D;
using Game.Newt.v2.Arcanorum.MapObjects;
using Game.Newt.v2.Arcanorum.Parts;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.Controls;
using Game.Newt.v2.GameItems.ShipEditor;
using Game.Newt.v2.GameItems.ShipParts;
using Game.Newt.v2.NewtonDynamics;
using SharpNeat.Domains;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.EvolutionAlgorithms.ComplexityRegulation;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks.Schedulers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Game.Newt.v2.Arcanorum.ArenaTester
{
    public partial class ArcArenaTester : Window
    {
        #region class: UnitTestWorker

        private class UnitTestWorker : IRoundRobinWorker
        {
            private readonly Evaluator2[] _evaluators;

            public UnitTestWorker(Evaluator2[] evaluators)
            {
                _evaluators = evaluators;
            }

            private readonly long _token = TokenGenerator.NextToken();
            public long Token => _token;

            public bool Step1()
            {
                foreach (Evaluator2 evaluator in _evaluators)
                {
                    evaluator.TestEvaluate1();
                }

                return false;
            }
            public bool Step2()
            {
                foreach (Evaluator2 evaluator in _evaluators)
                {
                    evaluator.TestEvaluate2();
                }

                return false;
            }
        }

        #endregion

        #region Declaration Section

        private const string WEAPON_STICK = "simple stick";
        private const string WEAPON_BALL = "with ball";
        private const string WEAPON_BALL2 = "with ball 2";
        private const string WEAPON_AXE = "single axe";
        private const string WEAPON_DOUBLEAXE_BALL = "double sided axe + ball";
        private const string WEAPON_AXE_AXE = "two axes";

        private const string SMACK_DIR_X = "+X";
        private const string SMACK_DIR_Y = "+Y";
        private const string SMACK_DIR_Z = "+Z";
        private const string SMACK_DIR_CIRCLE = "random circle";
        private const string SMACK_DIR_SPHERE = "random sphere";

        private const string SMACK_LOC_POSITION = "center position";
        private const string SMACK_LOC_MASS = "center mass";
        private const string SMACK_LOC_CIRCLE = "random circle";
        private const string SMACK_LOC_SPHERE = "random sphere";

        private const string SMACK_STRENGTH_REALLYWEAK = "really weak";
        private const string SMACK_STRENGTH_WEAK = "weak";
        private const string SMACK_STRENGTH_MED = "medium";
        private const string SMACK_STRENGTH_HARD = "hard";

        private const double BOUNDRYSIZE = 100;
        private const double BOUNDRYSIZEHALF = BOUNDRYSIZE * .5d;

        private Point3D _boundryMin;
        private Point3D _boundryMax;

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

        /// <summary>
        /// This listens to the mouse/keyboard and controls the camera
        /// </summary>
        private TrackBallRoam _trackball = null;

        private List<Visual3D> _shownForces = new List<Visual3D>();
        private List<Visual3D> _debugVisuals = new List<Visual3D>();

        private NeatGenomeView _genomeViewer = null;

        //------------------------- this is the currently running training session
        private TrainingSession _trainingSession = null;

        private Bot _bot = null;
        private BrainNEAT _brainPart = null;
        private DateTime? _lastRefresh = null;
        private ShipViewerWindow _viewer = null;

        private BotTrackingStorage _sessionLog = null;

        //private bool _isInitialized = false;

        #endregion

        #region Constructor

        public ArcArenaTester()
        {
            InitializeComponent();

            Background = SystemColors.ControlBrush;

            cboWeapon.Items.Add(WEAPON_STICK);
            cboWeapon.Items.Add(WEAPON_BALL);
            cboWeapon.Items.Add(WEAPON_BALL2);
            cboWeapon.Items.Add(WEAPON_AXE);
            cboWeapon.Items.Add(WEAPON_DOUBLEAXE_BALL);
            cboWeapon.Items.Add(WEAPON_AXE_AXE);
            cboWeapon.SelectedValue = WEAPON_BALL;

            cboSmackDirection.Items.Add(SMACK_DIR_X);
            cboSmackDirection.Items.Add(SMACK_DIR_Y);
            cboSmackDirection.Items.Add(SMACK_DIR_Z);
            cboSmackDirection.Items.Add(SMACK_DIR_CIRCLE);
            cboSmackDirection.Items.Add(SMACK_DIR_SPHERE);
            cboSmackDirection.SelectedValue = SMACK_DIR_Z;

            cboSmackLocation.Items.Add(SMACK_LOC_POSITION);
            cboSmackLocation.Items.Add(SMACK_LOC_MASS);
            cboSmackLocation.Items.Add(SMACK_LOC_CIRCLE);
            cboSmackLocation.Items.Add(SMACK_LOC_SPHERE);
            cboSmackLocation.SelectedValue = SMACK_LOC_MASS;

            cboSmackStrength.Items.Add(SMACK_STRENGTH_REALLYWEAK);
            cboSmackStrength.Items.Add(SMACK_STRENGTH_WEAK);
            cboSmackStrength.Items.Add(SMACK_STRENGTH_MED);
            cboSmackStrength.Items.Add(SMACK_STRENGTH_HARD);
            cboSmackStrength.SelectedValue = SMACK_STRENGTH_REALLYWEAK;
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

                _world.SetCollisionBoundry(_boundryMin, _boundryMax);

                //TODO: Only draw the boundry lines if options say to

                // Draw the lines
                ScreenSpaceLines3D boundryLines = new ScreenSpaceLines3D(true)
                {
                    Thickness = 1d,
                    Color = Colors.Silver,
                };

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

                //TODO: Uncomment these
                // Collisions
                //_materialManager.RegisterCollisionEvent(_materialIDs.Bot, _materialIDs.Bot, Collision_BotBot);
                //_materialManager.RegisterCollisionEvent(_materialIDs.Bot, _materialIDs.Weapon, Collision_BotWeapon);
                //_materialManager.RegisterCollisionEvent(_materialIDs.Weapon, _materialIDs.Weapon, Collision_WeaponWeapon);
                ////_materialManager.RegisterCollisionEvent(_materialIDs.Bot, _materialIDs.Wall, Collision_BotWall);
                //_materialManager.RegisterCollisionEvent(_materialIDs.Weapon, _materialIDs.Wall, Collision_WeaponWall);
                //_materialManager.RegisterCollisionEvent(_materialIDs.Weapon, _materialIDs.TreasureBox, Collision_WeaponTreasureBox);
                //_materialManager.RegisterCollisionEvent(_materialIDs.Bot, _materialIDs.TreasureBox, Collision_BotTreasureBox);

                #endregion
                #region Trackball

                // Trackball
                _trackball = new TrackBallRoam(_camera)
                {
                    KeyPanScale = 15d,
                    EventSource = grdViewPort,      //NOTE:  If this control doesn't have a background color set, the trackball won't see events (I think transparent is ok, just not null)
                    AllowZoomOnMouseWheel = true,
                    ShouldHitTestOnOrbit = true,
                };

                _trackball.Mappings.AddRange(TrackBallMapping.GetPrebuilt(TrackBallMapping.PrebuiltMapping.MouseComplete_NoLeft));
                _trackball.Mappings.AddRange(TrackBallMapping.GetPrebuilt(TrackBallMapping.PrebuiltMapping.Keyboard_ASDW_In));

                //_trackball.UserMovedCamera += new EventHandler<UserMovedCameraArgs>(Trackball_UserMovedCamera);
                //_trackball.GetOrbitRadius += new EventHandler<GetOrbitRadiusArgs>(Trackball_GetOrbitRadius);

                #endregion
                #region Map

                _map = new Map(_viewport, null, _world)
                {
                    ShouldBuildSnapshots = true,
                    //ShouldShowSnapshotLines = false,
                };

                #endregion
                #region UpdateManager

                //TODO: UpdateManager needs to inspect types as they are added to the map (map's ItemAdded event)
                _updateManager = new UpdateManager(
                    new Type[] { typeof(Bot) },
                    new Type[] { typeof(Bot) },
                    _map);

                #endregion
                #region Keep 2D

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

                _genomeViewer = new NeatGenomeView();
                nnViewerHost.Child = _genomeViewer;

                _world.UnPause();

                //_isInitialized = true;
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

                if (_trainingSession != null)
                {
                    _trainingSession.EA.Dispose();
                }

                _updateManager.Dispose();

                _keep2D.Dispose();

                _map.Dispose();		// this will dispose the physics bodies
                _map = null;

                _world.Dispose();
                _world = null;

                if (_viewer != null)
                {
                    _viewer.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Viewer_Closed(object sender, EventArgs e)
        {
            _viewer = null;
        }

        private void World_Updating(object sender, WorldUpdatingArgs e)
        {
            const double THICKNESS = .02;
            try
            {
                _updateManager.Update_MainThread(e.ElapsedTime);

                #region refill containers

                if (_bot != null)
                {
                    if (_bot.Fuel != null)
                    {
                        _bot.Fuel.QuantityCurrent = _bot.Fuel.QuantityMax;
                    }

                    if (_bot.Energy != null)
                    {
                        _bot.Energy.QuantityCurrent = _bot.Energy.QuantityMax;
                    }

                    if (_bot.Plasma != null)
                    {
                        _bot.Plasma.QuantityCurrent = _bot.Plasma.QuantityMax;
                    }
                }

                #endregion

                #region show force/torque

                _viewport.Children.RemoveAll(_shownForces);
                _shownForces.Clear();

                if (chkShowForces.IsChecked.Value)
                {
                    foreach (IMapObject item in _map.GetAllItems())
                    {
                        // Force
                        Point3D from = item.PositionWorld;
                        Point3D to = from + item.PhysicsBody.DirectionToWorld(item.PhysicsBody.ForceCached);
                        Visual3D visual = Debug3DWindow.GetLine(from, to, THICKNESS, Colors.Chartreuse);
                        _shownForces.Add(visual);
                        _viewport.Children.Add(visual);

                        //to = from + item.PhysicsBody.ForceCached;
                        //visual = Debug3DWindow.GetLine(from, to, THICKNESS, Colors.ForestGreen);
                        //_shownForces.Add(visual);
                        //_viewport.Children.Add(visual);

                        // Torque
                        to = from + item.PhysicsBody.DirectionToWorld(item.PhysicsBody.TorqueCached);
                        visual = Debug3DWindow.GetLine(from, to, THICKNESS, Colors.DeepPink);
                        _shownForces.Add(visual);
                        _viewport.Children.Add(visual);

                        //to = from + item.PhysicsBody.TorqueCached;
                        //visual = Debug3DWindow.GetLine(from, to, THICKNESS, Colors.Maroon);
                        //_shownForces.Add(visual);
                        //_viewport.Children.Add(visual);
                    }
                }

                #endregion
                #region debug visuals

                _viewport.Children.RemoveAll(_debugVisuals);
                _debugVisuals.Clear();

                if (chkDebugVisuals.IsChecked.Value)
                {
                    foreach (IMapObject item in _map.GetAllItems())
                    {
                        // Copied from Debug3DWindow.GetAxisLines

                        Point3D centerPoint = item.PositionWorld;
                        double length = item.Radius * 3;

                        var lines = new[]
                        {
                            Debug3DWindow.GetLine(centerPoint, centerPoint + item.PhysicsBody.DirectionToWorld(new Vector3D(length, 0, 0)), THICKNESS, UtilityWPF.ColorFromHex(Debug3DWindow.AXISCOLOR_X)),
                            Debug3DWindow.GetLine(centerPoint, centerPoint + item.PhysicsBody.DirectionToWorld(new Vector3D(0, length, 0)), THICKNESS, UtilityWPF.ColorFromHex(Debug3DWindow.AXISCOLOR_Y)),
                            Debug3DWindow.GetLine(centerPoint, centerPoint + item.PhysicsBody.DirectionToWorld(new Vector3D(0, 0, length)), THICKNESS, UtilityWPF.ColorFromHex(Debug3DWindow.AXISCOLOR_Z)),
                        };

                        _debugVisuals.AddRange(lines);
                        _viewport.Children.AddRange(lines);
                    }
                }

                #endregion
                #region winning genome stats

                if (_bot == null)
                {
                    lblCurrentWinnerStats.Text = "";
                }
                else
                {
                    lblCurrentWinnerStats.Text = string.Format
                    (
                        "distance: {0}\r\nspeed: {1}",
                        _bot.PositionWorld.ToVector().Length.ToStringSignificantDigits(3),
                        _bot.VelocityWorld.Length.ToStringSignificantDigits(3)
                    );
                }

                #endregion

                if (chkKeep2D.IsChecked.Value)
                {
                    _keep2D.Update();
                }
                else
                {
                    _keep2D.StopChasing();
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EA_UpdateEvent(object sender, EventArgs e)
        {
            const int SIZE = 50;

            try
            {
                //NOTE: This can be called from the main thread or the worker thread

                if (_trainingSession == null)
                {
                    return;
                }

                BotTrackingRun[] snapshots = null;
                if (_sessionLog != null)
                {
                    snapshots = _sessionLog.GetLatestGenerationSnapshots();
                    _sessionLog.IncrementGeneration();
                }

                Action action = () =>
                {
                    if (_trainingSession == null)
                    {
                        return;
                    }

                    eaStatus.Update(_trainingSession.EA);

                    #region snapshots

                    if (snapshots != null && snapshots.Length > 0)
                    {
                        snapshots = snapshots.
                            OrderByDescending(o => o.Score._fitness).
                            Take(10).
                            ToArray();

                        panelLogs.Children.Clear();

                        foreach (var snapshot in snapshots)
                        {
                            Grid grid = new Grid()
                            {
                                Margin = new Thickness(4),
                            };

                            grid.Children.Add(new Border()
                            {
                                BorderBrush = new SolidColorBrush(UtilityWPF.ColorFromHex("20000000")),
                                BorderThickness = new Thickness(1),
                                Child = DrawSnapshot(snapshot, SIZE),
                            });

                            grid.Children.Add(new TextBlock()
                            {
                                Foreground = new SolidColorBrush(UtilityWPF.ColorFromHex("80000000")),
                                HorizontalAlignment = HorizontalAlignment.Right,
                                VerticalAlignment = VerticalAlignment.Bottom,
                                FontSize = 8,
                                Margin = new Thickness(3),
                                Text = snapshot.Score._fitness.ToStringSignificantDigits(2),
                            });

                            panelLogs.Children.Add(grid);
                        }
                    }

                    #endregion

                    #region reset winning genome

                    // Periodically show the winning genome driving a bot
                    NeatGenome winningGenome = _trainingSession?.EA?.CurrentChampGenome;
                    DateTime now = DateTime.UtcNow;
                    if (winningGenome != null && _bot != null && _brainPart != null && (_lastRefresh == null || (now - _lastRefresh.Value).TotalSeconds > 15))
                    {
                        ExperimentInitArgs_Activation activation = TrainingSession.GetActivationFunctionArgs();
                        IBlackBox phenome = ExperimentNEATBase.GetBlackBox(winningGenome, activation);

                        _brainPart.SetPhenome(phenome, winningGenome, activation);

                        _bot.PhysicsBody.Position = new Point3D();
                        _bot.PhysicsBody.Velocity = new Vector3D();
                        _bot.PhysicsBody.Rotation = Quaternion.Identity;
                        _bot.PhysicsBody.AngularVelocity = new Vector3D();

                        _lastRefresh = now;

                        Point? viewerPos = null;
                        if (_viewer != null)
                        {
                            viewerPos = new Point(_viewer.Left, _viewer.Top);
                            _viewer.Close();
                        }

                        _viewer = new ShipViewerWindow(_bot, this);
                        _viewer.SetColorTheme_Light();

                        if (viewerPos != null)
                        {
                            _viewer.Left = viewerPos.Value.X;
                            _viewer.Top = viewerPos.Value.Y;
                        }

                        _viewer.Closed += Viewer_Closed;
                        _viewer.Show();

                        _genomeViewer.RefreshView(winningGenome);
                    }

                    #endregion
                };

                Dispatcher.BeginInvoke(action);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FullRun1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EndTraining();

                ShipDNA dna = GetBotDNA();

                #region ship extra args

                ShipExtraArgs extra_offline = new ShipExtraArgs()
                {
                    Options = new EditorOptions(),
                    ItemOptions = new ItemOptionsArco(),
                    NeuralPoolManual = new NeuralPool_ManualTick(),
                };
                extra_offline.Gravity = new GravityFieldUniform() { Gravity = new Vector3D(0, -((ItemOptionsArco)extra_offline.ItemOptions).Gravity, 0) };
                extra_offline.ItemOptions.Impulse_RotationStrengthRatio = 0;

                ShipExtraArgs extra_online = new ShipExtraArgs()
                {
                    Options = new EditorOptions(),
                    ItemOptions = new ItemOptionsArco(),
                    //NeuralPoolManual = new NeuralPool_ManualTick(),
                };
                extra_online.Gravity = new GravityFieldUniform() { Gravity = new Vector3D(0, -((ItemOptionsArco)extra_online.ItemOptions).Gravity, 0) };
                extra_online.ItemOptions.Impulse_RotationStrengthRatio = 0;

                #endregion
                #region create bot

                // Need to create a bot that will wire up neurons, then save off that dna.  Otherwise, each room's bot will have a random
                // wiring, and the training will be meaningless

                ShipCoreArgs core = new ShipCoreArgs()
                {
                    World = _world,
                    Material_Ship = _materialIDs.Bot,
                    Map = new Map(null, extra_online.CameraPool, _world),
                };

                //BotConstructor_Events events = new BotConstructor_Events();
                //events.

                // Create the bot
                BotConstruction_Result construction = BotConstructor.ConstructBot(dna, core, extra_online);
                _bot = new Bot(construction);

                dna = _bot.GetNewDNA();

                _brainPart = _bot.Parts.FirstOrDefault(o => o is BrainNEAT) as BrainNEAT;
                if (_brainPart == null)
                {
                    throw new ApplicationException("There needs to be a brain part");
                }

                foreach (PartBase part in _bot.Parts)
                {
                    if (part is SensorHoming partHoming)
                    {
                        partHoming.HomePoint = new Point3D(0, 0, 0);
                        partHoming.HomeRadius = (TrainingSession.ROOMSIZE / 2d) * Evaluator3.MULT_HOMINGSIZE;
                    }
                }

                int inputCount = _brainPart.Neruons_Writeonly.Count();
                int outputCount = _brainPart.Neruons_Readonly.Count();

                #endregion

                _keep2D.Add(_bot, false);
                _map.AddItem(_bot);
                //_bot.Dispose();
                //_bot = null;

                _sessionLog = new BotTrackingStorage();

                _trainingSession = new TrainingSession
                (
                    dna,
                    extra_offline,
                    inputCount,
                    outputCount,
                    (w, r) => new Evaluator3(w, r, _sessionLog, TrainingSession.GetActivationFunctionArgs(), maxEvalTime: 100)      // if the time is too short, the bot can get away with traveling slowly toward the wall.  The test period ends before it hits the wall and it gets a fairly good score
                );

                //TODO: TickGenomeListEvaluator needs to reference RoundRobinManager?
                //It's currently just running on the main thread
                //
                //The Evaluate method gets called from the main thread.  ParallelGenomeListEvaluator does a parallel.foreach on the genome list
                //
                //The tick needs to hand a list to round robin, then wait until the list has been tested

                _trainingSession.EA.UpdateEvent += EA_UpdateEvent;

                _trainingSession.EA.StartContinue();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void FullRun2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EndTraining();

                #region ship extra args

                ShipExtraArgs extra_offline = ArcBot2.GetArgs_Extra();
                extra_offline.NeuralPoolManual = new NeuralPool_ManualTick();

                ShipExtraArgs extra_online = ArcBot2.GetArgs_Extra();

                #endregion
                #region ship core args

                ShipCoreArgs core = new ShipCoreArgs()
                {
                    World = _world,
                    Material_Ship = _materialIDs.Bot,
                    Map = new Map(null, extra_online.CameraPool, _world),
                };

                #endregion

                WeaponDNA weaponDNA = new WeaponDNA()
                {
                    UniqueID = Guid.NewGuid(),
                    Handle = WeaponHandleDNA.GetRandomDNA(),
                };
                weaponDNA.Handle.HandleMaterial = WeaponHandleMaterial.Hard_Wood;

                Weapon weapon = new Weapon(weaponDNA, new Point3D(), _world, _materialIDs.Weapon);

                #region create bot

                // Need to create a bot that will wire up neurons, then save off that dna.  Otherwise, each room's bot will have a random
                // wiring, and the training will be meaningless

                int level = 5;      //StaticRandom.Next(1, 30);

                // Create the bot
                var construction = ArcBot2.GetConstruction(level, core, extra_online, _dragPlane, true);

                _bot = new ArcBot2(construction, _materialIDs, _keep2D, _viewport);

                ShipDNA dna = _bot.GetNewDNA();

                ((ArcBot2)_bot).AttachWeapon(weapon);

                _brainPart = _bot.Parts.FirstOrDefault(o => o is BrainNEAT) as BrainNEAT;
                if (_brainPart == null)
                {
                    throw new ApplicationException("There needs to be a brain part");
                }

                foreach (PartBase part in _bot.Parts)
                {
                    if (part is SensorHoming partHoming)
                    {
                        partHoming.HomePoint = new Point3D(0, 0, 0);
                        partHoming.HomeRadius = (TrainingSession.ROOMSIZE / 2d) * Evaluator3.MULT_HOMINGSIZE;
                    }
                }

                int inputCount = _brainPart.Neruons_Writeonly.Count();
                int outputCount = _brainPart.Neruons_Readonly.Count();

                #endregion

                _keep2D.Add(_bot, false);
                _map.AddItem(_bot);
                //_bot.Dispose();
                //_bot = null;

                _sessionLog = new BotTrackingStorage();

                double weaponSpeed_min = 1;
                double weaponSpeed_max = 2;

                _trainingSession = new TrainingSession
                (
                    dna,
                    extra_offline,
                    inputCount,
                    outputCount,
                    (w, r) => new Evaluator_WeaponSpin(w, r, _sessionLog, TrainingSession.GetActivationFunctionArgs(), weaponSpeed_min, weaponSpeed_max, maxEvalTime: 100),      // if the time is too short, the bot can get away with traveling slowly toward the wall.  The test period ends before it hits the wall and it gets a fairly good score
                    roomCount: 3
                );

                //NOTE: This event fires on a different thread
                _trainingSession.RequestCustomBot = (c, k, d, m) =>
                {
                    var con = ArcBot2.GetConstruction(level, c, extra_offline, d, true);
                    ArcBot2 bt2 = new ArcBot2(con, m, k, null);

                    Weapon wep = new Weapon(weaponDNA, new Point3D(), c.World, m.Weapon);
                    bt2.AttachWeapon(wep);

                    return bt2;
                };

                //TODO: TickGenomeListEvaluator needs to reference RoundRobinManager?
                //It's currently just running on the main thread
                //
                //The Evaluate method gets called from the main thread.  ParallelGenomeListEvaluator does a parallel.foreach on the genome list
                //
                //The tick needs to hand a list to round robin, then wait until the list has been tested

                _trainingSession.EA.UpdateEvent += EA_UpdateEvent;

                _trainingSession.EA.StartContinue();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EndTraining();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
        #region Event Listeners - tests

        private void EvaluatorPrototypeRun_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // This will deal with one bot, one phenotype tester at a time

                #region physics

                #region misc

                ItemOptionsArco itemOptions = new ItemOptionsArco();

                EditorOptions editorOptions = new EditorOptions();

                GravityFieldUniform gravity = new GravityFieldUniform() { Gravity = new Vector3D(0, -itemOptions.Gravity, 0) };

                #endregion

                #region init world

                double boundrySizeHalf = 50;

                Point3D boundryMin = new Point3D(-boundrySizeHalf, -boundrySizeHalf, -boundrySizeHalf);
                Point3D boundryMax = new Point3D(boundrySizeHalf, boundrySizeHalf, boundrySizeHalf);

                World world = new World(false);

                world.SetCollisionBoundry(boundryMin, boundryMax);

                //// Draw the lines
                //ScreenSpaceLines3D boundryLines = new ScreenSpaceLines3D(true)
                //{
                //    Thickness = 1d,
                //    Color = Colors.Silver,
                //};

                //boundryLines.AddLine(new Point3D(-BOUNDRYSIZEHALF, -BOUNDRYSIZEHALF, 0), new Point3D(BOUNDRYSIZEHALF, -BOUNDRYSIZEHALF, 0));
                //boundryLines.AddLine(new Point3D(BOUNDRYSIZEHALF, -BOUNDRYSIZEHALF, 0), new Point3D(BOUNDRYSIZEHALF, BOUNDRYSIZEHALF, 0));
                //boundryLines.AddLine(new Point3D(BOUNDRYSIZEHALF, BOUNDRYSIZEHALF, 0), new Point3D(-BOUNDRYSIZEHALF, BOUNDRYSIZEHALF, 0));
                //boundryLines.AddLine(new Point3D(-BOUNDRYSIZEHALF, BOUNDRYSIZEHALF, 0), new Point3D(-BOUNDRYSIZEHALF, -BOUNDRYSIZEHALF, 0));

                //_viewport.Children.Add(boundryLines);

                #endregion
                #region materials

                MaterialManager materialManager = new MaterialManager(world);
                MaterialIDs materialIDs = new MaterialIDs();

                // Wall
                Game.Newt.v2.NewtonDynamics.Material material = new Game.Newt.v2.NewtonDynamics.Material();
                material.Elasticity = ItemOptionsArco.ELASTICITY_WALL;
                materialIDs.Wall = materialManager.AddMaterial(material);

                // Bot
                material = new Game.Newt.v2.NewtonDynamics.Material();
                materialIDs.Bot = materialManager.AddMaterial(material);

                // Bot Ram
                material = new Game.Newt.v2.NewtonDynamics.Material();
                material.Elasticity = ItemOptionsArco.ELASTICITY_BOTRAM;
                materialIDs.BotRam = materialManager.AddMaterial(material);

                // Exploding Bot
                material = new Game.Newt.v2.NewtonDynamics.Material();
                material.IsCollidable = false;
                materialIDs.ExplodingBot = materialManager.AddMaterial(material);

                // Weapon
                material = new Game.Newt.v2.NewtonDynamics.Material();
                materialIDs.Weapon = materialManager.AddMaterial(material);

                // Treasure Box
                material = new Game.Newt.v2.NewtonDynamics.Material();
                materialIDs.TreasureBox = materialManager.AddMaterial(material);

                //TODO: Uncomment these
                // Collisions
                //_materialManager.RegisterCollisionEvent(_materialIDs.Bot, _materialIDs.Bot, Collision_BotBot);
                //_materialManager.RegisterCollisionEvent(_materialIDs.Bot, _materialIDs.Weapon, Collision_BotWeapon);
                //_materialManager.RegisterCollisionEvent(_materialIDs.Weapon, _materialIDs.Weapon, Collision_WeaponWeapon);
                ////_materialManager.RegisterCollisionEvent(_materialIDs.Bot, _materialIDs.Wall, Collision_BotWall);
                //_materialManager.RegisterCollisionEvent(_materialIDs.Weapon, _materialIDs.Wall, Collision_WeaponWall);
                //_materialManager.RegisterCollisionEvent(_materialIDs.Weapon, _materialIDs.TreasureBox, Collision_WeaponTreasureBox);
                //_materialManager.RegisterCollisionEvent(_materialIDs.Bot, _materialIDs.TreasureBox, Collision_BotTreasureBox);

                #endregion
                #region map

                Map map = new Map(null, null, world)
                {
                    ShouldBuildSnapshots = true,
                    //ShouldShowSnapshotLines = false,
                };

                #endregion
                #region keep 2D

                //TODO: drag plane should either be a plane or a large cylinder, based on the current (level|scene|stage|area|arena|map|place|region|zone)

                // This game is 3D emulating 2D, so always have the mouse go to the XY plane
                DragHitShape dragPlane = new DragHitShape();
                dragPlane.SetShape_Plane(new Triangle(new Point3D(-1, -1, 0), new Point3D(1, -1, 0), new Point3D(0, 1, 0)));

                // This will keep objects onto that plane using forces (not velocities)
                KeepItems2D keep2D = new KeepItems2D
                {
                    SnapShape = dragPlane,
                };

                #endregion
                #region update manager

                //TODO: UpdateManager needs to inspect types as they are added to the map (map's ItemAdded event)
                UpdateManager updateManager = new UpdateManager(
                    new Type[] { typeof(Bot) },
                    new Type[] { typeof(Bot) },
                    map,
                    useTimer: false);

                #endregion

                world.Updating += new EventHandler<WorldUpdatingArgs>((s, e1) =>
                {
                    updateManager.Update_MainThread(e1.ElapsedTime);
                    keep2D.Update();
                });

                world.UnPause();

                #endregion

                #region dna

                double halfSqrt2 = Math.Sqrt(2) / 2;

                // Create bot dna (remember the unique ID of the brainneat part)
                ShipPartDNA[] parts = new ShipPartDNA[]
                {
                    new ShipPartDNA() { PartType = SensorGravity.PARTTYPE, Position = new Point3D(0.0940410020016388,0.118721624747733,0.518988389824055), Orientation = new Quaternion(halfSqrt2,0,0,halfSqrt2), Scale = new Vector3D(1, 1, 1) },
                    //new ShipPartDNA() { PartType = SensorCollision.PARTTYPE, Position = new Point3D(-0.147082195253193,0.121721926231451,0.519004887758403), Orientation = new Quaternion(halfSqrt2,0,0,halfSqrt2), Scale = new Vector3D(1, 1, 1) },
                    new ShipPartDNA() { PartType = SensorHoming.PARTTYPE, Position = new Point3D(-0.149097495961565,-0.117096583438536,0.517691682363825), Orientation = new Quaternion(halfSqrt2,0,0,halfSqrt2), Scale = new Vector3D(1, 1, 1) },
                    new ShipPartDNA() { PartType = SensorVelocity.PARTTYPE, Position = new Point3D(0.0926218471349272,-0.123346967540649,0.517657313008903), Orientation = new Quaternion(halfSqrt2,0,0,halfSqrt2), Scale = new Vector3D(1, 1, 1) },

                    new ShipPartDNA() { PartType = EnergyTank.PARTTYPE, Position = new Point3D(0,0,0.307466813523783), Orientation = Quaternion.Identity, Scale = new Vector3D(1,1,0.149239910187544) },

                    new CameraHardCodedDNA() { PartType = CameraHardCoded.PARTTYPE, Position = new Point3D(0.249826620626459,0.20802214411661,-0.00333407878894652), Orientation = new Quaternion(-0.663257774324101,-0.239984678742854,-0.666577701043659,0.241185918408767), Scale = new Vector3D(1,1,1) },
                    new CameraHardCodedDNA() { PartType = CameraHardCoded.PARTTYPE, Position = new Point3D(0.262012369728025,-0.145863634157708,-0.00333407878894495), Orientation = new Quaternion(0.682710603553056,-0.177227969093181,0.686127901113979,0.178115081001736), Scale = new Vector3D(1,1,1) },
                    new CameraHardCodedDNA() { PartType = CameraHardCoded.PARTTYPE, Position = new Point3D(-0.303779554831675,-0.0127691984878863,-0.00333407878894622), Orientation = new Quaternion(0.0148144542160312,-0.705183701725268,0.0148886077416918,0.708713488037898), Scale = new Vector3D(1,1,1) },
                    new CameraHardCodedDNA() { PartType = CameraHardCoded.PARTTYPE, Position = new Point3D(-0.0661289759910267,-0.302046076168234,-0.00333407878894601), Orientation = new Quaternion(0.442211473505322,-0.549502078187993,0.444424956322137,0.552252602497621), Scale = new Vector3D(1,1,1) },
                    new CameraHardCodedDNA() { PartType = CameraHardCoded.PARTTYPE, Position = new Point3D(-0.110459969641624,0.279162567591854,-0.00333407878894656), Orientation = new Quaternion(-0.396521198492132,-0.583330489841766,-0.398505979331054,0.586250341752333), Scale = new Vector3D(1,1,1) },

                    // If there are more than one, give each a unique guid to know which to train (only want to train one at a time)
                    new BrainNEATDNA() { PartType = BrainNEAT.PARTTYPE, Position = new Point3D(0,0,-0.612726700748439), Orientation = Quaternion.Identity, Scale = new Vector3D(1,1,1) },

                    //new ShipPartDNA() { PartType = DirectionControllerRing.PARTTYPE, Position = new Point3D(0,0,-1.21923909614498), Orientation = Quaternion.Identity, Scale = new Vector3D(1,1,1) },
                    new ImpulseEngineDNA() { PartType = ImpulseEngine.PARTTYPE, Position = new Point3D(0,0,-1.21923909614498), Orientation = Quaternion.Identity, Scale = new Vector3D(1,1,1), ImpulseEngineType = ImpulseEngineType.Translate },
                };

                ShipDNA dna = ShipDNA.Create("Arcbot Attempt 1", parts);

                #endregion
                #region bot

                ShipCoreArgs core = new ShipCoreArgs()
                {
                    World = world,
                    Material_Ship = materialIDs.Bot,
                    Map = map,
                };

                ShipExtraArgs extra = new ShipExtraArgs()
                {
                    Options = editorOptions,
                    ItemOptions = itemOptions,
                };

                //BotConstructor_Events events = new BotConstructor_Events();
                //events.

                // Create the bot
                BotConstruction_Result construction = BotConstructor.ConstructBot(dna, core, extra);
                Bot bot = new Bot(construction);


                // Find the brainneat part
                BrainNEAT brainPart = bot.Parts.FirstOrDefault(o => o is BrainNEAT) as BrainNEAT;
                if (brainPart == null)
                {
                    throw new ApplicationException("Didn't find BrainNEAT part");
                }

                SensorHoming[] homingParts = bot.Parts.
                    Where(o => o is SensorHoming).
                    Select(o => (SensorHoming)o).
                    ToArray();

                if (homingParts.Length == 0)
                {
                    throw new ApplicationException("Didn't find SensorHoming part");
                }

                #endregion

                #region ai

                #region experiment args

                ExperimentInitArgs experimentArgs = new ExperimentInitArgs()
                {
                    Description = "prototype",
                    InputCount = brainPart.Neruons_Writeonly.Count(),
                    OutputCount = brainPart.Neruons_Readonly.Count(),
                    IsHyperNEAT = false,
                    PopulationSize = 15,
                    SpeciesCount = 2,
                    Activation = new ExperimentInitArgs_Activation_CyclicFixedTimesteps()
                    {
                        TimestepsPerActivation = 3,
                        FastFlag = true
                    },
                    Complexity_RegulationStrategy = ComplexityCeilingType.Absolute,
                    Complexity_Threshold = 150,
                };

                #endregion

                Evaluator1_FAIL phenomeEvaluator = new Evaluator1_FAIL(bot, brainPart, homingParts, new Point3D(0, 0, 0), experimentArgs.Activation);

                ExperimentNEATBase experiment = new ExperimentNEATBase();

                // Use the overload that takes the tick phenome
                experiment.Initialize("prototype", experimentArgs, new[] { phenomeEvaluator }, new RoundRobinManager(new StaTaskScheduler(1)), () => world.Update());

                NeatEvolutionAlgorithm<NeatGenome> ea = experiment.CreateEvolutionAlgorithm();

                // look in AnticipatePositionWindow for an example.  The update event just displays stats
                //ea.UpdateEvent += EA_UpdateEvent;
                //ea.PausedEvent += EA_PausedEvent;

                #endregion

                map.AddItem(bot);

                ea.StartContinue();

                #region testing manual calls

                //// run for a few generations
                //updateManager.Update_AnyThread();
                //world.Update();

                //updateManager.Update_AnyThread();
                //updateManager.Update_AnyThread();
                //updateManager.Update_AnyThread();

                //world.Update();

                //updateManager.Update_AnyThread();

                #endregion

                ea.Stop();

                world.Pause();

                updateManager.Dispose();
                map.Dispose();
                world.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ArenaAccessorPrototypeRun_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                #region dna

                double halfSqrt2 = Math.Sqrt(2) / 2;

                // Create bot dna (remember the unique ID of the brainneat part)
                ShipPartDNA[] parts = new ShipPartDNA[]
                {
                    new ShipPartDNA() { PartType = SensorGravity.PARTTYPE, Position = new Point3D(0.0940410020016388,0.118721624747733,0.518988389824055), Orientation = new Quaternion(halfSqrt2,0,0,halfSqrt2), Scale = new Vector3D(1, 1, 1) },
                    //new ShipPartDNA() { PartType = SensorCollision.PARTTYPE, Position = new Point3D(-0.147082195253193,0.121721926231451,0.519004887758403), Orientation = new Quaternion(halfSqrt2,0,0,halfSqrt2), Scale = new Vector3D(1, 1, 1) },
                    new ShipPartDNA() { PartType = SensorHoming.PARTTYPE, Position = new Point3D(-0.149097495961565,-0.117096583438536,0.517691682363825), Orientation = new Quaternion(halfSqrt2,0,0,halfSqrt2), Scale = new Vector3D(1, 1, 1) },
                    new ShipPartDNA() { PartType = SensorVelocity.PARTTYPE, Position = new Point3D(0.0926218471349272,-0.123346967540649,0.517657313008903), Orientation = new Quaternion(halfSqrt2,0,0,halfSqrt2), Scale = new Vector3D(1, 1, 1) },

                    new ShipPartDNA() { PartType = EnergyTank.PARTTYPE, Position = new Point3D(0,0,0.307466813523783), Orientation = Quaternion.Identity, Scale = new Vector3D(1,1,0.149239910187544) },

                    new CameraHardCodedDNA() { PartType = CameraHardCoded.PARTTYPE, Position = new Point3D(0.249826620626459,0.20802214411661,-0.00333407878894652), Orientation = new Quaternion(-0.663257774324101,-0.239984678742854,-0.666577701043659,0.241185918408767), Scale = new Vector3D(1,1,1) },
                    new CameraHardCodedDNA() { PartType = CameraHardCoded.PARTTYPE, Position = new Point3D(0.262012369728025,-0.145863634157708,-0.00333407878894495), Orientation = new Quaternion(0.682710603553056,-0.177227969093181,0.686127901113979,0.178115081001736), Scale = new Vector3D(1,1,1) },
                    new CameraHardCodedDNA() { PartType = CameraHardCoded.PARTTYPE, Position = new Point3D(-0.303779554831675,-0.0127691984878863,-0.00333407878894622), Orientation = new Quaternion(0.0148144542160312,-0.705183701725268,0.0148886077416918,0.708713488037898), Scale = new Vector3D(1,1,1) },
                    new CameraHardCodedDNA() { PartType = CameraHardCoded.PARTTYPE, Position = new Point3D(-0.0661289759910267,-0.302046076168234,-0.00333407878894601), Orientation = new Quaternion(0.442211473505322,-0.549502078187993,0.444424956322137,0.552252602497621), Scale = new Vector3D(1,1,1) },
                    new CameraHardCodedDNA() { PartType = CameraHardCoded.PARTTYPE, Position = new Point3D(-0.110459969641624,0.279162567591854,-0.00333407878894656), Orientation = new Quaternion(-0.396521198492132,-0.583330489841766,-0.398505979331054,0.586250341752333), Scale = new Vector3D(1,1,1) },

                    // If there are more than one, give each a unique guid to know which to train (only want to train one at a time)
                    new BrainNEATDNA() { PartType = BrainNEAT.PARTTYPE, Position = new Point3D(0,0,-0.612726700748439), Orientation = Quaternion.Identity, Scale = new Vector3D(1,1,1) },

                    //new ShipPartDNA() { PartType = DirectionControllerRing.PARTTYPE, Position = new Point3D(0,0,-1.21923909614498), Orientation = Quaternion.Identity, Scale = new Vector3D(1,1,1) },
                    new ImpulseEngineDNA() { PartType = ImpulseEngine.PARTTYPE, Position = new Point3D(0,0,-1.21923909614498), Orientation = Quaternion.Identity, Scale = new Vector3D(1,1,1), ImpulseEngineType = ImpulseEngineType.Translate },
                };

                ShipDNA dna = ShipDNA.Create("Arcbot Attempt 1", parts);

                #endregion

                #region misc

                ItemOptionsArco itemOptions = new ItemOptionsArco();

                EditorOptions editorOptions = new EditorOptions();

                GravityFieldUniform gravity = new GravityFieldUniform() { Gravity = new Vector3D(0, -itemOptions.Gravity, 0) };

                #endregion

                #region arena accessor (instantiate)

                // Give it the map and instructions about room sizes/locations
                ArenaAccessor_FAIL accessor = new ArenaAccessor_FAIL
                (
                    3, 100, 10,
                    false, false,
                    new Type[] { typeof(Bot) },
                    new Type[] { typeof(Bot) }
                );

                #endregion

                #region materials

                MaterialManager materialManager = new MaterialManager(accessor.World);
                MaterialIDs materialIDs = new MaterialIDs();

                // Wall
                Game.Newt.v2.NewtonDynamics.Material material = new Game.Newt.v2.NewtonDynamics.Material();
                material.Elasticity = ItemOptionsArco.ELASTICITY_WALL;
                materialIDs.Wall = materialManager.AddMaterial(material);

                // Bot
                material = new Game.Newt.v2.NewtonDynamics.Material();
                materialIDs.Bot = materialManager.AddMaterial(material);

                // Bot Ram
                material = new Game.Newt.v2.NewtonDynamics.Material();
                material.Elasticity = ItemOptionsArco.ELASTICITY_BOTRAM;
                materialIDs.BotRam = materialManager.AddMaterial(material);

                // Exploding Bot
                material = new Game.Newt.v2.NewtonDynamics.Material();
                material.IsCollidable = false;
                materialIDs.ExplodingBot = materialManager.AddMaterial(material);

                // Weapon
                material = new Game.Newt.v2.NewtonDynamics.Material();
                materialIDs.Weapon = materialManager.AddMaterial(material);

                // Treasure Box
                material = new Game.Newt.v2.NewtonDynamics.Material();
                materialIDs.TreasureBox = materialManager.AddMaterial(material);

                //TODO: Uncomment these
                // Collisions
                //_materialManager.RegisterCollisionEvent(_materialIDs.Bot, _materialIDs.Bot, Collision_BotBot);
                //_materialManager.RegisterCollisionEvent(_materialIDs.Bot, _materialIDs.Weapon, Collision_BotWeapon);
                //_materialManager.RegisterCollisionEvent(_materialIDs.Weapon, _materialIDs.Weapon, Collision_WeaponWeapon);
                ////_materialManager.RegisterCollisionEvent(_materialIDs.Bot, _materialIDs.Wall, Collision_BotWall);
                //_materialManager.RegisterCollisionEvent(_materialIDs.Weapon, _materialIDs.Wall, Collision_WeaponWall);
                //_materialManager.RegisterCollisionEvent(_materialIDs.Weapon, _materialIDs.TreasureBox, Collision_WeaponTreasureBox);
                //_materialManager.RegisterCollisionEvent(_materialIDs.Bot, _materialIDs.TreasureBox, Collision_BotTreasureBox);

                #endregion
                #region keep 2D

                //TODO: drag plane should either be a plane or a large cylinder, based on the current (level|scene|stage|area|arena|map|place|region|zone)

                // This game is 3D emulating 2D, so always have the mouse go to the XY plane
                DragHitShape dragPlane = new DragHitShape();
                dragPlane.SetShape_Plane(new Triangle(new Point3D(-1, -1, 0), new Point3D(1, -1, 0), new Point3D(0, 1, 0)));

                // This will keep objects onto that plane using forces (not velocities)
                KeepItems2D keep2D = new KeepItems2D
                {
                    SnapShape = dragPlane,
                };

                #endregion

                #region bot -> dna

                // Need to create a bot that will wire up neurons, then save off that dna.  Otherwise, each room's bot will have a random
                // wiring, and the training will be meaningless

                ShipCoreArgs core1 = new ShipCoreArgs()
                {
                    World = accessor.World,
                    Material_Ship = materialIDs.Bot,
                    Map = new Map(null, null, accessor.World),
                };

                ShipExtraArgs extra1 = new ShipExtraArgs()
                {
                    Options = editorOptions,
                    ItemOptions = itemOptions,
                    Gravity = gravity,
                };

                //BotConstructor_Events events = new BotConstructor_Events();
                //events.

                // Create the bot
                BotConstruction_Result construction1 = BotConstructor.ConstructBot(dna, core1, extra1);
                Bot bot1 = new Bot(construction1);

                dna = bot1.GetNewDNA();

                bot1.Dispose();
                bot1 = null;

                #endregion

                #region arena accessor (init)

                foreach (var initRoom in accessor.AllRooms)
                {
                    #region bot

                    ShipCoreArgs core = new ShipCoreArgs()
                    {
                        World = initRoom.room.World,
                        Material_Ship = materialIDs.Bot,
                        Map = initRoom.room.Map,
                    };

                    ShipExtraArgs extra = new ShipExtraArgs()
                    {
                        Options = editorOptions,
                        ItemOptions = itemOptions,
                        Gravity = gravity,
                    };

                    //BotConstructor_Events events = new BotConstructor_Events();
                    //events.

                    // Create the bot
                    BotConstruction_Result construction = BotConstructor.ConstructBot(dna, core, extra);
                    Bot bot = new Bot(construction);

                    // Find the brainneat part
                    BrainNEAT brainPart = bot.Parts.FirstOrDefault(o => o is BrainNEAT) as BrainNEAT;
                    if (brainPart == null)
                    {
                        throw new ApplicationException("Didn't find BrainNEAT part");
                    }

                    SensorHoming[] homingParts = bot.Parts.
                        Where(o => o is SensorHoming).
                        Select(o => (SensorHoming)o).
                        ToArray();

                    if (homingParts.Length == 0)
                    {
                        throw new ApplicationException("Didn't find SensorHoming part");
                    }

                    #endregion

                    initRoom.room.Bot = bot;
                    initRoom.room.BrainPart = brainPart;
                    initRoom.room.HomingParts = homingParts;

                    initRoom.room.Map.AddItem(bot);
                }

                #endregion

                #region checkout/checkin tests

                TrainingRoom room1 = accessor.CheckoutRoom();
                TrainingRoom room2 = accessor.CheckoutRoom();
                TrainingRoom room3 = accessor.CheckoutRoom();
                TrainingRoom room4 = accessor.CheckoutRoom();

                //accessor.UncheckoutRoom(room4);
                accessor.UncheckoutRoom(room2);
                //accessor.UncheckoutRoom(room2);

                TrainingRoom room5 = accessor.CheckoutRoom();

                accessor.UncheckoutRoom(room1);
                accessor.UncheckoutRoom(room3);

                TrainingRoom room6 = accessor.CheckoutRoom();
                TrainingRoom room7 = accessor.CheckoutRoom();
                TrainingRoom room8 = accessor.CheckoutRoom();
                TrainingRoom room9 = accessor.CheckoutRoom();

                //NOTE: even though room2 was already checked in, the double checkin works.  The only thing I can think of to fix that would be to
                //return an object that wraps the room.  That wrapper would be instantiated for the checkout and contain a token.  Then the accessor
                //would remember the checked out token
                //
                //that's a lot of work for something that should never happen
                accessor.UncheckoutRoom(room2);

                #endregion

                accessor.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UnitTestWorldAccessor_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                WorldAccessor worldAccessor = new WorldAccessor(new Point3D(-100, -100, -100), new Point3D(100, 100, 100));

                Evaluator2[] evaluators = new[]
                {
                    new Evaluator2(worldAccessor, null),
                    new Evaluator2(worldAccessor, null),
                    new Evaluator2(worldAccessor, null),
                };

                worldAccessor.Initialized += (s1, e1) =>
                {
                    World world = worldAccessor.World;

                    foreach (Evaluator2 evaluator in evaluators)
                    {
                        evaluator.UnitTest1_FinishInitialization(Guid.NewGuid());
                    }
                };

                UnitTestWorker workManager = new UnitTestWorker(evaluators);

                for (int cntr = 0; cntr < 10; cntr++)
                {
                    workManager.Step1();
                }

                worldAccessor.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void UnitTestArenaAccessor_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ArenaAccessor arena = new ArenaAccessor(10, 100, 10, false, false, null, null, null);

                Evaluator2[] evaluators = arena.AllRooms.
                    Select(o =>
                    {
                        Evaluator2 evaluator = new Evaluator2(arena.WorldAccessor, o.room);
                        o.room.Evaluator = evaluator;

                        return evaluator;
                    }).
                    ToArray();

                arena.WorldCreated += (s1, e1) =>
                {
                    // setup materials
                    // finish the bot dna if it needs it

                    foreach (var room in arena.AllRooms)
                    {
                        //room.room.Bot = ;
                        //room.room.BrainPart = ;
                        //room.room.HomingParts = ;
                    }
                };

                UnitTestWorker workManager = new UnitTestWorker(evaluators);

                for (int cntr = 0; cntr < 10; cntr++)
                {
                    workManager.Step2();
                }

                arena.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void UnitTestTrainingSession_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShipDNA dna = GetBotDNA();

                #region ship extra args

                ShipExtraArgs extra = new ShipExtraArgs()
                {
                    Options = new EditorOptions(),
                    ItemOptions = new ItemOptionsArco(),
                };
                extra.Gravity = new GravityFieldUniform() { Gravity = new Vector3D(0, -((ItemOptionsArco)extra.ItemOptions).Gravity, 0) };

                #endregion
                #region finish dna

                // Need to create a bot that will wire up neurons, then save off that dna.  Otherwise, each room's bot will have a random
                // wiring, and the training will be meaningless

                ShipCoreArgs core = new ShipCoreArgs()
                {
                    World = _world,
                    Material_Ship = _materialIDs.Bot,
                    Map = new Map(null, extra.CameraPool, _world),
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

                int inputCount = brainPart.Neruons_Writeonly.Count();
                int outputCount = brainPart.Neruons_Readonly.Count();

                bot.Dispose();
                bot = null;

                #endregion

                TrainingSession session = new TrainingSession
                (
                    dna, extra, inputCount, outputCount,
                    (w, r) => new Evaluator2(w, r)
                );


                //TODO: store this as a member variable, tell ea to start and make a stop button


                session.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TwoDisks_Click(object sender, RoutedEventArgs e)
        {
            const double THICKNESS = .005;
            const double DOT = THICKNESS * 3;
            const double HALFSEPARATION = 2.5;
            const double INITIALRADIUS = 1d;
            const double MUTATEDIST = .1d;
            const int COUNT = 20;
            const int NUMFINALLINKS = 22;

            try
            {
                Debug3DWindow window = new Debug3DWindow();

                Point3D[] parentPoints = Enumerable.Range(0, COUNT).
                    Select(o => Math3D.GetRandomVector_Circular(INITIALRADIUS).ToPoint()).
                    ToArray();

                Point3D[] childPoints = parentPoints.
                    Select(o => MutateUtility.Mutate_RandDistance(o.ToVector(), MUTATEDIST).ToPoint()).
                    ToArray();

                window.AddDots(parentPoints, DOT, Colors.DimGray);
                window.AddDots(childPoints, DOT, Colors.Silver);


                // FuzzyLink wasn't designed for from and to points to be sitting on top of each other

                Vector3D offset = new Vector3D(0, 0, HALFSEPARATION);

                Tuple<Point3D, Point3D, double>[] links = parentPoints.
                    Select(o => Tuple.Create(o - offset, o + offset, 1d)).
                    ToArray();

                Point3D[] pointsForFuzzy = parentPoints.Select(o => o - offset).
                    Concat(childPoints.Select(o => o + offset)).
                    ToArray();

                //window.AddDots(pointsForFuzzy, DOT, Colors.Plum);
                window.AddDots(parentPoints.Select(o => o - offset), DOT, Colors.DimGray);
                window.AddDots(childPoints.Select(o => o + offset), DOT, Colors.Silver);

                var newLinks1 = ItemLinker.FuzzyLink(links, pointsForFuzzy, NUMFINALLINKS, 6);
                var newLinks2 = ItemLinker.FuzzyLink(links, pointsForFuzzy, COUNT, 6);


                foreach (var link in newLinks1)
                {
                    window.AddLine(pointsForFuzzy[link.Item1], pointsForFuzzy[link.Item2], THICKNESS * link.Item3, Colors.Orchid);
                }

                foreach (var link in newLinks2)
                {
                    window.AddLine(pointsForFuzzy[link.Item1], pointsForFuzzy[link.Item2], THICKNESS * link.Item3, Colors.Coral);
                }


                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void TwoEvenDisks_small_Click(object sender, RoutedEventArgs e)
        {
            const double THICKNESS = .005;
            const double DOT = THICKNESS * 3;
            const double HALFSEPARATION = 2.5;
            const double INITIALRADIUS = 1d;
            const double MUTATEDIST = .1d;
            const int COUNT = 20;
            const int NUMFINALLINKS = 22;

            try
            {
                Debug3DWindow window = new Debug3DWindow();

                //Point3D[] parentPoints = Enumerable.Range(0, COUNT).
                //    Select(o => Math3D.GetRandomVector_Circular(INITIALRADIUS).ToPoint()).
                //    ToArray();
                Point3D[] parentPoints = Math3D.GetRandomVectors_Circular_EvenDist(COUNT, INITIALRADIUS).
                    Select(o => o.ToPoint3D()).
                    ToArray();

                Point3D[] childPoints = parentPoints.
                    Select(o => MutateUtility.Mutate_RandDistance(o.ToVector(), MUTATEDIST).ToPoint()).
                    ToArray();

                window.AddDots(parentPoints, DOT, Colors.DimGray);
                window.AddDots(childPoints, DOT, Colors.Silver);


                // FuzzyLink wasn't designed for from and to points to be sitting on top of each other

                Vector3D offset = new Vector3D(0, 0, HALFSEPARATION);

                Tuple<Point3D, Point3D, double>[] links = parentPoints.
                    Select(o => Tuple.Create(o - offset, o + offset, 1d)).
                    ToArray();

                Point3D[] pointsForFuzzy = parentPoints.Select(o => o - offset).
                    Concat(childPoints.Select(o => o + offset)).
                    ToArray();

                //window.AddDots(pointsForFuzzy, DOT, Colors.Plum);
                window.AddDots(parentPoints.Select(o => o - offset), DOT, Colors.DimGray);
                window.AddDots(childPoints.Select(o => o + offset), DOT, Colors.Silver);

                var newLinks1 = ItemLinker.FuzzyLink(links, pointsForFuzzy, NUMFINALLINKS, 6);
                var newLinks2 = ItemLinker.FuzzyLink(links, pointsForFuzzy, COUNT, 6);


                foreach (var link in newLinks1)
                {
                    window.AddLine(pointsForFuzzy[link.Item1], pointsForFuzzy[link.Item2], THICKNESS * link.Item3, Colors.Orchid);
                }

                foreach (var link in newLinks2)
                {
                    window.AddLine(pointsForFuzzy[link.Item1], pointsForFuzzy[link.Item2], THICKNESS * link.Item3, Colors.Coral);
                }


                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void TwoEvenDisks_large_Click(object sender, RoutedEventArgs e)
        {
            const double THICKNESS = .005;
            const double DOT = THICKNESS * 3;
            const double HALFSEPARATION = 2.5;
            const double INITIALRADIUS = 1d;
            const double MUTATEDIST = .33d;
            const int COUNT = 20;
            const int NUMFINALLINKS = 22;

            try
            {
                Debug3DWindow window = new Debug3DWindow();

                //Point3D[] parentPoints = Enumerable.Range(0, COUNT).
                //    Select(o => Math3D.GetRandomVector_Circular(INITIALRADIUS).ToPoint()).
                //    ToArray();
                Point3D[] parentPoints = Math3D.GetRandomVectors_Circular_EvenDist(COUNT, INITIALRADIUS).
                    Select(o => o.ToPoint3D()).
                    ToArray();

                Point3D[] childPoints = parentPoints.
                    Select(o => MutateUtility.Mutate_RandDistance(o.ToVector(), MUTATEDIST).ToPoint()).
                    ToArray();

                window.AddDots(parentPoints, DOT, Colors.DimGray);
                window.AddDots(childPoints, DOT, Colors.Silver);


                // FuzzyLink wasn't designed for from and to points to be sitting on top of each other

                Vector3D offset = new Vector3D(0, 0, HALFSEPARATION);

                Tuple<Point3D, Point3D, double>[] links = parentPoints.
                    Select(o => Tuple.Create(o - offset, o + offset, 1d)).
                    ToArray();

                Point3D[] pointsForFuzzy = parentPoints.Select(o => o - offset).
                    Concat(childPoints.Select(o => o + offset)).
                    ToArray();

                //window.AddDots(pointsForFuzzy, DOT, Colors.Plum);
                window.AddDots(parentPoints.Select(o => o - offset), DOT, Colors.DimGray);
                window.AddDots(childPoints.Select(o => o + offset), DOT, Colors.Silver);

                var newLinks1 = ItemLinker.FuzzyLink(links, pointsForFuzzy, NUMFINALLINKS, 6);
                var newLinks2 = ItemLinker.FuzzyLink(links, pointsForFuzzy, COUNT, 6);


                foreach (var link in newLinks1)
                {
                    window.AddLine(pointsForFuzzy[link.Item1], pointsForFuzzy[link.Item2], THICKNESS * link.Item3, Colors.Orchid);
                }

                foreach (var link in newLinks2)
                {
                    window.AddLine(pointsForFuzzy[link.Item1], pointsForFuzzy[link.Item2], THICKNESS * link.Item3, Colors.Coral);
                }


                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void DNAMutate_Click(object sender, RoutedEventArgs e)
        {
            const double THICKNESS = .005;
            const double DOT = THICKNESS * 3;

            try
            {
                Debug3DWindow window = new Debug3DWindow();

                //MutateUtility

                #region flying bean

                //// Create a new one (not derived from a winner)
                //newBeanDNA.ShipLineage = Guid.NewGuid().ToString();
                //if (StaticRandom.NextDouble() > .5d)		// 50/50 chance of mutating right away
                //{
                //    newBeanDNA = MutateUtility.Mutate(newBeanDNA, _options.MutateArgs);
                //    newBeanDNA.Generation--;
                //}




                //          public static MutateUtility.ShipMutateArgs BuildMutateArgs(FlyingBeanOptions options)
                //          {
                //              #region Neural

                //              MutateUtility.NeuronMutateArgs neuralArgs = null;

                //              if (options.MutateChangeNeural)
                //              {
                //                  MutateUtility.MuateArgs neuronMovement = new MutateUtility.MuateArgs(false, options.NeuronPercentToMutate, null, null,
                //                      new MutateUtility.MuateFactorArgs(MutateUtility.FactorType.Distance, options.NeuronMovementAmount));        // neurons are all point3D (positions need to drift around freely.  percent doesn't make much sense)

                //                  MutateUtility.MuateArgs linkMovement = new MutateUtility.MuateArgs(false, options.LinkPercentToMutate,
                //                      new Tuple<string, MutateUtility.MuateFactorArgs>[]
                //                      {
                //                  Tuple.Create("FromContainerPosition", new MutateUtility.MuateFactorArgs(MutateUtility.FactorType.Distance, options.LinkContainerMovementAmount)),
                //                  Tuple.Create("FromContainerOrientation", new MutateUtility.MuateFactorArgs(MutateUtility.FactorType.Percent, options.LinkContainerRotateAmount))
                //                      },
                //                      new Tuple<PropsByPercent.DataType, MutateUtility.MuateFactorArgs>[]
                //                      {
                //                  Tuple.Create(PropsByPercent.DataType.Double, new MutateUtility.MuateFactorArgs(MutateUtility.FactorType.Distance, options.LinkWeightAmount)),		// all the doubles are weights, which need to be able to cross over zero (percents can't go + to -)
                //Tuple.Create(PropsByPercent.DataType.Point3D, new MutateUtility.MuateFactorArgs(MutateUtility.FactorType.Distance, options.LinkMovementAmount)),        // using a larger value for the links
                //                      },
                //                      null);

                //                  neuralArgs = new MutateUtility.NeuronMutateArgs(neuronMovement, null, linkMovement, null);
                //              }

                //              #endregion
                //              #region Body

                //              MutateUtility.MuateArgs bodyArgs = null;

                //              if (options.MutateChangeBody)
                //              {
                //                  var mutate_Vector3D = Tuple.Create(PropsByPercent.DataType.Vector3D, new MutateUtility.MuateFactorArgs(MutateUtility.FactorType.Percent, options.BodySizeChangePercent));
                //                  var mutate_Point3D = Tuple.Create(PropsByPercent.DataType.Point3D, new MutateUtility.MuateFactorArgs(MutateUtility.FactorType.Distance, options.BodyMovementAmount));       // positions need to drift around freely.  percent doesn't make much sense
                //                  var mutate_Quaternion = Tuple.Create(PropsByPercent.DataType.Quaternion, new MutateUtility.MuateFactorArgs(MutateUtility.FactorType.Percent, options.BodyOrientationChangePercent));

                //                  //NOTE: The mutate class has special logic for Scale and ThrusterDirections
                //                  bodyArgs = new MutateUtility.MuateArgs(true, options.BodyNumToMutate,
                //                      null,
                //                      new Tuple<PropsByPercent.DataType, MutateUtility.MuateFactorArgs>[]
                //                          {
                //                      mutate_Vector3D,
                //                      mutate_Point3D,
                //                      mutate_Quaternion,
                //                          },
                //                      new MutateUtility.MuateFactorArgs(MutateUtility.FactorType.Percent, .01d));     // this is just other (currently there aren't any others - just being safe)
                //              }

                //              #endregion

                //              return new MutateUtility.ShipMutateArgs(null, bodyArgs, neuralArgs);
                //          }


                #endregion
                #region arc evolution dreamer

                //// Create a mutated copy of the winning design
                //dna = UtilityCore.Clone(winningBot);
                //dna.UniqueID = Guid.NewGuid();
                //dna.Generation++;

                //if (dna.Parts != null)
                //{
                //    MutateUtility.Mutate(dna.Parts, _mutateArgs);
                //}





                //          private static MutateUtility.NeuronMutateArgs GetMutateArgs(ItemOptionsArco options)
                //          {
                //              MutateUtility.NeuronMutateArgs retVal = null;

                //              MutateUtility.MuateArgs neuronMovement = new MutateUtility.MuateArgs(false, options.Neuron_PercentToMutate, null, null,
                //                  new MutateUtility.MuateFactorArgs(MutateUtility.FactorType.Distance, options.Neuron_MovementAmount));       // neurons are all point3D (positions need to drift around freely.  percent doesn't make much sense)

                //              MutateUtility.MuateArgs linkMovement = new MutateUtility.MuateArgs(false, options.Link_PercentToMutate,
                //                  new Tuple<string, MutateUtility.MuateFactorArgs>[]
                //                      {
                //                  Tuple.Create("FromContainerPosition", new MutateUtility.MuateFactorArgs(MutateUtility.FactorType.Distance, options.LinkContainer_MovementAmount)),
                //                  Tuple.Create("FromContainerOrientation", new MutateUtility.MuateFactorArgs(MutateUtility.FactorType.Percent, options.LinkContainer_RotateAmount))
                //                      },
                //                  new Tuple<PropsByPercent.DataType, MutateUtility.MuateFactorArgs>[]
                //                      {
                //                  Tuple.Create(PropsByPercent.DataType.Double, new MutateUtility.MuateFactorArgs(MutateUtility.FactorType.Distance, options.Link_WeightAmount)),		// all the doubles are weights, which need to be able to cross over zero (percents can't go + to -)
                //Tuple.Create(PropsByPercent.DataType.Point3D, new MutateUtility.MuateFactorArgs(MutateUtility.FactorType.Distance, options.Link_MovementAmount)),       // using a larger value for the links
                //                      },
                //                  null);

                //              retVal = new MutateUtility.NeuronMutateArgs(neuronMovement, null, linkMovement, null);

                //              return retVal;
                //          }





                #endregion

                EditorOptions options = new EditorOptions();
                ItemOptionsArco itemOptions = new ItemOptionsArco();
                MutateUtility.NeuronMutateArgs mutateArgs = GetMutateArgs(itemOptions);

                Container energy = new Container()
                {
                    QuantityMax = 1000,
                    QuantityCurrent = 1000,
                };

                #region dnaText

                string dnaText = @"<BrainNEATDNA AltNeurons='{x:Null}' ExternalLinks='{x:Null}' Hyper='{x:Null}' InternalLinks='{x:Null}' Genome='&lt;?xml version=&quot;1.0&quot; encoding=&quot;utf-16&quot;?&gt;&#xD;&#xA;&lt;Root&gt;&#xD;&#xA;  &lt;ActivationFunctions&gt;&#xD;&#xA;    &lt;Fn id=&quot;0&quot; name=&quot;SteepenedSigmoid&quot; prob=&quot;1&quot; /&gt;&#xD;&#xA;  &lt;/ActivationFunctions&gt;&#xD;&#xA;  &lt;Networks&gt;&#xD;&#xA;    &lt;Network id=&quot;72521&quot; birthGen=&quot;603&quot; fitness=&quot;9816.362284508692&quot;&gt;&#xD;&#xA;      &lt;Nodes&gt;&#xD;&#xA;        &lt;Node type=&quot;bias&quot; id=&quot;0&quot; /&gt;&#xD;&#xA;        &lt;Node type=&quot;in&quot; id=&quot;1&quot; /&gt;&#xD;&#xA;        &lt;Node type=&quot;in&quot; id=&quot;2&quot; /&gt;&#xD;&#xA;        &lt;Node type=&quot;in&quot; id=&quot;3&quot; /&gt;&#xD;&#xA;        &lt;Node type=&quot;in&quot; id=&quot;4&quot; /&gt;&#xD;&#xA;        &lt;Node type=&quot;in&quot; id=&quot;5&quot; /&gt;&#xD;&#xA;        &lt;Node type=&quot;in&quot; id=&quot;6&quot; /&gt;&#xD;&#xA;        &lt;Node type=&quot;in&quot; id=&quot;7&quot; /&gt;&#xD;&#xA;        &lt;Node type=&quot;in&quot; id=&quot;8&quot; /&gt;&#xD;&#xA;        &lt;Node type=&quot;in&quot; id=&quot;9&quot; /&gt;&#xD;&#xA;        &lt;Node type=&quot;in&quot; id=&quot;10&quot; /&gt;&#xD;&#xA;        &lt;Node type=&quot;in&quot; id=&quot;11&quot; /&gt;&#xD;&#xA;        &lt;Node type=&quot;in&quot; id=&quot;12&quot; /&gt;&#xD;&#xA;        &lt;Node type=&quot;in&quot; id=&quot;13&quot; /&gt;&#xD;&#xA;        &lt;Node type=&quot;in&quot; id=&quot;14&quot; /&gt;&#xD;&#xA;        &lt;Node type=&quot;in&quot; id=&quot;15&quot; /&gt;&#xD;&#xA;        &lt;Node type=&quot;in&quot; id=&quot;16&quot; /&gt;&#xD;&#xA;        &lt;Node type=&quot;out&quot; id=&quot;17&quot; /&gt;&#xD;&#xA;        &lt;Node type=&quot;out&quot; id=&quot;18&quot; /&gt;&#xD;&#xA;        &lt;Node type=&quot;out&quot; id=&quot;19&quot; /&gt;&#xD;&#xA;        &lt;Node type=&quot;out&quot; id=&quot;20&quot; /&gt;&#xD;&#xA;        &lt;Node type=&quot;out&quot; id=&quot;21&quot; /&gt;&#xD;&#xA;        &lt;Node type=&quot;out&quot; id=&quot;22&quot; /&gt;&#xD;&#xA;        &lt;Node type=&quot;out&quot; id=&quot;23&quot; /&gt;&#xD;&#xA;        &lt;Node type=&quot;out&quot; id=&quot;24&quot; /&gt;&#xD;&#xA;        &lt;Node type=&quot;out&quot; id=&quot;25&quot; /&gt;&#xD;&#xA;        &lt;Node type=&quot;out&quot; id=&quot;26&quot; /&gt;&#xD;&#xA;        &lt;Node type=&quot;out&quot; id=&quot;27&quot; /&gt;&#xD;&#xA;        &lt;Node type=&quot;out&quot; id=&quot;28&quot; /&gt;&#xD;&#xA;        &lt;Node type=&quot;out&quot; id=&quot;29&quot; /&gt;&#xD;&#xA;        &lt;Node type=&quot;out&quot; id=&quot;30&quot; /&gt;&#xD;&#xA;        &lt;Node type=&quot;out&quot; id=&quot;31&quot; /&gt;&#xD;&#xA;        &lt;Node type=&quot;out&quot; id=&quot;32&quot; /&gt;&#xD;&#xA;        &lt;Node type=&quot;hid&quot; id=&quot;394&quot; /&gt;&#xD;&#xA;        &lt;Node type=&quot;hid&quot; id=&quot;397&quot; /&gt;&#xD;&#xA;        &lt;Node type=&quot;hid&quot; id=&quot;726&quot; /&gt;&#xD;&#xA;      &lt;/Nodes&gt;&#xD;&#xA;      &lt;Connections&gt;&#xD;&#xA;        &lt;Con id=&quot;37&quot; src=&quot;0&quot; tgt=&quot;21&quot; wght=&quot;-4.9030871525100146&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;46&quot; src=&quot;0&quot; tgt=&quot;30&quot; wght=&quot;-1.1900600902577161&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;76&quot; src=&quot;2&quot; tgt=&quot;28&quot; wght=&quot;0.33907784892075871&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;82&quot; src=&quot;3&quot; tgt=&quot;18&quot; wght=&quot;1.2908896341268707&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;104&quot; src=&quot;4&quot; tgt=&quot;24&quot; wght=&quot;-4.0477270447266127&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;113&quot; src=&quot;5&quot; tgt=&quot;17&quot; wght=&quot;4.0852039121700026&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;114&quot; src=&quot;5&quot; tgt=&quot;18&quot; wght=&quot;1.0521783664340627&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;115&quot; src=&quot;5&quot; tgt=&quot;19&quot; wght=&quot;2.0079577874744947&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;141&quot; src=&quot;6&quot; tgt=&quot;29&quot; wght=&quot;4.6732053649611771&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;149&quot; src=&quot;7&quot; tgt=&quot;21&quot; wght=&quot;4.66791721759364&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;185&quot; src=&quot;9&quot; tgt=&quot;25&quot; wght=&quot;2.4770242773543418&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;197&quot; src=&quot;10&quot; tgt=&quot;21&quot; wght=&quot;0.39293525359781128&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;202&quot; src=&quot;10&quot; tgt=&quot;26&quot; wght=&quot;4.077070002083647&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;231&quot; src=&quot;12&quot; tgt=&quot;23&quot; wght=&quot;-0.53332647279141177&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;237&quot; src=&quot;12&quot; tgt=&quot;29&quot; wght=&quot;-4.2693965862822187&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;242&quot; src=&quot;13&quot; tgt=&quot;18&quot; wght=&quot;3.5589114863998117&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;245&quot; src=&quot;13&quot; tgt=&quot;21&quot; wght=&quot;-4.8424927316386066&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;249&quot; src=&quot;13&quot; tgt=&quot;25&quot; wght=&quot;0.42861991028722163&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;253&quot; src=&quot;13&quot; tgt=&quot;29&quot; wght=&quot;-1.0750430881532831&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;265&quot; src=&quot;14&quot; tgt=&quot;25&quot; wght=&quot;3.4222315455229344&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;281&quot; src=&quot;15&quot; tgt=&quot;25&quot; wght=&quot;-1.8773377213242781&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;288&quot; src=&quot;15&quot; tgt=&quot;32&quot; wght=&quot;-0.57685901203741441&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;296&quot; src=&quot;16&quot; tgt=&quot;24&quot; wght=&quot;-1.9480067523422997&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;300&quot; src=&quot;16&quot; tgt=&quot;28&quot; wght=&quot;-4.5020487685567581&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;325&quot; src=&quot;10&quot; tgt=&quot;28&quot; wght=&quot;-1.7897325824492216&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;347&quot; src=&quot;27&quot; tgt=&quot;20&quot; wght=&quot;-2.2192313608291916&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;349&quot; src=&quot;30&quot; tgt=&quot;24&quot; wght=&quot;2.4944046515055667&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;374&quot; src=&quot;22&quot; tgt=&quot;22&quot; wght=&quot;-4.6541668102145195&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;377&quot; src=&quot;5&quot; tgt=&quot;32&quot; wght=&quot;-0.048810859292735431&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;390&quot; src=&quot;27&quot; tgt=&quot;23&quot; wght=&quot;-0.58853387764780729&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;395&quot; src=&quot;10&quot; tgt=&quot;394&quot; wght=&quot;0.81597389237548434&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;411&quot; src=&quot;32&quot; tgt=&quot;32&quot; wght=&quot;-1.7912759350146146&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;455&quot; src=&quot;27&quot; tgt=&quot;27&quot; wght=&quot;-2.6052686810539072&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;474&quot; src=&quot;23&quot; tgt=&quot;17&quot; wght=&quot;-3.6380458921172263&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;507&quot; src=&quot;4&quot; tgt=&quot;25&quot; wght=&quot;-0.020424798134576344&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;516&quot; src=&quot;25&quot; tgt=&quot;18&quot; wght=&quot;-0.27936245633765527&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;524&quot; src=&quot;27&quot; tgt=&quot;24&quot; wght=&quot;-4.28506284515302&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;531&quot; src=&quot;28&quot; tgt=&quot;32&quot; wght=&quot;-3.0920540611258631&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;544&quot; src=&quot;1&quot; tgt=&quot;27&quot; wght=&quot;-3.904831787959691&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;585&quot; src=&quot;20&quot; tgt=&quot;18&quot; wght=&quot;-1.1849480754379151&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;609&quot; src=&quot;397&quot; tgt=&quot;28&quot; wght=&quot;-0.4452530505299081&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;627&quot; src=&quot;25&quot; tgt=&quot;25&quot; wght=&quot;-4.5748807171914061&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;628&quot; src=&quot;23&quot; tgt=&quot;19&quot; wght=&quot;-3.9566869649727132&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;686&quot; src=&quot;17&quot; tgt=&quot;26&quot; wght=&quot;-4.2408244270831013&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;717&quot; src=&quot;24&quot; tgt=&quot;29&quot; wght=&quot;-2.7164376772589285&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;727&quot; src=&quot;12&quot; tgt=&quot;726&quot; wght=&quot;-0.986667490802277&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;728&quot; src=&quot;726&quot; tgt=&quot;397&quot; wght=&quot;5&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;740&quot; src=&quot;18&quot; tgt=&quot;23&quot; wght=&quot;-2.6272220320806965&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;864&quot; src=&quot;18&quot; tgt=&quot;19&quot; wght=&quot;-1.6502315662220726&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;1055&quot; src=&quot;726&quot; tgt=&quot;23&quot; wght=&quot;0.021064103567223082&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;1090&quot; src=&quot;32&quot; tgt=&quot;17&quot; wght=&quot;-0.0842362482125808&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;1115&quot; src=&quot;0&quot; tgt=&quot;18&quot; wght=&quot;-0.21503649150993631&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;1118&quot; src=&quot;16&quot; tgt=&quot;25&quot; wght=&quot;0.067060311987954424&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;1119&quot; src=&quot;23&quot; tgt=&quot;31&quot; wght=&quot;0.10266128964854407&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;1230&quot; src=&quot;18&quot; tgt=&quot;31&quot; wght=&quot;-1.2408369477461065&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;1361&quot; src=&quot;726&quot; tgt=&quot;18&quot; wght=&quot;-0.1534090376257943&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;1475&quot; src=&quot;20&quot; tgt=&quot;24&quot; wght=&quot;0.024446482378084212&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;1697&quot; src=&quot;32&quot; tgt=&quot;397&quot; wght=&quot;4.4578678552363744&quot; /&gt;&#xD;&#xA;        &lt;Con id=&quot;1865&quot; src=&quot;22&quot; tgt=&quot;18&quot; wght=&quot;-1.2371902144514024&quot; /&gt;&#xD;&#xA;      &lt;/Connections&gt;&#xD;&#xA;    &lt;/Network&gt;&#xD;&#xA;  &lt;/Networks&gt;&#xD;&#xA;&lt;/Root&gt;' Orientation='Identity' PartType='BrainNEAT' PercentDamaged='0' Position='0,0,0' Scale='1,1,1' xmlns='clr-namespace:Game.Newt.v2.GameItems.ShipParts;assembly=Game.Newt.v2.GameItems' xmlns:av='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:ghn='clr-namespace:Game.HelperClassesAI.NEAT;assembly=Game.HelperClassesAI' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
  <BrainNEATDNA.Activation>
    <ghn:ExperimentInitArgs_Activation_CyclicFixedTimesteps FastFlag='True' TimestepsPerActivation='2' />
  </BrainNEATDNA.Activation>
  <BrainNEATDNA.NEATPositions_Input>
    <x:Array Type='av:Point3D'>
      <av:Point3D>-1,0,-1</av:Point3D>
      <av:Point3D>-0.866666666666667,0,-1</av:Point3D>
      <av:Point3D>-0.733333333333333,0,-1</av:Point3D>
      <av:Point3D>-0.6,0,-1</av:Point3D>
      <av:Point3D>-0.466666666666667,0,-1</av:Point3D>
      <av:Point3D>-0.333333333333333,0,-1</av:Point3D>
      <av:Point3D>-0.2,0,-1</av:Point3D>
      <av:Point3D>-0.0666666666666667,0,-1</av:Point3D>
      <av:Point3D>0.0666666666666667,0,-1</av:Point3D>
      <av:Point3D>0.2,0,-1</av:Point3D>
      <av:Point3D>0.333333333333333,0,-1</av:Point3D>
      <av:Point3D>0.466666666666667,0,-1</av:Point3D>
      <av:Point3D>0.6,0,-1</av:Point3D>
      <av:Point3D>0.733333333333333,0,-1</av:Point3D>
      <av:Point3D>0.866666666666667,0,-1</av:Point3D>
      <av:Point3D>1,0,-1</av:Point3D>
    </x:Array>
  </BrainNEATDNA.NEATPositions_Input>
  <BrainNEATDNA.NEATPositions_Output>
    <x:Array Type='av:Point3D'>
      <av:Point3D>-1,0,1</av:Point3D>
      <av:Point3D>-0.866666666666667,0,1</av:Point3D>
      <av:Point3D>-0.733333333333333,0,1</av:Point3D>
      <av:Point3D>-0.6,0,1</av:Point3D>
      <av:Point3D>-0.466666666666667,0,1</av:Point3D>
      <av:Point3D>-0.333333333333333,0,1</av:Point3D>
      <av:Point3D>-0.2,0,1</av:Point3D>
      <av:Point3D>-0.0666666666666667,0,1</av:Point3D>
      <av:Point3D>0.0666666666666667,0,1</av:Point3D>
      <av:Point3D>0.2,0,1</av:Point3D>
      <av:Point3D>0.333333333333333,0,1</av:Point3D>
      <av:Point3D>0.466666666666667,0,1</av:Point3D>
      <av:Point3D>0.6,0,1</av:Point3D>
      <av:Point3D>0.733333333333333,0,1</av:Point3D>
      <av:Point3D>0.866666666666667,0,1</av:Point3D>
      <av:Point3D>1,0,1</av:Point3D>
    </x:Array>
  </BrainNEATDNA.NEATPositions_Output>
  <BrainNEATDNA.Neurons>
    <x:Array Type='av:Point3D'>
      <av:Point3D>-1,0,-1</av:Point3D>
      <av:Point3D>-0.866666666666667,0,-1</av:Point3D>
      <av:Point3D>-0.733333333333333,0,-1</av:Point3D>
      <av:Point3D>-0.6,0,-1</av:Point3D>
      <av:Point3D>-0.466666666666667,0,-1</av:Point3D>
      <av:Point3D>-0.333333333333333,0,-1</av:Point3D>
      <av:Point3D>-0.2,0,-1</av:Point3D>
      <av:Point3D>-0.0666666666666667,0,-1</av:Point3D>
      <av:Point3D>0.0666666666666667,0,-1</av:Point3D>
      <av:Point3D>0.2,0,-1</av:Point3D>
      <av:Point3D>0.333333333333333,0,-1</av:Point3D>
      <av:Point3D>0.466666666666667,0,-1</av:Point3D>
      <av:Point3D>0.6,0,-1</av:Point3D>
      <av:Point3D>0.733333333333333,0,-1</av:Point3D>
      <av:Point3D>0.866666666666667,0,-1</av:Point3D>
      <av:Point3D>1,0,-1</av:Point3D>
      <av:Point3D>-1,0,1</av:Point3D>
      <av:Point3D>-0.866666666666667,0,1</av:Point3D>
      <av:Point3D>-0.733333333333333,0,1</av:Point3D>
      <av:Point3D>-0.6,0,1</av:Point3D>
      <av:Point3D>-0.466666666666667,0,1</av:Point3D>
      <av:Point3D>-0.333333333333333,0,1</av:Point3D>
      <av:Point3D>-0.2,0,1</av:Point3D>
      <av:Point3D>-0.0666666666666667,0,1</av:Point3D>
      <av:Point3D>0.0666666666666667,0,1</av:Point3D>
      <av:Point3D>0.2,0,1</av:Point3D>
      <av:Point3D>0.333333333333333,0,1</av:Point3D>
      <av:Point3D>0.466666666666667,0,1</av:Point3D>
      <av:Point3D>0.6,0,1</av:Point3D>
      <av:Point3D>0.733333333333333,0,1</av:Point3D>
      <av:Point3D>0.866666666666667,0,1</av:Point3D>
      <av:Point3D>1,0,1</av:Point3D>
    </x:Array>
  </BrainNEATDNA.Neurons>
</BrainNEATDNA>".
Replace('\'', '"');

                #endregion

                // Parent
                BrainNEATDNA dnaParent = UtilityCore.DeserializeFromString<BrainNEATDNA>(dnaText);
                BrainNEAT brainParent = new BrainNEAT(options, itemOptions, dnaParent, energy);

                // Mutate
                BrainNEATDNA dnaChild = UtilityCore.Clone(dnaParent);
                MutateUtility.Mutate(new ShipPartDNA[] { dnaChild }, mutateArgs);

                window.AddDots(dnaParent.Neurons, DOT, Colors.DimGray);
                window.AddDots(dnaChild.Neurons, DOT, Colors.Silver);

                // Child
                BrainNEAT brainChild = new BrainNEAT(options, itemOptions, dnaChild, energy);

                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ConePoints2_Click(object sender, RoutedEventArgs e)
        {
            const double SIZE = 2;
            const double THICKNESS = .005;
            const double DOT = THICKNESS * 3;

            try
            {
                Debug3DWindow window = new Debug3DWindow();

                window.AddAxisLines(SIZE * 1.5, THICKNESS);

                double angle = 83;

                window.AddDots(
                    Math3D.GetRandomVectors_Cone_EvenDist(120, new Vector3D(0, 0, 1), angle, SIZE, SIZE).Select(o => o.ToPoint()),
                    DOT,
                    Colors.DodgerBlue);

                window.AddDots(
                    Math3D.GetRandomVectors_Cone_EvenDist(25, new Vector3D(0, 0, -1), angle, SIZE, SIZE).Select(o => o.ToPoint()),
                    DOT,
                    Colors.IndianRed);

                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ConePoints4_Click(object sender, RoutedEventArgs e)
        {
            const double SIZE = 2;
            const double THICKNESS = .005;
            const double DOT = THICKNESS * 3;

            try
            {
                Debug3DWindow window = new Debug3DWindow();

                window.AddAxisLines(SIZE * 1.5, THICKNESS);


                Vector3D axis = new Vector3D(0, 0, SIZE);

                Vector3D[] orths1 = Enumerable.Range(0, 2000).
                    Select(o => Math3D.GetRandomVector_Spherical_Shell(1)).
                    ToArray();

                Vector3D[] orths2 = orths1.
                    Select(o => Vector3D.CrossProduct(axis, o)).
                    ToArray();

                Vector3D[] orths3 = orths1.
                    Select(o => Vector3D.CrossProduct(axis, o).ToUnit()).
                    ToArray();

                Vector3D[] final = Enumerable.Range(0, orths1.Length).
                    Select(o => axis.GetRotatedVector(orths3[o], StaticRandom.NextDouble(0, 90))).
                    ToArray();


                //window.AddDots(orths1.Select(o => o.ToPoint()), DOT, Colors.IndianRed);
                window.AddDots(orths2.Select(o => o.ToPoint()), DOT, Colors.DodgerBlue);
                window.AddDots(orths3.Select(o => o.ToPoint()), DOT, Colors.LimeGreen);
                window.AddDots(final.Select(o => o.ToPoint()), DOT, Colors.DeepPink);



                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void CirclePoints_Click(object sender, RoutedEventArgs e)
        {
            const int NUMSAMPLES = 1000;
            const double SIZE = 2;
            const double THICKNESS = .005;
            const double DOT = THICKNESS * 3;

            try
            {

                // c=pi*d
                // a=pi*r^2

                // get points along a line
                //linear = 
                //squared = 
                //squared/pi = 

                // draw circles at each point


                Debug3DWindow window = new Debug3DWindow();

                window.AddAxisLines(SIZE * 1.5, THICKNESS);


                // pick a random point along radius
                double[] linear = Enumerable.Range(0, NUMSAMPLES).
                    Select(o => StaticRandom.NextDouble()).
                    ToArray();

                double[] squared = Enumerable.Range(0, NUMSAMPLES).
                    Select(o => StaticRandom.NextPow(2)).
                    ToArray();

                double[] sqrroot = Enumerable.Range(0, NUMSAMPLES).
                    Select(o => StaticRandom.NextPow(.5)).
                    ToArray();

                Vector3D orth = new Vector3D(0, 0, 1);

                // pick a random angle, draw dot
                Point3D[] linearPoints = linear.
                    Select(o => new Vector3D(o * SIZE, 0, 0).GetRotatedVector(orth, StaticRandom.NextDouble(360)).ToPoint()).
                    ToArray();

                Point3D[] squaredPoints = squared.
                    Select(o => new Vector3D(o * SIZE, 0, 0).GetRotatedVector(orth, StaticRandom.NextDouble(360)).ToPoint()).
                    ToArray();

                Point3D[] sqrrootPoints = sqrroot.
                    Select(o => new Vector3D(o * SIZE, 0, 0).GetRotatedVector(orth, StaticRandom.NextDouble(360)).ToPoint()).
                    ToArray();

                window.AddDots(linearPoints.Select(o => o - new Vector3D(SIZE, 0, 0)), DOT, Colors.DodgerBlue);
                window.AddDots(squaredPoints.Select(o => o + new Vector3D(SIZE, 0, 0)), DOT, Colors.IndianRed);
                window.AddDots(sqrrootPoints.Select(o => o + new Vector3D(0, SIZE * Math.Sqrt(3), 0)), DOT, Colors.ForestGreen);

                window.AddDots(linear.Take(50).Select(o => new Point3D(o * SIZE, -SIZE * 1.5, 1)), DOT / 2, Colors.DarkBlue);
                window.AddDots(squared.Take(50).Select(o => new Point3D(o * SIZE, (-SIZE * 1.5) + .1, 1)), DOT / 2, Colors.Maroon);
                window.AddDots(sqrroot.Take(50).Select(o => new Point3D(o * SIZE, (-SIZE * 1.5) + .2, 1)), DOT / 2, Colors.DarkGreen);

                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void SqrtCos_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Random rand = StaticRandom.GetRandomForThread();

                Vector3D orig = new Vector3D(1, 0, 0);
                Vector3D orth = new Vector3D(0, 0, 1);

                GraphResult linear = Debug3DWindow.GetCountGraph(() => rand.NextDouble(), "linear");
                GraphResult squared = Debug3DWindow.GetCountGraph(() => rand.NextPow(2), "squared");
                GraphResult sqrroot = Debug3DWindow.GetCountGraph(() => StaticRandom.NextPow(.5), "sqrroot");

                //TODO: call getcountgraph with 
                GraphResult cos = Debug3DWindow.GetCountGraph(() => orig.GetRotatedVector(orth, rand.NextDouble(90)).X, "cos");


                Debug3DWindow window = new Debug3DWindow();

                var graphLocations = Math2D.GetCells_WithinSquare(5, 2);

                double mult = .9;

                window.AddGraph(linear, graphLocations[0].rect.ChangeSize(mult).ToRect3D());
                window.AddGraph(squared, graphLocations[1].rect.ChangeSize(mult).ToRect3D());
                window.AddGraph(sqrroot, graphLocations[2].rect.ChangeSize(mult).ToRect3D());
                window.AddGraph(cos, graphLocations[3].rect.ChangeSize(mult).ToRect3D());

                window.Show();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void BestPhi_Click(object sender, RoutedEventArgs e)
        {
            const double SIZE = 2;
            const double THICKNESS = .005;
            const double DOT = THICKNESS * 3;

            try
            {
                Random rand = StaticRandom.GetRandomForThread();

                double halfPI = Math.PI / 2;

                Func<double> getPhi1 = () =>
                    rand.NextBool() ?
                        halfPI * rand.NextPow(.5) :
                        halfPI + (halfPI * (1 - rand.NextPow(.5)));

                Func<double> getPhi2 = () =>
                {
                    // z is cos of phi, which isn't linear.  So the probability is higher that more will be at the poles.  Which means if I want
                    // a linear probability of z, I need to feed the cosine something that will flatten it into a line.  The curve that will do that
                    // is arccos (which basically rotates the cosine wave 90 degrees).  This means that it is undefined for any x outside the range
                    // of -1 to 1.  So I have to shift the random statement to go between -1 to 1, run it through the curve, then shift the result
                    // to go between 0 and pi.
                    //double phi = rand.NextDouble() * Math.PI;

                    double phi = (rand.NextDouble() * 2d) - 1d;     // value from -1 to 1
                    phi = -Math.Asin(phi) / (Math.PI * .5d);        // another value from -1 to 1
                    phi = (1d + phi) * Math.PI * .5d;		// from 0 to pi
                    return phi;
                };

                Func<double> getPhi3 = () =>
                {
                    // z is cos of phi, which isn't linear.  So the probability is higher that more will be at the poles.  Which means if I want
                    // a linear probability of z, I need to feed the cosine something that will flatten it into a line.  The curve that will do that
                    // is arccos (which basically rotates the cosine wave 90 degrees).  This means that it is undefined for any x outside the range
                    // of -1 to 1.  So I have to shift the random statement to go between -1 to 1, run it through the curve, then shift the result
                    // to go between 0 and pi.
                    //double phi = rand.NextDouble() * Math.PI;

                    double phi = -Math.Asin(rand.NextDouble(-1, 1)) / halfPI;        // another value from -1 to 1
                    phi = (1d + phi) * halfPI;		// from 0 to pi
                    return phi;
                };

                Debug3DWindow window = new Debug3DWindow();

                var graphs = new[]
                {
                    Debug3DWindow.GetCountGraph(getPhi1, "1"),
                    Debug3DWindow.GetCountGraph(getPhi2, "2"),
                    Debug3DWindow.GetCountGraph(getPhi3, "3"),
                };

                window.AddGraphs(graphs, new Point3D(0, 0, 0), SIZE);

                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void PolarCoords_Click(object sender, RoutedEventArgs e)
        {
            const int NUMSAMPLES = 2000;
            const double RADIUS = 2;
            const double THICKNESS = .005;
            const double DOT = THICKNESS * 3;

            try
            {
                // x=r*cos(theta)*sin(phi)
                // y=r*sin(theta)*sin(phi)
                // z=r*cos(phi)

                Random rand = StaticRandom.GetRandomForThread();

                Debug3DWindow window = new Debug3DWindow();

                window.AddAxisLines(RADIUS * 1.5, THICKNESS);

                (double theta, double phi)[] polarPoints = Enumerable.Range(0, NUMSAMPLES).
                    Select(o => (rand.NextDouble(2 * Math.PI), rand.NextDouble(Math.PI))).
                    ToArray();

                Point3D[] cartesianPoints = polarPoints.
                    Select(o => new Point3D
                        (
                            RADIUS * Math.Cos(o.theta) * Math.Sin(o.phi),
                            RADIUS * Math.Sin(o.theta) * Math.Sin(o.phi),
                            RADIUS * Math.Cos(o.phi)
                        )).
                    ToArray();

                window.AddDots(cartesianPoints, DOT, Colors.DodgerBlue);

                window.Show();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void PolarConeShell2_Click(object sender, RoutedEventArgs e)
        {
            const int NUMSAMPLES = 2000;
            const double RADIUS = 2;
            const double THICKNESS = .005;
            const double DOT = THICKNESS * 3;

            try
            {
                Debug3DWindow window = new Debug3DWindow();

                Random rand = StaticRandom.GetRandomForThread();

                window.AddAxisLines(RADIUS * 1.5, THICKNESS);

                double maxAngle = rand.NextBool(.33) ?
                    180d :
                    rand.NextDouble(180);
                double minAngle = rand.NextBool(.33) ?
                    0d :
                    maxAngle * rand.NextDouble();

                window.AddText(string.Format("min angle: {0}", minAngle.ToStringSignificantDigits(2)));
                window.AddText(string.Format("max angle: {0}", maxAngle.ToStringSignificantDigits(2)));

                Vector3D axis = Math3D.GetRandomVector_Spherical_Shell(1);

                Vector3D[] points = Math3D.GetRandomVectors_Cone(NUMSAMPLES, axis, minAngle, maxAngle, RADIUS, RADIUS);

                window.AddLine(new Point3D(0, 0, 0), (axis.ToUnit() * RADIUS * 1.5).ToPoint(), THICKNESS, Colors.DarkBlue);
                window.AddDots(points.Select(o => o.ToPoint()), DOT, Colors.DodgerBlue);

                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void PolarConeInterior2_Click(object sender, RoutedEventArgs e)
        {
            const int NUMSAMPLES = 2000;
            const double RADIUS = 2;
            const double THICKNESS = .005;
            const double DOT = THICKNESS * 3;

            try
            {
                Debug3DWindow window = new Debug3DWindow();

                Random rand = StaticRandom.GetRandomForThread();

                window.AddAxisLines(RADIUS * 1.5, THICKNESS);

                double maxAngle = rand.NextBool(.33) ?
                    180d :
                    rand.NextDouble(180);
                double minAngle = rand.NextBool(.33) ?
                    0d :
                    maxAngle * rand.NextDouble();

                double minRadius = rand.NextBool() ?
                    0d :
                    RADIUS * rand.NextDouble();

                window.AddText(string.Format("min angle: {0}", minAngle.ToStringSignificantDigits(2)));
                window.AddText(string.Format("max angle: {0}", maxAngle.ToStringSignificantDigits(2)));
                window.AddText(string.Format("min radius: {0}", minRadius.ToStringSignificantDigits(2)));
                window.AddText(string.Format("max radius: {0}", RADIUS.ToStringSignificantDigits(2)));

                Vector3D axis = Math3D.GetRandomVector_Spherical_Shell(1);

                Vector3D[] points = Math3D.GetRandomVectors_Cone(NUMSAMPLES, axis, minAngle, maxAngle, minRadius, RADIUS);

                window.AddLine(new Point3D(0, 0, 0), (axis.ToUnit() * RADIUS * 1.5).ToPoint(), THICKNESS, Colors.DarkBlue);
                window.AddDots(points.Select(o => o.ToPoint()), DOT, Colors.DodgerBlue);

                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Decorate_Click(object sender, RoutedEventArgs e)
        {
            const double SIZE = 5;
            const double THICKNESS = .005;

            try
            {
                _viewport.Children.AddRange(Debug3DWindow.GetAxisLines(SIZE, THICKNESS));
                _viewport.Children.Add(Debug3DWindow.GetCircle(new Point3D(0, 0, 0), SIZE, THICKNESS, UtilityWPF.ColorFromHex("aaa")));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void AddWeapon_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _keep2D.Clear();
                _map.Clear();

                #region dna

                string dnaText = null;
                switch (cboWeapon.SelectedValue.ToString())
                {
                    case WEAPON_STICK:
                        #region stick

                        dnaText = @"<WeaponDNA HeadLeft='{x:Null}' HeadRight='{x:Null}' Name='{x:Null}' UniqueID='adc3d156-f585-4857-baa0-630ac44eb2e1' xmlns='clr-namespace:Game.Newt.v2.Arcanorum;assembly=Game.Newt.v2.Arcanorum' xmlns:scg='clr-namespace:System.Collections.Generic;assembly=System' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
  <WeaponDNA.Handle>
    <WeaponHandleDNA MaterialsForCustomizable='{x:Null}' AttachPointPercent='0' HandleMaterial='Hard_Wood' HandleType='Rod' Length='2.5900995704764962' Radius='0.064746866968808159'>
      <WeaponHandleDNA.KeyValues>
        <scg:SortedList x:TypeArguments='x:String, x:Double' Capacity='16'>
          <x:Double x:Key='maxX1'>0.67683496679031985</x:Double>
          <x:Double x:Key='maxX2'>0.50307137060587825</x:Double>
          <x:Double x:Key='maxY1'>1.0365644308675848</x:Double>
          <x:Double x:Key='maxY2'>0.94373999326198366</x:Double>
          <x:Double x:Key='ring1'>0.37616062582439164</x:Double>
          <x:Double x:Key='ring2'>1.3387358588760903</x:Double>
          <x:Double x:Key='ring3'>4.187827089946734</x:Double>
          <x:Double x:Key='ring4'>5.8767038983650055</x:Double>
          <x:Double x:Key='ring5'>1.4104809962863725</x:Double>
          <x:Double x:Key='ring6'>0.32968040411449973</x:Double>
        </scg:SortedList>
      </WeaponHandleDNA.KeyValues>
    </WeaponHandleDNA>
  </WeaponDNA.Handle>
</WeaponDNA>".
Replace('\'', '"');

                        #endregion
                        break;

                    case WEAPON_BALL:
                        #region ball

                        dnaText = @"<WeaponDNA HeadLeft='{x:Null}' Name='{x:Null}' UniqueID='36fe515d-1ae2-4ba1-99e6-4b0ba3234e51' xmlns='clr-namespace:Game.Newt.v2.Arcanorum;assembly=Game.Newt.v2.Arcanorum' xmlns:gh='clr-namespace:Game.HelperClassesWPF;assembly=Game.HelperClassesWPF' xmlns:scg='clr-namespace:System.Collections.Generic;assembly=System' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
  <WeaponDNA.Handle>
    <WeaponHandleDNA AttachPointPercent='0' HandleMaterial='Klinth' HandleType='Rod' Length='2.0918118471707272' Radius='0.063954932183937607'>
      <WeaponHandleDNA.KeyValues>
        <scg:SortedList x:TypeArguments='x:String, x:Double' Capacity='0' />
      </WeaponHandleDNA.KeyValues>
      <WeaponHandleDNA.MaterialsForCustomizable>
        <x:Array Type='gh:MaterialDefinition'>
          <gh:MaterialDefinition SpecularPower='{x:Null}' DiffuseColor='#FFA338A5' EmissiveColor='#FFED0B8F' SpecularColor='#FFB24192' />
          <gh:MaterialDefinition SpecularPower='{x:Null}' DiffuseColor='#FF6B136D' EmissiveColor='#FFED0B8F' SpecularColor='#FFB2368F' />
        </x:Array>
      </WeaponHandleDNA.MaterialsForCustomizable>
    </WeaponHandleDNA>
  </WeaponDNA.Handle>
  <WeaponDNA.HeadRight>
    <WeaponSpikeBallDNA Material='Klinth' Radius='0.13011356048988015'>
      <WeaponSpikeBallDNA.KeyValues>
        <scg:SortedList x:TypeArguments='x:String, x:Double' Capacity='4'>
          <x:Double x:Key='spikeLength'>0.13954035650297764</x:Double>
          <x:Double x:Key='spikeRadius'>0.023722482109851426</x:Double>
        </scg:SortedList>
      </WeaponSpikeBallDNA.KeyValues>
      <WeaponSpikeBallDNA.MaterialsForCustomizable>
        <x:Array Type='gh:MaterialDefinition'>
          <gh:MaterialDefinition SpecularPower='{x:Null}' DiffuseColor='#FFA436A6' EmissiveColor='#FF4B17DA' SpecularColor='#FFB8BB5C' />
          <gh:MaterialDefinition SpecularPower='{x:Null}' DiffuseColor='#FF6C116E' EmissiveColor='#FF4B17DA' SpecularColor='#FFB8BB52' />
        </x:Array>
      </WeaponSpikeBallDNA.MaterialsForCustomizable>
    </WeaponSpikeBallDNA>
  </WeaponDNA.HeadRight>
</WeaponDNA>".
Replace('\'', '"');

                        #endregion
                        break;

                    case WEAPON_BALL2:
                        #region ball2

                        dnaText = @"<WeaponDNA HeadLeft='{x:Null}' Name='{x:Null}' UniqueID='bb9a5d0f-41e3-4176-a5d0-ce3f5b119ada' xmlns='clr-namespace:Game.Newt.v2.Arcanorum;assembly=Game.Newt.v2.Arcanorum' xmlns:gh='clr-namespace:Game.HelperClassesWPF;assembly=Game.HelperClassesWPF' xmlns:scg='clr-namespace:System.Collections.Generic;assembly=System' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
  <WeaponDNA.Handle>
    <WeaponHandleDNA AttachPointPercent='0.42572175074635155' HandleMaterial='Klinth' HandleType='Rod' Length='5.0468014936180792' Radius='0.11243044646104353'>
      <WeaponHandleDNA.KeyValues>
        <scg:SortedList x:TypeArguments='x:String, x:Double' Capacity='0' />
      </WeaponHandleDNA.KeyValues>
      <WeaponHandleDNA.MaterialsForCustomizable>
        <x:Array Type='gh:MaterialDefinition'>
          <gh:MaterialDefinition SpecularPower='{x:Null}' DiffuseColor='#FF2EBA9D' EmissiveColor='#FF2D4CCE' SpecularColor='#FF3ABE2E' />
          <gh:MaterialDefinition SpecularPower='{x:Null}' DiffuseColor='#FF077B63' EmissiveColor='#FF2D4CCE' SpecularColor='#FF2DBE20' />
        </x:Array>
      </WeaponHandleDNA.MaterialsForCustomizable>
    </WeaponHandleDNA>
  </WeaponDNA.Handle>
  <WeaponDNA.HeadRight>
    <WeaponSpikeBallDNA MaterialsForCustomizable='{x:Null}' Material='Iron_Steel' Radius='0.25188905996458055'>
      <WeaponSpikeBallDNA.KeyValues>
        <scg:SortedList x:TypeArguments='x:String, x:Double' Capacity='4'>
          <x:Double x:Key='spikeLen'>0.34297056001945758</x:Double>
          <x:Double x:Key='spikeRadMult'>0.83028933799839078</x:Double>
        </scg:SortedList>
      </WeaponSpikeBallDNA.KeyValues>
    </WeaponSpikeBallDNA>
  </WeaponDNA.HeadRight>
</WeaponDNA>".
Replace('\'', '"');

                        #endregion
                        break;

                    case WEAPON_AXE:
                        #region axe

                        dnaText = @"<WeaponDNA HeadLeft='{x:Null}' Name='{x:Null}' UniqueID='7248c222-8bbb-4edc-b4fa-a5d3599388d6' xmlns='clr-namespace:Game.Newt.v2.Arcanorum;assembly=Game.Newt.v2.Arcanorum' xmlns:scg='clr-namespace:System.Collections.Generic;assembly=System' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
  <WeaponDNA.Handle>
    <WeaponHandleDNA MaterialsForCustomizable='{x:Null}' AttachPointPercent='0' HandleMaterial='Bronze' HandleType='Rod' Length='1.56084459729532' Radius='0.049673187406581443'>
      <WeaponHandleDNA.KeyValues>
        <scg:SortedList x:TypeArguments='x:String, x:Double' Capacity='4'>
          <x:Double x:Key='capWidth'>0.055011542473714904</x:Double>
        </scg:SortedList>
      </WeaponHandleDNA.KeyValues>
    </WeaponHandleDNA>
  </WeaponDNA.Handle>
  <WeaponDNA.HeadRight>
    <WeaponAxeDNA MaterialsForCustomizable='{x:Null}' AxeType='Bearded' Sides='Single_BackSpike' SizeSingle='0.81253735637876079' Style='Basic' IsBackward='True'>
      <WeaponAxeDNA.KeyValues>
        <scg:SortedList x:TypeArguments='x:String, x:Double' Capacity='32'>
          <x:Double x:Key='B1AngleL'>10.858513780803659</x:Double>
          <x:Double x:Key='B1AngleR'>8.8823372403777583</x:Double>
          <x:Double x:Key='B1PercentL'>0.37720483908415997</x:Double>
          <x:Double x:Key='B1PercentR'>0.57833736015173487</x:Double>
          <x:Double x:Key='B2AngleL'>64.1799264141265</x:Double>
          <x:Double x:Key='B2AngleR'>49.671304677460014</x:Double>
          <x:Double x:Key='B2PercentL'>0.3311114884170458</x:Double>
          <x:Double x:Key='B2PercentR'>0.63837515250238364</x:Double>
          <x:Double x:Key='EdgeAngleB'>15.168299203584111</x:Double>
          <x:Double x:Key='EdgeAngleT'>18.637867566378727</x:Double>
          <x:Double x:Key='EdgePercentB'>0.30145344443221267</x:Double>
          <x:Double x:Key='EdgePercentT'>0.26102069091949837</x:Double>
          <x:Double x:Key='EndBL_1X'>-0.028659775505453549</x:Double>
          <x:Double x:Key='EndBL_1Y'>0.040587623943103925</x:Double>
          <x:Double x:Key='EndBL_1Z'>0</x:Double>
          <x:Double x:Key='EndBRX'>0.012987497108087073</x:Double>
          <x:Double x:Key='EndBRY'>-0.19880393224665971</x:Double>
          <x:Double x:Key='EndBRZ'>0</x:Double>
          <x:Double x:Key='EndTRX'>0.00033173801640860119</x:Double>
          <x:Double x:Key='EndTRY'>0.18187627262379669</x:Double>
          <x:Double x:Key='EndTRZ'>0</x:Double>
          <x:Double x:Key='shouldExtendBeard'>1</x:Double>
          <x:Double x:Key='spikeLength'>1.258913926574827</x:Double>
        </scg:SortedList>
      </WeaponAxeDNA.KeyValues>
    </WeaponAxeDNA>
  </WeaponDNA.HeadRight>
</WeaponDNA>".
Replace('\'', '"');

                        #endregion
                        break;

                    case WEAPON_DOUBLEAXE_BALL:
                        #region axe+ball

                        dnaText = @"<WeaponDNA Name='{x:Null}' UniqueID='af9866dd-a104-4e7d-b65e-30813a3fa779' xmlns='clr-namespace:Game.Newt.v2.Arcanorum;assembly=Game.Newt.v2.Arcanorum' xmlns:gh='clr-namespace:Game.HelperClassesWPF;assembly=Game.HelperClassesWPF' xmlns:scg='clr-namespace:System.Collections.Generic;assembly=System' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
  <WeaponDNA.Handle>
    <WeaponHandleDNA AttachPointPercent='0.59603854510748688' HandleMaterial='Klinth' HandleType='Rod' Length='3.2639648677147761' Radius='0.11914441863035058'>
      <WeaponHandleDNA.KeyValues>
        <scg:SortedList x:TypeArguments='x:String, x:Double' Capacity='0' />
      </WeaponHandleDNA.KeyValues>
      <WeaponHandleDNA.MaterialsForCustomizable>
        <x:Array Type='gh:MaterialDefinition'>
          <gh:MaterialDefinition SpecularPower='{x:Null}' DiffuseColor='#FF88851E' EmissiveColor='#FF4399B9' SpecularColor='#FFADCF5F' />
          <gh:MaterialDefinition SpecularPower='{x:Null}' DiffuseColor='#FF5A5702' EmissiveColor='#FF4399B9' SpecularColor='#FFAACF54' />
        </x:Array>
      </WeaponHandleDNA.MaterialsForCustomizable>
    </WeaponHandleDNA>
  </WeaponDNA.Handle>
  <WeaponDNA.HeadLeft>
    <WeaponAxeDNA MaterialsForCustomizable='{x:Null}' AxeType='Symetrical' Sides='Double' SizeSingle='0.91341230325094069' Style='Basic'>
      <WeaponAxeDNA.KeyValues>
        <scg:SortedList x:TypeArguments='x:String, x:Double' Capacity='32'>
          <x:Double x:Key='edgeAngle'>30.28466989956252</x:Double>
          <x:Double x:Key='edgePercent'>0.23299733935684372</x:Double>
          <x:Double x:Key='isCenterFilled'>1</x:Double>
          <x:Double x:Key='leftAngle'>23.430908282022415</x:Double>
          <x:Double x:Key='leftAway'>1</x:Double>
          <x:Double x:Key='leftPercent'>0.27132182348115452</x:Double>
          <x:Double x:Key='leftX'>1.7394035089478845</x:Double>
          <x:Double x:Key='leftY'>1.9572450322831259</x:Double>
          <x:Double x:Key='rightAngle'>2.8094773193865441</x:Double>
          <x:Double x:Key='rightAway'>0</x:Double>
          <x:Double x:Key='rightPercent'>0.32522132705208906</x:Double>
          <x:Double x:Key='Scale1X'>0.62583759959127638</x:Double>
          <x:Double x:Key='Scale1Y'>0.804465643737682</x:Double>
          <x:Double x:Key='Scale2X'>0.49576195454958916</x:Double>
          <x:Double x:Key='Scale2Y'>0.33371522120870045</x:Double>
          <x:Double x:Key='Z1'>0.15842347377342494</x:Double>
          <x:Double x:Key='Z2L'>0.55184548482151963</x:Double>
          <x:Double x:Key='Z2R'>0.32764143264481355</x:Double>
        </scg:SortedList>
      </WeaponAxeDNA.KeyValues>
    </WeaponAxeDNA>
  </WeaponDNA.HeadLeft>
  <WeaponDNA.HeadRight>
    <WeaponSpikeBallDNA MaterialsForCustomizable='{x:Null}' Material='Bronze_Iron' Radius='0.26104198518568489'>
      <WeaponSpikeBallDNA.KeyValues>
        <scg:SortedList x:TypeArguments='x:String, x:Double' Capacity='4'>
          <x:Double x:Key='spikeLen'>0.31097453531947072</x:Double>
          <x:Double x:Key='spikeRadMult'>0.90199943558406059</x:Double>
        </scg:SortedList>
      </WeaponSpikeBallDNA.KeyValues>
    </WeaponSpikeBallDNA>
  </WeaponDNA.HeadRight>
</WeaponDNA>".
Replace('\'', '"');

                        #endregion
                        break;

                    case WEAPON_AXE_AXE:
                        #region axe+axe

                        dnaText = @"<WeaponDNA Name='{x:Null}' UniqueID='2dbdb276-c93f-40c4-8798-7f40a8e18b92' xmlns='clr-namespace:Game.Newt.v2.Arcanorum;assembly=Game.Newt.v2.Arcanorum' xmlns:scg='clr-namespace:System.Collections.Generic;assembly=System' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
  <WeaponDNA.Handle>
    <WeaponHandleDNA MaterialsForCustomizable='{x:Null}' AttachPointPercent='0.31524617851956105' HandleMaterial='Moon' HandleType='Rod' Length='4.6281575471293914' Radius='0.1635825340000831'>
      <WeaponHandleDNA.KeyValues>
        <scg:SortedList x:TypeArguments='x:String, x:Double' Capacity='16'>
          <x:Double x:Key='gem0Percent'>0.81437866592983654</x:Double>
          <x:Double x:Key='gem0Width'>0.91585512242154776</x:Double>
          <x:Double x:Key='maxRad1'>0.81487970998272286</x:Double>
          <x:Double x:Key='maxRad2'>0.79920432289093934</x:Double>
          <x:Double x:Key='numGems'>1</x:Double>
          <x:Double x:Key='tube1'>0.23341699671606708</x:Double>
          <x:Double x:Key='tube2'>0.580381130650817</x:Double>
          <x:Double x:Key='tube3'>1.234147305592963</x:Double>
          <x:Double x:Key='tube4'>15.99084179831465</x:Double>
          <x:Double x:Key='tube5'>1.4142340791477981</x:Double>
          <x:Double x:Key='tube6'>0.66123306499791379</x:Double>
          <x:Double x:Key='tube7'>1.7790868658293972</x:Double>
        </scg:SortedList>
      </WeaponHandleDNA.KeyValues>
    </WeaponHandleDNA>
  </WeaponDNA.Handle>
  <WeaponDNA.HeadLeft>
    <WeaponAxeDNA MaterialsForCustomizable='{x:Null}' AxeType='Lumber' Sides='Single_BackSpike' SizeSingle='0.80575843435048977' Style='Basic'>
      <WeaponAxeDNA.KeyValues>
        <scg:SortedList x:TypeArguments='x:String, x:Double' Capacity='32'>
          <x:Double x:Key='B1AngleL'>11.045840245692917</x:Double>
          <x:Double x:Key='B1AngleR'>8.9462631860969211</x:Double>
          <x:Double x:Key='B1PercentL'>0.41201959651756082</x:Double>
          <x:Double x:Key='B1PercentR'>0.51190066367243448</x:Double>
          <x:Double x:Key='B2AngleL'>56.350302107795287</x:Double>
          <x:Double x:Key='B2AngleR'>77.197151257282655</x:Double>
          <x:Double x:Key='B2PercentL'>0.42402739879862755</x:Double>
          <x:Double x:Key='B2PercentR'>0.58573393384747285</x:Double>
          <x:Double x:Key='EdgeAngleB'>16.4473886678216</x:Double>
          <x:Double x:Key='EdgeAngleT'>13.887304348210344</x:Double>
          <x:Double x:Key='EdgePercentB'>0.305469226467176</x:Double>
          <x:Double x:Key='EdgePercentT'>0.31596551375275733</x:Double>
          <x:Double x:Key='EndBRX'>-0.14811919491634065</x:Double>
          <x:Double x:Key='EndBRY'>0.027393858824278902</x:Double>
          <x:Double x:Key='EndBRZ'>0</x:Double>
          <x:Double x:Key='EndTRX'>-0.061393550950418978</x:Double>
          <x:Double x:Key='EndTRY'>0.17939039902463194</x:Double>
          <x:Double x:Key='EndTRZ'>0</x:Double>
          <x:Double x:Key='spikeLength'>1.0957333554493884</x:Double>
        </scg:SortedList>
      </WeaponAxeDNA.KeyValues>
    </WeaponAxeDNA>
  </WeaponDNA.HeadLeft>
  <WeaponDNA.HeadRight>
    <WeaponAxeDNA MaterialsForCustomizable='{x:Null}' AxeType='Bearded' Sides='Single_BackSpike' SizeSingle='0.96132166756378568' Style='Basic'>
      <WeaponAxeDNA.KeyValues>
        <scg:SortedList x:TypeArguments='x:String, x:Double' Capacity='32'>
          <x:Double x:Key='B1AngleL'>11.026186779386451</x:Double>
          <x:Double x:Key='B1AngleR'>8.20585230998178</x:Double>
          <x:Double x:Key='B1PercentL'>0.37912275502650195</x:Double>
          <x:Double x:Key='B1PercentR'>0.40881150511002418</x:Double>
          <x:Double x:Key='B2AngleL'>62.948543887095781</x:Double>
          <x:Double x:Key='B2AngleR'>70.954499706139089</x:Double>
          <x:Double x:Key='B2PercentL'>0.45416472789559736</x:Double>
          <x:Double x:Key='B2PercentR'>0.61473708758816914</x:Double>
          <x:Double x:Key='EdgeAngleB'>18.3419615616286</x:Double>
          <x:Double x:Key='EdgeAngleT'>16.723372462076775</x:Double>
          <x:Double x:Key='EdgePercentB'>0.30583385249172984</x:Double>
          <x:Double x:Key='EdgePercentT'>0.36689111737855296</x:Double>
          <x:Double x:Key='EndBL_1X'>0.09720167014095886</x:Double>
          <x:Double x:Key='EndBL_1Y'>0.12857852884536083</x:Double>
          <x:Double x:Key='EndBL_1Z'>0</x:Double>
          <x:Double x:Key='EndBRX'>-0.14864533295119514</x:Double>
          <x:Double x:Key='EndBRY'>-0.15719429685554953</x:Double>
          <x:Double x:Key='EndBRZ'>0</x:Double>
          <x:Double x:Key='EndTRX'>-0.1161657338041531</x:Double>
          <x:Double x:Key='EndTRY'>-0.097627966008173431</x:Double>
          <x:Double x:Key='EndTRZ'>0</x:Double>
          <x:Double x:Key='shouldExtendBeard'>1</x:Double>
          <x:Double x:Key='spikeLength'>0.940874604387616</x:Double>
        </scg:SortedList>
      </WeaponAxeDNA.KeyValues>
    </WeaponAxeDNA>
  </WeaponDNA.HeadRight>
</WeaponDNA>".
Replace('\'', '"');

                        #endregion
                        break;

                    default:
                        throw new ApplicationException("Unknown weapon type: " + cboWeapon.SelectedValue.ToString());
                }

                WeaponDNA dna = UtilityCore.DeserializeFromString<WeaponDNA>(dnaText);

                #endregion

                Weapon weapon = new Weapon(dna, new Point3D(), _world, _materialIDs.Weapon)
                {
                    ShowAttachPoint = true,
                };

                _keep2D.Add(weapon, false);
                _map.AddItem(weapon);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void SmackIt_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                #region strength

                double strength = 0;
                switch (cboSmackStrength.SelectedValue.ToString())
                {
                    case SMACK_STRENGTH_REALLYWEAK:
                        strength = .25;
                        break;

                    case SMACK_STRENGTH_WEAK:
                        strength = 1;
                        break;

                    case SMACK_STRENGTH_MED:
                        strength = 3;
                        break;

                    case SMACK_STRENGTH_HARD:
                        strength = 10;
                        break;

                    default:
                        throw new ApplicationException("Unknown smack strength: " + cboSmackStrength.SelectedValue.ToString());
                }

                #endregion
                #region direction

                Vector3D direction = new Vector3D();
                switch (cboSmackDirection.SelectedValue.ToString())
                {
                    case SMACK_DIR_X:
                        direction = new Vector3D(strength, 0, 0);
                        break;

                    case SMACK_DIR_Y:
                        direction = new Vector3D(0, strength, 0);
                        break;

                    case SMACK_DIR_Z:
                        direction = new Vector3D(0, 0, strength);
                        break;

                    case SMACK_DIR_CIRCLE:
                        direction = Math3D.GetRandomVector_Circular_Shell(strength);
                        break;

                    case SMACK_DIR_SPHERE:
                        direction = Math3D.GetRandomVector_Spherical_Shell(strength);
                        break;

                    default:
                        throw new ApplicationException("Unknown smack direction: " + cboSmackDirection.SelectedValue.ToString());
                }

                #endregion

                foreach (IMapObject item in _map.GetAllItems())
                {
                    #region position

                    Point3D position = new Point3D();
                    switch (cboSmackLocation.SelectedValue.ToString())
                    {
                        case SMACK_LOC_POSITION:
                            position = item.PositionWorld;
                            break;

                        case SMACK_LOC_MASS:
                            position = item.PhysicsBody.PositionToWorld(item.PhysicsBody.CenterOfMass);
                            break;

                        case SMACK_LOC_CIRCLE:
                            position = item.PhysicsBody.PositionToWorld(Math3D.GetRandomVector_Circular(item.Radius).ToPoint());
                            break;

                        case SMACK_LOC_SPHERE:
                            position = item.PhysicsBody.PositionToWorld(Math3D.GetRandomVector_Spherical(item.Radius).ToPoint());
                            break;

                        default:
                            throw new ApplicationException("Unknown smack position: " + cboSmackLocation.SelectedValue.ToString());
                    }

                    #endregion

                    //item.PhysicsBody.AddForceAtPoint(force, );        // this can only be done from within the apply force and torque event
                    item.PhysicsBody.AddVelocityAtPoint(direction, position);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TestMotionController2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EditorOptions editorOptions = new EditorOptions();
                ItemOptionsArco itemOptions = new ItemOptionsArco();

                BotDNA dna = new BotDNA()
                {
                    UniqueID = Guid.NewGuid(),

                    DraggingMaxVelocity = StaticRandom.NextPercent(10, .25),
                    DraggingMultiplier = StaticRandom.NextPercent(40, .25),

                    Parts = new ShipPartDNA[]
                    {
                        //new ShipPartDNA() { PartType = SensorVision.PARTTYPE, Position = new Point3D(0, 0, 1.5), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1) },
                        //new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(0, 0, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1) },
                        //new ShipPartDNA() { PartType = MotionController_Linear.PARTTYPE, Position = new Point3D(0, 0, -1.5), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1) },
                        new ShipPartDNA() { PartType = MotionController2.PARTTYPE, Position = new Point3D(0, 0, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1) },
                    }
                };

                DragHitShape dragPlane = new DragHitShape();
                dragPlane.SetShape_Plane(new Triangle(new Point3D(-1, -1, 0), new Point3D(1, -1, 0), new Point3D(0, 1, 0)));

                AIMousePlate mousePlate = new AIMousePlate(dragPlane)
                {
                    MaxXY = 20,
                    Scale = 1,
                };

                MotionController2 controller = new MotionController2(editorOptions, itemOptions, dna.Parts.First(o => o.PartType == MotionController2.PARTTYPE), mousePlate);


                // Save/Load test
                ShipPartDNA newPartDNA = controller.GetNewDNA();
                BotDNA newDNA = UtilityCore.Clone(dna);
                newDNA.Parts = new[] { newPartDNA };

                MotionController2 controller2 = new MotionController2(editorOptions, itemOptions, newDNA.Parts.First(o => o.PartType == MotionController2.PARTTYPE), mousePlate);


                for (int cntr = 0; cntr < 10; cntr++)
                {
                    foreach (var neuron in controller2.Neruons_All)
                    {
                        if (neuron is Neuron_SensorPosition neuronCast)
                            neuronCast.Value = StaticRandom.NextDouble(neuron.IsPositiveOnly ? 0 : -1, 1);
                    }

                    controller2.Update_AnyThread(1);
                }

                controller.Dispose();
                controller2.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DrawArcArrow1_Click(object sender, RoutedEventArgs e)
        {
            const double THICKNESS = .025;
            const double RADIUS = 1;

            try
            {
                Color color = UtilityWPF.ColorFromHex("000000");

                Debug3DWindow window = new Debug3DWindow();

                window.AddAxisLines(RADIUS * 1.5, THICKNESS);

                Transform3DGroup transform = new Transform3DGroup();
                transform.Children.Add(new ScaleTransform3D(RADIUS, RADIUS, RADIUS));

                window.AddMesh(UtilityWPF.GetCircle2D(20, transform, Transform3D.Identity), color);


                //MaterialGroup materials = new MaterialGroup();
                //materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
                //materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(color, Colors.White, .5d)), 100d));

                //Transform3DGroup transform = new Transform3DGroup();
                //transform.Children.Add(new ScaleTransform3D(RADIUS, RADIUS, RADIUS));
                //transform.Children.Add(new TranslateTransform3D(0, 0, -.01));

                //GeometryModel3D geometry = new GeometryModel3D();
                //geometry.Material = materials;
                //geometry.BackMaterial = materials;
                //geometry.Geometry = UtilityWPF.GetCircle2D(20, transform, Transform3D.Identity);



                //ModelVisual3D visual = new ModelVisual3D();
                //visual.Content = model;

                //_visuals.Add(visual);
                //_viewport.Children.Add(visual);


                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void DrawArcArrow2_Click(object sender, RoutedEventArgs e)
        {
            const double THICKNESS = .01;
            const double THICKNESS2 = .025;
            const double RADIUS = 3;
            const double CIRCLETHICKNESS = .5;
            const int NUMSIDES_TOTAL = 13;
            const int NUMSIDES_USE = 11;

            try
            {
                Color gray = UtilityWPF.ColorFromHex("A0A0A0");
                Color darkGray = UtilityWPF.ColorFromHex("606060");
                Color red = UtilityWPF.ColorFromHex("E0C0C0");
                Color green = UtilityWPF.ColorFromHex("C0E0C0");
                Color blue = UtilityWPF.ColorFromHex("C0C0E0");

                Debug3DWindow window = new Debug3DWindow();

                window.AddAxisLines(RADIUS * 1.5, THICKNESS);

                Point[] pointsTheta = Math2D.GetCircle_Cached(NUMSIDES_TOTAL);

                var circlePoints = pointsTheta.
                    Select(o => GetCirclePoints1(o, RADIUS, CIRCLETHICKNESS)).
                    ToArray();

                window.AddLine(circlePoints[0].inner, circlePoints[0].outer, THICKNESS2, darkGray);

                for (int cntr = 0; cntr < NUMSIDES_USE - 1; cntr++)
                {
                    window.AddLine(circlePoints[cntr].inner, circlePoints[cntr + 1].inner, THICKNESS2, darkGray);

                    window.AddLine(circlePoints[cntr].inner, circlePoints[cntr].outer, THICKNESS, gray);
                    window.AddLine(circlePoints[cntr].mid, circlePoints[cntr + 1].mid, THICKNESS, gray);
                    window.AddLine(circlePoints[cntr + 1].inner, circlePoints[cntr + 1].outer, THICKNESS, gray);

                    window.AddLine(circlePoints[cntr].outer, circlePoints[cntr + 1].outer, THICKNESS2, darkGray);

                    window.AddTriangle(circlePoints[cntr].inner, circlePoints[cntr].outer, circlePoints[cntr + 1].inner, green);
                    window.AddTriangle(circlePoints[cntr].outer, circlePoints[cntr + 1].outer, circlePoints[cntr + 1].inner, blue);
                }

                int i1 = NUMSIDES_USE - 1;
                int i2 = NUMSIDES_USE;

                var arrowBase = GetCirclePoints1(pointsTheta[i1], RADIUS, CIRCLETHICKNESS * 2);

                window.AddLine(arrowBase.inner, circlePoints[i1].inner, THICKNESS2, darkGray);
                window.AddLine(arrowBase.outer, circlePoints[i1].outer, THICKNESS2, darkGray);

                window.AddLine(arrowBase.inner, circlePoints[i2].mid, THICKNESS2, darkGray);
                window.AddLine(circlePoints[i1].mid, circlePoints[i2].mid, THICKNESS, gray);
                window.AddLine(arrowBase.outer, circlePoints[i2].mid, THICKNESS2, darkGray);

                window.AddTriangle(arrowBase.inner, arrowBase.outer, circlePoints[i2].mid, red);

                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void DrawArcArrow3_Click(object sender, RoutedEventArgs e)
        {
            const double THICKNESS = .01;
            const double THICKNESS2 = .025;
            const double RADIUS = 3;
            const double CIRCLETHICKNESS = .5;
            const double ARROWTHICKNESS = CIRCLETHICKNESS * 2;
            const int NUMSIDES_TOTAL = 13;
            const int NUMSIDES_USE = 11;

            try
            {
                Color gray = UtilityWPF.ColorFromHex("A0A0A0");
                Color darkGray = UtilityWPF.ColorFromHex("606060");
                Color red = UtilityWPF.ColorFromHex("E0C0C0");
                Color green = UtilityWPF.ColorFromHex("C0E0C0");
                Color blue = UtilityWPF.ColorFromHex("C0C0E0");

                Debug3DWindow window = new Debug3DWindow();

                window.AddAxisLines(RADIUS * 1.5, THICKNESS);

                Point[] pointsTheta = Math2D.GetCircle_Cached(NUMSIDES_TOTAL);

                var circlePoints = pointsTheta.
                    Select(o => GetCirclePoints1(o, RADIUS, CIRCLETHICKNESS)).
                    ToArray();

                window.AddLine(circlePoints[0].inner, circlePoints[0].outer, THICKNESS2, darkGray);

                for (int cntr = 0; cntr < NUMSIDES_USE - 1; cntr++)
                {
                    window.AddLine(circlePoints[cntr].inner, circlePoints[cntr + 1].inner, THICKNESS2, darkGray);

                    window.AddLine(circlePoints[cntr].inner, circlePoints[cntr].outer, THICKNESS, gray);
                    window.AddLine(circlePoints[cntr].mid, circlePoints[cntr + 1].mid, THICKNESS, gray);
                    window.AddLine(circlePoints[cntr + 1].inner, circlePoints[cntr + 1].outer, THICKNESS, gray);

                    window.AddLine(circlePoints[cntr].outer, circlePoints[cntr + 1].outer, THICKNESS2, darkGray);

                    window.AddTriangle(circlePoints[cntr].inner, circlePoints[cntr].outer, circlePoints[cntr + 1].inner, green);
                    window.AddTriangle(circlePoints[cntr].outer, circlePoints[cntr + 1].outer, circlePoints[cntr + 1].inner, blue);
                }

                int i1 = NUMSIDES_USE - 1;
                int i2 = NUMSIDES_USE;

                var arrowBase = GetCirclePoints1(pointsTheta[i1], RADIUS, ARROWTHICKNESS);
                var arrowTip = GetCirclePoints1(pointsTheta[i2], RADIUS, CIRCLETHICKNESS * .75);

                window.AddLine(arrowBase.inner, circlePoints[i1].inner, THICKNESS2, darkGray);
                window.AddLine(arrowBase.outer, circlePoints[i1].outer, THICKNESS2, darkGray);

                window.AddLine(arrowBase.inner, arrowTip.outer, THICKNESS2, darkGray);
                window.AddLine(circlePoints[i1].mid, arrowTip.outer, THICKNESS, gray);
                window.AddLine(arrowBase.outer, arrowTip.outer, THICKNESS2, darkGray);

                window.AddTriangle(arrowBase.inner, arrowBase.outer, arrowTip.outer, red);

                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void DrawCrossArrow1_Click(object sender, RoutedEventArgs e)
        {
            const double THICKNESS = .01;
            const double THICKNESS2 = .025;
            const double RADIUS = 3;
            const double LINETHICKNESS = .5;
            const double ARROWTHICKNESS = LINETHICKNESS * 2;

            try
            {
                Color gray = UtilityWPF.ColorFromHex("A0A0A0");
                Color darkGray = UtilityWPF.ColorFromHex("606060");
                Color red = UtilityWPF.ColorFromHex("E0C0C0");
                Color green = UtilityWPF.ColorFromHex("C0E0C0");
                Color blue = UtilityWPF.ColorFromHex("C0C0E0");
                Color yellow = UtilityWPF.ColorFromHex("E0E0C0");
                Color purple = UtilityWPF.ColorFromHex("E0C0E0");

                Debug3DWindow window = new Debug3DWindow();

                window.AddAxisLines(RADIUS * 1.5, THICKNESS);

                Vector[] cores1 = new[]
                {
                    new Vector(1,0),
                    new Vector(0, 1),
                    new Vector(-1, 0),
                    new Vector(0, -1),
                };

                var cores2 = cores1.
                    Select(o =>
                    (
                        o * LINETHICKNESS / 2,
                        o * (RADIUS - 1),
                        o * RADIUS
                    )).
                    ToArray();

                var lines = cores2.
                    Select(o => new
                    {
                        from = o.Item1.ToPoint3D(),
                        to = o.Item2.ToPoint3D(),
                        tip = o.Item3.ToPoint3D(),
                        bar = GetLinePoints1(o.Item1, o.Item2, LINETHICKNESS, ARROWTHICKNESS),
                    }).
                    ToArray();

                foreach (var line in lines)
                {
                    window.AddDot(line.from, THICKNESS * 1.5, gray);
                    window.AddDot(line.to, THICKNESS * 1.5, gray);

                    window.AddLine(new Point3D(0, 0, 0), line.from, THICKNESS, gray);
                    window.AddLine(line.from, line.to, THICKNESS, gray);
                    window.AddLine(line.to, line.tip, THICKNESS, gray);

                    window.AddLine(new Point3D(0, 0, 0), line.bar.fromLeft, THICKNESS, gray);
                    window.AddLine(new Point3D(0, 0, 0), line.bar.fromRight, THICKNESS, gray);

                    window.AddLine(line.bar.fromLeft, line.bar.toLeft, THICKNESS2, darkGray);
                    window.AddLine(line.bar.toLeft, line.bar.baseLeft, THICKNESS2, darkGray);
                    window.AddLine(line.bar.baseLeft, line.tip, THICKNESS2, darkGray);
                    window.AddLine(line.bar.baseRight, line.tip, THICKNESS2, darkGray);
                    window.AddLine(line.bar.toRight, line.bar.baseRight, THICKNESS2, darkGray);
                    window.AddLine(line.bar.fromRight, line.bar.toRight, THICKNESS2, darkGray);

                    window.AddTriangle(line.bar.fromLeft, line.bar.fromRight, line.bar.toRight, green);
                    window.AddTriangle(line.bar.toRight, line.bar.toLeft, line.bar.fromLeft, blue);
                    window.AddTriangle(line.bar.baseLeft, line.bar.baseRight, line.tip, red);

                    window.AddTriangle(new Point3D(0, 0, 0), line.bar.fromRight, line.bar.fromLeft, yellow);

                }


                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Polar_Click(object sender, RoutedEventArgs e)
        {
            const double THICKNESS = .01;
            const double RADIUS = 3;
            const double DOT = .05;

            const double HALFPI = Math.PI / 2d;

            try
            {
                Debug3DWindow window = new Debug3DWindow();

                window.AddAxisLines(RADIUS * 1.5, THICKNESS);


                Point3D polarOrigin = Math3D.GetPoint_Polar(0, HALFPI, RADIUS);
                window.AddDot(polarOrigin, DOT, Colors.White);

                // ArcLength:
                // s=rt
                // t=s/r

                double pointX = 1;
                double pointY = 1;

                Point3D polarX = Math3D.GetPoint_Polar(pointX / RADIUS, HALFPI, RADIUS);
                window.AddDot(polarX, DOT, Colors.DodgerBlue);
                window.AddLine(polarOrigin, polarOrigin + new Vector3D(0, pointX, 0), THICKNESS, Colors.DodgerBlue);

                Point3D polarY = Math3D.GetPoint_Polar(0, HALFPI - (pointY / RADIUS), RADIUS);
                window.AddDot(polarY, DOT, Colors.OrangeRed);
                window.AddLine(polarOrigin, polarOrigin + new Vector3D(0, 0, pointY), THICKNESS, Colors.OrangeRed);


                for (double theta = 0; theta < Math.PI; theta += .1)
                {
                    window.AddDot(Math3D.GetPoint_Polar(theta, HALFPI, RADIUS), DOT / 4, Colors.DeepSkyBlue);
                }

                for (double phi = 0; phi < Math.PI; phi += .1)
                {
                    window.AddDot(Math3D.GetPoint_Polar(0, phi, RADIUS), DOT / 4, Colors.Coral);
                }



                window.AddDot(new Point3D(0, 0, 0), RADIUS, UtilityWPF.ColorFromHex("18000000"), isHiRes: true);

                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void RotateTetrahedron_Click(object sender, RoutedEventArgs e)
        {
            const double THICKNESS = .01;
            const double RADIUS = 3;
            const double DOT = .05;

            try
            {
                Debug3DWindow window = new Debug3DWindow();

                //window.AddAxisLines(RADIUS, THICKNESS);

                (Point3D pos, Vector3D right, Vector3D up) image = (new Point3D(RADIUS, 0, 0), new Vector3D(0, .33, 0), new Vector3D(0, 0, .33));

                Tetrahedron tetra = UtilityWPF.GetTetrahedron(1);

                window.AddLine(new Point3D(0, 0, 0), image.pos, THICKNESS, Colors.OliveDrab);
                window.AddLine(image.pos, image.pos + image.right, THICKNESS, Colors.OliveDrab);
                window.AddLine(image.pos, image.pos + image.up, THICKNESS, Colors.OliveDrab);
                window.AddDot(image.pos, DOT, Colors.OliveDrab);

                foreach (Point3D dir in tetra.AllPoints)
                {
                    Quaternion randRot = Math3D.GetRotation(image.right, Math3D.GetArbitraryOrhonganal(image.pos.ToVector()));
                    Quaternion majorRot = Math3D.GetRotation(image.pos.ToVector(), dir.ToVector());

                    Transform3DGroup transform = new Transform3DGroup();
                    transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(randRot)));
                    transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(majorRot)));

                    (Point3D pos, Vector3D right, Vector3D up) newImage = (transform.Transform(image.pos), transform.Transform(image.right), transform.Transform(image.up));

                    window.AddLine(new Point3D(0, 0, 0), newImage.pos, THICKNESS, Colors.OrangeRed);
                    window.AddLine(newImage.pos, newImage.pos + newImage.right, THICKNESS, Colors.OrangeRed);
                    window.AddLine(newImage.pos, newImage.pos + newImage.up, THICKNESS, Colors.OrangeRed);
                    window.AddDot(newImage.pos, DOT, Colors.OrangeRed);
                }

                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ImpulseRotate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _keep2D.Clear();
                _map.Clear();

                ShipPartDNA[] parts = new ShipPartDNA[]
                {
                    new ImpulseEngineDNA() { PartType = ImpulseEngine.PARTTYPE, Position = new Point3D(0,0,0), Orientation = Quaternion.Identity, Scale = new Vector3D(1,1,1), ImpulseEngineType = ImpulseEngineType.Rotate },
                };

                ShipDNA dna = ShipDNA.Create("i", parts);

                ShipCoreArgs core = new ShipCoreArgs()
                {
                    World = _world,
                    Material_Ship = _materialIDs.Bot,
                    Map = _map,
                };

                // Create the bot
                BotConstruction_Result construction = BotConstructor.ConstructBot(dna, core);
                _bot = new Bot(construction);

                //_keep2D.Add(_bot, false);
                _map.AddItem(_bot);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void DrawArcArrow4_Click(object sender, RoutedEventArgs e)
        {
            const double THICKNESS = .01;
            const double THICKNESS2 = .025;
            const double CIRCLERADIUS = 1;
            const double CIRCLETHICKNESS = .5 / 3;
            const double ARROWTHICKNESS = CIRCLETHICKNESS * 2;
            const int NUMSIDES_TOTAL = 13;
            const int NUMSIDES_USE = 11;

            try
            {
                if (!double.TryParse(txtSphereRadius.Text, out double SPHERERADIUS))
                {
                    MessageBox.Show("Couldn't parse sphere radius as a double", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Color gray = UtilityWPF.ColorFromHex("A0A0A0");
                Color darkGray = UtilityWPF.ColorFromHex("606060");
                Color red = UtilityWPF.ColorFromHex("E0C0C0");
                Color green = UtilityWPF.ColorFromHex("C0E0C0");
                Color blue = UtilityWPF.ColorFromHex("C0C0E0");
                Color yellow = UtilityWPF.ColorFromHex("E0E0C0");
                Color purple = UtilityWPF.ColorFromHex("E0C0E0");

                Debug3DWindow window = new Debug3DWindow();

                window.AddAxisLines(SPHERERADIUS * 1.5, THICKNESS);

                Point[] pointsTheta = Math2D.GetCircle_Cached(NUMSIDES_TOTAL);

                var circlePoints = pointsTheta.
                    Select(o => GetCirclePoints2(o, CIRCLERADIUS, CIRCLETHICKNESS, SPHERERADIUS)).
                    ToArray();

                window.AddLine(circlePoints[0].inner, circlePoints[0].outer, THICKNESS2, darkGray);

                for (int cntr = 0; cntr < NUMSIDES_USE - 1; cntr++)
                {
                    window.AddLine(circlePoints[cntr].inner, circlePoints[cntr + 1].inner, THICKNESS2, darkGray);

                    window.AddLine(circlePoints[cntr].inner, circlePoints[cntr].outer, THICKNESS, gray);
                    window.AddLine(circlePoints[cntr].mid, circlePoints[cntr + 1].mid, THICKNESS, gray);
                    window.AddLine(circlePoints[cntr + 1].inner, circlePoints[cntr + 1].outer, THICKNESS, gray);

                    window.AddLine(circlePoints[cntr].outer, circlePoints[cntr + 1].outer, THICKNESS2, darkGray);

                    window.AddTriangle(circlePoints[cntr].inner, circlePoints[cntr].outer, circlePoints[cntr + 1].inner, green);
                    window.AddTriangle(circlePoints[cntr].outer, circlePoints[cntr + 1].outer, circlePoints[cntr + 1].inner, blue);
                }

                int i1 = NUMSIDES_USE - 1;
                int i2 = NUMSIDES_USE;

                var arrowBase = GetCirclePoints2(pointsTheta[i1], CIRCLERADIUS, ARROWTHICKNESS, SPHERERADIUS);
                var arrowTip = GetCirclePoints2(pointsTheta[i2], CIRCLERADIUS, CIRCLETHICKNESS * .75, SPHERERADIUS);        //NOTE: Not using mid, because that curls the arrow too steeply (looks odd).  So using outer as a comprimise between pointing in and pointing straight

                window.AddLine(arrowBase.inner, circlePoints[i1].inner, THICKNESS2, darkGray);
                window.AddLine(arrowBase.outer, circlePoints[i1].outer, THICKNESS2, darkGray);

                window.AddLine(arrowBase.inner, arrowTip.outer, THICKNESS2, darkGray);
                window.AddLine(circlePoints[i1].inner, arrowTip.outer, THICKNESS, gray);
                window.AddLine(circlePoints[i1].outer, arrowTip.outer, THICKNESS, gray);
                window.AddLine(arrowBase.outer, arrowTip.outer, THICKNESS2, darkGray);

                window.AddTriangle(arrowBase.inner, circlePoints[i1].inner, arrowTip.outer, yellow);
                window.AddTriangle(circlePoints[i1].inner, circlePoints[i1].outer, arrowTip.outer, red);
                window.AddTriangle(circlePoints[i1].outer, arrowBase.outer, arrowTip.outer, purple);

                window.AddDot(new Point3D(0, 0, 0), SPHERERADIUS, UtilityWPF.ColorFromHex("18000000"), isHiRes: true);

                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void DrawCrossArrow2_Click(object sender, RoutedEventArgs e)
        {
            const double THICKNESS = .01;
            const double THICKNESS2 = .025;
            const double SIZE = 1;
            const double LINETHICKNESS = .5 / 3;
            const double ARROWTHICKNESS = LINETHICKNESS * 2;

            try
            {
                if (!double.TryParse(txtSphereRadius.Text, out double SPHERERADIUS))
                {
                    MessageBox.Show("Couldn't parse sphere radius as a double", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Color gray = UtilityWPF.ColorFromHex("A0A0A0");
                Color darkGray = UtilityWPF.ColorFromHex("606060");
                Color red = UtilityWPF.ColorFromHex("E0C0C0");
                Color green = UtilityWPF.ColorFromHex("C0E0C0");
                Color blue = UtilityWPF.ColorFromHex("C0C0E0");
                Color yellow = UtilityWPF.ColorFromHex("E0E0C0");
                Color purple = UtilityWPF.ColorFromHex("E0C0E0");

                Debug3DWindow window = new Debug3DWindow();

                window.AddAxisLines(SPHERERADIUS * 1.5, THICKNESS);

                Vector[] cores1 = new[]
                {
                    new Vector(1,0),
                    new Vector(0, 1),
                    new Vector(-1, 0),
                    new Vector(0, -1),
                };

                var cores2 = cores1.
                    Select(o =>
                    (
                        o * LINETHICKNESS / 2,
                        o * (SIZE - (1d / 3d)),
                        o * SIZE
                    )).
                    ToArray();

                var lines = cores2.
                    Select(o => new
                    {
                        from = Math3D.ProjectPointOntoSphere(o.Item1.X, o.Item1.Y, SPHERERADIUS),
                        to = Math3D.ProjectPointOntoSphere(o.Item2.X, o.Item2.Y, SPHERERADIUS),
                        tip = Math3D.ProjectPointOntoSphere(o.Item3.X, o.Item3.Y, SPHERERADIUS),
                        bar = GetLinePoints2(o.Item1, o.Item2, LINETHICKNESS, ARROWTHICKNESS, SPHERERADIUS),
                    }).
                    ToArray();

                Point3D origin = Math3D.ProjectPointOntoSphere(0, 0, SPHERERADIUS);

                foreach (var line in lines)
                {
                    window.AddDot(line.from, THICKNESS * 1.5, gray);
                    window.AddDot(line.to, THICKNESS * 1.5, gray);

                    window.AddLine(origin, line.from, THICKNESS, gray);
                    window.AddLine(line.from, line.to, THICKNESS, gray);

                    //window.AddLine(line.to, line.tip, THICKNESS, gray);
                    window.AddLine(line.bar.toLeft, line.tip, THICKNESS, gray);
                    window.AddLine(line.bar.toRight, line.tip, THICKNESS, gray);


                    window.AddLine(origin, line.bar.fromLeft, THICKNESS, gray);
                    window.AddLine(origin, line.bar.fromRight, THICKNESS, gray);

                    window.AddLine(line.bar.fromLeft, line.bar.toLeft, THICKNESS2, darkGray);
                    window.AddLine(line.bar.toLeft, line.bar.baseLeft, THICKNESS2, darkGray);
                    window.AddLine(line.bar.baseLeft, line.tip, THICKNESS2, darkGray);
                    window.AddLine(line.bar.baseRight, line.tip, THICKNESS2, darkGray);
                    window.AddLine(line.bar.toRight, line.bar.baseRight, THICKNESS2, darkGray);
                    window.AddLine(line.bar.fromRight, line.bar.toRight, THICKNESS2, darkGray);

                    window.AddTriangle(line.bar.fromLeft, line.bar.fromRight, line.bar.toRight, green);
                    window.AddTriangle(line.bar.toRight, line.bar.toLeft, line.bar.fromLeft, blue);




                    //window.AddTriangle(line.bar.baseLeft, line.bar.baseRight, line.tip, red);
                    window.AddTriangle(line.bar.baseLeft, line.bar.toLeft, line.tip, yellow);
                    window.AddTriangle(line.bar.toLeft, line.bar.toRight, line.tip, red);
                    window.AddTriangle(line.bar.toRight, line.bar.baseRight, line.tip, purple);




                    window.AddTriangle(origin, line.bar.fromRight, line.bar.fromLeft, yellow);
                }

                window.AddDot(new Point3D(0, 0, 0), SPHERERADIUS, UtilityWPF.ColorFromHex("18000000"), isHiRes: true);

                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void DrawArcArrow5_Click(object sender, RoutedEventArgs e)
        {
            const double THICKNESS = .01;
            const double THICKNESS2 = .025;
            const double CIRCLERADIUS = 1;
            const double CIRCLETHICKNESS = .5 / 3;
            const double ARROWTHICKNESS = CIRCLETHICKNESS * 2;
            const int NUMSIDES_TOTAL = 13;
            const int NUMSIDES_USE = 9;

            try
            {
                if (!double.TryParse(txtSphereRadius.Text, out double SPHERERADIUS))
                {
                    MessageBox.Show("Couldn't parse sphere radius as a double", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Color green = UtilityWPF.ColorFromHex("C0E0C0");

                Debug3DWindow window = new Debug3DWindow();

                window.AddAxisLines(SPHERERADIUS * 1.5, THICKNESS);

                Point[] pointsTheta = Math2D.GetCircle_Cached(NUMSIDES_TOTAL);

                var circlePoints = pointsTheta.
                    Select(o => GetCirclePoints2(o, CIRCLERADIUS, CIRCLETHICKNESS, SPHERERADIUS)).
                    ToArray();

                List<Triangle> triangles = new List<Triangle>();

                for (int cntr = 0; cntr < NUMSIDES_USE - 1; cntr++)
                {
                    triangles.Add(new Triangle(circlePoints[cntr].inner, circlePoints[cntr].outer, circlePoints[cntr + 1].inner));
                    triangles.Add(new Triangle(circlePoints[cntr].outer, circlePoints[cntr + 1].outer, circlePoints[cntr + 1].inner));
                }

                int i1 = NUMSIDES_USE - 1;
                int i2 = NUMSIDES_USE;

                var arrowBase = GetCirclePoints2(pointsTheta[i1], CIRCLERADIUS, ARROWTHICKNESS, SPHERERADIUS);
                var arrowTip = GetCirclePoints2(pointsTheta[i2], CIRCLERADIUS, CIRCLETHICKNESS * .75, SPHERERADIUS);        //NOTE: Not using mid, because that curls the arrow too steeply (looks odd).  So using outer as a comprimise between pointing in and pointing straight

                triangles.Add(new Triangle(arrowBase.inner, circlePoints[i1].inner, arrowTip.outer));
                triangles.Add(new Triangle(circlePoints[i1].inner, circlePoints[i1].outer, arrowTip.outer));
                triangles.Add(new Triangle(circlePoints[i1].outer, arrowBase.outer, arrowTip.outer));

                // It would be more efficient to build the link triangles directly, but a lot more logic
                ITriangleIndexed[] indexedTriangles = TriangleIndexed.ConvertToIndexed(triangles.ToArray());

                window.AddHull(indexedTriangles, green, isIndependentFaces: false);


                //window.AddDot(new Point3D(0, 0, 0), SPHERERADIUS, UtilityWPF.ColorFromHex("18000000"), isHiRes: true);

                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void DrawCrossArrow3_Click(object sender, RoutedEventArgs e)
        {
            const double THICKNESS = .01;
            const double THICKNESS2 = .025;
            const double SIZE = 1;
            const double LINETHICKNESS = .5 / 3;
            const double ARROWTHICKNESS = LINETHICKNESS * 2;

            try
            {
                if (!double.TryParse(txtSphereRadius.Text, out double SPHERERADIUS))
                {
                    MessageBox.Show("Couldn't parse sphere radius as a double", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Color red = UtilityWPF.ColorFromHex("E0C0C0");
                Color green = UtilityWPF.ColorFromHex("C0E0C0");
                Color blue = UtilityWPF.ColorFromHex("C0C0E0");
                Color yellow = UtilityWPF.ColorFromHex("E0E0C0");
                Color purple = UtilityWPF.ColorFromHex("E0C0E0");

                Debug3DWindow window = new Debug3DWindow();

                window.AddAxisLines(SPHERERADIUS * 1.5, THICKNESS);

                Vector[] cores1 = new[]
                {
                    new Vector(1,0),
                    new Vector(0, 1),
                    new Vector(-1, 0),
                    new Vector(0, -1),
                };

                var cores2 = cores1.
                    Select(o =>
                    (
                        o * LINETHICKNESS / 2,
                        o * (SIZE - (1d / 3d)),
                        o * SIZE
                    )).
                    ToArray();

                var lines = cores2.
                    Select(o => new
                    {
                        from = Math3D.ProjectPointOntoSphere(o.Item1.X, o.Item1.Y, SPHERERADIUS),
                        to = Math3D.ProjectPointOntoSphere(o.Item2.X, o.Item2.Y, SPHERERADIUS),
                        tip = Math3D.ProjectPointOntoSphere(o.Item3.X, o.Item3.Y, SPHERERADIUS),
                        bar = GetLinePoints2(o.Item1, o.Item2, LINETHICKNESS, ARROWTHICKNESS, SPHERERADIUS),
                    }).
                    ToArray();

                Point3D origin = Math3D.ProjectPointOntoSphere(0, 0, SPHERERADIUS);

                List<Triangle> triangles = new List<Triangle>();

                foreach (var line in lines)
                {
                    triangles.Add(new Triangle(origin, line.bar.fromRight, line.bar.fromLeft));

                    triangles.Add(new Triangle(line.bar.fromLeft, line.bar.fromRight, line.bar.toRight));
                    triangles.Add(new Triangle(line.bar.toRight, line.bar.toLeft, line.bar.fromLeft));

                    triangles.Add(new Triangle(line.bar.baseLeft, line.bar.toLeft, line.tip));
                    triangles.Add(new Triangle(line.bar.toLeft, line.bar.toRight, line.tip));
                    triangles.Add(new Triangle(line.bar.toRight, line.bar.baseRight, line.tip));
                }

                ITriangleIndexed[] indexedTriangles = TriangleIndexed.ConvertToIndexed(triangles.ToArray());

                window.AddHull(indexedTriangles, green, isIndependentFaces: false);

                //window.AddDot(new Point3D(0, 0, 0), SPHERERADIUS, UtilityWPF.ColorFromHex("18000000"), isHiRes: true);

                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Tiling2D_Click(object sender, RoutedEventArgs e)
        {
            const double CELLSIZE = 1;

            try
            {
                Debug3DWindow window = new Debug3DWindow();

                var samples = Enumerable.Range(1, 20).
                    Select(o =>
                    {
                        int sqrt = Math.Sqrt(o).ToInt_Floor();
                        int mult = o / sqrt;

                        return new
                        {
                            count = o,
                            cols = mult + ((o % sqrt) == 0 ? 0 : 1),
                            rows = sqrt,
                        };
                    }).
                    ToArray();

                double offSet = 0;

                foreach (var sample in samples)
                {
                    var cells = Math2D.GetCells(CELLSIZE, sample.cols, sample.rows).
                        ToArray();

                    for (int cntr = 0; cntr < sample.count; cntr++)
                    {
                        Rect cellRect = cells[cntr].rect.ChangeSize(.9);

                        window.AddMesh(UtilityWPF.GetSquare2D(
                            new Point(cellRect.X, cellRect.Y + offSet),
                            new Point(cellRect.X + cellRect.Width, cellRect.Y + cellRect.Height + offSet)),
                            UtilityWPF.AlphaBlend(Colors.Yellow, Colors.Blue, Convert.ToDouble(cntr) / Convert.ToDouble(sample.count)));
                    }

                    window.AddText3D(string.Format("{0}: {1}x{2}", sample.count, sample.cols, sample.rows), new Point3D(cells.Min(o => o.rect.X) - (CELLSIZE * 3), offSet, 0), new Vector3D(0, 0, 1), CELLSIZE / 2, Colors.Black, false);

                    offSet += (sample.rows * CELLSIZE) + CELLSIZE;
                }


                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Tiling2D_invert_Click(object sender, RoutedEventArgs e)
        {
            const double CELLSIZE = 1;

            try
            {
                Debug3DWindow window = new Debug3DWindow();

                window.AddAxisLines(CELLSIZE * 10, .005);

                var samples = Enumerable.Range(1, 50).
                    Select(o =>
                    {
                        int sqrt = Math.Sqrt(o).ToInt_Floor();
                        int mult = o / sqrt;

                        return new
                        {
                            count = o,
                            cols = mult + ((o % sqrt) == 0 ? 0 : 1),
                            rows = sqrt,
                        };
                    }).
                    ToArray();

                double offSet = 0;

                foreach (var sample in samples)
                {
                    var cells = Math2D.GetCells_InvertY(CELLSIZE, sample.cols, sample.rows);

                    for (int cntr = 0; cntr < sample.count; cntr++)
                    {
                        Rect cellRect = cells[cntr].rect.ChangeSize(.9);

                        window.AddMesh(UtilityWPF.GetSquare2D(
                            new Point(cellRect.X, cellRect.Y + offSet),
                            new Point(cellRect.X + cellRect.Width, cellRect.Y + cellRect.Height + offSet)),
                            sample.count == 1 ?
                                Colors.Blue :
                                UtilityWPF.AlphaBlend(Colors.Yellow, Colors.Blue, Convert.ToDouble(cntr) / Convert.ToDouble(sample.count - 1)));
                    }

                    window.AddText3D(
                        string.Format("{0}: {1}x{2}", sample.count, sample.cols, sample.rows),
                        new Point3D(cells.Min(o => o.rect.X) - (CELLSIZE * 3), offSet, 0),
                        new Vector3D(0, 0, 1),
                        CELLSIZE / 2,
                        Colors.Black,
                        false);

                    offSet -= (sample.rows * CELLSIZE) + CELLSIZE;
                }


                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Tiling2D_margin_Click(object sender, RoutedEventArgs e)
        {
            const double THICKNESS = .025;
            const double CELLSIZE = 5;
            const double MARGIN = .5;
            const int NUMX = 6;
            const int NUMY = 5;

            try
            {
                Debug3DWindow window = null;

                Action<(Rect rect, Point center)[], double, Color, Color> drawCells = (c, z, c1, c2) =>
                {
                    int index = 0;
                    foreach (var cell in c)
                    {
                        window.AddMesh(UtilityWPF.GetSquare2D(
                            cell.rect.Location,
                            cell.rect.BottomRight,
                            z),
                            c1);

                        window.AddLines(new[]
                            {
                                (cell.rect.TopLeft.ToPoint3D(z), cell.rect.TopRight.ToPoint3D(z)),
                                (cell.rect.BottomLeft.ToPoint3D(z), cell.rect.BottomRight.ToPoint3D(z)),
                                (cell.rect.TopLeft.ToPoint3D(z), cell.rect.BottomLeft.ToPoint3D(z)),
                                (cell.rect.TopRight.ToPoint3D(z), cell.rect.BottomRight.ToPoint3D(z)),
                            },
                            THICKNESS,
                            c2);

                        window.AddDot(cell.center.ToPoint3D(z), THICKNESS * 4, c2);

                        window.AddText3D(index.ToString(), cell.center.ToPoint3D(z), new Vector3D(0, 0, 1), cell.rect.Height / 3, c2, true);

                        index++;
                    }
                };

                window = new Debug3DWindow()
                {
                    Title = "Math2D.GetCells",
                };

                window.AddAxisLines(CELLSIZE * Math.Max(NUMX, NUMY), THICKNESS);

                drawCells(Math2D.GetCells(CELLSIZE, NUMX, NUMY), .1, Colors.RosyBrown, Colors.Sienna);
                drawCells(Math2D.GetCells(CELLSIZE, NUMX, NUMY, MARGIN), -.1, Colors.CadetBlue, Colors.DarkSlateGray);

                window.Show();


                window = new Debug3DWindow()
                {
                    Title = "Math2D.GetCells_InvertY",
                };

                window.AddAxisLines(CELLSIZE * Math.Max(NUMX, NUMY), THICKNESS);

                drawCells(Math2D.GetCells_InvertY(CELLSIZE, NUMX, NUMY), -.1, Colors.RosyBrown, Colors.Sienna);
                drawCells(Math2D.GetCells_InvertY(CELLSIZE, NUMX, NUMY, MARGIN), .1, Colors.CadetBlue, Colors.DarkSlateGray);

                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Tiling3D_Click(object sender, RoutedEventArgs e)
        {
            const double THICKNESS = .025;
            const double CELLSIZE = 5;
            const double MARGIN = .75;
            const int NUMX = 4;
            const int NUMY = 5;
            const int NUMZ = 3;

            try
            {
                Debug3DWindow window = null;

                Action<(Rect3D rect, Point3D center)[], Color, Color> drawCells = (c, c1, c2) =>
                {
                    int index = 0;
                    foreach (var cell in c)
                    {
                        Point3D p1 = cell.rect.Location;
                        Point3D p2 = cell.rect.Location + cell.rect.Size.ToVector();

                        window.AddLines(
                            UtilityWPF.GetCubeLines(p1, p2),
                            THICKNESS,
                            c2);

                        window.AddDot(cell.center, THICKNESS * 4, c2);

                        window.AddText3D(index.ToString(), cell.center, new Vector3D(0, 0, 1), cell.rect.SizeY / 3, c2, true);

                        window.AddMesh(UtilityWPF.GetCube_IndependentFaces(p1, p2), c1);

                        index++;
                    }
                };



                window = new Debug3DWindow()
                {
                    Title = "no margin",
                };

                window.AddAxisLines(CELLSIZE * Math1D.Max(NUMX, NUMY, NUMZ), THICKNESS);

                drawCells(Math3D.GetCells(CELLSIZE, NUMX, NUMY, NUMZ), UtilityWPF.ColorFromHex("20FFFFFF"), UtilityWPF.ColorFromHex("606060"));

                window.Show();





                window = new Debug3DWindow()
                {
                    Title = "margin",
                };

                window.AddAxisLines(CELLSIZE * Math1D.Max(NUMX, NUMY, NUMZ), THICKNESS);

                drawCells(Math3D.GetCells(CELLSIZE, NUMX, NUMY, NUMZ, MARGIN), UtilityWPF.ColorFromHex("20FFFFFF"), UtilityWPF.ColorFromHex("606060"));

                window.Show();


            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Tiling3D_cube_Click(object sender, RoutedEventArgs e)
        {
            const double THICKNESS = .025;
            const double CELLSIZE = 5;
            const double MARGIN = .75;
            int COUNT = 275;// StaticRandom.Next(70);

            try
            {

                Debug3DWindow window = null;

                Action<(Rect3D rect, Point3D center)[], Color, Color> drawCells = (c, c1, c2) =>
                {
                    int index = 0;
                    foreach (var cell in c)
                    {
                        Point3D p1 = cell.rect.Location;
                        Point3D p2 = cell.rect.Location + cell.rect.Size.ToVector();

                        window.AddLines(
                            UtilityWPF.GetCubeLines(p1, p2),
                            THICKNESS,
                            c2);

                        window.AddDot(cell.center, THICKNESS * 4, c2);

                        window.AddText3D(index.ToString(), cell.center, new Vector3D(0, 0, 1), cell.rect.SizeY / 3, c2, true);

                        window.AddMesh(UtilityWPF.GetCube_IndependentFaces(p1, p2), c1);

                        index++;
                    }
                };



                window = new Debug3DWindow()
                {
                    Title = "no margin: " + COUNT.ToString(),
                };

                window.AddAxisLines(CELLSIZE * (Math.Pow(COUNT, 1d / 3d) + 2), THICKNESS);

                drawCells(Math3D.GetCells_Cube(CELLSIZE, COUNT).Take(COUNT).ToArray(), UtilityWPF.ColorFromHex("20FFFFFF"), UtilityWPF.ColorFromHex("606060"));

                window.Show();





                //window = new Debug3DWindow()
                //{
                //    Title = "margin: " + COUNT.ToString(),
                //};

                //window.AddAxisLines(CELLSIZE * (Math.Pow(COUNT, 1d / 3d) + 2), THICKNESS);

                //drawCells(Math3D.GetCells_Cube(CELLSIZE, COUNT, MARGIN).Take(COUNT).ToArray(), UtilityWPF.ColorFromHex("20FFFFFF"), UtilityWPF.ColorFromHex("606060"));

                //window.Show();




            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private void EndTraining()
        {
            if (_trainingSession != null)
            {
                _trainingSession.Dispose();
                _trainingSession = null;
            }

            if (_bot != null)
            {
                _map.RemoveItem(_bot);
                _bot = null;
            }

            if (_viewer != null)
            {
                _viewer.Close();
            }
        }

        private static MutateUtility.NeuronMutateArgs GetMutateArgs(ItemOptionsArco options)
        {
            MutateUtility.NeuronMutateArgs retVal = null;

            MutateUtility.MuateArgs neuronMovement = new MutateUtility.MuateArgs(false, options.Neuron_PercentToMutate, null, null,
                new MutateUtility.MuateFactorArgs(MutateUtility.FactorType.Distance, options.Neuron_MovementAmount));       // neurons are all point3D (positions need to drift around freely.  percent doesn't make much sense)

            MutateUtility.MuateArgs linkMovement = new MutateUtility.MuateArgs(false, options.Link_PercentToMutate,
                new[]
                {
                    Tuple.Create("FromContainerPosition", new MutateUtility.MuateFactorArgs(MutateUtility.FactorType.Distance, options.LinkContainer_MovementAmount)),
                    Tuple.Create("FromContainerOrientation", new MutateUtility.MuateFactorArgs(MutateUtility.FactorType.Percent, options.LinkContainer_RotateAmount))
                },
                new[]
                {
                    Tuple.Create(PropsByPercent.DataType.Double, new MutateUtility.MuateFactorArgs(MutateUtility.FactorType.Distance, options.Link_WeightAmount)),		// all the doubles are weights, which need to be able to cross over zero (percents can't go + to -)
                    Tuple.Create(PropsByPercent.DataType.Point3D, new MutateUtility.MuateFactorArgs(MutateUtility.FactorType.Distance, options.Link_MovementAmount)),       // using a larger value for the links
                },
                null);

            retVal = new MutateUtility.NeuronMutateArgs(neuronMovement, null, linkMovement, null);

            return retVal;
        }

        private static ShipDNA GetBotDNA(string name = "arcbot neat test")
        {
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

            return ShipDNA.Create(name, parts);
        }

        private static UIElement DrawSnapshot(BotTrackingRun snapshot, int size)
        {
            if (snapshot.Log.Length < 2)
            {
                return null;
            }

            TransformGroup transform = new TransformGroup();
            transform.Children.Add(new TranslateTransform(-snapshot.RoomMin.X, -snapshot.RoomMin.Y));
            transform.Children.Add(new ScaleTransform(size / (snapshot.RoomMax.X - snapshot.RoomMin.X), size / (snapshot.RoomMax.Y - snapshot.RoomMin.Y)));

            DrawingGroup group = new DrawingGroup();

            // This rectangle forces the drawgroup to be the full size.  Otherwise it stretches the line.  There's probably a cleaner way
            // but after too much trial/error/reading, this works, so good enough, moving on
            group.Children.Add(new GeometryDrawing() { Brush = Brushes.Transparent, Geometry = new RectangleGeometry(new Rect(0, 0, size, size)) });

            for (int cntr = 0; cntr < snapshot.Log.Length - 1; cntr++)
            {
                double percentZ = Math.Abs(snapshot.Log[cntr + 1].Position.Z - snapshot.RoomCenter.Z) / (snapshot.RoomMax.Z - snapshot.RoomCenter.Z);

                Pen pen = new Pen()
                {
                    Thickness = size / 50,
                    DashCap = PenLineCap.Flat,
                    StartLineCap = PenLineCap.Round,
                    EndLineCap = PenLineCap.Round,
                    LineJoin = PenLineJoin.Round,
                    MiterLimit = 10,
                    //Brush = Brushes.Black,      //TODO: do a blue/red based on speed at each point
                    Brush = new SolidColorBrush(UtilityWPF.AlphaBlend(Colors.Red, Colors.Black, percentZ)),
                };

                GeometryDrawing drawing = new GeometryDrawing()
                {
                    Pen = pen,
                    Geometry = new LineGeometry(transform.Transform(snapshot.Log[cntr].Position.ToPoint2D()), transform.Transform(snapshot.Log[cntr + 1].Position.ToPoint2D())),
                };

                group.Children.Add(drawing);
            }

            return new Image()
            {
                Source = new DrawingImage(group),

                Width = size,
                Height = size,
                Stretch = Stretch.None,
            };
        }

        #endregion
        #region Private Methods - tests

        private static (Point3D inner, Point3D mid, Point3D outer) GetCirclePoints1(Point pointOnCircle, double radius, double thickness)
        {
            Vector3D asVect = pointOnCircle.ToVector3D();

            double half = thickness / 2;
            double innerRadius = Math.Max(0, radius - half);
            double outerRadius = radius + half;

            return
            (
                (asVect * innerRadius).ToPoint(),
                (asVect * radius).ToPoint(),
                (asVect * outerRadius).ToPoint()
            );
        }
        private static (Point3D inner, Point3D mid, Point3D outer) GetCirclePoints2(Point pointOnCircle, double circleRadius, double thickness, double sphereRadius)
        {
            Vector asVect = pointOnCircle.ToVector();

            double half = thickness / 2;
            double innerRadius = Math.Max(0, circleRadius - half);
            double outerRadius = circleRadius + half;

            Vector inner = asVect * innerRadius;
            Vector mid = asVect * circleRadius;
            Vector outer = asVect * outerRadius;

            return
            (
                Math3D.ProjectPointOntoSphere(inner.X, inner.Y, sphereRadius),
                Math3D.ProjectPointOntoSphere(mid.X, mid.Y, sphereRadius),
                Math3D.ProjectPointOntoSphere(outer.X, outer.Y, sphereRadius)
            );
        }

        private static (Point3D fromLeft, Point3D fromRight, Point3D toLeft, Point3D toRight, Point3D baseLeft, Point3D baseRight) GetLinePoints1(Vector from, Vector to, double shaftThickness, double arrowThickness)
        {
            Vector3D direction = (to - from).ToVector3D().ToUnit();

            Vector3D axis = new Vector3D(0, 0, 1);

            Vector3D left = direction.GetRotatedVector(axis, 90);
            Vector3D right = direction.GetRotatedVector(axis, -90);

            Point3D fromPoint = from.ToPoint3D();
            Point3D toPoint = to.ToPoint3D();

            double halfShaft = shaftThickness / 2;
            double halfArrow = arrowThickness / 2;

            return
            (
                fromPoint + (left * halfShaft),
                fromPoint + (right * halfShaft),
                toPoint + (left * halfShaft),
                toPoint + (right * halfShaft),
                toPoint + (left * halfArrow),
                toPoint + (right * halfArrow)
            );
        }
        private static (Point3D fromLeft, Point3D fromRight, Point3D toLeft, Point3D toRight, Point3D baseLeft, Point3D baseRight) GetLinePoints2(Vector from, Vector to, double shaftThickness, double arrowThickness, double sphereRadius)
        {
            Vector3D direction = (to - from).ToVector3D().ToUnit();

            Vector3D axis = new Vector3D(0, 0, 1);

            Vector left = direction.GetRotatedVector(axis, 90).ToVector2D();
            Vector right = direction.GetRotatedVector(axis, -90).ToVector2D();

            Point fromPoint = from.ToPoint();
            Point toPoint = to.ToPoint();

            double halfShaft = shaftThickness / 2;
            double halfArrow = arrowThickness / 2;

            Point fromLeft = fromPoint + (left * halfShaft);
            Point fromRight = fromPoint + (right * halfShaft);
            Point toLeft = toPoint + (left * halfShaft);
            Point toRight = toPoint + (right * halfShaft);
            Point baseLeft = toPoint + (left * halfArrow);
            Point baseRight = toPoint + (right * halfArrow);

            return
            (
                Math3D.ProjectPointOntoSphere(fromLeft.X, fromLeft.Y, sphereRadius),
                Math3D.ProjectPointOntoSphere(fromRight.X, fromRight.Y, sphereRadius),
                Math3D.ProjectPointOntoSphere(toLeft.X, toLeft.Y, sphereRadius),
                Math3D.ProjectPointOntoSphere(toRight.X, toRight.Y, sphereRadius),
                Math3D.ProjectPointOntoSphere(baseLeft.X, baseLeft.Y, sphereRadius),
                Math3D.ProjectPointOntoSphere(baseRight.X, baseRight.Y, sphereRadius)
            );
        }

        #endregion
    }
}
