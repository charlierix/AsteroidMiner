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
using Game.HelperClassesWPF.Primitives3D;
using Game.Newt.v1.NewtonDynamics1;

namespace Game.Newt.v1.AsteroidMiner1.AsteroidMiner2D_153
{
    public partial class Miner2D : Window
    {
        #region Declaration Section

        private const double MAXRANDVELOCITY = 10d;
        //private const double MAXZOOM = 2d;
        //private const double MINZOOM = .5d;
        private const double MAXZOOM = 15d;		// how far they can zoom in (closer in)
        private const double MINZOOM = .025d;		// how far they can zoom out (farther out)
        private const double GRAVITATIONALCONSTANT = .5d;		// I can't use a larger value because gravity is only calculated in about 5% of the frames, so they free float then lunge toward each other

        private Vector3D _boundryMin;
        private Vector3D _boundryMax;

        //TODO:  Only one instance is needed if they are all the same color/thickness
        private List<ScreenSpaceLines3D> _lines = new List<ScreenSpaceLines3D>();

        private MaterialManager _materialManager = null;

        private Map _map = null;

        private Ship _ship = null;
        private List<SpaceStation> _spaceStations = new List<SpaceStation>();
        private List<Asteroid> _asteroids = new List<Asteroid>();
        private Visual3D _stars = null;     // these are just visual, and sit below the sand box

        private SpaceStation _currentlyOverStation = null;

        // Reuse the same mesh for all the objects (I assume that's a good optimization)
        private SharedVisuals _sharedVisuals = new SharedVisuals();

        //TODO:  Make a chase camera class to hold all this (and support spring look stuff)
        private double _cameraZoom = 1d;
        private bool _cameraAlwaysLooksUp = false;
        private double _cameraAngle = 90d;
        private Vector3D _cameraUp = new Vector3D(0, 1, 0);
        private Vector3D _cameraRight = new Vector3D(1, 0, 0);		// used to do rotations (orthogonal to look dir and up dir)
        private Vector3D _cameraPosition = new Vector3D(0, 0, 60);
        private Vector3D _cameraLookDirection = new Vector3D(0, 0, -10);

        private bool _showDebugVisuals = false;

        private bool _hasGravity = false;
        private double _gravityStrength = 0d;
        private SortedList<Body, Vector3D> _asteroidGravityForces = new SortedList<Body, Vector3D>();

        // When the options dialog says to set velocities, the velocities can't be set immediately, they have to be set in the
        // body update event.  Once the bodies have updated, I set this back to null on the next global update
        private SetVelocitiesArgs _setVelocityArgs = null;
        private bool _hasSetVelocities = false;

        #endregion

        #region Constructor

        public Miner2D()
        {
            InitializeComponent();

            #region Init World

            // Set the size of the world to something a bit random (gets boring when it's always the same size)
            double halfSize = 325 + StaticRandom.Next(500);
            _boundryMin = new Vector3D(-halfSize, -halfSize, -10);		// I need to set Z to something.  If the Z plates are too close together, then objects will get stuck and not move (constantly colliding)
            _boundryMax = new Vector3D(halfSize, halfSize, 10);

            _world.InitialiseBodies();
            _world.ShouldForce2D = true;
            _world.Gravity = 0d;

            _materialManager = new MaterialManager(_world);
            _materialManager.SetCollisionCallback(_materialManager.ShipMaterialID, _materialManager.MineralMaterialID, null, Collision_Ship_Mineral);
            //_materialManager.SetCollisionCallback(_materialManager.ShipMaterialID, _materialManager.AsteroidMaterialID, null, Collision_Ship_Asteroid);

            List<Point3D[]> innerLines, outerLines;
            _world.SetCollisionBoundry(out innerLines, out outerLines, _viewport, _boundryMin, _boundryMax);

            Color lineColor = Color.FromArgb(255, 70, 70, 70); //UtilityWPF.AlphaBlend(Colors.White, Colors.DimGray, .1d);
            foreach (Point3D[] line in innerLines)
            {
                // Need to wait until the window is loaded to call lineModel.CalculateGeometry
                ScreenSpaceLines3D lineModel = new ScreenSpaceLines3D(false);
                lineModel.Thickness = 1d;
                lineModel.Color = lineColor;
                lineModel.AddLine(line[0], line[1]);

                _viewport.Children.Add(lineModel);
                _lines.Add(lineModel);
            }

            _world.SimulationSpeed = 1d;
            //_world.UnPause();       // wait till all the objects are loaded

            #endregion

            optionsPanel.WorldSize = halfSize * 2;
            optionsPanel.CameraAlwaysLooksUp = _cameraAlwaysLooksUp;
            optionsPanel.CameraAngle = _cameraAngle;
            optionsPanel.ShowDebugVisuals = _showDebugVisuals;
        }

