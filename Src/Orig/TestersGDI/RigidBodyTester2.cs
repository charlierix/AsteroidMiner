using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using Game.HelperClassesCore;
using Game.Orig.HelperClassesOrig;
using Game.Orig.Math3D;

namespace Game.Orig.TestersGDI
{
    public partial class RigidBodyTester2 : Form
    {
        #region Declaration Section

        private const double MINRADIUS = 0;
        private const double MAXRADIUS = 1000;
        private const double MINMASS = 10;
        private const double MAXMASS = 400;
        private const double MINMOMENTUM = -3000000;
        private const double MAXMOMENTUM = 3000000;

        private MyVector _boundryLower = new MyVector(-1200, -1200, 0);
        private MyVector _boundryUpper = new MyVector(1200, 1200, 0);

        private RigidBody _rigidBody = null;
        private PointMass _north = null;
        private PointMass _south = null;
        private PointMass _east = null;
        private PointMass _west = null;

        private bool _isProgramaticPointMass1D = false;
        private bool _isProgramaticPointMass2D = false;

        SolidBrush _massBrush = new SolidBrush(Color.Gold);
        SolidBrush _centerPosBrush = new SolidBrush(Color.Black);
        SolidBrush _centerMassBrush = new SolidBrush(Color.White);

        #endregion

        #region Constructor

        public RigidBodyTester2()
        {
            InitializeComponent();

            pictureBox1.AutoScroll = false;
            pictureBox1.PanMouseButton = MouseButtons.None;
            pictureBox1.SetBorder(_boundryLower, _boundryUpper);
            pictureBox1.ZoomFit();

            btnResetTotal_Click(this, new EventArgs());

            chkRunning_CheckedChanged(this, new EventArgs());
        }

        #endregion

        #region Misc Control Events

        private void btnResetTotal_Click(object sender, EventArgs e)
        {
            chkCoupledXPos.Checked = false;
            chkCoupledYPos.Checked = false;

            _rigidBody = new RigidBody(new MyVector(0, 0, 0), new DoubleVector(1, 0, 0, 0, 1, 0), 1, _boundryLower, _boundryUpper);

            // Set up PointMasses
            _north = _rigidBody.AddPointMass(0, 0, 0, MINMASS);
            _south = _rigidBody.AddPointMass(0, 0, 0, MINMASS);
            _east = _rigidBody.AddPointMass(0, 0, 0, MINMASS);
            _west = _rigidBody.AddPointMass(0, 0, 0, MINMASS);

            trkNorthPos.Value = 500;
            trkNorthPos_Scroll(this, new EventArgs());
            trkSouthPos.Value = 500;
            trkSouthPos_Scroll(this, new EventArgs());
            trkEastPos.Value = 500;
            trkEastPos_Scroll(this, new EventArgs());
            trkWestPos.Value = 500;
            trkWestPos_Scroll(this, new EventArgs());

            trkNorthMass.Value = 500;
            trkNorthMass_Scroll(this, new EventArgs());
            trkSouthMass.Value = 500;
            trkSouthMass_Scroll(this, new EventArgs());
            trkEastMass.Value = 500;
            trkEastMass_Scroll(this, new EventArgs());
            trkWestMass.Value = 500;
            trkWestMass_Scroll(this, new EventArgs());

            // Set the momentum
            trkAngularMomentum.Value = 500;
            trkAngularMomentum_Scroll(this, new EventArgs());
        }
        private void btnResetPartial_Click(object sender, EventArgs e)
        {
            _rigidBody = new RigidBody(new MyVector(0, 0, 0), new DoubleVector(1, 0, 0, 0, 1, 0), 1, _boundryLower, _boundryUpper);

            // Set up PointMasses
            _north = _rigidBody.AddPointMass(0, 0, 0, MINMASS);
            _south = _rigidBody.AddPointMass(0, 0, 0, MINMASS);
            _east = _rigidBody.AddPointMass(0, 0, 0, MINMASS);
            _west = _rigidBody.AddPointMass(0, 0, 0, MINMASS);

            trkNorthPos_Scroll(this, new EventArgs());
            trkSouthPos_Scroll(this, new EventArgs());
            trkEastPos_Scroll(this, new EventArgs());
            trkWestPos_Scroll(this, new EventArgs());

            trkNorthMass_Scroll(this, new EventArgs());
            trkSouthMass_Scroll(this, new EventArgs());
            trkEastMass_Scroll(this, new EventArgs());
            trkWestMass_Scroll(this, new EventArgs());

            // Set the momentum
            trkAngularMomentum_Scroll(this, new EventArgs());
        }

