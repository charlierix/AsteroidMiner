using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;

namespace Game.HelperClassesWPF
{
    //TODO: Move this to core, next to Math1D.  Need to make the GetPointOnSphere work for all dimensions, and not rely on wpf
    //http://mathoverflow.net/questions/136314/what-is-a-good-method-to-find-random-points-on-the-n-sphere-when-n-is-large
    //https://en.wikipedia.org/wiki/Box%E2%80%93Muller_transform
    //http://www.mlahanas.de/Math/nsphere.htm
    //http://mathworld.wolfram.com/HyperspherePointPicking.html

    #region OLD

    //    /// <summary>
    //    /// This uses double arrays as vectors that can be any number of dimensions
    //    /// NOTE: Any method that takes multiple vectors in the params needs all vectors to be the same dimension
    //    /// </summary>
    //    public static partial class MathND_OLD
    //    {
    //        #region class: BallOfSprings

    //        private static class BallOfSprings
    //        {
    //            public static double[][] ApplyBallOfSprings(double[][] positions, Tuple<int, int, double>[] desiredDistances, int numIterations)
    //            {
    //                const double MULT = .005;
    //                const double MAXSPEED = .05;
    //                const double MAXSPEED_SQUARED = MAXSPEED * MAXSPEED;

    //                double[][] retVal = positions.ToArray();

    //                for (int iteration = 0; iteration < numIterations; iteration++)
    //                {
    //                    // Get Forces (actually displacements, since there's no mass)
    //                    double[][] forces = GetForces(retVal, desiredDistances, MULT);

    //                    // Cap the speed
    //                    forces = forces.
    //                        Select(o => CapSpeed(o, MAXSPEED, MAXSPEED_SQUARED)).
    //                        ToArray();

    //                    // Move points
    //                    for (int cntr = 0; cntr < forces.Length; cntr++)
    //                    {
    //                        for (int i = 0; i < forces[cntr].Length; i++)
    //                        {
    //                            retVal[cntr][i] += forces[cntr][i];
    //                        }
    //                    }
    //                }

    //                return retVal;
    //            }

    //            #region Private Methods

    //            private static double[][] GetForces(double[][] positions, Tuple<int, int, double>[] desiredDistances, double mult)
    //            {
    //                // Calculate forces
    //                Tuple<int, double[]>[] forces = desiredDistances.
    //                    //AsParallel().     //TODO: if distances.Length > threshold, do this in parallel
    //                    SelectMany(o => GetForces_Calculate(o.Item1, o.Item2, o.Item3, positions, mult)).
    //                    ToArray();

    //                // Give them a very slight pull toward the origin so that the cloud doesn't drift away
    //                double[] center = MathND.GetCenter(positions);
    //                double centerMult = mult * -5;

    //                double[] centerPullForce = MathND.Multiply(center, centerMult);

    //                // Group by item
    //                var grouped = forces.
    //                    GroupBy(o => o.Item1).
    //                    OrderBy(o => o.Key);

    //                return grouped.
    //                    Select(o =>
    //                    {
    //                        double[] retVal = centerPullForce.ToArray();        // clone the center pull force

    //                        foreach (var force in o)
    //                        {
    //                            retVal = MathND.Add(retVal, force.Item2);
    //                        }

    //                        return retVal;
    //                    }).
    //                    ToArray();
    //            }
    //            private static Tuple<int, double[]>[] GetForces_Calculate(int index1, int index2, double desiredDistance, double[][] positions, double mult)
    //            {
    //                // Spring from 1 to 2
    //                double[] spring = MathND.Subtract(positions[index2], positions[index1]);
    //                double springLength = MathND.GetLength(spring);

    //                double difference = desiredDistance - springLength;
    //                difference *= mult;

    //                if (Math1D.IsNearZero(springLength) && !difference.IsNearZero())
    //                {
    //                    spring = GetRandomVector_Spherical_Shell(Math.Abs(difference), spring.Length);
    //                }
    //                else
    //                {
    //                    spring = MathND.Multiply(MathND.ToUnit(spring), Math.Abs(difference));
    //                }

    //                if (difference > 0)
    //                {
    //                    // Gap needs to be bigger, push them away (default is closing the gap)
    //                    spring = MathND.Negate(spring);
    //                }

    //                return new[]
    //                {
    //                    Tuple.Create(index1, spring),
    //                    Tuple.Create(index2, MathND.Negate(spring)),
    //                };
    //            }

    //            private static double[] GetRandomVector_Spherical_Shell(double radius, int numDimensions)
    //            {
    //                if (numDimensions == 2)
    //                {
    //                    Vector3D circle = Math3D.GetRandomVector_Circular_Shell(radius);
    //                    return new[] { circle.X, circle.Y };
    //                }
    //                else if (numDimensions == 3)
    //                {
    //                    Vector3D sphere = Math3D.GetRandomVector_Spherical_Shell(radius);
    //                    return new[] { sphere.X, sphere.Y, sphere.Z };
    //                }
    //                else
    //                {
    //                    //TODO: Figure out how to make this spherical instead of a cube
    //                    double[] retVal = new double[numDimensions];

    //                    for (int cntr = 0; cntr < numDimensions; cntr++)
    //                    {
    //                        retVal[cntr] = Math1D.GetNearZeroValue(radius);
    //                    }

    //                    retVal = MathND.Multiply(MathND.ToUnit(retVal, false), radius);

    //                    return retVal;
    //                }
    //            }

    //            private static double[] CapSpeed(double[] velocity, double maxSpeed, double maxSpeedSquared)
    //            {
    //                double lengthSquared = MathND.GetLengthSquared(velocity);

    //                if (lengthSquared < maxSpeedSquared)
    //                {
    //                    return velocity;
    //                }
    //                else
    //                {
    //                    double length = Math.Sqrt(lengthSquared);

    //                    double[] retVal = new double[velocity.Length];

    //                    //ToUnit * MaxSpeed
    //                    for (int cntr = 0; cntr < velocity.Length; cntr++)
    //                    {
    //                        retVal[cntr] = (velocity[cntr] / length) * maxSpeed;
    //                    }

    //                    return retVal;
    //                }
    //            }

    //            #endregion
    //        }

    //        #endregion
    //        #region class: EvenDistribution

    //        /// <summary>
    //        /// This was copied from Math3D
    //        /// </summary>
    //        private static class EvenDistribution
    //        {
    //            #region class: Dot

    //            private class Dot
    //            {
    //                public Dot(bool isStatic, double[] position, double repulseMultiplier)
    //                {
    //                    this.IsStatic = isStatic;
    //                    this.Position = position;
    //                    this.RepulseMultiplier = repulseMultiplier;
    //                }

    //                public readonly bool IsStatic;
    //                public double[] Position;
    //                public readonly double RepulseMultiplier;
    //            }

    //            #endregion
    //            #region class: ShortPair

    //            private class ShortPair
    //            {
    //                public ShortPair(int index1, int index2, double length, double lengthRatio, double avgMult, double[] link)
    //                {
    //                    this.Index1 = index1;
    //                    this.Index2 = index2;
    //                    this.Length = length;
    //                    this.LengthRatio = lengthRatio;
    //                    this.AvgMult = avgMult;
    //                    this.Link = link;
    //                }

    //                public readonly int Index1;
    //                public readonly int Index2;
    //                public readonly double Length;
    //                public readonly double LengthRatio;
    //                public readonly double AvgMult;
    //                public readonly double[] Link;
    //            }

    //            #endregion

    //            public static double[][] GetCube(int returnCount, Tuple<double[], double[]> aabb, double stopRadiusPercent, int stopIterationCount, double[] movableRepulseMultipliers, double[][] existingStaticPoints, double[] staticRepulseMultipliers)
    //            {
    //                // Start with randomly placed dots
    //                Dot[] dots = GetDots(returnCount, existingStaticPoints, aabb, movableRepulseMultipliers, staticRepulseMultipliers);

    //                return GetCube_Finish(dots, aabb, stopRadiusPercent, stopIterationCount);
    //            }
    //            public static double[][] GetCube(double[][] movable, Tuple<double[], double[]> aabb, double stopRadiusPercent, int stopIterationCount, double[] movableRepulseMultipliers, double[][] existingStaticPoints, double[] staticRepulseMultipliers)
    //            {
    //                Dot[] dots = GetDots(movable, existingStaticPoints, aabb, movableRepulseMultipliers, staticRepulseMultipliers);

    //                return GetCube_Finish(dots, aabb, stopRadiusPercent, stopIterationCount);
    //            }

    //            #region Private Methods

    //            private static double[][] GetCube_Finish(Dot[] dots, Tuple<double[], double[]> aabb, double stopRadiusPercent, int stopIterationCount)
    //            {
    //                const double MOVEPERCENT = .1;

    //                CapToCube(dots, aabb);

    //                double radius = MathND.GetLength(MathND.Subtract(aabb.Item2, aabb.Item1)) / 2;
    //                double stopAmount = radius * stopRadiusPercent;

    //                double? minDistance = GetMinDistance(dots, radius, aabb);

    //                for (int cntr = 0; cntr < stopIterationCount; cntr++)
    //                {
    //                    double amountMoved = MoveStep(dots, MOVEPERCENT, aabb, minDistance);
    //                    if (amountMoved < stopAmount)
    //                    {
    //                        break;
    //                    }
    //                }

    //                //NOTE: The movable dots are always the front of the list, so the returned array will be the same positions as the initial movable array
    //                return dots.
    //                    Where(o => !o.IsStatic).
    //                    Select(o => o.Position).
    //                    ToArray();
    //            }

    //            private static Dot[] GetDots(int movableCount, double[][] staticPoints, Tuple<double[], double[]> aabb, double[] movableRepulseMultipliers, double[] staticRepulseMultipliers)
    //            {
    //                // Seed the movable ones with random locations (that's the best that can be done right now)
    //                double[][] movable = new double[movableCount][];
    //                for (int cntr = 0; cntr < movableCount; cntr++)
    //                {
    //                    movable[cntr] = MathND.GetRandomVector_Cube(aabb);
    //                }

    //                // Call the other overload
    //                return GetDots(movable, staticPoints, aabb, movableRepulseMultipliers, staticRepulseMultipliers);
    //            }
    //            private static Dot[] GetDots(double[][] movable, double[][] staticPoints, Tuple<double[], double[]> aabb, double[] movableRepulseMultipliers, double[] staticRepulseMultipliers)
    //            {
    //                int movableCount = movable.Length;

    //                if (movableRepulseMultipliers != null && movableRepulseMultipliers.Length != movableCount)
    //                {
    //                    throw new ArgumentOutOfRangeException("movableRepulseMultipliers", "When movableRepulseMultipliers is nonnull, it must be the same length as the number of movable points");
    //                }

    //                // Figure out how big to make the return array
    //                int length = movableCount;
    //                if (staticPoints != null)
    //                {
    //                    length += staticPoints.Length;

