using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

using Game.HelperClasses;
using Game.Newt.HelperClasses;
using Game.Newt.NewtonDynamics;

namespace Game.Newt.AsteroidMiner2
{
    #region Notes

    // These are meant to be written to by a single thread, and consumed by many threads

    // I think the most efficient way to build the worker would be a concept similar to double buffering.  The worker thread is building
    // up a field, and when it's done, it exposes that for consumption.  All the gets from the other threads just get from the latest public
    // field.
    //
    // A downside of this would be calculating values for lots of grid cells that won't be requested.  A way to minimize that would be
    // to make grid cells larger where there are no bodies (and maybe just leave the value zero or some constant in those empty cells).
    //
    // An alternate way of processing would be to do nothing until a request comes in.  Then calculate the values for the cell surrounding
    // that position, and a timestamp.  The downside of this is the calculations would be done in the requesting thread, and I'm pretty
    // sure you would have more locking going on

    // Each time a field grid is built, start off with a random offset.  That way you won't have static noticable cells.  Instead they will jitter
    // and any math errors or unnatural stabilities will be spread around

    // I'm still not sure of the most efficient way to store and access the memory.  Locks would be expensive, I don't know if volatile
    // can be used on this more complex data.

    #endregion

    #region Interface: IGravityField

    public interface IGravityField
    {
        Vector3D GetForce(Point3D point);
    }

    #endregion
    #region Class: GravityFieldSpace

    //TODO: This started as just gravity, but tacked on swirl and boundry.  I don't like the name ForceField, but come up with something more generic than gravity

    /// <remarks>
    /// This one could be tricky:
    /// 
    /// For roughly similar mass objects (like an asteroid field), it should be sufficient to just have gravity represented as a vector field.  Cells
    /// that have high mass would actually have a very low net force, and the cells surrounding that mass would have the higher magnitude
    /// vectors
    /// 
    /// But when you have a very large high mass object that spans several cells, it will attract lots of small objects.  Those small objects won't
    /// stay stuck to the side like you'd expect (because that cell will have a lower net value), or they will stick to the large object, but push
    /// it around unnaturally - maybe  :)
    /// 
    /// I'm not sure, but I think two different approaches need to be done - large bodies need to be handled specially, so that each request
    /// for force from the small bodies will push the large body back at them so that forces balance (otherwise, if you have a bunch of small
    /// bodies on one side of a large body, it will push that large body around like a small thruster)
    /// </remarks>
    public class GravityFieldSpace : IGravityField
    {
        #region Class: GravityCell

        public class GravityCell
        {
            public GravityCell(double mass, Point3D position)
                : this(TokenGenerator.Instance.NextToken(), mass, position, new Vector3D(0, 0, 0)) { }

            public GravityCell(long token, double mass, Point3D position, Vector3D force)
            {
                this.Token = token;
                this.Mass = mass;
                this.Position = position;
                this.Force = force;
            }

            public readonly long Token;
            public readonly double Mass;
            public readonly Point3D Position;
            public readonly Vector3D Force;
        }

        #endregion
        #region Class: BoundryField

        /// <summary>
        /// This will be an inward pointing force near the boundry of the map.  It is a way to slow things down before
        /// hitting the edge of the map, and can be thought of as an elliptical shell
        /// </summary>
        public class BoundryField
        {
            #region Declaration Section

            // These multiply a point by these ratios to get a spherical map
            private readonly double _ratioX;
            private readonly double _ratioY;
            private readonly double _ratioZ;

            // The radius where the boundry starts and stops
            private readonly double _boundryStart;
            private readonly double _boundryStartSquared;
            private readonly double _boundryStop;

            // This is the c in: force = c * x^2
            private readonly double _equationConstant;

            // This is the center point between MapMin and MapMax
            private readonly Point3D _center;

            #endregion

            #region Constructor

            public BoundryField(double startPercent, double strengthHalf, double exponent, Point3D mapMin, Point3D mapMax)
            {
                this.StartPercent = startPercent;
                this.StrengthHalf = strengthHalf;
                this.Exponent = exponent;
                this.MapMin = mapMin;
                this.MapMax = mapMax;

                #region Initialize

                // Center point
                _center = new Point3D((this.MapMin.X + this.MapMax.X) / 2d, (this.MapMin.Y + this.MapMax.Y) / 2d, (this.MapMin.Z + this.MapMax.Z) / 2d);

                Vector3D offset = this.MapMax - _center;
                double maxValue = Math.Max(offset.X, Math.Max(offset.Y, offset.Z));

                // Boundries
                _boundryStop = maxValue;
                _boundryStart = _boundryStop * this.StartPercent;
                _boundryStartSquared = _boundryStart * _boundryStart;

                // force = c * x^2
                // c = force / x^2
                _equationConstant = this.StrengthHalf / Math.Pow((_boundryStop - _boundryStart) * .5d, this.Exponent);

                // Ratios
                if (Math3D.IsNearZero(offset.X))
                {
                    _ratioX = 1d;
                }
                else
                {
                    _ratioX = maxValue / offset.X;
                }

                if (Math3D.IsNearZero(offset.Y))
                {
                    _ratioY = 1d;
                }
                else
                {
                    _ratioY = maxValue / offset.Y;
                }

                if (Math3D.IsNearZero(offset.Z))
                {
                    _ratioZ = 1d;
                }
                else
                {
                    _ratioZ = maxValue / offset.Z;
                }

                #endregion
            }

            #endregion

