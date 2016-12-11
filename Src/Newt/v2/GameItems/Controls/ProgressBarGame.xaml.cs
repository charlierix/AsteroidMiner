using System;
using System.Collections.Generic;
using System.Linq;
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

using Game.HelperClassesCore;
using Game.HelperClassesWPF;

namespace Game.Newt.v2.GameItems.Controls
{
    public partial class ProgressBarGame : UserControl
    {
        #region Constructor

        public ProgressBarGame()
        {
            InitializeComponent();

            // Force the gradients to be calculated
            ProgressColorChanged(this, new DependencyPropertyChangedEventArgs(ProgressColorProperty, this.ProgressColor, this.ProgressColor));
            ProgressBackColorChanged(this, new DependencyPropertyChangedEventArgs(ProgressBackColorProperty, this.ProgressBackColor, this.ProgressBackColor));
            ProgressDamageColorChanged(this, new DependencyPropertyChangedEventArgs(ProgressDamageColorProperty, this.ProgressDamageColor, this.ProgressDamageColor));
        }

        #endregion

        #region Public Properties

        public Visibility LeftLabelVisibility
        {
            get { return (Visibility)GetValue(LeftLabelVisibilityProperty); }
            set { SetValue(LeftLabelVisibilityProperty, value); }
        }
        public static readonly DependencyProperty LeftLabelVisibilityProperty = DependencyProperty.Register("LeftLabelVisibility", typeof(Visibility), typeof(ProgressBarGame), new UIPropertyMetadata(Visibility.Collapsed));

        public string LeftLabelText
        {
            get { return (string)GetValue(LeftLabelTextProperty); }
            set { SetValue(LeftLabelTextProperty, value); }
        }
        public static readonly DependencyProperty LeftLabelTextProperty = DependencyProperty.Register("LeftLabelText", typeof(string), typeof(ProgressBarGame), new UIPropertyMetadata(""));

        public Visibility RightLabelVisibility
        {
            get { return (Visibility)GetValue(RightLabelVisibilityProperty); }
            set { SetValue(RightLabelVisibilityProperty, value); }
        }
        public static readonly DependencyProperty RightLabelVisibilityProperty = DependencyProperty.Register("RightLabelVisibility", typeof(Visibility), typeof(ProgressBarGame), new UIPropertyMetadata(Visibility.Collapsed));

        public string RightLabelText
        {
            get { return (string)GetValue(RightLabelTextProperty); }
            set { SetValue(RightLabelTextProperty, value); }
        }
        public static readonly DependencyProperty RightLabelTextProperty = DependencyProperty.Register("RightLabelText", typeof(string), typeof(ProgressBarGame), new UIPropertyMetadata(""));

        // These are the public properties that are simple colors
        public Color ProgressColor
        {
            get { return (Color)GetValue(ProgressColorProperty); }
            set { SetValue(ProgressColorProperty, value); }
        }
        public static readonly DependencyProperty ProgressColorProperty = DependencyProperty.Register("ProgressColor", typeof(Color), typeof(ProgressBarGame), new UIPropertyMetadata(Colors.DarkGreen, ProgressColorChanged));
        private static void ProgressColorChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ProgressBarGame senderCast = sender as ProgressBarGame;
            if (senderCast == null)
            {
                return;		// this should never happen
            }

            Color color = (Color)e.NewValue;

            // Turn the color into a slight gradient
            LinearGradientBrush brush = new LinearGradientBrush();
            brush.StartPoint = new Point(0, 0);
            brush.EndPoint = new Point(0, 1);

            GradientStopCollection gradients = new GradientStopCollection();
            gradients.Add(new GradientStop(UtilityWPF.AlphaBlend(Colors.White, color, .5d), 0d));
            gradients.Add(new GradientStop(UtilityWPF.AlphaBlend(Colors.White, color, .25d), .1d));
            gradients.Add(new GradientStop(color, .4d));
            gradients.Add(new GradientStop(UtilityWPF.AlphaBlend(Colors.Black, color, .2d), .9d));
            gradients.Add(new GradientStop(UtilityWPF.AlphaBlend(Colors.Black, color, .25d), 1d));
            brush.GradientStops = gradients;

            // The progress bar is actually tied to this property
            senderCast.ProgressBrush = brush;
        }

        public Color ProgressBackColor
        {
            get { return (Color)GetValue(ProgressBackColorProperty); }
            set { SetValue(ProgressBackColorProperty, value); }
        }
        public static readonly DependencyProperty ProgressBackColorProperty = DependencyProperty.Register("ProgressBackColor", typeof(Color), typeof(ProgressBarGame), new UIPropertyMetadata(Color.FromRgb(40, 40, 40), ProgressBackColorChanged));
        private static void ProgressBackColorChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ProgressBarGame senderCast = sender as ProgressBarGame;
            if (senderCast == null)
            {
                return;		// this should never happen
            }

