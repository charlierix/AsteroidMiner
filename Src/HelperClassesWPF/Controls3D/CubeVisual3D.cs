using System.Windows;
using System.Windows.Media.Media3D;

namespace Game.HelperClassesWPF.Controls3D
{
    public class CubeVisual3D : TessellianVisual3D
    {
        protected override Geometry3D Tessellate()
        {
            MeshGeometry3D mesh = new MeshGeometry3D();

            Point3D min = new Point3D(-0.5, -0.5, -0.5);
            Point3D max = new Point3D(0.5, 0.5, 0.5);

            #region Front Plane

            mesh.Positions.Add(new Point3D(min.X, max.Y, max.Z));
            mesh.Positions.Add(new Point3D(min.X, min.Y, max.Z));
            mesh.Positions.Add(new Point3D(max.X, min.Y, max.Z));
            mesh.Positions.Add(new Point3D(max.X, max.Y, max.Z));

            mesh.TextureCoordinates.Add(new Point(0, 0));
            mesh.TextureCoordinates.Add(new Point(0, 1));
            mesh.TextureCoordinates.Add(new Point(1, 1));
            mesh.TextureCoordinates.Add(new Point(1, 0));

            mesh.Normals.Add(new Vector3D(0, 0, 1));
            mesh.Normals.Add(new Vector3D(0, 0, 1));
            mesh.Normals.Add(new Vector3D(0, 0, 1));
            mesh.Normals.Add(new Vector3D(0, 0, 1));

            #endregion

            #region Right Plane

            mesh.Positions.Add(new Point3D(max.X, max.Y, max.Z));
            mesh.Positions.Add(new Point3D(max.X, min.Y, max.Z));
            mesh.Positions.Add(new Point3D(max.X, min.Y, min.Z));
            mesh.Positions.Add(new Point3D(max.X, max.Y, min.Z));

            mesh.TextureCoordinates.Add(new Point(0, 0));
            mesh.TextureCoordinates.Add(new Point(0, 1));
            mesh.TextureCoordinates.Add(new Point(1, 1));
            mesh.TextureCoordinates.Add(new Point(1, 0));

            mesh.Normals.Add(new Vector3D(1, 0, 0));
            mesh.Normals.Add(new Vector3D(1, 0, 0));
            mesh.Normals.Add(new Vector3D(1, 0, 0));
            mesh.Normals.Add(new Vector3D(1, 0, 0));

            #endregion

            #region Back Plane

            mesh.Positions.Add(new Point3D(max.X, max.Y, min.Z));
            mesh.Positions.Add(new Point3D(max.X, min.Y, min.Z));
            mesh.Positions.Add(new Point3D(min.X, min.Y, min.Z));
            mesh.Positions.Add(new Point3D(min.X, max.Y, min.Z));

            mesh.TextureCoordinates.Add(new Point(0, 0));
            mesh.TextureCoordinates.Add(new Point(0, 1));
            mesh.TextureCoordinates.Add(new Point(1, 1));
            mesh.TextureCoordinates.Add(new Point(1, 0));

            mesh.Normals.Add(new Vector3D(0, 0, -1));
            mesh.Normals.Add(new Vector3D(0, 0, -1));
            mesh.Normals.Add(new Vector3D(0, 0, -1));
            mesh.Normals.Add(new Vector3D(0, 0, -1));

            #endregion

            #region Left Plane

            mesh.Positions.Add(new Point3D(min.X, max.Y, min.Z));
            mesh.Positions.Add(new Point3D(min.X, min.Y, min.Z));
            mesh.Positions.Add(new Point3D(min.X, min.Y, max.Z));
            mesh.Positions.Add(new Point3D(min.X, max.Y, max.Z));

            mesh.TextureCoordinates.Add(new Point(0, 0));
            mesh.TextureCoordinates.Add(new Point(0, 1));
            mesh.TextureCoordinates.Add(new Point(1, 1));
            mesh.TextureCoordinates.Add(new Point(1, 0));

            mesh.Normals.Add(new Vector3D(-1, 0, 0));
            mesh.Normals.Add(new Vector3D(-1, 0, 0));
            mesh.Normals.Add(new Vector3D(-1, 0, 0));
            mesh.Normals.Add(new Vector3D(-1, 0, 0));

            #endregion

            #region Top Plane

            mesh.Positions.Add(new Point3D(min.X, max.Y, min.Z));
            mesh.Positions.Add(new Point3D(min.X, max.Y, max.Z));
            mesh.Positions.Add(new Point3D(max.X, max.Y, max.Z));
            mesh.Positions.Add(new Point3D(max.X, max.Y, min.Z));

            mesh.TextureCoordinates.Add(new Point(0, 0));
            mesh.TextureCoordinates.Add(new Point(0, 1));
            mesh.TextureCoordinates.Add(new Point(1, 1));
            mesh.TextureCoordinates.Add(new Point(1, 0));

            mesh.Normals.Add(new Vector3D(0, 1, 0));
            mesh.Normals.Add(new Vector3D(0, 1, 0));
            mesh.Normals.Add(new Vector3D(0, 1, 0));
            mesh.Normals.Add(new Vector3D(0, 1, 0));

            #endregion

            #region Bottom Plane

            mesh.Positions.Add(new Point3D(min.X, min.Y, max.Z));
            mesh.Positions.Add(new Point3D(min.X, min.Y, min.Z));
            mesh.Positions.Add(new Point3D(max.X, min.Y, min.Z));
            mesh.Positions.Add(new Point3D(max.X, min.Y, max.Z));

            mesh.TextureCoordinates.Add(new Point(0, 0));
            mesh.TextureCoordinates.Add(new Point(0, 1));
            mesh.TextureCoordinates.Add(new Point(1, 1));
            mesh.TextureCoordinates.Add(new Point(1, 0));

            mesh.Normals.Add(new Vector3D(0, -1, 0));
            mesh.Normals.Add(new Vector3D(0, -1, 0));
            mesh.Normals.Add(new Vector3D(0, -1, 0));
            mesh.Normals.Add(new Vector3D(0, -1, 0));

            #endregion

            const int TL = 0;
            const int BL = 1;
            const int BR = 2;
            const int TR = 3;

            int i = 0;
            for (int face = 0; face < 6; face++)
            {
                mesh.TriangleIndices.Add(TL + i);
                mesh.TriangleIndices.Add(BL + i);
                mesh.TriangleIndices.Add(BR + i);
                mesh.TriangleIndices.Add(TL + i);
                mesh.TriangleIndices.Add(BR + i);
                mesh.TriangleIndices.Add(TR + i);

                i += 4;
            }

            return mesh;
        }
    }
}
