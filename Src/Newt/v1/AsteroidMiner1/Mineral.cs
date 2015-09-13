using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using Game.HelperClassesWPF;
using Game.Newt.v1.NewtonDynamics1;
using Game.HelperClassesCore;

namespace Game.Newt.v1.AsteroidMiner1
{
    public class Mineral : IMapObject
    {
        #region Declaration Section

        private Map _map = null;

        // The physics engine keeps this one synced, no need to transform it every frame
        private ModelVisual3D _physicsModel = null;

        private SharedVisuals _sharedVisuals = null;

        private bool _hasSetInitialVelocity = false;

        #endregion

        #region Constructor

        public Mineral()
        {
        }

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

        private double _radius = 1d;      // this is the max of the x,y,z sizes
        public double Radius
        {
            get
            {
                return _radius;
            }
        }

        #endregion

        #region Public Properties

        private MineralType _mineralType = MineralType.Custom;
        public MineralType MineralType
        {
            get
            {
                return _mineralType;
            }
            set
            {
                if (_map != null)
                {
                    throw new InvalidOperationException("Can't set the mineral type after the mineral is created");
                }

                _mineralType = value;
            }
        }

        // These are overall, the ring definitions are still used, but these scale the entire model
        // Also note that I don't use them to calculate volume, they are just for size in world
        private double _sizeX = 1d;
        public double SizeX
        {
            get
            {
                return _sizeX;
            }
            set
            {
                if (_map != null)
                {
                    throw new InvalidOperationException("Can't set the size after the mineral is created");
                }

                _sizeX = value;

                _radius = GetBoundingRadius();
            }
        }
        private double _sizeY = 1d;
        public double SizeY
        {
            get
            {
                return _sizeY;
            }
            set
            {
                if (_map != null)
                {
                    throw new InvalidOperationException("Can't set the size after the mineral is created");
                }

                _sizeY = value;

                _radius = GetBoundingRadius();
            }
        }
        private double _sizeZ = 1d;
        public double SizeZ
        {
            get
            {
                return _sizeZ;
            }
            set
            {
                if (_map != null)
                {
                    throw new InvalidOperationException("Can't set the size after the mineral is created");
                }

                _sizeZ = value;

                _radius = GetBoundingRadius();
            }
        }

        // ----------------------------------------- these are only referenced when the mineral type is custom (but they are set during create to a hard coded definition if it's another type)

        private double _mass = 1d;
        public double Mass
        {
            get
            {
                return _mass;
            }
            set
            {
                if (_map != null)
                {
                    throw new InvalidOperationException("Can't set the mass after the mineral is created");
                }

                _mass = value;
            }
        }

        private Color _diffuseColor = Colors.Chartreuse;
        public Color DiffuseColor
        {
            get
            {
                return _diffuseColor;
            }
            set
            {
                if (_map != null)
                {
                    throw new InvalidOperationException("Can't set the color after the mineral is created");
                }

                _diffuseColor = value;
            }
        }

        private Color _specularColor = Colors.Green;
        public Color SpecularColor
        {
            get
            {
                return _specularColor;
            }
            set
            {
                if (_map != null)
                {
                    throw new InvalidOperationException("Can't set the color after the mineral is created");
                }

                _specularColor = value;
            }
        }

        private double _specularPower = 100d;
        public double SpecularPower
        {
            get
            {
                return _specularPower;
            }
            set
            {
                if (_map != null)
                {
                    throw new InvalidOperationException("Can't set the color after the mineral is created");
                }

                _specularPower = value;
            }
        }

        private Color _emissiveColor = UtilityWPF.AlphaBlend(Colors.YellowGreen, Colors.Transparent, .15d);
        public Color EmissiveColor
        {
            get
            {
                return _emissiveColor;
            }
            set
            {
                if (_map != null)
                {
                    throw new InvalidOperationException("Can't set the color after the mineral is created");
                }

                _emissiveColor = value;
            }
        }

