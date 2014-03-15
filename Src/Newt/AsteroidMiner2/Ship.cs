using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using Game.HelperClasses;
using Game.Newt.AsteroidMiner2.ShipEditor;
using Game.Newt.AsteroidMiner2.ShipParts;
using Game.Newt.HelperClasses;
using Game.Newt.NewtonDynamics;



using System.Diagnostics;
using Game.Newt.HelperClasses.Primitives3D;

namespace Game.Newt.AsteroidMiner2
{
    //TODO: Make sure the part visuals and hulls and mass breakdowns are aligned (not one along X and one along Z)
    //TODO: Support parts getting damaged/destroyed/repaired (probably keep the parts around maybe 75% mass, with a charred visual)
    //TODO: Improve the logic that checks for parts being too far away
    //TODO: Check for thrusters/weapons burning parts.  For rigid, a scan can be done up front.  Once joints are used, inter body checks need to be done when the joint moves
    //		- Thrusters that are blocked need to have an offset/reduction to where the force is applied

    public class Ship : IDisposable, IMapObject, IPartUpdatable
    {
        #region Class: PartSeparator

        /// <summary>
        /// This pulls apart parts that are intersecting with each other
        /// </summary>
        /// <remarks>
        /// There is just enough logic that I wanted a class instead of a bunch of loose methods
        /// 
        /// NOTE: There is a bug with thin parts (solar panels) where the parts will bounce back and forth.  I'm guessing it's because
        /// I take the average size, and the parts are thinner than that size - or maybe it's only when colliding with spheres
        /// 
        /// This is exposed as public so other testers can use it (not really meant to be used by production code)
        /// 
        /// This is copied from Game.Newt.Testers.OverlappingPartsWindow.PartSolver2
        /// (that's what I used to build and test it)
        /// </remarks>
        public static class PartSeparator
        {
            #region Class: Intersection

            private class Intersection
            {
                public Intersection(int index1, int index2, double avgSize1, double avgSize2, CollisionHull.IntersectionPoint[] intersections)
                {
                    this.Index1 = index1;
                    this.Index2 = index2;
                    this.AvgSize1 = avgSize1;
                    this.AvgSize2 = avgSize2;
                    this.Intersections = intersections;
                }

                public readonly int Index1;
                public readonly int Index2;

                public readonly double AvgSize1;
                public readonly double AvgSize2;

                public readonly CollisionHull.IntersectionPoint[] Intersections;
            }

            #endregion

            #region Declaration Section

            //TODO: Instead of a fixed number of steps, may want to stop when the distance of all moves gets below some percent (but that may
            //not catch some glitches where parts bounce back and forth)
            private const int MAXSTEPS = 50;		// using a max so it doesn't run really long.  After this many steps, the changes should be pretty minor anyway

            private const double IGNOREDEPTHPERCENT = .01d;

            private const double MOVEPERSTEPPERCENT = 1d;		// this seems to be stable with 100%, if nessassary, drop it down a bit so that parts don't move as far each step

            #endregion

            //TODO: Make a better version, probably a combination of pulling in and separating
            public static void PullInCrude(out bool changed, PartBase[] parts)
            {
                // Figure out the max radius
                double[] sizes = parts.Select(o => (o.ScaleActual.X + o.ScaleActual.Y + o.ScaleActual.Z) / 3d).ToArray();
                double largestPart = sizes.Max();
                double maxRadius = largestPart * 8d;
                double maxRadiusSquare = maxRadius * maxRadius;

                Point3D center = Math3D.GetCenter(parts.Select(o => Tuple.Create(o.Position, o.TotalMass)).ToArray());

                changed = false;

                for (int cntr = 0; cntr < parts.Length; cntr++)
                {
                    Vector3D offset = parts[cntr].Position - center;		//NOTE: This is just going to the center of the part, it's not considering the extents of the part (this method IS called crude)
                    if (offset.LengthSquared < maxRadiusSquare)
                    {
                        continue;
                    }

                    // Pull it straight in
                    double difference = offset.Length - maxRadius;
                    offset.Normalize();
                    offset *= difference * -1d;

                    parts[cntr].Position += offset;		//NOTE: I'm not going to change the center of mass

                    changed = true;
                }
            }

            public static CollisionHull[] Separate(out bool changed, PartBase[] parts, World world)
            {
                changed = false;

                bool[] hasMoved = new bool[parts.Length];		// defaults to false
                CollisionHull[] hulls = parts.Select(o => o.CreateCollisionHull(world)).ToArray();

                // Move the parts
                for (int cntr = 0; cntr < MAXSTEPS; cntr++)		// execution will break out of this loop early if parts are no longer intersecting
                {
                    Intersection[] intersections = GetIntersections(parts, hulls, hasMoved, world);
                    if (intersections.Length == 0)
                    {
                        break;
                    }

                    DoStep(intersections, parts, hasMoved);

                    changed = true;
                }

                // Ensure hulls are synced
                for (int cntr = 0; cntr < parts.Length; cntr++)
                {
                    if (hasMoved[cntr])
                    {
                        hulls[cntr].Dispose();
                        hulls[cntr] = parts[cntr].CreateCollisionHull(world);
                    }
                }

                // Exit Function
                return hulls;
            }

            #region Private Methods

            /// <summary>
            /// This finds intersections between all the hulls
            /// </summary>
            private static Intersection[] GetIntersections(PartBase[] parts, CollisionHull[] hulls, bool[] hasMoved, World world)
            {
                List<Intersection> retVal = new List<Intersection>();

                // Compare each hull to the others
                for (int outer = 0; outer < hulls.Length - 1; outer++)
                {
                    double? sizeOuter = null;

                    for (int inner = outer + 1; inner < hulls.Length; inner++)
                    {
                        // Rebuild hulls if nessessary
                        if (hasMoved[outer])
                        {
                            hulls[outer].Dispose();
                            hulls[outer] = parts[outer].CreateCollisionHull(world);
                            hasMoved[outer] = false;
                        }

                        if (hasMoved[inner])
                        {
                            hulls[inner].Dispose();
                            hulls[inner] = parts[inner].CreateCollisionHull(world);
                            hasMoved[inner] = false;
                        }

                        // Get intersecting points
                        CollisionHull.IntersectionPoint[] points = hulls[outer].GetIntersectingPoints_HullToHull(100, hulls[inner], 0);

                        if (points != null && points.Length > 0)
                        {
                            sizeOuter = sizeOuter ?? (parts[outer].ScaleActual.X + parts[outer].ScaleActual.X + parts[outer].ScaleActual.X) / 3d;
                            double sizeInner = (parts[inner].ScaleActual.X + parts[inner].ScaleActual.X + parts[inner].ScaleActual.X) / 3d;

                            double sumSize = sizeOuter.Value + sizeInner;
                            double minSize = sumSize * IGNOREDEPTHPERCENT;

                            // Filter out the shallow penetrations
                            //TODO: May need to add the lost distance to the remaining intersections
                            points = points.Where(o => o.PenetrationDistance > minSize).ToArray();

                            if (points != null && points.Length > 0)
                            {
                                retVal.Add(new Intersection(outer, inner, sizeOuter.Value, sizeInner, points));
                            }
                        }
                    }
                }

                // Exit Function
                return retVal.ToArray();
            }

