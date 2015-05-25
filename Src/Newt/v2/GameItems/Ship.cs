using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using Game.HelperClassesCore;
using Game.Newt.v2.GameItems.ShipEditor;
using Game.Newt.v2.GameItems.ShipParts;
using Game.HelperClassesWPF;
using Game.Newt.v2.NewtonDynamics;



using System.Diagnostics;
using Game.HelperClassesWPF.Primitives3D;

namespace Game.Newt.v2.GameItems
{
    //TODO: Make sure the part visuals and hulls and mass breakdowns are aligned (not one along X and one along Z)
    //TODO: Support parts getting damaged/destroyed/repaired (probably keep the parts around maybe 75% mass, with a charred visual)
    //TODO: Improve the logic that checks for parts being too far away
    //TODO: Check for thrusters/weapons burning parts.  For rigid, a scan can be done up front.  Once joints are used, inter body checks need to be done when the joint moves
    //		- Thrusters that are blocked need to have an offset/reduction to where the force is applied

    public class Ship : IDisposable, IMapObject, IPartUpdatable
    {
        #region Class: VisualEffects

        protected class VisualEffects
        {
            #region Declaration Section

            private readonly ItemOptions _itemOptions;

            private readonly List<Thruster> _thrusters;

            //private readonly DiffuseMaterial _materialThrust;

            /// <summary>
            /// Geometries will get added/removed from this, which will be seen from this.MiscVisual
            /// </summary>
            //private readonly Model3DGroup _miscGeometries;

            #endregion

            #region Constructor

            public VisualEffects(ItemOptions itemOptions, List<Thruster> thrusters)
            {
                _itemOptions = itemOptions;
                _thrusters = thrusters;

                // This isn't creating any real visuals right now, just setting up the framework
                //_materialThrust = new DiffuseMaterial(Brushes.Orange);
                this.ThrustVisual = new BillboardLine3DSet();
                this.ThrustVisual.Color = Colors.Orange;
                this.ThrustVisual.IsReflectiveColor = false;

                //_miscGeometries = new Model3DGroup();

                //ModelVisual3D model = new ModelVisual3D();
                //model.Content = _miscGeometries;
                //this.MiscVisual = model;
            }

            #endregion

            #region Public Properties

            //TODO: If other visuals are needed, put them here
            //public readonly Visual3D MiscVisual;

            public readonly BillboardLine3DSet ThrustVisual;

            #endregion

            #region Public Methods

            public void Refresh()
            {
                //_miscGeometries.Children.Clear();

                #region Thrust lines

                this.ThrustVisual.BeginAddingLines();

                foreach (Thruster thruster in _thrusters)
                {
                    // Look at the thrusts from the last firing
                    Vector3D?[] thrusts = thruster.FiredThrustsLastUpdate;
                    if (thrusts == null)
                    {
                        continue;
                    }

                    for (int cntr = 0; cntr < thrusts.Length; cntr++)
                    {
                        if (thrusts[cntr] != null)
                        {
                            // Get coords for visual (these are in model coords.  The visual will be transformed to world outside of this class)
                            var lineVect = GetThrustLine(thrusts[cntr].Value, _itemOptions.ThrusterStrengthRatio);		// this returns a vector in the opposite direction, so the line looks like a flame
                            Point3D lineStart = thruster.Position + (lineVect.Item1.ToUnit() * thruster.ThrustVisualStartRadius);

                            // Add it
                            this.ThrustVisual.AddLine(lineStart, lineStart + lineVect.Item1, lineVect.Item2);
                        }
                    }
                }

                this.ThrustVisual.EndAddingLines();

                #endregion
            }

            #endregion

            #region Private Methods

            private static Tuple<Vector3D, double> GetThrustLine(Vector3D force, double strengthRatio)
            {
                const double THICKATONE = .05d;		// the desired thickness when scaled forcelen is one
                const double MINTHICK = THICKATONE * .66d;		// thickness can't get less than this
                const double MAXTHICK = THICKATONE * 8d;
                const double THICKMULT = .08d;		// thickness grows/shrinks linearly based on this constant

                const double LENTHMULT = 16d;		// the length should be 4 when scaled forcelen is one

                // Scale the length of the force
                double forceLen = force.Length / (strengthRatio * ItemOptions.FORCESTRENGTHMULT);

                // Thickness [y=mx+b]
                double thickness = (THICKMULT * (forceLen - 1d)) + THICKATONE;
                if (thickness < MINTHICK)
                {
                    thickness = MINTHICK;
                }
                else if (thickness > MAXTHICK)
                {
                    thickness = MAXTHICK;
                }

                double length = Math.Sqrt(forceLen * LENTHMULT);

                // Exit Function
                return Tuple.Create(force.ToUnit() * (length * -1d), thickness);
            }

            #endregion
        }

        #endregion
        #region Class: PartContainerBuilding

        protected class PartContainerBuilding
        {
            // --------------- Individual Part Types
            public List<AmmoBox> Ammo = new List<AmmoBox>();
            public IContainer AmmoGroup = null;		// this is used by the converters to fill up the ammo boxes.  The logic to match ammo boxes with guns is more complex than a single group

            public List<FuelTank> Fuel = new List<FuelTank>();
            public IContainer FuelGroup = null;		// this will either be null, ContainerGroup, or a single FuelTank

            public List<EnergyTank> Energy = new List<EnergyTank>();
            public IContainer EnergyGroup = null;		// this will either be null, ContainerGroup, or a single EnergyTank

            public List<PlasmaTank> Plasma = new List<PlasmaTank>();
            public IContainer PlasmaGroup = null;     // this will either be null, ContainerGroup, or a single PlasmaTank

            public List<CargoBay> CargoBay = new List<CargoBay>();
            public CargoBayGroup CargoBayGroup = null;

            public List<ConverterMatterToFuel> ConvertMatterToFuel = new List<ConverterMatterToFuel>();
            public List<ConverterMatterToEnergy> ConvertMatterToEnergy = new List<ConverterMatterToEnergy>();
            public ConverterMatterGroup ConvertMatterGroup = null;

            public List<ConverterEnergyToAmmo> ConvertEnergyToAmmo = new List<ConverterEnergyToAmmo>();
            public List<ConverterEnergyToFuel> ConvertEnergyToFuel = new List<ConverterEnergyToFuel>();
            public List<ConverterFuelToEnergy> ConverterFuelToEnergy = new List<ConverterFuelToEnergy>();
            public List<ConverterRadiationToEnergy> ConvertRadiationToEnergy = new List<ConverterRadiationToEnergy>();

            public List<Thruster> Thrust = new List<Thruster>();
            public List<TractorBeam> TractorBeam = new List<TractorBeam>();

            public List<Brain> Brain = new List<Brain>();

            public List<SensorGravity> SensorGravity = new List<SensorGravity>();
            public List<SensorSpin> SensorSpin = new List<SensorSpin>();
            public List<SensorVelocity> SensorVelocity = new List<SensorVelocity>();

            public List<CameraColorRGB> CameraColorRGB = new List<CameraColorRGB>();

            public List<ProjectileGun> ProjectileGun = new List<ProjectileGun>();
            public List<BeamGun> BeamGun = new List<BeamGun>();

            public List<ShieldEnergy> ShieldEnergy = new List<ShieldEnergy>();
            public List<ShieldKinetic> ShieldKinetic = new List<ShieldKinetic>();
            public List<ShieldTractor> ShieldTractor = new List<ShieldTractor>();

            // --------------- Part Groups
            public PartBase[] AllParts = null;

            public IPartUpdatable[] UpdatableParts_MainThread = null;
            public IPartUpdatable[] UpdatableParts_AnyThread = null;

            public NeuralUtility.ContainerOutput[] Links = null;
            //public NeuralBucket LinkBucket = null;
        }

        #endregion
        #region Class: PartContainer

