using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using Game.HelperClasses;

namespace Game.Newt.HelperClasses
{
	public static class UtilityWPF
	{
		#region Class: QuickHull2D

		/// <remarks>
		/// Got this here (ported it from java):
		/// http://www.ahristov.com/tutorial/geometry-games/convex-hull.html
		/// </remarks>
		private static class QuickHull2D
		{
			public static QuickHull2DResult GetConvexHull(Point3D[] points)
			{
				Transform3D transform = null;

				//	If there are less than three points, just return everything
				if (points.Length == 0)
				{
					return new QuickHull2DResult(new Point[0], new int[0], null);
				}
				else if (points.Length == 1)
				{
					return new QuickHull2DResult(new Point[] { new Point(points[0].X, points[0].Y) }, new int[] { 0 }, new TranslateTransform3D(0, 0, -points[0].Z));
				}
				else if (points.Length == 2)
				{
					Point[] transformedPoints = GetRotatedPoints(out transform, points[0], points[1]);
					return new QuickHull2DResult(transformedPoints, new int[] { 0, 1 }, transform);
				}

				Point[] points2D = null;
				if (points.All(o => Math3D.IsNearZero(o.Z)))
				{
					//	There is no Z, so just directly convert the points (leave transform null)
					points2D = points.Select(o => new Point(o.X, o.Y)).ToArray();
				}
				else
				{
					//	Rotate the points so that Z drops out (and make sure they are all coplanar)
					points2D = GetRotatedPoints(out transform, points);
				}

				if (points2D == null)
				{
					return null;
				}

				//	Call quickhull
				QuickHull2DResult retVal = GetConvexHull(points2D);
				return new QuickHull2DResult(retVal.Points, retVal.PerimiterLines, transform);
			}
			public static QuickHull2DResult GetConvexHull(Point[] points)
			{
				if (points.Length < 3)
				{
					return new QuickHull2DResult(points, Enumerable.Range(0, points.Length).ToArray(), null);		//	return all the points
				}

				List<int> retVal = new List<int>();
				List<int> remainingPoints = Enumerable.Range(0, points.Length).ToList();

				#region Find two most extreme points

				int minIndex = -1;
				int maxIndex = -1;
				double minX = double.MaxValue;
				double maxX = double.MinValue;
				for (int cntr = 0; cntr < points.Length; cntr++)
				{
					if (points[cntr].X < minX)
					{
						minX = points[cntr].X;
						minIndex = cntr;
					}

					if (points[cntr].X > maxX)
					{
						maxX = points[cntr].X;
						maxIndex = cntr;
					}
				}

				#endregion

				#region Move points to return list

				retVal.Add(minIndex);
				retVal.Add(maxIndex);

				if (maxIndex > minIndex)
				{
					remainingPoints.RemoveAt(maxIndex);		//	need to remove the later index first so it doesn't shift
					remainingPoints.RemoveAt(minIndex);
				}
				else
				{
					remainingPoints.RemoveAt(minIndex);
					remainingPoints.RemoveAt(maxIndex);
				}

				#endregion

				#region Divide the list left and right of the line

				List<int> leftSet = new List<int>();
				List<int> rightSet = new List<int>();

				for (int cntr = 0; cntr < remainingPoints.Count; cntr++)
				{
					if (IsRightOfLine(minIndex, maxIndex, remainingPoints[cntr], points))
					{
						rightSet.Add(remainingPoints[cntr]);
					}
					else
					{
						leftSet.Add(remainingPoints[cntr]);
					}
				}

				#endregion

				//	Process these sets recursively, adding to retVal
				HullSet(minIndex, maxIndex, rightSet, retVal, points);
				HullSet(maxIndex, minIndex, leftSet, retVal, points);

				//	Exit Function
				return new QuickHull2DResult(points, retVal.ToArray(), null);
			}

			#region Private Methods

			private static void HullSet(int lineStart, int lineStop, List<int> set, List<int> hull, Point[] allPoints)
			{
				int insertPosition = hull.IndexOf(lineStop);

				if (set.Count == 0)
				{
					return;
				}
				else if (set.Count == 1)
				{
					hull.Insert(insertPosition, set[0]);
					set.RemoveAt(0);
					return;
				}

				#region Find most distant point

				double maxDistance = double.MinValue;
				int farIndexIndex = -1;
				for (int cntr = 0; cntr < set.Count; cntr++)
				{
					int point = set[cntr];
					double distance = GetDistanceFromLineSquared(allPoints[lineStart], allPoints[lineStop], allPoints[point]);
					if (distance > maxDistance)
					{
						maxDistance = distance;
						farIndexIndex = cntr;
					}
				}

				//	Move the point to the hull
				int farIndex = set[farIndexIndex];
				set.RemoveAt(farIndexIndex);
				hull.Insert(insertPosition, farIndex);

				#endregion

				#region Find everything left of (Start, Far)

				List<int> leftSet_Start_Far = new List<int>();
				for (int cntr = 0; cntr < set.Count; cntr++)
				{
					int pointIndex = set[cntr];
					if (IsRightOfLine(lineStart, farIndex, pointIndex, allPoints))
					{
						leftSet_Start_Far.Add(pointIndex);
					}
				}

				#endregion

				#region Find everything right of (Far, Stop)

				List<int> leftSet_Far_Stop = new List<int>();
				for (int cntr = 0; cntr < set.Count; cntr++)
				{
					int pointIndex = set[cntr];
					if (IsRightOfLine(farIndex, lineStop, pointIndex, allPoints))
					{
						leftSet_Far_Stop.Add(pointIndex);
					}
				}

				#endregion

				//	Recurse
				//NOTE: The set passed in was split into these two sets
				HullSet(lineStart, farIndex, leftSet_Start_Far, hull, allPoints);
				HullSet(farIndex, lineStop, leftSet_Far_Stop, hull, allPoints);
			}

			internal static bool IsRightOfLine(int lineStart, int lineStop, int testPoint, Point[] allPoints)
			{
				double cp1 = ((allPoints[lineStop].X - allPoints[lineStart].X) * (allPoints[testPoint].Y - allPoints[lineStart].Y)) -
									  ((allPoints[lineStop].Y - allPoints[lineStart].Y) * (allPoints[testPoint].X - allPoints[lineStart].X));

				return cp1 > 0;
				//return (cp1 > 0) ? 1 : -1;
			}
			internal static bool IsRightOfLine(int lineStart, int lineStop, Point testPoint, Point[] allPoints)
			{
				double cp1 = ((allPoints[lineStop].X - allPoints[lineStart].X) * (testPoint.Y - allPoints[lineStart].Y)) -
									  ((allPoints[lineStop].Y - allPoints[lineStart].Y) * (testPoint.X - allPoints[lineStart].X));

				return cp1 > 0;
				//return (cp1 > 0) ? 1 : -1;
			}

			private static double GetDistanceFromLineSquared(Point lineStart, Point lineStop, Point testPoint)
			{
				Point3D pointOnLine = new Point3D(lineStart.X, lineStart.Y, 0d);
				Vector3D lineDirection = new Point3D(lineStop.X, lineStop.Y, 0d) - pointOnLine;
				Point3D point = new Point3D(testPoint.X, testPoint.Y, 0d);

				Point3D nearestPoint = Math3D.GetNearestPointAlongLine(pointOnLine, lineDirection, point);

				return (point - nearestPoint).LengthSquared;
			}

			/// <summary>
			/// This overload assumes that there are at least 3 points, or it will just return null
			/// </summary>
			private static Point[] GetRotatedPoints(out Transform3D transform, Point3D[] points)
			{
				//	Make sure they are coplanar, and get a triangle that represents that plane
				ITriangle triangle = GetThreeCoplanarPoints(points);
				if (triangle == null)
				{
					transform = null;
					return null;
				}

				//	Figure out a transform that will make Z drop out
				transform = GetTransformTo2D(triangle);

				//	Transform them
				Point[] retVal = new Point[points.Length];
				for (int cntr = 0; cntr < points.Length; cntr++)
				{
					Point3D transformed = transform.Transform(points[cntr]);
					retVal[cntr] = new Point(transformed.X, transformed.Y);
				}

				//	Exit Function
				return retVal;
			}
			/// <summary>
			/// This overload handles exactly two points
			/// </summary>
			private static Point[] GetRotatedPoints(out Transform3D transform, Point3D point1, Point3D point2)
			{
				//	Get rotation
				Quaternion rotation = Quaternion.Identity;
				if (!Math3D.IsNearValue(point1, point2))
				{
					Vector3D line1 = point2 - point1;		//	this line is not along the z plane
					Vector3D line2 = new Point3D(point2.X, point2.Y, point1.Z) - point1;		//	this line uses point1's z so is in the z plane

					rotation = Math3D.GetRotation(line1, line2);
				}

				Transform3DGroup group = new Transform3DGroup();
				group.Children.Add(new RotateTransform3D(new QuaternionRotation3D(rotation)));

				//	Get Translation
				group.Children.Add(new TranslateTransform3D(0, 0, -point1.Z));

				transform = group;

				//	Transform the points
				Point[] retVal = new Point[2];
				Point3D transformedPoint = transform.Transform(point1);
				retVal[0] = new Point(transformedPoint.X, transformedPoint.Y);

				transformedPoint = transform.Transform(point2);
				retVal[1] = new Point(transformedPoint.X, transformedPoint.Y);

				//	Exit Function
				return retVal;
			}

			//NOTE: This also makes sure that all the points lie in the same plane as the returned triangle (some of the points may still be
			//colinear or the same, but at least they are on the same plane)
			private static ITriangle GetThreeCoplanarPoints(Point3D[] points)
			{
				Vector3D? line1 = null;
				Vector3D? line1Unit = null;

				ITriangle retVal = null;

				for (int cntr = 1; cntr < points.Length; cntr++)
				{
					if (Math3D.IsNearValue(points[0], points[cntr]))
					{
						//	These points are sitting on top of each other
						continue;
					}

					Vector3D line = points[cntr] - points[0];

					if (line1 == null)
					{
						//	Found the first line
						line1 = line;
						line1Unit = line.ToUnit();
						continue;
					}

					if (retVal == null)
					{
						if (!Math3D.IsNearValue(Math.Abs(Vector3D.DotProduct(line1Unit.Value, line.ToUnit())), 1d))
						{
							//	These two lines aren't colinear.  Found the second line
							retVal = new Triangle(points[0], points[0] + line1.Value, points[cntr]);
						}

						continue;
					}

					if (!Math3D.IsNearZero(Vector3D.DotProduct(retVal.Normal, line)))
					{
						//	This point isn't coplanar with the triangle
						return null;
					}
				}

				//	Exit Function
				return retVal;
			}

			private static Transform3D GetTransformTo2D(ITriangle triangle)
			{
				Vector3D line1 = triangle.Point1 - triangle.Point0;

				DoubleVector from = new DoubleVector(line1, Math3D.GetOrthogonal(line1, triangle.Point2 - triangle.Point0));
				DoubleVector to = new DoubleVector(new Vector3D(1, 0, 0), new Vector3D(0, 1, 0));

				Quaternion rotation = from.GetAngleAroundAxis(to);

				Transform3DGroup retVal = new Transform3DGroup();

				//	Rotate
				retVal.Children.Add(new RotateTransform3D(new QuaternionRotation3D(rotation)));

				//	Then translate
				retVal.Children.Add(new TranslateTransform3D(0, 0, -triangle.Point0.Z));

				return retVal;
			}

			#endregion
		}

		#endregion
		#region Class: QuickHull2DResult

		public class QuickHull2DResult
		{
			public QuickHull2DResult(Point[] points, int[] perimiterLines, Transform3D transform)
			{
				this.Points = points;
				this.PerimiterLines = perimiterLines;
				this.Transform = transform;
			}

			public readonly Point[] Points;
			public readonly int[] PerimiterLines;
			private readonly Transform3D Transform;

			public bool IsInside(Point point)
			{
				for (int cntr = 0; cntr < this.Points.Length - 1; cntr++)
				{
					if (!QuickHull2D.IsRightOfLine(cntr, cntr + 1, point, this.Points))
					{
						return false;
					}
				}

				if (!QuickHull2D.IsRightOfLine(this.Points.Length - 1, 0, point, this.Points))
				{
					return false;
				}

				return true;
			}
			public Point? GetTransformedPoint(Point3D point)
			{
				//	Use the transform to rotate/translate the point to the z plane
				Point3D transformed = point;
				if (this.Transform != null)
				{
					transformed = this.Transform.Transform(point);
				}

				//	Only return a value if it's now in the z plane, which will only work if the point passed in is in the same plane as this.Points
				if (Math3D.IsNearZero(transformed.Z))
				{
					return new Point(transformed.X, transformed.Y);
				}
				else
				{
					return null;
				}
			}
		}

		#endregion
		#region Class: QuickHull3D

		private static class QuickHull3D
		{
			#region Class: TriangleWithPoints

			/// <summary>
			/// This links a triangle with a set of points that sit "outside" the triangle
			/// </summary>
			public class TriangleWithPoints : TriangleIndexedLinked
			{
				public TriangleWithPoints()
					: base()
				{
					this.OutsidePoints = new List<int>();
				}

				public TriangleWithPoints(int index0, int index1, int index2, Point3D[] allPoints)
					: base(index0, index1, index2, allPoints)
				{
					this.OutsidePoints = new List<int>();
				}

				public List<int> OutsidePoints
				{
					get;
					private set;
				}
			}

			#endregion

			public static TriangleIndexed[] GetConvexHull(Point3D[] points)
			{
				if (points.Length < 4)
				{
					//throw new ArgumentException("There must be at least 4 points", "points");
					return null;
				}

				//	Pick 4 points
				int[] startPoints = GetStartingTetrahedron(points);
				List<TriangleWithPoints> retVal = ConvertStartingPointsToTriangles(startPoints, points);

				//	If any triangle has any points outside the hull, then remove that triangle, and replace it with triangles connected to the
				//	farthest out point (relative to the triangle that got removed)
				bool foundOne;
				do
				{
					foundOne = false;
					int index = 0;
					while (index < retVal.Count)
					{
						if (retVal[index].OutsidePoints.Count > 0)
						{
							foundOne = true;
							ProcessTriangle(retVal, index, points);
						}
						else
						{
							index++;
						}
					}
				} while (foundOne);

				if (retVal.Count == 0)
				{
					//	Found a case where the points were nearly coplanar, and adding another point wiped out the whole hull
					return null;
				}

				//	Exit Function
				return retVal.ToArray();
			}

			#region Private Methods

			/// <summary>
			/// I've seen various literature call this initial tetrahedron a simplex
			/// </summary>
			private static int[] GetStartingTetrahedron(Point3D[] points)
			{
				int[] retVal = new int[4];

				//	Validate the point cloud is 3D, also get the points that have the smallest and largest X (an exception is thrown if they are the same point)
				int minXIndex, maxXIndex;
				GetStartingTetrahedronSprtGetMinMaxX(out minXIndex, out maxXIndex, points);

				//	The first two return points will be the ones with the min and max X values
				retVal[0] = minXIndex;
				retVal[1] = maxXIndex;

				//	The third point will be the one that is farthest from the line defined by the first two
				int thirdIndex = GetStartingTetrahedronSprtFarthestFromLine(minXIndex, maxXIndex, points);
				retVal[2] = thirdIndex;

				//	The fourth point will be the one that is farthest from the plane defined by the first three
				int fourthIndex = GetStartingTetrahedronSprtFarthestFromPlane(minXIndex, maxXIndex, thirdIndex, points);
				retVal[3] = fourthIndex;

				//	Exit Function
				return retVal;
			}
			/// <summary>
			/// This does more than just look at X.  It looks at Y and Z as well to verify the point cloud has points in all 3 dimensions
			/// </summary>
			private static void GetStartingTetrahedronSprtGetMinMaxX(out int minXIndex, out int maxXIndex, Point3D[] points)
			{
				//	Create arrays to hold the min and max along each axis (0=X, 1=Y, 2=Z) - using the first point
				double[] minValues = new double[] { points[0].X, points[0].Y, points[0].Z };
				double[] maxValues = new double[] { points[0].X, points[0].Y, points[0].Z };
				int[] minIndicies = new int[] { 0, 0, 0 };
				int[] maxIndicies = new int[] { 0, 0, 0 };

				for (int cntr = 1; cntr < points.Length; cntr++)
				{
					#region Examine Point

					//	Min
					if (points[cntr].X < minValues[0])
					{
						minValues[0] = points[cntr].X;
						minIndicies[0] = cntr;
					}

					if (points[cntr].Y < minValues[1])
					{
						minValues[1] = points[cntr].Y;
						minIndicies[1] = cntr;
					}

					if (points[cntr].Z < minValues[2])
					{
						minValues[2] = points[cntr].Z;
						minIndicies[2] = cntr;
					}

					//	Max
					if (points[cntr].X > maxValues[0])
					{
						maxValues[0] = points[cntr].X;
						maxIndicies[0] = cntr;
					}

					if (points[cntr].Y > maxValues[1])
					{
						maxValues[1] = points[cntr].Y;
						maxIndicies[1] = cntr;
					}

					if (points[cntr].Z > maxValues[2])
					{
						maxValues[2] = points[cntr].Z;
						maxIndicies[2] = cntr;
					}

					#endregion
				}

				#region Validate

				for (int cntr = 0; cntr < 3; cntr++)
				{
					if (maxValues[cntr] == minValues[cntr])
					{
						throw new ApplicationException("The points passed in aren't in 3D - they are either all on the same point (0D), or colinear (1D), or coplanar (2D)");
					}
				}

				#endregion

				//	Return the two points that are the min/max X
				minXIndex = minIndicies[0];
				maxXIndex = maxIndicies[0];
			}
			/// <summary>
			/// Finds the point that is farthest from the line
			/// </summary>
			private static int GetStartingTetrahedronSprtFarthestFromLine(int index1, int index2, Point3D[] points)
			{
				Vector3D lineDirection = points[index2] - points[index1];		//	The nearest point method wants a vector, so calculate it once
				Point3D startPoint = points[index1];		//	not sure if there is much of a penalty to referencing a list by index, but I figure I'll just cache this point once

				double maxDistance = -1d;
				int retVal = -1;

				for (int cntr = 0; cntr < points.Length; cntr++)
				{
					if (cntr == index1 || cntr == index2)
					{
						continue;
					}

					//	Calculate the distance from the line
					Point3D nearestPoint = Math3D.GetNearestPointAlongLine(startPoint, lineDirection, points[cntr]);
					double distanceSquared = (points[cntr] - nearestPoint).LengthSquared;

					if (distanceSquared > maxDistance)
					{
						maxDistance = distanceSquared;
						retVal = cntr;
					}
				}

				if (retVal < 0)
				{
					throw new ApplicationException("Didn't find a return point, this should never happen");
				}

				//	Exit Function
				return retVal;
			}
			private static int GetStartingTetrahedronSprtFarthestFromPlane(int index1, int index2, int index3, Point3D[] points)
			{
				//NOTE:  I'm copying bits of Math3D.DistanceFromPlane here for optimization reasons
				Vector3D[] triangle = new Vector3D[] { points[index1].ToVector(), points[index2].ToVector(), points[index3].ToVector() };
				Vector3D normal = Math3D.Normal(triangle);
				double originDistance = Math3D.GetPlaneDistance(normal, triangle[0].ToPoint());

				double maxDistance = 0d;
				int retVal = -1;

				for (int cntr = 0; cntr < points.Length; cntr++)
				{
					if (cntr == index1 || cntr == index2 || cntr == index3)
					{
						continue;
					}

					//NOTE:  This is Math3D.DistanceFromPlane copied inline to speed things up
					Point3D point = points[cntr];
					double distance = ((normal.X * point.X) +					// Ax +
													(normal.Y * point.Y) +					// Bx +
													(normal.Z * point.Z)) + originDistance;	// Cz + D

					//	I don't care which side of the triangle the point is on for this initial tetrahedron
					distance = Math.Abs(distance);

					if (distance > maxDistance)
					{
						maxDistance = distance;
						retVal = cntr;
					}
				}

				if (retVal < 0)
				{
					throw new ApplicationException("Didn't find a return point, this should never happen");
				}

				//	Exit Function
				return retVal;
			}

