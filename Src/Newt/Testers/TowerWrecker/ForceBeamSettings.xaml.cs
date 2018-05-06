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

namespace Game.Newt.Testers.TowerWrecker
{
	public partial class ForceBeamSettings : UserControl
	{
		#region Events

		public event EventHandler<ForceBeamSettingsArgs> BeamSettingsChanged = null;

		#endregion

		#region Constructor

		public ForceBeamSettings()
		{
			InitializeComponent();
		}

		#endregion

		#region Public Properties

		public ForceBeamSettingsArgs BeamSettings
		{
			get
			{
				return new ForceBeamSettingsArgs(trkPushPull.Value, trkTowardAway.Value * -1d, trkAngle.Value, trkRadius.Value, chkLinearDropoff.IsChecked.Value);
			}
		}

		#endregion

		#region Event Listeners

		private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (this.BeamSettingsChanged != null)
			{
				this.BeamSettingsChanged(this, this.BeamSettings);
			}
		}

		private void btnReset_Click(object sender, RoutedEventArgs e)
		{
			trkPushPull.Value = 0d;
			trkTowardAway.Value = 0d;
			trkAngle.Value = 0d;
			trkRadius.Value = 3d;
		}

		#endregion
	}

	#region class: ForceBeamSettingsArgs

	public class ForceBeamSettingsArgs : EventArgs
	{
		public ForceBeamSettingsArgs(double pushPullForce, double towardAwayForce, double angle, double radius, bool isLinearDropoff)
		{
			this.PushPullForce = pushPullForce;
			this.TowardAwayForce = towardAwayForce;
			this.Angle = angle;
			this.Radius = radius;
			this.IsLinearDropoff = isLinearDropoff;
		}

		public double PushPullForce
		{
			get;
			private set;
		}
		public double TowardAwayForce
		{
			get;
			private set;
		}
		public double Angle
		{
			get;
			private set;
		}
		public double Radius
		{
			get;
			private set;
		}
		public bool IsLinearDropoff
		{
			get;
			private set;
		}
	}

	#endregion
}