        private int _numSides = 4;
        public int NumSides
        {
            get
            {
                return _numSides;
            }
            set
            {
                if (_map != null)
                {
                    throw new InvalidOperationException("Can't set the number of sides after the mineral is created");
                }

                _numSides = value;
            }
        }

        private List<TubeRingDefinition_ORIG> _rings = new List<TubeRingDefinition_ORIG>();
        public List<TubeRingDefinition_ORIG> Rings
        {
            get
            {
                return _rings;
            }
        }

        // -------------------------------------------

        private Vector3D _initialVelocity = new Vector3D(0, 0, 0);
        public Vector3D InitialVelocity
        {
            get
            {
                return _initialVelocity;
            }
            set
            {
                if (_map != null)
                {
                    throw new InvalidOperationException("Can't set the initial velocity after the asteroid is created");
                }

                _initialVelocity = value;
            }
        }

        #endregion

        #region Public Methods

        public void CreateMineral(MaterialManager materialManager, Map map, SharedVisuals sharedVisuals, Vector3D position, bool randomOrientation, double volumeInCubicMeters)
        {
            _map = map;
            _sharedVisuals = sharedVisuals;

            if (_mineralType == MineralType.Custom)
            {
                #region Validate

                if (_numSides < 3)
                {
                    throw new ApplicationException("The number of sides must at least be 3");
                }

                if (_rings.Count == 0)
                {
                    throw new ApplicationException("Need at least one ring");
                }

                #endregion
            }
            else
            {
                // Overwrite my public properties based on the mineral type
                StoreSettingsForMineralType(volumeInCubicMeters);
            }

            RotateTransform3D randomRotation = null;
            if (randomOrientation)
            {
                randomRotation = new RotateTransform3D(new AxisAngleRotation3D(Math3D.GetRandomVector_Spherical(1d), Math1D.GetNearZeroValue(360d)));
            }

            if (_mineralType == MineralType.Rixium)
            {
                #region Rixium Visuals

                Model3DGroup ringModels = new Model3DGroup();

                ringModels.Children.Add(GetRixiumTorusVisual(new Vector3D(0, 0, -.6), .38, .5, randomRotation));
                ringModels.Children.Add(GetRixiumTorusVisual(new Vector3D(0, 0, -.3), .44, .75, randomRotation));
                ringModels.Children.Add(GetRixiumTorusVisual(new Vector3D(0, 0, 0), .5, 1, randomRotation));
                ringModels.Children.Add(GetRixiumTorusVisual(new Vector3D(0, 0, .3), .44, .75, randomRotation));
                ringModels.Children.Add(GetRixiumTorusVisual(new Vector3D(0, 0, .6), .38, .5, randomRotation));

                //TODO:  Look at the global lighting options
                PointLight pointLight = new PointLight();
                pointLight.Color = Color.FromArgb(255, 54, 147, 168);
                pointLight.Range = 20;
                pointLight.LinearAttenuation = .33;
                ringModels.Children.Add(pointLight);

                ModelVisual3D ringModel = new ModelVisual3D();        // this is the expensive one, so as few of these should be made as possible
                ringModel.Content = ringModels;

                _visuals.Add(ringModel);
                _map.Viewport.Children.Add(ringModel);

                #endregion
            }

            #region WPF Model

            // Material
            MaterialGroup materials = new MaterialGroup();
            if (_diffuseColor.A > 0)
            {
                materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(_diffuseColor)));
            }

            if (_specularColor.A > 0)
            {
                materials.Children.Add(new SpecularMaterial(new SolidColorBrush(_specularColor), _specularPower));
            }

            if (_emissiveColor.A > 0)
            {
                materials.Children.Add(new EmissiveMaterial(new SolidColorBrush(_emissiveColor)));
            }

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;

            if (_mineralType == MineralType.Custom)
            {
                geometry.Geometry = UtilityWPF.GetMultiRingedTube_ORIG(_numSides, _rings, false, true);
            }
            else
            {
                geometry.Geometry = _sharedVisuals.GetMineralMesh(_mineralType);
            }
            if (randomOrientation)
            {
                geometry.Transform = randomRotation;
            }

