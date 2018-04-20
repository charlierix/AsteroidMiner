using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.HelperClassesCore;

namespace Game.Newt.v2.GameItems.Collections
{
    public class NonLifeEventSnapshots<T> where T : class
    {
        #region Class: Snapshot

        private class Snapshot<T>
        {
            public Snapshot(T item, DateTime time, double? percent)
            {
                this.Item = item;
                this.Time = time;
                this.Percent = percent;
            }

            public readonly T Item;
            public readonly DateTime Time;
            public readonly double? Percent;
        }

        #endregion

        #region Declaration Section

        private readonly object _lock = new object();

        private readonly TimeSpan _interval;

        private readonly TimeSpan _dontStoreTime;
        private readonly TimeSpan _tentativeTime;

        private readonly double _dontStore_Seconds;
        private readonly double _graySpan_Seconds;

        private readonly int _maxSize_Initial;
        private readonly int _maxSize_Tentative;

        /// <summary>
        /// When long term gets larger than this, quit using tentative
        /// </summary>
        private readonly int _abandonTentativeSize;

        /// <summary>
        /// When an overflow of initial occurs, this is how many items to leave in initial (the rest are moved off to
        /// more long term storage)
        /// </summary>
        private readonly int _keepInInitialCountOnOverflow;

        private readonly List<Snapshot<T>> _initialBuffer = new List<Snapshot<T>>();

        private readonly List<Snapshot<T>> _tentativeBuffer = new List<Snapshot<T>>();

        private readonly Snapshot<T>[] _longTermBuffer;
        private int _longTermIndex = -1;

        private DateTime _lastInitialAdd = DateTime.UtcNow;
        private DateTime? _lastLifeEvent = null;

        #endregion

        #region Constructor

        public NonLifeEventSnapshots(double interval_Seconds = 2, double dontStore_Seconds = 5, double tentative_Seconds = 12, int maxSize_Initial = 50, int maxSize_LongTerm = 100, int maxSize_Tentative = 30, int abandonTentativeSize = 30)
        {
            // Store params
            _interval = TimeSpan.FromSeconds(interval_Seconds);

            _dontStoreTime = TimeSpan.FromSeconds(dontStore_Seconds);
            _tentativeTime = TimeSpan.FromSeconds(tentative_Seconds);

            _maxSize_Initial = maxSize_Initial;
            _maxSize_Tentative = maxSize_Tentative;
            _longTermBuffer = new Snapshot<T>[maxSize_LongTerm];

            _abandonTentativeSize = abandonTentativeSize;

            // Keep count
            int keepOnOverflow = (tentative_Seconds / interval_Seconds).ToInt_Ceiling() + 1;
            if (keepOnOverflow > maxSize_Initial)
            {
                throw new ArgumentException("maxSize_Initial is too small based on the tentative and interval values");
            }
            _keepInInitialCountOnOverflow = keepOnOverflow;

            // Tentative - DontStore
            if (tentative_Seconds <= dontStore_Seconds * 1.01)
            {
                throw new ArgumentException("tentative_Seconds must be larger than dontStore_Seconds");
            }
            _dontStore_Seconds = dontStore_Seconds;
            _graySpan_Seconds = tentative_Seconds - dontStore_Seconds;
        }

        #endregion

        #region Public Methods

        public void Add(T item)
        {
            lock (_lock)
            {
                DateTime now = DateTime.UtcNow;

                if (now - _lastInitialAdd < _interval)
                {
                    return;
                }

                double? percent = null;
                if (_lastLifeEvent != null)
                {
                    var percentResult = GetPercent(now, _lastLifeEvent.Value, _dontStoreTime, _tentativeTime, _dontStore_Seconds, _graySpan_Seconds);
                    if (!percentResult.Item1)
                    {
                        // This is too close to the latest life event
                        return;
                    }

                    percent = percentResult.Item2;
                }

                if (_initialBuffer.Count > _maxSize_Initial)
                {
                    MoveFrontOfInitial();
                }

                _initialBuffer.Add(new Snapshot<T>(item, now, percent));
                _lastInitialAdd = now;
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _initialBuffer.Clear();
                _tentativeBuffer.Clear();
                _longTermIndex = -1;
                _lastLifeEvent = null;
            }
        }

        public void EventOcurred()
        {
            lock (_lock)
            {
                DateTime now = DateTime.UtcNow;

                MoveAllInitial(now);

                _lastLifeEvent = now;
            }
        }

