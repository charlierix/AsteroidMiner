using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;

namespace Game.HelperClassesWPF
{
    /// <summary>
    /// These are extension methods of various wpf types
    /// </summary>
    public static class Extenders
    {
        #region Color

        public static ColorHSV ToHSV(this Color color)
        {
            return UtilityWPF.RGBtoHSV(color);
        }

        public static string ToHex(this Color color, bool includeAlpha = true, bool includePound = true)
        {
            return UtilityWPF.ColorToHex(color, includeAlpha, includePound);
        }

        public static Color ToGray(this Color color)
        {
            return UtilityWPF.ConvertToGray(color);
        }

        #endregion

        #region Vector

        public static bool IsNearZero(this Vector vector)
        {
            return vector.X.IsNearZero() &&
                        vector.Y.IsNearZero();
        }
        public static bool IsNearValue(this Vector vector, Vector compare)
        {
            return vector.X.IsNearValue(compare.X) &&
                        vector.Y.IsNearValue(compare.Y);
        }

        public static bool IsInvalid(this Vector vector)
        {
            return Math2D.IsInvalid(vector);
        }

        public static Point ToPoint(this Vector vector)
        {
            return new Point(vector.X, vector.Y);
        }

        public static Point3D ToPoint3D(this Vector vector)
        {
            return new Point3D(vector.X, vector.Y, 0d);
        }
        public static Point3D ToPoint3D(this Vector vector, double z)
        {
            return new Point3D(vector.X, vector.Y, z);
        }

        public static Vector3D ToVector3D(this Vector vector)
        {
            return new Vector3D(vector.X, vector.Y, 0d);
        }
        public static Vector3D ToVector3D(this Vector vector, double z)
        {
            return new Vector3D(vector.X, vector.Y, z);
        }

        public static VectorND ToVectorND(this Vector vector, int? dimensions = null)
        {
            if (dimensions == null)
            {
                return new VectorND(vector.X, vector.Y);
            }
            else
            {
                double[] arr = new double[dimensions.Value];

                for (int cntr = 0; cntr < Math.Min(2, dimensions.Value); cntr++)
                {
                    switch (cntr)
                    {
                        case 0: arr[cntr] = vector.X; break;
                        case 1: arr[cntr] = vector.Y; break;
                    }
                }

                return new VectorND(arr);
            }
        }

        public static string ToString(this Vector vector, bool extensionsVersion)
        {
            return vector.X.ToString() + ", " + vector.Y.ToString();
        }
        public static string ToString(this Vector vector, int significantDigits)
        {
            return vector.X.ToString("N" + significantDigits.ToString()) + ", " + vector.Y.ToString("N" + significantDigits.ToString());
        }

        public static string ToStringSignificantDigits(this Vector vector, int significantDigits)
        {
            return string.Format("{0}, {1}", vector.X.ToStringSignificantDigits(significantDigits), vector.Y.ToStringSignificantDigits(significantDigits));
        }

        /// <summary>
        /// I was getting tired of needing two statements to get a unit vector
        /// </summary>
        public static Vector ToUnit(this Vector vector, bool useNaNIfInvalid = false)
        {
            Vector retVal = vector;
            retVal.Normalize();

            if (!useNaNIfInvalid && Math2D.IsInvalid(retVal))
            {
                retVal = new Vector(0, 0);
            }

            return retVal;
        }

        #endregion

        #region Point

        public static bool IsNearZero(this Point point)
        {
            return point.X.IsNearZero() &&
                        point.Y.IsNearZero();
        }
        public static bool IsNearValue(this Point point, Point compare)
        {
            return point.X.IsNearValue(compare.X) &&
                        point.Y.IsNearValue(compare.Y);
        }

        public static bool IsInvalid(this Point point)
        {
            return Math2D.IsInvalid(point);
        }

        public static Vector ToVector(this Point point)
        {
            return new Vector(point.X, point.Y);
        }

        public static Vector3D ToVector3D(this Point point)
        {
            return new Vector3D(point.X, point.Y, 0d);
        }
        public static Vector3D ToVector3D(this Point point, double z)
        {
            return new Vector3D(point.X, point.Y, z);
        }

