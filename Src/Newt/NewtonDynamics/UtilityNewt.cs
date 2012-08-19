using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Media3D;

namespace Game.Newt.NewtonDynamics
{
	public static class UtilityNewt
	{
		#region Enum: ObjectBreakdownType

		/// <summary>
		/// These objects are built along the same axiis that newton uses
		/// </summary>
		public enum ObjectBreakdownType
		{
			Box,
			Cylinder,
			//Cone,
			//Capsule,
		}

		#endregion
		#region Enum: MassDistribution

		public enum MassDistribution
		{
			/// <summary>
			/// Mass is spread evenly
			/// </summary>
			Uniform,
			///// <summary>
			///// All the mass is in the shell of the object (empty inside)
			///// </summary>
			//Shell,
			///// <summary>
			///// Mass goes from high in the center to 0 at the edges
			///// </summary>
			//LinearDown,
			///// <summary>
			///// Mass goes from 0 in the center to high at the edges
			///// </summary>
			//LinearUp,
		}

		#endregion
		#region Class: ObjectMassBreakdown

		public class ObjectMassBreakdown
		{
			#region Constructor

			public ObjectMassBreakdown(ObjectBreakdownType objectType, Vector3D objectSize, Point3D aabbMin, Point3D aabbMax, Point3D centerMass, double cellSize, int xLength, int yLength, int zLength, Point3D[] centers, double[] masses)
			{
				this.ObjectType = objectType;
				this.ObjectSize = objectSize;

				this.AABBMin = aabbMin;
				this.AABBMax = aabbMax;
				this.CenterMass = centerMass;
				this.CellSize = cellSize;

				this.XLength = xLength;
				this.YLength = yLength;
				this.ZLength = zLength;

				this.Centers = centers;
				this.Masses = masses;
			}

			#endregion

			public readonly ObjectBreakdownType ObjectType;
			public readonly Vector3D ObjectSize;

			//	The AABB size will always be a multiple of cell size (it will likely be a bit larger than ObjectSize - the object is centered inside of it)
			public readonly Point3D AABBMin;
			public readonly Point3D AABBMax;
			public readonly Point3D CenterMass;
			public readonly double CellSize;

			public readonly int XLength;
			public readonly int YLength;
			public readonly int ZLength;

			//public readonly Tuple<Point3D, double>[] Cells;		//	I don't want to take the expense of all the instances of tuple

			//	These are all the cells
			//NOTE: The arrays are readonly, but C# doesn't enforce the elements to be ready only.  Don't change them, that would be bad
			//NOTE: Multidimension arrays are slower than single dimension, so you'll have to do the math yourself (see this.GetIndex)
			public readonly Point3D[] Centers;
			public readonly double[] Masses;

			#region Private Methods

			/// <summary>
			/// This isn't meant to be used in production, just copy/paste
			/// </summary>
			public int GetIndex(int x, int y, int z)
			{
				//int zOffset = z * this.XLength * this.YLength;
				//int yOffset = y * this.XLength;
				//int xOffset = x;
				//return zOffset + yOffset + xOffset;

				return (z * this.XLength * this.YLength) + (y * this.XLength) + x;
			}

			#endregion
		}

		#endregion

