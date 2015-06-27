using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Game.HelperClassesCore
{
    /// <summary>
    /// This timer will create a new thread and fire all ticks on that thread.  It also exposes a startup and teardown delegate so that
    /// objects can be created that will be accessed by that thread
    /// </summary>
    /// <remarks>
    /// My goal for making this is to have an instance of world on its own thread, and to receive ticks on a regular interval.  I didn't want
    /// a tight while loop - I think the only reason for this timer in world is to give the neural nets a chance to fire multiple times between
    /// ticks.  Otherwise, if there are no neural, and no graphics, just run the world in a tight loop as fast as possible.
    /// </remarks>
    public class TimerCreateThread<T> : IDisposable
    {
        #region Declaration Section

        public delegate T PrepDelegate(params object[] args);
        public delegate void TickDelegate(T arg, double elapsedTime);
        public delegate void TearDownDelegate(T arg);

        private readonly ManualResetEvent _disposeEvent = new ManualResetEvent(false);
        private readonly ManualResetEvent _timerSettingsChanged = new ManualResetEvent(false);

        private readonly PrepDelegate _prep;
        private readonly object[] _prepArgs;
        private readonly TickDelegate _tick;
        private readonly TearDownDelegate _tearDown;

        private volatile bool _isTimerRunning = false;

        #endregion

        #region Constructor

        /// <summary>
        /// This creates a new timer that runs on a new thread (not threadpool thread).  All three of the delegates passed in will be executed
        /// from that newly created threads
        /// </summary>
        /// <param name="prep">
        /// This gives the caller a chance to set up objects for the tick to use (those objects won't need to be threadsafe, because they will be created and
        /// used only on the background thread for this instance of timer
        /// </param>
        /// <param name="tick">
        /// This gets fired every internal (arg is the object returned from prep
        /// NOTE: If the code in this tick takes longer than interval, subsequent ticks will just get eaten, so no danger
        /// </param>
        /// <param name="tearDown">
        /// This gets called when this timer is disposed.  Gives the user a chance to dispose anything they created in prep
        /// </param>
        public TimerCreateThread(PrepDelegate prep = null, TickDelegate tick = null, TearDownDelegate tearDown = null, params object[] prepArgs)
        {
            //TODO: figure out how to use ParameterizedThreadStart instead of using member variables
            _prep = prep;
            _prepArgs = prepArgs;
            _tick = tick;
            _tearDown = tearDown;

            Thread workerThread = new Thread(WorkerMethod);
            workerThread.IsBackground = true;
            workerThread.Start();
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _disposeEvent.Set();

                //TODO: I don't think it's necessary to call workerThread.Join() in dispose (only when getting a result back from the thread?).  But if it is necessary, then do it
            }
        }

        #endregion

        #region Public Properties

        // Milliseconds
        private volatile object _interval = 100d;
        /// <summary>
        /// Milliseconds (default is 100)
        /// </summary>
        public double Interval
        {
            get
            {
                return (double)_interval;
            }
            set
            {
                _interval = value;
                _timerSettingsChanged.Set();
            }
        }

        #endregion

        #region Public Methods

        // These could also be thought of as pause/resume
        public void Start()
        {
            _isTimerRunning = true;
            _timerSettingsChanged.Set();
        }
        public void Stop()
        {
            _isTimerRunning = false;
            _timerSettingsChanged.Set();
        }

        #endregion

        #region Private Methods

        private void WorkerMethod()
        {
            // Doing this so TaskScheduler.FromCurrentSynchronizationContext() will work
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());

            // Since _tearDown gets called after dispose, there's a slight chance that the underlying object could get garbage collected, so store local copies of these
            // two delegates up front
            TickDelegate tick = _tick;
            TearDownDelegate tearDown = _tearDown;

            // Let the client create an object from this thread
            T prepResults = default(T);
            if (_prep != null)
            {
                prepResults = _prep(_prepArgs);
            }

            #region Set up the timer

            DateTime lastTick = DateTime.UtcNow;

            ManualResetEvent tickEvent = new ManualResetEvent(false);

            System.Timers.Timer timer = new System.Timers.Timer();
            //timer.SynchronizingObject     //NOTE: I tried to implement ISynchonizeInvoke, but that approach was a mess.  I think this approach of creating a thead for the user is way cleaner
            timer.Interval = this.Interval;
            timer.Elapsed += delegate { tickEvent.Set(); };
            timer.AutoReset = false;
            if (_isTimerRunning)
            {
                timer.Start();
            }

            #endregion

            WaitHandle[] handles = new WaitHandle[] { _disposeEvent, _timerSettingsChanged, tickEvent };

            while (true)
            {
                // Hang this thread until something happens
                int handleIndex = WaitHandle.WaitAny(handles);

                if (handleIndex == 0)       // I would prefer a switch statement, but there is no Exit While statement
                {
                    // Dispose was called, this thread needs to finish
                    //_disposeEvent.Reset();        // just leave this one tripped so that 
                    break;
                }
                else if (handleIndex == 1)
                {
                    #region Timer settings changed

                    _timerSettingsChanged.Reset();

                    // Settings for the timer have changed
                    timer.Interval = this.Interval;

                    if (_isTimerRunning)
                    {
                        timer.Start();      // it appears to be safe to call this even when started
                    }
                    else
                    {
                        timer.Stop();       // and to call this again when stopped
                    }

                    #endregion
                }
                else if (handleIndex == 2)
                {
                    #region Timer ticked

                    DateTime currentTick = DateTime.UtcNow;
                    double elapsed = (currentTick - lastTick).TotalMilliseconds;
                    lastTick = currentTick;

                    tickEvent.Reset();

                    if (_isTimerRunning)
                    {
                        timer.Start();

                        if (tick != null)       // putting this inside the _isTimerRunning, because they may have wanted the timer stopped between ticks, so in that case, just ignore this tick event
                        {
                            tick(prepResults, elapsed);
                        }
                    }

                    #endregion
                }
                else
                {
                    throw new ApplicationException("Unknown wait handle: " + handleIndex.ToString());
                }
            }

            timer.Stop();
            timer.Dispose();

            if (tearDown != null)
            {
                tearDown(prepResults);
            }
        }

        #endregion
    }
}
