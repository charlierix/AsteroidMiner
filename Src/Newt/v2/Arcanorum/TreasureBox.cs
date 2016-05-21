using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Game.Newt.v2.GameItems;
using Game.HelperClassesWPF;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.Arcanorum
{
    //TODO: Currently, it's just a barrel, make a few other visuals, each type has a different number of HP
    //  Vase
    //  Crate
    //  TreasureBox
    //  Safe
    public class TreasureBox : IMapObject, ITakesDamage, IDisposable
    {
        #region Constructor

        public TreasureBox(Point3D position, double mass, double hitPoints, int materialID, World world, WeaponDamage damageMultipliers, object[] containedTreasureDNA = null)
        {
            const double RADIUS = .75d / 2d;
            const double HEIGHT = 1.5d;

            const double RING_Z1 = (HEIGHT / 2d) * .5d;     // this is distance from the end cap
            const double RING_Z2 = (HEIGHT / 2d) - RING_Z1;     // this one is distance from the origin

            const double RINGRADIUS_INNER = RADIUS * .5d;
            const double RINGRADIUS_OUTER = RADIUS * 1.05d;
            const double RINGHEIGHT = HEIGHT * .05d;

            const int DOMESEGMENTS = 3;
            const int CYLINDERSEGMENTS = 8;
            const int RINGSEGMENTS = 8;

            #region WPF Model

            Model3DGroup geometries = new Model3DGroup();

            #region Barrel

            // Material
            MaterialGroup material = new MaterialGroup();
            material.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("413D34"))));
            material.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("60C5CAA7")), 2d));// .25d));

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = material;
            geometry.BackMaterial = material;

            List<TubeRingBase> rings = new List<TubeRingBase>();
            rings.Add(new TubeRingDome(0d, false, DOMESEGMENTS));
            rings.Add(new TubeRingRegularPolygon(RING_Z1, false, RADIUS, RADIUS, false));
            rings.Add(new TubeRingRegularPolygon(HEIGHT - (RING_Z1 * 2), false, RADIUS, RADIUS, false));
            rings.Add(new TubeRingDome(RING_Z1, false, DOMESEGMENTS));
            geometry.Geometry = UtilityWPF.GetMultiRingedTube(CYLINDERSEGMENTS, rings, true, true);

            geometries.Children.Add(geometry);

            #endregion

            #region Rings

            // Material
            material = new MaterialGroup();
            //material.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("736E56"))));
            //material.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("B0EEF2CE")), .8d));
            material.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("CCBF81"))));
            material.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("B0D9D78D")), 8));// .8d));

            // Ring 1
            // Geometry Model
            geometry = new GeometryModel3D();
            geometry.Material = material;
            geometry.BackMaterial = material;
            geometry.Geometry = UtilityWPF.GetRing(RINGSEGMENTS, RINGRADIUS_INNER, RINGRADIUS_OUTER, RINGHEIGHT, new TranslateTransform3D(0, 0, -RING_Z2), false);

            geometries.Children.Add(geometry);

            // Ring 2
            // Geometry Model
            geometry = new GeometryModel3D();
            geometry.Material = material;
            geometry.BackMaterial = material;
            geometry.Geometry = UtilityWPF.GetRing(RINGSEGMENTS, RINGRADIUS_INNER, RINGRADIUS_OUTER, RINGHEIGHT, new TranslateTransform3D(0, 0, RING_Z2), false);

            geometries.Children.Add(geometry);

            // Ring 3
            // Geometry Model
            geometry = new GeometryModel3D();
            geometry.Material = material;
            geometry.BackMaterial = material;
            geometry.Geometry = UtilityWPF.GetRing(RINGSEGMENTS, RINGRADIUS_INNER, RINGRADIUS_OUTER, RINGHEIGHT);

            geometries.Children.Add(geometry);

            #endregion

            geometries.Transform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90));     // make this go along x instead of z

            this.Model = geometries;

            // Model Visual
            ModelVisual3D visual = new ModelVisual3D();        // this is the expensive one, so as few of these should be made as possible
            visual.Content = this.Model;

            #endregion

            #region Physics Body

            Transform3DGroup transform = new Transform3DGroup();
            //transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 90)));
            transform.Children.Add(new TranslateTransform3D(position.ToVector()));

            using (CollisionHull hull = CollisionHull.CreateCylinder(world, 0, RADIUS, HEIGHT, null))
            {
                this.PhysicsBody = new Body(hull, transform.Value, mass, new Visual3D[] { visual });
                this.PhysicsBody.MaterialGroupID = materialID;
                this.PhysicsBody.LinearDamping = 1d;
                this.PhysicsBody.AngularDamping = new Vector3D(.01d, .01d, .01d);

                //this.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(PhysicsBody_ApplyForceAndTorque);
            }

            #endregion

            this.Radius = Math.Sqrt((RADIUS * RADIUS) + ((HEIGHT / 2d) * (HEIGHT / 2d)));

            this.ReceiveDamageMultipliers = damageMultipliers ?? new WeaponDamage();

            this.HitPoints = new Container() { QuantityMax = hitPoints, QuantityCurrent = hitPoints };

            this.ContainedTreasureDNA = containedTreasureDNA;

            this.CreationTime = DateTime.UtcNow;
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.PhysicsBody.Dispose();
            }
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
        #region ITakesDamage Members

        public Tuple<bool, WeaponDamage> Damage(WeaponDamage damage, Weapon weapon = null)
        {
            return WeaponDamage.DoDamage(this, damage);
        }

        public WeaponDamage ReceiveDamageMultipliers
        {
            get;
            private set;
        }

        public Container HitPoints
        {
            get;
            private set;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// These are the hidden items that this box contains.  This shouldn't hold live IMapObjects, because that would require
        /// physics objects to be created, stored in world
        /// </summary>
        public object[] ContainedTreasureDNA
        {
            get;
            set;
        }

        #endregion
    }
}