		/// <summary>
		/// This divides an object up into cells, and tells the mass of each cell.  This is used to help calculate the mass matrix
		/// NOTE: The sum of the mass of the cells will always be 1
		/// </summary>
		/// <param name="size">This is the size of the box that the object sits in</param>
		/// <param name="cellSize">This is how big to make each cell.  The cells are always cubes</param>
		public static ObjectMassBreakdown GetMassBreakdown(ObjectBreakdownType objectType, MassDistribution distribution, Vector3D size, double cellSize)
		{
			//	Figure out how big to make the return (it will be an even multiple of the cell size)
			int xLen = Convert.ToInt32(Math.Ceiling(size.X / cellSize));
			int yLen = Convert.ToInt32(Math.Ceiling(size.Y / cellSize));
			int zLen = Convert.ToInt32(Math.Ceiling(size.Z / cellSize));

			Point3D aabbMax = new Point3D((cellSize * xLen) / 2d, (cellSize * yLen) / 2d, (cellSize * zLen) / 2d);
			Point3D aabbMin = new Point3D(-aabbMax.X, -aabbMax.Y, -aabbMax.Z);		//	it's centered at zero, so min is just max negated

			//	Instead of passing loose variables to the private methods, just fill out what I can of the return, and use it like an args class
			ObjectMassBreakdown args = new ObjectMassBreakdown(objectType, size, aabbMin, aabbMax, new Point3D(), cellSize, xLen, yLen, zLen, null, null);

			//TODO: This can be done in a separate task
			Point3D[] centers = GetCenters(args);

			Point3D centerMass;
			double[] masses;

			switch (objectType)
			{
				case ObjectBreakdownType.Box:
					#region Box

					centerMass = new Point3D(0, 0, 0);		//	for a box, the center of mass is the same as center of position

					if (xLen == 1 || yLen == 1 || zLen == 1)
					{
						GetMassBreakdownSprtBox2D(out masses, args);
					}
					else
					{
						GetMassBreakdownSprtBox3D(out masses, args);
					}

					#endregion
					break;

				case ObjectBreakdownType.Cylinder:
					#region Cylinder

					centerMass = new Point3D(0, 0, 0);		//	for a cylinder, the center of mass is the same as center of position

					GetMassBreakdownSprtCylinder(out masses, args);

					#endregion
					break;

				default:
					throw new ApplicationException("finish this");
			}

			//	Now make the total mass add up to 1
			NormalizeMasses(masses);

			//	Exit Function
			return new ObjectMassBreakdown(objectType, size, aabbMin, aabbMax, centerMass, cellSize, xLen, yLen, zLen, centers, masses);
		}

		#region Private Methods

		private static void GetMassBreakdownSprtBox2D(out double[] masses, ObjectMassBreakdown e)
		{
			masses = new double[e.ZLength * e.YLength * e.XLength];

			double halfSize = e.CellSize * .5d;
			Vector3D halfObjSize = new Vector3D(e.ObjectSize.X * .5d, e.ObjectSize.Y * .5d, e.ObjectSize.Z * .5d);
			double wholeVolume = e.CellSize * e.CellSize * e.CellSize;

			for (int z = 0; z < e.ZLength; z++)
			{
				int zOffset = z * e.XLength * e.YLength;

				for (int y = 0; y < e.YLength; y++)
				{
					int yOffset = y * e.XLength;

					for (int x = 0; x < e.XLength; x++)
					{
						//	Calculate the positions of the 4 corners
						Point3D min = new Point3D(e.AABBMin.X + (x * e.CellSize), e.AABBMin.Y + (y * e.CellSize), e.AABBMin.Z + (z * e.CellSize));
						Point3D max = new Point3D(min.X + e.CellSize, min.Y + e.CellSize, min.Z + e.CellSize);

						//	If the the cell is completly inside the object, then give it a mass of one.  Otherwise some percent of that

						double xAmt = GetMassBreakdownSprtBox2DSprtAxis(Math.Abs(min.X), Math.Abs(max.X), e.ObjectSize.X, halfObjSize.X, e.CellSize);
						double yAmt = GetMassBreakdownSprtBox2DSprtAxis(Math.Abs(min.Y), Math.Abs(max.Y), e.ObjectSize.Y, halfObjSize.Y, e.CellSize);
						double zAmt = GetMassBreakdownSprtBox2DSprtAxis(Math.Abs(min.Z), Math.Abs(max.Z), e.ObjectSize.Z, halfObjSize.Z, e.CellSize);

						double subVolume = xAmt * yAmt * zAmt;
						double percent = subVolume / wholeVolume;

						masses[e.GetIndex(x, y, z)] = percent;
					}
				}
			}

			if (masses.Any(o => o == 0d))
			{
				throw new ApplicationException("This shouldn't happen");
			}
		}
		private static double GetMassBreakdownSprtBox2DSprtAxis(double absMin, double absMax, double size, double halfSize, double cellSize)
		{
			double min = absMin;
			double max = absMax;
			if (absMin > absMax)
			{
				min = absMax;
				max = absMin;
			}

			if (min > halfSize && max > halfSize)
			{
				//	This cell is larger than the object
				return size;
			}
			else if (min > halfSize)
			{
				return 0d;		//	this should never happen
			}
			else if (max > halfSize)
			{
				return halfSize - min;
			}
			else
			{
				return cellSize;
			}
		}

