using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;

using Game.HelperClassesWPF;
using Game.Newt.v2.NewtonDynamics.Import;

namespace Game.Newt.v2.NewtonDynamics
{
    #region class: CollisionHull

    /// <summary>
    /// NOTE:  This is not a collision between two objects, it defines an object that can be collided
    /// NOTE:  A collision hull object can be reused by multiple bodies, which is a good way to optimize
    /// </summary>
    /// <remarks>
    /// Newton just calls this Collision, but I use the term CollisionHull
    /// 
    /// A good page about collision primitives:
    /// http://newtondynamics.com/wiki/index.php5?title=Collision_primitives
    /// 
    /// From the man himself about shapeID:
    /// shape id is for user defined material system or for any other cosification the user want to do. 
    /// the id is part of the caching system.
    /// 
    /// say you have tow boxes of the same size, but you wna the to be different collisions, the caching system will see retrun eth same pointer, 
    /// but if the have different ID then they are two separate instance.
    /// 
    /// see the wiki 101 tutorials, i implemented a unified Material system there.
    /// wit teh shape ID is possible to implement a unified material systems that play nice face IDs in collsion gtress and comppund ids in convex shapes, 
    /// all handled in one single callback. very good for editors and data driven engines.
    /// 
    /// setting then to a unique just because you can is a mistake because it will make one instance for each shape, and shapes are not lightweight objects.
    /// </remarks>
    public class CollisionHull : IDisposable
    {
        #region struct: IntersectionPoint

        public struct IntersectionPoint
        {
            public Point3D ContactPoint;
            public Vector3D Normal;
            public double PenetrationDistance;
            public double TimeOfImpact;
        }

        #endregion

        #region Declaration Section

        /// <summary>
        /// Null if there is no offset (this is converted from a matrix) - see the static create methods
        /// </summary>
        private float[] _offsetMatrix = null;

        #endregion

        #region Constructor

        //NOTE:  I'm keeping the number of constructor overloads small, and instead making the user call one of the static creation methods.  Otherwise it would be very
        //confusing to know what you're creating

        protected CollisionHull(IntPtr handle, WorldBase world, float[] offsetMatrix, CollisionShapeType collisionShape)
        {
            _handle = handle;
            _world = world;
            _offsetMatrix = offsetMatrix;
            _collisionShape = collisionShape;

            ObjectStorage.Instance.AddCollisionHull(_handle, this);
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
            if (disposing)// && _handle != IntPtr.Zero)
            {
                // Newton optimizes how many hulls are created, so if two bodies share the same shaped hull, newton only has one, even though .net doesn't know that.
                // So any given hull's dispose shouldn't remove from the object store.  Instead, it listen's to the world's hull removed event.
                //ObjectStorage.Instance.RemoveCollisionHull(_handle);

                // This may or may not physically remove the hull.  This just tells newton to decrement its reference count.  The important thing from the perspective
                // of this .net class is to match the number of calls to Newton.NewtonXXX with Newton.NewtonReleaseCollision.
                Newton.NewtonReleaseCollision(_world.Handle, _handle);

                //_handle = IntPtr.Zero;		// I don't like setting this to zero
            }
        }

        #endregion

        #region Public Properties

        private IntPtr _handle;
        public IntPtr Handle
        {
            get
            {
                return _handle;
            }
        }

        private WorldBase _world = null;
        public WorldBase World
        {
            get
            {
                return _world;
            }
        }

        private CollisionShapeType _collisionShape;
        public CollisionShapeType CollisionShape
        {
            get
            {
                return _collisionShape;
            }
        }

        /// <summary>
        /// Trigger volumes don't do physics, they just give you something to get events for
        /// </summary>
        public bool IsTriggerVolume
        {
            get
            {
                int retVal = Newton.NewtonCollisionIsTriggerVolume(_handle);
                return retVal == 1 ? true : false;
            }
            set
            {
                Newton.NewtonCollisionSetAsTriggerVolume(_handle, value ? 1 : 0);
            }
        }

        public double MaxBreakImpactImpulse
        {
            get
            {
                return Newton.NewtonCollisionGetMaxBreakImpactImpulse(_handle);
            }
            set
            {
                Newton.NewtonCollisionSetMaxBreakImpactImpulse(_handle, Convert.ToSingle(value));
            }
        }

        // Is this the same as the shapeID that was passed in the constructor?
        public uint UserID
        {
            get
            {
                return Newton.NewtonCollisionGetUserID(_handle);
            }
            set
            {
                Newton.NewtonCollisionSetUserID(_handle, value);
            }
        }

        #endregion

        #region Public Methods