    //                    if (staticRepulseMultipliers != null && staticRepulseMultipliers.Length != staticPoints.Length)
    //                    {
    //                        throw new ArgumentOutOfRangeException("staticRepulseMultipliers", "When staticRepulseMultipliers is nonnull, it must be the same length as the number of static points");
    //                    }
    //                }

    //                Dot[] retVal = new Dot[length];

    //                // Copy the moveable ones
    //                for (int cntr = 0; cntr < movableCount; cntr++)
    //                {
    //                    retVal[cntr] = new Dot(false, movable[cntr], movableRepulseMultipliers == null ? 1d : movableRepulseMultipliers[cntr]);
    //                }

    //                // Add the static points to the end
    //                if (staticPoints != null)
    //                {
    //                    for (int cntr = 0; cntr < staticPoints.Length; cntr++)
    //                    {
    //                        retVal[movableCount + cntr] = new Dot(true, staticPoints[cntr], staticRepulseMultipliers == null ? 1d : staticRepulseMultipliers[cntr]);
    //                    }
    //                }

    //                return retVal;
    //            }

    //            private static double MoveStep(IList<Dot> dots, double percent, Tuple<double[], double[]> aabb, double? minDistance)
    //            {
    //                // Find shortest pair lengths
    //                ShortPair[] shortPairs = GetShortestPair(dots);
    //                if (shortPairs.Length == 0)
    //                {
    //                    return 0;
    //                }

    //                // Move the shortest pair away from each other (based on how far they are away from the avg)
    //                double avg = shortPairs.Average(o => o.LengthRatio);

    //                // Artificially increase repulsive pressure
    //                if (minDistance != null && avg < minDistance.Value)
    //                {
    //                    avg = minDistance.Value;
    //                }

    //                double distToMoveMax = avg - shortPairs[0].LengthRatio;
    //                if (distToMoveMax.IsNearZero())
    //                {
    //                    // Found equilbrium
    //                    return 0;
    //                }

    //                double retVal = 0;

    //                bool isFirst = true;
    //                foreach (ShortPair pair in shortPairs)
    //                {
    //                    // Only want to move them if they are less than average
    //                    if (pair.LengthRatio >= avg)
    //                    {
    //                        break;      // they are sorted, so the rest of the list will also be greater
    //                    }

    //                    // Figure out how far they should move
    //                    double actualPercent, distToMoveRatio;
    //                    if (isFirst)
    //                    {
    //                        actualPercent = percent;
    //                        distToMoveRatio = distToMoveMax;
    //                    }
    //                    else
    //                    {
    //                        distToMoveRatio = avg - pair.LengthRatio;
    //                        actualPercent = (distToMoveRatio / distToMoveMax) * percent;        // don't use the full percent.  Reduce it based on the ratio of this distance with the max distance
    //                    }

    //                    isFirst = false;

    //                    double moveDist = distToMoveRatio * actualPercent * pair.AvgMult;
    //                    retVal = Math.Max(retVal, moveDist);

    //                    // Unit vector
    //                    double[] displaceUnit;
    //                    if (pair.Length.IsNearZero())
    //                    {
    //                        displaceUnit = MathND.GetRandomVector_Cube(aabb.Item1.Length, -1, 1);
    //                        displaceUnit = MathND.ToUnit(displaceUnit, false);
    //                    }
    //                    else
    //                    {
    //                        displaceUnit = MathND.ToUnit(pair.Link, false);
    //                    }

    //                    // Can't move evenly.  Divide it up based on the ratio of multipliers
    //                    double sumMult = dots[pair.Index1].RepulseMultiplier + dots[pair.Index2].RepulseMultiplier;
    //                    double percent2 = dots[pair.Index1].RepulseMultiplier / sumMult;      // flipping 1 and 2, because if 1 is bigger, 2 needs to move more
    //                    double percent1 = 1 - percent2;

    //                    // Move dots
    //                    Dot dot = dots[pair.Index1];
    //                    if (!dot.IsStatic)
    //                    {
    //                        double[] displace = MathND.Multiply(displaceUnit, moveDist * percent1);
    //                        dot.Position = MathND.Subtract(dot.Position, displace);
    //                        CapToCube(dot, aabb);
    //                    }

    //                    dot = dots[pair.Index2];
    //                    if (!dot.IsStatic)
    //                    {
    //                        double[] displace = MathND.Multiply(displaceUnit, moveDist * percent2);
    //                        dot.Position = MathND.Add(dot.Position, displace);
    //                        CapToCube(dot, aabb);
    //                    }
    //                }

    //                return retVal;
    //            }

    //            private static ShortPair[] GetShortestPair(IList<Dot> dots)
    //            {
    //                List<ShortPair> retVal = new List<ShortPair>();

    //                for (int outer = 0; outer < dots.Count - 1; outer++)
    //                {
    //                    ShortPair currentShortest = null;

    //                    for (int inner = outer + 1; inner < dots.Count; inner++)
    //                    {
    //                        if (dots[outer].IsStatic && dots[inner].IsStatic)
    //                        {
    //                            continue;
    //                        }

    //                        double[] link = MathND.Subtract(dots[inner].Position, dots[outer].Position);
    //                        double length = MathND.GetLength(link);
    //                        double avgMult = (dots[inner].RepulseMultiplier + dots[outer].RepulseMultiplier) / 2d;
    //                        double ratio = length / avgMult;

    //                        if (currentShortest == null || ratio < currentShortest.LengthRatio)
    //                        {
    //                            currentShortest = new ShortPair(outer, inner, length, ratio, avgMult, link);
    //                        }
    //                    }

    //                    if (currentShortest != null)
    //                    {
    //                        retVal.Add(currentShortest);
    //                    }
    //                }

    //                return retVal.
    //                    OrderBy(o => o.LengthRatio).
    //                    ToArray();
    //            }

    //            private static void CapToCube(IEnumerable<Dot> dots, Tuple<double[], double[]> aabb)
    //            {
    //                foreach (Dot dot in dots)
    //                {
    //                    CapToCube(dot, aabb);
    //                }
    //            }
    //            private static void CapToCube(Dot dot, Tuple<double[], double[]> aabb)
    //            {
    //                if (dot.IsStatic)
    //                {
    //                    return;
    //                }

    //                for (int cntr = 0; cntr < aabb.Item1.Length; cntr++)
    //                {
    //                    if (dot.Position[cntr] < aabb.Item1[cntr])
    //                    {
    //                        dot.Position[cntr] = aabb.Item1[cntr];
    //                    }
    //                    else if (dot.Position[cntr] > aabb.Item2[cntr])
    //                    {
    //                        dot.Position[cntr] = aabb.Item2[cntr];
    //                    }
    //                }
    //            }

    //            /// <summary>
    //            /// Without this, a 2 point request will never pull from each other
    //            /// </summary>
    //            /// <remarks>
    //            /// I didn't experiment too much with these values, but they seem pretty good
    //            /// </remarks>
    //            private static double? GetMinDistance(Dot[] dots, double radius, Tuple<double[], double[]> aabb)
    //            {
    //                int dimensions = aabb.Item1.Length;

    //                double numerator = radius * 3d / 2d;
    //                double divisor = Math.Pow(dots.Length, 1d / dimensions);

    //                return numerator / divisor;
    //            }

    //            #endregion
    //        }

    //        #endregion

    //        #region Simple

    //        public static bool IsNearZero(double[] vector)
    //        {
    //            return vector.All(o => Math.Abs(o) <= Math1D.NEARZERO);
    //        }
    //        public static bool IsNearValue(double[] vector1, double[] vector2)
    //        {
    //            return Enumerable.Range(0, vector1.Length).
    //                All(o => vector1[o] >= vector2[o] - Math1D.NEARZERO && vector1[o] <= vector2[o] + Math1D.NEARZERO);
    //        }

    //        public static double GetLength(double[] vector)
    //        {
    //            return Math.Sqrt(GetLengthSquared(vector));
    //        }
    //        public static double GetLengthSquared(double[] vector)
    //        {
    //            //copied from GetDistanceSquared for speed reasons

    //            //C^2 = (A1-A2)^2 + (B1-B2)^2 + .....

    //            double retVal = 0;

    //            for (int cntr = 0; cntr < vector.Length; cntr++)
    //            {
    //                retVal += vector[cntr] * vector[cntr];
    //            }

    //            return retVal;
    //        }

    //        public static double GetDistance(double[] vector1, double[] vector2)
    //        {
    //            return Math.Sqrt(GetDistanceSquared(vector1, vector2));
    //        }
    //        public static double GetDistanceSquared(double[] vector1, double[] vector2)
    //        {
    //            #region validate
    //#if DEBUG
    //            if (vector1 == null || vector2 == null)
    //            {
    //                throw new ArgumentException("Vector arrays can't be null");
    //            }
    //            else if (vector1.Length != vector2.Length)
    //            {
    //                throw new ArgumentException("Vector arrays must be the same length " + vector1.Length + ", " + vector2.Length);
    //            }
    //#endif
    //            #endregion

    //            //C^2 = (A1-A2)^2 + (B1-B2)^2 + .....

    //            double retVal = 0;

    //            for (int cntr = 0; cntr < vector1.Length; cntr++)
    //            {
    //                double diff = vector1[cntr] - vector2[cntr];

    //                retVal += diff * diff;
    //            }

    //            return retVal;
    //        }

    //        /// <summary>
    //        /// This scales the values so that the length of the vector is one
    //        /// </summary>
    //        public static double[] ToUnit(double[] vector, bool useNaNIfInvalid = true)
    //        {
    //            double length = GetLength(vector);

    //            if (Math1D.IsNearValue(length, 1))
    //            {
    //                return vector;
    //            }

    //            if (Math1D.IsInvalid(length))
    //            {
    //                double[] retVal = new double[vector.Length];

    //                for (int cntr = 0; cntr < vector.Length; cntr++)
    //                {
    //                    if (useNaNIfInvalid)
    //                    {
    //                        retVal[cntr] = double.NaN;
    //                    }
    //                    else
    //                    {
    //                        retVal[cntr] = 0;
    //                    }
    //                }
    //            }

    //            return Divide(vector, length);
    //        }

    //        /// <summary>
    //        /// This scales the values between -1 and 1
    //        /// NOTE: This is different than ToUnit
    //        /// </summary>
    //        public static double[] ScaledCap(double[] vector)
    //        {
    //            double min = vector.Min();
    //            double max = vector.Max();

    //            double minReturn = min <= 0 ? -1 : 0;
    //            double maxReturn = max <= 0 ? 0 : 1;

    //            return vector.
    //                Select(o => UtilityCore.GetScaledValue(minReturn, maxReturn, min, max, o)).
    //                ToArray();
    //        }

    //        public static double[] Add(double[] vector1, double[] vector2)
    //        {
    //            double[] retVal = new double[vector1.Length];

    //            for (int cntr = 0; cntr < vector1.Length; cntr++)
    //            {
    //                retVal[cntr] = vector1[cntr] + vector2[cntr];
    //            }

