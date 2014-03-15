using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Game.HelperClasses;
using Game.Newt.AsteroidMiner2;
using Game.Newt.AsteroidMiner2.ShipEditor;
using Game.Newt.AsteroidMiner2.ShipParts;
using Game.Newt.HelperClasses;
using Game.Newt.NewtonDynamics;

namespace Game.Newt.Testers.Arcanorum
{
    //TODO: Have treausre mules that follow the player - if multiple, use swarming behavior
    //TODO: The number of shells should be based on a player's experience level
    //TODO: Adjust color intensity based on health
    //TODO: Adjust spin rate based on energy/chi

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
    public class Bot : IMapObject, IPartUpdatable, IGivesDamage, ITakesDamage, IDisposable
    {
        #region Enum: ItemToFrom

        public enum ItemToFrom
        {
            Nowhere,
            Map,
            Inventory
        }

        #endregion
        #region Class: PartContainer

        protected class PartContainer
        {
            public List<SensorVision> Vision = new List<SensorVision>();
            public List<Brain> Brain = new List<Brain>();

            // --------------- Part Groups
            public PartBase[] AllParts = null;

            public IPartUpdatable[] UpdatableParts = null;
            /// <summary>
            /// This is the same size as UpdatableParts, and is used to process them in a random order each tick (it's stored as a member
            /// level variable so the array only needs to be allocated once)
            /// </summary>
            /// <remarks>
            /// NOTE: UtilityHelper.RandomRange has this same functionality, but would need to rebuild this array each time it's
            /// called.  So this logic is duplicated to save the processor
            /// </remarks>
            public int[] UpdatableIndices = null;

            public NeuralUtility.ContainerOutput[] Links = null;
            public NeuralBucket LinkBucket = null;
        }

        #endregion

        #region Declaration Section

        protected readonly World _world;
        private readonly Map _map;
        private readonly KeepItems2D _keepItems2D;
        private readonly MaterialIDs _materialIDs;
        private readonly Viewport3D _viewport;

        private JointBase _weaponAttachJoint = null;

        private SpriteGraphic_Shells _graphic = null;

        private readonly IGravityField _gravity;

        private SortedList<long, double> _weaponsColliding = new SortedList<long, double>();

        /// <summary>
        /// Parts are built async, but some require the physics body, so this goes through states:
        /// null=initial
        /// false=parts built callback finished
        /// true=parts have been finalized
        /// </summary>
        private bool? _hasFinishedParts = null;

        #endregion

        #region Constructor

