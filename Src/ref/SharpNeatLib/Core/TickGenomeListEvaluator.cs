using Game.HelperClassesCore;
using Game.HelperClassesCore.Threads;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SharpNeat.Core
{
    public class TickGenomeListEvaluator<TGenome, TPhenome> : IGenomeListEvaluator<TGenome>
        where TGenome : class, IGenome<TGenome>
        where TPhenome : class
    {
        #region class: EvaluateWorker

        private class EvaluateWorker : IRoundRobinWorker
        {
            #region Declaration Section

            private readonly IGenomeDecoder<TGenome, TPhenome> _genomeDecoder;
            private readonly IPhenomeTickEvaluator<TPhenome, TGenome>[] _phenomeEvaluators;
            private readonly TGenome[] _genomes;
            private readonly EventWaitHandle _waitHandle;
            private readonly Func<double> _worldTick;

            /// <summary>
            /// This gets set to true once step is called and set everything up
            /// </summary>
            private bool _initialized = false;

            //---------------- everything below is set up once _initialized is true

            List<TGenome> _remainingGenomes = null;
            List<IPhenomeTickEvaluator<TPhenome, TGenome>> _availableWorkers = null;
            List<(IPhenomeTickEvaluator<TPhenome, TGenome> worker, TGenome genome)> _runningWorkers = null;
            private bool _hasSetWaitHandle = false;

            #endregion

            #region Constructor

            public EvaluateWorker(IGenomeDecoder<TGenome, TPhenome> genomeDecoder, IPhenomeTickEvaluator<TPhenome, TGenome>[] phenomeEvaluators, TGenome[] genomes, EventWaitHandle waitHandle, Func<double> worldTick)
            {
                _genomeDecoder = genomeDecoder;
                _phenomeEvaluators = phenomeEvaluators;
                _genomes = genomes;
                _waitHandle = waitHandle;
                _worldTick = worldTick;
            }

            #endregion

            #region Public Properties

            private readonly long _token = TokenGenerator.NextToken();
            public long Token => _token;

            #endregion

            #region Public Methods

            public bool Step1()
            {
                if (!_initialized)
                {
                    _remainingGenomes = new List<TGenome>(_genomes);
                    _availableWorkers = new List<IPhenomeTickEvaluator<TPhenome, TGenome>>(_phenomeEvaluators);
                    _runningWorkers = new List<(IPhenomeTickEvaluator<TPhenome, TGenome> worker, TGenome genome)>();

                    _initialized = true;
                }

                if (_remainingGenomes.Count == 0 && _runningWorkers.Count == 0)
                {
                    if (!_hasSetWaitHandle)
                    {
                        // The work is done, tell the manager to stop calling tick
                        //NOTE: Only want to call this once, because the waiting thread will dispose the waithandle after it gets the signal
                        _waitHandle.Set();      // this tells the calling evaluator to stop waiting
                        _hasSetWaitHandle = true;
                    }

                    return false;
                }

                AssignWorkers(_remainingGenomes, _availableWorkers, _runningWorkers, _genomeDecoder);

                // Call a delegate to let the caller advance the world one tick
                double elapsedTime = _worldTick();

                // Advance each worker one tick
                int index = 0;
                while (index < _runningWorkers.Count)
                {
                    FitnessInfo? score = _runningWorkers[index].worker.EvaluateTick(elapsedTime);

                    if (score == null)
                    {
                        // This worker needs more time to evaluate its current phenome
                        index++;
                    }
                    else
                    {
                        // This worker is finished with the phenome and given a score.  Store the score and free up the worker
                        _runningWorkers[index].genome.EvaluationInfo.SetFitness(score.Value._fitness);
                        _runningWorkers[index].genome.EvaluationInfo.AuxFitnessArr = score.Value._auxFitnessArr;

                        _availableWorkers.Add(_runningWorkers[index].worker);
                        _runningWorkers.RemoveAt(index);
                    }
                }

                // There is more to do, tell the manager to call again (could test if finished here as well, but just let this method be called again, and
                // only have finalization logic in one place)
                return true;
            }

            #endregion
        }

        #endregion

        #region Events

        public event EventHandler<TickGenomeListEvaluator_WorldTickArgs> WorldTick = null;

        #endregion

        #region Declaration Section

        private readonly IGenomeDecoder<TGenome, TPhenome> _genomeDecoder;

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// The other genome evaluators just have one phenome evaluator instance.  But this class could have many instances loaded at once
        /// 
        /// NOTE: There can be fewer evaluators than genomes per run
        /// </remarks>
        private readonly IPhenomeTickEvaluator<TPhenome, TGenome>[] _phenomeEvaluators;

        private readonly RoundRobinManager _roundRobinManager;
        private readonly Func<double> _worldTick;

        #endregion

        #region Constructor

        /// <summary>
        /// 
        /// </summary>
        /// <param name="genomeDecoder"></param>
        /// <param name="phenomeEvaluators"></param>
        /// <param name="worldTick">This gets called from the worker thread and lets the caller advance the world one tick (the caller then returns the amount of elapsed time in seconds)</param>
        public TickGenomeListEvaluator(IGenomeDecoder<TGenome, TPhenome> genomeDecoder, IPhenomeTickEvaluator<TPhenome, TGenome>[] phenomeEvaluators, RoundRobinManager roundRobinManager, Func<double> worldTick)
        {
            _genomeDecoder = genomeDecoder;
            _phenomeEvaluators = phenomeEvaluators;
            _worldTick = worldTick;
            _roundRobinManager = roundRobinManager;
        }

        #endregion

        #region IGenomeListEvaluator<TGenome> Members

        private ulong _evaluationCount = 0;
        public ulong EvaluationCount => _evaluationCount;

        /// <summary>
        /// NOTE: The other genome evaluators just return _phenomeEval.StopConditionSatisfied, but those only have one, this has many.
        /// So if you want evaluations to stop, do it another way (or add a public setter)
        /// </summary>
        public bool StopConditionSatisfied => false;

        private void Evaluate_ORIG(IList<TGenome> genomeList)
        {
            _evaluationCount++;



            //TODO: this section needs to run from within a worker thread.  This class needs to hold a reference to an instance of
            //RoundRobinManager.  The below logic needs to be inside a class that implements IRoundRobinWorker
            //---------------------------------------------------------

            var remainingGenomes = new List<TGenome>(genomeList);
            var availableWorkers = new List<IPhenomeTickEvaluator<TPhenome, TGenome>>(_phenomeEvaluators);
            var runningWorkers = new List<(IPhenomeTickEvaluator<TPhenome, TGenome> worker, TGenome genome)>();

            while (remainingGenomes.Count > 0 || runningWorkers.Count > 0)
            {
                AssignWorkers(remainingGenomes, availableWorkers, runningWorkers, _genomeDecoder);

                // Raise an event to let the caller advance the world one tick
                double elapsedTime = OnWorldTick();

                // Advance each worker one tick
                int index = 0;
                while (index < runningWorkers.Count)
                {
                    FitnessInfo? score = runningWorkers[index].worker.EvaluateTick(elapsedTime);

                    if (score == null)
                    {
                        // This worker needs more time to evaluate its current phenome
                        index++;
                    }
                    else
                    {
                        // This worker is finished with the phenome and given a score.  Store the score and free up the worker
                        runningWorkers[index].genome.EvaluationInfo.SetFitness(score.Value._fitness);
                        runningWorkers[index].genome.EvaluationInfo.AuxFitnessArr = score.Value._auxFitnessArr;

                        availableWorkers.Add(runningWorkers[index].worker);
                        runningWorkers.RemoveAt(index);
                    }
                }
            }

            //---------------------------------------------------------




        }
        public void Evaluate(IList<TGenome> genomeList)
        {
            _evaluationCount++;

            using (EventWaitHandle waitHandle = new AutoResetEvent(false))
            {

                EvaluateWorker worker = new EvaluateWorker(_genomeDecoder, _phenomeEvaluators, genomeList.ToArray(), waitHandle, _worldTick);

                _roundRobinManager.Add(worker);

                // Hang this thread until it's done (it seems a bit silly to have two threads when this thread is just hanging, which could just be a bad
                // design.  One advantage of this design is that you could have one world being used by many evaluators, but I don't think it's
                // that expensive to have one world per evaluator)
                //
                // Thinking about it more, genome evaluator needs a tick aware interface as well so if you use IPhenomeTickEvaluator, it should be
                // run from a IGenomeTickEvaluator that is part of the same thread as the phenome evaluators
                waitHandle.WaitOne();


                //TODO: Figure out why execution never gets back to here


                _roundRobinManager.Remove(worker);
            }
        }

        public void Reset()
        {
            _evaluationCount = 0;

            foreach (var phenomeEval in _phenomeEvaluators)
            {
                phenomeEval.Reset();
            }
        }

        #endregion

        #region Private Methods

        private static void AssignWorkers(List<TGenome> remainingGenomes, List<IPhenomeTickEvaluator<TPhenome, TGenome>> availableWorkers, List<(IPhenomeTickEvaluator<TPhenome, TGenome> worker, TGenome genome)> runningWorkers, IGenomeDecoder<TGenome, TPhenome> genomeDecoder)
        {
            while (true)
            {
                if (availableWorkers.Count == 0 || remainingGenomes.Count == 0)
                {
                    return;
                }

                // Get the next genome
                TGenome genome = remainingGenomes[0];
                remainingGenomes.RemoveAt(0);

                TPhenome phenome = genomeDecoder.Decode(genome);
                if (phenome == null)
                {
                    // Non-viable genome.
                    genome.EvaluationInfo.SetFitness(0);
                    genome.EvaluationInfo.AuxFitnessArr = null;
                    continue;
                }

                // Assign to the next worker
                var worker = availableWorkers[0];
                availableWorkers.RemoveAt(0);

                worker.StartNewEvaluation(phenome, genome);

                runningWorkers.Add((worker, genome));
            }
        }

        private double OnWorldTick()
        {
            const double ELAPSED = 1d;

            if (WorldTick == null)
            {
                return ELAPSED;
            }

            TickGenomeListEvaluator_WorldTickArgs args = new TickGenomeListEvaluator_WorldTickArgs();
            WorldTick(this, args);

            return args.ElapsedTime ?? ELAPSED;
        }

        #endregion
    }

    #region class: TickGenomeListEvaluator_WorldTickArgs

    public class TickGenomeListEvaluator_WorldTickArgs
    {
        /// <summary>
        /// This isn't set by TickGenomeListEvaluator.  Instead the listener sets this to the value that was used.
        /// This elapsed time will be given to each phenome
        /// </summary>
        public double? ElapsedTime { get; set; }
    }

    #endregion
}
