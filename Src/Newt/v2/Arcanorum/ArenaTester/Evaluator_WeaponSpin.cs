using Game.HelperClassesAI;
using Game.HelperClassesCore;
using Game.Newt.v2.Arcanorum.MapObjects;
using Game.Newt.v2.GameItems;
using SharpNeat.Core;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes;
using System;
using System.Collections.Generic;
using System.Windows.Media.Media3D;

namespace Game.Newt.v2.Arcanorum.ArenaTester
{
    public class Evaluator_WeaponSpin : IPhenomeTickEvaluator<IBlackBox, NeatGenome>
    {
        #region Declaration Section

        public const double MULT_MAXDIST2 = .4;
        public const double MULT_MAXDIST = .3;

        private readonly WorldAccessor _worldAccessor;
        private readonly TrainingRoom _room;
        private readonly BotTrackingStorage _log;

        private readonly ExperimentInitArgs_Activation _activation;
        private readonly HyperNEAT_Args _hyperneatArgs;

        private readonly double _maxEvalTime;

        private readonly double _maxDistance;// = 30;
        private readonly double _maxDistance2;// = 30 * 1.25;

        private readonly double _weaponSpeed_Min;
        private readonly double _weaponSpeed_Max;
        private readonly double _weaponSpeed_Max2;

        private readonly double _roomRadius;

        private bool _isEvaluating = false;

        private readonly List<BotTrackingEntry> _snapshots = new List<BotTrackingEntry>();

        private double _error = 0;
        private double _runningTime = 0;

        #endregion

        #region Constructor

        public Evaluator_WeaponSpin(WorldAccessor worldAccessor, TrainingRoom room, BotTrackingStorage log, ExperimentInitArgs_Activation activation, double weaponSpeed_Min, double weaponSpeed_Max, HyperNEAT_Args hyperneatArgs = null, double maxEvalTime = 15)
        {
            _worldAccessor = worldAccessor;
            _room = room;
            _log = log;
            _activation = activation;
            _hyperneatArgs = hyperneatArgs;
            _maxEvalTime = maxEvalTime;

            // Set distances based on room size (or take as params?, or just a percent param?)
            double roomWidth = Math1D.Min
            (
                room.AABB.Item2.X - room.AABB.Item1.X,
                room.AABB.Item2.Y - room.AABB.Item1.Y,
                room.AABB.Item2.Z - room.AABB.Item1.Z
            );

            // Want radius, not diameter
            _roomRadius = roomWidth / 2;

            _maxDistance2 = _roomRadius * MULT_MAXDIST2;
            _maxDistance = _roomRadius * MULT_MAXDIST;

            // Weapon Speed
            _weaponSpeed_Min = weaponSpeed_Min;
            _weaponSpeed_Max = weaponSpeed_Max;
            _weaponSpeed_Max2 = _weaponSpeed_Max + ((_weaponSpeed_Max - _weaponSpeed_Min) / 2);
        }

        #endregion

        #region IPhenomeTickEvaluator<IBlackBox, NeatGenome>

        private ulong _evaluationCount = 0;
        public ulong EvaluationCount => throw new NotImplementedException();

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

            if (_room.Bot is ArcBot2 arc)
            {
                if (arc.Weapon == null)
                {
                    throw new ApplicationException("_room.Bot needs to have a weapon attached");
                }
            }
            else
            {
                throw new ApplicationException($"_room.Bot needs to be ArcBot2: {_room.Bot.GetType()}");
            }

            #endregion

            _room.BrainPart.SetPhenome(phenome, genome, _activation, _hyperneatArgs);

            arc.PhysicsBody.Position = _room.Center;

            arc.PhysicsBody.Velocity = new Vector3D(0, 0, 0);
            arc.PhysicsBody.Rotation = Quaternion.Identity;
            arc.PhysicsBody.AngularVelocity = new Vector3D(0, 0, 0);

            //NOTE: Leaving the position alone.  This should help with making the brain more robust
            arc.Weapon.PhysicsBody.Velocity = new Vector3D(0, 0, 0);

            _snapshots.Clear();

            _error = 0;
            _runningTime = 0;

            _evaluationCount++;

