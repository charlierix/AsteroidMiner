using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

using Game.HelperClassesCore;
using Game.Orig.HelperClassesOrig;
using Game.Orig.Math3D;

namespace Game.Orig.TestersGDI.PhysicsPainter
{
    public partial class GravityProps : Game.Orig.HelperClassesGDI.Controls.PiePanel
    {
        #region Declaration Section

        private GravityController _gravController = null;

        #endregion

        #region Constructor

        public GravityProps()
        {
            InitializeComponent();
        }

        #endregion

        #region Public Methods

        public void SetPointers(GravityController gravController)
        {
            _gravController = gravController;

            radGravity_CheckedChanged(this, new EventArgs());
            trkGravityForce_Scroll(this, new EventArgs());
        }

        #endregion

        #region Misc Control Events

        private void radGravity_CheckedChanged(object sender, EventArgs e)
        {
            if (_gravController == null)
            {
                return;
            }

            if (radGravityNone.Checked)
            {
                _gravController.Mode = GravityMode.None;
            }
            else if (radGravityDown.Checked)
            {
                _gravController.Mode = GravityMode.Down;
            }
            else if (radGravityBalls.Checked)
            {
                _gravController.Mode = GravityMode.EachOther;
            }
            else
            {
                MessageBox.Show("Unknown Gravity Radio Button", "Gravity Props", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void trkGravityForce_Scroll(object sender, EventArgs e)
        {
            if (_gravController == null)
            {
                return;
            }

            const double MINGRAVITY = 0d;
            const double MAXGRAVITY = 10d;

            _gravController.GravityMultiplier = UtilityCore.GetScaledValue(MINGRAVITY, MAXGRAVITY, trkGravityForce.Minimum, trkGravityForce.Maximum, trkGravityForce.Value);
        }

        #endregion
    }
}
