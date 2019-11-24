using SharpNeat.Core;
using System.Collections.Generic;
using System.Windows.Media.Media3D;

namespace Game.Newt.v2.Arcanorum.ArenaTester
{
    #region class: BotTrackingEntry

    /// <summary>
    /// This holds the state of the bot at a point in time
    /// </summary>
    public class BotTrackingEntry
    {
        public BotTrackingEntry(double elapsedTime, Point3D position, Vector3D velocity)
        {
            ElapsedTime = elapsedTime;
            Position = position;
            Velocity = velocity;
        }

        public double ElapsedTime { get; }

        public Point3D Position { get; }
        public Vector3D Velocity { get; }
    }

    #endregion
    #region class: BotTrackingRun

    public class BotTrackingRun
    {
        public BotTrackingRun(Point3D roomCenter, Point3D roomMin, Point3D roomMax, FitnessInfo score, BotTrackingEntry[] log)
        {
            RoomCenter = roomCenter;
            RoomMin = roomMin;
            RoomMax = roomMax;
            Score = score;
            Log = log;
        }

        public Point3D RoomCenter { get; }
        public Point3D RoomMin { get; }
        public Point3D RoomMax { get; }

        public FitnessInfo Score { get; }

        public BotTrackingEntry[] Log { get; }
    }

    #endregion

    #region class: BotTrackingStorage

    /// <summary>
    /// This is basically just a List or BotTrackingRun, but is made threadsafe
    /// </summary>
    public class BotTrackingStorage
    {
        #region Declaration Section

        private readonly object _lock = new object();

        private int _currentGeneration = 0;

        private SortedList<int, List<BotTrackingRun>> _log = new SortedList<int, List<BotTrackingRun>>();

        #endregion

        #region Public Methods

        public void IncrementGeneration()
        {
            lock (_lock)
            {
                _currentGeneration++;
            }
        }

        public void AddEntry(BotTrackingRun entry)
        {
            lock (_lock)
            {
                if (!_log.TryGetValue(_currentGeneration, out List<BotTrackingRun> generation))
                {
                    generation = new List<BotTrackingRun>();
                    _log.Add(_currentGeneration, generation);
                }

                generation.Add(entry);
            }
        }

        public BotTrackingRun[] GetLatestGenerationSnapshots()
        {
            lock (_lock)
            {
                if (_log.Keys.Count == 0)
                {
                    return new BotTrackingRun[0];
                }

                return _log[_log.Keys[_log.Keys.Count - 1]].ToArray();
            }
        }

        #endregion
    }

    #endregion
}
