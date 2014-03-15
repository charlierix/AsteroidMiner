using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using Game.HelperClasses;
using Game.Newt.AsteroidMiner_153.ShipAddons;
using Game.Newt.HelperClasses;
using Game.Newt.NewtonDynamics_153;

namespace Game.Newt.AsteroidMiner_153
{
    public class Ship : IMapObject
    {
        #region Enum: SwarmFormation

        //TODO:  Make one that chases the mouse
        public enum SwarmFormation
        {
            /// <summary>
            /// No swarmbots
            /// </summary>
            None,
            /// <summary>
            /// All directly in front of the ship
            /// </summary>
            AllFront,
            /// <summary>
            /// All directly behind the ship
            /// </summary>
            AllRear,
            /// <summary>
            /// Triangle with the point in front
            /// </summary>
            Triangle,
            /// <summary>
            /// Triangle with the point in rear
            /// </summary>
            ReverseTriangle,
            /// <summary>
            /// Pentagon with the point in front
            /// </summary>
            Pentagon,
            /// <summary>
            /// Pentagon with the point in rear
            /// </summary>
            ReversePentagon,
            /// <summary>
            /// These are attracted to the ship
            /// </summary>
            SurroundShip
        }

        #endregion

        #region Declaration Section

        public const bool DEBUGSHOWSACCELERATION = false;

        //private bool _isQPressed = false;
        //private bool _isEPressed = false;
        //private bool _isShiftPressed = false;
        //private bool _isCtrlPressed = false;

        private SortedList<Key, List<ThrustLine>> _thrustLines = new SortedList<Key, List<ThrustLine>>();

        private Vector3D _thrusterOffset_BottomRight;
        private Vector3D _thrusterOffset_BottomLeft;
        private Vector3D _thrusterOffset_TopRight;
        private Vector3D _thrusterOffset_TopLeft;

        private double _thrustForce = 0d;
        private double _torqueballLeftRightThrusterForce = 0d;

        private CargoBay _cargoBay = null;
        private FuelTank _fuelTank = null;
        private List<List<SwarmBot2>> _swarmBots = new List<List<SwarmBot2>>();    // these are independant swarms of swarmbots
        private List<Point3D> _swarmbotChasePoints = new List<Point3D>();    // this is the position relative to the ship where the corresponding swarm should be
        private List<PointVisualizer> _swarmbotChasePointSprites = new List<PointVisualizer>();    // same size as _swarmbotChasePoints, used to help debug

        private Map _map = null;

        // The physics engine keeps this one synced, no need to transform it every frame
        private ModelVisual3D _physicsModel = null;

        private SharedVisuals _sharedVisuals = null;

        // These are progress bars on the main screen
        private ProgressBarGame _progressBarCargo = null;
        private ProgressBarGame _progressBarFuel = null;

        #endregion

        #region IMapObject Members

        private ConvexBody3D _physicsBody = null;
        public ConvexBody3D PhysicsBody
        {
            get
            {
                return _physicsBody;
            }
        }

        private List<ModelVisual3D> _visuals = new List<ModelVisual3D>();
        public IEnumerable<ModelVisual3D> Visuals3D
        {
            get
            {
                return _visuals;
            }
        }

        public Point3D PositionWorld
        {
            get
            {
                return _physicsBody.PositionToWorld(_physicsBody.CenterOfMass);
            }
        }
        public Vector3D VelocityWorld
        {
            get
            {
                //return _physicsBody.Velocity;
                return _physicsBody.VelocityCached;		// this one is safer (can be called at any time, not just within the apply force/torque event)
            }
        }

        private double _radius = 1d;      // this is the max of the x,y,z radii
        public double Radius
        {
            get
            {
                return _radius;
            }
        }

        #endregion

        #region Public Properties

        //TODO:  Put a lot of these setup properties in some kind of a DNA class

        private double _hullMass = 5d;
        public double HullMass
        {
            get
            {
                return _hullMass;
            }
        }

        private double _cargoBayMass = .25d;
        public double CargoBayMass
        {
            get
            {
                return _cargoBayMass;
            }
        }

        private double _cargoBayVolume = 4d;
        public double CargoBayVolume
        {
            get
            {
                return _cargoBayVolume;
            }
        }

        private double _fuelTankMass = .25d;
        public double FuelTankMass
        {
            get
            {
                return _fuelTankMass;
            }
        }
        private double _fuelTankCapacity = 100d;
        public double FuelTankCapacity
        {
            get
            {
                return _fuelTankCapacity;
            }
        }
        private double _fuelDensity = .08d;
        public double FuelDensity
        {
            get
            {
                return _fuelDensity;
            }
        }
        private double _fuelThrustRatio = .005d;
        public double FuelThrustRatio
        {
            get
            {
                return _fuelThrustRatio;
            }
        }