        protected class PartContainer
        {
            public PartContainer(PartContainerBuilding parts)
            {
                Ammo = parts.Ammo.ToArray();
                AmmoGroup = parts.AmmoGroup;

                Fuel = parts.Fuel.ToArray();
                FuelGroup = parts.FuelGroup;

                Energy = parts.Energy.ToArray();
                EnergyGroup = parts.EnergyGroup;

                Plasma = parts.Plasma.ToArray();
                PlasmaGroup = parts.PlasmaGroup;

                CargoBay = parts.CargoBay.ToArray();
                CargoBayGroup = parts.CargoBayGroup;

                ConvertMatterToFuel = parts.ConvertMatterToFuel.ToArray();
                ConvertMatterToEnergy = parts.ConvertMatterToEnergy.ToArray();
                ConvertMatterGroup = parts.ConvertMatterGroup;

                ConvertEnergyToAmmo = parts.ConvertEnergyToAmmo.ToArray();
                ConvertEnergyToFuel = parts.ConvertEnergyToFuel.ToArray();
                ConverterFuelToEnergy = parts.ConverterFuelToEnergy.ToArray();
                ConvertRadiationToEnergy = parts.ConvertRadiationToEnergy.ToArray();

                Thrust = parts.Thrust.ToArray();
                Brain = parts.Brain.ToArray();

                SensorGravity = parts.SensorGravity.ToArray();
                SensorSpin = parts.SensorSpin.ToArray();
                SensorVelocity = parts.SensorVelocity.ToArray();

                CameraColorRGB = parts.CameraColorRGB.ToArray();

                ProjectileGun = parts.ProjectileGun.ToArray();

                AllParts = parts.AllParts;

                UpdatableParts_MainThread = parts.UpdatableParts_MainThread;
                UpdatableParts_AnyThread = parts.UpdatableParts_AnyThread;

                Links = parts.Links;
                //LinkBucket = parts.LinkBucket;
            }

            // --------------- Individual Part Types
            public readonly AmmoBox[] Ammo;
            public readonly IContainer AmmoGroup;		// this is used by the converters to fill up the ammo boxes.  The logic to match ammo boxes with guns is more complex than a single group (there are other groups handed to the guns, but those aren't stored here)

            public readonly FuelTank[] Fuel;
            public readonly IContainer FuelGroup;		// this will either be null, ContainerGroup, or a single FuelTank

            public readonly EnergyTank[] Energy;
            public readonly IContainer EnergyGroup;		// this will either be null, ContainerGroup, or a single EnergyTank

            public readonly PlasmaTank[] Plasma;
            public readonly IContainer PlasmaGroup;     // this will either be null, ContainerGroup, or a single PlasmaTank

            public readonly CargoBay[] CargoBay;
            public readonly CargoBayGroup CargoBayGroup;

            public readonly ConverterMatterToFuel[] ConvertMatterToFuel;
            public readonly ConverterMatterToEnergy[] ConvertMatterToEnergy;
            public readonly ConverterMatterGroup ConvertMatterGroup;

            public readonly ConverterEnergyToAmmo[] ConvertEnergyToAmmo;
            public readonly ConverterEnergyToFuel[] ConvertEnergyToFuel;
            public readonly ConverterFuelToEnergy[] ConverterFuelToEnergy;
            public readonly ConverterRadiationToEnergy[] ConvertRadiationToEnergy;

            public readonly Thruster[] Thrust;
            public readonly Brain[] Brain;

            public readonly SensorGravity[] SensorGravity;
            public readonly SensorSpin[] SensorSpin;
            public readonly SensorVelocity[] SensorVelocity;

            public readonly CameraColorRGB[] CameraColorRGB;

            public readonly ProjectileGun[] ProjectileGun;

            // --------------- Part Groups
            public readonly PartBase[] AllParts;

            public readonly IPartUpdatable[] UpdatableParts_MainThread;
            public readonly IPartUpdatable[] UpdatableParts_AnyThread;

            public readonly NeuralUtility.ContainerOutput[] Links;
            //public readonly NeuralBucket LinkBucket;      // had to move this, because it's built later
        }

        #endregion
        #region Class: BuildPartsResults

        public class BuildPartsResults
        {
            public PartBase[] AllParts = null;
            public ShipPartDNA[] DNA = null;
            public CollisionHull[] Hulls = null;

            public IPartUpdatable[] UpdatableParts_MainThread = null;
            public IPartUpdatable[] UpdatableParts_AnyThread = null;

            public NeuralUtility.ContainerOutput[] Links = null;
        }

        #endregion
        #region Class: ShipConstruction

        protected class ShipConstruction
        {
            public EditorOptions Options { get; set; }
            public ItemOptions ItemOptions { get; set; }

            public RadiationField Radiation { get; set; }
            public IGravityField Gravity { get; set; }
            public CameraPool CameraPool { get; set; }

            public PartContainerBuilding Parts { get; set; }
            public ShipDNA DNA { get; set; }

            public Model3DGroup Model { get; set; }
            public VisualEffects VisualEffects { get; set; }

            public Body PhysicsBody { get; set; }

            public double Radius { get; set; }

            // -------------- Everything below are intermediate properties that are shared between tasks, but won't be needed by the constructor
            public CollisionHull[] Hulls = null;

            public MassMatrix MassMatrix;
            public Point3D CenterMass;
        }

        #endregion

        #region Declaration Section

        private readonly object _lockRecalculateMass = new object();        //NOTE: Currently, this is the only lock in the class.  If there is need for more in the future, consider having a larger global lock, or make very sure they don't conflict

        protected EditorOptions _options = null;
        protected ItemOptions _itemOptions = null;
        private ShipDNA _dna = null;

        private RadiationField _radiation = null;
        private IGravityField _gravity = null;
        private CameraPool _cameraPool = null;

        private readonly PartContainer _parts;
        private long _partUpdateCount_MainThread = -1;      // starting at -1 so that the first increment will bring it to 0 (skips use mod, so zero mod anything is zero, so everything will fire on the first tick)
        private long _partUpdateCount_AnyThread = -1;       // this is incremented through Interlocked, so doesn't need to be volatile

        /// <summary>
        /// This should be set to true if the act of calling one of the part's update has a chance of slowly changing the
        /// ship's mass (like thrusters using fuel, or matter converters)
        /// </summary>
        private readonly bool _hasMassChangingUpdatables_Small;
        /// <summary>
        /// This is used for ammo boxes
        /// </summary>
        private readonly bool _hasMassChangingUpdatables_Medium;

        private volatile object _lastMassRecalculateTime = 5d;
        private volatile object _nextMatterTransferTime = 5d;        // since it multiplies by elapsed time, don't do this on the first tick - elasped time is larger than normal

        private VisualEffects _visualEffects = null;

        /// <remarks>
        /// Adding to the neural pool requires taking a lock, which slows down everything on this main thread (creation of
        /// wpf objects seems to be pretty hard hit for some strange reason).
        /// 
        /// So by doing this on another thread, this thread is saved from that expense.  But I don't think this is the best solution.
        /// I have a feeling a bunch of threads on the thread pool will get clogged instead of just this one
        /// 
        /// Instead of a hard lock, use reader lock and writer lock
        /// </remarks>
        private readonly Task<NeuralBucket> _neuralPoolAddTask;
        private readonly NeuralBucket _linkBucket;

        #endregion

        #region Constructor/Factory

        public static async Task<Ship> GetNewShipAsync(EditorOptions options, ItemOptions itemOptions, ShipDNA dna, World world, int material_Ship, int material_Projectile, RadiationField radiation, IGravityField gravity, CameraPool cameraPool, Map map, bool runNeural, bool repairPartPositions)
        {
            var construction = await GetNewShipConstructionAsync(options, itemOptions, dna, world, material_Ship, material_Projectile, radiation, gravity, cameraPool, map, runNeural, repairPartPositions);
            return new Ship(construction);
        }

