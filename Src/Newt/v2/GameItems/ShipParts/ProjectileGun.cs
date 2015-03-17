using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v2.GameItems.MapParts;
using Game.Newt.v2.GameItems.ShipEditor;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.GameItems.ShipParts
{
    #region Class: ProjectileGunToolItem

    public class ProjectileGunToolItem : PartToolItemBase
    {
        #region Constructor

        public ProjectileGunToolItem(EditorOptions options)
            : base(options)
        {
            _visual2D = PartToolItemBase.GetVisual2D(this.Name, this.Description, options.EditorColors);
            this.TabName = PartToolItemBase.TAB_SHIPPART;
        }

        #endregion

        #region Public Properties

        public override string Name
        {
            get
            {
                return "Projectile Weapon";
            }
        }
        public override string Description
        {
            get
            {
                return "Fires projectiles from ammo box";
            }
        }
        public override string Category
        {
            get
            {
                return PartToolItemBase.CATEGORY_WEAPON;
            }
        }

        private UIElement _visual2D = null;
        public override UIElement Visual2D
        {
            get
            {
                return _visual2D;
            }
        }

        #endregion

        #region Public Methods

        public override PartDesignBase GetNewDesignPart()
        {
            return new ProjectileGunDesign(this.Options);
        }

        #endregion
    }

    #endregion
    #region Class: ProjectileGunDesign

    public class ProjectileGunDesign : PartDesignBase
    {
        #region Declaration Section

        public const double RADIUSPERCENTOFSCALE = .12d;

        public const PartDesignAllowedScale ALLOWEDSCALE = PartDesignAllowedScale.XYZ;		// This is here so the scale can be known through reflection

        private Tuple<UtilityNewt.IObjectMassBreakdown, Vector3D, double> _massBreakdown = null;

        #endregion

        #region Constructor

        public ProjectileGunDesign(EditorOptions options)
            : base(options) { }

        #endregion

        #region Public Properties

        public override PartDesignAllowedScale AllowedScale
        {
            get
            {
                return ALLOWEDSCALE;
            }
        }
        public override PartDesignAllowedRotation AllowedRotation
        {
            get
            {
                return PartDesignAllowedRotation.X_Y_Z;
            }
        }

        private Model3DGroup _geometries = null;
        public override Model3D Model
        {
            get
            {
                if (_geometries == null)
                {
                    _geometries = CreateGeometry(false);
                }

                return _geometries;
            }
        }

        #endregion

        #region Public Methods

        public override Model3D GetFinalModel()
        {
            return CreateGeometry(true);
        }

        public override CollisionHull CreateCollisionHull(WorldBase world)
        {
            Transform3DGroup transform = new Transform3DGroup();
            //transform.Children.Add(new ScaleTransform3D(this.Scale));		// it ignores scale
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90)));		// the physics hull is along x, but dna is along z
            transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(this.Orientation)));
            transform.Children.Add(new TranslateTransform3D(this.Position.ToVector()));

            Vector3D scale = this.Scale;
            double radius = RADIUSPERCENTOFSCALE * ((scale.X + scale.Y) * .5d);
            double height = scale.Z;

            return CollisionHull.CreateCylinder(world, 0, radius, height, transform.Value);
        }

        public override UtilityNewt.IObjectMassBreakdown GetMassBreakdown(double cellSize)
        {
            if (_massBreakdown != null && _massBreakdown.Item2 == this.Scale && _massBreakdown.Item3 == cellSize)
            {
                // This has already been built for this size
                return _massBreakdown.Item1;
            }

            // Convert this.Scale into a size that the mass breakdown will use (mass breakdown wants height along X, and scale is for radius, but the mass breakdown wants diameter
            // Reducing Z a bit, because the energy tank has a rounded cap
            Vector3D size = new Vector3D(this.Scale.Z, this.Scale.X * RADIUSPERCENTOFSCALE * 2d, this.Scale.Y * RADIUSPERCENTOFSCALE * 2d);

            // Cylinder
            UtilityNewt.ObjectMassBreakdown cylinder = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Cylinder, UtilityNewt.MassDistribution.Uniform, size, cellSize);

            Transform3D transform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90));		// the physics hull is along x, but dna is along z

            // Rotated
            UtilityNewt.ObjectMassBreakdownSet combined = new UtilityNewt.ObjectMassBreakdownSet(
                new UtilityNewt.ObjectMassBreakdown[] { cylinder },
                new Transform3D[] { transform });

            // Store this
            _massBreakdown = new Tuple<UtilityNewt.IObjectMassBreakdown, Vector3D, double>(combined, this.Scale, cellSize);

            // Exit Function
            return _massBreakdown.Item1;
        }

        #endregion

        #region Private Methods

        private Model3DGroup CreateGeometry(bool isFinal)
        {
            Model3DGroup retVal = new Model3DGroup();

            GeometryModel3D geometry;
            MaterialGroup material;
            DiffuseMaterial diffuse;
            SpecularMaterial specular;

            //int domeSegments = isFinal ? 2 : 10;
            int cylinderSegments = isFinal ? 6 : 35;

            #region Mount Box

            geometry = new GeometryModel3D();
            material = new MaterialGroup();
            diffuse = new DiffuseMaterial(new SolidColorBrush(WorldColors.GunBase));
            this.MaterialBrushes.Add(new MaterialColorProps(diffuse, WorldColors.GunBase));
            material.Children.Add(diffuse);
            specular = WorldColors.GunBaseSpecular;
            this.MaterialBrushes.Add(new MaterialColorProps(specular));
            material.Children.Add(specular);

            if (!isFinal)
            {
                EmissiveMaterial selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
                material.Children.Add(selectionEmissive);
                this.SelectionEmissives.Add(selectionEmissive);
            }

            geometry.Material = material;
            geometry.BackMaterial = material;

            geometry.Geometry = UtilityWPF.GetCube(new Point3D(-.115, -.1, -.5), new Point3D(.115, .1, -.1));

            retVal.Children.Add(geometry);

            #endregion

            #region Barrel

            geometry = new GeometryModel3D();
            material = new MaterialGroup();
            diffuse = new DiffuseMaterial(new SolidColorBrush(WorldColors.GunBarrel));
            this.MaterialBrushes.Add(new MaterialColorProps(diffuse, WorldColors.GunBarrel));
            material.Children.Add(diffuse);
            specular = WorldColors.GunBarrelSpecular;
            this.MaterialBrushes.Add(new MaterialColorProps(specular));
            material.Children.Add(specular);

            if (!isFinal)
            {
                EmissiveMaterial selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
                material.Children.Add(selectionEmissive);
                this.SelectionEmissives.Add(selectionEmissive);
            }

            geometry.Material = material;
            geometry.BackMaterial = material;

            const double OUTERRADIUS = .045;
            const double INNERRADIUS = .04;

            List<TubeRingBase> rings = new List<TubeRingBase>();

            rings.Add(new TubeRingRegularPolygon(0, false, OUTERRADIUS * 1.75d, OUTERRADIUS * 1.75d, false));		// Start at the base of the barrel
            rings.Add(new TubeRingRegularPolygon(.49, false, OUTERRADIUS * 1.75d, OUTERRADIUS * 1.75d, false));
            rings.Add(new TubeRingRegularPolygon(.01, false, OUTERRADIUS, OUTERRADIUS, false));
            rings.Add(new TubeRingRegularPolygon(.4, false, OUTERRADIUS, OUTERRADIUS, false));
            rings.Add(new TubeRingRegularPolygon(.01, false, OUTERRADIUS * 1.25d, OUTERRADIUS * 1.25d, false));
            rings.Add(new TubeRingRegularPolygon(.08, false, OUTERRADIUS * 1.25d, OUTERRADIUS * 1.25d, false));		// This is the tip of the barrel
            rings.Add(new TubeRingRegularPolygon(0, false, INNERRADIUS, INNERRADIUS, false));		// Curl to the inside
            rings.Add(new TubeRingRegularPolygon(-.95d, false, INNERRADIUS, INNERRADIUS, false));		// Loop back to the base

            geometry.Geometry = UtilityWPF.GetMultiRingedTube(cylinderSegments, rings, true, false, new TranslateTransform3D(0, 0, -.49d));

            retVal.Children.Add(geometry);

            #endregion

            #region Trim

            geometry = new GeometryModel3D();
            material = new MaterialGroup();
            diffuse = new DiffuseMaterial(new SolidColorBrush(WorldColors.GunTrim));
            this.MaterialBrushes.Add(new MaterialColorProps(diffuse, WorldColors.GunTrim));
            material.Children.Add(diffuse);
            specular = WorldColors.GunTrimSpecular;
            this.MaterialBrushes.Add(new MaterialColorProps(specular));
            material.Children.Add(specular);

            if (!isFinal)
            {
                EmissiveMaterial selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
                material.Children.Add(selectionEmissive);
                this.SelectionEmissives.Add(selectionEmissive);
            }

            geometry.Material = material;
            geometry.BackMaterial = material;

            geometry.Geometry = UtilityWPF.GetCube(new Point3D(-.095, -.11, -.45), new Point3D(.095, .11, -.15));

            retVal.Children.Add(geometry);

            #endregion

            // Transform
            retVal.Transform = GetTransformForGeometry(isFinal);

            // Exit Function
            return retVal;
        }

        #endregion
    }

    #endregion
    #region Class: ProjectileGun

    public class ProjectileGun : PartBase, INeuronContainer, IPartUpdatable
    {
        #region Class: GunGroup

        private class GunGroup
        {
            public GunGroup(ProjectileGun[] guns, double caliber, double demandPerGun)
            {
                this.Guns = guns;
                this.Caliber = caliber;
                this.DemandPerGun = demandPerGun;
                this.DemandTotal = demandPerGun * guns.Length;
            }

            public readonly ProjectileGun[] Guns;

            /// <summary>
            /// This is the caliber to set the gun to
            /// </summary>
            public readonly double Caliber;

            /// <summary>
            /// This is how much one gun will pull from the ammo box
            /// </summary>
            public readonly double DemandPerGun;
            /// <summary>
            /// This is DemandPerGun * Guns.Length
            /// </summary>
            /// <remarks>
            /// Ideally, each gun in the group will fire at the same time, so the sum of their demand should be considered the demand of the group
            /// </remarks>
            public readonly double DemandTotal;
        }

        #endregion
        #region Class: GunAmmoGroup

        private class GunAmmoGroup
        {
            public GunAmmoGroup(GunGroup guns, AmmoBox[] ammo, int firings)
            {
                this.Guns = guns;
                this.Ammo = ammo;
                this.Firings = firings;
            }

            public readonly GunGroup Guns;
            public readonly AmmoBox[] Ammo;

            /// <summary>
            /// Each firing fires all guns
            /// </summary>
            public readonly int Firings;

            //public readonly double Error;     // not sure if there is a need for error here
        }

        #endregion
        #region Class: ProjectileProps

        /// <summary>
        /// This holds several props based on caliber.  Storing them in a class so it's a single volatile
        /// </summary>
        private class ProjectileProps
        {
            public ProjectileProps(double caliber, ItemOptions options)
            {
                this.Caliber = caliber;

                this.AmountToPull = GetAmmoVolume(caliber);

                double projectileRadiusActual = caliber / 2d;

                this.Radius = projectileRadiusActual * options.ProjectileRadiusRatio;

                //NOTE: The mass of the projectile is a sphere, but the volume pull out of the ammo box is a cube (using the
                //same density).  The difference can be assumed to be propellent
                this.Mass = (4d / 3d) * Math.PI * projectileRadiusActual * projectileRadiusActual * projectileRadiusActual * options.AmmoDensity;

                //TODO: options (bigger should go slightly slower)
                this.Speed = 15;

                //TODO: The times shouldn't be a straight linear.  There should be a min, then sort of a sqrt dropoff (see thrust line drawing for an example)
                //_projectileMaxAge = Scale * options.mult
                this.MaxAge = 3.5;
            }

            public readonly double Caliber;

            public readonly double AmountToPull;

            public readonly double Radius;
            public readonly double Mass;

            public readonly double Speed;
            public readonly double MaxAge;
        }

        #endregion

        #region Declaration Section

        public const string PARTTYPE = "ProjectileGun";

        private readonly ItemOptions _itemOptions;

        private readonly World _world;
        private readonly Map _map;
        private readonly int _material_Projectile;

        private volatile ProjectileProps _projectileProps = null;     // this gets set in the constructor, so no need to get more elaborite than null here

        private readonly double _timeBetweenShots;
        private double _timeSinceLastShot = 0;

        /// <summary>
        /// There is always only one neuron
        /// </summary>
        /// <remarks>
        /// There are three ways to use the gun:
        ///     Directly calling the fire method - rate of fire would be ignored
        ///     Setting Percents, which will get looked at each update tick
        ///     Using these neurons, which will get looked at each update tick
        /// </remarks>
        private readonly Neuron_ZeroPos[] _neurons;


        private readonly Vector3D _kickDirectionUnit;



        #endregion

        #region Constructor

        public ProjectileGun(EditorOptions options, ItemOptions itemOptions, PartDNA dna, Map map, World world, int material_Projectile)
            : base(options, dna)
        {
            _itemOptions = itemOptions;
            _world = world;
            _map = map;
            _material_Projectile = material_Projectile;

            _neurons = new[] { new Neuron_ZeroPos(new Point3D(0, 0, 0)) };

            this.Design = new ProjectileGunDesign(options);
            this.Design.SetDNA(dna);

            double volume, radius, barrelRadius;
            GetMass(out _mass, out volume, out radius, out barrelRadius, out _scaleActual, dna, itemOptions);

            this.Radius = radius;

            RotateTransform3D transform = new RotateTransform3D(new QuaternionRotation3D(dna.Orientation));
            _kickDirectionUnit = transform.Transform(new Vector3D(0, 0, -1));

            // Caliber range (diameter of bullet)
            double caliber = barrelRadius * 2 * _itemOptions.ProjectileWeaponCaliberRatio;

            double range = (caliber * _itemOptions.ProjectileWeaponCaliberRangePercent) / 2d;
            _caliberRange = Tuple.Create(caliber - range, caliber + range);

            this.Caliber = caliber;     //NOTE: Setting this will populate _projectileProps

            //TODO: allow .33 second bursts with a 1.5 second delay
            //NOTE: This should probably be based on the selected caliber, but it's easier to just calculated it once based on the gun's scale
            //_timeBetweenShots = inverse of: Scale * options.mult
            _timeBetweenShots = .75;
        }

        #endregion

        #region INeuronContainer Members

        public IEnumerable<INeuron> Neruons_Readonly
        {
            get
            {
                return Enumerable.Empty<INeuron>();
            }
        }
        public IEnumerable<INeuron> Neruons_ReadWrite
        {
            get
            {
                return Enumerable.Empty<INeuron>();
            }
        }
        public IEnumerable<INeuron> Neruons_Writeonly
        {
            get
            {
                return _neurons;
            }
        }

        public IEnumerable<INeuron> Neruons_All
        {
            get
            {
                return _neurons;
            }
        }

        public NeuronContainerType NeuronContainerType
        {
            get
            {
                return NeuronContainerType.Manipulator;
            }
        }

        public double Radius
        {
            get;
            private set;
        }

        public bool IsOn
        {
            get
            {
                // Even though gun exposes neurons to control it, that's just an interface to the gun, so there's
                // no reason to draw from an energy tank to power those neurons.  (this IsOn property is from the
                // perspective of a brain, that's why it's independant of how much ammo is available - you can always
                // try to tell the gun to fire, that's when it will check for ammo)
                return true;
            }
        }

        #endregion
        #region IPartUpdatable Members

        public void Update_MainThread(double elapsedTime)
        {
            //NOTE: Thruster has this logic in AnyThread.  If gun did that, it would need to create a threadsafe queue of bullets for the ship to pop and generate the bullets
            //during its main thread.  That would have an advantage of getting world position once per tick.  Also, map wouldn't need to be passed down this far.  But a
            //queue would have a greater chance of buildup, and two bullets generated in the same place at the same time (very unlikely though).

            // See if enough time has passed for another shot to be fired
            _timeSinceLastShot += elapsedTime;

            if (_timeSinceLastShot >= _timeBetweenShots)
            {
                #region Possibly Fire

                // Get the decision to fire from manual override or neuron
                bool? manualFire = this.ShouldFire;
                double percent;

                if (manualFire != null)      // If this.ShouldFire is populated, it overrides.  Otherwise, use the neuron
                {
                    percent = manualFire.Value ? 1d : 0d;
                }
                else
                {
                    percent = _neurons[0].Value;        // there's only one neuron
                }

                Vector3D? kickResults = null;

                // See if it should fire
                if (percent > .9d)      // using a pretty high threshold so that random noise doesn't cause the bot to be trigger happy
                {
                    kickResults = Fire();
                    _timeSinceLastShot = 0;
                }

                // Add kick.  The update method will clear the kick
                if (kickResults != null)
                {
                    lock (_kickLock)        // technically, these should be on the same thread, but it makes the code safe to migrate
                    {
                        if (_kickLastUpdate == null)
                        {
                            _kickLastUpdate = kickResults;
                        }
                        else
                        {
                            // Already some from another shot.  Build it up
                            _kickLastUpdate = _kickLastUpdate.Value + kickResults.Value;
                        }
                    }
                }

                #endregion
            }
        }
        public void Update_AnyThread(double elapsedTime)
        {
        }

        public int? IntervalSkips_MainThread
        {
            get
            {
                return 0;
            }
        }
        public int? IntervalSkips_AnyThread
        {
            get
            {
                return null;
            }
        }

        #endregion

        #region Public Properties

        private readonly double _mass;
        public override double DryMass
        {
            get
            {
                return _mass;
            }
        }
        public override double TotalMass
        {
            get
            {
                return _mass;
            }
        }

        private readonly Vector3D _scaleActual;
        public override Vector3D ScaleActual
        {
            get
            {
                return _scaleActual;
            }
        }

        private readonly Tuple<double, double> _caliberRange;
        /// <summary>
        /// This is the volume of shot that can be fired
        /// </summary>
        /// <remarks>
        /// Originally, I was going to only have one allowed caliber, but if a ship has a pair of guns and slightly
        /// mutates the size of one of the guns, then two ammo boxes would be required.  By giving a small range,
        /// it's easier to match ammo boxes with guns
        /// </remarks>
        public Tuple<double, double> CaliberRange
        {
            get
            {
                return _caliberRange;
            }
        }

        public double AmountToPull
        {
            get
            {
                var props = _projectileProps;
                if (props == null)
                {
                    throw new InvalidOperationException("Can't use this property until the caliber has been set");
                }

                return props.AmountToPull;
            }
        }

        /// <summary>
        /// This is the caliber that the gun will actually fire
        /// </summary>
        public double Caliber
        {
            get
            {
                var props = _projectileProps;
                if (props == null)
                {
                    throw new InvalidOperationException("Can't use this property until the caliber has been set");
                }

                return props.Caliber;
            }
            set
            {
                if (value < _caliberRange.Item1)
                {
                    throw new ArgumentOutOfRangeException(string.Format("Caliber is less than min allowed {0}, {1}", value.ToString(), _caliberRange.Item1));
                }
                else if (value > _caliberRange.Item2)
                {
                    throw new ArgumentOutOfRangeException(string.Format("Caliber is greater than max allowed {0}, {1}", value.ToString(), _caliberRange.Item2));
                }

                _projectileProps = new ProjectileProps(value, _itemOptions);
            }
        }

        private volatile IContainer _ammo = null;
        public IContainer Ammo
        {
            get
            {
                return _ammo;
            }
            set
            {
                if (value != null)
                {
                    double amountToPull = this.AmountToPull;

                    if (!Math3D.IsNearValue(value.RemovalMultiple, amountToPull))
                    {
                        throw new ArgumentException(string.Format("The ammo boxes passed in must have RemovalMultiple equal to this.AmountToPull (Use ProjectileGun.AssignAmmoBoxes to distribute ammo boxes to guns)\r\nAmountToPull={0}\r\nAmmo.RemovalMultiple={1}", amountToPull.ToString(), value.RemovalMultiple.ToString()));
                    }
                }

                _ammo = value;
            }
        }

        private volatile object _shouldFire = null;
        /// <summary>
        /// This allows the gun to be fired manually.  If this is non null, it will override the current value of the neuron
        /// NOTE: This is looked at during each update.  The rate of fire is still imposed
        /// </summary>
        public bool? ShouldFire
        {
            get
            {
                return (bool?)_shouldFire;
            }
            set
            {
                _shouldFire = value;
            }
        }

        private readonly object _kickLock = new object();
        /// <summary>
        /// This is only populated by the update method when a projectile is fired
        /// </summary>
        /// <remarks>
        /// This is exposed so the ship can apply forces
        /// </remarks>
        private Vector3D? _kickLastUpdate = null;

        #endregion

        #region Public Methods

        public Vector3D Fire()
        {
            #region Pull ammo

            var projProps = _projectileProps;
            if (projProps == null)
            {
                // The caliber hasn't been set yet (a default is set in the constructor, so this should never happen)
                return new Vector3D(0, 0, 0);
            }

            IContainer ammo = this.Ammo;
            if (ammo == null)
            {
                // No ammo
                return new Vector3D(0, 0, 0);
            }

            // Pull from ammo
            if(ammo.RemoveQuantity(projProps.AmountToPull, true) > 0)
            {
                // Not enough ammo
                return new Vector3D(0, 0, 0);
            }

            #endregion

            #region Get position/direction

            // Model coords
            Point3D modelPosition = this.Design.Position;

            double length = this.Design.Scale.Z / 2d;
            length *= 3;     // make it a bit longer so it doesn't collide with the gun
            Vector3D modelDirection = new Vector3D(0, 0, length);

            // World coords
            var worldLoc = GetWorldLocation();
            var worldSpeed = GetWorldSpeed(null);

            Vector3D worldDirection = worldLoc.Item2.GetRotatedVector(modelDirection);
            Vector3D worldDirectionUnit = worldDirection.ToUnit();

            Point3D worldPosition = worldLoc.Item1 + worldDirection;

            #endregion

            // Create the projectile
            Projectile projectile = new Projectile(projProps.Radius, projProps.Mass, worldPosition, _world, _material_Projectile, _itemOptions.ProjectileColor, projProps.MaxAge, _map);

            projectile.PhysicsBody.Velocity = worldSpeed.Item1 + (worldDirectionUnit * projProps.Speed);
            projectile.PhysicsBody.AngularVelocity = worldDirectionUnit * 5;        // add a little spin along the direction of travel (emulate rifling)

            _map.AddItem(projectile);

            // Calculate Kick
            //TODO: May want to add a bit extra to emulate the expanding gas pushing the gun bag after the bullet leaves the barrel
            double mult = 1000;
            //return -worldDirectionUnit * projProps.Speed * projProps.Mass * mult;
            return _kickDirectionUnit * projProps.Speed * projProps.Mass * mult;
        }

        /// <summary>
        /// NOTE: The act of calling this clears the kick
        /// </summary>
        public Vector3D? GetKickLastUpdate()
        {
            lock(_kickLock)
            {
                Vector3D? retVal = _kickLastUpdate;
                _kickLastUpdate = null;
                return retVal;
            }
        }

        public static void AssignAmmoBoxes(IEnumerable<ProjectileGun> guns, IEnumerable<AmmoBox> boxes)
        {
            var gunCombos = GetPossibleGroupings(guns);

            var gunAmmoGroupings = GetAmmoGroupings(gunCombos, boxes);

            // Choose the best set - some combination of most guns used with the most even spread of firings
            GunAmmoGroup[] best = GetBestAmmoAssignment(gunAmmoGroupings);

            // Lock the ammo boxes and guns into the calibers, distribute ammo boxes to guns
            foreach (GunAmmoGroup group in best)
            {
                IContainer container;
                if (group.Ammo.Length == 1)
                {
                    container = group.Ammo[0];
                }
                else
                {
                    // More than one.  Make a group to manage them (no need to expose this group to the caller, it's just a convenience
                    // that manages pulling ammo from multiple boxes)
                    ContainerGroup combined = new ContainerGroup();
                    combined.Ownership = ContainerGroup.ContainerOwnershipType.QuantitiesCanChange;

                    foreach (AmmoBox ammo in group.Ammo)
                    {
                        combined.AddContainer(ammo);
                    }

                    container = combined;
                }

                container.RemovalMultiple = group.Guns.DemandPerGun;

                foreach (ProjectileGun gun in group.Guns.Guns)
                {
                    gun.Caliber = group.Guns.Caliber;
                    gun.Ammo = container;
                }
            }
        }

        #endregion

        #region Private Methods - gun combos

        #region Class: GunMajorCombo

        private class GunMajorCombo
        {
            public GunMajorCombo(GunCombo[] combos)
            {
                this.Combos = combos;
            }

            public readonly GunCombo[] Combos;

            public override string ToString()
            {
                return string.Join("  |  ", this.Combos.Select(o => o.ToString()));
            }
        }

        #endregion
        #region Class: GunCombo

        /// <summary>
        /// Each instance of this represents a set of sets.  That set of sets represents all guns, no repeats
        /// </summary>
        /// <remarks>
        /// Example, output from UtilityCore.AllCombosEnumerator with a 4 gun major set:
        ///		0,1,2,3
        ///		0,1,2
        ///		0,1,3
        ///		0,2,3
        ///		1,2,3
        ///		0,1
        ///		0,2
        ///		0,3
        ///		1,2
        ///		1,3
        ///		2,3
        ///		0
        ///		1
        ///		2
        ///		3
        /// 
        /// Becomes:
        /// GunCombo
        ///     0,1,2,3
        ///     
        /// GunCombo
        ///     0,1,2
        ///     3
        ///     
        /// GunCombo
        ///     0,1,3
        ///     2
        ///     
        /// GunCombo
        ///     0,2,3
        ///     1
        ///     
        /// GunCombo
        ///     1,2,3
        ///     0
        ///     
        /// GunCombo
        ///     0,1
        ///     2,3
        ///     
        /// GunCombo
        ///     0,1
        ///     2
        ///     3
        ///     
        /// etc....
        /// </remarks>
        private class GunCombo
        {
            public GunCombo(Tuple<int, ProjectileGun>[][] guns)
            {
                this.Guns = guns;

                //NOTE: This is making the assumption that there are no gaps in caliber across the guns passed in
                this.Calibers = guns.Select(o => GetRange(o)).ToArray();
            }

            public readonly Tuple<int, ProjectileGun>[][] Guns;

            public readonly Tuple<double, double>[] Calibers;

            //Debugging without this tostring is maddening :D
            public override string ToString()
            {
                return string.Join(" - ", this.Guns.Select(o => string.Join(",", o.Select(p => p.Item1.ToString()))));
            }

            /// <summary>
            /// This returns the range that all guns can support
            /// </summary>
            /// <remarks>
            /// Say gun0 can go from 1 to 3
            /// and gun1 can go from 2 to 4
            /// 
            /// The return would be 2 to 3
            /// 
            /// If there's a gap, an exception is thrown
            /// </remarks>
            private static Tuple<double, double> GetRange(Tuple<int, ProjectileGun>[] guns)
            {
                double? max = null;
                double? min = null;

                for (int cntr = 0; cntr < guns.Length; cntr++)
                {
                    Tuple<double, double> range = guns[cntr].Item2.CaliberRange;

                    // Min
                    if (min == null)
                    {
                        min = range.Item1;
                    }
                    else if (range.Item1 > min.Value)
                    {
                        min = range.Item1;
                    }

                    // Max
                    if (max == null)
                    {
                        max = range.Item2;
                    }
                    else if (range.Item2 < max.Value)
                    {
                        max = range.Item2;
                    }
                }

                if (min == null || max == null)
                {
                    throw new ArgumentException("There were no guns passed in");
                }

                // Verify
                if (guns.Any(o => o.Item2.CaliberRange.Item1 > max.Value) || guns.Any(o => o.Item2.CaliberRange.Item2 < min.Value))
                {
                    throw new ArgumentException("There is a gap in caliber ranges");
                }

                return Tuple.Create(min.Value, max.Value);
            }
        }

        #endregion

        /// <summary>
        /// This returns a list of possible groupings
        /// </summary>
        /// <remarks>
        /// return[0] - a combination of guns
        /// return[1] - a different combination of guns
        /// etc
        /// 
        /// NOTE: return[0] will have exactly one of each of the guns
        /// return[1] will have exactly one each
        /// etc
        /// </remarks>
        private static GunGroup[][] GetPossibleGroupings(IEnumerable<ProjectileGun> guns)
        {
            ProjectileGun[] singles = guns.ToArray();

            // Build groups that can be the same caliber
            int[][] majorSets = GetMajorSets(singles);

            // Generate combinations of guns, each group set to a specific caliber
            return GetAllSetCombos(majorSets, singles);
        }

        private static int[][] GetMajorSets(ProjectileGun[] guns)
        {
            if (guns.Length == 1)
            {
                return new[] { new[] { 0 } };
            }

            // Get all the min/max boundries of the guns
            IEnumerable<double> boundries = guns.
                SelectMany(o => new[] { o.CaliberRange.Item1, o.CaliberRange.Item2 }).
                Distinct().
                OrderBy(o => o);

            // Go through each boundry, and see which guns straddle it
            List<int[]> gunsWithBoundry = new List<int[]>();

            foreach (double boundry in boundries)
            {
                int[] matches = Enumerable.Range(0, guns.Length).
                    Where(o => guns[o].CaliberRange.Item1 <= boundry && guns[o].CaliberRange.Item2 >= boundry).
                    OrderBy(o => o).
                    ToArray();

                gunsWithBoundry.Add(matches);
            }

            // Dedupe, remove subsets
            GetMajorSets_Consolidate(gunsWithBoundry);

            return gunsWithBoundry.ToArray();
        }
        private static void GetMajorSets_Consolidate(List<int[]> sets)
        {
            int index = 0;

            while (index < sets.Count)
            {
                bool shouldRemove = false;

                for (int cntr = 0; cntr < sets.Count; cntr++)
                {
                    if (cntr == index)
                    {
                        // Same item
                        continue;
                    }

                    if (IsSame(sets[index], sets[cntr]))
                    {
                        shouldRemove = true;
                        break;
                    }
                    else if (IsSubset(sets[index], sets[cntr]))
                    {
                        shouldRemove = true;
                        break;
                    }
                }

                if (shouldRemove)
                {
                    sets.RemoveAt(index);
                }
                else
                {
                    index++;
                }
            }
        }

        private static bool IsSame(int[] set1, int[] set2)
        {
            if (set1.Length != set2.Length)
            {
                return false;
            }

            for (int cntr = 0; cntr < set1.Length; cntr++)
            {
                //NOTE: This assumes that the sets are sorted the same
                if (set1[cntr] != set2[cntr])
                {
                    return false;
                }
            }

            return true;
        }
        private static bool IsSubset(int[] smaller, int[] larger)
        {
            if (smaller.Length >= larger.Length)
            {
                return false;
            }

            for (int cntr = 0; cntr < smaller.Length; cntr++)
            {
                if (!larger.Contains(smaller[cntr]))
                {
                    return false;
                }
            }

            return true;
        }

        private static GunGroup[][] GetAllSetCombos(int[][] majorSets, ProjectileGun[] guns)
        {
            const int MAXRETURN = 1000;

            // Get all combos within each major set
            GunMajorCombo[] intermediate = GetAllSetCombos_Intermediate(majorSets, guns);

            // Each element of intermediate is all combos of a major set.  So now, generate all combos of intermediate
            GunCombo[][] allCombos = IterateChain(intermediate);

            // Each element of allCombos is a unique arrangement of groups of guns.  Now need to come up with the caliber that the
            // groups will use
            //
            // Could have:
            //      All Min
            //      All Max
            //      All Avg
            //
            // --- and/or ---
            //
            //      Each group Rnd
            //
            // If allCombos.Length < X, then go nuts.  Otherwise, take randoms, and limit to a total of Y


            List<GunGroup[]> retVal = new List<GunGroup[]>();

            retVal.AddRange(ConvertSets(allCombos, o => o.Item1));     // min
            retVal.AddRange(ConvertSets(allCombos, o => o.Item1));     // max
            retVal.AddRange(ConvertSets(allCombos, o => (o.Item1 + o.Item2) / 2));     // avg

            Random rand = StaticRandom.GetRandomForThread();
            for (int cntr = 0; cntr < 10; cntr++)
            {
                retVal.AddRange(ConvertSets(allCombos, o => rand.NextDouble(o.Item1, o.Item2)));     // rand
            }

            if (retVal.Count <= MAXRETURN)
            {
                return retVal.ToArray();
            }
            else
            {
                // Too many, return a random subset
                return UtilityCore.RandomRange(0, retVal.Count, MAXRETURN).
                    Select(o => retVal[o]).
                    ToArray();
            }
        }
        private static GunMajorCombo[] GetAllSetCombos_Intermediate(int[][] majorSets, ProjectileGun[] guns)
        {
            GunMajorCombo[] retVal = new GunMajorCombo[majorSets.Length];

            for (int cntr = 0; cntr < retVal.Length; cntr++)
            {
                int[] set = majorSets[cntr];

                int[][] permutations = UtilityCore.AllCombosEnumerator(set.Length).
                    Select(o => o.Select(p => set[p]).ToArray()).       //core.combos returns 0 based combinations.  Use those as indices into set
                    ToArray();

                retVal[cntr] = new GunMajorCombo(GetAllSetCombos_Intermediate_Set(permutations, guns));
            }

            return retVal;
        }
        private static GunCombo[] GetAllSetCombos_Intermediate_Set(int[][] permutations, ProjectileGun[] guns)
        {
            List<GunCombo> retVal = new List<GunCombo>();

            int numGuns = permutations.SelectMany(o => o).Distinct().Count();

            if (numGuns == 1)
            {
                int index = permutations[0][0];
                return new[] { new GunCombo(new[] { new[] { Tuple.Create(index, guns[index]) } }) };
            }

            for (int outer = 0; outer < permutations.Length - 1; outer++)
            {
                int? currentSecondary = null;

                // There could be multiple combos that start with this outer, so keep looping until none are found
                while (true)
                {
                    List<int[]> blocks = new List<int[]>();

                    blocks.Add(permutations[outer]);

                    int innerStart = currentSecondary == null ? outer + 1 : currentSecondary.Value + 1;

                    for (int inner = innerStart; inner < permutations.Length; inner++)
                    {
                        // See if this block is unique to the blocks that are being built up
                        if (permutations[inner].All(o => !blocks.Any(p => p.Contains(o))))
                        {
                            if (blocks.Count == 1)
                            {
                                currentSecondary = inner;
                            }

                            blocks.Add(permutations[inner]);
                        }
                    }

                    if (blocks.SelectMany(o => o).Count() == numGuns)
                    {
                        retVal.Add(new GunCombo(blocks.Select(o => o.Select(p => Tuple.Create(p, guns[p])).ToArray()).ToArray()));
                    }

                    if (blocks.Count == 1)
                    {
                        // No secondaries for this outer were found, so quit looking
                        break;
                    }
                }
            }

            return retVal.ToArray();
        }

        /// <summary>
        /// This is a recursive method
        /// </summary>
        private static GunCombo[][] IterateChain(GunMajorCombo[] chain)
        {
            // Get all combos of the links to the right of the the first link
            GunCombo[][] rightChain = null;
            if (chain.Length > 1)
            {
                rightChain = IterateChain(chain.Skip(1).ToArray());
            }

            GunMajorCombo link = chain[0];

            List<GunCombo[]> retVal = new List<GunCombo[]>();

            // Iterate over all the items in the first link
            for (int outer = 0; outer < link.Combos.Length; outer++)
            {
                List<GunCombo> set = new List<GunCombo>();

                set.Add(link.Combos[outer]);

                if (rightChain != null)
                {
                    for (int inner = 0; inner < rightChain.Length; inner++)
                    {
                        set.AddRange(rightChain[inner]);
                    }
                }

                retVal.Add(set.ToArray());
            }

            return retVal.ToArray();
        }

        private static GunGroup[][] ConvertSets(GunCombo[][] combos, Func<Tuple<double, double>, double> caliberFunc)
        {
            GunGroup[][] retVal = new GunGroup[combos.Length][];

            for (int outer = 0; outer < combos.Length; outer++)
            {
                List<GunGroup> set = new List<GunGroup>();

                foreach (GunCombo combo in combos[outer])
                {
                    for (int inner = 0; inner < combo.Guns.Length; inner++)
                    {
                        double caliber = caliberFunc(combo.Calibers[inner]);

                        ProjectileGun[] guns = combo.Guns[inner].Select(o => o.Item2).ToArray();

                        double demand = GetAmmoVolume(caliber);

                        set.Add(new GunGroup(guns, caliber, demand));
                    }
                }

                retVal[outer] = set.ToArray();
            }

            return retVal;
        }

        #endregion
        #region Private Methods - assign ammo boxes

        private static GunAmmoGroup[][] GetAmmoGroupings(GunGroup[][] gunCombos, IEnumerable<AmmoBox> boxes)
        {
            double smallestDemand = gunCombos[0].Min(o => o.DemandPerGun);      // only need to look at one of the combos, because each combo has all guns
            double smallestCaliber = gunCombos[0].Min(o => o.Caliber);

            // Commit the boxes to an array that will be reused for each combination of guns
            AmmoBox[] sortedBoxes = boxes.
                Where(o => o.QuantityMax >= smallestDemand && Math3D.Min(o.ScaleActual.X, o.ScaleActual.Y, o.ScaleActual.Z) >= smallestCaliber).        // only keep what will fit the smallest gun
                OrderByDescending(o => o.QuantityMax).      // order the boxes descending so that the largest gets assigned first
                ToArray();

            // Get the distance between each ammo box and each gun
            Tuple<long, long, double>[] ammo_gun_distance = gunCombos[0].
                SelectMany(o => o.Guns).
                SelectMany(gun => sortedBoxes.Select(box => Tuple.Create(box.Token, gun.Token, (box.Position - gun.Position).Length))).
                ToArray();

            return gunCombos.
                AsParallel().
                Select(o => GetAmmoGroupings_Assign(o, sortedBoxes, ammo_gun_distance)).       // Assign all the boxes to this arrangment of guns
                ToArray();
        }
        private static GunAmmoGroup[] GetAmmoGroupings_Assign(IEnumerable<GunGroup> guns, AmmoBox[] boxesDescending, Tuple<long, long, double>[] ammo_gun_distance)
        {
            // Build a structure that can be added to
            var retVal = guns.
                Select(o => new { Gun = o, Boxes = new List<AmmoBox>() }).
                ToArray();

            for (int cntr = 0; cntr < boxesDescending.Length; cntr++)
            {
                double boxVolume = boxesDescending[cntr].QuantityMax;
                double boxMinDimension = Math3D.Min(boxesDescending[cntr].ScaleActual.X, boxesDescending[cntr].ScaleActual.Y, boxesDescending[cntr].ScaleActual.Z);
                long boxToken = boxesDescending[cntr].Token;

                // Find the gun that most needs this box
                var bestMatch = retVal.
                    Where(o => o.Gun.DemandPerGun <= boxVolume && o.Gun.Caliber <= boxMinDimension).       // only look at guns that could use this box
                    Select(o =>
                    {
                        var capacityFirings = Get_Capacity_Firings(o.Boxes, o.Gun);

                        // Add up the distance between the box and all the guns in this group
                        double distance = o.Gun.Guns.Sum(p => ammo_gun_distance.First(q => q.Item1 == boxToken && q.Item2 == p.Token).Item3);

                        return new
                        {
                            Item = o,
                            Capacity = capacityFirings.Item1,       // the current amount of capacity this gun has
                            Firings = capacityFirings.Item2,     // how many shots can be fired based on this capacity
                            Distance = distance,
                        };
                    }).
                    //TODO: Figure out how to come up with a scoring system that takes distance into account
                    OrderBy(o => o.Firings).       // order by guns with the smallest amount of available shots
                    ThenByDescending(o => o.Item.Gun.DemandTotal).       // if there's a tie, choose the greediest gun
                    First();

                // Give this box to the chosen set of guns
                bestMatch.Item.Boxes.Add(boxesDescending[cntr]);
            }

            return retVal.
                Select(o => new GunAmmoGroup(o.Gun, o.Boxes.ToArray(), Convert.ToInt32(Math.Floor(Get_Capacity_Firings(o.Boxes, o.Gun).Item2)))).
                ToArray();
        }
        private static Tuple<double, double> Get_Capacity_Firings(List<AmmoBox> boxes, GunGroup gun)
        {
            double capacity = 0;
            double firings = 0;

            if (boxes.Count > 0)
            {
                capacity = boxes.Sum(p => p.QuantityMax);
                firings = capacity / gun.DemandTotal;
            }

            return Tuple.Create(capacity, firings);
        }

        /// <summary>
        /// This finds the best entry out of the list passed int
        /// </summary>
        /// <param name="groupings">
        /// Item1=a set of guns + a set of boxes
        /// Item2=the number of boxes that couldn't be matched
        /// </param>
        private static GunAmmoGroup[] GetBestAmmoAssignment(GunAmmoGroup[][] groupings)
        {
            //TODO: May want to come up with some kind of scoring system instead of just sorts

            var best = groupings.
                // Count the number guns that don't have ammo boxes assigned
                OrderBy(o => o.Sum(p => p.Ammo.Length == 0 ? 1 : 0)).
                // Don't just want the most possible shots, or you would get a small gun tied to a large box.  But want the largest average amount.
                //TODO: Instead of a simple average, give preference to the smallest variance.  That way, all the guns run out at the same time
                ThenByDescending(o => o.Average(p => p.Firings)).
                // If there's a tie, choose the arrangement that does the most damage per shot
                ThenByDescending(o => o.Average(p => p.Guns.Caliber)).
                First();

            return best;
        }

        #endregion
        #region Private Methods

        private static void GetMass(out double mass, out double volume, out double radius, out double barrelRadius, out Vector3D actualScale, PartDNA dna, ItemOptions itemOptions)
        {
            // Just assume it's a cylinder
            double radX = dna.Scale.X * .5 * ProjectileGunDesign.RADIUSPERCENTOFSCALE;
            double radY = dna.Scale.Y * .5 * ProjectileGunDesign.RADIUSPERCENTOFSCALE;
            double height = dna.Scale.Z;

            barrelRadius = (radX + radY) / 2d;

            radius = (radX + radY + (height * .5d)) / 3d;		// this is just an approximation for the neural container

            actualScale = new Vector3D(radX * 2d, radY * 2d, height);

            volume = Math.PI * radX * radY * height;

            mass = volume * itemOptions.ProjectileWeaponDensity;
        }

        /// <summary>
        /// Assume that each bullet is a sphere
        /// </summary>
        private static double GetAmmoVolume(double caliber)
        {
            double radius = caliber / 2;
            double mult = 5;        // when the multiplier is one, the ammo box is barely depleted

            return (4d / 3d) * Math.PI * radius * radius * radius * mult;
        }

        #endregion
    }

    #endregion
}
