using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v2.GameItems.MapParts;

namespace Game.Newt.v2.GameItems
{
    public static class HullVoronoiExploder
    {
        #region Declaration Section

        //TODO: Spread this out a little
        //private static Lazy<RandomBellArgs> _coneAxisBell = new Lazy<RandomBellArgs>(() => new RandomBellArgs(.64, -7.3, .93, -32.7));
        private static Lazy<RandomBellArgs> _coneAxisBell = new Lazy<RandomBellArgs>(() => new RandomBellArgs(.4, -24.7, .69, -17.3));

        #endregion

        public static HullVoronoiExploder_Response ShootHull(ITriangleIndexed[] convexHull, Tuple<Point3D, Vector3D, double>[] shots, HullVoronoiExploder_Options options = null)
        {
            options = options ?? new HullVoronoiExploder_Options();

            var aabb = Math3D.GetAABB(convexHull);
            double aabbLen = (aabb.Item2 - aabb.Item1).Length;

            // Create a voronoi, and intersect with the hull
            Tuple<HullVoronoiExploder_Response, bool> retVal = null;
            for (int cntr = 0; cntr < 15; cntr++)
            {
                try
                {
                    retVal = SplitHull(convexHull, shots, options, aabbLen);
                    break;
                }
                catch (Exception)
                {
                    // Every once in a while, there is an error with the voronoi, or voronoi intersecting the hull, etc.  Just try
                    // again with new random points
                }
            }

            if (retVal == null) return null;
            else if (!retVal.Item2) return retVal.Item1;

            // Figure out velocities
            retVal.Item1.Velocities = GetVelocities(retVal.Item1.Shards, retVal.Item1.Hits, Math.Sqrt(aabbLen / 2), options);

            return retVal.Item1;
        }

        #region Private Methods - split

        private static Tuple<HullVoronoiExploder_Response, bool> SplitHull(ITriangleIndexed[] convexHull, Tuple<Point3D, Vector3D, double>[] shots, HullVoronoiExploder_Options options, double aabbLen)
        {
            #region intersect with the hull

            var hits = shots.
                Select(o => new HullVoronoiExploder_ShotHit()
                {
                    Shot = o,
                    Hit = GetHit(convexHull, o.Item1, o.Item2, o.Item3),
                }).
                Where(o => o.Hit != null).
                ToArray();

            if (hits.Length == 0)
            {
                return null;
            }

            #endregion

            #region voronoi

            Point3D[] controlPoints = GetVoronoiCtrlPoints(hits, convexHull, options.MinCount, options.MaxCount, aabbLen);

            VoronoiResult3D voronoi = Math3D.GetVoronoi(controlPoints, true);
            if (voronoi == null)
            {
                return null;
            }

            #endregion

            // There is enough to start populating the response
            HullVoronoiExploder_Response retVal = new HullVoronoiExploder_Response()
            {
                Hits = hits,
                ControlPoints = controlPoints,
                Voronoi = voronoi,
            };

            #region intersect voronoi and hull

            // Intersect
            Tuple<int, ITriangleIndexed[]>[] shards = null;
            try
            {
                shards = Math3D.GetIntersection_Hull_Voronoi_full(convexHull, voronoi);
            }
            catch (Exception)
            {
                return Tuple.Create(retVal, false);
            }

            if (shards == null)
            {
                return Tuple.Create(retVal, false);
            }

            // Smooth
            if (options.ShouldSmoothShards)
            {
                shards = shards.
                    Select(o => Tuple.Create(o.Item1, Asteroid.SmoothTriangles(o.Item2))).
                    ToArray();
            }

            // Validate
            shards = shards.
                    Where(o => o.Item2 != null && o.Item2.Length >= 3).
                    Where(o =>
                    {
                        Vector3D firstNormal = o.Item2[0].NormalUnit;
                        return o.Item2.Skip(1).Any(p => !Math.Abs(Vector3D.DotProduct(firstNormal, p.NormalUnit)).IsNearValue(1));
                    }).
                    ToArray();

            if (shards.Length == 0)
            {
                return Tuple.Create(retVal, false);
            }

            #endregion

            #region populate shards

            retVal.Shards = shards.
                Select(o =>
                {
                    var aabb = Math3D.GetAABB(o.Item2);

                    double radius = Math.Sqrt((aabb.Item2 - aabb.Item1).Length / 2);

                    Point3D center = Math3D.GetCenter(TriangleIndexed.GetUsedPoints(o.Item2));
                    Vector3D centerVect = center.ToVector();

                    Point3D[] allPoints = o.Item2[0].AllPoints.
                        Select(p => p - centerVect).
                        ToArray();

                    TriangleIndexed[] shiftedTriangles = o.Item2.
                        Select(p => new TriangleIndexed(p.Index0, p.Index1, p.Index2, allPoints)).
                        ToArray();

                    return new HullVoronoiExploder_Shard()
                    {
                        VoronoiControlPointIndex = o.Item1,
                        Hull_ParentCoords = o.Item2,
                        Hull_Centered = shiftedTriangles,
                        Radius = radius,
                        Center_ParentCoords = center,
                    };
                }).
                Where(o => o != null).
                ToArray();

            #endregion

            return Tuple.Create(retVal, true);
        }