            #region Public Properties

            /// <summary>
            /// This is the percent of the map's size where the boundry force starts to be felt (a good value is .85)
            /// </summary>
            public readonly double StartPercent;
            /// <summary>
            /// This is the strength of the force halfway between the start of the boundry and the map's edge (the strength doesn't climb linearly, it's quadratic)
            /// </summary>
            public readonly double StrengthHalf;
            /// <summary>
            /// This is how quickly strength should climb as distance goes past the boundry start.  This is the n in:
            ///		force = c * x^n
            /// </summary>
            public readonly double Exponent;

            public readonly Point3D MapMin;
            public readonly Point3D MapMax;

            #endregion

            #region Public Methods

            public Vector3D GetForce(Point3D point)
            {
                // I can't figure out how to do this with an elliptical map, so scale it to a sphere, do the calculations, then squash back to an ellipse
                Vector3D dirToCenter = point - _center;
                Vector3D scaledDirToCenter = new Vector3D(dirToCenter.X * _ratioX, dirToCenter.Y * _ratioY, dirToCenter.Z * _ratioZ);

                if (scaledDirToCenter.LengthSquared > _boundryStartSquared)
                {
                    double distFromBoundry = scaledDirToCenter.Length - _boundryStart;     // wait till now to do the expensive operation.

                    double force = _equationConstant * Math.Pow(distFromBoundry, this.Exponent);

                    // Notice that the magnitude of the force doesn't scale, but the direction is toward the center in the orig elliptical form
                    return dirToCenter.ToUnit() * -force;
                }
                else
                {
                    return new Vector3D(0, 0, 0);
                }
            }

            #endregion
        }

        #endregion
        #region Class: SwirlField

        public class SwirlField
        {
            #region Constructor

            public SwirlField(double strength, Vector3D axis, double angle)
            {
                this.Strength = strength;
                this.Axis = axis;
                this.Angle = angle;
            }

            #endregion

            #region Public Properties

            public readonly double Strength;		//= 250d;

            public readonly Vector3D Axis;		//= new Vector3D(0, 0, 1);
            public readonly double Angle;		//= 10d;

            #endregion

            #region Public Methods

            public Vector3D GetForce(Point3D point)
            {
                // Get a vector pointing toward the origin (the center of the field)
                Vector3D direction = point.ToVector() * -1d;
                direction.Normalize();

                // Rotate it a bit
                direction = direction.GetRotatedVector(this.Axis, this.Angle);

                // Exit Function
                return direction * this.Strength;
            }

            #endregion
        }

        #endregion

        #region Declaration Section

        private Map _map = null;

        private volatile FieldOctree<GravityCell> _fieldRoot = null;

        /// <summary>
        /// This is the map's root node token from when the last field was built
        /// </summary>
        private long? _lastSnapshotToken = null;

        /// <summary>
        /// This isn't meant to tick in regular intervals.  When a field snapshot is finished, this is enabled with an interval that will
        /// tick when the next snapshot should be built (and disabled until needed again)
        /// </summary>
        private DispatcherTimer _timer = null;

        //private volatile BoundryField _boundryField = null;

        #endregion

        #region Constructor

        public GravityFieldSpace(Map map)
        {
            _map = map;

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(map.SnapshotFequency_Milliseconds);
            _timer.Tick += new EventHandler(Timer_Tick);
            _timer.IsEnabled = true;
        }

        #endregion

        #region IGravityField Members

        public Vector3D GetForce(Point3D point)
        {
            Vector3D retVal = new Vector3D(0, 0, 0);

            // If there is a swirl, then calculate it for this point
            SwirlField swirl = this.Swirl;		// can only hit the property once, it could be swapped out at any moment
            if (swirl != null)
            {
                retVal += swirl.GetForce(point);
            }

            // Boundry field
            BoundryField boundry = this.Boundry;
            if (boundry != null)
            {
                retVal += boundry.GetForce(point);
            }

            // Grab the root for the remainder of this method (the public one could swap out at any moment)
            var root = _fieldRoot;
            if (root == null)
            {
                return retVal;
            }

            if (!Math3D.IsInside_AABB(root.MinRange, root.MaxRange, point))
            {
                // The point is outside the root, so just assume no gravity (the root should have been big enough to surround all objects, so there shouldn't
                // be any requests outside the root)
                return retVal;
            }

            // Recurse down, and get the leaf for this point
            GravityCell cell = root.GetLeaf(point);
            if (cell == null)
            {
                // There should be a leaf for all points within the tree.  But rather than an exception, just assume zero force
                return retVal;
            }

            // Exit Function
            return retVal + cell.Force;
        }

        #endregion

        #region Public Properties

        private volatile object _gravitationalConstant = 0d;// .0002d; //.00005d;
        public double GravitationalConstant
        {
            get
            {
                return (double)_gravitationalConstant;
            }
            set
            {
                _gravitationalConstant = value;
            }
        }

        /// <summary>
        /// This applies a force that will cause objects to orbit the center of the map
        /// </summary>
        public volatile SwirlField Swirl = null;

        /// <summary>
        /// This applies an inward pointing force if they get near the edge of the map
        /// </summary>
        public volatile BoundryField Boundry = null;

        #endregion

        #region Public Methods

