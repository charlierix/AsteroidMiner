using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Game.HelperClassesCore;

namespace Game.HelperClassesAI
{
    //TODO: Move a bunch of the methods from MutateUtility
    public static class UtilityAI
    {
        #region Class: CrossoverWorker

        private static class CrossoverWorker
        {
            public static T[][] Crossover<T>(T[][] parents, int numSlices)
            {
                if (parents == null || parents.Length == 0)
                {
                    return null;
                }
                else if (parents.Length == 1)
                {
                    return parents;
                }

                numSlices = Math.Min(numSlices, parents[0].Length - 1);       // one fewer than len to allow for the section to the right of the last slice
                int[] slicePoints = UtilityCore.RandomRange(0, parents[0].Length - 1, numSlices).
                    OrderBy().
                    ToArray();

                int[][] newMap = GetCrossedArrays(numSlices + 1, parents.Length, StaticRandom.GetRandomForThread());

                T[][] retVal = new T[parents.Length][];

                for (int cntr = 0; cntr < parents.Length; cntr++)
                {
                    retVal[cntr] = BuildCrossed(parents, slicePoints, newMap[cntr]);
                }

                return retVal;
            }

            #region Private Methods

            /// <summary>
            /// This returns a set of new child arrays that contain crossed over indicies
            /// </summary>
            /// <remarks>
            /// Example: 3 parents with 4 crossovers
            /// 
            /// Initial
            /// { 0 0 0 0 }
            /// { 1 1 1 1 }
            /// { 2 2 2 2 }
            /// 
            /// Possible return
            /// { 2 1 0 2 }
            /// { 0 2 1 0 }
            /// { 1 0 2 1 }
            /// 
            /// Another possible return
            /// { 0 2 0 1 }
            /// { 1 0 1 2 }
            /// { 2 1 2 0 }
            /// </remarks>
            private static int[][] GetCrossedArrays(int columns, int rows, Random rand)
            {
                // Get random stripes
                List<int[]> stripes = new List<int[]>();

                for (int cntr = 0; cntr < columns; cntr++)
                {
                    int[] prevStripe = null;
                    if (cntr > 0)
                    {
                        prevStripe = stripes[cntr - 1];
                    }

                    stripes.Add(GetStripe(rows, rand, prevStripe));
                }

                // Transpose
                int[][] retVal = new int[rows][];

                for (int row = 0; row < rows; row++)
                {
                    retVal[row] = Enumerable.Range(0, columns).
                        Select(o => stripes[o][row]).
                        ToArray();
                }

                return retVal;
            }

            /// <summary>
            /// This returns a vertical stripe
            /// </summary>
            private static int[] GetStripe(int count, Random rand, int[] prev = null)
            {
                if (prev == null)
                {
                    return UtilityCore.RandomRange(0, count).ToArray();
                }

                int[] retVal = new int[count];

                List<int> remaining = Enumerable.Range(0, count).ToList();

                for (int cntr = 0; cntr < count; cntr++)
                {
                    retVal[cntr] = GetStripe_ChooseIndex(prev, cntr, remaining, rand);
                }

                return retVal;
            }
            private static int GetStripe_ChooseIndex(int[] prev, int index, List<int> remaining, Random rand)
            {
                for (int cntr = 0; cntr < 100000; cntr++)
                {
                    // Grab one out of the hat
                    int remIndex = rand.Next(remaining.Count);

                    // Make sure it's not the same as the previous stripe
                    if (prev[index] == remaining[remIndex])
                    {
                        continue;
                    }

                    // When there are two left (but more than 2 total in the stripe), there is a possibility of trapping the last one:
                    //      2 - 0
                    //      0 - 2 --- If 2 is chosen here, then only 1 remains.  So 1 must be chosen here, which leaves 2 for the final slot
                    //      1 - 
                    if (remaining.Count == 2)
                    {
                        int otherIndex = remIndex == 0 ? 1 : 0;
                        if (prev[index + 1] == remaining[otherIndex])
                        {
                            remIndex = otherIndex;
                        }
                    }

                    int retVal = remaining[remIndex];
                    remaining.RemoveAt(remIndex);
                    return retVal;
                }

                throw new ApplicationException("Infinite loop detected");
            }

