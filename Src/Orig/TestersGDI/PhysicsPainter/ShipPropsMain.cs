using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

using Game.HelperClasses;
using Game.Orig.HelperClassesOrig;
using Game.Orig.HelperClassesGDI;
using Game.Orig.Map;

namespace Game.Orig.TestersGDI.PhysicsPainter
{
    public partial class ShipPropsMain : Game.Orig.HelperClassesGDI.Controls.PiePanelBottom
    {
        #region Declaration Section

        private const string TYPE_NONE = "No Ship";
        private const string TYPE_BALL = "Ball";
        private const string TYPE_SOLIDBALL = "Solid Ball";

        private LargeMapViewer2D _picturebox = null;
        private ShipController _shipController = null;

        private double _minSize = double.MinValue;
        private double _maxSize = double.MaxValue;

        #endregion

        #region Constructor

        public ShipPropsMain()
        {
            InitializeComponent();

            cboType.Items.Add(TYPE_NONE);
            cboType.Items.Add(TYPE_BALL);
            cboType.Items.Add(TYPE_SOLIDBALL);
            cboType.Text = TYPE_NONE;

            toolTip1.SetToolTip(btnChase, "Camera will chase the ship");
            toolTip1.SetToolTip(btnStop, "Make the ship stop");
            toolTip1.SetToolTip(cboType, "The type of object that the ship is based on:\nBall -> No angular velocity\nSolid Ball -> Has angular velocity");
        }

        #endregion

        #region Public Methods

        public void SetPointers(LargeMapViewer2D picturebox, ShipController shipController)
        {
            _picturebox = picturebox;
            _shipController = shipController;

            // Apply Settings
            txtMinSize_TextChanged(this, new EventArgs());
            txtMaxSize_TextChanged(this, new EventArgs());
            cboType_SelectedIndexChanged(this, new EventArgs());
            trkThrusterOffset_Scroll(this, new EventArgs());
            trkSize_Scroll(this, new EventArgs());
        }

        #endregion

        #region Misc Control Events

        private void cboType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_shipController == null)
            {
                // They haven't called this.SetPointers yet
                return;
            }

            switch (cboType.Text)
            {
                case TYPE_NONE:
                    #region None

                    btnChase.Enabled = false;
                    btnStop.Enabled = false;

                    trkThrusterOffset.Enabled = false;
                    trkSize.Enabled = false;
                    txtMinSize.Enabled = false;
                    txtMaxSize.Enabled = false;
                    lblThruster.ForeColor = SystemColors.GrayText;
                    lblThrustMin.ForeColor = SystemColors.GrayText;
                    lblThrustMax.ForeColor = SystemColors.GrayText;
                    lblSize.ForeColor = SystemColors.GrayText;

                    _shipController.ShipType = ShipController.ShipTypeQual.None;

                    #endregion
                    break;

                case TYPE_BALL:
                    #region Ball

                    btnChase.Enabled = true;
                    btnStop.Enabled = true;

                    trkThrusterOffset.Enabled = false;
                    trkSize.Enabled = true;
                    txtMinSize.Enabled = true;
                    txtMaxSize.Enabled = true;
                    lblThruster.ForeColor = SystemColors.GrayText;
                    lblThrustMin.ForeColor = SystemColors.GrayText;
                    lblThrustMax.ForeColor = SystemColors.GrayText;
                    lblSize.ForeColor = SystemColors.WindowText;

                    _shipController.ShipType = ShipController.ShipTypeQual.Ball;

                    #endregion
                    break;

                case TYPE_SOLIDBALL:
                    #region Solid Ball

                    btnChase.Enabled = true;
                    btnStop.Enabled = true;

                    trkThrusterOffset.Enabled = true;
                    trkSize.Enabled = true;
                    txtMinSize.Enabled = true;
                    txtMaxSize.Enabled = true;
                    lblThruster.ForeColor = SystemColors.WindowText;
                    lblThrustMin.ForeColor = SystemColors.WindowText;
                    lblThrustMax.ForeColor = SystemColors.WindowText;
                    lblSize.ForeColor = SystemColors.WindowText;

                    _shipController.ShipType = ShipController.ShipTypeQual.SolidBall;

                    #endregion
                    break;

                default:
                    MessageBox.Show("Unknown Ship Type: " + cboType.Text, "Ship Type Props", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
            }
        }

        private void trkThrusterOffset_Scroll(object sender, EventArgs e)
        {
            _shipController.ThrusterOffset = trkThrusterOffset.Value;
            toolTip1.SetToolTip(trkThrusterOffset, trkThrusterOffset.Value.ToString());
        }

        private void txtMinSize_TextChanged(object sender, EventArgs e)
        {
            //	Parse it
            double minSize;
            bool valid = false;
            if (double.TryParse(txtMinSize.Text, out minSize))
            {
                if (minSize > 0 && minSize <= _maxSize)
                {
                    valid = true;
                }
            }

            //	Store it
            if (valid)
            {
                _minSize = minSize;
                txtMinSize.ForeColor = SystemColors.WindowText;
                trkSize_Scroll(this, new EventArgs());

                if (txtMaxSize.ForeColor == Color.Red)
                {
                    txtMaxSize_TextChanged(this, new EventArgs());
                }
            }
            else
            {
                txtMinSize.ForeColor = Color.Red;
            }
        }
        private void txtMaxSize_TextChanged(object sender, EventArgs e)
        {
            //	Parse it
            double maxSize;
            bool valid = false;
            if (double.TryParse(txtMaxSize.Text, out maxSize))
            {
                if (maxSize > 0 && maxSize >= _minSize)
                {
                    valid = true;
                }
            }

            //	Store it
            if (valid)
            {
                _maxSize = maxSize;
                txtMaxSize.ForeColor = SystemColors.WindowText;
                trkSize_Scroll(this, new EventArgs());

                if (txtMinSize.ForeColor == Color.Red)
                {
                    txtMinSize_TextChanged(this, new EventArgs());
                }
            }
            else
            {
                txtMaxSize.ForeColor = Color.Red;
            }
        }
        private void trkSize_Scroll(object sender, EventArgs e)
        {
            _shipController.ShipSize = UtilityHelper.GetScaledValue(_minSize, _maxSize, trkSize.Minimum, trkSize.Maximum, trkSize.Value);
            toolTip1.SetToolTip(trkSize, Math.Round(_shipController.ShipSize, 0).ToString());
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            _shipController.StopShip();
        }
        private void btnChase_Click(object sender, EventArgs e)
        {
            BallBlip ship = _shipController.Ship;

            if (ship == null)
            {
                MessageBox.Show("No ship to chase", "Ship Type Props", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _picturebox.ChasePoint(ship.Ball.Position);
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            KeyboardHints form = new KeyboardHints();

            string text = "";
            text += "Arrow Keys = standard flying\n";
            text += "A, D = Slide left, right\n";
            text += "W, S = 10x force of up/down arrows\n\n";
            text += "Q = Tractor Beam (repulse)\n";
            text += "E = Tractor Beam (attract)\n";
            text += "Q + E = Tractor Beam (static)\n";
            text += "Double Tap Q and E to lock beam on\n\n";
            text += "1 = Set half power tractor beam\n";
            text += "2 = Set normal power tractor beam\n";
            text += "3 = Set double power tractor beam\n";
            text += "4 = Set infinite power tractor beam\n\n";
			text += "Ctrl = Machine Gun\n";
			text += "Shift = Cannon";

            form.KeyboardText = text;
            form.TopMost = true;

            form.Show();
        }

        #endregion
    }
}
