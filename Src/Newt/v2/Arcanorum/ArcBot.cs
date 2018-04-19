using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.ShipEditor;
using Game.Newt.v2.GameItems.ShipParts;
using Game.HelperClassesWPF;
using Game.Newt.v2.NewtonDynamics;
using Game.HelperClassesWPF.Controls3D;
using System.Xaml;
using System.Windows;
using Game.Newt.v2.Arcanorum.Parts;

namespace Game.Newt.v2.Arcanorum
{
    //TODO: Have treausre mules that follow the player - if multiple, use swarming behavior
    //TODO: The number of shells should be based on a player's experience level
    //TODO: Adjust color intensity based on health
    //TODO: Adjust spin rate based on energy/chi
    //TODO: Don't stack sensors into cylinders, arrange them like hexagonal umbrellas

    /// <remarks>
    /// ------------------------ Bot Stats:
    /// HP
    /// HP Regen
    /// 
    /// Mana
    /// Mana Regen
    /// 
    /// Strength - Amount of force that can be applied
    /// 
    /// Physical Toughness - resistance to knockback
    /// Mental Toughness - resistance to magic
    /// 
    /// ------------------------ Weapon Buffs: (in addition to the base stats)
    /// Magic Damage
    /// Piercing
    /// Confusion/Sleep
    /// Mana Drain
    /// 
    /// ------------------------ Armor Buffs: (in addition to the base stats)
    /// HP boost
    /// HP regen
    /// 
    /// Mana boost
    /// Mana regen
    /// 
    /// Strength boost
    /// 
    /// Toughness boost
    /// Magic resistance boost
    /// </remarks>
    public class ArcBot : IMapObject, IPartUpdatable, IGivesDamage, ITakesDamage, IDisposable
    {
        #region enum: ItemToFrom

        public enum ItemToFrom
        {
            Nowhere,
            Map,
            Inventory
        }

        #endregion
        #region class: PartContainerBuilding

        protected class PartContainerBuilding
        {
            public List<SensorHoming> Homing = new List<SensorHoming>();
            public List<SensorVision> Vision = new List<SensorVision>();
            public List<Brain> Brain = new List<Brain>();
            public List<MotionController2> MotionController = new List<MotionController2>();
        }

        #endregion
        #region class: PartContainer

        protected class PartContainer
        {
            public PartContainer(PartContainerBuilding parts, PartBase[] allParts, IPartUpdatable[] updatableParts_MainThread, IPartUpdatable[] updatableParts_AnyThread, NeuralUtility.ContainerOutput[] links, NeuralBucket linkBucket)
            {
                this.Homing = parts.Homing.ToArray();
                this.Vision = parts.Vision.ToArray();
                this.Brain = parts.Brain.ToArray();
                this.MotionController = parts.MotionController.ToArray();

                this.AllParts = allParts;

                this.UpdatableParts_MainThread = updatableParts_MainThread;
                this.UpdatableParts_AnyThread = updatableParts_AnyThread;

                this.Links = links;
                this.LinkBucket = linkBucket;
            }

            public readonly SensorHoming[] Homing;
            public readonly SensorVision[] Vision;
            public readonly Brain[] Brain;
            public readonly MotionController2[] MotionController;

            // --------------- Part Groups
            public readonly PartBase[] AllParts;

            public readonly IPartUpdatable[] UpdatableParts_MainThread;
            public readonly IPartUpdatable[] UpdatableParts_AnyThread;

            public readonly NeuralUtility.ContainerOutput[] Links;
            public readonly NeuralBucket LinkBucket;
        }

        #endregion

        #region Events

        public delegate void BodySwappedHandler(Body oldBody, Body newBody);

        public event BodySwappedHandler BodySwapped = null;

        #endregion

        #region Declaration Section

        private const bool SHOULDKEEP2D = true;

        protected readonly World _world;
        private readonly Map _map;
        private readonly KeepItems2D _keepItems2D;
        private readonly MaterialIDs _materialIDs;
        private readonly Viewport3D _viewport;
        private readonly ItemOptionsArco _itemOptions;

        private JointBase _weaponAttachJoint = null;

        private readonly SpriteGraphic_Shells _graphic;
        private readonly ModelVisual3D _visual;

        private readonly IGravityField _gravity;

        protected readonly DragHitShape _dragPlane;

        /// <summary>
        /// This will only be instantiated if there is a MotionController_Linear part defined in the dna class.  It is only needed for ai bots, but is
        /// defined here, because parts are at bot level
        /// </summary>
        protected volatile AIMousePlate _aiMousePlate;

        private SortedList<long, double> _weaponsColliding = new SortedList<long, double>();

        /// <summary>
        /// NOTE: This is built async, so could be null for a bit
        /// </summary>
        /// <remarks>
        /// With ship, parts are everything.  The ship is just a wrapper of the parts.
        /// 
        /// But with bot, parts are just for AI
        /// </remarks>
        protected volatile PartContainer _parts = null;
        private readonly Task<Tuple<PartContainer, ShipPartDNA[], CollisionHull[], AIMousePlate>> _partsTask;
        private long _partUpdateCount_MainThread = -1;      // starting at -1 so that the first increment will bring it to 0 (skips use mod, so zero mod anything is zero, so everything will fire on the first tick)
        private long _partUpdateCount_AnyThread = -1;       // this is incremented through Interlocked, so doesn't need to be volatile

        #endregion

        #region Constructor

