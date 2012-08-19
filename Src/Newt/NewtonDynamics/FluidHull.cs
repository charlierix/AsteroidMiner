using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;

using Game.Newt.HelperClasses;
using System.Windows;

namespace Game.Newt.NewtonDynamics
{

	//TODO: Support IsClosedConvex.  If this is true, then triangles that have flow coming from inside the hull won't have any force (can't just
	//	calculate for the entire triangle, because the hull could be spinning, but you take the flow at point dot triangle normal, and only apply force
	//	if the flow comes from outside the hull


	/// <summary>
	/// This does fluid friction calculations.  You define a hull, tell this the properties of the fluid, then call update
	/// 
	/// TODO:  Ignore triangles that are behind others (zbuffer?)
	/// TODO:  Reduce force for partially covered triangles? - this could get messy, because the force won't be applied at the center of the triangle any
	/// more.  It's probably cheaper to just define a few more triangles
	/// </summary>
	/// <remarks>
	/// This can work with or without a physics body attached to it
	///		If there is no body, the caller will need to look at the forces acting on each triangle after calling update.
	///		If there is a body, this will apply the forces to the body each time update is called
	/// 
	/// NOTE:  The fluid friction algorithm is very simple, it's just a dot product of the fluid direction with each triangle's normal.  It doesn't calculate
	/// laminar/turbulent flow, etc.  So a pointed object shouldn't have any less friction than the same diameter plate (along the direction of flow)
	/// 
	/// NOTE:  Even if there is a physics body attached, this class ignores the collision hull.  It always uses the triangles its told about
	/// </remarks>
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
			///	X=Percent along segment 0,1
			///	Y=Percent along segment 0,2
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

			public FluidTriangle()
				: base() { }

			public FluidTriangle(int index0, int index1, int index2, Point3D[] allPoints)
				: base(index0, index1, index2, allPoints) { }

			#endregion

			#region Public Properties

			//	Each time you call update, these tell what the force is on that triangle (these are in world coords)
			//public Point3D ForceAt = new Point3D();
			//public Vector3D Force = new Vector3D();

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

