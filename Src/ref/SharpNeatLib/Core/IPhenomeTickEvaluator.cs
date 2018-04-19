using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpNeat.Core
{
    /// <summary>
    /// IPhenomeEvaluator wants a phenome to be evaluated in one shot.  This evaluates over many timer ticks
    /// </summary>
    /// <remarks>
    /// This is needed for more complex cases where a bot would be added to a world, then run alongside other bots
    /// for a while before getting a final score.  Expected usage:
    /// 
    /// forall(evals)
    ///     eval.StartNew(phenome)
    /// 
    /// do
    ///     forall(remainingEvals)
    ///         eval.tick()  // once tick returns a score, it gets removed from the remaining list
    /// while(remainingEvals.count > 0)
    /// 
    /// compare scores
    /// </remarks>
    public interface IPhenomeTickEvaluator<TPhenome, TGenome>
    {
        /// <summary>
        /// Gets the total number of individual genome evaluations that have been performed by this evaluator.
        /// </summary>
        ulong EvaluationCount { get; }

        /// <summary>
        /// Gets a value indicating whether some goal fitness has been achieved and that
        /// the evolutionary algorithm search should stop. This property's value can remain false
        /// to allow the algorithm to run indefinitely.
        /// </summary>
        bool StopConditionSatisfied { get; }

        /// <summary>
        /// Reset the internal state of the evaluation scheme if any exists.
        /// </summary>
        void Reset();

        /// <summary>
        /// This starts a new scoring run
        /// </summary>
        /// <param name="phenome">This is neural net to be scored</param>
        /// <param name="genome">
        /// This isn't necessary for evaluating, but it's impossible to serialize/deserialize a phenome by itself - it must be created from
        /// a genome.  The genome evaluator already has the genome that made the phenome, so it's not much hassle to request the
        /// genome here
        /// </param>
        void StartNewEvaluation(TPhenome phenome, TGenome genome);

        /// <summary>
        /// Evaluate for one "tick".  Returns null if it expects more ticks.  Returns a score when it's done evaluating
        /// </summary>
        FitnessInfo? EvaluateTick(double elapedTime);
    }
}
