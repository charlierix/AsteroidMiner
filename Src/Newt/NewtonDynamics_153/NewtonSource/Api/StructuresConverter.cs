using System.Windows.Media.Media3D;

namespace Game.Newt.NewtonDynamics_153.Api
{
	public class NewtonVector3
	{
		public float[] NWVector3 = new float[3];

		public NewtonVector3(Vector3D pVector)
		{
			NWVector3[ 0 ] = (float)pVector.X;
			NWVector3[ 1 ] = (float)pVector.Y;
			NWVector3[ 2 ] = (float)pVector.Z;
		}

		public NewtonVector3(float[] pVector)
		{
			NWVector3[ 0 ] = (float)pVector[ 0 ];
			NWVector3[ 1 ] = (float)pVector[ 1 ];
			NWVector3[ 2 ] = (float)pVector[ 2 ];
		}

		public Vector3D ToDirectX()
		{
			Vector3D aDXVecor = new Vector3D();

			aDXVecor.X = this.NWVector3[0];
			aDXVecor.Y = this.NWVector3[1];
			aDXVecor.Z = this.NWVector3[2];

			return aDXVecor;
		}
	}

	public class NewtonVector4
	{
		public float[] NWVector4 = new float[4];

		static NewtonVector4() { }

		public NewtonVector4(Point4D pVector)
		{
			NWVector4[ 0 ] = (float)pVector.X;
			NWVector4[ 1 ] = (float)pVector.Y;
			NWVector4[ 2 ] = (float)pVector.Z;
			NWVector4[ 3 ] = (float)pVector.W;
		}

		public NewtonVector4(float[] pVector)
		{
			NWVector4[ 0 ] = (float)pVector[ 0 ];
			NWVector4[ 1 ] = (float)pVector[ 1 ];
			NWVector4[ 2 ] = (float)pVector[ 2 ];
			NWVector4[ 3 ] = (float)pVector[ 3 ];
		}

		public Point4D ToDirectX()
		{
			Point4D aDXVecor = new Point4D();

			aDXVecor.X = NWVector4[0];
			aDXVecor.Y = NWVector4[1];
			aDXVecor.Z = NWVector4[2];
			aDXVecor.W = NWVector4[3];

			return aDXVecor;
		}

	}

	public class NewtonQuaternion
	{
		public float[] NWQuaternion = new float[4];
	
		static NewtonQuaternion() { }

		public NewtonQuaternion(Quaternion pQuaternion)
		{
			NWQuaternion[ 0 ] = (float)pQuaternion.X;
			NWQuaternion[ 1 ] = (float)pQuaternion.Y;
			NWQuaternion[ 2 ] = (float)pQuaternion.Z;
			NWQuaternion[ 3 ] = (float)pQuaternion.W;
		}

		public NewtonQuaternion(float[] pQuaternion)
		{
			NWQuaternion[ 0 ] = (float)pQuaternion[ 0 ];
			NWQuaternion[ 1 ] = (float)pQuaternion[ 1 ];
			NWQuaternion[ 2 ] = (float)pQuaternion[ 2 ];
			NWQuaternion[ 3 ] = (float)pQuaternion[ 3 ];
		}

		public Quaternion ToDirectX()
		{
			Quaternion aDXQuaternion = new Quaternion();

			aDXQuaternion.X = NWQuaternion[0];
			aDXQuaternion.Y = NWQuaternion[1];
			aDXQuaternion.Z = NWQuaternion[2];
			aDXQuaternion.W = NWQuaternion[3];

			return aDXQuaternion;
		}
	}


	public class NewtonMatrix
	{
		public float[] NWMatrix = new float[16];

		static NewtonMatrix() { }

		public NewtonMatrix(Matrix3D pMatrix)
		{
			NWMatrix[ 0 ] = (float)pMatrix.M11;
			NWMatrix[ 1 ] = (float)pMatrix.M12;
			NWMatrix[ 2 ] = (float)pMatrix.M13;
			NWMatrix[ 3 ] = (float)pMatrix.M14;

			NWMatrix[ 4 ] = (float)pMatrix.M21;
			NWMatrix[ 5 ] = (float)pMatrix.M22;
			NWMatrix[ 6 ] = (float)pMatrix.M23;
			NWMatrix[ 7 ] = (float)pMatrix.M24;

			NWMatrix[ 8 ] = (float)pMatrix.M31;
			NWMatrix[ 9 ] = (float)pMatrix.M32;
			NWMatrix[ 10 ] = (float)pMatrix.M33;
			NWMatrix[ 11 ] = (float)pMatrix.M34;

			NWMatrix[ 12 ] = (float)pMatrix.OffsetX;
			NWMatrix[ 13 ] = (float)pMatrix.OffsetY;
			NWMatrix[ 14 ] = (float)pMatrix.OffsetZ;
			NWMatrix[ 15 ] = (float)pMatrix.M44;
		}

		public NewtonMatrix(float[] pMatrix)
		{
			NWMatrix[0] = pMatrix[0];
			NWMatrix[1] = pMatrix[1];
			NWMatrix[2] = pMatrix[2];
			NWMatrix[3] = pMatrix[3];
								 
			NWMatrix[4] = pMatrix[4];
			NWMatrix[5] = pMatrix[5];
			NWMatrix[6] = pMatrix[6];
			NWMatrix[7] = pMatrix[7];
								 
			NWMatrix[8] = pMatrix[8];
			NWMatrix[9] = pMatrix[9];
			NWMatrix[10] = pMatrix[10];
			NWMatrix[11] = pMatrix[11];
								  
			NWMatrix[12] = pMatrix[12];
			NWMatrix[13] = pMatrix[13];
			NWMatrix[14] = pMatrix[14];
			NWMatrix[15] = pMatrix[15];
		}

		public Matrix3D ToDirectX()
		{
			Matrix3D aDXMatrix = Matrix3D.Identity;

			aDXMatrix.M11 = NWMatrix[0];
			aDXMatrix.M12 = NWMatrix[1];
			aDXMatrix.M13 = NWMatrix[2];
			aDXMatrix.M14 = NWMatrix[3];

			aDXMatrix.M21 = NWMatrix[4];
			aDXMatrix.M22 = NWMatrix[5];
			aDXMatrix.M23 = NWMatrix[6];
			aDXMatrix.M24 = NWMatrix[7];

			aDXMatrix.M31 = NWMatrix[8];
			aDXMatrix.M32 = NWMatrix[9];
			aDXMatrix.M33 = NWMatrix[10];
			aDXMatrix.M34 = NWMatrix[11];

			aDXMatrix.OffsetX = NWMatrix[12];
			aDXMatrix.OffsetY = NWMatrix[13];
			aDXMatrix.OffsetZ = NWMatrix[14];
			aDXMatrix.M44 = NWMatrix[15];

			return aDXMatrix;
		}

		public class NewtonJacobian
		{
			public float[] NWJacobian = new float[6];

			static NewtonJacobian()	{}

			public NewtonJacobian(float[] pJacobian)
			{
				for(int i = 0; i < 6; i++)
				{
					NWJacobian[i] = pJacobian[i];
				}
			}
		}
	}
}