        /// <summary>
        /// This is exposed for debug purposes
        /// </summary>
        public IEnumerable<GravityCell> GetLeaves()
        {
            List<GravityCell> retVal = new List<GravityCell>();

            GetLeavesSprtRecurse(retVal, _fieldRoot);

            return retVal;
        }
        private static void GetLeavesSprtRecurse(List<GravityCell> returnList, FieldOctree<GravityCell> node)
        {
            if (node == null)
            {
                return;
            }

            if (node.IsLeaf)
            {
                returnList.Add(node.Leaf);
            }
            else
            {
                // Recurse
                GetLeavesSprtRecurse(returnList, node.X0_Y0_Z0);
                GetLeavesSprtRecurse(returnList, node.X0_Y0_Z1);
                GetLeavesSprtRecurse(returnList, node.X0_Y1_Z0);
                GetLeavesSprtRecurse(returnList, node.X0_Y1_Z1);
                GetLeavesSprtRecurse(returnList, node.X1_Y0_Z0);
                GetLeavesSprtRecurse(returnList, node.X1_Y0_Z1);
                GetLeavesSprtRecurse(returnList, node.X1_Y1_Z0);
                GetLeavesSprtRecurse(returnList, node.X1_Y1_Z1);
            }
        }

        #endregion

        #region Event Listeners

