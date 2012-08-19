using System.Collections.Generic;
using System.Windows.Media.Media3D;
using System;

namespace Game.Newt.NewtonDynamics_153
{
    public class TerrianBody3D : Visual3DBodyBase, IMesh
    {
        public TerrianBody3D()
        {
        }

        public TerrianBody3D(World world, ModelVisual3D model)
            : base(world, model)
        {
        }

        public new TerrianCollisionMask3D CollisionMask
        {
            get { return (TerrianCollisionMask3D)base.CollisionMask; }
        }

        protected override CollisionMask OnInitialise(Matrix3D initialMatrix)
        {
            if (Visual.Content != null)
            {
                TerrianCollisionMask3D terrianCollisionMask3D = null;
                CollisionMask collisionMask = _GetCollisionMask(this.Visual);
                if (collisionMask != null)
                {
                    if (!(collisionMask is TerrianCollisionMask3D))
                        throw new InvalidOperationException("Currently another type of CollisionMaskMask is attached to this visual.");
                    else
                        terrianCollisionMask3D = (TerrianCollisionMask3D)collisionMask;
                }
                else
                {
                    terrianCollisionMask3D = new TerrianCollisionMask3D();
                    _SetCollisionMask(this.Visual, terrianCollisionMask3D);
                }

                terrianCollisionMask3D.Initialise(this.World, initialMatrix);
                return terrianCollisionMask3D;
            }
            else
                return null;
        }

        protected override void CalculateMass(float mass)
        {
            // not used.
        }

        #region IMesh Members

        public IList<Point3D> GetPoints()
        {
            if (CollisionMask != null)
                return ((IMesh)CollisionMask).GetPoints();
            else
                return null;
        }

        #endregion

        internal static void _SetCollisionMask(ModelVisual3D visual, CollisionMask value)
        {
            if (visual == null) throw new ArgumentNullException("visual");

            TerrianCollisionMask3D collisionMask = (value as TerrianCollisionMask3D);
            if (collisionMask != null)
            {
                if (!(visual.Content is GeometryModel3D))
                    throw new InvalidOperationException("Current only GeometryModel3D models supported for TerrianCollisionMask3D.");
                collisionMask.Visual = visual;
                visual.SetValue(World.CollisionMaskProperty, value);
            }
        }

        internal static CollisionMask _GetCollisionMask(ModelVisual3D visual)
        {
            if (visual == null) throw new ArgumentNullException("visual");

            return (CollisionMask)visual.GetValue(World.CollisionMaskProperty);
        }
    }
}
