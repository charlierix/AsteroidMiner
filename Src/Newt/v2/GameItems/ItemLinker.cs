using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;

namespace Game.Newt.v2.GameItems
{
    //TODO: There are still a few methods that use the terms brain and io.  Rename Brain to Item1, IO to Item2
    /// <summary>
    /// This has methods that tell how to link items together based on positions, sizes
    /// </summary>
    public static class ItemLinker
    {
        #region Class: DistributeDistances

        private class DistributeDistances
        {
            public DistributeDistances(Tuple<int, double>[][] resistancesItem1, Distances2to1[] distances2to1)
            {
                this.ResistancesItem1 = resistancesItem1;
                this.Distances2to1 = distances2to1;
            }

            /// <summary>
            /// This holds links between all Item1s and the resistance (distance * mult)
            /// </summary>
            /// <remarks>
            /// Index into array is index of item1
            /// Tuple.Item1: Index of other item1
            /// Tuple.Item2: Resistance felt between the two
            /// </remarks>
            public readonly Tuple<int, double>[][] ResistancesItem1;

            //NOTE: These are ordered by distance
            public readonly Distances2to1[] Distances2to1;
        }

        #endregion
        #region Class: Distances2to1

        /// <summary>
        /// This holds an item2, and distances to all item1s
        /// </summary>
        private class Distances2to1
        {
            public Distances2to1(int index2, Tuple<int, double>[] distancesTo1)
            {
                this.Index2 = index2;
                this.DistancesTo1 = distancesTo1;
            }

            public readonly int Index2;

            //TODO: No need to store the entire array.  Just the closest one
            /// <summary>
            /// Item1: index into items1
            /// Item2: distance to item1
            /// NOTE: These are sorted lowest to highest distance
            /// </summary>
            public readonly Tuple<int, double>[] DistancesTo1;
        }

        #endregion
        #region Class: BrainBurden

        private class BrainBurden
        {
            #region Constructor

            public BrainBurden(int index, LinkItem[] brains, LinkItem[] io)
            {
                this.Index = index;
                this.Brain = brains[index];
                this.Size = this.Brain.Size;

                _io = io;
            }

            #endregion

            public readonly int Index;
            public readonly LinkItem Brain;
            public readonly double Size;

            private readonly LinkItem[] _io;

            private readonly List<int> _ioLinks = new List<int>();
            public IEnumerable<int> IOLinks { get { return _ioLinks; } }

            public double IOSize { get; private set; }

            public void AddIOLink(int index)
            {
                _ioLinks.Add(index);
                this.IOSize += _io[index].Size;
            }

            public static double CalculateBurden(double sumLinkSize, double brainSize)
            {
                return sumLinkSize / brainSize;
            }
        }

        #endregion

        #region Class: ClosestFuzzyResult

        private class ClosestFuzzyResult
        {
            public ClosestFuzzyResult(int index, double percent)
            {
                this.Index = index;
                this.Percent = percent;
            }

            public readonly int Index;
            public readonly double Percent;
        }

        #endregion

        /// <summary>
        /// Link brains to each other (delaunay graph, then prune thin triangles)
        /// </summary>
        /// <param name="all">
        /// Item1=Link between two items (sub item1 and 2 are the indices)
        /// Item2=Distance between those two items
        /// </param>
        /// <param name="final">
        /// This holds a set of links after the thin triangles are pruned.  There's a chance of items being merged
        /// </param>
        public static void Link_Self(out SortedList<Tuple<int, int>, double> all, out LinkSetPair[] final, LinkItem[] items, ItemLinker_CombineArgs combineArgs = null)
        {
            TriangleIndexed[] triangles;
            GetItemDelaunay(out all, out triangles, items);

            if (items.Length < 2)
            {
                //throw new ArgumentException("This method requires at least two brains: " + items.Length.ToString());
                final = new LinkSetPair[0];
                return;
            }

            // Prune links that don't make sense
            if (combineArgs != null && triangles.Length > 0)
            {
                final = PruneLinks(triangles, all, items, combineArgs);
            }
            else
            {
                final = all.Keys.
                    Select(o => new LinkSetPair(o.Item1, o.Item2, items)).
                    ToArray();
            }
        }

