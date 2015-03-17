using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Game.HelperClassesCore;
using Game.Newt.v2.GameItems;
using Game.HelperClassesWPF;

namespace Game.Newt.v2.FlyingBeans
{
    #region Class: WinnerList

    /// <summary>
    /// This can either track living instances or dna
    /// </summary>
    public class WinnerList
    {
        #region Class: WinningSet

        public class WinningSet
        {
            public WinningSet(string shipName, Tuple<string, WinningBean[]>[] beansByLineage)
            {
                this.ShipName = shipName;
                this.BeansByLineage = beansByLineage;
            }

            public readonly string ShipName;
            public readonly Tuple<string, WinningBean[]>[] BeansByLineage;
        }

        #endregion
        #region Class: WinningBean

        public class WinningBean
        {
            public WinningBean(ShipDNA dna, double score, double age)
            {
                this.Ship = null;
                this.DNA = dna;

                this.Score = score;
                this.Age = age;
            }
            public WinningBean(Ship ship, double score, double age)
            {
                this.Ship = ship;
                this.DNA = null;

                this.Score = score;
                this.Age = age;
            }

            // Only one of these two will be set
            public readonly Ship Ship;
            public readonly ShipDNA DNA;

            public readonly double Score;
            /// <summary>
            /// This is the age in seconds at the time of recording
            /// </summary>
            public readonly double Age;
        }

        #endregion

        #region Constructor

        public WinnerList()
        {
            //This default constructor is so serialization will work
        }
        public WinnerList(bool tracksLivingInstances, int maxLineages, int maxPerLineage)
        {
            this.TracksLivingInstances = tracksLivingInstances;
            this.MaxLineages = maxLineages;
            this.MaxPerLineage = maxPerLineage;
        }
        public WinnerList(bool tracksLivingInstances, int maxLineages, int maxPerLineage, WinningSet[] current)
            : this(tracksLivingInstances, maxLineages, maxLineages)
        {
            _current = Tuple.Create(DateTime.Now, current);
        }

        #endregion

        #region Public Properties

        private bool? _tracksLivingInstances = null;		// I can't make this readonly, because serialize needs a default constructor
        public bool TracksLivingInstances
        {
            get
            {
                if (_tracksLivingInstances == null)
                {
                    throw new InvalidOperationException("TracksLivingInstances hasn't been set yet");
                }

                return _tracksLivingInstances.Value;
            }
            set
            {
                if (_tracksLivingInstances != null)
                {
                    throw new InvalidOperationException("TracksLivingInstances can only be set once");
                }

                _tracksLivingInstances = value;
            }
        }

        public int MaxLineages
        {
            get;
            set;
        }
        public int MaxPerLineage
        {
            get;
            set;
        }

        private volatile Tuple<DateTime, WinningSet[]> _current = null;
        public WinningSet[] Current
        {
            get
            {
                var current = _current;
                if (current == null)
                {
                    return null;
                }
                else
                {
                    return current.Item2;
                }
            }
        }

        #endregion

        #region Public Methods

        public void StoreWinners(WinningBean[] beans)
        {
            StoreWinnersSprtDoIt(beans, null);
        }
        /// <summary>
        /// This method is only used for live tracking.  You can pass in tokens of ships that have died, and this will remove them
        /// from the list, as well as return the final score of any that were actually removed.
        /// </summary>
        public Tuple<long, WinningBean>[] StoreWinners(WinningBean[] adds, long[] removals)
        {
            if (!this.TracksLivingInstances)
            {
                throw new InvalidOperationException("This method can only be used when tracking live instances");
            }

            return StoreWinnersSprtDoIt(adds, removals);
        }

