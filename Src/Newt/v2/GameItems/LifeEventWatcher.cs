using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Game.HelperClassesCore;
using Game.Newt.v2.GameItems.ShipParts;

namespace Game.Newt.v2.GameItems
{
    /// <summary>
    /// This watches containers, and looks for spikes in activity
    /// </summary>
    public class LifeEventWatcher : IPartUpdatable
    {
        #region Class: ContainerTracker

        private class ContainerTracker
        {
            private readonly IContainer _container;
            private readonly LifeEventType? _up;
            private readonly LifeEventType? _down;

            private double _prev = 0d;      // make sure that all access to this variable is threadsafe

            public ContainerTracker(IContainer container, LifeEventType? up, LifeEventType? down)
            {
                _container = container;
                _up = up;
                _down = down;
                _prev = container.QuantityCurrent;
            }

            public Tuple<LifeEventType, double> ExamineContainer()
            {
                // Get the current quantities (doing it once, because other threads could change the values at any time)
                double current = _container.QuantityCurrent;
                double max = _container.QuantityMax;
                double prev = Interlocked.Exchange(ref _prev, current);

                double diff = current - prev;

                return ExamineValue(diff, max, _up, _down);
            }

            public static Tuple<LifeEventType, double> ExamineValue(double diff, double max, LifeEventType? up, LifeEventType? down)
            {
                const double TOLERANCEPERCENT = .01;

                // Don't report if the % change is too small
                if (Math.Abs(diff) < max * TOLERANCEPERCENT)
                {
                    return null;
                }

                // Report the direction and percent change
                if (diff > 0 && up != null)
                {
                    return Tuple.Create(up.Value, diff / max);
                }
                else if (diff < 0 && down != null)
                {
                    return Tuple.Create(down.Value, -diff / max);
                }

                return null;        // if execution gets here, the corresponding enum was null
            }
        }

        #endregion

        #region Events

        public event EventHandler<LifeEventArgs> EventOccurred = null;

        #endregion

        #region Declaration Section

        private readonly ContainerTracker[] _standardContainers;
        private readonly CargoBayGroup _cargoBay;
        //private readonly object[] _customContainerGroups;

        private double _prevCargo = 0;      // make sure to use Interlocked with this

        private readonly DateTime _createTime = DateTime.UtcNow;

        #endregion

        #region Constructor

        public LifeEventWatcher(BotConstruction_Containers containers)
        {
            List<ContainerTracker> standardContainers = new List<ContainerTracker>();

            //TODO: Add more
            if (containers.PlasmaGroup != null)
            {
                standardContainers.Add(new ContainerTracker(containers.PlasmaGroup, null, LifeEventType.LostPlasma));
            }

            _standardContainers = standardContainers.ToArray();

            _cargoBay = containers.CargoBayGroup;
            if (_cargoBay != null)
            {
                _prevCargo = _cargoBay.CargoVolume.Item1;
            }
        }

        #endregion

        #region IPartUpdatable Members

        public void Update_MainThread(double elapsedTime)
        {
        }
        public void Update_AnyThread(double elapsedTime)
        {
            if (this.EventOccurred == null)
            {
                return;
            }

            // Compare current state to previous state for each item to be watched
            var hits = new List<Tuple<LifeEventType, double>>();
            Tuple<LifeEventType, double> result;

            #region standard containers

            foreach (ContainerTracker standard in _standardContainers)
            {
                result = standard.ExamineContainer();
                if (result != null)
                {
                    hits.Add(result);
                }
            }

            #endregion
            #region cargo bays

            if (_cargoBay != null)
            {
                Tuple<double, double> currentCargo = _cargoBay.CargoVolume;
                double prevCargo = Interlocked.Exchange(ref _prevCargo, currentCargo.Item1);

                double diffCargo = currentCargo.Item1 - prevCargo;

                result = ContainerTracker.ExamineValue(diffCargo, currentCargo.Item2, LifeEventType.AddedCargo, null);

                if (result != null)
                {
                    hits.Add(result);
                }
            }

            #endregion

            if(!_shouldRaiseEvents)
            {
                //NOTE: Waiting until now, because the prev values need to stay current
                return;
            }

            // Raise the events
            if (hits.Count > 0)
            {
                DateTime time = DateTime.UtcNow;

                // If the bot was just created, then don't raise events (giving it time to initialize)
                if ((time - _createTime).TotalSeconds < 1.5)
                {
                    return;
                }

                foreach (Tuple<LifeEventType, double> hit in hits)
                {
                    this.EventOccurred(this, new LifeEventArgs(time, hit.Item1, hit.Item2));
                }
            }
        }

