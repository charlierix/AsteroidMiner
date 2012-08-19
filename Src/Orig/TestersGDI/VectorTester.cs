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
    public partial class VectorTester : Form
    {
        #region Constructor

        public VectorTester()
        {
            InitializeComponent();
        }

        #endregion

        #region Event Listeners

        private void button1_Click(object sender, EventArgs e)
        {
            MyVector v1 = new MyVector(3, 4, 5);

            v1.Add(1, 2, 3);

            v1.BecomeUnitVector();

            MyVector v2 = v1.Clone();

            v2.Multiply(3);

            v1.Divide(3);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            MyVector v1 = new MyVector(1, 0, 0);
            MyVector v2 = new MyVector(0, 1, 0);

            MyVector rotationAxis;
            double radians;
            v1.GetAngleAroundAxis(out rotationAxis, out radians, v2);

            v2.GetAngleAroundAxis(out rotationAxis, out radians, v1);
        }

		private void button3_Click(object sender, EventArgs e)
		{
			ClearPictureBox();

            //	Setup Orig Vector
            MyVector origVector = new MyVector(9, 0, 0);
			DrawVector(origVector, Color.Silver);

			#region Single Rotation

			//	Setup quat to do the whole rotation in one shot
            MyQuaternion largeRotationQuat = new MyQuaternion(new MyVector(0, 0, 1), Utility3D.GetDegreesToRadians(90));
            MyVector largeRotation = largeRotationQuat.GetRotatedVector(origVector, true);
			DrawVector(largeRotation, Color.White);

			#endregion

			#region Multi Rotations

			//	Setup quat that holds a 30 degree angle
            MyQuaternion multiRotationQuat = new MyQuaternion(new MyVector(0, 0, 1), Utility3D.GetDegreesToRadians(30));
            MyVector subRotation = multiRotationQuat.GetRotatedVector(origVector, true);
			DrawVector(subRotation, Color.Orange);


			//	Setup a quat that holds a 1 degree angle
            MyQuaternion anotherRotationQuat = new MyQuaternion(new MyVector(0, 0, 1), Utility3D.GetDegreesToRadians(1));
			
			//	Apply this to the multi quat 60 times
			for (int cntr = 1; cntr <= 60; cntr++)
			{
                multiRotationQuat = MyQuaternion.Multiply(anotherRotationQuat, multiRotationQuat);
			}

			//	Let's see what happened
			subRotation = multiRotationQuat.GetRotatedVector(origVector, true);
			DrawVector(subRotation, Color.HotPink);

			#endregion
			#region Multi Rotations (3D)
            /*
			//	Rotate around Y
			multiRotationQuat = new MyQuaternion(new MyVector(0, 1, 0), Utility3D.GetDegreesToRadians(90));

			//	Rotate around X
			anotherRotationQuat = new MyQuaternion(new MyVector(1, 0, 0), Utility3D.GetDegreesToRadians(90));
			multiRotationQuat = MyQuaternion.Multiply(anotherRotationQuat, multiRotationQuat);

			//	Draw the final output
			subRotation = multiRotationQuat.GetRotatedVector(origVector, true);
			DrawVector(subRotation, Color.Yellow);
			*/
            #endregion
            #region Multi Rotations (Vector3D)
            /*
			subRotation = origVector.Clone();

			subRotation.RotateAroundAxis(new MyVector(0, 1, 0), Utility3D.GetDegreesToRadians(90));
			subRotation.RotateAroundAxis(new MyVector(1, 0, 0), Utility3D.GetDegreesToRadians(90));

			DrawVector(subRotation, Color.Yellow);
			*/
            #endregion
        }

		private void button4_Click(object sender, EventArgs e)
		{
			ClearPictureBox();

            //	Setup Orig Vector
            MyVector origVector = new MyVector(9, 0, 0);
			DrawVector(origVector, Color.Silver);

            MyQuaternion multiRotationQuat = new MyQuaternion(new MyVector(0, 0, 0), Utility3D.GetDegreesToRadians(0));

			//	Rotate around Z
            MyQuaternion anotherRotationQuat = new MyQuaternion(new MyVector(0, 0, 1), Utility3D.GetDegreesToRadians(1));

			//List<double> lengths = new List<double>();

			for (int outerCntr = 1; outerCntr <= 100000; outerCntr++)
			{
				//lengths.Add(multiRotationQuat.GetMagnitude());
				for (int innerCntr = 1; innerCntr <= 360; innerCntr++)
				{
                    multiRotationQuat = MyQuaternion.Multiply(anotherRotationQuat, multiRotationQuat);
				}
				//multiRotationQuat.BecomeUnitQuaternion();
			}

			//	Draw the final output
            MyVector subRotation = multiRotationQuat.GetRotatedVector(origVector, true);
			DrawVector(subRotation, Color.Yellow);
		}

		private void btnRotationMatrix_Click(object sender, EventArgs e)
		{
			ClearPictureBox();

            //	Setup Orig Vector
            MyVector origVector = new MyVector(9, 0, 0);
			DrawVector(origVector, Color.Silver);

			//	Rotate around Z
            MyQuaternion rotationQuat = new MyQuaternion(new MyVector(0, 0, 1), Utility3D.GetDegreesToRadians(30));

            MyVector rotated = rotationQuat.GetRotatedVector(origVector, true);
			DrawVector(rotated, Color.Black);

			MyMatrix3 rotationMatrix;
			for (int cntr = 1; cntr <= 10000000; cntr++)
			{
				rotationMatrix = rotationQuat.ToMatrix3FromUnitQuaternion();

				rotationQuat = null;
                rotationQuat = new MyQuaternion();
				rotationQuat.FromRotationMatrix(rotationMatrix);

				rotationMatrix = null;
			}


			rotated = rotationQuat.GetRotatedVector(origVector, true);
			DrawVector(rotated, Color.DodgerBlue);

			rotationQuat.W *= -1;
            MyVector rotatedNegated = rotationQuat.GetRotatedVector(origVector, true);
			DrawVector(rotatedNegated, Color.Yellow);
		}
		private void button5_Click(object sender, EventArgs e)
		{



			//	This button tests TorqueBall.OrthonormalizeOrientation




			ClearPictureBox();

			//	Setup Orig Vector
            MyVector origVector = new MyVector(9, 0, 0);
			DrawVector(origVector, Color.Silver);

			//	Rotate around Z
            MyQuaternion rotationQuat = new MyQuaternion(new MyVector(0, 0, 1), Utility3D.GetDegreesToRadians(30));

            MyVector rotated = rotationQuat.GetRotatedVector(origVector, true);
			DrawVector(rotated, Color.Black);

            MyMatrix3 rotationMatrix = rotationQuat.ToMatrix3FromUnitQuaternion();




			//	See if this affects the rotation matrix
			TorqueBall.OrthonormalizeOrientation(rotationMatrix);





			rotationQuat = null;
            rotationQuat = new MyQuaternion();
			rotationQuat.FromRotationMatrix(rotationMatrix);

			rotationMatrix = null;


			//	Draw the results
			rotated = rotationQuat.GetRotatedVector(origVector, true);
			DrawVector(rotated, Color.DodgerBlue);
		}

        private void trackBar_Scroll(object sender, EventArgs e)
        {
            // Figure out the vector to rotate around
            MyVector rotateAround = new MyVector(trkXAxis.Value, trkYAxis.Value, trkZAxis.Value);

            if (rotateAround.X == 0 && rotateAround.Y == 0 && rotateAround.Z == 0)
            {
                pictureBox1.CreateGraphics().Clear(Color.Tomato);
                return;
            }

            // Rotate a vector
            MyVector rotatedVector = new MyVector(9, 0, 0);
            rotatedVector.RotateAroundAxis(rotateAround, Utility3D.GetDegreesToRadians(trackBar1.Value));

            // Draw it
            ClearPictureBox();
            DrawVector(rotateAround, Color.LightSteelBlue);
            DrawVector(rotatedVector, Color.GhostWhite);
        }

        #region Simple Draw Vector Buttons

        private void btnUp_Click(object sender, EventArgs e)
        {
            ClearPictureBox();
            DrawVector(new MyVector(0, 10, 0), Color.HotPink);
        }
        private void btnUpRight_Click(object sender, EventArgs e)
        {
            ClearPictureBox();
            DrawVector(new MyVector(10, 10, 0), Color.HotPink);
        }
        private void btnRight_Click(object sender, EventArgs e)
        {
            ClearPictureBox();
            DrawVector(new MyVector(10, 0, 0), Color.HotPink);
        }
        private void btnDownRight_Click(object sender, EventArgs e)
        {
            ClearPictureBox();
            DrawVector(new MyVector(10, -10, 0), Color.HotPink);
        }
        private void btnDown_Click(object sender, EventArgs e)
        {
            ClearPictureBox();
            DrawVector(new MyVector(0, -10, 0), Color.HotPink);
        }
        private void btnDownLeft_Click(object sender, EventArgs e)
        {
            ClearPictureBox();
            DrawVector(new MyVector(-10, -10, 0), Color.HotPink);
        }
        private void btnLeft_Click(object sender, EventArgs e)
        {
            ClearPictureBox();
            DrawVector(new MyVector(-10, 0, 0), Color.HotPink);
        }
        private void btnUpLeft_Click(object sender, EventArgs e)
        {
            ClearPictureBox();
            DrawVector(new MyVector(-10, 10, 0), Color.HotPink);
        }

        #endregion

        #endregion

        #region Private Methods

        private void ClearPictureBox()
        {
            pictureBox1.CreateGraphics().Clear(pictureBox1.BackColor);
        }

        /// <summary>
        /// This function draws the vector on the middle of the picturebox (from the middle to the vector coord)
        /// The picturebox is scaled to -10 to 10
        /// </summary>
        private void DrawVector(MyVector vector, Color color)
        {
            const double SCALEFACTOR = 500d / 20d;

            MyVector scaledVector = vector * SCALEFACTOR;
            scaledVector.Y *= -1;      // Invert Y
            scaledVector.Add(250, 250, 0);

            pictureBox1.CreateGraphics().DrawLine(new Pen(color, 5), new Point(250, 250), scaledVector.ToPoint());
        }

        #endregion
    }
}