        public double TotalMass
        {
            get
            {
                double retVal = _hullMass;

                if (_cargoBay == null)
                {
                    retVal += _cargoBayMass;
                }
                else
                {
                    retVal += _cargoBay.TotalMass;
                }

                if (_fuelTank == null)
                {
                    retVal += _fuelTankMass;
                }
                else
                {
                    retVal += _fuelTank.TotalMass;
                }

                return retVal;
            }
        }

        private double _radiusX = 1d;
        public double RadiusX
        {
            get
            {
                return _radiusX;
            }
            set
            {
                if (_map != null)
                {
                    throw new InvalidOperationException("Can't set the radius after the ship is created");
                }

                _radiusX = value;

                _radius = GetBoundingRadius();
            }
        }
        private double _radiusY = .66d;
        public double RadiusY
        {
            get
            {
                return _radiusY;
            }
            set
            {
                if (_map != null)
                {
                    throw new InvalidOperationException("Can't set the radius after the ship is created");
                }

                _radiusY = value;

                _radius = GetBoundingRadius();
            }
        }
        private double _radiusZ = .2d;
        public double RadiusZ
        {
            get
            {
                return _radiusZ;
            }
            set
            {
                if (_map != null)
                {
                    throw new InvalidOperationException("Can't set the radius after the ship is created");
                }

                _radiusZ = value;

                _radius = GetBoundingRadius();
            }
        }

        private Color _hullColor = Colors.White;
        //NOTE:  This should be stored opaque.  I make the hull semitransparent when I create it
        public Color HullColor
        {
            get
            {
                return _hullColor;
            }
            set
            {
                _hullColor = value;
            }
        }

        private SwarmFormation _swarmbotFormation = SwarmFormation.None;
        public SwarmFormation Swarmbots
        {
            get
            {
                return _swarmbotFormation;
            }
            set
            {
                ChangeSwarmBots(value);
            }
        }

        private int _numSwarmbots = 20;
        public int NumSwarmbots
        {
            get
            {
                return _numSwarmbots;
            }
            set
            {
                if (_numSwarmbots == value)
                {
                    return;
                }

                _numSwarmbots = value;

                ChangeSwarmBots(_swarmbotFormation);
            }
        }

        private bool _areSwarmbotsUniformSize = true;
        public bool AreSwarmbotsUniformSize
        {
            get
            {
                return _areSwarmbotsUniformSize;
            }
            set
            {
                if (_areSwarmbotsUniformSize == value)
                {
                    return;
                }

                _areSwarmbotsUniformSize = value;

                ChangeSwarmBots(_swarmbotFormation);
            }
        }

        private bool _showDebugVisuals = false;
        public bool ShowDebugVisuals
        {
            get
            {
                return _showDebugVisuals;
            }
            set
            {
                _showDebugVisuals = value;

                foreach (PointVisualizer sprite in _swarmbotChasePointSprites)
                {
                    sprite.ShowPosition = _showDebugVisuals;
                    sprite.ShowVelocity = _showDebugVisuals;
                    sprite.ShowAcceleration = _showDebugVisuals && DEBUGSHOWSACCELERATION;
                }

                foreach (List<SwarmBot2> bots in _swarmBots)
                {
                    foreach (SwarmBot2 bot in bots)
                    {
                        bot.ShouldDrawThrustLine = _showDebugVisuals;
                        bot.ShouldShowDebugVisuals = _showDebugVisuals;
                    }
                }
            }
        }

        // ------------- properties below here are the consumables

        public ReadOnlyCollection<Mineral> CargoBayContents
        {
            get
            {
                return _cargoBay.Contents;
            }
        }

        public double FuelQuantityCurrent
        {
            get
            {
                return _fuelTank.QuantityCurrent;
            }
        }
        public double FuelQuantityMax
        {
            get
            {
                return _fuelTank.QuantityMax;
            }
        }