        #region Position

        private void chkCoupledBothPos_CheckedChanged(object sender, EventArgs e)
        {
            if (chkCoupledBothPos.Checked)
            {
                chkCoupledXPos.Checked = true;
                chkCoupledYPos.Checked = true;

                // They may already be true.  Make sure one of them fires
                chkCoupledXPos_CheckedChanged(this, new EventArgs());
            }
        }
        private void chkCoupledXPos_CheckedChanged(object sender, EventArgs e)
        {
            if (chkCoupledXPos.Checked)
            {
                trkEastPos.Value = trkWestPos.Maximum - trkWestPos.Value;
                trkEastPos_Scroll(this, new EventArgs());
            }
            else
            {
                chkCoupledBothPos.Checked = false;
            }
        }
        private void chkCoupledYPos_CheckedChanged(object sender, EventArgs e)
        {
            if (chkCoupledYPos.Checked)
            {
                trkNorthPos.Value = trkSouthPos.Maximum - trkSouthPos.Value;
                trkNorthPos_Scroll(this, new EventArgs());
            }
            else
            {
                chkCoupledBothPos.Checked = false;
            }
        }

        private void trkNorthPos_Scroll(object sender, EventArgs e)
        {

            _north.Y = UtilityCore.GetScaledValue(MINRADIUS, MAXRADIUS, trkNorthPos.Minimum, trkNorthPos.Maximum, trkNorthPos.Maximum - trkNorthPos.Value);

            if (!_isProgramaticPointMass1D && chkCoupledYPos.Checked)
            {
                _isProgramaticPointMass1D = true;

                // Align 1D
                trkSouthPos.Value = trkNorthPos.Maximum - trkNorthPos.Value;
                trkSouthPos_Scroll(this, new EventArgs());

                _isProgramaticPointMass1D = false;

                if (!_isProgramaticPointMass2D && chkCoupledBothPos.Checked)
                {
                    _isProgramaticPointMass2D = true;

                    // Align 2D
                    trkEastPos.Value = trkSouthPos.Value;
                    trkEastPos_Scroll(this, new EventArgs());

                    _isProgramaticPointMass2D = false;
                }
            }

        }
        private void trkSouthPos_Scroll(object sender, EventArgs e)
        {

            _south.Y = -1d * UtilityCore.GetScaledValue(MINRADIUS, MAXRADIUS, trkSouthPos.Minimum, trkSouthPos.Maximum, trkSouthPos.Value);

            if (!_isProgramaticPointMass1D && chkCoupledYPos.Checked)
            {
                _isProgramaticPointMass1D = true;

                // Align 1D
                trkNorthPos.Value = trkSouthPos.Maximum - trkSouthPos.Value;
                trkNorthPos_Scroll(this, new EventArgs());

                _isProgramaticPointMass1D = false;

                if (!_isProgramaticPointMass2D && chkCoupledBothPos.Checked)
                {
                    _isProgramaticPointMass2D = true;

                    // Align 2D
                    trkEastPos.Value = trkSouthPos.Value;
                    trkEastPos_Scroll(this, new EventArgs());

                    _isProgramaticPointMass2D = false;
                }
            }

        }
        private void trkEastPos_Scroll(object sender, EventArgs e)
        {

            _east.X = UtilityCore.GetScaledValue(MINRADIUS, MAXRADIUS, trkEastPos.Minimum, trkEastPos.Maximum, trkEastPos.Value);

            if (!_isProgramaticPointMass1D && chkCoupledXPos.Checked)
            {
                _isProgramaticPointMass1D = true;

                // Align 1D
                trkWestPos.Value = trkEastPos.Maximum - trkEastPos.Value;
                trkWestPos_Scroll(this, new EventArgs());

                _isProgramaticPointMass1D = false;

                if (!_isProgramaticPointMass2D && chkCoupledBothPos.Checked)
                {
                    _isProgramaticPointMass2D = true;

                    // Align 2D
                    trkSouthPos.Value = trkEastPos.Value;
                    trkSouthPos_Scroll(this, new EventArgs());

                    _isProgramaticPointMass2D = false;
                }
            }

        }
        private void trkWestPos_Scroll(object sender, EventArgs e)
        {

            _west.X = -1d * UtilityCore.GetScaledValue(MINRADIUS, MAXRADIUS, trkWestPos.Minimum, trkWestPos.Maximum, trkWestPos.Maximum - trkWestPos.Value);

            if (!_isProgramaticPointMass1D && chkCoupledXPos.Checked)
            {
                _isProgramaticPointMass1D = true;

                // Align 1D
                trkEastPos.Value = trkWestPos.Maximum - trkWestPos.Value;
                trkEastPos_Scroll(this, new EventArgs());

                _isProgramaticPointMass1D = false;

                if (!_isProgramaticPointMass2D && chkCoupledBothPos.Checked)
                {
                    _isProgramaticPointMass2D = true;

                    // Align 2D
                    trkSouthPos.Value = trkEastPos.Value;
                    trkSouthPos_Scroll(this, new EventArgs());

                    _isProgramaticPointMass2D = false;
                }
            }

        }

