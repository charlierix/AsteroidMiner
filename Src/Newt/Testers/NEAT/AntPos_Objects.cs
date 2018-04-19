using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;

namespace Game.Newt.Testers.NEAT
{
    #region enum: TrackedItemType

    public enum TrackedItemType
    {
        // Independent Movers

        /// <summary>
        /// Only goes straight
        /// </summary>
        Streaker,       //AD263F
                        /// <summary>
                        /// Goes a set distance, turns a set angle....repeat
                        /// </summary>
        Polygon,        //476C5E
                        /// <summary>
                        /// Goes in an ellipse
                        /// </summary>
        Elliptical,     //588F27
                        /// <summary>
                        /// Moves randomly
                        /// </summary>
        Brownian,       //BF8A49

        // Dependent Movers

        //TODO: Make different swarming behaviors
    }

    #endregion

    #region class: TrackedItemBase

    public abstract class TrackedItemBase
    {
        public TrackedItemBase(double mapSize, bool bouncesOffWalls, double currentTime)
        {
            Token = TokenGenerator.NextToken();
            MapSize = mapSize;
            HalfMapSize = mapSize / 2d;
            BouncesOffWalls = bouncesOffWalls;
            CreateTime = currentTime;
        }

        public long Token { get; private set; }

        public double MapSize { get; private set; }
        public double HalfMapSize { get; private set; }
        public bool BouncesOffWalls { get; private set; }

        public double CreateTime { get; private set; }

        public Point Position { get; set; }
        public Vector Velocity { get; set; }

        // Objects like the straight line streaker should have zero error, but brownian would need to allow for more error (because it's unpredictible by design)
        // These are multipliers: 0 to 1.  They get multiplied by the max speed of the object (so they are used to create a dot radius in world coords)
        public abstract double MaxPositionError { get; }
        public abstract double MaxVelocityError { get; }

        public bool IsDead { get; set; }

        public abstract Color Color { get; }

        /// <summary>
        /// This returns false if the bot should die
        /// </summary>
        public abstract bool Tick(double elapsedTime);

        // No, just run the simulation for a few steps ahead of what you currently show
        //public abstract OutputPosition GetFuturePosition(double time);
    }

    #endregion
    #region class: TrackedItemStreaker

    public class TrackedItemStreaker : TrackedItemBase
    {
        public TrackedItemStreaker(double mapSize, bool bouncesOffWalls, double currentTime)
            : base(mapSize, bouncesOffWalls, currentTime) { }

        public override double MaxPositionError => 0d;
        public override double MaxVelocityError => 0d;

        public override Color Color
        {
            get
            {
                return UtilityWPF.ColorFromHex("AD263F");
            }
        }

        public override bool Tick(double elapsedTime)
        {
            if (IsDead)
            {
                return false;
            }

            if (Math.Abs(Position.X + (Velocity.X * elapsedTime)) > HalfMapSize)
            {
                if (BouncesOffWalls)
                {
                    Velocity = new Vector(-Velocity.X, Velocity.Y);
                }
                else
                {
                    IsDead = true;
                    return false;
                }
            }

            if (Math.Abs(Position.Y + (Velocity.Y * elapsedTime)) > HalfMapSize)
            {
                if (BouncesOffWalls)
                {
                    Velocity = new Vector(Velocity.X, -Velocity.Y);
                }
                else
                {
                    IsDead = true;
                    return false;
                }
            }

            Position = Position + (Velocity * elapsedTime);

            return true;
        }
    }

    #endregion

    #region class: TrackedItemHarness

    public class TrackedItemHarness
    {
        #region class: ItemHistoryEntry

        private class ItemHistoryEntry
        {
            public ItemHistoryEntry(double time, TrackedItemBase item = null)
            {
                Time = time;
                Item = item;

                if (item == null)
                {
                    Position_Velocity = null;
                }
                else
                {
                    Position_Velocity = Tuple.Create(item.Position, item.Velocity);
                }
            }

            public readonly double Time;

            // These two can be null if there was no item at that time
            public readonly TrackedItemBase Item;
            public readonly Tuple<Point, Vector> Position_Velocity;
        }

        #endregion

        #region Events

        public event EventHandler ItemAdded = null;
        public event EventHandler ItemRemoved = null;

        #endregion

        #region Declaration Section

        /// <summary>
        /// Holds previous positions
        /// </summary>
        /// <remarks>
        /// Starting this small, then expanding as needed
        /// </remarks>
        private CircularBuffer<ItemHistoryEntry> _snapshots = new CircularBuffer<ItemHistoryEntry>(100);

        #endregion

        #region Constructor