			private static List<TriangleWithPoints> ConvertStartingPointsToTriangles(int[] startPoints, Point3D[] allPoints)
			{
				if (startPoints.Length != 4)
				{
					throw new ArgumentException("This method expects exactly 4 points passed in");
				}

				List<TriangleWithPoints> retVal = new List<TriangleWithPoints>();

				//	Make triangles
				retVal.Add(CreateTriangle(startPoints[0], startPoints[1], startPoints[2], allPoints[startPoints[3]], allPoints, null));
				retVal.Add(CreateTriangle(startPoints[3], startPoints[1], startPoints[0], allPoints[startPoints[2]], allPoints, null));
				retVal.Add(CreateTriangle(startPoints[3], startPoints[2], startPoints[1], allPoints[startPoints[0]], allPoints, null));
				retVal.Add(CreateTriangle(startPoints[3], startPoints[0], startPoints[2], allPoints[startPoints[1]], allPoints, null));

				//	Link triangles together
				TriangleIndexedLinked.LinkTriangles_Edges(retVal.ConvertAll(o => (TriangleIndexedLinked)o).ToList(), true);

				#region Calculate outside points

				//	GetOutsideSet wants indicies to the points it needs to worry about.  This initial call needs all points
				List<int> allPointIndicies = Enumerable.Range(0, allPoints.Length).ToList();

				//	Remove the indicies that are in the return triangles (I ran into a case where 4 points were passed in, but they were nearly coplanar - enough
				//	that GetOutsideSet's Math3D.IsNearZero included it)
				foreach (int index in retVal.SelectMany(o => o.IndexArray).Distinct())
				{
					allPointIndicies.Remove(index);
				}

				//	For every triangle, find the points that are outside the polygon (not the points behind the triangle)
				//	Note that a point will never be shared between triangles
				foreach (TriangleWithPoints triangle in retVal)
				{
					if (allPointIndicies.Count > 0)
					{
						triangle.OutsidePoints.AddRange(GetOutsideSet(triangle, allPointIndicies, allPoints));
					}
				}

				#endregion

				//	Exit Function
				return retVal;
			}

			/// <summary>
			/// This works on a triangle that contains outside points.  It removes the triangle, and creates new ones connected to
			/// the outermost outside point
			/// </summary>
			private static void ProcessTriangle(List<TriangleWithPoints> hull, int hullIndex, Point3D[] allPoints)
			{
				TriangleWithPoints removedTriangle = hull[hullIndex];

				//	Find the farthest point from this triangle
				int fartherstIndex = ProcessTriangleSprtFarthestPoint(removedTriangle);
				if (fartherstIndex < 0)
				{
					//	The outside points are on the same plane as this triangle (and sitting within the bounds of the triangle).
					//	Just wipe the points and go away
					removedTriangle.OutsidePoints.Clear();
					return;
					//throw new ApplicationException(string.Format("Couldn't find a farthest point for triangle\r\n{0}\r\n\r\n{1}\r\n",
					//    removedTriangle.ToString(),
					//    string.Join("\r\n", removedTriangle.OutsidePoints.Select(o => o.Item1.ToString() + "   |   " + allPoints[o.Item1].ToString(true)).ToArray())));		//	this should never happen
				}

				//Key=which triangles to remove from the hull
				//Value=meaningless, I just wanted a sorted list
				SortedList<TriangleIndexedLinked, int> removedTriangles = new SortedList<TriangleIndexedLinked, int>();

				//Key=triangle on the hull that is a boundry (the new triangles will be added to these boundry triangles)
				//Value=the key's edges that are exposed to the removed triangles
				SortedList<TriangleIndexedLinked, List<TriangleEdge>> removedRim = new SortedList<TriangleIndexedLinked, List<TriangleEdge>>();

				//	Find all the triangles that can see this point (they will need to be removed from the hull)
				//NOTE:  This method is recursive
				ProcessTriangleSprtRemove(removedTriangle, removedTriangles, removedRim, allPoints[fartherstIndex].ToVector());

				//	Remove these from the hull
				ProcessTriangleSprtRemoveFromHull(hull, removedTriangles.Keys, removedRim);

				//	Get all the outside points
				//List<int> allOutsidePoints = removedTriangles.Keys.SelectMany(o => ((TriangleWithPoints)o).OutsidePoints).Distinct().ToList();		//	if you try to read what SelectMany does, it gets pretty wordy and unclear.  It just does a foreach across all the OutsidePoints lists from all the removedTriangles (treats the list of lists like a single list)
				List<int> allOutsidePoints = removedTriangles.Keys.SelectMany(o => ((TriangleWithPoints)o).OutsidePoints).ToList();		//	there's no need to call distinct, since the outside points aren't shared between triangles

				//	Create new triangles
				ProcessTriangleSprtNew(hull, fartherstIndex, removedRim, allPoints, allOutsidePoints);		//	Note that after this method, allOutsidePoints will only be the points left over (the ones that are inside the hull)
			}
			private static int ProcessTriangleSprtFarthestPoint(TriangleWithPoints triangle)
			{
				//NOTE: This method is nearly a copy of GetOutsideSet

				Vector3D[] polygon = new Vector3D[] { triangle.Point0.ToVector(), triangle.Point1.ToVector(), triangle.Point2.ToVector() };

				List<int> pointIndicies = triangle.OutsidePoints;
				Point3D[] allPoints = triangle.AllPoints;

				double maxDistance = 0d;
				int retVal = -1;

				for (int cntr = 0; cntr < pointIndicies.Count; cntr++)
				{
					double distance = Math3D.DistanceFromPlane(polygon, allPoints[pointIndicies[cntr]].ToVector());

					//	Distance should never be negative (or the point wouldn't be in the list of outside points).  If for some reason there is one with a negative distance,
					//	it shouldn't be considered, because it sits inside the hull
					if (distance > maxDistance)
					{
						maxDistance = distance;
						retVal = pointIndicies[cntr];
					}
					else if (Math3D.IsNearZero(distance) && Math3D.IsNearZero(maxDistance))		//	this is for a coplanar point that can have a very slightly negative distance
					{
						//	Can't trust the previous bary check, need another one (maybe it's false because it never went through that first check?)
						Vector bary = Math3D.ToBarycentric(triangle, allPoints[pointIndicies[cntr]]);
						if (bary.X < 0d || bary.Y < 0d || bary.X + bary.Y > 1d)
						{
							maxDistance = 0d;
							retVal = pointIndicies[cntr];
						}
					}
				}

				//	Exit Function
				return retVal;
			}

			private static void ProcessTriangleSprtRemove(TriangleIndexedLinked triangle, SortedList<TriangleIndexedLinked, int> removedTriangles, SortedList<TriangleIndexedLinked, List<TriangleEdge>> removedRim, Vector3D farPoint)
			{
				//	This triangle will need to be removed.  Keep track of it so it's not processed again (the int means nothing)
				removedTriangles.Add(triangle, 0);

				//	Try each neighbor
				ProcessTriangleSprtRemoveSprtNeighbor(triangle.Neighbor_01, removedTriangles, removedRim, farPoint, triangle);
				ProcessTriangleSprtRemoveSprtNeighbor(triangle.Neighbor_12, removedTriangles, removedRim, farPoint, triangle);
				ProcessTriangleSprtRemoveSprtNeighbor(triangle.Neighbor_20, removedTriangles, removedRim, farPoint, triangle);
			}
			private static void ProcessTriangleSprtRemoveSprtNeighbor(TriangleIndexedLinked triangle, SortedList<TriangleIndexedLinked, int> removedTriangles, SortedList<TriangleIndexedLinked, List<TriangleEdge>> removedRim, Vector3D farPoint, TriangleIndexedLinked fromTriangle)
			{
				if (removedTriangles.ContainsKey(triangle))
				{
					return;
				}

				if (removedRim.ContainsKey(triangle))
				{
					//	This triangle is already recognized as part of the hull.  Add a link from it to the from triangle (because two of its edges
					//	are part of the hull rim)
					removedRim[triangle].Add(triangle.WhichEdge(fromTriangle));
					return;
				}

				//	Need to subtract the far point from some point on this triangle, so that it's a vector from the triangle to the
				//	far point, and not from the origin
				double dot = Vector3D.DotProduct(triangle.Normal, (farPoint - triangle.Point0).ToVector());
				if (dot >= 0d || Math3D.IsNearZero(dot))		//	0 is coplanar, -1 is the opposite side
				{
					//	This triangle is visible to the point.  Remove it (recurse)
					ProcessTriangleSprtRemove(triangle, removedTriangles, removedRim, farPoint);
				}
				else
				{
					//	This triangle is invisible to the point, so needs to stay part of the hull.  Store this boundry
					removedRim.Add(triangle, new List<TriangleEdge>());
					removedRim[triangle].Add(triangle.WhichEdge(fromTriangle));
				}
			}

			private static void ProcessTriangleSprtRemoveFromHull(List<TriangleWithPoints> hull, IList<TriangleIndexedLinked> trianglesToRemove, SortedList<TriangleIndexedLinked, List<TriangleEdge>> removedRim)
			{
				//	Remove from the hull list
				foreach (TriangleIndexedLinked triangle in trianglesToRemove)
				{
					hull.Remove((TriangleWithPoints)triangle);
				}

				//	Break the links from the hull to the removed triangles (there's no need to break links in the other direction.  The removed triangles
				//	will become orphaned, and eventually garbage collected)
				foreach (TriangleIndexedLinked triangle in removedRim.Keys)
				{
					foreach (TriangleEdge edge in removedRim[triangle])
					{
						triangle.SetNeighbor(edge, null);
					}
				}
			}

			private static void ProcessTriangleSprtNew(List<TriangleWithPoints> hull, int fartherstIndex, SortedList<TriangleIndexedLinked, List<TriangleEdge>> removedRim, Point3D[] allPoints, List<int> outsidePoints)
			{
				List<TriangleWithPoints> newTriangles = new List<TriangleWithPoints>();

				//	Run around the rim, and build a triangle between the far point and each edge
				foreach (TriangleIndexedLinked rimTriangle in removedRim.Keys)
				{
					//	Get a point that is toward the hull (the created triangle will be built so its normal points away from this)
					Point3D insidePoint = ProcessTriangleSprtNewSprtInsidePoint(rimTriangle, removedRim[rimTriangle]);

					foreach (TriangleEdge rimEdge in removedRim[rimTriangle])
					{
						//	Get the points for this edge
						int index1, index2;
						rimTriangle.GetIndices(out index1, out index2, rimEdge);

						//	Build the triangle
						TriangleWithPoints triangle = CreateTriangle(fartherstIndex, index1, index2, insidePoint, allPoints, rimTriangle);

						//	Now link this triangle with the boundry triangle (just the one edge, the other edges will be joined later)
						TriangleIndexedLinked.LinkTriangles_Edges(triangle, rimTriangle);

						//	Store this triangle
						newTriangles.Add(triangle);
						hull.Add(triangle);
					}
				}

				//	The new triangles are linked to the boundry already.  Now link the other two edges to each other (newTriangles forms a
				//	triangle fan, but they aren't neccessarily consecutive)
				TriangleIndexedLinked.LinkTriangles_Edges(newTriangles.ConvertAll(o => (TriangleIndexedLinked)o).ToList(), false);

				//	Distribute the outside points to these new triangles
				foreach (TriangleWithPoints triangle in newTriangles)
				{
					//	Find the points that are outside the polygon (not the points behind the triangle)
					//	Note that a point will never be shared between triangles
					triangle.OutsidePoints.AddRange(GetOutsideSet(triangle, outsidePoints, allPoints));
				}
			}
			private static Point3D ProcessTriangleSprtNewSprtInsidePoint(TriangleIndexedLinked rimTriangle, List<TriangleEdge> sharedEdges)
			{
				bool[] used = new bool[3];

				//	Figure out which indices are used
				foreach (TriangleEdge edge in sharedEdges)
				{
					switch (edge)
					{
						case TriangleEdge.Edge_01:
							used[0] = true;
							used[1] = true;
							break;

						case TriangleEdge.Edge_12:
							used[1] = true;
							used[2] = true;
							break;

						case TriangleEdge.Edge_20:
							used[2] = true;
							used[0] = true;
							break;

						default:
							throw new ApplicationException("Unknown TriangleEdge: " + edge.ToString());
					}
				}

				//	Find one that isn't used
				for (int cntr = 0; cntr < 3; cntr++)
				{
					if (!used[cntr])
					{
						return rimTriangle[cntr];
					}
				}

				//	Project a point away from this triangle
				//TODO:  If the hull ends up concave, this is a very likely culprit.  May need to come up with a better way
				return rimTriangle.GetCenterPoint() + (rimTriangle.Normal * -.001);		//	by not using the unit normal, I'm keeping this scaled roughly to the size of the triangle
			}

			/// <summary>
			/// This takes in 3 points that belong to the triangle, and a point that is not on the triangle, but is toward the rest
			/// of the hull.  It then creates a triangle whose normal points away from the hull (right hand rule)
			/// </summary>
			private static TriangleWithPoints CreateTriangle(int point0, int point1, int point2, Point3D pointWithinHull, Point3D[] allPoints, ITriangle neighbor)
			{
				//	Try an arbitrary orientation
				TriangleWithPoints retVal = new TriangleWithPoints(point0, point1, point2, allPoints);

				//	Get a vector pointing from point0 to the inside point
				Vector3D towardHull = pointWithinHull - allPoints[point0];

				double dot = Vector3D.DotProduct(towardHull, retVal.Normal);
				if (dot > 0d)
				{
					//	When the dot product is greater than zero, that means the normal points in the same direction as the vector that points
					//	toward the hull.  So buid a triangle that points in the opposite direction
					retVal = new TriangleWithPoints(point0, point2, point1, allPoints);
				}
				else if (dot == 0d)
				{
					//	This new triangle is coplanar with the neighbor triangle, so pointWithinHull can't be used to figure out if this return
					//	triangle is facing the correct way.  Instead, make it point the same direction as the neighbor triangle
					dot = Vector3D.DotProduct(retVal.Normal, neighbor.Normal);
					if (dot < 0)
					{
						retVal = new TriangleWithPoints(point0, point2, point1, allPoints);
					}
				}

				//	Exit Function
				return retVal;
			}

			/// <summary>
			/// This returns the subset of points that are on the outside facing side of the triangle
			/// </summary>
			/// <remarks>
			/// I got these steps here:
			/// http://www.gamedev.net/topic/106765-determining-if-a-point-is-in-front-of-or-behind-a-plane/
			/// </remarks>
			/// <param name="pointIndicies">
			/// This method will only look at the points in pointIndicies.
			/// NOTE: This method will also remove values from here if they are part of an existing triangle, or get added to a triangle's outside list.
			/// </param>
			private static List<int> GetOutsideSet(TriangleIndexed triangle, List<int> pointIndicies, Point3D[] allPoints)
			{
				List<int> retVal = new List<int>();

				//	Compute D, using a arbitrary point P, that lies on the plane: D = - (Nx*Px + Ny*Py + Nz*Pz); Don't forget the inversion !
				double D = -((triangle.NormalUnit.X * triangle.Point0.X) + (triangle.NormalUnit.Y * triangle.Point0.Y) + (triangle.NormalUnit.Z * triangle.Point0.Z));

				int cntr = 0;
				while (cntr < pointIndicies.Count)
				{
					int index = pointIndicies[cntr];

					if (index == triangle.Index0 || index == triangle.Index1 || index == triangle.Index2)
					{
						pointIndicies.Remove(index);		//	no need to consider this for future calls
						continue;
					}

					//	You can test a point (T) with respect to the plane using the plane equation: res = Nx*Tx + Ny*Ty + Nz*Tz + D
					double res = (triangle.NormalUnit.X * allPoints[index].X) + (triangle.NormalUnit.Y * allPoints[index].Y) + (triangle.NormalUnit.Z * allPoints[index].Z) + D;

					if (res > 0d)		//	anything greater than zero lies outside the plane
					{
						retVal.Add(index);
						pointIndicies.Remove(index);		//	an outside point can only belong to one triangle
					}
					else if (Math3D.IsNearZero(res))
					{
						//	This point is coplanar.  Only consider it an outside point if it is outside the bounds of this triangle
						Vector bary = Math3D.ToBarycentric(triangle, allPoints[index]);
						if (bary.X < 0d || bary.Y < 0d || bary.X + bary.Y > 1d)
						{
							retVal.Add(index);
							pointIndicies.Remove(index);		//	an outside point can only belong to one triangle
						}
						else
						{
							cntr++;
						}
					}
					else
					{
						cntr++;
					}
				}

				return retVal;
			}

			#endregion
		}

		#endregion

		#region Declaration Section

		[StructLayout(LayoutKind.Sequential)]
		internal struct Win32Point
		{
			public Int32 X;
			public Int32 Y;
		}

		[StructLayout(LayoutKind.Sequential)]
		public class POINT
		{
			public int x = 0; public int y = 0;
		}

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool GetCursorPos(ref Win32Point pt);

