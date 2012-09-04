using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Media3D;

using Game.Newt.HelperClasses;

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
			Dome,		//	this is half a sphere
			//Cone,
			Combined
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

		public class ObjectMassBreakdown : IObjectMassBreakdown
		{
			#region Constructor

			public ObjectMassBreakdown(ObjectBreakdownType objectType, Vector3D objectSize, Point3D aabbMin, Point3D aabbMax, Point3D centerMass, double cellSize, int xLength, int yLength, int zLength, Point3D[] centers, double[] masses)
			{
				this.ObjectType = objectType;
				this.ObjectSize = objectSize;

				this.AABBMin = aabbMin;
				this.AABBMax = aabbMax;
				_centerMass = centerMass;
				this.CellSize = cellSize;

				this.XLength = xLength;
				this.YLength = yLength;
				this.ZLength = zLength;

				this.Centers = centers;
				this.Masses = masses;
			}

			#endregion

			#region IObjectMassBreakdown Members

			public Point3D CenterMass
			{
				get
				{
					return _centerMass;
				}
			}

			public IEnumerator<Tuple<Point3D, double>> GetEnumerator()
			{
				for (int cntr = 0; cntr < Masses.Length; cntr++)
				{
					yield return new Tuple<Point3D, double>(this.Centers[cntr], this.Masses[cntr]);
				}
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				//TODO: Figure out if this can call the other GetEnumerator
				for (int cntr = 0; cntr < Masses.Length; cntr++)
				{
					yield return new Tuple<Point3D, double>(this.Centers[cntr], this.Masses[cntr]);
				}
			}

			#endregion

			public readonly ObjectBreakdownType ObjectType;
			public readonly Vector3D ObjectSize;

			//	The AABB size will always be a multiple of cell size (it will likely be a bit larger than ObjectSize - the object is centered inside of it)
			public readonly Point3D AABBMin;
			public readonly Point3D AABBMax;
			private readonly Point3D _centerMass;
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
		#region Class: ObjectMassBreakdownSet

		public class ObjectMassBreakdownSet : IObjectMassBreakdown
		{
			#region Constructor

			public ObjectMassBreakdownSet(ObjectMassBreakdown[] objects, Transform3D[] transforms)
			{
				if (objects == null || transforms == null || objects.Length != transforms.Length)
				{
					throw new ArgumentException("The arrays passed in are null or different sizes");
				}

				this.Objects = objects;
				this.Transforms = transforms;

				//	Transform the centers (doing this once in the constructor so consumers can just use the values)
				this.TransformedCenters = new Point3D[objects.Length][];
				for (int cntr = 0; cntr < objects.Length; cntr++)
				{
					this.TransformedCenters[cntr] = objects[cntr].Centers.Select(o => transforms[cntr].Transform(o)).ToArray();
				}

				_centerMass = GetCenterOfMass(objects, this.TransformedCenters);
			}

			#endregion

			#region IObjectMassBreakdown Members

			public Point3D CenterMass
			{
				get
				{
					return _centerMass;
				}
			}

			public IEnumerator<Tuple<Point3D, double>> GetEnumerator()
			{
				for (int outerCntr = 0; outerCntr < this.Objects.Length; outerCntr++)
				{
					for (int innerCntr = 0; innerCntr < this.Objects[outerCntr].Masses.Length; innerCntr++)
					{
						//NOTE: Returning the transformed center
						yield return new Tuple<Point3D, double>(this.TransformedCenters[outerCntr][innerCntr], this.Objects[outerCntr].Masses[innerCntr]);
					}
				}
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				for (int outerCntr = 0; outerCntr < this.Objects.Length; outerCntr++)
				{
					for (int innerCntr = 0; innerCntr < this.Objects[outerCntr].Masses.Length; innerCntr++)
					{
						//NOTE: Returning the transformed center
						yield return new Tuple<Point3D, double>(this.TransformedCenters[outerCntr][innerCntr], this.Objects[outerCntr].Masses[innerCntr]);
					}
				}
			}

			#endregion

			private readonly Point3D _centerMass;

			//	These arrays are all the same size
			public ObjectMassBreakdown[] Objects;
			public Transform3D[] Transforms;
			public readonly Point3D[][] TransformedCenters;

			#region Private Methods

			/// <summary>
			/// Sum(m*r)/M
			/// </summary>
			private static Point3D GetCenterOfMass(ObjectMassBreakdown[] objects, Point3D[][] positions)
			{
				Point3D retVal = new Point3D(0, 0, 0);

				double totalMass = 0;

				for (int outerCntr = 0; outerCntr < objects.Length; outerCntr++)
				{
					for (int innerCntr = 0; innerCntr < objects[outerCntr].Masses.Length; innerCntr++)
					{
						retVal.X += positions[outerCntr][innerCntr].X * objects[outerCntr].Masses[innerCntr];
						retVal.Y += positions[outerCntr][innerCntr].Y * objects[outerCntr].Masses[innerCntr];
						retVal.Z += positions[outerCntr][innerCntr].Z * objects[outerCntr].Masses[innerCntr];
						totalMass += objects[outerCntr].Masses[innerCntr];
					}
				}

				retVal.X /= totalMass;
				retVal.Y /= totalMass;
				retVal.Z /= totalMass;

				//	Exit Function
				return retVal;
			}

			#endregion
		}

		#endregion
		#region Interface: IObjectMassBreakdown

		public interface IObjectMassBreakdown : IEnumerable<Tuple<Point3D, double>>
		{
			/// <summary>
			/// The center of position is always 0
			/// </summary>
			Point3D CenterMass
			{
				get;
			}
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
			//	Figure out the dimensions of the return
			Tuple<int, int, int> lengths;
			Tuple<Point3D, Point3D> aabb;
			CalculateReturnSize(out lengths, out aabb, size, cellSize);

			//	Instead of passing loose variables to the private methods, just fill out what I can of the return, and use it like an args class
			ObjectMassBreakdown args = new ObjectMassBreakdown(objectType, size, aabb.Item1, aabb.Item2, new Point3D(), cellSize, lengths.Item1, lengths.Item2, lengths.Item3, null, null);

			Point3D[] centers = GetCenters(args);

			Point3D centerMass;
			double[] masses;

			switch (objectType)
			{
				case ObjectBreakdownType.Box:
					#region Box

					centerMass = new Point3D(0, 0, 0);		//	for a box, the center of mass is the same as center of position

					if (lengths.Item1 == 1 || lengths.Item2 == 1 || lengths.Item3 == 1)
					{
						masses = GetMassBreakdownSprtBox2D(args);
					}
					else
					{
						masses = GetMassBreakdownSprtBox3D(args);
					}

					#endregion
					break;

				case ObjectBreakdownType.Cylinder:
					#region Cylinder

					centerMass = new Point3D(0, 0, 0);		//	for a cylinder, the center of mass is the same as center of position

					masses = GetMassBreakdownSprtCylinder(args);

					#endregion
					break;

				default:
					throw new ApplicationException("finish this");
			}

			//	Now make the total mass add up to 1
			Normalize(masses);

			//	Exit Function
			return new ObjectMassBreakdown(objectType, size, aabb.Item1, aabb.Item2, centerMass, cellSize, lengths.Item1, lengths.Item2, lengths.Item3, centers, masses);
		}

		/// <summary>
		/// This creates a set of breakdowns, which lets the user create more complex shapes (a capsule would be 2 domes and a cylinder,
		/// but the dome would only have to be calculated once)
		/// NOTE: The sum of the mass of the cells will always be 1
		/// NOTE: Be careful of overlapping, the mass will double up
		/// </summary>
		/// <param name="objects">
		/// ObjectMassBreakdown: The object to add to the set.
		/// Point3D: The offset.
		/// Quaternion: How this object should be rotated.
		/// double: The amount to multiply this object's mass.  Say object one has a mass of 10, and object two has a mass of 25.  The mass of the set will be 1, but object two will be 2.5 times greater than one.
		/// </param>
		public static ObjectMassBreakdownSet Combine(Tuple<ObjectMassBreakdown, Point3D, Quaternion, double>[] objects)
		{
			//	Make the mass of all the objects add to one (the ratios are preserved, just the sum of masses changes)
			ObjectMassBreakdown[] normalized = Normalize(objects);

			//	Build transforms
			Transform3D[] transforms = new Transform3D[objects.Length];
			for (int cntr = 0; cntr < objects.Length; cntr++)
			{
				Transform3DGroup transform = new Transform3DGroup();
				transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(objects[cntr].Item3)));
				transform.Children.Add(new TranslateTransform3D(objects[cntr].Item2.ToVector()));

				transforms[cntr] = transform;
			}

			//	Exit Function
			return new ObjectMassBreakdownSet(normalized, transforms);
		}

		#region Private Methods

		private static double[] GetMassBreakdownSprtBox2D(ObjectMassBreakdown e)
		{
			double[] retVal = new double[e.ZLength * e.YLength * e.XLength];

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

						retVal[e.GetIndex(x, y, z)] = percent;
					}
				}
			}

			if (retVal.Any(o => o == 0d))
			{
				throw new ApplicationException("This shouldn't happen");
			}

			return retVal;
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

		private static double[] GetMassBreakdownSprtBox3D(ObjectMassBreakdown e)
		{
			double[] retVal = new double[e.ZLength * e.YLength * e.XLength];

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

				retVal[e.GetIndex(0, 0, 0)] = percent;
				retVal[e.GetIndex(e.XLength - 1, 0, 0)] = percent;
				retVal[e.GetIndex(0, e.YLength - 1, 0)] = percent;
				retVal[e.GetIndex(e.XLength - 1, e.YLength - 1, 0)] = percent;

				retVal[e.GetIndex(0, 0, e.ZLength - 1)] = percent;
				retVal[e.GetIndex(e.XLength - 1, 0, e.ZLength - 1)] = percent;
				retVal[e.GetIndex(0, e.YLength - 1, e.ZLength - 1)] = percent;
				retVal[e.GetIndex(e.XLength - 1, e.YLength - 1, e.ZLength - 1)] = percent;
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
					retVal[e.GetIndex(x, 0, 0)] = percent;
					retVal[e.GetIndex(x, e.YLength - 1, 0)] = percent;

					retVal[e.GetIndex(x, 0, e.ZLength - 1)] = percent;
					retVal[e.GetIndex(x, e.YLength - 1, e.ZLength - 1)] = percent;
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
					retVal[e.GetIndex(0, y, 0)] = percent;
					retVal[e.GetIndex(e.XLength - 1, y, 0)] = percent;

					retVal[e.GetIndex(0, y, e.ZLength - 1)] = percent;
					retVal[e.GetIndex(e.XLength - 1, y, e.ZLength - 1)] = percent;
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
					retVal[e.GetIndex(0, 0, z)] = percent;
					retVal[e.GetIndex(e.XLength - 1, 0, z)] = percent;

					retVal[e.GetIndex(0, e.YLength - 1, z)] = percent;
					retVal[e.GetIndex(e.XLength - 1, e.YLength - 1, z)] = percent;
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
						retVal[e.GetIndex(e.XLength - 1, y, z)] = percent;
						retVal[e.GetIndex(0, y, z)] = percent;
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
						retVal[e.GetIndex(x, e.YLength - 1, z)] = percent;
						retVal[e.GetIndex(x, 0, z)] = percent;
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
						retVal[e.GetIndex(x, y, e.ZLength - 1)] = percent;
						retVal[e.GetIndex(x, y, 0)] = percent;
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
						retVal[zOffset + yOffset + x] = 1d;
					}
				}
			}

			#endregion

			if (retVal.Any(o => o == 0d))
			{
				throw new ApplicationException("This shouldn't happen");
			}

			return retVal;
		}

		private static double[] GetMassBreakdownSprtCylinder(ObjectMassBreakdown e)
		{
			double[] retVal = new double[e.ZLength * e.YLength * e.XLength];

			#region Pre calculations

			double halfSize = e.CellSize * .5d;
			Vector3D halfObjSize = new Vector3D(e.ObjectSize.X * .5d, e.ObjectSize.Y * .5d, e.ObjectSize.Z * .5d);
			double wholeVolume = e.CellSize * e.CellSize * e.CellSize;

			//	The cylinder's axis is along x
			//	Only go through the quadrant with positive values.  Then copy the results to the other three quadrants

			bool isYEven = true;
			int yStart = e.YLength / 2;
			if (e.YLength % 2 == 1)
			{
				yStart++;
				isYEven = false;
			}

			bool isZEven = true;
			int zStart = e.ZLength / 2;
			if (e.ZLength % 2 == 1)
			{
				zStart++;
				isZEven = false;
			}

			//	These tell how to convert the ellipse into a circle
			double objectSizeHalfY = e.ObjectSize.Y * .5d;
			double objectSizeHalfZ = e.ObjectSize.Z * .5d;
			double maxRadius = Math.Max(objectSizeHalfY, objectSizeHalfZ);
			double ratioY = maxRadius / objectSizeHalfY;
			double ratioZ = maxRadius / objectSizeHalfZ;

			#endregion

			#region Y,Z axis tiles

			if (!isYEven)
			{
				GetMassBreakdownSprtCylinderSprtYZero(retVal, yStart - 1, zStart, isZEven, ratioZ, ratioZ, halfObjSize, maxRadius, e);
			}

			if (!isZEven)
			{
				GetMassBreakdownSprtCylinderSprtZZero(retVal, yStart, zStart - 1, isYEven, ratioY, ratioZ, halfObjSize, maxRadius, e);
			}

			if (!isYEven && !isZEven)
			{
				GetMassBreakdownSprtCylinderSprtYZZero(retVal, yStart - 1, zStart - 1, ratioY, ratioZ, halfObjSize, maxRadius, e);
			}

			#endregion

			//	Quadrant tiles
			GetMassBreakdownSprtCylinderSprtQuadrant(retVal, yStart, zStart, isYEven, isZEven, ratioY, ratioZ, halfObjSize, maxRadius, e);

			//	Exit Function
			return retVal;
		}
		private static void GetMassBreakdownSprtCylinderSprtYZero(double[] masses, int y, int zStart, bool isZEven, double ratioY, double ratioZ, Vector3D halfObjSize, double maxRadius, ObjectMassBreakdown e)
		{
			for (int z = zStart; z < e.ZLength; z++)
			{
				int altZ = zStart - (z + (isZEven ? 1 : 2) - zStart);

				#region Area of Y,Z tile

				//	Figure out how much of the circle is inside this square (just 2D for now)
				Vector3D min = new Vector3D(0, e.CellSize * .5d * ratioY, (e.AABBMin.Z + (z * e.CellSize)) * ratioZ);		//NOTE: min has positive y as well as max.  This just makes some of the calculations easier
				Vector3D max = new Vector3D(0, min.Y, (e.AABBMin.Z + ((z + 1) * e.CellSize)) * ratioZ);

				double mass = 0d;

				double maxLength = max.Length;
				if (maxLength < maxRadius)
				{
					//	This tile is completely inside the circle
					mass = e.CellSize * e.CellSize;		//	just the 2D mass
				}
				else if (min.Z < maxRadius)		//	the bottom edge should never be greater than the circle, but just making sure
				{
					#region Intersect Circle

					List<double> lengths = new List<double>();
					double percent;
					bool isTriangle = false;

					#region Y

					double minLength = min.Length;
					if (minLength < maxRadius)
					{
						//	Circle intersects the line from min.Z,Y to max.Z,Y
						lengths.Add(e.CellSize);		//	add one for the bottom segment

						//	Add one for the y intercept
						percent = GetMassBreakdownSprtCylinderSprtIntersectPercent(new Vector3D(0d, min.Y, min.Z), new Vector3D(0d, max.Y, max.Z), maxRadius);
						lengths.Add(e.CellSize * percent);
						lengths.Add(e.CellSize * percent);
					}
					else
					{
						//TODO: Test this, execution has never gotten here

						//	Circle never goes as far as min/max.Y, find the intersect along the min z line
						//NOTE: There are 2 intersects, so define min z as 0
						percent = GetMassBreakdownSprtCylinderSprtIntersectPercent(new Vector3D(0d, 0d, min.Z), new Vector3D(0d, min.Y, min.Z), maxRadius);		//	min.Y is positive
						lengths.Add(e.CellSize * percent * 2d);		// percent is from 0 to Y, so double it to get -Y to Y
					}

					#endregion
					#region Z

					if (max.Z >= maxRadius)
					{
						//	The circle doesn't go all the way to the right edge, so assume a triangle
						percent = GetMassBreakdownSprtCylinderSprtIntersectPercent(new Vector3D(0d, 0d, min.Z), new Vector3D(0d, 0d, max.Z), maxRadius);
						lengths.Add(e.CellSize * percent);
						isTriangle = true;
					}
					else
					{
						//TODO: Test this, execution has never gotten here

						//	Find the intersect along the max z line
						percent = GetMassBreakdownSprtCylinderSprtIntersectPercent(new Vector3D(0d, 0d, max.Z), new Vector3D(0d, max.Y, max.Z), maxRadius);
						lengths.Add(e.CellSize * percent * 2d);		// percent is from 0 to Y, so double it to get -Y to Y
					}

					#endregion

					//	Calculate the area (just assuming the circle portion is a straight line)
					switch (lengths.Count)
					{
						case 2:
							//TODO: Test this, execution has never gotten here

							if (isTriangle)
							{
								//	Triangle (1/2 * base * height)
								mass = lengths[0] * lengths[1] * .5d;
							}
							else
							{
								//	Trapazoid (.5 * (b1 + b2) * h)
								mass = (lengths[0] + lengths[1]) * e.CellSize * .5d;
							}
							break;

						case 4:
							//	This is a rectangle + (triangle or trapazoid).  Either way, calculate the rectangle portion
							mass = lengths[0] * lengths[1];

							if (isTriangle)
							{
								//	Rect + Triangle
								mass += lengths[0] * (lengths[3] - lengths[1]) * .5d;
							}
							else
							{
								//TODO: Test this, execution has never gotten here

								//	Rect + Trapazoid
								mass += (lengths[0] + lengths[3]) * (e.CellSize - lengths[1]) * .5d;
							}
							break;

						default:
							throw new ApplicationException("Unexpected number of segments: " + lengths.Count.ToString());
					}

					#endregion
				}

				#endregion

				#region Set mass

				//	The end caps get partial height along x
				double xMin = e.AABBMin.X + ((e.XLength - 1) * e.CellSize);

				double volume = mass * (halfObjSize.X - xMin);
				masses[e.GetIndex(0, y, z)] = volume;
				masses[e.GetIndex(0, y, altZ)] = volume;

				masses[e.GetIndex(e.XLength - 1, y, z)] = volume;
				masses[e.GetIndex(e.XLength - 1, y, altZ)] = volume;

				//	Everything inside gets the full height
				volume = mass * e.CellSize;
				for (int x = 1; x < e.XLength - 1; x++)
				{
					masses[e.GetIndex(x, y, z)] = volume;
					masses[e.GetIndex(x, y, altZ)] = volume;
				}

				#endregion
			}
		}
		private static void GetMassBreakdownSprtCylinderSprtZZero(double[] masses, int yStart, int z, bool isYEven, double ratioY, double ratioZ, Vector3D halfObjSize, double maxRadius, ObjectMassBreakdown e)
		{
			for (int y = yStart; y < e.YLength; y++)
			{
				int altY = yStart - (y + (isYEven ? 1 : 2) - yStart);

				#region Area of Y,Z tile

				//	Figure out how much of the circle is inside this square (just 2D for now)
				Vector3D min = new Vector3D(0, (e.AABBMin.Y + (y * e.CellSize)) * ratioY, e.CellSize * .5d * ratioZ);		//NOTE: min has positive z as well as max.  This just makes some of the calculations easier
				Vector3D max = new Vector3D(0, (e.AABBMin.Y + ((y + 1) * e.CellSize)) * ratioY, min.Z);

				double mass = 0d;

				double maxLength = max.Length;
				if (maxLength < maxRadius)
				{
					//	This tile is completely inside the circle
					mass = e.CellSize * e.CellSize;		//	just the 2D mass
				}
				else if (min.Y < maxRadius)		//	the left edge should never be greater than the circle, but just making sure
				{
					#region Intersect Circle

					List<double> lengths = new List<double>();
					double percent;
					bool isTriangle = false;

					#region Z

					double minLength = min.Length;
					if (minLength < maxRadius)
					{
						//	Circle intersects the line from min.Y,Z to max.Y,Z
						lengths.Add(e.CellSize);		//	add one for the left segment

						//	Add one for the z intercept
						percent = GetMassBreakdownSprtCylinderSprtIntersectPercent(new Vector3D(0d, min.Y, min.Z), new Vector3D(0d, max.Y, max.Z), maxRadius);
						lengths.Add(e.CellSize * percent);
						lengths.Add(e.CellSize * percent);
					}
					else
					{
						//	Circle never goes as far as min/max.Z, find the intersect along the min y line
						//NOTE: There are 2 intersects, so define min z as 0
						percent = GetMassBreakdownSprtCylinderSprtIntersectPercent(new Vector3D(0d, min.Y, 0d), new Vector3D(0d, min.Y, min.Z), maxRadius);		//	min.Z is positive
						lengths.Add(e.CellSize * percent * 2d);		// percent is from 0 to Z, so double it to get -Z to Z
					}

					#endregion
					#region Y

					if (max.Y >= maxRadius)
					{
						//	The circle doesn't go all the way to the right edge, so assume a triangle
						percent = GetMassBreakdownSprtCylinderSprtIntersectPercent(new Vector3D(0d, min.Y, 0d), new Vector3D(0d, max.Y, 0), maxRadius);
						lengths.Add(e.CellSize * percent);
						isTriangle = true;
					}
					else
					{
						//TODO: Test this, execution has never gotten here

						//	Find the intersect along the max y line
						percent = GetMassBreakdownSprtCylinderSprtIntersectPercent(new Vector3D(0d, max.Y, 0d), new Vector3D(0d, max.Y, max.Z), maxRadius);
						lengths.Add(e.CellSize * percent * 2d);		// percent is from 0 to Z, so double it to get -Z to Z
					}

					#endregion

					//	Calculate the area (just assuming the circle portion is a straight line)
					switch (lengths.Count)
					{
						case 2:
							if (isTriangle)
							{
								//	Triangle (1/2 * base * height)
								mass = lengths[0] * lengths[1] * .5d;
							}
							else
							{
								//	Trapazoid (.5 * (b1 + b2) * h)
								mass = (lengths[0] + lengths[1]) * e.CellSize * .5d;
							}
							break;

						case 4:
							//	This is a rectangle + (triangle or trapazoid).  Either way, calculate the rectangle portion
							mass = lengths[0] * lengths[1];

							if (isTriangle)
							{
								//	Rect + Triangle
								mass += lengths[0] * (lengths[3] - lengths[1]) * .5d;
							}
							else
							{
								//TODO: Test this, execution has never gotten here

								//	Rect + Trapazoid
								mass += (lengths[0] + lengths[3]) * (e.CellSize - lengths[1]) * .5d;
							}
							break;

						default:
							throw new ApplicationException("Unexpected number of segments: " + lengths.Count.ToString());
					}

					#endregion
				}

				#endregion

				#region Set mass

				//	The end caps get partial height along x
				double xMin = e.AABBMin.X + ((e.XLength - 1) * e.CellSize);

				double volume = mass * (halfObjSize.X - xMin);
				masses[e.GetIndex(0, y, z)] = volume;
				masses[e.GetIndex(0, altY, z)] = volume;

				masses[e.GetIndex(e.XLength - 1, y, z)] = volume;
				masses[e.GetIndex(e.XLength - 1, altY, z)] = volume;

				//	Everything inside gets the full height
				volume = mass * e.CellSize;
				for (int x = 1; x < e.XLength - 1; x++)
				{
					masses[e.GetIndex(x, y, z)] = volume;
					masses[e.GetIndex(x, altY, z)] = volume;
				}

				#endregion
			}
		}
		private static void GetMassBreakdownSprtCylinderSprtYZZero(double[] masses, int y, int z, double ratioY, double ratioZ, Vector3D halfObjSize, double maxRadius, ObjectMassBreakdown e)
		{
			#region Area of Y,Z tile

			//	Figure out how much of the circle is inside this square (just 2D for now)
			Vector3D max = new Vector3D(0, e.CellSize * .5d * ratioY, e.CellSize * .5d * ratioZ);

			double mass = 0d;

			double maxLength = max.Length;
			if (maxLength < maxRadius)
			{
				//	This tile is completely inside the circle
				mass = e.CellSize * e.CellSize;		//	just the 2D mass
			}
			else
			{
				//double percent = maxRadius / maxLength;		//	radius is half of the cell, and max is also half of the cell, so no need to multiply anything by 2
				double percent = maxRadius / (e.CellSize * .5d);		//	the previous line is wrong, max.length goes to the corner of the cell, but maxRadius is calculated with the length to the side of the cell

				percent *= e.CellSize * .5d;		//	reuse percent as the scaled radius

				//	A = pi r^2
				mass = Math.PI * percent * percent;
			}

			#endregion

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
		private static void GetMassBreakdownSprtCylinderSprtQuadrant(double[] masses, int yStart, int zStart, bool isYEven, bool isZEven, double ratioY, double ratioZ, Vector3D halfObjSize, double maxRadius, ObjectMassBreakdown e)
		{
			//	Loop through the y,z tiles in only one quadrant, then just copy the result to the other 3 quadrants
			for (int z = zStart; z < e.ZLength; z++)
			{
				int zOffset = z * e.XLength * e.YLength;

				for (int y = yStart; y < e.YLength; y++)
				{
					int yOffset = y * e.XLength;

					//	Figure out what the coords are in the other quadrants (when the length of the axis odd, start needs to be skipped over)
					int altY = yStart - (y + (isYEven ? 1 : 2) - yStart);
					int altZ = zStart - (z + (isZEven ? 1 : 2) - zStart);

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

					#region Set mass

					//	The end caps get partial height along x
					double xMin = e.AABBMin.X + ((e.XLength - 1) * e.CellSize);

					double volume = mass * (halfObjSize.X - xMin);
					masses[e.GetIndex(0, y, z)] = volume;
					masses[e.GetIndex(0, altY, z)] = volume;
					masses[e.GetIndex(0, y, altZ)] = volume;
					masses[e.GetIndex(0, altY, altZ)] = volume;

					masses[e.GetIndex(e.XLength - 1, y, z)] = volume;
					masses[e.GetIndex(e.XLength - 1, altY, z)] = volume;
					masses[e.GetIndex(e.XLength - 1, y, altZ)] = volume;
					masses[e.GetIndex(e.XLength - 1, altY, altZ)] = volume;

					//	Everything inside gets the full height
					volume = mass * e.CellSize;
					for (int x = 1; x < e.XLength - 1; x++)
					{
						masses[e.GetIndex(x, y, z)] = volume;
						masses[e.GetIndex(x, altY, z)] = volume;
						masses[e.GetIndex(x, y, altZ)] = volume;
						masses[e.GetIndex(x, altY, altZ)] = volume;
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
				else if (Math3D.IsNearZero(t1))
				{
					return 0d;
				}
				else if (Math3D.IsNearValue(t1, 1d))
				{
					return 1d;
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
				else if (Math3D.IsNearZero(t2))
				{
					return 0d;
				}
				else if (Math3D.IsNearValue(t2, 1d))
				{
					return 1d;
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

		private static void CalculateReturnSize(out Tuple<int, int, int> lengths, out Tuple<Point3D, Point3D> aabb, Vector3D size, double cellSize)
		{
			//	Figure out how big to make the return (it will be an even multiple of the cell size)
			int xLen = Convert.ToInt32(Math.Ceiling(size.X / cellSize));
			int yLen = Convert.ToInt32(Math.Ceiling(size.Y / cellSize));
			int zLen = Convert.ToInt32(Math.Ceiling(size.Z / cellSize));

			Point3D aabbMax = new Point3D((cellSize * xLen) / 2d, (cellSize * yLen) / 2d, (cellSize * zLen) / 2d);
			Point3D aabbMin = new Point3D(-aabbMax.X, -aabbMax.Y, -aabbMax.Z);		//	it's centered at zero, so min is just max negated

			//	Build the returns
			lengths = new Tuple<int, int, int>(xLen, yLen, zLen);
			aabb = new Tuple<Point3D, Point3D>(aabbMin, aabbMax);
		}

		/// <summary>
		/// Once this method completes, the sum of the mass in the array will be 1
		/// </summary>
		private static void Normalize(double[] masses)
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

		/// <summary>
		/// This returns the same sized set of breakdown objects as what was passed in, but the sum of the returned mass adds to 1
		/// </summary>
		private static ObjectMassBreakdown[] Normalize(IEnumerable<Tuple<ObjectMassBreakdown, Point3D, Quaternion, double>> items)
		{
			List<ObjectMassBreakdown> retVal = new List<ObjectMassBreakdown>();

			double total = items.Sum(o => o.Item1.Masses.Sum() * o.Item4);
			if (total == 0d)
			{
				retVal.AddRange(items.Select(o => o.Item1));
				return retVal.ToArray();
			}

			double mult = 1d / total;		//	multiplication is cheaper than division, so just do the division once

			foreach (var item in items)
			{
				retVal.Add(new ObjectMassBreakdown(
					item.Item1.ObjectType, item.Item1.ObjectSize,
					item.Item1.AABBMin, item.Item1.AABBMax,
					item.Item1.CenterMass, item.Item1.CellSize,
					item.Item1.XLength, item.Item1.YLength, item.Item1.ZLength,
					item.Item1.Centers,
					item.Item1.Masses.Select(o => o * item.Item4 * mult).ToArray()));
			}

			return retVal.ToArray();
		}

		#endregion
	}
}
