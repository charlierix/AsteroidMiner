using Game.Newt.v1.NewtonDynamics1.Api;

namespace Game.Newt.v1.NewtonDynamics1
{
    public class NullCollision : CollisionMask
    {
        public NullCollision()
        {
            
        }

        public NullCollision(World world)
            : base(world)
        {
        }

        protected override CCollision OnInitialise()
        {
            CCollisionConvexPrimitives collision = new CCollisionConvexPrimitives(this.World.NewtonWorld);
            collision.CreateNull();
            return collision;
        }
    }
}
