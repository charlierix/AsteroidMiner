using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.HelperClassesWPF.Controls3D;
using MIConvexHull;

namespace Game.Newt.Testers
{
    /// <summary>
    /// Interaction logic for NatureOfShapes.xaml
    /// </summary>
    public partial class NatureOfShapes : Window
    {
        #region class: VisualizationColors

        private class VisualizationColors
        {
            public Color Gray { get; set; }
            public Color LightGray { get; set; }
            public Color Negative { get; set; }
            public Color Zero { get; set; }
            public Color Positive { get; set; }
            public SpecularMaterial Specular = new SpecularMaterial(UtilityWPF.BrushFromHex("50AAAAAA"), 2);
        }

        #endregion
        #region class: EdgeLink

        private class EdgeLink
        {
            public EdgeLink(int index0, int index1, VectorND[] allPoints, double desiredLength)
            {
                Index0 = index0;
                Index1 = index1;
                AllPoints = allPoints;
                DesiredLength = desiredLength;
            }

            public readonly int Index0;
            public readonly int Index1;

            public readonly VectorND[] AllPoints;

            public readonly double DesiredLength;

            public readonly long Token = TokenGenerator.NextToken();

            public VectorND Point0 => AllPoints[Index0];
            public VectorND Point1 => AllPoints[Index1];
        }

        #endregion
        #region class: LatchJoint

        private class LatchJoint
        {
            public LatchJoint(int index0, int index1, VectorND[] allPoints, double maxForce, double distance_Latch, double distance_Force, bool isLatched, int randBondDimensions = 2)
            {
                Index0 = index0;
                Index1 = index1;

                AllPoints = allPoints;

                MaxForce = maxForce;
                Distance_Latch = distance_Latch;
                Distance_Force = distance_Force;

                IsLatched = isLatched;

                RandomBondPos = new VectorND(randBondDimensions);
            }

            public readonly int Index0;
            public readonly int Index1;

            public readonly VectorND[] AllPoints;

            public VectorND Point0 => AllPoints[Index0];
            public VectorND Point1 => AllPoints[Index1];

            //NOTE: Each point is pulled with a force from 0 to MaxForce.  So the latch's breaking force is MaxForce*2.  Otherwise the latch would
            //break too quickly
            public double MaxForce { get; set; }
            public double Distance_Latch { get; set; }
            public double Distance_Force { get; set; }

            public bool IsLatched { get; set; }

            public VectorND RandomBondPos { get; private set; }

            public int GetIndex(bool isZero)
            {
                return isZero ?
                    Index0 :
                    Index1;
            }
            public VectorND GetPoint(bool isZero)
            {
                return isZero ?
                    Point0 :
                    Point1;
            }

            public void AdjustRandomBond()
            {
                Random rand = StaticRandom.GetRandomForThread();

                double[] array = RandomBondPos.VectorArray ?? new double[RandomBondPos.Size];

                for (int cntr = 0; cntr < array.Length; cntr++)
                {
                    array[cntr] = AdjustBondAxis(array[cntr], rand);
                }

                RandomBondPos = new VectorND(array);
            }

            public static LatchJoint[][] GetLatchIslands(LatchJoint[] joints, bool onlyLatched)
            {
                List<LatchJoint[]> retVal = new List<LatchJoint[]>();

                // Find the latches that should be in the return
                List<LatchJoint> remaining = joints.
                    Where(o => !onlyLatched || o.IsLatched).
                    ToList();

                while (remaining.Count > 0)
                {
                    // Start with one latch
                    List<LatchJoint> set = new List<LatchJoint>();
                    set.Add(remaining[0]);
                    remaining.RemoveAt(0);

                    // Keep doing passes through remaining until all neighbors are found
                    LatchJoint[] currentSet = new[] { set[0] };
                    while (currentSet.Length > 0)
                    {
                        currentSet = FindConnectedLatches(remaining, currentSet);
                        set.AddRange(currentSet);
                    }

                    retVal.Add(set.ToArray());
                }

                return retVal.ToArray();
            }

            public static int[] GetIndices(LatchJoint[] set)
            {
                return set.Select(o => o.Index0).
                    Concat(set.Select(o => o.Index1)).
                    Distinct().
                    OrderBy().
                    ToArray();
            }

            public override string ToString()
            {
                return string.Format("{0} - {1} | {2} | {3}",
                    Index0,
                    Index1,
                    IsLatched ? "latched" : "open",
                    (Point1 - Point0).IsNearZero() ? MathND.GetCenter(new[] { Point0, Point1 }).ToStringSignificantDigits(3) : $"{Point0.ToStringSignificantDigits(3)} - {Point1.ToStringSignificantDigits(3)}");
            }

            #region Private Methods

            private static LatchJoint[] FindConnectedLatches(List<LatchJoint> remaining, LatchJoint[] set)
            {
                List<LatchJoint> retVal = new List<LatchJoint>();

                int index = 0;
                while (index < remaining.Count)
                {
                    if (set.Any(o =>
                         o.Index0 == remaining[index].Index0 ||
                         o.Index1 == remaining[index].Index0 ||
                         o.Index0 == remaining[index].Index1 ||
                         o.Index1 == remaining[index].Index1))
                    {
                        retVal.Add(remaining[index]);
                        remaining.RemoveAt(index);
                    }
                    else
                    {
                        index++;
                    }
                }

                return retVal.ToArray();
            }

            private static double AdjustBondAxis(double current, Random rand)
            {
                double delta = StaticRandom.NextDouble(-1, 1);

                if (Math.Abs(current + delta) > 100 && Math1D.IsSameSign(current, delta))
                {
                    delta *= .5;
                }

                return current + delta;
            }

            #endregion
        }

        #endregion

        #region class: UtilityNOS

        private static class UtilityNOS
        {
            #region class: AttachPoints

            private class AttachPoints
            {
                /// <summary>
                /// These are points that can be attached to
                /// </summary>
                public List<(LatchJoint joint, bool isZero)> Points { get; set; }

                public override string ToString()
                {
                    return Points.
                        Select(o => string.Format("{0} | {1}", o.joint, o.isZero ? "zero" : "one")).
                        ToJoin(" ~ ");
                }
            }

            #endregion

            public static Color GetDotColor(VectorND point, bool colorByLastDim, double radius, VisualizationColors colors)
            {
                if (colorByLastDim)
                {
                    double lastCoord = point[point.Size - 1];

                    Color max = lastCoord < 0 ?
                        colors.Negative :
                        colors.Positive;

                    return UtilityWPF.AlphaBlend(max, colors.Zero, Math.Abs(lastCoord) / radius);
                }
                else
                {
                    return colors.Gray;
                }
            }
            public static Color GetLinkColor(double length, double desiredLength, VisualizationColors colors, double multCompress = .5, double multExpand = 2)
            {
                double percent;
                if (length < desiredLength)
                {
                    percent = UtilityCore.GetScaledValue_Capped(0, 1, desiredLength, desiredLength * multCompress, length);        // 50% compression is full red
                    return UtilityWPF.AlphaBlend(colors.Negative, colors.Zero, percent);
                }
                else
                {
                    percent = UtilityCore.GetScaledValue_Capped(0, 1, desiredLength, desiredLength * multExpand, length);        // 200% expansion is full blue
                    return UtilityWPF.AlphaBlend(colors.Positive, colors.Zero, percent);
                }
            }

            public static bool IsBottom(VectorND point)
            {
                return point[point.Size - 1] < 0;
            }

            public static (bool isInside, VectorND forceToward0) GetLatchForce(VectorND point0, VectorND point1, double radiusInner, double radiusOuter, double forceAtInner)
            {
                VectorND direction = point1 - point0;
                double distance = direction.LengthSquared;

                if (distance > radiusOuter * radiusOuter)
                {
                    return (false, new VectorND(point0.Size));
                }
                else if (distance <= radiusInner * radiusInner)
                {
                    return (true, direction.ToUnit() * forceAtInner);
                }

                //double force = (forceAtInner / 2) * (1 + Math.Cos((Math.PI * Math.Sqrt(distance)) / (radiusOuter - radiusInner)));        // cosine is too weak near the outer radius, and is expensive without any real benefit
                double force = UtilityCore.GetScaledValue(forceAtInner, 0, radiusInner, radiusOuter, Math.Sqrt(distance));

                return (false, direction.ToUnit() * force);
            }

            public static double[] UpdateLinkForces(VectorND[] forces, EdgeLink[] links, VectorND[] points, double multCompress, double multExpand)
            {
                // This was copied from MathND.EvenDistribution.MoveStep() (in Private Methods - linked open)

                List<double> linkLengths = new List<double>();

                foreach (EdgeLink link in links)
                {
                    VectorND line = points[link.Index1] - points[link.Index0];
                    double length = line.Length;
                    linkLengths.Add(length);

                    // Less than 1 is repulsive, Greater than 1 is attractive
                    double force = (link.DesiredLength - length) / link.DesiredLength;

                    double mult = length < link.DesiredLength ?
                        multCompress :
                        multExpand;

                    VectorND forceVect = (line / length) * (force * mult);
                    if (forceVect.IsInvalid())
                    {
                        continue;
                    }

                    forces[link.Index0] -= forceVect;
                    forces[link.Index1] += forceVect;
                }

                return linkLengths.ToArray();
            }

            public static void UpdateLatchForces(VectorND[] forces, EdgeLink[][] links, LatchJoint[][] joints)
            {
                var latchForces = new List<(LatchJoint joint, VectorND forceToward0)>();

                foreach (LatchJoint joint in joints.SelectMany(o => o))
                {
                    //TODO: Linear vs Cosine
                    var force = GetLatchForce(joint.Point0, joint.Point1, joint.Distance_Latch, joint.Distance_Force, joint.MaxForce);

                    joint.IsLatched = false;

                    if (force.isInside)
                    {
                        joint.IsLatched = ShouldBeLatched(forces, joint);
                    }

                    if (!joint.IsLatched)
                    {
                        // Can't add to forces yet, or it will interfere with other latch joint calculations
                        latchForces.Add((joint, force.forceToward0));
                    }
                }

                foreach (var latchForce in latchForces)
                {
                    forces[latchForce.joint.Index0] += latchForce.forceToward0;
                    forces[latchForce.joint.Index1] -= latchForce.forceToward0;
                }
            }

            /// <summary>
            /// This updates the positions by force*mult*elapsed
            /// (caps forces first)
            /// </summary>
            public static void ApplyForces(VectorND[] points, VectorND[] forces, double elapsedSeconds, double mult, double maxForce)
            {
                mult *= elapsedSeconds;

                double maxForceSqr = maxForce * maxForce;

                for (int cntr = 0; cntr < points.Length; cntr++)
                {
                    var result = ApplyForce(points[cntr], forces[cntr], mult, maxForce, maxForceSqr);
                    points[cntr] = result.position;
                    forces[cntr] = result.force;
                }
            }
            /// <summary>
            /// This overload considers latches
            /// </summary>
            public static void ApplyForces(VectorND[] points, VectorND[] forces, double elapsedSeconds, double mult, double maxForce, LatchJoint[][] joints)
            {
                mult *= Math.Min(elapsedSeconds, .1);

                double maxForceSqr = maxForce * maxForce;

                List<int> unlatched = new List<int>(Enumerable.Range(0, points.Length));

                foreach (LatchJoint[] set in joints)
                {
                    ApplyForces_LatchSet(set, unlatched, points, forces, mult, maxForce, maxForceSqr);
                }

                foreach (int index in unlatched)
                {
                    //TODO: May want to borrow a trick from the chase forces that dampens forces orhogonal to the direction toward the other point.
                    //This reduces the chance of the points orbiting each other when they get really close together and forces get high

                    var result = ApplyForce(points[index], forces[index], mult, maxForce, maxForceSqr);
                    points[index] = result.position;
                    forces[index] = result.force;
                }
            }

            public static double GetAlphaPercent(VectorND position, double radius, double slicePlane)
            {
                //const double LIMITPERCENT = .05;
                const double LIMITPERCENT = .1;

                double lastCoord = position[position.Size - 1];

                double distance = Math.Abs(slicePlane - lastCoord);

                if (distance > radius * LIMITPERCENT)
                {
                    return 0;
                }

                return UtilityCore.GetScaledValue(0, 1, radius * LIMITPERCENT, 0, distance);
            }

            public static (int, int)[][] GetDelaunay(int dimensions, VectorND[] points)
            {
                switch (dimensions)
                {
                    case 2:
                        TriangleIndexed[] triangles = Math2D.GetDelaunayTriangulation(points.Select(o => o.ToPoint()).ToArray(), points.Select(o => o.ToPoint3D(false)).ToArray());
                        return triangles.
                            Select(o => new[]
                            {
                                (o.Index0, o.Index1),
                                (o.Index1, o.Index2),
                                (o.Index2, o.Index0),
                            }).
                            ToArray();

                    case 3:
                        Tetrahedron[] tetras = Math3D.GetDelaunay(points.Select(o => o.ToPoint3D()).ToArray(), 3);
                        return tetras.
                            Select(o => o.EdgeArray.Select(p => (p.Item1, p.Item2)).ToArray()).
                            ToArray();

                    case 4:
                        //TODO: finish this
                        //MIConvexHull.DelaunayTriangulation
                        throw new ApplicationException("finish 4D delaunay");
                        break;

                    default:
                        throw new ApplicationException($"Unexpected dimensions {dimensions}");
                }
            }
            public static ((int, int)[][] delaunay, VectorND[] points, LatchJoint[][] latches) GetLatches((int, int)[][] delaunay, VectorND[] points, double maxForce, double distance_Latch, double distance_Force)
            {
                (int, int)[][] newDel = delaunay.
                    Select(o => o.ToArray()).
                    ToArray();

                List<VectorND> newPts = new List<VectorND>(points);

                var latches = new List<(int, int)[]>();

                for (int cntr = 0; cntr < points.Length; cntr++)
                {
                    // Find all the edges that touch this point
                    var latchesForPoint = GetLatches_Point(cntr, newPts, newDel);

                    if (latchesForPoint?.Length > 0)
                    {
                        latches.Add(latchesForPoint);
                    }
                }

                VectorND[] newPoints = newPts.
                    ToArray();

                LatchJoint[][] latchJoints = latches.
                    Select(o => o.Select(p => new LatchJoint(p.Item1, p.Item2, newPoints, maxForce, distance_Latch, distance_Force, true)).ToArray()).
                    ToArray();

                return (newDel, newPoints, latchJoints);
            }

            #region Private Methods

            private static (VectorND position, VectorND force) ApplyForce(VectorND position, VectorND force, double mult, double maxForce, double maxForceSqr)
            {
                force *= mult;

                // Cap the force to avoid instability
                if (force.LengthSquared > maxForceSqr)
                {
                    force = force.ToUnit(false) * maxForce;
                }

                return (position + force, force);
            }

            private static (int, int)[] GetLatches_Point(int pointIndex, List<VectorND> points, (int, int)[][] delaunay)
            {
                List<int> pointsNamedSue = new List<int>();

                for (int cntr = 0; cntr < delaunay.Length; cntr++)
                {
                    if (pointsNamedSue.Count == 0 && delaunay[cntr].Any(o => o.Item1 == pointIndex || o.Item2 == pointIndex))
                    {
                        // This is the first poly to see this point.  Make a note of that and move to the next poly
                        pointsNamedSue.Add(pointIndex);
                        continue;
                    }

                    int? newPoint = GetLatches_Point_AddPoint(pointIndex, cntr, points, delaunay);
                    if (newPoint != null)
                    {
                        pointsNamedSue.Add(newPoint.Value);
                    }
                }

                // Create pairs that link all these versions of the same point together.  These will become latch joints
                // once all the new points are created
                return UtilityCore.GetPairs(pointsNamedSue.Count).
                    Select(o => (pointsNamedSue[o.Item1], pointsNamedSue[o.Item2])).
                    ToArray();
            }
            private static int? GetLatches_Point_AddPoint(int pointIndex, int polyIndex, List<VectorND> points, (int, int)[][] delaunay)
            {
                int? retVal = null;

                var getPointIndex = new Func<int>(() =>
                {
                    if (retVal == null)
                    {
                        points.Add(points[pointIndex].Clone());
                        retVal = points.Count - 1;
                    }

                    return retVal.Value;
                });

                for (int cntr = 0; cntr < delaunay[polyIndex].Length; cntr++)
                {
                    if (delaunay[polyIndex][cntr].Item1 == pointIndex)
                    {
                        // This references the point.  Create a new edge that references the new point
                        delaunay[polyIndex][cntr] = (getPointIndex(), delaunay[polyIndex][cntr].Item2);
                    }
                    else if (delaunay[polyIndex][cntr].Item2 == pointIndex)
                    {
                        // Same, but at Item2
                        delaunay[polyIndex][cntr] = (delaunay[polyIndex][cntr].Item1, getPointIndex());
                    }
                }

                return retVal;
            }

            private static bool ShouldBeLatched(VectorND[] forces, LatchJoint joint)
            {
                VectorND v1onto2 = forces[joint.Index0].GetProjectedVector(forces[joint.Index1]);
                VectorND v2onto1 = forces[joint.Index1].GetProjectedVector(forces[joint.Index0]);

                double lenAlongs = (v2onto1 - v1onto2).LengthSquared;

                if (lenAlongs <= (joint.MaxForce * 2) * (joint.MaxForce * 2))
                {
                    // Not enough force: latch them
                    return true;
                }
                else
                {
                    // Too much force: break the latch
                    return false;
                }
            }

            private static void ApplyForces_LatchSet(LatchJoint[] set, List<int> unlatched, VectorND[] points, VectorND[] forces, double mult, double maxForce, double maxForceSqr)
            {
                // Set is a list of all the joints tied to a single point.  This only deals with the joints that are currently in a latched state
                if (set.All(o => !o.IsLatched))
                {
                    // All unlatched, nothing nothing special to do
                    return;
                }
                else if (set.All(o => o.IsLatched))
                {
                    ApplyForces_LatchSet_AllLatched(set, unlatched, points, forces, mult, maxForce, maxForceSqr);
                    return;
                }

                var islands = GetSetIslandsByPosition(set);

                foreach (var subset in islands.islands)
                {
                    ApplyForces_LatchSet_Subset(subset.joints, subset.center, islands.removed, unlatched, points, forces, mult, maxForce, maxForceSqr);
                }
            }

            private static void ApplyForces_LatchSet_AllLatched(LatchJoint[] set, List<int> unlatched, VectorND[] points, VectorND[] forces, double mult, double maxForce, double maxForceSqr)
            {
                int[] indices = LatchJoint.GetIndices(set);

                foreach (int index in indices)
                {
                    unlatched.Remove(index);
                }

                // Treat all the points in indicies like a single point
                //NOTE: Latch force was never applied to these points (it's considered a hidden internal force that makes them all become a single point).  So forces[] is only external forces and needs to be added up
                VectorND position = MathND.GetCenter(indices.Select(o => points[o]));
                VectorND force = MathND.GetSum(indices.Select(o => forces[o]));

                var result = ApplyForce(position, force, mult, maxForce, maxForceSqr);
                foreach (int index in indices)
                {
                    points[index] = result.position;
                    forces[index] = result.force;
                }
            }

            private static void ApplyForces_LatchSet_Subset(LatchJoint[] subset, VectorND center, LatchJoint[] partials, List<int> unlatched, VectorND[] points, VectorND[] forces, double mult, double maxForce, double maxForceSqr)
            {
                // Figure out which points should be together, and which should be apart

                // The latches that are open need to be allowed to be free from each other.  The remaining latched joints need to stick
                // to the closest free point

                // Use subset[9].RandomBond to see which to use



                //TODO: This is now working, rework the comments to go in the function's remarks
                //      ex: { 2-26 open }, { 2-25 closed } { 25-26 closed }
                //      2 was an attach point
                //      26 was an attach point
                //      
                //      the first attach point picked up all three indices (shouldn't have picked up 26)
                //      so when 26 ran in the lower loop (subsubset), it overwrote points[26] and forces[26] that was calculated from the previous iteration (the previous subsubset shouldn't have contained 26)



                // Get the points of open latches
                var attachPoints = ApplyForces_LatchSet_Subset_AttachPoints(subset, center, partials);
                if (attachPoints.attachPoints.Length == 0)
                {
                    return;
                }

                List<LatchJoint> remainingLatched = new List<LatchJoint>(attachPoints.remainingLatched);

                int infiniteLoop = 0;

                while (remainingLatched.Count > 0)
                {
                    int index = 0;
                    while (index < remainingLatched.Count)
                    {
                        // Try to bind this to the best attach point
                        if (ApplyForces_LatchSet_Subset_Bind(attachPoints.attachPoints, remainingLatched[index]))
                        {
                            remainingLatched.RemoveAt(index);
                        }
                        else
                        {
                            index++;
                        }
                    }

                    infiniteLoop++;
                    if (infiniteLoop > 100)
                    {
                        // When I added this check, attachPoints.attachPoints was empty and remainingLatched had one left.  Need to research more carefully
                        //throw new ApplicationException("Inifinite loop detected");
                        return;
                    }
                }

                // At this point, the latches have been divided among the attach points.
                //  Treat each attach point as an independent point.  Apply the latching logic to each
                foreach (var subsubset in attachPoints.attachPoints)
                {
                    ApplyForces_LatchSet_Subset_Subset(subsubset, unlatched, points, forces, mult, maxForce, maxForceSqr);
                }
            }
            private static bool ApplyForces_LatchSet_Subset_Bind(AttachPoints[] attachPoints, LatchJoint joint)
            {
                var bestMatch = attachPoints.
                    Select(o => new
                    {
                        attachPoint = o,
                        match = FindBestMatchingLatch(joint, o),
                    }).
                    Where(o => o.match != null).
                    OrderBy(o => o.match.Value.distanceSqr).
                    FirstOrDefault();

                if (bestMatch == null)
                {
                    return false;
                }

                // The joint passed in should always have both points as the same (should be in a latched state).  So add both points
                // as attachable
                if (!ContainsIndex(attachPoints, joint.Index0))
                {
                    bestMatch.attachPoint.Points.Add((joint, true));
                }

                if (!ContainsIndex(attachPoints, joint.Index1))
                {
                    bestMatch.attachPoint.Points.Add((joint, false));
                }

                return true;
            }
            private static void ApplyForces_LatchSet_Subset_Subset(AttachPoints set, List<int> unlatched, VectorND[] points, VectorND[] forces, double mult, double maxForce, double maxForceSqr)
            {
                int[] indices = set.Points.
                    Select(o => o.joint.GetIndex(o.isZero)).
                    Distinct().
                    ToArray();

                foreach (int index in indices)
                {
                    unlatched.Remove(index);
                }

                // Treat all the points in indicies like a single point
                //NOTE: Latch force was never applied to these points (it's considered a hidden internal force that makes them all become a single point).  So forces[] is only external forces and needs to be added up
                VectorND position = MathND.GetCenter(indices.Select(o => points[o]));
                VectorND force = MathND.GetSum(indices.Select(o => forces[o]));

                var result = ApplyForce(position, force, mult, maxForce, maxForceSqr);
                foreach (int index in indices)
                {
                    points[index] = result.position;
                    forces[index] = result.force;
                }
            }

            /// <summary>
            /// Finds latches that are open and returns each endpoint of those latches that are close enough to center.
            /// All the other closed latches are returned in remainingLatched
            /// </summary>
            /// <returns>
            /// attachPoints: Each instance is one point (so an open latch would have two instances added - one for each endpoint)
            /// remainingLatched: The other closed latches
            /// </returns>
            private static (AttachPoints[] attachPoints, LatchJoint[] remainingLatched) ApplyForces_LatchSet_Subset_AttachPoints(LatchJoint[] subset, VectorND center, LatchJoint[] partials)
            {
                // Find the attach points
                var attachPoints = new List<(LatchJoint joint, bool isZero)>();
                var remainingLatched = new List<LatchJoint>();

                foreach (LatchJoint joint in subset)
                {
                    if (joint.IsLatched)
                    {
                        remainingLatched.Add(joint);
                    }
                    else
                    {
                        attachPoints.Add((joint, true));
                        attachPoints.Add((joint, false));
                    }
                }

                foreach (LatchJoint joint in partials)
                {
                    //if(joint.IsLatched)       // they will probably always be unlatched if they are in this list.  But that's not a hard enough requirement to put a constrain around (it won't hurt anything to bind to latched points)
                    //{
                    //    continue;
                    //}

                    if ((joint.Point0 - center).LengthSquared < joint.Distance_Latch * joint.Distance_Latch)
                    {
                        attachPoints.Add((joint, true));
                    }
                    else if ((joint.Point1 - center).LengthSquared < joint.Distance_Latch * joint.Distance_Latch)
                    {
                        attachPoints.Add((joint, false));
                    }
                }

                return
                (
                    attachPoints.
                        Select(o => new AttachPoints()
                        {
                            Points = new List<(LatchJoint joint, bool isZero)>(new[] { o }),
                        }).
                        ToArray(),

                    remainingLatched.
                        ToArray()
                );
            }

            /// <summary>
            /// This divides the set based on physical position.  Subsets that are more than inner distance away from each other are put
            /// in separate groups
            /// </summary>
            /// <remarks>
            /// The set passed in should all belong to the same initial point.  Over time, some versions of that point could break apart
            /// and there could be several latched subsets that are located
            /// 
            /// If there is more than one island, then latches connecting those islands should be in an unlatched state.  Those unlatched
            /// bridges need to be thrown out from the sub islands, but forces still need to be applied
            /// </remarks>
            private static ((LatchJoint[] joints, VectorND center)[] islands, LatchJoint[] removed) GetSetIslandsByPosition(LatchJoint[] set)
            {
                // Remove individual joints whose points are too far from each other
                var removed = new List<LatchJoint>();
                var remaining = new List<LatchJoint>();

                foreach (LatchJoint joint in set)
                {
                    if ((joint.Point0 - joint.Point1).LengthSquared <= joint.Distance_Latch * joint.Distance_Latch)
                    {
                        remaining.Add(joint);
                    }
                    else
                    {
                        removed.Add(joint);
                    }
                }

                var retVal = new List<(LatchJoint[], VectorND)>();

                while (remaining.Count > 0)
                {
                    List<LatchJoint> island = new List<LatchJoint>();

                    island.Add(remaining[0]);
                    remaining.RemoveAt(0);

                    VectorND[] pts0 = new[] { island[0].Point0, island[0].Point1 };

                    List<VectorND> matchedPoints = new List<VectorND>();
                    matchedPoints.AddRange(pts0);

                    int index = 0;
                    while (index < remaining.Count)
                    {
                        // Latch distance should be the same for all latches, but just in case it's not, take the min
                        double latchDist = Math.Min(island[0].Distance_Latch, remaining[index].Distance_Latch);
                        latchDist *= latchDist;

                        VectorND[] pts1 = new[] { remaining[index].Point0, remaining[index].Point1 };

                        var matches = UtilityCore.Collate(pts0, pts1).
                            Where(o => (o.Item1 - o.Item2).LengthSquared <= latchDist).
                            ToArray();

                        if (matches.Length > 0)
                        {
                            matchedPoints.AddRange(matches.SelectMany(o => new[] { o.Item1, o.Item2 }));

                            island.Add(remaining[index]);
                            remaining.RemoveAt(index);
                        }
                        else
                        {
                            index++;
                        }
                    }

                    retVal.Add((island.ToArray(), MathND.GetCenter(matchedPoints)));
                }

                return (retVal.ToArray(), removed.ToArray());
            }

            private static bool ContainsIndex(AttachPoints[] attachPoints, int index)
            {
                foreach (AttachPoints set in attachPoints)
                {
                    if (set.Points.Any(o => o.joint.GetIndex(o.isZero) == index))
                    {
                        return true;
                    }
                }

                return false;
            }

            private static (LatchJoint joint, bool isZero, double distanceSqr)? FindBestMatchingLatch(LatchJoint joint, AttachPoints candidates)
            {
                // Zero
                int index = joint.GetIndex(true);

                var matches0 = candidates.Points.
                    Where(o => o.joint.GetIndex(o.isZero) == index).
                    ToArray();

                // One
                index = joint.GetIndex(false);

                var matches1 = candidates.Points.
                    Where(o => o.joint.GetIndex(o.isZero) == index).
                    ToArray();

                if (matches0.Length == 0 && matches1.Length == 0)
                {
                    // For some reason, the below linq statement is generating a tuple that contains default values: (null, false, 0).  So adding
                    // an explicit if statement
                    return null;
                }

                // Return the one that is closest to the joint
                return matches0.
                    Concat(matches1).
                    Select(o => (o.joint, o.isZero, (o.joint.RandomBondPos - joint.RandomBondPos).LengthSquared)).
                    OrderBy(o => o.LengthSquared).
                    FirstOrDefault();
            }

            #endregion
        }

        #endregion

        #region interface: ITimerVisualization

        private interface ITimerVisualization : IDisposable
        {
            void Tick(double elapsedSeconds);
        }

        #endregion
        #region class: VisualizeUniformPoints

        private class VisualizeUniformPoints : ITimerVisualization
        {
            #region class: Dot

            private class Dot
            {
                public Dot(bool isStatic, VectorND position, double repulseMultiplier, TranslateTransform3D translate, SolidColorBrush brush)
                {
                    IsStatic = isStatic;
                    Position = position;
                    RepulseMultiplier = repulseMultiplier;
                    Translate = translate;
                    Brush = brush;
                }

                public readonly bool IsStatic;
                public VectorND Position;
                public readonly double RepulseMultiplier;
                public readonly TranslateTransform3D Translate;
                public readonly SolidColorBrush Brush;
            }

            #endregion

            #region Declaration Section

            private const double RADIUS = 10;
            private readonly double _movePercent;

            private readonly VisualizationColors _colors;

            private readonly int _dimensions;
            private readonly Viewport3D _viewport;

            private readonly Dot[] _dots;

            private Visual3D _visual = null;

            #endregion

            #region Constructor

            public VisualizeUniformPoints(int dimensions, Viewport3D viewport, VisualizationColors colors)
            {
                _colors = colors;

                int count;
                switch (dimensions)
                {
                    case 1:
                        count = 4;
                        _movePercent = .001;
                        break;

                    case 2:
                        count = 40;
                        _movePercent = .01;
                        break;

                    case 3:
                        count = 150;
                        _movePercent = .1;
                        break;

                    default:
                        count = 300;
                        _movePercent = .1;
                        break;
                }

                _dimensions = dimensions;
                _viewport = viewport;
                VectorND[] positions = MathND.GetRandomVectors_Spherical_Shell(dimensions, RADIUS, count);

                var visuals = GetVisuals(positions);

                _dots = Enumerable.Range(0, count).
                    Select(o => new Dot(false, positions[o], 1, visuals.translates[o], visuals.brushes[o])).
                    ToArray();

                _visual = new ModelVisual3D()
                {
                    Content = visuals.modelGroup,
                };

                MoveVisuals(_dots, ShowIn1FewerDim, IgnoreBottom);
                ColorVisuals(_dots, ColorByLastDim, IgnoreBottom, RADIUS, _colors);

                _viewport.Children.Add(_visual);
            }

            #endregion

            public bool ShowIn1FewerDim { get; set; }
            public bool ColorByLastDim { get; set; }
            public bool IgnoreBottom { get; set; }

            public void Tick(double elapsedSeconds)
            {
                VectorND[] forces = Enumerable.Range(0, _dots.Length).
                    Select(o => new VectorND(_dimensions)).
                    ToArray();

                GetRepulsionForces(forces, _dots, 1);

                MovePoints(_dots, forces, RADIUS, _movePercent);

                MoveVisuals(_dots, ShowIn1FewerDim, IgnoreBottom);

                ColorVisuals(_dots, ColorByLastDim, IgnoreBottom, RADIUS, _colors);
            }

            public void Dispose()
            {
                if (_visual != null)
                {
                    _viewport.Children.Remove(_visual);
                    _visual = null;
                }
            }

            #region Private Methods

            private static (Model3DGroup modelGroup, TranslateTransform3D[] translates, SolidColorBrush[] brushes) GetVisuals(VectorND[] positions)
            {
                TranslateTransform3D[] translates = new TranslateTransform3D[positions.Length];
                SolidColorBrush[] brushes = new SolidColorBrush[positions.Length];

                Model3DGroup modelGroup = new Model3DGroup();

                MeshGeometry3D dotGeometry = UtilityWPF.GetSphere_Ico(.15, 1, true);

                for (int cntr = 0; cntr < positions.Length; cntr++)
                {
                    MaterialGroup material;
                    material = new MaterialGroup();

                    brushes[cntr] = new SolidColorBrush(Colors.Black);
                    material.Children.Add(new DiffuseMaterial(brushes[cntr]));
                    material.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("50AAAAAA")), 2));

                    translates[cntr] = new TranslateTransform3D();

                    modelGroup.Children.Add(new GeometryModel3D()
                    {
                        Material = material,
                        BackMaterial = material,
                        Geometry = dotGeometry,
                        Transform = translates[cntr],
                    });
                }

                return (modelGroup, translates, brushes);
            }

            // This is a copy of Math3D.EvenDistribution.GetRepulsionForces_Continuous - not sure what I meant by continuous
            private static double GetRepulsionForces(VectorND[] forces, Dot[] dots, double maxDist)
            {
                const double STRENGTH = 1d;

                // This returns the smallest distance between any two nodes
                double retVal = double.MaxValue;

                for (int outer = 0; outer < dots.Length - 1; outer++)
                {
                    for (int inner = outer + 1; inner < dots.Length; inner++)
                    {
                        VectorND link = dots[outer].Position - dots[inner].Position;

                        double linkLength = link.Length;
                        if (linkLength < retVal)
                        {
                            retVal = linkLength;
                        }

                        double force = STRENGTH * (maxDist / linkLength);
                        if (force.IsInvalid())
                        {
                            force = .0001d;
                        }

                        link = link.ToUnit() * force;

                        if (!dots[inner].IsStatic)
                        {
                            forces[inner] -= link;
                        }

                        if (!dots[outer].IsStatic)
                        {
                            forces[outer] += link;
                        }
                    }
                }

                return retVal;
            }

            // This is a copy of Math3D.EvenDistribution.MovePoints_Sphere
            private static void MovePoints(Dot[] dots, VectorND[] forces, double radius, double movePercent)
            {
                for (int cntr = 0; cntr < dots.Length; cntr++)		// only need to iterate up to returnCount.  All the rest in dots are immoble ones
                {
                    if (dots[cntr].IsStatic)
                    {
                        continue;
                    }

                    // Move point
                    dots[cntr].Position += forces[cntr] * movePercent;

                    // Force the dot onto the surface of the sphere
                    dots[cntr].Position = dots[cntr].Position.ToUnit() * radius;
                }
            }

            private static void MoveVisuals(Dot[] dots, bool showIn1FewerDim, bool ignoreBottom)
            {
                for (int cntr = 0; cntr < dots.Length; cntr++)
                {
                    //TODO: Have a function that maps the VectorND into 3D.  The current way is simply only taking the first 3 coords, but it might be more
                    //illuminating to allow custom rotations (especially if in vr and those rotations are mapped to the head's position/orientation)
                    Vector3D position;
                    if (showIn1FewerDim)
                    {
                        switch (dots[cntr].Position.Size)
                        {
                            case 2:
                                position = new Vector3D(dots[cntr].Position[0], 0, 0);
                                break;

                            case 3:
                                position = dots[cntr].Position.ToVector(false).ToVector3D();
                                break;

                            default:
                                position = dots[cntr].Position.ToVector3D(false);
                                break;
                        }
                    }
                    else
                    {
                        position = dots[cntr].Position.ToVector3D(false);
                    }

                    if (ignoreBottom && UtilityNOS.IsBottom(dots[cntr].Position))
                    {
                        position.Z = -100;
                    }

                    dots[cntr].Translate.OffsetX = position.X;
                    dots[cntr].Translate.OffsetY = position.Y;
                    dots[cntr].Translate.OffsetZ = position.Z;
                }
            }

            private static void ColorVisuals(Dot[] dots, bool colorByLastDim, bool ignoreBottom, double radius, VisualizationColors colors)
            {
                foreach (Dot dot in dots)
                {
                    if (ignoreBottom && UtilityNOS.IsBottom(dot.Position))
                    {
                        dot.Brush.Color = Colors.Transparent;
                    }
                    else
                    {
                        dot.Brush.Color = UtilityNOS.GetDotColor(dot.Position, colorByLastDim, radius, colors);
                    }
                }
            }

            #endregion
        }

        #endregion
        #region class: VisualizeTessellation

        private class VisualizeTessellation : ITimerVisualization
        {
            #region class: Dot

            private class Dot
            {
                public Dot(bool isStatic, VectorND position, double repulseMultiplier, TranslateTransform3D translate, SolidColorBrush brush)
                {
                    IsStatic = isStatic;
                    Position = position;
                    RepulseMultiplier = repulseMultiplier;
                    Translate = translate;
                    Brush = brush;
                }

                public readonly bool IsStatic;
                public VectorND Position;
                public readonly double RepulseMultiplier;
                public readonly TranslateTransform3D Translate;
                public readonly SolidColorBrush Brush;
            }

            #endregion

            #region Declaration Section

            private const double RADIUS = 10;
            private const double DOT = .15;
            private const double LINE = .02;
            private readonly double _movePercent;		// 10% seems to give good results

            private readonly VisualizationColors _colors;

            private readonly int _dimensions;
            private readonly Viewport3D _viewport;

            private readonly Dot[] _dots;

            private Visual3D _visual = null;
            private Visual3D _lines = null;

            #endregion

            #region Constructor

            public VisualizeTessellation(int dimensions, Viewport3D viewport, VisualizationColors colors)
            {
                _colors = colors;

                int count;
                switch (dimensions)
                {
                    case 1:
                        count = 4;
                        _movePercent = .001;
                        break;

                    case 2:
                        count = 20;
                        _movePercent = .01;
                        break;

                    case 3:
                        count = 80;
                        _movePercent = .1;
                        break;

                    default:
                        count = 100;
                        _movePercent = .1;
                        break;
                }

                _dimensions = dimensions;
                _viewport = viewport;
                VectorND[] positions = MathND.GetRandomVectors_Spherical_Shell(dimensions, RADIUS, count);

                var visuals = GetVisuals(positions);

                _dots = Enumerable.Range(0, count).
                    Select(o => new Dot(false, positions[o], 1, visuals.translates[o], visuals.brushes[o])).
                    ToArray();

                _visual = new ModelVisual3D()
                {
                    Content = visuals.modelGroup,
                };

                MoveVisuals(_dots, IgnoreBottom);
                ColorVisuals(_dots, ColorByLastDim, IgnoreBottom, RADIUS, _colors);

                _viewport.Children.Add(_visual);
            }

            #endregion

            public bool ColorByLastDim { get; set; }
            public bool IgnoreBottom { get; set; }

            public void Tick(double elapsedSeconds)
            {
                VectorND[] forces = Enumerable.Range(0, _dots.Length).
                    Select(o => new VectorND(_dimensions)).
                    ToArray();

                var links = GetRepulsionForces(forces, _dots, 1);

                MovePoints(_dots, forces, RADIUS, _movePercent);

                Visual3D lines = GetLines(_dots, ColorByLastDim, IgnoreBottom, RADIUS, _colors);

                MoveVisuals(_dots, IgnoreBottom);

                if (_lines != null)
                {
                    _viewport.Children.Remove(_lines);
                }

                _lines = lines;
                _viewport.Children.Add(_lines);

                ColorVisuals(_dots, ColorByLastDim, IgnoreBottom, RADIUS, _colors);
            }

            public void Dispose()
            {
                if (_lines != null)
                {
                    _viewport.Children.Remove(_lines);
                    _lines = null;
                }

                if (_visual != null)
                {
                    _viewport.Children.Remove(_visual);
                    _visual = null;
                }
            }

            #region Private Methods

            private static (Model3DGroup modelGroup, TranslateTransform3D[] translates, SolidColorBrush[] brushes) GetVisuals(VectorND[] positions)
            {
                TranslateTransform3D[] translates = new TranslateTransform3D[positions.Length];
                SolidColorBrush[] brushes = new SolidColorBrush[positions.Length];

                Model3DGroup modelGroup = new Model3DGroup();

                MeshGeometry3D dotGeometry = UtilityWPF.GetSphere_Ico(DOT, 1, true);

                for (int cntr = 0; cntr < positions.Length; cntr++)
                {
                    MaterialGroup material;
                    material = new MaterialGroup();

                    brushes[cntr] = new SolidColorBrush(Colors.Black);
                    material.Children.Add(new DiffuseMaterial(brushes[cntr]));
                    material.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("50AAAAAA")), 2));

                    translates[cntr] = new TranslateTransform3D();

                    modelGroup.Children.Add(new GeometryModel3D()
                    {
                        Material = material,
                        BackMaterial = material,
                        Geometry = dotGeometry,
                        Transform = translates[cntr],
                    });
                }

                return (modelGroup, translates, brushes);
            }

            // This is a copy of Math3D.EvenDistribution.GetRepulsionForces_Continuous - not sure what I meant by continuous
            private static (double shortest, (int, int, VectorND, double)[] links) GetRepulsionForces(VectorND[] forces, Dot[] dots, double maxDist)
            {
                const double STRENGTH = 1d;

                // This returns the smallest distance between any two nodes
                double shortest = double.MaxValue;
                var links = new List<(int, int, VectorND, double)>();

                for (int outer = 0; outer < dots.Length - 1; outer++)
                {
                    for (int inner = outer + 1; inner < dots.Length; inner++)
                    {
                        VectorND link = dots[outer].Position - dots[inner].Position;

                        double linkLength = link.Length;
                        if (linkLength < shortest)
                        {
                            shortest = linkLength;
                        }

                        links.Add((inner, outer, link, linkLength));

                        double force = STRENGTH * (maxDist / linkLength);
                        if (force.IsInvalid())
                        {
                            force = .0001d;
                        }

                        link = link.ToUnit() * force;

                        if (!dots[inner].IsStatic)
                        {
                            forces[inner] -= link;
                        }

                        if (!dots[outer].IsStatic)
                        {
                            forces[outer] += link;
                        }
                    }
                }

                return (shortest, links.ToArray());
            }

            // This is a copy of Math3D.EvenDistribution.MovePoints_Sphere
            private static void MovePoints(Dot[] dots, VectorND[] forces, double radius, double movePercent)
            {
                for (int cntr = 0; cntr < dots.Length; cntr++)		// only need to iterate up to returnCount.  All the rest in dots are immoble ones
                {
                    if (dots[cntr].IsStatic)
                    {
                        continue;
                    }

                    // Move point
                    dots[cntr].Position += forces[cntr] * movePercent;

                    // Force the dot onto the surface of the sphere
                    dots[cntr].Position = dots[cntr].Position.ToUnit() * radius;
                }
            }

            private static Visual3D GetLines(Dot[] dots, bool colorByLastDim, bool ignoreBottom, double radius, VisualizationColors colors)
            {
                //NOTE: The links passed in were calculated before the points moved, but it would be inneficient to recalculate just for slightly more accurate lenths

                var points = dots.
                    Select(o => o.Position.VectorArray).
                    ToArray();

                var convexHull = MIConvexHull.ConvexHull.Create(points);

                Model3DGroup group = new Model3DGroup();

                foreach (var face in convexHull.Faces)
                {
                    if (ignoreBottom && face.Vertices.Any(o => UtilityNOS.IsBottom(o.Position.ToVectorND())))
                    {
                        continue;
                    }

                    foreach (var indices in UtilityCore.IterateEdges(face.Vertices.Length))
                    {
                        double[] from = face.Vertices[indices.from].Position;
                        double[] to = face.Vertices[indices.to].Position;

                        BillboardLine3D line = new BillboardLine3D()
                        {
                            Thickness = LINE,
                            FromPoint = from.ToPoint3D(false),
                            ToPoint = to.ToPoint3D(false),
                        };

                        if (colorByLastDim)
                        {
                            line.ColorTo = UtilityNOS.GetDotColor(to.ToVectorND(), colorByLastDim, radius, colors);     // doing To first so that it doesn't unnecessarily build a solidcolor brush
                            line.Color = UtilityNOS.GetDotColor(from.ToVectorND(), colorByLastDim, radius, colors);
                        }
                        else
                        {
                            line.Color = colors.LightGray;
                        }

                        group.Children.Add(line.Model);
                    }
                }

                return new ModelVisual3D()
                {
                    Content = group,
                };
            }

            private static void MoveVisuals(Dot[] dots, bool ignoreBottom)
            {
                for (int cntr = 0; cntr < dots.Length; cntr++)
                {
                    //TODO: Have a function that maps the VectorND into 3D.  The current way is simply only taking the first 3 coords, but it might be more
                    //illuminating to allow custom rotations (especially if in vr and those rotations are mapped to the head's position/orientation)
                    Vector3D position = dots[cntr].Position.ToVector3D(false);

                    if (ignoreBottom && UtilityNOS.IsBottom(dots[cntr].Position))
                    {
                        position.Z = -100;
                    }

                    dots[cntr].Translate.OffsetX = position.X;
                    dots[cntr].Translate.OffsetY = position.Y;
                    dots[cntr].Translate.OffsetZ = position.Z;
                }
            }

            private static void ColorVisuals(Dot[] dots, bool colorByLastDim, bool ignoreBottom, double radius, VisualizationColors colors)
            {
                foreach (Dot dot in dots)
                {
                    if (ignoreBottom && UtilityNOS.IsBottom(dot.Position))
                    {
                        dot.Brush.Color = Colors.Transparent;
                    }
                    else
                    {
                        dot.Brush.Color = UtilityNOS.GetDotColor(dot.Position, colorByLastDim, radius, colors);
                    }
                }
            }

            #endregion
        }

        #endregion
        #region class: PolyAtAngle

        /// <remarks>
        /// This was going to have a slider bar that let the user pick an angle, and this would find the polygon most in line with
        /// that angle.  But it turned into several static views of random polygons - which was good enough to get an idea of
        /// how the polygons are linked together
        /// </remarks>
        private class PolyAtAngle : ITimerVisualization
        {
            #region Declaration Section

            private const double RADIUS = 10;
            private const double DOT = .15;
            private const double LINE = .02;

            private readonly VisualizationColors _colors;

            private readonly int _dimensions;
            private readonly Viewport3D _viewport;
            private readonly Grid _grid;

            private readonly VectorND[] _points;
            private readonly ConvexHull<DefaultVertex, DefaultConvexFace<DefaultVertex>> _convexHull;

            private readonly List<Visual3D> _visuals = new List<Visual3D>();

            #endregion

            #region Constructor

            public PolyAtAngle(int dimensions, Viewport3D viewport, Grid grid, VisualizationColors colors)
            {
                _colors = colors;

                int count;
                switch (dimensions)
                {
                    case 1:
                        count = 4;
                        break;

                    case 2:
                        count = 20;
                        break;

                    case 3:
                        count = 80;
                        break;

                    default:
                        count = 100;
                        break;
                }

                _dimensions = dimensions;
                _viewport = viewport;
                _grid = grid;

                _points = MathND.GetRandomVectors_Spherical_Shell_EvenDist(dimensions, RADIUS, count);

                _convexHull = MIConvexHull.ConvexHull.Create(_points.Select(o => o.VectorArray).ToArray());

                ShowPoints(_points, _colors);
                ShowTetraExtremes(_convexHull, _colors);
                for (int cntr = 0; cntr < 10; cntr++)
                    ShowTetraNeighborhood(_convexHull, _colors);
            }

            #endregion

            public bool ColorByLastDim { get; set; }

            public void Tick(double elapsedSeconds)
            {
                // nothing to do in the tick, it's all driven by other events
            }

            public void Dispose()
            {
                _viewport.Children.RemoveAll(_visuals);
                _visuals.Clear();

                _grid.Children.Clear();
            }

            #region Private Methods

            private static void ShowPoints(VectorND[] points, VisualizationColors colors)
            {
                Debug3DWindow window = new Debug3DWindow()
                {
                    Title = "points",
                };

                window.AddAxisLines(RADIUS * 1.25, LINE);

                foreach (VectorND point in points)
                {
                    window.AddDot(point.ToPoint3D(false), DOT, UtilityNOS.GetDotColor(point, true, RADIUS, colors));
                }

                window.Show();
            }
            private static void ShowTetraExtremes(ConvexHull<DefaultVertex, DefaultConvexFace<DefaultVertex>> convexHull, VisualizationColors colors)
            {
                var sorted = convexHull.Faces.
                    Select(o => new
                    {
                        face = o,
                        minW = o.Vertices.Min(p => p.Position[p.Position.Length - 1]),
                        maxW = o.Vertices.Max(p => p.Position[p.Position.Length - 1]),
                    }).
                    OrderBy(o => Math.Abs(o.maxW - o.minW)).        // shouldn't need the abs
                    ToArray();

                var samples = new[]
                {
                    new { title = "Smallest W", poly = sorted[0] },
                    new { title = "Largest W", poly = sorted[sorted.Length - 1] },
                };

                foreach (var sample in samples)
                {
                    Debug3DWindow window = new Debug3DWindow()
                    {
                        Title = sample.title,
                    };

                    window.AddAxisLines(RADIUS * 1.25, LINE);

                    foreach (var point in sample.poly.face.Vertices)
                    {
                        window.AddDot(point.Position.ToPoint3D(false), DOT, UtilityNOS.GetDotColor(point.Position.ToVectorND(), true, RADIUS, colors));
                    }

                    foreach (var pair in UtilityCore.GetPairs(sample.poly.face.Vertices))
                    {
                        window.AddLine
                        (
                            pair.Item1.Position.ToPoint3D(false),
                            pair.Item2.Position.ToPoint3D(false),
                            LINE,
                            UtilityNOS.GetDotColor(pair.Item1.Position.ToVectorND(), true, RADIUS, colors),
                            UtilityNOS.GetDotColor(pair.Item2.Position.ToVectorND(), true, RADIUS, colors)
                        );
                    }

                    window.AddText("coords:");
                    foreach (var point in sample.poly.face.Vertices)
                    {
                        window.AddText(point.Position.ToVectorND().ToStringSignificantDigits(3));
                    }

                    window.AddText($"normal: {sample.poly.face.Normal.ToVectorND().ToStringSignificantDigits(3)}");

                    window.Show();
                }
            }
            private static void ShowTetraNeighborhood(ConvexHull<DefaultVertex, DefaultConvexFace<DefaultVertex>> convexHull, VisualizationColors colors)
            {
                Debug3DWindow window = new Debug3DWindow()
                {
                    Title = "Tetra Neighborhood",
                };

                window.AddAxisLines(RADIUS * 1.25, LINE);

                var allFaces = convexHull.Faces.ToArray();

                var faces = new List<DefaultConvexFace<DefaultVertex>>();

                var centerFace = allFaces[StaticRandom.Next(allFaces.Length)];
                faces.Add(centerFace);

                if (centerFace.Adjacency != null)
                {
                    faces.AddRange(centerFace.Adjacency);
                }

                for (int cntr = 0; cntr < faces.Count; cntr++)
                {
                    var face = faces[cntr];

                    VectorND center = MathND.GetCenter(face.Vertices.Select(o => o.Position.ToVectorND()));

                    foreach (var point in face.Vertices)
                    {
                        window.AddDot(point.Position.ToPoint3D(false), DOT, UtilityNOS.GetDotColor(point.Position.ToVectorND(), true, RADIUS, colors));
                    }

                    foreach (var pair in UtilityCore.GetPairs(face.Vertices))
                    {
                        window.AddLine
                        (
                            pair.Item1.Position.ToPoint3D(false),
                            pair.Item2.Position.ToPoint3D(false),
                            LINE,
                            UtilityNOS.GetDotColor(pair.Item1.Position.ToVectorND(), true, RADIUS, colors),
                            UtilityNOS.GetDotColor(pair.Item2.Position.ToVectorND(), true, RADIUS, colors)
                        );
                    }

                    var arrows = face.Vertices.
                        Select(o => (o.Position.ToVectorND() - center) / 3).
                        Select(o => (center, center + o)).
                        Select(o => (o.Item1.ToPoint3D(false), o.Item2.ToPoint3D(false)));

                    window.AddLines(arrows, LINE * .5, Colors.White);

                    window.AddText3D(cntr.ToString(), center.ToPoint3D(false), new Vector3D(0, 0, 1), DOT * 7, Colors.DimGray, true);
                }

                window.AddText("center coords:");
                foreach (var point in centerFace.Vertices)
                {
                    window.AddText(point.Position.ToVectorND().ToStringSignificantDigits(3));
                }

                window.AddText($"center normal: {centerFace.Normal.ToVectorND().ToStringSignificantDigits(3)}");

                window.Show();
            }

            #endregion
        }

        #endregion
        #region class: MaintainTriangle

        private class MaintainTriangle : ITimerVisualization
        {
            #region Declaration Section

            private const double DOT = .15;
            private const double LINE = .02;

            private const int DIMENSIONS = 3;

            private double MAXEXTERNALFORCE = 10;
            private double LINKMULT_COMPRESS = 8;
            private double LINKMULT_EXPAND = 5;

            private readonly Viewport3D _viewport;
            private readonly Grid _grid;
            private readonly VisualizationColors _colors;

            private Visual3D _dotVisual = null;
            private Visual3D _linesVisual = null;
            private readonly List<Visual3D> _visuals = new List<Visual3D>();

            private VectorND[] _points;
            private readonly TranslateTransform3D[] _dotTransforms;

            private readonly EdgeLink[] _links;
            private readonly BillboardLine3D[] _linkVisuals;

            private readonly double _maxForce;

            private VectorND[] _externalForces = null;

            #endregion

            #region Constructor

            public MaintainTriangle(Viewport3D viewport, Grid grid, VisualizationColors colors)
            {
                _viewport = viewport;
                _grid = grid;
                _colors = colors;

                _externalForces = MathND.GetRandomVectors_Spherical(DIMENSIONS, MAXEXTERNALFORCE, 3);

                Triangle triangle = UtilityWPF.GetEquilateralTriangle(1);

                _points = new[]
                {
                    triangle.Point0.ToVectorND(DIMENSIONS),
                    triangle.Point1.ToVectorND(DIMENSIONS),
                    triangle.Point2.ToVectorND(DIMENSIONS),
                };

                _links = new EdgeLink[]
                {
                    new EdgeLink(0, 1, _points, (_points[1] - _points[0]).Length),
                    new EdgeLink(1, 2, _points, (_points[2] - _points[1]).Length),
                    new EdgeLink(2, 0, _points, (_points[0] - _points[2]).Length),
                };

                _maxForce = _links.Max(o => o.DesiredLength) * 4;

                // Dot Visuals
                var dotVisuals = GetDotVisuals(_points, colors);
                _dotTransforms = dotVisuals.transforms;

                _dotVisual = new ModelVisual3D()
                {
                    Content = dotVisuals.models,
                };
                _viewport.Children.Add(_dotVisual);

                // Line Visuals
                var lineVisuals = GetLineVisuals(_points, _links, colors);
                _linkVisuals = lineVisuals.lines;

                _linesVisual = new ModelVisual3D()
                {
                    Content = lineVisuals.models,
                };
                _viewport.Children.Add(_linesVisual);

                // WPF Controls
                BuildSliders();
            }

            #endregion

            public void Tick(double elapsedSeconds)
            {
                const double MULT = .1;

                VectorND[] forces = Enumerable.Range(0, _points.Length).
                    Select(o => new VectorND(DIMENSIONS)).
                    ToArray();

                #region external forces

                UpdateExternalForces(_externalForces, MAXEXTERNALFORCE);

                for (int cntr = 0; cntr < forces.Length; cntr++)
                {
                    forces[cntr] += _externalForces[cntr];
                }

                #endregion

                double[] linkLengths = UtilityNOS.UpdateLinkForces(forces, _links, _points, LINKMULT_COMPRESS, LINKMULT_EXPAND);       // increasing the force felt by the links to compare with MAXEXTERNALFORCE

                UtilityNOS.ApplyForces(_points, forces, elapsedSeconds, MULT, _maxForce);

                #region toward center

                VectorND center = MathND.GetCenter(_points);

                // instead of a force, go straight for displacement after forces are applied
                //for (int cntr = 0; cntr < forces.Length; cntr++)
                //{
                //    forces[cntr] -= center;
                //}

                for (int cntr = 0; cntr < _points.Length; cntr++)
                {
                    _points[cntr] -= center;
                }

                #endregion

                _viewport.Children.RemoveAll(_visuals);
                _visuals.Clear();

                #region update transforms/colors

                for (int cntr = 0; cntr < _points.Length; cntr++)
                {
                    Point3D point3D = _points[cntr].ToPoint3D(false);

                    _dotTransforms[cntr].OffsetX = point3D.X;
                    _dotTransforms[cntr].OffsetY = point3D.Y;
                    _dotTransforms[cntr].OffsetZ = point3D.Z;
                }

                for (int cntr = 0; cntr < _links.Length; cntr++)
                {
                    _linkVisuals[cntr].FromPoint = _points[_links[cntr].Index0].ToPoint3D(false);
                    _linkVisuals[cntr].ToPoint = _points[_links[cntr].Index1].ToPoint3D(false);

                    _linkVisuals[cntr].Color = UtilityNOS.GetLinkColor(linkLengths[cntr], _links[cntr].DesiredLength, _colors);
                }

                #endregion
                #region triangle normal

                if (_points.Length == 3)
                {
                    Triangle triangle = new Triangle(_points[0].ToPoint3D(false), _points[1].ToPoint3D(false), _points[2].ToPoint3D(false));

                    Visual3D visual = new ModelVisual3D()
                    {
                        Content = new BillboardLine3D()
                        {
                            FromPoint = triangle.GetCenterPoint(),
                            ToPoint = triangle.GetCenterPoint() + triangle.NormalUnit,
                            Thickness = LINE,
                            Color = UtilityWPF.ColorFromHex("DDD"),
                        }.Model,
                    };
                    _visuals.Add(visual);
                    _viewport.Children.Add(visual);
                }

                #endregion
            }

            public void Dispose()
            {
                _viewport.Children.Remove(_dotVisual);
                _dotVisual = null;

                _viewport.Children.Remove(_linesVisual);
                _linesVisual = null;

                _viewport.Children.RemoveAll(_visuals);
                _visuals.Clear();

                _grid.Children.Clear();
            }

            #region Private Methods

            private static (Model3DGroup models, TranslateTransform3D[] transforms) GetDotVisuals(VectorND[] points, VisualizationColors colors)
            {
                TranslateTransform3D[] dotTranslates = new TranslateTransform3D[points.Length];

                Model3DGroup modelGroup = new Model3DGroup();

                MeshGeometry3D dotGeometry = UtilityWPF.GetSphere_Ico(DOT, 1, true);

                for (int cntr = 0; cntr < points.Length; cntr++)
                {
                    MaterialGroup material = new MaterialGroup();
                    material.Children.Add(new DiffuseMaterial(new SolidColorBrush(colors.Gray)));
                    material.Children.Add(colors.Specular);

                    dotTranslates[cntr] = new TranslateTransform3D(points[cntr].ToVector3D(false));

                    modelGroup.Children.Add(new GeometryModel3D()
                    {
                        Material = material,
                        BackMaterial = material,
                        Geometry = dotGeometry,
                        Transform = dotTranslates[cntr],
                    });
                }

                return (modelGroup, dotTranslates);
            }
            private static (Model3DGroup models, BillboardLine3D[] lines) GetLineVisuals(VectorND[] points, EdgeLink[] links, VisualizationColors colors)
            {
                BillboardLine3D[] lines = new BillboardLine3D[links.Length];

                Model3DGroup group = new Model3DGroup();

                for (int cntr = 0; cntr < links.Length; cntr++)
                {
                    lines[cntr] = new BillboardLine3D()
                    {
                        Thickness = LINE,
                        FromPoint = points[links[cntr].Index0].ToPoint3D(false),
                        ToPoint = points[links[cntr].Index1].ToPoint3D(false),
                        Color = colors.LightGray,
                    };

                    group.Children.Add(lines[cntr].Model);
                }

                return (group, lines);
            }

            private static void UpdateExternalForces(VectorND[] forces, double maxExternalForce)
            {
                double maxDelta = maxExternalForce / 6;

                VectorND[] deltas = MathND.GetRandomVectors_Spherical(3, maxDelta, forces.Length);

                for (int cntr = 0; cntr < forces.Length; cntr++)
                {
                    VectorND newForce = forces[cntr] + deltas[cntr];

                    if (newForce.LengthSquared > maxExternalForce * maxExternalForce)
                    {
                        newForce = newForce.ToUnit() * maxExternalForce;
                    }

                    forces[cntr] = newForce;
                }
            }

            private void BuildSliders()
            {
                const int PRECISION = 3;

                Grid grid = new Grid()
                {
                    MinWidth = 250,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(8),
                };

                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(4, GridUnitType.Pixel) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(4, GridUnitType.Pixel) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });

                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(2, GridUnitType.Pixel) });
                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(2, GridUnitType.Pixel) });
                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });

                //MAXEXTERNALFORCE = 10;
                //LINKMULT_COMPRESS = 8;
                //LINKMULT_EXPAND = 5;

                #region max external force

                // Description
                TextBlock label = new TextBlock()
                {
                    Text = "max external force",
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                Grid.SetColumn(label, 0);
                Grid.SetRow(label, 0);
                grid.Children.Add(label);

                // Slider
                Slider slider_external = new Slider()
                {
                    Minimum = 1,
                    Maximum = 20,
                    Value = 10,
                };
                Grid.SetColumn(slider_external, 2);
                Grid.SetRow(slider_external, 0);
                grid.Children.Add(slider_external);

                // Value
                TextBlock label_external = new TextBlock()
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Text = slider_external.Value.ToStringSignificantDigits(PRECISION),
                };
                Grid.SetColumn(label_external, 4);
                Grid.SetRow(label_external, 0);
                grid.Children.Add(label_external);

                slider_external.ValueChanged += (s, e) =>
                {
                    MAXEXTERNALFORCE = slider_external.Value;
                    label_external.Text = slider_external.Value.ToStringSignificantDigits(PRECISION);
                };

                #endregion
                #region compress mult

                // Description
                label = new TextBlock()
                {
                    Text = "compress mult",
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                Grid.SetColumn(label, 0);
                Grid.SetRow(label, 2);
                grid.Children.Add(label);

                // Slider
                Slider slider_compress = new Slider()
                {
                    Minimum = 1,
                    Maximum = 20,
                    Value = 8,
                };
                Grid.SetColumn(slider_compress, 2);
                Grid.SetRow(slider_compress, 2);
                grid.Children.Add(slider_compress);

                // Value
                TextBlock label_compress = new TextBlock()
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Text = slider_compress.Value.ToStringSignificantDigits(PRECISION),
                };
                Grid.SetColumn(label_compress, 4);
                Grid.SetRow(label_compress, 2);
                grid.Children.Add(label_compress);

                slider_compress.ValueChanged += (s, e) =>
                {
                    LINKMULT_COMPRESS = slider_compress.Value;
                    label_compress.Text = slider_compress.Value.ToStringSignificantDigits(PRECISION);
                };

                #endregion
                #region expand mult

                // Description
                label = new TextBlock()
                {
                    Text = "expand mult",
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                Grid.SetColumn(label, 0);
                Grid.SetRow(label, 4);
                grid.Children.Add(label);

                // Slider
                Slider slider_expand = new Slider()
                {
                    Minimum = 1,
                    Maximum = 20,
                    Value = 5,
                };
                Grid.SetColumn(slider_expand, 2);
                Grid.SetRow(slider_expand, 4);
                grid.Children.Add(slider_expand);

                // Value
                TextBlock label_expand = new TextBlock()
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Text = slider_expand.Value.ToStringSignificantDigits(PRECISION),
                };
                Grid.SetColumn(label_expand, 4);
                Grid.SetRow(label_expand, 4);
                grid.Children.Add(label_expand);

                slider_expand.ValueChanged += (s, e) =>
                {
                    LINKMULT_EXPAND = slider_expand.Value;
                    label_expand.Text = slider_expand.Value.ToStringSignificantDigits(PRECISION);
                };

                #endregion

                _grid.Children.Add(grid);
            }

            #endregion
        }

        #endregion
        #region class: LatchJointTester

        //TODO: Can't directly latch the mouse cursor to a point.  Tie the mouse cursor to one end of a segment.  That was the latched joint
        //has forces to test against (latch force is a maximum possible, it's not a direct force)
        private class LatchJointTester : ITimerVisualization
        {
            #region Declaration Section

            private const double DOT = .15;
            private const double LINE = .02;

            private const int DIMENSIONS = 2;

            private const double LINKLENGTH = 4;
            private const double LINKMULT_COMPRESS = 8;
            private const double LINKMULT_EXPAND = 5;

            private const double LATCHFORCE = 3;
            private const double DISTANCE_LATCH = .33;
            private const double DISTANCE_FORCE = 2;

            private readonly Border _border;
            private readonly Viewport3D _viewport;
            private readonly PerspectiveCamera _camera;
            private readonly Grid _grid;
            private readonly VisualizationColors _colors;

            private readonly VectorND _anchorPoint;
            private VectorND _freePoint;
            private readonly SolidColorBrush _freePointBrush;
            private readonly TranslateTransform3D _freePointTransform;

            private readonly BillboardLine3D _linkVisual;

            private Visual3D _dotVisual = null;
            private Visual3D _lineVisual = null;
            private readonly List<Visual3D> _visuals = new List<Visual3D>();

            private Point? _mousePoint2D = null;

            private readonly double _maxForce;

            #endregion

            #region Constructor

            public LatchJointTester(Border border, Viewport3D viewport, PerspectiveCamera camera, Grid grid, VisualizationColors colors)
            {
                _border = border;
                _viewport = viewport;
                _camera = camera;
                _grid = grid;
                _colors = colors;

                _anchorPoint = new VectorND(DIMENSIONS);
                _freePoint = MathND.GetRandomVector_Spherical_Shell(DIMENSIONS, LINKLENGTH);

                // Dot Visuals
                var dotVisuals = GetDotVisuals(_anchorPoint, _freePoint, _colors);
                _freePointTransform = dotVisuals.freeTransform;
                _freePointBrush = dotVisuals.freeBrush;

                _dotVisual = new ModelVisual3D()
                {
                    Content = dotVisuals.models,
                };

                _viewport.Children.Add(_dotVisual);

                // Line Visual
                var lineVisual = GetLineVisual(_anchorPoint, _freePoint, _colors);
                _linkVisual = lineVisual.line;

                _lineVisual = new ModelVisual3D()
                {
                    Content = lineVisual.model,
                };
                _viewport.Children.Add(_lineVisual);

                _border.MouseMove += Border_MouseMove;
                _border.MouseLeave += Border_MouseLeave;

                _maxForce = LINKLENGTH * 4;
            }

            #endregion

            public void Tick(double elapsedSeconds)
            {
                const double MULT = .5;

                _viewport.Children.RemoveAll(_visuals);
                _visuals.Clear();

                VectorND[] forces = Enumerable.Range(0, 2).
                    Select(o => new VectorND(DIMENSIONS)).
                    ToArray();

                VectorND[] points = new[] { _anchorPoint, _freePoint };
                EdgeLink[] links = new[] { new EdgeLink(0, 1, points, LINKLENGTH) };

                double[] linkLengths = UtilityNOS.UpdateLinkForces(forces, links, points, LINKMULT_COMPRESS, LINKMULT_EXPAND);

                #region mouse

                // Project from the cursor to the plane
                Point3D? mousePos = GetMousePosition3D(_mousePoint2D, _camera, _viewport);
                bool isInLatchDistance = false;
                if (mousePos != null)
                {
                    // Draw two circles around the mouse
                    AddCircles(mousePos.Value);

                    // Draw the free point toward the mouse
                    var latchForce = UtilityNOS.GetLatchForce(mousePos.Value.ToVectorND(DIMENSIONS), _freePoint, DISTANCE_LATCH, DISTANCE_FORCE, LATCHFORCE);
                    forces[1] -= latchForce.forceToward0;
                    isInLatchDistance = latchForce.isInside;
                }

                #endregion

                UtilityNOS.ApplyForces(points, forces, elapsedSeconds, MULT, _maxForce);

                // This is wrong.  The latch force causes the total force to be in equilibreum.  Can't look at combined
                // force, need to consider all the individual forces acting on the latch.  Also, allow infinite compression.
                // Only expansion forces can break the latch
                //
                // The latch can be thought of as:
                //      forces[] - point | point - forces[]
                //
                // The final version can have multiple latches tied together (not just two)
                bool isLatched = isInLatchDistance && forces[1].LengthSquared <= LATCHFORCE * LATCHFORCE;

                if (isLatched)
                {
                    _freePoint = mousePos.Value.ToVectorND(DIMENSIONS);
                }
                else
                {
                    _freePoint = points[1];     // even though UtilityNOS.ApplyForces tried to move the anchor point, don't actually move the anchor
                }

                #region update transforms/colors

                Point3D point3D = _freePoint.ToPoint3D(false);

                _freePointTransform.OffsetX = point3D.X;
                _freePointTransform.OffsetY = point3D.Y;
                _freePointTransform.OffsetZ = point3D.Z;

                _freePointBrush.Color = isLatched ?
                    Colors.DarkOrange :
                    _colors.Gray;

                //_linkVisual.FromPoint = _anchorPoint.ToPoint3D(false);        // anchor never moves
                _linkVisual.ToPoint = _freePoint.ToPoint3D(false);

                _linkVisual.Color = UtilityNOS.GetLinkColor(linkLengths[0], links[0].DesiredLength, _colors);

                #endregion
            }

            public void Dispose()
            {
                _viewport.Children.Remove(_dotVisual);
                _dotVisual = null;

                _viewport.Children.Remove(_lineVisual);
                _lineVisual = null;

                _viewport.Children.RemoveAll(_visuals);
                _visuals.Clear();

                //_grid.Children.Clear();

                _border.MouseMove -= Border_MouseMove;
                _border.MouseLeave -= Border_MouseLeave;
            }

            private void Border_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
            {
                try
                {
                    _mousePoint2D = e.GetPosition(_border);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), nameof(LatchJointTester), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            private void Border_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
            {
                try
                {
                    _mousePoint2D = null;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), nameof(LatchJointTester), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            #region Private Methods

            private static (Model3DGroup models, TranslateTransform3D freeTransform, SolidColorBrush freeBrush) GetDotVisuals(VectorND anchorPoint, VectorND freePoint, VisualizationColors colors)
            {
                Model3DGroup modelGroup = new Model3DGroup();

                #region anchor

                MaterialGroup material = new MaterialGroup();
                material.Children.Add(new DiffuseMaterial(new SolidColorBrush(colors.Zero)));
                material.Children.Add(colors.Specular);

                modelGroup.Children.Add(new GeometryModel3D()
                {
                    Material = material,
                    BackMaterial = material,
                    Geometry = UtilityWPF.GetSphere_Ico(DOT * .33, 1, true),
                    Transform = new TranslateTransform3D(anchorPoint.ToVector3D(false)),
                });

                #endregion
                #region free point

                material = new MaterialGroup();
                SolidColorBrush freeBrush = new SolidColorBrush(colors.Gray);
                material.Children.Add(new DiffuseMaterial(freeBrush));
                material.Children.Add(colors.Specular);

                TranslateTransform3D translate = new TranslateTransform3D(freePoint.ToVector3D(false));

                modelGroup.Children.Add(new GeometryModel3D()
                {
                    Material = material,
                    BackMaterial = material,
                    Geometry = UtilityWPF.GetSphere_Ico(DOT, 1, true),
                    Transform = translate,
                });

                #endregion

                return (modelGroup, translate, freeBrush);
            }
            private static (Model3D model, BillboardLine3D line) GetLineVisual(VectorND anchorPoint, VectorND freePoint, VisualizationColors colors)
            {
                BillboardLine3D line = new BillboardLine3D()
                {
                    Thickness = LINE,
                    FromPoint = anchorPoint.ToPoint3D(false),
                    ToPoint = freePoint.ToPoint3D(false),
                    Color = colors.Zero,
                };

                return (line.Model, line);
            }

            private static Point3D? GetMousePosition3D(Point? mousePoint, PerspectiveCamera camera, Viewport3D viewport)
            {
                if (mousePoint == null)
                {
                    return null;
                }

                var ray = UtilityWPF.RayFromViewportPoint(camera, viewport, mousePoint.Value);

                Triangle plane = new Triangle(new Point3D(0, 0, 0), new Point3D(1, 0, 0), new Point3D(0, 1, 0));

                return Math3D.GetIntersection_Plane_Ray(plane, ray.Origin, ray.Direction);
            }

            private void AddCircles(Point3D position)
            {
                Visual3D visual = GetCircleVisual(position, DISTANCE_LATCH, _colors.LightGray);
                _visuals.Add(visual);
                _viewport.Children.Add(visual);

                visual = GetCircleVisual(position, DISTANCE_FORCE, _colors.LightGray);
                _visuals.Add(visual);
                _viewport.Children.Add(visual);
            }
            private static Visual3D GetCircleVisual(Point3D position, double radius, Color color)
            {
                const int CIRCLESIDES = 36;

                BillboardLine3DSet retVal = new BillboardLine3DSet()
                {
                    IsReflectiveColor = false,
                    Color = color,
                };

                retVal.BeginAddingLines();

                Point3D[] points = Math2D.GetCircle_Cached(CIRCLESIDES).
                    Select(o => position + (o.ToVector3D() * radius)).
                    ToArray();

                foreach (var line in UtilityCore.IterateEdges(CIRCLESIDES))
                {
                    retVal.AddLine(points[line.from], points[line.to], LINE);
                }

                retVal.EndAddingLines();

                return retVal;
            }

            #endregion
        }

        #endregion
        #region class: LatchJointTester2

        private class LatchJointTester2 : ITimerVisualization
        {
            #region class: LatchJoint_tri

            private class LatchJoint_tri
            {
                public readonly LatchTriangle Triangle1;
                public readonly int Index1;

                public readonly LatchTriangle Triangle2;
                public readonly int Index2;

                public readonly VectorND[] AllPoints;

                public bool IsLatched { get; set; }
            }

            #endregion
            #region class: LatchTriangle

            //TODO: See if this really needs to be a triangle.  4D would need a tetrahedron, so does a tetrahedron class need to be created?  Can this be simplified to a line segment?
            //Basically, some edges/verticies can't be broken apart, some can
            //      Collapsing a 2D circle into 1D creates a chain of segments that can be broken at every joint
            //      Collapsing a 3D sphere into 2D creates a mesh of triangles.  Individual triangles can't be broken, but inter triangle links can be broken
            //      Collapsing a 4D sphere into 3D creates a (mesh?) of tetrahedrons.  This has shared faces
            //
            //Each dimension up creates a more complex atomic object, and more complex links.  But the structure to represent this can just be edge links with some endpoints
            //being latch joints, and some endpoints being unbreakable links to other edges
            private class LatchTriangle
            {
                public readonly int Index0;
                public readonly int Index1;
                public readonly int Index2;

                public VectorND Point0 => AllPoints[Index0];
                public VectorND Point1 => AllPoints[Index1];
                public VectorND Point2 => AllPoints[Index2];

                public readonly EdgeLink Edge_01;
                public readonly EdgeLink Edge_12;
                public readonly EdgeLink Edge_20;

                public readonly VectorND[] AllPoints;

                public LatchJoint_tri[] Neighbor_0 { get; set; }
                public LatchJoint_tri[] Neighbor_1 { get; set; }
                public LatchJoint_tri[] Neighbor_2 { get; set; }

                public readonly long Token = TokenGenerator.NextToken();

                public VectorND GetCenterPoint()
                {
                    return MathND.GetCenter(new[] { Point0, Point1, Point2 });
                }
            }

            #endregion
            #region class: EdgeLinkLatch

            //TODO: This might be flawed.  within a triangle, two segments would share a vertex that is a latch joint
            private class EdgeLinkLatch : EdgeLink
            {
                public EdgeLinkLatch(int index0, int index1, VectorND[] allPoints, double desiredLength, LatchJoint[] latches0 = null, LatchJoint[] latches1 = null)
                    : base(index0, index1, allPoints, desiredLength)
                {
                    Latches0 = latches0;
                    Latches1 = latches1;
                }

                //NOTE: These both don't need to be populated.  There may be segments that are end points.  Or there may be shared
                //verticies that are permanent (not latched based on forces).  If edges share a permanent vertex, the corresponding index
                //will be the same
                public readonly LatchJoint[] Latches0;
                public readonly LatchJoint[] Latches1;
            }

            #endregion

            #region Declaration Section

            private const double DOT = .15;
            private const double LINE = .02;

            private const int DIMENSIONS = 2;

            private const double LINKLENGTH = 4;
            private const double LINKMULT_COMPRESS = 8;
            private const double LINKMULT_EXPAND = 5;

            private const double LATCHFORCE = 3;
            private const double DISTANCE_LATCH = DOT / 5;     // if this is very large at all, it will cause the points to noticably jump as they latch/unlatch.  If it's too small, the points will orbit each other wildly before latching
            private const double DISTANCE_FORCE = 3;

            private readonly Border _border;
            private readonly Viewport3D _viewport;
            private readonly PerspectiveCamera _camera;
            private readonly Grid _grid;
            private readonly VisualizationColors _colors;

            private Visual3D _dotVisual = null;
            private Visual3D _linesVisual = null;

            private readonly TranslateTransform3D[] _transforms;
            private readonly SolidColorBrush[] _brushes;

            private readonly VectorND[] _points;
            private readonly int[] _clickablePoints;

            private readonly EdgeLink[][] _links;
            private readonly LatchJoint[] _joints;

            private readonly BillboardLine3D[][] _billboards;

            private Point? _mousePoint2D = null;
            private bool _isMouseDown = false;
            private int? _mouseDraggingIndex = null;

            private readonly double _maxForce;

            private bool _drawSnapshot = false;

            #endregion

            #region Constructor

            public LatchJointTester2(Border border, Viewport3D viewport, PerspectiveCamera camera, Grid grid, VisualizationColors colors)
            {
                _border = border;
                _viewport = viewport;
                _camera = camera;
                _grid = grid;
                _colors = colors;

                _maxForce = LINKLENGTH * 4;

                List<VectorND> points = new List<VectorND>();
                List<int> clickable = new List<int>();

                // Points
                var triangles = GetTrianglePositions();
                clickable.Add(0);
                points.AddRange(triangles.triangleAnchor);

                clickable.Add(points.Count);
                points.AddRange(triangles.triangleMouse);

                _points = points.ToArray();
                _clickablePoints = clickable.ToArray();

                // Point Visuals
                var dots = GetDotVisual(_points, _clickablePoints, _colors);
                _dotVisual = dots.visual;
                _transforms = dots.transforms;
                _brushes = dots.brushes;
                _viewport.Children.Add(_dotVisual);

                // Joints
                _joints = GetJoints(_points, triangles.pairs);

                // Links
                _links = GetLinks(_points);

                // Link Visuals
                var lines = GetLineVisuals(_links, _colors);
                _linesVisual = lines.visual;
                _billboards = lines.lines;
                _viewport.Children.Add(_linesVisual);

                // Build WPF Controls
                BuildControls();

                // Event Listeners
                _border.MouseMove += Border_MouseMove;
                _border.MouseLeave += Border_MouseLeave;
                _border.MouseLeftButtonDown += Border_MouseLeftButtonDown;
                _border.MouseLeftButtonUp += Border_MouseLeftButtonUp;
            }

            #endregion

            public void Tick(double elapsedSeconds)
            {
                const double MULT = .5;

                VectorND[] forces = Enumerable.Range(0, _points.Length).
                    Select(o => new VectorND(DIMENSIONS)).
                    ToArray();

                double[][] linkLengths = _links.
                    Select(o => UtilityNOS.UpdateLinkForces(forces, o, _points, LINKMULT_COMPRESS, LINKMULT_EXPAND)).
                    ToArray();

                // Calculate latch forces, maybe break/combine latches
                UpdateLatchForces(forces, _links, _joints, ref _drawSnapshot);

                // Project from the cursor to the plane
                Point3D? mousePos = GetMousePosition3D(_mousePoint2D, _isMouseDown, _camera, _viewport);

                if (mousePos == null)
                {
                    _mouseDraggingIndex = null;
                }
                else if (_mouseDraggingIndex == null)
                {
                    _mouseDraggingIndex = FindClosestPoint(mousePos.Value.ToVectorND(DIMENSIONS), _points, _clickablePoints);
                }

                #region apply forces

                VectorND[] points = _points.
                    Select(o => o.Clone()).
                    ToArray();

                //UtilityNOS.ApplyForces(points, forces, elapsedSeconds, MULT, _maxForce);
                ApplyForces(points, forces, elapsedSeconds, MULT, _maxForce, _joints);

                if (mousePos != null && _mouseDraggingIndex != null)
                {
                    _points[_mouseDraggingIndex.Value] = mousePos.Value.ToVectorND(DIMENSIONS);
                }

                for (int cntr = 0; cntr < points.Length; cntr++)
                {
                    if (!_clickablePoints.Contains(cntr))
                    {
                        _points[cntr] = points[cntr];
                    }
                }

                #endregion

                UpdateTransformsColors(_points, _transforms, _brushes, _links, _billboards, linkLengths, _joints, _clickablePoints, _colors);
            }

            public void Dispose()
            {
                _viewport.Children.Remove(_dotVisual);
                _dotVisual = null;

                _viewport.Children.Remove(_linesVisual);
                _linesVisual = null;

                _border.MouseMove -= Border_MouseMove;
                _border.MouseLeave -= Border_MouseLeave;

                _grid.Children.Clear();
            }

            private void Border_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
            {
                try
                {
                    _mousePoint2D = e.GetPosition(_border);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), nameof(LatchJointTester2), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            private void Border_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
            {
                try
                {
                    _mousePoint2D = null;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), nameof(LatchJointTester2), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            private void Border_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
            {
                _isMouseDown = true;
            }
            private void Border_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
            {
                _isMouseDown = false;
            }

            #region Private Methods

            private static (VectorND[] triangleAnchor, VectorND[] triangleMouse, (int, int)[] pairs) GetTrianglePositions()
            {
                Triangle triangleAnchor = UtilityWPF.GetEquilateralTriangle(1);

                Transform3DGroup transform = new Transform3DGroup();
                transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 180)));
                transform.Children.Add(new TranslateTransform3D(0, -1 - (DISTANCE_FORCE * 1.5), 0));

                return
                (
                    new[]
                    {
                        triangleAnchor.Point0.ToVectorND(DIMENSIONS),
                        triangleAnchor.Point1.ToVectorND(DIMENSIONS),
                        triangleAnchor.Point2.ToVectorND(DIMENSIONS),
                    },

                    new[]
                    {
                        transform.Transform(triangleAnchor.Point0).ToVectorND(DIMENSIONS),
                        transform.Transform(triangleAnchor.Point2).ToVectorND(DIMENSIONS),     // switching 1 and 2 so that the corresponding points close to each other
                        transform.Transform(triangleAnchor.Point1).ToVectorND(DIMENSIONS),
                    },

                    new[]
                    {
                        (1, 4),
                        (2, 5),
                    }
                );
            }

            private static LatchJoint[] GetJoints(VectorND[] points, (int, int)[] latchPairs)
            {
                return latchPairs.
                    Select(o => new LatchJoint(o.Item1, o.Item2, points, LATCHFORCE, DISTANCE_LATCH, DISTANCE_FORCE, false)).        // the triangles are created away from each other in this tester
                    ToArray();
            }

            private static EdgeLink[][] GetLinks(VectorND[] points)
            {
                return Enumerable.Range(0, 2).
                    Select(o =>
                    {
                        int offset = o * 3;
                        return new EdgeLink[]
                        {
                            new EdgeLink(offset + 0, offset + 1, points, (points[offset + 1] - points[offset + 0]).Length),
                            new EdgeLink(offset + 1, offset + 2, points, (points[offset + 2] - points[offset + 1]).Length),
                            new EdgeLink(offset + 2, offset + 0, points, (points[offset + 0] - points[offset + 2]).Length),
                        };
                    }).
                    ToArray();
            }
            private static EdgeLink[][] GetLinks_NO(VectorND[] points, (int, int)[] latchPairs)
            {
                LatchJoint[] joints = latchPairs.
                    Select(o => new LatchJoint(o.Item1, o.Item2, points, LATCHFORCE, DISTANCE_LATCH, DISTANCE_FORCE, false)).        // the triangles are created away from each other in this tester
                    ToArray();

                var retVal = new List<EdgeLink[]>();

                for (int triangleCntr = 0; triangleCntr < 2; triangleCntr++)
                {
                    int offset = triangleCntr * 3;

                }

                return retVal.ToArray();
            }

            private static (Visual3D visual, TranslateTransform3D[] transforms, SolidColorBrush[] brushes) GetDotVisual(VectorND[] points, int[] clickable, VisualizationColors colors)
            {
                Model3DGroup modelGroup = new Model3DGroup();
                TranslateTransform3D[] transforms = new TranslateTransform3D[points.Length];
                SolidColorBrush[] brushes = new SolidColorBrush[points.Length];

                for (int cntr = 0; cntr < points.Length; cntr++)
                {
                    bool isClickable = clickable.Contains(cntr);

                    brushes[cntr] = isClickable ?
                        new SolidColorBrush(colors.Zero) :
                        new SolidColorBrush(colors.Gray);

                    MaterialGroup material = new MaterialGroup();
                    material.Children.Add(new DiffuseMaterial(brushes[cntr]));
                    material.Children.Add(colors.Specular);

                    transforms[cntr] = new TranslateTransform3D(points[cntr].ToVector3D(false));

                    double size = isClickable ?
                        DOT * .33 :
                        DOT;

                    modelGroup.Children.Add(new GeometryModel3D()
                    {
                        Material = material,
                        BackMaterial = material,
                        Geometry = UtilityWPF.GetSphere_Ico(size, 1, true),
                        Transform = transforms[cntr],
                    });
                }

                Visual3D visual = new ModelVisual3D()
                {
                    Content = modelGroup,
                };

                return (visual, transforms, brushes);
            }
            private static (Visual3D visual, BillboardLine3D[][] lines) GetLineVisuals(EdgeLink[][] links, VisualizationColors colors)
            {
                BillboardLine3D[][] lines = new BillboardLine3D[links.Length][];

                Model3DGroup group = new Model3DGroup();

                for (int outer = 0; outer < links.Length; outer++)
                {
                    lines[outer] = new BillboardLine3D[links[outer].Length];

                    for (int inner = 0; inner < links[outer].Length; inner++)
                    {
                        lines[outer][inner] = new BillboardLine3D()
                        {
                            Thickness = LINE,
                            FromPoint = links[outer][inner].Point0.ToPoint3D(false),
                            ToPoint = links[outer][inner].Point1.ToPoint3D(false),
                            Color = colors.LightGray,
                        };

                        group.Children.Add(lines[outer][inner].Model);
                    }
                }

                Visual3D visual = new ModelVisual3D()
                {
                    Content = group,
                };

                return (visual, lines);
            }

            private static Point3D? GetMousePosition3D(Point? mousePoint, bool isMouseDown, PerspectiveCamera camera, Viewport3D viewport)
            {
                if (!isMouseDown || mousePoint == null)
                {
                    return null;
                }

                var ray = UtilityWPF.RayFromViewportPoint(camera, viewport, mousePoint.Value);

                Triangle plane = new Triangle(new Point3D(0, 0, 0), new Point3D(1, 0, 0), new Point3D(0, 1, 0));

                return Math3D.GetIntersection_Plane_Ray(plane, ray.Origin, ray.Direction);
            }

            private static int FindClosestPoint(VectorND mousePos, VectorND[] points, int[] clickablePoints)
            {
                return clickablePoints.
                    Select(o => new
                    {
                        index = o,
                        distanceSqr = (points[o] - mousePos).LengthSquared,
                    }).
                    OrderBy(o => o.distanceSqr).
                    First().
                    index;
            }

            private static void UpdateTransformsColors(VectorND[] points, TranslateTransform3D[] translates, SolidColorBrush[] brushes, EdgeLink[][] links, BillboardLine3D[][] billboards, double[][] linkLengths, LatchJoint[] joints, int[] clickable, VisualizationColors colors)
            {
                LatchJoint[] latched = joints.
                    Where(o => o.IsLatched).
                    ToArray();

                for (int cntr = 0; cntr < points.Length; cntr++)
                {
                    Point3D point3D = points[cntr].ToPoint3D(false);

                    translates[cntr].OffsetX = point3D.X;
                    translates[cntr].OffsetY = point3D.Y;
                    translates[cntr].OffsetZ = point3D.Z;

                    if (latched.Any(o => o.Index0 == cntr || o.Index1 == cntr))
                    {
                        brushes[cntr].Color = Colors.OliveDrab;
                    }
                    else if (!clickable.Contains(cntr))
                    {
                        brushes[cntr].Color = colors.Gray;
                    }
                }

                for (int outer = 0; outer < links.Length; outer++)
                {
                    for (int inner = 0; inner < links[outer].Length; inner++)
                    {
                        billboards[outer][inner].FromPoint = points[links[outer][inner].Index0].ToPoint3D(false);
                        billboards[outer][inner].ToPoint = points[links[outer][inner].Index1].ToPoint3D(false);

                        billboards[outer][inner].Color = UtilityNOS.GetLinkColor(linkLengths[outer][inner], links[outer][inner].DesiredLength, colors);
                    }
                }
            }

            private static void UpdateLatchForces(VectorND[] forces, EdgeLink[][] links, LatchJoint[] joints, ref bool drawSnapshot)
            {
                var latchForces = new List<(LatchJoint joint, VectorND forceToward0)>();

                foreach (LatchJoint joint in joints)
                {
                    var force = UtilityNOS.GetLatchForce(joint.Point0, joint.Point1, joint.Distance_Latch, joint.Distance_Force, joint.MaxForce);

                    joint.IsLatched = false;

                    if (force.isInside)
                    {
                        if (drawSnapshot)
                        {
                            drawSnapshot = false;

                            //DrawSnapshot(forces, force.forceToward0, links, joint);
                            DrawSnapshot2(forces, joint);
                        }

                        joint.IsLatched = ShouldBeLatched(forces, joint);
                    }

                    if (!joint.IsLatched)
                    {
                        // Can't add to forces yet, or it will interfere with other latch joint calculations
                        latchForces.Add((joint, force.forceToward0));
                    }
                }

                foreach (var latchForce in latchForces)
                {
                    forces[latchForce.joint.Index0] += latchForce.forceToward0;
                    forces[latchForce.joint.Index1] -= latchForce.forceToward0;
                }
            }

            private static void DrawSnapshot(VectorND[] forces, VectorND latchForceToward0, EdgeLink[][] links, LatchJoint joint)
            {
                Debug3DWindow window = new Debug3DWindow();

                #region get lines for point

                var getLines = new Func<int, (VectorND, VectorND)[]>(i =>
                {
                    return links.
                        SelectMany(o => o).
                        Select(o =>
                        {
                            if (o.Index0 == i)
                            {
                                return (o.Point0, o.Point1);
                            }
                            else if (o.Index1 == i)
                            {
                                return (o.Point1, o.Point0);
                            }
                            else
                            {
                                return ((VectorND, VectorND)?)null;
                            }
                        }).
                        Where(o => o != null).
                        Select(o => o.Value).
                        ToArray();
                });

                var getLines3D = new Func<(VectorND, VectorND)[], (Point3D, Point3D)[]>(l =>
                    l.
                    Select(o => (o.Item1.ToPoint3D(false), o.Item2.ToPoint3D(false))).
                    ToArray());

                var getAvgLine = new Func<(VectorND, VectorND)[], VectorND>(a => MathND.GetCenter(a.Select(o => o.Item2 - o.Item1)));

                #endregion

                #region p0

                Point3D p0 = joint.Point0.ToPoint3D(false);
                window.AddDot(p0, DOT, Colors.DarkTurquoise);
                window.AddLine(p0, p0 + forces[joint.Index0].ToVector3D(false), LINE, Colors.Cyan);

                var lines = getLines(joint.Index0);
                if (lines.Length == 0)
                {
                    throw new ApplicationException("should never happen");
                }

                window.AddLines(getLines3D(lines), LINE, Colors.LightSeaGreen);

                VectorND avg0 = getAvgLine(lines);

                window.AddLine(p0, p0 + avg0.ToVector3D(false), LINE, Colors.DarkSlateGray);

                #endregion
                #region p1

                Point3D p1 = joint.Point1.ToPoint3D(false);
                window.AddDot(p1, DOT, Colors.DarkOrange);
                window.AddLine(p1, p1 + forces[joint.Index1].ToVector3D(false), LINE, Colors.Gold);

                lines = getLines(joint.Index1);
                if (lines.Length == 0)
                {
                    throw new ApplicationException("should never happen");
                }

                window.AddLines(getLines3D(lines), LINE, Colors.DarkGoldenrod);

                VectorND avg1 = getAvgLine(lines);

                window.AddLine(p1, p1 + avg1.ToVector3D(false), LINE, Colors.SaddleBrown);

                #endregion

                double dot0 = VectorND.DotProduct(forces[joint.Index0], avg0);
                double dot1 = VectorND.DotProduct(forces[joint.Index1], avg1);

                window.AddText($"dot0: {dot0.ToStringSignificantDigits(3)}");
                window.AddText($"dot1: {dot1.ToStringSignificantDigits(3)}");

                if ((dot0 > 0 && dot1 > 0) || (dot0 < 0 && dot1 < 0))
                {
                    window.AddText("dots say: possible to unlatch");

                    double dotForce = VectorND.DotProduct(forces[joint.Index0], forces[joint.Index1]);

                    window.AddText($"forces dot: {dotForce.ToStringSignificantDigits(3)}");

                    if (dotForce < 0)
                    {
                        window.AddText("forces oppose: possible to unlatch");

                        // Get the magnitude of each force that is directly opposed from each other
                        //NOTE: It doesn't matter what direction the along vectors point, just getting the magnitude (the dot product in
                        //the if statement tells they are in opposite directions).
                        VectorND forceAlongLatch0 = forces[joint.Index0].GetProjectedVector(latchForceToward0);
                        //VectorND forceAlongLatch1 = forces[joint.Index1].GetProjectedVector(latchForceToward0);
                        VectorND forceAlongLatch1 = forces[joint.Index1].GetProjectedVector(-latchForceToward0);


                        window.AddLine(new Point3D(), forceAlongLatch0.ToPoint3D(false), LINE, Colors.Black);
                        window.AddLine(new Point3D(), forceAlongLatch1.ToPoint3D(false), LINE, Colors.White);


                        //double magnitudeAlong0 = forceAlongLatch0.LengthSquared;
                        //double magnitudeAlong1 = forceAlongLatch1.LengthSquared;

                        //TODO: Figure out which of these to use
                        //double magnitudeAlong = forceAlongLatch0.LengthSquared + forceAlongLatch1.LengthSquared;
                        double magnitudeAlong = Math.Min(forceAlongLatch0.LengthSquared, forceAlongLatch1.LengthSquared) * 2;

                        // Take the min of those two forces
                        //      If <= joint.MaxForce, latch it
                        //      else break the latch
                        if (magnitudeAlong < joint.MaxForce * joint.MaxForce)
                        {
                            window.AddText("force isn't enough: latch it");
                        }
                        else
                        {
                            window.AddText("force is too much: break the latch");
                        }
                    }
                    else
                    {
                        window.AddText("forces aligned: can't unlatch");
                    }
                }
                else
                {
                    window.AddText("dots say: can't unlatch");
                }

                window.Show();
            }
            private static void DrawSnapshot2(VectorND[] forces, LatchJoint joint)
            {
                Debug3DWindow window = new Debug3DWindow();

                double dot = VectorND.DotProduct(forces[joint.Index0], forces[joint.Index1]);

                window.AddLine(new Point3D(), forces[joint.Index0].ToPoint3D(false), LINE, Colors.Orchid);
                window.AddLine(new Point3D(), forces[joint.Index1].ToPoint3D(false), LINE, Colors.Coral);

                VectorND v1onto2 = forces[joint.Index0].GetProjectedVector(forces[joint.Index1]);
                VectorND v2onto1 = forces[joint.Index1].GetProjectedVector(forces[joint.Index0]);

                window.AddDot(v1onto2.ToPoint3D(false), DOT, Colors.Orchid);
                window.AddDot(v2onto1.ToPoint3D(false), DOT, Colors.Coral);

                window.AddLine(v1onto2.ToPoint3D(false), v2onto1.ToPoint3D(false), LINE, Colors.White);

                double lenAlongs = (v2onto1 - v1onto2).Length;

                window.AddText($"dot: {dot.ToStringSignificantDigits(3)}");
                window.AddText($"len alongs: {lenAlongs.ToStringSignificantDigits(3)}");

                if (lenAlongs <= joint.MaxForce * 2)
                {
                    window.AddText("not enough force: latch them");
                }
                else
                {
                    window.AddText("too much force: break the latch");
                }

                window.Show();
            }

            //This is a copy of DrawSnapshot2
            private static bool ShouldBeLatched(VectorND[] forces, LatchJoint joint)
            {
                VectorND v1onto2 = forces[joint.Index0].GetProjectedVector(forces[joint.Index1]);
                VectorND v2onto1 = forces[joint.Index1].GetProjectedVector(forces[joint.Index0]);

                double lenAlongs = (v2onto1 - v1onto2).LengthSquared;

                if (lenAlongs <= (joint.MaxForce * 2) * (joint.MaxForce * 2))
                {
                    // Not enough force: latch them
                    return true;
                }
                else
                {
                    // Too much force: break the latch
                    return false;
                }
            }

            /// <summary>
            /// This updates the positions by force*mult*elapsed
            /// (caps the forces first)
            /// </summary>
            private static void ApplyForces(VectorND[] points, VectorND[] forces, double elapsedSeconds, double mult, double maxForce, LatchJoint[] joints)
            {
                // This was copied from UtilityNOS.ApplyForces

                mult *= elapsedSeconds;

                double maxForceSqr = maxForce * maxForce;

                List<int> unlatched = new List<int>(Enumerable.Range(0, points.Length));

                //TODO: Instead of scanning the entire mesh for possible islands every tick, do the big scan during initialization.  Then each tick,
                //see which joints within an island are currently latched
                LatchJoint[][] latchedSets = LatchJoint.GetLatchIslands(joints, true);

                foreach (LatchJoint[] set in latchedSets)
                {
                    int[] indices = LatchJoint.GetIndices(set);

                    foreach (int index in indices)
                    {
                        unlatched.Remove(index);
                    }

                    // Treat all the points in indicies like a single point
                    //NOTE: Latch force was never applied to these points (it's considered a hidden internal force that makes them all become a single point).  So forces[] is only external forces and needs to be added up
                    VectorND position = MathND.GetCenter(indices.Select(o => points[o]));
                    VectorND force = MathND.GetSum(indices.Select(o => forces[o]));

                    var result = ApplyForce(position, force, mult, maxForce, maxForceSqr);
                    foreach (int index in indices)
                    {
                        points[index] = result.position;
                        forces[index] = result.force;
                    }
                }

                foreach (int index in unlatched)
                {
                    //TODO: May want to borrow a trick from the chase forces that dampens forces orhogonal to the direction toward the other point.
                    //This reduces the chance of the points orbiting each other when they get really close together and forces get high

                    var result = ApplyForce(points[index], forces[index], mult, maxForce, maxForceSqr);
                    points[index] = result.position;
                    forces[index] = result.force;
                }
            }
            private static (VectorND position, VectorND force) ApplyForce(VectorND position, VectorND force, double mult, double maxForce, double maxForceSqr)
            {
                force *= mult;

                // Cap the force to avoid instability
                if (force.LengthSquared > maxForceSqr)
                {
                    force = force.ToUnit(false) * maxForce;
                }

                return (position + force, force);
            }

            private void BuildControls()
            {
                Button button = new Button()
                {
                    Content = "allow next latch snapshot view",
                    ToolTip = "The next time two points of a latch joint are within latching distance, this will draw the scene",
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(8),
                };

                button.Click += (s, e) => _drawSnapshot = true;

                _grid.Children.Add(button);
            }

            #endregion
        }

        #endregion
        #region class: LatchJointTester3

        private class LatchJointTester3 : ITimerVisualization
        {
            #region enum: ForceDropoffShape

            private enum ForceDropoffShape
            {
                Linear,
                Cosine,
            }

            #endregion
            #region class: AttachPoints

            private class AttachPoints
            {
                /// <summary>
                /// These are points that can be attached to
                /// </summary>
                public List<(LatchJoint joint, bool isZero)> Points { get; set; }

                public override string ToString()
                {
                    return Points.
                        Select(o => string.Format("{0} | {1}", o.joint, o.isZero ? "zero" : "one")).
                        ToJoin(" ~ ");
                }
            }

            #endregion

            #region Declaration Section

            private const double RADIUS = 10;

            private const double DOT = .1;
            private const double LINE = .02;

            private readonly Border _border;
            private readonly Viewport3D _viewport;
            private readonly PerspectiveCamera _camera;
            private readonly Grid _grid;
            private readonly VisualizationColors _colors;

            private Point? _mousePoint2D = null;
            private bool _isMouseDown = false;
            private int? _mouseDraggingIndex = null;
            private ITriangle _dragPlane = null;

            private bool _drawSnapshot = false;

            // Changing these doesn't affect the current simulation.  They are only looked at when Reset
            private int _numPoints = 15;
            private int _dimensions = 2;

            // ------------ Everything below is the currently running simulation ------------

            private double _maxForce = 0;

            // Points
            private VectorND[] _points;
            private int[] _clickablePoints;

            private Visual3D _dotVisual = null;
            private TranslateTransform3D[] _transforms;
            private SolidColorBrush[] _brushes;

            // Edges
            private EdgeLink[][] _links;

            private Visual3D _linesVisual = null;
            private BillboardLine3D[][] _billboards = null;

            // Latches
            private LatchJoint[][] _latches = null;

            #endregion

            public LatchJointTester3(Border border, Viewport3D viewport, PerspectiveCamera camera, Grid grid, VisualizationColors colors)
            {
                _border = border;
                _viewport = viewport;
                _camera = camera;
                _grid = grid;
                _colors = colors;

                BuildControls();

                // Event Listeners
                _border.MouseMove += Border_MouseMove;
                _border.MouseLeave += Border_MouseLeave;
                _border.MouseLeftButtonDown += Border_MouseLeftButtonDown;
                _border.MouseLeftButtonUp += Border_MouseLeftButtonUp;

                Reset();
            }

            public void Tick(double elapsedSeconds)
            {
                if (_points == null)
                {
                    return;
                }

                int dimensions = _points[0].Size;       // can't trust _dimensions, because the slider can play with the value and not hit the reset button yet

                VectorND[] forces = Enumerable.Range(0, _points.Length).
                    Select(o => new VectorND(dimensions)).
                    ToArray();

                double[][] linkLengths = _links.
                    Select(o => UtilityNOS.UpdateLinkForces(forces, o, _points, _linkMult_Compress, _linkMult_Expand)).
                    ToArray();

                if (_drawSnapshot)
                {
                    DrawSnapshot_ShouldLatch(_points, forces, _latches);
                }

                foreach (LatchJoint joint in _latches.SelectMany(o => o))
                {
                    joint.AdjustRandomBond();
                }

                // Calculate latch forces, maybe break/combine latches
                UpdateLatchForces(forces, _links, _latches);

                var mouse = GetMouseStatus(_isMouseDown, _mousePoint2D, _dimensions, _points, _clickablePoints, _mouseDraggingIndex, _dragPlane, _camera, _viewport);
                _mouseDraggingIndex = mouse.dragIndex;
                _dragPlane = mouse.dragPlane;

                VectorND[] points = _points.
                    Select(o => o.Clone()).
                    ToArray();

                //UtilityNOS.ApplyForces(points, forces, elapsedSeconds, MULT, _maxForce);
                ApplyForces(points, forces, elapsedSeconds, _speedMult, _maxForce, _latches, _drawSnapshot);

                if (mouse.mousePos != null && _mouseDraggingIndex != null)
                {
                    _points[_mouseDraggingIndex.Value] = mouse.mousePos.Value.ToVectorND(dimensions);
                }

                for (int cntr = 0; cntr < points.Length; cntr++)
                {
                    if (!_clickablePoints.Contains(cntr))
                    {
                        _points[cntr] = points[cntr];
                    }
                }

                UpdateTransformsColors(_points, _transforms, _brushes, _links, _billboards, linkLengths, new LatchJoint[0], _clickablePoints, _colors);

                _drawSnapshot = false;
            }

            public void Dispose()
            {
                Clear();

                _border.MouseMove -= Border_MouseMove;
                _border.MouseLeave -= Border_MouseLeave;

                _grid.Children.Clear();
            }

            private void Border_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
            {
                try
                {
                    _mousePoint2D = e.GetPosition(_border);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), nameof(LatchJointTester2), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            private void Border_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
            {
                try
                {
                    _mousePoint2D = null;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), nameof(LatchJointTester2), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            private void Border_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
            {
                _isMouseDown = true;
            }
            private void Border_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
            {
                _isMouseDown = false;
            }

            #region Properties

            private double _linkMult_Compress = 6;
            private double _linkMult_Expand = 6;

            private ForceDropoffShape _dropoffShape = ForceDropoffShape.Linear;
            private ForceDropoffShape DropoffShape
            {
                get
                {
                    return _dropoffShape;
                }
                set
                {
                    _dropoffShape = value;
                }
            }

            private double _latch_MaxForce = 3;
            private double Latch_MaxForce
            {
                get
                {
                    return _latch_MaxForce;
                }
                set
                {
                    _latch_MaxForce = value;

                    if (_latches != null)
                    {
                        foreach (LatchJoint joint in _latches.SelectMany(o => o))
                        {
                            joint.MaxForce = _latch_MaxForce;
                        }
                    }
                }
            }

            private double _latch_distance_inner = .03;
            private double Latch_Distance_Inner
            {
                get
                {
                    return _latch_distance_inner;
                }
                set
                {
                    _latch_distance_inner = value;

                    if (_latches != null)
                    {
                        foreach (LatchJoint joint in _latches.SelectMany(o => o))
                        {
                            joint.Distance_Latch = _latch_distance_inner;
                        }
                    }
                }
            }

            private double _latch_distance_outer = 3;
            private double Latch_Distance_Outer
            {
                get
                {
                    return _latch_distance_outer;
                }
                set
                {
                    _latch_distance_outer = value;

                    if (_latches != null)
                    {
                        foreach (LatchJoint joint in _latches.SelectMany(o => o))
                        {
                            joint.Distance_Force = _latch_distance_outer;
                        }
                    }
                }
            }

            private double _speedMult = .5;

            #endregion

            #region Private Methods

            private void Reset()
            {
                Clear();

                VectorND[] points = GetRandomPoints(_dimensions, _numPoints);

                var links = GetLinks(points, _dimensions, Latch_MaxForce, Latch_Distance_Inner, Latch_Distance_Outer);
                _points = links.points;
                _links = links.links;
                _clickablePoints = links.clickablePoints;
                _latches = links.latches;

                // Point Visuals
                var dots = GetDotVisual(_points, _clickablePoints, _colors);
                _dotVisual = dots.visual;
                _transforms = dots.transforms;
                _brushes = dots.brushes;
                _viewport.Children.Add(_dotVisual);

                // Link Visuals
                var lines = GetLineVisuals(_links, _colors);
                _linesVisual = lines.visual;
                _billboards = lines.lines;
                _viewport.Children.Add(_linesVisual);

                _maxForce = _links.SelectMany(o => o).Average(o => (o.Point1 - o.Point0).Length) * 4;
            }
            private void Clear()
            {
                if (_dotVisual != null)
                {
                    _viewport.Children.Remove(_dotVisual);
                    _dotVisual = null;
                }

                if (_linesVisual != null)
                {
                    _viewport.Children.Remove(_linesVisual);
                    _linesVisual = null;
                }
            }

            private static Color GetDotColor(VectorND point, bool isClickable, VisualizationColors colors)
            {
                if (isClickable)
                {
                    return colors.Gray;
                }
                else if (point.Size < 4)
                {
                    return colors.Zero;
                }
                else
                {
                    return UtilityNOS.GetDotColor(point, true, RADIUS, colors);
                }
            }

            #endregion
            #region Private Methods - build

            private void BuildControls()
            {
                StackPanel panel = new StackPanel()
                {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(8),
                };

                Grid.SetIsSharedSizeScope(panel, true);

                _grid.Children.Add(panel);

                #region Button: Reset

                Button button = new Button()
                {
                    Content = "Reset",
                    HorizontalAlignment = HorizontalAlignment.Left,
                };

                button.Click += (s, e) => Reset();

                panel.Children.Add(button);

                #endregion

                #region Expander: Create

                #region expander/panel

                Expander expanderGeneral = new Expander()
                {
                    Header = "Create",
                    IsExpanded = false,
                    Margin = new Thickness(0, 8, 0, 0),
                };

                panel.Children.Add(expanderGeneral);

                StackPanel panelGeneral = new StackPanel()
                {
                    Margin = new Thickness(8),
                };

                expanderGeneral.Content = panelGeneral;

                #endregion

                #region Radio: 2D/3D/4D

                // maybe, 3D would make choosing the drag plane complicated
                // 3D would allow for faces to be linked to each other instead of just edges
                // It wouldn't be that hard to calculate the drag plane, it's been done in other places in this solution:
                //      Plane is perpendicular to the camera's direction.  Point on the plane is the location of the dot that they started dragging around

                StackPanel panelDimensions = new StackPanel()
                {
                    HorizontalAlignment = HorizontalAlignment.Left,
                };

                panelGeneral.Children.Add(panelDimensions);

                RadioButton radio2D = new RadioButton()
                {
                    Content = "2D",
                    IsChecked = true,
                };

                panelDimensions.Children.Add(radio2D);

                RadioButton radio3D = new RadioButton()
                {
                    Content = "3D",
                    Margin = new Thickness(0, 2, 0, 0),
                };

                panelDimensions.Children.Add(radio3D);

                RadioButton radio4D = new RadioButton()
                {
                    Content = "4D",
                    Margin = new Thickness(0, 2, 0, 0),
                    IsEnabled = false,
                };

                panelDimensions.Children.Add(radio4D);

                var radioDimensionsClick = new Action(() =>
                {
                    _dimensions = radio2D.IsChecked.Value ?
                        2 :
                        radio3D.IsChecked.Value ?
                            3 :
                            4;
                });

                radio2D.Checked += (s, e) => radioDimensionsClick();
                radio2D.Unchecked += (s, e) => radioDimensionsClick();

                radio3D.Checked += (s, e) => radioDimensionsClick();
                radio3D.Unchecked += (s, e) => radioDimensionsClick();

                radio4D.Checked += (s, e) => radioDimensionsClick();
                radio4D.Unchecked += (s, e) => radioDimensionsClick();

                #endregion

                #region Slider: # points

                var numPoints = GetGrid_KeyValue_SharedSize("# points", false);
                panelGeneral.Children.Add(numPoints.grid);

                numPoints.slider.Minimum = 4;
                numPoints.slider.Maximum = 30;
                numPoints.slider.Value = 15;
                numPoints.slider.Interval = 1;
                numPoints.slider.IsSnapToTickEnabled = true;
                numPoints.slider.TickFrequency = 1;

                numPoints.slider.ValueChanged += (s, e) =>
                {
                    _numPoints = numPoints.slider.Value.ToInt_Round();
                    numPoints.label.Content = _numPoints.ToString();
                };

                _numPoints = numPoints.slider.Value.ToInt_Round();
                numPoints.label.Content = _numPoints.ToString();

                #endregion

                #endregion

                #region Expander: Edges

                #region expander/panel

                Expander expanderEdges = new Expander()
                {
                    Header = "Edges",
                    IsExpanded = false,
                    Margin = new Thickness(0, 8, 0, 0),
                };

                panel.Children.Add(expanderEdges);

                StackPanel panelEdges = new StackPanel()
                {
                    Margin = new Thickness(8),
                };

                expanderEdges.Content = panelEdges;

                #endregion

                #region Slider: Compression Mult

                var compMult = GetGrid_KeyValue_SharedSize("compress mult", false);
                panelEdges.Children.Add(compMult.grid);

                compMult.slider.Minimum = 1;
                compMult.slider.Maximum = 20;
                compMult.slider.Value = 6;

                compMult.slider.ValueChanged += (s, e) =>
                {
                    _linkMult_Compress = compMult.slider.Value;
                    compMult.label.Content = _linkMult_Compress.ToStringSignificantDigits(3);
                };

                _linkMult_Compress = compMult.slider.Value;
                compMult.label.Content = _linkMult_Compress.ToStringSignificantDigits(3);

                #endregion

                #region Slider: Expansion Mult

                var expandMult = GetGrid_KeyValue_SharedSize("expand mult", false);
                panelEdges.Children.Add(expandMult.grid);

                expandMult.slider.Minimum = 1;
                expandMult.slider.Maximum = 20;
                expandMult.slider.Value = 6;

                expandMult.slider.ValueChanged += (s, e) =>
                {
                    _linkMult_Expand = expandMult.slider.Value;
                    expandMult.label.Content = _linkMult_Expand.ToStringSignificantDigits(3);
                };

                _linkMult_Expand = expandMult.slider.Value;
                expandMult.label.Content = _linkMult_Expand.ToStringSignificantDigits(3);

                #endregion

                #endregion

                #region Expander: Latches

                #region expander/panel

                Expander expanderLatches = new Expander()
                {
                    Header = "Latches",
                    IsExpanded = false,
                    Margin = new Thickness(0, 8, 0, 0),
                };

                panel.Children.Add(expanderLatches);

                StackPanel panelLatches = new StackPanel()
                {
                    Margin = new Thickness(8),
                };

                expanderLatches.Content = panelLatches;

                #endregion

                #region Radio: linear or cosine for the latch force

                Label label = new Label()
                {
                    Content = "Force Dropoff:",
                };

                panelLatches.Children.Add(label);

                StackPanel panelForceShape = new StackPanel()
                {
                    HorizontalAlignment = HorizontalAlignment.Left,
                };

                panelLatches.Children.Add(panelForceShape);

                RadioButton radioLinear = new RadioButton()
                {
                    Content = "Linear",
                    IsChecked = true,
                };

                panelForceShape.Children.Add(radioLinear);

                RadioButton radioCosine = new RadioButton()
                {
                    Content = "Cosine",
                    Margin = new Thickness(0, 2, 0, 0),
                };

                panelForceShape.Children.Add(radioCosine);

                var radioShapeClick = new Action(() =>
                {
                    DropoffShape = radioCosine.IsChecked.Value ?
                        ForceDropoffShape.Cosine :
                        ForceDropoffShape.Linear;
                });

                radioLinear.Checked += (s, e) => radioShapeClick();
                radioLinear.Unchecked += (s, e) => radioShapeClick();

                radioCosine.Checked += (s, e) => radioShapeClick();
                radioCosine.Unchecked += (s, e) => radioShapeClick();

                #endregion

                #region Slider: Max Force

                var maxForce = GetGrid_KeyValue_SharedSize("max force", true);
                panelLatches.Children.Add(maxForce.grid);

                maxForce.slider.Minimum = 1;
                maxForce.slider.Maximum = 12;
                maxForce.slider.Value = 3;

                maxForce.slider.ValueChanged += (s, e) =>
                {
                    Latch_MaxForce = maxForce.slider.Value;
                    maxForce.label.Content = Latch_MaxForce.ToStringSignificantDigits(3);
                };

                Latch_MaxForce = maxForce.slider.Value;
                maxForce.label.Content = Latch_MaxForce.ToStringSignificantDigits(3);

                #endregion

                #region Slider: Inner Distance - the range of this should be pretty small

                var innerDistance = GetGrid_KeyValue_SharedSize("inner distance", true);
                panelLatches.Children.Add(innerDistance.grid);

                innerDistance.slider.Minimum = .005;
                innerDistance.slider.Maximum = .15;
                innerDistance.slider.Value = .03;

                innerDistance.slider.ValueChanged += (s, e) =>
                {
                    Latch_Distance_Inner = innerDistance.slider.Value;
                    innerDistance.label.Content = Latch_Distance_Inner.ToStringSignificantDigits(3);
                };

                Latch_Distance_Inner = innerDistance.slider.Value;
                innerDistance.label.Content = Latch_Distance_Inner.ToStringSignificantDigits(3);

                #endregion

                #region Slider: Outer Distance

                var outerDistance = GetGrid_KeyValue_SharedSize("outer distance", true);
                panelLatches.Children.Add(outerDistance.grid);

                outerDistance.slider.Minimum = .25;
                outerDistance.slider.Maximum = 6;
                outerDistance.slider.Value = 3;

                outerDistance.slider.ValueChanged += (s, e) =>
                {
                    Latch_Distance_Outer = outerDistance.slider.Value;
                    outerDistance.label.Content = Latch_Distance_Outer.ToStringSignificantDigits(3);
                };

                Latch_Distance_Outer = outerDistance.slider.Value;
                outerDistance.label.Content = Latch_Distance_Outer.ToStringSignificantDigits(3);

                #endregion

                #endregion

                #region Expander: Misc

                #region expander/panel

                Expander expanderMisc = new Expander()
                {
                    Header = "Misc",
                    IsExpanded = false,
                    Margin = new Thickness(0, 8, 0, 0),
                };

                panel.Children.Add(expanderMisc);

                StackPanel panelMisc = new StackPanel()
                {
                    Margin = new Thickness(8),
                };

                expanderMisc.Content = panelMisc;

                #endregion

                #region Slider: Speed Mult

                var speedMult = GetGrid_KeyValue_SharedSize("speed mult", false);
                panelMisc.Children.Add(speedMult.grid);

                speedMult.slider.Minimum = .01;
                speedMult.slider.Maximum = 2;
                speedMult.slider.Value = .5;

                speedMult.slider.ValueChanged += (s, e) =>
                {
                    _speedMult = speedMult.slider.Value;
                    speedMult.label.Content = _speedMult.ToStringSignificantDigits(2);
                };

                _speedMult = speedMult.slider.Value;
                speedMult.label.Content = _speedMult.ToStringSignificantDigits(2);

                #endregion

                #region Button: Snapshot

                button = new Button()
                {
                    Content = "allow snapshot",
                    ToolTip = "The next time two points of a latch joint are within latching distance, this will draw the scene",
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(0, 8, 0, 0),
                };

                button.Click += (s, e) => _drawSnapshot = true;

                panelMisc.Children.Add(button);

                #endregion

                #endregion

                // Session:  This way, stable configurations can be saved off
                // Combo Box
                // Save Button
                // Clear Button


                // Button: Report
                // This compares ratios of the various slider values, average length of the links and generates a report.  This should help with
                // figuring out programatically what settings to use

            }
            private static (Grid grid, Slider slider, Label label) GetGrid_KeyValue_SharedSize(string key, bool setVerticalMargin)
            {
                // Grid
                Grid grid = new Grid();

                if (setVerticalMargin)
                {
                    grid.Margin = new Thickness(0, 8, 0, 0);
                }

                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto), SharedSizeGroup = "KeyGroup" });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(8, GridUnitType.Pixel) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star), SharedSizeGroup = "ValueGroup" });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(8, GridUnitType.Pixel) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto), SharedSizeGroup = "LabelGroup" });

                // Left Label
                Label leftLabel = new Label()
                {
                    Content = key,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center,
                };

                Grid.SetColumn(leftLabel, 0);
                grid.Children.Add(leftLabel);

                // Slider
                Slider slider = new Slider()
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    MinWidth = 100,
                };

                Grid.SetColumn(slider, 2);
                grid.Children.Add(slider);

                // Right Label
                Label rightLabel = new Label()
                {
                    VerticalAlignment = VerticalAlignment.Center,
                };

                Grid.SetColumn(rightLabel, 4);
                grid.Children.Add(rightLabel);

                return (grid, slider, rightLabel);
            }

            private static VectorND[] GetRandomPoints(int dimensions, int count)
            {
                switch (dimensions)
                {
                    case 2:
                        return Math3D.GetRandomVectors_Circular_EvenDist(count, RADIUS).
                            Select(o => o.ToVectorND()).
                            ToArray();

                    case 3:
                        return Math3D.GetRandomVectors_Spherical_EvenDist(count, RADIUS).
                            Select(o => o.ToVectorND()).
                            ToArray();

                    case 4:
                        //TODO: Support even distribution in arbitrary dimensions
                        return MathND.GetRandomVectors_Spherical(dimensions, RADIUS, count);

                    default:
                        throw new ApplicationException($"Unexpected number of dimensions: {dimensions}");
                }
            }

            private static (EdgeLink[][] links, VectorND[] points, int[] clickablePoints) GetLinks_no_latch(VectorND[] points, int dimensions)
            {
                // Add some clickable points
                int numClickable;
                if (points.Length < 10)
                {
                    numClickable = 2;
                }
                else if (points.Length < 20)
                {
                    numClickable = 3;
                }
                else
                {
                    numClickable = 4;
                }

                VectorND[] clickable = GetClickable(dimensions, numClickable, RADIUS / 5);

                int[] clickableIndices = Enumerable.Range(0, numClickable).
                    Select(o => points.Length + o).
                    ToArray();

                VectorND[] allPoints = points.
                    Concat(clickable).
                    ToArray();

                // Delaunay
                var delaunay = GetDelaunay(dimensions, points);

                EdgeLink[][] links = delaunay.
                    Select(o => o.Select(p => new EdgeLink(p.Item1, p.Item2, allPoints, (points[p.Item2] - points[p.Item1]).Length)).ToArray()).
                    ToArray();

                //TODO: latch joints




                links = AttachClickable(links, clickable, clickableIndices, allPoints);

                int[] clickablePoints = Enumerable.Range(0, numClickable).
                    Select(o => points.Length + o).
                    ToArray();

                return (links, allPoints, clickablePoints);
            }
            private static (EdgeLink[][] links, LatchJoint[][] latches, VectorND[] points, int[] clickablePoints) GetLinks(VectorND[] points, int dimensions, double maxForce, double distance_Latch, double distance_Force)
            {
                // Add some clickable points
                int numClickable;
                if (points.Length < 8)
                {
                    numClickable = 2;
                }
                else if (points.Length < 13)
                {
                    numClickable = 3;
                }
                else if (points.Length < 20)
                {
                    numClickable = 4;
                }
                else
                {
                    numClickable = 6;
                }

                VectorND[] clickable = GetClickable(dimensions, numClickable, RADIUS / 5);

                int[] clickableIndices = Enumerable.Range(0, numClickable).
                    Select(o => points.Length + o).
                    ToArray();

                VectorND[] allPoints = points.
                    Concat(clickable).
                    ToArray();

                // Delaunay
                var delaunay = GetDelaunay(dimensions, points);




                var latches = GetLatches(delaunay, allPoints, maxForce, distance_Latch, distance_Force);
                delaunay = latches.delaunay;
                allPoints = latches.points;




                EdgeLink[][] links = delaunay.
                    Select(o => o.Select(p => new EdgeLink(p.Item1, p.Item2, allPoints, (allPoints[p.Item2] - allPoints[p.Item1]).Length)).ToArray()).
                    ToArray();





                links = AttachClickable(links, clickable, clickableIndices, allPoints);


                return (links, latches.latches, allPoints, clickableIndices);
            }

            private static (int, int)[][] GetDelaunay(int dimensions, VectorND[] points)
            {
                switch (dimensions)
                {
                    case 2:
                        TriangleIndexed[] triangles = Math2D.GetDelaunayTriangulation(points.Select(o => o.ToPoint()).ToArray(), points.Select(o => o.ToPoint3D(false)).ToArray());
                        return triangles.
                            Select(o => new[]
                            {
                                (o.Index0, o.Index1),
                                (o.Index1, o.Index2),
                                (o.Index2, o.Index0),
                            }).
                            ToArray();

                    case 3:
                        Tetrahedron[] tetras = Math3D.GetDelaunay(points.Select(o => o.ToPoint3D()).ToArray(), 3);
                        return tetras.
                            Select(o => o.EdgeArray.Select(p => (p.Item1, p.Item2)).ToArray()).
                            ToArray();

                    case 4:
                        //TODO: finish this
                        //MIConvexHull.DelaunayTriangulation
                        throw new ApplicationException("finish 4D delaunay");
                        break;

                    default:
                        throw new ApplicationException($"Unexpected dimensions {dimensions}");
                }
            }

            private static VectorND[] GetClickable(int dimensions, int count, double length)
            {
                double radius = RADIUS + length;

                switch (dimensions)
                {
                    case 2:
                        return Math3D.GetRandomVectors_CircularRing_EvenDist(count, radius).
                            Select(o => o.ToVectorND()).
                            ToArray();

                    case 3:
                        return Math3D.GetRandomVectors_SphericalShell_EvenDist(count, radius).
                            Select(o => o.ToVectorND()).
                            ToArray();

                    case 4:
                        return MathND.GetRandomVectors_Spherical_Shell_EvenDist(dimensions, radius, count);

                    default:
                        throw new ApplicationException($"Unexpected dimensions {dimensions}");
                }
            }

            private static EdgeLink[][] AttachClickable_ORIG(EdgeLink[][] links, VectorND[] points, VectorND[] clickable, VectorND[] allPoints)
            {
                List<EdgeLink>[] malleable = links.
                    Select(o => o.ToList()).
                    ToArray();

                List<int> previousPoints = new List<int>();

                for (int clickCntr = 0; clickCntr < clickable.Length; clickCntr++)
                {
                    // Find the point that is closest to this
                    int closest = points.
                        Select((o, i) => new
                        {
                            index = i,
                            distance = (o - clickable[clickCntr]).LengthSquared,
                        }).
                        Where(o => !previousPoints.Contains(o.index)).
                        OrderBy(o => o.distance).
                        First().
                        index;

                    previousPoints.Add(closest);

                    // Find the polygon that has this point
                    int polyIndex = links.
                        Select((o, i) => new
                        {
                            index = i,
                            hasIt = o.Any(p => p.Index0 == closest || p.Index1 == closest),
                        }).
                        Where(o => o.hasIt).
                        First().
                        index;

                    // Add an edge to this polygon
                    malleable[polyIndex].Add(new EdgeLink(closest, points.Length + clickCntr, allPoints, (clickable[clickCntr] - points[closest]).Length));
                }

                return malleable.
                    Select(o => o.ToArray()).
                    ToArray();
            }
            private static EdgeLink[][] AttachClickable(EdgeLink[][] links, VectorND[] clickable, int[] clickableIndices, VectorND[] allPoints)
            {
                List<EdgeLink>[] malleable = links.
                    Select(o => o.ToList()).
                    ToArray();

                List<int> previousPoints = new List<int>();

                for (int clickCntr = 0; clickCntr < clickable.Length; clickCntr++)
                {
                    // Find the point that is closest to this
                    int closest = allPoints.
                        Select((o, i) => new
                        {
                            index = i,
                            distance = (o - clickable[clickCntr]).LengthSquared,
                        }).
                        Where(o => !clickableIndices.Contains(o.index)).
                        Where(o => !previousPoints.Contains(o.index)).
                        OrderBy(o => o.distance).
                        First().
                        index;

                    previousPoints.Add(closest);

                    // Find the polygon that has this point
                    int polyIndex = links.
                        Select((o, i) => new
                        {
                            index = i,
                            hasIt = o.Any(p => p.Index0 == closest || p.Index1 == closest),
                        }).
                        Where(o => o.hasIt).
                        First().
                        index;

                    // Add an edge to this polygon
                    malleable[polyIndex].Add(new EdgeLink(closest, clickableIndices[clickCntr], allPoints, (clickable[clickCntr] - allPoints[closest]).Length));
                }

                return malleable.
                    Select(o => o.ToArray()).
                    ToArray();
            }

            private static ((int, int)[][] delaunay, VectorND[] points, LatchJoint[][] latches) GetLatches((int, int)[][] delaunay, VectorND[] points, double maxForce, double distance_Latch, double distance_Force)
            {
                (int, int)[][] newDel = delaunay.
                    Select(o => o.ToArray()).
                    ToArray();

                List<VectorND> newPts = new List<VectorND>(points);

                var latches = new List<(int, int)[]>();

                for (int cntr = 0; cntr < points.Length; cntr++)
                {
                    // Find all the edges that touch this point
                    var latchesForPoint = GetLatches_Point(cntr, newPts, newDel);

                    if (latchesForPoint?.Length > 0)
                    {
                        latches.Add(latchesForPoint);
                    }
                }

                VectorND[] newPoints = newPts.
                    ToArray();

                LatchJoint[][] latchJoints = latches.
                    Select(o => o.Select(p => new LatchJoint(p.Item1, p.Item2, newPoints, maxForce, distance_Latch, distance_Force, true)).ToArray()).
                    ToArray();

                return (newDel, newPoints, latchJoints);
            }
            private static (int, int)[] GetLatches_Point(int pointIndex, List<VectorND> points, (int, int)[][] delaunay)
            {
                List<int> pointsNamedSue = new List<int>();

                for (int cntr = 0; cntr < delaunay.Length; cntr++)
                {
                    if (pointsNamedSue.Count == 0 && delaunay[cntr].Any(o => o.Item1 == pointIndex || o.Item2 == pointIndex))
                    {
                        // This is the first poly to see this point.  Make a note of that and move to the next poly
                        pointsNamedSue.Add(pointIndex);
                        continue;
                    }

                    int? newPoint = GetLatches_Point_AddPoint(pointIndex, cntr, points, delaunay);
                    if (newPoint != null)
                    {
                        pointsNamedSue.Add(newPoint.Value);
                    }
                }

                // Create pairs that link all these versions of the same point together.  These will become latch joints
                // once all the new points are created
                return UtilityCore.GetPairs(pointsNamedSue.Count).
                    Select(o => (pointsNamedSue[o.Item1], pointsNamedSue[o.Item2])).
                    ToArray();
            }
            private static int? GetLatches_Point_AddPoint(int pointIndex, int polyIndex, List<VectorND> points, (int, int)[][] delaunay)
            {
                int? retVal = null;

                var getPointIndex = new Func<int>(() =>
                {
                    if (retVal == null)
                    {
                        points.Add(points[pointIndex].Clone());
                        retVal = points.Count - 1;
                    }

                    return retVal.Value;
                });

                for (int cntr = 0; cntr < delaunay[polyIndex].Length; cntr++)
                {
                    if (delaunay[polyIndex][cntr].Item1 == pointIndex)
                    {
                        // This references the point.  Create a new edge that references the new point
                        delaunay[polyIndex][cntr] = (getPointIndex(), delaunay[polyIndex][cntr].Item2);
                    }
                    else if (delaunay[polyIndex][cntr].Item2 == pointIndex)
                    {
                        // Same, but at Item2
                        delaunay[polyIndex][cntr] = (delaunay[polyIndex][cntr].Item1, getPointIndex());
                    }
                }

                return retVal;
            }

            private static (Visual3D visual, TranslateTransform3D[] transforms, SolidColorBrush[] brushes) GetDotVisual(VectorND[] points, int[] clickable, VisualizationColors colors)
            {
                Model3DGroup modelGroup = new Model3DGroup();
                TranslateTransform3D[] transforms = new TranslateTransform3D[points.Length];
                SolidColorBrush[] brushes = new SolidColorBrush[points.Length];

                for (int cntr = 0; cntr < points.Length; cntr++)
                {
                    bool isClickable = clickable.Contains(cntr);

                    brushes[cntr] = new SolidColorBrush(GetDotColor(points[cntr], isClickable, colors));

                    MaterialGroup material = new MaterialGroup();
                    material.Children.Add(new DiffuseMaterial(brushes[cntr]));
                    material.Children.Add(colors.Specular);

                    transforms[cntr] = new TranslateTransform3D(points[cntr].ToVector3D(false));

                    double size = isClickable ?
                        DOT * 1.5 :
                        DOT;

                    modelGroup.Children.Add(new GeometryModel3D()
                    {
                        Material = material,
                        BackMaterial = material,
                        Geometry = UtilityWPF.GetSphere_Ico(size, 1, true),
                        Transform = transforms[cntr],
                    });
                }

                Visual3D visual = new ModelVisual3D()
                {
                    Content = modelGroup,
                };

                return (visual, transforms, brushes);
            }
            private static (Visual3D visual, BillboardLine3D[][] lines) GetLineVisuals(EdgeLink[][] links, VisualizationColors colors)
            {
                BillboardLine3D[][] lines = new BillboardLine3D[links.Length][];

                Model3DGroup group = new Model3DGroup();

                for (int outer = 0; outer < links.Length; outer++)
                {
                    lines[outer] = new BillboardLine3D[links[outer].Length];

                    for (int inner = 0; inner < links[outer].Length; inner++)
                    {
                        lines[outer][inner] = new BillboardLine3D()
                        {
                            Thickness = LINE,
                            FromPoint = links[outer][inner].Point0.ToPoint3D(false),
                            ToPoint = links[outer][inner].Point1.ToPoint3D(false),
                            Color = colors.Zero,
                        };

                        group.Children.Add(lines[outer][inner].Model);
                    }
                }

                Visual3D visual = new ModelVisual3D()
                {
                    Content = group,
                };

                return (visual, lines);
            }

            #endregion
            #region Private Methods - tick

            private static (Point3D? mousePos, int? dragIndex, ITriangle dragPlane) GetMouseStatus(bool isMouseDown, Point? mousePoint2D, int dimensions, VectorND[] points, int[] clickablePoints, int? mouseDraggingIndex, ITriangle dragPlane, PerspectiveCamera camera, Viewport3D viewport)
            {
                if (!isMouseDown || mousePoint2D == null)
                {
                    return (null, null, null);
                }

                var ray = UtilityWPF.RayFromViewportPoint(camera, viewport, mousePoint2D.Value);

                if (mouseDraggingIndex != null && dragPlane != null)
                {
                    // They are currently dragging a point.  Find the new position
                    return
                    (
                        DragPoint(ray, mouseDraggingIndex.Value, dragPlane, points),
                        mouseDraggingIndex,
                        dragPlane
                    );
                }
                else
                {
                    // They are starting a new drag opertiation.  Find the nearest point to the ray, then come up with a drag plane
                    return StartDrag(ray, dimensions, points, clickablePoints);
                }
            }

            private static (Point3D? mousePos, int? dragIndex, ITriangle dragPlane) StartDrag(RayHitTestParameters ray, int dimensions, VectorND[] points, int[] clickableIndices)
            {
                const double MINDIST = RADIUS / 2;

                Point3D[] clickablePoints = clickableIndices.
                    Select(o => points[o].ToPoint3D(false)).
                    ToArray();

                if (dimensions == 2)
                {
                    return StartDrag2D(ray, points, clickableIndices, clickablePoints, MINDIST);
                }

                // Find the closest point to the ray
                var closest = clickablePoints.
                    Select((o, i) =>
                    {
                        Point3D pointOnLine = Math3D.GetClosestPoint_Line_Point(ray.Origin, ray.Direction, o);

                        return new
                        {
                            index = i,
                            pointOnLine,
                            dot = Vector3D.DotProduct(ray.Direction, pointOnLine - ray.Origin),
                            distanceSqr = (pointOnLine - o).LengthSquared,
                        };
                    }).
                    Where(o => o.dot > 0 && o.distanceSqr <= MINDIST).      // don't allow points behind the camera, or too far away from the click ray
                    OrderBy(o => o.distanceSqr).
                    FirstOrDefault();

                if (closest == null)
                {
                    return (null, null, null);
                }

                var dragPlane = Math3D.GetPlane(closest.pointOnLine, ray.Direction);

                return (closest.pointOnLine, clickableIndices[closest.index], dragPlane);
            }
            private static (Point3D? mousePos, int? dragIndex, Triangle dragPlane) StartDrag2D(RayHitTestParameters ray, VectorND[] points, int[] clickableIndices, Point3D[] clickablePoints, double minDistance)
            {
                Triangle plane = new Triangle(new Point3D(0, 0, 0), new Point3D(1, 0, 0), new Point3D(0, 1, 0));

                Point3D? intersect = Math3D.GetIntersection_Plane_Ray(plane, ray.Origin, ray.Direction);

                if (intersect == null)
                {
                    return (null, null, null);
                }

                //var closest = clickableIndices.
                var closest = Enumerable.Range(0, clickableIndices.Length).
                    Select(o => new
                    {
                        index = clickableIndices[o],
                        point = clickablePoints[o],
                        distanceSqr = (clickablePoints[o] - intersect.Value).LengthSquared,
                    }).
                    OrderBy(o => o.distanceSqr).
                    First();

                if (closest.distanceSqr > minDistance * minDistance)
                {
                    // A point was found, but it's too far away
                    return (null, null, null);
                }

                return (closest.point, closest.index, plane);
            }

            private static Point3D DragPoint(RayHitTestParameters ray, int mouseDraggingIndex, ITriangle dragPlane, VectorND[] points)
            {
                Point3D? intersect = Math3D.GetIntersection_Plane_Ray(dragPlane, ray.Origin, ray.Direction);

                if (intersect == null)
                {
                    // The only time this should happen is if the ray is parallel to the plane (which would be incredibly difficult
                    // to do because at the beginning of the drag operation, the plane was created perpendicular to the ray)

                    // Instead of nulling everything out, pretend that they are clicking exactly on the point.  That way the clicked
                    // index and drag plane are retained, and this will just be a momentary blip of inactivity during their drag
                    return points[mouseDraggingIndex].ToPoint3D(false);
                }
                else
                {
                    return intersect.Value;
                }
            }

            private static void UpdateLatchForces(VectorND[] forces, EdgeLink[][] links, LatchJoint[][] joints)
            {
                var latchForces = new List<(LatchJoint joint, VectorND forceToward0)>();

                foreach (LatchJoint joint in joints.SelectMany(o => o))
                {
                    //TODO: Linear vs Cosine
                    var force = UtilityNOS.GetLatchForce(joint.Point0, joint.Point1, joint.Distance_Latch, joint.Distance_Force, joint.MaxForce);

                    joint.IsLatched = false;

                    if (force.isInside)
                    {
                        joint.IsLatched = ShouldBeLatched(forces, joint);
                    }

                    if (!joint.IsLatched)
                    {
                        // Can't add to forces yet, or it will interfere with other latch joint calculations
                        latchForces.Add((joint, force.forceToward0));
                    }
                }

                foreach (var latchForce in latchForces)
                {
                    forces[latchForce.joint.Index0] += latchForce.forceToward0;
                    forces[latchForce.joint.Index1] -= latchForce.forceToward0;
                }
            }

            private static bool ShouldBeLatched(VectorND[] forces, LatchJoint joint)
            {
                VectorND v1onto2 = forces[joint.Index0].GetProjectedVector(forces[joint.Index1]);
                VectorND v2onto1 = forces[joint.Index1].GetProjectedVector(forces[joint.Index0]);

                double lenAlongs = (v2onto1 - v1onto2).LengthSquared;

                if (lenAlongs <= (joint.MaxForce * 2) * (joint.MaxForce * 2))
                {
                    // Not enough force: latch them
                    return true;
                }
                else
                {
                    // Too much force: break the latch
                    return false;
                }
            }

            /// <summary>
            /// This updates the positions by force*mult*elapsed
            /// (caps the forces first)
            /// </summary>
            private static void ApplyForces(VectorND[] points, VectorND[] forces, double elapsedSeconds, double mult, double maxForce, LatchJoint[][] joints, bool drawSnapshot)
            {
                // This was copied from UtilityNOS.ApplyForces

                mult *= elapsedSeconds;

                double maxForceSqr = maxForce * maxForce;

                List<int> unlatched = new List<int>(Enumerable.Range(0, points.Length));

                foreach (LatchJoint[] set in joints)
                {
                    //if (drawSnapshot)
                    //{
                    //    DrawSnapshot_ApplyLatchForces(set, unlatched, points, forces, mult, maxForce, maxForceSqr);
                    //}

                    ApplyForces_LatchSet(set, unlatched, points, forces, mult, maxForce, maxForceSqr, drawSnapshot);
                }

                foreach (int index in unlatched)
                {
                    //TODO: May want to borrow a trick from the chase forces that dampens forces orhogonal to the direction toward the other point.
                    //This reduces the chance of the points orbiting each other when they get really close together and forces get high

                    var result = ApplyForce(points[index], forces[index], mult, maxForce, maxForceSqr);
                    points[index] = result.position;
                    forces[index] = result.force;
                }
            }

            private static void ApplyForces_LatchSet_SLIGHTLYBETTER(LatchJoint[] set, List<int> unlatched, VectorND[] points, VectorND[] forces, double mult, double maxForce, double maxForceSqr)
            {
                // Set is a list of all the joints tied to a single point.  This only deals with the joints that are currently in a latched state
                LatchJoint[] latchedSet = set.
                    Where(o => o.IsLatched).
                    ToArray();

                if (latchedSet.Length == 0)
                {
                    return;
                }

                int[] indices = LatchJoint.GetIndices(latchedSet);

                foreach (int index in indices)
                {
                    unlatched.Remove(index);
                }

                // Treat all the points in indicies like a single point
                //NOTE: Latch force was never applied to these points (it's considered a hidden internal force that makes them all become a single point).  So forces[] is only external forces and needs to be added up
                VectorND position = MathND.GetCenter(indices.Select(o => points[o]));
                VectorND force = MathND.GetSum(indices.Select(o => forces[o]));

                var result = ApplyForce(position, force, mult, maxForce, maxForceSqr);
                foreach (int index in indices)
                {
                    points[index] = result.position;
                    forces[index] = result.force;
                }
            }
            private static void ApplyForces_LatchSet(LatchJoint[] set, List<int> unlatched, VectorND[] points, VectorND[] forces, double mult, double maxForce, double maxForceSqr, bool drawSnapshot)
            {
                // Set is a list of all the joints tied to a single point.  This only deals with the joints that are currently in a latched state
                if (set.All(o => !o.IsLatched))
                {
                    // All unlatched, nothing nothing special to do
                    return;
                }
                else if (set.All(o => o.IsLatched))
                {
                    ApplyForces_LatchSet_AllLatched(set, unlatched, points, forces, mult, maxForce, maxForceSqr);
                    return;
                }

                var islands = GetSetIslandsByPosition(set);

                foreach (var subset in islands.islands)
                {
                    ApplyForces_LatchSet_Subset(subset.joints, subset.center, islands.removed, unlatched, points, forces, mult, maxForce, maxForceSqr, drawSnapshot);
                }
            }

            private static void ApplyForces_LatchSet_AllLatched(LatchJoint[] set, List<int> unlatched, VectorND[] points, VectorND[] forces, double mult, double maxForce, double maxForceSqr)
            {
                int[] indices = LatchJoint.GetIndices(set);

                foreach (int index in indices)
                {
                    unlatched.Remove(index);
                }

                // Treat all the points in indicies like a single point
                //NOTE: Latch force was never applied to these points (it's considered a hidden internal force that makes them all become a single point).  So forces[] is only external forces and needs to be added up
                VectorND position = MathND.GetCenter(indices.Select(o => points[o]));
                VectorND force = MathND.GetSum(indices.Select(o => forces[o]));

                var result = ApplyForce(position, force, mult, maxForce, maxForceSqr);
                foreach (int index in indices)
                {
                    points[index] = result.position;
                    forces[index] = result.force;
                }
            }

            private static void ApplyForces_LatchSet_Subset(LatchJoint[] subset, VectorND center, LatchJoint[] partials, List<int> unlatched, VectorND[] points, VectorND[] forces, double mult, double maxForce, double maxForceSqr, bool drawSnapshot)
            {
                // Figure out which points should be together, and which should be apart

                // The latches that are open need to be allowed to be free from each other.  The remaining latched joints need to stick
                // to the closest free point

                // Use subset[9].RandomBond to see which to use



                //TODO: This is now working, rework the comments to go in the function's remarks
                //      ex: { 2-26 open }, { 2-25 closed } { 25-26 closed }
                //      2 was an attach point
                //      26 was an attach point
                //      
                //      the first attach point picked up all three indices (shouldn't have picked up 26)
                //      so when 26 ran in the lower loop (subsubset), it overwrote points[26] and forces[26] that was calculated from the previous iteration (the previous subsubset shouldn't have contained 26)



                // Get the points of open latches
                var attachPoints = ApplyForces_LatchSet_Subset_AttachPoints(subset, center, partials);

                List<LatchJoint> remainingLatched = new List<LatchJoint>(attachPoints.remainingLatched);

                while (remainingLatched.Count > 0)
                {
                    int index = 0;
                    while (index < remainingLatched.Count)
                    {
                        // Try to bind this to the best attach point
                        if (ApplyForces_LatchSet_Subset_Bind(attachPoints.attachPoints, remainingLatched[index]))
                        {
                            remainingLatched.RemoveAt(index);
                        }
                        else
                        {
                            index++;
                        }
                    }
                }

                // At this point, the latches have been divided among the attach points.
                //  Treat each attach point as an independent point.  Apply the latching logic to each
                foreach (var subsubset in attachPoints.attachPoints)
                {
                    ApplyForces_LatchSet_Subset_Subset(subsubset, unlatched, points, forces, mult, maxForce, maxForceSqr);
                }
            }
            private static bool ApplyForces_LatchSet_Subset_Bind(AttachPoints[] attachPoints, LatchJoint joint)
            {
                var bestMatch = attachPoints.
                    Select(o => new
                    {
                        attachPoint = o,
                        match = FindBestMatchingLatch(joint, o),
                    }).
                    Where(o => o.match != null).
                    OrderBy(o => o.match.Value.distanceSqr).
                    FirstOrDefault();

                if (bestMatch == null)
                {
                    return false;
                }

                // The joint passed in should always have both points as the same (should be in a latched state).  So add both points
                // as attachable
                if (!ContainsIndex(attachPoints, joint.Index0))
                {
                    bestMatch.attachPoint.Points.Add((joint, true));
                }

                if (!ContainsIndex(attachPoints, joint.Index1))
                {
                    bestMatch.attachPoint.Points.Add((joint, false));
                }

                return true;
            }
            private static void ApplyForces_LatchSet_Subset_Subset(/*LatchJoint[]*/ AttachPoints set, List<int> unlatched, VectorND[] points, VectorND[] forces, double mult, double maxForce, double maxForceSqr)
            {
                // This is a copy of ApplyForces_LatchSet_SLIGHTLYBETTER

                // Set is a list of all the joints tied to a single point.  This only deals with the joints that are currently in a latched state
                //LatchJoint[] latchedSet = set.
                //    Where(o => o.IsLatched).
                //    ToArray();

                //if (latchedSet.Length == 0)
                //{
                //    return;
                //}

                //int[] indices = LatchJoint.GetIndices(latchedSet);
                int[] indices = set.Points.
                    Select(o => o.joint.GetIndex(o.isZero)).
                    Distinct().
                    ToArray();

                foreach (int index in indices)
                {
                    unlatched.Remove(index);
                }

                // Treat all the points in indicies like a single point
                //NOTE: Latch force was never applied to these points (it's considered a hidden internal force that makes them all become a single point).  So forces[] is only external forces and needs to be added up
                VectorND position = MathND.GetCenter(indices.Select(o => points[o]));
                VectorND force = MathND.GetSum(indices.Select(o => forces[o]));

                var result = ApplyForce(position, force, mult, maxForce, maxForceSqr);
                foreach (int index in indices)
                {
                    points[index] = result.position;
                    forces[index] = result.force;
                }
            }

            private static bool ContainsIndex(AttachPoints[] attachPoints, int index)
            {
                foreach (AttachPoints set in attachPoints)
                {
                    if (set.Points.Any(o => o.joint.GetIndex(o.isZero) == index))
                    {
                        return true;
                    }
                }

                return false;
            }

            private static (LatchJoint joint, bool isZero, double distanceSqr)? FindBestMatchingLatch(LatchJoint joint, AttachPoints candidates)
            {
                // Zero
                int index = joint.GetIndex(true);

                var matches0 = candidates.Points.
                    Where(o => o.joint.GetIndex(o.isZero) == index).
                    ToArray();

                // One
                index = joint.GetIndex(false);

                var matches1 = candidates.Points.
                    Where(o => o.joint.GetIndex(o.isZero) == index).
                    ToArray();

                if (matches0.Length == 0 && matches1.Length == 0)
                {
                    // For some reason, the below linq statement is generating a tuple that contains default values: (null, false, 0).  So adding
                    // an explicit if statement
                    return null;
                }

                // Return the one that is closest to the joint
                return matches0.
                    Concat(matches1).
                    Select(o => (o.joint, o.isZero, (o.joint.RandomBondPos - joint.RandomBondPos).LengthSquared)).
                    OrderBy(o => o.LengthSquared).
                    FirstOrDefault();
            }

            /// <summary>
            /// Finds latches that are open and returns each endpoint of those latches that are close enough to center.
            /// All the other closed latches are returned in remainingLatched
            /// </summary>
            /// <returns>
            /// attachPoints: Each instance is one point (so an open latch would have two instances added - one for each endpoint)
            /// remainingLatched: The other closed latches
            /// </returns>
            private static (AttachPoints[] attachPoints, LatchJoint[] remainingLatched) ApplyForces_LatchSet_Subset_AttachPoints(LatchJoint[] subset, VectorND center, LatchJoint[] partials)
            {
                //TODO: This function was handed:
                //  subset = {48-49 closed}
                //  partials = {11-48 open}, {11-49 open}
                //
                //  this only created two attach points (11 and 49?) should have made 3 (11, 48, 49)
                // maybe there wasn't an error, should have continued debugging...
                //  also, I didn't consider which points were close to center and which weren't when writing this comment







                // Find the attach points
                var attachPoints = new List<(LatchJoint joint, bool isZero)>();
                var remainingLatched = new List<LatchJoint>();

                foreach (LatchJoint joint in subset)
                {
                    if (joint.IsLatched)
                    {
                        remainingLatched.Add(joint);
                    }
                    else
                    {
                        attachPoints.Add((joint, true));
                        attachPoints.Add((joint, false));
                    }
                }

                foreach (LatchJoint joint in partials)
                {
                    //if(joint.IsLatched)       // they will probably always be unlatched if they are in this list.  But that's not a hard enough requirement to put a constrain around (it won't hurt anything to bind to latched points)
                    //{
                    //    continue;
                    //}

                    if ((joint.Point0 - center).LengthSquared < joint.Distance_Latch * joint.Distance_Latch)
                    {
                        attachPoints.Add((joint, true));
                    }
                    else if ((joint.Point1 - center).LengthSquared < joint.Distance_Latch * joint.Distance_Latch)
                    {
                        attachPoints.Add((joint, false));
                    }
                }

                return
                (
                    attachPoints.
                        Select(o => new AttachPoints()
                        {
                            Points = new List<(LatchJoint joint, bool isZero)>(new[] { o }),
                        }).
                        ToArray(),

                    remainingLatched.
                        ToArray()
                );
            }

            private static (VectorND position, VectorND force) ApplyForce(VectorND position, VectorND force, double mult, double maxForce, double maxForceSqr)
            {
                force *= mult;

                // Cap the force to avoid instability
                if (force.LengthSquared > maxForceSqr)
                {
                    force = force.ToUnit(false) * maxForce;
                }

                return (position + force, force);
            }

            private static void UpdateTransformsColors(VectorND[] points, TranslateTransform3D[] translates, SolidColorBrush[] brushes, EdgeLink[][] links, BillboardLine3D[][] billboards, double[][] linkLengths, LatchJoint[] joints, int[] clickable, VisualizationColors colors)
            {
                LatchJoint[] latched = joints.
                    Where(o => o.IsLatched).
                    ToArray();

                for (int cntr = 0; cntr < points.Length; cntr++)
                {
                    Point3D point3D = points[cntr].ToPoint3D(false);

                    translates[cntr].OffsetX = point3D.X;
                    translates[cntr].OffsetY = point3D.Y;
                    translates[cntr].OffsetZ = point3D.Z;

                    if (latched.Any(o => o.Index0 == cntr || o.Index1 == cntr))
                    {
                        brushes[cntr].Color = Colors.OliveDrab;
                    }
                    else if (!clickable.Contains(cntr))
                    {
                        brushes[cntr].Color = GetDotColor(points[cntr], false, colors);
                    }
                }

                for (int outer = 0; outer < links.Length; outer++)
                {
                    for (int inner = 0; inner < links[outer].Length; inner++)
                    {
                        billboards[outer][inner].FromPoint = points[links[outer][inner].Index0].ToPoint3D(false);
                        billboards[outer][inner].ToPoint = points[links[outer][inner].Index1].ToPoint3D(false);

                        billboards[outer][inner].Color = UtilityNOS.GetLinkColor(linkLengths[outer][inner], links[outer][inner].DesiredLength, colors);
                    }
                }
            }

            private static void DrawSnapshot_ShouldLatch(VectorND[] points, VectorND[] forces, LatchJoint[][] latches)
            {
                Debug3DWindow window = new Debug3DWindow();

                LatchJoint[] largestSet = latches.OrderByDescending(o => o.Length).First();

                int[] indices = largestSet.
                    Select(o => o.Index0).
                    Concat(largestSet.Select(o => o.Index1)).
                    Distinct().
                    ToArray();

                Color[] colors = UtilityWPF.GetRandomColors(indices.Length, 64, 220);

                // Draw points and forces
                for (int cntr = 0; cntr < indices.Length; cntr++)
                {
                    window.AddDot(points[indices[cntr]].ToPoint3D(false), DOT, colors[cntr]);
                    window.AddLine(points[indices[cntr]].ToPoint3D(false), (points[indices[cntr]] + forces[indices[cntr]]).ToPoint3D(false), LINE, colors[cntr]);
                }

                // Create a report of indices used and force felt for each pair
                foreach (LatchJoint joint in largestSet)
                {
                    VectorND v1onto2 = forces[joint.Index0].GetProjectedVector(forces[joint.Index1]);
                    VectorND v2onto1 = forces[joint.Index1].GetProjectedVector(forces[joint.Index0]);
                    double lenAlongs = (v2onto1 - v1onto2).Length;

                    window.AddText(string.Format("{0} - {1} | f0={2} - f1={3} | along={4} max={5} shouldLatch={6} | isLatched={7}",
                        joint.Index0,
                        joint.Index1,
                        forces[joint.Index0].Length.ToStringSignificantDigits(2),
                        forces[joint.Index1].Length.ToStringSignificantDigits(2),
                        lenAlongs.ToStringSignificantDigits(2),
                        joint.MaxForce.ToStringSignificantDigits(2),
                        lenAlongs < joint.MaxForce * 2 ? "latch" : "break",
                        joint.IsLatched
                        ));
                }

                window.Show();

                window = new Debug3DWindow();

                Point[] circlePositions = Math2D.GetCircle_Cached(indices.Length);

                double z = .33;

                for (int cntr = 0; cntr < indices.Length; cntr++)
                {
                    window.AddDot(circlePositions[cntr].ToPoint3D(), DOT, colors[cntr]);

                    window.AddLine(circlePositions[cntr].ToPoint3D(-z), circlePositions[cntr].ToPoint3D(-z) + forces[indices[cntr]].ToVector3D(false), LINE, colors[cntr]);

                    window.AddText3D(indices[cntr].ToString(), circlePositions[cntr].ToPoint3D(z), new Vector3D(0, 0, 1), .2, colors[cntr], false);
                }

                foreach (LatchJoint joint in largestSet)
                {
                    int localIndex0 = indices.IndexOf(joint.Index0);
                    int localIndex1 = indices.IndexOf(joint.Index1);

                    window.AddLine(
                        circlePositions[localIndex0].ToPoint3D(),
                        circlePositions[localIndex1].ToPoint3D(),
                        LINE,
                        joint.IsLatched ? Colors.Tomato : Colors.DarkSeaGreen);
                }

                window.Show();
            }
            private static void DrawSnapshot_ApplyLatchForces(LatchJoint[] set, List<int> unlatched, VectorND[] points, VectorND[] forces, double mult, double maxForce, double maxForceSqr)
            {
                // Look for islands by grouping by points.  Threshold is latch.InnerDistance
                // Those islands need to be processed independently from each other
                //      If there are more than one island, then latches connecting those islands should be in an unlatched state.  Those unlatched bridges need to be thrown out
                //          thrown out, but still apply any forces felt - just add to unlatched, and let the calling fuction handle those points like normal - I think?

                var islands = GetSetIslandsByPosition(set);


                if (islands.islands.Length > 1)
                {
                    // Draw this


                }



                foreach (var subset in islands.islands)
                {
                    DrawSnapshot_ApplyLatchForces_Subset(subset.joints, subset.center, unlatched, points, forces, mult, maxForce, maxForceSqr);
                }


            }
            private static void DrawSnapshot_ApplyLatchForces_Subset(LatchJoint[] set, VectorND center, List<int> unlatched, VectorND[] points, VectorND[] forces, double mult, double maxForce, double maxForceSqr)
            {
                if (set.Length < 2)
                {
                    // Not enough latches to be a challenge worth drawing
                    return;
                }
                else if (set.All(o => !o.IsLatched))
                {
                    // All unlatched, nothing interesting to show
                    return;
                }
                else if (set.All(o => o.IsLatched))
                {
                    // All latched, nothing interesting to show
                    return;
                }

                // Some are latched, some aren't



                // I think this statement isn't needed any more, because the island function took care of this
                //
                // Look for individual points within this set where all links to it are unlatched.  These are truly free and can be processed normally as unlatched
                //      Also, throw out those latches from the rest of this function's processing?  I don't think they would add any value






                // Points that have a double latch definitely need to be stuck together

                // Points that have an unlatch need to?:
                //      distribute some force to see if the bond is strong enough to stick?
                //      the two points that are unlatched need to distribute forces into the ball to see what should stick to what?


            }

            /// <summary>
            /// This divides the set based on physical position.  Subsets that are more than inner distance away from each other are put
            /// in separate groups
            /// </summary>
            /// <remarks>
            /// The set passed in should all belong to the same initial point.  Over time, some versions of that point could break apart
            /// and there could be several latched subsets that are located
            /// 
            /// If there is more than one island, then latches connecting those islands should be in an unlatched state.  Those unlatched
            /// bridges need to be thrown out from the sub islands, but forces still need to be applied
            /// </remarks>
            private static (LatchJoint[][] islands, LatchJoint[] removed) GetSetIslandsByPosition_ONLYTWO(LatchJoint[] set)
            {
                // Remove individual joints whose points are too far from each other
                var removed = new List<LatchJoint>();
                var remaining = new List<LatchJoint>();

                foreach (LatchJoint joint in set)
                {
                    if ((joint.Point0 - joint.Point1).LengthSquared <= joint.Distance_Latch * joint.Distance_Latch)
                    {
                        remaining.Add(joint);
                    }
                    else
                    {
                        removed.Add(joint);
                    }
                }

                var retVal = new List<LatchJoint[]>();

                while (remaining.Count > 0)
                {
                    List<LatchJoint> island = new List<LatchJoint>();

                    island.Add(remaining[0]);
                    remaining.RemoveAt(0);

                    List<VectorND> matchedPoints = new List<VectorND>();

                    int index = 0;
                    while (index < remaining.Count)
                    {
                        // Latch distance should be the same for all latches, but just in case it's not, take the min
                        double latchDist = Math.Min(island[0].Distance_Latch, remaining[index].Distance_Latch);
                        latchDist *= latchDist;

                        if ((island[0].Point0 - remaining[index].Point0).LengthSquared <= latchDist)
                        {
                            island.Add(remaining[index]);
                            remaining.RemoveAt(index);
                        }
                        else if ((island[0].Point1 - remaining[index].Point1).LengthSquared <= latchDist)
                        {
                            island.Add(remaining[index]);
                            remaining.RemoveAt(index);
                        }
                        else
                        {
                            index++;
                        }
                    }

                    retVal.Add(island.ToArray());
                }

                return (retVal.ToArray(), removed.ToArray());
            }
            private static ((LatchJoint[] joints, VectorND center)[] islands, LatchJoint[] removed) GetSetIslandsByPosition(LatchJoint[] set)
            {
                // Remove individual joints whose points are too far from each other
                var removed = new List<LatchJoint>();
                var remaining = new List<LatchJoint>();

                foreach (LatchJoint joint in set)
                {
                    if ((joint.Point0 - joint.Point1).LengthSquared <= joint.Distance_Latch * joint.Distance_Latch)
                    {
                        remaining.Add(joint);
                    }
                    else
                    {
                        removed.Add(joint);
                    }
                }

                var retVal = new List<(LatchJoint[], VectorND)>();

                while (remaining.Count > 0)
                {
                    List<LatchJoint> island = new List<LatchJoint>();

                    island.Add(remaining[0]);
                    remaining.RemoveAt(0);

                    VectorND[] pts0 = new[] { island[0].Point0, island[0].Point1 };

                    List<VectorND> matchedPoints = new List<VectorND>();
                    matchedPoints.AddRange(pts0);

                    int index = 0;
                    while (index < remaining.Count)
                    {
                        // Latch distance should be the same for all latches, but just in case it's not, take the min
                        double latchDist = Math.Min(island[0].Distance_Latch, remaining[index].Distance_Latch);
                        latchDist *= latchDist;

                        VectorND[] pts1 = new[] { remaining[index].Point0, remaining[index].Point1 };

                        var matches = UtilityCore.Collate(pts0, pts1).
                            Where(o => (o.Item1 - o.Item2).LengthSquared <= latchDist).
                            ToArray();

                        if (matches.Length > 0)
                        {
                            matchedPoints.AddRange(matches.SelectMany(o => new[] { o.Item1, o.Item2 }));

                            island.Add(remaining[index]);
                            remaining.RemoveAt(index);
                        }
                        else
                        {
                            index++;
                        }
                    }

                    retVal.Add((island.ToArray(), MathND.GetCenter(matchedPoints)));
                }

                return (retVal.ToArray(), removed.ToArray());
            }

            #endregion
        }

        #endregion
        #region class: CollapseSphere3D

        private class CollapseSphere3D : ITimerVisualization
        {
            #region class: PointVisuals

            private class PointVisuals
            {
                #region Declaration Section

                private readonly VectorND[] _origPoints;     // these are the original colors
                private readonly VectorND[] _points;

                private readonly TranslateTransform3D[] _translates;

                private readonly SolidColorBrush[] _brushes;      // these are the original color + alpha (only have alpha if AlwaysVisible is unchecked)

                private readonly VisualizationColors _colors;

                private readonly double _slicePlane;

                #endregion

                #region Constructor

                public PointVisuals(VectorND[] origPoints, VectorND[] points, VisualizationColors colors, double slicePlane)
                {
                    _origPoints = origPoints;
                    _points = points;
                    _colors = colors;
                    _slicePlane = slicePlane;

                    var visuals = GetVisuals(_origPoints);
                    _brushes = visuals.brushes;
                    _translates = visuals.translates;

                    Visual = new ModelVisual3D()
                    {
                        Content = visuals.modelGroup,
                    };

                    MoveVisuals(_points, _translates);
                    ColorVisuals(_points, _origPoints, _brushes, RADIUS, _colors, _slicePlane, false);
                }

                #endregion

                public readonly Visual3D Visual;

                public void Tick(bool alwaysVisible)
                {
                    MoveVisuals(_points, _translates);
                    ColorVisuals(_points, _origPoints, _brushes, RADIUS, _colors, _slicePlane, alwaysVisible);
                }

                #region Private Methods

                private static (Model3DGroup modelGroup, TranslateTransform3D[] translates, SolidColorBrush[] brushes) GetVisuals(VectorND[] positions)
                {
                    TranslateTransform3D[] translates = new TranslateTransform3D[positions.Length];
                    SolidColorBrush[] brushes = new SolidColorBrush[positions.Length];

                    Model3DGroup modelGroup = new Model3DGroup();

                    MeshGeometry3D dotGeometry = UtilityWPF.GetSphere_Ico(DOT, 1, true);

                    for (int cntr = 0; cntr < positions.Length; cntr++)
                    {
                        MaterialGroup material = new MaterialGroup();

                        brushes[cntr] = new SolidColorBrush(Colors.Black);
                        material.Children.Add(new DiffuseMaterial(brushes[cntr]));
                        //material.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("50AAAAAA")), 2));

                        translates[cntr] = new TranslateTransform3D();

                        modelGroup.Children.Add(new GeometryModel3D()
                        {
                            Material = material,
                            BackMaterial = material,
                            Geometry = dotGeometry,
                            Transform = translates[cntr],
                        });
                    }

                    return (modelGroup, translates, brushes);
                }

                private static void MoveVisuals(VectorND[] points, TranslateTransform3D[] translates)
                {
                    for (int cntr = 0; cntr < points.Length; cntr++)
                    {
                        //TODO: Have a function that maps the VectorND into 3D.  The current way is simply only taking the first 3 coords, but it might be more
                        //illuminating to allow custom rotations (especially if in vr and those rotations are mapped to the head's position/orientation)
                        Vector3D position = points[cntr].ToVector3D(false);

                        translates[cntr].OffsetX = position.X;
                        translates[cntr].OffsetY = position.Y;
                        translates[cntr].OffsetZ = position.Z;
                    }
                }

                private static void ColorVisuals(VectorND[] points, VectorND[] origPoints, SolidColorBrush[] brushes, double radius, VisualizationColors colors, double slicePlane, bool alwaysVisible)
                {
                    for (int cntr = 0; cntr < points.Length; cntr++)
                    {
                        Color color = UtilityNOS.GetDotColor(origPoints[cntr], true, radius, colors);

                        if (!alwaysVisible)
                        {
                            double alpha = UtilityNOS.GetAlphaPercent(points[cntr], radius, slicePlane);
                            color = UtilityWPF.AlphaBlend(color, Colors.Transparent, alpha);
                        }

                        brushes[cntr].Color = color;
                    }
                }

                #endregion
            }

            #endregion
            #region class: LineVisuals

            private class LineVisuals
            {
                #region Declaration Section

                private readonly VisualizationColors _colors;
                private readonly double _slicePlane;

                private readonly EdgeLink[][] _links;
                private readonly BillboardLine3D[][] _billboards;

                #endregion

                #region Constructor

                public LineVisuals(EdgeLink[][] links, VisualizationColors colors, double slicePlane)
                {
                    _links = links;
                    _colors = colors;
                    _slicePlane = slicePlane;

                    var visuals = GetVisuals(links, colors);
                    _billboards = visuals.lines;

                    Visual = visuals.visual;
                }

                #endregion

                public readonly Visual3D Visual;

                public void Tick(bool alwaysVisible, double[][] linkLengths)
                {
                    for (int outer = 0; outer < _links.Length; outer++)
                    {
                        for (int inner = 0; inner < _links[outer].Length; inner++)
                        {
                            _billboards[outer][inner].FromPoint = _links[outer][inner].Point0.ToPoint3D(false);
                            _billboards[outer][inner].ToPoint = _links[outer][inner].Point1.ToPoint3D(false);

                            Color color = UtilityNOS.GetLinkColor(linkLengths[outer][inner], _links[outer][inner].DesiredLength, _colors);

                            if (alwaysVisible)
                            {
                                _billboards[outer][inner].Color = color;
                                if (_billboards[outer][inner].ColorTo != null)
                                {
                                    _billboards[outer][inner].ColorTo = null;
                                }
                            }
                            else
                            {
                                double alpha = UtilityNOS.GetAlphaPercent(_links[outer][inner].Point0, RADIUS, _slicePlane);
                                _billboards[outer][inner].Color = UtilityWPF.AlphaBlend(color, Colors.Transparent, alpha);

                                alpha = UtilityNOS.GetAlphaPercent(_links[outer][inner].Point1, RADIUS, _slicePlane);
                                _billboards[outer][inner].ColorTo = UtilityWPF.AlphaBlend(color, Colors.Transparent, alpha);
                            }
                        }
                    }
                }

                #region Private Methods

                private static (Visual3D visual, BillboardLine3D[][] lines) GetVisuals(EdgeLink[][] links, VisualizationColors colors)
                {
                    BillboardLine3D[][] lines = new BillboardLine3D[links.Length][];

                    Model3DGroup group = new Model3DGroup();

                    for (int outer = 0; outer < links.Length; outer++)
                    {
                        lines[outer] = new BillboardLine3D[links[outer].Length];

                        for (int inner = 0; inner < links[outer].Length; inner++)
                        {
                            lines[outer][inner] = new BillboardLine3D()
                            {
                                Thickness = LINE,
                                FromPoint = links[outer][inner].Point0.ToPoint3D(false),
                                ToPoint = links[outer][inner].Point1.ToPoint3D(false),
                                Color = colors.Zero,
                            };

                            group.Children.Add(lines[outer][inner].Model);
                        }
                    }

                    Visual3D visual = new ModelVisual3D()
                    {
                        Content = group,
                    };

                    return (visual, lines);
                }

                #endregion
            }

            #endregion
            #region class: FaceVisuals

            private class FaceVisuals
            {
                #region class: TriangleModel

                public class TriangleModel
                {
                    public GeometryModel3D Model { get; set; }
                    public MeshGeometry3D Geometry { get; set; }

                    public int[] Indices { get; set; }
                    public bool IsStandardNormal { get; set; }
                }

                #endregion

                #region Declaration Section

                private readonly VisualizationColors _colors;
                private readonly double _slicePlane;

                private readonly EdgeLink[][] _links;
                private readonly VectorND[] _points;

                private readonly Model3DGroup _group;
                public readonly TriangleModel[] _models;

                private readonly Color _colorFront = UtilityWPF.ColorFromHex("656970");
                private readonly Color _colorBack = UtilityWPF.ColorFromHex("8C6A5E");

                private bool _currentlyAlwaysVisible = true;

                #endregion

                #region Constructor

                public FaceVisuals(EdgeLink[][] links, VectorND[] points, VisualizationColors colors, double slicePlane)
                {
                    _links = links;
                    _points = points;
                    _colors = colors;
                    _slicePlane = slicePlane;

                    var visuals = GetVisuals(_links, _points, _colors, _colorFront, _colorBack);
                    Visual = visuals.visual;
                    _group = visuals.group;
                    _models = visuals.models;
                }

                #endregion

                public readonly Visual3D Visual;

                public void Tick(bool alwaysVisible)
                {
                    foreach (TriangleModel model in _models)
                    {
                        if (model.Indices.Length == 3)
                        {
                            Point3D p0 = _points[model.Indices[0]].ToPoint3D(false);
                            Point3D p1 = _points[model.Indices[1]].ToPoint3D(false);
                            Point3D p2 = _points[model.Indices[2]].ToPoint3D(false);

                            model.Geometry.Positions[0] = p0;
                            model.Geometry.Positions[1] = p1;
                            model.Geometry.Positions[2] = p2;

                            // This was copied from Triangle.Normal
                            Vector3D dir1 = p0 - p1;
                            Vector3D dir2 = p2 - p1;

                            Vector3D triangleNormal = Vector3D.CrossProduct(dir2, dir1);
                            if (!model.IsStandardNormal)
                            {
                                triangleNormal = -triangleNormal;
                            }

                            model.Geometry.Normals[0] = triangleNormal;
                            model.Geometry.Normals[1] = triangleNormal;
                            model.Geometry.Normals[2] = triangleNormal;
                        }
                        else
                        {
                            // Can't change the normal, so the triangles will be shaded according to the initial creation
                            for (int cntr = 0; cntr < model.Indices.Length; cntr++)
                            {
                                model.Geometry.Positions[cntr] = _points[model.Indices[cntr]].ToPoint3D(false);
                            }
                        }
                    }

                    if (alwaysVisible && _currentlyAlwaysVisible)
                    {
                        return;
                    }

                    if (alwaysVisible)
                    {
                        #region always visible

                        _group.Children.Clear();

                        DiffuseMaterial materialFront = new DiffuseMaterial(new SolidColorBrush(_colorFront));
                        DiffuseMaterial materialBack = new DiffuseMaterial(new SolidColorBrush(_colorBack));

                        foreach (TriangleModel model in _models)
                        {
                            if (model.IsStandardNormal)
                            {
                                model.Model.Material = materialFront;
                                model.Model.BackMaterial = materialBack;
                            }
                            else
                            {
                                model.Model.Material = materialBack;
                                model.Model.BackMaterial = materialFront;
                            }

                            _group.Children.Add(model.Model);
                        }

                        #endregion
                    }
                    else
                    {
                        #region partially visible

                        _group.Children.Clear();

                        foreach (TriangleModel model in _models)
                        {
                            double alpha0 = UtilityNOS.GetAlphaPercent(_points[model.Indices[0]], RADIUS, _slicePlane);
                            double alpha1 = UtilityNOS.GetAlphaPercent(_points[model.Indices[1]], RADIUS, _slicePlane);
                            double alpha2 = UtilityNOS.GetAlphaPercent(_points[model.Indices[2]], RADIUS, _slicePlane);

                            if (alpha0.IsNearZero() && alpha1.IsNearZero() && alpha2.IsNearZero())
                            {
                                continue;
                            }

                            _group.Children.Add(UtilityWPF.GetGradientTriangle
                            (
                                _points[model.Indices[0]].ToPoint3D(false),
                                _points[model.Indices[1]].ToPoint3D(false),
                                _points[model.Indices[2]].ToPoint3D(false),
                                UtilityWPF.AlphaBlend(_colorFront, Colors.Transparent, alpha0),     // because of semitransparency, backmaterial isn't used
                                UtilityWPF.AlphaBlend(_colorFront, Colors.Transparent, alpha1),
                                UtilityWPF.AlphaBlend(_colorFront, Colors.Transparent, alpha2),
                                false
                            ));
                        }

                        #endregion
                    }

                    _currentlyAlwaysVisible = alwaysVisible;
                }

                #region Private Methods

                private static (Visual3D visual, Model3DGroup group, TriangleModel[] models) GetVisuals(EdgeLink[][] links, VectorND[] points, VisualizationColors colors, Color colorFront, Color colorBack)
                {
                    Model3DGroup modelGroup = new Model3DGroup();

                    DiffuseMaterial materialFront = new DiffuseMaterial(new SolidColorBrush(colorFront));
                    DiffuseMaterial materialBack = new DiffuseMaterial(new SolidColorBrush(colorBack));

                    List<TriangleModel> models = new List<TriangleModel>();

                    foreach (EdgeLink[] face in links)
                    {
                        if (face.Length != 3)
                        {
                            // 4D may have more complex structures, but I'm guessing they will still go down to sets of 3
                            throw new ApplicationException("Unexpected number of links");
                        }

                        int[] indices = UtilityCore.GetPolygon(face.Select(o => (o.Index0, o.Index1)).ToArray());

                        #region 4d cross product thoughts

                        // Can't use the 3D normal.  Need to keep the higher dimension
                        //
                        //https://math.stackexchange.com/questions/2317604/cross-product-of-4d-vectors/2317673
                        //https://math.stackexchange.com/questions/904172/how-to-find-a-4d-vector-perpendicular-to-3-other-4d-vectors
                        //
                        // It looks like for N dimensions, you need N-1 vectors to be able to come up with a perpendicular
                        //
                        // I was thinking about it, and I wonder if the cross product of two vectors in 4d would result in a plane?
                        //
                        // I think this becomes a problem of trying to represent a 4D face as a triangle.  It would be a tetrahedron.  So one "side" of that
                        // tetrahedron would face away from the origin and the other side faces toward.
                        //
                        // But when rendering it in 3D, it will be a bunch of triangles, each triangles having two sides.  So which of the two colors should
                        // each of those triangles be painted?
                        //
                        //
                        // Don't try to take the cross product in 4D.  Work with one triangle at a time.  Rotate those three points into a 3D space, use that
                        // same transform to rotate the origin relative to the triangle.  Once in a 3D space (might not be able to always use XYZ?)
                        //
                        // Look at Math2D.GetTransformTo2D

                        #endregion

                        Triangle triangle = new Triangle(points[indices[0]].ToPoint3D(false), points[indices[1]].ToPoint3D(false), points[indices[2]].ToPoint3D(false));

                        double dot = Vector3D.DotProduct(triangle.GetCenterPoint().ToVector(), triangle.Normal);

                        GeometryModel3D model = new GeometryModel3D();

                        bool isStandardNormal = dot >= 0;
                        if (isStandardNormal)
                        {
                            model.Material = materialFront;
                            model.BackMaterial = materialBack;
                        }
                        else
                        {
                            model.Material = materialBack;
                            model.BackMaterial = materialFront;
                        }

                        MeshGeometry3D geometry = UtilityWPF.GetMeshFromTriangles_IndependentFaces(new[] { triangle });
                        model.Geometry = geometry;

                        modelGroup.Children.Add(model);

                        models.Add(new TriangleModel()
                        {
                            Model = model,
                            Geometry = geometry,
                            Indices = indices,
                            IsStandardNormal = isStandardNormal,
                        });
                    }

                    return
                    (
                        new ModelVisual3D()
                        {
                            Content = modelGroup,
                        },

                        modelGroup,

                        models.ToArray()
                    );
                }

                #endregion
            }

            #endregion
            #region class: IndexedVertex

            private class IndexedVertex : IVertex
            {
                public VectorND Point { get; set; }
                public double[] Position => Point.VectorArray;
                public int Index { get; set; }
            }

            #endregion
            #region class: CollapseSphere3D_Settings

            public class CollapseSphere3D_Settings
            {
                public bool ShowDots { get; set; }
                public bool ShowLines { get; set; }
                public bool ShowFaces { get; set; }
                public bool AlwaysVisible { get; set; }

                public double Latch_MaxForce { get; set; }
                public double Latch_Distance_Inner { get; set; }
                public double Latch_Distance_Outer { get; set; }
            }

            #endregion

            #region Declaration Section

            private const double RADIUS = 10;

            private const double DOT = .1;
            private const double LINE = .02;

            private readonly Border _border;
            private readonly Viewport3D _viewport;
            private readonly PerspectiveCamera _camera;
            private readonly Grid _grid;
            private readonly VisualizationColors _colors;

            private readonly double _slicePlane;

            private readonly VectorND[] _points;
            private readonly VectorND[] _origPoints;
            private PointVisuals _pointVisuals = null;

            private readonly EdgeLink[][] _links;
            private readonly LatchJoint[][] _latches;
            private LineVisuals _lineVisuals = null;
            private FaceVisuals _faceVisuals = null;
            private bool _isFaceVisualShown = false;

            private bool _showDots = false;
            private bool _showLines = false;
            private bool _showFaces = false;

            private bool _alwaysVisible = false;

            private double _linkMult_Compress = 6;
            private double _linkMult_Expand = 6;
            //private double _linkMult_Compress = 12;
            //private double _linkMult_Expand = 12;

            private double _speedMult = .5;

            private readonly double _maxForce;

            private CheckBox chkShowDots = null;
            private CheckBox chkShowLines = null;
            private CheckBox chkShowFaces = null;
            private CheckBox chkAlwaysVisible = null;

            #endregion

            #region Constructor

            public CollapseSphere3D(int numPoints, Border border, Viewport3D viewport, PerspectiveCamera camera, Grid grid, VisualizationColors colors, double slicePlane, CollapseSphere3D_Settings settings)
            {
                _border = border;
                _viewport = viewport;
                _camera = camera;
                _grid = grid;
                _colors = colors;
                _slicePlane = slicePlane * RADIUS;

                BuildControls(settings);

                var points = GetSpherePoints(3, numPoints, _slicePlane, Latch_MaxForce, Latch_Distance_Inner, Latch_Distance_Outer);
                _points = points.points;
                _origPoints = _points.
                    Select(o => o.Clone()).
                    ToArray();

                _links = points.links;
                _latches = points.latches;

                _maxForce = _links.SelectMany(o => o).Average(o => (o.Point1 - o.Point0).Length) * 4;

                // This needs to get built only once, otherwise the triangles will get colored wrong because the normals expect the triangles to be arranged in a sphere
                _faceVisuals = new FaceVisuals(_links, _points, _colors, _slicePlane);
            }

            #endregion

            public void Tick(double elapsedSeconds)
            {
                const double TOWARDPLANE_MAX = 1;
                const double AWAYFROMCENTER_MAX = .25;
                const double UPSIDEDOWN_MAX = 4;

                foreach (LatchJoint joint in _latches.SelectMany(o => o))
                {
                    joint.AdjustRandomBond();
                }

                int dimensions = _points[0].Size;       // can't trust _dimensions, because the slider can play with the value and not hit the reset button yet

                VectorND[] forces = Enumerable.Range(0, _points.Length).
                    Select(o => new VectorND(dimensions)).
                    ToArray();

                double[][] linkLengths = _links.
                    Select(o => UtilityNOS.UpdateLinkForces(forces, o, _points, _linkMult_Compress, _linkMult_Expand)).
                    ToArray();

                Force_TowardPlane(forces, _points, _slicePlane, dimensions, TOWARDPLANE_MAX);

                Force_AwayFromCenter(forces, _points, dimensions, AWAYFROMCENTER_MAX, RADIUS);

                Force_WrongFacing(forces, _points, _faceVisuals._models, dimensions, UPSIDEDOWN_MAX);

                // Calculate latch forces, maybe break/combine latches
                UtilityNOS.UpdateLatchForces(forces, _links, _latches);

                UtilityNOS.ApplyForces(_points, forces, elapsedSeconds, _speedMult, _maxForce, _latches);

                #region draw dots

                if (_showDots && _pointVisuals == null)
                {
                    _pointVisuals = new PointVisuals(_origPoints, _points, _colors, _slicePlane);
                    _viewport.Children.Add(_pointVisuals.Visual);
                }
                else if (!_showDots && _pointVisuals != null)
                {
                    _viewport.Children.Remove(_pointVisuals.Visual);
                    _pointVisuals = null;
                }

                if (_pointVisuals != null)
                {
                    _pointVisuals.Tick(_alwaysVisible);
                }

                #endregion
                #region draw lines

                if (_showLines && _lineVisuals == null)
                {
                    _lineVisuals = new LineVisuals(_links, _colors, _slicePlane);
                    _viewport.Children.Add(_lineVisuals.Visual);
                }
                else if (!_showLines && _lineVisuals != null)
                {
                    _viewport.Children.Remove(_lineVisuals.Visual);
                    _lineVisuals = null;
                }

                if (_lineVisuals != null)
                {
                    _lineVisuals.Tick(_alwaysVisible, linkLengths);
                }

                #endregion
                #region draw faces

                if (_showFaces && !_isFaceVisualShown)
                {
                    _viewport.Children.Add(_faceVisuals.Visual);
                    _isFaceVisualShown = true;
                }
                else if (!_showFaces && _isFaceVisualShown)
                {
                    _viewport.Children.Remove(_faceVisuals.Visual);
                    _isFaceVisualShown = false;
                }

                if (_isFaceVisualShown)
                {
                    _faceVisuals.Tick(_alwaysVisible);
                }

                #endregion
            }

            public void Dispose()
            {
                if (_pointVisuals != null)
                {
                    _viewport.Children.Remove(_pointVisuals.Visual);
                    _pointVisuals = null;
                }

                if (_lineVisuals != null)
                {
                    _viewport.Children.Remove(_lineVisuals.Visual);
                    _lineVisuals = null;
                }

                if (_faceVisuals != null && _isFaceVisualShown)
                {
                    _viewport.Children.Remove(_faceVisuals.Visual);
                    _faceVisuals = null;
                    _isFaceVisualShown = false;
                }

                _grid.Children.Clear();
            }

            #region Properties

            public CollapseSphere3D_Settings Settings
            {
                get
                {
                    return new CollapseSphere3D_Settings()
                    {
                        ShowDots = chkShowDots.IsChecked.Value,
                        ShowLines = chkShowLines.IsChecked.Value,
                        ShowFaces = chkShowFaces.IsChecked.Value,
                        AlwaysVisible = chkAlwaysVisible.IsChecked.Value,

                        Latch_MaxForce = Latch_MaxForce,
                        Latch_Distance_Inner = Latch_Distance_Inner,
                        Latch_Distance_Outer = Latch_Distance_Outer,
                    };
                }
            }

            private double _latch_MaxForce = 3;
            private double Latch_MaxForce
            {
                get
                {
                    return _latch_MaxForce;
                }
                set
                {
                    _latch_MaxForce = value;

                    if (_latches != null)
                    {
                        foreach (LatchJoint joint in _latches.SelectMany(o => o))
                        {
                            joint.MaxForce = _latch_MaxForce;
                        }
                    }
                }
            }

            private double _latch_distance_inner = .03;
            private double Latch_Distance_Inner
            {
                get
                {
                    return _latch_distance_inner;
                }
                set
                {
                    _latch_distance_inner = value;

                    if (_latches != null)
                    {
                        foreach (LatchJoint joint in _latches.SelectMany(o => o))
                        {
                            joint.Distance_Latch = _latch_distance_inner;
                        }
                    }
                }
            }

            private double _latch_distance_outer = 3;
            private double Latch_Distance_Outer
            {
                get
                {
                    return _latch_distance_outer;
                }
                set
                {
                    _latch_distance_outer = value;

                    if (_latches != null)
                    {
                        foreach (LatchJoint joint in _latches.SelectMany(o => o))
                        {
                            joint.Distance_Force = _latch_distance_outer;
                        }
                    }
                }
            }

            #endregion

            #region Private Methods - build

            private void BuildControls(CollapseSphere3D_Settings settings)
            {
                StackPanel panel = new StackPanel()
                {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(8),
                };

                Grid.SetIsSharedSizeScope(panel, true);

                _grid.Children.Add(panel);

                #region show dots

                chkShowDots = new CheckBox()
                {
                    Content = "Show Dots",
                    IsChecked = settings?.ShowDots ?? false,
                };

                chkShowDots.Checked += (s, e) => _showDots = chkShowDots.IsChecked.Value;
                chkShowDots.Unchecked += (s, e) => _showDots = chkShowDots.IsChecked.Value;

                _showDots = chkShowDots.IsChecked.Value;

                panel.Children.Add(chkShowDots);

                #endregion
                #region show lines

                chkShowLines = new CheckBox()
                {
                    Content = "Show Lines",
                    IsChecked = settings?.ShowLines ?? false,
                };

                chkShowLines.Checked += (s, e) => _showLines = chkShowLines.IsChecked.Value;
                chkShowLines.Unchecked += (s, e) => _showLines = chkShowLines.IsChecked.Value;

                _showLines = chkShowLines.IsChecked.Value;

                panel.Children.Add(chkShowLines);

                #endregion
                #region show faces

                chkShowFaces = new CheckBox()
                {
                    Content = "Show Faces",
                    IsChecked = settings?.ShowFaces ?? true,
                };

                chkShowFaces.Checked += (s, e) => _showFaces = chkShowFaces.IsChecked.Value;
                chkShowFaces.Unchecked += (s, e) => _showFaces = chkShowFaces.IsChecked.Value;

                _showFaces = chkShowFaces.IsChecked.Value;

                panel.Children.Add(chkShowFaces);

                #endregion

                #region always visible

                chkAlwaysVisible = new CheckBox()
                {
                    Content = "Always Visible",
                    ToolTip = "When unchecked, only triangles near the slice plane will be visible",
                    IsChecked = settings?.AlwaysVisible ?? true,       // true for 3D, false for 4D
                    Margin = new Thickness(0, 8, 0, 0),
                };

                chkAlwaysVisible.Checked += (s, e) => _alwaysVisible = chkAlwaysVisible.IsChecked.Value;
                chkAlwaysVisible.Unchecked += (s, e) => _alwaysVisible = chkAlwaysVisible.IsChecked.Value;

                _alwaysVisible = chkAlwaysVisible.IsChecked.Value;

                panel.Children.Add(chkAlwaysVisible);

                #endregion

                #region expander: latches

                #region expander/panel

                Expander expanderLatches = new Expander()
                {
                    Header = "Latches",
                    IsExpanded = false,
                    Margin = new Thickness(0, 8, 0, 0),
                };

                panel.Children.Add(expanderLatches);

                StackPanel panelLatches = new StackPanel()
                {
                    Margin = new Thickness(8),
                };

                expanderLatches.Content = panelLatches;

                #endregion

                #region Slider: Max Force

                var maxForce = GetGrid_KeyValue_SharedSize("max force", true);
                panelLatches.Children.Add(maxForce.grid);

                maxForce.slider.Minimum = .1;
                maxForce.slider.Maximum = 12;
                maxForce.slider.Value = settings?.Latch_MaxForce ?? 3;

                maxForce.slider.ValueChanged += (s, e) =>
                {
                    Latch_MaxForce = maxForce.slider.Value;
                    maxForce.label.Content = Latch_MaxForce.ToStringSignificantDigits(3);
                };

                Latch_MaxForce = maxForce.slider.Value;
                maxForce.label.Content = Latch_MaxForce.ToStringSignificantDigits(3);

                #endregion

                #region Slider: Inner Distance

                var innerDistance = GetGrid_KeyValue_SharedSize("inner distance", true);
                panelLatches.Children.Add(innerDistance.grid);

                innerDistance.slider.Minimum = .002;
                innerDistance.slider.Maximum = .3;
                innerDistance.slider.Value = settings?.Latch_Distance_Inner ?? .03;

                innerDistance.slider.ValueChanged += (s, e) =>
                {
                    Latch_Distance_Inner = innerDistance.slider.Value;
                    innerDistance.label.Content = Latch_Distance_Inner.ToStringSignificantDigits(3);
                };

                Latch_Distance_Inner = innerDistance.slider.Value;
                innerDistance.label.Content = Latch_Distance_Inner.ToStringSignificantDigits(3);

                #endregion

                #region Slider: Outer Distance

                var outerDistance = GetGrid_KeyValue_SharedSize("outer distance", true);
                panelLatches.Children.Add(outerDistance.grid);

                outerDistance.slider.Minimum = .25;
                outerDistance.slider.Maximum = 6;
                outerDistance.slider.Value = settings?.Latch_Distance_Outer ?? 3;

                outerDistance.slider.ValueChanged += (s, e) =>
                {
                    Latch_Distance_Outer = outerDistance.slider.Value;
                    outerDistance.label.Content = Latch_Distance_Outer.ToStringSignificantDigits(3);
                };

                Latch_Distance_Outer = outerDistance.slider.Value;
                outerDistance.label.Content = Latch_Distance_Outer.ToStringSignificantDigits(3);

                #endregion

                #endregion

                // Simulation speed?
                // Various force settings?
            }

            private static (EdgeLink[][] links, LatchJoint[][] latches, VectorND[] points) GetSpherePoints(int dimensions, int count, double slicePlane, double latch_MaxForce, double latch_Distance_Inner, double latch_Distance_Outer)
            {
                // Points
                VectorND[] points = MathND.GetRandomVectors_Spherical_Shell_EvenDist(dimensions, RADIUS, count);

                // The convex hull can be thought of as a delaunay around the surface of a sphere
                var hull = GetConvexHull(points);

                // Filter out points that are below the plane
                var filtered = FilterSlicePlane(points, hull, slicePlane);

                // Latches
                var latches = UtilityNOS.GetLatches(filtered.delanay, filtered.points, latch_MaxForce, latch_Distance_Inner, latch_Distance_Outer);

                // Links
                EdgeLink[][] links = latches.delaunay.
                    Select(o => o.Select(p => new EdgeLink(p.Item1, p.Item2, latches.points, (latches.points[p.Item2] - latches.points[p.Item1]).Length)).ToArray()).
                    ToArray();

                return (links, latches.latches, latches.points);
            }

            private static (int, int)[][] GetConvexHull(VectorND[] points)
            {
                IndexedVertex[] vertices = points.
                    Select((o, i) => new IndexedVertex()
                    {
                        Index = i,
                        Point = o,
                    }).
                    ToArray();

                var convexHull = MIConvexHull.ConvexHull.Create(vertices);

                List<(int, int)[]> retVal = new List<(int, int)[]>();

                foreach (var face in convexHull.Faces)
                {
                    retVal.Add(UtilityCore.IterateEdges(face.Vertices.Length).
                        Select(o => (face.Vertices[o.from].Index, face.Vertices[o.to].Index)).
                        ToArray());
                }

                return retVal.ToArray();
            }

            private static (VectorND[] points, (int, int)[][] delanay) FilterSlicePlane(VectorND[] points, (int, int)[][] delanay, double slicePlane)
            {
                int examineIndex = points[0].Size - 1;

                var delList = new List<(int, int)[]>(delanay);
                var removeIndices = new List<int>();

                // Find polys that need to be removed (remove if they are below the slice plane)
                int index = 0;

                while (index < delList.Count)
                {
                    if (delList[index].All(o => points[o.Item1][examineIndex] < slicePlane && points[o.Item2][examineIndex] < slicePlane))      // only removing if the entire poly is below the plane (keep the ones that straddle the plane)
                    {
                        removeIndices.AddRange(delList[index].Select(o => o.Item1));
                        removeIndices.AddRange(delList[index].Select(o => o.Item2));
                        delList.RemoveAt(index);
                    }
                    else
                    {
                        index++;
                    }
                }

                // Create a map between old and new index
                var (map, from_to) = UtilityCore.GetIndexMap(points.Length, removeIndices);

                // Apply the map
                foreach (var set in delList)
                {
                    for (int cntr = 0; cntr < set.Length; cntr++)
                    {
                        set[cntr] = (map[set[cntr].Item1], map[set[cntr].Item2]);
                    }
                }

                VectorND[] reducedPoints = from_to.
                    Select(o => points[o.from]).
                    ToArray();

                return
                (
                    reducedPoints.
                        ToArray(),

                    // There are polys along the edge that point to removed points.  Don't include those partial polys
                    delList.
                        Where(o => !o.Any(p => p.Item1 < 0 || p.Item2 < 0)).
                        ToArray()
                );
            }

            private static (Grid grid, Slider slider, Label label) GetGrid_KeyValue_SharedSize(string key, bool setVerticalMargin)
            {
                // Grid
                Grid grid = new Grid();

                if (setVerticalMargin)
                {
                    grid.Margin = new Thickness(0, 8, 0, 0);
                }

                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto), SharedSizeGroup = "KeyGroup" });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(8, GridUnitType.Pixel) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star), SharedSizeGroup = "ValueGroup" });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(8, GridUnitType.Pixel) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto), SharedSizeGroup = "LabelGroup" });

                // Left Label
                Label leftLabel = new Label()
                {
                    Content = key,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center,
                };

                Grid.SetColumn(leftLabel, 0);
                grid.Children.Add(leftLabel);

                // Slider
                Slider slider = new Slider()
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    MinWidth = 100,
                };

                Grid.SetColumn(slider, 2);
                grid.Children.Add(slider);

                // Right Label
                Label rightLabel = new Label()
                {
                    VerticalAlignment = VerticalAlignment.Center,
                };

                Grid.SetColumn(rightLabel, 4);
                grid.Children.Add(rightLabel);

                return (grid, slider, rightLabel);
            }

            #endregion
            #region Private Methods - tick

            private static void Force_TowardPlane(VectorND[] forces, VectorND[] points, double slicePlane, int dimensions, double max)
            {
                double[] towardSlicePlane = new double[dimensions];

                for (int cntr = 0; cntr < points.Length; cntr++)
                {
                    double distFromPlane = points[cntr][dimensions - 1] - slicePlane;

                    towardSlicePlane[dimensions - 1] = UtilityCore.Cap(-distFromPlane * .75, -max, max);

                    forces[cntr] += new VectorND(towardSlicePlane);
                }
            }

            private static void Force_AwayFromCenter(VectorND[] forces, VectorND[] points, int dimensions, double max, double radius)
            {
                int dim_1 = dimensions - 1;     // this is what's really needed througout this function

                VectorND[] points_0 = points.
                    Select(o =>
                    {
                        VectorND o1 = o.Clone();
                        o1[dim_1] = 0;
                        return o1;
                    }).
                    ToArray();

                VectorND center = MathND.GetCenter(points_0);

                double maxRadius = points_0.
                    Max(o => (o - center).LengthSquared);

                maxRadius = Math.Sqrt(maxRadius);

                for (int cntr = 0; cntr < points.Length; cntr++)
                {
                    VectorND away = points_0[cntr] - center;

                    //double force = (maxRadius / radius) * max;
                    double force = (away.Length / maxRadius) * max;

                    forces[cntr] += away.ToUnit(false) * force;
                }
            }

            private static void Force_WrongFacing(VectorND[] forces, VectorND[] points, FaceVisuals.TriangleModel[] faces, int dimensions, double force)
            {
                // This was copied from FaceVisuals.Tick.  Should probably optimize and only calculate the normal once

                foreach (var face in faces)
                {
                    if (face.Indices.Length != 3)
                    {
                        throw new ApplicationException("Handle 4D");
                    }

                    Point3D p0 = points[face.Indices[0]].ToPoint3D(false);
                    Point3D p1 = points[face.Indices[1]].ToPoint3D(false);
                    Point3D p2 = points[face.Indices[2]].ToPoint3D(false);

                    // This was copied from Triangle.Normal
                    Vector3D dir1 = p0 - p1;
                    Vector3D dir2 = p2 - p1;

                    Vector3D triangleNormal = Vector3D.CrossProduct(dir2, dir1);
                    if (!face.IsStandardNormal)
                    {
                        triangleNormal = -triangleNormal;
                    }

                    if (triangleNormal.Z < 0)
                    {
                        VectorND awayForce = (triangleNormal.ToUnit(false) * force).ToVectorND();

                        foreach (int index in face.Indices)
                        {
                            forces[index] += awayForce;
                        }
                    }
                }
            }

            #endregion
        }

        #endregion
        #region class: CollapseSphere4D

        private class CollapseSphere4D : ITimerVisualization
        {
            #region class: PointVisuals

            private class PointVisuals
            {
                #region Declaration Section

                private readonly VectorND[] _origPoints;     // these are the original colors
                private readonly VectorND[] _points;

                private readonly TranslateTransform3D[] _translates;

                private readonly SolidColorBrush[] _brushes;      // these are the original color + alpha (only have alpha if AlwaysVisible is unchecked)

                private readonly VisualizationColors _colors;

                private readonly double _slicePlane;

                #endregion

                #region Constructor

                public PointVisuals(VectorND[] origPoints, VectorND[] points, VisualizationColors colors, double slicePlane)
                {
                    _origPoints = origPoints;
                    _points = points;
                    _colors = colors;
                    _slicePlane = slicePlane;

                    var visuals = GetVisuals(_origPoints);
                    _brushes = visuals.brushes;
                    _translates = visuals.translates;

                    Visual = new ModelVisual3D()
                    {
                        Content = visuals.modelGroup,
                    };

                    MoveVisuals(_points, _translates);
                    ColorVisuals(_points, _origPoints, _brushes, RADIUS, _colors, _slicePlane, false);
                }

                #endregion

                public readonly Visual3D Visual;

                public void Tick(bool alwaysVisible)
                {
                    MoveVisuals(_points, _translates);
                    ColorVisuals(_points, _origPoints, _brushes, RADIUS, _colors, _slicePlane, alwaysVisible);
                }

                #region Private Methods

                private static (Model3DGroup modelGroup, TranslateTransform3D[] translates, SolidColorBrush[] brushes) GetVisuals(VectorND[] positions)
                {
                    TranslateTransform3D[] translates = new TranslateTransform3D[positions.Length];
                    SolidColorBrush[] brushes = new SolidColorBrush[positions.Length];

                    Model3DGroup modelGroup = new Model3DGroup();

                    MeshGeometry3D dotGeometry = UtilityWPF.GetSphere_Ico(DOT, 1, true);

                    for (int cntr = 0; cntr < positions.Length; cntr++)
                    {
                        MaterialGroup material = new MaterialGroup();

                        brushes[cntr] = new SolidColorBrush(Colors.Black);
                        material.Children.Add(new DiffuseMaterial(brushes[cntr]));
                        //material.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("50AAAAAA")), 2));

                        translates[cntr] = new TranslateTransform3D();

                        modelGroup.Children.Add(new GeometryModel3D()
                        {
                            Material = material,
                            BackMaterial = material,
                            Geometry = dotGeometry,
                            Transform = translates[cntr],
                        });
                    }

                    return (modelGroup, translates, brushes);
                }

                private static void MoveVisuals(VectorND[] points, TranslateTransform3D[] translates)
                {
                    for (int cntr = 0; cntr < points.Length; cntr++)
                    {
                        //TODO: Have a function that maps the VectorND into 3D.  The current way is simply only taking the first 3 coords, but it might be more
                        //illuminating to allow custom rotations (especially if in vr and those rotations are mapped to the head's position/orientation)
                        Vector3D position = points[cntr].ToVector3D(false);

                        translates[cntr].OffsetX = position.X;
                        translates[cntr].OffsetY = position.Y;
                        translates[cntr].OffsetZ = position.Z;
                    }
                }

                private static void ColorVisuals(VectorND[] points, VectorND[] origPoints, SolidColorBrush[] brushes, double radius, VisualizationColors colors, double slicePlane, bool alwaysVisible)
                {
                    for (int cntr = 0; cntr < points.Length; cntr++)
                    {
                        Color color = UtilityNOS.GetDotColor(origPoints[cntr], true, radius, colors);

                        if (!alwaysVisible)
                        {
                            double alpha = UtilityNOS.GetAlphaPercent(points[cntr], radius, slicePlane);
                            color = UtilityWPF.AlphaBlend(color, Colors.Transparent, alpha);
                        }

                        brushes[cntr].Color = color;
                    }
                }

                #endregion
            }

            #endregion
            #region class: LineVisuals

            private class LineVisuals
            {
                #region Declaration Section

                private readonly VisualizationColors _colors;
                private readonly double _slicePlane;

                private readonly EdgeLink[][] _links;
                private readonly BillboardLine3D[][] _billboards;

                #endregion

                #region Constructor

                public LineVisuals(EdgeLink[][] links, VisualizationColors colors, double slicePlane)
                {
                    _links = links;
                    _colors = colors;
                    _slicePlane = slicePlane;

                    var visuals = GetVisuals(links, colors);
                    _billboards = visuals.lines;

                    Visual = visuals.visual;
                }

                #endregion

                public readonly Visual3D Visual;

                public void Tick(bool alwaysVisible, double[][] linkLengths)
                {
                    for (int outer = 0; outer < _links.Length; outer++)
                    {
                        for (int inner = 0; inner < _links[outer].Length; inner++)
                        {
                            _billboards[outer][inner].FromPoint = _links[outer][inner].Point0.ToPoint3D(false);
                            _billboards[outer][inner].ToPoint = _links[outer][inner].Point1.ToPoint3D(false);

                            Color color = UtilityNOS.GetLinkColor(linkLengths[outer][inner], _links[outer][inner].DesiredLength, _colors);

                            if (alwaysVisible)
                            {
                                _billboards[outer][inner].Color = color;
                                if (_billboards[outer][inner].ColorTo != null)
                                {
                                    _billboards[outer][inner].ColorTo = null;
                                }
                            }
                            else
                            {
                                double alpha = UtilityNOS.GetAlphaPercent(_links[outer][inner].Point0, RADIUS, _slicePlane);
                                _billboards[outer][inner].Color = UtilityWPF.AlphaBlend(color, Colors.Transparent, alpha);

                                alpha = UtilityNOS.GetAlphaPercent(_links[outer][inner].Point1, RADIUS, _slicePlane);
                                _billboards[outer][inner].ColorTo = UtilityWPF.AlphaBlend(color, Colors.Transparent, alpha);
                            }
                        }
                    }
                }

                #region Private Methods

                private static (Visual3D visual, BillboardLine3D[][] lines) GetVisuals(EdgeLink[][] links, VisualizationColors colors)
                {
                    BillboardLine3D[][] lines = new BillboardLine3D[links.Length][];

                    Model3DGroup group = new Model3DGroup();

                    for (int outer = 0; outer < links.Length; outer++)
                    {
                        lines[outer] = new BillboardLine3D[links[outer].Length];

                        for (int inner = 0; inner < links[outer].Length; inner++)
                        {
                            lines[outer][inner] = new BillboardLine3D()
                            {
                                Thickness = LINE,
                                FromPoint = links[outer][inner].Point0.ToPoint3D(false),
                                ToPoint = links[outer][inner].Point1.ToPoint3D(false),
                                Color = colors.Zero,
                            };

                            group.Children.Add(lines[outer][inner].Model);
                        }
                    }

                    Visual3D visual = new ModelVisual3D()
                    {
                        Content = group,
                    };

                    return (visual, lines);
                }

                #endregion
            }

            #endregion
            #region class: FaceVisuals

            private class FaceVisuals
            {
                #region class: TriangleModel

                public class TriangleModel
                {
                    public GeometryModel3D Model { get; set; }
                    public MeshGeometry3D Geometry { get; set; }

                    public int[] Indices { get; set; }
                    public bool IsStandardNormal { get; set; }
                }

                #endregion
                #region class: TetraVisuals

                public class TetraVisuals
                {
                    public TriangleModel[] Triangles { get; set; }

                    public int[] Indices { get; set; }
                    public bool IsStandardNormal { get; set; }
                }

                #endregion

                #region Declaration Section

                private readonly VisualizationColors _colors;
                private readonly double _slicePlane;

                private readonly (int, int, int)[][] _faces;
                private readonly VectorND[] _points;

                private readonly Model3DGroup _group;
                public readonly TriangleModel[] _models;

                private readonly Color _colorFront = UtilityWPF.ColorFromHex("656970");
                private readonly Color _colorBack = UtilityWPF.ColorFromHex("8C6A5E");

                private bool _currentlyAlwaysVisible = true;

                #endregion

                #region Constructor

                public FaceVisuals((int, int, int)[][] faces, VectorND[] points, VisualizationColors colors, double slicePlane)
                {
                    _faces = faces;
                    _points = points;
                    _colors = colors;
                    _slicePlane = slicePlane;

                    var visuals = GetVisuals(_faces, _points, _colors, _colorFront, _colorBack);
                    Visual = visuals.visual;
                    _group = visuals.group;
                    _models = visuals.models;
                }

                #endregion

                public readonly Visual3D Visual;

                public void Tick(bool alwaysVisible)
                {
                    foreach (TriangleModel model in _models)
                    {
                        if (model.Indices.Length == 3)
                        {
                            Point3D p0 = _points[model.Indices[0]].ToPoint3D(false);
                            Point3D p1 = _points[model.Indices[1]].ToPoint3D(false);
                            Point3D p2 = _points[model.Indices[2]].ToPoint3D(false);

                            model.Geometry.Positions[0] = p0;
                            model.Geometry.Positions[1] = p1;
                            model.Geometry.Positions[2] = p2;

                            // This was copied from Triangle.Normal
                            Vector3D dir1 = p0 - p1;
                            Vector3D dir2 = p2 - p1;

                            Vector3D triangleNormal = Vector3D.CrossProduct(dir2, dir1);
                            if (!model.IsStandardNormal)
                            {
                                triangleNormal = -triangleNormal;
                            }

                            model.Geometry.Normals[0] = triangleNormal;
                            model.Geometry.Normals[1] = triangleNormal;
                            model.Geometry.Normals[2] = triangleNormal;
                        }
                        else
                        {
                            // Can't change the normal, so the triangles will be shaded according to the initial creation
                            for (int cntr = 0; cntr < model.Indices.Length; cntr++)
                            {
                                model.Geometry.Positions[cntr] = _points[model.Indices[cntr]].ToPoint3D(false);
                            }
                        }
                    }

                    if (alwaysVisible && _currentlyAlwaysVisible)
                    {
                        return;
                    }

                    if (alwaysVisible)
                    {
                        #region always visible

                        _group.Children.Clear();

                        DiffuseMaterial materialFront = new DiffuseMaterial(new SolidColorBrush(_colorFront));
                        DiffuseMaterial materialBack = new DiffuseMaterial(new SolidColorBrush(_colorBack));

                        foreach (TriangleModel model in _models)
                        {
                            if (model.IsStandardNormal)
                            {
                                model.Model.Material = materialFront;
                                model.Model.BackMaterial = materialBack;
                            }
                            else
                            {
                                model.Model.Material = materialBack;
                                model.Model.BackMaterial = materialFront;
                            }

                            _group.Children.Add(model.Model);
                        }

                        #endregion
                    }
                    else
                    {
                        #region partially visible

                        _group.Children.Clear();

                        foreach (TriangleModel model in _models)
                        {
                            double alpha0 = UtilityNOS.GetAlphaPercent(_points[model.Indices[0]], RADIUS, _slicePlane);
                            double alpha1 = UtilityNOS.GetAlphaPercent(_points[model.Indices[1]], RADIUS, _slicePlane);
                            double alpha2 = UtilityNOS.GetAlphaPercent(_points[model.Indices[2]], RADIUS, _slicePlane);

                            if (alpha0.IsNearZero() && alpha1.IsNearZero() && alpha2.IsNearZero())
                            {
                                continue;
                            }

                            _group.Children.Add(UtilityWPF.GetGradientTriangle
                            (
                                _points[model.Indices[0]].ToPoint3D(false),
                                _points[model.Indices[1]].ToPoint3D(false),
                                _points[model.Indices[2]].ToPoint3D(false),
                                UtilityWPF.AlphaBlend(_colorFront, Colors.Transparent, alpha0),     // because of semitransparency, backmaterial isn't used
                                UtilityWPF.AlphaBlend(_colorFront, Colors.Transparent, alpha1),
                                UtilityWPF.AlphaBlend(_colorFront, Colors.Transparent, alpha2),
                                false
                            ));
                        }

                        #endregion
                    }

                    _currentlyAlwaysVisible = alwaysVisible;
                }

                #region Private Methods

                private static (Visual3D visual, Model3DGroup group, TriangleModel[] models) GetVisuals((int, int, int)[][] faces, VectorND[] points, VisualizationColors colors, Color colorFront, Color colorBack)
                {
                    Model3DGroup modelGroup = new Model3DGroup();

                    DiffuseMaterial materialFront = new DiffuseMaterial(new SolidColorBrush(colorFront));
                    DiffuseMaterial materialBack = new DiffuseMaterial(new SolidColorBrush(colorBack));

                    List<TriangleModel> models = new List<TriangleModel>();

                    foreach (var face in faces.SelectMany(o => o))
                    {
                        int[] indices = new[] { face.Item1, face.Item2, face.Item3 };

                        #region 4d cross product thoughts

                        // Can't use the 3D normal.  Need to keep the higher dimension
                        //
                        //https://math.stackexchange.com/questions/2317604/cross-product-of-4d-vectors/2317673
                        //https://math.stackexchange.com/questions/904172/how-to-find-a-4d-vector-perpendicular-to-3-other-4d-vectors
                        //
                        // It looks like for N dimensions, you need N-1 vectors to be able to come up with a perpendicular
                        //
                        // I was thinking about it, and I wonder if the cross product of two vectors in 4d would result in a plane?
                        //
                        // I think this becomes a problem of trying to represent a 4D face as a triangle.  It would be a tetrahedron.  So one "side" of that
                        // tetrahedron would face away from the origin and the other side faces toward.
                        //
                        // But when rendering it in 3D, it will be a bunch of triangles, each triangles having two sides.  So which of the two colors should
                        // each of those triangles be painted?
                        //
                        //
                        // Don't try to take the cross product in 4D.  Work with one triangle at a time.  Rotate those three points into a 3D space, use that
                        // same transform to rotate the origin relative to the triangle.  Once in a 3D space (might not be able to always use XYZ?)
                        //
                        // Look at Math2D.GetTransformTo2D

                        #endregion

                        Triangle triangle = new Triangle(points[indices[0]].ToPoint3D(false), points[indices[1]].ToPoint3D(false), points[indices[2]].ToPoint3D(false));

                        double dot = Vector3D.DotProduct(triangle.GetCenterPoint().ToVector(), triangle.Normal);

                        GeometryModel3D model = new GeometryModel3D();

                        bool isStandardNormal = dot >= 0;
                        if (isStandardNormal)
                        {
                            model.Material = materialFront;
                            model.BackMaterial = materialBack;
                        }
                        else
                        {
                            model.Material = materialBack;
                            model.BackMaterial = materialFront;
                        }

                        MeshGeometry3D geometry = UtilityWPF.GetMeshFromTriangles_IndependentFaces(new[] { triangle });
                        model.Geometry = geometry;

                        modelGroup.Children.Add(model);

                        models.Add(new TriangleModel()
                        {
                            Model = model,
                            Geometry = geometry,
                            Indices = indices,
                            IsStandardNormal = isStandardNormal,
                        });
                    }

                    return
                    (
                        new ModelVisual3D()
                        {
                            Content = modelGroup,
                        },

                        modelGroup,

                        models.ToArray()
                    );
                }

                #endregion
            }

            #endregion
            #region class: IndexedVertex

            private class IndexedVertex : IVertex
            {
                public VectorND Point { get; set; }
                public double[] Position => Point.VectorArray;
                public int Index { get; set; }
            }

            #endregion
            #region class: TetraNormal

            private class TetraNormal
            {
                public int Index_Common { get; set; }
                public int[] Indices_To { get; set; }

                public IEnumerable<int> Indices
                {
                    get
                    {
                        yield return Index_Common;

                        foreach (int index in Indices_To)
                        {
                            yield return index;
                        }
                    }
                }

                public VectorND[] AllPoints { get; set; }

                public bool IsStandardNormal { get; set; }

                public VectorND GetNormal()
                {
                    VectorND[] vectors = new VectorND[Indices_To.Length];

                    for (int cntr = 0; cntr < vectors.Length; cntr++)
                    {
                        vectors[cntr] = AllPoints[Indices_To[cntr]] - AllPoints[Index_Common];
                    }

                    return VectorND.CrossProduct(vectors);
                }
            }

            #endregion
            #region class: CollapseSphere4D_Settings

            public class CollapseSphere4D_Settings
            {
                public bool ShowDots { get; set; }
                public bool ShowLines { get; set; }
                public bool ShowFaces { get; set; }
                public bool AlwaysVisible { get; set; }

                public double Latch_MaxForce { get; set; }
                public double Latch_Distance_Inner { get; set; }
                public double Latch_Distance_Outer { get; set; }
            }

            #endregion

            #region Declaration Section

            private const double RADIUS = 10;

            private const double DOT = .1;
            private const double LINE = .02;

            private readonly int _dimensions;
            private readonly Border _border;
            private readonly Viewport3D _viewport;
            private readonly PerspectiveCamera _camera;
            private readonly Grid _grid;
            private readonly VisualizationColors _colors;

            private readonly double _slicePlane;
            private readonly double? _topSlicePlane;

            private readonly VectorND[] _points;
            private readonly VectorND[] _origPoints;
            private PointVisuals _pointVisuals = null;

            private readonly EdgeLink[][] _links;
            private readonly LatchJoint[][] _latches;
            private readonly (int, int, int)[][] _faces;
            private readonly TetraNormal[] _normals;
            private LineVisuals _lineVisuals = null;
            private FaceVisuals _faceVisuals = null;
            private bool _isFaceVisualShown = false;

            private bool _showDots = false;
            private bool _showLines = false;
            private bool _showFaces = false;

            private bool _alwaysVisible = false;

            private double _linkMult_Compress = 6;
            private double _linkMult_Expand = 6;
            //private double _linkMult_Compress = 12;
            //private double _linkMult_Expand = 12;

            private double _speedMult = .5;

            private readonly double _maxForce;

            private CheckBox chkShowDots = null;
            private CheckBox chkShowLines = null;
            private CheckBox chkShowFaces = null;
            private CheckBox chkAlwaysVisible = null;

            #endregion

            #region Constructor

            public CollapseSphere4D(int dimensions, int numPoints, Border border, Viewport3D viewport, PerspectiveCamera camera, Grid grid, VisualizationColors colors, double slicePlane, double? topSlicePlane, CollapseSphere4D_Settings settings)
            {
                _dimensions = dimensions;
                _border = border;
                _viewport = viewport;
                _camera = camera;
                _grid = grid;
                _colors = colors;
                _slicePlane = slicePlane * RADIUS;
                _topSlicePlane = topSlicePlane * RADIUS;

                BuildControls(settings);

                var points = GetSpherePoints(_dimensions, numPoints, _slicePlane, _topSlicePlane, Latch_MaxForce, Latch_Distance_Inner, Latch_Distance_Outer);
                _points = points.points;
                _origPoints = _points.
                    Select(o => o.Clone()).
                    ToArray();

                _links = points.links;
                _latches = points.latches;
                _faces = points.faces;
                _normals = points.normals;

                _maxForce = _links.SelectMany(o => o).Average(o => (o.Point1 - o.Point0).Length) * 4;

                // This needs to get built only once, otherwise the triangles will get colored wrong because the normals expect the triangles to be arranged in a sphere
                _faceVisuals = new FaceVisuals(_faces, _points, _colors, _slicePlane);
            }

            #endregion

            public void Tick(double elapsedSeconds)
            {
                const double TOWARDPLANE_MAX = 1;
                const double AWAYFROMCENTER_MAX = .25;
                const double UPSIDEDOWN_MAX = 4;

                foreach (LatchJoint joint in _latches.SelectMany(o => o))
                {
                    joint.AdjustRandomBond();
                }

                int dimensions = _points[0].Size;       // can't trust _dimensions, because the slider can play with the value and not hit the reset button yet

                VectorND[] forces = Enumerable.Range(0, _points.Length).
                    Select(o => new VectorND(dimensions)).
                    ToArray();

                double[][] linkLengths = _links.
                    Select(o => UtilityNOS.UpdateLinkForces(forces, o, _points, _linkMult_Compress, _linkMult_Expand)).
                    ToArray();

                Force_TowardPlane(forces, _points, _slicePlane, dimensions, TOWARDPLANE_MAX);

                Force_AwayFromCenter(forces, _points, dimensions, AWAYFROMCENTER_MAX, RADIUS);

                Force_WrongFacing(forces, _points, _faceVisuals._models, _normals, dimensions, UPSIDEDOWN_MAX);

                // Calculate latch forces, maybe break/combine latches
                UtilityNOS.UpdateLatchForces(forces, _links, _latches);

                UtilityNOS.ApplyForces(_points, forces, elapsedSeconds, _speedMult, _maxForce, _latches);

                #region draw dots

                if (_showDots && _pointVisuals == null)
                {
                    _pointVisuals = new PointVisuals(_origPoints, _points, _colors, _slicePlane);
                    _viewport.Children.Add(_pointVisuals.Visual);
                }
                else if (!_showDots && _pointVisuals != null)
                {
                    _viewport.Children.Remove(_pointVisuals.Visual);
                    _pointVisuals = null;
                }

                if (_pointVisuals != null)
                {
                    _pointVisuals.Tick(_alwaysVisible);
                }

                #endregion
                #region draw lines

                if (_showLines && _lineVisuals == null)
                {
                    _lineVisuals = new LineVisuals(_links, _colors, _slicePlane);
                    _viewport.Children.Add(_lineVisuals.Visual);
                }
                else if (!_showLines && _lineVisuals != null)
                {
                    _viewport.Children.Remove(_lineVisuals.Visual);
                    _lineVisuals = null;
                }

                if (_lineVisuals != null)
                {
                    _lineVisuals.Tick(_alwaysVisible, linkLengths);
                }

                #endregion
                #region draw faces

                if (_showFaces && !_isFaceVisualShown)
                {
                    _viewport.Children.Add(_faceVisuals.Visual);
                    _isFaceVisualShown = true;
                }
                else if (!_showFaces && _isFaceVisualShown)
                {
                    _viewport.Children.Remove(_faceVisuals.Visual);
                    _isFaceVisualShown = false;
                }

                if (_isFaceVisualShown)
                {
                    _faceVisuals.Tick(_alwaysVisible);
                }

                #endregion
            }

            public void Dispose()
            {
                if (_pointVisuals != null)
                {
                    _viewport.Children.Remove(_pointVisuals.Visual);
                    _pointVisuals = null;
                }

                if (_lineVisuals != null)
                {
                    _viewport.Children.Remove(_lineVisuals.Visual);
                    _lineVisuals = null;
                }

                if (_faceVisuals != null && _isFaceVisualShown)
                {
                    _viewport.Children.Remove(_faceVisuals.Visual);
                    _faceVisuals = null;
                    _isFaceVisualShown = false;
                }

                _grid.Children.Clear();
            }

            #region Properties

            public CollapseSphere4D_Settings Settings
            {
                get
                {
                    return new CollapseSphere4D_Settings()
                    {
                        ShowDots = chkShowDots.IsChecked.Value,
                        ShowLines = chkShowLines.IsChecked.Value,
                        ShowFaces = chkShowFaces.IsChecked.Value,
                        AlwaysVisible = chkAlwaysVisible.IsChecked.Value,

                        Latch_MaxForce = Latch_MaxForce,
                        Latch_Distance_Inner = Latch_Distance_Inner,
                        Latch_Distance_Outer = Latch_Distance_Outer,
                    };
                }
            }

            private double _latch_MaxForce = 3;
            private double Latch_MaxForce
            {
                get
                {
                    return _latch_MaxForce;
                }
                set
                {
                    _latch_MaxForce = value;

                    if (_latches != null)
                    {
                        foreach (LatchJoint joint in _latches.SelectMany(o => o))
                        {
                            joint.MaxForce = _latch_MaxForce;
                        }
                    }
                }
            }

            private double _latch_distance_inner = .03;
            private double Latch_Distance_Inner
            {
                get
                {
                    return _latch_distance_inner;
                }
                set
                {
                    _latch_distance_inner = value;

                    if (_latches != null)
                    {
                        foreach (LatchJoint joint in _latches.SelectMany(o => o))
                        {
                            joint.Distance_Latch = _latch_distance_inner;
                        }
                    }
                }
            }

            private double _latch_distance_outer = 3;
            private double Latch_Distance_Outer
            {
                get
                {
                    return _latch_distance_outer;
                }
                set
                {
                    _latch_distance_outer = value;

                    if (_latches != null)
                    {
                        foreach (LatchJoint joint in _latches.SelectMany(o => o))
                        {
                            joint.Distance_Force = _latch_distance_outer;
                        }
                    }
                }
            }

            #endregion

            #region Private Methods - build

            private void BuildControls(CollapseSphere4D_Settings settings)
            {
                StackPanel panel = new StackPanel()
                {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(8),
                };

                Grid.SetIsSharedSizeScope(panel, true);

                _grid.Children.Add(panel);

                #region show dots

                chkShowDots = new CheckBox()
                {
                    Content = "Show Dots",
                    IsChecked = settings?.ShowDots ?? false,
                };

                chkShowDots.Checked += (s, e) => _showDots = chkShowDots.IsChecked.Value;
                chkShowDots.Unchecked += (s, e) => _showDots = chkShowDots.IsChecked.Value;

                _showDots = chkShowDots.IsChecked.Value;

                panel.Children.Add(chkShowDots);

                #endregion
                #region show lines

                chkShowLines = new CheckBox()
                {
                    Content = "Show Lines",
                    IsChecked = settings?.ShowLines ?? false,
                };

                chkShowLines.Checked += (s, e) => _showLines = chkShowLines.IsChecked.Value;
                chkShowLines.Unchecked += (s, e) => _showLines = chkShowLines.IsChecked.Value;

                _showLines = chkShowLines.IsChecked.Value;

                panel.Children.Add(chkShowLines);

                #endregion
                #region show faces

                chkShowFaces = new CheckBox()
                {
                    Content = "Show Faces",
                    IsChecked = settings?.ShowFaces ?? true,
                };

                chkShowFaces.Checked += (s, e) => _showFaces = chkShowFaces.IsChecked.Value;
                chkShowFaces.Unchecked += (s, e) => _showFaces = chkShowFaces.IsChecked.Value;

                _showFaces = chkShowFaces.IsChecked.Value;

                panel.Children.Add(chkShowFaces);

                #endregion

                #region always visible

                chkAlwaysVisible = new CheckBox()
                {
                    Content = "Always Visible",
                    ToolTip = "When unchecked, only triangles near the slice plane will be visible",
                    IsChecked = settings?.AlwaysVisible ?? true,       // true for 3D, false for 4D
                    Margin = new Thickness(0, 8, 0, 0),
                };

                chkAlwaysVisible.Checked += (s, e) => _alwaysVisible = chkAlwaysVisible.IsChecked.Value;
                chkAlwaysVisible.Unchecked += (s, e) => _alwaysVisible = chkAlwaysVisible.IsChecked.Value;

                _alwaysVisible = chkAlwaysVisible.IsChecked.Value;

                panel.Children.Add(chkAlwaysVisible);

                #endregion

                #region expander: latches

                #region expander/panel

                Expander expanderLatches = new Expander()
                {
                    Header = "Latches",
                    IsExpanded = false,
                    Margin = new Thickness(0, 8, 0, 0),
                };

                panel.Children.Add(expanderLatches);

                StackPanel panelLatches = new StackPanel()
                {
                    Margin = new Thickness(8),
                };

                expanderLatches.Content = panelLatches;

                #endregion

                #region Slider: Max Force

                var maxForce = GetGrid_KeyValue_SharedSize("max force", true);
                panelLatches.Children.Add(maxForce.grid);

                maxForce.slider.Minimum = .1;
                maxForce.slider.Maximum = 12;
                maxForce.slider.Value = settings?.Latch_MaxForce ?? 3;

                maxForce.slider.ValueChanged += (s, e) =>
                {
                    Latch_MaxForce = maxForce.slider.Value;
                    maxForce.label.Content = Latch_MaxForce.ToStringSignificantDigits(3);
                };

                Latch_MaxForce = maxForce.slider.Value;
                maxForce.label.Content = Latch_MaxForce.ToStringSignificantDigits(3);

                #endregion

                #region Slider: Inner Distance

                var innerDistance = GetGrid_KeyValue_SharedSize("inner distance", true);
                panelLatches.Children.Add(innerDistance.grid);

                innerDistance.slider.Minimum = .002;
                innerDistance.slider.Maximum = .3;
                innerDistance.slider.Value = settings?.Latch_Distance_Inner ?? .03;

                innerDistance.slider.ValueChanged += (s, e) =>
                {
                    Latch_Distance_Inner = innerDistance.slider.Value;
                    innerDistance.label.Content = Latch_Distance_Inner.ToStringSignificantDigits(3);
                };

                Latch_Distance_Inner = innerDistance.slider.Value;
                innerDistance.label.Content = Latch_Distance_Inner.ToStringSignificantDigits(3);

                #endregion

                #region Slider: Outer Distance

                var outerDistance = GetGrid_KeyValue_SharedSize("outer distance", true);
                panelLatches.Children.Add(outerDistance.grid);

                outerDistance.slider.Minimum = .25;
                outerDistance.slider.Maximum = 6;
                outerDistance.slider.Value = settings?.Latch_Distance_Outer ?? 3;

                outerDistance.slider.ValueChanged += (s, e) =>
                {
                    Latch_Distance_Outer = outerDistance.slider.Value;
                    outerDistance.label.Content = Latch_Distance_Outer.ToStringSignificantDigits(3);
                };

                Latch_Distance_Outer = outerDistance.slider.Value;
                outerDistance.label.Content = Latch_Distance_Outer.ToStringSignificantDigits(3);

                #endregion

                #endregion

                // Simulation speed?
                // Various force settings?
            }

            private static (EdgeLink[][] links, LatchJoint[][] latches, (int, int, int)[][] faces, TetraNormal[] normals, VectorND[] points) GetSpherePoints(int dimensions, int count, double slicePlane, double? topSlicePlane, double latch_MaxForce, double latch_Distance_Inner, double latch_Distance_Outer)
            {
                // Points
                VectorND[] points = MathND.GetRandomVectors_Spherical_Shell_EvenDist(dimensions, RADIUS, count);

                // The convex hull can be thought of as a delaunay around the surface of a sphere
                var hull = GetConvexHull(points);

                // Filter out points that are below the plane
                var filtered = FilterSlicePlane(points, hull, slicePlane, topSlicePlane);

                // Latches
                var latches = UtilityNOS.GetLatches(filtered.delanay, filtered.points, latch_MaxForce, latch_Distance_Inner, latch_Distance_Outer);

                // 3D can just take each delaunay cell and create a single triangle, but 4D needs to turn 4 points into a tetrahedron, which will be 4 triangles
                switch (dimensions)
                {
                    case 3:
                        var lf3 = GetSpherePoints_LinksFaces_3D(latches.delaunay, latches.points);

                        return (lf3.links, latches.latches, lf3.faces, null, latches.points);

                    case 4:
                        var lf4 = GetSpherePoints_LinksFaces_4D(latches.delaunay, latches.points);

                        return (lf4.links, latches.latches, lf4.faces, lf4.normals, latches.points);

                    default:
                        throw new ApplicationException($"Unexpected number of dimensions: {dimensions}");
                }
            }
            private static (EdgeLink[][] links, (int, int, int)[][] faces) GetSpherePoints_LinksFaces_3D((int, int)[][] delaunay, VectorND[] points)
            {
                // Links
                EdgeLink[][] links = delaunay.
                    Select(o => o.Select(p => new EdgeLink(p.Item1, p.Item2, points, (points[p.Item2] - points[p.Item1]).Length)).ToArray()).
                    ToArray();

                // Faces (each cell is 3 points, so a single triangle)
                var faces = delaunay.
                    Select(o =>
                    {
                        int[] indices = UtilityCore.GetPolygon(o);
                        return new[] { (indices[0], indices[1], indices[2]) };
                    }).
                    ToArray();

                return (links, faces);
            }
            private static (EdgeLink[][] links, (int, int, int)[][] faces, TetraNormal[] normals) GetSpherePoints_LinksFaces_4D((int, int)[][] delaunay, VectorND[] points)
            {
                // Each cell of a 4D convex hull is 4 points.  Those four points need to be made into a tetrahedron
                var tetras = delaunay.
                    Select(o =>
                    {
                        int[] indices = UtilityCore.GetPolygon(o);

                        return Tetrahedron.GetFacesEdges(indices[0], indices[1], indices[2], indices[3]);
                    }).
                    ToArray();

                EdgeLink[][] links = tetras.
                    Select(o => o.edges.Select(p => new EdgeLink(p.Item1, p.Item2, points, (points[p.Item2] - points[p.Item1]).Length)).ToArray()).
                    ToArray();

                var faces = tetras.
                    Select(o => o.faces).
                    ToArray();

                var normals = tetras.
                    Select(o => GetTetraNormal(o.edges, points)).
                    ToArray();

                return (links, faces, normals);
            }

            //NOTE: No matter which point is chosen, the normal is the same (some will point in the opposite direction, but otherwise the same)
            private static TetraNormal GetTetraNormal_FULLTEST((int, int)[] edges, VectorND[] points)
            {
                int[] indices = edges.
                    SelectMany(o => new[] { o.Item1, o.Item2 }).
                    Distinct().
                    ToArray();

                List<TetraNormal> retVal = new List<TetraNormal>();

                foreach (int from in indices)
                {
                    var touching = edges.
                        Where(o => o.Item1 == from).
                        Select(o => o.Item2).
                        Concat(edges.
                            Where(o => o.Item2 == from).
                            Select(o => o.Item1)).
                        ToArray();

                    retVal.Add(new TetraNormal()
                    {
                        AllPoints = points,
                        Index_Common = from,
                        Indices_To = touching,
                    });
                }

                var test1 = retVal.
                    Select(o => o.GetNormal()).
                    ToArray();

                var test2 = test1.
                    Select(o => o.ToUnit()).
                    ToArray();

                return retVal[0];
            }
            private static TetraNormal GetTetraNormal((int, int)[] edges, VectorND[] points)
            {
                int from = edges[0].Item1;

                var touching = edges.
                    Where(o => o.Item1 == from).
                    Select(o => o.Item2).
                    Concat(edges.
                        Where(o => o.Item2 == from).
                        Select(o => o.Item1)).
                    ToArray();

                TetraNormal retVal = new TetraNormal()
                {
                    AllPoints = points,
                    Index_Common = from,
                    Indices_To = touching,
                };

                VectorND normal = retVal.GetNormal();

                // They all seem to be very close to .9
                //double test = VectorND.DotProduct(points[from].ToUnit(), normal.ToUnit());

                retVal.IsStandardNormal = VectorND.DotProduct(points[from], normal) > 0;

                return retVal;
            }

            private static (int, int)[][] GetConvexHull(VectorND[] points)
            {
                IndexedVertex[] vertices = points.
                    Select((o, i) => new IndexedVertex()
                    {
                        Index = i,
                        Point = o,
                    }).
                    ToArray();

                var convexHull = MIConvexHull.ConvexHull.Create(vertices);

                List<(int, int)[]> retVal = new List<(int, int)[]>();

                foreach (var face in convexHull.Faces)
                {
                    retVal.Add(UtilityCore.IterateEdges(face.Vertices.Length).
                        Select(o => (face.Vertices[o.from].Index, face.Vertices[o.to].Index)).
                        ToArray());
                }

                return retVal.ToArray();
            }

            private static (VectorND[] points, (int, int)[][] delanay) FilterSlicePlane(VectorND[] points, (int, int)[][] delanay, double slicePlane, double? topSlicePlane)
            {
                int examineIndex = points[0].Size - 1;

                var shouldRemoveUnder = new Func<(int, int), bool>(o =>
                {
                    return points[o.Item1][examineIndex] < slicePlane && points[o.Item2][examineIndex] < slicePlane;
                });

                var shouldRemoveOver = new Func<(int, int), bool>(o =>
                {
                    if (topSlicePlane == null)
                    {
                        return false;
                    }

                    return points[o.Item1][examineIndex] > topSlicePlane.Value && points[o.Item2][examineIndex] > topSlicePlane.Value;
                });

                var delList = new List<(int, int)[]>(delanay);
                var removeIndices = new List<int>();

                // Find polys that need to be removed (remove if they are below the slice plane)
                int index = 0;

                while (index < delList.Count)
                {
                    //if (delList[index].All(o => points[o.Item1][examineIndex] < slicePlane && points[o.Item2][examineIndex] < slicePlane))      
                    if (delList[index].All(o => shouldRemoveUnder(o)) || delList[index].All(o => shouldRemoveOver(o)))      // only removing if the entire poly is below the plane (keep the ones that straddle the plane), or the entire poly is above
                    {
                        removeIndices.AddRange(delList[index].Select(o => o.Item1));
                        removeIndices.AddRange(delList[index].Select(o => o.Item2));
                        delList.RemoveAt(index);
                    }
                    else
                    {
                        index++;
                    }
                }

                // Create a map between old and new index
                var (map, from_to) = UtilityCore.GetIndexMap(points.Length, removeIndices);

                // Apply the map
                foreach (var set in delList)
                {
                    for (int cntr = 0; cntr < set.Length; cntr++)
                    {
                        set[cntr] = (map[set[cntr].Item1], map[set[cntr].Item2]);
                    }
                }

                VectorND[] reducedPoints = from_to.
                    Select(o => points[o.from]).
                    ToArray();

                return
                (
                    reducedPoints.
                        ToArray(),

                    // There are polys along the edge that point to removed points.  Don't include those partial polys
                    delList.
                        Where(o => !o.Any(p => p.Item1 < 0 || p.Item2 < 0)).
                        ToArray()
                );
            }

            private static (Grid grid, Slider slider, Label label) GetGrid_KeyValue_SharedSize(string key, bool setVerticalMargin)
            {
                // Grid
                Grid grid = new Grid();

                if (setVerticalMargin)
                {
                    grid.Margin = new Thickness(0, 8, 0, 0);
                }

                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto), SharedSizeGroup = "KeyGroup" });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(8, GridUnitType.Pixel) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star), SharedSizeGroup = "ValueGroup" });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(8, GridUnitType.Pixel) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto), SharedSizeGroup = "LabelGroup" });

                // Left Label
                Label leftLabel = new Label()
                {
                    Content = key,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center,
                };

                Grid.SetColumn(leftLabel, 0);
                grid.Children.Add(leftLabel);

                // Slider
                Slider slider = new Slider()
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    MinWidth = 100,
                };

                Grid.SetColumn(slider, 2);
                grid.Children.Add(slider);

                // Right Label
                Label rightLabel = new Label()
                {
                    VerticalAlignment = VerticalAlignment.Center,
                };

                Grid.SetColumn(rightLabel, 4);
                grid.Children.Add(rightLabel);

                return (grid, slider, rightLabel);
            }

            #endregion
            #region Private Methods - tick

            private static void Force_TowardPlane(VectorND[] forces, VectorND[] points, double slicePlane, int dimensions, double max)
            {
                double[] towardSlicePlane = new double[dimensions];

                for (int cntr = 0; cntr < points.Length; cntr++)
                {
                    double distFromPlane = points[cntr][dimensions - 1] - slicePlane;

                    towardSlicePlane[dimensions - 1] = UtilityCore.Cap(-distFromPlane * .75, -max, max);

                    forces[cntr] += new VectorND(towardSlicePlane);
                }
            }

            private static void Force_AwayFromCenter(VectorND[] forces, VectorND[] points, int dimensions, double max, double radius)
            {
                int dim_1 = dimensions - 1;     // this is what's really needed througout this function

                VectorND[] points_0 = points.
                    Select(o =>
                    {
                        VectorND o1 = o.Clone();
                        o1[dim_1] = 0;
                        return o1;
                    }).
                    ToArray();

                VectorND center = MathND.GetCenter(points_0);

                double maxRadius = points_0.
                    Max(o => (o - center).LengthSquared);

                maxRadius = Math.Sqrt(maxRadius);

                for (int cntr = 0; cntr < points.Length; cntr++)
                {
                    VectorND away = points_0[cntr] - center;

                    //double force = (maxRadius / radius) * max;
                    double force = (away.Length / maxRadius) * max;

                    forces[cntr] += away.ToUnit(false) * force;
                }
            }

            private static void Force_WrongFacing(VectorND[] forces, VectorND[] points, FaceVisuals.TriangleModel[] faces, TetraNormal[] normals, int dimensions, double force)
            {
                // This was copied from FaceVisuals.Tick.  Should probably optimize and only calculate the normal once

                switch (dimensions)
                {
                    case 3:
                        foreach (var face in faces)
                        {
                            Force_WrongFacing_3(forces, points, face, dimensions, force);
                        }
                        break;

                    case 4:
                        foreach (TetraNormal normal in normals)
                        {
                            Force_WrongFacing_4(forces, points, normal, dimensions, force);
                        }
                        break;

                    default:
                        throw new ApplicationException($"Unexpected number of dimensions: {dimensions}");
                }
            }
            private static void Force_WrongFacing_3(VectorND[] forces, VectorND[] points, FaceVisuals.TriangleModel face, int dimensions, double force)
            {
                Point3D p0 = points[face.Indices[0]].ToPoint3D(false);
                Point3D p1 = points[face.Indices[1]].ToPoint3D(false);
                Point3D p2 = points[face.Indices[2]].ToPoint3D(false);

                // This was copied from Triangle.Normal
                Vector3D dir1 = p0 - p1;
                Vector3D dir2 = p2 - p1;

                Vector3D triangleNormal = Vector3D.CrossProduct(dir2, dir1);
                if (!face.IsStandardNormal)
                {
                    triangleNormal = -triangleNormal;
                }

                if (triangleNormal.Z < 0)
                {
                    VectorND awayForce = (triangleNormal.ToUnit(false) * force).ToVectorND();

                    foreach (int index in face.Indices)
                    {
                        forces[index] += awayForce;
                    }
                }
            }
            private static void Force_WrongFacing_4(VectorND[] forces, VectorND[] points, TetraNormal tetra, int dimensions, double force)
            {
                VectorND normal = tetra.GetNormal();
                if (!tetra.IsStandardNormal)
                {
                    normal = -normal;
                }

                if (normal[normal.Size - 1] < 0)
                {
                    VectorND awayForce = normal.ToUnit() * force;

                    foreach (int index in tetra.Indices)
                    {
                        forces[index] += awayForce;
                    }
                }
            }

            #endregion
        }

        #endregion

        #region Declaration Section

        private readonly VisualizationColors _colors;

        private static double _radius = 1d;

        private readonly TrackBallRoam _trackball;
        private readonly DispatcherTimer _timer;
        private DateTime _lastTick;
        private ITimerVisualization _currentVisualization = null;

        private bool _initialized = false;

        #endregion

        #region Constructor

        public NatureOfShapes()
        {
            InitializeComponent();

            Background = SystemColors.ControlBrush;

            _colors = new VisualizationColors()
            {
                Gray = UtilityWPF.ColorFromHex("666"),
                LightGray = UtilityWPF.ColorFromHex("AAA"),
                Negative = UtilityWPF.ColorFromHex("F00"),
                Zero = UtilityWPF.ColorFromHex("FFF"),
                Positive = UtilityWPF.ColorFromHex("00F"),
            };

            cboZeroColor.DisplayMemberPath = "Key";
            cboZeroColor.SelectedValuePath = "Value";
            cboZeroColor.Items.Add(new KeyValuePair<string, Color>("white", UtilityWPF.ColorFromHex("FFF")));
            cboZeroColor.Items.Add(new KeyValuePair<string, Color>("black", UtilityWPF.ColorFromHex("000")));
            cboZeroColor.Items.Add(new KeyValuePair<string, Color>("gray", UtilityWPF.ColorFromHex("808080")));
            cboZeroColor.Items.Add(new KeyValuePair<string, Color>("green", UtilityWPF.ColorFromHex("0F0")));
            cboZeroColor.Items.Add(new KeyValuePair<string, Color>("yellow", UtilityWPF.ColorFromHex("FF0")));
            cboZeroColor.SelectedIndex = 0;

            _trackball = new TrackBallRoam(_camera);
            _trackball.EventSource = grdViewPort;       //NOTE:  If this control doesn't have a background color set, the trackball won't see events (I think transparent is ok, just not null)
            _trackball.AllowZoomOnMouseWheel = true;
            _trackball.InertiaPercentRetainPerSecond_Angular = .5;
            _trackball.Mappings.AddRange(TrackBallMapping.GetPrebuilt(TrackBallMapping.PrebuiltMapping.MouseComplete_NoLeft));
            //_trackball.GetOrbitRadius += new GetOrbitRadiusHandler(Trackball_GetOrbitRadius);

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(20);
            _timer.Tick += Timer_Tick;

            _initialized = true;
        }

        #endregion

        #region Event Listeners

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                StopTimer();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                var visualization = _currentVisualization;
                if (visualization == null)
                {
                    _timer.Stop();
                    return;
                }

                DateTime now = DateTime.UtcNow;

                visualization.Tick((now - _lastTick).TotalSeconds);

                _lastTick = now;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Line_Interior_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RunTest("Linear - interior", txtRadius.Text, GetLength_Line_Interior);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Square_Interior_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RunTest("Square - interior", txtRadius.Text, GetLength_Square_Interior);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Cube_Interior_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RunTest("Cube - interior", txtRadius.Text, GetLength_Cube_Interior);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Line_Perimiter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RunTest("Linear - perimiter", txtRadius.Text, GetLength_Line_Perimiter);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Square_Perimiter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RunTest("Square - perimiter", txtRadius.Text, GetLength_Square_Perimiter);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Cube_Perimiter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RunTest("Cube - perimiter", txtRadius.Text, GetLength_Cube_Perimiter);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Circle_Interior_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RunTest("Circle - interior", txtRadius.Text, GetLength_Circle_Interior);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Sphere_Interior_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RunTest("Sphere - interior", txtRadius.Text, GetLength_Sphere_Interior);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Circle_Perimiter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RunTest("Circle - perimiter", txtRadius.Text, GetLength_Circle_Perimiter);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Sphere_Perimiter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RunTest("Sphere - perimiter", txtRadius.Text, GetLength_Sphere_Perimiter);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Sphere4D_Perimiter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //https://www.researchgate.net/post/How_can_I_uniformly_distribute_n_points_on_the_surface_of_an_N_sphere

                //If you mean uniformly distributed in the statistical sense, you can start with generating standard Gaussian distributed points in n dimensions[1]

                //(or what works equally well, and is probably easier to program, all coordinates being independent standard Gaussian distributed). Throw away
                //all points with Euclidean norm less than epsilon. Then you normalise the vectors to norm 1 to get uniformly distributed points on the sphere.


                RunTest("Sphere 4D - perimiter", txtRadius.Text, GetLength_Sphere4D_Perimiter);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Sphere5D_Perimiter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RunTest("Sphere 5D - perimiter", txtRadius.Text, GetLength_Sphere5D_Perimiter);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Sphere6D_Perimiter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RunTest("Sphere 6D - perimiter", txtRadius.Text, GetLength_Sphere6D_Perimiter);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Sphere7D_Perimiter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RunTest("Sphere 7D - perimiter", txtRadius.Text, GetLength_Sphere7D_Perimiter);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Sphere8D_Perimiter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RunTest("Sphere 8D - perimiter", txtRadius.Text, GetLength_Sphere8D_Perimiter);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Sphere12D_Perimiter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RunTest("Sphere 12D - perimiter", txtRadius.Text, GetLength_Sphere12D_Perimiter);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Sphere60D_Perimiter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RunTest("Sphere 60D - perimiter", txtRadius.Text, GetLength_Sphere60D_Perimiter);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Sphere360D_Perimiter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RunTest("Sphere 360D - perimiter", txtRadius.Text, GetLength_Sphere360D_Perimiter);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void trkTopSlicePlane_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                lblTopSlicePlaceValue.Text = trkTopSlicePlane.Value.ToStringSignificantDigits(2);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void trkSlicePlane_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                lblSlicePlaceValue.Text = trkSlicePlane.Value.ToStringSignificantDigits(2);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void CollapseSphere3D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowCollapseSphere(3);

                #region OLD

                //CollapseSphere3D.CollapseSphere3D_Settings settings = null;
                //if (_currentVisualization is CollapseSphere3D existing)
                //{
                //    settings = existing.Settings;
                //}

                //StopTimer();

                //CollapseSphere3D visualization = new CollapseSphere3D(numPoints, grdViewPort, _viewport, _camera, frontPlate, _colors, trkSlicePlane.Value, settings);

                //StartTimer(visualization);

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void CollapseSphere4D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowCollapseSphere(4);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void chkShowIn1FewerDim_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentVisualization is VisualizeUniformPoints uniform)
                {
                    uniform.ShowIn1FewerDim = chkShowIn1FewerDim.IsChecked.Value;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void chkColorByLastDim_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentVisualization is VisualizeUniformPoints uniform)
                {
                    uniform.ColorByLastDim = chkColorByLastDim.IsChecked.Value;
                }
                else if (_currentVisualization is VisualizeTessellation tess)
                {
                    tess.ColorByLastDim = chkColorByLastDim.IsChecked.Value;
                }
                else if (_currentVisualization is PolyAtAngle poly)
                {
                    poly.ColorByLastDim = chkColorByLastDim.IsChecked.Value;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void chkIgnoreBottom_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentVisualization is VisualizeUniformPoints uniform)
                {
                    uniform.IgnoreBottom = chkIgnoreBottom.IsChecked.Value;
                }
                else if (_currentVisualization is VisualizeTessellation tess)
                {
                    tess.IgnoreBottom = chkIgnoreBottom.IsChecked.Value;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void cboZeroColor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                _colors.Zero = (Color)cboZeroColor.SelectedValue;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UniformPoints2D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StopTimer();

                VisualizeUniformPoints visualization = new VisualizeUniformPoints(2, _viewport, _colors)
                {
                    ColorByLastDim = chkColorByLastDim.IsChecked.Value,
                    ShowIn1FewerDim = chkShowIn1FewerDim.IsChecked.Value,
                    IgnoreBottom = chkIgnoreBottom.IsChecked.Value,
                };

                StartTimer(visualization);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void UniformPoints3D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StopTimer();

                VisualizeUniformPoints visualization = new VisualizeUniformPoints(3, _viewport, _colors)
                {
                    ColorByLastDim = chkColorByLastDim.IsChecked.Value,
                    ShowIn1FewerDim = chkShowIn1FewerDim.IsChecked.Value,
                    IgnoreBottom = chkIgnoreBottom.IsChecked.Value,
                };

                StartTimer(visualization);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void UniformPoints4D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StopTimer();

                VisualizeUniformPoints visualization = new VisualizeUniformPoints(4, _viewport, _colors)
                {
                    ColorByLastDim = chkColorByLastDim.IsChecked.Value,
                    ShowIn1FewerDim = chkShowIn1FewerDim.IsChecked.Value,
                    IgnoreBottom = chkIgnoreBottom.IsChecked.Value,
                };

                StartTimer(visualization);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Tessellate3D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StopTimer();

                VisualizeTessellation visualization = new VisualizeTessellation(3, _viewport, _colors)
                {
                    ColorByLastDim = chkColorByLastDim.IsChecked.Value,
                    IgnoreBottom = chkIgnoreBottom.IsChecked.Value,
                };

                StartTimer(visualization);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Tessellate4D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StopTimer();

                VisualizeTessellation visualization = new VisualizeTessellation(4, _viewport, _colors)
                {
                    ColorByLastDim = chkColorByLastDim.IsChecked.Value,
                    IgnoreBottom = chkIgnoreBottom.IsChecked.Value,
                };

                StartTimer(visualization);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PolyAtAngle3D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StopTimer();

                PolyAtAngle visualization = new PolyAtAngle(3, _viewport, frontPlate, _colors)
                {
                    ColorByLastDim = chkColorByLastDim.IsChecked.Value,
                };

                StartTimer(visualization);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void PolyAtAngle4D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StopTimer();

                PolyAtAngle visualization = new PolyAtAngle(4, _viewport, frontPlate, _colors)
                {
                    ColorByLastDim = chkColorByLastDim.IsChecked.Value,
                };

                StartTimer(visualization);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void chkRunning_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_timer == null)
                {
                    return;
                }

                if (chkRunning.IsChecked.Value)
                {
                    _lastTick = DateTime.UtcNow;
                    _timer.Start();
                }
                else
                {
                    _timer.Stop();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MaintainTriangle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StopTimer();

                MaintainTriangle visualization = new MaintainTriangle(_viewport, frontPlate, _colors);

                StartTimer(visualization);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void LatchJoint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StopTimer();

                LatchJointTester visualization = new LatchJointTester(grdViewPort, _viewport, _camera, frontPlate, _colors);

                StartTimer(visualization);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void LatchJoint2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StopTimer();

                LatchJointTester2 visualization = new LatchJointTester2(grdViewPort, _viewport, _camera, frontPlate, _colors);

                StartTimer(visualization);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void LatchJoint3_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StopTimer();

                LatchJointTester3 visualization = new LatchJointTester3(grdViewPort, _viewport, _camera, frontPlate, _colors);

                StartTimer(visualization);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Gaussian_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double LINE = .01;

                if (!double.TryParse(txtGaussianMax.Text, out double MAX_X))
                {
                    MessageBox.Show("Couldn't parse max X", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Debug3DWindow window = new Debug3DWindow()
                {
                    Title = "direct",
                };

                window.AddAxisLines(2, LINE);

                int count = 100;

                Point3D[] points = Enumerable.Range(0, count).
                    Select(o => UtilityCore.GetScaledValue(0, MAX_X, 0, count, o)).
                    Select(o => new Point3D(o, Math1D.GetGaussian(o), 0)).
                    ToArray();

                window.AddLines(points, LINE, Colors.White);

                window.Show();


                window = new Debug3DWindow()
                {
                    Title = "usage",
                };

                var graph = Debug3DWindow.GetCountGraph(() => Math1D.GetGaussian(StaticRandom.NextDouble(MAX_X)));
                window.AddGraph(graph, new Point3D(), 1);

                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void RandGuass1D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DoRandGaussTest(1, chkNegateGauss.IsChecked.Value, txtGaussianMax2.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void RandGuass2D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DoRandGaussTest(2, chkNegateGauss.IsChecked.Value, txtGaussianMax2.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void RandGuass3D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DoRandGaussTest(3, chkNegateGauss.IsChecked.Value, txtGaussianMax2.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void RandGuass4D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DoRandGaussTest(4, chkNegateGauss.IsChecked.Value, txtGaussianMax2.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void RandGuass5D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DoRandGaussTest(5, chkNegateGauss.IsChecked.Value, txtGaussianMax2.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void PythonSolution_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double DOT = .0075;
                double LINE = .005;

                #region notes

                //copied from a response here
                //https://stackoverflow.com/questions/33976911/generate-a-random-sample-of-points-distributed-on-the-surface-of-a-unit-sphere


                //https://www.onlinegdb.com/online_python_debugger

                //import numpy as np
                //normed = np.random.randn(3, 100)
                //normed /= np.linalg.norm(normed, axis = 0)
                //print(normed)

                #endregion

                var combos = new[]
                {
                    new { useNormed = false, abs=true },
                    new { useNormed = true, abs=true },
                    new { useNormed = false, abs=false },
                    new { useNormed = true, abs=false },
                };

                foreach (var combo in combos)
                {
                    List<string> numpyDumps = new List<string>();

                    if (combo.useNormed)
                    {
                        #region normed

                        numpyDumps.Add(@"[[-0.67732358 -0.08610528 -0.40442315  0.48643145  0.43422349 -0.45593619
   0.0170669  -0.32984358 -0.88499588  0.22419226 -0.37163382 -0.11346924
   0.78634639 -0.72247925 -0.10879031  0.02479572  0.07455399  0.37686087
  -0.24042707 -0.05546998 -0.96862401  0.63109137  0.81579408  0.60628959
  -0.2880953  -0.94440465 -0.04860639  0.82599986 -0.46612915 -0.13173716
  -0.06923744 -0.40074785 -0.10657543 -0.39031203  0.02083445 -0.85801496
  -0.44951042 -0.27326502 -0.63288705 -0.97232954  0.61510559 -0.95068857
   0.87146231  0.66049392  0.15666499  0.25038247 -0.57006171 -0.96970703
  -0.51011674  0.12865333  0.72516913  0.05673078 -0.92101725  0.09369391
   0.11697241 -0.33536162  0.19631397 -0.38147993 -0.95608536  0.99251892
  -0.63660796  0.5210779  -0.09984775 -0.96217248  0.70685183 -0.74301165
   0.96659302 -0.89859197  0.5482736   0.22006626  0.78379507 -0.98239157
  -0.79891242 -0.0292382   0.43677901 -0.63228973 -0.02570274  0.03314943
  -0.32008037  0.57361244  0.20677469  0.84198697 -0.63057858 -0.96850169
  -0.2044108  -0.54294913 -0.85097096  0.95801854 -0.37412553  0.85287718
  -0.52133878 -0.44541315  0.72838237 -0.03334    -0.69072878  0.58777425
  -0.42341205 -0.66793398 -0.91302612  0.48819006 -0.14715582 -0.37076019
   0.19689543 -0.47386771  0.01429368  0.678667    0.65340791  0.38699227
   0.91560712  0.4534181  -0.63084145  0.65833809 -0.23895648  0.65515958
  -0.23000603  0.58186281  0.6980769  -0.20680604  0.32954565  0.88825156
   0.9655872  -0.81063726  0.17089586 -0.98518535 -0.83805257 -0.84616496
   0.92355087  0.90385823 -0.8403909   0.504819   -0.03306567 -0.67162384
  -0.69131433 -0.7708821   0.03355181  0.09692509  0.91587825  0.72985709
  -0.45962617 -0.78206973 -0.73232551 -0.61830034 -0.25503422 -0.91320798
   0.45010897  0.29699104 -0.31929903  0.91887635  0.06795969 -0.70425194
  -0.47441257 -0.74069888  0.40210907 -0.99545782 -0.00567123 -0.07419422
  -0.35004482 -0.44192097 -0.21896505  0.74282605 -0.03325488 -0.63264501
  -0.23182984 -0.58313555  0.65568745  0.39478193  0.64051848 -0.67658216
  -0.8881127  -0.99394042  0.44970974  0.02403025 -0.41474368 -0.11298478
   0.34141857 -0.73939467 -0.71111207  0.36724526 -0.8124852  -0.36361791
  -0.36873117 -0.08459723 -0.41718621 -0.00556075  0.36032617  0.53739484
   0.35797941 -0.28417945  0.29988087 -0.28449577 -0.6515303   0.66000118
  -0.57357655 -0.86027614  0.09377281 -0.63702818 -0.15855804  0.62011357
  -0.97270626 -0.21845163]
 [-0.51613934  0.98999144 -0.20639569  0.18716462  0.87971736  0.88923163
   0.96777911  0.11256264 -0.18627613 -0.46212475  0.510661    0.78656015
   0.61294166 -0.62820175  0.70461984  0.22616951  0.82269711 -0.90916576
   0.10609175 -0.17184836  0.02494729  0.25160234 -0.4757363   0.65247476
  -0.73828616 -0.11537956  0.51982501  0.22368533  0.02778078 -0.8638695
  -0.16520825  0.69959422  0.87715213 -0.91866243 -0.95397633  0.45316962
   0.02413183 -0.59454265  0.3135018  -0.02018261 -0.70262354  0.23266654
  -0.41007253 -0.69679523  0.78034891 -0.20014972 -0.46897364 -0.03069489
   0.7476607  -0.18156178  0.09383518  0.47617104  0.00894981 -0.28207709
  -0.95971447  0.90967077  0.5874227  -0.66761918  0.13695591  0.10095854
  -0.01326994  0.84956267 -0.81857819  0.24450645  0.63587037  0.26946918
   0.07119621 -0.43593469  0.49297552 -0.1945172  -0.3231223   0.18383269
   0.43189489  0.86471046  0.63028576 -0.59100854  0.94010002 -0.45199416
   0.86668942 -0.21586013  0.34814041 -0.26453127 -0.63443887 -0.20191324
   0.11742449 -0.23247393  0.01520808  0.12481736  0.15973309  0.50457714
   0.39104087  0.64492441 -0.52455354  0.34441838 -0.6048208  -0.32530961
   0.58742044 -0.60070435 -0.3602316  -0.23470974  0.82936285 -0.92413378
  -0.85016638  0.6760306  -0.99982495 -0.5852477  -0.08745298 -0.34578398
   0.18038717  0.87295471 -0.55668203  0.19262587  0.6626696  -0.75484292
  -0.96295634  0.35982881 -0.49402218 -0.93504177  0.87993327  0.2476385
  -0.07007764 -0.49294959  0.87242913  0.12865162 -0.53445715 -0.44849537
   0.14271816 -0.21216309 -0.39988028  0.63230716 -0.19926116 -0.64403842
  -0.71450702  0.51782146  0.89517636 -0.05507576 -0.40004249 -0.53054707
  -0.70518251  0.30313873 -0.58875285  0.4259274   0.58255614 -0.26585075
  -0.88361447  0.8128299   0.53651446  0.37276287 -0.53306009 -0.08739378
  -0.64824155 -0.66948194 -0.33986397 -0.07285817  0.73648356  0.59515632
  -0.75491652  0.86198412  0.13415211  0.43028212  0.99410858  0.61753882
   0.7763659  -0.02216969  0.73494134  0.6726546   0.71295143 -0.69145558
   0.08571259  0.01097272 -0.04553715 -0.32610219  0.9096167  -0.01070796
  -0.72538044  0.31915833  0.04627676 -0.37305027  0.47008127  0.71938376
  -0.11243877 -0.95753061 -0.72803488 -0.23497535  0.93128354  0.10955831
   0.84214429 -0.93871706 -0.76009657 -0.00456476 -0.65384566 -0.64319376
   0.80970971 -0.45547831  0.33417309  0.02130462  0.77882329 -0.49201536
   0.0125265  -0.22210614]
 [ 0.52424511  0.11181604 -0.89097853  0.8534365  -0.19377131 -0.03727326
   0.25122166 -0.93730084 -0.42671243  0.85800848  0.77531519 -0.60699906
  -0.07721321 -0.28876684  0.7011958  -0.97377232  0.56357002  0.17718214
   0.96485199 -0.98356048 -0.24727548 -0.73377104 -0.32886927 -0.45463131
   0.60986444 -0.30787565 -0.85288884  0.5173868   0.88428041 -0.48618392
  -0.9838254  -0.59158185  0.46823692  0.06095789  0.29915729 -0.24175945
   0.89294907  0.75620452 -0.70793404  0.23274005  0.35772234  0.2050793
   0.26906127 -0.27968587 -0.60540207 -0.94723213  0.67460609 -0.24233468
  -0.42518747  0.97492751  0.68214712 -0.8775208   0.38941897 -0.95480572
   0.25547131  0.24501361 -0.78510853 -0.63934161  0.25912135 -0.06865538
   0.77107342 -0.08198225  0.56565023 -0.12016953 -0.30988605  0.6126337
  -0.24622964 -0.0499341   0.67555251  0.95589429 -0.53033693  0.03335184
   0.41857585  0.50141894  0.64184418  0.50091776 -0.33992842 -0.89140473
  -0.38261991  0.79017287  0.91435359  0.47019268  0.44705479  0.14572415
  -0.97181671 -0.80694616  0.52499252 -0.25811065 -0.91351816 -0.13417314
  -0.75848066 -0.6210311  -0.44079782  0.93822408 -0.39634021  0.74073956
   0.6896807   0.43933868  0.19135439  0.8405842   0.53898277 -0.09226939
  -0.48831273  0.56429781 -0.01207286 -0.44372991  0.75193755  0.85479262
   0.35933839 -0.17989467  0.54050364  0.72765804 -0.70976672  0.0312746
  -0.14075624  0.72935513  0.51829599  0.28797247  0.34222377  0.38689059
   0.25046054  0.31601889  0.45788865  0.11339573 -0.10965147 -0.2878485
  -0.35592882  0.37152001  0.36583999  0.58766098 -0.97938841  0.36624572
   0.10753704  0.37094706 -0.44444748  0.99376667 -0.03366054 -0.43107823
  -0.53987166 -0.54449412  0.3421541   0.66052293  0.77174211  0.30882772
  -0.12894722  0.50110266  0.78115322  0.129283    0.84334359  0.70455059
  -0.59558006 -0.05620586 -0.85017691  0.06128139  0.67643167  0.80017758
   0.55458963  0.24837318 -0.96646651 -0.51290034  0.10316122 -0.46733938
  -0.58609804  0.81207231 -0.1730179   0.62584584 -0.28537054  0.25323067
   0.45156305 -0.1093711   0.89201318 -0.94502905 -0.02418978  0.99353902
   0.59770944  0.59281826 -0.70155405 -0.85203545 -0.34480633  0.5918353
   0.9227106   0.27564188  0.5439861  -0.97198542  0.05362857  0.83618405
   0.40329113 -0.19507006  0.57647607  0.95866643 -0.38470004 -0.38820129
   0.12401665  0.22905127 -0.93783527  0.77054605  0.606872    0.61104832
  -0.23170157  0.95023563]]


");

                        #endregion
                        #region normed

                        numpyDumps.Add(@"[[ 0.87453577 -0.62676883  0.76766226 -0.78525688  0.86724294 -0.38805445
  -0.18752127  0.88665796 -0.65712962 -0.14461438  0.61528819 -0.36988019
   0.60413672  0.59872304  0.21221864 -0.28528356 -0.75279705  0.05300749
   0.17477995 -0.86121531  0.52103013  0.66183041  0.43234118 -0.0960575
   0.70434448  0.43498591 -0.01133028  0.10007901  0.54776901 -0.5724356
   0.17731368 -0.01160088  0.27508311  0.65141098 -0.44340314  0.59506409
   0.51080411  0.28880791  0.82345559 -0.49398974  0.2590257   0.59910682
   0.79714932 -0.29652293 -0.39977844  0.48280492 -0.89042144 -0.22534726
  -0.61587971  0.41361001 -0.44567069 -0.90467462 -0.63773468 -0.52554576
   0.48003891  0.08511536  0.33376007 -0.98755908 -0.16770266  0.25344151
  -0.85134807 -0.56800931 -0.43944598  0.46762625 -0.54180585  0.10242679
  -0.18725678  0.11429841 -0.19045793  0.56780803 -0.88341759 -0.79441105
   0.30321792  0.47452679 -0.91572461 -0.63607922 -0.91190653  0.75320362
  -0.21998679  0.76147125  0.86737002 -0.65958974 -0.59834997  0.59890526
  -0.28334997  0.59820165 -0.16849839 -0.15582343  0.0134318  -0.93084249
  -0.06401678 -0.68784848 -0.58908487  0.5709003  -0.34676761 -0.17460168
  -0.31458136  0.63352752  0.45071892 -0.51794322  0.84245482 -0.91791027
  -0.6764991   0.50706758  0.0087519   0.52850576  0.50524395 -0.55202512
  -0.81935955  0.52293212  0.29596707 -0.59855611  0.91200283 -0.47152805
   0.76298612  0.8937282   0.51576036 -0.53684378  0.76999268 -0.36959955
  -0.32196863  0.20389484 -0.22653067  0.83658632  0.93812275  0.56569724
   0.63618838 -0.99178837  0.50579368  0.69161815 -0.64144414 -0.64127327
   0.83853398 -0.38999667 -0.30936102 -0.79435075 -0.84468946  0.935991
  -0.97502944  0.69239844  0.48532474 -0.81957834 -0.42210493 -0.09419574
  -0.30508279 -0.09488694 -0.99459966 -0.40434521  0.47893607 -0.24387168
  -0.21295042 -0.04284952  0.24216281 -0.30338777  0.04423379 -0.07250328
   0.09072326 -0.4767138  -0.98645451  0.72063731 -0.4100656   0.27323635
  -0.59181815  0.49414811  0.65308293 -0.40970416 -0.83419911  0.81313092
   0.2856677  -0.98421426  0.19556387 -0.69258158  0.78214421 -0.00861174
   0.01313266  0.52114925  0.30251631 -0.09981813  0.75548607  0.9996912
  -0.29899838 -0.04527499  0.71301741 -0.55359408 -0.33712692 -0.03409977
   0.08575764  0.2925357  -0.82806836 -0.15259073  0.04430253 -0.54459163
   0.2286485  -0.44654558  0.11969455  0.90955475 -0.20159521 -0.86023118
  -0.44374767 -0.69673736]
 [ 0.210607   -0.21338975 -0.63988817 -0.61028235  0.31815321  0.7233503
   0.2043871   0.44577417 -0.48869318 -0.5482494  -0.15883753 -0.71320057
  -0.1194906   0.67928733 -0.18768082  0.92105005 -0.51154214  0.92656006
   0.39290861 -0.00702594  0.85348234 -0.43753734 -0.84853104 -0.92077584
  -0.51164089  0.89887781 -0.38452233  0.52587114 -0.02552552  0.18467448
   0.85502408  0.96539538  0.26284261 -0.41427499 -0.50646437  0.65494749
  -0.76509323  0.95688075 -0.51527314 -0.02722774  0.35635166 -0.7339796
  -0.41799395  0.67880654 -0.54123082  0.44055869  0.30715477 -0.91081303
   0.78781508 -0.45368493  0.00932448  0.42247689  0.76420175 -0.83890151
   0.16274934  0.01933503  0.93291144  0.11918704 -0.98573086 -0.64918431
  -0.31378151 -0.13082706  0.84095539 -0.87148939  0.76869728  0.2905249
   0.90894846  0.38175469  0.77390075 -0.19721811 -0.29741142 -0.60737811
  -0.18324097 -0.43787338  0.29823446 -0.32950064  0.31018713 -0.25556641
   0.78950107 -0.20631471  0.0355899   0.51873135 -0.49388982  0.77454411
   0.11406285  0.18931041 -0.78171359  0.60517079 -0.97497641 -0.36014314
  -0.64114221 -0.72585243 -0.72211246 -0.70135986  0.42697854 -0.98462452
  -0.94795139  0.67314607  0.27248524 -0.77608068 -0.5241709   0.16951704
  -0.33908809 -0.71474083  0.84380792  0.81163938  0.47882036  0.83036323
  -0.34409448  0.2331004   0.9466049   0.7927766  -0.21887833  0.85187509
   0.48202924  0.0831199   0.50617124 -0.49142323 -0.47314073  0.37211187
  -0.32062504 -0.48793638 -0.24235418  0.27041157  0.13050982 -0.56641509
  -0.65388313 -0.09137869 -0.75376801  0.61006801 -0.7291172   0.4613804
  -0.41399355 -0.53851004 -0.94800702 -0.25543826 -0.46559295  0.18268652
   0.11030568  0.71998132 -0.79558073 -0.4097814  -0.34784788  0.98695577
   0.94180626  0.74502615  0.10114039  0.81566551  0.76040095  0.6441953
   0.2079817   0.99862371  0.52270473  0.68847581 -0.28145302  0.24158741
  -0.34081281 -0.78114284  0.16402243 -0.29377729 -0.83653536 -0.4023765
   0.56289204  0.59401433 -0.73531093  0.62691936 -0.5458259   0.58015658
   0.88682647  0.06554259 -0.22090698  0.71995835 -0.61577713 -0.70638562
   0.99082252  0.5888673  -0.94667572 -0.01615104  0.64986499  0.02062865
  -0.84072837 -0.01601424 -0.53997064  0.78103426 -0.03973435  0.95666179
  -0.96488576 -0.71447983  0.26175494  0.09317587 -0.97289685 -0.75938576
  -0.78817781  0.68511166  0.87787866 -0.24755297 -0.82748346  0.03192578
   0.48958817 -0.52876125]
 [-0.43684308  0.74941687  0.03518225 -0.10453273 -0.38297287  0.57112003
  -0.96076099 -0.12297583  0.57389862  0.82371675 -0.77213411  0.59542724
  -0.78787107 -0.42438125  0.95903032 -0.26510395  0.41427194 -0.37239316
   0.90281493 -0.50819172 -0.00977194 -0.60872127  0.30508388 -0.37808042
   0.4920594   0.05297117 -0.92304615  0.84465599 -0.83624013 -0.79888224
   0.48733324  0.26053249  0.92479351 -0.63564139  0.73951843 -0.46577088
   0.39206059  0.03113228 -0.23751732 -0.8690413   0.89773001  0.31991401
   0.43569946 -0.67178555  0.73976104  0.75684044 -0.33586547  0.34588759
   0.00629181  0.78936478 -0.89514842 -0.05547176 -0.09638549  0.14158356
   0.86201815 -0.99618348  0.13520529  0.10257442 -0.01450859  0.71716605
  -0.42041364  0.8125575   0.3157234   0.14775633  0.33992781 -0.95136956
   0.3724884  -0.91716914  0.60398957  0.7991865   0.3621047  -0.00170637
  -0.93513723 -0.76360411  0.26926685  0.69773387  0.26871999  0.60611064
   0.57296935 -0.61448822 -0.49638958 -0.54392938 -0.63091217  0.20345497
  -0.95220925  0.77866319  0.6004433   0.78069672 -0.22190221  0.06188039
   0.76474735  0.00164721 -0.36267425 -0.42681049 -0.83512966 -0.00536726
   0.04926189 -0.38146722 -0.85006132 -0.35976881  0.12455823  0.3587544
   0.65373407 -0.48169287  0.53657394 -0.24884369  0.71795516 -0.07592878
  -0.45852908 -0.81988183  0.12783839 -0.11504713 -0.34690505  0.22796959
   0.43069709  0.44084122 -0.69121771  0.68578565 -0.42807606  0.85142758
   0.89080626  0.8487314  -0.94337071 -0.47644612  0.32076921 -0.59930007
  -0.40951337 -0.08947493  0.41953134 -0.38662819  0.23861586  0.61310416
  -0.35421759 -0.74693342 -0.07468899 -0.55114262  0.26405098 -0.30090943
  -0.19274398 -0.04702445  0.36264472  0.40046292 -0.83715547 -0.13055829
   0.1411576   0.66025185  0.02328383 -0.41375686  0.43864638 -0.7249407
   0.95467048 -0.03024231 -0.81739644  0.65875407 -0.95855494 -0.96766668
  -0.93574351  0.40318708 -0.00203425 -0.62799425 -0.36339344 -0.8737477
   0.57697818 -0.63479494 -0.18110915  0.66265724 -0.07865071 -0.04729113
   0.36322552  0.16439724  0.95548672 -0.04461758 -0.09523109 -0.70777482
   0.13452983  0.61776918  0.11085563 -0.99487461 -0.08316426 -0.01385492
   0.45141531 -0.99884619 -0.44725594 -0.28899668  0.94062034 -0.2891979
   0.24827624 -0.63556387 -0.49576925 -0.98388735 -0.22695594  0.35602419
  -0.57139793 -0.57551633 -0.46368338 -0.33380784 -0.524052    0.50890379
   0.75059405  0.48473559]]


");

                        #endregion
                        #region normed

                        numpyDumps.Add(@"[[ 0.70855522  0.13564605 -0.70934349  0.92494547 -0.70689624  0.62237477
   0.79275017  0.38825886  0.23344493  0.67101538  0.69417942 -0.58538838
   0.56952679 -0.23466704  0.31363264 -0.70381448 -0.55959323 -0.03451768
  -0.60812714 -0.87236406 -0.69653821  0.06357956  0.88166489  0.89436285
   0.61819266 -0.14164199 -0.65025963  0.85005849 -0.58741943  0.66734474
   0.74375344  0.90565963 -0.07542588  0.42585729  0.99590902 -0.97004323
   0.9137878   0.64259907 -0.67037457 -0.07095983 -0.24738389 -0.76964339
  -0.96930818  0.93290881 -0.79336428  0.53449972 -0.34302793  0.83941829
   0.40872078  0.85336961  0.0390132   0.67915554  0.4867999   0.4446795
  -0.96670801 -0.87404708 -0.18169642 -0.96039696 -0.30161464 -0.22326653
  -0.63084833  0.08269625  0.2401216   0.73609944 -0.66023111  0.89341015
   0.62341707 -0.58355137  0.43786668 -0.56150571 -0.53036248 -0.41582367
  -0.30425898 -0.05753785  0.98981876 -0.21132681  0.10337633  0.25128873
   0.06440951  0.64895334 -0.48853491  0.35171649 -0.68359748 -0.79389914
   0.97526878  0.70326687 -0.418919   -0.86619467 -0.76194491  0.61632339
  -0.28617839 -0.7404454   0.41112065  0.67244467  0.07789141 -0.2845397
  -0.8301944  -0.65553283  0.6096839   0.60664257  0.44474936 -0.97011355
  -0.05262006  0.23154889 -0.2464225  -0.7995364   0.99631916  0.15898639
  -0.88973612 -0.45228416 -0.63394461  0.96067097  0.30672371  0.98025718
  -0.01228479 -0.39537583  0.85688807  0.74698665 -0.12735696 -0.60934711
  -0.73422817  0.20087105  0.63120042  0.16646378  0.01754088 -0.92802981
   0.23780643  0.80247025 -0.88649362  0.42357321 -0.29100597 -0.00777964
   0.30295419 -0.0660863  -0.10999142 -0.32918622  0.03481886 -0.38473167
   0.8183941   0.03823744  0.75804434  0.62724218  0.20905315  0.02400858
   0.92845383 -0.83523437 -0.19159289 -0.19159204 -0.11189479 -0.23894512
  -0.42861914 -0.0942295  -0.53226866  0.89130032 -0.49288715  0.64589107
   0.02750083  0.07075683  0.60720506  0.50571537 -0.64465471 -0.11920755
   0.71963535  0.20288607  0.86752677  0.46735364  0.94835593  0.77826983
  -0.66883969  0.12019181 -0.58742707  0.18269911  0.14644322  0.05392219
  -0.72211513  0.43980544  0.06772716 -0.95567731  0.29827252 -0.73945836
   0.37270367  0.9448181  -0.30849517  0.88904113 -0.92572401  0.44901548
   0.03294403 -0.19281771  0.47443757  0.61273938 -0.66261002 -0.05963098
  -0.62190186  0.18780014  0.3263574   0.65048804 -0.1514943  -0.86140014
   0.82758295 -0.39840752]
 [-0.70411909 -0.95978935  0.69664856  0.04714148  0.66137657  0.7448842
   0.3077295  -0.52056506 -0.5803242  -0.58862747  0.43807249 -0.21264302
  -0.81547746  0.93989296  0.91607753  0.2399201  -0.81903574 -0.97792051
  -0.70461926  0.03056918 -0.70162087 -0.13377045 -0.12829614 -0.3556243
  -0.22665419 -0.97201413  0.72804364  0.51318082  0.12289753 -0.46141661
   0.0440482   0.39905265 -0.88468884  0.5008344  -0.08094391 -0.19025576
   0.36634045  0.59794506  0.6705215   0.05859346 -0.73376269 -0.635175
   0.0142819  -0.01158389 -0.3552913  -0.24898769  0.79605177 -0.54333209
   0.82803251  0.02858956  0.61966516  0.36444512 -0.56634244 -0.47996763
   0.08744011  0.45779484  0.95782835 -0.05894493 -0.57108893  0.83452667
   0.75437789 -0.25991269  0.96009804 -0.34332648 -0.07206996  0.35507993
  -0.34404318 -0.80874002 -0.6636774   0.10664114 -0.57772325  0.51593438
   0.95061149  0.24305507 -0.10655328  0.51583796  0.98479342  0.48090548
   0.9978683   0.66252839 -0.80826107  0.82181183 -0.68526912 -0.45740729
  -0.04803987 -0.3917402  -0.33349095  0.4972178  -0.64658924 -0.58876215
  -0.42180389 -0.28286758  0.9095823  -0.0050595   0.99694067 -0.90225865
  -0.52419953  0.44025847 -0.67481483  0.41464967  0.89220515 -0.11288506
   0.21446462  0.24471864  0.64059443 -0.3374809  -0.08561027 -0.65950221
  -0.07766771 -0.71116533 -0.25980863 -0.20584952  0.67179631 -0.19628785
  -0.21650925  0.27059959 -0.40164495  0.16375563 -0.9906685   0.71349205
  -0.17156862  0.43655969 -0.34394007  0.35596076  0.26574936 -0.07553479
  -0.05753782 -0.32950971  0.45486803 -0.87373994 -0.10846997  0.99996871
   0.02024401  0.98340749  0.13784102 -0.70264556 -0.99704586  0.29438657
   0.55944287  0.5225671  -0.3802799   0.58875568  0.05720546 -0.97889886
   0.24716038 -0.40559153 -0.96913432  0.98002219  0.65334023 -0.68543673
   0.76388647 -0.56935769 -0.59048288 -0.43915725 -0.8699178  -0.15240105
  -0.92175855  0.72390968  0.77415019 -0.85329105 -0.65015199 -0.07493462
   0.05954553 -0.77169691 -0.16642026 -0.32920317 -0.17969347 -0.38625261
  -0.71498782  0.11542762  0.44785534 -0.8557274   0.96576119  0.7683963
  -0.22517901 -0.04590461  0.34723441 -0.13382879 -0.0483296  -0.5963789
  -0.07021969 -0.02339377  0.24607124  0.30947981 -0.22437856 -0.00610672
   0.99854878 -0.95196832 -0.05220424  0.44468431 -0.41289092 -0.99184214
   0.74616293  0.70795304  0.31816667 -0.49564604  0.56027349 -0.43360724
  -0.4255279   0.62698872]
 [-0.04653819 -0.24577339  0.10729679 -0.37716516 -0.25075634 -0.24041042
  -0.52616511  0.76043874 -0.78020977 -0.45083928  0.57114571  0.78237037
   0.10312975 -0.24805767  0.24987303 -0.66864305  0.12663278  0.2061068
  -0.36564064 -0.48790006 -0.15020877  0.98897073 -0.45410034 -0.27137881
   0.75263916 -0.18741953  0.2170596   0.11851581  0.79989662 -0.58459021
  -0.66700118 -0.14330955 -0.46003977  0.753532   -0.04016609  0.15105917
  -0.17546092 -0.47909095 -0.31780316 -0.99575675 -0.63276657  0.06482109
   0.24543366 -0.35992634  0.49430882  0.80766031 -0.49863155  0.01292954
  -0.38380919 -0.52052181  0.78389608 -0.6371244  -0.66504293 -0.7562349
  -0.24047838 -0.16268245  0.22260111  0.27232915 -0.76346974  0.50370358
   0.18150587  0.96208457  0.14336447 -0.58333914 -0.74759669 -0.27520274
   0.70212922  0.07353488 -0.60646936  0.82057236  0.62044459  0.74893417
   0.06135368 -0.96830451  0.09436744 -0.83021213 -0.1396254   0.83999041
  -0.0105007  -0.37405305 -0.32870609 -0.44824194 -0.25119858 -0.40062791
   0.21573823  0.59325823 -0.84456537  0.04981208 -0.03690951 -0.52297667
   0.86033912 -0.60969381  0.0603311  -0.74013011 -0.0064989   0.32398534
   0.1897159   0.61355455 -0.41582507 -0.67827018 -0.0785365   0.21479446
   0.97531331  0.94154017 -0.72726524 -0.49683819  0.00436039  0.73469733
   0.44981926 -0.53822199 -0.72843237  0.18637933 -0.67424794  0.0238107
  -0.97620327 -0.87775498  0.3231473  -0.64435629 -0.04854003 -0.34586875
  -0.6568662  -0.87696434 -0.69519153 -0.91955519 -0.96388256 -0.36476729
  -0.96960688 -0.49745839 -0.08499498  0.23909047 -0.95055236  0.00143409
  -0.95279008 -0.16894468  0.98432807 -0.63081348 -0.06846316 -0.87482461
   0.13135739  0.85174029 -0.52986411  0.50983723 -0.97622964 -0.20293008
  -0.27728186 -0.3713207   0.15514781 -0.05337606 -0.74874969 -0.68780936
   0.48245528  0.81667167 -0.60664655  0.11280354 -0.01747191  0.74806326
   0.38678791  0.68625669  0.17886729 -0.12706828 -0.4021476  -0.99003756
  -0.69179426  0.60276125  0.46872338 -0.82049122  0.26140255 -0.4950808
   0.20358263  0.98601744 -0.67405863 -0.4840988   0.21414882  0.63769862
   0.65409796  0.89691914  0.93532951  0.26224175  0.95325639  0.31230362
   0.92528978 -0.32675908  0.91884692  0.33738423  0.3044492  -0.89350311
   0.04260302 -0.23786055  0.87873984 -0.65330415  0.62487523 -0.11266465
  -0.23765301  0.68083302 -0.89009034  0.57550006 -0.81433598  0.26452706
  -0.36610445  0.66944499]]


");

                        #endregion
                        #region normed

                        numpyDumps.Add(@"[[-0.22459944 -0.69504982 -0.21406769  0.55880643 -0.21390052  0.12209115
   0.26446362 -0.55989427  0.28995621 -0.40530376 -0.8717059  -0.3593508
  -0.84153801  0.47458651 -0.26300516  0.6868617  -0.97206462  0.69486166
   0.78654843  0.81239019  0.77533775 -0.33638624 -0.7175067  -0.09919074
  -0.03192622 -0.60690934 -0.48106353 -0.99510344 -0.29031067  0.60520584
   0.28256577  0.16692327 -0.2498886   0.01133977 -0.27665699  0.2933099
  -0.0378942   0.54405698  0.88242741  0.50024474  0.12361638  0.16967369
  -0.13142399 -0.39756433  0.94304303  0.38829628 -0.78138856  0.21212384
  -0.83362611  0.86466644  0.97050182  0.49824187  0.72863289 -0.85664966
   0.62150858  0.09520178  0.81060687 -0.83543953 -0.8055024   0.21045587
  -0.98419232 -0.52552625 -0.44406342  0.47604117 -0.32395391  0.54615252
  -0.33770869  0.40954047 -0.66733316 -0.20581178 -0.97467508 -0.11954283
  -0.7192709   0.41814876 -0.38906197 -0.89885065  0.76734302 -0.53815127
  -0.61234681  0.52668391  0.40964872  0.8372504   0.61516715  0.4842167
  -0.15700788 -0.53364788  0.03861479  0.91218825 -0.10749381  0.52065466
   0.39515997  0.11098036 -0.32703846  0.09558215  0.09554354 -0.72790012
   0.97175582  0.62156759  0.48413253 -0.53159241 -0.88666179 -0.03553648
   0.95822328 -0.39811426  0.55279808 -0.61486722  0.66670987  0.47706353
   0.5829867  -0.06964603 -0.19586836  0.99918407 -0.32683477  0.62922966
   0.9131312   0.55254307 -0.34285535 -0.10066129 -0.60208819  0.98840954
   0.61201528  0.24150212  0.7244435  -0.78198318  0.72937383  0.19583753
  -0.54546056  0.00101081  0.32671886 -0.62847186  0.38744628  0.78333537
   0.63822601 -0.16341598 -0.20972214 -0.58905543  0.77649552 -0.98491536
  -0.13863731  0.5196272  -0.61342515  0.33407567 -0.70546142 -0.4048022
   0.63907761 -0.98339173  0.73645914  0.87150604  0.49143678 -0.34208157
   0.19472273  0.93030835  0.66399774  0.63356836 -0.22102079 -0.77815199
   0.97252055 -0.76954642 -0.68169191  0.61951754  0.52219674  0.06696722
  -0.73605798 -0.41702464  0.87738465 -0.70092291  0.85726904 -0.98998439
   0.31923465  0.95990051  0.05331612  0.73938415 -0.05464846  0.87341625
   0.56457145  0.88299743  0.03150816  0.82228828 -0.88079987 -0.68838504
   0.33399728 -0.45225239  0.2364052  -0.03233296 -0.34457134  0.44382326
   0.9125059   0.19602508 -0.92104535 -0.88636604 -0.87538876  0.45047355
  -0.28040953 -0.70504357 -0.38957508 -0.10327359  0.50114701 -0.92741973
  -0.84767724 -0.16803767]
 [ 0.94430675 -0.1196705   0.81856719 -0.7006233   0.65447215  0.93334475
  -0.81264221  0.78050531 -0.69884188  0.83730693  0.44048313 -0.38497591
   0.26214864 -0.87416973  0.71042148  0.49754292 -0.23342051  0.42299012
   0.59497929 -0.15177111 -0.54705328  0.75909674 -0.53165436  0.7566458
   0.4805654   0.33495565 -0.11632327  0.08797692  0.09093314  0.16686253
  -0.627848    0.98462677  0.1356316  -0.39281966  0.12687074  0.79513536
   0.914988   -0.29023842 -0.23700089 -0.53979771  0.91464462 -0.19616703
   0.98974298 -0.64231702  0.2448671   0.87184226  0.61917688  0.29107583
  -0.25185872  0.2889833  -0.156987    0.18130759  0.25155267  0.03683241
  -0.68494752 -0.99496456  0.03655269 -0.5390115  -0.35893475 -0.19763349
  -0.16243249 -0.77422028  0.35216213 -0.573045    0.12446512 -0.43664387
  -0.86837241  0.59311554 -0.68930322 -0.51302875  0.11628387 -0.99086475
   0.6946433   0.42521411  0.92118143  0.33507708  0.23804167  0.66618563
  -0.75195615  0.64120792  0.83417015 -0.11894312  0.49117303 -0.32915127
   0.98620625 -0.4929196  -0.48543293 -0.40902361 -0.22945823  0.22668662
   0.510802    0.29699315  0.88099942  0.82243568 -0.65119923 -0.40873558
  -0.1635204   0.51559808 -0.77545787  0.62142602 -0.4614713  -0.28005478
  -0.14499222 -0.61401467  0.31802384  0.48004785 -0.27415209  0.87684374
  -0.81057528  0.6069349   0.54798242 -0.00436727 -0.93368917 -0.12087444
   0.20023502  0.31254294  0.93565004 -0.51440515 -0.27106708 -0.14217867
   0.77453003  0.66552992 -0.58459415 -0.06243005 -0.64820732 -0.63124951
  -0.30630833 -0.9926382  -0.17827194 -0.39673114 -0.85927236  0.61056005
   0.67074382 -0.23147829 -0.01782223  0.3327708   0.61901346 -0.17268123
  -0.06640175 -0.84894237  0.62695565  0.93946361  0.16712016 -0.14058759
  -0.76773859 -0.00265741 -0.66049534  0.11680977 -0.37941692 -0.31490193
   0.70995767  0.36416575 -0.74751482 -0.54473433 -0.83550131  0.62549594
  -0.04813652  0.40677767  0.00488296 -0.35977665  0.00217245  0.04152413
   0.02423626 -0.9048721   0.46442161 -0.30275467 -0.35512247 -0.09542327
  -0.8569333  -0.00241354  0.98953935  0.55823672  0.06605038  0.09667918
   0.02651593  0.4605001  -0.77439317  0.22546322  0.38873258  0.70773695
  -0.85446149  0.44729217  0.10015036 -0.98597952 -0.92838466  0.64578771
   0.24807486  0.95202659 -0.31798249 -0.43592949 -0.3692705  -0.52052918
   0.92237906  0.11896355  0.21599715 -0.00157445  0.52741224 -0.30515116
  -0.06222759  0.78856477]
 [ 0.24049918 -0.7089321   0.53303169 -0.44369174  0.72519844  0.33758158
  -0.51929918 -0.27808247 -0.65386958  0.36694137 -0.21471711 -0.85009444
   0.47232602 -0.10293165  0.65278604 -0.52978491 -0.02460165  0.58158974
  -0.16535175 -0.56301662  0.31556947  0.55732974  0.45003087 -0.64625717
  -0.87637755 -0.72073974 -0.86893428  0.04504672  0.95260216 -0.77838473
  -0.72523339  0.05144643  0.9587282  -0.91954561 -0.95255694 -0.53078156
   0.40169763  0.7872507   0.40638952 -0.67703296 -0.38489507 -0.96577913
  -0.05600514  0.65526441  0.2251887   0.29852482 -0.07779397 -0.9328871
  -0.49156353  0.41090218  0.18297896 -0.84786944 -0.63703639  0.5145821
  -0.38022887  0.03133919 -0.5844488   0.10727253  0.47152066  0.95741805
  -0.07057739 -0.35271109  0.82388683  0.66708638  0.93784983  0.7148843
   0.36315589 -0.69317427  0.28200624  0.83333248  0.19101452 -0.06242245
   0.01095682 -0.80271077 -0.0074534   0.28247275  0.59541654 -0.51632346
  -0.24411746  0.55808284  0.36925342 -0.5337268   0.61669963 -0.8106748
   0.05240006 -0.68720463  0.87342073  0.02474438  0.96736446 -0.82312326
   0.76349847 -0.94840836 -0.34188576  0.56077055 -0.75286851  0.55054213
  -0.17015203  0.58975617 -0.40531566  0.57553385  0.02958229  0.95932606
   0.24654697  0.68153578 -0.77024355  0.62569351  0.69306463  0.05962754
  -0.05562567  0.79169404 -0.81323481 -0.0401512   0.14629957  0.7677626
   0.35510188 -0.77266621 -0.08372105 -0.85161884 -0.75100762  0.05321464
   0.15981405 -0.70621998  0.3652825  -0.62016514  0.21872605 -0.75044768
   0.78015895  0.1211131   0.92815618  0.66904972 -0.33397065  0.11662815
  -0.37784957 -0.95901669  0.97759859 -0.73639479  0.11780086  0.01108736
  -0.98811462 -0.09635575  0.48024598 -0.07616812 -0.68876341 -0.90353213
   0.04644648  0.18147631  0.14619797  0.47626956  0.78392135 -0.88533439
  -0.67678886 -0.04370003  0.01812712  0.54941391 -0.5030779   0.05687108
   0.22778642 -0.49227049 -0.73162306 -0.697681   -0.85282228 -0.99689074
   0.67648448 -0.08542213 -0.12045224  0.64579151  0.3727973   0.10404474
   0.40466598 -0.28033049 -0.13404952 -0.37640782 -0.99631867 -0.47728104
  -0.82495817 -0.09085806  0.63191966  0.52250199  0.27033047  0.15885352
  -0.39792133  0.77162004  0.96647943 -0.16370392  0.13918516  0.6212722
   0.32525659  0.23498838 -0.22486129  0.155951    0.31198367 -0.72534333
   0.26568283 -0.69911461  0.89530804 -0.99465174 -0.68606705  0.21627623
   0.5268501   0.59154793]]


");

                        #endregion
                        #region normed

                        numpyDumps.Add(@"[[ 0.27222431 -0.55475525  0.42694409 -0.95109849 -0.63521443  0.67913629
   0.17635764 -0.40158672  0.19630251  0.5046586   0.78895927  0.31191345
  -0.25404054 -0.05005895 -0.78472589  0.50680761 -0.51524095 -0.2085667
  -0.18407013  0.23631941  0.6379024   0.79320248  0.83269541 -0.34701031
   0.64215654 -0.13034574  0.91666776 -0.03980358  0.46781816 -0.30156012
   0.24794951  0.74228848 -0.64977156 -0.08307987 -0.16343641 -0.50054119
   0.09711931 -0.44020085  0.79573678 -0.18571527 -0.86236113  0.35811009
  -0.86681949 -0.05426978  0.24393204 -0.95967818 -0.94102248 -0.72908778
   0.04290092  0.63392457 -0.03042251  0.8493531  -0.10111739  0.61543889
   0.24550107 -0.87819632 -0.29985151  0.03884346 -0.20765143  0.91945373
   0.36447895  0.72488133  0.66438937 -0.46035945  0.05859408 -0.5927682
   0.67972576 -0.3886805  -0.73900553 -0.90964521  0.92751083 -0.2331085
  -0.86070279 -0.09183728 -0.80336343  0.43863257  0.8201364   0.56876952
  -0.10665061  0.34439688 -0.87848999  0.10836132  0.05216356 -0.2669761
  -0.09503831  0.63623495 -0.41295966  0.09497658  0.54218192 -0.99497789
  -0.54226214  0.35852361  0.73291956 -0.4252016   0.42130067 -0.36108178
   0.12104166 -0.53960205 -0.96059881  0.63913227 -0.80400795  0.44913673
  -0.94910833  0.72753347 -0.47154488 -0.96566496  0.89971161 -0.73090845
  -0.78392284  0.37546535  0.32973854  0.46441807 -0.41945572  0.41209954
   0.1182612   0.15344638  0.30738706  0.235731   -0.87755015  0.98392843
  -0.52717029  0.16015361 -0.50412689  0.42530059 -0.54934797 -0.02064806
   0.76927471 -0.69583552 -0.97919832  0.94434682 -0.47904263  0.95604081
  -0.22098393 -0.74504938  0.38138644  0.09571994 -0.67499956 -0.43667372
  -0.20961566  0.73959838  0.76443534  0.59634284 -0.87843281  0.95735193
   0.41552345  0.51960676  0.94395641 -0.46209776 -0.19829876  0.66059925
   0.62905949  0.08918962 -0.11737095  0.10752357 -0.51760624 -0.73064776
   0.3559331   0.09415376  0.89863431 -0.68670956  0.94837927 -0.45402961
   0.58930651  0.69000041 -0.55410875 -0.96952746  0.82988517 -0.90548375
  -0.2829161  -0.09092626 -0.34211057  0.8080819   0.64458401  0.39578388
  -0.03713507  0.90688918 -0.35880891 -0.13926915 -0.42674259  0.77632853
   0.92698593  0.409598    0.7253459   0.9978451  -0.82308372  0.73151925
   0.68136884  0.24528241  0.68692361  0.28439544 -0.00671388  0.28923512
   0.04435404  0.46877011 -0.90416928  0.97474209 -0.85281653  0.18463034
   0.66439633  0.9066607 ]
 [-0.95801092 -0.76291751 -0.25911309 -0.18475212 -0.49315431 -0.23243512
   0.73265209 -0.8686925   0.61471953  0.66744075  0.18971389  0.23062954
   0.89857066  0.33922829  0.44598687  0.40467311  0.76733201 -0.82863433
   0.36614697 -0.27158632 -0.67370222 -0.02222331  0.16859505 -0.74233186
   0.7073929  -0.52461875  0.35483273  0.96159804 -0.45933001  0.01714733
   0.82843291  0.61079012 -0.59399809 -0.80715325  0.05220897 -0.37581682
  -0.97409047 -0.84931862  0.38207432  0.86022956  0.11762701 -0.77307385
   0.48214836  0.26045238 -0.44035386  0.16246429 -0.16012669  0.02235877
  -0.59137716  0.32009321 -0.42023661 -0.25885039  0.23283458  0.78783699
   0.25522787 -0.26409776 -0.42293693  0.88133988  0.82407595 -0.1549048
   0.36608938  0.46587249 -0.15676043  0.74549301  0.4363346  -0.59364193
  -0.12817267 -0.81820403 -0.64659787  0.16287262 -0.363524   -0.73182202
   0.43669035  0.90537619  0.57782523  0.62424021  0.5356057   0.79169229
   0.99256496  0.92902062 -0.47627067 -0.39161154  0.83775017  0.61562501
   0.9943568  -0.71645532 -0.23663471  0.62658397  0.59850618  0.09792295
  -0.39249905  0.46052737 -0.67574236  0.49587584  0.89070086 -0.53346498
  -0.97973945  0.36661105 -0.27574631  0.72039688 -0.57936347 -0.66788989
  -0.10234489  0.30629368 -0.3402763  -0.09177479 -0.26318966  0.19299243
  -0.62024157  0.73840273  0.58670308 -0.87158073 -0.36861428  0.85574864
   0.96223111  0.95607069 -0.30314803  0.14009726  0.0762666  -0.09313026
  -0.09030178 -0.95575337 -0.51850935  0.51654901  0.83156347 -0.04966461
   0.61837742  0.71743755  0.2011341  -0.31966725  0.84183641 -0.2803156
   0.95297498 -0.02072361 -0.68916358  0.38395531 -0.09298889  0.77088529
   0.9147695  -0.66880779  0.60224579 -0.72389193  0.38475732 -0.20729189
  -0.54075996 -0.22452193  0.09392981 -0.18824181 -0.81671132  0.30042552
   0.53836185  0.23464198  0.98726304  0.8361764  -0.65093844  0.17274535
  -0.71893589  0.98561327 -0.15851404 -0.70703416  0.07136112  0.10379945
  -0.17157141 -0.71665451  0.76379608 -0.1672325  -0.32230496  0.1839937
   0.13540555 -0.5064297  -0.52203266 -0.58823038  0.764239   -0.60403272
   0.48575571 -0.314034    0.57847092  0.09133434 -0.88845663  0.6149352
  -0.17159627  0.81997447 -0.68489876  0.04577674 -0.55940453  0.55782892
   0.01117593  0.70924559 -0.26053372  0.2862586   0.49365939  0.5322774
  -0.40515571 -0.56068639  0.40036469  0.10802302 -0.05492898 -0.16037757
  -0.08006947 -0.07870938]
 [-0.09005002 -0.3319691   0.86635972 -0.24754457  0.59439167 -0.69623833
   0.65735751 -0.29000248  0.7639275  -0.5475788  -0.58442442 -0.9216941
  -0.35781864 -0.93937121  0.43046601 -0.7611739   0.38174384 -0.51948539
  -0.91217026  0.93294909  0.37310299  0.60855234  0.52744104 -0.57317297
   0.29534768 -0.84129968 -0.18388571 -0.2715601  -0.7550908  -0.95329296
  -0.50221505 -0.27557802 -0.47430285  0.58446674 -0.98517144 -0.77988475
  -0.20424395  0.29134359 -0.46991722 -0.47488413  0.49244001  0.52355896
  -0.1271099   0.96396024  0.86405187  0.22939737 -0.29805391 -0.6840549
  -0.80525311 -0.70404544 -0.90690444 -0.45999542 -0.96724523  0.0234062
  -0.93519407 -0.39877762 -0.85511018  0.47088342 -0.52704811  0.36139914
  -0.85623225  0.50745432 -0.73076189 -0.48198481 -0.89787463  0.54425648
  -0.72218049  0.42363857  0.18916136 -0.38212315  0.08702851 -0.64038813
  -0.26171021 -0.41455984  0.14396249 -0.64647168  0.20125311 -0.22299
   0.05865533 -0.13531991  0.03770386 -0.91372765 -0.54355645  0.74143753
  -0.04714107 -0.28617629 -0.87947049 -0.77354507 -0.5897704   0.0207386
  -0.74289721 -0.81201932  0.07874756  0.75717287 -0.1707563   0.76487585
  -0.15956039  0.75790894 -0.03484116  0.26932931 -0.13382524  0.5934638
  -0.29785717  0.6139049   0.81354623  0.24304028 -0.34821003 -0.65461955
   0.02766531  0.5601671   0.73962963  0.15704423 -0.8295664   0.312839
   0.24520517 -0.24976596 -0.9020058  -0.96166712  0.47338055 -0.15235355
   0.84494797 -0.24675153  0.69065486  0.74316655  0.08196948  0.99855249
   0.16070403  0.03311025 -0.02675317 -0.07760118 -0.2486556  -0.08607627
  -0.20737596 -0.6666873   0.6161152   0.91837684 -0.73193487 -0.46373693
  -0.34533756 -0.07543459 -0.23008394 -0.34692318  0.28340358 -0.20126437
  -0.73138152  0.82437778  0.31642295 -0.86662027 -0.54190426 -0.68800664
  -0.56075901  0.96798159 -0.10740458 -0.53781755  0.55530435 -0.66053985
   0.59702831 -0.1403622  -0.40905949  0.16891618  0.30900543 -0.88491965
  -0.78948153  0.10151726  0.33102724 -0.17902458  0.455423    0.38242058
  -0.94953874  0.85747383 -0.78130805  0.03144288 -0.02121802  0.69173666
  -0.87330543 -0.28095314  0.73254868 -0.98603354  0.16892477 -0.13845111
   0.33354431 -0.39983915 -0.0691882   0.04700684  0.09797843 -0.39205419
   0.7318549  -0.66091393  0.67842327  0.91497281 -0.86962942 -0.79562794
   0.91317116  0.68255795 -0.14894977 -0.19547094 -0.51931375  0.9696343
   0.743079    0.41445291]]


");

                        #endregion
                    }
                    else
                    {
                        #region unnormed

                        numpyDumps.Add(@"[[-9.75923435e-01 -3.56575052e-01  1.27211087e+00  1.14448676e+00
  -6.91920482e-01 -6.01487792e-01 -4.54135638e-01 -1.61061934e+00
  -1.88922893e+00 -1.21168395e+00  4.92377068e-01 -3.63615815e-01
  -3.89571332e-01 -3.78077451e-01 -1.19545509e+00 -1.95973189e-01
  -1.03844241e+00 -8.57977610e-01 -1.28744351e+00  1.83093885e+00
   1.95614600e-02 -1.33654196e+00  1.47207851e+00 -1.12878101e+00
  -5.08722062e-02 -2.34454710e-02 -2.48278602e-01  1.41952226e+00
  -4.98694437e-01  6.52693967e-01  2.01324374e+00 -1.87933156e+00
  -7.27018209e-02 -7.38481043e-01  8.06044879e-01 -1.19013162e+00
  -3.00196649e+00 -2.11400071e+00  9.48484845e-01  5.87555948e-01
  -3.33637975e-01 -6.73442632e-02  4.53557836e-01  5.33496493e-01
   2.48555101e+00 -8.12585502e-01 -6.77109354e-02 -4.77662783e-01
  -1.41099338e-01 -6.54941254e-01  8.98404890e-01 -9.07919470e-01
  -4.97537323e-02  5.78359869e-01 -1.04696507e+00 -8.60035264e-01
   3.97160808e-01  2.44931271e+00 -8.70020026e-01  8.06022796e-01
  -2.82360233e+00  1.02512809e+00  3.43487723e-01  1.79648947e+00
  -5.74867223e-01  4.07655321e-01 -8.08007648e-01  6.68455329e-01
   1.50120178e+00  1.00591072e+00  1.01417147e-01 -1.62483305e+00
  -8.76770955e-01 -2.08551610e+00  4.51156574e-01  8.26641820e-01
  -1.08845753e+00  6.60904801e-01  1.04791607e+00 -1.62326866e+00
  -7.68638908e-01 -5.00569909e-01 -9.71306988e-02 -2.76574398e-01
   2.94536702e-01  2.54274210e-03  4.40424772e-01 -3.94064634e-02
   5.64920907e-01 -5.91676601e-01  7.30398420e-01  2.99006410e-01
   4.12579821e-01 -7.54446203e-01  1.13423310e+00  6.34297817e-02
   2.14275845e+00  4.97310674e-01  4.14037781e-01 -1.58324898e+00
   1.14219235e+00 -9.11826249e-03 -1.47011494e+00  3.78299788e-01
   2.19896182e-01  7.91045604e-02 -4.08079411e-01 -5.36551004e-02
   2.23342508e+00 -1.90819381e+00 -4.59104756e-01  5.49457815e-01
   1.69812294e+00  5.03368834e-01  8.12401426e-01 -8.97923783e-02
  -2.17922300e-01 -2.42504064e-01  1.04724857e+00 -9.07278813e-01
  -2.44672370e-01 -1.17996248e+00 -3.95147513e-01 -6.94938047e-01
  -1.80634034e+00  1.35031441e+00  2.87988755e-01  1.74451715e+00
  -2.73251938e-01 -1.39773790e+00  3.49215889e-01 -2.68777864e-01
   5.67407755e-01 -1.45852535e+00  3.84130885e-01 -7.08886925e-01
   1.40648829e+00  5.52607726e-01  6.47585354e-01  6.13777559e-01
  -7.10525661e-01 -2.32733576e-01 -2.25287438e+00 -2.19890673e-01
   2.87252860e+00 -6.24403922e-01 -1.26527167e+00 -8.56173527e-01
   1.02526920e-01  6.43761795e-01 -2.20430155e-01  1.55845330e+00
   1.61724773e-01 -7.44856657e-01 -1.50104426e+00  8.33849215e-01
   3.05560531e-01 -1.66601224e+00 -1.52358874e+00  2.53115378e-01
   8.68674817e-01 -7.95702995e-01  8.55398033e-01 -1.49008503e+00
   7.87366267e-01  5.36403024e-01 -3.64091531e-01 -1.00829215e+00
   4.89608704e-01  9.53676525e-01 -1.04312245e+00 -3.79378595e-01
   2.29949659e+00 -2.09165692e-01  2.13868535e+00 -8.03423709e-01
  -1.28015388e+00 -1.11458216e+00  6.74923760e-01 -4.38547203e-01
  -1.62925055e-01 -2.53165662e-02 -2.16668811e-02  4.31720405e-01
  -6.59118854e-01  7.95437825e-01 -7.85807897e-02 -4.75814721e-01
  -1.32741175e+00  1.11890134e+00  1.12738157e+00  8.38535768e-01
   5.06891827e-01 -1.20131800e-01  7.51601544e-01 -9.99734691e-01
  -7.70300257e-01  2.38433496e+00  1.52275629e+00  9.00804060e-01]
 [-8.82833938e-01  1.33684596e+00 -1.50234730e+00 -6.26738473e-01
  -1.07781417e-01 -3.55919778e-01 -5.95592043e-02 -1.52030331e+00
  -4.67499659e-02  1.31879856e+00 -1.99624510e-01 -1.39585741e-01
   1.18782801e+00  2.46134695e-01  3.74431021e-01  5.29635151e-01
   5.16567758e-01 -4.48301485e-02 -9.98914121e-01  1.73354922e+00
   3.20485881e-01  3.83600453e-01  9.76989580e-01 -1.70665194e-02
  -6.80481052e-01 -6.85021337e-01  4.90023478e-01 -8.37653771e-01
   1.99485773e-01  4.55109973e-01 -4.70478971e-01 -1.86723719e-01
   1.38248551e+00  1.08757853e-01 -2.64408165e-02  4.75830055e-01
  -4.39728405e-01  1.90298045e+00 -1.34180576e+00  7.41304601e-01
   1.32166335e-01  1.17743161e-01 -1.86972119e+00 -8.09853892e-01
   6.15728135e-01 -3.05115223e-01 -1.09350173e+00 -1.38120415e+00
   3.51087646e-01  4.65547571e-02  5.52839085e-01 -1.82500852e-02
   2.35570024e-01  4.68933558e-01 -1.24119825e+00  8.40060271e-01
  -6.53706032e-01 -2.78210647e-01  1.12542359e+00  1.17152890e+00
   1.03411626e-01 -1.82323043e+00  6.59273568e-01  3.40888750e-01
   1.05819711e+00  1.17034597e+00 -1.44143727e+00  1.94143513e+00
   9.40512786e-01 -2.04835651e+00 -6.48063527e-01 -2.19951271e-01
  -7.96830445e-01 -8.07776118e-01  1.63731759e-01 -1.07510051e+00
  -1.95422957e-01 -2.54190452e-01 -9.07255399e-01 -4.06661198e-01
   2.99780854e+00  7.41799202e-01 -8.86890926e-01  2.00822890e+00
   1.48854660e+00 -7.06226887e-01 -5.13351967e-01 -1.35491700e+00
   7.90120514e-01  1.12707505e+00  4.56865965e-01  6.08233191e-01
   1.00611126e-02 -1.41801781e-01 -1.73858804e-01 -1.51739914e+00
   5.68498184e-01 -3.83782362e-01 -2.30814452e-01  9.83407957e-01
   1.48193114e+00  7.16481288e-02  1.57219255e+00 -1.05309387e-02
  -2.11687659e-01 -4.55902099e-01  7.04197581e-01  9.26625247e-01
  -8.05540185e-01 -9.17017901e-01  1.85281699e-02 -2.62230028e-01
  -7.58867171e-01 -2.56892180e+00 -1.26149499e+00  6.27170229e-01
  -1.44906987e+00 -6.17184740e-01  1.23629856e+00  1.94799364e-01
   5.15127626e-01 -7.29891959e-01 -1.47318775e+00 -1.63466794e-01
  -1.46567242e+00 -4.00519074e-01 -8.74354823e-01  9.28667956e-01
  -3.23510520e-01 -7.04594068e-01 -5.54567455e-01 -3.53322346e-01
  -5.61954426e-01 -1.28060587e+00  5.54127223e-01 -2.00910389e+00
   1.31334533e+00 -3.87091578e-01 -2.29881996e-01 -1.20667314e+00
   6.03301750e-01  1.32157287e+00 -1.78737330e+00  1.75470497e-01
   4.93741840e-01 -1.81675845e+00 -8.63981778e-01  4.40347104e-01
  -3.90994152e-01 -4.50783898e-02  4.90726698e-01  3.67594524e-01
  -1.60127593e+00  7.97595746e-01  1.94980834e+00 -1.75203386e+00
  -1.30035843e+00  3.05800742e-02 -8.08392584e-01  1.78677173e+00
   1.35472838e+00 -7.45670931e-01  1.84681096e+00 -6.50994488e-01
  -2.45084249e+00  1.08380438e+00  2.33884290e-01 -4.53123916e-01
  -1.32891791e+00 -4.42665487e-01 -4.47344393e-01 -1.60503124e+00
   8.10016474e-01  4.71168359e-01 -4.59781149e-01  1.20192218e+00
  -1.27539966e+00 -1.03847643e+00 -2.52580133e-01 -2.12276122e-01
  -8.61308520e-01  1.20830102e+00  2.42689209e-01  4.88311764e-01
   1.36521366e+00 -2.15446780e-01  7.99899094e-01  1.64848985e+00
  -9.43589659e-01 -1.54687097e+00 -6.82814921e-01 -5.86345436e-01
  -8.06143112e-01  5.32703170e-01 -3.99646142e-01 -3.83703458e-01
   3.45606272e-01 -3.22609993e-01 -5.04973711e-01 -9.01352635e-01]
 [ 1.32223616e+00 -8.73082089e-01 -8.83511641e-02  6.40517840e-01
   1.03218010e+00  1.26058653e+00 -1.20182949e+00 -7.33122060e-01
  -5.96057083e-01  8.90705243e-01 -1.13819221e+00 -8.23519910e-02
  -2.35269262e+00  1.64630087e-01 -3.37992724e-01  7.70335748e-01
  -6.75370495e-01  3.99074448e-01 -1.54089834e-01 -8.46610641e-02
  -4.01644051e-03 -6.50119643e-01  9.79568749e-02  1.61158664e-01
   5.37963425e-01  1.70215220e-01 -1.06918221e+00  5.44109993e-01
  -4.17347598e-02  3.03245432e-01 -4.86443487e-01  3.80920441e-01
   1.04941880e+00 -1.95593459e-01 -9.93080203e-03  1.76892851e-01
  -5.30845957e-02 -3.46461341e-01 -9.14112170e-01  6.35157354e-01
   1.41703354e-01  1.32774780e+00  5.00698592e-01 -2.38547259e-01
   1.13617017e+00  7.31169091e-01  5.75707578e-01  7.22852237e-01
   1.77458854e+00 -2.00441432e-01 -6.60776175e-01 -2.64774403e-01
   4.54292445e-01 -4.12270587e-01  1.21626193e+00  1.70495829e+00
   1.02411108e+00  2.43246500e-01 -1.52734442e-01  2.74396625e+00
   1.46403950e+00 -7.72044272e-01  5.63511576e-01 -1.17651194e-02
  -1.25517934e+00  6.39925764e-01  7.22382351e-01  5.69865611e-01
  -2.02817906e+00  9.22841716e-01 -1.59102277e+00  2.78313187e-01
   6.81661277e-01  8.85029766e-01  6.19242582e-01 -1.75372789e-01
   5.26148119e-01 -1.49278361e+00 -1.40132083e+00  2.13820977e-01
   1.41924554e+00  2.75316348e-01 -8.12116885e-03  2.36256169e-01
  -2.01195747e-01  7.88273972e-01 -1.18203300e-01  7.36179954e-02
  -2.12480690e-01 -7.88674827e-01  3.48493879e-01 -1.88182525e+00
   4.03235834e-01  4.92465832e-01  1.46094363e-01  8.49018885e-01
   1.18973503e+00 -4.08107999e-01  6.95146355e-01 -6.63178801e-02
   1.28257598e+00  8.91912455e-01  1.55802297e-02  1.28064419e-01
   2.27747408e-01  3.59835471e-01 -7.44660964e-01  2.12830430e+00
  -3.72724189e-01 -7.69870925e-01  3.17119817e-01  1.26498121e+00
   1.29121704e-01 -5.53521560e-01  1.51628683e+00  7.36572680e-02
   8.14976578e-01  1.15898525e+00 -1.97905291e+00 -6.90833893e-01
  -1.62739181e+00  4.88703250e-01  7.17421011e-01 -1.09993701e+00
  -1.17841538e-01 -8.49641913e-01  2.46281232e-01 -1.19606007e+00
  -1.65531357e+00 -5.73003677e-01 -5.05681036e-01 -9.80309808e-01
   3.26581628e-01 -9.85529228e-03 -6.55181513e-02 -2.23215455e-01
  -1.23880296e+00  7.11724783e-01  5.52107535e-01 -1.07318256e+00
  -1.03803214e+00 -3.70087708e-01 -1.20159277e+00  2.00137443e+00
  -1.09240842e+00 -1.00633970e+00  1.72706717e+00 -2.14804632e-01
  -1.40236114e+00  1.21796693e+00  6.57012128e-03  7.22779960e-01
  -4.80762521e-01 -2.99113288e-01  2.25061723e+00  3.29853959e-01
  -4.88147962e-01  3.09226988e-01  1.26744568e-01  2.36964137e-01
   2.45984863e+00 -1.68013471e+00 -2.00152070e-01  1.18084025e+00
   1.22808043e+00  8.12827670e-02  3.17482420e+00  1.67659319e+00
   3.02251115e+00  1.38464387e+00 -5.82400759e-01 -7.29988548e-02
   2.24437040e+00 -1.85835721e+00 -1.11613347e+00 -1.28368677e-02
  -8.74445918e-01  1.12241305e-01  3.43058947e-01 -2.97502452e-01
   1.41865134e+00  1.12537601e+00 -6.59389207e-01 -1.05260832e+00
  -1.06022182e-01  4.53412735e-01 -1.79023512e+00 -6.68436563e-01
  -1.05581403e-01  2.33691866e-01 -1.44959610e-01 -2.99232155e-01
   2.59291809e-02  1.19527573e-01  1.02865999e+00  5.55437128e-01
  -2.80844963e-02 -1.07327277e+00 -1.67113972e-01  3.59485033e-01]]");

                        #endregion
                        #region unnormed

                        numpyDumps.Add(@"[[-6.08657459e-01  2.84630414e-01  6.99639616e-01 -1.06561160e+00
   1.34186005e+00  2.59286917e-01 -3.23266791e-01 -6.29480663e-01
  -1.10812959e+00 -5.07318031e-02 -8.81265834e-01  2.03680150e+00
   1.92739359e-01  2.15375173e+00  2.18803985e-01  3.58489132e-01
   2.64326485e-01  1.16041762e+00  4.03594876e-01  2.52459621e+00
   4.28312475e-01  1.50026325e-01 -7.77603423e-02  3.26235839e-01
  -2.38119793e-01  8.41412037e-01 -1.82373910e-01 -2.14709553e-01
  -1.68825831e-01  1.05662368e+00 -6.46705735e-01 -9.41663288e-01
   1.00823654e+00  1.86491530e+00  1.94049988e+00  4.09118334e-01
   5.63122534e-01 -4.50625388e-01 -7.52792464e-02 -7.83609166e-01
   8.61290429e-01  5.23885806e-01  1.60873664e+00  1.00060134e+00
   1.80027702e-01  1.34834989e-01 -1.11647270e+00 -1.36325506e+00
  -1.10047869e+00 -4.37332861e-01  2.62329224e-01  5.58637214e-01
   2.39945750e+00 -1.35633189e+00 -9.54338272e-01  4.90338853e-01
   1.04255681e+00 -4.36128010e-01  1.55677684e+00  1.62960354e+00
   9.86311083e-01  1.15337804e+00  7.08248886e-01  3.72421285e-01
  -1.10134957e+00  1.28137560e+00  2.01672311e-01  1.70598720e+00
  -3.11337653e-01  1.35890514e+00  3.30362727e-01 -1.48426965e+00
   3.36446606e-01 -6.57837494e-01  1.01513347e+00 -1.23689705e+00
   4.38186820e-01  1.02555488e+00 -1.27588238e-01 -9.14187473e-01
   5.90131085e-01 -2.46090638e-02  9.14543095e-01  2.79255037e+00
  -1.88849826e-01 -3.44922008e-01 -7.80285303e-01  1.53982523e+00
   1.36144439e+00 -1.00981067e+00 -1.18160604e+00  7.87619932e-01
  -7.36640476e-01  1.56452551e+00 -2.29494130e+00 -1.17859009e+00
   3.86038792e-01  6.64960537e-01 -1.46300716e+00 -1.67502948e-01
  -1.32955213e+00 -9.62251817e-01 -1.52625369e+00  1.81450834e+00
  -2.82271010e-01  4.64455162e-01  3.77054311e-01  2.27937698e-01
  -8.79971285e-01  1.89349901e+00 -1.76977577e+00  9.30227984e-01
  -5.14713152e-01  8.86533243e-01  3.49564483e-02 -5.93027690e-01
   9.96019308e-01 -1.79436727e-01  4.51198452e-01 -1.58045401e-01
  -2.52645697e-01  6.23414228e-01  6.11760590e-01 -4.17921860e-01
   1.55875531e+00  8.68795403e-01 -1.00492319e+00 -1.47455066e+00
   1.27745559e+00 -2.21473072e-01  9.98352070e-01 -1.97115308e+00
  -9.57407858e-01 -9.37759328e-02 -1.69067702e+00  9.34146717e-01
   2.23288612e-01  5.79104416e-02  5.16741392e-01  3.71692386e-01
  -1.65586835e-01 -2.93184198e+00 -1.46997170e-01 -7.82613135e-01
   9.91277512e-01  6.60901708e-01 -7.20978362e-01 -7.21687187e-01
  -8.70651202e-01  4.96803512e-01  5.39399431e-02 -6.03209131e-01
   1.45135399e+00 -1.64874955e+00  1.99123672e+00  1.01123228e+00
   7.94693726e-01 -6.75927324e-01  2.07128471e-01  1.23316331e+00
   1.74589501e+00  1.34473716e+00  8.54894015e-01  7.59154611e-01
   2.88166048e-01  7.31450056e-01 -2.18822187e-01 -1.32469173e+00
  -4.37015557e-01 -1.96912124e-01  1.13889753e+00  3.61561840e-01
   1.22048757e+00 -2.58857410e+00 -3.41023953e-01 -1.17398629e+00
  -1.05548550e+00 -8.32154162e-01  9.28416399e-01 -2.01582346e-01
  -7.10513252e-01 -6.76352700e-03  7.25992746e-01  3.90556189e-01
   6.95400443e-01 -1.73177020e+00  6.41771066e-01 -6.48963518e-01
   4.82755334e-01  2.84543491e+00 -8.04634255e-01  1.52705589e-01
  -7.29453284e-01  1.57971374e+00 -2.16198092e-01  1.66070630e+00
  -1.82838740e+00 -1.31759604e+00 -1.02440652e+00  4.69528709e-01]
 [ 4.87936822e-01 -1.32583206e+00  4.93633053e-01 -1.56018353e-01
  -1.26876844e+00  9.18094669e-01 -1.84501388e+00 -1.52337178e+00
   9.15121935e-02 -4.58434356e-01 -8.78794094e-01 -4.14317912e-01
  -4.92875891e-01 -9.45974862e-01  7.14233403e-01  9.22762938e-02
  -1.67100797e+00  4.64307072e-01  1.18054598e+00 -4.90612445e-01
   2.43134126e-01  4.25029324e-01 -1.46076565e+00  5.23534578e-01
  -7.26408066e-02  2.67904668e-01 -1.40229762e+00 -8.68301236e-01
  -6.55691154e-01 -5.12001092e-01  4.36628097e-01 -1.01680406e-01
  -1.70914551e+00  8.81432888e-01  1.72268500e+00  1.02514988e+00
   2.81881528e-01  1.07388237e+00  3.95012340e-01  9.30203038e-02
   4.17576601e-01 -1.37155483e+00 -5.64354455e-01 -3.54995145e-01
  -1.53341118e+00 -3.53824318e-02  1.65061254e+00  1.14472459e+00
   1.13582509e+00  5.05542758e-02 -2.04824999e+00  5.06893157e-01
   1.09313379e-01  1.30609036e+00  2.44624212e+00 -2.26845304e-01
  -1.30624850e+00  6.25575179e-01  1.77718579e+00 -7.31169137e-01
   7.52438465e-01 -6.45509624e-01 -2.66542551e-01 -5.32083348e-01
   1.21789896e+00  4.84917870e-01  6.55975289e-01 -9.75948238e-01
  -4.16549158e-01  2.30091217e+00 -1.37142322e+00 -2.58421091e-01
  -2.41117227e-01 -1.31486388e-02  3.26935755e-01 -1.44955037e-01
   4.85378803e-01  1.00837845e+00  1.40401851e-01  4.51491397e-01
   7.99971956e-01  1.22138772e+00 -4.53050087e-01 -4.75926539e-01
  -1.21819084e+00  5.46489421e-01 -6.60786078e-02 -1.93513716e+00
  -7.39604489e-01  6.63557638e-01 -4.04419332e-02  5.58254324e-01
  -6.33343604e-01 -1.36558226e+00  3.00649563e-02  9.76947181e-02
   7.49456712e-01 -7.88637948e-01 -1.24418893e+00 -6.30855870e-01
   1.16428454e-01  2.70218719e-01  2.26799060e-01  1.92881865e-01
  -7.55440627e-01 -1.03144520e-01 -1.89134564e-01  8.79007709e-02
   5.25955559e-01 -1.91777919e-01  9.84645073e-01  1.56751765e+00
  -1.66644441e+00 -3.66585272e-01  1.52093595e+00  1.28015999e-01
   1.03419327e+00  7.26946861e-01  1.88308336e-01 -2.94670639e-01
   1.92100928e+00  1.28081321e+00 -6.54478975e-01 -6.10665346e-01
  -9.60318037e-02  6.32299465e-01 -4.33457706e-01  6.51286617e-01
   1.22301953e-01  1.41344662e-01 -1.50406767e-01  1.18289579e+00
   7.39102423e-01  8.23545152e-01  7.47735854e-01 -5.24424106e-01
   7.88148483e-01  5.66454742e-01  7.17566644e-01  1.43276181e+00
  -1.62958054e+00  1.35610486e+00  3.13947370e-01 -7.66199770e-01
  -1.84030774e-01 -3.71377618e-01 -4.68078636e-01  1.10493335e+00
   1.13464293e-01 -5.71927126e-01 -7.19462839e-01  9.48132591e-01
  -7.29673704e-01  1.50902270e+00 -4.54372847e-01  8.26657598e-02
  -5.64646035e-01  9.79618472e-01  4.26908172e-01 -9.31293521e-01
  -4.44628681e-01  1.10395915e+00  1.19465299e+00  3.82463318e-01
  -1.37758748e+00  5.97500423e-02 -4.24012400e-01 -4.31479002e-01
   1.92111909e+00 -3.16519923e-01  1.81593586e+00 -1.06052529e+00
  -8.11301227e-01  7.97092091e-01  7.88550886e-01  1.95326082e+00
   2.35422530e-01  7.53532465e-01  8.25880425e-01 -2.54208176e-01
   5.77572048e-01 -1.03143462e+00  6.42219015e-01  4.65771165e-01
  -1.20536610e-02 -3.88065521e-01  9.93294552e-01 -6.00421286e-01
   8.13094492e-01 -8.81098294e-02 -7.47120248e-01 -8.93841234e-01
  -1.24906701e+00 -2.84790342e-01  5.89483629e-01  2.13616834e+00
   1.01640192e+00 -2.09762114e+00 -1.23460748e+00  1.27002006e+00]
 [-4.99692540e-01  5.75029390e-01  1.94462743e-01  6.67926773e-01
  -5.82272943e-01 -2.65640098e-01 -3.08090821e+00  1.07688886e+00
  -7.28195297e-01 -4.35237965e-01  1.50837509e+00 -1.45824401e+00
  -1.67934302e+00  5.62047399e-01 -7.28455843e-01 -1.25042051e+00
  -1.10379094e+00 -1.00014734e+00  1.31585695e+00 -1.44900984e+00
  -1.12741310e+00 -1.22866665e+00 -1.36604031e+00  6.95528125e-01
   9.18874174e-01  6.30467936e-01 -6.47970103e-01 -1.69819103e+00
   1.31848564e+00 -1.00872003e+00  7.67800068e-01  4.68280143e-02
  -4.67596640e-02 -1.33034994e+00  1.16202607e+00  6.39856067e-01
   6.90753979e-01 -9.89277246e-02  1.20842432e+00 -7.82749311e-01
   6.18579805e-01  3.27379910e-01 -4.13298338e-01 -5.40904262e-03
  -1.70422145e+00 -3.25064874e-01 -2.39943261e-01  9.62509255e-02
  -3.01524800e-01 -1.60189271e+00  8.83816661e-01  1.46567700e+00
   2.12848808e+00  2.26742018e-01 -1.44998409e-01 -5.23913292e-01
  -2.14696557e+00 -2.27113183e+00  9.04800358e-01  2.90513430e+00
  -8.47852123e-01  1.39456404e+00  9.41335091e-01 -6.16096383e-01
   1.16576467e+00  2.77004761e-01 -2.17186090e-01  2.79739009e-03
   7.98758805e-01  1.12498767e+00  1.39588892e+00  1.29339768e-01
  -5.36520911e-01  4.77395065e-01  2.27010326e-01  7.48560022e-01
   1.77746914e+00  1.55343007e+00 -1.38277131e+00 -1.57328047e+00
  -1.11064541e+00  6.31065520e-01 -1.14761681e+00  9.05651755e-01
   1.11758840e+00  1.29588139e+00  3.57944793e-01 -9.30004372e-02
   1.98671111e+00 -3.53399050e-02 -6.20334810e-02 -1.41601073e+00
   1.88669905e-01  9.05752723e-01  2.70595180e-01 -9.38749531e-01
  -3.56689403e-01  1.21155851e+00 -1.27296678e+00  7.13429077e-01
   8.09170318e-01 -1.01786031e-01  1.79105386e+00  3.20511372e-01
   8.53035327e-03 -1.38736100e+00  1.40367979e+00  3.02840186e-01
   5.88510930e-01  1.33971762e+00 -4.93709889e-01  1.54428082e-02
  -5.03491298e-01  1.62738695e+00  1.25438352e+00 -2.24824036e+00
  -8.47755233e-01  7.82340974e-01 -5.18642463e-01  5.37151585e-03
  -2.81200785e-01 -1.90718474e-01 -4.54001344e-01  1.36495039e-01
  -1.07260415e-01 -6.89301740e-01  1.73120329e+00 -2.30427088e+00
   2.53097459e-01 -1.02544406e+00  1.37769792e+00 -1.49572482e+00
   2.11044804e+00 -4.84490858e-01  1.02970628e-01  3.39708660e-03
   7.53413610e-01  2.22820426e+00 -6.52751300e-01  4.93096453e-01
  -1.32839719e-01  7.29835814e-02  2.22049787e-01 -1.22731217e+00
  -9.55912896e-01  7.25114609e-01  6.54017545e-01 -3.78300308e-01
  -2.15332531e-02 -6.34389873e-01  1.13996198e+00 -6.88900421e-01
   2.18689276e+00 -2.61843377e+00 -9.36775732e-03 -1.33370809e+00
   1.67957275e+00  6.81036979e-01  9.68287415e-01 -1.01024144e+00
  -2.64800128e+00  7.57457372e-01 -3.73539212e-01 -1.23207497e+00
  -2.41719835e-01 -1.31616040e+00  1.25606273e+00 -2.37696338e+00
   6.07825751e-01  1.19734402e+00 -7.06659160e-01 -1.25390932e+00
   9.89068224e-01 -1.24742285e+00  3.31903148e-01 -1.00063329e-01
  -6.64098857e-01  7.33642195e-01  8.31290548e-01 -3.16591409e+00
   3.92706373e-01  2.25604771e-01  1.32307275e+00  7.58258763e-01
   5.25157112e-01 -2.60744970e+00 -4.02595402e-01  1.60869791e-01
   6.90354232e-01  3.28511571e-01 -3.16387757e-01  6.26179899e-01
  -1.03219751e+00  1.40101679e+00 -3.28161422e-01  6.75680819e-01
  -1.48369634e-01  1.78879194e+00  6.85145630e-01  1.30232938e-01]]


");

                        #endregion
                        #region unnormed

                        numpyDumps.Add(@"[[-2.13627600e+00 -1.61424381e+00 -1.30521625e-01 -3.60808243e+00
  -1.52585268e+00  3.53174564e-02 -1.46745545e-01  2.85125647e-01
  -1.06504586e+00  1.05416300e+00  1.92341646e-01  1.61080222e+00
   1.43001981e+00  3.02259194e-01 -7.19754460e-01  1.54921401e+00
   2.05898944e-01 -7.49391958e-01  9.39624242e-01  2.68524547e-01
   4.20493044e-01 -3.59124838e-01  1.52195687e+00  9.92610446e-01
  -3.58064440e-01 -2.09595890e-01  5.75120614e-02  6.18960707e-01
   1.21150679e+00  5.69616508e-01  3.49882940e-01 -6.74465064e-01
  -1.18496083e+00 -1.00630931e+00 -5.11687188e-01 -1.33577522e+00
   3.62136081e-01 -1.49819127e+00  5.64672060e-01  7.25451798e-02
   3.88974428e-01  3.03858404e-01  1.23638626e+00  3.40208681e-01
   6.49946922e-01  1.71337692e+00  4.96312099e-01 -1.85365734e+00
  -2.58673873e-01 -8.01383588e-01 -1.47758689e-01 -1.38325055e+00
   1.93543707e+00  7.87015728e-01  6.54757455e-01 -8.96117152e-01
  -1.68449782e+00 -1.61515814e-01 -7.62729206e-01 -6.20594653e-01
  -1.17722820e+00 -9.67504059e-01 -2.06278502e+00 -3.13953644e-01
  -2.37303939e+00  1.86838195e+00 -7.46126435e-01  1.58618464e-01
  -1.97670568e-01 -1.02300937e+00  7.82872003e-01 -3.56442613e-02
   1.03155861e-01  8.76920091e-01  2.66338825e-01  9.98969400e-02
   4.90033835e-01 -1.29043118e+00 -1.05855890e+00 -2.43957769e-01
  -4.78831326e-01  8.57463694e-01  1.55303718e-01 -4.36948851e-01
  -1.87538177e+00  1.55703852e-01  7.48404993e-01 -5.54411734e-01
   2.07684423e-01 -1.00078450e+00 -4.44927244e-02  9.18654090e-01
  -7.65494191e-01 -1.13562424e+00 -1.05589382e+00  6.08616356e-04
  -2.98911597e-01  1.03754664e-01 -1.10626179e-01 -7.16764429e-01
   7.85117817e-01 -1.33727613e+00 -1.54866158e-01  2.63149934e-01
   1.07488467e+00 -1.26556000e+00 -6.99815792e-01 -1.22047667e+00
   4.22108668e-01  8.90292223e-01 -2.05731679e+00 -1.86909813e+00
   1.09555208e+00  1.33107161e+00 -4.41243153e-01 -1.14833683e+00
  -3.25862048e-01  2.51369500e-01 -3.00645400e+00 -5.84018020e-01
  -4.66262260e-01  2.07406358e-01  3.89282273e-01 -6.12151351e-01
   2.12484119e+00 -4.15814543e-01  9.01139533e-01  1.72476703e-01
  -7.90740683e-01  8.58957019e-01  2.36528793e-01 -3.71113092e-01
  -1.33631621e-01  8.47179736e-01  1.84589849e+00 -6.76587575e-01
   1.50301233e-01  6.24160358e-01 -3.99269575e-02  9.67925053e-01
  -7.20432587e-01 -1.88512406e+00 -2.05972012e-01  1.36078297e+00
   8.84296782e-01 -7.39723107e-01  6.73778922e-02 -5.40917208e-01
   2.23876330e+00 -3.23349863e-01 -2.06064580e-01 -1.52543923e+00
   2.51981747e-01  1.00554239e+00  7.87966502e-01 -1.68930395e+00
  -1.47086210e+00 -3.55159189e-02  2.17548981e-01 -7.86319178e-01
   1.64501664e+00 -4.14960778e-01  3.19000124e-01  5.59079916e-01
   8.91866669e-01 -2.82819644e-01 -1.50442513e+00  6.58044034e-02
   1.65916516e-02  1.45092329e-01 -7.46087224e-01  2.54101349e-01
   1.65392856e-01  2.12307545e+00  1.12237242e+00 -1.27443676e-01
  -1.09273566e+00  9.01524290e-01 -1.52022955e-02  1.55120359e+00
   2.06381375e-01  1.20021033e-01  1.25939243e+00 -5.06178293e-01
  -1.62187207e+00  1.43249364e+00  1.28738355e+00 -1.43095057e-01
  -9.50589736e-01  3.33761226e-01  2.61730909e-01  7.62221874e-01
   1.66506073e+00  2.21406655e-01  3.80778308e-01 -1.63542181e+00
   1.08869937e+00  8.18025537e-01  8.36946591e-01 -1.00604677e+00]
 [-8.70945355e-01  8.23309115e-01 -6.97308814e-02  1.11650344e+00
   1.57403507e+00  1.65005499e+00 -7.52638548e-01  2.23601803e-01
  -4.80952738e-01  1.24678705e+00  5.94459367e-01  2.20508608e-01
  -2.39437788e-01 -1.06805253e+00 -1.27185416e+00 -8.17775340e-01
   7.03219976e-01 -2.44168752e-01  3.97693164e-01  9.75268197e-01
  -1.70023970e+00 -8.55222382e-01 -1.20787857e-01 -1.87003208e+00
  -1.08940816e+00 -3.47580461e-02 -2.46759401e+00 -1.16022038e+00
  -7.20568859e-01  3.34349601e-01 -1.49343089e-01  2.60230291e-01
   9.45470972e-01 -2.77217392e-01 -8.93824629e-01 -5.27953956e-01
  -1.39577425e+00  1.28820618e+00  5.58652268e-01  3.27466336e-01
   3.10089009e-01  5.35232707e-01 -1.72477184e+00  9.81887072e-01
   2.31657707e+00  1.32609410e+00 -1.84021648e+00 -9.14743134e-01
   1.50005579e+00 -2.23982895e-01  9.95902810e-01  2.78983593e-01
  -1.47230398e+00  8.07648388e-01  1.33359893e+00 -1.75964028e-01
  -1.19121896e+00 -3.67517979e-01  1.86163227e+00  2.68709033e-01
  -9.84616350e-01 -1.08052888e+00  5.61854087e-01 -1.48282198e+00
  -5.11079463e-01  9.13809502e-01  3.25197084e-01  1.11689968e+00
   8.34465160e-01 -7.13813949e-01  1.30689898e+00 -4.84686020e-01
  -3.80650664e-01 -3.60549262e-01 -4.22771815e-01  2.04278660e+00
  -6.63192872e-01  1.02814302e+00  3.26048467e-01 -1.15630383e+00
   1.55083233e-01  3.50533194e-01 -4.60537603e-02 -1.30731063e-02
  -5.59633465e-01 -1.20386957e+00 -1.29670264e+00  2.40490810e-01
   5.65778796e-02 -1.94662312e+00 -3.93502130e-01  1.77463824e+00
  -1.24826762e-01 -1.11002104e+00  7.66183268e-01  8.33230628e-01
  -8.57279997e-01  1.18986901e+00 -1.23253927e+00  4.69790960e-01
   2.76522318e-01  6.91350972e-01 -1.37447990e+00 -3.22196589e-01
  -8.87616749e-01  1.10629523e+00 -5.82721221e-01  8.26076380e-01
   3.32207063e-01 -2.18597811e+00  1.38099014e-01 -4.35374575e-01
  -4.34008755e-01  4.91009648e-01 -2.04864984e-01  3.23989366e-02
  -2.09179352e-01  2.03952266e-01  1.10365229e+00 -1.21399462e+00
  -1.41220227e+00 -1.46080938e+00 -8.20903790e-01 -1.37349028e+00
  -1.08721248e+00  3.01435370e-02  9.99937647e-01  1.05033012e+00
   5.41114342e-01  1.80256383e+00 -5.47146808e-01 -2.17111920e-01
  -1.05655183e+00  1.97311679e+00 -4.86085564e-02 -4.03567968e-01
   1.23038088e+00  8.21742799e-01 -1.38153956e+00  1.45174698e-01
   1.19009355e+00  1.40362732e+00 -3.01096760e-01 -3.77490539e-01
  -1.07188214e+00  6.03422723e-01  9.80785376e-01 -2.33648075e+00
  -5.06822775e-01 -1.23257101e-01 -5.10591851e-02  2.66469977e+00
  -1.57807950e-01 -3.94882845e-01 -4.79105500e-01 -3.54280087e+00
  -6.87053694e-01 -3.13511150e-01  6.13367259e-01  5.68165113e-01
   4.82734988e-01 -1.11377421e+00 -9.95693521e-02 -1.27700383e+00
   4.29903514e-01  7.74326016e-01 -2.40053708e-01  2.18828426e-01
  -5.83710943e-01 -6.76431932e-01  7.21270179e-01 -5.49381900e-01
   8.05513908e-01  1.57620893e-01 -2.73361776e-01 -4.75071935e-01
  -1.29340472e-01  6.76198229e-01  1.42599856e+00  3.29568888e-02
   3.24860847e-01  2.68191887e+00  1.59277905e+00 -1.20097644e+00
   1.40065659e+00  4.86319689e-01  1.42855983e+00 -1.35773898e-01
   4.64746814e-01  3.17870520e-01 -1.09753023e+00  1.05132420e-01
   3.51930971e-01 -2.90601052e-02 -1.98142977e-01 -9.21474111e-01
   2.61130959e-01  8.40968306e-02 -4.26135074e-01 -2.25810533e-01]
 [ 5.93979776e-01 -1.59524659e+00  6.25827763e-01  1.07442988e+00
  -1.03464273e+00  1.52756699e-01  1.64385809e+00 -4.08549802e-01
  -9.03239734e-01  4.35486136e-01 -1.48767062e+00 -6.20472568e-01
  -1.25516178e-01 -8.24370591e-01 -1.24033831e+00 -1.05325948e+00
   6.26122975e-01  5.10204211e-01  6.90315929e-01  1.40743747e-01
   6.85420887e-01  4.96956281e-01  1.65010106e-01 -1.13285381e+00
   7.69488757e-01 -2.73771974e+00 -8.06473520e-03  3.32135984e-01
   1.34911314e-01 -2.96115623e-01 -8.22581247e-01  9.28174300e-01
   9.76083812e-01  3.18925783e-02 -9.11419888e-01 -2.85765912e-01
   2.38039493e+00  1.40231682e-01 -1.01457518e+00  1.69206249e+00
  -9.08593278e-01  2.03876347e-01 -5.90425061e-01  1.14616954e-01
  -1.25656618e+00  1.03203760e-01  1.57313461e+00 -1.07660489e-01
  -1.21802665e+00  1.44362491e+00 -1.39426506e+00 -1.13046796e+00
   7.32475731e-01  6.24690697e-01  2.88016077e-01  7.40513019e-01
  -1.14396176e+00  1.27919449e+00  7.99379052e-01  5.53835711e-01
   6.96656575e-01 -7.25964265e-01 -2.18969138e+00  1.85276374e+00
  -4.47179826e-01  6.39596396e-01  5.90819052e-01 -1.17412496e+00
   2.53129595e-01  1.70833021e+00  1.04513691e+00  1.31082401e+00
  -1.01374134e+00  1.15286013e+00  6.49897188e-01 -1.39554106e-01
  -1.34897075e+00 -1.16633770e+00  2.77710699e+00 -7.07379747e-01
   8.30935151e-01 -1.60747777e+00  3.80267269e-01  1.72711518e+00
   1.43946200e+00  7.66584217e-01 -1.92062419e-01 -8.02140315e-01
  -5.87859513e-01 -1.86454437e+00  5.42910127e-01  4.45561889e-01
  -7.24912709e-01 -5.10408806e-01  1.05662461e+00  7.07913024e-01
   6.58826428e-01 -7.58736792e-01 -4.58092341e+00 -3.05034444e+00
   7.32680238e-01 -2.42294967e+00 -3.22602128e-01 -5.48336453e-01
   3.37299636e+00  1.42181325e+00 -1.17779645e-01  8.85399172e-01
  -5.70811046e-01  3.12536056e-01  1.10986777e+00 -4.18564328e-01
   6.50523347e-01 -1.09266851e+00  7.12772132e-01 -1.53044503e+00
   1.37318837e+00 -5.60386019e-01 -1.25709702e+00 -1.66966745e+00
  -1.09122769e+00 -6.49911560e-01  7.59111573e-01 -4.38089635e-01
  -3.85258175e-01  5.57544364e-01  7.00375058e-01  1.80530834e+00
   3.00533266e-01  5.21055436e-01 -8.21974103e-01  2.05585322e+00
  -1.06532513e+00 -2.94431247e-01  1.25616546e+00  1.00556884e+00
   1.49828858e+00  1.83037034e+00  6.12398132e-01  6.72301325e-01
   6.26729869e-01 -2.31415672e-01 -3.19211965e-01  3.43525910e-01
  -1.12295213e+00  1.18287207e+00 -2.09891531e-01  3.79634395e-01
   8.74380342e-01 -1.07594615e-01  5.04039329e-02 -8.80445302e-01
  -1.87624367e-01 -6.80415242e-01  4.95418945e-01 -8.47974263e-01
  -8.94048484e-01 -8.23071503e-01  8.72049138e-01  8.61853576e-01
  -6.61027611e-01  5.51133119e-02 -1.46081558e-01 -1.34100878e+00
  -1.30542034e+00 -6.99477034e-01  1.69005356e+00 -5.67283483e-01
  -9.32362837e-01  9.51116700e-02  3.09722883e-01  4.26866129e-01
  -5.80590942e-01  7.08828685e-02 -1.31565347e+00  1.97113632e-01
   1.36227493e+00 -3.41854736e-01  3.68500862e-01  2.40259353e-01
   1.67688198e+00  2.03214824e+00  7.92995172e-01 -2.35900929e+00
   1.41652036e+00  8.93822637e-01 -3.80530763e-01  4.18570756e-01
   4.78903503e-01  5.22906280e-02 -6.25527284e-01 -5.08947364e-01
  -8.54487605e-02  2.06067890e+00 -2.22179913e-01  1.44524429e-01
   3.67812846e-01  6.60555075e-01 -4.08515198e-01  1.29704522e+00]]


");

                        #endregion
                        #region unnormed

                        numpyDumps.Add(@"[[-6.22262082e-01 -7.78223862e-01 -2.97750912e-02 -7.88913991e-01
   2.35676928e-02  8.63967111e-01  6.77428522e-01  7.62261103e-01
  -4.53814293e-01  1.57652915e+00 -9.26682754e-01  4.47758736e-01
  -2.19888225e-01 -3.38488747e-01 -1.77798613e+00  1.20627741e+00
   1.33881369e+00 -1.42017675e+00 -1.23186646e+00  4.72155829e-01
   5.03109221e-02 -5.50131265e-01  1.00507296e-01  9.69057297e-01
  -8.31142618e-01  7.35400858e-01  3.16068827e-01 -1.15475241e+00
   1.65618919e+00 -1.22871220e+00  8.15677923e-01  1.60338392e+00
   1.79314334e-01  6.34700601e-01  9.15535457e-01  1.05679852e+00
  -2.85648484e-01  1.99249789e+00  1.03670495e+00  1.04964680e+00
  -1.67386310e-01  3.06215045e+00 -7.96265179e-01  7.95548646e-01
   9.28737314e-01  7.77730304e-01 -4.35980837e-01  1.28347635e+00
  -4.35431344e-01 -1.48441301e-01  1.06762959e+00 -3.39631582e-01
  -3.35362792e-01  3.82751429e-01  6.58847448e-01  2.73622805e-01
   2.91210186e-01 -7.35835664e-01  1.92153407e+00  1.01282000e+00
  -2.08171388e+00 -2.25710792e+00  9.08216834e-01 -2.16850746e+00
  -4.03134799e-01  2.33921996e-02  3.77868264e-01  1.38406342e+00
  -1.92186444e+00  6.21707019e-01 -5.37384620e-01 -1.32385155e+00
   1.51551543e-01 -1.39721214e-01  2.80130686e-01  5.25817390e-01
   8.83999363e-01 -4.33513410e-01  1.44184824e+00 -9.02602689e-01
   2.87898723e-01 -2.88265933e-02  7.59204178e-01 -1.04636204e+00
  -1.93258291e+00  7.40863320e-01 -1.68165455e+00  6.02049762e-01
   1.23651867e+00  1.08857748e+00  2.94759905e-02 -8.23969634e-03
  -2.33692016e-01  1.05402130e+00 -2.19115320e-01  1.36768423e+00
  -4.04950768e-02  9.25541881e-01  5.90427245e-01  2.25459824e-01
  -8.46509043e-01  8.20690015e-01  1.08212334e+00 -6.60354470e-01
   7.65561159e-01  2.44401884e-01  1.15565767e+00  9.95783359e-01
  -9.36342167e-01  1.37120741e+00  4.87038650e-01 -6.63010134e-01
   1.77770635e+00  7.33927016e-01  1.25644125e-01 -1.17847132e+00
  -1.31545936e-01 -7.24582335e-01  1.30982982e+00 -1.02460976e+00
   8.21041612e-01  1.47085154e+00  1.96360531e-01  8.41888766e-01
   2.48806952e+00 -1.62473484e-02 -9.29339485e-01 -1.16522862e-01
   1.54341813e-01  2.00990828e-02 -1.52333274e-01 -5.04514811e-01
   8.47037439e-01 -1.04816421e-01  9.51189890e-01 -9.08730581e-01
   1.61901062e+00  9.48736706e-01 -5.19100148e-01  4.52525718e-01
   7.79130916e-01 -1.81555579e+00  6.26683665e-01 -5.93945849e-01
  -1.64753846e-01 -8.02458230e-01 -4.25491016e-01  1.92182739e-01
  -7.55874830e-01 -2.04742487e-01  1.58167349e+00  3.90114085e-01
  -7.24676974e-01 -1.11951929e-01  1.54193489e+00  1.09661974e+00
  -2.22457834e-01  9.87532772e-01  9.76340061e-01  1.05801449e+00
  -6.17012306e-01 -1.87581769e+00  3.32134342e-01 -1.72020599e+00
  -6.26526692e-01  5.06042988e-01 -1.44847135e+00  1.92637143e+00
   1.38624709e-02  1.41218771e-01  1.18110874e+00 -1.62660871e+00
   1.26102452e+00 -5.17585930e-01 -2.43242346e-02  1.27903412e-01
  -1.01486743e-02 -1.83599442e+00  1.10002895e+00 -9.90316613e-01
   2.01979835e+00 -4.89555740e-01 -1.59600904e-01 -1.54740813e+00
  -9.78614786e-01  4.16461278e-01  4.63505111e-02 -1.16010831e+00
   1.11280212e+00 -4.91415640e-01  1.55414125e+00 -1.72027314e+00
  -2.38340236e-01  6.19240842e-01  9.55904116e-01 -4.71460432e-01
   1.35957881e+00 -9.95297875e-01  1.12345988e+00 -6.54060143e-02]
 [-1.83099455e+00  3.28534521e-01  1.09175177e+00 -5.25123138e-01
  -9.52841034e-02 -1.11201799e+00  3.37397845e-01  1.72161992e-01
   2.36950503e-01 -2.79794975e-01 -4.58380434e-01  8.08195124e-01
  -1.24852684e+00  7.72684374e-01 -1.72071304e-01 -1.33725366e-01
  -5.44508702e-01  2.12067938e+00 -1.76018824e+00  4.49025547e-01
  -8.73472672e-01 -1.04878756e-01  2.81864767e-02  1.43745980e+00
  -1.35458139e+00  7.44501388e-01 -7.74981012e-01  2.56433535e+00
  -8.91974786e-01 -7.09299960e-01  1.04507163e+00 -5.24873990e-01
  -2.56594418e-01 -5.21502421e-01  2.23813831e-01  9.02411306e-01
   7.45378698e-01 -1.04255198e+00  8.47452842e-01  1.10102931e+00
   4.20314389e-01  5.73449851e-01  7.47062926e-01  3.08843850e-02
  -6.56704758e-01  4.86720110e-01 -1.36185523e+00 -1.43331121e+00
   8.54645275e-01  1.64502148e+00  4.87955542e-01  1.62764966e-01
  -1.62684059e+00 -1.36078164e+00 -8.22778800e-01  2.02333565e+00
   1.24575669e+00  2.01848097e-01  7.22430394e-01  1.74791472e-01
   3.38638655e-02  3.23286718e-01  5.34173691e-01 -3.10659222e-01
  -1.62106216e+00 -3.73845375e-01  4.08759759e-01  5.01215277e-02
  -1.82296559e-02  9.45625467e-01  1.02425332e+00 -3.68392674e-01
  -6.30466888e-01 -1.47969958e-01  7.30746527e-01 -5.04149275e-01
   1.32813666e+00 -1.21478991e+00  1.12934308e-01  2.98601411e-01
   1.32103442e+00 -1.56069977e-01  9.91835696e-01 -6.70295000e-01
   8.66491690e-01  4.55378124e-02 -1.26148812e+00  1.04975687e+00
   9.45993441e-01  7.32165356e-01  1.39099082e+00 -1.92876017e-01
  -8.89229415e-01 -1.47714032e+00 -1.51940815e+00  4.60885868e-01
   8.41693946e-01 -2.40121402e-01 -5.57184240e-01  4.36577007e-01
   6.92602913e-01  4.77442183e-01  5.12146913e-01  7.76529070e-01
  -6.51751591e-01 -3.02423733e-01 -5.30147945e-01  1.51832345e+00
  -1.19486223e+00 -8.84540989e-01  2.61116342e-01 -1.17883728e+00
   8.18748195e-01 -9.81110351e-01  1.31859557e+00  1.08146712e+00
  -9.74970813e-01  6.23982634e-02  2.41620820e+00 -1.64203358e-01
  -8.91482040e-02 -4.80189174e-01  1.29534726e+00 -5.92523681e-01
  -2.00029684e+00  7.64084149e-01  8.45461141e-02  1.28272858e+00
   1.16935326e+00 -1.33948932e+00  7.71287077e-01  7.81713053e-01
  -8.40337409e-01  1.11452414e+00 -5.41323593e-01  1.01286497e+00
   4.28378884e-01  2.17079986e-01 -1.13991041e+00  7.13624662e-01
  -7.09370829e-01 -1.01915523e+00 -4.53134343e-01  4.39177882e-01
   1.45411802e+00  3.94765658e-01 -8.45043433e-01 -2.99688019e-01
   1.49725502e+00  4.77253471e-01 -2.60858582e-01 -1.03734931e+00
   1.05724167e-01 -6.28048470e-01 -5.13356302e-01  4.02883796e-01
  -2.14903014e+00  6.85216128e-01 -2.46136967e+00 -6.90349101e-01
   1.12466992e+00 -1.24573097e+00 -3.27800238e+00 -4.31496308e-02
   3.16040641e-01 -1.20009176e+00 -1.74861606e+00  1.17535465e+00
   3.28577992e-01  4.01275374e-01  1.74606800e-01  6.07018319e-02
  -1.18161246e+00 -1.78757122e-01  2.07104535e-01  1.22576805e+00
   1.54109605e+00  1.61281937e-01 -2.14309854e-01  1.73832416e+00
   1.00739655e+00 -2.03885775e+00 -3.79372711e+00  6.47177050e-01
   9.28732461e-01 -5.98569855e-01  1.47329913e+00 -8.14458427e-01
  -9.97868739e-01 -2.66264699e-02  8.10529832e-02  2.13202725e-01
  -4.86147446e-01 -1.66997844e+00 -1.02666810e+00 -1.49776440e+00
   1.22639857e-01  1.72218867e+00 -8.80801903e-01  4.18627331e-01]
 [ 4.27500090e-01 -1.22956602e+00 -2.68222412e-01  5.86851152e-01
   4.83678926e-01 -1.85524073e+00 -6.97774434e-01 -1.27912743e+00
  -2.23881004e-01 -5.61557033e-01 -2.18395409e+00 -3.98374789e-01
   2.31799412e+00 -1.52139480e-01 -1.67250510e+00  8.03581909e-01
  -1.37022626e-01 -1.46892420e+00 -1.88513767e-01 -1.10432098e+00
  -8.14134681e-01  1.70596990e+00  1.18949689e+00  1.01000962e+00
   1.05157329e+00 -3.77275800e-01  8.13558712e-01 -8.57640045e-01
   2.72054949e+00 -2.02148731e+00 -9.34860174e-01 -6.85952380e-01
   1.20901801e-01  1.56721143e-01  2.43654705e-01  2.70903053e-01
  -9.51300001e-01 -2.22780191e+00  6.66278574e-01 -8.27588184e-02
  -4.41361938e-01  1.07794316e-02  6.47074304e-01  7.13469646e-01
  -9.17611408e-01  9.50385652e-01  8.93693812e-01  5.24787242e-01
   4.78478610e-01  3.09760582e-01 -3.82480010e-01  4.26294883e-01
  -2.92938254e-01 -6.68329443e-01 -1.55112462e+00 -3.35604642e-01
  -1.15326657e+00  1.44339855e+00  1.22564496e+00  1.95429220e+00
  -7.57373073e-02  1.12042238e+00  9.33853870e-01 -5.21235021e-01
  -4.90743350e-01 -6.63271409e-01  2.22698352e-01 -1.14304758e+00
   1.47888592e+00 -1.07321489e+00 -1.76309678e-01  9.41256682e-01
  -4.03513639e-01 -1.29758154e+00  3.82417645e-01 -2.67614539e+00
   1.86530040e-01 -3.24461732e-02  3.61684590e-02  7.58979399e-01
  -7.35836019e-01  1.71837813e+00  1.03949898e+00 -2.85879094e-03
   6.64307055e-01 -7.78556250e-02 -1.96825277e+00 -4.76260429e-02
   9.00916351e-01 -7.50334822e-01 -1.47072074e-01  5.37561182e-01
  -1.00091573e+00 -1.29371598e+00 -1.63890113e-01  5.12178377e-02
   9.85936996e-01 -3.59268557e-01 -1.33395801e+00 -5.71405118e-01
  -5.71844922e-01 -2.51736339e+00  2.34271212e+00 -9.96515537e-01
   8.29643309e-02 -2.14967852e-01 -8.35886022e-01  7.38693939e-01
  -9.56652144e-01  4.72043494e-01 -3.05157212e-01  8.42314559e-01
   7.49541002e-01  3.57575603e-02 -2.20276517e+00 -9.91631545e-02
  -7.54271964e-01 -1.10864890e-01  1.95119390e+00 -9.29753005e-01
  -2.32325824e-01  6.38557402e-01 -9.24613488e-01 -1.91773779e-01
   7.32970071e-01 -1.73055668e+00  9.18232423e-01 -1.45801454e+00
  -6.36227320e-02  8.97077443e-01 -2.15681270e-01 -1.43912302e-01
  -2.85983520e+00  3.44643106e-01  1.22737920e+00  2.10013619e+00
  -3.13365844e-01  1.23307543e+00  8.14435523e-01  1.66344305e-01
  -8.27385835e-02  1.45660342e+00  2.06674460e+00  3.33844753e+00
   1.27071250e+00 -6.35156572e-01  2.13924839e-01  1.08871446e+00
  -9.87950701e-01 -1.06236575e+00 -1.36401469e+00  1.31549150e+00
  -3.12985833e-01  4.91445358e-01 -4.59762132e-01  1.09897114e+00
   1.87568771e-01 -2.32416727e-02  5.42019055e-01 -2.05731433e-01
  -1.48356527e-02 -3.80772179e-01  3.33331793e-01 -4.83123209e-01
  -6.92583637e-01 -1.47075391e+00  4.27752101e-01  2.01382170e+00
   1.18849819e-01 -2.01240814e+00  1.99491413e-01  1.13937306e+00
  -1.46194873e+00 -3.82555859e-01 -1.08895253e-01  9.55754505e-03
   6.14123440e-01 -1.49534789e-01 -5.34243122e-02 -5.26445191e-01
   7.93179432e-01 -2.66430659e+00 -1.16396463e+00  1.05520622e+00
   9.79705196e-01  3.13080951e-01 -4.00993291e-01  2.40478784e+00
   3.39432218e-01 -1.28518602e+00 -2.81363145e-02 -2.32959937e+00
  -8.39443840e-02  5.39768148e-01 -3.31110593e-01  6.63217850e-02
   9.91984172e-01  1.53843986e+00 -2.58966125e+00 -6.48129443e-01]]


");

                        #endregion
                        #region unnormed

                        numpyDumps.Add(@"[[ 0.13811015  0.81463507 -1.54362617 -0.2336423  -1.73107298  0.4640255
  -1.42779908  0.36269758 -2.22416385  1.23530241 -0.14057952 -0.85371602
   0.91798988 -0.44490675  0.019993   -0.06094102  1.2174325   1.09546823
   0.1458867   0.6419085   1.36922989  0.54099467  1.13276871 -0.18135741
   0.43941684 -0.05770019  0.68602497  1.1618707   0.93101281 -0.1358612
   0.99874415 -0.44389321 -1.99795836 -0.47366501 -0.89421716 -1.51181677
   0.36874762  0.96170738 -0.20411512 -0.13207647 -0.79709214 -1.51243481
  -1.93886232 -0.23308617  0.8199445  -0.31917654 -0.96999526  0.5566197
   0.52821647 -1.19283566  0.11301735 -1.06406343  0.89430973 -0.64155809
   1.27272425  0.26214579 -1.83813426 -0.09423244 -1.14575421 -0.55872926
  -2.01211674  1.19692873  0.06874471  0.40983186 -0.642641   -0.09708497
  -0.90760728 -1.88592229 -0.21165288 -2.10846832 -0.84182316 -2.00177733
  -0.84792501 -0.73793078 -0.32548811  0.56243955  1.30959693  1.13516466
  -0.25672237 -1.29052797 -0.24271092 -0.05585152 -0.73210865  0.60905689
   0.3011629   0.08099822  1.44228441 -0.22831517 -0.76509752  0.88139121
  -1.09739554  2.21859792  1.26442937  1.32364393  0.95761094 -1.55922806
  -1.8934744   1.58931516 -0.42512367  3.05715085 -0.57287004 -0.65454108
   1.56587849 -1.12800405  1.59498653 -0.62179388 -0.72627853  0.04791623
   0.11877069 -0.34478552  0.18064942  1.28065447 -0.67033334 -0.92683083
   0.43432485 -0.55524012 -0.12917676  0.93981188  0.21970104 -0.75189739
   0.64487201 -0.52670918  1.67000118  1.88222188  0.45641299 -0.01924171
  -1.02619477 -1.20607147 -0.65776282 -0.75945049 -1.36438737 -0.95494419
  -1.05667507 -1.04185047 -0.05658817 -1.01195859  0.67969543  0.73842485
  -0.80680186 -0.08156182 -0.75866434 -0.31012337  0.54218203 -0.51796884
  -0.72795396 -1.89095311  0.95888475  0.34581912 -0.60058426 -1.17651234
  -0.69462699  1.4353172  -3.30167482 -1.66334207 -1.65607323 -1.74035504
  -2.29630387 -0.10792409  3.64277473  0.54062446  0.18915663  0.65247511
  -0.26229685  0.19742947 -0.31989811 -0.80548558 -0.90322086  0.31924735
   0.83281737  0.01344876  0.44646748 -0.08797842 -2.74836985 -0.68926748
  -0.89279868 -0.009722   -1.92668347 -1.15914885  1.07652355 -1.04183948
  -0.79460623  0.94858413 -0.87068836  0.09569474 -0.52510928 -0.66809641
  -0.28810157 -1.26671293  0.16986945  0.86471479  0.84049593 -0.35886759
   0.17777922  0.07593982 -0.18997717  3.24316502 -0.10280187 -0.55092242
  -1.4347852   1.24979066]
 [ 0.62898253  0.11752109 -1.2228645   0.06631532 -0.5776459  -0.42928743
  -0.83837188  1.17167078  0.93445516  2.61491089 -1.22744167  1.78986619
   0.06762283 -2.35586085  0.72289177  1.16917466 -0.08352608 -0.70674731
  -0.91235165 -0.23962842 -0.26407382  1.05203742 -0.57816148 -0.33404401
   1.11764322 -0.47664128  1.31567231 -1.28593504  0.66409199 -0.99108149
  -0.29772118 -2.45137761  0.59721411  1.06947373 -0.28914201  0.76973742
   2.08727387  0.07129608  1.18843079  0.87822421  1.27621397  0.33480133
  -0.62703062 -1.8066236  -0.66588483  1.10217115  1.15472496  1.02713973
   0.11288362 -0.30435449 -0.60396537 -0.49359902 -0.32536755  0.58683182
   0.87236519 -0.2758005  -0.37087483  0.66826133  1.86619705 -0.88585565
  -0.6369997   0.54296189  0.52430126  0.50858275 -1.20798915  0.22731249
  -0.40884574  0.93353864 -0.45175908  0.35787689  1.60683411 -0.01382451
  -1.17844722  0.2318621  -1.47355679 -0.48568109 -2.04317647 -0.52907951
  -0.02719491  1.11183527  0.90381374 -0.38120611 -0.11098976 -0.37753337
  -0.49118341  1.0394051   0.75196927  1.64490983  0.28733359  0.75188104
   0.86991669  0.20767245 -0.29729727  0.16147506 -1.42406989  1.71649957
  -0.40628241  0.00535121 -0.3090207  -0.61220187 -1.04406514 -0.05890185
  -1.14340467  0.3949087  -0.01994248 -0.27226207  0.0229188   0.43677988
   1.90035365 -0.40889756  0.08276983  2.70649633 -0.42362647 -1.53118368
  -1.22513549 -0.47249204 -1.49924131  1.80010622 -0.17867882  0.75024644
  -0.50176867 -1.09697908  0.19635084  1.31042935 -0.90228121 -0.31034576
   1.91152349  1.08073624  0.32364188 -2.46239086  0.81478636  0.19463123
   0.89098399 -0.83435807  0.99912508  0.36399663  0.56763526  0.23530867
   0.26537545  1.22983649 -0.12556476  1.06062838 -1.33096484 -1.58764966
  -1.24012282  1.439562   -0.71775112 -0.74092927  1.15414309  0.33376269
  -0.19439881  1.67778575  0.32415318  0.10008446  1.508685   -0.55231427
   1.20537236  0.50921295 -0.23008179 -0.21601846  0.04727689 -0.01138877
  -0.54223678  0.68869439 -1.63750634 -0.5130159   1.0463435  -0.19640189
  -0.58731014 -0.76978038 -0.32163674  0.44757464 -0.71426835 -0.51529407
   0.03248734 -0.27925503  1.15754707  1.07215155  0.52355712  0.31115254
  -0.35464225  0.35640208 -1.01351437  0.77238653 -0.56922973  0.32624269
   0.66655247 -1.31837597  0.64927458  0.76971457 -3.55279681  0.37082326
  -1.45653526 -2.75678418 -0.84614134  0.34025806 -0.46701003  0.48713826
   1.30211981 -0.59616249]
 [-0.98265762 -0.09869078 -1.71272569  0.89514264  1.29441515  0.24604398
  -0.33076039  0.48253499  0.34081224  1.15444141 -1.19198619  0.5142298
  -0.6255933   0.22658346 -0.62186514 -0.37089072  1.91180626 -0.99174843
  -0.85021567 -0.56688601 -0.52081665 -0.77335215  1.15764271 -0.65306851
  -1.14067512 -0.66979678  0.08244715 -0.24347319  0.53336588 -0.24801727
  -1.5795624   0.01469841  0.65359854  0.43871783 -0.10681485 -0.08935372
  -0.43250811  0.67895288 -0.03677485  0.03898957  0.27596609 -1.70482292
   0.42633851 -0.30681571  2.01716058 -1.04029469 -1.18589535  0.73316817
   0.53320275  0.83007047 -0.63368877  1.49227196 -1.16075643  1.0893175
   0.93206311  0.72746092 -0.58525284 -2.33523222 -0.27865909  1.59497576
   2.14435972 -0.71110048 -1.33904634 -0.79409201 -1.43694608 -0.98795565
  -0.09033931 -1.04710685 -2.35027656  1.40742546 -1.71590839  1.0015744
   1.86661028  2.55176691 -0.23524787 -2.36428449 -0.18532978 -0.25012055
  -0.20052763 -0.6248031  -1.0625837   0.99115435  1.59971265  1.01809936
   1.39547205 -0.1569102   0.05873795 -1.72648542  0.49436369 -1.29470928
   0.75064482 -0.65526103  0.52805878  1.2691474  -0.3256552   0.77844263
  -0.33881792  0.88829873  0.62842993  1.02428804 -0.93867863  0.97487848
   0.06542529 -0.17878795 -0.3572727   0.94073483 -0.08374581 -1.44946575
   0.71995263 -2.09843083 -1.05770844 -0.63311977 -0.22065868  1.62388234
   0.70460566 -1.20592978  1.2041027   1.78026563 -0.44516036 -0.38959707
   0.07767587  0.43370948 -0.6790865   0.46613052 -1.71361785 -0.0917277
   0.79197964 -0.61267063  1.18922949  0.10127457 -0.28148889 -1.03837735
  -0.07018658  1.90141705 -0.88807036 -1.81034684 -2.00223107  0.52392848
  -1.10512114 -0.73912837  0.69010493  0.25019418 -0.3608534  -0.02917818
   1.69355544  0.51557699 -0.56687114  0.07136388  0.51094272 -1.74198381
   2.38493514  1.25566916 -0.6114415  -0.81392904 -0.73490701 -0.902273
   0.15623991 -1.52912839 -0.13004092  1.64630473 -0.59140786 -0.45195775
  -1.17882197  0.26207266  1.66396267  0.98635359 -0.65794616  0.14055282
  -0.90466745  0.19831783  0.42203826 -1.13895314  0.25247931 -0.41467529
  -0.58423935  0.03384459 -0.69248333  1.80033663 -0.43494269  1.38818698
  -1.23131293  0.24543901 -1.40391766  2.02395852  0.82973808  1.30066328
   2.06177537 -0.26344203  1.21635381 -0.11355776  0.03680592 -1.24736339
   0.57799907 -0.27504048 -0.60822247 -1.01422274 -0.19331823 -0.02673184
  -0.06587213  0.29674746]]


");

                        #endregion
                    }

                    List<VectorND> allVectors = new List<VectorND>();

                    foreach (string numpyDump in numpyDumps)
                    {
                        string[] xyz = Regex.Matches(numpyDump, @"\[([^\]]+)").
                            AsEnumerable().
                            Select(o => o.Groups[1].Value).
                            Select(o => o.Replace("[", "").Replace("]", "")).
                            ToArray();

                        var furtherBroken = xyz.
                            Select(o => Regex.Matches(o, @"[^\s]+").
                                AsEnumerable().
                                Select(p => combo.abs ? Math.Abs(double.Parse(p.Value)) : double.Parse(p.Value)).
                                ToArray()
                                ).
                            ToArray();

                        var transposed = Enumerable.Range(0, furtherBroken[0].Length).
                            Select(o => Enumerable.Range(0, furtherBroken.Length).
                                Select(p => furtherBroken[p][o]).
                                ToArray().
                                ToVectorND()).
                            ToArray();

                        allVectors.AddRange(transposed);
                    }


                    Debug3DWindow window = new Debug3DWindow();

                    window.AddAxisLines(1, LINE);

                    window.AddDots(allVectors.Select(o => o.ToPoint3D()), combo.useNormed ? DOT : DOT * 3.5, Colors.Gray);

                    window.AddText($"max len={allVectors.Max(o => o.Length).ToStringSignificantDigits(3)}");

                    window.Show();
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Python1D_Click(object sender, RoutedEventArgs e)
        {
            double DOT = .0075 * 3.5;
            double LINE = .005 * 3.5;

            try
            {
                Random rand = StaticRandom.GetRandomForThread();

                #region dump

                string numpydump = @"[-2.02318070e+00  2.46373191e-02 -6.97972047e-01  9.50525965e-01
  7.09259151e-01 -1.89129707e+00 -2.26185295e+00 -2.17068003e-01
 -6.97764781e-01 -1.66347548e+00 -9.23537547e-01 -7.13806484e-01
  1.12998236e+00 -1.63106254e+00  1.20067685e+00 -1.84535875e+00
  6.07724967e-01 -1.55440198e+00  3.50360900e-01  5.62246672e-01
 -1.22588572e-01  2.94614947e-01  6.98395906e-01  1.23725372e+00
 -2.07933150e+00  6.57898992e-01  1.10189267e+00 -3.19434778e-01
 -1.70207989e-01 -9.25166597e-03  1.18413768e+00 -8.70410295e-01
 -6.39552804e-01 -1.01780179e+00  6.59530237e-01  1.12752426e-01
  5.68466407e-02  9.09392199e-02 -1.04590018e-01 -2.25821739e-01
 -1.77545723e-01 -2.59171815e-01 -9.28295932e-01 -2.22132461e-01
  9.65923816e-01  4.25040855e-02  2.45173770e-01 -7.45985353e-01
 -9.85708459e-01  2.60297908e-01  7.11032266e-01 -8.96554363e-01
 -7.03009181e-01  1.04603427e+00  4.78029687e-01  5.90927728e-01
  5.18972814e-01  9.30380360e-01 -1.96918274e+00  8.57068744e-01
  5.48134696e-01  4.44079631e-01 -2.72067625e-01 -1.21051780e+00
  3.85424701e-02 -5.50403944e-01 -1.71384578e+00 -5.96325847e-01
 -1.15631723e+00  9.24238965e-02 -1.20949262e+00 -2.02533676e-01
 -1.14511091e+00 -9.48567501e-01 -2.02553100e-01 -5.23911991e-01
 -1.28540124e+00  9.66483640e-01  1.79300996e-01  3.39060864e-01
  1.94268115e+00 -1.03838307e+00 -8.82154751e-01 -3.17629607e-01
  4.31677097e-01  7.03338592e-01  1.68093409e-01 -1.03111473e+00
 -3.27797384e-01 -2.93084285e-01 -7.79531258e-01 -2.25611292e-01
  2.48546868e-01 -1.94487571e+00  1.68189925e-01 -1.15483743e+00
 -8.08743671e-01 -1.16829773e+00  3.81374252e-01 -2.29081112e-01
 -6.21875132e-01 -8.01168383e-01  1.58892799e+00 -8.52671534e-01
 -2.04064084e-02 -1.56033258e+00  1.26641484e-01  6.35909391e-01
 -1.19116413e-01  6.03112791e-01  5.10349368e-01 -8.64595215e-01
  2.43665148e-01 -4.82538148e-01  1.82418943e-01 -4.71143641e-01
 -2.01897351e+00 -1.23840274e-01  7.70242195e-01  3.13632302e-01
  2.03690900e-01  5.18385895e-02  2.50426983e-01 -1.13277648e+00
  1.77155159e+00  1.50228303e+00  1.74176002e-01 -6.56162956e-02
 -5.68677986e-01  5.48958934e-01  1.63933169e+00 -2.26139421e+00
 -9.02781864e-01 -2.04309525e+00 -1.57522028e-01 -3.09949011e-01
 -8.84374748e-01 -2.18748701e+00 -1.28073364e+00 -1.47417834e+00
  2.68939782e-01 -5.23548452e-01 -1.84393142e+00 -1.29381540e+00
 -3.61761553e-01 -2.08294346e+00 -1.66979826e+00  1.23807257e+00
  5.19697794e-01 -1.95996631e+00 -2.68540671e-01 -7.49303425e-01
 -7.82708215e-01 -9.98015490e-01 -5.00989995e-01  1.93330972e-01
 -1.01271834e+00  8.08583632e-01 -1.42282173e+00 -2.80707837e+00
  1.10123562e+00  1.44975125e+00  2.41544267e-01 -9.17939877e-01
 -2.39304739e-01  9.72930806e-01 -4.66380117e-01  7.23557573e-02
  2.03238823e+00 -7.72780739e-01 -3.03034501e+00 -1.48008115e+00
  1.58639263e-01 -6.66627211e-01  3.69341485e-01 -6.78655565e-01
  8.28739989e-01 -4.51243676e-01  7.06067341e-01 -3.80369255e-02
 -1.30427526e+00  2.45424880e-01 -4.91928843e-02 -7.92301355e-01
  6.04240142e-01  1.96420302e+00  1.88796244e+00  6.34950332e-01
 -2.30520201e-01  1.65316734e+00  1.71582639e+00 -2.43990294e+00
  2.10685087e-01 -9.73827513e-01  1.06231516e+00  3.82557866e-01
 -3.25293495e+00 -1.23495669e-01 -2.61220020e-01 -2.45167810e+00
  9.24041075e-02 -3.50664953e-01  1.43160478e-03 -3.00298135e-01
  1.39875846e-01 -2.62895282e-02  1.97834362e-02 -1.33185500e+00
  6.47753043e-01 -2.87785508e-01  1.27830461e+00 -5.33488463e-01
  9.84280013e-01  7.16374806e-02 -9.37242887e-01  8.46850786e-01
  9.68842747e-01  1.72234230e+00 -1.03118990e+00  1.45269289e+00
 -6.87357661e-01 -4.43776153e-01 -2.01858100e+00  6.26299754e-01
  1.33554835e+00  9.62400116e-02 -2.00116989e+00  3.85608202e-01
  7.95368271e-01  3.17256821e-01  9.12781624e-01 -5.60274555e-01
  8.83427251e-01  1.61062054e+00  3.34933879e+00  1.14533148e-01
  2.56083391e-01  2.20119960e-01  7.23609189e-01  6.66752902e-01
 -1.10391921e+00 -4.84728629e-01 -9.66618990e-01  4.91572110e-02
 -2.68883680e-01  9.94627621e-01 -1.55092198e-01 -1.35396554e+00
 -9.89162794e-01  7.66817644e-01 -4.39107193e-01  2.24409085e+00
 -7.41164364e-01  8.29568325e-01 -4.89149561e-01 -5.85654229e-01
 -7.48494177e-02  2.01065243e+00  4.32914931e-01  9.86897215e-01
 -6.69132899e-01  3.81758634e-01  7.97084960e-01  1.42493497e+00
  2.50366921e-01 -7.92488898e-01 -1.66256582e+00 -9.63750836e-01
  1.00071459e+00 -2.53030465e-01 -1.08681996e+00  1.58567284e-02
 -1.03808365e-01  6.35036066e-01 -1.00589404e+00  2.64736787e-01
 -8.50777215e-01  1.68092981e+00 -1.82601219e+00  5.07876337e-01
 -1.14002185e+00  1.04405472e+00 -7.64912825e-01 -6.15253868e-01
  1.28762978e-01 -1.96017273e+00 -1.05735800e-01  3.35275851e-01
 -1.70928496e-01  4.89441627e-01 -6.03567709e-01  3.38505572e-01
 -9.99661746e-01  8.16354269e-01 -1.71842303e+00  1.50001340e-01
  6.68507638e-01 -3.65614102e-01 -1.88812600e+00 -5.06978537e-01
 -9.04696626e-01  2.09369086e+00 -8.54931670e-01  1.14858574e+00
  2.42361019e-01  6.71114211e-02 -1.37218712e-01 -6.66050327e-01
 -6.98784878e-02 -1.15434147e+00  3.18547030e-01  3.13048812e-01
  6.93123128e-01 -1.58288889e+00 -4.99289658e-01 -1.12186197e+00
  6.90710774e-01 -3.93126780e-02 -1.29328703e+00  2.21253163e+00
 -6.95763453e-01  7.48756374e-01  1.52863920e+00  8.38976560e-01
  8.18949097e-01 -4.26168633e-01 -6.98433396e-01 -7.35184929e-02
 -1.86566283e-02  2.15740224e-01 -8.11067251e-01 -4.24548608e-01
 -1.05859097e+00 -9.62675875e-01  6.16432258e-01 -8.26247035e-01
 -4.35523281e-01  1.14839023e+00  8.07293396e-01 -1.64522053e-01
  1.64503414e+00  1.53617153e+00 -1.03005142e+00 -2.95037930e-01
 -9.19458564e-01  9.27570729e-01 -2.07868151e-01 -1.64181328e+00
  1.01063068e-02  6.11286504e-01  3.28227515e-01  1.23396554e+00
  7.94465378e-01 -4.79511861e-01  1.18498844e+00 -4.32519923e-01
  1.19044365e+00  1.31786136e+00 -1.12810533e+00 -3.69470196e-01
  2.38309487e+00  5.44862841e-01  1.38497896e+00  5.90080320e-01
 -1.82267148e-01 -1.28555372e-01  9.44518733e-01  1.16139116e+00
 -3.47844527e-01  7.25654451e-01 -1.27974470e+00  8.19909891e-01
 -7.26842779e-01 -8.00008617e-01  6.07525020e-01  7.32322732e-01
  2.62712109e+00  8.06546225e-01  8.86127309e-01  1.70610610e+00
  2.79630472e-01 -5.45741181e-01 -2.26051043e+00  6.10690795e-01
 -1.01511557e+00  2.89733171e-01 -6.66913701e-01 -1.60660291e-01
 -1.22510369e+00 -2.64539863e-01 -4.87244197e-01  1.04026122e+00
 -1.76281282e+00  1.49389600e+00 -2.17007186e+00  9.97983762e-01
  1.46682500e+00 -1.25208759e+00  2.80906524e-01 -5.55376280e-01
  2.33624467e+00  7.69705877e-01  3.07727543e-01 -1.10971304e+00
 -1.33156626e+00 -4.98330891e-01 -3.97155663e-01  8.91329366e-01
 -3.00309779e+00  2.62243434e+00  2.01191654e-01 -8.88061063e-01
  5.02080671e-01 -1.02192845e+00 -9.81547307e-01 -1.02293876e+00
  4.17790727e-01  9.18150939e-01  1.40399441e+00  2.10213768e-01
 -5.41118900e-01 -1.36371565e+00  3.80572222e-01 -1.34442207e+00
  5.33252848e-01  6.39634612e-01  8.85332706e-02 -2.19492872e+00
 -1.42644657e-01 -1.22006324e+00 -1.26729370e-01 -1.41805581e+00
  1.69211286e+00  3.72025961e-01 -1.59954959e+00 -5.73127252e-04
  2.06855796e-01 -4.31547854e-01 -1.28546948e+00 -1.77977217e+00
 -4.59128270e-01 -8.35402175e-01 -1.42770193e-01  3.02644286e-01
 -1.94181368e-01  4.36547215e-01  1.48650982e+00 -9.03210688e-01
 -1.16109686e+00  1.52949178e+00 -3.94841547e-01 -2.09506203e+00
 -6.96102141e-01 -3.87602029e-01 -2.30586475e-01 -3.17307405e-01
 -1.94057624e-01 -6.19464131e-01 -1.45192393e-01  7.69717016e-01
  9.75642953e-02 -2.19452043e-01 -6.69309149e-01 -6.33931647e-01
  8.87030752e-01 -5.64878193e-01 -2.54643202e-01 -1.08042269e+00
  2.44642320e+00  9.17047444e-01  5.56484508e-01  6.05406169e-01
 -7.60475120e-01  1.29563092e-01  3.36717478e-01 -1.29638654e+00
 -6.12107440e-01  2.14536487e-01 -5.19653381e-01  3.10159247e-01
  5.42553687e-01  2.03367039e+00  5.74084650e-02  9.48996114e-01
  1.78499671e+00  1.19233540e+00 -5.72083887e-01  9.55608788e-01
 -1.83604899e+00 -1.88600412e+00 -1.40666926e+00  2.82828326e+00
 -5.55213106e-01  6.21050172e-01  6.93104943e-02 -7.13487678e-01
 -5.86241983e-01  2.55902755e-01 -1.00118469e+00 -6.15291189e-01
 -3.17334065e-02 -1.60628838e-01  1.07930239e-01  1.46425850e+00
  1.57052384e+00  9.33288848e-01 -1.31683912e+00 -8.15906956e-01
  9.49708042e-01  1.25619451e-01 -4.96673661e-01 -1.63519274e+00
  2.35781986e+00  1.19104758e+00  3.26068884e-01 -5.61263203e-01
  2.98960411e+00  4.59749962e-01 -2.91489499e-01 -6.07883996e-03
  2.09751780e-01 -1.47185803e+00 -6.74772730e-01  7.64122615e-01
 -1.84959545e+00 -1.89892297e+00  1.24535643e+00 -5.21959439e-01
  3.57534686e-01  2.71006411e-01  1.56975337e+00  1.62529734e+00
  1.50920800e+00 -5.91281366e-01  7.55611063e-01 -1.70373467e+00
  2.81521433e-01 -2.72839204e-01 -5.67316278e-01 -1.50425260e+00
 -2.20188336e+00 -9.67392573e-01 -2.13221939e+00 -1.60507786e+00
  1.58138366e-01 -1.13204187e-01  2.07297996e+00 -1.46448866e+00
 -1.50933764e-01 -3.01730898e-01  1.42232544e+00  1.05630506e+00
  1.25863757e+00 -7.68165819e-01  1.07564482e+00  1.19628812e+00
 -3.98341299e-01  9.22447657e-01  7.82820335e-01 -6.23624183e-01
  3.98939487e-01 -1.19337764e+00  9.67547346e-01 -1.22162875e+00
 -1.47890561e+00  2.67961141e-01  1.41195556e+00 -8.47747891e-01
  5.60087428e-02  1.06457981e-01 -1.50231512e+00 -1.42603898e+00
  1.26937135e-01 -7.22273790e-01 -4.72764164e-01 -3.19740234e-01
  2.20854069e+00 -6.58179548e-01  1.87218108e+00 -1.52633373e+00
  2.18002105e-01 -1.62304908e-01  1.11070707e+00 -2.13163569e-01
 -6.95297318e-01 -1.17680995e+00  9.94799606e-01 -3.18819276e-01
 -1.57963884e+00  1.59440536e+00 -1.25180415e+00 -2.65857807e-01
  4.35423909e-01 -5.21038503e-02  6.03471003e-01  1.15634074e-02
 -4.37194013e-01  3.65407643e-01  1.81330615e+00  1.84280937e-01]


";

                #endregion

                numpydump = numpydump.
                    Replace("[", "").
                    Replace("]", "");

                var cast = Regex.Matches(numpydump, @"[^\s]+").
                    AsEnumerable().
                    Select(o => double.Parse(o.Value)).
                    ToArray();


                Debug3DWindow window = new Debug3DWindow()
                {
                    Title = "direct",
                };

                window.AddAxisLines(2, LINE);

                int count = 100;

                window.AddDots(cast.Select(o => new Point3D(o, 0, 0)), LINE, Colors.Gray);

                window.Show();


                window = new Debug3DWindow()
                {
                    Title = "usage",
                };

                var graph = Debug3DWindow.GetCountGraph(() => cast[rand.Next(cast.Length)]);
                window.AddGraph(graph, new Point3D(), 1);

                window.Show();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Python1D2_Click(object sender, RoutedEventArgs e)
        {
            double DOT = .0075 * 3.5;
            double LINE = .005 * 3.5;

            try
            {
                Random rand = StaticRandom.GetRandomForThread();

                List<string> numpydumps = new List<string>();
                #region dump

                numpydumps.Add(@"[-2.02318070e+00  2.46373191e-02 -6.97972047e-01  9.50525965e-01
  7.09259151e-01 -1.89129707e+00 -2.26185295e+00 -2.17068003e-01
 -6.97764781e-01 -1.66347548e+00 -9.23537547e-01 -7.13806484e-01
  1.12998236e+00 -1.63106254e+00  1.20067685e+00 -1.84535875e+00
  6.07724967e-01 -1.55440198e+00  3.50360900e-01  5.62246672e-01
 -1.22588572e-01  2.94614947e-01  6.98395906e-01  1.23725372e+00
 -2.07933150e+00  6.57898992e-01  1.10189267e+00 -3.19434778e-01
 -1.70207989e-01 -9.25166597e-03  1.18413768e+00 -8.70410295e-01
 -6.39552804e-01 -1.01780179e+00  6.59530237e-01  1.12752426e-01
  5.68466407e-02  9.09392199e-02 -1.04590018e-01 -2.25821739e-01
 -1.77545723e-01 -2.59171815e-01 -9.28295932e-01 -2.22132461e-01
  9.65923816e-01  4.25040855e-02  2.45173770e-01 -7.45985353e-01
 -9.85708459e-01  2.60297908e-01  7.11032266e-01 -8.96554363e-01
 -7.03009181e-01  1.04603427e+00  4.78029687e-01  5.90927728e-01
  5.18972814e-01  9.30380360e-01 -1.96918274e+00  8.57068744e-01
  5.48134696e-01  4.44079631e-01 -2.72067625e-01 -1.21051780e+00
  3.85424701e-02 -5.50403944e-01 -1.71384578e+00 -5.96325847e-01
 -1.15631723e+00  9.24238965e-02 -1.20949262e+00 -2.02533676e-01
 -1.14511091e+00 -9.48567501e-01 -2.02553100e-01 -5.23911991e-01
 -1.28540124e+00  9.66483640e-01  1.79300996e-01  3.39060864e-01
  1.94268115e+00 -1.03838307e+00 -8.82154751e-01 -3.17629607e-01
  4.31677097e-01  7.03338592e-01  1.68093409e-01 -1.03111473e+00
 -3.27797384e-01 -2.93084285e-01 -7.79531258e-01 -2.25611292e-01
  2.48546868e-01 -1.94487571e+00  1.68189925e-01 -1.15483743e+00
 -8.08743671e-01 -1.16829773e+00  3.81374252e-01 -2.29081112e-01
 -6.21875132e-01 -8.01168383e-01  1.58892799e+00 -8.52671534e-01
 -2.04064084e-02 -1.56033258e+00  1.26641484e-01  6.35909391e-01
 -1.19116413e-01  6.03112791e-01  5.10349368e-01 -8.64595215e-01
  2.43665148e-01 -4.82538148e-01  1.82418943e-01 -4.71143641e-01
 -2.01897351e+00 -1.23840274e-01  7.70242195e-01  3.13632302e-01
  2.03690900e-01  5.18385895e-02  2.50426983e-01 -1.13277648e+00
  1.77155159e+00  1.50228303e+00  1.74176002e-01 -6.56162956e-02
 -5.68677986e-01  5.48958934e-01  1.63933169e+00 -2.26139421e+00
 -9.02781864e-01 -2.04309525e+00 -1.57522028e-01 -3.09949011e-01
 -8.84374748e-01 -2.18748701e+00 -1.28073364e+00 -1.47417834e+00
  2.68939782e-01 -5.23548452e-01 -1.84393142e+00 -1.29381540e+00
 -3.61761553e-01 -2.08294346e+00 -1.66979826e+00  1.23807257e+00
  5.19697794e-01 -1.95996631e+00 -2.68540671e-01 -7.49303425e-01
 -7.82708215e-01 -9.98015490e-01 -5.00989995e-01  1.93330972e-01
 -1.01271834e+00  8.08583632e-01 -1.42282173e+00 -2.80707837e+00
  1.10123562e+00  1.44975125e+00  2.41544267e-01 -9.17939877e-01
 -2.39304739e-01  9.72930806e-01 -4.66380117e-01  7.23557573e-02
  2.03238823e+00 -7.72780739e-01 -3.03034501e+00 -1.48008115e+00
  1.58639263e-01 -6.66627211e-01  3.69341485e-01 -6.78655565e-01
  8.28739989e-01 -4.51243676e-01  7.06067341e-01 -3.80369255e-02
 -1.30427526e+00  2.45424880e-01 -4.91928843e-02 -7.92301355e-01
  6.04240142e-01  1.96420302e+00  1.88796244e+00  6.34950332e-01
 -2.30520201e-01  1.65316734e+00  1.71582639e+00 -2.43990294e+00
  2.10685087e-01 -9.73827513e-01  1.06231516e+00  3.82557866e-01
 -3.25293495e+00 -1.23495669e-01 -2.61220020e-01 -2.45167810e+00
  9.24041075e-02 -3.50664953e-01  1.43160478e-03 -3.00298135e-01
  1.39875846e-01 -2.62895282e-02  1.97834362e-02 -1.33185500e+00
  6.47753043e-01 -2.87785508e-01  1.27830461e+00 -5.33488463e-01
  9.84280013e-01  7.16374806e-02 -9.37242887e-01  8.46850786e-01
  9.68842747e-01  1.72234230e+00 -1.03118990e+00  1.45269289e+00
 -6.87357661e-01 -4.43776153e-01 -2.01858100e+00  6.26299754e-01
  1.33554835e+00  9.62400116e-02 -2.00116989e+00  3.85608202e-01
  7.95368271e-01  3.17256821e-01  9.12781624e-01 -5.60274555e-01
  8.83427251e-01  1.61062054e+00  3.34933879e+00  1.14533148e-01
  2.56083391e-01  2.20119960e-01  7.23609189e-01  6.66752902e-01
 -1.10391921e+00 -4.84728629e-01 -9.66618990e-01  4.91572110e-02
 -2.68883680e-01  9.94627621e-01 -1.55092198e-01 -1.35396554e+00
 -9.89162794e-01  7.66817644e-01 -4.39107193e-01  2.24409085e+00
 -7.41164364e-01  8.29568325e-01 -4.89149561e-01 -5.85654229e-01
 -7.48494177e-02  2.01065243e+00  4.32914931e-01  9.86897215e-01
 -6.69132899e-01  3.81758634e-01  7.97084960e-01  1.42493497e+00
  2.50366921e-01 -7.92488898e-01 -1.66256582e+00 -9.63750836e-01
  1.00071459e+00 -2.53030465e-01 -1.08681996e+00  1.58567284e-02
 -1.03808365e-01  6.35036066e-01 -1.00589404e+00  2.64736787e-01
 -8.50777215e-01  1.68092981e+00 -1.82601219e+00  5.07876337e-01
 -1.14002185e+00  1.04405472e+00 -7.64912825e-01 -6.15253868e-01
  1.28762978e-01 -1.96017273e+00 -1.05735800e-01  3.35275851e-01
 -1.70928496e-01  4.89441627e-01 -6.03567709e-01  3.38505572e-01
 -9.99661746e-01  8.16354269e-01 -1.71842303e+00  1.50001340e-01
  6.68507638e-01 -3.65614102e-01 -1.88812600e+00 -5.06978537e-01
 -9.04696626e-01  2.09369086e+00 -8.54931670e-01  1.14858574e+00
  2.42361019e-01  6.71114211e-02 -1.37218712e-01 -6.66050327e-01
 -6.98784878e-02 -1.15434147e+00  3.18547030e-01  3.13048812e-01
  6.93123128e-01 -1.58288889e+00 -4.99289658e-01 -1.12186197e+00
  6.90710774e-01 -3.93126780e-02 -1.29328703e+00  2.21253163e+00
 -6.95763453e-01  7.48756374e-01  1.52863920e+00  8.38976560e-01
  8.18949097e-01 -4.26168633e-01 -6.98433396e-01 -7.35184929e-02
 -1.86566283e-02  2.15740224e-01 -8.11067251e-01 -4.24548608e-01
 -1.05859097e+00 -9.62675875e-01  6.16432258e-01 -8.26247035e-01
 -4.35523281e-01  1.14839023e+00  8.07293396e-01 -1.64522053e-01
  1.64503414e+00  1.53617153e+00 -1.03005142e+00 -2.95037930e-01
 -9.19458564e-01  9.27570729e-01 -2.07868151e-01 -1.64181328e+00
  1.01063068e-02  6.11286504e-01  3.28227515e-01  1.23396554e+00
  7.94465378e-01 -4.79511861e-01  1.18498844e+00 -4.32519923e-01
  1.19044365e+00  1.31786136e+00 -1.12810533e+00 -3.69470196e-01
  2.38309487e+00  5.44862841e-01  1.38497896e+00  5.90080320e-01
 -1.82267148e-01 -1.28555372e-01  9.44518733e-01  1.16139116e+00
 -3.47844527e-01  7.25654451e-01 -1.27974470e+00  8.19909891e-01
 -7.26842779e-01 -8.00008617e-01  6.07525020e-01  7.32322732e-01
  2.62712109e+00  8.06546225e-01  8.86127309e-01  1.70610610e+00
  2.79630472e-01 -5.45741181e-01 -2.26051043e+00  6.10690795e-01
 -1.01511557e+00  2.89733171e-01 -6.66913701e-01 -1.60660291e-01
 -1.22510369e+00 -2.64539863e-01 -4.87244197e-01  1.04026122e+00
 -1.76281282e+00  1.49389600e+00 -2.17007186e+00  9.97983762e-01
  1.46682500e+00 -1.25208759e+00  2.80906524e-01 -5.55376280e-01
  2.33624467e+00  7.69705877e-01  3.07727543e-01 -1.10971304e+00
 -1.33156626e+00 -4.98330891e-01 -3.97155663e-01  8.91329366e-01
 -3.00309779e+00  2.62243434e+00  2.01191654e-01 -8.88061063e-01
  5.02080671e-01 -1.02192845e+00 -9.81547307e-01 -1.02293876e+00
  4.17790727e-01  9.18150939e-01  1.40399441e+00  2.10213768e-01
 -5.41118900e-01 -1.36371565e+00  3.80572222e-01 -1.34442207e+00
  5.33252848e-01  6.39634612e-01  8.85332706e-02 -2.19492872e+00
 -1.42644657e-01 -1.22006324e+00 -1.26729370e-01 -1.41805581e+00
  1.69211286e+00  3.72025961e-01 -1.59954959e+00 -5.73127252e-04
  2.06855796e-01 -4.31547854e-01 -1.28546948e+00 -1.77977217e+00
 -4.59128270e-01 -8.35402175e-01 -1.42770193e-01  3.02644286e-01
 -1.94181368e-01  4.36547215e-01  1.48650982e+00 -9.03210688e-01
 -1.16109686e+00  1.52949178e+00 -3.94841547e-01 -2.09506203e+00
 -6.96102141e-01 -3.87602029e-01 -2.30586475e-01 -3.17307405e-01
 -1.94057624e-01 -6.19464131e-01 -1.45192393e-01  7.69717016e-01
  9.75642953e-02 -2.19452043e-01 -6.69309149e-01 -6.33931647e-01
  8.87030752e-01 -5.64878193e-01 -2.54643202e-01 -1.08042269e+00
  2.44642320e+00  9.17047444e-01  5.56484508e-01  6.05406169e-01
 -7.60475120e-01  1.29563092e-01  3.36717478e-01 -1.29638654e+00
 -6.12107440e-01  2.14536487e-01 -5.19653381e-01  3.10159247e-01
  5.42553687e-01  2.03367039e+00  5.74084650e-02  9.48996114e-01
  1.78499671e+00  1.19233540e+00 -5.72083887e-01  9.55608788e-01
 -1.83604899e+00 -1.88600412e+00 -1.40666926e+00  2.82828326e+00
 -5.55213106e-01  6.21050172e-01  6.93104943e-02 -7.13487678e-01
 -5.86241983e-01  2.55902755e-01 -1.00118469e+00 -6.15291189e-01
 -3.17334065e-02 -1.60628838e-01  1.07930239e-01  1.46425850e+00
  1.57052384e+00  9.33288848e-01 -1.31683912e+00 -8.15906956e-01
  9.49708042e-01  1.25619451e-01 -4.96673661e-01 -1.63519274e+00
  2.35781986e+00  1.19104758e+00  3.26068884e-01 -5.61263203e-01
  2.98960411e+00  4.59749962e-01 -2.91489499e-01 -6.07883996e-03
  2.09751780e-01 -1.47185803e+00 -6.74772730e-01  7.64122615e-01
 -1.84959545e+00 -1.89892297e+00  1.24535643e+00 -5.21959439e-01
  3.57534686e-01  2.71006411e-01  1.56975337e+00  1.62529734e+00
  1.50920800e+00 -5.91281366e-01  7.55611063e-01 -1.70373467e+00
  2.81521433e-01 -2.72839204e-01 -5.67316278e-01 -1.50425260e+00
 -2.20188336e+00 -9.67392573e-01 -2.13221939e+00 -1.60507786e+00
  1.58138366e-01 -1.13204187e-01  2.07297996e+00 -1.46448866e+00
 -1.50933764e-01 -3.01730898e-01  1.42232544e+00  1.05630506e+00
  1.25863757e+00 -7.68165819e-01  1.07564482e+00  1.19628812e+00
 -3.98341299e-01  9.22447657e-01  7.82820335e-01 -6.23624183e-01
  3.98939487e-01 -1.19337764e+00  9.67547346e-01 -1.22162875e+00
 -1.47890561e+00  2.67961141e-01  1.41195556e+00 -8.47747891e-01
  5.60087428e-02  1.06457981e-01 -1.50231512e+00 -1.42603898e+00
  1.26937135e-01 -7.22273790e-01 -4.72764164e-01 -3.19740234e-01
  2.20854069e+00 -6.58179548e-01  1.87218108e+00 -1.52633373e+00
  2.18002105e-01 -1.62304908e-01  1.11070707e+00 -2.13163569e-01
 -6.95297318e-01 -1.17680995e+00  9.94799606e-01 -3.18819276e-01
 -1.57963884e+00  1.59440536e+00 -1.25180415e+00 -2.65857807e-01
  4.35423909e-01 -5.21038503e-02  6.03471003e-01  1.15634074e-02
 -4.37194013e-01  3.65407643e-01  1.81330615e+00  1.84280937e-01]


");

                #endregion
                #region dump

                numpydumps.Add(@"[-8.23535933e-01  6.52453337e-01 -1.21534417e+00 -5.21235763e-01
 -1.82426590e+00 -3.84784340e-01  3.47701487e-01 -1.47136224e+00
  7.51630908e-01  2.29876425e-01  5.53938882e-01 -9.60344153e-01
  2.64629832e-01  4.43660595e-01 -1.13640064e+00  3.48815772e-01
 -2.53435937e+00 -2.18561130e-01 -6.02796424e-02 -7.95904179e-02
 -7.86637515e-02 -2.37557262e-01  1.66489911e+00 -1.40212516e+00
  4.11750356e-01 -9.40070561e-01  4.67217082e-01 -9.90563203e-01
 -1.46788426e-01 -1.14568377e+00  6.48124291e-01 -3.49564422e-01
 -6.20532230e-01  9.64367403e-01 -1.47771483e+00 -7.32512301e-01
 -2.60424835e+00 -1.77027195e+00  1.79080722e+00  1.17371415e+00
  7.50405494e-01 -1.19668714e+00  4.07425646e-01  1.27498901e+00
 -1.36336631e+00 -6.67601380e-03  1.31303774e+00  8.05166668e-01
  9.19685390e-01 -1.62711420e+00  4.52313802e-01  5.36590000e-01
  1.24120054e+00 -7.55800492e-01  1.22249067e+00  1.29248986e+00
 -6.09090587e-02 -8.91475285e-01 -2.18169323e-01 -2.60232084e-01
  2.18233227e-02 -1.13113119e+00  9.87898409e-01 -1.50678936e+00
 -4.64073230e-01  4.78775845e-01 -2.16573608e-01  2.29756345e-01
  8.03016227e-01  8.42810701e-01 -2.19683831e-01 -7.90498854e-02
  1.22497212e-01 -4.65943655e-01 -2.22080004e-01  4.42359833e-01
 -8.71132466e-01  7.54020313e-01  3.54529844e-01 -1.74051017e-01
 -7.22418258e-01 -1.13226751e-01 -5.45713814e-01 -1.42673677e+00
  4.68135390e-01  3.71928277e-01  1.40597577e+00 -1.13278718e+00
  9.96202811e-01 -4.88496448e-01 -9.76952277e-02 -4.72365146e-01
  5.19038495e-01 -4.03519880e-01 -6.95924011e-01  1.80314536e+00
 -6.20218791e-01  1.25836424e-01  3.22583479e-01 -2.78372179e+00
  2.71961462e-01 -2.14650751e-02 -9.08133635e-01 -1.62776004e+00
  8.90587489e-01  4.92073002e-01  6.23548601e-01 -1.29645777e+00
 -2.21031849e+00  9.24491526e-01 -1.25947186e+00  1.71835745e+00
 -1.58253217e+00  1.44584143e+00  5.72353816e-01 -6.92318073e-01
 -7.00209568e-01  1.32489876e-01  5.81779007e-01  2.70012633e+00
 -1.69756731e+00 -2.14171862e+00  1.39452648e-01 -2.22256678e-01
 -9.91270142e-01  1.34107058e+00 -1.14296169e+00 -1.11781253e+00
  4.12973470e-01  8.66874480e-01 -5.89148394e-01 -9.21335948e-01
  3.01493185e-01 -4.90913660e-01 -1.40867093e+00  1.76246677e+00
  3.91546238e-01 -7.46698725e-01 -3.48191473e-01 -5.26808479e-01
 -2.48420911e-01  7.51733950e-01  5.70815482e-01  3.20223361e+00
 -1.18563036e+00 -7.31038117e-01  7.75817923e-01 -8.43761915e-01
 -1.03588317e+00 -5.10146419e-01  4.00466461e-01 -1.09076569e+00
  4.69104750e-01 -5.51241618e-01  1.48498791e+00 -1.84643246e-02
 -2.67093993e-01  1.18651365e+00  8.34780625e-01  6.46855810e-01
 -5.15414537e-01  2.52418678e+00  6.06322832e-01  1.66608731e-01
  1.40154873e-01  1.83437860e-01 -1.14086632e-02 -1.11385821e+00
  9.93674023e-01  3.01647503e-01  5.22565982e-01 -1.26702497e+00
 -1.07774352e+00 -1.15627339e-02 -1.32294036e+00 -2.19927042e+00
  2.61634223e-01  6.41463687e-01 -3.18987177e-01 -4.89641620e-01
  7.01987721e-01  7.11246508e-01  4.89166551e-03  5.81176614e-01
  1.37126814e+00 -1.39706315e-01 -2.30288157e+00  1.41950565e-01
  1.40713664e+00 -7.86211217e-02 -6.97728770e-01 -2.03665915e-01
  7.97949260e-01 -3.92652382e-01 -1.53731584e-01 -1.40757495e+00
 -5.40277511e-01 -1.26759490e+00  6.45292940e-02 -2.09719135e+00
  8.26590408e-01 -7.21362952e-01  9.63632526e-01 -1.41619476e+00
  5.44588096e-01 -8.18117616e-01 -2.56211713e-01 -9.86979040e-01
 -2.48955272e-01  5.65018851e-02  1.08962192e+00  1.93169545e+00
  2.33580850e-01 -3.39348388e-01 -3.51359407e-01 -1.02213258e+00
  1.89179748e+00 -2.62485471e-01 -7.18532543e-01  1.24324773e-01
  5.80132182e-01  1.09331160e-02  3.67937735e-01  1.45986518e+00
  8.76775602e-01  1.49113908e+00  9.46580077e-01  6.03963417e-01
  7.92477252e-02  2.42009859e-01  5.11884543e-01  3.50356440e-01
 -7.88601859e-01  3.26297135e-01  2.34566270e-02 -8.43916805e-01
  8.84012496e-01  1.90876264e+00 -1.19956296e-01 -2.68905220e-01
  4.00995285e-01  2.93135577e-01  2.09305396e-01  4.52673968e-01
  2.11381632e+00  1.09793488e+00  1.81378828e+00 -8.43085462e-01
 -7.16148625e-01 -8.67553985e-01  6.58952440e-01  4.98038125e-02
 -1.60649892e-01 -9.84024798e-01  9.75953663e-01  3.93426862e-01
 -6.57653588e-02 -1.28343026e-02 -1.92350205e+00  9.11079868e-01
  1.17841192e+00 -1.51392292e+00 -4.25951473e-01 -7.91448839e-02
  1.20689474e+00  1.59916378e+00 -1.58772887e+00 -1.69658015e+00
 -1.35181815e+00  2.16614728e-01  9.67048146e-02 -2.49254294e-01
 -3.14249157e-01  5.80947741e-01 -1.27522236e+00 -2.03141950e+00
 -6.66617278e-01  1.83945439e-04  3.86611178e-01 -1.19377416e-01
 -1.01717811e-01 -4.76981454e-01 -3.15693030e-01  3.24820110e-01
 -8.95491557e-01 -3.88857577e-01 -7.23484539e-01  5.15980255e-01
 -1.45656724e+00  4.12270111e-01 -7.57422791e-01  4.15499135e-01
  4.69051415e-02  1.26557912e+00 -2.97820198e-01  3.04592797e-01
  6.29456826e-03 -7.30987852e-01  7.60568434e-01 -5.66598039e-01
 -1.12929215e-02 -1.67652038e+00 -8.63661817e-01 -2.00101041e-01
 -1.07585968e+00  1.03375640e+00 -1.40952545e+00 -6.12336143e-01
  4.79500986e-01  7.56973872e-02 -1.24627260e+00 -6.24490735e-01
  3.93364386e-01 -5.20022760e-01 -1.65403610e+00 -1.13318100e+00
  4.24589207e-01 -1.12291470e+00  5.46847341e-01  3.05291977e-01
 -6.25245719e-04 -1.46047826e+00 -8.09224288e-01  1.10080522e+00
  1.62237656e+00 -3.47692026e-01  1.72103236e+00 -1.78416944e+00
  8.14761365e-01 -6.60217594e-01 -3.06831015e-01  1.11367945e+00
  8.91595424e-01  2.62311140e+00  9.14879727e-01 -2.22593350e-01
  7.79086234e-01 -2.05720044e+00  6.81823690e-01 -2.99451242e-01
  5.60543610e-01 -5.61207953e-01  6.39182502e-01 -1.20105045e-01
 -9.89750529e-02 -1.05811712e+00  2.27112144e+00 -3.32699484e-01
  1.84799522e-01 -6.23493560e-01  3.94165677e-01  6.70794981e-01
  1.98065786e+00 -7.52344615e-01 -1.10645645e+00  1.29491773e+00
 -1.76142861e+00  1.36762539e+00  1.76102758e+00 -2.01866024e-01
  1.76040893e+00 -4.83945643e-01  5.89761346e-01  3.76238998e-01
  5.27351615e-01 -9.88041847e-01 -1.80287653e+00  7.34383100e-01
 -1.79762677e+00  6.35350784e-01  1.55541490e+00 -4.03577225e-01
  5.52272879e-01 -1.60131419e+00 -1.19518721e+00 -8.10952431e-01
 -1.24133040e+00  1.37933739e+00 -2.99592005e-01 -3.64429070e-01
 -4.48756422e-01  1.05586593e+00 -8.67029664e-01  4.94723837e-01
  2.43004991e-01 -8.19709987e-01  1.11334315e+00 -1.69609875e+00
  1.35766296e+00 -9.77689076e-01 -5.90423442e-01 -5.46712324e-01
  1.07925697e+00  6.78706020e-01 -2.08508534e+00  3.17944435e-03
  1.18172688e+00  6.40536739e-01 -1.89652000e-01 -5.40675954e-02
  1.76269781e+00 -1.31162643e+00 -1.02809095e+00  1.77524246e+00
  1.37308152e+00  6.82175703e-01 -1.61963081e-01 -1.04362337e+00
 -1.79874376e+00 -8.95719391e-01 -6.84001895e-01  8.56448139e-01
 -2.19097800e+00  1.56388534e-01  1.20810086e+00 -3.79039169e-01
 -7.75542052e-01  7.88203633e-01 -3.03339273e-01 -4.42455180e-01
 -2.69471547e+00  6.42588420e-01 -1.15050362e+00  1.54458480e-01
 -1.12465124e+00 -1.09673564e+00 -1.82438987e+00 -1.86471110e-01
 -2.73466423e-01 -2.29001706e-02 -2.68182590e+00  2.24886653e-01
  3.17142563e-01 -2.01837003e-01 -1.27376827e+00 -8.27089983e-01
 -4.33126339e-01 -2.37844277e-01 -1.05652709e+00  6.55764175e-01
 -7.46496084e-01 -8.43133636e-02  8.68366973e-01  1.34684615e+00
  1.30672649e-01 -6.31187407e-01  5.21283533e-01  6.60650517e-01
 -5.34836582e-01  1.62236421e-01  2.77591848e-01  2.19390382e-01
 -3.40399014e-01  1.05027752e+00 -1.71851982e+00 -1.11506812e+00
  1.07404502e+00  1.66705184e-02 -6.92890626e-01 -6.86871709e-01
  8.68318344e-01 -4.23116583e-01  8.61640246e-02 -1.92779726e+00
  1.69616927e+00 -2.59382206e+00 -3.80836855e-01  9.60915962e-01
  3.82434650e-03 -2.62137201e+00 -1.42009829e-01 -2.03094073e+00
  1.25766831e+00 -5.04370994e-01  9.53598574e-01  1.07620034e-01
  6.18562116e-01  1.13052217e-01  8.88448340e-01 -1.09097130e+00
 -1.15556630e+00 -1.05106772e-01 -1.39620072e+00 -5.42645957e-01
 -6.25051250e-01 -1.28151096e-01  6.98737419e-01 -2.15022541e-01
 -4.11161435e-01  2.91674672e-01  4.21835823e-01 -3.66211416e-01
 -1.02574618e+00 -2.05910617e+00  1.60137243e+00 -5.18406152e-01
  7.85529207e-01  8.75040597e-01  5.85490778e-01  6.80030928e-01
  9.42024572e-01 -2.47896434e-01  3.01266642e-01  1.68361857e+00
  1.82829047e-01  2.07931944e+00  1.87401834e-01 -4.98389121e-01
  1.45582642e+00 -7.85289671e-01 -1.33072662e+00 -1.91203399e+00
  6.44503220e-01  1.94095680e+00  3.07394699e-01 -1.76275673e+00
 -8.45612220e-01 -1.56325264e-01 -5.43087148e-01  1.16740670e+00
 -1.25196583e-01 -1.38655361e-01  1.15424216e-01 -1.44649123e+00
 -1.46492960e+00  5.70474184e-01  2.64160001e-01  6.81144359e-01
  2.05617848e+00 -4.67363704e-01 -7.84681752e-01 -4.33364775e-01
 -8.74337334e-01 -1.44849095e-03  1.18319883e+00  3.93258928e-01
  1.60359679e-01 -7.87624822e-01  4.26245456e-01 -6.78622680e-01
 -2.14905645e-01  1.29229890e-01  5.16908720e-01 -4.30083082e-01
 -3.89026483e-02  1.03563105e+00 -8.43979425e-01 -6.98814379e-01
 -6.07509443e-02  1.23668796e+00  1.34755291e-01 -2.68754564e-01
  1.09849232e+00  2.43993411e-01 -2.97738270e+00 -4.82586653e-01
  9.05128647e-01  9.41876665e-02  1.86900301e+00  9.74297545e-01
  4.22692824e-01  1.20876183e+00 -1.12013415e-01 -1.80691640e-01
 -1.53085295e+00 -1.85546569e+00  9.96152909e-02 -1.61942430e+00
 -9.52899229e-01  4.61409059e-01  1.01403843e+00  1.10037151e+00
  1.79777439e-01 -1.88945636e+00  1.68146497e+00 -1.29541400e+00
  1.63995025e+00  8.47051131e-01  5.13740819e-03 -6.22421677e-01
 -3.83865262e-02  6.63564203e-02  1.19676429e+00  1.34068367e+00
 -6.32399769e-01 -6.61624759e-01 -1.54083992e+00  9.18408756e-01
  5.40318797e-01 -4.51590895e-01  1.18178810e-01  9.14168092e-01
  1.31377669e-02 -6.40518685e-01  1.25480256e+00  4.57198179e-01
  6.38657383e-01 -4.70580669e-01 -1.42331358e+00  3.61874933e-01]


");

                #endregion
                #region dump

                numpydumps.Add(@"[ 0.80391859  0.10278664  0.22916101 -0.27871651  0.55460296 -0.02305693
  0.61710933 -1.19959141 -0.57253831 -0.15046079  0.73524744  0.24769234
 -0.92400193  0.47997876  1.30921777 -0.34934916 -1.74640281  2.45525533
 -2.32424758 -0.0242772   0.94423634 -1.12314444  0.77816973 -0.0213033
 -0.54547628  0.15483401 -0.10397126 -1.75338089 -1.21491015  2.35534395
 -0.950474   -0.37894257  0.69327813  0.00345891 -0.62609481 -0.94425493
  1.49211238 -1.05199964 -1.47765314  0.28109756 -0.04325842  0.6341947
  0.35158173  1.89173001 -0.57308932 -1.08191792 -0.40354671  0.47367277
 -1.14545542 -1.3581537  -0.00611398 -0.43686076 -1.66943097  0.15194402
 -1.3224044  -0.93847842 -1.53533706 -0.0036231  -0.44848024  0.6642615
 -1.75426809 -1.38650205 -0.96758155 -0.10699772  0.19358289  0.14298201
  0.04055795 -0.17937219  0.74764409  0.91546119  0.3609217  -0.09846226
 -0.50093301 -0.06665942  2.09816314 -0.17541434 -0.68258506  2.08006284
 -1.02021588 -0.60573547  0.36474233  0.755683   -0.60828872  0.79867482
 -1.02047734 -0.39605803  0.39846166  2.98209533 -1.25957814  0.22079734
  0.87778992 -0.38588591 -1.10149984 -0.24673581 -1.00122995  0.49263188
 -0.92964321  0.14383101 -0.24191757 -0.94175005  1.39574327 -0.37977228
  1.6688502   0.58812812 -0.04979969  0.99709486 -2.03831329  0.44313109
  0.82138458 -0.97670892  1.16799897  1.1208464  -0.68926557  0.064281
 -0.08210554  0.99786011 -1.97534722 -0.47372823  0.01978588  0.385626
  0.18558556  1.21944179  1.68787065 -0.47561     0.75636914  1.31792055
 -0.61606643 -0.45562102 -0.77851821 -0.96900003  0.63094453 -0.92924014
 -0.38909944  1.39528671  0.27330276 -0.43874094 -0.56488999  0.52466006
  0.30201454 -0.13037109 -1.48708686 -0.311646   -1.47925479  1.42008385
  0.4198385  -0.49260426  0.64122384 -1.2380998   0.43232388  1.08225209
 -0.28478725  0.08289923  0.52340587  0.48873802  0.03749247  2.04562839
 -0.79791066 -1.03050736  1.85239682  0.49968943 -1.60976902 -0.79812136
 -0.0234811  -1.30257004 -1.25260286  0.92480146  0.26216954 -0.22948221
  0.66849888 -0.69102791  0.73919977 -1.09384461  0.69879656 -1.44779724
  0.05497865  0.40691931 -3.15311162  0.03421924 -1.10064085 -0.60907398
 -0.12203215 -0.22404509 -1.37679482 -0.08637137  0.69373497 -1.26461383
 -0.05395872 -0.09811386  0.09636623 -1.94817223  0.50552813 -0.20134244
 -0.8622052  -1.54585237 -0.10247815 -1.32365591  0.32791565 -1.81668462
 -0.80706939 -0.52472934 -0.63162224 -2.28798655 -0.68241949 -0.11923601
 -0.17213539  0.37817703 -0.39267678  0.52083377  0.95493513  1.0601395
  0.21183934 -0.96453905 -0.43946201  0.73937737  0.15523211  1.24237199
 -0.36751682 -1.42905531 -0.09506246 -0.24724012 -0.75224826  1.22426867
  2.23685117  1.47024608  2.11146247 -0.60259509  0.62307751  0.97825362
 -0.32320755  0.8888965   1.13700504  1.92447381  0.1597565   0.86502331
  0.40778698  2.49249123  1.80711295 -0.78514166 -1.6870407   0.56985335
  1.42629985  0.82878345  0.42979983 -1.02241731  0.43834894  0.24552362
 -0.8274779   0.6391809   0.41493862  0.37473354  1.57819668  0.18235839
  0.11751355  1.37140201  1.70043212 -0.76832607 -0.18737096 -2.20171101
  0.12703241 -0.96773228 -1.17537316  1.02789727  2.24307469  2.22390366
  0.37260027  0.66061033  1.04807705 -0.83719163  1.12994876  1.38610303
 -0.95556291  1.23647162 -0.76003436  0.81092225  0.26159553 -0.00330763
 -0.29234983 -0.58254077 -1.12619976 -0.63127314 -0.18619527 -0.37362422
 -0.41408464  1.17810848  1.82147015 -0.00781903  0.22590425 -1.28425825
 -0.79813482 -1.69010155 -0.08134251 -0.02971179 -0.40501513  0.58408103
  0.03529018 -0.89864676 -0.48610068  0.4681628   0.29580482 -0.81128699
 -1.13947844 -1.16492285  0.15971227  1.33203267 -1.14250045  0.99360451
 -0.11136308  0.51624895  0.03919126  1.33752321 -0.43998773 -1.03114867
  0.15785527 -0.81653304  1.25731707  2.26972638 -0.55073292  0.01097868
  0.44071805  1.44497486 -0.0778107   0.20858009  1.31856856 -1.50814976
 -0.14650701  0.10962357 -0.40788893 -0.11125636 -0.54765081 -0.55213619
 -0.90375279  2.22246479  0.77847682  0.66276568  0.40187996  1.07958693
  0.41663524 -1.71706656 -1.07827902  1.37816328  0.47307106  0.53294883
  0.31212553  2.58186017 -1.48048661  1.34505532  0.16900505  0.40955431
 -0.17357977 -1.27484912  0.10301524 -0.71466069 -0.39909852 -0.67067852
 -0.68709778 -0.60706831  1.03452504  0.07602339  0.01323489 -0.34231602
 -2.05856709 -0.19911526 -0.12118978  0.26132796 -0.1977878   0.7035188
  1.64758737 -0.2425931  -0.20235447 -0.3690714  -0.74654807  1.1789075
  0.53498315  0.31555586 -0.64336503  2.20005736 -0.14583082 -1.77686014
  0.6820042   0.48561285  1.0476616  -0.4507585   1.19785251 -1.56621126
  1.95454811 -0.63523735  0.38108689  0.18422761  1.16961561 -1.22971947
  2.1932103  -2.27284175  1.13967581  0.23052303  0.25591083 -0.11278533
  0.9600196   0.22135873  1.1900194  -0.45802994 -1.63291885 -0.55965557
  0.41282968  0.89217277 -0.17983563 -0.97882123  1.95145752 -0.20159085
  0.67862784  0.13797813  0.25481313  0.01496777 -0.15771853  0.23906196
  0.09886838 -0.77131333 -0.95156905 -0.47740775 -2.54992949 -0.99194385
  1.54857175 -0.48000093  0.07924214  0.87418949 -0.49337261 -0.41981786
  1.38801635  0.91040288  1.37458698  1.64799931 -0.77015556  1.03692858
  0.65724022  0.05805088  1.75680704  0.34456878  0.56997313  0.56421219
  0.90074231 -0.77335669  0.89086942  0.31387671 -0.7381032   0.42128641
  0.92721484  1.4237665   0.2283053   0.857474    0.48395052  0.75115983
  0.44063459  1.26354988 -2.24716145 -1.6871391  -0.73175891  0.46341525
 -0.08571805 -0.15165508 -0.559989    0.46415519 -1.05203397 -1.31203713
 -2.04811747  0.07409555 -0.91328132  0.64086225  0.438647   -0.14905785
 -0.82706174  1.02239932  0.18860298  0.07345941 -0.18121461 -1.50667847
  0.42460435 -1.44385502 -0.83228321 -1.8571873  -0.66209836 -0.89962019
 -0.15514297  0.63393915  0.41455086  1.01877462  1.54349961  0.313764
 -0.2559313   0.56501287 -0.81486918  0.71777442  0.58919227 -0.15120794
  0.47852733 -1.25133053  0.74407125  0.02920643  0.76199662 -0.27070503
 -1.29333469  1.79983515  0.16063195  0.27698553 -0.47592786 -1.54293343
  0.63122348  1.00525168 -0.92520992  0.22785832  0.16597356  0.35503101
  0.5247295  -1.10063609  0.07539044 -0.69575342 -2.07675632  0.0458412
  0.18041636  0.28938883  0.14022216 -0.46374721  0.70442729  1.12013565
  0.23682993  1.97313778  1.07320216 -0.32399005  0.75332608  1.75629931
 -0.8446286  -0.60420888 -0.47089878  1.41428612 -2.07188329  0.86987506
 -0.14586397 -0.35724257  0.88600951  0.49632385  1.64390662 -0.6857074
  0.2033019  -0.60776641  0.74303641  0.60607188 -0.09708399  0.52642687
  0.14986556  0.24099015 -1.2171928  -0.41635612  0.89420886 -0.79999672
  1.02054619  1.03545625 -0.14748153 -1.44293614 -0.84405441  0.0090272
 -2.01904541  0.4267961  -0.75622271  0.92738137  0.35223498 -1.65800617
  0.24459419  1.04123392 -1.50223646 -1.3091124   0.42538755 -0.65189532
  1.92184337 -0.72861915 -0.07144972 -0.83058565  0.70645011 -0.45007879
 -0.3739981   0.32994091  0.18970272  1.99973326 -0.24893793  0.61359854
  0.45911267 -0.92709275 -0.64934486  0.39376088 -1.06838324 -1.31863459
  0.79598193  0.97878447 -0.57237777 -0.48119406  0.09398684  0.28349066
 -1.23102472 -0.77953292 -1.33782533 -0.35001999 -0.78047138  0.78200878]


");

                #endregion
                #region dump

                numpydumps.Add(@"[ 1.26832786  0.94793083 -0.22878991 -1.90105998 -0.08545618 -0.02618972
 -1.14414653 -0.99266076 -1.62362627 -0.61477097 -0.15851096 -0.34729519
 -1.54475758  1.35507219 -0.09945957  0.38870215  1.04704518  0.89137674
  0.31245159  0.15706904 -0.61732235  0.70206282 -0.93998276 -1.01140792
 -0.61932233 -0.762919    0.24785567 -0.0926865   2.22346208  1.06574251
  0.85512652  0.15865191 -2.47138456 -2.12238593  1.53390392  0.34635133
  0.44554315  0.15734301  0.7431841  -1.06470989  0.64216625  1.93930457
  2.32301854 -1.48782821  0.00368995  0.20140675  1.84425644 -0.67646804
  1.45001614  1.15974747  2.46975465 -0.24409313 -1.18414276  0.31678318
  0.6879701  -3.44844433 -0.53006328 -0.0695227  -0.1477092   0.92961915
  2.12843343  0.46588352  1.02968345  1.49353395 -0.02603015 -0.41035335
  0.26678934 -0.63152232  0.23313256 -0.76411211 -0.28149321  0.46324206
 -1.00633884  0.73473953 -0.78965671 -0.88651035  0.51933789 -0.47256481
  0.78435376 -1.12088529  1.00639136  1.44238677 -1.12277502  1.45159328
 -1.13547173 -0.61246079  1.91526569 -1.05971346 -1.81834408 -0.49601838
 -1.10314875  2.06717063 -1.43043404 -0.11569062 -1.30865847 -0.04431027
 -1.92704769  0.15819275 -0.1616143   1.25060038  0.31203258 -0.24876215
  0.37550787  1.04736101  0.57942409  0.13889342 -0.22787414  0.75537474
  1.55509331 -0.50816726  2.06640461  1.46542137  0.92501281 -0.50530471
  0.00600102 -0.07689935  0.23173618 -0.51229349 -0.35543686 -0.17983081
  0.29964181 -1.2897014  -0.51571669  0.36248044  0.84852445 -1.3665577
  0.64831769  1.67380317  0.49581581  2.29705104  0.42892698  0.14359976
  0.11478026 -1.42435712 -0.9815384   0.52871792 -1.16114829 -0.02163255
  1.22150593 -2.07689721  1.3139897  -0.40881537 -0.95359971 -0.22199285
  0.86961662  0.05558192  1.84266765 -0.55939614 -0.11104753 -0.56050582
 -0.58811017  1.69379953  0.65024428 -0.34250024  1.10076167  0.66607919
 -0.5974441   1.35506421 -0.03015206  0.1628228   0.18557069  1.10017862
 -0.96659328 -0.04267613  0.26807862  0.28230819  1.71410327 -1.97174316
 -0.17875025 -0.80897935 -0.20908065 -0.07037223  0.14573305 -0.10010674
  0.17308301 -0.35082248  0.32678861  0.88828119  1.82790399 -0.88474793
  1.31388037 -0.26087527  0.86418052 -0.06111121  0.75696379  0.11175699
  0.13017766 -0.73136113 -1.79655373 -1.79823571 -1.28518336 -0.47211564
 -0.94374313 -0.14817955  1.97097234  0.19608239 -0.10048317  1.1784223
 -0.67176776  2.04320593 -0.47184874 -1.39304954 -2.28663606 -0.95949543
  3.05683948  0.12902685 -0.97995809  0.05141481  0.51757737  0.14859336
 -0.21460974 -1.53956825  0.067784   -0.70281356  1.09062847 -0.07700298
 -0.44013214  0.09427102  0.87069114  0.81233314 -0.00633257  0.56603114
 -0.4849909   1.78783507  1.13090152 -0.19297674 -0.210199    1.72233146
 -0.47418177 -1.65667226 -0.91211276 -0.36886981 -0.02584507  0.88684565
  0.73779639  1.38319677 -0.44857759  1.35525621 -0.47340421  0.42596663
 -0.77791148  1.67662146  0.87670401  0.50321683 -0.12757321  0.15138577
 -1.27575315  0.53636952 -0.62472663 -0.48094445  0.9562829  -0.30089134
 -0.68081569 -0.18209226  0.12409056  0.43049748 -0.41590817  0.52818549
  0.0900824  -0.9330397   0.66416018 -0.98987039 -1.38741016 -0.19892142
  0.74435635  0.9617323   1.64037758  0.7300313   0.94274855  1.77758711
 -1.15224355 -1.45280502 -0.94121441  0.82039367 -1.6611119   0.70883105
  0.77067936 -0.22838021  0.19823789  1.21963025 -0.24582448  1.23700667
  1.1525921   0.1276455   0.41263771 -0.73951242  1.37501654 -2.36579531
  0.8955888   1.52212128  0.18474271 -1.37141969  1.20460271  0.52480891
 -0.56406255  0.21571507 -1.91809086  2.59249304  1.41912144 -1.2676893
  1.30838248  2.25093778  0.15884362  1.38385034  0.2124822   0.05204451
  1.5923534   0.50218477  0.23071433 -0.30301785 -0.19339661 -0.60490725
  0.215287   -0.98544335 -1.51549754  0.30307156  0.08497268 -0.82761138
 -0.04226464 -0.19172953 -0.09416681 -0.83881077  0.69096049  0.76598974
  1.05112389 -1.68081437  0.9645148  -1.72366777  2.13619377  0.68459159
 -1.22279007  0.17231615 -0.6225316  -0.39696981 -0.56644029 -1.86295876
  0.31347487  0.20156001 -1.93213628 -0.19513838  0.2681181  -0.96213015
  1.79754077 -0.57066439  2.40831263  0.58688046  1.17613212  1.55728263
 -0.6387144  -0.03802358 -0.04060016  0.49951856  0.36817242 -1.46387519
 -0.94525204  0.16474801 -0.33489538  1.00365424 -0.69270386 -0.58520096
 -1.38526159 -0.20572164  0.44406512 -1.20239688 -0.46592442 -1.12775216
 -1.30351786 -0.52368531  1.17951074  1.46255312 -1.11268252  0.52960944
  1.50058698 -0.91015452  0.5144652   0.57035606 -1.29013101  1.36345665
 -2.61702685 -2.16123156 -0.5195441  -0.29502782 -0.11741861 -1.44276922
  0.56762271 -0.43775205  1.80308124 -0.50448763  0.02296629  0.29524757
  1.09548705  0.05232204 -0.93788422 -0.11708143  1.06185264 -0.34007586
 -0.92623773  1.20314875 -0.00882399 -0.32238426 -0.4225791   1.39838437
 -1.61351688  2.31742573 -0.32809475 -0.72298643  0.24865422 -1.7502603
  0.5903963  -0.27734548  1.35736047  1.39380398  0.14077501 -0.84916667
 -0.41251247 -0.50581462  0.2445147  -1.25288091  2.29442942  0.74721946
  0.40018554 -0.17572247 -1.19394477  1.62877027 -1.2678177  -0.67588362
 -0.54381953 -0.13869971  0.04667228 -0.47603851  0.67313798  0.02544008
  1.19028922  0.68970176  0.78351233 -0.08113522  1.06253193  0.24659615
  0.19746989 -0.84741061  0.30035461  0.24915395 -0.58811389 -1.3285454
  0.13869832 -0.52828289  1.49449073  0.76557679  0.28829695 -0.74527037
 -1.13218627 -0.54952227  0.34374556 -0.10962066 -0.04232252  0.57553136
 -0.24899627 -0.13607325  0.37355193 -0.35924012 -0.12263529 -0.30777595
 -0.01783612 -1.14799459  0.88074639  0.64222595  0.15607946 -0.11844208
  0.88100571  0.68407668  0.76332294 -0.2475562  -1.54493987 -0.58269056
 -2.08766553 -0.4276544   0.29800191 -0.43857586  0.5117749  -0.89743623
  0.31294153 -2.2595398  -0.64888194 -0.40247641 -0.06655434 -0.79489652
 -0.22046114  0.09824448  1.16088306 -1.21675942  1.67347053  0.48823513
 -0.40785524 -0.45705876 -0.17710941 -0.84205198  0.14471648  1.0905697
  0.07753238  1.77356717  0.13165272  0.32942431  0.74592086 -1.24845796
  1.14206912  0.32893127  0.03877766 -1.85899603 -1.1557769   1.27381909
 -0.67636666  0.66976667  0.39241131 -1.38605888  1.07170888  1.54046852
 -1.2346144   1.32881206 -1.73562717  1.00760834  0.72839793 -0.29433574
  0.27054728 -1.07234877  0.28256112  0.93753805 -0.11325563  0.52937575
  0.46911378 -0.27560874 -1.57981462  0.36757333 -0.4237466   0.82619254
  0.26360246 -0.51224032 -1.03136113 -0.40141634 -0.25715908 -0.07565326
 -0.2344382   2.52553468  1.36226646  1.18663972  0.0865937  -2.06951002
 -0.42577115 -1.03959353 -0.5637889   0.23281847 -0.20170831  0.19605854
  0.7858351   0.56849551  0.17451531 -0.96854429  1.71348427 -0.56204794
  1.86509225  0.41499682 -1.11380248  0.17307257 -0.3060857  -0.65258814
 -1.8534302  -0.70398256  0.52217023 -1.11115067  1.47102498  1.47884845
  0.27882153 -0.77312419  0.79501044 -0.34020973 -0.18469618  0.47967815
 -0.70739933  1.44257395  0.93633376 -1.03547076 -1.7268527   0.16703234
 -0.71875885 -0.89218978  2.11921994  1.41074149  0.636396   -0.14192765
 -0.19744497  2.96830866  1.17678747  0.13555415 -0.36590682  1.23909043
 -1.31147633 -1.13383377 -0.09984366  1.63310797 -1.35221871  0.36201956]


");

                #endregion
                #region dump

                numpydumps.Add(@"[-8.85749648e-01  1.16586850e+00 -5.44979317e-01  5.02477196e-01
 -4.80210260e-03 -9.57983227e-01  4.21583902e-01 -5.00883325e-01
  1.09384054e+00  4.25410776e-01 -1.83739635e+00  3.10400388e-01
  9.27120981e-01 -3.56751997e-01 -9.66923606e-03 -1.78656385e-01
 -3.25859351e-01 -1.99919278e+00 -1.58443656e+00  5.81447745e-01
  6.43876246e-01 -1.23832751e-01  2.08947234e+00 -1.41191565e+00
  1.21723455e+00 -2.14097062e-01  8.40846223e-01  8.14019172e-01
  1.95215245e+00  6.93280313e-01 -5.56994731e-01  3.72950345e-01
 -1.47119873e-01  1.16601822e+00  1.40739725e+00  5.14065807e-01
 -1.14935414e+00 -4.09281336e-01  3.06686197e-01 -6.57939602e-01
 -4.14886671e-01  2.27484528e+00 -8.83867458e-01 -5.92880461e-01
  1.34761191e+00 -8.16765942e-01  4.49401647e-01  1.43880633e+00
 -4.45478999e-01  1.10945973e+00 -1.95464206e-01  2.01824550e-01
  6.37886880e-01 -9.84314889e-01 -6.12764540e-01 -2.18639378e-01
  8.70522435e-01 -1.84387087e+00  1.41098248e+00 -6.36298358e-01
 -4.72145062e-01  1.07648558e+00 -5.24784619e-01  6.95270969e-01
  2.00943629e+00  7.28684641e-01 -4.96642302e-01  7.99261761e-01
  1.43971045e+00 -3.40469773e-01  3.85777320e-01 -6.56920018e-01
  6.14042765e-01  3.06473781e-01  3.57443128e-01  6.43400117e-01
 -1.10205806e+00 -1.77049681e+00  1.51622497e-01 -2.97605836e-01
 -3.26157445e-01 -6.07145747e-01 -4.38620153e-01  1.21474526e+00
 -2.29867433e+00 -1.12197811e+00  5.64766974e-02 -3.08689351e-01
  1.38524720e+00  8.84471980e-01 -6.25595940e-01 -1.39545714e-01
  2.77304045e-01  1.50461491e+00  1.44380405e+00 -8.00199693e-01
 -1.63670367e+00 -8.64940371e-02 -5.07330990e-01 -1.18685728e+00
 -3.61787130e-01  7.79077517e-01 -1.49017022e+00 -1.98912673e+00
 -4.76680697e-02  9.88164426e-01  1.42904782e+00  2.24337256e-01
  3.85223347e-01 -1.04415039e+00  2.28281837e-01 -2.88241039e-01
 -3.82148764e-01  8.47820659e-01 -2.32631651e-01 -8.19081553e-01
 -1.20233865e+00  6.12812290e-01  1.81630885e-01  9.66432068e-02
 -1.49466547e+00 -9.36263254e-01 -3.79621604e-01 -1.07980373e+00
 -1.68751262e+00 -7.77877095e-01  3.39913792e-01  2.30616022e-03
  4.00348392e-01 -1.73440073e+00 -1.64185583e+00  2.30837062e-01
  1.52857775e+00 -5.60632200e-01  7.42203099e-01 -5.93334937e-01
 -9.89961907e-01  6.34021632e-01  6.94955378e-01 -4.69463680e-01
 -1.10798893e+00  8.02018844e-01 -1.43188368e-01 -8.36072983e-01
  1.00371976e+00 -5.11743502e-01 -1.43871665e+00  2.21829471e+00
 -1.23491421e+00 -1.01582961e+00 -5.83031581e-01  7.22590107e-01
 -7.39927587e-01 -6.09492619e-01 -3.88037447e-01 -1.23815378e+00
 -4.46984184e-01 -2.10207044e-01 -2.47298340e+00 -6.52325931e-01
 -1.41167768e+00  1.65044964e-02 -9.40687975e-01  4.44239760e-01
 -1.50450855e+00  9.09971747e-01 -6.35386421e-01  9.75843925e-01
  9.51089730e-01 -3.36521328e-01 -1.94471465e+00 -4.28464242e-01
 -1.50567720e+00  6.41356919e-01 -7.33730766e-01 -9.53059327e-01
  1.07465178e-01  1.24234316e+00 -1.20030630e+00 -1.22622162e+00
 -8.52075254e-02 -3.63280844e-01 -2.32413756e+00 -2.09293380e+00
 -2.30237320e-03 -8.76336884e-01 -5.99702450e-01 -2.32068071e-01
  8.31118075e-01  2.02015241e-01  9.01775893e-02 -8.90668727e-01
 -2.25336060e-01 -3.11156592e-01  5.50917014e-01  7.26247934e-01
 -1.83732160e-01 -1.26962518e-01  1.56448075e+00  6.12259851e-01
 -1.19013485e+00 -1.55493176e+00  7.22605261e-01 -6.27150180e-01
 -3.37989387e-02 -1.49007335e+00 -4.26374870e-01  1.80166968e-01
  4.85213080e-01  4.38050867e-01  1.22904971e-01 -3.60922009e-01
  3.47310753e-01  1.39945347e+00 -4.95956232e-01 -2.14053477e+00
 -4.76542659e-01 -5.30700493e-01 -1.89344832e-02  1.96055306e+00
  1.20432213e+00 -1.11474489e+00  2.23934621e-02  1.80411962e+00
 -1.94230571e+00  1.85633354e+00  5.82101107e-01  1.09410716e+00
 -4.53176653e-01  8.23786594e-01 -1.95198873e+00  2.30579038e+00
  1.69259068e+00  2.31301872e-01  1.59303940e+00  8.20025687e-01
 -1.30698205e+00 -9.88964932e-01  3.61366087e-02 -1.38365363e+00
  1.77153087e+00  1.19963779e-01  3.36152911e-01 -6.15934239e-01
  6.21884556e-01 -1.12189849e+00 -1.38810166e+00 -9.05825684e-01
 -6.77566983e-01 -5.50525317e-01 -7.69018273e-02  4.68724333e-01
 -1.71704003e-01  1.77986953e-01  9.90814829e-01 -1.13239178e-01
  1.62860295e-01 -1.19777261e+00  9.62395464e-02  9.15098601e-01
 -1.47370490e+00  5.43980414e-01  2.06285338e-02 -1.93451620e-01
  1.83545227e+00 -5.90156518e-01  7.62769518e-01  8.13463044e-01
  3.97793435e-02  1.96337090e+00 -1.58478318e-01 -1.33759174e+00
 -9.69835404e-01  8.71323352e-01  5.72662759e-01  4.14546007e-01
 -3.08583087e-01  1.44749316e+00 -3.90157036e-01  3.16603518e-01
 -4.61754454e-01  3.56980959e-01 -1.52557110e+00 -7.42606581e-01
  6.11068281e-02  2.07438080e+00  1.19730612e+00  6.53534220e-01
  3.95985187e-01  6.55685882e-01 -3.74725348e-01 -6.10125553e-01
  5.87952797e-01  7.12114761e-01 -1.07252623e+00 -1.28845384e+00
 -3.22615169e-01  1.25182269e+00  1.35452334e+00 -3.87333004e-01
  1.55159686e+00  8.68558388e-01  1.25417669e+00 -1.57209142e-01
 -2.29069454e+00  2.60412340e-01 -1.02404050e+00  3.91125938e-01
  2.69513454e-01  3.64614198e-03  1.20202232e-01 -1.17268584e+00
  3.83605820e-02  1.48038843e+00  3.40320402e-01  1.19135627e+00
 -2.03120301e-01  3.73826961e-01  1.78683428e+00  1.21907922e+00
 -3.11314557e-01 -1.33619063e+00  6.62175274e-02 -1.98298184e-02
  4.25899823e-02 -3.18795446e-01 -1.09913727e+00  1.46516773e+00
  1.27106571e+00 -1.43224564e-01 -5.26891243e-01 -2.09012832e+00
 -4.06030601e-01  1.11391847e+00 -1.53775772e+00  2.87107631e-01
  7.48372522e-02  1.03509864e+00  4.14700510e-01 -1.71946264e+00
  5.52010668e-01 -3.96774779e-01 -2.12283764e+00 -2.48420588e-01
 -2.98712509e-01  9.01044391e-01 -1.65001969e+00  6.14147293e-01
 -1.28325480e-01 -2.43738960e+00  2.30044049e-02  3.57860087e-01
 -1.48327470e+00  8.17810419e-01 -8.64506325e-01 -9.83439575e-01
  4.52506815e-01 -2.71015225e-01  5.90527655e-01 -2.50729165e-01
  5.89019963e-01  4.08342845e-01 -1.62428145e-01  2.88666775e-01
  6.39158235e-01  1.52695047e+00 -1.70049428e+00 -5.63195146e-01
 -5.77438539e-01  4.29085527e-01 -1.59079257e+00 -5.40839898e-01
 -3.26281411e-02  3.71461916e-01  1.80806460e+00 -7.53355523e-01
  1.03822183e+00  1.34874574e+00 -9.98974605e-01  1.42200644e+00
  1.02305604e-01  3.04796557e-01 -9.36204202e-01  1.41923841e-01
  1.07991936e+00 -7.25057280e-02 -5.01167221e-02 -2.35430139e+00
 -9.54051994e-01 -5.44181037e-01 -2.07352522e-01 -3.94517296e-01
  1.28089734e+00 -5.67515079e-01 -6.43648494e-02  1.81008963e-01
 -7.83842138e-01  3.08897192e-01 -3.02179556e-01 -4.35274578e-01
 -3.96441323e-01  6.39067424e-01 -4.23209012e-01  2.59471861e-01
  1.05001749e+00 -1.95768043e-01 -9.80215241e-01 -2.70558573e+00
 -1.77826920e+00 -1.23746725e+00  2.84931289e+00 -1.09717488e+00
  1.85678476e-01  5.81272893e-01 -2.61504367e+00 -3.29936854e-01
  7.39971642e-01 -1.77804394e+00 -1.30606072e-01  3.57199381e-01
  1.51098367e+00  2.33459302e-01 -1.31431877e-01 -9.52923604e-01
  2.29908296e+00 -1.15006214e+00  4.34225252e-01  5.54185428e-01
  9.50026262e-01  3.81629602e-02 -4.90896555e-02 -1.05931869e+00
 -2.71980570e-01  2.53122168e-01  1.49616216e+00  7.68172252e-01
 -1.12229064e+00  1.50687848e+00  1.73960645e-01 -5.41691105e-01
  9.25071185e-01  4.06912501e-02  1.13516252e+00 -7.71915270e-01
 -1.13420219e+00  2.26125765e+00  1.14730864e+00 -6.92184568e-01
 -2.04106865e+00  5.18384657e-01 -3.47708752e-01 -9.79976355e-01
 -1.28997992e+00 -5.80656067e-01  1.53607996e-01  1.25597427e+00
 -2.22262636e+00  2.50028389e-02 -6.62351275e-01 -1.25801203e+00
  2.92950249e-01 -6.08444129e-02  1.69850748e+00  1.02001336e+00
 -2.56952820e-02  4.88330646e-01 -1.40652091e+00  3.91354604e-01
 -3.12503704e-01  4.38376179e-02  1.46496744e+00 -5.30721562e-01
 -1.88597613e+00 -4.84140180e-01 -1.07166685e+00  8.00438958e-01
  1.92579898e-01 -1.18111917e+00 -7.80311368e-01  8.70384062e-01
  5.45131568e-01 -9.05909577e-02 -5.51397617e-01  5.81914977e-01
  4.70429383e-01  8.44093723e-02  7.02624768e-01 -6.89876494e-01
 -4.31959627e-01 -9.68100598e-02  1.79978918e+00  4.79555919e-01
  5.11899265e-01 -2.81392802e-01 -1.87872496e+00  1.52459606e+00
 -6.97472827e-01  1.60627304e-02  1.08405997e+00 -5.99509890e-01
  3.23491885e-02 -3.26992201e-01 -2.53771938e-01  1.50728650e-01
 -8.08283852e-01 -6.49573859e-01  5.05378914e-01  6.58369297e-02
 -7.46513052e-01  7.90973064e-01  6.00612286e-01  7.52902199e-01
  5.42561655e-01 -1.29965802e+00  6.65649937e-02 -1.19782312e+00
 -1.50616144e+00  6.76842883e-01  1.26281506e-01  1.04662138e+00
  1.24386054e-01  1.39951895e+00  7.23273450e-01 -2.96156000e-01
  1.14107448e+00  1.07192241e-01  1.66863996e-01 -1.00985397e+00
  5.96754197e-01  7.25832643e-01  1.19682282e-01 -3.77091366e-01
 -3.31704190e-01 -5.55769828e-01  1.33310410e+00  1.23030306e+00
 -6.33066236e-01 -7.44399477e-01  1.32249310e+00  9.22119341e-01
 -1.04338784e+00 -1.05601629e-01 -2.02210065e-01 -8.16121794e-01
 -7.68584925e-02  2.86737925e-01 -3.56262408e-01  6.01269149e-01
 -7.63295536e-02 -6.70856436e-01  1.08948109e+00 -1.26513361e+00
  1.65715503e+00 -2.57181893e-01 -1.18722637e+00  1.08757750e+00
 -2.52570377e-01 -2.64105711e-01  3.07556951e-01 -2.08550026e+00
  1.12422336e+00 -1.55284819e+00  1.77833364e+00 -8.16228033e-02
 -8.04024620e-01 -7.53081716e-01  7.21493679e-01  1.68205606e+00
 -2.59151098e-01 -3.16601969e+00  2.37287802e+00  1.27913632e+00
  1.24793494e+00 -1.17746160e+00  1.25092197e+00 -2.58038685e-01
  1.70470774e+00 -5.11939681e-02  7.77774919e-01  7.67959166e-01
 -1.07486198e+00  5.17176171e-01 -4.27089022e-01 -2.30016162e-01
 -3.55590617e-01 -1.06412575e+00  5.43515299e-01 -1.34698569e+00
 -4.57452431e-01 -4.29341140e-01 -1.28914978e-01  6.29283830e-01
 -7.22934331e-01 -1.05585259e+00  6.16757024e-01  1.25481858e+00
 -9.65106518e-01  9.64774010e-01  3.73106699e-01 -1.64553786e+00]


");

                #endregion
                #region dump

                numpydumps.Add(@"[-2.42795890e+00  1.54633020e+00 -9.16424303e-01  3.34634113e-01
  4.00203148e-01  5.28765539e-01  1.97694102e+00  5.16431412e-01
 -2.47200978e+00  1.35906173e+00 -8.14009583e-01 -9.52252326e-01
  9.72579294e-01 -8.91932738e-01  4.92413732e-01  1.04095943e+00
 -9.36861727e-01  8.15109617e-01 -9.18020963e-01 -9.47845202e-01
  1.98508078e-01  9.19625049e-01 -1.11023965e+00  5.20190401e-01
  2.48128565e-01 -9.68039527e-01  1.02071483e+00 -3.92749942e-01
  2.75227783e-01 -1.14697457e+00  5.51608231e-01 -6.38840989e-01
 -3.52993458e-01  4.15647968e-01  9.90656278e-01 -9.02643183e-01
  1.44616038e+00 -4.48897568e-01  1.16377338e+00 -4.92905310e-01
 -6.97670889e-01 -1.12422670e+00  4.01874007e-01 -1.51208815e+00
  6.02069008e-01  2.29929562e+00  5.32154642e-01 -9.21689864e-01
  9.38328780e-02 -1.20287717e+00  8.71593799e-01  4.20542299e-01
  1.55380046e+00  1.98202645e-02 -4.13864259e-01 -1.09381641e+00
 -1.09809952e+00 -1.27161221e+00  5.73881479e-01 -1.61730073e+00
  2.63941216e+00  3.05366161e+00 -3.84773691e-01 -7.38592721e-01
  7.95989450e-01  2.47198144e-01  1.51786293e+00  2.03531698e-01
 -5.07686705e-02  1.60090994e-01  1.14344200e+00  1.12340824e+00
 -3.94858020e-01 -4.57730494e-01 -1.64512519e+00 -1.63234730e+00
 -1.24350347e+00  1.51093088e+00  9.15941381e-01  3.05959186e-01
 -6.30411856e-01 -5.70736889e-01 -2.07358399e-01  8.53474559e-01
  1.83424229e+00  2.28156791e-01  4.02044315e-01  1.50996001e+00
  1.54403920e+00 -7.01179704e-02 -3.15859723e-01 -1.19100919e-01
  1.91776653e+00  7.95073435e-01  1.65222378e+00 -1.41859926e+00
 -8.57755157e-02 -5.46730600e-01  4.47590002e-01  3.32071748e-01
  2.91217915e-01 -4.60350215e-01 -7.10917082e-01  1.44564572e+00
 -3.51691238e-01 -4.06641313e-01  2.62917582e-01 -5.17723046e-01
  1.30685946e+00 -5.58933586e-01 -7.02170507e-01 -7.24676176e-01
 -1.55049742e+00 -9.41829860e-01 -1.03166525e+00  1.52151454e+00
 -1.10897092e+00  1.81055880e+00 -8.60557078e-01 -1.15875329e+00
 -2.90027239e-01  2.06657198e-01 -1.58254558e+00  4.82748069e-01
  9.95212419e-01  6.65392179e-01 -1.89190581e-01  6.73829864e-01
 -8.11541098e-01 -9.50240254e-01  7.30417036e-01  6.79665412e-02
  7.83728915e-01  1.12799374e+00  6.62575371e-01  9.63884484e-02
 -5.95624578e-01  2.71364358e-01 -6.84250033e-01 -6.16085912e-01
  2.54624091e-01 -1.26894979e-01 -4.03263607e-01  1.62101920e+00
 -2.11523718e-01  1.96652021e+00 -1.27917092e-01  2.69193504e+00
 -1.40892381e+00  2.83194486e-01  4.87188775e-01 -6.05917225e-01
  5.18979902e-01  2.25912240e-01 -1.32451764e+00  6.59414254e-01
 -7.17064996e-01  5.23589047e-01  1.52601193e+00 -8.99485935e-01
 -2.66619008e-01  2.06009721e-01  1.19918294e+00  4.09044170e-01
 -2.18367692e-01  5.19230965e-01 -1.12191494e+00 -5.70928928e-01
  6.26102024e-01  5.92648574e-02 -1.72984748e+00 -6.52590098e-01
 -4.76610703e-02  3.24427517e-01  7.11286070e-02 -3.68339849e-01
 -1.35299636e-01  2.19760426e+00  7.01200482e-01 -8.20830076e-01
  2.80183652e-01 -1.00746924e-01 -1.40609821e+00  1.73008506e+00
  8.23276489e-01  6.57645946e-01  1.69479888e+00  2.21158068e-01
  3.67278191e-02  5.47015898e-01  1.15154577e+00  1.36512314e+00
 -8.85723315e-01 -4.74650052e-01 -8.92891257e-01  1.99024752e-02
  5.68619424e-01 -2.26844077e-01 -5.31169405e-01  1.64723813e+00
 -2.37506422e-03 -7.88523355e-01  6.11808516e-01 -4.28874532e-01
  1.79950899e+00  7.85105714e-01 -1.05277282e-01 -2.98332477e-01
 -1.45061364e+00 -1.48178057e-01 -1.21982991e+00 -1.13923307e+00
  6.01628771e-01 -5.54625672e-01 -2.47981571e-01  1.11744461e+00
  1.20686053e+00 -6.19630869e-01  1.01695563e+00 -1.41275931e+00
 -5.50858525e-01 -1.36153382e+00  1.35780871e-01  3.67926279e-02
 -6.85421183e-01  1.17435809e+00 -1.60112516e+00 -1.04221948e+00
 -1.18127731e+00  5.12653058e-01 -1.81976334e-02 -1.01743338e+00
 -7.84179385e-01  6.25155740e-01  1.48555577e+00  1.27120592e+00
 -2.96647452e-01  6.64059005e-01 -5.42308797e-01  4.67601504e-01
  7.57954704e-01 -1.46596641e+00  9.14264944e-01  6.25004668e-01
  6.36484376e-01  3.95817114e-01  3.96797632e-01 -1.08552251e+00
  7.19387096e-01 -2.58104783e-01  4.75339023e-01 -7.83446798e-01
  1.12140739e-01  3.11627739e-02 -8.76098690e-01  1.17910012e+00
 -3.72969289e-01 -1.77125527e-01  4.23229745e-01  9.32463576e-02
 -1.83507703e+00 -5.54895524e-01 -1.35131441e-01 -1.55726443e+00
  6.75492014e-01 -1.16539562e+00 -3.43376845e-01 -2.44940649e+00
 -6.93738153e-01  5.32517017e-01  2.15262834e+00  5.11142715e-01
  2.99531122e+00 -4.42421807e-01  1.14895508e+00  1.09251908e+00
 -4.82073597e-01 -6.82739451e-01  5.52713106e-01  1.17038336e+00
  2.15524633e+00 -5.22833407e-01 -2.14907963e-01 -2.01191840e+00
 -8.19961546e-01 -2.14074770e+00 -2.35256613e-02  6.32976328e-01
 -8.50217052e-01 -6.54316739e-01 -5.57269211e-01  1.87615119e+00
 -3.96740908e-01 -1.38868946e+00  1.34524870e+00 -1.75255449e+00
  3.51753634e-01  2.00492787e+00  1.29392337e+00  3.53487111e-01
  1.53250326e+00 -3.50587615e-01  3.53704426e-01  1.81168666e+00
 -1.80403635e-01 -5.35173624e-01  2.25193409e-01  2.41665278e-01
 -6.10451808e-02  3.27796781e-01  9.78921035e-01 -1.40425378e-01
  2.48590341e+00 -3.09485370e-01  2.44627339e-01  2.14648378e+00
 -3.23630392e+00  7.15049181e-01  1.63735141e-01  2.33865814e-01
  5.54527157e-01 -3.20867781e-01  9.40879825e-01 -2.24642514e-01
  3.52799414e-01  1.39721668e+00 -5.88040242e-01  1.14395491e+00
 -1.34011623e-01 -3.88918748e-01  3.75043245e-01  8.00025265e-01
 -3.85633970e-01 -3.76417399e-01 -1.10056371e+00 -2.24292598e-01
  8.30420889e-01  2.63327742e-01 -6.80164385e-01 -7.80689945e-01
  5.85275688e-01 -5.93862435e-01 -2.03316384e-01 -1.36241805e-01
  8.72486569e-01  1.95547442e-02 -5.25953812e-01 -1.60276311e+00
  1.66742340e+00 -5.07570985e-01  7.43018467e-01  1.40501428e+00
 -2.16571574e-01 -1.88371724e+00  1.04089213e+00 -4.82983225e-01
  2.89610199e-01 -2.13554829e-01  8.34384450e-01  1.66389296e+00
  1.31900339e+00  6.28743110e-01 -1.71783073e+00 -1.82566951e+00
  1.72480611e-01 -1.98841128e-01  2.52181605e-01  1.23236606e+00
 -1.37645789e+00 -8.47370524e-01 -3.13680448e-01  2.11058904e-01
  6.08912469e-01 -1.35191010e+00  1.61411356e+00 -2.53654086e-01
  6.52696852e-01 -1.25302019e-01  2.91365864e+00  2.62980790e+00
  1.50602561e+00  1.71457547e+00  8.12169026e-02  3.89445127e-01
  6.66691318e-01  1.56601209e-01  1.73069583e-01 -5.64810262e-01
  7.52678196e-01  1.16218195e+00  2.35023741e-01  8.98212721e-01
  5.13476252e-01 -4.88252154e-01 -2.03025135e-02 -1.05425257e+00
  7.10160063e-01 -3.73871901e-01 -2.03894066e-01  3.20587405e-01
 -8.65983252e-01 -2.58849717e-01 -9.82543723e-01 -2.33064080e+00
 -2.04560700e+00 -2.07731857e+00  1.63206407e-01  1.25916553e-02
  1.00238318e+00 -2.98556738e-02 -1.06769253e+00  3.42317509e-01
  1.69850693e+00 -3.01007563e-01 -6.82852046e-01 -4.82265767e-01
  5.84596350e-02 -9.89226200e-02 -4.81712457e-01 -1.09972086e+00
  5.91450259e-02 -5.25692296e-01  3.72794081e-01 -1.54803954e+00
  2.18207060e-01 -8.05861782e-01  1.43582203e+00 -1.13398441e+00
 -4.67179160e-01 -5.02399961e-01  6.64660975e-01 -5.69862850e-02
  5.15386363e-01  3.73081924e-01 -1.30423294e+00 -7.23627399e-01
 -1.30853058e-01 -1.22625898e+00 -2.75293901e-01  2.13509149e-01
  1.61292600e+00  4.76129471e-01  3.17787667e-01 -5.79532308e-01
 -6.44971123e-01 -8.80156921e-01 -1.02521709e+00 -2.54814458e-01
  4.66250435e-01 -9.03189339e-01 -3.85930565e-01  7.03340410e-01
  6.91112876e-01  5.14030997e-01  4.21070216e-01  2.62520472e-01
 -1.70114804e+00 -8.84112582e-01  6.37855637e-01 -6.29546628e-01
  4.87443064e-01 -1.38907284e-01  5.24321347e-01 -1.04293657e+00
 -7.27422104e-01  7.73351909e-01  4.60834473e-02 -1.74596671e+00
  3.53572490e-01 -7.74936099e-01 -9.73668699e-01 -4.47695572e-01
  4.96086974e-01 -2.35311715e+00 -2.57792270e-01 -8.62415569e-01
  5.69757560e-01  2.44884267e+00 -5.53807304e-03 -4.61954211e-01
 -1.55519241e+00  6.37350346e-01 -1.53356383e-01 -6.13144519e-01
  1.71662892e+00  1.10609705e+00 -9.14959234e-01 -1.27275656e+00
  8.03261308e-01  9.18737188e-01  3.12139711e-01 -5.55660722e-01
  4.84060975e-01 -1.87202039e-01  1.66914643e+00  1.06482847e-01
 -1.75972999e+00 -4.89999397e-01 -1.44265365e-01  2.06506821e-01
 -1.75135864e-01  8.80671025e-01  3.98283550e-01  4.97678949e-02
 -1.55721944e+00 -9.86166756e-01  5.02690167e-01 -3.19113941e-01
 -9.18963589e-01  3.64295974e-01  1.03692793e+00  1.57740691e+00
  7.94180784e-01 -9.79882657e-01 -1.29352784e+00 -7.15111012e-01
 -1.73059572e-01  9.98692999e-01 -7.49616027e-01 -4.13069742e-02
  1.61912289e+00 -1.63721900e+00  4.31411878e-01  4.03783558e-01
  6.68241438e-01  1.88428951e+00 -1.87381771e+00  9.28730991e-01
 -2.95340990e+00 -9.45228502e-01 -9.44168950e-02  1.08332090e-02
 -5.21843213e-01  2.65179428e-01 -6.15305329e-01 -1.49505008e+00
 -7.72316719e-01  2.40600993e-01 -7.33100685e-01 -1.67847461e+00
  1.74213307e+00 -1.30137375e+00 -4.30373735e-01 -2.08484490e-01
  2.47046628e+00  2.59793244e-01  3.85308985e-01 -1.48170222e+00
 -2.74833891e-01  8.47519333e-01 -8.62334683e-01  2.15414790e+00
 -1.09824574e-01 -1.84275357e+00 -8.79266463e-01  4.80867154e-01
  1.19348006e-01 -2.34930051e+00 -4.37756021e-01  2.82586657e-01
  2.37864223e+00  2.48443290e-01  1.56203281e+00  1.16047675e+00
  2.88268301e-01 -8.03858828e-01 -1.60227883e+00  3.38114931e-02
  9.45152516e-01 -3.69662677e-01  8.26452334e-02  6.95824835e-01
  6.37460361e-01 -1.30487947e+00 -4.22649840e-01  2.98740877e-02
 -1.99996105e+00  2.99356320e-01 -5.69636099e-01  1.30940551e+00
  1.63141832e+00  1.12109765e+00 -1.18437229e+00  1.81858122e+00
  1.18086063e+00 -4.79648229e-01  1.50391387e+00  1.47582970e+00
 -1.76651906e+00  9.38760450e-01 -6.97601474e-02  1.08558071e+00
 -6.46408820e-01 -4.75613721e-01  5.68878176e-02  4.27663870e-01
  7.80326776e-02 -1.14284836e+00 -6.36544922e-01 -7.56280861e-01]


");

                #endregion
                #region dump

                numpydumps.Add(@"[ 4.64913364e-01 -1.06710473e+00 -1.51283449e-01 -3.48427754e-01
 -2.25402587e-01 -6.79980365e-01  3.40452997e-01  2.24816846e+00
  1.09072889e+00  4.92375985e-01  1.18304840e+00 -1.43803447e-03
  1.22234118e+00 -5.51450784e-01  1.85204988e-01 -3.76207495e-01
 -4.73444609e-01  8.96549745e-01  1.20673238e-02  7.69795314e-01
  2.23504465e+00  2.71802679e-01  2.21178035e-01 -2.01792588e+00
  8.27417804e-01  1.58175902e+00 -3.58832360e-01  1.53302825e-01
  3.25298649e-01  4.56591738e-01  9.29871337e-02  1.92317828e+00
 -6.26048088e-01  8.20434342e-01  2.09836611e+00 -8.97194951e-02
  2.01360415e+00  2.30693634e-01  1.10298558e+00 -3.01851632e-01
  1.34335960e+00  9.85652309e-01  1.64328868e-01 -1.80119975e+00
 -2.00432633e+00  1.60461140e+00  3.94489885e-01 -3.14900125e-01
  1.16028544e+00  5.63513511e-01  1.21932948e+00 -1.10119898e-01
  6.08817866e-01  1.18606861e+00 -6.98732056e-01  3.97366048e-02
 -1.30742854e-01 -1.15918625e+00 -8.18497622e-01  1.32876906e-01
 -1.89773541e+00  1.04609207e+00 -7.83753481e-01  1.63664552e+00
 -1.85377543e+00 -4.45186360e-01  5.73375432e-01  6.84642165e-01
  9.86734800e-01  5.14673321e-01  8.60681587e-01  7.81442514e-01
 -1.02184717e+00 -1.30409101e+00 -2.44646467e-01 -2.78577640e-01
 -1.92702716e-01  1.56610039e-01 -5.10520552e-02 -5.49766979e-02
  9.32060479e-01  8.61760696e-02 -5.90282563e-01 -6.69206897e-01
 -6.76195809e-01  5.27022071e-02 -1.03380549e+00 -6.85833237e-01
 -7.21746801e-01 -1.33345477e-01  1.21350593e+00 -1.34042111e+00
 -1.38844039e-01 -2.03035711e+00  1.08033275e+00 -3.79224985e-01
  1.50882008e-01  5.08838246e-01 -1.72474833e+00 -5.29344371e-02
  9.62221591e-01  5.92304766e-01  6.81318447e-01 -3.05224763e+00
 -2.12739701e-01 -9.84248250e-01  1.72139799e+00 -1.05689648e+00
  5.50782254e-01 -5.51396554e-03  1.36083344e+00  1.80723289e-01
  1.49179846e+00 -5.13838123e-02 -1.09945025e+00  4.57856227e-01
 -2.69507520e-01 -1.55734016e-01 -6.34139443e-01  3.06243742e-02
 -5.84053713e-01 -8.43640411e-01 -1.06313882e+00 -1.69472083e+00
 -1.78170981e-01 -8.63804142e-02 -3.83383655e-01 -6.60742869e-01
 -3.27628115e-01 -2.26124160e-01 -1.75684815e+00 -8.82398254e-01
  6.04764586e-01  7.36828286e-01 -9.68970088e-01  5.99281465e-01
 -7.15844240e-01  1.09340015e-01 -5.42853866e-01  3.94010237e-01
 -2.24798665e+00  8.74082792e-01 -1.27467506e+00 -6.45086946e-01
  1.09571245e+00  7.05724447e-01  1.61601796e+00  1.76914898e-01
 -5.62026686e-01 -3.60633555e-01 -7.04232206e-01  7.89118144e-01
 -1.27691279e-01 -1.16288228e+00  5.86715079e-01  1.02279939e+00
  1.95372998e-01  2.85311838e-01  3.34472858e-01 -4.39147362e-01
  5.34526233e-01 -9.51526276e-01  8.60351932e-01  8.78147976e-01
 -1.64733912e-01  9.00362238e-01 -1.77951659e+00  5.23119613e-01
  2.78597204e-01  1.18948005e+00 -4.50585260e-01 -8.25191361e-01
 -5.76594114e-01  1.54358492e+00  2.58544305e-01 -7.33788730e-01
 -3.46263504e-01  1.12140613e+00  1.28289317e-01 -3.32181283e-01
 -8.41418029e-02 -3.18223989e-01  1.52185680e+00  9.01710064e-01
 -3.25569452e-01  1.48346027e+00  4.38640238e-01  2.07474442e-01
  1.46317938e+00  1.61159880e+00 -9.16590132e-02  1.48655269e-01
 -2.69332763e+00 -1.51367088e+00  8.03965149e-01 -1.02434858e+00
 -4.82560624e-01 -1.14764697e+00 -1.83539071e+00  5.92461312e-01
  2.99713640e-02  4.72636642e-03  4.21492704e-01 -4.25179603e-01
  2.91476527e-01  3.47705418e-03  1.47574547e+00 -1.78584759e-01
  5.57763304e-01 -9.62894141e-02  2.13981758e-01  8.30010938e-01
  3.04977263e-01  9.41339547e-01  1.97518955e-01  3.40873767e-01
 -2.40465007e-01  6.21975565e-01  1.00823914e+00  2.76810578e+00
  1.60927768e+00 -1.36586211e+00  1.08681696e-01 -4.77527060e-01
 -5.12496558e-01  2.09579767e-01 -2.19032232e-01 -8.80910894e-01
  8.04556181e-01 -1.88191204e+00 -2.16848175e-01 -1.20452010e+00
  2.32641755e-01 -1.45332176e+00 -3.20621659e-01  2.09724678e+00
  2.98532042e-01 -1.49283879e+00 -1.65746852e+00  1.55282371e+00
  4.62762694e-01 -5.34640734e-01  4.39257818e-01 -1.95493744e+00
 -1.90402001e-01 -5.64507457e-01 -6.79958270e-01  1.56030269e+00
 -3

");

                #endregion
                #region dump

                numpydumps.Add(@"[-5.64711907e-01  1.86051016e-01  1.98568728e-01  3.48997580e-01
  1.85356596e+00 -1.04214255e+00  9.73374209e-01  1.09747560e+00
 -1.11591491e+00  3.53116312e-02 -1.67656135e-01  1.39635390e+00
 -1.29650932e+00 -5.75204289e-01  1.31995824e+00 -8.40191790e-01
  8.31442068e-01 -3.57057504e-01 -1.09668375e+00  1.02543550e-01
 -5.23337720e-01 -8.26118078e-01 -1.56270668e+00 -1.63849243e+00
 -1.56942690e-01 -1.22569376e+00  1.43479331e-01 -7.49043314e-01
 -1.27451768e+00 -1.21337623e+00  2.39293423e-03 -5.04990417e-02
  1.72895026e-01 -2.19318642e-01 -1.47402879e+00 -1.69299944e+00
 -4.41257109e-01 -1.80427683e-01  3.14241561e-01  3.10791266e-01
 -1.35402008e+00 -1.98782945e-01  2.02846261e-01  1.23301030e+00
  2.36839811e-01 -1.42589497e+00 -8.49432466e-02  7.77748737e-01
  1.78816479e-01  9.38166476e-01  1.32133172e+00 -1.37938732e+00
 -7.01143911e-01 -1.16903954e+00 -4.07794130e-01  4.77340035e-01
  1.24752182e+00  2.20282057e-01  2.34719272e-02 -1.47972874e+00
  3.20319113e-01 -2.79758715e+00 -4.99569310e-01 -1.87053531e+00
 -1.97229764e+00  2.45318136e-01  5.22859769e-01 -8.37633718e-01
 -1.73327450e+00  2.09568705e+00 -1.55794129e+00 -6.55209188e-01
  5.44054567e-01  2.05312855e+00 -3.33496344e-02 -7.90198488e-01
  1.24299908e+00  2.08262019e-02  1.40068581e+00  2.76058438e+00
  7.28235880e-01  2.05435888e-01 -4.96201991e-01  1.75040457e+00
  8.29164037e-01  1.38129837e+00  6.96951334e-01  3.73241408e-01
  9.95302202e-01  3.55564462e-01  4.69758630e-01  1.56501828e+00
 -1.00182505e+00  1.85113988e-01 -1.04057602e+00  1.13597148e+00
 -1.50927869e+00 -5.03735315e-01 -5.09582955e-01 -3.14024929e-01
  2.05498216e-01 -2.53672081e-01  1.23779926e-01 -4.27750342e-01
 -8.25594621e-01 -1.87802515e-01  8.43583298e-01  1.07509532e+00
  7.89199796e-01 -1.39291352e+00  1.94463188e-01 -2.82923696e+00
  1.50848775e+00  3.81268149e-01 -7.54564601e-01  1.32113419e-01
  1.09958725e+00 -3.46636324e-01 -9.06925202e-01  3.22576244e-02
 -2.12796820e+00  4.18975221e-01  5.64982028e-01 -9.12798875e-01
 -6.05040573e-01 -1.02365577e+00  8.96731254e-01 -8.38717967e-01
  1.48051497e+00  5.76097233e-01  3.25767327e-01  3.44206391e-01
  4.21819291e-02  5.03229563e-01 -1.32065991e-01 -4.47675354e-01
  1.05023329e+00 -1.12038114e+00 -7.93737297e-01  2.11475452e-01
 -9.79000101e-01  7.16488950e-01  3.28561864e-01  7.40034989e-01
  5.18533514e-01 -2.71056909e-01  7.21339897e-01 -8.39824481e-01
 -2.50377986e-02  2.87358847e-01 -5.42688567e-01  7.38765836e-01
  1.10678625e-01 -1.73081366e+00 -1.34218450e+00 -2.20284621e+00
 -1.82336627e+00  6.44009458e-01 -6.18955174e-01  1.46702757e+00
  3.45084522e-01  8.60490509e-01 -2.63285928e-01  3.24411869e-01
  5.21271517e-01  6.01653493e-01 -1.09065991e+00 -5.63391669e-01
 -5.72052174e-01 -1.63808606e+00 -2.67202181e-01 -7.09837263e-01
 -1.97947640e-01  3.50789391e-01 -7.70793482e-02  1.16411673e+00
 -8.13811483e-01  1.13546717e+00 -2.42851743e-01  1.41241305e+00
 -5.96486821e-01  6.48501502e-01 -1.25081441e+00  2.87582538e+00
  1.62902643e+00  5.99059516e-02  3.20191840e-01  5.61907821e-01
 -2.25369982e+00 -8.87056289e-01  7.39584140e-02  1.99124017e-01
 -1.03666637e+00 -5.91517971e-01  2.21203365e+00  7.79672013e-01
 -7.11467818e-01 -1.38609580e+00 -1.75175491e+00  7.59056521e-01
 -7.82234330e-01 -1.02409886e-02  1.10330415e+00  9.02949313e-01
  8.22365489e-01  1.01117895e+00  1.63777777e+00 -1.54360479e+00
 -1.90064583e+00 -8.07168157e-01  3.49114119e-01 -9.06029920e-01
 -2.11094619e-01  9.25219250e-01 -7.57468276e-01 -2.16792266e+00
  3.22550347e-01  6.15317848e-01 -8.35853148e-01  4.73008104e-01
  6.01648397e-01  1.46384797e+00  6.67885894e-02 -2.72991781e+00
  1.06867091e+00  8.62107291e-01 -2.71140547e-01  8.81286148e-01
 -1.39554973e+00 -4.68170853e-01  6.70487128e-01  5.08434414e-01
 -6.13030500e-01  1.84441022e+00  4.86571192e-01  4.22621183e-01
 -7.19541206e-01  1.69512698e+00  9.54508326e-01  2.90389946e-01
 -5.17971247e-01  1.49233244e-01 -1.91458321e+00  5.89979975e-01
 -9.23072013e-01 -3.03542947e-01 -5.42261640e-01 -2.37346182e-01
 -8.62137770e-01 -5.63845983e-01  5.98291493e-01 -4.74460928e-01
  5.89103457e-01 -4.95888595e-01  6.89184080e-01 -1.01112873e+00
 -6.44916864e-02 -1.06804364e+00  3.27293829e-01 -4.99709795e-01
  4.43557846e-01 -2.70809803e+00  6.36599829e-01  1.79053276e-01
  9.31746480e-01 -1.53978920e+00  1.86346370e-01 -2.42141782e+00
  1.76793150e+00 -7.28146552e-01 -2.22605331e+00 -2.75598210e-01
 -8.38950575e-01  1.04060739e+00  5.50481004e-01 -6.77418527e-01
 -3.88992838e-01  5.28683345e-01  4.04397095e-01 -1.27808512e+00
 -1.15660077e+00 -1.06611377e+00 -1.63439671e+00 -2.59357528e-01
  2.97783292e+00  1.53650065e+00  5.10250986e-01  4.37104120e-01
 -6.81230128e-03  5.96918031e-01  4.73323773e-01 -6.58710623e-01
 -1.12594253e+00  1.77223162e+00 -9.51020729e-01  4.56263731e-01
  3.33915989e-01  1.03591653e+00 -9.32126900e-02 -1.19263193e+00
 -1.07926658e+00 -1.19845631e+00 -7.63802159e-01 -2.23022646e+00
  1.25052582e+00  1.27863651e+00  8.57541703e-01  3.38846902e-01
  5.53819254e-02  1.68753446e+00 -6.96065141e-01  5.99453024e-02
 -6.58557073e-01 -2.29405050e+00 -7.96617747e-01  2.07401806e+00
  1.20328932e+00 -5.77405472e-02 -3.69948740e-01  7.90755502e-01
  5.91475873e-01 -1.47478693e-01  3.76115266e-02  1.49577268e+00
 -9.25016362e-01 -7.38758210e-01  1.89908820e+00 -9.90902582e-01
  8.32061998e-01 -1.26198365e+00  1.28693071e+00 -1.33133071e+00
 -9.53257367e-01 -5.86778718e-01 -1.29628323e+00 -3.27408501e-01
  7.85627366e-01 -9.40980405e-01 -2.15982749e-01 -1.03202157e+00
  3.24378659e-01 -4.76798980e-01 -7.95798839e-01 -7.04667460e-01
 -1.66847478e-01  6.01356959e-01 -2.07045326e-02 -1.98216062e+00
 -5.00945283e-01 -5.02891285e-01 -5.18425744e-01 -9.91234819e-01
 -2.23357607e+00 -5.38484233e-01 -4.81961038e-01 -1.08868771e+00
  3.36761302e-01  2.68285814e-01  4.53676244e-01 -8.52331937e-02
  1.75144118e+00 -5.35033859e-01  1.31670909e+00 -6.80320948e-01
  6.34450756e-02  1.64139077e+00  4.61363549e-01 -3.99150868e-01
  1.75686010e+00 -6.11039666e-01 -6.76282639e-01 -7.55023791e-01
  1.05962383e+00  2.83233130e-01  5.05168937e-01 -1.26551661e+00
  1.84846194e+00  8.16719983e-03  6.37772274e-01 -7.12574886e-01
 -5.70996182e-01  1.56668116e-01 -1.43709713e-01  2.90023021e-01
 -1.63802209e-01  2.00037837e-01  3.34453637e-01  8.96402016e-01
  2.07796409e+00  4.68093705e-01  2.71069136e-01 -1.45590571e+00
 -4.28747423e-01  1.13003174e+00 -1.96545287e-01 -1.41442367e-01
  2.47389584e+00  2.07031729e+00  4.85705112e-01 -3.34340413e-01
  8.31022568e-01  4.44548690e-01 -2.48451065e-01  2.94833065e-01
  1.33652857e+00  2.05605789e-01  1.57978979e+00  9.76362911e-01
 -7.21397396e-01 -2.52102544e+00 -9.23515115e-01  1.29861198e+00
  3.82131483e-01 -2.09692054e+00 -1.86750453e-02  1.01745083e-02
  1.12217957e-01  4.20782140e-02  5.02681797e-01  1.16353598e+00
  1.23994906e+00  1.49098224e-01 -1.04885095e+00  2.13782696e-01
  5.88760959e-01 -1.01005685e+00  6.77816406e-01 -1.41049594e-01
  5.14805966e-01 -1.71776433e+00  1.15001255e+00  8.68893102e-02
 -3.57023524e-01  1.91458266e-01  4.91813153e-01 -3.98175489e-01
 -1.37569217e+00 -1.13703679e+00 -6.51872677e-02 -1.32653648e+00
  1.41838746e+00 -6.67982733e-01  4.21725581e-01  5.27203651e-01
 -1.71699934e+00  1.03959115e+00 -1.26986095e-01 -6.82283791e-01
  7.43142065e-01 -3.98860314e-01  1.12989311e+00 -1.26004099e+00
  6.15991807e-01 -3.43384432e-01  1.72535032e+00  7.34240468e-01
 -6.43371058e-01  1.15248182e-01  6.09622795e-01  2.10331960e+00
  6.24579972e-01  1.53469691e+00 -7.78134496e-01 -2.52881811e-01
 -1.19612241e+00  5.77734230e-01 -5.71990079e-02  3.67166222e-01
 -1.08471505e-01  1.17089063e+00 -8.64061827e-01 -1.22189257e+00
  6.11595153e-01 -5.20976611e-01  2.49494624e-01 -6.37411413e-01
 -6.02520923e-01 -3.15144699e-01 -4.44061027e-01  5.99398560e-01
  2.19897345e+00  2.84614342e-01  1.56607897e+00 -2.43654432e-01
 -6.81268209e-01 -8.31077411e-01 -1.19297303e+00  9.95313340e-01
 -8.36376309e-01  1.39602082e+00  1.39720056e+00  1.13211552e-01
 -1.01316584e+00 -1.47346446e+00  2.77708771e+00 -3.49294595e-01
 -9.90197343e-01  1.51275798e+00 -5.88167308e-02 -1.40154085e+00
  1.93605550e-01 -7.03578953e-01  6.58057310e-01 -5.89821443e-01
  5.78494091e-01  6.54074258e-01 -1.73589318e+00  5.93416917e-01
  1.20331490e+00 -6.40509117e-02  7.56062603e-01 -1.55280479e+00
  5.48485139e-03  1.31975655e+00 -5.32131658e-01  1.91849699e+00
 -2.55055477e+00  1.14237448e+00 -8.59604044e-01 -2.66533885e-01
 -3.53288869e-01 -5.10152204e-01  5.37876815e-01  6.44280348e-01
  2.62101462e-01  1.82213777e-01  1.48163017e+00  5.57545311e-01
  9.68078265e-01  3.97623364e-01 -6.62031644e-01 -7.87411965e-02
 -1.31191706e+00 -1.15098136e+00 -5.07232014e-01 -5.33117681e-01
 -2.03818329e+00  4.81759571e-01  7.00432848e-01  2.42092469e-01
 -2.50784406e-01  1.54211163e-01  9.20358717e-01 -4.67161285e-02
  1.94984302e+00  1.05712908e+00  4.59261465e-02  1.47900290e+00
  1.18025194e+00  9.84087213e-01 -1.01746218e+00  1.04306011e+00
 -2.84515138e+00 -1.10596413e+00 -4.14022397e-01 -1.90618005e+00
 -6.93878638e-01  5.00783522e-01 -2.42366372e+00 -8.90470367e-01
 -1.43333401e+00  1.91762605e-01  1.85958157e+00 -5.35170650e-01
 -5.19340784e-01  4.27298721e-01  1.11614609e+00 -5.10724938e-01
  5.59649550e-01 -7.20545760e-01 -6.31213080e-01  9.80289496e-01
 -7.94583465e-01 -1.18405788e+00  6.48242643e-01 -9.13801312e-01
 -5.96719943e-01  9.99982193e-01  3.47212572e-01 -2.37767668e-01
 -5.83529157e-01  5.49563605e-01 -1.52921278e+00  6.14668300e-01
  1.32686567e+00  6.74375818e-01  8.10642772e-02  2.07833073e-01
  2.28108823e-01 -1.06674003e+00  1.07809930e+00  1.57066138e-01
 -1.72630779e-03  7.15944441e-01 -1.15822276e+00 -2.89048078e-01
 -2.78502592e-01  1.05299133e+00 -5.61217656e-01  2.49362553e-01]


");

                #endregion
                #region dump

                numpydumps.Add(@"[-0.12390437  1.16304496 -0.14586174 -0.15048108  0.00941603 -0.52994624
 -0.55060796 -1.93911189  1.29249965  0.27529111  0.48550581 -0.71581371
 -0.26392784  1.23598838 -0.498647    0.88034721  2.34264128  0.41230424
 -1.18214493 -1.4899626   1.37358803 -0.03068002  0.06881652 -1.3947119
  0.23638893 -1.07201305  0.04613397 -0.2769159   0.28714405  0.68370542
  1.62016818 -0.07335466 -0.12430687  0.53417248 -0.194047   -0.47586138
 -0.69916115 -1.51169621  1.21249035 -0.88831091  1.51859145  0.9975823
  0.67847141  0.71021585  0.368926   -0.094377   -0.66935516 -1.10034943
  1.20609147 -0.50019823  0.55200417 -1.67227684  0.61434217 -0.46904361
 -1.64176993  1.09357473 -0.29842532  0.20991144 -0.77976005 -1.70570066
 -1.61539342 -0.91094832  0.59735364  0.86622482  0.26288428 -0.66077546
  0.97031012  0.19142329  2.02459453 -0.39633399  0.61575001  0.47625454
  0.25985681 -1.23927625  0.66385246  2.2582202   1.01575062 -0.44971101
 -0.6176078  -0.86978406  0.41693935  0.32668291 -0.87591656  0.02331285
  0.98874057  1.65208135  1.28568805  0.70028894  0.15771631 -1.04745954
  1.03581026 -0.36589758  1.54547217 -1.7058853   0.93480928 -0.71813614
 -0.83404238  1.19633263 -1.81667159 -0.28729049 -0.46671279 -0.44384536
  0.17991496 -1.63253665 -0.29854903  0.21395715 -1.12140365  0.8317473
  1.66156622  1.28911522 -0.14937403  0.47885495 -0.32437709 -0.02961868
 -0.35869215  0.01309697 -0.74405662  2.09312809 -1.32595588  0.29698788
  0.68828796 -1.41835444  0.01333975 -0.179941    1.5618111  -2.3312706
  0.18720528  1.29721606 -0.23920865 -0.91997989  0.01190776  1.9586356
 -1.36945953 -0.89549728  0.78364833  0.21908659 -1.06663423 -1.780737
 -0.19288757 -2.08087314 -0.75749362 -1.35897919  1.06620238 -1.37358517
 -0.05143348 -1.05606486  0.601766   -0.91951371  0.32563201 -0.88550372
 -0.62253073 -0.28407198 -0.5236053   0.21598949  0.32156054 -0.2593958
  0.52767616 -0.96026117 -0.91395318  0.03694909 -0.8369577   0.54196922
  1.25856669 -1.12321265 -0.5986518   0.63917989  2.93676703 -0.2868107
  0.31943209  0.13438867  0.82367299 -0.55009549  0.17922328 -0.38475946
 -0.24441693  1.11459547  0.89876266  0.72315119 -0.55841076  1.33814537
 -0.84645835 -0.37336978 -1.05309625  0.08537021 -0.36310714  0.73778041
 -0.45507499 -1.08842718  1.9977424  -1.3442767  -0.66946733  1.39731308
 -0.53100057 -1.27290814  0.97129692 -0.46796727 -0.96727475  0.09204328
 -1.06383124 -0.55467351  0.02314144 -1.54051081  0.77819362 -0.03293601
  0.28909725 -1.92438774  0.19903497  0.10489569  1.33199971 -1.09885475
 -0.77473811  0.56978579 -1.73851882 -0.17197011 -0.81492546  1.22136945
 -0.48812591  1.8677688  -0.24548267  0.36525848 -0.6573699  -0.16653929
 -0.63126861  1.10673021  0.91879518 -1.88996161 -0.51291025  0.46427486
 -0.93201633 -2.07315092  1.01374155  0.17693759 -0.64335515  1.82596233
 -0.59482449 -1.12948988  1.77282369  1.61686659 -0.23339414 -1.07728182
  2.5206555   0.13775829  1.11112042  1.7844098  -0.21596761 -0.11481751
 -0.66871996 -0.54345863  0.95810338 -0.96421962  0.25293181 -1.0495602
  2.66190766  1.14445902 -0.35132436 -0.74805564  1.18615038 -0.5424971
 -0.40560453 -0.98096366 -0.62413874  0.17093588  0.04814347  0.78584785
 -1.29680903 -0.86701455 -1.91445074 -0.77772049  0.07699027  1.0715366
  0.00960339 -0.20722207  0.37021295  0.83230173  0.52007506  0.60112446
  0.95931119 -0.04051263 -0.41554903  0.7079964   1.0223928   1.35418903
  1.49099872  0.88581756 -0.1637658   0.42353029  1.24892623 -1.83867733
 -1.29275895 -0.06896871 -0.98325898  0.02636795 -0.6691346   0.39458231
  0.32566169  0.39302445  0.91415357 -0.73973831  0.29383777  0.86186559
  1.21765851  1.49351322  0.77694607  0.07981444 -2.34177704  0.69862612
 -0.33719323 -2.40825853  1.26748173  2.02117577  1.13442396  0.99383075
  0.69652403  0.13537191  0.5164005  -1.16726063 -0.49914112  1.00989734
  0.3015725  -0.0661849  -0.5597468  -0.67458473  0.20462845  0.7747758
  0.12288168 -0.56725322 -0.70710705 -0.2378496   0.0798144   0.44812438
 -0.1003449   0.74957838  0.61275174 -1.35890791  0.08909734 -1.25716562
 -1.1472255   0.32968824 -1.33127293  1.28242174 -0.47114116 -0.43532436
  1.05515096  1.96854989  0.43515475  0.86294395  0.08921987 -0.57863476
 -0.75700191  0.21430611  0.14876775  0.91048473 -0.01185102  0.6605087
 -0.46275503  0.97768741  0.66360087 -0.22205107 -1.74849216  2.95843091
 -1.07919074  1.75206764 -0.69519672  0.98412889  0.80518772  0.29396393
  0.41224609  1.73422782 -0.65540183  0.55161617 -1.45115257  1.14534268
  1.27308051  1.41081065 -0.35664009  0.02353795  0.59384176 -1.19272105
  0.71573874 -1.98101937 -1.36891321  0.23886218  1.00231995  1.04666611
 -0.96576064 -0.05743795  1.81067435  0.36035449 -1.2819452   1.05788838
 -1.03884851  2.90101603 -0.04155778  0.37479325 -1.56256272  0.39809896
 -0.67803505  0.06167131 -0.6256777   1.64766106 -0.360356   -1.38682077
 -0.13855468  1.04267228 -0.39417284 -0.4047194   0.2718667  -1.36865037
 -1.70359705  0.26752149 -1.50229072 -0.31528521 -0.36418834 -0.90110824
 -0.21713411  3.45736576  0.08192219 -1.1458367  -0.14611024 -1.0057563
 -0.01899595 -0.70775807  2.24664666 -0.08256469 -0.07952722  0.19750619
 -1.06015572  1.55453701 -0.4983411  -0.13505944 -2.18675583  0.84146301
 -1.6913539  -0.03230865 -1.48790694  0.26559938  0.69529851 -0.93394697
 -0.1004473   0.1639593   0.85276615  1.38522565  1.13205738 -0.07021786
 -0.26409457 -0.05115269  0.67152921  1.0431901  -0.07837503 -1.46942511
  2.10870704 -0.22559916  0.22583408  0.40123719  0.89221256  0.96859892
  1.18669477 -1.47117405 -0.56514871 -0.31588087 -1.32374653 -0.90368356
 -1.18321131  0.84720586  1.00310043 -0.75487953  1.09696709 -1.07343981
  1.10421696 -0.5844669  -0.19869846 -0.48361382 -0.67345678  0.42512861
  1.19783828 -0.33640884 -1.62207653  1.02384982 -0.03178775 -1.14533079
 -2.38338763 -0.58573398  1.73352723 -0.14877311 -0.63730249 -1.8394252
 -0.29253952 -0.50358825 -0.09254311 -0.39876278  1.07190442 -0.47676134
 -0.90219978 -0.23073157  1.40455654 -2.38469891  0.0398217  -0.67958331
  0.04631032 -0.67144477  0.2376305  -1.84477078  0.46028125  0.73862072
  0.09302836  0.92954939  0.66291216 -1.12331561 -0.44948967 -1.98788916
  0.56356927  0.30049402 -0.04325529 -2.18777453 -0.06017883  0.57496224
 -0.2590853  -0.80348739  0.70317833 -0.52553125  0.56798527  0.50808775
 -1.17520083  1.85552612 -0.54790998  0.00359979 -0.40177607  1.00030884
 -1.43025419  1.11714239 -1.71848765  1.84122629  1.11888605 -0.4635205
 -1.84594777  0.65899554  1.31509014 -0.12325619  0.81696824 -0.5305365
  0.95167306 -0.71761501  0.820521    1.33969822  0.39361052 -0.523721
  1.62119149  1.18153877 -1.15394706 -0.76124877 -0.35557711  0.46643001
  0.60017354  1.13953431  1.91799195 -0.45368883 -0.05801743 -0.04756361
 -0.12590485  0.94271362  0.85899877 -0.57001248 -0.01423607  1.44441259
  1.05082321 -1.01614985  0.40065309  0.13023553 -3.47968092  0.70457348
 -1.2715003   0.70581178 -0.75001562  1.61920911 -1.8177256   2.04668841
  0.17561353  0.99377361 -0.47248391 -0.55944132 -0.07231811 -0.19389753
  1.81581197 -0.79609498  0.89861665 -0.30050737  1.32403083  0.08992481
  0.90809502 -1.26079995 -0.99565651 -2.09633888  0.14600644  1.14316894
 -0.05328204  0.41903435 -0.90833537 -0.11968884  0.45911865 -0.83377381]


");

                #endregion

                List<double> allCast = new List<double>();

                foreach (string numpydump in numpydumps)
                {
                    string replaced = numpydump.
                        Replace("[", "").
                        Replace("]", "");

                    var cast = Regex.Matches(replaced, @"[^\s]+").
                        AsEnumerable().
                        Select(o => double.Parse(o.Value)).
                        ToArray();

                    allCast.AddRange(cast);
                }

                Debug3DWindow window = new Debug3DWindow()
                {
                    Title = "usage",
                };

                var graph = Debug3DWindow.GetCountGraph(() => allCast[rand.Next(allCast.Count)]);
                window.AddGraph(graph, new Point3D(), 1);

                window.Show();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void MultiColorTriangle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                #region from web

                //https://stackoverflow.com/questions/6653489/triangular-gradient-in-wpf3d

                #region 1
                //< ModelVisual3D >
                //          < ModelVisual3D.Content >
                //            < GeometryModel3D >
                //              < GeometryModel3D.Geometry >
                //                < MeshGeometry3D
                //                 TriangleIndices = "0,1,2 2,1,0 "
                //                 Normals = "0,0,1 0,0,1 0,0,1 0,0,1 0,0,1 0,0,1 "
                //                 TextureCoordinates = "0,0 1,0 0,1"
                //                 Positions = "-0.5,-0.5,0.5 0.5,-0.5,0.5 0,0.5,0.5 " />
                //              </ GeometryModel3D.Geometry >
                //              < GeometryModel3D.Material >
                //                < MaterialGroup >
                //                  < DiffuseMaterial Brush = "Black" />

                //                 </ MaterialGroup >

                //               </ GeometryModel3D.Material >

                //             </ GeometryModel3D >

                //           </ ModelVisual3D.Content >

                //         </ ModelVisual3D >
                #endregion
                #region 2
                //         < ModelVisual3D >

                //           < ModelVisual3D.Content >

                //             < GeometryModel3D >

                //               < GeometryModel3D.Geometry >

                //                 < MeshGeometry3D
                //                 TriangleIndices = "0,1,2 2,1,0 "
                //                 Normals = "0,0,1 0,0,1 0,0,1 0,0,1 0,0,1 0,0,1 "
                //                 TextureCoordinates = "0,0 1,0 0,1"
                //                 Positions = "-0.5,-0.5,0.5 0.5,-0.5,0.5 0,0.5,0.5 " />
                //              </ GeometryModel3D.Geometry >
                //              < GeometryModel3D.Material >
                //                < MaterialGroup >
                //                  < DiffuseMaterial >
                //                    < DiffuseMaterial.Brush >
                //                      < RadialGradientBrush Center = "0,0" GradientOrigin = "0,0" RadiusX = "1" RadiusY = "1" >

                //                               < RadialGradientBrush.GradientStops >

                //                                 < GradientStop Color = "#FFFF0000" Offset = "0" />

                //                                    < GradientStop Color = "#00FF0000" Offset = "1" />

                //                                     </ RadialGradientBrush.GradientStops >

                //                                   </ RadialGradientBrush >

                //                                 </ DiffuseMaterial.Brush >

                //                               </ DiffuseMaterial >

                //                             </ MaterialGroup >

                //                           </ GeometryModel3D.Material >

                //                         </ GeometryModel3D >

                //                       </ ModelVisual3D.Content >

                //                     </ ModelVisual3D >
                #endregion
                #region 3
                //                     < ModelVisual3D >

                //                       < ModelVisual3D.Content >

                //                         < GeometryModel3D >

                //                           < GeometryModel3D.Geometry >

                //                             < MeshGeometry3D
                //                 TriangleIndices = "0,1,2 2,1,0 "
                //                 Normals = "0,0,1 0,0,1 0,0,1 0,0,1 0,0,1 0,0,1 "
                //                 TextureCoordinates = "0,0 1,0 0,1"
                //                 Positions = "-0.5,-0.5,0.5 0.5,-0.5,0.5 0,0.5,0.5 " />
                //              </ GeometryModel3D.Geometry >
                //              < GeometryModel3D.Material >
                //                < MaterialGroup >
                //                  < DiffuseMaterial >
                //                    < DiffuseMaterial.Brush >
                //                      < RadialGradientBrush Center = "0.5,1" GradientOrigin = "0.5,1" RadiusX = "1" RadiusY = "1" >

                //                               < RadialGradientBrush.GradientStops >

                //                                 < GradientStop Color = "#FF00FF00" Offset = "0" />

                //                                    < GradientStop Color = "#0000FF00" Offset = "1" />

                //                                     </ RadialGradientBrush.GradientStops >

                //                                   </ RadialGradientBrush >

                //                                 </ DiffuseMaterial.Brush >

                //                               </ DiffuseMaterial >

                //                             </ MaterialGroup >

                //                           </ GeometryModel3D.Material >

                //                         </ GeometryModel3D >

                //                       </ ModelVisual3D.Content >

                //                     </ ModelVisual3D >
                #endregion
                #region 4
                //                     < ModelVisual3D >

                //                       < ModelVisual3D.Content >

                //                         < GeometryModel3D >

                //                           < GeometryModel3D.Geometry >

                //                             < MeshGeometry3D
                //                 TriangleIndices = "0,1,2 2,1,0 "
                //                 Normals = "0,0,1 0,0,1 0,0,1 0,0,1 0,0,1 0,0,1 "
                //                 TextureCoordinates = "0,0 1,0 0,1"
                //                 Positions = "-0.5,-0.5,0.5 0.5,-0.5,0.5 0,0.5,0.5 " />
                //              </ GeometryModel3D.Geometry >
                //              < GeometryModel3D.Material >
                //                < MaterialGroup >
                //                  < DiffuseMaterial >
                //                    < DiffuseMaterial.Brush >
                //                      < RadialGradientBrush Center = "1,0" GradientOrigin = "1,0" RadiusX = "1" RadiusY = "1" >

                //                               < RadialGradientBrush.GradientStops >

                //                                 < GradientStop Color = "#FF0000FF" Offset = "0" />

                //                                    < GradientStop Color = "#000000FF" Offset = "1" />

                //                                     </ RadialGradientBrush.GradientStops >

                //                                   </ RadialGradientBrush >

                //                                 </ DiffuseMaterial.Brush >

                //                               </ DiffuseMaterial >

                //                             </ MaterialGroup >

                //                           </ GeometryModel3D.Material >

                //                         </ GeometryModel3D >

                //                       </ ModelVisual3D.Content >

                //                     </ ModelVisual3D >
                #endregion

                #endregion

                Debug3DWindow window = new Debug3DWindow()
                {
                    Title = "from web",
                };

                ITriangle triangle = new Triangle(new Point3D(-0.5, -0.5, 0.5), new Point3D(0.5, -0.5, 0.5), new Point3D(0, 0.5, 0.5));
                Vector3D normal = triangle.Normal;

                MeshGeometry3D geometry = new MeshGeometry3D()
                {
                    TriangleIndices = new Int32Collection(new[] { 0, 1, 2, 2, 1, 0 }),
                    Normals = new Vector3DCollection(new[] { normal, normal, normal, normal, normal, normal }),
                    TextureCoordinates = new PointCollection(new[] { new Point(0, 0), new Point(1, 0), new Point(0, 1) }),
                    Positions = new Point3DCollection(new[] { triangle.Point0, triangle.Point1, triangle.Point2 }),
                };

                #region back black

                window.Visuals3D.Add(
                    new ModelVisual3D()
                    {
                        Content = new GeometryModel3D()
                        {
                            Material = new DiffuseMaterial(Brushes.Black),
                            Geometry = geometry,
                        },
                    });

                #endregion
                #region red

                window.Visuals3D.Add(
                    new ModelVisual3D()
                    {
                        Content = new GeometryModel3D()
                        {
                            Material = new DiffuseMaterial(new RadialGradientBrush()
                            {
                                Center = new Point(0, 0),
                                GradientOrigin = new Point(0, 0),
                                RadiusX = 1,
                                RadiusY = 1,
                                GradientStops = new GradientStopCollection(new[]
                                {
                                    new GradientStop(UtilityWPF.ColorFromHex( "#FFFF0000"), 0),
                                    new GradientStop(UtilityWPF.ColorFromHex("#00FF0000"), 1),
                                }),
                            }),
                            Geometry = geometry,
                        },
                    });

                #endregion
                #region green

                window.Visuals3D.Add(
                    new ModelVisual3D()
                    {
                        Content = new GeometryModel3D()
                        {
                            Material = new DiffuseMaterial(new RadialGradientBrush()
                            {
                                Center = new Point(0.5, 1),
                                GradientOrigin = new Point(0.5, 1),
                                RadiusX = 1,
                                RadiusY = 1,
                                GradientStops = new GradientStopCollection(new[]
                                {
                                    new GradientStop(UtilityWPF.ColorFromHex( "#FF00FF00"), 0),
                                    new GradientStop(UtilityWPF.ColorFromHex("#0000FF00"), 1),
                                }),
                            }),
                            Geometry = geometry,
                        },
                    });

                #endregion
                #region blue

                window.Visuals3D.Add(
                    new ModelVisual3D()
                    {
                        Content = new GeometryModel3D()
                        {
                            Material = new DiffuseMaterial(new RadialGradientBrush()
                            {
                                Center = new Point(1, 0),
                                GradientOrigin = new Point(1, 0),
                                RadiusX = 1,
                                RadiusY = 1,
                                GradientStops = new GradientStopCollection(new[]
                                {
                                    new GradientStop(UtilityWPF.ColorFromHex( "#FF0000FF"), 0),
                                    new GradientStop(UtilityWPF.ColorFromHex("#000000FF"), 1),
                                }),
                            }),
                            Geometry = geometry,
                        },
                    });

                #endregion

                window.Show();

                window = new Debug3DWindow()
                {
                    Title = "from function",
                };

                window.AddAxisLines(1.5, .005);

                window.Visuals3D.Add(new ModelVisual3D()
                {
                    Content = UtilityWPF.GetGradientTriangle
                    (
                        new Point3D(-1, 0, 0),
                        new Point3D(1, 0, 0),
                        new Point3D(0, 1, 0),

                        UtilityWPF.ColorFromHex("F00"),
                        UtilityWPF.ColorFromHex("0F0"),
                        UtilityWPF.ColorFromHex("00F"),

                        //UtilityWPF.ColorFromHex("F0F"),
                        //UtilityWPF.ColorFromHex("0F0"),
                        //UtilityWPF.ColorFromHex("FFF"),
                        true
                    ),
                });

                //window.Visuals3D.Add(new ModelVisual3D()
                //{
                //    Content = UtilityWPF.GetGradientTriangle
                //    (
                //        new Point3D(2, 0, 0),
                //        new Point3D(0, 2, 0),
                //        new Point3D(0, 0, 2),
                //        UtilityWPF.ColorFromHex("F00"),
                //        UtilityWPF.ColorFromHex("0F0"),
                //        UtilityWPF.ColorFromHex("00F")
                //    ),
                //});

                for (int cntr = 0; cntr < 15; cntr++)
                {
                    window.Visuals3D.Add(new ModelVisual3D()
                    {
                        Content = UtilityWPF.GetGradientTriangle
                        (
                            Math3D.GetRandomVector_Spherical(10).ToPoint(),
                            Math3D.GetRandomVector_Spherical(10).ToPoint(),
                            Math3D.GetRandomVector_Spherical(10).ToPoint(),
                            UtilityWPF.ColorFromHex("F00"),
                            UtilityWPF.ColorFromHex("0F0"),
                            UtilityWPF.ColorFromHex("00F"),
                            true
                        ),
                    });
                }

                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void GradientSquare_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Point3D from = new Point3D(0, 0, 0);
                Point3D to = new Point3D(0, 1, 0);
                double half = .2;


                Vector3D line = to - from;
                if (line.X == 0 && line.Y == 0 && line.Z == 0)
                    line.X = 0.000000001d;

                Vector3D orth1 = Math3D.GetArbitraryOrhonganal(line);
                orth1 = Math3D.RotateAroundAxis(orth1, line, StaticRandom.NextDouble() * Math.PI * 2d);     // give it a random rotation so that if many lines are created by this method, they won't all be oriented the same
                orth1 = orth1.ToUnit() * half;

                Vector3D orth2 = Vector3D.CrossProduct(line, orth1);
                orth2 = orth2.ToUnit() * half;


                Material material = new DiffuseMaterial(new LinearGradientBrush(Colors.Black, Colors.White, new Point(0, 0), new Point(1, 0)));


                Debug3DWindow window = new Debug3DWindow();

                window.AddAxisLines(1, .005);

                MeshGeometry3D geometry = new MeshGeometry3D();

                #region plate 1

                // 0
                geometry.Positions.Add(from + orth1);
                geometry.TextureCoordinates.Add(new Point(0, 0));

                // 1
                geometry.Positions.Add(to + orth1);
                geometry.TextureCoordinates.Add(new Point(1, 1));

                // 2
                geometry.Positions.Add(from - orth1);
                geometry.TextureCoordinates.Add(new Point(0, 0));

                // 3
                geometry.Positions.Add(to - orth1);
                geometry.TextureCoordinates.Add(new Point(1, 1));

                geometry.TriangleIndices.Add(0);
                geometry.TriangleIndices.Add(3);
                geometry.TriangleIndices.Add(1);

                geometry.TriangleIndices.Add(0);
                geometry.TriangleIndices.Add(2);
                geometry.TriangleIndices.Add(3);

                geometry.Normals.Add(orth2);
                geometry.Normals.Add(orth2);
                geometry.Normals.Add(orth2);
                geometry.Normals.Add(orth2);
                geometry.Normals.Add(orth2);
                geometry.Normals.Add(orth2);

                #endregion

                #region plate 2

                // 4
                geometry.Positions.Add(from + orth2);
                geometry.TextureCoordinates.Add(new Point(0, 0));

                // 5
                geometry.Positions.Add(to + orth2);
                geometry.TextureCoordinates.Add(new Point(1, 1));

                // 6
                geometry.Positions.Add(from - orth2);
                geometry.TextureCoordinates.Add(new Point(0, 0));

                // 7
                geometry.Positions.Add(to - orth2);
                geometry.TextureCoordinates.Add(new Point(1, 1));

                geometry.TriangleIndices.Add(4);
                geometry.TriangleIndices.Add(7);
                geometry.TriangleIndices.Add(5);

                geometry.TriangleIndices.Add(4);
                geometry.TriangleIndices.Add(6);
                geometry.TriangleIndices.Add(7);

                geometry.Normals.Add(orth1);
                geometry.Normals.Add(orth1);
                geometry.Normals.Add(orth1);
                geometry.Normals.Add(orth1);
                geometry.Normals.Add(orth1);
                geometry.Normals.Add(orth1);

                #endregion

                GeometryModel3D model = new GeometryModel3D()
                {
                    Material = material,
                    BackMaterial = material,
                    Geometry = geometry,
                };

                window.AddModel(model);



                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void GradientArrow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Debug3DWindow window = new Debug3DWindow();

                window.AddAxisLines(1, .005);

                Color colorFrom = Colors.Black;
                Color colorTo = Colors.White;

                BillboardLine3D line = new BillboardLine3D()
                {
                    FromPoint = new Point3D(-1, -1, -1),
                    ToPoint = new Point3D(1, 1, 1),
                    Thickness = .2,
                    Material = StaticRandom.NextBool() ?
                        BillboardLine3D.GetLinearGradientMaterial_Reflective(colorFrom, colorTo) :
                        BillboardLine3D.GetLinearGradientMaterial_Unlit(colorFrom, colorTo),
                };

                window.AddModel(line.Model);

                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void OppositeVector_Click(object sender, RoutedEventArgs e)
        {
            const double LINE = .005;
            const double DOT = .03;

            try
            {
                Debug3DWindow window = new Debug3DWindow();

                Vector3D vector1 = Math3D.GetRandomVector_Spherical_Shell(1);
                Vector3D vector2;
                double dot;

                do
                {
                    vector2 = Math3D.GetRandomVector_Spherical_Shell(1);
                    dot = Vector3D.DotProduct(vector1, vector2);
                } while (dot >= 0);

                window.AddLine(new Point3D(), vector1.ToPoint(), LINE, Colors.Orchid);
                window.AddLine(new Point3D(), vector2.ToPoint(), LINE, Colors.Coral);

                Vector3D v1onto2 = vector1.GetProjectedVector(vector2);
                Vector3D v2onto1 = vector2.GetProjectedVector(vector1);

                window.AddDot(v1onto2, DOT, Colors.Orchid);
                window.AddDot(v2onto1, DOT, Colors.Coral);

                window.AddLine(v1onto2, v2onto1, LINE, Colors.White);

                double halfLenAlongs = (v2onto1 - v1onto2).Length / 2;

                window.AddText($"dot: {dot.ToStringSignificantDigits(3)}");
                window.AddText($"half len alongs: {halfLenAlongs.ToStringSignificantDigits(3)}");

                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void PolyFromSegments_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var edges = new[]
                {
                    (0, 1),
                    (1, 2),
                    (0, 2),
                };

                int[] poly = UtilityCore.GetPolygon(edges);




                edges = new[]
                {
                    (0, 1),
                    (1, 2),
                };

                poly = UtilityCore.GetPolygon(edges);


            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Background_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (!_initialized)
                {
                    return;
                }

                grdViewPort.Background = new SolidColorBrush(UtilityWPF.AlphaBlend(
                    UtilityWPF.ColorFromHex("30FFFFFF"),
                    UtilityWPF.ColorFromHex("D0000000"),
                    UtilityCore.GetScaledValue_Capped(0, 1, trkBackground.Minimum, trkBackground.Maximum, trkBackground.Value)));

                grdViewPort.BorderBrush = new SolidColorBrush(UtilityWPF.AlphaBlend(
                    UtilityWPF.ColorFromHex("30000000"),
                    UtilityWPF.ColorFromHex("30FFFFFF"),
                    UtilityCore.GetScaledValue_Capped(0, 1, trkBackground.Minimum, trkBackground.Maximum, trkBackground.Value)));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StopTimer();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private void StartTimer(ITimerVisualization visualization)
        {
            _currentVisualization = visualization;
            _lastTick = DateTime.UtcNow;
            _timer.Start();

            chkRunning.IsChecked = true;
        }
        private void StopTimer()
        {
            _timer.Stop();

            ITimerVisualization visualization = _currentVisualization;
            _currentVisualization = null;

            if (visualization != null)
            {
                visualization.Dispose();
            }
        }

        private void ShowCollapseSphere(int dimensions)
        {
            if (!int.TryParse(txtNumPoints.Text, out int numPoints))
            {
                MessageBox.Show("Couldn't parse the number of points as an integer", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            double slicePlane = trkSlicePlane.Value;
            double? topSlicePlane = null;
            if (chkTopSlicePlane.IsChecked.Value)
            {
                if (trkSlicePlane.Value > trkTopSlicePlane.Value)
                {
                    slicePlane = trkTopSlicePlane.Value;        // instead of showing an error, just flip the values
                    topSlicePlane = trkSlicePlane.Value;
                }
                else
                {
                    topSlicePlane = trkTopSlicePlane.Value;
                }
            }

            CollapseSphere4D.CollapseSphere4D_Settings settings = null;
            if (_currentVisualization is CollapseSphere4D existing)
            {
                settings = existing.Settings;
            }

            StopTimer();

            CollapseSphere4D visualization = new CollapseSphere4D(dimensions, numPoints, grdViewPort, _viewport, _camera, frontPlate, _colors, slicePlane, topSlicePlane, settings);

            StartTimer(visualization);
        }

        private static void RunTest(string title, string radius, Func<(VectorND, VectorND)> func)
        {
            if (!double.TryParse(radius, out _radius))
            {
                MessageBox.Show("Invalid Radius", "Run Test", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            double LINE = _radius / 300;
            double THRESHOLD = _radius * .04;

            Color colorStandard = Colors.White;
            Color colorSelected = Colors.DodgerBlue;

            #region example

            Debug3DWindow windowSample = new Debug3DWindow()
            {
                Title = title,
                Trackball_InertiaPercentRetainPerSecond_Angular = 1,
            };

            (double length, BillboardLine3DSet visual)[] samples = Enumerable.Range(0, 300).
                Select(o =>
                {
                    var line = func();

                    BillboardLine3DSet visual = new BillboardLine3DSet();
                    visual.Color = colorStandard;
                    visual.BeginAddingLines();

                    visual.AddLine(line.Item1.ToPoint3D(false), line.Item2.ToPoint3D(false), LINE);

                    visual.EndAddingLines();

                    return (GetLength(line), visual);
                }).
                ToArray();

            windowSample.Visuals3D.AddRange(samples.Select(o => o.visual));

            var mouseMove = new Action<GraphMouseArgs>(e =>
            {
                foreach (var sample in samples)
                {
                    if (sample.length.IsNearValue(e.SelectedXValue, THRESHOLD))
                    {
                        sample.visual.Color = colorSelected;
                    }
                    else
                    {
                        sample.visual.Color = colorStandard;
                    }
                }
            });

            #endregion

            #region graph

            Debug3DWindow windowGraph = new Debug3DWindow()
            {
                Title = title,
            };

            double[] lengths = Enumerable.Range(0, 1000000).
                AsParallel().
                Select(o => GetLength(func())).
                ToArray();

            var avg_stddev = Math1D.Get_Average_StandardDeviation(lengths);
            windowGraph.AddText($"radius: {_radius.ToStringSignificantDigits(2)}");
            windowGraph.AddText($"avg: {avg_stddev.Item1.ToStringSignificantDigits(3)}");
            windowGraph.AddText($"std dev: {avg_stddev.Item2.ToStringSignificantDigits(3)}");

            GraphResult graph = Debug3DWindow.GetCountGraph(() => GetLength(func()));
            windowGraph.AddGraph(graph, new Point3D(0, 0, 0), 5, mouseMove);

            #endregion

            windowGraph.Show();
            windowSample.Show();
        }

        private static (VectorND, VectorND) GetLength_Line_Interior()
        {
            Random rand = StaticRandom.GetRandomForThread();

            double radius = _radius;

            return (new VectorND(rand.NextDouble(-radius, radius)), new VectorND(rand.NextDouble(-radius, radius)));
        }
        private static (VectorND, VectorND) GetLength_Square_Interior()
        {
            Vector3D from = Math3D.GetRandomVector(_radius);
            Vector3D to = Math3D.GetRandomVector(_radius);

            from.Z = 0;
            to.Z = 0;

            return (from.ToVectorND(), to.ToVectorND());
        }
        private static (VectorND, VectorND) GetLength_Cube_Interior()
        {
            Vector3D from = Math3D.GetRandomVector(_radius);
            Vector3D to = Math3D.GetRandomVector(_radius);

            return (from.ToVectorND(), to.ToVectorND());
        }

        private static (VectorND, VectorND) GetLength_Line_Perimiter()
        {
            Random rand = StaticRandom.GetRandomForThread();

            double radius = _radius;

            return (new VectorND(rand.NextBool() ? -radius : radius), new VectorND(rand.NextBool() ? -radius : radius));
        }
        private static (VectorND, VectorND) GetLength_Square_Perimiter()
        {
            Vector from = GetPerimiter2D();
            Vector to = GetPerimiter2D();

            return (from.ToVectorND(), to.ToVectorND());
        }
        private static (VectorND, VectorND) GetLength_Cube_Perimiter()
        {
            Vector3D from = GetPerimiter3D();
            Vector3D to = GetPerimiter3D();

            return (from.ToVectorND(), to.ToVectorND());
        }

        private static (VectorND, VectorND) GetLength_Circle_Interior()
        {
            Vector3D from = Math3D.GetRandomVector_Circular(_radius);
            Vector3D to = Math3D.GetRandomVector_Circular(_radius);

            return (from.ToVectorND(), to.ToVectorND());
        }
        private static (VectorND, VectorND) GetLength_Sphere_Interior()
        {
            Vector3D from = Math3D.GetRandomVector_Spherical(_radius);
            Vector3D to = Math3D.GetRandomVector_Spherical(_radius);

            return (from.ToVectorND(), to.ToVectorND());
        }

        private static (VectorND, VectorND) GetLength_Circle_Perimiter()
        {
            Vector3D from = Math3D.GetRandomVector_Circular_Shell(_radius);
            Vector3D to = Math3D.GetRandomVector_Circular_Shell(_radius);

            return (from.ToVectorND(), to.ToVectorND());
        }
        private static (VectorND, VectorND) GetLength_Sphere_Perimiter()
        {
            Vector3D from = Math3D.GetRandomVector_Spherical_Shell(_radius);
            Vector3D to = Math3D.GetRandomVector_Spherical_Shell(_radius);

            return (from.ToVectorND(), to.ToVectorND());
        }
        private static (VectorND, VectorND) GetLength_Sphere4D_Perimiter()
        {
            VectorND[] from_to = MathND.GetRandomVectors_Spherical_Shell(4, _radius, 2);

            return (from_to[0], from_to[1]);
        }
        private static (VectorND, VectorND) GetLength_Sphere5D_Perimiter()
        {
            VectorND[] from_to = MathND.GetRandomVectors_Spherical_Shell(5, _radius, 2);

            return (from_to[0], from_to[1]);
        }
        private static (VectorND, VectorND) GetLength_Sphere6D_Perimiter()
        {
            VectorND[] from_to = MathND.GetRandomVectors_Spherical_Shell(6, _radius, 2);

            return (from_to[0], from_to[1]);
        }
        private static (VectorND, VectorND) GetLength_Sphere7D_Perimiter()
        {
            VectorND[] from_to = MathND.GetRandomVectors_Spherical_Shell(7, _radius, 2);

            return (from_to[0], from_to[1]);
        }
        private static (VectorND, VectorND) GetLength_Sphere8D_Perimiter()
        {
            VectorND[] from_to = MathND.GetRandomVectors_Spherical_Shell(8, _radius, 2);

            return (from_to[0], from_to[1]);
        }
        private static (VectorND, VectorND) GetLength_Sphere12D_Perimiter()
        {
            VectorND[] from_to = MathND.GetRandomVectors_Spherical_Shell(12, _radius, 2);

            return (from_to[0], from_to[1]);
        }
        private static (VectorND, VectorND) GetLength_Sphere60D_Perimiter()
        {
            VectorND[] from_to = MathND.GetRandomVectors_Spherical_Shell(60, _radius, 2);

            return (from_to[0], from_to[1]);
        }
        private static (VectorND, VectorND) GetLength_Sphere360D_Perimiter()
        {
            VectorND[] from_to = MathND.GetRandomVectors_Spherical_Shell(360, _radius, 2);

            return (from_to[0], from_to[1]);
        }

        private static double GetLength((VectorND, VectorND) points)
        {
            return (points.Item2 - points.Item1).Length;
        }

        private static Vector GetPerimiter2D()
        {
            Random rand = StaticRandom.GetRandomForThread();

            double interior = rand.NextDouble(-_radius, _radius);
            double perimiter = rand.NextBool() ? -_radius : _radius;

            if (rand.NextBool())
            {
                return new Vector(interior, perimiter);
            }
            else
            {
                return new Vector(perimiter, interior);
            }
        }
        private static Vector3D GetPerimiter3D()
        {
            Random rand = StaticRandom.GetRandomForThread();

            double interior1 = rand.NextDouble(-_radius, _radius);
            double interior2 = rand.NextDouble(-_radius, _radius);
            double perimiter = rand.NextBool() ? -_radius : _radius;

            switch (rand.Next(3))
            {
                case 0:
                    return new Vector3D(perimiter, interior1, interior2);

                case 1:
                    return new Vector3D(interior1, perimiter, interior2);

                case 2:
                    return new Vector3D(interior1, interior2, perimiter);

                default:
                    throw new ApplicationException("Unknown axis");
            }
        }

        #endregion
        #region Private Methods - attempt rand spherical point

        // When compressing a sphere into 2D, give an option to texture map an image onto the sphere first (might help with visualizing the distortions)
        // Extending to the 4D sphere, the texture to map would probably be voxels and not just pixels?

        private static Point GetBoxMuller_1(double mean = 0, double stddev = 1)
        {
            //https://stackoverflow.com/questions/5825680/code-to-generate-gaussian-normally-distributed-random-numbers-in-ruby

            Random rand = StaticRandom.GetRandomForThread();

            double theta = 2 * Math.PI * rand.NextDouble();
            double rho = Math.Sqrt(-2 * Math.Log(1 - rand.NextDouble()));

            double scale = stddev * rho;

            double x = mean + scale * Math.Cos(theta);
            double y = mean + scale * Math.Sin(theta);

            return new Point(x, y);
        }

        private static void DoRandGaussTest(int dimensions, bool shouldNegate, string maxXText)
        {
            const double DOT = .007;
            const double LINE = .003;

            if (!double.TryParse(maxXText, out double maxX))
            {
                double smallestY = .0001d;      //UtilityCore.NEARZERO;
                maxX = GetGaussian_Inverse(smallestY);
            }

            int count;
            switch (dimensions)
            {
                case 1: count = 100; break;
                case 2: count = 300; break;
                case 3: count = 1000; break;
                default: count = 2000; break;
            }

            IEnumerator<double> randn = RandN().GetEnumerator();

            VectorND[] points1 = Enumerable.Range(0, count).
                //Select(o => GetGuassVector_1(dimensions, maxX, shouldNegate)).
                Select(o => GetGuassVector_2(dimensions, shouldNegate, randn)).
                ToArray();

            Debug3DWindow window = new Debug3DWindow()
            {
                Title = $"gauss {dimensions}D",
            };

            window.AddAxisLines(points1.Max(o => o.Length) * 1.25, LINE * 3.5);

            window.AddDots(points1.Select(o => o.ToPoint3D(false)), DOT * 3.5, Colors.Gray);

            window.Show();

            window = new Debug3DWindow()
            {
                Title = $"normallized {dimensions}D",
            };

            var points2 = points1.
                Select(o => o.ToUnit()).
                ToArray();

            window.AddAxisLines(1.25, LINE);

            window.AddDots(points2.Select(o => o.ToPoint3D(false)), DOT, Colors.Gray);

            window.Show();
        }
        private static VectorND GetGuassVector_1(int dimensions, double maxInputToGuass, bool randNegate)
        {
            double[] retVal = new double[dimensions];

            Random rand = StaticRandom.GetRandomForThread();

            for (int cntr = 0; cntr < dimensions; cntr++)
            {
                retVal[cntr] = Math1D.GetGaussian(rand.NextDouble(-maxInputToGuass, maxInputToGuass));

                if (randNegate && rand.NextBool())
                {
                    retVal[cntr] = -retVal[cntr];
                }
            }

            return retVal.ToVectorND();
        }
        private static VectorND GetGuassVector_2(int dimensions, bool randNegate, IEnumerator<double> randn)
        {
            double[] retVal = new double[dimensions];

            for (int cntr = 0; cntr < dimensions; cntr++)
            {
                randn.MoveNext();
                retVal[cntr] = randn.Current;

                if (!randNegate)
                {
                    retVal[cntr] = Math.Abs(retVal[cntr]);
                }
            }

            return retVal.ToVectorND();
        }

        /// <summary>
        /// This finds the X that returns the requested Y
        /// NOTE: This only returns the positive version
        /// </summary>
        /// <param name="smallestY">The Y to solve for</param>
        private static double GetGaussian_Inverse(double smallestY, double mean = 0, double stddev = 1)
        {
            double largestY = Math1D.GetGaussian(mean, mean, stddev);       // GetInputForDesiredOutput_PosInput_PosCorrelation needs the output to grow

            //TODO: Solve this algebraically
            double retVal = Math1D.GetInputForDesiredOutput_PosInput_PosCorrelation(largestY - smallestY, smallestY * .01, o => largestY - Math1D.GetGaussian(o, 0, stddev));

            return mean + retVal;

        }

        /// <summary>
        /// This is a c# implementation of numpy.random.randn (without all the count, array params)
        /// NOTE: This returns two numbers for efficiency reasons
        /// </summary>
        /// <remarks>
        /// https://github.com/numpy/numpy/blob/0a113ed38dd538983a00c0ff5d87a56df1b93867/numpy/random/mtrand/mtrand.pyx
        /// https://github.com/numpy/numpy/blob/17b332819e28a52aa0d6a4bd9060747cf9997193/numpy/random/mtrand/randomkit.c
        /// https://docs.scipy.org/doc/numpy-1.15.1/reference/generated/numpy.random.randn.html
        /// 
        /// randn is a high level wrapper in python that eventually calls a method in c (rk_guass).  So this function is a copy of rk_guass
        /// 
        /// randn() ->
        /// self.standard_normal() ->
        /// return cont0_array(self.internal_state, rk_gauss, size, self.lock) ->
        /// rk_gauss()
        /// </remarks>
        private static (double, double) RandN_verbatim()
        {
            Random rand = StaticRandom.GetRandomForThread();

            double x1, x2, r2;

            do
            {
                x1 = rand.NextDouble(-1, 1);
                x2 = rand.NextDouble(-1, 1);
                r2 = (x1 * x1) + (x2 * x2);
            }
            while (r2 >= 1 || r2.IsNearZero());

            // Polar method, a more efficient version of the Box-Muller approach
            double f = Math.Sqrt((-2 * Math.Log(r2)) / r2);

            return (f * x1, f * x2);
        }
        private static IEnumerable<double> RandN()
        {
            Random rand = StaticRandom.GetRandomForThread();

            double x1, x2, r2;

            while (true)
            {
                do
                {
                    x1 = rand.NextDouble(-1, 1);
                    x2 = rand.NextDouble(-1, 1);
                    r2 = (x1 * x1) + (x2 * x2);
                }
                while (r2 >= 1 || r2.IsNearZero());

                // Polar method, a more efficient version of the Box-Muller approach
                double f = Math.Sqrt((-2 * Math.Log(r2)) / r2);

                yield return f * x1;
                yield return f * x2;
            }
        }

        #endregion
    }
}