        /// <summary>
        /// Every item in 2 will have at least one link to a 1.  There could be some item1s that don't have a link
        /// </summary>
        /// <param name="overflowArgs">
        /// Item2s get matched to the nearest Item1.
        /// If this args is null then, it stays that way.
        /// If this is populated, then links will move from burdened item1s to less burdened
        /// </param>
        /// <returns>
        /// Item1=index into item1 list
        /// Item2=index into item2 list
        /// </returns>
        public static Tuple<int, int>[] Link_1_2(LinkItem[] items1, LinkItem[] items2, ItemLinker_OverflowArgs overflowArgs = null, ItemLinker_ExtraArgs extraArgs = null)
        {
            if (items1 == null || items2 == null || items1.Length == 0 || items2.Length == 0)
            {
                return new Tuple<int, int>[0];
            }

            Tuple<int, int>[] retVal = null;

            if (overflowArgs == null)
            {
                // Just link to the closest
                retVal = Link12_Closest(items1, items2);
            }

            if (overflowArgs == null && extraArgs == null)
            {
                // Nothing special to do, exit early
                return retVal;
            }

            DistributeDistances distances = GetDistances(items1, items2, overflowArgs);

            if (overflowArgs != null)
            {
                // Consider item1s burden when linking them
                retVal = Link12_Distribute(items1, items2, distances);
            }

            if (extraArgs != null)
            {
                // Add more links
                retVal = Link12_Extra(retVal, items1, items2, distances, extraArgs);
            }

            return retVal;
        }

        /// <summary>
        /// This takes links that were built against old positions, and links to the new positions
        /// </summary>
        /// <remarks>
        /// If a new link has no clean position to line up with, then it will split into multiple links (weight divided in proportion
        /// to distance from existing point) -- uses maxIntermediateLinks to figure out up to how many divisions to make
        /// 
        /// Once that bush of intermediate links is found, a final prune will pick the strongest, and move the deleted weights
        /// to the survivors
        /// </remarks>
        /// <param name="existing">
        /// These are the locations of existing links.
        /// NOTE: These points aren't required to match the points in all.  That's the whole point of this method, to find the closest matches
        /// </param>
        /// <param name="all">These are what the new links need to reference</param>
        /// <param name="maxFinalLinks">
        /// This is the return size.  Should probably be >= existing.Length (or too much will get pruned)
        /// </param>
        /// <param name="maxIntermediateLinks">
        /// This is per link.  So if it's 3, then up to 3 candidate links will be found for every existing link
        /// </param>
        /// <returns></returns>
        public static Tuple<int, int, double>[] FuzzyLink(Tuple<Point3D, Point3D, double>[] existing, Point3D[] all, int maxFinalLinks = 3, int maxIntermediateLinks = 3)
        {
            var existingPruned = existing;
            if (existing.Length > maxFinalLinks)
            {
                // Prune without distributing weight (if this isn't done here, then the prune at the bottom of this method will
                // artificially inflate weights with the links that this step is removing)
                existingPruned = Prune_Pre(existing, maxFinalLinks);
            }

            #region Find closest points

            // Get a unique list of points
            var resultsByPoint = new Dictionary<Point3D, ClosestFuzzyResult[]>();		// can't use SortedList, because point isn't sortable (probably doesn't have IComparable)
            foreach (var exist in existingPruned)
            {
                if (!resultsByPoint.ContainsKey(exist.Item1))
                {
                    resultsByPoint.Add(exist.Item1, GetClosestFuzzy(exist.Item1, all, maxIntermediateLinks));
                }

                if (!resultsByPoint.ContainsKey(exist.Item2))
                {
                    resultsByPoint.Add(exist.Item2, GetClosestFuzzy(exist.Item2, all, maxIntermediateLinks));
                }
            }

            #endregion

            var retVal = new List<Tuple<int, int, double>>();

            #region Build links

            foreach (var exist in existingPruned)
            {
                retVal.AddRange(
                    GetHighestPercent(resultsByPoint[exist.Item1], resultsByPoint[exist.Item2], maxFinalLinks, true).
                        Select(o => Tuple.Create(o.Item1, o.Item2, exist.Item3 * o.Item3))
                    );
            }

            #endregion

            // Exit Function
            if (retVal.Count > maxFinalLinks)
            {
                #region Prune

                // Prune the weakest links
                // Need to redistribute the lost weight (since this method divided links into smaller ones).  If I don't, then over many generations,
                // the links will tend toward zero

                retVal = retVal.
                    OrderByDescending(o => Math.Abs(o.Item3)).
                    ToList();

                var kept = retVal.Take(maxFinalLinks).ToArray();
                var removed = retVal.Skip(maxFinalLinks).ToArray();

                double keptSum = kept.Sum(o => Math.Abs(o.Item3));
                double removedSum = removed.Sum(o => Math.Abs(o.Item3));

                double ratio = keptSum / (keptSum + removedSum);
                ratio = 1d / ratio;

                return kept.
                    Select(o => Tuple.Create(o.Item1, o.Item2, o.Item3 * ratio)).
                    ToArray();

                #endregion
            }
            else
            {
                return retVal.ToArray();
            }
        }

        #region Private Methods - self

