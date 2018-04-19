using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using Game.HelperClassesCore;

namespace Game.HelperClassesWPF.Controls2D
{
    //TODO: derive from RangeBase like slider does
    public partial class SliderShowValues : UserControl
    {
        #region class: PropSync

        /// <summary>
        /// This is a helper class that will keep a slider tied to an options class
        /// TODO: This class doesn't really belong here
        /// </summary>
        public class PropSync
        {
            #region Declaration Section

            private readonly double _desiredMin;
            private readonly double _desiredMax;

            private bool _isProgramaticallyChanging = false;

            #endregion

            #region Constructor

            public PropSync(SliderShowValues slider, PropertyInfo prop, object item, double desiredMin, double desiredMax)
            {
                _desiredMin = desiredMin;
                _desiredMax = desiredMax;

                _slider = slider;
                _prop = prop;

                // Using the public property, because it modifies slider
                this.Item = item;

                _slider.ValueChanged += new EventHandler(Slider_ValueChanged);
            }

            #endregion

            #region Public Properties

            private readonly SliderShowValues _slider;
            public SliderShowValues Slider
            {
                get
                {
                    return _slider;
                }
            }

            private readonly PropertyInfo _prop;
            public PropertyInfo Prop
            {
                get
                {
                    return _prop;
                }
            }

            private object _item;
            public object Item
            {
                get
                {
                    return _item;
                }
                set
                {
                    // Reset Min/Max
                    _isProgramaticallyChanging = true;

                    _slider.Minimum = _desiredMin;
                    _slider.Maximum = _desiredMax;

                    _isProgramaticallyChanging = false;

                    _item = value;

                    // Store the value
                    Transfer_OptionsToSlider();
                }
            }

            #endregion

            #region Event Listeners

            private void Slider_ValueChanged(object sender, EventArgs e)
            {
                if (!_isProgramaticallyChanging)
                {
                    Transfer_SliderToOptions();
                }
            }

            #endregion

            #region Private Methods

            private void Transfer_SliderToOptions()
            {
                if (_slider.IsInteger)
                {
                    _prop.SetValue(_item, Convert.ToInt32(_slider.Value), null);
                }
                else
                {
                    _prop.SetValue(_item, _slider.Value, null);
                }
            }
            private void Transfer_OptionsToSlider()
            {
                // Pull the value out of the options class (any prop tied to a slider should be able to be cast as a double)
                double newValue = Convert.ToDouble(_prop.GetValue(_item, null));

                _isProgramaticallyChanging = true;

                // Make sure Min/Max can handle the new value
                if (newValue < _slider.Minimum)
                {
                    _slider.Minimum = newValue;
                }

                if (newValue > _slider.Maximum)
                {
                    _slider.Maximum = newValue;
                }

                // Store it
                _slider.Value = newValue;

                _isProgramaticallyChanging = false;
            }

            #endregion
        }

        #endregion

        #region Events

        public event EventHandler ValueChanged = null;

        #endregion

        #region Declaration Section

        private bool _settingValueDisplay = false;

        private MultiplierValueConverter _valueConverter = null;

        #endregion

        #region Constructor

        public SliderShowValues()
        {
            InitializeComponent();

            // Value converter
            _valueConverter = this.Resources["logConverter"] as MultiplierValueConverter;
            if (_valueConverter == null)
            {
                throw new ApplicationException("Didn't find the value converter");
            }
            _valueConverter.Parent = this;

            // Don't let the textboxes get wider than half the control's width
            SetTextBoxMaxWidth();
        }

        #endregion

        #region Public Properties

        public double Minimum
        {
            get { return (double)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }
        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register("Minimum", typeof(double), typeof(SliderShowValues), new UIPropertyMetadata(0d));

        public double Maximum
        {
            get { return (double)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }
        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register("Maximum", typeof(double), typeof(SliderShowValues), new UIPropertyMetadata(10d));

        public double Value
        {
            get { return (double)GetValue(ValueProperty); }
            set
            {
                SetValue(ValueProperty, value);
            }
        }
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(double), typeof(SliderShowValues), new UIPropertyMetadata(0d, ValuePropertyChanged));

        public string ValueDisplay
        {
            get { return (string)GetValue(ValueDisplayProperty); }
            set { SetValue(ValueDisplayProperty, value); }
        }
        public static readonly DependencyProperty ValueDisplayProperty = DependencyProperty.Register("ValueDisplay", typeof(string), typeof(SliderShowValues), new UIPropertyMetadata("0", ValueDisplayPropertyChanged));

        public bool IsInteger
        {
            get { return (bool)GetValue(IsIntegerProperty); }
            set { SetValue(IsIntegerProperty, value); }
        }
        public static readonly DependencyProperty IsIntegerProperty = DependencyProperty.Register("IsInteger", typeof(bool), typeof(SliderShowValues), new UIPropertyMetadata(false));

        /// <summary>
        /// If this is true, then this slider represents a multiplier
        /// </summary>
        /// <remarks>
        /// If this is true, then the middle of the slider is 1 (multiply by one to get no change)
        /// Everything to the right of the middle is from 1 to Max
        /// Everything to the left of the middle is from Min to 1
        /// </remarks>
        public bool IsMultiplier
        {
            get { return (bool)GetValue(IsMultiplierProperty); }
            set { SetValue(IsMultiplierProperty, value); }
        }
        public static readonly DependencyProperty IsMultiplierProperty = DependencyProperty.Register("IsMultiplier", typeof(bool), typeof(SliderShowValues), new PropertyMetadata(false));

        // This is used so the min/max textboxes don't go over half the width of the control
        private double TextBoxMaxWidth
        {
            get { return (double)GetValue(TextBoxMaxWidthProperty); }
            set { SetValue(TextBoxMaxWidthProperty, value); }
        }
        private static readonly DependencyProperty TextBoxMaxWidthProperty = DependencyProperty.Register("TextBoxMaxWidth", typeof(double), typeof(SliderShowValues), new UIPropertyMetadata(20d));

        #endregion

        #region Event Listeners

        private void Control_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetTextBoxMaxWidth();
        }

