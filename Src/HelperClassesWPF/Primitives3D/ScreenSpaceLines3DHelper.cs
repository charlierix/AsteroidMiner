using System;
using System.Windows.Media.Media3D;
using Game.HelperClassesWPF.Primitives3D;

namespace Game.HelperClassesWPF.Primitives3D
{
    public static class ScreenSpaceLines3DHelper
    {
        public static void AddCirle(ScreenSpaceLines3D line, Point3D center, double radius, int segments)
        {
            AddArc(line, center, radius, segments, 0, 360, false);
        }

        public static void AddArc(ScreenSpaceLines3D line, Point3D center, double radius, int segments, double startAngle, double stopAngle)
        {
            AddArc(line, center, radius, segments, startAngle, stopAngle, false);
        }

        private static void AddArc(ScreenSpaceLines3D line, Point3D center, double radius, int segments, double startAngle, double stopAngle, bool closeEnd)
        {
            startAngle = Math3D.DegreesToRadians(startAngle);
            stopAngle = Math3D.DegreesToRadians(stopAngle);

            // swap angles
            if (startAngle > stopAngle)
            {
                double temp = startAngle;
                startAngle = stopAngle;
                stopAngle = temp;
            }

            Point3D[] points = new Point3D[segments + 1];
            double inc = (stopAngle - startAngle) / segments;

            double r = startAngle;
            for (int i = 0; i <= segments; i++, r += inc)
            {
                points[i] = new Point3D(
                    center.X + (Math.Cos(-r) * radius),
                    center.Y + (Math.Sin(-r) * radius),
                    center.Z);
            }

            line.AddPolygon(closeEnd, points);
        }
    }
}
