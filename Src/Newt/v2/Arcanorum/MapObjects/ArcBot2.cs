using Game.HelperClassesWPF;
using Game.Newt.v2.Arcanorum.Parts;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.ShipEditor;
using Game.Newt.v2.GameItems.ShipParts;
using Game.Newt.v2.NewtonDynamics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media.Media3D;

namespace Game.Newt.v2.Arcanorum.MapObjects
{
    //TODO: When ArcBot2.PhysicsBody.Position is set manually, the weapon goes crazy.  override that and disconnect/reconnect the weapon
    public class ArcBot2 : Bot
    {
        #region Declaration Section

        private readonly World _world;
        private readonly Map _map;
        private readonly KeepItems2D _keepItems2D;
        private readonly MaterialIDs _materialIDs;
        private readonly Viewport3D _viewport;

        private JointBase _weaponAttachJoint = null;

        private readonly MapObjectTransferArgs _transferArgs;

        #endregion

        #region Constructor/Factory

        /// <param name="construction">Call GetConstruction for an instance of this</param>
        public ArcBot2(BotConstruction_Result construction, MaterialIDs materialIDs, KeepItems2D keepItems2D, Viewport3D viewport)
            : base(construction)
        {
            _world = construction.ArgsCore.World;
            _map = construction.ArgsCore.Map;
            _keepItems2D = keepItems2D;
            _materialIDs = materialIDs;
            _viewport = viewport;

            Inventory = new Inventory();

            //BrainNEAT brainPart = Parts.FirstOrDefault(o => o is BrainNEAT) as BrainNEAT;
            //if (brainPart == null)
            //{
            //    throw new ApplicationException("There needs to be a brain part");
            //}

            //foreach (PartBase part in Parts)
            //{
            //    if (part is SensorHoming partHoming)
            //    {
            //        partHoming.HomePoint = new Point3D(0, 0, 0);
            //        partHoming.HomeRadius = HOMINGRADIUS;
            //    }
            //}

            //int inputCount = brainPart.Neruons_Writeonly.Count();
            //int outputCount = brainPart.Neruons_Readonly.Count();

            #region MapObjectTransferArgs

            _transferArgs = new MapObjectTransferArgs()
            {
                World = _world,
                Map = _map,
                Inventory = Inventory,
                MaterialID_Weapon = _materialIDs.Weapon,
                Bot = this,
                ShouldKeep2D = _keepItems2D != null,
                KeepItems2D = _keepItems2D,
                Gravity = _gravity,
                Viewport = _viewport,
                Visuals3D = Visuals3D,
            };

            #endregion
        }

        /// <summary>
        /// This only needs to be called once for an arcanorum world.  All bots can be handed this same class
        /// </summary>
        public static ShipExtraArgs GetArgs_Extra()
        {
            //TODO: Adjust neuron count ratios so that there are enough neurons in the brain

            ShipExtraArgs extra = new ShipExtraArgs()
            {
                Options = new EditorOptions(),

                ItemOptions = new ItemOptionsArco()
                {
                    VisionSensor_NeuronDensity = 50,
                    BrainNEAT_NeuronDensity_Input = 400,
                },
            };

            extra.Gravity = new GravityFieldUniform()
            {
                Gravity = new Vector3D(0, -((ItemOptionsArco)extra.ItemOptions).Gravity, 0)
            };

            return extra;
        }

        /// <summary>
        /// This will return a constructed arcbot (arcbots have hardcoded dna)
        /// </summary>
        public static BotConstruction_Result GetConstruction(int level, ShipCoreArgs core, ShipExtraArgs extra, DragHitShape dragPlane, bool addHomingSensor)
        {
            BotConstructor_Events events = new BotConstructor_Events
            {
                InstantiateUnknownPart_Standard = InstantiateUnknownPart_Standard,
            };

            object[] botObjects = new object[]
            {
                new ArcBot2_ConstructionProps()
                {
                    DragPlane = dragPlane,
                    Level = level,
                },
            };

            ShipDNA dna = GetDefaultDNA(addHomingSensor);

            return BotConstructor.ConstructBot(dna, core, extra, events, botObjects);
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // This disposes both the weapon and the physics joint
                AttachWeapon(null);
            }

            base.Dispose(disposing);
        }

        #region Public Properties

        //TODO: Weapon attaching can currently only be done on the main thread.  Make it thread safe
        private Weapon _weapon = null;
        public Weapon Weapon => _weapon;

        public Inventory Inventory { get; }

        #endregion

        #region Public Methods