        private static void ValuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sender = d as SliderShowValues;
            if (sender != null)
            {
                sender.ThisValueChanged();
            }
        }
        private void ThisValueChanged()
        {
            if (this.IsInteger)
            {
                this.Value = Math.Round(this.Value);
            }

            #region Update ValueDisplay

            _settingValueDisplay = true;

            //NOTE: A simplified version of this logic was copied into UtilityCore.ToStringSignificantDigits

            int numMin = GetNumDecimals(this.Minimum);
            int numMax = GetNumDecimals(this.Maximum);

            if (numMin < 0 || numMax < 0)
            {
                // Unknown number of decimal places
                this.ValueDisplay = this.Value.ToString();
            }
            else
            {
                // Get the integer portion
                long intPortion = Convert.ToInt64(Math.Truncate(this.Value));		// going directly against the value for this (min could go from 1 to 1000.  1 needs two decimal places, 10 needs one, 100+ needs zero)
                int numInt;
                if (intPortion == 0)
                {
                    numInt = 0;
                }
                else
                {
                    numInt = intPortion.ToString().Length;
                }

                // Limit the number of significant digits
                int numPlaces;
                if (numInt == 0)
                {
                    numPlaces = Math.Max(numMin, numMax) + 2;
                }
                else if (numInt >= 3)
                {
                    numPlaces = 0;
                }
                else
                {
                    numPlaces = 3 - numInt;
                }

                // I was getting an exception from round, but couldn't recreate it, so I'm just throwing this in to avoid the exception
                if (numPlaces < 0)
                {
                    numPlaces = 0;
                }
                else if (numPlaces > 15)
                {
                    numPlaces = 15;
                }

                // Show a rounded number
                double rounded = Math.Round(this.Value, numPlaces);
                int numActualDecimals = GetNumDecimals(rounded);
                if (numActualDecimals < 0)
                {
                    this.ValueDisplay = rounded.ToString();		// it's weird, don't try to make it more readable
                }
                else
                {
                    this.ValueDisplay = rounded.ToString("N" + numActualDecimals);
                }
            }

            _settingValueDisplay = false;

            #endregion

            // Raise Event
            if (this.ValueChanged != null)
            {
                this.ValueChanged(this, new EventArgs());
            }
        }

