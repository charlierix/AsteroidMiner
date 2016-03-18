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

        #region Simple

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

        #endregion

        #region Misc

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

        /// <remarks>
        /// http://www.mathsisfun.com/data/standard-deviation.html
        /// </remarks>
        public static Tuple<double, double> Get_Average_StandardDeviation(IEnumerable<double> values)
        {
            double mean = values.Average();

            // Variance is the average of the of the distance squared from the mean
            double variance = values.
                Select(o =>
                {
                    double diff = o - mean;
                    return diff * diff;     // squaring makes sure it's positive
                }).
                Average();

            return Tuple.Create(mean, Math.Sqrt(variance));
        }
        public static Tuple<double, double> Get_Average_StandardDeviation(IEnumerable<int> values)
        {
            return Get_Average_StandardDeviation(values.Select(o => Convert.ToDouble(o)));
        }
        public static Tuple<DateTime, TimeSpan> Get_Average_StandardDeviation(IEnumerable<DateTime> dates)
        {
            // I don't know if this is the best way (timezones and all that craziness)

            DateTime first = dates.First();

            double[] hours = dates.
                Select(o => (o - first).TotalHours).
                ToArray();

            Tuple<double, double> retVal = Get_Average_StandardDeviation(hours);

            return Tuple.Create(first + TimeSpan.FromHours(retVal.Item1), TimeSpan.FromHours(retVal.Item2));
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

        /// <summary>
        /// This will try various inputs and come up with an input that produces the desired output
        /// NOTE: This method will only try positive inputs
        /// NOTE: This method assumes an increase in input causes the output to increase
        /// </summary>
        /// <remarks>
        /// NOTE: This first attempt doesn't try to guess the power of the equation (linear, square, sqrt, etc).
        /// It starts at 1, then either keeps multiplying or dividing by 10 until a high and low are found
        /// Then it does a binary search between high and low
        /// 
        /// TODO: Take a parameter: Approximate function
        ///     this is the definition of a function that could be run in reverse to approximate a good starting point, and help with coming up with
        ///     decent input attempts.  This should be refined within this method and returned by this method.  Use Math.NET's Fit.Polynomial?
        /// http://stackoverflow.com/questions/20786756/use-math-nets-fit-polynomial-method-on-functions-of-multiple-parameters
        /// http://www.mathdotnet.com/
        ///     
        /// TODO: Think about how to make this method more robust.  It could be a helper for a controller (thrust controller)
        /// </remarks>
        public static double GetInputForDesiredOutput_PosInput_PosCorrelation(double desiredOutput, double allowableError, Func<double, double> getOutput, int? maxIterations = 5000)
        {
            if (allowableError <= 0)
            {
                throw new ArgumentException("allowableError must be positive: " + allowableError.ToString());
            }

            // Start with an input of 1
            Tuple<double, double> current = Tuple.Create(1d, getOutput(1d));

            if (IsInRange(current.Item2, desiredOutput, allowableError))
            {
                return current.Item1;       // lucky guess
            }

            // See if it's above or below the desired output
            Tuple<double, double> low = null;
            Tuple<double, double> high = null;
            if (current.Item2 < desiredOutput)
            {
                low = current;
            }
            else
            {
                high = current;
            }

            int count = 0;

            while (maxIterations == null || count < maxIterations.Value)
            {
                double nextInput;

                if (low == null)
                {
                    #region too high

                    // Floor hasn't been found.  Try something smaller

                    nextInput = high.Item1 / 10d;

                    current = Tuple.Create(nextInput, getOutput(nextInput));

                    if (current.Item2 < desiredOutput)
                    {
                        low = current;      // floor and ceiling are now known
                    }
                    else if (current.Item2 < high.Item2)
                    {
                        high = current;     // floor still isn't known, but this is closer than the previous ceiling
                    }

                    #endregion
                }
                else if (high == null)
                {
                    #region too low

                    // Ceiling hasn't been found.  Try something larger

                    nextInput = low.Item1 * 10d;

                    current = Tuple.Create(nextInput, getOutput(nextInput));

                    if (current.Item2 > desiredOutput)
                    {
                        high = current;     // floor and ceiling are now known
                    }
                    else if (current.Item2 > low.Item2)
                    {
                        low = current;      // ceiling still isn't known, but this is closer than the previous floor
                    }

                    #endregion
                }
                else
                {
                    #region straddle

                    // Floor and ceiling are known.  Try an input that is between them

                    nextInput = Math1D.Avg(low.Item1, high.Item1);

                    current = Tuple.Create(nextInput, getOutput(nextInput));

                    if (current.Item2 < desiredOutput && current.Item2 > low.Item2)
                    {
                        low = current;      // increase the floor
                    }
                    else if (current.Item2 > desiredOutput && current.Item2 < high.Item2)
                    {
                        high = current;     // decrease the ceiling
                    }

                    #endregion
                }

                if (IsInRange(current.Item2, desiredOutput, allowableError))
                {
                    return current.Item1;
                }

                count++;
            }

            //TODO: Take in a param whether to throw an exception or return the best guess
            throw new ApplicationException("Couldn't find a solution");
        }

        /// <summary>
        /// This is a sigmoid that is stretched so that when:
        /// x=0, y=0
        /// x=maxY, y=~.8*maxY
        /// </summary>
        /// <param name="x">Expected range is 0 to infinity (negative x will return a negative y)</param>
        /// <param name="maxY">The return will approach this, but never hit it (asymptote)</param>
        /// <param name="slope">
        /// This is roughly the slope of the curve
        /// NOTE: Slope is probably the wrong term, because it is scaled to maxY (so if maxY is 10, then the actual slope would be about 10, even though you pass in slope=1)
        /// </param>
        public static double PositiveSCurve(double x, double maxY, double slope = 1)
        {
            //const double LN_19 = 2.94443897916644;        // y=.9*maxY at x=maxY
            const double LN_9 = 2.19722457733621;       // y=.8*maxY at x=maxY

            // Find a constant for the maxY passed in so that when x=maxY, the below function will equal .8*maxY (when slope is 1)
            //double factor = Math.Log(9) / (maxY * Math.E);
            double factor = LN_9 / (maxY * Math.E);

            //return -1 + (2 / (1 + e ^ (-slope * e * x)));
            //return -maxY + ((2 * maxY) / (1 + Math.E ^ (-slope * Math.E * x)));
            return -maxY + ((2 * maxY) / (1 + Math.Pow(Math.E, (-(factor * slope) * Math.E * x))));
        }

        #endregion

        #region Private Methods

        private static bool IsInRange(double testValue, double compareTo, double allowableError)
        {
            return testValue >= compareTo - allowableError && testValue <= compareTo + allowableError;
        }

        #endregion
    }
}