        public TrackedItemHarness(HarnessArgs args)
            : this(args.MapSize, args.VisionSize, args.OutputSize, args.InputSizeXY, args.OutputSizeXY, args.DelayBetweenInstances) { }
        public TrackedItemHarness(double mapSize, double visionSize, double outputSize, double inputDensity, double outputDensity, double delayBetweenInstances)
            : this(mapSize, visionSize, outputSize, (visionSize * inputDensity).ToInt_Round(), (outputSize * outputDensity).ToInt_Round(), delayBetweenInstances) { }
        public TrackedItemHarness(double mapSize, double visionSize, double outputSize, int inputSizeXY, int outputSizeXY, double delayBetweenInstances)
        {
            MapSize = mapSize;
            VisionSize = visionSize;
            OutputSize = outputSize;

            InputSizeXY = inputSizeXY;
            InputCellCenters = Math2D.GetCells_WithinSquare(visionSize, InputSizeXY).
                Select(o => o.center).
                ToArray();

            OutputSizeXY = outputSizeXY;
            OutputCellCenters = Math2D.GetCells_WithinSquare(outputSize, OutputSizeXY).
                Select(o => o.center).
                ToArray();

            DelayBetweenInstances = delayBetweenInstances;
        }

        #endregion

        #region Public Properties

        //NOTE: Some of these properties aren't used by the class, they are informational (this class could use a redesign)

        public double Time
        {
            get;
            set;
        }

        private TrackedItemBase _item = null;
        /// <summary>
        /// NOTE: This considers DelayBetweenInstances and returns null if the item is too new
        /// </summary>
        public TrackedItemBase Item
        {
            get
            {
                if (_item == null)
                {
                    return null;
                }
                else if (Time - _item.CreateTime < DelayBetweenInstances)
                {
                    // There is an item loaded, but pretend like it's not there yet
                    return null;
                }
                else
                {
                    return _item;
                }
            }
            private set
            {
                _item = value;
            }
        }

        /// <summary>
        /// The size of the entire map
        /// </summary>
        public double MapSize
        {
            get;
            private set;
        }
        /// <summary>
        /// How much of the map the NN can see
        /// </summary>
        public double VisionSize
        {
            get;
            private set;
        }
        /// <summary>
        /// How much of the map the NN needs to be anticipate
        /// </summary>
        /// <remarks>
        /// A good NN should be able to handle an output size that is a bit larger than vision size
        /// </remarks>
        public double OutputSize
        {
            get;
            private set;
        }

        /// <summary>
        /// The width and height of the input pixel array.  The total input neuron count is this squared
        /// </summary>
        public int InputSizeXY
        {
            get;
            private set;
        }
        public Point[] InputCellCenters
        {
            get;
            private set;
        }

        /// <summary>
        /// The width and height of the output pixel array.  The total input neuron count is this squared
        /// </summary>
        public int OutputSizeXY
        {
            get;
            private set;
        }
        public Point[] OutputCellCenters
        {
            get;
            private set;
        }

        /// <summary>
        /// When a new item is created, this is how long to wait before using/showing it
        /// </summary>
        public double DelayBetweenInstances
        {
            get;
            private set;
        }

        #endregion

        #region Public Methods

        public void SetItem(TrackedItemBase item)
        {
            ClearItem();

            Item = item;

            ItemAdded?.Invoke(this, new EventArgs());
        }
        public void ClearItem()
        {
            //if (Item != null)
            if (_item != null)       // can't use the public property, because it returns null if the item is new
            {
                Item = null;
                ItemRemoved?.Invoke(this, new EventArgs());
            }
        }

        public void Tick(double elapsedTime)
        {
            // Advance the clock
            Time += elapsedTime;

            // Tell the item to move
            if (Item != null)
            {
                if (!Item.Tick(elapsedTime))
                {
                    ClearItem();
                }
            }

            // Store the position (or store a null item at this time)
            _snapshots.Add(new ItemHistoryEntry(Time, Item));
        }

        //TODO: Expose outputs that visualizers or neural can use

        //TODO: Can't just say where != null.  Need to account for null times

