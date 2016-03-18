using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;

namespace Game.HelperClassesAI
{
    /// <summary>
    /// Breaks a set of inputs into catetories
    /// </summary>
    /// <remarks>
    /// This treats each input like a point in a high dimension space:
    /// 
    /// Creates a bunch of random points in the input's aabb (node centers):
    /// 
    /// Iterates many times.  Each time:
    ///     Pick a random input, find the closest node to that input, and pull the node toward that input
    ///     Also pull nearby nodes toward that input (the search radius shrinks a little each iteration)
    ///     (the strength of pulling also shrinks each iteration)
    /// 
    /// http://www.ai-junkie.com/ann/som/som1.html
    /// https://www.mql5.com/en/articles/283?source=metaeditor5_article
    ///
    /// Scroll to the section "IV. Determining the Quality of SOMs".  Instead of generating a second black/white image, draw borders between colors where it's too black
    /// http://davis.wpi.edu/~matt/courses/soms/
    /// implementation code (screen has the important code)
    /// http://davis.wpi.edu/~matt/courses/soms/soms.java
    /// http://davis.wpi.edu/~matt/courses/soms/Screen.java
    /// http://davis.wpi.edu/~matt/courses/soms/fpoint.java
    /// </remarks>
    public static class SelfOrganizingMaps
    {
        /// <summary>
        /// This creates nodes with random weights based on the input's weights.  After training, it creates random positions, and arranges
        /// the positions so similar sets are near each other
        /// </summary>
        /// <param name="inputs">These are items turned into vectors.  They could be images, db row hashes, whatever</param>
        /// <param name="isDisplay2D">This doesn't affect the actual algorithm, just node.Position (true is 2D, false is 3D)</param>
        /// <param name="returnEmptyNodes">This shouldn't even be an option.  Empty nodes are just artifacts that polute the final result</param>
        public static SOMResult TrainSOM(ISOMInput[] inputs, SOMRules rules, bool isDisplay2D, bool returnEmptyNodes = false)
        {
            double[][] nodeWeights = GetRandomNodeWeights(rules.NumNodes, inputs);

            SOMNode[] nodes = nodeWeights.
                Select(o => new SOMNode() { Weights = o }).
                ToArray();

            SOMResult retVal = TrainSOM(nodes, inputs, rules, returnEmptyNodes);

            // Inject positions into the nodes
            InjectNodePositions2D(retVal.Nodes);        //TODO: Look at isDisplay2D
            retVal = ArrangeNodes_LikesAttract(retVal);

            return retVal;
        }

        /// <summary>
        /// This overload does an initial training, then recurses on any node that has too wide of a range of values
        /// </summary>
        /// <remarks>
        /// This method is a bit of a failure.  Sometimes it works, but other times it just runs without fixing anything
        /// </remarks>
        /// <param name="maxSpreadPercent">
        /// Spread is an input's distance from the center of all inputs.  The percent is a node's max distance divided by all node's max distance.
        /// .65 to .75 is a good value to use (smaller values will chop up into more nodes)
        /// </param>
        public static SOMResult TrainSOM(ISOMInput[] inputs, SOMRules rules, double maxSpreadPercent, bool isDisplay2D, bool returnEmptyNodes = false)
        {
            const int MININPUTSFORSPLIT = 4;

            // Get the initial result
            SOMResult result = TrainSOM(inputs, rules, isDisplay2D, returnEmptyNodes);

            #region Divide large nodes

            double totalSpread = GetTotalSpread(inputs.Select(o => o.Weights));

            int infiniteLoop = 0;

            while (infiniteLoop < 50)     // if it exceeds this, just use whatever is there
            {
                // Split up nodes that have too much variation (image's distance from average)
                var reduced = Enumerable.Range(0, result.Nodes.Length).
                    AsParallel().
                    Select(o => SplitNode(o, result, MININPUTSFORSPLIT, maxSpreadPercent, totalSpread, rules)).
                    ToArray();

                if (reduced.All(o => !o.Item1))
                {
                    // No changes were needed this pass
                    break;
                }

                SOMNode[] reducedNodes = reduced.
                    SelectMany(o => o.Item2).
                    ToArray();

                // Rebuild result
                ISOMInput[][] imagesByNode = SelfOrganizingMaps.GetInputsByNode(reducedNodes, inputs);
                result = new SOMResult(reducedNodes, imagesByNode, false);

                result = SelfOrganizingMaps.RemoveZeroNodes(result);

                infiniteLoop++;
            }

            #endregion

            // Inject positions into the nodes
            InjectNodePositions2D(result.Nodes);        //TODO: Look at isDisplay2D
            result = ArrangeNodes_LikesAttract(result);

            return result;
        }

