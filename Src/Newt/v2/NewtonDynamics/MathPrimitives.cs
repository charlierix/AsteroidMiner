using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Windows.Media.Media3D;

namespace Game.Newt.v2.NewtonDynamics
{
    #region Class: NewtonVector3

    public class NewtonVector3
    {
        public float[] Vector = new float[3];

        public NewtonVector3()
            : this(new Vector3D()) { }
        public NewtonVector3(Vector3D vector)
        {
            this.Vector[0] = (float)vector.X;
            this.Vector[1] = (float)vector.Y;
            this.Vector[2] = (float)vector.Z;
        }
        public NewtonVector3(Point3D point)
        {
            this.Vector[0] = (float)point.X;
            this.Vector[1] = (float)point.Y;
            this.Vector[2] = (float)point.Z;
        }
        public NewtonVector3(float[] vector)
        {
            this.Vector[0] = vector[0];
            this.Vector[1] = vector[1];
            this.Vector[2] = vector[2];
        }
        public NewtonVector3(double x, double y, double z)
        {
            this.Vector[0] = (float)x;
            this.Vector[1] = (float)y;
            this.Vector[2] = (float)z;
        }

        public Vector3D ToVectorWPF()
        {
            Vector3D retVal = new Vector3D();

            retVal.X = this.Vector[0];
            retVal.Y = this.Vector[1];
            retVal.Z = this.Vector[2];

            return retVal;
        }
        public Point3D ToPointWPF()
        {
            Point3D retVal = new Point3D();

            retVal.X = this.Vector[0];
            retVal.Y = this.Vector[1];
            retVal.Z = this.Vector[2];

            return retVal;
        }
    }

    #endregion
    #region Class: NewtonVector4

    public class NewtonVector4
    {
        public float[] Vector = new float[4];

        public NewtonVector4(Point4D vector)
        {
            this.Vector[0] = (float)vector.X;
            this.Vector[1] = (float)vector.Y;
            this.Vector[2] = (float)vector.Z;
            this.Vector[3] = (float)vector.W;
        }
        public NewtonVector4(float[] vector)
        {
            this.Vector[0] = vector[0];
            this.Vector[1] = vector[1];
            this.Vector[2] = vector[2];
            this.Vector[3] = vector[3];
        }

        public Point4D ToWPF()
        {
            Point4D retVal = new Point4D();

            retVal.X = this.Vector[0];
            retVal.Y = this.Vector[1];
            retVal.Z = this.Vector[2];
            retVal.W = this.Vector[3];

            return retVal;
        }
    }

    #endregion
    #region Class: NewtonQuaternion

    public class NewtonQuaternion
    {
        public float[] Quaternion = new float[4];

        public NewtonQuaternion()
            : this(new Quaternion()) { }
        public NewtonQuaternion(Quaternion quaternion)
        {
            //this.Quaternion[0] = (float)quaternion.X;
            //this.Quaternion[1] = (float)quaternion.Y;
            //this.Quaternion[2] = (float)quaternion.Z;
            //this.Quaternion[3] = (float)quaternion.W;

            this.Quaternion[0] = (float)quaternion.W;
            this.Quaternion[1] = (float)quaternion.X;
            this.Quaternion[2] = (float)quaternion.Y;
            this.Quaternion[3] = (float)quaternion.Z;
        }
        public NewtonQuaternion(float[] quaternion)
        {
            this.Quaternion[0] = quaternion[0];
            this.Quaternion[1] = quaternion[1];
            this.Quaternion[2] = quaternion[2];
            this.Quaternion[3] = quaternion[3];
        }