            // Model Visual
            ModelVisual3D model = new ModelVisual3D();
            model.Content = geometry;
            model.Transform = new TranslateTransform3D(position);

            // Add to the viewport
            _visuals.Add(model);
            _map.Viewport.Children.Add(model);

            #endregion

            #region Physics Body

            // Make a physics body that represents this shape
            _physicsBody = new ConvexBody3D(_map.World, model);

            _physicsBody.MaterialGroupID = materialManager.MineralMaterialID;

            _physicsBody.NewtonBody.UserData = this;

            _physicsBody.Mass = Convert.ToSingle(_mass);

            _physicsBody.LinearDamping = .01f;
            _physicsBody.AngularDamping = new Vector3D(.01f, .01f, .01f);
            _physicsBody.Override2DEnforcement_Rotation = true;

            _physicsBody.ApplyForce += new BodyForceEventHandler(Body_ApplyForce);

            #endregion

            // Add to the map
            _map.AddItem(this);
        }

        public void WorldUpdating()
        {
            if (_mineralType == MineralType.Rixium)
            {
                // There are some extra visuals for this one

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
            }

            // Nothing to do
        }

        public static decimal GetSuggestedValue(MineralType mineralType)
        {
            //TODO:  Get these from some environment settings
            //NOTE:  The commented values are what I got from websites ($15,000 per carat for diamond seems a bit steep though)
            //I was screwing around, and this gave a pretty nice curve:
            //     =10 * ((5 + log10(value))^2)
            //
            // But I ended up rounding the numbers off to give a smoother curve

            switch (mineralType)
            {
                case MineralType.Ice:
                    //return .0003m;
                    return 25m;

                case MineralType.Iron:
                    //return .0017m;
                    return 50m;

                case MineralType.Graphite:
                    //return .08m;
                    return 150m;

                case MineralType.Gold:
                    //return 49420m;
                    return 400m;

                case MineralType.Platinum:
                    //return 59840m;
                    return 700m;

                case MineralType.Emerald:
                    //return 1250000m;
                    return 1000m;

                case MineralType.Saphire:
                    //return 5000000m;
                    return 1200m;

                case MineralType.Ruby:
                    //return 12500000m;
                    return 1500m;

                case MineralType.Diamond:
                    //return 75000000m;
                    return 2000m;

                case MineralType.Rixium:
                    //return 300000000m;
                    return 5000m;

                default:
                    throw new ApplicationException("Unknown MineralType: " + mineralType.ToString());
            }
        }

        #endregion

        #region Event Listeners

        private void Body_ApplyForce(Body sender, BodyForceEventArgs e)
        {
            if (sender != _physicsBody)
            {
                return;
            }

            if (!_hasSetInitialVelocity)
            {
                e.AddImpulse(_initialVelocity, Math3D.GetRandomVector_Spherical(this.Radius * .5d));     // whacking it at an offset, so it has some rotation as well
                _hasSetInitialVelocity = true;

                // No need to listen to this even anymore
                _physicsBody.ApplyForce -= new BodyForceEventHandler(Body_ApplyForce);
            }
        }

        #endregion

        #region Private Methods

        private void StoreSettingsForMineralType(double volumeInCubicMeters)
        {
            if (_mineralType == MineralType.Custom)
            {
                return;
            }

            // These have no meaning unless it's custom
            _numSides = 0;
            _rings.Clear();

            //NOTE:  The geometry is defined in SharedVisuals.GetMineralMesh()

            switch (_mineralType)
            {
                case MineralType.Ice:
                    #region Ice

                    // Going for an ice cube  :)

                    _diffuseColor = Color.FromArgb(192, 201, 233, 242);       // slightly bluish white
                    _specularColor = Color.FromArgb(255, 203, 212, 214);
                    _specularPower = 66d;
                    _emissiveColor = Colors.Transparent;

                    _mass = 934 * volumeInCubicMeters;

                    #endregion
                    break;

                case MineralType.Iron:
                    #region Iron

                    // This will be an iron bar (with rust)

                    _diffuseColor = Color.FromArgb(255, 92, 78, 72);
                    _specularColor = Color.FromArgb(255, 117, 63, 40);
                    _specularPower = 50d;
                    _emissiveColor = Colors.Transparent;

                    _mass = 7900 * volumeInCubicMeters;

                    #endregion
                    break;

                case MineralType.Graphite:
                    #region Graphite

                    // A shiny lump of coal

                    //_diffuseColor = Color.FromArgb(255, 64, 64, 64);
                    _diffuseColor = Color.FromArgb(255, 32, 32, 32);
                    _specularColor = Color.FromArgb(255, 209, 209, 209);
                    _specularPower = 75d;
                    _emissiveColor = Colors.Transparent;

                    _mass = 2267 * volumeInCubicMeters;

                    #endregion
                    break;

                case MineralType.Gold:
                    #region Gold

                    // A reflective gold bar

                    _diffuseColor = Color.FromArgb(255, 255, 191, 0);
                    _specularColor = Color.FromArgb(255, 212, 138, 0);
                    _specularPower = 75d;
                    _emissiveColor = Colors.Transparent;

                    _mass = 19300 * volumeInCubicMeters;

                    #endregion
                    break;

                case MineralType.Platinum:
                    #region Platinum

                    // A reflective platinum bar/plate
                    //TODO:  Make this a flat plate

                    _diffuseColor = Color.FromArgb(255, 166, 166, 166);
                    _specularColor = Color.FromArgb(255, 125, 57, 45);
                    _specularPower = 95d;
                    _emissiveColor = Color.FromArgb(20, 214, 214, 214);

                    _mass = 21450 * volumeInCubicMeters;

                    #endregion
                    break;

                case MineralType.Emerald:
                    #region Emerald

                    // A semi transparent double trapazoid

                    _diffuseColor = Color.FromArgb(192, 69, 128, 64);
                    _specularColor = Color.FromArgb(255, 26, 82, 20);
                    _specularPower = 100d;
                    _emissiveColor = Color.FromArgb(32, 64, 128, 0);

                    _mass = 2760 * volumeInCubicMeters;

                    #endregion
                    break;

                case MineralType.Saphire:
                    #region Saphire

                    // A jeweled oval

                    _diffuseColor = Color.FromArgb(160, 39, 53, 102);
                    _specularColor = Color.FromArgb(255, 123, 141, 201);
                    _specularPower = 100d;
                    _emissiveColor = Color.FromArgb(64, 17, 57, 189);

                    _mass = 4000 * volumeInCubicMeters;

                    #endregion
                    break;

                case MineralType.Ruby:
                    #region Ruby

                    // A jeweled oval

                    _diffuseColor = Color.FromArgb(180, 176, 0, 0);
                    _specularColor = Color.FromArgb(255, 255, 133, 133);
                    _specularPower = 100d;
                    _emissiveColor = Color.FromArgb(32, 156, 53, 53);

                    _mass = 4000 * volumeInCubicMeters;

                    #endregion
                    break;

                case MineralType.Diamond:
                    #region Diamond

                    // A jewel

                    _diffuseColor = Color.FromArgb(128, 230, 230, 230);
                    _specularColor = Color.FromArgb(255, 196, 196, 196);
                    _specularPower = 100d;
                    _emissiveColor = Color.FromArgb(32, 255, 255, 255);

                    _mass = 3515 * volumeInCubicMeters;

                    #endregion
                    break;

                case MineralType.Rixium:
                    #region Rixium

                    // A petagon rod
                    // There are also some toruses around it, but they are just visuals.  This rod is the collision mesh

                    _diffuseColor = Color.FromArgb(192, 92, 59, 112);
                    //_specularColor = Color.FromArgb(255, 145, 63, 196);
                    _specularColor = Color.FromArgb(255, 77, 127, 138);
                    _specularPower = 100d;
                    _emissiveColor = Color.FromArgb(64, 112, 94, 122);

                    _mass = 66666 * volumeInCubicMeters;

                    #endregion
                    break;

                default:
                    throw new ApplicationException("Unknown MineralType: " + _mineralType.ToString());
            }
        }

        /// <param name="intensity">brightness from 0 to 1</param>
        private Model3D GetRixiumTorusVisual(Vector3D location, double radius, double intensity, RotateTransform3D randomRotation)
        {
            // Material
            MaterialGroup material = new MaterialGroup();
            //material.Children.Add(new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(32, 44, 9, 82))));       // purple color
            material.Children.Add(new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(32, 30, 160, 189))));       // teal color
            //material.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromArgb(128, 104, 79, 130)), 100d));     // purple reflection
            material.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromArgb(128, 60, 134, 150)), 100d));

            byte emissiveAlpha = Convert.ToByte(140 * intensity);

            //material.Children.Add(new EmissiveMaterial(new SolidColorBrush(Color.FromArgb(64, 85, 50, 122))));
            material.Children.Add(new EmissiveMaterial(new SolidColorBrush(Color.FromArgb(emissiveAlpha, 85, 50, 122))));

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = material;
            geometry.BackMaterial = material;
            geometry.Geometry = _sharedVisuals.GetRixiumTorusMesh(radius);

            Transform3DGroup transforms = new Transform3DGroup();
            transforms.Children.Add(new TranslateTransform3D(location));
            if (randomRotation != null)
            {
                transforms.Children.Add(randomRotation);
            }
            //transforms.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 90)));
            geometry.Transform = transforms;

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
            double retVal = _sizeX;

            if (_sizeY > retVal)
            {
                retVal = _sizeY;
            }

            if (_sizeZ > retVal)
            {
                retVal = _sizeZ;
            }

            retVal *= .5d;     // I'm going for radius, not width

            return retVal;
        }

        #endregion
    }

    #region Enum: MineralType

    /// <summary>
    /// I tried to list these in order of value
    /// </summary>
    /// <remarks>
    /// The density is in kg/cu.m
    /// 1 carat is 200 mg (or .0002 kg)
    /// 
    /// TODO:  The $ are way out of whack to be useful in game
    /// </remarks>
    public enum MineralType
    {
        Custom,
        /// <summary>
        /// Density = 934
        /// </summary>
        Ice,
        /// <summary>
        /// $1.70 per metric ton
        /// $.0017 per kg
        /// Density = 7,900
        /// </summary>
        Iron,
        /// <summary>
        /// $70 per short ton
        /// $.08 per kg
        /// Density = 2,267
        /// </summary>
        /// <remarks>
        /// Can't use coal, because there's no way it would appear naturally in space
        /// </remarks>
        Graphite,
        /// <summary>
        /// $1,400 per oz
        /// $49,420 per kg
        /// Density = 19,300
        /// </summary>
        Gold,
        /// <summary>
        /// $1,700 per oz
        /// $59,840 per kg
        /// Density = 21,450
        /// </summary>
        Platinum,
        /// <summary>
        /// $250 per carat
        /// $1,250,000 per kg
        /// Density = 2,760
        /// </summary>
        Emerald,
        /// <summary>
        /// $1,000 per carat
        /// $5,000,000 per kg
        /// Density = 4,000
        /// </summary>
        Saphire,
        /// <summary>
        /// $2,500 per carat
        /// $12,500,000 per kg
        /// Density = 4,000
        /// </summary>
        Ruby,
        /// <summary>
        /// $15,000 per carat
        /// $75,000,000 per kg
        /// Density = 3,515
        /// </summary>
        Diamond,
        /// <summary>
        /// $300,000,000 per kg
        /// Density = 66,666
        /// </summary>
        Rixium
    }

    #endregion
}
