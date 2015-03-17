using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Game.HelperClassesWPF;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.GenePool.MapParts
{
    /// <summary>
    /// This is used to represent a newborn ship.  If you need more properties, than this base class provides, create a derived class
    /// </summary>
    public class Egg : IMapObject
    {
        #region Constructor

        public Egg(Point3D position, World world, int materialID, ItemOptions itemOptions, ShipDNA dna)
        {
            // The radius should be 20% the size of the adult ship
            this.Radius = dna.PartsByLayer.SelectMany(o => o.Value).
                Max(o => o.Position.ToVector().Length + Math3D.Max(o.Scale.X, o.Scale.Y, o.Scale.Z))
                * .2d;

            Vector3D scale = new Vector3D(.75d, .75d, 1d);

            #region WPF Model

            // Material
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(WorldColors.EggColor)));
            materials.Children.Add(WorldColors.EggSpecular);

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;
            geometry.Geometry = UtilityWPF.GetSphere_LatLon(5, this.Radius);
            geometry.Transform = new ScaleTransform3D(scale);

            this.Model = geometry;

            // Model Visual
            ModelVisual3D model = new ModelVisual3D();
            model.Content = geometry;

            #endregion

            #region Physics Body

            Transform3DGroup transform = new Transform3DGroup();
            transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(Math3D.GetRandomRotation())));
            transform.Children.Add(new TranslateTransform3D(position.ToVector()));

            double volume = (4d / 3d) * Math.PI * scale.X * this.Radius * scale.Y * this.Radius * scale.Z * this.Radius;
            double mass = volume * itemOptions.EggDensity;

            using (CollisionHull hull = CollisionHull.CreateSphere(world, 0, scale * this.Radius, null))
            {
                this.PhysicsBody = new Body(hull, transform.Value, mass, new Visual3D[] { model });
                this.PhysicsBody.MaterialGroupID = materialID;
                this.PhysicsBody.LinearDamping = .01f;
                this.PhysicsBody.AngularDamping = new Vector3D(.001f, .001f, .001f);
            }

            #endregion

            this.CreationTime = DateTime.Now;
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

        public ShipDNA DNA
        {
            get;
            private set;
        }

        #endregion
    }
}