        // These methods are used to create a collision (see class remarks for a description of the shapeID)
        //NOTE:  ShapeID means nothing to newton, it's just a way for the consumer to give special tokens to things (safe to pass zero for everything)
        //NOTE:  Elsewhere, the shapeID is considered a uint.  So to be safe, stay positive
        public static CollisionHull CreateNull(WorldBase world)
        {
            IntPtr handle = Newton.NewtonCreateNull(world.Handle);
            return new CollisionHull(handle, world, null, CollisionShapeType.Null);
        }
        public static CollisionHull CreateSphere(WorldBase world, int shapeID, Vector3D radius, Matrix3D? offsetMatrix)
        {
            float[] newtOffsetMatrix = null;		// null means no offset
            if (offsetMatrix != null)
            {
                newtOffsetMatrix = new NewtonMatrix(offsetMatrix.Value).Matrix;
            }

            IntPtr handle = Newton.NewtonCreateSphere(world.Handle, Convert.ToSingle(radius.X), Convert.ToSingle(radius.Y), Convert.ToSingle(radius.Z), shapeID, newtOffsetMatrix);

            return new CollisionHull(handle, world, newtOffsetMatrix, CollisionShapeType.Sphere);
        }
        public static CollisionHull CreateBox(WorldBase world, int shapeID, Vector3D size, Matrix3D? offsetMatrix)
        {
            float[] newtOffsetMatrix = null;		// null means no offset
            if (offsetMatrix != null)
            {
                newtOffsetMatrix = new NewtonMatrix(offsetMatrix.Value).Matrix;
            }

            IntPtr handle = Newton.NewtonCreateBox(world.Handle, Convert.ToSingle(size.X), Convert.ToSingle(size.Y), Convert.ToSingle(size.Z), shapeID, newtOffsetMatrix);

            return new CollisionHull(handle, world, newtOffsetMatrix, CollisionShapeType.Box);
        }
        public static CollisionHull CreateCone(WorldBase world, int shapeID, double radius, double height, Matrix3D? offsetMatrix)
        {
            //NOTE:  Height must be >= diameter

            float[] newtOffsetMatrix = null;		// null means no offset
            if (offsetMatrix != null)
            {
                newtOffsetMatrix = new NewtonMatrix(offsetMatrix.Value).Matrix;
            }

            IntPtr handle = Newton.NewtonCreateCone(world.Handle, Convert.ToSingle(radius), Convert.ToSingle(height), shapeID, newtOffsetMatrix);

            return new CollisionHull(handle, world, newtOffsetMatrix, CollisionShapeType.Cone);
        }
        public static CollisionHull CreateCapsule(WorldBase world, int shapeID, double radius, double height, Matrix3D? offsetMatrix)
        {
            //NOTE:  Height must be >= diameter.  If you want less, use ChamferCylinder

            float[] newtOffsetMatrix = null;		// null means no offset
            if (offsetMatrix != null)
            {
                newtOffsetMatrix = new NewtonMatrix(offsetMatrix.Value).Matrix;
            }

            IntPtr handle = Newton.NewtonCreateCapsule(world.Handle, Convert.ToSingle(radius), Convert.ToSingle(height), shapeID, newtOffsetMatrix);

            return new CollisionHull(handle, world, newtOffsetMatrix, CollisionShapeType.Capsule);
        }
        public static CollisionHull CreateCylinder(WorldBase world, int shapeID, double radius, double height, Matrix3D? offsetMatrix)
        {
            float[] newtOffsetMatrix = null;		// null means no offset
            if (offsetMatrix != null)
            {
                newtOffsetMatrix = new NewtonMatrix(offsetMatrix.Value).Matrix;
            }

            IntPtr handle = Newton.NewtonCreateCylinder(world.Handle, Convert.ToSingle(radius), Convert.ToSingle(height), shapeID, newtOffsetMatrix);

            return new CollisionHull(handle, world, newtOffsetMatrix, CollisionShapeType.Cylinder);
        }
        public static CollisionHull CreateChamferCylinder(WorldBase world, int shapeID, double radius, double height, Matrix3D? offsetMatrix)
        {
            //TODO: Figure out what is wrong when this is tall and skinny

            float[] newtOffsetMatrix = null;		// null means no offset
            if (offsetMatrix != null)
            {
                newtOffsetMatrix = new NewtonMatrix(offsetMatrix.Value).Matrix;
            }

            IntPtr handle = Newton.NewtonCreateChamferCylinder(world.Handle, Convert.ToSingle(radius), Convert.ToSingle(height), shapeID, newtOffsetMatrix);

            return new CollisionHull(handle, world, newtOffsetMatrix, CollisionShapeType.ChamferCylinder);
        }
        public static CollisionHull CreateConvexHull(WorldBase world, int shapeID, IEnumerable<Point3D> verticies, Matrix3D? offsetMatrix = null, double tolerance = .002d)
        {
            //NOTE:  If the mesh passed in is concave, newton will make it convex.  If you require concave, build up a complex collision out of convex primitives
            //NOTE:  Must have at least 4 verticies

            #region Tolerance Comments

            // Got this from here:
            //http://newtondynamics.com/wiki/index.php5?title=NewtonCreateConvexHull

            // dFloat tolerance - the vertex optimization tolerance. A higher number means the hull can be simplified where there are multiple vertices with distance lower than
            // the tolerance; this is useful when generating simpler convex hulls from highly detailed meshes.


            // It's new in Newton 2 and can speed up the performance by reducing the count of vertices of a convex hull.
            // Here is the description by Julio Jerez (author of Newton):

            // The convex hull tolerance does not always apply and it is hard to predict, say for example you have a box, then the parameter will do nothing because adjacent plane
            // bend 90 degree.
            //
            // Say some body have a cube with bevel edges, the at the corner you will have many points that are very close to each other this result on a convex hull that is very dense
            // on the edges and the vertex.
            //
            // In that case the tolerance parameter does make a difference. What is does is that after the Hull is made, for each vertex, an average plane is created by fanning of all the
            // vertices that can be reach from a direct edge going out of that vertex.
            //
            // If the distance from each vertex to the average plane is very small, 
            // And the distance from the center vertex to the plane is smaller than the tolerance, 
            // Then the center vertex can be extracted from the hull and the hull can be reconstructed, 
            // The resulting shape will not be too different from the ideal one. 
            //
            // It continues doing that until not more vertex can be subtracted from the original shape.
            //
            // Basically what is does is that is remove lots of vertices that are too close and make the shep to dense for collision.
            //
            // A tolerance of 0.002 is good candidate especially when you have large cloud of vertices because it will eliminate points that are a 2 millimeter of less from the ideal hull,
            // It leads to a great speed up in collision time.
            //
            // (additional note: this value depends to the kind of application and their dimensions)
            // Since Newton 2.25 the tolerance depend of the diagonal of the cloud point (or bounding box). Ex: A prism with large and small dimension (580 x 0.5 x 580) will have
            // a large diagonal (820). If you set tolerance at 0.001 then the function will eliminate point that are less close of 820*0.001=0.8 units. So your prism become a plane and
            // the function return NULL. To resolve this error just set a lower tolerance. This change was made to get same shape for hull with different scale.

            #endregion

            // Offset Matrix
            float[] newtOffsetMatrix = null;		// null means no offset
            if (offsetMatrix != null)
            {
                newtOffsetMatrix = new NewtonMatrix(offsetMatrix.Value).Matrix;
            }

            int vertexCount = verticies.Count();

            // Verticies
            float[,] vertexArray = new float[vertexCount, 3];

            int i = 0;
            foreach (Point3D vertex in verticies)
            {
                vertexArray[i, 0] = (float)vertex.X;
                vertexArray[i, 1] = (float)vertex.Y;
                vertexArray[i, 2] = (float)vertex.Z;
                i++;
            }

            // Create in newton
            IntPtr handle = Newton.NewtonCreateConvexHull(world.Handle, vertexCount, vertexArray, sizeof(float) * 3, Convert.ToSingle(tolerance), shapeID, newtOffsetMatrix);

            // Exit Function
            return new CollisionHull(handle, world, newtOffsetMatrix, CollisionShapeType.ConvexHull);
        }
        public static CollisionHull CreateConvexHullFromMesh()
        {
            // Not sure how the mesh is stored
            throw new ApplicationException("finish this");

            //internal static extern IntPtr NewtonCreateConvexHullFromMesh(IntPtr newtonWorld, IntPtr mesh, float tolerance, int shapeID);
        }
        public static CollisionHull CreateHeightFieldCollision(WorldBase world, int shapeID, short[,] heights, byte[,] materialIDs, bool gridsDiagonals_TopleftToBottomright, double horizontalScale, double verticalScale)
        {
            //NOTE:  Height field doesn't look at the body's mass, it is static (terrain).  Other stuff bounces off of this

            // I believe materialIDs is the material ID for each grid point, so the terrain can have different elasticities/friction

            //TODO:  Make a helper method that builds a height field out of a bitmap (or 2 bitmaps, one for the heights, one for the materials)

            // Turn the 2D array into a 1D array
            int width = heights.GetUpperBound(0);
            int height = heights.GetUpperBound(1);
            if (materialIDs.GetUpperBound(0) != width || materialIDs.GetUpperBound(1) != height)
            {
                throw new ArgumentException(string.Format("The height array is a different size than the materialID array (height={0}x{1}, material={2}x{3}", width.ToString(), height.ToString(), materialIDs.GetUpperBound(0).ToString(), materialIDs.GetUpperBound(1).ToString()));
            }

            short[] heights1D = new short[width * height];
            byte[] materials1D = new byte[heights1D.Length];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    heights1D[(width * x) + y] = heights[x, y];
                    materials1D[(width * x) + y] = materialIDs[x, y];
                }
            }