            private static void DoStep(Intersection[] intersections, PartBase[] parts, bool[] hasMoved)
            {
                SortedList<int, List<Tuple<Vector3D?, Quaternion?>>> moves = new SortedList<int, List<Tuple<Vector3D?, Quaternion?>>>();

                double crazyScale = 1d;
                if (intersections.Length > parts.Length)
                {
                    // If there are lots of parts intersecting each other at once, this will move parts too far (because it does a scan of all intersections,
                    // then moves the parts using the sum of all intersections).  This is a crude attempt to soften that effect
                    crazyScale = Convert.ToDouble(parts.Length) / Convert.ToDouble(intersections.Length);
                }

                // Shoot through all the part pairs
                foreach (var intersection in intersections)
                {
                    double mass1 = parts[intersection.Index1].TotalMass;
                    double mass2 = parts[intersection.Index2].TotalMass;
                    double totalMass = mass1 + mass2;

                    double sumPenetration = intersection.Intersections.Sum(o => o.PenetrationDistance);		// there really needs to be a joke here.  Something about seven inches at a time
                    double avgPenetration = sumPenetration / Convert.ToDouble(intersection.Intersections.Length);

                    Vector3D direction = (parts[intersection.Index2].Position - parts[intersection.Index1].Position).ToUnit();

                    double sizeScale = MOVEPERSTEPPERCENT * (1d / Convert.ToDouble(intersection.Intersections.Length));

                    // Shoot through the intersecting points between these two parts
                    foreach (var intersectPoint in intersection.Intersections)
                    {
                        // The sum of scaledDistance needs to add up to avgPenetration
                        double percentDistance = intersectPoint.PenetrationDistance / sumPenetration;
                        double scaledDistance = avgPenetration * percentDistance;

                        // May not want to move the full distance in one step
                        scaledDistance *= MOVEPERSTEPPERCENT * crazyScale;

                        double distance1 = scaledDistance * ((totalMass - mass1) / totalMass);
                        double distance2 = scaledDistance * ((totalMass - mass2) / totalMass);

                        // Part1
                        Vector3D translation, torque;
                        Vector3D offset1 = intersectPoint.ContactPoint - parts[intersection.Index1].Position;
                        Math3D.SplitForceIntoTranslationAndTorque(out translation, out torque, offset1, direction * (-1d * distance1));
                        DoStepSprtAddForce(moves, intersection.Index1, translation, DoStepSprtRotate(torque, intersection.AvgSize1, sizeScale));		// don't use the full size, or the rotation won't even be noticable

                        // Part2
                        Vector3D offset2 = intersectPoint.ContactPoint - parts[intersection.Index2].Position;
                        Math3D.SplitForceIntoTranslationAndTorque(out translation, out torque, offset2, direction * distance2);
                        DoStepSprtAddForce(moves, intersection.Index2, translation, DoStepSprtRotate(torque, intersection.AvgSize2, sizeScale));
                    }
                }

                // Apply the movements
                DoStepSprtMove(parts, moves);

                // Remember which parts were modified
                foreach (int index in moves.Keys)
                {
                    hasMoved[index] = true;
                }
            }

            private static Quaternion? DoStepSprtRotate(Vector3D torque, double size, double penetrationScale)
            {
                const double MAXANGLE = 12d; //22.5d;

                if (Math3D.IsNearZero(torque))
                {
                    return null;
                }

                double length = torque.Length;
                Vector3D axis = torque / length;

                // Since the max torque will be a full penetration out at radius, that will be penetration cross radius.  So the length of that will be
                // roughly (size/2)^2
                //double maxExpected = Math.Pow(size * .33d, 2d);		// make the max size a bit smaller than half, since a max penetration would be really rare
                //double maxExpected = size * size;

                double maxExpected = (size * .5d) * (size * penetrationScale);

                // Make the angle to be some proportion between the torque's length and the average size of the part
                double angle = UtilityHelper.GetScaledValue_Capped(0d, MAXANGLE, 0d, maxExpected, length);

                return new Quaternion(axis, angle);
            }

            private static void DoStepSprtAddForce(SortedList<int, List<Tuple<Vector3D?, Quaternion?>>> moves, int index, Vector3D? translation, Quaternion? rotation)
            {
                if (!moves.ContainsKey(index))
                {
                    moves.Add(index, new List<Tuple<Vector3D?, Quaternion?>>());
                }

                moves[index].Add(Tuple.Create(translation, rotation));
            }

            private static void DoStepSprtMove(PartBase[] parts, SortedList<int, List<Tuple<Vector3D?, Quaternion?>>> moves)
            {
                foreach (int partIndex in moves.Keys)
                {
                    foreach (var move in moves[partIndex])
                    {
                        if (move.Item1 != null)
                        {
                            parts[partIndex].Position += move.Item1.Value;
                        }

                        if (move.Item2 != null)
                        {
                            parts[partIndex].Orientation = parts[partIndex].Orientation.RotateBy(move.Item2.Value);
                        }
                    }
                }
            }

            #endregion
        }

        #endregion
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
        #region Class: PartContainer

        protected class PartContainer
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
            public List<Brain> Brain = new List<Brain>();

            public List<SensorGravity> SensorGravity = new List<SensorGravity>();
            public List<SensorSpin> SensorSpin = new List<SensorSpin>();
            public List<SensorVelocity> SensorVelocity = new List<SensorVelocity>();

            public List<CameraColorRGB> CameraColorRGB = new List<CameraColorRGB>();

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
        #region Class: BuildPartsResults

        public class BuildPartsResults
        {
            public PartBase[] AllParts = null;
            public PartDNA[] DNA = null;
            public CollisionHull[] Hulls = null;

            public IPartUpdatable[] UpdatableParts = null;
            public int[] UpdatableIndices = null;

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

            public PartContainer Parts { get; set; }
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

        protected EditorOptions _options = null;
        protected ItemOptions _itemOptions = null;
        private ShipDNA _dna = null;

        private RadiationField _radiation = null;
        private IGravityField _gravity = null;
        private CameraPool _cameraPool = null;

        private PartContainer _parts = null;

        /// <summary>
        /// This should be set to true if the act of calling one of the part's update has a chance of slowly changing the
        /// ship's mass (like thrusters using fuel, or matter converters)
        /// </summary>
        private bool _hasMassChangingUpdatables = false;

        private double _lastMassRecalculateTime = 5d;
        private double _nextMatterTransferTime = 5d;        // since it multiplies by elapsed time, don't do this on the first tick - elasped time is larger than normal

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
        private Task<NeuralBucket> _neuralPoolAddTask = null;

        #endregion

        #region Constructor/Factory

        public static async Task<Ship> GetNewShipAsync(EditorOptions options, ItemOptions itemOptions, ShipDNA dna, World world, int materialID, RadiationField radiation, IGravityField gravity, CameraPool cameraPool, bool runNeural, bool repairPartPositions)
        {
            var construction = await GetNewShipConstructionAsync(options, itemOptions, dna, world, materialID, radiation, gravity, cameraPool, runNeural, repairPartPositions);
            return new Ship(construction);
        }

        protected static async Task<ShipConstruction> GetNewShipConstructionAsync(EditorOptions options, ItemOptions itemOptions, ShipDNA dna, World world, int materialID, RadiationField radiation, IGravityField gravity, CameraPool cameraPool, bool runNeural, bool repairPartPositions)
        {
            TaskScheduler currentContext = TaskScheduler.FromCurrentSynchronizationContext();

            ShipConstruction con = new ShipConstruction();

            con.Options = options;
            con.ItemOptions = itemOptions;
            con.Radiation = radiation;
            con.Gravity = gravity;
            con.CameraPool = cameraPool;

            // Parts
            con.Parts = new PartContainer();

            var dna_hulls = await BuildParts(con.Parts, dna, runNeural, repairPartPositions, world, options, itemOptions, radiation, gravity, cameraPool);

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
            con.PhysicsBody.MaterialGroupID = materialID;
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

            _parts = construction.Parts;
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
            if ((_parts.Fuel.Count > 0 && _parts.Thrust.Count > 0) ||
                (_parts.CargoBay.Count > 0 && (_parts.ConvertMatterToEnergy.Count > 0 || _parts.ConvertMatterToFuel.Count > 0)) ||
                (_parts.Energy.Count > 0 && _parts.Fuel.Count > 0 && (_parts.ConvertEnergyToFuel.Count > 0 || _parts.ConverterFuelToEnergy.Count > 0))
                )
            {
                _hasMassChangingUpdatables = true;
            }

            // Set up a neural processor on its own thread/task
            if (_parts.Links != null)
            {
                #region Add to neural pool

                // Create the bucket
                _parts.LinkBucket = new NeuralBucket(_parts.Links.SelectMany(o => UtilityHelper.Iterate(o.InternalLinks, o.ExternalLinks)).ToArray());

                // Add to the pool from another thread.  This way the lock while waiting to add won't tie up this thread
                _neuralPoolAddTask = Task.Run(() =>
                    {
                        // _parts.LinkBucket could go null at any moment, so grab local copies, one at a time
                        var parts = _parts;
                        if (parts == null)
                        {
                            return null;
                        }

                        var linkBucket = parts.LinkBucket;
                        if (linkBucket == null)
                        {
                            return null;
                        }

                        NeuralPool.Instance.Add(linkBucket);
                        return linkBucket;
                    });

                #endregion
            }

            this.ShouldRecalcMass_Large = false;
            this.ShouldRecalcMass_Small = false;

            this.CreationTime = DateTime.Now;
        }

