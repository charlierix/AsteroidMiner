using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Game.HelperClassesCore;
using Game.HelperClassesCore.Threads;

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
            #region Class: SolutionWorker<T>

            private class SolutionWorker<T> : IRoundRobinWorker
            {
                #region Declaration Section

                private readonly DiscoverSolutionDelegates<T> _delegates;
                private readonly DiscoverSolutionOptions<T> _options;

                // These are stored in the worker thread (constructor gets called in a main thread, so it can't initialize things without memory barriers)
                private Random _rand = null;

                private SolutionItem<T>[] _generation = null;
                private SolutionItem<T>[] _predefined = null;
                private SolutionItem<T> _currentWinner = null;

                private List<double> _errorHistory = null;       //TODO: A simple list of arrays is too simplistic.  Each category should get its own top performer, as well as a global top performing score

                private int _iterationCntr = 0;
                private bool _isFinished = false;

                #endregion

                #region Constructor

                public SolutionWorker(DiscoverSolutionDelegates<T> delegates, DiscoverSolutionOptions<T> options)
                {
                    _delegates = delegates;
                    _options = options;
                    this.Token = TokenGenerator.NextToken();
                }

                #endregion

                #region IRoundRobinWorker Members

                public bool Step()
                {
                    if (_rand == null)
                    {
                        // This is the first time getting called.  Can't do this in the constructor, because the constructor is called in a different thread
                        _rand = StaticRandom.GetRandomForThread();
                        _errorHistory = new List<double>();
                        _predefined = GetPredefined(_options.Predefined, _delegates.GetError);
                    }

                    // Check for finished cases
                    if (_isFinished)
                    {
                        throw new InvalidOperationException("Step can't be called after it has returned false");
                    }
                    else if (_delegates.Cancel.IsCancellationRequested)
                    {
                        OnFinished(SolutionResultType.Premature_Cancelled);
                        return false;
                    }
                    else if (_iterationCntr >= _options.MaxIterations)
                    {
                        OnFinished(SolutionResultType.Premature_MaxIterations);
                        return false;
                    }

                    _iterationCntr++;

                    //TODO: Support species
                    //https://codesachin.wordpress.com/2015/12/26/fuzzy-speciation-in-genetic-algorithms-using-k-d-trees/
                    //
                    //Func<T[],T[],double> getSpeciesDiff
                    //
                    //This will group items that are genetically similar to each other, crossover will choose between parents of the same species


                    // Get the next generation
                    if (_generation == null)
                    {
                        // First time
                        _generation = GetRandomSamples<T>(_options.GenerationSize, _predefined, _delegates.GetNewSample, _delegates.GetError, _rand);
                    }
                    else
                    {
                        // Breed them (and rescore)
                        _generation = Step(_generation, _options, _delegates, _predefined, _rand);
                    }

                    _errorHistory.Add(_generation[0].Error.TotalError);

                    // Check for new best
                    if (_delegates.NewBestFound != null && (_currentWinner == null || _generation[0].Error.TotalError < _currentWinner.Error.TotalError))
                    {
                        _currentWinner = _generation[0];
                        _delegates.NewBestFound(new SolutionResult<T>(_currentWinner.Item, SolutionResultType.Intermediate_NewBest, _currentWinner.Error, _errorHistory.ToArray()));
                    }

                    // Check for success
                    if (_generation[0].Error.TotalError < _options.StopError)        // they are sorted by sumerror
                    {
                        OnFinished(SolutionResultType.Final_ErrorThreshold);
                        return false;
                    }

                    return true;
                }

                public long Token
                {
                    get;
                    private set;
                }

                #endregion

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

                private void OnFinished(SolutionResultType result)
                {
                    if (_isFinished)
                    {
                        throw new InvalidOperationException("This method should only be called once");
                    }

                    // Raise event
                    if (_delegates.FinalFound != null)
                    {
                        if (_generation == null)
                        {
                            _delegates.FinalFound(new SolutionResult<T>(null, result, null, _errorHistory.ToArray()));      // this should only happen on a cancel
                        }
                        else
                        {
                            _delegates.FinalFound(new SolutionResult<T>(_generation[0].Item, result, _generation[0].Error, _errorHistory.ToArray()));
                        }
                    }

                    // Mark this class as done
                    _isFinished = true;
                }

                #endregion
            }

            #endregion

            public static void DiscoverSolution<T>(DiscoverSolutionDelegates<T> delegates, DiscoverSolutionOptions<T> options = null)
            {
                options = options ?? new DiscoverSolutionOptions<T>();

                SolutionWorker<T> worker = new SolutionWorker<T>(delegates, options);

                if (options.ThreadShare == null)
                {
                    while (worker.Step()) { }
                }
                else
                {
                    options.ThreadShare.Add(worker);
                }
            }
        }

        #endregion
        #region Class: Discoverer2

        private static class Discoverer2
        {
            #region Class: SolutionItem<T>

            private class SolutionItem<T>
            {
                public SolutionItem(T[] item)
                {
                    this.Item = item;
                }

                public readonly T[] Item;

                public double[] Score { get; set; }
            }

            #endregion
            #region Class: Parents<T>

            private class Parents<T>
            {
                public Parents(ParentGroup<T>[] groups)
                {
                    this.Groups = groups;
                }

                /// <summary>
                /// These roots are items at score[0].  The first level of children are items at score[1]
                /// </summary>
                /// <remarks>
                /// There will only be one element at this level, unless there is speciation
                /// </remarks>
                public readonly ParentGroup<T>[] Groups;

                public IEnumerable<SolutionItem<T>> GetLeafItems()
                {
                    return this.Groups.SelectMany(o => o.GetLeafItems());
                }
                public IEnumerable<ParentGroup<T>> GetLeafGroups()
                {
                    return this.Groups.SelectMany(o => o.GetLeafGroups());
                }

                public IEnumerable<ParentGroup<T>> GetAllGroups()
                {
                    return this.Groups.SelectMany(o => o.GetAllGroups());
                }
            }

            #endregion
            #region Class: ParentGroup<T>

            private class ParentGroup<T>
            {
                public ParentGroup(int scoreIndex, bool isScoreAscending, SolutionItem<T>[] items)
                {
                    this.Items = items;
                }

                // These are just copied here for convenience
                public readonly int ScoreIndex;
                public readonly bool IsScoreAscending;

                /// <summary>
                /// These are winning items at this node's level (this.Index)
                /// </summary>
                /// <remarks>
                /// In order for an item to make it to this level, it needs to score well enough to make into into the previous
                /// layers
                /// 
                /// NOTE: These items are duplicates of items in the previous layer.  But each layer will have fewer items
                /// </remarks>
                public readonly SolutionItem<T>[] Items;

                //TODO: If they want speciation, store information about this node (node center)

                /// <summary>
                /// These are sets at score[scoreIndex+1]
                /// </summary>
                /// <remarks>
                /// There will only be one element unless there is speciation
                /// </remarks>
                public ParentGroup<T>[] Groups { get; set; }

                public IEnumerable<SolutionItem<T>> GetLeafItems()
                {
                    if (this.Groups == null)
                    {
                        return this.Items;
                    }
                    else
                    {
                        return this.Groups.SelectMany(o => o.GetLeafItems());
                    }
                }
                public IEnumerable<ParentGroup<T>> GetLeafGroups()
                {
                    if (this.Groups == null)
                    {
                        yield return this;
                    }
                    else
                    {
                        foreach (var group in this.Groups.SelectMany(o => o.GetLeafGroups()))
                        {
                            yield return group;
                        }
                    }
                }

                public IEnumerable<ParentGroup<T>> GetAllGroups()
                {
                    yield return this;

                    if (this.Groups != null)
                    {
                        foreach (var group in this.Groups.SelectMany(o => o.GetAllGroups()))
                        {
                            yield return group;
                        }
                    }
                }
            }

            #endregion

            #region Class: SolutionWorker<T>

            private class SolutionWorker<T> : IRoundRobinWorker
            {
                #region Declaration Section

                private readonly DiscoverSolutionDelegates2<T> _delegates;
                private readonly DiscoverSolutionOptions2<T> _options;

                // These are stored in the worker thread (constructor gets called in a main thread, so it can't initialize things without memory barriers)
                private Random _rand = null;

                private SolutionItem<T>[] _generation = null;
                private SolutionItem<T>[] _predefined = null;
                private SolutionItem<T> _currentWinner = null;

                private List<double[]> _scoreHistory = null;

                private int _iterationCntr = 0;
                private bool _isFinished = false;

                #endregion

                #region Constructor

                public SolutionWorker(DiscoverSolutionDelegates2<T> delegates, DiscoverSolutionOptions2<T> options)
                {
                    _delegates = delegates;
                    _options = options;
                    this.Token = TokenGenerator.NextToken();
                }

                #endregion

                #region IRoundRobinWorker Members

                public bool Step()
                {
                    if (_rand == null)
                    {
                        // This is the first time getting called.  Can't do this in the constructor, because the constructor is called in a different thread
                        _rand = StaticRandom.GetRandomForThread();
                        _scoreHistory = new List<double[]>();
                        _predefined = GetPredefined(_options.Predefined, _delegates.GetScore);
                    }

                    // Check for finished cases
                    if (_isFinished)
                    {
                        throw new InvalidOperationException("Step can't be called after it has returned false");
                    }
                    else if (_delegates.Cancel.IsCancellationRequested)
                    {
                        OnFinished(SolutionResultType.Premature_Cancelled);
                        return false;
                    }
                    else if (_iterationCntr >= _options.MaxIterations)
                    {
                        OnFinished(SolutionResultType.Premature_MaxIterations);
                        return false;
                    }

                    _iterationCntr++;

                    if (_generation == null)
                    {
                        // First time
                        _generation = GetRandomSamples<T>(_options.GenerationSize, _predefined, _delegates.GetNewSample, _delegates.GetScore, _rand);
                        return true;
                    }


                    // Breed them (and rescore)
                    // Take the best performers, and use them as parents for the next generation
                    Parents<T> parents = GetParents(_generation, _options, _delegates);


                    string excelDump_Generation = string.Join("\r\n", _generation.Select(o => string.Join("\t", o.Score)));
                    string excelDump_History = string.Join("\r\n", _scoreHistory.Select(o => string.Join("\t", o)));
                    string excelDump_Parents = GetExcelDump(parents, _options.ScoreAscendDescend.Length);



                    _generation = Step(parents, _options, _delegates, _predefined, _rand);

                    //TODO: This should be handed the current parents
                    SolutionItem<T> winner = GetWinner(parents);

                    _scoreHistory.Add(winner.Score);

                    // Check for new best
                    if (_delegates.NewBestFound != null && (_currentWinner == null || IsBetter(winner, _currentWinner, _options.ScoreAscendDescend)))
                    {
                        _currentWinner = winner;
                        _delegates.NewBestFound(new SolutionResult2<T>(_currentWinner.Item, SolutionResultType.Intermediate_NewBest, _currentWinner.Score, _scoreHistory.ToArray()));
                    }

                    //TODO: May want an optional delegate for IsGoodEnough
                    // Check for success
                    //if (_generation[0].Error.TotalError < _options.StopError)        // they are sorted by sumerror
                    //{
                    //    OnFinished(SolutionResultType.Final_ErrorThreshold);
                    //    return false;
                    //}

                    return true;
                }

                public long Token
                {
                    get;
                    private set;
                }

                #endregion

                #region Private Methods

                private static SolutionItem<T>[] Step<T>(Parents<T> parents, DiscoverSolutionOptions2<T> options, DiscoverSolutionDelegates2<T> delegates, SolutionItem<T>[] predefined, Random rand)
                {
                    const double NEWPERCENT = .01;

                    var retVal = new List<SolutionItem<T>>();

                    // Brand new samples
                    int numNew = Math.Max(1, (options.GenerationSize * NEWPERCENT).ToInt_Round());
                    retVal.AddRange(GetRandomSamples<T>(numNew, predefined, delegates.GetNewSample, delegates.GetScore, rand));       // this also scores them

                    // Children of top performers
                    while (retVal.Count < options.GenerationSize)
                    {
                        retVal.AddRange(GetNewChildren(parents, delegates.Mutate, delegates.GetScore, rand));
                    }

                    //NOTE: Adding parents at the end, so it doesn't affect generation size
                    retVal.AddRange(parents.GetLeafItems());       // no need to rescore these

                    return retVal.ToArray();
                }

                /// <summary>
                /// This either picks a random predefined item, or requests a new random item
                /// </summary>
                private static SolutionItem<T>[] GetRandomSamples<T>(int count, SolutionItem<T>[] predefined, Func<T[]> getNew, Func<T[], double[]> getScore, Random rand)
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
                            retVal[cntr].Score = getScore(retVal[cntr].Item);
                        }
                    }

                    return retVal.ToArray();
                }

                private static Parents<T> GetParents<T>(SolutionItem<T>[] generation, DiscoverSolutionOptions2<T> options, DiscoverSolutionDelegates2<T> delegates)
                {
                    ParentGroup<T>[] groups = GetParents_ScoreIndex(generation, options, delegates, 0);
                    return new Parents<T>(groups);
                }
                /// <summary>
                /// This returns the best examples for Score[scoreIndex] (and recurses for the rest of the scores after scoreIndex)
                /// </summary>
                private static ParentGroup<T>[] GetParents_ScoreIndex<T>(SolutionItem<T>[] items, DiscoverSolutionOptions2<T> options, DiscoverSolutionDelegates2<T> delegates, int scoreIndex)
                {
                    // Sort on score[index]
                    SolutionItem<T>[] candidates = GetParents_Sort(items, scoreIndex, options.ScoreAscendDescend[scoreIndex]);

                    // Get some best performers
                    SolutionItem<T>[] best = GetParents_Best(candidates, scoreIndex, options.ScoreAscendDescend[scoreIndex]);

                    // Speciate
                    ParentGroup<T>[] retVal = GetParents_Species(best, delegates.GetSpeciesPosition, scoreIndex, options.ScoreAscendDescend[scoreIndex]);

                    // ScoreIndex + 1
                    if (scoreIndex < options.ScoreAscendDescend.Length - 1)
                    {
                        foreach (ParentGroup<T> group in retVal)
                        {
                            group.Groups = GetParents_ScoreIndex(group.Items, options, delegates, scoreIndex + 1);      // recurse
                        }
                    }

                    return retVal;
                }
                /// <summary>
                /// This sorts on Score[scoreIndex], either ascending or descending
                /// </summary>
                private static SolutionItem<T>[] GetParents_Sort<T>(SolutionItem<T>[] items, int scoreIndex, bool isAscending)
                {
                    IEnumerable<SolutionItem<T>> retVal;

                    if (isAscending)
                    {
                        // Bigger score is better
                        retVal = items.OrderByDescending(o => o.Score[scoreIndex]);
                    }
                    else
                    {
                        // Smaller score is better
                        retVal = items.OrderBy(o => o.Score[scoreIndex]);
                    }

                    return retVal.ToArray();
                }
                /// <summary>
                /// This returns the items with the best score at Score[scoreIndex]
                /// NOTE: items must be sorted by Score[scoreIndex]
                /// </summary>
                private static SolutionItem<T>[] GetParents_Best<T>(SolutionItem<T>[] items, int scoreIndex, bool isAscending)
                {
                    const double MINCOUNT = 3;
                    //const double STDDEVMULT = 1;        // the larger this number, the more restrictive it is (0 would have the threshold be average)
                    const double STDDEVMULT = 0;        // the larger this number, the more restrictive it is (0 would have the threshold be average)

                    // The easiest is to just take the first half.  Count is guaranteed
                    // A little more complex is everything better than average.  Count could get small in some cases, but that might not be a bad thing
                    // Even more complex is to grab everything better that avg (+ or -) (x * std dev).  The count could get even smaller

                    #region get value threshold

                    // Get the average score
                    var avg_stddev = Math1D.Get_Average_StandardDeviation(items.Select(o => o.Score[scoreIndex]));

                    double threshold;
                    if (isAscending)
                    {
                        // Bigger score is better
                        threshold = avg_stddev.Item1 + (avg_stddev.Item2 * STDDEVMULT);
                    }
                    else
                    {
                        // Smaller score is better
                        threshold = avg_stddev.Item1 - (avg_stddev.Item2 * STDDEVMULT);
                    }

                    #endregion

                    List<SolutionItem<T>> retVal = new List<SolutionItem<T>>();

                    for (int cntr = 0; cntr < items.Length; cntr++)
                    {
                        if (cntr < MINCOUNT)
                        {
                            // Don't even bother looking at the score.  The min count hasn't been met yet
                            retVal.Add(items[cntr]);
                            continue;
                        }

                        if (isAscending && items[cntr].Score[scoreIndex] < threshold)
                        {
                            break;
                        }
                        else if (!isAscending && items[cntr].Score[scoreIndex] > threshold)
                        {
                            break;
                        }

                        retVal.Add(items[cntr]);
                    }

                    return retVal.ToArray();
                }
                /// <summary>
                /// This groups the items into species
                /// </summary>
                private static ParentGroup<T>[] GetParents_Species<T>(SolutionItem<T>[] items, Func<T[], double[]> getSpeciesPosition, int scoreIndex, bool isAscendDescend)
                {
                    if (getSpeciesPosition == null)
                    {
                        // There's no way to calculate species, so just return all in one group
                        return new[] { new ParentGroup<T>(scoreIndex, isAscendDescend, items) };
                    }

                    //TODO: Cluster species together
                    //NOTE: Probaly only want to speciate score[0].  All child nodes will have one group at each level.  Otherwise, the
                    //speciation will create far too many subcategories, and there won't be enough items in each group
                    throw new ApplicationException("finish speciation");
                }

                /// <summary>
                /// This randomly creates 1 to N children based on random parents.  It randomly chooses asexual, or any number
                /// of parents
                /// </summary>
                private static IEnumerable<SolutionItem<T>> GetNewChildren<T>(Parents<T> parents, Func<T[], T[]> mutate, Func<T[], double[]> getScore, Random rand)
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
                        item.Score = getScore(item.Item);
                    }

                    return retVal.ToArray();
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
                private static Tuple<int, SolutionItem<T>[]> GetNumParents_ORIG<T>(Parents<T> parents, Random rand)
                {
                    const double ONE = .25;
                    const double TWO = .25;
                    const double POWER = 1.5;       // the larger the power, the higher the chance for 3 vs higher

                    // Leaf groups will be the actual groups of parents.  The preceding branches are just intermediate (each leaf group will represent a species)
                    ParentGroup<T>[] groups = parents.GetLeafGroups().ToArray();

                    // Pick a random group (species), and get its items
                    SolutionItem<T>[] set = groups[rand.Next(groups.Length)].Items;

                    #region 0,1,2 total parents

                    if (set.Length == 0)
                    {
                        throw new ArgumentException("Need at least one parent in the pool");
                    }
                    else if (set.Length == 1)
                    {
                        return Tuple.Create(1, set);
                    }
                    else if (set.Length == 2)
                    {
                        int count = rand.NextDouble() <= (ONE / (ONE + TWO)) ? 1 : 2;
                        return Tuple.Create(count, set);
                    }

                    #endregion

                    double val = rand.NextDouble();

                    #region 1 or 2 return

                    if (val <= ONE)
                    {
                        return Tuple.Create(1, set);
                    }
                    else if (val <= ONE + TWO)
                    {
                        return Tuple.Create(2, set);
                    }

                    #endregion

                    int totalParents = Math.Min(set.Length, 7);       // too many parents will just make a scrambled mess

                    // Run random through power to give a non linear chance (favor fewer parents)
                    int retVal = UtilityCore.GetScaledValue(3, totalParents, 0, 1, rand.NextPow(POWER)).ToInt_Round();

                    if (retVal < 3) retVal = 3;
                    if (retVal > totalParents) retVal = totalParents;

                    return Tuple.Create(retVal, set);
                }
                private static Tuple<int, SolutionItem<T>[]> GetNumParents<T>(Parents<T> parents, Random rand)
                {
                    const double ONE = .25;
                    const double TWO = .25;
                    const double POWER = 1.5;       // the larger the power, the higher the chance for 3 vs higher

                    // Leaf groups will be the actual groups of parents.  The preceding branches are just intermediate (each leaf group will represent a species)
                    //ParentGroup<T>[] groups = parents.GetLeafGroups().ToArray();
                    ParentGroup<T>[] groups = parents.GetAllGroups().ToArray();

                    // Pick a random group (species), and get its items
                    SolutionItem<T>[] set = groups[rand.Next(groups.Length)].Items;

                    #region 0,1,2 total parents

                    if (set.Length == 0)
                    {
                        throw new ArgumentException("Need at least one parent in the pool");
                    }
                    else if (set.Length == 1)
                    {
                        return Tuple.Create(1, set);
                    }
                    else if (set.Length == 2)
                    {
                        int count = rand.NextDouble() <= (ONE / (ONE + TWO)) ? 1 : 2;
                        return Tuple.Create(count, set);
                    }

                    #endregion

                    double val = rand.NextDouble();

                    #region 1 or 2 return

                    if (val <= ONE)
                    {
                        return Tuple.Create(1, set);
                    }
                    else if (val <= ONE + TWO)
                    {
                        return Tuple.Create(2, set);
                    }

                    #endregion

                    int totalParents = Math.Min(set.Length, 7);       // too many parents will just make a scrambled mess

                    // Run random through power to give a non linear chance (favor fewer parents)
                    int retVal = UtilityCore.GetScaledValue(3, totalParents, 0, 1, rand.NextPow(POWER)).ToInt_Round();

                    if (retVal < 3) retVal = 3;
                    if (retVal > totalParents) retVal = totalParents;

                    return Tuple.Create(retVal, set);
                }

                private static SolutionItem<T> GetWinner(Parents<T> parents)
                {
                    // They should have been added in order, best to worst.  So the first item at each step will be the winner

                    ParentGroup<T> group = parents.Groups[0];

                    while (group.Groups != null)
                    {
                        group = group.Groups[0];
                    }

                    return group.Items[0];
                }

                /// <summary>
                /// This just converts to a solution item (which is a wrapper), then gets the error
                /// </summary>
                private static SolutionItem<T>[] GetPredefined<T>(T[][] predefined, Func<T[], double[]> getScore)
                {
                    if (predefined == null || predefined.Length == 0)
                    {
                        return null;
                    }

                    SolutionItem<T>[] retVal = new SolutionItem<T>[predefined.Length];

                    for (int cntr = 0; cntr < predefined.Length; cntr++)
                    {
                        retVal[cntr] = new SolutionItem<T>(predefined[cntr]);
                        retVal[cntr].Score = getScore(retVal[cntr].Item);
                    }

                    return retVal;
                }

                private static bool IsBetter(SolutionItem<T> test, SolutionItem<T> against, bool[] ascendDescend)
                {
                    for (int cntr = 0; cntr < ascendDescend.Length; cntr++)
                    {
                        if (!IsBetter(cntr, test, against, ascendDescend))
                        {
                            return false;
                        }
                    }

                    return true;
                }
                private static bool IsBetter(int index, SolutionItem<T> test, SolutionItem<T> against, bool[] ascendDescend)
                {
                    if (ascendDescend[index])
                    {
                        // Bigger score is better
                        return test.Score[index] > against.Score[index];
                    }
                    else
                    {
                        // Smaller score is better
                        return test.Score[index] < against.Score[index];
                    }
                }

                private static string GetExcelDump(Parents<T> parents, int scoreCount)
                {
                    string gap = new string('\t', scoreCount - 1);

                    #region get dumps of each level

                    List<List<string>> levels = new List<List<string>>();

                    ParentGroup<T>[] groups = parents.Groups;

                    while (groups.Length > 0)
                    {
                        #region dump current level

                        List<string> level = new List<string>();
                        levels.Add(level);

                        for (int cntr = 0; cntr < groups.Length; cntr++)
                        {
                            if (cntr > 0)
                            {
                                level.Add(gap);
                            }

                            level.AddRange(groups[cntr].Items.Select(o => string.Join("\t", o.Score)));
                        }

                        #endregion

                        // Get next level
                        groups = groups.
                            Where(o => o.Groups != null).
                            SelectMany(o => o.Groups).
                            ToArray();
                    }

                    #endregion

                    #region create final string

                    StringBuilder retVal = new StringBuilder(1024);

                    int index = 0;
                    while (true)
                    {
                        bool hadItem = false;

                        StringBuilder line = new StringBuilder(128);

                        foreach (List<string> level in levels)
                        {
                            if (level.Count > index)
                            {
                                line.Append(level[index]);
                                hadItem = true;
                            }
                            else
                            {
                                line.Append(gap);
                            }

                            line.Append("\t\t");
                        }

                        if (!hadItem)
                        {
                            break;
                        }

                        retVal.AppendLine(line.ToString());
                        index++;
                    }

                    #endregion

                    return retVal.ToString();
                }

                private void OnFinished(SolutionResultType result)
                {
                    if (_isFinished)
                    {
                        throw new InvalidOperationException("This method should only be called once");
                    }

                    // Raise event
                    if (_delegates.FinalFound != null)
                    {
                        if (_generation == null)
                        {
                            _delegates.FinalFound(new SolutionResult2<T>(null, result, null, _scoreHistory.ToArray()));      // this should only happen on a cancel
                        }
                        else
                        {
                            _delegates.FinalFound(new SolutionResult2<T>(_generation[0].Item, result, _generation[0].Score, _scoreHistory.ToArray()));
                        }
                    }

                    // Mark this class as done
                    _isFinished = true;
                }

                #endregion
            }

            #endregion

            public static void DiscoverSolution<T>(DiscoverSolutionDelegates2<T> delegates, DiscoverSolutionOptions2<T> options = null)
            {
                options = options ?? new DiscoverSolutionOptions2<T>();

                SolutionWorker<T> worker = new SolutionWorker<T>(delegates, options);

                if (options.ThreadShare == null)
                {
                    while (worker.Step()) { }
                }
                else
                {
                    options.ThreadShare.Add(worker);
                }
            }
        }

        #endregion

        //NOTE: This overload uses the getError and sumError.  Nothing is currently using it, but it might be useful
        public static void DiscoverSolution<T>(DiscoverSolutionDelegates<T> delegates, DiscoverSolutionOptions<T> options = null)
        {
            Discoverer.DiscoverSolution(delegates, options);
        }
        /// <summary>
        /// This will create a bunch of random samples.  Then, score them, breed/mutate them, repeat until a satisfactory
        /// solution is found
        /// </summary>
        public static void DiscoverSolution2<T>(DiscoverSolutionDelegates2<T> delegates, DiscoverSolutionOptions2<T> options = null)
        {
            Discoverer2.DiscoverSolution(delegates, options);
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

        /// <summary>
        /// If you want multiple discoverers to share the same thread, populate this
        /// </summary>
        public RoundRobinManager ThreadShare = null;
    }

    #endregion
    #region Class: DiscoverSolutionOptions2

    public class DiscoverSolutionOptions2<T>
    {
        //****************** Required ******************

        /// <summary>
        /// This is the number of items to run at once
        /// </summary>
        /// <remarks>
        /// Winning parents don't count toward this size.  Say there are 4 error functions, and parent size is 5.  That means
        /// 25 parents or more get carried over to the next generation (4sub*5 + 1global*5 + combos*5)
        /// 
        /// If generation size is 100, then 100 children get made, not 75
        /// </remarks>
        public int GenerationSize = 1000;

        /// <summary>
        /// This is how many elites to copy to the next generation without modification
        /// </summary>
        //public int NumStraightCopies = 5;     // this will be calculated based on generation size

        /// <summary>
        /// Every time there is another step/epoch/generation, this is how many brand new random items to create
        /// </summary>
        /// <remarks>
        /// Without new blood, they could stagnate, even though there is a very low chance of a new random item
        /// beating established items
        /// </remarks>
        //public int NumNewEachStep = 5;        // this will be calculated based on generation size

        /// <summary>
        /// 
        /// </summary>
        public int MaxIterations = 100000;

        /// <summary>
        /// Once a solution's error is less than this, it is declared the winner (unless max iterations is reached first)
        /// </summary>
        //public double StopError = .01d;

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

        /// <summary>
        /// This is the same size as the score array.  It tells whether each sub item is:
        /// True=Ascending (the bigger the score, the better)
        /// False=Descending (the smaller the score, the better)
        /// </summary>
        public bool[] ScoreAscendDescend = null;

        //****************** Optional ******************

        /// <summary>
        /// If you want multiple discoverers to share the same thread, populate this
        /// </summary>
        public RoundRobinManager ThreadShare = null;
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
        /// This gets called when a final solution is found, or some other exit condition (cancel, max iterations, etc)
        /// </summary>
        public Action<SolutionResult<T>> FinalFound = null;

        /// <summary>
        /// The caller can set this if they want to stop early
        /// </summary>
        public CancellationToken Cancel;
    }

    #endregion
    #region Class: DiscoverSolutionDelegates2<T>

    public class DiscoverSolutionDelegates2<T>
    {
        //****************** Required ******************
        /// <summary>
        /// This requests a new random sample
        /// </summary>
        public Func<T[]> GetNewSample = null;

        /// <summary>
        /// This should evaluate an item, and return scores.  The size needs to match options.ScoreAscendDescend
        /// </summary>
        /// <remarks>
        /// This is an array to allow for multiple criteria to be evaluated.  Score[0] will be the most important criteria
        /// </remarks>
        public Func<T[], double[]> GetScore = null;

        /// <summary>
        /// The caller should pick random items in the array, and mutate them slightly
        /// </summary>
        public Func<T[], T[]> Mutate = null;

        //****************** Optional ******************
        /// <summary>
        /// This isn't needed by the algorithm, it gets raised each time there's a new winner.  This is useful if the caller
        /// wants to use results immediately, even if they aren't optimal
        /// </summary>
        public Action<SolutionResult2<T>> NewBestFound = null;

        /// <summary>
        /// This gets called when a final solution is found, or some other exit condition (cancel, max iterations, etc)
        /// </summary>
        public Action<SolutionResult2<T>> FinalFound = null;

        /// <summary>
        /// This returns a position of an item, which can be used in speciation
        /// </summary>
        /// <remarks>
        /// When finding winning parents, they can be grouped together based on distance.  Items that are close
        /// together would be considered their own species.  Crossover within a species should have less chance of
        /// producing garbage
        /// 
        /// http://sharpneat.sourceforge.net/research/speciation-kmeans.html
        /// https://codesachin.wordpress.com/2015/12/26/fuzzy-speciation-in-genetic-algorithms-using-k-d-trees/
        /// </remarks>
        public Func<T[], double[]> GetSpeciesPosition = null;

        /// <summary>
        /// The caller can set this if they want to stop early
        /// </summary>
        public CancellationToken Cancel;
    }

    #endregion

    #region Class: SolutionError

    //NOTE: Went with the arrays (
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
    #region Class: SolutionResult2<T>

    public class SolutionResult2<T>
    {
        public SolutionResult2(T[] item, SolutionResultType reason, double[] score, double[][] history)
        {
            this.Item = item;
            this.Reason = reason;
            this.Score = score;
            this.History = history;
        }

        public readonly T[] Item;

        public readonly SolutionResultType Reason;
        /// <summary>
        /// This is the item's score
        /// </summary>
        public readonly double[] Score;

        /// <summary>
        /// This is a report of the top performing item of each run, up to and including this one
        /// </summary>
        public readonly double[][] History;
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