    //            return retVal;
    //        }
    //        /// <summary>
    //        /// Subtracts the specified vector from another specified vector (return v1 - v2)
    //        /// </summary>
    //        /// <param name="vector1">The vector from which vector2 is subtracted</param>
    //        /// <param name="vector2">The vector to subtract from vector1</param>
    //        /// <returns>The difference between vector1 and vector2</returns>
    //        public static double[] Subtract(double[] vector1, double[] vector2)
    //        {
    //            double[] retVal = new double[vector1.Length];

    //            for (int cntr = 0; cntr < vector1.Length; cntr++)
    //            {
    //                retVal[cntr] = vector1[cntr] - vector2[cntr];
    //            }

    //            return retVal;
    //        }
    //        public static double[] Multiply(double[] vector, double scalar)
    //        {
    //            double[] retVal = new double[vector.Length];

    //            for (int cntr = 0; cntr < vector.Length; cntr++)
    //            {
    //                retVal[cntr] = vector[cntr] * scalar;
    //            }

    //            return retVal;
    //        }
    //        public static double[] Divide(double[] vector, double scalar)
    //        {
    //            double[] retVal = new double[vector.Length];

    //            for (int cntr = 0; cntr < vector.Length; cntr++)
    //            {
    //                retVal[cntr] = vector[cntr] / scalar;
    //            }

    //            return retVal;
    //        }

    //        public static double[] Negate(double[] vector)
    //        {
    //            double[] retVal = new double[vector.Length];

    //            for (int cntr = 0; cntr < vector.Length; cntr++)
    //            {
    //                retVal[cntr] = -vector[cntr];
    //            }

    //            return retVal;
    //        }

    //        /// <summary>
    //        /// Get a random vector between boundry lower and boundry upper
    //        /// </summary>
    //        public static double[] GetRandomVector(double[] boundryLower, double[] boundryUpper)
    //        {
    //            double[] retVal = new double[boundryLower.Length];

    //            Random rand = StaticRandom.GetRandomForThread();

    //            for (int cntr = 0; cntr < retVal.Length; cntr++)
    //            {
    //                retVal[cntr] = rand.NextDouble(boundryLower[cntr], boundryUpper[cntr]);
    //            }

    //            return retVal;
    //        }

    //        #endregion

    //        #region Random

    //        public static double[] GetRandomVector_Cube(Tuple<double[], double[]> aabb)
    //        {
    //            Random rand = StaticRandom.GetRandomForThread();

    //            double[] retVal = new double[aabb.Item1.Length];

    //            for (int cntr = 0; cntr < retVal.Length; cntr++)
    //            {
    //                retVal[cntr] = rand.NextDouble(aabb.Item1[cntr], aabb.Item2[cntr]);
    //            }

    //            return retVal;
    //        }
    //        public static double[] GetRandomVector_Cube(int dimensions, double min, double max)
    //        {
    //            Random rand = StaticRandom.GetRandomForThread();

    //            double[] retVal = new double[dimensions];

    //            for (int cntr = 0; cntr < retVal.Length; cntr++)
    //            {
    //                retVal[cntr] = rand.NextDouble(min, max);
    //            }

    //            return retVal;
    //        }

    //        public static double[][] GetRandomVectors_Cube_EventDist(int returnCount, Tuple<double[], double[]> aabb, double[] movableRepulseMultipliers = null, double[][] existingStaticPoints = null, double[] staticRepulseMultipliers = null, double stopRadiusPercent = .004, int stopIterationCount = 1000)
    //        {
    //            return EvenDistribution.GetCube(returnCount, aabb, stopRadiusPercent, stopIterationCount, movableRepulseMultipliers, existingStaticPoints, staticRepulseMultipliers);
    //        }
    //        public static double[][] GetRandomVectors_Cube_EventDist(double[][] movable, Tuple<double[], double[]> aabb, double[] movableRepulseMultipliers = null, double[][] existingStaticPoints = null, double[] staticRepulseMultipliers = null, double stopRadiusPercent = .004, int stopIterationCount = 1000)
    //        {
    //            return EvenDistribution.GetCube(movable, aabb, stopRadiusPercent, stopIterationCount, movableRepulseMultipliers, existingStaticPoints, staticRepulseMultipliers);
    //        }

    //        #endregion

    //        #region Misc

    //        public static Tuple<double[], double[]> GetAABB(IEnumerable<double[]> points)
    //        {
    //            double[] first = points.FirstOrDefault();
    //            if (first == null)
    //            {
    //                throw new ArgumentException("The list of points is empty.  Can't determine number of dimensions");
    //            }

    //            double[] min = Enumerable.Range(0, first.Length).
    //                Select(o => double.MaxValue).
    //                ToArray();

    //            double[] max = Enumerable.Range(0, first.Length).
    //                Select(o => double.MinValue).
    //                ToArray();

    //            foreach (double[] point in points)
    //            {
    //                for (int cntr = 0; cntr < point.Length; cntr++)
    //                {
    //                    if (point[cntr] < min[cntr])
    //                    {
    //                        min[cntr] = point[cntr];
    //                    }

    //                    if (point[cntr] > max[cntr])
    //                    {
    //                        max[cntr] = point[cntr];
    //                    }
    //                }
    //            }

    //            return Tuple.Create(min, max);
    //        }

    //        /// <summary>
    //        /// This changes the size of aabb by percent
    //        /// </summary>
    //        /// <param name="percent">If less than 1, this will reduce the size.  If greater than 1, this will increase the size</param>   
    //        public static Tuple<double[], double[]> ResizeAABB(Tuple<double[], double[]> aabb, double percent)
    //        {
    //            double[] center = GetCenter(new[] { aabb.Item1, aabb.Item2 });

    //            double[] dirMin = Subtract(aabb.Item1, center);
    //            dirMin = Multiply(dirMin, percent);

    //            double[] dirMax = Subtract(aabb.Item2, center);
    //            dirMax = Multiply(dirMax, percent);

    //            return Tuple.Create(Add(center, dirMin), Add(center, dirMax));
    //        }

    //        /// <summary>
    //        /// This returns the center of position of the points
    //        /// </summary>
    //        public static double[] GetCenter(IEnumerable<double[]> points)
    //        {
    //            if (points == null)
    //            {
    //                throw new ArgumentException("Unknown number of dimensions");
    //            }

    //            double[] retVal = null;

    //            int length = 0;

    //            foreach (double[] point in points)
    //            {
    //                if (retVal == null)
    //                {
    //                    retVal = new double[point.Length];      // waiting until the first vector is seen to initialize the return array (I don't want to ask how many dimensions there are when it's defined by the points)
    //                }

    //                // Add this point to the total
    //                for (int cntr = 0; cntr < retVal.Length; cntr++)
    //                {
    //                    retVal[cntr] += point[cntr];
    //                }

    //                length++;
    //            }

    //            if (length == 0)
    //            {
    //                throw new ArgumentException("Unknown number of dimensions");
    //            }

    //            double oneOverLen = 1d / Convert.ToDouble(length);

    //            // Divide by count
    //            for (int cntr = 0; cntr < retVal.Length; cntr++)
    //            {
    //                retVal[cntr] *= oneOverLen;
    //            }

    //            return retVal;
    //        }

    //        /// <remarks>
    //        /// http://www.mathsisfun.com/data/standard-deviation.html
    //        /// </remarks>
    //        public static double GetStandardDeviation(IEnumerable<double[]> values)
    //        {
    //            double[] mean = GetCenter(values);

    //            // Variance is the average of the of the distance squared from the mean
    //            double variance = values.
    //                Select(o =>
    //                {
    //                    double[] diff = Subtract(o, mean);
    //                    return GetLengthSquared(diff);
    //                }).
    //                Average();

    //            return Math.Sqrt(variance);
    //        }

    //        public static Tuple<int, int, double>[] GetDistancesBetween(double[][] positions)
    //        {
    //            List<Tuple<int, int, double>> retVal = new List<Tuple<int, int, double>>();

    //            for (int outer = 0; outer < positions.Length - 1; outer++)
    //            {
    //                for (int inner = outer + 1; inner < positions.Length; inner++)
    //                {
    //                    double distance = MathND.GetDistance(positions[outer], positions[inner]);
    //                    retVal.Add(Tuple.Create(outer, inner, distance));
    //                }
    //            }

    //            return retVal.ToArray();
    //        }

    //        public static double[][] ApplyBallOfSprings(double[][] positions, Tuple<int, int, double>[] desiredDistances, int numIterations)
    //        {
    //            return BallOfSprings.ApplyBallOfSprings(positions, desiredDistances, numIterations);
    //        }

    //        public static bool IsInside(Tuple<double[], double[]> aabb, double[] testPoint)
    //        {
    //            for (int cntr = 0; cntr < testPoint.Length; cntr++)
    //            {
    //                if (testPoint[cntr] < aabb.Item1[cntr] || testPoint[cntr] > aabb.Item2[cntr])
    //                {
    //                    return false;
    //                }
    //            }

    //            return true;
    //        }

    //        /// <summary>
    //        /// This converts a base10 number into an arbitrary base
    //        /// </summary>
    //        /// <remarks>
    //        /// Silly example:
    //        ///     136 converted to base 10 will return { 1, 3, 6 }
    //        /// 
    //        /// Other examples:
    //        ///     5 -> base2 = { 1, 0, 1 }
    //        ///     6 -> base2 = { 1, 1, 0 }
    //        ///     
    //        ///     7 -> base16 = { 7 }
    //        ///     15 -> base16 = { 15 }
    //        ///     18 -> base16 = { 1, 2 }
    //        /// </remarks>
    //        /// <param name="base10">A number in base10</param>
    //        /// <param name="baseConvertTo">The base to convert to</param>
    //        public static int[] ConvertToBase(long base10, int baseConvertTo)
    //        {
    //            List<int> retVal = new List<int>();

    //            long num = base10;

    //            while (num != 0)
    //            {
    //                int remainder = Convert.ToInt32(num % baseConvertTo);
    //                num = num / baseConvertTo;
    //                retVal.Add(remainder);
    //            }

    //            retVal.Reverse();

    //            return retVal.ToArray();
    //        }

    //        /// <summary>
    //        /// This returns the "radius" of a cube
    //        /// TODO: Find the radius of a convex polygon instead of a cube
    //        /// </summary>
    //        public static double GetRadius(IEnumerable<double[]> points)
    //        {
    //            return GetRadius(GetAABB(points));
    //        }
    //        /// <summary>
    //        /// This returns the "radius" of a cube
    //        /// </summary>
    //        public static double GetRadius(Tuple<double[], double[]> aabb)
    //        {
    //            // Diameter of the circumscribed sphere
    //            double circumscribedDiam = GetDistance(aabb.Item1, aabb.Item2);

