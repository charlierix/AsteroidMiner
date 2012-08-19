using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Media3D;

namespace Game.Newt.HelperClasses
{
	/// <summary>
	/// These are extension methods of various types
	/// </summary>
	public static class Extenders
	{
		#region Vector3D

		public static bool IsZero(this Vector3D vector)
		{
			return vector.X == 0d && vector.Y == 0d && vector.Z == 0d;
		}
		public static bool IsNearZero(this Vector3D vector)
		{
			return Math3D.IsNearZero(vector);
		}

		public static Point3D ToPoint(this Vector3D vector)
		{
			return new Point3D(vector.X, vector.Y, vector.Z);
		}

		public static string ToString(this Vector3D vector, bool extensionsVersion)
		{
			return vector.X.ToString() + ", " + vector.Y.ToString() + ", " + vector.Z.ToString();
		}
		public static string ToString(this Vector3D vector, int significantDigits)
		{
			return vector.X.ToString("N" + significantDigits.ToString()) + ", " + vector.Y.ToString("N" + significantDigits.ToString()) + ", " + vector.Z.ToString("N" + significantDigits.ToString());
		}

		/// <summary>
		/// Rotates the vector around the angle in degrees
		/// </summary>
		public static Vector3D GetRotatedVector(this Vector3D vector, Vector3D axis, double angle)
		{
			Matrix3D matrix = new Matrix3D();
			matrix.Rotate(new Quaternion(axis, angle));

			MatrixTransform3D transform = new MatrixTransform3D(matrix);

			return transform.Transform(vector);
		}

		/// <summary>
		/// Returns the portion of this vector that lies along the other vector
		/// NOTE: The return will be the same direction as alongVector, but the length from zero to this vector's full length
		/// </summary>
		/// <remarks>
		/// Lookup "vector projection" to see the difference between this and dot product
		/// http://en.wikipedia.org/wiki/Vector_projection
		/// </remarks>
		public static Vector3D GetProjectedVector(this Vector3D vector, Vector3D alongVector)
		{
			// c = (a dot unit(b)) * unit(b)

			Vector3D alongVectorUnit = alongVector;
			alongVectorUnit.Normalize();

			double length = Vector3D.DotProduct(vector, alongVectorUnit);

			return alongVectorUnit * length;
		}
		public static Vector3D GetProjectedVector(this Vector3D vector, ITriangle alongPlane)
		{
			//	Get a line that is parallel to the plane, but along the direction of the vector
			Vector3D alongLine = Vector3D.CrossProduct(alongPlane.Normal, Vector3D.CrossProduct(vector, alongPlane.Normal));

			//	Use the other overload to get the portion of the vector along this line
			return vector.GetProjectedVector(alongLine);
		}

		/// <summary>
		/// I was getting tired of needing two statements to get a unit vector
		/// </summary>
		public static Vector3D ToUnit(this Vector3D vector)
		{
			Vector3D retVal = vector;
			retVal.Normalize();
			return retVal;
		}

		#endregion

		#region Point3D

		public static Vector3D ToVector(this Point3D point)
		{
			return new Vector3D(point.X, point.Y, point.Z);
		}

		public static string ToString(this Point3D point, bool extensionsVersion)
		{
			return point.X.ToString() + ", " + point.Y.ToString() + ", " + point.Z.ToString();
		}
		public static string ToString(this Point3D point, int significantDigits)
		{
			return point.X.ToString("N" + significantDigits.ToString()) + ", " + point.Y.ToString("N" + significantDigits.ToString()) + ", " + point.Z.ToString("N" + significantDigits.ToString());
		}

		#endregion

		#region Quaternion

		//	I copy the code in each of these overloads, rather than make a private method to increase speed
		public static Vector3D GetRotatedVector(this Quaternion quaternion, Vector3D vector)
		{
			Matrix3D matrix = new Matrix3D();
			matrix.Rotate(quaternion);

			MatrixTransform3D transform = new MatrixTransform3D(matrix);

			return transform.Transform(vector);
		}
		public static Point3D GetRotatedVector(this Quaternion quaternion, Point3D point)
		{
			Matrix3D matrix = new Matrix3D();
			matrix.Rotate(quaternion);

			MatrixTransform3D transform = new MatrixTransform3D(matrix);

			return transform.Transform(point);
		}
		public static DoubleVector GetRotatedVector(this Quaternion quaternion, DoubleVector doubleVector)
		{
			Matrix3D matrix = new Matrix3D();
			matrix.Rotate(quaternion);

			MatrixTransform3D transform = new MatrixTransform3D(matrix);

			return new DoubleVector(transform.Transform(doubleVector.Standard), transform.Transform(doubleVector.Orth));
		}