            Color color = (Color)e.NewValue;

            // Turn the color into a slight gradient
            LinearGradientBrush brush = new LinearGradientBrush();
            brush.StartPoint = new Point(0, 0);
            brush.EndPoint = new Point(0, 1);

            GradientStopCollection gradients = new GradientStopCollection();
            gradients.Add(new GradientStop(UtilityWPF.AlphaBlend(Colors.Black, color, .5d), 0d));
            gradients.Add(new GradientStop(UtilityWPF.AlphaBlend(Colors.Black, color, .25d), .1d));
            gradients.Add(new GradientStop(color, .6d));
            gradients.Add(new GradientStop(UtilityWPF.AlphaBlend(Colors.DimGray, color, .2d), .9d));
            gradients.Add(new GradientStop(UtilityWPF.AlphaBlend(Colors.DimGray, color, .25d), 1d));
            brush.GradientStops = gradients;

            // The progress bar is actually tied to this property
            senderCast.ProgressBackBrush = brush;
            senderCast.ProgressBackBorderBrush = new SolidColorBrush(UtilityWPF.AlphaBlend(Colors.Black, color, .25d));		// this is the same as the second gradient stop
        }

        public Color ProgressDamageColor
        {
            get { return (Color)GetValue(ProgressDamageColorProperty); }
            set { SetValue(ProgressDamageColorProperty, value); }
        }
        public static readonly DependencyProperty ProgressDamageColorProperty = DependencyProperty.Register("ProgressDamageColor", typeof(Color), typeof(ProgressBarGame), new UIPropertyMetadata(UtilityWPF.ColorFromHex("630E0E"), ProgressDamageColorChanged));
        private static void ProgressDamageColorChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ProgressBarGame senderCast = sender as ProgressBarGame;
            if (senderCast == null)
            {
                return;		// this should never happen
            }

            Color color = (Color)e.NewValue;

            // Turn the color into a slight gradient
            LinearGradientBrush brush = new LinearGradientBrush();
            brush.StartPoint = new Point(0, 0);
            brush.EndPoint = new Point(0, 1);

            GradientStopCollection gradients = new GradientStopCollection();
            gradients.Add(new GradientStop(UtilityWPF.AlphaBlend(Colors.Black, color, .5d), 0d));
            gradients.Add(new GradientStop(UtilityWPF.AlphaBlend(Colors.Black, color, .25d), .1d));
            gradients.Add(new GradientStop(color, .6d));
            gradients.Add(new GradientStop(UtilityWPF.AlphaBlend(Colors.DimGray, color, .2d), .9d));
            gradients.Add(new GradientStop(UtilityWPF.AlphaBlend(Colors.DimGray, color, .25d), 1d));
            brush.GradientStops = gradients;

            // The progress bar is actually tied to this property
            senderCast.ProgressDamageBrush = brush;
        }

        // These are what the progress bar is tied to, which is a linear gradient brush based on the color change event
        private Brush ProgressBrush
        {
            get { return (Brush)GetValue(ProgressBrushProperty); }
            set { SetValue(ProgressBrushProperty, value); }
        }
        private static readonly DependencyProperty ProgressBrushProperty = DependencyProperty.Register("ProgressBrush", typeof(Brush), typeof(ProgressBarGame), new UIPropertyMetadata(Brushes.DarkGreen));     //NOTE: this default is ignored, because the constructor calls ProgressColorChanged, which sets the brush to a gradient based on that color prop

        private Brush ProgressBackBrush
        {
            get { return (Brush)GetValue(ProgressBackBrushProperty); }
            set { SetValue(ProgressBackBrushProperty, value); }
        }
        private static readonly DependencyProperty ProgressBackBrushProperty = DependencyProperty.Register("ProgressBackBrush", typeof(Brush), typeof(ProgressBarGame), new UIPropertyMetadata(Brushes.Black));

        public Brush ProgressBackBorderBrush
        {
            get { return (Brush)GetValue(ProgressBackBorderBrushProperty); }
            set { SetValue(ProgressBackBorderBrushProperty, value); }
        }
        public static readonly DependencyProperty ProgressBackBorderBrushProperty = DependencyProperty.Register("ProgressBackBorderBrush", typeof(Brush), typeof(ProgressBarGame), new UIPropertyMetadata(Brushes.Black));

        private Brush ProgressDamageBrush
        {
            get { return (Brush)GetValue(ProgressDamageBrushProperty); }
            set { SetValue(ProgressDamageBrushProperty, value); }
        }
        private static readonly DependencyProperty ProgressDamageBrushProperty = DependencyProperty.Register("ProgressDamageBrush", typeof(Brush), typeof(ProgressBarGame), new UIPropertyMetadata(Brushes.Maroon));

        private double _minimum = 0d;
        public double Minimum
        {
            get
            {
                return _minimum;
            }
            set
            {
                _minimum = value;

                ResizeColorBar();
            }
        }

        private double _maximum = 100d;
        public double Maximum
        {
            get
            {
                return _maximum;
            }
            set
            {
                _maximum = value;

                ResizeColorBar();
            }
        }

        private double _value = 0d;
        public double Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;

                ResizeColorBar();
            }
        }

        private double _damagedPercent;
        public double DamagedPercent
        {
            get
            {
                return _damagedPercent;
            }
            set
            {
                _damagedPercent = value;

                ResizeColorBar();
            }
        }

        #endregion

        #region Private Methods

        private void ResizeColorBar_ORIG()
        {
            #region Figure out percent

            double percent = 0d;

            // If they don't add up, don't throw an error, just make it invisible (they are probably in the middle of setting the properties
            if (_minimum >= _maximum || _value < _minimum)
            {
                percent = 0d;
            }
            else if (_value > _maximum)
            {
                percent = 1d;
            }
            else
            {
                percent = UtilityCore.GetScaledValue_Capped(0d, 1d, _minimum, _maximum, _value);
            }

            #endregion

            #region Set grid column widths

            if (percent == 0d)
            {
                barWidth.Width = new GridLength(0d);
                remainderWidth.Width = new GridLength(1d, GridUnitType.Star);
            }
            else if (percent == 1d)
            {
                barWidth.Width = new GridLength(1d, GridUnitType.Star);
                remainderWidth.Width = new GridLength(0d);
            }
            else
            {
                // I propably don't need to multiply by 100, but I feel better
                barWidth.Width = new GridLength(percent * 100, GridUnitType.Star);
                remainderWidth.Width = new GridLength((1d - percent) * 100, GridUnitType.Star);
            }

            #endregion
        }
        private void ResizeColorBar()
        {
            #region Figure out percent

            double percent = 0d;

            // If they don't add up, don't throw an error, just make it invisible (they are probably in the middle of setting the properties
            if (_minimum >= _maximum || _value < _minimum)
            {
                percent = 0d;
            }
            else if (_value > _maximum)
            {
                percent = 1d;
            }
            else
            {
                percent = UtilityCore.GetScaledValue_Capped(0d, 1d, _minimum, _maximum, _value);
            }

            // Damage is just a percent, not a value
            double damagePercent = _damagedPercent;
            if (damagePercent < 0)
            {
                damagePercent = 0;
            }
            else if (damagePercent > 1)
            {
                damagePercent = 1;
            }

            double availablePercent = 1d - damagePercent;

            if (percent > availablePercent)
            {
                percent = availablePercent;
            }

            #endregion

            #region Set grid column widths

            // 3 cases where one thing is fully filling the bar
            if (percent.IsNearZero() && damagePercent.IsNearZero())
            {
                barWidth.Width = new GridLength(0d);
                remainderWidth.Width = new GridLength(1d, GridUnitType.Star);
                damageWidth.Width = new GridLength(0d);
            }
            else if (percent.IsNearZero() && damagePercent.IsNearValue(1d))
            {
                barWidth.Width = new GridLength(0d);
                remainderWidth.Width = new GridLength(0d);
                damageWidth.Width = new GridLength(1d, GridUnitType.Star);
            }
            else if (percent.IsNearValue(1d) && damagePercent.IsNearZero())
            {
                barWidth.Width = new GridLength(1d, GridUnitType.Star);
                remainderWidth.Width = new GridLength(0d);
                damageWidth.Width = new GridLength(0d);
            }

            // 3 cases where something is zero, the other two are partial
            else if (percent.IsNearZero())
            {
                barWidth.Width = new GridLength(0d);
                remainderWidth.Width = new GridLength((1d - damagePercent) * 100, GridUnitType.Star);
                damageWidth.Width = new GridLength(damagePercent * 100, GridUnitType.Star);
            }
            else if (damagePercent.IsNearZero())
            {
                barWidth.Width = new GridLength(percent * 100, GridUnitType.Star);
                remainderWidth.Width = new GridLength((1d - percent) * 100, GridUnitType.Star);
                damageWidth.Width = new GridLength(0d);
            }
            else if ((percent + damagePercent).IsNearValue(1d))
            {
                barWidth.Width = new GridLength(percent * 100, GridUnitType.Star);
                remainderWidth.Width = new GridLength(0d);
                damageWidth.Width = new GridLength(damagePercent * 100, GridUnitType.Star);
            }

            // last case where all three are partial
            else
            {
                barWidth.Width = new GridLength(percent * 100, GridUnitType.Star);
                remainderWidth.Width = new GridLength((1d - (percent + damagePercent)) * 100, GridUnitType.Star);
                damageWidth.Width = new GridLength(damagePercent * 100, GridUnitType.Star);
            }

            #endregion
        }

        #endregion
    }
}