            // Convert bool to int
            int gridsDiagonals = gridsDiagonals_TopleftToBottomright ? 1 : 0;		// false is bottom left to top right

            // Create in newton
            IntPtr handle = Newton.NewtonCreateHeightFieldCollision(world.Handle, width, height, gridsDiagonals, heights1D, materials1D, Convert.ToSingle(horizontalScale), Convert.ToSingle(verticalScale), shapeID);

            // Exit Function
            return new CollisionHull(handle, world, null, CollisionShapeType.HeightField);
        }
        public static CollisionHull CreateCompoundCollision(WorldBase world, int shapeID, CollisionHull[] hulls)
        {
            IntPtr[] handles = new IntPtr[hulls.Length];
            for (int cntr = 0; cntr < hulls.Length; cntr++)
            {
                handles[cntr] = hulls[cntr].Handle;
            }

            IntPtr handle = Newton.NewtonCreateCompoundCollision(world.Handle, hulls.Length, handles, shapeID);

            return new CollisionHull(handle, world, null, CollisionShapeType.Compound);
        }

        /// <summary>
        /// This method was added for body to call.  Compound hulls seem to get cloned when added to a body
        /// </summary>
        internal CollisionHull Clone(IntPtr handle)
        {
            return new CollisionHull(handle, _world, _offsetMatrix, _collisionShape);
        }

        public void MakeUnique()
        {
            //NOTE:  If you're going to call this, do it immediatly after creation

            Newton.NewtonCollisionMakeUnique(_world.Handle, _handle);
        }

        public double CalculateVolume()
        {
            return Newton.NewtonConvexCollisionCalculateVolume(_handle);
        }

        public InertialMatrix CalculateInertialMatrix()
        {
            //NOTE:  The generated inertia values should be multiplied by the object mass before calling NewtonBodySetMassMatrix

            NewtonVector3 inertia = new NewtonVector3();
            NewtonVector3 origin = new NewtonVector3();

            Newton.NewtonConvexCollisionCalculateInertialMatrix(_handle, inertia.Vector, origin.Vector);

            return new InertialMatrix(inertia.ToVectorWPF(), origin.ToVectorWPF());
        }

        /// <summary>
        /// Pass in a point that lies outside this collision hull, and a point along the hull is returned (that is closest to the point passed in)
        /// NOTE:  Point must be in local coords (relative to this collision hull)
        /// </summary>
        /// <param name="contactPoint">The closest point along the surface of the collision hull to the point passed in</param>
        /// <param name="normal">The normal of the face that the contact point is on</param>
        /// <param name="point">The point to search (sits outside the collision hull)</param>
        /// <returns>
        /// True:  A point was found
        /// False:  The search point was inside the collision hull, so nulls were returned
        /// </returns>
        public bool GetNearestPoint_PointToHull(out Point3D? contactPoint, Vector3D? normal, Vector3D point, int threadIndex)
        {
            return GetNearestPoint_PointToHull(out contactPoint, normal, point, threadIndex, null);
        }
        public bool GetNearestPoint_PointToHull(out Point3D? contactPoint, Vector3D? normal, Vector3D point, int threadIndex, Transform3D transform)
        {
            float[] finalOffset = transform == null ? new NewtonMatrix(Matrix3D.Identity).Matrix : new NewtonMatrix(transform.Value).Matrix;		// not including _offsetMatrix, because newton is already accounting for it.

            NewtonVector3 contactNewt = new NewtonVector3();
            NewtonVector3 normalNewt = new NewtonVector3();

            int retVal = Newton.NewtonCollisionPointDistance(_world.Handle, new NewtonVector3(point).Vector, _handle, finalOffset, contactNewt.Vector, normalNewt.Vector, threadIndex);

            // Exit Function
            if (retVal == 1)
            {
                contactPoint = contactNewt.ToPointWPF();
                normal = normalNewt.ToVectorWPF();
                return true;
            }
            else
            {
                contactPoint = null;
                normal = null;
                return false;
            }
        }

        /// <summary>
        /// Returns the closest points along the surface of the two hulls (null if the hulls intersect each other)
        /// </summary>
        public bool GetNearestPoint_HullToHull(out Point3D? contactPointThis, out Point3D? contactPointOther, out Vector3D? normal, CollisionHull otherHull, int threadIndex)
        {
            return GetNearestPoint_HullToHull(out contactPointThis, out contactPointOther, out normal, otherHull, threadIndex, null, null);
        }
        public bool GetNearestPoint_HullToHull(out Point3D? contactPointThis, out Point3D? contactPointOther, out Vector3D? normal, CollisionHull otherHull, int threadIndex, Transform3D thisTransform, Transform3D otherTransform)
        {
            float[] finalOffsetThis = thisTransform == null ? new NewtonMatrix(Matrix3D.Identity).Matrix : new NewtonMatrix(thisTransform.Value).Matrix;		// not including the member's offset, because newton is already accounting for it.
            float[] finalOffsetOther = thisTransform == null ? new NewtonMatrix(Matrix3D.Identity).Matrix : new NewtonMatrix(otherTransform.Value).Matrix;

            NewtonVector3 contactANewt = new NewtonVector3();
            NewtonVector3 contactBNewt = new NewtonVector3();
            NewtonVector3 normalNewt = new NewtonVector3();

            int retVal = Newton.NewtonCollisionClosestPoint(_world.Handle, _handle, finalOffsetThis, otherHull.Handle, finalOffsetOther, contactANewt.Vector, contactBNewt.Vector, normalNewt.Vector, threadIndex);

            // Exit Function
            if (retVal == 1)
            {
                contactPointThis = contactANewt.ToPointWPF();
                contactPointOther = contactBNewt.ToPointWPF();
                normal = normalNewt.ToVectorWPF();
                return true;
            }
            else
            {
                contactPointThis = null;
                contactPointOther = null;
                normal = null;
                return false;
            }
        }

        /// <summary>
        /// This returns intersection points between the two hulls (won't return anything if the hulls aren't touching)
        /// NOTE:  This overload is a snapshot in time, the other overload does collision calculations within a window of time
        /// </summary>
        /// <param name="maxReturnCount">The maximum number of points to return</param>
        public IntersectionPoint[] GetIntersectingPoints_HullToHull(int maxReturnCount, CollisionHull otherHull, int threadIndex)
        {
            return GetIntersectingPoints_HullToHull(maxReturnCount, otherHull, threadIndex, null, null);
        }
        public IntersectionPoint[] GetIntersectingPoints_HullToHull(int maxReturnCount, CollisionHull otherHull, int threadIndex, Transform3D thisTransform, Transform3D otherTransform)
        {
            float[] finalOffsetThis = thisTransform == null ? new NewtonMatrix(Matrix3D.Identity).Matrix : new NewtonMatrix(thisTransform.Value).Matrix;		// not including the member's offset, because newton is already accounting for it.
            float[] finalOffsetOther = otherTransform == null ? new NewtonMatrix(Matrix3D.Identity).Matrix : new NewtonMatrix(otherTransform.Value).Matrix;

            if (_handle == otherHull._handle && thisTransform == null && otherTransform == null)
            {
                // Newton throws an exception if trying to collide the same hull
                return new IntersectionPoint[0];
            }

            // Prep some arrays to hold the return values
            float[] contacts = new float[3 * maxReturnCount];
            float[] normals = new float[3 * maxReturnCount];
            float[] penetrationDistances = new float[maxReturnCount];

            // Call Newton
            int pointCount = Newton.NewtonCollisionCollide(_world.Handle, maxReturnCount, _handle, finalOffsetThis, otherHull.Handle, finalOffsetOther, contacts, normals, penetrationDistances, threadIndex);

            // Convert to c#
            List<IntersectionPoint> retVal = new List<IntersectionPoint>();

            for (int cntr = 0; cntr < pointCount; cntr++)
            {
                int offset = cntr * 3;

                IntersectionPoint point;
                point.ContactPoint = new NewtonVector3(contacts[offset], contacts[offset + 1], contacts[offset + 2]).ToPointWPF();
                point.Normal = new NewtonVector3(normals[offset], normals[offset + 1], normals[offset + 2]).ToVectorWPF();
                point.PenetrationDistance = penetrationDistances[cntr];
                point.TimeOfImpact = 0;		// time of impact has no meaning for this method (I'm just reusing the same structure between two methods)

                retVal.Add(point);
            }

            // Exit Function
            return retVal.ToArray();


        }

        /// <summary>
        /// This returns the intersection points and time of impact between two hulls within a window of time
        /// NOTE:  The hulls need to start out not touching, and collide within the timestep passed in
        /// </summary>
        /// <param name="maxReturnCount">The maximum number of points to return</param>
        /// <param name="timestep">Maximum time interval consided for the collision calculation</param>
        public IntersectionPoint[] GetIntersectingPoints_HullToHull(int maxReturnCount, double timestep, Vector3D velocity, Vector3D angularVelocity, CollisionHull otherHull, Vector3D otherVelocity, Vector3D otherAngularVelocity, int threadIndex)
        {
            return GetIntersectingPoints_HullToHull(maxReturnCount, timestep, velocity, angularVelocity, otherHull, otherVelocity, otherAngularVelocity, threadIndex, null, null);
        }
        public IntersectionPoint[] GetIntersectingPoints_HullToHull(int maxReturnCount, double timestep, Vector3D velocity, Vector3D angularVelocity, CollisionHull otherHull, Vector3D otherVelocity, Vector3D otherAngularVelocity, int threadIndex, Transform3D thisTransform, Transform3D otherTransform)
        {
            float[] finalOffsetThis = thisTransform == null ? new NewtonMatrix(Matrix3D.Identity).Matrix : new NewtonMatrix(thisTransform.Value).Matrix;		// not including the member's offset, because newton is already accounting for it.
            float[] finalOffsetOther = thisTransform == null ? new NewtonMatrix(Matrix3D.Identity).Matrix : new NewtonMatrix(otherTransform.Value).Matrix;

            // Prep some arrays to hold the return values
            float[] contacts = new float[3 * maxReturnCount];
            float[] normals = new float[3 * maxReturnCount];
            float[] penetrationDistances = new float[maxReturnCount];
            float[] timesOfImpact = new float[maxReturnCount];

            // Call Newton
            int pointCount = Newton.NewtonCollisionCollideContinue(_world.Handle, maxReturnCount, Convert.ToSingle(timestep),
                                                                                                                _handle, finalOffsetThis, new NewtonVector3(velocity).Vector, new NewtonVector3(angularVelocity).Vector,
                                                                                                                otherHull.Handle, finalOffsetOther, new NewtonVector3(otherVelocity).Vector, new NewtonVector3(otherAngularVelocity).Vector,
                                                                                                                timesOfImpact, contacts, normals, penetrationDistances,
                                                                                                                threadIndex);

            // Convert to c#
            List<IntersectionPoint> retVal = new List<IntersectionPoint>();

            for (int cntr = 0; cntr < pointCount; cntr++)
            {
                int offset = cntr * 3;

                IntersectionPoint point;
                point.ContactPoint = new NewtonVector3(contacts[offset], contacts[offset + 1], contacts[offset + 2]).ToPointWPF();
                point.Normal = new NewtonVector3(normals[offset], normals[offset + 1], normals[offset + 2]).ToVectorWPF();
                point.PenetrationDistance = penetrationDistances[cntr];
                point.TimeOfImpact = timesOfImpact[cntr];

                retVal.Add(point);
            }

            // Exit Function
            return retVal.ToArray();
        }

        public Point3D GetFarthestVertex(Vector3D direction)
        {
            // Look this up on the web page, it explains it pretty well (this method is useful for calculating custom bounding surfaces)

            NewtonVector3 retVal = new NewtonVector3();

            Newton.NewtonCollisionSupportVertex(_handle, new NewtonVector3(direction).Vector, retVal.Vector);

            return retVal.ToPointWPF();
        }

        public bool RayCast(out double percentAlongLine, out Point3D? contactPoint, out Vector3D? contactNormal, out int faceID, Point3D startPoint, Point3D endPoint)
        {
            NewtonVector3 normalNewt = new NewtonVector3();
            int faceIDNewt = -1;

            percentAlongLine = Newton.NewtonCollisionRayCast(_handle, new NewtonVector3(startPoint).Vector, new NewtonVector3(endPoint).Vector, normalNewt.Vector, ref faceIDNewt);

            if (percentAlongLine < 0d || percentAlongLine > 1d)
            {
                contactPoint = null;
                contactNormal = null;
                faceID = -1;
                return false;
            }
            else
            {
                contactPoint = startPoint + ((endPoint - startPoint) * percentAlongLine);
                contactNormal = normalNewt.ToVectorWPF();
                faceID = faceIDNewt;
                return true;
            }
        }

        //NOTE: This is not skintight.  For a more accurate AABB, see: http://newtondynamics.com/forum/viewtopic.php?f=12&t=5509#p39550
        public void CalculateAproximateAABB(out Point3D minPoint, out Point3D maxPoint)
        {
            CalculateAproximateAABB(out minPoint, out maxPoint, null);
        }
        public void CalculateAproximateAABB(out Point3D minPoint, out Point3D maxPoint, Transform3D transform)
        {
            float[] finalOffset = transform == null ? new NewtonMatrix(Matrix3D.Identity).Matrix : new NewtonMatrix(transform.Value).Matrix;		// not including _offsetMatrix, because newton is already accounting for it.

            NewtonVector3 minNewt = new NewtonVector3();
            NewtonVector3 maxNewt = new NewtonVector3();

            Newton.NewtonCollisionCalculateAABB(_handle, finalOffset, minNewt.Vector, maxNewt.Vector);

            minPoint = minNewt.ToPointWPF();
            maxPoint = maxNewt.ToPointWPF();
        }

        /// <summary>
        /// This is a helper method to visualize the collision hull.  It fires rays toward the hull, and returns where those rays hit
        /// </summary>
        /// <returns>
        /// Item1=Position
        /// Item2=Normal
        /// </returns>
        public Tuple<Point3D, Vector3D>[] GetVisualizationOfHull(int steps = 10)
        {
            Point3D aabbMin, aabbMax;
            CalculateAproximateAABB(out aabbMin, out aabbMax);

            List<Tuple<Point3D, Vector3D>> retVal = new List<Tuple<Point3D, Vector3D>>();

            //TODO: Adjust the steps to get a square dpi
            int xSteps = steps;
            int ySteps = steps;
            int zSteps = steps;

            // XY
            AxisForDouble axis1 = new AxisForDouble(Axis.X, aabbMin.X, aabbMax.X, xSteps);
            AxisForDouble axis2 = new AxisForDouble(Axis.Y, aabbMin.Y, aabbMax.Y, ySteps);
            Vector3D direction = new Vector3D(0, 0, aabbMax.Z - aabbMin.Z);
            retVal.AddRange(GetVisualizationOfHull_Plate(axis1, axis2, new AxisForDouble(Axis.Z, aabbMin.Z), direction));
            retVal.AddRange(GetVisualizationOfHull_Plate(axis1, axis2, new AxisForDouble(Axis.Z, aabbMax.Z), -direction));

            // XZ
            axis1 = new AxisForDouble(Axis.X, aabbMin.X, aabbMax.X, xSteps);
            axis2 = new AxisForDouble(Axis.Z, aabbMin.Z, aabbMax.Z, zSteps);
            direction = new Vector3D(0, aabbMax.Y - aabbMin.Y, 0);
            retVal.AddRange(GetVisualizationOfHull_Plate(axis1, axis2, new AxisForDouble(Axis.Y, aabbMin.Y), direction));
            retVal.AddRange(GetVisualizationOfHull_Plate(axis1, axis2, new AxisForDouble(Axis.Y, aabbMax.Y), -direction));

            // YZ
            axis1 = new AxisForDouble(Axis.Y, aabbMin.Y, aabbMax.Y, ySteps);
            axis2 = new AxisForDouble(Axis.Z, aabbMin.Z, aabbMax.Z, zSteps);
            direction = new Vector3D(aabbMax.X - aabbMin.X, 0, 0);
            retVal.AddRange(GetVisualizationOfHull_Plate(axis1, axis2, new AxisForDouble(Axis.X, aabbMin.X), direction));
            retVal.AddRange(GetVisualizationOfHull_Plate(axis1, axis2, new AxisForDouble(Axis.X, aabbMax.X), -direction));

            return retVal.ToArray();
        }
        private Tuple<Point3D, Vector3D>[] GetVisualizationOfHull_Plate(AxisForDouble axis1, AxisForDouble axis2, AxisForDouble axis3, Vector3D rayDirection)
        {
            //WARNING: Make sure rayDirection is long enough to pass through the entire hullD

            List<Tuple<Point3D, Vector3D>> retVal = new List<Tuple<Point3D, Vector3D>>();

            double percent;
            Point3D? contactPoint;
            Vector3D? contactNormal;
            int faceID;

            foreach (Point3D point in AxisForDouble.Iterate(axis1, axis2, axis3))
            {
                if (RayCast(out percent, out contactPoint, out contactNormal, out faceID, point, point + rayDirection))
                {
                    retVal.Add(Tuple.Create(contactPoint.Value, contactNormal.Value));
                }
            }

            return retVal.ToArray();
        }

        #endregion

        #region Private Methods

        //NOTE: This doesn't seem to be needed by anything, but is still a good reference
        //TODO: Put this either in UtilityNewt or NewtonMatrix
        //private static float[] GetCombinedOffset(float[] orig, Transform3D additional)
        //{
        //    if (orig == null && additional == null)
        //    {
        //        return null;
        //    }

        //    if (orig != null && additional == null)
        //    {
        //        return orig;
        //    }

        //    if (orig == null && additional != null)
        //    {
        //        return new NewtonMatrix(additional.Value).Matrix;
        //    }

        //    Matrix3D combined = Matrix3D.Multiply(new NewtonMatrix(orig).ToWPF(), additional.Value);

        //    return new NewtonMatrix(combined).Matrix;
        //}

        #endregion

        // I probably don't need to worry about these
        //internal static extern IntPtr NewtonCreateCollisionFromSerialization(IntPtr newtonWorld, NewtonDeserialize deserializeFunction, IntPtr serializeHandle);
        //internal static extern void NewtonCollisionSerialize(IntPtr newtonWorld, IntPtr collision, NewtonSerialize serializeFunction, IntPtr serializeHandle);

        //TODO:  Implement this (lets you apply a tranform matrix on an existing collision object - rotate, skew, etc).  Useful for elongating fast moving objects to ensure collision detection.  I don't get why another collision object is returned
        //internal static extern IntPtr NewtonCreateConvexHullModifier(IntPtr newtonWorld, IntPtr convexHullCollision, int shapeID);		// this is to create a 2nd collision that applies modifications to the first?  Or is it a clone that can then be modified?
        //internal static extern void NewtonConvexHullModifierGetMatrix(IntPtr convexHullCollision, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] matrix);		// this is a property of the collision class?  but why is a second object needed?  why not just directly manipulate the original?
        //internal static extern void NewtonConvexHullModifierSetMatrix(IntPtr convexHullCollision, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] matrix);

        //TODO:  Implement this (there is no documentation, and the cpp is incomplete)
        //internal static extern void NewtonHeightFieldSetUserRayCastCallback(IntPtr treeCollision, NewtonHeightFieldRayCastCallback rayHitCallback);

        //TODO:  Implement this (not sure how to return an arbitrary sized array).  Not quite sure what this is good for, maybe after ray casting or collision, you get the coordinates
        // of the desired triangle?
        //internal static extern int NewtonConvexHullGetFaceIndices(IntPtr convexHullCollision, int face, [MarshalAs(UnmanagedType.LPArray/*, SizeConst = 3*/)]int[] faceIndices);

        //TODO:  New to 2.0 is this add method, but no mention of why it's needed on top of the create methods?  Just to store extra references?
        //internal static extern int NewtonAddCollisionReference(IntPtr collision);

        //TODO:  Implement this if I need it (doubt I ever will)
        //internal static extern void NewtonCollisionForEachPolygonDo(IntPtr collision, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] matrix, NewtonCollisionIterator callback, IntPtr userData);
    }

    #endregion

    #region class: CollisionHullTree

    /// <summary>
    /// I think the tree is an arbitrary mesh (not forced to be convex)
    /// </summary>
    /// <remarks>
    /// Tree hull won't consider mass, so is a static object (buildings and stuff that never moves) - other stuff bounces off of this
    /// </remarks>
    public class CollisionHullTree : CollisionHull
    {
        #region Constructor

        private CollisionHullTree(IntPtr handle, WorldBase world, float[] offsetMatrix)
            : base(handle, world, offsetMatrix, CollisionShapeType.Tree) { }

        #endregion

        //TODO:  Implement this when I see the need.  I think I want to just expose a single create method that takes a wpf Geometry3D directly, and
        // it will call various create/add/end methods internally


        // This needs to be a static creation method
        //internal static extern IntPtr NewtonCreateTreeCollision(IntPtr newtonWorld, int shapeID);




        //internal static extern void NewtonTreeCollisionBeginBuild(IntPtr treeCollision);
        //internal static extern void NewtonTreeCollisionAddFace(IntPtr treeCollision, int vertexCount, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] vertexPtr, int strideInBytes, int faceAttribute);
        //internal static extern void NewtonTreeCollisionEndBuild(IntPtr treeCollision, int optimize);



        //internal static extern void NewtonTreeCollisionSetUserRayCastCallback(IntPtr treeCollision, NewtonCollisionTreeRayCastCallback rayHitCallback);


        //internal static extern int NewtonTreeCollisionGetFaceAtribute(IntPtr treeCollision, [MarshalAs(UnmanagedType.LPArray/*, SizeConst = */)]int[] faceIndexArray);
        //internal static extern void NewtonTreeCollisionSetFaceAtribute(IntPtr treeCollision, [MarshalAs(UnmanagedType.LPArray/*, SizeConst = */)]int[] faceIndexArray, int attribute);
        //internal static extern int NewtonTreeCollisionGetVertexListIndexListInAABB(IntPtr treeCollision, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] p0, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] p1, [MarshalAs(UnmanagedType.LPArray/*, SizeConst = 3*/)]float[,] vertexArray, [MarshalAs(UnmanagedType.LPArray/*, SizeConst = */)]int[] vertexCount, [MarshalAs(UnmanagedType.LPArray/*, SizeConst = */)]int[] vertexStrideInBytes, [MarshalAs(UnmanagedType.LPArray/*, SizeConst = */)]int[] indexList, int maxIndexCount, [MarshalAs(UnmanagedType.LPArray/*, SizeConst = */)]int[] faceAttribute);


        //internal static extern void NewtonStaticCollisionSetDebugCallback(IntPtr staticCollision, NewtonTreeCollisionCallback userCallback);
    }

    #endregion

    #region enum: CollisionShapeType

    /// <summary>
    /// This enum isn't passed to the newton dll, it's just a helper for convenience
    /// </summary>
    public enum CollisionShapeType
    {
        Null,
        Sphere,		// V = 4/3 * pi * r^3
        Box,		// V = length * width * height
        Cone,		// V = 1/3 * pi * r^2 * height
        Capsule,
        Cylinder,		// V = pi *r^2 * height
        ChamferCylinder,
        ConvexHull,
        Compound,
        HeightField,
        Tree
    }

    #endregion

    //Not including the complex breakable, because it's not final - I'll wait until I need it
}