        #region Misc Helpers

        public static SOMResult ArrangeNodes_LikesAttract(SOMResult result)
        {
            double[][] weights = result.Nodes.
                Select(o => o.Weights).
                ToArray();

            // Get the high dimension distances
            var desiredDistances = MathND.GetDistancesBetween(weights);

            // Merge nodes that have the same high dimension position
            if (MergeTouchingNodes(ref result, desiredDistances))
            {
                // Redo it
                weights = result.Nodes.
                    Select(o => o.Weights).
                    ToArray();

                desiredDistances = MathND.GetDistancesBetween(weights);
            }

            // Pull the low dimension positions to try to match the high dimension distances
            //NOTE: This has no effect on InputsByNode (those are high dimension)
            SOMNode[] nodes = MoveNodes_BallOfSprings(result.Nodes, desiredDistances, 1500);

            return new SOMResult(nodes, result.InputsByNode, result.IncludesEmptyNodes);
        }

        /// <summary>
        /// This is similar logic as standard deviation, but this returns the max distance from the average
        /// NOTE: Spread is probably the wrong word, since this only returns the max distance (radius instead of diameter)
        /// </summary>
        public static double GetTotalSpread(IEnumerable<double[]> values)
        {
            double[] mean = MathND.GetCenter(values);

            double distancesSquared = values.
                Select(o =>
                {
                    double[] diff = MathND.Subtract(o, mean);
                    return MathND.GetLengthSquared(diff);
                }).
                OrderByDescending().
                First();

            return Math.Sqrt(distancesSquared);
        }

        public static Tuple<SOMNode, int> GetClosest(SOMNode[] nodes, ISOMInput input)
        {
            int closestIndex = -1;
            double closest = double.MaxValue;
            SOMNode retVal = null;

            for (int cntr = 0; cntr < nodes.Length; cntr++)
            {
                double distSquared = MathND.GetDistanceSquared(nodes[cntr].Weights, input.Weights);

                if (distSquared < closest)
                {
                    closestIndex = cntr;
                    closest = distSquared;
                    retVal = nodes[cntr];
                }
            }

            return Tuple.Create(retVal, closestIndex);
        }

        #endregion

        #region Private Methods

        private static SOMResult TrainSOM(SOMNode[] nodes, ISOMInput[] inputs, SOMRules rules, bool returnEmptyNodes = false)
        {
            //double mapRadius = GetMapRadius(inputs);
            double mapRadius = GetMapRadius(nodes);

            SOMNode[] returnNodes = nodes.
                Select(o => o.Clone()).
                ToArray();

            double timeConstant = rules.NumIterations / Math.Log(mapRadius);

            int iteration = 0;
            int remainingIterations = rules.NumIterations;

            while (remainingIterations > 0)
            {
                foreach (ISOMInput input in UtilityCore.RandomOrder(inputs, Math.Min(remainingIterations, inputs.Length)))
                {
                    // Find closest node
                    SOMNode closest = GetClosest(returnNodes, input).Item1;

                    // Find other affected nodes (a node and distance squared)
                    double searchRadius = mapRadius * rules.InitialRadiusPercent * Math.Exp(-iteration / timeConstant);
                    Tuple<SOMNode, double>[] neigbors = GetNeighbors(returnNodes, closest, searchRadius);

                    double learningRate = rules.LearningRate * Math.Exp(-(double)iteration / (double)rules.NumIterations);

                    // Adjust the matched node (full learning rate)
                    AdjustNodeWeights(closest, input.Weights, learningRate);

                    double twiceSearchRadiusSquared = 2d * (searchRadius * searchRadius);

                    foreach (var node in neigbors)
                    {
                        double influence = Math.Exp(-node.Item2 / twiceSearchRadiusSquared);        // this will reduce the learning rate as distance increases (guassian dropoff)

                        // Adjust a neighbor
                        AdjustNodeWeights(node.Item1, input.Weights, learningRate * influence);
                    }

                    iteration++;
                }

                remainingIterations -= inputs.Length;
            }

            // See which images go with which nodes
            ISOMInput[][] inputsByNode = GetInputsByNode(returnNodes, inputs);

            SOMResult retVal = new SOMResult(returnNodes, inputsByNode, true);

            if (!returnEmptyNodes)
            {
                retVal = RemoveZeroNodes(retVal);
            }

            return retVal;
        }