        protected static async Task<ShipConstruction> GetNewShipConstructionAsync(EditorOptions options, ItemOptions itemOptions, ShipDNA dna, World world, int material_Ship, int material_Projectile, RadiationField radiation, IGravityField gravity, CameraPool cameraPool, Map map, bool runNeural, bool repairPartPositions)
        {
            TaskScheduler currentContext = TaskScheduler.FromCurrentSynchronizationContext();

            ShipConstruction con = new ShipConstruction();

            con.Options = options;
            con.ItemOptions = itemOptions;
            con.Radiation = radiation;
            con.Gravity = gravity;
            con.CameraPool = cameraPool;

            // Parts
            con.Parts = new PartContainerBuilding();

            var dna_hulls = await BuildParts(con.Parts, dna, runNeural, repairPartPositions, world, options, itemOptions, radiation, gravity, cameraPool, map, material_Projectile);

            con.DNA = dna_hulls.Item1;
            con.Hulls = dna_hulls.Item2;

            // Mass breakdown
            GetInertiaTensorAndCenterOfMass_Points(out con.MassMatrix, out con.CenterMass, con.Parts.AllParts.ToArray(), con.DNA.PartsByLayer.Values.SelectMany(o => o).ToArray(), itemOptions.MomentOfInertiaMultiplier);

            #region WPF - main

            //TODO: Remember this so that flames and other visuals can be added/removed.  That way there will still only be one model visual
            //TODO: When joints are supported, some of the parts (or part groups) will move relative to the others.  There can still be a single model visual
            Model3DGroup models = new Model3DGroup();

            foreach (PartBase part in con.Parts.AllParts)
            {
                models.Children.Add(part.Model);
            }

            con.Model = models;

            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = models;

            #endregion
            #region WPF - effects

            // These visuals will only show up in the main viewport (they won't be shared with the camera pool, so won't be visible to
            // other ships)
            con.VisualEffects = new VisualEffects(itemOptions, con.Parts.Thrust);

            #endregion

            #region Physics Body

            // For now, just make a composite collision hull out of all the parts
            CollisionHull hull = CollisionHull.CreateCompoundCollision(world, 0, con.Hulls);

            con.PhysicsBody = new Body(hull, Matrix3D.Identity, 1d, new Visual3D[] { visual, con.VisualEffects.ThrustVisual });		// just passing a dummy value for mass, the real mass matrix is calculated later
            con.PhysicsBody.MaterialGroupID = material_Ship;
            con.PhysicsBody.LinearDamping = .01d;
            con.PhysicsBody.AngularDamping = new Vector3D(.01d, .01d, .01d);
            con.PhysicsBody.CenterOfMass = con.CenterMass;
            con.PhysicsBody.MassMatrix = con.MassMatrix;

            hull.Dispose();
            foreach (CollisionHull partHull in con.Hulls)
            {
                partHull.Dispose();
            }

            #endregion

            // Calculate radius
            Point3D aabbMin, aabbMax;
            con.PhysicsBody.GetAABB(out aabbMin, out aabbMax);
            con.Radius = (aabbMax - aabbMin).Length / 2d;

            return con;
        }

        protected Ship(ShipConstruction construction)
        {
            // Transfer properties from construction object
            _options = construction.Options;
            _itemOptions = construction.ItemOptions;

            _radiation = construction.Radiation;
            _gravity = construction.Gravity;
            _cameraPool = construction.CameraPool;

            _parts = new PartContainer(construction.Parts);
            _dna = construction.DNA;

            this.Model = construction.Model;
            _visualEffects = construction.VisualEffects;

            this.PhysicsBody = construction.PhysicsBody;

            this.Radius = construction.Radius;

            // Hook up events
            this.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(PhysicsBody_ApplyForceAndTorque);

            foreach (var part in _parts.AllParts)
            {
                part.RequestWorldLocation += new EventHandler<PartRequestWorldLocationArgs>(Part_RequestWorldLocation);
                part.RequestWorldSpeed += new EventHandler<PartRequestWorldSpeedArgs>(Part_RequestWorldSpeed);
            }

            // See if there are parts that can gradually change the ship's mass
            if ((_parts.Fuel.Length > 0 && _parts.Thrust.Length > 0) ||
                (_parts.CargoBay.Length > 0 && (_parts.ConvertMatterToEnergy.Length > 0 || _parts.ConvertMatterToFuel.Length > 0)) ||
                (_parts.Energy.Length > 0 && _parts.Fuel.Length > 0 && (_parts.ConvertEnergyToFuel.Length > 0 || _parts.ConverterFuelToEnergy.Length > 0))
                )
            {
                _hasMassChangingUpdatables_Small = true;
            }
            else
            {
                _hasMassChangingUpdatables_Small = false;
            }

            if (_parts.Ammo.Length > 0 && _parts.ProjectileGun.Length > 0)
            {
                _hasMassChangingUpdatables_Medium = true;
            }
            else
            {
                _hasMassChangingUpdatables_Medium = false;
            }

            // Set up a neural processor on its own thread/task
            if (_parts.Links != null)
            {
                var bucketTask = AddToNeuralPool(_parts.Links);
                _neuralPoolAddTask = bucketTask.Item1;
                _linkBucket = bucketTask.Item2;
            }

            this.ShouldRecalcMass_Large = false;
            this.ShouldRecalcMass_Small = false;

            this.CreationTime = DateTime.Now;
        }

        #endregion

        #region IDisposable Members

        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_parts != null)
                {
                    if (_linkBucket != null)
                    {
                        //NOTE: Since this is async, must make sure the remove is called only after the add finishes
                        _neuralPoolAddTask.ContinueWith(t =>
                        {
                            if (t.Result != null)
                            {
                                NeuralPool.Instance.Remove(t.Result);
                            }
                        });
                    }

                    if (_parts.AllParts != null)
                    {
                        foreach (PartBase part in _parts.AllParts)
                        {
                            part.Dispose();
                        }
                    }
                    //_parts = null;        // GetNewDNA expects this to be around
                }