            private static T[] BuildCrossed<T>(T[][] parents, int[] slicePoints, int[] map)
            {
                T[] retVal = new T[parents[0].Length];

                int mapIndex = 0;

                foreach (int[] range in BuildCrossed_Range(slicePoints, retVal.Length))
                {
                    foreach (int column in range)
                    {
                        retVal[column] = parents[map[mapIndex]][column];
                    }

                    mapIndex++;
                }

                return retVal;
            }
            private static IEnumerable<int[]> BuildCrossed_Range(int[] slicePoints, int columnCount)
            {
                for (int cntr = 0; cntr < slicePoints.Length; cntr++)
                {
                    if (cntr == 0)
                    {
                        yield return Enumerable.Range(0, slicePoints[cntr] + 1).ToArray();
                    }
                    else
                    {
                        yield return Enumerable.Range(slicePoints[cntr - 1] + 1, slicePoints[cntr] - slicePoints[cntr - 1]).ToArray();
                    }
                }

                int lastIndex = slicePoints[slicePoints.Length - 1];
                if (lastIndex < columnCount - 1)
                {
                    yield return Enumerable.Range(lastIndex + 1, columnCount - lastIndex - 1).ToArray();
                }
            }

            #endregion
        }

        #endregion
        #region Class: Discoverer

        private static class Discoverer
        {
            #region Class: SolutionItem<T>

            private class SolutionItem<T>
            {
                public SolutionItem(T[] item)
                {
                    this.Item = item;
                }

                public readonly T[] Item;

                public SolutionError Error { get; set; }
            }

            #endregion
            #region Class: Parents<T>

            private class Parents<T>
            {
                public Parents(SolutionItem<T>[] overall, SolutionItem<T>[][] categories)
                {
                    this.Overall = overall;
                    this.Categories = categories;

                    if (categories == null)
                    {
                        this.Combined = overall;
                    }
                    else
                    {
                        this.Combined = overall.
                            Concat(categories.SelectMany(o => o)).
                            ToArray();
                    }
                }

                public readonly SolutionItem<T>[] Overall;
                public readonly SolutionItem<T>[][] Categories;

                public readonly SolutionItem<T>[] Combined;
            }

            #endregion

            public static SolutionResult<T> DiscoverSolution<T>(DiscoverSolutionDelegates<T> delegates, DiscoverSolutionOptions<T> options = null)
            {
                Random rand = StaticRandom.GetRandomForThread();

                options = options ?? new DiscoverSolutionOptions<T>();

                SolutionItem<T>[] predefined = GetPredefined(options.Predefined, delegates.GetError);

                // Generate random samples
                SolutionItem<T>[] generation = GetRandomSamples<T>(options.GenerationSize, predefined, delegates.GetNewSample, delegates.GetError, rand);

                SolutionItem<T> currentWinner = null;
                List<double> errorHistory = new List<double>();       //TODO: A simple list of arrays is too simplistic.  Each category should get its own top performer, as well as a global top performing score
                SolutionResultType? result = null;

                // Keep working them until there is an acceptable error
                for (int cntr = 0; cntr < options.MaxIterations; cntr++)        // could break early if the error is low enough
                {
                    errorHistory.Add(generation[0].Error.TotalError);

                    // Check for success
                    if (generation[0].Error.TotalError < options.StopError)        // they are sorted by sumerror
                    {
                        result = SolutionResultType.Final_ErrorThreshold;
                        break;
                    }

                    // Check for cancel
                    if (delegates.Cancel.IsCancellationRequested)
                    {
                        result = SolutionResultType.Premature_Cancelled;
                        break;
                    }

                    // Check for new best
                    if (delegates.NewBestFound != null && (currentWinner == null || generation[0].Error.TotalError < currentWinner.Error.TotalError))
                    {
                        currentWinner = generation[0];
                        delegates.NewBestFound(new SolutionResult<T>(currentWinner.Item, SolutionResultType.Intermediate_NewBest, currentWinner.Error, errorHistory.ToArray()));
                    }

                    // Breed them (and rescore)
                    generation = Step(generation, options, delegates, predefined, rand);
                }

                result = result ?? SolutionResultType.Premature_MaxIterations;

                return new SolutionResult<T>(generation[0].Item, result.Value, generation[0].Error, errorHistory.ToArray());
            }