        public T[] GetSamples(int count)
        {
            lock (_lock)
            {
                //TODO: May want to consider when count > one of these lists.  But probably not

                // This is used when the front of the list should be favored
                Random rand = StaticRandom.GetRandomForThread();
                var randFunc = new Func<int, int, int>((min, max) => min + UtilityCore.GetIndexIntoList(rand.NextPow(2), max - min));

                if (_longTermIndex < 0 && _tentativeBuffer.Count == 0)
                {
                    // Empty
                    return new T[0];
                }
                else if (_longTermIndex >= _abandonTentativeSize)
                {
                    #region all from longterm

                    return UtilityCore.RandomRange(0, _longTermIndex + 1, count).       // the randrange function takes care of reducing count if greater than range
                        Select(o => _longTermBuffer[o].Item).
                        ToArray();

                    #endregion
                }
                else if (_longTermIndex < 0)
                {
                    #region all from tentative

                    // Don't pull uniform randomly from tentative.  Favor items at the front of the list
                    return UtilityCore.RandomRange(0, _tentativeBuffer.Count, count, randFunc).
                        Select(o => _tentativeBuffer[o].Item).
                        ToArray();

                    #endregion
                }
                else
                {
                    #region mixture

                    // Pull from long term then tentative
                    return UtilityCore.RandomRange(0, _longTermIndex + 1 + _tentativeBuffer.Count, count, randFunc).
                        Select(o =>
                            {
                                if (o <= _longTermIndex)
                                    return _longTermBuffer[o].Item;
                                else
                                    return _tentativeBuffer[o - (_longTermIndex + 1)].Item;
                            }).
                        ToArray();

                    #endregion
                }
            }
        }
        public Tuple<string, T>[] GetSamples()
        {
            lock (_lock)
            {
                var retVal = new List<Tuple<string, T>>();

                if (_tentativeBuffer.Count > 0)
                {
                    retVal.AddRange(_tentativeBuffer.
                        Select(o => Tuple.Create(string.Format("tentative {0}", (o.Percent ?? 1d).ToStringSignificantDigits(2)), o.Item)));
                }

                if (_longTermIndex >= 0)
                {
                    retVal.AddRange(Enumerable.Range(0, _longTermIndex + 1).
                        Select(o => Tuple.Create("longterm", _longTermBuffer[o].Item)));
                }

                return retVal.ToArray();
            }
        }

        #endregion

        #region Private Methods

        private void MoveFrontOfInitial()
        {
            // Figure out how many to move
            int moveCount = _initialBuffer.Count - _keepInInitialCountOnOverflow;

            Random rand = StaticRandom.GetRandomForThread();

            //NOTE: Going backward to the longterm first.  That way there's a chance tentative won't need to be populated
            for (int cntr = moveCount - 1; cntr >= 0; cntr--)
            {
                if (_initialBuffer[cntr].Percent == null)
                {
                    TransferToLongterm(_initialBuffer[cntr], rand);
                }
                else
                {
                    TransferToTentative(_initialBuffer[cntr]);
                }

                _initialBuffer.RemoveAt(cntr);
            }
        }
        private void MoveAllInitial(DateTime eventTime)
        {
            Random rand = StaticRandom.GetRandomForThread();

            foreach (Snapshot<T> item in _initialBuffer)
            {
                var rightPercent = GetPercent(item.Time, eventTime, _dontStoreTime, _tentativeTime, _dontStore_Seconds, _graySpan_Seconds);
                if (!rightPercent.Item1)
                {
                    // This is too close to the event, and shouldn't be stored
                    continue;
                }

                Snapshot<T> finalItem = ExaminePercents(item, rightPercent.Item2);

                if (finalItem.Percent == null)
                {
                    TransferToLongterm(finalItem, rand);
                }
                else
                {
                    TransferToTentative(finalItem);
                }
            }

            _initialBuffer.Clear();
        }

        private void TransferToLongterm(Snapshot<T> item, Random rand)
        {
            if (_longTermIndex < _longTermBuffer.Length - 1)
            {
                // Still some empty slots available
                _longTermIndex++;
                _longTermBuffer[_longTermIndex] = item;
            }
            else
            {
                // Time has no priority (keep oldest or keep newest).  Since they are all equally valuable, just use random
                //
                // The flying beans archive had a method to keep an even spread of times, but I don't see much value in
                // taking that expense
                _longTermBuffer[rand.Next(_longTermBuffer.Length)] = item;
            }
        }
        private void TransferToTentative(Snapshot<T> item)
        {
            if (_longTermIndex >= _abandonTentativeSize)
            {
                return;
            }

            // Keep them sorted descending by percent
            bool addedIt = false;
            for (int cntr = 0; cntr < _tentativeBuffer.Count; cntr++)
            {
                if (item.Percent.Value > _tentativeBuffer[cntr].Percent.Value)
                {
                    _tentativeBuffer.Insert(cntr, item);
                    addedIt = true;
                    break;
                }
            }

            if (!addedIt)
            {
                _tentativeBuffer.Add(item);
            }

            // Make sure it doens't get too large
            while (_tentativeBuffer.Count > _maxSize_Tentative)
            {
                _tentativeBuffer.RemoveAt(_tentativeBuffer.Count - 1);
            }
        }

        private static Tuple<bool, double?> GetPercent(DateTime snapshotTime, DateTime lifeEventTime, TimeSpan dontStoreTime, TimeSpan tentativeTime, double dontStore_Seconds, double graySpan_Seconds)
        {
            TimeSpan fromLast = (snapshotTime - lifeEventTime).Duration();        // use duration to get the abs of the interval

            if (fromLast < dontStoreTime)
            {
                // This is too close to the life event
                return Tuple.Create(false, (double?)null);
            }

            double? percent = null;
            if (fromLast < tentativeTime)
            {
                percent = (fromLast.TotalSeconds - dontStore_Seconds) / graySpan_Seconds;
            }

            return Tuple.Create(true, percent);
        }

        /// <summary>
        /// This uses the more restrictive percent
        /// </summary>
        private Snapshot<T> ExaminePercents(Snapshot<T> item, double? rightPercent)
        {
            if (item.Percent == null && rightPercent == null)
            {
                return item;
            }
            else if (item.Percent != null && rightPercent == null)
            {
                return item;
            }
            else if (item.Percent == null && rightPercent != null)
            {
                return new Snapshot<T>(item.Item, item.Time, rightPercent);
            }

            // Both percents are populated, keep the smaller percent (because that's the one that is closer to a life event)
            if (item.Percent.Value < rightPercent.Value)
            {
                return item;
            }
            else
            {
                return new Snapshot<T>(item.Item, item.Time, rightPercent);
            }
        }

        #endregion
    }
}
