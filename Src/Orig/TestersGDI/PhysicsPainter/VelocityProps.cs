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
	public partial class VelocityProps : Game.Orig.HelperClassesGDI.Controls.PiePanelBottom
	{
		#region Declaration Section

		public event EventHandler ValueChanged = null;

		private BallProps _exposedProps = null;

		#endregion

		#region Constructor

		public VelocityProps()
		{
			InitializeComponent();

			//	Initialization moved to SetProps()
		}

		#endregion

		#region Public Methods

		public void SetProps(BallProps props)
		{
			_exposedProps = props;

			chkRandomVelocity_CheckedChanged(this, new EventArgs());
			vectorPanel1.Multiplier = double.Parse(txtMaxVelocity.Text);
			txtMaxVelocity_TextChanged(this, new EventArgs());

			txtAngularVelocityLeft_TextChanged(this, new EventArgs());
			txtAngularVelocityRight_TextChanged(this, new EventArgs());
			trkAngularVelocity_Scroll(this, new EventArgs());
			radAngularVelocity_CheckedChanged(this, new EventArgs());
		}

		#endregion

		#region Misc Control Events

		private void chkRandomVelocity_CheckedChanged(object sender, EventArgs e)
		{
			if (chkRandomVelocity.Checked)
			{
				_exposedProps.RandomVelocity = true;
				vectorPanel1.Enabled = false;
				vectorPanel1.BackColor = SystemColors.ControlLight;
			}
			else
			{
				_exposedProps.RandomVelocity = false;
				vectorPanel1.Enabled = true;
				vectorPanel1.BackColor = SystemColors.Window;
			}

			OnValueChanged();
		}
		private void vectorPanel1_ValueChanged(object sender, EventArgs e)
		{
			_exposedProps.Velocity.StoreNewValues(vectorPanel1.Value);

			OnValueChanged();
		}
		private void vectorPanel1_MultiplierChanged(object sender, EventArgs e)
		{
			txtMaxVelocity.Text = vectorPanel1.Multiplier.ToString();
			_exposedProps.MaxVelocity = vectorPanel1.Multiplier;

			OnValueChanged();
		}
		private void txtMaxVelocity_TextChanged(object sender, EventArgs e)
		{
			double maxVelocity;
			if (double.TryParse(txtMaxVelocity.Text, out maxVelocity))
			{
				vectorPanel1.Multiplier = maxVelocity;
				_exposedProps.MaxVelocity = maxVelocity;
				txtMaxVelocity.ForeColor = SystemColors.WindowText;
			}
			else
			{
				txtMaxVelocity.ForeColor = Color.Red;
			}

			OnValueChanged();
		}

		private void radAngularVelocity_CheckedChanged(object sender, EventArgs e)
		{

			//	Set the enum
			if (radAngularVelocityRandom.Checked)
			{
				_exposedProps.AngularVelocityMode = BallProps.AngularVelocityModes.Random;
			}
			else if (radAngularVelocityFixed.Checked)
			{
				_exposedProps.AngularVelocityMode = BallProps.AngularVelocityModes.Fixed;
			}
			else
			{
				MessageBox.Show("Unknown Angular Velocity Mode Option", "AngularVelocity Mode Change", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}

			//	Set enabled (also call change events to insure everything is properly set up)
			switch (_exposedProps.AngularVelocityMode)
			{
				case BallProps.AngularVelocityModes.Random:
					txtAngularVelocityLeft.Enabled = true;
					txtAngularVelocityRight.Enabled = true;
					trkAngularVelocity.Enabled = false;
					txtAngularVelocityLeft_TextChanged(this, new EventArgs());
					txtAngularVelocityRight_TextChanged(this, new EventArgs());
					trkAngularVelocity_Scroll(this, new EventArgs());
					break;

				case BallProps.AngularVelocityModes.Fixed:
					txtAngularVelocityLeft.Enabled = true;
					txtAngularVelocityRight.Enabled = true;
					trkAngularVelocity.Enabled = true;
					txtAngularVelocityLeft_TextChanged(this, new EventArgs());
					txtAngularVelocityRight_TextChanged(this, new EventArgs());
					trkAngularVelocity_Scroll(this, new EventArgs());
					break;

				default:
					MessageBox.Show("Unknown BallProps.AngularVelocityModes: " + _exposedProps.AngularVelocityMode.ToString(), "AngularVelocity Mode Change", MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
			}

			//	No need to call OnValueChanged, because it's been called indirectly already

		}
		private void txtAngularVelocityLeft_TextChanged(object sender, EventArgs e)
		{
			//	Parse it
			double angVelLeft;
			bool valid = false;
			if (double.TryParse(txtAngularVelocityLeft.Text, out angVelLeft))
			{
				if (angVelLeft < 0)
				{
					valid = true;
				}
			}

			//	Store it
			if (valid)
			{
				_exposedProps.MinRandAngularVelocity = angVelLeft;
				txtAngularVelocityLeft.ForeColor = SystemColors.WindowText;
				trkAngularVelocity_Scroll(this, new EventArgs());

				//if (txtAngularVelocityRight.ForeColor == Color.Red)
				//{
				//    txtAngularVelocityRight_TextChanged(this, new EventArgs());
				//}
			}
			else
			{
				txtAngularVelocityLeft.ForeColor = Color.Red;
			}

			//	No need to call OnValueChanged (it's already been called indirectly)
		}
		private void txtAngularVelocityRight_TextChanged(object sender, EventArgs e)
		{
			//	Parse it
			double angVelRight;
			bool valid = false;
			if (double.TryParse(txtAngularVelocityRight.Text, out angVelRight))
			{
				if (angVelRight > 0)
				{
					valid = true;
				}
			}

			//	Store it
			if (valid)
			{
				_exposedProps.MaxRandAngularVelocity = angVelRight;
				txtAngularVelocityRight.ForeColor = SystemColors.WindowText;
				trkAngularVelocity_Scroll(this, new EventArgs());

				//if (txtAngularVelocityLeft.ForeColor == Color.Red)
				//{
				//    txtAngularVelocityLeft_TextChanged(this, new EventArgs());
				//}
			}
			else
			{
				txtAngularVelocityRight.ForeColor = Color.Red;
			}

			//	No need to call OnValueChanged (it's already been called indirectly)
		}
		private void trkAngularVelocity_Scroll(object sender, EventArgs e)
		{
			//	Calculate the trackbar value
			double scaledValue = UtilityHelper.GetScaledValue(_exposedProps.MinRandAngularVelocity, _exposedProps.MaxRandAngularVelocity, trkAngularVelocity.Minimum, trkAngularVelocity.Maximum, trkAngularVelocity.Maximum - trkAngularVelocity.Value);
			toolTip1.SetToolTip(trkAngularVelocity, Math.Round(scaledValue, 2).ToString());

			//	I present angular velocity in rotations/frame.  But the torqueball wants it in radians/frame
			_exposedProps.AngularVelocityIfFixed = scaledValue * Math.PI * 2;

			OnValueChanged();
		}

		private void zeroToolStripMenuItem_Click(object sender, EventArgs e)
		{
			trkAngularVelocity.Value = 50;
			trkAngularVelocity_Scroll(this, new EventArgs());
		}

		#endregion

		#region Protected Methods

		protected virtual void OnValueChanged()
		{
			if (this.ValueChanged != null)
			{
				this.ValueChanged(this, new EventArgs());
			}
		}

		#endregion
	}
}