        #endregion

        #region Public Properties

        public bool CameraAlwaysLooksUp
        {
            get
            {
                return _cameraAlwaysLooksUp;
            }
            set
            {
                if (_ship == null)
                {
                    throw new InvalidOperationException("CameraAlwaysLooksUp can't be called until the ship is built");
                }

                _cameraAlwaysLooksUp = value;

                UpdateCameraPosition();
            }
        }
        public double CameraAngle
        {
            get
            {
                return _cameraAngle;
            }
            set
            {
                if (_ship == null)
                {
                    throw new InvalidOperationException("CameraAlwaysLooksUp can't be called until the ship is built");
                }

                _cameraAngle = value;

                UpdateCameraPosition();
            }
        }

        #endregion

        #region Event Listeners

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _map = new Map(_viewport, _viewportMap, _world);

            RecalculateLineGeometries();

            // Order is important because of semi transparency
            CreateStars();
            CreateSpaceStations();
            CreateShip();
            CreateAsteroids();
            CreateMinerals();
            //CreateMinerals_LINE();

            _world.UnPause();
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            if (_world != null)
            {
                _world.Dispose();
                _world = null;
            }
        }

        /// <summary>
        /// This is raised by _world once per frame (this is raised, then it requests forces for bodies)
        /// </summary>
        private void World_Updating(object sender, WorldUpdatingArgs e)
        {
            try
            {
                if (_hasSetVelocities && _setVelocityArgs != null)
                {
                    _hasSetVelocities = false;
                    _setVelocityArgs = null;
                }

                Vector3D shipPosition = _ship.PositionWorld.ToVector();

                _ship.WorldUpdating(e.ElapsedTime);

                #region Minerals

                foreach (Mineral mineral in _map.GetMinerals())
                {
                    mineral.WorldUpdating();
                }

                #endregion

                #region Space Stations

                SpaceStation currentlyOverStation = null;

                foreach (SpaceStation station in _spaceStations)
                {
                    station.WorldUpdating();

                    // See if the ship is over the station
                    Vector3D stationPosition = station.PositionWorld.ToVector();
                    stationPosition.Z = 0;

                    if ((stationPosition - shipPosition).LengthSquared < station.Radius * station.Radius)
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

                // Camera
                UpdateCameraPosition();

                #region Asteroid Gravity

                //TODO:  It's too expensive for every small item to examine every other object each frame, and it's choppy when the calculations are
                // intermitent (should do threads).  Set up a vector field for the minerals and bots to be attracted to
                //
                // On second thought, I don't think this will work.  It would be more efficient for the map to return objects in range, but that requires
                // the map to store things different (I think for 2D it's a quad tree, and 3D is an oct tree?)

                // Only doing asteroids - the ship does gravity calculation against the asteroids in it's apply force event
                if (_hasGravity)
                {
                    _asteroidGravityForces.Clear();

                    if (StaticRandom.NextDouble() < .05d)		// I don't want to do this every frame
                    {
                        // Init my forces list
                        for (int cntr = 0; cntr < _asteroids.Count; cntr++)
                        {
                            _asteroidGravityForces.Add(_asteroids[cntr].PhysicsBody, new Vector3D());
                        }

                        // Apply Gravity
                        for (int outerCntr = 0; outerCntr < _asteroids.Count - 1; outerCntr++)
                        {
                            Point3D centerMass1 = _asteroids[outerCntr].PositionWorld;

                            for (int innerCntr = outerCntr + 1; innerCntr < _asteroids.Count; innerCntr++)
                            {
                                #region Apply Gravity

                                Point3D centerMass2 = _asteroids[innerCntr].PositionWorld;

                                Vector3D gravityLink = centerMass1 - centerMass2;

                                double force = GRAVITATIONALCONSTANT * (_asteroids[outerCntr].Mass * _asteroids[innerCntr].Mass) / gravityLink.LengthSquared;

                                gravityLink.Normalize();
                                gravityLink *= force;

                                _asteroidGravityForces[_asteroids[innerCntr].PhysicsBody] += gravityLink;
                                _asteroidGravityForces[_asteroids[outerCntr].PhysicsBody] -= gravityLink;

                                #endregion
                            }
                        }

                        // I also need to attract to the ship, because it's attracted to the asteroids
                        // I can't because the ship is attracted every frame, so the forces are unbalanced
                        Point3D shipPos = _ship.PositionWorld;
                        double shipMass = _ship.PhysicsBody.Mass;

                        for (int cntr = 0; cntr < _asteroids.Count; cntr++)
                        {
                            #region Apply Gravity

                            Vector3D gravityLink = shipPos - _asteroids[cntr].PositionWorld;

                            double force = GRAVITATIONALCONSTANT * (_asteroids[cntr].Mass * shipMass) / gravityLink.LengthSquared;

                            gravityLink.Normalize();
                            gravityLink *= force;

                            _asteroidGravityForces[_asteroids[cntr].PhysicsBody] -= gravityLink;

                            #endregion
                        }
                    }
                }

                #endregion

                _map.WorldUpdating();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Body_ApplyForce(Body sender, BodyForceEventArgs e)
        {
            ConvexBody3D senderCast = sender as ConvexBody3D;
            if (senderCast == null)
            {
                return;
            }

            #region Boundry Force

            Vector3D positionWorld = senderCast.PositionToWorld(senderCast.CenterOfMass).ToVector();

            // Just using one property from boundry assumes the boundry is square, and max is positive
            double boundryMax = _boundryMax.X;
            double maxDistance = _boundryMax.X * .85d;
            if (positionWorld.LengthSquared > maxDistance * maxDistance)
            {
                // Get the distance from the max distance
                double distFromMax = positionWorld.Length - maxDistance;     // wait till now to do the expensive operation.

                // I want an acceleration of zero when distFromMax is 1, but an accel of 10 when it's boundryMax
                //NOTE: _boundryMax.X is to the edge of the box.  If they are in the corner the distance will be greater, so the accel will be greater
                double accel = UtilityCore.GetScaledValue(0, 30, 0, boundryMax - maxDistance, distFromMax);

                // Apply a force
                Vector3D force = positionWorld;
                force.Normalize();
                force *= accel * senderCast.Mass * -1d;
                e.AddForce(force);
            }

            #endregion

            #region Gravity

            if (_hasGravity)
            {
                //TODO:  Attract all other objects to the asteroids
                if (senderCast.MaterialGroupID == _materialManager.AsteroidMaterialID && _asteroidGravityForces.ContainsKey(senderCast))
                {
                    e.AddForce(_asteroidGravityForces[senderCast]);
                }
                else if (senderCast.MaterialGroupID == _materialManager.ShipMaterialID && _asteroidGravityForces.Keys.Count > 0)		// the asteroids only attract every few frames.  If I attract every frame, the forces are unbalanced, and the ship ends up propelling the asteroid around
                {
                    #region Ship to asteroids

                    Point3D shipPos = senderCast.PositionToWorld(senderCast.CenterOfMass);
                    double shipMass = senderCast.Mass;

                    Vector3D gravityForce = new Vector3D(0, 0, 0);

                    foreach (Asteroid asteroid in _asteroids)
                    {
                        #region Apply Gravity

                        Vector3D gravityLink = shipPos - asteroid.PositionWorld;

                        double force = GRAVITATIONALCONSTANT * (asteroid.Mass * shipMass) / gravityLink.LengthSquared;

                        gravityLink.Normalize();
                        gravityLink *= force;

                        //_asteroidGravityForces[_asteroids[innerCntr].PhysicsBody] += gravityLink;
                        gravityForce -= gravityLink;

                        #endregion
                    }

                    e.AddForce(gravityForce);

                    #endregion
                }
            }

            #endregion

            #region Set Velocities

            if (_setVelocityArgs != null)
            {
                _hasSetVelocities = true;

                // See if the velocity should be set for this type of object
                bool shouldSetVelocity = false;
                if (_setVelocityArgs.Asteroids && senderCast.MaterialGroupID == _materialManager.AsteroidMaterialID)
                {
                    shouldSetVelocity = true;
                }
                else if (_setVelocityArgs.Minerals && senderCast.MaterialGroupID == _materialManager.MineralMaterialID)
                {
                    shouldSetVelocity = true;
                }

                if (shouldSetVelocity)
                {
                    // Figure out the velocity
                    Vector3D velocity = new Vector3D(0, 0, 0);
                    Vector3D angularVelocity = new Vector3D(0, 0, 0);
                    if (_setVelocityArgs.Speed > 0d)
                    {
                        velocity = Math3D.GetRandomVector_Spherical(_setVelocityArgs.Speed);
                        angularVelocity = Math3D.GetRandomVector_Spherical(_setVelocityArgs.Speed / 10d);

                        if (_setVelocityArgs.IsRandom)
                        {
                            double halfSpeed = _setVelocityArgs.Speed / 2d;
                            if (velocity.Length < halfSpeed)
                            {
                                // It's going too slow, choose a new speed between half and full
                                velocity.Normalize();
                                double newSpeed = halfSpeed + (StaticRandom.NextDouble() * halfSpeed);
                                velocity *= newSpeed;
                            }
                        }
                        else
                        {
                            // GetRandomVectorSpherical returns random, but this should be fixed
                            velocity.Normalize();
                            velocity *= _setVelocityArgs.Speed;
                        }
                    }

                    // Set it (and clear rotation)
                    senderCast.Omega = angularVelocity;
                    senderCast.Velocity = velocity;
                }
            }

            #endregion
        }

        private void Collision_Ship_Mineral(object sender, CollisionEndEventArgs e)
        {

            // They all return 0's
            //Point3D contactPoint = e.ContactPositionWorld;
            //Vector3D contactNormal = e.ContactNormalWorld;
            //Vector3D contactForce = e.ContactForceWorld;


            Ship ship = null;
            Mineral mineral = null;
            if (e.Body1 is Ship)
            {
                ship = (Ship)e.Body1;
                mineral = (Mineral)e.Body2;
            }
            else
            {
                ship = (Ship)e.Body2;
                mineral = (Mineral)e.Body1;
            }

            if (ship.CollideWithMineral(mineral))
            {
                // The ship is taking the mineral
                e.AllowCollision = false;
                _map.RemoveItem(mineral);
            }
        }
        private void Collision_Ship_Asteroid(object sender, CollisionEndEventArgs e)
        {
            Ship ship = null;
            Asteroid asteroid = null;
            if (e.Body1 is Ship)
            {
                ship = (Ship)e.Body1;
                asteroid = (Asteroid)e.Body2;
            }
            else
            {
                ship = (Ship)e.Body2;
                asteroid = (Asteroid)e.Body1;
            }

            ship.CollideWithAsteroid(asteroid, e);
        }

        private void grdViewPort_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RecalculateLineGeometries();
        }
        private void Camera_Changed(object sender, EventArgs e)
        {
            RecalculateLineGeometries();
        }

        private Point _middleDragPoint;

        private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                _middleDragPoint = e.GetPosition(this);
            }
        }
        private void Window_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                Point newDragPoint = e.GetPosition(this);

