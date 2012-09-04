//***********************************************************************//
//																		 //
//		- "Talk to me like I'm a 3 year old!" Programming Lessons -		 //
//                                                                       //
//		$Author:		DigiBen			DigiBen@GameTutorials.com		 //
//																		 //
//		$Program:		PolygonCollision								 //
//																		 //
//		$Description:	This demonstrates line and polygon collision.	 //
//																		 //
//		$Date:			7/11/01											 //
//																		 //
//***********************************************************************//

//
//
// This file was build from the Ray Plane Collision tutorial.
// We added 5 new functions to this math file:  
//
// This returns the dot product between 2 vectors
// double Dot(Vector3D vVector1, Vector3D vVector2);
//
// This returns the angle between 2 vectors
// double AngleBetweenVectors(Vector3D Vector1, Vector3D Vector2);
//
// This returns an intersection point of a polygon and a line (assuming intersects the plane)
// Vector3D IntersectionPoint(Vector3D vNormal, Vector3D vLine[], double distance);
//
// This returns true if the intersection point is inside of the polygon
// bool InsidePolygon(Vector3D vIntersection, Vector3D Poly[], long verticeCount);
//
// Use this function to test collision between a line and polygon
// bool IntersectedPolygon(Vector3D vPoly[], Vector3D vLine[], int verticeCount);
//
// These will enable to check if we internet not just the plane of a polygon,
// but the actual polygon itself.  Once the line is outside the permiter, it will fail
// on a collision test.

/////////////////////////////////////////////////////////////////////////////////
//
// * QUICK NOTES * 
//
// WOW!  That is a lot of math for one day.  The good thing about this is that you 
// don't have to code it over again (except when you write tutorials :)  ).
// Now that you have these functions, you can use them and very rarely tweak them.
// This code is probably not the most optimized, but it works pretty good.
// Let us know if you find some good optimizations though.  I am always trying
// to make my code faster in any way possible.  There are a few things I see
// but I didn't want to implement because it would complicate the tutorial.
// 
// So, do you feel comfortable with this stuff?  At least enough to use the functions?
// The next tutorial will take this information and incorporate it into sphere collision.
// Sphere collision is wonderful.  It is easy, and it's very intuitive: If you go in my
// sphere, you collided!  You just need a radius and that's it.  No more complicated math.
// 
// This tutorial should give you enough to write your own simple collision routines.
// One might include making a simple maze and then checking collision with your view
// vector and position against all the maze walls.  This would be incredibly simple.
// just pass in your position and view and test against each polygon in the maze.
// 
// Later you can learn about space partitioning, but with a small maze it won't matter.
// 
// Let's go over the material once more in a brief overview.
// 
// 1) Once you find out that the line and the plane intersect, you need to get the intersection
//    point.  In order to get the intersection point, we needed to learn about the dot product.
//    The basic premise of getting the intersection point is finding the distant from a point on
//    the line, either end is fine, then moving from that point along the lines vector.  But,
//    we can't just directly move across that distance, because that is the distance from the plane,
//    it doesn't mean that it's the actual point of intersection.  Take the case that the line is
//    almost parallel with the plane, but is slightly tilted so it intersects a ways down.  Well,
//    the distance from the plane would be very short, where the distance to the intersection point
//    is considerably longer.  To solve this problem, we divide the negated distance by the dot product
//    of the normal and the lines vector.  This then gives us the correct distant to the intersection point.
//
// 2) Once we find the intersection point, we need to test if that point is inside of the polygon.
//    just because we collide with the plane, doesn't mean that we collided with the polygon.
//	  Planes are infinite, so the polygon could be hundred of coordinate units from us.  To test
//    to see if we are inside of the polygon, we create vectors from the intersection point to
//    each vertex of the polygon.  Then, as we do this, we calculate the angle between the vectors.
//    We create 2 vectors at a time, which then create a triangle.  We only care about the inner angle.
//    After we are finished adding up the angles between each vector of the polygon, if the angles
//    add up to 360 degrees, then the point is inside of the polygon.  We create a function called
//    AngleBetweenVectors() which gives us the angle between 2 vectors in radians.  So the angles
//    need to add up to at least 2 * PI.  To get the angle between 2 vectors we found out that
//    if we use this equation (V . W / || V || * || W || ) which is the dot product between
//    vector V and vector W, divided by the magnitude of vector V multiplied by the magnitude of vector W.
//    We then take the arc Math.CoMath.Sine of that result and it gives us the angle in radians.  If we
//    are working with unit vectors (vectors that are normalized with length of 1) we don't need
//    to do the || V || * || W || part of the equation because it gets canceled out from the dot product.
//
// 3) After we coded those last 2 steps, we put them into a usable function called IntersectedPolygon().
//    It's simple to use, just pass in an array that makes up the polygon, pass in the line array, 
//    then the vertex count of the polygon.  
//
// Let us know at www.GameTutorials.com if this was helpful to you.  Any feedback is welcome .
// 
// 
// Ben Humphrey (DigiBen)
// Game Programmer
// DigiBen@GameTutorials.com
// Co-Web Host of www.GameTutorials.com
//
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using Game.HelperClasses;

namespace Game.Newt.HelperClasses
{
	/// <summary>
	/// NOTE:  A lot of these methods seem to be the same as what is already provided by the framework
	/// </summary>
	public static class Math3D
	{
		#region Enum: RayCastReturn

		public enum RayCastReturn
		{
			AllPoints,
			ClosestToRayOrigin,
			ClosestToRay
		}

		#endregion

		#region Declaration Section

		private const double _180_over_PI = (180 / Math.PI);
		private const double _PI_over_180 = (Math.PI / 180);

		private const float _180_over_PI_FLOAT = (180 / (float)Math.PI);
		private const float _PI_over_180_FLOAT = ((float)Math.PI / 180);