                //NOTE: Map takes care of the standard part visuals
                //try
                //{
                //if (this.PhysicsBody != null)
                //{
                this.PhysicsBody.Dispose();
                //    this.PhysicsBody = null;      // leaving this instantiated.  Some properties may still be desired after dispose (like token)
                //}
                //}
                //catch (Exception) { }
            }
        }

        #endregion
        #region IMapObject Members

        public long Token
        {
            get
            {
                return this.PhysicsBody.Token;
            }
        }

        //TODO: In the future, there will be multiple bodies connected by joints
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
        #region IPartUpdatable Members

        private volatile object _ammoVolumeAtRecalc = null;

        public virtual void Update_MainThread(double elapsedTime)
        {
            _partUpdateCount_MainThread++;

            foreach (IPartUpdatable part in _parts.UpdatableParts_MainThread)      // Not bothering to call these in random order, because main thread should just be graphics
            {
                int skips = part.IntervalSkips_MainThread.Value;        // no need to check for null, nulls weren't added to the list
                if (skips > 0 && _partUpdateCount_MainThread % (skips + 1) != 0)
                {
                    continue;
                }

                part.Update_MainThread(elapsedTime + (elapsedTime * skips));        // if skips is greater than zero, then approximate how much time elapsed based on this tick's elapsed time
            }

            _visualEffects.Refresh();

            // Bump the age (doing it on the main thread to avoid the chance of thread conflicts)
            this.Age += elapsedTime;
        }
        public virtual void Update_AnyThread(double elapsedTime)
        {
            //NOTE: This method doesn't have a lock, so there could be a bit of reentry.  But the individual pieces should be ok with that

            double age = this.Age;
            long count = Interlocked.Increment(ref _partUpdateCount_AnyThread);

            #region Fill the converters

            if (_parts.ConvertMatterGroup != null && age >= (double)_nextMatterTransferTime)
            {
                if (_parts.ConvertMatterGroup.Transfer())
                {
                    this.ShouldRecalcMass_Large = true;
                }
                _nextMatterTransferTime = age + (elapsedTime * StaticRandom.Next(80, 120));      // no need to spin the processor unnecessarily each tick
            }

            #endregion

            #region Update Parts

            foreach (int index in UtilityCore.RandomRange(0, _parts.UpdatableParts_AnyThread.Length))
            {
                int skips = _parts.UpdatableParts_AnyThread[index].IntervalSkips_AnyThread.Value;        // no need to check for null, nulls weren't added to the list
                if (skips > 0 && count % (skips + 1) != 0)
                {
                    continue;
                }

                _parts.UpdatableParts_AnyThread[index].Update_AnyThread(elapsedTime + (elapsedTime * skips));       // if skips is greater than zero, then approximate how much time elapsed based on this tick's elapsed time
            }

            // Detect small change
            if (_hasMassChangingUpdatables_Small)
            {
                // There's a good chance that at least one of the parts changed the ship's mass a little bit
                this.ShouldRecalcMass_Small = true;
            }

            // Detect medium change
            if (_hasMassChangingUpdatables_Medium && !this.ShouldRecalcMass_Medium)
            {
                double? ammoVolumeAtRecalc = (double?)_ammoVolumeAtRecalc;
                double ammoMassCur = _parts.AmmoGroup.QuantityCurrent;

                if (ammoVolumeAtRecalc != null && !Math3D.IsNearValue(ammoVolumeAtRecalc.Value, ammoMassCur))
                {
                    this.ShouldRecalcMass_Medium = true;
                }
            }

            #endregion

            #region Recalc mass

            if (this.ShouldRecalcMass_Large)
            {
                RecalculateMass();
            }
            else if (this.ShouldRecalcMass_Medium && age > (double)_lastMassRecalculateTime + (elapsedTime * 200d))
            {
                RecalculateMass();
            }
            else if (this.ShouldRecalcMass_Small && age > (double)_lastMassRecalculateTime + (elapsedTime * 1000d))     //NOTE: This age check isn't atomic, so multiple threads could theoretically interfere, but I don't think it's worth worrying about (the RecalculateMass method itself is threadsafe)
            {
                RecalculateMass();
            }

            #endregion
        }

        public virtual int? IntervalSkips_MainThread
        {
            get
            {
                return 0;
            }
        }
        public virtual int? IntervalSkips_AnyThread
        {
            get
            {
                if (_parts.UpdatableParts_AnyThread.Length > 0)
                {
                    //TODO: Return UpdatableParts_AnyThread.Min
                    return 0;
                }
                else
                {
                    return null;
                }
            }
        }

        #endregion

        #region Public Properties

        //public double DryMass
        //{
        //    get;
        //    private set;
        //}
        //public double TotalMass
        //{
        //    get;
        //    private set;
        //}

        // These are exposed for debugging convienience.  Don't change their capacity, you'll mess stuff up
        public IContainer Ammo
        {
            get
            {
                return _parts.AmmoGroup;
            }
        }
        public IContainer Energy
        {
            get
            {
                return _parts.EnergyGroup;
            }
        }
        public IContainer Fuel
        {
            get
            {
                return _parts.FuelGroup;
            }
        }
        public IContainer Plasma
        {
            get
            {
                return _parts.PlasmaGroup;
            }
        }

        public CargoBayGroup CargoBays
        {
            get
            {
                return _parts.CargoBayGroup;
            }
        }

        // This is exposed for debugging
        public Thruster[] Thrusters
        {
            get
            {
                return _parts.Thrust;
            }
        }

        public IEnumerable<PartBase> Parts
        {
            get
            {
                return _parts.AllParts;
            }
        }

        // This is exposed for the ship viewer to be able to draw the links (the neurons are stored in the individual parts, but the links are stored at the ship level)
        public NeuralUtility.ContainerOutput[] NeuronLinks
        {
            get
            {
                return _parts.Links;
            }
        }

        public string Name
        {
            get
            {
                return _dna.ShipName;
            }
        }
        public string Lineage
        {
            get
            {
                return _dna.ShipLineage;
            }
        }
        public long Generation
        {
            get
            {
                return _dna.Generation;
            }
        }

        private volatile object _age = 0d;
        /// <summary>
        /// This is the age of the ship (sum of elapsed time).  This doesn't use datetime.now, it's all based on the elapsed time passed into update
        /// </summary>
        public double Age
        {
            get
            {
                return (double)_age;
            }
            private set
            {
                _age = value;
            }
        }

        private volatile bool _shouldRecalcMass_Large = false;
        /// <summary>
        /// This is used as a hint to recalcuate mass - set this when a large change occurs
        /// </summary>
        /// <remarks>
        /// Whenever cargo is added/removed/shifted around, this should be set to true.  Or any other large sudden changes
        /// in mass
        /// </remarks>
        protected bool ShouldRecalcMass_Large
        {
            get
            {
                return _shouldRecalcMass_Large;
            }
            set
            {
                _shouldRecalcMass_Large = value;
            }
        }

        private volatile bool _shouldRecalcMass_Medium = false;
        /// <summary>
        /// This is used as a hint to recalcuate mass - set this when a medium change occurs
        /// </summary>
        /// <remarks>
        /// Large recalcs mass the next tick.  Medium and Small recalc after a time (med in less time than small)
        /// </remarks>
        protected bool ShouldRecalcMass_Medium
        {
            get
            {
                return _shouldRecalcMass_Medium;
            }
            set
            {
                _shouldRecalcMass_Medium = value;
            }
        }

        private volatile bool _shouldRecalcMass_Small = false;
        /// <summary>
        /// This is used as a hint to recalcuate mass - set this when a small change occurs
        /// </summary>
        /// <remarks>
        /// This should be set when there are small changes in mass, like normal using up of fuel, or matter converters slowly consuming
        /// cargo
        /// </remarks>
        protected bool ShouldRecalcMass_Small
        {
            get
            {
                return _shouldRecalcMass_Small;
            }
            set
            {
                _shouldRecalcMass_Small = value;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// This is to manually update the mass matrix
        /// </summary>
        /// <remarks>
        /// NOTE: There is some expense to calling this, so avoid calling it too often
        /// </remarks>
        public void RecalculateMass()
        {
            lock (_lockRecalculateMass)
            {
                MassMatrix massMatrix;
                Point3D centerMass;
                GetInertiaTensorAndCenterOfMass_Points(out massMatrix, out centerMass, _parts.AllParts.ToArray(), _dna.PartsByLayer.Values.SelectMany(o => o).ToArray(), _itemOptions.MomentOfInertiaMultiplier);

                this.PhysicsBody.CenterOfMass = centerMass;
                this.PhysicsBody.MassMatrix = massMatrix;

                if (_parts.AmmoGroup != null)
                {
                    _ammoVolumeAtRecalc = _parts.AmmoGroup.QuantityCurrent;
                }

                _lastMassRecalculateTime = this.Age;
                this.ShouldRecalcMass_Large = false;
                this.ShouldRecalcMass_Medium = false;
                this.ShouldRecalcMass_Small = false;
            }

            OnMassRecalculated();
        }

        public virtual ShipDNA GetNewDNA()
        {
            // Create dna for each part using that part's current stats
            List<ShipPartDNA> dnaParts = new List<ShipPartDNA>();

            foreach (PartBase part in _parts.AllParts)
            {
                ShipPartDNA dna = part.GetNewDNA();

                if (_parts.Links != null && part is INeuronContainer)
                {
                    NeuralUtility.PopulateDNALinks(dna, (INeuronContainer)part, _parts.Links);
                }

                dnaParts.Add(dna);
            }

            // Build the ship dna
            ShipDNA retVal = ShipDNA.Create(_dna, dnaParts);

            // Exit Function
            return retVal;
        }

        public async static Task<BuildPartsResults> BuildParts_Finish(List<Tuple<PartBase, ShipPartDNA>> combined, ShipPartDNA[] usableParts, bool runNeural, bool repairPartPositions, ItemOptions itemOptions, World world)
        {
            BuildPartsResults retVal = new BuildPartsResults();

            #region Commit the lists

            // Store some different views of the parts

            // This is an array of all the parts
            retVal.AllParts = combined.Select(o => o.Item1).ToArray();

            // These are parts that need to have Update called each tick
            IPartUpdatable[] updatable = retVal.AllParts.Where(o => o is IPartUpdatable).Select(o => (IPartUpdatable)o).ToArray();
            retVal.UpdatableParts_MainThread = updatable.Where(o => o.IntervalSkips_MainThread != null).ToArray();
            retVal.UpdatableParts_AnyThread = updatable.Where(o => o.IntervalSkips_AnyThread != null).ToArray();

            #endregion

            #region Neural Links

            //NOTE: Doing this before moving the parts around in order to get more accurate linkage (the links store the position of the from container)
            if (runNeural)
            {
                retVal.Links = await Task.Run(() => LinkNeurons(combined.ToArray(), itemOptions));
            }

            #endregion

            // Move Parts (this must be done on the main thread)
            var hulls_dna = BuildParts_Move(retVal.AllParts, usableParts, repairPartPositions, world);
            retVal.DNA = hulls_dna.Item1;
            retVal.Hulls = hulls_dna.Item2;

            return retVal;
        }

        public static Tuple<Task<NeuralBucket>, NeuralBucket> AddToNeuralPool(NeuralUtility.ContainerOutput[] links)
        {
            // Create the bucket
            NeuralBucket bucket = new NeuralBucket(links.SelectMany(o => UtilityCore.Iterate(o.InternalLinks, o.ExternalLinks)).ToArray());

            // Add to the pool from another thread.  This way the lock while waiting to add won't tie up this thread
            Task<NeuralBucket> task = Task.Run(() =>
            {
                NeuralPool.Instance.Add(bucket);
                return bucket;
            });

            return Tuple.Create(task, bucket);
        }

        #endregion
        #region Protected Methods

        /// <summary>
        /// This gets called whenever the mass is recalculated
        /// NOTE: This will get called from arbitrary threads
        /// </summary>
        protected virtual void OnMassRecalculated()
        {
        }

        #endregion

        #region Event Listeners

        private void PhysicsBody_ApplyForceAndTorque(object sender, BodyApplyForceAndTorqueArgs e)
        {
            #region Thrusters

            //NOTE: The brain is running in another thread, so this method just applies the thrusters based on the current neural outputs

            foreach (Thruster thruster in _parts.Thrust)
            {
                // Look at the thrusts from the last firing
                Vector3D?[] thrusts = thruster.FiredThrustsLastUpdate;
                if (thrusts == null)
                {
                    continue;
                }

                for (int cntr = 0; cntr < thrusts.Length; cntr++)
                {
                    if (thrusts[cntr] != null)
                    {
                        // Apply force
                        Vector3D bodyForce = e.Body.DirectionToWorld(thrusts[cntr].Value);
                        Point3D bodyPoint = e.Body.PositionToWorld(thruster.Position);
                        e.Body.AddForceAtPoint(bodyForce, bodyPoint);
                    }
                }
            }

            #endregion

            #region Kick from guns

            foreach (ProjectileGun gun in _parts.ProjectileGun)
            {
                // Look at the kick from this gun firing (the vector builds up if multiple shots were fired between updates.
                // Calling GetKickLastUpdate clears the kick for next time)
                Vector3D? kick = gun.GetKickLastUpdate();

                if (kick == null)
                {
                    continue;
                }

                // Apply force
                Vector3D bodyForce = e.Body.DirectionToWorld(kick.Value);
                Point3D bodyPoint = e.Body.PositionToWorld(gun.Position);
                e.Body.AddForceAtPoint(bodyForce, bodyPoint);
            }

            #endregion
        }

        private void Part_RequestWorldLocation(object sender, PartRequestWorldLocationArgs e)
        {
            if (!(sender is PartBase))
            {
                throw new ApplicationException("Expected sender to be PartBase");
            }

            PartBase senderCast = (PartBase)sender;

            e.Orientation = this.PhysicsBody.Rotation.RotateBy(senderCast.Orientation);
            e.Position = this.PositionWorld + this.PhysicsBody.Rotation.GetRotatedVector(senderCast.Position.ToVector());
        }
        private void Part_RequestWorldSpeed(object sender, PartRequestWorldSpeedArgs e)
        {
            if (!(sender is PartBase))
            {
                throw new ApplicationException("Expected sender to be PartBase");
            }

            PartBase senderCast = (PartBase)sender;

            e.Velocity = this.PhysicsBody.Velocity;
            e.AngularVelocity = this.PhysicsBody.AngularVelocity;

            if (e.GetVelocityAtPoint != null)
            {
                e.VelocityAtPoint = this.PhysicsBody.GetVelocityAtPoint(this.PhysicsBody.Position + this.PhysicsBody.PositionToWorld(e.GetVelocityAtPoint.Value).ToVector());
            }
        }

        #endregion

        #region Private Methods

        private async static Task<Tuple<ShipDNA, CollisionHull[]>> BuildParts(PartContainerBuilding container, ShipDNA shipDNA, bool runNeural, bool repairPartPositions, World world, EditorOptions options, ItemOptions itemOptions, RadiationField radiation, IGravityField gravity, CameraPool cameraPool, Map map, int material_Projectile)
        {
            // Throw out parts that are too small
            ShipPartDNA[] usableParts = shipDNA.PartsByLayer.Values.SelectMany(o => o).Where(o => o.Scale.Length > .01d).ToArray();

            // Create the parts based on dna
            var combined = BuildParts_Create(container, usableParts, options, itemOptions, radiation, gravity, cameraPool, map, world, material_Projectile);

            BuildPartsResults results = await BuildParts_Finish(combined, usableParts, runNeural, repairPartPositions, itemOptions, world);

            container.AllParts = results.AllParts;
            container.UpdatableParts_MainThread = results.UpdatableParts_MainThread;
            container.UpdatableParts_AnyThread = results.UpdatableParts_AnyThread;
            container.Links = results.Links;

            //TODO: Preserve layers - actually, get rid of layers from dna.  It's just clutter
            ShipDNA retVal = ShipDNA.Create(shipDNA, results.DNA);
            return Tuple.Create(retVal, results.Hulls);
        }
        private static List<Tuple<PartBase, ShipPartDNA>> BuildParts_Create(PartContainerBuilding container, ShipPartDNA[] parts, EditorOptions options, ItemOptions itemOptions, RadiationField radiation, IGravityField gravity, CameraPool cameraPool, Map map, World world, int material_Projectile)
        {
            List<Tuple<PartBase, ShipPartDNA>> retVal = new List<Tuple<PartBase, ShipPartDNA>>();

            #region Containers

            // Containers need to be built up front
            foreach (ShipPartDNA dna in parts)
            {
                switch (dna.PartType)
                {
                    case AmmoBox.PARTTYPE:
                        BuildParts_Add(new AmmoBox(options, itemOptions, dna),
                            dna, container.Ammo, retVal);
                        break;

                    case FuelTank.PARTTYPE:
                        BuildParts_Add(new FuelTank(options, itemOptions, dna),
                            dna, container.Fuel, retVal);
                        break;

                    case EnergyTank.PARTTYPE:
                        BuildParts_Add(new EnergyTank(options, itemOptions, dna),
                            dna, container.Energy, retVal);
                        break;

                    case PlasmaTank.PARTTYPE:
                        BuildParts_Add(new PlasmaTank(options, itemOptions, dna),
                            dna, container.Plasma, retVal);
                        break;

                    case CargoBay.PARTTYPE:
                        BuildParts_Add(new CargoBay(options, itemOptions, dna),
                            dna, container.CargoBay, retVal);
                        break;
                }
            }

            //NOTE: The parts can handle being handed a null container.  It doesn't add much value to have parts that are dead
            //weight, but I don't want to penalize a design for having thrusters, but no fuel tank.  Maybe descendants will develop
            //fuel tanks and be a winning design

            // Build groups
            BuildParts_ContainerGroup(out container.FuelGroup, container.Fuel, Math3D.GetCenter(container.Fuel.Select(o => o.Position).ToArray()));
            BuildParts_ContainerGroup(out container.EnergyGroup, container.Energy, Math3D.GetCenter(container.Energy.Select(o => o.Position).ToArray()));
            BuildParts_ContainerGroup(out container.PlasmaGroup, container.Plasma, Math3D.GetCenter(container.Plasma.Select(o => o.Position).ToArray()));
            BuildParts_ContainerGroup(out container.AmmoGroup, container.Ammo, Math3D.GetCenter(container.Ammo.Select(o => o.Position).ToArray()), ContainerGroup.ContainerOwnershipType.QuantitiesCanChange);
            BuildParts_ContainerGroup(out container.CargoBayGroup, container.CargoBay);

            //TODO: Figure out which ammo boxes to put with guns.  These are some of the rules that should be considered, they are potentially competing
            //rules, so each rule should form its own links with a weight for each link.  Then choose the pairings with the highest weight (maybe use a bit of
            //randomness)
            //		- Group guns that are the same size
            //		- Pair up boxes and guns that are close together
            //		- Smaller guns should hook to smaller boxes

            //var gunsBySize = usableParts.Where(o => o.PartType == ProjectileGun.PARTTYPE).GroupBy(o => o.Scale.LengthSquared);

            #endregion
            #region Standard Parts

            foreach (ShipPartDNA dna in parts)
            {
                switch (dna.PartType)
                {
                    case AmmoBox.PARTTYPE:
                    case FuelTank.PARTTYPE:
                    case EnergyTank.PARTTYPE:
                    case PlasmaTank.PARTTYPE:
                    case CargoBay.PARTTYPE:
                        // These were built previously
                        break;

                    case ConverterMatterToFuel.PARTTYPE:
                        BuildParts_Add(new ConverterMatterToFuel(options, itemOptions, dna, container.FuelGroup),
                            dna, container.ConvertMatterToFuel, retVal);
                        break;

                    case ConverterMatterToEnergy.PARTTYPE:
                        BuildParts_Add(new ConverterMatterToEnergy(options, itemOptions, dna, container.EnergyGroup),
                            dna, container.ConvertMatterToEnergy, retVal);
                        break;

                    case ConverterEnergyToAmmo.PARTTYPE:
                        BuildParts_Add(new ConverterEnergyToAmmo(options, itemOptions, dna, container.EnergyGroup, container.AmmoGroup),
                            dna, container.ConvertEnergyToAmmo, retVal);
                        break;

                    case ConverterEnergyToFuel.PARTTYPE:
                        BuildParts_Add(new ConverterEnergyToFuel(options, itemOptions, dna, container.EnergyGroup, container.FuelGroup),
                            dna, container.ConvertEnergyToFuel, retVal);
                        break;

                    case ConverterFuelToEnergy.PARTTYPE:
                        BuildParts_Add(new ConverterFuelToEnergy(options, itemOptions, dna, container.FuelGroup, container.EnergyGroup),
                            dna, container.ConverterFuelToEnergy, retVal);
                        break;

                    case ConverterRadiationToEnergy.PARTTYPE:
                        BuildParts_Add(new ConverterRadiationToEnergy(options, itemOptions, (ConverterRadiationToEnergyDNA)dna, container.EnergyGroup, radiation),
                            dna, container.ConvertRadiationToEnergy, retVal);
                        break;

                    case Thruster.PARTTYPE:
                        BuildParts_Add(new Thruster(options, itemOptions, (ThrusterDNA)dna, container.FuelGroup),
                            dna, container.Thrust, retVal);
                        break;

                    case TractorBeam.PARTTYPE:
                        BuildParts_Add(new TractorBeam(options, itemOptions, dna, container.PlasmaGroup),
                            dna, container.TractorBeam, retVal);
                        break;

                    case Brain.PARTTYPE:
                        BuildParts_Add(new Brain(options, itemOptions, dna, container.EnergyGroup),
                            dna, container.Brain, retVal);
                        break;

                    case SensorGravity.PARTTYPE:
                        BuildParts_Add(new SensorGravity(options, itemOptions, dna, container.EnergyGroup, gravity),
                            dna, container.SensorGravity, retVal);
                        break;

                    case SensorSpin.PARTTYPE:
                        BuildParts_Add(new SensorSpin(options, itemOptions, dna, container.EnergyGroup),
                            dna, container.SensorSpin, retVal);
                        break;

                    case SensorVelocity.PARTTYPE:
                        BuildParts_Add(new SensorVelocity(options, itemOptions, dna, container.EnergyGroup),
                            dna, container.SensorVelocity, retVal);
                        break;

                    case CameraColorRGB.PARTTYPE:
                        BuildParts_Add(new CameraColorRGB(options, itemOptions, dna, container.EnergyGroup, cameraPool),
                            dna, container.CameraColorRGB, retVal);
                        break;

                    case ProjectileGun.PARTTYPE:
                        BuildParts_Add(new ProjectileGun(options, itemOptions, dna, map, world, material_Projectile),
                            dna, container.ProjectileGun, retVal);
                        break;

                    case BeamGun.PARTTYPE:
                        BuildParts_Add(new BeamGun(options, itemOptions, dna, container.PlasmaGroup),
                            dna, container.BeamGun, retVal);
                        break;

                    case ShieldEnergy.PARTTYPE:
                        BuildParts_Add(new ShieldEnergy(options, itemOptions, dna, container.PlasmaGroup),
                            dna, container.ShieldEnergy, retVal);
                        break;

                    case ShieldKinetic.PARTTYPE:
                        BuildParts_Add(new ShieldKinetic(options, itemOptions, dna, container.PlasmaGroup),
                            dna, container.ShieldKinetic, retVal);
                        break;

                    case ShieldTractor.PARTTYPE:
                        BuildParts_Add(new ShieldTractor(options, itemOptions, dna, container.PlasmaGroup),
                            dna, container.ShieldTractor, retVal);
                        break;

                    default:
                        throw new ApplicationException("Unknown dna.PartType: " + dna.PartType);
                }
            }

            #endregion

            #region Post Linkage

            // Distribute ammo boxes to guns based on the gun's caliber
            if (container.ProjectileGun.Count > 0 && container.Ammo.Count > 0)
            {
                ProjectileGun.AssignAmmoBoxes(container.ProjectileGun, container.Ammo);
            }

            // Link cargo bays with converters
            if (container.CargoBay.Count > 0)
            {
                IConverterMatter[] converters = UtilityCore.Iterate<IConverterMatter>(container.ConvertMatterToEnergy, container.ConvertMatterToFuel).ToArray();

                if (converters.Length > 0)
                {
                    container.ConvertMatterGroup = new ConverterMatterGroup(converters, container.CargoBayGroup);
                }
            }

            #endregion

            return retVal;
        }
        private static Tuple<ShipPartDNA[], CollisionHull[]> BuildParts_Move(PartBase[] allParts, ShipPartDNA[] usableParts, bool repairPartPositions, World world)
        {
            CollisionHull[] hulls = null;

            if (repairPartPositions)
            {
                bool changed1, changed2;

                PartSeparator_Part[] partWrappers = allParts.Select(o => new PartSeparator_Part(o)).ToArray();

                //TODO: Make a better version.  The PartSeparator should just expose one method, and do both internally
                PartSeparator.PullInCrude(out changed1, partWrappers);

                // Separate intersecting parts
                hulls = PartSeparator.Separate(out changed2, partWrappers, world);

                if (changed1 || changed2)
                {
                    usableParts = allParts.Select(o => o.GetNewDNA()).ToArray();
                }
            }
            else
            {
                hulls = allParts.Select(o => o.CreateCollisionHull(world)).ToArray();
            }

            return Tuple.Create(usableParts, hulls);
        }

        private static void BuildParts_ContainerGroup(out IContainer containerGroup, IEnumerable<IContainer> containers, Point3D center, ContainerGroup.ContainerOwnershipType? ownerType = null)
        {
            containerGroup = null;

            int count = containers.Count();

            if (count == 1)
            {
                containerGroup = containers.First();
            }
            else if (count > 1)
            {
                ContainerGroup group = new ContainerGroup();
                group.Ownership = ownerType ?? ContainerGroup.ContainerOwnershipType.GroupIsSoleOwner;		// this is the most efficient option
                foreach (IContainer container in containers)
                {
                    group.AddContainer(container);
                }

                containerGroup = group;
            }
        }
        private static void BuildParts_ContainerGroup(out CargoBayGroup cargoBayGroup, IEnumerable<CargoBay> cargoBays)
        {
            if (cargoBays.Count() == 0)
            {
                cargoBayGroup = null;
            }
            else
            {
                // There's no interface for cargo bay, so the other parts are hard coded to the group - this could be
                // changed to an interface in the future and made more efficient
                cargoBayGroup = new CargoBayGroup(cargoBays.ToArray());
            }
        }
        private static void BuildParts_Add<T>(T item, ShipPartDNA dna, List<T> specificList, List<Tuple<PartBase, ShipPartDNA>> combinedList) where T : PartBase
        {
            // This is just a helper method so one call adds to two lists
            specificList.Add(item);
            combinedList.Add(new Tuple<PartBase, ShipPartDNA>(item, dna));
        }

        private static NeuralUtility.ContainerOutput[] LinkNeurons(Tuple<PartBase, ShipPartDNA>[] parts, ItemOptions itemOptions)
        {
            #region Build up input args

            List<NeuralUtility.ContainerInput> inputs = new List<NeuralUtility.ContainerInput>();

            foreach (var part in parts.Where(o => o.Item1 is INeuronContainer))
            {
                INeuronContainer container = (INeuronContainer)part.Item1;
                ShipPartDNA dna = part.Item2;
                NeuralLinkDNA[] internalLinks = dna == null ? null : dna.InternalLinks;
                NeuralLinkExternalDNA[] externalLinks = dna == null ? null : dna.ExternalLinks;

                switch (container.NeuronContainerType)
                {
                    case NeuronContainerType.Sensor:
                        #region Sensor

                        // The sensor is a source, so shouldn't have any links.  But it needs to be included in the args so that other
                        // neuron containers can hook to it
                        inputs.Add(new NeuralUtility.ContainerInput(container, NeuronContainerType.Sensor, container.Position, container.Orientation, null, null, 0, null, null));

                        #endregion
                        break;

                    case NeuronContainerType.Brain:
                        #region Brain

                        int brainChemicalCount = 0;
                        if (part.Item1 is Brain)
                        {
                            brainChemicalCount = Convert.ToInt32(Math.Round(((Brain)part.Item1).BrainChemicalCount * 1.33d, 0));		// increasing so that there is a higher chance of listeners
                        }

                        inputs.Add(new NeuralUtility.ContainerInput(
                            container, NeuronContainerType.Brain,
                            container.Position, container.Orientation,
                            itemOptions.BrainLinksPerNeuron_Internal,
                            new Tuple<NeuronContainerType, NeuralUtility.ExternalLinkRatioCalcType, double>[]
							{
								Tuple.Create(NeuronContainerType.Sensor, NeuralUtility.ExternalLinkRatioCalcType.Smallest, itemOptions.BrainLinksPerNeuron_External_FromSensor),
								Tuple.Create(NeuronContainerType.Brain, NeuralUtility.ExternalLinkRatioCalcType.Average, itemOptions.BrainLinksPerNeuron_External_FromBrain),
								Tuple.Create(NeuronContainerType.Manipulator, NeuralUtility.ExternalLinkRatioCalcType.Smallest, itemOptions.BrainLinksPerNeuron_External_FromManipulator)
							},
                            brainChemicalCount,
                            internalLinks, externalLinks));

                        #endregion
                        break;

                    case NeuronContainerType.Manipulator:
                        #region Manipulator

                        inputs.Add(new NeuralUtility.ContainerInput(
                            container, NeuronContainerType.Manipulator,
                            container.Position, container.Orientation,
                            null,
                            new Tuple<NeuronContainerType, NeuralUtility.ExternalLinkRatioCalcType, double>[]
							{
								Tuple.Create(NeuronContainerType.Sensor, NeuralUtility.ExternalLinkRatioCalcType.Destination, itemOptions.ThrusterLinksPerNeuron_Sensor),
								Tuple.Create(NeuronContainerType.Brain, NeuralUtility.ExternalLinkRatioCalcType.Destination, itemOptions.ThrusterLinksPerNeuron_Brain),
							},
                            0,
                            null, externalLinks));

                        #endregion
                        break;

                    default:
                        throw new ApplicationException("Unknown NeuronContainerType: " + container.NeuronContainerType.ToString());
                }
            }

            #endregion

            // Create links
            NeuralUtility.ContainerOutput[] retVal = null;
            if (inputs.Count > 0)
            {
                retVal = NeuralUtility.LinkNeurons(inputs.ToArray(), itemOptions.NeuralLinkMaxWeight);
            }

            // Exit Function
            return retVal;
        }

        //TODO: Account for the invisible structural filler between the parts (probably just do a convex hull and give the filler a uniform density)
        private static void GetInertiaTensorAndCenterOfMass_Points(out MassMatrix matrix, out Point3D center, PartBase[] parts, ShipPartDNA[] dna, double inertiaMultiplier)
        {
            #region Prep work

            // Break the mass of the parts into pieces
            double cellSize = dna.Select(o => Math3D.Max(o.Scale.X, o.Scale.Y, o.Scale.Z)).Max() * .2d;		// break the largest object up into roughly 5x5x5
            UtilityNewt.IObjectMassBreakdown[] massBreakdowns = parts.Select(o => o.GetMassBreakdown(cellSize)).ToArray();

            double cellSphereMultiplier = (cellSize * .5d) * (cellSize * .5d) * .4d;		// 2/5 * r^2

            double[] partMasses = parts.Select(o => o.TotalMass).ToArray();
            double totalMass = partMasses.Sum();
            double totalMassInverse = 1d / totalMass;

            Vector3D axisX = new Vector3D(1d, 0d, 0d);
            Vector3D axisY = new Vector3D(0d, 1d, 0d);
            Vector3D axisZ = new Vector3D(0d, 0d, 1d);

            #endregion

            #region Ship's center of mass

            // Calculate the ship's center of mass
            double centerX = 0d;
            double centerY = 0d;
            double centerZ = 0d;
            for (int cntr = 0; cntr < massBreakdowns.Length; cntr++)
            {
                // Shift the part into ship coords
                //Point3D centerMass = parts[cntr].Position + massBreakdowns[cntr].CenterMass.ToVector();
                Point3D centerMass = parts[cntr].Position + parts[cntr].Orientation.GetRotatedVector(massBreakdowns[cntr].CenterMass.ToVector());

                centerX += centerMass.X * partMasses[cntr];
                centerY += centerMass.Y * partMasses[cntr];
                centerZ += centerMass.Z * partMasses[cntr];
            }

            center = new Point3D(centerX * totalMassInverse, centerY * totalMassInverse, centerZ * totalMassInverse);

            #endregion

            #region Local inertias

            // Get the local moment of inertia of each part for each of the three ship's axiis
            //TODO: If the number of cells is large, this would be a good candidate for running in parallel, but this method keeps cellSize pretty course
            Vector3D[] localInertias = new Vector3D[massBreakdowns.Length];
            for (int cntr = 0; cntr < massBreakdowns.Length; cntr++)
            {
                RotateTransform3D localRotation = new RotateTransform3D(new QuaternionRotation3D(parts[cntr].Orientation.ToReverse()));



                //TODO: Verify these results with the equation for the moment of inertia of a cylinder






                //NOTE: Each mass breakdown adds up to a mass of 1, so putting that mass back now (otherwise the ratios of masses between parts would be lost)
                localInertias[cntr] = new Vector3D(
                    GetInertia(massBreakdowns[cntr], localRotation.Transform(axisX), cellSphereMultiplier) * partMasses[cntr],
                    GetInertia(massBreakdowns[cntr], localRotation.Transform(axisY), cellSphereMultiplier) * partMasses[cntr],
                    GetInertia(massBreakdowns[cntr], localRotation.Transform(axisZ), cellSphereMultiplier) * partMasses[cntr]);
            }

            #endregion
            #region Global inertias

            // Apply the parallel axis theorem to each part
            double shipInertiaX = 0d;
            double shipInertiaY = 0d;
            double shipInertiaZ = 0d;
            for (int cntr = 0; cntr < massBreakdowns.Length; cntr++)
            {
                // Shift the part into ship coords
                Point3D partCenter = parts[cntr].Position + massBreakdowns[cntr].CenterMass.ToVector();

                shipInertiaX += GetInertia(partCenter, localInertias[cntr].X, partMasses[cntr], center, axisX);
                shipInertiaY += GetInertia(partCenter, localInertias[cntr].Y, partMasses[cntr], center, axisY);
                shipInertiaZ += GetInertia(partCenter, localInertias[cntr].Z, partMasses[cntr], center, axisZ);
            }

            #endregion

            // Newton wants the inertia vector to be one, so divide off the mass of all the parts <- not sure why I said they need to be one.  Here is a response from Julio Jerez himself:
            //this is the correct way
            //NewtonBodySetMassMatrix( priv->body, mass, mass * inertia[0], mass * inertia[1], mass * inertia[2] );
            //matrix = new MassMatrix(totalMass, new Vector3D(shipInertiaX * totalMassInverse, shipInertiaY * totalMassInverse, shipInertiaZ * totalMassInverse));
            matrix = new MassMatrix(totalMass, new Vector3D(shipInertiaX * inertiaMultiplier, shipInertiaY * inertiaMultiplier, shipInertiaZ * inertiaMultiplier));
        }

        /// <summary>
        /// This calculates the moment of inertia of the body around the axis (the axis goes through the center of mass)
        /// </summary>
        /// <remarks>
        /// Inertia of a body is the sum of all the mr^2
        /// 
        /// Each cell of the mass breakdown needs to be thought of as a sphere.  If it were a point mass, then for a body with only one
        /// cell, the mass would be at the center, and it would have an inertia of zero.  So by using the parallel axis theorem on each cell,
        /// the returned inertia is accurate.  The reason they need to thought of as spheres instead of cubes, is because the inertia is the
        /// same through any axis of a sphere, but not for a cube.
        /// 
        /// So sphereMultiplier needs to be 2/5 * cellRadius^2
        /// </remarks>
        private static double GetInertia(UtilityNewt.IObjectMassBreakdown body, Vector3D axis, double sphereMultiplier)
        {
            double retVal = 0d;

            // Cache this point in case the property call is somewhat expensive
            Point3D center = body.CenterMass;

            foreach (var pointMass in body)
            {
                if (pointMass.Item2 == 0d)
                {
                    continue;
                }

                // Tack on the inertia of the cell sphere (2/5*mr^2)
                retVal += pointMass.Item2 * sphereMultiplier;

                // Get the distance between this point and the axis
                double distance = Math3D.GetClosestDistance_Line_Point(body.CenterMass, axis, pointMass.Item1);

                // Now tack on the md^2
                retVal += pointMass.Item2 * distance * distance;
            }

            // Exit Function
            return retVal;
        }
        /// <summary>
        /// This returns the inertia of the part relative to the ship's axis
        /// NOTE: The other overload takes a vector that was transformed into the part's model coords.  The vector passed to this overload is in ship's model coords
        /// </summary>
        private static double GetInertia(Point3D partCenter, double partInertia, double partMass, Point3D shipCenterMass, Vector3D axis)
        {
            // Start with the inertia of the part around the axis passed in
            double retVal = partInertia;

            // Get the distance between the part and the axis
            double distance = Math3D.GetClosestDistance_Line_Point(shipCenterMass, axis, partCenter);

            // Now tack on the md^2
            retVal += partMass * distance * distance;

            // Exit Function
            return retVal;
        }

        private static void GetCenterMass(out Point3D center, out double mass, IEnumerable<PartBase> parts)
        {
            if (parts.Count() == 0)
            {
                center = new Point3D(0, 0, 0);
                mass = 0;
                return;
            }

            double x = 0;
            double y = 0;
            double z = 0;
            mass = 0;

            foreach (PartBase part in parts)
            {
                Point3D partPos = part.Position;
                double partMass = part.TotalMass;

                x += partPos.X * partMass;
                y += partPos.Y * partMass;
                z += partPos.Z * partMass;

                mass += partMass;
            }

            x /= mass;
            y /= mass;
            z /= mass;

            center = new Point3D(x, y, z);
        }

        #endregion
    }

    #region Class: ShipDNA

    //TODO: Make this derive from MapPartDNA
    public class ShipDNA
    {
        //TODO: Store mutation rates
        //TODO: Get rid of layers.  It's just tedious, and won't work with sub chassis anyway
        //TODO: Use arrays, not lists

        /// <summary>
        /// If the ship was created by a person, this will be a human friendly name of their choosing.  If the ship was randomly generated, this will
        /// probably just be a guid
        /// </summary>
        public string ShipName
        {
            get;
            set;
        }
        /// <summary>
        /// This is a way to give a lineage of ships the same or similar names
        /// </summary>
        /// <remarks>
        /// With asexual reproduction, just give the Gen0 ship a guid, and copy that same guid to all descendents
        /// 
        /// For multiple parents, there may not be a good way.  Maybe have some mashup algorithm, so if the parents are the same
        /// lineage/species, then the mashed string will be the same as the parents.  If the parents are similar, the string would also be
        /// similar, etc.
        /// 
        /// When the time comes for multiple parents, you may want another property that holds a family tree up to N steps back
        /// (but that tree will have N^2 nodes, so can't go too far back - even more if there are more than 2 parents at a time)
        /// </remarks>
        public string ShipLineage
        {
            get;
            set;
        }
        /// <summary>
        /// This gives a hint for how many ancestors this ship came from
        /// </summary>
        /// <remarks>
        /// For asexual reproduction, this is simple.  But for sexual, it's not so accurate - should probably either use parent's max + 1, or avg + 1
        /// </remarks>
        public long Generation
        {
            get;
            set;
        }

        public List<string> LayerNames
        {
            get;
            set;
        }
        public SortedList<int, List<ShipPartDNA>> PartsByLayer
        {
            get;
            set;
        }

        //TODO: Come up with a different name.  There is no difference between singular and plural
        //public SubChassisDNA[] SubChassis
        //{
        //    get;
        //    set;
        //}

        /// <summary>
        /// This is just a convenience if you don't care about the ship's name and layers (it always creates 1 layer)
        /// </summary>
        public static ShipDNA Create(IEnumerable<ShipPartDNA> parts)
        {
            return Create(Guid.NewGuid().ToString(), parts);
        }
        /// <summary>
        /// This is just a convenience if you don't care about the layers (it always creates 1 layer)
        /// </summary>
        public static ShipDNA Create(string name, IEnumerable<ShipPartDNA> parts)
        {
            ShipDNA retVal = new ShipDNA();

            retVal.ShipName = name;
            retVal.ShipLineage = Guid.NewGuid().ToString();		//this will probably be overwritten, but give it something unique if it's not
            retVal.Generation = 0;

            retVal.LayerNames = new string[] { "layer1" }.ToList();
            retVal.PartsByLayer = new SortedList<int, List<ShipPartDNA>>();
            retVal.PartsByLayer.Add(0, new List<ShipPartDNA>(parts));

            return retVal;
        }
        /// <summary>
        /// NOTE: Only the ship level properties are copied from prev
        /// TODO: Try to preserve layers (comparing prev.parts with parts)
        /// </summary>
        public static ShipDNA Create(ShipDNA prev, IEnumerable<ShipPartDNA> parts)
        {
            ShipDNA retVal = new ShipDNA();

            // Copy these from prev
            retVal.ShipName = prev.ShipName;
            retVal.ShipLineage = prev.ShipLineage;
            retVal.Generation = prev.Generation;

            // Copy these from parts
            retVal.LayerNames = new string[] { "layer1" }.ToList();
            retVal.PartsByLayer = new SortedList<int, List<ShipPartDNA>>();
            retVal.PartsByLayer.Add(0, new List<ShipPartDNA>(parts));

            return retVal;
        }
    }

    #endregion
    #region Class:  TODO: SubChassisDNA

    ///// <remarks>
    ///// This might be overkill:
    /////     The way newton uses joints, is it places two bodies in space, then the joint is created and told the pivot point.
    /////     
    /////     The way I think the DNA should be stored is each sub chassis is relative to the parent chassis, not all layed out
    /////     in world coords, then stapled together.
    /////     
    ///// Lots of experimentation will tell the best design
    ///// </remarks>
    //public struct JointAttachLocation
    //{
    //    public Point3D PointOnBody1;
    //    public Point3D PointOnJoint1;
    //    public Quaternion Orientation1;

    //    public Point3D PointOnBody2;
    //    public Point3D PointOnJoint2;
    //    public Quaternion Orientation2;
    //}

    //public class JointDNA
    //{
    //    public string JointType;

    //    public JointAttachLocation Location;

    //    // Not all joint types need all of these definitions, but including them all here so there's not a bunch of derived DNA classes for
    //    // different joint types
    //    public Point3D PivotPoint;
    //    public Vector3D Direction1;
    //    public Vector3D Direction2;
    //}

    ///// <summary>
    ///// Each subchassis is a rigid body (all parts tied to this chassis can't move relative to each other).  But there can be additional
    ///// subchassis children that move relative to this (the main shipdna instance acts like a root chassis)
    ///// </summary>
    ///// <remarks>
    ///// One failure of this design is that there is no way to make loops of chassis, only chains.  But I can't really think of a case where
    ///// having a floppy loop of parts makes sense
    ///// </remarks>
    //public class SubChassisDNA
    //{
    //    public JointDNA JointToParent;

    //    public PartDNA[] Parts;

    //    public SubChassisDNA[] SubChassis;
    //}

    #endregion
}
