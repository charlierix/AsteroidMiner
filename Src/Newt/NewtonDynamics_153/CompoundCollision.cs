using System;
using System.Collections.Generic;
using Game.Newt.NewtonDynamics_153.Api;
using System.Windows.Media.Media3D;

namespace Game.Newt.NewtonDynamics_153
{
    public class CompoundCollision : CollisionMask
    {
        private List<ConvexCollisionMask> _collisions = new List<ConvexCollisionMask>();

        public CompoundCollision(ICollection<ConvexCollisionMask> collisions)
        {
            if (collisions == null) throw new ArgumentNullException("collisions");
            if (collisions.Count < 1)
                throw new ArgumentOutOfRangeException(
                    "collisions",
                    collisions.Count,
                    "A Compound CollisionMaskMask has to contain at least one Convex CollisionMaskMask.");

            _collisions.AddRange(collisions);

            Initialise(_collisions[0].World);
        }

        public List<ConvexCollisionMask> Collisions
        {
            get { return _collisions; }
        }

        public new CCollisionComplexPrimitives NewtonCollision
        {
            get { return (CCollisionComplexPrimitives)base.NewtonCollision; }
        }

        public void Translate(Vector3D vector)
        {
            Matrix3D matrix = Matrix3D.Identity;
            matrix.Translate(vector);

            //TODO: maybe create a modifier collision if one doesn't exist.
            foreach (ConvexCollisionMask collision in this.Collisions)
            {
                ConvextCollisionModifier modifier = (collision as ConvextCollisionModifier);
                if (modifier != null)
                    modifier.ModifierMatrix = modifier.ModifierMatrix * matrix;
            }
        }

        protected override CCollision OnInitialise()
        {
            CCollisionComplexPrimitives collision = new CCollisionComplexPrimitives(this.World.NewtonWorld);
            collision.CreateCompound(GetNewtonCollisions(_collisions));
            return collision;
        }
    }
}