        public static PartBase InstantiateUnknownPart_Standard(ShipPartDNA dna, ShipCoreArgs core, ShipExtraArgs extra, BotConstruction_Containers containers, object[] botObjects)
        {
            ItemOptionsArco itemOptions = (ItemOptionsArco)extra.ItemOptions;

            var constructProps = botObjects?.FirstOrDefault(o => o is ArcBot2_ConstructionProps) as ArcBot2_ConstructionProps;
            if (constructProps == null)
            {
                throw new ApplicationException("ArcBot2_ConstructionProps needs to be given to the bot constructor in the botObjects param");
            }

            double botRadius = itemOptions.RadiusAtLevel[constructProps.Level];

            switch (dna.PartType)
            {
                case MotionController2.PARTTYPE:
                    //if (MotionController.Count > 0)
                    //{
                    //    throw new ApplicationException("There can be only one plate writer in a bot");
                    //}

                    AIMousePlate mousePlate = new AIMousePlate(constructProps.DragPlane);
                    mousePlate.MaxXY = botRadius * 20;
                    mousePlate.Scale = 1;

                    return new MotionController2(extra.Options, itemOptions, dna, mousePlate);

                case SensorVision.PARTTYPE:
                    if (dna is SensorVisionDNA sensorVisionDNA)
                    {
                        return new SensorVision(extra.Options, itemOptions, (SensorVisionDNA)dna, core.Map);
                    }
                    else
                    {
                        throw new ApplicationException($"SensorVision requires SensorVisionDNA: {dna.GetType()}");
                    }

                default:
                    throw new ApplicationException($"Unknown PartType: {dna.PartType}");
            }
        }

        public bool AddToInventory(Weapon newWeapon, ItemToFrom newFrom = ItemToFrom.Nowhere)
        {
            return MapObjectTransfer.AddToInventory(_transferArgs, newWeapon, newFrom);
        }
        public void RemoveFromInventory(Weapon removeWeapon, ItemToFrom existingTo = ItemToFrom.Nowhere)
        {
            MapObjectTransfer.RemoveFromInventory(_transferArgs, removeWeapon, existingTo);
        }

        /// <summary>
        /// This attaches the weapon passed in, and returns the previous weapon.  Also optionally manages the weapon's states within
        /// the map or inventory
        /// </summary>
        /// <param name="newFrom">If from map or inventory, this will remove it from those places</param>
        /// <param name="existingTo">If told to, will add the existing back into inventory or map</param>
        public void AttachWeapon(Weapon newWeapon, ItemToFrom newFrom = ItemToFrom.Nowhere, ItemToFrom existingTo = ItemToFrom.Nowhere)
        {
            MapObjectTransfer.AttachWeapon(_transferArgs, ref _weaponAttachJoint, ref _weapon, newWeapon, newFrom, existingTo);
        }

        /// <summary>
        /// When a weapon is attached, directly setting physicsbody.position can cause the weapon to go crazy.  This function
        /// unattaches and reattaches the weapon
        /// NOTE: The weapon will go back to pointing to the right after this method is called
        /// </summary>
        public void SetPosition(Point3D positionWorld, bool maintainWeaponSpin = true)
        {
            if (_weapon == null)
            {
                PhysicsBody.Position = positionWorld;
                return;
            }

            // This doesn't work
            //if (maintainWeaponSpin)
            //{
            //    Vector3D displacement = _weapon.PhysicsBody.Position - PhysicsBody.Position;
            //    PhysicsBody.Position = positionWorld;
            //    _weapon.PhysicsBody.Position = positionWorld + displacement;
            //    return;
            //}

            Vector3D angular = _weapon.PhysicsBody.AngularVelocity;

            WeaponDNA dna = _weapon.DNA;

            AttachWeapon(null);

            PhysicsBody.Position = positionWorld;

            AttachWeapon(new Weapon(dna, new Point3D(), _world, _materialIDs.Weapon));

            if (maintainWeaponSpin)
            {
                _weapon.PhysicsBody.AngularVelocity = angular;
            }
        }

        #endregion

        #region Private Methods

