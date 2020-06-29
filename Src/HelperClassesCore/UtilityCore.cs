using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Xaml;

namespace Game.HelperClassesCore
{
    public static class UtilityCore
    {
        #region class: EnumerateColumn

        public class EnumerateColumn
        {
            #region Declaration Section

            private bool _hasStarted = false;
            private bool _isFinished = false;

            #endregion

            #region Constructor

            public EnumerateColumn(int count)
            {
                _count = count;
            }

            #endregion

            #region Public Properties

            private readonly int _count;
            public int Count => _count;

            private EnumerateColumn _left = null;
            public EnumerateColumn Left
            {
                get
                {
                    return _left;
                }
                set
                {
                    if (_left != null)
                    {
                        throw new InvalidOperationException("Left can only be set once");
                    }

                    _left = value;
                }
            }

            private EnumerateColumn _right = null;
            public EnumerateColumn Right
            {
                get
                {
                    return _right;
                }
                set
                {
                    if (_right != null)
                    {
                        throw new InvalidOperationException("Right can only be set once");
                    }

                    _right = value;
                }
            }

            private int _value = -1;
            public int Value
            {
                get
                {
                    if (!_hasStarted)
                    {
                        throw new InvalidOperationException("Must call Start first");
                    }
                    else if (_isFinished)
                    {
                        throw new InvalidOperationException("This is finished");
                    }
                    else if (_value < 0 || _value >= _count)
                    {
                        throw new InvalidOperationException(string.Format("Value is in an invalid state: {0} (count is {1}", _value, _count));
                    }

                    return _value;
                }
            }

            #endregion

            #region Public Methods

            public void Start()
            {
                if (Left != null || Right == null)
                {
                    throw new InvalidOperationException("Start can only be called on the leftmost node");
                }
                else if (_hasStarted)
                {
                    throw new InvalidOperationException("Start can only be called once");
                }

                _hasStarted = true;

                _value = 0;

                Right.LeftAdvanced();
            }

            public bool Advance()
            {
                if (Left != null || Right == null)
                {
                    throw new InvalidOperationException("Start can only be called on the leftmost node");
                }
                else if (!_hasStarted)
                {
                    throw new InvalidOperationException("Must call Start first");
                }
                else if (_isFinished)
                {
                    throw new InvalidOperationException("This has already advanced as far as it can");
                }

                if (Right.TryAdvance())
                {
                    // One of the nodes to the right could advance
                    return true;
                }

                _value++;

                if (_value >= _count)
                {
                    _isFinished = true;
                    return false;
                }

                Right.LeftAdvanced();

                return true;
            }

            #endregion

            #region Private Methods

            /// <summary>
            /// This gets called from the node on the left.  It advanced one, so this class should reset and find the next available value
            /// </summary>
            private void LeftAdvanced()
            {
                if (Left == null)
                {
                    throw new InvalidOperationException("This can only be called from the left");
                }
                else if (_isFinished)
                {
                    throw new InvalidOperationException("This class is already finished");
                }

                // Only the leftmost node is explicitly started.  All nodes to the right get notified here (other calls call this method, but setting
                // the bool more than once won't hurt)
                _hasStarted = true;

                if (!FindNext(0))
                {
                    throw new ApplicationException("Should always find a value");
                }
            }

            private bool TryAdvance()
            {
                if (Left == null)
                {
                    throw new InvalidOperationException("This can only be called from the left");
                }
                else if (_isFinished)
                {
                    throw new InvalidOperationException("This class is already finished");
                }

                if (Right != null)
                {
                    if (Right.TryAdvance())
                    {
                        return true;
                    }
                }

                return FindNext(_value + 1);
            }

            private bool FindNext(int from)
            {
                // These classes act like an odometer.  When a node on the left advances, all the nodes to its right need to start over and find
                // the first available slot
                for (int cntr = from; cntr < _count; cntr++)
                {
                    if (Left.IsAvailable(cntr))
                    {
                        _value = cntr;

                        if (Right != null)
                        {
                            Right.LeftAdvanced();
                        }

                        return true;
                    }
                }

                _value = -1;    // set it to an invalid value
                return false;
            }

            private bool IsAvailable(int value)
            {
                if (value == _value)
                {
                    return false;
                }

                if (Left == null)
                {
                    return true;
                }

                return Left.IsAvailable(value);
            }

            #endregion
        }

        #endregion

        #region Declaration Section

        public const double NEARZERO = .000000001d;

        private const double FOURTHIRDS = 4d / 3d;

        #endregion

        #region Misc

        /// <summary>
        /// This is good for converting a trackbar into a double
        /// </summary>
        /// <param name="minReturn">This is the value that will be returned when valueRange == minRange</param>
        /// <param name="maxReturn">This is the value that will be returned with valueRange == maxRange</param>
        /// <param name="minRange">The lowest value that valueRange can be</param>
        /// <param name="maxRange">The highest value that valueRange can be</param>
        /// <param name="valueRange">The trackbar's value</param>
        /// <returns>Somewhere between minReturn and maxReturn</returns>
        public static double GetScaledValue(double minReturn, double maxReturn, double minRange, double maxRange, double valueRange)
        {
            if (minRange.IsNearValue(maxRange))
            {
                return minReturn;
            }

            // Get the percent of value within the range
            double percent = (valueRange - minRange) / (maxRange - minRange);

            // Get the lerp between the return range
            return minReturn + (percent * (maxReturn - minReturn));
        }
        /// <summary>
        /// This overload ensures that the return doen't go beyond the min and max return
        /// </summary>
        public static double GetScaledValue_Capped(double minReturn, double maxReturn, double minRange, double maxRange, double valueRange)
        {
            double retVal = GetScaledValue(minReturn, maxReturn, minRange, maxRange, valueRange);

            // Cap the return value
            if (minReturn < maxReturn)
            {
                if (retVal < minReturn)
                {
                    retVal = minReturn;
                }
                else if (retVal > maxReturn)
                {
                    retVal = maxReturn;
                }
            }
            else if (minReturn > maxReturn)
            {
                if (retVal < maxReturn)
                {
                    retVal = maxReturn;
                }
                else if (retVal > minReturn)
                {
                    retVal = minReturn;
                }
            }
            else
            {
                retVal = minReturn;
            }

            return retVal;
        }

        /// <summary>
        /// This makes sure that value is between min an max
        /// </summary>
        public static double Cap(double value, double min, double max)
        {
            if (value < min)
            {
                return min;
            }
            else if (value > max)
            {
                return max;
            }
            else
            {
                return value;
            }
        }

        /// <summary>
        /// This takes the base 10 offset, converts to a base 26 number, and represents each of those as letters
        /// </summary>
        /// <remarks>
        /// Examples
        ///     0=A
        ///     10=K
        ///     25=Z
        ///     26=AA
        ///     27=AB
        ///     300-KO
        ///     302351=QEFX
        /// </remarks>
        public static string ConvertToAlpha(ulong charOffset)
        {
            List<char> retVal = new List<char>();

            byte byteA = Convert.ToByte('A');
            ulong remaining = charOffset;

            while (true)
            {
                ulong current = remaining % 26;

                retVal.Add(Convert.ToChar(Convert.ToByte(byteA + current)));

                //remaining = (remaining / 26) - 1;     // can't do in one statement, because it was converted to unsigned
                remaining = remaining / 26;
                if (remaining == 0)
                {
                    break;
                }
                remaining--;
            }

            retVal.Reverse();

            return new string(retVal.ToArray());
        }
        public static string ConvertToAlpha(int charOffset)
        {
            return ConvertToAlpha((ulong)charOffset);
        }

        /// <summary>
        /// After this method: 1 = 2, 2 = 1
        /// </summary>
        public static void Swap<T>(ref T item1, ref T item2)
        {
            T temp = item1;
            item1 = item2;
            item2 = temp;
        }

        /// <summary>
        /// This makes sure that min is less than max.  If they are passed in backward, they get swapped
        /// </summary>
        public static void MinMax(ref int min, ref int max)
        {
            if (max < min)
            {
                Swap(ref min, ref max);
            }
        }
        public static void MinMax(ref double min, ref double max)
        {
            if (max < min)
            {
                Swap(ref min, ref max);
            }
        }
        public static void MinMax(ref decimal min, ref decimal max)
        {
            if (max < min)
            {
                Swap(ref min, ref max);
            }
        }
        public static void MinMax(ref byte min, ref byte max)
        {
            if (max < min)
            {
                Swap(ref min, ref max);
            }
        }
        public static void MinMax(ref string min, ref string max)
        {
            if (max.CompareTo(min) < 0)
            {
                Swap(ref min, ref max);
            }
        }

        public static double GetMassForRadius(double radius, double density)
        {
            // Volume = 4/3 * pi * r^3
            // Mass = Volume * Density
            return FOURTHIRDS * Math.PI * (radius * radius * radius) * density;
        }
        public static double GetRadiusForMass(double mass, double density)
        {
            // Volume = Mass / Density
            // r^3 = Volume / (4/3 * pi)
            return Math.Pow((mass / density) / (FOURTHIRDS * Math.PI), .333333d);
        }