            #region Private Methods

            private static SolutionItem<T>[] Step<T>(SolutionItem<T>[] generation, DiscoverSolutionOptions<T> options, DiscoverSolutionDelegates<T> delegates, SolutionItem<T>[] predefined, Random rand)
            {
                // Take the top X items, and use them as parents for the next generation
                Parents<T> parents = GetParents(generation, options.NumStraightCopies);

                #region build new items

                var retVal = new List<SolutionItem<T>>();

                // Brand new samples
                if (options.NumNewEachStep > 0)
                {
                    retVal.AddRange(GetRandomSamples<T>(options.NumNewEachStep, predefined, delegates.GetNewSample, delegates.GetError, rand));       // this also scores them
                }

                // Children of top performers
                while (retVal.Count < options.GenerationSize)
                {
                    retVal.AddRange(GetNewChildren(parents, delegates.Mutate, delegates.GetError, rand));
                }

                #endregion
                #region add parents

                //NOTE: Adding parents at the end, so it doesn't affect generation size

                retVal.AddRange(parents.Combined);       // no need to rescore these

                #endregion

                return retVal.
                    OrderBy(o => o.Error.TotalError).
                    ToArray();
            }

            /// <summary>
            /// Find the items with the lowest error in each category
            /// </summary>
            private static Parents<T> GetParents<T>(SolutionItem<T>[] generation, int count)
            {
                //TODO: If a couple more have similar scores, it might be good to keep a few more parents (but just keep the standard number if the scores are climbing quickly)

                // Get the top overall performers
                SolutionItem<T>[] overall = generation.
                    OrderBy(o => o.Error.TotalError).
                    Take(count).
                    ToArray();

                // Get the top performers in each category
                SolutionItem<T>[][] categories = null;
                if (generation[0].Error.Error.Length > 1)     // if it's only one, then categories would just be a duplicate of overall
                {
                    categories = Enumerable.Range(0, generation[0].Error.Error.Length).
                        Select(o => generation.
                            OrderBy(p => p.Error.Error[o]).
                            Take(count).
                            ToArray()).
                        ToArray();
                }

                return new Parents<T>(overall, categories);
            }

            /// <summary>
            /// This randomly creates 1 to N children based on random parents.  It randomly chooses asexual, or any number
            /// of parents
            /// </summary>
            private static IEnumerable<SolutionItem<T>> GetNewChildren<T>(Parents<T> parents, Func<T[], T[]> mutate, Func<T[], SolutionError> getError, Random rand)
            {
                Tuple<int, SolutionItem<T>[]> numParents = GetNumParents(parents, rand);

                SolutionItem<T>[] retVal = null;

                if (numParents.Item1 < 1)
                {
                    throw new ApplicationException("Need at least one parent");
                }
                else if (numParents.Item1 == 1)
                {
                    // Asexual
                    SolutionItem<T> child = numParents.Item2[rand.Next(numParents.Item2.Length)];
                    child = new SolutionItem<T>(mutate(child.Item));
                    retVal = new[] { child };
                }
                else
                {
                    // 2 or more parents
                    retVal = GetNewChildren_Crossover(numParents.Item2, numParents.Item1, mutate, rand);
                }

                // Score them
                foreach (var item in retVal)
                {
                    item.Error = getError(item.Item);
                }

                return retVal.
                    OrderBy(o => o.Error.TotalError).
                    ToArray();
            }

