using Game.HelperClassesCore;
using Game.HelperClassesCore.Threads;
using Game.Newt.v2.NewtonDynamics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Schedulers;
using System.Windows.Media.Media3D;

namespace Game.Newt.v2.Arcanorum.ArenaTester
{
    /// <summary>
    /// This gets instantiated and passed around from a main thread.  Then later from within a worker thread, the world will
    /// be requested (first request will create the world)
    /// </summary>
    public class WorldAccessor : IDisposable
    {
        #region Events

        public event EventHandler Initialized = null;

        #endregion

        #region Declaration Section

        private readonly Point3D _boundryMin;
        private readonly Point3D _boundryMax;

        /// <summary>
        /// This is used when telling the world to update (in seconds)
        /// </summary>
        /// <remarks>
        /// Null: pass null to the update method.  It will use the real elased time
        /// elapsed: this is the number of seconds to pass in +- elapsed*randPercent
        /// </remarks>
        private readonly (double elapsed, double randPercent)? _worldUpdateTime;

        //private bool _isInitialized = false;
        private int? _initializedThreadID = null;

        #endregion

        #region Constructor

        public WorldAccessor(Point3D boundryMin, Point3D boundryMax, (double elapsed, double randPercent)? worldUpdateTime = null)
        {
            _boundryMin = boundryMin;
            _boundryMax = boundryMax;
            _worldUpdateTime = worldUpdateTime;

            _roundRobinManager = new RoundRobinManager(new StaTaskScheduler(1));
        }

        #endregion

        #region IDisposable

        private bool _disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).

                    if (_world != null)
                    {
                        _world.Dispose();
                        _world = null;
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                _disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~WorldAccessor() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        #endregion

        #region Public Properties

        private World _world = null;
        public World World
        {
            get
            {
                if (_initializedThreadID == null)
                {
                    throw new InvalidOperationException("Must call EnsureInitialized first");
                }
                else if (_initializedThreadID.Value != Thread.CurrentThread.ManagedThreadId)
                {
                    throw new ApplicationException(string.Format("World must always be accessed from the same worker thread.  initial call: {0}, this call: {1}", _initializedThreadID.Value, Thread.CurrentThread.ManagedThreadId));
                }

                return _world;
            }
        }

        private readonly RoundRobinManager _roundRobinManager;
        public RoundRobinManager RoundRobinManager => _roundRobinManager;

        #endregion

        #region Public Methods

        /// <summary>
        /// This method must be called from the worker thread
        /// </summary>
        public void EnsureInitialized()
        {
            if (_initializedThreadID != null)
            {
                if (_initializedThreadID.Value != Thread.CurrentThread.ManagedThreadId)
                {
                    throw new ApplicationException(string.Format("EnsureInitialized must always be called from the same worker thread.  initial call: {0}, this call: {1}", _initializedThreadID.Value, Thread.CurrentThread.ManagedThreadId));
                }

                return;
            }

            _initializedThreadID = Thread.CurrentThread.ManagedThreadId;

            _world = new World(false);
            _world.SetCollisionBoundry(_boundryMin, _boundryMax);

            _world.UnPause();

            Initialized?.Invoke(this, new EventArgs());
        }

        public double UpdateWorld()
        {
            double? elapsed = null;
            if (_worldUpdateTime != null)
            {
                if (_worldUpdateTime.Value.randPercent.IsNearZero())
                {
                    elapsed = _worldUpdateTime.Value.elapsed;
                }
                else
                {
                    // Randomize it a bit so it feels more real
                    elapsed = StaticRandom.NextPercent(_worldUpdateTime.Value.elapsed, _worldUpdateTime.Value.randPercent);
                }
            }

            return World.Update(elapsed);
        }

        #endregion
    }
}