    //            // Diameter of inscribed sphere
    //            double inscribedDiam = 0;
    //            for (int cntr = 0; cntr < aabb.Item1.Length; cntr++)
    //            {
    //                inscribedDiam += aabb.Item2[cntr] - aabb.Item1[cntr];
    //            }
    //            inscribedDiam /= aabb.Item1.Length;

    //            // Return the average of the two
    //            //return (circumscribedDiam + inscribedDiam) / 4d;        // avg=sum/2, radius=diam/2, so divide by 4

    //            // The problem with taking the average of the circumscribed and inscribed radius, is that circumscribed grows
    //            // roughly sqrt(dimensions), but inscribed is constant.  So the more dimensions, the more innacurate inscribed
    //            // will be
    //            //
    //            // But just using circumscribed will always be overboard
    //            //return circumscribedDiam / 2;

    //            // Weighted average.  Dividing by 2 to turn diameter into radius
    //            return ((circumscribedDiam * .85) + (inscribedDiam * .15)) / 2d;
    //        }

    //        #endregion
    //    }

    #endregion

    /// <summary>
    /// NOTE: Any method that takes multiple vectors in the params needs all vectors to be the same size
    /// NOTE: A lot of these methods pull .VectorArray up front.  This is to avoid lots of calls to lock
    /// </summary>
    public static partial class MathND
    {
        #region class: BallOfSprings

        private static class BallOfSprings
        {
            public static VectorND[] ApplyBallOfSprings(VectorND[] positions, Tuple<int, int, double>[] desiredDistances, int numIterations)
            {
                const double MULT = .005;
                const double MAXSPEED = .05;
                const double MAXSPEED_SQUARED = MAXSPEED * MAXSPEED;

                VectorND[] retVal = MathND.Clone(positions);

                for (int iteration = 0; iteration < numIterations; iteration++)
                {
                    // Get Forces (actually displacements, since there's no mass)
                    VectorND[] forces = GetForces(retVal, desiredDistances, MULT);

                    // Cap the speed
                    forces = forces.
                        Select(o => CapSpeed(o, MAXSPEED, MAXSPEED_SQUARED)).
                        ToArray();

                    // Move points
                    for (int cntr = 0; cntr < forces.Length; cntr++)
                    {
                        retVal[cntr] += forces[cntr];
                    }
                }

                return retVal;
            }

            #region Private Methods

            private static VectorND[] GetForces(VectorND[] positions, Tuple<int, int, double>[] desiredDistances, double mult)
            {
                // Calculate forces
                Tuple<int, VectorND>[] forces = desiredDistances.
                    //AsParallel().     //TODO: if distances.Length > threshold, do this in parallel
                    SelectMany(o => GetForces_Calculate(o.Item1, o.Item2, o.Item3, positions, mult)).
                    ToArray();

                // Give them a very slight pull toward the origin so that the cloud doesn't drift away
                VectorND center = MathND.GetCenter(positions);
                double centerMult = mult * -5;

                VectorND centerPullForce = center * centerMult;

                // Group by item
                var grouped = forces.
                    GroupBy(o => o.Item1).
                    OrderBy(o => o.Key);

                return grouped.
                    Select(o =>
                    {
                        VectorND retVal = centerPullForce.Clone();

                        foreach (var force in o)
                        {
                            retVal += force.Item2;
                        }

                        return retVal;
                    }).
                    ToArray();
            }
            private static Tuple<int, VectorND>[] GetForces_Calculate(int index1, int index2, double desiredDistance, VectorND[] positions, double mult)
            {
                // Spring from 1 to 2
                VectorND spring = positions[index2] - positions[index1];
                double springLength = spring.Length;

                double difference = desiredDistance - springLength;
                difference *= mult;

                if (Math1D.IsNearZero(springLength) && !difference.IsNearZero())
                {
                    spring = GetRandomVector_Spherical_Shell(Math.Abs(difference), spring.Size);
                }
                else
                {
                    spring = spring.ToUnit(false) * Math.Abs(difference);
                }

                if (difference > 0)
                {
                    // Gap needs to be bigger, push them away (default is closing the gap)
                    spring.Negate();
                }

                return new[]
                {
                    Tuple.Create(index1, spring),
                    Tuple.Create(index2, -spring),
                };
            }

            private static VectorND GetRandomVector_Spherical_Shell(double radius, int numDimensions)
            {
                if (numDimensions == 2)
                {
                    Vector3D circle = Math3D.GetRandomVector_Circular_Shell(radius);
                    return new VectorND(new[] { circle.X, circle.Y });
                }
                else if (numDimensions == 3)
                {
                    Vector3D sphere = Math3D.GetRandomVector_Spherical_Shell(radius);
                    return new VectorND(new[] { sphere.X, sphere.Y, sphere.Z });
                }
                else
                {
                    //TODO: Figure out how to make this spherical instead of a cube
                    double[] retVal = new double[numDimensions];

                    for (int cntr = 0; cntr < numDimensions; cntr++)
                    {
                        retVal[cntr] = Math1D.GetNearZeroValue(radius);
                    }

                    return new VectorND(retVal).ToUnit(false) * radius;
                }
            }

            private static VectorND CapSpeed(VectorND velocity, double maxSpeed, double maxSpeedSquared)
            {
                double lengthSquared = velocity.LengthSquared;

                if (lengthSquared < maxSpeedSquared)
                {
                    return velocity;
                }
                else
                {
                    return (velocity / Math.Sqrt(lengthSquared)) * maxSpeed;
                }
            }

            #endregion
        }

        #endregion
        #region class: EvenDistribution

        /// <summary>
        /// This was copied from Math3D
        /// </summary>
        private static class EvenDistribution
        {
            #region class: Dot

            private class Dot
            {
                public Dot(bool isStatic, VectorND position, double repulseMultiplier)
                {
                    this.IsStatic = isStatic;
                    this.Position = position;
                    this.RepulseMultiplier = repulseMultiplier;
                }

                public readonly bool IsStatic;
                public VectorND Position;
                public readonly double RepulseMultiplier;
            }

            #endregion
            #region class: ShortPair

            private class ShortPair
            {
                public ShortPair(int index1, int index2, double length, double lengthRatio, double avgMult, VectorND link)
                {
                    this.Index1 = index1;
                    this.Index2 = index2;
                    this.Length = length;
                    this.LengthRatio = lengthRatio;
                    this.AvgMult = avgMult;
                    this.Link = link;
                }

                public readonly int Index1;
                public readonly int Index2;
                public readonly double Length;
                public readonly double LengthRatio;
                public readonly double AvgMult;
                public readonly VectorND Link;

                public override string ToString()
                {
                    return string.Format("{0} - {1} | {2} : {3} | {4} | {5}", Index1, Index2, Length, LengthRatio, AvgMult, Link);
                }
            }

            #endregion

            #region Declaration Section

            private const double SHIFTMULT = .005;

            #endregion

            public static VectorND[] GetCube(int returnCount, Tuple<VectorND, VectorND> aabb, double stopRadiusPercent, int stopIterationCount, double[] movableRepulseMultipliers, VectorND[] existingStaticPoints, double[] staticRepulseMultipliers)
            {
                // Start with randomly placed dots
                Dot[] dots = GetDots(returnCount, existingStaticPoints, aabb, movableRepulseMultipliers, staticRepulseMultipliers);

                return GetCube_Finish(dots, aabb, stopRadiusPercent, stopIterationCount);
            }
            public static VectorND[] GetCube(VectorND[] movable, Tuple<VectorND, VectorND> aabb, double stopRadiusPercent, int stopIterationCount, double[] movableRepulseMultipliers, VectorND[] existingStaticPoints, double[] staticRepulseMultipliers)
            {
                Dot[] dots = GetDots(movable, existingStaticPoints, movableRepulseMultipliers, staticRepulseMultipliers);

                return GetCube_Finish(dots, aabb, stopRadiusPercent, stopIterationCount);
            }

            /// <summary>
            /// This overload lets some of the points be linked to others, which will try to cluster linked points
            /// near each other
            /// </summary>
            /// <remarks>
            /// This overload also got rid of static points and per item repulse modifiers.  If they are really needed, they could be
            /// added but that's a lot of complexity that will probably never be used
            /// 
            /// When clustering by links, the added constraint of keeping in a cube makes a gnarled final structure.  This is needed if
            /// there are multiple independent sets of nodes, but a cleaner algorithm should be used if all nodes are interlinked
            /// </remarks>
            /// <param name="movable">These are the points (keeping the name movable, because the other overload also has static points)</param>
            /// <param name="aabb">The dimensions of the cube</param>
            public static VectorND[] GetCube(VectorND[] movable, Tuple<int, int>[] links, Tuple<VectorND, VectorND> aabb, double stopRadiusPercent, int stopIterationCount, double linkedMult, double unlinkedMult)
            {
                Dot[] dots = GetDots(movable, null, null, null);

                links = GetCleanedLinks(links);

                if (links.Length == 0)
                {
                    // Use the unlinked version
                    return GetCube_Finish(dots, aabb, stopRadiusPercent, stopIterationCount);
                }

                Tuple<int, int>[] unlinked = GetUnlinked(dots.Length, links);

                return GetCube_Finish(dots, links, unlinked, aabb, stopRadiusPercent, stopIterationCount, linkedMult, unlinkedMult);
            }

            //NOTE: This assume that are points are linked to something.  If there is a completely unlinked point, it will be pushed really far away
            public static VectorND[] GetOpenLinked(VectorND[] movable, Tuple<int, int>[] links, double linkDistance, double stopRadiusPercent, int stopIterationCount)
            {
                Dot[] dots = GetDots(movable, null, null, null);

                links = GetCleanedLinks(links);

                if (links.Length == 0)
                {
                    // No links, so do nothing
                    return movable;
                }

                Tuple<int, int>[] unlinked = GetUnlinked(dots.Length, links);

                return GetOpen_Finish(dots, links, unlinked, linkDistance, stopRadiusPercent, stopIterationCount);
            }

            #region Private Methods - linked cube

            private static VectorND[] GetCube_Finish(Dot[] dots, Tuple<int, int>[] linked, Tuple<int, int>[] unlinked, Tuple<VectorND, VectorND> aabb, double stopRadiusPercent, int stopIterationCount, double linkedMult, double unlinkedMult)
            {
                const double MOVEPERCENT = .1;

                double radius = (aabb.Item2 - aabb.Item1).Length / 2;
                double stopAmount = radius * stopRadiusPercent;

                CapToCube(dots, aabb);
                RandomShift(dots, radius * SHIFTMULT);       // if all the points started outside the cube, then CapToCube will have them all stuck to the walls.  When they push away from each other, they will be coplanar

                double? minDistance = GetMinDistance(dots, radius, aabb);

                for (int cntr = 0; cntr < stopIterationCount; cntr++)
                {
                    double amountMoved = MoveStep(dots, linked, unlinked, MOVEPERCENT, aabb, minDistance, linkedMult, unlinkedMult);
                    if (amountMoved < stopAmount)
                    {
                        break;
                    }
                }

                //NOTE: The movable dots are always the front of the list, so the returned array will be the same positions as the initial movable array
                return dots.
                    Where(o => !o.IsStatic).
                    Select(o => o.Position).
                    ToArray();
            }

