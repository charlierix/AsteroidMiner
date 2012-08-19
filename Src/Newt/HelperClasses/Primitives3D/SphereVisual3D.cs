using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Game.Newt.HelperClasses.Primitives3D
{
    public class SphereVisual3D : TessellianVisual3D
    {
        internal static Point3D GetPosition(double t, double y)
        {
            double r = Math.Sqrt(1 - y * y);
            double x = r * Math.Cos(t);
            double z = r * Math.Sin(t);

            return new Point3D(x, y, z);
        }

        private static Vector3D GetNormal(double t, double y)
        {
            return (Vector3D) GetPosition(t, y);
        }

        private static Point GetTextureCoordinate(double t, double y)
        {
            Matrix TYtoUV = new Matrix();
            TYtoUV.Scale(1 / (2 * Math.PI), -0.5);

            Point p = new Point(t, y);
            p = p * TYtoUV;

            return p;
        }

        protected override Geometry3D Tessellate()
        {
            int hDiv = this.Columns;
            int vDiv = this.Rows;

            double maxTheta = DegreesToRadians(360.0);
            double minY = -1;
            double maxY = 1;

            double dt = maxTheta / hDiv;
            double dy = (maxY - minY) / vDiv;

            MeshGeometry3D mesh = new MeshGeometry3D();

            for (int yi = 0; yi <= vDiv; yi++)
            {
                double y = minY + yi * dy;

                for (int ti = 0; ti <= hDiv; ti++)
                {
                    double t = ti * dt;

                    Point3D p = GetPosition(t, y);
                    p.X /= 2;
                    p.Y /= 2;
                    p.Z /= 2;
                    mesh.Positions.Add(p);
                    mesh.Normals.Add(GetNormal(t, y));
                    mesh.TextureCoordinates.Add(GetTextureCoordinate(t, y));
                }
            }

            for (int yi = 0; yi < vDiv; yi++)
            {
                for (int ti = 0; ti < hDiv; ti++)
                {
                    int x0 = ti;
                    int x1 = (ti + 1);
                    int y0 = yi * (hDiv + 1);
                    int y1 = (yi + 1) * (hDiv + 1);

                    mesh.TriangleIndices.Add(x0 + y0);
                    mesh.TriangleIndices.Add(x0 + y1);
                    mesh.TriangleIndices.Add(x1 + y0);

                    mesh.TriangleIndices.Add(x1 + y0);
                    mesh.TriangleIndices.Add(x0 + y1);
                    mesh.TriangleIndices.Add(x1 + y1);
                }
            }

            return mesh;
        }
    }
}