        #endregion
        #region Mass

        private void chkCoupledBothMass_CheckedChanged(object sender, EventArgs e)
        {
            if (chkCoupledBothMass.Checked)
            {
                chkCoupledXMass.Checked = true;
                chkCoupledYMass.Checked = true;

                // They may already be true.  Make sure one of them fires
                chkCoupledXMass_CheckedChanged(this, new EventArgs());
            }
        }
        private void chkCoupledXMass_CheckedChanged(object sender, EventArgs e)
        {
            if (chkCoupledXMass.Checked)
            {
                trkEastMass.Value = trkWestMass.Maximum - trkWestMass.Value;
                trkEastMass_Scroll(this, new EventArgs());
            }
            else
            {
                chkCoupledBothMass.Checked = false;
            }
        }
        private void chkCoupledYMass_CheckedChanged(object sender, EventArgs e)
        {
            if (chkCoupledYMass.Checked)
            {
                trkNorthMass.Value = trkSouthMass.Maximum - trkSouthMass.Value;
                trkNorthMass_Scroll(this, new EventArgs());
            }
            else
            {
                chkCoupledBothMass.Checked = false;
            }
        }

        private void trkNorthMass_Scroll(object sender, EventArgs e)
        {

            _north.Mass = UtilityCore.GetScaledValue(MINMASS, MAXMASS, trkNorthMass.Minimum, trkNorthMass.Maximum, trkNorthMass.Maximum - trkNorthMass.Value);

            if (!_isProgramaticPointMass1D && chkCoupledYMass.Checked)
            {
                _isProgramaticPointMass1D = true;

                // Align 1D
                trkSouthMass.Value = trkNorthMass.Maximum - trkNorthMass.Value;
                trkSouthMass_Scroll(this, new EventArgs());

                _isProgramaticPointMass1D = false;

                if (!_isProgramaticPointMass2D && chkCoupledBothMass.Checked)
                {
                    _isProgramaticPointMass2D = true;

                    // Align 2D
                    trkEastMass.Value = trkSouthMass.Value;
                    trkEastMass_Scroll(this, new EventArgs());

                    _isProgramaticPointMass2D = false;
                }
            }

        }
        private void trkSouthMass_Scroll(object sender, EventArgs e)
        {

            _south.Mass = UtilityCore.GetScaledValue(MINMASS, MAXMASS, trkSouthMass.Minimum, trkSouthMass.Maximum, trkSouthMass.Value);

            if (!_isProgramaticPointMass1D && chkCoupledYMass.Checked)
            {
                _isProgramaticPointMass1D = true;

                // Align 1D
                trkNorthMass.Value = trkSouthMass.Maximum - trkSouthMass.Value;
                trkNorthMass_Scroll(this, new EventArgs());

                _isProgramaticPointMass1D = false;

                if (!_isProgramaticPointMass2D && chkCoupledBothMass.Checked)
                {
                    _isProgramaticPointMass2D = true;

                    // Align 2D
                    trkEastMass.Value = trkSouthMass.Value;
                    trkEastMass_Scroll(this, new EventArgs());

                    _isProgramaticPointMass2D = false;
                }
            }

        }
        private void trkEastMass_Scroll(object sender, EventArgs e)
        {

            _east.Mass = UtilityCore.GetScaledValue(MINMASS, MAXMASS, trkEastMass.Minimum, trkEastMass.Maximum, trkEastMass.Value);

            if (!_isProgramaticPointMass1D && chkCoupledXMass.Checked)
            {
                _isProgramaticPointMass1D = true;

                // Align 1D
                trkWestMass.Value = trkEastMass.Maximum - trkEastMass.Value;
                trkWestMass_Scroll(this, new EventArgs());

                _isProgramaticPointMass1D = false;

                if (!_isProgramaticPointMass2D && chkCoupledBothMass.Checked)
                {
                    _isProgramaticPointMass2D = true;

                    // Align 2D
                    trkSouthMass.Value = trkEastMass.Value;
                    trkSouthMass_Scroll(this, new EventArgs());

                    _isProgramaticPointMass2D = false;
                }
            }

        }
        private void trkWestMass_Scroll(object sender, EventArgs e)
        {

            _west.Mass = UtilityCore.GetScaledValue(MINMASS, MAXMASS, trkWestMass.Minimum, trkWestMass.Maximum, trkWestMass.Maximum - trkWestMass.Value);

            if (!_isProgramaticPointMass1D && chkCoupledXMass.Checked)
            {
                _isProgramaticPointMass1D = true;

                // Align 1D
                trkEastMass.Value = trkWestMass.Maximum - trkWestMass.Value;
                trkEastMass_Scroll(this, new EventArgs());

                _isProgramaticPointMass1D = false;

                if (!_isProgramaticPointMass2D && chkCoupledBothMass.Checked)
                {
                    _isProgramaticPointMass2D = true;

                    // Align 2D
                    trkSouthMass.Value = trkEastMass.Value;
                    trkSouthMass_Scroll(this, new EventArgs());

                    _isProgramaticPointMass2D = false;
                }
            }

        }