            private static double MoveStep(IList<Dot> dots, Tuple<int, int>[] linked, Tuple<int, int>[] unlinked, double percent, Tuple<VectorND, VectorND> aabb, double? minDistance, double linkedMult, double unlinkedMult)
            {
                ShortPair[] linkedLengths = GetLengths(dots, linked);
                ShortPair[] unlinkedLengths = GetLengths(dots, unlinked);

                double avg = linkedLengths.Concat(unlinkedLengths).Average(o => o.LengthRatio);

                // Artificially increase repulsive pressure
                if (minDistance != null)
                {
                    if (avg < minDistance.Value)
                        avg = minDistance.Value;
                }

                double retVal = 0d;

                double distToMoveMax;

                #region linked

                // pull toward each other (if > avg)

                // Dividing makes the linked items cluster in a bit tighter
                double avgLinked = avg * linkedMult;

                distToMoveMax = linkedLengths[linkedLengths.Length - 1].LengthRatio - avgLinked;
                if (!distToMoveMax.IsNearZero())
                {
                    bool isFirst = true;
                    foreach (ShortPair pair in linkedLengths.Reverse())       // they are sorted closest to farthest.  Need to go the other way
                    {
                        if (pair.LengthRatio <= avgLinked)
                        {
                            break;
                        }

                        double distance = MovePair(ref isFirst, pair, percent, distToMoveMax, avgLinked, dots, aabb, false);

                        retVal = Math.Max(retVal, distance);
                    }
                }

                #endregion
                #region unlinked

                // push away from each other (if < avg)

                unlinkedLengths = linkedLengths.
                    Concat(unlinkedLengths).
                    OrderBy(o => o.LengthRatio).
                    ToArray();

                double avgUnLinked = avg * unlinkedMult;

                distToMoveMax = avgUnLinked - unlinkedLengths[0].LengthRatio;
                if (!distToMoveMax.IsNearZero())
                {
                    bool isFirst = true;
                    foreach (ShortPair pair in unlinkedLengths)
                    {
                        if (pair.LengthRatio >= avgUnLinked)
                        {
                            break;
                        }

                        double distance = MovePair(ref isFirst, pair, percent, distToMoveMax, avgUnLinked, dots, aabb, true);

                        retVal = Math.Max(retVal, distance);
                    }
                }

                #endregion

                return retVal;
            }

            private static Tuple<int, int>[] GetUnlinked(int count, Tuple<int, int>[] links)
            {
                List<Tuple<int, int>> retVal = new List<Tuple<int, int>>();

                int index = 0;

                for (int outer = 0; outer < count - 1; outer++)
                {
                    for (int inner = outer + 1; inner < count; inner++)
                    {
                        bool foundIt = false;
                        while (index < links.Length)
                        {
                            // compare item1 to outer
                            if (links[index].Item1 > outer)
                            {
                                break;
                            }
                            else if (links[index].Item1 < outer)
                            {
                                index++;        // should never happen
                            }

                            // item1 == outer, comare item2
                            else if (links[index].Item2 > inner)
                            {
                                break;
                            }
                            else if (links[index].Item2 < inner)
                            {
                                index++;        // should never happen
                            }

                            // they both equal
                            else
                            {
                                index++;
                                foundIt = true;
                                break;
                            }
                        }

                        if (!foundIt)
                        {
                            retVal.Add(Tuple.Create(outer, inner));
                        }
                    }
                }

                return retVal.ToArray();
            }

            #endregion
            #region Private Methods - linked open

            private static VectorND[] GetOpen_Finish(Dot[] dots, Tuple<int, int>[] linked, Tuple<int, int>[] unlinked, double linkDistance, double stopRadiusPercent, int stopIterationCount)
            {
                const double MOVEPERCENT = .1;

                double stopAmount = 10 * stopRadiusPercent;      // the choice of "radius" is arbitrary for this open method.  If the desired link length is one, then choose an overall radius that is several lengths

                for (int cntr = 0; cntr < stopIterationCount; cntr++)
                {
                    double amountMoved = MoveStep(dots, linked, unlinked, linkDistance, MOVEPERCENT);
                    if (amountMoved < stopAmount)
                    {
                        break;
                    }
                }

                // Center them
                VectorND[] retVal = dots.
                    Where(o => !o.IsStatic).
                    Select(o => o.Position).
                    ToArray();

                VectorND center = MathND.GetCenter(retVal);

                for (int cntr = 0; cntr < retVal.Length; cntr++)
                {
                    retVal[cntr] -= center;
                }

                return retVal;
            }

            private static double MoveStep(Dot[] dots, Tuple<int, int>[] linked, Tuple<int, int>[] unlinked, double linkDistance, double movePercent)
            {
                ShortPair[] linkedLengths = GetLengths(dots, linked);
                ShortPair[] unlinkedLengths = GetLengths(dots, unlinked);

                VectorND[] forces = Enumerable.Range(0, dots.Length).
                    Select(o => new VectorND(dots[0].Position.Size)).
                    ToArray();

                #region push/pull all linked

                foreach (ShortPair link in linkedLengths)
                {
                    // Less than 1 is repulsive, Greater than 1 is attractive
                    double force = (linkDistance - link.Length) / linkDistance;

                    VectorND forceVect = (link.Link / link.Length) * force;

                    forces[link.Index1] -= forceVect;
                    forces[link.Index2] += forceVect;
                }

                #endregion
                #region push away all unlinked

                foreach (ShortPair link in unlinkedLengths)
                {
                    double scaledLength = (link.Length / linkDistance);

                    double force = 1 / (scaledLength * scaledLength);

                    VectorND forceVect = (link.Link / link.Length) * force;

                    forces[link.Index1] -= forceVect;
                    forces[link.Index2] += forceVect;
                }

                #endregion

                #region apply forces

                // They aren't really forces, just dispacements

                double maxForce = linkDistance * 4;

                for (int cntr = 0; cntr < forces.Length; cntr++)
                {
                    // Cap the force to avoid instability
                    VectorND displace = forces[cntr];
                    if (displace.LengthSquared > maxForce * maxForce)
                    {
                        displace = displace.ToUnit(false) * maxForce;
                    }

                    dots[cntr].Position += displace * movePercent;
                }

                #endregion

                double retVal = forces.Max(o => o.LengthSquared);
                retVal = Math.Sqrt(movePercent);
                //retVal *= movePercent;        // don't want to do this, because it makes the error appear smaller

                return retVal;
            }

            #endregion
            #region Private Methods - standard cube

            private static VectorND[] GetCube_Finish(Dot[] dots, Tuple<VectorND, VectorND> aabb, double stopRadiusPercent, int stopIterationCount)
            {
                const double MOVEPERCENT = .1;

                double radius = (aabb.Item2 - aabb.Item1).Length / 2;
                double stopAmount = radius * stopRadiusPercent;

                CapToCube(dots, aabb);
                RandomShift(dots, radius * SHIFTMULT);

                double? minDistance = GetMinDistance(dots, radius, aabb);

                for (int cntr = 0; cntr < stopIterationCount; cntr++)
                {
                    double amountMoved = MoveStep(dots, MOVEPERCENT, aabb, minDistance);
                    if (amountMoved < stopAmount)
                    {
                        break;
                    }
                }

                //NOTE: The movable dots are always the front of the list, so the returned array will be the same positions as the initial movable array
                return dots.
                    Where(o => !o.IsStatic).
                    Select(o => o.Position).
                    ToArray();
            }

            private static double MoveStep(IList<Dot> dots, double percent, Tuple<VectorND, VectorND> aabb, double? minDistance)
            {
                // Find shortest pair lengths
                ShortPair[] shortPairs = GetShortestPair(dots);
                if (shortPairs.Length == 0)
                {
                    return 0;
                }

                // Move the shortest pair away from each other (based on how far they are away from the avg)
                double avg = shortPairs.Average(o => o.LengthRatio);

                // Artificially increase repulsive pressure
                if (minDistance != null && avg < minDistance.Value)
                {
                    avg = minDistance.Value;
                }

                double distToMoveMax = avg - shortPairs[0].LengthRatio;
                if (distToMoveMax.IsNearZero())
                {
                    // Found equilbrium
                    return 0;
                }

                double retVal = 0;

                bool isFirst = true;
                foreach (ShortPair pair in shortPairs)
                {
                    // Only want to move them if they are less than average
                    if (pair.LengthRatio >= avg)
                    {
                        break;      // they are sorted, so the rest of the list will also be greater
                    }

                    double distance = MovePair(ref isFirst, pair, percent, distToMoveMax, avg, dots, aabb, true);

                    retVal = Math.Max(retVal, distance);
                }

                return retVal;
            }

            private static ShortPair[] GetShortestPair(IList<Dot> dots)
            {
                List<ShortPair> retVal = new List<ShortPair>();

                for (int outer = 0; outer < dots.Count - 1; outer++)
                {
                    ShortPair currentShortest = null;

                    for (int inner = outer + 1; inner < dots.Count; inner++)
                    {
                        if (dots[outer].IsStatic && dots[inner].IsStatic)
                        {
                            continue;
                        }

                        VectorND link = dots[inner].Position - dots[outer].Position;
                        double length = link.Length;
                        double avgMult = (dots[inner].RepulseMultiplier + dots[outer].RepulseMultiplier) / 2d;
                        double ratio = length / avgMult;

                        if (currentShortest == null || ratio < currentShortest.LengthRatio)
                        {
                            currentShortest = new ShortPair(outer, inner, length, ratio, avgMult, link);
                        }
                    }

                    if (currentShortest != null)
                    {
                        retVal.Add(currentShortest);
                    }
                }

                return retVal.
                    OrderBy(o => o.LengthRatio).
                    ToArray();
            }

            #endregion
            #region Private Methods

