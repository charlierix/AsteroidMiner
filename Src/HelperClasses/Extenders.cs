using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.HelperClasses
{
    /// <summary>
    /// These are extension methods of various low level types (see Game.Newt.HelperClasses.Extenders for wpf types)
    /// </summary>
    public static class Extenders
    {
        #region LINQ

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
            double actualPercent = 1d +  (useRandomPercent ? percent * rand.NextDouble() : percent);

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

        #endregion
    }
}
