using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Newt.v2.GameItems;

namespace Game.Newt.v2.Arcanorum
{
    public class ItemOptionsArco : ItemOptions
    {
        public const double ELASTICITY_WALL = .1d;
        public const double ELASTICITY_BOTRAM = .95d;

        private volatile bool _shouldRechargeOnLevel = false;
        public bool ShouldRechargeOnLevel
        {
            get
            {
                return _shouldRechargeOnLevel;
            }
            set
            {
                _shouldRechargeOnLevel = value;
            }
        }

        private volatile object _gravity = 10d;
        public double Gravity
        {
            get
            {
                return (double)_gravity;
            }
            set
            {
                _gravity = value;
            }
        }

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

        // Homing Sensor
        private volatile object _homingSensorNeuronDensity = 20d;
        /// <summary>
        /// This is how many neurons to place inside of a homing sensor
        /// </summary>
        public double HomingSensorNeuronDensity
        {
            get
            {
                return (double)_homingSensorNeuronDensity;
            }
            set
            {
                _homingSensorNeuronDensity = value;
            }
        }

        private volatile object _homingSensorAmountToDraw = 1d; //.001d;  this is multiplied by ENERGYDRAWMULT
        public double HomingSensorAmountToDraw
        {
            get
            {
                return (double)_homingSensorAmountToDraw;
            }
            set
            {
                _homingSensorAmountToDraw = value;
            }
        }

        // Plate Writer
        private volatile object _motionController_Linear_NeuronDensity = 20d;
        /// <summary>
        /// This is how many neurons to place inside of a plate writer
        /// </summary>
        public double MotionController_Linear_NeuronDensity
        {
            get
            {
                return (double)_motionController_Linear_NeuronDensity;
            }
            set
            {
                _motionController_Linear_NeuronDensity = value;
            }
        }

        private volatile object _motionController_Linear_AmountToDraw = 1d; //.001d;  this is multiplied by ENERGYDRAWMULT
        public double MotionController_Linear_AmountToDraw
        {
            get
            {
                return (double)_motionController_Linear_AmountToDraw;
            }
            set
            {
                _motionController_Linear_AmountToDraw = value;
            }
        }

        // Nest
        //TODO: Instead of absolutes, these should be multipliers (to be multiplied with dna props)
        private volatile object _nestEnergy_Max = 100d;
        public double NestEnergy_Max
        {
            get
            {
                return (double)_nestEnergy_Max;
            }
            set
            {
                _nestEnergy_Max = value;
            }
        }

        private volatile object _nestEnergy_Add = 15d;
        /// <summary>
        /// How much energy to add per second
        /// </summary>
        public double NestEnergy_Add
        {
            get
            {
                return (double)_nestEnergy_Add;
            }
            set
            {
                _nestEnergy_Add = value;
            }
        }

        private volatile object _nestEnergy_Egg = 60d;
        /// <summary>
        /// This is the cost of an egg
        /// </summary>
        public double NestEnergy_Egg
        {
            get
            {
                return (double)_nestEnergy_Egg;
            }
            set
            {
                _nestEnergy_Egg = value;
            }
        }

        //-------------------------- Mutation
        private volatile object _neuronPercentToMutate = .02d;
        public double NeuronPercentToMutate
        {
            get
            {
                return (double)_neuronPercentToMutate;
            }
            set
            {
                _neuronPercentToMutate = value;
            }
        }

        private volatile object _neuronMovementAmount = .04d;
        public double NeuronMovementAmount
        {
            get
            {
                return (double)_neuronMovementAmount;
            }
            set
            {
                _neuronMovementAmount = value;
            }
        }

        private volatile object _linkPercentToMutate = .02d;
        public double LinkPercentToMutate
        {
            get
            {
                return (double)_linkPercentToMutate;
            }
            set
            {
                _linkPercentToMutate = value;
            }
        }

        private volatile object _linkContainerMovementAmount = .1d;
        public double LinkContainerMovementAmount
        {
            get
            {
                return (double)_linkContainerMovementAmount;
            }
            set
            {
                _linkContainerMovementAmount = value;
            }
        }

        private volatile object _linkContainerRotateAmount = .08d;
        public double LinkContainerRotateAmount
        {
            get
            {
                return (double)_linkContainerRotateAmount;
            }
            set
            {
                _linkContainerRotateAmount = value;
            }
        }

        private volatile object _linkWeightAmount = .1d;
        public double LinkWeightAmount
        {
            get
            {
                return (double)_linkWeightAmount;
            }
            set
            {
                _linkWeightAmount = value;
            }
        }

        private volatile object _linkMovementAmount = .2d;
        public double LinkMovementAmount
        {
            get
            {
                return (double)_linkMovementAmount;
            }
            set
            {
                _linkMovementAmount = value;
            }
        }
    }
}
