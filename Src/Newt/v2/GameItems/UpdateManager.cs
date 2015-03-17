using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Game.HelperClassesCore;

namespace Game.Newt.v2.GameItems
{
    //TODO: Instead of querying map each timer tick, have another timer that queries it, and spreads out the work.  If the any timer tick is taking too
    //long, make another timer and divide the work

    /// <summary>
    /// This is a helper class that takes care of firing IPartUpdatable updates
    /// </summary>
    public class UpdateManager : IDisposable
    {
        #region Declaration Section

        private readonly Map _map;

        private volatile bool _isDisposed = false;
        private readonly ManualResetEvent _disposeWait = new ManualResetEvent(false);

        // --------------------- variables for main thread

        private Tuple<Type, int>[] _typesMain = null;
        private Type[] _typesMainUnchecked = null;

        /// <summary>
        /// This is only used by the zero param overload of Update_MainThread
        /// </summary>
        private DateTime _lastMainUpdate = DateTime.Now;

        private long _updateCount_Main = -1;      // starting at -1 so that the first increment will bring it to 0 (skips use mod, so zero mod anything is zero, so everything will fire on the first tick)

        // --------------------- variables for any thread

        private System.Timers.Timer _timerAnyThread = null;

        private readonly object _lockTypesAny = new object();

        private volatile Tuple<Type, int>[] _typesAny = null;
        private volatile Type[] _typesAnyUnchecked = null;

        private volatile object _lastAnyUpdate = DateTime.Now;

        private long _updateCount_Any = -1;       // this is incremented through Interlocked, so doesn't need to be volatile

        #endregion

        #region Constructor

        //TODO: keep track of what types were added to map (a type added event) and store the types that implement IPartUpdatable
        //When that happens, ask map to include derived, and only store the base most type that implements IPartUpdatable
        /// <param name="types_main">Types to call update on the main thread (can add the same type to both main and any)</param>
        /// <param name="types_any">Types to call update on any thread (can add the same type to both main and any)</param>
        public UpdateManager(Type[] types_main, Type[] types_any, Map map, int interval = 50)
        {
            #region Validate

            Type updateableType = typeof(IPartUpdatable);
            Type nonImplement = UtilityCore.Iterate(types_main, types_any).
                Where(o => !o.GetInterfaces().Any(p => p.Equals(updateableType))).
                FirstOrDefault();       // one example is enough

            if (nonImplement != null)
            {
                throw new ArgumentException("Type passed in doesn't implement IPartUpdatable: " + nonImplement.ToString());
            }

            #endregion

            _typesMainUnchecked = types_main;
            _typesAnyUnchecked = types_any;

            _map = map;

            _timerAnyThread = new System.Timers.Timer();
            _timerAnyThread.Interval = 50;
            _timerAnyThread.AutoReset = false;       // makes sure only one tick is firing at a time
            _timerAnyThread.Elapsed += TimerAnyThread_Elapsed;
            _timerAnyThread.Start();
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timerAnyThread.Stop();
                _isDisposed = true;

                // Don't want to exit right away.  Block this main thread, and give the any thread timer a chance to finish
                _disposeWait.Reset();
                _disposeWait.WaitOne(Convert.ToInt32(_timerAnyThread.Interval * 2));
            }
        }

        #endregion

        public void Update_MainThread()
        {
            DateTime time = DateTime.Now;
            double elapsedTime = (time - _lastMainUpdate).TotalSeconds;
            _lastMainUpdate = time;

            Update_MainThread(elapsedTime);
        }
        public void Update_MainThread(double elapsedTime)
        {
            if (_isDisposed)
            {
                return;
            }

            if (_typesMainUnchecked != null)
            {
                #region Get intervals for types

                Tuple<Type, int>[] checkedTypes = GetTypeIntervals(_typesMainUnchecked, _map, true);
                if (checkedTypes != null)
                {
                    var transfer = TransferTypes(_typesMainUnchecked, _typesMain, checkedTypes);
                    _typesMainUnchecked = transfer.Item1;
                    _typesMain = transfer.Item2;
                }

                #endregion
            }

            if (_typesMain == null)
            {
                return;
            }

            _updateCount_Main++;

            foreach (var type in _typesMain)
            {
                if (type.Item2 > 0 && _updateCount_Main % (type.Item2 + 1) != 0)
                {
                    continue;
                }

                // Update all the live instances
                foreach (IPartUpdatable item in _map.GetItems(type.Item1, false))      // Not bothering to call these in random order, because main thread should just be graphics
                {
                    item.Update_MainThread(elapsedTime + (elapsedTime * type.Item2));        // if skips is greater than zero, then approximate how much time elapsed based on this tick's elapsed time
                }
            }
        }

