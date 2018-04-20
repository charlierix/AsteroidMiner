using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v2.GameItems.ShipParts;

namespace Game.Newt.v2.GameItems
{
    //TODO: Move most of this cs file into the AI.dll
    //It shouldn't know about physics bodies.  Just a neural library on its own

    //TODO: Read more about deep NN/convolution NN
    //http://colah.github.io/posts/2015-01-Visualizing-Representations/
    //https://christopherolah.wordpress.com/
    //http://colah.github.io/

    //TODO: Prove: Neurons/Links are stored in 2D or 3D.  What are the advantages of 3D over 2D?  More chances for groups to be near each other?  If
    //there's a clear advantage, what about 4D, 5D, etc?  Does there need to be a certain number/density of neurons before it makes sense to add a dimension?

    #region Enum: NeuronContainerType

    /// <summary>
    /// This allows more granular control of how many neural links to allow between types of containers
    /// </summary>
    public enum NeuronContainerType
    {
        /// <summary>
        /// May want to call this input
        /// </summary>
        Sensor,
        Brain,
        /// <summary>
        /// May want to call this output
        /// </summary>
        Manipulator,
        /// <summary>
        /// Don't link neurons
        /// </summary>
        /// <remarks>
        /// Added this when I implemented RGBCamera and Recognizer.  If a camera and recognizer are linked, then the camera should go to none, because
        /// the recognizer becomes the camera's output
        /// </remarks>
        None,
    }

    #endregion

    #region Interface: INeuron

    /// <summary>
    /// This represents a single neuron.  It could be the output of a sensor made to look like a neuron, it could be the input
    /// into a part (like a thruster), or it could be a free standing neuron
    /// </summary>
    /// <remarks>
    /// The ship parts can have multiple interfaces.  Thruster has a Fire(percent) method, but that isn't useful to a neural net.
    /// So the thruster also exposes INeuronContainer, and the activation of its neurons are converted into thrust firings.
    /// 
    /// Same thing with something like the gravity sensor.  It's efficient to simply expose a current gravity vector, but that's
    /// useless to a neural net.  So it exposes a cloud of neurons that activate based on how aligned they are to that vector.
    /// 
    /// A brain would hold pure neurons, and acts like glue between sensors and manipulators.  From the brain's perspective,
    /// it's all just a bunch of neurons that could be connected to, even adjacent brains.  Theoretically, if a ship has multiple
    /// brains, some would become specialized (visual cortex, motor cortex, etc)
    /// </remarks>
    public interface INeuron
    {
        /// <summary>
        /// This is the neuron's current output value
        /// </summary>
        double Value
        {
            get;
        }

        /// <summary>
        /// This is the position of the neuron in model space
        /// </summary>
        /// <remarks>
        /// The ship parts have positions in ship space.  The neuron's position isn't in that ship space, and isn't really constrained
        /// to the hosting part's size.  It's more of a virtual position, and only has meaning relative to the other neurons in the
        /// container.  Those positions should probably be normalized so that the farthest neuron has a radius of 1?
        /// </remarks>
        Point3D Position
        {
            get;
        }

        /// <summary>
        /// This is just here to help with drawing the neuron (pos only can be blue, neg/pos can be red/green)
        /// True: output goes from 0 to 1
        /// False: output goes from -1 to 1
        /// </summary>
        bool IsPositiveOnly
        {
            get;
        }

        /// <summary>
        /// The value can't be set directly, it must go through an activation function
        /// </summary>
        /// <remarks>
        /// This interface exposes this set method, but the implementor may choose to ignore the call (in the case of read only
        /// sensors)
        /// </remarks>
        void SetValue(double sumInputs);
    }

    #endregion
    #region Interface: INeuronContainer

    public interface INeuronContainer
    {
        // Exposing as three separate lists to make it easier for brains to wire in
        IEnumerable<INeuron> Neruons_Readonly
        {
            get;
        }
        IEnumerable<INeuron> Neruons_ReadWrite
        {
            get;
        }
        IEnumerable<INeuron> Neruons_Writeonly
        {
            get;
        }

        // This needs to always be the combination of the three specialized lists
        IEnumerable<INeuron> Neruons_All
        {
            get;
        }

        Point3D Position
        {
            get;
        }
        Quaternion Orientation
        {
            get;
        }
        /// <summary>
        /// This is just a rough estimate of the size of this container.  It is only an assist for creating neuron visuals, it has no effect on
        /// where actual neurons are placed.
        /// </summary>
        double Radius
        {
            get;
        }

        NeuronContainerType NeuronContainerType
        {
            get;
        }

        bool IsOn
        {
            get;
        }
    }

    #endregion

    #region Class: Neuron_ZeroPos

    /// <summary>
    /// This class is hardcoded to constrain the output from 0 to 1 (along an S curve)
    /// </summary>
    /// <remarks>
    /// I didn't want to make a single universal class with a switch statement, because I want speed
    /// </remarks>
    public class Neuron_ZeroPos : INeuron
    {
        #region Constructor

        public Neuron_ZeroPos(Point3D position)
        {
            _position = position;
        }

        #endregion

        private volatile object _value = 0d;
        public double Value
        {
            get
            {
                return (double)_value;
            }
            private set
            {
                _value = value;
            }
        }

        private readonly Point3D _position;
        public Point3D Position
        {
            get
            {
                return _position;
            }
        }

        public bool IsPositiveOnly
        {
            get
            {
                return true;
            }
        }

        public void SetValue(double sumInputs)
        {
            if (sumInputs == 0d)
            {
                _value = 0d;
                return;
            }

            // Manually inlined Transform_S_NegPos
            //_value = 1d / (1d + Math.Pow(Math.E, -1d * sumInputs));

            // if I don't subtract 5, then an input of zero will make an output of .5 (negative inputs would make a zero).  This will shift it so an input of zero will make .00669
            // But then the inputs need to be multiplied so it doesn't take such large inputs to get a value of one
            // This has the side affect of causing the neuron to be more sensitive
            double sumModified = (sumInputs * 3d) - 5d;

            _value = 1d / (1d + Math.Pow(Math.E, -1d * sumModified));
        }

        #region Private Methods

        //private static double Transform_LinearCapped_NegPos(double sumInputs)
        //{
        //    if (sumInputs < -1d)
        //    {
        //        return -1d;
        //    }
        //    else if (sumInputs > 1d)
        //    {
        //        return 1d;
        //    }
        //    else
        //    {
        //        return sumInputs;
        //    }
        //}
        //private static double Transform_LinearCapped_ZeroPos(double sumInputs)
        //{
        //    if (sumInputs < 0d)
        //    {
        //        return 0d;
        //    }
        //    else if (sumInputs > 1d)
        //    {
        //        return 1d;
        //    }
        //    else
        //    {
        //        return sumInputs;
        //    }
        //}
        //private static double Transform_S_NegPos(double sumInputs)
        //{
        //    // This returns an S curve with asymptotes at -1 and 1
        //    double e2x = Math.Pow(Math.E, 2d * sumInputs);
        //    return (e2x - 1d) / (e2x + 1d);
        //}
        //private static double Transform_S_ZeroPos(double sumInputs)
        //{
        //    // This returns an S curve with asymptotes at 0 and 1
        //    return 1d / (1d + Math.Pow(Math.E, -1d * sumInputs));
        //}

        #endregion
    }

    #endregion
    #region Class: Neuron_NegPos

    /// <summary>
    /// This class is hardcoded to constrain the output from -1 to 1 (along an S curve)
    /// </summary>
    /// <remarks>
    /// I didn't want to make a single universal class with a switch statement, because I want speed
    /// </remarks>
    public class Neuron_NegPos : INeuron
    {
        #region Constructor

        public Neuron_NegPos(Point3D position)
        {
            _position = position;
        }

        #endregion

        private volatile object _value = 0d;
        public double Value
        {
            get
            {
                return (double)_value;
            }
            private set
            {
                _value = value;
            }
        }

        private readonly Point3D _position;
        public Point3D Position
        {
            get
            {
                return _position;
            }
        }

        public bool IsPositiveOnly
        {
            get
            {
                return false;
            }
        }

        public void SetValue(double sumInputs)
        {
            // Manually inlined Transform_S_NegPos
            double e2x = Math.Pow(Math.E, 2d * sumInputs);
            _value = (e2x - 1d) / (e2x + 1d);
        }

        #region Private Methods

        //private static double Transform_LinearCapped_NegPos(double sumInputs)
        //{
        //    if (sumInputs < -1d)
        //    {
        //        return -1d;
        //    }
        //    else if (sumInputs > 1d)
        //    {
        //        return 1d;
        //    }
        //    else
        //    {
        //        return sumInputs;
        //    }
        //}
        //private static double Transform_LinearCapped_ZeroPos(double sumInputs)
        //{
        //    if (sumInputs < 0d)
        //    {
        //        return 0d;
        //    }
        //    else if (sumInputs > 1d)
        //    {
        //        return 1d;
        //    }
        //    else
        //    {
        //        return sumInputs;
        //    }
        //}
        //private static double Transform_S_NegPos(double sumInputs)
        //{
        //    // This returns an S curve with asymptotes at -1 and 1
        //    double e2x = Math.Pow(Math.E, 2d * sumInputs);
        //    return (e2x - 1d) / (e2x + 1d);
        //}
        //private static double Transform_S_ZeroPos(double sumInputs)
        //{
        //    // This returns an S curve with asymptotes at 0 and 1
        //    return 1d / (1d + Math.Pow(Math.E, -1d * sumInputs));
        //}

        #endregion
    }

    #endregion
    #region Class: Neuron_Spike

    /// <summary>
    /// This is an attempt to better simulate the way real neurons work.  It will only let this.Value be nonzero for a brief time after
    /// it's been set.
    /// </summary>
    /// <remarks>
    /// Real neurons don't put out the same value forever, but instead generate a momentary spike or pulse.  Because of that, real
    /// neurons can get all kinds of unsyncronized spikes, and just see that as background noise.  It's only when the spikes from many
    /// inputs coincide will a neuron decide to emit its own spike.
    /// 
    /// Modeling this can be a bit tricky though.  Each real world neuron works instantly, but in a computer, there is a timer tick that
    /// will evaluate many neurons at once.  Because of this, all neurons will appear to spike at the same time.
    /// 
    /// I thought about several ways to try to fix this, but decided that the heavy logic doesn't belong in the neuron, it belongs in the
    /// timer that evaluates neurons.  So this neuron class should be complete enough.
    /// 
    /// As far as implementing the neuron evaluator, the length of the link between neurons needs to be considered, and the spikes
    /// will travel a constant speed through those links.  Store those spike values on a modified link class that's designed sort of like
    /// a queue (put a spike in one end, and ask for the current value at the other end) - if the timer is too slow, spikes will expire
    /// from the queue without ever being used.
    /// 
    /// So the speed of the links will need to be fairly slow to make sure that several timer ticks fire while spikes are in transit.  This
    /// should be an accurate model, but will make the neural net very slow, and it won't be able to respond in real time to the
    /// simulation.
    /// 
    /// Sooooooo, this class is probably fairly useless, but I felt like writing it anyway.  Maybe if sprinkled lightly among regular neurons,
    /// it will have a purpose that I'm not considering.  Give evolution an opportunity to use it, and see if it gets used.
    /// 
    /// Thinking about this more, the spike neurons will be fairly useless without the queue links.  But a hybrid brain that uses fast
    /// for time critical, and slow for deeper thought may be worth it.  The article below talked about how spikes with a high amount
    /// of syncronization were good triggers for storing a strong memory.
    /// 
    /// A good read (from oct 2012 issue):
    /// http://www.scientificamerican.com/article.cfm?id=how-nerve-cells-communicate
    /// </remarks>
    public class Neuron_Spike : INeuron
    {
        #region Declaration Section

