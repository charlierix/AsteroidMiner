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

namespace Game.Newt.Testers.Newt2Tester
{
	public partial class BodyAttraction : UserControl
	{
		#region Events

		public event EventHandler<BodyAttractionArgs> AttractionChanged = null;

		#endregion

		#region Declaration Section

		private bool _isInitialized = false;

		#endregion

		#region Constructor

		public BodyAttraction()
		{
			InitializeComponent();

			_isInitialized = true;
		}

		#endregion

		#region Event Listeners

		private void Radio_Checked(object sender, RoutedEventArgs e)
		{
			try
			{
				if (!_isInitialized)
				{
					return;
				}

				SetDistanceTrackbarVisibility();

				FireAttractEvent();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), "Radio_Checked", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			try
			{
				if (!_isInitialized)
				{
					return;
				}

				FireAttractEvent();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), "Slider_ValueChanged", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		#endregion

		#region Private Methods

		private void FireAttractEvent()
		{
			if (this.AttractionChanged == null)
			{
				return;
			}

			BodyAttractionType attractionType = GetAttractionType();

			double distance = trkDistance.Value;
			double strength;
			switch (attractionType)
			{
				case BodyAttractionType.Gravity:
					strength = UtilityCore.GetScaledValue_Capped(0, 125, trkStrength.Minimum, trkStrength.Maximum, trkStrength.Value);
					break;

				case BodyAttractionType.Spring:
					strength = UtilityCore.GetScaledValue_Capped(0, 10, trkStrength.Minimum, trkStrength.Maximum, trkStrength.Value);
					break;

				case BodyAttractionType.SpringDesiredDistance:
					strength = UtilityCore.GetScaledValue_Capped(0, 50, trkStrength.Minimum, trkStrength.Maximum, trkStrength.Value);
					distance = UtilityCore.GetScaledValue_Capped(0, 100, trkDistance.Minimum, trkDistance.Maximum, trkDistance.Value);
					break;

				case BodyAttractionType.SpringInverseDist:
					strength = UtilityCore.GetScaledValue_Capped(0, 10, trkStrength.Minimum, trkStrength.Maximum, trkStrength.Value);
					break;

				case BodyAttractionType.Tangent:
					strength = UtilityCore.GetScaledValue_Capped(0, 200, trkStrength.Minimum, trkStrength.Maximum, trkStrength.Value);
					distance = UtilityCore.GetScaledValue_Capped(0, 50, trkDistance.Minimum, trkDistance.Maximum, trkDistance.Value);
					break;

				case BodyAttractionType.Constant:
					strength = UtilityCore.GetScaledValue_Capped(0, 20, trkStrength.Minimum, trkStrength.Maximum, trkStrength.Value);
					break;

				default:
					strength = UtilityCore.GetScaledValue_Capped(0, 100, trkStrength.Minimum, trkStrength.Maximum, trkStrength.Value);
					break;
			}

			BodyAttractionArgs args = new BodyAttractionArgs(GetAttractionType(), GetIsToward(), strength, distance);
			this.AttractionChanged(this, args);
		}

		private BodyAttractionType GetAttractionType()
		{
			if (radNone.IsChecked.Value)
			{
				return BodyAttractionType.None;
			}
			else if (radGravity.IsChecked.Value)
			{
				return BodyAttractionType.Gravity;
			}
			else if (radSpring.IsChecked.Value)
			{
				return BodyAttractionType.Spring;
			}
			else if (radSpringInverseDist.IsChecked.Value)
			{
				return BodyAttractionType.SpringInverseDist;
			}
			else if (radConstant.IsChecked.Value)
			{
				return BodyAttractionType.Constant;
			}
			else if (radSpringPushPull.IsChecked.Value)
			{
				return BodyAttractionType.SpringDesiredDistance;
			}
			else if (radTangent.IsChecked.Value)
			{
				return BodyAttractionType.Tangent;
			}
			else
			{
				throw new ApplicationException("Unknown attraction type");
			}
		}
		private bool GetIsToward()
		{
			if (radToward.IsChecked.Value)
			{
				return true;
			}
			else if (radAway.IsChecked.Value)
			{
				return false;
			}
			else
			{
				throw new ApplicationException("Unknown Toward/Away Setting");
			}
		}

		private void SetDistanceTrackbarVisibility()
		{
			BodyAttractionType attractionType = GetAttractionType();

			switch (attractionType)
			{
				case BodyAttractionType.None:
				case BodyAttractionType.Gravity:
				case BodyAttractionType.Spring:
				case BodyAttractionType.Constant:
					lblDistance.Visibility = Visibility.Collapsed;
					trkDistance.Visibility = Visibility.Collapsed;
					break;

				case BodyAttractionType.SpringInverseDist:
				case BodyAttractionType.SpringDesiredDistance:
				case BodyAttractionType.Tangent:
					lblDistance.Visibility = Visibility.Visible;
					trkDistance.Visibility = Visibility.Visible;
					break;

				default:
					throw new ApplicationException("Unknown BodyAttractionType: " + attractionType.ToString());
			}
		}

		#endregion
	}

	#region Enum: BodyAttractionType

	public enum BodyAttractionType
	{
		None,
		Gravity,
		Spring,
		SpringInverseDist,
		SpringDesiredDistance,
		Constant,
		Tangent
	}

	#endregion

	#region Class: BodyAttractionArgs

	public class BodyAttractionArgs : EventArgs
	{
		public BodyAttractionArgs(BodyAttractionType attractionType, bool isToward, double strength, double distance)
		{
			this.AttractionType = attractionType;
			this.IsToward = isToward;
			this.Strength = strength;
			this.Distance = distance;
		}

		public BodyAttractionType AttractionType
		{
			get;
			private set;
		}
		public bool IsToward
		{
			get;
			private set;
		}
		public double Strength
		{
			get;
			private set;
		}
		public double Distance
		{
			get;
			private set;
		}
	}

	#endregion
}
