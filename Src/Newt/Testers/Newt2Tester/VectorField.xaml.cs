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

namespace Game.Newt.Testers.Newt2Tester
{
	public partial class VectorField : UserControl
	{
		#region Events

		public event EventHandler<ApplyVectorFieldArgs> ApplyVectorField = null;

		#endregion

		#region Constructor

		public VectorField()
		{
			InitializeComponent();
		}

		#endregion

		#region Event Listeners

		private void Radio_Checked(object sender, RoutedEventArgs e)
		{
			try
			{
				FireApplyEvent();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), "Radio_Checked", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void trkStrength_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			try
			{
				FireApplyEvent();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), "Strength_ValueChanged", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		#endregion

		#region Private Methods

		private void FireApplyEvent()
		{
			if (this.ApplyVectorField == null)
			{
				return;
			}

			//	Field Type
			VectorFieldType fieldType;
			if (radNone.IsChecked.Value)
			{
				fieldType = VectorFieldType.None;
			}
			else if (radInward.IsChecked.Value)
			{
				fieldType = VectorFieldType.Inward;
			}
			else if (radOutward.IsChecked.Value)
			{
				fieldType = VectorFieldType.Outward;
			}
			else if (radSwirlInward.IsChecked.Value)
			{
				fieldType = VectorFieldType.SwirlInward;
			}
			else if (radSwirl.IsChecked.Value)
			{
				fieldType = VectorFieldType.Swirl;
			}
			else if (radTowardZ.IsChecked.Value)
			{
				fieldType = VectorFieldType.Z0Plane;
			}
			else
			{
				throw new ApplicationException("Unknown field type");
			}

			//	Force/Acceleration
			bool useForce;
			if (radForce.IsChecked.Value)
			{
				useForce = true;
			}
			else if (radAccel.IsChecked.Value)
			{
				useForce = false;
			}
			else
			{
				throw new ApplicationException("Unknown Force/Acceleration type");
			}

			//	Strength
			double strength;
			if (useForce)
			{
				strength = UtilityHelper.GetScaledValue_Capped(.5d, 100d, trkStrength.Minimum, trkStrength.Maximum, trkStrength.Value);
			}
			else
			{
				strength = UtilityHelper.GetScaledValue_Capped(.05d, 25d, trkStrength.Minimum, trkStrength.Maximum, trkStrength.Value);
			}

			//	Fire Event
			this.ApplyVectorField(this, new ApplyVectorFieldArgs(fieldType, strength, useForce));
		}

		#endregion
	}

	#region Enum: VectorFieldType

	public enum VectorFieldType
	{
		None,
		Inward,
		Outward,
		SwirlInward,
		Swirl,
		Z0Plane
	}

	#endregion

	#region Class: ApplyVectorFieldArgs

	public class ApplyVectorFieldArgs : EventArgs
	{
		public ApplyVectorFieldArgs(VectorFieldType fieldType, double strength, bool useForce)
		{
			this.FieldType = fieldType;
			this.Strength = strength;
			this.UseForce = useForce;
		}

		public VectorFieldType FieldType
		{
			get;
			private set;
		}
		public double Strength
		{
			get;
			private set;
		}
		/// <summary>
		/// True:  Strength is force
		/// False:  Strength is acceleration
		/// </summary>
		public bool UseForce
		{
			get;
			private set;
		}
	}

	#endregion
}
