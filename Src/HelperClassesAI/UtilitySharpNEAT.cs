using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.HelperClassesCore;
using SharpNeat.Phenomes;

namespace Game.HelperClassesAI
{
    public static class UtilitySharpNEAT
    {
    }

    #region class: RandomBlackBoxNetwork

    /// <summary>
    /// This is meant for unit tests.  It produces random output
    /// </summary>
    public class RandomBlackBoxNetwork : IBlackBox
    {
        private readonly bool _isPositiveOnly;

        public RandomBlackBoxNetwork(int inputCount, int outputCount, bool isPositiveOnly)
        {
            InputCount = inputCount;
            OutputCount = outputCount;

            _isPositiveOnly = isPositiveOnly;

            InputSignalArray = new SignalArray(new double[inputCount], 0, inputCount);
            OutputSignalArray = new SignalArray(new double[outputCount], 0, outputCount);
        }

        public int InputCount
        {
            get;
            private set;
        }
        public int OutputCount
        {
            get;
            private set;
        }

        public ISignalArray InputSignalArray
        {
            get;
            private set;
        }
        public ISignalArray OutputSignalArray
        {
            get;
            private set;
        }

        public bool IsStateValid => true;

        public void Activate()
        {
            // Don't even look at the input.  Just fill the output with random values

            Random rand = StaticRandom.GetRandomForThread();

            for (int cntr = 0; cntr < OutputSignalArray.Length; cntr++)
            {
                OutputSignalArray[cntr] = _isPositiveOnly ?
                    rand.NextDouble(1) :
                    rand.NextDouble(-1, 1);
            }
        }

        public void ResetState()
        {
            // Nothing to do here
        }
    }

    #endregion
}