        private void TimerAnyThread_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (_isDisposed)
            {
                return;
            }

            double elapsedTime = (e.SignalTime - (DateTime)_lastAnyUpdate).TotalSeconds;
            _lastAnyUpdate = e.SignalTime;

            Type[] typesUnchecked = _typesAnyUnchecked;
            if (typesUnchecked != null)
            {
                #region Get intervals for types

                Tuple<Type, int>[] checkedTypes = GetTypeIntervals(typesUnchecked, _map, false);
                if (checkedTypes != null)
                {
                    lock (_lockTypesAny)
                    {
                        // Do it again with the volatile to be safe
                        checkedTypes = GetTypeIntervals(_typesAnyUnchecked, _map, false);
                        if (checkedTypes != null)
                        {
                            var transfer = TransferTypes(_typesAnyUnchecked, _typesAny, checkedTypes);
                            _typesAnyUnchecked = transfer.Item1;
                            _typesAny = transfer.Item2;
                        }
                    }
                }

                #endregion
            }

            Tuple<Type, int>[] typesAny = _typesAny;
            if (typesAny == null)
            {
                _timerAnyThread.Start();
                return;
            }

            long count = Interlocked.Increment(ref _updateCount_Any);

            foreach (var type in typesAny)
            {
                if (type.Item2 > 0 && count % (type.Item2 + 1) != 0)
                {
                    continue;
                }

                // Update all the live instances in a random order
                IMapObject[] items = _map.GetItems(type.Item1, false).ToArray();
                foreach (int index in UtilityCore.RandomRange(0, items.Length))
                {
                    ((IPartUpdatable)items[index]).Update_AnyThread(elapsedTime + (elapsedTime * type.Item2));        // if skips is greater than zero, then approximate how much time elapsed based on this tick's elapsed time
                }
            }

            _disposeWait.Set();     // Dispose will hang the main thread to make sure this tick has finished

            _timerAnyThread.Start();        // it doesn't matter if this class is disposed, the bool is checked at the beginning of this method (no need to take the expense of checking again)
        }

        #region Private Methods

        /// <summary>
        /// This will see if the map has any instances of the types passed in.  If so, it will ask for the interval
        /// </summary>
        private static Tuple<Type, int>[] GetTypeIntervals(Type[] types, Map map, bool isMain)
        {
            List<Tuple<Type, int>> retVal = new List<Tuple<Type, int>>();

            foreach (Type type in types)
            {
                foreach (IPartUpdatable item in map.GetItems(type, false))     // any types handed to this class should implent IPartUpdatable
                {
                    int? interval = isMain ? item.IntervalSkips_MainThread : item.IntervalSkips_AnyThread;

                    if (interval == null)
                    {
                        throw new ApplicationException("This type doesn't support main update, and shouldn't have been given to this class: " + type.ToString());
                    }

                    retVal.Add(Tuple.Create(type, interval.Value));
                    break;
                }
            }

            if (retVal.Count == 0)
            {
                return null;
            }
            else
            {
                return retVal.ToArray();
            }
        }

        /// <summary>
        /// This will move the types in transfer from the from array to the to array.  The arrays passed in won't be affected,
        /// but the return will be the new from and to
        /// </summary>
        private static Tuple<Type[], Tuple<Type, int>[]> TransferTypes(Type[] existingFrom, Tuple<Type, int>[] existingTo, Tuple<Type, int>[] transfer)
        {
            // Only keep the types that aren't in transfer
            Type[] newFrom = existingFrom.Where(o => !transfer.Any(p => p.Item1.Equals(o))).ToArray();
            if (newFrom.Length == 0)
            {
                newFrom = null;
            }

            // Add transfer to existing
            Tuple<Type, int>[] newTo = UtilityCore.ArrayAdd(existingTo, transfer);

            return Tuple.Create(newFrom, newTo);
        }

        #endregion
    }
}