		private static void GetMassBreakdownSprtBox3D(out double[] masses, ObjectMassBreakdown e)
		{
			masses = new double[e.ZLength * e.YLength * e.XLength];

			double halfSize = e.CellSize * .5d;
			Vector3D halfObjSize = new Vector3D(e.ObjectSize.X * .5d, e.ObjectSize.Y * .5d, e.ObjectSize.Z * .5d);
			double wholeVolume = e.CellSize * e.CellSize * e.CellSize;

			//NOTE: With a box, there will never be any completely empty cells.  The outermost cells will either be partial or full.  Everything one layer in will always be full

			#region Corners

			if (e.XLength > 1 && e.YLength > 1 && e.ZLength > 1)
			{
				Point3D min = new Point3D(e.AABBMin.X + ((e.XLength - 1) * e.CellSize), e.AABBMin.Y + ((e.YLength - 1) * e.CellSize), e.AABBMin.Z + ((e.ZLength - 1) * e.CellSize));

				double subVolume = (halfObjSize.X - min.X) * (halfObjSize.Y - min.Y) * (halfObjSize.Z - min.Z);
				double percent = subVolume / wholeVolume;

				masses[e.GetIndex(0, 0, 0)] = percent;
				masses[e.GetIndex(e.XLength - 1, 0, 0)] = percent;
				masses[e.GetIndex(0, e.YLength - 1, 0)] = percent;
				masses[e.GetIndex(e.XLength - 1, e.YLength - 1, 0)] = percent;

				masses[e.GetIndex(0, 0, e.ZLength - 1)] = percent;
				masses[e.GetIndex(e.XLength - 1, 0, e.ZLength - 1)] = percent;
				masses[e.GetIndex(0, e.YLength - 1, e.ZLength - 1)] = percent;
				masses[e.GetIndex(e.XLength - 1, e.YLength - 1, e.ZLength - 1)] = percent;
			}

			#endregion

			#region Edges

			//	Along X, +-Y, +-Z
			if (e.XLength > 2 && e.YLength > 1 && e.ZLength > 1)
			{
				Point3D min = new Point3D(e.AABBMin.X + ((e.XLength - 2) * e.CellSize), e.AABBMin.Y + ((e.YLength - 1) * e.CellSize), e.AABBMin.Z + ((e.ZLength - 1) * e.CellSize));

				double subVolume = e.CellSize * (halfObjSize.Y - min.Y) * (halfObjSize.Z - min.Z);
				double percent = subVolume / wholeVolume;

				for (int x = 1; x < e.XLength - 1; x++)
				{
					masses[e.GetIndex(x, 0, 0)] = percent;
					masses[e.GetIndex(x, e.YLength - 1, 0)] = percent;

					masses[e.GetIndex(x, 0, e.ZLength - 1)] = percent;
					masses[e.GetIndex(x, e.YLength - 1, e.ZLength - 1)] = percent;
				}
			}

			//	Along Y, +-X, +-Z
			if (e.YLength > 2 && e.XLength > 1 && e.ZLength > 1)
			{
				Point3D min = new Point3D(e.AABBMin.X + ((e.XLength - 1) * e.CellSize), e.AABBMin.Y + ((e.YLength - 2) * e.CellSize), e.AABBMin.Z + ((e.ZLength - 1) * e.CellSize));

				double subVolume = (halfObjSize.X - min.X) * e.CellSize * (halfObjSize.Z - min.Z);
				double percent = subVolume / wholeVolume;

				for (int y = 1; y < e.YLength - 1; y++)
				{
					masses[e.GetIndex(0, y, 0)] = percent;
					masses[e.GetIndex(e.XLength - 1, y, 0)] = percent;

					masses[e.GetIndex(0, y, e.ZLength - 1)] = percent;
					masses[e.GetIndex(e.XLength - 1, y, e.ZLength - 1)] = percent;
				}
			}

			//	Along Z, +-X, +-Y
			if (e.ZLength > 2 && e.XLength > 1 && e.YLength > 1)
			{
				Point3D min = new Point3D(e.AABBMin.X + ((e.XLength - 1) * e.CellSize), e.AABBMin.Y + ((e.YLength - 1) * e.CellSize), e.AABBMin.Z + ((e.ZLength - 2) * e.CellSize));

				double subVolume = (halfObjSize.X - min.X) * (halfObjSize.Y - min.Y) * e.CellSize;
				double percent = subVolume / wholeVolume;

				for (int z = 1; z < e.ZLength - 1; z++)
				{
					masses[e.GetIndex(0, 0, z)] = percent;
					masses[e.GetIndex(e.XLength - 1, 0, z)] = percent;

					masses[e.GetIndex(0, e.YLength - 1, z)] = percent;
					masses[e.GetIndex(e.XLength - 1, e.YLength - 1, z)] = percent;
				}
			}

			#endregion

			#region Faces

			//	+-X
			if (e.XLength > 1 && e.YLength > 2 && e.ZLength > 2)
			{
				Point3D min = new Point3D(e.AABBMin.X + ((e.XLength - 1) * e.CellSize), e.AABBMin.Y + ((e.YLength - 2) * e.CellSize), e.AABBMin.Z + ((e.ZLength - 2) * e.CellSize));

				double subVolume = (halfObjSize.X - min.X) * e.CellSize * e.CellSize;
				double percent = subVolume / wholeVolume;

				for (int y = 1; y < e.YLength - 1; y++)
				{
					for (int z = 1; z < e.ZLength - 1; z++)
					{
						masses[e.GetIndex(e.XLength - 1, y, z)] = percent;
						masses[e.GetIndex(0, y, z)] = percent;
					}
				}
			}

			//	+-Y
			if (e.YLength > 1 && e.XLength > 2 && e.ZLength > 2)
			{
				Point3D min = new Point3D(e.AABBMin.X + ((e.XLength - 2) * e.CellSize), e.AABBMin.Y + ((e.YLength - 1) * e.CellSize), e.AABBMin.Z + ((e.ZLength - 2) * e.CellSize));

				double subVolume = e.CellSize * (halfObjSize.Y - min.Y) * e.CellSize;
				double percent = subVolume / wholeVolume;

				for (int x = 1; x < e.XLength - 1; x++)
				{
					for (int z = 1; z < e.ZLength - 1; z++)
					{
						masses[e.GetIndex(x, e.YLength - 1, z)] = percent;
						masses[e.GetIndex(x, 0, z)] = percent;
					}
				}
			}

			//	+-Z
			if (e.ZLength > 1 && e.XLength > 2 && e.YLength > 2)
			{
				Point3D min = new Point3D(e.AABBMin.X + ((e.XLength - 2) * e.CellSize), e.AABBMin.Y + ((e.YLength - 2) * e.CellSize), e.AABBMin.Z + ((e.ZLength - 1) * e.CellSize));

				double subVolume = e.CellSize * e.CellSize * (halfObjSize.Z - min.Z);
				double percent = subVolume / wholeVolume;

				for (int x = 1; x < e.XLength - 1; x++)
				{
					for (int y = 1; y < e.YLength - 1; y++)
					{
						masses[e.GetIndex(x, y, e.ZLength - 1)] = percent;
						masses[e.GetIndex(x, y, 0)] = percent;
					}
				}
			}

			#endregion

			#region Inside

			for (int z = 1; z < e.ZLength - 1; z++)
			{
				int zOffset = z * e.XLength * e.YLength;

				for (int y = 1; y < e.YLength - 1; y++)
				{
					int yOffset = y * e.XLength;

					for (int x = 1; x < e.XLength - 1; x++)
					{
						masses[zOffset + yOffset + x] = 1d;
					}
				}
			}

			#endregion

			if (masses.Any(o => o == 0d))
			{
				throw new ApplicationException("This shouldn't happen");
			}
		}