        public Quaternion ToWPF()
        {
            Quaternion retVal = new Quaternion();

            // Everything I've read says he uses 0 for W, then X,Y,Z

            //retVal.X = this.Quaternion[0];		// this is from the 1.53 wrapper I downloaded, but nothing ever used that, so not sure if it was ever tested
            //retVal.Y = this.Quaternion[1];
            //retVal.Z = this.Quaternion[2];
            //retVal.W = this.Quaternion[3];

            retVal.W = this.Quaternion[0];
            retVal.X = this.Quaternion[1];
            retVal.Y = this.Quaternion[2];
            retVal.Z = this.Quaternion[3];

            return retVal;
        }
    }

    #endregion
    #region Class: NewtonMatrix

    public class NewtonMatrix
    {
        #region Class: NewtonJacobian

        public class NewtonJacobian
        {
            public float[] Jacobian = new float[6];

            public NewtonJacobian(float[] jacobian)
            {
                for (int i = 0; i < 6; i++)
                {
                    this.Jacobian[i] = jacobian[i];
                }
            }
        }

        #endregion

        public float[] Matrix = new float[16];

        public NewtonMatrix()
            : this(new Matrix3D()) { }
        public NewtonMatrix(Matrix3D matrix)
        {
            this.Matrix[0] = (float)matrix.M11;
            this.Matrix[1] = (float)matrix.M12;
            this.Matrix[2] = (float)matrix.M13;
            this.Matrix[3] = (float)matrix.M14;

            this.Matrix[4] = (float)matrix.M21;
            this.Matrix[5] = (float)matrix.M22;
            this.Matrix[6] = (float)matrix.M23;
            this.Matrix[7] = (float)matrix.M24;

            this.Matrix[8] = (float)matrix.M31;
            this.Matrix[9] = (float)matrix.M32;
            this.Matrix[10] = (float)matrix.M33;
            this.Matrix[11] = (float)matrix.M34;

            this.Matrix[12] = (float)matrix.OffsetX;
            this.Matrix[13] = (float)matrix.OffsetY;
            this.Matrix[14] = (float)matrix.OffsetZ;
            this.Matrix[15] = (float)matrix.M44;
        }
        public NewtonMatrix(float[] matrix)
        {
            this.Matrix[0] = matrix[0];
            this.Matrix[1] = matrix[1];
            this.Matrix[2] = matrix[2];
            this.Matrix[3] = matrix[3];

            this.Matrix[4] = matrix[4];
            this.Matrix[5] = matrix[5];
            this.Matrix[6] = matrix[6];
            this.Matrix[7] = matrix[7];

            this.Matrix[8] = matrix[8];
            this.Matrix[9] = matrix[9];
            this.Matrix[10] = matrix[10];
            this.Matrix[11] = matrix[11];

            this.Matrix[12] = matrix[12];
            this.Matrix[13] = matrix[13];
            this.Matrix[14] = matrix[14];
            this.Matrix[15] = matrix[15];
        }

        public Matrix3D ToWPF()
        {
            Matrix3D retVal = Matrix3D.Identity;

            retVal.M11 = this.Matrix[0];
            retVal.M12 = this.Matrix[1];
            retVal.M13 = this.Matrix[2];
            retVal.M14 = this.Matrix[3];

            retVal.M21 = this.Matrix[4];
            retVal.M22 = this.Matrix[5];
            retVal.M23 = this.Matrix[6];
            retVal.M24 = this.Matrix[7];

            retVal.M31 = this.Matrix[8];
            retVal.M32 = this.Matrix[9];
            retVal.M33 = this.Matrix[10];
            retVal.M34 = this.Matrix[11];

            retVal.OffsetX = this.Matrix[12];
            retVal.OffsetY = this.Matrix[13];
            retVal.OffsetZ = this.Matrix[14];
            retVal.M44 = this.Matrix[15];

            return retVal;
        }
    }

    #endregion

    #region Struct: InertialMatrix

    public struct InertialMatrix
    {
        public Vector3D Inertia;
        public Vector3D Origin;

        public InertialMatrix(Vector3D inertia, Vector3D origin)
        {
            this.Inertia = inertia;
            this.Origin = origin;
        }
    }

    #endregion
}