        private static LinkSetPair[] PruneLinks(TriangleIndexed[] triangles, SortedList<Tuple<int, int>, double> all, LinkItem[] brains, ItemLinker_CombineArgs args)
        {
            List<Tuple<int[], int[]>> retVal = all.Keys.
                Select(o => Tuple.Create(new[] { o.Item1 }, new[] { o.Item2 })).
                ToList();

            foreach (TriangleIndexed triangle in triangles)
            {
                Tuple<bool, TriangleEdge> changeEdge = PruneLinks_LongThin(triangle, all, args);
                if (changeEdge == null)
                {
                    continue;
                }

                if (changeEdge.Item1)
                {
                    PruneLinks_Merge(triangle, changeEdge.Item2, retVal);
                }
                else
                {
                    PruneLinks_Remove(triangle, changeEdge.Item2, retVal);
                }
            }

            return retVal.Select(o => new LinkSetPair(o.Item1, o.Item2, brains)).ToArray();
        }

        /// <summary>
        /// If this triangle is long and thin, then this will decide whether to remove a link, or merge the two close brains
        /// </summary>
        /// <returns>
        /// Null : This is not a long thin triangle.  Move along
        /// Item1=True : Merge the two brains connected by Item2
        /// Item1=False : Remove the Item2 link
        /// </returns>
        private static Tuple<bool, TriangleEdge> PruneLinks_LongThin(ITriangleIndexed triangle, SortedList<Tuple<int, int>, double> all, ItemLinker_CombineArgs args)
        {
            var lengths = new[] { TriangleEdge.Edge_01, TriangleEdge.Edge_12, TriangleEdge.Edge_20 }.
                Select(o => new { Edge = o, Length = GetLength(triangle, o, all) }).
                OrderBy(o => o.Length).
                ToArray();

            //NOTE: The order of these if statements is important.  I ran into a case where it's both wide and
            //skinny (nearly colinear).  In that case, the long segment should be removed
            if (lengths[2].Length / (lengths[0].Length + lengths[1].Length) > args.Ratio_Wide)
            {
                // Wide base (small angles, one huge angle)
                return Tuple.Create(false, lengths[2].Edge);
            }
            else if (lengths[0].Length / lengths[1].Length < args.Ratio_Skinny && lengths[0].Length / lengths[2].Length < args.Ratio_Skinny)
            {
                #region Isosceles - skinny base

                if (StaticRandom.NextDouble() < args.MergeChance)
                {
                    // Treat the two close brains like one, and split the links evenly with the far brain
                    return Tuple.Create(true, lengths[0].Edge);
                }
                else
                {
                    // Choose one of the long links to remove
                    if (StaticRandom.NextBool())
                    {
                        return Tuple.Create(false, lengths[1].Edge);
                    }
                    else
                    {
                        return Tuple.Create(false, lengths[2].Edge);
                    }
                }

                #endregion
            }

            return null;
        }

        private static void PruneLinks_Merge(TriangleIndexed triangle, TriangleEdge edge, List<Tuple<int[], int[]>> links)
        {
            // Figure out which indexes to look for
            int[] pair = new[] { triangle.GetIndex(edge, true), triangle.GetIndex(edge, false) };
            int other = triangle.IndexArray.First(o => !pair.Contains(o));

            // Extract the affected links out of the list
            List<Tuple<int[], int[]>> affected = new List<Tuple<int[], int[]>>();

            int index = 0;
            while (index < links.Count)
            {
                var current = links[index];

                if (current.Item1.Contains(other) && current.Item2.Any(o => pair.Contains(o)))
                {
                    affected.Add(current);
                    links.RemoveAt(index);
                }
                else if (current.Item2.Contains(other) && current.Item1.Any(o => pair.Contains(o)))
                {
                    affected.Add(Tuple.Create(current.Item2, current.Item1));       // reversing them so that Item1 is always other
                    links.RemoveAt(index);
                }
                else
                {
                    index++;
                }
            }

            // Combine the affected links (there shouldn't be more than two)
            var merged = Tuple.Create(
                affected.SelectMany(o => o.Item1).Distinct().ToArray(),
                affected.SelectMany(o => o.Item2).Distinct().ToArray());

            links.Add(merged);
        }

