using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Game.HelperClassesCore;
using Game.HelperClassesCore.Threads;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.Testers
{
    /// <summary>
    /// The final goal of this tester is to figure out how to make a background thread that supports a timer that has ticks in that same
    /// thread (and if a tick handler takes too long, throw away extra ticks)
    /// </summary>
    public partial class MultithreadWorlds : Window
    {
        // moved to Game.HelperClassesCore.TimerCreateThread
        #region class: MyTimer

        ///// <summary>
        ///// Here is a stub for the type of timer I want to create
        ///// </summary>
        //private class MyTimer : IDisposable
        //{
        //    //This isn't needed
        //    #region class: MyThreadPool

        //    //private class MyThreadPool
        //    //{
        //    //    #region Declaration Section

        //    //    private static Lazy<MyThreadPool> _instance = new Lazy<MyThreadPool>();     // using lazy to make this class a singleton

        //    //    private readonly object _lock = new object();

        //    //    private readonly Thread _watcherThread;

        //    //    private readonly ManualResetEvent _activityEvent = new ManualResetEvent(false);

        //    //    private readonly List<Tuple<Thread, ManualResetEvent>> _watchedThreads = new List<Tuple<Thread, ManualResetEvent>>();

        //    //    #endregion

        //    //    #region Constructor

        //    //    // Making this private so that from the outside, only the public static methods can be used (this class is a singleton)
        //    //    private MyThreadPool()
        //    //    {
        //    //        // Once constructed, this thread will live for the rest of this process
        //    //        _watcherThread = new Thread(ThreadMethod);
        //    //        _watcherThread.IsBackground = true;
        //    //        _watcherThread.Start();
        //    //    }

        //    //    #endregion

        //    //    #region Public Methods

        //    //    /// <summary>
        //    //    /// This adds a thread for this class to manage.  When the kill event fires, the thread will be removed from memory
        //    //    /// </summary>
        //    //    public static void WatchThread(Thread thread, ManualResetEvent killEvent)
        //    //    {
        //    //        _instance.Value.WatchThread_priv(thread, killEvent);
        //    //    }

        //    //    #endregion

        //    //    #region Private Methods

        //    //    private void ThreadMethod()
        //    //    {
        //    //        Thread[] threads = null;
        //    //        WaitHandle[] handles = null;

        //    //        while (true)
        //    //        {
        //    //            lock (_lock)
        //    //            {
        //    //                threads = _watchedThreads.Select(o => o.Item1).ToArray();
        //    //                handles = UtilityHelper.Iterate(_watchedThreads.Select(o => o.Item2), new ManualResetEvent[] { _activityEvent }).ToArray();
        //    //            }

        //    //            // Hang out until something happens
        //    //            int handleIndex = WaitHandle.WaitAny(handles);

        //    //            if (handleIndex < threads.Length)       // the other one is threads.Length, which is _activityEvent.  If that ticks, just let the loop repeat to pick up the new thread to watch
        //    //            {
        //    //                threads[handleIndex].IsAlive

        //    //                // A thread kill event fired
        //    //                threads[handleIndex].Join();


        //    //            }











        //    //        }





        //    //    }

        //    //    private void WatchThread_priv(Thread thread, ManualResetEvent killEvent)
        //    //    {
        //    //        // Store this
        //    //        lock (_lock)
        //    //        {
        //    //            _watchedThreads.Add(Tuple.Create(thread, killEvent));
        //    //        }

        //    //        // Ring the bell
        //    //        _activityEvent.Set();
        //    //    }

        //    //    #endregion
        //    //}

        //    #endregion

        //    #region Declaration Section

        //    public delegate object PrepDelegate(params object[] args);
        //    public delegate void TickDelegate(object arg, double elapsedTime);
        //    public event TickDelegate Tick = null;      // also exposing as an event so that multiple handlers can be added if desired
        //    public delegate void TearDownDelegate(object arg);

        //    #endregion

        //    #region Constructor

        //    /// <summary>
        //    /// This creates a new timer that runs on a new thread (not threadpool thread).  All three of the delegates passed in will be executed
        //    /// from that newly created threads
        //    /// </summary>
        //    /// <param name="prep">
        //    /// This gives the caller a chance to set up objects for the tick to use (those objects won't need to be threadsafe, because they will be created and
        //    /// used only on the background thread for this instance of timer
        //    /// </param>
        //    /// <param name="tick">
        //    /// This gets fired every internal (arg is the object returned from prep
        //    /// NOTE: If the code in this tick takes longer than interval, subsequent ticks will just get eaten, so no danger
        //    /// NOTE: You can pass a delegate here, or use events (or both)
        //    /// </param>
        //    /// <param name="tearDown">
        //    /// This gets called when this timer is disposed.  Gives the user a chance to dispose anything they created in prep
        //    /// </param>
        //    public MyTimer(PrepDelegate prep = null, TickDelegate tick = null, TearDownDelegate tearDown = null)
        //    {
        //    }

        //    #endregion

        //    #region IDisposable Members

        //    public void Dispose()
        //    {
        //        // This will call the teardown delegate, then get rid of the underlying timer and thread
        //    }

        //    #endregion

        //    #region Public Properties

        //    // Milliseconds (default to 100, that's what the underlying timer's default is)
        //    public double Interval { get; set; }

        //    #endregion

        //    #region Public Methods

        //    // These could also be thought of as pause/resume
        //    public void Start()
        //    {
        //    }
        //    public void Stop()
        //    {
        //    }

        //    #endregion
        //}

        #endregion

        #region Declaration Section

        private CancellationTokenSource _cancel1 = null;
        private CancellationTokenSource _cancel2 = null;
        private ManualResetEvent _cancel3 = null;

        private List<TimerCreateThread<World>> _timers4 = new List<TimerCreateThread<World>>();

        #endregion

        #region Constructor

        public MultithreadWorlds()
        {
            InitializeComponent();

            this.Background = SystemColors.ControlBrush;
        }

        #endregion

        #region Event Listeners

        private void btnUI100_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                World world = new World(false);
                world.UnPause();

                for (int cntr = 0; cntr < 100; cntr++)
                {
                    Thread.Sleep(50);
                    world.Update();
                }

                world.Pause();      // pause really isn't necessary, since there isn't a timer to stop
                world.Dispose();

                MessageBox.Show("Finished", this.Title, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnTask100_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Task task = Task.Run(() =>
                    {
                        World world = new World(false);
                        world.UnPause();

                        for (int cntr = 0; cntr < 100; cntr++)
                        {
                            Thread.Sleep(50);
                            world.Update();
                        }

                        world.Pause();      // pause really isn't necessary, since there isn't a timer to stop
                        world.Dispose();
                    });

                task.ContinueWith(t =>
                {
                    MessageBox.Show("Finished", this.Title, MessageBoxButton.OK, MessageBoxImage.Information);
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnLongSleep0_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_cancel1 == null)
                {
                    _cancel1 = new CancellationTokenSource();
                }

                CancellationToken cancelToken = _cancel1.Token;

                Task task = Task.Factory.StartNew(a =>
                {
                    World world = new World(false);
                    world.UnPause();

                    while (!cancelToken.IsCancellationRequested)
                    {
                        Thread.Sleep(0);
                        world.Update();
                    }

                    world.Pause();      // pause really isn't necessary, since there isn't a timer to stop
                    world.Dispose();
                }, TaskCreationOptions.LongRunning, _cancel1.Token);

                task.ContinueWith(t =>
                {
                    _cancel1 = null;
                    MessageBox.Show("Finished", this.Title, MessageBoxButton.OK, MessageBoxImage.Information);
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnLongSleep0Cancel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_cancel1 == null)
                {
                    MessageBox.Show("Nothing to cancel", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    _cancel1.Cancel();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// http://stackoverflow.com/questions/9853216/alternative-to-thread-sleep
        /// </summary>
        private void btnLongTimerWait_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_cancel2 == null)
                {
                    _cancel2 = new CancellationTokenSource();
                }

                CancellationToken cancelToken = _cancel2.Token;

                Task task = Task.Factory.StartNew(a =>
                {
                    // It is from the thread pool.  Explicitly create a thread for cases like this instead of using task
                    //if (Thread.CurrentThread.IsThreadPoolThread)
                    //{
                    //    throw new ApplicationException("Long running thread can't be from the thread pool");
                    //}

                    // World
                    World world = new World(false);
                    world.UnPause();

                    #region Launch Timer

                    // Put a timer in another thread
                    ManualResetEvent elapsedEvent = new ManualResetEvent(false);

                    System.Timers.Timer timer = new System.Timers.Timer();
                    timer.Interval = 25;
                    timer.Elapsed += delegate { elapsedEvent.Set(); };
                    timer.AutoReset = false;
                    timer.Start();

                    #endregion

                    while (!cancelToken.IsCancellationRequested)
                    {
                        // Wait for the timer to tick and send a message
                        elapsedEvent.WaitOne();
                        elapsedEvent.Reset();
                        timer.Start();

                        world.Update();
                    }

                    timer.Stop();

                    world.Pause();      // pause really isn't necessary, since there isn't a timer to stop
                    world.Dispose();
                }, TaskCreationOptions.LongRunning, _cancel2.Token);

                task.ContinueWith(t =>
                {
                    _cancel2 = null;
                    MessageBox.Show("Finished", this.Title, MessageBoxButton.OK, MessageBoxImage.Information);
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnLongTimerWaitCancel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_cancel2 == null)
                {
                    MessageBox.Show("Nothing to cancel", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    _cancel2.Cancel();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnThreadTimerWait_Click(object sender, RoutedEventArgs e)
        {
            try
            {


                MessageBox.Show("finish this", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;



                if (_cancel3 == null)
                {
                    _cancel3 = new ManualResetEvent(false);
                }

                Thread workerThread = new Thread(() =>
                {
                    // World
                    World world = new World(false);
                    world.UnPause();

                    #region Launch Timer

                    // Put a timer in another thread
                    ManualResetEvent elapsedEvent = new ManualResetEvent(false);

                    System.Timers.Timer timer = new System.Timers.Timer();
                    timer.Interval = 25;
                    timer.Elapsed += delegate { elapsedEvent.Set(); };
                    timer.AutoReset = false;
                    timer.Start();

                    #endregion

                    WaitHandle[] handles = new WaitHandle[] { _cancel3, elapsedEvent };

                    while (true)
                    {
                        #region Wait for signal to go

                        int handleIndex = WaitHandle.WaitAny(handles);

                        if (handleIndex == 0)
                        {
                            // This thread needs to finish
                            break;      // I would prefer a switch statement, but there is no Exit While statement
                        }
                        else if (handleIndex == 1)
                        {
                            // Timer ticked
                            elapsedEvent.Reset();
                            timer.Start();
                        }
                        else
                        {
                            throw new ApplicationException("Unknown wait handle: " + handleIndex.ToString());
                        }

                        #endregion

                        world.Update();
                    }

                    timer.Stop();

                    world.Pause();      // pause really isn't necessary, since there isn't a timer to stop
                    world.Dispose();
                });

                workerThread.Start();



                //TODO: Figure out how to terminate the thread from this calling thread.  This article says to keep looping and checking workerThread.IsAlive.
                //That's a mess.  If that's the only way, then set up a little thread pool manager to do this
                //http://msdn.microsoft.com/en-us/library/7a2f3ay4(v=vs.90).aspx

                //task.ContinueWith(t =>
                //{
                //    workerThread.Join();
                //    workerThread = null;
                //    _cancel3 = null;
                //    MessageBox.Show("Finished", this.Title, MessageBoxButton.OK, MessageBoxImage.Information);
                //}, TaskScheduler.FromCurrentSynchronizationContext());


            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnThreadTimerWaitCancel_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                MessageBox.Show("finish this", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;


            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnTimerCreateThread_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TimerCreateThread<World> timer = new TimerCreateThread<World>(Prep4, Tick4, Teardown4);
                _timers4.Add(timer);

                timer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnTimerCreateThreadCancel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (var timer in _timers4)
                {
                    timer.Dispose();
                }

                _timers4.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private static World Prep4(params object[] args)
        {
            World world = new World(false);
            world.UnPause();

            return world;
        }
        private static void Tick4(World world, double elapsedTime)
        {
            world.Update();
        }
        private static void Teardown4(World world)
        {
            world.Pause();      // pause really isn't necessary, since there isn't a timer to stop
            world.Dispose();
        }

        #endregion
    }
}