            private static SolutionItem<T>[] GetNewChildren_Crossover<T>(SolutionItem<T>[] parents, int numParents, Func<T[], T[]> mutate, Random rand)
            {
                // Choose which parents to use
                T[][] chosenParents = UtilityCore.RandomRange(0, parents.Length, numParents).
                    Select(o => parents[o].Item).
                    ToArray();

                // Figure out how many slices in each array
                int numSlices = UtilityCore.GetScaledValue(1, chosenParents[0].Length - 1, 0, 1, rand.NextPow(2)).ToInt_Round();
                if (numSlices < 1) numSlices = 1;
                if (numSlices > chosenParents[0].Length - 1) numSlices = chosenParents[0].Length - 1;

                // Crossover
                T[][] children = Crossover(chosenParents, numSlices);

                bool shouldMutate = rand.NextBool();

                // Build return (maybe mutate)
                return children.
                    Select(o => new SolutionItem<T>(shouldMutate ? mutate(o) : o)).
                    ToArray();      //NOTE: This doesn't score and sort.  That is done by the calling method
            }

            /// <summary>
            /// This returns how many parents to use, and which bucket of parents to draw from
            /// </summary>
            private static Tuple<int, SolutionItem<T>[]> GetNumParents<T>(Parents<T> parents, Random rand)
            {
                const double ONE = .25;
                const double TWO = .25;
                const double POWER = 1.5;       // the larger the power, the higher the chance for 3 vs higher

                int totalParents = parents.Combined.Length;

                #region 0,1,2 total parents

                if (totalParents <= 0)
                {
                    throw new ArgumentException("Need at least one parent in the pool");
                }
                else if (totalParents == 1)
                {
                    return Tuple.Create(1, parents.Combined);
                }
                else if (totalParents == 2)
                {
                    int count = rand.NextDouble() <= (ONE / (ONE + TWO)) ? 1 : 2;

                    if (count == 1)
                    {
                        return Tuple.Create(1, parents.Combined);
                    }
                    else
                    {
                        SolutionItem<T>[] bucket = GetNumParents_Bucket_Two(parents, rand);
                        return Tuple.Create(2, bucket);
                    }
                }

                #endregion

                double val = rand.NextDouble();

                #region 1 or 2 return

                if (val <= ONE)
                {
                    return Tuple.Create(1, parents.Combined);
                }
                else if (val <= ONE + TWO)
                {
                    SolutionItem<T>[] bucket = GetNumParents_Bucket_Two(parents, rand);
                    return Tuple.Create(2, bucket);
                }

                #endregion

                SolutionItem<T>[] bucket2 = GetNumParents_Bucket_Many(parents, rand);
                totalParents = bucket2.Length;

                totalParents = Math.Min(totalParents, 7);       // too many parents will just make a scrambled mess

                // Run random through power to give a non linear chance (favor fewer parents)
                int retVal = UtilityCore.GetScaledValue(3, totalParents, 0, 1, rand.NextPow(POWER)).ToInt_Round();

                if (retVal < 3) retVal = 3;
                if (retVal > totalParents) retVal = totalParents;

                return Tuple.Create(retVal, bucket2);
            }
            private static SolutionItem<T>[] GetNumParents_Bucket_Two<T>(Parents<T> parents, Random rand)
            {
                // It was decided there will be two children.  Decide whether to return combined, or one of the specific buckets

                if (parents.Categories == null || rand.NextBool())
                {
                    return parents.Combined;
                }

                // Find categories that could potentially be returned
                List<SolutionItem<T>[]> nonCombo = new List<SolutionItem<T>[]>();

                if (parents.Overall.Length >= 2)
                {
                    nonCombo.Add(parents.Overall);
                }

                nonCombo.AddRange(parents.Categories.Where(o => o.Length >= 2));

                if (nonCombo.Count == 0)
                {
                    // No single category has enough
                    return parents.Combined;
                }
                else
                {
                    // Return one of the specific categories
                    return nonCombo[rand.Next(nonCombo.Count)];
                }
            }
            private static SolutionItem<T>[] GetNumParents_Bucket_Many<T>(Parents<T> parents, Random rand)
            {
                // If categories is null, then combined is the same as overall.  If it's not null, then flip a coin to see if combined
                // should be returned, or one of the specific buckets
                if (parents.Categories == null || rand.NextBool())
                {
                    return parents.Combined;
                }
                else
                {
                    // Choose a specific bucket
                    int index = rand.Next(parents.Categories.Length + 1);       // even chance of choosing one of the categories, or overall

                    if (index == parents.Categories.Length)
                    {
                        return parents.Overall;
                    }
                    else
                    {
                        return parents.Categories[index];
                    }
                }
            }