        #region TOO ASYNC

        //protected static async Task<ShipConstruction> GetNewShipConstructionAsync(EditorOptions options, ItemOptions itemOptions, ShipDNA dna, World world, int materialID, RadiationField radiation, IGravityField gravity, CameraPool cameraPool, bool runNeural, bool repairPartPositions)
        //{
        //    TaskScheduler currentContext = TaskScheduler.FromCurrentSynchronizationContext();

        //    // Arbitrary thread
        //    Task<ShipConstruction> prepTask = Task.Run(async () =>
        //    {
        //        ShipConstruction construction = new ShipConstruction();

        //        construction.Options = options;
        //        construction.ItemOptions = itemOptions;
        //        construction.Radiation = radiation;
        //        construction.Gravity = gravity;
        //        construction.CameraPool = cameraPool;

        //        // Parts
        //        construction.Parts = new PartContainer();

        //        var dna_hulls = await BuildParts(construction.Parts, dna, runNeural, repairPartPositions, world, options, itemOptions, radiation, gravity, cameraPool, currentContext);

        //        construction.DNA = dna_hulls.Item1;
        //        construction.Hulls = dna_hulls.Item2;

        //        // Mass breakdown
        //        GetInertiaTensorAndCenterOfMass_Points(out construction.MassMatrix, out construction.CenterMass, construction.Parts.AllParts.ToArray(), construction.DNA.PartsByLayer.Values.SelectMany(o => o).ToArray(), itemOptions.MomentOfInertiaMultiplier);

        //        return construction;
        //    });

        //    // Main thread
        //    Task<ShipConstruction> retVal = prepTask.ContinueWith(t =>
        //    {
        //        #region WPF - main

        //        //TODO: Remember this so that flames and other visuals can be added/removed.  That way there will still only be one model visual
        //        //TODO: When joints are supported, some of the parts (or part groups) will move relative to the others.  There can still be a single model visual
        //        Model3DGroup models = new Model3DGroup();

        //        foreach (PartBase part in t.Result.Parts.AllParts)
        //        {
        //            models.Children.Add(part.Model);
        //        }

        //        t.Result.Model = models;

        //        ModelVisual3D visual = new ModelVisual3D();
        //        visual.Content = models;

        //        #endregion
        //        #region WPF - effects

        //        // These visuals will only show up in the main viewport (they won't be shared with the camera pool, so won't be visible to
        //        // other ships)

        //        t.Result.VisualEffects = new VisualEffects(itemOptions, t.Result.Parts.Thrust);

        //        #endregion

        //        #region Physics Body

        //        // For now, just make a composite collision hull out of all the parts
        //        CollisionHull hull = CollisionHull.CreateCompoundCollision(world, 0, t.Result.Hulls);

        //        t.Result.PhysicsBody = new Body(hull, Matrix3D.Identity, 1d, new Visual3D[] { visual, t.Result.VisualEffects.Visual });		// just passing a dummy value for mass, the real mass matrix is calculated later
        //        t.Result.PhysicsBody.MaterialGroupID = materialID;
        //        t.Result.PhysicsBody.LinearDamping = .01f;
        //        t.Result.PhysicsBody.AngularDamping = new Vector3D(.01f, .01f, .01f);
        //        t.Result.PhysicsBody.CenterOfMass = t.Result.CenterMass;
        //        t.Result.PhysicsBody.MassMatrix = t.Result.MassMatrix;

        //        hull.Dispose();
        //        foreach (CollisionHull partHull in t.Result.Hulls)
        //        {
        //            partHull.Dispose();
        //        }

        //        #endregion

        //        // Calculate radius
        //        Point3D aabbMin, aabbMax;
        //        t.Result.PhysicsBody.GetAABB(out aabbMin, out aabbMax);
        //        t.Result.Radius = (aabbMax - aabbMin).Length / 2d;

        //        return t.Result;
        //    }, currentContext);

        //    // Exit function
        //    return await retVal;
        //}

        //private static async Task<Tuple<ShipDNA, CollisionHull[]>> BuildParts(PartContainer container, ShipDNA shipDNA, bool runNeural, bool repairPartPositions, World world, EditorOptions options, ItemOptions itemOptions, RadiationField radiation, IGravityField gravity, CameraPool cameraPool, TaskScheduler mainContext)
        //{
        //    // Throw out parts that are too small
        //    PartDNA[] usableParts = shipDNA.PartsByLayer.Values.SelectMany(o => o).Where(o => o.Scale.Length > .01d).ToArray();

        //    // Create the parts based on dna
        //    var combined = BuildPartsSprtCreate(container, usableParts, options, itemOptions, radiation, gravity, cameraPool);

        //    #region Commit the lists

        //    // Store some different views of the parts

        //    // This is an array of all the parts
        //    container.AllParts = combined.Select(o => o.Item1).ToArray();

        //    // These are parts that need to have Update called each tick
        //    container.UpdatableParts = container.AllParts.Where(o => o is IPartUpdatable).Select(o => (IPartUpdatable)o).ToArray();
        //    container.UpdatableIndices = new int[container.UpdatableParts.Length];

        //    #endregion

        //    #region Neural Links

        //    //NOTE: Doing this before moving the parts around in order to get more accurate linkage (the links store the position of the from container)
        //    if (runNeural)
        //    {
        //        container.Links = LinkNeurons(combined.ToArray(), itemOptions);
        //    }

        //    #endregion

        //    // Move Parts (this must be done on the main thread)
        //    var hulls_dna = await BuildPartsSprtMove(container.AllParts, usableParts, repairPartPositions, world, mainContext);
        //    usableParts = hulls_dna.Item1;
        //    CollisionHull[] hulls = hulls_dna.Item2;

        //    //TODO: Preserve layers - actually, get rid of layers from dna.  It's just clutter
        //    ShipDNA retVal = ShipDNA.Create(shipDNA, usableParts);
        //    return Tuple.Create(retVal, hulls);
        //}
        //private static List<Tuple<PartBase, PartDNA>> BuildPartsSprtCreate(PartContainer container, PartDNA[] parts, EditorOptions options, ItemOptions itemOptions, RadiationField radiation, IGravityField gravity, CameraPool cameraPool)
        //{
        //    List<Tuple<PartBase, PartDNA>> retVal = new List<Tuple<PartBase, PartDNA>>();

        //    #region Containers

        //    // Containers need to be built up front
        //    foreach (PartDNA dna in parts.Where(o => o.PartType == AmmoBox.PARTTYPE || o.PartType == FuelTank.PARTTYPE || o.PartType == EnergyTank.PARTTYPE || o.PartType == CargoBay.PARTTYPE))
        //    {
        //        switch (dna.PartType)
        //        {
        //            case AmmoBox.PARTTYPE:
        //                BuildPartsSprtAdd(new AmmoBox(options, itemOptions, dna),
        //                    dna, container.Ammo, retVal);
        //                break;

        //            case FuelTank.PARTTYPE:
        //                BuildPartsSprtAdd(new FuelTank(options, itemOptions, dna),
        //                    dna, container.Fuel, retVal);
        //                break;

        //            case EnergyTank.PARTTYPE:
        //                BuildPartsSprtAdd(new EnergyTank(options, itemOptions, dna),
        //                    dna, container.Energy, retVal);
        //                break;

        //            case PlasmaTank.PARTTYPE:
        //                BuildPartsSprtAdd(new PlasmaTank(options, itemOptions, dna),
        //                    dna, container.Plasma, retVal);
        //                break;

        //            case CargoBay.PARTTYPE:
        //                BuildPartsSprtAdd(new CargoBay(options, itemOptions, dna),
        //                    dna, container.CargoBay, retVal);
        //                break;

        //            default:
        //                throw new ApplicationException("Unknown dna.PartType: " + dna.PartType);
        //        }
        //    }

