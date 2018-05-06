using Game.HelperClassesAI;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v2.GameItems;
using SharpNeat.Core;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Game.Newt.v2.Arcanorum.ArenaTester
{
    public class Evaluator3 : IPhenomeTickEvaluator<IBlackBox, NeatGenome>
    {
        #region Declaration Section

        private readonly WorldAccessor _worldAccessor;
        private readonly TrainingRoom _room;
        private readonly BotTrackingStorage _log;

        private readonly ExperimentInitArgs_Activation _activation;
        private readonly HyperNEAT_Args _hyperneatArgs;

        private readonly double _minDistance;// = 10;
        private readonly double _maxDistance;// = 30;
        private readonly double _maxDistance2;// = 30 * 1.25;

        private readonly double _maxEvalTime;
        private readonly double? _initialRandomVelocity;

        private bool _isEvaluating = false;

        private readonly List<BotTrackingEntry> _snapshots = new List<BotTrackingEntry>();

        private double _error = 0;
        private double _runningTime = 0;

        #endregion

        #region Constructor

        public Evaluator3(WorldAccessor worldAccessor, TrainingRoom room, BotTrackingStorage log, ExperimentInitArgs_Activation activation, HyperNEAT_Args hyperneatArgs = null, double maxEvalTime = 15, double? initialRandomVelocity = null)
        {
            _worldAccessor = worldAccessor;
            _room = room;
            _log = log;
            _activation = activation;
            _hyperneatArgs = hyperneatArgs;
            _maxEvalTime = maxEvalTime;
            _initialRandomVelocity = initialRandomVelocity;

            // Set distances based on room size (or take as params?, or just a percent param?)
            double roomWidth = Math1D.Min(
                room.AABB.Item2.X - room.AABB.Item1.X,
                room.AABB.Item2.Y - room.AABB.Item1.Y,
                room.AABB.Item2.Z - room.AABB.Item1.Z);

            // Want radius, not diameter
            roomWidth = roomWidth / 2;

            _maxDistance2 = roomWidth * .9;
            _maxDistance = _maxDistance2 / 1.25;
            _minDistance = _maxDistance * .25;
        }

        #endregion

        #region IPhenomeTickEvaluator<IBlackBox, NeatGenome>

        private ulong _evaluationCount = 0;
        public ulong EvaluationCount => _evaluationCount;

        public bool StopConditionSatisfied => false;

        public void Reset()
        {
        }

        public void StartNewEvaluation(IBlackBox phenome, NeatGenome genome)
        {
            _worldAccessor.EnsureInitialized();

            #region validate

            if (_room.Map == null || _room.Bot == null || _room.BrainPart == null)
            {
                throw new ApplicationException(string.Format("_worldAccessor.EnsureInitialized didn't set everything up: _room.Map={0}, _room.Bot={1}",
                    _room.Map == null ? "null" : "notnull",
                    _room.Bot == null ? "null" : "notnull",
                    _room.BrainPart == null ? "null" : "notnull"));
            }

            #endregion

            _room.BrainPart.SetPhenome(phenome, genome, _activation, _hyperneatArgs);

            _room.Bot.PhysicsBody.Position = _room.Center;

            // Give it an initial random kick to force it to be a bit more robust (this could backfire though, because the evaluator
            // may favor good initial conditions instead of a better brain).  If random initial settings are used, there should be
            // several tests before a final score is given
            if (_initialRandomVelocity != null)
            {
                _room.Bot.PhysicsBody.Velocity = Math3D.GetRandomVector_Spherical(_initialRandomVelocity.Value);
            }

            _room.Bot.PhysicsBody.Rotation = Quaternion.Identity;
            _room.Bot.PhysicsBody.AngularVelocity = new Vector3D(0, 0, 0);

            _snapshots.Clear();

            _error = 0;
            _runningTime = 0;

            _evaluationCount++;

            _isEvaluating = true;
        }

        public FitnessInfo? EvaluateTick(double elapedTime)
        {
            //System.Diagnostics.Debug.WriteLine(Guid.NewGuid().ToString());

            if (!_isEvaluating)
            {
                // This is called after the score has been given, but before a new phenome is assigned
                throw new InvalidOperationException("EvaluateTick was called at an invalid time.  Either StartNewEvaluation was never called, or the current phenome has returned a final score and this class is waiting for a new call to StartNewEvaluation");
            }

            if (_room.Bot.Fuel != null)
            {
                _room.Bot.Fuel.QuantityCurrent = _room.Bot.Fuel.QuantityMax;
            }

            if (_room.Bot.Energy != null)
            {
                _room.Bot.Energy.QuantityCurrent = _room.Bot.Energy.QuantityMax;
            }

            if (_room.Bot.Plasma != null)
            {
                _room.Bot.Plasma.QuantityCurrent = _room.Bot.Plasma.QuantityMax;
            }

            //TODO: May want to give a 1 second warmup before evaluating

            var distance = GetDistanceError(_room.Bot, _room.Center, _minDistance, _maxDistance, _maxDistance2);

            //TODO: Speed

            //TODO: Combine distance and speed errors (normalize between 0 and 1)
            double error = distance.error;

            // Add the error to the total (cap it if time is exceeded)
            double actualElapsed = elapedTime;
            if (_runningTime + elapedTime > _maxEvalTime)
            {
                actualElapsed = _maxEvalTime - _runningTime;
            }

            error *= actualElapsed;

            _error += error;

            _runningTime += elapedTime;

            AddTrackingEntry();

            if (distance.distance > _maxDistance2)
            {
                // They went outside the allowed area.  Quit early and give maximum error for the remainder of the time
                _error += (_maxEvalTime - _runningTime);
                _runningTime = _maxEvalTime;
                return FinishEvaluation();
            }
            else if (_runningTime >= _maxEvalTime)
            {
                // If the counter is a certain amount, return the final score
                // It's been tested long enough.  Return the score (score needs to grow, so an error of zero will give max score)
                return FinishEvaluation();
            }
            else
            {
                // Still evaluating
                return null;
            }
        }

        #endregion

        #region Private Methods

        private FitnessInfo FinishEvaluation()
        {
            _isEvaluating = false;

            double fitness = _runningTime - _error;

            if (fitness < 0)
            {
                fitness = 0;
            }

            fitness *= 1000 / _maxEvalTime;     // scale the score to be 1000

            FitnessInfo retVal = new FitnessInfo(fitness, fitness);

            _log.AddEntry(new BotTrackingRun(_room.Center, _room.AABB.Item1, _room.AABB.Item2, retVal, _snapshots.ToArray()));

            return retVal;
        }

        private void AddTrackingEntry()
        {
            //_snapshots.Add(new BotTrackingEntry(_runningTime, _room.Bot.PositionWorld, _room.Bot.VelocityWorld));
            _snapshots.Add(new BotTrackingEntry_Eval3(_runningTime, _room.Bot));
        }

        private static (double distance, double error) GetDistanceError(Bot bot, Point3D center, double minDistance, double maxDistance, double maxDistance2)
        {
            // Get the bot's distance from home
            double distance = (bot.PositionWorld - center).Length;

            // Run that distance through a scoring function
            double error = 0d;

            if (distance < minDistance)
            {
                error = UtilityCore.GetScaledValue_Capped(1, 0, 0, minDistance, distance);
            }
            else if (distance > maxDistance2)
            {
                error = 1;
            }
            else if (distance > maxDistance)
            {
                error = UtilityCore.GetScaledValue_Capped(0, 1, maxDistance, maxDistance2, distance);
            }

            return (distance, error);
        }

        #endregion
    }

    public class BotTrackingEntry_Eval3 : BotTrackingEntry
    {
        public BotTrackingEntry_Eval3(double elapsedTime, Bot bot) :
            base(elapsedTime, bot.PositionWorld, bot.VelocityWorld)
        {
            var parts = bot.Parts.
                Where(o => o is INeuronContainer).
                Select(o => new
                {
                    Part = o,
                    Neural = (INeuronContainer)o,
                }).
                OrderBy(o => o.Part.PartType).
                ToArray();

            OffParts = parts
                .Where(o => !o.Neural.IsOn).
                Select(o => o.Part.PartType).
                ToJoin(", ");

            OnParts = parts
                .Where(o => o.Neural.IsOn).
                Select(o => o.Part.PartType).
                ToJoin(", ");
        }

        public string OffParts { get; private set; }
        public string OnParts { get; private set; }
    }
}