		public static void GetRotatedVector(this Quaternion quaternion, Vector3D[] vectors)
		{
			Matrix3D matrix = new Matrix3D();
			matrix.Rotate(quaternion);

			MatrixTransform3D transform = new MatrixTransform3D(matrix);

			transform.Transform(vectors);
		}
		public static void GetRotatedVector(this Quaternion quaternion, Point3D[] points)
		{
			Matrix3D matrix = new Matrix3D();
			matrix.Rotate(quaternion);

			MatrixTransform3D transform = new MatrixTransform3D(matrix);

			transform.Transform(points);
		}
		public static void GetRotatedVector(this Quaternion quaternion, DoubleVector[] doubleVectors)
		{
			Matrix3D matrix = new Matrix3D();
			matrix.Rotate(quaternion);

			MatrixTransform3D transform = new MatrixTransform3D(matrix);

			for (int cntr = 0; cntr < doubleVectors.Length; cntr++)
			{
				doubleVectors[cntr] = new DoubleVector(transform.Transform(doubleVectors[cntr].Standard), transform.Transform(doubleVectors[cntr].Orth));
			}
		}
		/// <summary>
		/// This overload will rotate arrays of different types.  Just pass null if you don't have any of that type
		/// </summary>
		public static void GetRotatedVector(this Quaternion quaternion, Vector3D[] vectors, Point3D[] points, DoubleVector[] doubleVectors)
		{
			Matrix3D matrix = new Matrix3D();
			matrix.Rotate(quaternion);

			MatrixTransform3D transform = new MatrixTransform3D(matrix);

			if (vectors != null)
			{
				transform.Transform(vectors);
			}

			if (points != null)
			{
				transform.Transform(points);
			}

			if (doubleVectors != null)
			{
				for (int cntr = 0; cntr < doubleVectors.Length; cntr++)
				{
					doubleVectors[cntr] = new DoubleVector(transform.Transform(doubleVectors[cntr].Standard), transform.Transform(doubleVectors[cntr].Orth));
				}
			}
		}

		public static Quaternion ToUnit(this Quaternion quaternion)
		{
			Quaternion retVal = quaternion;
			retVal.Normalize();
			return retVal;
		}

		#endregion

		#region ModelVisual3D

		public static string ReportGeometry(this ModelVisual3D model)
		{
			StringBuilder retVal = new StringBuilder();

			if (model.Content == null)
			{
				retVal.Append("Content is null");
			}
			else if (model.Content is GeometryModel3D)
			{
				retVal.Append("Content is GeometryModel3D:\r\n");
				retVal.Append(((GeometryModel3D)model.Content).ReportGeometry());
			}
			else
			{
				retVal.Append("Content is " + model.Content.GetType().ToString());
			}

			//	Exit Function
			return retVal.ToString();
		}

		#endregion

		#region GeometryModel3D

		public static string ReportGeometry(this GeometryModel3D geometry)
		{
			StringBuilder retVal = new StringBuilder();

			if (geometry.Geometry == null)
			{
				retVal.Append("Geometry is null");
			}
			else if (geometry.Geometry is MeshGeometry3D)
			{
				retVal.Append("Content is MeshGeometry3D:\r\n");
				retVal.Append(((MeshGeometry3D)geometry.Geometry).ReportGeometry());
			}
			else
			{
				retVal.Append("Geometry is " + geometry.Geometry.GetType().ToString());
			}

			//	Exit Function
			return retVal.ToString();
		}

		#endregion

		#region MeshGeometry3D

		public static string ReportGeometry(this MeshGeometry3D mesh)
		{
			StringBuilder retVal = new StringBuilder();

			if (mesh.Positions == null)
			{
				retVal.Append("Positions is null\r\n");
			}
			else
			{
				retVal.Append("Positions (");
				retVal.Append(mesh.Positions.Count.ToString());
				retVal.Append(")\r\n");

				foreach (Point3D pos in mesh.Positions)
				{
					retVal.Append(pos.ToString(true));
					retVal.Append("\r\n");
				}
			}

			if (mesh.TriangleIndices == null)
			{
				retVal.Append("TriangleIndices is null\r\n");
			}
			else
			{
				retVal.Append("TriangleIndices (");
				retVal.Append(mesh.TriangleIndices.Count.ToString());
				retVal.Append(")\r\n");

				for (int cntr = 0; cntr < mesh.TriangleIndices.Count / 3; cntr++)
				{
					retVal.Append(mesh.TriangleIndices[cntr].ToString());
					retVal.Append(" ");
					retVal.Append(mesh.TriangleIndices[cntr + 1].ToString());
					retVal.Append(" ");
					retVal.Append(mesh.TriangleIndices[cntr + 2].ToString());
					retVal.Append("\r\n");
				}

				//foreach (int index in mesh.TriangleIndices)
				//{
				//    retVal.Append(index.ToString());
				//    retVal.Append("\r\n");
				//}
			}

			if (mesh.Positions != null && mesh.TriangleIndices != null)
			{
				retVal.Append("Triangles (");
				retVal.Append((mesh.TriangleIndices.Count / 3).ToString());
				retVal.Append(")\r\n");

				for (int cntr = 0; cntr < mesh.TriangleIndices.Count / 3; cntr++)
				{
					retVal.Append("(");
					retVal.Append(mesh.Positions[mesh.TriangleIndices[cntr]].ToString(2));
					retVal.Append(") (");
					retVal.Append(mesh.Positions[mesh.TriangleIndices[cntr + 1]].ToString(2));
					retVal.Append(") (");
					retVal.Append(mesh.Positions[mesh.TriangleIndices[cntr + 2]].ToString(2));
					retVal.Append(")\r\n");
				}
			}

			//	Exit Function
			return retVal.ToString();
		}

		#endregion

		#region RotateTransform3D

		public static Quaternion ToQuaternion(this RotateTransform3D transform)
		{
			if (transform.Rotation == null)
			{
				throw new ArgumentException("transform.Rotation shouldn't be null");
			}
			else if (transform.Rotation is AxisAngleRotation3D)
			{
				AxisAngleRotation3D aaRot = (AxisAngleRotation3D)transform.Rotation;
				return new Quaternion(aaRot.Axis, aaRot.Angle);
			}
			else if (transform.Rotation is QuaternionRotation3D)
			{
				return ((QuaternionRotation3D)transform.Rotation).Quaternion;
			}
			else
			{
				throw new ArgumentException("Unexpected type for transform.Rotation: " + transform.Rotation.GetType().ToString());
			}
		}

		#endregion
	}
}
