using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Encog.Neural.Networks;

namespace Game.HelperClassesAI
{
    public static class Extenders
    {
        public static double[] Compute(this BasicNetwork network, double[] input)
        {
            double[] retVal = new double[network.OutputCount];

            network.Compute(input, retVal);

            return retVal;
        }
    }
}
