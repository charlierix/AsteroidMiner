using System.Collections.Generic;
using Game.Newt.NewtonDynamics_153.Api;
using System.Windows.Media.Media3D;
using System;
using System.Windows;

using Game.Newt.HelperClasses;

namespace Game.Newt.NewtonDynamics_153
{
    public class VisualNullBody3D : Visual3DBodyBase,
        INullBody
    {
        public VisualNullBody3D()
        {
        }

        public VisualNullBody3D(World world, ModelVisual3D model)
            : base(world, model)
        {
        }

        protected override CollisionMask OnInitialise(Matrix3D initialMatrix)
        {
            return new NullCollision(this.World);
        }

        protected override void CalculateMass(float mass)
        {
            // not implemented
        }
    }
}