        /// <summary>
        /// This is how long the value will stay nonzero after being set
        /// </summary>
        private volatile object _pulseDuration;		// this is a TimeSpan

        /// <summary>
        /// When this gets to zero or below, an eval needs to be started
        /// </summary>
        private int _countdownTillEval = -1;

        private readonly object _lock = new object();

        // These two are modified within the above lock
        private DateTime _startSampleTime;
        private int _sampleCounter = 0;

        #endregion

        #region Constructor

        public Neuron_Spike(Point3D position)
        {
            _position = position;
            _pulseDuration = TimeSpan.FromMinutes(1d);		// use way too long of a duration until a better value can be determined
            _value = new Tuple<double, DateTime>(0d, DateTime.UtcNow);
        }

        #endregion

        private volatile Tuple<double, DateTime> _value;		// double is the value, datetime is when it expires
        public double Value
        {
            get
            {
                var value = _value;		// since it's volatile, get a local copy
                if (DateTime.UtcNow < value.Item2)
                {
                    // They are still within the pulse's duration
                    return value.Item1;
                }
                else
                {
                    // They asked too late
                    return 0d;
                }
            }
        }

        private readonly Point3D _position;
        public Point3D Position
        {
            get
            {
                return _position;
            }
        }

        public bool IsPositiveOnly
        {
            get
            {
                return true;
            }
        }

        public void SetValue(double sumInputs)
        {
            const int SAMPLECOUNT = 15;		// record time across 15 ticks
            const double PULSEPERCENTOFTICK = 1.5d;		// .5 means that a pulse lasts half the time between ticks
            const int COUNTDOWNTICKS = 10000;		// how many ticks to wait before reevaluating

            //NOTE: It's harder to read, but I'm keeping all the logic in a single method so that this runs as fast as possible

            DateTime now = DateTime.UtcNow;

            #region Determine new value

            // Copied from Neuron_ZeroPos
            double sumModified = (sumInputs * 3d) - 5d;
            double value = 1d / (1d + Math.Pow(Math.E, -1d * sumModified));

            _value = new Tuple<double, DateTime>(value, now + (TimeSpan)_pulseDuration);

            #endregion

            // See if the pulse duration needs to be reevaluated
            if (Interlocked.Add(ref _countdownTillEval, -1) <= 0)
            {
                #region Determine pulse duration

                lock (_lock)
                {
                    if (_countdownTillEval <= 0)		// make sure some other thread didn't already finish taking samples.  Note that the lock doesn't fully protect this variable, but it should be good enough
                    {
                        if (_sampleCounter == 0)
                        {
                            // First time sampling.  Init
                            _startSampleTime = now;
                            _sampleCounter++;
                        }
                        else if (_sampleCounter > SAMPLECOUNT)
                        {
                            #region Finished taking samples

                            // Get the average time between ticks
                            double duration = (now - _startSampleTime).TotalMilliseconds / Convert.ToDouble(_sampleCounter);

                            // Now take that duration times some percent
                            int durationInt = Convert.ToInt32(Math.Round(duration * PULSEPERCENTOFTICK));
                            if (durationInt == 0)
                            {
                                durationInt = 1;
                            }

                            // Store as a timespan
                            _pulseDuration = TimeSpan.FromMilliseconds(durationInt);

                            // Reset variables - need to reevaluate the duration every once in a while in case the timings change: maybe there is more or less
                            // load in the future, maybe the AI threading gets throttled back because of some environment config, maybe the machine decided
                            // to run the processor at 100%, etc
                            _sampleCounter = 0;
                            _countdownTillEval = COUNTDOWNTICKS + Convert.ToInt32(Math.Floor(StaticRandom.NextDouble() * COUNTDOWNTICKS));		// adding a bit of random so that all neurons aren't reevaluating at the same time

                            #endregion
                        }
                        else
                        {
                            // Count this tick
                            _sampleCounter++;
                        }
                    }
                }

                #endregion
            }
        }
    }

    #endregion
    #region Class: Neuron_SensorPosition

    /// <summary>
    /// This is a readonly neuron that is meant to be used by sensors.  There are extra properties for position length, and the value
    /// property needs to be set directly by the sensor, SetValue is ignored by default
    /// </summary>
    public class Neuron_SensorPosition : INeuron
    {
        public Neuron_SensorPosition(Point3D position, bool isPositiveOnly, bool ignoreSetValue = true)
        {
            _position = position;
            _isPositiveOnly = isPositiveOnly;
            _ignoreSetValue = ignoreSetValue;

            if (Math3D.IsNearZero(position))
            {
                _positionUnit = null;
                _positionLength = 0d;
            }
            else
            {
                _positionUnit = position.ToVector().ToUnit();
                _positionLength = position.ToVector().Length;
            }
        }

        private readonly bool _ignoreSetValue;

        private volatile object _value = 0d;
        public double Value
        {
            get
            {
                return (double)_value;
            }
            set
            {
                _value = value;
            }
        }

        private readonly Point3D _position;
        public Point3D Position
        {
            get
            {
                return _position;
            }
        }

        private readonly Vector3D? _positionUnit;		// this is null if the position is at zero
        public Vector3D? PositionUnit
        {
            get
            {
                return _positionUnit;
            }
        }

        private readonly double _positionLength;
        public double PositionLength
        {
            get
            {
                return _positionLength;
            }
        }

        private readonly bool _isPositiveOnly;
        public bool IsPositiveOnly
        {
            get
            {
                return _isPositiveOnly;
            }
        }

        public void SetValue(double sumInputs)
        {
            if (!_ignoreSetValue)
            {
                // These sum functions are manually inlined
                if (_isPositiveOnly)
                {
                    if (sumInputs == 0d)
                    {
                        _value = 0d;
                    }
                    else
                    {
                        double sumModified = (sumInputs * 3d) - 5d;
                        _value = 1d / (1d + Math.Pow(Math.E, -1d * sumModified));
                    }
                }
                else
                {
                    double e2x = Math.Pow(Math.E, 2d * sumInputs);
                    _value = (e2x - 1d) / (e2x + 1d);
                }
            }
        }
    }

    #endregion
    #region Class: Neuron_Fade

    /// <summary>
    /// This is meant to be used as a brain sensor.  It takes a bit of time to get the value high, then the value slowly fades back to
    /// zero instead of instantly dropping back down
    /// </summary>
    public class Neuron_Fade : INeuron
    {
        #region Declaration Section

        private readonly double _kUp;       // = 10d;
        private readonly double _kDown;     // = 50d;

        private readonly double _valueCutoff;       // = .75d;

        /// <summary>
        /// This is the average amount of time between calls to SetValue.  This stays null until an accurate average can
        /// be calculated
        /// </summary>
        private volatile object _elapsedTime = null;

        /// <summary>
        /// When this gets to zero or below, an eval needs to be started
        /// </summary>
        private int _countdownTillEval = -1;

        private readonly object _lock = new object();

        // These two are modified within the above lock
        private DateTime _startSampleTime;
        private int _sampleCounter = 0;

        #endregion

        #region Constructor

        public Neuron_Fade(Point3D position, double kUp, double kDown, double valueCutoff)
        {
            _position = position;

            _kUp = kUp;
            _kDown = kDown;
            _valueCutoff = valueCutoff;
        }

        #endregion

        #region INeuron Members

        private volatile object _value = 0d;
        public double Value
        {
            get
            {
                return (double)_value;
            }
            private set
            {
                _value = value;
            }
        }

        private readonly Point3D _position;
        public Point3D Position
        {
            get
            {
                return _position;
            }
        }

        public bool IsPositiveOnly
        {
            get
            {
                return true;
            }
        }

        public void SetValue(double sumInputs)
        {
            const int SAMPLECOUNT = 150;		// record time across 15 ticks
            const int COUNTDOWNTICKS = 100000;		// how many ticks to wait before reevaluating

            //NOTE: It's harder to read, but I'm keeping all the logic in a single method so that this runs as fast as possible

            object elapsedTime = _elapsedTime;
            if (elapsedTime != null)        // need to get an average duration between calls before setting value (or assumed elapsed time could be way off, and value could go wild)
            {
                #region Filter Input

                // Copied from Neuron_ZeroPos
                double sumModified = (sumInputs * 3d) - 5d;
                double filteredValue = 1d / (1d + Math.Pow(Math.E, -1d * sumModified));

                // Chop off small values
                if (filteredValue < _valueCutoff)
                {
                    filteredValue = 0d;
                }

                #endregion
                #region Determine K

                double currentValue = this.Value;

                double k = _kUp;
                if (filteredValue < currentValue)
                {
                    k = _kDown;
                }

                #endregion

                // Use newton's law of heating/cooling:
                // T = T0 + Tdelta * e^-kt
                double difference = filteredValue - currentValue;
                currentValue = currentValue + (difference * Math.Pow(Math.E, -1d * k * (double)elapsedTime));

                // Cap the values (not really needed, just being overly safe)
                if (currentValue < 0d)
                {
                    currentValue = 0d;
                }
                else if (currentValue > 1d)
                {
                    currentValue = 1d;
                }

                this.Value = currentValue;
            }

            // See if the pulse duration needs to be reevaluated
            if (Interlocked.Add(ref _countdownTillEval, -1) <= 0)
            {
                #region Determine elapsed time

                lock (_lock)
                {
                    if (_countdownTillEval <= 0)		// make sure some other thread didn't already finish taking samples.  Note that the lock doesn't fully protect this variable, but it should be good enough
                    {
                        DateTime now = DateTime.UtcNow;

                        if (_sampleCounter == 0)
                        {
                            // First time sampling.  Init
                            _startSampleTime = now;
                            _sampleCounter++;
                        }
                        else if (_sampleCounter > SAMPLECOUNT)
                        {
                            #region Finished taking samples

                            // Get the average time between ticks
                            _elapsedTime = (now - _startSampleTime).TotalMilliseconds / Convert.ToDouble(_sampleCounter);       // using milliseconds so K doesn't have to be so large

                            // Reset variables - need to reevaluate the duration every once in a while in case the timings change: maybe there is more or less
                            // load in the future, maybe the AI threading gets throttled back because of some environment config, maybe the machine decided
                            // to run the processor at 100%, etc
                            _sampleCounter = 0;
                            _countdownTillEval = COUNTDOWNTICKS + Convert.ToInt32(Math.Floor(StaticRandom.NextDouble() * COUNTDOWNTICKS));		// adding a bit of random so that all neurons aren't reevaluating at the same time

                            #endregion
                        }
                        else
                        {
                            // Count this tick
                            _sampleCounter++;
                        }
                    }
                }

                #endregion
            }
        }

        #endregion
    }

    #endregion

    #region Class: NeuralLinkDNA

    /// <summary>
    /// This holds a link between two neurons
    /// </summary>
    /// <remarks>
    /// NOTE: This class will be included in dna, so needs to be xaml serializable
    /// 
    /// The reason why there is a separate dna class from an implementation class, is the dna only holds positions, not actual neurons.
    /// This way, neurons can shift, split, merge between a parent and child brain, and not break anything.
    /// 
    /// This also allows similar links to be grouped together to reduce storage size
    /// </remarks>
    public class NeuralLinkDNA
    {
        // These are both in local neural coords
        public Point3D From
        {
            get;
            set;
        }
        public Point3D To
        {
            get;
            set;
        }

