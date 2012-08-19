using Game.Newt.NewtonDynamics_153.Api;

namespace Game.Newt.NewtonDynamics_153
{
    public abstract class ConvexCollisionMask : CollisionMask
    {
        public ConvexCollisionMask()
        {
        }

        public ConvexCollisionMask(World world)
            : base(world)
        {
        }

        public new CCollisionConvexPrimitives NewtonCollision
        {
            get { return (CCollisionConvexPrimitives)base.NewtonCollision; }
        }
    }
}
