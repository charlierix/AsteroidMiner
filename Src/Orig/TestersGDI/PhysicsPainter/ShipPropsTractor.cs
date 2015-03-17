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
    public partial class ShipPropsTractor : Game.Orig.HelperClassesGDI.Controls.PiePanelBottom
    {
        #region Declaration Section

        //TODO: Make soft a boolean (except for default)
        private const string TYPE_OVERLAID = "Overlaid";
        private const string TYPE_INOUT = "In/Out";
        private const string TYPE_INOUTSOFT = "In/Out - soft";
        private const string TYPE_LEFTRIGHT = "Left/Right";
        private const string TYPE_LEFTRIGHTSOFT = "Left/Right - soft";
        private const string TYPE_DISTANTCIRCLE = "Distant Circle";
        private const string TYPE_DISTANTCIRCLESOFT = "Distant Circle - soft";

        //private const double TRACTOR_MAXFORCEPERTICK = ShipController.THRUSTER_FORCE * 2d;

        private ShipController _shipController = null;
        private SimpleMap _map = null;

        private double _strengthMax = 0;
        private double _strengthNear = 0;
        private double _strengthFar = 0;

        private double _sizeMax = 0;
        private double _sizeActual = 0;

        private double _sweepAngle = 45;

        #endregion

        #region Constructor

        public ShipPropsTractor()
        {
            InitializeComponent();

            cboType.Items.Add(TYPE_OVERLAID);
            cboType.Items.Add(TYPE_INOUT);
            cboType.Items.Add(TYPE_INOUTSOFT);
            cboType.Items.Add(TYPE_LEFTRIGHT);
            cboType.Items.Add(TYPE_LEFTRIGHTSOFT);
            cboType.Items.Add(TYPE_DISTANTCIRCLE);
            cboType.Items.Add(TYPE_DISTANTCIRCLESOFT);

            cboType.Text = TYPE_INOUT;
        }

        #endregion

        #region Public Methods

        public void SetPointers(ShipController shipController, SimpleMap map)
        {
            _shipController = shipController;
            _map = map;

            _shipController.CreateNewTractorBeams += new EventHandler(ShipController_CreateNewTractorBeams);
            _shipController.RecalcTractorBeamOffsets += new EventHandler(ShipController_RecalcTractorBeamOffsets);
            _shipController.ChangeTractorBeamPower += new EventHandler(ShipController_ChangeTractorBeamPower);

            // Apply Settings
            cboType_SelectedIndexChanged(this, new EventArgs());
            txtStrengthMax_TextChanged(this, new EventArgs());		// this method calls the trackbar methods
            txtMaxSize_TextChanged(this, new EventArgs());		// this method calls the trackbar's method
            trkSweepAngle_Scroll(this, new EventArgs());
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

            if (cboType.Text == TYPE_DISTANTCIRCLE || cboType.Text == TYPE_DISTANTCIRCLESOFT)
            {
                trkSweepAngle.Enabled = false;
            }
            else
            {
                trkSweepAngle.Enabled = true;
            }

            // Reset Tractors
            ShipController_CreateNewTractorBeams(this, new EventArgs());
            ShipController_RecalcTractorBeamOffsets(this, new EventArgs());
            ShipController_ChangeTractorBeamPower(this, new EventArgs());
        }

        private void txtStrengthMax_TextChanged(object sender, EventArgs e)
        {
            double strength;
            if (!double.TryParse(txtStrengthMax.Text, out strength))
            {
                txtStrengthMax.ForeColor = Color.Red;
                return;
            }

            txtStrengthMax.ForeColor = SystemColors.WindowText;
            _strengthMax = strength;

            trkStrengthNear_Scroll(this, new EventArgs());
            trkStrengthFar_Scroll(this, new EventArgs());
        }
        private void trkStrengthNear_Scroll(object sender, EventArgs e)
        {
            _strengthNear = UtilityCore.GetScaledValue(0, _strengthMax, trkStrengthNear.Minimum, trkStrengthNear.Maximum, trkStrengthNear.Value);

            toolTip1.SetToolTip(trkStrengthNear, _strengthNear.ToString());

            // Apply to tractor beams
            foreach (TractorBeamCone tractor in _shipController.TractorBeams)
            {
                tractor.ForceAtZero = _strengthNear;
            }

            ShipController_ChangeTractorBeamPower(this, new EventArgs());
        }
        private void trkStrengthFar_Scroll(object sender, EventArgs e)
        {
            _strengthFar = UtilityCore.GetScaledValue(0, _strengthMax, trkStrengthFar.Minimum, trkStrengthFar.Maximum, trkStrengthFar.Value);

            toolTip1.SetToolTip(trkStrengthFar, _strengthFar.ToString());

            // Apply to tractor beams
            foreach (TractorBeamCone tractor in _shipController.TractorBeams)
            {
                tractor.ForceAtMax = _strengthFar;
            }

            ShipController_ChangeTractorBeamPower(this, new EventArgs());
        }

        private void txtMaxSize_TextChanged(object sender, EventArgs e)
        {
            double size;
            if (!double.TryParse(txtMaxSize.Text, out size))
            {
                txtMaxSize.ForeColor = Color.Red;
                return;
            }

            txtMaxSize.ForeColor = SystemColors.WindowText;
            _sizeMax = size;

            trkSize_Scroll(this, new EventArgs());
        }
        private void trkSize_Scroll(object sender, EventArgs e)
        {
            _sizeActual = UtilityCore.GetScaledValue(100, _sizeMax, trkSize.Minimum, trkSize.Maximum, trkSize.Value);

            toolTip1.SetToolTip(trkSize, _sizeActual.ToString());

            bool changeOffset = false;

            // Apply to tractor beams
            foreach (TractorBeamCone tractor in _shipController.TractorBeams)
            {
                if (cboType.Text == TYPE_DISTANTCIRCLE || cboType.Text == TYPE_DISTANTCIRCLESOFT)
                {
                    changeOffset = true;

                    tractor.MaxDistance = _sizeActual / 3d;
                }
                else
                {
                    tractor.MaxDistance = _sizeActual;
                }
            }

            // Distant circle needs the offset changed
            if (changeOffset)
            {
                ShipController_RecalcTractorBeamOffsets(this, new EventArgs());
            }
        }

        private void trkSweepAngle_Scroll(object sender, EventArgs e)
        {
            toolTip1.SetToolTip(trkSweepAngle, trkSweepAngle.Value.ToString());

            _sweepAngle = Utility3D.GetDegreesToRadians(trkSweepAngle.Value);

            foreach (TractorBeamCone tractor in _shipController.TractorBeams)
            {
                tractor.SweepAngle = _sweepAngle;
            }
        }

        private void ShipController_CreateNewTractorBeams(object sender, EventArgs e)
        {
            _shipController.TractorBeams.Clear();

            if (_shipController.ShipType == ShipController.ShipTypeQual.None)
            {
                return;
            }

            switch (cboType.Text)
            {
                case TYPE_OVERLAID:
                    CreateNewTractor(TractorBeamCone.BeamMode.PushPull, false, _sweepAngle, _sizeActual, _strengthNear, _strengthFar);
                    CreateNewTractor(TractorBeamCone.BeamMode.LeftRight, false, _sweepAngle, _sizeActual, _strengthNear / 5d, _strengthFar / 5d);
                    break;

                case TYPE_INOUT:
                    CreateNewTractor(TractorBeamCone.BeamMode.PushPull, false, _sweepAngle, _sizeActual, _strengthNear, _strengthFar);
                    break;
                case TYPE_INOUTSOFT:
                    CreateNewTractor(TractorBeamCone.BeamMode.PushPull, true, _sweepAngle, _sizeActual, _strengthNear, _strengthFar);
                    break;

                case TYPE_LEFTRIGHT:
                    CreateNewTractor(TractorBeamCone.BeamMode.LeftRight, false, _sweepAngle, _sizeActual, _strengthNear, _strengthFar);		// may want to divide by 5
                    break;
                case TYPE_LEFTRIGHTSOFT:
                    CreateNewTractor(TractorBeamCone.BeamMode.LeftRight, true, _sweepAngle, _sizeActual, _strengthNear, _strengthFar);
                    break;

                case TYPE_DISTANTCIRCLE:
                    CreateNewTractor(TractorBeamCone.BeamMode.PushPull, false, Math.PI * 2d, _sizeActual / 3d, _strengthNear, _strengthFar);
                    break;
                case TYPE_DISTANTCIRCLESOFT:
                    CreateNewTractor(TractorBeamCone.BeamMode.PushPull, true, Math.PI * 2d, _sizeActual / 3d, _strengthNear, _strengthFar);
                    break;

                default:
                    throw new ApplicationException("Unknown Type: " + cboType.Text);
            }
        }
        private void ShipController_RecalcTractorBeamOffsets(object sender, EventArgs e)
        {
            // Figure out tractor beam placement
            foreach (TractorBeamCone tractor in _shipController.TractorBeams)
            {
                if (cboType.Text == TYPE_DISTANTCIRCLE || cboType.Text == TYPE_DISTANTCIRCLESOFT)
                {
                    #region Distant Circle

                    if (_shipController.ShipType != ShipController.ShipTypeQual.None)
                    {
                        double offsetLength = _shipController.Ship.Ball.Radius;
                        offsetLength += tractor.MaxDistance * 2d;

                        tractor.Offset = _shipController.Ship.Ball.OriginalDirectionFacing.Standard * offsetLength;    // orig dir facing is length 1
                    }

                    #endregion
                }
                else
                {
                    #region Standard Cone

                    switch (_shipController.ShipType)
                    {
                        case ShipController.ShipTypeQual.None:
                            // Nothing to do
                            break;

                        case ShipController.ShipTypeQual.Ball:
                            tractor.Offset = new MyVector(0, 0, 0);
                            break;

                        case ShipController.ShipTypeQual.SolidBall:
                            tractor.Offset = _shipController.Ship.Ball.OriginalDirectionFacing.Standard * _shipController.Ship.Ball.Radius;    // orig dir facing is length 1
                            break;
                    }

                    #endregion
                }
            }
        }
        private void ShipController_ChangeTractorBeamPower(object sender, EventArgs e)
        {
            double max = _strengthFar;
            if (_strengthNear > _strengthFar)
            {
                max = _strengthNear;
            }

            foreach (TractorBeamCone tractor in _shipController.TractorBeams)
            {
                switch (_shipController.PowerLevel)
                {
                    case 1:
                        tractor.MaxForcePerTick = max * .5d;
                        break;

                    case 2:
                        tractor.MaxForcePerTick = max;
                        break;

                    case 3:
                        tractor.MaxForcePerTick = max * 2d;
                        break;

                    default:		// 4
                        tractor.MaxForcePerTick = double.MaxValue;
                        break;
                }
            }
        }

        #endregion

        #region Private Methods

        private void CreateNewTractor(TractorBeamCone.BeamMode mode, bool isSoft, double sweepAngle, double maxDistance, double forceAtZero, double forceAtMax)
        {
            // New tractor beam (I'm not worried about the offset, it always gets reset in the other event) (also max force per tick)
            TractorBeamCone tractor = new TractorBeamCone(_map, _shipController.Ship, new MyVector(0, 0, 0), _shipController.Ship.Ball.OriginalDirectionFacing.Clone(), mode, isSoft, sweepAngle, maxDistance, forceAtZero, forceAtMax, 0);

            _shipController.TractorBeams.Add(tractor);
        }

        #endregion
    }
}
