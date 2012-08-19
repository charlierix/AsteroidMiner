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

using Game.HelperClasses;
using Game.Newt.HelperClasses;

namespace Game.Newt.AsteroidMiner_153
{
	public partial class ProgressBarGame1 : UserControl
	{
		#region Public Properties

		public ProgressBarGame1()
		{
			InitializeComponent();
		}

		#endregion

		#region Public Properties

		public string Text
		{
			get
			{
				return label1.Text;
			}
			set
			{
				label1.Text = value;
			}
		}

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

		private Color _barColor = Colors.Silver;
		public Color BarColor
		{
			get
			{
				return _barColor;
			}
			set
			{
				_barColor = value;

				// Turn the color into a slight gradient
				LinearGradientBrush brush = new LinearGradientBrush();
				brush.StartPoint = new Point(0, 0);
				brush.EndPoint = new Point(0, 1);

				GradientStopCollection gradients = new GradientStopCollection();
				gradients.Add(new GradientStop(UtilityWPF.AlphaBlend(Colors.White, _barColor, .5d), 0d));
				gradients.Add(new GradientStop(UtilityWPF.AlphaBlend(Colors.White, _barColor, .25d), .1d));
				gradients.Add(new GradientStop(_barColor, .4d));
				gradients.Add(new GradientStop(UtilityWPF.AlphaBlend(Colors.Black, _barColor, .2d), .9d));
				gradients.Add(new GradientStop(UtilityWPF.AlphaBlend(Colors.Black, _barColor, .25d), 1d));
				brush.GradientStops = gradients;

				rectangle1.Background = brush;  // rectangle1 is actually a border control
			}
		}

		#endregion

		#region Private Methods

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
				percent = UtilityHelper.GetScaledValue_Capped(0d, 1d, _minimum, _maximum, _value);
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

		#endregion
	}
}
