using System.Windows.Media.Media3D;

namespace Game.Newt.HelperClasses
{
    public static class Transform3DHelper
    {
        public static Transform3DGroup CreateTransformTRS(Vector3D offset, Vector3D rotationAxis, double rotationAngle, Vector3D scale)
        {
            Transform3DGroup result = new Transform3DGroup();
            TranslateTransform3D t = new TranslateTransform3D(offset);
            RotateTransform3D r = new RotateTransform3D(new AxisAngleRotation3D(rotationAxis, rotationAngle));
            ScaleTransform3D s = new ScaleTransform3D(scale);

            result.Children.Add(s);
            result.Children.Add(r);
            result.Children.Add(t);

            return result;
        }

        /*
        public static Transform3DGroup CreateTransformTRS(Vector3D offset, Vector3D rotation, Vector3D scale)
        {
            Transform3DGroup result = new Transform3DGroup();
            TranslateTransform3D t = new TranslateTransform3D(offset);
            MatrixTransform3D r = Math3D.CreateRotationTransform(rotation);
            ScaleTransform3D s = new ScaleTransform3D(scale);

            result.Children.Add(s);
            result.Children.Add(r);
            result.Children.Add(t);

            return result;
        }
        */

        public static Transform3DGroup CreateTransformTS(Vector3D offset, double scale)
        {
            Transform3DGroup result = new Transform3DGroup();
            TranslateTransform3D t = new TranslateTransform3D(offset);
            ScaleTransform3D s = new ScaleTransform3D(scale, scale, scale);

            result.Children.Add(s);
            result.Children.Add(t);

            return result;
        }

        /*
        public static Transform3DGroup CreateTransformTR(Vector3D offset, Vector3D rotation)
        {
            Transform3DGroup result = new Transform3DGroup();
            TranslateTransform3D t = new TranslateTransform3D(offset);
            MatrixTransform3D r = Math3D.CreateRotationTransform(rotation);

            result.Children.Add(r);
            result.Children.Add(t);

            return result;
        }
         */
    }
}