        public Tuple<TrackedItemBase, Point, Vector> GetPreviousPosition(double time)
        {
            // Find the two snapshots that straddle this requested time
            ItemHistoryEntry low = null;
            ItemHistoryEntry high = null;
            foreach (ItemHistoryEntry item in _snapshots.EnumerateReverse())
            {
                if (item.Time.IsNearValue(time))
                {
                    if (item.Item == null)
                    {
                        // There was no item at this time
                        return null;
                    }
                    else
                    {
                        return Tuple.Create(item.Item, item.Position_Velocity.Item1, item.Position_Velocity.Item2);
                    }
                }
                else if (item.Time < time)
                {
                    low = item;
                    break;
                }
                else
                {
                    high = item;
                }
            }

            if (_snapshots.HasWrapped && (low == null || high == null))
            {
                _snapshots.ChangeSize(Convert.ToInt32(_snapshots.MaxCount * 1.5));
            }

            // Return the lerp of those two
            if (low?.Item == null && high?.Item == null)
            {
                return null;
            }
            else if (low?.Item != null && high?.Item != null)
            {
                double percent = (time - low.Time) / (high.Time - low.Time);
                if (percent.IsInvalid())        // it should only be invalid if the denominator is zero
                {
                    percent = .5;
                }

                if (low.Item.Token == high.Item.Token)
                {
                    return Tuple.Create(
                        low.Item,
                        Math2D.LERP(low.Position_Velocity.Item1, high.Position_Velocity.Item1, percent),
                        Math2D.LERP(low.Position_Velocity.Item2, high.Position_Velocity.Item2, percent));
                }
                else if (percent < .5)
                {
                    // The item tokens changed between time steps, and this time is closer to the request, so return it
                    return Tuple.Create(low.Item, low.Position_Velocity.Item1, low.Position_Velocity.Item2);
                }
                else
                {
                    return Tuple.Create(high.Item, high.Position_Velocity.Item1, high.Position_Velocity.Item2);
                }
            }
            else if (low?.Item != null)
            {
                return Tuple.Create(low.Item, low.Position_Velocity.Item1, low.Position_Velocity.Item2);        // inaccurate, but as good as can be found (shouldn't happen, or very rarely)
            }
            else
            {
                // This happens when the harness is first created and being used.  There hasn't been enough history yet.  So return null instead of the item's current position
                //return Tuple.Create(high.Item, high.Position, high.Velocity);
                return null;
            }
        }

        #endregion
    }

    #endregion

    #region class: HarnessArgs

    public class HarnessArgs
    {
        public HarnessArgs(double mapSize, double visionSize, double outputSize, int inputSizeXY, int outputSizeXY, double delayBetweenInstances)
        {
            MapSize = mapSize;
            VisionSize = visionSize;
            OutputSize = outputSize;
            InputSizeXY = inputSizeXY;
            OutputSizeXY = outputSizeXY;
            DelayBetweenInstances = delayBetweenInstances;
        }

        public double MapSize
        {
            get;
            private set;
        }
        public double VisionSize
        {
            get;
            private set;
        }
        public double OutputSize
        {
            get;
            private set;
        }

        public int InputSizeXY
        {
            get;
            private set;
        }
        public int OutputSizeXY
        {
            get;
            private set;
        }

        public double DelayBetweenInstances
        {
            get;
            private set;
        }
    }

    #endregion
    #region class: EvaluatorArgs

    public class EvaluatorArgs
    {
        public EvaluatorArgs(int totalNumberEvaluations, double delay_Seconds, double elapsedTime_Seconds, Tuple<TrackedItemType, Point, Vector, bool>[] newItemStart, double newItem_Duration_Multiplier, double newItem_ErrorMultiplier, ScoreLeftRightBias errorBias)
            : this(totalNumberEvaluations, delay_Seconds, elapsedTime_Seconds, newItemStart.Max(o => o.Item3.Length), newItemStart.Any(o => o.Item4), null, newItemStart, newItem_Duration_Multiplier, newItem_ErrorMultiplier, errorBias) { }

        public EvaluatorArgs(int totalNumberEvaluations, double delay_Seconds, double elapsedTime_Seconds, double maxSpeed, bool bounceOffWalls, TrackedItemType[] itemTypes, double newItem_Duration_Multiplier, double newItem_ErrorMultiplier, ScoreLeftRightBias errorBias)
            : this(totalNumberEvaluations, delay_Seconds, elapsedTime_Seconds, maxSpeed, bounceOffWalls, itemTypes, null, newItem_ErrorMultiplier, newItem_ErrorMultiplier, errorBias) { }

        private EvaluatorArgs(int totalNumberEvaluations, double delay_Seconds, double elapsedTime_Seconds, double maxSpeed, bool bounceOffWalls, TrackedItemType[] itemTypes, Tuple<TrackedItemType, Point, Vector, bool>[] newItemStart, double newItem_Duration_Multiplier, double newItem_ErrorMultiplier, ScoreLeftRightBias errorBias)
        {
            TotalNumberEvaluations = totalNumberEvaluations;
            Delay_Seconds = delay_Seconds;
            ElapsedTime_Seconds = elapsedTime_Seconds;

            MaxSpeed = maxSpeed;
            BounceOffWalls = bounceOffWalls;
            ItemTypes = itemTypes;
            NewItemStart = newItemStart;

            NewItem_Duration_Multiplier = newItem_Duration_Multiplier;
            NewItem_ErrorMultiplier = newItem_ErrorMultiplier;

            MaxDistancePerTick = maxSpeed * elapsedTime_Seconds;

            ErrorBias = errorBias;
        }

