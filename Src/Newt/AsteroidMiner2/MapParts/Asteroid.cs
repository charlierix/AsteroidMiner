using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using Game.HelperClasses;
using Game.Newt.HelperClasses;
using Game.Newt.NewtonDynamics;

namespace Game.Newt.AsteroidMiner2.MapParts
{
    //TODO: There seems to sometimes be one triangle with the wrong normal (or it's defined backward).  Do a scan
    // of the completed mesh's normals to see if there is a bug

    public class Asteroid : IMapObject
    {
        #region Constructor

        public Asteroid(double radius, double mass, Point3D position, World world, int materialID)
            : this(radius, mass, position, world, materialID, GetHullTriangles(radius)) { }

        public Asteroid(double radius, double mass, Point3D position, World world, int materialID, TriangleIndexed[] triangles)
        {
            this.Radius = radius;

            #region WPF Model

            // Material
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(WorldColors.AsteroidColor)));
            materials.Children.Add(WorldColors.AsteroidSpecular);

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;
            //geometry.Geometry = UtilityWPF.GetSphere(5, radius);
            geometry.Geometry = UtilityWPF.GetMeshFromTriangles(triangles);

            this.Model = geometry;

            // Model Visual
            ModelVisual3D model = new ModelVisual3D();
            model.Content = geometry;

            #endregion

            #region Physics Body

            Transform3DGroup transform = new Transform3DGroup();
            transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(Math3D.GetRandomRotation())));
            transform.Children.Add(new TranslateTransform3D(position.ToVector()));

            using (CollisionHull hull = CollisionHull.CreateConvexHull(world, 0, triangles[0].AllPoints, 0.002d, null))
            {
                this.PhysicsBody = new Body(hull, transform.Value, mass, new Visual3D[] { model });
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

        #region Public Methods

        /// <summary>
        /// This is exposed so it can be called externally on a separate thread, then call this asteroid's constructor
        /// in the main thread once this returns
        /// </summary>
        public static TriangleIndexed[] GetHullTriangles(double radius)
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
                        points[cntr] = Math3D.GetRandomVectorSpherical(minRadius, radius).ToPoint();
                    }

                    // Squash it
                    Transform3DGroup transform = new Transform3DGroup();
                    transform.Children.Add(new ScaleTransform3D(.33d + (rand.NextDouble() * .66d), .33d + (rand.NextDouble() * .66d), 1d));		// squash it
                    transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(Math3D.GetRandomRotation())));		// giving it a random rotation, since it's always squashed along the same axiis
                    transform.Transform(points);

                    // Get a hull that wraps those points
                    TriangleIndexed[] retVal = Math3D.GetConvexHull(points.ToArray());

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
    }
}
