using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Game.HelperClassesCore
{
    /// <summary>
    /// This is similar in concept to winform's BackgroundWorker, but with tasks
    /// Instead this uses tasks, and 
    /// </summary>
    /// <remarks>
    /// If start is called multiple times, currently running tasks will be canceled, and finish will only be called for the latest start
    /// 
    /// This is useful for tying tasks to textbox change events.  The user can type, and stuff goes on in the background, stuff
    /// cancels, only the latest finish fires
    /// 
    /// Be sure to check for cancelled often in your worker method so that worker threads aren't building up and running
    /// unnecessarily
    /// </remarks>
    public class BackgroundTaskWorker<Treq, Tresp> : ICancel
    {
        #region Class: RunningTask

        private class RunningTask
        {
            public RunningTask(Task<Tresp> task, CancellationTokenSource cancelSource)
            {
                this.Token = TokenGenerator.NextToken();
                this.CancelSource = cancelSource;
                this.Task = task;
            }

            public readonly long Token;
            public readonly CancellationTokenSource CancelSource;
            public readonly Task<Tresp> Task;
        }

        #endregion

        #region Declaration Section

        /// <summary>
        /// This holds a link to the calling thread
        /// </summary>
        /// <remarks>
        /// Read this for details:
        /// https://msdn.microsoft.com/en-us/magazine/gg598924.aspx
        /// 
        /// Winform/WPF:
        ///     main thread has a context tied to that single thread
        /// 
        /// ASP:
        ///     some sort of hybrid
        ///     
        /// Everything else:
        ///     the default context will be returned, so execution will go to a random thread pool thread (or maybe the currently running thread?)
        /// </remarks>
        private readonly TaskScheduler _scheduler;

        private readonly Func<Treq, CancellationToken, Tresp> _doWork;     //TODO: Pass in cancel token as well
        private readonly Action<Treq, Tresp> _finished;
        private readonly Action<Treq, Exception> _finishedException;

        private readonly object _lock = new object();

        private readonly List<RunningTask> _running = new List<RunningTask>();
        private long? _currentToken = null;

        public readonly ICancel[] _cancelOthers;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new background worker, and remembers what thread it's called from
        /// </summary>
        /// <param name="doWork">This will be called on a threadpool thread</param>
        /// <param name="finished">
        /// This will be called on the same thread that this constructor is called from (assuming this constructor is called from the main thread
        /// of a winform or wpf app.  Otherwise it's up to TaskScheduler.FromCurrentSynchronizationContext())
        /// </param>
        /// <param name="finishedException">
        /// This also gets called on the same thread as this contructor (with the same caveat)
        /// This gets called if there was an exception
        /// </param>
        /// <param name="cancelOthers">
        /// If the act of starting or cancelling this worker will cause dependent workers to be cancelled, then pass them in
        /// </param>
        public BackgroundTaskWorker(Func<Treq, CancellationToken, Tresp> doWork, Action<Treq, Tresp> finished, Action<Treq, Exception> finishedException = null, IEnumerable<ICancel> cancelOthers = null)
        {
            _doWork = doWork;
            _finished = finished;
            _finishedException = finishedException;

            _scheduler = TaskScheduler.FromCurrentSynchronizationContext();

            _cancelOthers = cancelOthers == null ? new ICancel[0] : cancelOthers.ToArray();
        }

        #endregion

        #region Public Methods

        public void Start(Treq request)
        {
            // Create a cancel just for this task
            CancellationTokenSource cancelSource = new CancellationTokenSource();

            // Create a worker (but don't start it yet)
            var worker = new Task<Tresp>(() =>
            {
                return _doWork(request, cancelSource.Token);
            }, cancelSource.Token);

            RunningTask latest = new RunningTask(worker, cancelSource);

            // Create a finish
            worker.ContinueWith(result =>
            {
                FinishTask(request, latest);
            }, _scheduler);

            // Store this task
            lock (_lock)
            {
                foreach(ICancel other in _cancelOthers)
                {
                    other.Cancel();
                }

                foreach (RunningTask running in _running)
                {
                    running.CancelSource.Cancel();
                }

                _currentToken = latest.Token;
                _running.Add(latest);
            }

            // Need to wait until it's stored before starting it
            worker.Start();
        }

        public void Cancel()
        {
            lock (_lock)
            {
                foreach (ICancel other in _cancelOthers)
                {
                    other.Cancel();
                }

                _currentToken = null;

                foreach (RunningTask running in _running)
                {
                    running.CancelSource.Cancel();
                }
            }
        }

        #endregion

        #region Private Methods

        //NOTE: This method is called from the syncronized thread
        private void FinishTask(Treq request, RunningTask task)
        {
            lock (_lock)
            {
                // Remove from the list
                int index = 0;
                while (index < _running.Count)
                {
                    if (_running[index].Token == task.Token)
                    {
                        _running.RemoveAt(index);
                    }
                    else
                    {
                        index++;
                    }
                }

                // If this is an old task, then leave
                if (_currentToken == null || task.Token != _currentToken.Value)
                {
                    return;
                }

                // This is the latest, so clear the flag
                _currentToken = null;
            }

            if (task.Task.IsCanceled)
            {
                return;
            }

            // Call the finished delegate
            if (task.Task.Exception != null)
            {
                if (_finishedException != null)
                {
                    _finishedException(request, task.Task.Exception);
                }
            }
            else
            {
                _finished(request, task.Task.Result);
            }
        }

        #endregion
    }
}
