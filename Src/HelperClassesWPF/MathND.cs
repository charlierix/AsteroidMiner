using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;

namespace Game.HelperClassesWPF
{
    /// <summary>
    /// This uses double arrays as vectors, and can be any number of dimensions
    /// NOTE: Any method that takes multiple vectors in the params needs all vectors to be the same dimension
    /// </summary>
    public static class MathND
    {
        #region Class: BallOfSprings

        private static class BallOfSprings
        {
            public static double[][] ApplyBallOfSprings(double[][] positions, Tuple<int, int, double>[] desiredDistances, int numIterations)
            {
                const double MULT = .005;
                const double MAXSPEED = .05;
                const double MAXSPEED_SQUARED = MAXSPEED * MAXSPEED;

                double[][] retVal = positions.ToArray();

                for (int iteration = 0; iteration < numIterations; iteration++)
                {
                    // Get Forces (actually displacements, since there's no mass)
                    double[][] forces = GetForces(retVal, desiredDistances, MULT);

                    // Cap the speed
                    forces = forces.
                        Select(o => CapSpeed(o, MAXSPEED, MAXSPEED_SQUARED)).
                        ToArray();

                    // Move points
                    for (int cntr = 0; cntr < forces.Length; cntr++)
                    {
                        for (int i = 0; i < forces[cntr].Length; i++)
                        {
                            retVal[cntr][i] += forces[cntr][i];
                        }
                    }
                }

                return retVal;
            }

            #region Private Methods

            private static double[][] GetForces(double[][] positions, Tuple<int, int, double>[] desiredDistances, double mult)
            {
                // Calculate forces
                Tuple<int, double[]>[] forces = desiredDistances.
                    //AsParallel().     //TODO: if distances.Length > threshold, do this in parallel
                    SelectMany(o => GetForces_Calculate(o.Item1, o.Item2, o.Item3, positions, mult)).
                    ToArray();

                // Give them a very slight pull toward the origin so that the cloud doesn't drift away
                double[] center = MathND.GetCenter(positions);
                double centerMult = mult * -5;

                double[] centerPullForce = MathND.Multiply(center, centerMult);

                // Group by item
                var grouped = forces.
                    GroupBy(o => o.Item1).
                    OrderBy(o => o.Key);

                return grouped.
                    Select(o =>
                    {
                        double[] retVal = centerPullForce.ToArray();        // clone the center pull force

                        foreach (var force in o)
                        {
                            retVal = MathND.Add(retVal, force.Item2);
                        }

                        return retVal;
                    }).
                    ToArray();
            }
            private static Tuple<int, double[]>[] GetForces_Calculate(int index1, int index2, double desiredDistance, double[][] positions, double mult)
            {
                // Spring from 1 to 2
                double[] spring = MathND.Subtract(positions[index2], positions[index1]);
                double springLength = MathND.GetLength(spring);

                double difference = desiredDistance - springLength;
                difference *= mult;

                if (Math1D.IsNearZero(springLength) && !difference.IsNearZero())
                {
                    spring = GetRandomVector_Spherical_Shell(Math.Abs(difference), spring.Length);
                }
                else
                {
                    spring = MathND.Multiply(MathND.ToUnit(spring), Math.Abs(difference));
                }

                if (difference > 0)
                {
                    // Gap needs to be bigger, push them away (default is closing the gap)
                    spring = MathND.Negate(spring);
                }

                return new[]
                {
                    Tuple.Create(index1, spring),
                    Tuple.Create(index2, MathND.Negate(spring)),
                };
            }

            private static double[] GetRandomVector_Spherical_Shell(double radius, int numDimensions)
            {
                if (numDimensions == 2)
                {
                    Vector3D circle = Math3D.GetRandomVector_Circular_Shell(radius);
                    return new[] { circle.X, circle.Y };
                }
                else if (numDimensions == 3)
                {
                    Vector3D sphere = Math3D.GetRandomVector_Spherical_Shell(radius);
                    return new[] { sphere.X, sphere.Y, sphere.Z };
                }
                else
                {
                    //TODO: Figure out how to make this spherical instead of a cube
                    double[] retVal = new double[numDimensions];

                    for (int cntr = 0; cntr < numDimensions; cntr++)
                    {
                        retVal[cntr] = Math1D.GetNearZeroValue(radius);
                    }

                    retVal = MathND.Multiply(MathND.ToUnit(retVal, false), radius);

                    return retVal;
                }
            }

            private static double[] CapSpeed(double[] velocity, double maxSpeed, double maxSpeedSquared)
            {
                double lengthSquared = MathND.GetLengthSquared(velocity);

                if (lengthSquared < maxSpeedSquared)
                {
                    return velocity;
                }
                else
                {
                    double length = Math.Sqrt(lengthSquared);

                    double[] retVal = new double[velocity.Length];

                    //ToUnit * MaxSpeed
                    for (int cntr = 0; cntr < velocity.Length; cntr++)
                    {
                        retVal[cntr] = (velocity[cntr] / length) * maxSpeed;
                    }

                    return retVal;
                }
            }

