using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Orig.Math3D
{
    #region Interface: IMyPolygon

    /// <summary>
    /// This represents a polygon.  If your polygon is concave, you need to split it into convex pieces.
    /// </summary>
    public interface IMyPolygon
    {
        /// <summary>
        /// If True, then ChildPolygons holds the rest of the polygon definitions (this instance is either convex, or a composite of disjointed poly's)
        /// If False, then this instance holds all the definition of the polygon
        /// </summary>
        /// <remarks>
        /// NOTE:  If this is false, then it is assumed that this instance is convex (as opposed to concave).  If this is concave, then it needs to be divided
        /// into convex pieces - stored in ChildPolygons
        /// </remarks>
        bool HasChildren { get; }

        /// <summary>
        /// This is only filled out (and non null) if HasChildren is true
        /// </summary>
        IMyPolygon[] ChildPolygons { get; }

        /// <summary>
        /// This is only filled out (and non null) if HasChildren is false
        /// </summary>
        Triangle[] Triangles { get; }

        /// <summary>
        /// It is very likely that the points will be shared across triangles (it would be foolish not to)
        /// This list should always be filled out
        /// </summary>
        MyVector[] UniquePoints { get; }
    }

    #endregion
    #region Class: MyPolygon

    /// <summary>
    /// This is a definition of a polygon (or group of polygons)
    /// </summary>
    /// <remarks>
    /// The reason I don't make this as an abstract class, is because C# doesn't allow multiple inheritance, and
    /// this will be used by classes that are already derived from others.  So the next best thing is an interface.
    /// 
    /// TODO:  Support Children
    /// 
    /// This is NOT equivelent to System.Windows.Shapes.Polygon
    /// </remarks>
    public class MyPolygon : IMyPolygon
    {
        #region Declaration Section

        private Triangle[] _triangles;

        private MyVector[] _uniquePoints;

        #endregion

        #region Constructor

        public MyPolygon(MyVector[] uniquePoints, Triangle[] triangles)
        {
            _uniquePoints = uniquePoints;
            _triangles = triangles;
        }

        #endregion

        #region IMyPolygon Members

        public virtual bool HasChildren
        {
            get
            {
                //TODO:  Implement Children
                return false;
            }
        }
        public virtual IMyPolygon[] ChildPolygons
        {
            get
            {
                //TODO:  Implement Children
                return null;
            }
        }

        public virtual Triangle[] Triangles
        {
            get
            {
                return _triangles;
            }
        }

        public virtual MyVector[] UniquePoints
        {
            get
            {
                return _uniquePoints;
            }
        }

        #endregion

        #region Public Methods

        public virtual MyPolygon Clone()
        {
            //TODO:  Support children.  This will require a separate overload (probably private) that takes the subset of
            //unique points

            MyVector[] clonedPoints = Utility3D.GetClonedArray(_uniquePoints);

            return new MyPolygon(clonedPoints, Utility3D.GetClonedArray(clonedPoints, _triangles));
        }

        #endregion
        #region Public Static Create Polys

        public static MyPolygon CreateCube(double size, bool centered)
        {

            // Sketchup Kicks Ass!!!!!!

            MyVector[] uniquePoints = new MyVector[8];

            uniquePoints[0] = new MyVector(0, 0, 0);
            uniquePoints[1] = new MyVector(size, 0, size);
            uniquePoints[2] = new MyVector(0, 0, size);
            uniquePoints[3] = new MyVector(size, 0, 0);
            uniquePoints[4] = new MyVector(0, size, size);
            uniquePoints[5] = new MyVector(size, size, size);
            uniquePoints[6] = new MyVector(0, size, 0);
            uniquePoints[7] = new MyVector(size, size, 0);

            if (centered)
            {
                double negHalfSize = size * -.5d;
                MyVector offset = new MyVector(negHalfSize, negHalfSize, negHalfSize);

                for (int cntr = 0; cntr < uniquePoints.Length; cntr++)
                {
                    uniquePoints[cntr].Add(offset);
                }
            }

            Triangle[] triangles = new Triangle[12];

            triangles[0] = new Triangle(uniquePoints, 0, 1, 2);
            triangles[1] = new Triangle(uniquePoints, 0, 3, 1);
            triangles[2] = new Triangle(uniquePoints, 2, 1, 4);
            triangles[3] = new Triangle(uniquePoints, 1, 5, 4);
            triangles[4] = new Triangle(uniquePoints, 0, 2, 4);
            triangles[5] = new Triangle(uniquePoints, 0, 4, 6);
            triangles[6] = new Triangle(uniquePoints, 4, 5, 7);
            triangles[7] = new Triangle(uniquePoints, 4, 7, 6);
            triangles[8] = new Triangle(uniquePoints, 1, 3, 7);
            triangles[9] = new Triangle(uniquePoints, 1, 7, 5);
            triangles[10] = new Triangle(uniquePoints, 0, 6, 7);
            triangles[11] = new Triangle(uniquePoints, 0, 7, 3);

            return new MyPolygon(uniquePoints, triangles);
        }

        public static MyPolygon CreateTetrahedron(double size, bool centered)
        {

            // Sketchup and MathWorld are cool

            //double height2D = Math.Sqrt((size * size) - ((size * size) / 4d));
            double height2D = size * (Math.Sqrt(3) / 2d);
            double height3D = Math.Sqrt(6d) * (size / 3d);
            double halfSize = size / 2d;

            MyVector[] uniquePoints = new MyVector[4];

            uniquePoints[0] = new MyVector(0, 0, 0);
            uniquePoints[1] = new MyVector(size, 0, 0);
            uniquePoints[2] = new MyVector(halfSize, height2D, 0);
            uniquePoints[3] = new MyVector(halfSize, Math.Tan(Utility3D.GetDegreesToRadians(30)) * halfSize, height3D);

            if (centered)
            {
                double negHalfHeight3d = height3D * -.5d;
                MyVector offset = new MyVector(negHalfHeight3d, negHalfHeight3d, negHalfHeight3d);

                for (int cntr = 0; cntr < uniquePoints.Length; cntr++)
                {
                    uniquePoints[cntr].Add(offset);
                }
            }

            Triangle[] triangles = new Triangle[4];

            triangles[0] = new Triangle(uniquePoints, 0, 1, 3);
            triangles[1] = new Triangle(uniquePoints, 1, 2, 3);
            triangles[2] = new Triangle(uniquePoints, 0, 3, 2);
            triangles[3] = new Triangle(uniquePoints, 0, 2, 1);

            return new MyPolygon(uniquePoints, triangles);
        }

        #endregion
    }

    #endregion
    #region Class: MyPolygonSyncedRotation

    /// <summary>
    /// This remembers the original definition, but keeps a copy that is kept rotated to the quaternion.
    /// Since the quaternion doesn't have events, the sync will have to be manually called.
    /// </summary>
    public class MyPolygonSyncedRotation : MyPolygon
    {
        #region Declaration Section

        /// <summary>
        /// This is a clone of the base polygon, but is kept rotated
        /// </summary>
        private MyPolygon _rotatedPoly;

        /// <summary>
        /// This gets reset each time SyncRotation is called.  The only reason I keep it around is so Clone will work
        /// </summary>
        private MyQuaternion _rotationForClone = null;

        #endregion

        #region Constructor

        public MyPolygonSyncedRotation(MyVector[] uniquePoints, Triangle[] triangles, MyQuaternion rotation)
            : base(uniquePoints, triangles)
        {
            MyVector[] clonedPoints = Utility3D.GetClonedArray(uniquePoints);
            _rotatedPoly = new MyPolygon(clonedPoints, Utility3D.GetClonedArray(clonedPoints, triangles));

            SyncRotation(rotation);
        }

        #endregion

        #region IMyPolygon Members

        public override Triangle[] Triangles
        {
            get
            {
                return _rotatedPoly.Triangles;
            }
        }

        public override MyVector[] UniquePoints
        {
            get
            {
                return _rotatedPoly.UniquePoints;
            }
        }

        #endregion

        #region Public Methods

        public override MyPolygon Clone()
        {
            MyVector[] clonedPoints = Utility3D.GetClonedArray(base.UniquePoints);
            return new MyPolygonSyncedRotation(clonedPoints, Utility3D.GetClonedArray(clonedPoints, base.Triangles), _rotationForClone.Clone());
        }
        public MyPolygonSyncedRotation ClonePolygonSyncedRotation()
        {
            return (MyPolygonSyncedRotation)Clone();
        }

        /// <summary>
        /// Since the quaternion doesn't have any events, this function needs to be called manually whenever
        /// a rotation occurs
        /// </summary>
        /// <remarks>
        /// I need to take in the rotation, because the Sphere class keeps blowing away it's rotation, so this would
        /// immediately fall out of sync
        /// </remarks>
        public void SyncRotation(MyQuaternion rotation)
        {

            // I don't know if there's much of a gain in caching (or even a loss)
            MyVector[] cachedBasePoints = base.UniquePoints;
            MyVector[] cachedThisPoints = _rotatedPoly.UniquePoints;

            // Rotate all the points
            for (int pointCntr = 0; pointCntr < cachedBasePoints.Length; pointCntr++)
            {
                cachedThisPoints[pointCntr].StoreNewValues(rotation.GetRotatedVector(cachedBasePoints[pointCntr], true));
            }

            // Remember this for later
            _rotationForClone = rotation;

        }

        #endregion
    }

    #endregion

    #region Class: SpherePolygon

    public class SpherePolygon : Sphere, IMyPolygon
    {
        #region Declaration Section

        private MyPolygonSyncedRotation _polygon;

        #endregion

        #region Constructor

        /// <summary>
        /// NOTE: Radius should fully surround the polygon if this will be used in collisions
        /// </summary>
        public SpherePolygon(MyVector position, DoubleVector origDirectionFacing, MyPolygon polygon, double radius)
            : base(position, origDirectionFacing, radius)
        {
            _polygon = new MyPolygonSyncedRotation(polygon.UniquePoints, polygon.Triangles, this.Rotation);
        }

        /// <summary>
        /// This overload should only be used during a clone.  I simply trust the values passed to me
        /// </summary>
        protected SpherePolygon(MyVector position, DoubleVector origDirectionFacing, MyQuaternion rotation, MyPolygonSyncedRotation polygon, double radius)
            : base(position, origDirectionFacing, rotation, radius)
        {
            _polygon = polygon;
        }

        #endregion

        #region IPolygon Members

        public bool HasChildren
        {
            get { return _polygon.HasChildren; }
        }

        public IMyPolygon[] ChildPolygons
        {
            get { return _polygon.ChildPolygons; }
        }

        public Triangle[] Triangles
        {
            get { return _polygon.Triangles; }
        }

        public MyVector[] UniquePoints
        {
            get { return _polygon.UniquePoints; }
        }

        #endregion

        #region Public Methods

        public override Sphere Clone()
        {
            return new SpherePolygon(this.Position.Clone(), this.OriginalDirectionFacing.Clone(), this.Rotation.Clone(), _polygon.ClonePolygonSyncedRotation(), this.Radius);
        }
        public SpherePolygon CloneSpherePolygon()
        {
            return (SpherePolygon)Clone();
        }

        public override void RotateAroundAxis(MyVector rotateAround, double radians)
        {
            base.RotateAroundAxis(rotateAround, radians);

            _polygon.SyncRotation(this.Rotation);
        }

        #endregion
    }

    #endregion
    #region Class: BallPolygon

    public class BallPolygon : Ball, IMyPolygon
    {
        #region Declaration Section

        private MyPolygonSyncedRotation _polygon;

        #endregion

        #region Constructor

        public BallPolygon(MyVector position, DoubleVector origDirectionFacing, MyPolygon polygon, double radius, double mass)
            : base(position, origDirectionFacing, radius, mass)
        {
            _polygon = new MyPolygonSyncedRotation(polygon.UniquePoints, polygon.Triangles, this.Rotation);
        }

        public BallPolygon(MyVector position, DoubleVector origDirectionFacing, MyPolygon polygon, double radius, double mass, MyVector boundingBoxLower, MyVector boundingBoxUpper)
            : base(position, origDirectionFacing, radius, mass, boundingBoxLower, boundingBoxUpper)
        {
            _polygon = new MyPolygonSyncedRotation(polygon.UniquePoints, polygon.Triangles, this.Rotation);
        }

        /// <summary>
        /// This overload is used if you plan to do collisions
        /// </summary>
        public BallPolygon(MyVector position, DoubleVector origDirectionFacing, MyPolygon polygon, double radius, double mass, double elasticity, double kineticFriction, double staticFriction, MyVector boundingBoxLower, MyVector boundingBoxUpper)
            : base(position, origDirectionFacing, radius, mass, elasticity, kineticFriction, staticFriction, boundingBoxLower, boundingBoxUpper)
        {
            _polygon = new MyPolygonSyncedRotation(polygon.UniquePoints, polygon.Triangles, this.Rotation);
        }

        /// <summary>
        /// This one is used to assist with the clone method (especially for my derived classes)
        /// </summary>
        /// <param name="usesBoundingBox">Just pass in what you have</param>
        /// <param name="boundingBoxLower">Set this to null if bounding box is false</param>
        /// <param name="boundingBoxUpper">Set this to null if bounding box is false</param>
        protected BallPolygon(MyVector position, DoubleVector origDirectionFacing, MyPolygonSyncedRotation polygon, MyQuaternion rotation, double radius, double mass, double elasticity, double kineticFriction, double staticFriction, bool usesBoundingBox, MyVector boundingBoxLower, MyVector boundingBoxUpper)
            : base(position, origDirectionFacing, rotation, radius, mass, elasticity, kineticFriction, staticFriction, usesBoundingBox, boundingBoxLower, boundingBoxUpper)
        {
            _polygon = polygon;
        }

        #endregion

        #region IPolygon Members

        public bool HasChildren
        {
            get { return _polygon.HasChildren; }
        }

        public IMyPolygon[] ChildPolygons
        {
            get { return _polygon.ChildPolygons; }
        }

        public Triangle[] Triangles
        {
            get { return _polygon.Triangles; }
        }

        public MyVector[] UniquePoints
        {
            get { return _polygon.UniquePoints; }
        }

        #endregion

        #region Public Methods

        public override Sphere Clone()
        {
            // I want a copy of the bounding box, not a clone (everything else gets cloned)
            return new BallPolygon(this.Position.Clone(), this.OriginalDirectionFacing.Clone(), _polygon.ClonePolygonSyncedRotation(), this.Rotation.Clone(), this.Radius, this.Mass, this.Elasticity, this.KineticFriction, this.StaticFriction, this.UsesBoundingBox, this.BoundryLower, this.BoundryUpper);
        }
        public BallPolygon CloneBallPolygon()
        {
            return (BallPolygon)Clone();
        }

        public override void RotateAroundAxis(MyVector rotateAround, double radians)
        {
            base.RotateAroundAxis(rotateAround, radians);

            _polygon.SyncRotation(this.Rotation);
        }

        #endregion
    }

    #endregion
    #region Class: SolidBallPolygon

    public class SolidBallPolygon : SolidBall, IMyPolygon
    {
        #region Declaration Section

        private MyPolygonSyncedRotation _polygon;

        #endregion

        #region Constructor

        public SolidBallPolygon(MyVector position, DoubleVector origDirectionFacing, MyPolygon polygon, double radius, double mass)
            : base(position, origDirectionFacing, radius, mass)
        {
            _polygon = new MyPolygonSyncedRotation(polygon.UniquePoints, polygon.Triangles, this.Rotation);
        }

        public SolidBallPolygon(MyVector position, DoubleVector origDirectionFacing, MyPolygon polygon, double radius, double mass, MyVector boundingBoxLower, MyVector boundingBoxUpper)
            : base(position, origDirectionFacing, radius, mass, boundingBoxLower, boundingBoxUpper)
        {
            _polygon = new MyPolygonSyncedRotation(polygon.UniquePoints, polygon.Triangles, this.Rotation);
        }

        /// <summary>
        /// This overload is used if you plan to do collisions
        /// </summary>
        public SolidBallPolygon(MyVector position, DoubleVector origDirectionFacing, MyPolygon polygon, double radius, double mass, double elasticity, double kineticFriction, double staticFriction, MyVector boundingBoxLower, MyVector boundingBoxUpper)
            : base(position, origDirectionFacing, radius, mass, elasticity, kineticFriction, staticFriction, boundingBoxLower, boundingBoxUpper)
        {
            _polygon = new MyPolygonSyncedRotation(polygon.UniquePoints, polygon.Triangles, this.Rotation);
        }

        /// <summary>
        /// This one is used to assist with the clone method (especially for my derived classes)
        /// </summary>
        /// <param name="usesBoundingBox">Just pass in what you have</param>
        /// <param name="boundingBoxLower">Set this to null if bounding box is false</param>
        /// <param name="boundingBoxUpper">Set this to null if bounding box is false</param>
        protected SolidBallPolygon(MyVector position, DoubleVector origDirectionFacing, MyQuaternion rotation, MyPolygonSyncedRotation polygon, double radius, double mass, double elasticity, double kineticFriction, double staticFriction, bool usesBoundingBox, MyVector boundingBoxLower, MyVector boundingBoxUpper)
            : base(position, origDirectionFacing, rotation, radius, mass, elasticity, kineticFriction, staticFriction, usesBoundingBox, boundingBoxLower, boundingBoxUpper)
        {
            _polygon = polygon;
        }

        #endregion

        #region IPolygon Members

        public bool HasChildren
        {
            get { return _polygon.HasChildren; }
        }

        public IMyPolygon[] ChildPolygons
        {
            get { return _polygon.ChildPolygons; }
        }

        public Triangle[] Triangles
        {
            get { return _polygon.Triangles; }
        }

        public MyVector[] UniquePoints
        {
            get { return _polygon.UniquePoints; }
        }

        #endregion

        #region Public Methods

        public override Sphere Clone()
        {
            // I want a copy of the bounding box, not a clone (everything else gets cloned)
            return new SolidBallPolygon(this.Position.Clone(), this.OriginalDirectionFacing.Clone(), this.Rotation.Clone(), _polygon.ClonePolygonSyncedRotation(), this.Radius, this.Mass, this.Elasticity, this.KineticFriction, this.StaticFriction, this.UsesBoundingBox, this.BoundryLower, this.BoundryUpper);
        }
        public SolidBallPolygon CloneSolidBallPolygon()
        {
            return (SolidBallPolygon)Clone();
        }

        public override void RotateAroundAxis(MyVector rotateAround, double radians)
        {
            base.RotateAroundAxis(rotateAround, radians);

            _polygon.SyncRotation(this.Rotation);
        }

        #endregion
    }

    #endregion
    #region Class: RigidBodyPolygon

    public class RigidBodyPolygon : RigidBody, IMyPolygon
    {
        #region Declaration Section

        private MyPolygonSyncedRotation _polygon;

        #endregion

        #region Constructor

        public RigidBodyPolygon(MyVector position, DoubleVector origDirectionFacing, MyPolygon polygon, double radius)
            : base(position, origDirectionFacing, radius)
        {
            _polygon = new MyPolygonSyncedRotation(polygon.UniquePoints, polygon.Triangles, this.Rotation);
        }

        public RigidBodyPolygon(MyVector position, DoubleVector origDirectionFacing, MyPolygon polygon, double radius, MyVector boundingBoxLower, MyVector boundingBoxUpper)
            : base(position, origDirectionFacing, radius, boundingBoxLower, boundingBoxUpper)
        {
            _polygon = new MyPolygonSyncedRotation(polygon.UniquePoints, polygon.Triangles, this.Rotation);
        }

        /// <summary>
        /// This overload is used if you plan to do collisions
        /// </summary>
        public RigidBodyPolygon(MyVector position, DoubleVector origDirectionFacing, MyPolygon polygon, double radius, double elasticity, double kineticFriction, double staticFriction, MyVector boundingBoxLower, MyVector boundingBoxUpper)
            : base(position, origDirectionFacing, radius, elasticity, kineticFriction, staticFriction, boundingBoxLower, boundingBoxUpper)
        {
            _polygon = new MyPolygonSyncedRotation(polygon.UniquePoints, polygon.Triangles, this.Rotation);
        }

        /// <summary>
        /// This one is used to assist with the clone method (especially for my derived classes)
        /// </summary>
        /// <param name="usesBoundingBox">Just pass in what you have</param>
        /// <param name="boundingBoxLower">Set this to null if bounding box is false</param>
        /// <param name="boundingBoxUpper">Set this to null if bounding box is false</param>
        protected RigidBodyPolygon(MyVector position, DoubleVector origDirectionFacing, MyQuaternion rotation, MyPolygonSyncedRotation polygon, double radius, double elasticity, double kineticFriction, double staticFriction, bool usesBoundingBox, MyVector boundingBoxLower, MyVector boundingBoxUpper)
            : base(position, origDirectionFacing, rotation, radius, elasticity, kineticFriction, staticFriction, usesBoundingBox, boundingBoxLower, boundingBoxUpper)
        {
            _polygon = polygon;
        }

        #endregion

        #region IPolygon Members

        public bool HasChildren
        {
            get { return _polygon.HasChildren; }
        }

        public IMyPolygon[] ChildPolygons
        {
            get { return _polygon.ChildPolygons; }
        }

        public Triangle[] Triangles
        {
            get { return _polygon.Triangles; }
        }

        public MyVector[] UniquePoints
        {
            get { return _polygon.UniquePoints; }
        }

        #endregion

        #region Public Methods

        public override Sphere Clone()
        {
            // Make a shell
            // I want a copy of the bounding box, not a clone (everything else gets cloned)
            RigidBodyPolygon retVal = new RigidBodyPolygon(this.Position.Clone(), this.OriginalDirectionFacing.Clone(), this.Rotation.Clone(), _polygon.ClonePolygonSyncedRotation(), this.Radius, this.Elasticity, this.KineticFriction, this.StaticFriction, this.UsesBoundingBox, this.BoundryLower, this.BoundryUpper);

            PointMass[] clonedMasses = this.PointMasses;

            // Clone them (I don't want the events to go with)
            for (int massCntr = 0; massCntr < clonedMasses.Length; massCntr++)
            {
                clonedMasses[massCntr] = clonedMasses[massCntr].Clone();
            }

            // Now I can add the masses to my return
            retVal.AddRangePointMasses(clonedMasses);

            // Exit Function
            return retVal;
        }
        public RigidBodyPolygon CloneRigidBodyPolygon()
        {
            return (RigidBodyPolygon)Clone();
        }

        public override void RotateAroundAxis(MyVector rotateAround, double radians)
        {
            base.RotateAroundAxis(rotateAround, radians);

            _polygon.SyncRotation(this.Rotation);
        }

        #endregion
    }

    #endregion
}