            _isEvaluating = true;
        }

        public FitnessInfo? EvaluateTick(double elapedTime)
        {
            if (!_isEvaluating)
            {
                // This is called after the score has been given, but before a new phenome is assigned
                throw new InvalidOperationException("EvaluateTick was called at an invalid time.  Either StartNewEvaluation was never called, or the current phenome has returned a final score and this class is waiting for a new call to StartNewEvaluation");
            }

            _room.Bot.RefillContainers();

            //TODO: May want to give a 1 second warmup before evaluating

            var distance = GetDistanceError(_room.Bot, _room.Center, _maxDistance, _maxDistance2);

            var weaponSpeed = GetWeaponSpeedError((ArcBot2)_room.Bot, _weaponSpeed_Min, _weaponSpeed_Max, _weaponSpeed_Max2);

            // Combine errors
            //TODO: Have multipliers so that distance could be weighted differently than speed - be sure the final value is normalized to 0 to 1
            double error = Math1D.Avg(distance.error, weaponSpeed.error);

            double actualElapsed = EvaluatorUtil.GetActualElapsedTime(ref _runningTime, elapedTime, _maxEvalTime);

            // Add the error to the total (cap it if time is exceeded)
            error *= actualElapsed;
            _error += error;

            _snapshots.Add(new BotTrackingEntry_WeaponSpin(_runningTime, _room.Bot, weaponSpeed.speed));

            if (distance.distance > _maxDistance2)
            {
                // They went outside the allowed area.  Quit early and give maximum error for the remainder of the time
                _error += (_maxEvalTime - _runningTime);
                _runningTime = _maxEvalTime;
                return EvaluatorUtil.FinishEvaluation(ref _isEvaluating, _runningTime, _error, _maxEvalTime, _log, _room, _snapshots.ToArray());
            }
            else if (_runningTime >= _maxEvalTime)
            {
                // If the counter is a certain amount, return the final score
                // It's been tested long enough.  Return the score (score needs to grow, so an error of zero will give max score)
                return EvaluatorUtil.FinishEvaluation(ref _isEvaluating, _runningTime, _error, _maxEvalTime, _log, _room, _snapshots.ToArray());
            }
            else
            {
                // Still evaluating
                return null;
            }
        }

        #endregion

        #region Private Methods

        private static (double distance, double error) GetDistanceError(Bot bot, Point3D center, double maxDistance, double maxDistance2)
        {
            // Get the bot's distance from home
            double distance = (bot.PositionWorld - center).Length;

            // Run that distance through a scoring function
            double error = 0d;

            if (distance > maxDistance2)
            {
                error = 1;
            }
            else if (distance > maxDistance)
            {
                error = UtilityCore.GetScaledValue_Capped(0, 1, maxDistance, maxDistance2, distance);
            }

            return (distance, error);
        }

        private static (double speed, double error) GetWeaponSpeedError(ArcBot2 bot, double minSpeed, double maxSpeed, double maxSpeed2)
        {
            Vector3D relativeSpeed = bot.Weapon.VelocityWorld - bot.VelocityWorld;

            // This is kind of rough, because it will be the speed of the center point.  May also need angular velocity
            double speed = relativeSpeed.Length;

            double error = 0;

            if (speed < minSpeed)
            {
                error = UtilityCore.GetScaledValue_Capped(0, 1, minSpeed, 0, speed);
            }
            else if (speed > maxSpeed)
            {
                if (speed > maxSpeed2)
                {
                    error = 1;
                }
                else
                {
                    error = UtilityCore.GetScaledValue_Capped(0, 1, maxSpeed, maxSpeed2, speed);
                }
            }

            return (speed, error);
        }

        #endregion
    }

    #region class: BotTrackingEntry_WeaponSpin

    public class BotTrackingEntry_WeaponSpin : BotTrackingEntry
    {
        public BotTrackingEntry_WeaponSpin(double elapsedTime, Bot bot, double weaponSpeed)
            : base(elapsedTime, bot.PositionWorld, bot.VelocityWorld)
        {
            WeaponSpeed = weaponSpeed;
        }

        public double WeaponSpeed { get; }
    }

    #endregion
}