        private static ShipDNA GetDefaultDNA(bool addHomingSensor)
        {
            // Create bot dna (remember the unique ID of the brainneat part)
            var parts = new List<ShipPartDNA>();

            #region misc

            // If there are more than one, give each a unique guid to know which to train (only want to train one at a time)
            parts.Add(new BrainNEATDNA()
            {
                PartType = BrainNEAT.PARTTYPE,
                Position = new Point3D(0, 0, 0),
                Orientation = Quaternion.Identity,
                Scale = new Vector3D(1, 1, 1)
            });

            parts.Add(new ShipPartDNA()
            {
                PartType = MotionController2.PARTTYPE,
                Position = new Point3D(0, 0, -.65),
                Orientation = Quaternion.Identity,
                Scale = new Vector3D(.75, .75, .75)
            });

            #endregion

            #region cargo parts

            // These go in a ring around the brain

            const double RING_Z = 0;

            Point3D[] positions_cargo = Math3D.GetRandomVectors_Circular_EvenDist(3, .66).
                Select(o => o.ToPoint3D(RING_Z)).
                ToArray();

            Point3D pointTo = new Point3D(0, 0, RING_Z);

            // Make the cargo bay long, because it's storing pole arms
            Vector3D cargo_vect = pointTo - positions_cargo[0];
            Vector3D cargo_vectX = Vector3D.CrossProduct(cargo_vect, new Vector3D(0, 1, 0));
            Vector3D cargo_vectY = pointTo - positions_cargo[0];

            parts.Add(new ShipPartDNA()
            {
                PartType = CargoBay.PARTTYPE,
                Position = positions_cargo[0],
                Orientation = Math3D.GetRotation(new DoubleVector(1, 0, 0, 0, 1, 0), new DoubleVector(cargo_vectX, cargo_vectY)),       // need a double vector because the cargo bay is a box.  The other two are cylinders, so it doesn't matter which way the orth vector is pointing
                Scale = new Vector3D(.35, .15, 1.35)
            });

            parts.Add(new ShipPartDNA()
            {
                PartType = EnergyTank.PARTTYPE,
                Position = positions_cargo[1],
                Orientation = Math3D.GetRotation(new Vector3D(0, 0, 1), pointTo - positions_cargo[1]),
                Scale = new Vector3D(.6, .6, .15)
            });

            parts.Add(new ShipPartDNA()
            {
                PartType = PlasmaTank.PARTTYPE,
                Position = positions_cargo[2],
                Orientation = Math3D.GetRotation(new Vector3D(0, 0, 1), pointTo - positions_cargo[2]),
                Scale = new Vector3D(.6, .6, .15)
            });

            #endregion

            #region sensors

            int sensorCount = 2;
            if (addHomingSensor)
            {
                sensorCount++;
            }

            Point3D[] positions_sensor = Math3D.GetRandomVectors_Cone_EvenDist(sensorCount, new Vector3D(0, 0, 1), 40, 1.1, 1.1).
                Select(o => o.ToPoint() + new Vector3D(0, 0, -.3)).
                ToArray();

            // Vision Far
            parts.Add(new SensorVisionDNA()
            {
                PartType = SensorVision.PARTTYPE,
                Position = positions_sensor[0],
                Orientation = Quaternion.Identity,
                Scale = new Vector3D(1, 1, 1),
                SearchRadius = 60,
                Filters = new[]
                {
                    SensorVisionFilterType.Nest,
                    SensorVisionFilterType.Bot_Family,
                    SensorVisionFilterType.Bot_Other,
                    SensorVisionFilterType.TreasureBox,
                    SensorVisionFilterType.Weapon_FreeFloating,
                },
            });

            // Vision Near
            parts.Add(new SensorVisionDNA()
            {
                PartType = SensorVision.PARTTYPE,
                Position = positions_sensor[1],
                Orientation = Quaternion.Identity,
                Scale = new Vector3D(1, 1, 1),
                SearchRadius = 12,
                Filters = new[]
                {
                    SensorVisionFilterType.Bot_Family,
                    SensorVisionFilterType.Bot_Other,
                    SensorVisionFilterType.Weapon_Attached_Personal,
                    SensorVisionFilterType.Weapon_Attached_Other,
                },
            });

            // Homing
            if (addHomingSensor)
            {
                parts.Add(new ShipPartDNA()
                {
                    PartType = SensorHoming.PARTTYPE,
                    Position = positions_sensor[2],
                    Orientation = Quaternion.Identity,
                    Scale = new Vector3D(1, 1, 1)
                });
            }


            //new ShipPartDNA() { PartType = SensorVision.PARTTYPE, Position = new Point3D(0, 0, 1.5), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1) },
            //new ShipPartDNA() { PartType = SensorCollision.PARTTYPE, Position = new Point3D(-sensorXY, sensorXY, sensorZ), Orientation = new Quaternion(halfSqrt2,0,0,halfSqrt2), Scale = new Vector3D(1, 1, 1) },
            //new ShipPartDNA() { PartType = SensorHoming.PARTTYPE, Position = new Point3D(-sensorXY, -sensorXY, sensorZ), Orientation = new Quaternion(halfSqrt2,0,0,halfSqrt2), Scale = new Vector3D(1, 1, 1) },
            //new ShipPartDNA() { PartType = SensorVelocity.PARTTYPE, Position = new Point3D(sensorXY, -sensorXY, sensorZ), Orientation = new Quaternion(halfSqrt2,0,0,halfSqrt2), Scale = new Vector3D(1, 1, 1) },

            // gravity is constant, so the only reason it would be needed is if the bot spins around
            //new ShipPartDNA() { PartType = SensorGravity.PARTTYPE, Position = new Point3D(sensorXY, sensorXY, sensorZ), Orientation = new Quaternion(halfSqrt2,0,0,halfSqrt2), Scale = new Vector3D(1, 1, 1) },

            // SensorPain

            #endregion

            return ShipDNA.Create("ArcBot2", parts);
        }

        #endregion
    }

    #region class: ArcBot2_ConstructionProps

    /// <summary>
    /// This gets handed to BotConstructor and contains properties about the bot that are beyond the standard interface
    /// of BotConstructor
    /// </summary>
    /// <remarks>
    /// BotConstructor doesn't know about arcanorum parts.  So it takes in object[] botObjects.  Then when it comes across
    /// a part it doesn't know about, it calls a delegate passing in botObjects.
    /// 
    /// So this class holds whatever properties are needed by the arcanorum specific parts
    /// </remarks>
    public class ArcBot2_ConstructionProps
    {
        public DragHitShape DragPlane { get; set; }
        public int Level { get; set; }
    }

    #endregion
}