        /// <summary>
        /// How many frames to evaluate before coming up with a final score
        /// </summary>
        public int TotalNumberEvaluations
        {
            get;
            private set;
        }

        /// <summary>
        /// How long between the previous and current positions
        /// </summary>
        /// <remarks>
        /// This is how long in the future the brain will be trained for
        /// </remarks>
        public double Delay_Seconds
        {
            get;
            private set;
        }

        /// <summary>
        /// How much time each tick should be
        /// </summary>
        public double ElapsedTime_Seconds
        {
            get;
            private set;
        }

        //---------------------------------------- If NewItemStart is populated, then this secion is ignored
        public double MaxSpeed
        {
            get;
            private set;
        }
        /// <summary>
        /// True: The item will bounce off walls and live forever
        /// False: The item will die when it hits a wall.  A new random item will spawn
        /// </summary>
        public bool BounceOffWalls
        {
            get;
            private set;
        }
        /// <summary>
        /// What types are possible to spawn
        /// </summary>
        public TrackedItemType[] ItemTypes
        {
            get;
            private set;
        }
        //----------------------------------------

        /// <summary>
        /// If this is populated, then the new item will start with one of these values
        /// </summary>
        /// <remarks>
        /// Item1=Type of item
        /// Item2=Staring position
        /// Item3=Starting velocity
        /// Item4=Bounce off walls
        /// </remarks>
        public Tuple<TrackedItemType, Point, Vector, bool>[] NewItemStart
        {
            get;
            private set;
        }

        /// <summary>
        /// How long before the error should settle down to standard size
        /// </summary>
        public double NewItem_Duration_Multiplier
        {
            get;
            private set;
        }
        /// <summary>
        /// How large the error should be when an item is first seen
        /// </summary>
        /// <remarks>
        /// When a new item appears, there needs to be a higher error until it can be determined how fast/what direction
        /// the item is going (because the first frame is just a static dot.  You need a few frames to see how it's moving)
        /// </remarks>
        public double NewItem_ErrorMultiplier
        {
            get;
            private set;
        }

        /// <summary>
        /// This is a calculated field.  It's the max distance the item could possibly travel in a tick
        /// </summary>
        public double MaxDistancePerTick
        {
            get;
            private set;
        }

        public ScoreLeftRightBias ErrorBias
        {
            get;
            private set;
        }
    }

    #endregion

    #region class: OutputPosition

    public class OutputPosition
    {
        private double[] _rawOutput = null;
        public double[] RawOutput
        {
            get
            {
                if (_rawOutput == null)
                {
                    _rawOutput = new double[8];
                }

                return _rawOutput;
            }
            set
            {
                if (value != null && value.Length != 8)
                {
                    throw new ArgumentException("The size of the output vector must be 8: " + value.Length.ToString());
                }

                _rawOutput = value;
            }
        }

        public Point3D Position
        {
            get
            {
                double[] raw = RawOutput;       // let this do length checks

                return new Point3D(raw[0], raw[1], raw[2]);
            }
            set
            {
                double[] raw = RawOutput;

                raw[0] = value.X;
                raw[1] = value.Y;
                raw[2] = value.Z;
            }
        }
        public double PositionUncertaintyRadius
        {
            get
            {
                double[] raw = RawOutput;       // let this do length checks

                return raw[3];
            }
            set
            {
                double[] raw = RawOutput;

                raw[3] = value;
            }
        }

        public Vector3D Velocity
        {
            get
            {
                double[] raw = RawOutput;       // let this do length checks

                return new Vector3D(raw[4], raw[5], raw[6]);
            }
            set
            {
                double[] raw = RawOutput;

                raw[4] = value.X;
                raw[5] = value.Y;
                raw[6] = value.Z;
            }
        }
        public double VelocityUnceartaintyRadius
        {
            get
            {
                double[] raw = RawOutput;       // let this do length checks

                return raw[7];
            }
            set
            {
                double[] raw = RawOutput;

                raw[7] = value;
            }
        }
    }

    #endregion

    #region enum: ScoreLeftRightBias

    public enum ScoreLeftRightBias
    {
        /// <summary>
        /// There is no bias.  The error is a standard error squared
        /// </summary>
        Standard,
        /// <summary>
        /// This penalizes false positives more than false negatives.  False Positives are a square root (the error climbs quickly
        /// instead of slowly).  False Negatives are standard squared error
        /// </summary>
        PenalizeFalsePositives_Weak,
        PenalizeFalsePositives_Medium,
        PenalizeFalsePositives_Strong,
        /// <summary>
        /// This is just the opposite.  False Negatives error quickly
        /// </summary>
        PenalizeFalseNegatives,
    }

    #endregion
}