		private static void GetMassBreakdownSprtCylinder(out double[] masses, ObjectMassBreakdown e)
		{
			masses = new double[e.ZLength * e.YLength * e.XLength];

			double halfSize = e.CellSize * .5d;
			Vector3D halfObjSize = new Vector3D(e.ObjectSize.X * .5d, e.ObjectSize.Y * .5d, e.ObjectSize.Z * .5d);
			double wholeVolume = e.CellSize * e.CellSize * e.CellSize;

			//	The cylinder's axis is along x
			//	Only go through the quadrant with positive values.  Then copy the results to the other three quadrants

			int zStart = e.ZLength / 2;
			if (zStart % 2 == 1)
			{
				zStart++;
			}

			int yStart = e.YLength / 2;
			if (yStart % 2 == 1)
			{
				yStart++;
			}

			//	These tell how to convert the ellipse into a circle
			double maxRadius = Math.Max(e.ObjectSize.Y, e.ObjectSize.Z);
			double ratioY = maxRadius / e.ObjectSize.Y;
			double ratioZ = maxRadius / e.ObjectSize.Z;

			for (int z = zStart; z < e.ZLength; z++)
			{
				int zOffset = z * e.XLength * e.YLength;

				for (int y = yStart; y < e.YLength; y++)
				{
					int yOffset = y * e.XLength;

					#region Area of Y,Z tile

					//	Figure out how much of the circle is inside this square (just 2D for now)
					Vector3D min = new Vector3D(0, (e.AABBMin.Y + (y * e.CellSize)) * ratioY, (e.AABBMin.Z + (z * e.CellSize)) * ratioZ);
					Vector3D max = new Vector3D(0, (e.AABBMin.Y + ((y + 1) * e.CellSize)) * ratioY, (e.AABBMin.Z + ((z + 1) * e.CellSize)) * ratioZ);

					double mass = 0d;

					double maxLength = max.Length;
					if (maxLength < maxRadius)
					{
						//	This tile is completely inside the circle
						mass = e.CellSize * e.CellSize;		//	just the 2D mass
					}
					else
					{
						double minLength = min.Length;
						if (minLength > maxRadius)
						{
							//	This tile is completely outside the circle
							mass = 0d;
						}
						else
						{
							#region Intersect Circle

							//	This tile is clipped by the circle.  Figure out which edges the circle is intersecting
							Vector3D minPlusY = new Vector3D(0, min.Y + (e.CellSize * ratioY), min.Z);
							Vector3D minPlusZ = new Vector3D(0, min.Y, min.Z + (e.CellSize * ratioZ));

							//	When calculating the area, it will either be a triangle, trapazoid or diamond
							List<double> lengths = new List<double>();

							double percent;

							if (minPlusY.Length > maxRadius)
							{
								//	The intercept is along the line from min to min+Y
								percent = GetMassBreakdownSprtCylinderSprtIntersectPercent(min, minPlusY, maxRadius);
								lengths.Add(e.CellSize * percent);
							}
							else
							{
								//	The intercept is along the line from min+Y to max
								lengths.Add(e.CellSize);
								percent = GetMassBreakdownSprtCylinderSprtIntersectPercent(minPlusY, max, maxRadius);
								lengths.Add(e.CellSize * percent);
							}

							if (minPlusZ.Length > maxRadius)
							{
								//	The intercept is along the line from min to min+Z
								percent = GetMassBreakdownSprtCylinderSprtIntersectPercent(min, minPlusZ, maxRadius);
								lengths.Add(e.CellSize * percent);
							}
							else
							{
								//	The intercept is along the line from min+Z to max
								lengths.Add(e.CellSize);
								percent = GetMassBreakdownSprtCylinderSprtIntersectPercent(minPlusZ, max, maxRadius);
								lengths.Add(e.CellSize * percent);
							}

							//	Calculate the area (just assuming the circle portion is a straight line)
							switch (lengths.Count)
							{
								case 2:
									//	Triangle (1/2 * base * height)
									mass = lengths[0] * lengths[1] * .5d;
									break;

								case 3:
									//	Trapazoid
									lengths.Sort();
									mass = lengths[0] * lengths[2];		//	get the area of the shortest side x base
									mass += lengths[2] * (lengths[1] - lengths[0]) * .5d;		// tack on the area of the triangle above that rectangle
									break;

								case 4:
									//	Diamond
									lengths.Sort();
									mass = lengths[3] * lengths[1];		//	get the area of the "horizontal" rectangle
									mass += (lengths[2] - lengths[1]) * lengths[0];		//	add the area of the "vertical" rectangle
									mass += (lengths[3] - lengths[0]) * (lengths[2] - lengths[1]) * .5d;
									break;

								default:
									throw new ApplicationException("Unexpected number of segments: " + lengths.Count.ToString());
							}

							#endregion
						}
					}

					#endregion

					//TODO: Set the mass in the other 3 quadrants

					#region Set mass

					//	The end caps get partial height along x
					double xMin = e.AABBMin.X + ((e.XLength - 1) * e.CellSize);

					double volume = mass * (halfObjSize.X - xMin);
					masses[e.GetIndex(0, y, z)] = volume;
					masses[e.GetIndex(e.XLength - 1, y, z)] = volume;

					//	Everything inside gets the full height
					volume = mass * e.CellSize;
					for (int x = 1; x < e.XLength - 1; x++)
					{
						masses[e.GetIndex(x, y, z)] = volume;
					}

					#endregion
				}
			}
		}

