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
using Game.HelperClassesWPF.Controls2D;
using Game.HelperClassesWPF.Controls3D;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.MapParts;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.Testers
{
    public partial class SwarmBotTester : Window
    {
        #region class: ClusterVisuals

        private class ClusterVisuals
        {
            #region Declaration Section

            private readonly bool _showVelocity;

            private readonly Tuple<TranslateTransform3D, ScaleTransform3D, BillboardLine3D>[] _transforms;

            #endregion

            #region Constructor

            public ClusterVisuals(SwarmCluster[] clusters, bool showVelocity)
            {
                this.Clusters = clusters;
                _showVelocity = showVelocity;

                var visual = CreateVisual(clusters, showVelocity);

                this.Visual = visual.Item1;
                _transforms = visual.Item2;
            }

            #endregion

            #region Public Properties

            public readonly SwarmCluster[] Clusters;

            public readonly Visual3D Visual;

            #endregion

            #region Public Methods

            public void Update()
            {
                const double TINY = .0001;
                const double HUGE = 3000;

                for (int cntr = 0; cntr < this.Clusters.Length; cntr++)
                {
                    var info = this.Clusters[cntr].GetCurrentInfo();

                    if (info == null)
                    {
                        #region disposed

                        // The bots were disposed

                        _transforms[cntr].Item1.OffsetX = HUGE;
                        _transforms[cntr].Item1.OffsetY = HUGE;
                        _transforms[cntr].Item1.OffsetZ = HUGE;

                        _transforms[cntr].Item2.ScaleX = TINY;
                        _transforms[cntr].Item2.ScaleY = TINY;
                        _transforms[cntr].Item2.ScaleZ = TINY;

                        if (_transforms[cntr].Item3 != null)
                        {
                            _transforms[cntr].Item3.SetPoints(new Point3D(HUGE, HUGE, HUGE), new Point3D(HUGE + TINY, HUGE + TINY, HUGE + TINY), TINY);
                        }

                        #endregion
                        continue;
                    }

                    // Center
                    _transforms[cntr].Item1.OffsetX = info.Center.X;
                    _transforms[cntr].Item1.OffsetY = info.Center.Y;
                    _transforms[cntr].Item1.OffsetZ = info.Center.Z;

                    // Radius
                    _transforms[cntr].Item2.ScaleX = info.Radius;
                    _transforms[cntr].Item2.ScaleY = info.Radius;
                    _transforms[cntr].Item2.ScaleZ = info.Radius;

                    // Velocity
                    if (_transforms[cntr].Item3 != null)
                    {
                        _transforms[cntr].Item3.SetPoints(info.Center, info.Center + info.Velocity);
                    }
                }
            }

            #endregion

            #region Private Methods

            private static Tuple<Visual3D, Tuple<TranslateTransform3D, ScaleTransform3D, BillboardLine3D>[]> CreateVisual(SwarmCluster[] clusters, bool showVelocity)
            {
                Model3DGroup models = new Model3DGroup();

                // Hull Material
                MaterialGroup materials = new MaterialGroup();
                materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("10FFFFFF"))));
                materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("18FFFFFF")), 4));

                var transforms = new Tuple<TranslateTransform3D, ScaleTransform3D, BillboardLine3D>[clusters.Length];

                for (int cntr = 0; cntr < clusters.Length; cntr++)
                {
                    SwarmClusterInfo clusterInfo = clusters[cntr].GetCurrentInfo();

                    #region velocity

                    BillboardLine3D velocity = null;
                    if (showVelocity)
                    {
                        velocity = new BillboardLine3D()
                        {
                            Color = UtilityWPF.ColorFromHex("20FFFFFF"),
                            IsReflectiveColor = false,
                            Thickness = .2,
                        };

                        velocity.SetPoints(clusterInfo.Center, clusterInfo.Center + clusterInfo.Velocity);

                        models.Children.Add(velocity.Model);
                    }

                    #endregion

                    #region hull

                    // Model
                    GeometryModel3D hull = new GeometryModel3D();
                    hull.Material = materials;
                    hull.BackMaterial = materials;
                    hull.Geometry = UtilityWPF.GetSphere_Ico(1d, 2, true);

                    // Transform
                    Transform3DGroup transformGroup = new Transform3DGroup();

                    ScaleTransform3D scale = new ScaleTransform3D(clusterInfo.Radius, clusterInfo.Radius, clusterInfo.Radius);
                    transformGroup.Children.Add(scale);

                    TranslateTransform3D translate = new TranslateTransform3D(clusterInfo.Center.ToVector());
                    transformGroup.Children.Add(translate);

                    hull.Transform = transformGroup;

                    models.Children.Add(hull);

                    #endregion

                    transforms[cntr] = Tuple.Create(translate, scale, velocity);
                }

                ModelVisual3D visual = new ModelVisual3D();
                visual.Content = models;

                return Tuple.Create((Visual3D)visual, transforms);
            }

            #endregion
        }

        #endregion

        #region Declaration Section

        private const double BOUNDRYSIZE = 100;
        private const double BOUNDRYSIZEHALF = BOUNDRYSIZE * .5d;

        private const double ASTEROIDRADIUS = 5;
        private const double ASTEROIDDENSITY = 1000;

        private const double SWARMBOTRADIUS = .5;
        private const double SWARMBOTRANDSIZEPERCENT = .75d;

        private const double SUBSTROKESIZE = SWARMBOTRADIUS * 4;

        private Point3D _boundryMin;
        private Point3D _boundryMax;

        private World _world = null;
        private Map _map = null;

        private UpdateManager _updateManager = null;

        private SwarmClusters _botClusters = null;
        private System.Timers.Timer _botClusterTimer = null;

        private MaterialManager _materialManager = null;
        private int _material_Bot = -1;
        private int _material_Asteroid = -1;

        private BackgoundTiles _backgroundTiles = null;

        /// <summary>
        /// This listens to the mouse/keyboard and controls the camera
        /// </summary>
        private TrackBallRoam _trackball = null;

        // These are just cached values of simple calculations (all based on _boundryMax.X)
        private readonly double _boundryForceBegin;
        private readonly double _boundryForceBeginSquared;
        private readonly double _boundryForceEnd;
        private readonly double _boundryWidth;

        private SwarmObjectiveStrokes _strokes = null;
        private ITriangle _clickPlane = null;

        private List<Tuple<long, Visual3D>> _strokeVisuals = new List<Tuple<long, Visual3D>>();
        private ClusterVisuals _clusterVisuals = null;

        private bool _initialized = false;

        #endregion

        #region Constructor

        public SwarmBotTester()
        {
            InitializeComponent();

            _boundryForceEnd = BOUNDRYSIZEHALF;
            _boundryForceBegin = _boundryForceEnd * .85d;
            _boundryForceBeginSquared = _boundryForceBegin * _boundryForceBegin;
            _boundryWidth = _boundryForceEnd - _boundryForceBegin;

            _initialized = true;
        }

        #endregion

        #region Event Listeners

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                //_backgroundTiles = new BackgoundTiles(_sceneCanvas, _camera, (Brush)this.FindResource("color_SceneBackAlt"));

                #region Init World

                _boundryMin = new Point3D(-BOUNDRYSIZEHALF, -BOUNDRYSIZEHALF, -BOUNDRYSIZEHALF);
                _boundryMax = new Point3D(BOUNDRYSIZEHALF, BOUNDRYSIZEHALF, BOUNDRYSIZEHALF);

                _world = new World();
                _world.Updating += new EventHandler<WorldUpdatingArgs>(World_Updating);

                _world.SetCollisionBoundry(_boundryMin, _boundryMax);

                //TODO: Only draw the boundry lines if options say to

                #endregion
                #region Materials

                _materialManager = new MaterialManager(_world);

                // Bot
                var material = new Game.Newt.v2.NewtonDynamics.Material();
                _material_Bot = _materialManager.AddMaterial(material);

                // Asteroid
                material = new Game.Newt.v2.NewtonDynamics.Material();
                material.Elasticity = .1d;
                _material_Asteroid = _materialManager.AddMaterial(material);

                // Collisions
                _materialManager.RegisterCollisionEvent(_material_Bot, _material_Bot, Collision_BotBot);
                _materialManager.RegisterCollisionEvent(_material_Bot, _material_Asteroid, Collision_BotAsteroid);

                #endregion
                #region Trackball

                // Camera Trackball
                _trackball = new TrackBallRoam(_camera);
                _trackball.EventSource = grdViewPort;		//NOTE:  If this control doesn't have a background color set, the trackball won't see events (I think transparent is ok, just not null)
                _trackball.AllowZoomOnMouseWheel = true;
                _trackball.ShouldHitTestOnOrbit = true;
                _trackball.Mappings.AddRange(TrackBallMapping.GetPrebuilt(TrackBallMapping.PrebuiltMapping.MouseComplete_NoLeft));
                //_trackball.GetOrbitRadius += new GetOrbitRadiusHandler(Trackball_GetOrbitRadius);

                #endregion
                #region Map

                _map = new Map(_viewport, null, _world);
                _map.SnapshotFequency_Milliseconds = 250;// 125;
                _map.SnapshotMaxItemsPerNode = 10;
                _map.ShouldBuildSnapshots = true;
                _map.ShouldShowSnapshotLines = false;
                _map.ShouldSnapshotCentersDrift = true;
                _map.ItemAdded += Map_ItemAdded;
                _map.ItemRemoved += Map_ItemRemoved;

                #endregion
                #region Update Manager

                _updateManager = new UpdateManager(
                    new Type[] { typeof(SwarmBot1a) },
                    new Type[] { typeof(SwarmBot1a) },
                    _map);

                #endregion
                #region Strokes

                _strokes = new SwarmObjectiveStrokes(_world.WorldClock, SUBSTROKESIZE, 15);
                _strokes.PointsChanged += Strokes_PointsChanged;

                #endregion
                #region Bot Clusters

                _botClusters = new SwarmClusters(_map);

                _botClusterTimer = new System.Timers.Timer();
                _botClusterTimer.Interval = 1111;
                _botClusterTimer.AutoReset = false;       // makes sure only one tick is firing at a time
                _botClusterTimer.Elapsed += BotClusterTimer_Elapsed;
                _botClusterTimer.Start();

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

                _botClusterTimer.Stop();
                _updateManager.Dispose();

                if (_backgroundTiles != null)
                {
                    _backgroundTiles.Dispose();
                    _backgroundTiles = null;
                }

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

                _strokes.Tick();

                UpdateClusterVisuals();

                #region report thread ratios

                if (chkShowBotThreadUsage.IsChecked.Value)
                {
                    long totalMain = 0;
                    long totalAny = 0;

                    foreach (SwarmBot1a bot in _map.GetItems(typeof(SwarmBot1a), false))
                    {
                        totalMain += bot.MainThreadCounter;
                        totalAny += bot.AnyThreadCounter;
                    }

                    double ratio = totalMain == 0 ? 999d : Convert.ToDouble(totalAny) / Convert.ToDouble(totalMain);

                    lblCounter.Text = string.Format("main: {0}\r\nany: {1}\r\nratio: {2}", totalMain.ToString(), totalAny.ToString(), ratio.ToStringSignificantDigits(1));
                }
                else
                {
                    lblCounter.Text = "";
                }

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void BotClusterTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                _botClusters.Tick();

                _botClusterTimer.Start();
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
                if (e.Item.PhysicsBody != null)
                {
                    e.Item.PhysicsBody.ApplyForceAndTorque += PhysicsBody_ApplyForceAndTorque;
                }
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

                //if (e.Item is IDisposable)
                //{
                //    ((IDisposable)e.Item).Dispose();
                //}
                //else if (e.Item.PhysicsBody != null)
                //{
                //    e.Item.PhysicsBody.Dispose();
                //}
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PhysicsBody_ApplyForceAndTorque(object sender, BodyApplyForceAndTorqueArgs e)
        {
            try
            {
                #region Boundry Force

                Vector3D position = e.Body.Position.ToVector();

                //NOTE: Even though the boundry is square, this turns it into a circle (so the corners of the map are even harder
                //to get to - which is good, the map feel circular)
                if (position.LengthSquared > _boundryForceBeginSquared)
                {
                    // See how far into the boundry the item is
                    double distaceInto = position.Length - _boundryForceBegin;

                    // I want an acceleration of zero when distFromMax is 1, but an accel of 10 when it's boundryMax
                    //NOTE: _boundryMax.X is to the edge of the box.  If they are in the corner the distance will be greater, so the accel will be greater
                    double accel = UtilityCore.GetScaledValue(0, 30, 0, _boundryWidth, distaceInto);

                    // Apply a force toward the center
                    Vector3D force = position.ToUnit();
                    force *= accel * e.Body.Mass * -1d;

                    e.Body.AddForce(force);
                }

                #endregion
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
                //TODO: Tell the bots so they can increase their personal space
                //Also may want to slightly damage them


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
        private void Collision_BotAsteroid(object sender, MaterialCollisionArgs e)
        {
            try
            {
                //TODO: Damage the asteroid, slightly damage the bot
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Strokes_PointsChanged(object sender, SwarmObjectiveStrokes.PointsChangedArgs e)
        {
            try
            {
                switch (e.ChangeType)
                {
                    case SwarmObjectiveStrokes.PointChangeType.AddingPointsToStroke:
                        // Just draw the points in a simple way
                        break;

                    case SwarmObjectiveStrokes.PointChangeType.AddNewStroke:
                    case SwarmObjectiveStrokes.PointChangeType.ConvertStroke_Add:
                        #region add stroke

                        if (e.Points == null || e.Points.Length == 0)
                        {
                            throw new ArgumentException("Added stroke is empty");
                        }
                        else if (e.StrokeToken == null)
                        {
                            throw new ArgumentException("Expected token to be populated for a new stroke");
                        }

                        foreach (Visual3D visual in GetStrokeVisual(e.Points))
                        {
                            _strokeVisuals.Add(Tuple.Create(e.StrokeToken.Value, visual));
                            _viewport.Children.Add(visual);
                        }

                        #endregion
                        break;

                    case SwarmObjectiveStrokes.PointChangeType.ConvertStroke_Remove:
                    case SwarmObjectiveStrokes.PointChangeType.RemoveStroke_Clear:
                    case SwarmObjectiveStrokes.PointChangeType.RemoveStroke_Remove:
                    case SwarmObjectiveStrokes.PointChangeType.RemoveStroke_Timeout:
                        if (e.StrokeToken == null)
                        {
                            // Remove the pre stroke
                        }
                        else
                        {
                            // Remove the full stroke
                            _viewport.Children.RemoveAll(
                                _strokeVisuals.RemoveWhere(o => o.Item1 == e.StrokeToken.Value).
                                    Select(o => o.Item2));
                        }
                        break;

                    default:
                        throw new ApplicationException("Unknown SwarmObjectiveStrokes.PointChangeType: " + e.ChangeType.ToString());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void grdViewPort_MouseDown(object sender, MouseButtonEventArgs e)
        {
            const double CLICKDISTANCE = BOUNDRYSIZE / 8;

            try
            {
                if (e.ChangedButton != MouseButton.Left)
                {
                    // All the special logic in this method is for the left button
                    return;
                }

                // Fire a ray at the mouse point
                Point clickPoint2D = e.GetPosition(grdViewPort);

                RayHitTestParameters clickRay;
                List<MyHitTestResult> hits = UtilityWPF.CastRay(out clickRay, clickPoint2D, _viewport, _camera, _viewport, false);

                // Figure out where to place the point
                Point3D clickPoint3D;
                ITriangle clickPlane = null;
                if (hits != null && hits.Count > 0)
                {
                    // They clicked on something, so use that
                    clickPoint3D = hits[0].Point;
                }
                else
                {
                    //TODO: If there is a mothership, choose a point in a plane that goes through it (orth to the click ray)
                    //
                    // If there isn't, but they click near a swarm, use that plane
                    //
                    // Or if they click near something (like an asteroid)

                    double? clickDistNearSwarm = GetClickDistanceNearSwarm(clickRay);

                    double clickDist;
                    if (clickDistNearSwarm == null)
                    {
                        clickDist = Math.Max(CLICKDISTANCE, _camera.Position.ToVector().Length / 1.1);
                    }
                    else
                    {
                        clickDist = clickDistNearSwarm.Value;
                    }

                    clickPoint3D = clickRay.Origin + clickRay.Direction.ToUnit() * clickDist;
                }

                // Update the click plane
                if (clickPlane == null)
                {
                    Vector3D standard = Math3D.GetArbitraryOrhonganal(clickRay.Direction);
                    Vector3D orth = Vector3D.CrossProduct(standard, clickRay.Direction);
                    clickPlane = new Triangle(clickPoint3D, clickPoint3D + standard, clickPoint3D + orth);
                }

                // Store the point and plane
                _strokes.AddPointToStroke(clickPoint3D);
                _clickPlane = clickPlane;
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
                if (_clickPlane == null)
                {
                    return;
                }

                // Fire a ray at the mouse point
                Point clickPoint2D = e.GetPosition(grdViewPort);

                RayHitTestParameters clickRay = UtilityWPF.RayFromViewportPoint(_camera, _viewport, clickPoint2D);

                Point3D? hitPoint = Math3D.GetIntersection_Plane_Line(_clickPlane, clickRay.Origin, clickRay.Direction);
                if (hitPoint == null)
                {
                    return;
                }

                _strokes.AddPointToStroke(hitPoint.Value);
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

                _clickPlane = null;
                _strokes.StopStroke();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddAsteroid_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Point3D position = Math3D.GetRandomVector_Spherical(BOUNDRYSIZEHALF * .75d).ToPoint();

                Asteroid asteroid = new Asteroid(StaticRandom.NextPercent(ASTEROIDRADIUS, .5), GetAsteroidMass, position, _world, _map, _material_Asteroid);

                asteroid.PhysicsBody.AngularVelocity = Math3D.GetRandomVector_Spherical(1d);
                asteroid.PhysicsBody.Velocity = Math3D.GetRandomVector_Spherical(4d);

                _map.AddItem(asteroid);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void AddBot_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                const double DISTANCE = 3;

                Point3D position = _camera.Position + (_camera.LookDirection.ToUnit() * DISTANCE);
                position += Math3D.GetRandomVector_Spherical(DISTANCE / 3);

                double radius;
                if (chkRandBotSize.IsChecked.Value)
                {
                    double percent = StaticRandom.NextPow(3, isPlusMinus: true);
                    //double percent = StaticRandom.NextBool() ? 1d : -1d;      // use to test the extremes
                    percent *= SWARMBOTRANDSIZEPERCENT;

                    if (percent > 0)
                    {
                        radius = SWARMBOTRADIUS * (1d + percent);
                    }
                    else
                    {
                        radius = SWARMBOTRADIUS / (1d + Math.Abs(percent));
                    }
                }
                else
                {
                    radius = SWARMBOTRADIUS;
                }

                SwarmBot1a bot = new SwarmBot1a(radius, position, _world, _map, _strokes, _material_Bot);

                SetBotConstraints(bot);

                bot.PhysicsBody.AngularVelocity = Math3D.GetRandomVector_Spherical(3d);

                _map.AddItem(bot);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ClearAsteroids_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (Asteroid asteroid in _map.GetItems(typeof(Asteroid), false))
                {
                    _map.RemoveItem(asteroid);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ClearBots_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (SwarmBot1a bot in _map.GetItems(typeof(SwarmBot1a), false))
                {
                    _map.RemoveItem(bot);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _map.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Maxes_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                if (!_initialized || _map == null)
                {
                    return;
                }

                foreach (SwarmBot1a bot in _map.GetItems(typeof(SwarmBot1a), false))
                {
                    SetBotConstraints(bot);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RandVelLinearSame_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_initialized || _map == null)
                {
                    return;
                }

                Vector3D velocity = Math3D.GetRandomVector_Spherical(trkMaxSpeed.Value * 10);

                foreach (SwarmBot1a bot in _map.GetItems(typeof(SwarmBot1a), false))
                {
                    bot.PhysicsBody.Velocity = velocity;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void RandVelLinearDiff_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_initialized || _map == null)
                {
                    return;
                }

                foreach (SwarmBot1a bot in _map.GetItems(typeof(SwarmBot1a), false))
                {
                    bot.PhysicsBody.Velocity = Math3D.GetRandomVector_Spherical(trkMaxSpeed.Value * 10);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void StopLinear_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_initialized || _map == null)
                {
                    return;
                }

                foreach (SwarmBot1a bot in _map.GetItems(typeof(SwarmBot1a), false))
                {
                    bot.PhysicsBody.Velocity = new Vector3D(0, 0, 0);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private Visual3D GetStrokeVisual_PATH(Tuple<Point3D, Vector3D>[] points)
        {

            //TODO: Draw the first points larger, then taper off.  May want a bezier with the velocities as control points

            ScreenSpaceLines3D retVal = new ScreenSpaceLines3D();
            retVal.Color = UtilityWPF.ColorFromHex("CCC");
            retVal.Thickness = 2;

            if (points.Length == 1)
            {
                #region single point

                Point3D secondPoint = points[0].Item1;
                if (points[0].Item2.IsNearZero())
                {
                    secondPoint += Math3D.GetRandomVector_Spherical(.02);
                }
                else
                {
                    secondPoint += points[0].Item2;
                }

                retVal.AddLine(points[0].Item1, secondPoint);

                #endregion
            }
            else
            {
                for (int cntr = 0; cntr < points.Length - 1; cntr++)
                {
                    retVal.AddLine(points[cntr].Item1, points[cntr + 1].Item1);
                }
            }

            return retVal;
        }
        private Visual3D[] GetStrokeVisual_VELOCITIES(Tuple<Point3D, Vector3D>[] points)
        {
            ScreenSpaceLines3D retVal = new ScreenSpaceLines3D();
            retVal.Color = UtilityWPF.ColorFromHex("EEE");
            retVal.Thickness = 2;

            for (int cntr = 0; cntr < points.Length - 1; cntr++)
            {
                Point3D secondPoint = points[cntr].Item1;
                if (points[cntr].Item2.IsNearZero())
                {
                    secondPoint += Math3D.GetRandomVector_Spherical(.02);
                }
                else
                {
                    secondPoint += points[cntr].Item2;
                }

                retVal.AddLine(points[cntr].Item1, secondPoint);
            }

            return new Visual3D[] { retVal };
        }
        private Visual3D[] GetStrokeVisual(Tuple<Point3D, Vector3D>[] points)
        {
            Model3DGroup models = new Model3DGroup();

            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("EEE"))));
            materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("FFF")), 4));

            ScreenSpaceLines3D lines = new ScreenSpaceLines3D();
            lines.Color = UtilityWPF.ColorFromHex("EEE");
            lines.Thickness = 2;

            for (int cntr = 0; cntr < points.Length - 1; cntr++)
            {
                #region velocity line

                Point3D secondPoint = points[cntr].Item1;
                if (points[cntr].Item2.IsNearZero())
                {
                    secondPoint += Math3D.GetRandomVector_Spherical(.02);
                }
                else
                {
                    secondPoint += points[cntr].Item2 * .75;
                }

                lines.AddLine(points[cntr].Item1, secondPoint);

                #endregion
                #region dot

                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = materials;
                geometry.BackMaterial = materials;
                geometry.Geometry = UtilityWPF.GetSphere_Ico(.15, 1, true);

                geometry.Transform = new TranslateTransform3D(points[cntr].Item1.ToVector());

                models.Children.Add(geometry);

                #endregion
            }

            ModelVisual3D dotVisual = new ModelVisual3D();
            dotVisual.Content = models;

            return new Visual3D[] { lines, dotVisual };
        }

        private void UpdateClusterVisuals()
        {
            // See if they should be shown
            if (!chkShowClusters.IsChecked.Value)
            {
                if (_clusterVisuals != null)
                {
                    _viewport.Children.Remove(_clusterVisuals.Visual);
                    _clusterVisuals = null;
                }

                return;
            }

            // Get current clusters
            SwarmCluster[] clusters = _botClusters.Clusters;

            // Check for empty
            if (clusters == null || clusters.Length == 0)
            {
                if (_clusterVisuals != null)
                {
                    _viewport.Children.Remove(_clusterVisuals.Visual);
                    _clusterVisuals = null;
                }

                return;
            }

            // See if different
            if (_clusterVisuals != null && !SwarmCluster.IsSame(clusters, _clusterVisuals.Clusters))
            {
                _viewport.Children.Remove(_clusterVisuals.Visual);
                _clusterVisuals = null;
            }

            if (_clusterVisuals == null)
            {
                // Create new
                _clusterVisuals = new ClusterVisuals(clusters, false);
                _viewport.Children.Add(_clusterVisuals.Visual);
            }
            else
            {
                // Update position, size
                _clusterVisuals.Update();
            }
        }

        private void SetBotConstraints(SwarmBot1a bot)
        {
            const double SEARCHSLOPE = .66d;
            const double STROKESEARCHMULT = 2d;
            const double NEIGHBORSLOPE = .75d;
            const double ACCELSLOPE = 1.33d;
            const double ANGACCELSLOPE = .75d;
            const double ANGSPEEDSLOPE = .25d;

            double ratio = bot.Radius / SWARMBOTRADIUS;

            bot.SearchRadius = trkSearchRadius.Value * SetBotConstraints_Mult(SEARCHSLOPE, ratio);
            bot.SearchRadius_Strokes = bot.SearchRadius * STROKESEARCHMULT;
            bot.ChaseNeighborCount = (trkChaseNeighborCount.Value * SetBotConstraints_Mult(NEIGHBORSLOPE, ratio)).ToInt_Round();
            bot.MaxAccel = trkMaxAccel.Value * SetBotConstraints_Mult(ACCELSLOPE, 1 / ratio);
            bot.MaxAngularAccel = trkMaxAngAccel.Value * SetBotConstraints_Mult(ANGACCELSLOPE, 1 / ratio);
            bot.MaxSpeed = trkMaxSpeed.Value;       // not adjusting this, or the swarm will pull apart when they get up to speed
            bot.MaxAngularSpeed = trkMaxAngSpeed.Value * SetBotConstraints_Mult(ANGSPEEDSLOPE, 1 / ratio);
        }
        private static double SetBotConstraints_Mult(double slope, double value, double center = 1d)
        {
            // y=mx+b
            // y=mx + (1-m)

            double minX = center / 2d;
            double maxX = center * 2d;

            double minY = (slope * minX) + (center - slope);
            double maxY = (slope * maxX) + (center - slope);

            return UtilityCore.GetScaledValue(minY, maxY, minX, maxX, value);
        }

        private double? GetClickDistanceNearSwarm_ALLBOTS(RayHitTestParameters clickRay)
        {
            Point3D? closestPoint = null;
            double minDist = double.MaxValue;

            foreach (SwarmBot1a bot in _map.GetItems<SwarmBot1a>(false))
            {
                Point3D botPos = bot.PositionWorld;

                Point3D closest = Math3D.GetClosestPoint_Line_Point(clickRay.Origin, clickRay.Direction, botPos);
                if (Vector3D.DotProduct(closest - clickRay.Origin, clickRay.Direction) < 0)
                {
                    // It's behind the camera
                    continue;
                }

                double lenSqr = (botPos - closest).LengthSquared;

                if (lenSqr < minDist)
                {
                    minDist = lenSqr;
                    closestPoint = closest;
                }
            }

            if (closestPoint == null)
            {
                return null;
            }

            return (closestPoint.Value - clickRay.Origin).Length;
        }
        private double? GetClickDistanceNearSwarm(RayHitTestParameters clickRay)
        {
            SwarmCluster[] clusters = _botClusters.Clusters;
            if (clusters == null || clusters.Length == 0)
            {
                return null;
            }

            Point3D? closestPoint = null;
            double minDist = double.MaxValue;

            foreach (SwarmCluster cluster in clusters)
            {
                SwarmClusterInfo clusterInfo = cluster.GetCurrentInfo();

                Point3D closest = Math3D.GetClosestPoint_Line_Point(clickRay.Origin, clickRay.Direction, clusterInfo.Center);
                if (Vector3D.DotProduct(closest - clickRay.Origin, clickRay.Direction) < 0)
                {
                    // It's behind the camera
                    continue;
                }

                double lenSqr = (clusterInfo.Center - closest).LengthSquared;

                if (lenSqr < minDist)
                {
                    minDist = lenSqr;
                    closestPoint = closest;
                }
            }

            if (closestPoint == null)
            {
                return null;
            }
            else if (Math.Sqrt(minDist) > BOUNDRYSIZE / 4)
            {
                return null;
            }

            return (closestPoint.Value - clickRay.Origin).Length;
        }

        private static double GetAsteroidMass(double radius, ITriangleIndexed[] triangles)
        {
            double volume = 4d / 3d * Math.PI * radius * radius * radius;
            return ASTEROIDDENSITY * volume;
        }

        #endregion
    }
}