        private static void PruneLinks_Remove(TriangleIndexed triangle, TriangleEdge edge, List<Tuple<int[], int[]>> links)
        {
            int index1 = triangle.GetIndex(edge, true);
            int index2 = triangle.GetIndex(edge, false);

            Tuple<int[], int[]> existing = null;
            bool? is1in1 = null;

            // Find and remove the link that contains this edge
            for (int cntr = 0; cntr < links.Count; cntr++)
            {
                if (links[cntr].Item1.Contains(index1) && links[cntr].Item2.Contains(index2))
                {
                    is1in1 = true;
                }
                else if (links[cntr].Item1.Contains(index2) && links[cntr].Item2.Contains(index1))
                {
                    is1in1 = false;
                }
                else
                {
                    continue;
                }

                existing = links[cntr];
                links.RemoveAt(cntr);
                break;
            }

            if (existing == null)
            {
                //throw new ArgumentException("Didn't find the link");

                // A neighbor triangle probably removed it
                return;
            }

            // Add back if there were more than 2 involved
            if (existing.Item1.Length == 1 && existing.Item2.Length == 1)
            {
                // This link only holds one item on each side.  It's already removed from the list, so there is nothing left to do
                return;
            }

            int[] newItem1 = PruneLinks_Remove_Reduce(existing.Item1, index1, index2, is1in1.Value);
            int[] newItem2 = PruneLinks_Remove_Reduce(existing.Item2, index2, index1, is1in1.Value);

            links.Add(Tuple.Create(newItem1, newItem2));
        }
        private static int[] PruneLinks_Remove_Reduce(int[] existing, int index1, int index2, bool use1)
        {
            if (existing.Length == 1)
            {
                return existing;
            }

            int removeIndex = use1 ? index1 : index2;

            // Keep all but the one to remove
            return existing.Where(o => o != removeIndex).ToArray();
        }

        private static double GetLength(ITriangleIndexed triangle, TriangleEdge edge, SortedList<Tuple<int, int>, double> lengths)
        {
            return GetLength(triangle.GetIndex(edge, true), triangle.GetIndex(edge, false), lengths);
        }
        private static double GetLength(Tuple<int, int> pair, SortedList<Tuple<int, int>, double> lengths)
        {
            if (pair.Item1 < pair.Item2)
            {
                return lengths[pair];
            }
            else
            {
                return lengths[Tuple.Create(pair.Item2, pair.Item1)];
            }
        }
        private static double GetLength(int index1, int index2, SortedList<Tuple<int, int>, double> lengths)
        {
            if (index1 < index2)
            {
                return lengths[Tuple.Create(index1, index2)];
            }
            else
            {
                return lengths[Tuple.Create(index2, index1)];
            }
        }

        #endregion
        #region Private Methods - 1->2

        /// <summary>
        /// This links closest items together
        /// </summary>
        private static Tuple<int, int>[] Link12_Closest(LinkItem[] items1, LinkItem[] items2)
        {
            var retVal = new Tuple<int, int>[items2.Length];

            for (int cntr = 0; cntr < items2.Length; cntr++)
            {
                int closest = items1.
                    Select((o, i) => new { Index = i, Position = o.Position, DistSqr = (o.Position - items2[cntr].Position).LengthSquared }).
                    OrderBy(o => o.DistSqr).
                    First().
                    Index;

                retVal[cntr] = Tuple.Create(closest, cntr);
            }

            return retVal;
        }

        private static Tuple<int, int>[] Link12_Distribute(LinkItem[] items1, LinkItem[] items2, DistributeDistances distances)
        {
            IEnumerable<int> addOrder = Enumerable.Range(0, distances.Distances2to1.Length);

            return AddLinks(items1, items2, distances, addOrder);
        }

        private static Tuple<int, int>[] Link12_Extra(Tuple<int, int>[] initial, LinkItem[] items1, LinkItem[] items2, DistributeDistances distances, ItemLinker_ExtraArgs extraArgs)
        {
            Random rand = StaticRandom.GetRandomForThread();

            int wholePercent = extraArgs.Percent.ToInt_Floor();

            List<int> addOrder = new List<int>();

            if (extraArgs.BySize)
            {
                double totalSize = items2.Sum(o => o.Size);
                double maxSize = extraArgs.Percent * totalSize;
                double usedSize = 0;

                if (extraArgs.EvenlyDistribute)
                {
                    #region by size, evenly distribute

                    // Add some complete passes if over 100% percent
                    for (int cntr = 0; cntr < wholePercent; cntr++)
                    {
                        addOrder.AddRange(UtilityCore.RandomRange(0, items2.Length));
                    }

                    usedSize = wholePercent * totalSize;

                    #endregion
                }

                #region by size, distribute the rest

                //NOTE: Building this list by size so that larger items have a higher chance of being chosen
                var bySize = items2.
                    Select((o, i) => Tuple.Create(i, o.Size / totalSize)).
                    OrderByDescending(o => o.Item2).
                    ToArray();

                // Keep selecting items unti the extra size is consumed (or if no more can be added)
                while (true)
                {
                    bool foundOne = false;

                    for (int cntr = 0; cntr < 1000; cntr++)     // this is an infinite loop detector
                    {
                        int attemptIndex = UtilityCore.GetIndexIntoList(rand.NextDouble(), bySize);     // get the index into the list that the rand percent represents
                        attemptIndex = bySize[attemptIndex].Item1;      // get the index into items2

                        if (items2[attemptIndex].Size + usedSize <= maxSize)
                        {
                            foundOne = true;
                            usedSize += items2[attemptIndex].Size;
                            addOrder.Add(attemptIndex);
                            break;
                        }
                    }

                    if (!foundOne)
                    {
                        // No more will fit
                        break;
                    }
                }

                #endregion
            }
            else
            {
                if (extraArgs.EvenlyDistribute)
                {
                    #region ignore size, evenly distribute

                    // Add some complete passes if over 100% percent
                    for (int cntr = 0; cntr < wholePercent; cntr++)
                    {
                        addOrder.AddRange(UtilityCore.RandomRange(0, items2.Length));
                    }

                    // Add some items based on the portion of percent that is less than 100%
                    int remainder = (items2.Length * (extraArgs.Percent - wholePercent)).
                        ToInt_Round();

                    addOrder.AddRange(UtilityCore.RandomRange(0, items2.Length, remainder));

                    #endregion
                }
                else
                {
                    #region ignore size, randomly distribute

                    int totalCount = (items2.Length * extraArgs.Percent).
                        ToInt_Round();

                    //NOTE: UtilityCore.RandomRange stops when the list is exhausted, and makes sure not to have dupes.  That's not what is wanted
                    //here.  Just randomly pick X times
                    addOrder.AddRange(Enumerable.Range(0, totalCount).Select(o => rand.Next(items2.Length)));

                    #endregion
                }
            }

            return AddLinks(items1, items2, distances, addOrder, initial);
        }

