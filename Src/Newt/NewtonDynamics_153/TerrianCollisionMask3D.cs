using System;
using System.Collections.Generic;
using Game.Newt.NewtonDynamics_153.Api;
using System.Windows.Media.Media3D;

using Game.Newt.HelperClasses;

namespace Game.Newt.NewtonDynamics_153
{
    public class TerrianCollisionMask3D : CollisionTree
    {
        private ModelVisual3D _visual;

        public TerrianCollisionMask3D()
        {
        }
        public TerrianCollisionMask3D(World world, ModelVisual3D visual)
        {
            if (visual == null) throw new ArgumentNullException("visual");

            _visual = visual;
            Initialise(world);
        }

        public ModelVisual3D Visual
        {
            get { return _visual; }
            internal set
            {
                if (this.IsInitialised) throw new InvalidOperationException("Can not set the Geometry property after the CollisionMaskMask has been initialised.");
                _visual = value;
            }
        }

        public new IList<Point3D> Points
        {
            get { return base.Points; }
        }

        protected override CCollision OnInitialise()
        {
            if (_visual == null) throw new InvalidOperationException("The Geometry has not been initialised yet.");

            return base.OnInitialise();
        }

        protected override void UpdateFaces()
        {
            int addedCount = AddModelFaces(InitialMatrix, _visual.Content);
            if (addedCount == 0) throw new InvalidOperationException("No Geometry found for TerrianCollisionMask.");
        }

        private int AddModelFaces(Matrix3D parentMatrix, Model3D model)
        {
            if (model.Transform != null)
                parentMatrix = model.Transform.Value * parentMatrix;

            int result = 0;
            Model3DGroup models = (model as Model3DGroup);
            if (models != null)
            {
                // This is a group.  Recurse through the children
                foreach (Model3D m in models.Children)
                {
                    result += AddModelFaces(parentMatrix, m);
                }
            }
            else
            {
                if (!(model is GeometryModel3D))
                    throw new InvalidOperationException("Current only GeometryModel3D models supported for TerrianCollisionMask3D.");

                Geometry3D geometry = ((GeometryModel3D)model).Geometry;

                IList<Point3D> meshPoints = GeometryHelper.GetGeometryPoints((MeshGeometry3D)geometry, parentMatrix);
                if (meshPoints != null)
                {
                    AddFaces(meshPoints, 3, false);
                    result = meshPoints.Count;
                }
            }

            return result;
        }
    }
}
