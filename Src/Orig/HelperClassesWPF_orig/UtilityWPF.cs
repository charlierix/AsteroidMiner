using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using Game.Orig.Math3D;

namespace Game.Orig.HelperClassesWPF
{
    public static class UtilityWPF
    {
        /// <summary>
        /// This will return a transform for a model or camera so that the model/camera will be placed and oriented just like
        /// the physical sphere.  Just set my return to model.Transform
        /// </summary>
        /// <param name="modelSphere">The tranform will be applied to the model that is described by this modelSphere</param>
        /// <param name="physicalSphere">This is where the model should be transformed to (the physical location)</param>
        public static Transform3D GetTransfromForSlaving(Sphere modelSphere, Sphere physicalSphere)
        {
            Transform3DGroup retVal = new Transform3DGroup();

            // Rotate
            MyQuaternion rotation = modelSphere.DirectionFacing.GetAngleAroundAxis(physicalSphere.DirectionFacing);
            retVal.Children.Add(new RotateTransform3D(new QuaternionRotation3D(new Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W))));

            //// Translate
            //MyVector offset = physicalSphere.Position - modelSphere.Position;
            //retVal.Children.Add(new TranslateTransform3D(offset.X, offset.Y, offset.Z));

            // Exit Function
            return retVal;
        }

        public static MeshGeometry3D GetCube(double size)
        {
            double halfSize = size / 2d;

            // Define 3D mesh object
            MeshGeometry3D retVal = new MeshGeometry3D();

            retVal.Positions.Add(new Point3D(-halfSize, -halfSize, halfSize));
            retVal.Positions.Add(new Point3D(halfSize, -halfSize, halfSize));
            retVal.Positions.Add(new Point3D(halfSize, halfSize, halfSize));
            retVal.Positions.Add(new Point3D(-halfSize, halfSize, halfSize));

            retVal.Positions.Add(new Point3D(-halfSize, -halfSize, -halfSize));
            retVal.Positions.Add(new Point3D(halfSize, -halfSize, -halfSize));
            retVal.Positions.Add(new Point3D(halfSize, halfSize, -halfSize));
            retVal.Positions.Add(new Point3D(-halfSize, halfSize, -halfSize));

            // Front face
            retVal.TriangleIndices.Add(0);
            retVal.TriangleIndices.Add(1);
            retVal.TriangleIndices.Add(2);
            retVal.TriangleIndices.Add(2);
            retVal.TriangleIndices.Add(3);
            retVal.TriangleIndices.Add(0);

            // Back face
            retVal.TriangleIndices.Add(6);
            retVal.TriangleIndices.Add(5);
            retVal.TriangleIndices.Add(4);
            retVal.TriangleIndices.Add(4);
            retVal.TriangleIndices.Add(7);
            retVal.TriangleIndices.Add(6);

            // Right face
            retVal.TriangleIndices.Add(1);
            retVal.TriangleIndices.Add(5);
            retVal.TriangleIndices.Add(2);
            retVal.TriangleIndices.Add(5);
            retVal.TriangleIndices.Add(6);
            retVal.TriangleIndices.Add(2);

            // Top face
            retVal.TriangleIndices.Add(2);
            retVal.TriangleIndices.Add(6);
            retVal.TriangleIndices.Add(3);
            retVal.TriangleIndices.Add(3);
            retVal.TriangleIndices.Add(6);
            retVal.TriangleIndices.Add(7);

            // Bottom face
            retVal.TriangleIndices.Add(5);
            retVal.TriangleIndices.Add(1);
            retVal.TriangleIndices.Add(0);
            retVal.TriangleIndices.Add(0);
            retVal.TriangleIndices.Add(4);
            retVal.TriangleIndices.Add(5);

            // Right face
            retVal.TriangleIndices.Add(4);
            retVal.TriangleIndices.Add(0);
            retVal.TriangleIndices.Add(3);
            retVal.TriangleIndices.Add(3);
            retVal.TriangleIndices.Add(7);
            retVal.TriangleIndices.Add(4);

            // Exit Function
            retVal.Freeze();
            return retVal;
        }
        public static MeshGeometry3D GetSphere(int separators, double radius)
        {
            double segmentRad = Math.PI / 2 / (separators + 1);
            int numberOfSeparators = 4 * separators + 4;

            MeshGeometry3D retVal = new MeshGeometry3D();

            // Calculate all the positions
            for (int e = -separators; e <= separators; e++)
            {
                double r_e = radius * Math.Cos(segmentRad * e);
                double y_e = radius * Math.Sin(segmentRad * e);

                for (int s = 0; s <= (numberOfSeparators - 1); s++)
                {
                    double z_s = r_e * Math.Sin(segmentRad * s) * (-1);
                    double x_s = r_e * Math.Cos(segmentRad * s);
                    retVal.Positions.Add(new Point3D(x_s, y_e, z_s));
                }
            }
            retVal.Positions.Add(new Point3D(0, radius, 0));
            retVal.Positions.Add(new Point3D(0, -1 * radius, 0));

            // Main Body
            int maxIterate = 2 * separators;
            for (int y = 0; y < maxIterate; y++)      // phi?
            {
                for (int x = 0; x < numberOfSeparators; x++)      // theta?
                {
                    retVal.TriangleIndices.Add(y * numberOfSeparators + (x + 1) % numberOfSeparators + numberOfSeparators);
                    retVal.TriangleIndices.Add(y * numberOfSeparators + x + numberOfSeparators);
                    retVal.TriangleIndices.Add(y * numberOfSeparators + x);

                    retVal.TriangleIndices.Add(y * numberOfSeparators + x);
                    retVal.TriangleIndices.Add(y * numberOfSeparators + (x + 1) % numberOfSeparators);
                    retVal.TriangleIndices.Add(y * numberOfSeparators + (x + 1) % numberOfSeparators + numberOfSeparators);
                }
            }

            // Top Cap
            for (int i = 0; i < numberOfSeparators; i++)
            {
                retVal.TriangleIndices.Add(maxIterate * numberOfSeparators + i);
                retVal.TriangleIndices.Add(maxIterate * numberOfSeparators + (i + 1) % numberOfSeparators);
                retVal.TriangleIndices.Add(numberOfSeparators * (2 * separators + 1));
            }

            // Bottom Cap
            for (int i = 0; i < numberOfSeparators; i++)
            {
                retVal.TriangleIndices.Add(numberOfSeparators * (2 * separators + 1) + 1);
                retVal.TriangleIndices.Add((i + 1) % numberOfSeparators);
                retVal.TriangleIndices.Add(i);

            }

            // Exit Function
            retVal.Freeze();
            return retVal;
        }
    }
}