        /// <summary>
        /// This adds 2s to 1s one at a time (specified by distances2to1_AddOrder)
        /// </summary>
        private static Tuple<int, int>[] AddLinks(LinkItem[] items1, LinkItem[] items2, DistributeDistances distances, IEnumerable<int> distances2to1_AddOrder, Tuple<int, int>[] initial = null)
        {
            // Store the inital link burdens
            BrainBurden[] links = Enumerable.Range(0, items1.Length).
                Select(o => new BrainBurden(o, items1, items2)).
                ToArray();

            if (initial != null)
            {
                #region Store initial

                foreach (var set in initial.ToLookup(o => o.Item1))
                {
                    foreach (int item2Index in set.Select(o => o.Item2))
                    {
                        links[set.Key].AddIOLink(item2Index);
                    }
                }

                #endregion
            }

            foreach (var distanceIO in distances2to1_AddOrder.Select(o => distances.Distances2to1[o]))
            {
                int ioIndex = distanceIO.Index2;
                int closestBrainIndex = distanceIO.DistancesTo1[0].Item1;

                AddIOLink(links, ioIndex, items2[ioIndex].Size, closestBrainIndex, distances.ResistancesItem1[closestBrainIndex]);
            }

            // Build the return
            List<Tuple<int, int>> retVal = new List<Tuple<int, int>>();
            foreach (BrainBurden burden in links)
            {
                retVal.AddRange(burden.IOLinks.Select(o => Tuple.Create(burden.Index, o)));
            }

            return retVal.ToArray();
        }

        private static DistributeDistances GetDistances(LinkItem[] items1, LinkItem[] items2, ItemLinker_OverflowArgs overflowArgs)
        {
            // Distances between all item1s (not just delaunay, but all pairs)
            Tuple<int, double>[][] resistancesItem1 = ItemItemResistance(items1, overflowArgs == null ? 1 : overflowArgs.LinkResistanceMult);

            // Figure out the distances between 2s and 1s
            var distances2to1 = Enumerable.Range(0, items2.Length).
                Select(o => new Distances2to1
                (
                    o,
                    Enumerable.Range(0, items1.Length).
                        Select(p => Tuple.Create(p, (items1[p].Position - items2[o].Position).Length)).        //Item1=items1 index, Item2=distance to item1
                        OrderBy(p => p.Item2).      // first item1 needs to be the shortest distance
                        ToArray()
                )).
                OrderBy(o => o.DistancesTo1.First().Item2).
                ToArray();

            return new DistributeDistances(resistancesItem1, distances2to1);
        }

        private static Tuple<int, double>[][] ItemItemResistance(LinkItem[] items, double linkResistanceMult)
        {
            // Get the AABB, and use the diagonal as the size
            var aabb = Math3D.GetAABB(items.Select(o => o.Position));
            double maxDistance = (aabb.Item2 - aabb.Item1).Length;

            // Get links between the items and distances of each link
            var links2 = UtilityCore.GetPairs(items.Length).
                Select(o =>
                {
                    double distance = (items[o.Item1].Position - items[o.Item2].Position).Length;
                    double resistance = (distance / maxDistance) * linkResistanceMult;

                    return Tuple.Create(o.Item2, o.Item1, resistance);
                }).
                ToArray();

            Tuple<int, double>[][] retVal = new Tuple<int, double>[items.Length][];

            for (int cntr = 0; cntr < items.Length; cntr++)
            {
                // Store all links for this item
                retVal[cntr] = links2.
                    Where(o => o.Item1 == cntr || o.Item2 == cntr).       // find links between this item and another
                    Select(o => Tuple.Create(o.Item1 == cntr ? o.Item2 : o.Item1, o.Item3)).     // store the link to the other item and the resistance
                    OrderBy(o => o.Item2).
                    ToArray();
            }

            return retVal;
        }