				if (_forcePointSets != null)		//	if it's null, then this triangle doesn't have any sample force points, so there is nothing to do
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
					//	This frame doesn't have any sample points
					return null;
				}

				if (_forcesAt == null)
				{
					if (_forcePointSets[_currentFrame] == null)
					{
						//	Some frames may not have any sample points (especially for smaller triangles, or sparsely sampled hulls)
						return null;
					}

					//	The positions are stored as percents along these two lines
					Vector3D line01 = _allPointsWorld[this.Index1] - _allPointsWorld[this.Index0];
					Vector3D line02 = _allPointsWorld[this.Index2] - _allPointsWorld[this.Index0];

					_forcesAt = new Point3D[_forcePointSets[_currentFrame].Length];

					for (int cntr = 0; cntr < _forcesAt.Length; cntr++)
					{
						//	P0 + %V01 + %V02
						_forcesAt[cntr] = _allPointsWorld[this.Index0] +
														(line01 * _forcePointSets[_currentFrame][cntr].X) +
														(line02 * _forcePointSets[_currentFrame][cntr].Y);
					}
				}

				//	Exit Function
				return _forcesAt;
			}
			/// <summary>
			/// This stores the forces at each sample point for the current frame
			/// </summary>
			internal void SetForces(Vector3D[] forces)
			{
				_forces = forces;
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

		/// <summary>
		/// During the update, this will go true if the hull actually feels any fluid forces.  It will stay false if there are no fluid forces
		/// on the hull (in a vacuum, or not moving) - even though update keeps getting called.
		/// </summary>
		private bool _areForcesSet = false;

		#endregion

		#region Constructor

		public FluidHull()
		{
		}

		#endregion

		#region Public Properties

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
		public bool IsClosedConvex
		{
			get;
			set;
		}

		public Transform3D Transform
		{
			get;
			set;
		}

		/// <summary>
		/// This is the speed and direction of the flowing fluid (world coords)
		/// </summary>
		public Vector3D FluidFlow
		{
			get;
			set;
		}
		/// <summary>
		/// This is the viscosity of the fluid
		/// NOTE: This just acts as a multiplier (newtonian fluid)
		/// </summary>
		/// <remarks>
		/// I did some reading, and I think just multipying the final force by this is accurate.  If not, then this needs to be fixed
		/// </remarks>
		public double FluidViscosity
		{
			get;
			set;
		}

		public Body Body
		{
			get;
			set;
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// This returns a hull based on the triangles defined in the geometry (the hull won't be tied to the geometry afterward, it just gets
		/// the points)
		/// </summary>
		public static FluidHull FromGeometry(MeshGeometry3D geometry, Transform3D transform, bool isClosedConvex)
		{
			return FromGeometry(new MeshGeometry3D[] { geometry }, new Transform3D[] { transform }, isClosedConvex);
		}
		public static FluidHull FromGeometry(MeshGeometry3D[] geometries, Transform3D[] transforms, bool isClosedConvex)
		{
			#region AllPoints

			//Point3D[] allPoints = geometries.SelectMany(o => o.Positions).ToArray();		//	this is too simple.  The positions must be transformed

			List<Point3D> allPointsList = new List<Point3D>();
			for (int cntr = 0; cntr < geometries.Length; cntr++)
			{
				Point3D[] positions = geometries[cntr].Positions.ToArray();

				if (transforms != null && transforms[cntr] != null)
				{
					transforms[cntr].Transform(positions);
				}

				allPointsList.AddRange(positions);
			}

			Point3D[] allPoints = allPointsList.ToArray();

			#endregion

			#region Geometries

			List<FluidTriangle> triangles = new List<FluidTriangle>();

			int posOffset = 0;

			foreach (MeshGeometry3D geometry in geometries)
			{
				if (geometry.TriangleIndices.Count % 3 != 0)
				{
					throw new ArgumentException("The geometry's triangle indicies need to be divisible by 3");
				}

				int numTriangles = geometry.TriangleIndices.Count / 3;

				for (int cntr = 0; cntr < numTriangles; cntr++)
				{
					FluidTriangle triangle = new FluidTriangle();
					triangle.Index0 = posOffset + geometry.TriangleIndices[cntr * 3];
					triangle.Index1 = posOffset + geometry.TriangleIndices[(cntr * 3) + 1];
					triangle.Index2 = posOffset + geometry.TriangleIndices[(cntr * 3) + 2];
					triangle.AllPoints = allPoints;

					triangles.Add(triangle);
				}

				posOffset += geometry.Positions.Count;
			}

			#endregion

			FluidHull retVal = new FluidHull();
			retVal.Triangles = triangles.ToArray();
			retVal.IsClosedConvex = isClosedConvex;

			//	Exit Function
			return retVal;
		}

		/// <summary>
		/// If this fluid hull is tied to a physics body, then this method must be called from within that body's apply force/torque callback
		/// </summary>
		public void Update()
		{
			#region Check if there are any forces to calculate

			if (Math3D.IsNearZero(this.FluidFlow.LengthSquared) &&
					(this.Body == null ||
						(Math3D.IsNearZero(this.Body.Velocity.LengthSquared) && Math3D.IsNearZero(this.Body.AngularVelocity.LengthSquared))))
			{
				if (_areForcesSet)		//	added this so I don't waste the processor clearing forces on a dead object each frame
				{
					#region Clear Forces

					foreach (FluidTriangle triangle in _triangles)
					{
						triangle.NextFrame(null);
					}

					#endregion
				}
				return;
			}

			_areForcesSet = true;

			#endregion

			if (!_areForcePointsChosen)
			{
				//	First time init
				ChooseForcePoints();
				_areForcePointsChosen = true;
			}

			//	Transform the points to world coords
			Point3D[] worldPoints = GetTrianglePointsWorld();

			foreach (FluidHull.FluidTriangle triangle in this.Triangles)
			{
				triangle.NextFrame(worldPoints);

				Point3D[] forcesAt = triangle.GetForceAt();
				if (forcesAt == null)
				{
					continue;
				}

				Vector3D[] forces = new Vector3D[forcesAt.Length];

				//	Can't use triangle.Normal, because it's still in model coords
				Vector3D normal = Vector3D.CrossProduct(worldPoints[triangle.Index2] - worldPoints[triangle.Index1],
																						worldPoints[triangle.Index0] - worldPoints[triangle.Index1]);

				//double avgNormalLength = triangle.Normal.Length / forcesAt.Length;
				double avgNormalLength = normal.Length / forcesAt.Length;

				for (int cntr = 0; cntr < forcesAt.Length; cntr++)
				{
					#region Sample Point

					//	Calculate the velocity of the triangle at this point
					Vector3D flowAtPoint = this.FluidFlow;

					Vector3D velocityAtPoint = new Vector3D(0, 0, 0);
					if (this.Body != null)
					{
						velocityAtPoint = this.Body.GetVelocityAtPoint(forcesAt[cntr]);
					}

					flowAtPoint -= velocityAtPoint;


					//	If it's convex, only allow forces that flow into the hull
					if (!this.IsClosedConvex || Vector3D.DotProduct(flowAtPoint, normal) < 0)
					{
						//	Scale the normal based on how much of the flow is along it
						forces[cntr] = flowAtPoint.GetProjectedVector(normal);		//	the length returned is from zero to flowAtPoint.Length.  avgNormal's length is ignored, only its direction

						//	Scale the force by the area of the triangle (cross gives area of parallelagram, so taking half)
						//	Also scale by viscocity
						forces[cntr] *= avgNormalLength * .5d * this.FluidViscosity;

						//	Apply the force to the body
						if (this.Body != null && !Math3D.IsNearZero(forces[cntr].LengthSquared))
						{
							this.Body.AddForceAtPoint(forces[cntr], forcesAt[cntr]);
						}
					}


					#endregion
				}

				triangle.SetForces(forces);		//	this is only stored here for debugging, etc (physics doesn't look at it)
			}
		}
		#region ORIG
		//public void Update_ORIG()
		//{
		//    #region Check if there are any forces to calculate

		//    if (Math3D.IsNearZero(this.FluidFlow.LengthSquared) &&
		//            (this.Body == null ||
		//                (Math3D.IsNearZero(this.Body.Velocity.LengthSquared) && Math3D.IsNearZero(this.Body.AngularVelocity.LengthSquared))))
		//    {
		//        if (_areForcesSet)		//	added this so I don't waste the processor clearing forces on a dead object each frame
		//        {
		//            #region Clear Forces

		//            foreach (FluidTriangle triangle in _triangles)
		//            {
		//                triangle.ForceAt = new Point3D();
		//                triangle.Force = new Vector3D();
		//            }

		//            #endregion
		//        }
		//        return;
		//    }

		//    _areForcesSet = true;

		//    #endregion

		//    //	Transform the points to world coords
		//    Point3D[] worldPoints = GetTrianglePointsWorld();
		//    Point3D? bodyPositionWorld = null;
		//    Vector3D? angularVelocityModel = null;

		//    int triangleIndex = 0;
		//    foreach (FluidHull.FluidTriangle triangle in this.Triangles)
		//    {
		//        #region Triangle

		//        //	Store the force location (center of triangle)
		//        //NOTE:  Since this is a rigid body, I should be ok with just applying the force in the center.  If this is modeling a sheet of fabric, 1/3 of the force
		//        //	should be applied to each vertex? - now that I think about it, maybe the center is still ok?
		//        triangle.ForceAt = ((worldPoints[triangle.Index0].ToVector() + worldPoints[triangle.Index1].ToVector() + worldPoints[triangle.Index2].ToVector()) * .333333333d).ToPoint();

		//        //	Calculate the velocity of the triangle at its center (not sure if it would be much more accurate if I calculate for each point, then
		//        //	average.  Or I could calculate the velocity and force at each point, and use 1/3 of the triangle's area?  All of that burns the processor
		//        //	but not sure if it would be any more realistic)
		//        Vector3D flowAtPoint = this.FluidFlow;

		//        Vector3D velocityAtPoint = new Vector3D(0, 0, 0);
		//        if (this.Body != null)
		//        {
		//            velocityAtPoint = this.Body.GetVelocityAtPoint(triangle.ForceAt);
		//        }

		//        flowAtPoint -= velocityAtPoint;

		//        Vector3D triangleDir1 = worldPoints[triangle.Index0] - worldPoints[triangle.Index1];
		//        Vector3D triangleDir2 = worldPoints[triangle.Index2] - worldPoints[triangle.Index1];

		//        Vector3D triangleNormal = Vector3D.CrossProduct(triangleDir2, triangleDir1);

		//        //	Scale the normal based on how much of the flow is along it
		//        triangle.Force = flowAtPoint.GetProjectedVector(triangleNormal);		//	the length returned is from zero to flowVector.Length.  triangleNormal's length is ignored, only its direction

		//        //	Scale the force by the area of the triangle (cross gives area of parallelagram, so taking half)
		//        //	Also scale by viscocity
		//        triangle.Force *= triangleNormal.Length * .5d * this.FluidViscosity;




		//        //triangle.Force = flowAtPoint;




		//        //	Apply the force to the body
		//        if (this.Body != null && !Math3D.IsNearZero(triangle.Force.LengthSquared))
		//        {
		//            this.Body.AddForceAtPoint(triangle.Force, triangle.ForceAt);
		//        }

		//        triangleIndex++;

		//        #endregion
		//    }
		//}
		#endregion

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

		private void ChooseForcePoints()
		{
			const int NUMPOINTSPERSET = 25;
			const int NUMSETS = 8;

			SortedList<int, List<Point3D>[]> pointSets = new SortedList<int, List<Point3D>[]>();

			for (int cntr = 0; cntr < NUMSETS; cntr++)
			{
				//TODO:  This method should return barycentric coords directly
				//	Create random points across the triangles
				SortedList<int, List<Point3D>> points = Math3D.GetRandomPointsOnHull_Structured(_triangles, NUMPOINTSPERSET);

				//	Add these to the sets
				foreach (int triangleIndex in points.Keys)
				{
					if (!pointSets.ContainsKey(triangleIndex))
					{
						pointSets.Add(triangleIndex, new List<Point3D>[NUMSETS]);
					}

					pointSets[triangleIndex][cntr] = points[triangleIndex];
				}
			}

			foreach (int triangleIndex in pointSets.Keys)
			{
				List<Vector[]> localSets = new List<Vector[]>();

				for (int setCntr = 0; setCntr < NUMSETS; setCntr++)
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

				//	Store these sets in this triangle
				this.Triangles[triangleIndex].StoreForcePointSets(localSets);
			}
		}

		#endregion
	}
}
