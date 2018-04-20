using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;

namespace Game.HelperClassesWPF
{
    public static class StaticRandomWPF
    {
        public static double NextBell(RandomBellArgs args)
        {
            return StaticRandom.GetRandomForThread().NextBell(args);
        }
    }

    #region Class: RandomBellArgs

    public class RandomBellArgs
    {
        /// <summary>
        /// Play with the NonlinearRandom tester to come up with values
        /// </summary>
        public RandomBellArgs(double leftArmLength, double leftArmAngle, double rightArmLength, double rightArmAngle)
        {
            List<Point3D> controlPoints = new List<Point3D>();

            // Arm1
            if (!leftArmLength.IsNearZero())
            {
                Vector arm1 = new Vector(1, 1).ToUnit() * leftArmLength;

                controlPoints.Add(arm1.ToVector3D().GetRotatedVector(new Vector3D(0, 0, -1), leftArmAngle).ToPoint());
            }

            // Arm2
            if (!rightArmLength.IsNearZero())
            {
                Vector arm2 = new Vector(-1, -1).ToUnit() * rightArmLength;

                Vector3D arm2Rotated = arm2.
                    ToVector3D().
                    GetRotatedVector(new Vector3D(0, 0, -1), rightArmAngle);

                controlPoints.Add(new Point3D(1 + arm2Rotated.X, 1 + arm2Rotated.Y, 0));
            }

            // Bezier
            this.Bezier = new BezierSegment3D(0, 1, controlPoints.ToArray(), new[] { new Point3D(0, 0, 0), new Point3D(1, 1, 0) });
        }

        //NOTE: The bezier currenly only supports 3D.  This class only needs 2D
        public readonly BezierSegment3D Bezier;
    }

    #endregion
}