            private static Dot[] GetDots(int movableCount, VectorND[] staticPoints, Tuple<VectorND, VectorND> aabb, double[] movableRepulseMultipliers, double[] staticRepulseMultipliers)
            {
                // Seed the movable ones with random locations (that's the best that can be done right now)
                VectorND[] movable = Enumerable.Range(0, movableCount).
                    Select(o => MathND.GetRandomVector_Cube(aabb)).
                    ToArray();

                // Call the other overload
                return GetDots(movable, staticPoints, movableRepulseMultipliers, staticRepulseMultipliers);
            }
            private static Dot[] GetDots(VectorND[] movable, VectorND[] staticPoints, double[] movableRepulseMultipliers, double[] staticRepulseMultipliers)
            {
                int movableCount = movable.Length;

                if (movableRepulseMultipliers != null && movableRepulseMultipliers.Length != movableCount)
                {
                    throw new ArgumentOutOfRangeException("movableRepulseMultipliers", "When movableRepulseMultipliers is nonnull, it must be the same length as the number of movable points");
                }

                // Figure out how big to make the return array
                int length = movableCount;
                if (staticPoints != null)
                {
                    length += staticPoints.Length;

                    if (staticRepulseMultipliers != null && staticRepulseMultipliers.Length != staticPoints.Length)
                    {
                        throw new ArgumentOutOfRangeException("staticRepulseMultipliers", "When staticRepulseMultipliers is nonnull, it must be the same length as the number of static points");
                    }
                }

                Dot[] retVal = new Dot[length];

                // Copy the moveable ones
                for (int cntr = 0; cntr < movableCount; cntr++)
                {
                    retVal[cntr] = new Dot(false, movable[cntr], movableRepulseMultipliers == null ? 1d : movableRepulseMultipliers[cntr]);
                }

                // Add the static points to the end
                if (staticPoints != null)
                {
                    for (int cntr = 0; cntr < staticPoints.Length; cntr++)
                    {
                        retVal[movableCount + cntr] = new Dot(true, staticPoints[cntr], staticRepulseMultipliers == null ? 1d : staticRepulseMultipliers[cntr]);
                    }
                }

                return retVal;
            }

            private static double MovePair(ref bool isFirst, ShortPair pair, double percent, double distToMoveMax, double avg, IList<Dot> dots, Tuple<VectorND, VectorND> aabb, bool isAway)
            {
                // Figure out how far they should move
                double actualPercent, distToMoveRatio;
                if (isFirst)
                {
                    actualPercent = percent;
                    distToMoveRatio = distToMoveMax;
                }
                else
                {
                    distToMoveRatio = isAway ? avg - pair.LengthRatio : pair.LengthRatio - avg;
                    actualPercent = (distToMoveRatio / distToMoveMax) * percent;        // don't use the full percent.  Reduce it based on the ratio of this distance with the max distance
                }

                isFirst = false;

                double moveDist = distToMoveRatio * actualPercent * pair.AvgMult;

                // Unit vector
                VectorND displaceUnit;
                if (pair.Length.IsNearZero())
                {
                    displaceUnit = MathND.GetRandomVector_Cube(aabb.Item1.Size, -1, 1);
                    displaceUnit.Normalize(false);
                }
                else
                {
                    displaceUnit = pair.Link.ToUnit(false);
                }

                // Can't move evenly.  Divide it up based on the ratio of multipliers
                double sumMult = dots[pair.Index1].RepulseMultiplier + dots[pair.Index2].RepulseMultiplier;
                double percent2 = dots[pair.Index1].RepulseMultiplier / sumMult;      // flipping 1 and 2, because if 1 is bigger, 2 needs to move more
                double percent1 = 1 - percent2;

                // Move dots
                Dot dot = dots[pair.Index1];
                if (!dot.IsStatic)
                {
                    VectorND displace = displaceUnit * (moveDist * percent1);
                    if (isAway)
                    {
                        dot.Position -= displace;
                    }
                    else
                    {
                        dot.Position += displace;
                    }
                    CapToCube(dot, aabb);
                }

                dot = dots[pair.Index2];
                if (!dot.IsStatic)
                {
                    VectorND displace = displaceUnit * (moveDist * percent2);
                    if (isAway)
                    {
                        dot.Position += displace;
                    }
                    else
                    {
                        dot.Position -= displace;
                    }
                    CapToCube(dot, aabb);
                }

                return moveDist;
            }

            private static void CapToCube(IEnumerable<Dot> dots, Tuple<VectorND, VectorND> aabb)
            {
                foreach (Dot dot in dots)
                {
                    CapToCube(dot, aabb);
                }
            }
            private static void CapToCube(Dot dot, Tuple<VectorND, VectorND> aabb)
            {
                if (dot.IsStatic)
                {
                    return;
                }

                for (int cntr = 0; cntr < aabb.Item1.Size; cntr++)
                {
                    if (dot.Position[cntr] < aabb.Item1[cntr])
                    {
                        dot.Position[cntr] = aabb.Item1[cntr];
                    }
                    else if (dot.Position[cntr] > aabb.Item2[cntr])
                    {
                        dot.Position[cntr] = aabb.Item2[cntr];
                    }
                }
            }

            private static void RandomShift(Dot[] dots, double max)
            {
                for (int cntr = 0; cntr < dots.Length; cntr++)
                {
                    dots[cntr].Position += MathND.GetRandomVector_Cube(dots[cntr].Position.Size, -max, max);
                }
            }

            /// <summary>
            /// Without this, a 2 point request will never pull from each other
            /// </summary>
            /// <remarks>
            /// I didn't experiment too much with these values, but they seem pretty good
            /// </remarks>
            private static double? GetMinDistance(Dot[] dots, double radius, Tuple<VectorND, VectorND> aabb)
            {
                int dimensions = aabb.Item1.Size;

                double numerator = radius * 3d / 2d;
                double divisor = Math.Pow(dots.Length, 1d / dimensions);

                return numerator / divisor;
            }

            private static Tuple<int, int>[] GetCleanedLinks(Tuple<int, int>[] links)
            {
                return links.
                    Where(o => o.Item1 != o.Item2).
                    Select(o => o.Item1 < o.Item2 ? o : Tuple.Create(o.Item2, o.Item1)).        // ensure item1 is less than item2
                    Distinct().
                    OrderBy(o => o.Item1).
                    ThenBy(o => o.Item2).
                    ToArray();
            }

            private static ShortPair[] GetLengths(IList<Dot> dots, Tuple<int, int>[] links)
            {
                return links.
                    Select(o =>
                    {
                        VectorND line = dots[o.Item2].Position - dots[o.Item1].Position;
                        double length = line.Length;
                        return new ShortPair(o.Item1, o.Item2, length, length, 1d, line);       // this class is designed to handle different sized dots, so they repel each other with different ratios.  See GetShortestPair()
                    }).
                    OrderBy(o => o.LengthRatio).
                    ToArray();
            }

            #endregion
        }

        #endregion

        #region Simple

        /// <summary>
        /// Get a random vector between boundry lower and boundry upper
        /// </summary>
        public static VectorND GetRandomVector(VectorND boundryLower, VectorND boundryUpper)
        {
            double[] bndLow = boundryLower.VectorArray;
            double[] bndUp = boundryUpper.VectorArray;

            double[] retVal = new double[bndLow.Length];

            Random rand = StaticRandom.GetRandomForThread();

            for (int cntr = 0; cntr < retVal.Length; cntr++)
            {
                retVal[cntr] = rand.NextDouble(bndLow[cntr], bndUp[cntr]);
            }

            return new VectorND(retVal);
        }

        public static VectorND[] Clone(IEnumerable<VectorND> vectors)
        {
            return vectors.
                Select(o => o.Clone()).
                ToArray();
        }

        #endregion

        #region Random

        public static VectorND GetRandomVector_Cube(Tuple<VectorND, VectorND> aabb)
        {
            return GetRandomVector(aabb.Item1, aabb.Item2);
        }
        public static VectorND GetRandomVector_Cube(int dimensions, double min, double max)
        {
            Random rand = StaticRandom.GetRandomForThread();

            double[] retVal = new double[dimensions];

            for (int cntr = 0; cntr < retVal.Length; cntr++)
            {
                retVal[cntr] = rand.NextDouble(min, max);
            }

            return new VectorND(retVal);
        }

        public static VectorND[] GetRandomVectors_Cube_EventDist(int returnCount, Tuple<VectorND, VectorND> aabb, double[] movableRepulseMultipliers = null, VectorND[] existingStaticPoints = null, double[] staticRepulseMultipliers = null, double stopRadiusPercent = .004, int stopIterationCount = 1000)
        {
            return EvenDistribution.GetCube(returnCount, aabb, stopRadiusPercent, stopIterationCount, movableRepulseMultipliers, existingStaticPoints, staticRepulseMultipliers);
        }
        public static VectorND[] GetRandomVectors_Cube_EventDist(VectorND[] movable, Tuple<VectorND, VectorND> aabb, double[] movableRepulseMultipliers = null, VectorND[] existingStaticPoints = null, double[] staticRepulseMultipliers = null, double stopRadiusPercent = .004, int stopIterationCount = 1000)
        {
            return EvenDistribution.GetCube(movable, aabb, stopRadiusPercent, stopIterationCount, movableRepulseMultipliers, existingStaticPoints, staticRepulseMultipliers);
        }
        public static VectorND[] GetRandomVectors_Cube_EventDist(VectorND[] movable, Tuple<int, int>[] links, Tuple<VectorND, VectorND> aabb, double stopRadiusPercent = .004, int stopIterationCount = 1000, double linkedMult = .2, double unlinkedMult = .4)
        {
            return EvenDistribution.GetCube(movable, links, aabb, stopRadiusPercent, stopIterationCount, linkedMult, unlinkedMult);
        }

        public static VectorND[] GetRandomVectors_Open_EventDist(VectorND[] movable, Tuple<int, int>[] links, double linkDistance = 1d, double stopRadiusPercent = .004, int stopIterationCount = 1000)
        {
            if (linkDistance < 1d)
            {
                throw new ArgumentOutOfRangeException("linkDistance", linkDistance, "linkDistance can't be less than 1 (things get unstable): " + linkDistance.ToString());
            }

            return EvenDistribution.GetOpenLinked(movable, links, linkDistance, stopRadiusPercent, stopIterationCount);
        }

        #endregion

        #region Misc

        public static Tuple<VectorND, VectorND> GetAABB(IEnumerable<VectorND> points)
        {
            VectorND first = points.FirstOrDefault();
            if (first == null)
            {
                throw new ArgumentException("The list of points is empty.  Can't determine number of dimensions");
            }

            double[] min = Enumerable.Range(0, first.Size).
                Select(o => double.MaxValue).
                ToArray();

            double[] max = Enumerable.Range(0, first.Size).
                Select(o => double.MinValue).
                ToArray();

            foreach (VectorND point in points)
            {
                double[] pointArr = point.VectorArray;

                for (int cntr = 0; cntr < pointArr.Length; cntr++)
                {
                    if (pointArr[cntr] < min[cntr])
                    {
                        min[cntr] = pointArr[cntr];
                    }

                    if (pointArr[cntr] > max[cntr])
                    {
                        max[cntr] = pointArr[cntr];
                    }
                }
            }

            return Tuple.Create(new VectorND(min), new VectorND(max));
        }

        /// <summary>
        /// This changes the size of aabb by percent
        /// </summary>
        /// <param name="percent">If less than 1, this will reduce the size.  If greater than 1, this will increase the size</param>   
        public static Tuple<VectorND, VectorND> ResizeAABB(Tuple<VectorND, VectorND> aabb, double percent)
        {
            VectorND center = GetCenter(new[] { aabb.Item1, aabb.Item2 });

            VectorND dirMin = (aabb.Item1 - center) * percent;
            VectorND dirMax = (aabb.Item2 - center) * percent;

            return Tuple.Create(center + dirMin, center + dirMax);
        }

