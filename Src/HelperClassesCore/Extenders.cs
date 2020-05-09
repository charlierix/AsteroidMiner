using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading;

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

        public static byte ToByte(this int value)
        {
            if (value < 0) value = 0;
            else if (value > 255) value = 255;

            return Convert.ToByte(value);
        }

        #endregion

        #region long

        /// <summary>
        /// This just does Convert.ToDouble().  It doesn't save much typing, but feels more natural
        /// </summary>
        public static double ToDouble(this long value)
        {
            return Convert.ToDouble(value);
        }

        public static byte ToByte(this long value)
        {
            if (value < 0) value = 0;
            else if (value > 255) value = 255;

            return Convert.ToByte(value);
        }

        #endregion

        #region double

        public static bool IsNearZero(this double item, double threshold = UtilityCore.NEARZERO)
        {
            return Math.Abs(item) <= threshold;
        }

        public static bool IsNearValue(this double item, double compare, double threshold = UtilityCore.NEARZERO)
        {
            return item >= compare - threshold && item <= compare + threshold;
        }

        public static bool IsInvalid(this double item)
        {
            return Math1D.IsInvalid(item);
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

        #region decimal

        /// <summary>
        /// This is useful for displaying a double value in a textbox when you don't know the range (could be
        /// 1000001 or .1000001 or 10000.5 etc)
        /// </summary>
        public static string ToStringSignificantDigits(this decimal value, int significantDigits)
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

        public static string ToInvert(this string text)
        {
            char[] inverted = text.ToCharArray();

            for (int cntr = 0; cntr < inverted.Length; cntr++)
            {
                if (char.IsLetter(inverted[cntr]))
                {
                    if (char.IsUpper(inverted[cntr]))
                    {
                        inverted[cntr] = char.ToLower(inverted[cntr]);
                    }
                    else
                    {
                        inverted[cntr] = char.ToUpper(inverted[cntr]);
                    }
                }
            }

            return new string(inverted);
        }

        /// <summary>
        /// This is a string.Join, but written to look like a linq statement
        /// </summary>
        public static string ToJoin(this IEnumerable<string> strings, string separator)
        {
            return string.Join(separator, strings);
        }

        public static bool In_ignorecase(this string value, params string[] compare)
        {
            if (compare == null)
            {
                return false;
            }
            else if (value == null)
            {
                return compare.Any(o => o == null);
            }

            return compare.Any(o => value.Equals(o, StringComparison.OrdinalIgnoreCase));
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
        /// Ex (assuming Node has a property IEnumerable<Node> Children):
        ///     Node[] all = root.Descendants(o => o.Children).ToArray();
        ///     
        /// The original code has two: depth first, breadth first.  But for simplicity, I'm just using depth first.  Uncomment the
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
        //WARNING: this doesn't scale well.  Implement your own IEqualityComparer that has a good GetHashCode
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
        public static int IndexOf<T>(this IEnumerable<T> source, T item)
        {
            return source.IndexOf(item, (t1, t2) => t1.Equals(t2));
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

        public static IEnumerable<(T1, T2)> SelectManys<T1, T2>(this IEnumerable<T1> source, SelectManysArgs2<T1, T2> args)
        {
            //NOTE: This version 2 is the same as one of the overloads as the standard SelectMany.  Just including it here for completeness

            if (args == null)
            {
                throw new ArgumentNullException("args");
            }
            else if (args.Select_1_2 == null)
            {
                throw new ArgumentNullException("args.Select_1_2");
            }

            if (source == null)
            {
                yield break;
            }

            var list1 = source;

            if (args.Where_1 != null)
            {
                list1 = list1.
                    Where(o => args.Where_1(o));
            }

            foreach (T1 t1 in list1)
            {
                var list2 = args.Select_1_2(t1);

                if (args.Where_2 != null)
                {
                    list2 = list2.
                        Where(o => args.Where_2(o));
                }

                foreach (T2 t2 in list2)
                {
                    yield return (t1, t2);
                }
            }
        }
        public static IEnumerable<(T1, T2, T3)> SelectManys<T1, T2, T3>(this IEnumerable<T1> source, SelectManysArgs3<T1, T2, T3> args)
        {
            #region validate

            if (args == null)
            {
                throw new ArgumentNullException("args");
            }
            else if (args.Select_1_2 == null)
            {
                throw new ArgumentNullException("args.Select_1_2");
            }
            else if (args.Select_2_3 == null)
            {
                throw new ArgumentNullException("args.Select_2_3");
            }

            #endregion

            if (source == null)
            {
                yield break;
            }

            var list1 = source;

            if (args.Where_1 != null)
            {
                list1 = list1.
                    Where(o => args.Where_1(o));
            }

            foreach (T1 t1 in list1)
            {
                #region select 2

                var list2 = args.Select_1_2(t1);

                if (args.Where_2 != null)
                {
                    list2 = list2.
                        Where(o => args.Where_2(o));
                }

                foreach (T2 t2 in list2)
                {
                    #region select 3

                    var list3 = args.Select_2_3(t2);

                    if (args.Where_3 != null)
                    {
                        list3 = list3.
                            Where(o => args.Where_3(o));
                    }

                    foreach (T3 t3 in list3)
                    {
                        yield return (t1, t2, t3);
                    }

                    #endregion
                }

                #endregion
            }
        }
        public static IEnumerable<(T1, T2, T3, T4)> SelectManys<T1, T2, T3, T4>(this IEnumerable<T1> source, SelectManysArgs4<T1, T2, T3, T4> args)
        {
            #region validate

            if (args == null)
            {
                throw new ArgumentNullException("args");
            }
            else if (args.Select_1_2 == null)
            {
                throw new ArgumentNullException("args.Select_1_2");
            }
            else if (args.Select_2_3 == null)
            {
                throw new ArgumentNullException("args.Select_2_3");
            }
            else if (args.Select_3_4 == null)
            {
                throw new ArgumentNullException("args.Select_3_4");
            }

            #endregion

            if (source == null)
            {
                yield break;
            }

            var list1 = source;

            if (args.Where_1 != null)
            {
                list1 = list1.
                    Where(o => args.Where_1(o));
            }

            foreach (T1 t1 in list1)
            {
                #region select 2

                var list2 = args.Select_1_2(t1);

                if (args.Where_2 != null)
                {
                    list2 = list2.
                        Where(o => args.Where_2(o));
                }

                foreach (T2 t2 in list2)
                {
                    #region select 3

                    var list3 = args.Select_2_3(t2);

                    if (args.Where_3 != null)
                    {
                        list3 = list3.
                            Where(o => args.Where_3(o));
                    }

                    foreach (T3 t3 in list3)
                    {
                        #region select 4

                        var list4 = args.Select_3_4(t3);

                        if (args.Where_4 != null)
                        {
                            list4 = list4.
                                Where(o => args.Where_4(o));
                        }

                        foreach (T4 t4 in list4)
                        {
                            yield return (t1, t2, t3, t4);
                        }

                        #endregion
                    }

                    #endregion
                }

                #endregion
            }
        }
        public static IEnumerable<(T1, T2, T3, T4, T5)> SelectManys<T1, T2, T3, T4, T5>(this IEnumerable<T1> source, SelectManysArgs5<T1, T2, T3, T4, T5> args)
        {
            #region validate

            if (args == null)
            {
                throw new ArgumentNullException("args");
            }
            else if (args.Select_1_2 == null)
            {
                throw new ArgumentNullException("args.Select_1_2");
            }
            else if (args.Select_2_3 == null)
            {
                throw new ArgumentNullException("args.Select_2_3");
            }
            else if (args.Select_3_4 == null)
            {
                throw new ArgumentNullException("args.Select_3_4");
            }
            else if (args.Select_4_5 == null)
            {
                throw new ArgumentNullException("args.Select_4_5");
            }

            #endregion

            if (source == null)
            {
                yield break;
            }

            var list1 = source;

            if (args.Where_1 != null)
            {
                list1 = list1.
                    Where(o => args.Where_1(o));
            }

            foreach (T1 t1 in list1)
            {
                #region select 2

                var list2 = args.Select_1_2(t1);

                if (args.Where_2 != null)
                {
                    list2 = list2.
                        Where(o => args.Where_2(o));
                }

                foreach (T2 t2 in list2)
                {
                    #region select 3

                    var list3 = args.Select_2_3(t2);

                    if (args.Where_3 != null)
                    {
                        list3 = list3.
                            Where(o => args.Where_3(o));
                    }

                    foreach (T3 t3 in list3)
                    {
                        #region select 4

                        var list4 = args.Select_3_4(t3);

                        if (args.Where_4 != null)
                        {
                            list4 = list4.
                                Where(o => args.Where_4(o));
                        }

                        foreach (T4 t4 in list4)
                        {
                            #region select 5

                            var list5 = args.Select_4_5(t4);

                            if (args.Where_5 != null)
                            {
                                list5 = list5.
                                    Where(o => args.Where_5(o));
                            }

                            foreach (T5 t5 in list5)
                            {
                                yield return (t1, t2, t3, t4, t5);
                            }

                            #endregion
                        }

                        #endregion
                    }

                    #endregion
                }

                #endregion
            }
        }
        public static IEnumerable<(T1, T2, T3, T4, T5, T6)> SelectManys<T1, T2, T3, T4, T5, T6>(this IEnumerable<T1> source, SelectManysArgs6<T1, T2, T3, T4, T5, T6> args)
        {
            #region validate

            if (args == null)
            {
                throw new ArgumentNullException("args");
            }
            else if (args.Select_1_2 == null)
            {
                throw new ArgumentNullException("args.Select_1_2");
            }
            else if (args.Select_2_3 == null)
            {
                throw new ArgumentNullException("args.Select_2_3");
            }
            else if (args.Select_3_4 == null)
            {
                throw new ArgumentNullException("args.Select_3_4");
            }
            else if (args.Select_4_5 == null)
            {
                throw new ArgumentNullException("args.Select_4_5");
            }
            else if (args.Select_5_6 == null)
            {
                throw new ArgumentNullException("args.Select_5_6");
            }

            #endregion

            if (source == null)
            {
                yield break;
            }

            var list1 = source;

            if (args.Where_1 != null)
            {
                list1 = list1.
                    Where(o => args.Where_1(o));
            }

            foreach (T1 t1 in list1)
            {
                #region select 2

                var list2 = args.Select_1_2(t1);

                if (args.Where_2 != null)
                {
                    list2 = list2.
                        Where(o => args.Where_2(o));
                }

                foreach (T2 t2 in list2)
                {
                    #region select 3

                    var list3 = args.Select_2_3(t2);

                    if (args.Where_3 != null)
                    {
                        list3 = list3.
                            Where(o => args.Where_3(o));
                    }

                    foreach (T3 t3 in list3)
                    {
                        #region select 4

                        var list4 = args.Select_3_4(t3);

                        if (args.Where_4 != null)
                        {
                            list4 = list4.
                                Where(o => args.Where_4(o));
                        }

                        foreach (T4 t4 in list4)
                        {
                            #region select 5

                            var list5 = args.Select_4_5(t4);

                            if (args.Where_5 != null)
                            {
                                list5 = list5.
                                    Where(o => args.Where_5(o));
                            }

                            foreach (T5 t5 in list5)
                            {
                                #region select 6

                                var list6 = args.Select_5_6(t5);

                                if (args.Where_6 != null)
                                {
                                    list6 = list6.
                                        Where(o => args.Where_6(o));
                                }

                                foreach (T6 t6 in list6)
                                {
                                    yield return (t1, t2, t3, t4, t5, t6);
                                }

                                #endregion
                            }

                            #endregion
                        }

                        #endregion
                    }

                    #endregion
                }

                #endregion
            }
        }

        /// <summary>
        /// This is just like FirstOrDefault, but is meant to be used when you know that TSource is a value type.  This is hard
        /// coded to return nullable T
        /// </summary>
        /// <remarks>
        /// It's really annoying trying to use FirstOrDefault with value types, because they have to be converted to nullable first.
        /// Extra annoying if the value type is a tuple with named items:
        ///     (int index, double weight, SomeType obj)
        /// would need to be duplicated verbatim, but with a ? at the end
        ///     (int index, double weight, SomeType obj)?
        /// </remarks>
        public static TSource? FirstOrDefault_val<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate) where TSource : struct
        {
            // Here is an alternative
            //return source.
            //    Cast<TSource?>().
            //    FirstOrDefault(o => predicate(o.Value));

            foreach (TSource item in source)
            {
                if (predicate(item))
                {
                    return item;
                }
            }

            return null;
        }
        public static TSource? FirstOrDefault_val<TSource>(this IEnumerable<TSource> source) where TSource : struct
        {
            foreach (TSource item in source)
            {
                return item;
            }

            return null;
        }

        #endregion

        #region IList

        //NOTE: AsEnumerable doesn't exist for IList, but when I put it here, it was ambiguous with other collections.  So this is spelled with an i
        public static IEnumerable<object> AsEnumerabIe(this IList list)
        {
            foreach (object item in list)
            {
                yield return item;
            }
        }
        public static IEnumerable<T> AsEnumerabIe<T>(this IList<T> list)
        {
            foreach (T item in list)
            {
                yield return item;
            }
        }

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

        /// <summary>
        /// NOTE: This removes from the list.  The returned items are what was removed
        /// </summary>
        public static IEnumerable<T> RemoveWhere<T>(this IList<T> list, Func<T, bool> constraint)
        {
            List<T> removed = new List<T>();

            int index = 0;

            while (index < list.Count)
            {
                if (constraint(list[index]))
                {
                    //yield return list[index];
                    removed.Add(list[index]);       //NOTE: can't use yield, because if there are no consumers of the returned results, the compiler wasn't even calling this method
                    list.RemoveAt(index);
                }
                else
                {
                    index++;
                }
            }

            return removed;
        }

        public static void RemoveAll<T>(this IList<T> list, IEnumerable<T> itemsToRemove)
        {
            if (itemsToRemove is IList<T> itemsList)
            {
                // The remove list already supports random access, so use it directly
                list.RemoveWhere(o => itemsList.Contains(o));
            }
            else
            {
                // Cache the values in case the list is the result of an expensive linq statement (it would get reevaluated for every
                // item in list)
                T[] array = itemsToRemove.ToArray();

                list.RemoveWhere(o => array.Contains(o));
            }
        }

        public static bool IsNullOrEmpty<T>(this IList<T> list)
        {
            return list == null || list.Count == 0;
        }

        #endregion

        #region MatchCollection

        public static IEnumerable<Match> AsEnumerable(this MatchCollection matches)
        {
            foreach (Match match in matches)
            {
                yield return match;
            }
        }

        #endregion

        #region SortedList

        /// <summary>
        /// TryGetValue is really useful, but the out param can't be used in linq.  So this wraps that method to return a tuple instead
        /// </summary>
        public static (bool isSuccessful, TValue value) TryGetValue<TKey, TValue>(this SortedList<TKey, TValue> list, TKey key)
        {
            bool found = list.TryGetValue(key, out TValue value);

            return (found, value);
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
        /// Returns a boolean, but allows a threshold for true
        /// </summary>
        /// <param name="rand"></param>
        /// <param name="chanceForTrue">0 would be no chance of true.  1 would be 100% chance of true</param>
        /// <returns></returns>
        public static bool NextBool(this Random rand, double chanceForTrue = .5)
        {
            return rand.NextDouble() < chanceForTrue;
        }

        /// <summary>
        /// Returns a string of random upper case characters
        /// </summary>
        /// <remarks>
        /// TODO: Make NextSentence method that returns several "words" separated by spaces
        /// </remarks>
        public static string NextString(this Random rand, int length, string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ")
        {
            return new string
            (
                Enumerable.Range(0, length).
                    Select(o => chars[rand.Next(chars.Length)]).
                    ToArray()
            );
        }
        public static string NextString(this Random rand, int randLengthFrom, int randLengthTo, string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ")
        {
            return rand.NextString(rand.Next(randLengthFrom, randLengthTo), chars);
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

        #region T

        public static bool In<T>(this T value, params T[] compare)
        {
            if (compare == null)
            {
                return false;
            }
            else if (value == null)
            {
                return compare.Any(o => o == null);
            }

            return compare.Any(o => value.Equals(o));
        }

        #endregion

        #region Private Methods

        private static int GetNumDecimals(double value)
        {
            return GetNumDecimals_ToString(value.ToString(System.Globalization.CultureInfo.InvariantCulture));      // I think this forces decimal to always be a '.' ?
        }
        private static int GetNumDecimals(decimal value)
        {
            return GetNumDecimals_ToString(value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        private static int GetNumDecimals_ToString(string text)
        {
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

        private static string ToStringSignificantDigits_PossibleScientific(double value, int significantDigits)
        {
            return ToStringSignificantDigits_PossibleScientific_ToString(
                value.ToString(System.Globalization.CultureInfo.InvariantCulture),      // I think this forces decimal to always be a '.' ?
                value.ToString(),
                significantDigits);
        }
        private static string ToStringSignificantDigits_PossibleScientific(decimal value, int significantDigits)
        {
            return ToStringSignificantDigits_PossibleScientific_ToString(
                value.ToString(System.Globalization.CultureInfo.InvariantCulture),      // I think this forces decimal to always be a '.' ?
                value.ToString(),
                significantDigits);
        }
        private static string ToStringSignificantDigits_PossibleScientific_ToString(string textInvariant, string text, int significantDigits)
        {
            Match match = Regex.Match(textInvariant, @"^(?<num>\d\.\d+)(?<exp>E(-|)\d+)$");
            if (!match.Success)
            {
                // Unknown
                return text;
            }

            string standard = ToStringSignificantDigits_Standard(Convert.ToDouble(match.Groups["num"].Value), significantDigits, false);

            return standard + match.Groups["exp"].Value;
        }

        private static string ToStringSignificantDigits_Standard(double value, int significantDigits, bool useN)
        {
            return ToStringSignificantDigits_Standard(Convert.ToDecimal(value), significantDigits, useN);
        }
        private static string ToStringSignificantDigits_Standard(decimal value, int significantDigits, bool useN)
        {
            // Get the integer portion
            //long intPortion = Convert.ToInt64(Math.Truncate(value));		// going directly against the value for this (min could go from 1 to 1000.  1 needs two decimal places, 10 needs one, 100+ needs zero)
            BigInteger intPortion = new BigInteger(Math.Truncate(value));       // ran into a case that didn't fit in a long
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
            decimal rounded = Math.Round(value, numPlaces);
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

    #region class: SelectManysArgs2

    public class SelectManysArgs2<T1, T2>
    {
        public Func<T1, IEnumerable<T2>> Select_1_2 { get; set; }

        // Where clauses are optional
        public Func<T1, bool> Where_1 { get; set; }
        public Func<T2, bool> Where_2 { get; set; }
    }

    #endregion
    #region class: SelectManysArgs3

    public class SelectManysArgs3<T1, T2, T3>
    {
        public Func<T1, IEnumerable<T2>> Select_1_2 { get; set; }
        public Func<T2, IEnumerable<T3>> Select_2_3 { get; set; }

        // Where clauses are optional
        public Func<T1, bool> Where_1 { get; set; }
        public Func<T2, bool> Where_2 { get; set; }
        public Func<T3, bool> Where_3 { get; set; }
    }

    #endregion
    #region class: SelectManysArgs4

    public class SelectManysArgs4<T1, T2, T3, T4>
    {
        public Func<T1, IEnumerable<T2>> Select_1_2 { get; set; }
        public Func<T2, IEnumerable<T3>> Select_2_3 { get; set; }
        public Func<T3, IEnumerable<T4>> Select_3_4 { get; set; }

        // Where clauses are optional
        public Func<T1, bool> Where_1 { get; set; }
        public Func<T2, bool> Where_2 { get; set; }
        public Func<T3, bool> Where_3 { get; set; }
        public Func<T4, bool> Where_4 { get; set; }
    }

    #endregion
    #region class: SelectManysArgs5

    public class SelectManysArgs5<T1, T2, T3, T4, T5>
    {
        public Func<T1, IEnumerable<T2>> Select_1_2 { get; set; }
        public Func<T2, IEnumerable<T3>> Select_2_3 { get; set; }
        public Func<T3, IEnumerable<T4>> Select_3_4 { get; set; }
        public Func<T4, IEnumerable<T5>> Select_4_5 { get; set; }

        // Where clauses are optional
        public Func<T1, bool> Where_1 { get; set; }
        public Func<T2, bool> Where_2 { get; set; }
        public Func<T3, bool> Where_3 { get; set; }
        public Func<T4, bool> Where_4 { get; set; }
        public Func<T5, bool> Where_5 { get; set; }
    }

    #endregion
    #region class: SelectManysArgs6

    public class SelectManysArgs6<T1, T2, T3, T4, T5, T6>
    {
        public Func<T1, IEnumerable<T2>> Select_1_2 { get; set; }
        public Func<T2, IEnumerable<T3>> Select_2_3 { get; set; }
        public Func<T3, IEnumerable<T4>> Select_3_4 { get; set; }
        public Func<T4, IEnumerable<T5>> Select_4_5 { get; set; }
        public Func<T5, IEnumerable<T6>> Select_5_6 { get; set; }

        // Where clauses are optional
        public Func<T1, bool> Where_1 { get; set; }
        public Func<T2, bool> Where_2 { get; set; }
        public Func<T3, bool> Where_3 { get; set; }
        public Func<T4, bool> Where_4 { get; set; }
        public Func<T5, bool> Where_5 { get; set; }
        public Func<T6, bool> Where_6 { get; set; }
    }

    #endregion
}
