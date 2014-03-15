using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using Game.Orig.Math3D;

namespace Game.Orig.TestersGDI
{
    public partial class RotateAroundPointTester : Form
    {
        #region Declaration Section

        private Bitmap _bitmap = null;
        private Graphics _graphics = null;

        private Sphere _sphere = null;

        private MyVector _offset = new MyVector(40, 40, 0);

        private double _prevRadians = 0;
        private double _currentRadians = 0;
        MyVector _rotationAxis = new MyVector(0, 0, 1);

        #endregion

        #region Constructor

        public RotateAroundPointTester()
        {
            InitializeComponent();

            _bitmap = new Bitmap(pictureBox1.DisplayRectangle.Width, pictureBox1.DisplayRectangle.Height);
            _graphics = Graphics.FromImage(_bitmap);

            btnReset_Click(this, new EventArgs());

            timer1.Enabled = true;
        }

        #endregion

        #region Misc Control Events

        private void btnReset_Click(object sender, EventArgs e)
        {
            _sphere = new Sphere(GetMiddlePoint(), new DoubleVector(0, 1, 0, -1, 0, 0), 200);
            _prevRadians = 0;
            _currentRadians = 0;
            trackBar1.Value = 0;
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            _prevRadians = _currentRadians;
            _currentRadians = Utility3D.GetDegreesToRadians(trackBar1.Value);

            #region Draw Current

            Graphics graphics = pictureBox2.CreateGraphics();
            graphics.Clear(pictureBox1.BackColor);

            DrawVector(graphics, _sphere.Position, _sphere.Position + (_sphere.DirectionFacing.Standard * 100d), Color.White);
            DrawVector(graphics, _sphere.Position, _sphere.Position + (_sphere.DirectionFacing.Orth * 100d), Color.Silver);

            MyVector rotatedOffset = _sphere.Rotation.GetRotatedVector(_offset, true);

            DrawVector(graphics, _sphere.Position, _sphere.Position + rotatedOffset, Color.Orange);
            DrawDot(graphics, _sphere.Position + rotatedOffset, 3, Color.Gold);

            #endregion

            double radians = _currentRadians - _prevRadians;

            if (radOffset.Checked)
            {
                // Remember where the offset is in world coords
                MyVector offsetRotated = _sphere.Rotation.GetRotatedVector(_offset, true);
                MyVector offsetWorld = _sphere.Position + offsetRotated;

                DrawDot(graphics, offsetWorld, 5, Color.DodgerBlue);

                // Get the opposite of the local offset
                MyVector posRelativeToOffset = offsetRotated.Clone();
                posRelativeToOffset.Multiply(-1d);

                // Rotate the center of position around the center of mass
                posRelativeToOffset.RotateAroundAxis(_rotationAxis, radians);

                // Now figure out the new center of position
                _sphere.Position.X = offsetWorld.X + posRelativeToOffset.X;
                _sphere.Position.Y = offsetWorld.Y + posRelativeToOffset.Y;
                _sphere.Position.Z = offsetWorld.Z + posRelativeToOffset.Z;
            }

            _sphere.RotateAroundAxis(_rotationAxis, radians);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            _graphics.Clear(pictureBox1.BackColor);

            DrawVector(_graphics, _sphere.Position, _sphere.Position + (_sphere.DirectionFacing.Standard * 100d), Color.White);
            DrawVector(_graphics, _sphere.Position, _sphere.Position + (_sphere.DirectionFacing.Orth * 100d), Color.Silver);

            MyVector rotatedOffset = _sphere.Rotation.GetRotatedVector(_offset, true);

            DrawVector(_graphics, _sphere.Position, _sphere.Position + rotatedOffset, Color.Orange);
            DrawDot(_graphics, _sphere.Position + rotatedOffset, 3, Color.Gold);

            pictureBox1.CreateGraphics().DrawImageUnscaled(_bitmap, 0, 0);
        }

        #endregion

        #region Private Methods

        private MyVector GetMiddlePoint()
        {
            MyVector retVal = new MyVector();

            retVal.X = pictureBox1.DisplayRectangle.Width / 2d;
            retVal.Y = pictureBox1.DisplayRectangle.Height / 2d;
            retVal.Z = 0d;

            return retVal;
        }

        private static void DrawVector(Graphics graphics, MyVector fromPoint, MyVector toPoint, Color color)
        {
            graphics.DrawLine(new Pen(color), fromPoint.ToPointF(), toPoint.ToPointF());
        }

        private static void DrawDot(Graphics graphics, MyVector centerPoint, double radius, Color color)
        {
            float centerX = Convert.ToSingle(centerPoint.X);
            float centerY = Convert.ToSingle(centerPoint.Y);

            graphics.FillEllipse(new SolidBrush(color), Convert.ToSingle(centerX - radius), Convert.ToSingle(centerY - radius), Convert.ToSingle(radius) * 2f, Convert.ToSingle(radius) * 2f);
        }

        #endregion
    }
}