        private static Tuple<SOMNode, double>[] GetNeighbors(SOMNode[] nodes, SOMNode match, double maxDistance)
        {
            List<Tuple<SOMNode, double>> retVal = new List<Tuple<SOMNode, double>>();

            double maxDistanceSquared = maxDistance * maxDistance;

            for (int cntr = 0; cntr < nodes.Length; cntr++)
            {
                if (nodes[cntr].Token == match.Token)
                {
                    continue;
                }

                double distSquared = MathND.GetDistanceSquared(nodes[cntr].Weights, match.Weights);

                if (distSquared < maxDistanceSquared)
                {
                    retVal.Add(Tuple.Create(nodes[cntr], distSquared));     // no need for a square root.  The calling method needs it squared
                }
            }

            return retVal.ToArray();
        }

        private static void AdjustNodeWeights(SOMNode node, double[] position, double percent)
        {
            //W(t+1) = W(t) + (pos - W(t)) * %

            for (int cntr = 0; cntr < position.Length; cntr++)
            {
                node.Weights[cntr] += (position[cntr] - node.Weights[cntr]) * percent;
            }
        }

        /// <summary>
        /// This gets the bounding box of all the input values, then creates random vectors within that box
        /// </summary>
        private static double[][] GetRandomNodeWeights(int count, ISOMInput[] inputs)
        {
            var aabb = MathND.GetAABB(inputs.Select(o => o.Weights));
            aabb = MathND.ResizeAABB(aabb, 1.1);     // allow the return vectors to be slightly outside the input box

            return Enumerable.Range(0, count).
                Select(o => MathND.GetRandomVector(aabb.Item1, aabb.Item2)).
                ToArray();
        }
        /// <summary>
        /// This does a bounding box of inputs, and also makes sure all positions are closer to the desired node than
        /// other nodes
        /// </summary>
        private static double[][] GetRandomWeights_InsideCell(int count, ISOMInput[] inputs, SOMNode[] nodes, int nodeIndex)
        {
            var inputAABB = MathND.GetAABB(inputs.Select(o => o.Weights));

            // This actually could happen.  Detecting an infinite loop instead
            //if (!MathND.IsInside(inputAABB, nodes[nodeIndex].Weights))
            //{
            //    throw new ArgumentException("The node sits outside the input's aabb");
            //}

            List<double[]> retVal = new List<double[]>();

            for (int cntr = 0; cntr < count; cntr++)
            {
                int infiniteLoopDetector = 0;

                while (true)
                {
                    double[] attempt = MathND.GetRandomVector(inputAABB.Item1, inputAABB.Item2);

                    var closest = nodes.
                        Select((o, i) => new { Index = i, DistSquared = MathND.GetDistanceSquared(attempt, o.Weights) }).
                        OrderBy(o => o.DistSquared).
                        First();

                    if (closest.Index == nodeIndex)
                    {
                        retVal.Add(attempt);
                        break;
                    }

                    infiniteLoopDetector++;
                    if (infiniteLoopDetector > 1000)
                    {
                        // Instead of giving up, increase the range that the weights can exist in.  When testing this other method, there is almost never an improved
                        // node.  It's just an infinite loop in the caller (keeps trying for an improvement, but it never happens)
                        return GetRandomWeights_InsideCell_LARGERBUTFAIL(count, inputs, nodes, nodeIndex);
                    }
                }
            }

            return retVal.ToArray();
        }
        /// <summary>
        /// Only call this from the other overload.  This uses a much larger bounding box (it still makes sure all returned points
        /// are closer to the desired node than other nodes)
        /// </summary>
        private static double[][] GetRandomWeights_InsideCell_LARGERBUTFAIL(int count, ISOMInput[] inputs, SOMNode[] nodes, int nodeIndex)
        {
            //TODO: May want to get a voronoi, then choose random points within the convex hull (more elegant, but may not be any faster)

            // This works, but the caller generally never finds a solution that's better then what it already has.  So if you need
            // to use this overload, you're just going to spin the processor

            #region calculate rectangle to choose points from

            // Get some nearby nodes
            double? largestDistance = Enumerable.Range(0, nodes.Length).
                Where(o => o != nodeIndex).
                Select(o => MathND.GetDistanceSquared(nodes[o].Weights, nodes[nodeIndex].Weights)).
                OrderByDescending(o => o).
                FirstOrDefault();

            var inputAABB = MathND.GetAABB(inputs.Select(o => o.Weights));

            if (largestDistance == null)
            {
                largestDistance = Math.Max(
                    MathND.GetDistanceSquared(inputAABB.Item1, nodes[nodeIndex].Weights),
                    MathND.GetDistanceSquared(inputAABB.Item2, nodes[nodeIndex].Weights));
            }

            largestDistance = Math.Sqrt(largestDistance.Value);

            var bounds = MathND.GetAABB(new[]
            {
                nodes[nodeIndex].Weights.Select(o => o - largestDistance.Value).ToArray(),
                nodes[nodeIndex].Weights.Select(o => o + largestDistance.Value).ToArray(),
                inputAABB.Item1,
                inputAABB.Item2,
            });

            #endregion



            List<double[]> retVal = new List<double[]>();
            //int largestIteration = 0;

            for (int cntr = 0; cntr < count; cntr++)
            {
                int infiniteLoopDetector = 0;

                while (true)
                {
                    //if(infiniteLoopDetector > largestIteration)
                    //{
                    //    largestIteration = infiniteLoopDetector;      // I tested several times, and averaged about 5-15 iterations
                    //}

                    double[] attempt = MathND.GetRandomVector(bounds.Item1, bounds.Item2);

                    var closest = nodes.
                        Select((o, i) => new { Index = i, DistSquared = MathND.GetDistanceSquared(attempt, o.Weights) }).
                        OrderBy(o => o.DistSquared).
                        First();

                    if (closest.Index == nodeIndex)
                    {
                        retVal.Add(attempt);
                        break;
                    }

                    infiniteLoopDetector++;
                    if (infiniteLoopDetector > 1000)
                    {
                        throw new ApplicationException("Infinite loop detected");
                    }
                }
            }

            return retVal.ToArray();
        }

