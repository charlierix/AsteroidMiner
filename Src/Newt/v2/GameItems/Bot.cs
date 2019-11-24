using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v2.GameItems.ShipEditor;
using Game.Newt.v2.GameItems.ShipParts;
using Game.Newt.v2.NewtonDynamics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Game.Newt.v2.GameItems
{
    //TODO: Make sure the part visuals and hulls and mass breakdowns are aligned (not one along X and one along Z)
    //TODO: Support parts getting damaged/destroyed/repaired (probably keep the parts around maybe 75% mass, with a charred visual)
    //TODO: Improve the logic that checks for parts being too far away
    //TODO: Check for thrusters/weapons burning parts.  For rigid, a scan can be done up front.  Once joints are used, inter body checks need to be done when the joint moves
    //		- Thrusters that are blocked need to have an offset/reduction to where the force is applied (because the thrust is reflecting off that object)

    /// <summary>
    /// This is a rewrite of Ship (eventually, ship will be removed)
    /// </summary>
    public class Bot : IDisposable, IMapObject, IPartUpdatable, ITakesDamage
    {
        #region Events

        /// <summary>
        /// NOTE: This can be raised from any thread
        /// </summary>
        public event EventHandler MassChanged = null;

        #endregion

        #region Declaration Section

        private readonly object _lockRecalculateMass = new object();        //NOTE: Currently, this is the only lock in the class.  If there is need for more in the future, consider having a larger global lock, or make very sure they don't conflict

        protected readonly EditorOptions _options;
        protected readonly ItemOptions _itemOptions;
        private readonly ShipDNA _dna;
        private readonly ShipPartDNA[] _dnaParts;

        protected readonly RadiationField _radiation = null;
        protected readonly IGravityField _gravity = null;
        private readonly CameraPool _cameraPool = null;

        private readonly BotConstruction_Parts _parts;
        private readonly IPartUpdatable[] _updatableParts_MainThread;
        private readonly IPartUpdatable[] _updatableParts_AnyThread;
        private long _partUpdateCount_MainThread = -1;      // starting at -1 so that the first increment will bring it to 0 (skips use mod, so zero mod anything is zero, so everything will fire on the first tick)
        private long _partUpdateCount_AnyThread = -1;       // this is incremented through Interlocked, so doesn't need to be volatile

        /// <summary>
        /// These are used to transform from ship coords to the part's model coords
        /// </summary>
        private readonly Transform3D[] _partTransformToModel;

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

        private volatile object _ammoVolumeAtRecalc = null;

        private Ship.VisualEffects _visualEffects = null;

        private readonly NeuralBucket _linkBucket;
        // Only one of these two will be populated (or none), never both
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
        private readonly NeuralPool_ManualTick _neuralPoolManualTick;

        private readonly LifeEventWatcher _lifeEvents;

        #endregion

        #region Constructor

        public Bot(BotConstruction_Result construction)
        {
            _options = construction.ArgsExtra.Options;
            _itemOptions = construction.ArgsExtra.ItemOptions;

            _radiation = construction.ArgsExtra.Radiation;
            _gravity = construction.ArgsExtra.Gravity;
            _cameraPool = construction.ArgsExtra.CameraPool;

            _parts = construction.PartConstruction;
            _thrusters = construction.PartConstruction.GetStandardParts<Thruster>(Thruster.PARTTYPE).ToArray();
            _impulseEngines = construction.PartConstruction.GetStandardParts<ImpulseEngine>(ImpulseEngine.PARTTYPE).ToArray();
            _projectileGuns = construction.PartConstruction.GetStandardParts<ProjectileGun>(ProjectileGun.PARTTYPE).ToArray();
            _updatableParts_MainThread = construction.UpdatableParts_MainThread;
            _updatableParts_AnyThread = construction.UpdatableParts_AnyThread;
            _dna = construction.DNA;
            _dnaParts = construction.DNAParts;

            this.Model = construction.Model;
            _visualEffects = construction.VisualEffects;

            _isPhysicsStatic = construction.ArgsExtra.IsPhysicsStatic;
            this.PhysicsBody = construction.PhysicsBody;

            this.Radius = construction.Radius;

            _partTransformToModel = _parts.AllPartsArray.
                Select(o =>
                {
                    Transform3DGroup transform = new Transform3DGroup();
                    transform.Children.Add(new TranslateTransform3D(-o.Position.ToVector()));
                    transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(o.Orientation.ToReverse())));
                    return transform;
                }).
                ToArray();

            // Hook up events
            if (!_isPhysicsStatic)
            {
                this.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(PhysicsBody_ApplyForceAndTorque);
            }

            foreach (var part in _parts.AllPartsArray)
            {
                part.RequestWorldLocation += new EventHandler<PartRequestWorldLocationArgs>(Part_RequestWorldLocation);
                part.RequestWorldSpeed += new EventHandler<PartRequestWorldSpeedArgs>(Part_RequestWorldSpeed);
                part.RequestParent += new EventHandler<PartRequestParentArgs>(Part_RequestParent);

                part.Resurrected += Part_Resurrected;
                part.Destroyed += Part_Destroyed;
            }

            // See if there are parts that can gradually change the ship's mass
            if ((_parts.Containers.Fuels.Count > 0 && _parts.StandardParts.ContainsKey(Thruster.PARTTYPE)) ||
                (_parts.Containers.CargoBays.Count > 0 && (_parts.StandardParts.ContainsKey(ConverterMatterToEnergy.PARTTYPE) || _parts.StandardParts.ContainsKey(ConverterMatterToFuel.PARTTYPE))) ||
                (_parts.Containers.Energies.Count > 0 && _parts.Containers.Fuels.Count > 0 && (_parts.StandardParts.ContainsKey(ConverterEnergyToFuel.PARTTYPE) || _parts.StandardParts.ContainsKey(ConverterFuelToEnergy.PARTTYPE)))
                )
            {
                _hasMassChangingUpdatables_Small = true;
            }
            else
            {
                _hasMassChangingUpdatables_Small = false;
            }

            if (_parts.Containers.Ammos.Count > 0 && _parts.StandardParts.ContainsKey(ProjectileGun.PARTTYPE))
            {
                _hasMassChangingUpdatables_Medium = true;
            }
            else
            {
                _hasMassChangingUpdatables_Medium = false;
            }

            // Set up a neural processor on its own thread/task
            _neuronLinks = construction.Links;
            if (_neuronLinks != null)
            {
                var result = AddToNeuralPool(_neuronLinks, construction.ArgsExtra.NeuralPoolManual);
                _linkBucket = result.bucket;
                _neuralPoolAddTask = result.addTask;
                _neuralPoolManualTick = result.manualPool;
            }

            _lifeEvents = construction.PartConstruction.LifeEventWatcher;

            this.ShouldRecalcMass_Large = false;
            this.ShouldRecalcMass_Small = false;

            this.CreationTime = DateTime.UtcNow;
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
                _isDisposed = true;

                if (_parts != null)
                {
                    if (_linkBucket != null)
                    {
                        if (_neuralPoolAddTask != null)
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
                        else if (_neuralPoolManualTick != null)
                        {
                            _neuralPoolManualTick.Remove(_linkBucket);
                        }
                    }

                    if (_parts.AllPartsArray != null)
                    {
                        foreach (PartBase part in _parts.AllPartsArray)
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

        private volatile bool _isDisposed = false;
        public bool IsDisposed
        {
            get
            {
                return _isDisposed || this.PhysicsBody.IsDisposed;
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

        public virtual void Update_MainThread(double elapsedTime)
        {
            _partUpdateCount_MainThread++;

            foreach (IPartUpdatable part in _updatableParts_MainThread)      // Not bothering to call these in random order, because main thread should just be graphics
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

            if (_parts.Containers.ConvertMatterGroup != null && age >= (double)_nextMatterTransferTime)
            {
                if (_parts.Containers.ConvertMatterGroup.Transfer())
                {
                    this.ShouldRecalcMass_Large = true;
                }

                //NOTE: Throwing a min in there, because if I have breakpoints set elsewhere, and resume after awhile, elapsed time will be
                //huge, and the calculated next transer time will be obscene.  In those cases, the odds are good that the bot will be fairly young
                //so a simple 110% of age should be enough of a safeguard
                //_nextMatterTransferTime = Math.Min(age * 1.1, age + (elapsedTime * StaticRandom.Next(80, 120)));      // no need to spin the processor unnecessarily each tick
                _nextMatterTransferTime = Math.Min(age * 1.1, age + (elapsedTime * StaticRandom.Next(20, 60)));      // no need to spin the processor unnecessarily each tick
            }

            #endregion

            #region Update Parts

            foreach (int index in UtilityCore.RandomRange(0, _updatableParts_AnyThread.Length))
            {
                int skips = _updatableParts_AnyThread[index].IntervalSkips_AnyThread.Value;        // no need to check for null, nulls weren't added to the list
                if (skips > 0 && count % (skips + 1) != 0)
                {
                    continue;
                }

                _updatableParts_AnyThread[index].Update_AnyThread(elapsedTime + (elapsedTime * skips));       // if skips is greater than zero, then approximate how much time elapsed based on this tick's elapsed time
            }

            if (_lifeEvents != null)
            {
                _lifeEvents.Update_AnyThread(elapsedTime);
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
                double ammoMassCur = _parts.Containers.AmmoGroup.QuantityCurrent;

                if (ammoVolumeAtRecalc != null && !Math1D.IsNearValue(ammoVolumeAtRecalc.Value, ammoMassCur))
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
                if (_updatableParts_AnyThread.Length > 0)
                {
                    //TODO: This is too simplistic.  Need the greatest common denominator: return _updatableParts_AnyThread.Min(o => o.IntervalSkips_AnyThread);
                    return 0;
                }
                else
                {
                    return null;
                }
            }
        }

        #endregion
        #region ITakesDamage

        public event EventHandler Destroyed = null;
        public event EventHandler Resurrected = null;

        public virtual void TakeDamage_Collision(IMapObject collidedWith, Point3D positionModel, Vector3D velocityModel, double mass1, double mass2)
        {
            if (_parts == null || _parts.AllPartsArray == null || _parts.AllPartsArray.Length == 0)
            {
                return;
            }

            #region EXTREME

            // This is a lot of work, but just finding the closest point seems to return the same thing

            //var allHits = _parts.AllPartsArray.
            //    Select(o =>
            //    {
            //        Point3D nearest = Math3D.GetClosestPoint_Line_Point(positionModel, velocityModel, o.Position);

            //        return new
            //        {
            //            Part = o,
            //            PartRadius = Math1D.Avg(o.ScaleActual.X, o.ScaleActual.Y, o.ScaleActual.Z) / 2,
            //            Nearest = nearest,
            //            DistFromLineSqr = (nearest - o.Position).LengthSquared,
            //            DistFromHitSqr = (o.Position - positionModel).LengthSquared,
            //        };
            //    }).
            //    //OrderBy(o => o.DistFromHitSqr).       // don't bother yet
            //    ToArray();

            // Only consider parts that the impact velocity line pierces
            //var bestHit = allHits.
            //    Where(o => o.DistFromLineSqr <= o.PartRadius * o.PartRadius).
            //    OrderBy(o => o.DistFromHitSqr).
            //    FirstOrDefault();

            //if (bestHit == null)
            //{
            //    // The impact line doesn't intersect any parts.  Just choose the part that is closest to the impact point
            //    bestHit = allHits.
            //        OrderBy(o => o.DistFromHitSqr).
            //        First();
            //}

            #endregion

            //TODO: If it's a hard enough impact, damage parts that are in line and touching - make the chance of distribution higher if the impacted
            //part is already destroyed

            // Find the nearest part
            Tuple<PartBase, int> nearestPart = _parts.AllPartsArray.
                Select((o, i) => Tuple.Create(o, i)).
                OrderBy(o => (o.Item1.Position - positionModel).LengthSquared).
                First();

            // Transform the hit into part's coords
            Point3D positionPart = _partTransformToModel[nearestPart.Item2].Transform(positionModel);
            Vector3D velocityPart = _partTransformToModel[nearestPart.Item2].Transform(velocityModel);

            nearestPart.Item1.TakeDamage_Collision(collidedWith, positionPart, velocityPart, mass1, mass2);
        }
        public virtual void TakeDamage_Energy(double amount, Point3D positionModel)
        {
        }
        public virtual void TakeDamage_Heat(double amount, Point3D positionModel)
        {
        }

        public virtual bool IsDestroyed
        {
            get
            {
                return false;
            }
        }

        public virtual double HitPoints_Current
        {
            get
            {
                //return 1;
                return _parts.AllPartsArray.Sum(o => o.HitPoints_Max);
            }
        }
        public virtual double HitPoints_Max
        {
            get
            {
                //return 1;
                return _parts.AllPartsArray.Sum(o => o.HitPoints_Max);
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
        public IContainer Ammo => _parts.Containers.AmmoGroup;
        public IContainer Energy => _parts.Containers.EnergyGroup;
        public IContainer Fuel => _parts.Containers.FuelGroup;
        public IContainer Plasma => _parts.Containers.PlasmaGroup;

        public CargoBayGroup CargoBays => _parts.Containers.CargoBayGroup;

        private readonly Thruster[] _thrusters;
        public Thruster[] Thrusters => _thrusters;

        private readonly ImpulseEngine[] _impulseEngines;
        public ImpulseEngine[] ImpulseEngines => _impulseEngines;

        private readonly ProjectileGun[] _projectileGuns;
        public ProjectileGun[] ProjectileGuns => _projectileGuns;

        public IEnumerable<PartBase> Parts => _parts.AllPartsArray;

        // This is exposed for the ship viewer to be able to draw the links (the neurons are stored in the individual parts, but the links are stored at the ship level)
        private readonly NeuralUtility.ContainerOutput[] _neuronLinks;
        public NeuralUtility.ContainerOutput[] NeuronLinks => _neuronLinks;

        public string Name => _dna.ShipName;
        public string Lineage => _dna.ShipLineage;
        public long Generation => _dna.Generation;

        private volatile object _age = 0d;
        /// <summary>
        /// This is the age of the ship (sum of elapsed time).  This doesn't use datetime.utcnow, it's all based on the elapsed time passed into update (mainthread)
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

        private volatile bool _shouldLearnFromLifeEvents = true;
        /// <summary>
        /// Suppress learning from life events if you are programatically setting container quantities
        /// </summary>
        /// <remarks>
        /// The bot will watch container quantities.  If there are large changes, it remembers sensor values, and associates what
        /// it saw with those changes (running over a mineral would cause cargo to go up, associating minerals with food)
        /// 
        /// But if the bot is being artificially adjusted, it shouldn't try to learn during that time
        /// </remarks>
        public bool ShouldLearnFromLifeEvents
        {
            get
            {
                return _shouldLearnFromLifeEvents;
            }
            set
            {
                _shouldLearnFromLifeEvents = value;
                _lifeEvents.ShouldRaiseEvents = value;
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

        private readonly bool _isPhysicsStatic;
        /// <summary>
        /// If this is true, then the ship's mass will be set to zero, and the newton engine will treat it like it's static
        /// </summary>
        /// <remarks>
        /// Velocity won't matter, it will be stationary.  It can be positioned manually
        /// Other bodies will still bounce off of it (unless the matierial's IsCollidable==false)
        /// Two bodies that are zero mass will just ignore each other
        /// I'm not sure how joints are treated
        /// 
        /// It's ok to set the various ShouldRecalcMass_ properties when this is true.  Even if they are true, the mass won't be recalculated
        /// </remarks>
        public bool IsPhysicsStatic => _isPhysicsStatic;

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
                Tuple<MassMatrix, Point3D> massBreakdown = BotConstructor.GetInertiaTensorAndCenterOfMass_Points(_parts.AllPartsArray, _dnaParts, _itemOptions.MomentOfInertiaMultiplier);

                if (_isPhysicsStatic)
                {
                    this.PhysicsBody.CenterOfMass = new Point3D(0, 0, 0);       // Part_RequestWorldLocation is wrong if this isn't zero as well
                    this.PhysicsBody.Mass = 0;
                }
                else
                {
                    this.PhysicsBody.CenterOfMass = massBreakdown.Item2;
                    this.PhysicsBody.MassMatrix = massBreakdown.Item1;
                }

                if (_parts.Containers.AmmoGroup != null)
                {
                    _ammoVolumeAtRecalc = _parts.Containers.AmmoGroup.QuantityCurrent;
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

            foreach (PartBase part in _parts.AllPartsArray)
            {
                ShipPartDNA dna = part.GetNewDNA();

                if (_neuronLinks != null && part is INeuronContainer)
                {
                    NeuralUtility.PopulateDNALinks(dna, (INeuronContainer)part, _neuronLinks);
                }

                dnaParts.Add(dna);
            }

            // Build the ship dna
            return ShipDNA.Create(_dna, dnaParts);
        }

        public static (NeuralBucket bucket, Task<NeuralBucket> addTask, NeuralPool_ManualTick manualPool) AddToNeuralPool(NeuralUtility.ContainerOutput[] links, NeuralPool_ManualTick manualPool)
        {
            // Create the bucket
            NeuralBucket bucket = new NeuralBucket(links.SelectMany(o => UtilityCore.Iterate(o.InternalLinks, o.ExternalLinks)).ToArray());

            Task<NeuralBucket> task = null;

            if (manualPool != null)
            {
                manualPool.Add(bucket);
            }
            else
            {
                // Add to the pool from another thread.  This way the lock while waiting to add won't tie up this thread
                task = Task.Run(() =>
                {
                    NeuralPool.Instance.Add(bucket);
                    return bucket;
                });
            }

            return (bucket, task, manualPool);
        }

        /// <summary>
        /// This is a helper method to keep the containers full - useful for testers.  Call this every tick
        /// </summary>
        public void RefillContainers()
        {
            if (Ammo != null)
            {
                Ammo.QuantityCurrent = Ammo.QuantityMax;
            }

            if (Fuel != null)
            {
                Fuel.QuantityCurrent = Fuel.QuantityMax;
            }

            if (Energy != null)
            {
                Energy.QuantityCurrent = Energy.QuantityMax;
            }

            if (Plasma != null)
            {
                Plasma.QuantityCurrent = Plasma.QuantityMax;
            }
        }

        #endregion
        #region Protected Methods

        /// <summary>
        /// This gets called whenever the mass is recalculated
        /// NOTE: This will get called from arbitrary threads
        /// </summary>
        protected virtual void OnMassRecalculated()
        {
            if (this.MassChanged != null)
            {
                this.MassChanged(this, new EventArgs());
            }
        }

        #endregion

        #region Event Listeners

        private void PhysicsBody_ApplyForceAndTorque(object sender, BodyApplyForceAndTorqueArgs e)
        {
            #region Thrusters

            //NOTE: The brain is running in another thread, so this method just applies the thrusters based on the current neural outputs

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
                        // Apply force
                        Vector3D bodyForce = e.Body.DirectionToWorld(thrusts[cntr].Value);
                        Point3D bodyPoint = e.Body.PositionToWorld(thruster.Position);
                        e.Body.AddForceAtPoint(bodyForce, bodyPoint);
                    }
                }
            }

            #endregion

            #region Impulse Engines

            foreach (ImpulseEngine impulse in _impulseEngines)
            {
                // Look at the thrusts from the last firing
                ImpulseEngineOutput impulseOutput = impulse.ThrustsTorquesLastUpdate;
                if (impulseOutput == null)
                {
                    continue;
                }

                // Apply force
                if (impulseOutput.Linear != null)
                {
                    //NOTE: Not looking at the part's orientation.  The impulse engine is a bit of a cheat part, and just works regardless of position/orientation
                    e.Body.AddForce(e.Body.DirectionToWorld(impulseOutput.Linear.Value));
                }

                // Apply torque
                if (impulseOutput.Torque != null)
                {
                    e.Body.AddTorque(e.Body.DirectionToWorld(impulseOutput.Torque.Value));
                }
            }

            #endregion

            #region Kick from guns

            foreach (ProjectileGun gun in _projectileGuns)
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
        private void Part_RequestParent(object sender, PartRequestParentArgs e)
        {
            e.Parent = this;
        }

        private void Part_Resurrected(object sender, EventArgs e)
        {
            // If there is only one part that isn't destroyed, then that must be what was resurrected.  Which means the bot went from a destroyed to resurrected state
            if (this.Resurrected != null && _parts.AllPartsArray.Count(o => !o.IsDestroyed) == 1)
            {
                this.Resurrected(this, new EventArgs());
            }
        }
        private void Part_Destroyed(object sender, EventArgs e)
        {
            // Only raise event if all parts are destroyed
            if (this.Destroyed != null && _parts.AllPartsArray.All(o => o.IsDestroyed))
            {
                this.Destroyed(this, new EventArgs());
            }
        }

        #endregion

        #region OLD
        //private static void UnitTestToModel()
        //{
        //    Point3D[] testPositions = new[]
        //    {
        //        new Point3D(0, 0, 0),
        //        new Point3D(1, 0, 0),
        //        new Point3D(0, 1, 0),
        //        new Point3D(0, 0, 1),
        //        new Point3D(1, 1, 1),
        //    };

        //    Vector3D[] testDirections = new[]
        //    {
        //        new Vector3D(0, 0, 0),
        //        new Vector3D(1, 0, 0),
        //        new Vector3D(0, 1, 0),
        //        new Vector3D(0, 0, 1),
        //        new Vector3D(1, 1, 1),
        //    };

        //    #region test 1

        //    Point3D bodyPos1 = new Point3D(1, 0, 0);
        //    Quaternion bodyRot1 = Quaternion.Identity;

        //    Transform3D transform1 = new TranslateTransform3D(-bodyPos1.ToVector());

        //    var transformed1a = testPositions.
        //        Select(o => new { Orig = o, New = transform1.Transform(o) }).
        //        ToArray();

        //    var transformed1b = testDirections.
        //        Select(o => new { Orig = o, New = transform1.Transform(o) }).
        //        ToArray();

        //    #endregion

        //    #region test 2

        //    Point3D bodyPos2 = new Point3D(0, 0, 0);
        //    Quaternion bodyRot2 = new Quaternion(new Vector3D(0, 0, 1), 90);

        //    Transform3D transform2 = new RotateTransform3D(new QuaternionRotation3D(bodyRot2.ToReverse()));

        //    var transformed2a = testPositions.
        //        Select(o => new { Orig = o, New = transform2.Transform(o) }).
        //        ToArray();

        //    var transformed2b = testDirections.
        //        Select(o => new { Orig = o, New = transform2.Transform(o) }).
        //        ToArray();

        //    #endregion

        //    #region test 3

        //    Point3D bodyPos3 = new Point3D(1, 0, 0);
        //    Quaternion bodyRot3 = new Quaternion(new Vector3D(0, 0, 1), 90);

        //    Transform3DGroup transform3 = new Transform3DGroup();
        //    transform3.Children.Add(new TranslateTransform3D(-bodyPos3.ToVector()));
        //    transform3.Children.Add(new RotateTransform3D(new QuaternionRotation3D(bodyRot3.ToReverse())));

        //    var transformed3a = testPositions.
        //        Select(o => new { Orig = o, New = transform3.Transform(o) }).
        //        ToArray();

        //    var transformed3b = testDirections.
        //        Select(o => new { Orig = o, New = transform3.Transform(o) }).
        //        ToArray();

        //    #endregion
        //}
        #endregion
    }
}
