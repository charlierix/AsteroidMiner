using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.MapParts;
using Game.Newt.v2.GameItems.ShipParts;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.AsteroidMiner.MapParts
{
    public class Asteroid : IMapObject
    {
        #region Class: HitTracker1

        /// <summary>
        /// This simply adds up the lengths of the collisions, and splits the asteroid when the sum exceeds the radius
        /// </summary>
        private class HitTracker1
        {
            #region Class: AsteroidOrMineralDefinition

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
                _radiusMin = Math3D.Min(_radius.X, _radius.Y, _radius.Z);
            }

            #endregion

            #region Public Methods

            // If this returns something, it will be the asteroid split into pieces, as well as some minerals
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
                    double destroyedMass = GetDestroyedMass(Math3D.Max(_radius.X, _radius.Y, _radius.Z), radius, _getMassByRadius);
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
                        retVal[cntr] = new Asteroid(children[cntr].AsteroidRadius, _getMassByRadius, position, _world, _map, _materialID, children[cntr].AsteroidTriangles, _getMineralsByDestroyedMass, _mineralMaterialID, _minChildRadius);
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
                double retVal = Math3D.Max(parentRadius.X, parentRadius.Y, parentRadius.Z);

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
        #region Class: MineralDefinition

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

        public const string PARTTYPE = "Asteroid";

        private readonly Map _map;
        private readonly HitTracker1 _hitTracker;

        private readonly ITriangleIndexed[] _triangles;

        #endregion

        #region Constructor

        public Asteroid(double radius, Func<double, ITriangleIndexed[], double> getMassByRadius, Point3D position, World world, Map map, int materialID, Func<double, MineralDNA[]> getMineralsByDestroyedMass = null, int mineralMaterialID = -1, double minChildRadius = 1d)
            : this(radius, getMassByRadius, position, world, map, materialID, GetHullTriangles(radius), getMineralsByDestroyedMass, mineralMaterialID, minChildRadius) { }

        public Asteroid(double radius, Func<double, ITriangleIndexed[], double> getMassByRadius, Point3D position, World world, Map map, int materialID, ITriangleIndexed[] triangles, Func<double, MineralDNA[]> getMineralsByDestroyedMass = null, int mineralMaterialID = -1, double minChildRadius = 1d)
        {
            _map = map;
            _triangles = triangles;

            // The asteroid is roughly an ellipse, so get the radius of that ellipse
            Point3D[] points = triangles.SelectMany(o => o.PointArray).ToArray();
            this.RadiusVect = new Vector3D(
                points.Max(o => Math.Abs(o.X)),
                points.Max(o => Math.Abs(o.Y)),
                points.Max(o => Math.Abs(o.Z)));

            this.Radius = radius;
            double mass = getMassByRadius(radius, triangles);

            #region WPF Model

            // Material
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(WorldColors.AsteroidColor)));
            materials.Children.Add(WorldColors.AsteroidSpecular);

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;
            //geometry.Geometry = UtilityWPF.GetSphere(5, radius);
            geometry.Geometry = UtilityWPF.GetMeshFromTriangles(triangles);

            this.Model = geometry;

            // Model Visual
            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = geometry;

            #endregion

            #region Physics Body

            Transform3DGroup transform = new Transform3DGroup();
            transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(Math3D.GetRandomRotation())));
            transform.Children.Add(new TranslateTransform3D(position.ToVector()));

            using (CollisionHull hull = CollisionHull.CreateConvexHull(world, 0, triangles[0].AllPoints))
            {
                this.PhysicsBody = new Body(hull, transform.Value, mass, new Visual3D[] { visual });
                //this.PhysicsBody.IsContinuousCollision = true;
                this.PhysicsBody.MaterialGroupID = materialID;
                this.PhysicsBody.LinearDamping = .01d;
                this.PhysicsBody.AngularDamping = new Vector3D(.01d, .01d, .01d);

                //this.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(Body_ApplyForceAndTorque);
            }

            #endregion

            this.CreationTime = DateTime.Now;

            _hitTracker = new HitTracker1(world, map, materialID, this, triangles, this.RadiusVect, minChildRadius, getMassByRadius, getMineralsByDestroyedMass, mineralMaterialID);
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
            TakeDamage(position, velocity * mass);
        }
        /// <summary>
        /// This will take damage based on the severity of the impact
        /// (impacted with another asteroid)
        /// </summary>
        public void TakeDamage_Asteroid(double mass, Point3D position, Vector3D velocity)
        {
            //TODO: A projectile pierces, but asteroids are blunt impacts.  Emulate blunt by making several shallow impacts
            //across an area (the larger the mass, the wider the area) (the vectors should be slightly diverging)
            TakeDamage(position, velocity * (mass * .05));
        }
        /// <summary>
        /// This will take damage based on the severity of the impact
        /// (impacted with something else)
        /// </summary>
        public void TakeDamage_Other(double mass, Point3D position, Vector3D velocity)
        {
            TakeDamage(position, velocity * (mass * .25));
        }

        /// <summary>
        /// This is exposed so it can be called externally on a separate thread, then call this asteroid's constructor
        /// in the main thread once this returns
        /// </summary>
        public static ITriangleIndexed[] GetHullTriangles(double radius)
        {
            const double RATIO = .93;

            // This holds the mesh so far before an exception occurs
            ITriangleIndexed[] retVal = null;

            // Math3D.SliceLargeTriangles_Smooth + Math3D.RemoveThinTriangles sometimes throws an exception.  So try a few times before giving up
            for (int cntr = 0; cntr < 20; cntr++)
            {
                try
                {
                    // Get randomly generated triangles
                    ITriangleIndexed[] hull = Asteroid.GetHullTriangles_Initial(radius);
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

                    return secondSlicedHull;
                }
                catch (Exception) { }
            }

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

        private void TakeDamage(Point3D position, Vector3D amount)
        {
            _hitTracker.TakeDamage(position, amount);




            // If this is really close to an existing impact, add it



            // Once the depth of the vector exceeds a certain amount, split up the asteroid (or maybe the sum of depths)
            // If they are punching into the center of the asteroid, the depth needs to be more.  If they are impacting a corner, make
            // it easier to only remove that corner shard




            #region Voronoi Thoughts

            // Various impacts and their depths will define fault lines (planes).  Use those planes to do a sort of reverse voronoi that
            // figures out where to place control points (then maybe randomly add a few more)
            //
            // Then do a proper voronoi and clip with the asteroid hull to get the final shattered pieces
            //
            // Doing this two step will roughly split the asteroid along the faults, which is more realistic.  If the control points were
            // placed at the impacts, then the impacts would define the shards, which is the opposite of what's wanted

            // When figuring out where to place control points, use the distance between impacts to determine how far from the plane
            // the control point should be
            //
            // Then use the average depth to see if the control point should be deep or shallow

            // Another thing to consider when placing control points is to favor the deepest locations within the asteroid




            // Another option would be to instantly vaporize a chunk, leaving a crater:
            //      Define a control point some depth directly below the impact
            //      Randomly create several more control points in a somewhat circular pattern around that main control point
            //      Run voronoi
            //      Remove only the poly for the main control point (optionally divide it up as a temporary debris cloud)
            //
            // This would be good for medium sized impacts (or explosive projectiles)

            #endregion
        }

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

    #region Class: AsteroidDNA

    public class AsteroidDNA : MapPartDNA
    {
        public ITriangleIndexed[] Triangles { get; set; }
    }

    #endregion
}
