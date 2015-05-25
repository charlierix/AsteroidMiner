using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Media3D;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
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

        public static string ToString(this Vector vector, bool extensionsVersion)
        {
            return vector.X.ToString() + ", " + vector.Y.ToString();
        }
        public static string ToString(this Vector vector, int significantDigits)
        {
            return vector.X.ToString("N" + significantDigits.ToString()) + ", " + vector.Y.ToString("N" + significantDigits.ToString());
        }

        /// <summary>
        /// I was getting tired of needing two statements to get a unit vector
        /// </summary>
        public static Vector ToUnit(this Vector vector, bool useNaNIfInvalid = true)
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

        public static string ToString(this Point point, bool extensionsVersion)
        {
            return point.X.ToString() + ", " + point.Y.ToString();
        }
        public static string ToString(this Point point, int significantDigits)
        {
            return point.X.ToString("N" + significantDigits.ToString()) + ", " + point.Y.ToString("N" + significantDigits.ToString());
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
        public static Vector3D ToUnit(this Vector3D vector, bool useNaNIfInvalid = true)
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

        public static string ToString(this Point3D point, bool extensionsVersion)
        {
            return point.X.ToString() + ", " + point.Y.ToString() + ", " + point.Z.ToString();
        }
        public static string ToString(this Point3D point, int significantDigits)
        {
            return point.X.ToString("N" + significantDigits.ToString()) + ", " + point.Y.ToString("N" + significantDigits.ToString()) + ", " + point.Z.ToString("N" + significantDigits.ToString());
        }

        /// <summary>
        /// I was getting tired of needing two statements to get a unit vector
        /// </summary>
        /// <param name="useNaNIfInvalid">
        /// True=Standard behavior.  By definition a unit vector always has length of one, so if the initial length is zero, then the length becomes NaN
        /// False=Vector just goes to zero
        /// </param>
        public static Point3D ToUnit(this Point3D point, bool useNaNIfInvalid = true)
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

        #region Rect3D

        /// <summary>
        /// Returns true if either rectangle is inside the other or touching
        /// </summary>
        public static bool OverlapsWith(this Rect3D thisRect, Rect3D rect)
        {
            return thisRect.IntersectsWith(rect) || thisRect.Contains(rect) || rect.Contains(thisRect);
        }

        public static double DiagonalLength(this Rect3D thisRect)
        {
            return new Vector3D(thisRect.SizeX, thisRect.SizeY, thisRect.SizeZ).Length;
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
    }
}
