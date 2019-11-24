using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using System.Xaml;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.HelperClassesWPF.Controls3D;
using Game.Newt.v2.GameItems;

namespace Game.Newt.Testers
{
    public partial class BrainLinks : Window
    {
        #region enum: AddItemLocation

        private enum AddItemLocation
        {
            Anywhere,

            Left,
            Center,
            Right,

            Inner,
            Outer,
        }

        #endregion
        #region enum: ClearWhich

        private enum ClearWhich
        {
            All,
            Inputs,
            Outputs,
            Brains,
        }

        #endregion
        #region class: Item2D

        private class Item2D
        {
            public Item2D(UIElement visual)
                : this(visual, 0, new Point()) { }
            public Item2D(UIElement visual, double size, Point position, Point? position2 = null)
            {
                this.Visual = visual;
                this.Size = size;
                this.Position = position;
                this.Position2 = position2;
            }

            public readonly UIElement Visual;

            /// <summary>
            /// When used for an input/output/brain, this represents the number of neurons (not the physical size of the graphic)
            /// </summary>
            public readonly double Size;

            public readonly Point Position;
            public readonly Point? Position2;
        }

        #endregion
        #region class: Item3D

        private class Item3D
        {
            #region Constructor

            public Item3D(Visual3D visual)
                : this(visual, 0, new Point3D()) { }

            public Item3D(Visual3D visual, double size, Point3D position)
                : this(visual, size, new[] { position }) { }

            public Item3D(Visual3D visual, double size, Point3D[] positions)
            {
                this.Visual = visual;
                this.Size = size;
                this.Positions = positions;

                if (positions.Length == 1)
                {
                    this.Position = positions[0];
                }
                else
                {
                    this.Position = null;
                }
            }

            #endregion

            public readonly Visual3D Visual;

            /// <summary>
            /// When used for an input/output/brain, this represents the number of neurons (not the physical size of the graphic)
            /// </summary>
            public readonly double Size;

            public readonly Point3D[] Positions;
            public readonly Point3D? Position;
        }

        #endregion

        #region class: Set2D

        private class Set2D
        {
            #region Constructor

            public Set2D(int index, Item2D[] all)
                : this(new[] { index }, all) { }

            public Set2D(IEnumerable<int> indices, Item2D[] all)
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
                    this.Center = Math2D.GetCenter(this.Positions);
                }
            }

            #endregion

            public readonly int[] Indices;
            public readonly Item2D[] Items;