        #endregion

        private void trkAngularMomentum_Scroll(object sender, EventArgs e)
        {
            double magnitude = UtilityCore.GetScaledValue(MINMOMENTUM, MAXMOMENTUM, trkAngularMomentum.Minimum, trkAngularMomentum.Maximum, trkAngularMomentum.Value);

            _rigidBody.AngularMomentum.StoreNewValues(new MyVector(0, 0, magnitude));

            txtAngularMomentum.Text = _rigidBody.AngularMomentum.Z.ToString();
        }

        private void chkRunning_CheckedChanged(object sender, EventArgs e)
        {
            timer1.Enabled = chkRunning.Checked;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            // Physics
            _rigidBody.PrepareForNewTimerCycle();
            _rigidBody.TimerTestPosition(1);
            _rigidBody.TimerFinish();

            txtAngularVelocity.Text = _rigidBody.AngularVelocity.Z.ToString();

            // Drawing
            pictureBox1.PrepareForNewDraw();

            // North
            MyVector massPosition = _rigidBody.Position + _rigidBody.Rotation.GetRotatedVector(_north.Position, true);
            pictureBox1.FillCircle(_massBrush, massPosition, _north.Mass);
            pictureBox1.DrawCircle(Color.Orange, 10d, massPosition, _north.Mass);

            // South
            massPosition = _rigidBody.Position + _rigidBody.Rotation.GetRotatedVector(_south.Position, true);
            pictureBox1.FillCircle(_massBrush, massPosition, _south.Mass);
            pictureBox1.DrawCircle(Color.Orange, 10d, massPosition, _south.Mass);

            // East
            massPosition = _rigidBody.Position + _rigidBody.Rotation.GetRotatedVector(_east.Position, true);
            pictureBox1.FillCircle(_massBrush, massPosition, _east.Mass);
            pictureBox1.DrawCircle(Color.Orange, 10d, massPosition, _east.Mass);

            // West
            massPosition = _rigidBody.Position + _rigidBody.Rotation.GetRotatedVector(_west.Position, true);
            pictureBox1.FillCircle(_massBrush, massPosition, _west.Mass);
            pictureBox1.DrawCircle(Color.Orange, 10d, massPosition, _west.Mass);

            // Dots
            pictureBox1.FillCircle(_centerPosBrush, _rigidBody.Position, 20);
            pictureBox1.FillCircle(_centerMassBrush, _rigidBody.Position + _rigidBody.Rotation.GetRotatedVector(_rigidBody.CenterOfMass, true), 20);

            pictureBox1.FinishedDrawing();
        }

        #endregion
    }
}