        private decimal _credits = 0m;
        /// <summary>
        /// This is how much money the ship has
        /// </summary>
        /// <remarks>
        /// In the future, I may want to make a player class that holds things like money
        /// </remarks>
        public decimal Credits
        {
            get
            {
                return _credits;
            }
            set
            {
                _credits = value;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Set the properties, then call create
        /// NOTE:  This adds itself to the viewport and world.  In the future, that should be handled by the caller
        /// </summary>
        public void CreateShip(MaterialManager materialManager, SharedVisuals sharedVisuals, Map map, ProgressBarGame progressBarCargo, ProgressBarGame progressBarFuel)
        {
            const double THRUSTLINELENGTH = .5d;
            const double THRUSTLINELENGTH_EXTRA = .75d;
            const double THRUSTLINELENGTH_TURN = .3;

            _sharedVisuals = sharedVisuals;
            _map = map;
            _progressBarCargo = progressBarCargo;
            _progressBarFuel = progressBarFuel;

            #region Thrusters

            // These need to be definded before the visuals are created
            double radians = Math3D.DegreesToRadians(225);
            _thrusterOffset_BottomLeft = new Vector3D(this.RadiusX * Math.Cos(radians), this.RadiusY * Math.Sin(radians), 0);
            radians = Math3D.DegreesToRadians(135);
            _thrusterOffset_TopLeft = new Vector3D(this.RadiusX * Math.Cos(radians), this.RadiusY * Math.Sin(radians), 0);

            radians = Math3D.DegreesToRadians(315);
            _thrusterOffset_BottomRight = new Vector3D(this.RadiusX * Math.Cos(radians), this.RadiusY * Math.Sin(radians), 0);
            radians = Math3D.DegreesToRadians(45);
            _thrusterOffset_TopRight = new Vector3D(this.RadiusX * Math.Cos(radians), this.RadiusY * Math.Sin(radians), 0);

            _thrustForce = 100d;
            _torqueballLeftRightThrusterForce = -10d;

            #region Define ThrustLines

            //NOTE: The ThrustLine class will add/remove visuals directly from the viewport.  There's no need for the map to get involved

            // The reference to the fuel tank will be made when I create the tank

            ThrustLine thrustLine;

            // Up
            _thrustLines.Add(Key.Up, new List<ThrustLine>());

            thrustLine = new ThrustLine(_map.Viewport, _sharedVisuals, new Vector3D(0, 1, 0) * _thrustForce, _thrusterOffset_BottomRight);
            thrustLine.LineMaxLength = THRUSTLINELENGTH;
            _thrustLines[Key.Up].Add(thrustLine);

            thrustLine = new ThrustLine(_map.Viewport, _sharedVisuals, new Vector3D(0, 1, 0) * _thrustForce, _thrusterOffset_BottomLeft);
            thrustLine.LineMaxLength = THRUSTLINELENGTH;
            _thrustLines[Key.Up].Add(thrustLine);

            // W
            _thrustLines.Add(Key.W, new List<ThrustLine>());

            thrustLine = new ThrustLine(_map.Viewport, _sharedVisuals, new Vector3D(0, 1, 0) * (_thrustForce * 2d), _thrusterOffset_BottomRight);
            thrustLine.LineMaxLength = THRUSTLINELENGTH_EXTRA;
            _thrustLines[Key.W].Add(thrustLine);

            thrustLine = new ThrustLine(_map.Viewport, _sharedVisuals, new Vector3D(0, 1, 0) * (_thrustForce * 2d), _thrusterOffset_BottomLeft);
            thrustLine.LineMaxLength = THRUSTLINELENGTH_EXTRA;
            _thrustLines[Key.W].Add(thrustLine);

            // Down
            _thrustLines.Add(Key.Down, new List<ThrustLine>());

            thrustLine = new ThrustLine(_map.Viewport, _sharedVisuals, new Vector3D(0, -1, 0) * _thrustForce, _thrusterOffset_TopRight);
            thrustLine.LineMaxLength = THRUSTLINELENGTH;
            _thrustLines[Key.Down].Add(thrustLine);

            thrustLine = new ThrustLine(_map.Viewport, _sharedVisuals, new Vector3D(0, -1, 0) * _thrustForce, _thrusterOffset_TopLeft);
            thrustLine.LineMaxLength = THRUSTLINELENGTH;
            _thrustLines[Key.Down].Add(thrustLine);

            // S
            _thrustLines.Add(Key.S, new List<ThrustLine>());

            thrustLine = new ThrustLine(_map.Viewport, _sharedVisuals, new Vector3D(0, -1, 0) * (_thrustForce * 2d), _thrusterOffset_TopRight);
            thrustLine.LineMaxLength = THRUSTLINELENGTH_EXTRA;
            _thrustLines[Key.S].Add(thrustLine);

            thrustLine = new ThrustLine(_map.Viewport, _sharedVisuals, new Vector3D(0, -1, 0) * (_thrustForce * 2d), _thrusterOffset_TopLeft);
            thrustLine.LineMaxLength = THRUSTLINELENGTH_EXTRA;
            _thrustLines[Key.S].Add(thrustLine);

            // Left
            _thrustLines.Add(Key.Left, new List<ThrustLine>());

            thrustLine = new ThrustLine(_map.Viewport, _sharedVisuals, new Vector3D(0, -1, 0) * _torqueballLeftRightThrusterForce, _thrusterOffset_BottomRight);
            thrustLine.LineMaxLength = THRUSTLINELENGTH_TURN;
            _thrustLines[Key.Left].Add(thrustLine);

            thrustLine = new ThrustLine(_map.Viewport, _sharedVisuals, new Vector3D(0, 1, 0) * _torqueballLeftRightThrusterForce, _thrusterOffset_TopLeft);
            thrustLine.LineMaxLength = THRUSTLINELENGTH_TURN;
            _thrustLines[Key.Left].Add(thrustLine);

            // Right
            _thrustLines.Add(Key.Right, new List<ThrustLine>());

            thrustLine = new ThrustLine(_map.Viewport, _sharedVisuals, new Vector3D(0, 1, 0) * _torqueballLeftRightThrusterForce, _thrusterOffset_TopRight);
            thrustLine.LineMaxLength = THRUSTLINELENGTH_TURN;
            _thrustLines[Key.Right].Add(thrustLine);

            thrustLine = new ThrustLine(_map.Viewport, _sharedVisuals, new Vector3D(0, -1, 0) * _torqueballLeftRightThrusterForce, _thrusterOffset_BottomLeft);
            thrustLine.LineMaxLength = THRUSTLINELENGTH_TURN;
            _thrustLines[Key.Right].Add(thrustLine);

            // A
            _thrustLines.Add(Key.A, new List<ThrustLine>());

            thrustLine = new ThrustLine(_map.Viewport, _sharedVisuals, new Vector3D(-1, 0, 0) * _thrustForce, _thrusterOffset_TopRight);
            thrustLine.LineMaxLength = THRUSTLINELENGTH;
            _thrustLines[Key.A].Add(thrustLine);

            thrustLine = new ThrustLine(_map.Viewport, _sharedVisuals, new Vector3D(-1, 0, 0) * _thrustForce, _thrusterOffset_BottomRight);
            thrustLine.LineMaxLength = THRUSTLINELENGTH;
            _thrustLines[Key.A].Add(thrustLine);

            // D
            _thrustLines.Add(Key.D, new List<ThrustLine>());

            thrustLine = new ThrustLine(_map.Viewport, _sharedVisuals, new Vector3D(1, 0, 0) * _thrustForce, _thrusterOffset_TopLeft);
            thrustLine.LineMaxLength = THRUSTLINELENGTH;
            _thrustLines[Key.D].Add(thrustLine);

            thrustLine = new ThrustLine(_map.Viewport, _sharedVisuals, new Vector3D(1, 0, 0) * _thrustForce, _thrusterOffset_BottomLeft);
            thrustLine.LineMaxLength = THRUSTLINELENGTH;
            _thrustLines[Key.D].Add(thrustLine);

            #endregion

            #endregion

            MaterialGroup material = null;
            GeometryModel3D geometry = null;
            Model3DGroup models = new Model3DGroup();
            ModelVisual3D model = null;

            #region Interior Extra Visuals

            // These are visuals that will stay oriented to the ship, but don't count in collision calculations

            #region Thrusters

            // These are the little balls, not the thrust lines

            double thrusterLocScale = 1d;

            models.Children.Add(GetThrusterVisual(_thrusterOffset_BottomLeft * thrusterLocScale));
            models.Children.Add(GetThrusterVisual(_thrusterOffset_BottomRight * thrusterLocScale));
            models.Children.Add(GetThrusterVisual(_thrusterOffset_TopLeft * thrusterLocScale));
            models.Children.Add(GetThrusterVisual(_thrusterOffset_TopRight * thrusterLocScale));

            #endregion
            #region Cargo Bay

            //TODO:  Make a visual for this (probably pretty cube like)

            _cargoBay = new CargoBay(_cargoBayMass, _cargoBayVolume);

            _progressBarCargo.Minimum = 0d;
            _progressBarCargo.Maximum = _cargoBay.MaxVolume;
            _progressBarCargo.Value = 0d;

            #endregion
            #region Fuel Tank

            //TODO:  This visual should be a pill

            _fuelTank = new FuelTank();
            _fuelTank.DryMass = _fuelTankMass;
            _fuelTank.QuantityMax = _fuelTankCapacity;
            _fuelTank.QuantityCurrent = _fuelTank.QuantityMax;		// a full tank with the purchace of a new ship!!!
            _fuelTank.FuelDensity = _fuelDensity;

            _progressBarFuel.Minimum = 0d;
            _progressBarFuel.Maximum = _fuelTank.QuantityMax;
            _progressBarFuel.Value = _fuelTank.QuantityCurrent;

            // Link to the thrusters
            foreach (List<ThrustLine> thrustLines in _thrustLines.Values)
            {
                foreach (ThrustLine thrustLine1 in thrustLines)
                {
                    thrustLine1.FuelToThrustRatio = _fuelThrustRatio;
                    thrustLine1.FuelTank = _fuelTank;
                }
            }

            #endregion

            #region Core

            // This just looks stupid.  The idea is that you would see the various components (which would also be point masses).  But until
            // the user can modify their ship, it's just an arbitrary ellipse that is ugly

            //// Material
            //material = new MaterialGroup();
            //material.Children.Add(new DiffuseMaterial(Brushes.DimGray));
            //material.Children.Add(new SpecularMaterial(Brushes.DimGray, 100d));

            //// Geometry Model
            //geometry = new GeometryModel3D();
            //geometry.Material = material;
            //geometry.BackMaterial = material;
            //geometry.Geometry = UtilityWPF.GetSphere(5, .45, .25, .05);
            //geometry.Transform = new TranslateTransform3D(0, this.RadiusY * -.25, 0);

            //// Model Visual
            //model = new ModelVisual3D();
            //model.Content = geometry;

            ////NOTE: model.Transform is set to the physics body's transform every frame

            //// Add to the viewport
            //_viewport.Children.Add(model);

            //_visuals.Add(model);

            #endregion

            // Make a model visual for what I have so far
            model = new ModelVisual3D();        // this is the expensive one, so as few of these should be made as possible
            model.Content = models;

            _visuals.Add(model);
            _map.Viewport.Children.Add(model);

            #endregion

            #region WPF Collision Model

            // Material
            //NOTE:  There seems to be an issue with drawing objects inside a semitransparent object - I think they have to be added in a certain order or something
            //Brush skinBrush = new SolidColorBrush(Color.FromArgb(50, _hullColor.R, _hullColor.G, _hullColor.B));  // making the skin semitransparent, so you can see the components inside
            Brush skinBrush = new SolidColorBrush(_hullColor);  // decided to make it opaque, since I'm not showing anything inside

            material = new MaterialGroup();
            material.Children.Add(new DiffuseMaterial(skinBrush));
            material.Children.Add(new SpecularMaterial(Brushes.White, 75d));     // more reflective (and white light)

            MaterialGroup backMaterial = new MaterialGroup();
            backMaterial.Children.Add(new DiffuseMaterial(skinBrush));
            backMaterial.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromArgb(255, 20, 20, 20)), 10d));       // dark light, and not very reflective

