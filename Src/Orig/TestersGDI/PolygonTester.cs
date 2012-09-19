using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using Game.Orig.HelperClassesOrig;
using Game.Orig.Math3D;

namespace Game.Orig.TestersGDI
{
	public partial class PolygonTester : Form
	{
		#region Declaration Section

		private const double BOUNDRYSIZE = 500d;
		private const double MOMENTUM = 12;

		private Random _rand = new Random();

        private MyVector _boundryLower = new MyVector(BOUNDRYSIZE / -2d, BOUNDRYSIZE / -2d, 0);
        private MyVector _boundryUpper = new MyVector(BOUNDRYSIZE / 2d, BOUNDRYSIZE / 2d, 0);

		private SolidBallPolygon _polygon = null;		//	I need this to get angular velocity

		#endregion

		#region Constructor

		public PolygonTester()
		{
			InitializeComponent();

            pictureBox1.SetBorder(_boundryLower, _boundryUpper);
            pictureBox1.ZoomFit();

			radPolygon_CheckedChanged(this, new EventArgs());

			chkRunning_CheckedChanged(this, new EventArgs());
		}

		#endregion

		#region Misc Control Events

		private void chkRunning_CheckedChanged(object sender, EventArgs e)
		{
			timer1.Enabled = chkRunning.Checked;
		}

		private void radPolygon_CheckedChanged(object sender, EventArgs e)
		{
			const double POLYSIZE = 200d;

			lblNotes.Text = "";

			MyPolygon poly = null;

			//	Get the polygon
			if (radCube.Checked)
			{
                poly = MyPolygon.CreateCube(POLYSIZE, true);
			}
			else if (radTetrahedron.Checked)
			{
                poly = MyPolygon.CreateTetrahedron(POLYSIZE, true);
                lblNotes.Text = "Lengths:\n0,1=" + MyVector.Subtract(poly.UniquePoints[1], poly.UniquePoints[0]).GetMagnitude().ToString() + "\n0,2=" + MyVector.Subtract(poly.UniquePoints[2], poly.UniquePoints[0]).GetMagnitude().ToString() + "\n0,3=" + MyVector.Subtract(poly.UniquePoints[3], poly.UniquePoints[0]).GetMagnitude().ToString() + "\n1,2=" + MyVector.Subtract(poly.UniquePoints[2], poly.UniquePoints[1]).GetMagnitude().ToString() + "\n1,3=" + MyVector.Subtract(poly.UniquePoints[3], poly.UniquePoints[1]).GetMagnitude().ToString() + "\n2,3=" + MyVector.Subtract(poly.UniquePoints[3], poly.UniquePoints[2]).GetMagnitude().ToString();
			}
			else
			{
				_polygon = null;
				MessageBox.Show("Unknown Polygon", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			//	Make a new solidball
            _polygon = new SolidBallPolygon(new MyVector(0, 0, 0), new DoubleVector(1, 0, 0, 0, 1, 0), poly, 10, 10);
		}

		private void btnSpinStop_Click(object sender, EventArgs e)
		{
			if (_polygon == null)
			{
				MessageBox.Show("No polygon to stop", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			_polygon.StopBall();
		}
		private void btnSpinRandom_Click(object sender, EventArgs e)
		{
			if (_polygon == null)
			{
				MessageBox.Show("No polygon to spin", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			_polygon.AngularMomentum.StoreNewValues(Utility3D.GetRandomVector(MOMENTUM));
		}
		private void btnSpinX_Click(object sender, EventArgs e)
		{
			if (_polygon == null)
			{
				MessageBox.Show("No polygon to spin", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			_polygon.AngularMomentum.X = Utility3D.GetNearZeroValue(MOMENTUM);
			_polygon.AngularMomentum.Y = 0;
			_polygon.AngularMomentum.Z = 0;
		}
		private void btnSpinY_Click(object sender, EventArgs e)
		{
			if (_polygon == null)
			{
				MessageBox.Show("No polygon to spin", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			_polygon.AngularMomentum.X = 0;
			_polygon.AngularMomentum.Y = Utility3D.GetNearZeroValue(MOMENTUM);
			_polygon.AngularMomentum.Z = 0;
		}
		private void btnSpinZ_Click(object sender, EventArgs e)
		{
			if (_polygon == null)
			{
				MessageBox.Show("No polygon to spin", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			_polygon.AngularMomentum.X = 0;
			_polygon.AngularMomentum.Y = 0;
			_polygon.AngularMomentum.Z = Utility3D.GetNearZeroValue(MOMENTUM);
		}

		private void timer1_Tick(object sender, EventArgs e)
		{
			//	Physics
			if (_polygon != null)
			{
				_polygon.PrepareForNewTimerCycle();
				_polygon.TimerTestPosition(1d);
				_polygon.TimerFinish();
			}

			//	Drawing
			pictureBox1.PrepareForNewDraw();

			if (_polygon != null)
			{
                pictureBox1.FillPolygon(Color.RoyalBlue, Color.DodgerBlue, _polygon.Position, _polygon);

                pictureBox1.FillCircle(Color.Black, _polygon.Position + _polygon.UniquePoints[0], 5);
                pictureBox1.FillCircle(Color.Red, _polygon.Position + _polygon.UniquePoints[1], 5);
                pictureBox1.FillCircle(Color.Green, _polygon.Position + _polygon.UniquePoints[2], 5);
                pictureBox1.FillCircle(Color.White, _polygon.Position + _polygon.UniquePoints[3], 5);
			}

            pictureBox1.FinishedDrawing();
		}

		#endregion
	}
}