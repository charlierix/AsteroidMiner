using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Media3D;

namespace Game.Newt.HelperClasses
{
    public static class CameraHelper
    {
        public static void LookAt(Camera camera, Point3D worldPos)
        {
            ProjectionCamera c = (camera as ProjectionCamera);
            if (c == null)
                throw new ArgumentException("Only projection camera's can be set to look at.");

            Vector3D direction = (worldPos - c.Position);
            direction.Normalize();

            c.LookDirection = direction;
        }

        public static void LookAt(Camera camera, Visual3D visual)
        {
            Matrix3D visualToWorld = MathUtils.GetTransformToWorld(visual);
            LookAt(camera, visualToWorld.Transform(new Point3D()));
        }
    }
}
