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
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;

namespace Game.HelperClassesWPF
{
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
        #region Class: EvenDistribution

        /// <summary>
        /// This class has several methods that have a similar behavior.  The method will place random points, then shift them around until a
        /// threshold is met
        /// </summary>
        /// <remarks>
        /// NOTE: The code was copied from the EvenDistributionSphere tester
        /// TODO: Make another one that is SphericalShell (store the repulsive forces as quaternions)
        /// 
        /// There's a bit too much custom logic to put directly into Math3D, but I want it to appear from the outside that Math3D is doing
        /// all the work (so that consumers have just one util class to go to)
        /// </remarks>
        private class EvenDistribution
        {
            #region Class: Dot

            private class Dot
            {
                public Dot(bool isStatic, Vector3D position, double repulseMultiplier)
                {
                    this.IsStatic = isStatic;
                    this.Position = position;
                    this.RepulseMultiplier = repulseMultiplier;
                }

                public readonly bool IsStatic;
                public Vector3D Position;
                public readonly double RepulseMultiplier;
            }

            #endregion

            // These return points that are evenly distributed
            public static Vector3D[] GetSpherical(int returnCount, Vector3D[] existingStaticPoints, double radius, double stopRadiusPercent, int stopIterationCount, double[] movableRepulseMultipliers, double[] staticRepulseMultipliers)
            {
                // Start with randomly placed dots
                Dot[] dots = GetDots(returnCount, existingStaticPoints, radius, movableRepulseMultipliers, staticRepulseMultipliers, true);

                return GetSpherical_Finish(dots, returnCount, radius, stopRadiusPercent, stopIterationCount);
            }
            public static Vector3D[] GetSpherical(Vector3D[] movable, Vector3D[] existingStaticPoints, double radius, double stopRadiusPercent, int stopIterationCount, double[] movableRepulseMultipliers, double[] staticRepulseMultipliers)
            {
                Dot[] dots = GetDots(movable, existingStaticPoints, radius, movableRepulseMultipliers, staticRepulseMultipliers);

                return GetSpherical_Finish(dots, movable.Length, radius, stopRadiusPercent, stopIterationCount);
            }

            // These return points that are randomly clustered, but points won't be closer than a certain distance
            public static Vector3D[] GetSpherical_ClusteredMinDist(int returnCount, Vector3D[] existingStaticPoints, double radius, double minDist, double stopRadiusPercent, int stopIterationCount, double[] movableRepulseMultipliers, double[] staticRepulseMultipliers)
            {
                // Start with randomly placed dots
                Dot[] dots = GetDots(returnCount, existingStaticPoints, radius, movableRepulseMultipliers, staticRepulseMultipliers, true);

                return GetSpherical_ClusteredMinDist_Finish(dots, returnCount, radius, minDist, stopRadiusPercent, stopIterationCount);
            }
            public static Vector3D[] GetSpherical_ClusteredMinDist(Vector3D[] movable, Vector3D[] existingStaticPoints, double radius, double minDist, double stopRadiusPercent, int stopIterationCount, double[] movableRepulseMultipliers, double[] staticRepulseMultipliers)
            {
                Dot[] dots = GetDots(movable, existingStaticPoints, radius, movableRepulseMultipliers, staticRepulseMultipliers);

                return GetSpherical_ClusteredMinDist_Finish(dots, movable.Length, radius, minDist, stopRadiusPercent, stopIterationCount);
            }

            public static Vector3D[] GetSphericalShell(int returnCount, Vector3D[] existingStaticPoints, double radius, double stopRadiusPercent, int stopIterationCount, double[] movableRepulseMultipliers, double[] staticRepulseMultipliers)
            {
                // Start with randomly placed dots
                Dot[] dots = GetDots(returnCount, existingStaticPoints, radius, movableRepulseMultipliers, staticRepulseMultipliers, true);

                return GetSphericalShell_Finish(dots, returnCount, radius, stopRadiusPercent, stopIterationCount);
            }
            public static Vector3D[] GetSphericalShell(Vector3D[] movable, Vector3D[] existingStaticPoints, double radius, double stopRadiusPercent, int stopIterationCount, double[] movableRepulseMultipliers, double[] staticRepulseMultipliers)
            {
                Dot[] dots = GetDots(movable, existingStaticPoints, radius, movableRepulseMultipliers, staticRepulseMultipliers);

                return GetSphericalShell_Finish(dots, movable.Length, radius, stopRadiusPercent, stopIterationCount);
            }

            public static Vector[] GetCircular(int returnCount, Vector[] existingStaticPoints, double radius, double stopRadiusPercent, int stopIterationCount, double[] movableRepulseMultipliers, double[] staticRepulseMultipliers)
            {
                // Convert to 3D
                Vector3D[] existingStaticCast = null;
                if (existingStaticPoints != null)
                {
                    existingStaticCast = existingStaticPoints.Select(o => new Vector3D(o.X, o.Y, 0)).ToArray();
                }

                // Start with randomly placed dots
                Dot[] dots = GetDots(returnCount, existingStaticCast, radius, movableRepulseMultipliers, staticRepulseMultipliers, false);

                Vector3D[] retVal = GetCircular_Finish(dots, returnCount, radius, stopRadiusPercent, stopIterationCount);

                // Convert to 2D
                return retVal.Select(o => new Vector(o.X, o.Y)).ToArray();
            }
            public static Vector[] GetCircular(Vector[] movable, Vector[] existingStaticPoints, double radius, double stopRadiusPercent, int stopIterationCount, double[] movableRepulseMultipliers, double[] staticRepulseMultipliers)
            {
                // Convert to 3D
                Vector3D[] movableCast = null;
                if (movable != null)
                {
                    movableCast = movable.Select(o => new Vector3D(o.X, o.Y, 0)).ToArray();
                }

                Vector3D[] existingStaticCast = null;
                if (existingStaticPoints != null)
                {
                    existingStaticCast = existingStaticPoints.Select(o => new Vector3D(o.X, o.Y, 0)).ToArray();
                }

                // Convert to dots
                Dot[] dots = GetDots(movableCast, existingStaticCast, radius, movableRepulseMultipliers, staticRepulseMultipliers);

                Vector3D[] retVal = GetCircular_Finish(dots, movable.Length, radius, stopRadiusPercent, stopIterationCount);

                // Convert to 2D
                return retVal.Select(o => new Vector(o.X, o.Y)).ToArray();
            }

            public static Vector[] GetCircular_CenterPacked(int returnCount, Vector[] existingStaticPoints, double radius, double stopRadiusPercent, int stopIterationCount, double[] movableRepulseMultipliers, double[] staticRepulseMultipliers)
            {
                // Convert to 3D
                Vector3D[] existingStaticCast = null;
                if (existingStaticPoints != null)
                {
                    existingStaticCast = existingStaticPoints.Select(o => new Vector3D(o.X, o.Y, 0)).ToArray();
                }

                // Start with randomly placed dots
                Dot[] dots = GetDots(returnCount, existingStaticCast, radius, movableRepulseMultipliers, staticRepulseMultipliers, false);

                Vector3D[] retVal = GetCircular_CenterPacked_Finish(dots, returnCount, radius, stopRadiusPercent, stopIterationCount);

                // Convert to 2D
                return retVal.Select(o => new Vector(o.X, o.Y)).ToArray();
            }
            public static Vector[] GetCircular_CenterPacked(Vector[] movable, Vector[] existingStaticPoints, double radius, double stopRadiusPercent, int stopIterationCount, double[] movableRepulseMultipliers, double[] staticRepulseMultipliers)
            {
                // Convert to 3D
                Vector3D[] movableCast = null;
                if (movable != null)
                {
                    movableCast = movable.Select(o => new Vector3D(o.X, o.Y, 0)).ToArray();
                }

                Vector3D[] existingStaticCast = null;
                if (existingStaticPoints != null)
                {
                    existingStaticCast = existingStaticPoints.Select(o => new Vector3D(o.X, o.Y, 0)).ToArray();
                }

                // Convert to dots
                Dot[] dots = GetDots(movableCast, existingStaticCast, radius, movableRepulseMultipliers, staticRepulseMultipliers);

                Vector3D[] retVal = GetCircular_CenterPacked_Finish(dots, movable.Length, radius, stopRadiusPercent, stopIterationCount);

                // Convert to 2D
                return retVal.Select(o => new Vector(o.X, o.Y)).ToArray();
            }

            public static Vector[] GetCircular_ClusteredMinDist(int returnCount, Vector[] existingStaticPoints, double radius, double minDist, double stopRadiusPercent, int stopIterationCount, double[] movableRepulseMultipliers, double[] staticRepulseMultipliers)
            {
                // Convert to 3D
                Vector3D[] existingStaticCast = null;
                if (existingStaticPoints != null)
                {
                    existingStaticCast = existingStaticPoints.Select(o => new Vector3D(o.X, o.Y, 0)).ToArray();
                }

                // Start with randomly placed dots
                Dot[] dots = GetDots(returnCount, existingStaticCast, radius, movableRepulseMultipliers, staticRepulseMultipliers, false);

                Vector3D[] retVal = GetCircular_ClusteredMinDist_Finish(dots, returnCount, radius, minDist, stopRadiusPercent, stopIterationCount);

                // Convert to 2D
                return retVal.Select(o => new Vector(o.X, o.Y)).ToArray();
            }
            public static Vector[] GetCircular_ClusteredMinDist(Vector[] movable, Vector[] existingStaticPoints, double radius, double minDist, double stopRadiusPercent, int stopIterationCount, double[] movableRepulseMultipliers, double[] staticRepulseMultipliers)
            {
                // Convert to 3D
                Vector3D[] movableCast = null;
                if (movable != null)
                {
                    movableCast = movable.Select(o => new Vector3D(o.X, o.Y, 0)).ToArray();
                }

                Vector3D[] existingStaticCast = null;
                if (existingStaticPoints != null)
                {
                    existingStaticCast = existingStaticPoints.Select(o => new Vector3D(o.X, o.Y, 0)).ToArray();
                }

                // Convert to dots
                Dot[] dots = GetDots(movableCast, existingStaticCast, radius, movableRepulseMultipliers, staticRepulseMultipliers);

                Vector3D[] retVal = GetCircular_ClusteredMinDist_Finish(dots, movable.Length, radius, minDist, stopRadiusPercent, stopIterationCount);

                // Convert to 2D
                return retVal.Select(o => new Vector(o.X, o.Y)).ToArray();
            }

            public static Vector[] GetCircularRing_EvenDist(int returnCount, Vector[] existingStaticPoints, double radius, double stopRadiusPercent, int stopIterationCount, double[] movableRepulseMultipliers, double[] staticRepulseMultipliers)
            {
                // Convert to 3D
                Vector3D[] existingStaticCast = null;
                if (existingStaticPoints != null)
                {
                    existingStaticCast = existingStaticPoints.Select(o => new Vector3D(o.X, o.Y, 0)).ToArray();
                }

                // Start with randomly placed dots
                Dot[] dots = GetDots(returnCount, existingStaticCast, radius, movableRepulseMultipliers, staticRepulseMultipliers, false);

                Vector3D[] retVal = GetCircularRing_Finish(dots, returnCount, radius, stopRadiusPercent, stopIterationCount);

                // Convert to 2D
                return retVal.Select(o => new Vector(o.X, o.Y)).ToArray();
            }
            public static Vector[] GetCircularRing_EvenDist(Vector[] movable, Vector[] existingStaticPoints, double radius, double stopRadiusPercent, int stopIterationCount, double[] movableRepulseMultipliers, double[] staticRepulseMultipliers)
            {
                // Convert to 3D
                Vector3D[] movableCast = null;
                if (movable != null)
                {
                    movableCast = movable.Select(o => new Vector3D(o.X, o.Y, 0)).ToArray();
                }

                // Convert to dots
                Dot[] dots = GetDots(movableCast, null, radius, movableRepulseMultipliers, null);

                Vector3D[] retVal = GetCircularRing_Finish(dots, movable.Length, radius, stopRadiusPercent, stopIterationCount);

                // Convert to 2D
                return retVal.Select(o => new Vector(o.X, o.Y)).ToArray();
            }

            #region Private Methods

            private static Vector3D[] GetSpherical_Finish(Dot[] dots, int returnCount, double radius, double stopRadiusPercent, int stopIterationCount)
            {
                const double PERCENT = .1d;		// 10% seems to give good results

                //TODO: This is a good starting point, but make small adjustments to this each step to let the radius get closer
                double calcDist = GetCalcDistance(GetApproximateCount(dots, radius), radius);
                double smallestAllowed = calcDist * .5d;

                double stopDist = radius * stopRadiusPercent;
                int numIterations = 0;
                double smallestLength = 0d;

                Vector3D[] forces = new Vector3D[dots.Length];

                do
                {
                    // Calculate forces
                    GetInwardForces(forces, dots);
                    smallestLength = GetRepulsionForces(forces, dots, calcDist);

                    // Move one step
                    numIterations++;
                } while (MovePoints(returnCount, dots, forces, PERCENT, stopDist, numIterations, stopIterationCount, smallestLength < smallestAllowed));

                // Exit Function
                return BuildReturn(dots, returnCount);
            }
            private static Vector3D[] GetSpherical_ClusteredMinDist_Finish(Dot[] dots, int returnCount, double radius, double minDist, double stopRadiusPercent, int stopIterationCount)
            {
                const double PERCENT = .5d;		// This method can afford to be more aggressive.  It is assumed that minDist is quite a bit smaller than radius

                double stopDist = radius * stopRadiusPercent;
                int numIterations = 0;
                double smallestLength = 0d;
                double smallestAllowed = minDist * .5d;

                Vector3D[] forces = new Vector3D[dots.Length];

                do
                {
                    // Calculate forces
                    GetInwardForces_ExceedOnly(forces, dots, radius);
                    smallestLength = GetRepulsionForces(forces, dots, minDist);

                    // Move one step
                    numIterations++;
                } while (MovePoints(returnCount, dots, forces, PERCENT, stopDist, numIterations, stopIterationCount, smallestLength < smallestAllowed));

                // Exit Function
                return BuildReturn(dots, returnCount);
            }

            private static Vector3D[] GetCircular_Finish(Dot[] dots, int returnCount, double radius, double stopRadiusPercent, int stopIterationCount)
            {
                const double PERCENT = .1d;		// 10% seems to give good results

                //TODO: This is a good starting point, but make small adjustments to this each step to let the radius get closer
                double calcDist = GetCalcDistance(GetApproximateCount(dots, radius), radius);
                double smallestAllowed = calcDist * .5d;

                double stopDist = radius * stopRadiusPercent;
                int numIterations = 0;
                double smallestLength = 0d;

                Vector3D[] forces = new Vector3D[dots.Length];

                bool hasRepulseMultipliers = dots.Any(o => !o.RepulseMultiplier.IsNearValue(1d));

                do
                {
                    // Calculate forces
                    GetInwardForces(forces, dots);

                    //NOTE: Ideally, there would only be one GetRepulsionForces method.  Continuous is much better at obeying the max
                    //radius, but doesn't look at the repulse modifier property.  So only using the standard method if repulse modifiers are used
                    if (hasRepulseMultipliers)
                    {
                        smallestLength = GetRepulsionForces(forces, dots, calcDist);
                    }
                    else
                    {
                        smallestLength = GetRepulsionForces_Continuous(forces, dots, calcDist);
                    }

                    // While testing, I didn't see any nonzeros
                    //double[] nonZero = forces.Where(o => !Math3D.IsNearZero(o.Z)).Select(o => o.Z).ToArray();

                    // Don't allow any Z movement - there shouldn't be any anyway
                    for (int cntr = 0; cntr < forces.Length; cntr++)
                    {
                        forces[cntr].Z = 0;
                    }

                    // Move one step
                    numIterations++;
                } while (MovePoints(returnCount, dots, forces, PERCENT, stopDist, numIterations, stopIterationCount, smallestLength < smallestAllowed));

                // Circular produces a radius that's way off, so this method will fix scale the outputs to the appropriate radius, but can only be
                // called when all points are movable
                if (returnCount == dots.Length)
                {
                    FixRadius(dots, radius);
                }

                // Exit Function
                return BuildReturn(dots, returnCount);
            }
            private static Vector3D[] GetCircular_CenterPacked_Finish(Dot[] dots, int returnCount, double radius, double stopRadiusPercent, int stopIterationCount)
            {
                const double PERCENT = .1d;		// 10% seems to give good results

                //TODO: This is a good starting point, but make small adjustments to this each step to let the radius get closer
                double calcDist = GetCalcDistance(GetApproximateCount(dots, radius), radius);
                double smallestAllowed = calcDist * .5d;

                double stopDist = radius * stopRadiusPercent;
                int numIterations = 0;
                double smallestLength = 0d;

                double calcDistAtZero = calcDist * .25d;
                double calcDistAtRadius = calcDist * 1.1d;

                Vector3D[] forces = new Vector3D[dots.Length];

                do
                {
                    // Calculate forces
                    GetInwardForces(forces, dots);
                    smallestLength = GetRepulsionForces_Continuous_CenterPacked(forces, dots, calcDistAtZero, calcDistAtRadius, radius);

                    // While testing, I didn't see any nonzeros
                    //double[] nonZero = forces.Where(o => !Math3D.IsNearZero(o.Z)).Select(o => o.Z).ToArray();

                    // Don't allow any Z movement - there shouldn't be any anyway
                    for (int cntr = 0; cntr < forces.Length; cntr++)
                    {
                        forces[cntr].Z = 0;
                    }

                    // Move one step
                    numIterations++;
                } while (MovePoints(returnCount, dots, forces, PERCENT, stopDist, numIterations, stopIterationCount, smallestLength < smallestAllowed));

                // Circular produces a radius that's way off, so this method will fix scale the outputs to the appropriate radius, but can only be
                // called when all points are movable
                if (returnCount == dots.Length)
                {
                    FixRadius(dots, radius);
                }

                // Exit Function
                return BuildReturn(dots, returnCount);
            }
            private static Vector3D[] GetCircular_ClusteredMinDist_Finish(Dot[] dots, int returnCount, double radius, double minDist, double stopRadiusPercent, int stopIterationCount)
            {
                const double PERCENT = .5d;		// This method can afford to be more aggressive.  It is assumed that minDist is quite a bit smaller than radius

                double stopDist = radius * stopRadiusPercent;
                int numIterations = 0;
                double smallestLength = 0d;
                double smallestAllowed = minDist * .5d;

                Vector3D[] forces = new Vector3D[dots.Length];

                do
                {
                    // Calculate forces
                    GetInwardForces_ExceedOnly(forces, dots, radius);
                    smallestLength = GetRepulsionForces(forces, dots, minDist);

                    // Don't allow any Z movement - there shouldn't be any anyway
                    for (int cntr = 0; cntr < forces.Length; cntr++)
                    {
                        forces[cntr].Z = 0;
                    }

                    // Move one step
                    numIterations++;
                } while (MovePoints(returnCount, dots, forces, PERCENT, stopDist, numIterations, stopIterationCount, smallestLength < smallestAllowed));

                // Circular produces a radius that's way off, so this method will fix scale the outputs to the appropriate radius, but can only be
                // called when all points are movable
                if (returnCount == dots.Length)
                {
                    FixRadius(dots, radius);
                }

                // Exit Function
                return BuildReturn(dots, returnCount);
            }

            private static Vector3D[] GetSphericalShell_Finish(Dot[] dots, int returnCount, double radius, double stopRadiusPercent, int stopIterationCount)
            {
                const double PERCENT = .1d;		// 10% seems to give good results

                //TODO: This is a good starting point, but make small adjustments to this each step to let the radius get closer
                double calcDist = GetCalcDistance(GetApproximateCount(dots, radius), radius);
                double smallestAllowed = calcDist * .5d;

                double stopDist = radius * stopRadiusPercent;
                int numIterations = 0;
                double smallestLength = 0d;

                Vector3D[] forces = new Vector3D[dots.Length];

                do
                {
                    // Calculate forces (no inward when constraining the the surface of the shell)
                    //TODO: The forces try to push the dots away from the shell, but they are snapped back each step.  However, the exit conditions are based on those forces, so it will always keep going until stopIterationCount
                    smallestLength = GetRepulsionForces_Continuous(forces, dots, calcDist);

                    // Move one step
                    numIterations++;
                } while (MovePoints(returnCount, dots, forces, PERCENT, stopDist, numIterations, stopIterationCount, smallestLength < smallestAllowed, radius));        // passing in radius will constrain the dots to the surface of the sphere

                // Exit Function
                return BuildReturn(dots, returnCount);
            }
            private static Vector3D[] GetCircularRing_Finish(Dot[] dots, int returnCount, double radius, double stopRadiusPercent, int stopIterationCount)
            {
                const double PERCENT = .1d;		// 10% seems to give good results

                //TODO: This is a good starting point, but make small adjustments to this each step to let the radius get closer
                double calcDist = GetCalcDistance(GetApproximateCount(dots, radius), radius);
                double smallestAllowed = calcDist * .5d;

                double stopDist = radius * stopRadiusPercent;
                int numIterations = 0;
                double smallestLength = 0d;

                Vector3D[] forces = new Vector3D[dots.Length];

                do
                {
                    // Calculate forces (no inward when constraining the the surface of the ring)
                    //TODO: The forces try to push the dots away from the ring, but they are snapped back each step.  However, the exit conditions are based on those forces, so it will always keep going until stopIterationCount
                    smallestLength = GetRepulsionForces_Continuous(forces, dots, calcDist);

                    // While testing, I didn't see any nonzeros
                    //double[] nonZero = forces.Where(o => !Math3D.IsNearZero(o.Z)).Select(o => o.Z).ToArray();

                    // Don't allow any Z movement - there shouldn't be any anyway
                    for (int cntr = 0; cntr < forces.Length; cntr++)
                    {
                        forces[cntr].Z = 0;
                    }

                    // Move one step
                    numIterations++;
                } while (MovePoints(returnCount, dots, forces, PERCENT, stopDist, numIterations, stopIterationCount, smallestLength < smallestAllowed, radius));        // passing in radius will constrain the dots to the surface of the sphere

                // Exit Function
                return BuildReturn(dots, returnCount);
            }

            private static bool MovePoints(int returnCount, Dot[] dots, Vector3D[] forces, double movePercent, double stopDist, int numIterations, int stopIterationCount, bool areTooClose, double? snapRadius = null)
            {
                double maxLength = 0d;

                #region Move points

                for (int cntr = 0; cntr < returnCount; cntr++)		// only need to iterate up to returnCount.  All the rest in dots are immoble ones
                {
                    // Move point
                    dots[cntr].Position += forces[cntr] * movePercent;

                    if (snapRadius != null)
                    {
                        // Force the dot onto the surface of the sphere
                        dots[cntr].Position = dots[cntr].Position.ToUnit() * snapRadius.Value;
                    }

                    // Find the dot that needs to move the farthest
                    double curLength = forces[cntr].LengthSquared;
                    if (curLength > maxLength)
                    {
                        maxLength = curLength;
                    }
                }

                #endregion

                #region Exit condition

                // See if this is good enough
                maxLength = Math.Sqrt(maxLength);
                if ((maxLength < stopDist && !areTooClose) || numIterations > stopIterationCount)
                {
                    return false;
                }

                #endregion

                #region Clear the forces

                for (int cntr = 0; cntr < forces.Length; cntr++)
                {
                    forces[cntr] = new Vector3D(0, 0, 0);
                }

                #endregion

                // The exit condition hasn't been hit yet
                return true;
            }

            private static Vector3D[] BuildReturn(Dot[] dots, int returnCount)
            {
                Vector3D[] retVal = new Vector3D[returnCount];

                for (int cntr = 0; cntr < returnCount; cntr++)		// The movable vectors are always the first items in the dots array
                {
                    retVal[cntr] = dots[cntr].Position;
                }

                return retVal;
            }

            private static Dot[] GetDots(int movableCount, Vector3D[] staticPoints, double radius, double[] movableRepulseMultipliers, double[] staticRepulseMultipliers, bool isSpherical)
            {
                // Seed the movable ones with random spherical locations (that's the best that can be done right now)
                Vector3D[] movable = new Vector3D[movableCount];
                for (int cntr = 0; cntr < movableCount; cntr++)
                {
                    if (isSpherical)
                    {
                        movable[cntr] = Math3D.GetRandomVector_Spherical(radius);
                    }
                    else
                    {
                        movable[cntr] = Math3D.GetRandomVector_Circular(radius);
                    }
                }

                // Call the other overload
                return GetDots(movable, staticPoints, radius, movableRepulseMultipliers, staticRepulseMultipliers);
            }
            private static Dot[] GetDots(Vector3D[] movable, Vector3D[] staticPoints, double radius, double[] movableRepulseMultipliers, double[] staticRepulseMultipliers)
            {
                int movableCount = movable.Length;

                if (movableRepulseMultipliers != null && movableRepulseMultipliers.Length != movableCount)
                {
                    throw new ArgumentOutOfRangeException("movableRepulseMultipliers", "When movableRepulseMultipliers is nonnull, it must be the same length as the number of movable points");
                }

                // Figure out how big to make the return array
                int length = movableCount;
                if (staticPoints != null)
                {
                    length += staticPoints.Length;

                    if (staticRepulseMultipliers != null && staticRepulseMultipliers.Length != staticPoints.Length)
                    {
                        throw new ArgumentOutOfRangeException("staticRepulseMultipliers", "When staticRepulseMultipliers is nonnull, it must be the same length as the number of static points");
                    }
                }

                Dot[] retVal = new Dot[length];

                // Copy the moveable ones
                for (int cntr = 0; cntr < movableCount; cntr++)
                {
                    retVal[cntr] = new Dot(false, movable[cntr], movableRepulseMultipliers == null ? 1d : movableRepulseMultipliers[cntr]);
                }

                // Add the static points to the end
                if (staticPoints != null)
                {
                    for (int cntr = 0; cntr < staticPoints.Length; cntr++)
                    {
                        retVal[movableCount + cntr] = new Dot(true, staticPoints[cntr], staticRepulseMultipliers == null ? 1d : staticRepulseMultipliers[cntr]);
                    }
                }

                // Exit Function
                return retVal;
            }

            private static double GetApproximateCount(Dot[] dots, double radius)
            {
                if (dots.All(o => !o.IsStatic))
                {
                    return dots.Length;
                }

                double[] rads = dots.Select(o => o.Position.Length).ToArray();
                double maxRadius = radius * .15d;
                double retVal = 0d;

                for (int cntr = 0; cntr < dots.Length; cntr++)
                {
                    if (!dots[cntr].IsStatic)
                    {
                        // The movable dots are always counted
                        retVal += 1d;
                    }
                    else if (rads[cntr] <= radius)
                    {
                        // Fully include everything that's <= radius
                        retVal += 1d;
                    }
                    else
                    {
                        // Scale out the ones that are > radius
                        double percent = (rads[cntr] - radius) / maxRadius;
                        if (percent < 1d && percent > 0d)
                        {
                            retVal += percent;
                        }
                    }
                }

                // Exit Function
                return retVal;
            }
            /// <summary>
            /// I used this website to curve fit:
            /// http://zunzun.com/
            /// </summary>
            private static double GetCalcDistance(double count, double radius)
            {
                //TODO: These values are good for count <= 20.  Need different values for a larger count

                const double A = .013871652011048237d;
                const double B = .50644756797276580d;
                const double C = -.15228176942968946d;
                const double D = .026504677205836713d;

                if (count <= 1)
                {
                    return 0d;
                }

                double ln = Math.Log(count, Math.E);

                double resultRadius = A + B * ln + C * Math.Pow(ln, 2d) + D * Math.Pow(ln, 3d);

                return radius / resultRadius;
            }

            private static void GetInwardForces(Vector3D[] forces, Dot[] dots)
            {
                const double INWARDFORCE = 1d;

                for (int cntr = 0; cntr < dots.Length; cntr++)
                {
                    if (dots[cntr].IsStatic)
                    {
                        continue;
                    }

                    Vector3D direction = dots[cntr].Position;
                    direction = direction.ToUnit() * (direction.Length * INWARDFORCE * -1d);

                    forces[cntr] += direction;
                }
            }
            private static void GetInwardForces_ExceedOnly(Vector3D[] forces, Dot[] dots, double radius)
            {
                // This only has an inward force if the dots are sitting outside the radius

                const double INWARDFORCE = 1d;
                double radSquared = radius * radius;

                for (int cntr = 0; cntr < dots.Length; cntr++)
                {
                    if (dots[cntr].IsStatic)
                    {
                        continue;
                    }

                    Vector3D direction = dots[cntr].Position;
                    if (direction.LengthSquared < radSquared)
                    {
                        continue;
                    }

                    direction = direction.ToUnit() * (direction.Length * INWARDFORCE * -1d);

                    forces[cntr] += direction;
                }
            }

            private static double GetRepulsionForces(Vector3D[] forces, Dot[] dots, double maxDist)
            {
                const double STRENGTH = 1d;


                //NOTE: This method fails with large numbers of dots - it takes many more dots to be noticable in 3D than 2D, that's why 2D uses
                //the continuous method, but the continuous method is bad at making a fixed radius (radius grows when you have more dots)


                // This returns the smallest distance between any two nodes
                double retVal = double.MaxValue;

                for (int outer = 0; outer < dots.Length - 1; outer++)
                {
                    for (int inner = outer + 1; inner < dots.Length; inner++)
                    {
                        Vector3D link = dots[outer].Position - dots[inner].Position;

                        double linkLength = link.Length;
                        if (linkLength < retVal)
                        {
                            retVal = linkLength;
                        }

                        // Force should be max when distance is zero, and linearly drop off to nothing
                        double inverseDistance = maxDist - linkLength;

                        double? force = 0d;
                        if (inverseDistance > 0d)
                        {
                            force = STRENGTH * inverseDistance * ((dots[outer].RepulseMultiplier + dots[inner].RepulseMultiplier) * .5d);
                        }

                        if (force != null && !double.IsNaN(force.Value) && !double.IsInfinity(force.Value))
                        {
                            link.Normalize();
                            link *= force.Value;

                            if (!dots[inner].IsStatic)
                            {
                                forces[inner] -= link;
                            }

                            if (!dots[outer].IsStatic)
                            {
                                forces[outer] += link;
                            }
                        }
                    }
                }

                return retVal;
            }
            private static double GetRepulsionForces_Continuous(Vector3D[] forces, Dot[] dots, double maxDist)
            {
                const double STRENGTH = 1d;

                // This returns the smallest distance between any two nodes
                double retVal = double.MaxValue;

                for (int outer = 0; outer < dots.Length - 1; outer++)
                {
                    for (int inner = outer + 1; inner < dots.Length; inner++)
                    {
                        Vector3D link = dots[outer].Position - dots[inner].Position;

                        double linkLength = link.Length;
                        if (linkLength < retVal)
                        {
                            retVal = linkLength;
                        }

                        double force = STRENGTH * (maxDist / linkLength);
                        if (double.IsNaN(force) || double.IsInfinity(force))
                        {
                            force = .0001d;
                        }

                        link.Normalize();
                        link *= force;

                        if (!dots[inner].IsStatic)
                        {
                            forces[inner] -= link;
                        }

                        if (!dots[outer].IsStatic)
                        {
                            forces[outer] += link;
                        }
                    }
                }

                return retVal;
            }
            private static double GetRepulsionForces_Continuous_CenterPacked(Vector3D[] forces, Dot[] dots, double maxDistAtZero, double maxDistAtRadius, double radius)
            {
                const double STRENGTH = 1d;

                // This returns the smallest distance between any two nodes
                double retVal = double.MaxValue;

                for (int outer = 0; outer < dots.Length - 1; outer++)
                {
                    for (int inner = outer + 1; inner < dots.Length; inner++)
                    {
                        Vector3D link = dots[outer].Position - dots[inner].Position;

                        double linkLength = link.Length;
                        if (linkLength < retVal)
                        {
                            retVal = linkLength;
                        }

                        // Figure out distance
                        double dist1 = UtilityCore.GetScaledValue(maxDistAtZero, maxDistAtRadius, 0d, radius, dots[outer].Position.Length);
                        double dist2 = UtilityCore.GetScaledValue(maxDistAtZero, maxDistAtRadius, 0d, radius, dots[inner].Position.Length);

                        double avgDist = ((dist1 + dist2) * .5d);

                        // Turn that into a force
                        double force = STRENGTH * (avgDist / linkLength);
                        if (double.IsNaN(force) || double.IsInfinity(force))
                        {
                            force = .0001d;
                        }

                        // Apply the force
                        link.Normalize();
                        link *= force;

                        if (!dots[inner].IsStatic)
                        {
                            forces[inner] -= link;
                        }

                        if (!dots[outer].IsStatic)
                        {
                            forces[outer] += link;
                        }
                    }
                }

                return retVal;
            }

            private static void FixRadius(Dot[] dots, double radius)
            {
                if (dots.Length < 2)
                {
                    return;
                }

                // Get the largest radius of all the points
                double maxLength = dots.Max(o => o.Position.Length);

                if (Math1D.IsNearValue(maxLength, radius) || Math1D.IsNearZero(maxLength))
                {
                    // Nothing to fix (or divisor would be zero which is bad)
                    return;
                }

                double multiplier = radius / maxLength;
                if (double.IsInfinity(multiplier) || double.IsNaN(multiplier))
                {
                    return;
                }

                for (int cntr = 0; cntr < dots.Length; cntr++)
                {
                    dots[cntr].Position *= multiplier;
                }
            }

            #endregion;
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

                // Pick 4 points
                int[] startPoints = GetStartingTetrahedron(points);
                List<TriangleWithPoints> retVal = ConvertStartingPointsToTriangles(startPoints, points);

                // If any triangle has any points outside the hull, then remove that triangle, and replace it with triangles connected to the
                // farthest out point (relative to the triangle that got removed)
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
                    // Found a case where the points were nearly coplanar, and adding another point wiped out the whole hull
                    return null;
                }

