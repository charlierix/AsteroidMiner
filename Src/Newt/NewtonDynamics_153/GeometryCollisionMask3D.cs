using System;
using System.Collections.Generic;
using Game.Newt.NewtonDynamics_153.Api;
using System.Windows.Media.Media3D;

namespace Game.Newt.NewtonDynamics_153
{
    public class GeometryCollisionMask3D : GeometrylBasedCollisionMask,
        IMesh
    {
        private IList<Point3D> _points;

        public GeometryCollisionMask3D()
        {
        }

        public GeometryCollisionMask3D(World world, Geometry3D geometry)
            : base(world, geometry)
        {
        }

        protected override CCollision OnInitialise()
        {
            base.OnInitialise();

            if (!(this.Geometry is MeshGeometry3D))
                throw new InvalidOperationException("Only MeshGeometry3D supported.");

            _points = GeometryHelper.GetGeometryPoints((MeshGeometry3D)this.Geometry, Matrix3D.Identity);
            if (_points == null)
                throw new InvalidOperationException("The MeshGeometry3D contains no geometry.");

            CCollisionConvexPrimitives collision = new CCollisionConvexPrimitives(this.World.NewtonWorld);
            collision.CreateConvexHull(_points, Matrix3D.Identity);

            return collision;
        }

        public IList<Point3D> GetPoints()
        {
            return _points;
        }
    }
}