		private static void GetMassBreakdownSprtTemplate(out Point3D centerMass, out Point3D[] centers, out double[] masses, ObjectMassBreakdown e)
		{
			centerMass = new Point3D(0, 0, 0);
			centers = new Point3D[e.ZLength * e.YLength * e.XLength];
			masses = new double[centers.Length];

			double halfSize = e.CellSize * .5d;
			Vector3D halfObjSize = new Vector3D(e.ObjectSize.X * .5d, e.ObjectSize.Y * .5d, e.ObjectSize.Z * .5d);

			for (int z = 0; z < e.ZLength; z++)
			{
				int zOffset = z * e.XLength * e.YLength;

				for (int y = 0; y < e.YLength; y++)
				{
					int yOffset = y * e.XLength;

					for (int x = 0; x < e.XLength; x++)
					{
						//	Calculate the positions of the 4 corners
						Point3D min = new Point3D(e.AABBMin.X + (x * e.CellSize), e.AABBMin.Y + (y * e.CellSize), e.AABBMin.Z + (z * e.CellSize));
						Point3D max = new Point3D(min.X + e.CellSize, min.Y + e.CellSize, min.Z + e.CellSize);
						centers[zOffset + yOffset + x] = new Point3D(min.X + halfSize, min.Y + halfSize, min.Z + halfSize);

						//	If the the cell is completly inside the object, then give it a mass of one.  Otherwise some percent of that





					}
				}
			}
		}

