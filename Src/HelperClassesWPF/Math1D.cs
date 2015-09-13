using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.HelperClassesCore;

namespace Game.HelperClassesWPF
{
    public static class Math1D
    {
        #region Declaration Section

        private const double _180_over_PI = (180d / Math.PI);
        private const double _PI_over_180 = (Math.PI / 180d);

        private const float _180_over_PI_FLOAT = (180f / (float)Math.PI);
        private const float _PI_over_180_FLOAT = ((float)Math.PI / 180f);

        public const double Radian360 = 360 * _PI_over_180;

        public const double GOLDENRATIO = 1.61803398875;

        #endregion

        /// <summary>
        /// Gets a value between -maxValue and maxValue
        /// </summary>
        public static double GetNearZeroValue(double maxValue)
        {
            Random rand = StaticRandom.GetRandomForThread();

            double retVal = rand.NextDouble() * maxValue;

            if (rand.Next(0, 2) == 1)
            {
                retVal *= -1d;
            }

            return retVal;
        }
        /// <summary>
        /// This gets a value between minValue and maxValue, and has a 50/50 chance of negating that
        /// </summary>
        public static double GetNearZeroValue(double minValue, double maxValue)
        {
            Random rand = StaticRandom.GetRandomForThread();

            double retVal = minValue + (rand.NextDouble() * (maxValue - minValue));

            if (rand.Next(0, 2) == 1)
            {
                retVal *= -1d;
            }

            return retVal;
        }

        public static bool IsNearZero(double testValue)
        {
            return Math.Abs(testValue) <= Math3D.NEARZERO;
        }
        public static bool IsNearValue(double testValue, double compareTo)
        {
            return testValue >= compareTo - Math3D.NEARZERO && testValue <= compareTo + Math3D.NEARZERO;
        }

        /// <summary>
        /// Returns true if the double is NaN or Infinity
        /// </summary>
        public static bool IsInvalid(double testValue)
        {
            return double.IsNaN(testValue) || double.IsInfinity(testValue);
        }

        //TODO: Come up with a better name for these.  The test value must exceed a threshold before these return true (a value that IsNearZero would call true won't make these true)
        public static bool IsNearNegative(double testValue)
        {
            return testValue < -Math3D.NEARZERO;
        }
        public static bool IsNearPositive(double testValue)
        {
            return testValue > Math3D.NEARZERO;
        }

        public static bool IsDivisible(double larger, double smaller)
        {
            if (IsNearZero(smaller))
            {
                // Divide by zero.  Nothing is divisible by zero, not even zero.  (I looked up "is zero divisible by zero", and got very
                // technical reasons why it's not.  It would be cool to be able to view the world the way math people do.  Visualizing
                // complex equations, etc)
                return false;
            }

            // Divide the larger by the smaller.  If the result is an integer (or very close to an integer), then they are divisible
            double division = larger / smaller;
            double divisionInt = Math.Round(division);

            return IsNearValue(division, divisionInt);
        }

        public static double DegreesToRadians(double degrees)
        {
            return degrees * _PI_over_180;
        }
        public static float DegreesToRadians(float degrees)
        {
            return degrees * _PI_over_180_FLOAT;
        }

        public static double RadiansToDegrees(double radians)
        {
            return radians * _180_over_PI;
        }
        public static float RadiansToDegrees(float radians)
        {
            return radians * _180_over_PI_FLOAT;
        }

        /// <summary>
        /// This returns the minimum and maximum value (throws exception if empty list)
        /// </summary>
        public static Tuple<double, double> MinMax(IEnumerable<double> values)
        {
            double min = double.MaxValue;
            double max = double.MinValue;
            bool hasEntry = false;      // don't want to use Count(), because that would iterate the whole list

            foreach (double value in values)
            {
                hasEntry = true;

                if (value < min)
                {
                    min = value;
                }

                if (value > max)
                {
                    max = value;
                }
            }

            if (!hasEntry)
            {
                throw new InvalidOperationException("Sequence contains no elements");       // this is the same error that .Max() gives
            }

            return Tuple.Create(min, max);
        }

        // I got tired of nesting min/max statements
        public static int Min(int v1, int v2, int v3)
        {
            return Math.Min(Math.Min(v1, v2), v3);
        }
        public static int Min(int v1, int v2, int v3, int v4)
        {
            return Math.Min(Math.Min(v1, v2), Math.Min(v3, v4));
        }
        public static int Min(int v1, int v2, int v3, int v4, int v5)
        {
            return Math.Min(Math.Min(Math.Min(v1, v2), v3), Math.Min(v4, v5));
        }
        public static int Min(int v1, int v2, int v3, int v4, int v5, int v6)
        {
            return Math.Min(Math.Min(Math.Min(v1, v2), v3), Math.Min(Math.Min(v4, v5), v6));
        }
        public static int Min(int v1, int v2, int v3, int v4, int v5, int v6, int v7)
        {
            return Math.Min(Math.Min(Math.Min(v1, v2), Math.Min(v3, v4)), Math.Min(Math.Min(v5, v6), v7));
        }
        public static int Min(int v1, int v2, int v3, int v4, int v5, int v6, int v7, int v8)
        {
            return Math.Min(Math.Min(Math.Min(v1, v2), Math.Min(v3, v4)), Math.Min(Math.Min(v5, v6), Math.Min(v7, v8)));
        }

