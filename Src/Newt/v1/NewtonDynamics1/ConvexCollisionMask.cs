using Game.Newt.v1.NewtonDynamics1.Api;

namespace Game.Newt.v1.NewtonDynamics1
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
