using System;
using Game.Newt.NewtonDynamics_153.Api;
using System.Windows.Media.Media3D;

namespace Game.Newt.NewtonDynamics_153
{
    public abstract class GeometrylBasedCollisionMask : ConvexCollisionMask
    {
        private Geometry3D _geometry;

        public GeometrylBasedCollisionMask()
        {
        }

        public GeometrylBasedCollisionMask(World world, Geometry3D geometry)
        {
            if (geometry == null) throw new ArgumentNullException("geometry");

            _geometry = geometry;
            Initialise(world);
        }

        public Geometry3D Geometry
        {
            get { return _geometry; }
            internal set
            {
                if (this.IsInitialised) throw new InvalidOperationException("Can not set the Geometry property after the CollisionMaskMask has been initialised.");
                _geometry = value;
            }
        }

        protected override CCollision OnInitialise()
        {
            if (_geometry == null) throw new InvalidOperationException("The Geometry has not been initialised yet.");

            return null;
        }
    }
}
