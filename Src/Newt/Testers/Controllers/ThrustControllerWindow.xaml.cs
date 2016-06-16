using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using Game.HelperClassesAI;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.ShipParts;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.Testers.Controllers
{
    public partial class ThrustControllerWindow : Window
    {
        #region Enum: ThrusterTypeValues

        private enum ThrusterTypeValues
        {
            Single,
            Dual,
            Six,
            Random,
        }

        #endregion

        #region Declaration Section

        private const double BOUNDRYSIZE = 10000;
        private const double BOUNDRYSIZEHALF = BOUNDRYSIZE * .5d;

        private Point3D _boundryMin;
        private Point3D _boundryMax;

        private World _world = null;
        private Map _map = null;

        private UpdateManager _updateManager = null;

        private MaterialManager _materialManager = null;
        private int _material_Bot = -1;

        private ControlledThrustBot _bot = null;

        private ThrusterMap _idealMap = null;

        private readonly DropShadowEffect _errorEffect;

        private CancellationTokenSource _cancelCurrentBalancer = null;

        private bool _initialized = false;

        #endregion

        #region Constructor

        public ThrustControllerWindow()
        {
            InitializeComponent();

            // Error Effect
            _errorEffect = new DropShadowEffect()
            {
                Color = UtilityWPF.ColorFromHex("FF8175"),
                BlurRadius = 9,
                Direction = 0,
                ShadowDepth = 0,
                Opacity = .8,
            };

            // ThrusterType Combo
            foreach (ThrusterTypeValues value in Enum.GetValues(typeof(ThrusterTypeValues)))
            {
                cboThrusterTypes.Items.Add(value);
            }
            cboThrusterTypes.SelectedItem = ThrusterTypeValues.Random;

            _initialized = true;
        }

        #endregion

        #region Event Listeners

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
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

                // Bot
                var material = new Game.Newt.v2.NewtonDynamics.Material();
                _material_Bot = _materialManager.AddMaterial(material);

                // Collisions
                //_materialManager.RegisterCollisionEvent(_material_Bot, _material_Bot, Collision_BotBot);
                //_materialManager.RegisterCollisionEvent(_material_Bot, _material_Asteroid, Collision_BotAsteroid);

                #endregion
                #region Map

                _map = new Map(_viewport, null, _world);
                _map.SnapshotFequency_Milliseconds = 250;
                _map.SnapshotMaxItemsPerNode = 10;
                _map.ShouldBuildSnapshots = false;
                _map.ShouldShowSnapshotLines = false;
                _map.ShouldSnapshotCentersDrift = true;
                _map.ItemAdded += Map_ItemAdded;
                _map.ItemRemoved += Map_ItemRemoved;

                #endregion
                #region Update Manager

                _updateManager = new UpdateManager(
                    new Type[] { typeof(ControlledThrustBot) },
                    new Type[] { typeof(ControlledThrustBot) },
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

                if (_bot == null)
                {
                    return;
                }

                RefillContainers(_bot);

                // If close to edge, move to origin




            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void PhysicsBody_BodyMoved(object sender, EventArgs e)
        {
            try
            {
                // Set camera
                if (_bot != null)
                {
                    _camera.Position = _bot.PositionWorld + new Vector3D(0, 12, 0);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Map_ItemAdded(object sender, MapItemArgs e)
        {
            try
            {
                //if (e.Item.PhysicsBody != null)
                //{
                //    e.Item.PhysicsBody.ApplyForceAndTorque += PhysicsBody_ApplyForceAndTorque;
                //}
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Map_ItemRemoved(object sender, MapItemArgs e)
        {
            try
            {
                //if (e.Item.PhysicsBody != null)
                //{
                //    e.Item.PhysicsBody.ApplyForceAndTorque -= PhysicsBody_ApplyForceAndTorque;
                //}

                if (e.Item is IDisposable)
                {
                    ((IDisposable)e.Item).Dispose();
                }
                else if (e.Item.PhysicsBody != null)
                {
                    e.Item.PhysicsBody.Dispose();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void txtNumThrusters_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (!_initialized)
                {
                    return;
                }

                int cast;
                if (int.TryParse(txtNumThrusters.Text, out cast) && cast >= 2)
                {
                    txtNumThrusters.Effect = null;
                }
                else
                {
                    txtNumThrusters.Effect = _errorEffect;
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetBot_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int numThrusters;
                if (!int.TryParse(txtNumThrusters.Text, out numThrusters))
                {
                    MessageBox.Show("Couldn't parse the number of thrusters", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                else if (numThrusters < 2)
                {
                    MessageBox.Show("Must have at least two thrusters", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                #region DNA

                List<ShipPartDNA> parts = new List<ShipPartDNA>();

                parts.Add(new ShipPartDNA() { PartType = EnergyTank.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0, 0, 0.283735185991315), Scale = new Vector3D(2.05963659008885, 2.05963659008885, 0.301082085094996) });

                parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0, 0, -0.490401090473184), Scale = new Vector3D(1.92697281805995, 1.92697281805995, 1.1591130722673) });

                parts.Add(new ShipPartDNA() { PartType = SensorSpin.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0, 0, 0.57985483357727), Scale = new Vector3D(1, 1, 1) });

                parts.AddRange(GetRandomThrusters(numThrusters, chkRandRotateThrusters.IsChecked.Value, (ThrusterTypeValues)cboThrusterTypes.SelectedItem));

                ShipDNA dna = ShipDNA.Create(parts);
                dna.ShipLineage = Guid.NewGuid().ToString();

                #endregion

                ShipCoreArgs core = new ShipCoreArgs()
                {
                    Map = _map,
                    Material_Ship = _material_Bot,
                    World = _world,
                };

                BotConstruction_Result seed = BotConstructor.ConstructBot(dna, core);
                ControlledThrustBot bot = new ControlledThrustBot(seed);

                SetBot(bot);

                // Start looking for a solution immediately
                if (chkBalanceThrusters.IsChecked.Value)
                {
                    BalanceBot_Forward(_bot);
                }
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

        private void BalanceLinear_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_bot == null)
                {
                    MessageBox.Show("Set a bot first", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                BalanceBot_Forward(_bot);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void BalanceSpin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_bot == null)
                {
                    MessageBox.Show("Set a bot first", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                BalanceBot_Spin(_bot);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Crossover2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                debugOverlay.Content = UnitTestCrossover(2);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Crossover3_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                debugOverlay.Content = UnitTestCrossover(3);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Crossover6_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                debugOverlay.Content = UnitTestCrossover(6);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Crossover16_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                debugOverlay.Content = UnitTestCrossover(16);
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
            // Thrust balancer
            if (_cancelCurrentBalancer != null)
            {
                _cancelCurrentBalancer.Cancel();
                _cancelCurrentBalancer = null;
            }

            // Bot
            if (_bot != null)
            {
                _bot.PhysicsBody.BodyMoved -= PhysicsBody_BodyMoved;

                _map.RemoveItem(_bot);

                //_bot.Dispose();       // Map_ItemRemoved event listener already disposed it
                _bot = null;
            }
        }
        private void SetBot(ControlledThrustBot bot)
        {
            // Remove
            RemoveBot();

            RefillContainers(bot);

            // Set bot
            _map.AddItem(bot);
            _bot = bot;

            bot.PhysicsBody.BodyMoved += PhysicsBody_BodyMoved;
            PhysicsBody_BodyMoved(this, new EventArgs());
        }

        private void BalanceBot_Forward(ControlledThrustBot bot)
        {
            if (_cancelCurrentBalancer != null)
            {
                _cancelCurrentBalancer.Cancel();
                _cancelCurrentBalancer = null;
            }

            if (bot.Thrusters == null || bot.Thrusters.Length == 0)
            {
                MessageBox.Show("This bot has no thrusters", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Vector3D ideal = new Vector3D(0, 0, 1);

            _cancelCurrentBalancer = new CancellationTokenSource();

            var newBestFound = new Action<ThrusterMap>(o => bot.ForwardMap = o);

            ThrustControlUtil.DiscoverSolutionAsync(bot, ideal, null, _cancelCurrentBalancer.Token, null, newBestFound);
        }
        private void BalanceBot_Spin(ControlledThrustBot bot)
        {
            if (_cancelCurrentBalancer != null)
            {
                _cancelCurrentBalancer.Cancel();
                _cancelCurrentBalancer = null;
            }

            if (bot.Thrusters == null || bot.Thrusters.Length == 0)
            {
                MessageBox.Show("This bot has no thrusters", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Vector3D ideal = new Vector3D(1, 0, 0);

            _cancelCurrentBalancer = new CancellationTokenSource();

            var newBestFound = new Action<ThrusterMap>(o => bot.ForwardMap = o);        // using forward map, even though this is spin

            ThrustControlUtil.DiscoverSolutionAsync(bot, null, ideal, _cancelCurrentBalancer.Token, null, newBestFound);
        }

        private static void RefillContainers(ControlledThrustBot bot)
        {
            bot.ShouldLearnFromLifeEvents = false;

            if (bot.Energy != null)
            {
                bot.Energy.QuantityCurrent = bot.Energy.QuantityMax;
            }
            if (bot.Fuel != null)
            {
                bot.Fuel.QuantityCurrent = bot.Fuel.QuantityMax;
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
        }

        private static ShipPartDNA[] GetRandomThrusters(int count, bool randomOrientations, ThrusterTypeValues thrustType)
        {
            const double MINRADIUS = 1.5;
            const double MAXRADIUS = 3;
            const double MINSIZE = .5;
            const double MAXSIZE = 1.5;

            if (count < 2)
            {
                throw new ApplicationException("Must have at least two thrusters");
            }

            List<ShipPartDNA> retVal = new List<ShipPartDNA>();

            Random rand = StaticRandom.GetRandomForThread();

            double stepAngle = 360d / count;
            double rangeAngle = 120d / count;       // the sum of these pie slices should cover 2/3 of the area

            Vector3D axis = new Vector3D(0, 0, 1);
            double startAngle = rand.NextDouble(360);

            for (int cntr = 0; cntr < count; cntr++)
            {
                double angle = startAngle + (cntr * stepAngle);
                angle += rand.NextDouble(-rangeAngle, rangeAngle);

                Vector3D position = new Vector3D(rand.NextDouble(MINRADIUS, MAXRADIUS), 0, 0);
                position = position.GetRotatedVector(axis, angle);

                double size = rand.NextDouble(MINSIZE, MAXSIZE);

                //Random direction
                Quaternion orientation = Quaternion.Identity;
                if (randomOrientations)
                {
                    Vector3D standardVect = new Vector3D(0, 0, 1);
                    Vector3D rotatedVect = Math3D.GetRandomVector_Cone(axis, 90);
                    orientation = Math3D.GetRotation(standardVect, rotatedVect);
                }

                ThrusterType type;
                switch (thrustType)
                {
                    case ThrusterTypeValues.Single:
                        type = ThrusterType.One;
                        break;

                    case ThrusterTypeValues.Dual:
                        type = ThrusterType.Two;
                        break;

                    case ThrusterTypeValues.Six:
                        type = ThrusterType.Two_Two_Two;
                        break;

                    case ThrusterTypeValues.Random:
                        type = UtilityCore.GetRandomEnum<ThrusterType>(ThrusterType.Custom);
                        break;

                    default:
                        throw new ApplicationException("Unknown ThrusterTypeValues: " + thrustType.ToString());
                }

                retVal.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = orientation, Position = position.ToPoint(), Scale = new Vector3D(size, size, size), ThrusterType = type });
            }

            return retVal.ToArray();
        }

        private static UniformGrid UnitTestCrossover(int rows)
        {
            int columns = StaticRandom.Next(2, 13);

            #region build parents

            SolidColorBrush[][] parents = UtilityWPF.GetRandomColors(rows, 96, 224).
                Select(o =>
                {
                    SolidColorBrush brush = new SolidColorBrush(o);

                    return Enumerable.Range(0, columns).
                        Select(p => brush).
                        ToArray();
                }).
                ToArray();

            #endregion

            int numSlices = 1 + StaticRandom.Next(columns - 1);

            // Crossover
            SolidColorBrush[][] children = UtilityAI.Crossover(parents, numSlices);

            #region build return

            UniformGrid grid = new UniformGrid()
            {
                Columns = columns,
                Rows = rows,
            };

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    Border rect = new Border()
                    {
                        Background = children[row][col],
                        BorderBrush = Brushes.Black,
                        CornerRadius = new CornerRadius(6),
                        BorderThickness = new Thickness(1),
                        Margin = new Thickness(3),
                    };

                    if (col == 0)
                    {
                        Color color = UtilityWPF.ExtractColor(children[row][col]);

                        TextBlock text = new TextBlock()
                        {
                            Text = string.Format("{0} {1} {2}", color.R, color.G, color.B),
                            Foreground = new SolidColorBrush(UtilityWPF.OppositeColor(color)),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                        };

                        //rect.Child = text;
                    }

                    grid.Children.Add(rect);
                }
            }

            #endregion

            return grid;
        }

        #endregion
    }
}
