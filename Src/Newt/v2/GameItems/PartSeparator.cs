using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.GameItems
{
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
        public static void PullInCrude(out bool changed, PartSeparator_Part[] parts)
        {
            // Figure out the max radius
            double[] sizes = parts.Select(o => (o.Size.X + o.Size.Y + o.Size.Z) / 3d).ToArray();
            double largestPart = sizes.Max();
            double maxRadius = largestPart * 8d;
            double maxRadiusSquare = maxRadius * maxRadius;

            Point3D center = Math3D.GetCenter(parts.Select(o => Tuple.Create(o.Position, o.Mass)).ToArray());

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

        public static CollisionHull[] Separate(out bool changed, PartSeparator_Part[] parts, World world)
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
        private static Intersection[] GetIntersections(PartSeparator_Part[] parts, CollisionHull[] hulls, bool[] hasMoved, World world)
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
                        sizeOuter = sizeOuter ?? (parts[outer].Size.X + parts[outer].Size.X + parts[outer].Size.X) / 3d;
                        double sizeInner = (parts[inner].Size.X + parts[inner].Size.X + parts[inner].Size.X) / 3d;

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

        private static void DoStep(Intersection[] intersections, PartSeparator_Part[] parts, bool[] hasMoved)
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
                double mass1 = parts[intersection.Index1].Mass;
                double mass2 = parts[intersection.Index2].Mass;
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
            double angle = UtilityCore.GetScaledValue_Capped(0d, MAXANGLE, 0d, maxExpected, length);

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

        private static void DoStepSprtMove(PartSeparator_Part[] parts, SortedList<int, List<Tuple<Vector3D?, Quaternion?>>> moves)
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

    #region Class: PartSeparator_Part

    /// <summary>
    /// Instead of making an interface that various items need to implement, this is a wrapper class meant only for the part separator
    /// </summary>
    /// <remarks>
    /// All the properties/methods are virtual so a custom derived class could be used instead
    /// </remarks>
    public class PartSeparator_Part
    {
        #region Declaration Section

        private readonly bool _isLooseProps;

        // This is used if _isLooseProps is false
        private readonly PartBase _part;

        // These are used if _isLooseProps is true
        private readonly Point3D[] _convexPoints;
        private readonly Vector3D _size;
        private readonly double _mass;
        private Point3D _position = new Point3D(0, 0, 0);
        private Quaternion _orientation = Quaternion.Identity;

        #endregion

        #region Constructor

        protected PartSeparator_Part()
        {
            _isLooseProps = false;
            _part = null;
            _convexPoints = null;
            _size = new Vector3D(0, 0, 0);
            _mass = 0;
        }
        public PartSeparator_Part(PartBase part)
        {
            _part = part;

            _isLooseProps = false;
            _convexPoints = null;
            _size = new Vector3D(0, 0, 0);
            _mass = 0;
        }
        public PartSeparator_Part(Point3D[] convexPoints, double mass, Point3D position, Quaternion orientation)
        {
            _isLooseProps = true;

            _convexPoints = convexPoints;
            _mass = mass;
            _position = position;
            _orientation = orientation;

            _size = new Vector3D(
                convexPoints.Max(o => Math.Abs(o.X)),
                convexPoints.Max(o => Math.Abs(o.Y)),
                convexPoints.Max(o => Math.Abs(o.Z)));
        }

        #endregion

        #region Public Properties

        public virtual Vector3D Size
        {
            get
            {
                if (_isLooseProps)
                {
                    return _size;
                }
                else
                {
                    return _part.ScaleActual;
                }
            }
        }

        public virtual Point3D Position
        {
            get
            {
                if (_isLooseProps)
                {
                    return _position;
                }
                else
                {
                    return _part.Position;
                }
            }
            set
            {
                if (_isLooseProps)
                {
                    _position = value;
                }
                else
                {
                    _part.Position = value;
                }
            }
        }
        public virtual Quaternion Orientation
        {
            get
            {
                if (_isLooseProps)
                {
                    return _orientation;
                }
                else
                {
                    return _part.Orientation;
                }
            }
            set
            {
                if (_isLooseProps)
                {
                    _orientation = value;
                }
                else
                {
                    _part.Orientation = value;
                }
            }
        }

        public virtual double Mass
        {
            get
            {
                if (_isLooseProps)
                {
                    return _mass;
                }
                else
                {
                    return _part.TotalMass;
                }
            }
        }

        #endregion

        #region Public Methods

        public virtual CollisionHull CreateCollisionHull(World world)
        {
            if (_isLooseProps)
            {
                Transform3DGroup transform = new Transform3DGroup();
                //transform.Children.Add(new ScaleTransform3D(this.Scale));		// it ignores scale
                transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(_orientation)));
                transform.Children.Add(new TranslateTransform3D(_position.ToVector()));

                return CollisionHull.CreateConvexHull(world, 0, _convexPoints, transform.Value);
            }
            else
            {
                return _part.CreateCollisionHull(world);
            }
        }

        #endregion
    }

    #endregion
}
