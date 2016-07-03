using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Game.HelperClassesCore
{
    /// <summary>
    /// These are extension methods of various low level types (see Game.HelperClassesWPF.Extenders for wpf types)
    /// </summary>
    public static class Extenders
    {
        #region int

        /// <summary>
        /// This just does Convert.ToDouble().  It doesn't save much typing, but feels more natural
        /// </summary>
        public static double ToDouble(this int value)
        {
            return Convert.ToDouble(value);
        }

        #endregion

        #region double

        public static bool IsNearZero(this double item)
        {
            return Math.Abs(item) <= UtilityCore.NEARZERO;
        }

        // I really wanted to be able to define my own operator :(
        //      =~
        public static bool IsNearValue(this double item, double compare)
        {
            return item >= compare - UtilityCore.NEARZERO && item <= compare + UtilityCore.NEARZERO;
        }

        /// <summary>
        /// This is useful for displaying a double value in a textbox when you don't know the range (could be
        /// 1000001 or .1000001 or 10000.5 etc)
        /// </summary>
        public static string ToStringSignificantDigits(this double value, int significantDigits)
        {
            int numDecimals = GetNumDecimals(value);

            if (numDecimals < 0)
            {
                return ToStringSignificantDigits_PossibleScientific(value, significantDigits);
            }
            else
            {
                return ToStringSignificantDigits_Standard(value, significantDigits, true);
            }
        }

        public static int ToInt_Round(this double value)
        {
            return ToIntSafe(Math.Round(value));
        }
        public static int ToInt_Floor(this double value)
        {
            return ToIntSafe(Math.Floor(value));
        }
        public static int ToInt_Ceiling(this double value)
        {
            return ToIntSafe(Math.Ceiling(value));
        }

        public static byte ToByte_Round(this double value)
        {
            return ToByteSafe(Math.Round(value));
        }
        public static byte ToByte_Floor(this double value)
        {
            return ToByteSafe(Math.Floor(value));
        }
        public static byte ToByte_Ceiling(this double value)
        {
            return ToByteSafe(Math.Ceiling(value));
        }

        #endregion

        #region string

        /// <summary>
        /// This capitalizes the first letter of each word
        /// </summary>
        /// <param name="convertToLowerFirst">
        /// True: The input string is converted to lowercase first.  I think this gives the most expected results
        /// False: Words in all caps look like they're left alone
        /// </param>
        public static string ToProper(this string text, bool convertToLowerFirst = true)
        {
            if (convertToLowerFirst)
            {
                return Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(text.ToLower());
            }
            else
            {
                return Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(text);
            }
        }

        #endregion

        #region IEnumerable

        //public static IEnumerable<T> Descendants_DepthFirst<T>(this T head, Func<T, IEnumerable<T>> childrenFunc)
        //{
        //    yield return head;

        //    foreach (var node in childrenFunc(head))
        //    {
        //        foreach (var child in Descendants_DepthFirst(node, childrenFunc))
        //        {
        //            yield return child;
        //        }
        //    }
        //}
        //public static IEnumerable<T> Descendants_BreadthFirst<T>(this T head, Func<T, IEnumerable<T>> childrenFunc)
        //{
        //    yield return head;

        //    var last = head;
        //    foreach (var node in Descendants_BreadthFirst(head, childrenFunc))
        //    {
        //        foreach (var child in childrenFunc(node))
        //        {
        //            yield return child;
        //            last = child;
        //        }

        //        if (last.Equals(node)) yield break;
        //    }
        //}

        /// <summary>
        /// Lets you walk a tree as a 1D list (hard coded to depth first)
        /// </summary>
        /// <remarks>
        /// Got this here:
        /// http://www.claassen.net/geek/blog/2009/06/searching-tree-of-objects-with-linq.html
        /// 
        /// Ex (assuming Node as a property IEnumerable<Node> Children):
        ///     Node[] all = root.Descendants(o => o.Children).ToArray();
        ///     
        /// The original code has two depth first, breadth first.  But for simplicity, I'm just using depth first.  Uncomment the
        /// more explicit methods if neeeded
        /// </remarks>
        public static IEnumerable<T> Descendants<T>(this T head, Func<T, IEnumerable<T>> childrenFunc)
        {
            yield return head;

            var children = childrenFunc(head);
            if (children != null)
            {
                foreach (var node in childrenFunc(head))
                {
                    foreach (var child in Descendants(node, childrenFunc))
                    {
                        yield return child;
                    }
                }
            }
        }

        public static IEnumerable<TSource> Distinct<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            List<TSource> retVal = new List<TSource>();

            retVal.AddRangeUnique(source, keySelector);

            return retVal;
        }
        public static IEnumerable<T> Distinct<T>(this IEnumerable<T> source, Func<T, T, bool> comparer)
        {
            //List<T> retVal = new List<T>();
            //retVal.AddRangeUnique(source, comparer);
            //return retVal;

            return source.Distinct(new DelegateComparer<T>(comparer));
        }

        // Added these so the caller doesn't need pass a lambda
        public static IOrderedEnumerable<T> OrderBy<T>(this IEnumerable<T> source)
        {
            return source.OrderBy(o => o);
        }
        public static IOrderedEnumerable<T> OrderByDescending<T>(this IEnumerable<T> source)
        {
            return source.OrderByDescending(o => o);
        }

        /// <summary>
        /// This acts like the standard IndexOf, but with a custom comparer (good for floating point numbers, or objects that don't properly implement IEquatable)
        /// </summary>
        public static int IndexOf<T>(this IEnumerable<T> source, T item, Func<T, T, bool> comparer)
        {
            int index = 0;

            foreach (T candidate in source)
            {
                if (comparer(candidate, item))
                {
                    return index;
                }

                index++;
            }

            return -1;
        }

        public static bool Contains<T>(this IEnumerable<T> source, T item, Func<T, T, bool> comparer)
        {
            foreach (T candidate in source)
            {
                if (comparer(candidate, item))
                {
                    return true;
                }
            }

            return false;
        }

        public static IEnumerable<IGrouping<T, T>> GroupBy<T>(this IEnumerable<T> source)
        {
            return source.GroupBy(o => o);
        }
        /// <summary>
        /// This overload lets you pass the comparer as a delegate
        /// ex (ignoring length checks):
        /// points.GroupBy((o,p) => Math3D.IsNearValue(o, p))
        /// </summary>
        public static IEnumerable<IGrouping<T, T>> GroupBy<T>(this IEnumerable<T> source, Func<T, T, bool> comparer)
        {
            return source.GroupBy(o => o, new DelegateComparer<T>(comparer));
        }
        public static IEnumerable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TKey, TKey, bool> comparer)
        {
            return source.GroupBy(keySelector, new DelegateComparer<TKey>(comparer));
        }

        public static ILookup<T, T> ToLookup<T>(this IEnumerable<T> source)
        {
            return source.ToLookup(o => o);
        }
        public static ILookup<T, T> ToLookup<T>(this IEnumerable<T> source, Func<T, T, bool> comparer)
        {
            return source.ToLookup(o => o, new DelegateComparer<T>(comparer));
        }
        public static ILookup<TKey, TSource> ToLookup<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TKey, TKey, bool> comparer)
        {
            return source.ToLookup(keySelector, new DelegateComparer<TKey>(comparer));
        }

        #endregion

        #region IList

        public static void AddRange<T>(this IList<T> list, IEnumerable<T> items)
        {
            foreach (T item in items)
            {
                list.Add(item);
            }
        }

        /// <summary>
        /// This only adds the items that aren't already in list
        /// This overload can be used when T has a comparable that makes sense
        /// </summary>
        public static void AddRangeUnique<T>(this IList<T> list, IEnumerable<T> items)
        {
            foreach (T item in items.Distinct())
            {
                if (!list.Contains(item))
                {
                    list.Add(item);
                }
            }
        }
        /// <summary>
        /// This only adds the items that aren't already in list
        /// This overload takes a func that just returns a key that is comparable
        /// </summary>
        /// <remarks>
        /// Usage:
        /// 
        /// SomeList.AddRangeUnique(items, o => o.Prop);
        /// </remarks>
        public static void AddRangeUnique<TSource, TKey>(this IList<TSource> list, IEnumerable<TSource> items, Func<TSource, TKey> keySelector)
        {
            List<TKey> keys = new List<TKey>();

            foreach (TSource item in items)
            {
                TKey key = keySelector(item);

                bool foundIt = false;

                for (int cntr = 0; cntr < list.Count; cntr++)
                {
                    if (keys.Count <= cntr)
                    {
                        keys.Add(keySelector(list[cntr]));
                    }

                    if (keys[cntr].Equals(key))
                    {
                        foundIt = true;
                        break;
                    }
                }

                if (!foundIt)
                {
                    list.Add(item);
                    keys.Add(key);      // if execution got here, then keys is the same size as list
                }
            }
        }
        /// <summary>
        /// This only adds the items that aren't already in list
        /// This overload takes a custom comparer
        /// </summary>
        /// <remarks>
        /// Usage:
        /// 
        /// SomeList.AddRangeUnique(items, (o,p) => o.Prop1 == p.Prop1 && o.Prop2 == p.Prop2);
        /// </remarks>
        public static void AddRangeUnique<T>(this IList<T> list, IEnumerable<T> items, Func<T, T, bool> comparer)
        {
            foreach (T item in items)
            {
                bool foundIt = false;

                foreach (T listItem in list)
                {
                    if (comparer(item, listItem))
                    {
                        foundIt = true;
                        break;
                    }
                }

                if (!foundIt)
                {
                    list.Add(item);
                }
            }
        }

        public static IEnumerable<T> RemoveWhere<T>(this IList<T> list, Func<T, bool> constraint)
        {
            int index = 0;

            while (index < list.Count)
            {
                if (constraint(list[index]))
                {
                    yield return list[index];
                    list.RemoveAt(index);
                }
                else
                {
                    index++;
                }
            }
        }

        #endregion

        #region SortedList

        /// <summary>
        /// TryGetValue is really useful, but the out param can't be used in linq.  So this wraps that method to return a tuple instead
        /// </summary>
        public static Tuple<bool, TValue> TryGetValue<TKey, TValue>(this SortedList<TKey, TValue> list, TKey key)
        {
            TValue value;
            bool found = list.TryGetValue(key, out value);

            return Tuple.Create(found, value);
        }

        #endregion

        #region Random

        public static double NextDouble(this Random rand, double maxValue)
        {
            return rand.NextDouble() * maxValue;
        }
        public static double NextDouble(this Random rand, double minValue, double maxValue)
        {
            return minValue + (rand.NextDouble() * (maxValue - minValue));
        }

        /// <summary>
        /// Returns between: (mid / 1+%) to (mid * 1+%)
        /// </summary>
        /// <param name="percent">0=0%, .1=10%</param>
        /// <param name="useRandomPercent">
        /// True=Percent is random from 0*percent to 1*percent
        /// False=Percent is always percent (the only randomness is whether to go up or down)
        /// </param>
        public static double NextPercent(this Random rand, double midPoint, double percent, bool useRandomPercent = true)
        {
            double actualPercent = 1d + (useRandomPercent ? percent * rand.NextDouble() : percent);

            if (rand.Next(2) == 0)
            {
                // Add
                return midPoint * actualPercent;
            }
            else
            {
                // Remove
                return midPoint / actualPercent;
            }
        }
        /// <summary>
        /// Returns between: (mid - drift) to (mid + drift)
        /// </summary>
        /// <param name="useRandomDrift">
        /// True=Drift is random from 0*drift to 1*drift
        /// False=Drift is always drift (the only randomness is whether to go up or down)
        /// </param>
        public static double NextDrift(this Random rand, double midPoint, double drift, bool useRandomDrift = true)
        {
            double actualDrift = useRandomDrift ? drift * rand.NextDouble() : drift;

            if (rand.Next(2) == 0)
            {
                // Add
                return midPoint + actualDrift;
            }
            else
            {
                // Remove
                return midPoint - actualDrift;
            }
        }

        /// <summary>
        /// This runs Random.NextDouble^power.  This skews the probability of what values get returned
        /// </summary>
        /// <remarks>
        /// The standard call to random gives an even chance of any number between 0 and 1.  This is not an even chance
        /// 
        /// If power is greater than 1, lower numbers are preferred
        /// If power is less than 1, larger numbers are preferred
        /// 
        /// My first attempt was to use a bell curve instead of power, but the result still gave a roughly even chance of values
        /// (except a spike right around 0).  Then it ocurred to me that the bell curve describes distribution.  If you want the output
        /// to follow that curve, use the integral of that equation - or something like that :)
        /// </remarks>
        /// <param name="power">
        /// .5 would be square root
        /// 2 would be squared
        /// </param>
        /// <param name="maxValue">The output is scaled by this value</param>
        /// <param name="isPlusMinus">
        /// True=output is -maxValue to maxValue
        /// False=output is 0 to maxValue
        /// </param>
        public static double NextPow(this Random rand, double power, double maxValue = 1d, bool isPlusMinus = false)
        {
            // Run the random method through power.  This will give a greater chance of returning zero than
            // one (or the opposite if power is less than one)
            //NOTE: This works, because random only returns between 0 and 1
            double random = Math.Pow(rand.NextDouble(), power);

            double retVal = random * maxValue;

            if (!isPlusMinus)
            {
                // They only want positive
                return retVal;
            }

            // They want - to +
            if (rand.Next(2) == 0)
            {
                return retVal;
            }
            else
            {
                return -retVal;
            }
        }

        public static bool NextBool(this Random rand)
        {
            return rand.Next(2) == 0;
        }

        /// <summary>
        /// Returns a string of random upper case characters
        /// </summary>
        /// <remarks>
        /// TODO: Take in an enum for what types of characters to return (alpha, numeric, alphanumeric)
        /// TODO: Make NextSentence method that returns several "words" separated by spaces
        /// </remarks>
        public static string NextString(this Random rand, int length)
        {
            char[] retVal = new char[length];

            for (int cntr = 0; cntr < length; cntr++)
            {
                retVal[cntr] = Convert.ToChar(Convert.ToByte(Convert.ToByte('A') + rand.Next(26)));
            }

            return new string(retVal);
        }
        public static string NextString(this Random rand, int randLengthFrom, int randLengthTo)
        {
            return rand.NextString(rand.Next(randLengthFrom, randLengthTo));
        }

        /// <summary>
        /// This chooses a random item from the list.  It doesn't save much typing
        /// </summary>
        public static T NextItem<T>(this Random rand, T[] items)
        {
            return items[rand.Next(items.Length)];
        }
        public static T NextItem<T>(this Random rand, IList<T> items)
        {
            return items[rand.Next(items.Count)];
        }

        #endregion

        #region Private Methods

        private static int GetNumDecimals(double value)
        {
            string text = value.ToString(System.Globalization.CultureInfo.InvariantCulture);		// I think this forces decimal to always be a '.' ?

            if (Regex.IsMatch(text, "[a-z]", RegexOptions.IgnoreCase))
            {
                // This is in exponential notation, just give up (or maybe NaN)
                return -1;
            }

            int decimalIndex = text.IndexOf(".");

            if (decimalIndex < 0)
            {
                // It's an integer
                return 0;
            }
            else
            {
                // Just count the decimals
                return (text.Length - 1) - decimalIndex;
            }
        }

        private static string ToStringSignificantDigits_Standard(double value, int significantDigits, bool useN)
        {
            // Get the integer portion
            long intPortion = Convert.ToInt64(Math.Truncate(value));		// going directly against the value for this (min could go from 1 to 1000.  1 needs two decimal places, 10 needs one, 100+ needs zero)
            int numInt;
            if (intPortion == 0)
            {
                numInt = 0;
            }
            else
            {
                numInt = intPortion.ToString().Length;
            }

            // Limit the number of significant digits
            int numPlaces;
            if (numInt == 0)
            {
                numPlaces = significantDigits;
            }
            else if (numInt >= significantDigits)
            {
                numPlaces = 0;
            }
            else
            {
                numPlaces = significantDigits - numInt;
            }

            // I was getting an exception from round, but couldn't recreate it, so I'm just throwing this in to avoid the exception
            if (numPlaces < 0)
            {
                numPlaces = 0;
            }
            else if (numPlaces > 15)
            {
                numPlaces = 15;
            }

            // Show a rounded number
            double rounded = Math.Round(value, numPlaces);
            int numActualDecimals = GetNumDecimals(rounded);
            if (numActualDecimals < 0 || !useN)
            {
                return rounded.ToString();		// it's weird, don't try to make it more readable
            }
            else
            {
                return rounded.ToString("N" + numActualDecimals);
            }
        }
        private static string ToStringSignificantDigits_PossibleScientific(double value, int significantDigits)
        {
            string text = value.ToString(System.Globalization.CultureInfo.InvariantCulture);		// I think this forces decimal to always be a '.' ?

            Match match = Regex.Match(text, @"^(?<num>\d\.\d+)(?<exp>E(-|)\d+)$");
            if (!match.Success)
            {
                // Unknown
                return value.ToString();
            }

            string standard = ToStringSignificantDigits_Standard(Convert.ToDouble(match.Groups["num"].Value), significantDigits, false);

            return standard + match.Groups["exp"].Value;
        }

        private static int ToIntSafe(double value)
        {
            double retVal = value;
            if (retVal < int.MinValue) retVal = int.MinValue;
            else if (retVal > int.MaxValue) retVal = int.MaxValue;
            else if (Math1D.IsInvalid(retVal)) retVal = int.MaxValue;
            return Convert.ToInt32(retVal);
        }
        private static byte ToByteSafe(double value)
        {
            int retVal = ToIntSafe(Math.Ceiling(value));
            if (retVal < 0) retVal = 0;
            else if (retVal > 255) retVal = 255;
            else if (Math1D.IsInvalid(retVal)) retVal = 255;
            return Convert.ToByte(retVal);
        }

        #endregion
    }
}