                // Exit Function
                return retVal.ToArray();
            }

            #region Private Methods

            /// <summary>
            /// I've seen various literature call this initial tetrahedron a simplex
            /// </summary>
            private static int[] GetStartingTetrahedron(Point3D[] points)
            {
                int[] retVal = new int[4];

                // Validate the point cloud is 3D, also get the points that have the smallest and largest X (an exception is thrown if they are the same point)
                int minXIndex, maxXIndex;
                GetStartingTetrahedronSprtGetMinMaxX(out minXIndex, out maxXIndex, points);

                // The first two return points will be the ones with the min and max X values
                retVal[0] = minXIndex;
                retVal[1] = maxXIndex;

                // The third point will be the one that is farthest from the line defined by the first two
                int thirdIndex = GetStartingTetrahedronSprtFarthestFromLine(minXIndex, maxXIndex, points);
                retVal[2] = thirdIndex;

                // The fourth point will be the one that is farthest from the plane defined by the first three
                int fourthIndex = GetStartingTetrahedronSprtFarthestFromPlane(minXIndex, maxXIndex, thirdIndex, points);
                retVal[3] = fourthIndex;

                // Exit Function
                return retVal;
            }
            /// <summary>
            /// This does more than just look at X.  It looks at Y and Z as well to verify the point cloud has points in all 3 dimensions
            /// </summary>
            private static void GetStartingTetrahedronSprtGetMinMaxX(out int minXIndex, out int maxXIndex, Point3D[] points)
            {
                // Create arrays to hold the min and max along each axis (0=X, 1=Y, 2=Z) - using the first point
                double[] minValues = new double[] { points[0].X, points[0].Y, points[0].Z };
                double[] maxValues = new double[] { points[0].X, points[0].Y, points[0].Z };
                int[] minIndicies = new int[] { 0, 0, 0 };
                int[] maxIndicies = new int[] { 0, 0, 0 };

                for (int cntr = 1; cntr < points.Length; cntr++)
                {
                    #region Examine Point

                    // Min
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

                    // Max
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

                // Return the two points that are the min/max X
                minXIndex = minIndicies[0];
                maxXIndex = maxIndicies[0];
            }
            /// <summary>
            /// Finds the point that is farthest from the line
            /// </summary>
            private static int GetStartingTetrahedronSprtFarthestFromLine(int index1, int index2, Point3D[] points)
            {
                Vector3D lineDirection = points[index2] - points[index1];		// The nearest point method wants a vector, so calculate it once
                Point3D startPoint = points[index1];		// not sure if there is much of a penalty to referencing a list by index, but I figure I'll just cache this point once

                double maxDistance = -1d;
                int retVal = -1;

                for (int cntr = 0; cntr < points.Length; cntr++)
                {
                    if (cntr == index1 || cntr == index2)
                    {
                        continue;
                    }

                    // Calculate the distance from the line
                    Point3D nearestPoint = Math3D.GetClosestPoint_Line_Point(startPoint, lineDirection, points[cntr]);
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

                // Exit Function
                return retVal;
            }
            private static int GetStartingTetrahedronSprtFarthestFromPlane(int index1, int index2, int index3, Point3D[] points)
            {
                //NOTE:  I'm copying bits of Math3D.DistanceFromPlane here for optimization reasons
                Vector3D[] triangle = new Vector3D[] { points[index1].ToVector(), points[index2].ToVector(), points[index3].ToVector() };
                Vector3D normal = Math3D.GetTriangleNormalUnit(triangle);
                double originDistance = Math3D.GetPlaneOriginDistance(normal, triangle[0].ToPoint());

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

                    // I don't care which side of the triangle the point is on for this initial tetrahedron
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

                // Exit Function
                return retVal;
            }

            private static List<TriangleWithPoints> ConvertStartingPointsToTriangles(int[] startPoints, Point3D[] allPoints)
            {
                if (startPoints.Length != 4)
                {
                    throw new ArgumentException("This method expects exactly 4 points passed in");
                }

                List<TriangleWithPoints> retVal = new List<TriangleWithPoints>();

                // Make triangles
                retVal.Add(CreateTriangle(startPoints[0], startPoints[1], startPoints[2], allPoints[startPoints[3]], allPoints, null));
                retVal.Add(CreateTriangle(startPoints[3], startPoints[1], startPoints[0], allPoints[startPoints[2]], allPoints, null));
                retVal.Add(CreateTriangle(startPoints[3], startPoints[2], startPoints[1], allPoints[startPoints[0]], allPoints, null));
                retVal.Add(CreateTriangle(startPoints[3], startPoints[0], startPoints[2], allPoints[startPoints[1]], allPoints, null));

                // Link triangles together
                TriangleIndexedLinked.LinkTriangles_Edges(retVal.ConvertAll(o => (TriangleIndexedLinked)o).ToList(), true);

                #region Calculate outside points

                // GetOutsideSet wants indicies to the points it needs to worry about.  This initial call needs all points
                List<int> allPointIndicies = Enumerable.Range(0, allPoints.Length).ToList();

                // Remove the indicies that are in the return triangles (I ran into a case where 4 points were passed in, but they were nearly coplanar - enough
                // that GetOutsideSet's Math3D.IsNearZero included it)
                foreach (int index in retVal.SelectMany(o => o.IndexArray).Distinct())
                {
                    allPointIndicies.Remove(index);
                }

                // For every triangle, find the points that are outside the polygon (not the points behind the triangle)
                // Note that a point will never be shared between triangles
                foreach (TriangleWithPoints triangle in retVal)
                {
                    if (allPointIndicies.Count > 0)
                    {
                        triangle.OutsidePoints.AddRange(GetOutsideSet(triangle, allPointIndicies, allPoints));
                    }
                }

                #endregion

                // Exit Function
                return retVal;
            }

            /// <summary>
            /// This works on a triangle that contains outside points.  It removes the triangle, and creates new ones connected to
            /// the outermost outside point
            /// </summary>
            private static void ProcessTriangle(List<TriangleWithPoints> hull, int hullIndex, Point3D[] allPoints)
            {
                TriangleWithPoints removedTriangle = hull[hullIndex];

                // Find the farthest point from this triangle
                int fartherstIndex = ProcessTriangleSprtFarthestPoint(removedTriangle);
                if (fartherstIndex < 0)
                {
                    // The outside points are on the same plane as this triangle (and sitting within the bounds of the triangle).
                    // Just wipe the points and go away
                    removedTriangle.OutsidePoints.Clear();
                    return;
                    //throw new ApplicationException(string.Format("Couldn't find a farthest point for triangle\r\n{0}\r\n\r\n{1}\r\n",
                    //    removedTriangle.ToString(),
                    //    string.Join("\r\n", removedTriangle.OutsidePoints.Select(o => o.Item1.ToString() + "   |   " + allPoints[o.Item1].ToString(true)).ToArray())));		// this should never happen
                }

                //Key=which triangles to remove from the hull
                //Value=meaningless, I just wanted a sorted list
                SortedList<TriangleIndexedLinked, int> removedTriangles = new SortedList<TriangleIndexedLinked, int>();

                //Key=triangle on the hull that is a boundry (the new triangles will be added to these boundry triangles)
                //Value=the key's edges that are exposed to the removed triangles
                SortedList<TriangleIndexedLinked, List<TriangleEdge>> removedRim = new SortedList<TriangleIndexedLinked, List<TriangleEdge>>();

                // Find all the triangles that can see this point (they will need to be removed from the hull)
                //NOTE:  This method is recursive
                ProcessTriangleSprtRemove(removedTriangle, removedTriangles, removedRim, allPoints[fartherstIndex].ToVector());

                // Remove these from the hull
                ProcessTriangleSprtRemoveFromHull(hull, removedTriangles.Keys, removedRim);

                // Get all the outside points
                //List<int> allOutsidePoints = removedTriangles.Keys.SelectMany(o => ((TriangleWithPoints)o).OutsidePoints).Distinct().ToList();		// if you try to read what SelectMany does, it gets pretty wordy and unclear.  It just does a foreach across all the OutsidePoints lists from all the removedTriangles (treats the list of lists like a single list)
                List<int> allOutsidePoints = removedTriangles.Keys.SelectMany(o => ((TriangleWithPoints)o).OutsidePoints).ToList();		// there's no need to call distinct, since the outside points aren't shared between triangles

                // Create new triangles
                ProcessTriangleSprtNew(hull, fartherstIndex, removedRim, allPoints, allOutsidePoints);		// Note that after this method, allOutsidePoints will only be the points left over (the ones that are inside the hull)
            }
            private static int ProcessTriangleSprtFarthestPoint(TriangleWithPoints triangle)
            {
                //NOTE: This method is nearly a copy of GetOutsideSet

                double planeOriginDist = Math3D.GetPlaneOriginDistance(triangle.NormalUnit, triangle.Point0);

                List<int> pointIndicies = triangle.OutsidePoints;
                Point3D[] allPoints = triangle.AllPoints;

                double maxDistance = 0d;
                int retVal = -1;

                for (int cntr = 0; cntr < pointIndicies.Count; cntr++)
                {
                    double distance = Math3D.DistanceFromPlane(triangle.NormalUnit, planeOriginDist, allPoints[pointIndicies[cntr]]);

                    // Distance should never be negative (or the point wouldn't be in the list of outside points).  If for some reason there is one with a negative distance,
                    // it shouldn't be considered, because it sits inside the hull
                    if (distance > maxDistance)
                    {
                        maxDistance = distance;
                        retVal = pointIndicies[cntr];
                    }
                    else if (Math1D.IsNearZero(distance) && Math1D.IsNearZero(maxDistance))		// this is for a coplanar point that can have a very slightly negative distance
                    {
                        // Can't trust the previous bary check, need another one (maybe it's false because it never went through that first check?)
                        Vector bary = Math3D.ToBarycentric(triangle, allPoints[pointIndicies[cntr]]);
                        if (bary.X < 0d || bary.Y < 0d || bary.X + bary.Y > 1d)
                        {
                            maxDistance = 0d;
                            retVal = pointIndicies[cntr];
                        }
                    }
                }

                // Exit Function
                return retVal;
            }

            private static void ProcessTriangleSprtRemove(TriangleIndexedLinked triangle, SortedList<TriangleIndexedLinked, int> removedTriangles, SortedList<TriangleIndexedLinked, List<TriangleEdge>> removedRim, Vector3D farPoint)
            {
                // This triangle will need to be removed.  Keep track of it so it's not processed again (the int means nothing)
                removedTriangles.Add(triangle, 0);

                // Try each neighbor
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
                    // This triangle is already recognized as part of the hull.  Add a link from it to the from triangle (because two of its edges
                    // are part of the hull rim)
                    removedRim[triangle].Add(triangle.WhichEdge(fromTriangle));
                    return;
                }

                // Need to subtract the far point from some point on this triangle, so that it's a vector from the triangle to the
                // far point, and not from the origin
                double dot = Vector3D.DotProduct(triangle.Normal, (farPoint - triangle.Point0).ToVector());
                if (dot >= 0d || Math1D.IsNearZero(dot))		// 0 is coplanar, -1 is the opposite side
                {
                    // This triangle is visible to the point.  Remove it (recurse)
                    ProcessTriangleSprtRemove(triangle, removedTriangles, removedRim, farPoint);
                }
                else
                {
                    // This triangle is invisible to the point, so needs to stay part of the hull.  Store this boundry
                    removedRim.Add(triangle, new List<TriangleEdge>());
                    removedRim[triangle].Add(triangle.WhichEdge(fromTriangle));
                }
            }

            private static void ProcessTriangleSprtRemoveFromHull(List<TriangleWithPoints> hull, IList<TriangleIndexedLinked> trianglesToRemove, SortedList<TriangleIndexedLinked, List<TriangleEdge>> removedRim)
            {
                // Remove from the hull list
                foreach (TriangleIndexedLinked triangle in trianglesToRemove)
                {
                    hull.Remove((TriangleWithPoints)triangle);
                }

                // Break the links from the hull to the removed triangles (there's no need to break links in the other direction.  The removed triangles
                // will become orphaned, and eventually garbage collected)
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

                // Run around the rim, and build a triangle between the far point and each edge
                foreach (TriangleIndexedLinked rimTriangle in removedRim.Keys)
                {
                    // Get a point that is toward the hull (the created triangle will be built so its normal points away from this)
                    Point3D insidePoint = ProcessTriangleSprtNewSprtInsidePoint(rimTriangle, removedRim[rimTriangle]);

                    foreach (TriangleEdge rimEdge in removedRim[rimTriangle])
                    {
                        // Get the points for this edge
                        int index1, index2;
                        rimTriangle.GetIndices(out index1, out index2, rimEdge);

                        // Build the triangle
                        TriangleWithPoints triangle = CreateTriangle(fartherstIndex, index1, index2, insidePoint, allPoints, rimTriangle);

                        // Now link this triangle with the boundry triangle (just the one edge, the other edges will be joined later)
                        TriangleIndexedLinked.LinkTriangles_Edges(triangle, rimTriangle);

                        // Store this triangle
                        newTriangles.Add(triangle);
                        hull.Add(triangle);
                    }
                }

                // The new triangles are linked to the boundry already.  Now link the other two edges to each other (newTriangles forms a
                // triangle fan, but they aren't neccessarily consecutive)
                TriangleIndexedLinked.LinkTriangles_Edges(newTriangles.ConvertAll(o => (TriangleIndexedLinked)o).ToList(), false);

                // Distribute the outside points to these new triangles
                foreach (TriangleWithPoints triangle in newTriangles)
                {
                    // Find the points that are outside the polygon (not the points behind the triangle)
                    // Note that a point will never be shared between triangles
                    triangle.OutsidePoints.AddRange(GetOutsideSet(triangle, outsidePoints, allPoints));
                }
            }
            private static Point3D ProcessTriangleSprtNewSprtInsidePoint(TriangleIndexedLinked rimTriangle, List<TriangleEdge> sharedEdges)
            {
                bool[] used = new bool[3];

                // Figure out which indices are used
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

                // Find one that isn't used
                for (int cntr = 0; cntr < 3; cntr++)
                {
                    if (!used[cntr])
                    {
                        return rimTriangle[cntr];
                    }
                }

                // Project a point away from this triangle
                //TODO:  If the hull ends up concave, this is a very likely culprit.  May need to come up with a better way
                return rimTriangle.GetCenterPoint() + (rimTriangle.Normal * -.001);		// by not using the unit normal, I'm keeping this scaled roughly to the size of the triangle
            }

            /// <summary>
            /// This takes in 3 points that belong to the triangle, and a point that is not on the triangle, but is toward the rest
            /// of the hull.  It then creates a triangle whose normal points away from the hull (right hand rule)
            /// </summary>
            private static TriangleWithPoints CreateTriangle(int point0, int point1, int point2, Point3D pointWithinHull, Point3D[] allPoints, ITriangle neighbor)
            {
                // Try an arbitrary orientation
                TriangleWithPoints retVal = new TriangleWithPoints(point0, point1, point2, allPoints);

                // Get a vector pointing from point0 to the inside point
                Vector3D towardHull = pointWithinHull - allPoints[point0];

                double dot = Vector3D.DotProduct(towardHull, retVal.Normal);
                if (dot > 0d)
                {
                    // When the dot product is greater than zero, that means the normal points in the same direction as the vector that points
                    // toward the hull.  So buid a triangle that points in the opposite direction
                    retVal = new TriangleWithPoints(point0, point2, point1, allPoints);
                }
                else if (dot == 0d)
                {
                    // This new triangle is coplanar with the neighbor triangle, so pointWithinHull can't be used to figure out if this return
                    // triangle is facing the correct way.  Instead, make it point the same direction as the neighbor triangle
                    dot = Vector3D.DotProduct(retVal.Normal, neighbor.Normal);
                    if (dot < 0)
                    {
                        retVal = new TriangleWithPoints(point0, point2, point1, allPoints);
                    }
                }

                // Exit Function
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

                // Compute D, using a arbitrary point P, that lies on the plane: D = - (Nx*Px + Ny*Py + Nz*Pz); Don't forget the inversion !
                double D = -((triangle.NormalUnit.X * triangle.Point0.X) + (triangle.NormalUnit.Y * triangle.Point0.Y) + (triangle.NormalUnit.Z * triangle.Point0.Z));

                int cntr = 0;
                while (cntr < pointIndicies.Count)
                {
                    int index = pointIndicies[cntr];

                    if (index == triangle.Index0 || index == triangle.Index1 || index == triangle.Index2)
                    {
                        pointIndicies.Remove(index);		// no need to consider this for future calls
                        continue;
                    }

                    // You can test a point (T) with respect to the plane using the plane equation: res = Nx*Tx + Ny*Ty + Nz*Tz + D
                    double res = (triangle.NormalUnit.X * allPoints[index].X) + (triangle.NormalUnit.Y * allPoints[index].Y) + (triangle.NormalUnit.Z * allPoints[index].Z) + D;

                    if (res > 0d)		// anything greater than zero lies outside the plane
                    {
                        retVal.Add(index);
                        pointIndicies.Remove(index);		// an outside point can only belong to one triangle
                    }
                    else if (Math1D.IsNearZero(res))
                    {
                        // This point is coplanar.  Only consider it an outside point if it is outside the bounds of this triangle
                        Vector bary = Math3D.ToBarycentric(triangle, allPoints[index]);
                        if (bary.X < 0d || bary.Y < 0d || bary.X + bary.Y > 1d)
                        {
                            retVal.Add(index);
                            pointIndicies.Remove(index);		// an outside point can only belong to one triangle
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
        #region Class: SliceTriangles

        private static class SliceTriangles
        {
            public static ITriangle[] Slice(ITriangle[] triangles, double maxEdgeLength, int maxPasses = 3)
            {
                if (maxPasses < 1)
                {
                    return triangles;
                }

                List<ITriangle> retVal = new List<ITriangle>();

                bool foundLarger = false;

                for (int cntr = 0; cntr < triangles.Length; cntr++)
                {
                    // Get the length of the longest edge
                    Tuple<TriangleEdge, double>[] edgeLengths = Triangle.Edges.Select(o => Tuple.Create(o, triangles[cntr].GetEdgeLength(o))).OrderBy(o => o.Item2).ToArray();
                    double maxLength = edgeLengths.Max(o => o.Item2);

                    if (maxLength > maxEdgeLength)
                    {
                        // This is too big.  Cut it up
                        retVal.AddRange(Divide(triangles[cntr], edgeLengths));
                        foundLarger = true;
                    }
                    else
                    {
                        // Keep as is
                        retVal.Add(triangles[cntr]);
                    }
                }

                if (foundLarger && maxPasses > 1)
                {
                    ITriangle[] newTriangles = retVal.ToArray();
                    retVal.Clear();

                    // Recurse
                    retVal.AddRange(Slice(newTriangles, maxEdgeLength, maxPasses - 1));
                }

                // Exit Function
                return retVal.ToArray();
            }
            public static ITriangleIndexed[] Slice_Smooth(ITriangleIndexed[] triangles, double maxEdgeLength, int maxPasses = 3)
            {
                if (maxPasses < 1)
                {
                    return triangles;
                }

                List<ITriangle> sliced = new List<ITriangle>();

                bool foundLarger = false;

                SortedList<int, ITriangle> planes = new SortedList<int, ITriangle>();
                SortedList<Tuple<int, int>, BezierSegment3D> edgeBeziers = new SortedList<Tuple<int, int>, BezierSegment3D>();

                for (int cntr = 0; cntr < triangles.Length; cntr++)
                {
                    // Get the length of the longest edge
                    Tuple<TriangleEdge, double>[] edgeLengths = Triangle.Edges.
                        Select(o => Tuple.Create(o, triangles[cntr].GetEdgeLength(o))).
                        OrderBy(o => o.Item2).
                        ToArray();

                    double maxLength = edgeLengths.Max(o => o.Item2);

                    if (maxLength > maxEdgeLength)
                    {
                        // This is too big.  Cut it up
                        sliced.AddRange(Divide(triangles[cntr], edgeLengths, triangles, planes, edgeBeziers));
                        foundLarger = true;
                    }
                    else
                    {
                        // Keep as is
                        sliced.Add(triangles[cntr]);
                    }
                }

                if (!foundLarger)
                {
                    return triangles;
                }

                // Find any unsliced triangles that are neighbors with sliced, and slice them (not introducing new points, just pulling
                // triangles out to the neighbor's new points)
                foreach (ITriangleIndexed triangle in sliced.ToArray())
                {
                    DivideNeighbor(triangle, sliced, edgeBeziers);
                }

                // Convert to indexed
                TriangleIndexed[] slicedIndexed = TriangleIndexed.ConvertToIndexed(sliced.ToArray());

                if (maxPasses > 1)
                {
                    // Recurse
                    return Slice_Smooth(slicedIndexed, maxEdgeLength, maxPasses - 1);
                }
                else
                {
                    return slicedIndexed;
                }
            }

            #region Private Methods - standard

            private static ITriangle[] Divide(ITriangle triangle, Tuple<TriangleEdge, double>[] edgeLengths)
            {
                // Take the shortest length divided by the longest (the array is sorted)
                double ratio = edgeLengths[0].Item2 / edgeLengths[2].Item2;

                TriangleIndexed[] retVal = null;

                if (ratio > .75d)
                {
                    // The 3 egde lengths are roughly equal.  Divide into 4
                    retVal = Divide_4(triangle);
                }
                else if (ratio < .33d)
                {
                    // Skinny base, and two large sides
                    retVal = Divide_SkinnyBase(triangle, edgeLengths);
                }
                else
                {
                    // Wide base, and two smaller sides
                    retVal = Divide_WideBase(triangle, edgeLengths);
                }

                // Make sure the normals point in the same direction
                for (int cntr = 0; cntr < retVal.Length; cntr++)
                {
                    if (Vector3D.DotProduct(retVal[cntr].Normal, triangle.Normal) < 0)
                    {
                        // Reverse it
                        retVal[cntr] = new TriangleIndexed(retVal[cntr].Index0, retVal[cntr].Index2, retVal[cntr].Index1, retVal[cntr].AllPoints);
                    }
                }

                return retVal;
            }

            private static TriangleIndexed[] Divide_4(ITriangle triangle)
            {
                Point3D[] points = new Point3D[6];
                points[0] = triangle.Point0;
                points[1] = triangle.Point1;
                points[2] = triangle.Point2;
                points[3] = triangle.GetEdgeMidpoint(TriangleEdge.Edge_01);
                points[4] = triangle.GetEdgeMidpoint(TriangleEdge.Edge_12);
                points[5] = triangle.GetEdgeMidpoint(TriangleEdge.Edge_20);

                List<TriangleIndexed> retVal = new List<TriangleIndexed>();

                retVal.Add(new TriangleIndexed(0, 3, 5, points));       // Bottom Left
                retVal.Add(new TriangleIndexed(1, 4, 3, points));       // Top
                retVal.Add(new TriangleIndexed(2, 5, 4, points));       // Bottom Right
                retVal.Add(new TriangleIndexed(3, 4, 5, points));       // Center

                return retVal.ToArray();
            }
            private static TriangleIndexed[] Divide_SkinnyBase(ITriangle triangle, Tuple<TriangleEdge, double>[] edgeLengths)
            {
                Point3D[] points = new Point3D[5];
                points[0] = triangle.GetCommonPoint(edgeLengths[0].Item1, edgeLengths[1].Item1);        // Bottom Left
                points[1] = triangle.GetCommonPoint(edgeLengths[1].Item1, edgeLengths[2].Item1);        // Top
                points[2] = triangle.GetCommonPoint(edgeLengths[0].Item1, edgeLengths[2].Item1);        // Bottom Right
                points[3] = triangle.GetEdgeMidpoint(edgeLengths[1].Item1);        // Mid Left
                points[4] = triangle.GetEdgeMidpoint(edgeLengths[2].Item1);        // Mid Right

                List<TriangleIndexed> retVal = new List<TriangleIndexed>();

                retVal.Add(new TriangleIndexed(1, 4, 3, points));       // Top
                retVal.Add(new TriangleIndexed(0, 4, 2, points));       // Bottom Right
                retVal.Add(new TriangleIndexed(0, 3, 4, points));       // Middle Left

                return retVal.ToArray();
            }
            private static TriangleIndexed[] Divide_WideBase(ITriangle triangle, Tuple<TriangleEdge, double>[] edgeLengths)
            {
                Point3D[] points = new Point3D[4];
                points[0] = triangle.GetCommonPoint(edgeLengths[2].Item1, edgeLengths[0].Item1);        // Bottom Left
                points[1] = triangle.GetCommonPoint(edgeLengths[0].Item1, edgeLengths[1].Item1);        // Top
                points[2] = triangle.GetCommonPoint(edgeLengths[2].Item1, edgeLengths[1].Item1);        // Bottom Right
                points[3] = triangle.GetEdgeMidpoint(edgeLengths[2].Item1);        // Mid Bottom

                List<TriangleIndexed> retVal = new List<TriangleIndexed>();

                retVal.Add(new TriangleIndexed(0, 1, 3, points));       // Left
                retVal.Add(new TriangleIndexed(1, 2, 3, points));       // Right

                return retVal.ToArray();
            }

            #endregion
            #region Private Methods - smooth

            private static ITriangle[] Divide(ITriangleIndexed triangle, Tuple<TriangleEdge, double>[] edgeLengths, ITriangleIndexed[] triangles, SortedList<int, ITriangle> planes, SortedList<Tuple<int, int>, BezierSegment3D> edgeBeziers)
            {
                // Define beziers for the three edges
                var curves = Triangle.Edges.
                    Select(o => Tuple.Create(o, GetCurvedEdge(triangle, o, triangles, planes, edgeBeziers))).
                    ToArray();

                // Take the shortest length divided by the longest (the array is sorted)
                double ratio = edgeLengths[0].Item2 / edgeLengths[2].Item2;

                TriangleIndexed[] retVal = null;

                if (ratio > .75d)
                {
                    // The 3 egde lengths are roughly equal.  Divide into 4
                    retVal = Divide_4(triangle, curves);
                }
                else if (ratio < .33d)
                {
                    // Skinny base, and two large sides
                    retVal = Divide_SkinnyBase(triangle, edgeLengths, curves);
                }
                else
                {
                    // Wide base, and two smaller sides
                    retVal = Divide_WideBase(triangle, edgeLengths, curves);
                }

                // Make sure the normals point in the same direction
                for (int cntr = 0; cntr < retVal.Length; cntr++)
                {
                    if (Vector3D.DotProduct(retVal[cntr].Normal, triangle.Normal) < 0)
                    {
                        // Reverse it
                        retVal[cntr] = new TriangleIndexed(retVal[cntr].Index0, retVal[cntr].Index2, retVal[cntr].Index1, retVal[cntr].AllPoints);
                    }
                }

                return retVal;
            }

            private static TriangleIndexed[] Divide_4(ITriangleIndexed triangle, Tuple<TriangleEdge, BezierSegment3D>[] curves)
            {
                List<Point3D> points = new List<Point3D>();
                List<int> map = new List<int>();

                //NOTE: Reusing existing points, then just adding new.  The constructed hull will have patches of triangles with
                //differing point arrays, but there is a pass at the end that rebuilds the hull with a single unique point array
                points.AddRange(triangle.AllPoints);

                map.Add(triangle.Index0);      // 0
                map.Add(triangle.Index1);      // 1
                map.Add(triangle.Index2);      // 2

                points.Add(BezierUtil.GetPoint(.5, curves.First(o => o.Item1 == TriangleEdge.Edge_01).Item2));       // 3
                map.Add(points.Count - 1);

                points.Add(BezierUtil.GetPoint(.5, curves.First(o => o.Item1 == TriangleEdge.Edge_12).Item2));       // 4
                map.Add(points.Count - 1);

                points.Add(BezierUtil.GetPoint(.5, curves.First(o => o.Item1 == TriangleEdge.Edge_20).Item2));       // 5
                map.Add(points.Count - 1);

                Point3D[] pointArr = points.ToArray();

                List<TriangleIndexed> retVal = new List<TriangleIndexed>();

                retVal.Add(MatchNormal(new TriangleIndexed(map[0], map[3], map[5], pointArr), triangle));       // Bottom Left
                retVal.Add(MatchNormal(new TriangleIndexed(map[1], map[4], map[3], pointArr), triangle));       // Top
                retVal.Add(MatchNormal(new TriangleIndexed(map[2], map[5], map[4], pointArr), triangle));       // Bottom Right
                retVal.Add(MatchNormal(new TriangleIndexed(map[3], map[4], map[5], pointArr), triangle));       // Center

                return retVal.ToArray();
            }
            private static TriangleIndexed[] Divide_SkinnyBase(ITriangleIndexed triangle, Tuple<TriangleEdge, double>[] edgeLengths, Tuple<TriangleEdge, BezierSegment3D>[] curves)
            {
                List<Point3D> points = new List<Point3D>();
                List<int> map = new List<int>();

                points.AddRange(triangle.AllPoints);

                map.Add(triangle.GetCommonIndex(edgeLengths[0].Item1, edgeLengths[1].Item1));        // Bottom Left (0)
                map.Add(triangle.GetCommonIndex(edgeLengths[1].Item1, edgeLengths[2].Item1));        // Top (1)
                map.Add(triangle.GetCommonIndex(edgeLengths[0].Item1, edgeLengths[2].Item1));        // Bottom Right (2)

                points.Add(BezierUtil.GetPoint(.5, curves.First(o => o.Item1 == edgeLengths[1].Item1).Item2));        // Mid Left (3)
                map.Add(points.Count - 1);

                points.Add(BezierUtil.GetPoint(.5, curves.First(o => o.Item1 == edgeLengths[2].Item1).Item2));        // Mid Right (4)
                map.Add(points.Count - 1);

                Point3D[] pointArr = points.ToArray();

                List<TriangleIndexed> retVal = new List<TriangleIndexed>();

                retVal.Add(MatchNormal(new TriangleIndexed(map[1], map[4], map[3], pointArr), triangle));       // Top
                retVal.Add(MatchNormal(new TriangleIndexed(map[0], map[4], map[2], pointArr), triangle));       // Bottom Right
                retVal.Add(MatchNormal(new TriangleIndexed(map[0], map[3], map[4], pointArr), triangle));       // Middle Left

                return retVal.ToArray();
            }
            private static TriangleIndexed[] Divide_WideBase(ITriangleIndexed triangle, Tuple<TriangleEdge, double>[] edgeLengths, Tuple<TriangleEdge, BezierSegment3D>[] curves)
            {
                List<Point3D> points = new List<Point3D>();
                List<int> map = new List<int>();

                points.AddRange(triangle.AllPoints);

                map.Add(triangle.GetCommonIndex(edgeLengths[2].Item1, edgeLengths[0].Item1));        // Bottom Left (0)
                map.Add(triangle.GetCommonIndex(edgeLengths[0].Item1, edgeLengths[1].Item1));        // Top (1)
                map.Add(triangle.GetCommonIndex(edgeLengths[2].Item1, edgeLengths[1].Item1));        // Bottom Right (2)

                points.Add(BezierUtil.GetPoint(.5, curves.First(o => o.Item1 == edgeLengths[2].Item1).Item2));        // Mid Bottom (3)
                map.Add(points.Count - 1);

                Point3D[] pointArr = points.ToArray();

                List<TriangleIndexed> retVal = new List<TriangleIndexed>();

                retVal.Add(MatchNormal(new TriangleIndexed(map[0], map[1], map[3], pointArr), triangle));       // Left
                retVal.Add(MatchNormal(new TriangleIndexed(map[1], map[2], map[3], pointArr), triangle));       // Right

                return retVal.ToArray();
            }

            private static void DivideNeighbor(ITriangleIndexed triangle, List<ITriangle> returnList, SortedList<Tuple<int, int>, BezierSegment3D> edgeBeziers)
            {
                // Find beziers for the edges of this triangle (if a bezier was created, then that edge was sliced in half)
                var bisectedEdges = Triangle.Edges.
                    Select(o => new { Edge = o, From = triangle.GetIndex(o, true), To = triangle.GetIndex(o, false) }).
                    Select(o => new { Edge = o.Edge, Key = Tuple.Create(Math.Min(o.From, o.To), Math.Max(o.From, o.To)) }).
                    Select(o => new { Edge = o.Edge, Key = o.Key, Match = edgeBeziers.TryGetValue(o.Key) }).
                    Where(o => o.Match.Item1).
                    Select(o => Tuple.Create(o.Edge, o.Key, o.Match.Item2)).
                    ToArray();

                TriangleIndexed[] replacement;
                long[] retVal = new long[0];

                switch (bisectedEdges.Length)
                {
                    case 0:
                        return;

                    case 1:
                        replacement = DivideNeighbor_1(triangle, bisectedEdges[0]);
                        break;

                    case 2:
                        replacement = DivideNeighbor_2(triangle, bisectedEdges);
                        break;

                    case 3:
                        replacement = DivideNeighbor_3(triangle, bisectedEdges);
                        break;

                    default:
                        throw new ApplicationException("Unexpected number of matches: " + bisectedEdges.Length.ToString());
                }

                // Swap them
                returnList.Remove(triangle);
                returnList.AddRange(replacement);
            }

            private static TriangleIndexed[] DivideNeighbor_1(ITriangleIndexed triangle, Tuple<TriangleEdge, Tuple<int, int>, BezierSegment3D> bisectedEdge)
            {
                List<Point3D> points = new List<Point3D>();
                List<int> map = new List<int>();

                points.AddRange(triangle.AllPoints);

                map.Add(triangle.GetIndex(bisectedEdge.Item1, true));        // Bottom Left (0)
                map.Add(triangle.GetOppositeIndex(bisectedEdge.Item1));        // Top (1)
                map.Add(triangle.GetIndex(bisectedEdge.Item1, false));        // Bottom Right (2)

                points.Add(BezierUtil.GetPoint(.5, bisectedEdge.Item3));        // Mid Bottom (3)
                map.Add(points.Count - 1);

                Point3D[] pointArr = points.ToArray();

                List<TriangleIndexed> retVal = new List<TriangleIndexed>();

                retVal.Add(MatchNormal(new TriangleIndexed(map[0], map[1], map[3], pointArr), triangle));       // Left
                retVal.Add(MatchNormal(new TriangleIndexed(map[1], map[2], map[3], pointArr), triangle));       // Right

                return retVal.ToArray();
            }
            private static TriangleIndexed[] DivideNeighbor_2(ITriangleIndexed triangle, Tuple<TriangleEdge, Tuple<int, int>, BezierSegment3D>[] bisectedEdges)
            {
                List<Point3D> points = new List<Point3D>();
                List<int> map = new List<int>();

                points.AddRange(triangle.AllPoints);

                map.Add(triangle.GetUncommonIndex(bisectedEdges[0].Item1, bisectedEdges[1].Item1));        // Bottom Left (0)
                map.Add(triangle.GetCommonIndex(bisectedEdges[0].Item1, bisectedEdges[1].Item1));        // Top (1)
                map.Add(triangle.GetUncommonIndex(bisectedEdges[1].Item1, bisectedEdges[0].Item1));        // Bottom Right (2)

                points.Add(BezierUtil.GetPoint(.5, bisectedEdges[0].Item3));        // Mid Left (3)
                map.Add(points.Count - 1);

                points.Add(BezierUtil.GetPoint(.5, bisectedEdges[1].Item3));        // Mid Right (4)
                map.Add(points.Count - 1);

                Point3D[] pointArr = points.ToArray();

                List<TriangleIndexed> retVal = new List<TriangleIndexed>();

                retVal.Add(MatchNormal(new TriangleIndexed(map[1], map[4], map[3], pointArr), triangle));       // Top
                retVal.Add(MatchNormal(new TriangleIndexed(map[0], map[4], map[2], pointArr), triangle));       // Bottom Right
                retVal.Add(MatchNormal(new TriangleIndexed(map[0], map[3], map[4], pointArr), triangle));       // Middle Left

                return retVal.ToArray();
            }
            private static TriangleIndexed[] DivideNeighbor_3(ITriangleIndexed triangle, Tuple<TriangleEdge, Tuple<int, int>, BezierSegment3D>[] bisectedEdges)
            {
                List<Point3D> points = new List<Point3D>();
                List<int> map = new List<int>();

                points.AddRange(triangle.AllPoints);

                map.Add(triangle.Index0);      // 0
                map.Add(triangle.Index1);      // 1
                map.Add(triangle.Index2);      // 2

                points.Add(BezierUtil.GetPoint(.5, bisectedEdges.First(o => o.Item1 == TriangleEdge.Edge_01).Item3));       // 3
                map.Add(points.Count - 1);

                points.Add(BezierUtil.GetPoint(.5, bisectedEdges.First(o => o.Item1 == TriangleEdge.Edge_12).Item3));       // 4
                map.Add(points.Count - 1);

                points.Add(BezierUtil.GetPoint(.5, bisectedEdges.First(o => o.Item1 == TriangleEdge.Edge_20).Item3));       // 5
                map.Add(points.Count - 1);

                Point3D[] pointArr = points.ToArray();

                List<TriangleIndexed> retVal = new List<TriangleIndexed>();

                retVal.Add(MatchNormal(new TriangleIndexed(map[0], map[3], map[5], pointArr), triangle));       // Bottom Left
                retVal.Add(MatchNormal(new TriangleIndexed(map[1], map[4], map[3], pointArr), triangle));       // Top
                retVal.Add(MatchNormal(new TriangleIndexed(map[2], map[5], map[4], pointArr), triangle));       // Bottom Right
                retVal.Add(MatchNormal(new TriangleIndexed(map[3], map[4], map[5], pointArr), triangle));       // Center

                return retVal.ToArray();
            }

            private static TriangleIndexed MatchNormal(TriangleIndexed newTriangle, ITriangle compareTo)
            {
                if (Vector3D.DotProduct(newTriangle.Normal, compareTo.Normal) > 0)
                {
                    return newTriangle;
                }
                else
                {
                    return new TriangleIndexed(newTriangle.Index0, newTriangle.Index2, newTriangle.Index1, newTriangle.AllPoints);
                }
            }

            private static BezierSegment3D GetCurvedEdge(ITriangleIndexed triangle, TriangleEdge edge, ITriangleIndexed[] triangles, SortedList<int, ITriangle> planes, SortedList<Tuple<int, int>, BezierSegment3D> edges, double controlPercent = .25d)
            {
                int fromIndex = triangle.GetIndex(edge, true);
                int toIndex = triangle.GetIndex(edge, false);

                var key = Tuple.Create(Math.Min(fromIndex, toIndex), Math.Max(fromIndex, toIndex));

                BezierSegment3D retVal;
                if (edges.TryGetValue(key, out retVal))
                {
                    return retVal;
                }

                retVal = GetCurvedEdge(triangle, edge, triangles, planes, controlPercent);

                edges.Add(key, retVal);

                return retVal;
            }
            /// <summary>
            /// Each point on the hull will get its own tangent plane.  This will return a bezier with control points
            /// snapped to the coresponding planes.  That way, any beziers that meet at a point will have a smooth
            /// transition
            /// </summary>
            private static BezierSegment3D GetCurvedEdge(ITriangleIndexed triangle, TriangleEdge edge, ITriangleIndexed[] triangles, SortedList<int, ITriangle> planes, double controlPercent = .25d)
            {
                // Points
                int fromIndex = triangle.GetIndex(edge, true);
                int toIndex = triangle.GetIndex(edge, false);

                Point3D fromPoint = triangle.GetPoint(edge, true);
                Point3D toPoint = triangle.GetPoint(edge, false);

                // Snap planes
                ITriangle fromPlane = GetTangentPlane(fromIndex, triangles, planes);
                ITriangle toPlane = GetTangentPlane(toIndex, triangles, planes);

                // Edge line segment
                Vector3D dir = toPoint - fromPoint;

                // Control points
                Point3D fromControl = Math3D.GetClosestPoint_Plane_Point(fromPlane, fromPoint + (dir * controlPercent));
                Point3D toControl = Math3D.GetClosestPoint_Plane_Point(toPlane, toPoint + (-dir * controlPercent));

                return new BezierSegment3D(fromIndex, toIndex, new[] { fromControl, toControl }, triangle.AllPoints);
            }

            private static ITriangle GetTangentPlane(int index, ITriangleIndexed[] triangles, SortedList<int, ITriangle> planes)
            {
                ITriangle retVal;
                if (planes.TryGetValue(index, out retVal))
                {
                    return retVal;
                }

                retVal = GetTangentPlane(index, triangles);

                planes.Add(index, retVal);

                return retVal;
            }
            private static ITriangle GetTangentPlane(int index, ITriangleIndexed[] triangles)
            {
                // Find the triangles that touch this point
                ITriangleIndexed[] touching = triangles.
                    Where(o => o.IndexArray.Contains(index)).
                    ToArray();

                // Get all the points in those triangles that aren't the point passed in
                Point3D[] otherPoints = touching.
                    SelectMany(o => o.IndexArray).
                    Where(o => o != index).
                    Select(o => triangles[0].AllPoints[o]).
                    ToArray();

                // Use those points to define a plane
                ITriangle plane = Math2D.GetPlane_Average(otherPoints);

                // Translate the plane
                Point3D requestPoint = triangles[0].AllPoints[index];
                return new Triangle(requestPoint, requestPoint + (plane.Point1 - plane.Point0), requestPoint + (plane.Point2 - plane.Point0));
            }

            #endregion
        }

        #endregion
        #region Class: RemoveThin

        private static class RemoveThin
        {
            public static ITriangleIndexed[] ThinIt(ITriangleIndexed[] triangles, double skipThinRatio = .9)
            {
                List<ITriangleIndexed> list = new List<ITriangleIndexed>(triangles);

                while (true)
                {
                    var thin = list.
                        Select(o => new { Triangle = o, Edges = ShouldRemoveThin(o, skipThinRatio) }).
                        Where(o => o.Edges != null).
                        FirstOrDefault();

                    if (thin == null)
                    {
                        break;
                    }

                    RemoveThinTriangle(thin.Triangle, thin.Edges, list);
                }

                return list.ToArray();
            }

            #region Private Methods

            /// <summary>
            /// This compares the ratio of the two shorter edges with the longest edge.  If the triangle is too thin,
            /// this method returns the edges in length order
            /// </summary>
            /// <returns>
            /// If too thin: edges from shortest to longest
            /// Otherwise null
            /// </returns>
            private static TriangleEdge[] ShouldRemoveThin(ITriangle triangle, double ratio)
            {
                var lengths = Triangle.Edges.
                    Select(o => new { Edge = o, Length = triangle.GetEdgeLength(o) }).
                    OrderBy(o => o.Length).
                    ToArray();

                // The closer the ratio gets to one, the thinner the triangle is
                if (lengths[2].Length / (lengths[0].Length + lengths[1].Length) >= ratio)
                {
                    return lengths.Select(o => o.Edge).ToArray();
                }
                else
                {
                    return null;
                }
            }

            /// <summary>
            /// This will remove the triangle that is passed in (unless it's already been removed), as well as corner
            /// neighbors.  Then runs a 2D delaunay to retriangulate that polygon.  Then puts those triangles in
            /// the list (as well as relinking the triangles)
            /// </summary>
            /// <param name="edges">The edges from shortest to longest</param>
            private static void RemoveThinTriangle(ITriangleIndexed triangle, TriangleEdge[] edges, List<ITriangleIndexed> allTriangles)
            {
                if (!allTriangles.Contains(triangle))
                {
                    // This triangle has already been removed
                    return;
                }

                // The point that is common between the two shorter edges will get removed
                int removeIndex = triangle.GetCommonIndex(edges[0], edges[1]);

                // Get affected triangles
                ITriangleIndexed[] removes = allTriangles.
                    Where(o => o.IndexArray.Contains(removeIndex)).
                    ToArray();

                // Get the points that aren't the remove point
                int[] otherIndices = removes.
                    SelectMany(o => o.IndexArray).
                    Where(o => o != removeIndex).
                    Distinct().
                    ToArray();

                // Get the edges along the perimiter
                var perimiterEdges = removes.
                    SelectMany(o => Triangle.Edges.Select(p => Tuple.Create(o.GetIndex(p, true), o.GetIndex(p, false)))).
                    Where(o => otherIndices.Contains(o.Item1) && otherIndices.Contains(o.Item2)).
                    ToArray();

                // Get the average of the removed triangle's normals so that the replacement triangles can match the direction
                //NOTE: Not taking the unit so that larger triangles get more influence
                //Vector3D avgNormal = Math3D.GetAverage(removes.Select(o => o.Normal));
                Vector3D avgNormal = Math3D.GetSum(removes.Select(o => o.Normal));

                // Create replacement triangles to fill the gap
                ITriangleIndexed[] adds = GetReplacementPatch(otherIndices, perimiterEdges, triangle, removeIndex, avgNormal);

                // Swap them out
                foreach (ITriangleIndexed remove in removes)
                {
                    allTriangles.Remove(remove);
                }

                allTriangles.AddRange(adds);
            }

            private static ITriangleIndexed[] GetReplacementPatch(int[] perimiter, Tuple<int, int>[] perimiterEdges, ITriangleIndexed triangle, int removeIndex, Vector3D avgNormal)
            {
                if (perimiter.Length < 3)
                {
                    throw new ArgumentException("Need at least 3 perimiter points: " + perimiter.Length.ToString());
                }
                else if (perimiter.Length == 3)
                {
                    return new[] { MatchNormal(new TriangleIndexed(perimiter[0], perimiter[1], perimiter[2], triangle.AllPoints), triangle) };
                }

                // Get points for the delaunay
                Point3D[] perimPoints = perimiter.
                    Select(o => triangle.AllPoints[o]).
                    ToArray();

                // Map the perimiter edges for the triangles generated by delaunay
                SortedList<int, int> perimMap = new SortedList<int, int>();
                for (int cntr = 0; cntr < perimiter.Length; cntr++)
                {
                    perimMap.Add(perimiter[cntr], cntr);
                }

                Tuple<int, int>[] perimiterMapped = perimiterEdges.
                    Select(o => Tuple.Create(perimMap[o.Item1], perimMap[o.Item2])).
                    Select(o => o.Item1 < o.Item2 ? o : Tuple.Create(o.Item2, o.Item1)).
                    ToArray();

                // Use delaunay to triangulate the hole
                Tetrahedron[] tetras = Math3D.GetDelaunay(perimPoints);

                // Throw out interior triangles (delaunay returns good triangles, but too many)
                List<TriangleIndexedLinked> retVal = tetras.
                    SelectMany(o => o.FaceArray).
                    ToLookup(o => o.Token).
                    Where(o => o.Count() == 1).
                    Select(o => o.First()).
                    ToList();

                // Find the triangle that is closest to the remove point
                Point3D removePoint = triangle.AllPoints[removeIndex];

                ITriangleIndexed closest = retVal.
                    Select(o => new { Triangle = o, Distance = Math3D.GetClosestDistance_Triangle_Point(o, removePoint) }).
                    OrderBy(o => o.Distance).
                    First().
                    Triangle;

                // Keep the set of triangles that touch that closest triangle, don't walk past a perimiter
                List<long> keepers = new List<long>();
                GetHemisphere(closest, retVal, keepers, perimiterMapped);

                retVal = retVal.
                    Where(o => keepers.Contains(o.Token)).
                    ToList();

                // Fix the normals
                for (int cntr = 0; cntr < retVal.Count; cntr++)
                {
                    //retVal[cntr] = FixNormal(retVal[cntr], tetras);
                    retVal[cntr] = FixNormal(retVal[cntr], avgNormal);
                }

                // Swap out with allpoints
                return retVal.
                    //Select(o => new TriangleIndexed(perimiter[o.Index0], perimiter[o.Index2], perimiter[o.Index1], triangle.AllPoints)).        // reversing the triangles, because GetDelaunay always returns the normals backward
                    Select(o => new TriangleIndexed(perimiter[o.Index0], perimiter[o.Index1], perimiter[o.Index2], triangle.AllPoints)).
                    ToArray();
            }

            private static void GetHemisphere(ITriangleIndexed current, IEnumerable<ITriangleIndexed> candidates, List<long> visited, Tuple<int, int>[] perimiter)
            {
                visited.Add(current.Token);

                foreach (TriangleEdge edge in Triangle.Edges)
                {
                    int fromIndex = current.GetIndex(edge, true);
                    int toIndex = current.GetIndex(edge, false);

                    Tuple<int, int> edgeKey = Tuple.Create(Math.Min(fromIndex, toIndex), Math.Max(fromIndex, toIndex));

                    // See if this edge sits on the perimiter
                    if (perimiter.Contains(edgeKey))
                    {
                        continue;
                    }

                    // It's not on the perimiter, include the neighbor
                    //NOTE: There should only be one neighbor for each edge by the time this method is called (assuming the list was filtered properly)
                    foreach (ITriangleIndexed neighbor in candidates.Where(o => o.Token != current.Token && o.IndexArray.Contains(fromIndex) && o.IndexArray.Contains(toIndex)))
                    {
                        if (visited.Contains(neighbor.Token))
                        {
                            continue;
                        }

                        // Recurse
                        GetHemisphere(neighbor, candidates, visited, perimiter);
                    }
                }
            }

            private static TriangleIndexed MatchNormal(TriangleIndexed newTriangle, ITriangle compareTo)
            {
                if (Vector3D.DotProduct(newTriangle.Normal, compareTo.Normal) > 0)
                {
                    return newTriangle;
                }
                else
                {
                    return new TriangleIndexed(newTriangle.Index0, newTriangle.Index2, newTriangle.Index1, newTriangle.AllPoints);
                }
            }

            private static TriangleIndexedLinked FixNormal_ORIG(TriangleIndexedLinked triangle, Tetrahedron[] tetras)
            {
                // Find the tetrahedron that this triangle came from
                Tetrahedron owner = tetras.FirstOrDefault(o => o.FaceArray.Any(p => p.Token == triangle.Token));
                if (owner == null)
                {
                    throw new ArgumentException("Didn't find the owning tetrahedron");
                }

                Vector3D dirToCenter = triangle.GetCenterPoint() - owner.GetCenterPoint();

                // Make sure that the normal faces away from the center
                if (Vector3D.DotProduct(triangle.Normal, dirToCenter) < 0)
                {
                    // The triangle is already facing away from the center
                    return triangle;
                }
                else
                {
                    // The triangle is facing toward the center, it needs to face away
                    return new TriangleIndexedLinked(triangle.Index0, triangle.Index2, triangle.Index1, triangle.AllPoints);        //NOTE: There's no need to link up neighbors
                }
            }
            private static TriangleIndexedLinked FixNormal(TriangleIndexedLinked triangle, Vector3D avgRemovedNormals)
            {
                if (Vector3D.DotProduct(triangle.Normal, avgRemovedNormals) > 0)
                {
                    // The triangle is already facing the same direction as what was removed
                    return triangle;
                }
                else
                {
                    // The triangle is facing backward
                    return new TriangleIndexedLinked(triangle.Index0, triangle.Index2, triangle.Index1, triangle.AllPoints);        //NOTE: There's no need to link up neighbors
                }
            }

            #endregion
        }

        #endregion
        #region Class: HullTriangleIntersect

        private static class HullTriangleIntersect
        {
            //NOTE: These will only return a proper polygon, or null (hull-triangle will return null if the triangle is only touching the hull, but not intersecting)

            public static Point3D[] GetIntersection_Hull_Plane(ITriangleIndexed[] convexHull, ITriangle plane)
            {
                List<Tuple<Point3D, Point3D>> lineSegments = GetIntersectingLineSegements_simple(convexHull, plane);
                if (lineSegments == null)
                {
                    return null;
                }

                // Stitch the line segments together
                Point3D[] retVal = GetIntersection_Hull_PlaneSprtStitchSegments(lineSegments);

                if (retVal == null)
                {
                    // In some cases, the above method fails, so call the more generic 2D convex hull method
                    //NOTE: This assumes the hull is convex
                    retVal = GetIntersection_Hull_PlaneSprtConvexHull(lineSegments);
                }

                // Exit Function
                return retVal;      // could still be null
            }

            public static Point3D[] GetIntersection_Hull_Triangle(ITriangleIndexed[] convexHull, ITriangle triangle)
            {
                // Think of the triangle as a plane, and get a polygon of it slicing through the hull
                Point3D[] planePoly = GetIntersection_Hull_Plane(convexHull, triangle);
                if (planePoly == null || planePoly.Length < 3)
                {
                    return null;
                }

                // Now intersect that polygon with the triangle (could return null)
                return Math2D.GetIntersection_Polygon_Triangle(planePoly, triangle);
            }

            public static ContourPolygon[] GetIntersection_Mesh_Plane(ITriangleIndexed[] mesh, ITriangle plane)
            {
                Point3D[][] initalPolys;
                ContourTriangleIntersect[][] initalTriangles;
                if (!GetIntersection_Mesh_Plane_Initial(out initalPolys, out initalTriangles, mesh, plane))
                {
                    return null;
                }

                // Convert to 2D
                Quaternion rotation = Math3D.GetRotation(plane.Normal, new Vector3D(0, 0, 1));
                RotateTransform3D rotateTo2D = new RotateTransform3D(new QuaternionRotation3D(rotation));

                Point[][] initalPolys2D = initalPolys.Select(o => o.Select(p => rotateTo2D.Transform(p).ToPoint2D()).ToArray()).ToArray();

                // Figure out which polys are inside of others (holes)
                Tuple<int, int[]>[] polyIslands = Math2D.GetPolygonIslands(initalPolys2D);

                return polyIslands.
                    Select(o => new ContourPolygon(
                        initalPolys[o.Item1],
                        o.Item2.Select(p => initalPolys[p]).ToArray(),
                        initalPolys2D[o.Item1],
                        o.Item2.Select(p => initalPolys2D[p]).ToArray(),
                        initalTriangles[o.Item1],
                        o.Item2.Select(p => initalTriangles[p]).ToArray(),
                        plane)
                    ).ToArray();
            }

            #region Private Methods

            #region Convex hull methods

            private static List<Tuple<Point3D, Point3D>> GetIntersectingLineSegements_simple(ITriangleIndexed[] triangles, ITriangle plane)
            {
                // Shoot through all the triangles in the hull, and get line segment intersections
                List<Tuple<Point3D, Point3D>> retVal = null;

                if (triangles.Length > 100)
                {
                    retVal = triangles.
                        AsParallel().
                        Select(o => Math3D.GetIntersection_Plane_Triangle(plane, o)).
                        Where(o => o != null).
                        ToList();
                }
                else
                {
                    retVal = triangles.
                        Select(o => Math3D.GetIntersection_Plane_Triangle(plane, o)).
                        Where(o => o != null).
                        ToList();
                }

                if (retVal.Count < 2)
                {
                    // length of 0 is a clear miss, 1 is just touching
                    //NOTE: All the points could be colinear, and just touching the hull, but deeper analysis is needed
                    return null;
                }

                return retVal;
            }

            private static Point3D[] GetIntersection_Hull_PlaneSprtStitchSegments(List<Tuple<Point3D, Point3D>> lineSegments)
            {
                // Need to remove single vertex matches, or the loop below will have problems
                var fixedSegments = lineSegments.Where(o => !Math3D.IsNearValue(o.Item1, o.Item2)).ToList();
                if (fixedSegments.Count == 0)
                {
                    return null;
                }

                List<Point3D> retVal = new List<Point3D>();

                // Stitch the segments together into a polygon
                retVal.Add(fixedSegments[0].Item1);
                Point3D currentPoint = fixedSegments[0].Item2;
                fixedSegments.RemoveAt(0);

                while (fixedSegments.Count > 0)
                {
                    var match = FindAndRemoveMatchingSegment(fixedSegments, currentPoint);
                    if (match == null)
                    {
                        //TODO: If this becomes an issue, make a method that builds fragments, then the final polygon will hop from fragment to fragment
                        //TODO: Or, use Math2D.GetConvexHull - make an overload that rotates the points into the xy plane
                        //throw new ApplicationException("The hull passed in has holes in it");
                        return null;
                    }

                    retVal.Add(match.Item1);
                    currentPoint = match.Item2;
                }

                if (!Math3D.IsNearValue(retVal[0], currentPoint))
                {
                    //throw new ApplicationException("The hull passed in has holes in it");
                    return null;
                }

                if (retVal.Count < 3)
                {
                    return null;
                }

                // Exit Function
                return retVal.ToArray();
            }
            private static Point3D[] GetIntersection_Hull_PlaneSprtConvexHull(List<Tuple<Point3D, Point3D>> lineSegments)
            {
                // Convert the line segments into single points (deduped)
                List<Point3D> points = new List<Point3D>();
                foreach (var segment in lineSegments)
                {
                    if (!points.Any(o => Math3D.IsNearValue(o, segment.Item1)))
                    {
                        points.Add(segment.Item1);
                    }

                    if (!points.Any(o => Math3D.IsNearValue(o, segment.Item2)))
                    {
                        points.Add(segment.Item2);
                    }
                }

                // Build a polygon out of the outermost points
                var hull2D = Math2D.GetConvexHull(points.ToArray());
                if (hull2D == null || hull2D.PerimiterLines.Length == 0)
                {
                    return null;
                }

                // Transform to 3D
                return hull2D.PerimiterLines.Select(o => hull2D.GetTransformedPoint(hull2D.Points[o])).ToArray();
            }

            /// <summary>
            /// This compares the test point to each end of each line segment.  Then removes that matching segment from the list
            /// </summary>
            /// <returns>
            /// null, or:
            /// Item1=Common Point
            /// Item2=Other Point
            /// </returns>
            private static Tuple<Point3D, Point3D> FindAndRemoveMatchingSegment(List<Tuple<Point3D, Point3D>> lineSegments, Point3D testPoint)
            {
                for (int cntr = 0; cntr < lineSegments.Count; cntr++)
                {
                    if (Math3D.IsNearValue(lineSegments[cntr].Item1, testPoint))
                    {
                        Tuple<Point3D, Point3D> retVal = lineSegments[cntr];
                        lineSegments.RemoveAt(cntr);
                        return retVal;
                    }

                    if (Math3D.IsNearValue(lineSegments[cntr].Item2, testPoint))
                    {
                        Tuple<Point3D, Point3D> retVal = Tuple.Create(lineSegments[cntr].Item2, lineSegments[cntr].Item1);
                        lineSegments.RemoveAt(cntr);
                        return retVal;
                    }
                }

                // No match found
                return null;
            }

            #endregion
            #region Mesh methods

            private static bool GetIntersection_Mesh_Plane_Initial(out Point3D[][] polygons, out ContourTriangleIntersect[][] intersectedTriangles, ITriangleIndexed[] mesh, ITriangle plane)
            {
                List<Tuple<Point3D, Point3D, ITriangle>> lineSegments = GetIntersectingLineSegements(mesh, plane);
                if (lineSegments == null || lineSegments.Count == 0)
                {
                    polygons = null;
                    intersectedTriangles = null;
                    return false;
                }

                Point3D[] allPoints;
                List<Tuple<int, int, ITriangle>> lineInts = ConvertToInts(out allPoints, lineSegments);

                //TODO: Make sure there are no segments with the same point

                List<Tuple<Point3D[], ContourTriangleIntersect[]>> polys = new List<Tuple<Point3D[], ContourTriangleIntersect[]>>();

                while (lineInts.Count > 0)
                {
                    var poly = GetNextPoly(lineInts, allPoints);
                    if (poly == null)
                    {
                        //return null;
                        continue;
                    }

                    polys.Add(poly);
                }

                polygons = polys.Select(o => o.Item1).ToArray();
                intersectedTriangles = polys.Select(o => o.Item2).ToArray();
                return true;
            }

            private static List<Tuple<Point3D, Point3D, ITriangle>> GetIntersectingLineSegements(ITriangleIndexed[] triangles, ITriangle plane)
            {
                // Shoot through all the triangles in the hull, and get line segment intersections
                List<Tuple<Point3D, Point3D, ITriangle>> retVal = null;

                if (triangles.Length > 100)
                {
                    retVal = triangles.
                        AsParallel().
                        Select(o => new { Intersect = Math3D.GetIntersection_Plane_Triangle(plane, o), Triangle = o }).
                        Where(o => o.Intersect != null).
                        Select(o => Tuple.Create(o.Intersect.Item1, o.Intersect.Item2, (ITriangle)o.Triangle)).
                        ToList();
                }
                else
                {
                    retVal = triangles.
                        Select(o => new { Intersect = Math3D.GetIntersection_Plane_Triangle(plane, o), Triangle = o }).
                        Where(o => o.Intersect != null).
                        Select(o => Tuple.Create(o.Intersect.Item1, o.Intersect.Item2, (ITriangle)o.Triangle)).
                        ToList();
                }

                if (retVal.Count < 2)
                {
                    // length of 0 is a clear miss, 1 is just touching
                    //NOTE: All the points could be colinear, and just touching the hull, but deeper analysis is needed
                    return null;
                }

                return retVal;
            }

            private static List<Tuple<int, int, ITriangle>> ConvertToInts(out Point3D[] allPoints, List<Tuple<Point3D, Point3D, ITriangle>> lineSegments)
            {
                List<Point3D> dedupedPoints = new List<Point3D>();

                List<Tuple<int, int, ITriangle>> retVal = new List<Tuple<int, int, ITriangle>>();

                foreach (var segment in lineSegments)
                {
                    retVal.Add(Tuple.Create(ConvertToIntsSprtIndex(dedupedPoints, segment.Item1), ConvertToIntsSprtIndex(dedupedPoints, segment.Item2), segment.Item3));
                }

                allPoints = dedupedPoints.ToArray();
                return retVal;
            }
            private static int ConvertToIntsSprtIndex(List<Point3D> deduped, Point3D point)
            {
                for (int cntr = 0; cntr < deduped.Count; cntr++)
                {
                    if (Math3D.IsNearValue(deduped[cntr], point))
                    {
                        return cntr;
                    }
                }

                deduped.Add(point);
                return deduped.Count - 1;
            }

            private static Tuple<Point3D[], ContourTriangleIntersect[]> GetNextPoly(List<Tuple<int, int, ITriangle>> segments, Point3D[] allPoints)
            {
                List<int> polygon = new List<int>();
                List<ContourTriangleIntersect> triangles = new List<ContourTriangleIntersect>();

                polygon.Add(segments[0].Item1);
                int currentPoint = segments[0].Item2;
                ContourTriangleIntersect currentTriangle = new ContourTriangleIntersect(allPoints[segments[0].Item1], allPoints[segments[0].Item2], segments[0].Item3);
                segments.RemoveAt(0);

                while (true)
                {
                    polygon.Add(currentPoint);
                    triangles.Add(currentTriangle);

                    // Find the segment that contains currentPoint, and get the other end
                    Tuple<int, ContourTriangleIntersect> nextPoint = GetNextPolySprtNextSegment(segments, currentPoint, allPoints);        //NOTE: This removes the match from segments
                    if (nextPoint == null)
                    {
                        if (polygon.Count < 3)
                        {
                            // This only happens when the intersection is along a single triangle (where the plane just barely touches a triangle)
                            return null;
                        }
                        else
                        {
                            // This is a fragment of a polygon.  Exit early, and it will be considered a full polygon
                            break;
                        }
                    }

                    if (nextPoint.Item1 == polygon[0])
                    {
                        // This polygon is complete
                        triangles.Add(nextPoint.Item2);     // polygon is a list of points, but triangles is a list of segments.  So polygon infers this last segment, but triangles needs to have it explicitely added
                        break;
                    }

                    currentPoint = nextPoint.Item1;
                    currentTriangle = nextPoint.Item2;
                }

                return Tuple.Create(polygon.Select(o => allPoints[o]).ToArray(), triangles.ToArray());
            }
            private static Tuple<int, ContourTriangleIntersect> GetNextPolySprtNextSegment(List<Tuple<int, int, ITriangle>> segments, int currentPoint, Point3D[] allPoints)
            {
                int? nextPoint = null;
                int index = -1;

                for (int cntr = 0; cntr < segments.Count; cntr++)
                {
                    if (segments[cntr].Item1 == currentPoint)
                    {
                        // Found at 1, return point 2
                        nextPoint = segments[cntr].Item2;
                        index = cntr;
                        break;
                    }
                    else if (segments[cntr].Item2 == currentPoint)
                    {
                        // Found at 2, return point 1
                        nextPoint = segments[cntr].Item1;
                        index = cntr;
                        break;
                    }
                }

                if (nextPoint == null)
                {
                    return null;
                }
                else
                {
                    // Grab the triangle for this segment
                    ContourTriangleIntersect triangle = new ContourTriangleIntersect(allPoints[currentPoint], allPoints[nextPoint.Value], segments[index].Item3);

                    // Remove this segment from the list of remaining segments
                    segments.RemoveAt(index);

                    return Tuple.Create(nextPoint.Value, triangle);
                }
            }

            #endregion
            #region Mesh methods - OLD

            //public static Point3D[][] GetIntersection_Mesh_Plane_OLD(ITriangleIndexed[] mesh, ITriangle plane)
            //{
            //    List<Tuple<Point3D, Point3D>> lineSegments = GetIntersectingLineSegements(mesh, plane);
            //    if (lineSegments == null || lineSegments.Count == 0)
            //    {
            //        return null;
            //    }

            //    Point3D[] allPoints;
            //    List<Tuple<int, int>> lineInts = ConvertToInts(out allPoints, lineSegments);

            //    //TODO: Make sure there are no segments with the same point

            //    List<Point3D[]> polys = new List<Point3D[]>();

            //    while (lineInts.Count > 0)
            //    {
            //        Point3D[] poly = GetNextPoly(lineInts, allPoints);
            //        if (poly == null)
            //        {
            //            //return null;
            //            continue;
            //        }

            //        polys.Add(poly);
            //    }

            //    return polys.ToArray();
            //}



            //private static List<Tuple<int, int>> ConvertToInts(out Point3D[] allPoints, List<Tuple<Point3D, Point3D>> lineSegments)
            //{
            //    List<Point3D> dedupedPoints = new List<Point3D>();

            //    List<Tuple<int, int>> retVal = new List<Tuple<int, int>>();

            //    foreach (var segment in lineSegments)
            //    {
            //        retVal.Add(Tuple.Create(ConvertToIntsSprtIndex(dedupedPoints, segment.Item1), ConvertToIntsSprtIndex(dedupedPoints, segment.Item2)));
            //    }

            //    allPoints = dedupedPoints.ToArray();
            //    return retVal;
            //}
            //private static int ConvertToIntsSprtIndex(List<Point3D> deduped, Point3D point)
            //{
            //    for (int cntr = 0; cntr < deduped.Count; cntr++)
            //    {
            //        if (Math3D.IsNearValue(deduped[cntr], point))
            //        {
            //            return cntr;
            //        }
            //    }

            //    deduped.Add(point);
            //    return deduped.Count - 1;
            //}

            //private static Point3D[] GetNextPoly(List<Tuple<int, int>> segments, Point3D[] allPoints)
            //{
            //    List<int> retVal = new List<int>();

            //    retVal.Add(segments[0].Item1);
            //    int currentPoint = segments[0].Item2;
            //    segments.RemoveAt(0);

            //    while (true)
            //    {
            //        retVal.Add(currentPoint);

            //        // Find the segment that contains currentPoint, and get the other end
            //        int? nextPoint = GetNextPolySprtNextSegment(segments, currentPoint);        //NOTE: This removes the match from segments
            //        if (nextPoint == null)
            //        {
            //            if (retVal.Count < 3)
            //            {
            //                // This only happens when the intersection is along a single triangle (where the plane just barely touches a triangle)
            //                return null;
            //            }
            //            else
            //            {
            //                // This is a fragment of a polygon.  Exit early, and it will be considered a full polygon
            //                break;
            //            }
            //        }

            //        if (nextPoint.Value == retVal[0])
            //        {
            //            // This polygon is complete
            //            break;
            //        }

            //        currentPoint = nextPoint.Value;
            //    }

            //    return retVal.Select(o => allPoints[o]).ToArray();
            //}
            //private static int? GetNextPolySprtNextSegment(List<Tuple<int, int>> segments, int currentPoint)
            //{
            //    int? retVal = null;

            //    for (int cntr = 0; cntr < segments.Count; cntr++)
            //    {
            //        if (segments[cntr].Item1 == currentPoint)
            //        {
            //            retVal = segments[cntr].Item2;
            //            segments.RemoveAt(cntr);
            //            break;
            //        }
            //        else if (segments[cntr].Item2 == currentPoint)
            //        {
            //            retVal = segments[cntr].Item1;
            //            segments.RemoveAt(cntr);
            //            break;
            //        }
            //    }

            //    return retVal;
            //}

            #endregion

            #endregion
        }

        #endregion
        #region Class: DelaunayVoronoi3D

        /// <remarks>
        /// This uses MIConvexHull, which is under GNU Lesser General Public License
        /// https://miconvexhull.codeplex.com/
        /// </remarks>
        private static class DelaunayVoronoi3D
        {
            #region Class: Vertex

            private class Vertex : Game.HelperClassesWPF.MIConvexHull.IVertex
            {
                public Vertex(int index, Point3D point)
                {
                    this.Index = index;
                    this.Point = point;

                    this.Position = new[] { point.X, point.Y, point.Z };
                }

                public readonly int Index;
                public readonly Point3D Point;

                // This is what the interface expects
                public double[] Position { get; set; }
            }

            #endregion
            #region Class: Cell

            private class Cell : Game.HelperClassesWPF.MIConvexHull.TriangulationCell<Vertex, Cell>
            {
                // These get populated after the voronoi result is returned
                public int Index = -1;
                public Point3D Circumcenter = new Point3D(0, 0, 0);
            }

            #endregion

            #region Class: EdgeIndex

            private class EdgeIndex : IComparable<EdgeIndex>, IComparable, IEquatable<EdgeIndex>
            {
                public EdgeIndex(int index, Edge3D edge)
                {
                    this.Index = index;
                    this.Edge = edge;
                }

                public readonly int Index;
                public readonly Edge3D Edge;

                #region IComparable<EdgeIndex> Members

                public int CompareTo(EdgeIndex other)
                {
                    throw new NotImplementedException();
                }

                #endregion

                #region IComparable Members

                public int CompareTo(object obj)
                {
                    throw new NotImplementedException();
                }

                #endregion

                #region IEquatable<EdgeIndex> Members

                public bool Equals(EdgeIndex other)
                {
                    throw new NotImplementedException();
                }

                #endregion
            }

            #endregion

            public static Tetrahedron[] GetDelaunay(Point3D[] points)
            {
                SortedList<Tuple<int, int, int>, TriangleIndexedLinked> triangleDict = new SortedList<Tuple<int, int, int>, TriangleIndexedLinked>();

                if (points == null)
                {
                    throw new ArgumentNullException("points");
                }
                else if (points.Length < 4)
                {
                    throw new ArgumentException("Must have at least 4 points: " + points.Length.ToString());
                }
                else if (points.Length == 4)
                {
                    // The MIConvexHull library needs at least 5 points.  Just manually create a tetrahedron
                    return new[] { new Tetrahedron(0, 1, 2, 3, points, triangleDict) };
                }

                // Convert the points into something the MIConvexHull library can use
                Vertex[] vertices = points.Select((o, i) => new Vertex(i, o)).ToArray();

                // Run the delaunay
                var tetrahedrons1 = Game.HelperClassesWPF.MIConvexHull.Triangulation.CreateDelaunay<Vertex>(vertices).Cells.ToArray();

                // Convert into my classes
                Tetrahedron[] tetrahedrons2 = tetrahedrons1.Select(o => new Tetrahedron(o.Vertices[0].Index, o.Vertices[1].Index, o.Vertices[2].Index, o.Vertices[3].Index, points, triangleDict)).ToArray();

                // Link them
                for (int cntr = 0; cntr < tetrahedrons1.Length; cntr++)
                {
                    foreach (var neighbor1 in tetrahedrons1[cntr].Adjacency)
                    {
                        if (neighbor1 == null)
                        {
                            // This is on the edge of the hull (all of tetrahedrons2 neighbors start out as null, so it's the non nulls that
                            // need something done to them)
                            continue;
                        }

                        int[] indecies = neighbor1.Vertices.Select(o => o.Index).OrderBy(o => o).ToArray();

                        // Find the tetra2
                        Tetrahedron neighbor2 = tetrahedrons2.First(o => o.IsMatch(indecies));

                        tetrahedrons2[cntr].SetNeighbor(neighbor2);
                    }
                }

                return tetrahedrons2;
            }

            public static VoronoiResult3D GetVoronoi(Point3D[] points, bool buildFaces)
            {
                // Delaunay
                Tetrahedron[] delaunay = GetDelaunay(points);

                // Voronoi
                var voronoiMesh = Game.HelperClassesWPF.MIConvexHull.VoronoiMesh.Create<Vertex, Cell>(points.Select((o, i) => new Vertex(i, o)));

                // Calculate the centers
                int index = 0;
                foreach (Cell cell in voronoiMesh.Vertices)
                {
                    cell.Index = index;
                    cell.Circumcenter = Math3D.GetCircumsphere(cell.Vertices[0].Point, cell.Vertices[1].Point, cell.Vertices[2].Point, cell.Vertices[3].Point).Item1;
                    index++;
                }

                Point3D[] edgePoints = voronoiMesh.Vertices.Select(o => o.Circumcenter).ToArray();


                // Edges
                var interiorSegments = voronoiMesh.Edges.Select(o => new Edge3D(o.Source.Index, o.Target.Index, edgePoints));

                // Rays
                var rays = GetRays(voronoiMesh, delaunay, edgePoints);

                Edge3D[] edges = UtilityCore.Iterate(interiorSegments, rays).ToArray();

                // Faces
                Face3D[] faces = null;
                int[][] facesByControlPoint = null;
                if (buildFaces)
                {
                    Tuple<Face3D[], int[][]> faces2 = GetFaces(points, edges, edgePoints, voronoiMesh);
                    faces = faces2.Item1;
                    facesByControlPoint = faces2.Item2;
                }

                return new VoronoiResult3D(points, edgePoints, edges, faces, facesByControlPoint, delaunay);
            }

            #region Private Methods - Get Rays

            private static IEnumerable<Edge3D> GetRays(HelperClassesWPF.MIConvexHull.VoronoiMesh<Vertex, Cell, HelperClassesWPF.MIConvexHull.VoronoiEdge<Vertex, Cell>> voronoiMesh, Tetrahedron[] delaunay, Point3D[] allCircumcenters)
            {
                List<Edge3D> retVal = new List<Edge3D>();

                foreach (var cell in voronoiMesh.Vertices)
                {
                    int[] nulls = Enumerable.Range(0, cell.Adjacency.Length).Where(o => cell.Adjacency[o] == null).ToArray();

                    if (nulls.Length == 0)
                    {
                        continue;
                    }

                    Point3D rayStart = cell.Circumcenter;

                    Point3D cellCenter = Math3D.GetCenter(cell.Vertices.Select(o => o.Point));

                    Vertex[] neighbors = cell.Vertices.Where((_, i) => !nulls.Contains(i)).ToArray();

                    // Figure out how many rays to create
                    Vector3D[] directions;
                    switch (neighbors.Length)
                    {
                        case 1:     // 3 rays
                            directions = GetRays_OneNeighbor(rayStart, cellCenter, neighbors, delaunay);
                            break;

                        case 2:     // 2 rays
                            directions = GetRays_TwoNeighbors(rayStart, cellCenter, neighbors, delaunay);
                            break;

                        case 3:     // 1 ray
                            directions = GetRays_ThreeNeighbors(rayStart, cellCenter, neighbors);
                            break;

                        default:
                            throw new ApplicationException("Unexpected number of neighbors: " + neighbors.Length.ToString());
                    }

                    retVal.AddRange(directions.Select(o => new Edge3D(EdgeType.Ray, cell.Index, o, allCircumcenters)));
                }

                return retVal;
            }

            private static Vector3D[] GetRays_OneNeighbor(Point3D rayStart, Point3D cellCenter, Vertex[] neighbors, Tetrahedron[] delaunay)
            {
                // Find the triangles that have the neighbor point (should always be three triangles)
                var delaunayNeighbors = delaunay.
                    SelectMany(o => o.FaceArray).
                    Where(o => o.IndexArray.Contains(neighbors[0].Index)).
                    ToArray();

                if (delaunayNeighbors.Length != 3)
                {
                    throw new ApplicationException("Should have found three triangles: " + delaunayNeighbors.Length.ToString());
                }

                return delaunayNeighbors.Select(o => GetRayDirection(rayStart, cellCenter, o)).ToArray();
            }
            private static Vector3D[] GetRays_TwoNeighbors(Point3D rayStart, Point3D cellCenter, Vertex[] neighbors, Tetrahedron[] delaunay)
            {
                // Find the triangles that have both points from neighbors (should always be two triangles)
                var delaunayNeighbors = delaunay.
                    SelectMany(o => o.FaceArray).
                    Where(o => o.IndexArray.Contains(neighbors[0].Index) && o.IndexArray.Contains(neighbors[1].Index)).
                    ToArray();

                if (delaunayNeighbors.Length != 2)
                {
                    throw new ApplicationException("Should have found two triangles: " + delaunayNeighbors.Length.ToString());
                }

                return delaunayNeighbors.Select(o => GetRayDirection(rayStart, cellCenter, o)).ToArray();
            }
            private static Vector3D[] GetRays_ThreeNeighbors(Point3D rayStart, Point3D cellCenter, Vertex[] neighbors)
            {
                // The one and two neighbor methods had to scan the delaunay to find the corresponding triangles.
                // But when all three neighbor points are known, the delanauy triangle will be those three neighbor points.
                // So, saving a scan, and just creating a temp triangle
                return new[] { GetRayDirection(rayStart, cellCenter, new Triangle(neighbors[0].Point, neighbors[1].Point, neighbors[2].Point)) };
            }

            private static Vector3D GetRayDirection(Point3D rayStart, Point3D cellCenter, ITriangle triangle)
            {
                Vector3D retVal = triangle.Normal;

                // The check below needs the ray pointing in a consistent direction
                if (Vector3D.DotProduct(retVal, Math3D.GetClosestPoint_Plane_Point(triangle, rayStart) - rayStart) < 0)
                {
                    retVal = -retVal;
                }

                // See if the line between circumcenter and centroid passes through that plane (see if they are on the same side
                // of the plane, or opposing sides)
                bool isAbove1 = Math3D.DistanceFromPlane(triangle, rayStart) > 0d;
                bool isAbove2 = Math3D.DistanceFromPlane(triangle, cellCenter) > 0d;

                if (isAbove1 != isAbove2)
                {
                    retVal = -retVal;
                }

                return retVal;
            }

            #endregion
            #region Private Methods - Get Faces

            private static Tuple<Face3D[], int[][]> GetFaces(Point3D[] controlPoints, Edge3D[] edges, Point3D[] edgePoints, HelperClassesWPF.MIConvexHull.VoronoiMesh<Vertex, Cell, HelperClassesWPF.MIConvexHull.VoronoiEdge<Vertex, Cell>> voronoiMesh)
            {
                List<Tuple<int[], Face3D>> uniqueFaces = new List<Tuple<int[], Face3D>>();

                int[][] facesByCtrlPoint = new int[controlPoints.Length][];

                for (int cntr = 0; cntr < controlPoints.Length; cntr++)
                {
                    // Figure out which edge points are used by which control points
                    int[] edgePointIndices = voronoiMesh.Vertices.        // Vertices is a misleading name.  These are 4 point cells (tetrahedrons).  The circumcenter of each cell is an edge point
                        Where(p => p.Vertices.Any(q => q.Index == cntr)).      // the 4 points of this cell are the control points.  Grab every cell where one of its 4 points is the desired control point
                        Select(p => p.Index).       // store the index of this cell (edge point)
                        ToArray();

                    // Generate faces out of these edges (each face is a coplanar polygon, but I don't want to call them polygons, because they
                    // could be U shaped that start and stop with a ray)
                    Face3D[] faces = GetFaces_CtrlPoint(edgePointIndices, edges, controlPoints[cntr]);

                    // Only store the unique faces, link to any existing
                    facesByCtrlPoint[cntr] = AddUniqueFaces(uniqueFaces, faces);
                }

                return Tuple.Create(uniqueFaces.Select(o => o.Item2).ToArray(), facesByCtrlPoint);
            }

            /// <summary>
            /// This returns all the faces for one control point
            /// </summary>
            private static Face3D[] GetFaces_CtrlPoint(int[] edgePointIndices, Edge3D[] allEdges, Point3D controlPoint)
            {
                #region Get edges

                // Get the edges that are represented by these indices
                var edges = allEdges.
                    Select((o, i) => new EdgeIndex(i, o)).
                    Where(o =>
                    {
                        switch (o.Edge.EdgeType)
                        {
                            case EdgeType.Segment:
                                return edgePointIndices.Contains(o.Edge.Index0) && edgePointIndices.Contains(o.Edge.Index1.Value);

                            case EdgeType.Ray:
                                return edgePointIndices.Contains(o.Edge.Index0);

                            case EdgeType.Line:
                                throw new ApplicationException("Lines aren't supported");

                            default:
                                throw new ApplicationException("Unknown EdgeType: " + o.Edge.EdgeType.ToString());
                        }
                    }).ToArray();

                #endregion

                List<Face3D> retVal = new List<Face3D>();

                #region Chains

                string report = string.Join("\r\n", edges.Select(o => o.Index.ToString() + " : " + o.Edge.ToString()));

                foreach (var segment in edges.Where(o => o.Edge.EdgeType == EdgeType.Segment))
                {
                    retVal.AddRange(WalkChains(segment, edges, allEdges, retVal));
                }

                #endregion
                #region Multi rays

                // Find multiple rays tied to a single point
                var multiRays = edges.
                    Where(o => o.Edge.EdgeType == EdgeType.Ray).
                    GroupBy(o => o.Edge.Index0).
                    Where(o => o.Count() > 1);

                foreach (var multiRay in multiRays)
                {
                    switch (multiRay.Count())
                    {
                        case 2:
                            retVal.Add(new Face3D(multiRay.Select(o => o.Index).ToArray(), allEdges));
                            break;

                        case 3:
                            retVal.AddRange(UtilityCore.GetPairs(multiRay.ToArray()).
                                Select(o => new Face3D(new[] { o.Item1.Index, o.Item2.Index }, allEdges)));
                            break;

                        default:
                            throw new ApplicationException("Finish this: " + multiRay.Count().ToString());
                    }
                }

                #endregion

                #region Remove extra faces

                while (true)
                {
                    // Search for edges that are owned by more than two faces
                    var extraFacesByEdge = edges.
                        Select(o => new
                        {
                            Edge = o,
                            Faces = retVal.Where(p => p.Edges.Any(q => AreEqual(q, o.Edge))).ToArray()
                        }).
                        Where(o => o.Faces.Length > 2).
                        ToList();

                    if (extraFacesByEdge.Count == 0)
                    {
                        break;
                    }

                    foreach (var extra in extraFacesByEdge)
                    {
                        RemoveExtraFace(extra.Edge, extra.Faces, controlPoint, retVal);
                    }
                }

                #endregion

                return retVal.ToArray();
            }

            /// <summary>
            /// This takes a starting segment, and returns all faces that contain that segment
            /// </summary>
            /// <remarks>
            /// A face can either begin and end with rays, or can be a loop of segments.  This method walks in one direction:
            ///     If it finds a ray, it walks the other direction to find the other ray
            ///     If it loops back to the original segment, then it stops with that face
            ///     
            /// While walking, there will be other segments and rays that branch (not coplanar with the currently walked chain).  Those
            /// are ignored.  This method will get called for all segments, so those other branches will get picked up in another run
            /// </remarks>
            private static Face3D[] WalkChains(EdgeIndex start, EdgeIndex[] edges, Edge3D[] allEdges, IEnumerable<Face3D> finishedFaces)
            {
                var nextLeft = GetNext_Point_Edges(start, edges);

                List<Face3D> retVal = new List<Face3D>();

                foreach (EdgeIndex nextLeftEdge in nextLeft.Item2)
                {
                    if (HasSeenEdges(start.Edge, nextLeftEdge.Edge, finishedFaces))
                    {
                        continue;
                    }

                    Vector3D axis = GetAxisUnit(start.Edge, nextLeftEdge.Edge);

                    #region Walk left

                    Tuple<bool, EdgeIndex[]> partialLeft;
                    if (nextLeftEdge.Edge.EdgeType == EdgeType.Ray)
                    {
                        // Found a ray immediately, no need to walk
                        partialLeft = Tuple.Create(false, new[] { nextLeftEdge });
                    }
                    else
                    {
                        // This is a segment, call a method that will keep going
                        partialLeft = WalkChain(start, nextLeftEdge, nextLeft.Item1, axis, true, edges, true);
                    }

                    #endregion

                    if (partialLeft.Item1)
                    {
                        // Going left formed a loop
                        retVal.Add(BuildFace(allEdges, partialLeft.Item2, start));
                    }
                    else
                    {
                        #region Walk right

                        // Going left ended in a ray.  Go right to find the other ray (but only the path with the same axis)
                        var partialRight = WalkChain(start, start, null, axis, false, edges, false);
                        if (partialRight == null)
                        {
                            // This happens when partialLeft is for a ray that belongs to a different control point
                            continue;
                        }

                        #region asserts
#if DEBUG
                        if (partialRight.Item1)
                        {
                            throw new ApplicationException("When walking right, there should never be a loop");
                        }
#endif
                        #endregion

                        // Stitch the two chains together (both end in a ray)
                        retVal.Add(BuildFace(allEdges, partialLeft.Item2, start, partialRight.Item2));

                        #endregion
                    }
                }

                return retVal.ToArray();
            }

            /// <summary>
            /// This walks in one direction, and recurses for each edge it encounters that is along the same plane as it's walking.
            /// It stops when it encounters a ray, or loops back to the original segment
            /// </summary>
            /// <returns>
            /// Item1 = Whether the face is a loop (if false, the last element of Item2 is a ray)
            /// Item2 = Edges that make a face
            /// </returns>
            private static Tuple<bool, EdgeIndex[]> WalkChain(EdgeIndex start, EdgeIndex current, int? commonPointIndex, Vector3D axis_PrevCurrent, bool isLeft, EdgeIndex[] edges, bool includeCurrentInReturn = false)
            {
                #region asserts
#if DEBUG
                if (current.Edge.EdgeType != EdgeType.Segment)
                {
                    throw new ApplicationException("current must be type segment: " + current.Edge.EdgeType.ToString());
                }
#endif
                #endregion

                List<EdgeIndex> returnChain = new List<EdgeIndex>();
                bool? endedAsLoop = null;

                if (includeCurrentInReturn)
                {
                    returnChain.Add(current);
                }

                // Find segments tied to the part of current that isn't commonPointIndex
                var next = GetNext_Point_Edges(current, edges, isLeft, commonPointIndex);

                foreach (var nextEdge in next.Item2)
                {
                    if (nextEdge.Index == start.Index)
                    {
                        #region asserts
#if DEBUG
                        if (!Math1D.IsNearValue(Math.Abs(Vector3D.DotProduct(axis_PrevCurrent, GetAxisUnit(current.Edge, nextEdge.Edge))), 1d))
                        {
                            throw new ApplicationException("Looped around, but it's not coplanar");
                        }
#endif
                        #endregion

                        // Looped around.  Don't add this to the return list
                        endedAsLoop = true;
                        continue;       // can't return yet, because there may still be branches
                    }

                    Vector3D axis_CurrentNext = GetAxisUnit(current.Edge, nextEdge.Edge);

                    // See if they are the same axis (taking abs, because the vector may be in the opposite direction)
                    if (Math1D.IsNearValue(Math.Abs(Vector3D.DotProduct(axis_PrevCurrent, axis_CurrentNext)), 1d))
                    {
                        if (endedAsLoop != null)
                        {
                            throw new ApplicationException("Found two links in the same plane");
                        }

                        returnChain.Add(nextEdge);

                        if (nextEdge.Edge.EdgeType == EdgeType.Ray)
                        {
                            // Ray, the chain is ended
                            endedAsLoop = false;
                        }
                        else if (nextEdge.Edge.EdgeType == EdgeType.Segment)
                        {
                            // Segment, recurse
                            var recursedResult = WalkChain(start, nextEdge, next.Item1, axis_CurrentNext, isLeft, edges);

                            returnChain.AddRange(recursedResult.Item2);
                            endedAsLoop = recursedResult.Item1;
                        }
                        else
                        {
                            throw new ApplicationException("Unexpected EdgeType: " + nextEdge.Edge.EdgeType.ToString());
                        }

#if !DEBUG
                        // No need to keep looping.  All other possibilities are different planes (continuing to loop in debug mode as a verification)
                        break;
#endif
                    }
                }

                if (endedAsLoop == null)
                {
                    // This is sometimes a legitimate case.  When getting edges by control point, multirays sometimes have a ray that is for a different
                    // control point.  This if statement should only hit when walking right (because walking left found a ray)
                    if (isLeft)
                    {
                        throw new ApplicationException("Didn't find the next link in the chain");
                    }

                    return null;
                }

                return Tuple.Create(endedAsLoop.Value, returnChain.ToArray());
            }

            private static bool RemoveExtraFace(EdgeIndex edge, Face3D[] faces, Point3D controlPoint, List<Face3D> returnFaces)
            {
                ITriangle plane = Math3D.GetPlane(edge.Edge.Point0, edge.Edge.DirectionExt);

                Point3D pointOnPlane = Math3D.GetClosestPoint_Plane_Point(plane, controlPoint);
                Vector3D flattenedControl = pointOnPlane - edge.Edge.Point0;

                Tuple<int, double>[] angles = new Tuple<int, double>[faces.Length];

                Vector3D? prevCross = null;

                for (int cntr = 0; cntr < faces.Length; cntr++)
                {
                    // Get an arbitrary edge from this face (that isn't the edge passed in)
                    Edge3D otherEdge = faces[cntr].Edges.First(o => !AreEqual(o, edge.Edge));

                    Point3D otherPoint = Edge3D.GetOtherPointExt(otherEdge, edge.Edge);

                    // Project that onto the test plane
                    pointOnPlane = Math3D.GetClosestPoint_Plane_Point(plane, otherPoint);

                    Vector3D flattenedDirection = pointOnPlane - edge.Edge.Point0;

                    // Crossing all the vectors with the control.  Since they are all crossed the same, the output vectors will all be in
                    // the same direction
                    Vector3D cross = Vector3D.CrossProduct(flattenedDirection, flattenedControl);

                    double angle = Vector3D.AngleBetween(flattenedDirection, flattenedControl);

                    if (prevCross == null)
                    {
                        prevCross = cross;
                    }
                    else
                    {
                        if (Vector3D.DotProduct(prevCross.Value, cross) < 0)
                        {
                            angle = -angle;
                        }
                    }

                    angles[cntr] = Tuple.Create(cntr, angle);
                }

                // Keep the smallest positive and smallest negative angle
                var smallestNeg = angles.Where(o => o.Item2 < 0).OrderByDescending(o => o.Item2).FirstOrDefault();
                var smallestPos = angles.Where(o => o.Item2 > 0).OrderBy(o => o.Item2).FirstOrDefault();

                if (smallestNeg == null || smallestPos == null)
                {
                    throw new ApplicationException("Should always find at least one negative and one positive angle");
                }

                bool retVal = false;

                for (int cntr = 0; cntr < faces.Length; cntr++)
                {
                    if (cntr == smallestNeg.Item1 || cntr == smallestPos.Item1)
                    {
                        continue;
                    }

                    retVal |= RemoveFace(faces[cntr], returnFaces);
                }

                return retVal;
            }

            /// <summary>
            /// This returns edges that touch one of the ends of the segment passed in (current must be of type segment)
            /// NOTE: This only returns edges that touch one side of current, not both
            /// </summary>
            /// <remarks>
            /// This gets called from several places.  If it's called for the first time, then isLeft becomes important (left is index0, right is index1)
            /// If it's called while walking a chain, then commonPointIndex becomes important (returns for index that isn't commonPointIndex)
            /// </remarks>
            private static Tuple<int, EdgeIndex[]> GetNext_Point_Edges(EdgeIndex current, EdgeIndex[] edges, bool isLeft = true, int? commonPointIndex = null)
            {
                #region asserts
#if DEBUG
                if (current.Edge.EdgeType != EdgeType.Segment)
                {
                    throw new ArgumentException("Only segments can be passed to this method: " + current.Edge.EdgeType.ToString());
                }
#endif
                #endregion

                int nextPoint;
                if (commonPointIndex == null)
                {
                    nextPoint = isLeft ? current.Edge.Index0 : current.Edge.Index1.Value;
                }
                else if (current.Edge.Index0 == commonPointIndex.Value)
                {
                    nextPoint = current.Edge.Index1.Value;
                }
                else
                {
                    nextPoint = current.Edge.Index0;
                }

                // Find the edges that have this point
                EdgeIndex[] nextEdges = edges.
                    Where(o => o.Index != current.Index && o.Edge.ContainsPoint(nextPoint)).
                    ToArray();

                return Tuple.Create(nextPoint, nextEdges);
            }

            private static bool HasSeenEdges(Edge3D edge1, Edge3D edge2, IEnumerable<Face3D> faces)
            {
                foreach (Face3D face in faces)
                {
                    // See if this face contains both edges
                    if (face.Edges.Any(o => AreEqual(o, edge1)) && face.Edges.Any(o => AreEqual(o, edge2)))
                    {
                        return true;
                    }
                }

                return false;
            }
            private static bool AreEqual(Edge3D edge1, Edge3D edge2)
            {
                //NOTE: I decided against overriding .Equals in Edge3D.  This method assumes that AllPoints is the same.  A more generic
                //method can't make that assumption, and would need more extensive compares

                if (edge1 == null && edge2 == null)
                {
                    return true;
                }
                else if (edge1 == null || edge2 == null)
                {
                    return false;
                }

                if (edge1.EdgeType != edge2.EdgeType)
                {
                    return false;
                }

                if (edge1.Index0 != edge2.Index0)
                {
                    return false;
                }

                switch (edge1.EdgeType)
                {
                    case EdgeType.Line:
                    case EdgeType.Ray:
                        return Math3D.IsNearValue(edge1.Direction.Value, edge2.Direction.Value);

                    case EdgeType.Segment:
                        return edge1.Index1.Value == edge2.Index1.Value;

                    default:
                        throw new ApplicationException("Unknown EdgeType: " + edge1.EdgeType.ToString());
                }
            }

            private static Face3D BuildFace(Edge3D[] allEdges, EdgeIndex[] chain1, EdgeIndex middle, EdgeIndex[] chain2 = null)
            {
                if (chain2 == null)
                {
                    return new Face3D(UtilityCore.Iterate<int>(middle.Index, chain1.Select(o => o.Index)).ToArray(), allEdges);
                }

                // Start and stop with a ray
                int[] indices = UtilityCore.Iterate<int>(
                    chain1.Select(o => o.Index).Reverse(),      // reversing, because the chain walks toward a ray.  Need to go from ray toward middle
                    middle.Index,
                    chain2.Select(o => o.Index)
                    ).ToArray();

                return new Face3D(indices, allEdges);
            }

            private static Vector3D GetAxisUnit(Edge3D edge0, Edge3D edge1)
            {
                var directions = Edge3D.GetRays(edge0, edge1);

                return Vector3D.CrossProduct(directions.Item2, directions.Item3).ToUnit();
            }

            private static int[] AddUniqueFaces(List<Tuple<int[], Face3D>> uniqueFaces, Face3D[] faces)
            {
                int[] retVal = new int[faces.Length];

                for (int cntr = 0; cntr < faces.Length; cntr++)
                {
                    int[] key = GetKey(faces[cntr]);

                    int index = -1;

                    // Find this face in the list
                    for (int inner = 0; inner < uniqueFaces.Count; inner++)
                    {
                        if (IsSameKey(uniqueFaces[inner].Item1, key))
                        {
                            index = inner;
                            break;
                        }
                    }

                    // This face is unique, add it
                    if (index < 0)
                    {
                        uniqueFaces.Add(Tuple.Create(key, faces[cntr]));
                        index = uniqueFaces.Count - 1;
                    }

                    // Store the index to the unique face
                    retVal[cntr] = index;
                }

                return retVal;
            }

            private static int[] GetKey(Face3D face)
            {
                // The key can just be the edges sorted, since all the faces passed to this method share the same set of edges
                return face.EdgeIndices.OrderBy().ToArray();
            }
            private static bool IsSameKey(int[] key1, int[] key2)
            {
                // Different length is definately not the same
                if (key1.Length != key2.Length)
                {
                    return false;
                }

                // Do an element by element compare (safe to do, because the lists are sorted)
                for (int cntr = 0; cntr < key1.Length; cntr++)
                {
                    if (key1[cntr] != key2[cntr])
                    {
                        return false;
                    }
                }

                return true;
            }

            private static bool RemoveFace(Face3D face, List<Face3D> faces)
            {
                int[] key = GetKey(face);

                for (int cntr = 0; cntr < faces.Count; cntr++)
                {
                    if (IsSameKey(key, GetKey(faces[cntr])))
                    {
                        faces.RemoveAt(cntr);
                        return true;
                    }
                }

                return false;
            }

            #endregion
        }

        #endregion
        #region Class: HullVoronoiIntersect

        private static class HullVoronoiIntersect
        {
            #region Class: TriangleIntersection 1,2,3

            private class TriInt_I
            {
                public TriInt_I(ITriangleIndexed triangle, int triangleIndex)
                {
                    this.Triangle = triangle;
                    this.TriangleIndex = triangleIndex;
                }

                public readonly ITriangleIndexed Triangle;
                public readonly int TriangleIndex;

                public readonly List<TriInt_II> ControlPoints = new List<TriInt_II>();
            }

            private class TriInt_II
            {
                public TriInt_II(int controlPointIndex)
                {
                    this.ControlPointIndex = controlPointIndex;
                }

                public readonly int ControlPointIndex;
                public readonly List<TriInt_III> Faces = new List<TriInt_III>();
            }

            private class TriInt_III
            {
                public TriInt_III(Face3D face, int faceIndex, Tuple<Point3D, Point3D> segment, int[] triangleIndices)
                {
                    this.Face = face;
                    this.FaceIndex = faceIndex;
                    this.Segment = segment;
                    this.TriangleIndices = triangleIndices;
                }

                // The face that the triangle intersected
                public readonly Face3D Face;
                public readonly int FaceIndex;

                // The points of intersection
                public readonly Tuple<Point3D, Point3D> Segment;

                // The indicies of the triangle that are part of the parent control point
                public readonly int[] TriangleIndices;
            }

            #endregion
            #region Class: PolyFragIntermediate

            private class PolyFragIntermediate
            {
                public PolyFragIntermediate(int controlPointIndex, int originalTriangleIndex, Tuple<TriangleEdge, int>[] faceIndices, int triangleIndex0, int triangleIndex1, int triangleIndex2)
                {
                    this.ControlPointIndex = controlPointIndex;
                    this.OriginalTriangleIndex = originalTriangleIndex;

                    this.FaceIndices = faceIndices;

                    this.TriangleIndex0 = triangleIndex0;
                    this.TriangleIndex1 = triangleIndex1;
                    this.TriangleIndex2 = triangleIndex2;
                }

                public readonly int ControlPointIndex;
                public readonly int OriginalTriangleIndex;

                public readonly Tuple<TriangleEdge, int>[] FaceIndices;

                public readonly int TriangleIndex0;
                public readonly int TriangleIndex1;
                public readonly int TriangleIndex2;
            }

            #endregion

            #region Class: TestFaceResult

            public class PolyFaceResult
            {
                public PolyFaceResult(Face3D face, int faceIndex, ITriangleIndexed[] polygon)
                {
                    this.Face = face;
                    this.FaceIndex = faceIndex;
                    this.Polygon = polygon;
                }

                public readonly Face3D Face;
                public readonly int FaceIndex;

                public readonly ITriangleIndexed[] Polygon;
            }

            #endregion

            public static Tuple<int, ITriangleIndexed[]>[] GetIntersection_Hull_Voronoi_full(ITriangleIndexed[] convexHull, VoronoiResult3D voronoi)
            {
                // If there is any portion that is concave, the intersection of the hull with a voronoi's face will make the lower logic
                // go the wrong direction when including face verticies (looks really wrong)
                convexHull = Math3D.GetConvexHull(TriangleIndexed.GetUsedPoints(convexHull, true));

                Point3D[] hullPoints = TriangleIndexed.GetUsedPoints(convexHull);
                Point3D hullCenter = Math3D.GetCenter(hullPoints);

                var hullAABB = Math3D.GetAABB(hullPoints);
                double hullAABBLen = (hullAABB.Item2 - hullAABB.Item1).Length;
                double rayExtend = hullAABBLen * 10;

                // Slice the hull if it intersects a voronoi wall
                VoronoiHullIntersect_PatchFragment[] trisByCtrl = GetIntersection_Hull_Voronoi_surface(convexHull, voronoi);

                var retVal = new List<Tuple<int, ITriangleIndexed[]>>();

                List<Face3D> insideFaces = new List<Face3D>();

                #region completely inside

                // Return control point cells that are completely inside the hull
                var skippedControlPoints = Enumerable.Range(0, voronoi.ControlPoints.Length).
                    Where(o => !trisByCtrl.Any(p => p.ControlPointIndex == o));

                foreach (int index in skippedControlPoints)
                {
                    if (IsSubHullInsideMainHull(voronoi, index, convexHull))
                    {
                        ITriangleIndexed[] subHull = voronoi.GetVoronoiCellHull(index);
                        insideFaces.AddRange(voronoi.FacesByControlPoint[index].Select(o => voronoi.Faces[o]));

                        retVal.Add(Tuple.Create(index, subHull));
                    }
                }

                #endregion
                #region intersecting

                // Go through each patch, and include the face polygons to get depth
                retVal.AddRange(trisByCtrl.
                    Select(o =>
                    {
                        ITriangleIndexed[] triangles = o.Polygon.
                            Select(p => p.Triangle).
                            ToArray();

                        ITriangleIndexed[] subHull = GetSubHull(o, triangles, voronoi, hullCenter, rayExtend, insideFaces);

                        return Tuple.Create(o.ControlPointIndex, subHull);
                    }).
                    Where(o => o.Item2 != null));

                #endregion
                #region validate

                double hullSqrEnlarged = (hullAABBLen * hullAABBLen) * 1.25;        // ran into cases where the sub aabb is .00000000000001 larger

                // Ran into a case where a very large shard was created.  This is a rough validation to make sure nothing too crazy is returned
                retVal = retVal.
                    Where(o =>
                    {
                        var subAABB = Math3D.GetAABB(o.Item2);
                        double subAABBLenSqr = (hullAABB.Item2 - hullAABB.Item1).LengthSquared;

                        return subAABBLenSqr < hullSqrEnlarged;
                    }).
                    ToList();

                #endregion

                return retVal.ToArray();
            }

            public static VoronoiHullIntersect_PatchFragment[] GetIntersection_Hull_Voronoi_surface(ITriangleIndexed[] triangles, VoronoiResult3D voronoi)
            {
                if (triangles == null || triangles.Length == 0)
                {
                    return new VoronoiHullIntersect_PatchFragment[0];
                }

                Point3D[] allPoints = triangles[0].AllPoints;

                // Figure out which control point owns each triangle point
                int[] controlPointsByTrianglePoint = GetNearestControlPoints(allPoints, voronoi);

                // Divide the triangles by control point
                return GetTrianglesByControlPoint(triangles, voronoi, controlPointsByTrianglePoint);
            }

            #region Private Methods - fragments

            private static VoronoiHullIntersect_PatchFragment[] GetTrianglesByControlPoint(ITriangleIndexed[] triangles, VoronoiResult3D voronoi, int[] controlPointsByTrianglePoint)
            {
                List<PolyFragIntermediate> buildingFinals = new List<PolyFragIntermediate>();

                List<Point3D> newPoints = new List<Point3D>();

                List<Tuple<long, int>> triangleFaceMisses = new List<Tuple<long, int>>();
                List<TriInt_I> intermediate = new List<TriInt_I>();

                // Figure out how long rays should extend
                var aabb = Math3D.GetAABB(voronoi.EdgePoints);
                double rayLength = (aabb.Item2 - aabb.Item1).Length * 10;

                // Get the aabb of each face
                var faceAABBs = voronoi.Faces.
                    Select(o => Math3D.GetAABB(o.Edges, rayLength)).
                    ToArray();

                for (int triangleIndex = 0; triangleIndex < triangles.Length; triangleIndex++)
                {
                    ITriangleIndexed triangle = triangles[triangleIndex];

                    if (controlPointsByTrianglePoint[triangle.Index0] == controlPointsByTrianglePoint[triangle.Index1] && controlPointsByTrianglePoint[triangle.Index0] == controlPointsByTrianglePoint[triangle.Index2])
                    {
                        #region Interior Triangle

                        // The entire triangle is inside the control point's cell
                        int index0 = GetPointIndex(triangle.Point0, newPoints);
                        int index1 = GetPointIndex(triangle.Point1, newPoints);
                        int index2 = GetPointIndex(triangle.Point2, newPoints);

                        var faceIndices = new Tuple<TriangleEdge, int>[0];

                        buildingFinals.Add(new PolyFragIntermediate(controlPointsByTrianglePoint[triangle.Index0], triangleIndex, faceIndices, index0, index1, index2));

                        #endregion
                        continue;
                    }

                    var triangleAABB = Math3D.GetAABB(triangle);

                    for (int faceIndex = 0; faceIndex < voronoi.Faces.Length; faceIndex++)
                    {
                        if (!Math3D.IsIntersecting_AABB_AABB(triangleAABB.Item1, triangleAABB.Item2, faceAABBs[faceIndex].Item1, faceAABBs[faceIndex].Item2))
                        {
                            continue;
                        }

                        GetTrianglesByControlPoint_FaceTriangle(triangle, triangleIndex, faceIndex, voronoi, controlPointsByTrianglePoint, intermediate, triangleFaceMisses, rayLength);
                    }
                }

                // Convert each intersected triangle into smaller triangles
                buildingFinals.AddRange(ConvertToSmaller(newPoints, intermediate));

                Point3D[] allPoints = newPoints.ToArray();

                // Map to the output type
                return buildingFinals.
                    GroupBy(o => o.ControlPointIndex).
                    Select(o => new VoronoiHullIntersect_PatchFragment(o.Select(p => new VoronoiHullIntersect_TriangleFragment(
                        new TriangleIndexed(p.TriangleIndex0, p.TriangleIndex1, p.TriangleIndex2, allPoints),
                        p.FaceIndices,
                        p.OriginalTriangleIndex
                        )).ToArray(), o.Key)).
                    ToArray();
            }

            private static void GetTrianglesByControlPoint_FaceTriangle(ITriangleIndexed triangle, int triangleIndex, int faceIndex, VoronoiResult3D voronoi, int[] controlPointsByTrianglePoint, List<TriInt_I> intersections, List<Tuple<long, int>> triangleFaceMisses, double rayLength)
            {
                Tuple<long, int> key = Tuple.Create(triangle.Token, faceIndex);

                // See if this triangle and face have been intersected
                if (triangleFaceMisses.Contains(key) || HasIntersection(triangle, faceIndex, intersections))
                {
                    return;
                }

                // Intersect the face and triangle
                Tuple<Point3D, Point3D> segment = Math3D.GetIntersection_Face_Triangle(voronoi.Faces[faceIndex], triangle, rayLength);

                if (segment == null)
                {
                    triangleFaceMisses.Add(key);
                    return;
                }

                // Store this intersection segment for every control point that has this face
                foreach (int controlIndex in voronoi.GetControlPoints(faceIndex))
                {
                    // Figure out which corners of the triangle are in this control point's cell
                    int[] triangleIndices = triangle.IndexArray.
                        Where(o => controlPointsByTrianglePoint[o] == controlIndex).
                        ToArray();

                    AddIntersection(triangle, triangleIndex, triangleIndices, controlIndex, faceIndex, voronoi.Faces[faceIndex], segment, intersections);
                }
            }

            private static IEnumerable<PolyFragIntermediate> ConvertToSmaller(List<Point3D> newPoints, List<TriInt_I> intersections)
            {
                List<PolyFragIntermediate> retVal = new List<PolyFragIntermediate>();

                foreach (TriInt_I triangle in intersections)
                {
                    foreach (TriInt_II controlPoint in triangle.ControlPoints)
                    {
                        IEnumerable<ITriangle> smalls;

                        if (controlPoint.Faces.Count == 1)
                        {
                            smalls = ConvertToSmaller_Single(triangle, controlPoint, controlPoint.Faces[0]);
                        }
                        else
                        {
                            smalls = ConvertToSmaller_Multi(triangle, controlPoint, controlPoint.Faces);
                        }

                        foreach (ITriangle small in smalls)
                        {
                            ITriangle smallValidNormal = MatchNormal(small, triangle.Triangle);

                            // Convert small into a final triangle indexed
                            int index0 = GetPointIndex(smallValidNormal.Point0, newPoints);
                            int index1 = GetPointIndex(smallValidNormal.Point1, newPoints);
                            int index2 = GetPointIndex(smallValidNormal.Point2, newPoints);

                            Tuple<TriangleEdge, int>[] faceIndices = LinkSegmentsWithFaces(index0, index1, index2, newPoints, controlPoint.Faces);

                            retVal.Add(new PolyFragIntermediate(controlPoint.ControlPointIndex, triangle.TriangleIndex, faceIndices, index0, index1, index2));
                        }
                    }
                }

                return retVal;
            }
            private static IEnumerable<ITriangle> ConvertToSmaller_Single(TriInt_I triangle, TriInt_II controlPoint, TriInt_III face)
            {
                Point3D[] points3D = UtilityCore.Iterate(face.TriangleIndices.Select(o => triangle.Triangle.AllPoints[o]), new[] { face.Segment.Item1, face.Segment.Item2 }).ToArray();

                return ConvertToSmaller_Finish(triangle, points3D);
            }
            private static IEnumerable<ITriangle> ConvertToSmaller_Multi(TriInt_I triangle, TriInt_II controlPoint, List<TriInt_III> faces)
            {
                IEnumerable<Point3D> trianglePoints = faces.
                    SelectMany(o => o.TriangleIndices).
                    Distinct().
                    Select(o => triangle.Triangle.AllPoints[o]);

                var chains1 = UtilityCore.GetChains(faces.Select(o => o.Segment), (o, p) => Math3D.IsNearValue(o, p));
                var chains2 = chains1;
                if (chains2.Length == 0)
                {
                    return new ITriangle[0];
                }
                else if (chains2.Length > 1 && chains2.Any(o => o.Item2))
                {
                    return new ITriangle[0];
                }
                else if (chains2.Length > 1)
                {
                    // This sometimes happens.  Just combine the chains
                    chains2 = new[] { Tuple.Create(chains2.SelectMany(o => o.Item1).ToArray(), false) };
                }

                Point3D[] points3D = UtilityCore.Iterate(trianglePoints, chains2[0].Item1).ToArray();

                return ConvertToSmaller_Finish(triangle, points3D);
            }
            private static IEnumerable<ITriangle> ConvertToSmaller_Finish(TriInt_I triangle, Point3D[] points)
            {
                if (points.Length < 3)
                {
                    //throw new ApplicationException("handle less than 2");
                    return new ITriangle[0];
                }
                else if (points.Length == 3)
                {
                    return new[] { new Triangle(points[0], points[1], points[2]) };
                }

                // The points could come in any order, so convert into a polygon to figure out the order
                //NOTE: At first, I tried Math2D.GetDelaunayTriangulation(), which works in most cases.  But when two points are really close together, it weirds out and returns nothing
                var hull2D = Math2D.GetConvexHull(points);

                // Now triangulate it
                return Math2D.GetTrianglesFromConvexPoly(hull2D.PerimiterLines.Select(o => points[o]).ToArray());
            }

            private static Tuple<TriangleEdge, int>[] LinkSegmentsWithFaces(int index0, int index1, int index2, List<Point3D> newPoints, List<TriInt_III> faces)
            {
                var retVal = new List<Tuple<TriangleEdge, int>>();

                // Store the triangle's points as something queryable
                var points = new[]
                    {
                        new { Index = 0, Point = newPoints[index0]},
                        new { Index = 1, Point = newPoints[index1]},
                        new { Index = 2, Point = newPoints[index2]},
                    };

                foreach (var face in faces)
                {
                    // Find two points of the triangle that match this face intersect
                    var matches = new[] { face.Segment.Item1, face.Segment.Item2 }.
                        Select(o => points.FirstOrDefault(p => Math3D.IsNearValue(p.Point, o))).
                        Where(o => o != null).
                        OrderBy(o => o.Index).
                        ToArray();

                    if (matches.Length < 2)
                    {
                        continue;
                    }

                    TriangleEdge edge;

                    if (matches[0].Index == 0 && matches[1].Index == 1)
                    {
                        edge = TriangleEdge.Edge_01;
                    }
                    else if (matches[0].Index == 0 && matches[1].Index == 2)
                    {
                        edge = TriangleEdge.Edge_20;
                    }
                    else if (matches[0].Index == 1 && matches[1].Index == 2)
                    {
                        edge = TriangleEdge.Edge_12;
                    }
                    else
                    {
                        throw new ApplicationException("Unexpected pair: " + matches[0].Index.ToString() + ", " + matches[1].Index.ToString());
                    }

                    retVal.Add(Tuple.Create(edge, face.FaceIndex));
                }

                return retVal.ToArray();
            }

            private static void AddFinal(int controlPoint, ITriangleIndexed triangle, SortedList<int, List<ITriangleIndexed>> triangleByControlPoint)
            {
                List<ITriangleIndexed> list;
                if (!triangleByControlPoint.TryGetValue(controlPoint, out list))
                {
                    list = new List<ITriangleIndexed>();
                    triangleByControlPoint.Add(controlPoint, list);
                }

                list.Add(triangle);
            }

            private static bool HasIntersection(ITriangleIndexed triangle, int faceIndex, List<TriInt_I> intersections)
            {
                TriInt_I i = intersections.FirstOrDefault(o => o.Triangle.Token == triangle.Token);
                if (i == null)
                {
                    return false;
                }

                foreach (TriInt_II ii in i.ControlPoints)
                {
                    TriInt_III iii = ii.Faces.FirstOrDefault(o => o.FaceIndex == faceIndex);
                    if (iii != null)
                    {
                        return true;
                    }
                }

                return false;
            }
            private static void AddIntersection(ITriangleIndexed triangle, int triangleIndex, int[] triangleIndices, int controlPointIndex, int faceIndex, Face3D face, Tuple<Point3D, Point3D> segment, List<TriInt_I> intersections)
            {
                TriInt_I i = intersections.FirstOrDefault(o => o.Triangle.Token == triangle.Token);
                if (i == null)
                {
                    i = new TriInt_I(triangle, triangleIndex);
                    intersections.Add(i);
                }

                TriInt_II ii = i.ControlPoints.FirstOrDefault(o => o.ControlPointIndex == controlPointIndex);
                if (ii == null)
                {
                    ii = new TriInt_II(controlPointIndex);
                    i.ControlPoints.Add(ii);
                }

                TriInt_III iii = ii.Faces.FirstOrDefault(o => o.FaceIndex == faceIndex);
                if (iii == null)
                {
                    iii = new TriInt_III(face, faceIndex, segment, triangleIndices);
                    ii.Faces.Add(iii);
                }
            }

            private static void RemoveNulls(List<TriInt_I> intersections)
            {
                // ugly, but it works :)

                int i = 0;
                while (i < intersections.Count)
                {
                    #region i

                    int ii = 0;
                    while (ii < intersections[i].ControlPoints.Count)
                    {
                        #region ii

                        int iii = 0;
                        while (iii < intersections[i].ControlPoints[ii].Faces.Count)
                        {
                            #region iii

                            if (intersections[i].ControlPoints[ii].Faces[iii].Segment == null)
                            {
                                intersections[i].ControlPoints[ii].Faces.RemoveAt(iii);
                            }
                            else
                            {
                                iii++;
                            }

                            #endregion
                        }

                        if (intersections[i].ControlPoints[ii].Faces.Count == 0)
                        {
                            intersections[i].ControlPoints.RemoveAt(ii);
                        }
                        else
                        {
                            ii++;
                        }

                        #endregion
                    }

                    if (intersections[i].ControlPoints.Count == 0)
                    {
                        intersections.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }

                    #endregion
                }
            }

            private static int GetPointIndex(Point3D point, List<Point3D> points)
            {
                for (int cntr = 0; cntr < points.Count; cntr++)
                {
                    if (Math3D.IsNearValue(points[cntr], point))
                    {
                        return cntr;
                    }
                }

                points.Add(point);
                return points.Count - 1;
            }

            private static ITriangle MatchNormal(ITriangle toMatch, ITriangle matchAgainst)
            {
                if (Vector3D.DotProduct(toMatch.Normal, matchAgainst.Normal) < 0)
                {
                    return new Triangle(toMatch.Point0, toMatch.Point2, toMatch.Point1);
                }
                else
                {
                    return toMatch;
                }
            }

            /// <summary>
            /// This returns which control point each triangle point is closest to
            /// </summary>
            /// <returns>
            /// Index into return = Triangle point index
            /// Value of return = Voronoi control point index
            /// </returns>
            private static int[] GetNearestControlPoints(Point3D[] allPoints, VoronoiResult3D voronoi)
            {
                // Cache the index with the point
                var controlPoints = voronoi.ControlPoints.
                    Select((o, i) => new { Point = o, Index = i }).
                    ToArray();

                // Find the closest control point for each triangle point
                return allPoints.
                    Select(o =>
                        controlPoints.
                            OrderBy(p => (o - p.Point).LengthSquared).
                            First().
                            Index).
                    ToArray();
            }

            #endregion
            #region Private Methods - face polys

            private static TriangleIndexed[] GetSubHull(VoronoiHullIntersect_PatchFragment patch, ITriangleIndexed[] hullPatch, VoronoiResult3D voronoi, Point3D hullCenter, double rayExtend, List<Face3D> insideFaces)
            {
                List<PolyFaceResult> retVal = new List<PolyFaceResult>();

                int[] faces = voronoi.FacesByControlPoint[patch.ControlPointIndex];

                List<int> potentialFullFaces = new List<int>();

                foreach (int faceIndex in faces)
                {
                    var intersects = patch.Polygon.
                        Select(o => new
                        {
                            Triangle = o,
                            FaceHits = o.FaceIndices.Where(p => p.Item2 == faceIndex).ToArray()
                        }).
                        Where(o => o.FaceHits.Length > 0).
                        ToArray();

                    Point3D[] points = intersects.
                        SelectMany(o => o.FaceHits.SelectMany(p => new[] { o.Triangle.Triangle.GetPoint(p.Item1, true), o.Triangle.Triangle.GetPoint(p.Item1, false) })).
                        Distinct((p1, p2) => Math3D.IsNearValue(p1, p2)).
                        ToArray();

                    if (points.Length == 0)
                    {
                        // Add the entire face
                        //NOTE: Open faces should be ignored, because the only ones we care about are the ones that intersect.  Imagine the hull intersecing
                        //the base of a Y.  The V portion of the Y should be ignored
                        if (voronoi.Faces[faceIndex].IsClosed)
                        {
                            //TODO: Need an additional filter to see if this face touches the hull.  Store this face for later inclusion once the hull is more fully worked
                            //TODO: For v4, don't include this.  Just let the convex hull method fill in holes
                            //retVal.Add(new TestFaceResult(voronoi.Faces[faceIndex], faceIndex, voronoi.Faces[faceIndex].GetPolygonTriangles(), null));
                            potentialFullFaces.Add(faceIndex);
                        }
                        continue;
                    }

                    points = AddFaceVerticesToPolygon(points, voronoi.Faces[faceIndex], rayExtend);
                    if (points.Length < 3)
                    {
                        continue;
                    }

                    // Triangulate these points
                    var hull2D = Math2D.GetConvexHull(points);
                    if (hull2D == null)
                    {
                        // The points are colinear
                        continue;
                    }

                    TriangleIndexed[] triangles = Math2D.GetTrianglesFromConvexPoly(hull2D.PerimiterLines.Select(o => points[o]).ToArray());

                    retVal.Add(new PolyFaceResult(voronoi.Faces[faceIndex], faceIndex, triangles));
                }

                // Add skipped whole faces
                //TODO: Fix this.  I think my design is flawed (simplifying to a cutoff plane)
                //retVal.AddRange(AddFullFacesToHull_BROKEN(hullPatch, hullCenter, voronoi, potentialFullFaces));
                retVal.AddRange(AddFullFacesToHull(potentialFullFaces, voronoi, insideFaces));

                // Convert to a convex hull.  This will clean up any small holes that were missed above
                List<Point3D> finalPoints = new List<Point3D>();
                finalPoints.AddRange(TriangleIndexed.GetUsedPoints(retVal.Select(o => o.Polygon)));
                finalPoints.AddRangeUnique(TriangleIndexed.GetUsedPoints(hullPatch), (p1, p2) => Math3D.IsNearValue(p1, p2));

                return Math3D.GetConvexHull(finalPoints.ToArray());
            }

            /// <summary>
            /// This takes points that touch a face, and adds some of the face's vertices
            /// </summary>
            /// <remarks>
            /// The points passed in form an arc.  The direction of the curve of the arc tells which face vertices to include
            /// (think of a snow cone, or an umbrella)
            /// </remarks>
            private static Point3D[] AddFaceVerticesToPolygon(Point3D[] points, Face3D face, double rayExtend)
            {
                // Detect which of the points are on the face's edges
                var faceEdgeHits = face.Edges.
                    Select(o => new
                    {
                        Edge = o,
                        Points = points.Where(p => Math1D.IsNearZero(Math3D.GetClosestDistance_Line_Point(o.Point0, o.DirectionExt, p))).ToArray()        // Even though the edge is finite, there should never be a case where a face hit is off the edge segment.  So save some processing and just assume it's a line
                    }).
                    Where(o => o.Points.Length > 0).
                    ToArray();

                // If the edge hits are on one edge, then it is a polygon on the face, and the face's verticies should't be added (they definately
                // outside the polygon)
                if (faceEdgeHits.Length == 1) return points;

                // Choose one of the points that aren't in faceEdgeHits.  This will define a direction not to go.  Then go through all the
                // face's points, and keep the ones that are the opposite direction of that line

                Point3D[] edgePoints = faceEdgeHits.
                    SelectMany(o => o.Points).
                    Distinct((o1, o2) => Math3D.IsNearValue(o1, o2)).
                    ToArray();

                if (edgePoints.Length > 2)
                {
                    return points.
                        Concat(edgePoints).
                        Distinct((o1, o2) => Math3D.IsNearValue(o1, o2)).
                        ToArray();
                }

                if (edgePoints == null) return points;
                else if (edgePoints.Length != 2) return points;

                // Find the point that is farthest from the intersects
                Vector3D line = edgePoints[1] - edgePoints[0];

                var otherPoint = points.
                    Where(o => !edgePoints.Any(p => p.IsNearValue(o))).
                    Select(o =>
                    {
                        Point3D intersect = Math3D.GetClosestPoint_Line_Point(edgePoints[0], line, o);
                        Vector3D heightLine = o - intersect;

                        return new
                        {
                            Intersect = intersect,
                            HeightLine = heightLine,
                            HeightLineLen = heightLine.Length,
                        };
                    }).
                    OrderByDescending(o => o.HeightLineLen).
                    FirstOrDefault();

                // Detect thin triangle
                if (otherPoint == null) return points;
                else if (otherPoint.HeightLine.IsNearZero()) return points;
                else if (otherPoint.HeightLineLen / line.Length < .015) return points;

                // Draw lines between the face verticies and that intersect points, and keep the ones with a negative dot product (the verticies that are away from that line)
                Point3D[] faceMatches = face.Edges.
                    Select(o => new[] { o.Point0, o.GetPoint1Ext(rayExtend) }).
                    SelectMany(o => o).
                    Where(o => Vector3D.DotProduct(o - otherPoint.Intersect, otherPoint.HeightLine) < 0).
                    Distinct((o1, o2) => Math3D.IsNearValue(o1, o2)).
                    ToArray();

                if (faceMatches.Length == 0) return points;

                return points.
                    Concat(faceMatches).
                    ToArray();
            }

            /// <summary>
            /// This returns full faces that are below the plane of the face intersects
            /// </summary>
            /// <remarks>
            /// This is very similar in spirit with AddFaceVerticesToPolygon().  That works in 2D, this is 3D
            /// </remarks>
            private static IEnumerable<PolyFaceResult> AddFullFacesToHull_BROKEN(ITriangleIndexed[] hullPatch, Point3D hullCenter, VoronoiResult3D voronoi, List<int> potentialFullFaces)
            {
                if (potentialFullFaces.Count == 0)
                {
                    return new PolyFaceResult[0];
                }

                Point3D[] patchPoints = TriangleIndexed.GetUsedPoints(hullPatch);

                //TODO: This is probably too simplistic, but should get most faces
                ITriangle plane = Math2D.GetPlane_Average(patchPoints);
                Vector3D direction = plane.Point0 - hullCenter;
                if (Vector3D.DotProduct(direction, plane.Normal) < 0)
                {
                    plane = new Triangle(plane.Point0, plane.Point2, plane.Point1);
                }

                List<PolyFaceResult> retVal = new List<PolyFaceResult>();

                foreach (int faceIndex in potentialFullFaces)
                {
                    if (!voronoi.Faces[faceIndex].IsClosed)
                    {
                        continue;
                    }

                    bool isUnder = true;

                    var facePolygon = voronoi.Faces[faceIndex].GetPolygon();
                    foreach (int pointIndex in facePolygon.Item1)
                    {
                        if (!Math3D.IsAbovePlane(plane, facePolygon.Item2[pointIndex]))
                        {
                            isUnder = false;
                            break;
                        }
                    }

                    if (isUnder)
                    {
                        retVal.Add(new PolyFaceResult(voronoi.Faces[faceIndex], faceIndex, voronoi.Faces[faceIndex].GetPolygonTriangles()));
                    }
                }

                return retVal;
            }

            /// <summary>
            /// This is very simplistic.  Since all control points that are fully inside the hull are known (and by extension, all of their faces are known), then
            /// add faces that match those.
            /// NOTE: This method still won't catch all the faces
            /// </summary>
            /// <param name="potentialFullFaces">Candidates to be added</param>
            /// <param name="insideFaces">Faces that are definately inside the outer hull</param>
            private static IEnumerable<PolyFaceResult> AddFullFacesToHull(List<int> potentialFullFaces, VoronoiResult3D voronoi, List<Face3D> insideFaces)
            {
                foreach (int potentialIndex in potentialFullFaces)
                {
                    long token = voronoi.Faces[potentialIndex].Token;
                    if (insideFaces.Any(o => o.Token == token))
                    {
                        yield return new PolyFaceResult(voronoi.Faces[potentialIndex], potentialIndex, voronoi.Faces[potentialIndex].GetPolygonTriangles());
                    }
                }
            }

            private static bool IsSubHullInsideMainHull(VoronoiResult3D voronoi, int index, ITriangleIndexed[] convexHull)
            {
                Point3D[] subPoints = voronoi.FacesByControlPoint[index].
                    Where(o => voronoi.Faces[o].IsClosed).
                    SelectMany(o =>
                    {
                        var points = voronoi.Faces[o].GetPolygon();
                        return points.Item1.Select(p => points.Item2[p]);
                    }).
                    ToArray();

                // .All is returning true when the list is empty
                if (subPoints.Length == 0)
                {
                    return false;
                }

                return subPoints.All(o => Math3D.IsInside_ConvexHull(convexHull, o));
            }

            #endregion
        }

        #endregion

        #region Declaration Section

        public static readonly Matrix3D ZeroMatrix = new Matrix3D(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        public static readonly Vector3D ScaleIdentity = new Vector3D(1, 1, 1);

        //public const double NEARZERO = .000001d;
        //public const double NEARZERO = .000000001d;
        public const double NEARZERO = UtilityCore.NEARZERO;

        #endregion

        #region Simple

        public static bool IsNearZero(Vector3D testVect)
        {
            return Math.Abs(testVect.X) <= NEARZERO && Math.Abs(testVect.Y) <= NEARZERO && Math.Abs(testVect.Z) <= NEARZERO;
        }
        public static bool IsNearZero(Point3D testPoint)
        {
            return Math.Abs(testPoint.X) <= NEARZERO && Math.Abs(testPoint.Y) <= NEARZERO && Math.Abs(testPoint.Z) <= NEARZERO;
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
        public static bool IsNearValue(Quaternion testQuat, Quaternion compareTo)
        {
            return testQuat.X >= compareTo.X - NEARZERO && testQuat.X <= compareTo.X + NEARZERO &&
                        testQuat.Y >= compareTo.Y - NEARZERO && testQuat.Y <= compareTo.Y + NEARZERO &&
                        testQuat.Z >= compareTo.Z - NEARZERO && testQuat.Z <= compareTo.Z + NEARZERO &&
                        testQuat.W >= compareTo.W - NEARZERO && testQuat.W <= compareTo.W + NEARZERO;
        }

        /// <summary>
        /// Returns true if the vector contains NaN or Infinity
        /// </summary>
        public static bool IsInvalid(Vector3D testVect)
        {
            return Math1D.IsInvalid(testVect.X) || Math1D.IsInvalid(testVect.Y) || Math1D.IsInvalid(testVect.Z);
        }
        /// <summary>
        /// Returns true if the point contains NaN or Infinity
        /// </summary>
        public static bool IsInvalid(Point3D testPoint)
        {
            return Math1D.IsInvalid(testPoint.X) || Math1D.IsInvalid(testPoint.Y) || Math1D.IsInvalid(testPoint.Z);
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

            retVal.X = rand.NextDouble(boundryLower.X, boundryUpper.X);
            retVal.Y = rand.NextDouble(boundryLower.Y, boundryUpper.Y);
            retVal.Z = rand.NextDouble(boundryLower.Z, boundryUpper.Z);

            return retVal;
        }
        /// <summary>
        /// This chooses a random point somewhere between the inner rectactle and the outer one
        /// </summary>
        public static Vector3D GetRandomVector(Vector3D outerRect_Min, Vector3D outerRect_Max, Vector3D innerRect_Min, Vector3D innerRect_Max)
        {
            Vector3D retVal = new Vector3D();

            Random rand = StaticRandom.GetRandomForThread();

            for (int cntr = 0; cntr < 1000; cntr++)
            {
                retVal.X = rand.NextDouble(outerRect_Min.X, outerRect_Max.X);
                retVal.Y = rand.NextDouble(outerRect_Min.Y, outerRect_Max.Y);
                retVal.Z = rand.NextDouble(outerRect_Min.Z, outerRect_Max.Z);

                if (!(retVal.X >= innerRect_Min.X && retVal.X <= innerRect_Max.X &&
                    retVal.Y >= innerRect_Min.Y && retVal.Y <= innerRect_Max.Y &&
                    retVal.Z >= innerRect_Min.Z && retVal.Z <= innerRect_Max.Z))
                {
                    return retVal;
                }
            }

            throw new ApplicationException("Infinite loop detected (hole is likely larger than the box)");
        }
        /// <summary>
        /// Get a random vector between maxValue*-1 and maxValue
        /// </summary>
        public static Vector3D GetRandomVector(double maxValue)
        {
            Vector3D retVal = new Vector3D();

            retVal.X = Math1D.GetNearZeroValue(maxValue);
            retVal.Y = Math1D.GetNearZeroValue(maxValue);
            retVal.Z = Math1D.GetNearZeroValue(maxValue);

            return retVal;
        }
        /// <summary>
        /// Gets a random vector with radius between maxRadius*-1 and maxRadius (bounds are spherical,
        /// rather than cube)
        /// </summary>
        public static Vector3D GetRandomVector_Spherical(double maxRadius)
        {
            return GetRandomVector_Spherical(0d, maxRadius);
        }
        /// <summary>
        /// Gets a random vector with radius between maxRadius*-1 and maxRadius (bounds are spherical,
        /// rather than cube).  The radius will never be inside minRadius
        /// </summary>
        /// <remarks>
        /// The sqrt idea came from here:
        /// http://dzindzinovic.blogspot.com/2010/05/xna-random-point-in-circle.html
        /// </remarks>
        public static Vector3D GetRandomVector_Spherical(double minRadius, double maxRadius)
        {
            // A sqrt, sin and cos  :(           can it be made cheaper?
            double radius = minRadius + ((maxRadius - minRadius) * Math.Sqrt(StaticRandom.NextDouble()));		// without the square root, there is more chance at the center than the edges

            return GetRandomVector_Spherical_Shell(radius);
        }
        /// <summary>
        /// Gets a random vector with the radius passed in (bounds are spherical, rather than cube)
        /// </summary>
        public static Vector3D GetRandomVector_Spherical_Shell(double radius)
        {
            Random rand = StaticRandom.GetRandomForThread();

            double theta = rand.NextDouble() * Math.PI * 2d;

            // z is cos of phi, which isn't linear.  So the probability is higher that more will be at the poles.  Which means if I want
            // a linear probability of z, I need to feed the cosine something that will flatten it into a line.  The curve that will do that
            // is arccos (which basically rotates the cosine wave 90 degrees).  This means that it is undefined for any x outside the range
            // of -1 to 1.  So I have to shift the random statement to go between -1 to 1, run it through the curve, then shift the result
            // to go between 0 and pi.
            //double phi = rand.NextDouble() * Math.PI;

            double phi = (rand.NextDouble() * 2d) - 1d;		// value from -1 to 1
            phi = -Math.Asin(phi) / (Math.PI * .5d);		// another value from -1 to 1
            phi = (1d + phi) * Math.PI * .5d;		// from 0 to pi

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
        public static Vector3D GetRandomVector_Circular(double maxRadius)
        {
            return GetRandomVector_Circular(0d, maxRadius);
        }
        /// <summary>
        /// Gets a random vector with radius between maxRadius*-1 and maxRadius (bounds are spherical,
        /// rather than cube).  The radius will never be inside minRadius.  Z will always be zero.
        /// </summary>
        /// <remarks>
        /// The sqrt idea came from here:
        /// http://dzindzinovic.blogspot.com/2010/05/xna-random-point-in-circle.html
        /// </remarks>
        public static Vector3D GetRandomVector_Circular(double minRadius, double maxRadius)
        {
            double radius = minRadius + ((maxRadius - minRadius) * Math.Sqrt(StaticRandom.NextDouble()));		// without the square root, there is more chance at the center than the edges

            return GetRandomVector_Circular_Shell(radius);
        }
        /// <summary>
        /// Gets a random vector with the radius passed in (bounds are spherical, rather than cube).  Z will always be zero.
        /// </summary>
        public static Vector3D GetRandomVector_Circular_Shell(double radius)
        {
            double angle = StaticRandom.NextDouble() * Math.PI * 2d;

            double x = radius * Math.Cos(angle);
            double y = radius * Math.Sin(angle);

            return new Vector3D(x, y, 0d);
        }

        /// <summary>
        /// This returns points evenly distributed within a sphere
        /// </summary>
        /// <param name="returnCount">How many points to return</param>
        /// <param name="existingStaticPoints">
        /// The static points won't be moved, but the return points will be repelled from them.
        /// NOTE: Pass in null if there are no static points
        /// </param>
        /// <param name="radius">
        /// The radius of the sphere the points should be contained in
        /// NOTE: The final output won't be exact it could be +- about 10% of this value
        /// </param>
        /// <param name="stopRadiusPercent">
        /// Each step, the return points are shifted a bit more into position.  When the max shift length is less than this (radius * stopRadiusPercent), then that's
        /// considered good enough, and the method stops
        /// NOTE: Somewhere between 5% to 1% seem to give pretty good results (the smaller the percent, the more exact the final result)
        /// </param>
        /// <param name="stopIterationCount">
        /// If the number of steps has gone past this count, the method stops (even though stopRadiusPercent hasn't been met yet).  This count acts like a
        /// safety to avoid taking way too long
        /// </param>
        /// <param name="movableRepulseMultipliers">
        /// This is useful if some points should be more repulsive than others (1 is a standard multiplier, 2 is twice, etc).
        /// NOTE: Only the force is multiplied, the distance calculation stays the same
        /// NOTE: Pass in null if they are all the same
        /// </param>
        public static Vector3D[] GetRandomVectors_Spherical_EvenDist(int returnCount, double radius, double stopRadiusPercent = .03, int stopIterationCount = 1000, double[] movableRepulseMultipliers = null, Vector3D[] existingStaticPoints = null, double[] staticRepulseMultipliers = null)
        {
            return EvenDistribution.GetSpherical(returnCount, existingStaticPoints, radius, stopRadiusPercent, stopIterationCount, movableRepulseMultipliers, staticRepulseMultipliers);
        }
        public static Vector3D[] GetRandomVectors_Spherical_EvenDist(Vector3D[] movable, double radius, double stopRadiusPercent = .03, int stopIterationCount = 1000, double[] movableRepulseMultipliers = null, Vector3D[] existingStaticPoints = null, double[] staticRepulseMultipliers = null)
        {
            return EvenDistribution.GetSpherical(movable, existingStaticPoints, radius, stopRadiusPercent, stopIterationCount, movableRepulseMultipliers, staticRepulseMultipliers);
        }

        public static Vector3D[] GetRandomVectors_Spherical_ClusteredMinDist(int returnCount, double radius, double minDist, double stopRadiusPercent = .03, int stopIterationCount = 1000, double[] movableRepulseMultipliers = null, Vector3D[] existingStaticPoints = null, double[] staticRepulseMultipliers = null)
        {
            return EvenDistribution.GetSpherical_ClusteredMinDist(returnCount, existingStaticPoints, radius, minDist, stopRadiusPercent, stopIterationCount, movableRepulseMultipliers, staticRepulseMultipliers);
        }
        public static Vector3D[] GetRandomVectors_Spherical_ClusteredMinDist(Vector3D[] movable, double radius, double minDist, double stopRadiusPercent = .03, int stopIterationCount = 1000, double[] movableRepulseMultipliers = null, Vector3D[] existingStaticPoints = null, double[] staticRepulseMultipliers = null)
        {
            return EvenDistribution.GetSpherical_ClusteredMinDist(movable, existingStaticPoints, radius, minDist, stopRadiusPercent, stopIterationCount, movableRepulseMultipliers, staticRepulseMultipliers);
        }

        public static Vector3D[] GetRandomVectors_SphericalShell_EvenDist(int returnCount, double radius, double stopRadiusPercent = .03, int stopIterationCount = 1000, double[] movableRepulseMultipliers = null, Vector3D[] existingStaticPoints = null, double[] staticRepulseMultipliers = null)
        {
            return EvenDistribution.GetSphericalShell(returnCount, existingStaticPoints, radius, stopRadiusPercent, stopIterationCount, movableRepulseMultipliers, staticRepulseMultipliers);
        }
        public static Vector3D[] GetRandomVectors_SphericalShell_EvenDist(Vector3D[] movable, double radius, double stopRadiusPercent = .03, int stopIterationCount = 1000, double[] movableRepulseMultipliers = null, Vector3D[] existingStaticPoints = null, double[] staticRepulseMultipliers = null)
        {
            return EvenDistribution.GetSphericalShell(movable, existingStaticPoints, radius, stopRadiusPercent, stopIterationCount, movableRepulseMultipliers, staticRepulseMultipliers);
        }

        public static Vector[] GetRandomVectors_Circular_EvenDist(int returnCount, double radius, double stopRadiusPercent = .03, int stopIterationCount = 1000, double[] movableRepulseMultipliers = null, Vector[] existingStaticPoints = null, double[] staticRepulseMultipliers = null)
        {
            return EvenDistribution.GetCircular(returnCount, existingStaticPoints, radius, stopRadiusPercent, stopIterationCount, movableRepulseMultipliers, staticRepulseMultipliers);
        }
        public static Vector[] GetRandomVectors_Circular_EvenDist(Vector[] movable, double radius, double stopRadiusPercent = .03, int stopIterationCount = 1000, double[] movableRepulseMultipliers = null, Vector[] existingStaticPoints = null, double[] staticRepulseMultipliers = null)
        {
            return EvenDistribution.GetCircular(movable, existingStaticPoints, radius, stopRadiusPercent, stopIterationCount, movableRepulseMultipliers, staticRepulseMultipliers);
        }

        public static Vector[] GetRandomVectors_Circular_CenterPacked(int returnCount, double radius, double stopRadiusPercent = .03, int stopIterationCount = 1000, double[] movableRepulseMultipliers = null, Vector[] existingStaticPoints = null, double[] staticRepulseMultipliers = null)
        {
            return EvenDistribution.GetCircular_CenterPacked(returnCount, existingStaticPoints, radius, stopRadiusPercent, stopIterationCount, movableRepulseMultipliers, staticRepulseMultipliers);
        }
        public static Vector[] GetRandomVectors_Circular_CenterPacked(Vector[] movable, double radius, double stopRadiusPercent = .03, int stopIterationCount = 1000, double[] movableRepulseMultipliers = null, Vector[] existingStaticPoints = null, double[] staticRepulseMultipliers = null)
        {
            return EvenDistribution.GetCircular_CenterPacked(movable, existingStaticPoints, radius, stopRadiusPercent, stopIterationCount, movableRepulseMultipliers, staticRepulseMultipliers);
        }

        public static Vector[] GetRandomVectors_Circular_ClusteredMinDist(int returnCount, double radius, double minDist, double stopRadiusPercent = .03, int stopIterationCount = 1000, double[] movableRepulseMultipliers = null, Vector[] existingStaticPoints = null, double[] staticRepulseMultipliers = null)
        {
            return EvenDistribution.GetCircular_ClusteredMinDist(returnCount, existingStaticPoints, radius, minDist, stopRadiusPercent, stopIterationCount, movableRepulseMultipliers, staticRepulseMultipliers);
        }
        public static Vector[] GetRandomVectors_Circular_ClusteredMinDist(Vector[] movable, double radius, double minDist, double stopRadiusPercent = .03, int stopIterationCount = 1000, double[] movableRepulseMultipliers = null, Vector[] existingStaticPoints = null, double[] staticRepulseMultipliers = null)
        {
            return EvenDistribution.GetCircular_ClusteredMinDist(movable, existingStaticPoints, radius, minDist, stopRadiusPercent, stopIterationCount, movableRepulseMultipliers, staticRepulseMultipliers);
        }

        public static Vector[] GetRandomVectors_CircularRing_EvenDist(int returnCount, double radius, double stopRadiusPercent = .03, int stopIterationCount = 1000, double[] movableRepulseMultipliers = null, Vector[] existingStaticPoints = null, double[] staticRepulseMultipliers = null)
        {
            //TODO: The exit conditions are a bit broken.  stopRadiusPercent will never hit (unless it's made too large, but that could cause the
            //method to exit too early).  So it will always run to stopIterationCount.
            return EvenDistribution.GetCircularRing_EvenDist(returnCount, existingStaticPoints, radius, stopRadiusPercent, stopIterationCount, movableRepulseMultipliers, staticRepulseMultipliers);
        }
        public static Vector[] GetRandomVectors_CircularRing_EvenDist(Vector[] movable, double radius, double stopRadiusPercent = .03, int stopIterationCount = 1000, double[] movableRepulseMultipliers = null, Vector[] existingStaticPoints = null, double[] staticRepulseMultipliers = null)
        {
            return EvenDistribution.GetCircularRing_EvenDist(movable, existingStaticPoints, radius, stopRadiusPercent, stopIterationCount, movableRepulseMultipliers, staticRepulseMultipliers);
        }

        public static Vector3D GetRandomVector_Cone(Vector3D axis, double maxAngle)
        {
            // Get a rotate axis that is always orthogonal to the center axis
            Vector3D rotateAxis = Vector3D.CrossProduct(axis, GetRandomVector_Spherical_Shell(1));

            return axis.GetRotatedVector(rotateAxis, StaticRandom.NextDouble(maxAngle));
        }

        public static Quaternion GetRandomRotation()
        {
            return new Quaternion(GetRandomVector_Spherical_Shell(1d), Math1D.GetNearZeroValue(360d));
        }

        /// <remarks>
        /// Got this here:
        /// http://adamswaab.wordpress.com/2009/12/11/random-point-in-a-triangle-barycentric-coordinates/
        /// </remarks>
        public static Point3D GetRandomPoint_InTriangle(Point3D a, Point3D b, Point3D c)
        {
            Vector3D ab = b - a;
            Vector3D ac = c - a;

            Random rand = StaticRandom.GetRandomForThread();
            double percentAB = rand.NextDouble();		// % along ab
            double percentAC = rand.NextDouble();		// % along ac

            if (percentAB + percentAC >= 1d)
            {
                // Mirror back onto the triangle (otherwise this would make a parallelogram)
                percentAB = 1d - percentAB;
                percentAC = 1d - percentAC;
            }

            // Now add the two weighted vectors to a
            return a + ((ab * percentAB) + (ac * percentAC));
        }
        public static Point3D GetRandomPoint_InTriangle(ITriangle triangle)
        {
            //Vector3D ab = b - a;
            //Vector3D ac = c - a;
            Vector3D ab = triangle.Point1 - triangle.Point0;
            Vector3D ac = triangle.Point2 - triangle.Point0;

            Random rand = StaticRandom.GetRandomForThread();
            double percentAB = rand.NextDouble();		// % along ab
            double percentAC = rand.NextDouble();		// % along ac

            if (percentAB + percentAC >= 1d)
            {
                // Mirror back onto the triangle (otherwise this would make a parallelogram)
                percentAB = 1d - percentAB;
                percentAC = 1d - percentAC;
            }

            // Now add the two weighted vectors to a
            return triangle.Point0 + ((ab * percentAB) + (ac * percentAC));
        }

        /// <summary>
        /// This returns a list of points evenly distributed across the surface of the hull passed in
        /// </summary>
        /// <remarks>
        /// Note that the triangles within hull don't need to actually be a continuous hull.  They can be a scattered mess of triangles, and
        /// the returned points will still be constrained to the surface of those triangles (evenly distributed across those triangles)
        /// </remarks>
        public static Point3D[] GetRandomPoints_OnHull(ITriangle[] hull, int numPoints)
        {
            // Calculate each triangle's % of the total area of the hull (sorted smallest to largest)
            int[] trianglePointers;
            double[] triangleSizes;
            GetRandomPoints_OnHullSprtSizes(out trianglePointers, out triangleSizes, hull);

            Random rand = StaticRandom.GetRandomForThread();

            Point3D[] retVal = new Point3D[numPoints];

            for (int cntr = 0; cntr < numPoints; cntr++)
            {
                // Pick a random location on the hull (a percent of the hull's size from 0% to 100%)
                double percent = rand.NextDouble();

                // Find the triangle that straddles this percent
                int index = GetRandomPoints_OnHullSprtFindTriangle(trianglePointers, triangleSizes, percent);

                // Create a point somewhere on this triangle
                retVal[cntr] = GetRandomPoint_InTriangle(hull[index]);
            }

            // Exit Function
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
        public static SortedList<int, List<Point3D>> GetRandomPoints_OnHull_Structured(ITriangle[] hull, int numPoints)
        {
            // Calculate each triangle's % of the total area of the hull (sorted smallest to largest)
            int[] trianglePointers;
            double[] triangleSizes;
            GetRandomPoints_OnHullSprtSizes(out trianglePointers, out triangleSizes, hull);

            Random rand = StaticRandom.GetRandomForThread();

            SortedList<int, List<Point3D>> retVal = new SortedList<int, List<Point3D>>();

            for (int cntr = 0; cntr < numPoints; cntr++)
            {
                // Pick a random location on the hull (a percent of the hull's size from 0% to 100%)
                double percent = rand.NextDouble();

                // Find the triangle that straddles this percent
                int index = GetRandomPoints_OnHullSprtFindTriangle(trianglePointers, triangleSizes, percent);

                if (!retVal.ContainsKey(index))
                {
                    retVal.Add(index, new List<Point3D>());
                }

                // Create a point somewhere on this triangle
                retVal[index].Add(GetRandomPoint_InTriangle(hull[index]));
            }

            // Exit Function
            return retVal;
        }

        /// <summary>
        /// This function will pick an arbitrary orthogonal to the vector passed in
        /// </summary>
        public static Vector3D GetArbitraryOrhonganal_FAIL(Vector3D vector)
        {
            // Clone the vector passed in
            Vector3D retVal = new Vector3D(vector.X, vector.Y, vector.Z);

            // Make sure that none of the values are equal to zero.
            if (retVal.X == 0) retVal.X = 0.000000001d;
            if (retVal.Y == 0) retVal.Y = 0.000000001d;
            if (retVal.Z == 0) retVal.Z = 0.000000001d;

            // Figure out the orthogonal X and Y slopes
            double orthM = (retVal.X * -1) / retVal.Y;
            double orthN = (retVal.Y * -1) / retVal.Z;

            // When calculating the new coords, I will default Y to 1, and find an X and Z that satisfy that.  I will go ahead and reuse the retVal
            retVal.Y = 1;
            retVal.X = 1 / orthM;
            retVal.Z = orthN;

            // Exit Function
            return retVal;
        }
        public static Vector3D GetArbitraryOrhonganal(Vector3D vector)
        {
            if (Math3D.IsInvalid(vector) || Math3D.IsNearZero(vector))
            {
                return new Vector3D(double.NaN, double.NaN, double.NaN);
            }

            Vector3D rand = Math3D.GetRandomVector(1);

            for (int cntr = 0; cntr < 10; cntr++)
            {
                Vector3D retVal = Vector3D.CrossProduct(vector, rand);

                if (Math3D.IsInvalid(retVal))
                {
                    rand = Math3D.GetRandomVector(1);
                }
                else
                {
                    return retVal;
                }
            }

            throw new ApplicationException("Infinite loop detected");
        }

        #endregion

        #region Misc

        /// <summary>
        /// This tests to see if the point is sitting on the triangle
        /// </summary>
        /// <param name="shouldTestCoplanar">Pass in false if you're already sure the point is coplanar with the triangle (saves from unnecessary processing)</param>
        public static bool IsNearTriangle(ITriangle triangle, Point3D testPoint, bool shouldTestCoplanar)
        {
            // Ensure coplanar
            if (shouldTestCoplanar)
            {
                double distFromPlane = DistanceFromPlane(triangle, testPoint);
                if (!Math1D.IsNearZero(distFromPlane))
                {
                    return false;
                }
            }

            // Ensure inside triangle
            Vector bary = Math3D.ToBarycentric(triangle, testPoint);
            if (Math1D.IsNearNegative(bary.X) || Math1D.IsNearNegative(bary.Y) || Math1D.IsNearPositive(bary.X + bary.Y - 1d))
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

            // Exit Function
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
            //Vector3D line01 = p1 - p0;
            //Vector3D line02 = p2 - p0;
            Vector3D line01 = p2 - p0;		// ToBarycentric has x as p2
            Vector3D line02 = p1 - p0;

            return p0 + (line01 * bary.X) + (line02 * bary.Y);
        }

        /// <summary>
        /// This function will rotate the vector around any arbitrary axis
        /// </summary>
        /// <param name="vector">The vector to get rotated</param>
        /// <param name="rotateAround">Any vector to rotate around</param>
        /// <param name="radians">How far to rotate</param>
        public static Vector3D RotateAroundAxis(Vector3D vector, Vector3D rotateAround, double radians)
        {
            // Create a quaternion that represents the axis and angle passed in
            Quaternion rotationQuat = new Quaternion(rotateAround, Math1D.RadiansToDegrees(radians));

            Matrix3D matrix = new Matrix3D();
            matrix.Rotate(rotationQuat);

            // Get a vector that represents me rotated by the quaternion
            Vector3D retVal = matrix.Transform(vector);

            // Exit Function
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
            // Grab the angle
            double angle = Vector3D.AngleBetween(from, to);
            if (Double.IsNaN(angle))
            {
                return Quaternion.Identity;
            }

            // I need to pull the cross product from me to the vector passed in
            Vector3D axis = Vector3D.CrossProduct(from, to);

            // If the cross product is zero, then there are two possibilities.  The vectors point in the same direction, or opposite directions.
            if (axis.IsZero())
            {
                // If I am here, then the angle will either be 0 or 180.
                if (angle == 0)
                {
                    // The vectors sit on top of each other.  I will set the orthoganal to an arbitrary value, and return zero for the radians
                    return Quaternion.Identity;
                }
                else
                {
                    // The vectors are pointing directly away from each other, so I will need to be more careful when I create my orthoganal.
                    axis = GetArbitraryOrhonganal(from);
                }
            }

            // Exit Function
            return new Quaternion(axis, angle);
        }
        /// <summary>
        /// This returns how much from should be rotated (and through what axis) to line up with to
        /// NOTE:  To rotate a vector pair (standard and orthogonal), see DoubleVector.GetAngleAroundAxis)
        /// </summary>
        public static void GetRotation(out Vector3D axis, out double radians, Vector3D from, Vector3D to)
        {
            // Grab the angle
            radians = Math1D.DegreesToRadians(Vector3D.AngleBetween(from, to));
            if (Double.IsNaN(radians))
            {
                radians = 0;
            }

            // I need to pull the cross product from me to the vector passed in
            axis = Vector3D.CrossProduct(from, to);

            // If the cross product is zero, then there are two possibilities.  The vectors point in the same direction, or opposite directions.
            if (axis.IsZero())
            {
                // If I am here, then the angle will either be 0 or PI.
                if (radians == 0)
                {
                    // The vectors sit on top of each other.  I will set the orthoganal to an arbitrary value, and return zero for the radians
                    axis.X = 1;
                    radians = 0;
                }
                else
                {
                    // The vectors are pointing directly away from each other, so I will need to be more careful when I create my orthoganal.
                    axis = GetArbitraryOrhonganal(from);
                }
            }

            //rotationAxis.BecomeUnitVector();		// It would be nice to be tidy, but not nessassary, and I don't want slow code
        }
        public static Quaternion GetRotation(DoubleVector from, DoubleVector to)
        {
            return GetRotation(from.Standard, from.Orth, to.Standard, to.Orth);
        }
        public static Quaternion GetRotation(Quaternion from, Quaternion to)
        {
            //http://stackoverflow.com/questions/1755631/difference-between-two-quaternions

            Quaternion fromInverse = from.ToUnit();
            fromInverse.Invert();
            return Quaternion.Multiply(to.ToUnit(), fromInverse);
        }
        /// <summary>
        /// This gets the rotation between the planes
        /// NOTE: This is just how to rotate from one plane to another.  There is no secondary rotation to get the points within the triangles to line up
        /// </summary>
        public static Quaternion GetRotation(ITriangle fromPlane, ITriangle toPlane)
        {
            return GetRotation(fromPlane.Normal, toPlane.Normal);
        }
        /// <summary>
        /// This is more exact than the plane overload
        /// </summary>
        /// <remarks>
        /// from1 and 2 define one plane, to1 and 2 define another plane
        /// 
        /// It is expected that to is a rotated version of from
        /// (at least from1 and to1.  from2 and to2 are only used to get the plane level rotation.)
        /// In other words, once the plane rotation is calculated, there is a secondary rotation that maps from1 to to1
        /// 
        /// from2 doesn't need to be orthogonal to from1, just not parallel
        /// </remarks>
        public static Quaternion GetRotation(Vector3D from1, Vector3D from2, Vector3D to1, Vector3D to2)
        {
            // Calculate normals
            Vector3D fromNormalUnit = Vector3D.CrossProduct(from1, from2).ToUnit();
            Vector3D toNormalUnit = Vector3D.CrossProduct(to1, to2).ToUnit();

            if (IsInvalid(fromNormalUnit) || IsInvalid(toNormalUnit))     // if the normals are invalid, then the two directions are colinear (or one is zero length, or invalid)
            {
                //TODO: May want to throw an exception instead
                return Quaternion.Identity;
            }

            // Detect Parallel
            if (Math1D.IsNearValue(Math.Abs(Vector3D.DotProduct(fromNormalUnit, toNormalUnit)), 1d))
            {
                return Math3D.GetRotation(from1, to1);
            }

            // Figure out how to rotate the planes onto each other
            Quaternion planeRotation = GetRotation(fromNormalUnit, toNormalUnit);

            // Rotate from onto to
            Vector3D rotated1 = planeRotation.GetRotatedVector(from1);

            // Now that they are in the same plane, rotate the vectors onto each other
            Quaternion secondRotation = GetRotation(rotated1, to1);

            // Combine the two rotations
            return Quaternion.Multiply(secondRotation, planeRotation);		// note that order is important (stand, orth is wrong)
        }

        /// <summary>
        /// This returns a vector that is orthogonal to standard, and in the same plane as direction
        /// </summary>
        public static Vector3D GetOrthogonal(Vector3D standard, Vector3D direction)
        {
            Vector3D cross = Vector3D.CrossProduct(standard, direction);		// getting an orthogonal of the two vectors passed in
            return Vector3D.CrossProduct(cross, standard);		// now get an orthogonal pointing in the same direction as direction
        }

        /// <summary>
        /// This returns the center of position of the points
        /// </summary>
        public static Point3D GetCenter(IEnumerable<Point3D> points)
        {
            if (points == null)
            {
                return new Point3D(0, 0, 0);
            }

            double x = 0d;
            double y = 0d;
            double z = 0d;

            int length = 0;

            foreach (Point3D point in points)
            {
                x += point.X;
                y += point.Y;
                z += point.Z;

                length++;
            }

            if (length == 0)
            {
                return new Point3D(0, 0, 0);
            }

            double oneOverLen = 1d / Convert.ToDouble(length);

            return new Point3D(x * oneOverLen, y * oneOverLen, z * oneOverLen);
        }
        /// <summary>
        /// This returns the center of mass of the points
        /// </summary>
        public static Point3D GetCenter(Tuple<Point3D, double>[] pointsMasses)
        {
            if (pointsMasses == null || pointsMasses.Length == 0)
            {
                return new Point3D(0, 0, 0);
            }

            double totalMass = pointsMasses.Sum(o => o.Item2);
            if (Math1D.IsNearZero(totalMass))
            {
                return GetCenter(pointsMasses.Select(o => o.Item1).ToArray());
            }

            double x = 0d;
            double y = 0d;
            double z = 0d;

            foreach (var pointMass in pointsMasses)
            {
                x += pointMass.Item1.X * pointMass.Item2;
                y += pointMass.Item1.Y * pointMass.Item2;
                z += pointMass.Item1.Z * pointMass.Item2;
            }

            double totalMassInverse = 1d / totalMass;

            return new Point3D(x * totalMassInverse, y * totalMassInverse, z * totalMassInverse);
        }

        /// <summary>
        /// This is identical to GetCenter.  (with points, that is thought of as the center.  With vectors, that's thought of as the
        /// average - even though it's the same logic)
        /// </summary>
        public static Vector3D GetAverage(IEnumerable<Vector3D> vectors)
        {
            if (vectors == null)
            {
                return new Vector3D(0, 0, 0);
            }

            double x = 0d;
            double y = 0d;
            double z = 0d;

            int length = 0;

            foreach (Vector3D vector in vectors)
            {
                x += vector.X;
                y += vector.Y;
                z += vector.Z;

                length++;
            }

            if (length == 0)
            {
                return new Vector3D(0, 0, 0);
            }

            double oneOverLen = 1d / Convert.ToDouble(length);

            return new Vector3D(x * oneOverLen, y * oneOverLen, z * oneOverLen);
        }

        public static Vector3D GetSum(IEnumerable<Vector3D> vectors)
        {
            if (vectors == null)
            {
                return new Vector3D(0, 0, 0);
            }

            double x = 0d;
            double y = 0d;
            double z = 0d;

            foreach (Vector3D vector in vectors)
            {
                x += vector.X;
                y += vector.Y;
                z += vector.Z;
            }

            return new Vector3D(x, y, z);
        }

        //TODO: See if there is a way to do something like points.Distinct((o,p) => IsNearValue(o,p))
        public static Point3D[] GetUnique(IEnumerable<Point3D> points)
        {
            List<Point3D> retVal = new List<Point3D>();

            foreach (Point3D point in points)
            {
                if (!retVal.Any(o => IsNearValue(o, point)))
                {
                    retVal.Add(point);
                }
            }

            return retVal.ToArray();
        }
        public static Vector3D[] GetUnique(IEnumerable<Vector3D> vectors)
        {
            List<Vector3D> retVal = new List<Vector3D>();

            foreach (Vector3D vector in vectors)
            {
                if (!retVal.Any(o => IsNearValue(o, vector)))
                {
                    retVal.Add(vector);
                }
            }

            return retVal.ToArray();
        }

        public static Tuple<Point3D, Point3D> GetAABB(ITriangle triangle)
        {
            return GetAABB(new Point3D[] { triangle.Point0, triangle.Point1, triangle.Point2 });
        }
        public static Rect3D GetAABB_Rect(ITriangle triangle)
        {
            return GetAABB_Rect(new Point3D[] { triangle.Point0, triangle.Point1, triangle.Point2 });
        }
        public static Tuple<Point3D, Point3D> GetAABB(IEnumerable<ITriangle> triangles)
        {
            return GetAABB(triangles.SelectMany(o => new[] { o.Point0, o.Point1, o.Point2 }));
        }
        public static Rect3D GetAABB_Rect(IEnumerable<ITriangle> triangles)
        {
            return GetAABB_Rect(triangles.SelectMany(o => new[] { o.Point0, o.Point1, o.Point2 }));
        }
        public static Tuple<Point3D, Point3D> GetAABB(IEnumerable<Edge3D> edges, double rayLength)
        {
            return GetAABB(edges.SelectMany(o => new[] { o.Point0, o.GetPoint1Ext(rayLength) }));
        }
        public static Rect3D GetAABB_Rect(IEnumerable<Edge3D> edges, double rayLength)
        {
            return GetAABB_Rect(edges.SelectMany(o => new[] { o.Point0, o.GetPoint1Ext(rayLength) }));
        }
        public static Tuple<Point3D, Point3D> GetAABB(IEnumerable<Point3D> points)
        {
            bool foundOne = false;
            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double minZ = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;
            double maxZ = double.MinValue;

            foreach (Point3D point in points)
            {
                foundOne = true;        // it's too expensive to look at points.Count()

                if (point.X < minX)
                {
                    minX = point.X;
                }

                if (point.Y < minY)
                {
                    minY = point.Y;
                }

                if (point.Z < minZ)
                {
                    minZ = point.Z;
                }

                if (point.X > maxX)
                {
                    maxX = point.X;
                }

                if (point.Y > maxY)
                {
                    maxY = point.Y;
                }

                if (point.Z > maxZ)
                {
                    maxZ = point.Z;
                }
            }

            if (!foundOne)
            {
                // There were no points passed in
                //TODO: May want an exception
                return Tuple.Create(new Point3D(0, 0, 0), new Point3D(0, 0, 0));
            }

            // Exit Function
            return Tuple.Create(new Point3D(minX, minY, minZ), new Point3D(maxX, maxY, maxZ));
        }
        public static Rect3D GetAABB_Rect(IEnumerable<Point3D> points)
        {
            var aabb = GetAABB(points);
            return new Rect3D(aabb.Item1, (aabb.Item2 - aabb.Item1).ToSize());
        }

        public static Point3D LERP(Point3D a, Point3D b, double percent)
        {
            return new Point3D(
                a.X + (b.X - a.X) * percent,
                a.Y + (b.Y - a.Y) * percent,
                a.Z + (b.Z - a.Z) * percent);
        }
        public static Vector3D LERP(Vector3D a, Vector3D b, double percent)
        {
            return new Vector3D(
                a.X + (b.X - a.X) * percent,
                a.Y + (b.Y - a.Y) * percent,
                a.Z + (b.Z - a.Z) * percent);
        }

        /// <summary>
        /// This returns a triangle that represents the plane
        /// </summary>
        /// <param name="point">A point on the plane</param>
        /// <param name="normal">A vector that is perpendicular to the plane</param>
        public static ITriangle GetPlane(Point3D point, Vector3D normal)
        {
            Vector3D orth1 = Math3D.GetArbitraryOrhonganal(normal);
            Vector3D orth2 = Vector3D.CrossProduct(normal, orth1);

            return new Triangle(point, point + orth1, point + orth2);
        }

        /// <summary>
        /// This returns the circumsphere of 4 points
        /// </summary>
        /// <remarks>
        /// Got this here:
        /// http://people.sc.fsu.edu/~jburkardt/cpp_src/tetrahedron_properties/tetrahedron_properties.html
        /// </remarks>
        public static Tuple<Point3D, double> GetCircumsphere(Point3D p0, Point3D p1, Point3D p2, Point3D p3)
        {
            double[] tetraVerts = new[]
            {
                p0.X, p0.Y, p0.Z,
                p1.X, p1.Y, p1.Z,
                p2.X, p2.Y, p2.Z,
                p3.X, p3.Y, p3.Z,
            };

            double radius;
            double[] centerVerts;
            tetrahedron_circumsphere(tetraVerts, out radius, out centerVerts);

            if (radius < 0)
            {
                throw new ApplicationException("Couldn't find a circumsphere.  This tetrahedron is probably coplanar");
            }
            else
            {
                return Tuple.Create(new Point3D(centerVerts[0], centerVerts[1], centerVerts[2]), radius);
            }
        }

        // This came from Game.Orig.Math3D.TorqueBall
        public static void SplitForceIntoTranslationAndTorque(out Vector3D translationForce, out Vector3D torque, Vector3D offsetFromCenterMass, Vector3D force)
        {
            // Torque is how much of the force is applied perpendicular to the radius
            torque = Vector3D.CrossProduct(offsetFromCenterMass, force);

            // I'm still not convinced this is totally right, but none of the articles I've read seem to do anything different
            translationForce = force;
        }

        /// <summary>
        /// This returns a convex hull of triangles that uses the outermost points (uses the quickhull algorithm)
        /// </summary>
        public static TriangleIndexed[] GetConvexHull(Point3D[] points)
        {
            return QuickHull3D.GetConvexHull(points);
        }

        public static Tetrahedron[] GetDelaunay(Point3D[] points)
        {
            return DelaunayVoronoi3D.GetDelaunay(points);
        }
        public static VoronoiResult3D GetVoronoi(Point3D[] points, bool buildFaces)
        {
            return DelaunayVoronoi3D.GetVoronoi(points, buildFaces);
        }

        /// <param name="isHullSubsetOfAllPoints">
        /// True=The hull doesn't use all of AllPoints, so an extra step is needed to figure out which of those points are used.
        /// False=The center is just the average of AllPoints (this is cheaper to calculate, but will give a bad result if any of AllPoints isn't used).
        /// </param>
        public static double GetVolume_ConvexHull(TriangleIndexed[] hull, bool isHullSubsetOfAllPoints = false)
        {
            if (hull.Length == 0)
            {
                return 0;
            }

            // Add up the sum of the tetrahedrons formed by the faces going toward the center point
            //http://en.wikipedia.org/wiki/Tetrahedron#Volume

            Point3D center;
            if (isHullSubsetOfAllPoints)
            {
                Point3D[] allPoints = hull[0].AllPoints;
                center = GetCenter(hull.SelectMany(o => o.IndexArray).Distinct().Select(o => allPoints[o]));        // only use the points that are referenced by the triangles in hull
            }
            else
            {
                center = GetCenter(hull[0].AllPoints);
            }

            double retVal = 0d;

            foreach (ITriangle triangle in hull)
            {
                // Volume of a triangular pyramid is one third area of base times height
                retVal += ((triangle.NormalLength / 2d) * Math.Abs(DistanceFromPlane(triangle, center))) / 3d;
            }

            return retVal;
        }

        /// <summary>
        /// If a triangle has an edge longer than the length passed in, it will get chopped into smaller triangles
        /// </summary>
        /// <param name="maxPasses">
        /// A pass is one look at all the triangles, a second pass is a recursion.  If maxEdgeLength is excessively
        /// small, and maxPasses is too large, the number of triangles gets insane.  So maxPasses is a safeguard
        /// against runaway recursion
        /// </param>
        public static ITriangle[] SliceLargeTriangles(ITriangle[] triangles, double maxEdgeLength, int maxPasses = 3)
        {
            return SliceTriangles.Slice(triangles, maxEdgeLength, maxPasses);
        }
        /// <summary>
        /// This is the same as SliceLargeTriangles, but each time a triangle is sliced, the new midpoint of each edge
        /// is projected onto a curve
        /// </summary>
        public static ITriangleIndexed[] SliceLargeTriangles_Smooth(ITriangleIndexed[] triangles, double maxEdgeLength, int maxPasses = 3)
        {
            return SliceTriangles.Slice_Smooth(triangles, maxEdgeLength, maxPasses);
        }

        public static ITriangleIndexed[] RemoveThinTriangles(ITriangleIndexed[] triangles, double skipThinRatio = .9)
        {
            return RemoveThin.ThinIt(triangles, skipThinRatio);
        }

        public static ITriangleIndexed[] SortByPlaneDistance(ITriangleIndexed[] triangles, Point3D pointOnPlane, Vector3D planeNormal)
        {
            //NOTE: This method assumes that the plane is away from all the triangles.  If it cuts through the triangles, the results will be a bit odd

            if (triangles.Length == 0)
            {
                return triangles;
            }

            Vector3D normalUnit = planeNormal.ToUnit();
            double originDistance = GetPlaneOriginDistance(normalUnit, pointOnPlane);

            Point3D[] allPoints = triangles[0].AllPoints;     // this assumes that all triangles share the same point list

            // Sort the points by their distance to the plane
            var planeDistances = Enumerable.Range(0, allPoints.Length).
                Select(o => Tuple.Create(o, DistanceFromPlane(normalUnit, originDistance, allPoints[o]))).
                OrderBy(o => o.Item2).
                ToArray();

            //Key=Index into allPoints
            //Value=Index into planeDistances (the lower, the closer it is to the plane)
            SortedList<int, int> indexMap = new SortedList<int, int>();

            for (int cntr = 0; cntr < planeDistances.Length; cntr++)
            {
                indexMap.Add(planeDistances[cntr].Item1, cntr);
            }

            // OrderBy needs a tuple when ordering by multiple items.  So create a tuple for each triangle, that is
            // locally ordered by distance to plane.
            Tuple<int, Tuple<int, int, int>>[] ranks = new Tuple<int, Tuple<int, int, int>>[triangles.Length];

            for (int cntr = 0; cntr < triangles.Length; cntr++)
            {
                int[] subRank = triangles[cntr].IndexArray.Select(o => indexMap[o]).OrderBy(o => o).ToArray();

                ranks[cntr] = Tuple.Create(cntr, Tuple.Create(subRank[0], subRank[1], subRank[2]));
            }

            //var debug = ranks.OrderBy(o => o.Item2).ToArray();

            // Now sort the entire list to see which triangles are closest, and return the triangles according to that order
            return ranks.OrderBy(o => o.Item2).Select(o => triangles[o.Item1]).ToArray();
        }

        public static Vector3D GetNewVector(Axis axis, double value)
        {
            switch (axis)
            {
                case Axis.X:
                    return new Vector3D(value, 0, 0);

                case Axis.Y:
                    return new Vector3D(0, value, 0);

                case Axis.Z:
                    return new Vector3D(0, 0, value);

                default:
                    throw new ApplicationException("Unknown Axis: " + axis.ToString());
            }
        }
        public static Point3D GetNewPoint(Axis axis, double value)
        {
            switch (axis)
            {
                case Axis.X:
                    return new Point3D(value, 0, 0);

                case Axis.Y:
                    return new Point3D(0, value, 0);

                case Axis.Z:
                    return new Point3D(0, 0, value);

                default:
                    throw new ApplicationException("Unknown Axis: " + axis.ToString());
            }
        }

        public static Tuple<int, int, double>[] GetDistancesBetween(Point3D[] positions)
        {
            List<Tuple<int, int, double>> retVal = new List<Tuple<int, int, double>>();

            for (int outer = 0; outer < positions.Length - 1; outer++)
            {
                for (int inner = outer + 1; inner < positions.Length; inner++)
                {
                    double distance = (positions[outer] - positions[inner]).Length;
                    retVal.Add(Tuple.Create(outer, inner, distance));
                }
            }

            return retVal.ToArray();
        }

        public static Point3D[] ApplyBallOfSprings(Point3D[] positions, Tuple<int, int, double>[] desiredDistances, int numIterations)
        {
            double[][] pos = positions.
                Select(o => new[] { o.X, o.Y, o.Z }).
                ToArray();

            double[][] retVal = MathND.ApplyBallOfSprings(pos, desiredDistances, numIterations);

            return retVal.
                Select(o => new Point3D(o[0], o[1], o[2])).
                ToArray();
        }

        /// <summary>
        /// Dot product of unit vectors is cosine.  This makes it look roughly linear
        /// NOTE: You must pass in unit vectors, or the results will be wild
        /// </summary>
        public static double GetLinearDotProduct(Vector3D unit1, Vector3D unit2)
        {
            //TODO: This was just visually determined on a graphing calculator.  It could be refined
            const double POW = .57;

            double dot = Vector3D.DotProduct(unit1, unit2);

            if (dot >= 0)
            {
                // This will be roughly y=-x (in the range 0 to 1)
                return 1 - Math.Pow(1 - dot, POW);
            }
            else
            {
                // There doesn't seem to be any power that can directly convert the negative side to a near linear.  The abs of the negative
                // looks like the first 90 degrees of a sine.  So take the arcsine to get the theta that made that sine.  With that theta, take the
                // cos.  Now it is the same shape as the positive side
                double cos = Math.Cos(Math.Asin(-dot));

                // Do similar to the positive side, then invert
                return -Math.Pow(1 - cos, POW);
            }
        }

        #endregion

        #region Intersections

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
        public static bool IsIntersecting_AABB_Sphere(Point3D min, Point3D max, Point3D center, double radius)
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
        public static bool IsIntersecting_AABB_AABB(Point3D min1, Point3D max1, Point3D min2, Point3D max2)
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
        /// This returns whether the point is inside all the planes (the triangles don't define finite triangles, but whole planes)
        /// NOTE: Make sure the normals point outward, or there will be odd results
        /// </summary>
        /// <remarks>
        /// This is a reworked copy of QuickHull3D.GetOutsideSet, which was inspired by:
        /// http://www.gamedev.net/topic/106765-determining-if-a-point-is-in-front-of-or-behind-a-plane/
        /// </remarks>
        public static bool IsInside_Planes(IEnumerable<ITriangle> planes, Point3D testPoint)
        {
            foreach (ITriangle plane in planes)
            {
                if (IsAbovePlane(plane, testPoint, true))
                {
                    return false;
                }
            }

            return true;
        }
        public static bool IsInside_AABB(Point3D min, Point3D max, Point3D testPoint)
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
        public static bool IsInside_AABB(Vector3D min, Vector3D max, Vector3D testPoint)
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

        public static bool IsAbovePlane(ITriangle plane, Point3D testPoint, bool trueIfOnPlane = false)
        {
            // Compute D, using a arbitrary point P, that lies on the plane: D = - (Nx*Px + Ny*Py + Nz*Pz); Don't forget the inversion !
            double d = -((plane.NormalUnit.X * plane.Point0.X) + (plane.NormalUnit.Y * plane.Point0.Y) + (plane.NormalUnit.Z * plane.Point0.Z));

            // You can test a point (T) with respect to the plane using the plane equation: res = Nx*Tx + Ny*Ty + Nz*Tz + D
            double res = (plane.NormalUnit.X * testPoint.X) + (plane.NormalUnit.Y * testPoint.Y) + (plane.NormalUnit.Z * testPoint.Z) + d;

            if (res > 0)
            {
                return true;        // above the plane
            }
            else if (trueIfOnPlane && Math1D.IsNearZero(res))
            {
                return true;        // on the plane
            }
            else
            {
                return false;       // below the plane
            }
        }

        /// <summary>
        /// This returns a point along the line that is the shortest distance to the test point
        /// NOTE:  The line passed in is assumed to be infinite, not a line segment
        /// </summary>
        /// <param name="pointOnLine">Any arbitrary point along the line</param>
        /// <param name="lineDirection">The direction of the line (slope in x,y,z)</param>
        /// <param name="testPoint">The point that is not on the line</param>
        public static Point3D GetClosestPoint_Line_Point(Point3D pointOnLine, Vector3D lineDirection, Point3D testPoint)
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
        public static double GetClosestDistance_Line_Point(Point3D pointOnLine, Vector3D lineDirection, Point3D testPoint)
        {
            return (testPoint - GetClosestPoint_Line_Point(pointOnLine, lineDirection, testPoint)).Length;
        }

        public static Point3D GetClosestPoint_Plane_Point(ITriangle plane, Point3D testPoint)
        {
            Point3D? retVal = GetIntersection_Plane_Line(plane, testPoint, plane.Normal);
            if (retVal == null)
            {
                throw new ApplicationException("Intersection between a plane and its normal should never be null");
            }

            return retVal.Value;
        }

        public static Point3D GetClosestPoint_Triangle_Point(ITriangle triangle, Point3D testPoint)
        {
            Point3D pointOnPlane = GetClosestPoint_Plane_Point(triangle, testPoint);

            Vector bary = ToBarycentric(triangle, pointOnPlane);

            if (bary.X >= 0 && bary.Y >= 0 && bary.X + bary.Y <= 1)
            {
                // It's inside the triangle
                return pointOnPlane;
            }

            // Cap to one of the edges
            if (bary.X < 0)
            {
                return GetClosestPoint_LineSegment_Point(triangle.Point0, triangle.Point1, testPoint);      // see the comments in ToBarycentric for how I know which points to use
            }
            else if (bary.Y < 0)
            {
                return GetClosestPoint_LineSegment_Point(triangle.Point0, triangle.Point2, testPoint);
            }
            else
            {
                return GetClosestPoint_LineSegment_Point(triangle.Point1, triangle.Point2, testPoint);
            }
        }
        public static double GetClosestDistance_Triangle_Point(ITriangle triangle, Point3D testPoint)
        {
            return (testPoint - GetClosestPoint_Triangle_Point(triangle, testPoint)).Length;
        }

        /// <summary>
        /// This returns the distance beween two skew lines at their closest point
        /// </summary>
        /// <remarks>
        /// http://2000clicks.com/mathhelp/GeometryPointsAndLines3D.aspx
        /// </remarks>
        public static double GetClosestDistance_Line_Line(Point3D point1, Vector3D dir1, Point3D point2, Vector3D dir2)
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
        public static bool GetClosestPoints_Line_Line(out Point3D? resultPoint1, out Point3D? resultPoint2, Point3D point1, Vector3D dir1, Point3D point2, Vector3D dir2)
        {
            return GetClosestPoints_Line_Line(out resultPoint1, out resultPoint2, point1, point1 + dir1, point2, point2 + dir2);
        }
        public static bool GetClosestPoints_Line_Line(out Point3D? resultPoint1, out Point3D? resultPoint2, Point3D line1Point1, Point3D line1Point2, Point3D line2Point1, Point3D line2Point2)
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
        public static bool GetClosestPoints_Line_LineSegment(out Point3D[] resultPointsLine, out Point3D[] resultPointsLineSegment, Point3D pointOnLine, Vector3D lineDirection, Point3D lineSegmentStart, Point3D lineSegmentStop)
        {
            Point3D? result1, result2;
            if (!GetClosestPoints_Line_Line(out result1, out result2, pointOnLine, pointOnLine + lineDirection, lineSegmentStart, lineSegmentStop))
            {
                // If line/line fails, it's because they are parallel.  If the lines coincide, then return the segment start and stop


                //TODO: Finish this



                resultPointsLine = new Point3D[0];
                resultPointsLineSegment = new Point3D[0];
                return false;
            }

            // Make sure the line segment result isn't beyond the line segment
            Vector3D segmentDirLen = lineSegmentStop - lineSegmentStart;
            Vector3D resultDirLen = result2.Value - lineSegmentStart;

            if (!Math1D.IsNearValue(Vector3D.DotProduct(segmentDirLen.ToUnit(), resultDirLen.ToUnit()), 1d))
            {
                // It's the other direction (beyond segment start)
                resultPointsLine = new Point3D[0];
                resultPointsLineSegment = new Point3D[0];
                return false;
            }

            if (resultDirLen.LengthSquared > segmentDirLen.LengthSquared)
            {
                // It's beyond segment stop
                resultPointsLine = new Point3D[0];
                resultPointsLineSegment = new Point3D[0];
                return false;
            }

            // Return single points (this is the standard flow)
            resultPointsLine = new Point3D[] { result1.Value };
            resultPointsLineSegment = new Point3D[] { result2.Value };
            return true;
        }

        public static Point3D GetClosestPoint_LineSegment_Point(Point3D segmentStart, Point3D segmentStop, Point3D testPoint)
        {
            Vector3D lineDir = segmentStop - segmentStart;

            Point3D retVal = GetClosestPoint_Line_Point(segmentStart, lineDir, testPoint);

            Vector3D returnDir = retVal - segmentStart;

            if (Vector3D.DotProduct(lineDir, returnDir) < 0)
            {
                // It's going in the wrong direction, so start point is the closest
                return segmentStart;
            }
            else if (returnDir.LengthSquared > lineDir.LengthSquared)
            {
                // It's past the segment stop
                return segmentStop;
            }
            else
            {
                // The return point is sitting somewhere on the line segment
                return retVal;
            }
        }

        public static Point3D? GetClosestPoint_Circle_Point(ITriangle circlePlane, Point3D circleCenter, double circleRadius, Point3D testPoint)
        {
            // Project the test point onto the circle's plane
            Point3D planePoint = GetClosestPoint_Plane_Point(circlePlane, testPoint);

            if (IsNearValue(planePoint, circleCenter))
            {
                // The test point is directly over the center of the circle (or is the center of the circle)
                return null;
            }

            // Get the line from the circle's center to that point
            Vector3D line = planePoint - circleCenter;

            // Project out to the length of the circle
            return circleCenter + (line.ToUnit() * circleRadius);
        }

        public static Point3D? GetClosestPoint_Cylinder_Point(Point3D pointOnAxis, Vector3D axisDirection, double radius, Point3D testPoint)
        {
            // Get the shortest point between the cylinder's axis and the test point
            Point3D nearestAxisPoint = GetClosestPoint_Line_Point(pointOnAxis, axisDirection, testPoint);

            // Get the line from that point to the test point
            Vector3D line = testPoint - nearestAxisPoint;

            if (IsNearZero(line))
            {
                // The test point is sitting on the axis
                return null;
            }

            // Project out to the radius of the cylinder
            return nearestAxisPoint + (line.ToUnit() * radius);
        }

        public static Point3D? GetClosestPoint_Sphere_Point(Point3D centerPoint, double radius, Point3D testPoint)
        {
            if (IsNearValue(centerPoint, testPoint))
            {
                // The test point is the center of the sphere
                return null;
            }

            // Get the line from the center to the test point
            Vector3D line = testPoint - centerPoint;

            // Project out to the radius of the sphere
            return centerPoint + (line.ToUnit() * radius);
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
        public static bool GetClosestPoints_Circle_Line(out Point3D[] circlePoints, out Point3D[] linePoints, ITriangle circlePlane, Point3D circleCenter, double circleRadius, Point3D pointOnLine, Vector3D lineDirection, RayCastReturn returnWhich)
        {
            // There are too many loose variables, so package them up
            CircleLineArgs args = new CircleLineArgs()
            {
                CirclePlane = circlePlane,
                CircleCenter = circleCenter,
                CircleRadius = circleRadius,
                PointOnLine = pointOnLine,
                LineDirection = lineDirection
            };

            // Call the overload
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

            //// There is more than one point, and they want a single point
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

            //// Return only the closest point
            //circlePoints = new Point3D[] { circlePoints[minIndex] };
            //linePoints = new Point3D[] { linePoints[minIndex] };
            //return true;

            #endregion
        }

        public static bool GetClosestPoints_Cylinder_Line(out Point3D[] cylinderPoints, out Point3D[] linePoints, Point3D pointOnAxis, Vector3D axisDirection, double radius, Point3D pointOnLine, Vector3D lineDirection, RayCastReturn returnWhich)
        {
            // Get the shortest point between the cylinder's axis and the line
            Point3D? nearestAxisPoint, nearestLinePoint;
            if (!GetClosestPoints_Line_Line(out nearestAxisPoint, out nearestLinePoint, pointOnAxis, axisDirection, pointOnLine, lineDirection))
            {
                // The axis and line are parallel
                cylinderPoints = null;
                linePoints = null;
                return false;
            }

            Vector3D nearestLine = nearestLinePoint.Value - nearestAxisPoint.Value;
            double nearestDistance = nearestLine.Length;

            if (nearestDistance >= radius)
            {
                // Sitting outside the cylinder, so just project the line to the cylinder wall
                cylinderPoints = new Point3D[] { nearestAxisPoint.Value + (nearestLine.ToUnit() * radius) };
                linePoints = new Point3D[] { nearestLinePoint.Value };
                return true;
            }

            // The rest of this function is for a line intersect inside the cylinder (there's always two intersect points)

            // Make a plane that the circle sits in (this is used by code shared with the circle/line intersect)
            //NOTE: The plane is using nearestAxisPoint, and not the arbitrary point that was passed in (this makes later logic easier)
            Vector3D circlePlaneLine1 = Math1D.IsNearZero(nearestDistance) ? GetArbitraryOrhonganal(axisDirection) : nearestLine;
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
                    // Nothing more to do
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

        public static void GetClosestPoints_Sphere_Line(out Point3D[] spherePoints, out Point3D[] linePoints, Point3D centerPoint, double radius, Point3D pointOnLine, Vector3D lineDirection, RayCastReturn returnWhich)
        {
            // Get the shortest point between the sphere's center and the line
            Point3D nearestLinePoint = GetClosestPoint_Line_Point(pointOnLine, lineDirection, centerPoint);

            Vector3D nearestLine = nearestLinePoint - centerPoint;
            double nearestDistance = nearestLine.Length;

            if (nearestDistance >= radius)
            {
                // Sitting outside the sphere, so just project the line to the sphere wall
                spherePoints = new Point3D[] { centerPoint + (nearestLine.ToUnit() * radius) };
                linePoints = new Point3D[] { nearestLinePoint };
                return;
            }

            // The rest of this function is for a line intersect inside the sphere (there's always two intersect points)

            // Make a plane that the circle sits in (this is used by code shared with the circel/line intersect)
            //NOTE: The plane is oriented along the shortest path line
            Vector3D circlePlaneLine1 = Math1D.IsNearZero(nearestDistance) ? new Vector3D(1, 0, 0) : nearestLine;
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

            // Get the circle intersects (since the line is on the circle's plane, this is the final answer)
            GetClosestPointsBetweenLineCircleSprtInsidePerps(out spherePoints, out linePoints, args, intersectArgs);

            switch (returnWhich)
            {
                case RayCastReturn.AllPoints:
                    // Nothing more to do
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

        #region FLAWED

        ///// <summary>
        ///// This finds the intersection point of two lines.  If they are parallel or skew, null is returned
        ///// </summary>
        ///// <remarks>
        ///// Got this here:
        ///// http://stackoverflow.com/questions/2316490/the-algorithm-to-find-the-point-of-intersection-of-two-3d-line-segment
        ///// 
        ///// Which references here:
        ///// http://mathforum.org/library/drmath/view/62814.html
        ///// 
        ///// Thanks for writing to Doctor Math.
        ///// 
        ///// Let's try this with vector algebra. First write the two equations like 
        ///// this.
        ///// 
        /////   L1 = P1 + a V1
        ///// 
        /////   L2 = P2 + b V2
        ///// 
        ///// P1 and P2 are points on each line. V1 and V2 are the direction vectors 
        ///// for each line.
        ///// 
        ///// If we assume that the lines intersect, we can look for the point on L1 
        ///// that satisfies the equation for L2. This gives us this equation to 
        ///// solve.
        ///// 
        /////   P1 + a V1 = P2 + b V2
        ///// 
        ///// Now rewrite it like this.
        ///// 
        /////   a V1 = (P2 - P1) + b V2
        ///// 
        ///// Now take the cross product of each side with V2. This will make the 
        ///// term with 'b' drop out.
        ///// 
        /////   a (V1 X V2) = (P2 - P1) X V2
        ///// 
        ///// If the lines intersect at a single point, then the resultant vectors 
        ///// on each side of this equation must be parallel, and the left side must 
        ///// not be the zero vector. We should check to make sure that this is 
        ///// true. Once we have checked this, we can solve for 'a' by taking the 
        ///// magnitude of each side and dividing. If the resultant vectors are 
        ///// parallel, but in opposite directions, then 'a' is the negative of the 
        ///// ratio of magnitudes. Once we have 'a' we can go back to the equation 
        ///// for L1 to find the intersection point.
        ///// 
        ///// Write back if you need more help with this.
        ///// 
        ///// - Doctor George, The Math Forum
        /////   http://mathforum.org/dr.math/ 		
        ///// </remarks>
        //private static Point3D? GetIntersection_Line_Line_BAD(Point3D pointOnLine1, Vector3D lineDirection1, Point3D pointOnLine2, Vector3D lineDirection2)
        //{
        //    throw new ApplicationException("This method is broken, finish the alternate (it looks like it's cheaper)");

        //    // a (V1 X V2) = (P2 - P1) X V2
        //    // and solve for a

        //    // Convert to unit vectors
        //    //Vector3D v1 = lineDirection1.ToUnit();
        //    //Vector3D v2 = lineDirection2.ToUnit();

        //    Vector3D p2_p1 = pointOnLine2 - pointOnLine1;

        //    //Vector3D leftCross = Vector3D.CrossProduct(v1, v2);
        //    //Vector3D rightCross = Vector3D.CrossProduct(p2_p1, v2);
        //    Vector3D leftCross = Vector3D.CrossProduct(lineDirection1, lineDirection2);
        //    Vector3D rightCross = Vector3D.CrossProduct(p2_p1, lineDirection2);

        //    // Make sure they are parallel
        //    //double dot = Vector3D.DotProduct(leftCross, rightCross.ToUnit());		// left cross is the cross of 2 units, so is also a unit vector
        //    double dot = Vector3D.DotProduct(leftCross.ToUnit(), rightCross.ToUnit());
        //    if (!IsNearValue(Math.Abs(dot), 1d))
        //    {
        //        return null;
        //    }

        //    // Figure out the length
        //    //double lengthA = 1d / p2_p1.Length;
        //    double lengthA = leftCross.Length / rightCross.Length;
        //    if (dot < 0)
        //    {
        //        lengthA *= -1;
        //    }

        //    return (lineDirection1 * lengthA).ToPoint();
        //}
        ///// <remarks>
        ///// Got this here:
        ///// http://mathforum.org/library/drmath/view/63719.html
        ///// 
        ///// Define line 1 to contain point (x1,y1,z1) with vector (a1,b1,c1).
        ///// Define line 2 to contain point (x2,y2,z2) with vector (a2,b2,c2).
        ///// 
        ///// We can write these parametric equations for the lines.
        ///// 
        /////       Line1                         Line2
        /////       -----                         -----
        /////   x = x1 + a1 * t1              x = x2 + a2 * t2
        /////   y = y1 + b1 * t1              y = y2 + b2 * t2
        /////   z = z1 + c1 * t1              z = z2 + c2 * t2
        ///// 
        ///// If we set the two x values equal, and the two y values equal we get
        ///// these two equations.
        ///// 
        /////   x1 + a1 * t1 = x2 + a2 * t2
        /////   y1 + b1 * t1 = y2 + b2 * t2
        ///// 
        ///// You can solve these equations for t1 and t2. Then put those values
        ///// back into the parametric equations to solve for the intersection 
        ///// point.
        ///// 
        ///// If you have done the arithmetic correctly, you only need to use one of
        ///// the equations for each of x and y. You should check both equations for
        ///// z to make sure they give the same result. If they give different
        ///// results then the lines are skew.
        ///// 
        ///// 
        ///// 
        ///// 
        ///// Now try Dr. George's method. The lines are
        ///// 
        /////      Line1                    Line2
        /////      -----                    -----
        /////   x = 1 + 2t1              x = 0 + 5t2
        /////   y = 0 + 3t1              y = 5 + 1t2
        /////   z = 0 + 1t1              z = 5 - 3t2
        ///// 
        ///// Setting the x's and y's equal, we have to solve
        ///// 
        /////   1 + 2t1 = 0 + 5t2
        /////   0 + 3t1 = 5 + 1t2
        ///// 
        ///// This reduces to
        ///// 
        /////   2t1 - 5t2 = -1
        /////   3t1 - 1t2 = 5
        ///// 
        ///// We can multiply the first equation by -1 and the second equation by
        ///// 5 and add:
        ///// 
        /////   -2t1 + 5t2 =  1
        /////   15t1 - 5t2 = 25
        /////   ---------------
        /////   13t1       = 26
        ///// 
        /////   t1 = 2
        ///// 
        ///// Plugging that into the second of our pair, we get
        ///// 
        /////   3*2 - t2 = 5
        ///// 
        ///// which gives
        ///// 
        /////   t2 = 1
        ///// 
        ///// Plugging those into all six equations for x, y, and z, we get
        ///// 
        /////   x = 1 + 2*2 = 5     x = 0 + 5*1 = 5
        /////   y = 0 + 3*2 = 6     y = 5 + 1*1 = 6
        /////   z = 0 + 1*2 = 2     z = 5 - 3*1 = 2
        ///// 
        ///// So this is indeed the intersection of the lines.
        ///// </remarks>
        //private static Point3D? GetIntersection_Line_Line(Point3D pointOnLine1, Vector3D lineDirection1, Point3D pointOnLine2, Vector3D lineDirection2)
        //{
        //    throw new ApplicationException("finish this");
        //}

        #endregion

        /// <summary>
        /// This gets the line of intersection between two planes (returns false if they are parallel)
        /// </summary>
        /// <remarks>
        /// Got this here:
        /// http://forums.create.msdn.com/forums/t/40119.aspx
        /// </remarks>
        public static bool GetIntersection_Plane_Plane(out Point3D point, out Vector3D direction, ITriangle plane1, ITriangle plane2)
        {
            Vector3D normal1 = plane1.NormalUnit;
            Vector3D normal2 = plane2.NormalUnit;

            // Find a point that satisfies both plane equations
            double distance1 = -plane1.PlaneDistance;		// Math3D.PlaneDistance uses "normal + d = 0", but the equation below uses "normal = d", so the distances need to be negated
            double distance2 = -plane2.PlaneDistance;

            double offDiagonal = Vector3D.DotProduct(normal1, normal2);
            if (Math1D.IsNearValue(Math.Abs(offDiagonal), 1d))
            {
                // The planes are parallel
                point = new Point3D();
                direction = new Vector3D();
                return false;
            }

            double det = 1d - offDiagonal * offDiagonal;

            double a = (distance1 - distance2 * offDiagonal) / det;
            double b = (distance2 - distance1 * offDiagonal) / det;
            point = (a * normal1 + b * normal2).ToPoint();

            // The line is perpendicular to both normals
            direction = Vector3D.CrossProduct(normal1, normal2);

            return true;
        }

        public static Tuple<Point3D, Point3D> GetIntersection_Plane_Triangle(ITriangle plane, ITriangle triangle)
        {
            // Get the line of intersection of the two planes
            Point3D pointOnLine;
            Vector3D lineDirection;
            if (!GetIntersection_Plane_Plane(out pointOnLine, out lineDirection, plane, triangle))
            {
                return null;
            }

            // Cap to the triangle
            return GetIntersection_Line_Triangle_sameplane(pointOnLine, lineDirection, triangle);
        }

        public static Tuple<Point3D, Point3D> GetIntersection_Triangle_Triangle(ITriangle triangle1, ITriangle triangle2)
        {
            // Get the line of intersection of the two planes
            Point3D pointOnLine;
            Vector3D lineDirection;
            if (!GetIntersection_Plane_Plane(out pointOnLine, out lineDirection, triangle1, triangle2))
            {
                return null;
            }

            // Cap to the triangles
            Tuple<Point3D, Point3D> segment1 = GetIntersection_Line_Triangle_sameplane(pointOnLine, lineDirection, triangle1);
            if (segment1 == null)
            {
                return null;
            }

            Tuple<Point3D, Point3D> segment2 = GetIntersection_Line_Triangle_sameplane(pointOnLine, lineDirection, triangle2);
            if (segment2 == null)
            {
                return null;
            }

            // Cap the line segments
            Point3D? point1_A = GetIntersection_LineSegment_Point_colinear(segment1.Item1, segment1.Item2, segment2.Item1);
            Point3D? point1_B = GetIntersection_LineSegment_Point_colinear(segment1.Item1, segment1.Item2, segment2.Item2);

            Point3D? point2_A = GetIntersection_LineSegment_Point_colinear(segment2.Item1, segment2.Item2, segment1.Item1);
            Point3D? point2_B = GetIntersection_LineSegment_Point_colinear(segment2.Item1, segment2.Item2, segment1.Item2);

            List<Point3D> retVal = new List<Point3D>();

            AddIfUnique(retVal, point1_A);
            AddIfUnique(retVal, point1_B);
            AddIfUnique(retVal, point2_A);
            AddIfUnique(retVal, point2_B);

            if (retVal.Count == 0)
            {
                // The triangles aren't touching
                return null;
            }
            else if (retVal.Count == 1)
            {
                // The triangles are touching at a point, just consider that a non touch
                return null;
            }
            else if (retVal.Count == 2)
            {
                return Tuple.Create(retVal[0], retVal[1]);
            }
            else
            {
                throw new ApplicationException(string.Format("Didn't expect more than 2 unique points.  Got {0} unique points", retVal.Count));
            }
        }

        public static Tuple<Point3D, Point3D> GetIntersection_Face_Triangle(Face3D face, ITriangle triangle, double rayLength = 1000)
        {
            TriangleIndexed[] facePoly = face.GetPolygonTriangles(rayLength);

            Tuple<Point3D, Point3D>[] intersections = facePoly.
                Select(o => Math3D.GetIntersection_Triangle_Triangle(o, triangle)).
                Where(o => o != null).
                ToArray();

            if (intersections.Length == 0)
            {
                return null;
            }

            // Turn them into a single segment
            //NOTE: This will fail if face.GetPolygonTriangles returns a non continuous chain of triangles
            Point3D end1 = intersections[0].Item1;
            Point3D end2 = intersections[0].Item2;

            for (int cntr = 1; cntr < intersections.Length; cntr++)
            {
                if (Math3D.IsNearValue(end1, intersections[cntr].Item1))
                {
                    end1 = intersections[cntr].Item2;
                }
                else if (Math3D.IsNearValue(end2, intersections[cntr].Item1))
                {
                    end2 = intersections[cntr].Item2;
                }
                else if (Math3D.IsNearValue(end1, intersections[cntr].Item2))
                {
                    end1 = intersections[cntr].Item1;
                }
                else if (Math3D.IsNearValue(end2, intersections[cntr].Item2))
                {
                    end2 = intersections[cntr].Item1;
                }
            }

            return Tuple.Create(end1, end2);
        }

        /// <summary>
        /// This returns the distance between a plane and the origin
        /// WARNING: Make sure you actually want this instead of this.DistanceFromPlane
        /// NOTE: Normal must be a unit vector
        /// </summary>
        public static double GetPlaneOriginDistance(Vector3D normalUnit, Point3D pointOnPlane)
        {
            double distance = 0;	// This variable holds the distance from the plane to the origin

            // Use the plane equation to find the distance (Ax + By + Cz + D = 0)  We want to find D.
            // So, we come up with D = -(Ax + By + Cz)
            // Basically, the negated dot product of the normal of the plane and the point. (More about the dot product in another tutorial)
            distance = -((normalUnit.X * pointOnPlane.X) + (normalUnit.Y * pointOnPlane.Y) + (normalUnit.Z * pointOnPlane.Z));

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
        public static bool IsIntersecting_Plane_Line(Vector3D[] polygon, Vector3D[] line, out Vector3D normal, out double originDistance)
        {
            if (line.Length != 2) throw new ArgumentException("A line vector can only be 2 verticies.", "vLine");

            double distance1 = 0, distance2 = 0;						// The distances from the 2 points of the line from the plane

            normal = GetTriangleNormalUnit(polygon);							// We need to get the normal of our plane to go any further

            // Let's find the distance our plane is from the origin.  We can find this value
            // from the normal to the plane (polygon) and any point that lies on that plane (Any vertice)
            originDistance = GetPlaneOriginDistance(normal, polygon[0].ToPoint());

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
        public static Point3D? GetIntersection_Plane_Line(ITriangle plane, Point3D pointOnLine, Vector3D lineDir)
        {
            Vector3D? retVal = GetIntersection_Plane_Line(plane.NormalUnit, new Vector3D[] { pointOnLine.ToVector(), (pointOnLine + lineDir).ToVector() }, plane.PlaneDistance, EdgeType.Line);
            if (retVal == null)
            {
                return null;
            }
            else
            {
                return retVal.Value.ToPoint();
            }
        }
        public static Point3D? GetIntersection_Plane_Ray(ITriangle plane, Point3D rayStart, Vector3D rayDir)
        {
            Vector3D? retVal = GetIntersection_Plane_Line(plane.NormalUnit, new Vector3D[] { rayStart.ToVector(), (rayStart + rayDir).ToVector() }, plane.PlaneDistance, EdgeType.Ray);
            if (retVal == null)
            {
                return null;
            }
            else
            {
                return retVal.Value.ToPoint();
            }
        }
        public static Point3D? GetIntersection_Plane_LineSegment(ITriangle plane, Point3D lineStart, Point3D lineStop)
        {
            Vector3D? retVal = GetIntersection_Plane_Line(plane.NormalUnit, new Vector3D[] { lineStart.ToVector(), lineStop.ToVector() }, plane.PlaneDistance, EdgeType.Segment);
            if (retVal == null)
            {
                return null;
            }
            else
            {
                return retVal.Value.ToPoint();
            }
        }
        private static Vector3D? GetIntersection_Plane_Line(Vector3D normal, Vector3D[] line, double originDistance, EdgeType edgeType)
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
                return null;		// line is parallel to plane

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

            switch (edgeType)
            {
                case EdgeType.Ray:
                    if (dist < 0d)
                    {
                        // This is outside of the ray
                        return null;
                    }
                    break;

                case EdgeType.Segment:
                    if (dist < 0d || dist > 1d)
                    {
                        // This is outside of the line segment
                        return null;
                    }
                    break;
            }

            // Now, like we said above, we times the dist by the vector, then add our arbitrary point.
            // This essentially moves the point along the vector to a certain distance.  This now gives
            // us the intersection point.  Yay!

            result.X = line[0].X + (lineDir.X * dist);
            result.Y = line[0].Y + (lineDir.Y * dist);
            result.Z = line[0].Z + (lineDir.Z * dist);

            return result;								// Return the intersection point
        }

        public static Point3D? GetIntersection_Triangle_Line(ITriangle triangle, Point3D pointOnLine, Vector3D lineDir)
        {
            // Plane
            Point3D? retVal = GetIntersection_Plane_Line(triangle, pointOnLine, lineDir);
            if (retVal == null)
            {
                return null;
            }

            // Constrain to triangle
            Vector bary = ToBarycentric(triangle, retVal.Value);
            if (bary.X < 0 || bary.Y < 0 || bary.X + bary.Y > 1)
            {
                return null;
            }

            // The return point is inside the triangle
            return retVal.Value;
        }
        public static Point3D? GetIntersection_Triangle_Ray(ITriangle triangle, Point3D rayStart, Vector3D rayDir)
        {
            // Plane
            Point3D? retVal = GetIntersection_Plane_Ray(triangle, rayStart, rayDir);
            if (retVal == null)
            {
                return null;
            }

            // Constrain to triangle
            Vector bary = ToBarycentric(triangle, retVal.Value);
            if (bary.X < 0 || bary.Y < 0 || bary.X + bary.Y > 1)
            {
                return null;
            }

            // The return point is inside the triangle
            return retVal.Value;
        }
        public static Point3D? GetIntersection_Triangle_LineSegment(ITriangle triangle, Point3D lineStart, Point3D lineStop)
        {
            // Plane
            Point3D? retVal = GetIntersection_Plane_LineSegment(triangle, lineStart, lineStop);
            if (retVal == null)
            {
                return null;
            }

            // Constrain to triangle
            Vector bary = ToBarycentric(triangle, retVal.Value);
            if (bary.X < 0 || bary.Y < 0 || bary.X + bary.Y > 1)
            {
                return null;
            }

            // The return point is inside the triangle
            return retVal.Value;
        }

        public static Point3D[] GetIntersection_Hull_Plane(ITriangleIndexed[] convexHull, ITriangle plane)
        {
            return HullTriangleIntersect.GetIntersection_Hull_Plane(convexHull, plane);
        }
        public static Point3D[] GetIntersection_Hull_Triangle(ITriangleIndexed[] convexHull, ITriangle triangle)
        {
            return HullTriangleIntersect.GetIntersection_Hull_Triangle(convexHull, triangle);
        }

        /// <summary>
        /// This returns hits ordered by distance from the rayStart
        /// </summary>
        /// <returns>
        /// Item1=Point of intersection between the ray and a triangle
        /// Item2=The triangle
        /// Item3=Distance from rayStart
        /// </returns>
        public static Tuple<Point3D, ITriangle, double>[] GetIntersection_Hull_Ray(ITriangle[] triangles, Point3D rayStart, Vector3D rayDir)
        {
            // Make a delagate to intersect the ray with a triangle
            var getHit = new Func<ITriangle, Point3D, Vector3D, Tuple<Point3D, ITriangle, double>>((t, p, v) =>
            {
                Point3D? hit = GetIntersection_Triangle_Ray(t, p, v);

                if (hit == null)
                {
                    return (Tuple<Point3D, ITriangle, double>)null;
                }

                double distance = (hit.Value - p).Length;

                return Tuple.Create(hit.Value, t, distance);
            });

            // Test all triangles
            if (triangles.Length > 100)
            {
                return triangles.
                    AsParallel().
                    Select(o => getHit(o, rayStart, rayDir)).
                    Where(o => o != null).
                    OrderBy(o => o.Item3).
                    ToArray();
            }
            else
            {
                return triangles.
                    Select(o => getHit(o, rayStart, rayDir)).
                    Where(o => o != null).
                    OrderBy(o => o.Item3).
                    ToArray();
            }
        }

        //public static Point3D[][] GetIntersection_Mesh_Plane_OLD(ITriangleIndexed[] mesh, ITriangle plane)
        //{
        //    return HullTriangleIntersect.GetIntersection_Mesh_Plane_OLD(mesh, plane);
        //}
        public static ContourPolygon[] GetIntersection_Mesh_Plane(ITriangleIndexed[] mesh, ITriangle plane)
        {
            return HullTriangleIntersect.GetIntersection_Mesh_Plane(mesh, plane);
        }

        /// <summary>
        /// This intersects a convex hull with a voronoi.  Returns convex hulls
        /// NOTE: This works in most cases, but may return hulls that are shaved off more than they should be
        /// </summary>
        /// <returns>
        /// Item1=Control point index
        /// Item2=Convex hull
        /// </returns>
        public static Tuple<int, ITriangleIndexed[]>[] GetIntersection_Hull_Voronoi_full(ITriangleIndexed[] convexHull, VoronoiResult3D voronoi)
        {
            return HullVoronoiIntersect.GetIntersection_Hull_Voronoi_full(convexHull, voronoi);
        }
        /// <summary>
        /// This intersects a convex hull with a voronoi.  Only returns the patches of the hull.  Doesn't return any depth along
        /// the voronoi's faces
        /// </summary>
        /// <returns></returns>
        public static VoronoiHullIntersect_PatchFragment[] GetIntersection_Triangles_Voronoi_surface(ITriangleIndexed[] triangles, VoronoiResult3D voronoi)
        {
            return HullVoronoiIntersect.GetIntersection_Hull_Voronoi_surface(triangles, voronoi);
        }

        /// <summary>
        /// This checks to see if a point is inside the ranges of a polygon
        /// TODO: Figure out why this is giving false positives - I think it's meant for a 2D polygon within 3D.  Not a 3D hull
        /// </summary>
        public static bool IsInside_Polygon2D(Vector3D intersectionPoint, Vector3D[] polygon2D, long verticeCount)
        {
            //const double MATCH_FACTOR = 0.9999;		// Used to cover up the error in floating point
            const double MATCH_FACTOR = 0.999999;		// Used to cover up the error in floating point
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
                Vector3D vA = polygon2D[i] - intersectionPoint;	// Subtract the intersection point from the current vertex
                // Subtract the point from the next vertex
                Vector3D vB = polygon2D[(i + 1) % verticeCount] - intersectionPoint;

                Angle += Math1D.DegreesToRadians(Vector3D.AngleBetween(vA, vB));	// Find the angle between the 2 vectors and add them all up as we go along
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
        public static bool IsIntersecting_Polygon2D_Line(Vector3D[] polygon2D, Vector3D[] line, int verticeCount, out Vector3D normal, out double originDistance, out Vector3D? intersectionPoint)
        {
            intersectionPoint = null;

            // First we check to see if our line intersected the plane.  If this isn't true
            // there is no need to go on, so return false immediately.
            // We pass in address of vNormal and originDistance so we only calculate it once

            // Reference
            if (!IsIntersecting_Plane_Line(polygon2D, line, out normal, out originDistance))
                return false;

            // Now that we have our normal and distance passed back from IntersectedPlane(), 
            // we can use it to calculate the intersection point.  The intersection point
            // is the point that actually is ON the plane.  It is between the line.  We need
            // this point test next, if we are inside the polygon.  To get the I-Point, we
            // give our function the normal of the plan, the points of the line, and the originDistance.

            intersectionPoint = GetIntersection_Plane_Line(normal, line, originDistance, EdgeType.Line);
            if (intersectionPoint == null)
                return false;

            // Now that we have the intersection point, we need to test if it's inside the polygon.
            // To do this, we pass in :
            // (our intersection point, the polygon, and the number of vertices our polygon has)

            if (IsInside_Polygon2D(intersectionPoint.Value, polygon2D, verticeCount))
                return true;							// We collided!	  Return success


            // If we get here, we must have NOT collided

            return false;								// There was no collision, so return false
        }
        /// <summary>
        /// This checks if a line is intersecting a polygon
        /// </summary>
        public static bool IsIntersecting_Polygon2D_Line(Vector3D[] polygon2D, Vector3D[] line)
        {
            Vector3D normal;
            double originDistance;
            Vector3D? intersectionPoint;

            return IsIntersecting_Polygon2D_Line(polygon2D, line, polygon2D.Length, out normal, out originDistance, out intersectionPoint);
        }

        /// <summary>
        /// NOTE: This only works if all of the triangle's normals point outward
        /// </summary>
        public static bool IsInside_ConvexHull(IEnumerable<ITriangle> hull, Point3D point)
        {
            foreach (ITriangle triangle in hull)
            {
                if (Vector3D.DotProduct(triangle.Normal, point - triangle.Point0) > 0d)
                {
                    // This point is outside
                    return false;
                }
            }

            return true;
        }
        /// <summary>
        /// This will work for concave or convex hulls.  The triangle normals don't matter
        /// NOTE: It's up to the caller to do sphere or AABB check before taking the expense of calling this method
        /// </summary>
        /// <param name="asParallel">WARNING: Test both to see which is faster before automatically choosing parallel - it can be slower</param>
        /// <remarks>
        /// Got this here:
        /// http://www.yaldex.com/game-programming/0131020099_ch22lev1sec1.html
        /// </remarks>
        public static bool IsInside_ConcaveHull(IEnumerable<ITriangle> hull, Point3D point, bool asParallel = false)
        {
            //Vector3D rayDirection = new Vector3D(1, 0, 0);        // can't use this, because I was getting flaky results when the ray was shooting through perfectly aligned hulls (ray was intersecting the edges)
            Vector3D rayDirection = GetRandomVector(1);

            int numIntersections = 0;

            if (asParallel)
            {
                numIntersections = hull.
                    AsParallel().
                    Sum(o => (GetIntersection_Triangle_Ray(o, point, rayDirection) != null) ? 1 : 0);
            }
            else
            {
                foreach (ITriangle triangle in hull)        // Avoiding linq for speed reasons
                {
                    if (GetIntersection_Triangle_Ray(triangle, point, rayDirection) != null)
                    {
                        numIntersections++;
                    }
                }
            }

            // If the number of intersections is odd, then the point started inside the hull.
            // If zero, it missed
            // If even, it punched all the way through
            return numIntersections % 2 == 1;
        }

        //WARNING: This returns negative values if below the plane, so if you want distance, take the absolute value
        public static double DistanceFromPlane(ITriangle plane, Point3D point)
        {
            // This code was copied from the other overload to run as fast as possible (instead of converting all the points)

            double originDistance = GetPlaneOriginDistance(plane.NormalUnit, plane.Point0);

            return DistanceFromPlane(plane.NormalUnit, originDistance, point);
        }
        public static double DistanceFromPlane(Vector3D[] polygon, Vector3D point)
        {
            Vector3D normal = GetTriangleNormalUnit(polygon); // We need to get the normal of our plane to go any further

            // Let's find the distance our plane is from the origin.  We can find this value
            // from the normal to the plane (polygon) and any point that lies on that plane (Any vertice)
            double originDistance = GetPlaneOriginDistance(normal, polygon[0].ToPoint());

            // Get the distance from point1 from the plane uMath.Sing: Ax + By + Cz + D = (The distance from the plane)
            return DistanceFromPlane(normal, originDistance, point);
        }
        public static double DistanceFromPlane(Vector3D normalUnit, double originDistance, Vector3D point)
        {
            return ((normalUnit.X * point.X) +					// Ax +
                    (normalUnit.Y * point.Y) +					// Bx +
                    (normalUnit.Z * point.Z)) + originDistance;	// Cz + D
        }
        public static double DistanceFromPlane(Vector3D normalUnit, double originDistance, Point3D point)
        {
            //This overload is copied to save converting from vector to point

            return ((normalUnit.X * point.X) +					// Ax +
                    (normalUnit.Y * point.Y) +					// Bx +
                    (normalUnit.Z * point.Z)) + originDistance;	// Cz + D
        }

        /// <summary>
        /// This splits the vector into a vector along the plane, and orthogonal to the plane
        /// </summary>
        /// <remarks>
        /// The two returned vectors will add up to the original vector passed in
        /// </remarks>
        public static DoubleVector SplitVector(Vector3D vector, ITriangle plane)
        {
            // Get portion along normal: this is up/down
            Vector3D orth = vector.GetProjectedVector(plane.Normal);

            // Subtract that off: this is left/right
            Vector3D along = vector - orth;

            // Exit Function
            return new DoubleVector(along, orth);
        }

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
                vRot.Y = Math1D.RadiansToDegrees(Math.Atan2(/*mRot.M31*/vCols[2].X, /*mRot.M33*/vCols[2].Z));
                vRot.Z = Math1D.RadiansToDegrees(Math.Atan2(/*mRot.M12*/vCols[0].Y, /*mRot.M22*/vCols[1].Y));
            }
            else
            {
                vRot.Y = 0.0;
                vRot.Z = Math1D.RadiansToDegrees(Math.Atan2(-/*mRot.M21*/vCols[1].X, /*mRot.M11*/vCols[0].X));
            }
            vRot.X = Math1D.RadiansToDegrees(vRot.X);
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
            double x = Math1D.DegreesToRadians(xPitchDegrees);
            double y = Math1D.DegreesToRadians(yYawDegrees);
            double z = Math1D.DegreesToRadians(zRollDegrees);

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

        #region Private Methods

        private static void GetRandomPoints_OnHullSprtSizes(out int[] trianglePointers, out double[] triangleSizes, ITriangle[] hull)
        {
            trianglePointers = Enumerable.Range(0, hull.Length).ToArray();
            triangleSizes = new double[hull.Length];

            // Get the size of each triangle
            for (int cntr = 0; cntr < hull.Length; cntr++)
            {
                triangleSizes[cntr] = hull[cntr].NormalLength / 2d;
            }

            // Sort them (I'd sort descending if I could.  That would make the find method easier to read, but the call to reverse
            // is an unnecessary expense)
            Array.Sort(triangleSizes, trianglePointers);

            // Normalize them so the sum is 1.  Note that after this, each item in sizes will be that item's percent of the whole
            double totalSize = triangleSizes.Sum();
            for (int cntr = 0; cntr < triangleSizes.Length; cntr++)
            {
                triangleSizes[cntr] = triangleSizes[cntr] / totalSize;
            }
        }
        private static int GetRandomPoints_OnHullSprtFindTriangle(int[] trianglePointers, double[] triangleSizes, double percent)
        {
            double accumSize = 1d;

            // Find the triangle that is this occupying this percent of the total size (walking backward will cut down on
            // the number of triangles that need to be searched through)
            for (int cntr = triangleSizes.Length - 1; cntr > 0; cntr--)
            {
                if (accumSize - triangleSizes[cntr] < percent)
                {
                    return trianglePointers[cntr];
                }

                accumSize -= triangleSizes[cntr];
            }

            // This should only happen if the requested percent is zero
            return trianglePointers[0];
        }

        /// <summary>
        /// This returns a number between LeftOuter--LeftInner or RightInner--RightOuter
        /// </summary>
        private static double GetRandomValue_Hole(Random rand, double outerLeft, double holeLeft, double holeRight, double outerRight)
        {
            double leftLength = holeLeft - outerLeft;
            double rightLength = outerRight - holeRight;

            if (leftLength.IsNearZero() && rightLength.IsNearZero())
            {
                return rand.NextBool() ? holeLeft : holeRight;
            }
            else if (leftLength < 0 && rightLength < 0)
            {
                throw new ArgumentException("The hole is bigger than the outer");
            }
            else if (leftLength <= 0)
            {
                return rand.NextDouble(holeRight, outerRight);
            }
            else if (rightLength <= 0)
            {
                return rand.NextDouble(outerLeft, holeLeft);
            }

            double randValue = rand.NextDouble(leftLength + rightLength);

            if (randValue < leftLength)
            {
                return outerLeft + randValue;
            }
            else
            {
                return holeRight + (randValue - leftLength);
            }
        }

        /// <summary>
        /// This returns the normal of a polygon (The direction the polygon is facing)
        /// </summary>
        private static Vector3D GetTriangleNormalUnit(Vector3D[] triangle)
        {
            // This is the original, but was returning a left handed normal
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

        private static Tuple<Point3D, Point3D> GetIntersection_Line_Triangle_sameplane(Point3D pointOnLine, Vector3D lineDirection, ITriangle triangle)
        {
            List<Point3D> retval = new List<Point3D>();

            // Cap the line to the triangle
            foreach (TriangleEdge edge in Triangle.Edges)
            {
                Point3D[] resultsLine, resultsLineSegment;
                if (!GetClosestPoints_Line_LineSegment(out resultsLine, out resultsLineSegment, pointOnLine, lineDirection, triangle.GetPoint(edge, true), triangle.GetPoint(edge, false)))
                {
                    continue;
                }

                if (resultsLine.Length != resultsLineSegment.Length)
                {
                    throw new ApplicationException("The line vs line segments have a different number of matches");
                }

                // This method is dealing with lines that are in the same plane, so if the result point for the plane/plane line is different
                // than the triangle edge, then throw out this match
                bool allMatched = true;
                for (int cntr = 0; cntr < resultsLine.Length; cntr++)
                {
                    if (!IsNearValue(resultsLine[cntr], resultsLineSegment[cntr]))
                    {
                        allMatched = false;
                        break;
                    }
                }

                if (!allMatched)
                {
                    continue;
                }

                retval.AddRange(resultsLineSegment);

                if (retval.Count >= 2)
                {
                    // No need to keep looking, there will only be up to two points of intersection
                    break;
                }
            }

            // Exit Function
            if (retval.Count == 0)
            {
                return null;
            }
            else if (retval.Count == 1)
            {
                // Only touched one vertex
                return Tuple.Create(retval[0], retval[0]);
            }
            else if (retval.Count == 2)
            {
                // Standard result
                return Tuple.Create(retval[0], retval[1]);
            }
            else
            {
                throw new ApplicationException("Found more than two intersection points");
            }
        }

        private static Point3D? GetIntersection_LineSegment_Point_colinear(Point3D segmentStart, Point3D segmentStop, Point3D point)
        {
            if (IsNearValue(segmentStart, point) || IsNearValue(segmentStop, point))
            {
                // It's touching one of the endpoints
                return point;
            }

            // Make sure the point isn't beyond the line segment
            Vector3D segmentDir = segmentStop - segmentStart;
            Vector3D testDir = point - segmentStart;

            if (!Math1D.IsNearValue(Vector3D.DotProduct(segmentDir.ToUnit(), testDir.ToUnit()), 1d))
            {
                // It's the other direction (beyond segment start)
                return null;
            }

            if (testDir.LengthSquared > segmentDir.LengthSquared)
            {
                // It's beyond segment stop
                return null;
            }

            // It's somewhere inside the segment
            return point;
        }

        private static void AddIfUnique(List<Point3D> list, Point3D? test)
        {
            if (test == null)
            {
                return;
            }

            if (list.Any(o => IsNearValue(o, test.Value)))
            {
                return;
            }

            list.Add(test.Value);
        }

        //****************************************************************************
        //
        //  Purpose:
        //
        //    TETRAHEDRON_CIRCUMSPHERE computes the circumsphere of a tetrahedron.
        //
        //  Discussion:
        //
        //    The circumsphere, or circumscribed sphere, of a tetrahedron is the 
        //    sphere that passes through the four vertices.  The circumsphere is not
        //    necessarily the smallest sphere that contains the tetrahedron.
        //
        //    Surprisingly, the diameter of the sphere can be found by solving
        //    a 3 by 3 linear system.  This is because the vectors P2 - P1,
        //    P3 - P1 and P4 - P1 are secants of the sphere, and each forms a
        //    right triangle with the diameter through P1.  Hence, the dot product of
        //    P2 - P1 with that diameter is equal to the square of the length
        //    of P2 - P1, and similarly for P3 - P1 and P4 - P1.  This determines
        //    the diameter vector originating at P1, and hence the radius and
        //    center.
        //
        //  Licensing:
        //
        //    This code is distributed under the GNU LGPL license. 
        //
        //  Modified:
        //
        //    10 August 2005
        //
        //  Author:
        //
        //    John Burkardt
        //    http://people.sc.fsu.edu/~jburkardt/cpp_src/tetrahedron_properties/tetrahedron_properties.html
        //
        //  Reference:
        //
        //    Adrian Bowyer, John Woodwark,
        //    A Programmer's Geometry,
        //    Butterworths, 1983.
        //
        //  Parameters:
        //
        //    Input, double TETRA[3*4], the vertices of the tetrahedron.
        //
        //    Output, double &R, PC[3], the coordinates of the center of the
        //    circumscribed sphere, and its radius.  If the linear system is
        //    singular, then R = -1, PC[] = 0.
        //void tetrahedron_circumsphere ( double tetra[3*4], double &r, double pc[3] )
        private static void tetrahedron_circumsphere(double[] tetra, out double r, out double[] pc)
        {
            pc = new double[3];

            double[] a = new double[3 * 4]; //double a[3*4];
            int info;
            //
            //  Set up the linear system.
            //
            a[0 + 0 * 3] = tetra[0 + 1 * 3] - tetra[0 + 0 * 3];
            a[0 + 1 * 3] = tetra[1 + 1 * 3] - tetra[1 + 0 * 3];
            a[0 + 2 * 3] = tetra[2 + 1 * 3] - tetra[2 + 0 * 3];
            a[0 + 3 * 3] = Math.Pow(tetra[0 + 1 * 3] - tetra[0 + 0 * 3], 2)
                     + Math.Pow(tetra[1 + 1 * 3] - tetra[1 + 0 * 3], 2)
                     + Math.Pow(tetra[2 + 1 * 3] - tetra[2 + 0 * 3], 2);

            a[1 + 0 * 3] = tetra[0 + 2 * 3] - tetra[0 + 0 * 3];
            a[1 + 1 * 3] = tetra[1 + 2 * 3] - tetra[1 + 0 * 3];
            a[1 + 2 * 3] = tetra[2 + 2 * 3] - tetra[2 + 0 * 3];
            a[1 + 3 * 3] = Math.Pow(tetra[0 + 2 * 3] - tetra[0 + 0 * 3], 2)
                     + Math.Pow(tetra[1 + 2 * 3] - tetra[1 + 0 * 3], 2)
                     + Math.Pow(tetra[2 + 2 * 3] - tetra[2 + 0 * 3], 2);

            a[2 + 0 * 3] = tetra[0 + 3 * 3] - tetra[0 + 0 * 3];
            a[2 + 1 * 3] = tetra[1 + 3 * 3] - tetra[1 + 0 * 3];
            a[2 + 2 * 3] = tetra[2 + 3 * 3] - tetra[2 + 0 * 3];
            a[2 + 3 * 3] = Math.Pow(tetra[0 + 3 * 3] - tetra[0 + 0 * 3], 2)
                     + Math.Pow(tetra[1 + 3 * 3] - tetra[1 + 0 * 3], 2)
                     + Math.Pow(tetra[2 + 3 * 3] - tetra[2 + 0 * 3], 2);
            //
            //  Solve the linear system.
            //
            info = r8mat_solve(3, 1, a);
            //
            //  If the system was singular, return a consolation prize.
            //
            if (info != 0)
            {
                r = -1.0;
                r8vec_zero(3, pc);
                return;
            }
            //
            //  Compute the radius and center.
            //
            r = 0.5 * Math.Sqrt
              (a[0 + 3 * 3] * a[0 + 3 * 3]
              + a[1 + 3 * 3] * a[1 + 3 * 3]
              + a[2 + 3 * 3] * a[2 + 3 * 3]);

            pc[0] = tetra[0 + 0 * 3] + 0.5 * a[0 + 3 * 3];
            pc[1] = tetra[1 + 0 * 3] + 0.5 * a[1 + 3 * 3];
            pc[2] = tetra[2 + 0 * 3] + 0.5 * a[2 + 3 * 3];

            return;
        }
        //****************************************************************************
        //
        //  Purpose:
        //
        //    R8MAT_SOLVE uses Gauss-Jordan elimination to solve an N by N linear system.
        //
        //  Discussion: 							    
        //
        //    An R8MAT is a doubly dimensioned array of R8 values,  stored as a vector 
        //    in column-major order.
        //
        //    Entry A(I,J) is stored as A[I+J*N]
        //
        //  Licensing:
        //
        //    This code is distributed under the GNU LGPL license. 
        //
        //  Modified:
        //
        //    29 August 2003
        //
        //  Author:
        //
        //    John Burkardt
        //    http://people.sc.fsu.edu/~jburkardt/cpp_src/tetrahedron_properties/tetrahedron_properties.html
        //
        //  Parameters:
        //
        //    Input, int N, the order of the matrix.
        //
        //    Input, int RHS_NUM, the number of right hand sides.  RHS_NUM
        //    must be at least 0.
        //
        //    Input/output, double A[N*(N+RHS_NUM)], contains in rows and columns 1
        //    to N the coefficient matrix, and in columns N+1 through
        //    N+RHS_NUM, the right hand sides.  On output, the coefficient matrix
        //    area has been destroyed, while the right hand sides have
        //    been overwritten with the corresponding solutions.
        //
        //    Output, int R8MAT_SOLVE, singularity flag.
        //    0, the matrix was not singular, the solutions were computed;
        //    J, factorization failed on step J, and the solutions could not
        //    be computed.
        private static int r8mat_solve(int n, int rhs_num, double[] a)
        {
            double apivot;
            double factor;
            int i;
            int ipivot;
            int j;
            int k;
            double temp;

            for (j = 0; j < n; j++)
            {
                //
                //  Choose a pivot row.
                //
                ipivot = j;
                apivot = a[j + j * n];

                for (i = j; i < n; i++)
                {
                    if (Math.Abs(apivot) < Math.Abs(a[i + j * n]))
                    {
                        apivot = a[i + j * n];
                        ipivot = i;
                    }
                }

                if (apivot == 0.0)
                {
                    return j;
                }
                //
                //  Interchange.
                //
                for (i = 0; i < n + rhs_num; i++)
                {
                    temp = a[ipivot + i * n];
                    a[ipivot + i * n] = a[j + i * n];
                    a[j + i * n] = temp;
                }
                //
                //  A(J,J) becomes 1.
                //
                a[j + j * n] = 1.0;
                for (k = j; k < n + rhs_num; k++)
                {
                    a[j + k * n] = a[j + k * n] / apivot;
                }
                //
                //  A(I,J) becomes 0.
                //
                for (i = 0; i < n; i++)
                {
                    if (i != j)
                    {
                        factor = a[i + j * n];
                        a[i + j * n] = 0.0;
                        for (k = j; k < n + rhs_num; k++)
                        {
                            a[i + k * n] = a[i + k * n] - factor * a[j + k * n];
                        }
                    }
                }
            }

            return 0;
        }
        //****************************************************************************
        //
        //  Purpose:
        //
        //    R8VEC_ZERO zeroes an R8VEC.
        //
        //  Discussion:
        //
        //    An R8VEC is a vector of R8's.
        //
        //  Licensing:
        //
        //    This code is distributed under the GNU LGPL license. 
        //
        //  Modified:
        //
        //    03 July 2005
        //
        //  Author:
        //
        //    John Burkardt
        //    http://people.sc.fsu.edu/~jburkardt/cpp_src/tetrahedron_properties/tetrahedron_properties.html
        //
        //  Parameters:
        //
        //    Input, int N, the number of entries in the vector.
        //
        //    Output, double A[N], a vector of zeroes.
        //
        private static void r8vec_zero(int n, double[] a)
        {
            int i;

            for (i = 0; i < n; i++)
            {
                a[i] = 0.0;
            }
            return;
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
            // This is the line that the planes intersect along
            public Point3D PointOnLine;
            public Vector3D LineDirection;

            // This is a line from the circle's center to the intersect line
            public Point3D NearestToCenter;
            public Vector3D CenterToNearest;
            public double CenterToNearestLength;

            // This is whether NearestToCenter is within the circle or outside of it
            public bool IsInsideCircle;
        }

        private static bool GetClosestPointsBetweenLineCircle(out Point3D[] circlePoints, out Point3D[] linePoints, CircleLineArgs args)
        {
            #region Scenarios

            // Line intersects plane inside circle:
            //		Calculate intersect point to circle rim
            //		Calculate two perps to circle rim
            //
            // Take the closest of those three points

            // Line intersects plane outside circle, but passes over circle:
            //		Calculate intersect point to circle rim
            //		Calculate two perps to circle rim
            //
            // Take the closest of those three points

            // Line is parallel to the plane, passes over circle
            //		Calculate two perps to circle rim

            // Line is parallel to the plane, does not pass over circle
            //		Get closest point between center and line, project onto plane, find point along the circle

            // Line does not pass over the circle
            //		Calculate intersect point to circle rim
            //		Get closest point between plane intersect line and circle center
            //
            // Take the closest of those two points

            // Line is perpendicular to the plane
            //		Calculate intersect point to circle rim

            #endregion

            // Detect perpendicular
            double dot = Vector3D.DotProduct(args.CirclePlane.NormalUnit, args.LineDirection.ToUnit());
            if (Math1D.IsNearValue(Math.Abs(dot), 1d))
            {
                return GetClosestPointsBetweenLineCircleSprtPerpendicular(out circlePoints, out linePoints, args);
            }

            // Project the line onto the circle's plane
            CirclePlaneIntersectProps planeIntersect = GetClosestPointsBetweenLineCircleSprtPlaneIntersect(args);

            // There's less to do if the line is parallel
            if (Math1D.IsNearZero(dot))
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
            Point3D planeIntersect = GetClosestPoint_Line_Point(args.PointOnLine, args.LineDirection, args.CircleCenter);

            if (IsNearValue(planeIntersect, args.CircleCenter))
            {
                // This is a perpendicular ray shot straight through the center.  All circle points are closest to the line
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
                linePoints = new Point3D[] { GetClosestPoint_Line_Point(args.PointOnLine, args.LineDirection, circlePoints[0]) };
            }
        }
        private static void GetClosestPointsBetweenLineCircleSprtOther(out Point3D[] circlePoints, out Point3D[] linePoints, CircleLineArgs args, CirclePlaneIntersectProps planeIntersect)
        {
            // See where the line intersects the circle's plane
            Point3D? lineIntersect = GetIntersection_Plane_Line(args.CirclePlane, args.PointOnLine, args.LineDirection);
            if (lineIntersect == null)		// this should never happen, since an IsParallel check was already done (but one might be stricter than the other)
            {
                GetClosestPointsBetweenLineCircleSprtParallel(out circlePoints, out linePoints, args, planeIntersect);
                return;
            }

            if (planeIntersect.IsInsideCircle)
            {
                #region Line is over circle

                // Line intersects plane inside circle:
                //		Calculate intersect point to circle rim
                //		Calculate two perps to circle rim
                //
                // Take the closest of those three points

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

                // Line does not pass over the circle
                //		Calculate intersect point to circle rim
                //		Get closest point between plane intersect line and circle center
                //
                // Take the closest of those two points

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

            // Find the shortest distance across the pairs
            if (circlePoints1 != null)
            {
                for (int cntr = 0; cntr < circlePoints1.Length; cntr++)
                {
                    double localDistance = (linePoints1[cntr] - circlePoints1[cntr]).Length;

                    if (Math1D.IsNearValue(localDistance, distance))
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

                    if (Math1D.IsNearValue(localDistance, distance))
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

            // Return the result
            circlePoints = circlePointList.ToArray();
            linePoints = linePointList.ToArray();
        }
        private static CirclePlaneIntersectProps GetClosestPointsBetweenLineCircleSprtPlaneIntersect(CircleLineArgs args)
        {
            CirclePlaneIntersectProps retVal;

            // The slice plane runs perpendicular to the circle's plane
            Triangle slicePlane = new Triangle(args.PointOnLine, args.PointOnLine + args.LineDirection, args.PointOnLine + args.CirclePlane.Normal);

            // Use that slice plane to project the line onto the circle's plane
            if (!GetIntersection_Plane_Plane(out retVal.PointOnLine, out retVal.LineDirection, args.CirclePlane, slicePlane))
            {
                throw new ApplicationException("The slice plane should never be parallel to the circle's plane");		// it was defined as perpendicular
            }

            // Find the closest point between the circle's center to this intersection line
            retVal.NearestToCenter = GetClosestPoint_Line_Point(retVal.PointOnLine, retVal.LineDirection, args.CircleCenter);
            retVal.CenterToNearest = retVal.NearestToCenter - args.CircleCenter;
            retVal.CenterToNearestLength = retVal.CenterToNearest.Length;

            retVal.IsInsideCircle = retVal.CenterToNearestLength <= args.CircleRadius;

            // Exit Function
            return retVal;
        }
        private static void GetClosestPointsBetweenLineCircleSprtCenterToPlaneIntersect(out Point3D[] circlePoints, out Point3D[] linePoints, CircleLineArgs args, Point3D planeIntersect)
        {
            Vector3D centerToIntersect = planeIntersect - args.CircleCenter;
            double centerToIntersectLength = centerToIntersect.Length;

            if (Math1D.IsNearZero(centerToIntersectLength))
            {
                circlePoints = null;
                linePoints = null;
            }
            else
            {
                circlePoints = new Point3D[] { args.CircleCenter + (centerToIntersect * (args.CircleRadius / centerToIntersectLength)) };
                linePoints = new Point3D[] { GetClosestPoint_Line_Point(args.PointOnLine, args.LineDirection, circlePoints[0]) };
            }
        }
        private static void GetClosestPointsBetweenLineCircleSprtInsidePerps(out Point3D[] circlePoints, out Point3D[] linePoints, CircleLineArgs args, CirclePlaneIntersectProps planeIntersect)
        {
            // See if the line passes through the center
            if (Math1D.IsNearZero(planeIntersect.CenterToNearestLength))
            {
                //Vector3D lineDirUnit = args.LineDirection.ToUnit();
                Vector3D lineDirUnit = planeIntersect.LineDirection.ToUnit();

                // The line passes over the circle's center, so the nearest points will shoot straight from the center in the direction of the line
                circlePoints = new Point3D[2];
                circlePoints[0] = args.CircleCenter + (lineDirUnit * args.CircleRadius);
                circlePoints[1] = args.CircleCenter - (lineDirUnit * args.CircleRadius);
            }
            else
            {
                // The two points are perpendicular to this line.  Use A^2 + B^2 = C^2 to get the length of the perpendiculars
                double perpLength = Math.Sqrt((args.CircleRadius * args.CircleRadius) - (planeIntersect.CenterToNearestLength * planeIntersect.CenterToNearestLength));
                Vector3D perpDirection = Vector3D.CrossProduct(planeIntersect.CenterToNearest, args.CirclePlane.Normal).ToUnit();

                circlePoints = new Point3D[2];
                circlePoints[0] = planeIntersect.NearestToCenter + (perpDirection * perpLength);
                circlePoints[1] = planeIntersect.NearestToCenter - (perpDirection * perpLength);
            }

            // Get corresponding points along the line
            linePoints = new Point3D[2];
            linePoints[0] = GetClosestPoint_Line_Point(args.PointOnLine, args.LineDirection, circlePoints[0]);
            linePoints[1] = GetClosestPoint_Line_Point(args.PointOnLine, args.LineDirection, circlePoints[1]);
        }

        /// <summary>
        /// This one returns the one that is closest to pointOnLine
        /// </summary>
        private static void GetClosestPointsBetweenLineCircleSprtClosest_RayOrigin(ref Point3D[] circlePoints, ref Point3D[] linePoints, Point3D rayOrigin)
        {
            #region Find closest point

            // There is more than one point, and they want a single point
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

            // Return only the closest point
            circlePoints = new Point3D[] { circlePoints[minIndex] };
            linePoints = new Point3D[] { linePoints[minIndex] };
        }
        /// <summary>
        /// This one returns the one that is closest between the two hits
        /// </summary>
        private static void GetClosestPointsBetweenLineCircleSprtClosest_CircleLine(ref Point3D[] circlePoints, ref Point3D[] linePoints, Point3D rayOrigin)
        {
            #region Find closest point

            // There is more than one point, and they want a single point
            double minDistance = double.MaxValue;
            double minOriginDistance = double.MaxValue;		// use this as a secondary sort (really important if the collision shape is a cylinder or sphere.  The line will have two exact matches, so return the one closest to the ray cast origin)
            int minIndex = -1;

            for (int cntr = 0; cntr < circlePoints.Length; cntr++)
            {
                double distance = (linePoints[cntr] - circlePoints[cntr]).LengthSquared;
                double originDistance = (linePoints[cntr] - rayOrigin).LengthSquared;

                bool isEqualDistance = Math1D.IsNearValue(distance, minDistance);

                //NOTE: I can't just say distance < minDistance, because for a sphere, it kept jittering between the near
                // side and far side, so it has to be closer by a decisive amount
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

            // Return only the closest point
            circlePoints = new Point3D[] { circlePoints[minIndex] };
            linePoints = new Point3D[] { linePoints[minIndex] };
        }

        #endregion
        #region Cylinder/Line Intersect Helpers

        private static CirclePlaneIntersectProps GetClosestPointsBetweenLineCylinderSprtPlaneIntersect(CircleLineArgs args, Point3D nearestLinePoint, Vector3D nearestLine, double nearestLineDistance)
        {
            //NOTE: This is nearly identical to GetClosestPointsBetweenLineCircleSprtPlaneIntersect, but since some stuff was already done,
            // it's more just filling out the struct

            CirclePlaneIntersectProps retVal;

            // The slice plane runs perpendicular to the circle's plane
            Triangle slicePlane = new Triangle(args.PointOnLine, args.PointOnLine + args.LineDirection, args.PointOnLine + args.CirclePlane.Normal);

            // Use that slice plane to project the line onto the circle's plane
            if (!GetIntersection_Plane_Plane(out retVal.PointOnLine, out retVal.LineDirection, args.CirclePlane, slicePlane))
            {
                throw new ApplicationException("The slice plane should never be parallel to the circle's plane");		// it was defined as perpendicular
            }

            // Store what was passed in (the circle/line intersect waits till now to do this, but for cylinder, this was done previously)
            retVal.NearestToCenter = nearestLinePoint;
            retVal.CenterToNearest = nearestLine;
            retVal.CenterToNearestLength = nearestLineDistance;

            retVal.IsInsideCircle = true;		// this method is only called when true

            // Exit Function
            return retVal;
        }

        private static void GetClosestPointsBetweenLineCylinderSprtFinish(out Point3D[] cylinderPoints, out Point3D[] linePoints, CircleLineArgs args, CirclePlaneIntersectProps intersectArgs)
        {
            // Get the circle intersects
            Point3D[] circlePoints2D, linePoints2D;
            GetClosestPointsBetweenLineCircleSprtInsidePerps(out circlePoints2D, out linePoints2D, args, intersectArgs);

            // Project the circle hits onto the original line
            Point3D? p1, p2, p3, p4;
            GetClosestPoints_Line_Line(out p1, out p2, args.PointOnLine, args.LineDirection, circlePoints2D[0], args.CirclePlane.Normal);
            GetClosestPoints_Line_Line(out p3, out p4, args.PointOnLine, args.LineDirection, circlePoints2D[1], args.CirclePlane.Normal);

            // p1 and p2 are the same, p3 and p4 are the same
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
    }

    #region Class: ContourPolygon

    public class ContourPolygon
    {
        #region Constructor

        public ContourPolygon(Point3D[] polygon3D, Point3D[][] holes3D, Point[] polygon2D, Point[][] holes2D, ContourTriangleIntersect[] intersectedTriangles, ContourTriangleIntersect[][] intersectedTrianglesHoles, ITriangle plane)
        {
            this.Polygon3D = polygon3D;
            this.Holes3D = holes3D;

            this.Polygon2D = polygon2D;
            this.Holes2D = holes2D;

            this.IntersectedTriangles = intersectedTriangles;
            this.IntersectedTrianglesHoles = intersectedTrianglesHoles;

            this.Plane = plane;
        }

        #endregion

        #region Public Properties

        public readonly Point3D[] Polygon3D;
        public readonly Point3D[][] Holes3D;

        // This is the same polygon and holes, but rotated onto the XY plane
        public readonly Point[] Polygon2D;
        public readonly Point[][] Holes2D;

        // These are the triangles and intersect points where the polygons sliced through
        public readonly ContourTriangleIntersect[] IntersectedTriangles;
        public readonly ContourTriangleIntersect[][] IntersectedTrianglesHoles;

        public readonly ITriangle Plane;

        #endregion

        #region Public Methods

        /// <summary>
        /// This calculates the volume of the polygon above the plane
        /// WARNING: The intersect triangles need to be of type TriangleIndexedLinked, or this method will throw an exception (they need to
        /// be linked by edges, no need to link corners)
        /// </summary>
        public double GetVolumeAbove()
        {
            // All the perimiter triangles must be done here.  Otherwise holes will cause problems
            long[] permiters = UtilityCore.Iterate(this.IntersectedTriangles, this.IntersectedTrianglesHoles.SelectMany(o => o)).
                Select(o => o.Triangle.Token).
                ToArray();

            List<long> seenMiddles = new List<long>();

            // Main Polygon
            double retVal = GetVolumeAbove_DoIt(this.IntersectedTriangles, this.Plane, permiters, seenMiddles);

            // Holes
            foreach (var hole in this.IntersectedTrianglesHoles)
            {
                //NOTE: Adding to the total, not subtracting.  This is because the part of the triangle that is above the plane is leaning
                //over into the main part of the polygon (the hole is the part of the triangle that's below the plane)
                retVal += GetVolumeAbove_DoIt(hole, this.Plane, permiters, seenMiddles);
            }

            return retVal;
        }

        #endregion

        #region Private Methods

        private static double GetVolumeAbove_DoIt(ContourTriangleIntersect[] triangles, ITriangle plane, long[] permiters, List<long> seenMiddles)
        {
            double retVal = 0;

            foreach (ContourTriangleIntersect triangle in triangles)
            {
                TriangleIndexedLinked triangleCast = triangle.Triangle as TriangleIndexedLinked;
                if (triangleCast == null)
                {
                    throw new ArgumentException("Triangles must be of type TriangleIndexedLinked");
                }

                #region Edge triangle

                // Get the points that are above the plane, and the corresponding point on the plane
                var abovePoints = new TriangleCorner[] { TriangleCorner.Corner_0, TriangleCorner.Corner_1, TriangleCorner.Corner_2 }.
                    Where(o => Math3D.IsAbovePlane(plane, triangleCast.GetPoint(o))).
                    Select(o => new { Corner = o, AbovePoint = triangleCast.GetPoint(o), PlanePoint = Math3D.GetClosestPoint_Plane_Point(plane, triangleCast.GetPoint(o)) }).
                    ToArray();

                foreach (var abovePoint in abovePoints)     // there will either be 1 or 2 points
                {
                    // Get the area of the base
                    Vector3D baseVect = triangle.Intersect2 - triangle.Intersect1;
                    double baseHeight = (Math3D.GetClosestPoint_Line_Point(triangle.Intersect1, baseVect, abovePoint.PlanePoint) - abovePoint.PlanePoint).Length;
                    double area = baseVect.Length * baseHeight / 2d;

                    // Get the 3D height
                    double hullHeight = (abovePoint.AbovePoint - abovePoint.PlanePoint).Length;

                    // Now add this volume to the total
                    retVal += area * hullHeight / 3d;
                }

                #endregion

                if (abovePoints.Length == 2)
                {
                    #region Middle triangles

                    TriangleEdge edge = TriangleIndexedLinked.GetEdge(abovePoints[0].Corner, abovePoints[1].Corner);

                    TriangleIndexedLinked neighbor = triangleCast.GetNeighbor(edge);
                    if (neighbor != null && !permiters.Contains(neighbor.Token) && !seenMiddles.Contains(neighbor.Token))
                    {
                        //NOTE: This is a recursive method
                        retVal += GetVolumeAbove_Middle(neighbor, permiters, seenMiddles, plane);
                    }

                    #endregion
                }
            }

            return retVal;
        }
        private static double GetVolumeAbove_Middle(TriangleIndexedLinked triangle, long[] permiters, List<long> seenMiddles, ITriangle plane)
        {
            seenMiddles.Add(triangle.Token);

            if (!triangle.IndexArray.All(o => Math3D.IsAbovePlane(plane, triangle.AllPoints[o])))
            {
                // This seems to only happen when the plane is Z=0
                return 0;
            }

            #region Get the volume under this triangle

            Point3D[] points = new Point3D[]
                {
                    Math3D.GetClosestPoint_Plane_Point(plane, triangle.Point0),     // 0
                    Math3D.GetClosestPoint_Plane_Point(plane, triangle.Point1),     // 1
                    Math3D.GetClosestPoint_Plane_Point(plane, triangle.Point2),     // 2
                    triangle.Point0,        // 3
                    triangle.Point1,        // 4
                    triangle.Point2     // 5
                };

            List<TriangleIndexed> hull = new List<TriangleIndexed>();

            // Bottom
            hull.Add(new TriangleIndexed(0, 1, 2, points));

            // Top
            hull.Add(new TriangleIndexed(3, 4, 5, points));

            // Face 0-1
            hull.Add(new TriangleIndexed(0, 1, 3, points));
            hull.Add(new TriangleIndexed(0, 1, 4, points));

            // Face 1-2
            hull.Add(new TriangleIndexed(1, 2, 4, points));
            hull.Add(new TriangleIndexed(1, 2, 5, points));

            // Face 2-0
            hull.Add(new TriangleIndexed(0, 2, 3, points));
            hull.Add(new TriangleIndexed(0, 2, 5, points));

            double retVal = Math3D.GetVolume_ConvexHull(hull.ToArray());

            #endregion

            // Try each of the three neighbors
            foreach (TriangleEdge edge in new TriangleEdge[] { TriangleEdge.Edge_01, TriangleEdge.Edge_12, TriangleEdge.Edge_20 })
            {
                TriangleIndexedLinked neighbor = triangle.GetNeighbor(edge);        // neighbor should never be null in practice - just in some edge cases :D
                if (neighbor != null && !permiters.Contains(neighbor.Token) && !seenMiddles.Contains(neighbor.Token))
                {
                    // Recurse
                    retVal += GetVolumeAbove_Middle(neighbor, permiters, seenMiddles, plane);
                }
            }

            return retVal;
        }

        #endregion
    }

    #endregion
    #region Class: ContourTriangleIntersect

    public class ContourTriangleIntersect
    {
        public ContourTriangleIntersect(Point3D intersect1, Point3D intersect2, ITriangle triangle)
        {
            this.Intersect1 = intersect1;
            this.Intersect2 = intersect2;
            this.Triangle = triangle;
        }

        public readonly Point3D Intersect1;
        public readonly Point3D Intersect2;
        public readonly ITriangle Triangle;
    }

    #endregion

    #region Class: VoronoiResult3D

    public class VoronoiResult3D
    {
        public VoronoiResult3D(Point3D[] controlPoints, Point3D[] edgePoints, Edge3D[] edges, Face3D[] faces, int[][] facesByControlPoint, Tetrahedron[] delaunay)
        {
            this.ControlPoints = controlPoints;
            this.EdgePoints = edgePoints;
            this.Edges = edges;
            this.Faces = faces;
            this.FacesByControlPoint = facesByControlPoint;
            this.Delaunay = delaunay;
        }

        /// <summary>
        /// These are the points that were passed in to the delaunay/voronoi method
        /// </summary>
        public readonly Point3D[] ControlPoints;

        /// <summary>
        /// These are the points that make up the edges
        /// </summary>
        public readonly Point3D[] EdgePoints;
        /// <summary>
        /// These are the edges that make up the faces
        /// </summary>
        public readonly Edge3D[] Edges;
        /// <summary>
        /// These are the faces (boundaries between control points)
        /// </summary>
        public readonly Face3D[] Faces;

        /// <summary>
        /// This tells which set of faces are for each control point
        /// </summary>
        /// <remarks>
        /// FacesByControlPoint[control points index][i]
        /// </remarks>
        public readonly int[][] FacesByControlPoint;

        /// <summary>
        /// This is the delanunay that links the points together
        /// </summary>
        /// <remarks>
        /// This needs to be calculated anyway to figure out the directions of the rays, so just including it in the results
        /// </remarks>
        public readonly Tetrahedron[] Delaunay;

        /// <summary>
        /// This returns the control points that have this face
        /// </summary>
        public int[] GetControlPoints(int faceIndex)
        {
            List<int> retVal = new List<int>();

            for (int cntr = 0; cntr < this.FacesByControlPoint.Length; cntr++)
            {
                if (this.FacesByControlPoint[cntr].Any(o => o == faceIndex))
                {
                    retVal.Add(cntr);
                }
            }

            return retVal.ToArray();
        }

        public TriangleIndexed[] GetVoronoiCellHull(int index, double rayLength = 1000)
        {
            // Ask each face for a tesselation
            var facePolys = this.FacesByControlPoint[index].
                SelectMany(o => this.Faces[o].GetPolygonTriangles(rayLength)).
                ToArray();

            // Can't trust that the indices to points are the same across faces, so dedupe points and return a consistent set
            return TriangleIndexed.ConvertToIndexed(facePolys);
        }
    }

    #endregion
    #region Class: Face3D

    public class Face3D
    {
        #region Declaration Section

        private readonly object _lock = new object();

        #endregion

        #region Constructor

        public Face3D(int[] edges, Edge3D[] allEdges)
        {
            //TODO: May want to validate that the points are coplanar

            this.AllEdges = allEdges;

            this.EdgeIndices = edges;
            this.Edges = edges.Select(o => allEdges[o]).ToArray();

            this.IsClosed = this.Edges.All(o => o.EdgeType == EdgeType.Segment);
        }

        #endregion

        public readonly int[] EdgeIndices;

        public readonly Edge3D[] Edges;

        public readonly Edge3D[] AllEdges;

        /// <summary>
        /// True: All edges are segments
        /// False:  This face contains rays
        /// </summary>
        public readonly bool IsClosed;

        private long? _token = null;
        public long Token
        {
            get
            {
                lock (_lock)
                {
                    if (_token == null)
                    {
                        _token = TokenGenerator.NextToken();
                    }

                    return _token.Value;
                }
            }
        }

        #region Public Methods

        /// <returns>
        /// Item1=Indices into all points
        /// Item2=All points
        /// </returns>
        public Tuple<int[], Point3D[]> GetPolygon(double rayLength = 1000)
        {
            if (this.IsClosed)
            {
                return GetPolygon_Closed(this.Edges);
            }
            else
            {
                return GetPolygon_Open(this.Edges, rayLength);
            }
        }
        /// <summary>
        /// This converts into polygons
        /// NOTE: The triangle's index has nothing to do with this.EdgeIndicies.  The values are local to the set of triangles returned
        /// </summary>
        public TriangleIndexed[] GetPolygonTriangles(double rayLength = 1000)
        {
            var polyPoints = GetPolygon(rayLength);
            return Math2D.GetTrianglesFromConvexPoly(polyPoints.Item1, polyPoints.Item2);
        }

        #endregion

        #region Private Methods

        private static Tuple<int[], Point3D[]> GetPolygon_Closed(Edge3D[] edges)
        {
            #region asserts
#if DEBUG

            if (edges.Length < 3)
            {
                throw new ArgumentException("Must have at least three edges: " + edges.Length.ToString());
            }

            if (!edges.All(o => o.EdgeType == EdgeType.Segment))
            {
                throw new ArgumentException("All edges must be segments when calling the closed method");
            }

#endif
            #endregion

            List<int> indices = new List<int>();
            Point3D[] points = edges[0].AllEdgePoints;

            // Set up a sequence to avoid duplicating logic when looping back from the last edge to first
            var edgePairs = UtilityCore.Iterate(
                Enumerable.Range(0, edges.Length - 1).Select(o => Tuple.Create(edges[o], edges[o + 1])),
                new[] { Tuple.Create(edges[edges.Length - 1], edges[0]) });

            foreach (Tuple<Edge3D, Edge3D> pair in edgePairs)
            {
                // Add the point from edge1 that is shared with edge2
                int commonIndex = Edge3D.GetCommonIndex(pair.Item1, pair.Item2);
                if (commonIndex < 0)
                {
                    // While in this main loop, there can't be any breaks
                    throw new ApplicationException("Didn't find common point between edges");
                }
                else
                {
                    indices.Add(commonIndex);
                }
            }

            return Tuple.Create(indices.ToArray(), points);
        }
        private Tuple<int[], Point3D[]> GetPolygon_Open(Edge3D[] edges, double rayLength)
        {
            #region asserts
#if DEBUG

            if (edges.Length < 2)
            {
                throw new ArgumentException("Must have at least two edges: " + edges.Length.ToString());
            }

            if (edges[0].EdgeType != EdgeType.Ray || edges[edges.Length - 1].EdgeType != EdgeType.Ray)
            {
                throw new ArgumentException("First and last edges must be rays when calling the open method");
            }

            if (Enumerable.Range(1, edges.Length - 2).Any(o => edges[o].EdgeType != EdgeType.Segment))
            {
                throw new ArgumentException("Middle edges must be segments when calling the open method");
            }

#endif
            #endregion

            List<int> indices = new List<int>();

            List<Point3D> points = new List<Point3D>(edges[0].AllEdgePoints);       // two more will be added at the end of the list for the rays
            points.Add(edges[0].GetPoint1Ext(rayLength));
            points.Add(edges[edges.Length - 1].GetPoint1Ext(rayLength));

            // The end of the first ray
            indices.Add(points.Count - 2);

            foreach (Tuple<Edge3D, Edge3D> pair in Enumerable.Range(0, edges.Length - 1).Select(o => Tuple.Create(edges[o], edges[o + 1])))
            {
                // Add the point from edge1 that is shared with edge2
                int commonIndex = Edge3D.GetCommonIndex(pair.Item1, pair.Item2);
                if (commonIndex < 0)
                {
                    // While in this main loop, there can't be any breaks
                    throw new ApplicationException("Didn't find common point between edges");
                }
                else
                {
                    indices.Add(commonIndex);
                }
            }

            // The end of the last ray
            indices.Add(points.Count - 1);

            return Tuple.Create(indices.ToArray(), points.ToArray());
        }

        #endregion
    }

    #endregion
    #region Class: Edge3D

    /// <summary>
    /// This represents a line in 3D (either a line segment, ray, or infinite line)
    /// </summary>
    /// <remarks>
    /// I decided to take an array of points, and store indexes into that array.  That makes it easier to compare points across
    /// multiple edges to see which ones are using the same points (int comparisons are exact, doubles are iffy)
    /// </remarks>
    public class Edge3D
    {
        #region Declaration Section

        private readonly object _lock = new object();

        #endregion

        #region Constructor

        public Edge3D(Point3D point0, Point3D point1)
        {
            this.EdgeType = EdgeType.Segment;
            this.Index0 = 0;
            this.Index1 = 1;
            this.Direction = null;
            this.AllEdgePoints = new Point3D[] { point0, point1 };
        }
        public Edge3D(int index0, int index1, Point3D[] allEdgePoints)
        {
            this.EdgeType = EdgeType.Segment;
            this.Index0 = index0;
            this.Index1 = index1;
            this.Direction = null;
            this.AllEdgePoints = allEdgePoints;
        }
        public Edge3D(EdgeType edgeType, int index0, Vector3D direction, Point3D[] allEdgePoints)
        {
            if (edgeType == EdgeType.Segment)
            {
                throw new ArgumentException("This overload requires edge type to be Ray or Line, not Segment");
            }

            this.EdgeType = edgeType;
            this.Index0 = index0;
            this.Index1 = null;
            this.Direction = direction;
            this.AllEdgePoints = allEdgePoints;
        }

        #endregion

        /// <summary>
        /// This tells what type of line this edge represents
        /// </summary>
        /// <remarks>
        /// Segment:
        ///     Index0, Index1 will be populated
        /// 
        /// Ray:
        ///     Index0, Direction will be populated
        ///     
        /// Line:
        ///     Index0, Direction will be populated, but to get the full line, use the opposite of direction as well
        /// </remarks>
        public readonly EdgeType EdgeType;

        public readonly int Index0;
        public readonly int? Index1;

        public readonly Vector3D? Direction;
        /// <summary>
        /// This either returns Direction (if the edge is a ray or line), or it returns Point1 - Point0
        /// (this is helpful if you just want to treat the edge like a ray)
        /// </summary>
        public Vector3D DirectionExt
        {
            get
            {
                if (this.Direction != null)
                {
                    return this.Direction.Value;
                }
                else
                {
                    return this.Point1.Value - this.Point0;
                }
            }
        }

        public Point3D Point0
        {
            get
            {
                return this.AllEdgePoints[this.Index0];
            }
        }
        public Point3D? Point1
        {
            get
            {
                if (this.Index1 == null)
                {
                    return null;
                }

                return this.AllEdgePoints[this.Index1.Value];
            }
        }
        /// <summary>
        /// This either returns Point1 (if the edge is a segment), or it returns Point0 + Direction
        /// (this is helpful if you just want to always treat the edge like a segment)
        /// </summary>
        public Point3D Point1Ext
        {
            get
            {
                if (this.Point1 != null)
                {
                    return this.Point1.Value;
                }
                else
                {
                    return this.Point0 + this.Direction.Value;
                }
            }
        }

        public readonly Point3D[] AllEdgePoints;

        private long? _token = null;
        public long Token
        {
            get
            {
                lock (_lock)
                {
                    if (_token == null)
                    {
                        _token = TokenGenerator.NextToken();
                    }

                    return _token.Value;
                }
            }
        }

        #region Public Methods

        /// <summary>
        /// This finds all unique lines, and converts them into line segments
        /// NOTE: This returns more points than edges[0].AllEdgePoints, because it creates points for rays
        /// </summary>
        public static Tuple<Tuple<int, int>[], Point3D[]> GetUniqueLines(Edge3D[] edges, double? rayLength = null)
        {
            if (edges.Length == 0)
            {
                return Tuple.Create(new Tuple<int, int>[0], new Point3D[0]);
            }

            // Dedupe the edges
            Edge3D[] uniqueEdges = GetUniqueLines(edges);

            List<Tuple<int, int>> segments = new List<Tuple<int, int>>();
            List<Point3D> points = new List<Point3D>();

            // Segments
            foreach (Edge3D segment in uniqueEdges.Where(o => o.EdgeType == EdgeType.Segment))
            {
                if (points.Count == 0)
                {
                    points.AddRange(segment.AllEdgePoints);
                }

                segments.Add(Tuple.Create(segment.Index0, segment.Index1.Value));
            }

            // Rays
            foreach (Edge3D ray in uniqueEdges.Where(o => o.EdgeType == EdgeType.Ray))
            {
                points.Add(ray.GetPoint1Ext(rayLength));

                segments.Add(Tuple.Create(ray.Index0, points.Count - 1));
            }

            return Tuple.Create(segments.ToArray(), points.ToArray());
        }

        public static Edge3D[] GetUniqueLines(Edge3D[] edges)
        {
            List<Edge3D> retVal = new List<Edge3D>();

            // Interior edges
            retVal.AddRange(edges.
                Where(o => o.EdgeType == EdgeType.Segment).
                Select(o => new { Edge = o, Key = Tuple.Create(Math.Min(o.Index0, o.Index1.Value), Math.Max(o.Index0, o.Index1.Value)) }).
                Distinct(o => o.Key).
                Select(o => o.Edge));

            // Rays
            var rays = edges.
                Where(o => o.EdgeType == EdgeType.Ray).
                Select(o => new { Ray = o, DirectionUnit = o.Direction.Value.ToUnit() }).
                ToLookup(o => o.Ray.Index0);

            foreach (var raySet in rays)
            {
                // These are the rays off of a point.  Dedupe
                retVal.AddRange(raySet.
                    Distinct((o, p) => Math3D.IsNearValue(o.DirectionUnit, p.DirectionUnit)).
                    Select(o => o.Ray));
            }

            return retVal.ToArray();
        }

        /// <summary>
        /// This is the same as Point1Ext get, but lets the user pass in how long the extension should be (if null is passed in
        /// just uses direction's length - which is what Point1Ext does)
        /// </summary>
        public Point3D GetPoint1Ext(double? rayLength = null)
        {
            if (this.Point1 != null)
            {
                return this.Point1.Value;
            }
            else
            {
                if (rayLength == null)
                {
                    return this.Point0 + this.Direction.Value;
                }
                else
                {
                    return this.Point0 + (this.Direction.Value.ToUnit() * rayLength.Value);
                }
            }
        }

        public Point3D GetMidpointExt(double? rayLength = null)
        {
            return this.Point0 + ((GetPoint1Ext(rayLength) - this.Point0) * .5);
        }

        /// <summary>
        /// This returns the point from edge that isn't in common with otherEdge (makes up a point if edge is a ray)
        /// </summary>
        public static Point3D GetOtherPointExt(Edge3D edge, Edge3D otherEdge, double? rayLength = null)
        {
            int common = GetCommonIndex(edge, otherEdge);

            if (edge.Index0 == common)
            {
                return edge.GetPoint1Ext(rayLength);
            }
            else
            {
                return edge.Point0;
            }
        }

        /// <summary>
        /// This tells which edges are touching each of the other edges
        /// NOTE: This only looks at the ints.  It doesn't project lines
        /// </summary>
        public static int[][] GetTouchingEdges(Edge3D[] edges)
        {
            int[][] retVal = new int[edges.Length][];

            for (int outer = 0; outer < edges.Length; outer++)
            {
                List<int> touching = new List<int>();

                for (int inner = 0; inner < edges.Length; inner++)
                {
                    if (outer == inner)
                    {
                        continue;
                    }

                    if (IsTouching(edges[outer], edges[inner]))
                    {
                        touching.Add(inner);
                    }
                }

                retVal[outer] = touching.ToArray();
            }

            return retVal;
        }

        /// <summary>
        /// This returns whether the two edges touch
        /// NOTE: It only compares ints.  It doesn't check if lines cross each other
        /// </summary>
        public static bool IsTouching(Edge3D edge0, Edge3D edge1)
        {
            return GetCommonIndex(edge0, edge1) >= 0;
        }
        public static int GetCommonIndex(Edge3D edge0, Edge3D edge1)
        {
            //  All edge types have an index 0, so get that comparison out of the way
            if (edge0.Index0 == edge1.Index0)
            {
                return edge0.Index0;
            }

            if (edge0.EdgeType == EdgeType.Segment)
            {
                //  Extra check, since edge0 is a segment
                if (edge0.Index1.Value == edge1.Index0)
                {
                    return edge0.Index1.Value;
                }

                //  If edge1 is also a segment, then compare its endpoint to edge0's points
                if (edge1.EdgeType == EdgeType.Segment)
                {
                    if (edge1.Index1.Value == edge0.Index0)
                    {
                        return edge1.Index1.Value;
                    }
                    else if (edge1.Index1.Value == edge0.Index1.Value)
                    {
                        return edge1.Index1.Value;
                    }
                }
            }
            else if (edge1.EdgeType == EdgeType.Segment)
            {
                //  Edge1 is a segment, but edge0 isn't, so just need the single compare
                if (edge1.Index1.Value == edge0.Index0)
                {
                    return edge1.Index1.Value;
                }
            }

            //  No more compares needed (this method doesn't bother with projecting rays/lines to see if they intersect, that's left up to the caller if they need it)
            return -1;
        }

        /// <summary>
        /// This returns the point in common between the two edges, and vectors that represent rays coming out of
        /// that point
        /// </summary>
        /// <remarks>
        /// This will throw an exception if the edges don't share a common point
        /// 
        /// It doesn't matter if the edges are segments or rays (will bomb if either is a line)
        /// </remarks>
        public static Tuple<Point3D, Vector3D, Vector3D> GetRays(Edge3D edge0, Edge3D edge1)
        {
            if (edge0.EdgeType == EdgeType.Line || edge1.EdgeType == EdgeType.Line)
            {
                throw new ArgumentException("This method doesn't allow lines, only segments and rays");
            }

            int common = GetCommonIndex(edge0, edge1);
            if (common < 0)
            {
                throw new ArgumentException("The edges passed in don't share a common point");
            }

            return Tuple.Create(
                edge0.AllEdgePoints[common],
                GetDirectionFromPoint(edge0, common),
                GetDirectionFromPoint(edge1, common));
        }

        /// <summary>
        /// This returns the direction from the point at index passed in to the other point (rays only go one direction, but
        /// segments can go either, lines throw an exception)
        /// </summary>
        public static Vector3D GetDirectionFromPoint(Edge3D edge, int index)
        {
            switch (edge.EdgeType)
            {
                case EdgeType.Line:
                    throw new ArgumentException("This method doesn't make sense for lines");        //  because lines can go two directions

                case EdgeType.Ray:
                    #region Ray

                    if (edge.Index0 != index)
                    {
                        throw new ArgumentException("The index passed in doesn't belong to this edge");
                    }

                    return edge.Direction.Value;

                    #endregion

                case EdgeType.Segment:
                    #region Segment

                    if (edge.Index0 == index)
                    {
                        return edge.Point1.Value - edge.Point0;
                    }
                    else if (edge.Index1.Value == index)
                    {
                        return edge.Point0 - edge.Point1.Value;
                    }
                    else
                    {
                        throw new ArgumentException("The index passed in doesn't belong to this edge");
                    }

                    #endregion

                default:
                    throw new ApplicationException("Unknown EdgeType: " + edge.EdgeType.ToString());
            }
        }

        public bool ContainsPoint(int index)
        {
            return this.Index0 == index || (this.Index1 != null && this.Index1.Value == index);
        }

        /// <summary>
        /// This is useful when looking at lists of edges in the quick watch
        /// </summary>
        public override string ToString()
        {
            const string DELIM = "       |       ";

            StringBuilder retVal = new StringBuilder(100);

            retVal.Append(this.EdgeType.ToString());
            retVal.Append(DELIM);

            switch (this.EdgeType)
            {
                case HelperClassesWPF.EdgeType.Segment:
                    retVal.Append(string.Format("{0} - {1}{2}({3}) ({4})",
                        this.Index0,
                        this.Index1,
                        DELIM,
                        this.Point0.ToString(2),
                        this.Point1.Value.ToString(2)));
                    break;

                case HelperClassesWPF.EdgeType.Ray:
                    retVal.Append(string.Format("{0}{1}({2}) --> ({3})",
                        this.Index0,
                        DELIM,
                        this.Point0.ToString(2),
                        this.Direction.Value.ToString(2)));
                    break;

                case HelperClassesWPF.EdgeType.Line:
                    retVal.Append(string.Format("{0}{1}({2}) <---> ({3})",
                        this.Index0,
                        DELIM,
                        this.Point0.ToString(2),
                        this.Direction.Value.ToString(2)));
                    break;

                default:
                    retVal.Append("Unknown EdgeType");
                    break;
            }

            return retVal.ToString();
        }

        #endregion
    }

    #endregion

    #region Class: Fragment Results

    public class VoronoiHullIntersect_PatchFragment
    {
        public VoronoiHullIntersect_PatchFragment(VoronoiHullIntersect_TriangleFragment[] polygon, int controlPointIndex)
        {
            this.Polygon = polygon;
            this.ControlPointIndex = controlPointIndex;
        }

        public readonly VoronoiHullIntersect_TriangleFragment[] Polygon;

        public readonly int ControlPointIndex;
    }

    public class VoronoiHullIntersect_TriangleFragment
    {
        public VoronoiHullIntersect_TriangleFragment(ITriangleIndexed triangle, Tuple<TriangleEdge, int>[] faceIndices, int originalTriangleIndex)
        {
            this.Triangle = triangle;
            this.FaceIndices = faceIndices;
            this.OriginalTriangleIndex = originalTriangleIndex;
        }

        public readonly ITriangleIndexed Triangle;
        public readonly Tuple<TriangleEdge, int>[] FaceIndices;
        public readonly int OriginalTriangleIndex;
    }

    #endregion
}
