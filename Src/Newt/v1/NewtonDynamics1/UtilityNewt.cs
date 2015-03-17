using System;
using System.Collections.Generic;
using System.Text;

using Game.Newt.v1.NewtonDynamics1.Api;

namespace Game.Newt.v1.NewtonDynamics1
{
    public static class UtilityNewt
    {
        public static Body GetBodyFromUserData(CBody body)
        {
            if (body.UserData is Body)
            {
                return (Body)body.UserData;
            }
            else if (body.UserData is IMapObject)
            {
                return (Body)((IMapObject)body.UserData).PhysicsBody;
            }

            return null;
        }
        public static ConvexBody3D GetConvexBodyFromUserData(CBody body)
        {
            if (body.UserData is ConvexBody3D)
            {
                return (ConvexBody3D)body.UserData;
            }
            else if (body.UserData is IMapObject)
            {
                return (ConvexBody3D)((IMapObject)body.UserData).PhysicsBody;
            }

            return null;
        }
    }
}