        /// <summary>
        /// This adds ioIndex to one of finalLinks
        /// </summary>
        /// <param name="closestBrainIndex">Index of the brain that is closest to the IO.  There is no extra burden for linking to this one</param>
        /// <param name="brainBrainBurdens">
        /// Item1=Index of other brain
        /// Item2=Link resistance (burden) between closestBrainIndex and this brain
        /// </param>
        private static void AddIOLink(BrainBurden[] finalLinks, int ioIndex, double ioSize, int closestBrainIndex, Tuple<int, double>[] brainBrainBurdens)
        {
            // Figure out the cost of adding the link to the various brains
            List<Tuple<int, double>> burdens = new List<Tuple<int, double>>();

            for (int cntr = 0; cntr < finalLinks.Length; cntr++)
            {
                if (finalLinks[cntr].IOLinks.Contains(ioIndex))
                {
                    // This only happens when extra links are requested
                    continue;
                }

                int brainIndex = finalLinks[cntr].Index;        // this is likely always the same as cntr, but since that object has brainIndex as a property, I feel safer using it

                // Adding to the closest brain has no exta cost.  Adding to any other brain has a cost based on the
                // distance between the closest brain and that other brain
                double linkCost = 0d;
                if (brainIndex != closestBrainIndex)
                {
                    var matchingBrain = brainBrainBurdens.FirstOrDefault(o => o.Item1 == brainIndex);
                    if (matchingBrain == null)
                    {
                        //NOTE: All brain-brain distances should be passed in, so this should never happen
                        continue;
                    }

                    linkCost = matchingBrain.Item2;
                }

                // LinkCost + IOStorageCost
                burdens.Add(Tuple.Create(cntr, linkCost + BrainBurden.CalculateBurden(finalLinks[cntr].IOSize + ioSize, finalLinks[cntr].Size)));
            }

            if (burdens.Count == 0)
            {
                // This io has already been added to all brains
                return;
            }

            int cheapestIndex = burdens.
                OrderBy(o => o.Item2).First().Item1;

            finalLinks[cheapestIndex].AddIOLink(ioIndex);
        }

        #endregion
        #region Private Methods - fuzzy link

        private static Tuple<Point3D, Point3D, double>[] Prune_Pre(Tuple<Point3D, Point3D, double>[] existing, int count)
        {
            // Sort it so the smaller links have a higher chance of being removed
            var retVal = existing.
                OrderByDescending(o => Math.Abs(o.Item3)).      // the weights could be negative, so sort on distance from zero
                ToList();

            Random rand = StaticRandom.GetRandomForThread();

            while (retVal.Count > count)
            {
                // Choose a random item to remove - favor the end of the list (where the smallest links are)
                double percent = rand.NextPow(10);
                int index = UtilityCore.GetIndexIntoList(percent, retVal.Count);

                retVal.RemoveAt(index);
            }

            return retVal.ToArray();
        }

        private static ClosestFuzzyResult[] GetClosestFuzzy(Point3D search, Point3D[] points, int maxReturn)
        {
            const double SEARCHRADIUSMULT = 2.5d;		// looks at other nodes that are up to minradius * mult

            // Check for exact match
            int index = FindExact(search, points);
            if (index >= 0)
            {
                return new ClosestFuzzyResult[] { new ClosestFuzzyResult(index, 1d) };
            }

            // Get a list of nodes that are close to the search point
            var nearNodes = GetNearNodes(search, points, SEARCHRADIUSMULT);

            if (nearNodes.Count == 1)
            {
                // There's only one, so give it the full weight
                return new ClosestFuzzyResult[] { new ClosestFuzzyResult(nearNodes[0].Item1, 1d) };
            }

            // Don't allow too many divisions
            if (nearNodes.Count > maxReturn)
            {
                nearNodes = nearNodes.
                    OrderBy(o => o.Item2).
                    Take(maxReturn).
                    ToList();
            }

            // Figure out what percent of the weight to give these nodes (based on the ratio of their distances to the search point)
            var percents = GetPercentOfWeight(nearNodes, SEARCHRADIUSMULT);

            return percents.
                Select(o => new ClosestFuzzyResult(o.Item1, o.Item2)).
                ToArray();
        }

        private static int FindExact(Point3D search, Point3D[] points)
        {
            for (int cntr = 0; cntr < points.Length; cntr++)
            {
                if (Math3D.IsNearValue(points[cntr], search))
                {
                    return cntr;
                }
            }

            return -1;
        }