        public static Point3D ToPoint3D(this Point point)
        {
            return new Point3D(point.X, point.Y, 0d);
        }
        public static Point3D ToPoint3D(this Point point, double z)
        {
            return new Point3D(point.X, point.Y, z);
        }

        public static VectorND ToVectorND(this Point point, int? dimensions = null)
        {
            if (dimensions == null)
            {
                return new VectorND(point.X, point.Y);
            }
            else
            {
                double[] arr = new double[dimensions.Value];

                for (int cntr = 0; cntr < Math.Min(2, dimensions.Value); cntr++)
                {
                    switch (cntr)
                    {
                        case 0: arr[cntr] = point.X; break;
                        case 1: arr[cntr] = point.Y; break;
                    }
                }

                return new VectorND(arr);
            }
        }

        public static string ToString(this Point point, bool extensionsVersion)
        {
            return point.X.ToString() + ", " + point.Y.ToString();
        }
        public static string ToString(this Point point, int significantDigits)
        {
            return point.X.ToString("N" + significantDigits.ToString()) + ", " + point.Y.ToString("N" + significantDigits.ToString());
        }

        public static string ToStringSignificantDigits(this Point point, int significantDigits)
        {
            return string.Format("{0}, {1}", point.X.ToStringSignificantDigits(significantDigits), point.Y.ToStringSignificantDigits(significantDigits));
        }

        #endregion

        #region Size

        public static Size3D ToSize3D(this Size size, double z = 0)
        {
            return new Size3D(size.Width, size.Height, z);
        }

        #endregion

        #region Vector3D

        public static bool IsZero(this Vector3D vector)
        {
            return vector.X == 0d && vector.Y == 0d && vector.Z == 0d;
        }

        public static bool IsNearZero(this Vector3D vector)
        {
            return vector.X.IsNearZero() &&
                        vector.Y.IsNearZero() &&
                        vector.Z.IsNearZero();
        }
        public static bool IsNearValue(this Vector3D vector, Vector3D compare)
        {
            return vector.X.IsNearValue(compare.X) &&
                        vector.Y.IsNearValue(compare.Y) &&
                        vector.Z.IsNearValue(compare.Z);
        }

        public static bool IsInvalid(this Vector3D vector)
        {
            return Math3D.IsInvalid(vector);
        }

        public static Point3D ToPoint(this Vector3D vector)
        {
            return new Point3D(vector.X, vector.Y, vector.Z);
        }

        public static Vector ToVector2D(this Vector3D vector)
        {
            return new Vector(vector.X, vector.Y);
        }
        public static Point ToPoint2D(this Vector3D vector)
        {
            return new Point(vector.X, vector.Y);
        }

        public static Size3D ToSize(this Vector3D vector)
        {
            return new Size3D(Math.Abs(vector.X), Math.Abs(vector.Y), Math.Abs(vector.Z));
        }

        public static VectorND ToVectorND(this Vector3D vector, int? dimensions = null)
        {
            if (dimensions == null)
            {
                return new VectorND(vector.X, vector.Y, vector.Z);
            }
            else
            {
                double[] arr = new double[dimensions.Value];

                for (int cntr = 0; cntr < Math.Min(3, dimensions.Value); cntr++)
                {
                    switch (cntr)
                    {
                        case 0: arr[cntr] = vector.X; break;
                        case 1: arr[cntr] = vector.Y; break;
                        case 2: arr[cntr] = vector.Z; break;
                    }
                }

                return new VectorND(arr);
            }
        }

        public static double[] ToArray(this Vector3D vector)
        {
            return new[] { vector.X, vector.Y, vector.Z };
        }

        public static string ToString(this Vector3D vector, bool extensionsVersion)
        {
            return vector.X.ToString() + ", " + vector.Y.ToString() + ", " + vector.Z.ToString();
        }
        public static string ToString(this Vector3D vector, int significantDigits)
        {
            return vector.X.ToString("N" + significantDigits.ToString()) + ", " + vector.Y.ToString("N" + significantDigits.ToString()) + ", " + vector.Z.ToString("N" + significantDigits.ToString());
        }