        /// <summary>
        /// This does a new SOM for one node (sort of like recursing on a node)
        /// </summary>
        /// <param name="index">The node to break apart</param>
        private static Tuple<bool, SOMNode[]> SplitNode(int index, SOMResult result, int minNodeItemsForSplit, double maxSpreadPercent, double totalSpread, SOMRules rules)
        {
            ISOMInput[] inputs = result.InputsByNode[index];

            // Don't split if there aren't enough inputs in the parent
            if (inputs.Length < minNodeItemsForSplit)
            {
                return Tuple.Create(false, new[] { result.Nodes[index] });
            }

            // See how this node's distances from the average compare with the total
            double nodeSpread = GetTotalSpread(inputs.Select(o => o.Weights));
            double percentSpread = nodeSpread / totalSpread;

            if (percentSpread < maxSpreadPercent)
            {
                return Tuple.Create(false, new[] { result.Nodes[index] });
            }

            // Get random node weights.  Don't let any of those weights be closer to other nodes than this node
            double[][] weights = GetRandomWeights_InsideCell(rules.NumNodes, inputs, result.Nodes, index);

            SOMNode[] nodes = Enumerable.Range(0, rules.NumNodes).
                Select(o => new SOMNode() { Weights = weights[o] }).
                ToArray();

            // Split up this node
            SOMResult subResult = TrainSOM(nodes, inputs, rules, false);

            return Tuple.Create(true, subResult.Nodes);
        }

        /// <summary>
        /// This pulls the low dimension positions toward ideal configurations based on the corresponding high
        /// dimension relationships (the links between nodes act like springs)
        /// </summary>
        private static SOMNode[] MoveNodes_BallOfSprings(SOMNode[] nodes, Tuple<int, int, double>[] desiredDistances, int numIterations)
        {
            double[][] positions = nodes.
                Select(o => o.Position).
                ToArray();

            positions = MathND.ApplyBallOfSprings(positions, desiredDistances, numIterations);

            // Rebuild nodes
            return Enumerable.Range(0, nodes.Length).
                Select(o => new SOMNode()
                {
                    Weights = nodes[o].Weights,
                    Position = positions[o]
                }).
                ToArray();
        }

