using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;

namespace Game.HelperClassesWPF
{
    /// <summary>
    /// This is useful for holding pairs of vectors (a vector, and some orthoganal to that vector).  There is nothing in this class
    /// that enforces that, it's just a simple container
    /// </summary>
    public struct DoubleVector
    {
        public Vector3D Standard;
        public Vector3D Orth;

        public DoubleVector(Vector3D standard, Vector3D orthogonalToStandard)
        {
            this.Standard = standard;
            this.Orth = orthogonalToStandard;
        }
        public DoubleVector(double standardX, double standardY, double standardZ, double orthogonalX, double orthogonalY, double orthogonalZ)
        {
            this.Standard = new Vector3D(standardX, standardY, standardZ);
            this.Orth = new Vector3D(orthogonalX, orthogonalY, orthogonalZ);
        }

        public Quaternion GetRotation(DoubleVector destination)
        {
            return Math3D.GetRotation(this, destination);
        }

        /// <summary>
        /// Rotates the double vector around the angle in degrees
        /// </summary>
        public DoubleVector GetRotatedVector(Vector3D axis, double angle)
        {
            Matrix3D matrix = new Matrix3D();
            matrix.Rotate(new Quaternion(axis, angle));

            MatrixTransform3D transform = new MatrixTransform3D(matrix);

            return new DoubleVector(transform.Transform(this.Standard), transform.Transform(this.Orth));
        }
    }
}
