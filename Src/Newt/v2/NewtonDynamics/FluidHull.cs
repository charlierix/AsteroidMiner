using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;

using Game.HelperClassesWPF;
using System.Windows;

namespace Game.Newt.v2.NewtonDynamics
{
    public class FluidHull
    {
        #region Class: FluidTriangle

        public class FluidTriangle : TriangleIndexed
        {
            #region Declaration Section

            /// <summary>
            /// These are sets of sample points for this triangle
            /// </summary>
            /// <remarks>
            /// It would be expensive to sample too many points in any one frame.  So each frame, a subset of the points are sampled used.
            /// 
            /// The index into _forcePointSets is what will be used in a frame
            /// 
            /// The vectors are 2D, because they are the percent along the 2 edges:
            /// X=Percent along segment 0,1
            /// Y=Percent along segment 0,2
            /// </remarks>
            private List<Vector[]> _forcePointSets = null;

            private int _currentFrame = -1;

            /// <summary>
            /// This is the same size as base.AllPoints.  But the base's points are in model coords.  This is world coords, and
            /// is refreshed each frame
            /// </summary>
            private Point3D[] _allPointsWorld = null;

            private Point3D[] _forcesAt = null;
            private Vector3D[] _forces = null;

            #endregion

            #region Constructor

            public FluidTriangle(int index0, int index1, int index2, Point3D[] allPoints)
                : base(index0, index1, index2, allPoints) { }

            #endregion

            #region Public Methods

            /// <summary>
            /// This gets the forces acting on this triangle this frame
            /// </summary>
            public void GetForces(out Point3D[] forcesAt, out Vector3D[] forces)
            {
                if (_forcesAt != null && _forces != null)
                {
                    forcesAt = _forcesAt;
                    forces = _forces;
                }
                else
                {
                    forcesAt = null;
                    forces = null;
                }
            }

            internal void StoreForcePointSets(List<Vector[]> forcePointSets)
            {
                _forcePointSets = forcePointSets;
                _currentFrame = 0;
            }

            internal void NextFrame(Point3D[] allPointsWorld)
            {
                _allPointsWorld = allPointsWorld;
                _forcesAt = null;
                _forces = null;

                if (_forcePointSets != null)		// if it's null, then this triangle doesn't have any sample force points, so there is nothing to do
                {
                    _currentFrame++;
                    if (_currentFrame >= _forcePointSets.Count)
                    {
                        _currentFrame = 0;
                    }
                }
            }

            /// <summary>
            /// This gets the sample points for the current frame
            /// </summary>
            internal Point3D[] GetForceAt()
            {
                if (_forcePointSets == null || _forcePointSets[_currentFrame] == null)
                {
                    // This frame doesn't have any sample points
                    return null;
                }

                if (_forcesAt == null)
                {
                    if (_forcePointSets[_currentFrame] == null)
                    {
                        // Some frames may not have any sample points (especially for smaller triangles, or sparsely sampled hulls)
                        return null;
                    }

                    // The positions are stored as percents along these two lines
                    Vector3D line01 = _allPointsWorld[this.Index1] - _allPointsWorld[this.Index0];
                    Vector3D line02 = _allPointsWorld[this.Index2] - _allPointsWorld[this.Index0];

                    _forcesAt = new Point3D[_forcePointSets[_currentFrame].Length];

                    for (int cntr = 0; cntr < _forcesAt.Length; cntr++)
                    {
                        // P0 + %V01 + %V02
                        _forcesAt[cntr] = _allPointsWorld[this.Index0] +
                                                        (line01 * _forcePointSets[_currentFrame][cntr].X) +
                                                        (line02 * _forcePointSets[_currentFrame][cntr].Y);
                    }
                }

                // Exit Function
                return _forcesAt;
            }
            /// <summary>
            /// This stores the forces at each sample point for the current frame
            /// </summary>
            internal void SetForces(Vector3D[] forces)
            {
                _forces = forces;
            }

            // These are just wrappers to utilitywpf as a convenience
            public static FluidTriangle[] GetTrianglesFromMesh(MeshGeometry3D mesh, Transform3D transform = null)
            {
                return UtilityWPF.GetTrianglesFromMesh(mesh, transform).
                    Select(o => new FluidTriangle(o.Index0, o.Index1, o.Index2, o.AllPoints)).
                    ToArray();
            }
            public static FluidTriangle[] GetTrianglesFromMesh(MeshGeometry3D[] meshes, Transform3D[] transforms = null)
            {
                return UtilityWPF.GetTrianglesFromMesh(meshes, transforms).
                    Select(o => new FluidTriangle(o.Index0, o.Index1, o.Index2, o.AllPoints)).
                    ToArray();
            }

