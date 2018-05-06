using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.HelperClassesCore
{
    /// <summary>
    /// This is a fixed size buffer that will overwrite the oldest entry once the size is exceeded.
    /// NOTE: Externally, the oldest item will be index 0
    /// NOTE: This class IS threadsafe
    /// </summary>
    /// <remarks>
    /// When searching for an implementation of a rolling buffer | circular buffer, there were a lot of ideas about what that is.
    /// Lots of people seem to think of it as a queue.  Not sure why there is so much confusion
    /// </remarks>
    public class CircularBuffer<T> : IEnumerable<T>, IList<T>
    {
        #region Declaration Section

        private readonly object _lock = new object();

        private T[] _buffer;
        private int _index = -1;

        #endregion

        #region Constructor

        public CircularBuffer(int maxCount)
        {
            _maxCount = maxCount;
            _buffer = new T[maxCount];
        }

        #endregion

        #region IEnumerable<T>

        public IEnumerator<T> GetEnumerator()
        {
            lock (_lock)
            {
                return GetEnumerator_private();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            lock (_lock)
            {
                return GetEnumerator_private();
            }
        }

        #endregion
        #region IList<T>

        public int IndexOf(T item)
        {
            lock (_lock)
            {
                int index = 0;

                foreach (T candidate in Enumerate_private())
                {
                    if (item == null && candidate == null)
                    {
                        return index;
                    }
                    else if (item != null && candidate != null && item.Equals(candidate))
                    {
                        return index;
                    }

                    index++;
                }

                return -1;
            }
        }

        public T this[int index]
        {
            get
            {
                lock (_lock)
                {
                    int internalIndex = GetInternalIndex(index);

                    if (internalIndex < 0)
                    {
                        throw new IndexOutOfRangeException(string.Format("Couldn't map external index to internal index.  ExternalIndex={0} BufferSize={1} HasWrapped={2} _index={3}", index, _buffer.Length, _hasWrapped, _index));
                    }

                    return _buffer[internalIndex];
                }
            }
            set => throw new NotImplementedException();
        }

        public void Add(T item)
        {
            lock (_lock)
            {
                _index++;
                if (_index >= _buffer.Length)
                {
                    _index = 0;
                    _hasWrapped = true;
                }

                _buffer[_index] = item;
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _hasWrapped = false;
                _index = -1;
            }
        }

        public bool Contains(T item)
        {
            lock (_lock)
            {
                return Enumerate_private().
                    Contains(item);
            }
        }

        public int Count
        {
            get
            {
                lock (_lock)
                {
                    if (_hasWrapped)
                    {
                        return _buffer.Length;
                    }
                    else
                    {
                        return _index + 1;
                    }
                }
            }
        }

        public bool IsReadOnly => false;

        //TODO: May want to support these
        public void Insert(int index, T item)
        {
            throw new NotImplementedException();
        }
        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }
        public bool Remove(T item)
        {
            throw new NotImplementedException();
        }
        public void CopyTo(T[] array, int arrayIndex)
        {
            lock (_lock)
            {
                if (array == null)
                {
                    throw new ArgumentNullException("array", "array can't be null");
                }
                else if (arrayIndex < 0)
                {
                    throw new ArgumentOutOfRangeException("arrayIndex", "arrayIndex must be 0 or greater: " + arrayIndex.ToString());
                }
                else if (arrayIndex >= array.Length)
                {
                    throw new ArgumentOutOfRangeException("arrayIndex", string.Format("arrayIndex must be less than the length of the array.  ArrayLen={0} arrayIndex={1} ", array.Length, arrayIndex));
                }
                else if (array.Length < arrayIndex + Count)
                {
                    throw new ArgumentException(string.Format("The array isn't large enough to hold the items.  ArrayLen={0} arrayIndex={1} this.Count={2}", array.Length, arrayIndex, Count));
                }

                int index = 0;
                foreach (T item in Enumerate_private())
                {
                    array[arrayIndex + index] = item;
                    index++;
                }
            }
        }

        #endregion

        #region Public Properties

        private int _maxCount;
        public int MaxCount => _maxCount;

        /// <summary>
        /// If this is false, then Count is _index + 1, and everything to the right of _index is undefined.  But once this
        /// is true, the buffer is full and Count is _buffer.Length (external index of zero then starts one position to the
        /// right of _index)
        /// </summary>
        private bool _hasWrapped = false;
        public bool HasWrapped
        {
            get
            {
                lock (_lock)
                {
                    return _hasWrapped;
                }
            }
        }

        #endregion

        #region Public Methods

        public IEnumerable<T> EnumerateReverse()
        {
            lock (_lock)
            {
                return EnumerateReverse_private();
            }
        }

        public int IndexOfReverse(T item)
        {
            lock (_lock)
            {
                int index = Count - 1;

                foreach (T candidate in EnumerateReverse_private())
                {
                    if (item == null && candidate == null)
                    {
                        return index;
                    }
                    else if (item != null && candidate != null && item.Equals(candidate))
                    {
                        return index;
                    }

                    index--;
                }

                return -1;
            }
        }

        public void ChangeSize(int maxCount)
        {
            lock (_lock)
            {
                if (maxCount == MaxCount)
                {
                    return;
                }
                else if (maxCount < MaxCount)
                {
                    // Shrink.  Get rid of the oldest items
                    T[] smaller = new T[maxCount];

                    int index = maxCount;

                    foreach (T item in EnumerateReverse_private().Take(maxCount))
                    {
                        index--;
                        smaller[index] = item;
                    }

                    _index = maxCount - 1;
                    _hasWrapped = false;
                    _buffer = smaller;
                }
                else
                {
                    // Grow
                    T[] larger = new T[maxCount];

                    int index = -1;
                    foreach (T item in Enumerate_private())
                    {
                        index++;
                        larger[index] = item;
                    }

                    _index = index;
                    _hasWrapped = false;
                    _buffer = larger;
                }
            }
        }

        #endregion

        #region Private Methods

        private IEnumerator<T> GetEnumerator_private()
        {
            if (_hasWrapped)
            {
                for (int cntr = _index + 1; cntr < _buffer.Length; cntr++)
                {
                    yield return _buffer[cntr];
                }
            }

            for (int cntr = 0; cntr <= _index; cntr++)
            {
                yield return _buffer[cntr];
            }
        }
        private IEnumerable<T> Enumerate_private()
        {
            IEnumerator<T> enumerator = GetEnumerator_private();

            while (enumerator.MoveNext())
            {
                yield return enumerator.Current;
            }
        }

        private IEnumerable<T> EnumerateReverse_private()
        {
            for (int cntr = _index; cntr >= 0; cntr--)
            {
                yield return _buffer[cntr];
            }

            if (_hasWrapped)
            {
                for (int cntr = _buffer.Length - 1; cntr > _index; cntr--)
                {
                    yield return _buffer[cntr];
                }
            }
        }

        /// <summary>
        /// External index doesn't know about _hasWrapped and _index.  It just wants the Nth item
        /// </summary>
        private int GetInternalIndex(int index)
        {
            if (index < 0)
            {
                return -1;
            }

            if (_hasWrapped)
            {
                int offset = _buffer.Length - 1 - _index;

                if (index < offset)
                {
                    return _index + 1 + index;
                }
                else if (index >= _buffer.Length)
                {
                    return -1;
                }
                else
                {
                    return index - offset;
                }
            }
            else
            {
                if (index > _index)
                {
                    return -1;
                }
                else
                {
                    return index;
                }
            }
        }

        #endregion
    }
}
