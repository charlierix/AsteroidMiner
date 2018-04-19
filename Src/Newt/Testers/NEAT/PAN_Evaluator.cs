using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.HelperClassesCore;
using SharpNeat.Core;
using SharpNeat.Phenomes;

namespace Game.Newt.Testers.NEAT
{
    /// <summary>
    /// A phenome seems to be what sharpneat calls an instance of a neural net
    /// </summary>
    /// <remarks>
    /// I'm guessing that genome describes roughly how to create a neural net, and phenome are specific instance
    /// </remarks>
    public class PAN_Evaluator : IPhenomeEvaluator<IBlackBox>
    {
        #region Declaration Section

        private const double ERRORMULT = 100;

        private readonly bool[][] _trueVectors;
        private readonly Tuple<double[], double>[] _allCases;

        private readonly double _maxScore;

        #endregion

        #region Constructor

        /// <summary>
        /// PickANumber is meant to be very simple.  A few numbers (bit patterns) that should have an output of 1, all others have an output of 0
        /// </summary>
        /// <param name="trueVectors">These are the bit patterns that will return an output of 1</param>
        public PAN_Evaluator(int inputArrSize, bool[][] trueVectors)
        {
            _trueVectors = trueVectors;

            _allCases = new Tuple<double[], double>[Convert.ToInt32(Math.Pow(2, inputArrSize)) + 1];

            for (int cntr = 0; cntr < _allCases.Length; cntr++)
            {
                bool[] input = UtilityCore.ConvertToBase2(cntr, inputArrSize);

                bool isTrueCase = trueVectors.Any(o => UtilityCore.IsArrayEqual(o, input));

                _allCases[cntr] = Tuple.Create(
                    input.Select(o => o ? 1d : 0d).ToArray(),
                    isTrueCase ? 1d : 0d);
            }

            _allCases[_allCases.Length - 1] = Tuple.Create(
                Enumerable.Range(0, inputArrSize).Select(o => .5).ToArray(),
                0d);

            _maxScore = Math.Pow(ERRORMULT, inputArrSize);
        }

        #endregion

        #region IPhenomeEvaluator<IBlackBox>

        private ulong _evaluationCount;
        public ulong EvaluationCount
        {
            get
            {
                return _evaluationCount;
            }
        }

        public bool StopConditionSatisfied
        {
            get
            {
                return false;       // real world evaluators should have logic to know when to stop
            }
        }

        public FitnessInfo Evaluate(IBlackBox phenome)
        {
            double totalError = 0d;

            foreach (var sample in _allCases)
            {
                phenome.InputSignalArray.CopyFrom(sample.Item1, 0);

                phenome.Activate();

                double dist = Math.Abs(phenome.OutputSignalArray[0] - sample.Item2);

                // Square the error to really penalize invalid data, but need to get it above zero first
                dist *= ERRORMULT;
                dist = dist * dist;

                totalError += dist;
            }

            // Sharpneat wants larger numbers to mean better.  But this method is written in terms of error (zero being the best).  So
            // flip the number so that zero gives the best score
            //TODO: See if it's possible to make sharpneat allow zero as best
            totalError = _maxScore - totalError;

            _evaluationCount++;

            return new FitnessInfo(totalError, totalError);
        }

        public void Reset()
        {
        }

        #endregion
    }
}
