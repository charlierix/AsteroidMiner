using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v2.AsteroidMiner.MapParts;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.MapParts;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.AsteroidMiner.AstMin2D
{
    /// <summary>
    /// This keeps the map populated.  Removes stuff if too much, adds if too little
    /// </summary>
    /// <remarks>
    /// TODO: The counts shouldn't be constant.  Allow some feasts and famines
    /// </remarks>
    public class MapPopulationManager : IPartUpdatable
    {
        #region Enum: InstructionType

        private enum InstructionType
        {
            Add,
            Remove,
            Merge
        }

        #endregion
        #region Class: ChangeInstruction

        private class ChangeInstruction
        {
            #region Constructor

            // Add
            public ChangeInstruction(Point3D add_Position, Vector3D add_Velocity, Vector3D add_AngVel, AsteroidDNA asteroid)
                : this(add_Position, add_Velocity, add_AngVel, asteroid, (MineralDNA)null) { }
            public ChangeInstruction(Point3D add_Position, Vector3D add_Velocity, Vector3D add_AngVel, MineralDNA mineral)
                : this(add_Position, add_Velocity, add_AngVel, null, mineral) { }
            private ChangeInstruction(Point3D add_Position, Vector3D add_Velocity, Vector3D add_AngVel, AsteroidDNA asteroid, MineralDNA mineral)
            {
                this.InstructionType = MapPopulationManager.InstructionType.Add;

                this.Add_Position = add_Position;
                this.Add_Velocity = add_Velocity;
                this.Add_AngVel = add_AngVel;

                this.Asteroid = asteroid;
                this.Mineral = mineral;

                this.Remove_Asteroids = null;
                this.Remove_Minerals = null;
            }

            // Remove
            public ChangeInstruction(Asteroid[] remove_Asteroids)
                : this(remove_Asteroids, (Mineral[])null) { }
            public ChangeInstruction(Mineral[] remove_Minerals)
                : this((Asteroid[])null, remove_Minerals) { }
            private ChangeInstruction(Asteroid[] remove_Asteroids, Mineral[] remove_Minerals)
            {
                this.InstructionType = MapPopulationManager.InstructionType.Remove;

                this.Remove_Asteroids = remove_Asteroids;
                this.Remove_Minerals = remove_Minerals;

                this.Add_Position = new Point3D();
                this.Add_Velocity = new Vector3D();
                this.Add_AngVel = new Vector3D();
                this.Asteroid = null;
                this.Mineral = null;
            }

            // Merge
            public ChangeInstruction(Point3D add_Position, Vector3D add_Velocity, Vector3D add_AngVel, AsteroidDNA asteroid, Asteroid[] remove_Asteroids)
                : this(add_Position, add_Velocity, add_AngVel, asteroid, null, remove_Asteroids, null) { }
            public ChangeInstruction(Point3D add_Position, Vector3D add_Velocity, Vector3D add_AngVel, MineralDNA mineral, Mineral[] remove_Minerals)
                : this(add_Position, add_Velocity, add_AngVel, null, mineral, null, remove_Minerals) { }
            private ChangeInstruction(Point3D add_Position, Vector3D add_Velocity, Vector3D add_AngVel, AsteroidDNA asteroid, MineralDNA mineral, Asteroid[] remove_Asteroids, Mineral[] remove_Minerals)
            {
                this.InstructionType = MapPopulationManager.InstructionType.Merge;

                this.Add_Position = add_Position;
                this.Add_Velocity = add_Velocity;
                this.Add_AngVel = add_AngVel;

                this.Asteroid = asteroid;
                this.Mineral = mineral;

                this.Remove_Asteroids = remove_Asteroids;
                this.Remove_Minerals = remove_Minerals;
            }

            #endregion

            public readonly InstructionType InstructionType;

            public readonly Point3D Add_Position;
            public readonly Vector3D Add_Velocity;
            public readonly Vector3D Add_AngVel;

            public readonly AsteroidDNA Asteroid;
            public readonly MineralDNA Mineral;

            public readonly Asteroid[] Remove_Asteroids;
            public readonly Mineral[] Remove_Minerals;
        }

        #endregion

        #region Class: Boundry

        private class Boundry
        {
            public Boundry(Point3D min, Point3D max)
            {
                // Scale these down a little bit
                this.Outer_Min = min.ToVector() * .97;
                this.Outer_Max = max.ToVector() * .97;

                this.Inner_Min = min.ToVector() * .89;
                this.Inner_Max = max.ToVector() * .89;
            }

            public readonly Vector3D Outer_Min;
            public readonly Vector3D Outer_Max;

            public readonly Vector3D Inner_Min;
            public readonly Vector3D Inner_Max;
        }

        #endregion

        #region Declaration Section

        private const int MIN_ASTEROIDS = 33;
        private const int MAX_ASTEROIDS = 80;

        private const int MIN_MINERALS = 5;     // it should get desparate before this class intervenes.  The game is called asteroid miner, not pick up minerals
        private const int MAX_MINERALS = 110;

        private const int MAXCHANGES = 12;

        private readonly Boundry _boundry;

        private readonly Map _map;
        private readonly World _world;
        private readonly SharedVisuals _sharedVisuals = new SharedVisuals();

        private readonly int _material_Asteroid;
        private readonly int _material_Mineral;

        private readonly Func<double, ITriangleIndexed[], double> _getAsteroidMassByRadius;
        private readonly Func<double, MineralDNA[]> _getMineralsByDestroyedMass;

        private readonly double _minChildAsteroidRadius;

        private readonly MineralType[] _mineralTypesByValue;

        /// <summary>
        /// These instructions are generated in an arbitrary thread, then executed in the main thread
        /// </summary>
        private volatile ChangeInstruction[] _instructions = null;

        #endregion

        #region Constructor

        public MapPopulationManager(Map map, World world, Point3D boundryMin, Point3D boundryMax, int material_Asteroid, int material_Mineral, Func<double, ITriangleIndexed[], double> getAsteroidMassByRadius, Func<double, MineralDNA[]> getMineralsByDestroyedMass, double minChildAsteroidRadius)
        {
            _map = map;
            _world = world;

            _boundry = new Boundry(boundryMin, boundryMax);

            _material_Asteroid = material_Asteroid;
            _material_Mineral = material_Mineral;

            _getAsteroidMassByRadius = getAsteroidMassByRadius;
            _getMineralsByDestroyedMass = getMineralsByDestroyedMass;

            _minChildAsteroidRadius = minChildAsteroidRadius;

            _mineralTypesByValue = ((MineralType[])Enum.GetValues(typeof(MineralType))).
                Select(o => new { Type = o, Value = ItemOptionsAstMin2D.GetCredits_Mineral(o) }).
                OrderBy(o => o.Value).
                Select(o => o.Type).
                ToArray();
        }

        #endregion

        #region IPartUpdatable Members

        /// <summary>
        /// This runs in the main thread, and does the actual adds and removes (those instructions are figured out in the any thread)
        /// </summary>
        public void Update_MainThread(double elapsedTime)
        {
            ChangeInstruction[] instructions = null;
            instructions = Interlocked.Exchange(ref _instructions, null);

            if (instructions == null)
            {
                return;
            }

            foreach (var instruction in instructions)
            {
                switch (instruction.InstructionType)
                {
                    case InstructionType.Add:
                        #region Add

                        if (instruction.Asteroid != null)
                        {
                            AddAsteroid(instruction);
                        }
                        else if (instruction.Mineral != null)
                        {
                            AddMineral(instruction);
                        }
                        else
                        {
                            throw new ApplicationException("Unknown type of item to add");
                        }

                        #endregion
                        break;

                    case InstructionType.Remove:
                        #region Remove

                        if (instruction.Remove_Asteroids != null)
                        {
                            foreach (Asteroid asteroid in instruction.Remove_Asteroids)
                            {
                                _map.RemoveItem(asteroid);
                            }
                        }
                        else if (instruction.Remove_Minerals != null)
                        {
                            foreach (Mineral mineral in instruction.Remove_Minerals)
                            {
                                _map.RemoveItem(mineral);
                            }
                        }
                        else
                        {
                            throw new ApplicationException("Unknown type of item to remove");
                        }

                        #endregion
                        break;

                    case InstructionType.Merge:
                        //If one of the items to remove is already gone, then build a smaller merged item.
                        //If only one is left, then just put the removed back into the map
                        break;

                    default:
                        throw new ApplicationException("Unknown InstructionType: " + instruction.InstructionType);
                }
            }
        }
        /// <summary>
        /// This fires in an arbitrary thread.  It looks at what's in the map, and builds instructions that will be run in the main thread
        /// (adds/removes)
        /// </summary>
        public void Update_AnyThread(double elapsedTime)
        {
            MapOctree snapshot = _map.LatestSnapshot;
            if (snapshot == null)
            {
                return;
            }

            IEnumerable<MapObjectInfo> allItems = snapshot.GetItems();

            // Look for too few/many
            ChangeInstruction[] asteroids = ExamineAsteroids(allItems, _boundry);
            ChangeInstruction[] minerals = ExamineMinerals(allItems, _boundry, _mineralTypesByValue);

            // Store these instructions for the main thread to do
            if (asteroids != null || minerals != null)
            {
                ChangeInstruction[] instructions = UtilityCore.ArrayAdd(asteroids, minerals);

                if (instructions.Length > MAXCHANGES)
                {
                    instructions = UtilityCore.RandomOrder(instructions, MAXCHANGES).ToArray();
                }

                _instructions = instructions;
            }
        }

        public int? IntervalSkips_MainThread
        {
            get
            {
                return 4000;
            }
        }
        public int? IntervalSkips_AnyThread
        {
            get
            {
                // The any thread should fire slightly more often than the main thread.  This will keep the timings shuffled a bit
                return Convert.ToInt32(Math.Floor(IntervalSkips_MainThread.Value * .9));
            }
        }

        #endregion

        #region Private Methods

        private void AddAsteroid(ChangeInstruction instr)
        {
            //NOTE: The dna only holds radius, no triangles
            Asteroid asteroid = new Asteroid(instr.Asteroid.Radius, _getAsteroidMassByRadius, instr.Add_Position, _world, _map, _material_Asteroid, _getMineralsByDestroyedMass, _material_Mineral, _minChildAsteroidRadius);

            asteroid.PhysicsBody.AngularVelocity = instr.Add_AngVel;
            asteroid.PhysicsBody.Velocity = instr.Add_Velocity;

            _map.AddItem(asteroid);
        }
        private void AddMineral(ChangeInstruction instr)
        {
            Mineral mineral = new Mineral(instr.Mineral.MineralType, instr.Add_Position, instr.Mineral.Volume, _world, _material_Mineral, _sharedVisuals, ItemOptionsAstMin2D.MINERAL_DENSITYMULT, instr.Mineral.Volume / ItemOptionsAstMin2D.MINERAL_AVGVOLUME);

            mineral.PhysicsBody.AngularVelocity = instr.Add_AngVel;
            mineral.PhysicsBody.Velocity = instr.Add_Velocity;

            _map.AddItem(mineral);
        }

        private static ChangeInstruction[] ExamineAsteroids(IEnumerable<MapObjectInfo> allItems, Boundry boundry)
        {
            var asteroids = allItems.
                Where(o => o.MapObject is Asteroid).
                ToArray();

            if (asteroids.Length < MIN_ASTEROIDS)
            {
                #region Add

                // Figure out how many to add
                int count = MIN_ASTEROIDS - asteroids.Length;
                if (count < MAXCHANGES)
                {
                    count = StaticRandom.Next(count, MAXCHANGES + 1);
                }
                else if (count > MAXCHANGES)
                {
                    count = MAXCHANGES;
                }

                // Define new asteroids
                return ExamineAsteroids_Add1(count, boundry);

                #endregion
            }
            else if (asteroids.Length > MAX_ASTEROIDS)
            {
                #region Remove

                // Figure out how many to remove
                int count = asteroids.Length - MAX_ASTEROIDS;
                if (count > MAXCHANGES)
                {
                    count = MAXCHANGES;
                }

                return ExamineAsteroids_Remove1(asteroids, count);

                #endregion
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// This creates random size in random location
        /// TODO: Look at existing sizes more carefully to see what size to create
        /// TODO: Don't create something near the player - take a list of spheres to avoid (or maybe cubes)
        /// TODO: Have certain features of the map be sources - maybe a couple hidden moving sources that slowly move around the edge of the map
        /// TODO: When creating asteroids, create a small stream of them
        /// </summary>
        private static ChangeInstruction[] ExamineAsteroids_Add1(int count, Boundry boundry)
        {
            ChangeInstruction[] retVal = new ChangeInstruction[count];

            for (int cntr = 0; cntr < count; cntr++)
            {
                //asteroidSize = 4 + (rand.NextDouble() * 6);       // medium
                //asteroidSize = 9 + (rand.NextDouble() * 10);      // large
                double asteroidSize = 4 + StaticRandom.NextDouble(16);

                Point3D position = Math3D.GetRandomVector(boundry.Outer_Min, boundry.Outer_Max, boundry.Inner_Min, boundry.Inner_Max).ToPoint();

                Vector3D velocity = Math3D.GetRandomVector_Circular(6d);
                Vector3D angVel = Math3D.GetRandomVector_Spherical(1d);

                AsteroidDNA asteroid = new AsteroidDNA()
                {
                    PartType = Asteroid.PARTTYPE,
                    Radius = asteroidSize,
                    Position = position,        //NOTE: These are duplication and aren't used, but storing them anyway
                    Velocity = velocity,
                    AngularVelocity = angVel,
                };

                retVal[cntr] = new ChangeInstruction(position, velocity, angVel, asteroid);
            }

            return retVal;
        }
        /// <summary>
        /// This just removes the smallest ones
        /// TODO: Look for chances to merge
        /// TODO: Don't remove near the player
        /// </summary>
        private static ChangeInstruction[] ExamineAsteroids_Remove1(IEnumerable<MapObjectInfo> asteroids, int count)
        {
            return asteroids.
                OrderBy(o => o.Radius).
                Take(count).
                Select(o => new ChangeInstruction(new[] { (Asteroid)o.MapObject })).      // need to make an instruction per asteroid, because if there are too many total instructions, each needs a fair chance of being kept/removed
                ToArray();
        }

        private static ChangeInstruction[] ExamineMinerals(IEnumerable<MapObjectInfo> allItems, Boundry boundry, MineralType[] mineralTypesByValue)
        {
            var minerals = allItems.
                Where(o => o.MapObject is Mineral).
                ToArray();

            //TODO: Add minerals if too few

            if (minerals.Length > MAX_MINERALS)
            {
                #region Remove

                // Figure out how many to remove
                int count = minerals.Length - MAX_MINERALS;
                if (count > MAXCHANGES)
                {
                    count = MAXCHANGES;
                }

                return ExamineMinerals_Remove1(minerals, count);

                #endregion
            }
            else
            {
                return null;
            }
        }
        private static ChangeInstruction[] ExamineMinerals_Remove1(MapObjectInfo[] minerals, int count)
        {
            return minerals.
                Select(o => (Mineral)o.MapObject).
                OrderBy(o => o.Credits).
                Take(count).
                Select(o => new ChangeInstruction(new[] { o })).
                ToArray();
        }

        #endregion
    }
}