        /// <summary>
        /// NOTE: This doesn't add itself to the map or viewport, those are passed in so that it can add/remove equipment
        /// </summary>
        public ArcBot(BotDNA dna, int level, Point3D position, World world, Map map, KeepItems2D keepItems2D, MaterialIDs materialIDs, Viewport3D viewport, EditorOptions editorOptions, ItemOptionsArco itemOptions, IGravityField gravity, DragHitShape dragPlane, Point3D homingPoint, double homingRadius, bool runNeural, bool repairPartPositions)
        {
            _world = world;
            _map = map;
            _keepItems2D = keepItems2D;
            _materialIDs = materialIDs;
            _viewport = viewport;
            _gravity = gravity;
            _dragPlane = dragPlane;
            _itemOptions = itemOptions;
            _homingPoint = homingPoint;

            Inventory = new Inventory();

            DNAPartial = GetFinalDNA(dna);        // this needs to be done before parts are created, because when the async comes back, it will add the part dna to it

            // Ram
            Ram = new RamWeapon(DNAPartial.Ram, this, viewport);

            #region WPF Model

            _graphic = new SpriteGraphic_Shells(level, DNAPartial.ShellColors, _itemOptions);

            Model = _graphic.Model;

            // Model Visual (this is the expensive one, so as few of these should be made as possible)
            _visual = new ModelVisual3D
            {
                Content = Model
            };

            #endregion

            HitPoints = new Container() { QuantityMax = 1000000000, QuantityCurrent = 1000000000 };        // the current/max will get set by the level change.  Making it very large so when it gets reduced, current is still full

            Level = level;     // the property set will set radius, build the physics body, update graphic

            PhysicsBody.Position = position;

            #region Parts

            //NOTE: Some of the parts are dependent on the physics body, so building them after

            if (dna != null && dna.Parts != null)
            {
                _partsTask = BuildParts(dna.Parts, runNeural, repairPartPositions, _world, editorOptions, itemOptions, _gravity, _map, _dragPlane, Radius, Token, homingPoint, homingRadius, this);

                _partsTask.ContinueWith(t =>
                {
                    if (t.Result != null)
                    {
                        _parts = t.Result.Item1;
                        DNAPartial.Parts = t.Result.Item2;        //NOTE: Parts were already cloned from the dna passed into the constructor.  This replaces with the final (throws out parts that are too small, fills in links, makes sure parts aren't intersecting)
                        DNAFinal = DNAPartial;
                        _aiMousePlate = t.Result.Item4;

                        //Ship creates its collision hull as a composite of these part hulls, but bot is just a sphere.  These hulls still need to be disposed though
                        if (t.Result.Item3 != null)
                        {
                            foreach (CollisionHull partHull in t.Result.Item3)
                            {
                                partHull.Dispose();
                            }
                        }

                        // The homing point may have changed
                        foreach (SensorHoming homing in _parts.Homing)
                        {
                            homing.HomePoint = _homingPoint;
                        }
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }

            #endregion

            //TODO: Calculate these based on dna
            ReceiveDamageMultipliers = new WeaponDamage();

            CreationTime = DateTime.UtcNow;
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _isDisposed = true;

                this.PhysicsBody.ApplyForceAndTorque -= new EventHandler<BodyApplyForceAndTorqueArgs>(Body_ApplyForceAndTorque);

                if (_partsTask != null)
                {
                    //NOTE: Since this is async, must make sure the remove is called only after the add finishes
                    _partsTask.ContinueWith(t =>
                    {
                        if (t.Result != null)
                        {
                            NeuralPool.Instance.Remove(t.Result.Item1.LinkBucket);
                        }
                    });
                }

                AttachWeapon(null);     // when sending the existing to nowhere, it gets disposed

                this.DraggingBot.Dispose();

                this.Ram.Dispose();

                this.PhysicsBody.Dispose();
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

            if (_parts != null)     // once non null, it will never go back to null
            {
                foreach (IPartUpdatable part in _parts.UpdatableParts_MainThread)      // Not bothering to call these in random order, because main thread should just be graphics
                {
                    int skips = part.IntervalSkips_MainThread.Value;        // no need to check for null, nulls weren't added to the list
                    if (skips > 0 && _partUpdateCount_MainThread % (skips + 1) != 0)
                    {
                        continue;
                    }

                    part.Update_MainThread(elapsedTime);
                }
            }

            this.Ram.Update_MainThread(elapsedTime);

            _graphic.Update(elapsedTime);

            #region Cleanup continuous collision

            int index = 0;
            while (index < _weaponsColliding.Keys.Count)
            {
                long key = _weaponsColliding.Keys[index];
                double remaining = _weaponsColliding[key] - elapsedTime;

                if (remaining < 0)
                {
                    _weaponsColliding.Remove(key);
                }
                else
                {
                    _weaponsColliding[key] = remaining;
                    index++;
                }
            }

            #endregion
        }
        public virtual void Update_AnyThread(double elapsedTime)
        {
            long count = Interlocked.Increment(ref _partUpdateCount_AnyThread);

            var parts = _parts;
            if (parts != null)
            {
                foreach (int index in UtilityCore.RandomRange(0, parts.UpdatableParts_AnyThread.Length))
                {
                    int skips = parts.UpdatableParts_AnyThread[index].IntervalSkips_AnyThread.Value;        // no need to check for null, nulls weren't added to the list
                    if (skips > 0 && count % (skips + 1) != 0)
                    {
                        continue;
                    }

                    parts.UpdatableParts_AnyThread[index].Update_AnyThread(elapsedTime);
                }
            }

            this.Ram.Update_AnyThread(elapsedTime);
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
                return 0;
            }
        }

        #endregion
        #region IGivesDamage Members

        public WeaponDamage CalculateDamage(MaterialCollision[] collisions)
        {
            return this.Ram.CalculateDamage(collisions);
        }

        #endregion
        #region ITakesDamage Members

        public (bool isDead, WeaponDamage actualDamage)? Damage(WeaponDamage damage, Weapon weapon = null)
        {
            const double ELAPSED = 1;

            if (weapon != null && _weaponsColliding.ContainsKey(weapon.Token))
            {
                // Don't double count the same collision (this happens because the bot is being actively driven around by
                // a MapObjectChaseVelocity)
                _weaponsColliding[weapon.Token] = ELAPSED;      // start the countdown over
                return null;
            }

            if (weapon != null)
            {
                _weaponsColliding.Add(weapon.Token, ELAPSED);
            }

            return WeaponDamage.DoDamage(this, damage);
        }

        public WeaponDamage ReceiveDamageMultipliers
        {
            get;
            private set;
        }

        public Container HitPoints
        {
            get;
            private set;
        }

        #endregion

        #region Public Properties

        public int Level
        {
            get
            {
                return _graphic.Level;
            }
            set
            {
                int newValue = value;
                if (newValue < 1)
                {
                    newValue = 1;
                }
                else if (newValue > ItemOptionsArco.MAXLEVEL)
                {
                    newValue = ItemOptionsArco.MAXLEVEL;
                }

                //if(newValue == _graphic.Level)
                //{
                //    return;
                //}

                _graphic.Level = value;

                LevelChanged();
            }
        }

        /// <summary>
        /// This resets to zero whenever the level changes
        /// </summary>
        public double Experience { get; private set; }

        //TODO: Weapon attaching can currently only be done on the main thread.  Make it thread safe

        //TODO: The weapon shouldn't be added to the viewport when it is attached to this class.  It should have been removed from the map
        //outside of this class, then call attach.  This class should be responsible for adding to the viewport
        private Weapon _weapon = null;
        public Weapon Weapon
        {
            get
            {
                return _weapon;
            }
        }

        public RamWeapon Ram
        {
            get;
            private set;
        }

        public Inventory Inventory
        {
            get;
            private set;
        }

        // Don't expose this because it's null then set async
        //public IEnumerable<PartBase> Parts => _parts.AllParts;

        private Point3D _homingPoint;
        public Point3D HomingPoint
        {
            get
            {
                return _homingPoint;
            }
            set
            {
                _homingPoint = value;

                if (_parts != null)
                {
                    foreach (SensorHoming homing in _parts.Homing)
                    {
                        homing.HomePoint = _homingPoint;
                    }
                }
            }
        }

        /// <summary>
        /// This moves the bot around.  In this.Update, the chase point is set.  This class hooks itself to
        /// this.PhysicsBody.ApplyForceAndTorque, so no need to tell it when to go.  Just keep refreshing
        /// the chase point
        /// </summary>
        public MapObject_ChasePoint_Velocity DraggingBot
        {
            get;
            private set;
        }

        /// <summary>
        /// This has everything but finalized parts
        /// </summary>
        public readonly BotDNA DNAPartial;
        /// <summary>
        /// This stays null until the dna is fully built.  This way, there's no chance of a partial dna being exposed to the public.
        /// NOTE: Can't _partsTask.ContinueWith( ... TaskScheduler.FromCurrentSynchronizationContext).Wait(), because that will deadlock this thread.  If the consumer really wants
        /// to wait for this to be built, then make this return Task(BotDNA), and rework the continuewith in the constructor
        /// </summary>
        public volatile BotDNA DNAFinal = null;

        #endregion

        #region Public Methods

        /// <summary>
        /// This may or may not pick up the treasure
        /// </summary>
        public void CollidedWithTreasure(IMapObject treasure)
        {
            // Inventory needs to have a set of rules to see if this treasure is worth picking up:
            //  - Items could be individually locked
            //  - Have custom rules that allow lower value items to be replaced with higher value items, or preferred item types (sword over hammer), etc
            //IMapObject[] discarded = _inventory.Add(treasure);

            //if (discarded != null)
            //{
            //    _map.Remove(treasure);

            //    _map.Add(discarded);
            //}
        }

        /// <summary>
        /// This attaches the weapon passed in, and returns the previous weapon.  Also optionally manages the weapon's states within
        /// the map or inventory
        /// </summary>
        /// <param name="newFrom">If from map or inventory, this will remove it from those places</param>
        /// <param name="existingTo">If told to, will add the existing back into inventory or map</param>
        public void AttachWeapon(Weapon newWeapon, ItemToFrom newFrom = ItemToFrom.Nowhere, ItemToFrom existingTo = ItemToFrom.Nowhere)
        {
            Weapon existing = _weapon;

            // Pause the world
            bool shouldResume = false;
            if (!_world.IsPaused)
            {
                shouldResume = true;
                _world.Pause();
            }

            // Unhook previous weapon (remove joint)
            AttachWeapon_UnhookPrevious();

            // Hand off existing weapon
            if (existing != null)
            {
                AttachWeapon_HandOffWeapon(existing, existingTo);
            }

            // Take new weapon
            AttachWeapon_TakeNewWeapon(ref newWeapon, newFrom);

            // Hook up new weapon (add joint)
            AttachWeapon_HookupNew(newWeapon);

            // In order for semitransparency to work, this bot's visuals must be added to the viewport last
            if (newWeapon != null && _viewport != null && Visuals3D != null)
            {
                _viewport.Children.RemoveAll(Visuals3D);
                _viewport.Children.AddRange(Visuals3D);
            }

            // Resume the world
            if (shouldResume)
            {
                _world.UnPause();
            }
        }

        public bool AddToInventory(Weapon newWeapon, ItemToFrom newFrom = ItemToFrom.Nowhere)
        {
            newWeapon.ShowAttachPoint = true;

            switch (newFrom)
            {
                case ItemToFrom.Nowhere:
                    if (newWeapon.IsGraphicsOnly)
                    {
                        this.Inventory.Weapons.Add(newWeapon);
                    }
                    else
                    {
                        // The inventory can only hold graphics only, so convert it and dispose the physics version
                        this.Inventory.Weapons.Add(new Weapon(newWeapon.DNA, new Point3D(), null, _materialIDs.Weapon));
                        newWeapon.Dispose();
                    }
                    return true;

                case ItemToFrom.Inventory:      // there should be no reason to call this method when it's already in inventory
                    return true;

                case ItemToFrom.Map:

                    //TODO: Ask inventory first
                    //if (!this.Inventory.CanTake(newWeapon))
                    //{
                    //    return false;
                    //}


                    // Clone the weapon, but no physics
                    Weapon nonPhysics = new Weapon(newWeapon.DNA, new Point3D(), null, _materialIDs.Weapon);

                    // Dispose the physics version
                    if (SHOULDKEEP2D) _keepItems2D.Remove(newWeapon);
                    _map.RemoveItem(newWeapon, true);     // the map also removes from the viewport
                    //_viewport.Children.RemoveAll(newWeapon.Visuals3D);

                    nonPhysics.ShowAttachPoint = true;

                    // Add to inventory
                    this.Inventory.Weapons.Add(nonPhysics);
                    return true;

                default:
                    throw new ApplicationException("Unknown ItemToFrom: " + newFrom.ToString());
            }
        }
        public void RemoveFromInventory(Weapon removeWeapon, ItemToFrom existingTo = ItemToFrom.Nowhere)
        {
            switch (existingTo)
            {
                case ItemToFrom.Inventory:      // there should be no reason to call this method when it's going back into inventory
                    break;

                case ItemToFrom.Nowhere:
                    // Make sure it's in the inventory
                    if (this.Inventory.Weapons.Remove(removeWeapon))
                    {
                        removeWeapon.Dispose();
                    }
                    break;

                case ItemToFrom.Map:
                    // Make sure it's in the inventory
                    if (this.Inventory.Weapons.Remove(removeWeapon))
                    {
                        // Convert from graphics only to a physics weapon
                        Weapon physicsWeapon = new Weapon(removeWeapon.DNA, new Point3D(), _world, _materialIDs.Weapon);

                        removeWeapon.Dispose();

                        physicsWeapon.ShowAttachPoint = true;

                        SetDropOffset(physicsWeapon);

                        if (SHOULDKEEP2D)
                        {
                            _keepItems2D.Add(physicsWeapon, false);
                        }

                        _map.AddItem(physicsWeapon);
                    }
                    break;

                default:
                    throw new ApplicationException("Unknown ItemToFrom: " + existingTo.ToString());
            }
        }

        internal static void GetSettingsForLevel(out int numLayers, out double radius, out double mass, out double innerShellAlphaMult, out double outerShellAlphaMult, out double hitPoints, int level, ItemOptionsArco itemOptions)
        {
            if (level <= 0)
            {
                throw new ArgumentException("level must be a positive number: " + level.ToString());
            }
            else if (level > ItemOptionsArco.MAXLEVEL)
            {
                throw new ArgumentException(string.Format("level can't exceed {0} ({1})", ItemOptionsArco.MAXLEVEL, level));
            }

            //NOTE: There is a chance that these arrays could be null or not large enough for MAXLEVEL, but that's the fault of whoever changed the options
            radius = itemOptions.RadiusAtLevel[level];
            mass = itemOptions.MassAtLevel[level];
            hitPoints = itemOptions.HitPointsAtLevel[level];

            double numLayersReal = UtilityCore.GetScaledValue(1, 7, 1, ItemOptionsArco.MAXLEVEL, level);
            numLayers = Convert.ToInt32(Math.Floor(numLayersReal));

            double fraction = numLayersReal - numLayers;        // the closer it is to making a new layer, the darker it should get
            innerShellAlphaMult = UtilityCore.GetScaledValue_Capped(.5, 1, 0, 1, fraction);
            outerShellAlphaMult = UtilityCore.GetScaledValue_Capped(.2, .8, 0, 1, fraction);
        }

        public void AddExperience(double xp)
        {
            double[] levelUps = _itemOptions.XPForNexLevel;

            // Just do the null check once up front (this should never happen, but if it does, let the xp keep growing)
            if (levelUps == null || Level >= levelUps.Length - 1 || Level >= ItemOptionsArco.MAXLEVEL)
            {
                Experience += xp;
                return;
            }

            while (true)
            {
                if (Experience + xp >= levelUps[Level])
                {
                    // Store the remainder (if the experience was already greater, then xp will get larger than what was passed in, but future iterations of this loop will use it up)
                    xp = (Experience + xp) - levelUps[Level];

                    // The property set does quite a bit when leveling (also resets Experience)
                    Level++;
                }
                else
                {
                    // Not enough to cause a level change
                    Experience += xp;
                    break;
                }
            }
        }

        #endregion
        #region Protected Methods

        protected virtual void OnBodySwapped(Body oldBody, Body newBody)
        {
            // Apply force and torque
            if (oldBody != null)
            {
                oldBody.ApplyForceAndTorque -= new EventHandler<BodyApplyForceAndTorqueArgs>(Body_ApplyForceAndTorque);
            }

            newBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(Body_ApplyForceAndTorque);

            // This will keep the player tied to the plane using velocities (not forces)
            this.DraggingBot = new MapObject_ChasePoint_Velocity(this);        // the old one was disposed before the swap, because it talks to this.PhysicsBody
            this.DraggingBot.MaxVelocity = this.DNAPartial.DraggingMaxVelocity.Value;
            this.DraggingBot.Multiplier = this.DNAPartial.DraggingMultiplier.Value;

            // Inform the world
            if (this.BodySwapped != null)
            {
                this.BodySwapped(oldBody, newBody);
            }
        }

        #endregion

        #region Event Listeners

        private void Body_ApplyForceAndTorque(object sender, BodyApplyForceAndTorqueArgs e)
        {
            //NOTE: this.DraggingPlayer is handling the force toward the mouse

            double inventoryMass = Inventory.Mass;
            if (inventoryMass > 0 && _gravity != null)
            {
                //TODO: Allow the bot to have weight reduction multiplier perks (level up and spend perk points to not be so burdened by inventory)
                Vector3D force = _gravity.GetForce(PositionWorld);
                force *= inventoryMass * _itemOptions.InventoryWeightPercent;

                e.Body.AddForce(force);
            }


            //TODO: Make a class that does this
            Vector3D angularVelocity = PhysicsBody.AngularVelocity;

            if (angularVelocity.LengthSquared > 1)
            {
                PhysicsBody.AngularVelocity = angularVelocity * .9d;
            }
        }

        private void Part_RequestWorldLocation(object sender, PartRequestWorldLocationArgs e)
        {
            if (!(sender is PartBase))
            {
                throw new ApplicationException("Expected sender to be PartBase");
            }

            PartBase senderCast = (PartBase)sender;

            e.Orientation = senderCast.Orientation.RotateBy(this.PhysicsBody.Rotation);		//TODO: Make sure the order is correct
            e.Position = this.PositionWorld + senderCast.Position.ToVector();
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

        //NOTE: This is sort of a copy of Ship.BuildParts.  The main difference is that this does everything here.  Ship does some in BuildParts, then a bit more in a task off the constructor.
        //Ship requires a factory method that builds a ship async.  Bot kicks this off async from the constructor, so bot will run awhile before _parts gets populated.
        private async static Task<Tuple<PartContainer, ShipPartDNA[], CollisionHull[], AIMousePlate>> BuildParts(ShipPartDNA[] dna, bool runNeural, bool repairPartPositions, World world, EditorOptions editorOptions, ItemOptionsArco itemOptions, IGravityField gravity, Map map, DragHitShape dragPlane, double botRadius, long botToken, Point3D homingPoint, double homingRadius, ArcBot thisBot)
        {
            if (dna == null)
            {
                return null;
            }

            // Throw out parts that are too small
            ShipPartDNA[] usableParts = dna.Where(o => o.Scale.Length > .01d).ToArray();
            if (usableParts.Length == 0)
            {
                return null;
            }

            AIMousePlate mousePlate = null;

            PartContainerBuilding containerBuilding = new PartContainerBuilding();

            // Create the parts based on dna
            var combined = BuildParts_Create(ref mousePlate, containerBuilding, usableParts, editorOptions, itemOptions, gravity, map, dragPlane, botRadius, botToken, homingPoint, homingRadius);

            Ship.BuildPartsResults results = await Ship.BuildParts_Finish(combined, usableParts, runNeural, repairPartPositions, itemOptions, world);

            //NOTE: This must be done before adding to the neural pool (SensorHoming is crashing because it can't get world position)
            foreach (var part in results.AllParts)
            {
                part.RequestWorldLocation += new EventHandler<PartRequestWorldLocationArgs>(thisBot.Part_RequestWorldLocation);
                part.RequestWorldSpeed += new EventHandler<PartRequestWorldSpeedArgs>(thisBot.Part_RequestWorldSpeed);
            }

            // Link bucket
            //NOTE: Ship.AddToNeuralPool is designed funny, because the ship lets the task run while it's going.  This bot will wait for it to finish before returning
            //      - ship stores parts as readonly, but needs to hang onto the task
            //      - bot stores parts as volatile, but there will need to be if checks all over
            //Time will tell which design is better
            var linkBucket = Ship.AddToNeuralPool(results.Links);
            await linkBucket.Item1;

            PartContainer container = new PartContainer(containerBuilding, results.AllParts, results.UpdatableParts_MainThread, results.UpdatableParts_AnyThread, results.Links, linkBucket.Item2);

            return Tuple.Create(container, results.DNA, results.Hulls, mousePlate);
        }
        private static List<Tuple<PartBase, ShipPartDNA>> BuildParts_Create(ref AIMousePlate mousePlate, PartContainerBuilding container, ShipPartDNA[] parts, EditorOptions editorOptions, ItemOptionsArco itemOptions, IGravityField gravity, Map map, DragHitShape dragPlane, double botRadius, long botToken, Point3D homingPoint, double homingRadius)
        {
            //TODO: Figure these out based on this.Radius
            const double SEARCHRADIUS = 10;

            List<Tuple<PartBase, ShipPartDNA>> retVal = new List<Tuple<PartBase, ShipPartDNA>>();

            //TODO: EnergyTank

            foreach (ShipPartDNA dna in parts)
            {
                switch (dna.PartType)
                {
                    case SensorHoming.PARTTYPE:
                        SensorHoming sensorHoming = new SensorHoming(editorOptions, itemOptions, dna, map, new ContainerInfinite(), homingPoint, homingRadius);

                        BuildParts_Add(sensorHoming,
                            dna, container.Homing, retVal);
                        break;

                    case SensorVision.PARTTYPE:
                        //TODO: Add filter type to the dna, use reflection to find the type

                        SensorVision sensorVision = new SensorVision(editorOptions, itemOptions, dna, map, SEARCHRADIUS, null);
                        sensorVision.BotToken = botToken;

                        BuildParts_Add(sensorVision,
                            dna, container.Vision, retVal);
                        break;

                    case Brain.PARTTYPE:
                        BuildParts_Add(new Brain(editorOptions, itemOptions, dna, null),// container.EnergyGroup),
                            dna, container.Brain, retVal);
                        break;

                    case MotionController2.PARTTYPE:
                        if (container.MotionController.Count > 0)
                        {
                            throw new ApplicationException("There can be only one plate writer in a bot");
                        }

                        mousePlate = new AIMousePlate(dragPlane);
                        mousePlate.MaxXY = botRadius * 20;
                        mousePlate.Scale = 1;

                        BuildParts_Add(new MotionController2(editorOptions, itemOptions, dna, mousePlate),// container.EnergyGroup),
                            dna, container.MotionController, retVal);
                        break;

                    default:
                        throw new ApplicationException("Unexpected dna.PartType: " + dna.PartType);
                }
            }

            return retVal;
        }
        private static void BuildParts_Add<T>(T item, ShipPartDNA dna, List<T> specificList, List<Tuple<PartBase, ShipPartDNA>> combinedList) where T : PartBase
        {
            // This is just a helper method so one call adds to two lists
            specificList.Add(item);
            combinedList.Add(new Tuple<PartBase, ShipPartDNA>(item, dna));
        }

        private void LevelChanged()
        {
            // Get values
            GetSettingsForLevel(out _, out double radius, out double mass, out _, out _, out double hitPoints, Level, _itemOptions);

            // Store new values
            Radius = radius;

            HitPoints.QuantityMax = hitPoints;
            if (_itemOptions.ShouldRechargeOnLevel)
            {
                HitPoints.QuantityCurrent = hitPoints;
            }

            Experience = 0;

            // Whenever radius changes, the body needs to be swapped out
            Weapon weapon = _weapon;
            Body oldBody = PhysicsBody;

            if (PhysicsBody != null)
            {
                AttachWeapon(null);
            }

            if (DraggingBot != null)
            {
                DraggingBot.Dispose();
            }

            PhysicsBody = GetPhysicsBody(Radius, mass, PhysicsBody, _world, _visual, _materialIDs.Bot);

            OnBodySwapped(oldBody, PhysicsBody);

            if (oldBody != null)
            {
                oldBody.Dispose();
            }

            if (weapon != null)
            {
                //Need to clone it, because unattaching disposed it
                weapon = new Arcanorum.Weapon(weapon.DNA, new Point3D(0, 0, 0), _world, _materialIDs.Weapon);
                AttachWeapon(weapon);
            }
        }

        private static Body GetPhysicsBody(double radius, double mass, Body current, World world, Visual3D visual, int materialID)
        {
            Point3D position = new Point3D(0, 0, 0);
            long? token = null;

            if (current != null)
            {
                position = current.Position;
                token = current.Token;
            }

            Transform3DGroup transform = new Transform3DGroup();
            //transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 90)));
            transform.Children.Add(new TranslateTransform3D(position.ToVector()));

            Body retVal = null;

            using (CollisionHull hull = CollisionHull.CreateSphere(world, 0, new Vector3D(radius, radius, radius), null))
            {
                retVal = new Body(hull, transform.Value, mass, new Visual3D[] { visual }, token);
                retVal.MaterialGroupID = materialID;
                retVal.LinearDamping = .01f;
                retVal.AngularDamping = new Vector3D(.01f, .01f, .01f);
            }

            return retVal;
        }

        /// <summary>
        /// This figures out where to drop the item
        /// </summary>
        private void SetDropOffset(IMapObject item)
        {
            if (item.PhysicsBody == null)
            {
                throw new ArgumentException("The physics body should be populated for this item");
            }

            //TODO: See if there are other items that this item may collide with
            Vector3D offset = Math3D.GetRandomVector_Circular_Shell((Radius * 3d) + (item.Radius * 2));

            item.PhysicsBody.Position = PositionWorld + offset;
            item.PhysicsBody.Velocity = VelocityWorld + (offset.ToUnit(false) * (VelocityWorld.Length * .25d));     // don't perfectly mirror this bot's velocity, push it away as well (otherwise it's too easy to run into and pick back up)
        }

        private void AttachWeapon_UnhookPrevious()
        {
            if (_weaponAttachJoint != null)
            {
                _weaponAttachJoint.Dispose();
            }

            _weaponAttachJoint = null;
        }
        private void AttachWeapon_HookupNew(Weapon newWeapon)
        {
            _weapon = newWeapon;

            if (_weapon != null)
            {
                Point3D position = this.PositionWorld;

                // Move the weapon into position
                _weapon.MoveToAttachPoint(position);

                JointBallAndSocket ballAndSocket = JointBallAndSocket.CreateBallAndSocket(_world, position, this.PhysicsBody, _weapon.PhysicsBody);
                ballAndSocket.ShouldLinkedBodiesCollideEachOther = false;

                _weaponAttachJoint = ballAndSocket;

                _weapon.Gravity = _gravity;
            }
        }

        private void AttachWeapon_HandOffWeapon(Weapon existing, ItemToFrom existingTo)
        {
            existing.Gravity = null;

            existing.ShowAttachPoint = true;

            switch (existingTo)
            {
                case ItemToFrom.Nowhere:
                    // Dispose the existing weapon
                    if (SHOULDKEEP2D) _keepItems2D.Remove(existing);
                    if (_viewport != null && existing.Visuals3D != null)
                    {
                        _viewport.Children.RemoveAll(existing.Visuals3D);
                    }
                    existing.Dispose();
                    break;

                case ItemToFrom.Map:
                    // Give the weapon back to the map
                    SetDropOffset(existing);

                    if (_viewport != null && existing.Visuals3D != null)
                    {
                        _viewport.Children.RemoveAll(existing.Visuals3D);       // Map adds to the viewport, so remove from viewport first
                    }
                    _map.AddItem(existing);

                    // no need to add to _keepItems2D, because the existing was added to it when hooked up to this bot
                    break;

                case ItemToFrom.Inventory:
                    // Clone the weapon, but no physics
                    Weapon nonPhysics = new Weapon(existing.DNA, new Point3D(), null, _materialIDs.Weapon);

                    // Dispose the physics object
                    if (SHOULDKEEP2D) _keepItems2D.Remove(existing);
                    if (_viewport != null && existing.PhysicsBody.Visuals != null)
                    {
                        _viewport.Children.RemoveAll(existing.PhysicsBody.Visuals);
                    }
                    existing.Dispose();

                    // Add to inventory
                    this.Inventory.Weapons.Add(nonPhysics);
                    break;

                default:
                    throw new ApplicationException("Unknown ItemToFrom: " + existingTo.ToString());
            }
        }
        private void AttachWeapon_TakeNewWeapon(ref Weapon newWeapon, ItemToFrom newFrom)
        {
            if (newWeapon == null)
            {
                return;
            }

            newWeapon.ShowAttachPoint = false;

            switch (newFrom)
            {
                case ItemToFrom.Nowhere:
                    if (SHOULDKEEP2D) _keepItems2D.Add(newWeapon, true);
                    if (_viewport != null && newWeapon.Visuals3D != null)
                    {
                        _viewport.Children.AddRange(newWeapon.Visuals3D);
                    }
                    break;

                case ItemToFrom.Map:
                    _map.RemoveItem(newWeapon, true, false);     // this also removes from the viewport
                    if (_viewport != null && newWeapon.Visuals3D != null)
                    {
                        _viewport.Children.AddRange(newWeapon.Visuals3D);       // put the visuals back
                    }
                    break;

                case ItemToFrom.Inventory:
                    // Make sure it's in the inventory
                    if (this.Inventory.Weapons.Remove(newWeapon))
                    {
                        // Convert from graphics only to a physics weapon
                        Weapon physicsWeapon = new Weapon(newWeapon.DNA, new Point3D(), _world, _materialIDs.Weapon);

                        // Swap them
                        newWeapon.Dispose();
                        newWeapon = physicsWeapon;

                        newWeapon.ShowAttachPoint = false;

                        if (SHOULDKEEP2D) _keepItems2D.Add(newWeapon, true);
                        if (_viewport != null && newWeapon.Visuals3D != null)
                        {
                            _viewport.Children.AddRange(newWeapon.Visuals3D);
                        }
                    }
                    break;

                default:
                    throw new ApplicationException("Unknown ItemToFrom: " + newFrom.ToString());
            }
        }

        /// <summary>
        /// This clones the dna, and makes sure it's properly filled out
        /// </summary>
        private static BotDNA GetFinalDNA(BotDNA dna)
        {
            BotDNA retVal = null;
            if (dna == null)
            {
                retVal = new BotDNA();
            }
            else
            {
                retVal = UtilityCore.Clone(dna);
            }

            if (retVal.UniqueID == Guid.Empty)
            {
                retVal.UniqueID = Guid.NewGuid();
            }

            retVal.Name = retVal.Name ?? "";

            retVal.DraggingMaxVelocity = retVal.DraggingMaxVelocity ?? StaticRandom.NextPercent(10, .25);
            retVal.DraggingMultiplier = retVal.DraggingMultiplier ?? StaticRandom.NextPercent(40, .25);

            if (retVal.ShellColors == null)
            {
                retVal.ShellColors = BotShellColorsDNA.GetRandomColors();
            }

            retVal.Ram = RamWeaponDNA.Fix(retVal.Ram);

            return retVal;
        }

        #endregion
    }

    #region class: SpriteGraphic_Shells

    public class SpriteGraphic_Shells
    {
        #region Declaration Section

        private readonly bool _isBot;
        private readonly BotShellColorsDNA _colors;
        private readonly ItemOptionsArco _itemOptions;

        private AnimateRotation[] _rotations = null;

        #endregion

        #region Constructor

        /// <summary>
        /// This graphic is for a bot
        /// </summary>
        public SpriteGraphic_Shells(int level, BotShellColorsDNA colors, ItemOptionsArco itemOptions)
        {
            _isBot = true;
            _colors = colors;
            _itemOptions = itemOptions;

            this.Model = new Model3DGroup();

            this.Level = level;     // the property set will create the build the graphic
        }

        /// <summary>
        /// This graphic is for the backdrop panel
        /// </summary>
        public SpriteGraphic_Shells(BotShellColorsDNA colors, int numLayers, double radius, ItemOptionsArco itemOptions)
        {
            _isBot = false;
            _colors = colors;
            _itemOptions = itemOptions;

            this.Model = new Model3DGroup();

            RebuildGraphic(numLayers, radius, 1d, .5d);
        }

        #endregion

        #region Public Properties

        public readonly Model3DGroup Model;

        private int _level = 0;
        public int Level
        {
            get
            {
                return _level;
            }
            set
            {
                if (!_isBot)
                {
                    throw new InvalidOperationException("Setting the level is only valid when this represents a bot");
                }

                _level = value;

                ArcBot.GetSettingsForLevel(out int numLayers, out double radius, out _, out double innerShellAlphaMult, out double outerShellAlphaMult, out _, _level, _itemOptions);

                RebuildGraphic(numLayers, radius, innerShellAlphaMult, outerShellAlphaMult);
            }
        }

        #endregion

        #region Public Methods

        public void Update(double elapsedTime)
        {
            foreach (AnimateRotation rotation in _rotations)
            {
                rotation.Tick(elapsedTime);
            }
        }

        #endregion

        #region Private Methods

        private void RebuildGraphic(int numLayers, double radius, double innerShellAlphaMult, double outerShellAlphaMult)
        {
            BotShellColorsDNA colors = _isBot ? _colors : GetColorsForBackground(_colors, numLayers);

            _rotations = new AnimateRotation[numLayers];

            this.Model.Children.Clear();

            // Light
            this.Model.Children.Add(GetLight(_isBot, colors, radius));

            //GlassLayers_ORIG(this.Model, _rotations, colors, numLayers, radius, _isBot);
            GlassLayers(this.Model, _rotations, colors, numLayers, radius, _isBot, innerShellAlphaMult, outerShellAlphaMult);
        }

        private static BotShellColorsDNA GetColorsForBackground(BotShellColorsDNA colors, int numLayers)
        {
            ColorHSV diffuseHSVReversed = UtilityWPF.ColorFromHex(colors.InnerColorDiffuse).ToHSV();
            diffuseHSVReversed = new ColorHSV(UtilityWPF.GetCappedAngle(diffuseHSVReversed.H - (colors.DiffuseDrift * numLayers)), diffuseHSVReversed.S, diffuseHSVReversed.V);

            BotShellColorsDNA retVal = new BotShellColorsDNA()
            {
                // The diffuse drift needs to be reversed, because the inner color is expected to be the color passed in.  But the non bot shell
                // is built in reverse order (so that semi transparency will work properly)
                InnerColorDiffuse = diffuseHSVReversed.ToRGB().ToHex(),
                DiffuseDrift = colors.DiffuseDrift * -1,

                EmissiveColor = colors.EmissiveColor,
                Light = colors.Light,
            };

            return retVal;
        }

        private static PointLight GetLight(bool isBot, BotShellColorsDNA colors, double radius)
        {
            Color lightColor = UtilityWPF.ColorFromHex(colors.Light);
            if (!isBot)
            {
                // Make the light very passive when it's not a bot, otherwise everything will reflect that color.  Instead, use that color for the specular
                lightColor = UtilityWPF.AlphaBlend(UtilityWPF.AlphaBlend(Colors.White, lightColor, .85d), Colors.Transparent, .5d);
            }

            PointLight retVal = new PointLight(lightColor, new Point3D(0, 0, 0));

            UtilityWPF.SetAttenuation(retVal, radius * 10d, .1d);

            return retVal;
        }

        private static void GlassLayers_ORIG(Model3DGroup model, AnimateRotation[] rotations, BotShellColorsDNA colors, int numLayers, double radius, bool isBot)
        {
            int numPoints = isBot ? 50 : 100;

            double alphaMult = 1d;
            ColorHSV diffuseOrigHSV = UtilityWPF.ColorFromHex(colors.InnerColorDiffuse).ToHSV();
            double diffuseHueCurrent = diffuseOrigHSV.H;

            AxisFor forloop = isBot ? new AxisFor(Axis.X, 0, numLayers - 1) : new AxisFor(Axis.X, numLayers - 1, 0);

            foreach (int cntr in forloop.Iterate())
            {
                double percent = 1d;
                if (numLayers > 1)
                {
                    percent = Convert.ToDouble(cntr) / Convert.ToDouble(numLayers - 1);
                }

                double layerRadius = UtilityCore.GetScaledValue_Capped(radius * .33d, radius, 0d, 1d, percent);

                // Material
                MaterialGroup materials = new MaterialGroup();
                materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.HSVtoRGB(Convert.ToByte(128 * alphaMult), diffuseHueCurrent, diffuseOrigHSV.S, diffuseOrigHSV.V))));

                if (isBot)
                {
                    materials.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromArgb(Convert.ToByte(96 * alphaMult), 255, 255, 255)), 3d));
                }
                else
                {
                    Color specularColor = UtilityWPF.ColorFromHex(colors.Light);

                    materials.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromArgb(Convert.ToByte(32 * alphaMult), specularColor.R, specularColor.G, specularColor.B)), 3d));
                }

                Color emissive = UtilityWPF.ColorFromHex(colors.EmissiveColor);
                materials.Children.Add(new EmissiveMaterial(new SolidColorBrush(Color.FromArgb(Convert.ToByte(32 * alphaMult), emissive.R, emissive.G, emissive.B))));

                // Geometry Model
                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = materials;
                geometry.BackMaterial = materials;
                geometry.Geometry = GetLayer(layerRadius, numPoints, !isBot);

                double rotateRate = StaticRandom.NextDouble(12d, 50d);      // no need for randomly negating the angle.  if the axis were pointing in the opposite direction, that would be the same as negating
                if (!isBot)
                {
                    rotateRate /= 100d;
                }
                QuaternionRotation3D rotateTransform = new QuaternionRotation3D(Math3D.GetRandomRotation());

                geometry.Transform = new RotateTransform3D(rotateTransform);

                rotations[cntr] = AnimateRotation.Create_AnyOrientation_LimitChange(rotateTransform, 12, rotateRate, 417);

                model.Children.Add(geometry);

                // Alpha of each layer needs to fade away, but need to guarantee that if there is only one layer, it's the most opaque setting
                alphaMult *= .85d;

                // Drift the hue
                diffuseHueCurrent += colors.DiffuseDrift;
                if (diffuseHueCurrent < 0)
                {
                    diffuseHueCurrent += 360;
                }
                else if (diffuseHueCurrent > 360)
                {
                    diffuseHueCurrent -= 360;
                }
            }
        }
        private static void GlassLayers(Model3DGroup model, AnimateRotation[] rotations, BotShellColorsDNA colors, int numLayers, double radius, bool isBot, double innerShellAlphaMult, double outerShellAlphaMult)
        {
            int numPoints = isBot ? 50 : 100;



            //double alphaMult = 1d;
            double alphaMult = numLayers == 1 ? outerShellAlphaMult : innerShellAlphaMult;        // when there's only one, the outer shell is it
            double alphaAdd = -1d * ((innerShellAlphaMult - outerShellAlphaMult) / (numLayers - 1));


            ColorHSV diffuseOrigHSV = UtilityWPF.ColorFromHex(colors.InnerColorDiffuse).ToHSV();
            double diffuseHueCurrent = diffuseOrigHSV.H;

            AxisFor forloop = isBot ? new AxisFor(Axis.X, 0, numLayers - 1) : new AxisFor(Axis.X, numLayers - 1, 0);

            foreach (int cntr in forloop.Iterate())
            {
                double percent = 1d;
                if (numLayers > 1)
                {
                    percent = Convert.ToDouble(cntr) / Convert.ToDouble(numLayers - 1);
                }

                double layerRadius = UtilityCore.GetScaledValue_Capped(radius * .33d, radius, 0d, 1d, percent);

                // Material
                MaterialGroup materials = new MaterialGroup();
                materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.HSVtoRGB(Convert.ToByte(128 * alphaMult), diffuseHueCurrent, diffuseOrigHSV.S, diffuseOrigHSV.V))));

                if (isBot)
                {
                    materials.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromArgb(Convert.ToByte(96 * alphaMult), 255, 255, 255)), 3d));
                }
                else
                {
                    Color specularColor = UtilityWPF.ColorFromHex(colors.Light);

                    materials.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromArgb(Convert.ToByte(32 * alphaMult), specularColor.R, specularColor.G, specularColor.B)), 3d));
                }

                Color emissive = UtilityWPF.ColorFromHex(colors.EmissiveColor);
                materials.Children.Add(new EmissiveMaterial(new SolidColorBrush(Color.FromArgb(Convert.ToByte(32 * alphaMult), emissive.R, emissive.G, emissive.B))));

                // Geometry Model
                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = materials;
                geometry.BackMaterial = materials;
                geometry.Geometry = GetLayer(layerRadius, numPoints, !isBot);

                double rotateRate = StaticRandom.NextDouble(12d, 50d);      // no need for randomly negating the angle.  if the axis were pointing in the opposite direction, that would be the same as negating
                if (!isBot)
                {
                    rotateRate /= 100d;
                }
                QuaternionRotation3D rotateTransform = new QuaternionRotation3D(Math3D.GetRandomRotation());

                geometry.Transform = new RotateTransform3D(rotateTransform);

                rotations[cntr] = AnimateRotation.Create_AnyOrientation_LimitChange(rotateTransform, 12, rotateRate, 417);

                model.Children.Add(geometry);



                // Alpha of each layer needs to fade away, but need to guarantee that if there is only one layer, it's the most opaque setting
                //alphaMult *= .85d;

                // Instead of exponential decay, just do a linear
                alphaMult += alphaAdd;



                // Drift the hue
                diffuseHueCurrent += colors.DiffuseDrift;
                if (diffuseHueCurrent < 0)
                {
                    diffuseHueCurrent += 360;
                }
                else if (diffuseHueCurrent > 360)
                {
                    diffuseHueCurrent -= 360;
                }
            }
        }

        private static MeshGeometry3D GetLayer(double radius, int numPoints, bool independentFaces)
        {
            double minRadius = radius * .9d;

            Point3D[] points = Enumerable.Range(0, numPoints).Select(o => Math3D.GetRandomVector_Spherical(minRadius, radius).ToPoint()).ToArray();

            var hull = Math3D.GetConvexHull(points);

            if (independentFaces)
            {
                return UtilityWPF.GetMeshFromTriangles_IndependentFaces(hull);
            }
            else
            {
                return UtilityWPF.GetMeshFromTriangles(hull);
            }
        }

        #endregion
    }

    #endregion

    #region class: BotDNA

    public class BotDNA
    {
        /// <summary>
        /// This is generated when first created, but then needs to persist for the life of the bot, across saves/loads
        /// </summary>
        public Guid UniqueID { get; set; }

        /// <summary>
        /// This doesn't have to be unique, just giving the user a chance to name it
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// This is a way to give a lineage of bots the same or similar names
        /// </summary>
        /// <remarks>
        /// With asexual reproduction, just give the Gen0 bot a guid, and copy that same guid to all descendents
        /// 
        /// For multiple parents, there may not be a good way.  Maybe have some mashup algorithm, so if the parents are the same
        /// lineage/species, then the mashed string will be the same as the parents.  If the parents are similar, the string would also be
        /// similar, etc.
        /// 
        /// When the time comes for multiple parents, you may want another property that holds a family tree up to N steps back
        /// (but that tree will have N^2 nodes, so can't go too far back - even more if there are more than 2 parents at a time)
        /// </remarks>
        public string Lineage
        {
            get;
            set;
        }
        /// <summary>
        /// This gives a hint for how many ancestors this bot came from
        /// </summary>
        /// <remarks>
        /// For asexual reproduction, this is simple.  But for sexual, it's not so accurate - should probably either use parent's max + 1, or avg + 1
        /// </remarks>
        public long Generation
        {
            get;
            set;
        }

        //public BotLevelDNA Level { get; set; }

        public double? DraggingMaxVelocity { get; set; }
        public double? DraggingMultiplier { get; set; }

        public BotShellColorsDNA ShellColors { get; set; }

        public RamWeaponDNA Ram { get; set; }

        public ShipPartDNA[] Parts { get; set; }

        // Level - also the number of shells (or somehow derived from level)

        // XP - not just a single value, various categories:
        //      one head rods
        //      two head rods
        //          attach point near end (this should probably be considered the same as a one head rod. test to see if it plays different)
        //          attach point near center mass
        //      rope
        //          rope + head
        //          rods + ropes + heads
        //      extendable rod
        //      extendable rope
        //
        //      each head type should have its own category (sword, hammer, etc)
        //
        //      any category of spells


        // HP
        // Max Speed
        // Strength - also equates to acceleration

        // InventoryItems[] - this will probably have to be of type object

        // MuleDNA[] - each mule will have its own inventory
    }

    #endregion
    #region class: BotShellColorsDNA

    public class BotShellColorsDNA
    {
        public string InnerColorDiffuse { get; set; }
        public double DiffuseDrift { get; set; }
        public string EmissiveColor { get; set; }
        public string Light { get; set; }

        public static BotShellColorsDNA GetRandomColors()
        {
            BotShellColorsDNA retVal = new BotShellColorsDNA();

            retVal.InnerColorDiffuse = UtilityWPF.GetRandomColor(0, 96).ToHex();
            retVal.DiffuseDrift = Math1D.GetNearZeroValue(60);      // up to 60 degree drift in hue between shells

            retVal.EmissiveColor = UtilityWPF.GetRandomColor(64, 192).ToHex();

            retVal.Light = UtilityWPF.GetRandomColor(128, 255).ToHex();

            return retVal;
        }
    }

    #endregion
    #region class: BotLevelDNA

    //TODO: Finish this.  This holds how they've leveled up - which aspects get boosted
    public class BotLevelDNA
    {
        //public double? HitPoints { get; set; }
        //public double? RamDamage { get; set; }
        //public double? Strength { get; set; }
        //public double? Speed { get; set; }

        //public double? WeaponHandleDmg { get; set; }
        //blunt, blade, etc
    }

    #endregion
}
