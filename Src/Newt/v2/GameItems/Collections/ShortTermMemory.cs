using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Newt.v2.GameItems.Collections
{
    /// <summary>
    /// This holds a rolling buffer of data
    /// </summary>
    /// <remarks>
    /// There may be other uses for this, but the use it was written for is this:
    /// 
    /// This will be handed neural dumps on a regular basis (a copy of input to a recognizer, which would probably be raw sensor data).
    /// When some life event happens (eat something, take damage, etc), a request for the neural input data preceeding that event will
    /// be requested.  That combination of input data with a life event classification can be used to train recognizers
    /// 
    /// The flow chain would be:
    ///     Sensor > ShortTermMemory |tee| Recognizer > Higher level brain
    ///     
    /// NOTE: Currently using datetime, mainly for convenience.  Some kind of double that represents elapsed time would be better, but
    /// that would require a central clock
    /// </remarks>
    public class ShortTermMemory<T> where T : class
    {
        #region Declaration Section

        private readonly object _lock = new object();

        private readonly Tuple<DateTime, T>[] _buffer;       // the contents of the buffer aren't readonly, just the array

        private readonly TimeSpan _interval;

        private DateTime _prevTime = DateTime.UtcNow;
        private int _nextIndex = 0;

        #endregion

        #region Constructor

        public ShortTermMemory(double millisecondsBetween = 150, int bufferSize = 100)
        {
            _interval = TimeSpan.FromMilliseconds(millisecondsBetween);
            _buffer = new Tuple<DateTime, T>[bufferSize];
        }

        #endregion

        /// <summary>
        /// Stores the neural snapshot as well as the current time
        /// </summary>
        public void StoreSnapshot(T snapshot)
        {
            DateTime time = DateTime.UtcNow;

            lock (_lock)
            {
                if (time - _prevTime < _interval)
                {
                    return;
                }

                _buffer[_nextIndex] = Tuple.Create(time, snapshot);

                _prevTime = time;

                _nextIndex++;
                if (_nextIndex >= _buffer.Length)
                {
                    _nextIndex = 0;
                }
            }
        }

        /// <summary>
        /// This finds the closest snapshot that occurred before the request time
        /// NOTE: Be sure to use DateTime.UtcNow
        /// </summary>
        public T GetSnapshot(DateTime time)
        {
            lock (_lock)
            {
                for (int cntr = _nextIndex - 1; cntr >= 0; cntr--)
                {
                    if (_buffer[cntr].Item1 <= time)
                    {
                        return _buffer[cntr].Item2;
                    }
                }

                for (int cntr = _buffer.Length - 1; cntr >= _nextIndex; cntr--)
                {
                    if (_buffer[cntr] != null && _buffer[cntr].Item1 <= time)
                    {
                        return _buffer[cntr].Item2;
                    }
                }

                return null;
            }
        }
        /// <summary>
        /// This finds the closest X snapshots that occurred before the request time
        /// NOTE: Be sure to use DateTime.UtcNow
        /// </summary>
        public T[] GetSnapshots(DateTime time, int count)
        {
            lock (_lock)
            {
                #region get best match

                Tuple<DateTime, T> bestMatch = null;
                int bestIndex = -1;

                for (int cntr = _nextIndex - 1; cntr >= 0; cntr--)
                {
                    if (_buffer[cntr].Item1 <= time)
                    {
                        bestMatch = _buffer[cntr];
                        bestIndex = cntr;
                        break;
                    }
                }

                if (bestMatch == null)
                {
                    for (int cntr = _buffer.Length - 1; cntr >= _nextIndex; cntr--)
                    {
                        if (_buffer[cntr] != null && _buffer[cntr].Item1 <= time)
                        {
                            bestMatch = _buffer[cntr];
                            bestIndex = cntr;
                            break;
                        }
                    }
                }

                #endregion

                if (bestMatch == null)
                {
                    return new T[0];
                }

                #region walk back

                // Since the array is a rolling buffer, the next closest will be immediately before the found item

                List<T> retVal = new List<T>();
                retVal.Add(bestMatch.Item2);

                if (retVal.Count < count)
                {
                    for (int cntr = bestIndex - 1; cntr >= 0; cntr--)
                    {
                        if (_buffer[cntr] == null || _buffer[cntr].Item1 > time)
                        {
                            // There should never be nulls or dates greater before the matched item, but doing the check just in case
                            break;
                        }

                        retVal.Add(_buffer[cntr].Item2);

                        if (retVal.Count >= count)
                        {
                            break;
                        }
                    }
                }

                if (retVal.Count < count)
                {
                    for (int cntr = _buffer.Length - 1; cntr > bestIndex; cntr--)
                    {
                        if (_buffer[cntr] == null || _buffer[cntr].Item1 > time)
                        {
                            // Nulls could happen (buffer just hasn't been written to yet.  But there should never be dates greater
                            break;
                        }

                        retVal.Add(_buffer[cntr].Item2);

                        if (retVal.Count >= count)
                        {
                            break;
                        }
                    }
                }


                #endregion

                return retVal.ToArray();
            }
        }
        /// <summary>
        /// This returns all snapshots, oldest first
        /// </summary>
        public T[] GetSnapshots()
        {
            lock (_lock)
            {
                return Enumerable.Range(_nextIndex, _buffer.Length - _nextIndex).       // _nextIndex is where the next will get written, so is the oldest item
                    Concat(Enumerable.Range(0, _nextIndex)).        // walk up to, but not including _nextIndex
                    Where(o => _buffer[o] != null).
                    Select(o => _buffer[o].Item2).
                    ToArray();
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                for (int cntr = 0; cntr < _buffer.Length; cntr++)
                {
                    _buffer[cntr] = null;
                }
            }
        }
    }

    //TODO: Put this in its own file
    #region Class: MemoryFragment

    /// <summary>
    /// This allows bots to share training data with each other
    /// </summary>
    /// <remarks>
    /// Major life events could be passed to offspring or other friendlies, or maybe even eaten.  This will allow experience
    /// to be shared instead of waiting for it to be relived
    /// 
    /// The consumer would need to pass this training data to similar parts, and stretch it to fit that new part
    /// </remarks>
    public class MemoryFragment
    {
        /// <summary>
        /// This is what part this came from
        /// </summary>
        public string PartType { get; set; }

        /// <summary>
        /// This is the raw input to the neural network
        /// </summary>
        public double[] Input { get; set; }

        //TODO: Store an equivalent that is serializable
        public LifeEventVectorArgs Classification { get; set; }


        // These are information about the part that created the input data.  They will be used to resize the data for different
        // sized future parts
        public double Scale { get; set; }

        //TODO: Add more as needed
        public int? Width { get; set; }
        public int? Height { get; set; }
    }

    #endregion
}
