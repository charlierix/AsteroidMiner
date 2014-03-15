using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Orig.Math3D
{
    /// <summary>
    /// For rotations, xyz is the vector, and w is the angle to rotate around that vector
    /// </summary>
    /// <remarks>
    /// I'm not too familiar with quaternions.  From what I've read, all you really need is a 4x4 matrix, but the examples I'm
    /// seeing use quaternions, so maybe it's easier to wrap your head around (or maybe some effieciency reason?)
    /// 
    /// This is equivelent to System.Windows.Media.Media3D.Quaternion
    /// </remarks>
    public class MyQuaternion
    {
        #region Declaration Section

        public double X = 0;
        public double Y = 0;
        public double Z = 0;
        public double W = 0;

        #endregion

        #region Constructor

        public MyQuaternion()
        {
        }
        public MyQuaternion(double x, double y, double z, double w)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.W = w;
        }
        /// <summary>
        /// This constructs a unit quaternion that represents the rotation
        /// </summary>
        public MyQuaternion(MyVector rotationAxis, double radians)
        {
            double halfAngle = radians / 2d;
            double sinHalfAngle = Math.Sin(halfAngle);
            MyVector rotateAroundUnit = MyVector.BecomeUnitVector(rotationAxis);

            // Set my values
            this.X = rotateAroundUnit.X * sinHalfAngle;
            this.Y = rotateAroundUnit.Y * sinHalfAngle;
            this.Z = rotateAroundUnit.Z * sinHalfAngle;
            this.W = Math.Cos(halfAngle);
        }

        #endregion

        #region Public Methods

        public MyQuaternion Clone()
        {
            return new MyQuaternion(this.X, this.Y, this.Z, this.W);
        }

        public double GetMagnitude()
        {
            return Math.Sqrt((this.X * this.X) + (this.Y * this.Y) + (this.Z * this.Z) + (this.W * this.W));
        }
        public double GetMagnitudeSqured()
        {
            return (this.X * this.X) + (this.Y * this.Y) + (this.Z * this.Z) + (this.W * this.W);
        }

        public void BecomeUnitQuaternion()
        {
            double length = this.GetMagnitude();

            this.X /= length;
            this.Y /= length;
            this.Z /= length;
            this.W /= length;
        }

        /// <summary>
        /// There are classes that expose quaternions as read only properties (so the pointers can be shared).  I just made
        /// this function so it's easier to shuttle values around in one shot
        /// </summary>
        public void StoreNewValues(MyQuaternion valuesToGrab)
        {
            this.X = valuesToGrab.X;
            this.Y = valuesToGrab.Y;
            this.Z = valuesToGrab.Z;
            this.W = valuesToGrab.W;
        }

        /// <summary>
        /// This function creates a vector that is the vector passed in rotated by my definition
        /// </summary>
        /// <param name="vector">The vector to rotate (I don't touch this vector, I create a new one that is rotated)</param>
        /// <param name="isQuatNormalized">Whether this class is already normalized or not (if you don't know, pass false)</param>
        public MyVector GetRotatedVector(MyVector vector, bool isQuatNormalized)
        {
            if (!isQuatNormalized)
            {
                // I'm not normalized, clone myself and normalize it
                MyQuaternion myUnitClone = new MyQuaternion(this.X, this.Y, this.Z, this.W);
                myUnitClone.BecomeUnitQuaternion();

                return myUnitClone.GetRotatedVector(vector, true);
            }

            MyVector qvec = new MyVector(this.X, this.Y, this.Z);

            //Vector uv = qvec.Cross(vector);
            MyVector uv = MyVector.Cross(qvec, vector);

            //Vector uuv = qvec.Cross(uv);
            MyVector uuv = MyVector.Cross(qvec, uv);

            //uv *= (2.0f * quat.w);
            uv.Multiply(this.W * 2d);

            //uuv *= 2.0f;
            uuv.Multiply(2d);

            //return vector + uv + uuv;
            MyVector retVal = vector.Clone();
            retVal.Add(uv);
            retVal.Add(uuv);
            return retVal;
        }
        public DoubleVector GetRotatedVector(DoubleVector doubleVector, bool isQuatNormalized)
        {
            if (!isQuatNormalized)
            {
                // I'm not normalized, clone myself and normalize it
                MyQuaternion myUnitClone = new MyQuaternion(this.X, this.Y, this.Z, this.W);
                myUnitClone.BecomeUnitQuaternion();

                return myUnitClone.GetRotatedVector(doubleVector, true);
            }

            return new DoubleVector(GetRotatedVector(doubleVector.Standard, true), GetRotatedVector(doubleVector.Orth, true));
        }
        /// <summary>
        /// This function returns a vector that is rotated by the opposite of me
        /// </summary>
        public MyVector GetRotatedVectorReverse(MyVector vector, bool isQuatNormalized)
        {
            if (!isQuatNormalized)
            {
                // I'm not normalized, clone myself and normalize it
                MyQuaternion myUnitClone = new MyQuaternion(this.X, this.Y, this.Z, this.W);
                myUnitClone.BecomeUnitQuaternion();

                return myUnitClone.GetRotatedVectorReverse(vector, true);
            }

            MyVector qvec = new MyVector(this.X, this.Y, this.Z);

            //Vector uv = qvec.Cross(vector);
            MyVector uv = MyVector.Cross(qvec, vector);

            //Vector uuv = qvec.Cross(uv);
            MyVector uuv = MyVector.Cross(qvec, uv);

            //uv *= (2.0f * quat.w);
            uv.Multiply(this.W * -2d);

            //uuv *= 2.0f;
            uuv.Multiply(2d);

            //return vector + uv + uuv;
            MyVector retVal = vector.Clone();
            retVal.Add(uv);
            retVal.Add(uuv);
            return retVal;
        }
        public DoubleVector GetRotatedVectorReverse(DoubleVector doubleVector, bool isQuatNormalized)
        {
            if (!isQuatNormalized)
            {
                // I'm not normalized, clone myself and normalize it
                MyQuaternion myUnitClone = new MyQuaternion(this.X, this.Y, this.Z, this.W);
                myUnitClone.BecomeUnitQuaternion();

                return myUnitClone.GetRotatedVectorReverse(doubleVector, true);
            }

            return new DoubleVector(GetRotatedVectorReverse(doubleVector.Standard, true), GetRotatedVectorReverse(doubleVector.Orth, true));
        }

        public static MyQuaternion Multiply(MyQuaternion q1, MyQuaternion q2)
        {
            MyQuaternion retVal = new MyQuaternion();

            retVal.W = (q1.W * q2.W) - (q1.X * q2.X) - (q1.Y * q2.Y) - (q1.Z * q2.Z);
            retVal.X = (q1.W * q2.X) + (q1.X * q2.W) + (q1.Y * q2.Z) - (q1.Z * q2.Y);
            retVal.Y = (q1.W * q2.Y) - (q1.X * q2.Z) + (q1.Y * q2.W) + (q1.Z * q2.X);
            retVal.Z = (q1.W * q2.Z) + (q1.X * q2.Y) - (q1.Y * q2.X) + (q1.Z * q2.W);

            return retVal;
        }
        public static MyVector Multiply(MyQuaternion quat, MyVector vector)
        {
            // nVidia SDK implementation
            MyVector uv, uuv;
            MyVector qvec = new MyVector(quat.X, quat.Y, quat.Z);

            uv = MyVector.Cross(qvec, vector);
            uuv = MyVector.Cross(qvec, uv);
            uv.Multiply(2.0d * quat.W);
            uuv.Multiply(2.0d);

            // Add them together
            return vector + uv + uuv;

            // get the rotation matrix of the Quaternion and multiply it times the vector
            //return quat.ToRotationMatrix() * vector;
        }

        public void FromRotationMatrix(MyMatrix3 matrix)
        {
            double trace = matrix.M11 + matrix.M22 + matrix.M33 + 1;
            double s;		// I'm not sure what s should be called

            if (trace > 0d)
            {
                #region Instant Calculation

                s = Math.Sqrt(trace) * 2d;

                this.W = 0.25d * s;		// the other had this .25/s
                this.X = (matrix.M32 - matrix.M23) / s;
                this.Y = (matrix.M13 - matrix.M31) / s;
                this.Z = (matrix.M21 - matrix.M12) / s;

                #endregion
            }
            else
            {
                // Find the major diagonal element with the greatest value
                if (matrix.M11 > matrix.M22 && matrix.M11 > matrix.M33)
                {
                    #region Column 0

                    s = Math.Sqrt(1d + matrix.M11 - matrix.M22 - matrix.M33) * 2d;
                    this.X = 0.25d * s;
                    this.Y = (matrix.M21 + matrix.M12) / s;
                    this.Z = (matrix.M13 + matrix.M31) / s;
                    this.W = (matrix.M32 - matrix.M23) / s;

                    /*
                    s = Math.Sqrt(1d + matrix.M11 - matrix.M22 - matrix.M33) * 2d;

                    this.X = 0.5d / s;
                    this.Y = (matrix.M21 + matrix.M12) / s;
                    this.Z = (matrix.M32 + matrix.M13) / s;
                    this.W = (matrix.M32 + matrix.M23) / s;
                    */

                    #endregion
                }
                else if (matrix.M22 > matrix.M11 && matrix.M22 > matrix.M33)
                {
                    #region Column 1

                    s = Math.Sqrt(1d + matrix.M22 - matrix.M11 - matrix.M33) * 2d;

                    this.X = (matrix.M21 + matrix.M12) / s;
                    this.Y = 0.25d * s;
                    this.Z = (matrix.M32 + matrix.M23) / s;
                    this.W = (matrix.M13 - matrix.M31) / s;

                    /*
                    s = Math.Sqrt(1d + matrix.M22 - matrix.M11 - matrix.M33) * 2d;

                    this.X = (matrix.M21 + matrix.M12) / s;
                    this.Y = 0.5d / s;
                    this.Z = (matrix.M32 + matrix.M23) / s;
                    this.W = (matrix.M31 + matrix.M13) / s;
                    */

                    #endregion
                }
                else
                {
                    #region Column 2

                    s = Math.Sqrt(1d + matrix.M33 - matrix.M11 - matrix.M22) * 2d;
                    this.X = (matrix.M13 + matrix.M31) / s;
                    this.Y = (matrix.M32 + matrix.M23) / s;
                    this.Z = 0.25d * s;
                    this.W = (matrix.M21 - matrix.M12) / s;

                    /*
                    s = Math.Sqrt(1d + matrix.M33 - matrix.M11 - matrix.M22) * 2d;

                    this.X = (matrix.M31 + matrix.M13) / s;
                    this.Y = (matrix.M32 + matrix.M23) / s;
                    this.Z = 0.5d / s;
                    this.W = (matrix.M21 + matrix.M12) / s;
                    */

                    #endregion
                }
            }

            // This makes me feel better
            this.BecomeUnitQuaternion();

            #region Reading Material
            /*

						Q48. How do I convert a rotation matrix to a quaternion?
			--------------------------------------------------------

			  A rotation may be converted back to a quaternion through the use of
			  the following algorithm:

			  The process is performed in the following stages, which are as follows:

				Calculate the trace of the matrix T from the equation:

							2     2     2
				  T = 4 - 4x  - 4y  - 4z

							 2    2    2
					= 4( 1 -x  - y  - z )

					= mat[0] + mat[5] + mat[10] + 1


				If the trace of the matrix is greater than zero, then
				perform an "instant" calculation.

				  S = 0.5 / sqrt(T)

				  W = 0.25 / S

				  X = ( mat[9] - mat[6] ) * S

				  Y = ( mat[2] - mat[8] ) * S

				  Z = ( mat[4] - mat[1] ) * S
			*/
            #endregion
            #region More Work
            /*
				If the trace of the matrix is less than or equal to zero
				then identify which major diagonal element has the greatest
				value.

				Depending on this value, calculate the following:

				  Column 0:
					S  = sqrt( 1.0 + mr[0] - mr[5] - mr[10] ) * 2;

					Qx = 0.5 / S;
					Qy = (mr[1] + mr[4] ) / S;
					Qz = (mr[2] + mr[8] ) / S;
					Qw = (mr[6] + mr[9] ) / S;

				  Column 1:
					S  = sqrt( 1.0 + mr[5] - mr[0] - mr[10] ) * 2;

					Qx = (mr[1] + mr[4] ) / S;
					Qy = 0.5 / S;
					Qz = (mr[6] + mr[9] ) / S;
					Qw = (mr[2] + mr[8] ) / S;

				  Column 2:
					S  = sqrt( 1.0 + mr[10] - mr[0] - mr[5] ) * 2;

					Qx = (mr[2] + mr[8] ) / S;
					Qy = (mr[6] + mr[9] ) / S;
					Qz = 0.5 / S;
					Qw = (mr[1] + mr[4] ) / S;

				 The quaternion is then defined as:

				   Q = | Qx Qy Qz Qw |

			*/
            #endregion
        }

        /// <remarks>
        /// This function assumes that the quaternion has a magnitude of one.  (it's optimized for that)
        /// 
        /// I still have no clue what should be done with this matrix, but here it is
        /// 
        /// I copied the code from the other overload, because I want absolute performance
        /// </remarks>
        public MyMatrix3 ToMatrix3FromUnitQuaternion()
        {
            MyMatrix3 retVal = new MyMatrix3();

            // All these cached results are used at least twice, so I'm not being wastefull
            double xx = this.X * this.X;
            double yy = this.Y * this.Y;
            double zz = this.Z * this.Z;

            double xy = this.X * this.Y;
            double xz = this.X * this.Z;

            double yz = this.Y * this.Z;

            double wx = this.W * this.X;
            double wy = this.W * this.Y;
            double wz = this.W * this.Z;

            retVal.M11 = 1 - 2 * (yy + zz);
            retVal.M12 = 2 * (xy - wz);
            retVal.M13 = 2 * (xz + wy);

            retVal.M21 = 2 * (xy + wz);
            retVal.M22 = 1 - 2 * (xx + zz);
            retVal.M23 = 2 * (yz - wx);

            retVal.M31 = 2 * (xz - wy);
            retVal.M32 = 2 * (yz + wx);
            retVal.M33 = 1 - 2 * (xx + yy);

            #region Old
            /*
			// Row 1
			retVal.M11 = 1 - ((2 * yy) - (2 * zz));
			retVal.M12 = (2 * xy) - (2 * wz);
			retVal.M13 = (2 * xz) + (2 * wy);

			// Row 2
			retVal.M21 = (2 * xy) + (2 * wz);
			retVal.M22 = 1 - ((2 * xx) - (2 * zz));
			retVal.M23 = (2 * yz) - (2 * wz);

			// Row 3
			retVal.M31 = (2 * xz) - (2 * wy);
			retVal.M32 = (2 * yz) - (2 * wz);
			retVal.M33 = 1 - ((2 * xx) - (2 * yy));
			*/
            #endregion

            // Exit Function
            return retVal;
        }
        /// <remarks>
        /// This function assumes that the quaternion has a magnitude of one.  (it's optimized for that)
        /// 
        /// I still have no clue what should be done with this matrix, but here it is
        /// </remarks>
        public MyMatrix4 ToMatrix4FromUnitQuaternion()
        {
            throw new ApplicationException("Test this function first");

            MyMatrix4 retVal = new MyMatrix4();

            // All these cached results are used at least twice, so I'm not being wastefull
            double xx = this.X * this.X;
            double yy = this.Y * this.Y;
            double zz = this.Z * this.Z;

            double xy = this.X * this.Y;
            double xz = this.X * this.Z;

            double yz = this.Y * this.Z;

            double wy = this.W * this.Y;
            double wz = this.W * this.Z;

            // Row 1
            retVal.M11 = 1 - ((2 * yy) - (2 * zz));
            retVal.M12 = (2 * xy) - (2 * wz);
            retVal.M13 = (2 * xz) + (2 * wy);
            retVal.M14 = 0;

            // Row 2
            retVal.M21 = (2 * xy) + (2 * wz);
            retVal.M22 = 1 - ((2 * xx) - (2 * zz));
            retVal.M23 = (2 * yz) - (2 * wz);
            retVal.M24 = 0;

            // Row 3
            retVal.M31 = (2 * xz) - (2 * wy);
            retVal.M32 = (2 * yz) - (2 * wz);
            retVal.M33 = 1 - ((2 * xx) - (2 * yy));
            retVal.M34 = 0;

            // Row 4
            retVal.M41 = 0;
            retVal.M42 = 0;
            retVal.M43 = 0;
            retVal.M44 = 1;

            // Exit Function
            return retVal;
        }

        #endregion
    }
}