        public double Weight
        {
            get;
            set;
        }

        /// <summary>
        /// These are percents that are multiplied to weight.  The way it works, is the percents get averaged based on current chemicals in the brain.
        /// Then that averaged percent gets multiplied to weight.
        /// </summary>
        /// <remarks>
        /// I want to stay a bit loose about defining brain chemicals, and what each means.  So the size of this array could be zero, which means that
        /// the link is unaffected by any chemicals, or it could be 10 which would be affected by 10 different chemicals.
        /// 
        /// Even though a link may support 10 chemicals, a brain my only have 5, so the last 5 go unused.
        /// 
        /// Since the number of chemicals is arbitrary and loose, the first chemical will probably end up being very primitive (like a fear response, or
        /// hunger response).  But the chemicals shouldn't be classified by intended function, their function is determined by how the brain interprets
        /// them.
        /// </remarks>
        public double[] BrainChemicalModifiers
        {
            get;
            set;
        }
    }

    #endregion
    #region Class: NeuralLinkExternalDNA

    /// <summary>
    /// This is used to store external links
    /// </summary>
    /// <remarks>
    /// There is no need to store ToContainer, because that is the container that owns the dna that this class is stored in
    /// 
    /// I hate being so inheritance crazy, but tuples can't be serialized
    /// </remarks>
    public class NeuralLinkExternalDNA : NeuralLinkDNA
    {
        // These are the location and orientation of the part being linked to in ship coords (this is easy when the ship is a rigid body.
        // Once joints are introduced, there will need to be some kind of resting location for each part)
        public Point3D FromContainerPosition
        {
            get;
            set;
        }
        public Quaternion FromContainerOrientation
        {
            get;
            set;
        }
    }

    #endregion
    #region Class: NeuralLink

    public class NeuralLink
    {
        public NeuralLink(INeuronContainer fromContainer, INeuronContainer toContainer, INeuron from, INeuron to, double weight, double[] brainChemicalModifiers)
        {
            this.FromContainer = fromContainer;
            this.ToContainer = toContainer;
            this.From = from;
            this.To = to;
            this.Weight = weight;
            this.BrainChemicalModifiers = brainChemicalModifiers;
        }

        /// <summary>
        /// This provides a way to tell what container the From node is in (the links will be stored in a container,
        /// and the To node will always be part of that container)
        /// </summary>
        public readonly INeuronContainer FromContainer;
        public readonly INeuronContainer ToContainer;

        public readonly INeuron From;
        public readonly INeuron To;

        public readonly double Weight;

        // See NeuralLinkDNA for a description of this property
        public readonly double[] BrainChemicalModifiers;
    }

    #endregion

    #region Class: NeuralBucket

    /// <summary>
    /// This will process an arbitrary set of links
    /// </summary>
    /// <remarks>
    /// This class is meant to be run on an arbitrary thread, so the number of links passed to it should be a comfortable amount
    /// for a single tick.
    /// 
    /// This class doesn't care how many different ships own the neurons passed to it, just note that if one ship dies, a new instance
    /// of this class would need to be created so that neurons aren't processed wastefully.
    /// 
    /// The opposite could be done as well.  There could be several instances of this class processing different sets of neurons from
    /// a single ship.  This would enable multiple threads to run a single brain (but would only make sense if that ship has enough
    /// complexity to justify so much processing power)
    /// 
    /// Also note that there is a small cost in the constructor to group links by output neuron.
    /// </remarks>
    public class NeuralBucket
    {
        #region Class: NeuronBackPointer

        /// <summary>
        /// This holds a reference to a neuron, and all the neurons that feed it
        /// </summary>
        private class NeuronBackPointer
        {
            public NeuronBackPointer(INeuron neuron, INeuronContainer container, NeuralLink[] links)
            {
                this.Neuron = neuron;
                this.Container = container;
                this.Links = links;
            }

            public readonly INeuron Neuron;
            public readonly INeuronContainer Container;

            // These arrays are the same size (may want a single array of tuples, but that seems like a lot of unnessassary tuple instances created)
            public readonly NeuralLink[] Links;
        }

        #endregion

        #region Declaration Section

        /// <summary>
        /// This is the links grouped by To neuron
        /// </summary>
        private readonly NeuronBackPointer[] _neurons;

        /// <summary>
        /// This is the same size as _neurons, and is used to process them in a random order each tick (it's stored as a member
        /// level variable so the array only needs to be allocated once)
        /// </summary>
        /// <remarks>
        /// NOTE: UtilityHelper.RandomRange has this same functionality, but would need to rebuild this array each time it's
        /// called.  So this logic is duplicated to save the processor (I'm guessing that profiling will show this.Tick to be one of
        /// the bigger hits, so any savings will really pay off)
        /// </remarks>
        private int[] _indices = null;

        #endregion

        #region Constructor

        public NeuralBucket(NeuralLink[] links)
        {
            // Group the links up by the output neuron, and feeder neurons
            _neurons = links
                .GroupBy(o => o.To)
                .Select(o => new NeuronBackPointer(o.Key, o.First().ToContainer, o.ToArray()))
                .ToArray();

            // Just create it.  It will be populated each tick
            _indices = new int[_neurons.Length];

            this.Count = _neurons.Length;
        }

        #endregion

        #region Public Properties

        public readonly int Count;
        public readonly long Token = TokenGenerator.NextToken();

        #endregion

        #region Public Methods

        public void Tick()
        {
            // Reset the indices
            for (int cntr = 0; cntr < _neurons.Length; cntr++)
            {
                _indices[cntr] = cntr;
            }

            Random rand = StaticRandom.GetRandomForThread();

            for (int cntr = _neurons.Length - 1; cntr >= 0; cntr--)
            {
                // Come up with a random neuron
                int index1 = rand.Next(cntr + 1);
                int index2 = _indices[index1];
                _indices[index1] = _indices[cntr];

                // Check if the container is switched off
                if (!_neurons[index2].Container.IsOn)
                {
                    _neurons[index2].Neuron.SetValue(0d);
                    continue;
                }

                double weight = 0;

                // Add up the input neuron weights
                foreach (var link in _neurons[index2].Links)
                {
                    if (!link.FromContainer.IsOn)
                    {
                        continue;
                    }

                    #region Add up brain chemicals

                    //TODO: Fix this.  Starting at one means that it takes more negative modifiers to bring the multiplier negative.
                    //But starting at zero means that if there are no brain chemicals, the multiplier will be zero, and this link will be dead

                    double brainChemicalMultiplier = 1d;		// defaulting to 1 so that if there are no brain chemicals, it multiplies by one

                    if (link.BrainChemicalModifiers != null && link.FromContainer is Brain)		// for now, the only way to be in the presence of brain chemicals is if the from neuron is inside a brain
                    {
                        Brain brain = (Brain)link.FromContainer;

                        // Add up the brain chemicals
                        for (int i = 0; i < link.BrainChemicalModifiers.Length; i++)
                        {
                            brainChemicalMultiplier += link.BrainChemicalModifiers[i] * brain.GetBrainChemicalValue(i);		// brain just returns zero for chemicals it doesn't have
                        }
                    }

                    #endregion

                    weight += link.From.Value * link.Weight * brainChemicalMultiplier;
                }

                _neurons[index2].Neuron.SetValue(weight);
            }
        }

        #endregion
    }

    #endregion
    #region Class: NeuralPool

    public class NeuralPool
    {
        #region Class: TaskWrapper

        /// <summary>
        /// This is a single thread
        /// </summary>
        private class TaskWrapper : IDisposable
        {
            #region Declaration Section

            private readonly CancellationTokenSource _cancel = new CancellationTokenSource();

            private Task _task = null;

            #endregion

            #region Constructor

            public TaskWrapper()
            {
                // Create the task (it just runs forever until dispose is called)
                _task = Task.Factory.StartNew(() =>
                {
                    Run(this, _cancel.Token);
                }, _cancel.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }

            #endregion

            #region IDisposable Members

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    // Cancel
                    _cancel.Cancel();
                    try
                    {
                        _task.Wait();
                    }
                    catch (Exception) { }

                    // Clean up
                    _task.Dispose();
                    _task = null;
                }
            }

            #endregion

            #region Public Properties

            //True=Add, False=Remove
            public readonly ConcurrentQueue<Tuple<NeuralBucket, bool>> AddRemoves = new ConcurrentQueue<Tuple<NeuralBucket, bool>>();

            private volatile int _count = 0;
            /// <summary>
            /// This is how many items are contained in this worker (helpful for load balancing)
            /// </summary>
            /// <remarks>
            /// NOTE: This is the sum of each NeuralOperation.Count, not the number of NeuralWorkers
            /// </remarks>
            public int Count
            {
                get
                {
                    return _count;
                }
            }

            #endregion

            #region Private Methods

            private static void Run(TaskWrapper parent, CancellationToken cancel)
            {
                //NOTE: This method is running on an arbitrary thread
                try
                {
                    List<NeuralBucket> buckets = new List<NeuralBucket>();

                    while (!cancel.IsCancellationRequested)
                    {
                        #region Add/Remove buckets

                        bool wasListChanged = false;

                        Tuple<NeuralBucket, bool> bucket;
                        while (parent.AddRemoves.TryDequeue(out bucket))
                        {
                            if (bucket.Item2)
                            {
                                buckets.Add(bucket.Item1);
                            }
                            else
                            {
                                buckets.Remove(bucket.Item1);
                            }

                            wasListChanged = true;
                        }

                        // Store the new count
                        if (wasListChanged)
                        {
                            parent._count = buckets.Sum(o => o.Count);
                        }

                        #endregion

                        if (buckets.Count == 0)
                        {
                            // Hang out for a bit, then try again.  No need to burn up the processor
                            Thread.Sleep(450 + StaticRandom.Next(100));
                            continue;
                        }

                        #region Process buckets

                        foreach (NeuralBucket bucket2 in buckets)
                        {
                            bucket2.Tick();
                        }

                        #endregion

                        Thread.Sleep(0);		// not sure if this is useful or not
                        //Thread.Yield();
                    }
                }
                catch (Exception)
                {
                    // Don't leak errors, just go away
                }
            }

            #endregion
        }

        #endregion

        #region Declaration Section

        private static readonly object _lockStatic = new object();
        private readonly object _lockInstance;

        /// <summary>
        /// The static constructor makes sure that this instance is created only once.  The outside users of this class
        /// call the static property Instance to get this one instance copy.  (then they can use the rest of the instance
        /// methods)
        /// </summary>
        private static NeuralPool _instance;

        /// <summary>
        /// Each of these has its own set of items that it's calling in a tight loop
        /// </summary>
        private TaskWrapper[] _tasks;

        private SortedList<long, Tuple<NeuralBucket, TaskWrapper>> _itemsByToken;

        #endregion

        #region Constructor

        /// <summary>
        /// Static constructor.  Called only once before the first time you use my static properties/methods.
        /// </summary>
        static NeuralPool()
        {
            lock (_lockStatic)
            {
                // If the instance version of this class hasn't been instantiated yet, then do so
                if (_instance == null)
                {
                    _instance = new NeuralPool();
                }
            }
        }
        /// <summary>
        /// Instance constructor.  This is called only once by one of the calls from my static constructor.
        /// </summary>
        private NeuralPool()
        {
            _lockInstance = new object();

            // Take a rough guess at how many to create
            int numTasks = Convert.ToInt32(Math.Floor(Environment.ProcessorCount * .5d));
            if (numTasks < 1)
            {
                numTasks = 1;
            }

            _itemsByToken = new SortedList<long, Tuple<NeuralBucket, TaskWrapper>>();

            // Create the tasks
            _tasks = new TaskWrapper[numTasks];
            for (int cntr = 0; cntr < numTasks; cntr++)
            {
                _tasks[cntr] = new TaskWrapper();
            }
        }

