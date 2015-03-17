using System;
using Game.Newt.v1.NewtonDynamics1.Api;
using System.Windows.Media.Media3D;

namespace Game.Newt.v1.NewtonDynamics1
{
    public class ConvextCollisionModifier : ConvexCollisionMask
    {
        private readonly ConvexCollisionMask _collisionMask;

        public ConvextCollisionModifier(ConvexCollisionMask collisionMask, Matrix3D initialMatrix)
        {
            if (collisionMask == null) throw new ArgumentNullException("collisionMask");

            _collisionMask = collisionMask;
            Initialise(collisionMask.World);
            NewtonCollision.ConvexHullModifierMatrix = initialMatrix;
        }

        public ConvexCollisionMask CollisionMask
        {
            get { return _collisionMask; }
        }

        public Matrix3D ModifierMatrix
        {
            get { return NewtonCollision.ConvexHullModifierMatrix; }
            set { NewtonCollision.ConvexHullModifierMatrix = value; }
        }

        protected override CCollision OnInitialise()
        {
            return _collisionMask.NewtonCollision.CreateConvexHullModifier();
        }
    }
}
