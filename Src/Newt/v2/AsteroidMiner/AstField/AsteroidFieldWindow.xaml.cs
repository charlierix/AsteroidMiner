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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.HelperClassesWPF.Controls3D;
using Game.Newt.v2.AsteroidMiner.MapParts;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.MapParts;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.AsteroidMiner.AstField
{
    public partial class AsteroidFieldWindow : Window
    {
        #region Declaration Section

        private const bool SHOWGRAVITYLINES = false;
        private const bool SHOWGRAVITYMASSES = false;

        private Point3D _boundryMin;
        private Point3D _boundryMax;

        private OptionsPanel _optionsPanel = new OptionsPanel();

        private World _world = null;
        private Map _map = null;
        private UpdateManager _updateManager = null;
        private GravityFieldSpace _gravityField = null;

        private MaterialManager _materialManager = null;
        private int _material_Asteroid = -1;
        private int _material_Mineral = -1;
        private int _material_SpaceStation = -1;

        private ScreenSpaceLines3D _boundryLines = null;
        private List<ModelVisual3D> _stars = new List<ModelVisual3D>();

        // Reuse the same mesh for all the objects (I assume that's a good optimization)
        private SharedVisuals _sharedVisuals = new SharedVisuals(1.1);

        /// <summary>
        /// This listens to the mouse/keyboard and controls the camera
        /// </summary>
        /// <remarks>
        /// I'll only create this when debuging
        /// </remarks>
        private TrackBallRoam _trackball = null;

        private DateTime _debugGravityVisualsLastBuild = DateTime.UtcNow;
        private List<Visual3D> _debugGravityVisuals = null;

        #endregion

        #region Constructor

        public AsteroidFieldWindow()
        {
            InitializeComponent();

            _optionsPanel.OKClicked += new EventHandler(OptionsPanel_OKClicked);
        }

        #endregion

        #region Event Listeners

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                //_camera.Position = new Point3D(0, 0, -1000);
                //_camera.LookDirection = new Vector3D(0, 0, 1);
                //_camera.UpDirection = new Vector3D(0, 1, 0);

                #region Init World

                // Set the size of the world to something a bit random (gets boring when it's always the same size)
                double halfSize = 325 + StaticRandom.Next(500);
                halfSize *= 1d + (StaticRandom.NextDouble() * 5d);
                _boundryMin = new Point3D(-halfSize, -halfSize, halfSize * -.25);
                _boundryMax = new Point3D(halfSize, halfSize, halfSize * .25);

                _world = new World();
                _world.Updating += new EventHandler<WorldUpdatingArgs>(World_Updating);

                var boundryLines = _world.SetCollisionBoundry(_boundryMin, _boundryMax);

                // Draw the lines
                _boundryLines = new ScreenSpaceLines3D(true);
                _boundryLines.Thickness = 1d;
                _boundryLines.Color = WorldColors.BoundryLines;
                _viewport.Children.Add(_boundryLines);

                foreach (var line in boundryLines.innerLines)
                {
                    _boundryLines.AddLine(line.from, line.to);
                }

                #endregion
                #region Materials

                _materialManager = new MaterialManager(_world);

                // Asteroid
                Game.Newt.v2.NewtonDynamics.Material material = new Game.Newt.v2.NewtonDynamics.Material();
                material.Elasticity = .25d;
                material.StaticFriction = .9d;
                material.KineticFriction = .75d;
                _material_Asteroid = _materialManager.AddMaterial(material);

                // Mineral
                material = new Game.Newt.v2.NewtonDynamics.Material();
                material.Elasticity = .5d;
                material.StaticFriction = .9d;
                material.KineticFriction = .4d;
                _material_Mineral = _materialManager.AddMaterial(material);

                // Space Station (force field)
                material = new Game.Newt.v2.NewtonDynamics.Material();
                material.Elasticity = .99d;
                material.StaticFriction = .02d;
                material.KineticFriction = .01d;
                _material_SpaceStation = _materialManager.AddMaterial(material);

                _materialManager.RegisterCollisionEvent(_material_SpaceStation, _material_Asteroid, Collision_SpaceStation);
                _materialManager.RegisterCollisionEvent(_material_SpaceStation, _material_Mineral, Collision_SpaceStation);

                #endregion
                #region Trackball

                // Trackball
                _trackball = new TrackBallRoam(_camera);
                _trackball.KeyPanScale = 15d;
                _trackball.EventSource = grdViewPort;		//NOTE:  If this control doesn't have a background color set, the trackball won't see events (I think transparent is ok, just not null)
                _trackball.AllowZoomOnMouseWheel = true;
                _trackball.Mappings.AddRange(TrackBallMapping.GetPrebuilt(TrackBallMapping.PrebuiltMapping.MouseComplete_NoLeft_RightRotateInPlace));
                _trackball.Mappings.AddRange(TrackBallMapping.GetPrebuilt(TrackBallMapping.PrebuiltMapping.Keyboard_ASDW_In));
                //_trackball.GetOrbitRadius += new GetOrbitRadiusHandler(Trackball_GetOrbitRadius);
                _trackball.ShouldHitTestOnOrbit = true;

                #endregion
                #region Map

                _map = new Map(_viewport, null, _world);
                _map.SnapshotFequency_Milliseconds = 125;
                _map.SnapshotMaxItemsPerNode = 10;
                _map.ShouldBuildSnapshots = true;
                _map.ShouldShowSnapshotLines = _optionsPanel.OctreeShowLines;
                _map.ShouldSnapshotCentersDrift = _optionsPanel.OctreeCentersDrift;

                #endregion
                #region UpdateManager

                _updateManager = new UpdateManager(
                    new Type[] { typeof(SpaceStation) },
                    new Type[0],
                    _map);

                #endregion
                CreateFields();

                //TODO:  Add these during a timer to minimize the load time
                if (_optionsPanel.ShowStars)
                {
                    CreateStars();
                }
                CreateAsteroids();
                CreateMinerals();
                CreateSpaceStations();		// creating these last so stuff shows up behind them

                #region Start Position

                #region Farthest Station

                //// The center is too chaotic, so choose the farthest out space station
                //Point3D farthestStationPoint = new Point3D(0, 0, 0);
                //double farthestStationDistance = 0d;
                //foreach (Point3D stationPosition in _map.GetAllObjects().Where(o => o is SpaceStation).Select(o => o.PositionWorld))
                //{
                //    double distance = stationPosition.ToVector().LengthSquared;
                //    if (distance > farthestStationDistance)
                //    {
                //        farthestStationPoint = stationPosition;
                //        farthestStationDistance = distance;
                //    }
                //}

                //// Set the camera near there
                //_camera.Position = farthestStationPoint + (farthestStationPoint.ToVector().ToUnit() * 75d) + Math3D.GetRandomVectorSphericalShell(25d);
                //_camera.LookDirection = _camera.Position.ToVector() * -1d;
                //_camera.UpDirection = new Vector3D(0, 0, 1);

                #endregion
                #region Random Station

                List<Point3D> stationPoints = _map.GetAllItems().Where(o => o is SpaceStation).Select(o => o.PositionWorld).ToList();
                Point3D stationPoint = stationPoints[StaticRandom.Next(stationPoints.Count)];

                // Set the camera near there
                _camera.Position = stationPoint + (stationPoint.ToVector().ToUnit() * 75d) + Math3D.GetRandomVector_Spherical_Shell(25d);
                _camera.LookDirection = _camera.Position.ToVector() * -1d;
                _camera.UpDirection = new Vector3D(0, 0, 1);

                #endregion

                #endregion

                //TODO:  kuler for dialogs

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
            _updateManager.Update_MainThread(e.ElapsedTime);

            if ((SHOWGRAVITYLINES || SHOWGRAVITYMASSES) && (DateTime.UtcNow - _debugGravityVisualsLastBuild).TotalMilliseconds > 150d)
            {
                #region Show gravity lines

                // Clear existing visuals
                if (_debugGravityVisuals == null)
                {
                    _debugGravityVisuals = new List<Visual3D>();
                }

                foreach (Visual3D model in _debugGravityVisuals)
                {
                    _viewport.Children.Remove(model);
                }
                _debugGravityVisuals.Clear();

                if (SHOWGRAVITYLINES)
                {
                    #region Lines

                    ScreenSpaceLines3D line = new ScreenSpaceLines3D(true);
                    line.Color = UtilityWPF.ColorFromHex("80B8DE2F");
                    line.Thickness = 1d;

                    double multiplier = .01d;

                    foreach (var field in _gravityField.GetLeaves())
                    {
                        line.AddLine(field.Position, field.Position + (field.Force * multiplier));
                    }

                    _debugGravityVisuals.Add(line);
                    _viewport.Children.Add(line);

                    #endregion
                }

                if (SHOWGRAVITYMASSES)
                {
                    #region Boxes

                    Model3DGroup geometries = new Model3DGroup();

                    double multiplier = .5d;
                    double oneThird = 1d / 3d;

                    foreach (var field in _gravityField.GetLeaves())
                    {
                        MaterialGroup material = new MaterialGroup();
                        material.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("40CF835B"))));
                        material.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("A8FFA170")), 50d));

                        GeometryModel3D geometry = new GeometryModel3D();
                        geometry.Material = material;
                        geometry.BackMaterial = material;

                        double length = Math.Pow(field.Mass, oneThird) * multiplier;
                        length *= .5d;

                        geometry.Geometry = UtilityWPF.GetCube_IndependentFaces(new Point3D(field.Position.X - length, field.Position.Y - length, field.Position.Z - length), new Point3D(field.Position.X + length, field.Position.Y + length, field.Position.Z + length));

                        geometries.Children.Add(geometry);
                    }

                    ModelVisual3D model = new ModelVisual3D();
                    model.Content = geometries;

                    _debugGravityVisuals.Add(model);
                    _viewport.Children.Add(model);

                    #endregion
                }

                _debugGravityVisualsLastBuild = DateTime.UtcNow;

                #endregion
            }
        }
        private void Asteroid_ApplyForceAndTorque(object sender, BodyApplyForceAndTorqueArgs e)
        {
            e.Body.AddForce(_gravityField.GetForce(e.Body.Position));
        }
        private void Mineral_ApplyForceAndTorque(object sender, BodyApplyForceAndTorqueArgs e)
        {
            const double POW = 1d / 2.5d;

            Vector3D force = _gravityField.GetForce(e.Body.Position);

            double origLength = force.Length;
            if (Math1D.IsNearZero(origLength))
            {
                return;
            }

            //double newLength = Math.Sqrt(origLength);
            double newLength = Math.Pow(origLength, POW);
            double multiplier = newLength / origLength;

            e.Body.AddForce(new Vector3D(force.X * multiplier, force.Y * multiplier, force.Z * multiplier));
        }

        private void Collision_SpaceStation(object sender, MaterialCollisionArgs e)
        {
            SpaceStation station = _map.GetItem<SpaceStation>(e.GetBody(_material_SpaceStation));
            if (station != null)
            {
                station.Collided(e);
            }
        }

        private void btnOptions_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _world.Pause();

                darkPlate.Visibility = Visibility.Visible;

                dialogContainer.Content = _optionsPanel;
                dialogContainer.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void OptionsPanel_OKClicked(object sender, EventArgs e)
        {
            try
            {
                // Hide the dialog
                dialogContainer.Visibility = Visibility.Collapsed;
                dialogContainer.Content = null;

                darkPlate.Visibility = Visibility.Collapsed;

                // Resume the world
                _world.UnPause();

                // Transfer settings from the options
                _map.ShouldShowSnapshotLines = _optionsPanel.OctreeShowLines;
                _map.ShouldSnapshotCentersDrift = _optionsPanel.OctreeCentersDrift;

                #region Stars

                if (_stars.Count > 0 && !_optionsPanel.ShowStars)
                {
                    foreach (Visual3D star in _stars)
                    {
                        _viewport.Children.Remove(star);
                    }

                    _stars.Clear();
                }
                else if (_stars.Count == 0 && _optionsPanel.ShowStars)
                {
                    CreateStars();
                }

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private void CreateFields()
        {
            _gravityField = new GravityFieldSpace(_map);

            // These two can't deviate too far from each other.  Too much gravity with no swirl will just make a big blob.  Too much swirl with no gravity
            // isn't as bad, but more gravity makes it more interesting
            double gravitationalConstant = UtilityCore.GetScaledValue(.0001d, .0005d, 0d, 1d, StaticRandom.NextDouble());
            double swirlStrength = UtilityCore.GetScaledValue(50d, 600d, 0d, 1d, StaticRandom.NextDouble());

            double percent = UtilityCore.GetScaledValue(.1d, 1.5d, 0d, 1d, StaticRandom.NextDouble());

            gravitationalConstant *= percent;
            swirlStrength *= percent;

            _gravityField.GravitationalConstant = gravitationalConstant;

            _gravityField.Swirl = new GravityFieldSpace.SwirlField(swirlStrength, new Vector3D(0, 0, 1), 10d);
            _gravityField.Boundry = new GravityFieldSpace.BoundryField(.85d, 5000d, 2d, _boundryMin, _boundryMax);
        }

        private void CreateStars()
        {
            const double DENSITY = 1000d / (800d * 800d);
            const double STARSIZE = 1d / 1500d;

            double innerRadius = Math1D.Max(Math.Abs(_boundryMax.X), Math.Abs(_boundryMax.Y), Math.Abs(_boundryMax.Z));
            innerRadius *= 2;
            double outerRadius = innerRadius * 2d;

            double ratio;
            switch (_optionsPanel.NumStartingObjects)
            {
                case NumberOfStartingObjects.VeryFew:
                    ratio = .1d;
                    break;

                case NumberOfStartingObjects.Few:
                    ratio = .5d;
                    break;

                case NumberOfStartingObjects.Normal:
                    ratio = 1d;
                    break;

                case NumberOfStartingObjects.Many:
                    ratio = 1.5d;
                    break;

                default:
                    throw new ApplicationException("Unknown NumberOfStartingObjects: " + _optionsPanel.NumStartingObjects.ToString());
            }

            int numStars = Convert.ToInt32(DENSITY * innerRadius * innerRadius * ratio);
            int maxStars = Convert.ToInt32(5000d * ratio);
            if (numStars > maxStars)
            {
                numStars = maxStars;
            }

            Random rand = StaticRandom.GetRandomForThread();

            Model3DGroup geometries = new Model3DGroup();

            for (int cntr = 0; cntr < numStars; cntr++)
            {
                #region Geometry

                // Material
                MaterialGroup materials = new MaterialGroup();
                materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(WorldColors.Star_Color)));
                materials.Children.Add(new EmissiveMaterial(new SolidColorBrush(WorldColors.Star_Emissive)));

                // Geometry Model
                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = materials;
                geometry.BackMaterial = materials;
                geometry.Geometry = _sharedVisuals.StarMesh;

                // Transform
                Transform3DGroup transform = new Transform3DGroup();

                double scale = innerRadius * STARSIZE;
                if (rand.NextDouble() > .95d)
                {
                    scale *= 1d + (rand.NextDouble() * 1.5);
                }
                transform.Children.Add(new ScaleTransform3D(scale, scale, scale));		// scale needs to be first (or the prev transforms will be scaled as well)

                transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(Math3D.GetRandomRotation())));		// rotate needs to be added before translate
                transform.Children.Add(new TranslateTransform3D(Math3D.GetRandomVector_Spherical(innerRadius, outerRadius)));
                geometry.Transform = transform;

                geometries.Children.Add(geometry);

                #endregion
            }

            // Model Visual
            ModelVisual3D model = new ModelVisual3D();		// since the stars are static, only make one model.  It's way more efficient
            model.Content = geometries;

            // Add to the viewport
            _viewport.Children.Add(model);
            _stars.Add(model);
        }

        private void CreateSpaceStations()
        {
            double minDistanceBetweenStations = 200d;

            List<SpaceStation> stations = new List<SpaceStation>();

            #region Home Station

            // The center has become too violent

            //// Make one right next to the player at startup (always a distance of 10*sqrt(2), but at a random angle, just so it's not boring)
            //Vector3D homeLocation = new Vector3D(14.142, 0, 0);
            //homeLocation = homeLocation.GetRotatedVector(Math3D.GetRandomVectorSpherical(10d), Math3D.GetNearZeroValue(360d));

            //CreateSpaceStationsSprtBuild(homeLocation.ToPoint(), stations);

            #endregion

            // Make a few more
            for (int cntr = 0; cntr < 5; cntr++)
            {
                // Come up with a position that isn't too close to any other station
                Vector3D position = new Vector3D();
                while (true)
                {
                    #region Figure out position

                    position = Math3D.GetRandomVector(_boundryMin.ToVector() * .7d, _boundryMax.ToVector() * .7d);

                    // See if this is too close to an existing station
                    bool isTooClose = false;
                    foreach (SpaceStation station in stations)
                    {
                        Vector3D offset = position - station.PositionWorld.ToVector();
                        if (offset.Length < minDistanceBetweenStations)
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

                CreateSpaceStationsSprtBuild(position.ToPoint(), stations);
            }
        }
        private void CreateSpaceStationsSprtBuild(Point3D position, List<SpaceStation> allStations)
        {
            SpaceStation spaceStation = new SpaceStation(position, _world, _material_SpaceStation, Math3D.GetRandomRotation());

            spaceStation.SpinDegreesPerSecond = Math1D.GetNearZeroValue(.33, 1.1);

            //double angularSpeed = Math3D.GetNearZeroValue(.33d, .66d);
            //spaceStation.PhysicsBody.AngularVelocity = spaceStation.PhysicsBody.DirectionToWorld(new Vector3D(0, 0, angularSpeed));

            //spaceStation.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(Body_ApplyForceAndTorque);

            _map.AddItem(spaceStation);

            allStations.Add(spaceStation);
        }

        private void CreateAsteroids()
        {
            Random rand = StaticRandom.GetRandomForThread();
            double asteroidSize;

            int numSmall, numMedium, numLarge;
            switch (_optionsPanel.NumStartingObjects)
            {
                case NumberOfStartingObjects.VeryFew:
                    numSmall = 20;
                    numMedium = 6;
                    numLarge = 1;
                    break;

                case NumberOfStartingObjects.Few:
                    numSmall = 80;
                    numMedium = 25;
                    numLarge = 3;
                    break;

                case NumberOfStartingObjects.Normal:
                    numSmall = 150;
                    numMedium = 50;
                    numLarge = 7;
                    break;

                case NumberOfStartingObjects.Many:
                    numSmall = 250;
                    numMedium = 84;
                    numLarge = 12;
                    break;

                default:
                    throw new ApplicationException("Unknown NumberOfStartingObjects: " + _optionsPanel.NumStartingObjects.ToString());
            }

            //CreateAsteroidsSprtBuild(1d, new Point3D(0, 0, 0));

            // Small
            for (int cntr = 0; cntr < numSmall; cntr++)
            {
                asteroidSize = .5 + (rand.NextDouble() * 2);
                CreateAsteroids_Build(asteroidSize);
            }

            // Medium
            for (int cntr = 0; cntr < numMedium; cntr++)
            {
                asteroidSize = 2 + (rand.NextDouble() * 3);
                CreateAsteroids_Build(asteroidSize);
            }

            // Large
            for (int cntr = 0; cntr < numLarge; cntr++)
            {
                asteroidSize = 4.5 + (rand.NextDouble() * 5);
                CreateAsteroids_Build(asteroidSize);
            }

            //TODO: Make a planet class instead, and handle its gravity explicitely
            //for (int cntr = 0; cntr < 4; cntr++)
            //{
            //    if (rand.Next(8) == 0)
            //    {
            //        asteroidSize = 30 + (rand.NextDouble() * 25);
            //        CreateAsteroidsSprtBuild(asteroidSize, CreateAsteroidsSprtPosition());
            //    }
            //}
        }
        private void CreateAsteroids_Build(double radius)
        {
            var getMass = new Func<double, ITriangleIndexed[], double>((rad, tris) => 10d + (rad * 100d));      // this isn't as realistic as 4/3 pi r^3, but is more fun and stable

            Asteroid asteroid = new Asteroid(radius, getMass, new Point3D(0, 0, 0), _world, _map, _material_Asteroid);

            var posVel = GetPositionVelocity(asteroid.PhysicsBody.Mass);

            asteroid.PhysicsBody.Position = posVel.Item1;

            asteroid.PhysicsBody.AngularVelocity = Math3D.GetRandomVector_Spherical(1d);
            //asteroid.PhysicsBody.Velocity = asteroid.PhysicsBody.Position.ToVector().ToUnit() * -10d;		// makes all the asteroids come toward the origin (this is just here for testing)
            asteroid.PhysicsBody.Velocity = posVel.Item2;


            asteroid.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(Asteroid_ApplyForceAndTorque);

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
            double ratio = 1d;
            switch (_optionsPanel.NumStartingObjects)
            {
                case NumberOfStartingObjects.VeryFew:
                    ratio = .13d;
                    break;

                case NumberOfStartingObjects.Few:
                    ratio = .53d;
                    break;

                case NumberOfStartingObjects.Normal:
                    ratio = 1d;
                    break;

                case NumberOfStartingObjects.Many:
                    ratio = 1.667d;
                    break;

                default:
                    throw new ApplicationException("Unknown NumberOfStartingObjects: " + _optionsPanel.NumStartingObjects.ToString());
            }

            for (int cntr = 0; cntr < Math.Ceiling(150d * ratio); cntr++)
            {
                CreateMinerals_Build(MineralType.Ice);
            }

            for (int cntr = 0; cntr < Math.Ceiling(25d * ratio); cntr++)
            {
                CreateMinerals_Build(MineralType.Iron);
            }

            for (int cntr = 0; cntr < Math.Ceiling(20d * ratio); cntr++)
            {
                CreateMinerals_Build(MineralType.Graphite);
            }

            for (int cntr = 0; cntr < Math.Ceiling(8d * ratio); cntr++)
            {
                CreateMinerals_Build(MineralType.Gold);
            }

            for (int cntr = 0; cntr < Math.Ceiling(6d * ratio); cntr++)
            {
                CreateMinerals_Build(MineralType.Platinum);
            }

            for (int cntr = 0; cntr < Math.Ceiling(5d * ratio); cntr++)
            {
                CreateMinerals_Build(MineralType.Emerald);
            }

            for (int cntr = 0; cntr < Math.Ceiling(4d * ratio); cntr++)
            {
                CreateMinerals_Build(MineralType.Saphire);
            }

            for (int cntr = 0; cntr < Math.Ceiling(4d * ratio); cntr++)
            {
                CreateMinerals_Build(MineralType.Ruby);
            }

            for (int cntr = 0; cntr < Math.Ceiling(3d * ratio); cntr++)
            {
                CreateMinerals_Build(MineralType.Diamond);
            }

            for (int cntr = 0; cntr < Math.Ceiling(2d * ratio); cntr++)
            {
                CreateMinerals_Build(MineralType.Rixium);
            }
        }
        private void CreateMinerals_Build(MineralType mineralType)
        {
            Mineral mineral = new Mineral(mineralType, new Point3D(0, 0, 0), .0005d, _world, _material_Mineral, _sharedVisuals);

            var posVel = GetPositionVelocity(mineral.PhysicsBody.Mass);

            mineral.PhysicsBody.Position = posVel.Item1;

            mineral.PhysicsBody.AngularVelocity = Math3D.GetRandomVector_Spherical(1d);
            mineral.PhysicsBody.Velocity = posVel.Item2;

            mineral.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(Mineral_ApplyForceAndTorque);

            _map.AddItem(mineral);
        }

        private Tuple<Point3D, Vector3D> GetPositionVelocity(double mass)
        {
            //mass of ice = .46
            //mass of platinum = 10.7

            //small asteroid = 80-226
            //large asteroid = 550-916

            // Percent (more massive toward the center)
            double percent = UtilityCore.GetScaledValue_Capped(.05, .85, 0, 1000, mass);
            percent = 1d - percent;     //GetScaledValue can't handle inverted ranges, so invert outside of it
            percent = percent * percent * percent;      // it works better with nonlinear

            // Position
            Vector3D posXY = Math3D.GetRandomVector_Circular(_boundryMax.X * (percent - .03), _boundryMax.X * (percent + .03));
            double z = Math1D.GetNearZeroValue(_boundryMax.Z) * .15;

            Point3D position = new Point3D(posXY.X, posXY.Y, z);

            // Velocity
            double speed = UtilityCore.GetScaledValue_Capped(_boundryMax.X * .001, _boundryMax.X * .05, 0, 1, percent);

            Vector3D velocity = posXY.ToUnit().GetRotatedVector(new Vector3D(0, 0, 1), -90) * speed;

            return Tuple.Create(position, velocity);
        }

        #endregion
    }
}
