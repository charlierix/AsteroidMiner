using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.MapParts;
using Game.Newt.v2.GameItems.ShipParts;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.GameItems.MapParts
{
    public class Asteroid : IMapObject, IPartUpdatable
    {
        #region class: HitTracker1

        /// <summary>
        /// This simply adds up the lengths of the collisions, and splits the asteroid when the sum exceeds the radius
        /// </summary>
        private class HitTracker1
        {
            #region class: AsteroidOrMineralDefinition

            private class AsteroidOrMineralDefinition
            {
                #region Constructor

                // Asteroid
                public AsteroidOrMineralDefinition(PartSeparator_Part part, ITriangleIndexed[] asteroidTriangles, double asteroidRadius)
                {
                    this.IsAsteroid = true;
                    this.Part = part;

                    this.AsteroidTriangles = asteroidTriangles;
                    this.AsteroidRadius = asteroidRadius;

                    this.MineralDefinition = null;
                }

                // Mineral
                public AsteroidOrMineralDefinition(PartSeparator_Part part, MineralDNA mineralDefinition)
                {
                    this.IsAsteroid = false;
                    this.Part = part;

                    this.MineralDefinition = mineralDefinition;

                    this.AsteroidTriangles = null;
                    this.AsteroidRadius = 0;
                }

                #endregion

                /// <summary>
                /// True: Asteroid
                /// False: Mineral
                /// </summary>
                public readonly bool IsAsteroid;

                public readonly PartSeparator_Part Part;

                public readonly ITriangleIndexed[] AsteroidTriangles;
                public readonly double AsteroidRadius;

                public readonly MineralDNA MineralDefinition;
            }

            #endregion

            #region Declaration Section

            private readonly World _world;
            private readonly Map _map;
            private readonly int _materialID;

            private readonly Asteroid _parent;

            private readonly double _minChildRadius;
            private readonly Vector3D _radius;
            private readonly double _radiusMin;

            private readonly Func<double, ITriangleIndexed[], double> _getMassByRadius;

            private readonly Func<double, MineralDNA[]> _getMineralsByDestroyedMass;
            private readonly int _mineralMaterialID;

            private double _damage = 0;
            private int _numHits = 0;
            private readonly int _maxHits = StaticRandom.Next(5, 20);

            private static ThreadLocal<SharedVisuals> _sharedVisuals = new ThreadLocal<SharedVisuals>(() => new SharedVisuals());

            #endregion

            #region Constructor

            public HitTracker1(World world, Map map, int materialID, Asteroid parent, ITriangleIndexed[] triangles, Vector3D radius, double minChildRadius, Func<double, ITriangleIndexed[], double> getMassByRadius, Func<double, MineralDNA[]> getMineralsByDestroyedMass, int mineralMaterialID)
            {
                _world = world;
                _map = map;
                _materialID = materialID;
                _parent = parent;
                _minChildRadius = minChildRadius;
                _getMassByRadius = getMassByRadius;
                _getMineralsByDestroyedMass = getMineralsByDestroyedMass;
                _mineralMaterialID = mineralMaterialID;

                _radius = radius;
                _radiusMin = Math1D.Min(_radius.X, _radius.Y, _radius.Z);
            }

            #endregion

            #region Public Methods

            /// <summary>
            /// This will remember the damage, and if it's too large, break up the asteroid
            /// </summary>
            /// <remarks>
            /// If this returns something, it will be the asteroid split into pieces, as well as some minerals
            /// </remarks>
            public bool TakeDamage(Point3D position, Vector3D amount)
            {
                // Convert to model coords
                Point3D posModel = _parent.PhysicsBody.PositionFromWorld(position);
                Vector3D dirModel = _parent.PhysicsBody.DirectionFromWorld(amount);

                //TODO: Only look at the portion of the vector that is orthoganal to the ellipse

                // Reducing the length to match radius units
                double length = amount.Length;
                length /= 600;

                // Add that length to the total damage
                _damage += length;

                // Remember how many times this asteroid has been hit
                _numHits++;

                if (_numHits < _maxHits && _damage < _radiusMin)
                {
                    // It's not damaged enough to split apart
                    return false;
                }

                Point3D parentPos = _parent.PositionWorld;
                Vector3D parentVel = _parent.VelocityWorld;

                if (!_map.RemoveItem(_parent))
                {
                    // Something else already removed it from the map
                    return false;
                }

                double overDamage = _damage / _radiusMin;

                var children = GetChildAsteroids(overDamage, parentPos, parentVel);

                //NOTE: This parent asteroid was already removed from the map
                if (children != null)
                {
                    foreach (var child in children)
                    {
                        if (child is Asteroid)
                        {
                            _map.AddItem((Asteroid)child);      // the add doesn't allow base types to be passed in
                        }
                        else if (child is Mineral)
                        {
                            _map.AddItem((Mineral)child);
                        }
                        else
                        {
                            throw new ApplicationException("Unknown type of child");
                        }
                    }
                }

                return children != null;
            }

            #endregion

            #region Private Methods

            // This also creates minerals
            private IMapObject[] GetChildAsteroids(double overDamage, Point3D parentPos, Vector3D parentVel)
            {
                const double MAXOVERDMG = 13;       // overdamage of 1 is the smallest value (the asteroid was barely destroyed).  Larger values are overkill, and the asteroid becomes more fully destroyed
                const int MAXCHILDREN = 5;

                #region Child asteroid sizes

                // Figure out the radius of the child asteroids
                double radius = GetTotalChildRadius(_radius, overDamage, MAXOVERDMG);

                // Get the volumes of the child asteroids
                double[] asteroidVolumes = null;
                if (radius > _minChildRadius)
                {
                    double totalVolume = 4d / 3d * Math.PI * radius * radius * radius;
                    double minVolume = 4d / 3d * Math.PI * _minChildRadius * _minChildRadius * _minChildRadius;

                    int numChildren = GetNumChildren(radius, MAXCHILDREN, totalVolume, minVolume, overDamage, MAXOVERDMG);
                    asteroidVolumes = GetChildVolumes(numChildren, totalVolume, minVolume);
                }
                else
                {
                    // Not enough left, don't create any child asteroids
                    radius = 0;
                    asteroidVolumes = new double[0];
                }

                #endregion
                #region Mineral sizes

                MineralDNA[] mineralDefinitions = null;
                if (_getMineralsByDestroyedMass != null)
                {
                    //double destroyedMass = GetDestroyedMass(Math3D.Avg(_radius.X, _radius.Y, _radius.Z), radius, _getMassByRadius);       // using avg had too many cases where the returned mass was negative
                    double destroyedMass = GetDestroyedMass(Math1D.Max(_radius.X, _radius.Y, _radius.Z), radius, _getMassByRadius);
                    if (destroyedMass > 0)      // child radius is calculated using max of _radius, but avg was passed to the getmass method.  So there's a chance that getmass returns negative
                    {
                        mineralDefinitions = _getMineralsByDestroyedMass(destroyedMass);
                    }
                }

                if (mineralDefinitions == null)
                {
                    mineralDefinitions = new MineralDNA[0];
                }

                #endregion

                if (asteroidVolumes.Length == 0 && mineralDefinitions.Length == 0)
                {
                    // Nothing to spawn
                    return null;
                }

                // Figure out positions
                AsteroidOrMineralDefinition[] children = PositionChildAsteroidsAndMinerals(asteroidVolumes, mineralDefinitions);

                #region Create IMapObjects

                IMapObject[] retVal = new IMapObject[children.Length];

                for (int cntr = 0; cntr < retVal.Length; cntr++)
                {
                    Point3D position = parentPos + children[cntr].Part.Position.ToVector();

                    if (children[cntr].IsAsteroid)
                    {
                        // Asteroid
                        AsteroidExtra extra = new AsteroidExtra()
                        {
                            Triangles = children[cntr].AsteroidTriangles,
                            GetMineralsByDestroyedMass = _getMineralsByDestroyedMass,
                            MineralMaterialID = _mineralMaterialID,
                            MinChildRadius = _minChildRadius,
                        };

                        retVal[cntr] = new Asteroid(children[cntr].AsteroidRadius, _getMassByRadius, position, _world, _map, _materialID, extra);
                    }
                    else
                    {
                        // Mineral
                        MineralDNA mindef = children[cntr].MineralDefinition;
                        double densityMult = mindef.Density / Mineral.GetSettingsForMineralType(mindef.MineralType).Density;
                        retVal[cntr] = new Mineral(mindef.MineralType, position, mindef.Volume, _world, _mineralMaterialID, _sharedVisuals.Value, densityMult, mindef.Scale);
                    }

                    retVal[cntr].PhysicsBody.Rotation = children[cntr].Part.Orientation;

                    Vector3D velFromCenter = children[cntr].Part.Position.ToVector().ToUnit(false);
                    velFromCenter *= UtilityCore.GetScaledValue(1, 4, 1, MAXOVERDMG, overDamage);

                    retVal[cntr].PhysicsBody.Velocity = parentVel + velFromCenter;

                    retVal[cntr].PhysicsBody.AngularVelocity = Math3D.GetRandomVector_Spherical(UtilityCore.GetScaledValue(.5, 8, 1, MAXOVERDMG, overDamage));
                }

                #endregion

                return retVal;
            }

            /// <summary>
            /// Figure how much radius remains based on how much damage was done.  This remaining radius will be
            /// distributed among the child asteroids
            /// </summary>
            private static double GetTotalChildRadius(Vector3D parentRadius, double overDamage, double maxOverDamage)
            {
                double retVal = Math1D.Max(parentRadius.X, parentRadius.Y, parentRadius.Z);

                double reducePercent = UtilityCore.GetScaledValue(.75, 0, 1, maxOverDamage, overDamage);
                if (reducePercent > .99)     // this can happen if overDamage < 1
                {
                    reducePercent = .99;
                }

                retVal *= reducePercent;

                return retVal;
            }

            /// <summary>
            /// This is how many child asteroids to create
            /// </summary>
            private static int GetNumChildren(double radius, int maxChildren, double totalVolume, double minVolume, double overDamage, double maxOverDamage)
            {
                // Max is how many times min can go into total
                int max = Convert.ToInt32(Math.Floor(totalVolume / minVolume));
                if (max > maxChildren)
                {
                    max = maxChildren;
                }

                // The stronger the hit, the more likely of smaller debris
                int retVal = Convert.ToInt32(Math.Round(UtilityCore.GetScaledValue(1d, max, 1d, maxOverDamage / 2d, overDamage)));
                if (retVal < 1)
                {
                    retVal = 1;
                }

                retVal += StaticRandom.NextDouble() < .33 ? 1 : 0;      // give a small chance of creating one extra
                if (retVal > max)
                {
                    retVal = max;
                }

                return retVal;
            }

            /// <summary>
            /// This randomly distributes volume to the child asteroids, so that the sum of the children matches the
            /// total passed in
            /// </summary>
            private static double[] GetChildVolumes(int numChildren, double totalVolume, double minVolume)
            {
                // Figure out the volume of each child asteroid: min + rand(remaining)
                double[] volumes = new double[numChildren];
                double remainingVolume = totalVolume - (minVolume * numChildren);

                for (int cntr = 0; cntr < numChildren - 1; cntr++)
                {
                    double addVolume = StaticRandom.NextDouble(remainingVolume);
                    remainingVolume -= addVolume;
                    volumes[cntr] = minVolume + addVolume;
                }

                volumes[volumes.Length - 1] = minVolume + remainingVolume;

                return volumes;
            }

            /// <summary>
            /// Converts radius into mass, then subtracts
            /// NOTE: The actual asteroids are ellipses.  The single radius value is just an approximation
            /// </summary>
            private static double GetDestroyedMass(double parentRadius, double childRadius, Func<double, ITriangleIndexed[], double> getMassByRadius)
            {
                double parentMass = getMassByRadius(parentRadius, null);

                if (childRadius > 0)
                {
                    double childMass = getMassByRadius(childRadius, null);

                    return parentMass - childMass;
                }
                else
                {
                    return parentMass;
                }
            }

            /// <summary>
            /// Defines the child asteroid shapes, then finds positions/orientations for all child asteroids and minerals (makes
            /// sure nothing overlaps)
            /// </summary>
            private AsteroidOrMineralDefinition[] PositionChildAsteroidsAndMinerals(double[] asteroidVolumes, MineralDNA[] mineralDefinitions)
            {
                const double FOURTHIRDSPI = 4d / 3d * Math.PI;
                const double ONETHRID = 1d / 3d;

                double positionRange = _radiusMin * .05;

                #region Asteroids

                PartSeparator_Part[] asteroidParts = new PartSeparator_Part[asteroidVolumes.Length];
                ITriangleIndexed[][] asteroidTriangles = new ITriangleIndexed[asteroidVolumes.Length][];
                double[] asteroidRadii = new double[asteroidVolumes.Length];

                for (int cntr = 0; cntr < asteroidVolumes.Length; cntr++)
                {
                    // r^3=v/(4/3pi)
                    asteroidRadii[cntr] = Math.Pow(asteroidVolumes[cntr] / FOURTHIRDSPI, ONETHRID);

                    asteroidTriangles[cntr] = GetHullTriangles_Initial(asteroidRadii[cntr]);

                    double currentMass = _getMassByRadius(asteroidRadii[cntr], asteroidTriangles[cntr]);

                    asteroidParts[cntr] = new PartSeparator_Part(asteroidTriangles[cntr][0].AllPoints, currentMass, Math3D.GetRandomVector_Spherical(positionRange).ToPoint(), Math3D.GetRandomRotation());
                }

                #endregion
                #region Minerals

                PartSeparator_Part[] mineralParts = new PartSeparator_Part[mineralDefinitions.Length];

                for (int cntr = 0; cntr < mineralDefinitions.Length; cntr++)
                {
                    //NOTE: Copied logic from Mineral that gets collision points and mass

                    // Points
                    Point3D[] mineralPoints = UtilityWPF.GetPointsFromMesh((MeshGeometry3D)_sharedVisuals.Value.GetMineralMesh(mineralDefinitions[cntr].MineralType));

                    // Mass
                    double mass = mineralDefinitions[cntr].Density * mineralDefinitions[cntr].Volume;

                    // Store it
                    mineralParts[cntr] = new PartSeparator_Part(mineralPoints, mass, Math3D.GetRandomVector_Spherical(positionRange).ToPoint(), Math3D.GetRandomRotation());
                }

                #endregion

                #region Pull Apart

                if (asteroidParts.Length + mineralParts.Length > 1)
                {
                    PartSeparator_Part[] allParts = UtilityCore.ArrayAdd(asteroidParts, mineralParts);

                    bool dummy;
                    CollisionHull[] hulls = PartSeparator.Separate(out dummy, allParts, _world);

                    foreach (CollisionHull hull in hulls)
                    {
                        hull.Dispose();
                    }
                }

                #endregion

                #region Build Return

                List<AsteroidOrMineralDefinition> retVal = new List<AsteroidOrMineralDefinition>();

                for (int cntr = 0; cntr < asteroidParts.Length; cntr++)
                {
                    retVal.Add(new AsteroidOrMineralDefinition(asteroidParts[cntr], asteroidTriangles[cntr], asteroidRadii[cntr]));
                }

                for (int cntr = 0; cntr < mineralParts.Length; cntr++)
                {
                    retVal.Add(new AsteroidOrMineralDefinition(mineralParts[cntr], mineralDefinitions[cntr]));
                }

                #endregion

                return retVal.ToArray();
            }

            #endregion
        }

        #endregion
        #region class: HitTracker2

        /// <summary>
        /// This simply adds up the lengths of the collisions, and splits the asteroid when the sum exceeds the radius
        /// </summary>
        private class HitTracker2
        {
            #region class: AsteroidOrMineralDefinition

            private class AsteroidOrMineralDefinition
            {
                #region Constructor

                // Asteroid
                public AsteroidOrMineralDefinition(PartSeparator_Part part, ITriangleIndexed[] asteroidTriangles, double asteroidRadius, Vector3D velocity, bool shouldSelfDestruct, MineralDNA[] mineralsAfterSelfDestruct)
                {
                    this.IsAsteroid = true;
                    this.Part = part;

                    this.AsteroidTriangles = asteroidTriangles;
                    this.AsteroidRadius = asteroidRadius;
                    this.ShouldAsteroidSelfDestruct = shouldSelfDestruct;
                    this.MineralsAfterSelfDestruct = mineralsAfterSelfDestruct;

                    this.Velocity = velocity;

                    this.MineralDefinition = null;
                }

                // Mineral
                public AsteroidOrMineralDefinition(PartSeparator_Part part, MineralDNA mineralDefinition, Vector3D velocity)
                {
                    this.IsAsteroid = false;
                    this.Part = part;

                    this.MineralDefinition = mineralDefinition;

                    this.Velocity = velocity;

                    this.AsteroidTriangles = null;
                    this.AsteroidRadius = 0;
                    this.ShouldAsteroidSelfDestruct = false;
                }

                #endregion

                /// <summary>
                /// True: Asteroid
                /// False: Mineral
                /// </summary>
                public readonly bool IsAsteroid;

                public readonly PartSeparator_Part Part;

                public readonly ITriangleIndexed[] AsteroidTriangles;
                public readonly double AsteroidRadius;
                public readonly bool ShouldAsteroidSelfDestruct;
                public readonly MineralDNA[] MineralsAfterSelfDestruct;

                public readonly MineralDNA MineralDefinition;

                public readonly Vector3D Velocity;
            }

            #endregion

            #region Declaration Section

            private const double MAXVELOCITY = 15;
            private const double SELFDESTRUCTTIME = 4;

            private readonly World _world;
            private readonly Map _map;
            private readonly int _materialID;

            private readonly Asteroid _parent;

            private readonly double _minChildRadius;
            private readonly Vector3D _radius;
            private readonly double _radiusMin;

            private readonly Func<double, ITriangleIndexed[], double> _getMassByRadius;

            private readonly Func<double, MineralDNA[]> _getMineralsByDestroyedMass;
            private readonly int _mineralMaterialID;

            private double _damage = 0;
            private readonly List<Tuple<Point3D, Vector3D, double>> _hits = new List<Tuple<Point3D, Vector3D, double>>();
            private readonly int _maxHits = StaticRandom.Next(5, 20);

            private static ThreadLocal<SharedVisuals> _sharedVisuals = new ThreadLocal<SharedVisuals>(() => new SharedVisuals());

            #endregion

            #region Constructor

            public HitTracker2(World world, Map map, int materialID, Asteroid parent, Vector3D radius, double minChildRadius, Func<double, ITriangleIndexed[], double> getMassByRadius, Func<double, MineralDNA[]> getMineralsByDestroyedMass, int mineralMaterialID)
            {
                _world = world;
                _map = map;
                _materialID = materialID;
                _parent = parent;
                _minChildRadius = minChildRadius;
                _getMassByRadius = getMassByRadius;
                _getMineralsByDestroyedMass = getMineralsByDestroyedMass;
                _mineralMaterialID = mineralMaterialID;

                _radius = radius;
                _radiusMin = Math1D.Min(_radius.X, _radius.Y, _radius.Z);
            }

            #endregion

            #region Public Properties

            private bool _isDestroying = false;
            public bool IsDestroying
            {
                get
                {
                    return _isDestroying;
                }
            }

            #endregion

            #region Public Methods

            /// <summary>
            /// This will remember the damage, and if it's too large, break up the asteroid
            /// </summary>
            /// <remarks>
            /// If this returns true, then the asteroid was split into pieces, as well as some minerals (the parent asteroid will be
            /// removed from the map)
            /// </remarks>
            /// <param name="position">Point of impact</param>
            /// <param name="amount">Direction and intensity of impact</param>
            /// <param name="piercePercent">
            /// 0 to 1
            /// This affects where voronoi control points are created, and where the impact velocities originate.  Percent of zero will make points at the surface.
            /// Percent of 1 (100%) will cause deep control points
            /// </param>
            public bool TakeDamage(Point3D position, Vector3D amount, double piercePercent)
            {
                if (_isDestroying)
                {
                    return false;
                }

                #region store hit

                // Convert to model coords
                Point3D posModel = _parent.PhysicsBody.PositionFromWorld(position);
                Vector3D dirModel = _parent.PhysicsBody.DirectionFromWorld(amount);

                //TODO: Only look at the portion of the vector that is orthoganal to the ellipse

                // Reducing the length to match radius units
                double length = amount.Length;
                length /= 600;

                // Add that length to the total damage
                _damage += length;

                // Remember how many times this asteroid has been hit
                _hits.Add(Tuple.Create(posModel, dirModel, piercePercent));

                #endregion

                if (_hits.Count < _maxHits && _damage < _radiusMin)
                {
                    // It's not damaged enough to split apart
                    return false;
                }

                #region prep

                _isDestroying = true;       //NOTE: In the future, the code below will be async, so setting this now so no more damage will be taken

                Point3D parentPos = _parent.PositionWorld;
                Vector3D parentVel = _parent.VelocityWorld;
                Quaternion parentRot = _parent.PhysicsBody.Rotation;

                #endregion

                if (!_map.RemoveItem(_parent))
                {
                    // Something else already removed it from the map
                    return false;
                }

                double overDamage = _damage / _radiusMin;

                AsteroidOrMineralDefinition[] children = GetChildAsteroidsOrMinerals(_hits.ToArray(), overDamage);

                //NOTE: This parent asteroid was already removed from the map
                if (children != null)
                {
                    IMapObject[] mapObjects = ConvertToMapObjects(children, parentPos, parentVel, parentRot);
                    AddToMap(mapObjects);
                }

                return children != null;
            }

            /// <summary>
            /// This will convert to real minerals, pull them apart, and place them on the map
            /// NOTE: If these are replacing an asteroid, be sure the asteroid is removed before calling this
            /// </summary>
            public void PlaceMinerals(MineralDNA[] minerals, Point3D position, Vector3D velocity, double? maxRandomVelocity = null)
            {
                //TODO: Angular velocity
                AsteroidOrMineralDefinition[] positioned = PositionMinerals(minerals, maxRandomVelocity);

                IMapObject[] mapObjects = ConvertToMapObjects(positioned, position, velocity, Quaternion.Identity);

                AddToMap(mapObjects);
            }

            #endregion

            #region Private Methods

            // This also creates minerals
            private AsteroidOrMineralDefinition[] GetChildAsteroidsOrMinerals(Tuple<Point3D, Vector3D, double>[] hits, double overDamage)
            {
                int childCount = GetChildCounts(overDamage);

                // Reduce the length of the hits so the shard velocities aren't so high
                hits = hits.
                    Select(o => Tuple.Create(o.Item1, o.Item2 * .33, o.Item3)).
                    ToArray();

                HullVoronoiExploder_Options options = new HullVoronoiExploder_Options()
                {
                    MinCount = childCount,
                    MaxCount = childCount,
                };

                HullVoronoiExploder_Response shards = HullVoronoiExploder.ShootHull(_parent._triangles, hits, options);

                #region cap velocities

                //NOTE: This caps the velocities by running them through an s curve, but if they are all too high to begin with, they
                //will all become very near MAXVELOCITY

                if (shards != null && shards.Velocities != null)
                {
                    shards.Velocities = shards.Velocities.
                        Select(o =>
                        {
                            double length = o.Length;
                            double adjusted = Math1D.PositiveSCurve(length, MAXVELOCITY, .75);
                            double ratio = adjusted / length;
                            return o * ratio;
                        }).
                        ToArray();
                }

                #endregion

                return DetermineDestroyedChildrenMinerals(shards, overDamage, _parent.RadiusVect, _minChildRadius, _getMassByRadius, _getMineralsByDestroyedMass);
            }

            /// <summary>
            /// This figures out how many total fragments to create (before self destruction)
            /// </summary>
            private static int GetChildCounts(double overDamage)
            {
                //http://mycurvefit.com/

                // Four Parameter Logistic
                //https://psg.hitachi-solutions.com/masterplex/blog/the-4-parameter-logistic-4pl-nonlinear-regression-model

                double A = 1.879608178;     // minimum asymptote
                double B = 0.859043691;     // hill slope
                double C = 12.28951161;     // inflection point
                double D = 31.92569835;     // maximum asymptote

                //double count = ((A - D) / (1 + ((overDamage / C) ^ B))) + D;
                double count = ((A - D) / (1 + Math.Pow((overDamage / C), B))) + D;

                // Randomize a bit
                Random rand = StaticRandom.GetRandomForThread();
                count = rand.NextPercent(count, .25);
                if (rand.NextDouble() < .1)
                {
                    count += rand.NextBool() ? 1d : -1d;
                }

                int retVal = count.ToInt_Round();

                if (retVal < 2) retVal = 2;
                if (retVal > 25) retVal = 25;

                return retVal;
            }

            private AsteroidOrMineralDefinition[] DetermineDestroyedChildrenMinerals(HullVoronoiExploder_Response shards, double overDamage, Vector3D parentRadius, double minChildRadius, Func<double, ITriangleIndexed[], double> getMassByRadius, Func<double, MineralDNA[]> getMineralsByDestroyedMass)
            {
                const double MAXOVERDMG = 13;       // overdamage of 1 is the smallest value (the asteroid was barely destroyed).  Larger values are overkill, and the asteroid becomes more fully destroyed

                if (shards == null || shards.Shards == null || shards.Shards.Length == 0)
                {
                    // There was problem, so just pop out some minerals
                    return DetermineDestroyedChildrenMinerals_Minerals(parentRadius, getMassByRadius, getMineralsByDestroyedMass);
                }

                Random rand = StaticRandom.GetRandomForThread();

                #region calculate volumes

                var shardVolumes = shards.Shards.
                    Select(o =>
                        {
                            Vector3D radius = GetEllipsoidRadius(o.Hull_Centered);
                            return new { Radius = radius, Volume = GetEllipsoidVolume(radius) };
                        }).
                    ToArray();

                // Figure out how much volume should permanently be destroyed
                double parentVolume = GetEllipsoidVolume(parentRadius);
                double volumeToDestroy = GetVolumeToDestroy(parentVolume, overDamage, MAXOVERDMG);

                #endregion

                double destroyedVolume = 0;
                bool[] shouldSelfDestruct = new bool[shards.Shards.Length];

                #region detect too small

                // Get rid of any that are too small
                //TODO: Also get rid of any that are too thin
                for (int cntr = 0; cntr < shards.Shards.Length; cntr++)
                {
                    if (shards.Shards[cntr].Radius < minChildRadius)
                    {
                        shouldSelfDestruct[cntr] = true;
                        destroyedVolume += shardVolumes[cntr].Volume;
                    }
                }

                #endregion

                if (destroyedVolume < volumeToDestroy)
                {
                    #region remove more

                    // Find the shards that could be removed
                    var candidates = Enumerable.Range(0, shards.Shards.Length).
                        Select(o => new
                        {
                            Index = o,
                            Shard = shards.Shards[o],
                            Volume = shardVolumes[o]
                        }).
                        Where(o => !shouldSelfDestruct[o.Index] && o.Volume.Volume < volumeToDestroy - destroyedVolume).
                        OrderBy(o => o.Volume.Volume).
                        ToList();

                    while (candidates.Count > 0 && destroyedVolume < volumeToDestroy)
                    {
                        // Figure out which to self destruct (the rand power will favor inicies closer to zero)
                        int index = UtilityCore.GetIndexIntoList(rand.NextPow(2), candidates.Count);

                        // Remove it
                        shouldSelfDestruct[candidates[index].Index] = true;
                        destroyedVolume += candidates[index].Volume.Volume;
                        candidates.RemoveAt(index);

                        // Remove the items at the end that are now too large
                        index = candidates.Count - 1;
                        while (index >= 0)
                        {
                            if (candidates[index].Volume.Volume < volumeToDestroy - destroyedVolume)
                            {
                                break;      // it's sorted, so the rest will also be under
                            }
                            else
                            {
                                candidates.RemoveAt(index);
                                index--;
                            }
                        }
                    }

                    #endregion
                }

                #region distribute minerals

                // Figure out the mineral value of the destroyed volume
                MineralDNA[] mineralDefinitions = null;
                if (destroyedVolume > 0 && getMineralsByDestroyedMass != null)
                {
                    double inferredRadius = GetEllipsoidRadius(destroyedVolume);
                    double destroyedMass = getMassByRadius(inferredRadius, null);

                    if (destroyedMass > 0)
                    {
                        mineralDefinitions = getMineralsByDestroyedMass(destroyedMass);
                    }
                }

                // Figure out which of the temp asteroids should contain minerals
                var packedMinerals = new Tuple<int, MineralDNA[]>[0];

                if (mineralDefinitions != null && mineralDefinitions.Length > 0)
                {
                    int[] destroyedIndicies = Enumerable.Range(0, shouldSelfDestruct.Length).
                        Where(o => shouldSelfDestruct[o]).
                        ToArray();

                    packedMinerals = DistributeMinerals(mineralDefinitions, destroyedIndicies);
                }

                #endregion

                #region final array

                AsteroidOrMineralDefinition[] retVal = new AsteroidOrMineralDefinition[shards.Shards.Length];

                for (int cntr = 0; cntr < retVal.Length; cntr++)
                {
                    Vector3D velocity = new Vector3D(0, 0, 0);
                    if (shards.Velocities != null && shards.Velocities.Length == shards.Shards.Length)
                    {
                        velocity = shards.Velocities[cntr];
                    }

                    //NOTE: Only position is needed (The first attempt created random asteroids and pulled them apart.  This second attempt
                    //doesn't need to pull them apart)
                    PartSeparator_Part part = new PartSeparator_Part(new[] { new Point3D() }, 0, shards.Shards[cntr].Center_ParentCoords, Quaternion.Identity);

                    //MineralDNA[] mineralsAfter = packedMinerals?.FirstOrDefault(o => o.Item1 == cntr)?.Item2;
                    MineralDNA[] mineralsAfter = null;
                    if (packedMinerals != null)
                    {
                        var found = packedMinerals.FirstOrDefault(o => o.Item1 == cntr);
                        if (found != null)
                        {
                            mineralsAfter = found.Item2;
                        }
                    }

                    retVal[cntr] = new AsteroidOrMineralDefinition(part, shards.Shards[cntr].Hull_Centered, shards.Shards[cntr].Radius, velocity, shouldSelfDestruct[cntr], mineralsAfter);
                }

                #endregion

                return retVal;
            }
            private AsteroidOrMineralDefinition[] DetermineDestroyedChildrenMinerals_Minerals(Vector3D parentRadius, Func<double, ITriangleIndexed[], double> getMassByRadius, Func<double, MineralDNA[]> getMineralsByDestroyedMass)
            {
                double parentVolume = GetEllipsoidVolume(parentRadius);     // this returned volume is ellipse like
                double inferredRadius = GetEllipsoidRadius(parentVolume);       // this returned radius is an average (treats volume like it came from a sphere)
                double destroyedMass = getMassByRadius(inferredRadius, null);

                if (destroyedMass.IsNearZero() || destroyedMass < 0)
                {
                    return null;
                }

                MineralDNA[] mineralDefinitions = getMineralsByDestroyedMass(destroyedMass);
                return PositionMinerals(mineralDefinitions, MAXVELOCITY * .25);
            }

            /// <summary>
            /// Figure how much radius remains based on how much damage was done.  This remaining radius will be
            /// distributed among the child asteroids
            /// </summary>
            private static double GetVolumeToDestroy(double parentVolume, double overDamage, double maxOverDamage)
            {
                double keepPercent = UtilityCore.GetScaledValue(.75, 0, 1, maxOverDamage, overDamage);

                double retVal = parentVolume - (parentVolume * keepPercent);
                if (retVal < 0) retVal = 0;
                if (retVal > parentVolume) retVal = parentVolume;

                return retVal;
            }

            private static Tuple<int, MineralDNA[]>[] DistributeMinerals(MineralDNA[] mineralDefinitions, int[] destroyedIndicies)
            {
                // I was going to keep track of burdens, and assign each mineral to the least burdened shard, with some
                // randomness.  But that would be a lot of sorting and divisions and resorting
                //
                // There won't be enough minerals or destroyed shards to make all that effort worthwhile
                //
                // So instead, just randomly assigning them

                Random rand = StaticRandom.GetRandomForThread();

                List<MineralDNA>[] building = Enumerable.Range(0, destroyedIndicies.Length).
                    Select(o => new List<MineralDNA>()).
                    ToArray();

                foreach (MineralDNA mineral in mineralDefinitions)
                {
                    building[rand.Next(building.Length)].Add(mineral);
                }

                return Enumerable.Range(0, building.Length).
                    Select(o => Tuple.Create(destroyedIndicies[o], building[o].ToArray())).
                    Where(o => o.Item2.Length > 0).
                    ToArray();
            }

            private static Vector3D GetEllipsoidRadius(IEnumerable<ITriangle> centeredHull)
            {
                Point3D[] points = centeredHull.
                    SelectMany(o => o.PointArray).
                    ToArray();

                return new Vector3D(
                    points.Max(o => Math.Abs(o.X)),
                    points.Max(o => Math.Abs(o.Y)),
                    points.Max(o => Math.Abs(o.Z)));
            }
            private static double GetEllipsoidVolume(Vector3D radius)
            {
                const double FOURTHIRDSPI = 4d / 3d * Math.PI;
                return FOURTHIRDSPI * radius.X * radius.Y * radius.Z;
            }
            private static double GetEllipsoidRadius(double volume)
            {
                const double THREEQUARTEROVERPI = .75d / Math.PI;
                const double ONETHIRD = 1d / 3d;
                return Math.Pow(volume * THREEQUARTEROVERPI, ONETHIRD);
            }

            private AsteroidOrMineralDefinition[] PositionMinerals(MineralDNA[] minerals, double? maxRandomVelocity)
            {
                double positionRange = _radiusMin * .05;

                #region build parts

                PartSeparator_Part[] parts = new PartSeparator_Part[minerals.Length];

                for (int cntr = 0; cntr < minerals.Length; cntr++)
                {
                    //NOTE: Copied logic from Mineral that gets collision points and mass

                    // Points
                    Point3D[] mineralPoints = UtilityWPF.GetPointsFromMesh((MeshGeometry3D)_sharedVisuals.Value.GetMineralMesh(minerals[cntr].MineralType));

                    // Mass
                    double mass = minerals[cntr].Density * minerals[cntr].Volume;

                    // Store it
                    parts[cntr] = new PartSeparator_Part(mineralPoints, mass, Math3D.GetRandomVector_Spherical(positionRange).ToPoint(), Math3D.GetRandomRotation());
                }

                #endregion

                #region pull apart

                if (parts.Length > 1)
                {
                    bool dummy;
                    CollisionHull[] hulls = PartSeparator.Separate(out dummy, parts, _world);

                    foreach (CollisionHull hull in hulls)
                    {
                        hull.Dispose();
                    }
                }

                #endregion

                #region build return

                List<AsteroidOrMineralDefinition> retVal = new List<AsteroidOrMineralDefinition>();

                for (int cntr = 0; cntr < parts.Length; cntr++)
                {
                    Vector3D velocity = new Vector3D(0, 0, 0);
                    if (maxRandomVelocity != null)
                    {
                        velocity = Math3D.GetRandomVector_Spherical(maxRandomVelocity.Value);
                    }

                    retVal.Add(new AsteroidOrMineralDefinition(parts[cntr], minerals[cntr], velocity));
                }

                #endregion

                return retVal.ToArray();
            }

            private IMapObject[] ConvertToMapObjects(AsteroidOrMineralDefinition[] items, Point3D parentPos, Vector3D parentVel, Quaternion parentRot)
            {
                IMapObject[] retVal = new IMapObject[items.Length];

                RotateTransform3D rotate = new RotateTransform3D(new QuaternionRotation3D(parentRot));

                for (int cntr = 0; cntr < retVal.Length; cntr++)
                {
                    Point3D position = parentPos + rotate.Transform(items[cntr].Part.Position.ToVector());

                    if (items[cntr].IsAsteroid)
                    {
                        // Asteroid
                        AsteroidExtra extra = new AsteroidExtra()
                        {
                            Triangles = TriangleIndexed.Clone_Transformed(items[cntr].AsteroidTriangles, rotate),
                            GetMineralsByDestroyedMass = _getMineralsByDestroyedMass,
                            MineralMaterialID = _mineralMaterialID,
                            MinChildRadius = _minChildRadius,
                            RandomRotation = false,
                            SelfDestructAfterElapse = items[cntr].ShouldAsteroidSelfDestruct ? StaticRandom.NextPercent(SELFDESTRUCTTIME, .5) : (double?)null,
                            MineralsWhenSelfDestruct = items[cntr].MineralsAfterSelfDestruct,
                        };

                        retVal[cntr] = new Asteroid(items[cntr].AsteroidRadius, _getMassByRadius, position, _world, _map, _materialID, extra);
                    }
                    else
                    {
                        // Mineral
                        MineralDNA mindef = items[cntr].MineralDefinition;
                        double densityMult = mindef.Density / Mineral.GetSettingsForMineralType(mindef.MineralType).Density;
                        retVal[cntr] = new Mineral(mindef.MineralType, position, mindef.Volume, _world, _mineralMaterialID, _sharedVisuals.Value, densityMult, mindef.Scale);
                    }

                    retVal[cntr].PhysicsBody.Velocity = parentVel + rotate.Transform(items[cntr].Velocity);

                    // Need to be careful if setting this.  Too much, and it will come apart unnaturally
                    //retVal[cntr].PhysicsBody.AngularVelocity = ;
                }

                return retVal;
            }

            private void AddToMap(IMapObject[] items)
            {
                foreach (var item in items)
                {
                    if (item is Asteroid)
                    {
                        _map.AddItem((Asteroid)item);      // the add doesn't allow base types to be passed in
                    }
                    else if (item is Mineral)
                    {
                        _map.AddItem((Mineral)item);
                    }
                    else
                    {
                        throw new ApplicationException("Unknown type of child");
                    }
                }
            }

            #region OLD

            private IMapObject[] GetChildAsteroids_ORIG(Tuple<Point3D, Vector3D, double>[] hits, double overDamage, Point3D parentPos, Vector3D parentVel)
            {
                const double MAXOVERDMG = 13;       // overdamage of 1 is the smallest value (the asteroid was barely destroyed).  Larger values are overkill, and the asteroid becomes more fully destroyed
                const int MAXCHILDREN = 5;

                HullVoronoiExploder_Response shards = HullVoronoiExploder.ShootHull(_parent._triangles, hits);

                // Figure out how much volume should permanently survive
                // Figure out the mineral value of the destroyed volume
                //
                // Figure out which asteroid shards will be permanent, and which will dissapear after a short time (temps should be a darker color?)
                //
                // Figure out which of the temp asteroids should contain minerals


                //------------------------TODO: Rework these
                #region Child asteroid sizes

                // Figure out the radius of the child asteroids
                double radius = GetTotalChildRadius(_radius, overDamage, MAXOVERDMG);

                // Get the volumes of the child asteroids
                double[] asteroidVolumes = null;
                if (radius > _minChildRadius)
                {
                    double totalVolume = 4d / 3d * Math.PI * radius * radius * radius;
                    double minVolume = 4d / 3d * Math.PI * _minChildRadius * _minChildRadius * _minChildRadius;

                    int numChildren = GetNumChildren(radius, MAXCHILDREN, totalVolume, minVolume, overDamage, MAXOVERDMG);
                    asteroidVolumes = GetChildVolumes(numChildren, totalVolume, minVolume);
                }
                else
                {
                    // Not enough left, don't create any child asteroids
                    radius = 0;
                    asteroidVolumes = new double[0];
                }

                #endregion
                #region Mineral sizes

                MineralDNA[] mineralDefinitions = null;
                if (_getMineralsByDestroyedMass != null)
                {
                    //double destroyedMass = GetDestroyedMass(Math3D.Avg(_radius.X, _radius.Y, _radius.Z), radius, _getMassByRadius);       // using avg had too many cases where the returned mass was negative
                    double destroyedMass = GetDestroyedMass(Math1D.Max(_radius.X, _radius.Y, _radius.Z), radius, _getMassByRadius);
                    if (destroyedMass > 0)      // child radius is calculated using max of _radius, but avg was passed to the getmass method.  So there's a chance that getmass returns negative
                    {
                        mineralDefinitions = _getMineralsByDestroyedMass(destroyedMass);
                    }
                }

                if (mineralDefinitions == null)
                {
                    mineralDefinitions = new MineralDNA[0];
                }

                #endregion
                //------------------------


                if (asteroidVolumes.Length == 0 && mineralDefinitions.Length == 0)
                {
                    // Nothing to spawn
                    return null;
                }

                // Figure out positions
                AsteroidOrMineralDefinition[] children = PositionChildAsteroidsAndMinerals(asteroidVolumes, mineralDefinitions);

                #region Create IMapObjects

                IMapObject[] retVal = new IMapObject[children.Length];

                for (int cntr = 0; cntr < retVal.Length; cntr++)
                {
                    Point3D position = parentPos + children[cntr].Part.Position.ToVector();

                    if (children[cntr].IsAsteroid)
                    {
                        // Asteroid
                        AsteroidExtra extra = new AsteroidExtra()
                        {
                            Triangles = children[cntr].AsteroidTriangles,
                            GetMineralsByDestroyedMass = _getMineralsByDestroyedMass,
                            MineralMaterialID = _mineralMaterialID,
                            MinChildRadius = _minChildRadius,
                        };

                        retVal[cntr] = new Asteroid(children[cntr].AsteroidRadius, _getMassByRadius, position, _world, _map, _materialID, extra);
                    }
                    else
                    {
                        // Mineral
                        MineralDNA mindef = children[cntr].MineralDefinition;
                        double densityMult = mindef.Density / Mineral.GetSettingsForMineralType(mindef.MineralType).Density;
                        retVal[cntr] = new Mineral(mindef.MineralType, position, mindef.Volume, _world, _mineralMaterialID, _sharedVisuals.Value, densityMult, mindef.Scale);
                    }

                    retVal[cntr].PhysicsBody.Rotation = children[cntr].Part.Orientation;

                    Vector3D velFromCenter = children[cntr].Part.Position.ToVector().ToUnit(false);
                    velFromCenter *= UtilityCore.GetScaledValue(1, 4, 1, MAXOVERDMG, overDamage);

                    retVal[cntr].PhysicsBody.Velocity = parentVel + velFromCenter;

                    retVal[cntr].PhysicsBody.AngularVelocity = Math3D.GetRandomVector_Spherical(UtilityCore.GetScaledValue(.5, 8, 1, MAXOVERDMG, overDamage));
                }

                #endregion

                return retVal;
            }

            /// <summary>
            /// Figure how much radius remains based on how much damage was done.  This remaining radius will be
            /// distributed among the child asteroids
            /// </summary>
            private static double GetTotalChildRadius(Vector3D parentRadius, double overDamage, double maxOverDamage)
            {
                double retVal = Math1D.Max(parentRadius.X, parentRadius.Y, parentRadius.Z);

                double keepPercent = UtilityCore.GetScaledValue(.75, 0, 1, maxOverDamage, overDamage);
                if (keepPercent > .99)     // this can happen if overDamage < 1
                {
                    keepPercent = .99;
                }

                retVal *= keepPercent;

                return retVal;
            }

            /// <summary>
            /// This is how many child asteroids to create
            /// </summary>
            private static int GetNumChildren(double radius, int maxChildren, double totalVolume, double minVolume, double overDamage, double maxOverDamage)
            {
                // Max is how many times min can go into total
                int max = Convert.ToInt32(Math.Floor(totalVolume / minVolume));
                if (max > maxChildren)
                {
                    max = maxChildren;
                }

                // The stronger the hit, the more likely of smaller debris
                int retVal = Convert.ToInt32(Math.Round(UtilityCore.GetScaledValue(1d, max, 1d, maxOverDamage / 2d, overDamage)));
                if (retVal < 1)
                {
                    retVal = 1;
                }

                retVal += StaticRandom.NextDouble() < .33 ? 1 : 0;      // give a small chance of creating one extra
                if (retVal > max)
                {
                    retVal = max;
                }

                return retVal;
            }

            /// <summary>
            /// This randomly distributes volume to the child asteroids, so that the sum of the children matches the
            /// total passed in
            /// </summary>
            private static double[] GetChildVolumes(int numChildren, double totalVolume, double minVolume)
            {
                // Figure out the volume of each child asteroid: min + rand(remaining)
                double[] volumes = new double[numChildren];
                double remainingVolume = totalVolume - (minVolume * numChildren);

                for (int cntr = 0; cntr < numChildren - 1; cntr++)
                {
                    double addVolume = StaticRandom.NextDouble(remainingVolume);
                    remainingVolume -= addVolume;
                    volumes[cntr] = minVolume + addVolume;
                }

                volumes[volumes.Length - 1] = minVolume + remainingVolume;

                return volumes;
            }

            /// <summary>
            /// Converts radius into mass, then subtracts
            /// NOTE: The actual asteroids are ellipses.  The single radius value is just an approximation
            /// </summary>
            private static double GetDestroyedMass(double parentRadius, double childRadius, Func<double, ITriangleIndexed[], double> getMassByRadius)
            {
                double parentMass = getMassByRadius(parentRadius, null);

                if (childRadius > 0)
                {
                    double childMass = getMassByRadius(childRadius, null);

                    return parentMass - childMass;
                }
                else
                {
                    return parentMass;
                }
            }

            /// <summary>
            /// Defines the child asteroid shapes, then finds positions/orientations for all child asteroids and minerals (makes
            /// sure nothing overlaps)
            /// </summary>
            private AsteroidOrMineralDefinition[] PositionChildAsteroidsAndMinerals(double[] asteroidVolumes, MineralDNA[] mineralDefinitions)
            {
                const double FOURTHIRDSPI = 4d / 3d * Math.PI;
                const double ONETHRID = 1d / 3d;

                double positionRange = _radiusMin * .05;

                #region Asteroids

                PartSeparator_Part[] asteroidParts = new PartSeparator_Part[asteroidVolumes.Length];
                ITriangleIndexed[][] asteroidTriangles = new ITriangleIndexed[asteroidVolumes.Length][];
                double[] asteroidRadii = new double[asteroidVolumes.Length];

                for (int cntr = 0; cntr < asteroidVolumes.Length; cntr++)
                {
                    // r^3=v/(4/3pi)
                    asteroidRadii[cntr] = Math.Pow(asteroidVolumes[cntr] / FOURTHIRDSPI, ONETHRID);

                    asteroidTriangles[cntr] = GetHullTriangles_Initial(asteroidRadii[cntr]);

                    double currentMass = _getMassByRadius(asteroidRadii[cntr], asteroidTriangles[cntr]);

                    asteroidParts[cntr] = new PartSeparator_Part(asteroidTriangles[cntr][0].AllPoints, currentMass, Math3D.GetRandomVector_Spherical(positionRange).ToPoint(), Math3D.GetRandomRotation());
                }

                #endregion
                #region Minerals

                PartSeparator_Part[] mineralParts = new PartSeparator_Part[mineralDefinitions.Length];

                for (int cntr = 0; cntr < mineralDefinitions.Length; cntr++)
                {
                    //NOTE: Copied logic from Mineral that gets collision points and mass

                    // Points
                    Point3D[] mineralPoints = UtilityWPF.GetPointsFromMesh((MeshGeometry3D)_sharedVisuals.Value.GetMineralMesh(mineralDefinitions[cntr].MineralType));

                    // Mass
                    double mass = mineralDefinitions[cntr].Density * mineralDefinitions[cntr].Volume;

                    // Store it
                    mineralParts[cntr] = new PartSeparator_Part(mineralPoints, mass, Math3D.GetRandomVector_Spherical(positionRange).ToPoint(), Math3D.GetRandomRotation());
                }

                #endregion

                #region Pull Apart

                if (asteroidParts.Length + mineralParts.Length > 1)
                {
                    PartSeparator_Part[] allParts = UtilityCore.ArrayAdd(asteroidParts, mineralParts);

                    bool dummy;
                    CollisionHull[] hulls = PartSeparator.Separate(out dummy, allParts, _world);

                    foreach (CollisionHull hull in hulls)
                    {
                        hull.Dispose();
                    }
                }

                #endregion

                #region Build Return

                List<AsteroidOrMineralDefinition> retVal = new List<AsteroidOrMineralDefinition>();

                for (int cntr = 0; cntr < asteroidParts.Length; cntr++)
                {
                    retVal.Add(new AsteroidOrMineralDefinition(asteroidParts[cntr], asteroidTriangles[cntr], asteroidRadii[cntr], new Vector3D(), false, null));
                }

                for (int cntr = 0; cntr < mineralParts.Length; cntr++)
                {
                    retVal.Add(new AsteroidOrMineralDefinition(mineralParts[cntr], mineralDefinitions[cntr], new Vector3D()));
                }

                #endregion

                return retVal.ToArray();
            }

            #endregion

            #endregion
        }

        #endregion
        #region class: MineralDefinition

        //public class MineralDefinition
        //{
        //    public MineralDefinition(MineralType mineralType, double volumeInCubicMeters = .15d, double densityMult = 1d, double scale = 1d)
        //    {
        //        this.MineralType = mineralType;
        //        this.VolumeInCubicMeters = volumeInCubicMeters;
        //        this.DensityMult = densityMult;
        //        this.Scale = scale;
        //    }
        //    public MineralDefinition(Mineral mineral)
        //    {
        //        this.MineralType = mineral.MineralType;
        //        this.VolumeInCubicMeters = mineral.VolumeInCubicMeters;
        //        this.DensityMult = mineral.Density / Mineral.GetSettingsForMineralType(this.MineralType).Density;
        //        this.Scale = mineral.Scale;
        //    }
        //    public MineralDefinition(Cargo_Mineral mineral, double scale)
        //    {
        //        this.MineralType = mineral.MineralType;
        //        this.VolumeInCubicMeters = mineral.Volume;
        //        this.DensityMult = mineral.Density / Mineral.GetSettingsForMineralType(this.MineralType).Density;
        //        this.Scale = scale;
        //    }

        //    public readonly MineralType MineralType;
        //    public readonly double VolumeInCubicMeters;
        //    /// <summary>
        //    /// This gets multiplied by Mineral.GetSettingsForMineralType(this.MineralType).Density to get the final density
        //    /// </summary>
        //    public readonly double DensityMult;
        //    public readonly double Scale;

        //    public double DensityFinal
        //    {
        //        get
        //        {
        //            return Mineral.GetSettingsForMineralType(this.MineralType).Density * this.DensityMult;
        //        }
        //    }
        //}

        #endregion

        #region Declaration Section

        public const string PARTTYPE = nameof(Asteroid);

        private readonly Map _map;
        private readonly HitTracker2 _hitTracker;

        private readonly ITriangleIndexed[] _triangles;

        private double _elapsedAge = 0;     // this is only updated if marked for self destruct
        private double? _selfDestructAfterElapse;
        private readonly MineralDNA[] _mineralsWhenSelfDestruct;

        private readonly double _pierceSpeed100Percent;

        private bool _isDestroying = false;

        #endregion

        #region Constructor

        public Asteroid(double radius, Func<double, ITriangleIndexed[], double> getMassByRadius, Point3D position, World world, Map map, int materialID, AsteroidExtra extra = null)
        {
            extra = extra ?? new AsteroidExtra();

            _map = map;
            _triangles = extra.Triangles ?? GetHullTriangles(radius);

            _selfDestructAfterElapse = extra.SelfDestructAfterElapse;
            _mineralsWhenSelfDestruct = extra.MineralsWhenSelfDestruct;
            if (_selfDestructAfterElapse != null)
            {
                // Don't bother taking damage
                _isDestroying = true;
            }

            _pierceSpeed100Percent = extra.PierceSpeed100Percent;

            // The asteroid is roughly an ellipse, so get the radius of that ellipse
            Point3D[] points = _triangles.SelectMany(o => o.PointArray).ToArray();
            this.RadiusVect = new Vector3D(
                points.Max(o => Math.Abs(o.X)),
                points.Max(o => Math.Abs(o.Y)),
                points.Max(o => Math.Abs(o.Z)));

            this.Radius = radius;
            double mass = getMassByRadius(radius, _triangles);

            #region WPF Model

            Tuple<Model3D, Visual3D[]> visuals;
            if (extra.GetVisual == null)
            {
                // Material
                MaterialGroup materials = new MaterialGroup();
                materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(WorldColors.Asteroid_Color)));
                materials.Children.Add(WorldColors.Asteroid_Specular);

                // Geometry Model
                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = materials;
                geometry.BackMaterial = materials;
                geometry.Geometry = UtilityWPF.GetMeshFromTriangles(_triangles);

                // Model Visual
                ModelVisual3D visual = new ModelVisual3D();
                visual.Content = geometry;

                visuals = new Tuple<Model3D, Visual3D[]>(geometry, new[] { visual });
            }
            else
            {
                visuals = extra.GetVisual(_triangles);
            }

            this.Model = visuals.Item1;

            #endregion

            #region Physics Body

            Transform3DGroup transform = new Transform3DGroup();
            if (extra.RandomRotation)
            {
                transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(Math3D.GetRandomRotation())));
            }
            transform.Children.Add(new TranslateTransform3D(position.ToVector()));

            using (CollisionHull hull = CollisionHull.CreateConvexHull(world, 0, _triangles[0].AllPoints))
            {
                this.PhysicsBody = new Body(hull, transform.Value, mass, visuals.Item2);
                //this.PhysicsBody.IsContinuousCollision = true;
                this.PhysicsBody.MaterialGroupID = materialID;
                this.PhysicsBody.LinearDamping = .01d;
                this.PhysicsBody.AngularDamping = new Vector3D(.01d, .01d, .01d);

                //this.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(Body_ApplyForceAndTorque);
            }

            #endregion

            this.CreationTime = DateTime.UtcNow;

            _hitTracker = new HitTracker2(world, map, materialID, this, this.RadiusVect, extra.MinChildRadius, getMassByRadius, extra.GetMineralsByDestroyedMass, extra.MineralMaterialID);
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

        public bool IsDisposed
        {
            get
            {
                //return _isDestroying || _hitTracker.IsDestroying || this.PhysicsBody.IsDisposed;      //NOOOOO!!!!!!!!!     when this is true, the map won't return it in GetItems.  It should only return true when it's actually disposed
                return this.PhysicsBody.IsDisposed;
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

        public void Update_MainThread(double elapsedTime)
        {
            if (_selfDestructAfterElapse == null)
            {
                return;
            }

            _elapsedAge += elapsedTime;

            if (_elapsedAge < _selfDestructAfterElapse.Value)
            {
                return;
            }

            _selfDestructAfterElapse = null;        // make sure this only self destructs once

            Point3D position = this.PositionWorld;
            Vector3D velocity = this.VelocityWorld;

            //NOTE: It is expected that something is listening to Map.Map_ItemRemoved, and is disposing those objects
            if (!_map.RemoveItem(this))
            {
                return;
            }

            // Replace with minerals
            if (_mineralsWhenSelfDestruct != null && _mineralsWhenSelfDestruct.Length > 0)
            {
                _hitTracker.PlaceMinerals(_mineralsWhenSelfDestruct, position, velocity * .75, 5);
            }
        }
        public void Update_AnyThread(double elapsedTime)
        {
        }

        public int? IntervalSkips_MainThread
        {
            get
            {
                return 15;
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

        public Vector3D RadiusVect
        {
            get;
            private set;
        }

        #endregion

        #region Public Methods

        public virtual AsteroidDNA GetNewDNA()
        {
            return new AsteroidDNA()
            {
                PartType = PARTTYPE,
                Position = this.PositionWorld,
                Radius = this.Radius,
                Orientation = this.PhysicsBody.Rotation,
                Velocity = this.PhysicsBody.Velocity,
                AngularVelocity = this.PhysicsBody.AngularVelocity,
                Triangles = _triangles,
            };
        }

        //NOTE: These are in world coords
        /// <summary>
        /// This will take damage based on the severity of the impact
        /// (impacted with a projectile)
        /// </summary>
        public void TakeDamage_Projectile(double mass, Point3D position, Vector3D velocity)
        {
            if (_isDestroying || _hitTracker.IsDestroying)
            {
                return;
            }

            Vector3D projected = (velocity - this.VelocityWorld).GetProjectedVector(velocity);      // a negative projected means the asteroid is flying the same direction as the projectile, but faster (the asteroid ran into the projectile)

            double speed = projected.Length;
            double piercePercent = Math1D.PositiveSCurve(speed, _pierceSpeed100Percent) / _pierceSpeed100Percent;

            _hitTracker.TakeDamage(position, projected * mass, piercePercent);
        }
        /// <summary>
        /// This will take damage based on the severity of the impact
        /// (impacted with another asteroid)
        /// </summary>
        public void TakeDamage_Asteroid(double mass, Point3D position, Vector3D velocity)
        {
            if (_isDestroying || _hitTracker.IsDestroying)
            {
                return;
            }

            //TODO: A projectile pierces, but asteroids are blunt impacts.  Emulate blunt by making several shallow impacts
            //across an area (the larger the mass, the wider the area) (the vectors should be slightly diverging)
            _hitTracker.TakeDamage(position, velocity * (mass * .05), .05);
        }
        /// <summary>
        /// This will take damage based on the severity of the impact
        /// (impacted with something else)
        /// </summary>
        public void TakeDamage_Other(double mass, Point3D position, Vector3D velocity)
        {
            if (_isDestroying || _hitTracker.IsDestroying)
            {
                return;
            }

            _hitTracker.TakeDamage(position, velocity * (mass * .25), .25);
        }

        /// <summary>
        /// This is exposed so it can be called externally on a separate thread, then call this asteroid's constructor
        /// in the main thread once this returns
        /// </summary>
        public static ITriangleIndexed[] GetHullTriangles(double radius)
        {
            // This holds the mesh so far before an exception occurs
            ITriangleIndexed[] initialHull = null;

            // Math3D.SliceLargeTriangles_Smooth + Math3D.RemoveThinTriangles sometimes throws an exception.  So try a few times before giving up
            for (int cntr = 0; cntr < 20; cntr++)
            {
                try
                {
                    initialHull = Asteroid.GetHullTriangles_Initial(radius);

                    return SmoothTriangles(initialHull);
                }
                catch (Exception) { }
            }

            if (initialHull == null)
            {
                throw new ApplicationException("Couldn't create a hull");
            }
            else
            {
                return initialHull;
            }
        }
        public static ITriangleIndexed[] SmoothTriangles(ITriangleIndexed[] hull)
        {
            const double RATIO = .93;

            // This holds the mesh so far before an exception occurs
            ITriangleIndexed[] retVal = hull;

            // Math3D.SliceLargeTriangles_Smooth + Math3D.RemoveThinTriangles sometimes throws an exception.  So try a few times before giving up
            try
            {
                retVal = hull;

                // Remove excessively thin triangles
                ITriangleIndexed[] slicedHull = Math3D.RemoveThinTriangles(hull, RATIO);
                retVal = slicedHull;

                // Now round it out (which adds triangles)
                Point3D[] hullPoints = slicedHull[0].AllPoints;

                double[] lengths = TriangleIndexed.GetUniqueLines(slicedHull).
                    Select(o => (hullPoints[o.Item1] - hullPoints[o.Item2]).Length).
                    ToArray();

                double avgLen = lengths.Average();
                double maxLen = lengths.Max();
                double sliceLen = UtilityCore.GetScaledValue(avgLen, maxLen, 0, 1, .333);

                ITriangleIndexed[] smoothHull = Math3D.SliceLargeTriangles_Smooth(slicedHull, sliceLen);
                retVal = smoothHull;

                // Remove again
                ITriangleIndexed[] secondSlicedHull = Math3D.RemoveThinTriangles(smoothHull, RATIO);
                retVal = secondSlicedHull;

                // Sometimes there were holes, so call convex to fix that
                ITriangleIndexed[] convex = Math3D.GetConvexHull(Triangle.GetUniquePoints(secondSlicedHull));

                return convex;
            }
            catch (Exception) { }

            if (retVal == null)
            {
                throw new ApplicationException("Couldn't create a hull");
            }
            else
            {
                return retVal;
            }
        }

        #endregion

        #region Private Methods

        private static ITriangleIndexed[] GetHullTriangles_Initial(double radius)
        {
            const int NUMPOINTS = 40;       // too many, and it looks too perfect

            Exception lastException = null;
            Random rand = StaticRandom.GetRandomForThread();

            for (int infiniteLoopCntr = 0; infiniteLoopCntr < 50; infiniteLoopCntr++)		// there is a slight chance that the convex hull generator will choke on the inputs.  If so just retry
            {
                try
                {
                    double minRadius = radius * .9d;

                    Point3D[] points = new Point3D[NUMPOINTS];

                    // Make a point cloud
                    for (int cntr = 0; cntr < NUMPOINTS; cntr++)
                    {
                        points[cntr] = Math3D.GetRandomVector_Spherical(minRadius, radius).ToPoint();
                    }

                    // Squash it
                    Transform3DGroup transform = new Transform3DGroup();
                    transform.Children.Add(new ScaleTransform3D(.33d + (rand.NextDouble() * .66d), .33d + (rand.NextDouble() * .66d), 1d));		// squash it
                    transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(Math3D.GetRandomRotation())));		// giving it a random rotation, since it's always squashed along the same axiis
                    transform.Transform(points);

                    // Get a hull that wraps those points
                    ITriangleIndexed[] retVal = Math3D.GetConvexHull(points.ToArray());

                    // Get rid of unused points
                    retVal = TriangleIndexed.Clone_CondensePoints(retVal);

                    // Exit Function
                    return retVal;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }
            }

            throw new ApplicationException(lastException.ToString());
        }

        #endregion
    }

    #region class: AsteroidExtra

    public class AsteroidExtra
    {
        public ITriangleIndexed[] Triangles = null;

        public Func<double, MineralDNA[]> GetMineralsByDestroyedMass = null;

        public int MineralMaterialID = -1;

        public double MinChildRadius = 1d;

        public bool RandomRotation = true;

        public Func<ITriangleIndexed[], Tuple<Model3D, Visual3D[]>> GetVisual = null;

        public double? SelfDestructAfterElapse = null;
        public MineralDNA[] MineralsWhenSelfDestruct = null;

        /// <summary>
        /// When the relative speed difference is >= this, then the projectile will pierce at 100%
        /// </summary>
        /// <remarks>
        /// The projectile gun's default speed is 15, so the ship would have to be flying pretty fast before firing (or the asteroid coming at the ship
        /// pretty fast)
        /// </remarks>
        public double PierceSpeed100Percent = 40;
    }

    #endregion
    #region class: AsteroidDNA

    public class AsteroidDNA : MapPartDNA
    {
        public ITriangleIndexed[] Triangles { get; set; }
    }

    #endregion
}