        /// <summary>
        /// This returns a random winner for the type requested (could return null)
        /// WARNING: Don't modify the returned dna directly, make a copy first
        /// </summary>
        public ShipDNA GetWinner(string name)
        {
            var current = _current;		// store local because it's volatile
            if (current == null)
            {
                return null;
            }

            // Find the set for the name requested
            WinningSet set = current.Item2.Where(o => o.ShipName == name).FirstOrDefault();
            if (set == null)
            {
                return null;
            }

            // Get all the beans across all the lineages
            var beans = set.BeansByLineage.SelectMany(o => o.Item2).ToArray();

            // Pick a random one
            int index = StaticRandom.Next(beans.Length);
            if (beans[index].Ship == null)
            {
                return beans[index].DNA;
            }
            else
            {
                return beans[index].Ship.GetNewDNA();
            }
        }

        /// <summary>
        /// This returns how many lineages are currently being tracked for the name passed in
        /// </summary>
        /// <remarks>
        /// You can compare this returned value to this.MaxLineages to see how many are still available, or whatever
        /// </remarks>
        public int GetNumUsedLineages(string name)
        {
            var current = _current;		// store local because it's volatile
            if (current == null)
            {
                return 0;
            }

            // Find the set for the name requested
            WinningSet set = current.Item2.Where(o => o.ShipName == name).FirstOrDefault();
            if (set == null)
            {
                return 0;
            }

            // Just return the number of lineages
            return set.BeansByLineage.Length;
        }

        //Item1=ShipName, Item2=Lineage, Item3=Score
        public Tuple<string, string, double>[] GetMinsByType()
        {
            var current = _current;
            if (current == null)
            {
                return null;
            }

            List<Tuple<string, string, double>> retVal = new List<Tuple<string, string, double>>();

            foreach (var set in current.Item2)
            {
                foreach (var lineage in set.BeansByLineage)
                {
                    retVal.Add(Tuple.Create(
                        set.ShipName,
                        lineage.Item1,
                        lineage.Item2.Min(o => o.Score)));
                }
            }

            return retVal.ToArray();
        }
        public Tuple<string, string, double>[] GetMaxesByType()
        {
            var current = _current;
            if (current == null)
            {
                return null;
            }

            List<Tuple<string, string, double>> retVal = new List<Tuple<string, string, double>>();

            foreach (var set in current.Item2)
            {
                foreach (var lineage in set.BeansByLineage)
                {
                    retVal.Add(Tuple.Create(
                        set.ShipName,
                        lineage.Item1,
                        lineage.Item2.Max(o => o.Score)));
                }
            }

            return retVal.ToArray();
        }

        #endregion

        #region Private Methods