        /// <summary>
        /// This is how you get at my instance.  The act of calling this property guarantees that the static constructor gets called
        /// exactly once (per process?)
        /// </summary>
        public static NeuralPool Instance
        {
            get
            {
                // There is no need to check the static lock, because _instance is only set one time, and that is guaranteed to be
                // finished before this function gets called
                return _instance;
            }
        }

        #endregion

        #region Public Properties

        public int NumThreads
        {
            get
            {
                lock (_lockInstance)
                {
                    return _tasks.Length;
                }
            }
            set
            {
                lock (_lockInstance)
                {
                    if (_tasks.Length == value)
                    {
                        return;
                    }

                    #region Pop existing

                    //NOTE: One at a time is a bit inneficient, but calling resize should be rare

                    // Pop all the items out of the tasks that are going to be removed
                    List<NeuralBucket> orphans = new List<NeuralBucket>();

                    foreach (var existing in _itemsByToken.Values.ToArray())		// using toarray, because remove will modify _itemsByToken
                    {
                        RemovePrivate(existing.Item1);
                        orphans.Add(existing.Item1);
                    }

                    #endregion

                    if (_tasks.Length < value)
                    {
                        #region Increase

                        TaskWrapper[] newArray = new TaskWrapper[value];

                        // Copy the existing tasks into the larger array
                        Array.Copy(_tasks, newArray, _tasks.Length);

                        // Create new tasks in the extra slots
                        for (int cntr = _tasks.Length; cntr < value; cntr++)
                        {
                            newArray[cntr] = new TaskWrapper();
                        }

                        // Swap arrays
                        _tasks = newArray;

                        #endregion
                    }
                    else if (_tasks.Length > value)
                    {
                        #region Decrease

                        TaskWrapper[] newArray = new TaskWrapper[value];

                        // Copy the existing tasks into the smaller array
                        Array.Copy(_tasks, newArray, value);

                        // Dispose the rest
                        for (int cntr = value; cntr < _tasks.Length; cntr++)
                        {
                            _tasks[cntr].Dispose();
                        }

                        // Swap arrays
                        _tasks = newArray;

                        #endregion
                    }

                    #region Redistribute

                    foreach (NeuralBucket orphan in orphans)
                    {
                        AddPrivate(orphan);
                    }

                    #endregion
                }
            }
        }

        #endregion

        #region Public Methods

        public void Add(NeuralBucket bucket)
        {
            lock (_lockInstance)
            {
                AddPrivate(bucket);
            }
        }
        public void Remove(NeuralBucket bucket)
        {
            lock (_lockInstance)
            {
                RemovePrivate(bucket);
            }
        }

        // This is just for debugging
        public void GetStats(out int numBuckets, out int numLinks)
        {
            lock (_lockInstance)
            {
                numBuckets = _itemsByToken.Count;
                numLinks = _itemsByToken.Values.Sum(o => o.Item1.Count);
            }
        }

        #endregion

        #region Private Methods

        private void AddPrivate(NeuralBucket bucket)
        {
            if (_itemsByToken.ContainsKey(bucket.Token))
            {
                throw new ArgumentException("This bucket has already been added");
            }

            // Find the wrapper with the least to do
            TaskWrapper wrapper = _tasks.
                Select(o => new { Count = o.Count, Wrapper = o }).
                OrderBy(o => o.Count).
                First().Wrapper;

            // Add to the wrapper
            wrapper.AddRemoves.Enqueue(Tuple.Create(bucket, true));

            // Remember where it is
            _itemsByToken.Add(bucket.Token, Tuple.Create(bucket, wrapper));
        }
        private void RemovePrivate(NeuralBucket bucket)
        {
            if (!_itemsByToken.ContainsKey(bucket.Token))
            {
                throw new ArgumentException("This bucket was never added");
            }

            TaskWrapper wrapper = _itemsByToken[bucket.Token].Item2;

            // Remove from the wrapper
            wrapper.AddRemoves.Enqueue(Tuple.Create(bucket, false));

            // Forget where it was
            _itemsByToken.Remove(bucket.Token);
        }