            // Geometry Model
            geometry = new GeometryModel3D();
            geometry.Material = material;
            geometry.BackMaterial = backMaterial;
            geometry.Geometry = UtilityWPF.GetSphere(5, this.RadiusX, this.RadiusY, this.RadiusZ);

            // Transform
            Transform3DGroup transform = new Transform3DGroup();		// rotate needs to be added before translate
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 0)));
            transform.Children.Add(new TranslateTransform3D(new Vector3D(0, 0, 0)));

            // Model Visual
            model = new ModelVisual3D();
            model.Content = geometry;
            model.Transform = transform;

            _physicsModel = model;    // remember this, so I don't set its transform
            _visuals.Add(model);

            // Add to the viewport (the physics body constructor requires it to be added)
            _map.Viewport.Children.Add(model);

            #endregion
            #region Physics Body

            // Make a physics body that represents this shape
            _physicsBody = new ConvexBody3D(_map.World, model);

            _physicsBody.MaterialGroupID = materialManager.ShipMaterialID;

            _physicsBody.NewtonBody.UserData = this;

            _physicsBody.Mass = Convert.ToSingle(this.TotalMass);

            _physicsBody.LinearDamping = .01f;
            //_physicsBody.AngularDamping = new Vector3D(.01f, .01f, .01f);
            //_physicsBody.AngularDamping = new Vector3D(10f, 10f, 10f);
            _physicsBody.AngularDamping = new Vector3D(1f, 1f, 1f);

            _physicsBody.ApplyForce += new BodyForceEventHandler(Body_ApplyForce);

            #endregion

            #region Exterior Extra Visuals

            // There is a bug in WPF where visuals added after a semitransparent one won't show inside.  The cockpit looks stupid when you can see
            // it inside the skin

            #region Cockpit

            // Material
            material = new MaterialGroup();
            material.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(Colors.Teal, Colors.DimGray, .2d))));
            material.Children.Add(new SpecularMaterial(Brushes.White, 100d));

            // Geometry Model
            geometry = new GeometryModel3D();
            geometry.Material = material;
            geometry.BackMaterial = material;
            //geometry.Geometry = UtilityWPF.GetSphere(3, .4, .2, .3);
            geometry.Geometry = UtilityWPF.GetSphere(3, .45, .25, .25);
            geometry.Transform = new TranslateTransform3D(0, this.RadiusY * .5, 0);

            // Model Visual
            model = new ModelVisual3D();
            model.Content = geometry;

            // Add to the viewport
            _visuals.Add(model);
            _map.Viewport.Children.Add(model);

            #endregion
            #region Headlight

            SpotLight spotLight = new SpotLight();
            //spotLight.Color = Color.FromArgb(255, 50, 170, 50);
            spotLight.Color = UtilityWPF.AlphaBlend(Colors.White, _hullColor, .25d);
            spotLight.Direction = new Vector3D(0, 1, 0);
            spotLight.OuterConeAngle = 25;
            spotLight.InnerConeAngle = 5;
            //spotLight.LinearAttenuation = .1;
            spotLight.QuadraticAttenuation = .0001;
            spotLight.Range = 1000;

            model = new ModelVisual3D();
            model.Content = spotLight;

            _visuals.Add(model);
            _map.Viewport.Children.Add(model);

            #endregion

            #endregion

            // Add to the map
            _map.AddItem(this);
        }

        public void WorldUpdating(double elapsedTime)
        {
            #region Visuals

            foreach (ModelVisual3D model in _visuals)
            {
                if (model == _physicsModel)
                {
                    // The physics engine already transformed this one to world
                    continue;
                }

                // By setting this transform, is will render wherever the physics body is
                model.Transform = _physicsBody.Transform;
            }

            #endregion

            #region Thrust Lines

            Transform3D transform = _physicsBody.Transform;        // I just don't want to keep hitting the property
            foreach (List<ThrustLine> thrusts in _thrustLines.Values)
            {
                foreach (ThrustLine thrust in thrusts)
                {
                    thrust.DrawVisual(1d, transform);
                }
            }

            #endregion

            #region Swarmbots

            for (int cntr = 0; cntr < _swarmBots.Count; cntr++)
            {
                #region Swarm Bot Set

                //TODO:  Let the chase points rotate more loosely than the ship

                // Figure out the chase point
                Point3D chasePoint = _physicsBody.PositionToWorld(_swarmbotChasePoints[cntr]);
                Vector3D chasePointVelocity = this.VelocityWorld;
                Vector3D chasePointAcceleration = _physicsBody.AccelerationCached;

                _swarmbotChasePointSprites[cntr].Update(chasePoint, chasePointVelocity, chasePointAcceleration);

                foreach (SwarmBot2 bot in _swarmBots[cntr])
                {
                    bot.ChasePoint = chasePoint;
                    bot.ChasePointVelocity = chasePointVelocity;
                    bot.WorldUpdating();
                }

                #endregion
            }

            #endregion

            // Progress bars
            _progressBarFuel.Value = _fuelTank.QuantityCurrent;

            // I don't want to change the mass every frame (lots of calculations need to be made), so only do it if there is a
            // greater than 3% difference in mass
            //NOTE:  Currently only fuel is the variable mass, but there could be others in the future
            double totalMass = this.TotalMass;
            if (_lastSetMass == null || Math.Abs(totalMass - _lastSetMass.Value) > totalMass * .03d)
            {
                _physicsBody.Mass = Convert.ToSingle(totalMass);
                _lastSetMass = totalMass;
            }
        }

        private double? _lastSetMass = null;

        //private SoundPool _testSound = new SoundPool(@"C:\Temp\thrustquick.wav");

        // The user is pressing keys on the keyboard
        public void KeyDown(Key key)
        {
            if (_thrustLines.ContainsKey(key))
            {
                foreach (ThrustLine thrust in _thrustLines[key])
                {
                    thrust.IsFiring = true;
                }


                //This doesn't work, because it's trying to instantiate everything too often?  Or the instance gets collected too quickly?
                //new SoundPool().Test(@"C:\Temp\THRUST.WAV");

                // This works better, but there is still a gap before it plays again
                //_testSound.Test1();

            }
        }
        public void KeyUp(Key key)
        {
            if (_thrustLines.ContainsKey(key))
            {
                foreach (ThrustLine thrust in _thrustLines[key])
                {
                    thrust.IsFiring = false;
                }
            }
        }

        public bool CollideWithMineral(Mineral mineral)
        {
            bool retVal = false;

            //TODO:  Only try to add if the relative velocity is small enough

            if (_cargoBay.Add(mineral))
            {
                _physicsBody.Mass = Convert.ToSingle(this.TotalMass);

                _progressBarCargo.Value = _cargoBay.UsedVolume;

                retVal = true;
            }

            return retVal;
        }
        public void SellCargo(decimal credits)
        {
            _cargoBay.ClearContents();
            _physicsBody.Mass = Convert.ToSingle(this.TotalMass);
            _progressBarCargo.Value = _cargoBay.UsedVolume;
            _credits += credits;
        }

        public void CollideWithAsteroid(Asteroid asteroid, CollisionEndEventArgs e)
        {
            // Figure out the impulse










        }

        public void BuyFuel(double fuel, decimal credits)
        {
            _fuelTank.QuantityCurrent += fuel;
            _physicsBody.Mass = Convert.ToSingle(this.TotalMass);
            _progressBarFuel.Value = _fuelTank.QuantityCurrent;
            _credits -= credits;
        }

        #endregion

        #region Event Listeners

        private void Body_ApplyForce(Body sender, BodyForceEventArgs e)
        {
            if (sender != _physicsBody)
            {
                return;
            }
            //e.ElapsedTime
            #region Thrusters

            Transform3D transform = _physicsBody.Transform;        // I just don't want to keep hitting the property
            foreach (List<ThrustLine> thrusts in _thrustLines.Values)
            {
                foreach (ThrustLine thrust in thrusts)
                {
                    thrust.ApplyForce(1d, transform, e);
                }
            }

            #endregion
        }

        #endregion

        #region Private Methods

        private Model3D GetThrusterVisual(Vector3D location)
        {
            // Material
            MaterialGroup material = new MaterialGroup();
            material.Children.Add(new DiffuseMaterial(Brushes.DarkGray));
            material.Children.Add(new SpecularMaterial(Brushes.Gray, 100d));

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = material;
            geometry.BackMaterial = material;
            geometry.Geometry = UtilityWPF.GetSphere(3, .1, .1, .1);
            geometry.Transform = new TranslateTransform3D(location);

            // Exit Function
            return geometry;

            //// Model Visual
            //ModelVisual3D retVal = new ModelVisual3D();
            //retVal.Content = geometry;

            //// Exit Function
            //return retVal;
        }

        private double GetBoundingRadius()
        {
            double retVal = _radiusX;

            if (_radiusY > retVal)
            {
                retVal = _radiusY;
            }

            if (_radiusZ > retVal)
            {
                retVal = _radiusZ;
            }

            return retVal;
        }

        private void ChangeSwarmBots(SwarmFormation formation)
        {
            ChangeSwarmBotsSprtRemoveAll();

            if (formation == SwarmFormation.None)
            {
                _swarmbotFormation = SwarmFormation.None;
                return;
            }

            double startAngle, stepAngle;       // 0 is straight in front of the ship
            int numBots;
            double distanceMult;

            switch (formation)
            {
                case SwarmFormation.AllFront:
                    #region AllFront

                    startAngle = 0d;
                    stepAngle = 360d;

                    //numBots = 20;
                    numBots = _numSwarmbots;

                    distanceMult = 8d;

                    #endregion
                    break;

                case SwarmFormation.AllRear:
                    #region AllRear

                    startAngle = 180d;
                    stepAngle = 360d;

                    //numBots = 20;
                    numBots = _numSwarmbots;

                    distanceMult = 8d;

                    #endregion
                    break;

                case SwarmFormation.Triangle:
                    #region Triangle

                    startAngle = 0d;
                    stepAngle = 120d;

                    //numBots = 10;
                    numBots = _numSwarmbots / 3;
                    if (numBots % 3 != 0 || numBots == 0)
                    {
                        numBots++;
                    }

                    distanceMult = 9d;

                    #endregion
                    break;

                case SwarmFormation.ReverseTriangle:
                    #region ReverseTriangle

                    startAngle = 180d;
                    stepAngle = 120d;

                    //numBots = 10;
                    numBots = _numSwarmbots / 3;
                    if (numBots % 3 != 0 || numBots == 0)
                    {
                        numBots++;
                    }

                    distanceMult = 9d;

                    #endregion
                    break;

                case SwarmFormation.Pentagon:
                    #region Pentagon

                    startAngle = 0d;
                    stepAngle = 72d;

                    //numBots = 7;
                    numBots = _numSwarmbots / 5;
                    if (numBots % 5 != 0 || numBots == 0)
                    {
                        numBots++;
                    }

                    distanceMult = 9d;

                    #endregion
                    break;

                case SwarmFormation.ReversePentagon:
                    #region ReversePentagon

                    startAngle = 180d;
                    stepAngle = 72d;

                    //numBots = 7;
                    numBots = _numSwarmbots / 5;
                    if (numBots % 5 != 0 || numBots == 0)
                    {
                        numBots++;
                    }

                    distanceMult = 9d;

                    #endregion
                    break;

                case SwarmFormation.SurroundShip:
                    #region SurroundShip

                    startAngle = 0d;
                    stepAngle = 360d;

                    //numBots = 30;
                    numBots = _numSwarmbots;

                    distanceMult = 0d;

                    #endregion
                    break;

                default:
                    throw new ApplicationException("Unknown SwarmFormation: " + formation.ToString());
            }

            // Figure out how far away from the ship the bots should be
            double chaseOffsetDistance = (_radiusX + _radiusY) / 2d;
            chaseOffsetDistance *= distanceMult;

            // Build the swarms
            double angle = startAngle;
            while (angle < startAngle + 360d)
            {
                Vector3D chasePoint = new Vector3D(0, chaseOffsetDistance, 0);
                chasePoint = chasePoint.GetRotatedVector(new Vector3D(0, 0, 1), angle);

                _swarmBots.Add(ChangeSwarmBotsSprtSwarm(numBots, chasePoint));
                _swarmbotChasePoints.Add(chasePoint.ToPoint());

                PointVisualizer chasePointSprite = new PointVisualizer(_map.Viewport, _sharedVisuals);
                chasePointSprite.PositionRadius = .1d;
                chasePointSprite.VelocityAccelerationLengthMultiplier = .05d;
                chasePointSprite.ShowPosition = _showDebugVisuals;
                chasePointSprite.ShowVelocity = _showDebugVisuals;
                chasePointSprite.ShowAcceleration = _showDebugVisuals && DEBUGSHOWSACCELERATION;
                _swarmbotChasePointSprites.Add(chasePointSprite);

                angle += stepAngle;
            }

            _swarmbotFormation = formation;
        }
        private void ChangeSwarmBotsSprtRemoveAll()
        {
            // Swarmbots
            foreach (List<SwarmBot2> bots in _swarmBots)
            {
                foreach (SwarmBot2 bot in bots)
                {
                    _map.RemoveItem(bot);
                }
            }

            // Chase points
            foreach (PointVisualizer chaseVisual in _swarmbotChasePointSprites)
            {
                chaseVisual.HideAll();
            }

            _swarmBots.Clear();
            _swarmbotChasePoints.Clear();
            _swarmbotChasePointSprites.Clear();
        }
        private List<SwarmBot2> ChangeSwarmBotsSprtSwarm(int numBots, Vector3D startPoint)
        {
            List<SwarmBot2> retVal = new List<SwarmBot2>();

            for (int cntr = 0; cntr < numBots; cntr++)
            {
                #region Bots

                // Set up the bot's properties
                SwarmBot2 swarmBot = new SwarmBot2();

                double sizeMultiplier = 1d;
                if (!_areSwarmbotsUniformSize)
                {
                    //sizeMultiplier = UtilityHelper.GetScaledValue(.5d, 4d, 0d, 1d, _rand.NextDouble());		// this does an even distribution of sizes, but I want most to be size 1

                    // This scales from .5 to about 4.5, and keeps the chances of it being around 1 longer
                    // .5 + (.75 + (2x-.88)^3)^2
                    //sizeMultiplier = 2 * _rand.NextDouble() - .88d;
                    //sizeMultiplier = Math.Pow(sizeMultiplier, 3d);
                    //sizeMultiplier += .75d;
                    //sizeMultiplier = Math.Pow(sizeMultiplier, 2d);
                    //sizeMultiplier += .5d;

                    sizeMultiplier = .5 + Math.Pow(.75 + Math.Pow(2 * StaticRandom.NextDouble() - .88, 3), 2);		// hard to read either way
                }

                double radius, mass, thrustForce, visionLimit;
                int numClosestBotsToLookAt;
                ChangeSwarmBotsSprtSizeProps(out radius, out mass, out thrustForce, out numClosestBotsToLookAt, out visionLimit, sizeMultiplier);
                swarmBot.Radius = radius;
                swarmBot.Mass = mass;
                swarmBot.ThrustForce = thrustForce;
                swarmBot.NumClosestBotsToLookAt = numClosestBotsToLookAt;
                swarmBot.VisionLimit = visionLimit;

                swarmBot.Behavior = SwarmBot2.BehaviorType.Flocking_ChasePoint_AvoidKnownObsticles;
                swarmBot.Obsticles.Add(_physicsBody);

                swarmBot.CoreColor = _hullColor;

                swarmBot.ShouldDrawThrustLine = _showDebugVisuals;
                swarmBot.ThrustLineMultiplier = 1.5d;

                swarmBot.ShouldShowDebugVisuals = _showDebugVisuals;

                // Figure out position
                Vector3D position = startPoint + Math3D.GetRandomVectorSpherical(3d);
                position += _physicsBody.PositionToWorld(_physicsBody.CenterOfMass).ToVector();		// this isn't right

                // Create the bot
                //NOTE:  I don't need to manipulate the swarm bots directly, so I won't hook to the body update event
                swarmBot.CreateBot(_map.Viewport, _sharedVisuals, _map.World, position.ToPoint());
                _map.AddItem(swarmBot);
                retVal.Add(swarmBot);

                #endregion
            }

            // Now that they're all created, tell each about the others
            foreach (SwarmBot2 bot in retVal)
            {
                #region Tell about each other

                foreach (SwarmBot2 otherBot in retVal)
                {
                    if (otherBot == bot)
                    {
                        continue;
                    }

                    bot.OtherBots.Add(otherBot);
                }

                #endregion
            }

            return retVal;
        }
        private void ChangeSwarmBotsSprtSizeProps(out double radius, out double mass, out double thrustForce, out int numClosestBotsToLookAt, out double visionLimit, double sizeMultiplier)
        {
            const double RADIUS = .125d;
            const double DENSITY = 61.115d;		// I want the mass to be .5 when radius is .125:  d = m / (4/3 * pi * r^3)
            const double ACCEL_HALF = 190d;		// note that these don't scale evenly with mass.  I don't want the acceleration affected that much (I want the acceleration to be 160 when size multiplier is 1)
            const double ACCEL_FOUR = 130d;
            const double VISIONLIMIT_HALF = 12d;		// middle is 20
            const double VISIONLIMIT_FOUR = 28d;
            const double CLOSECOUNT_HALF = 2d;
            const double CLOSECOUNT_TWICE = 4d;

            radius = RADIUS * sizeMultiplier;

            // v = 4/3 * pi * r^3
            // m = d * v
            // m = d * 4/3 * pi * r^3
            mass = DENSITY * 4 / 3 * Math.PI * Math.Pow(radius, 3);

            // f = ma
            // a = f / m
            double accel = UtilityHelper.GetScaledValue(ACCEL_HALF, ACCEL_FOUR, .5d, 4d, sizeMultiplier);		//NOTE:  not capping it, the min/max are just for scaling
            thrustForce = mass * accel;

            visionLimit = UtilityHelper.GetScaledValue(VISIONLIMIT_HALF, VISIONLIMIT_FOUR, .5d, 4d, sizeMultiplier);

            numClosestBotsToLookAt = Convert.ToInt32(Math.Round(UtilityHelper.GetScaledValue(CLOSECOUNT_HALF, CLOSECOUNT_TWICE, .5d, 2d, sizeMultiplier)));
            if (numClosestBotsToLookAt < 2)
            {
                numClosestBotsToLookAt = 2;
            }
        }

        #endregion
    }
}