        public int? IntervalSkips_MainThread
        {
            get
            {
                return null;
            }
        }
        public int? IntervalSkips_AnyThread
        {
            get
            {
                return 0;
            }
        }

        #endregion

        #region Public Properties

        private volatile bool _shouldRaiseEvents = true;
        public bool ShouldRaiseEvents
        {
            get
            {
                return _shouldRaiseEvents;
            }
            set
            {
                _shouldRaiseEvents = value;
            }
        }

        #endregion
    }

    public class LifeEventToVector
    {
        #region Events

        public event EventHandler<LifeEventVectorArgs> EventOccurred = null;

        #endregion

        #region Declaration Section

        private readonly LifeEventWatcher _watcher;
        private readonly Tuple<LifeEventType, double>[] _types;

        #endregion

        #region Constructor

        public LifeEventToVector(LifeEventWatcher watcher, LifeEventType[] types)
            : this(watcher, types.Select(o => Tuple.Create(o, 0d)).ToArray()) { }

        public LifeEventToVector(LifeEventWatcher watcher, Tuple<LifeEventType, double>[] types)
        {
            _watcher = watcher;
            _types = types;
            _typesProp = types.
                Select(o => o.Item1).
                ToArray();

            _watcher.EventOccurred += Watcher_EventOccurred;
        }

        #endregion

        #region Public Properties

        private readonly LifeEventType[] _typesProp;
        public LifeEventType[] Types
        {
            get
            {
                return _typesProp;
            }
        }

        #endregion

        #region Event Listeners

        /// <summary>
        /// This will see if the vector includes this type (and if the event is strong enough).  If so, it will raise its own event
        /// </summary>
        private void Watcher_EventOccurred(object sender, LifeEventArgs e)
        {
            if (this.EventOccurred == null)
            {
                return;
            }

            // Find the location in the vector for this type
            for (int cntr = 0; cntr < _types.Length; cntr++)
            {
                if (_types[cntr].Item1 != e.Type)
                {
                    continue;       // wrong type
                }

                if (e.Strength < _types[cntr].Item2)
                {
                    return;     // not strong enough
                }

                // Create the vector
                double[] vector = new double[_types.Length];
                double[] vector_scaled = new double[_types.Length];

                vector[cntr] = 1;
                vector_scaled[cntr] = UtilityCore.GetScaledValue(0, 1, _types[cntr].Item2, 1, e.Strength);

                // Raise event
                this.EventOccurred(this, new LifeEventVectorArgs(e, vector, vector_scaled));
            }
        }

        #endregion
    }

    #region Class: LifeEventArgs

    public class LifeEventArgs
    {
        public LifeEventArgs(DateTime time, LifeEventType type, double strength)
        {
            this.Time = time;
            this.Type = type;
            this.Strength = strength;
        }

        /// <summary>
        /// DateTime.UTCNow
        /// </summary>
        public readonly DateTime Time;
        public readonly LifeEventType Type;
        /// <summary>
        /// 0 to 1
        /// Gives listeners a chance to filter out minor events
        /// </summary>
        public readonly double Strength;
    }

    #endregion
    #region Class: LifeEventVectorArgs

    public class LifeEventVectorArgs
    {
        public LifeEventVectorArgs(LifeEventArgs args, double[] vector, double[] vector_scaled)
        {
            this.Time = args.Time;
            this.Type = args.Type;
            this.Strength = args.Strength;

            this.Vector = vector;
            this.Vector_Scaled = vector_scaled;
        }

        public readonly DateTime Time;
        public readonly LifeEventType Type;
        /// <summary>
        /// 0 to 1
        /// Gives listeners a chance to filter out minor events
        /// </summary>
        public readonly double Strength;

        public readonly double[] Vector;
        public readonly double[] Vector_Scaled;
    }

    #endregion

    #region Enum: LifeEventType

    public enum LifeEventType
    {
        AddedCargo,
        LostPlasma,     // using plasma like it is hitpoints
    }

    #endregion
}
