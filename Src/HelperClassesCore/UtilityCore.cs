using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xaml;
using System.Web.Script.Serialization;
using System.Text.RegularExpressions;

namespace Game.HelperClassesCore
{
    public static class UtilityCore
    {
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
            // Get the percent of value within the range
            double percent = (valueRange - minRange) / (maxRange - minRange);

            // Get the lerp between the return range
            double retVal = minReturn + (percent * (maxReturn - minReturn));

            // Cap the return value
            if (retVal < minReturn)
            {
                retVal = minReturn;
            }
            else if (retVal > maxReturn)
            {
                retVal = maxReturn;
            }

            // Exit Function
            return retVal;
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

                // Rather than going through the overhead of building an array of all values up front, just remember what I've returned
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

                // Reuse my other overload, just stop prematurely

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
        /// This lets T's and IEnumerable(T)'s to intermixed
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
        /// This returns all combinations of the lists passed in.  This is a nested loop, which makes it easier to
        /// write linq statements against
        /// </summary>
        public static IEnumerable<Tuple<T1, T2>> Collate<T1, T2>(IEnumerable<T1> t1s, IEnumerable<T2> t2s)
        {
            T2[] t2Arr = t2s.ToArray();

            foreach (T1 t1 in t1s)
            {
                foreach (T2 t2 in t2Arr)
                {
                    yield return Tuple.Create(t1, t2);
                }
            }
        }
        public static IEnumerable<Tuple<T1, T2, T3>> Collate<T1, T2, T3>(IEnumerable<T1> t1s, IEnumerable<T2> t2s, IEnumerable<T3> t3s)
        {
            T2[] t2Arr = t2s.ToArray();
            T3[] t3Arr = t3s.ToArray();

            foreach (T1 t1 in t1s)
            {
                foreach (T2 t2 in t2Arr)
                {
                    foreach (T3 t3 in t3Arr)
                    {
                        yield return Tuple.Create(t1, t2, t3);
                    }
                }
            }
        }
        public static IEnumerable<Tuple<T1, T2, T3, T4>> Collate<T1, T2, T3, T4>(IEnumerable<T1> t1s, IEnumerable<T2> t2s, IEnumerable<T3> t3s, IEnumerable<T4> t4s)
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
                            yield return Tuple.Create(t1, t2, t3, t4);
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
                #region XAML

                using (FileStream file = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    return XamlServices.Load(file);
                }

                #endregion
            }
            else if (filename.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                #region JSON

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
                #region XAML

                using (FileStream file = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    return (T)XamlServices.Load(file);
                }

                #endregion
            }
            else if (filename.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                #region JSON

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

        #endregion
    }

    #region Enum: SerializeMode

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
}
