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

        #region Vision Sensor

        private volatile object _visionSensor_NeuronDensity = 20d;
        /// <summary>
        /// This is how many neurons to place inside of a vision sensor
        /// </summary>
        public double VisionSensor_NeuronDensity
        {
            get
            {
                return (double)_visionSensor_NeuronDensity;
            }
            set
            {
                _visionSensor_NeuronDensity = value;
            }
        }

        private volatile object _visionSensor_AmountToDraw = 1d; //.001d;  this is multiplied by ENERGYDRAWMULT
        public double VisionSensor_AmountToDraw
        {
            get
            {
                return (double)_visionSensor_AmountToDraw;
            }
            set
            {
                _visionSensor_AmountToDraw = value;
            }
        }

        public readonly DamageProps VisionSensor_Damage = new DamageProps(
            HITPOINTMIN,
            HITPOINTSLOPE,
            DAMAGE_VELOCITYTHRESHOLD,
            DAMAGE_ENERGYTHESHOLD,
            DAMAGE_ENERGYTOHITPOINTMULT,
            DAMAGE_RANDOMMAX);

        #endregion
        #region Homing Sensor

        private volatile object _homingSensor_NeuronDensity = 20d;
        /// <summary>
        /// This is how many neurons to place inside of a homing sensor
        /// </summary>
        public double HomingSensor_NeuronDensity
        {
            get
            {
                return (double)_homingSensor_NeuronDensity;
            }
            set
            {
                _homingSensor_NeuronDensity = value;
            }
        }

        private volatile object _homingSensor_AmountToDraw = 1d; //.001d;  this is multiplied by ENERGYDRAWMULT
        public double HomingSensor_AmountToDraw
        {
            get
            {
                return (double)_homingSensor_AmountToDraw;
            }
            set
            {
                _homingSensor_AmountToDraw = value;
            }
        }

        public readonly DamageProps HomingSensor_Damage = new DamageProps(
            HITPOINTMIN,
            HITPOINTSLOPE,
            DAMAGE_VELOCITYTHRESHOLD,
            DAMAGE_ENERGYTHESHOLD,
            DAMAGE_ENERGYTOHITPOINTMULT,
            DAMAGE_RANDOMMAX);

        #endregion
        #region Motion Controller

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

        public readonly DamageProps MotionController_Damage = new DamageProps(
            HITPOINTMIN,
            HITPOINTSLOPE,
            DAMAGE_VELOCITYTHRESHOLD,
            DAMAGE_ENERGYTHESHOLD,
            DAMAGE_ENERGYTOHITPOINTMULT,
            DAMAGE_RANDOMMAX);

        #endregion
        #region Nest

        //TODO: Instead of absolutes, these should be multipliers (to be multiplied with dna props)
        private volatile object _nest_Energy_Max = 100d;
        public double Nest_Energy_Max
        {
            get
            {
                return (double)_nest_Energy_Max;
            }
            set
            {
                _nest_Energy_Max = value;
            }
        }

        private volatile object _nest_Energy_Add = 15d;
        /// <summary>
        /// How much energy to add per second
        /// </summary>
        public double Nest_Energy_Add
        {
            get
            {
                return (double)_nest_Energy_Add;
            }
            set
            {
                _nest_Energy_Add = value;
            }
        }

        private volatile object _nest_Energy_Egg = 60d;
        /// <summary>
        /// This is the cost of an egg
        /// </summary>
        public double Nest_Energy_Egg
        {
            get
            {
                return (double)_nest_Energy_Egg;
            }
            set
            {
                _nest_Energy_Egg = value;
            }
        }

        #endregion

        //-------------------------- Mutation
        private volatile object _neuron_PercentToMutate = .02d;
        public double Neuron_PercentToMutate
        {
            get
            {
                return (double)_neuron_PercentToMutate;
            }
            set
            {
                _neuron_PercentToMutate = value;
            }
        }

        private volatile object _neuron_MovementAmount = .04d;
        public double Neuron_MovementAmount
        {
            get
            {
                return (double)_neuron_MovementAmount;
            }
            set
            {
                _neuron_MovementAmount = value;
            }
        }

        private volatile object _link_PercentToMutate = .02d;
        public double Link_PercentToMutate
        {
            get
            {
                return (double)_link_PercentToMutate;
            }
            set
            {
                _link_PercentToMutate = value;
            }
        }

        private volatile object _linkContainer_MovementAmount = .1d;
        public double LinkContainer_MovementAmount
        {
            get
            {
                return (double)_linkContainer_MovementAmount;
            }
            set
            {
                _linkContainer_MovementAmount = value;
            }
        }

        private volatile object _linkContainer_RotateAmount = .08d;
        public double LinkContainer_RotateAmount
        {
            get
            {
                return (double)_linkContainer_RotateAmount;
            }
            set
            {
                _linkContainer_RotateAmount = value;
            }
        }

        private volatile object _link_WeightAmount = .1d;
        public double Link_WeightAmount
        {
            get
            {
                return (double)_link_WeightAmount;
            }
            set
            {
                _link_WeightAmount = value;
            }
        }

        private volatile object _link_MovementAmount = .2d;
        public double Link_MovementAmount
        {
            get
            {
                return (double)_link_MovementAmount;
            }
            set
            {
                _link_MovementAmount = value;
            }
        }
    }
}