		[DllImport("User32", EntryPoint = "ClientToScreen", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
		private static extern int ClientToScreen(IntPtr hWnd, [In, Out] POINT pt);

		#endregion

		/// <summary>
		/// This returns a color that is the result of the two colors blended
		/// </summary>
		/// <param name="alpha">0 is all back color, 1 is all fore color, .5 is half way between</param>
		public static Color AlphaBlend(Color foreColor, Color backColor, double alpha)
		{
			//	Figure out the new color (I use integers instead of bytes, so I can gracefully handle overflows)
			int aNew, rNew, gNew, bNew;
			if (foreColor.A == 0)
			{
				//	Fore is completely transparent, so only worry about blending the alpha
				aNew = Convert.ToInt32(Convert.ToDouble(backColor.A) + (((Convert.ToDouble(foreColor.A) - Convert.ToDouble(backColor.A)) / 255) * alpha * 255));
				rNew = backColor.R;
				gNew = backColor.G;
				bNew = backColor.B;
			}
			else if (backColor.A == 0)
			{
				//	Back is completely transparent, so only worry about blending the alpha
				aNew = Convert.ToInt32(Convert.ToDouble(backColor.A) + (((Convert.ToDouble(foreColor.A) - Convert.ToDouble(backColor.A)) / 255) * alpha * 255));
				rNew = foreColor.R;
				gNew = foreColor.G;
				bNew = foreColor.B;
			}
			else
			{
				aNew = Convert.ToInt32(Convert.ToDouble(backColor.A) + (((Convert.ToDouble(foreColor.A) - Convert.ToDouble(backColor.A)) / 255) * alpha * 255));
				rNew = Convert.ToInt32(Convert.ToDouble(backColor.R) + (((Convert.ToDouble(foreColor.R) - Convert.ToDouble(backColor.R)) / 255) * alpha * 255));
				gNew = Convert.ToInt32(Convert.ToDouble(backColor.G) + (((Convert.ToDouble(foreColor.G) - Convert.ToDouble(backColor.G)) / 255) * alpha * 255));
				bNew = Convert.ToInt32(Convert.ToDouble(backColor.B) + (((Convert.ToDouble(foreColor.B) - Convert.ToDouble(backColor.B)) / 255) * alpha * 255));
			}

			//	Make sure the values are in range
			if (aNew < 0)
			{
				aNew = 0;
			}
			else if (aNew > 255)
			{
				aNew = 255;
			}

			if (rNew < 0)
			{
				rNew = 0;
			}
			else if (rNew > 255)
			{
				rNew = 255;
			}

			if (gNew < 0)
			{
				gNew = 0;
			}
			else if (gNew > 255)
			{
				gNew = 255;
			}

			if (bNew < 0)
			{
				bNew = 0;
			}
			else if (bNew > 255)
			{
				bNew = 255;
			}

			//	Exit Function
			return Color.FromArgb(Convert.ToByte(aNew), Convert.ToByte(rNew), Convert.ToByte(gNew), Convert.ToByte(bNew));
		}

		public static Color GetRandomColor(byte alpha, byte min, byte max)
		{
			Random rand = StaticRandom.GetRandomForThread();
			return Color.FromArgb(alpha, Convert.ToByte(rand.Next(min, max + 1)), Convert.ToByte(rand.Next(min, max + 1)), Convert.ToByte(rand.Next(min, max + 1)));
		}
		public static Color GetRandomColor(byte alpha, byte minRed, byte maxRed, byte minGreen, byte maxGreen, byte minBlue, byte maxBlue)
		{
			Random rand = StaticRandom.GetRandomForThread();
			return Color.FromArgb(alpha, Convert.ToByte(rand.Next(minRed, maxRed + 1)), Convert.ToByte(rand.Next(minGreen, maxGreen + 1)), Convert.ToByte(rand.Next(minBlue, maxBlue + 1)));
		}

		/// <summary>
		/// This is just a wrapper to the color converter (why can't they have a method off the color class with all
		/// the others?)
		/// </summary>
		public static Color ColorFromHex(string hexValue)
		{
			if (hexValue.StartsWith("#"))
			{
				return (Color)ColorConverter.ConvertFromString(hexValue);
			}
			else
			{
				return (Color)ColorConverter.ConvertFromString("#" + hexValue);
			}
		}
		public static string ColorToHex(Color color)
		{
			//	I think color.ToString does the same thing, but this is explicit
			return "#" + color.A.ToString("X2") + color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");
		}

		/// <summary>
		/// Returns the color as hue/saturation/value
		/// </summary>
		/// <remarks>
		/// Got this here:
		/// http://stackoverflow.com/questions/4123998/algorithm-to-switch-between-rgb-and-hsb-color-values
		/// </remarks>
		/// <param name="h">from 0 to 360</param>
		/// <param name="s">from 0 to 100</param>
		/// <param name="v">from 0 to 100</param>
		public static void RGBtoHSV(out double h, out double s, out double v, Color color)
		{
			// Normalize the RGB values by scaling them to be between 0 and 1
			double red = color.R / 255d;
			double green = color.G / 255d;
			double blue = color.B / 255d;

			double minValue = Math.Min(red, Math.Min(green, blue));
			double maxValue = Math.Max(red, Math.Max(green, blue));		//	TODO: don't use math.max, then switch.  Instead, do the if statements manually
			double delta = maxValue - minValue;

			v = maxValue;

			#region Get Hue

			//	Calculate the hue (in degrees of a circle, between 0 and 360)
			if (red >= green && red >= blue)
			{
				if (green >= blue)
				{
					if (delta <= 0)
					{
						h = 0d;
					}
					else
					{
						h = 60d * (green - blue) / delta;
					}
				}
				else
				{
					h = 60d * (green - blue) / delta + 360d;
				}
			}
			else if (green >= red && green >= blue)
			{
				h = 60d * (blue - red) / delta + 120d;
			}
			else //if (blue >= red && blue >= green)
			{
				h = 60d * (red - green) / delta + 240d;
			}

			#endregion

			//	Calculate the saturation (between 0 and 1)
			if (maxValue == 0d)
			{
				s = 0d;
			}
			else
			{
				s = 1d - (minValue / maxValue);
			}

			//	Scale the saturation and value to a percentage between 0 and 100
			s *= 100d;
			v *= 100d;

			#region Cap Values

			if (h < 0d)
			{
				h = 0d;
			}
			else if (h > 360d)
			{
				h = 360d;
			}

			if (s < 0d)
			{
				s = 0d;
			}
			else if (s > 100d)
			{
				s = 100d;
			}

			if (v < 0d)
			{
				v = 0d;
			}
			else if (v > 100d)
			{
				v = 100d;
			}

			#endregion
		}

		public static Color HSVtoRGB(double h, double s, double v)
		{
			return HSVtoRGB(255, h, s, v);
		}
		/// <summary>
		/// Converts hue/saturation/value to a color
		/// </summary>
		/// <remarks>
		/// Got this here:
		/// http://stackoverflow.com/questions/4123998/algorithm-to-switch-between-rgb-and-hsb-color-values
		/// </remarks>
		/// <param name="a">0 to 255</param>
		/// <param name="h">0 to 360</param>
		/// <param name="s">0 to 100</param>
		/// <param name="v">0 to 100</param>
		public static Color HSVtoRGB(byte a, double h, double s, double v)
		{
			//	Scale the Saturation and Value components to be between 0 and 1
			double hue = h;
			double sat = s / 100d;
			double val = v / 100d;

			double r, g, b;		//	these go between 0 and 1

			if (sat == 0d)
			{
				#region Gray

				//	If the saturation is 0, then all colors are the same.
				//	(This is some flavor of gray.)
				r = val;
				g = val;
				b = val;

				#endregion
			}
			else
			{
				#region Color

				//	Calculate the appropriate sector of a 6-part color wheel
				double sectorPos = hue / 60d;
				int sectorNumber = Convert.ToInt32(Math.Floor(sectorPos));

				//	Get the fractional part of the sector (that is, how many degrees into the sector you are)
				double fractionalSector = sectorPos - sectorNumber;

				//	Calculate values for the three axes of the color
				double p = val * (1d - sat);
				double q = val * (1d - (sat * fractionalSector));
				double t = val * (1d - (sat * (1d - fractionalSector)));

				//	Assign the fractional colors to red, green, and blue
				//	components based on the sector the angle is in
				switch (sectorNumber)
				{
					case 0:
					case 6:
						r = val;
						g = t;
						b = p;
						break;

					case 1:
						r = q;
						g = val;
						b = p;
						break;

					case 2:
						r = p;
						g = val;
						b = t;
						break;

					case 3:
						r = p;
						g = q;
						b = val;
						break;

					case 4:
						r = t;
						g = p;
						b = val;
						break;

					case 5:
						r = val;
						g = p;
						b = q;
						break;

					default:
						throw new ArgumentException("Invalid hue: " + h.ToString());
				}

				#endregion
			}

			#region Scale/Cap 255

			//	Scale to 255 (using int to make it easier to handle overflow)
			int rNew = Convert.ToInt32(Math.Round(r * 255d));
			int gNew = Convert.ToInt32(Math.Round(g * 255d));
			int bNew = Convert.ToInt32(Math.Round(b * 255d));

			//	Make sure the values are in range
			if (rNew < 0)
			{
				rNew = 0;
			}
			else if (rNew > 255)
			{
				rNew = 255;
			}

			if (gNew < 0)
			{
				gNew = 0;
			}
			else if (gNew > 255)
			{
				gNew = 255;
			}

			if (bNew < 0)
			{
				bNew = 0;
			}
			else if (bNew > 255)
			{
				bNew = 255;
			}

			#endregion

			//	Exit Function
			return Color.FromArgb(a, Convert.ToByte(rNew), Convert.ToByte(gNew), Convert.ToByte(bNew));
		}

		public static bool IsTransparent(Color color)
		{
			return color.A == 0;
		}
		public static bool IsTransparent(Brush brush)
		{
			if (brush is SolidColorBrush)
			{
				return IsTransparent(((SolidColorBrush)brush).Color);
			}
			else if (brush is GradientBrush)
			{
				GradientBrush brushCast = (GradientBrush)brush;
				if (brushCast.Opacity == 0d)
				{
					return true;
				}

				return !brushCast.GradientStops.Any(o => !IsTransparent(o.Color));		//	if any are non-transparent, return false
			}

			//	Not sure what it is, probably a bitmap or something, so just assume it's not transparent
			return false;
		}

		public static MeshGeometry3D GetSquare2D(double size)
		{
			double halfSize = size / 2d;

			// Define 3D mesh object
			MeshGeometry3D retVal = new MeshGeometry3D();

			retVal.Positions.Add(new Point3D(-halfSize, -halfSize, 0));
			retVal.Positions.Add(new Point3D(halfSize, -halfSize, 0));
			retVal.Positions.Add(new Point3D(halfSize, halfSize, 0));
			retVal.Positions.Add(new Point3D(-halfSize, halfSize, 0));

			// Face
			retVal.TriangleIndices.Add(0);
			retVal.TriangleIndices.Add(1);
			retVal.TriangleIndices.Add(2);
			retVal.TriangleIndices.Add(2);
			retVal.TriangleIndices.Add(3);
			retVal.TriangleIndices.Add(0);

			//	shouldn't I set normals?
			//retVal.Normals

			// Exit Function
			//retVal.Freeze();
			return retVal;
		}
		public static MeshGeometry3D GetCube(double size)
		{
			double halfSize = size / 2d;

			// Define 3D mesh object
			MeshGeometry3D retVal = new MeshGeometry3D();

			retVal.Positions.Add(new Point3D(-halfSize, -halfSize, halfSize));		// 0
			retVal.Positions.Add(new Point3D(halfSize, -halfSize, halfSize));		// 1
			retVal.Positions.Add(new Point3D(halfSize, halfSize, halfSize));		// 2
			retVal.Positions.Add(new Point3D(-halfSize, halfSize, halfSize));		// 3

			retVal.Positions.Add(new Point3D(-halfSize, -halfSize, -halfSize));		// 4
			retVal.Positions.Add(new Point3D(halfSize, -halfSize, -halfSize));		// 5
			retVal.Positions.Add(new Point3D(halfSize, halfSize, -halfSize));		// 6
			retVal.Positions.Add(new Point3D(-halfSize, halfSize, -halfSize));		// 7

			// Front face
			retVal.TriangleIndices.Add(0);
			retVal.TriangleIndices.Add(1);
			retVal.TriangleIndices.Add(2);
			retVal.TriangleIndices.Add(2);
			retVal.TriangleIndices.Add(3);
			retVal.TriangleIndices.Add(0);

			// Back face
			retVal.TriangleIndices.Add(6);
			retVal.TriangleIndices.Add(5);
			retVal.TriangleIndices.Add(4);
			retVal.TriangleIndices.Add(4);
			retVal.TriangleIndices.Add(7);
			retVal.TriangleIndices.Add(6);

			// Right face
			retVal.TriangleIndices.Add(1);
			retVal.TriangleIndices.Add(5);
			retVal.TriangleIndices.Add(2);
			retVal.TriangleIndices.Add(5);
			retVal.TriangleIndices.Add(6);
			retVal.TriangleIndices.Add(2);

			// Top face
			retVal.TriangleIndices.Add(2);
			retVal.TriangleIndices.Add(6);
			retVal.TriangleIndices.Add(3);
			retVal.TriangleIndices.Add(3);
			retVal.TriangleIndices.Add(6);
			retVal.TriangleIndices.Add(7);

			// Bottom face
			retVal.TriangleIndices.Add(5);
			retVal.TriangleIndices.Add(1);
			retVal.TriangleIndices.Add(0);
			retVal.TriangleIndices.Add(0);
			retVal.TriangleIndices.Add(4);
			retVal.TriangleIndices.Add(5);

			// Right face
			retVal.TriangleIndices.Add(4);
			retVal.TriangleIndices.Add(0);
			retVal.TriangleIndices.Add(3);
			retVal.TriangleIndices.Add(3);
			retVal.TriangleIndices.Add(7);
			retVal.TriangleIndices.Add(4);

			//	shouldn't I set normals?
			//retVal.Normals

			// Exit Function
			//retVal.Freeze();
			return retVal;
		}
		public static MeshGeometry3D GetCube(Point3D min, Point3D max)
		{
			// Define 3D mesh object
			MeshGeometry3D retVal = new MeshGeometry3D();

			retVal.Positions.Add(new Point3D(min.X, min.Y, max.Z));		//	0
			retVal.Positions.Add(new Point3D(max.X, min.Y, max.Z));		//	1
			retVal.Positions.Add(new Point3D(max.X, max.Y, max.Z));		//	2
			retVal.Positions.Add(new Point3D(min.X, max.Y, max.Z));		//	3

			retVal.Positions.Add(new Point3D(min.X, min.Y, min.Z));		//	4
			retVal.Positions.Add(new Point3D(max.X, min.Y, min.Z));		//	5
			retVal.Positions.Add(new Point3D(max.X, max.Y, min.Z));		//	6
			retVal.Positions.Add(new Point3D(min.X, max.Y, min.Z));		//	7

			// Front face
			retVal.TriangleIndices.Add(0);
			retVal.TriangleIndices.Add(1);
			retVal.TriangleIndices.Add(2);
			retVal.TriangleIndices.Add(2);
			retVal.TriangleIndices.Add(3);
			retVal.TriangleIndices.Add(0);

			// Back face
			retVal.TriangleIndices.Add(6);
			retVal.TriangleIndices.Add(5);
			retVal.TriangleIndices.Add(4);
			retVal.TriangleIndices.Add(4);
			retVal.TriangleIndices.Add(7);
			retVal.TriangleIndices.Add(6);

			// Right face
			retVal.TriangleIndices.Add(1);
			retVal.TriangleIndices.Add(5);
			retVal.TriangleIndices.Add(2);
			retVal.TriangleIndices.Add(5);
			retVal.TriangleIndices.Add(6);
			retVal.TriangleIndices.Add(2);

			// Top face
			retVal.TriangleIndices.Add(2);
			retVal.TriangleIndices.Add(6);
			retVal.TriangleIndices.Add(3);
			retVal.TriangleIndices.Add(3);
			retVal.TriangleIndices.Add(6);
			retVal.TriangleIndices.Add(7);

			// Bottom face
			retVal.TriangleIndices.Add(5);
			retVal.TriangleIndices.Add(1);
			retVal.TriangleIndices.Add(0);
			retVal.TriangleIndices.Add(0);
			retVal.TriangleIndices.Add(4);
			retVal.TriangleIndices.Add(5);

			// Right face
			retVal.TriangleIndices.Add(4);
			retVal.TriangleIndices.Add(0);
			retVal.TriangleIndices.Add(3);
			retVal.TriangleIndices.Add(3);
			retVal.TriangleIndices.Add(7);
			retVal.TriangleIndices.Add(4);

			//	shouldn't I set normals?
			//retVal.Normals

			// Exit Function
			//retVal.Freeze();
			return retVal;
		}
		/// <summary>
		/// GetCube shares verticies between faces, so light reflects blended and unnatural (but is a bit more efficient)
		/// This looks a bit better, but has a higher vertex count
		/// </summary>
		public static MeshGeometry3D GetCube_IndependentFaces(Point3D min, Point3D max)
		{
			// Define 3D mesh object
			MeshGeometry3D retVal = new MeshGeometry3D();

			//retVal.Positions.Add(new Point3D(min.X, min.Y, max.Z));		//	0
			//retVal.Positions.Add(new Point3D(max.X, min.Y, max.Z));		//	1
			//retVal.Positions.Add(new Point3D(max.X, max.Y, max.Z));		//	2
			//retVal.Positions.Add(new Point3D(min.X, max.Y, max.Z));		//	3

			//retVal.Positions.Add(new Point3D(min.X, min.Y, min.Z));		//	4
			//retVal.Positions.Add(new Point3D(max.X, min.Y, min.Z));		//	5
			//retVal.Positions.Add(new Point3D(max.X, max.Y, min.Z));		//	6
			//retVal.Positions.Add(new Point3D(min.X, max.Y, min.Z));		//	7

			// Front face
			retVal.Positions.Add(new Point3D(min.X, min.Y, max.Z));		//	0
			retVal.Positions.Add(new Point3D(max.X, min.Y, max.Z));		//	1
			retVal.Positions.Add(new Point3D(max.X, max.Y, max.Z));		//	2
			retVal.Positions.Add(new Point3D(min.X, max.Y, max.Z));		//	3
			retVal.TriangleIndices.Add(0);
			retVal.TriangleIndices.Add(1);
			retVal.TriangleIndices.Add(2);
			retVal.TriangleIndices.Add(2);
			retVal.TriangleIndices.Add(3);
			retVal.TriangleIndices.Add(0);

			// Back face
			retVal.Positions.Add(new Point3D(min.X, min.Y, min.Z));		//	4
			retVal.Positions.Add(new Point3D(max.X, min.Y, min.Z));		//	5
			retVal.Positions.Add(new Point3D(max.X, max.Y, min.Z));		//	6
			retVal.Positions.Add(new Point3D(min.X, max.Y, min.Z));		//	7
			retVal.TriangleIndices.Add(6);
			retVal.TriangleIndices.Add(5);
			retVal.TriangleIndices.Add(4);
			retVal.TriangleIndices.Add(4);
			retVal.TriangleIndices.Add(7);
			retVal.TriangleIndices.Add(6);

			// Right face
			retVal.Positions.Add(new Point3D(max.X, min.Y, max.Z));		//	1-8
			retVal.Positions.Add(new Point3D(max.X, max.Y, max.Z));		//	2-9
			retVal.Positions.Add(new Point3D(max.X, min.Y, min.Z));		//	5-10
			retVal.Positions.Add(new Point3D(max.X, max.Y, min.Z));		//	6-11
			retVal.TriangleIndices.Add(8);		//	1
			retVal.TriangleIndices.Add(10);		//	5
			retVal.TriangleIndices.Add(9);		//	2
			retVal.TriangleIndices.Add(10);		//	5
			retVal.TriangleIndices.Add(11);		//	6
			retVal.TriangleIndices.Add(9);		//	2

			// Top face
			retVal.Positions.Add(new Point3D(max.X, max.Y, max.Z));		//	2-12
			retVal.Positions.Add(new Point3D(min.X, max.Y, max.Z));		//	3-13
			retVal.Positions.Add(new Point3D(max.X, max.Y, min.Z));		//	6-14
			retVal.Positions.Add(new Point3D(min.X, max.Y, min.Z));		//	7-15
			retVal.TriangleIndices.Add(12);		//	2
			retVal.TriangleIndices.Add(14);		//	6
			retVal.TriangleIndices.Add(13);		//	3
			retVal.TriangleIndices.Add(13);		//	3
			retVal.TriangleIndices.Add(14);		//	6
			retVal.TriangleIndices.Add(15);		//	7

			// Bottom face
			retVal.Positions.Add(new Point3D(min.X, min.Y, max.Z));		//	0-16
			retVal.Positions.Add(new Point3D(max.X, min.Y, max.Z));		//	1-17
			retVal.Positions.Add(new Point3D(min.X, min.Y, min.Z));		//	4-18
			retVal.Positions.Add(new Point3D(max.X, min.Y, min.Z));		//	5-19
			retVal.TriangleIndices.Add(19);		//	5
			retVal.TriangleIndices.Add(17);		//	1
			retVal.TriangleIndices.Add(16);		//	0
			retVal.TriangleIndices.Add(16);		//	0
			retVal.TriangleIndices.Add(18);		//	4
			retVal.TriangleIndices.Add(19);		//	5

			// Right face
			retVal.Positions.Add(new Point3D(min.X, min.Y, max.Z));		//	0-20
			retVal.Positions.Add(new Point3D(min.X, max.Y, max.Z));		//	3-21
			retVal.Positions.Add(new Point3D(min.X, min.Y, min.Z));		//	4-22
			retVal.Positions.Add(new Point3D(min.X, max.Y, min.Z));		//	7-23
			retVal.TriangleIndices.Add(22);		//	4
			retVal.TriangleIndices.Add(20);		//	0
			retVal.TriangleIndices.Add(21);		//	3
			retVal.TriangleIndices.Add(21);		//	3
			retVal.TriangleIndices.Add(23);		//	7
			retVal.TriangleIndices.Add(22);		//	4

			//	shouldn't I set normals?
			//retVal.Normals

			// Exit Function
			//retVal.Freeze();
			return retVal;
		}
		public static MeshGeometry3D GetSphere(int separators, double radius)
		{
			return GetSphere(separators, radius, radius, radius);
		}
		public static MeshGeometry3D GetSphere(int separators, double radiusX, double radiusY, double radiusZ)
		{
			double segmentRad = Math.PI / 2 / (separators + 1);
			int numberOfSeparators = 4 * separators + 4;

			MeshGeometry3D retVal = new MeshGeometry3D();

			// Calculate all the positions
			for (int e = -separators; e <= separators; e++)
			{
				double r_e = Math.Cos(segmentRad * e);
				double y_e = radiusY * Math.Sin(segmentRad * e);

				for (int s = 0; s <= (numberOfSeparators - 1); s++)
				{
					double z_s = radiusZ * r_e * Math.Sin(segmentRad * s) * (-1);
					double x_s = radiusX * r_e * Math.Cos(segmentRad * s);
					retVal.Positions.Add(new Point3D(x_s, y_e, z_s));
				}
			}
			retVal.Positions.Add(new Point3D(0, radiusY, 0));
			retVal.Positions.Add(new Point3D(0, -1 * radiusY, 0));

			// Main Body
			int maxIterate = 2 * separators;
			for (int y = 0; y < maxIterate; y++)      // phi?
			{
				for (int x = 0; x < numberOfSeparators; x++)      // theta?
				{
					retVal.TriangleIndices.Add(y * numberOfSeparators + (x + 1) % numberOfSeparators + numberOfSeparators);
					retVal.TriangleIndices.Add(y * numberOfSeparators + x + numberOfSeparators);
					retVal.TriangleIndices.Add(y * numberOfSeparators + x);

					retVal.TriangleIndices.Add(y * numberOfSeparators + x);
					retVal.TriangleIndices.Add(y * numberOfSeparators + (x + 1) % numberOfSeparators);
					retVal.TriangleIndices.Add(y * numberOfSeparators + (x + 1) % numberOfSeparators + numberOfSeparators);
				}
			}

			// Top Cap
			for (int i = 0; i < numberOfSeparators; i++)
			{
				retVal.TriangleIndices.Add(maxIterate * numberOfSeparators + i);
				retVal.TriangleIndices.Add(maxIterate * numberOfSeparators + (i + 1) % numberOfSeparators);
				retVal.TriangleIndices.Add(numberOfSeparators * (2 * separators + 1));
			}

			// Bottom Cap
			for (int i = 0; i < numberOfSeparators; i++)
			{
				retVal.TriangleIndices.Add(numberOfSeparators * (2 * separators + 1) + 1);
				retVal.TriangleIndices.Add((i + 1) % numberOfSeparators);
				retVal.TriangleIndices.Add(i);
			}

			// Exit Function
			//retVal.Freeze();
			return retVal;
		}

		public static MeshGeometry3D GetCylinder_AlongX(int numSegments, double radius, double height)
		{
			//NOTE: All the other geometries in this class are along the x axis, so I want to follow suit, but I think best along the z axis.  So I'll transform the points before commiting them to the geometry
			//TODO: This is so close to GetMultiRingedTube, the only difference is the multi ring tube has "hard" faces, and this has "soft" faces (this one shares points and normals, so the lighting is smoother)

			if (numSegments < 3)
			{
				throw new ArgumentException("numSegments must be at least 3: " + numSegments.ToString(), "numSegments");
			}

			MeshGeometry3D retVal = new MeshGeometry3D();

			#region Initial calculations

			Transform3D transform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90d));

			double halfHeight = height / 2d;
			//double deltaTheta = 2d * Math.PI / numSegments;
			//double theta = 0d;

			//Point[] points = new Point[numSegments];		//	these define a unit circle

			//for (int cntr = 0; cntr < numSegments; cntr++)
			//{
			//    points[cntr] = new Point(Math.Cos(theta), Math.Sin(theta));
			//    theta += deltaTheta;
			//}

			Point[] points = TubeRingRegularPolygon.PointsSingleton.Instance.GetPoints(numSegments);

			#endregion

			#region Side

			for (int cntr = 0; cntr < numSegments; cntr++)
			{
				retVal.Positions.Add(transform.Transform(new Point3D(points[cntr].X * radius, points[cntr].Y * radius, -halfHeight)));
				retVal.Positions.Add(transform.Transform(new Point3D(points[cntr].X * radius, points[cntr].Y * radius, halfHeight)));

				retVal.Normals.Add(transform.Transform(new Vector3D(points[cntr].X, points[cntr].Y, 0d)));		//	the normals point straight out of the side
				retVal.Normals.Add(transform.Transform(new Vector3D(points[cntr].X, points[cntr].Y, 0d)));
			}

			for (int cntr = 0; cntr < numSegments - 1; cntr++)
			{
				//	0,2,3
				retVal.TriangleIndices.Add((cntr * 2) + 0);
				retVal.TriangleIndices.Add((cntr * 2) + 2);
				retVal.TriangleIndices.Add((cntr * 2) + 3);

				//	0,3,1
				retVal.TriangleIndices.Add((cntr * 2) + 0);
				retVal.TriangleIndices.Add((cntr * 2) + 3);
				retVal.TriangleIndices.Add((cntr * 2) + 1);
			}

			//	Connecting the last 2 points to the first 2
			//	last,0,1
			int offset = (numSegments - 1) * 2;
			retVal.TriangleIndices.Add(offset + 0);
			retVal.TriangleIndices.Add(0);
			retVal.TriangleIndices.Add(1);

			//	last,1,last+1
			retVal.TriangleIndices.Add(offset + 0);
			retVal.TriangleIndices.Add(1);
			retVal.TriangleIndices.Add(offset + 1);

			#endregion

			//	Caps
			int pointOffset = retVal.Positions.Count;

			//NOTE: The normals are backward from what you'd think

			GetCylinder_AlongXSprtEndCap(ref pointOffset, retVal, points, new Vector3D(0, 0, 1), radius, radius, -halfHeight, transform);
			GetCylinder_AlongXSprtEndCap(ref pointOffset, retVal, points, new Vector3D(0, 0, -1), radius, radius, halfHeight, transform);

			// Exit Function
			//retVal.Freeze();
			return retVal;
		}
		private static void GetCylinder_AlongXSprtEndCap(ref int pointOffset, MeshGeometry3D geometry, Point[] points, Vector3D normal, double radiusX, double radiusY, double z, Transform3D transform)
		{
			//NOTE: This expects the cylinder's height to be along z, but will transform the points before commiting them to the geometry
			//TODO: This was copied from GetMultiRingedTubeSprtEndCap, make a good generic method

			#region Add points and normals

			for (int cntr = 0; cntr < points.Length; cntr++)
			{
				geometry.Positions.Add(transform.Transform(new Point3D(points[cntr].X * radiusX, points[cntr].Y * radiusY, z)));
				geometry.Normals.Add(transform.Transform(normal));
			}

			#endregion

			#region Add the triangles

			// Start with 0,1,2
			geometry.TriangleIndices.Add(pointOffset + 0);
			geometry.TriangleIndices.Add(pointOffset + 1);
			geometry.TriangleIndices.Add(pointOffset + 2);

			int lowerIndex = 2;
			int upperIndex = points.Length - 1;
			int lastUsedIndex = 0;
			bool shouldBumpLower = true;

			// Do the rest of the triangles
			while (lowerIndex < upperIndex)
			{
				geometry.TriangleIndices.Add(pointOffset + lowerIndex);
				geometry.TriangleIndices.Add(pointOffset + upperIndex);
				geometry.TriangleIndices.Add(pointOffset + lastUsedIndex);

				if (shouldBumpLower)
				{
					lastUsedIndex = lowerIndex;
					lowerIndex++;
				}
				else
				{
					lastUsedIndex = upperIndex;
					upperIndex--;
				}

				shouldBumpLower = !shouldBumpLower;
			}

			#endregion

			pointOffset += points.Length;
		}

