using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v2.Arcanorum.MapObjects;

namespace Game.Newt.v2.Arcanorum
{
    #region class: FitnessRule_TooFar

    /// <summary>
    /// This rewards bots that stay inside a certain distance from a center
    /// </summary>
    public class FitnessRule_TooFar : IFitnessRule
    {
        #region Declaration Section

        private readonly Point3D _center;

        /// <summary>
        /// Anything inside this distance will get the maximum score of one
        /// </summary>
        private readonly double _maxSafeDistance;
        /// <summary>
        /// Any distance farther than this gives a score of zero
        /// </summary>
        private readonly double _maxUnsafeDistance;

        #endregion

        #region Constructor

        public FitnessRule_TooFar(ArcBot bot, Point3D center, double maxSafeDistance, double maxUnsafeDistance)
        {
            this.Bot = bot;

            _center = center;
            _maxSafeDistance = maxSafeDistance;
            _maxUnsafeDistance = maxUnsafeDistance;
        }

        #endregion

        #region IFitnessRule Members

        public void Update_AnyThread(double elapsedTime)
        {
            double distance = (this.Bot.PositionWorld - _center).Length;

            double score;

            if (distance < _maxSafeDistance)
            {
                score = 1d;
            }
            else if (distance > _maxUnsafeDistance)
            {
                score = 0d;
            }
            else
            {
                score = 1 - UtilityCore.GetScaledValue(0, 1, _maxSafeDistance, _maxUnsafeDistance, distance);
            }

            _score = (double)_score + (score * elapsedTime);
        }

        public ArcBot Bot
        {
            get;
            private set;
        }

        private volatile object _score = 0d;
        public double Score
        {
            get
            {
                return (double)_score;
            }
        }

        #endregion
    }

    #endregion
    #region class: FitnessRule_TooStill

    /// <summary>
    /// This punishes bots that hold still for long periods of time
    /// </summary>
    /// <remarks>
    /// One way would be to punish if the bot is slower than some speed.  But that would select for bots that constantly stay moving (and
    /// if faster is better, it will select for fast moving bots)
    /// 
    /// So instead, this needs to punish if the bot holds still for too long.  Small amounts of non movement should be allowed
    /// </remarks>
    public class FitnessRule_TooStill : IFitnessRule
    {
        #region Declaration Section

        /// <summary>
        /// Any speed lower than this will be considered stationary
        /// </summary>
        private readonly double _stationarySpeed;

        /// <summary>
        /// This is how long the bot is allowed to stay stationary until their score drops
        /// </summary>
        private readonly double _maxAllowedStationaryTime;

        private volatile object _timeStationary = 0d;

        #endregion

        #region Constructor

        public FitnessRule_TooStill(ArcBot bot, double stationarySpeed, double maxAllowedStationaryTime)
        {
            this.Bot = bot;

            _stationarySpeed = stationarySpeed;
            _maxAllowedStationaryTime = maxAllowedStationaryTime;
        }

        #endregion

        #region IFitnessRule Members

        public void Update_AnyThread(double elapsedTime)
        {
            double speed = this.Bot.VelocityWorld.Length;

            double score;

            if (speed > _stationarySpeed)
            {
                score = 1d;
                _timeStationary = 0d;
            }
            else
            {
                _timeStationary = (double)_timeStationary + elapsedTime;

                if ((double)_timeStationary > _maxAllowedStationaryTime)
                {
                    score = 0d;
                }
                else
                {
                    score = 1d;
                }
            }

            _score = (double)_score + (score * elapsedTime);
        }

        public ArcBot Bot
        {
            get;
            private set;
        }

        private volatile object _score = 0d;
        public double Score
        {
            get
            {
                return (double)_score;
            }
        }

        #endregion
    }

    #endregion
    #region class: FitnessRule_TooTwitchy