        private static Tuple<Point3D, Point3D> GetHit(ITriangle[] convexHull, Point3D shotStart, Vector3D shotDirection, double percent)
        {
            var intersections = Math3D.GetIntersection_Hull_Ray(convexHull, shotStart, shotDirection);

            if (intersections.Length != 2)
            {
                return (Tuple<Point3D, Point3D>)null;
            }

            Vector3D penetrate = (intersections[1].Item1 - intersections[0].Item1) * percent;

            return Tuple.Create(intersections[0].Item1, intersections[0].Item1 + penetrate);
        }

        private static void AddToHitline(ref int runningSum, List<Tuple<int, List<Point3D>>> sets, int index, Tuple<Point3D, Point3D> hit, double entryRadius, double exitRadius, double maxAxisLength, Random rand)
        {
            Point3D[] points = GetVoronoiCtrlPoints_AroundLine_Cone(hit.Item1, hit.Item2, rand.Next(2, 5), entryRadius, exitRadius, maxAxisLength);

            runningSum += points.Length;

            var existing = sets.FirstOrDefault(o => o.Item1 == index);
            if (existing == null)
            {
                existing = Tuple.Create(index, new List<Point3D>());
                sets.Add(existing);
            }

            existing.Item2.AddRange(points);
        }

        /// <summary>
        /// Each hit line should create a cone of probability to place control points onto
        /// Once a point on the surface of a cone is chosen, choose a couple more on that circle
        /// </summary>
        private static Point3D[] GetVoronoiCtrlPoints_AroundLine_Cone(Point3D lineStart, Point3D lineStop, int count, double entryRadius, double exitRadius, double maxAxisLength)
        {
            // Figure out where on the axis the control point ring should go (the bell has a spike around 30% in)
            double axisPercent = StaticRandomWPF.NextBell(_coneAxisBell.Value);

            Vector3D axis = lineStop - lineStart;
            double axisLength = axis.Length;

            Point3D ringCenter = lineStart + (axis * axisPercent);

            // Figure out the radius of the cone at this point
            double exitRadiusAdjusted = (axisLength / maxAxisLength) * exitRadius;

            double ringRadius = UtilityCore.GetScaledValue(entryRadius, exitRadiusAdjusted, 0, 1, axisPercent);

            // Get some points
            var points = Enumerable.Range(0, count).
                Select(o => Math3D.GetRandomVector_Circular_Shell(ringRadius).ToPoint());





            //TODO: Figure out the minimum to shift by
            //double shiftRadius = Math.Max(.01, ringRadius / 20);
            double shiftRadius = ringRadius / 20;
            points = points.Select(o => o + Math3D.GetRandomVector_Spherical(shiftRadius));     // the voronoi can't handle coplanar input







            // Rotate/Translate
            Quaternion roation = Math3D.GetRotation(new Vector3D(0, 0, 1), axis);

            Transform3DGroup transform = new Transform3DGroup();
            transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(roation)));
            transform.Children.Add(new TranslateTransform3D(ringCenter.X, ringCenter.Y, ringCenter.Z));

            points = points.Select(o => transform.Transform(o));