        private Tuple<long, WinningBean>[] StoreWinnersSprtDoIt(WinningBean[] beans, long[] removals)
        {
            if ((removals == null || removals.Length == 0) && (beans == null || beans.Length == 0))
            {
                return null;
            }

            // Remove - this thread
            Tuple<long, WinningBean>[] retVal = null;
            WinningBean[] reducedBeans = beans;
            if (removals != null && removals.Length > 0)
            {
                retVal = RemoveBeans(out reducedBeans, beans, removals);
            }

            if (reducedBeans != null && reducedBeans.Length > 0)
            {
                int maxLineages = this.MaxLineages;
                int maxPerLineage = this.MaxPerLineage;
                bool tracksLiveInstances = this.TracksLivingInstances;

                #region Merge - other thread

                // Merge the scores passed in with the current winners
                var task = Task.Factory.StartNew(() =>
                {
                    // Merge
                    return GetWinners(_current, reducedBeans, maxLineages, maxPerLineage, tracksLiveInstances);
                });

                #endregion
                #region Store results - this thread

                // Store the results
                task.ContinueWith(result =>
                {
                    var prev = _current;		// since it's volatile, only read once

                    // Store if newer
                    if (prev == null || result.Result.Item1 > prev.Item1)
                    {
                        _current = result.Result;
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());

                #endregion
            }

            // Exit Function
            return retVal;
        }

        /// <summary>
        /// This pulls beans out of the list passed in and the current tree
        /// </summary>
        private Tuple<long, WinningBean>[] RemoveBeans(out WinningBean[] remaining, WinningBean[] beans, long[] removals)
        {
            List<Tuple<long, WinningBean>> retVal = new List<Tuple<long, WinningBean>>();

            // Reduce the list of beans passed in
            remaining = RemoveBeansSprtArgs(retVal, beans, removals);

            //NOTE: Since there could be other threads working this tree, it's possible for removed beans to reemerge.  But that would
            //only happen if StoreWinners is called faster than a thread can process, which shouldn't happen in reality
            _current = RemoveBeansSprtTree(retVal, _current, removals);

            // Exit Function
            return retVal.ToArray();
        }
        private static WinningBean[] RemoveBeansSprtArgs(List<Tuple<long, WinningBean>> returnList, WinningBean[] beans, long[] removals)
        {
            if (beans == null)
            {
                return null;
            }

            List<WinningBean> remaining = new List<WinningBean>();

            for (int cntr = 0; cntr < beans.Length; cntr++)
            {
                long token = beans[cntr].Ship.PhysicsBody.Token;
                if (removals.Contains(token))
                {
                    returnList.Add(Tuple.Create(token, beans[cntr]));
                }
                else
                {
                    remaining.Add(beans[cntr]);
                }
            }

            return remaining.ToArray();
        }
        private static Tuple<DateTime, WinningSet[]> RemoveBeansSprtTree(List<Tuple<long, WinningBean>> returnList, Tuple<DateTime, WinningSet[]> current, long[] removals)
        {
            if (current == null)
            {
                return null;
            }

            List<WinningSet> retVal = new List<WinningSet>();

            foreach (WinningSet set in current.Item2)
            {
                List<Tuple<string, WinningBean[]>> lineages = new List<Tuple<string, WinningBean[]>>();

                foreach (var lineage in set.BeansByLineage)
                {
                    #region Examine lineage

                    List<WinningBean> beans = new List<WinningBean>();

                    foreach (WinningBean bean in lineage.Item2)
                    {
                        #region Examine bean

                        long token = bean.Ship.PhysicsBody.Token;

                        if (removals.Contains(token))
                        {
                            returnList.Add(Tuple.Create(token, bean));
                        }
                        else
                        {
                            beans.Add(bean);
                        }

                        #endregion
                    }

                    // Store lineage if beans remain
                    if (beans.Count > 0)
                    {
                        lineages.Add(Tuple.Create(lineage.Item1, beans.ToArray()));
                    }

                    #endregion
                }

                // Store set if lineages remain
                if (lineages.Count > 0)
                {
                    retVal.Add(new WinningSet(set.ShipName, lineages.ToArray()));
                }
            }

            // Exit Function
            return Tuple.Create(DateTime.Now, retVal.ToArray());
        }

        /// <summary>
        /// This merges the current tree with the beans passed in.  Only keeps the top performers, independent of where they came from
        /// </summary>
        private static Tuple<DateTime, WinningSet[]> GetWinners(Tuple<DateTime, WinningSet[]> existing, WinningBean[] candidates, int maxLineages, int maxPerLineage, bool tracksLivingInstances)
        {
            List<WinningSet> retVal = new List<WinningSet>();

            #region Validate candidates

            if (candidates != null)
            {
                if (tracksLivingInstances)
                {
                    if (candidates.Any(o => o.DNA != null))
                    {
                        throw new ArgumentException("When tracking live instances, raw dna shouldn't be passed in");
                    }
                }
                else
                {
                    if (candidates.Any(o => o.Ship != null))
                    {
                        throw new ArgumentException("When tracking dna, live instances shouldn't be passed in");
                    }
                }
            }

            #endregion

            // Shoot through all the unique names across exising and candidate
            foreach (string name in UtilityCore.Iterate(
                existing == null ? null : existing.Item2.Select(o => o.ShipName),
                tracksLivingInstances ? candidates.Select(o => o.Ship.Name) : candidates.Select(o => o.DNA.ShipName)
                ).Distinct())
            {
                // Each name is treated independently of the other names.  So one name could have far worse winners than
                // others, and it wouldn't matter
                retVal.Add(GetWinnersSprtName(name,
                    existing == null ? null : existing.Item2.Where(o => o.ShipName == name).FirstOrDefault(),
                    tracksLivingInstances ? candidates.Where(o => o.Ship.Name == name).ToArray() : candidates.Where(o => o.DNA.ShipName == name).ToArray(),
                    maxLineages, maxPerLineage, tracksLivingInstances));
            }

            // Sort the list by the highest performer
            return Tuple.Create(DateTime.Now,
                retVal.OrderByDescending(o => o.BeansByLineage.Max(p => p.Item2.Max(q => q.Score))).
                ToArray());
        }
        private static WinningSet GetWinnersSprtName(string name, WinningSet existing, WinningBean[] candidates, int maxLineages, int maxPerLineage, bool tracksLivingInstances)
        {
            if (candidates == null)
            {
                #region Validate Existing

                //TODO: See if the constraints are more restrictive
                //if (existing != null)
                //{
                //    if (existing.BeansByLineage.Length > maxLineages)
                //    {

                //    }
                //}

                #endregion
                return existing;
            }

            // Group the candidates up by lineage
            var candidateLineage = candidates.GroupBy(o => tracksLivingInstances ? o.Ship.Lineage : o.DNA.ShipLineage).ToArray();

            List<Tuple<string, WinningBean[]>> retVal = new List<Tuple<string, WinningBean[]>>();

            // Shoot through all the unique lineages across existing and candidate
            foreach (string lineage in UtilityCore.Iterate(
                existing == null ? null : existing.BeansByLineage.Select(o => o.Item1),
                candidateLineage.Select(o => o.Key)).
                Distinct())
            {
                var matchingCandidate = candidateLineage.Where(o => o.Key == lineage).FirstOrDefault();

                // Get the top beans for this lineage
                retVal.Add(GetWinnersSprtLineage(lineage,
                    existing == null ? null : existing.BeansByLineage.Where(o => o.Item1 == lineage).FirstOrDefault(),
                    matchingCandidate == null ? null : matchingCandidate.ToArray(),
                    maxPerLineage, tracksLivingInstances));
            }

            // Sort, take top
            return new WinningSet(name,
                retVal.OrderByDescending(o => o.Item2.Max(p => p.Score)).
                Take(maxLineages).
                ToArray());
        }
        private static Tuple<string, WinningBean[]> GetWinnersSprtLineage(string lineage, Tuple<string, WinningBean[]> existing, WinningBean[] candidates, int maxPerLineage, bool tracksLivingInstances)
        {

            List<WinningBean> distinctBeans = new List<WinningBean>();

            if (tracksLivingInstances)
            {
                // Existing and candidate lists could have the same instance of a bean (from different moments in time).  Group by
                // bean instances
                var beanGroups = UtilityCore.Iterate(existing == null ? null : existing.Item2, candidates).
                    GroupBy(o => o.Ship.PhysicsBody).		// the physics body implements IComparable (compares on token)
                    ToArray();

                // Now pull only the best example from each instance of a bean
                foreach (var group in beanGroups)
                {
                    distinctBeans.Add(group.OrderByDescending(o => o.Score).First());
                }
            }
            else
            {
                // DNA should never be shared between existing and new, so everything is unique
                distinctBeans = UtilityCore.Iterate(existing == null ? null : existing.Item2, candidates).ToList();
            }

            // Sort by score, and take the top performers
            WinningBean[] retVal = distinctBeans.
                OrderByDescending(o => o.Score).
                Take(maxPerLineage).
                ToArray();

            // Exit Function
            return Tuple.Create(lineage, retVal);
        }

        #endregion
    }

    #endregion

    #region Class: CandidateWinners

    /// <summary>
    /// This holds a list of ships that need to be tested for final ranking
    /// </summary>
    public class CandidateWinners
    {
        #region Declaration Section

        private const int MAX = 33;

        private readonly object _lock = new object();		// there's not much point in getting more elaborate than a lock, add and pop shouldn't be called too frequently

        private SortedList<string, List<Tuple<ShipDNA, double>>> _list = new SortedList<string, List<Tuple<ShipDNA, double>>>();

        #endregion

        #region Public Methods

        public void Add(ShipDNA ship, double score)
        {
            lock (_lock)
            {
                if (!_list.ContainsKey(ship.ShipName))
                {
                    _list.Add(ship.ShipName, new List<Tuple<ShipDNA, double>>());
                }

                _list[ship.ShipName].Add(Tuple.Create(ship, score));

                if (_list[ship.ShipName].Count > MAX * 3)
                {
                    PurgeLowScores(_list[ship.ShipName]);
                }
            }
        }

        public ShipDNA Pop(string name)
        {
            lock (_lock)
            {
                if (!_list.ContainsKey(name))
                {
                    return null;
                }

                var list = _list[name];

                double maxScore = double.MinValue;
                int index = -1;

                // Find the highest scorer
                for (int cntr = 0; cntr < list.Count; cntr++)
                {
                    if (list[cntr].Item2 > maxScore)
                    {
                        maxScore = list[cntr].Item2;
                        index = cntr;
                    }
                }

                if (index < 0)
                {
                    return null;
                }
                else
                {
                    // Pop it
                    ShipDNA retVal = list[index].Item1;
                    list.RemoveAt(index);

                    return retVal;
                }
            }
        }

        #endregion

        #region Private Methods

        private static void PurgeLowScores(List<Tuple<ShipDNA, double>> list)
        {
            var best = list.OrderByDescending(o => o.Item2).Take(MAX).ToArray();

            list.Clear();
            list.AddRange(best);
        }

        #endregion
    }

    #endregion

    #region Class: WinnerManager

    //NOTE: This class is intermixing the words finalist and candidate as basically the same thing
    public class WinnerManager
    {
        #region Class: TrackingCandidate

        /// <summary>
        /// This keeps track of finalist instances.  This is handed dna of a ship that needs to have several instances spawned to prove
        /// its score isn't a fluke.
        /// </summary>
        /// <remarks>
        /// Every time you spawn an instance, store its token.  When that instance dies, store its score.  When you're done spawning
        /// enough instances, get the average score.
        /// </remarks>
        private class TrackingCandidate
        {
            #region Declaration Section

            private SortedList<long, double?> _history = new SortedList<long, double?>();

            #endregion

            #region Constructor

            public TrackingCandidate(ShipDNA dna)
            {
                this.DNA = dna;
                this.Token = TokenGenerator.NextToken();
            }

            #endregion

            #region Public Properties

            //NOTE: This is NOT the ship's token, it is a unique token given to this object
            public readonly long Token;

            public readonly ShipDNA DNA;

            public int NumStarted
            {
                get
                {
                    return _history.Keys.Count;
                }
            }
            public int NumFinished
            {
                get
                {
                    return _history.Keys.Where(o => _history[o] != null).Count();
                }
            }

            #endregion

            #region Public Methods

            public void StartedShip(long token)
            {
                _history.Add(token, null);
            }
            public void FinishedShip(long token, double score)
            {
                _history[token] = score;
            }

            public bool Contains(long token)
            {
                return _history.Keys.Contains(token);
            }

            public double GetAverage()
            {
                if (this.NumStarted != this.NumFinished)
                {
                    throw new InvalidOperationException("This method can only be called when they are all finished");
                }

                return _history.Values.Sum(o => o.Value) / Convert.ToDouble(_history.Keys.Count);
            }

            public double[] GetFinishedScores()
            {
                return _history.Values.Where(o => o != null).Select(o => o.Value).ToArray();
            }

            public long[] GetLivingCandidates()
            {
                return _history.Keys.Where(o => _history[o] == null).ToArray();
            }

            #endregion
        }

        #endregion

        #region Declaration Section

        private List<TrackingCandidate> _finalists = new List<TrackingCandidate>();

        /// <summary>
        /// Finalists are tracked separately from first spawn living, and the final scores.  The finalist will be on this list even if it
        /// isn't performing very well
        /// </summary>
        /// <remarks>
        /// At first, I was keeping the live finalists with the other live ships.  But one run may not make the live list and be scored
        /// as a zero.  It got frustrating to watch the live scores be 25+% higher than the final score, but the final score would never
        /// be updated.  I would let the simulation run all night and not get very many generations.
        /// 
        /// By always monitoring a finalist's score, it will get an accurate average score instead of being severly penalized for not
        /// making the live list.
        /// </remarks>
        private SortedList<long, WinnerList.WinningBean> _livingFinalistTopScores = new SortedList<long, WinnerList.WinningBean>();

        /// <summary>
        /// Whenever a ship is removed, its token is added to this list.  Then during the next winner scan, this list is handed to the live winner
        /// list.  If that removed ship was a live winner, it is placed in the candidate queue (from there, that dna may end up in the final winner
        /// list)
        /// </summary>
        private List<long> _removedTokens = new List<long>();

        #endregion

        #region Constructor

        public WinnerManager(WinnerList live, CandidateWinners candidates, WinnerList final, int finalistCount)
        {
            this.Live = live;
            this.Candidates = candidates;
            this.Final = final;
            this.FinalistCount = finalistCount;
        }

        #endregion

        #region Public Properties

        private WinnerList _live = null;
        /// <summary>
        /// This holds ships that are currently living, that have a score large enough to be tracked
        /// </summary>
        public WinnerList Live
        {
            get
            {
                return _live;
            }
            set
            {
                _live = value;
                _finalists.Clear();
                _livingFinalistTopScores.Clear();
            }
        }

        private CandidateWinners _candidates = null;
        /// <summary>
        /// When a ship in the live list dies, its dna is stored here, and several instances need to be spawned to get an
        /// average score
        /// </summary>
        public CandidateWinners Candidates
        {
            get
            {
                return _candidates;
            }
            set
            {
                _candidates = value;
                _finalists.Clear();
                _livingFinalistTopScores.Clear();
            }
        }

        /// <summary>
        /// These are the current top scores of any currently living finalists
        /// </summary>
        //public WinnerList.WinningBean[] CandidateCurrentScores
        //{
        //    get
        //    {
        //        return _livingFinalistTopScores.Values.ToArray();
        //    }
        //}

        private WinnerList _final = null;
        /// <summary>
        /// If the final average score from the candidate list is good enough, it will be stored in this list
        /// </summary>
        public WinnerList Final
        {
            get
            {
                return _final;
            }
            set
            {
                _final = value;
                _finalists.Clear();
                _livingFinalistTopScores.Clear();
            }
        }

        /// <summary>
        /// This is how many candidates to spawn and average scores together before attempting to add to the final list
        /// </summary>
        public int FinalistCount
        {
            get;
            set;
        }

        #endregion

        #region Public Methods

        public void ShipCreated(long candidateToken, Ship ship)
        {
            // Find the candidate list that this referes to
            TrackingCandidate finalist = _finalists.Where(o => o.Token == candidateToken).FirstOrDefault();
            if (finalist == null)
            {
                throw new ArgumentException("Didn't find the candidate referenced by the token");
            }

            // Store the ship's token
            finalist.StartedShip(ship.PhysicsBody.Token);
        }
        public void ShipDied(Ship ship)
        {
            _removedTokens.Add(ship.PhysicsBody.Token);
        }

        /// <summary>
        /// This returns a candidate that still needs to be spawned and scored
        /// </summary>
        public Tuple<long, ShipDNA> GetCandidate(string shipName)
        {
            // Find the finalists that still need more spawned
            TrackingCandidate[] finalists = _finalists.Where(o => o.DNA.ShipName == shipName && o.NumStarted < this.FinalistCount).ToArray();

            TrackingCandidate finalist = null;
            if (finalists.Length > 0)
            {
                // Pick a random one
                finalist = finalists[StaticRandom.Next(finalists.Length)];
            }
            else
            {
                // See if there are any candidates waiting to go in the finalist list
                ShipDNA dna = this.Candidates.Pop(shipName);
                if (dna == null)
                {
                    return null;
                }

                finalist = new TrackingCandidate(dna);
                _finalists.Add(finalist);
            }

            // Exit Function
            return Tuple.Create(finalist.Token, finalist.DNA);
        }

        /// <summary>
        /// This is the list of candidates that are currently alive (based on calls to ShipCreated and ShipDied)
        /// </summary>
        /// <remarks>
        /// I made this as a convenience to know which ships to score before calling RefreshWinners.  The tokens returned may not be
        /// in CandidateCurrentScores yet, because that is during each call to RefreshWinners.
        /// 
        /// NOTE: When calling RefreshWinners, any ships that are living candidates should always be scored.  The other living ships
        /// will probably only be scored if they exceed some threshold
        /// </remarks>
        public long[] GetLivingCandidates()
        {
            return _finalists.SelectMany(o => o.GetLivingCandidates()).ToArray();
        }

        /// <summary>
        /// This returns the scores of the currently living candidates.
        /// Item1=CurrentlyLiving
        /// Item2=Scores of previous runs
        /// </summary>
        public Tuple<WinnerList.WinningBean, double[]>[] GetCandidateScoreDump()
        {
            List<Tuple<WinnerList.WinningBean, double[]>> retVal = new List<Tuple<WinnerList.WinningBean, double[]>>();

            foreach (long token in _livingFinalistTopScores.Keys)
            {
                TrackingCandidate finalist = _finalists.Where(o => o.Contains(token)).FirstOrDefault();
                if (finalist == null)
                {
                    throw new ApplicationException("Didn't find ship's token in the finalist list: " + token.ToString());
                }

                retVal.Add(Tuple.Create(_livingFinalistTopScores[token], finalist.GetFinishedScores()));
            }

            // Exit Function
            return retVal.ToArray();
        }

        public bool HasLivingCandidate(string shipName)
        {
            return _finalists.Any(o => o.DNA.ShipName == shipName && o.NumStarted > o.NumFinished);
        }

        public void RefreshWinners(WinnerList.WinningBean[] liveScores, WinnerList.WinningBean[] liveCandidateScores)
        {
            // Get the remove tokens
            long[] removedTokens = _removedTokens.ToArray();
            _removedTokens.Clear();

            // Refresh the live list
            var winningLiveRemovals = this.Live.StoreWinners(liveScores, removedTokens);

            // Refresh the finalist scores
            var candidateRemovals = UpdateFinalistScores(liveCandidateScores, removedTokens);

            // Of the ships that died, track candidates (not all dead ships will be tracked in candidates)
            TransitionToCandidates(winningLiveRemovals, candidateRemovals, removedTokens);

            // Look for candidates that are finished being tested, and see if they are good enough to be stored in the final list
            TransitionToFinal();
        }

        #endregion

        #region Private Methods

        private Tuple<long, WinnerList.WinningBean>[] UpdateFinalistScores(WinnerList.WinningBean[] scores, long[] removedTokens)
        {
            // Update the scores
            foreach (var score in scores)
            {
                long token = score.Ship.PhysicsBody.Token;

                if (_livingFinalistTopScores.ContainsKey(token))
                {
                    if (score.Score > _livingFinalistTopScores[token].Score)
                    {
                        _livingFinalistTopScores[token] = score;		// only keeping the highest score achieved
                    }
                }
                else
                {
                    _livingFinalistTopScores.Add(token, score);		// add for the first time
                }
            }

            // Remove any that are now dead
            List<Tuple<long, WinnerList.WinningBean>> retVal = new List<Tuple<long, WinnerList.WinningBean>>();

            foreach (long dead in removedTokens)
            {
                if (_livingFinalistTopScores.ContainsKey(dead))
                {
                    retVal.Add(Tuple.Create(dead, _livingFinalistTopScores[dead]));
                    _livingFinalistTopScores.Remove(dead);
                }
            }

            // Exit Function
            return retVal.ToArray();
        }

        private void TransitionToCandidates(Tuple<long, WinnerList.WinningBean>[] deadLive, Tuple<long, WinnerList.WinningBean>[] deadFinalists, long[] removedTokens)
        {
            // These are ships that died, and were on the live winner list
            if (deadLive != null && deadLive.Length > 0)
            {
                foreach (var win in deadLive)
                {
                    // Store this as a finalist
                    this.Candidates.Add(win.Item2.Ship.GetNewDNA(), win.Item2.Score);
                }
            }

            // Apply final scores to the dead finalists
            if (deadFinalists != null && deadFinalists.Length > 0)
            {
                foreach (var final in deadFinalists)
                {
                    TrackingCandidate finalist = _finalists.Where(o => o.Contains(final.Item1)).FirstOrDefault();
                    if (finalist == null)
                    {
                        throw new ApplicationException("Didn't find the finalist passed in: " + final.Item1);
                    }

                    finalist.FinishedShip(final.Item1, final.Item2.Score);
                }
            }

            // There is a chance that a ship died before getting added to _livingFinalistTopScores.  If that happens, then it never gets into deadFinalists.
            // So look for this case, and set directly into _finalists
            if (removedTokens != null && removedTokens.Length > 0)
            {
                long[] uniqueDead = removedTokens;
                if (deadFinalists != null && deadFinalists.Length > 0)
                {
                    uniqueDead = removedTokens.Where(o => !deadFinalists.Any(p => o == p.Item1)).ToArray();
                }

                foreach (long dead in uniqueDead)
                {
                    TrackingCandidate finalist = _finalists.Where(o => o.Contains(dead)).FirstOrDefault();
                    if (finalist != null)
                    {
                        finalist.FinishedShip(dead, 0d);
                    }
                }

            }
        }
        private void TransitionToCandidates_OLD(long[] removed, Tuple<long, WinnerList.WinningBean>[] wins)
        {
            #region Wins

            // These are ships that died, and were on the live winner list

            if (wins != null && wins.Length > 0)
            {
                foreach (var win in wins)
                {
                    // See if this was a finalist
                    TrackingCandidate finalist = _finalists.Where(o => o.Contains(win.Item1)).FirstOrDefault();
                    if (finalist != null)
                    {
                        finalist.FinishedShip(win.Item1, win.Item2.Score);
                    }
                    else
                    {
                        // This was a new life, store it as a finalist
                        this.Candidates.Add(win.Item2.Ship.GetNewDNA(), win.Item2.Score);
                    }
                }
            }

            #endregion
            #region Losses

            // These are ships that died, but weren't on the live winner list

            long[] losses = wins == null ? removed : removed.Except(wins.Select(o => o.Item1)).ToArray();

            foreach (long loss in losses)
            {
                TrackingCandidate finalist = _finalists.Where(o => o.Contains(loss)).FirstOrDefault();
                if (finalist != null)
                {
                    // It couldn't compete with the other live winners
                    finalist.FinishedShip(loss, 0d);
                }
            }

            #endregion
        }

        private void TransitionToFinal()
        {
            List<TrackingCandidate> finalists = new List<TrackingCandidate>();
            #region Find finished finalists

            // Get all the finalists that are finished
            int index = 0;
            while (index < _finalists.Count)
            {
                TrackingCandidate finalist = _finalists[index];
                int numStarted = finalist.NumStarted;

                if (numStarted >= this.FinalistCount && numStarted == finalist.NumFinished)
                {
                    if (!Math3D.IsNearZero(finalist.GetAverage()))		// don't let zero scores into the final (the way to get a zero score is to become a candidate, but then each attempt resulted in a zero score)
                    {
                        finalists.Add(finalist);
                    }

                    _finalists.RemoveAt(index);
                }
                else
                {
                    index++;
                }
            }

            #endregion

            if (finalists.Count > 0)
            {
                this.Final.StoreWinners(finalists.Select(o => new WinnerList.WinningBean(o.DNA, o.GetAverage(), 0d)).ToArray());
            }
        }

        #endregion
    }

    #endregion
}
