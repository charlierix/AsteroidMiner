using Game.Newt.NewtonDynamics_153.Api;

namespace Game.Newt.NewtonDynamics_153
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