        /// <summary>
        /// This returns the index and distance of the nodes that are close to search
        /// </summary>
        private static List<Tuple<int, double>> GetNearNodes(Point3D search, Point3D[] points, double searchRadiusMultiplier)
        {
            // Get the distances to each point
            double[] distSquared = points.
                Select(o => (o - search).LengthSquared).
                ToArray();

            // Find the smallest distance
            int smallestIndex = 0;
            for (int cntr = 1; cntr < distSquared.Length; cntr++)
            {
                if (distSquared[cntr] < distSquared[smallestIndex])
                {
                    smallestIndex = cntr;
                }
            }

            // Figure out how far out to allow
            double min = Math.Sqrt(distSquared[smallestIndex]);
            double maxSquared = Math.Pow(min * searchRadiusMultiplier, 2d);

            // Find all the points in range
            List<Tuple<int, double>> retVal = new List<Tuple<int, double>>();

            // This one is obviously in range (adding it now to avoid an unnessary sqrt)
            retVal.Add(new Tuple<int, double>(smallestIndex, min));

            for (int cntr = 0; cntr < distSquared.Length; cntr++)
            {
                if (cntr == smallestIndex)
                {
                    continue;
                }

                if (distSquared[cntr] < maxSquared)
                {
                    retVal.Add(new Tuple<int, double>(cntr, Math.Sqrt(distSquared[cntr])));
                }
            }

            return retVal;
        }

        /// <summary>
        /// This takes in a list of distances, and returns a list of percents (the int just comes along for the ride)
        /// </summary>
        private static List<Tuple<int, double>> GetPercentOfWeight(List<Tuple<int, double>> distances, double searchRadiusMultiplier)
        {
            const double OFFSET = .1d;

            // Find the smallest distance in the list
            double min = distances.Min(o => o.Item2);

            // Figure out what the maximum possible distance would be
            double maxRange = (min * searchRadiusMultiplier) - min;

            // Figure out ratios base on distance
            double[] ratios = new double[distances.Count];
            for (int cntr = 0; cntr < ratios.Length; cntr++)
            {
                // Normalize the distance
                ratios[cntr] = UtilityCore.GetScaledValue_Capped(0d, 1d, 0d, maxRange, distances[cntr].Item2 - min);

                // Run it through a function
                ratios[cntr] = 1d / (ratios[cntr] + OFFSET);		// need to add an offset, because one of these will be zero
            }

            double total = ratios.Sum();

            // Turn those ratios into percents (normalizing the ratios)
            List<Tuple<int, double>> retVal = new List<Tuple<int, double>>();
            for (int cntr = 0; cntr < ratios.Length; cntr++)
            {
                retVal.Add(new Tuple<int, double>(distances[cntr].Item1, ratios[cntr] / total));
            }

            return retVal;
        }

        /// <summary>
        /// This returns the top X strongest links
        /// </summary>
        private static Tuple<int, int, double>[] GetHighestPercent(ClosestFuzzyResult[] from, ClosestFuzzyResult[] to, int maxReturn, bool isFromSameList)
        {
            // Find the combinations that have the highest percentage
            List<Tuple<int, int, double>> products = new List<Tuple<int, int, double>>();
            for (int fromCntr = 0; fromCntr < from.Length; fromCntr++)
            {
                for (int toCntr = 0; toCntr < to.Length; toCntr++)
                {
                    if (isFromSameList && from[fromCntr].Index == to[toCntr].Index)
                    {
                        continue;
                    }

                    products.Add(new Tuple<int, int, double>(from[fromCntr].Index, to[toCntr].Index, from[fromCntr].Percent * to[toCntr].Percent));
                }
            }

            // Don't return too many
            IEnumerable<Tuple<int, int, double>> topProducts = null;
            if (products.Count <= maxReturn)
            {
                topProducts = products;		// no need to sort or limit
            }
            else
            {
                topProducts = products.
                    OrderByDescending(o => o.Item3).
                    Take(maxReturn).
                    ToArray();
            }

            // Normalize
            double totalPercent = topProducts.Sum(o => o.Item3);

            return topProducts.
                Select(o => Tuple.Create(o.Item1, o.Item2, o.Item3 / totalPercent)).
                ToArray();
        }

        #endregion
        #region Private Methods

        private static void GetItemDelaunay(out SortedList<Tuple<int, int>, double> segments, out TriangleIndexed[] triangles, LinkItem[] items)
        {
            Tuple<int, int>[] links = null;

            if (items.Length < 2)
            {
                links = new Tuple<int, int>[0];
                triangles = new TriangleIndexed[0];
            }
            else if (items.Length == 2)
            {
                links = new[] { Tuple.Create(0, 1) };
                triangles = new TriangleIndexed[0];
            }
            else if (items.Length == 3)
            {
                links = new[] 
                    {
                        Tuple.Create(0, 1),
                        Tuple.Create(0, 2),
                        Tuple.Create(1, 2),
                    };

                triangles = new[] { new TriangleIndexed(0, 1, 2, items.Select(o => o.Position).ToArray()) };
            }
            else
            {
                Tetrahedron[] tetras = Math3D.GetDelaunay(items.Select(o => o.Position).ToArray());
                links = Tetrahedron.GetUniqueLines(tetras);
                triangles = Tetrahedron.GetUniqueTriangles(tetras);
            }

            segments = GetLengths(links, items);
        }

