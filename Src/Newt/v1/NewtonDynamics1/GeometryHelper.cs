using System.Collections.Generic;
using System.Windows.Media.Media3D;

namespace Game.Newt.v1.NewtonDynamics1
{
    public static class GeometryHelper
    {
        public static IList<Point3D> GetGeometryPoints(MeshGeometry3D mesh, Matrix3D transformMatrix)
        {
            if (mesh != null)
            {
                bool applyMatrix = (transformMatrix != Matrix3D.Identity);

                List<Point3D> points;
                if ((mesh.TriangleIndices != null) && (mesh.TriangleIndices.Count > 0))
                {
                    points = new List<Point3D>(mesh.TriangleIndices.Count);
                    for (int i = 0, count = mesh.TriangleIndices.Count; i < count; i++)
                    {
                        int positionIndex = mesh.TriangleIndices[i];
                        if ((positionIndex < 0) || (positionIndex >= mesh.Positions.Count))
                            break;

                        if (applyMatrix)
                            points.Add(transformMatrix.Transform(mesh.Positions[positionIndex]));
                        else
                            points.Add(mesh.Positions[positionIndex]);
                    }
                }
                else
                {
                    points = new List<Point3D>(mesh.Positions.Count);
                    if (applyMatrix)
                    {
                        foreach (Point3D p in mesh.Positions)
                            points.Add(transformMatrix.Transform(p));
                    }
                    else
                        points.AddRange(mesh.Positions);
                }

                if (points.Count > 0)
                    return points;
                else
                    return null;
            }
            else
                return null;
        }

        public static IList<Point3D> GetGeometryPoints(MeshGeometry3D mesh)
        {
            return GetGeometryPoints(mesh, Matrix3D.Identity);
        }

        public static BoundingBox GetBoundingBox(IEnumerable<Point3D> points)
        {
            BoundingBox box = new BoundingBox();

            if (points != null)
            {
                box.Min = new Point3D(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity);
                box.Max = new Point3D(double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity);
                foreach (Point3D point in points)
                {
                    if (point.X < box.Min.X)
                        box.Min.X = point.X;
                    if (point.Y < box.Min.Y)
                        box.Min.Y = point.Y;
                    if (point.Z < box.Min.Z)
                        box.Min.Z = point.Z;

                    if (point.X > box.Max.X)
                        box.Max.X = point.X;
                    if (point.Y > box.Max.Y)
                        box.Max.Y = point.Y;
                    if (point.Z > box.Max.Z)
                        box.Max.Z = point.Z;
                }
            }
            return box;
        }

        /*
        public static Rect Get2DBoundingBox(Viewport3D vp)
        {
            bool bOK;

            Viewport3DVisual vpv = ViewportHelper.GetViewport(vp.Children[0]);

            Matrix3D m = MathUtils.TryWorldToViewportTransform(vpv, out bOK);

            bool bFirst = true;
            Rect r = new Rect();

            foreach (Visual3D v3d in vp.Children)
            {
                if (v3d is ModelVisual3D)
                {
                    ModelVisual3D mv3d = (ModelVisual3D)v3d;
                    if (mv3d.Content is GeometryModel3D)
                    {
                        GeometryModel3D gm3d =
                            (GeometryModel3D)mv3d.Content;

                        if (gm3d.Geometry is MeshGeometry3D)
                        {
                            MeshGeometry3D mg3d =
                                (MeshGeometry3D)gm3d.Geometry;

                            foreach (Point3D p3d in mg3d.Positions)
                            {
                                Point3D pb = m.Transform(p3d);
                                Point p2d = new Point(pb.X, pb.Y);
                                if (bFirst)
                                {
                                    r = new Rect(p2d, new Size(1, 1));
                                    bFirst = false;
                                }
                                else
                                {
                                    r.Union(p2d);
                                }
                            }
                        }
                    }
                }
            }

            return r;
        }
         */
    }
}
