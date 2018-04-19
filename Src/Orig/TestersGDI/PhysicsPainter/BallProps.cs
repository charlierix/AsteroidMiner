using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

using Game.HelperClassesCore;
using Game.Orig.HelperClassesOrig;
using Game.Orig.Map;
using Game.Orig.Math3D;

namespace Game.Orig.TestersGDI.PhysicsPainter
{
    public partial class PieBallProps : Game.Orig.HelperClassesGDI.Controls.PiePanel
    {
        #region Declaration Section

        public BallProps ExposedProps = new BallProps();

        #endregion

        #region Constructor

        public PieBallProps()
        {
            InitializeComponent();

            vectorPanel1.Multiplier = double.Parse(txtMaxVelocity.Text);

            vectorPanel1.MultiplierChanged += new EventHandler(vectorPanel1_MultiplierChanged);
            vectorPanel1.ValueChanged += new EventHandler(vectorPanel1_ValueChanged);
            txtMaxVelocity_TextChanged(this, new EventArgs());

            txtMinSize.Text = "100";
            txtMaxSize.Text = "500";
            trkSize_Scroll(this, new EventArgs());

            chkRandom_CheckedChanged(this, new EventArgs());

            cboCollisionStyle.Items.Add(CollisionStyle.Standard.ToString());
            cboCollisionStyle.Items.Add(CollisionStyle.Stationary.ToString());
            cboCollisionStyle.Items.Add(CollisionStyle.Ghost.ToString());

            cboCollisionStyle.Text = CollisionStyle.Standard.ToString();
            cboCollisionStyle_SelectedIndexChanged(this, new EventArgs());

            Size_CheckedChanged(this, new EventArgs());
        }

        #endregion

        #region Misc Control Events

        private void Size_CheckedChanged(object sender, EventArgs e)
        {

            // Set the enum
            if (radRandomSize.Checked)
            {
                this.ExposedProps.SizeMode = BallProps.SizeModes.Random;
            }
            else if (radDrawSize.Checked)
            {
                this.ExposedProps.SizeMode = BallProps.SizeModes.Draw;
            }
            else if (radFixedSize.Checked)
            {
                this.ExposedProps.SizeMode = BallProps.SizeModes.Fixed;
            }
            else
            {
                MessageBox.Show("Unknown Size Mode Option", "SizeMode Change", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // Set enabled (also call change events to insure everything is properly set up)
            switch (this.ExposedProps.SizeMode)
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
                    MessageBox.Show("Unknown BallProps.SizeModes: " + this.ExposedProps.SizeMode.ToString(), "SizeMode Change", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
            }

        }

        private void chkRandom_CheckedChanged(object sender, EventArgs e)
        {
            if (chkRandom.Checked)
            {
                this.ExposedProps.RandomVelocity = true;
                vectorPanel1.Enabled = false;
                vectorPanel1.BackColor = SystemColors.ControlLight;
            }
            else
            {
                this.ExposedProps.RandomVelocity = false;
                vectorPanel1.Enabled = true;
                vectorPanel1.BackColor = SystemColors.Window;
            }
        }

        private void vectorPanel1_ValueChanged(object sender, EventArgs e)
        {
            this.ExposedProps.Velocity.StoreNewValues(vectorPanel1.Value);
        }

        private void vectorPanel1_MultiplierChanged(object sender, EventArgs e)
        {
            txtMaxVelocity.Text = vectorPanel1.Multiplier.ToString();
            this.ExposedProps.MaxVelocity = vectorPanel1.Multiplier;
        }
        private void txtMaxVelocity_TextChanged(object sender, EventArgs e)
        {
            double maxVelocity;
            if (double.TryParse(txtMaxVelocity.Text, out maxVelocity))
            {
                vectorPanel1.Multiplier = maxVelocity;
                this.ExposedProps.MaxVelocity = maxVelocity;
                txtMaxVelocity.ForeColor = SystemColors.WindowText;
            }
            else
            {
                txtMaxVelocity.ForeColor = Color.Red;
            }
        }

        private void txtMinSize_TextChanged(object sender, EventArgs e)
        {
            // Parse it
            double minSize;
            bool valid = false;
            if (double.TryParse(txtMinSize.Text, out minSize))
            {
                if (minSize > 0 && minSize <= this.ExposedProps.MaxRandSize)
                {
                    valid = true;
                }
            }

            // Store it
            if (valid)
            {
                this.ExposedProps.MinRandSize = minSize;
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
            // Parse it
            double maxSize;
            bool valid = false;
            if (double.TryParse(txtMaxSize.Text, out maxSize))
            {
                if (maxSize > 0 && maxSize >= this.ExposedProps.MinRandSize)
                {
                    valid = true;
                }
            }

            // Store it
            if (valid)
            {
                this.ExposedProps.MaxRandSize = maxSize;
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
            this.ExposedProps.SizeIfFixed = UtilityCore.GetScaledValue(this.ExposedProps.MinRandSize, this.ExposedProps.MaxRandSize, trkSize.Minimum, trkSize.Maximum, trkSize.Value);
            toolTip1.SetToolTip(trkSize, this.ExposedProps.SizeIfFixed.ToString());
        }

        private void cboCollisionStyle_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.ExposedProps.CollisionStyle = (CollisionStyle)Enum.Parse(typeof(CollisionStyle), cboCollisionStyle.Text);
        }

        #endregion
    }

    #region class: BallProps

    public class BallProps
    {
        #region enum: SizeModes

        public enum SizeModes
        {
            Random = 0,
            Draw,
            Fixed
        }

        #endregion
        #region enum: AngularVelocityModes

        public enum AngularVelocityModes
        {
            Random = 0,
            Fixed
        }

        #endregion

        public CollisionStyle CollisionStyle = CollisionStyle.Standard;

        public bool Temporary = false;

        #region Size

        /// <summary>
        /// This is how to determine what size the next new object should be
        /// </summary>
        public SizeModes SizeMode = SizeModes.Random;

        /// <summary>
        /// If fixed, then this is how large each new object should be
        /// </summary>
        public double SizeIfFixed = 1d;

        // These are used when the mode is set to random size
        public double MinRandSize = 0d;
        public double MaxRandSize = double.MaxValue;		// I need to set it like this so the min textbox's change event won't error during initialization

        #endregion

        #region Velocity

        /// <summary>
        /// If this is true, then each time an object is created, it needs a random vector, otherwise it gets a clone of this.Velocity
        /// </summary>
        public bool RandomVelocity = false;
        /// <summary>
        /// This is the largest the random velocity vector should become
        /// </summary>
        public double MaxVelocity = 3;
        /// <summary>
        /// If it's not random, then this is the velocity that each new object should get
        /// </summary>
        public MyVector Velocity = new MyVector();

        #endregion
        #region Angular Velocity

        /// <summary>
        /// This is how to determine what angular velocity the next new object should have
        /// </summary>
        public AngularVelocityModes AngularVelocityMode = AngularVelocityModes.Fixed;
        /// <summary>
        /// If fixed, then this is how much angular velocity each new object should get
        /// </summary>
        public double AngularVelocityIfFixed = 0d;

        // These are used when the mode is set to random
        public double MinRandAngularVelocity = double.MinValue;		// I need to set it like this so the textbox's change events won't error during initialization
        public double MaxRandAngularVelocity = double.MaxValue;

        #endregion
    }

    #endregion
}