            #endregion
        }

        #endregion

        #region Simple

        public static bool IsNearZero(double[] vector)
        {
            return vector.All(o => Math.Abs(o) <= Math3D.NEARZERO);
        }
        public static bool IsNearValue(double[] vector1, double[] vector2)
        {
            return Enumerable.Range(0, vector1.Length).
                All(o => vector1[o] >= vector2[o] - Math3D.NEARZERO && vector1[o] <= vector2[o] + Math3D.NEARZERO);
        }

        public static double GetLength(double[] vector)
        {
            return Math.Sqrt(GetLengthSquared(vector));
        }
        public static double GetLengthSquared(double[] vector)
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

        public static double GetDistance(double[] vector1, double[] vector2)
        {
            return Math.Sqrt(GetDistanceSquared(vector1, vector2));
        }
        public static double GetDistanceSquared(double[] vector1, double[] vector2)
        {
            #region validate
#if DEBUG
            if (vector1 == null || vector2 == null)
            {
                throw new ArgumentException("Vector arrays can't be null");
            }
            else if (vector1.Length != vector2.Length)
            {
                throw new ArgumentException("Vector arrays must be the same length " + vector1.Length + ", " + vector2.Length);
            }
#endif
            #endregion

            //C^2 = (A1-A2)^2 + (B1-B2)^2 + .....

            double retVal = 0;

            for (int cntr = 0; cntr < vector1.Length; cntr++)
            {
                double diff = vector1[cntr] - vector2[cntr];

                retVal += diff * diff;
            }

            return retVal;
        }

        /// <summary>
        /// This scales the values so that the length of the vector is one
        /// </summary>
        public static double[] ToUnit(double[] vector, bool useNaNIfInvalid = true)
        {
            double length = GetLength(vector);

            if (Math1D.IsNearValue(length, 1))
            {
                return vector;
            }

            if (Math1D.IsInvalid(length))
            {
                double[] retVal = new double[vector.Length];

                for (int cntr = 0; cntr < vector.Length; cntr++)
                {
                    if (useNaNIfInvalid)
                    {
                        retVal[cntr] = double.NaN;
                    }
                    else
                    {
                        retVal[cntr] = 0;
                    }
                }
            }

            return Divide(vector, length);
        }

        /// <summary>
        /// This scales the values between -1 and 1
        /// NOTE: This is different than ToUnit
        /// </summary>
        public static double[] Normalize(double[] vector)
        {
            double min = vector.Min();
            double max = vector.Max();

            double minReturn = min <= 0 ? -1 : 0;
            double maxReturn = max <= 0 ? 0 : 1;

            return vector.
                Select(o => UtilityCore.GetScaledValue(minReturn, maxReturn, min, max, o)).
                ToArray();
        }

        public static double[] Add(double[] vector1, double[] vector2)
        {
            double[] retVal = new double[vector1.Length];

            for (int cntr = 0; cntr < vector1.Length; cntr++)
            {
                retVal[cntr] = vector1[cntr] + vector2[cntr];
            }

            return retVal;
        }
        /// <summary>
        /// Subtracts the specified vector from another specified vector (return v1 - v2)
        /// </summary>
        /// <param name="vector1">The vector from which vector2 is subtracted</param>
        /// <param name="vector2">The vector to subtract from vector1</param>
        /// <returns>The difference between vector1 and vector2</returns>
        public static double[] Subtract(double[] vector1, double[] vector2)
        {
            double[] retVal = new double[vector1.Length];

            for (int cntr = 0; cntr < vector1.Length; cntr++)
            {
                retVal[cntr] = vector1[cntr] - vector2[cntr];
            }

            return retVal;
        }
        public static double[] Multiply(double[] vector, double scalar)
        {
            double[] retVal = new double[vector.Length];

            for (int cntr = 0; cntr < vector.Length; cntr++)
            {
                retVal[cntr] = vector[cntr] * scalar;
            }

            return retVal;
        }
        public static double[] Divide(double[] vector, double scalar)
        {
            double[] retVal = new double[vector.Length];

            for (int cntr = 0; cntr < vector.Length; cntr++)
            {
                retVal[cntr] = vector[cntr] / scalar;
            }

            return retVal;
        }

        public static double[] Negate(double[] vector)
        {
            double[] retVal = new double[vector.Length];

            for (int cntr = 0; cntr < vector.Length; cntr++)
            {
                retVal[cntr] = -vector[cntr];
            }

            return retVal;
        }