        /// <summary>
        /// NOTE: This doesn't add itself to the map or viewport, those are passed in so that it can add/remove equipment
        /// </summary>
        public Bot(BotDNA dna, Point3D position, World world, Map map, KeepItems2D keepItems2D, MaterialIDs materialIDs, Viewport3D viewport, EditorOptions editorOptions, ItemOptionsArco itemOptions, IGravityField gravity, bool runNeural, bool repairPartPositions)
        {
            const double RADIUS = .5d;
            const double DENSITY = 100d;

            _world = world;
            _map = map;
            _keepItems2D = keepItems2D;
            _materialIDs = materialIDs;
            _viewport = viewport;
            _gravity = gravity;

            this.Inventory = new Inventory();

            this.DNA = GetFinalDNA(dna);        // this needs to be done before parts are created, because when the async comes back, it will add the part dna to it

            #region Parts

            if (dna != null && dna.Parts != null)
            {
                var task = BuildParts(dna.Parts, runNeural, repairPartPositions, world, editorOptions, itemOptions, gravity, map);

                task.ContinueWith(t =>
                    {
                        _hasFinishedParts = false;

                        if (t.Result != null)
                        {
                            this.Parts = t.Result.Item1;
                            this.DNA.Parts = t.Result.Item2;        //NOTE: Parts were already cloned from the dna passed into the constructor.  This replaces with the final (throws out parts that are too small, fills in links, makes sure parts aren't intersecting)

                            foreach (var part in this.Parts.AllParts)
                            {
                                part.RequestWorldLocation += new EventHandler<PartRequestWorldLocationArgs>(Part_RequestWorldLocation);
                                part.RequestWorldSpeed += new EventHandler<PartRequestWorldSpeedArgs>(Part_RequestWorldSpeed);
                            }
                        }
                    });//, TaskScheduler.FromCurrentSynchronizationContext());
            }

            #endregion

            #region WPF Model

            _graphic = new SpriteGraphic_Shells(5, RADIUS, BotShellColorsDNA.GetRandomColors(), true);

            this.Model = _graphic.Model;

            // Model Visual
            ModelVisual3D visual = new ModelVisual3D();        // this is the expensive one, so as few of these should be made as possible
            visual.Content = this.Model;

            #endregion

            #region Physics Body

            Transform3DGroup transform = new Transform3DGroup();
            //transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 90)));
            transform.Children.Add(new TranslateTransform3D(position.ToVector()));

            double volume = 4d / 3d * Math.PI * RADIUS * RADIUS * RADIUS;
            double mass = volume * DENSITY;

            using (CollisionHull hull = CollisionHull.CreateSphere(world, 0, new Vector3D(RADIUS, RADIUS, RADIUS), null))
            {
                this.PhysicsBody = new Body(hull, transform.Value, mass, new Visual3D[] { visual });
                this.PhysicsBody.MaterialGroupID = _materialIDs.Bot;
                this.PhysicsBody.LinearDamping = .01f;
                this.PhysicsBody.AngularDamping = new Vector3D(.01f, .01f, .01f);

                //this.PhysicsBody.ApplyForce += new BodyForceEventHandler(Body_ApplyForce);
            }

            #endregion

            this.Radius = RADIUS;

            //TODO: Calculate these based on dna
            this.ReceiveDamageMultipliers = new WeaponDamage();

            this.HitPoints = new Container() { QuantityMax = this.DNA.HitPoints.Value, QuantityCurrent = this.DNA.HitPoints.Value };

            // This will keep the player tied to the plane using velocities (not forces)
            this.DraggingBot = new MapObjectChaseVelocity(this);
            this.DraggingBot.MaxVelocity = this.DNA.DraggingMaxVelocity.Value;
            this.DraggingBot.Multiplier = this.DNA.DraggingMultiplier.Value;

            // Ram
            this.Ram = new RamWeapon(this.DNA.Ram, this, viewport);

            this.CreationTime = DateTime.Now;
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

        public void Update(double elapsedTime)
        {
            if (_hasFinishedParts != null && !_hasFinishedParts.Value)
            {
                #region Finish Parts

                _hasFinishedParts = true;

                if (this.Parts != null && this.Parts.Vision != null)
                {
                    foreach (SensorVision vision in this.Parts.Vision)
                    {
                        vision.BotToken = this.Token;       // PhysicsBody needs to be created before this is done, but the parts were built async, so this is a 
                    }
                }

                #endregion
            }

            this.Ram.Update(elapsedTime);
            _graphic.Update(elapsedTime);

            if (this.Parts != null && _hasFinishedParts != null && _hasFinishedParts.Value)
            {
                //TODO: Update the updatable parts in random order
                Ship.UpdateParts_RandomOrder(this.Parts.UpdatableParts, this.Parts.UpdatableIndices, elapsedTime);
            }

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

        #endregion
        #region IGivesDamage Members

        public WeaponDamage CalculateDamage(MaterialCollision[] collisions)
        {
            return this.Ram.CalculateDamage(collisions);
        }

        #endregion
        #region ITakesDamage Members

        public Tuple<bool, WeaponDamage> Damage(WeaponDamage damage, Weapon weapon = null)
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

        /// <summary>
        /// This moves the bot around.  In this.Update, the chase point is set.  This class hooks itself to
        /// this.PhysicsBody.ApplyForceAndTorque, so no need to tell it when to go.  Just keep refreshing
        /// the chase point
        /// </summary>
        public MapObjectChaseVelocity DraggingBot
        {
            get;
            private set;
        }

        public readonly BotDNA DNA;

        /// <summary>
        /// NOTE: This is built async, so could be null for a bit
        /// </summary>
        /// <remarks>
        /// With ship, parts are everything.  The ship is just a wrapper of the parts.
        /// 
        /// But with bot, parts are just for AI
        /// </remarks>
        protected PartContainer Parts = null;

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
            if (newWeapon != null)
            {
                _viewport.Children.RemoveAll(this.Visuals3D);
                _viewport.Children.AddRange(this.Visuals3D);
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
                    _keepItems2D.Remove(newWeapon);
                    _map.RemoveItem(newWeapon);     // the map also removes from the viewport
                    //_viewport.Children.RemoveAll(newWeapon.Visuals3D);
                    newWeapon.Dispose();

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

                        _keepItems2D.Add(physicsWeapon);
                        _map.AddItem(physicsWeapon);
                    }
                    break;

                default:
                    throw new ApplicationException("Unknown ItemToFrom: " + existingTo.ToString());
            }
        }

