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

namespace Game.Newt.Testers.Newt2Tester
{
	public partial class FluidEmulation : UserControl
	{
		#region Events

		public event EventHandler<FluidEmulationArgs> ValueChanged = null;

		#endregion

		#region Constructor

		public FluidEmulation()
		{
			InitializeComponent();
		}

		#endregion

		#region Event Listeners

		private void chkFluid_Checked(object sender, RoutedEventArgs e)
		{
			if (this.ValueChanged != null)
			{
				this.ValueChanged(this, new FluidEmulationArgs(chkFluid.IsChecked.Value, trkViscosity.Value));
			}
		}
		private void trkViscosity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (this.ValueChanged != null)
			{
				this.ValueChanged(this, new FluidEmulationArgs(chkFluid.IsChecked.Value, trkViscosity.Value));
			}
		}

		#endregion
	}

	#region class: FluidEmulation

	public class FluidEmulationArgs : EventArgs
	{
		public FluidEmulationArgs(bool emulateFluid, double viscosity)
		{
			this.EmulateFluid = emulateFluid;
			this.Viscosity = viscosity;
		}

		public bool EmulateFluid
		{
			get;
			private set;
		}
		public double Viscosity
		{
			get;
			private set;
		}
	}

	#endregion
}