        //    //NOTE: The parts can handle being handed a null container.  It doesn't add much value to have parts that are dead
        //    //weight, but I don't want to penalize a design for having thrusters, but no fuel tank.  Maybe descendants will develop
        //    //fuel tanks and be a winning design

        //    // Build groups
        //    BuildPartsSprtContainerGroup(out container.FuelGroup, container.Fuel, Math3D.GetCenter(container.Fuel.Select(o => o.Position).ToArray()));
        //    BuildPartsSprtContainerGroup(out container.EnergyGroup, container.Energy, Math3D.GetCenter(container.Energy.Select(o => o.Position).ToArray()));
        //    BuildPartsSprtContainerGroup(out container.PlasmaGroup, container.Plasma, Math3D.GetCenter(container.Plasma.Select(o => o.Position).ToArray()));
        //    BuildPartsSprtContainerGroup(out container.AmmoGroup, container.Ammo, Math3D.GetCenter(container.Ammo.Select(o => o.Position).ToArray()));
        //    BuildPartsSprtContainerGroup(out container.CargoBayGroup, container.CargoBay);

        //    //TODO: Figure out which ammo boxes to put with guns.  These are some of the rules that should be considered, they are potentially competing
        //    //rules, so each rule should form its own links with a weight for each link.  Then choose the pairings with the highest weight (maybe use a bit of
        //    //randomness)
        //    //		- Group guns that are the same size
        //    //		- Pair up boxes and guns that are close together
        //    //		- Smaller guns should hook to smaller boxes

        //    //var gunsBySize = usableParts.Where(o => o.PartType == ProjectileGun.PARTTYPE).GroupBy(o => o.Scale.LengthSquared);

        //    #endregion
        //    #region Standard Parts

        //    foreach (PartDNA dna in parts)
        //    {
        //        switch (dna.PartType)
        //        {
        //            case AmmoBox.PARTTYPE:
        //            case FuelTank.PARTTYPE:
        //            case EnergyTank.PARTTYPE:
        //            case PlasmaTank.PARTTYPE:
        //            case CargoBay.PARTTYPE:
        //                // These were built previously
        //                break;

        //            case ConverterMatterToFuel.PARTTYPE:
        //                BuildPartsSprtAdd(new ConverterMatterToFuel(options, itemOptions, dna, container.FuelGroup),
        //                    dna, container.ConvertMatterToFuel, retVal);
        //                break;

        //            case ConverterMatterToEnergy.PARTTYPE:
        //                BuildPartsSprtAdd(new ConverterMatterToEnergy(options, itemOptions, dna, container.EnergyGroup),
        //                    dna, container.ConvertMatterToEnergy, retVal);
        //                break;

        //            case ConverterEnergyToAmmo.PARTTYPE:
        //                BuildPartsSprtAdd(new ConverterEnergyToAmmo(options, itemOptions, dna, container.EnergyGroup, container.AmmoGroup),
        //                    dna, container.ConvertEnergyToAmmo, retVal);
        //                break;

        //            case ConverterEnergyToFuel.PARTTYPE:
        //                BuildPartsSprtAdd(new ConverterEnergyToFuel(options, itemOptions, dna, container.EnergyGroup, container.FuelGroup),
        //                    dna, container.ConvertEnergyToFuel, retVal);
        //                break;

        //            case ConverterFuelToEnergy.PARTTYPE:
        //                BuildPartsSprtAdd(new ConverterFuelToEnergy(options, itemOptions, dna, container.FuelGroup, container.EnergyGroup),
        //                    dna, container.ConverterFuelToEnergy, retVal);
        //                break;

        //            case ConverterRadiationToEnergy.PARTTYPE:
        //                BuildPartsSprtAdd(new ConverterRadiationToEnergy(options, itemOptions, (ConverterRadiationToEnergyDNA)dna, container.EnergyGroup, radiation),
        //                    dna, container.ConvertRadiationToEnergy, retVal);
        //                break;

        //            case Thruster.PARTTYPE:
        //                BuildPartsSprtAdd(new Thruster(options, itemOptions, (ThrusterDNA)dna, container.FuelGroup),
        //                    dna, container.Thrust, retVal);
        //                break;

        //            case Brain.PARTTYPE:
        //                BuildPartsSprtAdd(new Brain(options, itemOptions, (PartNeuralDNA)dna, container.EnergyGroup),
        //                    dna, container.Brain, retVal);
        //                break;

        //            case SensorGravity.PARTTYPE:
        //                BuildPartsSprtAdd(new SensorGravity(options, itemOptions, (PartNeuralDNA)dna, container.EnergyGroup, gravity),
        //                    dna, container.SensorGravity, retVal);
        //                break;

        //            case SensorSpin.PARTTYPE:
        //                BuildPartsSprtAdd(new SensorSpin(options, itemOptions, (PartNeuralDNA)dna, container.EnergyGroup),
        //                    dna, container.SensorSpin, retVal);
        //                break;

        //            case SensorVelocity.PARTTYPE:
        //                BuildPartsSprtAdd(new SensorVelocity(options, itemOptions, (PartNeuralDNA)dna, container.EnergyGroup),
        //                    dna, container.SensorVelocity, retVal);
        //                break;

        //            case CameraColorRGB.PARTTYPE:
        //                BuildPartsSprtAdd(new CameraColorRGB(options, itemOptions, (PartNeuralDNA)dna, container.EnergyGroup, cameraPool),
        //                    dna, container.CameraColorRGB, retVal);
        //                break;

        //            default:
        //                throw new ApplicationException("Unknown dna.PartType: " + dna.PartType);
        //        }
        //    }

        //    #endregion

        //    #region Post Linkage

        //    // Link cargo bays with converters
        //    if (container.CargoBay.Count > 0)
        //    {
        //        IConverterMatter[] converters = UtilityHelper.Iterate<IConverterMatter>(container.ConvertMatterToEnergy, container.ConvertMatterToFuel).ToArray();

        //        if (converters.Length > 0)
        //        {
        //            container.ConvertMatterGroup = new ConverterMatterGroup(converters, container.CargoBayGroup);
        //        }
        //    }

        //    #endregion

        //    return retVal;
        //}
        //private static async Task<Tuple<PartDNA[], CollisionHull[]>> BuildPartsSprtMove(PartBase[] allParts, PartDNA[] usableParts, bool repairPartPositions, World world, TaskScheduler context)
        //{
        //    var retVal = Task.Factory.StartNew(t =>
        //    {
        //        CollisionHull[] hulls = null;

        //        if (repairPartPositions)
        //        {
        //            bool changed1, changed2;

        //            //TODO: Make a better version.  The PartSeparator should just expose one method, and do both internally
        //            PartSeparator.PullInCrude(out changed1, allParts);

        //            // Separate intersecting parts
        //            hulls = PartSeparator.Separate(out changed2, allParts, world);

        //            if (changed1 || changed2)
        //            {
        //                usableParts = allParts.Select(o => o.GetNewDNA()).ToArray();
        //            }
        //        }
        //        else
        //        {
        //            hulls = allParts.Select(o => o.CreateCollisionHull(world)).ToArray();
        //        }

        //        return Tuple.Create(usableParts, hulls);
        //    }, context);

        //    return await retVal;
        //}

        #endregion

        #region Original Constructor

        //public Ship(EditorOptions options, ItemOptions itemOptions, ShipDNA dna, World world, int materialID, RadiationField radiation, IGravityField gravity, CameraPool cameraPool, bool runNeural, bool repairPartPositions)
        //{
        //    _options = options;
        //    _itemOptions = itemOptions;
        //    _radiation = radiation;
        //    _gravity = gravity;
        //    _cameraPool = cameraPool;

        //    #region Parts

        //    _parts = new PartContainer();

        //    CollisionHull[] hulls;
        //    _dna = BuildParts(out hulls, _parts, dna, runNeural, repairPartPositions, world, _options, _itemOptions, _radiation, _gravity, _cameraPool);

        //    // Hook up events
        //    foreach (var part in _parts.AllParts)
        //    {
        //        part.RequestWorldLocation += new EventHandler<PartRequestWorldLocationArgs>(Part_RequestWorldLocation);
        //        part.RequestWorldSpeed += new EventHandler<PartRequestWorldSpeedArgs>(Part_RequestWorldSpeed);
        //    }