        #endregion

        #region Event Listeners

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

        //NOTE: This is sort of a copy of Ship.BuildParts
        private async static Task<Tuple<PartContainer, PartDNA[], CollisionHull[]>> BuildParts(PartDNA[] dna, bool runNeural, bool repairPartPositions, World world, EditorOptions editorOptions, ItemOptionsArco itemOptions, IGravityField gravity, Map map)
        {
            if (dna == null)
            {
                return null;
            }

            // Throw out parts that are too small
            PartDNA[] usableParts = dna.Where(o => o.Scale.Length > .01d).ToArray();
            if (usableParts.Length == 0)
            {
                return null;
            }

            PartContainer container = new PartContainer();

            // Create the parts based on dna
            var combined = BuildPartsSprtCreate(container, usableParts, editorOptions, itemOptions, gravity, map);

            Ship.BuildPartsResults results = await Ship.BuildParts_Finish(combined, usableParts, runNeural, repairPartPositions, itemOptions, world);

            container.AllParts = results.AllParts;
            container.UpdatableParts = results.UpdatableParts;
            container.UpdatableIndices = results.UpdatableIndices;
            container.Links = results.Links;

            return Tuple.Create(container, results.DNA, results.Hulls);
        }
        private static List<Tuple<PartBase, PartDNA>> BuildPartsSprtCreate(PartContainer container, PartDNA[] parts, EditorOptions editorOptions, ItemOptionsArco itemOptions, IGravityField gravity, Map map)
        {
            //TODO: Figure this out based on this.Radius
            const double SEARCHRADIUS = 10;

            List<Tuple<PartBase, PartDNA>> retVal = new List<Tuple<PartBase, PartDNA>>();

            //TODO: EnergyTank

            foreach (PartDNA dna in parts)
            {
                switch (dna.PartType)
                {
                    case SensorVision.PARTTYPE:
                        //TODO: Add filter type to the dna, use reflection to find the type
                        BuildPartsSprtAdd(new SensorVision(editorOptions, itemOptions, (PartNeuralDNA)dna, map, SEARCHRADIUS, null),
                            dna, container.Vision, retVal);
                        break;

                    case Brain.PARTTYPE:
                        BuildPartsSprtAdd(new Brain(editorOptions, itemOptions, (PartNeuralDNA)dna, null),// container.EnergyGroup),
                            dna, container.Brain, retVal);
                        break;

                    default:
                        throw new ApplicationException("Unexpected dna.PartType: " + dna.PartType);
                }
            }

            return retVal;
        }
        private static void BuildPartsSprtAdd<T>(T item, PartDNA dna, List<T> specificList, List<Tuple<PartBase, PartDNA>> combinedList) where T : PartBase
        {
            // This is just a helper method so one call adds to two lists
            specificList.Add(item);
            combinedList.Add(new Tuple<PartBase, PartDNA>(item, dna));
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
            Vector3D offset = Math3D.GetRandomVectorSphericalShell((this.Radius * 3d) + item.Radius);

            item.PhysicsBody.Position = this.PositionWorld + offset;
            item.PhysicsBody.Velocity = this.VelocityWorld + (offset.ToUnit(false) * (this.VelocityWorld.Length * .25d));     // don't perfectly mirror this bot's velocity, push it away as well (otherwise it's too easy to run into and pick back up)
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
                    _keepItems2D.Remove(existing);
                    _viewport.Children.RemoveAll(existing.Visuals3D);
                    existing.Dispose();
                    break;

                case ItemToFrom.Map:
                    // Give the weapon back to the map
                    SetDropOffset(existing);

                    _viewport.Children.RemoveAll(existing.Visuals3D);       // Map adds to the viewport, so remove from viewport first
                    _map.AddItem(existing);

                    // no need to add to _keepItems2D, because the existing was added to it when hooked up to this bot
                    break;

                case ItemToFrom.Inventory:
                    // Clone the weapon, but no physics
                    Weapon nonPhysics = new Weapon(existing.DNA, new Point3D(), null, _materialIDs.Weapon);

                    // Dispose the physics object
                    _keepItems2D.Remove(existing);
                    _viewport.Children.RemoveAll(existing.PhysicsBody.Visuals);
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
            if (newWeapon != null)
            {
                newWeapon.ShowAttachPoint = false;

                switch (newFrom)
                {
                    case ItemToFrom.Nowhere:
                        _keepItems2D.Add(newWeapon);
                        _viewport.Children.AddRange(_weapon.Visuals3D);
                        break;

                    case ItemToFrom.Map:
                        _map.RemoveItem(newWeapon);     // this also removes from the viewport
                        _viewport.Children.AddRange(newWeapon.Visuals3D);       // put the visuals back
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

                            _keepItems2D.Add(newWeapon);
                            _viewport.Children.AddRange(newWeapon.Visuals3D);
                        }
                        break;

                    default:
                        throw new ApplicationException("Unknown ItemToFrom: " + newFrom.ToString());
                }
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
                retVal = UtilityHelper.Clone(dna);
            }

