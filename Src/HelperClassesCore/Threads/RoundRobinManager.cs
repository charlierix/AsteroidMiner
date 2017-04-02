using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Schedulers;

namespace Game.HelperClassesCore.Threads
{


    //TODO: Add some kind of Priority property to IRoundRobinWorker.  If the worker is used to train lots of independent
    //things, then as each individual task's score improves, it could lower its priority.  This will give the round robin scheduler
    //a hint to give more steps to the high priority tasks (the ones that aren't as far along in their training)




    /// <summary>
    /// This is meant for workers that need to do many steps before they are finished.  The workers share a single thread, and
    /// are called round robin
    /// </summary>
    /// <remarks>
    /// The case for the creation of this class is a genetic algorithm solver.  The solver needs many iterations to complete.  The
    /// user needed many solvers to run at once.  When each solver had its own thread, they crushed the computer.  So this class
    /// will run all the solvers on a single thread
    /// </remarks>
    public class RoundRobinManager : IDisposable
    {
        #region Declaration Section

        /// <summary>
        /// This is used in dispose to stop all the workers
        /// </summary>
        private readonly CancellationTokenSource _cancel = new CancellationTokenSource();

        /// <summary>
        /// When the worker thread has no more workers, it waits for a signal from this, so it can wake up and grab
        /// workers out of the queue
        /// </summary>
        /// <remarks>
        /// http://stackoverflow.com/questions/382173/what-are-alternative-ways-to-suspend-and-resume-a-thread
        /// </remarks>
        private readonly EventWaitHandle _waitHandle = new AutoResetEvent(false);

        /// <summary>
        /// This is the thread that the workers run on
        /// </summary>
        private readonly Task _task;

        /// <summary>
        /// When the caller adds a worker, they get put in this threadsafe queue.  The worker thread then periodically pops the
        /// workers out of this queue, and stores them in a local list
        /// </summary>
        /// <remarks>
        /// This is done so that the worker thread isn't needing a special threadsafe list, or locks everywhere.  It's just a standard
        /// local list that gets manipulated on that worker thread's terms
        /// </remarks>
        private readonly ConcurrentQueue<Tuple<bool, IRoundRobinWorker>> _addRemoveQueue = new ConcurrentQueue<Tuple<bool, IRoundRobinWorker>>();

        #endregion

        #region Constructor

        public RoundRobinManager(StaTaskScheduler staScheduler)
        {
            // Create the task (it just runs forever until dispose is called)
            _task = Task.Factory.StartNew(() =>
            {
                Run(_addRemoveQueue, _waitHandle, _cancel.Token);
            }, _cancel.Token, TaskCreationOptions.LongRunning, staScheduler);
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
                // Cancel
                _cancel.Cancel();
                _waitHandle.Set();      // it may be waiting for this to wake it up
                try
                {
                    _task.Wait();
                }
                catch (Exception) { }

                // Clean up
                _task.Dispose();
            }
        }

        #endregion

        #region Public Methods

        public void Add(IRoundRobinWorker worker)
        {
            _addRemoveQueue.Enqueue(Tuple.Create(true, worker));
            _waitHandle.Set();      // this will wake up the thread if it's waiting
        }
        public void Remove(IRoundRobinWorker worker)
        {
            _addRemoveQueue.Enqueue(Tuple.Create(false, worker));
            _waitHandle.Set();      // this will wake up the thread if it's waiting
        }

        #endregion

        #region Private Methods - worker thread

        /// <summary>
        /// This is the main method in the long running thread (it is running in its own thread)
        /// </summary>
        private static void Run(ConcurrentQueue<Tuple<bool, IRoundRobinWorker>> addRemoveQueue, EventWaitHandle waitHandle, CancellationToken cancel)
        {
            //NOTE: This method is running on an arbitrary thread
            try
            {
                List<IRoundRobinWorker> workers = new List<IRoundRobinWorker>();

                while (!cancel.IsCancellationRequested)
                {
                    // Add items
                    AddRemoveItems(addRemoveQueue, waitHandle, workers);

                    if (workers.Count == 0)
                    {
                        // No workers.  Wait until the add method is called
                        waitHandle.WaitOne();
                        continue;
                    }

                    // Let each do one step
                    int index = 0;
                    while (index < workers.Count)
                    {
                        bool shouldRemove = false;
                        try
                        {
                            if (!workers[index].Step())
                            {
                                shouldRemove = true;
                            }
                        }
                        catch (Exception)
                        {
                            //NOTE: This is eating the exception.  The called step method should have caught the exception
                            shouldRemove = true;
                        }

                        if (shouldRemove)
                        {
                            workers.RemoveAt(index);
                        }
                        else
                        {
                            index++;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Don't leak errors, just go away
            }
        }

        private static void AddRemoveItems(ConcurrentQueue<Tuple<bool, IRoundRobinWorker>> addRemoveQueue, EventWaitHandle waitHandle, List<IRoundRobinWorker> workers)
        {
            waitHandle.Reset();

            Tuple<bool, IRoundRobinWorker> worker;
            while (addRemoveQueue.TryDequeue(out worker))
            {
                if (worker.Item1)
                {
                    // Add
                    workers.Add(worker.Item2);
                }
                else
                {
                    // Remove
                    int index = 0;
                    while (index < workers.Count)
                    {
                        if (workers[index].Token == worker.Item2.Token)
                        {
                            workers.RemoveAt(index);        // there should never be more than one, but just loop through them all to be sure
                        }
                        else
                        {
                            index++;
                        }
                    }
                }
            }
        }

        #endregion
        #region Private Methods - caller thread

        #endregion
    }

    #region Interface: IRoundRobinWorker

    public interface IRoundRobinWorker
    {
        /// <summary>
        /// This gives the woker a chance to do one iteration of work
        /// </summary>
        /// <remarks>
        /// Remember that many workers will share a thread, and be called round robin.  So step can't take very long
        /// </remarks>
        /// <returns>
        /// True: This worker has more to do, keep calling Step.
        /// False: This worker is finished, and should never be called again.
        /// </returns>
        bool Step();

        /// <summary>
        /// This is needed when removing a worker.  It's a unique ID to be able to find it
        /// </summary>
        long Token { get; }
    }

    #endregion
}