        /// <summary>
        /// If two nodes are too close to each other, they get merged into one
        /// </summary>
        private static bool MergeTouchingNodes(ref SOMResult result, Tuple<int, int, double>[] distances, double minDist = .01)
        {
            // Find touching
            var touching = distances.
                Where(o => o.Item3 < minDist).
                ToArray();

            if (touching.Length == 0)
            {
                return false;
            }

            #region Merge key pairs

            // There could be several pairs that need to be joined.  ex:
            //      {0,2} {0,3} {2,5}       ->      {0,2,3,5}
            //      {1,6}       ->      {1,6}

            List<List<int>> sets = new List<List<int>>();

            foreach (var pair in touching)
            {
                List<int> existing = sets.FirstOrDefault(o => o.Contains(pair.Item1) || o.Contains(pair.Item2));
                if (existing == null)
                {
                    existing = new List<int>();
                    existing.Add(pair.Item1);
                    existing.Add(pair.Item2);
                    sets.Add(existing);
                }
                else
                {
                    if (!existing.Contains(pair.Item1))
                    {
                        existing.Add(pair.Item1);
                    }
                    else if (!existing.Contains(pair.Item2))     // if it didn't contain 1, then it matched on 2, so no need to look for 2
                    {
                        existing.Add(pair.Item2);
                    }
                }
            }

            #endregion
            #region Singular sets

            // Identify stand alone nodes, and add their index to the sets list (makes the next section easier to implement)

            for (int cntr = 0; cntr < result.Nodes.Length; cntr++)
            {
                if (!sets.Any(o => o.Contains(cntr)))
                {
                    List<int> singleSet = new List<int>();
                    singleSet.Add(cntr);
                    sets.Add(singleSet);
                }
            }

            #endregion
            #region Merge nodes

            List<SOMNode> newNodes = new List<SOMNode>();
            List<ISOMInput[]> newImagesByNode = new List<ISOMInput[]>();

            foreach (List<int> set in sets)
            {
                // Just use the first node (no need to take the average of weights since they're nearly identical, and taking the average position
                // doesn't add any value - later methods will move the node positions around anyway)
                newNodes.Add(result.Nodes[set[0]]);

                if (set.Count == 1)
                {
                    newImagesByNode.Add(result.InputsByNode[set[0]]);
                }
                else
                {
                    List<ISOMInput> mergedInputs = new List<ISOMInput>();
                    foreach (int index in set)
                    {
                        mergedInputs.AddRange(result.InputsByNode[index]);
                    }

                    newImagesByNode.Add(mergedInputs.ToArray());
                }
            }

            #endregion

            result = new SOMResult(newNodes.ToArray(), newImagesByNode.ToArray(), result.IncludesEmptyNodes);
            return true;
        }

        /// <summary>
        /// This returns which inputs belong to which node (which node it's closest to)
        /// </summary>
        private static ISOMInput[][] GetInputsByNode(SOMNode[] nodes, ISOMInput[] inputs)
        {
            List<ISOMInput>[] retVal = Enumerable.Range(0, nodes.Length).
                Select(o => new List<ISOMInput>()).
                ToArray();

            foreach (var image in inputs)
            {
                var match = GetClosest(nodes, image);
                retVal[match.Item2].Add(image);
            }

            return retVal.
                Select(o => o.ToArray()).
                ToArray();
        }

        /// <summary>
        /// Remove nodes that don't have any inputs
        /// </summary>
        private static SOMResult RemoveZeroNodes(SOMResult result)
        {
            List<SOMNode> subNodes = new List<SOMNode>();
            List<ISOMInput[]> subImagesByNode = new List<ISOMInput[]>();

            for (int cntr = 0; cntr < result.Nodes.Length; cntr++)
            {
                if (result.InputsByNode[cntr].Length == 0)
                {
                    continue;
                }

                subNodes.Add(result.Nodes[cntr]);
                subImagesByNode.Add(result.InputsByNode[cntr]);
            }

            return new SOMResult(subNodes.ToArray(), subImagesByNode.ToArray(), false);
        }

