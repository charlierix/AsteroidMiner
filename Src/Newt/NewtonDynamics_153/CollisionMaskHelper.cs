using System.Windows.Media.Media3D;

namespace Game.Newt.NewtonDynamics_153
{
    public static class CollisionMaskHelper
    {
        public static Point3D GetCenterPos(Body body)
        {
            BoundingBox box = GetBoundingBox(body);
            return box.CenterPos;
        }

        public static BoundingBox GetBoundingBox(Body body)
        {
            if (body is IMesh)
                return GeometryHelper.GetBoundingBox(((IMesh)body).GetPoints());
            else
                return new BoundingBox();
        }

        public static BoundingBox GetBoundingBox(Visual3DCollection visualItems)
        {
            BoundingBox result = new BoundingBox();

            foreach (Visual3D item in visualItems)
            {
                ModelVisual3D visual = (item as ModelVisual3D);
                if (visual != null)
                {
                    Body body = World.GetBody(visual);
                    if (body != null)
                    {
                        BoundingBox box = GetBoundingBox(body);

                        Matrix3D matrix = (body.Transform != null) ? body.Transform.Value : Matrix3D.Identity;
                        box.Min = matrix.Transform(box.Min);
                        box.Max = matrix.Transform(box.Max);

                        result.Union(box);
                    }
                }
            }

            return result;
        }
    }
}
