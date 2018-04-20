﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.HelperClassesWPF.Controls3D;

namespace Game.Newt.v2.NewtonDynamics
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
            Dome,		// this is half a sphere
            //Cone,
            Sphere,
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

            // The AABB size will always be a multiple of cell size (it will likely be a bit larger than ObjectSize - the object is centered inside of it)
            public readonly Point3D AABBMin;
            public readonly Point3D AABBMax;
            private readonly Point3D _centerMass;
            public readonly double CellSize;

            public readonly int XLength;
            public readonly int YLength;
            public readonly int ZLength;

            //public readonly Tuple<Point3D, double>[] Cells;		// I don't want to take the expense of all the instances of tuple

            // These are all the cells
            //NOTE: The arrays are readonly, but C# doesn't enforce the elements to be ready only.  Don't change them, that would be bad
            //NOTE: Multidimension arrays are slower than single dimension, so you'll have to do the math yourself (see this.GetIndex)
            public readonly Point3D[] Centers;
            public readonly double[] Masses;

            #region Private Methods

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

                // Transform the centers (doing this once in the constructor so consumers can just use the values)
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

            // These arrays are all the same size
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

                // Exit Function
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
        #region Class: MassPart

        public class MassPart
        {
            public MassPart(Point3D position, double mass, Quaternion orientation, IObjectMassBreakdown breakdown)
            {
                this.Position = position;
                this.Mass = mass;
                this.Orientation = orientation;
                this.Breakdown = breakdown;
            }

            public readonly Point3D Position;
            public readonly double Mass;
            public readonly Quaternion Orientation;

            public readonly IObjectMassBreakdown Breakdown;
        }

        #endregion

        #region Class: AxisInfo_Sphere

        private class AxisInfo_Sphere
        {
            public Tuple<int, int, int>[] Indices { get; set; }
            public Tuple<Point3D, Point3D>[] Cells { get; set; }
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
            // Figure out the dimensions of the return
            Tuple<int, int, int> lengths;
            Tuple<Point3D, Point3D> aabb;
            CalculateReturnSize(out lengths, out aabb, size, cellSize);

            // Instead of passing loose variables to the private methods, just fill out what I can of the return, and use it like an args class
            ObjectMassBreakdown args = new ObjectMassBreakdown(objectType, size, aabb.Item1, aabb.Item2, new Point3D(), cellSize, lengths.Item1, lengths.Item2, lengths.Item3, null, null);

            Point3D[] centers = GetCenters(args);

            Point3D centerMass;
            double[] masses;

            switch (objectType)
            {
                case ObjectBreakdownType.Box:
                    #region Box

                    centerMass = new Point3D(0, 0, 0);		// for a box, the center of mass is the same as center of position

                    if (lengths.Item1 == 1 || lengths.Item2 == 1 || lengths.Item3 == 1)
                    {
                        masses = GetMassBreakdown_Box2D(args);
                    }
                    else
                    {
                        masses = GetMassBreakdown_Box3D(args);
                    }

                    #endregion
                    break;

                case ObjectBreakdownType.Cylinder:
                    #region Cylinder

                    centerMass = new Point3D(0, 0, 0);		// for a cylinder, the center of mass is the same as center of position

                    masses = GetMassBreakdown_Cylinder(args);

                    #endregion
                    break;

                case ObjectBreakdownType.Sphere:
                    #region Sphere

                    centerMass = new Point3D(0, 0, 0);		// for a sphere, the center of mass is the same as center of position

                    if (size.X != size.Y || size.X != size.Z)
                    {
                        masses = GetMassBreakdown_Ellipsoid(args);
                    }
                    else
                    {
                        //NOTE: There is no need to break down a perfect sphere.  The parallel axis theorem works just fine with a single one.  So always return a single cell
                        masses = new double[] { 1d };

                        cellSize = size.X;
                        CalculateReturnSize(out lengths, out aabb, size, cellSize);
                    }

                    #endregion
                    break;

                default:
                    throw new ApplicationException("finish this - " + objectType.ToString());
            }

            // Now make the total mass add up to 1
            Normalize(masses);

            // Exit Function
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
            // Make the mass of all the objects add to one (the ratios are preserved, just the sum of masses changes)
            ObjectMassBreakdown[] normalized = Normalize(objects);

            // Build transforms
            Transform3D[] transforms = new Transform3D[objects.Length];
            for (int cntr = 0; cntr < objects.Length; cntr++)
            {
                Transform3DGroup transform = new Transform3DGroup();
                transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(objects[cntr].Item3)));
                transform.Children.Add(new TranslateTransform3D(objects[cntr].Item2.ToVector()));

                transforms[cntr] = transform;
            }

            // Exit Function
            return new ObjectMassBreakdownSet(normalized, transforms);
        }

        public static Tuple<MassMatrix, Point3D> GetMassMatrix_CenterOfMass(MassPart[] parts, double cellSize, double inertiaMultiplier = 1d)
        {
            #region Prep work

            double cellSphereMultiplier = (cellSize * .5d) * (cellSize * .5d) * .4d;		// 2/5 * r^2

            double totalMass = parts.Sum(o => o.Mass);
            double totalMassInverse = 1d / totalMass;

            Vector3D axisX = new Vector3D(1d, 0d, 0d);
            Vector3D axisY = new Vector3D(0d, 1d, 0d);
            Vector3D axisZ = new Vector3D(0d, 0d, 1d);

            #endregion

            #region Center of mass

            // Calculate the center of mass
            double centerX = 0d;
            double centerY = 0d;
            double centerZ = 0d;
            for (int cntr = 0; cntr < parts.Length; cntr++)
            {
                // Shift the part into ship coords
                Point3D centerMass = parts[cntr].Position + parts[cntr].Breakdown.CenterMass.ToVector();

                centerX += centerMass.X * parts[cntr].Mass;
                centerY += centerMass.Y * parts[cntr].Mass;
                centerZ += centerMass.Z * parts[cntr].Mass;
            }

            Point3D center = new Point3D(centerX * totalMassInverse, centerY * totalMassInverse, centerZ * totalMassInverse);

            #endregion

            #region Local inertias

            // Get the local moment of inertia of each part for each of the three ship's axiis
            //TODO: If the number of cells is large, this would be a good candidate for running in parallel, but this method keeps cellSize pretty course
            Vector3D[] localInertias = new Vector3D[parts.Length];

            for (int cntr = 0; cntr < parts.Length; cntr++)
            {
                RotateTransform3D localRotation = new RotateTransform3D(new QuaternionRotation3D(parts[cntr].Orientation.ToReverse()));

                //TODO: Verify these results with the equation for the moment of inertia of a cylinder

                //NOTE: Each mass breakdown adds up to a mass of 1, so putting that mass back now (otherwise the ratios of masses between parts would be lost)
                localInertias[cntr] = new Vector3D(
                    GetInertia(parts[cntr].Breakdown, localRotation.Transform(axisX), cellSphereMultiplier) * parts[cntr].Mass,
                    GetInertia(parts[cntr].Breakdown, localRotation.Transform(axisY), cellSphereMultiplier) * parts[cntr].Mass,
                    GetInertia(parts[cntr].Breakdown, localRotation.Transform(axisZ), cellSphereMultiplier) * parts[cntr].Mass);
            }

            #endregion
            #region Global inertias

            // Apply the parallel axis theorem to each part
            double shipInertiaX = 0d;
            double shipInertiaY = 0d;
            double shipInertiaZ = 0d;
            for (int cntr = 0; cntr < parts.Length; cntr++)
            {
                // Shift the part into ship coords
                Point3D partCenter = parts[cntr].Position + parts[cntr].Breakdown.CenterMass.ToVector();

                shipInertiaX += GetInertia(partCenter, localInertias[cntr].X, parts[cntr].Mass, center, axisX);
                shipInertiaY += GetInertia(partCenter, localInertias[cntr].Y, parts[cntr].Mass, center, axisY);
                shipInertiaZ += GetInertia(partCenter, localInertias[cntr].Z, parts[cntr].Mass, center, axisZ);
            }

            #endregion

            // Newton wants the inertia vector to be one, so divide off the mass of all the parts <- not sure why I said they need to be one.  Here is a response from Julio Jerez himself:
            //this is the correct way
            //NewtonBodySetMassMatrix( priv->body, mass, mass * inertia[0], mass * inertia[1], mass * inertia[2] );
            //matrix = new MassMatrix(totalMass, new Vector3D(shipInertiaX * totalMassInverse, shipInertiaY * totalMassInverse, shipInertiaZ * totalMassInverse));
            MassMatrix matrix = new MassMatrix(totalMass, new Vector3D(shipInertiaX * inertiaMultiplier, shipInertiaY * inertiaMultiplier, shipInertiaZ * inertiaMultiplier));

            return Tuple.Create(matrix, center);
        }

        #region Private Methods - cylinder

        private static double[] GetMassBreakdown_Cylinder(ObjectMassBreakdown e)
        {
            double[] retVal = new double[e.ZLength * e.YLength * e.XLength];

            #region Pre calculations

            double halfSize = e.CellSize * .5d;
            Vector3D halfObjSize = new Vector3D(e.ObjectSize.X * .5d, e.ObjectSize.Y * .5d, e.ObjectSize.Z * .5d);
            double wholeVolume = e.CellSize * e.CellSize * e.CellSize;

            // The cylinder's axis is along x
            // Only go through the quadrant with positive values.  Then copy the results to the other three quadrants (and then other 4 quadrants in the -x)

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

            // These tell how to convert the ellipse into a circle
            double objectSizeHalfY = e.ObjectSize.Y * .5d;
            double objectSizeHalfZ = e.ObjectSize.Z * .5d;
            double maxRadius = Math.Max(objectSizeHalfY, objectSizeHalfZ);
            double ratioY = maxRadius / objectSizeHalfY;
            double ratioZ = maxRadius / objectSizeHalfZ;

            #endregion

            #region Y,Z axis tiles

            if (!isYEven)
            {
                GetMassBreakdown_Cylinder_YZero(retVal, yStart - 1, zStart, isZEven, ratioZ, ratioZ, halfObjSize, maxRadius, e);
            }

            if (!isZEven)
            {
                GetMassBreakdown_Cylinder_ZZero(retVal, yStart, zStart - 1, isYEven, ratioY, ratioZ, halfObjSize, maxRadius, e);
            }

            if (!isYEven && !isZEven)
            {
                GetMassBreakdown_Cylinder_YZZero(retVal, yStart - 1, zStart - 1, ratioY, ratioZ, halfObjSize, maxRadius, e);
            }

            #endregion

            // Quadrant tiles
            GetMassBreakdown_Cylinder_Quadrant(retVal, yStart, zStart, isYEven, isZEven, ratioY, ratioZ, halfObjSize, maxRadius, e);

            // Exit Function
            return retVal;
        }
        private static void GetMassBreakdown_Cylinder_YZero(double[] masses, int y, int zStart, bool isZEven, double ratioY, double ratioZ, Vector3D halfObjSize, double maxRadius, ObjectMassBreakdown e)
        {
            for (int z = zStart; z < e.ZLength; z++)
            {
                int altZ = zStart - (z + (isZEven ? 1 : 2) - zStart);

                #region Area of Y,Z tile

                // Figure out how much of the circle is inside this square (just 2D for now)
                Vector3D min = new Vector3D(0, e.CellSize * .5d * ratioY, (e.AABBMin.Z + (z * e.CellSize)) * ratioZ);		//NOTE: min has positive y as well as max.  This just makes some of the calculations easier
                Vector3D max = new Vector3D(0, min.Y, (e.AABBMin.Z + ((z + 1) * e.CellSize)) * ratioZ);

                double mass = 0d;

                double maxLength = max.Length;
                if (maxLength < maxRadius)
                {
                    // This tile is completely inside the circle
                    mass = e.CellSize * e.CellSize;		// just the 2D mass
                }
                else if (min.Z < maxRadius)		// the bottom edge should never be greater than the circle, but just making sure
                {
                    #region Intersect Circle

                    List<double> lengths = new List<double>();
                    double percent;
                    bool isTriangle = false;

                    #region Y

                    double minLength = min.Length;
                    if (minLength < maxRadius)
                    {
                        // Circle intersects the line from min.Z,Y to max.Z,Y
                        lengths.Add(e.CellSize);		// add one for the bottom segment

                        // Add one for the y intercept
                        percent = GetMassBreakdown_Cylinder_IntersectPercent(new Vector3D(0d, min.Y, min.Z), new Vector3D(0d, max.Y, max.Z), maxRadius);
                        lengths.Add(e.CellSize * percent);
                        lengths.Add(e.CellSize * percent);
                    }
                    else
                    {
                        //TODO: Test this, execution has never gotten here

                        // Circle never goes as far as min/max.Y, find the intersect along the min z line
                        //NOTE: There are 2 intersects, so define min z as 0
                        percent = GetMassBreakdown_Cylinder_IntersectPercent(new Vector3D(0d, 0d, min.Z), new Vector3D(0d, min.Y, min.Z), maxRadius);		// min.Y is positive
                        lengths.Add(e.CellSize * percent * 2d);		// percent is from 0 to Y, so double it to get -Y to Y
                    }

                    #endregion
                    #region Z

                    if (max.Z >= maxRadius)
                    {
                        // The circle doesn't go all the way to the right edge, so assume a triangle
                        percent = GetMassBreakdown_Cylinder_IntersectPercent(new Vector3D(0d, 0d, min.Z), new Vector3D(0d, 0d, max.Z), maxRadius);
                        lengths.Add(e.CellSize * percent);
                        isTriangle = true;
                    }
                    else
                    {
                        //TODO: Test this, execution has never gotten here

                        // Find the intersect along the max z line
                        percent = GetMassBreakdown_Cylinder_IntersectPercent(new Vector3D(0d, 0d, max.Z), new Vector3D(0d, max.Y, max.Z), maxRadius);
                        lengths.Add(e.CellSize * percent * 2d);		// percent is from 0 to Y, so double it to get -Y to Y
                    }

                    #endregion

                    // Calculate the area (just assuming the circle portion is a straight line)
                    switch (lengths.Count)
                    {
                        case 2:
                            //TODO: Test this, execution has never gotten here

                            if (isTriangle)
                            {
                                // Triangle (1/2 * base * height)
                                mass = lengths[0] * lengths[1] * .5d;
                            }
                            else
                            {
                                // Trapazoid (.5 * (b1 + b2) * h)
                                mass = (lengths[0] + lengths[1]) * e.CellSize * .5d;
                            }
                            break;

                        case 4:
                            // This is a rectangle + (triangle or trapazoid).  Either way, calculate the rectangle portion
                            mass = lengths[0] * lengths[1];

                            if (isTriangle)
                            {
                                // Rect + Triangle
                                mass += lengths[0] * (lengths[3] - lengths[1]) * .5d;
                            }
                            else
                            {
                                //TODO: Test this, execution has never gotten here

                                // Rect + Trapazoid
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

                // The end caps get partial height along x
                double xMin = e.AABBMin.X + ((e.XLength - 1) * e.CellSize);

                double volume = mass * (halfObjSize.X - xMin);
                masses[e.GetIndex(0, y, z)] = volume;
                masses[e.GetIndex(0, y, altZ)] = volume;

                masses[e.GetIndex(e.XLength - 1, y, z)] = volume;
                masses[e.GetIndex(e.XLength - 1, y, altZ)] = volume;

                // Everything inside gets the full height
                volume = mass * e.CellSize;
                for (int x = 1; x < e.XLength - 1; x++)
                {
                    masses[e.GetIndex(x, y, z)] = volume;
                    masses[e.GetIndex(x, y, altZ)] = volume;
                }

                #endregion
            }
        }
        private static void GetMassBreakdown_Cylinder_ZZero(double[] masses, int yStart, int z, bool isYEven, double ratioY, double ratioZ, Vector3D halfObjSize, double maxRadius, ObjectMassBreakdown e)
        {
            for (int y = yStart; y < e.YLength; y++)
            {
                int altY = yStart - (y + (isYEven ? 1 : 2) - yStart);

                #region Area of Y,Z tile

                // Figure out how much of the circle is inside this square (just 2D for now)
                Vector3D min = new Vector3D(0, (e.AABBMin.Y + (y * e.CellSize)) * ratioY, e.CellSize * .5d * ratioZ);		//NOTE: min has positive z as well as max.  This just makes some of the calculations easier
                Vector3D max = new Vector3D(0, (e.AABBMin.Y + ((y + 1) * e.CellSize)) * ratioY, min.Z);

                double mass = 0d;

                double maxLength = max.Length;
                if (maxLength < maxRadius)
                {
                    // This tile is completely inside the circle
                    mass = e.CellSize * e.CellSize;		// just the 2D mass
                }
                else if (min.Y < maxRadius)		// the left edge should never be greater than the circle, but just making sure
                {
                    #region Intersect Circle

                    List<double> lengths = new List<double>();
                    double percent;
                    bool isTriangle = false;

                    #region Z

                    double minLength = min.Length;
                    if (minLength < maxRadius)
                    {
                        // Circle intersects the line from min.Y,Z to max.Y,Z
                        lengths.Add(e.CellSize);		// add one for the left segment

                        // Add one for the z intercept
                        percent = GetMassBreakdown_Cylinder_IntersectPercent(new Vector3D(0d, min.Y, min.Z), new Vector3D(0d, max.Y, max.Z), maxRadius);
                        lengths.Add(e.CellSize * percent);
                        lengths.Add(e.CellSize * percent);
                    }
                    else
                    {
                        // Circle never goes as far as min/max.Z, find the intersect along the min y line
                        //NOTE: There are 2 intersects, so define min z as 0
                        percent = GetMassBreakdown_Cylinder_IntersectPercent(new Vector3D(0d, min.Y, 0d), new Vector3D(0d, min.Y, min.Z), maxRadius);		// min.Z is positive
                        lengths.Add(e.CellSize * percent * 2d);		// percent is from 0 to Z, so double it to get -Z to Z
                    }

                    #endregion
                    #region Y

                    if (max.Y >= maxRadius)
                    {
                        // The circle doesn't go all the way to the right edge, so assume a triangle
                        percent = GetMassBreakdown_Cylinder_IntersectPercent(new Vector3D(0d, min.Y, 0d), new Vector3D(0d, max.Y, 0), maxRadius);
                        lengths.Add(e.CellSize * percent);
                        isTriangle = true;
                    }
                    else
                    {
                        //TODO: Test this, execution has never gotten here

                        // Find the intersect along the max y line
                        percent = GetMassBreakdown_Cylinder_IntersectPercent(new Vector3D(0d, max.Y, 0d), new Vector3D(0d, max.Y, max.Z), maxRadius);
                        lengths.Add(e.CellSize * percent * 2d);		// percent is from 0 to Z, so double it to get -Z to Z
                    }

                    #endregion

                    // Calculate the area (just assuming the circle portion is a straight line)
                    switch (lengths.Count)
                    {
                        case 2:
                            if (isTriangle)
                            {
                                // Triangle (1/2 * base * height)
                                mass = lengths[0] * lengths[1] * .5d;
                            }
                            else
                            {
                                // Trapazoid (.5 * (b1 + b2) * h)
                                mass = (lengths[0] + lengths[1]) * e.CellSize * .5d;
                            }
                            break;

                        case 4:
                            // This is a rectangle + (triangle or trapazoid).  Either way, calculate the rectangle portion
                            mass = lengths[0] * lengths[1];

                            if (isTriangle)
                            {
                                // Rect + Triangle
                                mass += lengths[0] * (lengths[3] - lengths[1]) * .5d;
                            }
                            else
                            {
                                //TODO: Test this, execution has never gotten here

                                // Rect + Trapazoid
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

                // The end caps get partial height along x
                double xMin = e.AABBMin.X + ((e.XLength - 1) * e.CellSize);

                double volume = mass * (halfObjSize.X - xMin);
                masses[e.GetIndex(0, y, z)] = volume;
                masses[e.GetIndex(0, altY, z)] = volume;

                masses[e.GetIndex(e.XLength - 1, y, z)] = volume;
                masses[e.GetIndex(e.XLength - 1, altY, z)] = volume;

                // Everything inside gets the full height
                volume = mass * e.CellSize;
                for (int x = 1; x < e.XLength - 1; x++)
                {
                    masses[e.GetIndex(x, y, z)] = volume;
                    masses[e.GetIndex(x, altY, z)] = volume;
                }

                #endregion
            }
        }
        private static void GetMassBreakdown_Cylinder_YZZero(double[] masses, int y, int z, double ratioY, double ratioZ, Vector3D halfObjSize, double maxRadius, ObjectMassBreakdown e)
        {
            #region Area of Y,Z tile

            // Figure out how much of the circle is inside this square (just 2D for now)
            Vector3D max = new Vector3D(0, e.CellSize * .5d * ratioY, e.CellSize * .5d * ratioZ);

            double mass = 0d;

            double maxLength = max.Length;
            if (maxLength < maxRadius)
            {
                // This tile is completely inside the circle
                mass = e.CellSize * e.CellSize;		// just the 2D mass
            }
            else
            {
                //double percent = maxRadius / maxLength;		// radius is half of the cell, and max is also half of the cell, so no need to multiply anything by 2
                double percent = maxRadius / (e.CellSize * .5d);		// the previous line is wrong, max.length goes to the corner of the cell, but maxRadius is calculated with the length to the side of the cell

                percent *= e.CellSize * .5d;		// reuse percent as the scaled radius

                // A = pi r^2
                mass = Math.PI * percent * percent;
            }

            #endregion

            #region Set mass

            // The end caps get partial height along x
            double xMin = e.AABBMin.X + ((e.XLength - 1) * e.CellSize);

            double volume = mass * (halfObjSize.X - xMin);
            masses[e.GetIndex(0, y, z)] = volume;
            masses[e.GetIndex(e.XLength - 1, y, z)] = volume;

            // Everything inside gets the full height
            volume = mass * e.CellSize;
            for (int x = 1; x < e.XLength - 1; x++)
            {
                masses[e.GetIndex(x, y, z)] = volume;
            }

            #endregion
        }
        private static void GetMassBreakdown_Cylinder_Quadrant(double[] masses, int yStart, int zStart, bool isYEven, bool isZEven, double ratioY, double ratioZ, Vector3D halfObjSize, double maxRadius, ObjectMassBreakdown e)
        {
            // Loop through the y,z tiles in only one quadrant, then just copy the result to the other 3 quadrants
            for (int z = zStart; z < e.ZLength; z++)
            {
                for (int y = yStart; y < e.YLength; y++)
                {
                    // Figure out what the coords are in the other quadrants (when the length of the axis odd, start needs to be skipped over)
                    int altY = yStart - (y + (isYEven ? 1 : 2) - yStart);
                    int altZ = zStart - (z + (isZEven ? 1 : 2) - zStart);

                    #region Area of Y,Z tile

                    // Figure out how much of the circle is inside this square (just 2D for now)
                    Vector3D min = new Vector3D(
                        0,
                        (e.AABBMin.Y + (y * e.CellSize)) * ratioY,
                        (e.AABBMin.Z + (z * e.CellSize)) * ratioZ);

                    Vector3D max = new Vector3D(0,
                        (e.AABBMin.Y + ((y + 1) * e.CellSize)) * ratioY,
                        (e.AABBMin.Z + ((z + 1) * e.CellSize)) * ratioZ);

                    double mass = 0d;

                    double maxLength = max.Length;
                    if (maxLength < maxRadius)
                    {
                        // This tile is completely inside the circle
                        mass = e.CellSize * e.CellSize;		// just the 2D mass
                    }
                    else
                    {
                        double minLength = min.Length;
                        if (minLength > maxRadius)
                        {
                            // This tile is completely outside the circle
                            mass = 0d;
                        }
                        else
                        {
                            #region Intersect Circle

                            // This tile is clipped by the circle.  Figure out which edges the circle is intersecting
                            Vector3D minPlusY = new Vector3D(0, max.Y, min.Z);
                            Vector3D minPlusZ = new Vector3D(0, min.Y, max.Z);

                            // When calculating the area, it will either be a triangle, trapazoid or diamond
                            List<double> lengths = new List<double>();

                            double percent;

                            if (minPlusY.Length > maxRadius)
                            {
                                // The intercept is along the line from min to min+Y
                                percent = GetMassBreakdown_Cylinder_IntersectPercent(min, minPlusY, maxRadius);
                                lengths.Add(e.CellSize * percent);
                            }
                            else
                            {
                                // The intercept is along the line from min+Y to max
                                lengths.Add(e.CellSize);
                                percent = GetMassBreakdown_Cylinder_IntersectPercent(minPlusY, max, maxRadius);
                                lengths.Add(e.CellSize * percent);
                            }

                            if (minPlusZ.Length > maxRadius)
                            {
                                // The intercept is along the line from min to min+Z
                                percent = GetMassBreakdown_Cylinder_IntersectPercent(min, minPlusZ, maxRadius);
                                lengths.Add(e.CellSize * percent);
                            }
                            else
                            {
                                // The intercept is along the line from min+Z to max
                                lengths.Add(e.CellSize);
                                percent = GetMassBreakdown_Cylinder_IntersectPercent(minPlusZ, max, maxRadius);
                                lengths.Add(e.CellSize * percent);
                            }

                            // Calculate the area (just assuming the circle portion is a straight line)
                            switch (lengths.Count)
                            {
                                case 2:
                                    // Triangle (1/2 * base * height)
                                    mass = lengths[0] * lengths[1] * .5d;
                                    break;

                                case 3:
                                    // Trapazoid
                                    lengths.Sort();
                                    mass = lengths[0] * lengths[2];		// get the area of the shortest side x base
                                    mass += lengths[2] * (lengths[1] - lengths[0]) * .5d;		// tack on the area of the triangle above that rectangle
                                    break;

                                case 4:
                                    // Diamond
                                    lengths.Sort();
                                    mass = lengths[3] * lengths[1];		// get the area of the "horizontal" rectangle
                                    mass += (lengths[2] - lengths[1]) * lengths[0];		// add the area of the "vertical" rectangle
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

                    // The end caps get partial height along x
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

                    // Everything inside gets the full height
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
        private static double GetMassBreakdown_Cylinder_IntersectPercent(Vector3D lineStart, Vector3D lineStop, double radius)
        {
            Point start2D = new Point(lineStart.Y, lineStart.Z);
            Point stop2D = new Point(lineStop.Y, lineStop.Z);

            double? retVal = Math2D.GetIntersection_LineSegment_Circle_percent(start2D, stop2D, new Point(0, 0), radius);
            if (retVal == null)
            {
                throw new ApplicationException("Shouldn't get null");
            }

            return retVal.Value;
        }

        #endregion
        #region Private Methods - ellipsoid

        private static double[] GetMassBreakdown_Ellipsoid(ObjectMassBreakdown e)
        {
            double[] retVal = new double[e.ZLength * e.YLength * e.XLength];

            #region pre calculations

            // Only go through the octant with positive values.  Then copy the results to the other 7 octants

            bool isXEven = true;
            int xStart = e.XLength / 2;
            if (e.XLength % 2 == 1)
            {
                xStart++;
                isXEven = false;
            }

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

            // These tell how to convert the ellipse into a circle
            double objectSizeHalfX = e.ObjectSize.X / 2;
            double objectSizeHalfY = e.ObjectSize.Y / 2;
            double objectSizeHalfZ = e.ObjectSize.Z / 2;
            double maxRadius = Math1D.Max(objectSizeHalfX, objectSizeHalfY, objectSizeHalfZ);
            double ratioX = maxRadius / objectSizeHalfX;
            double ratioY = maxRadius / objectSizeHalfY;
            double ratioZ = maxRadius / objectSizeHalfZ;

            #endregion

            // This method handles cells that staddle the axiis (when isEven is false)
            GetMassBreakdown_Ellipsoid_Axis(retVal, xStart, yStart, zStart, isXEven, isYEven, isZEven, ratioX, ratioY, ratioZ, maxRadius, e);

            // This handles all the cells that are fully in the first octant, then copies that result to the other 7
            GetMassBreakdown_Ellipsoid_Octant(retVal, xStart, yStart, zStart, isXEven, isYEven, isZEven, ratioX, ratioY, ratioZ, maxRadius, e);

            return retVal;
        }
        private static void GetMassBreakdown_Ellipsoid_Octant(double[] masses, int xStart, int yStart, int zStart, bool isXEven, bool isYEven, bool isZEven, double ratioX, double ratioY, double ratioZ, double maxRadius, ObjectMassBreakdown e)
        {
            #region visualize

            //const double LINETHICK = .005;
            //const double DOTRAD = .02;

            //Debug3DWindow window1 = new Debug3DWindow()
            //{
            //    Title = "GetMassBreakdown_Ellipsoid 1",
            //};

            //Debug3DWindow window2 = new Debug3DWindow()
            //{
            //    Title = "GetMassBreakdown_Ellipsoid 2",
            //};

            //window1.AddAxisLines(maxRadius * 1.25, LINETHICK);
            //window2.AddAxisLines(maxRadius * 1.25, LINETHICK);

            ////for (int x = 0; x < e.XLength; x++)
            ////{
            ////    window.AddDot(new Point3D(x * e.CellSize, 0, 0), DOTRAD, UtilityWPF.ColorFromHex("866"));
            ////}

            ////for (int y = 0; y < e.YLength; y++)
            ////{
            ////    window.AddDot(new Point3D(0, y * e.CellSize, 0), DOTRAD, UtilityWPF.ColorFromHex("686"));
            ////}

            ////for (int z = 0; z < e.ZLength; z++)
            ////{
            ////    window.AddDot(new Point3D(0, 0, z * e.CellSize), DOTRAD, UtilityWPF.ColorFromHex("668"));
            ////}

            //window1.AddDot(new Point3D(0, 0, 0), maxRadius, UtilityWPF.ColorFromHex("80808080"), isHiRes: true);



            //var material = Debug3DWindow.GetMaterial(true, UtilityWPF.ColorFromHex("80808080"));

            //GeometryModel3D geometry = new GeometryModel3D();
            //geometry.Material = material;
            //geometry.BackMaterial = material;

            //geometry.Geometry = UtilityWPF.GetSphere_Ico(maxRadius, 3, true);

            //geometry.Transform = new ScaleTransform3D(1 / ratioX, 1 / ratioY, 1 / ratioZ);

            //ModelVisual3D visual = new ModelVisual3D();
            //visual.Content = geometry;
            //window2.Visuals3D.Add(visual);

            #endregion

            int[] xyzStart = new[] { xStart, yStart, zStart };
            bool[] isXYZEven = new[] { isXEven, isYEven, isZEven };

            // Loop through the x,y,z cells in only one octant, then just copy the result to the other 7 octants
            for (int z = zStart; z < e.ZLength; z++)
            {
                for (int y = yStart; y < e.YLength; y++)
                {
                    for (int x = xStart; x < e.XLength; x++)
                    {
                        // Figure out how much of the sphere is inside this cube
                        Vector3D cellMin = new Vector3D(
                            (e.AABBMin.X + (x * e.CellSize)) * ratioX,
                            (e.AABBMin.Y + (y * e.CellSize)) * ratioY,
                            (e.AABBMin.Z + (z * e.CellSize)) * ratioZ);

                        Vector3D cellMax = new Vector3D(
                            (e.AABBMin.X + ((x + 1) * e.CellSize)) * ratioX,
                            (e.AABBMin.Y + ((y + 1) * e.CellSize)) * ratioY,
                            (e.AABBMin.Z + ((z + 1) * e.CellSize)) * ratioZ);

                        ProcessCell_Sphere(masses, x, y, z, xyzStart, isXYZEven, cellMin, cellMax, maxRadius, e);

                        #region visualize

                        ////window.AddDot(new Point3D(0, 0, 0), maxRadius, UtilityWPF.ColorFromHex("80FFFFFF"), isHiRes: true);

                        //Color color = UtilityWPF.GetRandomColor(32, 0, 255);

                        //window1.AddMesh(
                        //    UtilityWPF.GetCube_IndependentFaces(min.ToPoint(), max.ToPoint()),
                        //    color);

                        //window2.AddMesh(
                        //    UtilityWPF.GetCube_IndependentFaces(new Point3D(min.X / ratioX, min.Y / ratioY, min.Z / ratioZ), new Point3D(max.X / ratioX, max.Y / ratioY, max.Z / ratioZ)),
                        //    color);

                        #endregion
                    }
                }
            }

            #region visualize

            //window1.Show();
            //window2.Show();

            #endregion
        }

        private static void GetMassBreakdown_Ellipsoid_Axis(double[] masses, int xStart, int yStart, int zStart, bool isXEven, bool isYEven, bool isZEven, double ratioX, double ratioY, double ratioZ, double maxRadius, ObjectMassBreakdown e)
        {
            if (isXEven && isYEven && isZEven)
            {
                return;
            }

            double offsetX = isXEven ? e.CellSize : e.CellSize / 2;
            double offsetY = isYEven ? e.CellSize : e.CellSize / 2;
            double offsetZ = isZEven ? e.CellSize : e.CellSize / 2;

            // The logic below needs to work with a standard sphere.  So stretch the cube into a rectangle.  Now it's a standard
            // sphere with a stretched cube instead of a standard cube with a stretched sphere
            //Point3D cellMin = new Point3D(0, 0, 0);
            Point3D cellMax = new Point3D(ratioX * offsetX, ratioY * offsetY, ratioZ * offsetZ);

            int[] xyzStart = new[] { xStart, yStart, zStart };
            int[] xyzLength = new[] { e.XLength, e.YLength, e.ZLength };
            VectorND xyzRatios = new VectorND(new[] { ratioX, ratioY, ratioZ });
            bool[] isEven = new[] { isXEven, isYEven, isZEven };

            // Origin
            AxisInfo_Sphere origin = new AxisInfo_Sphere()
            {
                Indices = new[] { Tuple.Create((isXEven ? xStart : xStart - 1), (isYEven ? yStart : yStart - 1), (isZEven ? zStart : zStart - 1)) },
                Cells = new[] { Tuple.Create(new Point3D(0, 0, 0), cellMax) },
            };

            int[] originIndex = new[]
            {
                origin.Indices[0].Item1,
                origin.Indices[0].Item2,
                origin.Indices[0].Item3,
            };

            // If an axis is odd, the other two need to be populated
            var axiis = new List<Tuple<Axis, AxisInfo_Sphere>>();
            var planes = new List<Tuple<Axis, Axis, AxisInfo_Sphere>>();

            if (!isXEven)
            {
                GetMassBreakdown_Ellipsoid_Axis_Add(Axis.Y, Axis.Z, cellMax, originIndex, xyzLength, xyzRatios, e.CellSize, isEven, axiis, planes);
            }

            if (!isYEven)
            {
                GetMassBreakdown_Ellipsoid_Axis_Add(Axis.X, Axis.Z, cellMax, originIndex, xyzLength, xyzRatios, e.CellSize, isEven, axiis, planes);
            }

            if (!isZEven)
            {
                GetMassBreakdown_Ellipsoid_Axis_Add(Axis.X, Axis.Y, cellMax, originIndex, xyzLength, xyzRatios, e.CellSize, isEven, axiis, planes);
            }

            #region visualize

            //const double AXISLEN = 3;
            //const double LINETHICK = .005;
            //const double DOTRAD = .02;

            //Debug3DWindow window = new Debug3DWindow()
            //{
            //    Title = "GetMassBreakdown_Ellipsoid_Axis_fillaxiis",
            //};

            //window.AddAxisLines(AXISLEN, LINETHICK);

            //window.AddDot(new Point3D(e.CellSize * ratioX, 0, 0), DOTRAD, UtilityWPF.ColorFromHex("844"));
            //window.AddDot(new Point3D(0, e.CellSize * ratioY, 0), DOTRAD, UtilityWPF.ColorFromHex("484"));
            //window.AddDot(new Point3D(0, 0, e.CellSize * ratioZ), DOTRAD, UtilityWPF.ColorFromHex("448"));

            //window.AddText(string.Format("isEven: {0}, {1}, {2}", isXEven, isYEven, isZEven));


            //// Show cubes
            //window.AddMesh(UtilityWPF.GetCube_IndependentFaces(origin.Cells[0].Item1, origin.Cells[0].Item2), UtilityWPF.ColorFromHex("60444444"));

            //window.AddText(string.Format("xyzStart {0}, {1}, {2}", xStart, yStart, zStart));

            //window.AddText(string.Format("Origin Index = {0}", origin.Indices[0].ToString()));

            //var getColor = new Func<Axis, Color>(a =>
            //{
            //    switch (a)
            //    {
            //        case Axis.X: return UtilityWPF.ColorFromHex("60804040");
            //        case Axis.Y: return UtilityWPF.ColorFromHex("60408040");
            //        case Axis.Z: return UtilityWPF.ColorFromHex("60404080");

            //        default:
            //            throw new ApplicationException("Unknown Axis: " + a.ToString());
            //    }
            //});

            //foreach (var axis in axiis.OrderBy(o => o.Item1))
            //{
            //    Color color = getColor(axis.Item1);

            //    foreach (var cell in axis.Item2.Cells)
            //    {
            //        window.AddMesh(UtilityWPF.GetCube_IndependentFaces(cell.Item1, cell.Item2), UtilityWPF.GetRandomColor(color.A, color.R, color.G, color.B, 8));
            //    }

            //    window.AddText(string.Format("Axis {0} indices {1}", axis.Item1, axis.Item2.Indices.Select(o => o.ToString()).ToJoin(" | ")));
            //}

            //foreach (var plane in planes.OrderBy(o => o.Item1).ThenBy(o => o.Item2))
            //{
            //    byte[] color = new byte[] { 64, 64, 64 };
            //    color[GetAxisIndex(plane.Item1)] = 128;
            //    color[GetAxisIndex(plane.Item2)] = 128;

            //    foreach (var cell in plane.Item3.Cells)
            //    {
            //        window.AddMesh(UtilityWPF.GetCube_IndependentFaces(cell.Item1, cell.Item2), UtilityWPF.GetRandomColor(96, color[0], color[1], color[2], 8));
            //    }

            //    window.AddText(string.Format("Axis {0}, {1} indices {2}", plane.Item1, plane.Item2, plane.Item3.Indices.Select(o => o.ToString()).ToJoin(" | ")));
            //}

            //window.AddDot(new Point3D(0, 0, 0), maxRadius, UtilityWPF.ColorFromHex("08808080"), isHiRes: true);

            //window.Show();

            #endregion

            var iterateCells = UtilityCore.Iterate<AxisInfo_Sphere>(origin, axiis.Select(o => o.Item2), planes.Select(o => o.Item3)).
                SelectMany(o => Enumerable.Range(0, o.Cells.Length).Select(p => new
                {
                    X = o.Indices[p].Item1,
                    Y = o.Indices[p].Item2,
                    Z = o.Indices[p].Item3,
                    CellMin = o.Cells[p].Item1.ToVector(),
                    CellMax = o.Cells[p].Item2.ToVector(),
                }));

            foreach (var axisCell in iterateCells)
            {
                ProcessCell_Sphere(masses, axisCell.X, axisCell.Y, axisCell.Z, xyzStart, isEven, axisCell.CellMin, axisCell.CellMax, /*axisCell.VolumeMultiplier,*/ maxRadius, e);
            }
        }
        private static void GetMassBreakdown_Ellipsoid_Axis_Add(Axis axis1, Axis axis2, Point3D cellMax, int[] originIndex, int[] xyzLength, VectorND xyzRatios, double cellSize, bool[] isEven, List<Tuple<Axis, AxisInfo_Sphere>> axiis, List<Tuple<Axis, Axis, AxisInfo_Sphere>> planes)
        {
            #region axiis

            foreach (Axis axis in new[] { axis1, axis2 })
            {
                if (!axiis.Any(o => o.Item1 == axis))
                {
                    int index = GetAxisIndex(axis);

                    IEnumerable<int> rangeIterator = Enumerable.Range(originIndex[index], xyzLength[index] - originIndex[index] - 1);

                    axiis.Add(new Tuple<Axis, AxisInfo_Sphere>(axis,
                        new AxisInfo_Sphere()
                        {
                            Cells = rangeIterator.
                                Select(o =>
                                {
                                    VectorND cellPosMin = new VectorND(3);
                                    VectorND cellPosMax = cellMax.ToVectorND();

                                    AdjustSpherePosition(cellPosMin, cellPosMax, axis, o, cellSize, xyzRatios, cellMax, originIndex);

                                    return Tuple.Create(cellPosMin.ToPoint3D(), cellPosMax.ToPoint3D());
                                }).
                                ToArray(),

                            Indices = rangeIterator.
                                Select(o =>
                                {
                                    int[] cellIndex = originIndex.ToArray();

                                    cellIndex[index] = o + 1;

                                    return Tuple.Create(cellIndex[0], cellIndex[1], cellIndex[2]);
                                }).
                                ToArray(),
                        }));
                }
            }

            #endregion

            #region plane

            int index1 = GetAxisIndex(axis1);
            int index2 = GetAxisIndex(axis2);

            var iteratator = UtilityCore.Collate(
                Enumerable.Range(originIndex[index1], xyzLength[index1] - originIndex[index1] - 1),
                Enumerable.Range(originIndex[index2], xyzLength[index2] - originIndex[index2] - 1));

            planes.Add(new Tuple<Axis, Axis, AxisInfo_Sphere>(axis1, axis2,
                new AxisInfo_Sphere()
                {
                    Cells = iteratator.
                        Select(o =>
                        {
                            VectorND cellPosMin = new VectorND(3);
                            VectorND cellPosMax = cellMax.ToVectorND();

                            AdjustSpherePosition(cellPosMin, cellPosMax, axis1, o.Item1, cellSize, xyzRatios, cellMax, originIndex);
                            AdjustSpherePosition(cellPosMin, cellPosMax, axis2, o.Item2, cellSize, xyzRatios, cellMax, originIndex);

                            return Tuple.Create(cellPosMin.ToPoint3D(), cellPosMax.ToPoint3D());
                        }).
                        ToArray(),

                    Indices = iteratator.
                        Select(o =>
                        {
                            int[] cellIndex = originIndex.ToArray();

                            cellIndex[index1] = o.Item1 + 1;
                            cellIndex[index2] = o.Item2 + 1;

                            return Tuple.Create(cellIndex[0], cellIndex[1], cellIndex[2]);
                        }).
                        ToArray(),
                }));

            #endregion
        }

        private static int GetAxisIndex(Axis axis)
        {
            switch (axis)
            {
                case Axis.X: return 0;
                case Axis.Y: return 1;
                case Axis.Z: return 2;

                default:
                    throw new ApplicationException("Unknown Axis: " + axis.ToString());
            }
        }

        private static void AdjustSpherePosition(VectorND cellPosMin, VectorND cellPosMax, Axis axis, int iteration, double cellSize, VectorND xyzRatios, Point3D cellMax, int[] xyzStart)
        {
            int index = GetAxisIndex(axis);

            double minSubtract = (cellSize * xyzRatios[index]) - cellPosMax[index];

            double offset = (iteration - xyzStart[index] + 1) * (cellSize * xyzRatios[index]);

            cellPosMin[index] += offset - minSubtract;        // the max is fine, but min needs to be subtracted by half if it's odd along that axis
            cellPosMax[index] += offset;
        }

        private static void ProcessCell_Sphere(double[] masses, int x, int y, int z, int[] xyzStart, bool[] isXYZEven, Vector3D cellMin, Vector3D cellMax, double maxRadius, ObjectMassBreakdown e)
        {
            if (cellMin.LengthSquared > maxRadius * maxRadius)
            {
                // This cell is completely outside the sphere.  Leave the mass as zero
                return;
            }

            double volume = e.CellSize * e.CellSize * e.CellSize;

            if (cellMax.LengthSquared > maxRadius * maxRadius)
            {
                // This cell is clipped by the sphere.  Figure out which faces the sphere is intersecting
                double? volumePercent = GetVolumePercentIntersectedCube(cellMin.ToPoint(), cellMax.ToPoint(), maxRadius);
                volume *= volumePercent ?? 0d;
            }

            // Figure out what the coords are in the other quadrants (when the length of the axis odd, start needs to be skipped over)
            //NOTE: If x y or z are on the axis, then alt will also be that axis.  The same cell will be populated multiple times with the same value,
            //but it saves on a bunch of if statements
            int altX = x;
            if (x >= xyzStart[0])
                altX = xyzStart[0] - (x + (isXYZEven[0] ? 1 : 2) - xyzStart[0]);

            int altY = y;
            if (y >= xyzStart[1])
                altY = xyzStart[1] - (y + (isXYZEven[1] ? 1 : 2) - xyzStart[1]);

            int altZ = z;
            if (z >= xyzStart[2])
                altZ = xyzStart[2] - (z + (isXYZEven[2] ? 1 : 2) - xyzStart[2]);

            masses[e.GetIndex(x, y, z)] = volume;
            masses[e.GetIndex(x, altY, z)] = volume;
            masses[e.GetIndex(x, y, altZ)] = volume;
            masses[e.GetIndex(x, altY, altZ)] = volume;

            masses[e.GetIndex(altX, y, z)] = volume;
            masses[e.GetIndex(altX, altY, z)] = volume;
            masses[e.GetIndex(altX, y, altZ)] = volume;
            masses[e.GetIndex(altX, altY, altZ)] = volume;
        }

        private static double? GetVolumePercentIntersectedCube(Point3D cellMin, Point3D cellMax, double maxRadius)
        {
            if (cellMin.ToVector().LengthSquared > maxRadius * maxRadius)
            {
                return null;
            }

            #region cube faces

            // Convert the cube into 6 faces (using the transformed points)

            Point3D[] cellPoints = new[]
            {
                new Point3D(cellMin.X, cellMin.Y, cellMin.Z),     // 0

                new Point3D(cellMax.X, cellMin.Y, cellMin.Z),     // 1
                new Point3D(cellMin.X, cellMax.Y, cellMin.Z),     // 2
                new Point3D(cellMin.X, cellMin.Y, cellMax.Z),     // 3

                new Point3D(cellMin.X, cellMax.Y, cellMax.Z),     // 4
                new Point3D(cellMax.X, cellMin.Y, cellMax.Z),     // 5
                new Point3D(cellMax.X, cellMax.Y, cellMin.Z),     // 6

                new Point3D(cellMax.X, cellMax.Y, cellMax.Z),     // 7
            };

            Edge3D[] scaledCubeEdges = new[]
            {
                new Edge3D(0, 1, cellPoints),     // 0
                new Edge3D(1, 5, cellPoints),     // 1
                new Edge3D(3, 5 ,cellPoints),     // 2
                new Edge3D(0, 3, cellPoints),     // 3

                new Edge3D(2, 6, cellPoints),     // 4
                new Edge3D(6, 7, cellPoints),     // 5
                new Edge3D(4, 7, cellPoints),     // 6
                new Edge3D(2, 4, cellPoints),     // 7

                new Edge3D(0, 2, cellPoints),     // 8
                new Edge3D(3, 4, cellPoints),     // 9
                new Edge3D(5, 7, cellPoints),     // 10
                new Edge3D(1, 6, cellPoints),     // 11
            };

            Face3D[] cellFaces = new[]
            {
                new Face3D(new[] { 0, 8, 4, 11 }, scaledCubeEdges),       // 0
                new Face3D(new[] { 0, 1, 2, 3 }, scaledCubeEdges),       // 1
                new Face3D(new[] { 3, 9, 7, 8 }, scaledCubeEdges),       // 2
                new Face3D(new[] { 5, 10, 1, 11 }, scaledCubeEdges),       // 3
                new Face3D(new[] { 4, 7, 6, 5 }, scaledCubeEdges),       // 4
                new Face3D(new[] { 6, 9, 2, 10 }, scaledCubeEdges),       // 5
            };

            #endregion

            #region visualize

            //foreach (Face3D face in scaledCubeFaces)
            //{
            //    VisualizeFaceCircleIntercept_rotation(face, new Point3D(0, 0, 0), maxRadius);
            //}

            #endregion

            Point3D[][] polys = cellFaces.
                Select(o => Math3D.GetIntersection_Face_Sphere(o, new Point3D(0, 0, 0), maxRadius)).
                ToArray();

            var hull = Math3D.GetConvexHull(polys.SelectMany(o => o).ToArray());
            if (hull == null)
            {
                #region visualize

                //Point3D[] polyPoints = polys.
                //    SelectMany(o => o).
                //    ToArray();

                //if (polyPoints.Length > 1)
                //{
                //    Debug3DWindow window = new Debug3DWindow()
                //    {
                //        Title = "Couldn't generate hull",
                //        Background = new SolidColorBrush(UtilityWPF.ColorFromHex("A77")),
                //    };

                //    window.AddDots(polyPoints, .02, Colors.Red);

                //    window.Show();
                //}

                #endregion
                return null;        // this should never happen
            }

            double volumeFull = (cellMax.X - cellMin.X) * (cellMax.Y - cellMin.Y) * (cellMax.Z - cellMin.Z);
            double volume = Math3D.GetVolume_ConvexHull(hull);
            if (volume > volumeFull)
            {
                volume = .5;     // this should never happen
            }

            #region visualize

            //VisualizeSphereCubeRatios(scaledCubeFaces, maxRadius, ratioX, ratioY, ratioZ);

            //VisualizeSphereCubeIntersect(scaledCubeFaces, maxRadius, polys);
            //VisualizeIntersectedHull(polys, hull, volumeScaled, fullVolumeScaled);

            #endregion

            return volume / volumeFull;
        }

        private static void VisualizeFaceCircleIntercept_rotation(Face3D face, Point3D sphereCenter, double sphereRadius)
        {
            const double AXISLEN = 3;
            const double LINETHICK = .01;
            const double DOTRAD = .05;

            var window = new Debug3DWindow();

            window.AddAxisLines(AXISLEN, LINETHICK);

            #region orig plane

            ITriangle plane = face.GetPlane();

            Point3D[] polyPoints = face.GetPolygonPoints();

            // Orig
            window.AddDots(plane.PointArray, DOTRAD * 1.1, UtilityWPF.ColorFromHex("FF0000"));
            window.AddPlane(plane, AXISLEN, UtilityWPF.ColorFromHex("FF0000"));

            #endregion
            #region transformed 2D

            // Transformed (I think this is failing because the vectors are along the x,y,z planes)
            var transform2D = Math2D.GetTransformTo2D(plane);

            var transformed = polyPoints.
                Select(o => transform2D.Item1.Transform(o).ToPoint2D().ToPoint3D()).
                //Select(o => transform2D.Item1.Transform(o)).
                ToArray();

            ITriangle transformedPlane = new Triangle(transformed[0], transformed[1], transformed[2]);

            window.AddDots(transformed, DOTRAD, UtilityWPF.ColorFromHex("FF7070"));
            window.AddPlane(transformedPlane, AXISLEN, UtilityWPF.ColorFromHex("FF4040"), UtilityWPF.ColorFromHex("FFC0C0"));

            #endregion
            #region transformed back 3D

            // Transformed back
            var transformedBack = transformed.
                Select(o => transform2D.Item2.Transform(o)).
                ToArray();

            window.AddDots(transformedBack, DOTRAD, UtilityWPF.ColorFromHex("FFC0C0"));
            window.AddPlane(new Triangle(transformedBack[0], transformedBack[1], transformedBack[2]), AXISLEN, UtilityWPF.ColorFromHex("FFC0C0"));

            #endregion
            #region transformed back 3D from actual 2D

            var transformedBack2 = transformed.
                Select(o => o.ToPoint2D().ToPoint3D()).
                Select(o => transform2D.Item2.Transform(o)).
                ToArray();

            window.AddDots(transformedBack2, DOTRAD, UtilityWPF.ColorFromHex("EEE"));
            window.AddPlane(new Triangle(transformedBack2[0], transformedBack2[1], transformedBack2[2]), AXISLEN, UtilityWPF.ColorFromHex("EEE"));

            #endregion

            #region analyze transform

            //Vector3D line1 = plane.Point1 - plane.Point0;
            //Vector3D randomOrth = Math3D.GetOrthogonal(line1, plane.Point2 - plane.Point0);

            //window.AddLine(new Point3D(0, 0, 0), line1.ToPoint(), LINETHICK * 2, UtilityWPF.ColorFromHex("B0B0FF"));
            //window.AddLine(new Point3D(0, 0, 0), randomOrth.ToPoint(), LINETHICK * 2, UtilityWPF.ColorFromHex("5050FF"));


            //DoubleVector from = new DoubleVector(line1, randomOrth);
            //DoubleVector to = new DoubleVector(new Vector3D(1, 0, 0), new Vector3D(0, 1, 0));

            ////Quaternion rotation = from.GetRotation(to);
            //Quaternion rotation = GetRotation_custom(window, from.Standard, from.Orth, to.Standard, to.Orth);

            #endregion

            #region window background

            if (Math.Abs(Vector3D.DotProduct(transformedPlane.NormalUnit, new Vector3D(0, 0, 1))).IsNearValue(1d))
            {
                if (Enumerable.Range(0, polyPoints.Length).All(o => polyPoints[o].IsNearValue(transformedBack[o]) && polyPoints[o].IsNearValue(transformedBack2[o])))
                {
                    window.Background = new SolidColorBrush(UtilityWPF.ColorFromHex("788878"));
                }
                else
                {
                    window.Background = new SolidColorBrush(UtilityWPF.ColorFromHex("787888"));

                    string planeDump = plane.PointArray.
                        Select(o => o.ToString()).
                        ToJoin("\r\n");

                    string polyDump = polyPoints.
                        Select(o => o.ToString()).
                        ToJoin("\r\n");

                    window.Messages_Bottom.Add(new System.Windows.Controls.TextBox()
                    {
                        AcceptsReturn = true,
                        Background = Brushes.Transparent,
                        Foreground = Brushes.White,
                        Text = string.Format("plane:\r\n{0}\r\n\r\npoly:\r\n{1}", planeDump, polyDump),
                    });
                }
            }
            else
            {
                window.Background = new SolidColorBrush(UtilityWPF.ColorFromHex("887878"));
            }

            #endregion

            window.Show();
        }
        private static void VisualizeSphereCubeIntersect(Face3D[] scaledCubeFaces, double sphereRadius, Point3D[][] polys)
        {
            const double AXISLEN = 3;
            const double LINETHICK = .005;
            const double DOTRAD = .02;

            var window = new Debug3DWindow();

            window.AddAxisLines(AXISLEN, LINETHICK);

            Point3D[] facePoints = scaledCubeFaces.
                SelectMany(o => o.GetPolygonPoints()).
                ToArray();

            Point3D[] polyPoints = Math3D.GetUnique(polys.SelectMany(o => o));

            var edges = Edge3D.GetUniqueLines(scaledCubeFaces.SelectMany(o => o.Edges).ToArray());

            window.AddDots(facePoints, DOTRAD * .75, UtilityWPF.ColorFromHex("000"));
            window.AddDots(polyPoints, DOTRAD, UtilityWPF.ColorFromHex("FFF"));

            window.AddLines(edges.Select(o => Tuple.Create(o.Point0, o.Point1Ext)), LINETHICK, UtilityWPF.ColorFromHex("000"));

            window.AddDot(new Point3D(0, 0, 0), sphereRadius, UtilityWPF.ColorFromHex("30EEEEEE"), isHiRes: true);

            window.Show();
        }
        private static void VisualizeSphereCubeRatios(Face3D[] scaledCubeFaces, double sphereRadius, double ratioX, double ratioY, double ratioZ)
        {
            const double AXISLEN = 3;
            const double LINETHICK = .005;
            const double DOTRAD = .02;

            string id = Guid.NewGuid().ToString();

            #region sphere

            var window = new Debug3DWindow()
            {
                Title = id,
                Background = new SolidColorBrush(UtilityWPF.ColorFromHex("BAA")),
            };

            window.AddAxisLines(AXISLEN, LINETHICK);

            Point3D[] facePoints = scaledCubeFaces.
                SelectMany(o => o.GetPolygonPoints()).
                ToArray();

            var edges = Edge3D.GetUniqueLines(scaledCubeFaces.SelectMany(o => o.Edges).ToArray());

            window.AddDots(facePoints, DOTRAD * .75, UtilityWPF.ColorFromHex("000"));

            window.AddLines(edges.Select(o => Tuple.Create(o.Point0, o.Point1Ext)), LINETHICK, UtilityWPF.ColorFromHex("000"));

            window.AddDot(new Point3D(0, 0, 0), sphereRadius, UtilityWPF.ColorFromHex("30EEEEEE"), isHiRes: true);

            window.Show();

            #endregion
            #region ellipsoid

            Transform3D unscaleTransform = new ScaleTransform3D(1 / ratioX, 1 / ratioY, 1 / ratioZ);

            Point3D[] unscaledPoints = scaledCubeFaces[0].AllEdges[0].AllEdgePoints.
                Select(o => unscaleTransform.Transform(o)).
                ToArray();

            Edge3D[] unscaledEdges = scaledCubeFaces[0].AllEdges.
                Select(o => new Edge3D(o.Index0, o.Index1.Value, unscaledPoints)).
                ToArray();

            Face3D[] unscaledCubeFaces = scaledCubeFaces.
                Select(o => new Face3D(o.EdgeIndices, unscaledEdges)).
                ToArray();

            window = new Debug3DWindow()
            {
                Title = id,
                Background = new SolidColorBrush(UtilityWPF.ColorFromHex("AAB")),
            };

            window.AddAxisLines(AXISLEN, LINETHICK);

            facePoints = unscaledCubeFaces.
                SelectMany(o => o.GetPolygonPoints()).
                ToArray();

            edges = Edge3D.GetUniqueLines(unscaledCubeFaces.SelectMany(o => o.Edges).ToArray());

            window.AddDots(facePoints, DOTRAD * .75, UtilityWPF.ColorFromHex("000"));

            window.AddLines(edges.Select(o => Tuple.Create(o.Point0, o.Point1Ext)), LINETHICK, UtilityWPF.ColorFromHex("000"));

            window.AddEllipse(new Point3D(0, 0, 0), new Vector3D(sphereRadius / ratioX, sphereRadius / ratioY, sphereRadius / ratioZ), UtilityWPF.ColorFromHex("30EEEEEE"), isHiRes: true);

            window.Show();

            #endregion
        }
        private static void VisualizeIntersectedHull(Point3D[][] polys, TriangleIndexed[] hull, double polyVolume, double cellVolume)
        {
            const double AXISLEN = 3;
            const double LINETHICK = .005;
            const double DOTRAD = .02;

            var window = new Debug3DWindow()
            {
                Background = new SolidColorBrush(UtilityWPF.ColorFromHex("D4B9A7")),
            };

            window.AddAxisLines(AXISLEN, LINETHICK);

            Point3D[] points = Math3D.GetUnique(polys.SelectMany(o => o));

            window.AddDots(points, DOTRAD, UtilityWPF.ColorFromHex("FFF"));

            //window.AddHull(hull, UtilityWPF.ColorFromHex("40FF9775"), UtilityWPF.ColorFromHex("FFE2D9"), LINETHICK);      // the lines are distracting
            window.AddHull(hull, UtilityWPF.ColorFromHex("40FF9775"));

            window.AddMessage(string.Format("poly\t{0}", polyVolume));
            window.AddMessage(string.Format("cell\t{0}", cellVolume));
            window.AddMessage(string.Format("percent\t{0}", polyVolume / cellVolume));

            window.Show();
        }

        #endregion
        #region Private Methods

        private static double[] GetMassBreakdown_Box2D(ObjectMassBreakdown e)
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
                        // Calculate the positions of the 4 corners
                        Point3D min = new Point3D(e.AABBMin.X + (x * e.CellSize), e.AABBMin.Y + (y * e.CellSize), e.AABBMin.Z + (z * e.CellSize));
                        Point3D max = new Point3D(min.X + e.CellSize, min.Y + e.CellSize, min.Z + e.CellSize);

                        // If the the cell is completly inside the object, then give it a mass of one.  Otherwise some percent of that

                        double xAmt = GetMassBreakdown_Box2D_Axis(Math.Abs(min.X), Math.Abs(max.X), e.ObjectSize.X, halfObjSize.X, e.CellSize);
                        double yAmt = GetMassBreakdown_Box2D_Axis(Math.Abs(min.Y), Math.Abs(max.Y), e.ObjectSize.Y, halfObjSize.Y, e.CellSize);
                        double zAmt = GetMassBreakdown_Box2D_Axis(Math.Abs(min.Z), Math.Abs(max.Z), e.ObjectSize.Z, halfObjSize.Z, e.CellSize);

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
        private static double GetMassBreakdown_Box2D_Axis(double absMin, double absMax, double size, double halfSize, double cellSize)
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
                // This cell is larger than the object
                return size;
            }
            else if (min > halfSize)
            {
                return 0d;		// this should never happen
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

        private static double[] GetMassBreakdown_Box3D(ObjectMassBreakdown e)
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

            // Along X, +-Y, +-Z
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

            // Along Y, +-X, +-Z
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

            // Along Z, +-X, +-Y
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

            // +-X
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

            // +-Y
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

            // +-Z
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

        private static void GetMassBreakdown_Template(out Point3D centerMass, out Point3D[] centers, out double[] masses, ObjectMassBreakdown e)
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
                        // Calculate the positions of the 4 corners
                        Point3D min = new Point3D(e.AABBMin.X + (x * e.CellSize), e.AABBMin.Y + (y * e.CellSize), e.AABBMin.Z + (z * e.CellSize));
                        Point3D max = new Point3D(min.X + e.CellSize, min.Y + e.CellSize, min.Z + e.CellSize);
                        centers[zOffset + yOffset + x] = new Point3D(min.X + halfSize, min.Y + halfSize, min.Z + halfSize);

                        // If the the cell is completly inside the object, then give it a mass of one.  Otherwise some percent of that





                    }
                }
            }
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
                        // Calculate the positions of the 4 corners
                        //Point3D min = new Point3D(e.AABBMin.X + (x * e.CellSize), e.AABBMin.Y + (y * e.CellSize), e.AABBMin.Z + (z * e.CellSize));
                        //Point3D max = new Point3D(min.X + e.CellSize, min.Y + e.CellSize, min.Z + e.CellSize);

                        // This is halfway between min and max
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
            // Figure out how big to make the return (it will be an even multiple of the cell size)
            int xLen = Convert.ToInt32(Math.Ceiling(size.X / cellSize));
            int yLen = Convert.ToInt32(Math.Ceiling(size.Y / cellSize));
            int zLen = Convert.ToInt32(Math.Ceiling(size.Z / cellSize));

            Point3D aabbMax = new Point3D((cellSize * xLen) / 2d, (cellSize * yLen) / 2d, (cellSize * zLen) / 2d);
            Point3D aabbMin = new Point3D(-aabbMax.X, -aabbMax.Y, -aabbMax.Z);      // it's centered at zero, so min is just max negated

            // Build the returns
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

            double mult = 1d / total;

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

            double mult = 1d / total;       // multiplication is cheaper than division, so just do the division once

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

        /// <summary>
        /// This calculates the moment of inertia of the body around the axis (the axis goes through the center of mass)
        /// </summary>
        /// <remarks>
        /// Inertia of a body is the sum of all the mr^2
        /// 
        /// Each cell of the mass breakdown needs to be thought of as a sphere.  If it were a point mass, then for a body with only one
        /// cell, the mass would be at the center, and it would have an inertia of zero.  So by using the parallel axis theorem on each cell,
        /// the returned inertia is accurate.  The reason they need to thought of as spheres instead of cubes, is because the inertia is the
        /// same through any axis of a sphere, but not for a cube.
        /// 
        /// So sphereMultiplier needs to be 2/5 * cellRadius^2
        /// </remarks>
        private static double GetInertia(UtilityNewt.IObjectMassBreakdown body, Vector3D axis, double sphereMultiplier)
        {
            double retVal = 0d;

            // Cache this point in case the property call is somewhat expensive
            Point3D center = body.CenterMass;

            foreach (var pointMass in body)
            {
                if (pointMass.Item2 == 0d)
                {
                    continue;
                }

                // Tack on the inertia of the cell sphere (2/5*mr^2)
                retVal += pointMass.Item2 * sphereMultiplier;

                // Get the distance between this point and the axis
                double distance = Math3D.GetClosestDistance_Line_Point(body.CenterMass, axis, pointMass.Item1);

                // Now tack on the md^2
                retVal += pointMass.Item2 * distance * distance;
            }

            // Exit Function
            return retVal;
        }
        /// <summary>
        /// This returns the inertia of the part relative to the ship's axis
        /// NOTE: The other overload takes a vector that was transformed into the part's model coords.  The vector passed to this overload is in ship's model coords
        /// </summary>
        private static double GetInertia(Point3D partCenter, double partInertia, double partMass, Point3D shipCenterMass, Vector3D axis)
        {
            // Start with the inertia of the part around the axis passed in
            double retVal = partInertia;

            // Get the distance between the part and the axis
            double distance = Math3D.GetClosestDistance_Line_Point(shipCenterMass, axis, partCenter);

            // Now tack on the md^2
            retVal += partMass * distance * distance;

            // Exit Function
            return retVal;
        }

        #endregion
    }
}
