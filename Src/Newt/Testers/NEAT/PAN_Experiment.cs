using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.HelperClassesAI;
using Game.HelperClassesCore;

namespace Game.Newt.Testers.NEAT
{
    public class PAN_Experiment : ExperimentNEATBase
    {
        #region Constructor

        public PAN_Experiment(int inputCount, int[] trueValues)
        {
            TrueVectors = GetTrueVectors(inputCount, trueValues);
            TrueValues = trueValues;
        }

        #endregion

        #region Public Properties

        public int[] TrueValues
        {
            get;
            private set;
        }

        public bool[][] TrueVectors
        {
            get;
            private set;
        }

        #endregion

        #region Public Methods

        public static bool[][] GetTrueVectors(int inputCount, int[] trueValues)
        {
            return trueValues.
                Select(o => UtilityCore.ConvertToBase2(o, inputCount)).
                ToArray();
        }

        #endregion
    }
}
