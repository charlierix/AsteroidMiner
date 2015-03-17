using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.ShipEditor;
using Game.HelperClassesWPF;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.Arcanorum
{
    /// <summary>
    /// This runs a world in its own thread
    /// </summary>
    public class OfflineWorld : IDisposable
    {
        #region Class: WorldVars

        private class WorldVars
        {
            public Point3D BoundryMin;
            public Point3D BoundryMax;

            public World World;
            public Map Map;
            public GravityFieldUniform Gravity;

            public UpdateManager UpdateManager;

            public EditorOptions EditorOptions;
            public ItemOptionsArco ItemOptions;

            public MaterialManager MaterialManager;
            public MaterialIDs MaterialIDs;

            public DragHitShape DragPlane;

            public KeepItems2D Keep2D;
        }

        #endregion

        #region Delegates

        public delegate void ItemAddedHandler(IMapObject item);

        public delegate void ItemRemovedHandler();

        #endregion

        #region Class: AddArgs

        public abstract class AddArgs
        {
            public AddArgs(Point3D position, Vector3D? angularVelocity, ItemAddedHandler itemAdded)
            {
                this.Position = position;
                this.AngularVelocity = angularVelocity;

                this.ItemAdded = itemAdded;
            }

            public readonly Point3D Position;
            public readonly Vector3D? AngularVelocity;

            public readonly ItemAddedHandler ItemAdded;
        }

        #endregion
        #region Class: AddBotArgs

        public class AddBotArgs : AddArgs
        {
            public AddBotArgs(Point3D position, Vector3D? angularVelocity, ItemAddedHandler itemAdded, BotDNA bot, int level, Point3D homingPoint, double homingRadius, WeaponDNA attachedWeapon = null, WeaponDNA[] inventoryWeapons = null)
                : base(position, angularVelocity, itemAdded)
            {
                this.Bot = bot;
                this.Level = level;
                this.HomingPoint = homingPoint;
                this.HomingRadius = homingRadius;
                this.AttachedWeapon = attachedWeapon;
                this.InventoryWeapons = inventoryWeapons;
            }

            public readonly BotDNA Bot;

            public readonly int Level;

            public readonly Point3D HomingPoint;
            public readonly double HomingRadius;

            public readonly WeaponDNA AttachedWeapon;
            public readonly WeaponDNA[] InventoryWeapons;
        }

        #endregion
        #region Class: AddWeaponArgs

        public class AddWeaponArgs : AddArgs
        {
            public AddWeaponArgs(Point3D position, Vector3D? angularVelocity, ItemAddedHandler itemAdded, WeaponDNA weapon)
                : base(position, angularVelocity, itemAdded)
            {
                this.Weapon = weapon;
            }

            public WeaponDNA Weapon;
        }

        #endregion

        #region Class: RemoveArgs

        public class RemoveArgs
        {
            public RemoveArgs(long token, ItemRemovedHandler itemRemoved = null)
            {
                this.Token = token;
                this.ItemRemoved = itemRemoved;
            }

            public readonly long Token;
            public readonly ItemRemovedHandler ItemRemoved;
        }

        #endregion

        #region Declaration Section

        private readonly TimerCreateThread<WorldVars> _thread;

        private volatile WorldVars _vars = null;

        private ConcurrentQueue<Tuple<object, bool>> _addRemoves = new ConcurrentQueue<Tuple<object, bool>>();

        #endregion

        #region Constructor

        public OfflineWorld(double size, ItemOptionsArco itemOptions)
        {
            const int INTERVAL = 50;

            this.Size = size;

            _thread = new TimerCreateThread<WorldVars>(Prep, Tick, TearDown,
                itemOptions, size, INTERVAL);

            _thread.Interval = INTERVAL;

            _thread.Start();
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _thread.Dispose();
            }
        }

        #endregion

        #region Public Properties

        public Map Map
        {
            get
            {
                return _vars.Map;
            }
        }

        public readonly double Size;

        #endregion

        #region Public Methods

        public void Add(AddBotArgs bot)
        {
            _addRemoves.Enqueue(new Tuple<object, bool>(bot, true));
        }
        public void Add(AddWeaponArgs weapon)
        {
            _addRemoves.Enqueue(new Tuple<object, bool>(weapon, true));
        }

        public void Remove(RemoveArgs item)
        {
            _addRemoves.Enqueue(new Tuple<object, bool>(item, false));
        }

        #endregion

        #region Private Methods

        private WorldVars Prep(params object[] args)
        {
            // Cast the args
            ItemOptionsArco itemOptions = (ItemOptionsArco)args[0];
            double boundrySize = (double)args[1];
            int interval = (int)args[2];

            // Build the return object
            WorldVars retVal = new WorldVars();

            #region Misc

            retVal.ItemOptions = itemOptions;

            retVal.Gravity = new GravityFieldUniform() { Gravity = new Vector3D(0, -retVal.ItemOptions.Gravity, 0) };

            retVal.EditorOptions = new EditorOptions();

            #endregion
            #region Init World

            double boundrySizeHalf = boundrySize / 2d;

            retVal.BoundryMin = new Point3D(-boundrySizeHalf, -boundrySizeHalf, -boundrySizeHalf);
            retVal.BoundryMax = new Point3D(boundrySizeHalf, boundrySizeHalf, boundrySizeHalf);

            retVal.World = new World(false);

            List<Point3D[]> innerLines, outerLines;
            retVal.World.SetCollisionBoundry(out innerLines, out outerLines, retVal.BoundryMin, retVal.BoundryMax);

            #endregion
            #region Materials

            retVal.MaterialManager = new MaterialManager(retVal.World);
            retVal.MaterialIDs = new MaterialIDs();

            // Wall
            Game.Newt.v2.NewtonDynamics.Material material = new Game.Newt.v2.NewtonDynamics.Material();
            material.Elasticity = ItemOptionsArco.ELASTICITY_WALL;
            retVal.MaterialIDs.Wall = retVal.MaterialManager.AddMaterial(material);

            // Bot
            material = new Game.Newt.v2.NewtonDynamics.Material();
            retVal.MaterialIDs.Bot = retVal.MaterialManager.AddMaterial(material);

            // Bot Ram
            material = new Game.Newt.v2.NewtonDynamics.Material();
            material.Elasticity = ItemOptionsArco.ELASTICITY_BOTRAM;
            retVal.MaterialIDs.BotRam = retVal.MaterialManager.AddMaterial(material);

            // Exploding Bot
            material = new Game.Newt.v2.NewtonDynamics.Material();
            material.IsCollidable = false;
            retVal.MaterialIDs.ExplodingBot = retVal.MaterialManager.AddMaterial(material);

            // Weapon
            material = new Game.Newt.v2.NewtonDynamics.Material();
            retVal.MaterialIDs.Weapon = retVal.MaterialManager.AddMaterial(material);

            // Treasure Box
            material = new Game.Newt.v2.NewtonDynamics.Material();
            retVal.MaterialIDs.TreasureBox = retVal.MaterialManager.AddMaterial(material);

            //// Collisions
            //_materialManager.RegisterCollisionEvent(_materialIDs.Bot, _materialIDs.Bot, Collision_BotBot);
            //_materialManager.RegisterCollisionEvent(_materialIDs.Bot, _materialIDs.Weapon, Collision_BotWeapon);
            //_materialManager.RegisterCollisionEvent(_materialIDs.Weapon, _materialIDs.Weapon, Collision_WeaponWeapon);
            ////_materialManager.RegisterCollisionEvent(_materialIDs.Bot, _materialIDs.Wall, Collision_BotWall);
            //_materialManager.RegisterCollisionEvent(_materialIDs.Weapon, _materialIDs.Wall, Collision_WeaponWall);
            //_materialManager.RegisterCollisionEvent(_materialIDs.Weapon, _materialIDs.TreasureBox, Collision_WeaponTreasureBox);
            //_materialManager.RegisterCollisionEvent(_materialIDs.Bot, _materialIDs.TreasureBox, Collision_BotTreasureBox);

            #endregion
            #region Map

            retVal.Map = new Map(null, null, retVal.World);
            retVal.Map.ShouldBuildSnapshots = true;

            #endregion
            #region Keep 2D

            //TODO: drag plane should either be a plane or a large cylinder, based on the current (level|scene|stage|area|arena|map|place|region|zone)

            // This game is 3D emulating 2D, so always have the mouse go to the XY plane
            retVal.DragPlane = new DragHitShape();
            retVal.DragPlane.SetShape_Plane(new Triangle(new Point3D(-1, -1, 0), new Point3D(1, -1, 0), new Point3D(0, 1, 0)));

            // This will keep objects onto that plane using forces (not velocities)
            retVal.Keep2D = new KeepItems2D();
            retVal.Keep2D.SnapShape = retVal.DragPlane;

            #endregion
            #region Update Manager

            retVal.UpdateManager = new UpdateManager(
                new Type[] { typeof(Bot), typeof(BotNPC) },
                new Type[] { typeof(Bot), typeof(BotNPC) },
                retVal.Map, interval);

            #endregion

            retVal.World.UnPause();

            // Store this at the class level so that public methods can get access to it
            _vars = retVal;

            return retVal;
        }
        private static void TearDown(WorldVars arg)
        {
            arg.World.Pause();

            arg.UpdateManager.Dispose();

            arg.Keep2D.Dispose();

            arg.Map.Dispose();		// this will dispose the physics bodies
            arg.Map = null;

            arg.World.Dispose();
            arg.World = null;
        }

        private void Tick(WorldVars arg, double elapsedTime)
        {
            arg.World.Update();

            arg.UpdateManager.Update_MainThread(elapsedTime);

            Tuple<object, bool> item;
            while (_addRemoves.TryDequeue(out item))
            {
                if (item.Item2)
                {
                    #region Add

                    if (item.Item1 is AddBotArgs)
                    {
                        AddBot((AddBotArgs)item.Item1, arg);
                    }
                    else if (item.Item1 is AddWeaponArgs)
                    {
                        AddWeapon((AddWeaponArgs)item.Item1, arg);
                    }
                    else
                    {
                        throw new ApplicationException("Unknown type of item to add: " + item.Item1.GetType().ToString());
                    }

                    #endregion
                }
                else
                {
                    #region Remove

                    //NOTE: Currently, only token is valid for remove
                    RemoveArgs token = (RemoveArgs)item.Item1;

                    IMapObject mapObject = arg.Map.GetItem_UnknownType(token.Token);

                    if (mapObject != null)
                    {
                        arg.Map.RemoveItem(mapObject, false);
                    }

                    // Call delegate
                    if (token.ItemRemoved != null)
                    {
                        token.ItemRemoved();
                    }

                    #endregion
                }
            }
        }

        private static void AddBot(AddBotArgs dna, WorldVars arg)
        {
            // Bot
            BotNPC bot = new BotNPC(dna.Bot, dna.Level, dna.Position, arg.World, arg.Map, arg.Keep2D, arg.MaterialIDs, null, arg.EditorOptions, arg.ItemOptions, arg.Gravity, arg.DragPlane, dna.HomingPoint, dna.HomingRadius, true, true);

            if (dna.AngularVelocity != null)
            {
                bot.PhysicsBody.AngularVelocity = dna.AngularVelocity.Value;
            }

            arg.Map.AddItem(bot);

            // Attached Weapon
            if (dna.AttachedWeapon != null)
            {
                Weapon weapon = new Weapon(dna.AttachedWeapon, new Point3D(0, 0, 0), arg.World, arg.MaterialIDs.Weapon);

                bot.AttachWeapon(weapon);
            }

            // Inventory Weapons
            if (dna.InventoryWeapons != null)
            {
                foreach (var weapDNA in dna.InventoryWeapons)
                {
                    Weapon weapon = new Weapon(weapDNA, new Point3D(0, 0, 0), arg.World, arg.MaterialIDs.Weapon);

                    bot.AddToInventory(weapon);
                }
            }

            // Call delegate
            if (dna.ItemAdded != null)
            {
                dna.ItemAdded(bot);
            }
        }
        private static void AddWeapon(AddWeaponArgs dna, WorldVars arg)
        {
            Weapon weapon = new Weapon(dna.Weapon, dna.Position, arg.World, arg.MaterialIDs.Weapon);

            if (dna.AngularVelocity != null)
            {
                weapon.PhysicsBody.AngularVelocity = dna.AngularVelocity.Value;
            }

            arg.Keep2D.Add(weapon, false);
            arg.Map.AddItem(weapon);

            // Call delegate
            if (dna.ItemAdded != null)
            {
                dna.ItemAdded(weapon);
            }
        }

        #endregion
    }
}
