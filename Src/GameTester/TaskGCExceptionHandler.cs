using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Game.GameTester
{
    public class TaskGCExceptionHandler
    {
        #region Declaration Section

        private static readonly object _lockStatic = new object();
        private readonly object _lockInstance;

        //	The static constructor makes sure that this instance is created only once.  The outside users of this class
        //	call the static property Instance to get this one instance copy of me.  (then they can use the rest of the
        //	instance methods)
        private static TaskGCExceptionHandler _instance;

        /// <summary>
        /// This tells whether the event listener has been added (should only be done once)
        /// </summary>
        private bool _isListening;

        #endregion

        #region Constructor / Instance Property

        /// <summary>
        /// Static constructor.  Called only once before the first time you use the static properties/methods.
        /// </summary>
        static TaskGCExceptionHandler()
        {
            lock (_lockStatic)
            {
                //	If the instance version of this class hasn't been instantiated yet, then do so
                if (_instance == null)
                {
                    _instance = new TaskGCExceptionHandler();
                }
            }
        }
        /// <summary>
        /// Instance constructor.  This is called only once by one of the calls from the static constructor.
        /// </summary>
        private TaskGCExceptionHandler()
        {
            _lockInstance = new object();
            _isListening = false;
        }

        /// <summary>
        /// This is how you get at the instance.  The act of calling this property guarantees that the static constructor gets called
        /// exactly once (per process?)
        /// </summary>
        public static TaskGCExceptionHandler Instance
        {
            get
            {
                //	There is no need to check the static lock, because _instance is only set one time, and that is guaranteed to be
                //	finished before this function gets called
                return _instance;
            }
        }

        #endregion

        #region Public Methods

        public void EnsureSetup()
        {
            //	This could have been done in the instance constructor, but it's a bit odd to have a singleton with no method
            //	to call

            lock (_lockInstance)
            {
                if (!_isListening)
                {
                    //	Only have one listener.  Otherwise the event will be raised multiple times for each exception
                    TaskScheduler.UnobservedTaskException += new EventHandler<UnobservedTaskExceptionEventArgs>(TaskScheduler_UnobservedTaskException);
                    _isListening = true;
                }
            }
        }

        #endregion

        #region Event Listeners

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            e.SetObserved();

            // Calling flatten to get rid of any inner aggregate exceptions.  Handle will iterate through all inner exceptions
            //	(each iteration must return true, or those falses will rethrow, and bring down the process)
            e.Exception.Flatten().Handle(ex =>
            {
                try
                {
                    //TODO: Log ex


                    //	Tell the exception not to worry
                    return true;
                }
                catch (Exception)
                {
                    //	Logging failed, but it doesn't matter.  Don't let this bomb the process
                    return true;
                }
            });
        }

        #endregion
    }
}