            return points.ToArray();
        }

        private static Point3D[] GetVoronoiCtrlPoints(HullVoronoiExploder_ShotHit[] hits, ITriangle[] convexHull, int minCount, int maxCount, double aabbLen)
        {
            const int MIN = 5;

            Random rand = StaticRandom.GetRandomForThread();

            #region examine hits

            Tuple<int, double>[] hitsByLength = hits.
                Select((o, i) => Tuple.Create(i, (o.Hit.Item2 - o.Hit.Item1).Length)).
                OrderByDescending(o => o.Item2).
                ToArray();

            double totalLength = hitsByLength.Sum(o => o.Item2);

            Tuple<int, double>[] hitsByPercentLength = hitsByLength.
                Select(o => Tuple.Create(o.Item1, o.Item2 / totalLength)).
                ToArray();

            #endregion

            #region define control point cones

            double entryRadius = aabbLen * .05;
            double exitRadius = aabbLen * .35;
            double maxAxisLength = aabbLen * .75;

            int count = hits.Length * 2;
            count += (totalLength / (aabbLen * .1)).ToInt_Round();

            if (count < minCount) count = minCount;
            else if (count > maxCount) count = maxCount;

            #endregion

            #region randomly pick control points

            // Keep adding rings around shots until the count is exceeded

            var sets = new List<Tuple<int, List<Point3D>>>();
            int runningSum = 0;

            // Make sure each hit line gets some points
            for (int cntr = 0; cntr < hits.Length; cntr++)
            {
                AddToHitline(ref runningSum, sets, cntr, hits[cntr].Hit, entryRadius, exitRadius, maxAxisLength, rand);
            }

            // Now that all hit lines have points, randomly choose lines until count is exceeded
            while (runningSum < count)
            {
                var pointsPerLength = hitsByLength.
                    Select(o =>
                    {
                        var pointsForIndex = sets.FirstOrDefault(p => p.Item1 == o.Item1);
                        int countForIndex = pointsForIndex == null ? 0 : pointsForIndex.Item2.Count;
                        return Tuple.Create(o.Item1, countForIndex / o.Item2);
                    }).
                    OrderBy(o => o.Item2).
                    ToArray();

                double sumRatio = pointsPerLength.Sum(o => o.Item2);

                var pointsPerLengthNormalized = pointsPerLength.
                    Select(o => Tuple.Create(o.Item1, o.Item2 / sumRatio)).
                    ToArray();

                int index = UtilityCore.GetIndexIntoList(rand.NextPow(3), pointsPerLengthNormalized);

                AddToHitline(ref runningSum, sets, index, hits[index].Hit, entryRadius, exitRadius, maxAxisLength, rand);
            }

            #endregion

            #region remove excessive points

            while (runningSum > count)
            {
                Tuple<int, double>[] fractions = sets.
                    Select((o, i) => Tuple.Create(i, o.Item2.Count.ToDouble() / runningSum.ToDouble())).
                    OrderByDescending(o => o.Item2).
                    ToArray();

                int fractionIndex = UtilityCore.GetIndexIntoList(rand.NextPow(1.5), fractions);     //nextPow will favor the front of the list, which is where the rings with the most points are
                int setIndex = fractions[fractionIndex].Item1;

                sets[setIndex].Item2.RemoveAt(UtilityCore.GetIndexIntoList(rand.NextDouble(), sets[setIndex].Item2.Count));

                runningSum--;
            }

            #endregion

            #region ensure enough for voronoi algorithm

            List<Point3D> retVal = new List<Point3D>();
            retVal.AddRange(sets.SelectMany(o => o.Item2));

            // The voronoi algrorithm fails if there aren't at least 5 points (really should fix that).  So add points that are
            // way away from the hull.  This will make the voronoi algorithm happy, and won't affect the local results
            if (count < MIN)
            {
                retVal.AddRange(
                    Enumerable.Range(0, MIN - count).
                    Select(o => Math3D.GetRandomVector_Spherical_Shell(aabbLen * 20).ToPoint())
                    );
            }

            #endregion

            return retVal.ToArray();
        }

        #endregion
        #region Private Methods - velocities

        private static Vector3D[] GetVelocities(HullVoronoiExploder_Shard[] hullShards, HullVoronoiExploder_ShotHit[] hits, double hullRadius, HullVoronoiExploder_Options options)
        {
            //TODO: When the asteroid shards aren't resmoothed, the velocities are very small
            //
            //Maybe add more orth velocity
            //
            //Maybe pull the hit source slightly into the asteroid --- done
            //
            //Maybe add some random velocity (size proportional to the velocity) --- done

            Vector3D[] retVal = CalculateVelocites(hullShards.Select(o => o.Center_ParentCoords).ToArray(), hits, hullRadius, options);

            if (options.RandomVelocityPercent != null)
            {
                retVal = retVal.
                    Select(o => o + Math3D.GetRandomVector_Spherical(o.Length * options.RandomVelocityPercent.Value)).
                    ToArray();
            }

            return retVal;
        }

        private static Vector3D[] CalculateVelocites(Point3D[] centers, HullVoronoiExploder_ShotHit[] hits, double hullRadius, HullVoronoiExploder_Options options)
        {
            Vector3D[] retVal = new Vector3D[centers.Length];

            foreach (var hit in hits)
            {
                Point3D shotStart = hit.Hit.Item1;
                if (options.InteriorVelocityCenterPercent != null)
                {
                    shotStart += ((hit.Hit.Item2 - hit.Hit.Item1) * options.InteriorVelocityCenterPercent.Value);
                }

                //Vector3D[] velocities = DistributeForces(centers, shotStart, hit.Shot.Item2 * (hit.Shot.Item3 * options.ShotMultiplier), hullRadius, options);
                Vector3D[] velocities = DistributeForces(centers, shotStart, hit.Shot.Item2 * hit.Shot.Item3, hullRadius, options);

                for (int cntr = 0; cntr < retVal.Length; cntr++)
                {
                    retVal[cntr] += velocities[cntr];
                }
            }

            return retVal;
        }

        /// <summary>
        /// This will figure how much velocity to apply to each bodyCenter
        /// TODO: Also calculate angular velocities
        /// </summary>
        /// <param name="bodyCenters">The bodies to apply velocity to</param>
        /// <param name="hitStart">The point of impact</param>
        /// <param name="hitDirection">The direction and force of the impact</param>
        /// <param name="mainBodyRadius">The avg radius of the body that is getting blown apart</param>
        private static Vector3D[] DistributeForces(Point3D[] bodyCenters, Point3D hitStart, Vector3D hitDirection, double mainBodyRadius, HullVoronoiExploder_Options options)
        {
            Vector3D hitDirectionUnit = hitDirection.ToUnit();
            double hitForceBase = hitDirection.Length / mainBodyRadius;

            var vectors = bodyCenters.
                Select(o =>
                {
                    Vector3D direction = o - hitStart;
                    Vector3D directionUnit = direction.ToUnit();

                    double distance = direction.Length;
                    double scaledDistance = distance;
                    if (options.DistanceDot_Power != null)
                    {
                        double linearDot = Math3D.GetLinearDotProduct(hitDirectionUnit, directionUnit);     // making it linear so the power function is more predictable

                        // Exaggerate the distance based on dot product.  That way, points in line with the hit will get more of the impact
                        double scale = (1 - Math.Abs(linearDot));
                        scale = Math.Pow(scale, options.DistanceDot_Power.Value);
                        scaledDistance = scale * distance;
                    }

                    return new
                    {
                        ForcePoint = o,
                        Distance = distance,
                        ScaledDistance = scaledDistance,
                        DirectionUnit = directionUnit,
                    };
                }).
                ToArray();

            double[] percents = GetPercentOfProjForce(vectors.Select(o => o.ScaledDistance).ToArray());

            Vector3D[] retVal = new Vector3D[vectors.Length];

            for (int cntr = 0; cntr < vectors.Length; cntr++)
            {
                retVal[cntr] = vectors[cntr].DirectionUnit * (hitForceBase * percents[cntr]);
            }

            return retVal;
        }

        /// <summary>
        /// Figures out how much of the force should go to each point.  The distances are distances from the point of impact.
        /// NOTE: This is basically: (1+baseConst)^-x
        /// </summary>
        /// <param name="baseConst">
        /// Any number from 0 to infinity.  Realistic values should be between .5 and 8.  There's nothing magical about using e as
        /// the constant, it just seems to have a good amount of curve
        /// </param>
        /// <remarks>
        /// This doesn't need to create super realistic percents.  If in doubt, use a larger base constant, which will give more of the force
        /// to the closer points.  The physics engine won't let close objects fly past farther, so just like racking pool balls, the outer will
        /// fly out.  But distributing the force a bit should help with jumpy behavior if the physics engine is under load
        /// 
        /// I don't know for sure, but I'm guessing the shape of the curve should be roughly 1/x^2.  This method isn't that, but should
        /// be similar enough.
        /// </remarks>
        private static double[] GetPercentOfProjForce(double[] distances, double baseConst = Math.E)
        {
            var avg_stdev = Math1D.Get_Average_StandardDeviation(distances);
            double scale = avg_stdev.Item2 / avg_stdev.Item1;
            scale *= baseConst;
            scale += 1;

            var deviations = distances.
                Select(o =>
                {
                    double distFromAvg = Math.Abs(o - avg_stdev.Item1);
                    double deviationsFromAvg = distFromAvg / avg_stdev.Item2;

                    if (o > avg_stdev.Item1)
                    {
                        deviationsFromAvg = -deviationsFromAvg;
                    }

                    //return Math.Pow(Math.E, deviationsFromAvg);
                    return Math.Pow(scale, deviationsFromAvg);
                }).
                ToArray();

            double sum = deviations.Sum();

            return deviations.
                Select(o => o / sum).
                ToArray();
        }

        #endregion
    }

    #region Class: HullVoronoiExploder_Options

    public class HullVoronoiExploder_Options
    {
        public bool ShouldSmoothShards = true;

        /// <summary>
        /// When calculating velocities for each shot, this will advance the hit start to:
        ///     hitstart + (hitDir * %)
        /// 
        /// This will cause the shards to fly out more than just forward (because the hit start will be more inside the hull)
        /// 
        /// WARNING: Be careful not to use too large of a percent, or the shards will fly backward
        /// </summary>
        public double? InteriorVelocityCenterPercent = .15;

        /// <summary>
        /// When calculating distance between the point of impact and each body center, that distance can be warped based on
        /// the dot product.  This will cause points in line with the impact to get more of the force
        /// 
        /// If this is populated, then the linear dot product is taken to the power.  Less than one will favor items with....idk, makes
        /// the effect a bit less pronounced.  Powers greater than one amplify the dot product's effect
        /// <summary>
        public double? DistanceDot_Power = .4;

        /// <summary>
        /// Takes calculated velocity + Math3D.GetRandomVector_Spherical(velocity.Length * %))
        /// </summary>
        public double? RandomVelocityPercent = .33;

        public int MinCount = 2;
        public int MaxCount = 33;
    }

    #endregion
    #region Class: HullVoronoiExploder_Response

    public class HullVoronoiExploder_Response
    {
        public HullVoronoiExploder_ShotHit[] Hits { get; set; }
        public Point3D[] ControlPoints { get; set; }
        public VoronoiResult3D Voronoi { get; set; }

        //NOTE: If there is an error, everything below will be null
        //public Tuple<int, ITriangleIndexed[]>[] Shards { get; set; }
        public HullVoronoiExploder_Shard[] Shards { get; set; }

        public Vector3D[] Velocities { get; set; }
    }

    #endregion
    #region Class: HullVoronoiExploder_Shard

    public class HullVoronoiExploder_Shard
    {
        public int VoronoiControlPointIndex { get; set; }
        public ITriangleIndexed[] Hull_ParentCoords { get; set; }
        public Point3D Center_ParentCoords { get; set; }

        // These properties would be used to make an actual IMapObject (Asteroid)
        public ITriangleIndexed[] Hull_Centered { get; set; }
        public double Radius { get; set; }
    }

    #endregion
    #region Class: HullVoronoiExploder_ShotHit

    /// <summary>
    /// This stores a shot and corresponding hull hit
    /// </summary>
    public class HullVoronoiExploder_ShotHit
    {
        /// <summary>
        /// Item1=From
        /// Item2=Direction (length is explosion velocity multiplier)
        /// Item3=% penetration (0 to 1)
        /// </summary>
        public Tuple<Point3D, Vector3D, double> Shot { get; set; }
        /// <summary>
        /// Item1=Point of impact
        /// Item2=Depth of impact (based on Shot.Item3)
        /// </summary>
        public Tuple<Point3D, Point3D> Hit { get; set; }
    }

    #endregion
}