            /// <summary>
            /// This either picks a random predefined item, or requests a new random item
            /// </summary>
            private static SolutionItem<T>[] GetRandomSamples<T>(int count, SolutionItem<T>[] predefined, Func<T[]> getNew, Func<T[], SolutionError> getError, Random rand)
            {
                var retVal = new SolutionItem<T>[count];

                // Create and score them
                for (int cntr = 0; cntr < count; cntr++)
                {
                    if (predefined != null && rand.NextBool())
                    {
                        retVal[cntr] = predefined[rand.Next(predefined.Length)];        // predefined have already been scored
                    }
                    else
                    {
                        retVal[cntr] = new SolutionItem<T>(getNew());
                        retVal[cntr].Error = getError(retVal[cntr].Item);
                    }
                }

                // Always keep a generation sorted
                return retVal.
                    OrderBy(o => o.Error.TotalError).
                    ToArray();
            }

            /// <summary>
            /// This just converts to a solution item (which is a wrapper), then gets the error
            /// </summary>
            private static SolutionItem<T>[] GetPredefined<T>(T[][] predefined, Func<T[], SolutionError> getError)
            {
                if (predefined == null || predefined.Length == 0)
                {
                    return null;
                }

                SolutionItem<T>[] retVal = new SolutionItem<T>[predefined.Length];

                for (int cntr = 0; cntr < predefined.Length; cntr++)
                {
                    retVal[cntr] = new SolutionItem<T>(predefined[cntr]);
                    retVal[cntr].Error = getError(retVal[cntr].Item);
                }

                // no need to sort these

                return retVal;
            }

            #endregion
        }

        #endregion

        /// <summary>
        /// This will create a bunch of random samples.  Then, score them breed/mutate them, repeat until a satisfactory
        /// solution is found
        /// </summary>
        public static SolutionResult<T> DiscoverSolution<T>(DiscoverSolutionDelegates<T> delegates, DiscoverSolutionOptions<T> options = null)
        {
            return Discoverer.DiscoverSolution(delegates, options);
        }

        /// <summary>
        /// This does crossover of multiple parents (genetic algorithm)
        /// NOTE: All parent arrays need to be the same size
        /// </summary>
        public static T[][] Crossover<T>(T[][] parents, int numSlices)
        {
            return CrossoverWorker.Crossover(parents, numSlices);
        }
    }

    #region Class: DiscoverSolutionOptions

    public class DiscoverSolutionOptions<T>
    {
        /// <summary>
        /// This is the number of items to run at once
        /// </summary>
        /// <remarks>
        /// Winning parents don't count toward this size.  Say there are 4 error functions, and parent size is 5.  That means
        /// 25 parents or more get carried over to the next generation (4sub*5 + 1global*5 + combos*5)
        /// 
        /// If generation size is 100, then 100 children get made, not 75
        /// </remarks>
        public int GenerationSize = 100;

        /// <summary>
        /// This is how many elites to copy to the next generation without modification
        /// </summary>
        public int NumStraightCopies = 5;

        /// <summary>
        /// Every time there is another step/epoch/generation, this is how many brand new random items to create
        /// </summary>
        /// <remarks>
        /// Without new blood, they could stagnate, even though there is a very low chance of a new random item
        /// beating established items
        /// </remarks>
        public int NumNewEachStep = 1;

        /// <summary>
        /// 
        /// </summary>
        public int MaxIterations = 100000;