            public readonly Point Center;
            public readonly Point[] Positions;
        }

        #endregion
        #region class: LinkBrain2D

        private class LinkBrain2D
        {
            #region Constructor

            public LinkBrain2D(int item1, int item2, Item2D[] all)
                : this(new Set2D(item1, all), new Set2D(item2, all)) { }

            public LinkBrain2D(IEnumerable<int> set1, IEnumerable<int> set2, Item2D[] all)
                : this(new Set2D(set1, all), new Set2D(set2, all)) { }

            public LinkBrain2D(Set2D brains1, Set2D brains2)
            {
                this.Brains1 = brains1;
                this.Brains2 = brains2;
            }

            #endregion

            public readonly Set2D Brains1;
            public readonly Set2D Brains2;

            //TODO: May want to store AllBrains[]
        }

        #endregion
        #region class: LinkIO

        private class LinkIO
        {
            #region Constructor

            public LinkIO(int brain, int io, Item2D[] allBrains, Item2D[] allIO)
                : this(new Set2D(brain, allBrains), io, allIO) { }

            public LinkIO(IEnumerable<int> brains, int io, Item2D[] allBrains, Item2D[] allIO)
                : this(new Set2D(brains, allBrains), io, allIO) { }

            public LinkIO(Set2D brains, int io, Item2D[] allIO)
            {
                this.Brains = brains;

                this.IOIndex = io;
                this.IO = allIO[io];
            }

            #endregion

            public readonly Set2D Brains;

            public readonly int IOIndex;
            public readonly Item2D IO;

            //TODO: May want to store AllBrains[]
            //TODO: May want to store AllIO[]
        }

        #endregion

        #region class: Worker2D_Simple

        private static class Worker2D_Simple
        {
            public static Tuple<int, int>[] LinkBrainsToIO_Voronoi(VoronoiResult2D voronoi, Point[] brains, Point[] io)
            {
                if (brains.Length == 0)
                {
                    return new Tuple<int, int>[0];
                }
                else if (brains.Length == 1)
                {
                    return Enumerable.Range(0, io.Length).Select(o => Tuple.Create(0, o)).ToArray();
                }

                List<Tuple<int, int>> retVal = new List<Tuple<int, int>>();
                List<int> remainingIO = Enumerable.Range(0, io.Length).ToList();        // store the remaining so that they can be removed as found (avoid unnecessary IsInside checks)

                for (int brainCntr = 0; brainCntr < brains.Length; brainCntr++)
                {
                    Edge2D[] edges = voronoi.EdgesByControlPoint[brainCntr].Select(o => voronoi.Edges[o]).ToArray();

                    if (edges.Length == 1)
                    {
                        throw new ApplicationException("finish this");
                    }

                    foreach (int ioIndex in remainingIO.ToArray())
                    {
                        if (Edge2D.IsInside(edges, io[ioIndex]))
                        {
                            retVal.Add(Tuple.Create(brainCntr, ioIndex));
                            remainingIO.Remove(ioIndex);
                        }
                    }
                }

                return retVal.ToArray();
            }
        }

        #endregion
        #region class: Worker2D_Voronoi

        /// <summary>
        /// This class:
        ///     Divides the brains into a voronoi
        ///     Links IO to brains according to the voronoi cell the io is in
        ///     Redistributes IO links to less burdened brains
        /// </summary>
        /// <remarks>
        /// The idea of redistributing is difficult to do right, is a LOT of complex code, and the results aren't that great
        /// 
        /// I think a better approach would be pure distance based
        /// </remarks>
        private static class Worker2D_Voronoi
        {
            #region class: BrainBurden

            private class BrainBurden
            {
                #region Constructor

                public BrainBurden(int index, IEnumerable<int> links)
                {
                    this.Index = index;
                    this.LinksOrig = links.ToList();
                    this.LinksMoved = new List<int>();
                }

                #endregion

                /// <summary>
                /// This is the index of the brain
                /// </summary>
                public readonly int Index;

                // These are the index into IO
                public readonly List<int> LinksOrig;        // links that were originally assigned to this brain
                public readonly List<int> LinksMoved;       // links that moved from another brain because of burden

                public double BrainSize;
                public double Burden;

                public void Recalc(Set2D[] brainSets, Item2D[] io)
                {
                    this.BrainSize = brainSets[this.Index].Items.Sum(o => o.Size);
                    this.Burden = CalculateBurden(UtilityCore.Iterate(this.LinksOrig, this.LinksMoved).Sum(o => io[o].Size), this.BrainSize);        // size of the links / size of the brain
                }

                public static double CalculateBurden(double sumLinkSize, double brainSize)
                {
                    return sumLinkSize / brainSize;
                }
            }

            #endregion

            //TODO: Put all the contstants in an options class

            #region Declaration Section

            private const double BRAINPRUNE_RATIO_SKINNY = .3;
            private const double BRAINPRUNE_RATIO_WIDE = .92;

            private const double BRAINPRUNE_MERGECHANCE = .5;

            private const bool IOPREMERGE_SHOULD = true;
            private const double IOPREMERGE_DISTANCE = .05;     // this is a % of the size of the aabb

            private const double IOPOSTMERGE_LINKRESISTANCEMULT = 10;

            #endregion

            public static void GetLinks(out LinkBrain2D[] brainLinks, out LinkIO[] ioLinks, Item2D[] brains, Item2D[] io)
            {
                if (brains == null || brains.Length == 0)
                {
                    brainLinks = null;
                    ioLinks = null;
                    return;
                }
                else if (brains.Length == 1)
                {
                    brainLinks = null;
                    ioLinks = Enumerable.Range(0, io.Length).Select(o => new LinkIO(0, o, brains, io)).ToArray();
                    return;
                }

                // Link brains:brains
                SortedList<Tuple<int, int>, double> allBrainLinks;
                BrainLinks(out allBrainLinks, out brainLinks, brains);

                // Link brains:io
                ioLinks = IOLinks(brains, io, allBrainLinks);
            }

            #region Private Methods

            /// <summary>
            /// Link brains to each other (delaunay graph, then prune thin triangles)
            /// </summary>
            internal static void BrainLinks(out SortedList<Tuple<int, int>, double> all, out LinkBrain2D[] pruned, Item2D[] brains)
            {
                if (brains.Length < 2)
                {
                    throw new ArgumentException("This method requires at least two brains: " + brains.Length.ToString());
                }
                else if (brains.Length == 2)
                {
                    all = GetLengths(new[] { Tuple.Create(0, 1) }, brains);
                    pruned = all.Keys.Select(o => new LinkBrain2D(o.Item1, o.Item2, brains)).ToArray();
                    return;
                }

                TriangleIndexed[] triangles = Math2D.GetDelaunayTriangulation(brains.Select(o => o.Position).ToArray(), brains.Select(o => o.Position.ToPoint3D()).ToArray());

                all = GetLengths(TriangleIndexed.GetUniqueLines(triangles), brains);

                // Prune links that don't make sense
                pruned = PruneBrainLinks(triangles, all, brains);
            }

            private static LinkBrain2D[] PruneBrainLinks(TriangleIndexed[] triangles, SortedList<Tuple<int, int>, double> all, Item2D[] brains)
            {
                List<Tuple<int[], int[]>> retVal = all.Keys.
                    Select(o => Tuple.Create(new[] { o.Item1 }, new[] { o.Item2 })).
                    ToList();

                foreach (TriangleIndexed triangle in triangles)
                {
                    Tuple<bool, TriangleEdge> changeEdge = PruneBrainLinks_LongThin(triangle, all);
                    if (changeEdge == null)
                    {
                        continue;
                    }

                    if (changeEdge.Item1)
                    {
                        PruneBrainLinks_Merge(triangle, changeEdge.Item2, retVal);
                    }
                    else
                    {
                        PruneBrainLinks_Remove(triangle, changeEdge.Item2, retVal);
                    }
                }

                return retVal.Select(o => new LinkBrain2D(o.Item1, o.Item2, brains)).ToArray();
            }
            /// <summary>
            /// If this triangle is long and thin, then this will decide whether to remove a link, or merge the two close brains
            /// </summary>
            /// <returns>
            /// Null : This is not a long thin triangle.  Move along
            /// Item1=True : Merge the two brains connected by Item2
            /// Item1=False : Remove the Item2 link
            /// </returns>
            private static Tuple<bool, TriangleEdge> PruneBrainLinks_LongThin(ITriangleIndexed triangle, SortedList<Tuple<int, int>, double> all)
            {
                var lengths = new[] { TriangleEdge.Edge_01, TriangleEdge.Edge_12, TriangleEdge.Edge_20 }.
                    Select(o => new { Edge = o, Length = GetLength(triangle, o, all) }).
                    OrderBy(o => o.Length).
                    ToArray();

                if (lengths[0].Length / lengths[1].Length < BRAINPRUNE_RATIO_SKINNY && lengths[0].Length / lengths[2].Length < BRAINPRUNE_RATIO_SKINNY)
                {
                    #region Isosceles - skinny base

                    if (StaticRandom.NextDouble() < BRAINPRUNE_MERGECHANCE)
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
                else if (lengths[2].Length / (lengths[0].Length + lengths[1].Length) > BRAINPRUNE_RATIO_WIDE)
                {
                    // Wide base (small angles, one huge angle)
                    return Tuple.Create(false, lengths[2].Edge);
                }

                return null;
            }
            private static void PruneBrainLinks_Merge(TriangleIndexed triangle, TriangleEdge edge, List<Tuple<int[], int[]>> links)
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

            private static void PruneBrainLinks_Remove(TriangleIndexed triangle, TriangleEdge edge, List<Tuple<int[], int[]>> links)
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

                int[] newItem1 = PruneBrainLinks_Remove_Reduce(existing.Item1, index1, index2, is1in1.Value);
                int[] newItem2 = PruneBrainLinks_Remove_Reduce(existing.Item2, index2, index1, is1in1.Value);

                links.Add(Tuple.Create(newItem1, newItem2));
            }
            private static int[] PruneBrainLinks_Remove_Reduce(int[] existing, int index1, int index2, bool use1)
            {
                if (existing.Length == 1)
                {
                    return existing;
                }

                int removeIndex = use1 ? index1 : index2;

                // Keep all but the one to remove
                return existing.Where(o => o != removeIndex).ToArray();
            }

            //NOTE: This makes sure that key.item1 is less than key.item2
            private static SortedList<Tuple<int, int>, double> GetLengths(Tuple<int, int>[] keys, Item2D[] brains)
            {
                SortedList<Tuple<int, int>, double> retVal = new SortedList<Tuple<int, int>, double>();

                foreach (Tuple<int, int> key in keys.Select(o => o.Item1 < o.Item2 ? o : Tuple.Create(o.Item2, o.Item1)))
                {
                    double distance = (brains[key.Item1].Position - brains[key.Item2].Position).Length;

                    retVal.Add(key, distance);
                }

                return retVal;
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

            /// <summary>
            /// Link brains to io
            /// </summary>
            private static LinkIO[] IOLinks(Item2D[] brains, Item2D[] io, SortedList<Tuple<int, int>, double> allBrainLinks)
            {
                if (brains.Length < 2)
                {
                    throw new ArgumentException("This method requires at least two brains: " + brains.Length.ToString());
                }

                // Look for brains that are really close together, treat those as single brains
                Set2D[] brainSets = IOLinks_PreMergeBrains(brains, allBrainLinks);

                // Link IO to each brain set
                VoronoiResult2D voronoi = Math2D.GetVoronoi(brainSets.Select(o => o.Center).ToArray(), true);
                Tuple<int, int>[] initial = IOLinks_Initial(brainSets, io, voronoi);

                //TODO: If a brain set is saturated, and there is an under used brainset nearby, transfer some links
                Tuple<int, int>[] adjusted = IOLinks_PostAdjust(initial, brainSets, io);

                return adjusted.Select(o => new LinkIO(brainSets[o.Item1], o.Item2, io)).ToArray();
            }

            /// <summary>
            /// This finds brains that are really close together, and turns them into sets (the rest of the brains become 1 item sets)
            /// </summary>
            internal static Set2D[] IOLinks_PreMergeBrains(Item2D[] brains, SortedList<Tuple<int, int>, double> links)
            {
                if (!IOPREMERGE_SHOULD)
                {
                    return Enumerable.Range(0, brains.Length).Select(o => new Set2D(o, brains)).ToArray();
                }

                // Get the AABB of the brains, and use the diagonal as the size
                var aabb = Math2D.GetAABB(brains.Select(o => o.Position));
                double maxDistance = (aabb.Item2 - aabb.Item1).Length;

                // Figure out the max distance allowed for a merge
                double threshold = maxDistance * IOPREMERGE_DISTANCE;

                // Find links that should be merged
                var closePairs = links.
                    Where(o => o.Value <= threshold).
                    OrderBy(o => o.Value).
                    ToArray();

                if (closePairs.Length == 0)
                {
                    return Enumerable.Range(0, brains.Length).Select(o => new Set2D(o, brains)).ToArray();
                }

                // Combine any close pairs that share a point (turn pairs into triples)
                List<List<int>> sets = IOLinks_PreMergeBrains_Sets(closePairs);

                // Build the final list
                return IOLinks_PreMergeBrains_Centers(sets, brains, links);
            }
            private static List<List<int>> IOLinks_PreMergeBrains_Sets(KeyValuePair<Tuple<int, int>, double>[] closePairs)
            {
                List<List<int>> retVal = new List<List<int>>();

                foreach (var pair in closePairs)
                {
                    bool foundOne = false;

                    for (int cntr = 0; cntr < retVal.Count; cntr++)
                    {
                        if (retVal[cntr].Contains(pair.Key.Item1) || retVal[cntr].Contains(pair.Key.Item2))
                        {
                            retVal[cntr].Add(pair.Key.Item1);     // I'll run a distinct at the end of this method
                            retVal[cntr].Add(pair.Key.Item2);

                            foundOne = true;
                            break;
                        }
                    }

                    if (!foundOne)
                    {
                        List<int> set = new List<int>();
                        set.Add(pair.Key.Item1);
                        set.Add(pair.Key.Item2);

                        retVal.Add(set);
                    }
                }

                return retVal;
            }
            private static Set2D[] IOLinks_PreMergeBrains_Centers(List<List<int>> sets, Item2D[] brains, SortedList<Tuple<int, int>, double> links)
            {
                List<Set2D> retVal = new List<Set2D>();

                // Multi brain sets
                retVal.AddRange(sets.Select(o => new Set2D(o.Distinct(), brains)));

                // Single brain sets
                foreach (Tuple<int, int> key in links.Keys)
                {
                    if (!retVal.Any(o => o.Indices.Contains(key.Item1)))
                    {
                        retVal.Add(new Set2D(key.Item1, brains));
                    }

                    if (!retVal.Any(o => o.Indices.Contains(key.Item2)))
                    {
                        retVal.Add(new Set2D(key.Item2, brains));
                    }
                }

                return retVal.ToArray();
            }

            private static Tuple<int, int>[] IOLinks_Initial(Set2D[] brains, Item2D[] io, VoronoiResult2D voronoi)
            {
                List<Tuple<int, int>> retVal = new List<Tuple<int, int>>();
                List<int> remainingIO = Enumerable.Range(0, io.Length).ToList();        // store the remaining so that they can be removed as found (avoid unnecessary IsInside checks)

                for (int brainCntr = 0; brainCntr < brains.Length; brainCntr++)
                {
                    Edge2D[] edges = voronoi.EdgesByControlPoint[brainCntr].Select(o => voronoi.Edges[o]).ToArray();

                    if (edges.Length == 1)
                    {
                        throw new ApplicationException("finish this");
                    }

                    foreach (int ioIndex in remainingIO.ToArray())
                    {
                        if (Edge2D.IsInside(edges, io[ioIndex].Position))
                        {
                            retVal.Add(Tuple.Create(brainCntr, ioIndex));
                            remainingIO.Remove(ioIndex);
                        }
                    }
                }

                return retVal.ToArray();
            }

            private static Tuple<int, int>[] IOLinks_PostAdjust(Tuple<int, int>[] initial, Set2D[] brainSets, Item2D[] io)
            {
                // Get the links and distances between brain sets
                Tuple<int, double>[][] brainLinks = IOLinks_PostAdjust_BrainBrainLinks(brainSets);

                // Turn the initial links into something more usable (list of links by brain)
                BrainBurden[] ioLinks = initial.
                    GroupBy(o => o.Item1).
                    Select(o => new BrainBurden(o.Key, o.Select(p => p.Item2))).
                    ToArray();

                // Make sure there is an element in ioLinks for each brainset (the groupby logic above only grabs brainsets that have links)
                int[] missing = Enumerable.Range(0, brainLinks.Length).Where(o => !ioLinks.Any(p => p.Index == o)).ToArray();

                ioLinks = UtilityCore.Iterate(ioLinks, missing.Select(o => new BrainBurden(o, Enumerable.Empty<int>()))).ToArray();

                // Move one link at a time
                while (true)
                {
                    // Figure out how burdened each brain set is
                    foreach (BrainBurden brain in ioLinks)
                    {
                        brain.Recalc(brainSets, io);
                    }

                    // Find the best link to move
                    if (!IOLinks_PostAdjust_MoveNext(ioLinks, brainLinks, brainSets, io))
                    {
                        break;
                    }
                }


                // Just build it manually
                //return ioLinks.Select(o => new { BrainIndex = o.Index, UtilityCore.Iterate(o.LinksOrig, o.LinksMoved)

                //var test = ioLinks.
                //    Select(o => new { BrainIndex = o.Index, Links = UtilityCore.Iterate(o.LinksOrig, o.LinksMoved).ToArray() }).
                //    ToArray();


                List<Tuple<int, int>> retVal = new List<Tuple<int, int>>();

                foreach (BrainBurden brain in ioLinks)
                {
                    foreach (int ioIndex in UtilityCore.Iterate(brain.LinksOrig, brain.LinksMoved))
                    {
                        retVal.Add(Tuple.Create(brain.Index, ioIndex));
                    }
                }

                return retVal.ToArray();


                //return initial;
            }
            private static Tuple<int, double>[][] IOLinks_PostAdjust_BrainBrainLinks(Set2D[] brainSets)
            {
                // Get the AABB of the brain sets, and use the diagonal as the size
                var aabb = Math2D.GetAABB(brainSets.Select(o => o.Center));
                double maxDistance = (aabb.Item2 - aabb.Item1).Length;

                // Get links between the brains and distances of each link
                var links = Math2D.GetDelaunayTriangulation(brainSets.Select(o => o.Center).ToArray(), true).
                    Select(o => new { Brain1 = o.Item1, Brain2 = o.Item2, Distance = (brainSets[o.Item1].Center - brainSets[o.Item2].Center).Length }).

                    // break here, get stats on lengths, omit links that are excessively long

                    Select(o => new { Brain1 = o.Brain1, Brain2 = o.Brain2, Resistane = (o.Distance / maxDistance) * IOPOSTMERGE_LINKRESISTANCEMULT }).      // calculate the wire's resistance (in terms of burden)
                    ToArray();


                //string dots = string.Join("\r\n", brainSets.Select(o => string.Format("_linksIO2D.Add(AddDot(new Point({0}, {1}), Brushes.Black, Brushes.Gray));", o.Center.X, o.Center.Y)));
                //string lines = string.Join("\r\n", links.Select(o => string.Format("_linksIO2D.Add(AddLine(new Point({0}, {1}), new Point({2}, {3}), Brushes.White));", brainSets[o.Brain1].Center.X, brainSets[o.Brain1].Center.Y, brainSets[o.Brain2].Center.X, brainSets[o.Brain2].Center.Y)));
                //string combined = dots + "\r\n\r\n" + lines;

                Tuple<int, double>[][] retVal = new Tuple<int, double>[brainSets.Length][];

                for (int cntr = 0; cntr < brainSets.Length; cntr++)
                {
                    // Store all links for this brain
                    retVal[cntr] = links.
                        Where(o => o.Brain1 == cntr || o.Brain2 == cntr).       // find links between this brain and another
                        Select(o => Tuple.Create(o.Brain1 == cntr ? o.Brain2 : o.Brain1, o.Resistane)).     // store the link to the other brain and the resistance
                        ToArray();
                }

                return retVal;
            }
            private static bool IOLinks_PostAdjust_MoveNext(BrainBurden[] ioLinks, Tuple<int, double>[][] brainLinks, Set2D[] brainSets, Item2D[] io)
            {
                List<Tuple<BrainBurden, int, double, double>> candidates = new List<Tuple<BrainBurden, int, double, double>>();

                // Find brains that have excess links to give away, and which ones to hand to
                foreach (BrainBurden ioLink in ioLinks.Where(o => o.LinksOrig.Count > 0))
                {
                    foreach (Tuple<int, double> brainLink in brainLinks[ioLink.Index])
                    {
                        double otherBurden = ioLinks.First(o => o.Index == brainLink.Item1).Burden;

                        if (ioLink.Burden > otherBurden + brainLink.Item2)
                        {
                            candidates.Add(Tuple.Create(ioLink, brainLink.Item1, ioLink.Burden - (otherBurden + brainLink.Item2), brainLink.Item2));
                        }
                    }
                }

                // Iterate over them with the largest distance first (can't just pull the top, because an individual io link may represent too big of a burden
                // to be able to shift)
                foreach (var candidate in candidates.OrderByDescending(o => o.Item3))
                {
                    // Try to move a link
                    if (IOLinks_PostAdjust_MoveNext_Brain(candidate.Item1, ioLinks.First(o => o.Index == candidate.Item2), candidate.Item4, brainSets, io))
                    {
                        return true;
                    }
                }

                return false;
            }
            private static bool IOLinks_PostAdjust_MoveNext_Brain(BrainBurden from, BrainBurden to, double linkBurden, Set2D[] brainSets, Item2D[] io)
            {
                // Cache the sizes of the current io links
                double fromIOSize = UtilityCore.Iterate(from.LinksOrig, from.LinksMoved).Sum(o => io[o].Size);
                double toIOSize = UtilityCore.Iterate(to.LinksOrig, to.LinksMoved).Sum(o => io[o].Size);

                // See what the new burdens would be for from and to for each io link
                var newBurdens = from.LinksOrig.Select(o => new
                {
                    IOIndex = o,
                    FromBurden = BrainBurden.CalculateBurden(fromIOSize - io[o].Size, from.BrainSize),
                    ToBurden = BrainBurden.CalculateBurden(toIOSize + io[o].Size, to.BrainSize),
                }).
                    Where(o => o.FromBurden >= linkBurden + o.ToBurden).        // throw out any moves that cause a reverse in burden
                    Select(o => new
                    {
                        IOIndex = o.IOIndex,
                        FromBurden = o.FromBurden,
                        ToBurden = o.ToBurden,
                        Distance = (brainSets[to.Index].Center - io[o.IOIndex].Position).Length,
                        //TODO: Other scoring criterea here
                    }).
                    ToArray();

                if (newBurdens.Length == 0)
                {
                    return false;
                }

                // Come up with the best io to move
                //TODO: Things to consider
                //      Distance between io and to brain
                //      Types of IO that from and to have (try to specialize)
                //
                // When combining scores, can't just score things linearly, then add up the score.  Average performers of multiple categories
                // would win
                var best = newBurdens.OrderBy(o => o.Distance).First();

                // Move the link
                to.LinksMoved.Add(best.IOIndex);
                from.LinksOrig.Remove(best.IOIndex);

                return true;
            }

            #endregion
        }

        #endregion
        #region class: Worker2D_Distance

        private static class Worker2D_Distance
        {
            #region enum: IOLinkupPriority

            public enum IOLinkupPriority
            {
                ShortestDistFirst,
                LongestDistFirst,
                RandomOrder
            }

            #endregion
            #region class: BrainBurden

            private class BrainBurden
            {
                #region Constructor

                public BrainBurden(int index, Set2D[] brainSets, Item2D[] io)
                {
                    this.Index = index;
                    this.Brain = brainSets[index];
                    this.Size = this.Brain.Items.Sum(o => o.Size);

                    _io = io;
                }

                #endregion

                public readonly int Index;
                public readonly Set2D Brain;
                public readonly double Size;

                private readonly Item2D[] _io;

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

            public static void GetLinks(out LinkBrain2D[] brainLinks, out LinkIO[] ioLinks, Item2D[] brains, Item2D[] io, IOLinkupPriority ioLinkupPriority = IOLinkupPriority.ShortestDistFirst, double brainLinkResistanceMult = 10)
            {
                if (brains == null || brains.Length == 0)
                {
                    brainLinks = null;
                    ioLinks = null;
                    return;
                }
                else if (brains.Length == 1)
                {
                    brainLinks = null;
                    ioLinks = Enumerable.Range(0, io.Length).Select(o => new LinkIO(0, o, brains, io)).ToArray();
                    return;
                }

                // Link brains:brains
                SortedList<Tuple<int, int>, double> allBrainLinks;
                Worker2D_Voronoi.BrainLinks(out allBrainLinks, out brainLinks, brains);

                // Link brains:io
                ioLinks = IOLinks(brains, io, allBrainLinks, ioLinkupPriority, brainLinkResistanceMult);
            }

            #region Private Methods

            private static LinkIO[] IOLinks(Item2D[] brains, Item2D[] io, SortedList<Tuple<int, int>, double> allBrainLinks, IOLinkupPriority ioLinkupPriority, double brainLinkResistanceMult)
            {
                // Look for brains that are really close together, treat those as single brains
                Set2D[] brainSets = Worker2D_Voronoi.IOLinks_PreMergeBrains(brains, allBrainLinks);

                #region Brain-IO Distances

                // Figure out the distances between io and brains
                var distancesIO = Enumerable.Range(0, io.Length).
                    Select(o => new
                    {
                        IOIndex = o,
                        BrainDistances = Enumerable.Range(0, brainSets.Length).
                            Select(p => Tuple.Create(p, (brainSets[p].Center - io[o].Position).Length)).        //Item1=BrainIndex, Item2=DistanceToBrain
                            OrderBy(p => p.Item2).      // first brain needs to be the shortest distance
                            ToArray()
                    });

                switch (ioLinkupPriority)
                {
                    case IOLinkupPriority.ShortestDistFirst:
                        distancesIO = distancesIO.OrderBy(o => o.BrainDistances.First().Item2);
                        break;

                    case IOLinkupPriority.LongestDistFirst:
                        distancesIO = distancesIO.OrderByDescending(o => o.BrainDistances.First().Item2);
                        break;

                    case IOLinkupPriority.RandomOrder:
                        //TODO: Come up with a less expensive way of doing this
                        //.Select(o => new { Orig=o, Key=Guid.NewGuid }).OrderBy(o => o.Key).Select(o => o.Orig)
                        Random rand = StaticRandom.GetRandomForThread();
                        distancesIO = distancesIO.
                            Select(o => new { Orig = o, Key = rand.Next(int.MaxValue) }).
                            OrderBy(o => o.Key).
                            Select(o => o.Orig);
                        break;

                    default:
                        throw new ApplicationException("Unknown IOLinkupPriority: " + ioLinkupPriority.ToString());
                }

                distancesIO = distancesIO.ToArray();

                #endregion

                // Brain-Brain Distances
                var distancesBrain = BrainBrainDistances(brainSets, brainLinkResistanceMult);

                // Link IO to brains
                BrainBurden[] links = Enumerable.Range(0, brainSets.Length).Select(o => new BrainBurden(o, brainSets, io)).ToArray();

                foreach (var distanceIO in distancesIO)
                {
                    int ioIndex = distanceIO.IOIndex;
                    int closestBrainIndex = distanceIO.BrainDistances[0].Item1;

                    AddIOLink(links, ioIndex, io[ioIndex].Size, closestBrainIndex, distancesBrain[closestBrainIndex]);
                }

                // Build the return
                List<LinkIO> retVal = new List<LinkIO>();
                foreach (BrainBurden brain in links)
                {
                    foreach (int ioIndex in brain.IOLinks)
                    {
                        retVal.Add(new LinkIO(brain.Brain, ioIndex, io));
                    }
                }

                return retVal.ToArray();
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
                Tuple<int, double>[] burdens = new Tuple<int, double>[finalLinks.Length];

                for (int cntr = 0; cntr < finalLinks.Length; cntr++)
                {
                    int brainIndex = finalLinks[cntr].Index;        // this is likely always the same as cntr, but since that object has brainIndex as a property, I feel safer using it

                    // Adding to the closest brain has no exta cost.  Adding to any other brain has a cost based on the
                    // distance between the closest brain and that other brain
                    double linkCost = 0d;
                    if (brainIndex != closestBrainIndex)
                    {
                        //TODO: Link every brain to every other brain, then get rid of this if statement
                        var matchingBrain = brainBrainBurdens.FirstOrDefault(o => o.Item1 == brainIndex);
                        if (matchingBrain == null)
                        {
                            continue;
                        }

                        linkCost = matchingBrain.Item2;
                    }

                    // LinkCost + IOStorageCost
                    burdens[cntr] = Tuple.Create(cntr, linkCost + BrainBurden.CalculateBurden(finalLinks[cntr].IOSize + ioSize, finalLinks[cntr].Size));
                }

                int cheapestIndex = burdens.
                    Where(o => o != null).
                    OrderBy(o => o.Item2).First().Item1;

                finalLinks[cheapestIndex].AddIOLink(ioIndex);
            }

            private static Tuple<int, double>[][] BrainBrainDistances(Set2D[] brainSets, double brainLinkResistanceMult)
            {
                // Get the AABB of the brain sets, and use the diagonal as the size
                var aabb = Math2D.GetAABB(brainSets.Select(o => o.Center));
                double maxDistance = (aabb.Item2 - aabb.Item1).Length;

                // Get links between the brains and distances of each link
                List<Tuple<int, int, double>> links2 = new List<Tuple<int, int, double>>();
                for (int outer = 0; outer < brainSets.Length - 1; outer++)
                {
                    for (int inner = outer + 1; inner < brainSets.Length; inner++)
                    {
                        double distance = (brainSets[outer].Center - brainSets[inner].Center).Length;
                        double resistance = (distance / maxDistance) * brainLinkResistanceMult;

                        links2.Add(Tuple.Create(outer, inner, resistance));
                    }
                }

                Tuple<int, double>[][] retVal = new Tuple<int, double>[brainSets.Length][];

                for (int cntr = 0; cntr < brainSets.Length; cntr++)
                {
                    // Store all links for this brain
                    retVal[cntr] = links2.
                        Where(o => o.Item1 == cntr || o.Item2 == cntr).       // find links between this brain and another
                        Select(o => Tuple.Create(o.Item1 == cntr ? o.Item2 : o.Item1, o.Item3)).     // store the link to the other brain and the resistance
                        OrderBy(o => o.Item2).
                        ToArray();
                }

                return retVal;
            }

            #endregion
        }

        #endregion

        #region Declaration Section

        private const double HALFSIZE3D = 5;
        private const double RAYLENGTH = 1000;
        private const string OPTIONSFOLDER = "BrainTester2";

        //------------------------------------------------- 2D

        private List<Item2D> _inputs2D = new List<Item2D>();
        private List<Item2D> _outputs2D = new List<Item2D>();
        private List<Item2D> _brains2D = new List<Item2D>();

        private VoronoiResult2D _voronoi2D = null;
        private List<Item2D> _voronoiLines2D = new List<Item2D>();

        /// <summary>
        /// Links between brains and IO
        /// </summary>
        private List<Item2D> _linksIO2D = new List<Item2D>();
        /// <summary>
        /// Brain-Brain links
        /// </summary>
        private List<Item2D> _linksBrain2D = new List<Item2D>();

        private List<Item2D> _misc2D = new List<Item2D>();

        private Brush _voronoiGrayBrush = new SolidColorBrush(UtilityWPF.ColorFromHex("50A0A0A0"));
        private Color _brainLinkColor = UtilityWPF.ColorFromHex("60854D6B");
        private Brush _brainLinkBrush = null;       // this has to be initialized in the constructor
        private Color _brainGroupColor = UtilityWPF.ColorFromHex("C29DA4");
        private Brush _brainGroupBrush = null;
        private Color _ioLinkColor = UtilityWPF.ColorFromHex("FFFFFF");
        private Brush _ioLinkBrush = null;

        //------------------------------------------------- 3D

        /// <summary>
        /// This listens to the mouse/keyboard and controls the camera
        /// </summary>
        private TrackBallRoam _trackballFull = null;

        private List<Item3D> _inputs3D = new List<Item3D>();
        private List<Item3D> _outputs3D = new List<Item3D>();
        private List<Item3D> _brains3D = new List<Item3D>();

        /// <summary>
        /// Links between brains and IO
        /// </summary>
        private List<Item3D> _linksIO3D = new List<Item3D>();
        /// <summary>
        /// Brain-Brain links
        /// </summary>
        private List<Item3D> _linksBrain3D = new List<Item3D>();

        private List<Item3D> _dots3D = new List<Item3D>();
        private Guid? _dots3DID = null;

        private List<Item3D> _misc3D = new List<Item3D>();
        private List<Item3D> _clickRays3D = new List<Item3D>();
        private List<Item3D> _voronoiBlob3D = new List<Item3D>();
        private bool _isNextBlob = StaticRandom.NextBool();

        private bool _isShiftDown = false;
        private List<Point3D> _voronoi2NeighborRayStarts = new List<Point3D>();
        //private List<Voronoi2NeighborData> _voronoi2NeighborData = new List<Voronoi2NeighborData>();
        private VoronoiResult3D _voronoiResult = null;

        private Button _lastClicked3D = null;

        private Color _dotColor = UtilityWPF.ColorFromHex("404040");

        private static Lazy<FontFamily> _font = new Lazy<FontFamily>(() => UtilityWPF.GetFont(UtilityCore.RandomOrder(new[] { "Californian FB", "Garamond", "MingLiU", "MS PMincho", "Palatino Linotype", "Plantagenet Cherokee", "Raavi", "Shonar Bangla", "Calibri" })));

        #endregion

        #region Constructor

        public BrainLinks()
        {
            InitializeComponent();

            // Camera Trackball
            _trackballFull = new TrackBallRoam(_cameraFull);
            _trackballFull.EventSource = grdViewPort;		//NOTE:  If this control doesn't have a background color set, the trackball won't see events (I think transparent is ok, just not null)
            _trackballFull.AllowZoomOnMouseWheel = true;
            _trackballFull.Mappings.AddRange(TrackBallMapping.GetPrebuilt(TrackBallMapping.PrebuiltMapping.MouseComplete));
            //_trackballFull.GetOrbitRadius += new GetOrbitRadiusHandler(Trackball_GetOrbitRadius);

            #region AddItemLocation

            (ComboBox combo, AddItemLocation initial)[] combos1 = new[]
            {
                (cbo2DAddInput, AddItemLocation.Left),
                (cbo2DAddOutput, AddItemLocation.Right),
                (cbo2DAddBrain, AddItemLocation.Center),

                (cbo3DAddInput, AddItemLocation.Left),
                (cbo3DAddOutput, AddItemLocation.Right),
                (cbo3DAddBrain, AddItemLocation.Center),
            };

            foreach (string option in Enum.GetNames(typeof(AddItemLocation)))
            {
                foreach (var combo in combos1)
                {
                    combo.combo.Items.Add(option);
                }
            }

            foreach (var combo in combos1)
            {
                var index =  combo.combo.Items.
                    AsEnumerabIe().
                    Select((o, i) => new { index = i, item = o.ToString() }).
                    First(o => o.item == combo.initial.ToString());

                combo.combo.SelectedIndex = index.index;
            }

            #endregion
            #region ClearWhich

            ComboBox[] combos2 = new[]
            {
                cbo2DClear,
                cbo3DClear,
            };

            foreach (string option in Enum.GetNames(typeof(ClearWhich)))
            {
                foreach (ComboBox combo in combos2)
                {
                    combo.Items.Add(option);
                }
            }

            foreach (ComboBox combo in combos2)
            {
                combo.SelectedIndex = 0;
            }

            #endregion
            #region IOLinkupPriority

            combos2 = new[]
            {
                cbo2DIOLinkupPriority,
                //cbo3DIOLinkupPriority,
            };

            foreach (string option in Enum.GetNames(typeof(Worker2D_Distance.IOLinkupPriority)))
            {
                foreach (ComboBox combo in combos2)
                {
                    combo.Items.Add(option);
                }
            }

            foreach (ComboBox combo in combos2)
            {
                combo.SelectedIndex = 0;
            }

            #endregion

            _brainLinkBrush = new SolidColorBrush(_brainLinkColor);
            _brainGroupBrush = new SolidColorBrush(_brainGroupColor);
            _ioLinkBrush = new SolidColorBrush(_ioLinkColor);
        }

        #endregion

        #region Event Listeners

        private void grdViewPort_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.LeftShift)
                {
                    _isShiftDown = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void grdViewPort_KeyUp(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.LeftShift)
                {
                    _isShiftDown = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void grdViewPort_MouseDown(object sender, MouseButtonEventArgs e)
        {
            const double MAXHITDISTANCE = .3;

            try
            {
                if (!_isShiftDown)
                {
                    // Only fire a ray when they're holding in the shift key
                    return;
                }

                // Fire a ray from the mouse point
                Point clickPoint = e.GetPosition(grdViewPort);
                var clickRay = UtilityWPF.RayFromViewportPoint(_cameraFull, _viewportFull, clickPoint);

                // Combine the possible clickable points into a single list
                var testPoints = UtilityCore.Iterate(
                    _voronoiResult == null ? null : _voronoiResult.ControlPoints.Select((o, i) => Tuple.Create(true, o, i)),
                    _voronoi2NeighborRayStarts.Select((o, i) => Tuple.Create(false, o, i)));

                // See if it passes near one of the voronoi ray start points
                var hit = testPoints.
                    Select(o => new
                    {
                        IsControlPoint = o.Item1,
                        Point = o.Item2,
                        Index = o.Item3,
                        Distance = Math3D.GetClosestDistance_Line_Point(clickRay.Origin, clickRay.Direction, o.Item2)     //NOTE: This treats the ray like a line, but anybody clicking around likely won't have points behind the camera
                    }).
                    Where(o => o.Distance <= MAXHITDISTANCE).
                    OrderBy(o => (o.Point - clickRay.Origin).Length).
                    FirstOrDefault();

                if (hit != null && hit.IsControlPoint)
                {
                    // Remove previous blob
                    _viewportFull.Children.RemoveAll(_voronoiBlob3D.Select(o => o.Visual));
                    _voronoiBlob3D.Clear();

                    _voronoiBlob3D.Add(new Item3D(AddText3D(_viewportFull, hit.Index.ToString(), hit.Point, .4, Colors.Gray)));

                    if (_isNextBlob)
                    {
                        #region Draw Faces

                        foreach (int faceIndex in _voronoiResult.FacesByControlPoint[hit.Index])
                        {
                            Color color1 = UtilityWPF.GetRandomColor(100, 200);
                            Color color1a = Color.FromArgb(64, color1.R, color1.G, color1.B);
                            Color color2 = UtilityWPF.AlphaBlend(color1, Colors.White, .75);
                            Color color2a = Color.FromArgb(64, color2.R, color2.G, color2.B);

                            _voronoiBlob3D.Add(new Item3D(AddFace(_viewportFull, _voronoiResult.Faces[faceIndex], color1a, color2a)));
                        }

                        #endregion
                    }
                    else
                    {
                        #region Draw Blob

                        // Generate a bunch of random points, and only keep the ones for the control point that was clicked
                        var blobPoints = Enumerable.Range(0, 150000).
                            AsParallel().
                            Select(o => Math3D.GetRandomVector_Spherical(HALFSIZE3D * 3).ToPoint()).
                            Select(o => new
                            {
                                Point = o,
                                Closest = _voronoiResult.ControlPoints.Select((p, i) => new { Index = i, Distance = (p - o).LengthSquared }).
                                    OrderBy(p => p.Distance).
                                    First().
                                    Index
                            }).
                            Where(o => o.Closest == hit.Index).
                            ToArray();

                        // Draw a blob out of those points
                        if (blobPoints.Length >= 4)
                        {
                            TriangleIndexed[] triangles = Math3D.GetConvexHull(blobPoints.Select(o => o.Point).ToArray());

                            Color color1 = UtilityWPF.GetRandomColor(100, 200);
                            Color color1a = Color.FromArgb(64, color1.R, color1.G, color1.B);
                            Color color2 = UtilityWPF.AlphaBlend(color1, Colors.White, .75);
                            Color color2a = Color.FromArgb(64, color2.R, color2.G, color2.B);

                            _voronoiBlob3D.Add(new Item3D(AddTriangles(_viewportFull, triangles, color1a, color2a, true)));
                        }

                        #endregion
                    }

                    _isNextBlob = !_isNextBlob;
                }
                else
                {
                    #region Draw Line

                    Point3D from, to;
                    Color color;

                    if (hit == null)
                    {
                        from = clickRay.Origin;
                        to = clickRay.Origin + (clickRay.Direction.ToUnit() * RAYLENGTH);
                        color = Colors.Red;
                    }
                    else
                    {
                        from = hit.Point;
                        to = hit.Point + ((clickRay.Origin - hit.Point).ToUnit() * RAYLENGTH);
                        color = Colors.DodgerBlue;
                    }

                    _clickRays3D.Add(new Item3D(AddLine(_viewportFull, from, to, color, .03), 0, new[] { from, to }));

                    #endregion
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //------- 2D Links
        private void AddInput2D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrepFor2D();
                ClearTempVisuals();

                double size = GetItemSize(chk2DRandomSize.IsChecked.Value);
                Point position = GetRandomPosition2D(GetItemLocation(cbo2DAddInput));
                UIElement visual = AddDot(position, Brushes.LawnGreen, Brushes.DarkGreen, GetDotRadius2D(size));

                _inputs2D.Add(new Item2D(visual, size, position));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void AddOutput2D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrepFor2D();
                ClearTempVisuals();

                double size = GetItemSize(chk2DRandomSize.IsChecked.Value);
                Point position = GetRandomPosition2D(GetItemLocation(cbo2DAddOutput));
                UIElement visual = AddDot(position, Brushes.DodgerBlue, Brushes.Indigo, GetDotRadius2D(size));

                _outputs2D.Add(new Item2D(visual, size, position));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void AddBrain2D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrepFor2D();
                ClearTempVisuals();

                double size = GetItemSize(chk2DRandomSize.IsChecked.Value);
                Point position = GetRandomPosition2D(GetItemLocation(cbo2DAddBrain));
                UIElement visual = AddDot(position, Brushes.HotPink, Brushes.Crimson, GetDotRadius2D(size));

                _brains2D.Add(new Item2D(visual, size, position));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Clear2D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrepFor2D();
                ClearTempVisuals();

                List<Item2D>[] lists = null;

                switch (GetClearWhich(cbo2DClear))
                {
                    case ClearWhich.All:
                        lists = new[] { _inputs2D, _brains2D, _outputs2D };
                        break;

                    case ClearWhich.Brains:
                        lists = new[] { _brains2D };
                        break;

                    case ClearWhich.Inputs:
                        lists = new[] { _inputs2D };
                        break;

                    case ClearWhich.Outputs:
                        lists = new[] { _outputs2D };
                        break;

                    default:
                        throw new ApplicationException("Unknown ClearWhich: " + GetClearWhich(cbo2DClear).ToString());
                }

                foreach (var list in lists)
                {
                    canvas.Children.RemoveAll(list.Select(o => o.Visual));
                    list.Clear();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void VoronoiBrains2D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrepFor2D();
                ClearTempVisuals();

                if (_brains2D.Count == 0)
                {
                    return;
                }

                _voronoi2D = Math2D.GetVoronoi(_brains2D.Select(o => o.Position).ToArray(), true);

                _voronoiLines2D.AddRange(DrawVoronoi(_voronoi2D, Brushes.Black));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void CreateLinksSimple2D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrepFor2D();
                ClearTempVisuals();

                if (_brains2D.Count == 0)
                {
                    return;
                }

                Point[] brainPositions = _brains2D.Select(o => o.Position).ToArray();
                Point[] ioPositions = UtilityCore.Iterate(_inputs2D, _outputs2D).Select(o => o.Position).ToArray();

                //TODO: Don't link every brain to every other brain.  Maybe do a similar voronoi, and only link clusters
                //TODO: It might be interesting to have some brains that are independent of others (only if they are connected to io devices)
                Tuple<int, int>[] brainLinks = Math2D.GetDelaunayTriangulation(brainPositions, chk2DSkipThin.IsChecked.Value, trk2DThinRatio.Value);

                _voronoi2D = Math2D.GetVoronoi(brainPositions, true);

                //TODO: If a brain is over saturated, hand some links to a nearby brain
                //TODO: This method needs to have the logic that spreads out links
                //TODO: Do some random linkage between neighboring cells
                Tuple<int, int>[] ioLinks = Worker2D_Simple.LinkBrainsToIO_Voronoi(_voronoi2D, brainPositions, ioPositions);        //TODO: Don't just take in points.  Need sizes as well

                #region Draw

                // Voronoi lines
                _voronoiLines2D.AddRange(DrawVoronoi(_voronoi2D, _voronoiGrayBrush, 1));

                // Brain-Brain links
                foreach (Tuple<int, int> link in brainLinks)
                {
                    Point from = _brains2D[link.Item1].Position;
                    Point to = _brains2D[link.Item2].Position;

                    UIElement visual = AddLine(from, to, _brainLinkBrush, 2, true);

                    _linksBrain2D.Add(new Item2D(visual, 0, from, to));
                }

                // Brain-IO links
                foreach (Tuple<int, int> link in ioLinks)
                {
                    Point from = _brains2D[link.Item1].Position;
                    Point to = ioPositions[link.Item2];

                    UIElement visual = AddLine(from, to, _ioLinkBrush, isUnder: true);

                    _linksIO2D.Add(new Item2D(visual, 0, from, to));
                }

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void CreateLinksVoronoi2D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrepFor2D();
                ClearTempVisuals();

                if (_brains2D.Count == 0)
                {
                    return;
                }

                var io = UtilityCore.Iterate(_inputs2D, _outputs2D).ToArray();

                LinkBrain2D[] brainLinks = null;
                LinkIO[] ioLinks = null;

                Worker2D_Voronoi.GetLinks(out brainLinks, out ioLinks, _brains2D.ToArray(), io);

                Point[] brainSetPoints = Math2D.GetUnique(ioLinks.Select(o => o.Brains.Center));
                if (brainSetPoints.Length > 1)
                {
                    _voronoi2D = Math2D.GetVoronoi(brainSetPoints, true);       // this is redundant, since the worker has to get this too, but it's just a tester, and I don't want to complicate the interface
                }

                #region Draw

                // Voronoi lines
                if (_voronoi2D != null)
                {
                    _voronoiLines2D.AddRange(DrawVoronoi(_voronoi2D, _voronoiGrayBrush, 1));
                }

                //TODO: Represent the number of links between two items as line thickness

                // Brain-Brain links
                if (brainLinks != null)
                {
                    foreach (var link in brainLinks)
                    {
                        // Group From
                        if (link.Brains1.Items.Length > 1)
                        {
                            _linksBrain2D.Add(DrawBrainGroup(link.Brains1.Positions, link.Brains2.Center));
                        }

                        // Group To
                        if (link.Brains2.Items.Length > 1)
                        {
                            _linksBrain2D.Add(DrawBrainGroup(link.Brains2.Positions, link.Brains2.Center));
                        }

                        // Link
                        UIElement visual = AddLine(link.Brains1.Center, link.Brains2.Center, _brainLinkBrush);

                        _linksBrain2D.Add(new Item2D(visual, 0, link.Brains1.Center, link.Brains2.Center));
                    }
                }

                // Brain-IO links
                if (ioLinks != null)
                {
                    foreach (var link in ioLinks)
                    {
                        // Group From
                        if (link.Brains.Items.Length > 1)
                        {
                            _linksIO2D.Add(DrawBrainGroup(link.Brains.Positions, link.Brains.Center));
                        }

                        // Link
                        UIElement visual = AddLine(link.Brains.Center, link.IO.Position, _ioLinkBrush, isUnder: true);

                        _linksIO2D.Add(new Item2D(visual, 0, link.Brains.Center, link.IO.Position));
                    }
                }

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void CreateLinksDistance2D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrepFor2D();
                ClearTempVisuals();

                if (_brains2D.Count == 0)
                {
                    return;
                }

                var io = UtilityCore.Iterate(_inputs2D, _outputs2D).ToArray();

                LinkBrain2D[] brainLinks = null;
                LinkIO[] ioLinks = null;

                Worker2D_Distance.GetLinks(out brainLinks, out ioLinks, _brains2D.ToArray(), io, GetIOLinkupPriority(cbo2DIOLinkupPriority), trk2DLinkResistMult.Value);

                Point[] brainSetPoints = Math2D.GetUnique(ioLinks.Select(o => o.Brains.Center));

                #region Draw

                //TODO: Represent the number of links between two items as line thickness

                // Brain-Brain links
                if (brainLinks != null)
                {
                    foreach (var link in brainLinks)
                    {
                        // Group From
                        if (link.Brains1.Items.Length > 1)
                        {
                            _linksBrain2D.Add(DrawBrainGroup(link.Brains1.Positions, link.Brains2.Center));
                        }

                        // Group To
                        if (link.Brains2.Items.Length > 1)
                        {
                            _linksBrain2D.Add(DrawBrainGroup(link.Brains2.Positions, link.Brains2.Center));
                        }

                        // Link
                        UIElement visual = AddLine(link.Brains1.Center, link.Brains2.Center, _brainLinkBrush);

                        _linksBrain2D.Add(new Item2D(visual, 0, link.Brains1.Center, link.Brains2.Center));
                    }
                }

                var brushesByBrain = ioLinks.Select(o => o.Brains.Indices[0]).
                    Select(o => new { FirstIndex = o, Brush = chk2DRainbowLinks.IsChecked.Value ? new SolidColorBrush(UtilityWPF.GetRandomColor(150, 220)) : _ioLinkBrush }).
                    ToArray();

                // Brain-IO links
                if (ioLinks != null)
                {
                    foreach (var link in ioLinks)
                    {
                        // Group From
                        if (link.Brains.Items.Length > 1)
                        {
                            _linksIO2D.Add(DrawBrainGroup(link.Brains.Positions, link.Brains.Center));
                        }

                        // Link
                        Brush brush = brushesByBrain.First(o => o.FirstIndex == link.Brains.Indices[0]).Brush;
                        UIElement visual = AddLine(link.Brains.Center, link.IO.Position, brush);

                        _linksIO2D.Add(new Item2D(visual, 0, link.Brains.Center, link.IO.Position));
                    }
                }

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //------- 3D Links
        private void AddInput3D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrepFor3D();
                ClearTempVisuals();

                double size = GetItemSize(chk3DRandomSize.IsChecked.Value);
                Point3D position = GetRandomPosition3D(GetItemLocation(cbo3DAddInput));

                Visual3D visual = AddDot(_viewportFull, position, UtilityWPF.AlphaBlend(Colors.DarkGreen, Colors.LawnGreen, .2), GetDotRadius3D(size), true);
                _inputs3D.Add(new Item3D(visual, size, position));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void AddOutput3D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrepFor3D();
                ClearTempVisuals();

                double size = GetItemSize(chk3DRandomSize.IsChecked.Value);
                Point3D position = GetRandomPosition3D(GetItemLocation(cbo3DAddOutput));

                Visual3D visual = AddDot(_viewportFull, position, UtilityWPF.AlphaBlend(Colors.Indigo, Colors.DodgerBlue, .2), GetDotRadius3D(size), true);
                _outputs3D.Add(new Item3D(visual, size, position));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void AddBrain3D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrepFor3D();
                ClearTempVisuals();

                double size = GetItemSize(chk3DRandomSize.IsChecked.Value);
                Point3D position = GetRandomPosition3D(GetItemLocation(cbo3DAddBrain));

                Visual3D visual = AddDot(_viewportFull, position, UtilityWPF.AlphaBlend(Colors.Crimson, Colors.HotPink, .2), GetDotRadius3D(size), true);
                _brains3D.Add(new Item3D(visual, size, position));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Clear3D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrepFor3D();
                ClearTempVisuals();

                List<Item3D>[] lists = null;

                switch (GetClearWhich(cbo3DClear))
                {
                    case ClearWhich.All:
                        lists = new[] { _inputs3D, _brains3D, _outputs3D };
                        break;

                    case ClearWhich.Brains:
                        lists = new[] { _brains3D };
                        break;

                    case ClearWhich.Inputs:
                        lists = new[] { _inputs3D };
                        break;

                    case ClearWhich.Outputs:
                        lists = new[] { _outputs3D };
                        break;

                    default:
                        throw new ApplicationException("Unknown ClearWhich: " + GetClearWhich(cbo3DClear).ToString());
                }

                foreach (var list in lists)
                {
                    _viewportFull.Children.RemoveAll(list.Select(o => o.Visual));
                    list.Clear();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateLinksSimple3D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrepFor3D();
                ClearTempVisuals();

                if (_brains3D.Count == 0)
                {
                    return;
                }

                Point3D[] brainPositions = _brains3D.SelectMany(o => o.Positions).ToArray();
                Point3D[] ioPositions = UtilityCore.Iterate(_inputs3D, _outputs3D).SelectMany(o => o.Positions).ToArray();
                Item3D[] io = UtilityCore.Iterate(_inputs3D, _outputs3D).ToArray();

                #region Brain Links

                Tuple<int, int>[] brainLinks = null;

                if (brainPositions.Length < 4)
                {
                    brainLinks = UtilityCore.GetPairs(Enumerable.Range(0, brainPositions.Length).ToArray()).ToArray();
                }
                else
                {
                    Tetrahedron[] delaunay = Math3D.GetDelaunay(brainPositions);
                    brainLinks = Tetrahedron.GetUniqueLines(delaunay);
                }

                #endregion

                #region IO Links

                Tuple<int, int>[] ioLinks = null;       //  1=brainIndex, 2=ioIndex

                if (brainPositions.Length > 0 && ioPositions.Length > 0)
                {
                    ioLinks = new Tuple<int, int>[ioPositions.Length];

                    for (int cntr = 0; cntr < ioPositions.Length; cntr++)
                    {
                        int closestBrain = brainPositions.
                            Select((o, i) => new { Index = i, Position = o, DistSqr = (o - ioPositions[cntr]).LengthSquared }).
                            OrderBy(o => o.DistSqr).
                            First().
                            Index;

                        ioLinks[cntr] = Tuple.Create(closestBrain, cntr);
                    }
                }

                #endregion

                #region Draw

                if (brainLinks != null)
                {
                    _linksBrain3D.Add(new Item3D(AddLines(_viewportFull, brainLinks, brainPositions, _brainLinkColor)));
                }

                if (ioLinks != null)
                {
                    _linksIO3D.AddRange(DrawBrainIOLinks3D(_viewportFull, ioLinks, _brains3D.ToArray(), io, _ioLinkColor, chk3DRainbowLinks.IsChecked.Value));
                }

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LinksBrainBrain3D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrepFor3D();
                ClearTempVisuals();

                if (_brains3D.Count == 0)
                {
                    return;
                }

                LinkItem[] brains = _brains3D.
                    Select(o => new LinkItem(o.Position.Value, o.Size)).
                    ToArray();

                ItemLinker_CombineArgs combineArgs = new ItemLinker_CombineArgs()
                {
                    //Ratio_Skinny = .3,
                    //Ratio_Wide = .88,
                    //MergeChance = 0,
                };

                SortedList<Tuple<int, int>, double> all;
                LinkSetPair[] final;
                ItemLinker.Link_Self(out all, out final, brains, combineArgs);

                #region Draw

                if (final != null)
                {
                    foreach (var link in final)
                    {
                        // Group From
                        if (link.Set1.Items.Length > 1)
                        {
                            _linksBrain3D.Add(DrawBrainGroup(_viewportFull, link.Set1.Positions, link.Set2.Center, _brainGroupColor));
                        }

                        // Group To
                        if (link.Set2.Items.Length > 1)
                        {
                            _linksBrain3D.Add(DrawBrainGroup(_viewportFull, link.Set2.Positions, link.Set2.Center, _brainGroupColor));
                        }

                        // Link
                        Visual3D visual = AddLine(_viewportFull, link.Set1.Center, link.Set2.Center, _brainLinkColor);

                        _linksBrain3D.Add(new Item3D(visual, 0, new[] { link.Set1.Center, link.Set2.Center }));
                    }
                }

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void LinksBrainIO3D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrepFor3D();
                ClearTempVisuals();

                LinkItem[] brains = _brains3D.
                    Select(o => new LinkItem(o.Position.Value, o.Size)).
                    ToArray();

                LinkItem[] io_input = _inputs3D.
                    Select(o => new LinkItem(o.Position.Value, o.Size, 0)).
                    ToArray();

                LinkItem[] io_output = _outputs3D.
                    Select(o => new LinkItem(o.Position.Value, o.Size, 1)).
                    ToArray();

                LinkItem[] io = io_input.
                    Concat(io_output).
                    ToArray();

                ItemLinker_OverflowArgs overflowArgs = new ItemLinker_OverflowArgs()
                {
                    LinkResistanceMult = trk3DLinkResistMult.Value,
                };

                ItemLinker_ExtraArgs extraArgs = null;
                if (!trk3DExtraLinkPercentInput.Value.IsNearZero() || !trk3DExtraLinkPercentOutput.Value.IsNearZero())
                {
                    extraArgs = new ItemLinker_ExtraArgs()
                    {
                        Percents = new[]
                        {
                            trk3DExtraLinkPercentInput.Value / 100d,
                            trk3DExtraLinkPercentOutput.Value / 100d,
                        },
                        BySize = chk3DExtraLinkBySize.IsChecked.Value,
                        EvenlyDistribute = chk3DExtraLinkEvenDistribute.IsChecked.Value,
                    };
                }

                Tuple<int, int>[] links = ItemLinker.Link_1_2(brains, io, overflowArgs, extraArgs);

                // Draw
                _linksIO3D.AddRange(DrawBrainIOLinks3D(_viewportFull, links, _brains3D.ToArray(), _inputs3D.Concat(_outputs3D).ToArray(), _ioLinkColor, chk3DRainbowLinks.IsChecked.Value));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void LinksBrainInput3D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrepFor3D();
                ClearTempVisuals();

                LinkItem[] items1 = _inputs3D.
                    Select(o => new LinkItem(o.Position.Value, o.Size)).
                    ToArray();

                LinkItem[] items2 = _brains3D.
                    Select(o => new LinkItem(o.Position.Value, o.Size)).
                    ToArray();

                ItemLinker_OverflowArgs overflowArgs = new ItemLinker_OverflowArgs()
                {
                    LinkResistanceMult = trk3DLinkResistMult.Value,
                };

                Tuple<int, int>[] links = ItemLinker.Link_1_2(items1, items2, overflowArgs);

                // Draw
                _linksIO3D.AddRange(DrawBrainIOLinks3D(_viewportFull, links, _inputs3D.ToArray(), _brains3D.ToArray(), _ioLinkColor, chk3DRainbowLinks.IsChecked.Value));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //------- 3D Voronoi
        private void RandomSquare3D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrepFor3D();
                ClearTempVisuals();
                ClearClickRayData();

                int numPoints;
                if (!int.TryParse(txt3DNumAdd.Text, out numPoints))
                {
                    MessageBox.Show("Couldn't parse the number of points as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Point3D[] points = Enumerable.Range(0, numPoints).Select(o => Math3D.GetRandomVector(HALFSIZE3D).ToPoint()).ToArray();

                Visual3D visual = AddDots(_viewportFull, points, _dotColor);
                _dots3D.Add(new Item3D(visual, 0, points));

                _dots3DID = Guid.NewGuid();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void RandomSphere3D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrepFor3D();
                ClearTempVisuals();
                ClearClickRayData();

                int numPoints;
                if (!int.TryParse(txt3DNumAdd.Text, out numPoints))
                {
                    MessageBox.Show("Couldn't parse the number of points as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Point3D[] points = Enumerable.Range(0, numPoints).Select(o => Math3D.GetRandomVector_Spherical(HALFSIZE3D).ToPoint()).ToArray();

                Visual3D visual = AddDots(_viewportFull, points, _dotColor);
                _dots3D.Add(new Item3D(visual, 0, points));

                _dots3DID = Guid.NewGuid();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void EvenSphere3D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrepFor3D();
                ClearTempVisuals();
                ClearClickRayData();

                int numPoints;
                if (!int.TryParse(txt3DNumAdd.Text, out numPoints))
                {
                    MessageBox.Show("Couldn't parse the number of points as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Point3D[] points = Math3D.GetRandomVectors_Spherical_EvenDist(numPoints, HALFSIZE3D).Select(o => o.ToPoint()).ToArray();

                Visual3D visual = AddDots(_viewportFull, points, _dotColor);
                _dots3D.Add(new Item3D(visual, 0, points));

                _dots3DID = Guid.NewGuid();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void EvenSphereShell3D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrepFor3D();
                ClearTempVisuals();
                ClearClickRayData();

                int numPoints;
                if (!int.TryParse(txt3DNumAdd.Text, out numPoints))
                {
                    MessageBox.Show("Couldn't parse the number of points as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Point3D[] points = Math3D.GetRandomVectors_SphericalShell_EvenDist(numPoints, HALFSIZE3D).Select(o => o.ToPoint()).ToArray();

                Visual3D visual = AddDots(_viewportFull, points, _dotColor);
                _dots3D.Add(new Item3D(visual, 0, points));

                _dots3DID = Guid.NewGuid();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void RandomCircle3D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrepFor3D();
                ClearTempVisuals();
                ClearClickRayData();

                int numPoints;
                if (!int.TryParse(txt3DNumAdd.Text, out numPoints))
                {
                    MessageBox.Show("Couldn't parse the number of points as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Point3D[] points = Enumerable.Range(0, numPoints).Select(o => Math3D.GetRandomVector_Circular(HALFSIZE3D).ToPoint()).ToArray();

                Visual3D visual = AddDots(_viewportFull, points, _dotColor);
                _dots3D.Add(new Item3D(visual, 0, points));

                _dots3DID = Guid.NewGuid();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Perturb3D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrepFor3D();
                ClearTempVisuals();

                Point3D[] points = _dots3D.SelectMany(o => o.Positions).ToArray();
                _viewportFull.Children.RemoveAll(_dots3D.Select(o => o.Visual));
                _dots3D.Clear();

                #region Which Points

                // Figure out which points to move
                int[] whichPoints = null;

                //if (chk3DPerturbOnlyRayClicked.IsChecked.Value)
                //{
                //    if (_voronoi2NeighborData.Count > 0)
                //    {
                //        var clickData = _voronoi2NeighborData[_voronoi2NeighborData.Count - 1];

                //        whichPoints = clickData.Neighbors;
                //    }
                //    else
                //    {
                //        MessageBox.Show("No points to move", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                //        return;
                //    }
                //}
                //else
                //{
                whichPoints = Enumerable.Range(0, points.Length).ToArray();
                //}

                #endregion

                ClearClickRayData();

                // Figure out how much to move them
                double maxDist = trk3DPerturbDist.Value;

                // Move the points
                foreach (int index in whichPoints)
                {
                    points[index] = points[index] + Math3D.GetRandomVector_Spherical(maxDist);
                }

                // Rebuild the visuals
                Visual3D visual = AddDots(_viewportFull, points, _dotColor);
                _dots3D.Add(new Item3D(visual, 0, points));

                //NOTE: Not changing _dots3DID.  That's the whole point of that guid, so that saves can be grouped by it with slightly different perturbations

                if (chk3DPerturbRerun.IsChecked.Value && _lastClicked3D != null)
                {
                    //http://stackoverflow.com/questions/728432/how-to-programmatically-click-a-button-in-wpf
                    _lastClicked3D.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ClearDelVor3D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrepFor3D();
                ClearAllVisuals();

                _dots3DID = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Button3D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //if (e.OriginalSource == btn3DVor2D2Save)
                //{
                //    return;
                //}

                if (e.OriginalSource is Button)
                {
                    _lastClicked3D = (Button)e.OriginalSource;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Delaunay3D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrepFor3D();
                ClearTempVisuals();

                Point3D[] points = _dots3D.SelectMany(o => o.Positions).ToArray();

                Tetrahedron[] delaunay = Math3D.GetDelaunay(points);

                _misc3D.Add(new Item3D(AddLines(_viewportFull, Tetrahedron.GetUniqueLines(delaunay), points, Colors.White)));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void DelaunayPolys3D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrepFor3D();
                ClearTempVisuals();

                Point3D[] points = _dots3D.SelectMany(o => o.Positions).ToArray();

                Tetrahedron[] delaunay = Math3D.GetDelaunay(points);

                var tetras = delaunay.Select(o => new
                {
                    Tetra = o,
                    Color = UtilityWPF.GetRandomColor(100, 200),
                }).ToArray();

                // White lines
                _misc3D.Add(new Item3D(AddLines(_viewportFull, Tetrahedron.GetUniqueLines(delaunay), points, Colors.White, .008)));

                List<Point3D[]> scaledPoints = new List<Point3D[]>();

                // Calculate scaled points, draw edges
                foreach (var tetra in tetras)
                {
                    Point3D center = tetra.Tetra.GetCenterPoint();

                    //_misc3D.Add(new Item3D(AddDot(_viewportFull, center, color, .06, true)));

                    Point3D[] scaledLocal = new Point3D[points.Length];

                    foreach (int index in tetra.Tetra.IndexArray)
                    {
                        scaledLocal[index] = center + ((points[index] - center) * .66);
                    }

                    scaledPoints.Add(scaledLocal);

                    _misc3D.Add(new Item3D(AddLines(_viewportFull, tetra.Tetra.EdgeArray, scaledLocal, tetra.Color)));
                }

                // Draw scaled faces (doing this after all lines to minimize wpf's semitransparency bug)
                for (int cntr = 0; cntr < delaunay.Length; cntr++)
                {
                    Color color = tetras[cntr].Color;

                    var triangles = tetras[cntr].Tetra.FaceArray.
                        Select(o => new TriangleIndexed(o.Index0, o.Index1, o.Index2, scaledPoints[cntr]));

                    _misc3D.Add(new Item3D(AddTriangles(_viewportFull, triangles, Color.FromArgb(64, color.R, color.G, color.B))));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Voronoi3D_Click(object sender, RoutedEventArgs e)
        {
            //const double DELAUNAYRADIUS = .008;
            const double VORONOIRADIUS = .015;

            try
            {
                PrepFor3D();
                ClearTempVisuals();

                _voronoiResult = Math3D.GetVoronoi(_dots3D.SelectMany(o => o.Positions).ToArray(), true);

                // Delaunay
                //_misc3D.Add(new Item3D(AddLines(_viewportFull, Tetrahedron.GetUniqueLines(_voronoiResult.Delaunay), _voronoiResult.ControlPoints, UtilityWPF.ColorFromHex("40000000"), DELAUNAYRADIUS)));

                // Voronoi Interior Edges
                var interiorEdges = _voronoiResult.Edges.
                    Where(o => o.EdgeType == EdgeType.Segment).
                    Select(o => Tuple.Create(o.Index0, o.Index1.Value));

                _misc3D.Add(new Item3D(AddLines(_viewportFull, interiorEdges, _voronoiResult.EdgePoints, Colors.White, VORONOIRADIUS)));

                // Voronoi Rays
                #region all same color

                //var rays = _voronoiResult.Edges.
                //    Where(o => o.EdgeType == EdgeType.Ray).
                //    Select(o => Tuple.Create(o.Point0, o.Point0 + (o.Direction.Value * RAYLENGTH)));

                //_misc3D.Add(new Item3D(AddLines(_viewportFull, rays, Colors.Cornsilk, VORONOIRADIUS)));

                #endregion
                #region color by count

                var raysByIndex = _voronoiResult.Edges.
                    Where(o => o.EdgeType == EdgeType.Ray).
                    ToLookup(o => o.Index0);

                var raysByCount = raysByIndex.
                    Select(o => new { Count = o.Count(), Rays = o }).
                    ToLookup(o => o.Count);

                foreach (var raySet in raysByCount)
                {
                    var rayTuples = raySet.
                        SelectMany(o => o.Rays).
                        Select(o => Tuple.Create(o.Point0, o.Point0 + (o.Direction.Value * RAYLENGTH)));

                    Color color;
                    switch (raySet.Key)
                    {
                        case 1:
                            color = Colors.Cornsilk;
                            break;
                        case 2:
                            color = Colors.Honeydew;
                            break;
                        default:
                            color = Colors.Violet;
                            break;
                    }

                    _misc3D.Add(new Item3D(AddLines(_viewportFull, rayTuples, color, VORONOIRADIUS)));
                }

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void VoronoiDelaunay3D_Click(object sender, RoutedEventArgs e)
        {
            const double DELAUNAYRADIUS = .008;
            const double VORONOIRADIUS = .015;

            try
            {
                PrepFor3D();
                ClearTempVisuals();

                _voronoiResult = Math3D.GetVoronoi(_dots3D.SelectMany(o => o.Positions).ToArray(), true);

                // Delaunay
                _misc3D.Add(new Item3D(AddLines(_viewportFull, Tetrahedron.GetUniqueLines(_voronoiResult.Delaunay), _voronoiResult.ControlPoints, UtilityWPF.ColorFromHex("40000000"), DELAUNAYRADIUS)));

                // Voronoi Interior Edges
                var interiorEdges = _voronoiResult.Edges.
                    Where(o => o.EdgeType == EdgeType.Segment).
                    Select(o => Tuple.Create(o.Index0, o.Index1.Value));

                _misc3D.Add(new Item3D(AddLines(_viewportFull, interiorEdges, _voronoiResult.EdgePoints, Colors.White, VORONOIRADIUS)));

                // Voronoi Rays
                #region all same color

                //var rays = _voronoiResult.Edges.
                //    Where(o => o.EdgeType == EdgeType.Ray).
                //    Select(o => Tuple.Create(o.Point0, o.Point0 + (o.Direction.Value * RAYLENGTH)));

                //_misc3D.Add(new Item3D(AddLines(_viewportFull, rays, Colors.Cornsilk, VORONOIRADIUS)));

                #endregion
                #region color by count

                var raysByIndex = _voronoiResult.Edges.
                    Where(o => o.EdgeType == EdgeType.Ray).
                    ToLookup(o => o.Index0);

                var raysByCount = raysByIndex.
                    Select(o => new { Count = o.Count(), Rays = o }).
                    ToLookup(o => o.Count);

                foreach (var raySet in raysByCount)
                {
                    var rayTuples = raySet.
                        SelectMany(o => o.Rays).
                        Select(o => Tuple.Create(o.Point0, o.Point0 + (o.Direction.Value * RAYLENGTH)));

                    Color color;
                    switch (raySet.Key)
                    {
                        case 1:
                            color = Colors.Cornsilk;
                            break;
                        case 2:
                            color = Colors.Honeydew;
                            break;
                        default:
                            color = Colors.Violet;
                            break;
                    }

                    _misc3D.Add(new Item3D(AddLines(_viewportFull, rayTuples, color, VORONOIRADIUS)));
                }

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //------- Misc
        private void PlaneTiles2D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrepFor2D();
                ClearAllVisuals();

                double width = canvas.ActualWidth;
                double height = canvas.ActualHeight;
                Point center = new Point(width / 2d, height / 2d);

                double size = Math.Min(width, height) * .75;
                double halfSize = size / 2d;

                int numCells = 7;
                double cellSize = size / numCells;

                double tileSizeHalf = cellSize / (4d * 2d);

                // Interior
                //for (int cntr = 1; cntr < numCells; cntr++)
                //{
                //    double x = (center.X - halfSize) + (cntr * cellSize);
                //    double y = (center.Y - halfSize) + (cntr * cellSize);

                //    _misc2D.Add(new Item2D(AddLine(new Point(x, center.Y - halfSize), new Point(x, center.Y + halfSize), Brushes.White, 1)));
                //    _misc2D.Add(new Item2D(AddLine(new Point(center.X - halfSize, y), new Point(center.X + halfSize, y), Brushes.White, 1)));
                //}

                // Border
                _misc2D.Add(new Item2D(AddLine(new Point(center.X - halfSize, center.Y - halfSize), new Point(center.X + halfSize, center.Y - halfSize), Brushes.Black)));
                _misc2D.Add(new Item2D(AddLine(new Point(center.X + halfSize, center.Y - halfSize), new Point(center.X + halfSize, center.Y + halfSize), Brushes.Black)));
                _misc2D.Add(new Item2D(AddLine(new Point(center.X + halfSize, center.Y + halfSize), new Point(center.X - halfSize, center.Y + halfSize), Brushes.Black)));
                _misc2D.Add(new Item2D(AddLine(new Point(center.X - halfSize, center.Y + halfSize), new Point(center.X - halfSize, center.Y - halfSize), Brushes.Black)));

                // Tiles
                for (int xCntr = 0; xCntr <= numCells; xCntr++)
                {
                    double x = (center.X - halfSize) + (xCntr * cellSize);
                    double left = xCntr == 0 ? 0 : -tileSizeHalf;
                    double right = xCntr == numCells ? 0 : tileSizeHalf;

                    for (int yCntr = 0; yCntr <= numCells; yCntr++)
                    {
                        double y = (center.Y - halfSize) + (yCntr * cellSize);
                        double up = yCntr == 0 ? 0 : -tileSizeHalf;
                        double down = yCntr == numCells ? 0 : tileSizeHalf;

                        _misc2D.Add(new Item2D(AddSquare(new Point(x + left, y + up), new Point(x + right, y + down), Brushes.DodgerBlue)));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void PlaneTiles3D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrepFor3D();
                ClearAllVisuals();

                TriangleIndexed plane = new TriangleIndexed(0, 1, 2, new[] { Math3D.GetRandomVector_Spherical(HALFSIZE3D).ToPoint(), Math3D.GetRandomVector_Spherical(HALFSIZE3D).ToPoint(), Math3D.GetRandomVector_Spherical(HALFSIZE3D).ToPoint() });

                // Draw the triangle
                _misc3D.Add(new Item3D(AddLines(_viewportFull, new[] { Tuple.Create(0, 1), Tuple.Create(1, 2), Tuple.Create(2, 0) }, plane.AllPoints, Colors.ForestGreen)));

                // Draw the plane
                ModelVisual3D visual = new ModelVisual3D();
                visual.Content = UtilityWPF.GetPlane(plane, HALFSIZE3D * 2, UtilityWPF.ColorFromHex("555548"), UtilityWPF.ColorFromHex("8F8E3C"));

                _viewportFull.Children.Add(visual);
                _misc3D.Add(new Item3D(visual));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void CircumcenterTetra_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrepFor3D();
                ClearAllVisuals();

                // Create dots
                Point3D[] points = Enumerable.Range(0, 4).Select(o => Math3D.GetRandomVector_Spherical(HALFSIZE3D).ToPoint()).ToArray();

                Visual3D visual = AddDots(_viewportFull, points, _dotColor);
                _dots3D.Add(new Item3D(visual, 0, points));

                // Create a tetrahedron
                Tetrahedron tetra = new Tetrahedron(0, 1, 2, 3, points, new SortedList<Tuple<int, int, int>, TriangleIndexedLinked>());

                // Edge lines
                _misc3D.Add(new Item3D(AddLines(_viewportFull, tetra.EdgeArray, points, Colors.Black)));



                Point3D center = tetra.CircumsphereCenter;

                _misc3D.Add(new Item3D(AddDot(_viewportFull, center, Colors.ForestGreen)));
                _misc3D.Add(new Item3D(AddDot(_viewportFull, center, UtilityWPF.ColorFromHex("205ED66C"), tetra.CircumsphereRadius, true)));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Text3D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrepFor3D();
                ClearAllVisuals();

                FontFamily[] sysFonts = Fonts.SystemFontFamilies.ToArray();
                FontFamily font = sysFonts[StaticRandom.Next(sysFonts.Length)];

                string text = font.Source;

                Color color1 = UtilityWPF.GetRandomColor(0, 255);
                Color color1a = UtilityWPF.GetRandomColor(0, 255);
                Color color2 = UtilityWPF.GetRandomColor(0, 255);
                Color color2a = UtilityWPF.GetRandomColor(0, 255);

                double height = HALFSIZE3D;
                double depth = HALFSIZE3D / StaticRandom.NextDouble(2, 40);

                TextAlignment align = TextAlignment.Center;

                if (DateTime.Today.Month == 5 && DateTime.Today.Day == 4)
                {
                    #region Star Wars

                    color1 = Color.FromRgb(229, 177, 58);
                    color1a = UtilityWPF.ColorFromHex("20FFFFFF");

                    color2 = UtilityWPF.ColorFromHex("000");        // the edge won't be used when the depth is zero
                    color2a = UtilityWPF.ColorFromHex("000");

                    font = UtilityWPF.GetFont("Franklin Gothic");

                    depth = 0;      // when depth is zero, the function won't create edges, and will only create one face

                    grdViewPort.Background = Brushes.Black;

                    align = TextAlignment.Justify;      //TODO: Figure out how to make justified work (formattedText probably needs to be sitting in a container)

                    text =
@"It is a period of civil war. Rebel
spaceships, striking from a hidden
base, have won their first victory
against the evil Galactic Empire.

During the battle, Rebel spies managed
to steal secret plans to the Empire’s
ultimate weapon, the DEATH STAR, an
armored space station with enough
power to destroy an entire planet. 

Pursued by the Empire’s sinister agents,
Princess Leia races home aboard her
starship, custodian of the stolen plans
that can save her people and restore
freedom to the galaxy….";

                    #endregion
                }

                MaterialGroup faceMaterial = new MaterialGroup();
                faceMaterial.Children.Add(new DiffuseMaterial(new SolidColorBrush(color1)));
                faceMaterial.Children.Add(new SpecularMaterial(new SolidColorBrush(color1a), 5d));

                MaterialGroup edgeMaterial = new MaterialGroup();
                edgeMaterial.Children.Add(new DiffuseMaterial(new SolidColorBrush(color2)));
                edgeMaterial.Children.Add(new SpecularMaterial(new SolidColorBrush(color2a), 5d));

                ModelVisual3D visual = new ModelVisual3D();
                visual.Content = UtilityWPF.GetText3D(text, font, faceMaterial, edgeMaterial, height, depth, alignment: align);

                _viewportFull.Children.Add(visual);
                _misc3D.Add(new Item3D(visual));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region ATTEMPTS

        //private void btn3DVoronoing_Click(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        PrepFor3D_Full();
        //        ClearTempVisuals();

        //        Point3D[] points = _dots3D.SelectMany(o => o.Positions).ToArray();

        //        Vertex[] vertices = Enumerable.Range(0, points.Length).Select(o => new Vertex(o, points[o])).ToArray();

        //        //var voronoiMesh = Game.HelperClassesWPF.MIConvexHull.VoronoiMesh.Create(vertices);
        //        var voronoiMesh = Game.HelperClassesWPF.MIConvexHull.VoronoiMesh.Create<Vertex, Cell>(vertices);

        //        // Calculate the centers
        //        List<Point3D> circumcenters = new List<Point3D>();
        //        foreach (Cell cell in voronoiMesh.Vertices)
        //        {
        //            cell.Index = circumcenters.Count;

        //            Point3D circum = Math3D.GetCircumsphere(cell.Vertices[0].Point, cell.Vertices[1].Point, cell.Vertices[2].Point, cell.Vertices[3].Point).Item1;

        //            // See if this already exists in circumcenters
        //            int index = -1;
        //            for (int cntr = 0; cntr < circumcenters.Count; cntr++)
        //            {
        //                if (Math3D.IsNearValue(circumcenters[cntr], circum))
        //                {
        //                    index = cntr;
        //                    break;
        //                }
        //            }

        //            if (index < 0)
        //            {
        //                circumcenters.Add(circum);
        //                cell.CircumcenterIndex = circumcenters.Count - 1;
        //                cell.Circumcenter = circum;
        //            }
        //            else
        //            {
        //                cell.CircumcenterIndex = index;
        //                cell.Circumcenter = circumcenters[index];
        //            }
        //        }

        //        // Interior Edges
        //        var segments = voronoiMesh.Edges.Select(o => Tuple.Create(o.Source.Index, o.Target.Index));
        //        _misc3D.Add(new Item3D(AddLines(_viewportFull, segments, circumcenters.ToArray(), Colors.White)));



        //        // Rays

        //        //NOTE: In 2D there were always 2 neighbors.  In 3D, it looks like there are always 3.  BUT, there are sometimes two
        //        //cells that share the same circumcenter.  I believe those cells need to be grouped together before doing this final pass


        //        var groups = voronoiMesh.Vertices.GroupBy(o => o.CircumcenterIndex).ToArray();






        //        foreach (var cell in voronoiMesh.Vertices)
        //        {
        //            for (int i = 0; i < 4; i++)
        //            {
        //                if (cell.Adjacency[i] == null)
        //                {
        //                    Color color = UtilityWPF.GetRandomColor(0, 255);

        //                    // From
        //                    Point3D from = cell.Circumcenter;
        //                    _misc3D.Add(new Item3D(AddDot(_viewportFull, from, color, .1, true)));

        //                    Point3D cellCenter = Math3D.GetCenter(cell.Vertices.Select(o => o.Point));
        //                    _misc3D.Add(new Item3D(AddDot(_viewportFull, cellCenter, color, .02, true)));


        //                    // Neighbor cells
        //                    Vertex[] neighbors = cell.Vertices.Where((_, j) => j != i).ToArray();

        //                    foreach (Vertex neighbor in neighbors)
        //                    {
        //                        _misc3D.Add(new Item3D(AddLine(_viewportFull, from, neighbor.Point, color, .0075)));
        //                    }

        //                    // The ray needs to be equidistant between the neighboring cell's centers
        //                    Vector3D dir = Math3D.GetCenter(neighbors.Select(o => o.Point)) - from;

        //                    Point3D to = from + dir;
        //                    _misc3D.Add(new Item3D(AddLine(_viewportFull, from, to, Colors.Cornsilk)));

        //                    to = from - dir;
        //                    _misc3D.Add(new Item3D(AddLine(_viewportFull, from, to, Colors.Cornsilk)));
        //                }
        //            }
        //        }





        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}
        //private void btn3DVortex_Click(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        PrepFor3D_Full();
        //        ClearTempVisuals();

        //        Point3D[] points = _dots3D.SelectMany(o => o.Positions).ToArray();

        //        Vertex[] vertices = Enumerable.Range(0, points.Length).Select(o => new Vertex(o, points[o])).ToArray();

        //        //var voronoiMesh = Game.HelperClassesWPF.MIConvexHull.VoronoiMesh.Create(vertices);
        //        var voronoiMesh = Game.HelperClassesWPF.MIConvexHull.VoronoiMesh.Create<Vertex, Cell>(vertices);

        //        // Calculate the centers
        //        int index = 0;
        //        foreach (Cell cell in voronoiMesh.Vertices)
        //        {
        //            cell.Index = index;
        //            cell.CircumcenterIndex = index;     //TODO: no need for this in the final version
        //            cell.Circumcenter = Math3D.GetCircumsphere(cell.Vertices[0].Point, cell.Vertices[1].Point, cell.Vertices[2].Point, cell.Vertices[3].Point).Item1;
        //            index++;
        //        }

        //        // Interior Edges
        //        var segments = voronoiMesh.Edges.Select(o => Tuple.Create(o.Source.Index, o.Target.Index));
        //        _misc3D.Add(new Item3D(AddLines(_viewportFull, segments, voronoiMesh.Vertices.Select(o => o.Circumcenter).ToArray(), Colors.White)));

        //        // Rays
        //        foreach (var cell in voronoiMesh.Vertices)
        //        {
        //            int[] nulls = Enumerable.Range(0, cell.Adjacency.Length).Where(o => cell.Adjacency[o] == null).ToArray();

        //            if (nulls.Length == 0)
        //            {
        //                continue;
        //            }

        //            Color color = UtilityWPF.GetRandomColor(0, 255);

        //            // From
        //            Point3D from = cell.Circumcenter;
        //            _misc3D.Add(new Item3D(AddDot(_viewportFull, from, color, .1, true)));

        //            Point3D cellCenter = Math3D.GetCenter(cell.Vertices.Select(o => o.Point));
        //            _misc3D.Add(new Item3D(AddDot(_viewportFull, cellCenter, color, .02, true)));

        //            // Neighbor cells
        //            Vertex[] neighbors = cell.Vertices.Where((_, i) => !nulls.Contains(i)).ToArray();

        //            foreach (Vertex neighbor in neighbors)
        //            {
        //                _misc3D.Add(new Item3D(AddLine(_viewportFull, from, neighbor.Point, color, .0075)));
        //            }

        //            // The ray needs to be equidistant between the neighboring cell's centers

        //            Vector3D dir;
        //            switch (neighbors.Length)
        //            {
        //                case 1:
        //                    dir = neighbors[0].Point - from;
        //                    break;

        //                case 2:
        //                    dir = Math3D.GetCenter(neighbors.Select(o => o.Point)) - from;
        //                    break;

        //                case 3:
        //                    dir = Vector3D.CrossProduct(neighbors[1].Point - neighbors[0].Point, neighbors[2].Point - neighbors[0].Point);
        //                    break;

        //                default:
        //                    throw new ApplicationException("Unexpected number of neighbors: " + neighbors.Length.ToString());
        //            }

        //            Point3D to = from + dir;
        //            _misc3D.Add(new Item3D(AddLine(_viewportFull, from, to, Colors.Cornsilk)));

        //            to = from - dir;
        //            _misc3D.Add(new Item3D(AddLine(_viewportFull, from, to, Colors.Cornsilk)));
        //        }



        //        var test1 = voronoiMesh.Vertices.Where(o => o.Vertices.Length != 4).ToArray();
        //        var test2 = voronoiMesh.Vertices.Where(o => o.Adjacency.Length != 4).ToArray();

        //        if (test1.Length > 0 || test2.Length > 0)
        //        {
        //            int three = 298329;
        //        }


        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}
        //private void btn3DVortimort_Click(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        PrepFor3D_Full();
        //        ClearTempVisuals();

        //        Point3D[] points = _dots3D.SelectMany(o => o.Positions).ToArray();

        //        Vertex[] vertices = Enumerable.Range(0, points.Length).Select(o => new Vertex(o, points[o])).ToArray();

        //        //var voronoiMesh = Game.HelperClassesWPF.MIConvexHull.VoronoiMesh.Create(vertices);
        //        var voronoiMesh = Game.HelperClassesWPF.MIConvexHull.VoronoiMesh.Create<Vertex, Cell>(vertices);

        //        // Calculate the centers
        //        int index = 0;
        //        foreach (Cell cell in voronoiMesh.Vertices)
        //        {
        //            cell.Index = index;
        //            cell.CircumcenterIndex = index;     //TODO: no need for this in the final version
        //            cell.Circumcenter = Math3D.GetCircumsphere(cell.Vertices[0].Point, cell.Vertices[1].Point, cell.Vertices[2].Point, cell.Vertices[3].Point).Item1;
        //            index++;
        //        }

        //        // Interior Edges
        //        var segments = voronoiMesh.Edges.Select(o => Tuple.Create(o.Source.Index, o.Target.Index));
        //        _misc3D.Add(new Item3D(AddLines(_viewportFull, segments, voronoiMesh.Vertices.Select(o => o.Circumcenter).ToArray(), Colors.White)));

        //        // Rays
        //        foreach (var cell in voronoiMesh.Vertices)
        //        {
        //            int[] nulls = Enumerable.Range(0, cell.Adjacency.Length).Where(o => cell.Adjacency[o] == null).ToArray();

        //            if (nulls.Length == 0)
        //            {
        //                continue;
        //            }

        //            Color color = UtilityWPF.GetRandomColor(0, 255);

        //            // From
        //            Point3D from = cell.Circumcenter;
        //            //_misc3D.Add(new Item3D(AddDot(_viewportFull, from, color, .1, true)));

        //            Point3D cellCenter = Math3D.GetCenter(cell.Vertices.Select(o => o.Point));
        //            //_misc3D.Add(new Item3D(AddDot(_viewportFull, cellCenter, color, .02, true)));

        //            // Neighbor cells
        //            Vertex[] neighbors = cell.Vertices.Where((_, i) => !nulls.Contains(i)).ToArray();

        //            //foreach (Vertex neighbor in neighbors)
        //            //{
        //            //    _misc3D.Add(new Item3D(AddLine(_viewportFull, from, neighbor.Point, color, .0075)));
        //            //}

        //            // The ray needs to be equidistant between the neighboring cell's centers

        //            Vector3D dir;
        //            switch (neighbors.Length)
        //            {
        //                case 1:
        //                    dir = neighbors[0].Point - from;
        //                    break;

        //                case 2:
        //                    dir = Math3D.GetCenter(neighbors.Select(o => o.Point)) - from;
        //                    break;

        //                case 3:
        //                    dir = Vector3D.CrossProduct(neighbors[1].Point - neighbors[0].Point, neighbors[2].Point - neighbors[0].Point);
        //                    break;

        //                default:
        //                    throw new ApplicationException("Unexpected number of neighbors: " + neighbors.Length.ToString());
        //            }

        //            Point3D to = from + dir;
        //            _misc3D.Add(new Item3D(AddLine(_viewportFull, from, to, Colors.Cornsilk)));

        //            to = from - dir;
        //            _misc3D.Add(new Item3D(AddLine(_viewportFull, from, to, Colors.Cornsilk)));
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}
        //private void btn3DVordka_Click(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        PrepFor3D_Full();
        //        ClearTempVisuals();

        //        Point3D[] points = _dots3D.SelectMany(o => o.Positions).ToArray();

        //        Vertex[] vertices = Enumerable.Range(0, points.Length).Select(o => new Vertex(o, points[o])).ToArray();

        //        //var voronoiMesh = Game.HelperClassesWPF.MIConvexHull.VoronoiMesh.Create(vertices);
        //        var voronoiMesh = Game.HelperClassesWPF.MIConvexHull.VoronoiMesh.Create<Vertex, Cell>(vertices);

        //        // Calculate the centers
        //        int index = 0;
        //        foreach (Cell cell in voronoiMesh.Vertices)
        //        {
        //            cell.Index = index;
        //            cell.CircumcenterIndex = index;     //TODO: no need for this in the final version
        //            cell.Circumcenter = Math3D.GetCircumsphere(cell.Vertices[0].Point, cell.Vertices[1].Point, cell.Vertices[2].Point, cell.Vertices[3].Point).Item1;
        //            index++;
        //        }

        //        // Interior Edges
        //        var segments = voronoiMesh.Edges.Select(o => Tuple.Create(o.Source.Index, o.Target.Index));
        //        _misc3D.Add(new Item3D(AddLines(_viewportFull, segments, voronoiMesh.Vertices.Select(o => o.Circumcenter).ToArray(), Colors.White)));

        //        bool drewExtra = false;

        //        // Rays
        //        foreach (var cell in voronoiMesh.Vertices)
        //        {
        //            int[] nulls = Enumerable.Range(0, cell.Adjacency.Length).Where(o => cell.Adjacency[o] == null).ToArray();

        //            if (nulls.Length == 0)
        //            {
        //                continue;
        //            }

        //            Color color = UtilityWPF.GetRandomColor(0, 255);

        //            // From
        //            Point3D from = cell.Circumcenter;
        //            //_misc3D.Add(new Item3D(AddDot(_viewportFull, from, color, .1, true)));

        //            Point3D cellCenter = Math3D.GetCenter(cell.Vertices.Select(o => o.Point));
        //            //_misc3D.Add(new Item3D(AddDot(_viewportFull, cellCenter, color, .02, true)));

        //            // Neighbor cells
        //            Vertex[] neighbors = cell.Vertices.Where((_, i) => !nulls.Contains(i)).ToArray();

        //            //foreach (Vertex neighbor in neighbors)
        //            //{
        //            //    _misc3D.Add(new Item3D(AddLine(_viewportFull, from, neighbor.Point, color, .0075)));
        //            //}

        //            // The ray needs to be equidistant between the neighboring cell's centers

        //            Point3D neighborCenter = Math3D.GetCenter(neighbors.Select(o => o.Point));

        //            Vector3D dir;
        //            switch (neighbors.Length)
        //            {
        //                case 1:
        //                    // Not sure what to do here
        //                    _misc3D.Add(new Item3D(AddDot(_viewportFull, from, color, .1, true)));
        //                    dir = Math3D.GetRandomVector_Circular_Shell(100);
        //                    break;

        //                case 2:
        //                    //TODO: This line is too simplistic.  The final line will be along the plane between these two points, but not necessarily this line
        //                    dir = neighborCenter - from;       // the bisector between the two points is just through the center
        //                    break;

        //                case 3:
        //                    dir = Vector3D.CrossProduct(neighbors[1].Point - neighbors[0].Point, neighbors[2].Point - neighbors[0].Point);      // avg won't work for three points.  Instead get the normal of the triangle they form
        //                    break;

        //                default:
        //                    throw new ApplicationException("Unexpected number of neighbors: " + neighbors.Length.ToString());
        //            }


        //            #region multiplier = 1 or -1

        //            double dot = Vector3D.DotProduct(cellCenter - from, neighborCenter - from);
        //            color = neighbors.Length == 2 ? Colors.Red : (dot > 0 ? Colors.MediumSlateBlue : Colors.SeaGreen);

        //            double dot2 = Vector3D.DotProduct(dir, cellCenter - from);

        //            int multiplier = 1;
        //            if (dot > 0)
        //            {
        //                // Dir needs to be away from cell center
        //                if (dot2 > 0)
        //                {
        //                    multiplier = -1;
        //                }
        //            }
        //            else
        //            {
        //                // Dir needs to be toward cell center
        //                if (dot2 < 0)
        //                {
        //                    multiplier = -1;
        //                }
        //            }

        //            #endregion

        //            Point3D to = from + (dir * multiplier);
        //            _misc3D.Add(new Item3D(AddLine(_viewportFull, from, to, color)));


        //            if (neighbors.Length == 3 && !drewExtra)
        //            {
        //                #region Draw Extra

        //                drewExtra = true;

        //                // Draw the neighbor points larger
        //                _misc3D.Add(new Item3D(AddDots(_viewportFull, neighbors.Select(o => o.Point), UtilityWPF.ColorFromHex("71C74A"), .08, true)));

        //                // Draw the neighbor triangles
        //                Cell[] adjacentCells = cell.Adjacency.Where(o => o != null).ToArray();

        //                Triangle[] adjacentTriangles = new[] { Tuple.Create(0, 1), Tuple.Create(1, 2), Tuple.Create(2, 0) }.
        //                    Select(o => new Triangle(from, adjacentCells[o.Item1].Circumcenter, adjacentCells[o.Item2].Circumcenter)).
        //                    ToArray();

        //                _misc3D.Add(new Item3D(AddTriangles(_viewportFull, adjacentTriangles, UtilityWPF.ColorFromHex("8071C74A"))));








        //                #endregion
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}
        //private void btn3DVormouth_Click(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        PrepFor3D_Full();
        //        ClearTempVisuals();

        //        Point3D[] points = _dots3D.SelectMany(o => o.Positions).ToArray();

        //        Vertex[] vertices = Enumerable.Range(0, points.Length).Select(o => new Vertex(o, points[o])).ToArray();

        //        //var voronoiMesh = Game.HelperClassesWPF.MIConvexHull.VoronoiMesh.Create(vertices);
        //        var voronoiMesh = Game.HelperClassesWPF.MIConvexHull.VoronoiMesh.Create<Vertex, Cell>(vertices);

        //        // Calculate the centers
        //        int index = 0;
        //        foreach (Cell cell in voronoiMesh.Vertices)
        //        {
        //            cell.Index = index;
        //            cell.CircumcenterIndex = index;     //TODO: no need for this in the final version
        //            cell.Circumcenter = Math3D.GetCircumsphere(cell.Vertices[0].Point, cell.Vertices[1].Point, cell.Vertices[2].Point, cell.Vertices[3].Point).Item1;
        //            index++;
        //        }

        //        // Interior Edges
        //        var segments = voronoiMesh.Edges.Select(o => Tuple.Create(o.Source.Index, o.Target.Index));
        //        _misc3D.Add(new Item3D(AddLines(_viewportFull, segments, voronoiMesh.Vertices.Select(o => o.Circumcenter).ToArray(), Colors.White)));

        //        bool drewExtra = false;

        //        // Rays
        //        foreach (var cell in voronoiMesh.Vertices)
        //        {
        //            int[] nulls = Enumerable.Range(0, cell.Adjacency.Length).Where(o => cell.Adjacency[o] == null).ToArray();

        //            if (nulls.Length == 0)
        //            {
        //                continue;
        //            }

        //            Color color = UtilityWPF.GetRandomColor(0, 255);

        //            // From
        //            Point3D from = cell.Circumcenter;

        //            Point3D cellCenter = Math3D.GetCenter(cell.Vertices.Select(o => o.Point));

        //            // Neighbor cells
        //            Vertex[] neighbors = cell.Vertices.Where((_, i) => !nulls.Contains(i)).ToArray();

        //            // The ray needs to be equidistant between the neighboring cell's centers

        //            Point3D neighborCenter = Math3D.GetCenter(neighbors.Select(o => o.Point));

        //            Vector3D dir;
        //            switch (neighbors.Length)
        //            {
        //                case 1:
        //                    // Not sure what to do here
        //                    _misc3D.Add(new Item3D(AddDot(_viewportFull, from, color, .1, true)));
        //                    dir = Math3D.GetRandomVector_Circular_Shell(RAYLENGTH);
        //                    break;

        //                case 2:
        //                    //TODO: This line is too simplistic.  The final line will be along the plane between these two points, but not necessarily this line
        //                    //Also consider that since there are only two neighbors, that means there are two ?????  Maybe this is just a flaw in his algorithm, skipping points
        //                    dir = neighborCenter - from;       // the bisector between the two points is just through the center
        //                    dir = dir.ToUnit() * RAYLENGTH;
        //                    break;

        //                case 3:
        //                    dir = Vector3D.CrossProduct(neighbors[1].Point - neighbors[0].Point, neighbors[2].Point - neighbors[0].Point);      // avg won't work for three points.  Instead get the normal of the triangle they form
        //                    dir = dir.ToUnit() * RAYLENGTH;

        //                    if (Vector3D.DotProduct(neighborCenter - from, dir) < 0)
        //                    {
        //                        // The the logic below assumes that the direction is away from the neighbor center
        //                        dir = -dir;
        //                    }

        //                    break;

        //                default:
        //                    throw new ApplicationException("Unexpected number of neighbors: " + neighbors.Length.ToString());
        //            }

        //            #region multiplier = 1 or -1 (WRONG)

        //            //double dot = Vector3D.DotProduct(cellCenter - neighborCenter, from - neighborCenter);
        //            //color = neighbors.Length == 2 ? Colors.Red : (dot > 0 ? Colors.MediumSlateBlue : Colors.SeaGreen);

        //            //double dot2 = Vector3D.DotProduct(dir, from - neighborCenter);

        //            //int multiplier = 1;
        //            //if (dot > 0)
        //            //{
        //            //    // Dir needs to be away from cell center
        //            //    if (dot2 > 0)
        //            //    {
        //            //        multiplier = -1;
        //            //    }
        //            //}
        //            //else
        //            //{
        //            //    // Dir needs to be toward cell center
        //            //    if (dot2 < 0)
        //            //    {
        //            //        multiplier = -1;
        //            //    }
        //            //}

        //            #endregion
        //            #region multiplier = 1 or -1

        //            int multiplier = 1;
        //            color = neighbors.Length == 2 ? Colors.Red : Colors.MediumSlateBlue;

        //            switch (neighbors.Length)
        //            {
        //                //TODO: Handle the others

        //                case 3:
        //                    // Get the plane defined by the three neighbors
        //                    Triangle plane = new Triangle(neighbors[0].Point, neighbors[1].Point, neighbors[2].Point);

        //                    // See if the line between circumcenter and centroid passes through that plane (see if they are on the same side
        //                    // of the plane, or opposing sides)
        //                    bool isAbove1 = Math3D.DistanceFromPlane(plane, from) > 0d;
        //                    bool isAbove2 = Math3D.DistanceFromPlane(plane, cellCenter) > 0d;

        //                    if (isAbove1 != isAbove2)
        //                    {
        //                        multiplier = -1;
        //                        color = Colors.SeaGreen;
        //                    }
        //                    break;
        //            }

        //            #endregion

        //            Point3D to = from + (dir * multiplier);
        //            _misc3D.Add(new Item3D(AddLine(_viewportFull, from, to, color)));


        //            if (neighbors.Length == 3 && !drewExtra)
        //            {
        //                #region Draw Extra

        //                drewExtra = true;

        //                // These two points are what needs to be considered when deciding direction
        //                _misc3D.Add(new Item3D(AddDot(_viewportFull, from, Colors.White, .06, true)));
        //                _misc3D.Add(new Item3D(AddDot(_viewportFull, cellCenter, Colors.Black, .06, true)));

        //                // Draw the neighbor points larger
        //                _misc3D.Add(new Item3D(AddDots(_viewportFull, neighbors.Select(o => o.Point), UtilityWPF.ColorFromHex("71C74A"), .08, true)));

        //                // Draw the plane of that the neighbor points define
        //                _misc3D.Add(new Item3D(AddPlane(_viewportFull, new Triangle(neighbors[0].Point, neighbors[1].Point, neighbors[2].Point), Colors.Gray, Colors.Lime, neighborCenter)));

        //                // Draw the neighbor triangles
        //                Cell[] adjacentCells = cell.Adjacency.Where(o => o != null).ToArray();

        //                Triangle[] adjacentTriangles = new[] { Tuple.Create(0, 1), Tuple.Create(1, 2), Tuple.Create(2, 0) }.
        //                    Select(o => new Triangle(from, adjacentCells[o.Item1].Circumcenter, adjacentCells[o.Item2].Circumcenter)).
        //                    ToArray();

        //                _misc3D.Add(new Item3D(AddTriangles(_viewportFull, adjacentTriangles, UtilityWPF.ColorFromHex("8071C74A"))));

        //                #endregion
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}
        //private void btn3DVortini_Click(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        PrepFor3D_Full();
        //        ClearTempVisuals();

        //        Point3D[] points = _dots3D.SelectMany(o => o.Positions).ToArray();

        //        Vertex[] vertices = Enumerable.Range(0, points.Length).Select(o => new Vertex(o, points[o])).ToArray();

        //        //var voronoiMesh = Game.HelperClassesWPF.MIConvexHull.VoronoiMesh.Create(vertices);
        //        var voronoiMesh = Game.HelperClassesWPF.MIConvexHull.VoronoiMesh.Create<Vertex, Cell>(vertices);

        //        // Calculate the centers
        //        int index = 0;
        //        foreach (Cell cell in voronoiMesh.Vertices)
        //        {
        //            cell.Index = index;
        //            cell.CircumcenterIndex = index;     //TODO: no need for this in the final version
        //            cell.Circumcenter = Math3D.GetCircumsphere(cell.Vertices[0].Point, cell.Vertices[1].Point, cell.Vertices[2].Point, cell.Vertices[3].Point).Item1;
        //            index++;
        //        }

        //        // Interior Edges
        //        var segments = voronoiMesh.Edges.Select(o => Tuple.Create(o.Source.Index, o.Target.Index));
        //        _misc3D.Add(new Item3D(AddLines(_viewportFull, segments, voronoiMesh.Vertices.Select(o => o.Circumcenter).ToArray(), Colors.White)));

        //        bool drewExtra = false;

        //        // Rays
        //        foreach (var cell in voronoiMesh.Vertices)
        //        {
        //            int[] nulls = Enumerable.Range(0, cell.Adjacency.Length).Where(o => cell.Adjacency[o] == null).ToArray();

        //            if (nulls.Length == 0)
        //            {
        //                continue;
        //            }

        //            Color color = UtilityWPF.GetRandomColor(0, 255);

        //            // From
        //            Point3D from = cell.Circumcenter;

        //            Point3D cellCenter = Math3D.GetCenter(cell.Vertices.Select(o => o.Point));

        //            // Neighbor cells
        //            Vertex[] neighbors = cell.Vertices.Where((_, i) => !nulls.Contains(i)).ToArray();

        //            // The ray needs to be equidistant between the neighboring cell's centers

        //            Point3D neighborCenter = Math3D.GetCenter(neighbors.Select(o => o.Point));

        //            Vector3D dir;
        //            switch (neighbors.Length)
        //            {
        //                case 1:
        //                    // Not sure what to do here
        //                    _misc3D.Add(new Item3D(AddDot(_viewportFull, from, color, .1, true)));
        //                    dir = Math3D.GetRandomVector_Circular_Shell(RAYLENGTH);
        //                    break;

        //                case 2:
        //                    //TODO: This line is too simplistic.  The final line will be along the plane between these two points, but not necessarily this line
        //                    //Also consider that since there are only two neighbors, that means there are two ?????  Maybe this is just a flaw in his algorithm, skipping points
        //                    dir = neighborCenter - from;       // the bisector between the two points is just through the center
        //                    dir = dir.ToUnit() * RAYLENGTH;
        //                    break;

        //                case 3:
        //                    dir = Vector3D.CrossProduct(neighbors[1].Point - neighbors[0].Point, neighbors[2].Point - neighbors[0].Point);      // avg won't work for three points.  Instead get the normal of the triangle they form
        //                    dir = dir.ToUnit() * RAYLENGTH;

        //                    if (Vector3D.DotProduct(neighborCenter - from, dir) < 0)
        //                    {
        //                        // The the logic below assumes that the direction is away from the neighbor center
        //                        dir = -dir;
        //                    }

        //                    break;

        //                default:
        //                    throw new ApplicationException("Unexpected number of neighbors: " + neighbors.Length.ToString());
        //            }

        //            #region multiplier = 1 or -1

        //            int multiplier = 1;

        //            switch (neighbors.Length)
        //            {
        //                //TODO: Handle the others

        //                case 1:
        //                    color = Colors.Red;
        //                    break;

        //                case 2:
        //                    color = Colors.MediumSlateBlue;
        //                    //if ( something )
        //                    //{
        //                    multiplier = -1;
        //                    //    color = Colors.SeaGreen;
        //                    //}
        //                    break;

        //                case 3:
        //                    // Get the plane defined by the three neighbors
        //                    Triangle plane = new Triangle(neighbors[0].Point, neighbors[1].Point, neighbors[2].Point);

        //                    // See if the line between circumcenter and centroid passes through that plane (see if they are on the same side
        //                    // of the plane, or opposing sides)
        //                    bool isAbove1 = Math3D.DistanceFromPlane(plane, from) > 0d;
        //                    bool isAbove2 = Math3D.DistanceFromPlane(plane, cellCenter) > 0d;

        //                    if (isAbove1 != isAbove2)
        //                    {
        //                        multiplier = -1;
        //                    }

        //                    color = Colors.Cornsilk;
        //                    break;
        //            }

        //            #endregion

        //            Point3D to = from + (dir * multiplier);
        //            _misc3D.Add(new Item3D(AddLine(_viewportFull, from, to, color)));


        //            if (neighbors.Length == 2 && !drewExtra)
        //            {
        //                #region Draw Extra

        //                drewExtra = true;

        //                // These two points are what needs to be considered when deciding direction
        //                _misc3D.Add(new Item3D(AddDot(_viewportFull, from, Colors.White, .06, true)));
        //                _misc3D.Add(new Item3D(AddDot(_viewportFull, cellCenter, Colors.Black, .06, true)));

        //                // Draw the neighbor points larger
        //                _misc3D.Add(new Item3D(AddDots(_viewportFull, neighbors.Select(o => o.Point), UtilityWPF.ColorFromHex("71C74A"), .08, true)));




        //                // Draw a triangle between { from, neighbor1, neighbor2 }
        //                _misc3D.Add(new Item3D(AddTriangles(_viewportFull, new[] { new Triangle(from, neighbors[0].Point, neighbors[1].Point) }, UtilityWPF.ColorFromHex("8071C74A"))));



        //                // This isn't needed, it's just the two endpoints of the white lines
        //                //Cell[] adjacentCells = cell.Adjacency.Where(o => o != null).ToArray();


        //                #endregion
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}
        //private void btn3DVoraVoraVora_Click(object sender, RoutedEventArgs e)
        //{
        //    const double DELAUNAYRADIUS = .008;
        //    const double VORONOIRADIUS = .015;

        //    try
        //    {
        //        PrepFor3D_Full();
        //        ClearTempVisuals();

        //        Point3D[] points = _dots3D.SelectMany(o => o.Positions).ToArray();

        //        // Delaunay
        //        Tetrahedron[] delaunay = GetDelaunay(points);
        //        _misc3D.Add(new Item3D(AddLines(_viewportFull, Tetrahedron.GetUniqueLines(delaunay), points, Colors.Black, DELAUNAYRADIUS)));

        //        // Voronoi
        //        Vertex[] vertices = Enumerable.Range(0, points.Length).Select(o => new Vertex(o, points[o])).ToArray();

        //        var voronoiMesh = Game.HelperClassesWPF.MIConvexHull.VoronoiMesh.Create<Vertex, Cell>(vertices);

        //        // Calculate the centers
        //        int index = 0;
        //        foreach (Cell cell in voronoiMesh.Vertices)
        //        {
        //            cell.Index = index;
        //            cell.CircumcenterIndex = index;     //TODO: no need for this in the final version
        //            cell.Circumcenter = Math3D.GetCircumsphere(cell.Vertices[0].Point, cell.Vertices[1].Point, cell.Vertices[2].Point, cell.Vertices[3].Point).Item1;
        //            index++;
        //        }

        //        // Interior Edges
        //        var segments = voronoiMesh.Edges.Select(o => Tuple.Create(o.Source.Index, o.Target.Index));
        //        _misc3D.Add(new Item3D(AddLines(_viewportFull, segments, voronoiMesh.Vertices.Select(o => o.Circumcenter).ToArray(), Colors.White, VORONOIRADIUS)));

        //        #region Rays

        //        foreach (var cell in voronoiMesh.Vertices)
        //        {
        //            int[] nulls = Enumerable.Range(0, cell.Adjacency.Length).Where(o => cell.Adjacency[o] == null).ToArray();

        //            if (nulls.Length == 0)
        //            {
        //                continue;
        //            }

        //            Color color = UtilityWPF.GetRandomColor(0, 255);

        //            // From
        //            Point3D from = cell.Circumcenter;

        //            Point3D cellCenter = Math3D.GetCenter(cell.Vertices.Select(o => o.Point));

        //            // Neighbor cells
        //            Vertex[] neighbors = cell.Vertices.Where((_, i) => !nulls.Contains(i)).ToArray();

        //            // The ray needs to be equidistant between the neighboring cell's centers

        //            Point3D neighborCenter = Math3D.GetCenter(neighbors.Select(o => o.Point));

        //            #region Direction

        //            Vector3D dir;
        //            Vector3D? dir2 = null;
        //            switch (neighbors.Length)
        //            {
        //                case 1:
        //                    // Not sure what to do here
        //                    _misc3D.Add(new Item3D(AddDot(_viewportFull, from, color, .1, true)));
        //                    dir = Math3D.GetRandomVector_Circular_Shell(RAYLENGTH);
        //                    break;

        //                case 2:
        //                    //Tuple<Vector3D, Vector3D> dirs = Get2Ray(from, neighbors, cell.Adjacency.Where(o => o != null).Select(o => o.Circumcenter).ToArray());
        //                    Tuple<Vector3D, Vector3D> dirs = Get2RayFixed(from, cellCenter, neighbors, cell.Adjacency.Where(o => o != null).Select(o => o.Circumcenter).ToArray(), delaunay);

        //                    dir = dirs.Item1.ToUnit() * RAYLENGTH;
        //                    dir2 = dirs.Item2.ToUnit() * RAYLENGTH;
        //                    break;

        //                case 3:
        //                    //dir = Vector3D.CrossProduct(neighbors[1].Point - neighbors[0].Point, neighbors[2].Point - neighbors[0].Point);      // avg won't work for three points.  Instead get the normal of the triangle they form
        //                    dir = from - neighborCenter;
        //                    dir = dir.ToUnit() * RAYLENGTH;

        //                    if (Vector3D.DotProduct(neighborCenter - from, dir) < 0)
        //                    {
        //                        // The the logic below assumes that the direction is away from the neighbor center
        //                        dir = -dir;
        //                    }

        //                    break;

        //                default:
        //                    throw new ApplicationException("Unexpected number of neighbors: " + neighbors.Length.ToString());
        //            }

        //            #endregion

        //            #region multiplier = 1 or -1

        //            int multiplier = 1;

        //            switch (neighbors.Length)
        //            {
        //                //TODO: Handle the others

        //                case 1:
        //                    color = Colors.Red;
        //                    break;

        //                case 2:
        //                    color = Colors.MediumSlateBlue;
        //                    //if ( something )
        //                    //{
        //                    multiplier = -1;
        //                    //    color = Colors.SeaGreen;
        //                    //}
        //                    break;

        //                case 3:
        //                    // Get the plane defined by the three neighbors
        //                    Triangle plane = new Triangle(neighbors[0].Point, neighbors[1].Point, neighbors[2].Point);

        //                    // See if the line between circumcenter and centroid passes through that plane (see if they are on the same side
        //                    // of the plane, or opposing sides)
        //                    bool isAbove1 = Math3D.DistanceFromPlane(plane, from) > 0d;
        //                    bool isAbove2 = Math3D.DistanceFromPlane(plane, cellCenter) > 0d;

        //                    if (isAbove1 != isAbove2)
        //                    {
        //                        multiplier = -1;
        //                    }

        //                    color = Colors.Cornsilk;
        //                    break;
        //            }

        //            #endregion

        //            Point3D to = from + (dir * multiplier);

        //            switch (neighbors.Length)
        //            {
        //                case 1:
        //                    break;

        //                case 2:
        //                    _misc3D.Add(new Item3D(AddLine(_viewportFull, from, to, color, VORONOIRADIUS)));

        //                    if (dir2 != null)
        //                    {
        //                        _misc3D.Add(new Item3D(AddLine(_viewportFull, from, from + (dir2.Value * multiplier), color, VORONOIRADIUS)));
        //                    }
        //                    break;

        //                case 3:
        //                    _misc3D.Add(new Item3D(AddLine(_viewportFull, from, to, color, VORONOIRADIUS)));
        //                    break;

        //                default:
        //                    throw new ApplicationException("Unexpected number of neighbors");
        //            }
        //        }

        //        #endregion

        //        #region Intersected Triangles

        //        //// Detect broken tetrahedrons (lines through triangles
        //        //List<ITriangleIndexed> intersectedTriangles = new List<ITriangleIndexed>();
        //        //List<Point3D> intersectPoints = new List<Point3D>();

        //        //foreach (var segment in delaunay.SelectMany(o => o.EdgeArray))
        //        //{
        //        //    foreach (var face in delaunay.SelectMany(o => o.FaceArray))
        //        //    {
        //        //        if (face.IndexArray.Any(o => o == segment.Item1 || o == segment.Item2))
        //        //        {
        //        //            continue;
        //        //        }

        //        //        Point3D? intersect = Math3D.GetIntersection_Triangle_LineSegment(face, points[segment.Item1], points[segment.Item2]);

        //        //        if (intersect == null)
        //        //        {
        //        //            continue;
        //        //        }

        //        //        if (Math3D.IsNearValue(intersect.Value, points[segment.Item1]) || Math3D.IsNearValue(intersect.Value, points[segment.Item2]))
        //        //        {
        //        //            continue;
        //        //        }

        //        //        if (Math3D.IsNearZero(Vector3D.DotProduct(face.Normal, points[segment.Item2] - points[segment.Item1])))
        //        //        {
        //        //            continue;
        //        //        }

        //        //        // Add visuals
        //        //        if (!intersectedTriangles.Contains(face))
        //        //        {
        //        //            intersectedTriangles.Add(face);
        //        //        }

        //        //        if (!intersectPoints.Any(o => Math3D.IsNearValue(o, intersect.Value)))
        //        //        {
        //        //            intersectPoints.Add(intersect.Value);
        //        //        }
        //        //    }
        //        //}

        //        //if (intersectPoints.Count > 0)
        //        //{
        //        //    _misc3D.Add(new Item3D(AddDots(_viewportFull, intersectPoints, UtilityWPF.ColorFromHex("CC3936"), .06, true)));
        //        //}

        //        //if (intersectedTriangles.Count > 0)
        //        //{
        //        //    _misc3D.Add(new Item3D(AddTriangles(_viewportFull, intersectedTriangles, UtilityWPF.ColorFromHex("80FF4844"))));
        //        //}

        //        #endregion
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}
        //private void btn3DVorpewpewpew_Click(object sender, RoutedEventArgs e)
        //{
        //    const double DELAUNAYRADIUS = .008;
        //    const double VORONOIRADIUS = .04;
        //    const int NUMSCATTERPOINTS = 5000;

        //    try
        //    {
        //        PrepFor3D_Full();
        //        ClearTempVisuals();

        //        Point3D[] points = _dots3D.SelectMany(o => o.Positions).ToArray();

        //        var coloredPoints = points.Select(o => new { Point = o, Color = UtilityWPF.GetRandomColor(100, 200) }).ToArray();

        //        foreach (var coloredPoint in coloredPoints)
        //        {
        //            _misc3D.Add(new Item3D(AddDot(_viewportFull, coloredPoint.Point, coloredPoint.Color, .1)));
        //        }

        //        // Delaunay
        //        //Tetrahedron[] delaunay = GetDelaunay(points);
        //        //_misc3D.Add(new Item3D(AddLines(_viewportFull, Tetrahedron.GetUniqueLines(delaunay), points, Colors.Black, DELAUNAYRADIUS)));

        //        // Voronoi
        //        Vertex[] vertices = Enumerable.Range(0, points.Length).Select(o => new Vertex(o, points[o])).ToArray();

        //        var voronoiMesh = Game.HelperClassesWPF.MIConvexHull.VoronoiMesh.Create<Vertex, Cell>(vertices);

        //        // Calculate the centers
        //        int index = 0;
        //        foreach (Cell cell in voronoiMesh.Vertices)
        //        {
        //            cell.Index = index;
        //            cell.CircumcenterIndex = index;     //TODO: no need for this in the final version
        //            cell.Circumcenter = Math3D.GetCircumsphere(cell.Vertices[0].Point, cell.Vertices[1].Point, cell.Vertices[2].Point, cell.Vertices[3].Point).Item1;
        //            index++;
        //        }

        //        // Interior Edges
        //        var segments = voronoiMesh.Edges.Select(o => Tuple.Create(o.Source.Index, o.Target.Index));
        //        _misc3D.Add(new Item3D(AddLines(_viewportFull, segments, voronoiMesh.Vertices.Select(o => o.Circumcenter).ToArray(), Colors.White, VORONOIRADIUS)));

        //        #region Rays

        //        foreach (var cell in voronoiMesh.Vertices)
        //        {
        //            int[] nulls = Enumerable.Range(0, cell.Adjacency.Length).Where(o => cell.Adjacency[o] == null).ToArray();

        //            if (nulls.Length == 0)
        //            {
        //                continue;
        //            }

        //            Color color = UtilityWPF.GetRandomColor(0, 255);

        //            // From
        //            Point3D from = cell.Circumcenter;

        //            Point3D cellCenter = Math3D.GetCenter(cell.Vertices.Select(o => o.Point));

        //            // Neighbor cells
        //            Vertex[] neighbors = cell.Vertices.Where((_, i) => !nulls.Contains(i)).ToArray();

        //            // The ray needs to be equidistant between the neighboring cell's centers

        //            Point3D neighborCenter = Math3D.GetCenter(neighbors.Select(o => o.Point));

        //            #region Direction

        //            Vector3D dir;
        //            switch (neighbors.Length)
        //            {
        //                case 1:
        //                    // Not sure what to do here
        //                    _misc3D.Add(new Item3D(AddDot(_viewportFull, from, color, .1, true)));
        //                    dir = Math3D.GetRandomVector_Circular_Shell(RAYLENGTH);
        //                    break;

        //                case 2:
        //                    dir = cellCenter - from;

        //                    // Only keep the portion that is in the plane that bisects the two neighbors
        //                    dir = dir.GetProjectedVector(Math3D.GetPlane(from, neighbors[1].Point - neighbors[0].Point));

        //                    dir = dir.ToUnit() * RAYLENGTH;
        //                    break;

        //                case 3:
        //                    dir = Vector3D.CrossProduct(neighbors[1].Point - neighbors[0].Point, neighbors[2].Point - neighbors[0].Point);      // avg won't work for three points.  Instead get the normal of the triangle they form
        //                    dir = dir.ToUnit() * RAYLENGTH;

        //                    if (Vector3D.DotProduct(neighborCenter - from, dir) < 0)
        //                    {
        //                        // The the logic below assumes that the direction is away from the neighbor center
        //                        dir = -dir;
        //                    }

        //                    break;

        //                default:
        //                    throw new ApplicationException("Unexpected number of neighbors: " + neighbors.Length.ToString());
        //            }

        //            #endregion

        //            #region multiplier = 1 or -1

        //            int multiplier = 1;

        //            switch (neighbors.Length)
        //            {
        //                //TODO: Handle the others

        //                case 1:
        //                    color = Colors.Red;
        //                    break;

        //                case 2:
        //                    color = Colors.MediumSlateBlue;
        //                    //if ( something )
        //                    //{
        //                    multiplier = -1;
        //                    //    color = Colors.SeaGreen;
        //                    //}
        //                    break;

        //                case 3:
        //                    // Get the plane defined by the three neighbors
        //                    Triangle plane = new Triangle(neighbors[0].Point, neighbors[1].Point, neighbors[2].Point);

        //                    // See if the line between circumcenter and centroid passes through that plane (see if they are on the same side
        //                    // of the plane, or opposing sides)
        //                    bool isAbove1 = Math3D.DistanceFromPlane(plane, from) > 0d;
        //                    bool isAbove2 = Math3D.DistanceFromPlane(plane, cellCenter) > 0d;

        //                    if (isAbove1 != isAbove2)
        //                    {
        //                        multiplier = -1;
        //                    }

        //                    color = Colors.Cornsilk;
        //                    break;
        //            }

        //            #endregion

        //            Point3D to = from + (dir * multiplier);

        //            switch (neighbors.Length)
        //            {
        //                case 1:
        //                    _misc3D.Add(new Item3D(AddDot(_viewportFull, from, Colors.White, .1)));
        //                    _voronoi2NeighborRayStarts.Add(from);
        //                    break;

        //                case 2:
        //                    //_misc3D.Add(new Item3D(AddLine(_viewportFull, from, to, color, VORONOIRADIUS)));
        //                    _misc3D.Add(new Item3D(AddDot(_viewportFull, from, Colors.White, .1)));
        //                    _voronoi2NeighborRayStarts.Add(from);
        //                    break;

        //                case 3:
        //                    _misc3D.Add(new Item3D(AddLine(_viewportFull, from, to, color, VORONOIRADIUS)));
        //                    break;

        //                default:
        //                    throw new ApplicationException("Unexpected number of neighbors");
        //            }
        //        }

        //        #endregion

        //        #region Scattered Points

        //        var results = Enumerable.Range(0, NUMSCATTERPOINTS).
        //            AsParallel().
        //            Select(o => Math3D.GetRandomVector_Spherical(HALFSIZE3D * 5).ToPoint()).
        //            Select(o => new
        //            {
        //                Point = o,
        //                Closest = points.Select((p, i) => new { Index = i, Distance = (p - o).LengthSquared }).
        //                    OrderBy(p => p.Distance).
        //                    First().
        //                    Index
        //            }).
        //            GroupBy(o => o.Closest).
        //            ToArray();

        //        //foreach (var set in results)
        //        //{
        //        //    _misc3D.Add(new Item3D(AddDots(_viewportFull, set.Select(o => o.Point), coloredPoints[set.Key].Color)));
        //        //}

        //        foreach (var set in results)
        //        {
        //            if (set.Count() < 4)
        //            {
        //                continue;
        //            }

        //            TriangleIndexed[] triangles = Math3D.GetConvexHull(set.Select(o => o.Point).ToArray());

        //            Color color1 = coloredPoints[set.Key].Color;
        //            Color color1a = Color.FromArgb(64, color1.R, color1.G, color1.B);
        //            Color color2 = UtilityWPF.AlphaBlend(color1, Colors.White, .75);
        //            Color color2a = Color.FromArgb(64, color2.R, color2.G, color2.B);

        //            _misc3D.Add(new Item3D(AddTriangles(_viewportFull, triangles, color1a, color2a, true)));
        //        }

        //        #endregion
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}
        //private void btn3DVor2D2_Click(object sender, RoutedEventArgs e)
        //{
        //    const double DELAUNAYRADIUS = .008;
        //    const double VORONOIRADIUS = .015;

        //    try
        //    {
        //        PrepFor3D_Full();
        //        ClearTempVisuals();

        //        Point3D[] points = _dots3D.SelectMany(o => o.Positions).ToArray();

        //        // Delaunay
        //        Tetrahedron[] delaunay = GetDelaunay(points);
        //        //_misc3D.Add(new Item3D(AddLines(_viewportFull, Tetrahedron.GetUniqueLines(delaunay), points, Colors.Black, DELAUNAYRADIUS)));

        //        // Voronoi
        //        Vertex[] vertices = Enumerable.Range(0, points.Length).Select(o => new Vertex(o, points[o])).ToArray();

        //        var voronoiMesh = Game.HelperClassesWPF.MIConvexHull.VoronoiMesh.Create<Vertex, Cell>(vertices);

        //        // Calculate the centers
        //        int index = 0;
        //        foreach (Cell cell in voronoiMesh.Vertices)
        //        {
        //            cell.Index = index;
        //            cell.CircumcenterIndex = index;     //TODO: no need for this in the final version
        //            cell.Circumcenter = Math3D.GetCircumsphere(cell.Vertices[0].Point, cell.Vertices[1].Point, cell.Vertices[2].Point, cell.Vertices[3].Point).Item1;
        //            index++;
        //        }

        //        // Interior Edges
        //        var segments = voronoiMesh.Edges.Select(o => Tuple.Create(o.Source.Index, o.Target.Index));
        //        _misc3D.Add(new Item3D(AddLines(_viewportFull, segments, voronoiMesh.Vertices.Select(o => o.Circumcenter).ToArray(), Colors.White, VORONOIRADIUS)));

        //        #region Rays

        //        foreach (var cell in voronoiMesh.Vertices)
        //        {
        //            int[] nulls = Enumerable.Range(0, cell.Adjacency.Length).Where(o => cell.Adjacency[o] == null).ToArray();

        //            if (nulls.Length == 0)
        //            {
        //                continue;
        //            }

        //            Color color = UtilityWPF.GetRandomColor(0, 255);

        //            // From
        //            Point3D from = cell.Circumcenter;

        //            Point3D cellCenter = Math3D.GetCenter(cell.Vertices.Select(o => o.Point));

        //            // Neighbor cells
        //            Vertex[] neighbors = cell.Vertices.Where((_, i) => !nulls.Contains(i)).ToArray();

        //            // The ray needs to be equidistant between the neighboring cell's centers

        //            Point3D neighborCenter = Math3D.GetCenter(neighbors.Select(o => o.Point));

        //            #region Direction

        //            Vector3D dir;
        //            Vector3D? dir2 = null;
        //            switch (neighbors.Length)
        //            {
        //                case 1:
        //                    // Not sure what to do here
        //                    _misc3D.Add(new Item3D(AddDot(_viewportFull, from, color, .1, true)));
        //                    dir = Math3D.GetRandomVector_Circular_Shell(RAYLENGTH);
        //                    break;

        //                case 2:
        //                    //Tuple<Vector3D, Vector3D> dirs = Get2Ray(from, neighbors, cell.Adjacency.Where(o => o != null).Select(o => o.Circumcenter).ToArray());
        //                    Tuple<Vector3D, Vector3D> dirs = Get2RayFixed(from, cellCenter, neighbors, cell.Adjacency.Where(o => o != null).Select(o => o.Circumcenter).ToArray(), delaunay);

        //                    dir = dirs.Item1.ToUnit() * RAYLENGTH;
        //                    dir2 = dirs.Item2.ToUnit() * RAYLENGTH;
        //                    break;

        //                case 3:
        //                    //dir = Vector3D.CrossProduct(neighbors[1].Point - neighbors[0].Point, neighbors[2].Point - neighbors[0].Point);      // avg won't work for three points.  Instead get the normal of the triangle they form
        //                    dir = from - neighborCenter;
        //                    dir = dir.ToUnit() * RAYLENGTH;

        //                    if (Vector3D.DotProduct(neighborCenter - from, dir) < 0)
        //                    {
        //                        // The the logic below assumes that the direction is away from the neighbor center
        //                        dir = -dir;
        //                    }

        //                    break;

        //                default:
        //                    throw new ApplicationException("Unexpected number of neighbors: " + neighbors.Length.ToString());
        //            }

        //            #endregion

        //            #region multiplier = 1 or -1

        //            int multiplier = 1;

        //            switch (neighbors.Length)
        //            {
        //                //TODO: Handle the others

        //                case 1:
        //                    color = Colors.Red;
        //                    break;

        //                case 2:
        //                    color = Colors.MediumSlateBlue;
        //                    //if ( something )
        //                    //{
        //                    multiplier = -1;
        //                    //    color = Colors.SeaGreen;
        //                    //}
        //                    break;

        //                case 3:
        //                    // Get the plane defined by the three neighbors
        //                    Triangle plane = new Triangle(neighbors[0].Point, neighbors[1].Point, neighbors[2].Point);

        //                    // See if the line between circumcenter and centroid passes through that plane (see if they are on the same side
        //                    // of the plane, or opposing sides)
        //                    bool isAbove1 = Math3D.DistanceFromPlane(plane, from) > 0d;
        //                    bool isAbove2 = Math3D.DistanceFromPlane(plane, cellCenter) > 0d;

        //                    if (isAbove1 != isAbove2)
        //                    {
        //                        multiplier = -1;
        //                    }

        //                    color = Colors.Cornsilk;
        //                    break;
        //            }

        //            #endregion

        //            Point3D to = from + (dir * multiplier);

        //            if (neighbors.Length == 3)
        //            {
        //                _misc3D.Add(new Item3D(AddLine(_viewportFull, from, to, color, VORONOIRADIUS)));
        //            }
        //            else
        //            {
        //                _misc3D.Add(new Item3D(AddLine(_viewportFull, from, to, Colors.Gold, VORONOIRADIUS)));

        //                if (dir2 != null)
        //                {
        //                    _misc3D.Add(new Item3D(AddLine(_viewportFull, from, from + (dir2.Value * multiplier), Colors.Gold, VORONOIRADIUS)));
        //                }

        //                // See if this is one of the points that they clicked on
        //                var matchingClickRays = _clickRays3D.Where(o => o.Positions != null && Math3D.IsNearValue(from, o.Positions[0])).ToArray();

        //                if (matchingClickRays.Length > 0)
        //                {
        //                    #region Visualize

        //                    _misc3D.Add(new Item3D(AddDot(_viewportFull, cellCenter, Colors.Black, .06, true)));

        //                    // Draw the neighbor points larger
        //                    color = UtilityWPF.GetRandomColor(100, 200);
        //                    _misc3D.Add(new Item3D(AddDots(_viewportFull, neighbors.Select(o => o.Point), color, .08, true)));

        //                    if (neighbors.Length == 2)
        //                    {
        //                        // Line between the neighorbors
        //                        _misc3D.Add(new Item3D(AddLine(_viewportFull, neighbors[0].Point, neighbors[1].Point, color)));

        //                        // A bisecting plane between the two neighbors
        //                        Point3D midPoint = Math3D.GetCenter(neighbors.Select(o => o.Point));

        //                        ITriangle plane = Math3D.GetPlane(midPoint, neighbors[1].Point - midPoint);

        //                        _misc3D.Add(new Item3D(AddPlane(_viewportFull, plane, Colors.Gray, color, midPoint)));

        //                        // There's not one ray for a 2 neighbor, there are two.  They are always in the same plane that bisects
        //                        // neighbors[0].Point and neighbors[0].Point
        //                        //
        //                        // The angle between those two rays appears to be the same as the angle between the two points in
        //                        // cell.Adjacency.Where(o => o != null) . Circumcenter
        //                        Point3D[] adjacentCircumcenters = cell.Adjacency.Where(o => o != null).Select(o => o.Circumcenter).ToArray();

        //                        if (adjacentCircumcenters.Length == 2 && matchingClickRays.Length == 2)
        //                        {
        //                            double angleClicked = Vector3D.AngleBetween(matchingClickRays[0].Positions[1] - matchingClickRays[0].Positions[0], matchingClickRays[1].Positions[1] - matchingClickRays[1].Positions[0]);
        //                            double angleAdjacent = Vector3D.AngleBetween(adjacentCircumcenters[0] - from, adjacentCircumcenters[1] - from);

        //                            pnlStats.Children.Add(new TextBlock() { FontSize = 14, Text = string.Format("clicked = {0}", Math.Round(angleClicked, 2).ToString()) });
        //                            pnlStats.Children.Add(new TextBlock() { FontSize = 14, Text = string.Format("adjacent = {0}", Math.Round(angleAdjacent, 2).ToString()) });


        //                            // Store a snapshot in case they want to save it
        //                            Voronoi2NeighborData snapshot = new Voronoi2NeighborData()
        //                            {
        //                                PointsID = _dots3DID.Value,

        //                                Points = points,
        //                                Vertices = vertices.Select(o => Voronoi2NeighborDataExtra.CreateVertex(o.Index, o.Point)).ToArray(),
        //                                //VoronoiMesh = voronoiMesh,

        //                                //Cell = cell,
        //                                CellVertices = cell.Vertices.Select(o => Voronoi2NeighborDataExtra.CreateVertex(o.Index, o.Point)).ToArray(),
        //                                From = from,
        //                                CellCenter = cellCenter,
        //                                CellIndex = cell.Index,
        //                                CellAdjacentCells = cell.Adjacency.Where(o => o != null).Select(o => Voronoi2NeighborDataExtra.CreateAdjacentCell(o.Index, o.Circumcenter, Math3D.GetCenter(o.Vertices.Select(p => p.Point)))).ToArray(),

        //                                Neighbors = neighbors.Select(o => o.Index).ToArray(),
        //                                NeighborCenter = neighborCenter,
        //                                NeighborMidPoint = midPoint,        // this is probably the same as NeighborCenter
        //                                NeighborBisectPlane = plane,

        //                                AdjacentCircumcenters = adjacentCircumcenters,

        //                                ClickRays = matchingClickRays.Select(o => Voronoi2NeighborDataExtra.CreateClickRay(o.Positions[0], o.Positions[1])).ToArray(),

        //                                AngleClicked = angleClicked,
        //                                AngleAdjacent = angleAdjacent,
        //                            };

        //                            _voronoi2NeighborData.Add(snapshot);
        //                            btn3DVor2D2Save.IsEnabled = true;
        //                        }

        //                        // Draw more visuals
        //                        Get2Ray(from, neighbors, adjacentCircumcenters, _viewportFull, _misc3D);
        //                    }

        //                    #endregion
        //                }
        //            }
        //        }

        //        #endregion
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}
        //private void btn3DVor2D2Save_Click(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        if (_voronoi2NeighborData.Count == 0)
        //        {
        //            return;
        //        }

        //        //C:\Users\<username>\AppData\Roaming\Asteroid Miner\BrainTester2\
        //        string baseFolder = UtilityCore.GetOptionsFolder();
        //        string saveFolder = System.IO.Path.Combine(baseFolder, OPTIONSFOLDER);
        //        Directory.CreateDirectory(saveFolder);

        //        // Build a file for each snapshot
        //        foreach (var snapshot in _voronoi2NeighborData)
        //        {
        //            // Create a unique filename
        //            string filename = null;

        //            for (int cntr = 1; cntr < int.MaxValue; cntr++)
        //            {
        //                filename = System.IO.Path.Combine(saveFolder, snapshot.PointsID.ToString() + " - " + cntr.ToString() + ".xml");

        //                if (!File.Exists(filename))
        //                {
        //                    break;
        //                }
        //            }

        //            UtilityCore.SerializeToFile(filename, snapshot);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}
        //private void btn3DVorrestRun_Click(object sender, RoutedEventArgs e)
        //{
        //    const double DELAUNAYRADIUS = .008;
        //    const double VORONOIRADIUS = .04;
        //    const int NUMSCATTERPOINTS = 5000;

        //    try
        //    {
        //        PrepFor3D_Full();
        //        ClearTempVisuals();

        //        Point3D[] points = _dots3D.SelectMany(o => o.Positions).ToArray();

        //        var coloredPoints = points.Select(o => new { Point = o, Color = UtilityWPF.GetRandomColor(100, 200) }).ToArray();

        //        foreach (var coloredPoint in coloredPoints)
        //        {
        //            _misc3D.Add(new Item3D(AddDot(_viewportFull, coloredPoint.Point, coloredPoint.Color, .1)));
        //        }

        //        // Delaunay
        //        Tetrahedron[] delaunay = GetDelaunay(points);
        //        //_misc3D.Add(new Item3D(AddLines(_viewportFull, Tetrahedron.GetUniqueLines(delaunay), points, Colors.Black, DELAUNAYRADIUS)));

        //        // Voronoi
        //        #region Calc Voronoi

        //        Vertex[] vertices = Enumerable.Range(0, points.Length).Select(o => new Vertex(o, points[o])).ToArray();

        //        var voronoiMesh = Game.HelperClassesWPF.MIConvexHull.VoronoiMesh.Create<Vertex, Cell>(vertices);

        //        // Calculate the centers
        //        int index = 0;
        //        foreach (Cell cell in voronoiMesh.Vertices)
        //        {
        //            cell.Index = index;
        //            cell.CircumcenterIndex = index;     //TODO: no need for this in the final version
        //            cell.Circumcenter = Math3D.GetCircumsphere(cell.Vertices[0].Point, cell.Vertices[1].Point, cell.Vertices[2].Point, cell.Vertices[3].Point).Item1;
        //            index++;
        //        }

        //        #endregion

        //        // Interior Edges
        //        var segments = voronoiMesh.Edges.Select(o => Tuple.Create(o.Source.Index, o.Target.Index));
        //        _misc3D.Add(new Item3D(AddLines(_viewportFull, segments, voronoiMesh.Vertices.Select(o => o.Circumcenter).ToArray(), Colors.White, VORONOIRADIUS)));

        //        #region Rays

        //        foreach (var cell in voronoiMesh.Vertices)
        //        {
        //            int[] nulls = Enumerable.Range(0, cell.Adjacency.Length).Where(o => cell.Adjacency[o] == null).ToArray();

        //            if (nulls.Length == 0)
        //            {
        //                continue;
        //            }

        //            Color color = UtilityWPF.GetRandomColor(0, 255);

        //            // From
        //            Point3D from = cell.Circumcenter;

        //            Point3D cellCenter = Math3D.GetCenter(cell.Vertices.Select(o => o.Point));

        //            // Neighbor cells
        //            Vertex[] neighbors = cell.Vertices.Where((_, i) => !nulls.Contains(i)).ToArray();

        //            // The ray needs to be equidistant between the neighboring cell's centers

        //            Point3D neighborCenter = Math3D.GetCenter(neighbors.Select(o => o.Point));

        //            #region Direction

        //            Vector3D dir;
        //            Vector3D? dir2 = null;
        //            switch (neighbors.Length)
        //            {
        //                case 1:
        //                    // Not sure what to do here
        //                    _misc3D.Add(new Item3D(AddDot(_viewportFull, from, color, .1, true)));
        //                    dir = Math3D.GetRandomVector_Circular_Shell(RAYLENGTH);
        //                    break;

        //                case 2:
        //                    //Tuple<Vector3D, Vector3D> dirs = Get2Ray(from, neighbors, cell.Adjacency.Where(o => o != null).Select(o => o.Circumcenter).ToArray());
        //                    Tuple<Vector3D, Vector3D> dirs = Get2RayFixed(from, cellCenter, neighbors, cell.Adjacency.Where(o => o != null).Select(o => o.Circumcenter).ToArray(), delaunay);

        //                    dir = dirs.Item1.ToUnit() * RAYLENGTH;
        //                    dir2 = dirs.Item2.ToUnit() * RAYLENGTH;
        //                    break;

        //                case 3:
        //                    dir = Vector3D.CrossProduct(neighbors[1].Point - neighbors[0].Point, neighbors[2].Point - neighbors[0].Point);      // avg won't work for three points.  Instead get the normal of the triangle they form
        //                    dir = dir.ToUnit() * RAYLENGTH;

        //                    if (Vector3D.DotProduct(neighborCenter - from, dir) < 0)
        //                    {
        //                        // The the logic below assumes that the direction is away from the neighbor center
        //                        dir = -dir;
        //                    }

        //                    break;

        //                default:
        //                    throw new ApplicationException("Unexpected number of neighbors: " + neighbors.Length.ToString());
        //            }

        //            #endregion

        //            #region multiplier = 1 or -1

        //            int multiplier = 1;

        //            switch (neighbors.Length)
        //            {
        //                //TODO: Handle the others

        //                case 1:
        //                    color = Colors.Red;
        //                    break;

        //                case 2:
        //                    color = Colors.MediumSlateBlue;
        //                    //if ( something )
        //                    //{
        //                    multiplier = -1;
        //                    //    color = Colors.SeaGreen;
        //                    //}
        //                    break;

        //                case 3:
        //                    // Get the plane defined by the three neighbors
        //                    Triangle plane = new Triangle(neighbors[0].Point, neighbors[1].Point, neighbors[2].Point);

        //                    // See if the line between circumcenter and centroid passes through that plane (see if they are on the same side
        //                    // of the plane, or opposing sides)
        //                    bool isAbove1 = Math3D.DistanceFromPlane(plane, from) > 0d;
        //                    bool isAbove2 = Math3D.DistanceFromPlane(plane, cellCenter) > 0d;

        //                    if (isAbove1 != isAbove2)
        //                    {
        //                        multiplier = -1;
        //                    }

        //                    color = Colors.Cornsilk;
        //                    break;
        //            }

        //            #endregion

        //            Point3D to = from + (dir * multiplier);

        //            switch (neighbors.Length)
        //            {
        //                case 1:
        //                    _misc3D.Add(new Item3D(AddDot(_viewportFull, from, Colors.White, .1)));
        //                    _voronoi2NeighborRayStarts.Add(from);
        //                    break;

        //                case 2:
        //                    _misc3D.Add(new Item3D(AddLine(_viewportFull, from, to, color, VORONOIRADIUS)));

        //                    if (dir2 != null)
        //                    {
        //                        _misc3D.Add(new Item3D(AddLine(_viewportFull, from, from + (dir2.Value * multiplier), color, VORONOIRADIUS)));
        //                    }

        //                    _misc3D.Add(new Item3D(AddDot(_viewportFull, from, Colors.White, .1)));
        //                    _voronoi2NeighborRayStarts.Add(from);
        //                    break;

        //                case 3:
        //                    _misc3D.Add(new Item3D(AddLine(_viewportFull, from, to, color, VORONOIRADIUS)));
        //                    break;

        //                default:
        //                    throw new ApplicationException("Unexpected number of neighbors");
        //            }
        //        }

        //        #endregion

        //        #region Scattered Points

        //        var results = Enumerable.Range(0, NUMSCATTERPOINTS).
        //            AsParallel().
        //            Select(o => Math3D.GetRandomVector_Spherical(HALFSIZE3D * 5).ToPoint()).
        //            Select(o => new
        //            {
        //                Point = o,
        //                Closest = points.Select((p, i) => new { Index = i, Distance = (p - o).LengthSquared }).
        //                    OrderBy(p => p.Distance).
        //                    First().
        //                    Index
        //            }).
        //            ToLookup(o => o.Closest);

        //        //foreach (var set in results)
        //        //{
        //        //    _misc3D.Add(new Item3D(AddDots(_viewportFull, set.Select(o => o.Point), coloredPoints[set.Key].Color)));
        //        //}

        //        foreach (var set in results)
        //        {
        //            if (set.Count() < 4)
        //            {
        //                continue;
        //            }

        //            TriangleIndexed[] triangles = Math3D.GetConvexHull(set.Select(o => o.Point).ToArray());

        //            Color color1 = coloredPoints[set.Key].Color;
        //            Color color1a = Color.FromArgb(64, color1.R, color1.G, color1.B);
        //            Color color2 = UtilityWPF.AlphaBlend(color1, Colors.White, .75);
        //            Color color2a = Color.FromArgb(64, color2.R, color2.G, color2.B);

        //            _misc3D.Add(new Item3D(AddTriangles(_viewportFull, triangles, color1a, color2a, true)));
        //        }

        //        #endregion
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}
        //private void btn3DVorever_Click(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        const double DELAUNAYRADIUS = .008;
        //        const double VORONOIRADIUS = .015;

        //        PrepFor3D_Full();
        //        ClearTempVisuals();

        //        Point3D[] points = _dots3D.SelectMany(o => o.Positions).ToArray();

        //        // Delaunay
        //        Tetrahedron[] delaunay = GetDelaunay(points);
        //        _misc3D.Add(new Item3D(AddLines(_viewportFull, Tetrahedron.GetUniqueLines(delaunay), points, UtilityWPF.ColorFromHex("40000000"), DELAUNAYRADIUS)));

        //        // Voronoi
        //        Vertex[] vertices = points.Select((o, i) => new Vertex(i, o)).ToArray();

        //        var voronoiMesh = Game.HelperClassesWPF.MIConvexHull.VoronoiMesh.Create<Vertex, Cell>(vertices);

        //        // Calculate the centers
        //        int index = 0;
        //        foreach (Cell cell in voronoiMesh.Vertices)
        //        {
        //            cell.Index = index;
        //            cell.CircumcenterIndex = index;     //TODO: no need for this in the final version
        //            cell.Circumcenter = Math3D.GetCircumsphere(cell.Vertices[0].Point, cell.Vertices[1].Point, cell.Vertices[2].Point, cell.Vertices[3].Point).Item1;
        //            index++;
        //        }

        //        // Interior Edges
        //        var segments = voronoiMesh.Edges.Select(o => Tuple.Create(o.Source.Index, o.Target.Index));
        //        _misc3D.Add(new Item3D(AddLines(_viewportFull, segments, voronoiMesh.Vertices.Select(o => o.Circumcenter).ToArray(), Colors.White, VORONOIRADIUS)));

        //        #region Rays

        //        foreach (var cell in voronoiMesh.Vertices)
        //        {
        //            int[] nulls = Enumerable.Range(0, cell.Adjacency.Length).Where(o => cell.Adjacency[o] == null).ToArray();

        //            if (nulls.Length == 0)
        //            {
        //                continue;
        //            }

        //            Color color = UtilityWPF.GetRandomColor(0, 255);

        //            // From
        //            Point3D from = cell.Circumcenter;

        //            Point3D cellCenter = Math3D.GetCenter(cell.Vertices.Select(o => o.Point));

        //            // Neighbor cells
        //            Vertex[] neighbors = cell.Vertices.Where((_, i) => !nulls.Contains(i)).ToArray();

        //            // The ray needs to be equidistant between the neighboring cell's centers

        //            Point3D neighborCenter = Math3D.GetCenter(neighbors.Select(o => o.Point));

        //            #region Direction

        //            Vector3D dir;
        //            Vector3D? dir2 = null;
        //            switch (neighbors.Length)
        //            {
        //                case 1:
        //                    // Not sure what to do here
        //                    _misc3D.Add(new Item3D(AddDot(_viewportFull, from, color, .1, true)));
        //                    dir = Math3D.GetRandomVector_Circular_Shell(RAYLENGTH);
        //                    break;

        //                case 2:
        //                    //Tuple<Vector3D, Vector3D> dirs = Get2Ray(from, neighbors, cell.Adjacency.Where(o => o != null).Select(o => o.Circumcenter).ToArray());
        //                    Tuple<Vector3D, Vector3D> dirs = Get2RayFixed(from, cellCenter, neighbors, cell.Adjacency.Where(o => o != null).Select(o => o.Circumcenter).ToArray(), delaunay);

        //                    dir = dirs.Item1.ToUnit() * RAYLENGTH;
        //                    dir2 = dirs.Item2.ToUnit() * RAYLENGTH;
        //                    break;

        //                case 3:
        //                    //dir = Vector3D.CrossProduct(neighbors[1].Point - neighbors[0].Point, neighbors[2].Point - neighbors[0].Point);      // avg won't work for three points.  Instead get the normal of the triangle they form
        //                    dir = from - neighborCenter;
        //                    dir = dir.ToUnit() * RAYLENGTH;

        //                    if (Vector3D.DotProduct(neighborCenter - from, dir) < 0)
        //                    {
        //                        // The the logic below assumes that the direction is away from the neighbor center
        //                        dir = -dir;
        //                    }

        //                    break;

        //                default:
        //                    throw new ApplicationException("Unexpected number of neighbors: " + neighbors.Length.ToString());
        //            }

        //            #endregion

        //            #region multiplier = 1 or -1

        //            int multiplier = 1;

        //            switch (neighbors.Length)
        //            {
        //                //TODO: Handle the others

        //                case 1:
        //                    color = Colors.Red;
        //                    break;

        //                case 2:
        //                    color = Colors.MediumSlateBlue;
        //                    //if ( something )
        //                    //{
        //                    multiplier = -1;
        //                    //    color = Colors.SeaGreen;
        //                    //}
        //                    break;

        //                case 3:
        //                    // Get the plane defined by the three neighbors
        //                    Triangle plane = new Triangle(neighbors[0].Point, neighbors[1].Point, neighbors[2].Point);

        //                    // See if the line between circumcenter and centroid passes through that plane (see if they are on the same side
        //                    // of the plane, or opposing sides)
        //                    bool isAbove1 = Math3D.DistanceFromPlane(plane, from) > 0d;
        //                    bool isAbove2 = Math3D.DistanceFromPlane(plane, cellCenter) > 0d;

        //                    if (isAbove1 != isAbove2)
        //                    {
        //                        multiplier = -1;
        //                    }

        //                    color = Colors.Cornsilk;
        //                    break;
        //            }

        //            #endregion

        //            Point3D to = from + (dir * multiplier);

        //            #region draw rays

        //            switch (neighbors.Length)
        //            {
        //                case 1:
        //                    break;

        //                case 2:
        //                    _misc3D.Add(new Item3D(AddLine(_viewportFull, from, to, color, VORONOIRADIUS)));

        //                    if (dir2 != null)
        //                    {
        //                        _misc3D.Add(new Item3D(AddLine(_viewportFull, from, from + (dir2.Value * multiplier), color, VORONOIRADIUS)));
        //                    }
        //                    break;

        //                case 3:
        //                    _misc3D.Add(new Item3D(AddLine(_viewportFull, from, to, color, VORONOIRADIUS)));
        //                    break;

        //                default:
        //                    throw new ApplicationException("Unexpected number of neighbors");
        //            }

        //            #endregion
        //        }

        //        #endregion
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}

        //private void btn3DAnalyzeSaves_Click(object sender, RoutedEventArgs e)
        //{
        //    const double MARGIN = HALFSIZE3D / 2;

        //    try
        //    {
        //        //C:\Users\<username>\AppData\Roaming\Asteroid Miner\BrainTester2\
        //        string folder = System.IO.Path.Combine(UtilityCore.GetOptionsFolder(), OPTIONSFOLDER);
        //        if (!Directory.Exists(folder))
        //        {
        //            MessageBox.Show("Saves folder doesn't exist", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
        //            return;
        //        }

        //        string[] filenames = Directory.GetFiles(folder);

        //        if (filenames.Length == 0)
        //        {
        //            MessageBox.Show("There are no saved files", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
        //            return;
        //        }

        //        // The files start with the same guid, so group by that
        //        var singleSetFilenames = filenames.
        //            Select(o => new { Orig = o, File = System.IO.Path.GetFileName(o) }).
        //            Select(o => new { Orig = o.Orig, GUID = o.File.Substring(0, o.File.IndexOf(" - ")) }).
        //            GroupBy(o => o.GUID).
        //            ToArray();

        //        // Choose a random guid set to display (otherwise it's just a mess)
        //        Voronoi2NeighborData[] snapshots = singleSetFilenames[StaticRandom.Next(singleSetFilenames.Length)].
        //            AsParallel().
        //            Select(o => UtilityCore.DeserializeFromFile<Voronoi2NeighborData>(o.Orig)).
        //            OrderBy(o => o.AngleClicked).
        //            ToArray();

        //        PrepFor3D_Full();
        //        ClearTempVisuals();

        //        double offset = 0;

        //        Color neighborColor = Colors.ForestGreen;

        //        foreach (Voronoi2NeighborData snapshot in snapshots)
        //        {
        //            Transform3DGroup transformMain = new Transform3DGroup();
        //            Transform3DGroup transformRay = new Transform3DGroup();

        //            transformMain.Children.Add(new TranslateTransform3D(-snapshot.From.ToVector()));        // from is the centerpoint
        //            transformRay.Children.Add(new TranslateTransform3D(-snapshot.From.ToVector()));

        //            // Orient along the bisect plane (rotate onto xy plane)
        //            DoubleVector fromDbl = new DoubleVector(snapshot.From - snapshot.NeighborMidPoint, snapshot.Points[snapshot.Neighbors[0]] - snapshot.NeighborMidPoint);
        //            DoubleVector toDbl = new DoubleVector(new Vector3D(1, 0, 0), new Vector3D(0, 1, 0));
        //            Quaternion neighborQuat = Math3D.GetRotation(fromDbl, toDbl);
        //            transformMain.Children.Add(new RotateTransform3D(new QuaternionRotation3D(neighborQuat)));
        //            transformRay.Children.Add(new RotateTransform3D(new QuaternionRotation3D(neighborQuat)));

        //            transformRay.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 90)));       // extra rotation to put the rays in the same plane

        //            transformMain.Children.Add(new TranslateTransform3D(0, 0, offset));
        //            transformRay.Children.Add(new TranslateTransform3D(0, 0, offset));

        //            // Planes
        //            //_misc3D.Add(new Item3D(AddPlane(_viewportFull, new Triangle(new Point3D(-1, 0, 0), new Point3D(0, 0, 0), new Point3D(0, 1, 0)), Colors.Silver, Colors.White, new Point3D(0, 0, offset))));

        //            // From
        //            Point3D from = transformMain.Transform(snapshot.From);
        //            _misc3D.Add(new Item3D(AddDot(_viewportFull, from, Colors.White, .1)));

        //            // Neighbors
        //            Point3D neighbor0 = transformMain.Transform(snapshot.Points[snapshot.Neighbors[0]]);
        //            Point3D neighbor1 = transformMain.Transform(snapshot.Points[snapshot.Neighbors[1]]);
        //            _misc3D.Add(new Item3D(AddDots(_viewportFull, new[] { neighbor0, neighbor1 }, neighborColor, .08, true)));
        //            _misc3D.Add(new Item3D(AddLine(_viewportFull, neighbor0, neighbor1, neighborColor)));

        //            _misc3D.Add(new Item3D(AddLine(_viewportFull, from, Math3D.GetCenter(new[] { neighbor0, neighbor1 }), Colors.Chartreuse)));

        //            // Cell Center
        //            Point3D cellCenter = transformMain.Transform(snapshot.CellCenter);
        //            _misc3D.Add(new Item3D(AddDot(_viewportFull, cellCenter, Colors.Black, .06, true)));
        //            _misc3D.Add(new Item3D(AddLine(_viewportFull, from, cellCenter, Colors.Silver)));

        //            // Adjacent lines
        //            _misc3D.Add(new Item3D(AddLines(_viewportFull, new[] { Tuple.Create(0, 1), Tuple.Create(0, 2) }, new[] { transformMain.Transform(snapshot.From), transformMain.Transform(snapshot.AdjacentCircumcenters[0]), transformMain.Transform(snapshot.AdjacentCircumcenters[1]) }, Colors.White)));

        //            // Rays
        //            foreach (var clickRay in snapshot.ClickRays)
        //            {
        //                Point3D ray1 = transformRay.Transform(clickRay.ClickRay.Item1);     //transformMain.Transform(clickRay.ClickRay.Item1)
        //                Point3D ray2 = transformRay.Transform(clickRay.ClickRay.Item2);     //transformMain.Transform(clickRay.ClickRay.Item2)
        //                //_misc3D.Add(new Item3D(AddLine(_viewportFull, ray1, ray2, Colors.DodgerBlue)));
        //                _misc3D.Add(new Item3D(AddLine(_viewportFull, ray1, ray2, Colors.DodgerBlue)));
        //            }

        //            _misc3D.Add(new Item3D(AddLine(_viewportFull, from, Math3D.GetCenter(snapshot.ClickRays.Select(o => transformRay.Transform(o.ClickRay.Item2))), Colors.LightSkyBlue)));

        //            offset += MARGIN;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}
        //private void btn3DAnalyzeSaves2_Click(object sender, RoutedEventArgs e)
        //{
        //    const double MARGIN = HALFSIZE3D / 2;
        //    const double TEXTHEIGHT = .4;

        //    try
        //    {
        //        #region Read Files

        //        //C:\Users\<username>\AppData\Roaming\Asteroid Miner\BrainTester2\
        //        string folder = System.IO.Path.Combine(UtilityCore.GetOptionsFolder(), OPTIONSFOLDER);
        //        if (!Directory.Exists(folder))
        //        {
        //            MessageBox.Show("Saves folder doesn't exist", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
        //            return;
        //        }

        //        string[] filenames = Directory.GetFiles(folder);

        //        if (filenames.Length == 0)
        //        {
        //            MessageBox.Show("There are no saved files", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
        //            return;
        //        }

        //        // The files start with the same guid, so group by that
        //        var singleSetFilenames = filenames.
        //            Select(o => new { Orig = o, File = System.IO.Path.GetFileName(o) }).
        //            Select(o => new { Orig = o.Orig, GUID = o.File.Substring(0, o.File.IndexOf(" - ")) }).
        //            GroupBy(o => o.GUID).
        //            ToArray();

        //        // Choose a random guid set to display (otherwise it's just a mess)
        //        Voronoi2NeighborData[] snapshots = singleSetFilenames[StaticRandom.Next(singleSetFilenames.Length)].
        //            AsParallel().
        //            Select(o => UtilityCore.DeserializeFromFile<Voronoi2NeighborData>(o.Orig)).
        //            OrderBy(o => o.AngleClicked).
        //            ToArray();

        //        #endregion

        //        PrepFor3D_Full();
        //        ClearTempVisuals();

        //        double offset = 0;

        //        Color neighborColor = Colors.ForestGreen;

        //        foreach (Voronoi2NeighborData snapshot in snapshots)
        //        {
        //            #region Transforms

        //            Transform3DGroup transformMain = new Transform3DGroup();
        //            Transform3DGroup transformRay = new Transform3DGroup();

        //            transformMain.Children.Add(new TranslateTransform3D(-snapshot.From.ToVector()));        // from is the centerpoint
        //            transformRay.Children.Add(new TranslateTransform3D(-snapshot.From.ToVector()));

        //            // Orient along the bisect plane (rotate onto xy plane)
        //            DoubleVector fromDbl = new DoubleVector(snapshot.From - snapshot.NeighborMidPoint, snapshot.Points[snapshot.Neighbors[0]] - snapshot.NeighborMidPoint);
        //            DoubleVector toDbl = new DoubleVector(new Vector3D(1, 0, 0), new Vector3D(0, 1, 0));
        //            Quaternion neighborQuat = Math3D.GetRotation(fromDbl, toDbl);
        //            transformMain.Children.Add(new RotateTransform3D(new QuaternionRotation3D(neighborQuat)));
        //            transformRay.Children.Add(new RotateTransform3D(new QuaternionRotation3D(neighborQuat)));

        //            transformRay.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 90)));       // extra rotation to put the rays in the same plane

        //            transformMain.Children.Add(new TranslateTransform3D(0, 0, offset));
        //            transformRay.Children.Add(new TranslateTransform3D(0, 0, offset));

        //            #endregion

        //            // Planes
        //            Triangle plane = new Triangle(new Point3D(-1, 0, 0), new Point3D(0, 0, 0), new Point3D(0, 1, 0));
        //            //_misc3D.Add(new Item3D(AddPlane(_viewportFull, plane, Colors.Silver, Colors.White, new Point3D(0, 0, offset))));

        //            // From
        //            Point3D from = transformMain.Transform(snapshot.From);
        //            _misc3D.Add(new Item3D(AddDot(_viewportFull, from, Colors.White, .1)));

        //            // Neighbors
        //            Point3D neighbor0 = transformMain.Transform(snapshot.Points[snapshot.Neighbors[0]]);
        //            Point3D neighbor1 = transformMain.Transform(snapshot.Points[snapshot.Neighbors[1]]);
        //            _misc3D.Add(new Item3D(AddDots(_viewportFull, new[] { neighbor0, neighbor1 }, neighborColor, .08, true)));
        //            _misc3D.Add(new Item3D(AddLine(_viewportFull, neighbor0, neighbor1, neighborColor)));

        //            Point3D neighborCenter = Math3D.GetCenter(new[] { neighbor0, neighbor1 });

        //            _misc3D.Add(new Item3D(AddLine(_viewportFull, from, neighborCenter, Colors.Chartreuse)));

        //            // Cell Center
        //            Point3D cellCenter = transformMain.Transform(snapshot.CellCenter);
        //            _misc3D.Add(new Item3D(AddDot(_viewportFull, cellCenter, Colors.Black, .06, true)));
        //            _misc3D.Add(new Item3D(AddLine(_viewportFull, from, cellCenter, Colors.Silver)));

        //            // Project cell center onto the plane
        //            Point3D cellCenterProjected = Math3D.GetClosestPoint_Point_Plane(plane, cellCenter);

        //            Vector3D cellCenterProjectedDir = cellCenterProjected - from;
        //            double cellCenterAngle = Vector3D.AngleBetween(cellCenterProjectedDir, neighborCenter - from);

        //            //TODO: Pass in a double vector for orientation
        //            _misc3D.Add(new Item3D(AddText3D(_viewportFull, Math.Round(cellCenterAngle).ToString(), neighborCenter + ((neighborCenter - from).ToUnit() * (TEXTHEIGHT * 1.5)), TEXTHEIGHT, Colors.Chartreuse)));

        //            // Adjacent lines
        //            _misc3D.Add(new Item3D(AddLines(_viewportFull, new[] { Tuple.Create(0, 1), Tuple.Create(0, 2) }, new[] { transformMain.Transform(snapshot.From), transformMain.Transform(snapshot.AdjacentCircumcenters[0]), transformMain.Transform(snapshot.AdjacentCircumcenters[1]) }, Colors.White)));

        //            // Rays
        //            foreach (var clickRay in snapshot.ClickRays)
        //            {
        //                Point3D ray1 = transformRay.Transform(clickRay.ClickRay.Item1);     //transformMain.Transform(clickRay.ClickRay.Item1)
        //                Point3D ray2 = transformRay.Transform(clickRay.ClickRay.Item2);     //transformMain.Transform(clickRay.ClickRay.Item2)
        //                //_misc3D.Add(new Item3D(AddLine(_viewportFull, ray1, ray2, Colors.DodgerBlue)));
        //                _misc3D.Add(new Item3D(AddLine(_viewportFull, ray1, ray2, Colors.DodgerBlue)));
        //            }

        //            Point3D centerRays = Math3D.GetCenter(snapshot.ClickRays.Select(o => transformRay.Transform(o.ClickRay.Item2)));

        //            _misc3D.Add(new Item3D(AddLine(_viewportFull, from, centerRays, Colors.LightSkyBlue)));

        //            double angleCenterRays = Vector3D.AngleBetween(centerRays - from, from - neighborCenter);

        //            //Point3D startPoint = (centerRays.ToVector().ToUnit() * (cellCenterProjectedDir.Length * .5)).ToPoint();
        //            Point3D startPoint = from + ((centerRays - from).ToUnit() * (cellCenterProjectedDir.Length * .5));
        //            _misc3D.Add(new Item3D(AddText3D(_viewportFull, Math.Round(angleCenterRays).ToString(), startPoint + ((from - neighborCenter).ToUnit() * (TEXTHEIGHT * 1.5)), TEXTHEIGHT, Colors.LightSkyBlue)));

        //            offset += MARGIN;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}

        #region PRIVATES

        //#region class: Cell

        //public class Cell : Game.HelperClassesWPF.MIConvexHull.TriangulationCell<Vertex, Cell>
        //{
        //    // These get populated after the voronoi result is returned
        //    public int Index = -1;

        //    public Point3D Circumcenter = new Point3D(0, 0, 0);
        //    public int CircumcenterIndex = -1;
        //}

        //#endregion

        //private static Tuple<Vector3D, Vector3D> Get2Ray(Point3D from, Vertex[] neighbors, Point3D[] adjacentCircumcenters, Viewport3D viewport = null, List<Item3D> visuals = null)
        //{
        //    if (neighbors.Length != 2 || adjacentCircumcenters.Length != 2)
        //    {
        //        throw new ApplicationException("Expected 2 of each");
        //    }

        //    Vector3D adjacent1 = adjacentCircumcenters[0] - from;
        //    Vector3D adjacent2 = adjacentCircumcenters[1] - from;

        //    Point3D neighborMidPoint = Math3D.GetCenter(neighbors.Select(o => o.Point));
        //    Vector3D neighborLine = neighbors[1].Point - neighborMidPoint;

        //    // Get a plane that bisects the neighbors
        //    ITriangle bisectPlane = Math3D.GetPlane(neighborMidPoint, neighborLine);


        //    // Draw a line between the adjacents.  Where they cross the bisect plane defines the second
        //    // point for the rotate axis
        //    Point3D? point = Math3D.GetIntersection_Plane_Line(bisectPlane, adjacentCircumcenters[0], adjacentCircumcenters[1] - adjacentCircumcenters[0]);
        //    if (point == null)
        //    {
        //        throw new ApplicationException("Adjacents should always cross the bisect plane");
        //    }

        //    Vector3D rotateAxis = point.Value - from;

        //    // Put a vector on the bisect plane that they need to rotate onto
        //    Vector3D rotateTo = Vector3D.CrossProduct(neighborLine, rotateAxis);

        //    // Define a plane that the adjacents can project to (so that the angles can be calculated
        //    // in 2D)
        //    ITriangle projectPlane = new Triangle(from + neighborLine, from, from + rotateTo);

        //    Vector3D adjacentProjected1 = adjacent1.GetProjectedVector(projectPlane);
        //    Vector3D adjacentProjected2 = adjacent2.GetProjectedVector(projectPlane);




        //    // Figure out how much to rotate the adjacents to get them onto the bisecting plane
        //    double angle = -Vector3D.AngleBetween(adjacentProjected1, rotateTo);

        //    if (Math.Abs(angle) < 90)
        //    {
        //        if (angle > 0)
        //        {
        //            angle -= 180;
        //        }
        //        else
        //        {
        //            angle += 180;
        //        }
        //    }

        //    // Rotate them
        //    Vector3D dir1 = adjacent1.GetRotatedVector(rotateAxis, angle);
        //    Vector3D dir2 = adjacent2.GetRotatedVector(rotateAxis, angle);




        //    if (viewport != null && visuals != null)
        //    {
        //        visuals.Add(new Item3D(AddLine(viewport, from, from + rotateTo, Colors.Black)));
        //        visuals.Add(new Item3D(AddLine(viewport, from, from + rotateAxis, Colors.Red)));

        //        visuals.Add(new Item3D(AddLine(viewport, from, from + adjacentProjected1, Colors.HotPink)));
        //        visuals.Add(new Item3D(AddLine(viewport, from, from + adjacentProjected2, Colors.LightPink)));

        //        visuals.Add(new Item3D(AddPlane(viewport, projectPlane, Colors.White, Colors.HotPink, from)));
        //    }






        //    //TODO: Rotate again so they are pointing away




        //    return Tuple.Create(dir1, dir2);
        //}
        //private static Tuple<Vector3D, Vector3D> Get2RayFixed(Point3D from, Point3D cellCenter, Vertex[] neighbors, Point3D[] adjacentCircumcenters, Tetrahedron[] delaunay)
        //{
        //    // Find the triangles that have both points from neighbors (should always be two triangles)
        //    var delaunayNeighbors = delaunay.
        //        SelectMany(o => o.FaceArray).
        //        Where(o => o.IndexArray.Contains(neighbors[0].Index) && o.IndexArray.Contains(neighbors[1].Index)).
        //        ToArray();

        //    if (delaunayNeighbors.Length != 2)
        //    {
        //        throw new ApplicationException("Should have found two triangles: " + delaunayNeighbors.Length.ToString());
        //    }

        //    List<Vector3D> retVal = new List<Vector3D>();

        //    foreach (var triangle in delaunayNeighbors)
        //    {
        //        Vector3D normal = triangle.Normal;

        //        // The check below assumes that the normal is along the same direction as toward from
        //        if (Vector3D.DotProduct(normal, from - Math3D.GetClosestPoint_Point_Plane(triangle, from)) < 0)
        //        {
        //            normal = -normal;
        //        }

        //        // See if the line between circumcenter and centroid passes through that plane (see if they are on the same side
        //        // of the plane, or opposing sides)
        //        bool isAbove1 = Math3D.DistanceFromPlane(triangle, from) > 0d;
        //        bool isAbove2 = Math3D.DistanceFromPlane(triangle, cellCenter) > 0d;

        //        if (isAbove1 != isAbove2)
        //        {
        //            normal = -normal;
        //        }

        //        retVal.Add(normal);
        //    }

        //    //return Tuple.Create(delaunayNeighbors[0].Normal, delaunayNeighbors[1].Normal);
        //    return Tuple.Create(retVal[0], retVal[1]);
        //}

        //#region OLD

        /////// <summary>
        /////// This returns the center point of the circumsphere
        /////// </summary>
        /////// <remarks>
        /////// http://jwilson.coe.uga.edu/EMAT6680Su11/Huckaby/3DGEO/Circumsphere/Circumsphere.html
        /////// 
        /////// In 2D it's the intersection of lines that bisect the edges (any two lines are enough)
        /////// In 3D is the intersection of planes that bisect the edges (any three planes are enough)
        /////// 
        /////// Bisecting Planes:
        /////// Normal is edge vector
        /////// One point is edge midpoint
        /////// Other point is another vertex of tetra
        /////// 
        /////// Get intersection line of two planes
        /////// Get intersection point of line and a third plane
        /////// </remarks>
        ////private static Point3D GetCircumCenter(Tetrahedron tetra)
        ////{
        ////    // Get bisecting planes for 3 of the sides (only need 3 of the 4 - the fourth would just be redundant)
        ////    ITriangle plane0 = GetBisectingPlane(tetra.Face_012);
        ////    ITriangle plane1 = GetBisectingPlane(tetra.Face_023);
        ////    ITriangle plane2 = GetBisectingPlane(tetra.Face_132);

        ////    // Intersect two of the planes
        ////    Point3D lineAt;
        ////    Vector3D lineDir;
        ////    if (!Math3D.GetIntersection_Plane_Plane(out lineAt, out lineDir, plane0, plane1))
        ////    {
        ////        throw new ApplicationException("The two planes don't intersect.  This should never happen for a tetrahedron: " + tetra.ToString());
        ////    }

        ////    // Intersect the third plane
        ////    Point3D? retVal = Math3D.GetIntersection_Plane_Line(plane2, lineAt, lineDir);
        ////    if (retVal == null)
        ////    {
        ////        throw new ApplicationException(string.Format("The plane should have intersected the line: (plane={0}) (lineAt={1}) (lineDir={2}", plane2.ToString(), lineAt.ToString(true), lineDir.ToString(true)));
        ////    }

        ////    return retVal.Value;
        ////}
        ////private static ITriangle GetBisectingPlane(ITriangle triangle)
        ////{
        ////    // 01 will be the edge that defines the plane's normal
        ////    Point3D mid = triangle.GetEdgeMidpoint(TriangleEdge.Edge_01);

        ////    Vector3D dir1 = triangle.Point2 - mid;
        ////    Vector3D normal = triangle.Point1 - mid;
        ////    Vector3D dir2 = Vector3D.CrossProduct(dir1, normal);

        ////    return new Triangle(mid, mid + dir2, triangle.Point2);
        ////}

        //#endregion

        ////TODO: Move this to Math3D
        //#region class: Vertex

        //public class Vertex : Game.HelperClassesWPF.MIConvexHull.IVertex
        //{
        //    public Vertex(int index, Point3D point)
        //    {
        //        this.Index = index;
        //        this.Point = point;

        //        this.Position = new[] { point.X, point.Y, point.Z };
        //    }

        //    public readonly int Index;
        //    public readonly Point3D Point;

        //    // This is what the interface expects
        //    public double[] Position { get; set; }
        //}

        //#endregion

        //private static Tetrahedron[] GetDelaunay(Point3D[] points)
        //{
        //    SortedList<Tuple<int, int, int>, TriangleIndexedLinked> triangleDict = new SortedList<Tuple<int, int, int>, TriangleIndexedLinked>();

        //    if (points == null)
        //    {
        //        throw new ArgumentNullException("points");
        //    }
        //    else if (points.Length < 4)
        //    {
        //        throw new ArgumentException("Must have at least 4 points: " + points.Length.ToString());
        //    }
        //    else if (points.Length == 4)
        //    {
        //        // The MIConvexHull library needs at least 5 points.  Just manually create a tetrahedron
        //        return new[] { new Tetrahedron(0, 1, 2, 3, points, triangleDict) };
        //    }

        //    // Convert the points into something the MIConvexHull library can use
        //    Vertex[] vertices = points.Select((o, i) => new Vertex(i, o)).ToArray();

        //    // Run the delaunay
        //    var tetrahedrons1 = Game.HelperClassesWPF.MIConvexHull.Triangulation.CreateDelaunay<Vertex>(vertices).Cells.ToArray();

        //    // Convert into my classes
        //    Tetrahedron[] tetrahedrons2 = tetrahedrons1.Select(o => new Tetrahedron(o.Vertices[0].Index, o.Vertices[1].Index, o.Vertices[2].Index, o.Vertices[3].Index, points, triangleDict)).ToArray();

        //    // Link them
        //    for (int cntr = 0; cntr < tetrahedrons1.Length; cntr++)
        //    {
        //        foreach (var neighbor1 in tetrahedrons1[cntr].Adjacency)
        //        {
        //            if (neighbor1 == null)
        //            {
        //                // This is on the edge of the hull (all of tetrahedrons2 neighbors start out as null, so it's the non nulls that
        //                // need something done to them)
        //                continue;
        //            }

        //            int[] indecies = neighbor1.Vertices.Select(o => o.Index).OrderBy(o => o).ToArray();

        //            // Find the tetra2
        //            Tetrahedron neighbor2 = tetrahedrons2.First(o => o.IsMatch(indecies));

        //            tetrahedrons2[cntr].SetNeighbor(neighbor2);
        //        }
        //    }

        //    return tetrahedrons2;
        //}

        #endregion
        #region CLASSES

        //#region class: Voronoi2NeighborData

        //public class Voronoi2NeighborData
        //{
        //    public Guid PointsID { get; set; }

        //    public Point3D[] Points { get; set; }
        //    public Voronoi2NeighborDataExtra[] Vertices { get; set; }
        //    //public HelperClassesWPF.MIConvexHull.VoronoiMesh<BrainTester2.Vertex, BrainTester2.Cell> VoronoiMesh { get; set; }

        //    //public BrainTester2.Cell Cell { get; set; }
        //    public Voronoi2NeighborDataExtra[] CellVertices { get; set; }
        //    public Point3D From { get; set; }       // this is the cell.Circumcenter
        //    public Point3D CellCenter { get; set; }
        //    public int CellIndex { get; set; }
        //    public Voronoi2NeighborDataExtra[] CellAdjacentCells { get; set; }       // Item1=CellIndex, Item2=CellCircumcenter, Item3=CellCenter

        //    //public BrainTester2.Vertex[] Neighbors { get; set; }
        //    public int[] Neighbors { get; set; }
        //    public Point3D NeighborCenter { get; set; }
        //    public Point3D NeighborMidPoint { get; set; }      // this is probably the same as NeighborCenter
        //    public ITriangle NeighborBisectPlane { get; set; }

        //    public Point3D[] AdjacentCircumcenters { get; set; }

        //    public Voronoi2NeighborDataExtra[] ClickRays { get; set; }

        //    public double AngleClicked { get; set; }
        //    public double AngleAdjacent { get; set; }
        //}

        //#endregion
        //#region class: Voronoi2NeighborDataExtra

        //public class Voronoi2NeighborDataExtra
        //{
        //    public static Voronoi2NeighborDataExtra CreateVertex(int index, Point3D point)
        //    {
        //        return new Voronoi2NeighborDataExtra()
        //        {
        //            Int = index,
        //            Point1 = point,
        //        };
        //    }
        //    public static Voronoi2NeighborDataExtra CreateAdjacentCell(int index, Point3D circumcenter, Point3D center)
        //    {
        //        return new Voronoi2NeighborDataExtra()
        //        {
        //            Int = index,
        //            Point1 = circumcenter,
        //            Point2 = center,
        //        };
        //    }
        //    public static Voronoi2NeighborDataExtra CreateClickRay(Point3D from, Point3D to)
        //    {
        //        return new Voronoi2NeighborDataExtra()
        //        {
        //            Point1 = from,
        //            Point2 = to,
        //        };
        //    }

        //    public int? Int { get; set; }
        //    public Point3D? Point1 { get; set; }
        //    public Point3D? Point2 { get; set; }

        //    public Tuple<int, Point3D> Vertex
        //    {
        //        get
        //        {
        //            return Tuple.Create(this.Int.Value, this.Point1.Value);
        //        }
        //    }
        //    public Tuple<int, Point3D, Point3D> CellAdjacentCell
        //    {
        //        get
        //        {
        //            return Tuple.Create(this.Int.Value, this.Point1.Value, this.Point2.Value);
        //        }
        //    }
        //    public Tuple<Point3D, Point3D> ClickRay
        //    {
        //        get
        //        {
        //            return Tuple.Create(this.Point1.Value, this.Point2.Value);
        //        }
        //    }
        //}

        //#endregion

        #endregion

        #endregion

        #endregion

        #region Private Methods

        //TODO: Clear existing visuals
        private void PrepFor2D(bool clearExisting = false)
        {
            pnlViewport.Visibility = Visibility.Collapsed;
            pnlViewportNeural.Visibility = Visibility.Collapsed;
            pnlViewportFull.Visibility = Visibility.Collapsed;

            canvas.Visibility = Visibility.Visible;
        }
        private void PrepFor3D(bool clearExisting = false)
        {
            canvas.Visibility = Visibility.Collapsed;

            pnlViewport.Visibility = Visibility.Collapsed;
            pnlViewportNeural.Visibility = Visibility.Collapsed;

            pnlViewportFull.Visibility = Visibility.Visible;
        }

        private void ClearAllVisuals()
        {
            ClearTempVisuals();
            ClearClickRayData();

            // 2D
            foreach (var list in new[] { _inputs2D, _brains2D, _outputs2D })
            {
                canvas.Children.RemoveAll(list.Select(o => o.Visual));
                list.Clear();
            }

            // 3D
            _viewportFull.Children.RemoveAll(_dots3D.Select(o => o.Visual));
            _dots3D.Clear();
        }
        private void ClearTempVisuals()
        {
            _voronoi2D = null;

            // 2D
            canvas.Children.RemoveAll(_voronoiLines2D.Select(o => o.Visual));
            _voronoiLines2D.Clear();

            canvas.Children.RemoveAll(_linksBrain2D.Select(o => o.Visual));
            _linksBrain2D.Clear();

            canvas.Children.RemoveAll(_linksIO2D.Select(o => o.Visual));
            _linksIO2D.Clear();

            canvas.Children.RemoveAll(_misc2D.Select(o => o.Visual));
            _misc2D.Clear();

            // 3D
            _viewportFull.Children.RemoveAll(_linksBrain3D.Select(o => o.Visual));
            _linksBrain3D.Clear();

            _viewportFull.Children.RemoveAll(_linksIO3D.Select(o => o.Visual));
            _linksIO3D.Clear();

            _viewportFull.Children.RemoveAll(_misc3D.Select(o => o.Visual));
            _misc3D.Clear();

            _viewportFull.Children.RemoveAll(_voronoiBlob3D.Select(o => o.Visual));
            _voronoiBlob3D.Clear();

            // Stats
            pnlStats.Children.Clear();

            //_voronoi2NeighborData.Clear();
            //btn3DVor2D2Save.IsEnabled = false;
        }
        private void ClearClickRayData()
        {
            _voronoi2NeighborRayStarts.Clear();
            _voronoiResult = null;

            _viewportFull.Children.RemoveAll(_clickRays3D.Select(o => o.Visual));
            _clickRays3D.Clear();
        }

        private Item2D[] DrawVoronoi(VoronoiResult2D voronoi, Brush brush, double width = 2)
        {
            List<Item2D> retVal = new List<Item2D>();

            // This is used to extend rays, ensure they always go off screen
            double extensionLength = Math.Max(canvas.ActualWidth, canvas.ActualHeight) * 2;

            foreach (Edge2D edge in voronoi.Edges)
            {
                Point from = edge.Point0;
                Point to = edge.GetPoint1Ext(extensionLength);      // if it's a ray, this method will extend it into a segment

                if (edge.EdgeType == EdgeType.Line)
                {
                    // Instead of a ray, make a line
                    from = from + (from - to);
                }

                UIElement visual = AddLine(from, to, brush, width);
                retVal.Add(new Item2D(visual, 0, from, to));
            }

            return retVal.ToArray();
        }
        private Item2D DrawBrainGroup(Point[] points, Point center)
        {
            Canvas group = new Canvas();

            foreach (Point point in points)
            {
                Line line = new Line()
                {
                    X1 = center.X,
                    Y1 = center.Y,
                    X2 = point.X,
                    Y2 = point.Y,
                    Stroke = _brainGroupBrush,
                    StrokeThickness = 10,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                };

                group.Children.Add(line);
            }

            canvas.Children.Insert(0, group);       // always putting it under

            #region FAIL

            // Polygon doesn't work (usually it's only two points, and if there's more than three, should call Math2D.GetConvexHull)

            //Polygon poly = new Polygon()
            //{
            //    Fill = _brainGroupFillBrush,
            //    Stroke = _brainGroupStrokeBrush,
            //    StrokeThickness = 24,
            //};

            //foreach (Point point in points)
            //{
            //    poly.Points.Add(point);
            //}

            //canvas.Children.Insert(0, poly);        // this always gets drawn under the brains

            #endregion

            return new Item2D(group, 0, center);
        }
        private static Item3D DrawBrainGroup(Viewport3D viewport, Point3D[] points, Point3D center, Color color)
        {
            var lines = points.
                Select(o => Tuple.Create(o, center));

            Visual3D visual = AddLines(viewport, lines, color, .09);

            return new Item3D(visual, 0, center);
        }
        private static Item3D[] DrawBrainIOLinks3D(Viewport3D viewport, Tuple<int, int>[] links, Item3D[] items1, Item3D[] items2, Color defaultColor, bool isRainbow)
        {
            const double THICKNESS = .025;

            if (!isRainbow)
            {
                var lines = links.Select(o => Tuple.Create(items1[o.Item1].Position.Value, items2[o.Item2].Position.Value));

                return new[]
                {
                    new Item3D(AddLines(viewport, lines, defaultColor, THICKNESS)),
                };
            }

            List<Item3D> retVal = new List<Item3D>();

            foreach (var byItem1 in links.ToLookup(o => o.Item1))
            {
                //Color color = UtilityWPF.GetRandomColor(150, 220);
                Color color = new ColorHSV(StaticRandom.NextDouble(360), 60, 100).ToRGB();

                var lines = byItem1.Select(o => Tuple.Create(items1[byItem1.Key].Position.Value, items2[o.Item2].Position.Value));

                retVal.Add(new Item3D(AddLines(viewport, lines, color, THICKNESS)));
            }

            return retVal.ToArray();
        }

        private UIElement AddDot(Point position, Brush fill, Brush stroke, double size = 20)
        {
            Ellipse dot = new Ellipse()
            {
                Fill = fill,
                Stroke = stroke,
                StrokeThickness = 1,
                Width = size,
                Height = size
            };

            double halfSize = size / 2d;

            Canvas.SetLeft(dot, position.X - halfSize);
            Canvas.SetTop(dot, position.Y - halfSize);

            canvas.Children.Add(dot);

            return dot;
        }
        private UIElement AddLine(Point from, Point to, Brush brush, double width = 2, bool isUnder = false)
        {
            Line line = new Line()
            {
                X1 = from.X,
                Y1 = from.Y,
                X2 = to.X,
                Y2 = to.Y,
                Stroke = brush,
                StrokeThickness = width
            };

            if (isUnder)
            {
                canvas.Children.Insert(0, line);
            }
            else
            {
                canvas.Children.Add(line);
            }

            return line;
        }
        private UIElement AddSquare(Point min, Point max, Brush brush)
        {
            Rectangle rect = new Rectangle()
            {
                Width = max.X - min.X,
                Height = max.Y - min.Y,
                Fill = brush,
            };

            Canvas.SetLeft(rect, min.X);
            Canvas.SetTop(rect, min.Y);

            canvas.Children.Add(rect);
            return rect;
        }

        private static Visual3D AddDot(Viewport3D viewport, Point3D position, Color color, double radius = .03, bool isHiRes = false)
        {
            // Material
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
            materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(Colors.White, color, .3d)), 30d));

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;
            geometry.Geometry = UtilityWPF.GetSphere_Ico(radius, isHiRes ? 4 : 0, true);

            // Model Visual
            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = geometry;
            visual.Transform = new TranslateTransform3D(position.ToVector());

            viewport.Children.Add(visual);
            return visual;
        }
        private static Visual3D AddDots(Viewport3D viewport, IEnumerable<Point3D> positions, Color color, double radius = .03, bool isHiRes = false)
        {
            // Material
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
            materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(Colors.White, color, .3d)), 30d));

            Model3DGroup geometries = new Model3DGroup();

            foreach (Point3D position in positions)
            {
                // Geometry Model
                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = materials;
                geometry.BackMaterial = materials;
                geometry.Geometry = UtilityWPF.GetSphere_Ico(radius, isHiRes ? 4 : 0, true);
                geometry.Transform = new TranslateTransform3D(position.ToVector());

                geometries.Children.Add(geometry);
            }

            // Model Visual
            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = geometries;

            viewport.Children.Add(visual);
            return visual;
        }
        private static Visual3D AddLine(Viewport3D viewport, Point3D from, Point3D to, Color color, double thickness = .015)
        {
            return AddLines(viewport, new[] { Tuple.Create(0, 1) }, new[] { from, to }, color, thickness);
        }
        private static Visual3D AddLines(Viewport3D viewport, IEnumerable<Tuple<int, int>> lines, Point3D[] allPoints, Color color, double thickness = .015)
        {
            BillboardLine3DSet retVal = new BillboardLine3DSet();
            retVal.Color = color;
            retVal.BeginAddingLines();

            foreach (Tuple<int, int> line in lines)
            {
                retVal.AddLine(allPoints[line.Item1], allPoints[line.Item2], thickness);
            }

            retVal.EndAddingLines();

            viewport.Children.Add(retVal);
            return retVal;
        }
        private static Visual3D AddLines(Viewport3D viewport, IEnumerable<Tuple<Point3D, Point3D>> lines, Color color, double thickness = .015)
        {
            BillboardLine3DSet retVal = new BillboardLine3DSet();
            retVal.Color = color;
            retVal.BeginAddingLines();

            foreach (Tuple<Point3D, Point3D> line in lines)
            {
                retVal.AddLine(line.Item1, line.Item2, thickness);
            }

            retVal.EndAddingLines();

            viewport.Children.Add(retVal);
            return retVal;
        }
        private static Visual3D AddPlane(Viewport3D viewport, ITriangle plane, Color fillColor, Color reflectiveColor, Point3D? center = null, double size = HALFSIZE3D * 2)
        {
            // Model Visual
            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = UtilityWPF.GetPlane(plane, size, fillColor, reflectiveColor, center: center);

            viewport.Children.Add(visual);
            return visual;
        }
        private static Visual3D AddTriangles(Viewport3D viewport, IEnumerable<ITriangle> triangles, Color color, Color? reflectColor = null, bool isSoft = false)
        {
            // Material
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));

            if (reflectColor == null)
            {
                materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(color, Colors.White, .5d)), 50d));
            }
            else
            {
                materials.Children.Add(new SpecularMaterial(new SolidColorBrush(reflectColor.Value), 5d));
            }

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;
            if (isSoft && triangles.First() is ITriangleIndexed)
            {
                geometry.Geometry = UtilityWPF.GetMeshFromTriangles(triangles.Select(o => (ITriangleIndexed)o));
            }
            else
            {
                geometry.Geometry = UtilityWPF.GetMeshFromTriangles_IndependentFaces(triangles);
            }

            // Model Visual
            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = geometry;

            viewport.Children.Add(visual);
            return visual;
        }
        private static Visual3D AddText3D(Viewport3D viewport, string text, Point3D center, double height, Color color)
        {
            MaterialGroup faceMaterial = new MaterialGroup();
            faceMaterial.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
            faceMaterial.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("60FFFFFF")), 5d));

            MaterialGroup edgeMaterial = new MaterialGroup();
            edgeMaterial.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.OppositeColor(color))));
            edgeMaterial.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("80808080")), 1d));

            double edgeDepth = height / 15;

            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = UtilityWPF.GetText3D(text, _font.Value, faceMaterial, edgeMaterial, height, edgeDepth);
            visual.Transform = new TranslateTransform3D(center.ToVector());

            viewport.Children.Add(visual);

            return visual;
        }
        private static Visual3D AddFace(Viewport3D viewport, Face3D face, Color color, Color? reflectColor = null)
        {
            Tuple<int[], Point3D[]> poly = face.GetPolygon(500);

            TriangleIndexed[] triangles = Math2D.GetTrianglesFromConvexPoly(poly.Item1, poly.Item2);

            return AddTriangles(viewport, triangles, color, reflectColor);
        }

        private static AddItemLocation GetItemLocation(ComboBox combo)
        {
            if (combo.SelectedItem == null)
            {
                throw new ApplicationException("combo.SelectedItem is null");
            }

            return (AddItemLocation)Enum.Parse(typeof(AddItemLocation), combo.SelectedItem.ToString());
        }
        private static ClearWhich GetClearWhich(ComboBox combo)
        {
            if (combo.SelectedItem == null)
            {
                throw new ApplicationException("combo.SelectedItem is null");
            }

            return (ClearWhich)Enum.Parse(typeof(ClearWhich), combo.SelectedItem.ToString());
        }
        private static Worker2D_Distance.IOLinkupPriority GetIOLinkupPriority(ComboBox combo)
        {
            if (combo.SelectedItem == null)
            {
                throw new ApplicationException("combo.SelectedItem is null");
            }

            return (Worker2D_Distance.IOLinkupPriority)Enum.Parse(typeof(Worker2D_Distance.IOLinkupPriority), combo.SelectedItem.ToString());
        }

        private static double GetItemSize(bool isRandom)
        {
            if (isRandom)
            {
                return StaticRandom.NextDouble(1, 25);
            }
            else
            {
                return 10d;
            }
        }
        private static double GetDotRadius2D(double size)
        {
            return UtilityCore.GetScaledValue(5, 50, 1, 25, size);
        }
        private static double GetDotRadius3D(double size)
        {
            return UtilityCore.GetScaledValue(.08, .4, 1, 25, size);
        }

        private Point GetRandomPosition2D(AddItemLocation location)
        {
            Rect3D bounds = new Rect3D(0, 0, 0, canvas.ActualWidth, canvas.ActualHeight, 0);

            return GetRandomPosition(location, bounds).ToPoint2D();
        }
        private Point3D GetRandomPosition3D(AddItemLocation location)
        {
            Rect3D bounds;

            switch (location)
            {
                case AddItemLocation.Inner:
                case AddItemLocation.Outer:
                    bounds = new Rect3D(-HALFSIZE3D, -HALFSIZE3D, -HALFSIZE3D, HALFSIZE3D * 2, HALFSIZE3D * 2, HALFSIZE3D * 2);
                    break;

                default:
                    double x = HALFSIZE3D * 1.5;
                    bounds = new Rect3D(-x, -HALFSIZE3D, -HALFSIZE3D, x * 2, HALFSIZE3D * 2, HALFSIZE3D * 2);
                    break;
            }

            return GetRandomPosition(location, bounds);
        }

        private static Point3D GetRandomPosition(AddItemLocation location, Rect3D bounds)
        {
            double bx = bounds.X;
            double by = bounds.Y;
            double bz = bounds.Z;

            double width = bounds.SizeX;
            double height = bounds.SizeY;
            double depth = bounds.SizeZ;

            double w3 = width / 3;      // thirdWidth
            double h3 = height / 3;        // thirdHeight
            double d3 = depth / 3;      // thirdDepth

            double m;       // margin
            double mZ;
            if (depth.IsNearZero())
            {
                m = Math.Min(w3, height) * .05;
                mZ = 0;
            }
            else
            {
                m = Math1D.Min(w3, height, d3) * .05;      // margin
                mZ = m;
            }
            double m2 = m * 2;
            double mZ2 = mZ * 2;

            Rect3D leftRect = new Rect3D(
                bx + m, by + m, bz + mZ,
                w3 - m2, height - m2, depth - mZ2);

            Rect3D centerRect = new Rect3D(
                bx + w3 + m, by + m, bz + mZ,
                w3 - m2, height - m2, depth - mZ2);

            Rect3D rightRect = new Rect3D(
                bx + (w3 * 2) + m, by + m, bz + mZ,
                w3 - m2, height - m2, depth - mZ2);

            Rect3D innerRect1 = new Rect3D(
                bx + w3 + m, by + h3 + m, bz + d3 + mZ,
                w3 - m2, h3 - m2, d3 - mZ2);        // items go into this one

            Rect3D innerRect2 = new Rect3D(
                bx + w3 - m, by + h3 - m, bz + d3 - mZ,
                w3 + m2, h3 + m2, d3 + mZ2);        // this is the hole for outer

            Rect3D outerRect = new Rect3D(
                bx + m, by + m, bz + mZ,
                width - m2, height - m2, depth - mZ2);

            Rect3D rect;

            switch (location)
            {
                case AddItemLocation.Anywhere:
                    rect = outerRect;
                    break;

                case AddItemLocation.Left:
                    rect = leftRect;
                    break;

                case AddItemLocation.Center:
                    rect = centerRect;
                    break;

                case AddItemLocation.Right:
                    rect = rightRect;
                    break;

                case AddItemLocation.Inner:
                    rect = innerRect1;
                    break;

                case AddItemLocation.Outer:
                    return Math3D.GetRandomVector(
                        new Vector3D(outerRect.X, outerRect.Y, outerRect.Z),
                        new Vector3D(outerRect.X + outerRect.SizeX, outerRect.Y + outerRect.SizeY, outerRect.Z + outerRect.SizeZ),
                        new Vector3D(innerRect2.X, innerRect2.Y, innerRect2.Z),
                        new Vector3D(innerRect2.X + innerRect2.SizeX, innerRect2.Y + innerRect2.SizeY, innerRect2.Z + innerRect2.SizeZ)
                        ).ToPoint();

                default:
                    throw new ApplicationException("Unknown AddItemLocation: " + location.ToString());
            }

            return Math3D.GetRandomVector(new Vector3D(rect.X, rect.Y, rect.Z), new Vector3D(rect.X + rect.SizeX, rect.Y + rect.SizeY, rect.Z + rect.SizeZ)).ToPoint();
        }

        #endregion

        #region OLD

        #region class: Set3D

        //private class Set3D
        //{
        //    #region Constructor

        //    public Set3D(int index, Item3D[] all)
        //        : this(new[] { index }, all) { }

        //    public Set3D(IEnumerable<int> indices, Item3D[] all)
        //    {
        //        if (all.Any(o => o.Position == null))
        //        {
        //            throw new ArgumentException("Only items with a single position are allowed");
        //        }

        //        this.Indices = indices.ToArray();
        //        this.Items = this.Indices.Select(o => all[o]).ToArray();

        //        this.Positions = this.Items.Select(o => o.Position.Value).ToArray();

        //        if (this.Items.Length == 1)
        //        {
        //            this.Center = this.Items[0].Position.Value;
        //        }
        //        else
        //        {
        //            this.Center = Math3D.GetCenter(this.Positions);
        //        }
        //    }

        //    #endregion

        //    public readonly int[] Indices;
        //    public readonly Item3D[] Items;

        //    public readonly Point3D Center;
        //    public readonly Point3D[] Positions;
        //}

        #endregion
        #region class: LinkSet3D

        //private class LinkSet3D
        //{
        //    #region Constructor

        //    public LinkSet3D(int item1, int item2, Item3D[] all)
        //        : this(new Set3D(item1, all), new Set3D(item2, all)) { }

        //    public LinkSet3D(IEnumerable<int> set1, IEnumerable<int> set2, Item3D[] all)
        //        : this(new Set3D(set1, all), new Set3D(set2, all)) { }

        //    public LinkSet3D(Set3D set1, Set3D set2)
        //    {
        //        this.Set1 = set1;
        //        this.Set2 = set2;
        //    }

        //    #endregion

        //    public readonly Set3D Set1;
        //    public readonly Set3D Set2;

        //    //TODO: May want to store AllItems[]
        //}

        #endregion

        #region class: CombineItemsArgs

        //private class CombineItemsArgs
        //{
        //    public double Ratio_Skinny = .3;
        //    public double Ratio_Wide = .92;

        //    public double MergeChance = .5;
        //}

        #endregion
        #region class: ItemOverflowArgs

        //private class ItemOverflowArgs
        //{
        //    /// <summary>
        //    /// This is multiplied by the distance between the items being linked.  The larger the value, the more the distance
        //    /// acts as a resistance.  If you use zero, the links will be distributed by burden, ignoring distance.
        //    /// </summary>
        //    public double LinkResistanceMult = 1;
        //}

        #endregion
        #region class: ExtraLinkArgs

        //private class ExtraLinkArgs
        //{
        //    /// <summary>
        //    /// If .5, then an extra 50% links will be added
        //    /// </summary>
        //    public double Percent = .5;

        //    /// <summary>
        //    /// True: The number of extra links is calculated by size and count
        //    /// False: Item size is ignored.  The number of extra links is calculated by count only
        //    /// </summary>
        //    public bool BySize = true;

        //    /// <summary>
        //    /// This only has an effect when percent is over 1 ( > 100%)
        //    /// 
        //    /// True: The list of item2s is mapped fully for each amount over 100%
        //    ///     so if Percent=3.5, then the item2 list will be mapped 3 full times, then randomly map half
        //    ///     
        //    /// False: The number of additions is calculated, then a random item is selected that many times
        //    ///     so there's a good chance that some items will get mapped more than average, some may not get any extra links
        //    /// </summary>
        //    public bool EvenlyDistribute = true;

        //}

        #endregion

        #region class: Worker3D

        //private static class Worker3D
        //{
        //    #region class: DistributeDistances

        //    private class DistributeDistances
        //    {
        //        public DistributeDistances(Tuple<int, double>[][] resistancesItem1, Distances2to1[] distances2to1)
        //        {
        //            this.ResistancesItem1 = resistancesItem1;
        //            this.Distances2to1 = distances2to1;
        //        }

        //        /// <summary>
        //        /// This holds links between all Item1s and the resistance (distance * mult)
        //        /// </summary>
        //        /// <remarks>
        //        /// Index into array is index of item1
        //        /// Tuple.Item1: Index of other item1
        //        /// Tuple.Item2: Resistance felt between the two
        //        /// </remarks>
        //        public readonly Tuple<int, double>[][] ResistancesItem1;

        //        //NOTE: These are ordered by distance
        //        public readonly Distances2to1[] Distances2to1;
        //    }

        //    #endregion
        //    #region class: Distances2to1

        //    /// <summary>
        //    /// This holds an item2, and distances to all item1s
        //    /// </summary>
        //    private class Distances2to1
        //    {
        //        public Distances2to1(int index2, Tuple<int, double>[] distancesTo1)
        //        {
        //            this.Index2 = index2;
        //            this.DistancesTo1 = distancesTo1;
        //        }

        //        public readonly int Index2;

        //        //TODO: No need to store the entire array.  Just the closest one
        //        /// <summary>
        //        /// Item1: index into items1
        //        /// Item2: distance to item1
        //        /// NOTE: These are sorted lowest to highest distance
        //        /// </summary>
        //        public readonly Tuple<int, double>[] DistancesTo1;
        //    }

        //    #endregion
        //    #region class: BrainBurden

        //    private class BrainBurden
        //    {
        //        #region Constructor

        //        public BrainBurden(int index, Item3D[] brains, Item3D[] io)
        //        {
        //            this.Index = index;
        //            this.Brain = brains[index];
        //            this.Size = this.Brain.Size;

        //            _io = io;
        //        }

        //        #endregion

        //        public readonly int Index;
        //        public readonly Item3D Brain;
        //        public readonly double Size;

        //        private readonly Item3D[] _io;

        //        private readonly List<int> _ioLinks = new List<int>();
        //        public IEnumerable<int> IOLinks { get { return _ioLinks; } }

        //        public double IOSize { get; private set; }

        //        public void AddIOLink(int index)
        //        {
        //            _ioLinks.Add(index);
        //            this.IOSize += _io[index].Size;
        //        }

        //        public static double CalculateBurden(double sumLinkSize, double brainSize)
        //        {
        //            return sumLinkSize / brainSize;
        //        }
        //    }

        //    #endregion

        //    /// <summary>
        //    /// Link brains to each other (delaunay graph, then prune thin triangles)
        //    /// </summary>
        //    /// <param name="all">
        //    /// Item1=Link between two items (sub item1 and 2 are the indices)
        //    /// Item2=Distance between those two items
        //    /// </param>
        //    /// <param name="final">
        //    /// This holds a set of links after the thin triangles are pruned.  There's a chance of items being merged
        //    /// </param>
        //    public static void Link_Self(out SortedList<Tuple<int, int>, double> all, out LinkSet3D[] final, Item3D[] items, CombineItemsArgs combineArgs = null)
        //    {
        //        if (items.Any(o => o.Position == null))
        //        {
        //            throw new ArgumentException("All brains passed in must have a single position");
        //        }

        //        TriangleIndexed[] triangles;
        //        GetItemDelaunay(out all, out triangles, items);

        //        if (items.Length < 2)
        //        {
        //            //throw new ArgumentException("This method requires at least two brains: " + items.Length.ToString());
        //            final = new LinkSet3D[0];
        //            return;
        //        }

        //        // Prune links that don't make sense
        //        if (combineArgs != null && triangles.Length > 0)
        //        {
        //            final = PruneLinks(triangles, all, items, combineArgs);
        //        }
        //        else
        //        {
        //            final = all.Keys.
        //                Select(o => new LinkSet3D(o.Item1, o.Item2, items)).
        //                ToArray();
        //        }
        //    }

        //    /// <summary>
        //    /// Every item in 2 will have at least one link to a 1.  There could be some item1s that don't have a link
        //    /// </summary>
        //    /// <param name="overflowArgs">
        //    /// Item2s get matched to the nearest Item1.
        //    /// If this args is null then, it stays that way.
        //    /// If this is populated, then links will move from burdened item1s to less burdened
        //    /// </param>
        //    /// <returns>
        //    /// Item1=index into item1 list
        //    /// Item2=index into item2 list
        //    /// </returns>
        //    public static Tuple<int, int>[] Link_1_2(Item3D[] items1, Item3D[] items2, ItemOverflowArgs overflowArgs = null, ExtraLinkArgs extraArgs = null)
        //    {
        //        if (items1 == null || items2 == null || items1.Length == 0 || items2.Length == 0)
        //        {
        //            return new Tuple<int, int>[0];
        //        }
        //        else if (items1.Concat(items2).Any(o => o.Position == null))
        //        {
        //            throw new ArgumentException("All items passed in must have a single position");
        //        }

        //        Tuple<int, int>[] retVal = null;

        //        if (overflowArgs == null)
        //        {
        //            // Just link to the closest
        //            retVal = Link12_Closest(items1, items2);
        //        }

        //        if (overflowArgs == null && extraArgs == null)
        //        {
        //            // Nothing special to do, exit early
        //            return retVal;
        //        }

        //        DistributeDistances distances = GetDistances(items1, items2, overflowArgs);

        //        if (overflowArgs != null)
        //        {
        //            // Consider item1s burden when linking them
        //            retVal = Link12_Distribute(items1, items2, distances);
        //        }

        //        if (extraArgs != null)
        //        {
        //            // Add more links
        //            retVal = Link12_Extra(retVal, items1, items2, distances, extraArgs);
        //        }

        //        return retVal;
        //    }

        //    #region Private Methods - self

        //    private static LinkSet3D[] PruneLinks(TriangleIndexed[] triangles, SortedList<Tuple<int, int>, double> all, Item3D[] brains, CombineItemsArgs args)
        //    {
        //        List<Tuple<int[], int[]>> retVal = all.Keys.
        //            Select(o => Tuple.Create(new[] { o.Item1 }, new[] { o.Item2 })).
        //            ToList();

        //        foreach (TriangleIndexed triangle in triangles)
        //        {
        //            Tuple<bool, TriangleEdge> changeEdge = PruneLinks_LongThin(triangle, all, args);
        //            if (changeEdge == null)
        //            {
        //                continue;
        //            }

        //            if (changeEdge.Item1)
        //            {
        //                PruneLinks_Merge(triangle, changeEdge.Item2, retVal);
        //            }
        //            else
        //            {
        //                PruneLinks_Remove(triangle, changeEdge.Item2, retVal);
        //            }
        //        }

        //        return retVal.Select(o => new LinkSet3D(o.Item1, o.Item2, brains)).ToArray();
        //    }

        //    /// <summary>
        //    /// If this triangle is long and thin, then this will decide whether to remove a link, or merge the two close brains
        //    /// </summary>
        //    /// <returns>
        //    /// Null : This is not a long thin triangle.  Move along
        //    /// Item1=True : Merge the two brains connected by Item2
        //    /// Item1=False : Remove the Item2 link
        //    /// </returns>
        //    private static Tuple<bool, TriangleEdge> PruneLinks_LongThin(ITriangleIndexed triangle, SortedList<Tuple<int, int>, double> all, CombineItemsArgs args)
        //    {
        //        var lengths = new[] { TriangleEdge.Edge_01, TriangleEdge.Edge_12, TriangleEdge.Edge_20 }.
        //            Select(o => new { Edge = o, Length = GetLength(triangle, o, all) }).
        //            OrderBy(o => o.Length).
        //            ToArray();

        //        //NOTE: The order of these if statements is important.  I ran into a case where it's both wide and
        //        //skinny (nearly colinear).  In that case, the long segment should be removed
        //        if (lengths[2].Length / (lengths[0].Length + lengths[1].Length) > args.Ratio_Wide)
        //        {
        //            // Wide base (small angles, one huge angle)
        //            return Tuple.Create(false, lengths[2].Edge);
        //        }
        //        else if (lengths[0].Length / lengths[1].Length < args.Ratio_Skinny && lengths[0].Length / lengths[2].Length < args.Ratio_Skinny)
        //        {
        //            #region Isosceles - skinny base

        //            if (StaticRandom.NextDouble() < args.MergeChance)
        //            {
        //                // Treat the two close brains like one, and split the links evenly with the far brain
        //                return Tuple.Create(true, lengths[0].Edge);
        //            }
        //            else
        //            {
        //                // Choose one of the long links to remove
        //                if (StaticRandom.NextBool())
        //                {
        //                    return Tuple.Create(false, lengths[1].Edge);
        //                }
        //                else
        //                {
        //                    return Tuple.Create(false, lengths[2].Edge);
        //                }
        //            }

        //            #endregion
        //        }

        //        return null;
        //    }

        //    private static void PruneLinks_Merge(TriangleIndexed triangle, TriangleEdge edge, List<Tuple<int[], int[]>> links)
        //    {
        //        // Figure out which indexes to look for
        //        int[] pair = new[] { triangle.GetIndex(edge, true), triangle.GetIndex(edge, false) };
        //        int other = triangle.IndexArray.First(o => !pair.Contains(o));

        //        // Extract the affected links out of the list
        //        List<Tuple<int[], int[]>> affected = new List<Tuple<int[], int[]>>();

        //        int index = 0;
        //        while (index < links.Count)
        //        {
        //            var current = links[index];

        //            if (current.Item1.Contains(other) && current.Item2.Any(o => pair.Contains(o)))
        //            {
        //                affected.Add(current);
        //                links.RemoveAt(index);
        //            }
        //            else if (current.Item2.Contains(other) && current.Item1.Any(o => pair.Contains(o)))
        //            {
        //                affected.Add(Tuple.Create(current.Item2, current.Item1));       // reversing them so that Item1 is always other
        //                links.RemoveAt(index);
        //            }
        //            else
        //            {
        //                index++;
        //            }
        //        }

        //        // Combine the affected links (there shouldn't be more than two)
        //        var merged = Tuple.Create(
        //            affected.SelectMany(o => o.Item1).Distinct().ToArray(),
        //            affected.SelectMany(o => o.Item2).Distinct().ToArray());

        //        links.Add(merged);
        //    }

        //    private static void PruneLinks_Remove(TriangleIndexed triangle, TriangleEdge edge, List<Tuple<int[], int[]>> links)
        //    {
        //        int index1 = triangle.GetIndex(edge, true);
        //        int index2 = triangle.GetIndex(edge, false);

        //        Tuple<int[], int[]> existing = null;
        //        bool? is1in1 = null;

        //        // Find and remove the link that contains this edge
        //        for (int cntr = 0; cntr < links.Count; cntr++)
        //        {
        //            if (links[cntr].Item1.Contains(index1) && links[cntr].Item2.Contains(index2))
        //            {
        //                is1in1 = true;
        //            }
        //            else if (links[cntr].Item1.Contains(index2) && links[cntr].Item2.Contains(index1))
        //            {
        //                is1in1 = false;
        //            }
        //            else
        //            {
        //                continue;
        //            }

        //            existing = links[cntr];
        //            links.RemoveAt(cntr);
        //            break;
        //        }

        //        if (existing == null)
        //        {
        //            //throw new ArgumentException("Didn't find the link");

        //            // A neighbor triangle probably removed it
        //            return;
        //        }

        //        // Add back if there were more than 2 involved
        //        if (existing.Item1.Length == 1 && existing.Item2.Length == 1)
        //        {
        //            // This link only holds one item on each side.  It's already removed from the list, so there is nothing left to do
        //            return;
        //        }

        //        int[] newItem1 = PruneLinks_Remove_Reduce(existing.Item1, index1, index2, is1in1.Value);
        //        int[] newItem2 = PruneLinks_Remove_Reduce(existing.Item2, index2, index1, is1in1.Value);

        //        links.Add(Tuple.Create(newItem1, newItem2));
        //    }
        //    private static int[] PruneLinks_Remove_Reduce(int[] existing, int index1, int index2, bool use1)
        //    {
        //        if (existing.Length == 1)
        //        {
        //            return existing;
        //        }

        //        int removeIndex = use1 ? index1 : index2;

        //        // Keep all but the one to remove
        //        return existing.Where(o => o != removeIndex).ToArray();
        //    }

        //    private static double GetLength(ITriangleIndexed triangle, TriangleEdge edge, SortedList<Tuple<int, int>, double> lengths)
        //    {
        //        return GetLength(triangle.GetIndex(edge, true), triangle.GetIndex(edge, false), lengths);
        //    }
        //    private static double GetLength(Tuple<int, int> pair, SortedList<Tuple<int, int>, double> lengths)
        //    {
        //        if (pair.Item1 < pair.Item2)
        //        {
        //            return lengths[pair];
        //        }
        //        else
        //        {
        //            return lengths[Tuple.Create(pair.Item2, pair.Item1)];
        //        }
        //    }
        //    private static double GetLength(int index1, int index2, SortedList<Tuple<int, int>, double> lengths)
        //    {
        //        if (index1 < index2)
        //        {
        //            return lengths[Tuple.Create(index1, index2)];
        //        }
        //        else
        //        {
        //            return lengths[Tuple.Create(index2, index1)];
        //        }
        //    }

        //    #endregion
        //    #region Private Methods - 1->2

        //    /// <summary>
        //    /// This links closest items together
        //    /// </summary>
        //    private static Tuple<int, int>[] Link12_Closest(Item3D[] items1, Item3D[] items2)
        //    {
        //        var retVal = new Tuple<int, int>[items2.Length];

        //        for (int cntr = 0; cntr < items2.Length; cntr++)
        //        {
        //            int closest = items1.
        //                Select((o, i) => new { Index = i, Position = o.Position.Value, DistSqr = (o.Position.Value - items2[cntr].Position.Value).LengthSquared }).
        //                OrderBy(o => o.DistSqr).
        //                First().
        //                Index;

        //            retVal[cntr] = Tuple.Create(closest, cntr);
        //        }

        //        return retVal;
        //    }

        //    private static Tuple<int, int>[] Link12_Distribute(Item3D[] items1, Item3D[] items2, DistributeDistances distances)
        //    {
        //        IEnumerable<int> addOrder = Enumerable.Range(0, distances.Distances2to1.Length);

        //        return AddLinks(items1, items2, distances, addOrder);
        //    }

        //    private static Tuple<int, int>[] Link12_Extra(Tuple<int, int>[] initial, Item3D[] items1, Item3D[] items2, DistributeDistances distances, ExtraLinkArgs extraArgs)
        //    {
        //        Random rand = StaticRandom.GetRandomForThread();

        //        int wholePercent = extraArgs.Percent.ToInt_Floor();

        //        List<int> addOrder = new List<int>();

        //        if (extraArgs.BySize)
        //        {
        //            double totalSize = items2.Sum(o => o.Size);
        //            double maxSize = extraArgs.Percent * totalSize;
        //            double usedSize = 0;

        //            if (extraArgs.EvenlyDistribute)
        //            {
        //                #region by size, evenly distribute

        //                // Add some complete passes if over 100% percent
        //                for (int cntr = 0; cntr < wholePercent; cntr++)
        //                {
        //                    addOrder.AddRange(UtilityCore.RandomRange(0, items2.Length));
        //                }

        //                usedSize = wholePercent * totalSize;

        //                #endregion
        //            }

        //            #region by size, distribute the rest

        //            //NOTE: Building this list by size so that larger items have a higher chance of being chosen
        //            var bySize = items2.
        //                Select((o, i) => Tuple.Create(i, o.Size / totalSize)).
        //                OrderByDescending(o => o.Item2).
        //                ToArray();

        //            // Keep selecting items unti the extra size is consumed (or if no more can be added)
        //            while (true)
        //            {
        //                bool foundOne = false;

        //                for (int cntr = 0; cntr < 1000; cntr++)     // this is an infinite loop detector
        //                {
        //                    int attemptIndex = UtilityCore.GetIndexIntoList(rand.NextDouble(), bySize);     // get the index into the list that the rand percent represents
        //                    attemptIndex = bySize[attemptIndex].Item1;      // get the index into items2

        //                    if (items2[attemptIndex].Size + usedSize <= maxSize)
        //                    {
        //                        foundOne = true;
        //                        usedSize += items2[attemptIndex].Size;
        //                        addOrder.Add(attemptIndex);
        //                        break;
        //                    }
        //                }

        //                if (!foundOne)
        //                {
        //                    // No more will fit
        //                    break;
        //                }
        //            }

        //            #endregion
        //        }
        //        else
        //        {
        //            if (extraArgs.EvenlyDistribute)
        //            {
        //                #region ignore size, evenly distribute

        //                // Add some complete passes if over 100% percent
        //                for (int cntr = 0; cntr < wholePercent; cntr++)
        //                {
        //                    addOrder.AddRange(UtilityCore.RandomRange(0, items2.Length));
        //                }

        //                // Add some items based on the portion of percent that is less than 100%
        //                int remainder = (items2.Length * (extraArgs.Percent - wholePercent)).
        //                    ToInt_Round();

        //                addOrder.AddRange(UtilityCore.RandomRange(0, items2.Length, remainder));

        //                #endregion
        //            }
        //            else
        //            {
        //                #region ignore size, randomly distribute

        //                int totalCount = (items2.Length * extraArgs.Percent).
        //                    ToInt_Round();

        //                //NOTE: UtilityCore.RandomRange stops when the list is exhausted, and makes sure not to have dupes.  That's not what is wanted
        //                //here.  Just randomly pick X times
        //                addOrder.AddRange(Enumerable.Range(0, totalCount).Select(o => rand.Next(items2.Length)));

        //                #endregion
        //            }
        //        }

        //        return AddLinks(items1, items2, distances, addOrder, initial);
        //    }

        //    /// <summary>
        //    /// This adds 2s to 1s one at a time (specified by distances2to1_AddOrder)
        //    /// </summary>
        //    private static Tuple<int, int>[] AddLinks(Item3D[] items1, Item3D[] items2, DistributeDistances distances, IEnumerable<int> distances2to1_AddOrder, Tuple<int, int>[] initial = null)
        //    {
        //        // Store the inital link burdens
        //        BrainBurden[] links = Enumerable.Range(0, items1.Length).
        //            Select(o => new BrainBurden(o, items1, items2)).
        //            ToArray();

        //        if (initial != null)
        //        {
        //            #region Store initial

        //            foreach (var set in initial.ToLookup(o => o.Item1))
        //            {
        //                foreach (int item2Index in set.Select(o => o.Item2))
        //                {
        //                    links[set.Key].AddIOLink(item2Index);
        //                }
        //            }

        //            #endregion
        //        }

        //        foreach (var distanceIO in distances2to1_AddOrder.Select(o => distances.Distances2to1[o]))
        //        {
        //            int ioIndex = distanceIO.Index2;
        //            int closestBrainIndex = distanceIO.DistancesTo1[0].Item1;

        //            //TODO: This needs to account for the link to already exist.  In that case, it needs to find the next most desirable item to link to
        //            AddIOLink(links, ioIndex, items2[ioIndex].Size, closestBrainIndex, distances.ResistancesItem1[closestBrainIndex]);
        //        }

        //        // Build the return
        //        List<Tuple<int, int>> retVal = new List<Tuple<int, int>>();
        //        foreach (BrainBurden burden in links)
        //        {
        //            retVal.AddRange(burden.IOLinks.Select(o => Tuple.Create(burden.Index, o)));
        //        }

        //        return retVal.ToArray();
        //    }

        //    private static DistributeDistances GetDistances(Item3D[] items1, Item3D[] items2, ItemOverflowArgs overflowArgs)
        //    {
        //        // Distances between all item1s (not just delaunay, but all pairs)
        //        Tuple<int, double>[][] resistancesItem1 = ItemItemResistance(items1, overflowArgs == null ? 1 : overflowArgs.LinkResistanceMult);

        //        // Figure out the distances between 2s and 1s
        //        var distances2to1 = Enumerable.Range(0, items2.Length).
        //            Select(o => new Distances2to1
        //            (
        //                o,
        //                Enumerable.Range(0, items1.Length).
        //                    Select(p => Tuple.Create(p, (items1[p].Position.Value - items2[o].Position.Value).Length)).        //Item1=items1 index, Item2=distance to item1
        //                    OrderBy(p => p.Item2).      // first item1 needs to be the shortest distance
        //                    ToArray()
        //            )).
        //            OrderBy(o => o.DistancesTo1.First().Item2).
        //            ToArray();

        //        return new DistributeDistances(resistancesItem1, distances2to1);
        //    }

        //    private static Tuple<int, double>[][] ItemItemResistance(Item3D[] items, double linkResistanceMult)
        //    {
        //        // Get the AABB, and use the diagonal as the size
        //        var aabb = Math3D.GetAABB(items.Select(o => o.Position.Value));
        //        double maxDistance = (aabb.Item2 - aabb.Item1).Length;

        //        // Get links between the items and distances of each link
        //        var links2 = UtilityCore.GetPairs(items.Length).
        //            Select(o =>
        //            {
        //                double distance = (items[o.Item1].Position.Value - items[o.Item2].Position.Value).Length;
        //                double resistance = (distance / maxDistance) * linkResistanceMult;

        //                return Tuple.Create(o.Item2, o.Item1, resistance);
        //            }).
        //            ToArray();

        //        Tuple<int, double>[][] retVal = new Tuple<int, double>[items.Length][];

        //        for (int cntr = 0; cntr < items.Length; cntr++)
        //        {
        //            // Store all links for this item
        //            retVal[cntr] = links2.
        //                Where(o => o.Item1 == cntr || o.Item2 == cntr).       // find links between this item and another
        //                Select(o => Tuple.Create(o.Item1 == cntr ? o.Item2 : o.Item1, o.Item3)).     // store the link to the other item and the resistance
        //                OrderBy(o => o.Item2).
        //                ToArray();
        //        }

        //        return retVal;
        //    }

        //    /// <summary>
        //    /// This adds ioIndex to one of finalLinks
        //    /// </summary>
        //    /// <param name="closestBrainIndex">Index of the brain that is closest to the IO.  There is no extra burden for linking to this one</param>
        //    /// <param name="brainBrainBurdens">
        //    /// Item1=Index of other brain
        //    /// Item2=Link resistance (burden) between closestBrainIndex and this brain
        //    /// </param>
        //    private static void AddIOLink(BrainBurden[] finalLinks, int ioIndex, double ioSize, int closestBrainIndex, Tuple<int, double>[] brainBrainBurdens)
        //    {
        //        // Figure out the cost of adding the link to the various brains
        //        List<Tuple<int, double>> burdens = new List<Tuple<int, double>>();

        //        for (int cntr = 0; cntr < finalLinks.Length; cntr++)
        //        {
        //            if (finalLinks[cntr].IOLinks.Contains(ioIndex))
        //            {
        //                // This only happens when extra links are requested
        //                continue;
        //            }

        //            int brainIndex = finalLinks[cntr].Index;        // this is likely always the same as cntr, but since that object has brainIndex as a property, I feel safer using it

        //            // Adding to the closest brain has no exta cost.  Adding to any other brain has a cost based on the
        //            // distance between the closest brain and that other brain
        //            double linkCost = 0d;
        //            if (brainIndex != closestBrainIndex)
        //            {
        //                var matchingBrain = brainBrainBurdens.FirstOrDefault(o => o.Item1 == brainIndex);
        //                if (matchingBrain == null)
        //                {
        //                    //NOTE: All brain-brain distances should be passed in, so this should never happen
        //                    continue;
        //                }

        //                linkCost = matchingBrain.Item2;
        //            }

        //            // LinkCost + IOStorageCost
        //            burdens.Add(Tuple.Create(cntr, linkCost + BrainBurden.CalculateBurden(finalLinks[cntr].IOSize + ioSize, finalLinks[cntr].Size)));
        //        }

        //        if (burdens.Count == 0)
        //        {
        //            // This io has already been added to all brains
        //            return;
        //        }

        //        int cheapestIndex = burdens.
        //            OrderBy(o => o.Item2).First().Item1;

        //        finalLinks[cheapestIndex].AddIOLink(ioIndex);
        //    }

        //    #endregion
        //    #region Private Methods

        //    private static void GetItemDelaunay(out SortedList<Tuple<int, int>, double> segments, out TriangleIndexed[] triangles, Item3D[] items)
        //    {
        //        Tuple<int, int>[] links = null;

        //        if (items.Length < 2)
        //        {
        //            links = new Tuple<int, int>[0];
        //            triangles = new TriangleIndexed[0];
        //        }
        //        else if (items.Length == 2)
        //        {
        //            links = new[] { Tuple.Create(0, 1) };
        //            triangles = new TriangleIndexed[0];
        //        }
        //        else if (items.Length == 3)
        //        {
        //            links = new[] 
        //            {
        //                Tuple.Create(0, 1),
        //                Tuple.Create(0, 2),
        //                Tuple.Create(1, 2),
        //            };

        //            triangles = new[] { new TriangleIndexed(0, 1, 2, items.Select(o => o.Position.Value).ToArray()) };
        //        }
        //        else
        //        {
        //            Tetrahedron[] tetras = Math3D.GetDelaunay(items.Select(o => o.Position.Value).ToArray());
        //            links = Tetrahedron.GetUniqueLines(tetras);
        //            triangles = Tetrahedron.GetUniqueTriangles(tetras);
        //        }

        //        segments = GetLengths(links, items);
        //    }

        //    //NOTE: This makes sure that key.item1 is less than key.item2
        //    private static SortedList<Tuple<int, int>, double> GetLengths(Tuple<int, int>[] keys, Item3D[] items)
        //    {
        //        SortedList<Tuple<int, int>, double> retVal = new SortedList<Tuple<int, int>, double>();

        //        foreach (Tuple<int, int> key in keys.Select(o => o.Item1 < o.Item2 ? o : Tuple.Create(o.Item2, o.Item1)))
        //        {
        //            double distance = (items[key.Item1].Position.Value - items[key.Item2].Position.Value).Length;

        //            retVal.Add(key, distance);
        //        }

        //        return retVal;
        //    }

        //    #endregion

        //    #region OLD
        //    //TODO: Break this method in half.  The first half figures out which order to iterate over 2s.  The second half distributes them
        //    //TODO: Call the second half from a separate method that passes in random indices
        //    //private static Tuple<int, int>[] Link12_Distribute_ORIG(Tuple<int, int>[] initial, Item3D[] items1, Item3D[] items2, DistributeDistances distances, ItemOverflowArgs overflowArgs)
        //    //{
        //    //    #region Item 1->2 Distances

        //    //    // Figure out the distances between 2s and 1s
        //    //    var distancesIO = Enumerable.Range(0, items2.Length).
        //    //        Select(o => new
        //    //        {
        //    //            Index2 = o,
        //    //            DistancesTo1 = Enumerable.Range(0, items1.Length).
        //    //                Select(p => Tuple.Create(p, (items1[p].Position.Value - items2[o].Position.Value).Length)).        //Item1=items1 index, Item2=distance to item1
        //    //                OrderBy(p => p.Item2).      // first item1 needs to be the shortest distance
        //    //                ToArray()
        //    //        }).
        //    //        OrderBy(o => o.DistancesTo1.First().Item2).
        //    //        ToArray();

        //    //    #endregion

        //    //    // Link IO to brains
        //    //    BrainBurden[] links = Enumerable.Range(0, items1.Length).
        //    //        Select(o => new BrainBurden(o, items1, items2)).
        //    //        ToArray();

        //    //    foreach (var distanceIO in distancesIO)
        //    //    {
        //    //        int ioIndex = distanceIO.Index2;
        //    //        int closestBrainIndex = distanceIO.DistancesTo1[0].Item1;

        //    //        AddIOLink(links, ioIndex, items2[ioIndex].Size, closestBrainIndex, distances.ResistancesItem1[closestBrainIndex]);
        //    //    }

        //    //    // Build the return
        //    //    List<Tuple<int, int>> retVal = new List<Tuple<int, int>>();
        //    //    foreach (BrainBurden burden in links)
        //    //    {
        //    //        retVal.AddRange(burden.IOLinks.Select(o => Tuple.Create(burden.Index, o)));
        //    //    }

        //    //    //return initial;
        //    //    return retVal.ToArray();
        //    //}
        //    #endregion
        //}

        #endregion

        //private void btn3DLinksBrainBrain_Click_ORIG(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        PrepFor3D();
        //        ClearTempVisuals();

        //        if (_brains3D.Count == 0)
        //        {
        //            return;
        //        }

        //        CombineItemsArgs combineArgs = new CombineItemsArgs()
        //        {
        //            //Ratio_Skinny = .3,
        //            //Ratio_Wide = .88,
        //            //MergeChance = 0,
        //        };

        //        SortedList<Tuple<int, int>, double> all;
        //        LinkSet3D[] final;
        //        Worker3D.Link_Self(out all, out final, _brains3D.ToArray(), combineArgs);

        //        #region Draw

        //        if (final != null)
        //        {
        //            foreach (var link in final)
        //            {
        //                // Group From
        //                if (link.Set1.Items.Length > 1)
        //                {
        //                    _linksBrain3D.Add(DrawBrainGroup(_viewportFull, link.Set1.Positions, link.Set2.Center, _brainGroupColor));
        //                }

        //                // Group To
        //                if (link.Set2.Items.Length > 1)
        //                {
        //                    _linksBrain3D.Add(DrawBrainGroup(_viewportFull, link.Set2.Positions, link.Set2.Center, _brainGroupColor));
        //                }

        //                // Link
        //                Visual3D visual = AddLine(_viewportFull, link.Set1.Center, link.Set2.Center, _brainLinkColor);

        //                _linksBrain3D.Add(new Item3D(visual, 0, new[] { link.Set1.Center, link.Set2.Center }));
        //            }






        //            //_linksBrain3D.Add(new Item3D(AddLines(_viewportFull, brainLinks, brainPositions, _brainLinkColor)));
        //        }

        //        #endregion
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}
        //private void btn3DLinksBrainIO_Click_ORIG(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        PrepFor3D();
        //        ClearTempVisuals();

        //        Item3D[] io = _inputs3D.
        //            Concat(_outputs3D).
        //            ToArray();

        //        ItemOverflowArgs overflowArgs = new ItemOverflowArgs()
        //        {
        //            LinkResistanceMult = trk3DLinkResistMult.Value,
        //        };

        //        ExtraLinkArgs extraArgs = null;
        //        if (!trk3DExtraLinkPercent.Value.IsNearZero())
        //        {
        //            extraArgs = new ExtraLinkArgs()
        //            {
        //                Percent = trk3DExtraLinkPercent.Value / 100d,
        //                BySize = chk3DExtraLinkBySize.IsChecked.Value,
        //                EvenlyDistribute = chk3DExtraLinkEvenDistribute.IsChecked.Value,
        //            };
        //        }

        //        Tuple<int, int>[] links = Worker3D.Link_1_2(_brains3D.ToArray(), io, overflowArgs, extraArgs);

        //        // Draw
        //        _linksIO3D.AddRange(DrawBrainIOLinks3D(_viewportFull, links, _brains3D.ToArray(), io, _ioLinkColor, chk3DRainbowLinks.IsChecked.Value));
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}
        //private void btn3DLinksBrainInput_Click_ORIG(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        PrepFor3D();
        //        ClearTempVisuals();

        //        ItemOverflowArgs overflowArgs = new ItemOverflowArgs()
        //        {
        //            LinkResistanceMult = trk3DLinkResistMult.Value,
        //        };

        //        Tuple<int, int>[] links = Worker3D.Link_1_2(_inputs3D.ToArray(), _brains3D.ToArray(), overflowArgs);

        //        // Draw
        //        _linksIO3D.AddRange(DrawBrainIOLinks3D(_viewportFull, links, _inputs3D.ToArray(), _brains3D.ToArray(), _ioLinkColor, chk3DRainbowLinks.IsChecked.Value));
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}

        #endregion
    }
}
