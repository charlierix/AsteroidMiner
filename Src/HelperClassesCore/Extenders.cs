using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Game.HelperClassesCore
{
    /// <summary>
    /// These are extension methods of various low level types (see Game.HelperClassesWPF.Extenders for wpf types)
    /// </summary>
    public static class Extenders
    {
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
        public static double NextPow(this Random rand, double power, double maxValue = 1d, bool isPlusMinus = true)
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
    }
}