        /// <summary>
        /// Get a random vector between boundry lower and boundry upper
        /// </summary>
        public static double[] GetRandomVector(double[] boundryLower, double[] boundryUpper)
        {
            double[] retVal = new double[boundryLower.Length];

            Random rand = StaticRandom.GetRandomForThread();

            for (int cntr = 0; cntr < retVal.Length; cntr++)
            {
                retVal[cntr] = rand.NextDouble(boundryLower[cntr], boundryUpper[cntr]);
            }

            return retVal;
        }

        #endregion

        #region Misc

        public static Tuple<double[], double[]> GetAABB(IEnumerable<double[]> points)
        {
            double[] first = points.FirstOrDefault();
            if (first == null)
            {
                throw new ArgumentException("The list of points is empty.  Can't determine number of dimensions");
            }

            double[] min = Enumerable.Range(0, first.Length).
                Select(o => double.MaxValue).
                ToArray();

            double[] max = Enumerable.Range(0, first.Length).
                Select(o => double.MinValue).
                ToArray();

            foreach (double[] point in points)
            {
                for (int cntr = 0; cntr < point.Length; cntr++)
                {
                    if (point[cntr] < min[cntr])
                    {
                        min[cntr] = point[cntr];
                    }

                    if (point[cntr] > max[cntr])
                    {
                        max[cntr] = point[cntr];
                    }
                }
            }

            return Tuple.Create(min, max);
        }

        /// <summary>
        /// This changes the size of aabb by percent
        /// </summary>
        /// <param name="percent">If less than 1, this will reduce the size.  If greater than 1, this will increase the size</param>   
        public static Tuple<double[], double[]> ResizeAABB(Tuple<double[], double[]> aabb, double percent)
        {
            double[] center = GetCenter(new[] { aabb.Item1, aabb.Item2 });

            double[] dirMin = Subtract(aabb.Item1, center);
            dirMin = Multiply(dirMin, percent);

            double[] dirMax = Subtract(aabb.Item2, center);
            dirMax = Multiply(dirMax, percent);

            return Tuple.Create(Add(center, dirMin), Add(center, dirMax));
        }

        /// <summary>
        /// This returns the center of position of the points
        /// </summary>
        public static double[] GetCenter(IEnumerable<double[]> points)
        {
            if (points == null)
            {
                throw new ArgumentException("Unknown number of dimensions");
            }

            double[] retVal = null;

            int length = 0;

            foreach (double[] point in points)
            {
                if (retVal == null)
                {
                    retVal = new double[point.Length];      // waiting until the first vector is seen to initialize the return array (I don't want to ask how many dimensions there are when it's defined by the points)
                }

                // Add this point to the total
                for (int cntr = 0; cntr < retVal.Length; cntr++)
                {
                    retVal[cntr] += point[cntr];
                }

                length++;
            }

            if (length == 0)
            {
                throw new ArgumentException("Unknown number of dimensions");
            }

            double oneOverLen = 1d / Convert.ToDouble(length);

            // Divide by count
            for (int cntr = 0; cntr < retVal.Length; cntr++)
            {
                retVal[cntr] *= oneOverLen;
            }

            return retVal;
        }

        /// <remarks>
        /// http://www.mathsisfun.com/data/standard-deviation.html
        /// </remarks>
        public static double GetStandardDeviation(IEnumerable<double[]> values)
        {
            double[] mean = GetCenter(values);

            // Variance is the average of the of the distance squared from the mean
            double variance = values.
                Select(o =>
                {
                    double[] diff = Subtract(o, mean);
                    return GetLengthSquared(diff);
                }).
                Average();

            return Math.Sqrt(variance);
        }

        public static Tuple<int, int, double>[] GetDistancesBetween(double[][] positions)
        {
            List<Tuple<int, int, double>> retVal = new List<Tuple<int, int, double>>();

            for (int outer = 0; outer < positions.Length - 1; outer++)
            {
                for (int inner = outer + 1; inner < positions.Length; inner++)
                {
                    double distance = MathND.GetDistance(positions[outer], positions[inner]);
                    retVal.Add(Tuple.Create(outer, inner, distance));
                }
            }

            return retVal.ToArray();
        }

        public static double[][] ApplyBallOfSprings(double[][] positions, Tuple<int, int, double>[] desiredDistances, int numIterations)
        {
            return BallOfSprings.ApplyBallOfSprings(positions, desiredDistances, numIterations);
        }

        public static bool IsInside(Tuple<double[], double[]> aabb, double[] testPoint)
        {
            for (int cntr = 0; cntr < testPoint.Length; cntr++)
            {
                if (testPoint[cntr] < aabb.Item1[cntr] || testPoint[cntr] > aabb.Item2[cntr])
                {
                    return false;
                }
            }

            return true;
        }

        #endregion
    }
}
