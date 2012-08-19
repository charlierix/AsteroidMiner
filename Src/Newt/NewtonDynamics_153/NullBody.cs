using System.Windows.Media.Media3D;

namespace Game.Newt.NewtonDynamics_153
{
    public class NullBody : Body, 
        INullBody
    {
        public NullBody()
        {
        }

        public NullBody(World world)
        {
            Initialise(world);
        }

        protected override CollisionMask OnInitialise(World world)
        {
            return new NullCollision(world);
        }

        protected override void CalculateMass(float mass)
        {
            // not used.
        }
    }
}