		public static MeshGeometry3D GetCone_AlongX(int numSegments, double radius, double height)
		{
			//	This is a copy of GetCylinder_AlongX
			//TODO: This is so close to GetMultiRingedTube, the only difference is the multi ring tube has "hard" faces, and this has "soft" faces (this one shares points and normals, so the lighting is smoother)

			if (numSegments < 3)
			{
				throw new ArgumentException("numSegments must be at least 3: " + numSegments.ToString(), "numSegments");
			}

			MeshGeometry3D retVal = new MeshGeometry3D();

			#region Initial calculations

			Transform3D transform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90d));

			double halfHeight = height / 2d;
			//double deltaTheta = 2d * Math.PI / numSegments;
			//double theta = 0d;

			//Point[] points = new Point[numSegments];		//	these define a unit circle

			//for (int cntr = 0; cntr < numSegments; cntr++)
			//{
			//    points[cntr] = new Point(Math.Cos(theta), Math.Sin(theta));
			//    theta += deltaTheta;
			//}

			Point[] points = TubeRingRegularPolygon.PointsSingleton.Instance.GetPoints(numSegments);

			double rotateAngleForPerp = Vector3D.AngleBetween(new Vector3D(1, 0, 0), new Vector3D(radius, 0, height));		//	the 2nd vector is perpendicular to line formed from the edge of the cone to the tip

			#endregion

			#region Side

			for (int cntr = 0; cntr < numSegments; cntr++)
			{
				retVal.Positions.Add(transform.Transform(new Point3D(points[cntr].X * radius, points[cntr].Y * radius, -halfHeight)));
				retVal.Positions.Add(transform.Transform(new Point3D(0, 0, halfHeight)));		//	not sure if each face needs its own point, or if they can all share the same one

				//	The normal points straight out for a cylinder, but for a cone, it needs to be perpendicular to the slope of the cone
				Vector3D normal = new Vector3D(points[cntr].X, points[cntr].Y, 0d);
				Vector3D rotateAxis = new Vector3D(-points[cntr].Y, points[cntr].X, 0d);
				normal = normal.GetRotatedVector(rotateAxis, rotateAngleForPerp);

				retVal.Normals.Add(transform.Transform(normal));

				//	This one just points straight up
				retVal.Normals.Add(transform.Transform(new Vector3D(0, 0, 1d)));
			}

			for (int cntr = 0; cntr < numSegments - 1; cntr++)
			{
				//	0,2,3
				retVal.TriangleIndices.Add((cntr * 2) + 0);
				retVal.TriangleIndices.Add((cntr * 2) + 2);
				retVal.TriangleIndices.Add((cntr * 2) + 3);

				//	0,3,1
				retVal.TriangleIndices.Add((cntr * 2) + 0);
				retVal.TriangleIndices.Add((cntr * 2) + 3);
				retVal.TriangleIndices.Add((cntr * 2) + 1);
			}

			//	Connecting the last 2 points to the first 2
			//	last,0,1
			int offset = (numSegments - 1) * 2;
			retVal.TriangleIndices.Add(offset + 0);
			retVal.TriangleIndices.Add(0);
			retVal.TriangleIndices.Add(1);

			//	last,1,last+1
			retVal.TriangleIndices.Add(offset + 0);
			retVal.TriangleIndices.Add(1);
			retVal.TriangleIndices.Add(offset + 1);

			#endregion

			//	Caps
			int pointOffset = retVal.Positions.Count;

			//NOTE: The normals are backward from what you'd think

			GetCylinder_AlongXSprtEndCap(ref pointOffset, retVal, points, new Vector3D(0, 0, 1), radius, radius, -halfHeight, transform);
			//GetCylinder_AlongXSprtEndCap(ref pointOffset, retVal, points, new Vector3D(0, 0, -1), radius, halfHeight, transform);