            if (retVal.UniqueID == Guid.Empty)
            {
                retVal.UniqueID = Guid.NewGuid();
            }

            retVal.Name = retVal.Name ?? "";

            retVal.HitPoints = retVal.HitPoints ?? StaticRandom.NextPercent(500, .25);

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

    #region Class: SparkGraphic

    public class SpriteGraphic_Shells
    {
        #region Declaration Section

        private AnimateRotation[] _rotations = null;

        #endregion

        #region Constructor

        /// <param name="isBot">
        /// True=This graphic is for a bot
        /// False=This graphic is for the backdrop panel
        /// </param>
        public SpriteGraphic_Shells(int numLayers, double radius, BotShellColorsDNA colors, bool isBot)
        {
            if (!isBot)
            {
                //Color backColor = UtilityWPF.ColorFromHex("404040");
                ColorHSV diffuseHSVReversed = UtilityWPF.ColorFromHex(colors.InnerColorDiffuse).ToHSV();
                diffuseHSVReversed = new ColorHSV(UtilityWPF.GetCappedAngle(diffuseHSVReversed.H - (colors.DiffuseDrift * numLayers)), diffuseHSVReversed.S, diffuseHSVReversed.V);

                colors = new BotShellColorsDNA()
                {
                    // The diffuse drift needs to be reversed, because the inner color is expected to be the color passed in.  But the non bot shell
                    // is built in reverse order (so that semi transparency will work properly)
                    //InnerColorDiffuse = UtilityWPF.AlphaBlend(diffuseHSVReversed.ToRGB(), backColor, .25d).ToHex(),
                    InnerColorDiffuse = diffuseHSVReversed.ToRGB().ToHex(),
                    DiffuseDrift = colors.DiffuseDrift * -1,

                    //EmissiveColor = UtilityWPF.AlphaBlend(UtilityWPF.ColorFromHex(colors.EmissiveColor), backColor, .25d).ToHex(),
                    EmissiveColor = colors.EmissiveColor,
                    Light = colors.Light,
                };
            }

            int numPoints = isBot ? 50 : 100;

            _rotations = new AnimateRotation[numLayers];

            Model3DGroup model = new Model3DGroup();

            // Light
            Color lightColor = UtilityWPF.ColorFromHex(colors.Light);
            if (!isBot)
            {
                // Make the light very passive when it's not a bot, otherwise everything will reflect that color.  Instead, use that color for the specular
                lightColor = UtilityWPF.AlphaBlend(UtilityWPF.AlphaBlend(Colors.White, lightColor, .85d), Colors.Transparent, .5d);
            }
            PointLight pointLight = new PointLight(lightColor, new Point3D(0, 0, 0));
            UtilityWPF.SetAttenuation(pointLight, radius * 10d, .1d);

            model.Children.Add(pointLight);

            #region Glass Layers

            double alphaMult = 1d;
            ColorHSV diffuseOrigHSV = UtilityWPF.ColorFromHex(colors.InnerColorDiffuse).ToHSV();
            double diffuseHueCurrent = diffuseOrigHSV.H;

            AxisFor forloop = isBot ? new AxisFor(Axis.X, 0, numLayers - 1) : new AxisFor(Axis.X, numLayers - 1, 0);

            //for (int cntr = 0; cntr < numLayers; cntr++)
            foreach (int cntr in forloop.Iterate())
            {
                double percent = 1d;
                if (numLayers > 1)
                {
                    percent = Convert.ToDouble(cntr) / Convert.ToDouble(numLayers - 1);
                }

                double layerRadius = UtilityHelper.GetScaledValue_Capped(radius * .33d, radius, 0d, 1d, percent);

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

                _rotations[cntr] = AnimateRotation.Create_AnyOrientation_LimitChange(rotateTransform, 12, rotateRate, 417);

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

            #endregion

            this.Model = model;
        }

        #endregion

        #region Public Properties

        public readonly Model3D Model;

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

        private static MeshGeometry3D GetLayer(double radius, int numPoints, bool independentFaces)
        {
            double minRadius = radius * .9d;

            Point3D[] points = Enumerable.Range(0, numPoints).Select(o => Math3D.GetRandomVectorSpherical(minRadius, radius).ToPoint()).ToArray();

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

    #region Class: BotDNA

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

        public double? HitPoints { get; set; }

        public double? DraggingMaxVelocity { get; set; }
        public double? DraggingMultiplier { get; set; }

        public BotShellColorsDNA ShellColors { get; set; }

        public RamWeaponDNA Ram { get; set; }

        public PartDNA[] Parts { get; set; }

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
    #region Class: BotShellColorsDNA

    public class BotShellColorsDNA
    {
        public string InnerColorDiffuse { get; set; }
        public double DiffuseDrift { get; set; }
        public string EmissiveColor { get; set; }
        public string Light { get; set; }

        public static BotShellColorsDNA GetRandomColors()
        {
            BotShellColorsDNA retVal = new BotShellColorsDNA();

            retVal.InnerColorDiffuse = UtilityWPF.GetRandomColor(255, 0, 96).ToHex();
            retVal.DiffuseDrift = Math3D.GetNearZeroValue(60);      // up to 60 degree drift in hue between shells

            retVal.EmissiveColor = UtilityWPF.GetRandomColor(255, 64, 192).ToHex();

            retVal.Light = UtilityWPF.GetRandomColor(255, 128, 255).ToHex();

            return retVal;
        }
    }

    #endregion
}