		public static readonly Matrix3D ZeroMatrix = new Matrix3D(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
		public static readonly Vector3D ScaleIdentity = new Vector3D(1, 1, 1);

		public const double Radian360 = 360 * _PI_over_180;

		//public const double NEARZERO = .000001d;
		public const double NEARZERO = .000000001d;

		#endregion

		#region Simple Math

		public static Point3D Add(Point3D p1, Point3D p2)
		{
			return new Point3D(p1.X + p2.X, p1.Y + p2.Y, p1.Z + p2.Z);
		}
		public static Point3D Subtract(Point3D p1, Point3D p2)
		{
			return new Point3D(p1.X - p2.X, p1.Y - p2.Y, p1.Z - p2.Z);
		}

		#endregion

		#region Random

		/// <summary>
		/// Get a random vector between boundry lower and boundry upper
		/// </summary>
		public static Vector3D GetRandomVector(Vector3D boundryLower, Vector3D boundryUpper)
		{
			Vector3D retVal = new Vector3D();

			Random rand = StaticRandom.GetRandomForThread();

			retVal.X = boundryLower.X + (rand.NextDouble() * (boundryUpper.X - boundryLower.X));
			retVal.Y = boundryLower.Y + (rand.NextDouble() * (boundryUpper.Y - boundryLower.Y));
			retVal.Z = boundryLower.Z + (rand.NextDouble() * (boundryUpper.Z - boundryLower.Z));

			return retVal;
		}
		/// <summary>
		/// Get a random vector between maxValue*-1 and maxValue
		/// </summary>
		public static Vector3D GetRandomVector(double maxValue)
		{
			Vector3D retVal = new Vector3D();

			retVal.X = GetNearZeroValue(maxValue);
			retVal.Y = GetNearZeroValue(maxValue);
			retVal.Z = GetNearZeroValue(maxValue);

			return retVal;
		}
		/// <summary>
		/// Gets a random vector with radius between maxRadius*-1 and maxRadius (bounds are spherical,
		/// rather than cube)
		/// </summary>
		public static Vector3D GetRandomVectorSpherical(double maxRadius)
		{
			return GetRandomVectorSpherical(0d, maxRadius);
		}
		/// <summary>
		/// Gets a random vector with radius between maxRadius*-1 and maxRadius (bounds are spherical,
		/// rather than cube).  The radius will never be inside minRadius
		/// </summary>
		/// <remarks>
		/// The sqrt idea came from here:
		/// http://dzindzinovic.blogspot.com/2010/05/xna-random-point-in-circle.html
		/// </remarks>
		public static Vector3D GetRandomVectorSpherical(double minRadius, double maxRadius)
		{
			//	A sqrt, sin and cos  :(           can it be made cheaper?
			double radius = minRadius + ((maxRadius - minRadius) * Math.Sqrt(StaticRandom.NextDouble()));		//	without the square root, there is more chance at the center than the edges

			return GetRandomVectorSphericalShell(radius);
		}
		/// <summary>
		/// Gets a random vector with the radius passed in (bounds are spherical, rather than cube)
		/// </summary>
		public static Vector3D GetRandomVectorSphericalShell(double radius)
		{
			Random rand = StaticRandom.GetRandomForThread();

			double theta = rand.NextDouble() * Math.PI * 2d;

			//	z is cos of phi, which isn't linear.  So the probability is higher that more will be at the poles.  Which means if I want
			//	a linear probability of z, I need to feed the cosine something that will flatten it into a line.  The curve that will do that
			//	is arccos (which basically rotates the cosine wave 90 degrees).  This means that it is undefined for any x outside the range
			//	of -1 to 1.  So I have to shift the random statement to go between -1 to 1, run it through the curve, then shift the result
			//	to go between 0 and pi.
			//double phi = rand.NextDouble() * Math.PI;

			double phi = (rand.NextDouble() * 2d) - 1d;		//	value from -1 to 1
			phi = -Math.Asin(phi) / (Math.PI * .5d);		//	another value from -1 to 1
			phi = (1d + phi) * Math.PI * .5d;		//	from 0 to pi

			double sinPhi = Math.Sin(phi);

			double x = radius * Math.Cos(theta) * sinPhi;
			double y = radius * Math.Sin(theta) * sinPhi;
			double z = radius * Math.Cos(phi);

			return new Vector3D(x, y, z);
		}
		/// <summary>
		/// Gets a random vector with radius between maxRadius*-1 and maxRadius (bounds are spherical,
		/// rather than cube).  Z will always be zero.
		/// </summary>
		public static Vector3D GetRandomVectorSpherical2D(double maxRadius)
		{
			return GetRandomVectorSpherical2D(0d, maxRadius);
		}
		/// <summary>
		/// Gets a random vector with radius between maxRadius*-1 and maxRadius (bounds are spherical,
		/// rather than cube).  The radius will never be inside minRadius.  Z will always be zero.
		/// </summary>
		/// <remarks>
		/// The sqrt idea came from here:
		/// http://dzindzinovic.blogspot.com/2010/05/xna-random-point-in-circle.html
		/// </remarks>
		public static Vector3D GetRandomVectorSpherical2D(double minRadius, double maxRadius)
		{
			double radius = minRadius + ((maxRadius - minRadius) * Math.Sqrt(StaticRandom.NextDouble()));		//	without the square root, there is more chance at the center than the edges

			return GetRandomVectorSphericalShell2D(radius);
		}
		/// <summary>
		/// Gets a random vector with the radius passed in (bounds are spherical, rather than cube).  Z will always be zero.
		/// </summary>
		public static Vector3D GetRandomVectorSphericalShell2D(double radius)
		{
			double angle = StaticRandom.NextDouble() * Math.PI * 2d;

			double x = radius * Math.Cos(angle);
			double y = radius * Math.Sin(angle);

			return new Vector3D(x, y, 0d);
		}

		public static Quaternion GetRandomRotation()
		{
			return new Quaternion(GetRandomVectorSphericalShell(1d), GetNearZeroValue(360d));
		}

		/// <remarks>
		/// Got this here:
		/// http://adamswaab.wordpress.com/2009/12/11/random-point-in-a-triangle-barycentric-coordinates/
		/// </remarks>
		public static Point3D GetRandomPointInTriangle(Point3D a, Point3D b, Point3D c)
		{
			Vector3D ab = b - a;
			Vector3D ac = c - a;

			Random rand = StaticRandom.GetRandomForThread();
			double percentAB = rand.NextDouble();		//	% along ab
			double percentAC = rand.NextDouble();		//	% along ac

			if (percentAB + percentAC >= 1d)
			{
				//	Mirror back onto the triangle (otherwise this would make a parallelogram)
				percentAB = 1d - percentAB;
				percentAC = 1d - percentAC;
			}

			//	Now add the two weighted vectors to a
			return a + ((ab * percentAB) + (ac * percentAC));
		}
		public static Point3D GetRandomPointInTriangle(ITriangle triangle)
		{
			//Vector3D ab = b - a;
			//Vector3D ac = c - a;
			Vector3D ab = triangle.Point1 - triangle.Point0;
			Vector3D ac = triangle.Point2 - triangle.Point0;

			Random rand = StaticRandom.GetRandomForThread();
			double percentAB = rand.NextDouble();		//	% along ab
			double percentAC = rand.NextDouble();		//	% along ac

			if (percentAB + percentAC >= 1d)
			{
				//	Mirror back onto the triangle (otherwise this would make a parallelogram)
				percentAB = 1d - percentAB;
				percentAC = 1d - percentAC;
			}

			//	Now add the two weighted vectors to a
			return triangle.Point0 + ((ab * percentAB) + (ac * percentAC));
		}

		/// <summary>
		/// This returns a list of points evenly distributed across the surface of the hull passed in
		/// </summary>
		/// <remarks>
		/// Note that the triangles within hull don't need to actually be a continuous hull.  They can be a scattered mess of triangles, and
		/// the returned points will still be constrained to the surface of those triangles (evenly distributed across those triangles)
		/// </remarks>
		public static Point3D[] GetRandomPointsOnHull(ITriangle[] hull, int numPoints)
		{
			//	Calculate each triangle's % of the total area of the hull (sorted smallest to largest)
			int[] trianglePointers;
			double[] triangleSizes;
			GetRandomPointsOnHullSprtSizes(out trianglePointers, out triangleSizes, hull);

			Random rand = StaticRandom.GetRandomForThread();

			Point3D[] retVal = new Point3D[numPoints];

			for (int cntr = 0; cntr < numPoints; cntr++)
			{
				//	Pick a random location on the hull (a percent of the hull's size from 0% to 100%)
				double percent = rand.NextDouble();

				//	Find the triangle that straddles this percent
				int index = GetRandomPointsOnHullSprtFindTriangle(trianglePointers, triangleSizes, percent);

				//	Create a point somewhere on this triangle
				retVal[cntr] = GetRandomPointInTriangle(hull[index]);
			}

			//	Exit Function
			return retVal;
		}
		/// <summary>
		/// Instead of just returning the list of points, it returns which triangle contains which points
		/// </summary>
		/// <remarks>
		/// The code is copied from the other overload for efficiency reasons
		/// This is a little more expensive than the other overload
		/// </remarks>
		/// <returns>
		/// Key=index into hull
		/// Value=points belonging to that triangle
		/// </returns>
		public static SortedList<int, List<Point3D>> GetRandomPointsOnHull_Structured(ITriangle[] hull, int numPoints)
		{
			//	Calculate each triangle's % of the total area of the hull (sorted smallest to largest)
			int[] trianglePointers;
			double[] triangleSizes;
			GetRandomPointsOnHullSprtSizes(out trianglePointers, out triangleSizes, hull);

			Random rand = StaticRandom.GetRandomForThread();

			SortedList<int, List<Point3D>> retVal = new SortedList<int, List<Point3D>>();

			for (int cntr = 0; cntr < numPoints; cntr++)
			{
				//	Pick a random location on the hull (a percent of the hull's size from 0% to 100%)
				double percent = rand.NextDouble();

				//	Find the triangle that straddles this percent
				int index = GetRandomPointsOnHullSprtFindTriangle(trianglePointers, triangleSizes, percent);

				if (!retVal.ContainsKey(index))
				{
					retVal.Add(index, new List<Point3D>());
				}

				//	Create a point somewhere on this triangle
				retVal[index].Add(GetRandomPointInTriangle(hull[index]));
			}

			//	Exit Function
			return retVal;
		}
		private static void GetRandomPointsOnHullSprtSizes(out int[] trianglePointers, out double[] triangleSizes, ITriangle[] hull)
		{
			trianglePointers = UtilityHelper.GetIncrementingArray(hull.Length);
			triangleSizes = new double[hull.Length];

			//	Get the size of each triangle
			for (int cntr = 0; cntr < hull.Length; cntr++)
			{
				triangleSizes[cntr] = hull[cntr].NormalLength;
			}

			//	Sort them (I'd sort descending if I could.  That would make the find method easier to read, but the call to reverse
			//	is an unnecessary expense)
			Array.Sort(triangleSizes, trianglePointers);

			//	Normalize them so the sum is 1.  Note that after this, each item in sizes will be that item's percent of the whole
			double totalSize = triangleSizes.Sum();
			for (int cntr = 0; cntr < triangleSizes.Length; cntr++)
			{
				triangleSizes[cntr] = triangleSizes[cntr] / totalSize;
			}
		}
		private static int GetRandomPointsOnHullSprtFindTriangle(int[] trianglePointers, double[] triangleSizes, double percent)
		{
			double accumSize = 1d;

			//	Find the triangle that is this occupying this percent of the total size (walking backward will cut down on
			//	the number of triangles that need to be searched through)
			for (int cntr = triangleSizes.Length - 1; cntr > 0; cntr--)
			{
				if (accumSize - triangleSizes[cntr] < percent)
				{
					return trianglePointers[cntr];
				}

				accumSize -= triangleSizes[cntr];
			}

			//	This should only happen if the requested percent is zero
			return trianglePointers[0];
		}

		/// <summary>
		/// Gets a value between -maxValue and maxValue
		/// </summary>
		public static double GetNearZeroValue(double maxValue)
		{
			Random rand = StaticRandom.GetRandomForThread();

			double retVal = rand.NextDouble() * maxValue;

			if (rand.Next(0, 2) == 1)
			{
				retVal *= -1d;
			}

			return retVal;
		}
		/// <summary>
		/// This gets a value between minValue and maxValue, and has a 50/50 chance of negating that
		/// </summary>
		public static double GetNearZeroValue(double minValue, double maxValue)
		{
			Random rand = StaticRandom.GetRandomForThread();

			double retVal = minValue + (rand.NextDouble() * (maxValue - minValue));

			if (rand.Next(0, 2) == 1)
			{
				retVal *= -1d;
			}

			return retVal;
		}

		/// <summary>
		/// This function will pick an arbitrary orthogonal to the vector passed in.  This will only be useful if you are going
		/// to rotate 180
		/// </summary>
		public static Vector3D GetArbitraryOrhonganal(Vector3D vector)
		{
			//	Clone the vector passed in
			Vector3D retVal = new Vector3D(vector.X, vector.Y, vector.Z);

			//	Make sure that none of the values are equal to zero.
			if (retVal.X == 0) retVal.X = 0.000000001d;
			if (retVal.Y == 0) retVal.Y = 0.000000001d;
			if (retVal.Z == 0) retVal.Z = 0.000000001d;

			//	Figure out the orthogonal X and Y slopes
			double orthM = (retVal.X * -1) / retVal.Y;
			double orthN = (retVal.Y * -1) / retVal.Z;

			//	When calculating the new coords, I will default Y to 1, and find an X and Z that satisfy that.  I will go ahead and reuse the retVal
			retVal.Y = 1;
			retVal.X = 1 / orthM;
			retVal.Z = orthN;

			//	Exit Function
			return retVal;
		}

		#endregion

		#region Misc

		public static bool IsNearZero(double testValue)
		{
			return Math.Abs(testValue) <= NEARZERO;
		}
		public static bool IsNearZero(Vector3D testVect)
		{
			return Math.Abs(testVect.X) <= NEARZERO && Math.Abs(testVect.Y) <= NEARZERO && Math.Abs(testVect.Z) <= NEARZERO;
		}
		public static bool IsNearZero(Point3D testPoint)
		{
			return Math.Abs(testPoint.X) <= NEARZERO && Math.Abs(testPoint.Y) <= NEARZERO && Math.Abs(testPoint.Z) <= NEARZERO;
		}
		public static bool IsNearValue(double testValue, double compareTo)
		{
			return testValue >= compareTo - NEARZERO && testValue <= compareTo + NEARZERO;
		}
		public static bool IsNearValue(Vector3D testVect, Vector3D compareTo)
		{
			return testVect.X >= compareTo.X - NEARZERO && testVect.X <= compareTo.X + NEARZERO &&
						testVect.Y >= compareTo.Y - NEARZERO && testVect.Y <= compareTo.Y + NEARZERO &&
						testVect.Z >= compareTo.Z - NEARZERO && testVect.Z <= compareTo.Z + NEARZERO;
		}
		public static bool IsNearValue(Point3D testPoint, Point3D compareTo)
		{
			return testPoint.X >= compareTo.X - NEARZERO && testPoint.X <= compareTo.X + NEARZERO &&
						testPoint.Y >= compareTo.Y - NEARZERO && testPoint.Y <= compareTo.Y + NEARZERO &&
						testPoint.Z >= compareTo.Z - NEARZERO && testPoint.Z <= compareTo.Z + NEARZERO;
		}

		public static bool IsDivisible(double larger, double smaller)
		{
			if (Math3D.IsNearZero(smaller))
			{
				//	Divide by zero.  Nothing is divisible by zero, not even zero.  (I looked up "is zero divisible by zero", and got very
				//	technical reasons why it's not.  It would be cool to be able to view the world the way math people do.  Visualizing
				//	complex equations, etc)
				return false;
			}

			//	Divide the larger by the smaller.  If the result is an integer (or very close to an integer), then they are divisible
			double division = larger / smaller;
			double divisionInt = Math.Round(division);

			return Math3D.IsNearValue(division, divisionInt);
		}

		/// <summary>
		/// This returns whether the point is inside all the planes (the triangles don't define finite triangles, but whole planes)
		/// NOTE: Make sure the normals point outward, or there will be odd results
		/// </summary>
		/// <remarks>
		/// This is a reworked copy of QuickHull3D.GetOutsideSet, which was inspired by:
		/// http://www.gamedev.net/topic/106765-determining-if-a-point-is-in-front-of-or-behind-a-plane/
		/// </remarks>
		public static bool IsInside(List<ITriangle> planes, Point3D testPoint)
		{
			for (int cntr = 0; cntr < planes.Count; cntr++)
			{
				ITriangle plane = planes[cntr];

				//	Compute D, using a arbitrary point P, that lies on the plane: D = - (Nx*Px + Ny*Py + Nz*Pz); Don't forget the inversion !
				double d = -((plane.NormalUnit.X * plane.Point0.X) + (plane.NormalUnit.Y * plane.Point0.Y) + (plane.NormalUnit.Z * plane.Point0.Z));

				//	You can test a point (T) with respect to the plane using the plane equation: res = Nx*Tx + Ny*Ty + Nz*Tz + D
				double res = (plane.NormalUnit.X * testPoint.X) + (plane.NormalUnit.Y * testPoint.Y) + (plane.NormalUnit.Z * testPoint.Z) + d;

				if (res >= 0)		//	greater than zero is outside the plane
				{
					return false;
				}
			}

			return true;
		}
		public static bool IsInside(Point3D min, Point3D max, Point3D testPoint)
		{
			if (testPoint.X < min.X)
			{
				return false;
			}
			else if (testPoint.X > max.X)
			{
				return false;
			}
			else if (testPoint.Y < min.Y)
			{
				return false;
			}
			else if (testPoint.Y > max.Y)
			{
				return false;
			}
			else if (testPoint.Z < min.Z)
			{
				return false;
			}
			else if (testPoint.Z > max.Z)
			{
				return false;
			}

			return true;
		}
		public static bool IsInside(Vector3D min, Vector3D max, Vector3D testPoint)
		{
			if (testPoint.X < min.X)
			{
				return false;
			}
			else if (testPoint.X > max.X)
			{
				return false;
			}
			else if (testPoint.Y < min.Y)
			{
				return false;
			}
			else if (testPoint.Y > max.Y)
			{
				return false;
			}
			else if (testPoint.Z < min.Z)
			{
				return false;
			}
			else if (testPoint.Z > max.Z)
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// This returns true if any part of the sphere intersects any part of the AABB
		/// (also returns true if one is inside the other)
		/// </summary>
		/// <remarks>
		/// Got this here:
		/// http://stackoverflow.com/questions/4578967/cube-sphere-intersection-test
		/// 
		/// Which referenced:
		/// http://www.ics.uci.edu/~arvo/code/BoxSphereIntersect.c
		/// </remarks>
		public static bool Intersects(Point3D min, Point3D max, Point3D center, double radius)
		{
			double r2 = radius * radius;
			double dmin = 0d;

			if (center.X < min.X)
			{
				dmin += (center.X - min.X) * (center.X - min.X);
			}
			else if (center.X > max.X)
			{
				dmin += (center.X - max.X) * (center.X - max.X);
			}

			if (center.Y < min.Y)
			{
				dmin += (center.Y - min.Y) * (center.Y - min.Y);
			}
			else if (center.Y > max.Y)
			{
				dmin += (center.Y - max.Y) * (center.Y - max.Y);
			}

			if (center.Z < min.Z)
			{
				dmin += (center.Z - min.Z) * (center.Z - min.Z);
			}
			else if (center.Z > max.Z)
			{
				dmin += (center.Z - max.Z) * (center.Z - max.Z);
			}

			return dmin <= r2;
		}
		/// <summary>
		/// This returns true if any part of AABB1 intersects any part of the AABB2
		/// (also returns true if one is inside the other)
		/// </summary>
		public static bool Intersects(Point3D min1, Point3D max1, Point3D min2, Point3D max2)
		{
			if (min1.X > max2.X || min2.X > max1.X)
			{
				return false;
			}
			else if (min1.Y > max2.Y || min2.Y > max1.Y)
			{
				return false;
			}
			else if (min1.Z > max2.Z || min2.Z > max1.Z)
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// This returns the location of the point relative to the triangle
		/// </summary>
		/// <remarks>
		/// The term Barycentric for a triangle seems to be 3 positions, so I'm not sure if this method is named right
		/// 
		/// This is useful if you want to store a point's location relative to a triangle when that triangle will move all
		/// around.  You don't need to know the transform used to move that triangle, just the triangle's final position
		/// 
		/// This is also useful to see if the point is inside the triangle.  If x or y is negative, or they add up to > 1, then it
		/// is outside the triangle:
		///		if x is zero, it's on the 0_1 edge
		///		if y is zero, it's on the 0_2 edge
		///		if x+y is one, it's on the 1_2 edge
		/// 
		/// Got this here (good flash visualization too):
		/// http://www.blackpawn.com/texts/pointinpoly/default.html
		/// </remarks>
		/// <returns>
		/// X = % along the line triangle.P0 to triangle.P1
		/// Y = % along the line triangle.P0 to triangle.P2
		/// </returns>
		public static Vector ToBarycentric(ITriangle triangle, Point3D point)
		{
			return ToBarycentric(triangle.Point0, triangle.Point1, triangle.Point2, point);
		}
		public static Vector ToBarycentric(Point3D p0, Point3D p1, Point3D p2, Point3D testPoint)
		{
			// Compute vectors        
			Vector3D v0 = p2 - p0;
			Vector3D v1 = p1 - p0;
			Vector3D v2 = testPoint - p0;

			// Compute dot products
			double dot00 = Vector3D.DotProduct(v0, v0);
			double dot01 = Vector3D.DotProduct(v0, v1);
			double dot02 = Vector3D.DotProduct(v0, v2);
			double dot11 = Vector3D.DotProduct(v1, v1);
			double dot12 = Vector3D.DotProduct(v1, v2);

			// Compute barycentric coordinates
			double invDenom = 1d / (dot00 * dot11 - dot01 * dot01);
			double u = (dot11 * dot02 - dot01 * dot12) * invDenom;
			double v = (dot00 * dot12 - dot01 * dot02) * invDenom;

			//	Exit Function
			return new Vector(u, v);
		}
		/// <summary>
		/// This projects the barycentric back into cartesian coords
		/// </summary>
		/// <param name="bary">Save me Bary!!! (Misfits)</param>
		public static Point3D FromBarycentric(ITriangle triangle, Vector bary)
		{
			return FromBarycentric(triangle.Point0, triangle.Point1, triangle.Point2, bary);
		}
		public static Point3D FromBarycentric(Point3D p0, Point3D p1, Point3D p2, Vector bary)
		{
			Vector3D line01 = p1 - p0;
			Vector3D line02 = p2 - p0;

			return p0 + (line01 * bary.X) + (line02 * bary.Y);
		}

		public static double GetAspectRatio(Size size)
		{
			return size.Width / size.Height;
		}

		public static double DegreesToRadians(double degrees)
		{
			return degrees * _PI_over_180;
		}
		public static float DegreesToRadians(float degrees)
		{
			return degrees * _PI_over_180_FLOAT;
		}

		public static double RadiansToDegrees(double radians)
		{
			return radians * _180_over_PI;
		}
		public static float RadiansToDegrees(float radians)
		{
			return radians * _180_over_PI_FLOAT;
		}

		/// <summary>
		/// This function will rotate the vector around any arbitrary axis
		/// </summary>
		/// <param name="vector">The vector to get rotated</param>
		/// <param name="rotateAround">Any vector to rotate around</param>
		/// <param name="radians">How far to rotate</param>
		public static Vector3D RotateAroundAxis(Vector3D vector, Vector3D rotateAround, double radians)
		{
			//	Create a quaternion that represents the axis and angle passed in
			Quaternion rotationQuat = new Quaternion(rotateAround, RadiansToDegrees(radians));

			Matrix3D matrix = new Matrix3D();
			matrix.Rotate(rotationQuat);

			//	Get a vector that represents me rotated by the quaternion
			Vector3D retVal = matrix.Transform(vector);

			//	Exit Function
			return retVal;
		}

		/// <summary>
		/// This overload returns a quaternion
		/// </summary>
		/// <remarks>
		/// I decided to copy the other method, so it's a bit more optimized
		/// </remarks>
		public static Quaternion GetRotation(Vector3D from, Vector3D to)
		{
			//	Grab the angle
			double angle = Vector3D.AngleBetween(from, to);
			if (Double.IsNaN(angle))
			{
				return Quaternion.Identity;
			}

			//	I need to pull the cross product from me to the vector passed in
			Vector3D axis = Vector3D.CrossProduct(from, to);

			//	If the cross product is zero, then there are two possibilities.  The vectors point in the same direction, or opposite directions.
			if (axis.IsZero())
			{
				//	If I am here, then the angle will either be 0 or 180.
				if (angle == 0)
				{
					//	The vectors sit on top of each other.  I will set the orthoganal to an arbitrary value, and return zero for the radians
					return Quaternion.Identity;
				}
				else
				{
					//	The vectors are pointing directly away from each other, so I will need to be more careful when I create my orthoganal.
					axis = GetArbitraryOrhonganal(from);
				}
			}

			//	Exit Function
			return new Quaternion(axis, angle);
		}
		/// <summary>
		/// This returns how much from should be rotated (and through what axis) to line up with to
		/// NOTE:  To rotate a vector pair (standard and orthogonal), see DoubleVector.GetAngleAroundAxis)
		/// </summary>
		public static void GetRotation(out Vector3D axis, out double radians, Vector3D from, Vector3D to)
		{
			//	Grab the angle
			radians = DegreesToRadians(Vector3D.AngleBetween(from, to));
			if (Double.IsNaN(radians))
			{
				radians = 0;
			}

			//	I need to pull the cross product from me to the vector passed in
			axis = Vector3D.CrossProduct(from, to);

			//	If the cross product is zero, then there are two possibilities.  The vectors point in the same direction, or opposite directions.
			if (axis.IsZero())
			{
				//	If I am here, then the angle will either be 0 or PI.
				if (radians == 0)
				{
					//	The vectors sit on top of each other.  I will set the orthoganal to an arbitrary value, and return zero for the radians
					axis.X = 1;
					radians = 0;
				}
				else
				{
					//	The vectors are pointing directly away from each other, so I will need to be more careful when I create my orthoganal.
					axis = GetArbitraryOrhonganal(from);
				}
			}

			//rotationAxis.BecomeUnitVector();		//	It would be nice to be tidy, but not nessassary, and I don't want slow code
		}

		// This came from Game.Orig.Math3D.TorqueBall
		public static void SplitForceIntoTranslationAndTorque(out Vector3D translationForce, out Vector3D torque, Vector3D offsetFromCenterMass, Vector3D force)
		{
			//	Torque is how much of the force is applied perpendicular to the radius
			torque = Vector3D.CrossProduct(offsetFromCenterMass, force);

			//	I'm still not convinced this is totally right, but none of the articles I've read seem to do anything different
			translationForce = force;
		}

		#endregion

		#region Plane/Line Intersections

		//TODO: Rework the methods to take in triangles as plane

		/// <summary>
		/// This returns a point along the line that is the shortest distance to the test point
		/// NOTE:  The line passed in is assumed to be infinite, not a line segment
		/// </summary>
		/// <param name="pointOnLine">Any arbitrary point along the line</param>
		/// <param name="lineDirection">The direction of the line (slope in x,y,z)</param>
		/// <param name="testPoint">The point that is not on the line</param>
		public static Point3D GetNearestPointAlongLine(Point3D pointOnLine, Vector3D lineDirection, Point3D testPoint)
		{
			Vector3D dirToPoint = testPoint - pointOnLine;

			double dot1 = Vector3D.DotProduct(dirToPoint, lineDirection);
			double dot2 = Vector3D.DotProduct(lineDirection, lineDirection);
			double ratio = dot1 / dot2;

			Point3D retVal = pointOnLine + (ratio * lineDirection);

			return retVal;
		}
		/// <summary>
		/// This is a wrapper to GetNearestPointAlongLine that just returns the distance
		/// </summary>
		public static double GetClosestDistanceBetweenPointAndLine(Point3D pointOnLine, Vector3D lineDirection, Point3D testPoint)
		{
			return (testPoint - GetNearestPointAlongLine(pointOnLine, lineDirection, testPoint)).Length;
		}

		/// <summary>
		/// This returns the distance beween two skew lines at their closest point
		/// </summary>
		/// <remarks>
		/// http://2000clicks.com/mathhelp/GeometryPointsAndLines3D.aspx
		/// </remarks>
		public static double GetClosestDistanceBetweenLines(Point3D point1, Vector3D dir1, Point3D point2, Vector3D dir2)
		{
			//g = (a-c) · (b×d) / |b×d|
			//dist = (a - c) dot (b cross d).ToUnit
			//dist = (point1 - point2) dot (dir1 cross dir2).ToUnit

			//TODO: Detect if they are parallel and return the distance

			Vector3D cross1_2 = Vector3D.CrossProduct(dir1, dir2).ToUnit();
			Vector3D sub1_2 = point1 - point2;

			double retVal = Vector3D.DotProduct(sub1_2, cross1_2);

			return retVal;
		}

		/// <summary>
		/// Calculates the intersection line segment between 2 lines (not segments).
		/// Returns false if no solution can be found.
		/// </summary>
		/// <remarks>
		/// Got this here:
		/// http://local.wasp.uwa.edu.au/~pbourke/geometry/lineline3d/calclineline.cs
		/// 
		/// Which was ported from the C algorithm of Paul Bourke:
		/// http://local.wasp.uwa.edu.au/~pbourke/geometry/lineline3d/
		/// </remarks>
		public static bool GetClosestPointsBetweenLines(out Point3D? resultPoint1, out Point3D? resultPoint2, Point3D point1, Vector3D dir1, Point3D point2, Vector3D dir2)
		{
			return GetClosestPointsBetweenLines(out resultPoint1, out resultPoint2, point1, point1 + dir1, point2, point2 + dir2);
		}
		public static bool GetClosestPointsBetweenLines(out Point3D? resultPoint1, out Point3D? resultPoint2, Point3D line1Point1, Point3D line1Point2, Point3D line2Point1, Point3D line2Point2)
		{
			resultPoint1 = null;
			resultPoint2 = null;

			Point3D p1 = line1Point1;
			Point3D p2 = line1Point2;
			Point3D p3 = line2Point1;
			Point3D p4 = line2Point2;
			Vector3D p13 = p1 - p3;
			Vector3D p43 = p4 - p3;

			//if (IsNearZero(p43.LengthSquared))
			//{
			//    return false;
			//}

			Vector3D p21 = p2 - p1;
			//if (IsNearZero(p21.LengthSquared))
			//{
			//    return false;
			//}

			double d1343 = (p13.X * p43.X) + (p13.Y * p43.Y) + (p13.Z * p43.Z);
			double d4321 = (p43.X * p21.X) + (p43.Y * p21.Y) + (p43.Z * p21.Z);
			double d1321 = (p13.X * p21.X) + (p13.Y * p21.Y) + (p13.Z * p21.Z);
			double d4343 = (p43.X * p43.X) + (p43.Y * p43.Y) + (p43.Z * p43.Z);
			double d2121 = (p21.X * p21.X) + (p21.Y * p21.Y) + (p21.Z * p21.Z);

			double denom = (d2121 * d4343) - (d4321 * d4321);
			//if (IsNearZero(denom))
			//{
			//    return false;
			//}
			double numer = (d1343 * d4321) - (d1321 * d4343);

			double mua = numer / denom;
			if (double.IsNaN(mua))
			{
				return false;
			}

			double mub = (d1343 + d4321 * (mua)) / d4343;

			resultPoint1 = new Point3D(p1.X + mua * p21.X, p1.Y + mua * p21.Y, p1.Z + mua * p21.Z);
			resultPoint2 = new Point3D(p3.X + mub * p43.X, p3.Y + mub * p43.Y, p3.Z + mub * p43.Z);

			if (double.IsNaN(resultPoint1.Value.X) || double.IsNaN(resultPoint1.Value.Y) || double.IsNaN(resultPoint1.Value.Z) ||
				double.IsNaN(resultPoint2.Value.X) || double.IsNaN(resultPoint2.Value.Y) || double.IsNaN(resultPoint2.Value.Z))
			{
				return false;
			}
			else
			{
				return true;
			}
		}

		/// <summary>
		/// This will figure out the nearest points between a circle and line
		/// </summary>
		/// <remarks>
		/// circlePoints will be the nearest point on the circle to the line, and linePoints will hold the closest point on the line to
		/// the corresponding element of circlePoints
		/// 
		/// The only time false is returned is if the line is perpendicular to the circle and goes through the center of the circle (in
		/// that case, linePoints will hold the circle's center point
		/// 
		/// Most of the time, only one output point will be returned, but there are some cases where two are returned
		/// 
		/// If onlyReturnSinglePoint is true, then the arrays will never be larger than one (the point in linePoints that is closest to
		/// pointOnLine is chosen)
		/// </remarks>
		public static bool GetClosestPointsBetweenLineCircle(out Point3D[] circlePoints, out Point3D[] linePoints, ITriangle circlePlane, Point3D circleCenter, double circleRadius, Point3D pointOnLine, Vector3D lineDirection, RayCastReturn returnWhich)
		{
			//	There are too many loose variables, so package them up
			CircleLineArgs args = new CircleLineArgs()
			{
				CirclePlane = circlePlane,
				CircleCenter = circleCenter,
				CircleRadius = circleRadius,
				PointOnLine = pointOnLine,
				LineDirection = lineDirection
			};

			//	Call the overload
			bool retVal = GetClosestPointsBetweenLineCircle(out circlePoints, out linePoints, args);
			if (returnWhich == RayCastReturn.AllPoints || !retVal || circlePoints.Length == 1)
			{
				return retVal;
			}

			switch (returnWhich)
			{
				case RayCastReturn.ClosestToRay:
					GetClosestPointsBetweenLineCircleSprtClosest_CircleLine(ref circlePoints, ref linePoints, pointOnLine);
					break;

				case RayCastReturn.ClosestToRayOrigin:
					GetClosestPointsBetweenLineCircleSprtClosest_RayOrigin(ref circlePoints, ref linePoints, pointOnLine);
					break;

				default:
					throw new ApplicationException("Unexpected RayCastReturn: " + returnWhich.ToString());
			}

			return true;

			#region OLD

			//#region Find closest point

			////	There is more than one point, and they want a single point
			//double minDistance = double.MaxValue;
			//int minIndex = -1;

			//for (int cntr = 0; cntr < circlePoints.Length; cntr++)
			//{
			//    double distance = (linePoints[cntr] - pointOnLine).Length;

			//    if (distance < minDistance)
			//    {
			//        minDistance = distance;
			//        minIndex = cntr;
			//    }
			//}

			//if (minIndex < 0)
			//{
			//    throw new ApplicationException("Should always find a closest point");
			//}

			//#endregion

			////	Return only the closest point
			//circlePoints = new Point3D[] { circlePoints[minIndex] };
			//linePoints = new Point3D[] { linePoints[minIndex] };
			//return true;

			#endregion
		}

		public static bool GetClosestPointsBetweenLineCylinder(out Point3D[] cylinderPoints, out Point3D[] linePoints, Point3D pointOnAxis, Vector3D axisDirection, double radius, Point3D pointOnLine, Vector3D lineDirection, RayCastReturn returnWhich)
		{
			//	Get the shortest point between the cylinder's axis and the line
			Point3D? nearestAxisPoint, nearestLinePoint;
			if (!GetClosestPointsBetweenLines(out nearestAxisPoint, out nearestLinePoint, pointOnAxis, axisDirection, pointOnLine, lineDirection))
			{
				//	The axis and line are parallel
				cylinderPoints = null;
				linePoints = null;
				return false;
			}

			Vector3D nearestLine = nearestLinePoint.Value - nearestAxisPoint.Value;
			double nearestDistance = nearestLine.Length;

			if (nearestDistance >= radius)
			{
				//	Sitting outside the cylinder, so just project the line to the cylinder wall
				cylinderPoints = new Point3D[] { nearestAxisPoint.Value + (nearestLine.ToUnit() * radius) };
				linePoints = new Point3D[] { nearestLinePoint.Value };
				return true;
			}

			//	The rest of this function is for a line intersect inside the cylinder (there's always two intersect points)

			//	Make a plane that the circle sits in (this is used by code shared with the circle/line intersect)
			//NOTE: The plane is using nearestAxisPoint, and not the arbitrary point that was passed in (this makes later logic easier)
			Vector3D circlePlaneLine1 = IsNearZero(nearestDistance) ? GetArbitraryOrhonganal(axisDirection) : nearestLine;
			Vector3D circlePlaneLine2 = Vector3D.CrossProduct(axisDirection, circlePlaneLine1);
			ITriangle circlePlane = new Triangle(nearestAxisPoint.Value, nearestAxisPoint.Value + circlePlaneLine1, nearestAxisPoint.Value + circlePlaneLine2);

			CircleLineArgs args = new CircleLineArgs()
			{
				CircleCenter = nearestAxisPoint.Value,
				CirclePlane = circlePlane,
				CircleRadius = radius,
				PointOnLine = pointOnLine,
				LineDirection = lineDirection
			};

			CirclePlaneIntersectProps intersectArgs = GetClosestPointsBetweenLineCylinderSprtPlaneIntersect(args, nearestLinePoint.Value, nearestLine, nearestDistance);

			GetClosestPointsBetweenLineCylinderSprtFinish(out cylinderPoints, out linePoints, args, intersectArgs);

			switch (returnWhich)
			{
				case RayCastReturn.AllPoints:
					//	Nothing more to do
					break;

				case RayCastReturn.ClosestToRay:
					GetClosestPointsBetweenLineCircleSprtClosest_CircleLine(ref cylinderPoints, ref linePoints, pointOnLine);
					break;

				case RayCastReturn.ClosestToRayOrigin:
					GetClosestPointsBetweenLineCircleSprtClosest_RayOrigin(ref cylinderPoints, ref linePoints, pointOnLine);
					break;

				default:
					throw new ApplicationException("Unknown RayCastReturn: " + returnWhich.ToString());
			}

			return true;
		}

		public static void GetClosestPointsBetweenLineSphere(out Point3D[] spherePoints, out Point3D[] linePoints, Point3D centerPoint, double radius, Point3D pointOnLine, Vector3D lineDirection, RayCastReturn returnWhich)
		{
			//	Get the shortest point between the sphere's center and the line
			Point3D nearestLinePoint = GetNearestPointAlongLine(pointOnLine, lineDirection, centerPoint);

			Vector3D nearestLine = nearestLinePoint - centerPoint;
			double nearestDistance = nearestLine.Length;

			if (nearestDistance >= radius)
			{
				//	Sitting outside the sphere, so just project the line to the sphere wall
				spherePoints = new Point3D[] { centerPoint + (nearestLine.ToUnit() * radius) };
				linePoints = new Point3D[] { nearestLinePoint };
				return;
			}

			//	The rest of this function is for a line intersect inside the sphere (there's always two intersect points)

			//	Make a plane that the circle sits in (this is used by code shared with the circel/line intersect)
			//NOTE: The plane is oriented along the shortest path line
			Vector3D circlePlaneLine1 = IsNearZero(nearestDistance) ? new Vector3D(1, 0, 0) : nearestLine;
			Vector3D circlePlaneLine2 = lineDirection;
			ITriangle circlePlane = new Triangle(centerPoint, centerPoint + circlePlaneLine1, centerPoint + circlePlaneLine2);

			CircleLineArgs args = new CircleLineArgs()
			{
				CircleCenter = centerPoint,
				CirclePlane = circlePlane,
				CircleRadius = radius,
				PointOnLine = pointOnLine,
				LineDirection = lineDirection
			};

			CirclePlaneIntersectProps intersectArgs = new CirclePlaneIntersectProps()
			{
				PointOnLine = pointOnLine,
				LineDirection = lineDirection,
				NearestToCenter = nearestLinePoint,
				CenterToNearest = nearestLine,
				CenterToNearestLength = nearestDistance,
				IsInsideCircle = true
			};

			//	Get the circle intersects (since the line is on the circle's plane, this is the final answer)
			GetClosestPointsBetweenLineCircleSprtInsidePerps(out spherePoints, out linePoints, args, intersectArgs);

			switch (returnWhich)
			{
				case RayCastReturn.AllPoints:
					//	Nothing more to do
					break;

				case RayCastReturn.ClosestToRay:
					GetClosestPointsBetweenLineCircleSprtClosest_CircleLine(ref spherePoints, ref linePoints, pointOnLine);
					break;

				case RayCastReturn.ClosestToRayOrigin:
					GetClosestPointsBetweenLineCircleSprtClosest_RayOrigin(ref spherePoints, ref linePoints, pointOnLine);
					break;

				default:
					throw new ApplicationException("Unknown RayCastReturn: " + returnWhich.ToString());
			}
		}

		/// <summary>
		/// This finds the intersection point of two lines.  If they are parallel or skew, null is returned
		/// </summary>
		/// <remarks>
		/// Got this here:
		/// http://stackoverflow.com/questions/2316490/the-algorithm-to-find-the-point-of-intersection-of-two-3d-line-segment
		/// 
		/// Which references here:
		/// http://mathforum.org/library/drmath/view/62814.html
		/// 
		/// Thanks for writing to Doctor Math.
		/// 
		/// Let's try this with vector algebra. First write the two equations like 
		/// this.
		/// 
		///   L1 = P1 + a V1
		/// 
		///   L2 = P2 + b V2
		/// 
		/// P1 and P2 are points on each line. V1 and V2 are the direction vectors 
		/// for each line.
		/// 
		/// If we assume that the lines intersect, we can look for the point on L1 
		/// that satisfies the equation for L2. This gives us this equation to 
		/// solve.
		/// 
		///   P1 + a V1 = P2 + b V2
		/// 
		/// Now rewrite it like this.
		/// 
		///   a V1 = (P2 - P1) + b V2
		/// 
		/// Now take the cross product of each side with V2. This will make the 
		/// term with 'b' drop out.
		/// 
		///   a (V1 X V2) = (P2 - P1) X V2
		/// 
		/// If the lines intersect at a single point, then the resultant vectors 
		/// on each side of this equation must be parallel, and the left side must 
		/// not be the zero vector. We should check to make sure that this is 
		/// true. Once we have checked this, we can solve for 'a' by taking the 
		/// magnitude of each side and dividing. If the resultant vectors are 
		/// parallel, but in opposite directions, then 'a' is the negative of the 
		/// ratio of magnitudes. Once we have 'a' we can go back to the equation 
		/// for L1 to find the intersection point.
		/// 
		/// Write back if you need more help with this.
		/// 
		/// - Doctor George, The Math Forum
		///   http://mathforum.org/dr.math/ 		
		/// </remarks>
		private static Point3D? GetIntersectionOfTwoLines_BAD(Point3D pointOnLine1, Vector3D lineDirection1, Point3D pointOnLine2, Vector3D lineDirection2)
		{
			throw new ApplicationException("This method is broken, finish the alternate (it looks like it's cheaper)");

			//	a (V1 X V2) = (P2 - P1) X V2
			//	and solve for a

			//	Convert to unit vectors
			//Vector3D v1 = lineDirection1.ToUnit();
			//Vector3D v2 = lineDirection2.ToUnit();

			Vector3D p2_p1 = pointOnLine2 - pointOnLine1;

			//Vector3D leftCross = Vector3D.CrossProduct(v1, v2);
			//Vector3D rightCross = Vector3D.CrossProduct(p2_p1, v2);
			Vector3D leftCross = Vector3D.CrossProduct(lineDirection1, lineDirection2);
			Vector3D rightCross = Vector3D.CrossProduct(p2_p1, lineDirection2);

			//	Make sure they are parallel
			//double dot = Vector3D.DotProduct(leftCross, rightCross.ToUnit());		//	left cross is the cross of 2 units, so is also a unit vector
			double dot = Vector3D.DotProduct(leftCross.ToUnit(), rightCross.ToUnit());
			if (!IsNearValue(Math.Abs(dot), 1d))
			{
				return null;
			}

			//	Figure out the length
			//double lengthA = 1d / p2_p1.Length;
			double lengthA = leftCross.Length / rightCross.Length;
			if (dot < 0)
			{
				lengthA *= -1;
			}

			return (lineDirection1 * lengthA).ToPoint();
		}
		/// <summary>
		/// 
		/// </summary>
		/// <remarks>
		/// Got this here:
		/// http://mathforum.org/library/drmath/view/63719.html
		/// 
		/// Define line 1 to contain point (x1,y1,z1) with vector (a1,b1,c1).
		/// Define line 2 to contain point (x2,y2,z2) with vector (a2,b2,c2).
		/// 
		/// We can write these parametric equations for the lines.
		/// 
		///       Line1                         Line2
		///       -----                         -----
		///   x = x1 + a1 * t1              x = x2 + a2 * t2
		///   y = y1 + b1 * t1              y = y2 + b2 * t2
		///   z = z1 + c1 * t1              z = z2 + c2 * t2
		/// 
		/// If we set the two x values equal, and the two y values equal we get
		/// these two equations.
		/// 
		///   x1 + a1 * t1 = x2 + a2 * t2
		///   y1 + b1 * t1 = y2 + b2 * t2
		/// 
		/// You can solve these equations for t1 and t2. Then put those values
		/// back into the parametric equations to solve for the intersection 
		/// point.
		/// 
		/// If you have done the arithmetic correctly, you only need to use one of
		/// the equations for each of x and y. You should check both equations for
		/// z to make sure they give the same result. If they give different
		/// results then the lines are skew.
		/// 
		/// 
		/// 
		/// 
		/// Now try Dr. George's method. The lines are
		/// 
		///      Line1                    Line2
		///      -----                    -----
		///   x = 1 + 2t1              x = 0 + 5t2
		///   y = 0 + 3t1              y = 5 + 1t2
		///   z = 0 + 1t1              z = 5 - 3t2
		/// 
		/// Setting the x's and y's equal, we have to solve
		/// 
		///   1 + 2t1 = 0 + 5t2
		///   0 + 3t1 = 5 + 1t2
		/// 
		/// This reduces to
		/// 
		///   2t1 - 5t2 = -1
		///   3t1 - 1t2 = 5
		/// 
		/// We can multiply the first equation by -1 and the second equation by
		/// 5 and add:
		/// 
		///   -2t1 + 5t2 =  1
		///   15t1 - 5t2 = 25
		///   ---------------
		///   13t1       = 26
		/// 
		///   t1 = 2
		/// 
		/// Plugging that into the second of our pair, we get
		/// 
		///   3*2 - t2 = 5
		/// 
		/// which gives
		/// 
		///   t2 = 1
		/// 
		/// Plugging those into all six equations for x, y, and z, we get
		/// 
		///   x = 1 + 2*2 = 5     x = 0 + 5*1 = 5
		///   y = 0 + 3*2 = 6     y = 5 + 1*1 = 6
		///   z = 0 + 1*2 = 2     z = 5 - 3*1 = 2
		/// 
		/// So this is indeed the intersection of the lines.
		/// </remarks>
		private static Point3D? GetIntersectionOfTwoLines(Point3D pointOnLine1, Vector3D lineDirection1, Point3D pointOnLine2, Vector3D lineDirection2)
		{
			throw new ApplicationException("finish this");
		}

		/// <summary>
		/// This gets the line of intersection between two planes (returns false if they are parallel)
		/// </summary>
		/// <remarks>
		/// Got this here:
		/// http://forums.create.msdn.com/forums/t/40119.aspx
		/// </remarks>
		public static bool GetIntersectingLine(out Point3D point, out Vector3D direction, ITriangle plane1, ITriangle plane2)
		{
			Vector3D normal1 = plane1.NormalUnit;
			Vector3D normal2 = plane2.NormalUnit;

			//	Find a point that satisfies both plane equations
			double distance1 = -plane1.PlaneDistance;		// Math3D.PlaneDistance uses "normal + d = 0", but the equation below uses "normal = d", so the distances need to be negated
			double distance2 = -plane2.PlaneDistance;

			double offDiagonal = Vector3D.DotProduct(normal1, normal2);
			if (IsNearValue(Math.Abs(offDiagonal), 1d))
			{
				//	The planes are parallel
				point = new Point3D();
				direction = new Vector3D();
				return false;
			}

			double det = 1d - offDiagonal * offDiagonal;

			double a = (distance1 - distance2 * offDiagonal) / det;
			double b = (distance2 - distance1 * offDiagonal) / det;
			point = (a * normal1 + b * normal2).ToPoint();

			//	The line is perpendicular to both normals
			direction = Vector3D.CrossProduct(normal1, normal2);

			return true;
		}

		/// <summary>
		/// This returns the distance between a plane and the origin
		/// NOTE: Normal must be a unit vector
		/// </summary>
		public static double GetPlaneDistance(Vector3D normal, Point3D point)
		{
			double distance = 0;	// This variable holds the distance from the plane to the origin

			// Use the plane equation to find the distance (Ax + By + Cz + D = 0)  We want to find D.
			// So, we come up with D = -(Ax + By + Cz)
			// Basically, the negated dot product of the normal of the plane and the point. (More about the dot product in another tutorial)
			distance = -((normal.X * point.X) + (normal.Y * point.Y) + (normal.Z * point.Z));

			return distance; // Return the distance
		}

		// Math.Since the last tutorial, we added 2 more parameters for the normal and the distance
		// from the origin.  This is so we don't have to recalculate it 3 times in our IntersectionPoint() 
		// IntersectedPolygon() functions.  We would probably make 2 different functions for
		// this so we have the choice of getting the normal and distance back, or not.
		// I also changed the vTriangle to "vPoly" because it isn't always a triangle.
		// The code doesn't change, it's just more correct (though we only need 3 points anyway).
		// For C programmers, the '&' is called a reference and is the same concept as the '*' for addresMath.Sing.

		/// <summary>
		/// This checks to see if a line intersects a plane
		/// </summary>
		public static bool IntersectedPlane(Vector3D[] polygon, Vector3D[] line, out Vector3D normal, out double originDistance)
		{
			if (line.Length != 2) throw new ArgumentException("A line vector can only be 2 verticies.", "vLine");

			double distance1 = 0, distance2 = 0;						// The distances from the 2 points of the line from the plane

			normal = Normal(polygon);							// We need to get the normal of our plane to go any further

			// Let's find the distance our plane is from the origin.  We can find this value
			// from the normal to the plane (polygon) and any point that lies on that plane (Any vertice)
			originDistance = GetPlaneDistance(normal, polygon[0].ToPoint());

			// Get the distance from point1 from the plane uMath.Sing: Ax + By + Cz + D = (The distance from the plane)

			distance1 = ((normal.X * line[0].X) +					// Ax +
						 (normal.Y * line[0].Y) +					// Bx +
						 (normal.Z * line[0].Z)) + originDistance;	// Cz + D

			// Get the distance from point2 from the plane uMath.Sing Ax + By + Cz + D = (The distance from the plane)

			distance2 = ((normal.X * line[1].X) +					// Ax +
						 (normal.Y * line[1].Y) +					// Bx +
						 (normal.Z * line[1].Z)) + originDistance;	// Cz + D

			// Now that we have 2 distances from the plane, if we times them together we either
			// get a positive or negative number.  If it's a negative number, that means we collided!
			// This is because the 2 points must be on either side of the plane (IE. -1 * 1 = -1).

			if (distance1 * distance2 >= 0)			// Check to see if both point's distances are both negative or both positive
				return false;						// Return false if each point has the same sign.  -1 and 1 would mean each point is on either side of the plane.  -1 -2 or 3 4 wouldn't...

			return true;							// The line intersected the plane, Return TRUE
		}

		/// <summary>
		/// This returns the intersection point of the line that intersects the plane
		/// </summary>
		public static Point3D? GetIntersectionOfPlaneAndLine(ITriangle plane, Point3D pointOnLine, Vector3D lineDir)
		{
			Vector3D? retVal = GetIntersectionOfPlaneAndLine(plane.NormalUnit, new Vector3D[] { pointOnLine.ToVector(), (pointOnLine + lineDir).ToVector() }, plane.PlaneDistance);
			if (retVal == null)
			{
				return null;
			}
			else
			{
				return retVal.Value.ToPoint();
			}
		}
		private static Vector3D? GetIntersectionOfPlaneAndLine(Vector3D normal, Vector3D[] line, double originDistance)
		{
			Vector3D result = new Vector3D();

			// Here comes the confuMath.Sing part.  We need to find the 3D point that is actually
			// on the plane.  Here are some steps to do that:

			// 1)  First we need to get the vector of our line, Then normalize it so it's a length of 1
			Vector3D lineDir = line[1] - line[0];		// Get the Vector of the line
			lineDir.Normalize();				// Normalize the lines vector


			// 2) Use the plane equation (distance = Ax + By + Cz + D) to find the distance from one of our points to the plane.
			//    Here I just chose a arbitrary point as the point to find that distance.  You notice we negate that
			//    distance.  We negate the distance because we want to eventually go BACKWARDS from our point to the plane.
			//    By doing this is will basically bring us back to the plane to find our intersection point.
			double numerator = -(normal.X * line[0].X +		// Use the plane equation with the normal and the line
											   normal.Y * line[0].Y +
											   normal.Z * line[0].Z + originDistance);

			// 3) If we take the dot product between our line vector and the normal of the polygon,
			//    this will give us the Math.CoMath.Sine of the angle between the 2 (Math.Since they are both normalized - length 1).
			//    We will then divide our Numerator by this value to find the offset towards the plane from our arbitrary point.
			double denominator = Vector3D.DotProduct(normal, lineDir);		// Get the dot product of the line's vector and the normal of the plane

			// Math.Since we are uMath.Sing division, we need to make sure we don't get a divide by zero error
			// If we do get a 0, that means that there are INFINATE points because the the line is
			// on the plane (the normal is perpendicular to the line - (Normal.Vector = 0)).  
			// In this case, we should just return any point on the line.

			if (denominator == 0.0)						// Check so we don't divide by zero
				return null;		//	line is parallel to plane

			// We divide the (distance from the point to the plane) by (the dot product)
			// to get the distance (dist) that we need to move from our arbitrary point.  We need
			// to then times this distance (dist) by our line's vector (direction).  When you times
			// a scalar (Math.Single number) by a vector you move along that vector.  That is what we are
			// doing.  We are moving from our arbitrary point we chose from the line BACK to the plane
			// along the lines vector.  It seems logical to just get the numerator, which is the distance
			// from the point to the line, and then just move back that much along the line's vector.
			// Well, the distance from the plane means the SHORTEST distance.  What about in the case that
			// the line is almost parallel with the polygon, but doesn't actually intersect it until half
			// way down the line's length.  The distance from the plane is short, but the distance from
			// the actual intersection point is pretty long.  If we divide the distance by the dot product
			// of our line vector and the normal of the plane, we get the correct length.  Cool huh?

			double dist = numerator / denominator;				// Divide to get the multiplying (percentage) factor

			// Now, like we said above, we times the dist by the vector, then add our arbitrary point.
			// This essentially moves the point along the vector to a certain distance.  This now gives
			// us the intersection point.  Yay!

			result.X = line[0].X + (lineDir.X * dist);
			result.Y = line[0].Y + (lineDir.Y * dist);
			result.Z = line[0].Z + (lineDir.Z * dist);

			return result;								// Return the intersection point
		}

		/// <summary>
		/// This checks to see if a point is inside the ranges of a polygon
		/// </summary>
		public static bool InsidePolygon(Vector3D intersectionPoint, Vector3D[] polygon, long verticeCount)
		{
			const double MATCH_FACTOR = 0.9999;		// Used to cover up the error in floating point
			double Angle = 0.0;						// Initialize the angle

			// Just because we intersected the plane, doesn't mean we were anywhere near the polygon.
			// This functions checks our intersection point to make sure it is inside of the polygon.
			// This is another tough function to grasp at first, but let me try and explain.
			// It's a brilliant method really, what it does is create triangles within the polygon
			// from the intersection point.  It then adds up the inner angle of each of those triangles.
			// If the angles together add up to 360 degrees (or 2 * PI in radians) then we are inside!
			// If the angle is under that value, we must be outside of polygon.  To further
			// understand why this works, take a pencil and draw a perfect triangle.  Draw a dot in
			// the middle of the triangle.  Now, from that dot, draw a line to each of the vertices.
			// Now, we have 3 triangles within that triangle right?  Now, we know that if we add up
			// all of the angles in a triangle we get 360 right?  Well, that is kinda what we are doing,
			// but the inverse of that.  Say your triangle is an isosceles triangle, so add up the angles
			// and you will get 360 degree angles.  90 + 90 + 90 is 360.

			for (int i = 0; i < verticeCount; i++)		// Go in a circle to each vertex and get the angle between
			{
				Vector3D vA = polygon[i] - intersectionPoint;	// Subtract the intersection point from the current vertex
				// Subtract the point from the next vertex
				Vector3D vB = polygon[(i + 1) % verticeCount] - intersectionPoint;

				Angle += DegreesToRadians(Vector3D.AngleBetween(vA, vB));	// Find the angle between the 2 vectors and add them all up as we go along
			}

			// Now that we have the total angles added up, we need to check if they add up to 360 degrees.
			// Math.Since we are uMath.Sing the dot product, we are working in radians, so we check if the angles
			// equals 2*PI.  We defined PI in 3DMath.h.  You will notice that we use a MATCH_FACTOR
			// in conjunction with our desired degree.  This is because of the inaccuracy when working
			// with floating point numbers.  It usually won't always be perfectly 2 * PI, so we need
			// to use a little twiddling.  I use .9999, but you can change this to fit your own desired accuracy.

			if (Angle >= (MATCH_FACTOR * (2.0 * Math.PI)))	// If the angle is greater than 2 PI, (360 degrees)
				return true;							// The point is inside of the polygon

			return false;								// If you get here, it obviously wasn't inside the polygon, so Return FALSE
		}

		/// <summary>
		/// This checks if a line is intersecting a polygon
		/// </summary>
		public static bool IntersectedPolygon(Vector3D[] polygon, Vector3D[] line, int verticeCount, out Vector3D normal, out double originDistance)
		{
			// First we check to see if our line intersected the plane.  If this isn't true
			// there is no need to go on, so return false immediately.
			// We pass in address of vNormal and originDistance so we only calculate it once

			// Reference
			if (!IntersectedPlane(polygon, line, out normal, out originDistance))
				return false;

			// Now that we have our normal and distance passed back from IntersectedPlane(), 
			// we can use it to calculate the intersection point.  The intersection point
			// is the point that actually is ON the plane.  It is between the line.  We need
			// this point test next, if we are inside the polygon.  To get the I-Point, we
			// give our function the normal of the plan, the points of the line, and the originDistance.

			Vector3D? vIntersection = GetIntersectionOfPlaneAndLine(normal, line, originDistance);
			if (vIntersection == null)
				return false;

			// Now that we have the intersection point, we need to test if it's inside the polygon.
			// To do this, we pass in :
			// (our intersection point, the polygon, and the number of vertices our polygon has)

			if (InsidePolygon(vIntersection.Value, polygon, verticeCount))
				return true;							// We collided!	  Return success


			// If we get here, we must have NOT collided

			return false;								// There was no collision, so return false
		}
		/// <summary>
		/// This checks if a line is intersecting a polygon
		/// </summary>
		public static bool IntersectedPolygon(Vector3D[] polygon, Vector3D[] line)
		{
			Vector3D normal;
			double originDistance;

			return IntersectedPolygon(polygon, line, polygon.Length, out normal, out originDistance);
		}

		public static double DistanceFromPlane(Vector3D[] polygon, Vector3D point)
		{
			Vector3D normal = Normal(polygon); // We need to get the normal of our plane to go any further

			// Let's find the distance our plane is from the origin.  We can find this value
			// from the normal to the plane (polygon) and any point that lies on that plane (Any vertice)
			double originDistance = GetPlaneDistance(normal, polygon[0].ToPoint());

			// Get the distance from point1 from the plane uMath.Sing: Ax + By + Cz + D = (The distance from the plane)
			return DistanceFromPlane(normal, originDistance, point);
		}
		public static double DistanceFromPlane(Vector3D normal, double originDistance, Vector3D point)
		{
			return ((normal.X * point.X) +					// Ax +
					(normal.Y * point.Y) +					// Bx +
					(normal.Z * point.Z)) + originDistance;	// Cz + D
		}

		/// <summary>
		/// This returns the normal of a polygon (The direction the polygon is facing)
		/// </summary>
		public static Vector3D Normal(Vector3D[] triangle)
		{
			//	This is the original, but was returning a left handed normal
			//Vector3D vVector1 = triangle[2] - triangle[0];
			//Vector3D vVector2 = triangle[1] - triangle[0];

			//Vector3D vNormal = Vector3D.CrossProduct(vVector1, vVector2);		// Take the cross product of our 2 vectors to get a perpendicular vector


			Vector3D dir1 = triangle[0] - triangle[1];
			Vector3D dir2 = triangle[2] - triangle[1];

			Vector3D normal = Vector3D.CrossProduct(dir2, dir1);


			// Now we have a normal, but it's at a strange length, so let's make it length 1.
			normal.Normalize();

			return normal;										// Return our normal at our desired length
		}

		#region Circle/Line Intersect Helpers

		private struct CircleLineArgs
		{
			public ITriangle CirclePlane;
			public Point3D CircleCenter;
			public double CircleRadius;
			public Point3D PointOnLine;
			public Vector3D LineDirection;
		}

		private struct CirclePlaneIntersectProps
		{
			//	This is the line that the planes intersect along
			public Point3D PointOnLine;
			public Vector3D LineDirection;

			//	This is a line from the circle's center to the intersect line
			public Point3D NearestToCenter;
			public Vector3D CenterToNearest;
			public double CenterToNearestLength;

			//	This is whether NearestToCenter is within the circle or outside of it
			public bool IsInsideCircle;
		}

		private static bool GetClosestPointsBetweenLineCircle(out Point3D[] circlePoints, out Point3D[] linePoints, CircleLineArgs args)
		{
			#region Scenarios

			//	Line intersects plane inside circle:
			//		Calculate intersect point to circle rim
			//		Calculate two perps to circle rim
			//
			//	Take the closest of those three points

			//	Line intersects plane outside circle, but passes over circle:
			//		Calculate intersect point to circle rim
			//		Calculate two perps to circle rim
			//
			//	Take the closest of those three points

			//	Line is parallel to the plane, passes over circle
			//		Calculate two perps to circle rim

			//	Line is parallel to the plane, does not pass over circle
			//		Get closest point between center and line, project onto plane, find point along the circle

			//	Line does not pass over the circle
			//		Calculate intersect point to circle rim
			//		Get closest point between plane intersect line and circle center
			//
			//	Take the closest of those two points

			//	Line is perpendicular to the plane
			//		Calculate intersect point to circle rim

			#endregion

			//	Detect perpendicular
			double dot = Vector3D.DotProduct(args.CirclePlane.NormalUnit, args.LineDirection.ToUnit());
			if (IsNearValue(Math.Abs(dot), 1d))
			{
				return GetClosestPointsBetweenLineCircleSprtPerpendicular(out circlePoints, out linePoints, args);
			}

			//	Project the line onto the circle's plane
			CirclePlaneIntersectProps planeIntersect = GetClosestPointsBetweenLineCircleSprtPlaneIntersect(args);

			//	There's less to do if the line is parallel
			if (IsNearZero(dot))
			{
				GetClosestPointsBetweenLineCircleSprtParallel(out circlePoints, out linePoints, args, planeIntersect);
			}
			else
			{
				GetClosestPointsBetweenLineCircleSprtOther(out circlePoints, out linePoints, args, planeIntersect);
			}

			return true;
		}
		private static bool GetClosestPointsBetweenLineCircleSprtPerpendicular(out Point3D[] circlePoints, out Point3D[] linePoints, CircleLineArgs args)
		{
			Point3D planeIntersect = GetNearestPointAlongLine(args.PointOnLine, args.LineDirection, args.CircleCenter);

			if (IsNearValue(planeIntersect, args.CircleCenter))
			{
				//	This is a perpendicular ray shot straight through the center.  All circle points are closest to the line
				circlePoints = null;
				linePoints = new Point3D[] { args.CircleCenter };
				return false;
			}

			GetClosestPointsBetweenLineCircleSprtCenterToPlaneIntersect(out circlePoints, out linePoints, args, planeIntersect);
			return true;
		}
		private static void GetClosestPointsBetweenLineCircleSprtParallel(out Point3D[] circlePoints, out Point3D[] linePoints, CircleLineArgs args, CirclePlaneIntersectProps planeIntersect)
		{
			if (planeIntersect.IsInsideCircle)
			{
				GetClosestPointsBetweenLineCircleSprtInsidePerps(out circlePoints, out linePoints, args, planeIntersect);
			}
			else
			{
				circlePoints = new Point3D[] { args.CircleCenter + (planeIntersect.CenterToNearest * (args.CircleRadius / planeIntersect.CenterToNearestLength)) };
				linePoints = new Point3D[] { GetNearestPointAlongLine(args.PointOnLine, args.LineDirection, circlePoints[0]) };
			}
		}
		private static void GetClosestPointsBetweenLineCircleSprtOther(out Point3D[] circlePoints, out Point3D[] linePoints, CircleLineArgs args, CirclePlaneIntersectProps planeIntersect)
		{
			//	See where the line intersects the circle's plane
			Point3D? lineIntersect = GetIntersectionOfPlaneAndLine(args.CirclePlane, args.PointOnLine, args.LineDirection);
			if (lineIntersect == null)		//	this should never happen, since an IsParallel check was already done (but one might be stricter than the other)
			{
				GetClosestPointsBetweenLineCircleSprtParallel(out circlePoints, out linePoints, args, planeIntersect);
				return;
			}

			if (planeIntersect.IsInsideCircle)
			{
				#region Line is over circle

				//	Line intersects plane inside circle:
				//		Calculate intersect point to circle rim
				//		Calculate two perps to circle rim
				//
				//	Take the closest of those three points

				Point3D[] circlePoints1;
				Point3D[] linePoints1;
				GetClosestPointsBetweenLineCircleSprtCenterToPlaneIntersect(out circlePoints1, out linePoints1, args, lineIntersect.Value);

				Point3D[] circlePoints2;
				Point3D[] linePoints2;
				GetClosestPointsBetweenLineCircleSprtInsidePerps(out circlePoints2, out linePoints2, args, planeIntersect);

				GetClosestPointsBetweenLineCircleSprtOtherSprtMin(out circlePoints, out linePoints, circlePoints1, linePoints1, circlePoints2, linePoints2);

				#endregion
			}
			else
			{
				#region Line is outside circle

				//	Line does not pass over the circle
				//		Calculate intersect point to circle rim
				//		Get closest point between plane intersect line and circle center
				//
				//	Take the closest of those two points

				Point3D[] circlePoints3;
				Point3D[] linePoints3;
				GetClosestPointsBetweenLineCircleSprtCenterToPlaneIntersect(out circlePoints3, out linePoints3, args, lineIntersect.Value);

				Point3D[] circlePoints4;
				Point3D[] linePoints4;
				GetClosestPointsBetweenLineCircleSprtCenterToPlaneIntersect(out circlePoints4, out linePoints4, args, planeIntersect.NearestToCenter);

				GetClosestPointsBetweenLineCircleSprtOtherSprtMin(out circlePoints, out linePoints, circlePoints3, linePoints3, circlePoints4, linePoints4);

				#endregion
			}
		}
		private static void GetClosestPointsBetweenLineCircleSprtOtherSprtMin(out Point3D[] circlePoints, out Point3D[] linePoints, Point3D[] circlePoints1, Point3D[] linePoints1, Point3D[] circlePoints2, Point3D[] linePoints2)
		{
			List<Point3D> circlePointList = new List<Point3D>();
			List<Point3D> linePointList = new List<Point3D>();
			double distance = double.MaxValue;

			//	Find the shortest distance across the pairs
			if (circlePoints1 != null)
			{
				for (int cntr = 0; cntr < circlePoints1.Length; cntr++)
				{
					double localDistance = (linePoints1[cntr] - circlePoints1[cntr]).Length;

					if (IsNearValue(localDistance, distance))
					{
						circlePointList.Add(circlePoints1[cntr]);
						linePointList.Add(linePoints1[cntr]);
					}
					else if (localDistance < distance)
					{
						circlePointList.Clear();
						linePointList.Clear();
						circlePointList.Add(circlePoints1[cntr]);
						linePointList.Add(linePoints1[cntr]);
						distance = localDistance;
					}
				}
			}

			if (circlePoints2 != null)
			{
				for (int cntr = 0; cntr < circlePoints2.Length; cntr++)
				{
					double localDistance = (linePoints2[cntr] - circlePoints2[cntr]).Length;

					if (IsNearValue(localDistance, distance))
					{
						circlePointList.Add(circlePoints2[cntr]);
						linePointList.Add(linePoints2[cntr]);
					}
					else if (localDistance < distance)
					{
						circlePointList.Clear();
						linePointList.Clear();
						circlePointList.Add(circlePoints2[cntr]);
						linePointList.Add(linePoints2[cntr]);
						distance = localDistance;
					}
				}
			}

			if (circlePointList.Count == 0)
			{
				throw new ApplicationException("Couldn't find a return point");
			}

			//	Return the result
			circlePoints = circlePointList.ToArray();
			linePoints = linePointList.ToArray();
		}
		private static CirclePlaneIntersectProps GetClosestPointsBetweenLineCircleSprtPlaneIntersect(CircleLineArgs args)
		{
			CirclePlaneIntersectProps retVal;

			//	The slice plane runs perpendicular to the circle's plane
			Triangle slicePlane = new Triangle(args.PointOnLine, args.PointOnLine + args.LineDirection, args.PointOnLine + args.CirclePlane.Normal);

			//	Use that slice plane to project the line onto the circle's plane
			if (!GetIntersectingLine(out retVal.PointOnLine, out retVal.LineDirection, args.CirclePlane, slicePlane))
			{
				throw new ApplicationException("The slice plane should never be parallel to the circle's plane");		//	it was defined as perpendicular
			}

			//	Find the closest point between the circle's center to this intersection line
			retVal.NearestToCenter = GetNearestPointAlongLine(retVal.PointOnLine, retVal.LineDirection, args.CircleCenter);
			retVal.CenterToNearest = retVal.NearestToCenter - args.CircleCenter;
			retVal.CenterToNearestLength = retVal.CenterToNearest.Length;

			retVal.IsInsideCircle = retVal.CenterToNearestLength <= args.CircleRadius;

			//	Exit Function
			return retVal;
		}
		private static void GetClosestPointsBetweenLineCircleSprtCenterToPlaneIntersect(out Point3D[] circlePoints, out Point3D[] linePoints, CircleLineArgs args, Point3D planeIntersect)
		{
			Vector3D centerToIntersect = planeIntersect - args.CircleCenter;
			double centerToIntersectLength = centerToIntersect.Length;

			if (IsNearZero(centerToIntersectLength))
			{
				circlePoints = null;
				linePoints = null;
			}
			else
			{
				circlePoints = new Point3D[] { args.CircleCenter + (centerToIntersect * (args.CircleRadius / centerToIntersectLength)) };
				linePoints = new Point3D[] { GetNearestPointAlongLine(args.PointOnLine, args.LineDirection, circlePoints[0]) };
			}
		}
		private static void GetClosestPointsBetweenLineCircleSprtInsidePerps(out Point3D[] circlePoints, out Point3D[] linePoints, CircleLineArgs args, CirclePlaneIntersectProps planeIntersect)
		{
			//	See if the line passes through the center
			if (IsNearZero(planeIntersect.CenterToNearestLength))
			{
				//Vector3D lineDirUnit = args.LineDirection.ToUnit();
				Vector3D lineDirUnit = planeIntersect.LineDirection.ToUnit();

				//	The line passes over the circle's center, so the nearest points will shoot straight from the center in the direction of the line
				circlePoints = new Point3D[2];
				circlePoints[0] = args.CircleCenter + (lineDirUnit * args.CircleRadius);
				circlePoints[1] = args.CircleCenter - (lineDirUnit * args.CircleRadius);
			}
			else
			{
				//	The two points are perpendicular to this line.  Use A^2 + B^2 = C^2 to get the length of the perpendiculars
				double perpLength = Math.Sqrt((args.CircleRadius * args.CircleRadius) - (planeIntersect.CenterToNearestLength * planeIntersect.CenterToNearestLength));
				Vector3D perpDirection = Vector3D.CrossProduct(planeIntersect.CenterToNearest, args.CirclePlane.Normal).ToUnit();

				circlePoints = new Point3D[2];
				circlePoints[0] = planeIntersect.NearestToCenter + (perpDirection * perpLength);
				circlePoints[1] = planeIntersect.NearestToCenter - (perpDirection * perpLength);
			}

			//	Get corresponding points along the line
			linePoints = new Point3D[2];
			linePoints[0] = GetNearestPointAlongLine(args.PointOnLine, args.LineDirection, circlePoints[0]);
			linePoints[1] = GetNearestPointAlongLine(args.PointOnLine, args.LineDirection, circlePoints[1]);
		}

		/// <summary>
		/// This one returns the one that is closest to pointOnLine
		/// </summary>
		private static void GetClosestPointsBetweenLineCircleSprtClosest_RayOrigin(ref Point3D[] circlePoints, ref Point3D[] linePoints, Point3D rayOrigin)
		{
			#region Find closest point

			//	There is more than one point, and they want a single point
			double minDistance = double.MaxValue;
			int minIndex = -1;

			for (int cntr = 0; cntr < circlePoints.Length; cntr++)
			{
				double distance = (linePoints[cntr] - rayOrigin).LengthSquared;

				if (distance < minDistance)
				{
					minDistance = distance;
					minIndex = cntr;
				}
			}

			if (minIndex < 0)
			{
				throw new ApplicationException("Should always find a closest point");
			}

			#endregion

			//	Return only the closest point
			circlePoints = new Point3D[] { circlePoints[minIndex] };
			linePoints = new Point3D[] { linePoints[minIndex] };
		}
		/// <summary>
		/// This one returns the one that is closest between the two hits
		/// </summary>
		private static void GetClosestPointsBetweenLineCircleSprtClosest_CircleLine(ref Point3D[] circlePoints, ref Point3D[] linePoints, Point3D rayOrigin)
		{
			#region Find closest point

			//	There is more than one point, and they want a single point
			double minDistance = double.MaxValue;
			double minOriginDistance = double.MaxValue;		//	use this as a secondary sort (really important if the collision shape is a cylinder or sphere.  The line will have two exact matches, so return the one closest to the ray cast origin)
			int minIndex = -1;

			for (int cntr = 0; cntr < circlePoints.Length; cntr++)
			{
				double distance = (linePoints[cntr] - circlePoints[cntr]).LengthSquared;
				double originDistance = (linePoints[cntr] - rayOrigin).LengthSquared;

				bool isEqualDistance = IsNearValue(distance, minDistance);

				//NOTE: I can't just say distance < minDistance, because for a sphere, it kept jittering between the near
				//	side and far side, so it has to be closer by a decisive amount
				if ((!isEqualDistance && distance < minDistance) || (isEqualDistance && originDistance < minOriginDistance))
				{
					minDistance = distance;
					minOriginDistance = originDistance;
					minIndex = cntr;
				}
			}

			if (minIndex < 0)
			{
				throw new ApplicationException("Should always find a closest point");
			}

			#endregion

			//	Return only the closest point
			circlePoints = new Point3D[] { circlePoints[minIndex] };
			linePoints = new Point3D[] { linePoints[minIndex] };
		}

		#endregion
		#region Cylinder/Line Intersect Helpers

		private static CirclePlaneIntersectProps GetClosestPointsBetweenLineCylinderSprtPlaneIntersect(CircleLineArgs args, Point3D nearestLinePoint, Vector3D nearestLine, double nearestLineDistance)
		{
			//NOTE: This is nearly identical to GetClosestPointsBetweenLineCircleSprtPlaneIntersect, but since some stuff was already done,
			//	it's more just filling out the struct

			CirclePlaneIntersectProps retVal;

			//	The slice plane runs perpendicular to the circle's plane
			Triangle slicePlane = new Triangle(args.PointOnLine, args.PointOnLine + args.LineDirection, args.PointOnLine + args.CirclePlane.Normal);

			//	Use that slice plane to project the line onto the circle's plane
			if (!GetIntersectingLine(out retVal.PointOnLine, out retVal.LineDirection, args.CirclePlane, slicePlane))
			{
				throw new ApplicationException("The slice plane should never be parallel to the circle's plane");		//	it was defined as perpendicular
			}

			//	Store what was passed in (the circle/line intersect waits till now to do this, but for cylinder, this was done previously)
			retVal.NearestToCenter = nearestLinePoint;
			retVal.CenterToNearest = nearestLine;
			retVal.CenterToNearestLength = nearestLineDistance;

			retVal.IsInsideCircle = true;		//	this method is only called when true

			//	Exit Function
			return retVal;
		}

		private static void GetClosestPointsBetweenLineCylinderSprtFinish(out Point3D[] cylinderPoints, out Point3D[] linePoints, CircleLineArgs args, CirclePlaneIntersectProps intersectArgs)
		{
			//	Get the circle intersects
			Point3D[] circlePoints2D, linePoints2D;
			GetClosestPointsBetweenLineCircleSprtInsidePerps(out circlePoints2D, out linePoints2D, args, intersectArgs);

			//	Project the circle hits onto the original line
			Point3D? p1, p2, p3, p4;
			GetClosestPointsBetweenLines(out p1, out p2, args.PointOnLine, args.LineDirection, circlePoints2D[0], args.CirclePlane.Normal);
			GetClosestPointsBetweenLines(out p3, out p4, args.PointOnLine, args.LineDirection, circlePoints2D[1], args.CirclePlane.Normal);

			//	p1 and p2 are the same, p3 and p4 are the same
			if (p1 == null || p3 == null)
			{
				cylinderPoints = new Point3D[] { circlePoints2D[0], circlePoints2D[1] };
			}
			else
			{
				cylinderPoints = new Point3D[] { p1.Value, p3.Value };
			}
			linePoints = cylinderPoints;
		}

		#endregion

		#endregion

		#region Matrix Logic

		public static void MatrixDecompose(Matrix3D mat, out Vector3D vTrans, out Vector3D vScale, out Vector3D vRot, bool scaleTransform)
		{
			vTrans = new Vector3D();
			vScale = new Vector3D();

			Vector3D[] vCols = new Vector3D[]{
                    new Vector3D(mat.M11,mat.M12,mat.M13),
                    new Vector3D(mat.M21,mat.M22,mat.M23),
                    new Vector3D(mat.M31,mat.M32,mat.M33)  
                };

			vScale.X = vCols[0].Length;
			vScale.Y = vCols[1].Length;
			vScale.Z = vCols[2].Length;

			if (scaleTransform)
			{
				vTrans.X = mat.OffsetX / (vScale.X == 0 ? 1 : vScale.X);
				vTrans.Y = mat.OffsetY / (vScale.Y == 0 ? 1 : vScale.Y);
				vTrans.Z = mat.OffsetZ / (vScale.Z == 0 ? 1 : vScale.Z);
			}
			else
			{
				vTrans.X = mat.OffsetX;
				vTrans.Y = mat.OffsetY;
				vTrans.Z = mat.OffsetZ;
			}

			if (vScale.X != 0)
			{
				vCols[0].X /= vScale.X;
				vCols[0].Y /= vScale.X;
				vCols[0].Z /= vScale.X;
			}
			if (vScale.Y != 0)
			{
				vCols[1].X /= vScale.Y;
				vCols[1].Y /= vScale.Y;
				vCols[1].Z /= vScale.Y;
			}
			if (vScale.Z != 0)
			{
				vCols[2].X /= vScale.Z;
				vCols[2].Y /= vScale.Z;
				vCols[2].Z /= vScale.Z;
			}

			/*
			Matrix3D mRot = new Matrix3D();
			mRot.M11 = vCols[0].X;
			mRot.M12 = vCols[0].Y;
			mRot.M13 = vCols[0].Z;
			mRot.M14 = 0;
			mRot.M21 = vCols[1].X;
			mRot.M22 = vCols[1].Y;
			mRot.M23 = vCols[1].Z;
			mRot.M24 = 0;
			mRot.M31 = vCols[2].X;
			mRot.M32 = vCols[2].Y;
			mRot.M33 = vCols[2].Z;
			mRot.M34 = 0;
			mRot.OffsetX = 0;
			mRot.OffsetY = 0;
			mRot.OffsetZ = 0;
			mRot.M44 = 1;
			*/

			vRot = new Vector3D();
			vRot.X = Math.Asin(/*-mRot.M32*/-vCols[2].Y);

			double threshold = 0.001;
			double test = Math.Cos(vRot.X);
			if (Math.Abs(test) > threshold)
			{
				vRot.Y = RadiansToDegrees(Math.Atan2(/*mRot.M31*/vCols[2].X, /*mRot.M33*/vCols[2].Z));
				vRot.Z = RadiansToDegrees(Math.Atan2(/*mRot.M12*/vCols[0].Y, /*mRot.M22*/vCols[1].Y));
			}
			else
			{
				vRot.Y = 0.0;
				vRot.Z = RadiansToDegrees(Math.Atan2(-/*mRot.M21*/vCols[1].X, /*mRot.M11*/vCols[0].X));
			}
			vRot.X = RadiansToDegrees(vRot.X);
		}

		public static Vector3D GetMatrixScale(ref Matrix3D mat)
		{
			Vector3D[] vCols = new Vector3D[]{
                    new Vector3D(mat.M11,mat.M12,mat.M13),
                    new Vector3D(mat.M21,mat.M22,mat.M23),
                    new Vector3D(mat.M31,mat.M32,mat.M33)  
                };

			return new Vector3D(vCols[0].Length, vCols[1].Length, vCols[2].Length);
		}

		public static Matrix3D GetScaleMatrix(ref Matrix3D matrix)
		{
			Matrix3D result = Matrix3D.Identity;
			result.Scale(GetMatrixScale(ref matrix));
			return result;
		}

		/*
		public static Matrix3D CreateRotationMatrix(Vector3D rotation)
		{
			return CreateYawPitchRollMatrix(rotation.X, rotation.Y, rotation.Z);
		}

		public static Matrix3D CreateRotationX(double pitchInDegrees)
		{
			double x = DegreesToRadians(pitchInDegrees);

			double cx = Math.Cos(x);
			double sx = Math.Sin(x);

			Matrix3D mRot = new Matrix3D();
			mRot.M11 = 1;
			mRot.M12 = 0;
			mRot.M13 = 0;

			mRot.M21 = 0;
			mRot.M22 = cx;
			mRot.M23 = -sx;

			mRot.M31 = 0;
			mRot.M32 = sx;
			mRot.M33 = cx;

			mRot.M44 = 1;

			return mRot;
		}

		public static Matrix3D CreateRotationY(double yawInDegrees)
		{
			double y = DegreesToRadians(yawInDegrees);

			double cy = Math.Cos(y);
			double sy = Math.Sin(y);

			Matrix3D mRot = new Matrix3D();
			mRot.M11 = cy;
			mRot.M12 = 0;
			mRot.M13 = sy;

			mRot.M21 = 0;
			mRot.M22 = 1;
			mRot.M23 = 0;

			mRot.M31 = -sy;
			mRot.M32 = 0;
			mRot.M33 = cy;

			mRot.M44 = 1;

			return mRot;
		}

		public static Matrix3D CreateRotationZ(double rollInDegrees)
		{
			double z = DegreesToRadians(rollInDegrees);

			double cz = Math.Cos(z);
			double sz = Math.Sin(z);

			Matrix3D mRot = new Matrix3D();
			mRot.M11 = cz;
			mRot.M12 = -sz;
			mRot.M13 = 0;

			mRot.M21 = sz;
			mRot.M22 = cz;
			mRot.M23 = 0;

			mRot.M31 = 0;
			mRot.M32 = 0;
			mRot.M33 = 1;

			mRot.M44 = 1;

			return mRot;
		}
		*/

		/// <summary>
		/// Creates a YawPitchRoll matrix.
		/// </summary>
		/// <param name="xPitchDegrees">Pitch.</param>
		/// <param name="yYawDegrees">Yaw.</param>
		/// <param name="zRollDegrees">Roll.</param>
		/// <returns>The rotation matrix.</returns>
		/// 
		public static Matrix3D CreateYawPitchRollMatrix(double xPitchDegrees, double yYawDegrees, double zRollDegrees)
		{
			double x = DegreesToRadians(xPitchDegrees);
			double y = DegreesToRadians(yYawDegrees);
			double z = DegreesToRadians(zRollDegrees);

			double cx = Math.Cos(x);
			double cy = Math.Cos(y);
			double cz = Math.Cos(z);

			double sx = Math.Sin(x);
			double sy = Math.Sin(y);
			double sz = Math.Sin(z);

			Matrix3D mRot = new Matrix3D();
			mRot.M11 = cz * cy + sz * sx * sy;
			mRot.M12 = sz * cx;
			mRot.M13 = cz * -sy + sz * sx * sy;

			mRot.M21 = -sz * cy + cz * sx * sy;
			mRot.M22 = cz * cx;
			mRot.M23 = sz * sy + cz * sx * cy;

			mRot.M31 = cx * sy;
			mRot.M32 = -sx;
			mRot.M33 = cx * cy;

			mRot.M44 = 1;

			return mRot;

			//return CreateRotationZ(zRollDegrees) * CreateRotationX(xPitchDegrees) * CreateRotationY(yYawDegrees);
		}
		public static Matrix3D CreateYawPitchRollMatrix(Vector3D rotation)
		{
			return CreateYawPitchRollMatrix(rotation.X, rotation.Y, rotation.Z);
		}

		public static void RemoveMatrixScale(ref Matrix3D matrix)
		{
			Vector3D v = GetFrontVector(ref matrix);
			v.Normalize();
			SetFrontVector(ref matrix, v);

			v = GetUpVector(ref matrix);
			v.Normalize();
			SetUpVector(ref matrix, v);

			v = GetRightVector(ref matrix);
			v.Normalize();
			SetRightVector(ref matrix, v);
		}

		public static Matrix3D GetRotationMatrix(Matrix3D matrix)
		{
			Matrix3D result = Matrix3D.Identity;

			Vector3D v = GetFrontVector(ref matrix);
			v.Normalize();
			SetFrontVector(ref result, v);

			v = GetUpVector(ref matrix);
			v.Normalize();
			SetUpVector(ref result, v);

			v = GetRightVector(ref matrix);
			v.Normalize();
			SetRightVector(ref result, v);

			return result;
		}
		public static Matrix3D GetTranslationMatrix(Matrix3D matrix)
		{
			Matrix3D result = Matrix3D.Identity;

			SetOffset(ref result, GetOffset(ref matrix));

			return result;
		}

		/*
		public static Matrix3D CreateZDirectionMatrix(Point3D position, Vector3D lookDirection, Vector3D upDirection)
		{
			Vector3D zAxis = lookDirection;
			zAxis.Normalize();

			Vector3D xAxis = Vector3D.CrossProduct(upDirection, zAxis);
			if (xAxis.Length > 0)
			{
				xAxis.Normalize();

				Vector3D yAxis = Vector3D.CrossProduct(zAxis, xAxis);

				Vector3D p = (Vector3D)position;
				double offsetX = -Vector3D.DotProduct(xAxis, p);
				double offsetY = -Vector3D.DotProduct(yAxis, p);
				double offsetZ = -Vector3D.DotProduct(zAxis, p);

				Matrix3D result = new Matrix3D(
					xAxis.X, yAxis.X, zAxis.X, 0,
					xAxis.Y, yAxis.Y, zAxis.Y, 0,
					xAxis.Z, yAxis.Z, zAxis.Z, 0,
					offsetX, offsetY, offsetZ, 1);

				result.Invert();
				return result;
			}
			else
				return ZeroMatrix;
		}
		*/

		public static Matrix3D CreateZDirectionMatrix(Point3D offset, Vector3D lookDirection)
		{
			Vector3D up;
			Vector3D front;
			Vector3D right = lookDirection;
			right.Normalize();

			if (Math.Abs(right.Z) > 0.577f)
				front = Vector3D.CrossProduct(new Vector3D(right.Y, right.Z, 0), right);
			else
				front = Vector3D.CrossProduct(new Vector3D(right.Y, right.X, 0), right);

			front.Normalize();

			up = Vector3D.CrossProduct(right, front);

			return CreateMatrix(front, up, right, offset, false);
		}
		public static Matrix3D CreateZDirectionMatrix(Vector3D lookDirection)
		{
			return CreateZDirectionMatrix(new Point3D(), lookDirection);
		}

		/*
		public static MatrixTransform3D CreateZDirectionRotationTransform(Point3D position, Vector3D lookDirection, Vector3D upDirection)
		{
			return new MatrixTransform3D(CreateZDirectionMatrix(position, lookDirection, upDirection));
		}
		*/

		/*
		public static MatrixTransform3D CreateRotationTransform(double xDegrees, double yDegrees, double zDegrees)
		{
			return new MatrixTransform3D(CreateYawPitchRollMatrix(xDegrees, yDegrees, zDegrees));
		}

		public static MatrixTransform3D CreateRotationTransform(Vector3D rotation)
		{
			return new MatrixTransform3D(CreateRotationMatrix(rotation));
		}
		*/

		/*
		public static Quaternion CreateQuaternionFromAxisAngle(Vector3D axis, double angle)
		{
			Quaternion quaternion = new Quaternion();
			double num2 = angle * 0.5;
			double num = Math.Sin(num2);
			double num3 = Math.Cos(num2);
			quaternion.X = axis.X * num;
			quaternion.Y = axis.Y * num;
			quaternion.Z = axis.Z * num;
			quaternion.W = num3;
			return quaternion;
		}
		*/

		public static Vector3D GetFrontVector(ref Matrix3D matrix)
		{
			return new Vector3D(matrix.M11, matrix.M12, matrix.M13);
		}
		public static void SetFrontVector(ref Matrix3D matrix, Vector3D vector)
		{
			matrix.M11 = vector.X;
			matrix.M12 = vector.Y;
			matrix.M13 = vector.Z;
		}

		public static Vector3D GetUpVector(ref Matrix3D matrix)
		{
			return new Vector3D(matrix.M21, matrix.M22, matrix.M23);
		}
		public static void SetUpVector(ref Matrix3D matrix, Vector3D vector)
		{
			matrix.M21 = vector.X;
			matrix.M22 = vector.Y;
			matrix.M23 = vector.Z;
		}

		public static Vector3D GetRightVector(ref Matrix3D matrix)
		{
			return new Vector3D(matrix.M31, matrix.M32, matrix.M33);
		}
		public static void SetRightVector(ref Matrix3D matrix, Vector3D vector)
		{
			matrix.M31 = vector.X;
			matrix.M32 = vector.Y;
			matrix.M33 = vector.Z;
		}

		public static Vector3D GetOffset(ref Matrix3D matrix)
		{
			return new Vector3D(matrix.OffsetX, matrix.OffsetY, matrix.OffsetZ);
		}
		public static void SetOffset(ref Matrix3D matrix, Vector3D offset)
		{
			matrix.OffsetX = offset.X;
			matrix.OffsetY = offset.Y;
			matrix.OffsetZ = offset.Z;
		}

		public static Vector3D UnTransform(ref Matrix3D matrix, Vector3D vector)
		{
			return new Vector3D(
				Vector3D.DotProduct(vector, GetFrontVector(ref matrix)),
				Vector3D.DotProduct(vector, GetUpVector(ref matrix)),
				Vector3D.DotProduct(vector, GetRightVector(ref matrix)));
		}
		public static Point3D UnTransform(ref Matrix3D matrix, Point3D point)
		{
			return (Point3D)UnTransform(ref matrix, (Vector3D)point - GetOffset(ref matrix));
		}

		public static Vector3D Transform(Quaternion deltaQuaternion, Vector3D vector)
		{
			Quaternion direction = new Quaternion(vector.X, vector.Y, vector.Z, 0);

			// Compose the delta with the orientation
			direction = deltaQuaternion * direction;

			// convert back to vector
			deltaQuaternion.Conjugate();
			direction *= deltaQuaternion;

			return new Vector3D(direction.X, direction.Y, direction.Z);
		}
		public static void Transform(Quaternion deltaQuaternion, ProjectionCamera camera)
		{
			camera.LookDirection = Transform(deltaQuaternion, camera.LookDirection);
			camera.UpDirection = Transform(deltaQuaternion, camera.UpDirection);
		}

		public static Matrix3D CreateMatrix(Vector3D front, Vector3D up, Vector3D right, Point3D offset, bool normalize)
		{
			if (normalize)
			{
				front.Normalize();
				up.Normalize();
				right.Normalize();
			}

			return new Matrix3D(
				front.X, front.Y, front.Z, 0,
				up.X, up.Y, up.Z, 0,
				right.X, right.Y, right.Z, 0,
				offset.X, offset.Y, offset.Z, 1);
		}

		#endregion

		#region Should go in MathUtils

		public static Point3D TransformToWorld(DependencyObject visual, Point3D point)
		{
			Matrix3D m = MathUtils.GetTransformToWorld(visual);
			return m.Transform(point);
		}
		public static Vector3D TransformToWorld(DependencyObject visual, Vector3D vector)
		{
			Matrix3D m = MathUtils.GetTransformToWorld(visual);
			return m.Transform(vector);
		}

		public static Point3D TransformToLocal(DependencyObject visual, Point3D point)
		{
			Matrix3D m = MathUtils.GetTransformToLocal(visual);
			return m.Transform(point);
		}
		public static Vector3D TransformToLocal(DependencyObject visual, Vector3D vector)
		{
			Matrix3D m = MathUtils.GetTransformToLocal(visual);
			return m.Transform(vector);
		}

		#endregion

		#region Duplication with wpf

		/*
		/// <summary>
		/// This returns a perpendicular vector from 2 given vectors by taking the cross product.
		/// </summary>
		public static Vector3D Cross(Vector3D vector1, Vector3D vector2)
		{
			Vector3D result = new Vector3D();

			// Once again, if we are given 2 vectors (directions of 2 sides of a polygon)
			// then we have a plane define.  The cross product finds a vector that is perpendicular
			// to that plane, which means it's point straight out of the plane at a 90 degree angle.

			// The X value for the vector is:  (V1.Y * V2.Z) - (V1.Z * V2.Y)													// Get the X value
			result.X = ((vector1.Y * vector2.Z) - (vector1.Z * vector2.Y));

			// The Y value for the vector is:  (V1.Z * V2.X) - (V1.X * V2.Z)
			result.Y = ((vector1.Z * vector2.X) - (vector1.X * vector2.Z));

			// The Z value for the vector is:  (V1.X * V2.Y) - (V1.Y * V2.X)
			result.Z = ((vector1.X * vector2.Y) - (vector1.Y * vector2.X));

			return result; // Return the cross product (Direction the polygon is facing - Normal)
		}

		/// <summary>
		/// This returns the magnitude of a normal (or any other vector)
		/// TODO:  Verify that this is the same as Vector3D.Length
		/// </summary>
		public static double Magnitude(Vector3D normal)
		{
			// This will give us the magnitude or "Norm" as some say, of our normal.
			// Here is the equation:  magnitude = Math.Sqrt(V.X^2 + V.Y^2 + V.Z^2)  Where V is the vector

			return Math.Sqrt((normal.X * normal.X) + (normal.Y * normal.Y) + (normal.Z * normal.Z));
		}

		/// <summary>
		/// This computers the dot product of 2 vectors
		/// </summary>
		public static double Dot(Vector3D vector1, Vector3D vector2)
		{
			// The dot product is this equation: V1.V2 = (V1.X * V2.X  +  V1.Y * V2.Y  +  V1.Z * V2.Z)
			// In math terms, it looks like this:  V1.V2 = ||V1|| ||V2|| Math.Cos(theta)
			// The '.' means DOT.   The || || is magnitude.  So the magnitude of V1 times the magnitude
			// of V2 times the Math.CoMath.Sine of the angle.  It seems confuMath.Sing now, but it will become more clear.
			// This function is used for a ton of things, which we will cover in other tutorials.
			// For this tutorial, we use it to compute the angle between 2 vectors.  If the vectors
			// are normalize, the dot product returns the Math.CoMath.Sine of the angle between the 2 vectors.
			// What does that mean? Well, it doesn't return the actual angle, it returns the value of:
			// Math.Cos(angle).	Well, what if we want to get the actual angle?  Then we use the arc Math.CoMath.Sine.
			// There is more on this in the below function AngleBetweenVectors().  Let's give some
			// applications of uMath.Sing the dot product.  How would you tell if the angle between the
			// 2 vectors is perpendicular (90 degrees)?  Well, if we normalize the vectors we can
			// get rid of the ||V1|| * ||V2|| in front, which just leaves us with:  Math.Cos(theta).
			// If a vector is normalize, it's magnitude is 1, so it would be: 1 * 1 * Math.Cos(theta) , 
			// which is pointless, so we discard that part of the equation.  So, What is the Math.CoMath.Sine of 90?
			// If you punch it in your calculator you will find that it's 0.  So that means
			// if the dot product of 2 angles is 0, then they are perpendicular.  What we did in
			// our mind is take the arc Math.CoMath.Sine of 0, which is 90 (or PI/2 in radians).  More on this below.

			//    (V1.X * V2.X        +        V1.Y * V2.Y        +        V1.Z * V2.Z)
			return ((vector1.X * vector2.X) + (vector1.Y * vector2.Y) + (vector1.Z * vector2.Z));
		}

		/// <summary>
		/// This checks to see if a point is inside the ranges of a polygon
		/// </summary>
		public static double AngleBetweenVectors(Vector3D vector1, Vector3D vector2)
		{
			// Remember, above we said that the Dot Product of returns the Math.CoMath.Sine of the angle
			// between 2 vectors?  Well, that is assuming they are unit vectors (normalize vectors).
			// So, if we don't have a unit vector, then instead of just saying  arcMath.Cos(DotProduct(A, B))
			// We need to divide the dot product by the magnitude of the 2 vectors multiplied by each other.
			// Here is the equation:   arc Math.CoMath.Sine of (V . W / || V || * || W || )
			// the || V || means the magnitude of V.  This then cancels out the magnitudes dot product magnitudes.
			// But basically, if you have normalize vectors already, you can forget about the magnitude part.

			// Get the dot product of the vectors
			double dotProduct = Dot(vector1, vector2);

			// Get the product of both of the vectors magnitudes
			double vectorsMagnitude = Magnitude(vector1) * Magnitude(vector2);

			// Get the arc Math.CoMath.Sine of the (dotProduct / vectorsMagnitude) which is the angle in RADIANS.
			// (IE.   PI/2 radians = 90 degrees      PI radians = 180 degrees    2*PI radians = 360 degrees)
			// To convert radians to degress use this equation:   radians * (PI / 180)
			// TO convert degrees to radians use this equation:   degrees * (180 / PI)

			double angle = Math.Cos(dotProduct / vectorsMagnitude);

			// Here we make sure that the angle is not a -1.#IND0000000 number, which means indefinate.
			// Math.AMath.Cos() thinks it's funny when it returns -1.#IND0000000.  If we don't do this check,
			// our collision results will sometimes say we are colliding when we aren't.  I found this
			// out the hard way after MANY hours and already wrong written tutorials :)  Usually
			// this value is found when the dot product and the maginitude are the same value.
			// We want to return 0 when this happens.

			if (double.IsNaN(angle))
				return 0;

			// Return the angle in radians
			return (angle);
		}
		*/

		#endregion
	}
}
