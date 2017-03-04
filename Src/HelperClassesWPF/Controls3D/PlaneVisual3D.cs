using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Media3D;
using System.Windows;

namespace Game.HelperClassesWPF.Controls3D
{
    //TODO: a plane is not a Tessellian
    public class PlaneVisual3D : TessellianVisual3D
    {
        protected override Geometry3D Tessellate()
        {
            MeshGeometry3D mesh = new MeshGeometry3D();

            mesh.Positions.Add(new Point3D(-0.5, 0.5, 0));
            mesh.Positions.Add(new Point3D(-0.5, -0.5, 0));
            mesh.Positions.Add(new Point3D(0.5, -0.5, 0));
            mesh.Positions.Add(new Point3D(0.5, 0.5, 0));

            mesh.TextureCoordinates.Add(new Point(0, 0));
            mesh.TextureCoordinates.Add(new Point(0, 1));
            mesh.TextureCoordinates.Add(new Point(1, 1));
            mesh.TextureCoordinates.Add(new Point(1, 0));

            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(2);
            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(2);
            mesh.TriangleIndices.Add(3);

            mesh.Normals.Add(new Vector3D(0, 0, 1));
            mesh.Normals.Add(new Vector3D(0, 0, 1));
            mesh.Normals.Add(new Vector3D(0, 0, 1));
            mesh.Normals.Add(new Vector3D(0, 0, 1));

            return mesh;
        }
    }
}