        private void Timer_Tick(object sender, EventArgs e)
        {
            _timer.IsEnabled = false;
            DateTime startTime = DateTime.Now;

            MapOctree snapshot = _map.LatestSnapshot;
            if (snapshot == null || (_lastSnapshotToken != null && snapshot.Token == _lastSnapshotToken.Value))
            {
                // There is nothing to do, rig the timer to try again later
                ScheduleNextTick(startTime);
                return;
            }

            _lastSnapshotToken = snapshot.Token;
            double gravConstant = this.GravitationalConstant;

            // Build the field on a different thread
            var task = Task.Factory.StartNew(() =>
            {
                _fieldRoot = BuildField(snapshot, gravConstant);
            });

            // After the tree is built, schedule the next build from within this current thread
            task.ContinueWith(resultTask =>
            {
                // Schedule the next snapshot
                ScheduleNextTick(startTime);
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        #endregion

        #region Private Methods

        private static FieldOctree<GravityCell> BuildField(MapOctree mapSnapshot, double gravConstant)
        {
            // Dig through the tree, and construct my tree with partially populated leaves (mass but no force)
            List<GravityCell> allLeavesList = new List<GravityCell>();
            FieldOctree<GravityCell> retVal = BuildFieldSprtNode(allLeavesList, null, mapSnapshot);

            GravityCell[] allLeaves = allLeavesList.ToArray();
            Vector3D[] forces = new Vector3D[allLeaves.Length];

            // Calculate the force of gravity for each cell
            //TODO: Individual far away cells shouldn't need to be processed individually, use their parent instead
            BuildFieldSprtForces(allLeaves, forces, gravConstant);

            // Sort on token to make things easier for the swapper
            SortedList<long, int> indicesByToken = new SortedList<long, int>();
            for (int cntr = 0; cntr < allLeaves.Length; cntr++)
            {
                indicesByToken.Add(allLeaves[cntr].Token, cntr);
            }

            // Inject the forces into the leaves
            BuildFieldSprtSwapLeaves(retVal, indicesByToken, forces);

            // Exit Function
            return retVal;
        }

        private static FieldOctree<GravityCell> BuildFieldSprtNode(List<GravityCell> allLeaves, MapOctree[] ancestors, MapOctree node)
        {
            #region Add up mass

            double massOfNode = 0d;
            if (node.Items != null)
            {
                foreach (MapObjectInfo item in node.Items)
                {
                    massOfNode += item.Mass;
                }
            }

            #endregion

            if (!node.HasChildren)
            {
                #region Leaf

                // Exit Function
                GravityCell cell = new GravityCell(BuildFieldSprtNodeSprtAncestorMass(ancestors, node.MinRange, node.MaxRange) + massOfNode, node.CenterPoint);
                allLeaves.Add(cell);
                return new FieldOctree<GravityCell>(node.MinRange, node.MaxRange, node.CenterPoint, cell);

                #endregion
            }

            // Make the return node
            FieldOctree<GravityCell> retVal = new FieldOctree<GravityCell>(node.MinRange, node.MaxRange, node.CenterPoint);

            #region Build ancestor arrays

            // Create new arrays that hold this node's values (to pass to the children)
            MapOctree[] ancestorsNew = null;
            if (ancestors == null)
            {
                // This is the root
                ancestorsNew = new MapOctree[] { node };
            }
            else
            {
                // This is a middle ancestor
                ancestorsNew = new MapOctree[ancestors.Length + 1];
                Array.Copy(ancestors, ancestorsNew, ancestors.Length);
                ancestorsNew[ancestorsNew.Length - 1] = node;
            }

            #endregion

            #region Add the children

            //NOTE: The map's octree will leave children null if there are no items in them.  But this tree always has all 8 children

            if (node.X0_Y0_Z0 == null)
            {
                Point3D childMin = new Point3D(node.MinRange.X, node.MinRange.Y, node.MinRange.Z);
                Point3D childMax = new Point3D(node.CenterPoint.X, node.CenterPoint.Y, node.CenterPoint.Z);
                retVal.X0_Y0_Z0 = BuildFieldSprtNode(allLeaves, BuildFieldSprtNodeSprtAncestorMass(ancestors, childMin, childMax) + massOfNode, childMin, childMax);
            }
            else
            {
                retVal.X0_Y0_Z0 = BuildFieldSprtNode(allLeaves, ancestorsNew, node.X0_Y0_Z0);
            }

            if (node.X0_Y0_Z1 == null)
            {
                Point3D childMin = new Point3D(node.MinRange.X, node.MinRange.Y, node.CenterPoint.Z);
                Point3D childMax = new Point3D(node.CenterPoint.X, node.CenterPoint.Y, node.MaxRange.Z);
                retVal.X0_Y0_Z1 = BuildFieldSprtNode(allLeaves, BuildFieldSprtNodeSprtAncestorMass(ancestors, childMin, childMax) + massOfNode, childMin, childMax);
            }
            else
            {
                retVal.X0_Y0_Z1 = BuildFieldSprtNode(allLeaves, ancestorsNew, node.X0_Y0_Z1);
            }

            if (node.X0_Y1_Z0 == null)
            {
                Point3D childMin = new Point3D(node.MinRange.X, node.CenterPoint.Y, node.MinRange.Z);
                Point3D childMax = new Point3D(node.CenterPoint.X, node.MaxRange.Y, node.CenterPoint.Z);
                retVal.X0_Y1_Z0 = BuildFieldSprtNode(allLeaves, BuildFieldSprtNodeSprtAncestorMass(ancestors, childMin, childMax) + massOfNode, childMin, childMax);
            }
            else
            {
                retVal.X0_Y1_Z0 = BuildFieldSprtNode(allLeaves, ancestorsNew, node.X0_Y1_Z0);
            }

            if (node.X0_Y1_Z1 == null)
            {
                Point3D childMin = new Point3D(node.MinRange.X, node.CenterPoint.Y, node.CenterPoint.Z);
                Point3D childMax = new Point3D(node.CenterPoint.X, node.MaxRange.Y, node.MaxRange.Z);
                retVal.X0_Y1_Z1 = BuildFieldSprtNode(allLeaves, BuildFieldSprtNodeSprtAncestorMass(ancestors, childMin, childMax) + massOfNode, childMin, childMax);
            }
            else
            {
                retVal.X0_Y1_Z1 = BuildFieldSprtNode(allLeaves, ancestorsNew, node.X0_Y1_Z1);
            }

            if (node.X1_Y0_Z0 == null)
            {
                Point3D childMin = new Point3D(node.CenterPoint.X, node.MinRange.Y, node.MinRange.Z);
                Point3D childMax = new Point3D(node.MaxRange.X, node.CenterPoint.Y, node.CenterPoint.Z);
                retVal.X1_Y0_Z0 = BuildFieldSprtNode(allLeaves, BuildFieldSprtNodeSprtAncestorMass(ancestors, childMin, childMax) + massOfNode, childMin, childMax);
            }
            else
            {
                retVal.X1_Y0_Z0 = BuildFieldSprtNode(allLeaves, ancestorsNew, node.X1_Y0_Z0);
            }

            if (node.X1_Y0_Z1 == null)
            {
                Point3D childMin = new Point3D(node.CenterPoint.X, node.MinRange.Y, node.CenterPoint.Z);
                Point3D childMax = new Point3D(node.MaxRange.X, node.CenterPoint.Y, node.MaxRange.Z);
                retVal.X1_Y0_Z1 = BuildFieldSprtNode(allLeaves, BuildFieldSprtNodeSprtAncestorMass(ancestors, childMin, childMax) + massOfNode, childMin, childMax);
            }
            else
            {
                retVal.X1_Y0_Z1 = BuildFieldSprtNode(allLeaves, ancestorsNew, node.X1_Y0_Z1);
            }

            if (node.X1_Y1_Z0 == null)
            {
                Point3D childMin = new Point3D(node.CenterPoint.X, node.CenterPoint.Y, node.MinRange.Z);
                Point3D childMax = new Point3D(node.MaxRange.X, node.MaxRange.Y, node.CenterPoint.Z);
                retVal.X1_Y1_Z0 = BuildFieldSprtNode(allLeaves, BuildFieldSprtNodeSprtAncestorMass(ancestors, childMin, childMax) + massOfNode, childMin, childMax);
            }
            else
            {
                retVal.X1_Y1_Z0 = BuildFieldSprtNode(allLeaves, ancestorsNew, node.X1_Y1_Z0);
            }

            if (node.X1_Y1_Z1 == null)
            {
                Point3D childMin = new Point3D(node.CenterPoint.X, node.CenterPoint.Y, node.CenterPoint.Z);
                Point3D childMax = new Point3D(node.MaxRange.X, node.MaxRange.Y, node.MaxRange.Z);
                retVal.X1_Y1_Z1 = BuildFieldSprtNode(allLeaves, BuildFieldSprtNodeSprtAncestorMass(ancestors, childMin, childMax) + massOfNode, childMin, childMax);
            }
            else
            {
                retVal.X1_Y1_Z1 = BuildFieldSprtNode(allLeaves, ancestorsNew, node.X1_Y1_Z1);
            }

            #endregion

            // Exit Function
            return retVal;
        }
        private static double BuildFieldSprtNodeSprtAncestorMass(MapOctree[] ancestors, Point3D minRange, Point3D maxRange)
        {
            if (ancestors == null)
            {
                return 0d;
            }

            double retVal = 0d;

            for (int ancestorCntr = 0; ancestorCntr < ancestors.Length; ancestorCntr++)
            {
                if (ancestors[ancestorCntr].Items == null)
                {
                    continue;
                }

                for (int itemCntr = 0; itemCntr < ancestors[ancestorCntr].Items.Length; itemCntr++)
                {
                    MapObjectInfo item = ancestors[ancestorCntr].Items[itemCntr];

                    // Make sure the item is inside this cell
                    if (!Math3D.IsIntersecting_AABB_AABB(item.AABBMin, item.AABBMax, minRange, maxRange))
                    {
                        continue;
                    }

                    // Get the amount of the ancestor's shape that is inside this cell
                    double fromX = minRange.X < item.AABBMin.X ? item.AABBMin.X : minRange.X;
                    double toX = maxRange.X > item.AABBMax.X ? item.AABBMax.X : maxRange.X;

                    double fromY = minRange.Y < item.AABBMin.Y ? item.AABBMin.Y : minRange.Y;
                    double toY = maxRange.Y > item.AABBMax.Y ? item.AABBMax.Y : maxRange.Y;

                    double fromZ = minRange.Z < item.AABBMin.Z ? item.AABBMin.Z : minRange.Z;
                    double toZ = maxRange.Z > item.AABBMax.Z ? item.AABBMax.Z : maxRange.Z;

                    // Get the percent that's inside compared to the whoe shape
                    double subVolume = (toX - fromX) * (toY - fromY) * (toZ - fromZ);
                    double wholeVolume = (item.AABBMax.X - item.AABBMin.X) * (item.AABBMax.Y - item.AABBMin.Y) * (item.AABBMax.Z - item.AABBMin.Z);

                    double percent = subVolume / wholeVolume;

                    // Take that percent of the shape's mass
                    retVal += item.Mass * percent;
                }
            }

            return retVal;
        }

        #region OLD

        //private static FieldOctree<GravityCell> BuildFieldSprtNode_OLD(List<GravityCell> allLeaves, MapOctree[] ancestors, double[] ancestorMasses, MapOctree node)
        //{
        //    #region Add up mass

        //    // Add up all the mass sitting in the ancestors
        //    double massFromAbove = 0d;
        //    if (ancestorMasses != null)
        //    {
        //        for (int cntr = 0; cntr < ancestorMasses.Length; cntr++)
        //        {
        //            // Each child shares 1/8th of the parent's mass + 1/64th of the grandparents, etc
        //            // (these masses directly at each ancestor is of the objects that are straddling the lines between nodes)
        //            massFromAbove += ancestorMasses[cntr] * Math.Pow(.125d, ancestorMasses.Length - cntr);
        //        }
        //    }

        //    double massOfNode = 0d;
        //    if (node.Items != null)
        //    {
        //        foreach (MapObjectInfo item in node.Items)
        //        {
        //            massOfNode += item.Mass;
        //        }
        //    }

        //    #endregion

        //    if (!node.HasChildren)
        //    {
        //        #region Leaf

        //        // Exit Function
        //        GravityCell cell = new GravityCell(massFromAbove + massOfNode, node.CenterPoint);
        //        allLeaves.Add(cell);
        //        return new FieldOctree<GravityCell>(node.MinRange, node.MaxRange, node.CenterPoint, cell);

        //        #endregion
        //    }

        //    // Make the return node
        //    FieldOctree<GravityCell> retVal = new FieldOctree<GravityCell>(node.MinRange, node.MaxRange, node.CenterPoint);

        //    #region Build ancestor arrays

        //    // Create new arrays that hold this node's values (to pass to the children)
        //    MapOctree[] ancestorsNew = null;
        //    double[] ancestorMassesNew = null;
        //    if (ancestors == null)
        //    {
        //        // This is the root
        //        ancestorsNew = new MapOctree[] { node };
        //        ancestorMassesNew = new double[] { massOfNode };
        //    }
        //    else
        //    {
        //        // This is a middle ancestor
        //        ancestorsNew = new MapOctree[ancestors.Length + 1];
        //        Array.Copy(ancestors, ancestorsNew, ancestors.Length);
        //        ancestorsNew[ancestorsNew.Length - 1] = node;

        //        ancestorMassesNew = new double[ancestorMasses.Length + 1];
        //        Array.Copy(ancestorMasses, ancestorMassesNew, ancestorMasses.Length);
        //        ancestorMassesNew[ancestorMassesNew.Length - 1] = massOfNode;
        //    }

        //    #endregion

        //    #region Add the children

        //    //NOTE: The map's octree will leave children null if there are no items in them.  But this tree always has all 8 children

        //    if (node.X0_Y0_Z0 == null)
        //    {
        //        retVal.X0_Y0_Z0 = BuildFieldSprtNode(allLeaves, massFromAbove + massOfNode, new Point3D(node.MinRange.X, node.MinRange.Y, node.MinRange.Z), new Point3D(node.CenterPoint.X, node.CenterPoint.Y, node.CenterPoint.Z));
        //    }
        //    else
        //    {
        //        retVal.X0_Y0_Z0 = BuildFieldSprtNode(allLeaves, ancestorsNew, ancestorMassesNew, node.X0_Y0_Z0);
        //    }

        //    if (node.X0_Y0_Z1 == null)
        //    {
        //        retVal.X0_Y0_Z1 = BuildFieldSprtNode(allLeaves, massFromAbove + massOfNode, new Point3D(node.MinRange.X, node.MinRange.Y, node.CenterPoint.Z), new Point3D(node.CenterPoint.X, node.CenterPoint.Y, node.MaxRange.Z));
        //    }
        //    else
        //    {
        //        retVal.X0_Y0_Z1 = BuildFieldSprtNode(allLeaves, ancestorsNew, ancestorMassesNew, node.X0_Y0_Z1);
        //    }

        //    if (node.X0_Y1_Z0 == null)
        //    {
        //        retVal.X0_Y1_Z0 = BuildFieldSprtNode(allLeaves, massFromAbove + massOfNode, new Point3D(node.MinRange.X, node.CenterPoint.Y, node.MinRange.Z), new Point3D(node.CenterPoint.X, node.MaxRange.Y, node.CenterPoint.Z));
        //    }
        //    else
        //    {
        //        retVal.X0_Y1_Z0 = BuildFieldSprtNode(allLeaves, ancestorsNew, ancestorMassesNew, node.X0_Y1_Z0);
        //    }

        //    if (node.X0_Y1_Z1 == null)
        //    {
        //        retVal.X0_Y1_Z1 = BuildFieldSprtNode(allLeaves, massFromAbove + massOfNode, new Point3D(node.MinRange.X, node.CenterPoint.Y, node.CenterPoint.Z), new Point3D(node.CenterPoint.X, node.MaxRange.Y, node.MaxRange.Z));
        //    }
        //    else
        //    {
        //        retVal.X0_Y1_Z1 = BuildFieldSprtNode(allLeaves, ancestorsNew, ancestorMassesNew, node.X0_Y1_Z1);
        //    }

        //    if (node.X1_Y0_Z0 == null)
        //    {
        //        retVal.X1_Y0_Z0 = BuildFieldSprtNode(allLeaves, massFromAbove + massOfNode, new Point3D(node.CenterPoint.X, node.MinRange.Y, node.MinRange.Z), new Point3D(node.MaxRange.X, node.CenterPoint.Y, node.CenterPoint.Z));
        //    }
        //    else
        //    {
        //        retVal.X1_Y0_Z0 = BuildFieldSprtNode(allLeaves, ancestorsNew, ancestorMassesNew, node.X1_Y0_Z0);
        //    }

        //    if (node.X1_Y0_Z1 == null)
        //    {
        //        retVal.X1_Y0_Z1 = BuildFieldSprtNode(allLeaves, massFromAbove + massOfNode, new Point3D(node.CenterPoint.X, node.MinRange.Y, node.CenterPoint.Z), new Point3D(node.MaxRange.X, node.CenterPoint.Y, node.MaxRange.Z));
        //    }
        //    else
        //    {
        //        retVal.X1_Y0_Z1 = BuildFieldSprtNode(allLeaves, ancestorsNew, ancestorMassesNew, node.X1_Y0_Z1);
        //    }

        //    if (node.X1_Y1_Z0 == null)
        //    {
        //        retVal.X1_Y1_Z0 = BuildFieldSprtNode(allLeaves, massFromAbove + massOfNode, new Point3D(node.CenterPoint.X, node.CenterPoint.Y, node.MinRange.Z), new Point3D(node.MaxRange.X, node.MaxRange.Y, node.CenterPoint.Z));
        //    }
        //    else
        //    {
        //        retVal.X1_Y1_Z0 = BuildFieldSprtNode(allLeaves, ancestorsNew, ancestorMassesNew, node.X1_Y1_Z0);
        //    }

        //    if (node.X1_Y1_Z1 == null)
        //    {
        //        retVal.X1_Y1_Z1 = BuildFieldSprtNode(allLeaves, massFromAbove + massOfNode, new Point3D(node.CenterPoint.X, node.CenterPoint.Y, node.CenterPoint.Z), new Point3D(node.MaxRange.X, node.MaxRange.Y, node.MaxRange.Z));
        //    }
        //    else
        //    {
        //        retVal.X1_Y1_Z1 = BuildFieldSprtNode(allLeaves, ancestorsNew, ancestorMassesNew, node.X1_Y1_Z1);
        //    }

        //    #endregion

        //    // Exit Function
        //    return retVal;
        //}

        #endregion

        private static FieldOctree<GravityCell> BuildFieldSprtNode(List<GravityCell> allLeaves, double mass, Point3D min, Point3D max)
        {
            Point3D center = new Point3D((min.X + max.X) * .5d, (min.Y + max.Y) * .5d, (min.Z + max.Z) * .5d);

            GravityCell cell = new GravityCell(mass, center);
            allLeaves.Add(cell);
            return new FieldOctree<GravityCell>(min, max, center, cell);
        }

        private static void BuildFieldSprtForces(GravityCell[] leaves, Vector3D[] forces, double gravConstant)
        {
            for (int outer = 0; outer <= leaves.Length - 1; outer++)
            {
                for (int inner = outer + 1; inner < leaves.Length; inner++)
                {
                    double distance = (leaves[outer].Position - leaves[inner].Position).Length;

                    double force = 0d;
                    if (distance > 0d)		// cells should never have a distance of zero
                    {
                        force = (gravConstant * leaves[outer].Mass * leaves[inner].Mass) / distance;
                    }

                    Vector3D forceVect = (leaves[inner].Position - leaves[outer].Position).ToUnit() * force;

                    forces[outer] += forceVect;
                    forces[inner] -= forceVect;
                }
            }
        }

        private static void BuildFieldSprtSwapLeaves(FieldOctree<GravityCell> node, SortedList<long, int> indicesByToken, Vector3D[] forces)
        {
            if (node.IsLeaf)
            {
                // Find the index into forces
                int index = indicesByToken[node.Leaf.Token];

                // Swap it out
                node.Leaf = new GravityCell(node.Leaf.Token, node.Leaf.Mass, node.Leaf.Position, forces[index]);
            }
            else
            {
                // Every child is non null, so just recurse with each one
                BuildFieldSprtSwapLeaves(node.X0_Y0_Z0, indicesByToken, forces);
                BuildFieldSprtSwapLeaves(node.X0_Y0_Z1, indicesByToken, forces);
                BuildFieldSprtSwapLeaves(node.X0_Y1_Z0, indicesByToken, forces);
                BuildFieldSprtSwapLeaves(node.X0_Y1_Z1, indicesByToken, forces);
                BuildFieldSprtSwapLeaves(node.X1_Y0_Z0, indicesByToken, forces);
                BuildFieldSprtSwapLeaves(node.X1_Y0_Z1, indicesByToken, forces);
                BuildFieldSprtSwapLeaves(node.X1_Y1_Z0, indicesByToken, forces);
                BuildFieldSprtSwapLeaves(node.X1_Y1_Z1, indicesByToken, forces);
            }
        }

        private void ScheduleNextTick(DateTime startTime)
        {
            double nextSnapshotWaitTime = _map.SnapshotFequency_Milliseconds - (DateTime.Now - startTime).TotalMilliseconds;
            if (nextSnapshotWaitTime < 1d)
            {
                nextSnapshotWaitTime = 1d;
            }

            _timer.Interval = TimeSpan.FromMilliseconds(nextSnapshotWaitTime);
            _timer.IsEnabled = true;
        }

        #endregion
    }

    #endregion
    #region Class: GravityFieldUniform

    /// <summary>
    /// This has the same gravity regardless of location
    /// NOTE: If you want to emulate constant gravity like on a planet, you should directly set acceleration instead of force
    /// </summary>
    public class GravityFieldUniform : IGravityField
    {
        #region IGravityField Members

        public Vector3D GetForce(Point3D point)
        {
            // The location passed in has no meaning for this class.  Gravity is the same everywhere
            return this.Gravity;
        }

        #endregion

        #region Public Properties

        private volatile object _gravity = new Vector3D(0, 0, 0);
        public Vector3D Gravity
        {
            get
            {
                return (Vector3D)_gravity;
            }
            set
            {
                _gravity = value;
            }
        }

        #endregion
    }

    #endregion

    #region Class: RadiationField

    /// <summary>
    /// This represents radiation.  It's useful for things like solar panels, and parts that get damaged or work inefficiently in the
    /// presence of too much radiation
    /// </summary>
    /// <remarks>
    /// This will have two types of radiation:
    /// 	Ambient:
    /// 		Just a constant value
    ///
    /// 	Directional:
    /// 		Radiation will emit from sources, and be blocked by large bodies (point lights and shadows)
    /// 		A given cell will have more than just a single vector flowing through it.  Each radiation source will make its own vector
    /// </remarks>
    public class RadiationField
    {
        #region Public Properties

        private volatile object _ambientRadiation = 0d;
        public double AmbientRadiation
        {
            get
            {
                return (double)_ambientRadiation;
            }
            set
            {
                _ambientRadiation = value;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// This returns the sum of the radiation at that point
        /// </summary>
        /// <remarks>
        /// Use this overload if you are doing basic calculations and don't care about directional stuff
        /// 
        /// A good use for this would be internal ship parts that are assumed to be roughly spherical/cubical.  Pre calculations can be done
        /// to see how protected those parts are by other parts of the ship (but only if it's protected on all sides).  Then the value returned
        /// by this method would be multipied by that protection percent.
        /// </remarks>
        public double GetRadiation(Point3D point)
        {
            return this.AmbientRadiation;
        }
        /// <summary>
        /// This overload is meant for things like solar panels and radiation shielding
        /// NOTE: Ambient radiation is never diminished (which isn't very realistic, so be careful about setting ambient radiation too high)
        /// </summary>
        /// <remarks>
        /// Up front calculations should be done to figure out how much parts are obscured in various directions before calling this method
        /// in real time.
        /// 
        /// For example, you may have a double sided solar panel, but one side may be blocked by all kinds of parts, so the normal passed
        /// in for the blocked side of the solar panel would be something less than the actual area of that panel.
        /// </remarks>
        public double GetRadiation(Point3D point, Vector3D normal, bool ignoreBackSide)
        {
            return this.AmbientRadiation;
        }

        #endregion
    }

    #endregion

    #region Class: FieldOctree

    //TODO: Clean up the comments
    //TODO: Make this Tparent, Tleaf so that info can be stored in each parent

    /// <remarks>
    /// The goal of this class is to be written to in one thread, and read by many threads
    /// 
    /// Parts of it should be immutable, and the parts that change need to be done without using locks (Interlocked.Exchange)
    /// 
    /// The immutable parts will be bounding range (its location in the world)
    /// 
    /// The mutable but thread safe atomic parts are the children, or T if this is a leaf ~ I think?
    /// </remarks>
    public class FieldOctree<T> where T : class  // the where is needed so that leaf can be marked volatile
    {
        #region Constructor

        /// <summary>
        /// This overload defines a parent
        /// </summary>
        public FieldOctree(Point3D minRange, Point3D maxRange, Point3D centerPoint)
        {
            _isLeaf = false;
            _minRange = minRange;
            _maxRange = maxRange;
            _centerPoint = centerPoint;
        }
        /// <summary>
        /// This overload defines a leaf
        /// </summary>
        public FieldOctree(Point3D minRange, Point3D maxRange, Point3D centerPoint, T leaf)
        {
            _isLeaf = true;
            _minRange = minRange;
            _maxRange = maxRange;
            _centerPoint = centerPoint;

            this.Leaf = leaf;
        }

        #endregion

        #region Public Properties

        private readonly Point3D _centerPoint;
        /// <summary>
        /// This is useful for figuring out which child tree to look in
        /// NOTE: use LessThanOrEqual, and GreaterThan for doing comparisons (never GreaterThanOrEqual)
        /// </summary>
        public Point3D CenterPoint
        {
            get
            {
                return _centerPoint;
            }
        }

        // These are readonly so that math can be performed without worrying if the state will change in the middle (because
        // of another thread writing to this class)
        //
        // It can't be enforced by the compiler, but if this is a parent, then each child will be an even subdivision of this volume (1/8th)
        private readonly Point3D _minRange;
        public Point3D MinRange
        {
            get
            {
                return _minRange;
            }
        }
        private readonly Point3D _maxRange;
        public Point3D MaxRange
        {
            get
            {
                return _maxRange;
            }
        }

        // Either _value will be set, or the children will be set.  If a tree needs to be made the space of a leaf, a new instance of
        // FieldOctree needs to be created, and swapped to the parent
        //
        // This is readonly so that once you're handed an instance of this octree, you can be certain that it will always be a leaf
        // or a parent (because seeing if it's a leaf, and getting the value are two steps, so can't be made threadsafe without a lock,
        // or _isLeaf being imutable)
        private readonly bool _isLeaf;
        public bool IsLeaf
        {
            get
            {
                return _isLeaf;
            }
        }

        /// <summary>
        /// This only has meaning if IsLeaf is true
        /// </summary>
        /// <remarks>
        /// IsLeaf is readonly, but Leaf can be swapped out at whim by another thread.  The properties of leaf need to be readonly (can't be
        /// enforced by this class, but they should be)
        /// 
        /// So always grab an instance of this.Leaf, and pull properties off of that local reference.
        /// 
        /// For example:
        ///		Never get this.Leaf.Var1, and this.Leaf.Var2
        ///		Instead get Leaf leaf = this.Leaf; leaf.Var1; leaf.Var2;
        /// </remarks>
        public volatile T Leaf;

        // Since an octree has 8 children, each child corresponds to a +-X, +-Y, +-Z
        // For naming, negative is 0, positive is 1.  So X0Y0Z0 is the bottom left back cell, and X1Y1Z1 is the top right front cell
        public volatile FieldOctree<T> X0_Y0_Z0 = null;
        public volatile FieldOctree<T> X0_Y0_Z1 = null;
        public volatile FieldOctree<T> X0_Y1_Z0 = null;
        public volatile FieldOctree<T> X0_Y1_Z1 = null;
        public volatile FieldOctree<T> X1_Y0_Z0 = null;
        public volatile FieldOctree<T> X1_Y0_Z1 = null;
        public volatile FieldOctree<T> X1_Y1_Z0 = null;
        public volatile FieldOctree<T> X1_Y1_Z1 = null;

        #endregion

        #region Public Methods

        public T GetLeaf(Point3D position)
        {
            if (!Math3D.IsInside_AABB(_minRange, _maxRange, position))
            {
                // Let a tree node throw an exception.  The container of the entire tree can decide whether to return null or an exception if a request
                // is out of bounds of the entire tree.
                throw new ArgumentOutOfRangeException("The position is not inside this tree");
            }

            if (_isLeaf)
            {
                return this.Leaf;
            }

            // Figure out which child to look in
            //NOTE: Because the children can be swapped out at any momemnt, I have to get that instance, and then check if that instance is null (can't
            // double call this.Child)

            FieldOctree<T> child = null;

            if (position.X <= _centerPoint.X && position.Y <= _centerPoint.Y && position.Z <= _centerPoint.Z)		// 0
            {
                child = this.X0_Y0_Z0;
            }
            else if (position.X <= _centerPoint.X && position.Y <= _centerPoint.Y && position.Z > _centerPoint.Z)		// 1
            {
                child = this.X0_Y0_Z1;
            }
            else if (position.X <= _centerPoint.X && position.Y > _centerPoint.Y && position.Z <= _centerPoint.Z)		// 2
            {
                child = this.X0_Y1_Z0;
            }
            else if (position.X <= _centerPoint.X && position.Y > _centerPoint.Y && position.Z > _centerPoint.Z)		// 3
            {
                child = this.X0_Y1_Z1;
            }
            else if (position.X > _centerPoint.X && position.Y <= _centerPoint.Y && position.Z <= _centerPoint.Z)		// 4
            {
                child = this.X1_Y0_Z0;
            }
            else if (position.X > _centerPoint.X && position.Y <= _centerPoint.Y && position.Z > _centerPoint.Z)		// 5
            {
                child = this.X1_Y0_Z1;
            }
            else if (position.X > _centerPoint.X && position.Y > _centerPoint.Y && position.Z <= _centerPoint.Z)		// 6
            {
                child = this.X1_Y1_Z0;
            }
            else //if (position.X > _centerPoint.X && position.Y > _centerPoint.Y && position.Z > _centerPoint.Z)		// 7
            {
                child = this.X1_Y1_Z1;
            }

            if (child == null)
            {
                return null;
            }

            // Recurse
            return child.GetLeaf(position);
        }

        #endregion
    }

    #endregion
}
