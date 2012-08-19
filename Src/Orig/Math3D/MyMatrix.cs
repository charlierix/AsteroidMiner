using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Orig.Math3D
{
	#region Class: Matrix3

    /// <summary>
    /// This is equivelent to System.Windows.Media.Matrix
    /// </summary>
	public class MyMatrix3
	{
		#region Declaration Section

		/// <summary>
		/// [1,1]
		/// </summary>
		public double M11;
		/// <summary>
		/// [1,2]
		/// </summary>
		public double M12;
		/// <summary>
		/// [1,3]
		/// </summary>
		public double M13;


		/// <summary>
		/// [2,1]
		/// </summary>
		public double M21;
		/// <summary>
		/// [2,2]
		/// </summary>
		public double M22;
		/// <summary>
		/// [2,3]
		/// </summary>
		public double M23;


		/// <summary>
		/// [3,1]
		/// </summary>
		public double M31;
		/// <summary>
		/// [3,2]
		/// </summary>
		public double M32;
		/// <summary>
		/// [3,3]
		/// </summary>
		public double M33;

		#endregion

		#region Public Properties

		public static MyMatrix3 IdentityMatrix
		{
			get
			{
				MyMatrix3 retVal = new MyMatrix3();

				retVal.M11 = 1;
				retVal.M21 = 0;
				retVal.M31 = 0;

				retVal.M12 = 0;
				retVal.M22 = 1;
				retVal.M32 = 0;

				retVal.M13 = 0;
				retVal.M23 = 0;
				retVal.M33 = 1;

				return retVal;
			}
		}

		#endregion

		#region Public Methods

		public MyMatrix3 Clone()
		{
			MyMatrix3 retVal = new MyMatrix3();

			retVal.M11 = this.M11;
			retVal.M12 = this.M12;
			retVal.M13 = this.M13;

			retVal.M21 = this.M21;
			retVal.M22 = this.M22;
			retVal.M23 = this.M23;

			retVal.M31 = this.M31;
			retVal.M32 = this.M32;
			retVal.M33 = this.M33;

			return retVal;
		}

		public void Transpose()
		{
			double temp;

			//	12, 21
			temp = this.M12;
			this.M12 = this.M21;
			this.M21 = temp;

			//	13, 31
			temp = this.M13;
			this.M13 = this.M31;
			this.M31 = temp;

			//	23, 32
			temp = this.M23;
			this.M23 = this.M32;
			this.M32 = temp;
		}
		public static MyMatrix3 Transpose(MyMatrix3 matrix)
		{
			MyMatrix3 retVal = matrix.Clone();
			retVal.Transpose();
			return retVal;
		}

		/// <summary>
		/// Transforms a vector by a matrix.
		/// </summary>
		/// <remarks>
		/// I've seen this in other code as matrix * vector
		/// </remarks>
		public static MyVector Transform(MyMatrix3 matrix, MyVector vector)
		{
			return new MyVector(
				(matrix.M11 * vector.X) + (matrix.M12 * vector.Y) + (matrix.M13 * vector.Z),
				(matrix.M21 * vector.X) + (matrix.M22 * vector.Y) + (matrix.M23 * vector.Z),
				(matrix.M31 * vector.X) + (matrix.M32 * vector.Y) + (matrix.M33 * vector.Z));
		}

		/// <summary>
		/// When you take m * my result, you get the identity matrix
		/// </summary>
		public static MyMatrix3 Inverse(MyMatrix3 m)
		{

			double determinant = Determinant(m);

			if (determinant > -0.0005d && determinant < 0.0005d)
			{
				return MyMatrix3.IdentityMatrix;
			}
			else
			{
				MyMatrix3 retVal = new MyMatrix3();

				//	The determinant is non-zero, so I can take the inverse (no divide by zero issues)
				retVal.M11 = (m.M22 * m.M33 - m.M32 * m.M23) / determinant;
				retVal.M21 = -(m.M21 * m.M33 - m.M23 * m.M31) / determinant;
				retVal.M31 = (m.M21 * m.M32 - m.M22 * m.M31) / determinant;

				retVal.M12 = -(m.M12 * m.M33 - m.M32 * m.M13) / determinant;
				retVal.M22 = (m.M11 * m.M33 - m.M13 * m.M31) / determinant;
				retVal.M32 = -(m.M11 * m.M32 - m.M12 * m.M31) / determinant;

				retVal.M13 = (m.M12 * m.M23 - m.M13 * m.M22) / determinant;
				retVal.M23 = -(m.M11 * m.M23 - m.M13 * m.M21) / determinant;
				retVal.M33 = (m.M11 * m.M22 - m.M21 * m.M12) / determinant;

				return retVal;
			}

		}

		public static double Determinant(MyMatrix3 m)
		{
			return m.M11 * (m.M22 * m.M33 - m.M23 * m.M32)
				- m.M21 * (m.M12 * m.M33 - m.M13 * m.M32)
				+ m.M31 * (m.M12 * m.M23 - m.M13 * m.M22);
		}

		public static MyMatrix3 Multiply(MyMatrix3 a, MyMatrix3 b)
		{
			MyMatrix3 retVal = new MyMatrix3();

			retVal.M11 = (a.M11 * b.M11) + (a.M12 * b.M21) + (a.M13 * b.M31);
			retVal.M12 = (a.M11 * b.M12) + (a.M12 * b.M22) + (a.M13 * b.M32);
			retVal.M13 = (a.M11 * b.M13) + (a.M12 * b.M23) + (a.M13 * b.M33);

			retVal.M21 = (a.M21 * b.M11) + (a.M22 * b.M21) + (a.M23 * b.M31);
			retVal.M22 = (a.M21 * b.M12) + (a.M22 * b.M22) + (a.M23 * b.M32);
			retVal.M23 = (a.M21 * b.M13) + (a.M22 * b.M23) + (a.M23 * b.M33);

			retVal.M31 = (a.M31 * b.M11) + (a.M32 * b.M21) + (a.M33 * b.M31);
			retVal.M32 = (a.M31 * b.M12) + (a.M32 * b.M22) + (a.M33 * b.M32);
			retVal.M33 = (a.M31 * b.M13) + (a.M32 * b.M23) + (a.M33 * b.M33);

			return retVal;
		}
		/// <summary>
		/// This multiplies the matrix by the vector, and returns the result as a vector (I think)
		/// </summary>
		/// <remarks>
		/// It's sort of a tossup where this function belongs.  It should probably go in Utility, but nobody will think to look there.
		/// And I don't want to dirty up the vector class, it's already pretty full.
		/// </remarks>
		public static MyVector Multiply(MyMatrix3 matrix, MyVector vector)
		{

			MyVector retVal = new MyVector();

			retVal.X = matrix.M11 * vector.X;
			retVal.X += matrix.M12 * vector.Y;
			retVal.X += matrix.M13 * vector.Z;

			retVal.Y = matrix.M21 * vector.X;
			retVal.Y += matrix.M22 * vector.Y;
			retVal.Y += matrix.M23 * vector.Z;

			retVal.Z = matrix.M31 * vector.X;
			retVal.Z += matrix.M32 * vector.Y;
			retVal.Z += matrix.M33 * vector.Z;

			return retVal;
		}

		public void Multiply(double scalar)
		{
			this.M11 *= scalar;
			this.M12 *= scalar;
			this.M13 *= scalar;

			this.M21 *= scalar;
			this.M22 *= scalar;
			this.M23 *= scalar;

			this.M31 *= scalar;
			this.M32 *= scalar;
			this.M33 *= scalar;
		}
		public static MyMatrix3 Multiply(MyMatrix3 matrix, double scalar)
		{
			MyMatrix3 retVal = matrix.Clone();
			retVal.Multiply(scalar);
			return retVal;
		}

		public void Add(MyMatrix3 matrix)
		{
			this.M11 += matrix.M11;
			this.M12 += matrix.M12;
			this.M13 += matrix.M13;

			this.M21 += matrix.M21;
			this.M22 += matrix.M22;
			this.M23 += matrix.M23;

			this.M31 += matrix.M31;
			this.M32 += matrix.M32;
			this.M33 += matrix.M33;
		}
		public static MyMatrix3 Add(MyMatrix3 a, MyMatrix3 b)
		{
			MyMatrix3 retVal = a.Clone();
			retVal.Add(b);
			return retVal;
		}

		#endregion
	}

	#endregion

	#region Class: Matrix4

	/// <summary>
	/// This is not complete, but I don't want to lose the definition
	/// </summary>
    /// <remarks>
    /// This is equivelent to System.Windows.Media.Media3D.Matrix3D
    /// </remarks>
	public class MyMatrix4
	{
		#region Declaration Section

		/// <summary>
		/// [1,1]
		/// </summary>
		public double M11;
		/// <summary>
		/// [1,2]
		/// </summary>
		public double M12;
		/// <summary>
		/// [1,3]
		/// </summary>
		public double M13;
		/// <summary>
		/// [1,4]
		/// </summary>
		public double M14;


		/// <summary>
		/// [2,1]
		/// </summary>
		public double M21;
		/// <summary>
		/// [2,2]
		/// </summary>
		public double M22;
		/// <summary>
		/// [2,3]
		/// </summary>
		public double M23;
		/// <summary>
		/// [2,4]
		/// </summary>
		public double M24;


		/// <summary>
		/// [3,1]
		/// </summary>
		public double M31;
		/// <summary>
		/// [3,2]
		/// </summary>
		public double M32;
		/// <summary>
		/// [3,3]
		/// </summary>
		public double M33;
		/// <summary>
		/// [3,4]
		/// </summary>
		public double M34;


		/// <summary>
		/// [4,1]
		/// </summary>
		public double M41;
		/// <summary>
		/// [4,2]
		/// </summary>
		public double M42;
		/// <summary>
		/// [4,3]
		/// </summary>
		public double M43;
		/// <summary>
		/// [4,4]
		/// </summary>
		public double M44;

		#endregion
	}

	#endregion
}
