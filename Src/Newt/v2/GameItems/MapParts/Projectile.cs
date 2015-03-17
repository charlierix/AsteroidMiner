using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.GameItems.MapParts
{
    public class Projectile : IMapObject, IPartUpdatable
    {
        #region Declaration Section

        private readonly Map _map;

        private readonly double? _maxAge;
        private double _age = 0;

        #endregion

        #region Constructor

        public Projectile(double radius, double mass, Point3D position, World world, int materialID, Color? color = null, double? maxAge = null, Map map = null)
        {
            if (maxAge != null && map == null)
            {
                throw new ArgumentException("Map must be populated if max age is set");
            }

            this.Radius = radius;
            _maxAge = maxAge;
            _map = map;

            #region WPF Model

            this.Model = GetModel(color);
            this.Model.Transform = new ScaleTransform3D(radius, radius, radius);

            // Model Visual
            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = this.Model;

            #endregion

            #region Physics Body

            Transform3DGroup transform = new Transform3DGroup();
            transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(Math3D.GetRandomRotation())));
            transform.Children.Add(new TranslateTransform3D(position.ToVector()));

            using (CollisionHull hull = CollisionHull.CreateSphere(world, 0, new Vector3D(radius, radius, radius), null))
            {
                this.PhysicsBody = new Body(hull, transform.Value, mass, new Visual3D[] { visual });
                //this.PhysicsBody.IsContinuousCollision = true;
                this.PhysicsBody.MaterialGroupID = materialID;
                this.PhysicsBody.LinearDamping = .01d;
                this.PhysicsBody.AngularDamping = new Vector3D(.01d, .01d, .01d);

                //this.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(Body_ApplyForceAndTorque);
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
        #region IPartUpdatable Members

        public void Update_MainThread(double elapsedTime)
        {
            if(_maxAge == null)
            {
                return;
            }

            _age += elapsedTime;

            if(_age > _maxAge.Value)
            {
                _map.RemoveItem(this);      //NOTE: it is expected that there is a listener that will dispose this projectile
            }
        }

        public void Update_AnyThread(double elapsedTime)
        {
            // nothing to do
        }

        public int? IntervalSkips_MainThread
        {
            get
            {
                return 3;
            }
        }
        public int? IntervalSkips_AnyThread
        {
            get
            {
                return null;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// This is exposed so it can be called externally on a separate thread, then call this asteroid's constructor
        /// in the main thread once this returns
        /// </summary>
        public static ITriangleIndexed[] GetHullTriangles(double radius)
        {
            const int NUMPOINTS = 60;		// too many, and it looks too perfect

            Exception lastException = null;
            Random rand = StaticRandom.GetRandomForThread();

            for (int infiniteLoopCntr = 0; infiniteLoopCntr < 50; infiniteLoopCntr++)		// there is a slight chance that the convex hull generator will choke on the inputs.  If so just retry
            {
                try
                {
                    double minRadius = radius * .9d;

                    Point3D[] points = new Point3D[NUMPOINTS];

                    // Make a point cloud
                    for (int cntr = 0; cntr < NUMPOINTS; cntr++)
                    {
                        points[cntr] = Math3D.GetRandomVector_Spherical(minRadius, radius).ToPoint();
                    }

                    // Squash it
                    Transform3DGroup transform = new Transform3DGroup();
                    transform.Children.Add(new ScaleTransform3D(.33d + (rand.NextDouble() * .66d), .33d + (rand.NextDouble() * .66d), 1d));		// squash it
                    transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(Math3D.GetRandomRotation())));		// giving it a random rotation, since it's always squashed along the same axiis
                    transform.Transform(points);

                    // Get a hull that wraps those points
                    ITriangleIndexed[] retVal = Math3D.GetConvexHull(points.ToArray());

                    // Get rid of unused points
                    retVal = TriangleIndexed.Clone_CondensePoints(retVal);

                    // Exit Function
                    return retVal;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }
            }

            throw new ApplicationException(lastException.ToString());
        }

        #endregion

        #region Private Methods

        //TODO: Make this a ThreadLocal
        private Model3D GetModel(Color? color = null)
        {
            var ball = UtilityWPF.GetTruncatedIcosidodecahedron(1, null);

            Model3DGroup retVal = new Model3DGroup();

            // Black
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("141414"))));
            materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("08FF0000")), 20));

            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;

            geometry.Geometry = UtilityWPF.GetMeshFromTriangles(UtilityCore.Iterate(ball.Hexagons, ball.Squares).SelectMany(o => o));

            retVal.Children.Add(geometry);

            // White
            materials = new MaterialGroup();
            if (color == null)
            {
                materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("F8F8F8"))));
                //materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("222"))));
                //materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(Colors.Chartreuse)));
            }
            else
            {
                materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color.Value)));
            }
            materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("A0FF0000")), 30));

            geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;

            geometry.Geometry = UtilityWPF.GetMeshFromTriangles_IndependentFaces(ball.Decagons.SelectMany(o => o));

            retVal.Children.Add(geometry);

            return retVal;
        }

        #endregion
    }
}