        /// <summary>
        /// This returns the center of position of the points
        /// </summary>
        public static VectorND GetCenter(IEnumerable<VectorND> points)
        {
            if (points == null)
            {
                throw new ArgumentException("Unknown number of dimensions");
            }

            VectorND retVal = null;

            int length = 0;

            foreach (VectorND point in points)
            {
                if (retVal == null)
                {
                    retVal = new VectorND(point.Size);
                }

                // Add this point to the total
                retVal += point;

                length++;
            }

            if (length == 0)
            {
                throw new ArgumentException("Unknown number of dimensions");
            }

            // Divide by count
            retVal /= Convert.ToDouble(length);

            return retVal;
        }

        /// <remarks>
        /// http://www.mathsisfun.com/data/standard-deviation.html
        /// </remarks>
        public static double GetStandardDeviation(IEnumerable<VectorND> values)
        {
            VectorND mean = GetCenter(values);

            // Variance is the average of the of the distance squared from the mean
            double variance = values.
                Select(o => (o - mean).LengthSquared).
                Average();

            return Math.Sqrt(variance);
        }

        public static Tuple<int, int, double>[] GetDistancesBetween(VectorND[] positions)
        {
            List<Tuple<int, int, double>> retVal = new List<Tuple<int, int, double>>();

            for (int outer = 0; outer < positions.Length - 1; outer++)
            {
                for (int inner = outer + 1; inner < positions.Length; inner++)
                {
                    retVal.Add(Tuple.Create(outer, inner, (positions[outer] - positions[inner]).Length));
                }
            }

            return retVal.ToArray();
        }

        public static VectorND[] ApplyBallOfSprings(VectorND[] positions, Tuple<int, int, double>[] desiredDistances, int numIterations)
        {
            return BallOfSprings.ApplyBallOfSprings(positions, desiredDistances, numIterations);
        }