			// Exit Function
			//retVal.Freeze();
			return retVal;
		}

		/// <summary>
		/// This is a cylinder with a dome on each end.  The cylinder part's height is total height minus twice radius (the
		/// sum of the endcaps).  If there isn't enough height for all that, a sphere is returned instead (so height will never
		/// be less than diameter)
		/// </summary>
		/// <param name="numSegmentsPhi">This is the number of segments for one dome</param>
		public static MeshGeometry3D GetCapsule_AlongZ(int numSegmentsTheta, int numSegmentsPhi, double radius, double height)
		{
			//NOTE: All the other geometries in this class are along the x axis, so I want to follow suit, but I think best along the z axis.  So I'll transform the points before commiting them to the geometry
			//TODO: This is so close to GetMultiRingedTube, the only difference is the multi ring tube has "hard" faces, and this has "soft" faces (this one shares points and normals, so the lighting is smoother)

			if (numSegmentsTheta < 3)
			{
				throw new ArgumentException("numSegmentsTheta must be at least 3: " + numSegmentsTheta.ToString(), "numSegmentsTheta");
			}

			if (height < radius * 2d)
			{
				//NOTE:  The separators aren't the same.  I believe the sphere method uses 2N+1 separators (or something like that)
				return GetSphere(numSegmentsTheta, radius);
			}

			MeshGeometry3D retVal = new MeshGeometry3D();

			#region Initial calculations

			Transform3D transform = Transform3D.Identity; //new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90d));

			double halfHeight = (height - (radius * 2d)) / 2d;
			//double deltaTheta = 2d * Math.PI / numSegmentsTheta;
			//double theta = 0d;

			//Point[] points = new Point[numSegmentsTheta];		//	these define a unit circle

			//for (int cntr = 0; cntr < numSegmentsTheta; cntr++)
			//{
			//    points[cntr] = new Point(Math.Cos(theta), Math.Sin(theta));
			//    theta += deltaTheta;
			//}

			Point[] points = TubeRingRegularPolygon.PointsSingleton.Instance.GetPoints(numSegmentsTheta);

			#endregion

			#region Side

			for (int cntr = 0; cntr < numSegmentsTheta; cntr++)
			{
				retVal.Positions.Add(transform.Transform(new Point3D(points[cntr].X * radius, points[cntr].Y * radius, -halfHeight)));
				retVal.Positions.Add(transform.Transform(new Point3D(points[cntr].X * radius, points[cntr].Y * radius, halfHeight)));

				retVal.Normals.Add(transform.Transform(new Vector3D(points[cntr].X, points[cntr].Y, 0d)));		//	the normals point straight out of the side
				retVal.Normals.Add(transform.Transform(new Vector3D(points[cntr].X, points[cntr].Y, 0d)));
			}

			for (int cntr = 0; cntr < numSegmentsTheta - 1; cntr++)
			{
				//	0,2,3
				retVal.TriangleIndices.Add((cntr * 2) + 0);
				retVal.TriangleIndices.Add((cntr * 2) + 2);
				retVal.TriangleIndices.Add((cntr * 2) + 3);

				//	0,3,1
				retVal.TriangleIndices.Add((cntr * 2) + 0);
				retVal.TriangleIndices.Add((cntr * 2) + 3);
				retVal.TriangleIndices.Add((cntr * 2) + 1);
			}

			//	Connecting the last 2 points to the first 2
			//	last,0,1
			int offset = (numSegmentsTheta - 1) * 2;
			retVal.TriangleIndices.Add(offset + 0);
			retVal.TriangleIndices.Add(0);
			retVal.TriangleIndices.Add(1);

			//	last,1,last+1
			retVal.TriangleIndices.Add(offset + 0);
			retVal.TriangleIndices.Add(1);
			retVal.TriangleIndices.Add(offset + 1);

			#endregion

			#region Caps

			//TODO: Get the dome to not recreate the same points that the cylinder part uses (not nessassary, just inefficient)

			int pointOffset = retVal.Positions.Count;

			Transform3DGroup domeTransform = new Transform3DGroup();
			domeTransform.Children.Add(new TranslateTransform3D(0, 0, halfHeight));
			domeTransform.Children.Add(transform);
			GetDome(ref pointOffset, retVal, points, domeTransform, numSegmentsPhi, radius, radius, radius);

			domeTransform = new Transform3DGroup();
			domeTransform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 180d)));
			domeTransform.Children.Add(new TranslateTransform3D(0, 0, -halfHeight));
			domeTransform.Children.Add(transform);
			GetDome(ref pointOffset, retVal, points, domeTransform, numSegmentsPhi, radius, radius, radius);

			#endregion

			// Exit Function
			//retVal.Freeze();
			return retVal;
		}

		public static MeshGeometry3D GetTorus(int spineSegments, int fleshSegments, double innerRadius, double outerRadius)
		{
			MeshGeometry3D retVal = new MeshGeometry3D();

			// The spine is the circle around the hole in the
			// torus, the flesh is a set of circles around the
			// spine.
			int cp = 0; // Index of last added point.

			for (int i = 0; i < spineSegments; ++i)
			{
				double spineParam = ((double)i) / ((double)spineSegments);
				double spineAngle = Math.PI * 2 * spineParam;
				Vector3D spineVector = new Vector3D(Math.Cos(spineAngle), Math.Sin(spineAngle), 0);

				for (int j = 0; j < fleshSegments; ++j)
				{
					double fleshParam = ((double)j) / ((double)fleshSegments);
					double fleshAngle = Math.PI * 2 * fleshParam;
					Vector3D fleshVector = spineVector * Math.Cos(fleshAngle) + new Vector3D(0, 0, Math.Sin(fleshAngle));
					Point3D p = new Point3D(0, 0, 0) + outerRadius * spineVector + innerRadius * fleshVector;

					retVal.Positions.Add(p);
					retVal.Normals.Add(-fleshVector);
					retVal.TextureCoordinates.Add(new Point(spineParam, fleshParam));

					// Now add a quad that has it's upper-right corner at the point we just added.
					// i.e. cp . cp-1 . cp-1-_fleshSegments . cp-_fleshSegments
					int a = cp;
					int b = cp - 1;
					int c = cp - (int)1 - fleshSegments;
					int d = cp - fleshSegments;

					// The next two if statements handle the wrapping around of the torus.  For either i = 0 or j = 0
					// the created quad references vertices that haven't been created yet.
					if (j == 0)
					{
						b += fleshSegments;
						c += fleshSegments;
					}

					if (i == 0)
					{
						c += fleshSegments * spineSegments;
						d += fleshSegments * spineSegments;
					}

					retVal.TriangleIndices.Add((ushort)a);
					retVal.TriangleIndices.Add((ushort)b);
					retVal.TriangleIndices.Add((ushort)c);

					retVal.TriangleIndices.Add((ushort)a);
					retVal.TriangleIndices.Add((ushort)c);
					retVal.TriangleIndices.Add((ushort)d);
					++cp;
				}
			}

			// Exit Function
			//retVal.Freeze();
			return retVal;
		}

		public static MeshGeometry3D GetRing(int numSides, double innerRadius, double outerRadius, double height, Transform3D transform)
		{
			MeshGeometry3D retVal = new MeshGeometry3D();

			if (transform == null)
			{
				transform = Transform3D.Identity;
			}

			Point[] points = TubeRingRegularPolygon.PointsSingleton.Instance.GetPoints(numSides);
			double halfHeight = height * .5d;

			int pointOffset = 0;

			#region Outer Ring

			#region Positions/Normals

			for (int cntr = 0; cntr < numSides; cntr++)
			{
				retVal.Positions.Add(transform.Transform(new Point3D(points[cntr].X * outerRadius, points[cntr].Y * outerRadius, -halfHeight)));
				retVal.Normals.Add(transform.Transform(new Vector3D(points[cntr].X * outerRadius, points[cntr].Y * outerRadius, 0d).ToUnit()));		//	the normals point straight out of the side
			}

			for (int cntr = 0; cntr < numSides; cntr++)
			{
				retVal.Positions.Add(transform.Transform(new Point3D(points[cntr].X * outerRadius, points[cntr].Y * outerRadius, halfHeight)));
				retVal.Normals.Add(transform.Transform(new Vector3D(points[cntr].X * outerRadius, points[cntr].Y * outerRadius, 0d).ToUnit()));		//	the normals point straight out of the side
			}

			#endregion

			#region Triangles

			int zOffsetBottom = pointOffset;
			int zOffsetTop = zOffsetBottom + numSides;

			for (int cntr = 0; cntr < numSides - 1; cntr++)
			{
				//	Top/Left triangle
				retVal.TriangleIndices.Add(zOffsetBottom + cntr + 0);
				retVal.TriangleIndices.Add(zOffsetTop + cntr + 1);
				retVal.TriangleIndices.Add(zOffsetTop + cntr + 0);

				//	Bottom/Right triangle
				retVal.TriangleIndices.Add(zOffsetBottom + cntr + 0);
				retVal.TriangleIndices.Add(zOffsetBottom + cntr + 1);
				retVal.TriangleIndices.Add(zOffsetTop + cntr + 1);
			}

			//	Connecting the last 2 points to the first 2
			//	Top/Left triangle
			retVal.TriangleIndices.Add(zOffsetBottom + (numSides - 1) + 0);
			retVal.TriangleIndices.Add(zOffsetTop);		//	wrapping back around
			retVal.TriangleIndices.Add(zOffsetTop + (numSides - 1) + 0);

			//	Bottom/Right triangle
			retVal.TriangleIndices.Add(zOffsetBottom + (numSides - 1) + 0);
			retVal.TriangleIndices.Add(zOffsetBottom);
			retVal.TriangleIndices.Add(zOffsetTop);

			#endregion

			pointOffset = retVal.Positions.Count;

			#endregion

			#region Inner Ring

			#region Positions/Normals

			for (int cntr = 0; cntr < numSides; cntr++)
			{
				retVal.Positions.Add(transform.Transform(new Point3D(points[cntr].X * innerRadius, points[cntr].Y * innerRadius, -halfHeight)));
				retVal.Normals.Add(transform.Transform(new Vector3D(points[cntr].X * innerRadius, points[cntr].Y * innerRadius, 0d).ToUnit() * -1d));		//	the normals point straight in from the side
			}

			for (int cntr = 0; cntr < numSides; cntr++)
			{
				retVal.Positions.Add(transform.Transform(new Point3D(points[cntr].X * innerRadius, points[cntr].Y * innerRadius, halfHeight)));
				retVal.Normals.Add(transform.Transform(new Vector3D(points[cntr].X * innerRadius, points[cntr].Y * innerRadius, 0d).ToUnit() * -1d));		//	the normals point straight in from the side
			}

			#endregion

			#region Triangles

			zOffsetBottom = pointOffset;
			zOffsetTop = zOffsetBottom + numSides;

			for (int cntr = 0; cntr < numSides - 1; cntr++)
			{
				//	Top/Left triangle
				retVal.TriangleIndices.Add(zOffsetBottom + cntr + 0);
				retVal.TriangleIndices.Add(zOffsetTop + cntr + 0);
				retVal.TriangleIndices.Add(zOffsetTop + cntr + 1);

				//	Bottom/Right triangle
				retVal.TriangleIndices.Add(zOffsetBottom + cntr + 0);
				retVal.TriangleIndices.Add(zOffsetTop + cntr + 1);
				retVal.TriangleIndices.Add(zOffsetBottom + cntr + 1);
			}

			//	Connecting the last 2 points to the first 2
			//	Top/Left triangle
			retVal.TriangleIndices.Add(zOffsetBottom + (numSides - 1) + 0);
			retVal.TriangleIndices.Add(zOffsetTop + (numSides - 1) + 0);
			retVal.TriangleIndices.Add(zOffsetTop);		//	wrapping back around

			//	Bottom/Right triangle
			retVal.TriangleIndices.Add(zOffsetBottom + (numSides - 1) + 0);
			retVal.TriangleIndices.Add(zOffsetTop);
			retVal.TriangleIndices.Add(zOffsetBottom);

			#endregion

			pointOffset = retVal.Positions.Count;

			#endregion

			#region Top Cap

			Transform3DGroup capTransform = new Transform3DGroup();
			capTransform.Children.Add(new TranslateTransform3D(0, 0, halfHeight));
			capTransform.Children.Add(transform);

			GetRingSprtCap(ref pointOffset, retVal, capTransform, points, numSides, innerRadius, outerRadius);

			#endregion

			#region Bottom Cap

			capTransform = new Transform3DGroup();
			capTransform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 180d)));
			capTransform.Children.Add(new TranslateTransform3D(0, 0, -halfHeight));
			capTransform.Children.Add(transform);

			GetRingSprtCap(ref pointOffset, retVal, capTransform, points, numSides, innerRadius, outerRadius);

			#endregion

			//	Exit Function
			return retVal;
		}
		private static void GetRingSprtCap(ref int pointOffset, MeshGeometry3D geometry, Transform3D transform, Point[] points, int numSides, double innerRadius, double outerRadius)
		{
			//	Points/Normals
			for (int cntr = 0; cntr < numSides; cntr++)
			{
				geometry.Positions.Add(transform.Transform(new Point3D(points[cntr].X * outerRadius, points[cntr].Y * outerRadius, 0d)));
				geometry.Normals.Add(transform.Transform(new Vector3D(0, 0, 1)).ToUnit());
			}

			for (int cntr = 0; cntr < numSides; cntr++)
			{
				geometry.Positions.Add(transform.Transform(new Point3D(points[cntr].X * innerRadius, points[cntr].Y * innerRadius, 0d)));
				geometry.Normals.Add(transform.Transform(new Vector3D(0, 0, 1)).ToUnit());
			}

			int zOffsetOuter = pointOffset;
			int zOffsetInner = zOffsetOuter + numSides;

			//	Triangles
			for (int cntr = 0; cntr < numSides - 1; cntr++)
			{
				//	Bottom Right triangle
				geometry.TriangleIndices.Add(zOffsetOuter + cntr + 0);
				geometry.TriangleIndices.Add(zOffsetOuter + cntr + 1);
				geometry.TriangleIndices.Add(zOffsetInner + cntr + 1);

				//	Top Left triangle
				geometry.TriangleIndices.Add(zOffsetOuter + cntr + 0);
				geometry.TriangleIndices.Add(zOffsetInner + cntr + 1);
				geometry.TriangleIndices.Add(zOffsetInner + cntr + 0);
			}

			//	Connecting the last 2 points to the first 2
			//	Bottom/Right triangle
			geometry.TriangleIndices.Add(zOffsetOuter + (numSides - 1) + 0);
			geometry.TriangleIndices.Add(zOffsetOuter);
			geometry.TriangleIndices.Add(zOffsetInner);

			//	Top/Left triangle
			geometry.TriangleIndices.Add(zOffsetOuter + (numSides - 1) + 0);
			geometry.TriangleIndices.Add(zOffsetInner);		//	wrapping back around
			geometry.TriangleIndices.Add(zOffsetInner + (numSides - 1) + 0);

			pointOffset = geometry.Positions.Count;
		}

		public static MeshGeometry3D GetCircle2D(int numSides, Transform3D transform, Transform3D normalTransform)
		{
			//NOTE: This also sets the texture coordinates

			int pointOffset = 0;
			Point[] pointsTheta = TubeRingRegularPolygon.PointsSingleton.Instance.GetPoints(numSides);

			MeshGeometry3D retVal = new MeshGeometry3D();

			#region Positions/Normals

			for (int thetaCntr = 0; thetaCntr < pointsTheta.Length; thetaCntr++)
			{
				Point3D point = new Point3D(pointsTheta[thetaCntr].X, pointsTheta[thetaCntr].Y, 0d);
				retVal.Positions.Add(transform.Transform(point));

				Point texturePoint = new Point(.5d + (pointsTheta[thetaCntr].X * .5d), .5d + (pointsTheta[thetaCntr].Y * .5d));
				retVal.TextureCoordinates.Add(texturePoint);

				Vector3D normal = new Vector3D(0, 0, 1);
				retVal.Normals.Add(normalTransform.Transform(normal));
			}

			#endregion

			#region Add the triangles

			// Start with 0,1,2
			retVal.TriangleIndices.Add(pointOffset + 0);
			retVal.TriangleIndices.Add(pointOffset + 1);
			retVal.TriangleIndices.Add(pointOffset + 2);

			int lowerIndex = 2;
			int upperIndex = pointsTheta.Length - 1;
			int lastUsedIndex = 0;
			bool shouldBumpLower = true;

			// Do the rest of the triangles
			while (lowerIndex < upperIndex)
			{
				retVal.TriangleIndices.Add(pointOffset + lowerIndex);
				retVal.TriangleIndices.Add(pointOffset + upperIndex);
				retVal.TriangleIndices.Add(pointOffset + lastUsedIndex);

				if (shouldBumpLower)
				{
					lastUsedIndex = lowerIndex;
					lowerIndex++;
				}
				else
				{
					lastUsedIndex = upperIndex;
					upperIndex--;
				}

				shouldBumpLower = !shouldBumpLower;
			}

			#endregion

			return retVal;
		}

		/// <summary>
		/// This can be used to create all kinds of shapes.  Each ring defines either an end cap or the tube's side
		/// </summary>
		/// <remarks>
		/// If you pass in 2 rings, equal radius, you have a cylinder
		/// If one of them goes to a point, it will be a cone
		/// etc
		/// 
		/// softSides:
		///	If you are making something discreet, like dice, or a jewel, you will want each face to reflect like mirrors (not soft).  But if you
		///	are making a cylinder, etc, you don't want the faces to stand out.  Instead, they should blend together, so you will want soft
		/// </remarks>
		/// <param name="softSides">
		/// True: The normal for each vertex point out from the vertex, so the faces appear to blend together.
		/// False: The normal for each vertex of a triangle is that triangle's normal, so each triangle reflects like cut glass.
		/// </param>
		/// <param name="shouldCenterZ">
		/// True: Centers the object along Z (so even though you start the rings at Z of zero, that first ring will be at negative half height).
		/// False: Goes from 0 to height
		/// </param>
		public static MeshGeometry3D GetMultiRingedTube_ORIG(int numSides, List<TubeRingDefinition_ORIG> rings, bool softSides, bool shouldCenterZ)
		{
			#region Validate

			if (rings.Count == 1)
			{
				if (rings[0].RingType == TubeRingType_ORIG.Point)
				{
					throw new ArgumentException("Only a single ring was passed in, and it's a point");
				}
			}

			#endregion

			MeshGeometry3D retVal = new MeshGeometry3D();

			#region Calculate total height

			double height = 0d;
			for (int cntr = 1; cntr < rings.Count; cntr++)     // the first ring's distance from prev ring is ignored
			{
				if (rings[cntr].DistFromPrevRing <= 0)
				{
					throw new ArgumentException("DistFromPrevRing must be positive: " + rings[cntr].DistFromPrevRing.ToString());
				}

				height += rings[cntr].DistFromPrevRing;
			}

			#endregion

			double curZ = 0;
			if (shouldCenterZ)
			{
				curZ = height * -.5d;      // starting in the negative
			}

			// Get the points (unit circle)
			Point[] points = GetMultiRingedTubeSprtPoints_ORIG(numSides);

			int pointOffset = 0;

			if (rings[0].RingType == TubeRingType_ORIG.Ring_Closed)
			{
				GetMultiRingedTubeSprtEndCap_ORIG(ref pointOffset, retVal, numSides, points, rings[0], true, curZ);
			}

			for (int cntr = 0; cntr < rings.Count - 1; cntr++)
			{
				if ((cntr > 0 && cntr < rings.Count - 1) &&
					(rings[cntr].RingType == TubeRingType_ORIG.Dome || rings[cntr].RingType == TubeRingType_ORIG.Point))
				{
					throw new ArgumentException("Rings aren't allowed to be points in the middle of the tube");
				}

				GetMultiRingedTubeSprtBetweenRings_ORIG(ref pointOffset, retVal, numSides, points, rings[cntr], rings[cntr + 1], curZ);

				curZ += rings[cntr + 1].DistFromPrevRing;
			}

			if (rings.Count > 1 && rings[rings.Count - 1].RingType == TubeRingType_ORIG.Ring_Closed)
			{
				GetMultiRingedTubeSprtEndCap_ORIG(ref pointOffset, retVal, numSides, points, rings[rings.Count - 1], false, curZ);
			}

			// Exit Function
			//retVal.Freeze();
			return retVal;
		}

		//NOTE: The original had x,y as diameter.  This one has them as radius
		public static MeshGeometry3D GetMultiRingedTube(int numSides, List<TubeRingBase> rings, bool softSides, bool shouldCenterZ)
		{
			return GetMultiRingedTube(numSides, rings, softSides, shouldCenterZ, Transform3D.Identity);
		}
		public static MeshGeometry3D GetMultiRingedTube(int numSides, List<TubeRingBase> rings, bool softSides, bool shouldCenterZ, Transform3D transform)
		{
			//	Do some validation/prep work
			double height, curZ;
			InitializeMultiRingedTube(out height, out curZ, numSides, rings, shouldCenterZ);

			//	This is here so in the future, I could lay down the points differently without changing any other code.  The points/normals will
			//	be transformed as they are being added to the geometry
			//Transform3D transform = Transform3D.Identity;

			MeshGeometry3D retVal = new MeshGeometry3D();

			int pointOffset = 0;

			//	This is used when softSides is true.  This allows for a way to have a common normal between one ring's bottom and the next ring's top
			double[] rotateAnglesForPerp = null;

			TubeRingBase nextRing = rings.Count > 1 ? rings[1] : null;
			MultiRingEndCap(ref pointOffset, ref rotateAnglesForPerp, retVal, numSides, null, rings[0], nextRing, transform, true, curZ, softSides);

			for (int cntr = 0; cntr < rings.Count - 1; cntr++)
			{
				if (cntr > 0 && cntr < rings.Count - 1 && !(rings[cntr] is TubeRingRegularPolygon))
				{
					throw new ArgumentException("Only rings are allowed in the middle of the tube");
				}

				MultiRingMiddle(ref pointOffset, ref rotateAnglesForPerp, retVal, transform, numSides, rings[cntr], rings[cntr + 1], curZ, softSides);

				curZ += rings[cntr + 1].DistFromPrevRing;
			}

			TubeRingBase prevRing = rings.Count > 1 ? rings[rings.Count - 2] : null;
			MultiRingEndCap(ref pointOffset, ref rotateAnglesForPerp, retVal, numSides, prevRing, rings[rings.Count - 1], null, transform, false, curZ, softSides);

			// Exit Function
			//retVal.Freeze();
			return retVal;
		}

		public static MeshGeometry3D GetMeshFromTriangles_IndependentFaces(ITriangle[] triangles)
		{
			MeshGeometry3D retVal = new MeshGeometry3D();

			for (int cntr = 0; cntr < triangles.Length; cntr++)
			{
				retVal.Positions.Add(triangles[cntr].Point0);
				retVal.Positions.Add(triangles[cntr].Point1);
				retVal.Positions.Add(triangles[cntr].Point2);

				retVal.Normals.Add(triangles[cntr].Normal);
				retVal.Normals.Add(triangles[cntr].Normal);
				retVal.Normals.Add(triangles[cntr].Normal);

				retVal.TriangleIndices.Add(cntr * 3);
				retVal.TriangleIndices.Add((cntr * 3) + 1);
				retVal.TriangleIndices.Add((cntr * 3) + 2);
			}

			// Exit Function
			//retVal.Freeze();
			return retVal;
		}
		public static MeshGeometry3D GetMeshFromTriangles(TriangleIndexed[] triangles)
		{
			MeshGeometry3D retVal = new MeshGeometry3D();

			//	All the triangles in the list of TriangleIndexed should share the same points, so just use the first triangle's list of points
			foreach (Point3D point in triangles[0].AllPoints)
			{
				retVal.Positions.Add(point);
			}

			foreach (TriangleIndexed triangle in triangles)
			{
				retVal.TriangleIndices.Add(triangle.Index0);
				retVal.TriangleIndices.Add(triangle.Index1);
				retVal.TriangleIndices.Add(triangle.Index2);
			}

			// Exit Function
			//retVal.Freeze();
			return retVal;
		}

		public static TriangleIndexed[] GetTrianglesFromMesh(MeshGeometry3D mesh)
		{
			return GetTrianglesFromMesh(mesh, null);
		}
		public static TriangleIndexed[] GetTrianglesFromMesh(MeshGeometry3D mesh, Transform3D transform)
		{
			if (mesh == null)
			{
				return null;
			}

			string report = mesh.ReportGeometry();

			Point3D[] points = mesh.Positions.ToArray();

			if (transform != null)
			{
				transform.Transform(points);
			}

			TriangleIndexed[] retVal = new TriangleIndexed[mesh.TriangleIndices.Count / 3];

			for (int cntr = 0; cntr < retVal.Length; cntr++)
			{
				retVal[cntr] = new TriangleIndexed(mesh.TriangleIndices[cntr * 3], mesh.TriangleIndices[(cntr * 3) + 1], mesh.TriangleIndices[(cntr * 3) + 2], points);
			}

			return retVal;
		}

		public static Point3D[] GetPointsFromMesh(MeshGeometry3D mesh)
		{
			return GetPointsFromMesh(mesh, null);
		}
		public static Point3D[] GetPointsFromMesh(MeshGeometry3D mesh, Transform3D transform)
		{
			if (mesh == null)
			{
				return null;
			}

			Point3D[] points = null;
			if (mesh.TriangleIndices != null && mesh.TriangleIndices.Count > 0)
			{
				//	Referenced points
				points = mesh.TriangleIndices.Select(o => mesh.Positions[o]).ToArray();
			}
			else
			{
				//	Directly used points
				points = mesh.Positions.ToArray();
			}

			if (transform != null)
			{
				transform.Transform(points);
			}

			//	Exit Function
			return points;
		}

		/// <summary>
		/// This returns a convex hull of triangles that uses the outermost points (uses the quickhull algorithm)
		/// </summary>
		public static TriangleIndexed[] GetConvexHull(Point3D[] points)
		{
			return QuickHull3D.GetConvexHull(points);
		}
		/// <summary>
		/// This returns a convex hull of lines that uses the outermost points (uses the quickhull algorithm)
		/// NOTE:  Even though the param is Point3D, Z is ignored
		/// </summary>
		public static QuickHull2DResult GetConvexHull2D(Point3D[] points)
		{
			return QuickHull2D.GetConvexHull(points);
		}
		/// <summary>
		/// This returns a convex hull of lines that uses the outermost points (uses the quickhull algorithm)
		/// </summary>
		public static QuickHull2DResult GetConvexHull2D(Point[] points)
		{
			return QuickHull2D.GetConvexHull(points);
		}

		/// <summary>
		/// Aparently, there are some known bugs with Mouse.GetPosition() - especially with dragdrop.  This method
		/// should always work
		/// </summary>
		/// <remarks>
		/// Got this here:
		/// http://www.switchonthecode.com/tutorials/wpf-snippet-reliably-getting-the-mouse-position
		/// </remarks>
		public static Point GetPositionCorrect(Visual relativeTo)
		{
			Win32Point w32Mouse = new Win32Point();
			GetCursorPos(ref w32Mouse);

			return relativeTo.PointFromScreen(new Point(w32Mouse.X, w32Mouse.Y));
		}

		/// <summary>
		/// This converts the position into screen coords
		/// </summary>
		/// <remarks>
		/// Got this here:
		/// http://blogs.msdn.com/llobo/archive/2006/05/02/Code-for-getting-screen-relative-Position-in-WPF.aspx
		/// </remarks>
		public static Point TransformToScreen(Point point, Visual relativeTo)
		{
			HwndSource hwndSource = PresentationSource.FromVisual(relativeTo) as HwndSource;
			Visual root = hwndSource.RootVisual;

			// Translate the point from the visual to the root.
			GeneralTransform transformToRoot = relativeTo.TransformToAncestor(root);
			Point pointRoot = transformToRoot.Transform(point);

			// Transform the point from the root to client coordinates.
			Matrix m = Matrix.Identity;
			Transform transform = VisualTreeHelper.GetTransform(root);

			if (transform != null)
			{
				m = Matrix.Multiply(m, transform.Value);
			}

			Vector offset = VisualTreeHelper.GetOffset(root);
			m.Translate(offset.X, offset.Y);

			Point pointClient = m.Transform(pointRoot);

			// Convert from “device-independent pixels” into pixels.
			pointClient = hwndSource.CompositionTarget.TransformToDevice.Transform(pointClient);

			POINT pointClientPixels = new POINT();
			pointClientPixels.x = (0 < pointClient.X) ? (int)(pointClient.X + 0.5) : (int)(pointClient.X - 0.5);
			pointClientPixels.y = (0 < pointClient.Y) ? (int)(pointClient.Y + 0.5) : (int)(pointClient.Y - 0.5);

			// Transform the point into screen coordinates.
			POINT pointScreenPixels = pointClientPixels;
			ClientToScreen(hwndSource.Handle, pointScreenPixels);
			return new Point(pointScreenPixels.x, pointScreenPixels.y);
		}

		/// <summary>
		/// Converts the 2D point into 3D world point and ray
		/// </summary>
		/// <remarks>
		/// This method uses reflection to get at an internal method off of camera (I think it's rigged up for a perspective camera)
		/// http://grokys.blogspot.com/2010/08/wpf-3d-translating-2d-point-into-3d.html
		/// </remarks>
		public static RayHitTestParameters RayFromViewportPoint(Camera camera, Viewport3D viewport, Point point)
		{
			Size viewportSize = new Size(viewport.ActualWidth, viewport.ActualHeight);

			System.Reflection.MethodInfo method = typeof(Camera).GetMethod("RayFromViewportPoint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			double distanceAdjustment = 0;
			object[] parameters = new object[] { point, viewportSize, null, distanceAdjustment };

			return (RayHitTestParameters)method.Invoke(camera, parameters);
		}

		#region Private Methods

		private static void GetDome(ref int pointOffset, MeshGeometry3D geometry, Point[] pointsTheta, Transform3D transform, int numSegmentsPhi, double radiusX, double radiusY, double radiusZ)
		{
			#region Initial calculations

			//	NOTE: There is one more than what the passed in
			Point[] pointsPhi = new Point[numSegmentsPhi + 1];

			pointsPhi[0] = new Point(1d, 0d);		//	along the equator
			pointsPhi[numSegmentsPhi] = new Point(0d, 1d);		//	north pole

			if (pointsPhi.Length > 2)
			{
				//	Need to go from 0 to half pi
				double halfPi = Math.PI * .5d;
				double deltaPhi = halfPi / pointsPhi.Length;		//	there is one more point than numSegmentsPhi

				for (int cntr = 1; cntr < numSegmentsPhi; cntr++)
				{
					double phi = deltaPhi * cntr;		//	phi goes from 0 to pi for a full sphere, so start halfway up
					pointsPhi[cntr] = new Point(Math.Cos(phi), Math.Sin(phi));
				}
			}

			#endregion

			#region Positions/Normals

			//	Can't use all of the transform passed in for the normal, because translate portions will skew the normal funny
			Transform3DGroup normalTransform = new Transform3DGroup();
			if (transform is Transform3DGroup)
			{
				foreach (var subTransform in ((Transform3DGroup)transform).Children)
				{
					if (!(subTransform is TranslateTransform3D))
					{
						normalTransform.Children.Add(subTransform);
					}
				}
			}
			else if (transform is TranslateTransform3D)
			{
				normalTransform.Children.Add(Transform3D.Identity);
			}
			else
			{
				normalTransform.Children.Add(transform);
			}

			//for (int phiCntr = 0; phiCntr < numSegmentsPhi; phiCntr++)		//	The top point will be added after this loop
			for (int phiCntr = pointsPhi.Length - 1; phiCntr > 0; phiCntr--)
			{
				for (int thetaCntr = 0; thetaCntr < pointsTheta.Length; thetaCntr++)
				{
					//	Phi points are going from bottom to equator.  

					Point3D point = new Point3D(
						radiusX * pointsTheta[thetaCntr].X * pointsPhi[phiCntr].Y,
						radiusY * pointsTheta[thetaCntr].Y * pointsPhi[phiCntr].Y,
						radiusZ * pointsPhi[phiCntr].X);

					geometry.Positions.Add(transform.Transform(point));

					//TODO: For a standalone dome, the bottom rings will point straight out.  But for something like a snow cone, the normal will have to be averaged with the cone
					geometry.Normals.Add(normalTransform.Transform(point).ToVector().ToUnit());		//	the normal is the same as the point for a sphere (but no tranlate transform)
				}
			}

			//	This is north pole point
			geometry.Positions.Add(transform.Transform(new Point3D(0, 0, radiusZ)));
			geometry.Normals.Add(transform.Transform(new Vector3D(0, 0, 1)));

			#endregion

			#region Triangles - Rings

			int zOffsetBottom = pointOffset;
			int zOffsetTop;

			for (int phiCntr = 0; phiCntr < numSegmentsPhi - 1; phiCntr++)		//	The top cone will be added after this loop
			{
				zOffsetTop = zOffsetBottom + pointsTheta.Length;

				for (int thetaCntr = 0; thetaCntr < pointsTheta.Length - 1; thetaCntr++)
				{
					//	Top/Left triangle
					geometry.TriangleIndices.Add(zOffsetBottom + thetaCntr + 0);
					geometry.TriangleIndices.Add(zOffsetTop + thetaCntr + 1);
					geometry.TriangleIndices.Add(zOffsetTop + thetaCntr + 0);

					//	Bottom/Right triangle
					geometry.TriangleIndices.Add(zOffsetBottom + thetaCntr + 0);
					geometry.TriangleIndices.Add(zOffsetBottom + thetaCntr + 1);
					geometry.TriangleIndices.Add(zOffsetTop + thetaCntr + 1);
				}

				//	Connecting the last 2 points to the first 2
				//	Top/Left triangle
				geometry.TriangleIndices.Add(zOffsetBottom + (pointsTheta.Length - 1) + 0);
				geometry.TriangleIndices.Add(zOffsetTop);		//	wrapping back around
				geometry.TriangleIndices.Add(zOffsetTop + (pointsTheta.Length - 1) + 0);

				//	Bottom/Right triangle
				geometry.TriangleIndices.Add(zOffsetBottom + (pointsTheta.Length - 1) + 0);
				geometry.TriangleIndices.Add(zOffsetBottom);
				geometry.TriangleIndices.Add(zOffsetTop);

				//	Prep for the next ring
				zOffsetBottom = zOffsetTop;
			}

			#endregion
			#region Triangles - Cap

			int topIndex = geometry.Positions.Count - 1;

			for (int cntr = 0; cntr < pointsTheta.Length - 1; cntr++)
			{
				geometry.TriangleIndices.Add(zOffsetBottom + cntr + 0);
				geometry.TriangleIndices.Add(zOffsetBottom + cntr + 1);
				geometry.TriangleIndices.Add(topIndex);
			}

			//	The last triangle links back to zero
			geometry.TriangleIndices.Add(zOffsetBottom + pointsTheta.Length - 1 + 0);
			geometry.TriangleIndices.Add(zOffsetBottom + 0);
			geometry.TriangleIndices.Add(topIndex);

			#endregion

			pointOffset = geometry.Positions.Count;
		}

		/// <summary>
		/// This isn't meant to be used by anything.  It's just a pure implementation of a dome that can be a template for the
		/// real methods
		/// </summary>
		private static MeshGeometry3D GetDome_Template(int numSegmentsTheta, int numSegmentsPhi, double radiusX, double radiusY, double radiusZ)
		{
			//	This will be along the z axis.  It will go from z=0 to z=radiusZ

			if (numSegmentsTheta < 3)
			{
				throw new ArgumentException("numSegments must be at least 3: " + numSegmentsTheta.ToString(), "numSegments");
			}

			MeshGeometry3D retVal = new MeshGeometry3D();

			#region Initial calculations

			//Transform3D transform = Transform3D.Identity;

			double deltaTheta = 2d * Math.PI / numSegmentsTheta;

			Point[] pointsTheta = new Point[numSegmentsTheta];		//	these define a unit circle

			for (int cntr = 0; cntr < numSegmentsTheta; cntr++)
			{
				pointsTheta[cntr] = new Point(Math.Cos(deltaTheta * cntr), Math.Sin(deltaTheta * cntr));
			}

			//	NOTE: There is one more than what the passed in
			Point[] pointsPhi = new Point[numSegmentsPhi + 1];

			pointsPhi[0] = new Point(1d, 0d);		//	along the equator
			pointsPhi[numSegmentsPhi] = new Point(0d, 1d);		//	north pole

			if (pointsPhi.Length > 2)
			{
				//double halfPi = Math.PI * .5d;
				////double deltaPhi = halfPi / numSegmentsPhi;
				//double deltaPhi = halfPi / pointsPhi.Length;		//	there is one more point than numSegmentsPhi
				////double deltaPhi = Math.PI / numSegmentsPhi;

				//for (int cntr = 1; cntr < numSegmentsPhi; cntr++)
				//{
				//    double phi = halfPi + (deltaPhi * cntr);		//	phi goes from 0 to pi for a full sphere, so start halfway up
				//    //double phi = deltaPhi * cntr;
				//    pointsPhi[cntr] = new Point(Math.Cos(phi), Math.Sin(phi));
				//}



				//	Need to go from 0 to half pi
				double halfPi = Math.PI * .5d;
				double deltaPhi = halfPi / pointsPhi.Length;		//	there is one more point than numSegmentsPhi

				for (int cntr = 1; cntr < numSegmentsPhi; cntr++)
				{
					double phi = deltaPhi * cntr;		//	phi goes from 0 to pi for a full sphere, so start halfway up
					pointsPhi[cntr] = new Point(Math.Cos(phi), Math.Sin(phi));
				}


			}

			#endregion

			#region Positions/Normals

			//for (int phiCntr = 0; phiCntr < numSegmentsPhi; phiCntr++)		//	The top point will be added after this loop
			for (int phiCntr = pointsPhi.Length - 1; phiCntr > 0; phiCntr--)
			{
				for (int thetaCntr = 0; thetaCntr < numSegmentsTheta; thetaCntr++)
				{

					//	I think phi points are going from bottom to equator.  

					Point3D point = new Point3D(
						radiusX * pointsTheta[thetaCntr].X * pointsPhi[phiCntr].Y,
						radiusY * pointsTheta[thetaCntr].Y * pointsPhi[phiCntr].Y,
						radiusZ * pointsPhi[phiCntr].X);

					//point = transform.Transform(point);

					retVal.Positions.Add(point);

					//TODO: For a standalone dome, the bottom ring's will point straight out.  But for something like a snow cone, the normal will have to be averaged with the cone
					retVal.Normals.Add(point.ToVector().ToUnit());		//	the normal is the same as the point for a sphere
				}
			}

			//	This is north pole point
			//retVal.Positions.Add(transform.Transform(new Point3D(0, 0, radiusZ)));
			//retVal.Normals.Add(transform.Transform(new Vector3D(0, 0, 1)));
			retVal.Positions.Add(new Point3D(0, 0, radiusZ));
			retVal.Normals.Add(new Vector3D(0, 0, 1));

			#endregion

			#region Triangles - Rings

			int zOffsetBottom = 0;
			int zOffsetTop;

			for (int phiCntr = 0; phiCntr < numSegmentsPhi - 1; phiCntr++)		//	The top cone will be added after this loop
			{
				zOffsetTop = zOffsetBottom + numSegmentsTheta;

				for (int thetaCntr = 0; thetaCntr < numSegmentsTheta - 1; thetaCntr++)
				{
					//	Top/Left triangle
					retVal.TriangleIndices.Add(zOffsetBottom + thetaCntr + 0);
					retVal.TriangleIndices.Add(zOffsetTop + thetaCntr + 1);
					retVal.TriangleIndices.Add(zOffsetTop + thetaCntr + 0);

					//	Bottom/Right triangle
					retVal.TriangleIndices.Add(zOffsetBottom + thetaCntr + 0);
					retVal.TriangleIndices.Add(zOffsetBottom + thetaCntr + 1);
					retVal.TriangleIndices.Add(zOffsetTop + thetaCntr + 1);
				}

				//	Connecting the last 2 points to the first 2
				//	Top/Left triangle
				retVal.TriangleIndices.Add(zOffsetBottom + (numSegmentsTheta - 1) + 0);
				retVal.TriangleIndices.Add(zOffsetTop);		//	wrapping back around
				retVal.TriangleIndices.Add(zOffsetTop + (numSegmentsTheta - 1) + 0);

				//	Bottom/Right triangle
				retVal.TriangleIndices.Add(zOffsetBottom + (numSegmentsTheta - 1) + 0);
				retVal.TriangleIndices.Add(zOffsetBottom);
				retVal.TriangleIndices.Add(zOffsetTop);

				//	Prep for the next ring
				zOffsetBottom = zOffsetTop;
			}

			#endregion
			#region Triangles - Cap

			int topIndex = retVal.Positions.Count - 1;

			for (int cntr = 0; cntr < numSegmentsTheta - 1; cntr++)
			{
				retVal.TriangleIndices.Add(zOffsetBottom + cntr + 0);
				retVal.TriangleIndices.Add(zOffsetBottom + cntr + 1);
				retVal.TriangleIndices.Add(topIndex);
			}

			//	The last triangle links back to zero
			retVal.TriangleIndices.Add(zOffsetBottom + numSegmentsTheta - 1 + 0);
			retVal.TriangleIndices.Add(zOffsetBottom + 0);
			retVal.TriangleIndices.Add(topIndex);

			#endregion

			// Exit Function
			//retVal.Freeze();
			return retVal;
		}

		#region GetMultiRingedTube helpers (ORIG)

		private static Point[] GetMultiRingedTubeSprtPoints_ORIG(int numSides)
		{
			// This calculates the points (note they are 2D, because each ring will have its own z anyway)
			// Also, the points are around a circle with diameter of 1.  Each ring will define it's own x,y scale

			Point[] retVal = new Point[numSides];

			//double stepAngle = 360d / numSides;
			double stepRadians = (Math.PI * 2d) / numSides;

			for (int cntr = 0; cntr < numSides; cntr++)
			{
				double radians = stepRadians * cntr;

				double x = .5d * Math.Cos(radians);
				double y = .5d * Math.Sin(radians);

				retVal[cntr] = new Point(x, y);
			}

			// Exit Function
			return retVal;
		}
		private static void GetMultiRingedTubeSprtEndCap_ORIG(ref int pointOffset, MeshGeometry3D geometry, int numSides, Point[] points, TubeRingDefinition_ORIG ring, bool isFirst, double z)
		{
			#region Figure out the normal

			Vector3D normal;
			if (isFirst)
			{
				// The first is in the negative Z, so the normal points down
				// For some reason, it's backward from what I think it should be
				normal = new Vector3D(0, 0, 1);
			}
			else
			{
				normal = new Vector3D(0, 0, 1);
			}

			#endregion

			#region Add points and normals

			for (int cntr = 0; cntr < numSides; cntr++)
			{
				double x = ring.RadiusX * points[cntr].X;
				double y = ring.RadiusY * points[cntr].Y;

				geometry.Positions.Add(new Point3D(x, y, z));

				geometry.Normals.Add(normal);
			}

			#endregion

			#region Add the triangles

			// Start with 0,1,2
			geometry.TriangleIndices.Add(pointOffset + 0);
			geometry.TriangleIndices.Add(pointOffset + 1);
			geometry.TriangleIndices.Add(pointOffset + 2);

			int lowerIndex = 2;
			int upperIndex = numSides - 1;
			int lastUsedIndex = 0;
			bool shouldBumpLower = true;

			// Do the rest of the triangles
			while (lowerIndex < upperIndex)
			{
				geometry.TriangleIndices.Add(pointOffset + lowerIndex);
				geometry.TriangleIndices.Add(pointOffset + upperIndex);
				geometry.TriangleIndices.Add(pointOffset + lastUsedIndex);

				if (shouldBumpLower)
				{
					lastUsedIndex = lowerIndex;
					lowerIndex++;
				}
				else
				{
					lastUsedIndex = upperIndex;
					upperIndex--;
				}

				shouldBumpLower = !shouldBumpLower;
			}

			#endregion

			pointOffset += numSides;
		}
		private static void GetMultiRingedTubeSprtBetweenRings_ORIG(ref int pointOffset, MeshGeometry3D geometry, int numSides, Point[] points, TubeRingDefinition_ORIG ring1, TubeRingDefinition_ORIG ring2, double z)
		{
			if (ring1.RingType == TubeRingType_ORIG.Point || ring2.RingType == TubeRingType_ORIG.Point)
			{
				GetMultiRingedTubeSprtBetweenRingsSprtPyramid_ORIG(ref pointOffset, geometry, numSides, points, ring1, ring2, z);
			}
			else
			{
				GetMultiRingedTubeSprtBetweenRingsSprtTube_ORIG(ref pointOffset, geometry, numSides, points, ring1, ring2, z);
			}

			#region OLD
			/*
            if (ring1.IsPoint)
            {
            }
            else if (ring2.IsPoint)
            {
                #region Pyramid 2

                #region Add points and normals

                // Determine 3D positions (they are referenced a lot, so just calculating them once
                Point3D tipPoint = new Point3D(0, 0, z + ring2.DistFromPrevRing);

                Point3D[] sidePoints = new Point3D[numSides];
                for (int cntr = 0; cntr < numSides; cntr++)
                {
                    sidePoints[cntr] = new Point3D(ring1.SizeX * points[cntr].X, ring1.SizeY * points[cntr].Y, z);
                }

                Vector3D v1, v2;

                // Sides - adding the points twice, since 2 triangles will use each point (and each triangle gets its own point)
                for (int cntr = 0; cntr < numSides; cntr++)
                {
                    // Even
                    geometry.Positions.Add(sidePoints[cntr]);

                    #region normal

                    // (tip - cur) x (prev - cur)

                    v1 = tipPoint.ToVector() - sidePoints[cntr].ToVector();
                    if (cntr == 0)
                    {
                        v2 = sidePoints[sidePoints.Length - 1].ToVector() - sidePoints[0].ToVector();
                    }
                    else
                    {
                        v2 = sidePoints[cntr - 1].ToVector() - sidePoints[cntr].ToVector();
                    }

                    geometry.Normals.Add(Vector3D.CrossProduct(v1, v2));

                    #endregion

                    // Odd
                    geometry.Positions.Add(sidePoints[cntr]);

                    #region normal

                    // (next - cur) x (tip - cur)

                    if (cntr == sidePoints.Length - 1)
                    {
                        v1 = sidePoints[0].ToVector() - sidePoints[cntr].ToVector();
                    }
                    else
                    {
                        v1 = sidePoints[cntr + 1].ToVector() - sidePoints[cntr].ToVector();
                    }

                    v2 = tipPoint.ToVector() - sidePoints[cntr].ToVector();

                    geometry.Normals.Add(Vector3D.CrossProduct(v1, v2));

                    #endregion
                }

                int lastPoint = numSides * 2;

                // Top point (all triangles use this same one)
                geometry.Positions.Add(tipPoint);

                // This one is straight up
                geometry.Normals.Add(new Vector3D(0, 0, 1));

                #endregion

                #region Add the triangles

                for (int cntr = 0; cntr < numSides; cntr++)
                {
                    geometry.TriangleIndices.Add(pointOffset + ((cntr * 2) + 1));      // this will be the second of the pair of points at this location

                    if (cntr == numSides - 1)
                    {
                        geometry.TriangleIndices.Add(pointOffset);  // on the last point, so loop back to point zero
                    }
                    else
                    {
                        geometry.TriangleIndices.Add(pointOffset + ((cntr + 1) * 2));      // this will be the first of the pair of points at the next location
                    }

                    geometry.TriangleIndices.Add(pointOffset + lastPoint);   // the tip
                }

                #endregion

                #endregion
            }
            else
            {
            }
            */
			#endregion
		}
		private static void GetMultiRingedTubeSprtBetweenRingsSprtPyramid_ORIG(ref int pointOffset, MeshGeometry3D geometry, int numSides, Point[] points, TubeRingDefinition_ORIG ring1, TubeRingDefinition_ORIG ring2, double z)
		{
			#region Add points and normals

			#region Determine 3D positions (they are referenced a lot, so just calculating them once

			Point3D tipPoint;
			Vector3D tipNormal;
			double sideZ, sizeX, sizeY;
			if (ring1.RingType == TubeRingType_ORIG.Point)
			{
				// Upside down pyramid
				tipPoint = new Point3D(0, 0, z);
				tipNormal = new Vector3D(0, 0, -1);
				sideZ = z + ring2.DistFromPrevRing;
				sizeX = ring2.RadiusX;
				sizeY = ring2.RadiusY;
			}
			else
			{
				// Rightside up pyramid
				tipPoint = new Point3D(0, 0, z + ring2.DistFromPrevRing);
				tipNormal = new Vector3D(0, 0, 1);
				sideZ = z;
				sizeX = ring1.RadiusX;
				sizeY = ring1.RadiusY;
			}

			Point3D[] sidePoints = new Point3D[numSides];
			for (int cntr = 0; cntr < numSides; cntr++)
			{
				sidePoints[cntr] = new Point3D(sizeX * points[cntr].X, sizeY * points[cntr].Y, sideZ);
			}

			#endregion

			Vector3D v1, v2;

			// Sides - adding the points twice, since 2 triangles will use each point (and each triangle gets its own point)
			for (int cntr = 0; cntr < numSides; cntr++)
			{
				// Even
				geometry.Positions.Add(sidePoints[cntr]);

				#region normal

				// (tip - cur) x (prev - cur)

				v1 = tipPoint.ToVector() - sidePoints[cntr].ToVector();
				if (cntr == 0)
				{
					v2 = sidePoints[sidePoints.Length - 1].ToVector() - sidePoints[0].ToVector();
				}
				else
				{
					v2 = sidePoints[cntr - 1].ToVector() - sidePoints[cntr].ToVector();
				}

				geometry.Normals.Add(Vector3D.CrossProduct(v1, v2));

				#endregion

				// Odd
				geometry.Positions.Add(sidePoints[cntr]);

				#region normal

				// (next - cur) x (tip - cur)

				if (cntr == sidePoints.Length - 1)
				{
					v1 = sidePoints[0].ToVector() - sidePoints[cntr].ToVector();
				}
				else
				{
					v1 = sidePoints[cntr + 1].ToVector() - sidePoints[cntr].ToVector();
				}

				v2 = tipPoint.ToVector() - sidePoints[cntr].ToVector();

				geometry.Normals.Add(Vector3D.CrossProduct(v1, v2));

				#endregion
			}

			int lastPoint = numSides * 2;

			//TODO: This is wrong, each triangle should have its own copy of the point with the normal the same as the 2 base points
			// Top point (all triangles use this same one)
			geometry.Positions.Add(tipPoint);

			// This one is straight up
			geometry.Normals.Add(tipNormal);

			#endregion

			#region Add the triangles

			for (int cntr = 0; cntr < numSides; cntr++)
			{
				geometry.TriangleIndices.Add(pointOffset + ((cntr * 2) + 1));      // this will be the second of the pair of points at this location

				if (cntr == numSides - 1)
				{
					geometry.TriangleIndices.Add(pointOffset);  // on the last point, so loop back to point zero
				}
				else
				{
					geometry.TriangleIndices.Add(pointOffset + ((cntr + 1) * 2));      // this will be the first of the pair of points at the next location
				}

				geometry.TriangleIndices.Add(pointOffset + lastPoint);   // the tip
			}

			#endregion

			pointOffset += lastPoint + 1;
		}
		private static void GetMultiRingedTubeSprtBetweenRingsSprtTube_ORIG(ref int pointOffset, MeshGeometry3D geometry, int numSides, Point[] points, TubeRingDefinition_ORIG ring1, TubeRingDefinition_ORIG ring2, double z)
		{
			#region Add points and normals

			#region Determine 3D positions (they are referenced a lot, so just calculating them once

			Point3D[] sidePoints1 = new Point3D[numSides];
			Point3D[] sidePoints2 = new Point3D[numSides];
			for (int cntr = 0; cntr < numSides; cntr++)
			{
				sidePoints1[cntr] = new Point3D(ring1.RadiusX * points[cntr].X, ring1.RadiusY * points[cntr].Y, z);
				sidePoints2[cntr] = new Point3D(ring2.RadiusX * points[cntr].X, ring2.RadiusY * points[cntr].Y, z + ring2.DistFromPrevRing);
			}

			#endregion
			#region Determine normals

			Vector3D v1, v2;

			// The normal at 0 is for the face between 0 and 1.  The normal at 1 is the face between 1 and 2, etc.
			Vector3D[] sideNormals = new Vector3D[numSides];
			for (int cntr = 0; cntr < numSides; cntr++)
			{
				// (next1 - cur1) x (cur2 - cur1)

				if (cntr == numSides - 1)
				{
					v1 = sidePoints1[0] - sidePoints1[cntr];
				}
				else
				{
					v1 = sidePoints1[cntr + 1] - sidePoints1[cntr];
				}

				v2 = sidePoints2[cntr] - sidePoints1[cntr];

				sideNormals[cntr] = Vector3D.CrossProduct(v1, v2);
			}

			#endregion

			#region Commit points/normals

			for (int ringCntr = 1; ringCntr <= 2; ringCntr++)     // I want all the points in ring1 laid down before doing ring2 (this stays similar to the pyramid method's logic)
			{
				// Sides - adding the points twice, since 2 triangles will use each point (and each triangle gets its own point)
				for (int cntr = 0; cntr < numSides; cntr++)
				{
					// Even
					if (ringCntr == 1)
					{
						geometry.Positions.Add(sidePoints1[cntr]);
					}
					else
					{
						geometry.Positions.Add(sidePoints2[cntr]);
					}

					// even always selects the previous side's normal
					if (cntr == 0)
					{
						geometry.Normals.Add(sideNormals[numSides - 1]);
					}
					else
					{
						geometry.Normals.Add(sideNormals[cntr - 1]);
					}

					// Odd
					if (ringCntr == 1)
					{
						geometry.Positions.Add(sidePoints1[cntr]);
					}
					else
					{
						geometry.Positions.Add(sidePoints2[cntr]);
					}

					// odd always selects the current side's normal
					geometry.Normals.Add(sideNormals[cntr]);
				}
			}

			#endregion

			int ring2Start = numSides * 2;

			#endregion

			#region Add the triangles

			for (int cntr = 0; cntr < numSides; cntr++)
			//for (int cntr = 0; cntr < 1; cntr++)
			{
				//--------------Bottom Right Triangle

				// Ring 1, bottom left
				geometry.TriangleIndices.Add(pointOffset + ((cntr * 2) + 1));      // this will be the second of the pair of points at this location

				// Ring 1, bottom right
				if (cntr == numSides - 1)
				{
					geometry.TriangleIndices.Add(pointOffset);  // on the last point, so loop back to point zero
				}
				else
				{
					geometry.TriangleIndices.Add(pointOffset + ((cntr + 1) * 2));      // this will be the first of the pair of points at the next location
				}

				// Ring 2, top right (adding twice, because it starts the next triangle)
				if (cntr == numSides - 1)
				{
					geometry.TriangleIndices.Add(pointOffset + ring2Start);  // on the last point, so loop back to point zero
					geometry.TriangleIndices.Add(pointOffset + ring2Start);
				}
				else
				{
					geometry.TriangleIndices.Add(pointOffset + ring2Start + ((cntr + 1) * 2));      // this will be the first of the pair of points at the next location
					geometry.TriangleIndices.Add(pointOffset + ring2Start + ((cntr + 1) * 2));
				}

				//--------------Top Left Triangle

				// Ring 2, top left
				geometry.TriangleIndices.Add(pointOffset + ring2Start + ((cntr * 2) + 1));      // this will be the second of the pair of points at this location

				// Ring 1, bottom left (same as the very first point added in this for loop)
				geometry.TriangleIndices.Add(pointOffset + ((cntr * 2) + 1));      // this will be the second of the pair of points at this location
			}

			#endregion

			pointOffset += numSides * 4;
		}

		#endregion
		#region GetMultiRingedTube helpers

		private static void InitializeMultiRingedTube(out double height, out double startZ, int numSides, List<TubeRingBase> rings, bool shouldCenterZ)
		{
			#region Validate

			if (rings.Count == 1)
			{
				TubeRingRegularPolygon ringCast = rings[0] as TubeRingRegularPolygon;
				if (ringCast == null || !ringCast.IsClosedIfEndCap)
				{
					throw new ArgumentException("Only a single ring was passed in, so the only valid type is a closed ring: " + rings[0].GetType().ToString());
				}
			}
			else if (rings.Count == 2)
			{
				if (!rings.Any(o => o is TubeRingRegularPolygon))
				{
					//	Say both are points - you'd have a line.  Domes must attach to a ring, not a point or another dome
					throw new ArgumentException(string.Format("When only two rings definitions are passed in, at least one of them must be a ring:\r\n{0}\r\n{1}", rings[0].GetType().ToString(), rings[1].GetType().ToString()));
				}
			}

			if (numSides < 3)
			{
				throw new ArgumentException("numSides must be at least 3: " + numSides.ToString(), "numSides");
			}

			#endregion

			//	Calculate total height
			height = 0d;
			for (int cntr = 1; cntr < rings.Count; cntr++)     // the first ring's distance from prev ring is ignored
			{
				//	I had a case where I wanted to make an arrow where the end cap comes backward a bit
				//if (rings[cntr].DistFromPrevRing <= 0)
				//{
				//    throw new ArgumentException("DistFromPrevRing must be positive: " + rings[cntr].DistFromPrevRing.ToString());
				//}

				height += rings[cntr].DistFromPrevRing;
			}

			//	Figure out the starting Z
			startZ = 0d;
			if (shouldCenterZ)
			{
				startZ = height * -.5d;      // starting in the negative
			}
		}

		private static Point[] MultiRingSprtGetPointsRegPoly(int numSides, TubeRingRegularPolygon ring)
		{
			//	Multiply the returned unit circle by the ring's radius
			return ring.GetUnitPointsTheta(numSides).Select(o => new Point(ring.RadiusX * o.X, ring.RadiusY * o.Y)).ToArray();
		}

		private static void MultiRingEndCap(ref int pointOffset, ref double[] rotateAnglesForPerp, MeshGeometry3D geometry, int numSides, TubeRingBase ringPrev, TubeRingBase ringCurrent, TubeRingBase ringNext, Transform3D transform, bool isFirst, double z, bool softSides)
		{
			if (ringCurrent is TubeRingDome)
			{
				#region Dome

				Point[] domePointsTheta = MultiRingEndCapSprtGetPoints(ringPrev, ringNext, numSides, isFirst);
				double capHeight = MultiRingEndCapSprtGetCapHeight(ringCurrent, ringNext, isFirst);
				Transform3D domeTransform = MultiRingEndCapSprtGetTransform(transform, isFirst, z, capHeight);
				Transform3D domeTransformNormal = MultiRingEndCapSprtGetNormalTransform(domeTransform);

				if (softSides)
				{
					MultiRingEndCapSprtDomeSoft(ref pointOffset, ref rotateAnglesForPerp, geometry, domePointsTheta, domeTransform, domeTransformNormal, (TubeRingDome)ringCurrent, capHeight, isFirst);
				}
				else
				{
					throw new ApplicationException("finish this");
				}

				#endregion
			}
			else if (ringCurrent is TubeRingPoint)
			{
				#region Point

				Point[] conePointsTheta = MultiRingEndCapSprtGetPoints(ringPrev, ringNext, numSides, isFirst);
				double capHeight = MultiRingEndCapSprtGetCapHeight(ringCurrent, ringNext, isFirst);
				Transform3D coneTransform = MultiRingEndCapSprtGetTransform(transform, isFirst, z, capHeight);
				Transform3D coneTransformNormal = MultiRingEndCapSprtGetNormalTransform(coneTransform);

				if (softSides)
				{
					MultiRingEndCapSprtConeSoft(ref pointOffset, ref rotateAnglesForPerp, geometry, conePointsTheta, coneTransform, coneTransformNormal, (TubeRingPoint)ringCurrent, capHeight, isFirst);
				}
				else
				{
					throw new ApplicationException("finish this");
				}

				#endregion
			}
			else if (ringCurrent is TubeRingRegularPolygon)
			{
				#region Regular Polygon

				TubeRingRegularPolygon ringCurrentCast = (TubeRingRegularPolygon)ringCurrent;

				Point[] polyPointsTheta = MultiRingSprtGetPointsRegPoly(numSides, ringCurrentCast);
				Transform3D polyTransform = MultiRingEndCapSprtGetTransform(transform, isFirst, z);
				Transform3D polyTransformNormal = MultiRingEndCapSprtGetNormalTransform(polyTransform);

				if (ringCurrentCast.IsClosedIfEndCap)		//	if it's open, there is nothing to do for the end cap
				{
					if (softSides)
					{
						MultiRingEndCapSprtPlateSoft(ref pointOffset, ref rotateAnglesForPerp, geometry, polyPointsTheta, polyTransform, polyTransformNormal, ringCurrent, isFirst);
					}
					else
					{
						throw new ApplicationException("finish this");
					}
				}

				#endregion
			}
			else
			{
				throw new ApplicationException("Unknown tube ring type: " + ringCurrent.GetType().ToString());
			}
		}
		private static double MultiRingEndCapSprtGetCapHeight(TubeRingBase ringCurrent, TubeRingBase ringNext, bool isFirst)
		{
			if (isFirst)
			{
				//	ringCurrent.DistFromPrevRing is ignored (because there is no previous).  So the cap's height is the next ring's dist from prev
				return ringNext.DistFromPrevRing;
			}
			else
			{
				//	This is the last, so dist from prev has meaning
				return ringCurrent.DistFromPrevRing;
			}
		}
		private static Point[] MultiRingEndCapSprtGetPoints(TubeRingBase ringPrev, TubeRingBase ringNext, int numSides, bool isFirst)
		{
			//	Figure out which ring to pull from
			TubeRingBase ring = null;
			if (isFirst)
			{
				ring = ringNext;
			}
			else
			{
				ring = ringPrev;
			}

			//	Get the points
			Point[] retVal = null;
			if (ring != null && ring is TubeRingRegularPolygon)
			{
				retVal = MultiRingSprtGetPointsRegPoly(numSides, (TubeRingRegularPolygon)ring);
			}

			if (retVal == null)
			{
				throw new ApplicationException("The points are null for dome/point.  Validation should have caught this before now");
			}

			//	Exit Function
			return retVal;
		}
		private static Transform3D MultiRingEndCapSprtGetTransform(Transform3D transform, bool isFirst, double z)
		{
			//	This overload is for a flat plate

			Transform3DGroup retVal = new Transform3DGroup();

			if (isFirst)
			{
				//	This still needs to be flipped for a flat cap so the normals turn out right
				retVal.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 180d)));
				retVal.Children.Add(new TranslateTransform3D(0, 0, z));
				retVal.Children.Add(transform);
			}
			else
			{
				retVal.Children.Add(new TranslateTransform3D(0, 0, z));
				retVal.Children.Add(transform);
			}

			return retVal;
		}
		private static Transform3D MultiRingEndCapSprtGetTransform(Transform3D transform, bool isFirst, double z, double capHeight)
		{
			//This overload is for a cone/dome

			Transform3DGroup retVal = new Transform3DGroup();

			if (isFirst)
			{
				//	The dome/point methods are hard coded to go from 0 to capHeight, so rotate it so it will build from capHeight
				//	down to zero (offset by z)
				retVal.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 180d)));
				retVal.Children.Add(new TranslateTransform3D(0, 0, z + capHeight));
				retVal.Children.Add(transform);
			}
			else
			{
				retVal.Children.Add(new TranslateTransform3D(0, 0, z - capHeight));		//	z is currently at the tip of the dome (it works for a flat cap, but a dome has height, so must back up to the last drawn object)
				retVal.Children.Add(transform);
			}

			return retVal;
		}
		private static Transform3D MultiRingEndCapSprtGetNormalTransform(Transform3D transform)
		{
			//	Can't use all of the transform passed in for the normal, because translate portions will skew the normals funny
			Transform3DGroup retVal = new Transform3DGroup();
			if (transform is Transform3DGroup)
			{
				foreach (var subTransform in ((Transform3DGroup)transform).Children)
				{
					if (!(subTransform is TranslateTransform3D))
					{
						retVal.Children.Add(subTransform);
					}
				}
			}
			else if (transform is TranslateTransform3D)
			{
				retVal.Children.Add(Transform3D.Identity);
			}
			else
			{
				retVal.Children.Add(transform);
			}

			return retVal;
		}

		private static void MultiRingEndCapSprtDomeSoft(ref int pointOffset, ref double[] rotateAnglesForPerp, MeshGeometry3D geometry, Point[] pointsTheta, Transform3D transform, Transform3D normalTransform, TubeRingDome ring, double capHeight, bool isFirst)
		{
			//	NOTE: There is one more than NumSegmentsPhi
			Point[] pointsPhi = ring.GetUnitPointsPhi(ring.NumSegmentsPhi);

			#region Positions/Normals

			//for (int phiCntr = 0; phiCntr < numSegmentsPhi; phiCntr++)		//	The top point will be added after this loop
			for (int phiCntr = pointsPhi.Length - 1; phiCntr > 0; phiCntr--)
			{
				if (!isFirst && ring.MergeNormalWithPrevIfSoft)
				{
					//	Just reuse the points/normals from the previous ring
					continue;
				}

				for (int thetaCntr = 0; thetaCntr < pointsTheta.Length; thetaCntr++)
				{
					//	Phi points are going from bottom to equator.  
					//	pointsTheta are already the length they are supposed to be (not nessassarily a unit circle)

					Point3D point = new Point3D(
						pointsTheta[thetaCntr].X * pointsPhi[phiCntr].Y,
						pointsTheta[thetaCntr].Y * pointsPhi[phiCntr].Y,
						capHeight * pointsPhi[phiCntr].X);

					geometry.Positions.Add(transform.Transform(point));

					if (ring.MergeNormalWithPrevIfSoft)
					{
						//TODO:  Merge the normal with rotateAngleForPerp (see the GetCone method)
						throw new ApplicationException("finish this");
					}
					else
					{
						geometry.Normals.Add(normalTransform.Transform(point).ToVector().ToUnit());		//	the normal is the same as the point for a sphere (but no tranlate transform)
					}
				}
			}

			//	This is north pole point
			geometry.Positions.Add(transform.Transform(new Point3D(0, 0, capHeight)));
			geometry.Normals.Add(transform.Transform(new Vector3D(0, 0, 1)));

			#endregion

			#region Triangles - Rings

			int zOffsetBottom = pointOffset;
			int zOffsetTop;

			for (int phiCntr = 0; phiCntr < ring.NumSegmentsPhi - 1; phiCntr++)		//	The top cone will be added after this loop
			{
				zOffsetTop = zOffsetBottom + pointsTheta.Length;

				for (int thetaCntr = 0; thetaCntr < pointsTheta.Length - 1; thetaCntr++)
				{
					//	Top/Left triangle
					geometry.TriangleIndices.Add(zOffsetBottom + thetaCntr + 0);
					geometry.TriangleIndices.Add(zOffsetTop + thetaCntr + 1);
					geometry.TriangleIndices.Add(zOffsetTop + thetaCntr + 0);

					//	Bottom/Right triangle
					geometry.TriangleIndices.Add(zOffsetBottom + thetaCntr + 0);
					geometry.TriangleIndices.Add(zOffsetBottom + thetaCntr + 1);
					geometry.TriangleIndices.Add(zOffsetTop + thetaCntr + 1);
				}

				//	Connecting the last 2 points to the first 2
				//	Top/Left triangle
				geometry.TriangleIndices.Add(zOffsetBottom + (pointsTheta.Length - 1) + 0);
				geometry.TriangleIndices.Add(zOffsetTop);		//	wrapping back around
				geometry.TriangleIndices.Add(zOffsetTop + (pointsTheta.Length - 1) + 0);

				//	Bottom/Right triangle
				geometry.TriangleIndices.Add(zOffsetBottom + (pointsTheta.Length - 1) + 0);
				geometry.TriangleIndices.Add(zOffsetBottom);
				geometry.TriangleIndices.Add(zOffsetTop);

				//	Prep for the next ring
				zOffsetBottom = zOffsetTop;
			}

			#endregion
			#region Triangles - Cap

			int topIndex = geometry.Positions.Count - 1;

			for (int cntr = 0; cntr < pointsTheta.Length - 1; cntr++)
			{
				geometry.TriangleIndices.Add(zOffsetBottom + cntr + 0);
				geometry.TriangleIndices.Add(zOffsetBottom + cntr + 1);
				geometry.TriangleIndices.Add(topIndex);
			}

			//	The last triangle links back to zero
			geometry.TriangleIndices.Add(zOffsetBottom + pointsTheta.Length - 1 + 0);
			geometry.TriangleIndices.Add(zOffsetBottom + 0);
			geometry.TriangleIndices.Add(topIndex);

			#endregion

			pointOffset = geometry.Positions.Count;
		}

		private static void MultiRingEndCapSprtConeSoft(ref int pointOffset, ref double[] rotateAnglesForPerp, MeshGeometry3D geometry, Point[] pointsTheta, Transform3D transform, Transform3D normalTransform, TubeRingPoint ring, double capHeight, bool isFirst)
		{
			#region Positions/Normals

			if (isFirst || !ring.MergeNormalWithPrevIfSoft)
			{
				for (int thetaCntr = 0; thetaCntr < pointsTheta.Length; thetaCntr++)
				{
					Point3D point = new Point3D(pointsTheta[thetaCntr].X, pointsTheta[thetaCntr].Y, 0d);
					geometry.Positions.Add(transform.Transform(point));
					geometry.Normals.Add(normalTransform.Transform(point).ToVector().ToUnit());		//	the normal is the same as the point for a sphere (but no tranlate transform)
				}
			}

			//	Cone tip
			geometry.Positions.Add(transform.Transform(new Point3D(0, 0, capHeight)));
			geometry.Normals.Add(transform.Transform(new Vector3D(0, 0, 1)));

			#endregion

			#region Triangles

			int topIndex = geometry.Positions.Count - 1;

			for (int cntr = 0; cntr < pointsTheta.Length - 1; cntr++)
			{
				geometry.TriangleIndices.Add(pointOffset + cntr + 0);
				geometry.TriangleIndices.Add(pointOffset + cntr + 1);
				geometry.TriangleIndices.Add(topIndex);
			}

			//	The last triangle links back to zero
			geometry.TriangleIndices.Add(pointOffset + pointsTheta.Length - 1 + 0);
			geometry.TriangleIndices.Add(pointOffset + 0);
			geometry.TriangleIndices.Add(topIndex);

			#endregion

			pointOffset = geometry.Positions.Count;
		}

		private static void MultiRingEndCapSprtPlateSoft(ref int pointOffset, ref double[] rotateAnglesForPerp, MeshGeometry3D geometry, Point[] pointsTheta, Transform3D transform, Transform3D normalTransform, TubeRingBase ring, bool isFirst)
		{
			#region Positions/Normals

			if (isFirst || !ring.MergeNormalWithPrevIfSoft)
			{
				for (int thetaCntr = 0; thetaCntr < pointsTheta.Length; thetaCntr++)
				{
					Point3D point = new Point3D(pointsTheta[thetaCntr].X, pointsTheta[thetaCntr].Y, 0d);
					geometry.Positions.Add(transform.Transform(point));

					Vector3D normal;
					if (ring.MergeNormalWithPrevIfSoft)
					{
						//normal = point.ToVector();		//	this isn't right
						throw new ApplicationException("finish this");
					}
					else
					{
						normal = new Vector3D(0, 0, 1);
					}

					geometry.Normals.Add(normalTransform.Transform(normal).ToUnit());
				}
			}

			#endregion

			#region Add the triangles

			// Start with 0,1,2
			geometry.TriangleIndices.Add(pointOffset + 0);
			geometry.TriangleIndices.Add(pointOffset + 1);
			geometry.TriangleIndices.Add(pointOffset + 2);

			int lowerIndex = 2;
			int upperIndex = pointsTheta.Length - 1;
			int lastUsedIndex = 0;
			bool shouldBumpLower = true;

			// Do the rest of the triangles
			while (lowerIndex < upperIndex)
			{
				geometry.TriangleIndices.Add(pointOffset + lowerIndex);
				geometry.TriangleIndices.Add(pointOffset + upperIndex);
				geometry.TriangleIndices.Add(pointOffset + lastUsedIndex);

				if (shouldBumpLower)
				{
					lastUsedIndex = lowerIndex;
					lowerIndex++;
				}
				else
				{
					lastUsedIndex = upperIndex;
					upperIndex--;
				}

				shouldBumpLower = !shouldBumpLower;
			}

			#endregion

			pointOffset = geometry.Positions.Count;
		}

		private static void MultiRingMiddle(ref int pointOffset, ref double[] rotateAnglesForPerp, MeshGeometry3D geometry, Transform3D transform, int numSides, TubeRingBase ring1, TubeRingBase ring2, double curZ, bool softSides)
		{
			if (ring1 is TubeRingRegularPolygon && ring2 is TubeRingRegularPolygon)
			{
				#region Tube

				if (softSides)
				{
					MultiRingMiddleSprtTubeSoft(ref pointOffset, ref rotateAnglesForPerp, geometry, transform, numSides, (TubeRingRegularPolygon)ring1, (TubeRingRegularPolygon)ring2, curZ);
				}
				else
				{
					throw new ApplicationException("finish this");
				}

				#endregion
			}
			else
			{
				//	There are no other combos that need to show a visual right now (eventually, I'll have a definition for
				//	a non regular polygon - low in fiber)
			}
		}

		private static void MultiRingMiddleSprtTubeSoft(ref int pointOffset, ref double[] rotateAnglesForPerp, MeshGeometry3D geometry, Transform3D transform, int numSides, TubeRingRegularPolygon ring1, TubeRingRegularPolygon ring2, double curZ)
		{
			if (ring1.MergeNormalWithPrevIfSoft || ring2.MergeNormalWithPrevIfSoft)
			{
				throw new ApplicationException("finish this");
			}

			Point[] points1 = ring1.GetUnitPointsTheta(numSides);
			Point[] points2 = ring2.GetUnitPointsTheta(numSides);

			#region Points/Normals

			//TODO: Don't add the bottom ring's points, only the top

			//	Ring 1
			for (int cntr = 0; cntr < numSides; cntr++)
			{
				geometry.Positions.Add(transform.Transform(new Point3D(points1[cntr].X * ring1.RadiusX, points1[cntr].Y * ring1.RadiusY, curZ)));
				geometry.Normals.Add(transform.Transform(new Vector3D(points1[cntr].X * ring1.RadiusX, points1[cntr].Y * ring1.RadiusY, 0d).ToUnit()));		//	the normals point straight out of the side
			}

			//	Ring 2
			for (int cntr = 0; cntr < numSides; cntr++)
			{
				geometry.Positions.Add(transform.Transform(new Point3D(points2[cntr].X * ring2.RadiusX, points2[cntr].Y * ring2.RadiusY, curZ + ring2.DistFromPrevRing)));
				geometry.Normals.Add(transform.Transform(new Vector3D(points2[cntr].X * ring2.RadiusX, points2[cntr].Y * ring2.RadiusY, 0d).ToUnit()));		//	the normals point straight out of the side
			}

			#endregion

			#region Triangles

			int zOffsetBottom = pointOffset;
			int zOffsetTop = zOffsetBottom + numSides;

			for (int cntr = 0; cntr < numSides - 1; cntr++)
			{
				//	Top/Left triangle
				geometry.TriangleIndices.Add(zOffsetBottom + cntr + 0);
				geometry.TriangleIndices.Add(zOffsetTop + cntr + 1);
				geometry.TriangleIndices.Add(zOffsetTop + cntr + 0);

				//	Bottom/Right triangle
				geometry.TriangleIndices.Add(zOffsetBottom + cntr + 0);
				geometry.TriangleIndices.Add(zOffsetBottom + cntr + 1);
				geometry.TriangleIndices.Add(zOffsetTop + cntr + 1);
			}

			//	Connecting the last 2 points to the first 2
			//	Top/Left triangle
			geometry.TriangleIndices.Add(zOffsetBottom + (numSides - 1) + 0);
			geometry.TriangleIndices.Add(zOffsetTop);		//	wrapping back around
			geometry.TriangleIndices.Add(zOffsetTop + (numSides - 1) + 0);

			//	Bottom/Right triangle
			geometry.TriangleIndices.Add(zOffsetBottom + (numSides - 1) + 0);
			geometry.TriangleIndices.Add(zOffsetBottom);
			geometry.TriangleIndices.Add(zOffsetTop);

			#endregion

			pointOffset = geometry.Positions.Count;
		}

		#endregion

		#region OLD

		//public static MeshGeometry3D GetCone_AlongX_OLD(int numSegments, double radius, double height)
		//{
		//    //NOTE:  This is a copy of the cylinder method with very minor tweaks (commented code is from the cylinder)

		//    MeshGeometry3D retVal = new MeshGeometry3D();

		//    Point3D origin = new Point3D(0, 0, 0);
		//    Vector3D negX = new Vector3D(-1, 0, 0);
		//    Point texCoordTop = new Point(0, 0);
		//    Point texCoordBottom = new Point(1, 0);
		//    double lonDeltaTheta = 2.0 * Math.PI / (double)numSegments;
		//    double x0 = height / 2;
		//    double x1 = -x0;
		//    double lonTheta = Math.PI;
		//    ushort indices = 0;
		//    Vector3D normUp = new Vector3D(1, 0, 0);

		//    for (int lon = 0; lon < numSegments; lon++)
		//    {
		//        double u0 = (double)lon / (double)numSegments;
		//        double y0 = radius * Math.Cos(lonTheta);
		//        double z0 = radius * Math.Sin(lonTheta);

		//        if (lon == numSegments - 1)
		//        {
		//            lonTheta = Math.PI;
		//        }
		//        else
		//        {
		//            lonTheta -= lonDeltaTheta;
		//        }

		//        double u1 = (double)(lon + 1) / (double)numSegments;
		//        double y1 = radius * Math.Cos(lonTheta);
		//        double z1 = radius * Math.Sin(lonTheta);
		//        Point3D p0 = new Point3D(x1, y0, z0);
		//        Point3D p1 = new Point3D(x0, 0, 0);		//Point3D p1 = new Point3D(x0, y0, z0);
		//        Point3D p2 = new Point3D(x1, y1, z1);
		//        Point3D p3 = new Point3D(x0, 0, 0);		//Point3D p3 = new Point3D(x0, y1, z1);
		//        Vector3D norm0 = new Vector3D(0, y0, z0);
		//        Vector3D norm1 = new Vector3D(0, y1, z1);

		//        norm0.Normalize();
		//        norm1.Normalize();

		//        // The triangles on the lateral face
		//        retVal.Positions.Add(p0);
		//        retVal.Positions.Add(p1);
		//        retVal.Positions.Add(p2);
		//        retVal.Positions.Add(p3);
		//        retVal.Normals.Add(norm0);
		//        retVal.Normals.Add(normUp);		//retVal.Normals.Add(norm0);
		//        retVal.Normals.Add(norm1);
		//        retVal.Normals.Add(normUp);		//retVal.Normals.Add(norm1);
		//        retVal.TextureCoordinates.Add(new Point(u0, 1));
		//        retVal.TextureCoordinates.Add(new Point(u0, 0));
		//        retVal.TextureCoordinates.Add(new Point(u1, 1));
		//        retVal.TextureCoordinates.Add(new Point(u1, 0));
		//        retVal.TriangleIndices.Add(indices++);  // 0
		//        retVal.TriangleIndices.Add(indices++);  // 1
		//        retVal.TriangleIndices.Add(indices++);  // 2
		//        retVal.TriangleIndices.Add(indices--);  // 3
		//        retVal.TriangleIndices.Add(indices--);  // 2
		//        retVal.TriangleIndices.Add(indices++);  // 1
		//        indices += 2;

		//        // The triangle at the base
		//        retVal.Positions.Add(p0);
		//        retVal.Positions.Add(p2);
		//        retVal.Positions.Add(new Point3D(x1, 0, 0));
		//        retVal.Normals.Add(negX);
		//        retVal.Normals.Add(negX);
		//        retVal.Normals.Add(negX);
		//        retVal.TextureCoordinates.Add(texCoordBottom);
		//        retVal.TextureCoordinates.Add(texCoordBottom);
		//        retVal.TextureCoordinates.Add(texCoordBottom);
		//        retVal.TriangleIndices.Add(indices++);
		//        retVal.TriangleIndices.Add(indices++);
		//        retVal.TriangleIndices.Add(indices++);
		//    }

		//    // Exit Function
		//    //retVal.Freeze();
		//    return retVal;
		//}

		#endregion

		#endregion
	}

	#region Enum: TubeRingType_ORIG

	//TODO: Turn dome into Dome_Hemisphere, Dome_Tangent.  If it's tangent, then the height will be calculated on the fly (allow for acute and obtuse angles)
	//If you have a dome_tangent - ring - dome_tangent, they both emulate dome_hemisphere (so a sphere with a radius defined by the middle ring)
	public enum TubeRingType_ORIG
	{
		Point,
		Ring_Open,
		Ring_Closed,
		Dome
	}

	#endregion
	#region Class: TubeRingDefinition_ORIG

	/// <summary>
	/// This defines a single ring - if it's the first or last in the list, then it's an end cap, and has more options of what it can
	/// be (rings in the middle can only be rings)
	/// </summary>
	/// <remarks>
	/// It might be worth it to get rid of the enum, and go with a base abstract class with derived ring/point/dome classes.
	/// ring and dome would both need radiusx/y though.  But it would make it easier to know what type uses what properties
	/// </remarks>
	public class TubeRingDefinition_ORIG
	{
		#region Constructor

		/// <summary>
		/// This overload defines a point (can only be used as an end cap)
		/// </summary>
		public TubeRingDefinition_ORIG(double distFromPrevRing, bool mergeNormalWithPrevIfSoft)
		{
			this.RingType = TubeRingType_ORIG.Point;
			this.DistFromPrevRing = distFromPrevRing;
			this.MergeNormalWithPrevIfSoft = mergeNormalWithPrevIfSoft;
		}

		/// <summary>
		/// This overload defines a polygon
		/// NOTE:  The number of points aren't defined within this class, but is the same for a list of these classes
		/// </summary>
		/// <param name="isClosedIfEndCap">
		/// Ignored if this is a middle ring.
		/// True: If this is an end cap, a disc is created to seal up the hull (like a lid on a cylinder).
		/// False: If this is an end cap, the space is left open (like an open bucket)
		/// </param>
		public TubeRingDefinition_ORIG(double radiusX, double radiusY, double distFromPrevRing, bool isClosedIfEndCap, bool mergeNormalWithPrevIfSoft)
		{
			if (isClosedIfEndCap)
			{
				this.RingType = TubeRingType_ORIG.Ring_Closed;
			}
			else
			{
				this.RingType = TubeRingType_ORIG.Ring_Open;
			}

			this.DistFromPrevRing = distFromPrevRing;
			this.RadiusX = radiusX;
			this.RadiusY = radiusY;
			this.MergeNormalWithPrevIfSoft = mergeNormalWithPrevIfSoft;
		}

		/// <summary>
		/// This overload defines a dome (can only be used as an end cap)
		/// TODO: Give an option for a partial dome (not a full hemisphere, but calculate where to start it so the dome is tangent with the neighboring ring)
		/// </summary>
		/// <param name="numSegmentsPhi">This lets you fine tune how many vertical separations there are in the dome (usually just use the same number as horizontal segments)</param>
		public TubeRingDefinition_ORIG(double radiusX, double radiusY, double distFromPrevRing, int numSegmentsPhi, bool mergeNormalWithPrevIfSoft)
		{
			this.RingType = TubeRingType_ORIG.Dome;
			this.RadiusX = radiusX;
			this.RadiusY = radiusY;
			this.DistFromPrevRing = distFromPrevRing;
			this.NumSegmentsPhi = numSegmentsPhi;
			this.MergeNormalWithPrevIfSoft = mergeNormalWithPrevIfSoft;
		}

		#endregion

		#region Public Properties

		public TubeRingType_ORIG RingType
		{
			get;
			private set;
		}

		/// <summary>
		/// This is how far this ring is (in Z) from the previous ring in the tube.  The first ring in the tube ignores this property
		/// </summary>
		public double DistFromPrevRing
		{
			get;
			private set;
		}

		/// <summary>
		/// This isn't the size of a single edge, but the radius of the ring.  So if you're making a cube with length one, you'd want the size
		/// to be .5*sqrt(2)
		/// </summary>
		public double RadiusX
		{
			get;
			private set;
		}
		public double RadiusY
		{
			get;
			private set;
		}

		/// <summary>
		/// This only has meaning if ringtype is dome
		/// </summary>
		public int NumSegmentsPhi
		{
			get;
			private set;
		}

		/// <summary>
		/// This is only looked at if doing soft sides
		/// True: When calculating normals, the prev ring is considered (this would make sense if the angle between the other ring and this one is low)
		/// False: The normals for this ring will be calculated independent of the prev ring (good for a traditional cone)
		/// </summary>
		/// <remarks>
		/// This only affects the normals at the bottom of the ring.  The top of the ring is defined by the next ring's property
		/// 
		/// This has no meaning for the very first ring
		/// 
		/// Examples:
		///	Cylinder - You would want false for both caps, because it's 90 degrees between the end cap and side
		///	Pyramid/Cone - You would want false, because it's greater than 90 degrees between the base cap and side
		///	Rings meant to look seamless - When the angle is low between two rings, that's when this should be true
		/// </remarks>
		public bool MergeNormalWithPrevIfSoft
		{
			get;
			private set;
		}

		#endregion
	}

	#endregion

	//TODO: Make a way to define a ring that isn't a regular polygon (like a rectangle), just takes a list of 2D Point.  Call it TubeRingPath.  See Game.Newt.AsteroidMiner2.ShipParts.ConverterRadiationToEnergyDesign.GetShape - the generic one will be a lot harder because one could be 3 points, the next could be 4.  QuickHull is too complex, but need something like that to see which points should make triangles from
	#region Class: TubeRingRegularPolygon

	public class TubeRingRegularPolygon : TubeRingBase
	{
		#region Class: PointsSingleton

		internal class PointsSingleton
		{
			#region Declaration Section

			private static readonly object _lockStatic = new object();
			private readonly object _lockInstance;

			/// <summary>
			/// The static constructor makes sure that this instance is created only once.  The outside users of this class
			/// call the static property Instance to get this one instance copy.  (then they can use the rest of the instance
			/// methods)
			/// </summary>
			private static PointsSingleton _instance;

			private SortedList<int, Point[]> _points;

			#endregion

			#region Constructor / Instance Property

			/// <summary>
			/// Static constructor.  Called only once before the first time you use my static properties/methods.
			/// </summary>
			static PointsSingleton()
			{
				lock (_lockStatic)
				{
					//	If the instance version of this class hasn't been instantiated yet, then do so
					if (_instance == null)
					{
						_instance = new PointsSingleton();
					}
				}
			}
			/// <summary>
			/// Instance constructor.  This is called only once by one of the calls from my static constructor.
			/// </summary>
			private PointsSingleton()
			{
				_lockInstance = new object();

				_points = new SortedList<int, Point[]>();
			}

			/// <summary>
			/// This is how you get at my instance.  The act of calling this property guarantees that the static constructor gets called
			/// exactly once (per process?)
			/// </summary>
			public static PointsSingleton Instance
			{
				get
				{
					//	There is no need to check the static lock, because _instance is only set one time, and that is guaranteed to be
					//	finished before this function gets called
					return _instance;
				}
			}

			#endregion

			#region Public Methods

			public Point[] GetPoints(int numSides)
			{
				lock (_lockInstance)
				{
					if (!_points.ContainsKey(numSides))
					{
						double deltaTheta = 2d * Math.PI / numSides;
						double theta = 0d;

						Point[] points = new Point[numSides];		//	these define a unit circle

						for (int cntr = 0; cntr < numSides; cntr++)
						{
							points[cntr] = new Point(Math.Cos(theta), Math.Sin(theta));
							theta += deltaTheta;
						}

						_points.Add(numSides, points);
					}

					return _points[numSides];
				}
			}

			#endregion
		}

		#endregion

		#region Constructor

		public TubeRingRegularPolygon(double distFromPrevRing, bool mergeNormalWithPrevIfSoft, double radiusX, double radiusY, bool isClosedIfEndCap)
			: base(distFromPrevRing, mergeNormalWithPrevIfSoft)
		{
			this.RadiusX = radiusX;
			this.RadiusY = radiusY;
			this.IsClosedIfEndCap = isClosedIfEndCap;
		}

		#endregion

		#region Public Properties

		/// <summary>
		/// This isn't the size of a single edge, but the radius of the ring.  So if you're making a cube with length one, you'd want the size
		/// to be .5*sqrt(2)
		/// </summary>
		public double RadiusX
		{
			get;
			private set;
		}
		public double RadiusY
		{
			get;
			private set;
		}

		/// <summary>
		/// This property only has meaning when this ring is the first or last in the list.
		/// True: If this is an end cap, a disc is created to seal up the hull (like a lid on a cylinder).
		/// False: If this is an end cap, the space is left open (like an open bucket)
		/// </summary>
		public bool IsClosedIfEndCap
		{
			get;
			private set;
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// This returns points in a unit circle that go around theta
		/// </summary>
		public Point[] GetUnitPointsTheta(int numSides)
		{
			return PointsSingleton.Instance.GetPoints(numSides);
		}

		#endregion
	}

	#endregion
	#region Class: TubeRingPoint

	/// <summary>
	/// This will end the tube in a point (like a cone or a pyramid)
	/// NOTE: This is only valid if this is the first or last ring in the list
	/// NOTE: This must be tied to a ring
	/// </summary>
	/// <remarks>
	/// This must be tied to a ring.  Two points become a line, a point directly to a dome has no meaning.
	/// 
	/// If you want to make a dual cone, you need three items in the list: { TubeRingPoint, TubeRing, TubeRingPoint }
	/// If you want to make an ice cream cone, you also need thee items: { TubeRingPoint, TubeRing, TubeRingDome }
	/// If you want a simple cone or pyramid, you only need two items: { TubeRingPoint, TubeRing }
	/// </remarks>
	public class TubeRingPoint : TubeRingBase
	{
		public TubeRingPoint(double distFromPrevRing, bool mergeNormalWithPrevIfSoft)
			: base(distFromPrevRing, mergeNormalWithPrevIfSoft) { }

		//	This doesn't need any extra properties
	}

	#endregion
	#region Class: TubeRingDome

	public class TubeRingDome : TubeRingBase
	{
		#region Class: PointsSingleton

		private class PointsSingleton
		{
			#region Declaration Section

			private static readonly object _lockStatic = new object();
			private readonly object _lockInstance;

			/// <summary>
			/// The static constructor makes sure that this instance is created only once.  The outside users of this class
			/// call the static property Instance to get this one instance copy.  (then they can use the rest of the instance
			/// methods)
			/// </summary>
			private static PointsSingleton _instance;

			private SortedList<int, Point[]> _points;

			#endregion

			#region Constructor / Instance Property

			/// <summary>
			/// Static constructor.  Called only once before the first time you use my static properties/methods.
			/// </summary>
			static PointsSingleton()
			{
				lock (_lockStatic)
				{
					//	If the instance version of this class hasn't been instantiated yet, then do so
					if (_instance == null)
					{
						_instance = new PointsSingleton();
					}
				}
			}
			/// <summary>
			/// Instance constructor.  This is called only once by one of the calls from my static constructor.
			/// </summary>
			private PointsSingleton()
			{
				_lockInstance = new object();

				_points = new SortedList<int, Point[]>();
			}

			/// <summary>
			/// This is how you get at my instance.  The act of calling this property guarantees that the static constructor gets called
			/// exactly once (per process?)
			/// </summary>
			public static PointsSingleton Instance
			{
				get
				{
					//	There is no need to check the static lock, because _instance is only set one time, and that is guaranteed to be
					//	finished before this function gets called
					return _instance;
				}
			}

			#endregion

			#region Public Methods

			public Point[] GetPoints(int numSides)
			{
				lock (_lockInstance)
				{
					if (!_points.ContainsKey(numSides))
					{
						//	NOTE: There is one more than what the passed in
						Point[] pointsPhi = new Point[numSides + 1];

						pointsPhi[0] = new Point(1d, 0d);		//	along the equator
						pointsPhi[numSides] = new Point(0d, 1d);		//	north pole

						if (pointsPhi.Length > 2)
						{
							//	Need to go from 0 to half pi
							double halfPi = Math.PI * .5d;
							double deltaPhi = halfPi / pointsPhi.Length;		//	there is one more point than numSegmentsPhi

							for (int cntr = 1; cntr < numSides; cntr++)
							{
								double phi = deltaPhi * cntr;		//	phi goes from 0 to pi for a full sphere, so start halfway up
								pointsPhi[cntr] = new Point(Math.Cos(phi), Math.Sin(phi));
							}
						}

						_points.Add(numSides, pointsPhi);
					}

					return _points[numSides];
				}
			}

			#endregion
		}

		#endregion

		#region Constructor

		public TubeRingDome(double distFromPrevRing, bool mergeNormalWithPrevIfSoft, int numSegmentsPhi)
			: base(distFromPrevRing, mergeNormalWithPrevIfSoft)
		{
			this.NumSegmentsPhi = numSegmentsPhi;
		}

		#endregion

		#region Public Properties

		/// <summary>
		/// This lets you fine tune how many vertical separations there are in the dome (usually just use the same number as horizontal segments)
		/// </summary>
		public int NumSegmentsPhi
		{
			get;
			private set;
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// This returns points for phi going from pi/2 to pi (a full circle goes from 0 to pi, but this class is only a dome)
		/// NOTE: The array will hold numSides + 1 elements (because 1 side requires 2 ends)
		/// </summary>
		public Point[] GetUnitPointsPhi(int numSides)
		{
			return PointsSingleton.Instance.GetPoints(numSides);
		}

		#endregion
	}

	#endregion
	#region Class: TubeRingBase

	public abstract class TubeRingBase
	{
		protected TubeRingBase(double distFromPrevRing, bool mergeNormalWithPrevIfSoft)
		{
			this.DistFromPrevRing = distFromPrevRing;
			this.MergeNormalWithPrevIfSoft = mergeNormalWithPrevIfSoft;
		}

		/// <summary>
		/// This is how far this ring is (in Z) from the previous ring in the tube.  The first ring in the tube ignores this property
		/// </summary>
		public double DistFromPrevRing
		{
			get;
			private set;
		}

		/// <summary>
		/// This is only looked at if doing soft sides
		/// True: When calculating normals, the prev ring is considered (this would make sense if the angle between the other ring and this one is low)
		/// False: The normals for this ring will be calculated independent of the prev ring (good for a traditional cone)
		/// </summary>
		/// <remarks>
		/// This only affects the normals at the bottom of the ring.  The top of the ring is defined by the next ring's property
		/// 
		/// This has no meaning for the very first ring
		/// 
		/// Examples:
		///	Cylinder - You would want false for both caps, because it's 90 degrees between the end cap and side
		///	Pyramid/Cone - You would want false, because it's greater than 90 degrees between the base cap and side
		///	Rings meant to look seamless - When the angle is low between two rings, that's when this should be true
		/// </remarks>
		public bool MergeNormalWithPrevIfSoft
		{
			get;
			private set;
		}
	}

	#endregion
}