        public static double Min(double v1, double v2, double v3)
        {
            return Math.Min(Math.Min(v1, v2), v3);
        }
        public static double Min(double v1, double v2, double v3, double v4)
        {
            return Math.Min(Math.Min(v1, v2), Math.Min(v3, v4));
        }
        public static double Min(double v1, double v2, double v3, double v4, double v5)
        {
            return Math.Min(Math.Min(Math.Min(v1, v2), v3), Math.Min(v4, v5));
        }
        public static double Min(double v1, double v2, double v3, double v4, double v5, double v6)
        {
            return Math.Min(Math.Min(Math.Min(v1, v2), v3), Math.Min(Math.Min(v4, v5), v6));
        }
        public static double Min(double v1, double v2, double v3, double v4, double v5, double v6, double v7)
        {
            return Math.Min(Math.Min(Math.Min(v1, v2), Math.Min(v3, v4)), Math.Min(Math.Min(v5, v6), v7));
        }
        public static double Min(double v1, double v2, double v3, double v4, double v5, double v6, double v7, double v8)
        {
            return Math.Min(Math.Min(Math.Min(v1, v2), Math.Min(v3, v4)), Math.Min(Math.Min(v5, v6), Math.Min(v7, v8)));
        }

        public static int Max(int v1, int v2, int v3)
        {
            return Math.Max(Math.Max(v1, v2), v3);
        }
        public static int Max(int v1, int v2, int v3, int v4)
        {
            return Math.Max(Math.Max(v1, v2), Math.Max(v3, v4));
        }
        public static int Max(int v1, int v2, int v3, int v4, int v5)
        {
            return Math.Max(Math.Max(Math.Max(v1, v2), v3), Math.Max(v4, v5));
        }
        public static int Max(int v1, int v2, int v3, int v4, int v5, int v6)
        {
            return Math.Max(Math.Max(Math.Max(v1, v2), v3), Math.Max(Math.Max(v4, v5), v6));
        }
        public static int Max(int v1, int v2, int v3, int v4, int v5, int v6, int v7)
        {
            return Math.Max(Math.Max(Math.Max(v1, v2), Math.Max(v3, v4)), Math.Max(Math.Max(v5, v6), v7));
        }
        public static int Max(int v1, int v2, int v3, int v4, int v5, int v6, int v7, int v8)
        {
            return Math.Max(Math.Max(Math.Max(v1, v2), Math.Max(v3, v4)), Math.Max(Math.Max(v5, v6), Math.Max(v7, v8)));
        }

        public static double Max(double v1, double v2, double v3)
        {
            return Math.Max(Math.Max(v1, v2), v3);
        }
        public static double Max(double v1, double v2, double v3, double v4)
        {
            return Math.Max(Math.Max(v1, v2), Math.Max(v3, v4));
        }
        public static double Max(double v1, double v2, double v3, double v4, double v5)
        {
            return Math.Max(Math.Max(Math.Max(v1, v2), v3), Math.Max(v4, v5));
        }
        public static double Max(double v1, double v2, double v3, double v4, double v5, double v6)
        {
            return Math.Max(Math.Max(Math.Max(v1, v2), v3), Math.Max(Math.Max(v4, v5), v6));
        }
        public static double Max(double v1, double v2, double v3, double v4, double v5, double v6, double v7)
        {
            return Math.Max(Math.Max(Math.Max(v1, v2), Math.Max(v3, v4)), Math.Max(Math.Max(v5, v6), v7));
        }
        public static double Max(double v1, double v2, double v3, double v4, double v5, double v6, double v7, double v8)
        {
            return Math.Max(Math.Max(Math.Max(v1, v2), Math.Max(v3, v4)), Math.Max(Math.Max(v5, v6), Math.Max(v7, v8)));
        }

        public static double Avg(double v1, double v2)
        {
            return (v1 + v2) / 2d;
        }
        public static double Avg(double v1, double v2, double v3)
        {
            return (v1 + v2 + v3) / 3d;
        }
        public static double Avg(double v1, double v2, double v3, double v4)
        {
            return (v1 + v2 + v3 + v4) / 4d;
        }
        public static double Avg(double v1, double v2, double v3, double v4, double v5)
        {
            return (v1 + v2 + v3 + v4 + v5) / 5d;
        }
        public static double Avg(double v1, double v2, double v3, double v4, double v5, double v6)
        {
            return (v1 + v2 + v3 + v4 + v5 + v6) / 6d;
        }
        public static double Avg(double v1, double v2, double v3, double v4, double v5, double v6, double v7)
        {
            return (v1 + v2 + v3 + v4 + v5 + v6 + v7) / 7d;
        }
        public static double Avg(double v1, double v2, double v3, double v4, double v5, double v6, double v7, double v8)
        {
            return (v1 + v2 + v3 + v4 + v5 + v6 + v7 + v8) / 8d;
        }
    }
}