		private static double GetMassBreakdownSprtCylinderSprtIntersectPercent(Vector3D lineStart, Vector3D lineStop, double radius)
		{
			Point start2D = new Point(lineStart.Y, lineStart.Z);
			Point stop2D = new Point(lineStop.Y, lineStop.Z);

			double? retVal = GetLineCircleIntersectPercent(start2D, stop2D - start2D, new Point(0, 0), radius);
			if (retVal == null)
			{
				throw new ApplicationException("Shouldn't get null");
			}

			return retVal.Value;
		}

		//private static double GetLineCircleIntersectPercent(Point lineStart, Point lineStop, Point circleCenter, double radius)
		//{
		//    Vector direction = lineStop - lineStart;

		//    Point intersect = GetLineCircleIntersect(lineStart, direction, circleCenter, radius);

		//    double intersectLen = (intersect - lineStart).Length;
		//    return intersectLen / direction.Length;
		//}
		/// <summary>
		/// Got this here:
		/// http://stackoverflow.com/questions/1073336/circle-line-collision-detection
		/// </summary>
		private static double? GetLineCircleIntersectPercent(Point lineStart, Vector lineDir, Point circleCenter, double radius)
		{
			Point C = circleCenter;
			double r = radius;
			Point E = lineStart;
			Vector d = lineDir;
			Vector f = E - C;

			Vector3D d3D = new Vector3D(d.X, d.Y, 0);
			Vector3D f3D = new Vector3D(f.X, f.Y, 0);

			double a = Vector3D.DotProduct(d3D, d3D);
			double b = 2d * Vector3D.DotProduct(f3D, d3D);
			double c = Vector3D.DotProduct(f3D, f3D) - (r * r);

			double discriminant = (b * b) - (4 * a * c);
			if (discriminant < 0d)
			{
				// no intersection
				return null;
			}
			else
			{
				// ray didn't totally miss circle, so there is a solution to the equation.

				discriminant = Math.Sqrt(discriminant);

				// either solution may be on or off the ray so need to test both
				double t1 = (-b + discriminant) / (2d * a);
				double t2 = (-b - discriminant) / (2d * a);

				if (t1 >= 0d && t1 <= 1d)
				{
					// t1 solution on is ON THE RAY.
					return t1;
				}
				else
				{
					// t1 solution "out of range" of ray
					//return null;
				}

				if (t2 >= 0d && t2 <= 1d)
				{
					// t2 solution on is ON THE RAY.
					return t2;
				}
				else
				{
					// t2 solution "out of range" of ray
				}
			}

			return null;


		}