        private static void ValueDisplayPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sender = d as SliderShowValues;
            if (sender != null)
            {
                sender.ThisValueDisplayChanged();
            }
        }
        private void ThisValueDisplayChanged()
        {
            bool isError;

            if (_settingValueDisplay)
            {
                isError = false;
            }
            else
            {
                isError = true;

                // They typed a value directly.  Set this.Value, which will update the slider, raise a change event
                double valueCast;
                if (double.TryParse(this.ValueDisplay, out valueCast))
                {
                    // Don't bother constraining to min/max.  They typed a value, just expand the min/max (they can always change the min/max directly, but that's tedious)
                    if (valueCast < this.Minimum)
                    {
                        this.Minimum = valueCast;
                    }

                    if (valueCast > this.Maximum)
                    {
                        this.Maximum = valueCast;
                    }

                    this.Value = valueCast;
                    isError = false;
                }
            }

            if (isError)
            {
                txtValue.BorderThickness = new Thickness(2d);
                txtValue.BorderBrush = Brushes.Red;
            }
            else
            {
                txtValue.BorderThickness = new Thickness(0d);
                txtValue.BorderBrush = Brushes.Transparent;
            }
        }

        #endregion

        #region Private Methods

        private void SetTextBoxMaxWidth()
        {
            double width = this.ActualWidth;
            width *= .45d;

            this.TextBoxMaxWidth = width;		// the min/max textbox's maxwidth is set to this property
        }

        private static int GetNumDecimals(double value)
        {
            string text = value.ToString(System.Globalization.CultureInfo.InvariantCulture);		// I think this forces decimal to always be a '.' ?

            if (Regex.IsMatch(text, "[a-z]", RegexOptions.IgnoreCase))
            {
                // This is in exponential notation, just give up (or maybe NaN)
                return -1;
            }

            int decimalIndex = text.IndexOf(".");

            if (decimalIndex < 0)
            {
                // It's an integer
                return 0;
            }
            else
            {
                // Just count the decimals
                return (text.Length - 1) - decimalIndex;
            }
        }

        #endregion
    }

    #region class: MultiplierValueConverter

    public class MultiplierValueConverter : IValueConverter
    {
        #region class: ConvertProps

        private class ConvertProps
        {
            public ConvertProps(double absMin, double absMax, double absMiddle, bool isPositive)
            {
                this.AbsMin = absMin;
                this.AbsMax = absMax;
                this.AbsMiddle = absMiddle;
                this.IsPositive = isPositive;
            }

            public readonly double AbsMin;
            public readonly double AbsMax;
            public readonly double AbsMiddle;

            public readonly bool IsPositive;
        }

        #endregion

        public SliderShowValues Parent = null;

        /// <summary>
        /// Take the public value, and maps it onto the slider
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double valueCast = (double)value;

            ConvertProps props = IsMultiplier();
            if (props != null)
            {
                double absValue = Math.Abs(valueCast);

                double retVal;

                if (absValue > 1d)
                {
                    double percent = (absValue - 1d) / (props.AbsMax - 1d);

                    // Scale that to go from middle to max
                    retVal = UtilityCore.GetScaledValue_Capped(props.AbsMiddle, props.AbsMax, 0d, 1d, percent);
                }
                else
                {
                    double percent = (absValue - props.AbsMin) / (1d - props.AbsMin);

                    // Scale that to go from min to middle
                    retVal = UtilityCore.GetScaledValue_Capped(props.AbsMin, props.AbsMiddle, 0d, 1d, percent);
                }

                if (!props.IsPositive)
                {
                    // Negate
                    retVal *= -1d;
                }

                return retVal;
            }
            else
            {
                return valueCast;
            }
        }

        /// <summary>
        /// Take the slider value, and map it to the public value
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double valueCast = (double)value;

            ConvertProps props = IsMultiplier();
            if (props != null)
            {
                double absValue = Math.Abs(valueCast);

                double retVal;

                if (absValue > props.AbsMiddle)
                {
                    double percent = (absValue - props.AbsMiddle) / (props.AbsMax - props.AbsMiddle);

                    // Scale that to go from 1 to max
                    retVal = UtilityCore.GetScaledValue_Capped(1d, props.AbsMax, 0d, 1d, percent);
                }
                else
                {
                    double percent = (absValue - props.AbsMin) / (props.AbsMiddle - props.AbsMin);

                    // Scale that to go from min to 1
                    retVal = UtilityCore.GetScaledValue_Capped(props.AbsMin, 1d, 0d, 1d, percent);
                }

                if (!props.IsPositive)
                {
                    // Negate
                    retVal *= -1d;
                }

                return retVal;
            }
            else
            {
                return valueCast;
            }
        }

        #region Private Methods

        /// <summary>
        /// This will return null if it's not a multiplier, or not valid to do a multiplier
        /// </summary>
        private ConvertProps IsMultiplier()
        {
            if (this.Parent == null)
            {
                return null;
            }
            else if (!this.Parent.IsMultiplier)
            {
                return null;
            }

            // They want log, make sure min and max are appropriate

            double absMin = Math.Abs(this.Parent.Minimum);
            double absMax = Math.Abs(this.Parent.Maximum);

            if (absMin > 1 || absMax < 1)
            {
                // 1 needs to be between them
                return null;
            }
            else if (absMin == 0)
            {
                // Min can't be zero, that's an asymptote
                return null;
            }

            bool isMinPositive = this.Parent.Minimum > 0;
            bool isMaxPositive = this.Parent.Maximum > 0;

            if (isMinPositive != isMaxPositive)
            {
                // They must either both be negative, or both be positive
                return null;
            }

            // Use the ratio of min:max to figure out the percent along the progress bar one should be at.  If the ratio is the same
            // (.25 to 4, or .1 to 10, etc), then the % should be .5.  If there is more to the right than left, then adjust the mid point
            // left a bit so that more of the progress bar is available
            double invertAbsMin = 1d / absMin;

            double ratio = invertAbsMin / absMax;
            if (ratio < 1)
            {
                ratio = UtilityCore.GetScaledValue_Capped(.25d, .5, .1, 1, ratio);        //reusing the ratio variable to be % along progress bar.  never let it go less than .25 so that the progress bar stays usable
            }
            else
            {
                ratio = UtilityCore.GetScaledValue_Capped(.5d, .75, 1, 10, ratio);
            }

            double absMiddle = absMin + ((absMax - absMin) * ratio);

            // Need to do a multiplier scale.  Return a filled out object
            return new ConvertProps(absMin, absMax, absMiddle, isMinPositive);
        }

        #endregion
    }

    #endregion
}
