using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

using Game.HelperClasses;
using Game.Orig.Math3D;
using Game.Orig.Map;
using Game.Orig.HelperClassesOrig;

namespace Game.Orig.TestersGDI.PhysicsPainter
{
	public partial class MapShape : Game.Orig.HelperClassesGDI.Controls.PiePanel
	{
		#region Declaration Section

		private const double BOUNDRYMAX = PhysicsPainterMainForm.BOUNDRY * 5;

        private MyVector _boundryLower = null;
        private MyVector _boundryUpper = null;

		#endregion

		#region Constructor

		public MapShape()
		{
			InitializeComponent();
		}

		#endregion

		#region Public Methods

        public void SetPointers(MyVector boundryLower, MyVector boundryUpper)
		{
			_boundryLower = boundryLower;
			_boundryUpper = boundryUpper;

			// Apply Settings
			trkWidth_Scroll(this, new EventArgs());
			trkHeight_Scroll(this, new EventArgs());
		}

		#endregion

		#region Misc Control Events

		private void chkForceSquare_CheckedChanged(object sender, EventArgs e)
		{
			if (chkForceSquare.Checked)
			{
				trkHeight.Value = trkWidth.Value;
				trkHeight_Scroll(this, new EventArgs());
			}
		}

		private void trkWidth_Scroll(object sender, EventArgs e)
		{
			double size = UtilityHelper.GetScaledValue(0, BOUNDRYMAX, trkWidth.Minimum, trkWidth.Maximum, trkWidth.Value);

			_boundryLower.X = size * -1d;
			_boundryUpper.X = size;

			if (chkForceSquare.Checked && trkHeight.Value != trkWidth.Value)
			{
				trkHeight.Value = trkWidth.Value;
				trkHeight_Scroll(this, new EventArgs());
			}

			ShowDimensions();
		}
		private void trkHeight_Scroll(object sender, EventArgs e)
		{
			double size = UtilityHelper.GetScaledValue(0, BOUNDRYMAX, trkHeight.Minimum, trkHeight.Maximum, trkHeight.Value);

			_boundryLower.Y = size * -1d;
			_boundryUpper.Y = size;

			if (chkForceSquare.Checked && trkWidth.Value != trkHeight.Value)
			{
				trkWidth.Value = trkHeight.Value;
				trkWidth_Scroll(this, new EventArgs());
			}

			ShowDimensions();
		}

		#endregion

		#region Private Methods

		private void ShowDimensions()
		{
			double width = _boundryUpper.X * 2;
			double height = _boundryUpper.Y * 2;

			width = Math.Round(width, 0);
			height = Math.Round(height, 0);

			lblSize.Text = width.ToString("N0") + " x " + height.ToString("N0");
		}

		#endregion
	}
}