        private static void InjectNodePositions2D(SOMNode[] nodes)
        {
            double[][] positions = Math3D.GetRandomVectors_Circular_EvenDist(nodes.Length, 1).
                Select(o => new[] { o.X, o.Y }).
                ToArray();

            for (int cntr = 0; cntr < nodes.Length; cntr++)
            {
                nodes[cntr].Position = positions[cntr];
            }
        }

        private static double GetMapRadius(SOMNode[] nodes)
        {
            var aabb = MathND.GetAABB(nodes.Select(o => o.Weights));

            // Get half the diagonal
            double diagRadius = MathND.GetDistance(aabb.Item1, aabb.Item2) / 2d;

            // Divide by sqrt to get the radius instead of diagonal radius
            return diagRadius / Math.Sqrt(aabb.Item1.Length);
        }

        #endregion
    }

    #region Class: SOMRules

    public class SOMRules
    {
        public SOMRules(int numNodes, int numIterations, double initialRadiusPercent, double learningRate)
        {
            this.NumNodes = numNodes;
            this.NumIterations = numIterations;
            this.InitialRadiusPercent = initialRadiusPercent;
            this.LearningRate = learningRate;
        }

        /// <summary>
        /// 20 to 50
        /// </summary>
        public readonly int NumNodes;
        /// <summary>
        /// 2000 to 5000
        /// </summary>
        public readonly int NumIterations;
        /// <summary>
        /// .33
        /// </summary>
        /// <remarks>
        /// This one is tricky.  With weights in 3 dimensions, 33% worked well.  With 49 dimensions, I had to put it around 400% just for any effect, but
        /// then things were grouped together too much
        /// 
        /// So maybe lower dimension data benefits from this more than higher dimensions?
        /// 
        /// 
        /// The problem is with calculating distance.  To the the length of the sides of the cube, take the diagonal divided by sqrt(dimension).
        /// But nodes in a high dimension have larger distances from each other than lower dimensions
        /// TODO: Use distance squared instead of distance?
        /// </remarks>
        public readonly double InitialRadiusPercent;
        /// <summary>
        /// .1
        /// </summary>
        public readonly double LearningRate;
    }

    #endregion
    #region Interface: ISOMInput

    public interface ISOMInput
    {
        double[] Weights { get; }
    }

    #endregion
    #region Class: SOMInput

    /// <summary>
    /// If all you want is a link to the original source, you can use this
    /// </summary>
    public class SOMInput<T> : ISOMInput
    {
        public T Source { get; set; }
        public double[] Weights { get; set; }
    }

    #endregion
    #region Class: SOMNode

    public class SOMNode
    {
        public readonly long Token = TokenGenerator.NextToken();

        /// <summary>
        /// These should be from 0 to 1
        /// </summary>
        public double[] Weights { get; set; }

        /// <summary>
        /// This doesn't have anything to do with the SOM algorithm.  It is the location of the node in presentation
        /// coords (probably 2D or 3D)
        /// </summary>
        /// <remarks>
        /// By being explicit with position, the nodes could be layed out as rectangles (like all the examples), or arranged
        /// as a voronoi
        /// 
        /// If you go the voronoi route, rearrange the positions after the weights have been trained, so that nodes with
        /// similar weights get displayed near each other
        /// </remarks>
        public double[] Position { get; set; }

        public SOMNode Clone()
        {
            SOMNode retVal = new SOMNode();

            if (this.Weights != null)
            {
                retVal.Weights = this.Weights.ToArray();
            }

            if (this.Position != null)
            {
                retVal.Position = this.Position.ToArray();
            }

            return retVal;
        }
    }

    #endregion

    #region Class: SOMResult

    public class SOMResult
    {
        public SOMResult(SOMNode[] nodes, ISOMInput[][] inputsByNode, bool includesEmptyNodes)
        {
            this.Nodes = nodes;
            this.InputsByNode = inputsByNode;
            this.IncludesEmptyNodes = includesEmptyNodes;
        }

        public readonly SOMNode[] Nodes;
        public readonly ISOMInput[][] InputsByNode;

        /// <summary>
        /// True: InputsByNode will have some items that are empty (all original nodes that were trained against were included in this result).
        /// False: Only nodes that have matching inputs are included (the nodes were pruned before populating this result).
        /// </summary>
        public readonly bool IncludesEmptyNodes;
    }

    #endregion
}