        //    // See if there are parts that can gradually change the ship's mass
        //    if ((_parts.Fuel.Count > 0 && _parts.Thrust.Count > 0) ||
        //        (_parts.CargoBay.Count > 0 && (_parts.ConvertMatterToEnergy.Count > 0 || _parts.ConvertMatterToFuel.Count > 0)) ||
        //        (_parts.Energy.Count > 0 && _parts.Fuel.Count > 0 && (_parts.ConvertEnergyToFuel.Count > 0 || _parts.ConverterFuelToEnergy.Count > 0))
        //        )
        //    {
        //        _hasMassChangingUpdatables = true;
        //    }

        //    #endregion

        //    #region WPF - main

        //    //TODO: Remember this so that flames and other visuals can be added/removed.  That way there will still only be one model visual
        //    //TODO: When joints are supported, some of the parts (or part groups) will move relative to the others.  There can still be a single model visual
        //    Model3DGroup models = new Model3DGroup();

        //    foreach (PartBase part in _parts.AllParts)
        //    {
        //        models.Children.Add(part.Model);
        //    }

        //    this.Model = models;

        //    ModelVisual3D model = new ModelVisual3D();
        //    model.Content = models;

        //    #endregion
        //    #region WPF - effects

        //    // These visuals will only show up in the main viewport (they won't be shared with the camera pool, so won't be visible to
        //    // other ships)

        //    _visualEffects = new VisualEffects(_itemOptions, _parts.Thrust);

        //    #endregion

        //    #region Physics Body

        //    MassMatrix massMatrix;
        //    Point3D centerMass;
        //    GetInertiaTensorAndCenterOfMass_Points(out massMatrix, out centerMass, _parts.AllParts.ToArray(), _dna.PartsByLayer.Values.SelectMany(o => o).ToArray(), _itemOptions.MomentOfInertiaMultiplier);

        //    // For now, just make a composite collision hull out of all the parts
        //    CollisionHull hull = CollisionHull.CreateCompoundCollision(world, 0, hulls);

        //    this.PhysicsBody = new Body(hull, Matrix3D.Identity, 1d, new Visual3D[] { model, _visualEffects.Visual });		// just passing a dummy value for mass, the real mass matrix is calculated later
        //    this.PhysicsBody.MaterialGroupID = materialID;
        //    this.PhysicsBody.LinearDamping = .01f;
        //    this.PhysicsBody.AngularDamping = new Vector3D(.01f, .01f, .01f);
        //    this.PhysicsBody.CenterOfMass = centerMass;
        //    this.PhysicsBody.MassMatrix = massMatrix;

        //    this.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(PhysicsBody_ApplyForceAndTorque);

        //    hull.Dispose();
        //    foreach (CollisionHull partHull in hulls)
        //    {
        //        partHull.Dispose();
        //    }

        //    #endregion

        //    // Calculate radius
        //    Point3D aabbMin, aabbMax;
        //    this.PhysicsBody.GetAABB(out aabbMin, out aabbMax);
        //    this.Radius = (aabbMax - aabbMin).Length / 2d;

        //    // Set up a neural processor on its own thread/task
        //    if (_parts.Links != null)
        //    {
        //        _parts.LinkBucket = new NeuralBucket(_parts.Links.SelectMany(o => UtilityHelper.Iterate(o.InternalLinks, o.ExternalLinks)).ToArray());
        //        NeuralPool.Instance.Add(_parts.LinkBucket);
        //    }

        //    this.ShouldRecalcMass_Large = false;
        //    this.ShouldRecalcMass_Small = false;
        //}

