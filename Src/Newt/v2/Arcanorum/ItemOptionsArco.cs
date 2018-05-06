using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.HelperClassesCore;
using Game.Newt.v2.GameItems;

namespace Game.Newt.v2.Arcanorum
{
    public class ItemOptionsArco : ItemOptions
    {
        public const double ELASTICITY_WALL = .1d;
        public const double ELASTICITY_BOTRAM = .95d;

        #region Level

        public const int MAXLEVEL = 40;

        private volatile bool _shouldRechargeOnLevel = true;
        /// <summary>
        /// If true, things like hitpoints will refill during a level up
        /// </summary>
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

        private volatile double[] _xpForNexLevel = Enumerable.Range(0, MAXLEVEL + 1).
            Select(o => Math.Pow(20 * o, 1.65)).
            ToArray();
        /// <summary>
        /// This is how much xp is required to get to the next level
        /// </summary>
        /// <remarks>
        /// The array is zero based, but the equation will just return 0 at 0.  So just ignore element zero.  Use it like this:
        /// 
        /// At level N, element[N] tells how much xp is needed to get to level N+1
        /// </remarks>
        public double[] XPForNexLevel
        {
            get
            {
                return _xpForNexLevel;
            }
            set
            {
                _xpForNexLevel = value;
            }
        }

        //NOTE: These arrays are zero based, but there is no zero level.  Just ignore array[0]

        private volatile double[] _radiusAtLevel = Enumerable.Range(0, MAXLEVEL + 1).
            Select(o => UtilityCore.GetScaledValue(.25, .8, 1, MAXLEVEL, o)).
            ToArray();
        public double[] RadiusAtLevel
        {
            get
            {
                return _radiusAtLevel;
            }
            set
            {
                _radiusAtLevel = value;
            }
        }

        private volatile double[] _massAtLevel = Enumerable.Range(0, MAXLEVEL + 1).
            Select(o => UtilityCore.GetScaledValue(2, 800, 1, MAXLEVEL, o)).
            ToArray();
        public double[] MassAtLevel
        {
            get
            {
                return _massAtLevel;
            }
            set
            {
                _massAtLevel = value;
            }
        }

        private volatile double[] _hitPointsAtLevel = Enumerable.Range(0, MAXLEVEL + 1).
            Select(o => UtilityCore.GetScaledValue(20, 2000, 1, MAXLEVEL, o)).
            ToArray();
        public double[] HitPointsAtLevel
        {
            get
            {
                return _hitPointsAtLevel;
            }
            set
            {
                _hitPointsAtLevel = value;
            }
        }

        private volatile object _xpRatio_Treasure = .33d;
        /// <summary>
        /// How much xp to gain based on the amount of damage hp dealt to a treasure box
        /// </summary>
        public double XPRatio_Treasure
        {
            get
            {
                return (double)_xpRatio_Treasure;
            }
            set
            {
                _xpRatio_Treasure = value;
            }
        }

        private volatile object _xpRatio_Bot = 1d;
        /// <summary>
        /// How much xp to gain based on the amount of damage hp dealt to another bot
        /// </summary>
        /// <remarks>
        /// This should be more complex than a single ratio.  Hurting a "dumb" bot should give less xp than a proven fighter
        /// </remarks>
        public double XPRatio_Bot
        {
            get
            {
                return (double)_xpRatio_Bot;
            }
            set
            {
                _xpRatio_Bot = value;
            }
        }

        #endregion

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

        private volatile object _inventoryWeightPercent = .5d;
        /// <summary>
        /// When an item is in a bot's inventory, this is what it weighs (don't want zero weight, because then there's no penalty
        /// for hoarding)
        /// </summary>
        public double InventoryWeightPercent
        {
            get
            {
                return (double)_inventoryWeightPercent;
            }
            set
            {
                _inventoryWeightPercent = value;
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
        #region Motion Controller - linear

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
        #region Motion Controller2

        private volatile object _motionController2_NeuronDensity = 8d;
        /// <summary>
        /// This is how many neurons to place around the perimiter (this doesn't include the 2 interior neurons)
        /// </summary>
        public double MotionController2_NeuronDensity
        {
            get
            {
                return (double)_motionController2_NeuronDensity;
            }
            set
            {
                _motionController2_NeuronDensity = value;
            }
        }

        private volatile object _motionController2_AmountToDraw = 1d; //.001d;  this is multiplied by ENERGYDRAWMULT
        public double MotionController2_AmountToDraw
        {
            get
            {
                return (double)_motionController2_AmountToDraw;
            }
            set
            {
                _motionController2_AmountToDraw = value;
            }
        }

        public readonly DamageProps MotionController2_Damage = new DamageProps(
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
