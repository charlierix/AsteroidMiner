using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;

namespace Game.Newt.HelperClasses
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

        /// <summary>
        /// This function takes in a destination double vector, and I will tell you how much you need to rotate me in order for me to end up
        /// along that destination double vector.
        /// </summary>
        /// <remarks>
        /// If I am already aligned with the vector passed in, then I will return an arbitrary orthoganal, and an angle of zero.
        /// </remarks>
        /// <param name="destination">This is the double vector you want me to align myself with</param>
        public Quaternion GetRotation(DoubleVector destination)
        {
            #region Standard

            // Get the angle
            double rotationDegrees = Vector3D.AngleBetween(this.Standard, destination.Standard);
            if (Double.IsNaN(rotationDegrees))
            {
                rotationDegrees = 0d;
            }

            // I need to pull the cross product from me to the vector passed in
            Vector3D rotationAxis = Vector3D.CrossProduct(this.Standard, destination.Standard);

            // If the cross product is zero, then there are two possibilities.  The vectors point in the same direction, or opposite directions.
            if (rotationAxis.IsNearZero())
            {
                // If I am here, then the angle will either be 0 or PI.
                if (Math3D.IsNearZero(rotationDegrees))
                {
                    // The vectors sit on top of each other.  I will set the orthoganal to an arbitrary value, and return zero for the radians
                    rotationAxis.X = 1d;
                    rotationAxis.Y = 0d;
                    rotationAxis.Z = 0d;
                    rotationDegrees = 0d;
                }
                else
                {
                    // The vectors are pointing directly away from each other, because this is a double vector, I must rotate along an axis that
                    // is orthogonal to my standard and orth
                    rotationAxis = this.Orth; //MyVector.Cross(this.Standard, this.Orth);
                }
            }

            Quaternion quatStandard = new Quaternion(rotationAxis, rotationDegrees);
            quatStandard.Normalize();

            #endregion
            #region Orthogonal

            // I only need to rotate the orth, because I already know where the standard will be
            Vector3D rotatedOrth = quatStandard.GetRotatedVector(this.Orth);

            Quaternion quatOrth = Math3D.GetRotation(rotatedOrth, destination.Orth);
            quatOrth.Normalize();

            #region OLD

            // This only works half the time (because Vector3D.AngleBetween won't go greater than 180, and the axis is always assumed to be destination.Standard, but
            // sometimes needs to be negated)

            //// Grab the angle
            //rotationDegrees = Vector3D.AngleBetween(rotatedOrth, destination.Orth);
            //if (Double.IsNaN(rotationDegrees))
            //{
            //    rotationDegrees = 0d;
            //}

            //// Since I've rotated the standards onto each other, the rotation axis of the orth is the standard (asumming it was truely orthogonal
            //// to begin with)
            //rotationAxis = destination.Standard;

            //Quaternion quatOrth = new Quaternion(rotationAxis, rotationDegrees);
            //quatOrth.Normalize();

            #endregion

            #endregion

            // Exit Function
            return Quaternion.Multiply(quatOrth, quatStandard);		// note that order is important (stand, orth is wrong)
        }
        private Quaternion GetRotation_ORIG(DoubleVector destination)
        {
            #region Standard

            // Get the angle
            double rotationDegrees = Vector3D.AngleBetween(this.Standard, destination.Standard);
            if (Double.IsNaN(rotationDegrees))
            {
                rotationDegrees = 0d;
            }

            // I need to pull the cross product from me to the vector passed in
            Vector3D rotationAxis = Vector3D.CrossProduct(this.Standard, destination.Standard);

            // If the cross product is zero, then there are two possibilities.  The vectors point in the same direction, or opposite directions.
            if (rotationAxis.IsNearZero())
            {
                // If I am here, then the angle will either be 0 or PI.
                if (Math3D.IsNearZero(rotationDegrees))
                {
                    // The vectors sit on top of each other.  I will set the orthoganal to an arbitrary value, and return zero for the radians
                    rotationAxis.X = 1d;
                    rotationAxis.Y = 0d;
                    rotationAxis.Z = 0d;
                    rotationDegrees = 0d;
                }
                else
                {
                    // The vectors are pointing directly away from each other, because this is a double vector, I must rotate along an axis that
                    // is orthogonal to my standard and orth
                    rotationAxis = this.Orth; //MyVector.Cross(this.Standard, this.Orth);
                }
            }

            Quaternion quatStandard = new Quaternion(rotationAxis, rotationDegrees);
            quatStandard.Normalize();

            #endregion

            // I only need to rotate the orth, because I already know where the standard will be
            Vector3D rotatedOrth = quatStandard.GetRotatedVector(this.Orth);

            #region Orthogonal

            // Grab the angle
            rotationDegrees = Vector3D.AngleBetween(rotatedOrth, destination.Orth);
            if (Double.IsNaN(rotationDegrees))
            {
                rotationDegrees = 0d;
            }

            // Since I've rotated the standards onto each other, the rotation axis of the orth is the standard (asumming it was truely orthogonal
            // to begin with)
            rotationAxis = destination.Standard;

            Quaternion quatOrth = new Quaternion(rotationAxis, rotationDegrees);
            quatOrth.Normalize();

            #endregion

            // Exit Function
            //return MyQuaternion.Multiply(quatOrth, quatStandard);
            return Quaternion.Multiply(quatStandard, quatOrth);
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
