using SharpNeat.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            _elapsedTime = elapsedTime;
            _position = position;
            _velocity = velocity;
        }

        private readonly double _elapsedTime;
        public double ElapsedTime => _elapsedTime;

        private readonly Point3D _position;
        public Point3D Position => _position;

        private readonly Vector3D _velocity;
        public Vector3D Velocity => _velocity;
    }

    #endregion
    #region class: BotTrackingRun

    public class BotTrackingRun
    {
        public BotTrackingRun(Point3D roomCenter, Point3D roomMin, Point3D roomMax, FitnessInfo score, BotTrackingEntry[] log)
        {
            _roomCenter = roomCenter;
            _roomMin = roomMin;
            _roomMax = roomMax;
            _score = score;
            _log = log;
        }

        private readonly Point3D _roomCenter;
        public Point3D RoomCenter => _roomCenter;

        private readonly Point3D _roomMin;
        public Point3D RoomMin => _roomMin;

        private readonly Point3D _roomMax;
        public Point3D RoomMax => _roomMax;

        private readonly FitnessInfo _score;
        public FitnessInfo Score => _score;

        private readonly BotTrackingEntry[] _log;
        public BotTrackingEntry[] Log => _log;
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
                if(_log.Keys.Count == 0)
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
