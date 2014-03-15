using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Newt.AsteroidMiner2;

namespace Game.Newt.Testers.Arcanorum
{
    public class ItemOptionsArco : ItemOptions
    {
        // Vision Sensor
        private volatile object _visionSensorNeuronDensity = 20d;
        /// <summary>
        /// This is how many neurons to place inside of a vision sensor
        /// </summary>
        public double VisionSensorNeuronDensity
        {
            get
            {
                return (double)_visionSensorNeuronDensity;
            }
            set
            {
                _visionSensorNeuronDensity = value;
            }
        }

        private volatile object _visionSensorAmountToDraw = 1d; //.001d;  this is multiplied by ENERGYDRAWMULT
        public double VisionSensorAmountToDraw
        {
            get
            {
                return (double)_visionSensorAmountToDraw;
            }
            set
            {
                _visionSensorAmountToDraw = value;
            }
        }

    }
}