        //NOTE: This makes sure that key.item1 is less than key.item2
        private static SortedList<Tuple<int, int>, double> GetLengths(Tuple<int, int>[] keys, LinkItem[] items)
        {
            SortedList<Tuple<int, int>, double> retVal = new SortedList<Tuple<int, int>, double>();

            foreach (Tuple<int, int> key in keys.Select(o => o.Item1 < o.Item2 ? o : Tuple.Create(o.Item2, o.Item1)))
            {
                double distance = (items[key.Item1].Position - items[key.Item2].Position).Length;

                retVal.Add(key, distance);
            }

            return retVal;
        }

        #endregion
    }

    #region Class: LinkItem

    public class LinkItem
    {
        public LinkItem(Point3D position, double size)
        {
            this.Position = position;
            this.Size = size;
        }

        public readonly Point3D Position;
        public readonly double Size;
    }

    #endregion
    #region Class: LinkSet

    public class LinkSet
    {
        #region Constructor

        public LinkSet(int index, LinkItem[] all)
            : this(new[] { index }, all) { }

        public LinkSet(IEnumerable<int> indices, LinkItem[] all)
        {
            this.Indices = indices.ToArray();
            this.Items = this.Indices.Select(o => all[o]).ToArray();

            this.Positions = this.Items.Select(o => o.Position).ToArray();

            if (this.Items.Length == 1)
            {
                this.Center = this.Items[0].Position;
            }
            else
            {
                this.Center = Math3D.GetCenter(this.Positions);
            }
        }

        #endregion

        public readonly int[] Indices;
        public readonly LinkItem[] Items;

        public readonly Point3D Center;
        public readonly Point3D[] Positions;
    }

    #endregion
    #region Class: LinkSetPair

    public class LinkSetPair
    {
        #region Constructor

        public LinkSetPair(int item1, int item2, LinkItem[] all)
            : this(new LinkSet(item1, all), new LinkSet(item2, all)) { }

        public LinkSetPair(IEnumerable<int> set1, IEnumerable<int> set2, LinkItem[] all)
            : this(new LinkSet(set1, all), new LinkSet(set2, all)) { }

        public LinkSetPair(LinkSet set1, LinkSet set2)
        {
            this.Set1 = set1;
            this.Set2 = set2;
        }

        #endregion

        public readonly LinkSet Set1;
        public readonly LinkSet Set2;
    }

    #endregion

    #region Class: ItemLinker_CombineArgs

    /// <summary>
    /// This is used when doing a self linkage.  It gives thresholds for skinny triangles, and what to do with them (removing useless links)
    /// </summary>
    public class ItemLinker_CombineArgs
    {
        public double Ratio_Skinny = .3;
        public double Ratio_Wide = .92;

        public double MergeChance = .5;
    }

    #endregion
    #region Class: ItemLinker_OverflowArgs

    /// <summary>
    /// This tells how to handle items that have too many links (move the link to a less burdened item)
    /// </summary>
    public class ItemLinker_OverflowArgs
    {
        /// <summary>
        /// This is multiplied by the distance between the items being linked.  The larger the value, the more the distance
        /// acts as a resistance.  If you use zero, the links will be distributed by burden, ignoring distance.
        /// </summary>
        public double LinkResistanceMult = 1;
    }

    #endregion
    #region Class: ItemLinker_ExtraArgs

    /// <summary>
    /// Default behavior is for each item to get one link.  This describes ways to have more than that minimum amount of links
    /// </summary>
    public class ItemLinker_ExtraArgs
    {
        /// <summary>
        /// If .5, then an extra 50% links will be added
        /// </summary>
        public double Percent = .5;

        /// <summary>
        /// True: The number of extra links is calculated by size and count
        /// False: Item size is ignored.  The number of extra links is calculated by count only
        /// </summary>
        public bool BySize = true;

        /// <summary>
        /// This only has an effect when percent is over 1 ( > 100%)
        /// 
        /// True: The list of item2s is mapped fully for each amount over 100%
        ///     so if Percent=3.5, then the item2 list will be mapped 3 full times, then randomly map half
        ///     
        /// False: The number of additions is calculated, then a random item is selected that many times
        ///     so there's a good chance that some items will get mapped more than average, some may not get any extra links
        /// </summary>
        public bool EvenlyDistribute = true;
    }

    #endregion
}