        public static string ToStringSignificantDigits(this Vector3D vector, int significantDigits)
        {
            return string.Format("{0}, {1}, {2}", vector.X.ToStringSignificantDigits(significantDigits), vector.Y.ToStringSignificantDigits(significantDigits), vector.Z.ToStringSignificantDigits(significantDigits));
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
        public static Vector3D GetProjectedVector(this Vector3D vector, Vector3D alongVector, bool eitherDirection = true)
        {
            // c = (a dot unit(b)) * unit(b)

            if (Math3D.IsNearZero(vector) || Math3D.IsNearZero(alongVector))
            {
                return new Vector3D(0, 0, 0);
            }

            Vector3D alongVectorUnit = alongVector;
            alongVectorUnit.Normalize();

            double length = Vector3D.DotProduct(vector, alongVectorUnit);

            if (!eitherDirection && length < 0)
            {
                // It's in the oppositie direction, and that isn't allowed
                return new Vector3D(0, 0, 0);
            }

            return alongVectorUnit * length;
        }
        public static Vector3D GetProjectedVector(this Vector3D vector, ITriangle alongPlane)
        {
            // Get a line that is parallel to the plane, but along the direction of the vector
            Vector3D alongLine = Vector3D.CrossProduct(alongPlane.Normal, Vector3D.CrossProduct(vector, alongPlane.Normal));

            // Use the other overload to get the portion of the vector along this line
            return vector.GetProjectedVector(alongLine);
        }

        /// <summary>
        /// I was getting tired of needing two statements to get a unit vector
        /// </summary>
        /// <param name="useNaNIfInvalid">
        /// True=Standard behavior.  By definition a unit vector always has length of one, so if the initial length is zero, then the length becomes NaN
        /// False=Vector just goes to zero
        /// </param>
        public static Vector3D ToUnit(this Vector3D vector, bool useNaNIfInvalid = false)
        {
            Vector3D retVal = vector;
            retVal.Normalize();

            if (!useNaNIfInvalid && Math3D.IsInvalid(retVal))
            {
                retVal = new Vector3D(0, 0, 0);
            }

            return retVal;
        }

        public static double Coord(this Vector3D vector, Axis axis)
        {
            switch (axis)
            {
                case Axis.X:
                    return vector.X;

                case Axis.Y:
                    return vector.Y;

                case Axis.Z:
                    return vector.Z;

                default:
                    throw new ApplicationException("Unknown Axis: " + axis.ToString());
            }
        }

        #endregion

        #region Point3D

        public static Point3D MultiplyBy(this Point3D point, double scalar)
        {
            return new Point3D(point.X * scalar, point.Y * scalar, point.Z * scalar);
        }
        public static Point3D DivideBy(this Point3D point, double scalar)
        {
            return new Point3D(point.X / scalar, point.Y / scalar, point.Z / scalar);
        }

        public static bool IsInvalid(this Point3D point)
        {
            return Math3D.IsInvalid(point);
        }

        public static bool IsNearZero(this Point3D point)
        {
            return point.X.IsNearZero() &&
                        point.Y.IsNearZero() &&
                        point.Z.IsNearZero();
        }
        public static bool IsNearValue(this Point3D point, Point3D compare)
        {
            return point.X.IsNearValue(compare.X) &&
                        point.Y.IsNearValue(compare.Y) &&
                        point.Z.IsNearValue(compare.Z);
        }

        public static Vector3D ToVector(this Point3D point)
        {
            return new Vector3D(point.X, point.Y, point.Z);
        }

        public static Point ToPoint2D(this Point3D point)
        {
            return new Point(point.X, point.Y);
        }
        public static Vector ToVector2D(this Point3D point)
        {
            return new Vector(point.X, point.Y);
        }

        public static VectorND ToVectorND(this Point3D point, int? dimensions = null)
        {
            if (dimensions == null)
            {
                return new VectorND(point.X, point.Y, point.Z);
            }
            else
            {
                double[] arr = new double[dimensions.Value];

                for (int cntr = 0; cntr < Math.Min(3, dimensions.Value); cntr++)
                {
                    switch (cntr)
                    {
                        case 0: arr[cntr] = point.X; break;
                        case 1: arr[cntr] = point.Y; break;
                        case 2: arr[cntr] = point.Z; break;
                    }
                }

                return new VectorND(arr);
            }
        }

        public static double[] ToArray(this Point3D point)
        {
            return new[] { point.X, point.Y, point.Z };
        }

        public static string ToString(this Point3D point, bool extensionsVersion)
        {
            return point.X.ToString() + ", " + point.Y.ToString() + ", " + point.Z.ToString();
        }
        public static string ToString(this Point3D point, int significantDigits)
        {
            return point.X.ToString("N" + significantDigits.ToString()) + ", " + point.Y.ToString("N" + significantDigits.ToString()) + ", " + point.Z.ToString("N" + significantDigits.ToString());
        }

        public static string ToStringSignificantDigits(this Point3D point, int significantDigits)
        {
            return string.Format("{0}, {1}, {2}", point.X.ToStringSignificantDigits(significantDigits), point.Y.ToStringSignificantDigits(significantDigits), point.Z.ToStringSignificantDigits(significantDigits));
        }

        /// <summary>
        /// I was getting tired of needing two statements to get a unit vector
        /// </summary>
        /// <param name="useNaNIfInvalid">
        /// True=Standard behavior.  By definition a unit vector always has length of one, so if the initial length is zero, then the length becomes NaN
        /// False=Vector just goes to zero
        /// </param>
        public static Point3D ToUnit(this Point3D point, bool useNaNIfInvalid = false)
        {
            return point.ToVector().ToUnit(useNaNIfInvalid).ToPoint();
        }

        public static double Coord(this Point3D point, Axis axis)
        {
            switch (axis)
            {
                case Axis.X:
                    return point.X;

                case Axis.Y:
                    return point.Y;

                case Axis.Z:
                    return point.Z;

                default:
                    throw new ApplicationException("Unknown Axis: " + axis.ToString());
            }
        }

        #endregion

        #region VectorND

        public static bool IsNearZero(this VectorND vector)
        {
            if (vector.VectorArray == null)
            {
                return true;
            }

            return vector.VectorArray.All(o => o.IsNearZero());
        }
        public static bool IsNearValue(this VectorND vector, VectorND other)
        {
            return MathND.IsNearValue(vector.VectorArray, other.VectorArray);
        }

        public static bool IsInvalid(this VectorND vector)
        {
            double[] arr = vector.VectorArray;
            if (arr == null)
            {
                return false;       // it could be argued that this is invalid, but all the other types that have IsInvalid only consider the values that Math1D.IsInvalid looks at
            }

            foreach (double value in arr)
            {
                if (Math1D.IsInvalid(value))
                {
                    return true;
                }
            }

            return false;
        }

        public static string ToStringSignificantDigits(this VectorND vector, int significantDigits)
        {
            double[] arr = vector.VectorArray;
            if (arr == null)
            {
                return "<null>";
            }

            return arr.
                Select(o => o.ToStringSignificantDigits(significantDigits)).
                ToJoin(", ");
        }

        /// <summary>
        /// Returns the portion of this vector that lies along the other vector
        /// NOTE: The return will be the same direction as alongVector, but the length from zero to this vector's full length
        /// </summary>
        /// <remarks>
        /// This is copied from the Vector3D version
        /// </remarks>
        public static VectorND GetProjectedVector(this VectorND vector, VectorND alongVector, bool eitherDirection = true)
        {
            // c = (a dot unit(b)) * unit(b)

            if (vector.IsNearZero() || alongVector.IsNearZero())
            {
                return MathND.GetZeroVector(vector, alongVector);
            }

            VectorND alongVectorUnit = alongVector.ToUnit();

            double length = VectorND.DotProduct(vector, alongVectorUnit);

            if (!eitherDirection && length < 0)
            {
                // It's in the oppositie direction, and that isn't allowed
                return MathND.GetZeroVector(vector, alongVector);
            }

            return alongVectorUnit * length;
        }

        #endregion

        #region Size3D

        public static Size ToSize2D(this Size3D size)
        {
            return new Size(size.X, size.Y);
        }

        public static Vector3D ToVector(this Size3D size)
        {
            return new Vector3D(size.X, size.Y, size.Z);
        }

        #endregion

        #region Quaternion

        public static bool IsNearValue(this Quaternion quaternion, Quaternion compare)
        {
            return quaternion.X.IsNearValue(compare.X) &&
                        quaternion.Y.IsNearValue(compare.Y) &&
                        quaternion.Z.IsNearValue(compare.Z) &&
                        quaternion.W.IsNearValue(compare.W);
        }

        // I copy the code in each of these overloads, rather than make a private method to increase speed
        //TODO: I don't use these rotate extension methods anymore, but it would be worth testing whether to use this matrix transform, or use: new RotateTransform3D(new QuaternionRotation3D(quaternion))
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

        /// <summary>
        /// This returns the current quaternion rotated by the delta
        /// </summary>
        /// <remarks>
        /// This method is really simple, but I'm tired of trial and error with multiplication order every time I need
        /// to rotate quaternions
        /// </remarks>
        public static Quaternion RotateBy(this Quaternion quaternion, Quaternion delta)
        {
            //return delta.ToUnit() * quaternion.ToUnit();		// this is the one that's backward (I think)
            return quaternion.ToUnit() * delta.ToUnit();
        }

        /// <summary>
        /// This returns a quaternion that will rotate in the opposite direction
        /// </summary>
        public static Quaternion ToReverse(this Quaternion quaternion)
        {
            #region OLD

            // From MSDN:
            //		Conjugate - Replaces a quaternion with its conjugate.
            //		Invert - Replaces the specified quaternion with its inverse
            //
            // Awesome explanation.  I'm assuming that conjugate is the inverse of a unit quaternion, and invert is the inverse of any quaternion (slower but safer)
            //
            // I poked around, and found the source for quaternion here:
            // http://reflector.webtropy.com/default.aspx/Dotnetfx_Vista_SP2/Dotnetfx_Vista_SP2/8@0@50727@4016/DEVDIV/depot/DevDiv/releases/Orcas/QFE/wpf/src/Core/CSharp/System/Windows/Media3D/Quaternion@cs/1/Quaternion@cs
            //
            // Here is the important part of the invert method:
            //		Conjugate();
            //		double norm2 = _x * _x + _y * _y + _z * _z + _w * _w;
            //		_x /= norm2;
            //		_y /= norm2;
            //		_z /= norm2;
            //		_w /= norm2;

            // I was about to do this, but I'm not sure if that would return what I think, so I'll just use the same axis and invert the angle
            //Quaternion retVal = quaternion;
            //if (!retVal.IsNormalized)
            //{
            //    retVal.Normalize();
            //}
            //retVal.Conjugate();
            //return retVal;

            #endregion

            if (quaternion.IsIdentity)
            {
                return Quaternion.Identity;
            }
            else
            {
                return new Quaternion(quaternion.Axis, quaternion.Angle * -1d);
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

            // Exit Function
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

            // Exit Function
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

            // Exit Function
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

        #region Rect

        public static double CenterX(this Rect rect)
        {
            return rect.X + (rect.Width / 2d);
        }
        public static double CenterY(this Rect rect)
        {
            return rect.Y + (rect.Height / 2d);
        }

        public static Point Center(this Rect rect)
        {
            return new Point(rect.CenterX(), rect.CenterY());
        }

        /// <summary>
        /// This returns a rectangle that is the new size, but still centered around the original's center point
        /// </summary>
        public static Rect ChangeSize(this Rect rect, double multiplier)
        {
            double halfWidth = rect.Width / 2;
            double halfHeight = rect.Height / 2;

            return new Rect(
                (rect.X + halfWidth) - (halfWidth * multiplier),
                (rect.Y + halfHeight) - (halfHeight * multiplier),
                rect.Width * multiplier,
                rect.Height * multiplier);
        }

        public static Rect3D ToRect3D(this Rect rect, double z = 0)
        {
            return new Rect3D(rect.Location.ToPoint3D(z), rect.Size.ToSize3D());
        }

        #endregion

        #region Rect3D

        public static double CenterX(this Rect3D rect)
        {
            return rect.X + (rect.SizeX / 2d);
        }
        public static double CenterY(this Rect3D rect)
        {
            return rect.Y + (rect.SizeY / 2d);
        }
        public static double CenterZ(this Rect3D rect)
        {
            return rect.Z + (rect.SizeZ / 2d);
        }

        /// <summary>
        /// Returns true if either rectangle is inside the other or touching
        /// </summary>
        public static bool OverlapsWith(this Rect3D thisRect, Rect3D rect)
        {
            return thisRect.IntersectsWith(rect) || thisRect.Contains(rect) || rect.Contains(thisRect);
        }

        public static double DiagonalLength(this Rect3D rect)
        {
            return new Vector3D(rect.SizeX, rect.SizeY, rect.SizeZ).Length;
        }

        /// <summary>
        /// This returns a rectangle that is the new size, but still centered around the original's center point
        /// </summary>
        public static Rect3D ChangeSize(this Rect3D rect, double multiplier)
        {
            return new Rect3D(
                rect.X - (rect.SizeX * multiplier / 2),
                rect.Y - (rect.SizeY * multiplier / 2),
                rect.Z - (rect.SizeZ * multiplier / 2),
                rect.SizeX * multiplier,
                rect.SizeY * multiplier,
                rect.SizeZ * multiplier);
        }

        public static Rect ToRect2D(this Rect3D rect)
        {
            return new Rect(rect.Location.ToPoint2D(), rect.Size.ToSize2D());
        }

        #endregion

        #region Visual3DCollection

        // I got tired of writing these loops

        public static void AddRange(this Visual3DCollection collection, IEnumerable<Visual3D> visuals)
        {
            foreach (Visual3D visual in visuals)
            {
                collection.Add(visual);
            }
        }

        public static void RemoveAll(this Visual3DCollection collection, IEnumerable<Visual3D> visuals)
        {
            foreach (Visual3D visual in visuals)
            {
                collection.Remove(visual);
            }
        }

        #endregion

        #region UIElementCollection

        public static void AddRange(this UIElementCollection collection, IEnumerable<UIElement> visuals)
        {
            foreach (UIElement visual in visuals)
            {
                collection.Add(visual);
            }
        }

        public static void RemoveAll(this UIElementCollection collection, IEnumerable<UIElement> visuals)
        {
            foreach (UIElement visual in visuals)
            {
                collection.Remove(visual);
            }
        }

        #endregion

        #region TreeView

        /// <summary>
        /// There is no native easy way to clear a treeview's selection, so I added this
        /// </summary>
        public static void ClearSelection(this TreeView treeview)
        {
            ClearTreeViewSelection(treeview.Items, treeview.ItemContainerGenerator);
        }

        #endregion

        #region Random

        public static double NextBell(this Random rand, RandomBellArgs args)
        {
            double retVal = BezierUtil.GetPoint(rand.NextDouble(), args.Bezier).Y;

            if (retVal < 0) retVal = 0;
            else if (retVal > 1) retVal = 1;

            return retVal;
        }

        /// <summary>
        /// The percent will go to 1/max to max
        /// </summary>
        public static double NextBellPercent(this Random rand, RandomBellArgs args, double max)
        {
            // Get a value from 0 to 1 (the bell args could be anything, but it would make sense for this method if it's centered on .5)
            double retVal = rand.NextBell(args);

            // Adjust to centered at zero.  Multiply by two so it goes -1 to 1.  Then multiply by max-1 so it goes -max-1 to max-1
            // (the 1 gets added in the next step)
            retVal = (retVal - .5) * (2 * (max - 1));

            // Turn into a multiplier
            if (retVal < 0)
            {
                retVal = 1 / (1 + Math.Abs(retVal));
            }
            else
            {
                retVal += 1;
            }

            return retVal;
        }

        #endregion

        #region double[]

        public static Vector ToVector(this double[] values, bool enforceSize = true)
        {
            if (enforceSize)
            {
                if (values == null)
                {
                    throw new ArgumentNullException("values");
                }
                else if (values.Length != 2)
                {
                    throw new ArgumentOutOfRangeException("values", string.Format("This method requires the double array to be length 2.  len={0}", values.Length));
                }

                return new Vector(values[0], values[1]);
            }
            else
            {
                if (values == null)
                {
                    return new Vector();
                }

                return new Vector
                (
                    values.Length >= 1 ?
                        values[0] :
                        0,
                    values.Length >= 2 ?
                        values[1] :
                        0
                );
            }
        }
        public static Vector3D ToVector3D(this double[] values, bool enforceSize = true)
        {
            if (enforceSize)
            {
                if (values == null)
                {
                    throw new ArgumentNullException("values");
                }
                else if (values.Length != 3)
                {
                    throw new ArgumentOutOfRangeException("values", string.Format("This method requires the double array to be length 3.  len={0}", values.Length));
                }

                return new Vector3D(values[0], values[1], values[2]);
            }
            else
            {
                if (values == null)
                {
                    return new Vector3D();
                }

                return new Vector3D
                (
                    values.Length >= 1 ?
                        values[0] :
                        0,
                    values.Length >= 2 ?
                        values[1] :
                        0,
                    values.Length >= 3 ?
                        values[2] :
                        0
                );
            }
        }
        public static VectorND ToVectorND(this double[] values)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }
            else if (values.Length == 0)
            {
                throw new ArgumentOutOfRangeException("values", "This method requires the double array to be greater than length 0");
            }

            return new VectorND(values);
        }

        public static Point ToPoint(this double[] values, bool enforceSize = true)
        {
            if (enforceSize)
            {
                if (values == null)
                {
                    throw new ArgumentNullException("values");
                }
                else if (values.Length != 2)
                {
                    throw new ArgumentOutOfRangeException("values", string.Format("This method requires the double array to be length 2.  len={0}", values.Length));
                }

                return new Point(values[0], values[1]);
            }
            else
            {
                if (values == null)
                {
                    return new Point();
                }

                return new Point
                (
                    values.Length >= 1 ?
                        values[0] :
                        0,
                    values.Length >= 2 ?
                        values[1] :
                        0
                );
            }
        }
        public static Point3D ToPoint3D(this double[] values, bool enforceSize = true)
        {
            if (enforceSize)
            {
                if (values == null)
                {
                    throw new ArgumentNullException("values");
                }
                else if (values.Length != 3)
                {
                    throw new ArgumentOutOfRangeException("values", string.Format("This method requires the double array to be length 3.  len={0}", values.Length));
                }

                return new Point3D(values[0], values[1], values[2]);
            }
            else
            {
                if (values == null)
                {
                    return new Point3D();
                }

                return new Point3D
                (
                    values.Length >= 1 ?
                        values[0] :
                        0,
                    values.Length >= 2 ?
                        values[1] :
                        0,
                    values.Length >= 3 ?
                        values[2] :
                        0
                );
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Treeview doesn't have a simple clear selection
        /// </summary>
        /// <remarks>
        /// Got this here:
        /// http://stackoverflow.com/questions/676819/wpf-treeview-clear-selection
        /// </remarks>
        private static void ClearTreeViewSelection(ItemCollection items, ItemContainerGenerator containerGenerator)
        {
            if (items != null && containerGenerator != null)
            {
                for (int cntr = 0; cntr < items.Count; cntr++)
                {
                    // Can't use item directly, because it could be any UIElement
                    TreeViewItem itemContainer = containerGenerator.ContainerFromIndex(cntr) as TreeViewItem;
                    if (itemContainer != null)
                    {
                        // Recurse
                        ClearTreeViewSelection(itemContainer.Items, itemContainer.ItemContainerGenerator);
                        itemContainer.IsSelected = false;
                    }
                }
            }
        }

        #endregion
    }
}