		private static Point3D[] GetCenters(ObjectMassBreakdown e)
		{
			Point3D[] retVal = new Point3D[e.ZLength * e.YLength * e.XLength];

			double halfSize = e.CellSize * .5d;

			for (int z = 0; z < e.ZLength; z++)
			{
				int zOffset = z * e.XLength * e.YLength;

				for (int y = 0; y < e.YLength; y++)
				{
					int yOffset = y * e.XLength;

					for (int x = 0; x < e.XLength; x++)
					{
						//	Calculate the positions of the 4 corners
						//Point3D min = new Point3D(e.AABBMin.X + (x * e.CellSize), e.AABBMin.Y + (y * e.CellSize), e.AABBMin.Z + (z * e.CellSize));
						//Point3D max = new Point3D(min.X + e.CellSize, min.Y + e.CellSize, min.Z + e.CellSize);

						//	This is halfway between min and max
						retVal[zOffset + yOffset + x] = new Point3D(
							e.AABBMin.X + (x * e.CellSize) + halfSize,
							e.AABBMin.Y + (y * e.CellSize) + halfSize,
							e.AABBMin.Z + (z * e.CellSize) + halfSize);
					}
				}
			}

			return retVal;
		}

		/// <summary>
		/// Once this method completes, the sum of the mass in the array will be 1
		/// </summary>
		private static void NormalizeMasses(double[] masses)
		{
			double total = masses.Sum();
			if (total == 0d)
			{
				return;
			}

			double mult = 1d / total;		//	multiplication is cheaper than division, so just do the division once

			for (int cntr = 0; cntr < masses.Length; cntr++)
			{
				masses[cntr] *= mult;
			}
		}

		#endregion
	}
}
