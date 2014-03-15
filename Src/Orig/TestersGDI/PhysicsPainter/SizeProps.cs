using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

using Game.HelperClasses;
using Game.Orig.HelperClassesOrig;
using Game.Orig.Map;

namespace Game.Orig.TestersGDI.PhysicsPainter
{
    public partial class SizeProps : Game.Orig.HelperClassesGDI.Controls.PiePanelBottom
    {
        #region Declaration Section

        public event EventHandler ValueChanged = null;

        private BallProps _exposedProps = null;

        #endregion

        #region Constructor

        public SizeProps()
        {
            InitializeComponent();

            // Initialization moved to SetProps()
        }

        #endregion

        #region Public Methods

        public void SetProps(BallProps props, bool allowStationaryRotatable)
        {
            _exposedProps = props;

            toolTip1.SetToolTip(chkTemporary, "Disapears after collision");

            // Add combo items
            cboCollisionStyle.Items.Add(CollisionStyle.Standard.ToString());
            cboCollisionStyle.Items.Add(CollisionStyle.Stationary.ToString());
            if (allowStationaryRotatable)
            {
                cboCollisionStyle.Items.Add(CollisionStyle.StationaryRotatable);
            }
            cboCollisionStyle.Items.Add(CollisionStyle.Ghost.ToString());

            // Store the collision style
            cboCollisionStyle.Text = CollisionStyle.Standard.ToString();
            cboCollisionStyle_SelectedIndexChanged(this, new EventArgs());

            // Store the size
            Size_CheckedChanged(this, new EventArgs());
            trkSize_Scroll(this, new EventArgs());
        }

        #endregion

        #region Misc Control Events

        private void Size_CheckedChanged(object sender, EventArgs e)
        {
            if (_exposedProps == null)
            {
                return;
            }

            // Set the enum
            if (radRandomSize.Checked)
            {
                _exposedProps.SizeMode = BallProps.SizeModes.Random;
            }
            else if (radDrawSize.Checked)
            {
                _exposedProps.SizeMode = BallProps.SizeModes.Draw;
            }
            else if (radFixedSize.Checked)
            {
                _exposedProps.SizeMode = BallProps.SizeModes.Fixed;
            }
            else
            {
                MessageBox.Show("Unknown Checked Button", "SizeMode Change", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // Set enabled (also call change events to insure everything is properly set up)
            switch (_exposedProps.SizeMode)
            {
                case BallProps.SizeModes.Draw:
                    txtMinSize.Enabled = false;
                    txtMaxSize.Enabled = false;
                    trkSize.Enabled = false;
                    break;

                case BallProps.SizeModes.Random:
                    txtMinSize.Enabled = true;
                    txtMaxSize.Enabled = true;
                    trkSize.Enabled = false;
                    txtMinSize_TextChanged(this, new EventArgs());
                    txtMaxSize_TextChanged(this, new EventArgs());
                    trkSize_Scroll(this, new EventArgs());
                    break;

                case BallProps.SizeModes.Fixed:
                    txtMinSize.Enabled = true;
                    txtMaxSize.Enabled = true;
                    trkSize.Enabled = true;
                    txtMinSize_TextChanged(this, new EventArgs());
                    txtMaxSize_TextChanged(this, new EventArgs());
                    trkSize_Scroll(this, new EventArgs());
                    break;

                default:
                    MessageBox.Show("Unknown BallProps.SizeModes: " + _exposedProps.SizeMode.ToString(), "SizeMode Change", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
            }

            // Raise Event
            OnValueChanged();
        }

        private void txtMinSize_TextChanged(object sender, EventArgs e)
        {
            if (_exposedProps == null)
            {
                return;
            }

            // Parse it
            double minSize;
            bool valid = false;
            if (double.TryParse(txtMinSize.Text, out minSize))
            {
                if (minSize > 0 && minSize <= _exposedProps.MaxRandSize)
                {
                    valid = true;
                }
            }

            // Store it
            if (valid)
            {
                _exposedProps.MinRandSize = minSize;
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

            // Raise Event
            OnValueChanged();
        }
        private void txtMaxSize_TextChanged(object sender, EventArgs e)
        {
            if (_exposedProps == null)
            {
                return;
            }

            // Parse it
            double maxSize;
            bool valid = false;
            if (double.TryParse(txtMaxSize.Text, out maxSize))
            {
                if (maxSize > 0 && maxSize >= _exposedProps.MinRandSize)
                {
                    valid = true;
                }
            }

            // Store it
            if (valid)
            {
                _exposedProps.MaxRandSize = maxSize;
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

            // Raise Event
            OnValueChanged();
        }
        private void trkSize_Scroll(object sender, EventArgs e)
        {
            if (_exposedProps == null)
            {
                return;
            }

            // Store the value
            _exposedProps.SizeIfFixed = UtilityHelper.GetScaledValue(_exposedProps.MinRandSize, _exposedProps.MaxRandSize, trkSize.Minimum, trkSize.Maximum, trkSize.Value);
            toolTip1.SetToolTip(trkSize, _exposedProps.SizeIfFixed.ToString());

            // Raise Event
            OnValueChanged();
        }

        private void cboCollisionStyle_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_exposedProps == null)
            {
                return;
            }

            _exposedProps.CollisionStyle = (CollisionStyle)Enum.Parse(typeof(CollisionStyle), cboCollisionStyle.Text);

            OnValueChanged();
        }

        private void chkTemporary_CheckedChanged(object sender, EventArgs e)
        {
            _exposedProps.Temporary = chkTemporary.Checked;
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