            #endregion
        }

        #endregion

        #region Declaration Section

        /// <summary>
        /// Random points across the hull are chosen for calculating forces.  When the hull is newly created, this stays false until the first time Update
        /// is called
        /// </summary>
        private bool _areForcePointsChosen = false;

        private bool _areForcesSet = false;

        private double _sampleRadius = 1d;

        #endregion

        #region Public Properties

        public IFluidField Field
        {
            get;
            set;
        }

        private FluidTriangle[] _triangles = null;
        public FluidTriangle[] Triangles
        {
            get
            {
                return _triangles;
            }
            set
            {
                _triangles = value;
                _areForcePointsChosen = false;
            }
        }

        /// <summary>
        /// If this is true, then only the triangles that are in the wind will feel force
        /// </summary>
        /// <remarks>
        /// It's not quite as simple as an entire triangle being in the wind, because the body could be spinning
        /// </remarks>
        public bool IsClosedConvexInUniformField
        {
            get;
            set;
        }

        public Transform3D Transform
        {
            get;
            set;
        }

        public Body Body
        {
            get;
            set;
        }

        private int _numPointsPerSet = 25;
        public int NumPointsPerSet
        {
            get
            {
                return _numPointsPerSet;
            }
            set
            {
                _numPointsPerSet = value;
                _areForcePointsChosen = false;
            }
        }
        private int _numSets = 10;
        public int NumSets
        {
            get
            {
                return _numSets;
            }
            set
            {
                _numSets = value;
                _areForcePointsChosen = false;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// If this fluid hull is tied to a physics body, then this method must be called from within that body's apply force/torque callback
        /// </summary>
        public void Update()
        {
            if (AreForcesZero())
            {
                // All forces are zero, so don't waste the processor
                #region Clear Forces

                if (_triangles != null && !_areForcesSet)		// added this so I don't waste the processor clearing forces on a dead object each frame
                {
                    foreach (FluidTriangle triangle in _triangles)
                    {
                        triangle.NextFrame(null);
                    }

                    _areForcesSet = true;
                }

                #endregion
                return;
            }

            if (!_areForcePointsChosen)
            {
                // First time init
                ChooseForcePoints();
                _areForcePointsChosen = true;
            }

            // Transform the points to world coords
            Point3D[] worldPoints = GetTrianglePointsWorld();

            foreach (FluidTriangle triangle in _triangles)      // AreForcesZero verified that _triangles isn't null
            {
                triangle.NextFrame(worldPoints);

                Point3D[] forcesAt = triangle.GetForceAt();
                if (forcesAt == null)
                {
                    continue;
                }

                Vector3D[] forces = new Vector3D[forcesAt.Length];

                // Can't use triangle.Normal, because it's still in model coords
                Vector3D normal = Vector3D.CrossProduct(worldPoints[triangle.Index2] - worldPoints[triangle.Index1],
                                                                                        worldPoints[triangle.Index0] - worldPoints[triangle.Index1]);

                //double avgNormalLength = triangle.Normal.Length / forcesAt.Length;
                double avgNormalLength = normal.Length / forcesAt.Length;

                Tuple<Vector3D, double>[] flowsAt = this.Field.GetForce(forcesAt, _sampleRadius);

                for (int cntr = 0; cntr < forcesAt.Length; cntr++)
                {
                    #region Sample Point

                    // Calculate the velocity of the triangle at this point
                    Vector3D velocityAtPoint = new Vector3D(0, 0, 0);
                    if (this.Body != null)
                    {
                        velocityAtPoint = this.Body.GetVelocityAtPoint(forcesAt[cntr]);
                    }

                    Vector3D flowAtPoint = flowsAt[cntr].Item1 - velocityAtPoint;

                    // If it's convex/uniform, only allow forces that flow into the hull
                    if (!this.IsClosedConvexInUniformField || Vector3D.DotProduct(flowAtPoint, normal) < 0)
                    {
                        // Scale the normal based on how much of the flow is along it
                        forces[cntr] = flowAtPoint.GetProjectedVector(normal);		// the length returned is from zero to flowAtPoint.Length.  avgNormal's length is ignored, only its direction

                        // Scale the force by the area of the triangle (cross gives area of parallelagram, so taking half)
                        // Also scale by viscocity
                        forces[cntr] *= avgNormalLength * (.5d * flowsAt[cntr].Item2);

                        // Apply the force to the body
                        if (this.Body != null && !Math1D.IsNearZero(forces[cntr].LengthSquared))
                        {
                            this.Body.AddForceAtPoint(forces[cntr], forcesAt[cntr]);
                        }
                    }

                    #endregion
                }

                triangle.SetForces(forces);		// this is only stored here for debugging, etc (physics doesn't look at it)
            }
        }

        public Point3D[] GetTrianglePointsWorld()
        {
            if (_triangles == null || _triangles.Length == 0)
            {
                return null;
            }

            Point3D[] retVal = new Point3D[_triangles[0].AllPoints.Length];

            for (int cntr = 0; cntr < retVal.Length; cntr++)
            {
                if (this.Transform == null)
                {
                    retVal[cntr] = _triangles[0].AllPoints[cntr];
                }
                else
                {
                    retVal[cntr] = this.Transform.Transform(_triangles[0].AllPoints[cntr]);
                }
            }

            return retVal;
        }

        #endregion

        #region Private Methods

        private bool AreForcesZero()
        {
            if (this.Field == null || _triangles == null || _triangles.Length == 0)
            {
                return true;
            }

            bool isZeroForce = false;

            if (this.Field is FluidFieldUniform)
            {
                var velocities = this.Field.GetForce(new[] { new Point3D(0, 0, 0) }, 1);

                isZeroForce = Math1D.IsNearZero(velocities[0].Item1.LengthSquared);
            }

            if (isZeroForce && (this.Body == null || (Math1D.IsNearZero(this.Body.Velocity.LengthSquared) && Math1D.IsNearZero(this.Body.AngularVelocity.LengthSquared))))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void ChooseForcePoints()
        {
            SortedList<int, List<Point3D>[]> pointSets = new SortedList<int, List<Point3D>[]>();

            for (int cntr = 0; cntr < _numSets; cntr++)
            {
                //TODO:  This method should return barycentric coords directly
                // Create random points across the triangles
                SortedList<int, List<Point3D>> points = Math3D.GetRandomPoints_OnHull_Structured(_triangles, _numPointsPerSet);

                // Add these to the sets
                foreach (int triangleIndex in points.Keys)
                {
                    if (!pointSets.ContainsKey(triangleIndex))
                    {
                        pointSets.Add(triangleIndex, new List<Point3D>[_numSets]);
                    }

                    pointSets[triangleIndex][cntr] = points[triangleIndex];
                }
            }

            foreach (int triangleIndex in pointSets.Keys)
            {
                List<Vector[]> localSets = new List<Vector[]>();

                for (int setCntr = 0; setCntr < _numSets; setCntr++)
                {
                    List<Point3D> points = pointSets[triangleIndex][setCntr];

                    if (points == null || points.Count == 0)
                    {
                        localSets.Add(null);
                        continue;
                    }

                    Vector[] set = new Vector[points.Count];
                    localSets.Add(set);

                    for (int cntr = 0; cntr < points.Count; cntr++)
                    {
                        set[cntr] = Math3D.ToBarycentric(this.Triangles[triangleIndex], points[cntr]);
                    }
                }

                // Store these sets in this triangle
                this.Triangles[triangleIndex].StoreForcePointSets(localSets);
            }

            // Calculate the sample radius
            if (_triangles.Length > 0)
            {
                // The sum of the volumes of point samples needs to equal the volume of the hull:
                // N(4/3 pi r^3)=Vol
                // r=cube root((Vol/N)/(4/3 pi))

                var aabb = Math3D.GetAABB(_triangles[0].AllPoints);     // using AABB as a safe way to get the volume of the hull

                double volume = Math.Abs(aabb.Item2.X - aabb.Item1.X) * Math.Abs(aabb.Item2.Y - aabb.Item1.Y) * Math.Abs(aabb.Item2.Z - aabb.Item1.Z);

                double intermediate = (volume / _numPointsPerSet) / ((4d / 3d) * Math.PI);

                _sampleRadius = Math.Abs(Math.Pow(intermediate, 1d / 3d));
            }
            else
            {
                _sampleRadius = 0d;
            }
        }

        #endregion
    }
}