        public static bool IsInside(Tuple<VectorND, VectorND> aabb, VectorND testPoint)
        {
            double[] min = aabb.Item1.VectorArray;
            double[] max = aabb.Item2.VectorArray;
            double[] test = testPoint.VectorArray;

            for (int cntr = 0; cntr < test.Length; cntr++)
            {
                if (test[cntr] < min[cntr] || test[cntr] > max[cntr])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// This converts a base10 number into an arbitrary base
        /// </summary>
        /// <remarks>
        /// Silly example:
        ///     136 converted to base 10 will return { 1, 3, 6 }
        /// 
        /// Other examples:
        ///     5 -> base2 = { 1, 0, 1 }
        ///     6 -> base2 = { 1, 1, 0 }
        ///     
        ///     7 -> base16 = { 7 }
        ///     15 -> base16 = { 15 }
        ///     18 -> base16 = { 1, 2 }
        /// </remarks>
        /// <param name="base10">A number in base10</param>
        /// <param name="baseConvertTo">The base to convert to</param>
        public static int[] ConvertToBase(long base10, int baseConvertTo)
        {
            List<int> retVal = new List<int>();

            long num = base10;

            while (num != 0)
            {
                int remainder = Convert.ToInt32(num % baseConvertTo);
                num = num / baseConvertTo;
                retVal.Add(remainder);
            }

            retVal.Reverse();

            return retVal.ToArray();
        }

        /// <summary>
        /// This returns the "radius" of a cube
        /// TODO: Find the radius of a convex polygon instead of a cube
        /// </summary>
        public static double GetRadius(IEnumerable<VectorND> points)
        {
            return GetRadius(GetAABB(points));
        }
        /// <summary>
        /// This returns the "radius" of a cube
        /// </summary>
        public static double GetRadius(Tuple<VectorND, VectorND> aabb)
        {
            // Diameter of the circumscribed sphere
            double circumscribedDiam = (aabb.Item1 - aabb.Item2).Length;

            // Diameter of inscribed sphere
            double inscribedDiam = 0;
            for (int cntr = 0; cntr < aabb.Item1.Size; cntr++)
            {
                inscribedDiam += aabb.Item2[cntr] - aabb.Item1[cntr];
            }
            inscribedDiam /= aabb.Item1.Size;

            // Return the average of the two
            //return (circumscribedDiam + inscribedDiam) / 4d;        // avg=sum/2, radius=diam/2, so divide by 4

            // The problem with taking the average of the circumscribed and inscribed radius, is that circumscribed grows
            // roughly sqrt(dimensions), but inscribed is constant.  So the more dimensions, the more innacurate inscribed
            // will be
            //
            // But just using circumscribed will always be overboard
            //return circumscribedDiam / 2;

            // Weighted average.  Dividing by 2 to turn diameter into radius
            return ((circumscribedDiam * .85) + (inscribedDiam * .15)) / 2d;
        }

        #endregion
    }

    #region class: VectorND

    public class VectorND : IEnumerable<double>
    {
        #region Declaration Section

        private readonly object _lock = new object();

        #endregion

        #region Constructor

        public VectorND() { }

        public VectorND(int size)
            : this(new double[size]) { }

        public VectorND(double[] vector)
        {
            lock (_lock)
            {
                _vectorArray = vector;
            }
        }

        #endregion

        #region IEnumerable Members

        //NOTE: These iterate on a copy so the lock doesn't stay open

        public IEnumerator<double> GetEnumerator()
        {
            double[] copy = null;
            lock (_lock)
            {
                copy = _vectorArray.ToArray();
            }

            if (copy == null)
            {
                throw new InvalidOperationException("VectorArray hasn't been assigned yet");
            }

            foreach (double number in copy)
            {
                yield return number;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            double[] copy = null;
            lock (_lock)
            {
                copy = _vectorArray.ToArray();
            }

            if (copy == null)
            {
                throw new InvalidOperationException("VectorArray hasn't been assigned yet");
            }

            return copy.GetEnumerator();
        }

        #endregion

        #region Public Properties

        private double[] _vectorArray = null;
        public double[] VectorArray
        {
            get
            {
                lock (_lock)
                {
                    return _vectorArray;
                }
            }
            set
            {
                lock (_lock)
                {
                    if (value == null)
                    {
                        throw new ArgumentNullException("Can't set vector array back to null");
                    }
                    else if (_vectorArray != null && _vectorArray.Length != value.Length)
                    {
                        throw new ArgumentException(string.Format("VectorArray has already been assigned a different size.  existing={0} attempted new={1}", _vectorArray.Length, value.Length));
                    }

                    _vectorArray = value;
                }
            }
        }

        public double this[int index]
        {
            get
            {
                lock (_lock)
                {
                    if (_vectorArray == null)
                    {
                        throw new InvalidOperationException("VectorArray hasn't been assigned yet");
                    }
                    else if (index < 0 || index >= _vectorArray.Length)
                    {
                        throw new ArgumentOutOfRangeException(string.Format("Index is out of range.  array size={0}, index={1}", _vectorArray.Length, index));
                    }

                    return _vectorArray[index];
                }
            }
            set
            {
                lock (_lock)
                {
                    if (_vectorArray == null)
                    {
                        throw new InvalidOperationException("VectorArray hasn't been assigned yet");
                    }
                    else if (index < 0 || index >= _vectorArray.Length)
                    {
                        throw new ArgumentOutOfRangeException(string.Format("Index is out of range.  array size={0}, index={1}", _vectorArray.Length, index));
                    }

                    _vectorArray[index] = value;
                }
            }
        }

        /// <summary>
        /// This is the number of dimensions (a 3D vector would return 3)
        /// </summary>
        public int Size
        {
            get
            {
                lock (_lock)
                {
                    if (_vectorArray == null)
                    {
                        throw new InvalidOperationException("VectorArray hasn't been assigned yet");
                    }

                    return _vectorArray.Length;
                }
            }
        }

        public double Length
        {
            get
            {
                return Math.Sqrt(this.LengthSquared);
            }
        }
        public double LengthSquared
        {
            get
            {
                lock (_lock)
                {
                    if (_vectorArray == null)
                    {
                        throw new InvalidOperationException("VectorArray hasn't been assigned yet");
                    }

                    return GetLengthSquared(_vectorArray);
                }
            }
        }

        #endregion

        #region Public Methods

        public static VectorND Add(VectorND vector1, VectorND vector2)
        {
            double[] arr1 = vector1.VectorArray;        // grabbing the arrays to reduce locks, and more thread safe
            double[] arr2 = vector2.VectorArray;

            if (arr1 == null || arr2 == null)
            {
                throw new ArgumentException(string.Format("One of the arrays is still null: arr1={0}, arr2={2}", arr1 == null ? "<null>" : "len " + arr1.Length.ToString(), arr2 == null ? "<null>" : "len " + arr2.Length.ToString()));
            }
            else if (arr1.Length != arr2.Length)
            {
                throw new ArgumentException(string.Format("Vector sizes are different: size1={0}, size2={1}", arr1.Length, arr2.Length));
            }

            double[] retVal = new double[arr1.Length];

            for (int cntr = 0; cntr < arr1.Length; cntr++)
            {
                retVal[cntr] = arr1[cntr] + arr2[cntr];
            }

            return new VectorND(retVal);
        }
        /// <summary>
        /// Subtracts the specified vector from another specified vector (return v1 - v2)
        /// </summary>
        /// <param name="vector1">The vector from which vector2 is subtracted</param>
        /// <param name="vector2">The vector to subtract from vector1</param>
        /// <returns>The difference between vector1 and vector2</returns>
        public static VectorND Subtract(VectorND vector1, VectorND vector2)
        {
            double[] arr1 = vector1.VectorArray;        // grabbing the arrays to reduce locks, and more thread safe
            double[] arr2 = vector2.VectorArray;

            if (arr1 == null || arr2 == null)
            {
                throw new ArgumentException(string.Format("One of the arrays is still null: arr1={0}, arr2={2}", arr1 == null ? "<null>" : "len " + arr1.Length.ToString(), arr2 == null ? "<null>" : "len " + arr2.Length.ToString()));
            }
            else if (arr1.Length != arr2.Length)
            {
                throw new ArgumentException(string.Format("Vector sizes are different: size1={0}, size2={1}", arr1.Length, arr2.Length));
            }

            double[] retVal = new double[arr1.Length];

            for (int cntr = 0; cntr < arr1.Length; cntr++)
            {
                retVal[cntr] = arr1[cntr] - arr2[cntr];
            }

            return new VectorND(retVal);
        }
        public static VectorND Multiply(VectorND vector, double scalar)
        {
            double[] arr = vector.VectorArray;        // grabbing the array to reduce locks, and more thread safe

            if (arr == null)
            {
                throw new ArgumentException("Array is still null");
            }

            double[] retVal = new double[arr.Length];

            for (int cntr = 0; cntr < arr.Length; cntr++)
            {
                retVal[cntr] = arr[cntr] * scalar;
            }

            return new VectorND(retVal);
        }
        public static VectorND Divide(VectorND vector, double scalar)
        {
            double[] arr = vector.VectorArray;        // grabbing the array to reduce locks, and more thread safe

            if (arr == null)
            {
                throw new ArgumentException("Array is still null");
            }

            double[] retVal = new double[arr.Length];

            for (int cntr = 0; cntr < arr.Length; cntr++)
            {
                retVal[cntr] = arr[cntr] / scalar;
            }

            return new VectorND(retVal);
        }

        public VectorND Clone()
        {
            lock (_lock)
            {
                if (_vectorArray == null)
                {
                    throw new InvalidOperationException("VectorArray hasn't been assigned yet");
                }

                return new VectorND(_vectorArray.ToArray());
            }
        }

        public void Negate()
        {
            lock (_lock)
            {
                if (_vectorArray == null)
                {
                    throw new InvalidOperationException("VectorArray hasn't been assigned yet");
                }

                for (int cntr = 0; cntr < _vectorArray.Length; cntr++)
                {
                    _vectorArray[cntr] = -_vectorArray[cntr];
                }
            }
        }
        public VectorND ToNegated()
        {
            VectorND retVal = Clone();

            retVal.Negate();

            return retVal;
        }

        public void Normalize(bool useNaNIfInvalid)
        {
            lock (_lock)
            {
                if (_vectorArray == null)
                {
                    throw new InvalidOperationException("VectorArray hasn't been assigned yet");
                }

                double length = Math.Sqrt(GetLengthSquared(_vectorArray));
                if (Math1D.IsNearZero(length) || Math1D.IsNearValue(length, 1))
                {
                    return;
                }
                else if (Math1D.IsInvalid(length))
                {
                    for (int cntr = 0; cntr < _vectorArray.Length; cntr++)
                    {
                        if (useNaNIfInvalid)
                        {
                            _vectorArray[cntr] = double.NaN;
                        }
                        else
                        {
                            _vectorArray[cntr] = 0;
                        }
                    }
                }
                else
                {
                    for (int cntr = 0; cntr < _vectorArray.Length; cntr++)
                    {
                        _vectorArray[cntr] = _vectorArray[cntr] / length;
                    }
                }
            }
        }
        public VectorND ToUnit(bool useNaNIfInvalid)
        {
            VectorND retVal = Clone();

            retVal.Normalize(useNaNIfInvalid);

            return retVal;
        }

        /// <summary>
        /// This scales the values between -1 and 1
        /// NOTE: This is different than Normalize,ToUnit
        /// </summary>
        public void ScaledCap()
        {
            lock (_lock)
            {
                if (_vectorArray == null)
                {
                    throw new InvalidOperationException("VectorArray hasn't been assigned yet");
                }

                double min = _vectorArray.Min();
                double max = _vectorArray.Max();

                double minReturn = min <= 0 ? -1 : 0;
                double maxReturn = max <= 0 ? 0 : 1;

                for (int cntr = 0; cntr < _vectorArray.Length; cntr++)
                {
                    _vectorArray[cntr] = UtilityCore.GetScaledValue(minReturn, maxReturn, min, max, _vectorArray[cntr]);
                }
            }
        }
        public VectorND ToScaledCap()
        {
            VectorND retVal = Clone();

            retVal.ScaledCap();

            return retVal;
        }

        public static double DotProduct(VectorND vector1, VectorND vector2)
        {
            double[] arr1 = vector1.VectorArray;        // grabbing the arrays to reduce locks, and more thread safe
            double[] arr2 = vector2.VectorArray;

            if (arr1 == null || arr2 == null)
            {
                throw new ArgumentException(string.Format("One of the arrays is still null: arr1={0}, arr2={2}", arr1 == null ? "<null>" : "len " + arr1.Length.ToString(), arr2 == null ? "<null>" : "len " + arr2.Length.ToString()));
            }
            else if (arr1.Length != arr2.Length)
            {
                throw new ArgumentException(string.Format("Vector sizes are different: size1={0}, size2={1}", arr1.Length, arr2.Length));
            }

            double retVal = 0;

            for (int cntr = 0; cntr < arr1.Length; cntr++)
            {
                retVal += arr1[cntr] * arr2[cntr];
            }

            return retVal;
        }

        /// <summary>
        /// This compares the values at each index
        /// </summary>
        public static bool Equals(VectorND vector1, VectorND vector2)
        {
            // If both are null, or both are same instance, return true.
            if (System.Object.ReferenceEquals(vector1, vector2))
            {
                return true;
            }

            //if (vector1 == null && vector2 == null)       // this == calls VectorND's == operator overload, which comes back here...stack overflow
            if ((object)vector1 == null && (object)vector2 == null)
            {
                return true;
            }
            else if ((object)vector1 == null || (object)vector2 == null)
            {
                return false;
            }

            double[] arr1 = vector1.VectorArray;        // grabbing the arrays to reduce locks, and more thread safe
            double[] arr2 = vector2.VectorArray;

            if (arr1 == null || arr2 == null)
            {
                throw new ArgumentException(string.Format("One of the arrays is still null: arr1={0}, arr2={2}", arr1 == null ? "<null>" : "len " + arr1.Length.ToString(), arr2 == null ? "<null>" : "len " + arr2.Length.ToString()));
            }
            else if (arr1.Length != arr2.Length)
            {
                return false;
            }

            for (int cntr = 0; cntr < arr1.Length; cntr++)
            {
                if (arr1[cntr] != arr2[cntr])
                {
                    return false;
                }
            }

            return true;
        }
        public bool Equals(VectorND vector)
        {
            return VectorND.Equals(this, vector);
        }
        public override bool Equals(object obj)
        {
            return VectorND.Equals(this, obj as VectorND);
        }

        public override int GetHashCode()
        {
            lock (_lock)
            {
                if (_vectorArray == null)
                {
                    return 0;
                }
                else
                {
                    return _vectorArray.GetHashCode();
                }
            }
        }

        public static bool IsNearZero(VectorND vector)
        {
            double[] arr = vector.VectorArray;        // grabbing the arrays to reduce locks, and more thread safe

            if (arr == null)
            {
                throw new ArgumentException("Array is still null");
            }

            for (int cntr = 0; cntr < arr.Length; cntr++)
            {
                if (Math.Abs(arr[cntr]) > Math1D.NEARZERO)
                {
                    return false;
                }
            }

            return true;
        }
        public static bool IsNearValue(VectorND vector1, VectorND vector2)
        {
            double[] arr1 = vector1.VectorArray;        // grabbing the arrays to reduce locks, and more thread safe
            double[] arr2 = vector2.VectorArray;

            if (arr1 == null || arr2 == null)
            {
                throw new ArgumentException(string.Format("One of the arrays is still null: arr1={0}, arr2={2}", arr1 == null ? "<null>" : "len " + arr1.Length.ToString(), arr2 == null ? "<null>" : "len " + arr2.Length.ToString()));
            }
            else if (arr1.Length != arr2.Length)
            {
                throw new ArgumentException(string.Format("Vector sizes are different: size1={0}, size2={1}", arr1.Length, arr2.Length));
            }

            for (int cntr = 0; cntr < arr1.Length; cntr++)
            {
                if (arr1[cntr] < arr2[cntr] - Math1D.NEARZERO || arr1[cntr] > arr2[cntr] + Math1D.NEARZERO)
                {
                    return false;
                }
            }

            return true;
        }

        public Point ToPoint()
        {
            double[] vector = this.VectorArray;
            if (vector == null || vector.Length != 2)
            {
                throw new InvalidOperationException("This vector isn't set up to return a 2D point: " + vector == null ? "null" : vector.Length.ToString());
            }

            return new Point(vector[0], vector[1]);
        }
        public Vector ToVector()
        {
            double[] vector = this.VectorArray;
            if (vector == null || vector.Length != 2)
            {
                throw new InvalidOperationException("This vector isn't set up to return a 2D vector: " + vector == null ? "null" : vector.Length.ToString());
            }

            return new Vector(vector[0], vector[1]);
        }
        public Point3D ToPoint3D()
        {
            double[] vector = this.VectorArray;
            if (vector == null || vector.Length != 3)
            {
                throw new InvalidOperationException("This vector isn't set up to return a 3D point: " + vector == null ? "null" : vector.Length.ToString());
            }

            return new Point3D(vector[0], vector[1], vector[2]);
        }
        public Vector3D ToVector3D()
        {
            double[] vector = this.VectorArray;
            if (vector == null || vector.Length != 3)
            {
                throw new InvalidOperationException("This vector isn't set up to return a 3D vector: " + vector == null ? "null" : vector.Length.ToString());
            }

            return new Vector3D(vector[0], vector[1], vector[2]);
        }

        public override string ToString()
        {
            double[] vector = this.VectorArray;
            if (vector == null)
            {
                return "null";
            }
            else
            {
                return vector.
                    Select(o => o.ToString()).
                    ToJoin(", ");
            }
        }

        //public static double AngleBetween(VectorND vector1, VectorND vector2)
        //{
        //    //http://math.stackexchange.com/questions/1267817/angle-between-two-4d-vectors
        //}
        //public static VectorND CrossProduct(VectorND[] vectors)
        //{
        //    //A cross product of k vectors is defined in k + 1 dimensional space. So, for a cross product in two dimensions, you need
        //    //one vector.  In 3D, you need two vectors, 4D you need 3 vectors and so forth.
        //    //https://www.gamedev.net/topic/181474-4-dimensional-crossproduct/
        //}

        #endregion

        #region Operator Overloads

        public static VectorND operator +(VectorND vector1, VectorND vector2)
        {
            return VectorND.Add(vector1, vector2);
        }

        public static VectorND operator -(VectorND vector)
        {
            return vector.ToNegated();
        }

        public static VectorND operator -(VectorND vector1, VectorND vector2)
        {
            return VectorND.Subtract(vector1, vector2);
        }

        public static VectorND operator *(double scalar, VectorND vector)
        {
            return VectorND.Multiply(vector, scalar);
        }
        public static VectorND operator *(VectorND vector, double scalar)
        {
            return VectorND.Multiply(vector, scalar);
        }

        public static VectorND operator /(VectorND vector, double scalar)
        {
            return VectorND.Divide(vector, scalar);
        }

        public static bool operator ==(VectorND vector1, VectorND vector2)
        {
            return VectorND.Equals(vector1, vector2);
        }
        public static bool operator !=(VectorND vector1, VectorND vector2)
        {
            return !VectorND.Equals(vector1, vector2);
        }

        #endregion

        #region Private Methods

        private static double GetLengthSquared(double[] vector)
        {
            //copied from GetDistanceSquared for speed reasons

            //C^2 = (A1-A2)^2 + (B1-B2)^2 + .....

            double retVal = 0;

            for (int cntr = 0; cntr < vector.Length; cntr++)
            {
                retVal += vector[cntr] * vector[cntr];
            }

            return retVal;
        }

        #endregion
    }

    #endregion
}
