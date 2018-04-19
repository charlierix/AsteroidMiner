/* ***************************************************************************
 * This file is part of SharpNEAT - Evolution of Neural Networks.
 * 
 * Copyright 2004-2016 Colin Green (sharpneat@gmail.com)
 *
 * SharpNEAT is free software; you can redistribute it and/or modify
 * it under the terms of The MIT License (MIT).
 *
 * You should have received a copy of the MIT License
 * along with SharpNEAT; if not, see https://opensource.org/licenses/MIT.
 */
using System;
using System.Collections.Generic;
using System.Threading;
using log4net;
using SharpNeat.Core;
using System.Threading.Tasks;
using Game.HelperClassesCore.Threads;
using Game.HelperClassesCore;
using System.Threading.Tasks.Schedulers;

// Disable missing comment warnings for non-private variables.
#pragma warning disable 1591

namespace SharpNeat.EvolutionAlgorithms
{
    /// <summary>
    /// Abstract class providing some common/baseline data and methods for implementations of IEvolutionAlgorithm.
    /// </summary>
    /// <typeparam name="TGenome">The genome type that the algorithm will operate on.</typeparam>
    public abstract class AbstractGenerationalAlgorithm<TGenome> : IEvolutionAlgorithm<TGenome>, IRoundRobinWorker, IDisposable
        where TGenome : class, IGenome<TGenome>
    {
        private static readonly ILog __log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #region Instance Fields

        private static Lazy<RoundRobinManager> _roundRobin = new Lazy<RoundRobinManager>(() => new RoundRobinManager(new StaTaskScheduler(1)));

        protected IGenomeListEvaluator<TGenome> _genomeListEvaluator;
        protected IGenomeFactory<TGenome> _genomeFactory;
        protected List<TGenome> _genomeList;
        protected int _populationSize;
        protected TGenome _currentBestGenome;

        // Algorithm state data.
        RunState _runState = RunState.NotReady;
        protected uint _currentGeneration;

        // Update event scheme / data.
        UpdateScheme _updateScheme;
        uint _prevUpdateGeneration;
        long _prevUpdateTimeTick;


        //TODO: IEvolutionAlgorithm needs to implement IDisposable
        //TODO: Make a dispose request flag.  Use it similar to _pauseRequestFlag

        // Misc working variables.
        //Thread _algorithmThread;
        bool _pauseRequestFlag;
        readonly AutoResetEvent _awaitPauseEvent = new AutoResetEvent(false);
        //readonly AutoResetEvent _awaitRestartEvent = new AutoResetEvent(false);

        #endregion

        #region Events

        /// <summary>
        /// Notifies listeners that some state change has occurred.
        /// </summary>
        public event EventHandler UpdateEvent;
        /// <summary>
        /// Notifies listeners that the algorithm has paused.
        /// </summary>
        public event EventHandler PausedEvent;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the current generation.
        /// </summary>
        public uint CurrentGeneration
        {
            get { return _currentGeneration; }
        }

        public IGenomeFactory<TGenome> GenomeFactory => _genomeFactory;

        #endregion

        #region IEvolutionAlgorithm<TGenome> Members

        /// <summary>
        /// Gets or sets the algorithm's update scheme.
        /// </summary>
        public UpdateScheme UpdateScheme
        {
            get { return _updateScheme; }
            set { _updateScheme = value; }
        }

        /// <summary>
        /// Gets the current execution/run state of the IEvolutionAlgorithm.
        /// </summary>
        public RunState RunState
        {
            get { return _runState; }
        }

        /// <summary>
        /// Gets the population's current champion genome.
        /// </summary>
        public TGenome CurrentChampGenome
        {
            get { return _currentBestGenome; }
        }

        /// <summary>
        /// Gets a value indicating whether some goal fitness has been achieved and that the algorithm has therefore stopped.
        /// </summary>
        public bool StopConditionSatisfied
        {
            get { return _genomeListEvaluator.StopConditionSatisfied; }
        }

        /// <summary>
        /// Initializes the evolution algorithm with the provided IGenomeListEvaluator
        /// and an IGenomeFactory that can be used to create an initial population of genomes.
        /// </summary>
        /// <param name="genomeListEvaluator">The genome evaluation scheme for the evolution algorithm.</param>
        /// <param name="genomeFactory">The factory that was used to create the genomeList and which is therefore referenced by the genomes.</param>
        /// <param name="populationSize">The number of genomes to create for the initial population.</param>
        public virtual void Initialize(IGenomeListEvaluator<TGenome> genomeListEvaluator, IGenomeFactory<TGenome> genomeFactory, int populationSize)
        {
            //NOTE: Not calling the other public overload because it's virtual and that could mess with derived classes
            Initialize_private(genomeListEvaluator, genomeFactory, genomeFactory.CreateGenomeList(populationSize, _currentGeneration));
        }
        /// <summary>
        /// Initializes the evolution algorithm with the provided IGenomeListEvaluator, IGenomeFactory
        /// and an initial population of genomes.
        /// </summary>
        /// <param name="genomeListEvaluator">The genome evaluation scheme for the evolution algorithm.</param>
        /// <param name="genomeFactory">The factory that was used to create the genomeList and which is therefore referenced by the genomes.</param>
        /// <param name="genomeList">An initial genome population.</param>
        public virtual void Initialize(IGenomeListEvaluator<TGenome> genomeListEvaluator, IGenomeFactory<TGenome> genomeFactory, List<TGenome> genomeList)
        {
            Initialize_private(genomeListEvaluator, genomeFactory, genomeList);
        }

        /// <summary>
        /// Starts the algorithm running. The algorithm will switch to the Running state from either
        /// the Ready or Paused states.
        /// </summary>
        public void StartContinue()
        {
            // RunState must be Ready or Paused.
            if (RunState.Ready == _runState)
            {   // Create a new thread and start it running.
                //_algorithmThread = new Thread(AlgorithmThreadMethod);
                //_algorithmThread.IsBackground = true;
                //_algorithmThread.Priority = ThreadPriority.BelowNormal;
                _runState = RunState.Running;
                OnUpdateEvent();
                //_algorithmThread.Start();
                _roundRobin.Value.Add(this);
            }
            else if (RunState.Paused == _runState)
            {   // Thread is paused. Resume execution.
                _runState = RunState.Running;
                OnUpdateEvent();
                //_awaitRestartEvent.Set();
                _roundRobin.Value.Add(this);
            }
            else if (RunState.Running == _runState)
            {   // Already running. Log a warning.
                __log.Warn("StartContinue() called but algorithm is already running.");
            }
            else
            {
                throw new SharpNeatException(string.Format("StartContinue() call failed. Unexpected RunState [{0}]", _runState));
            }
        }

        /// <summary>
        /// Alias for RequestPause().
        /// </summary>
        public void Stop()
        {
            RequestPause();
        }

        /// <summary>
        /// Requests that the algorithm pauses but doesn't wait for the algorithm thread to stop.
        /// The algorithm thread will pause when it is next convenient to do so, and will notify
        /// listeners via an UpdateEvent.
        /// </summary>
        public void RequestPause()
        {
            if (RunState.Running == _runState)
            {
                _pauseRequestFlag = true;
                //_roundRobin.Value.Remove(this);       // letting the step method request the removal
            }
            else
            {
                __log.Warn("RequestPause() called but algorithm is not running.");
            }
        }

        /// <summary>
        /// Request that the algorithm pause and waits for the algorithm to do so. The algorithm
        /// thread will pause when it is next convenient to do so and notifies any UpdateEvent 
        /// listeners prior to returning control to the caller. Therefore it's generally a bad idea 
        /// to call this method from a GUI thread that also has code that may be called by the
        /// UpdateEvent - doing so will result in deadlocked threads.
        /// </summary>
        public void RequestPauseAndWait()
        {
            if (RunState.Running == _runState)
            {   // Set a flag that tells the algorithm thread to enter the paused state and wait 
                // for a signal that tells us the thread has paused.
                _pauseRequestFlag = true;
                //_roundRobin.Value.Remove(this);       // don't explicitly remove it.  Let the step method see the pause request and request removal.  It will also set the _awaitPauseEvent
                _awaitPauseEvent.WaitOne();
            }
            else
            {
                __log.Warn("RequestPauseAndWait() called but algorithm is not running.");
            }
        }

        #endregion
        #region IRoundRobinWorker Members

        public bool Step1()
        {
            _currentGeneration++;
            PerformOneGeneration();

            if (ShouldRaiseUpdate())
            {
                _prevUpdateGeneration = _currentGeneration;
                _prevUpdateTimeTick = DateTime.UtcNow.Ticks;
                OnUpdateEvent();
            }

            // Check if a pause has been requested. 
            // Access to the flag is not thread synchronized, but it doesn't really matter if
            // we miss it being set and perform one other generation before pausing.
            if (_pauseRequestFlag || _genomeListEvaluator.StopConditionSatisfied)
            {
                // Signal to any waiting thread that we are pausing
                _awaitPauseEvent.Set();

                // Reset the flag. Update RunState and notify any listeners of the state change.
                _pauseRequestFlag = false;
                _runState = RunState.Paused;
                OnUpdateEvent();
                OnPausedEvent();

                // Wait indefinitely for a signal to wake up and continue.
                //_awaitRestartEvent.WaitOne();

                return false;
            }
            else
            {
                return true;
            }
        }

        private readonly long _token = TokenGenerator.NextToken();
        public long Token => _token;

        #endregion
        #region IDisposable Members

        private bool _disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    _roundRobin.Value.Remove(this);     // it may already be removed, but just making sure
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                _disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~AbstractGenerationalAlgorithm() {
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

        #region Private/Protected Methods [Evolution Algorithm]

        private void Initialize_private(IGenomeListEvaluator<TGenome> genomeListEvaluator, IGenomeFactory<TGenome> genomeFactory, List<TGenome> genomeList)
        {
            _prevUpdateGeneration = 0;
            _prevUpdateTimeTick = DateTime.UtcNow.Ticks;

            _currentGeneration = 0;
            _genomeListEvaluator = genomeListEvaluator;
            _genomeFactory = genomeFactory;
            _genomeList = genomeList;
            _populationSize = _genomeList.Count;
            _runState = RunState.Ready;
            _updateScheme = new UpdateScheme(new TimeSpan(0, 0, 1));
        }

        /// <summary>
        /// Returns true if it is time to raise an update event.
        /// </summary>
        private bool ShouldRaiseUpdate()
        {
            if (UpdateMode.Generational == _updateScheme.UpdateMode)
            {
                return (_currentGeneration - _prevUpdateGeneration) >= _updateScheme.Generations;
            }

            return (DateTime.UtcNow.Ticks - _prevUpdateTimeTick) >= _updateScheme.TimeSpan.Ticks;
        }

        private void OnUpdateEvent()
        {
            if (null != UpdateEvent)
            {
                // Catch exceptions thrown by even listeners. This prevents listener exceptions from terminating the algorithm thread.
                try
                {
                    UpdateEvent(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    __log.Error("UpdateEvent listener threw exception", ex);
                }
            }
        }

        private void OnPausedEvent()
        {
            if (null != PausedEvent)
            {
                // Catch exceptions thrown by even listeners. This prevents listener exceptions from terminating the algorithm thread.
                try
                {
                    PausedEvent(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    __log.Error("PausedEvent listener threw exception", ex);
                }
            }
        }

        /// <summary>
        /// Progress forward by one generation. Perform one generation/cycle of the evolution algorithm.
        /// </summary>
        protected abstract void PerformOneGeneration();

        #endregion
    }
}