    /// <remarks>
    /// Take the dot product of the velocity from the previous frame to the current frame.  1 is fine, -1 is a reversal.
    /// 
    /// Note that moving in tight circles to get and keep the weapon swinging is desirable.  But if this rule is running
    /// on a slow interval, it will appear that the bot is twitchy when it's actually running optimal.  So be carefull about
    /// giving this rule too much weight.
    /// 
    /// Don't just sum up the dot products directly.  Count the number of sharp turns in a period of time, and reduce
    /// score accordingly --------- maybe?
    /// </remarks>
    public class FitnessRule_TooTwitchy : IFitnessRule
    {
        #region Declaration Section

        private readonly double _minDot;

        private volatile object _velocityUnitLastTick = new Vector3D(0, 0, 0);

        #endregion

        #region Constructor

        public FitnessRule_TooTwitchy(ArcBot bot, double minDot)
        {
            this.Bot = bot;

            _minDot = minDot;
        }

        #endregion

        #region IFitnessRule Members

        public void Update_AnyThread(double elapsedTime)
        {
            Vector3D velocity = this.Bot.VelocityWorld.ToUnit();

            double dot = Vector3D.DotProduct((Vector3D)_velocityUnitLastTick, velocity);

            _velocityUnitLastTick = velocity;

            double score;

            if (Math1D.IsInvalid(dot))
            {
                score = .5d;        // not sure what to do here
            }
            else if (dot < _minDot)
            {
                //score = UtilityHelper.GetScaledValue(0, 1, -1, _minDot, dot);
                score = 0d;     // this class is pretty high scoring as it is, so I don't really want a gradient
            }
            else
            {
                score = 1d;     // I don't want a gradient above this min.  Circular motion will have a dot of zero, and shouldn't be favored or punished over straight line motion
            }

            _score = (double)_score + (score * elapsedTime);
        }

        public ArcBot Bot
        {
            get;
            private set;
        }

        private volatile object _score = 0d;
        public double Score
        {
            get
            {
                return (double)_score;
            }
        }

        #endregion
    }

    #endregion

    #region class: FitnessTracker

    /// <summary>
    /// This class is just a convenience to tie multiple rules (and a weight for each) to a bot
    /// </summary>
    public class FitnessTracker : IFitnessRule
    {
        #region Declaration Section

        private readonly double _ruleCount;     // storing this as a double to speed up the score get

        #endregion

        #region Constructor

        public FitnessTracker(ArcBot bot, Tuple<IFitnessRule, double>[] rules)
        {
            if (bot != null)
            {
                if (rules.Any(o => o.Item1.Bot.Token != bot.Token))
                {
                    throw new ArgumentException("All rules must be for the same bot");
                }
            }

            this.Bot = bot;

            _rules = rules;
            _ruleCount = _rules.Length;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Item1=A rule to apply
        /// Item2=A multiplier (its weight relative to the other rules)
        /// </summary>
        private readonly Tuple<IFitnessRule, double>[] _rules;
        public Tuple<IFitnessRule, double>[] Rules
        {
            get
            {
                return _rules;
            }
        }

        #endregion

        #region IFitnessRule Members

        public void Update_AnyThread(double elapsedTime)
        {
            for (int cntr = 0; cntr < _rules.Length; cntr++)
            {
                _rules[cntr].Item1.Update_AnyThread(elapsedTime);
            }
        }

        public ArcBot Bot
        {
            get;
            private set;
        }

        public double Score
        {
            get
            {
                //NOTE: Dividing by the number of rules so that can be compared directly
                return _rules.Sum(o => o.Item1.Score * o.Item2) / _ruleCount;
            }
        }

        #endregion
    }

    #endregion

    #region interface: IFitnessRule

    public interface IFitnessRule
    {
        void Update_AnyThread(double elapsedTime);

        /// <summary>
        /// This is the bot that's being tracked
        /// </summary>
        ArcBot Bot { get; }

        /// <remarks>
        /// Score needs to be normalized to be useful.  It accumulates with time, so an older bot will have higher score, so rate of
        /// score accumulation may be more valuable than total.
        /// 
        /// Score should not accumulate faster than 1 per unit of time.
        /// </remarks>
        double Score { get; }
    }

    #endregion
}
