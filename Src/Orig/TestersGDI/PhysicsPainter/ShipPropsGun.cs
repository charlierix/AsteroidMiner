using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

using Game.HelperClasses;
using Game.Orig.HelperClassesOrig;

namespace Game.Orig.TestersGDI.PhysicsPainter
{
	public partial class ShipPropsGun : Game.Orig.HelperClassesGDI.Controls.PiePanelBottom
	{
		#region Declaration Section

		private ShipController _shipController = null;

		#endregion

		#region Constructor

		public ShipPropsGun()
		{
			InitializeComponent();
		}

		#endregion

		#region Public Methods

		public void SetPointers(ShipController shipController)
		{
			_shipController = shipController;

			// Apply Settings
			trkMachineGunOffset_Scroll(this, new EventArgs());
			chkInfinity_CheckedChanged(this, new EventArgs());
			trkCrossoverDistance_Scroll(this, new EventArgs());
		}

		#endregion

		#region Misc Control Events

		private void chkIgnoreOtherProjectiles_CheckedChanged(object sender, EventArgs e)
		{
			_shipController.IgnoreOtherProjectiles = chkIgnoreOtherProjectiles.Checked;
		}

		private void chkInfinity_CheckedChanged(object sender, EventArgs e)
		{
			bool enableTrack = !chkInfinity.Checked;
			trkCrossoverDistance.Enabled = enableTrack;
			lblCrossoverMin.Enabled = enableTrack;
			lblCrossoverMax.Enabled = enableTrack;

			_shipController.MachineGunCrossoverDistanceIsInfinity = chkInfinity.Checked;
		}
		private void trkCrossoverDistance_Scroll(object sender, EventArgs e)
		{
			double distance = UtilityHelper.GetScaledValue_Capped(double.Parse(lblCrossoverMin.Text), double.Parse(lblCrossoverMax.Text), trkCrossoverDistance.Minimum, trkCrossoverDistance.Maximum, trkCrossoverDistance.Value);

			toolTip1.SetToolTip(trkCrossoverDistance, distance.ToString());

			_shipController.MachineGunCrossoverDistance = distance;
		}

		private void trkMachineGunOffset_Scroll(object sender, EventArgs e)
		{
			toolTip1.SetToolTip(trkMachineGunOffset, trkMachineGunOffset.Value.ToString());

			_shipController.MachineGunOffset = trkMachineGunOffset.Value;
		}

		#endregion
	}
}
