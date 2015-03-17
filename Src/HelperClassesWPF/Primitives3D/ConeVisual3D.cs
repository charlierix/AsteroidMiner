using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Game.HelperClassesWPF.Primitives3D
{
    public class ConeVisual3D : TessellianVisual3D
    {
        static ConeVisual3D()
        {
            RowsProperty.OverrideMetadata(typeof(ConeVisual3D), new PropertyMetadata(1));
        }

        internal Point3D GetPosition(double t, double y)
        {
            double r = (0.5 - y) / 2;
            double x = r * Math.Cos(t);
            double z = r * Math.Sin(t);

            return new Point3D(x, y, z);
        }

        private Vector3D GetNormal(double t, double y)
        {
            double x = 2 * Math.Cos(t);
            double z = 2 * Math.Sin(t);

            return new Vector3D(x, 1, z);
        }

        private Point GetTextureCoordinate(double t, double y)
        {
            Matrix m = new Matrix();
            m.Scale(1 / (2 * Math.PI), -0.5);

            Point p = new Point(t, y);
            p = p * m;

            return p;
        }

        protected override Geometry3D Tessellate()
        {
            int tDiv = this.Columns;
            int yDiv = this.Rows;

            double maxTheta = DegreesToRadians(360.0);
            double minY = -0.5;
            double maxY = 0.5;

            double dt = maxTheta / tDiv;
            double dy = (maxY - minY) / yDiv;

            MeshGeometry3D mesh = new MeshGeometry3D();

            for (int yi = 0; yi <= yDiv; yi++)
            {
                double y = minY + yi * dy;

                for (int ti = 0; ti <= tDiv; ti++)
                {
                    double t = ti * dt;

                    mesh.Positions.Add(GetPosition(t, y));
                    mesh.Normals.Add(GetNormal(t, y));
                    mesh.TextureCoordinates.Add(GetTextureCoordinate(t, y));
                }
            }

            bool isSolid = this.IsSolid;
            if (isSolid)
            {
                double y = minY;
                for (int ti = 0; ti <= tDiv; ti++)
                {
                    double t = ti * dt;

                    mesh.Positions.Add(GetPosition(t, y));
                    mesh.Normals.Add(new Vector3D(0, -1, 0));
                    mesh.TextureCoordinates.Add(new Point(0, 0));
                }

                mesh.Positions.Add(new Point3D(0, y, 0));
                mesh.Normals.Add(new Vector3D(0, -1, 0));
                mesh.TextureCoordinates.Add(new Point(0, 0));
            }

            for (int yi = 0; yi < yDiv; yi++)
            {
                for (int ti = 0; ti < tDiv; ti++)
                {
                    int x0 = ti;
                    int x1 = (ti + 1);
                    int y0 = yi * (tDiv + 1);
                    int y1 = (yi + 1) * (tDiv + 1);

                    mesh.TriangleIndices.Add(x0 + y0);
                    mesh.TriangleIndices.Add(x0 + y1);
                    mesh.TriangleIndices.Add(x1 + y0);

                    mesh.TriangleIndices.Add(x1 + y0);
                    mesh.TriangleIndices.Add(x0 + y1);
                    mesh.TriangleIndices.Add(x1 + y1);
                }
            }

            if (isSolid)
            {
                int yi = yDiv;
                int last = mesh.Positions.Count - 1;
                for (int ti = 0; ti < tDiv; ti++)
                {
                    int x0 = ti;
                    int x1 = (ti + 1);
                    int y0 = yi * (tDiv + 1);
                    int y1 = (yi + 1) * (tDiv + 1);

                    mesh.TriangleIndices.Add(x0 + y1);
                    mesh.TriangleIndices.Add(x1 + y1);
                    mesh.TriangleIndices.Add(last);
                }
            }

            mesh.Freeze();
            return mesh;
        }
    }
}
