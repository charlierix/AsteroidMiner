using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v2.GameItems.ShipParts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

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

    #region enum: NeuronContainerType

    /// <summary>
    /// This allows more granular control of how many neural links to allow between types of containers
    /// </summary>
    public enum NeuronContainerType
    {
        /// <summary>
        /// May want to call this input
        /// </summary>
        Sensor,
        /// <summary>
        /// Probably only the original Brain class will be this.  Links between this and other parts will have random weights
        /// </summary>
        Brain_Standalone,
        /// <summary>
        /// This is a brain that contains extra hard coded machine learning algorithms.  Those internal neural nets read and
        /// write to the INeuronContainer's neurons.  Links between this and other parts will have a weight of 1 (it's up to
        /// the internal NN to generate meaningful weights)
        /// </summary>
        Brain_HasInternalNN,
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

    #region interface: INeuron

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
        double Value { get; }

        /// <summary>
        /// This is the position of the neuron in model space
        /// </summary>
        /// <remarks>
        /// The ship parts have positions in ship space.  The neuron's position isn't in that ship space, and isn't really constrained
        /// to the hosting part's size.  It's more of a virtual position, and only has meaning relative to the other neurons in the
        /// container.  Those positions should probably be normalized so that the farthest neuron has a radius of 1?
        /// </remarks>
        Point3D Position { get; }

        /// <summary>
        /// This is just here to help with drawing the neuron (pos only can be blue, neg/pos can be red/green)
        /// True: output goes from 0 to 1
        /// False: output goes from -1 to 1
        /// </summary>
        bool IsPositiveOnly { get; }

        long Token { get; }

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
    #region interface: INeuronContainer

    public interface INeuronContainer
    {
        /// <summary>
        /// Other parts can only read these neuron values.  Trying to set the values will have no effect on this part
        /// </summary>
        /// <remarks>
        /// This would be used for a sensor's output, or a traditional feed forward NN's output
        /// </remarks>
        IEnumerable<INeuron> Neruons_Readonly { get; }
        /// <summary>
        /// These serve as input and output.  External parts can set values and this part will also change these values
        /// </summary>
        IEnumerable<INeuron> Neruons_ReadWrite { get; }
        /// <summary>
        /// Other parts can only write to these neurons.  (you could read the values, but this part doesn't change those values)
        /// </summary>
        /// <remarks>
        /// This would be used for a manipulator's inputs, or a feed forward NN's input
        /// </remarks>
        IEnumerable<INeuron> Neruons_Writeonly { get; }

        /// <summary>
        /// This needs to always be the combination of the three specialized lists (no extras or omissions)
        /// </summary>
        IEnumerable<INeuron> Neruons_All { get; }

        Point3D Position { get; }
        Quaternion Orientation { get; }
        /// <summary>
        /// This is just a rough estimate of the size of this container.  It is only an assist for creating neuron visuals, it has no effect on
        /// where actual neurons are placed.
        /// </summary>
        double Radius { get; }

        NeuronContainerType NeuronContainerType { get; }

        bool IsOn { get; }

        long Token { get; }
    }

    #endregion

    #region class: Neuron_ZeroPos

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
        public Point3D Position => _position;

        public bool IsPositiveOnly => true;

        private readonly long _token = TokenGenerator.NextToken();
        public long Token => _token;

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

        public override string ToString()
        {
            return string.Format
            (
                "{0} | {1} | {2} | {3}",
                IsPositiveOnly ? "positive" : "pos neg",
                Value.ToStringSignificantDigits(2),
                Position.ToStringSignificantDigits(3),
                Token
            );
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
    #region class: Neuron_NegPos

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
        public Point3D Position => _position;

        public bool IsPositiveOnly => false;

        private readonly long _token = TokenGenerator.NextToken();
        public long Token => _token;

        public void SetValue(double sumInputs)
        {
            // Manually inlined Transform_S_NegPos
            double e2x = Math.Pow(Math.E, 2d * sumInputs);
            _value = (e2x - 1d) / (e2x + 1d);
        }

        public override string ToString()
        {
            return string.Format
            (
                "{0} | {1} | {2} | {3}",
                IsPositiveOnly ? "positive" : "pos neg",
                Value.ToStringSignificantDigits(2),
                Position.ToStringSignificantDigits(3),
                Token
            );
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
    #region class: Neuron_Spike

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
        public Point3D Position => _position;

        public bool IsPositiveOnly => true;

        private readonly long _token = TokenGenerator.NextToken();
        public long Token => _token;

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

        public override string ToString()
        {
            return string.Format
            (
                "{0} | {1} | {2} | {3}",
                IsPositiveOnly ? "positive" : "pos neg",
                Value.ToStringSignificantDigits(2),
                Position.ToStringSignificantDigits(3),
                Token
            );
        }
    }

    #endregion
    #region class: Neuron_SensorPosition

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
        public Point3D Position => _position;

        private readonly long _token = TokenGenerator.NextToken();
        public long Token => _token;

        private readonly Vector3D? _positionUnit;		// this is null if the position is at zero
        public Vector3D? PositionUnit => _positionUnit;

        private readonly double _positionLength;
        public double PositionLength => _positionLength;

        private readonly bool _isPositiveOnly;
        public bool IsPositiveOnly => _isPositiveOnly;

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

        public override string ToString()
        {
            return string.Format
            (
                "{0} | {1} | {2} | {3}",
                IsPositiveOnly ? "positive" : "pos neg",
                Value.ToStringSignificantDigits(2),
                Position.ToStringSignificantDigits(3),
                Token
            );
        }
    }

    #endregion
    #region class: Neuron_Fade

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

        private readonly long _token = TokenGenerator.NextToken();
        public long Token => _token;

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

        public override string ToString()
        {
            return string.Format
            (
                "{0} | {1} | {2} | {3}",
                IsPositiveOnly ? "positive" : "pos neg",
                Value.ToStringSignificantDigits(2),
                Position.ToStringSignificantDigits(3),
                Token
            );
        }
    }

    #endregion
    #region class: Neuron_Direct

    /// <summary>
    /// This doesn't run the input through an activation function, it simply stores the value that came in.  The output is still capped to
    /// either -1 to 1, or 0 to 1
    /// </summary>
    /// <remarks>
    /// With the Neuron_ZeroPos, a single input with a value of 1 would cause an output of .1.  It would take an input of 3 to get
    /// close to an output of 1.  So Neuron_ZeroPos makes sense when there are multiple neurons feeding it, but there are some cases
    /// where the neurons should directly repeat the input (because of a high chance of a 1:1 link)
    /// 
    /// For example:
    /// 
    /// Manipulators have a high chance of being driven by a single brain, and there's a good chance of a brain's single output neuron
    /// being tied to a manipulator's single input neuron
    /// 
    /// Brains that are wrappers to custom neural nets (NeuronContainerType.Brain_HasInternalNN).  The brain's inputs should take
    /// direct mapping from sensors
    /// </remarks>
    public class Neuron_Direct : INeuron
    {
        #region Declaration Section

        /// <summary>
        /// Storing -1 or 0 in the constructor to reduce if statements in SetValue
        /// </summary>
        private readonly double _minValue;

        #endregion

        #region Constructor

        public Neuron_Direct(Point3D position, bool isPositiveOnly)
        {
            _position = position;
            _isPositiveOnly = isPositiveOnly;

            _minValue = _isPositiveOnly ? 0 : -1;
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
        public Point3D Position => _position;

        private readonly bool _isPositiveOnly;
        public bool IsPositiveOnly => _isPositiveOnly;

        private readonly long _token = TokenGenerator.NextToken();
        public long Token => _token;

        public void SetValue(double sumInputs)
        {
            if (sumInputs < _minValue)
            {
                _value = _minValue;
            }
            else if (sumInputs > 1)
            {
                _value = 1d;
            }
            else
            {
                _value = sumInputs;
            }
        }

        public override string ToString()
        {
            return string.Format
            (
                "{0} | {1} | {2} | {3}",
                IsPositiveOnly ? "positive" : "pos neg",
                Value.ToStringSignificantDigits(2),
                Position.ToStringSignificantDigits(3),
                Token
            );
        }
    }

    #endregion

    #region class: NeuralLinkDNA

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
    #region class: NeuralLinkExternalDNA

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
    #region class: NeuralLink

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

        public override string ToString()
        {
            return string.Format
            (
                "{0} - {1} | {2}",
                FromContainer?.GetType().Name ?? "<null>",
                ToContainer?.GetType().Name ?? "<null>",
                Weight.ToStringSignificantDigits(3)
            );
        }
    }

    #endregion

    #region class: NeuralBucket

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
        #region class: NeuronBackPointer

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
                .ToLookup(o => o.To)
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

        public void Tick_ORIG()
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
        public void Tick()
        {
            foreach (NeuronBackPointer neuron in UtilityCore.RandomOrder(_neurons))
            {
                // Check if the container is switched off
                if (!neuron.Container.IsOn)
                {
                    neuron.Neuron.SetValue(0d);
                    continue;
                }

                double weight = 0;

                // Add up the input neuron weights
                foreach (var link in neuron.Links)
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

                neuron.Neuron.SetValue(weight);
            }
        }

        #endregion
    }

    #endregion
    #region class: NeuralPool

    public class NeuralPool
    {
        #region class: TaskWrapper

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

                        foreach (NeuralBucket bucket2 in UtilityCore.RandomOrder(buckets))
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
            System.Diagnostics.Debug.WriteLine($"Removing Neural Bucket: {bucket.Token}");

            if (!_itemsByToken.ContainsKey(bucket.Token))
            {
                //throw new ArgumentException("This bucket was never added");
                return;
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
    #region class: NeuralPool_ManualTick

    /// <summary>
    /// The neural pool works well for items running in real time.  But for worlds running in background threads,
    /// the neural pool appears to not be able to keep up.  So this class gives direct control of the neurons firing
    /// </summary>
    /// <remarks>
    /// It's funny how much simpler this class is when everything is running on the same thread :)
    /// </remarks>
    public class NeuralPool_ManualTick
    {
        #region Declaration Section

        private List<NeuralBucket> _buckets = new List<NeuralBucket>();

        #endregion

        #region Public Properties

        public int NumBuckets => _buckets.Count;
        public int NumLinks => _buckets.Sum(o => o.Count);

        #endregion

        #region Public Methods

        public void Add(NeuralBucket bucket)
        {
            _buckets.Add(bucket);
        }
        public void Remove(NeuralBucket bucket)
        {
            _buckets.Remove(bucket);
        }

        /// <summary>
        /// Each call does one pass through all the neurons
        /// </summary>
        public void Tick()
        {
            foreach (NeuralBucket bucket in UtilityCore.RandomOrder(_buckets))
            {
                bucket.Tick();
            }
        }

        #endregion
    }

    #endregion
}