        public static bool IsWhitespace(char text)
        {
            //http://stackoverflow.com/questions/18169006/all-the-whitespace-characters-is-it-language-independent

            // Here are some more chars that could be space
            //http://www.fileformat.info/info/unicode/category/Zs/list.htm

            switch (text)
            {
                case '\0':
                case '\t':
                case '\r':
                case '\v':
                case '\f':
                case '\n':
                case ' ':
                case '\u00A0':      // NO-BREAK SPACE
                case '\u1680':      // OGHAM SPACE MARK
                case '\u2000':      // EN QUAD
                case '\u2001':      // EM QUAD
                case '\u2002':      // EN SPACE
                case '\u2003':      // EM SPACE
                case '\u2004':      // THREE-PER-EM SPACE
                case '\u2005':      // FOUR-PER-EM SPACE
                case '\u2006':      // SIX-PER-EM SPACE
                case '\u2007':      // FIGURE SPACE
                case '\u2008':      // PUNCTUATION SPACE
                case '\u2009':      // THIN SPACE
                case '\u200A':      // HAIR SPACE
                case '\u202F':      // NARROW NO-BREAK SPACE
                case '\u205F':      // MEDIUM MATHEMATICAL SPACE
                case '\u3000':      // IDEOGRAPHIC SPACE
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// This will try to get the output, and retry a few times if invalid
        /// </summary>
        /// <param name="adjustInput">
        /// For certain types of operations, adjusting the input could be how to fix the output (finding hulls, voronoi, etc).  If the
        /// failure is a DB or web call, then just retry without changing inputs
        /// </param>
        /// <returns>
        /// Either a valid output, or null if the retry count was exceeded
        /// </returns>
        public static Toutput RetryWrapper<Tinput, Toutput>(Tinput input, int retryCount, Func<Tinput, Toutput> getOutput, Func<Toutput, bool> isValid, Func<Tinput, Tinput> adjustInput = null) where Toutput : class
        {
            Tinput inputActual = input;

            for (int cntr = 0; cntr <= retryCount; cntr++)      // saying ==, because the 0 iteration is the first (so even if they say 0 retries, the loop will still run once)
            {
                if (cntr > 0 && adjustInput != null)
                {
                    inputActual = adjustInput(inputActual);
                }

                try
                {
                    Toutput retVal = getOutput(inputActual);

                    if (isValid(retVal))
                    {
                        return retVal;
                    }
                }
                catch { }
            }

            return null;
        }

        /// <summary>
        /// This compares two arrays.  If they are the same size, and each element equals, then this returns true
        /// </summary>
        public static bool IsArrayEqual<T>(T[] arr1, T[] arr2)
        {
            if (arr1 == null && arr2 == null)
            {
                return true;
            }
            else if (arr1 == null || arr2 == null)
            {
                return false;
            }
            else if (arr1.Length != arr2.Length)
            {
                return false;
            }

            for (int cntr = 0; cntr < arr1.Length; cntr++)
            {
                if (!arr1[cntr].Equals(arr2[cntr]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Converts a base 10 number to base 2
        /// </summary>
        public static bool[] ConvertToBase2(long value, int vectorSize)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException("This method can't handle negative numbers: " + value.ToString());
            }

            return Convert.ToString(value, 2).       // convert the base10 cntr to base2
                PadLeft(vectorSize, '0').     // force constant width
                Select(o => o == '1').      // convert to bool
                ToArray();
        }
        /// <summary>
        /// Converts a base 2 number to base 10
        /// </summary>
        public static long ConvertToBase10(bool[] bits)
        {
            string text = new string(bits.Select(o => o ? '1' : '0').ToArray());
            return Convert.ToInt64(text, 2);
        }

        // These aren't concerned with the meaning of the value, just storing all possible values into the other type (they just shift up or down by minvalue)
        public static ulong Map_long_ulong(long value)
        {
            return unchecked((ulong)(value - long.MinValue));
        }
        public static long Map_ulong_long(ulong value)
        {
            return unchecked((long)value + long.MinValue);
        }

        private static string[] _suffix = { " ", "K", "M", "G", "T", "P", "E", "Z", "Y" };  // longs run out around EB -- yotta is bigger than zetta :)
        public static string GetSizeDisplay(long size, int decimalPlaces = 0, bool includeB = false)
        {
            //http://stackoverflow.com/questions/281640/how-do-i-get-a-human-readable-file-size-in-bytes-abbreviation-using-net

            if (size == 0)
            {
                return "0 " + _suffix[0] + (includeB ? "B" : "");
            }

            long abs = Math.Abs(size);

            int place = Convert.ToInt32(Math.Floor(Math.Log(abs, 1024)));

            string numberText;
            if (decimalPlaces > 0)
            {
                double num = abs / Math.Pow(1024, place);
                numberText = (Math.Sign(size) * num).ToStringSignificantDigits(decimalPlaces);
            }
            else
            {
                double num = Math.Ceiling(abs / Math.Pow(1024, place));        //NOTE: windows uses ceiling, so doing the same (showing decimal places just clutters the view if looking at a list)
                numberText = (Math.Sign(size) * num).ToString("N0");
            }

            return numberText + " " + _suffix[place] + (includeB ? "B" : "");
        }

        public static int? GetCommonIndex((int, int) edge1, (int, int) edge2)
        {
            //This is a copy of Edge3D.GetCommonIndex

            // 1.1 : 2.1
            if (edge1.Item1 == edge2.Item1)
                return edge1.Item1;

            // 1.1 : 2.2
            if (edge1.Item1 == edge2.Item2)
                return edge1.Item1;

            // 1.2 : 2.1
            if (edge1.Item2 == edge2.Item1)
                return edge1.Item2;

            // 1.2 : 2.2
            if (edge1.Item2 == edge2.Item2)
                return edge1.Item2;

            return null;
        }
        public static int[] GetPolygon((int, int)[] edges)
        {
            // This is a copy of Edge2D.GetPolygon

            if (edges.Length < 2)
            {
                throw new ArgumentException($"Not enough segments to make a polygon: {edges.Length}");
            }

            List<int> retVal = new List<int>();
            int? commonIndex;

            for (int cntr = 0; cntr < edges.Length - 1; cntr++)
            {
                #region cntr, cntr+1

                // Add the point from cntr that isn't shared with cntr + 1
                commonIndex = GetCommonIndex(edges[cntr], edges[cntr + 1]);
                if (commonIndex == null)
                {
                    // While in this main loop, there can't be any breaks
                    throw new ApplicationException("Didn't find common point between edges");
                }
                else
                {
                    retVal.Add(commonIndex.Value);
                }

                #endregion
            }

            #region last edge

            if (edges.Length == 2)
            {
                // These edges define an open polygon - looks like a U

                // Take the item from the second edge that isn't in the list
                if (edges[1].Item1 == retVal[0])
                {
                    retVal.Add(edges[1].Item2);
                }
                else
                {
                    retVal.Add(edges[1].Item1);
                }

                // Now loop back, taking the item from the first edge that isn't in the list
                if (edges[0].Item1 == retVal[0])
                {
                    retVal.Add(edges[0].Item2);
                }
                else
                {
                    retVal.Add(edges[0].Item1);
                }
            }
            else
            {
                commonIndex = GetCommonIndex(edges[0], edges[edges.Length - 1]);
                if (commonIndex == null)
                {
                    throw new ApplicationException("Didn't find common point between edges");
                }

                retVal.Add(commonIndex.Value);
            }

            #endregion

            return retVal.ToArray();
        }

        #endregion

        #region Enums

        public static T GetRandomEnum<T>(T excluding) where T : struct
        {
            return GetRandomEnum<T>(new T[] { excluding });
        }
        public static T GetRandomEnum<T>(IEnumerable<T> excluding) where T : struct
        {
            while (true)
            {
                T retVal = GetRandomEnum<T>();
                if (!excluding.Contains(retVal))
                {
                    return retVal;
                }
            }
        }
        public static T GetRandomEnum<T>() where T : struct
        {
            Array allValues = Enum.GetValues(typeof(T));
            if (allValues.Length == 0)
            {
                throw new ArgumentException("This enum has no values");
            }

            return (T)allValues.GetValue(StaticRandom.Next(allValues.Length));
        }

        /// <summary>
        /// This is just a wrapper to Enum.GetValues.  Makes the caller's code a bit less ugly
        /// </summary>
        public static T[] GetEnums<T>() where T : struct
        {
            return (T[])Enum.GetValues(typeof(T));
        }
        public static T[] GetEnums<T>(T excluding) where T : struct
        {
            return GetEnums<T>(new T[] { excluding });
        }
        public static T[] GetEnums<T>(IEnumerable<T> excluding) where T : struct
        {
            T[] all = (T[])Enum.GetValues(typeof(T));

            return all.
                Where(o => !excluding.Contains(o)).
                ToArray();
        }

        /// <summary>
        /// This is a strongly typed wrapper to Enum.Parse
        /// </summary>
        public static T EnumParse<T>(string text, bool ignoreCase = true) where T : struct // can't constrain to enum
        {
            return (T)Enum.Parse(typeof(T), text, ignoreCase);
        }

        #endregion

        #region Lists

        /// <summary>
        /// This iterates over all combinations of a set of numbers
        /// NOTE: The number of iterations is (2^inputSize) - 1, so be careful with input sizes over 10 to 15
        /// </summary>
        /// <remarks>
        /// For example, if you pass in 4, you will get:
        ///		0,1,2,3
        ///		0,1,2
        ///		0,1,3
        ///		0,2,3
        ///		1,2,3
        ///		0,1
        ///		0,2
        ///		0,3
        ///		1,2
        ///		1,3
        ///		2,3
        ///		0
        ///		1
        ///		2
        ///		3
        /// </remarks>
        public static IEnumerable<int[]> AllCombosEnumerator(int inputSize)
        {
            int inputMax = inputSize - 1;		// save me from subtracting one all the time

            for (int numUsed = inputSize; numUsed >= 1; numUsed--)
            {
                int usedMax = numUsed - 1;		// save me from subtracting one all the time

                // Seed the return with everything at the left
                int[] retVal = Enumerable.Range(0, numUsed).ToArray();
                yield return (int[])retVal.Clone();		// if this isn't cloned here, then the consumer needs to do it

                while (true)
                {
                    // Try to bump the last item
                    if (retVal[usedMax] < inputMax)
                    {
                        retVal[usedMax]++;
                        yield return (int[])retVal.Clone();
                        continue;
                    }

                    // The last item is as far as it will go, find an item to the left of it to bump
                    bool foundOne = false;

                    for (int cntr = usedMax - 1; cntr >= 0; cntr--)
                    {
                        if (retVal[cntr] < retVal[cntr + 1] - 1)
                        {
                            // This one has room to bump
                            retVal[cntr]++;

                            // Reset everything to the right of this spot
                            for (int resetCntr = cntr + 1; resetCntr < numUsed; resetCntr++)
                            {
                                retVal[resetCntr] = retVal[cntr] + (resetCntr - cntr);
                            }

                            foundOne = true;
                            yield return (int[])retVal.Clone();
                            break;
                        }
                    }

                    if (!foundOne)
                    {
                        // This input size is exhausted (everything is as far right as they can go)
                        break;
                    }
                }
            }
        }
        /// <summary>
        /// The sets are sets of indices that can be together.  Each row returned from this method will contain every index exactly once
        /// </summary>
        /// <remarks>
        /// This is a pretty specialist method.  Not sure if it will be needed outside the one case I made it for
        /// 
        /// Example1        UtilityCore.AllCombosEnumerator(new[] { new[] { 0, 1, 2 }, new[] { 0, 2 }, new[] { 1, 3 } });
        /// input: {0,1,2} {0,2} {1,3}
        /// return: {0} {1} {2} {3}
        /// return: {0,1,2} {3}
        /// return: {0,2} {1,3}
        /// return: {0,2} {1} {3}
        /// return: {1,3} {0} {2}
        /// 
        /// Example2        UtilityCore.AllCombosEnumerator(new[] { new[] { 0, 1, 2 }, new[] { 3, 4 }, new[] { 0, 5 } }).ToArray();
        /// input: {0,1,2} {3,4} {0,5}
        /// return: {0} {1} {2} {3} {4} {5}
        /// return: {0,1,2} {3,4} {5}
        /// return: {0,1,2} {3} {4} {5}
        /// return: {3,4} {0,5} {1} {2}
        /// return: {3,4} {0} {1} {2} {5}
        /// return: {0,5} {1} {2} {3} {4}
        /// </remarks>
        public static IEnumerable<int[][]> AllCombosEnumerator(int[][] sets, int? maxValue = null)
        {
            int max = sets.Max(o => o.Max());
            if (maxValue != null)
            {
                if (maxValue.Value < max)
                {
                    throw new ArgumentException("maxValue is lower than the values in sets");
                }

                max = maxValue.Value;
            }

            // Create an entry that is all singles
            yield return Enumerable.Range(0, max + 1).
                Select(o => new[] { o }).
                ToArray();

            for (int cntr = 0; cntr < sets.Length; cntr++)
            {
                // Get all valid combinations that include sets[cntr]
                foreach (int[][] subSet in AllCombosEnumerator_Set(sets, cntr, max))
                {
                    yield return subSet;
                }
            }
        }

        /// <summary>
        /// This acts like Enumerable.Range, but the values returned are in a random order
        /// </summary>
        public static IEnumerable<int> RandomRange(int start, int count)
        {
            // Prepare a list of indices (these represent what's left to return)
            //int[] indices = Enumerable.Range(start, count).ToArray();		// this is a smaller amount of code, but slower
            int[] indices = new int[count];
            for (int cntr = 0; cntr < count; cntr++)
            {
                indices[cntr] = start + cntr;
            }

            Random rand = StaticRandom.GetRandomForThread();

            for (int cntr = count - 1; cntr >= 0; cntr--)
            {
                // Come up with a random value that hasn't been returned yet
                int index1 = rand.Next(cntr + 1);
                int index2 = indices[index1];
                indices[index1] = indices[cntr];

                yield return index2;
            }
        }
        /// <summary>
        /// This overload wont iterate over all the values, just some of them
        /// </summary>
        /// <param name="rangeCount">When returning a subset of a big list, rangeCount is the size of the big list</param>
        /// <param name="iterateCount">When returning a subset of a big list, iterateCount is the size of the subset</param>
        /// <remarks>
        /// Example:
        ///		start=0, rangeCount=10, iterateCount=3
        ///		This will return 3 values, but their range is from 0 to 10 (and it will never return dupes)
        /// </remarks>
        public static IEnumerable<int> RandomRange(int start, int rangeCount, int iterateCount)
        {
            if (iterateCount > rangeCount)
            {
                //throw new ArgumentOutOfRangeException(string.Format("iterateCount can't be greater than rangeCount.  iterateCount={0}, rangeCount={1}", iterateCount.ToString(), rangeCount.ToString()));
                iterateCount = rangeCount;
            }

            if (iterateCount < rangeCount / 3)
            {
                #region While Loop

                Random rand = StaticRandom.GetRandomForThread();

                // Rather than going through the overhead of building an array of all values up front, just remember what's been returned
                List<int> used = new List<int>();
                int maxValue = start + rangeCount;

                for (int cntr = 0; cntr < iterateCount; cntr++)
                {
                    // Find a value that hasn't been returned yet
                    int retVal = 0;
                    while (true)
                    {
                        retVal = rand.Next(start, maxValue);

                        if (!used.Contains(retVal))
                        {
                            used.Add(retVal);
                            break;
                        }
                    }

                    // Return this
                    yield return retVal;
                }

                #endregion
            }
            else if (iterateCount > 0)
            {
                #region Maintain Array

                // Reuse the other overload, just stop prematurely

                int cntr = 0;
                foreach (int retVal in RandomRange(start, rangeCount))
                {
                    yield return retVal;

                    cntr++;
                    if (cntr == iterateCount)
                    {
                        break;
                    }
                }

                #endregion
            }
        }
        /// <summary>
        /// This overload lets the user pass in their own random function -- ex: rand.NextPow(2)
        /// </summary>
        /// <param name="rand">
        /// int1 = min value
        /// int2 = max value (up to, but not including max value)
        /// return = a random index
        /// </param>
        public static IEnumerable<int> RandomRange(int start, int rangeCount, int iterateCount, Func<int, int, int> rand)
        {
            if (iterateCount > rangeCount)
            {
                //throw new ArgumentOutOfRangeException(string.Format("iterateCount can't be greater than rangeCount.  iterateCount={0}, rangeCount={1}", iterateCount.ToString(), rangeCount.ToString()));
                iterateCount = rangeCount;
            }

            if (iterateCount < rangeCount * .15)
            {
                #region While Loop

                // Rather than going through the overhead of building an array of all values up front, just remember what's been returned
                List<int> used = new List<int>();
                int maxValue = start + rangeCount;

                for (int cntr = 0; cntr < iterateCount; cntr++)
                {
                    // Find a value that hasn't been returned yet
                    int retVal = 0;
                    while (true)
                    {
                        retVal = rand(start, maxValue);

                        if (!used.Contains(retVal))
                        {
                            used.Add(retVal);
                            break;
                        }
                    }

                    // Return this
                    yield return retVal;
                }

                #endregion
            }
            else if (iterateCount > 0)
            {
                #region Destroy list

                // Since the random passed in is custom, there is a good chance that the list the indices will be used for is sorted.
                // So create a list of candidate indices, and whittle that down

                List<int> available = new List<int>(Enumerable.Range(start, rangeCount));

                for (int cntr = 0; cntr < iterateCount; cntr++)
                {
                    int index = rand(0, available.Count);

                    yield return available[index];
                    available.RemoveAt(index);
                }

                #endregion
            }
        }

        /// <summary>
        /// This enumerates the array in a random order
        /// </summary>
        public static IEnumerable<T> RandomOrder<T>(T[] array, int? max = null)
        {
            int actualMax = max ?? array.Length;
            if (actualMax > array.Length)
            {
                actualMax = array.Length;
            }

            foreach (int index in RandomRange(0, array.Length, actualMax))
            {
                yield return array[index];
            }
        }
        /// <summary>
        /// This enumerates the list in a random order
        /// </summary>
        public static IEnumerable<T> RandomOrder<T>(IList<T> list, int? max = null)
        {
            int actualMax = max ?? list.Count;
            if (actualMax > list.Count)
            {
                actualMax = list.Count;
            }

            foreach (int index in RandomRange(0, list.Count, actualMax))
            {
                yield return list[index];
            }
        }

        /// <summary>
        /// I had a case where I had several arrays that may or may not be null, and wanted to iterate over all of the non null ones
        /// Usage: foreach(T item in Iterate(array1, array2, array3))
        /// </summary>
        /// <remarks>
        /// I just read about a method called Concat, which seems to be very similar to this Iterate (but iterate can handle null inputs)
        /// </remarks>
        public static IEnumerable<T> Iterate<T>(IEnumerable<T> list1 = null, IEnumerable<T> list2 = null, IEnumerable<T> list3 = null, IEnumerable<T> list4 = null, IEnumerable<T> list5 = null, IEnumerable<T> list6 = null, IEnumerable<T> list7 = null, IEnumerable<T> list8 = null)
        {
            if (list1 != null)
            {
                foreach (T item in list1)
                {
                    yield return item;
                }
            }

            if (list2 != null)
            {
                foreach (T item in list2)
                {
                    yield return item;
                }
            }

            if (list3 != null)
            {
                foreach (T item in list3)
                {
                    yield return item;
                }
            }

            if (list4 != null)
            {
                foreach (T item in list4)
                {
                    yield return item;
                }
            }

            if (list5 != null)
            {
                foreach (T item in list5)
                {
                    yield return item;
                }
            }

            if (list6 != null)
            {
                foreach (T item in list6)
                {
                    yield return item;
                }
            }

            if (list7 != null)
            {
                foreach (T item in list7)
                {
                    yield return item;
                }
            }

            if (list8 != null)
            {
                foreach (T item in list8)
                {
                    yield return item;
                }
            }
        }
        /// <summary>
        /// This lets T's and IEnumerable(T)'s be intermixed
        /// </summary>
        public static IEnumerable<T> Iterate<T>(params object[] items)
        {
            foreach (object item in items)
            {
                if (item == null)
                {
                    continue;
                }
                else if (item is T)
                {
                    yield return (T)item;
                }
                else if (item is IEnumerable<T>)
                {
                    foreach (T child in (IEnumerable<T>)item)
                    {
                        //NOTE: child could be null.  I originally had if(!null), but that is inconsistent with how the other overload is written
                        yield return (T)child;
                    }
                }
                else
                {
                    throw new ArgumentException(string.Format("Unexpected type ({0}).  Should have been singular or enumerable ({1})", item.GetType().ToString(), typeof(T).ToString()));
                }
            }
        }

        /// <summary>
        /// This can be used to iterate over line segments of a polygon
        /// </summary>
        /// <remarks>
        /// If 4 is passed in, this will return:
        ///     0,1
        ///     1,2
        ///     2,3
        ///     3,0
        /// </remarks>
        public static IEnumerable<(int from, int to)> IterateEdges(int count)
        {
            for (int cntr = 0; cntr < count - 1; cntr++)
            {
                yield return (cntr, cntr + 1);
            }

            yield return (count - 1, 0);
        }

        /// <summary>
        /// This returns all combinations of the lists passed in.  This is a nested loop, which makes it easier to
        /// write linq statements against
        /// </summary>
        public static IEnumerable<(T1, T2)> Collate<T1, T2>(IEnumerable<T1> t1s, IEnumerable<T2> t2s)
        {
            T2[] t2Arr = t2s.ToArray();

            foreach (T1 t1 in t1s)
            {
                foreach (T2 t2 in t2Arr)
                {
                    yield return (t1, t2);
                }
            }
        }
        public static IEnumerable<(T1, T2, T3)> Collate<T1, T2, T3>(IEnumerable<T1> t1s, IEnumerable<T2> t2s, IEnumerable<T3> t3s)
        {
            T2[] t2Arr = t2s.ToArray();
            T3[] t3Arr = t3s.ToArray();

            foreach (T1 t1 in t1s)
            {
                foreach (T2 t2 in t2Arr)
                {
                    foreach (T3 t3 in t3Arr)
                    {
                        yield return (t1, t2, t3);
                    }
                }
            }
        }
        public static IEnumerable<(T1, T2, T3, T4)> Collate<T1, T2, T3, T4>(IEnumerable<T1> t1s, IEnumerable<T2> t2s, IEnumerable<T3> t3s, IEnumerable<T4> t4s)
        {
            T2[] t2Arr = t2s.ToArray();
            T3[] t3Arr = t3s.ToArray();
            T4[] t4Arr = t4s.ToArray();

            foreach (T1 t1 in t1s)
            {
                foreach (T2 t2 in t2Arr)
                {
                    foreach (T3 t3 in t3Arr)
                    {
                        foreach (T4 t4 in t4Arr)
                        {
                            yield return (t1, t2, t3, t4);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// This iterates over all possible pairs of the items
        /// </summary>
        /// <remarks>
        /// if you pass in:
        /// { A, B, C, D, E}
        /// 
        /// you get:
        /// { A, B }, { A, C}, { A, D }, { A, E }, { B, C }, { B, D }, { B, E }, { C, D }, { C, E }, { D, E }
        /// </remarks>
        public static IEnumerable<Tuple<T, T>> GetPairs<T>(T[] items)
        {
            for (int outer = 0; outer < items.Length - 1; outer++)
            {
                for (int inner = outer + 1; inner < items.Length; inner++)
                {
                    yield return Tuple.Create(items[outer], items[inner]);
                }
            }
        }
        public static IEnumerable<Tuple<T, T>> GetPairs<T>(IList<T> items)
        {
            for (int outer = 0; outer < items.Count - 1; outer++)
            {
                for (int inner = outer + 1; inner < items.Count; inner++)
                {
                    yield return Tuple.Create(items[outer], items[inner]);
                }
            }
        }
        public static IEnumerable<Tuple<int, int>> GetPairs(int count)
        {
            for (int outer = 0; outer < count - 1; outer++)
            {
                for (int inner = outer + 1; inner < count; inner++)
                {
                    yield return Tuple.Create(outer, inner);
                }
            }
        }

        /// <summary>
        /// This is like calling RandomRange() on GetPairs().  But is optimized to not build all intermediate pairs
        /// </summary>
        /// <param name="itemCount">This would be the count passed into GetPairs</param>
        /// <param name="returnCount">This is how many random samples to take</param>
        public static IEnumerable<Tuple<int, int>> GetRandomPairs(int itemCount, int returnCount)
        {
            int linkCount = ((itemCount * itemCount) - itemCount) / 2;

            return RandomRange(0, linkCount, returnCount).
                Select(o => GetPair(o, itemCount));
        }

        /// <summary>
        /// WARNING: Only use this overload if the type is comparable with .Equals - (like int)
        /// </summary>
        public static Tuple<T[], bool>[] GetChains<T>(IEnumerable<Tuple<T, T>> segments)
        {
            return GetChains(segments, (o, p) => o.Equals(p));
        }
        /// <summary>
        /// This converts the set of segments into chains (good for making polygons out of line segments)
        /// WARNING: This method fails if the segments form a spoke wheel (more than 2 segments share a point)
        /// </summary>
        /// <returns>
        /// Item1=A chain or loop of items
        /// Item2=True: Loop, False: Chain
        /// </returns>
        public static Tuple<T[], bool>[] GetChains<T>(IEnumerable<Tuple<T, T>> segments, Func<T, T, bool> compare)
        {
            // Convert the segments into chains
            List<T[]> chains = segments.
                Select(o => new[] { o.Item1, o.Item2 }).
                ToList();

            // Keep trying to merge the chains until no more merges are possible
            while (true)
            {
                #region Merge pass

                if (chains.Count == 1) break;

                bool hadJoin = false;

                for (int outer = 0; outer < chains.Count - 1; outer++)
                {
                    for (int inner = outer + 1; inner < chains.Count; inner++)
                    {
                        // See if these two can be merged
                        T[] newChain = TryJoinChains(chains[outer], chains[inner], compare);

                        if (newChain != null)
                        {
                            // Swap the sub chains with the new combined one
                            chains.RemoveAt(inner);
                            chains.RemoveAt(outer);

                            chains.Add(newChain);

                            hadJoin = true;
                            break;
                        }
                    }

                    if (hadJoin) break;
                }

                if (!hadJoin) break;        // compared all the mini chains, and there were no merges.  Quit looking

                #endregion
            }

            #region Detect loops

            List<Tuple<T[], bool>> retVal = new List<Tuple<T[], bool>>();

            foreach (T[] chain in chains)
            {
                if (compare(chain[0], chain[chain.Length - 1]))
                {
                    T[] loop = chain.Skip(1).ToArray();
                    retVal.Add(Tuple.Create(loop, true));
                }
                else
                {
                    retVal.Add(Tuple.Create(chain, false));
                }
            }

            #endregion

            return retVal.ToArray();
        }

        /// <summary>
        /// This is a pretty specific use case.  Within an NxN square, each column and row can only be used once.  This method iterates
        /// over all possible sets of those arrangments
        /// </summary>
        public static IEnumerable<(int index1, int index2)[]> AllUniquePairSets(int count)
        {
            if (count == 1)
            {
                yield return new[]
                {
                    (0, 0)
                };
                yield break;
            }

            #region initialize worker nodes

            // This is a very OO approach.  Somewhat easy to implement instead of having a master method with lots of for loops back and forth, but not
            // the most intuitive to use.  The idea is build a chain of nodes, only publicly give instructions to the first node.  That instruction bounces back and
            // forth internally among the nodes.  Then read the results

            // The result set is always square (count x count).  So create columns[count], telling each to have count rows
            EnumerateColumn[] columns = Enumerable.Range(0, count).
                Select(o => new EnumerateColumn(count)).
                ToArray();

            // Link them together
            for (int cntr = 0; cntr < count; cntr++)
            {
                if (cntr > 0)
                {
                    columns[cntr].Left = columns[cntr - 1];
                }

                if (cntr < count - 1)
                {
                    columns[cntr].Right = columns[cntr + 1];
                }
            }

            #endregion

            // Tell them to take on the first state
            columns[0].Start();

            // Return that first state
            yield return columns.
                Select((o, i) => (i, o.Value)).
                ToArray();

            // Iterate over the rest of the states
            while (true)
            {
                if (!columns[0].Advance())
                {
                    break;
                }

                yield return columns.
                    Select((o, i) => (i, o.Value)).
                    ToArray();
            }
        }

        /// <summary>
        /// If there are 10 items, and the percent is .43, then the 4th item will be returned (index=3)
        /// </summary>
        /// <param name="percent">A percent (from 0 to 1)</param>
        /// <param name="count">The number of items in the list</param>
        /// <returns>The index into the list</returns>
        public static int GetIndexIntoList(double percent, int count)
        {
            if (count <= 0)
            {
                throw new ArgumentException("Count must be greater than zero");
            }

            int retVal = Convert.ToInt32(Math.Floor(count * percent));
            if (retVal < 0) retVal = 0;
            if (retVal >= count) retVal = count - 1;

            return retVal;
        }
        /// <summary>
        /// This walks fractionsOfWhole, and returns the index that percent lands on
        /// NOTE: fractionsOfWhole should be sorted descending
        /// WARNING: fractionsOfWhole.Sum(o => o.Item2) must be one.  If it's less, this method will sometimes return -1.  If it's more, the items over one will never be chosen
        /// </summary>
        /// <param name="percent">The percent to seek</param>
        /// <param name="fractionsOfWhole">
        /// Item1=Index into original list (this isn't used by this method, but will be a link to the item represented by this item)
        /// Item2=Percent of whole that this item represents (the sum of the percents should add up to 1)
        /// </param>
        public static int GetIndexIntoList(double percent, (int index, double percent)[] fractionsOfWhole)
        {
            double used = 0;

            for (int cntr = 0; cntr < fractionsOfWhole.Length; cntr++)
            {
                if (percent >= used && percent <= used + fractionsOfWhole[cntr].percent)
                {
                    return cntr;
                }

                used += fractionsOfWhole[cntr].percent;
            }

            return -1;
        }

        /// <summary>
        /// This tells where to insert to keep it sorted
        /// </summary>
        public static int GetInsertIndex<T>(IEnumerable<T> items, T newItem) where T : IComparable<T>
        {
            int index = 0;

            foreach (T existing in items)
            {
                if (existing.CompareTo(newItem) > 0)
                {
                    return index;
                }

                index++;
            }

            return index;
        }

        /// <summary>
        /// This creates a new array with the item added to the end
        /// </summary>
        public static T[] ArrayAdd<T>(T[] array, T item)
        {
            if (array == null)
            {
                return new T[] { item };
            }

            T[] retVal = new T[array.Length + 1];

            Array.Copy(array, retVal, array.Length);
            retVal[retVal.Length - 1] = item;

            return retVal;
        }
        /// <summary>
        /// This creates a new array with the items added to the end
        /// </summary>
        public static T[] ArrayAdd<T>(T[] array, T[] items)
        {
            if (array == null)
            {
                return items.ToArray();
            }
            else if (items == null)
            {
                return array.ToArray();
            }

            T[] retVal = new T[array.Length + items.Length];

            Array.Copy(array, retVal, array.Length);
            Array.Copy(items, 0, retVal, array.Length, items.Length);

            return retVal;
        }

        /// <summary>
        /// Returns true if both lists share the same item
        /// </summary>
        /// <remarks>
        /// Example of True:
        ///     { 1, 2, 3, 4 }
        ///     { 5, 6, 7, 2 }
        /// 
        /// Example of False:
        ///     { 1, 2, 3, 4 }
        ///     { 5, 6, 7, 8 }
        /// </remarks>
        public static bool SharesItem<T>(IEnumerable<T> list1, IEnumerable<T> list2)
        {
            foreach (T item1 in list1)
            {
                if (list2.Any(item2 => item2.Equals(item1)))
                {
                    return true;
                }
            }

            return false;
        }

        public static T[][] ConvertJaggedArray<T>(object[][] jagged)
        {
            return jagged.
                Select(o => o.Select(p => (T)p).ToArray()).
                ToArray();
        }

        /// <summary>
        /// This is a helper method that wraps objects to be passed to SeparateUnlinkedSets()
        /// </summary>
        public static Tuple<LinkedItemWrapper<Titem>[], LinkWrapper<Titem, Tlink>[]> GetWrappers<Titem, Tlink>(Titem[] items, Tuple<int, int, Tlink>[] links)
        {
            // Items
            var itemWrappers = items.
                Select((o, i) => new LinkedItemWrapper<Titem>()
                {
                    Item = o,
                    Index = i,
                }).
                ToArray();

            for (int cntr = 0; cntr < itemWrappers.Length; cntr++)
            {
                itemWrappers[cntr].Links = links.
                    Select(o =>
                    {
                        if (o.Item1 == cntr)
                        {
                            return itemWrappers[o.Item2];
                        }
                        else if (o.Item2 == cntr)
                        {
                            return itemWrappers[o.Item1];
                        }
                        else
                        {
                            return null;
                        }
                    }).
                    Where(o => o != null).
                    ToArray();
            }

            // Links
            var linkWrappers = links.
                Select(o => new LinkWrapper<Titem, Tlink>()
                {
                    Link = o.Item3,
                    Item1 = itemWrappers[o.Item1],
                    Item2 = itemWrappers[o.Item2],
                }).
                ToArray();

            return Tuple.Create(itemWrappers, linkWrappers);
        }
        /// <summary>
        /// This separates unique islands of sets
        /// </summary>
        /// <remarks>
        /// The items and links could be anything: tables and constraints, social media friends, etc
        /// 
        /// This method identifies independent sets of items, and puts them in their own slot in the return array
        /// 
        /// Example:
        /// Input:
        ///     A, B, C, D, E
        ///     A-B
        ///     B-E
        ///     C-D
        /// 
        /// Output:
        ///     A,B,E | A-B, B-E
        ///     C,D | C-D
        /// </remarks>
        /// <typeparam name="Titem">This is just along for the ride, could be null</typeparam>
        /// <typeparam name="Tlink">This is just along for the ride, could be null</typeparam>
        /// <param name="consolidateSetLengths">
        /// If these sets are passed to a visual clustering algorithm, or some other report, you may want sets of 2 or 3 to be bundled into the
        /// same set.  This is the max length of the small sets (if you want every set up to 3 together, then pass in 3)
        /// </param>
        public static Tuple<LinkedItemWrapper<Titem>[], LinkWrapper<Titem, Tlink>[]>[] SeparateUnlinkedSets<Titem, Tlink>(LinkedItemWrapper<Titem>[] items, LinkWrapper<Titem, Tlink>[] links, int? consolidateSetLengths = null)
        {
            var retVal = new List<Tuple<LinkedItemWrapper<Titem>[], LinkWrapper<Titem, Tlink>[]>>();

            var unlinked = items.
                Where(o => o.Links == null || o.Links.Length == 0).
                ToArray();

            if (unlinked.Length > 0)
            {
                retVal.Add(Tuple.Create(unlinked, new LinkWrapper<Titem, Tlink>[0]));

                items = items.
                    Where(o => o.Links != null && o.Links.Length > 0).
                    ToArray();
            }

            var remainingItems = new List<LinkedItemWrapper<Titem>>(items);
            var remainingLinks = new List<LinkWrapper<Titem, Tlink>>(links);

            while (remainingItems.Count > 0)
            {
                retVal.Add(SeparateUnlinkedSets_Set(remainingItems, remainingLinks));
            }

            // There tends to be lots of 2s and 3s, so make a single set of them
            if (consolidateSetLengths != null)
            {
                SeparateUnlinkedSets_Consolidate(retVal, consolidateSetLengths.Value);
            }

            return retVal.ToArray();
        }

        /// <summary>
        /// This returns a map between the old index and new index after some items are removed
        /// </summary>
        /// <remarks>
        /// ex: count=6, remove={ 2, 4, 4, 0 }
        /// final={ -1, 0, -1, 1, -1, 2 }
        /// 0: -1
        /// 1: 0
        /// 2: -1
        /// 3: 1
        /// 4: -1
        /// 5: 2
        /// </remarks>
        /// <param name="count">how many items were in the original list</param>
        /// <param name="removeIndices">indices that will be removed from the original list</param>
        /// <returns>
        /// map: an array the size of count.  Each element will be the index to the reduced array, or -1 for removed items
        /// from_to: an array the same size as the reduced list that tells old an new index
        /// </returns>
        public static (int[] map, (int from, int to)[] from_to) GetIndexMap(int count, IEnumerable<int> removeIndices)
        {
            List<int> preMap = Enumerable.Range(0, count).
                Select(o => o).
                ToList();

            foreach (int rem in removeIndices.Distinct())
            {
                preMap.Remove(rem);
            }

            var midMap = preMap.
                Select((o, i) => (from: o, to: i)).
                ToArray();

            int[] map = Enumerable.Range(0, count).
                //Select(o => midMap.FirstOrDefault(p => p.from == o)?.to ?? -1).       // can't use ?. with a new style tuple
                Select(o =>
                {
                    foreach (var mid in midMap)
                        if (mid.from == o)
                            return mid.to;
                    return -1;
                }).
                ToArray();

            return (map, midMap);
        }

        /// <summary>
        /// This is useful if you have some outer loop that needs to access a set of items in a round robin.  Just hand
        /// that loop an enumerator of this
        /// </summary>
        /// <remarks>
        /// var getIndex = UtilityCore.InfiniteRoundRobin(count).GetEnumerator();
        /// 
        /// while (someCondition)
        /// {
        ///     getIndex.MoveNext();
        ///     int index = getIndex.Current;
        ///     
        ///     ...
        /// }
        /// </remarks>
        public static IEnumerable<int> InfiniteRoundRobin(int itemCount)
        {
            while (true)
            {
                for (int cntr = 0; cntr < itemCount; cntr++)
                {
                    yield return cntr;
                }
            }
        }
        public static IEnumerable<T> InfiniteRoundRobin<T>(T[] items)
        {
            foreach (int index in InfiniteRoundRobin(items.Length))
            {
                yield return items[index];
            }
        }

        #endregion

        #region Serialization/Save

        /// <summary>
        /// This serializes/deserializes to do a deep clone
        /// </summary>
        /// <param name="useJSON">
        /// True=Serializes to/from json
        /// False=Serializes to/from xaml
        /// </param>
        /// <remarks>
        /// When using xaml:
        /// WARNING: This only works if T is serializable
        /// WARNING: Dictionary fails on load, use SortedList instead
        /// WARNING: This fails with classes declared inside of other classes
        /// 
        /// When using json
        /// WARNING: Seems to fail a lot with wpf objects
        /// WARNING: I had a case where it chose to deserialize as a base class when none of the derived class's properties were set
        /// </remarks>
        public static T Clone<T>(T item, bool useJSON = false)
        {
            if (useJSON)
            {
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                return (T)serializer.Deserialize(serializer.Serialize(item), typeof(T));
            }
            else
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    XamlServices.Save(stream, item);
                    stream.Position = 0;
                    return (T)XamlServices.Load(stream);
                }
            }
        }
        /// <summary>
        /// Instead of serialize/deserialize, this uses reflection
        /// </summary>
        /// <remarks>
        /// One big disadvantage of xamlservices save/load is that it can't handle classes defined within other classes.  If there is a class like that,
        /// and it only has 1 level of value props, then this method is good enough
        /// 
        /// NOTE: Clone now uses json, but I haven't tested it much, so there could be other types that fail (like Tuple)
        /// </remarks>
        public static T Clone_Shallow<T>(T item) where T : class
        {
            T retVal = Activator.CreateInstance<T>();

            foreach (PropertyInfo prop in typeof(T).GetProperties())
            {
                if (prop.CanRead && prop.CanWrite)
                {
                    SetValue(prop, retVal, 0, GetValue(prop, item, 0));
                }
            }

            foreach (FieldInfo field in typeof(T).GetFields())
            {
                if (field.IsLiteral)// || field.IsInitOnly) it seems to allow setting initonly (readonly)
                {
                    continue;
                }

                SetValue(field, retVal, 0, GetValue(field, item, 0));
            }

            return retVal;
        }

        /// <remarks>
        ///http://stackoverflow.com/questions/18538428/loading-a-json-file-into-c-sharp-program
        ///
        /// See Microsofts JavaScriptSerializer
        /// 
        /// The JavaScriptSerializer class is used internally by the asynchronous communication layer to serialize and deserialize the data that
        /// is passed between the browser and the Web server. You cannot access that instance of the serializer. However, this class exposes a
        /// public API. Therefore, you can use the class when you want to work with JavaScript Object Notation (JSON) in managed code.
        /// 
        /// Assembly: System.Web.Extensions (in System.Web.Extensions.dll)
        /// 
        /// Namespace: System.Web.Script.Serialization
        /// </remarks>
        /// <param name="useJSON">
        /// True=JSON
        /// False=XAML
        /// </param>
        /// <param name="jsonEmbedType">If this is true, puts [type] as the first line.  This is non standard json, but if read by this.DeserializeFromFile, it will be understood</param>
        public static void SerializeToFile(string filename, object obj, SerializeMode mode = SerializeMode.XAML)
        {
            // Tack on .json or .xml if needed
            string endsWith = mode == SerializeMode.XAML ? ".xml" : ".json";

            if (!filename.EndsWith(endsWith, StringComparison.OrdinalIgnoreCase))
            {
                filename += endsWith;
            }

            // Get the type if needed
            string type = null;
            if (mode == SerializeMode.JSON_TypeInFileContents || mode == SerializeMode.JSON_TypeInFileName)
            {
                type = obj.GetType().ToString();
            }

            // Create the file
            using (StreamWriter writer = new StreamWriter(filename, false))
            {
                if (mode == SerializeMode.JSON_TypeInFileContents)
                {
                    writer.WriteLine("[" + type + "]");
                }

                // Serialize it
                switch (mode)
                {
                    case SerializeMode.JSON_NoType:
                    case SerializeMode.JSON_TypeInFileContents:
                    case SerializeMode.JSON_TypeInFileName:
                        writer.Write(new JavaScriptSerializer().Serialize(obj));
                        break;

                    case SerializeMode.XAML:
                        writer.Write(XamlServices.Save(obj));
                        break;

                    default:
                        throw new ApplicationException("Unknown SerializeMode: " + mode.ToString());
                }
            }
        }
        public static object DeserializeFromFile(string filename)
        {
            if (filename.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            {
                #region xaml

                using (FileStream file = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    return XamlServices.Load(file);
                }

                #endregion
            }
            else if (filename.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                #region json

                using (StreamReader reader = new StreamReader(new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                {
                    // See if the first line contains the type (serialized with SerializeMode.JSON_TypeInFileContents)
                    //NOTE: The file could be huge, and on a single line
                    string firstLine = reader.ReadLine();

                    string type = null;

                    // Get type from file contents (also gets reader's position ready for a ReadToEnd)
                    if (firstLine.StartsWith("[") && firstLine.EndsWith("]"))
                    {
                        type = firstLine.Substring(1, firstLine.Length - 2);
                    }
                    else
                    {
                        // It's less efficient to reread the first line, but safer.  Did the line end with \n or \r\n?  Does it matter?
                        reader.BaseStream.Position = 0;
                    }

                    // Get type from filename
                    if (type == null)
                    {
                        Match typeMatch = Regex.Match(filename, @"^(?<type>[^ ]+) - ");
                        if (typeMatch.Success)
                        {
                            type = typeMatch.Groups["type"].Value;
                        }
                    }

                    // Cast type
                    Type typeCast = null;
                    if (type != null)
                    {
                        try
                        {
                            typeCast = Type.GetType(type);
                        }
                        catch (Exception) { }
                    }

                    if (typeCast == null)
                    {
                        // Unknown type.  This returns a Dictionary<string, object>
                        return new JavaScriptSerializer().DeserializeObject(reader.ReadToEnd());
                    }
                    else
                    {
                        // Deserialize into the specified type
                        return new JavaScriptSerializer().Deserialize(reader.ReadToEnd(), typeCast);
                    }
                }

                #endregion
            }
            else
            {
                throw new ApplicationException("Can't determine type of file (unknown extension): " + filename);
            }
        }
        public static T DeserializeFromFile<T>(string filename)
        {
            if (filename.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            {
                #region xaml

                using (FileStream file = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    return (T)XamlServices.Load(file);
                }

                #endregion
            }
            else if (filename.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                #region json

                using (StreamReader reader = new StreamReader(new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                {
                    // See if the first line contains the type (serialized with SerializeMode.JSON_TypeInFileContents)
                    //NOTE: The file could be huge, and on a single line
                    string firstLine = reader.ReadLine();

                    if (!firstLine.StartsWith("["))
                    {
                        // It's less efficient to reread the first line, but safer.  Did the line end with \n or \r\n?  Does it matter?
                        reader.BaseStream.Position = 0;
                    }

                    //NOTE: If the file defines a type, I could compare that type to T, but I'd rather just let the serializer throw a cast
                    //exception if it's the wrong type

                    return (T)new JavaScriptSerializer().Deserialize(reader.ReadToEnd(), typeof(T));
                }

                #endregion
            }
            else
            {
                throw new ApplicationException("Can't determine type of file (unknown extension): " + filename);
            }
        }

        public static T DeserializeFromString<T>(string text, SerializeMode mode = SerializeMode.XAML)
        {
            if (mode == SerializeMode.XAML)
            {
                #region xaml

                using (MemoryStream stream = new MemoryStream(Encoding.Unicode.GetBytes(text)))
                {
                    return (T)XamlServices.Load(stream);
                }

                #endregion
            }
            else
            {
                #region json

                // SerializeToFile
                if (text.StartsWith("["))
                {
                    int index = text.IndexOf('\n');
                    if (index < 0 || text.Length <= index + 1)
                    {
                        throw new ApplicationException("Couldn't deserialize json:  Couldn't get to the json");
                    }

                    text = text.Substring(index + 1);
                }

                //NOTE: If the file defines a type, I could compare that type to T, but I'd rather just let the serializer throw a cast
                //exception if it's the wrong type
                return (T)new JavaScriptSerializer().Deserialize(text, typeof(T));

                #endregion
            }
        }

        /// <summary>
        /// This deserializes the options class from a previous call in appdata
        /// </summary>
        public static T ReadOptions<T>(string filenameNoFolder) where T : class
        {
            string filename = GetOptionsFilename(filenameNoFolder);
            if (!File.Exists(filename))
            {
                return null;
            }

            T retVal = null;
            using (FileStream file = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                //deserialized = XamlReader.Load(file);		// this is the old way, it doesn't like generic lists
                retVal = XamlServices.Load(file) as T;
            }

            return retVal;
        }
        public static void SaveOptions<T>(T options, string filenameNoFolder) where T : class
        {
            string filename = GetOptionsFilename(filenameNoFolder);

            //string xamlText = XamlWriter.Save(options);		// this is the old one, it doesn't like generic lists
            string xamlText = XamlServices.Save(options);

            using (StreamWriter writer = new StreamWriter(filename, false))
            {
                writer.Write(xamlText);
            }
        }
        private static string GetOptionsFilename(string filenameNoFolder)
        {
            string foldername = UtilityCore.GetOptionsFolder();
            return Path.Combine(foldername, filenameNoFolder);
        }

        /// <summary>
        /// This is where all user options xml files should be stored
        /// </summary>
        public static string GetOptionsFolder()
        {
            string foldername = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            foldername = Path.Combine(foldername, "Asteroid Miner");

            // Make sure the folder exists
            if (!Directory.Exists(foldername))
            {
                Directory.CreateDirectory(foldername);
            }

            return foldername;
        }

        #endregion

        #region Private Methods

        // These were copied from MutateUtility.PropTracker
        private static object GetValue(PropertyInfo prop, object item, int subIndex)
        {
            string name = prop.PropertyType.FullName;

            if (name.EndsWith("[][][]"))
            {
                throw new ArgumentException("Can't handle more than two levels of array:" + name);
            }
            else if (name.EndsWith("[][]"))
            {
                // Jagged
                Array[] jaggedArr = (Array[])prop.GetValue(item, null);

                Tuple<int, int> jIndex = GetJaggedIndex(jaggedArr, subIndex);
                return ((Array)jaggedArr.GetValue(jIndex.Item1)).GetValue(jIndex.Item2);
            }
            else if (name.EndsWith("[]"))
            {
                // Array
                Array strArr = (Array)prop.GetValue(item, null);
                return strArr.GetValue(subIndex);
            }
            else
            {
                // Single value
                return prop.GetValue(item, null);
            }
        }
        private static object GetValue(FieldInfo field, object item, int subIndex)
        {
            string name = field.FieldType.FullName;

            if (name.EndsWith("[][][]"))
            {
                throw new ArgumentException("Can't handle more than two levels of array:" + name);
            }
            else if (name.EndsWith("[][]"))
            {
                // Jagged
                Array[] jaggedArr = (Array[])field.GetValue(item);

                Tuple<int, int> jIndex = GetJaggedIndex(jaggedArr, subIndex);
                return ((Array)jaggedArr.GetValue(jIndex.Item1)).GetValue(jIndex.Item2);
            }
            else if (name.EndsWith("[]"))
            {
                // Array
                Array strArr = (Array)field.GetValue(item);
                return strArr.GetValue(subIndex);
            }
            else
            {
                // Single value
                return field.GetValue(item);
            }
        }

        private static void SetValue(PropertyInfo prop, object item, int subIndex, object value)
        {
            string name = prop.PropertyType.FullName.ToLower();

            if (name.EndsWith("[][][]"))
            {
                throw new ArgumentException("Can't handle more than two levels of array:" + name);
            }
            else if (name.EndsWith("[][]"))
            {
                // Jagged
                Array[] jaggedArr = (Array[])prop.GetValue(item, null);

                Tuple<int, int> jIndex = GetJaggedIndex(jaggedArr, subIndex);
                ((Array)jaggedArr.GetValue(jIndex.Item1)).SetValue(value, jIndex.Item2);

                prop.SetValue(item, jaggedArr, null);
            }
            else if (name.EndsWith("[]"))
            {
                // Array
                Array strArr = (Array)prop.GetValue(item, null);
                strArr.SetValue(value, subIndex);
                prop.SetValue(item, strArr, null);		//NOTE: Technically, the array is now already modified, so there is no reason to store the array back into the class.  But it feels cleaner to do this (and will throw an exception if that property is readonly)
            }
            else
            {
                // Single value
                prop.SetValue(item, value, null);
            }
        }
        private static void SetValue(FieldInfo field, object item, int subIndex, object value)
        {
            string name = field.FieldType.FullName.ToLower();

            if (name.EndsWith("[][][]"))
            {
                throw new ArgumentException("Can't handle more than two levels of array:" + name);
            }
            else if (name.EndsWith("[][]"))
            {
                // Jagged
                Array[] jaggedArr = (Array[])field.GetValue(item);

                Tuple<int, int> jIndex = GetJaggedIndex(jaggedArr, subIndex);
                ((Array)jaggedArr.GetValue(jIndex.Item1)).SetValue(value, jIndex.Item2);

                field.SetValue(item, jaggedArr);
            }
            else if (name.EndsWith("[]"))
            {
                // Array
                Array strArr = (Array)field.GetValue(item);
                strArr.SetValue(value, subIndex);
                field.SetValue(item, strArr);		//NOTE: Technically, the array is now already modified, so there is no reason to store the array back into the class.  But it feels cleaner to do this (and will throw an exception if that property is readonly)
            }
            else
            {
                // Single value
                field.SetValue(item, value);
            }
        }

        private static Tuple<int, int> GetJaggedIndex(IEnumerable<Array> jagged, int index)
        {
            int used = 0;
            int outer = -1;

            foreach (Array arr in jagged)
            {
                outer++;

                if (arr == null || arr.Length == 0)
                {
                    continue;
                }

                if (used + arr.Length > index)
                {
                    return new Tuple<int, int>(outer, index - used);
                }

                used += arr.Length;
            }

            throw new ApplicationException("The index passed in is larger than the jagged array");
        }

        private static T[] TryJoinChains<T>(T[] chain1, T[] chain2, Func<T, T, bool> compare)
        {
            if (compare(chain1[0], chain2[0]))
            {
                return UtilityCore.Iterate(chain1.Reverse<T>(), chain2.Skip(1)).ToArray();
            }
            else if (compare(chain1[chain1.Length - 1], chain2[0]))
            {
                return UtilityCore.Iterate(chain1, chain2.Skip(1)).ToArray();
            }
            else if (compare(chain1[0], chain2[chain2.Length - 1]))
            {
                return UtilityCore.Iterate(chain2, chain1.Skip(1)).ToArray();
            }
            else if (compare(chain1[chain1.Length - 1], chain2[chain2.Length - 1]))
            {
                return UtilityCore.Iterate(chain2, chain1.Reverse<T>().Skip(1)).ToArray();
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// This calculates which pair the index points to
        /// </summary>
        /// <remarks>
        /// See GetPairs() to see how these are generated
        /// 
        /// Here is what the pairs look like for 7.  So if you pass in an index of 15, this returns [3,4].
        /// 
        ///index    left    right
        ///0	0	1
        ///1	0	2
        ///2	0	3
        ///3	0	4
        ///4	0	5
        ///5	0	6
        ///6	1	2
        ///7	1	3
        ///8	1	4
        ///9	1	5
        ///10	1	6
        ///11	2	3
        ///12	2	4
        ///13	2	5
        ///14	2	6
        ///15	3	4
        ///16	3	5
        ///17	3	6
        ///18	4	5
        ///19	4	6
        ///20	5	6
        /// 
        /// The linkCount is (c^2-c)/2, but I couldn't think of a way to do the reverse with some kind of sqrt or division (the divisor
        /// shrinks per set).  So I went with a loop to find the current set
        /// </remarks>
        private static Tuple<int, int> GetPair(int index, int itemCount)
        {
            // Init to point to the first set
            int left = 0;
            int maxIndex = itemCount - 2;
            int setSize = maxIndex + 1;

            // Loop to find the set that the index falls into
            while (setSize > 0)
            {
                if (index <= maxIndex)
                {
                    int right = left + (setSize - (maxIndex - index));
                    return Tuple.Create(left, right);
                }

                setSize -= 1;
                maxIndex += setSize;
                left++;
            }

            throw new ArgumentException(string.Format("Index is too large\r\nIndex={0}\r\nItemCount={1}\r\nLinkCount={2}", index, itemCount, ((itemCount * itemCount) - itemCount) / 2));
        }

        /// <summary>
        /// This creates all possible full sets that include the major set passed in
        /// NOTE: This doesn't go left of index, because it's assumed that this method was already called for those indices
        /// </summary>
        private static IEnumerable<int[][]> AllCombosEnumerator_Set(int[][] majorSets, int index, int max)
        {
            // There are two things this function needs to return:
            //      major[index] + combos of major[index+n] + remaining singles
            //      major[index] + remaining singles

            // Cache this list
            int[] remainingStarter = Enumerable.Range(0, max + 1).
                Where(o => !majorSets[index].Contains(o)).
                ToArray();

            if (index < majorSets.Length - 1)
            {
                // Get a list of combinations of major sets to try
                var majorSetComboIndexSets = UtilityCore.AllCombosEnumerator(majorSets.Length - index - 1).
                    Select(o => o.Select(p => p + index + 1).ToArray());

                foreach (int[] majorSetComboIndexSet in majorSetComboIndexSets)
                {
                    if (AllCombosEnumerator_IsUnique(majorSets, index, majorSetComboIndexSet))
                    {
                        yield return AllCombosEnumerator_BuildEntry(majorSets, index, majorSetComboIndexSet, remainingStarter);
                    }
                }
            }

            // Add singles
            yield return AllCombosEnumerator_BuildEntry(majorSets, index, new int[0], remainingStarter);
        }
        /// <summary>
        /// This will make sure that the jagged array holds one of everything
        /// NOTE: It doesn't ensure there won't be dupes if majorSets contains dupes across index1-indices2
        /// </summary>
        private static int[][] AllCombosEnumerator_BuildEntry(int[][] majorSets, int index1, int[] indices2, int[] remainingStarter)
        {
            List<int[]> retVal = new List<int[]>();

            List<int> remaining = new List<int>(remainingStarter);

            foreach (int majorIndex in UtilityCore.Iterate<int>(index1, indices2))
            {
                retVal.Add(majorSets[majorIndex]);

                remaining.RemoveWhere(o => majorSets[majorIndex].Contains(o));
            }

            // Add remaining singles to the end
            retVal.AddRange(remaining.Select(o => new[] { o }));

            return retVal.ToArray();
        }
        /// <summary>
        /// This returns true if all internal indices (contained in majorSets) pointed to by majorSets[index1] and majorSets[indices2] are unique
        /// </summary>
        /// <param name="majorSets">Holds sets of indices to items</param>
        /// <param name="index1">An index into majorSets</param>
        /// <param name="indices2">More indices into majorSets</param>
        private static bool AllCombosEnumerator_IsUnique(int[][] majorSets, int index1, int[] indices2)
        {
            // Get all the internal indices that are pointed to by majorSets
            int[] raw = UtilityCore.Iterate<int>(index1, indices2).
                SelectMany(o => majorSets[o]).
                ToArray();

            // If there are dupes, then the distinct count will be less than raw count
            return raw.Distinct().Count() == raw.Length;
        }

        private static Tuple<LinkedItemWrapper<Titem>[], LinkWrapper<Titem, Tlink>[]> SeparateUnlinkedSets_Set<Titem, Tlink>(List<LinkedItemWrapper<Titem>> remainingItems, List<LinkWrapper<Titem, Tlink>> remainingLinks)
        {
            var returnItems = new List<LinkedItemWrapper<Titem>>();
            var returnLinks = new List<LinkWrapper<Titem, Tlink>>();

            var toAddItems = new List<LinkedItemWrapper<Titem>>();

            toAddItems.Add(remainingItems[0]);
            remainingItems.RemoveAt(0);

            while (toAddItems.Count > 0)
            {
                var current = toAddItems[0];
                toAddItems.RemoveAt(0);

                returnItems.Add(current);

                var removedLinks = RemoveLinks(remainingLinks, current.Index);
                returnLinks.AddRange(removedLinks);

                if (removedLinks.Length > 0)
                {
                    toAddItems.AddRange(RemoveItems(remainingItems, removedLinks));
                }
            }

            return Tuple.Create(returnItems.ToArray(), returnLinks.ToArray());
        }
        private static void SeparateUnlinkedSets_Consolidate<Titem, Tlink>(List<Tuple<LinkedItemWrapper<Titem>[], LinkWrapper<Titem, Tlink>[]>> sets, int maxLength)
        {
            var items = new List<LinkedItemWrapper<Titem>>();
            var links = new List<LinkWrapper<Titem, Tlink>>();

            int index = 0;
            while (index < sets.Count)
            {
                int len = sets[index].Item1.Length;
                if (len > 1 && len <= maxLength)
                {
                    items.AddRange(sets[index].Item1);
                    links.AddRange(sets[index].Item2);
                    sets.RemoveAt(index);
                }
                else
                {
                    index++;
                }
            }

            if (items.Count > 0)
            {
                sets.Add(Tuple.Create(items.ToArray(), links.ToArray()));
            }
        }

        private static LinkWrapper<Titem, Tlink>[] RemoveLinks<Titem, Tlink>(List<LinkWrapper<Titem, Tlink>> links, int itemIndex)
        {
            var retVal = new List<LinkWrapper<Titem, Tlink>>();

            int index = 0;
            while (index < links.Count)
            {
                if (links[index].Item1.Index == itemIndex || links[index].Item2.Index == itemIndex)
                {
                    retVal.Add(links[index]);
                    links.RemoveAt(index);
                }
                else
                {
                    index++;
                }
            }

            return retVal.ToArray();
        }
        private static LinkedItemWrapper<Titem>[] RemoveItems<Titem, Tlink>(List<LinkedItemWrapper<Titem>> items, LinkWrapper<Titem, Tlink>[] links)
        {
            var retVal = new List<LinkedItemWrapper<Titem>>();

            int index = 0;
            while (index < items.Count)
            {
                if (links.Any(o => items[index].Index == o.Item1.Index || items[index].Index == o.Item2.Index))
                {
                    retVal.Add(items[index]);
                    items.RemoveAt(index);
                }
                else
                {
                    index++;
                }
            }

            return retVal.ToArray();
        }

        #endregion
    }

    #region enum: SerializeMode

    public enum SerializeMode
    {
        /// <summary>
        /// Serialize to a xaml file
        /// </summary>
        /// <remarks>
        /// These pros/cons are comparing xaml to json
        /// 
        /// Pros:
        ///     Very good for serializing/deserializing c# types
        ///     Can store circular references
        ///     
        /// Cons:
        ///     Slightly larger files
        ///     Can't serialize classes that are embedded in other classes:
        ///         public class Class1
        ///         {
        ///             public class Class1a        // xamlservices will choke when told to serialize this embedded class
        ///             {
        ///                 public string Data { get; set; }
        ///             }
        ///         }
        /// </remarks>
        XAML,
        /// <summary>
        /// This doesn't store the type that was serialized
        /// </summary>
        /// <remarks>
        /// Pros:
        ///     Nothing added to contents or filename
        ///     Type isn't needed if it won't be deserialized back into a c# type (can still deserialize into Dictionary[string,object]), or if only one type is stored in certain folders or filenames (so that the caller can call the Deserialize[T] overload)
        ///     
        /// Cons:
        ///     Can't deserialize back into the c# type that it came from (unless the caller already knows what types are stored as what files/folders)
        /// </remarks>
        JSON_NoType,
        /// <summary>
        /// This puts the type in the first line of the file:
        /// [Namespace.Type]
        /// { json contents }
        /// </summary>
        /// <remarks>
        /// Pros:
        ///     Type is known, and can be deserialized into that type
        ///     Filename stays small
        /// 
        /// Cons:
        ///     Not a valid json file (would need to strip out that first line)
        /// </remarks>
        JSON_TypeInFileContents,
        /// <summary>
        /// This puts the type in the beginning of the filename:
        /// Namespace.Type - other text.json
        /// </summary>
        /// <remarks>
        /// Pros:
        ///     Type is known, and can be deserialized into that type
        ///     The file is a valid json, and can be directly parsed by anything that expects json
        /// 
        /// Cons:
        ///     The filename could get too long
        ///     Just using obj.GetType().ToString(), so there's a chance of invalid characters
        /// </remarks>
        JSON_TypeInFileName,
    }

    #endregion

    #region class: LinkedItemWrapper

    public class LinkedItemWrapper<T>
    {
        public T Item { get; set; }
        public int Index { get; set; }
        public LinkedItemWrapper<T>[] Links { get; set; }
    }

    #endregion
    #region class: LinkWrapper

    public class LinkWrapper<Titem, Tlink>
    {
        public Tlink Link { get; set; }
        public LinkedItemWrapper<Titem> Item1 { get; set; }
        public LinkedItemWrapper<Titem> Item2 { get; set; }
    }

    #endregion
}
