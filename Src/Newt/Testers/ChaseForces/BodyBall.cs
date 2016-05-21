using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.HelperClassesWPF.Primitives3D;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.Testers.ChaseForces
{
    public class BodyBall : IMapObject
    {
        #region Constructor

        public BodyBall(World world)
        {
            this.Model = GetModel();
            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = this.Model;

            using (CollisionHull hull = CollisionHull.CreateNull(world))
            {
                this.PhysicsBody = new Body(hull, Matrix3D.Identity, 100, new[] { visual });
            }

            this.Radius = 2;

            this.CreationTime = DateTime.UtcNow;
        }

        #endregion

        #region IMapObject Members

        public long Token
        {
            get
            {
                return this.PhysicsBody.Token;
            }
        }

        public bool IsDisposed
        {
            get
            {
                return this.PhysicsBody.IsDisposed;
            }
        }

        public Body PhysicsBody
        {
            get;
            private set;
        }

        public Visual3D[] Visuals3D
        {
            get
            {
                return this.PhysicsBody.Visuals;
            }
        }
        public Model3D Model
        {
            get;
            private set;
        }

        public Point3D PositionWorld
        {
            get
            {
                return this.PhysicsBody.Position;
            }
        }
        public Vector3D VelocityWorld
        {
            get
            {
                return this.PhysicsBody.Velocity;
            }
        }
        public Matrix3D OffsetMatrix
        {
            get
            {
                return this.PhysicsBody.OffsetMatrix;
            }
        }

        public double Radius
        {
            get;
            private set;
        }

        public DateTime CreationTime
        {
            get;
            private set;
        }

        public int CompareTo(IMapObject other)
        {
            return MapObjectUtil.CompareToT(this, other);
        }

        public bool Equals(IMapObject other)
        {
            return MapObjectUtil.EqualsT(this, other);
        }
        public override bool Equals(object obj)
        {
            return MapObjectUtil.EqualsObj(this, obj);
        }

        public override int GetHashCode()
        {
            return MapObjectUtil.GetHashCode(this);
        }

        #endregion

        #region Public Properties

        public double Mass
        {
            get
            {
                return this.PhysicsBody.Mass;
            }
            set
            {
                this.PhysicsBody.Mass = value;
            }
        }

        #endregion

        #region Private Methods

        private Model3D GetModel()
        {
            const double SIZE = 4;

            Model3DGroup retVal = new Model3DGroup();
            GeometryModel3D geometry;
            MaterialGroup material;

            var rhomb = UtilityWPF.GetRhombicuboctahedron(SIZE, SIZE, SIZE);
            TriangleIndexed[] triangles;

            #region X,Y,Z spikes

            double thickness = .2;
            double length = SIZE * 1.5;

            retVal.Children.Add(new BillboardLine3D()
            {
                Color = UtilityWPF.ColorFromHex("80" + ChaseColors.X),
                IsReflectiveColor = false,
                Thickness = thickness,
                FromPoint = new Point3D(0, 0, 0),
                ToPoint = new Point3D(length, 0, 0)
            }.Model);

            retVal.Children.Add(new BillboardLine3D()
            {
                Color = UtilityWPF.ColorFromHex("80" + ChaseColors.Y),
                IsReflectiveColor = false,
                Thickness = thickness,
                FromPoint = new Point3D(0, 0, 0),
                ToPoint = new Point3D(0, length, 0)
            }.Model);

            retVal.Children.Add(new BillboardLine3D()
            {
                Color = UtilityWPF.ColorFromHex("80" + ChaseColors.Z),
                IsReflectiveColor = false,
                Thickness = thickness,
                FromPoint = new Point3D(0, 0, 0),
                ToPoint = new Point3D(0, 0, length)
            }.Model);

            #endregion

            #region X plates

            geometry = new GeometryModel3D();

            material = new MaterialGroup();
            material.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("80" + ChaseColors.X))));
            material.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("70DCE01D")), 5));

            geometry.Material = material;
            geometry.BackMaterial = material;

            triangles = rhomb.Squares_Orth.
                SelectMany(o => o).
                Where(o => o.IndexArray.All(p => Math1D.IsNearValue(Math.Abs(o.AllPoints[p].X * 2), SIZE))).
                ToArray();

            geometry.Geometry = UtilityWPF.GetMeshFromTriangles_IndependentFaces(triangles);

            retVal.Children.Add(geometry);

            #endregion
            #region Y plates

            geometry = new GeometryModel3D();

            material = new MaterialGroup();
            material.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("80" + ChaseColors.Y))));
            material.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("702892BF")), 3));

            geometry.Material = material;
            geometry.BackMaterial = material;

            triangles = rhomb.Squares_Orth.
                SelectMany(o => o).
                Where(o => o.IndexArray.All(p => Math1D.IsNearValue(Math.Abs(o.AllPoints[p].Y * 2), SIZE))).
                ToArray();

            geometry.Geometry = UtilityWPF.GetMeshFromTriangles_IndependentFaces(triangles);

            retVal.Children.Add(geometry);

            #endregion
            #region Z plates

            geometry = new GeometryModel3D();

            material = new MaterialGroup();
            material.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("80" + ChaseColors.Z))));
            material.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("B0CF1919")), 17));

            geometry.Material = material;
            geometry.BackMaterial = material;

            triangles = rhomb.Squares_Orth.
                SelectMany(o => o).
                Where(o => o.IndexArray.All(p => Math1D.IsNearValue(Math.Abs(o.AllPoints[p].Z * 2), SIZE))).
                ToArray();

            geometry.Geometry = UtilityWPF.GetMeshFromTriangles_IndependentFaces(triangles);

            retVal.Children.Add(geometry);

            #endregion
            #region Base

            geometry = new GeometryModel3D();

            material = new MaterialGroup();
            material.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("305B687A"))));
            material.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("507E827A")), 12));

            geometry.Material = material;
            geometry.BackMaterial = material;

            triangles = UtilityCore.Iterate(
                rhomb.Squares_Diag.SelectMany(o => o),
                rhomb.Triangles
                ).ToArray();

            geometry.Geometry = UtilityWPF.GetMeshFromTriangles_IndependentFaces(triangles);

            retVal.Children.Add(geometry);

            #endregion

            return retVal;
        }

        #endregion
    }
}