        /// <summary>
        /// Once a solution's error is less than this, it is declared the winner (unless max iterations is reached first)
        /// </summary>
        public double StopError = .01d;

        /// <summary>
        /// If you have some previous best winners, you can pass them here
        /// </summary>
        /// <remarks>
        /// When coming up with a generation, this will draw from this list and new random entries
        /// 
        /// Say the ship had a mass recalculation.  The previous best solution is probably close to the new ideal,
        /// so give that solution here
        /// 
        /// Or say you have a solution for going up and one for going right.  If you now want a solution for going
        /// up and right, pass those two here
        /// </remarks>
        public T[][] Predefined = null;
    }

    #endregion

    #region Class: DiscoverSolutionDelegates<T>

    public class DiscoverSolutionDelegates<T>
    {
        //****************** Required ******************
        /// <summary>
        /// This requests a new random sample
        /// </summary>
        public Func<T[]> GetNewSample = null;

        /// <summary>
        /// This should evaluate an item, and return scores.  The closer to zero the better
        /// </summary>
        /// <remarks>
        /// This is an array to allow for multiple criteria to be evaluated.  Each criteria will get its own set of winning
        /// parents that get carried over to the next generation
        /// 
        /// There will also be one set of global winners that get carried over (entries with the lowest TotalError)
        /// </remarks>
        public Func<T[], SolutionError> GetError = null;

        /// <summary>
        /// The caller should pick random items in the array, and mutate them slightly
        /// </summary>
        public Func<T[], T[]> Mutate = null;

        //****************** Optional ******************
        /// <summary>
        /// This isn't needed by the algorithm, it gets raised each time there's a new winner.  This is useful if the caller
        /// wants to use results immediately, even if they aren't optimal
        /// </summary>
        public Action<SolutionResult<T>> NewBestFound = null;

        /// <summary>
        /// The caller can set this if they want to stop early
        /// </summary>
        public CancellationToken Cancel;
    }

    #endregion
    #region Class: SolutionError

    public class SolutionError
    {
        /// <summary>
        /// Use this overload if you only have a single error category
        /// </summary>
        public SolutionError(double error)
        {
            this.Error = new[] { error };
            this.TotalError = error;
        }
        public SolutionError(double[] error, double totalError)
        {
            this.Error = error;
            this.TotalError = totalError;
        }

        /// <summary>
        /// Each element in this will be an error score for a particular aspect
        /// NOTE: All solution entries must have the same size error array for all generations
        /// </summary>
        /// <remarks>
        /// If you think of this algorithm as trying to satisfy a bunch of unit tests, then each element is the result of one
        /// unit test
        /// 
        /// When choosing parents for the next generation, each element of this array will generate its own pool of parents
        /// that got the lowest error (for that element)
        /// </remarks>
        public readonly double[] Error;

        /// <summary>
        /// This is the sum of each error element.  The overall winner will be the items with this as the lowest score
        /// </summary>
        /// <remarks>
        /// This doesn't need to be a simple sum.  You'll probably end up with something like:
        /// TotalError = A*Error[0] + B*Error[1] + ... N*Error[n]
        /// 
        /// This is because some error categories will be more important than others
        /// </remarks>
        public readonly double TotalError;
    }

    #endregion

    #region Class: SolutionResult<T>

    public class SolutionResult<T>
    {
        public SolutionResult(T[] item, SolutionResultType reason, SolutionError error, double[] history)
        {
            this.Item = item;
            this.Reason = reason;
            this.Error = error;
            this.History = history;
        }

        public readonly T[] Item;

        public readonly SolutionResultType Reason;
        /// <summary>
        /// This is the item's score
        /// </summary>
        public readonly SolutionError Error;

        /// <summary>
        /// This is a report of the top score (lowest error) of each run, up to and including this one
        /// </summary>
        public readonly double[] History;
    }

    #endregion
    #region Enum: SolutionResultType

    public enum SolutionResultType
    {
        Final_ErrorThreshold,
        Intermediate_NewBest,
        Premature_Cancelled,
        Premature_MaxIterations,
    }

    #endregion
}