                // Dragging down makes the angle go toward zero
                double change = newDragPoint.Y - _middleDragPoint.Y;
                change *= .33;

                double angle = _cameraAngle + change;

                if (angle < -90)		// these limits need to stay in sync with the option panel's slider control
                {
                    angle = -90;
                }
                else if (angle > 90)
                {
                    angle = 90;
                }

                // This causes an event to fire
                optionsPanel.CameraAngle = angle;

                _middleDragPoint = newDragPoint;
            }
        }
        private void Window_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {

        }
        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // The farther they zoom in, the slower it goes
            //double newZoom = _cameraZoom + (e.Delta * .0001);
            double newZoom = _cameraZoom * (1 + (e.Delta * .0001));

            // The camera will be adjusted in the world update event

            if (newZoom > MAXZOOM)
            {
                _cameraZoom = MAXZOOM;
            }
            else if (newZoom < MINZOOM)
            {
                _cameraZoom = MINZOOM;
            }
            else
            {
                _cameraZoom = newZoom;
            }
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            //this.Title = "KeyDown: " + e.Key.ToString();

            if (_currentlyOverStation != null && e.Key == Key.Space)
            {
                ShowSpaceDock(_currentlyOverStation);
            }
            else
            {
                _ship.KeyDown(e.Key);
            }

            e.Handled = true;
        }
        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            //this.Title = "KeyUp: " + e.Key.ToString();

            _ship.KeyUp(e.Key);
            e.Handled = true;
        }

        private void spaceDock_LaunchShip(object sender, EventArgs e)
        {
            HideSpaceDock();
        }

        private void btnOptions_Click(object sender, RoutedEventArgs e)
        {
            ShowOptions();
        }
        private void optionsPanel_ValueChanged(object sender, EventArgs e)
        {
            this.CameraAlwaysLooksUp = optionsPanel.CameraAlwaysLooksUp;
            this.CameraAngle = optionsPanel.CameraAngle;

            _ship.ShowDebugVisuals = optionsPanel.ShowDebugVisuals;

            _hasGravity = optionsPanel.HasGravity;
            _gravityStrength = optionsPanel.GravityStrength;
        }
        private void optionsPanel_SetVelocities(object sender, SetVelocitiesArgs e)
        {
            _setVelocityArgs = e;
            _hasSetVelocities = false;
        }
        private void optionsPanel_CloseDialog(object sender, EventArgs e)
        {
            HideOptions();
        }

        #endregion

        #region Private Methods

        private void ShowSpaceDock(SpaceStation station)
        {
            PauseWorld();

            spaceDock.ShipDocking(_ship, station);

            spaceDock.Visibility = Visibility.Visible;
        }
        private void HideSpaceDock()
        {
            spaceDock.Visibility = Visibility.Collapsed;

            this.Focus();

            ResumeWorld();
        }

        private void ShowOptions()
        {
            PauseWorld();

            optionsPanel.Visibility = Visibility.Visible;
        }
        private void HideOptions()
        {
            optionsPanel.Visibility = Visibility.Collapsed;

            this.Focus();

            ResumeWorld();
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

        private void CreateShip()
        {
            if (_ship != null)
            {
                throw new ApplicationException("Ship already created");
            }

            _ship = new Ship();
            _ship.HullColor = UtilityWPF.AlphaBlend(UtilityWPF.GetRandomColor(255, 64, 192), Color.FromArgb(255, 64, 64, 64), .5d);
            _ship.ShowDebugVisuals = _showDebugVisuals;
            _ship.CreateShip(_materialManager, _sharedVisuals, _map, progressBarCargo, progressBarFuel);

            _ship.PhysicsBody.ApplyForce += new BodyForceEventHandler(Body_ApplyForce);
        }

        private void CreateSpaceStations()
        {
            double xyCoord = _boundryMax.X * .7;
            double zCoord = _boundryMin.Z - 3;
            double minDistanceBetweenStations = 200d;

            #region Home Station

            // Make one right next to the player at startup (always a distance of 10*sqrt(2), but at a random angle, just so it's not boring)
            Vector3D homeLocation = new Vector3D(14.142, 0, 0);
            homeLocation = homeLocation.GetRotatedVector(new Vector3D(0, 0, 1), Math3D.GetNearZeroValue(360d));

            CreateSpaceStationsSprtBuild(new Point3D(homeLocation.X, homeLocation.Y, zCoord));

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
                    position.Z = zCoord;

                    // See if this is too close to an existing station
                    bool isTooClose = false;
                    foreach (SpaceStation station in _spaceStations)
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

                CreateSpaceStationsSprtBuild(position.ToPoint());
            }
        }
        private void CreateSpaceStationsSprtBuild(Point3D position)
        {
            SpaceStation spaceStation = new SpaceStation();
            spaceStation.SpinDegreesPerSecond = Math3D.GetNearZeroValue(10d);
            spaceStation.HullColor = UtilityWPF.AlphaBlend(UtilityWPF.GetRandomColor(108, 148), Colors.Gray, .25);
            spaceStation.CreateStation(_map, position);

            _spaceStations.Add(spaceStation);
        }

        private void CreateAsteroids()
        {
            double maxPos = _boundryMax.X * .9d;     // this form will start pushing back at .85, so some of them will be inbound  :)
            double asteroidSize;

            Random rand = StaticRandom.GetRandomForThread();

            // Small
            for (int cntr = 0; cntr < 65; cntr++)
            {
                asteroidSize = .5 + (rand.NextDouble() * 2);
                CreateAsteroidsSprtBuild(asteroidSize, CreateAsteroidsSprtPosition(maxPos));
            }

            // Medium
            for (int cntr = 0; cntr < 20; cntr++)
            {
                asteroidSize = 2 + (rand.NextDouble() * 3);
                CreateAsteroidsSprtBuild(asteroidSize, CreateAsteroidsSprtPosition(maxPos));
            }

            // Large
            for (int cntr = 0; cntr < 3; cntr++)
            {
                asteroidSize = 4.5 + (rand.NextDouble() * 5);
                CreateAsteroidsSprtBuild(asteroidSize, CreateAsteroidsSprtPosition(maxPos));
            }
        }
        private Vector3D CreateAsteroidsSprtPosition(double maxPos)
        {
            Vector3D retVal;

            while (true)
            {
                retVal = Math3D.GetRandomVector_Circular(maxPos);

                if (retVal.Length > 50)
                {
                    break;  // not sure how to do a do until loop
                }
            }

            return retVal;
        }
        private void CreateAsteroidsSprtBuild(double radius, Vector3D position)
        {
            Asteroid asteroid = new Asteroid();
            asteroid.Radius = radius;
            asteroid.Mass = 10 + (radius * 100d);
            asteroid.InitialVelocity = Math3D.GetRandomVector_Circular(6);
            asteroid.CreateAsteroid(_materialManager, _map, _sharedVisuals, position);

            asteroid.PhysicsBody.ApplyForce += new BodyForceEventHandler(Body_ApplyForce);

            _asteroids.Add(asteroid);
        }

        private void CreateMinerals_LINE()
        {
            List<MineralType> mineralTypes = new List<MineralType>();
            mineralTypes.AddRange((MineralType[])Enum.GetValues(typeof(MineralType)));
            mineralTypes.Remove(MineralType.Custom);

            double x = -15;

            foreach (MineralType mineralType in mineralTypes)
            {
                Mineral mineral = new Mineral();
                mineral.MineralType = mineralType;

                mineral.InitialVelocity = Math3D.GetRandomVector_Circular(.25);
                mineral.CreateMineral(_materialManager, _map, _sharedVisuals, new Vector3D(x, 10, 0), true, .0005);

                mineral.PhysicsBody.ApplyForce += new BodyForceEventHandler(Body_ApplyForce);

                x += 2;
            }
        }
        private void CreateMinerals()
        {
            double maxPos = _boundryMax.X * .9d;     // this form will start pushing back at .85, so some of them will be inbound  :)

            for (int cntr = 0; cntr < 50; cntr++)
            {
                CreateMineralsSprtBuild(MineralType.Ice, CreateMineralsSprtPosition(maxPos));
            }

            for (int cntr = 0; cntr < 25; cntr++)
            {
                CreateMineralsSprtBuild(MineralType.Iron, CreateMineralsSprtPosition(maxPos));
            }

            for (int cntr = 0; cntr < 20; cntr++)
            {
                CreateMineralsSprtBuild(MineralType.Graphite, CreateMineralsSprtPosition(maxPos));
            }

            for (int cntr = 0; cntr < 8; cntr++)
            {
                CreateMineralsSprtBuild(MineralType.Gold, CreateMineralsSprtPosition(maxPos));
            }

            for (int cntr = 0; cntr < 6; cntr++)
            {
                CreateMineralsSprtBuild(MineralType.Platinum, CreateMineralsSprtPosition(maxPos));
            }

            for (int cntr = 0; cntr < 5; cntr++)
            {
                CreateMineralsSprtBuild(MineralType.Emerald, CreateMineralsSprtPosition(maxPos));
            }

            for (int cntr = 0; cntr < 4; cntr++)
            {
                CreateMineralsSprtBuild(MineralType.Saphire, CreateMineralsSprtPosition(maxPos));
            }

            for (int cntr = 0; cntr < 4; cntr++)
            {
                CreateMineralsSprtBuild(MineralType.Ruby, CreateMineralsSprtPosition(maxPos));
            }

            for (int cntr = 0; cntr < 3; cntr++)
            {
                CreateMineralsSprtBuild(MineralType.Diamond, CreateMineralsSprtPosition(maxPos));
            }

            for (int cntr = 0; cntr < 2; cntr++)
            {
                CreateMineralsSprtBuild(MineralType.Rixium, CreateMineralsSprtPosition(maxPos));
            }
        }
        private Vector3D CreateMineralsSprtPosition(double maxPos)
        {
            Vector3D retVal;

            while (true)
            {
                retVal = Math3D.GetRandomVector_Circular(maxPos);

                if (retVal.Length > 50)
                {
                    break;  // not sure how to do a do until loop
                }
            }

            return retVal;
        }
        private void CreateMineralsSprtBuild(MineralType mineralType, Vector3D position)
        {
            Mineral mineral = new Mineral();
            mineral.MineralType = mineralType;

            mineral.InitialVelocity = Math3D.GetRandomVector_Circular(8);
            mineral.CreateMineral(_materialManager, _map, _sharedVisuals, position, true, .0005);

            mineral.PhysicsBody.ApplyForce += new BodyForceEventHandler(Body_ApplyForce);
        }

        private void CreateStars_GRID()
        {
            // Material
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(Brushes.GhostWhite));
            materials.Children.Add(new SpecularMaterial(Brushes.White, 100d));

            Model3DGroup geometries = new Model3DGroup();

            for (double x = _boundryMin.X * 1.1; x < _boundryMax.X * 1.1; x += 30)
            {
                for (double y = _boundryMin.Y * 1.1; y < _boundryMax.Y * 1.1; y += 30)
                {
                    for (double z = _boundryMin.Z * 1.5; z > _boundryMin.Z * 10; z -= 30)
                    {
                        // Geometry Model
                        GeometryModel3D geometry = new GeometryModel3D();
                        geometry.Material = materials;
                        geometry.BackMaterial = materials;
                        geometry.Geometry = _sharedVisuals.StarMesh;
                        geometry.Transform = new TranslateTransform3D(new Vector3D(x, y, z));

                        geometries.Children.Add(geometry);
                    }
                }
            }

            // Visual
            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = geometries;

            _viewport.Children.Add(visual);
            _stars = visual;
        }
        private void CreateStars()
        {
            // When the size is 400 (doubled is 800, with margins is 880), the number of stars that looks good is 1000.  So I want to keep that same density
            const double DENSITY = 1000d / (880d * 880d);

            Vector3D starMin = new Vector3D(_boundryMin.X * 1.1, _boundryMin.Y * 1.1, _boundryMin.Z * 10);
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
                // Geometry Model
                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = materials;
                geometry.BackMaterial = materials;
                geometry.Geometry = _sharedVisuals.StarMesh;
                geometry.Transform = new TranslateTransform3D(Math3D.GetRandomVector(starMin, starMax));

                geometries.Children.Add(geometry);
            }

            // Visual
            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = geometries;

            _viewport.Children.Add(visual);
            _stars = visual;
        }

        private void RecalculateLineGeometries()
        {
            foreach (ScreenSpaceLines3D line in _lines)
            {
                line.CalculateGeometry();
            }
        }

        private void UpdateCameraPosition()
        {
            Point3D shipPosWorld = _ship.PhysicsBody.PositionToWorld(_ship.PhysicsBody.CenterOfMass);

            _cameraMap.Position = new Point3D(shipPosWorld.X, shipPosWorld.Y, 500);

            //TODO:  Eliminate the wobble - may need to have desired properties for the camera, but diffuse that a bit over time (sounds good when I write it, but how)
            // Maybe give the camera mass, and let the change in position be some kind of spring force - however, it is spring forces that clamp the ship into 2D

            Vector3D cameraRight = _ship.PhysicsBody.DirectionToWorld(_cameraRight);
            Vector3D cameraUp = _ship.PhysicsBody.DirectionToWorld(_cameraUp);
            Vector3D cameraPos = _cameraPosition * (1d / _cameraZoom);
            Vector3D cameraDirFacing = _cameraLookDirection;

            double actualRotateDegrees = 90d - _cameraAngle;		// the camera angle is stored so 90 is looking straight down and 0 is looking straight out, but cameraPos starts looking straight down
            cameraPos = cameraPos.GetRotatedVector(cameraRight, actualRotateDegrees);
            cameraUp = cameraUp.GetRotatedVector(cameraRight, actualRotateDegrees);
            cameraDirFacing = cameraDirFacing.GetRotatedVector(cameraRight, actualRotateDegrees);

            _camera.Position = (_ship.PositionWorld.ToVector() + cameraPos).ToPoint();
            _camera.UpDirection = cameraUp;
            _camera.LookDirection = cameraDirFacing;

            if (!_cameraAlwaysLooksUp)
            {
                //TODO:  Fix this (along with position)
                //_camera.UpDirection = _ship.PhysicsBody.DirectionToWorld(_cameraUp);


                _cameraMap.UpDirection = _ship.PhysicsBody.DirectionToWorld(_cameraUp);
            }
        }

        #endregion
    }
}