        #endregion
    }

    #endregion

    #region Class: NeuralUtility

    public static class NeuralUtility
    {
        #region Enum: ExternalLinkRatioCalcType

        /// <summary>
        /// When calculating how many links to build between neuron containers, this tells what values to look at
        /// In other words take the number of neuron in which container(s) * some ratio
        /// </summary>
        public enum ExternalLinkRatioCalcType
        {
            /// <summary>
            /// Use the number of neurons from whichever container has fewer neurons
            /// </summary>
            Smallest,
            /// <summary>
            /// Use the number of neurons from whichever container has the most neurons
            /// </summary>
            Largest,
            /// <summary>
            /// Take the average of the two container's neurons
            /// </summary>
            Average,
            /// <summary>
            /// Use the number of neurons in the source container
            /// </summary>
            Source,
            /// <summary>
            /// Use the number of neurons in the destination container
            /// </summary>
            Destination
        }

        #endregion
        #region Class: ContainerInput

        public class ContainerInput
        {
            public ContainerInput(long token, INeuronContainer container, NeuronContainerType containerType, Point3D position, Quaternion orientation, double? internalRatio, Tuple<NeuronContainerType, ExternalLinkRatioCalcType, double>[] externalRatios, int brainChemicalCount, NeuralLinkDNA[] internalLinks, NeuralLinkExternalDNA[] externalLinks)
            {
                this.Token = token;
                this.Container = container;
                this.ContainerType = containerType;
                this.Position = position;
                this.Orientation = orientation;
                this.InternalRatio = internalRatio;
                this.ExternalRatios = externalRatios;
                this.BrainChemicalCount = brainChemicalCount;
                this.InternalLinks = internalLinks;
                this.ExternalLinks = externalLinks;
            }

            /// <summary>
            /// This should come from the PartBase
            /// </summary>
            public readonly long Token;

            public readonly INeuronContainer Container;
            public readonly NeuronContainerType ContainerType;

            public readonly Point3D Position;
            public readonly Quaternion Orientation;

            /// <summary>
            /// This is how many internal links to actually build.  It is calculated as the number of neurons * ratio
            /// </summary>
            public readonly double? InternalRatio;
            /// <summary>
            /// This is how many links should be built from the perspective of this container (how many listener links to create)
            /// It is broken down by container type
            /// </summary>
            public readonly Tuple<NeuronContainerType, ExternalLinkRatioCalcType, double>[] ExternalRatios;

            /// <summary>
            /// This is how many brain chemicals this container can support
            /// </summary>
            /// <remarks>
            /// This is just used to calculate brain chemical listeners off of links.  So this value doesn't have to be the literal count:
            ///		It could stay zero if you don't want links to have brain chemical listeners.
            ///		It could be double the actual count, and links would end up with more listeners than needed.
            ///		etc
            ///		
            /// Also, when creating a link, it won't always have this number of listeners, it will have random up to this number.
            /// </remarks>
            public readonly int BrainChemicalCount;

            //NOTE: It's ok if either of these two are null.  These are just treated as suggestions
            public readonly NeuralLinkDNA[] InternalLinks;
            public readonly NeuralLinkExternalDNA[] ExternalLinks;
        }

        #endregion
        #region Class: ContainerOutput

        public class ContainerOutput
        {
            public ContainerOutput(INeuronContainer container, NeuralLink[] internalLinks, NeuralLink[] externalLinks)
            {
                this.Container = container;
                this.InternalLinks = internalLinks;
                this.ExternalLinks = externalLinks;
            }

            public readonly INeuronContainer Container;
            //public readonly Point3D Position;		// there should be no reason to store position
            public readonly NeuralLink[] InternalLinks;
            public readonly NeuralLink[] ExternalLinks;
        }

        #endregion

        #region Class: LinkIndexed

        public class LinkIndexed
        {
            public LinkIndexed(int from, int to, double weight, double[] brainChemicalModifiers)
            {
                this.From = from;
                this.To = to;
                this.Weight = weight;
                this.BrainChemicalModifiers = brainChemicalModifiers;
            }

            public readonly int From;
            public readonly int To;
            public readonly double Weight;
            public readonly double[] BrainChemicalModifiers;
        }

        #endregion
        #region Class: ContainerPoints

        private class ContainerPoints
        {
            public ContainerPoints(ContainerInput container, int maxLinks)
            {
                this.Container = container;
                _maxLinks = maxLinks;
                this.AllNeurons = container.Container.Neruons_All.ToArray();
                this.AllNeuronPositions = this.AllNeurons.Select(o => o.Position).ToArray();		//TODO: transform by orientation
            }

            public readonly ContainerInput Container;
            public readonly INeuron[] AllNeurons;
            public readonly Point3D[] AllNeuronPositions;

            private readonly int _maxLinks;
            private Dictionary<Point3D, ClosestExistingResult[]> _nearestPoints = new Dictionary<Point3D, ClosestExistingResult[]>();

            public ClosestExistingResult[] GetNearestPoints(Point3D position)
            {
                //TODO: transform position by orientation
                //TODO: Take orientation into account:  I think rotate each part's neurons by orientation

                if (!_nearestPoints.ContainsKey(position))
                {
                    _nearestPoints.Add(position, GetClosestExisting(position, this.AllNeuronPositions, _maxLinks));
                }

                return _nearestPoints[position];
            }
        }

        #endregion
        #region Class: ClosestExistingResult

        private class ClosestExistingResult
        {
            public ClosestExistingResult(bool isExactMatch, int index, double percent)
            {
                this.IsExactMatch = isExactMatch;
                this.Index = index;
                this.Percent = percent;
            }

            public readonly bool IsExactMatch;
            public readonly int Index;
            public readonly double Percent;
        }

        #endregion
        #region Class: HighestPercentResult

        private class HighestPercentResult
        {
            public HighestPercentResult(ClosestExistingResult from, ClosestExistingResult to, double percent)
            {
                this.From = from;
                this.To = to;
                this.Percent = percent;
            }

            public readonly ClosestExistingResult From;
            public readonly ClosestExistingResult To;
            public readonly double Percent;
        }

        #endregion

        /// <summary>
        /// After creating your ship, this method will wire up neurons
        /// </summary>
        /// <remarks>
        /// This will create random links if the link dna is null.  Or it will use the link dna (and prune if there are too many)
        /// </remarks>
        /// <param name="maxWeight">This is only used when creating random links</param>
        public static ContainerOutput[] LinkNeurons(ContainerInput[] containers, double maxWeight)
        {
            //TODO: Take these as params (these are used when hooking up existing links)
            const int MAXINTERMEDIATELINKS = 3;
            const int MAXFINALLINKS = 3;

            NeuralLink[][] internalLinks, externalLinks;
            if (containers.Any(o => o.ExternalLinks != null || o.InternalLinks != null))
            {
                internalLinks = BuildInternalLinksExisting(containers, MAXINTERMEDIATELINKS, MAXFINALLINKS);
                externalLinks = BuildExternalLinksExisting(containers, MAXINTERMEDIATELINKS, MAXFINALLINKS);

                internalLinks = CapWeights(internalLinks, maxWeight);
                externalLinks = CapWeights(externalLinks, maxWeight);
            }
            else
            {
                internalLinks = BuildInternalLinksRandom(containers, maxWeight);
                externalLinks = BuildExternalLinksRandom(containers, maxWeight);
            }

            // Build the return
            ContainerOutput[] retVal = new ContainerOutput[containers.Length];
            for (int cntr = 0; cntr < containers.Length; cntr++)
            {
                retVal[cntr] = new ContainerOutput(containers[cntr].Container, internalLinks[cntr], externalLinks[cntr]);
            }

            // Exit Function
            return retVal;
        }
        /// <summary>
        /// This overload takes a map that tells which parts can link to which
        /// </summary>
        public static ContainerOutput[] LinkNeurons(BotConstruction_PartMap partMap, ContainerInput[] containers, double maxWeight)
        {
            //TODO: Take these as params (these are used when hooking up existing links)
            const int MAXINTERMEDIATELINKS = 3;
            const int MAXFINALLINKS = 3;

            NeuralLink[][] internalLinks, externalLinks;
            if (containers.Any(o => o.ExternalLinks != null || o.InternalLinks != null))
            {
                internalLinks = BuildInternalLinksExisting(containers, MAXINTERMEDIATELINKS, MAXFINALLINKS);
                externalLinks = BuildExternalLinksExisting(containers, MAXINTERMEDIATELINKS, MAXFINALLINKS);        //NOTE: The partMap isn't needed for existing links.  It is just to help figure out new random links

                internalLinks = CapWeights(internalLinks, maxWeight);
                externalLinks = CapWeights(externalLinks, maxWeight);
            }
            else
            {
                internalLinks = BuildInternalLinksRandom(containers, maxWeight);
                externalLinks = BuildExternalLinksRandom(partMap, containers, maxWeight);
            }

            // Build the return
            ContainerOutput[] retVal = new ContainerOutput[containers.Length];
            for (int cntr = 0; cntr < containers.Length; cntr++)
            {
                retVal[cntr] = new ContainerOutput(containers[cntr].Container, internalLinks[cntr], externalLinks[cntr]);
            }

            // Exit Function
            return retVal;
        }

        /// <summary>
        /// This is used when saving to DNA.
        /// Individual parts don't hold links, so when you call PartBase.GetNewDNA, the links will always be null.
        /// This populates dna.InternalLinks and dna.ExternalLinks based on the links stored in outputs.
        /// </summary>
        /// <param name="dna">This is the dna to populate</param>
        /// <param name="dnaSource">This is the container that the dna came from</param>
        /// <param name="outputs">This is all of the containers, and their links</param>
        public static void PopulateDNALinks(ShipPartDNA dna, INeuronContainer dnaSource, IEnumerable<ContainerOutput> outputs)
        {
            // Find the output for the source passed in
            ContainerOutput output = outputs.Where(o => o.Container == dnaSource).FirstOrDefault();
            if (output == null)
            {
                return;
            }

            // Internal
            dna.InternalLinks = null;
            if (output.InternalLinks != null)
            {
                dna.InternalLinks = output.InternalLinks.Select(o => new NeuralLinkDNA()
                    {
                        From = o.From.Position,
                        To = o.To.Position,
                        Weight = o.Weight,
                        BrainChemicalModifiers = o.BrainChemicalModifiers == null ? null : o.BrainChemicalModifiers.ToArray()		// using ToArray to make a copy
                    }).ToArray();
            }

            // External
            dna.ExternalLinks = null;
            if (output.ExternalLinks != null)
            {
                dna.ExternalLinks = output.ExternalLinks.Select(o => new NeuralLinkExternalDNA()
                    {
                        FromContainerPosition = o.FromContainer.Position,
                        FromContainerOrientation = o.FromContainer.Orientation,
                        From = o.From.Position,
                        To = o.To.Position,
                        Weight = o.Weight,
                        BrainChemicalModifiers = o.BrainChemicalModifiers == null ? null : o.BrainChemicalModifiers.ToArray()		// using ToArray to make a copy
                    }).ToArray();
            }
        }

        #region Private Methods - internal links

        /// <summary>
        /// This builds random links between neurons that are all in the same container
        /// </summary>
        private static NeuralLink[][] BuildInternalLinksRandom(ContainerInput[] containers, double maxWeight)
        {
            NeuralLink[][] retVal = new NeuralLink[containers.Length][];

            for (int cntr = 0; cntr < containers.Length; cntr++)
            {
                ContainerInput container = containers[cntr];

                if (container.InternalRatio == null)
                {
                    // If ratio is null, that means to not build any links
                    retVal[cntr] = null;
                    continue;
                }

                #region Separate into buckets

                // Separate into readable and writeable buckets
                List<INeuron> readable = new List<INeuron>();
                readable.AddRange(container.Container.Neruons_Readonly);
                int[] readonlyIndices = Enumerable.Range(0, readable.Count).ToArray();

                List<INeuron> writeable = new List<INeuron>();
                writeable.AddRange(container.Container.Neruons_Writeonly);
                int[] writeonlyIndices = Enumerable.Range(0, writeable.Count).ToArray();

                SortedList<int, int> readwritePairs = new SortedList<int, int>();		// storing illegal pairs so that neurons can't link to themselves (I don't know if they can in real life.  That's a tough term to search for, google gave far too generic answers)
                foreach (INeuron neuron in container.Container.Neruons_ReadWrite)
                {
                    readwritePairs.Add(readable.Count, writeable.Count);
                    readable.Add(neuron);
                    writeable.Add(neuron);
                }

                #endregion

                // Figure out how many to make
                int smallerNeuronCount = Math.Min(readable.Count, writeable.Count);
                int count = Convert.ToInt32(Math.Round(container.InternalRatio.Value * smallerNeuronCount));
                if (count == 0)
                {
                    // There are no links to create
                    retVal[cntr] = null;
                }

                // Create Random
                LinkIndexed[] links = GetRandomLinks(readable.Count, writeable.Count, count, readonlyIndices, writeonlyIndices, readwritePairs, maxWeight).		// get links
                    Select(o => new LinkIndexed(o.Item1, o.Item2, o.Item3, GetBrainChemicalModifiers(container, maxWeight))).       // tack on the brain chemical receptors
                    ToArray();

                // Exit Function
                retVal[cntr] = links.
                    Select(o => new NeuralLink(container.Container, container.Container, readable[o.From], writeable[o.To], o.Weight, o.BrainChemicalModifiers)).
                    ToArray();
            }

            // Exit Function
            return retVal;
        }

        /// <summary>
        /// This builds links between neurons that are all in the same container, based on exising links
        /// </summary>
        private static NeuralLink[][] BuildInternalLinksExisting(ContainerInput[] containers, int maxIntermediateLinks, int maxFinalLinks)
        {
            NeuralLink[][] retVal = new NeuralLink[containers.Length][];

            for (int cntr = 0; cntr < containers.Length; cntr++)
            {
                ContainerInput container = containers[cntr];

                if (container.InternalLinks == null || container.InternalLinks.Length == 0)
                {
                    // There are no existing internal links
                    retVal[cntr] = null;
                    continue;
                }

                //TODO: May want to use readable/writable instead of all (doing this first because it's easier).  Also, the ratios are good suggestions
                //for creating a good random brain, but from there, maybe the rules should be relaxed?
                INeuron[] allNeurons = container.Container.Neruons_All.ToArray();

                int count = Convert.ToInt32(Math.Round(container.InternalRatio.Value * allNeurons.Length));
                if (count == 0)
                {
                    retVal[cntr] = null;
                    continue;
                }

                // All the real work is done in this method
                LinkIndexed[] links = BuildInternalLinksExisting_Continue(allNeurons, count, container.InternalLinks, container.BrainChemicalCount, maxIntermediateLinks, maxFinalLinks);

                // Exit Function
                retVal[cntr] = links.Select(o => new NeuralLink(container.Container, container.Container, allNeurons[o.From], allNeurons[o.To], o.Weight, o.BrainChemicalModifiers)).ToArray();
            }

            // Exit Function
            return retVal;
        }

        /// <summary>
        /// This does a fuzzy match to find the best links it can
        /// </summary>
        /// <remarks>
        /// The existing links passed in point to where neurons used to be.  This method compares those positions with where
        /// neurons are now, and tries to match up with the closest it can
        /// 
        /// NOTE: Both neurons and links could have been mutated independent of each other.  This method doesn't care how
        /// they got the way they are, it just goes by position
        /// </remarks>
        private static LinkIndexed[] BuildInternalLinksExisting_Continue(IEnumerable<INeuron> neurons, int count, NeuralLinkDNA[] existing, int maxBrainChemicals, int maxIntermediateLinks, int maxFinalLinks)
        {
            NeuralLinkDNA[] existingPruned = existing;
            if (existing.Length > count)
            {
                // Prune without distributing weight (if this isn't done here, then the prune at the bottom of this method will
                // artificially inflate weights with the links that this step is removing)
                existingPruned = existing.OrderByDescending(o => Math.Abs(o.Weight)).Take(count).ToArray();
            }

            Point3D[] allPoints = neurons.Select(o => o.Position).ToArray();

            #region Find closest points

            // Get a unique list of points
            Dictionary<Point3D, ClosestExistingResult[]> resultsByPoint = new Dictionary<Point3D, ClosestExistingResult[]>();		// can't use SortedList, because point isn't sortable (probably doesn't have IComparable)
            foreach (var exist in existingPruned)
            {
                if (!resultsByPoint.ContainsKey(exist.From))
                {
                    resultsByPoint.Add(exist.From, GetClosestExisting(exist.From, allPoints, maxIntermediateLinks));
                }

                if (!resultsByPoint.ContainsKey(exist.To))
                {
                    resultsByPoint.Add(exist.To, GetClosestExisting(exist.To, allPoints, maxIntermediateLinks));
                }
            }

            #endregion

            List<LinkIndexed> retVal = new List<LinkIndexed>();

            #region Build links

            foreach (var exist in existingPruned)
            {
                HighestPercentResult[] links = GetHighestPercent(resultsByPoint[exist.From], resultsByPoint[exist.To], maxFinalLinks, true);

                foreach (HighestPercentResult link in links)
                {
                    double[] brainChemicals = null;
                    if (exist.BrainChemicalModifiers != null)
                    {
                        brainChemicals = exist.BrainChemicalModifiers.
                            Take(maxBrainChemicals).		// if there are more, just drop them
                            Select(o => o).
                            //Select(o => o * link.Percent).		// I decided not to multiply by percent.  The weight is already reduced, no point in double reducing
                            ToArray();
                    }

                    retVal.Add(new LinkIndexed(link.From.Index, link.To.Index, exist.Weight * link.Percent, brainChemicals));
                }
            }

            #endregion

            // Exit Function
            if (retVal.Count > count)
            {
                #region Prune

                // Prune the weakest links
                // Need to redistribute the lost weight (since this method divided links into smaller ones).  If I don't, then over many generations,
                // the links will tend toward zero

                retVal = retVal.OrderByDescending(o => Math.Abs(o.Weight)).ToList();

                LinkIndexed[] kept = retVal.Take(count).ToArray();
                LinkIndexed[] removed = retVal.Skip(count).ToArray();

                double keptSum = kept.Sum(o => Math.Abs(o.Weight));
                double removedSum = removed.Sum(o => Math.Abs(o.Weight));

                double ratio = keptSum / (keptSum + removedSum);
                ratio = 1d / ratio;

                return kept.Select(o => new LinkIndexed(o.From, o.To, o.Weight * ratio, o.BrainChemicalModifiers)).ToArray();

                #endregion
            }
            else
            {
                return retVal.ToArray();
            }
        }

        #endregion
        #region Private Methods - external links

        #region Random - map

        private static NeuralLink[][] BuildExternalLinksRandom(BotConstruction_PartMap partMap, ContainerInput[] containers, double maxWeight)
        {
            NeuralLink[][] retVal = new NeuralLink[containers.Length][];

            // Pull out the readable nodes from each container
            List<INeuron>[] readable = BuildExternalLinksRandom_FindReadable(containers);

            // Shoot through each container, and create links that feed it
            for (int cntr = 0; cntr < containers.Length; cntr++)
            {
                ContainerInput container = containers[cntr];

                if (container.ExternalRatios == null || container.ExternalRatios.Length == 0)
                {
                    // This container shouldn't be fed by other containers (it's probably a sensor)
                    retVal[cntr] = null;
                    continue;
                }

                // Find writable nodes
                List<INeuron> writeable = new List<INeuron>();
                writeable.AddRange(container.Container.Neruons_ReadWrite);
                writeable.AddRange(container.Container.Neruons_Writeonly);

                if (writeable.Count == 0)
                {
                    // There are no nodes that can be written
                    retVal[cntr] = null;
                    continue;
                }

                List<NeuralLink> links = new List<NeuralLink>();

                foreach (var ratio in container.ExternalRatios)
                {
                    // Link to this container type
                    links.AddRange(BuildExternalLinksRandom_Continue(partMap, containers, cntr, ratio, readable, writeable, maxWeight));
                }

                // Add links to the return jagged array
                if (links.Count == 0)
                {
                    retVal[cntr] = null;
                }
                else
                {
                    retVal[cntr] = links.ToArray();
                }
            }

            // Exit Function
            return retVal;
        }

        private static List<NeuralLink> BuildExternalLinksRandom_Continue(BotConstruction_PartMap partMap, ContainerInput[] containers, int currentIndex, Tuple<NeuronContainerType, ExternalLinkRatioCalcType, double> ratio, List<INeuron>[] readable, List<INeuron> writeable, double maxWeight)
        {
            List<NeuralLink> retVal = new List<NeuralLink>();

            // Find eligible containers
            var matchingInputs = BuildExternalLinksRandom_Continue_Eligible(partMap, ratio.Item1, currentIndex, containers, readable);
            if (matchingInputs.Count == 0)
            {
                return retVal;
            }

            // Add up all the eligible neurons
            int sourceNeuronCount = matchingInputs.Sum(o => o.Item2.Count);

            double multiplier = matchingInputs.Average(o => o.Item3);
            multiplier *= ratio.Item3;

            // Figure out how many to create (this is the total count.  Each feeder container will get a percent of these based on its ratio of
            // neurons compared to the other feeders)
            int count = BuildExternalLinks_Count(ratio.Item2, sourceNeuronCount, writeable.Count, multiplier);
            if (count == 0)
            {
                return retVal;
            }

            // I don't want to draw so evenly from all the containers.  Draw from all containers at once.  This will have more clumping, and some containers
            // could be completely skipped.
            //
            // My reasoning for this is manipulators like thrusters don't really make sense to be fed evenly from every single sensor.  Also, thruster's count is by
            // destination neuron, which is very small.  So fewer total links will be created by doing just one pass
            List<Tuple<int, int>> inputLookup = new List<Tuple<int, int>>();
            for (int outer = 0; outer < matchingInputs.Count; outer++)
            {
                for (int inner = 0; inner < matchingInputs[outer].Item2.Count; inner++)
                {
                    //Item1 = index into matchingInputs
                    //Item2 = index into neuron
                    inputLookup.Add(new Tuple<int, int>(outer, inner));
                }
            }

            // For now, just build completely random links
            //NOTE: This is ignoring the relative weights in the map passed in.  Those weights were used to calculate how many total links there should be
            Tuple<int, int, double>[] links = GetRandomLinks(inputLookup.Count, writeable.Count, count, Enumerable.Range(0, inputLookup.Count).ToArray(), Enumerable.Range(0, writeable.Count).ToArray(), new SortedList<int, int>(), maxWeight);

            foreach (var link in links)
            {
                // link.Item1 is the from neuron.  But all the inputs were put into a single list, so use the inputLookup to figure out which container/neuron
                // is being referenced
                var input = matchingInputs[inputLookup[link.Item1].Item1];
                int neuronIndex = inputLookup[link.Item1].Item2;

                double[] brainChemicals = GetBrainChemicalModifiers(input.Item1, maxWeight);		// the brain chemicals are always based on the from container (same in NeuralOperation.Tick)

                retVal.Add(new NeuralLink(input.Item1.Container, containers[currentIndex].Container, input.Item2[neuronIndex], writeable[link.Item2], link.Item3, brainChemicals));
            }

            // Exit Function
            return retVal;
        }

        /// <summary>
        /// Gets the readable neurons from the containers of the type requested (and skips the current container)
        /// </summary>
        private static List<Tuple<ContainerInput, List<INeuron>, double>> BuildExternalLinksRandom_Continue_Eligible(BotConstruction_PartMap partMap, NeuronContainerType containerType, int currentIndex, ContainerInput[] containers, List<INeuron>[] readable)
        {
            var retVal = new List<Tuple<ContainerInput, List<INeuron>, double>>();

            for (int cntr = 0; cntr < containers.Length; cntr++)
            {
                if (cntr == currentIndex)
                {
                    continue;
                }

                if (containers[cntr].ContainerType != containerType)
                {
                    continue;
                }

                double? weight = null;
                foreach (var mapped in partMap.Actual)
                {
                    if ((mapped.Item1.Token == containers[currentIndex].Token && mapped.Item2.Token == containers[cntr].Token) ||
                        (mapped.Item2.Token == containers[currentIndex].Token && mapped.Item1.Token == containers[cntr].Token))
                    {
                        weight = mapped.Item3;
                        break;
                    }
                }

                if (weight == null)
                {
                    // The map doesn't hold a link between these two containers
                    continue;
                }

                retVal.Add(Tuple.Create(containers[cntr], readable[cntr], weight.Value));
            }

            return retVal;
        }

        #endregion
        #region Random - old

        /// <summary>
        /// This builds links between neurons across containers
        /// </summary>
        private static NeuralLink[][] BuildExternalLinksRandom(ContainerInput[] containers, double maxWeight)
        {
            NeuralLink[][] retVal = new NeuralLink[containers.Length][];

            // Pull out the readable nodes from each container
            List<INeuron>[] readable = BuildExternalLinksRandom_FindReadable(containers);

            // Shoot through each container, and create links that feed it
            for (int cntr = 0; cntr < containers.Length; cntr++)
            {
                ContainerInput container = containers[cntr];

                if (container.ExternalRatios == null || container.ExternalRatios.Length == 0)
                {
                    // This container shouldn't be fed by other containers (it's probably a sensor)
                    retVal[cntr] = null;
                    continue;
                }

                // Find writable nodes
                List<INeuron> writeable = new List<INeuron>();
                writeable.AddRange(container.Container.Neruons_ReadWrite);
                writeable.AddRange(container.Container.Neruons_Writeonly);

                if (writeable.Count == 0)
                {
                    // There are no nodes that can be written
                    retVal[cntr] = null;
                    continue;
                }

                List<NeuralLink> links = new List<NeuralLink>();

                foreach (var ratio in container.ExternalRatios)
                {
                    // Link to this container type
                    links.AddRange(BuildExternalLinksRandom_Continue(containers, cntr, ratio, readable, writeable, maxWeight));
                }

                // Add links to the return jagged array
                if (links.Count == 0)
                {
                    retVal[cntr] = null;
                }
                else
                {
                    retVal[cntr] = links.ToArray();
                }
            }

            // Exit Function
            return retVal;
        }
        private static List<INeuron>[] BuildExternalLinksRandom_FindReadable(ContainerInput[] containers)
        {
            return containers.
                Select(o => o.Container.Neruons_Readonly.
                    Concat(o.Container.Neruons_ReadWrite).
                    ToList()).
                ToArray();
        }

        private static List<NeuralLink> BuildExternalLinksRandom_Continue(ContainerInput[] containers, int currentIndex, Tuple<NeuronContainerType, ExternalLinkRatioCalcType, double> ratio, List<INeuron>[] readable, List<INeuron> writeable, double maxWeight)
        {
            List<NeuralLink> retVal = new List<NeuralLink>();

            // Find eligible containers
            var matchingInputs = BuildExternalLinksRandom_Continue_Eligible(ratio.Item1, currentIndex, containers, readable);
            if (matchingInputs.Count == 0)
            {
                return retVal;
            }

            // Add up all the eligible neurons
            int sourceNeuronCount = matchingInputs.Sum(o => o.Item2.Count);

            // Figure out how many to create (this is the total count.  Each feeder container will get a percent of these based on its ratio of
            // neurons compared to the other feeders)
            int count = BuildExternalLinks_Count(ratio.Item2, sourceNeuronCount, writeable.Count, ratio.Item3);
            if (count == 0)
            {
                return retVal;
            }

            // I don't want to draw so evenly from all the containers.  Draw from all containers at once.  This will have more clumping, and some containers
            // could be completely skipped.
            //
            // My reasoning for this is manipulators like thrusters don't really make sense to be fed evenly from every single sensor.  Also, thruster's count is by
            // destination neuron, which is very small.  So fewer total links will be created by doing just one pass
            List<Tuple<int, int>> inputLookup = new List<Tuple<int, int>>();
            for (int outer = 0; outer < matchingInputs.Count; outer++)
            {
                for (int inner = 0; inner < matchingInputs[outer].Item2.Count; inner++)
                {
                    //Item1 = index into matchingInputs
                    //Item2 = index into neuron
                    inputLookup.Add(new Tuple<int, int>(outer, inner));
                }
            }

            // For now, just build completely random links
            Tuple<int, int, double>[] links = GetRandomLinks(inputLookup.Count, writeable.Count, count, Enumerable.Range(0, inputLookup.Count).ToArray(), Enumerable.Range(0, writeable.Count).ToArray(), new SortedList<int, int>(), maxWeight);

            foreach (var link in links)
            {
                // link.Item1 is the from neuron.  But all the inputs were put into a single list, so use the inputLookup to figure out which container/neuron
                // is being referenced
                var input = matchingInputs[inputLookup[link.Item1].Item1];
                int neuronIndex = inputLookup[link.Item1].Item2;

                double[] brainChemicals = GetBrainChemicalModifiers(input.Item1, maxWeight);		// the brain chemicals are always based on the from container (same in NeuralOperation.Tick)

                retVal.Add(new NeuralLink(input.Item1.Container, containers[currentIndex].Container, input.Item2[neuronIndex], writeable[link.Item2], link.Item3, brainChemicals));
            }

            // Exit Function
            return retVal;
        }

        /// <summary>
        /// Gets the readable neurons from the containers of the type requested (and skips the current container)
        /// </summary>
        private static List<Tuple<ContainerInput, List<INeuron>>> BuildExternalLinksRandom_Continue_Eligible(NeuronContainerType containerType, int currentIndex, ContainerInput[] containers, List<INeuron>[] readable)
        {
            List<Tuple<ContainerInput, List<INeuron>>> retVal = new List<Tuple<ContainerInput, List<INeuron>>>();

            for (int cntr = 0; cntr < containers.Length; cntr++)
            {
                if (cntr == currentIndex)
                {
                    continue;
                }

                if (containers[cntr].ContainerType != containerType)
                {
                    continue;
                }

                retVal.Add(new Tuple<ContainerInput, List<INeuron>>(containers[cntr], readable[cntr]));
            }

            return retVal;
        }

        #endregion
        #region Existing

        private static NeuralLink[][] BuildExternalLinksExisting(ContainerInput[] containers, int maxIntermediateLinks, int maxFinalLinks)
        {
            NeuralLink[][] retVal = new NeuralLink[containers.Length][];

            // Figure out which containers (parts) are closest to the containers[].ExternalLinks[].FromContainerPosition
            var partBreakdown = BuildExternalLinksExisting_ContainerPoints(containers, maxIntermediateLinks);

            // This gets added to as needed (avoids recalculating best matching neurons by position)
            Dictionary<ContainerInput, ContainerPoints> nearestNeurons = new Dictionary<ContainerInput, ContainerPoints>();

            for (int cntr = 0; cntr < containers.Length; cntr++)
            {
                ContainerInput container = containers[cntr];

                if (container.ExternalLinks == null || container.ExternalLinks.Length == 0)
                {
                    // There are no existing external links
                    retVal[cntr] = null;
                    continue;
                }

                List<NeuralLink> containerLinks = new List<NeuralLink>();

                // The external links are from shifting from containers, but the to container is always known (this container), so the to container
                // is always an array of one
                ClosestExistingResult[] toPart = new ClosestExistingResult[] { new ClosestExistingResult(true, cntr, 1d) };

                // Link part to part
                foreach (var exist in container.ExternalLinks)
                {
                    // Figure out which parts to draw from
                    HighestPercentResult[] partLinks = GetHighestPercent(partBreakdown[exist.FromContainerPosition], toPart, maxIntermediateLinks, true);

                    // Get links between neurons in between the matching parts
                    containerLinks.AddRange(BuildExternalLinksExisting_AcrossParts(exist, partLinks, containers, nearestNeurons, maxIntermediateLinks, maxFinalLinks));
                }

                // Prune
                containerLinks = BuildExternalLinksExisting_Prune(containerLinks, containers, container);

                retVal[cntr] = containerLinks.ToArray();
            }

            // Exit Function
            return retVal;
        }

        private static NeuralLink[] BuildExternalLinksExisting_AcrossParts(NeuralLinkExternalDNA dnaLink, HighestPercentResult[] partLinks, ContainerInput[] containers, Dictionary<ContainerInput, ContainerPoints> nearestNeurons, int maxIntermediateLinks, int maxFinalLinks)
        {
            List<NeuralLink> retVal = new List<NeuralLink>();

            foreach (HighestPercentResult partLink in partLinks)
            {
                #region Get containers

                ContainerInput fromContainer = containers[partLink.From.Index];
                if (!nearestNeurons.ContainsKey(fromContainer))
                {
                    nearestNeurons.Add(fromContainer, new ContainerPoints(fromContainer, maxIntermediateLinks));
                }
                ContainerPoints from = nearestNeurons[fromContainer];

                ContainerInput toContainer = containers[partLink.To.Index];
                if (!nearestNeurons.ContainsKey(toContainer))
                {
                    nearestNeurons.Add(toContainer, new ContainerPoints(toContainer, maxIntermediateLinks));
                }
                ContainerPoints to = nearestNeurons[toContainer];

                #endregion

                List<LinkIndexed> links = new List<LinkIndexed>();

                // Build links
                HighestPercentResult[] bestLinks = GetHighestPercent(from.GetNearestPoints(dnaLink.From), to.GetNearestPoints(dnaLink.To), maxIntermediateLinks, false);
                foreach (HighestPercentResult link in bestLinks)
                {
                    double[] brainChemicals = null;
                    if (dnaLink.BrainChemicalModifiers != null)
                    {
                        brainChemicals = dnaLink.BrainChemicalModifiers.
                            Take(fromContainer.BrainChemicalCount).		// if there are more, just drop them
                            Select(o => o).
                            //Select(o => o * link.Percent).		// I decided not to multiply by percent.  The weight is already reduced, no point in double reducing
                            ToArray();
                    }

                    links.Add(new LinkIndexed(link.From.Index, link.To.Index, dnaLink.Weight * link.Percent, brainChemicals));
                }

                // Convert the indices into return links
                retVal.AddRange(links.Select(o => new NeuralLink(fromContainer.Container, toContainer.Container, from.AllNeurons[o.From], to.AllNeurons[o.To], partLink.Percent * o.Weight, o.BrainChemicalModifiers)));
            }

            if (retVal.Count > maxFinalLinks)
            {
                // Prune and normalize percents
                var pruned = Prune(retVal.Select(o => o.Weight).ToArray(), maxFinalLinks);		// choose the top X weights, and tell what the new weight should be
                retVal = pruned.Select(o => new NeuralLink(retVal[o.Item1].FromContainer, retVal[o.Item1].ToContainer, retVal[o.Item1].From, retVal[o.Item1].To, o.Item2, retVal[o.Item1].BrainChemicalModifiers)).ToList();		// copy the referenced retVal items, but with the altered weights
            }

            // Exit Function
            return retVal.ToArray();
        }

        /// <summary>
        /// The containers may not be in the same place that the links think they are.  So this method goes through all the container positions
        /// that all the links point to, and figures out which containers are closest to those positions
        /// </summary>
        private static Dictionary<Point3D, ClosestExistingResult[]> BuildExternalLinksExisting_ContainerPoints(ContainerInput[] containers, int maxIntermediateLinks)
        {
            Dictionary<Point3D, ClosestExistingResult[]> retVal = new Dictionary<Point3D, ClosestExistingResult[]>();		// can't use SortedList, because point isn't sortable (probably doesn't have IComparable)

            Point3D[] allPartPoints = containers.Select(o => o.Position).ToArray();

            foreach (ContainerInput container in containers.Where(o => o.ExternalLinks != null && o.ExternalLinks.Length > 0))
            {
                // Get a unique list of referenced parts
                foreach (var exist in container.ExternalLinks)
                {
                    if (!retVal.ContainsKey(exist.FromContainerPosition))
                    {
                        retVal.Add(exist.FromContainerPosition, GetClosestExisting(exist.FromContainerPosition, allPartPoints, maxIntermediateLinks));
                    }
                }
            }

            // Exit Function
            return retVal;
        }

        private static List<NeuralLink> BuildExternalLinksExisting_Prune(List<NeuralLink> links, ContainerInput[] containers, ContainerInput toContainer)
        {
            List<NeuralLink> retVal = new List<NeuralLink>();

            // Process each from container separately
            foreach (var group in links.GroupBy(o => o.FromContainer))
            {
                ContainerInput fromContainer = containers.Where(o => o.Container == group.Key).First();

                // Find the ratio that talks about this from container
                var ratio = toContainer.ExternalRatios.Where(o => o.Item1 == fromContainer.ContainerType).FirstOrDefault();
                if (ratio == null)
                {
                    // Links aren't allowed between these types of containers
                    continue;
                }

                NeuralLink[] groupLinks = group.ToArray();

                // Figure out how many links are supported
                int maxCount = BuildExternalLinks_Count(ratio.Item2, fromContainer.Container.Neruons_All.Count(), toContainer.Container.Neruons_All.Count(), ratio.Item3);
                if (groupLinks.Length <= maxCount)
                {
                    // No need to prune, keep all of these
                    retVal.AddRange(groupLinks);
                }
                else
                {
                    // Prune
                    var pruned = Prune(groupLinks.Select(o => o.Weight).ToArray(), maxCount);		// get the largest weights, and normalize them
                    retVal.AddRange(pruned.Select(o => new NeuralLink(groupLinks[o.Item1].FromContainer, groupLinks[o.Item1].ToContainer, groupLinks[o.Item1].From, groupLinks[o.Item1].To, o.Item2, groupLinks[o.Item1].BrainChemicalModifiers)));		// copy the referenced links, but use the altered weights
                }
            }

            // Exit Function
            return retVal;
        }

        #endregion

        /// <summary>
        /// Figures out how many links to create based on the number of neurons
        /// </summary>
        private static int BuildExternalLinks_Count(ExternalLinkRatioCalcType calculationType, int sourceNeuronCount, int destinationNeuronCount, double ratio)
        {
            double retVal;

            switch (calculationType)
            {
                case ExternalLinkRatioCalcType.Smallest:
                    int smallerNeuronCount = Math.Min(sourceNeuronCount, destinationNeuronCount);
                    retVal = smallerNeuronCount;
                    break;

                case ExternalLinkRatioCalcType.Largest:
                    int largerNeuronCount = Math.Max(sourceNeuronCount, destinationNeuronCount);
                    retVal = largerNeuronCount;
                    break;

                case ExternalLinkRatioCalcType.Average:
                    double averageNeuronCount = Math.Round((sourceNeuronCount + destinationNeuronCount) / 2d);
                    retVal = averageNeuronCount;
                    break;

                case ExternalLinkRatioCalcType.Source:
                    retVal = sourceNeuronCount;
                    break;

                case ExternalLinkRatioCalcType.Destination:
                    retVal = destinationNeuronCount;
                    break;

                default:
                    throw new ApplicationException("Unknown ExternalLinkRatioCalcType: " + calculationType.ToString());
            }

            return (retVal * ratio).ToInt_Round();
        }

        #endregion
        #region Private Methods - random

        /// <summary>
        /// This comes up with completely random links
        /// NOTE: readwritePairs means neurons that are in both read and write lists (not nessassarily INeuronContainer.Neruons_ReadWrite)
        /// </summary>
        private static Tuple<int, int, double>[] GetRandomLinks(int fromCount, int toCount, int count, int[] readonlyIndices, int[] writeonlyIndices, SortedList<int, int> readwritePairs, double maxWeight)
        {
            // See how many links are possible
            int possibleCount = GetPossibilityCount(fromCount, toCount, readwritePairs.Count);

            List<Tuple<int, int>> possibleLinks = null;

            if (count > possibleCount)
            {
                #region Build all possible

                // Return all possible links (but still give them random weights)
                possibleLinks = GetAllPossibleLinks(fromCount, toCount, readonlyIndices, writeonlyIndices, readwritePairs);

                #endregion
            }
            else if (count > possibleCount / 2)
            {
                #region Reduce from all possible

                // Build a list of all possible links, then choose random ones out of that list (more efficient than having a while loop throwing out dupes)
                possibleLinks = GetAllPossibleLinks(fromCount, toCount, readonlyIndices, writeonlyIndices, readwritePairs);

                Random rand = StaticRandom.GetRandomForThread();

                // Remove random links until possible links has the correct number of links
                while (possibleLinks.Count > count)
                {
                    possibleLinks.RemoveAt(rand.Next(possibleLinks.Count));
                }

                #endregion
            }
            else
            {
                #region Pick random links

                possibleLinks = new List<Tuple<int, int>>();

                Random rand = StaticRandom.GetRandomForThread();

#if DEBUG
                int numIterations = 0;
#endif
                while (possibleLinks.Count < count)
                {
#if DEBUG
                    numIterations++;
#endif

                    // Make a random attempt
                    Tuple<int, int> link = new Tuple<int, int>(rand.Next(fromCount), rand.Next(toCount));

                    if (readwritePairs.ContainsKey(link.Item1) && readwritePairs[link.Item1] == link.Item2)
                    {
                        // This represents the same node (can't have nodes pointing to themselves)
                        continue;
                    }

                    if (possibleLinks.Contains(link))
                    {
                        // This link was already used
                        continue;
                    }

                    possibleLinks.Add(link);
                }

                #endregion
            }

            // Assign random weights
            return possibleLinks.
                Select(o => Tuple.Create(o.Item1, o.Item2, Math1D.GetNearZeroValue(maxWeight))).
                ToArray();
        }
        private static Tuple<int, int, double>[] GetRandomLinks_Weight(IEnumerable<Tuple<int, int>> links, double maxWeight)
        {
            List<Tuple<int, int, double>> retVal = new List<Tuple<int, int, double>>();

            foreach (var link in links)
            {
                retVal.Add(new Tuple<int, int, double>(link.Item1, link.Item2, Math1D.GetNearZeroValue(maxWeight)));
            }

            return retVal.ToArray();
        }

        /// <summary>
        /// This returns how many links would be created if every node were connected to every other node
        /// </summary>
        private static int GetPossibilityCount(int fromCount, int toCount, int sharedCount)
        {
            // Both from and to have the same shared items, so subtract that off
            int readCount = fromCount - sharedCount;
            int writeCount = toCount - sharedCount;

            // Number of combinations between readonly and writeonly
            int retVal = readCount * writeCount;

            if (sharedCount > 0)
            {
                // Include links from read to readwrite, and links from readwrite to write
                retVal += readCount * sharedCount;
                retVal += sharedCount * writeCount;

                // Number of combinations of readwrite
                retVal += (sharedCount * (sharedCount - 1)) / 2;		// it should be safe to leave this as integer division, because the multiplication will always produce an even number
            }

            return retVal;
        }

        private static List<Tuple<int, int>> GetAllPossibleLinks(int fromCount, int toCount, int[] readonlyIndices, int[] writeonlyIndices, SortedList<int, int> readwritePairs)
        {
            List<Tuple<int, int>> retVal = new List<Tuple<int, int>>();

            #region readonly -> writeonly

            if (writeonlyIndices.Length > 0)
            {
                for (int outer = 0; outer < readonlyIndices.Length; outer++)
                {
                    for (int inner = 0; inner < writeonlyIndices.Length; inner++)
                    {
                        retVal.Add(new Tuple<int, int>(readonlyIndices[outer], writeonlyIndices[inner]));
                    }
                }
            }

            #endregion

            #region readonly -> readwrite

            if (readwritePairs.Count > 0)
            {
                for (int outer = 0; outer < readonlyIndices.Length; outer++)
                {
                    for (int inner = 0; inner < readwritePairs.Count; inner++)
                    {
                        retVal.Add(new Tuple<int, int>(readonlyIndices[outer], readwritePairs.Values[inner]));
                    }
                }
            }

            #endregion

            #region readwrite -> writeonly

            if (writeonlyIndices.Length > 0)
            {
                for (int outer = 0; outer < readwritePairs.Count; outer++)
                {
                    for (int inner = 0; inner < writeonlyIndices.Length; inner++)
                    {
                        retVal.Add(new Tuple<int, int>(readwritePairs.Keys[outer], writeonlyIndices[inner]));
                    }
                }
            }

            #endregion

            #region readwrite -> readwrite

            for (int outer = 0; outer < readwritePairs.Count - 1; outer++)
            {
                for (int inner = outer + 1; inner < readwritePairs.Count; inner++)
                {
                    retVal.Add(new Tuple<int, int>(readwritePairs.Keys[outer], readwritePairs.Values[inner]));
                }
            }

            #endregion

            return retVal;
        }

        private static double[] GetBrainChemicalModifiers(ContainerInput container, double maxWeight)
        {
            if (container.BrainChemicalCount == 0)
            {
                return null;
            }

            int count = StaticRandom.Next(container.BrainChemicalCount);
            if (count == 0)
            {
                return null;
            }

            double[] retVal = new double[count];

            for (int cntr = 0; cntr < count; cntr++)
            {
                retVal[cntr] = Math1D.GetNearZeroValue(maxWeight);
            }

            return retVal;
        }

        #endregion
        #region Private Methods - existing

        private static ClosestExistingResult[] GetClosestExisting(Point3D search, Point3D[] points, int maxReturn)
        {
            const double SEARCHRADIUSMULT = 2.5d;		// looks at other nodes that are up to minradius * mult

            // Check for exact match
            int index = FindExact(search, points);
            if (index >= 0)
            {
                return new ClosestExistingResult[] { new ClosestExistingResult(true, index, 1d) };
            }

            // Get a list of nodes that are close to the search point
            var nearNodes = GetNearNodes(search, points, SEARCHRADIUSMULT);

            if (nearNodes.Count == 1)
            {
                // There's only one, so give it the full weight
                return new ClosestExistingResult[] { new ClosestExistingResult(false, nearNodes[0].Item1, 1d) };
            }

            // Don't allow too many divisions
            if (nearNodes.Count > maxReturn)
            {
                nearNodes = nearNodes.OrderBy(o => o.Item2).Take(maxReturn).ToList();
            }

            // Figure out what percent of the weight to give these nodes (based on the ratio of their distances to the search point)
            var percents = GetPercentOfWeight(nearNodes, SEARCHRADIUSMULT);

            // Exit Function
            return percents.Select(o => new ClosestExistingResult(false, o.Item1, o.Item2)).ToArray();
        }
        private static int FindExact(Point3D search, Point3D[] points)
        {
            for (int cntr = 0; cntr < points.Length; cntr++)
            {
                if (Math3D.IsNearValue(points[cntr], search))
                {
                    return cntr;
                }
            }

            return -1;
        }
        /// <summary>
        /// This returns the index and distance of the nodes that are close to search
        /// </summary>
        private static List<Tuple<int, double>> GetNearNodes(Point3D search, Point3D[] points, double searchRadiusMultiplier)
        {
            // Get the distances to each point
            double[] distSquared = points.Select(o => (o - search).LengthSquared).ToArray();

            // Find the smallest distance
            int smallestIndex = 0;
            for (int cntr = 1; cntr < distSquared.Length; cntr++)
            {
                if (distSquared[cntr] < distSquared[smallestIndex])
                {
                    smallestIndex = cntr;
                }
            }

            // Figure out how far out to allow
            double min = Math.Sqrt(distSquared[smallestIndex]);
            double maxSquared = Math.Pow(min * searchRadiusMultiplier, 2d);

            // Find all the points in range
            List<Tuple<int, double>> retVal = new List<Tuple<int, double>>();

            // This one is obviously in range (adding it now to avoid an unnessary sqrt)
            retVal.Add(new Tuple<int, double>(smallestIndex, min));

            for (int cntr = 0; cntr < distSquared.Length; cntr++)
            {
                if (cntr == smallestIndex)
                {
                    continue;
                }

                if (distSquared[cntr] < maxSquared)
                {
                    retVal.Add(new Tuple<int, double>(cntr, Math.Sqrt(distSquared[cntr])));
                }
            }

            // Exit Function
            return retVal;
        }
        /// <summary>
        /// This takes in a list of distances, and returns a list of percents (the int just comes along for the ride)
        /// </summary>
        private static List<Tuple<int, double>> GetPercentOfWeight(List<Tuple<int, double>> distances, double searchRadiusMultiplier)
        {
            const double OFFSET = .1d;

            // Find the smallest distance in the list
            double min = distances.Min(o => o.Item2);

            // Figure out what the maximum possible distance would be
            double maxRange = (min * searchRadiusMultiplier) - min;

            // Figure out ratios base on distance
            double[] ratios = new double[distances.Count];
            for (int cntr = 0; cntr < ratios.Length; cntr++)
            {
                // Normalize the distance
                ratios[cntr] = UtilityCore.GetScaledValue_Capped(0d, 1d, 0d, maxRange, distances[cntr].Item2 - min);

                // Run it through a function
                ratios[cntr] = 1d / (ratios[cntr] + OFFSET);		// need to add an offset, because one of these will be zero
            }

            double total = ratios.Sum();

            // Turn those ratios into percents (normalizing the ratios)
            List<Tuple<int, double>> retVal = new List<Tuple<int, double>>();
            for (int cntr = 0; cntr < ratios.Length; cntr++)
            {
                retVal.Add(new Tuple<int, double>(distances[cntr].Item1, ratios[cntr] / total));
            }

            // Exit Function
            return retVal;
        }

        private static HighestPercentResult[] GetHighestPercent(ClosestExistingResult[] from, ClosestExistingResult[] to, int maxReturn, bool isFromSameList)
        {
            // Find the combinations that have the highest percentage
            List<Tuple<int, int, double>> products = new List<Tuple<int, int, double>>();
            for (int fromCntr = 0; fromCntr < from.Length; fromCntr++)
            {
                for (int toCntr = 0; toCntr < to.Length; toCntr++)
                {
                    if (isFromSameList && from[fromCntr].Index == to[toCntr].Index)
                    {
                        continue;
                    }

                    products.Add(new Tuple<int, int, double>(fromCntr, toCntr, from[fromCntr].Percent * to[toCntr].Percent));
                }
            }

            // Don't return too many
            IEnumerable<Tuple<int, int, double>> topProducts = null;
            if (products.Count <= maxReturn)
            {
                topProducts = products;		// no need to sort or limit
            }
            else
            {
                topProducts = products.OrderByDescending(o => o.Item3).Take(maxReturn).ToArray();
            }

            // Normalize
            double totalPercent = topProducts.Sum(o => o.Item3);
            HighestPercentResult[] retVal = topProducts.Select(o => new HighestPercentResult(from[o.Item1], to[o.Item2], o.Item3 / totalPercent)).ToArray();

            return retVal;
        }

        /// <summary>
        /// This is meant to be a generic prune method.  It returns the top weights, and normalizes them so the sum of the smaller
        /// set is the same as the sum of the larger set
        /// </summary>
        private static Tuple<int, double>[] Prune(double[] weights, int count)
        {
            // Convert the weights into a list with the original index
            Tuple<int, double>[] weightsIndexed = new Tuple<int, double>[weights.Length];
            for (int cntr = 0; cntr < weights.Length; cntr++)
            {
                weightsIndexed[cntr] = new Tuple<int, double>(cntr, weights[cntr]);
            }

            if (count > weights.Length)
            {
                // This method shouldn't have been called, there is nothing to do
                return weightsIndexed;
            }

            Tuple<int, double>[] topWeights = weightsIndexed.OrderByDescending(o => Math.Abs(o.Item2)).Take(count).ToArray();

            double sumWeights = weights.Sum(o => Math.Abs(o));
            double sumTop = topWeights.Sum(o => Math.Abs(o.Item2));

            double ratio = sumWeights / sumTop;
            if (double.IsNaN(ratio))
            {
                ratio = 1d;		// probably divide by zero
            }

            // Normalize the top weights
            return topWeights.Select(o => new Tuple<int, double>(o.Item1, o.Item2 * ratio)).ToArray();
        }

        private static NeuralLink[][] CapWeights(NeuralLink[][] links, double maxWeight)
        {
            if (links == null)
            {
                return null;
            }

            NeuralLink[][] retVal = new NeuralLink[links.Length][];

            for (int outer = 0; outer < links.Length; outer++)
            {
                if (links[outer] == null)
                {
                    retVal[outer] = null;
                    continue;
                }

                retVal[outer] = links[outer].Select(o => new NeuralLink(o.FromContainer, o.ToContainer, o.From, o.To, CapWeights_Weight(o.Weight, maxWeight),
                    o.BrainChemicalModifiers == null ? null : o.BrainChemicalModifiers.Select(p => CapWeights_Weight(p, maxWeight)).ToArray())).ToArray();
            }

            return retVal;
        }
        private static double CapWeights_Weight(double weight, double max)
        {
            if (Math.Abs(weight) > max)
            {
                if (weight > 0)
                {
                    return max;
                }
                else
                {
                    return -max;
                }
            }
            else
            {
                return weight;
            }
        }

        #endregion
    }

    #endregion
}