        #endregion

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
                    if (_parts.LinkBucket != null)
                    {
                        //_parts.LinkBucket = null;

                        //NOTE: Since this is async, must make sure the remove is called only after the add finishes
                        _neuralPoolAddTask.ContinueWith(t =>
                        {
                            if (t.Result != null)
                            {
                                //NeuralPool.Instance.Remove(_parts.LinkBucket);        // by the time this runs, _parts will already be null.  So the bucket is kept part of the task args
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

        public virtual void Update(double elapsedTime)
        {
            #region Fill the converters

            if (_parts.ConvertMatterGroup != null && this.Age >= _nextMatterTransferTime)
            {
                if (_parts.ConvertMatterGroup.Transfer())
                {
                    this.ShouldRecalcMass_Large = true;
                }
                _nextMatterTransferTime = this.Age + (elapsedTime * StaticRandom.Next(80, 120));      // no need to spin the processor unnecessarily each tick
                //_nextMatterTransferTime = 0;
            }

            #endregion

            #region Update Parts

            UpdateParts_RandomOrder(_parts.UpdatableParts, _parts.UpdatableIndices, elapsedTime);

            if (_hasMassChangingUpdatables)
            {
                // There's a good chance that at least one of the parts changed the ship's mass a little bit
                this.ShouldRecalcMass_Small = true;
            }

            #endregion

            #region Recalc mass

            if (this.ShouldRecalcMass_Large)
            {
                RecalculateMass();
            }
            else if (this.ShouldRecalcMass_Small && this.Age > _lastMassRecalculateTime + (elapsedTime * 1500d))
            {
                RecalculateMass();
            }

            #endregion

            _visualEffects.Refresh();

            // Bump the age
            this.Age += elapsedTime;
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
        public List<Thruster> Thrusters
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

        /// <summary>
        /// This is the age of the ship (sum of elapsed time).  This doesn't use datetime.now, it's all based on the elapsed time passed into update
        /// </summary>
        public double Age
        {
            get;
            private set;
        }

        /// <summary>
        /// This is used as a hint to recalcuate mass - set this when a large change occurs
        /// </summary>
        /// <remarks>
        /// Whenever cargo is added/removed/shifted around, this should be set to true.  Or any other large sudden changes
        /// in mass
        /// </remarks>
        protected bool ShouldRecalcMass_Large
        {
            get;
            set;
        }
        /// <summary>
        /// This is used as a hint to recalcuate mass - set this when a small change occurs
        /// </summary>
        /// <remarks>
        /// This should be set when there are small changes in mass, like normal using up of fuel, or matter converters slowly consuming
        /// cargo
        /// </remarks>
        protected bool ShouldRecalcMass_Small
        {
            get;
            set;
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
            MassMatrix massMatrix;
            Point3D centerMass;
            GetInertiaTensorAndCenterOfMass_Points(out massMatrix, out centerMass, _parts.AllParts.ToArray(), _dna.PartsByLayer.Values.SelectMany(o => o).ToArray(), _itemOptions.MomentOfInertiaMultiplier);

            this.PhysicsBody.CenterOfMass = centerMass;
            this.PhysicsBody.MassMatrix = massMatrix;

            _lastMassRecalculateTime = this.Age;
            this.ShouldRecalcMass_Large = false;
            this.ShouldRecalcMass_Small = false;
        }

        public virtual ShipDNA GetNewDNA()
        {
            // Create dna for each part using that part's current stats
            List<PartDNA> dnaParts = new List<PartDNA>();

            foreach (PartBase part in _parts.AllParts)
            {
                PartDNA dna = part.GetNewDNA();

                if (_parts.Links != null && dna is PartNeuralDNA && part is INeuronContainer)
                {
                    NeuralUtility.PopulateDNALinks((PartNeuralDNA)dna, (INeuronContainer)part, _parts.Links);
                }

                dnaParts.Add(dna);
            }

            // Build the ship dna
            ShipDNA retVal = ShipDNA.Create(_dna, dnaParts);

            // Exit Function
            return retVal;
        }

        public async static Task<BuildPartsResults> BuildParts_Finish(List<Tuple<PartBase, PartDNA>> combined, PartDNA[] usableParts, bool runNeural, bool repairPartPositions, ItemOptions itemOptions, World world)
        {
            BuildPartsResults retVal = new BuildPartsResults();

            #region Commit the lists

            // Store some different views of the parts

            // This is an array of all the parts
            retVal.AllParts = combined.Select(o => o.Item1).ToArray();

            // These are parts that need to have Update called each tick
            retVal.UpdatableParts = retVal.AllParts.Where(o => o is IPartUpdatable).Select(o => (IPartUpdatable)o).ToArray();
            retVal.UpdatableIndices = new int[retVal.UpdatableParts.Length];

            #endregion

            #region Neural Links

            //NOTE: Doing this before moving the parts around in order to get more accurate linkage (the links store the position of the from container)
            if (runNeural)
            {
                retVal.Links = await Task.Run(() => LinkNeurons(combined.ToArray(), itemOptions));
            }

            #endregion

            // Move Parts (this must be done on the main thread)
            var hulls_dna = BuildPartsSprtMove(retVal.AllParts, usableParts, repairPartPositions, world);
            retVal.DNA = hulls_dna.Item1;
            retVal.Hulls = hulls_dna.Item2;

            return retVal;
        }

        public static void UpdateParts_RandomOrder(IPartUpdatable[] updatableParts, int[] updatableIndices, double elapsedTime)
        {
            // Reset the indices
            for (int cntr = 0; cntr < updatableParts.Length; cntr++)
            {
                updatableIndices[cntr] = cntr;
            }

            Random rand = StaticRandom.GetRandomForThread();

            for (int cntr = updatableParts.Length - 1; cntr >= 0; cntr--)
            {
                // Come up with a random part
                int index1 = rand.Next(cntr + 1);
                int index2 = updatableIndices[index1];
                updatableIndices[index1] = updatableIndices[cntr];

                // Update it
                updatableParts[index2].Update(elapsedTime);
            }
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

        #region OLD
        //private ShipDNA BuildParts(out CollisionHull[] hulls, ShipDNA shipDNA, bool runNeural, bool repairPartPositions, World world)
        //{
        //    // Throw out parts that are too small
        //    PartDNA[] usableParts = shipDNA.PartsByLayer.Values.SelectMany(o => o).Where(o => o.Scale.Length > .01d).ToArray();

        //    // Create the parts based on dna
        //    var combined = BuildPartsSprtCreate(usableParts);

        //    #region Hook up events

        //    foreach (var part in combined)
        //    {
        //        part.Item1.RequestWorldLocation += new EventHandler<PartRequestWorldLocationArgs>(Part_RequestWorldLocation);
        //        part.Item1.RequestWorldSpeed += new EventHandler<PartRequestWorldSpeedArgs>(Part_RequestWorldSpeed);
        //    }

        //    #endregion

        //    #region Commit the lists

        //    // Store some different views of the parts

        //    // This is an array of all the parts
        //    _allParts = combined.Select(o => o.Item1).ToArray();

        //    // These are parts that need to have Update called each tick
        //    _updatableParts = _allParts.Where(o => o is IPartUpdatable).Select(o => (IPartUpdatable)o).ToArray();
        //    _updatableIndices = new int[_updatableParts.Length];

        //    #endregion

        //    #region Neural Links

        //    //NOTE: Doing this before moving the parts around in order to get more accurate linkage (the links store the position of the from container)
        //    if (runNeural)
        //    {
        //        _links = LinkNeurons(combined.ToArray(), /*_groupNeurons,*/ _itemOptions);
        //    }

        //    #endregion

        //    #region Move Parts

        //    if (repairPartPositions)
        //    {
        //        bool changed1, changed2;

        //        //TODO: Make a better version.  The PartSeparator should just expose one method, and do both internally
        //        PartSeparator.PullInCrude(out changed1, _allParts);

        //        // Separate intersecting parts
        //        hulls = PartSeparator.Separate(out changed2, _allParts, world);

        //        if (changed1 || changed2)
        //        {
        //            usableParts = _allParts.Select(o => o.GetNewDNA()).ToArray();
        //        }
        //    }
        //    else
        //    {
        //        hulls = _allParts.Select(o => o.CreateCollisionHull(world)).ToArray();
        //    }

        //    #endregion

        //    // See if there are parts that can gradually change the ship's mass
        //    if ((_fuel.Count > 0 && _thrust.Count > 0) ||
        //        (_cargoBay.Count > 0 && (_convertMatterToEnergy.Count > 0 || _convertMatterToFuel.Count > 0)) ||
        //        (_energy.Count > 0 && _fuel.Count > 0 && (_convertEnergyToFuel.Count > 0 || _converterFuelToEnergy.Count > 0))
        //        )
        //    {
        //        _hasMassChangingUpdatables = true;
        //    }

        //    //TODO: Preserve layers
        //    ShipDNA retVal = ShipDNA.Create(shipDNA, usableParts);
        //    return retVal;
        //}
        //private List<Tuple<PartBase, PartDNA>> BuildPartsSprtCreate(PartDNA[] parts)
        //{
        //    List<Tuple<PartBase, PartDNA>> retVal = new List<Tuple<PartBase, PartDNA>>();

        //    #region Containers

        //    // Containers need to be built up front
        //    foreach (PartDNA dna in parts.Where(o => o.PartType == AmmoBox.PARTTYPE || o.PartType == FuelTank.PARTTYPE || o.PartType == EnergyTank.PARTTYPE || o.PartType == CargoBay.PARTTYPE))
        //    {
        //        switch (dna.PartType)
        //        {
        //            case AmmoBox.PARTTYPE:
        //                BuildPartsSprtAdd(new AmmoBox(_options, _itemOptions, dna),
        //                    dna, _ammo, retVal);
        //                break;

        //            case FuelTank.PARTTYPE:
        //                BuildPartsSprtAdd(new FuelTank(_options, _itemOptions, dna),
        //                    dna, _fuel, retVal);
        //                break;

        //            case EnergyTank.PARTTYPE:
        //                BuildPartsSprtAdd(new EnergyTank(_options, _itemOptions, dna),
        //                    dna, _energy, retVal);
        //                break;

        //            case PlasmaTank.PARTTYPE:
        //                BuildPartsSprtAdd(new PlasmaTank(_options, _itemOptions, dna),
        //                    dna, _plasma, retVal);
        //                break;

        //            case CargoBay.PARTTYPE:
        //                BuildPartsSprtAdd(new CargoBay(_options, _itemOptions, dna),
        //                    dna, _cargoBay, retVal);
        //                break;

        //            default:
        //                throw new ApplicationException("Unknown dna.PartType: " + dna.PartType);
        //        }
        //    }

        //    //NOTE: The parts can handle being handed a null container.  It doesn't add much value to have parts that are dead
        //    //weight, but I don't want to penalize a design for having thrusters, but no fuel tank.  Maybe descendants will develop
        //    //fuel tanks and be a winning design

        //    // Build groups
        //    BuildPartsSprtContainerGroup(out _fuelGroup, _fuel, Math3D.GetCenter(_fuel.Select(o => o.Position).ToArray()));
        //    BuildPartsSprtContainerGroup(out _energyGroup, _energy, Math3D.GetCenter(_energy.Select(o => o.Position).ToArray()));
        //    BuildPartsSprtContainerGroup(out _plasmaGroup, _plasma, Math3D.GetCenter(_plasma.Select(o => o.Position).ToArray()));
        //    BuildPartsSprtContainerGroup(out _ammoGroup, _ammo, Math3D.GetCenter(_ammo.Select(o => o.Position).ToArray()));
        //    BuildPartsSprtContainerGroup(out _cargoBayGroup, _cargoBay);

        //    //TODO: Figure out which ammo boxes to put with guns.  These are some of the rules that should be considered, they are potentially competing
        //    //rules, so each rule should form its own links with a weight for each link.  Then choose the pairings with the highest weight (maybe use a bit of
        //    //randomness)
        //    //		- Group guns that are the same size
        //    //		- Pair up boxes and guns that are close together
        //    //		- Smaller guns should hook to smaller boxes

        //    //var gunsBySize = usableParts.Where(o => o.PartType == ProjectileGun.PARTTYPE).GroupBy(o => o.Scale.LengthSquared);

        //    #endregion
        //    #region Standard Parts

        //    foreach (PartDNA dna in parts)
        //    {
        //        switch (dna.PartType)
        //        {
        //            case AmmoBox.PARTTYPE:
        //            case FuelTank.PARTTYPE:
        //            case EnergyTank.PARTTYPE:
        //            case PlasmaTank.PARTTYPE:
        //            case CargoBay.PARTTYPE:
        //                // These were built previously
        //                break;

        //            case ConverterMatterToFuel.PARTTYPE:
        //                BuildPartsSprtAdd(new ConverterMatterToFuel(_options, _itemOptions, dna, _fuelGroup),
        //                    dna, _convertMatterToFuel, retVal);
        //                break;

        //            case ConverterMatterToEnergy.PARTTYPE:
        //                BuildPartsSprtAdd(new ConverterMatterToEnergy(_options, _itemOptions, dna, _energyGroup),
        //                    dna, _convertMatterToEnergy, retVal);
        //                break;

        //            case ConverterEnergyToAmmo.PARTTYPE:
        //                BuildPartsSprtAdd(new ConverterEnergyToAmmo(_options, _itemOptions, dna, _energyGroup, _ammoGroup),
        //                    dna, _convertEnergyToAmmo, retVal);
        //                break;

        //            case ConverterEnergyToFuel.PARTTYPE:
        //                BuildPartsSprtAdd(new ConverterEnergyToFuel(_options, _itemOptions, dna, _energyGroup, _fuelGroup),
        //                    dna, _convertEnergyToFuel, retVal);
        //                break;

        //            case ConverterFuelToEnergy.PARTTYPE:
        //                BuildPartsSprtAdd(new ConverterFuelToEnergy(_options, _itemOptions, dna, _fuelGroup, _energyGroup),
        //                    dna, _converterFuelToEnergy, retVal);
        //                break;

        //            case ConverterRadiationToEnergy.PARTTYPE:
        //                BuildPartsSprtAdd(new ConverterRadiationToEnergy(_options, _itemOptions, (ConverterRadiationToEnergyDNA)dna, _energyGroup, _radiation),
        //                    dna, _convertRadiationToEnergy, retVal);
        //                break;

        //            case Thruster.PARTTYPE:
        //                BuildPartsSprtAdd(new Thruster(_options, _itemOptions, (ThrusterDNA)dna, _fuelGroup),
        //                    dna, _thrust, retVal);
        //                break;

        //            case Brain.PARTTYPE:
        //                BuildPartsSprtAdd(new Brain(_options, _itemOptions, (PartNeuralDNA)dna, _energyGroup),
        //                    dna, _brain, retVal);
        //                break;

        //            case SensorGravity.PARTTYPE:
        //                BuildPartsSprtAdd(new SensorGravity(_options, _itemOptions, (PartNeuralDNA)dna, _energyGroup, _gravity),
        //                    dna, _sensorGravity, retVal);
        //                break;

        //            case SensorSpin.PARTTYPE:
        //                BuildPartsSprtAdd(new SensorSpin(_options, _itemOptions, (PartNeuralDNA)dna, _energyGroup),
        //                    dna, _sensorSpin, retVal);
        //                break;

        //            case SensorVelocity.PARTTYPE:
        //                BuildPartsSprtAdd(new SensorVelocity(_options, _itemOptions, (PartNeuralDNA)dna, _energyGroup),
        //                    dna, _sensorVelocity, retVal);
        //                break;

        //            case CameraColorRGB.PARTTYPE:
        //                BuildPartsSprtAdd(new CameraColorRGB(_options, _itemOptions, (PartNeuralDNA)dna, _energyGroup, _cameraPool),
        //                    dna, _cameraColorRGB, retVal);
        //                break;

        //            default:
        //                throw new ApplicationException("Unknown dna.PartType: " + dna.PartType);
        //        }
        //    }

        //    #endregion

        //    #region Post Linkage

        //    // Link cargo bays with converters
        //    if (_cargoBay.Count > 0)
        //    {
        //        IConverterMatter[] converters = UtilityHelper.Iterate<IConverterMatter>(_convertMatterToEnergy, _convertMatterToFuel).ToArray();

        //        if (converters.Length > 0)
        //        {
        //            _convertMatterGroup = new ConverterMatterGroup(converters, _cargoBayGroup);
        //        }
        //    }

        //    #endregion

        //    return retVal;
        //}
        #endregion

        private async static Task<Tuple<ShipDNA, CollisionHull[]>> BuildParts(PartContainer container, ShipDNA shipDNA, bool runNeural, bool repairPartPositions, World world, EditorOptions options, ItemOptions itemOptions, RadiationField radiation, IGravityField gravity, CameraPool cameraPool)
        {
            // Throw out parts that are too small
            PartDNA[] usableParts = shipDNA.PartsByLayer.Values.SelectMany(o => o).Where(o => o.Scale.Length > .01d).ToArray();

            // Create the parts based on dna
            var combined = BuildPartsSprtCreate(container, usableParts, options, itemOptions, radiation, gravity, cameraPool);

            //TODO: Instead of all those out params, build a result object
            BuildPartsResults results = await BuildParts_Finish(combined, usableParts, runNeural, repairPartPositions, itemOptions, world);

            container.AllParts = results.AllParts;
            container.UpdatableParts = results.UpdatableParts;
            container.UpdatableIndices = results.UpdatableIndices;
            container.Links = results.Links;

            //TODO: Preserve layers - actually, get rid of layers from dna.  It's just clutter
            ShipDNA retVal = ShipDNA.Create(shipDNA, results.DNA);
            return Tuple.Create(retVal, results.Hulls);
        }
        private static List<Tuple<PartBase, PartDNA>> BuildPartsSprtCreate(PartContainer container, PartDNA[] parts, EditorOptions options, ItemOptions itemOptions, RadiationField radiation, IGravityField gravity, CameraPool cameraPool)
        {
            List<Tuple<PartBase, PartDNA>> retVal = new List<Tuple<PartBase, PartDNA>>();

            #region Containers

            // Containers need to be built up front
            foreach (PartDNA dna in parts.Where(o => o.PartType == AmmoBox.PARTTYPE || o.PartType == FuelTank.PARTTYPE || o.PartType == EnergyTank.PARTTYPE || o.PartType == CargoBay.PARTTYPE))
            {
                switch (dna.PartType)
                {
                    case AmmoBox.PARTTYPE:
                        BuildPartsSprtAdd(new AmmoBox(options, itemOptions, dna),
                            dna, container.Ammo, retVal);
                        break;

                    case FuelTank.PARTTYPE:
                        BuildPartsSprtAdd(new FuelTank(options, itemOptions, dna),
                            dna, container.Fuel, retVal);
                        break;

                    case EnergyTank.PARTTYPE:
                        BuildPartsSprtAdd(new EnergyTank(options, itemOptions, dna),
                            dna, container.Energy, retVal);
                        break;

                    case PlasmaTank.PARTTYPE:
                        BuildPartsSprtAdd(new PlasmaTank(options, itemOptions, dna),
                            dna, container.Plasma, retVal);
                        break;

                    case CargoBay.PARTTYPE:
                        BuildPartsSprtAdd(new CargoBay(options, itemOptions, dna),
                            dna, container.CargoBay, retVal);
                        break;

                    default:
                        throw new ApplicationException("Unknown dna.PartType: " + dna.PartType);
                }
            }

            //NOTE: The parts can handle being handed a null container.  It doesn't add much value to have parts that are dead
            //weight, but I don't want to penalize a design for having thrusters, but no fuel tank.  Maybe descendants will develop
            //fuel tanks and be a winning design

            // Build groups
            BuildPartsSprtContainerGroup(out container.FuelGroup, container.Fuel, Math3D.GetCenter(container.Fuel.Select(o => o.Position).ToArray()));
            BuildPartsSprtContainerGroup(out container.EnergyGroup, container.Energy, Math3D.GetCenter(container.Energy.Select(o => o.Position).ToArray()));
            BuildPartsSprtContainerGroup(out container.PlasmaGroup, container.Plasma, Math3D.GetCenter(container.Plasma.Select(o => o.Position).ToArray()));
            BuildPartsSprtContainerGroup(out container.AmmoGroup, container.Ammo, Math3D.GetCenter(container.Ammo.Select(o => o.Position).ToArray()));
            BuildPartsSprtContainerGroup(out container.CargoBayGroup, container.CargoBay);

            //TODO: Figure out which ammo boxes to put with guns.  These are some of the rules that should be considered, they are potentially competing
            //rules, so each rule should form its own links with a weight for each link.  Then choose the pairings with the highest weight (maybe use a bit of
            //randomness)
            //		- Group guns that are the same size
            //		- Pair up boxes and guns that are close together
            //		- Smaller guns should hook to smaller boxes

            //var gunsBySize = usableParts.Where(o => o.PartType == ProjectileGun.PARTTYPE).GroupBy(o => o.Scale.LengthSquared);

            #endregion
            #region Standard Parts

            foreach (PartDNA dna in parts)
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
                        BuildPartsSprtAdd(new ConverterMatterToFuel(options, itemOptions, dna, container.FuelGroup),
                            dna, container.ConvertMatterToFuel, retVal);
                        break;

                    case ConverterMatterToEnergy.PARTTYPE:
                        BuildPartsSprtAdd(new ConverterMatterToEnergy(options, itemOptions, dna, container.EnergyGroup),
                            dna, container.ConvertMatterToEnergy, retVal);
                        break;

                    case ConverterEnergyToAmmo.PARTTYPE:
                        BuildPartsSprtAdd(new ConverterEnergyToAmmo(options, itemOptions, dna, container.EnergyGroup, container.AmmoGroup),
                            dna, container.ConvertEnergyToAmmo, retVal);
                        break;

                    case ConverterEnergyToFuel.PARTTYPE:
                        BuildPartsSprtAdd(new ConverterEnergyToFuel(options, itemOptions, dna, container.EnergyGroup, container.FuelGroup),
                            dna, container.ConvertEnergyToFuel, retVal);
                        break;

                    case ConverterFuelToEnergy.PARTTYPE:
                        BuildPartsSprtAdd(new ConverterFuelToEnergy(options, itemOptions, dna, container.FuelGroup, container.EnergyGroup),
                            dna, container.ConverterFuelToEnergy, retVal);
                        break;

                    case ConverterRadiationToEnergy.PARTTYPE:
                        BuildPartsSprtAdd(new ConverterRadiationToEnergy(options, itemOptions, (ConverterRadiationToEnergyDNA)dna, container.EnergyGroup, radiation),
                            dna, container.ConvertRadiationToEnergy, retVal);
                        break;

                    case Thruster.PARTTYPE:
                        BuildPartsSprtAdd(new Thruster(options, itemOptions, (ThrusterDNA)dna, container.FuelGroup),
                            dna, container.Thrust, retVal);
                        break;

                    case Brain.PARTTYPE:
                        BuildPartsSprtAdd(new Brain(options, itemOptions, (PartNeuralDNA)dna, container.EnergyGroup),
                            dna, container.Brain, retVal);
                        break;

                    case SensorGravity.PARTTYPE:
                        BuildPartsSprtAdd(new SensorGravity(options, itemOptions, (PartNeuralDNA)dna, container.EnergyGroup, gravity),
                            dna, container.SensorGravity, retVal);
                        break;

                    case SensorSpin.PARTTYPE:
                        BuildPartsSprtAdd(new SensorSpin(options, itemOptions, (PartNeuralDNA)dna, container.EnergyGroup),
                            dna, container.SensorSpin, retVal);
                        break;

                    case SensorVelocity.PARTTYPE:
                        BuildPartsSprtAdd(new SensorVelocity(options, itemOptions, (PartNeuralDNA)dna, container.EnergyGroup),
                            dna, container.SensorVelocity, retVal);
                        break;

                    case CameraColorRGB.PARTTYPE:
                        BuildPartsSprtAdd(new CameraColorRGB(options, itemOptions, (PartNeuralDNA)dna, container.EnergyGroup, cameraPool),
                            dna, container.CameraColorRGB, retVal);
                        break;

                    default:
                        throw new ApplicationException("Unknown dna.PartType: " + dna.PartType);
                }
            }

            #endregion

            #region Post Linkage

            // Link cargo bays with converters
            if (container.CargoBay.Count > 0)
            {
                IConverterMatter[] converters = UtilityHelper.Iterate<IConverterMatter>(container.ConvertMatterToEnergy, container.ConvertMatterToFuel).ToArray();

                if (converters.Length > 0)
                {
                    container.ConvertMatterGroup = new ConverterMatterGroup(converters, container.CargoBayGroup);
                }
            }

            #endregion

            return retVal;
        }
        private static Tuple<PartDNA[], CollisionHull[]> BuildPartsSprtMove(PartBase[] allParts, PartDNA[] usableParts, bool repairPartPositions, World world)
        {
            CollisionHull[] hulls = null;

            if (repairPartPositions)
            {
                bool changed1, changed2;

                //TODO: Make a better version.  The PartSeparator should just expose one method, and do both internally
                PartSeparator.PullInCrude(out changed1, allParts);

                // Separate intersecting parts
                hulls = PartSeparator.Separate(out changed2, allParts, world);

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

        private static void BuildPartsSprtContainerGroup(out IContainer containerGroup, IEnumerable<IContainer> containers, Point3D center)
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
                group.Ownership = ContainerGroup.ContainerOwnershipType.GroupIsSoleOwner;		// this is the most efficient option
                foreach (IContainer container in containers)
                {
                    group.AddContainer(container);
                }

                containerGroup = group;
            }
        }
        private static void BuildPartsSprtContainerGroup(out CargoBayGroup cargoBayGroup, IEnumerable<CargoBay> cargoBays)
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
        private static void BuildPartsSprtAdd<T>(T item, PartDNA dna, List<T> specificList, List<Tuple<PartBase, PartDNA>> combinedList) where T : PartBase
        {
            // This is just a helper method so one call adds to two lists
            specificList.Add(item);
            combinedList.Add(new Tuple<PartBase, PartDNA>(item, dna));
        }

        private static NeuralUtility.ContainerOutput[] LinkNeurons(Tuple<PartBase, PartDNA>[] parts, ItemOptions itemOptions)
        {
            #region Build up input args

            List<NeuralUtility.ContainerInput> inputs = new List<NeuralUtility.ContainerInput>();

            foreach (var part in parts.Where(o => o.Item1 is INeuronContainer))
            {
                INeuronContainer container = (INeuronContainer)part.Item1;
                PartNeuralDNA dna = part.Item2 as PartNeuralDNA;
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
        private static void GetInertiaTensorAndCenterOfMass_Points(out MassMatrix matrix, out Point3D center, PartBase[] parts, PartDNA[] dna, double inertiaMultiplier)
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
                Point3D centerMass = parts[cntr].Position + massBreakdowns[cntr].CenterMass.ToVector();

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
                double distance = Math3D.GetClosestDistance_Point_Line(body.CenterMass, axis, pointMass.Item1);

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
            double distance = Math3D.GetClosestDistance_Point_Line(shipCenterMass, axis, partCenter);

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
        public SortedList<int, List<PartDNA>> PartsByLayer
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
        public static ShipDNA Create(IEnumerable<PartDNA> parts)
        {
            return Create(Guid.NewGuid().ToString(), parts);
        }
        /// <summary>
        /// This is just a convenience if you don't care about the layers (it always creates 1 layer)
        /// </summary>
        public static ShipDNA Create(string name, IEnumerable<PartDNA> parts)
        {
            ShipDNA retVal = new ShipDNA();

            retVal.ShipName = name;
            retVal.ShipLineage = Guid.NewGuid().ToString();		//this will probably be overwritten, but give it something unique if it's not
            retVal.Generation = 0;

            retVal.LayerNames = new string[] { "layer1" }.ToList();
            retVal.PartsByLayer = new SortedList<int, List<PartDNA>>();
            retVal.PartsByLayer.Add(0, new List<PartDNA>(parts));

            return retVal;
        }
        /// <summary>
        /// NOTE: Only the ship level properties are copied from prev
        /// TODO: Try to preserve layers (comparing prev.parts with parts)
        /// </summary>
        public static ShipDNA Create(ShipDNA prev, IEnumerable<PartDNA> parts)
        {
            ShipDNA retVal = new ShipDNA();

            // Copy these from prev
            retVal.ShipName = prev.ShipName;
            retVal.ShipLineage = prev.ShipLineage;
            retVal.Generation = prev.Generation;

            // Copy these from parts
            retVal.LayerNames = new string[] { "layer1" }.ToList();
            retVal.PartsByLayer = new SortedList<int, List<PartDNA>>();
            retVal.PartsByLayer.Add(0, new List<PartDNA>(parts));

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
