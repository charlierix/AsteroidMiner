using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Game.HelperClassesAI;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.ShipParts;
using SharpNeat.Core;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes;

namespace Game.Newt.v2.Arcanorum.ArenaTester
{
    // This one should be as simple as possible.  Maybe stay within a certain radius of home, but don't stop moving

    public class Evaluator1_FAIL : IPhenomeTickEvaluator<IBlackBox, NeatGenome>
    {
        #region Declaration Section

        private readonly Bot _bot;
        private readonly BrainNEAT _brainPart;
        private readonly SensorHoming[] _homingParts;

        private readonly Point3D _homePoint;
        private readonly double _minDistance = 10;
        private readonly double _maxDistance = 30;
        private readonly double _maxDistance2 = 30 * 1.25;

        private readonly double _initialVelocity = 5;
        private readonly double _maxEvalTime = 15;

        private readonly ExperimentInitArgs_Activation _activation;
        private readonly HyperNEAT_Args _hyperneatArgs;

        private bool _isEvaluating = false;

        private double _error = 0;
        private double _runningTime = 0;

        #endregion

        #region Constructor

        //TODO: In more complete evaluators, take in an arena manager that lets you check out a room.  That will return the bot/brain that was
        //created for that room (request a room each time StartNewEvaluation is called, or if no rooms available at that moment, each tick until
        //a room is free - then start evaluating from that moment)
        public Evaluator1_FAIL(Bot bot, BrainNEAT brainPart, SensorHoming[] homingParts, Point3D homePoint, ExperimentInitArgs_Activation activation, HyperNEAT_Args hyperneatArgs = null)
        {
            _bot = bot;
            _brainPart = brainPart;
            _homingParts = homingParts;

            _homePoint = homePoint;

            _activation = activation;
            _hyperneatArgs = hyperneatArgs;

            foreach (SensorHoming homingPart in homingParts)
            {
                homingPart.HomePoint = homePoint;
                homingPart.HomeRadius = _maxDistance * 1.25;
            }
        }

        #endregion

        #region IPhenomeEvaluator<IBlackBox>

        public ulong EvaluationCount { get; private set; }

        public bool StopConditionSatisfied => false;        // real world evaluators should have logic to know when to stop (once enough samples have had a good enough score)

        public void Reset()
        {
        }

        public void StartNewEvaluation(IBlackBox phenome, NeatGenome genome)
        {
            _brainPart.SetPhenome(phenome, genome, _activation, _hyperneatArgs);

            _bot.PhysicsBody.Position = _homePoint;

            // Give it an initial random kick to force it to be a bit more robust
            _bot.PhysicsBody.Velocity = Math3D.GetRandomVector_Spherical(_initialVelocity);

            _error = 0;
            _runningTime = 0;

            _isEvaluating = true;
        }

        public FitnessInfo? EvaluateTick(double elapedTime)
        {
            if (!_isEvaluating)
            {
                // This is called after the score has been given, but before a new phenome is assigned
                throw new InvalidOperationException("EvaluateTick was called at an invalid time.  Either StartNewEvaluation was never called, or the current phenome has returned a final score and this class is waiting for a new call to StartNewEvaluation");
            }

            // Get the bot's distance from home
            double distance = (_bot.PositionWorld - _homePoint).Length;

            // Run that distance through a scoring function
            double thisError = 0d;

            if (distance < _minDistance)
            {
                thisError = UtilityCore.GetScaledValue_Capped(1, 0, 0, _minDistance, distance);
            }
            else if (distance > _maxDistance2)
            {
                thisError = 1;
            }
            else if (distance > _maxDistance)
            {
                thisError = UtilityCore.GetScaledValue_Capped(0, 1, _maxDistance, _maxDistance2, distance);
            }

            // Add the error to the total (cap it if time is exceeded)
            double actualElapsed = elapedTime;
            if(_runningTime + elapedTime > _maxEvalTime)
            {
                actualElapsed = _maxEvalTime - _runningTime;
            }

            thisError *= actualElapsed;

            _error += thisError;

            _runningTime += elapedTime;

            // If the counter is a certain amount, return the final score
            if(_runningTime >= _maxEvalTime)
            {
                // It's been tested long enough.  Return the score (score needs to grow, so an error of zero will give max score)
                double fitness = _runningTime - _error;
                return new FitnessInfo(fitness, fitness);
            }
            else
            {
                // Still evaluating
                return null;
            }
        }

        #endregion
    }
}
