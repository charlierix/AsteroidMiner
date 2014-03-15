using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

using Game.HelperClasses;
using System.IO;

namespace Game.Newt.HelperClasses
{
    public static class UtilityWPF
    {
        #region Class: BuildTube

        private static class BuildTube
        {
            public static MeshGeometry3D Build(int numSides, List<TubeRingBase> rings, bool softSides, bool shouldCenterZ, Transform3D transform = null)
            {
                if (transform == null)
                {
                    transform = Transform3D.Identity;
                }

                // Do some validation/prep work
                double height, curZ;
                Initialize(out height, out curZ, numSides, rings, shouldCenterZ);

                MeshGeometry3D retVal = new MeshGeometry3D();

                int pointOffset = 0;

                // This is used when softSides is true.  This allows for a way to have a common normal between one ring's bottom and the next ring's top
                double[] rotateAnglesForPerp = null;

                TubeRingBase nextRing = rings.Count > 1 ? rings[1] : null;
                EndCap(ref pointOffset, ref rotateAnglesForPerp, retVal, numSides, null, rings[0], nextRing, transform, true, curZ, softSides);

                for (int cntr = 0; cntr < rings.Count - 1; cntr++)
                {
                    if (cntr > 0 && cntr < rings.Count - 1 && !(rings[cntr] is TubeRingRegularPolygon))
                    {
                        throw new ArgumentException("Only rings are allowed in the middle of the tube");
                    }

                    Middle(ref pointOffset, ref rotateAnglesForPerp, retVal, transform, numSides, rings[cntr], rings[cntr + 1], curZ, softSides);

                    curZ += rings[cntr + 1].DistFromPrevRing;
                }

                TubeRingBase prevRing = rings.Count > 1 ? rings[rings.Count - 2] : null;
                EndCap(ref pointOffset, ref rotateAnglesForPerp, retVal, numSides, prevRing, rings[rings.Count - 1], null, transform, false, curZ, softSides);

                // Exit Function
                //retVal.Freeze();
                return retVal;
            }

            #region Private Methods

            private static void Initialize(out double height, out double startZ, int numSides, List<TubeRingBase> rings, bool shouldCenterZ)
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
                        // Say both are points - you'd have a line.  Domes must attach to a ring, not a point or another dome
                        throw new ArgumentException(string.Format("When only two rings definitions are passed in, at least one of them must be a ring:\r\n{0}\r\n{1}", rings[0].GetType().ToString(), rings[1].GetType().ToString()));
                    }
                }

                if (numSides < 3)
                {
                    throw new ArgumentException("numSides must be at least 3: " + numSides.ToString(), "numSides");
                }

                #endregion

                // Calculate total height
                height = TubeRingBase.GetTotalHeight(rings);

                // Figure out the starting Z
                startZ = 0d;
                if (shouldCenterZ)
                {
                    startZ = height * -.5d;      // starting in the negative
                }
            }

            private static Point[] GetPointsRegPoly(int numSides, TubeRingRegularPolygon ring)
            {
                // Multiply the returned unit circle by the ring's radius
                return Math2D.GetCircle_Cached(numSides).Select(o => new Point(ring.RadiusX * o.X, ring.RadiusY * o.Y)).ToArray();
            }

            private static void EndCap(ref int pointOffset, ref double[] rotateAnglesForPerp, MeshGeometry3D geometry, int numSides, TubeRingBase ringPrev, TubeRingBase ringCurrent, TubeRingBase ringNext, Transform3D transform, bool isFirst, double z, bool softSides)
            {
                if (ringCurrent is TubeRingDome)
                {
                    #region Dome

                    Point[] domePointsTheta = EndCap_GetPoints(ringPrev, ringNext, numSides, isFirst);
                    double capHeight = EndCap_GetCapHeight(ringCurrent, ringNext, isFirst);
                    Transform3D domeTransform = EndCap_GetTransform(transform, isFirst, z, capHeight);
                    Transform3D domeTransformNormal = EndCap_GetNormalTransform(domeTransform);

                    if (softSides)
                    {
                        EndCap_DomeSoft(ref pointOffset, ref rotateAnglesForPerp, geometry, domePointsTheta, domeTransform, domeTransformNormal, (TubeRingDome)ringCurrent, capHeight, isFirst);
                    }
                    else
                    {
                        EndCap_DomeHard(ref pointOffset, ref rotateAnglesForPerp, geometry, domePointsTheta, domeTransform, domeTransformNormal, (TubeRingDome)ringCurrent, capHeight, isFirst);
                    }

                    #endregion
                }
                else if (ringCurrent is TubeRingPoint)
                {
                    #region Point

                    Point[] conePointsTheta = EndCap_GetPoints(ringPrev, ringNext, numSides, isFirst);
                    double capHeight = EndCap_GetCapHeight(ringCurrent, ringNext, isFirst);
                    Transform3D coneTransform = EndCap_GetTransform(transform, isFirst, z, capHeight);

                    if (softSides)
                    {
                        Transform3D coneTransformNormal = EndCap_GetNormalTransform(coneTransform);     // only need the transform for soft, because it cheats and has a hardcode normal (hard calculates the normal for each triangle)

                        EndCap_ConeSoft(ref pointOffset, ref rotateAnglesForPerp, geometry, conePointsTheta, coneTransform, coneTransformNormal, (TubeRingPoint)ringCurrent, capHeight, isFirst);
                    }
                    else
                    {
                        EndCap_ConeHard(ref pointOffset, ref rotateAnglesForPerp, geometry, conePointsTheta, coneTransform, (TubeRingPoint)ringCurrent, capHeight, isFirst);
                    }

                    #endregion
                }
                else if (ringCurrent is TubeRingRegularPolygon)
                {
                    #region Regular Polygon

                    TubeRingRegularPolygon ringCurrentCast = (TubeRingRegularPolygon)ringCurrent;

                    if (ringCurrentCast.IsClosedIfEndCap)		// if it's open, there is nothing to do for the end cap
                    {
                        Point[] polyPointsTheta = GetPointsRegPoly(numSides, ringCurrentCast);
                        Transform3D polyTransform = EndCap_GetTransform(transform, isFirst, z);
                        Transform3D polyTransformNormal = EndCap_GetNormalTransform(polyTransform);

                        if (softSides)
                        {
                            EndCap_PlateSoft(ref pointOffset, ref rotateAnglesForPerp, geometry, polyPointsTheta, polyTransform, polyTransformNormal, ringCurrent, isFirst);
                        }
                        else
                        {
                            EndCap_PlateHard(ref pointOffset, ref rotateAnglesForPerp, geometry, polyPointsTheta, polyTransform, polyTransformNormal, ringCurrent, isFirst);
                        }
                    }

                    #endregion
                }
                else
                {
                    throw new ApplicationException("Unknown tube ring type: " + ringCurrent.GetType().ToString());
                }
            }
            private static double EndCap_GetCapHeight(TubeRingBase ringCurrent, TubeRingBase ringNext, bool isFirst)
            {
                if (isFirst)
                {
                    // ringCurrent.DistFromPrevRing is ignored (because there is no previous).  So the cap's height is the next ring's dist from prev
                    return ringNext.DistFromPrevRing;
                }
                else
                {
                    // This is the last, so dist from prev has meaning
                    return ringCurrent.DistFromPrevRing;
                }
            }
            private static Point[] EndCap_GetPoints(TubeRingBase ringPrev, TubeRingBase ringNext, int numSides, bool isFirst)
            {
                // Figure out which ring to pull from
                TubeRingBase ring = null;
                if (isFirst)
                {
                    ring = ringNext;
                }
                else
                {
                    ring = ringPrev;
                }

                // Get the points
                Point[] retVal = null;
                if (ring != null && ring is TubeRingRegularPolygon)
                {
                    retVal = GetPointsRegPoly(numSides, (TubeRingRegularPolygon)ring);
                }

                if (retVal == null)
                {
                    throw new ApplicationException("The points are null for dome/point.  Validation should have caught this before now");
                }

                // Exit Function
                return retVal;
            }
            private static Transform3D EndCap_GetTransform(Transform3D transform, bool isFirst, double z)
            {
                // This overload is for a flat plate

                Transform3DGroup retVal = new Transform3DGroup();

                if (isFirst)
                {
                    // This still needs to be flipped for a flat cap so the normals turn out right
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
            private static Transform3D EndCap_GetTransform(Transform3D transform, bool isFirst, double z, double capHeight)
            {
                //This overload is for a cone/dome

                Transform3DGroup retVal = new Transform3DGroup();

                if (isFirst)
                {
                    // The dome/point methods are hard coded to go from 0 to capHeight, so rotate it so it will build from capHeight
                    // down to zero (offset by z)
                    retVal.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 180d)));
                    retVal.Children.Add(new TranslateTransform3D(0, 0, z + capHeight));
                    retVal.Children.Add(transform);
                }
                else
                {
                    retVal.Children.Add(new TranslateTransform3D(0, 0, z - capHeight));		// z is currently at the tip of the dome (it works for a flat cap, but a dome has height, so must back up to the last drawn object)
                    retVal.Children.Add(transform);
                }

                return retVal;
            }
            private static Transform3D EndCap_GetNormalTransform(Transform3D transform)
            {
                // Can't use all of the transform passed in for the normal, because translate portions will skew the normals funny
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

            private static void EndCap_DomeSoft(ref int pointOffset, ref double[] rotateAnglesForPerp, MeshGeometry3D geometry, Point[] pointsTheta, Transform3D transform, Transform3D normalTransform, TubeRingDome ring, double capHeight, bool isFirst)
            {
                // NOTE: There is one more than NumSegmentsPhi
                Point[] pointsPhi = ring.GetUnitPointsPhi(ring.NumSegmentsPhi);

                #region Positions/Normals

                //for (int phiCntr = 0; phiCntr < numSegmentsPhi; phiCntr++)		// The top point will be added after this loop
                for (int phiCntr = pointsPhi.Length - 1; phiCntr > 0; phiCntr--)
                {
                    if (!isFirst && ring.MergeNormalWithPrevIfSoft)
                    {
                        // Just reuse the points/normals from the previous ring
                        continue;
                    }

                    for (int thetaCntr = 0; thetaCntr < pointsTheta.Length; thetaCntr++)
                    {
                        // Phi points are going from bottom to equator.  
                        // pointsTheta are already the length they are supposed to be (not nessassarily a unit circle)

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
                            geometry.Normals.Add(normalTransform.Transform(point).ToVector().ToUnit());		// the normal is the same as the point for a sphere (but no tranlate transform)
                        }
                    }
                }

                // This is north pole point
                geometry.Positions.Add(transform.Transform(new Point3D(0, 0, capHeight)));
                geometry.Normals.Add(transform.Transform(new Vector3D(0, 0, 1)));

                #endregion

                #region Triangles - Rings

                int zOffsetBottom = pointOffset;
                int zOffsetTop;

                for (int phiCntr = 0; phiCntr < ring.NumSegmentsPhi - 1; phiCntr++)		// The top cone will be added after this loop
                {
                    zOffsetTop = zOffsetBottom + pointsTheta.Length;

                    for (int thetaCntr = 0; thetaCntr < pointsTheta.Length - 1; thetaCntr++)
                    {
                        // Top/Left triangle
                        geometry.TriangleIndices.Add(zOffsetBottom + thetaCntr + 0);
                        geometry.TriangleIndices.Add(zOffsetTop + thetaCntr + 1);
                        geometry.TriangleIndices.Add(zOffsetTop + thetaCntr + 0);

                        // Bottom/Right triangle
                        geometry.TriangleIndices.Add(zOffsetBottom + thetaCntr + 0);
                        geometry.TriangleIndices.Add(zOffsetBottom + thetaCntr + 1);
                        geometry.TriangleIndices.Add(zOffsetTop + thetaCntr + 1);
                    }

                    // Connecting the last 2 points to the first 2
                    // Top/Left triangle
                    geometry.TriangleIndices.Add(zOffsetBottom + (pointsTheta.Length - 1) + 0);
                    geometry.TriangleIndices.Add(zOffsetTop);		// wrapping back around
                    geometry.TriangleIndices.Add(zOffsetTop + (pointsTheta.Length - 1) + 0);

                    // Bottom/Right triangle
                    geometry.TriangleIndices.Add(zOffsetBottom + (pointsTheta.Length - 1) + 0);
                    geometry.TriangleIndices.Add(zOffsetBottom);
                    geometry.TriangleIndices.Add(zOffsetTop);

                    // Prep for the next ring
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

                // The last triangle links back to zero
                geometry.TriangleIndices.Add(zOffsetBottom + pointsTheta.Length - 1 + 0);
                geometry.TriangleIndices.Add(zOffsetBottom + 0);
                geometry.TriangleIndices.Add(topIndex);

                #endregion

                pointOffset = geometry.Positions.Count;
            }
            private static void EndCap_DomeHard_FLAWED(ref int pointOffset, ref double[] rotateAnglesForPerp, MeshGeometry3D geometry, Point[] pointsTheta, Transform3D transform, Transform3D normalTransform, TubeRingDome ring, double capHeight, bool isFirst)
            {
                // NOTE: There is one more than NumSegmentsPhi
                Point[] pointsPhi = ring.GetUnitPointsPhi(ring.NumSegmentsPhi);

                Point3D point;
                Vector3D normal;
                int zOffset = pointOffset;

                #region Triangles - Rings

                for (int phiCntr = 0; phiCntr < ring.NumSegmentsPhi - 1; phiCntr++)		// The top cone will be added after this loop
                {
                    for (int thetaCntr = 0; thetaCntr < pointsTheta.Length - 1; thetaCntr++)
                    {
                        // Phi points are going from bottom to equator.             <---------- NO!!!
                        // Phi points are going from the equator to the top





                        // pointsTheta are already the length they are supposed to be (not nessassarily a unit circle)

                        #region Top/Left triangle

                        point = new Point3D(
                            pointsTheta[thetaCntr].X * pointsPhi[phiCntr].Y,
                            pointsTheta[thetaCntr].Y * pointsPhi[phiCntr].Y,
                            capHeight * pointsPhi[phiCntr].X);

                        geometry.Positions.Add(transform.Transform(point));

                        point = new Point3D(
                            pointsTheta[thetaCntr + 1].X * pointsPhi[phiCntr + 1].Y,
                            pointsTheta[thetaCntr + 1].Y * pointsPhi[phiCntr + 1].Y,
                            capHeight * pointsPhi[phiCntr + 1].X);

                        geometry.Positions.Add(transform.Transform(point));

                        point = new Point3D(
                            pointsTheta[thetaCntr].X * pointsPhi[phiCntr + 1].Y,
                            pointsTheta[thetaCntr].Y * pointsPhi[phiCntr + 1].Y,
                            capHeight * pointsPhi[phiCntr + 1].X);

                        geometry.Positions.Add(transform.Transform(point));

                        normal = GetNormal(geometry.Positions[zOffset + 0], geometry.Positions[zOffset + 1], geometry.Positions[zOffset + 2]);
                        geometry.Normals.Add(normal);		// the normals point straight out of the face
                        geometry.Normals.Add(normal);
                        geometry.Normals.Add(normal);

                        geometry.TriangleIndices.Add(zOffset + 0);
                        geometry.TriangleIndices.Add(zOffset + 1);
                        geometry.TriangleIndices.Add(zOffset + 2);

                        #endregion

                        zOffset += 3;

                        #region Bottom/Right triangle

                        point = new Point3D(
                            pointsTheta[thetaCntr].X * pointsPhi[phiCntr].Y,
                            pointsTheta[thetaCntr].Y * pointsPhi[phiCntr].Y,
                            capHeight * pointsPhi[phiCntr].X);

                        geometry.Positions.Add(transform.Transform(point));

                        point = new Point3D(
                            pointsTheta[thetaCntr + 1].X * pointsPhi[phiCntr].Y,
                            pointsTheta[thetaCntr + 1].Y * pointsPhi[phiCntr].Y,
                            capHeight * pointsPhi[phiCntr].X);

                        geometry.Positions.Add(transform.Transform(point));

                        point = new Point3D(
                            pointsTheta[thetaCntr + 1].X * pointsPhi[phiCntr + 1].Y,
                            pointsTheta[thetaCntr + 1].Y * pointsPhi[phiCntr + 1].Y,
                            capHeight * pointsPhi[phiCntr + 1].X);

                        geometry.Positions.Add(transform.Transform(point));

                        normal = GetNormal(geometry.Positions[zOffset + 0], geometry.Positions[zOffset + 1], geometry.Positions[zOffset + 2]);
                        geometry.Normals.Add(normal);		// the normals point straight out of the face
                        geometry.Normals.Add(normal);
                        geometry.Normals.Add(normal);

                        geometry.TriangleIndices.Add(zOffset + 0);
                        geometry.TriangleIndices.Add(zOffset + 1);
                        geometry.TriangleIndices.Add(zOffset + 2);

                        #endregion

                        zOffset += 3;
                    }

                    // Connecting the last 2 points to the first 2
                    #region Top/Left triangle

                    point = new Point3D(
                        pointsTheta[pointsTheta.Length - 1].X * pointsPhi[phiCntr].Y,
                        pointsTheta[pointsTheta.Length - 1].Y * pointsPhi[phiCntr].Y,
                        capHeight * pointsPhi[phiCntr].X);

                    geometry.Positions.Add(transform.Transform(point));

                    point = new Point3D(
                        pointsTheta[0].X * pointsPhi[phiCntr + 1].Y,        // wrapping theta back around
                        pointsTheta[0].Y * pointsPhi[phiCntr + 1].Y,
                        capHeight * pointsPhi[phiCntr + 1].X);

                    geometry.Positions.Add(transform.Transform(point));

                    point = new Point3D(
                        pointsTheta[pointsTheta.Length - 1].X * pointsPhi[phiCntr + 1].Y,
                        pointsTheta[pointsTheta.Length - 1].Y * pointsPhi[phiCntr + 1].Y,
                        capHeight * pointsPhi[phiCntr + 1].X);

                    geometry.Positions.Add(transform.Transform(point));

                    normal = GetNormal(geometry.Positions[zOffset + 0], geometry.Positions[zOffset + 1], geometry.Positions[zOffset + 2]);
                    geometry.Normals.Add(normal);		// the normals point straight out of the face
                    geometry.Normals.Add(normal);
                    geometry.Normals.Add(normal);

                    geometry.TriangleIndices.Add(zOffset + 0);
                    geometry.TriangleIndices.Add(zOffset + 1);
                    geometry.TriangleIndices.Add(zOffset + 2);

                    #endregion

                    zOffset += 3;

                    #region Bottom/Right triangle

                    point = new Point3D(
                        pointsTheta[pointsTheta.Length - 1].X * pointsPhi[phiCntr].Y,
                        pointsTheta[pointsTheta.Length - 1].Y * pointsPhi[phiCntr].Y,
                        capHeight * pointsPhi[phiCntr].X);

                    geometry.Positions.Add(transform.Transform(point));

                    point = new Point3D(
                        pointsTheta[0].X * pointsPhi[phiCntr].Y,
                        pointsTheta[0].Y * pointsPhi[phiCntr].Y,
                        capHeight * pointsPhi[phiCntr].X);

                    geometry.Positions.Add(transform.Transform(point));

                    point = new Point3D(
                        pointsTheta[0].X * pointsPhi[phiCntr + 1].Y,
                        pointsTheta[0].Y * pointsPhi[phiCntr + 1].Y,
                        capHeight * pointsPhi[phiCntr + 1].X);

                    geometry.Positions.Add(transform.Transform(point));

                    normal = GetNormal(geometry.Positions[zOffset + 0], geometry.Positions[zOffset + 1], geometry.Positions[zOffset + 2]);
                    geometry.Normals.Add(normal);		// the normals point straight out of the face
                    geometry.Normals.Add(normal);
                    geometry.Normals.Add(normal);

                    geometry.TriangleIndices.Add(zOffset + 0);
                    geometry.TriangleIndices.Add(zOffset + 1);
                    geometry.TriangleIndices.Add(zOffset + 2);

                    #endregion

                    zOffset += 3;
                }

                #endregion
                #region Triangles - Cap

                // This is basically the same idea as EndCap_ConeHard, except for the extra phi bits

                Point3D topPoint = transform.Transform(new Point3D(0, 0, capHeight));

                for (int thetaCntr = 0; thetaCntr < pointsTheta.Length - 1; thetaCntr++)
                {
                    point = new Point3D(
                        pointsTheta[thetaCntr].X * pointsPhi[pointsPhi.Length - 1].Y,
                        pointsTheta[thetaCntr].Y * pointsPhi[pointsPhi.Length - 1].Y,
                        capHeight * pointsPhi[pointsPhi.Length - 1].X);

                    geometry.Positions.Add(transform.Transform(point));

                    point = new Point3D(
                        pointsTheta[thetaCntr + 1].X * pointsPhi[pointsPhi.Length - 1].Y,
                        pointsTheta[thetaCntr + 1].Y * pointsPhi[pointsPhi.Length - 1].Y,
                        capHeight * pointsPhi[pointsPhi.Length - 1].X);

                    geometry.Positions.Add(transform.Transform(point));

                    geometry.Positions.Add(topPoint);

                    normal = GetNormal(geometry.Positions[zOffset + 0], geometry.Positions[zOffset + 1], geometry.Positions[zOffset + 2]);
                    geometry.Normals.Add(normal);		// the normals point straight out of the face
                    geometry.Normals.Add(normal);
                    geometry.Normals.Add(normal);

                    geometry.TriangleIndices.Add(zOffset + 0);
                    geometry.TriangleIndices.Add(zOffset + 1);
                    geometry.TriangleIndices.Add(zOffset + 2);

                    zOffset += 3;
                }

                // The last triangle links back to zero
                point = new Point3D(
                    pointsTheta[pointsTheta.Length - 1].X * pointsPhi[pointsPhi.Length - 1].Y,
                    pointsTheta[pointsTheta.Length - 1].Y * pointsPhi[pointsPhi.Length - 1].Y,
                    capHeight * pointsPhi[pointsPhi.Length - 1].X);

                geometry.Positions.Add(transform.Transform(point));

                point = new Point3D(
                    pointsTheta[0].X * pointsPhi[pointsPhi.Length - 1].Y,
                    pointsTheta[0].Y * pointsPhi[pointsPhi.Length - 1].Y,
                    capHeight * pointsPhi[pointsPhi.Length - 1].X);

                geometry.Positions.Add(transform.Transform(point));

                geometry.Positions.Add(topPoint);

                normal = GetNormal(geometry.Positions[zOffset + 0], geometry.Positions[zOffset + 1], geometry.Positions[zOffset + 2]);
                geometry.Normals.Add(normal);		// the normals point straight out of the face
                geometry.Normals.Add(normal);
                geometry.Normals.Add(normal);

                geometry.TriangleIndices.Add(zOffset + 0);
                geometry.TriangleIndices.Add(zOffset + 1);
                geometry.TriangleIndices.Add(zOffset + 2);

                zOffset += 3;

                #endregion

                pointOffset = geometry.Positions.Count;
            }
            private static void EndCap_DomeHard(ref int pointOffset, ref double[] rotateAnglesForPerp, MeshGeometry3D geometry, Point[] pointsTheta, Transform3D transform, Transform3D normalTransform, TubeRingDome ring, double capHeight, bool isFirst)
            {
                // NOTE: There is one more than NumSegmentsPhi
                Point[] pointsPhi = ring.GetUnitPointsPhi(ring.NumSegmentsPhi);

                Point3D point;
                Vector3D normal;
                int zOffset = pointOffset;

                #region Triangles - Rings

                for (int phiCntr = 1; phiCntr < pointsPhi.Length - 1; phiCntr++)		// The top cone will be added after this loop
                {
                    for (int thetaCntr = 0; thetaCntr < pointsTheta.Length - 1; thetaCntr++)
                    {
                        // Phi points are going from bottom to equator                          <----------- NO?????????
                        // Phi points are going from the equator to the top

                        // pointsTheta are already the length they are supposed to be (not nessassarily a unit circle)

                        #region Top/Left triangle

                        point = new Point3D(
                            pointsTheta[thetaCntr].X * pointsPhi[phiCntr].Y,
                            pointsTheta[thetaCntr].Y * pointsPhi[phiCntr].Y,
                            capHeight * pointsPhi[phiCntr].X);

                        geometry.Positions.Add(transform.Transform(point));

                        point = new Point3D(
                            pointsTheta[thetaCntr].X * pointsPhi[phiCntr + 1].Y,
                            pointsTheta[thetaCntr].Y * pointsPhi[phiCntr + 1].Y,
                            capHeight * pointsPhi[phiCntr + 1].X);

                        geometry.Positions.Add(transform.Transform(point));

                        point = new Point3D(
                            pointsTheta[thetaCntr + 1].X * pointsPhi[phiCntr + 1].Y,
                            pointsTheta[thetaCntr + 1].Y * pointsPhi[phiCntr + 1].Y,
                            capHeight * pointsPhi[phiCntr + 1].X);

                        geometry.Positions.Add(transform.Transform(point));

                        normal = GetNormal(geometry.Positions[zOffset + 0], geometry.Positions[zOffset + 1], geometry.Positions[zOffset + 2]);
                        geometry.Normals.Add(normal);		// the normals point straight out of the face
                        geometry.Normals.Add(normal);
                        geometry.Normals.Add(normal);

                        geometry.TriangleIndices.Add(zOffset + 0);
                        geometry.TriangleIndices.Add(zOffset + 1);
                        geometry.TriangleIndices.Add(zOffset + 2);

                        #endregion

                        zOffset += 3;

                        #region Bottom/Right triangle

                        point = new Point3D(
                            pointsTheta[thetaCntr].X * pointsPhi[phiCntr].Y,
                            pointsTheta[thetaCntr].Y * pointsPhi[phiCntr].Y,
                            capHeight * pointsPhi[phiCntr].X);

                        geometry.Positions.Add(transform.Transform(point));

                        point = new Point3D(
                            pointsTheta[thetaCntr + 1].X * pointsPhi[phiCntr + 1].Y,
                            pointsTheta[thetaCntr + 1].Y * pointsPhi[phiCntr + 1].Y,
                            capHeight * pointsPhi[phiCntr + 1].X);

                        geometry.Positions.Add(transform.Transform(point));

                        point = new Point3D(
                            pointsTheta[thetaCntr + 1].X * pointsPhi[phiCntr].Y,
                            pointsTheta[thetaCntr + 1].Y * pointsPhi[phiCntr].Y,
                            capHeight * pointsPhi[phiCntr].X);

                        geometry.Positions.Add(transform.Transform(point));

                        normal = GetNormal(geometry.Positions[zOffset + 0], geometry.Positions[zOffset + 1], geometry.Positions[zOffset + 2]);
                        geometry.Normals.Add(normal);		// the normals point straight out of the face
                        geometry.Normals.Add(normal);
                        geometry.Normals.Add(normal);

                        geometry.TriangleIndices.Add(zOffset + 0);
                        geometry.TriangleIndices.Add(zOffset + 1);
                        geometry.TriangleIndices.Add(zOffset + 2);

                        #endregion

                        zOffset += 3;
                    }

                    // Connecting the last 2 points to the first 2
                    #region Top/Left triangle

                    point = new Point3D(
                        pointsTheta[pointsTheta.Length - 1].X * pointsPhi[phiCntr].Y,
                        pointsTheta[pointsTheta.Length - 1].Y * pointsPhi[phiCntr].Y,
                        capHeight * pointsPhi[phiCntr].X);

                    geometry.Positions.Add(transform.Transform(point));

                    point = new Point3D(
                        pointsTheta[pointsTheta.Length - 1].X * pointsPhi[phiCntr + 1].Y,
                        pointsTheta[pointsTheta.Length - 1].Y * pointsPhi[phiCntr + 1].Y,
                        capHeight * pointsPhi[phiCntr + 1].X);

                    geometry.Positions.Add(transform.Transform(point));

                    point = new Point3D(
                        pointsTheta[0].X * pointsPhi[phiCntr + 1].Y,        // wrapping theta back around
                        pointsTheta[0].Y * pointsPhi[phiCntr + 1].Y,
                        capHeight * pointsPhi[phiCntr + 1].X);

                    geometry.Positions.Add(transform.Transform(point));

                    normal = GetNormal(geometry.Positions[zOffset + 0], geometry.Positions[zOffset + 1], geometry.Positions[zOffset + 2]);
                    geometry.Normals.Add(normal);		// the normals point straight out of the face
                    geometry.Normals.Add(normal);
                    geometry.Normals.Add(normal);

                    geometry.TriangleIndices.Add(zOffset + 0);
                    geometry.TriangleIndices.Add(zOffset + 1);
                    geometry.TriangleIndices.Add(zOffset + 2);

                    #endregion

                    zOffset += 3;

                    #region Bottom/Right triangle

                    point = new Point3D(
                        pointsTheta[pointsTheta.Length - 1].X * pointsPhi[phiCntr].Y,
                        pointsTheta[pointsTheta.Length - 1].Y * pointsPhi[phiCntr].Y,
                        capHeight * pointsPhi[phiCntr].X);

                    geometry.Positions.Add(transform.Transform(point));

                    point = new Point3D(
                        pointsTheta[0].X * pointsPhi[phiCntr + 1].Y,
                        pointsTheta[0].Y * pointsPhi[phiCntr + 1].Y,
                        capHeight * pointsPhi[phiCntr + 1].X);

                    geometry.Positions.Add(transform.Transform(point));

                    point = new Point3D(
                        pointsTheta[0].X * pointsPhi[phiCntr].Y,
                        pointsTheta[0].Y * pointsPhi[phiCntr].Y,
                        capHeight * pointsPhi[phiCntr].X);

                    geometry.Positions.Add(transform.Transform(point));

                    normal = GetNormal(geometry.Positions[zOffset + 0], geometry.Positions[zOffset + 1], geometry.Positions[zOffset + 2]);
                    geometry.Normals.Add(normal);		// the normals point straight out of the face
                    geometry.Normals.Add(normal);
                    geometry.Normals.Add(normal);

                    geometry.TriangleIndices.Add(zOffset + 0);
                    geometry.TriangleIndices.Add(zOffset + 1);
                    geometry.TriangleIndices.Add(zOffset + 2);

                    #endregion

                    zOffset += 3;
                }

                #endregion
                #region Triangles - Cap

                // This is basically the same idea as EndCap_ConeHard, except for the extra phi bits

                Point3D topPoint = transform.Transform(new Point3D(0, 0, capHeight));

                for (int thetaCntr = 0; thetaCntr < pointsTheta.Length - 1; thetaCntr++)
                {
                    point = new Point3D(
                        pointsTheta[thetaCntr].X * pointsPhi[1].Y,
                        pointsTheta[thetaCntr].Y * pointsPhi[1].Y,
                        capHeight * pointsPhi[1].X);

                    geometry.Positions.Add(transform.Transform(point));

                    point = new Point3D(
                        pointsTheta[thetaCntr + 1].X * pointsPhi[1].Y,
                        pointsTheta[thetaCntr + 1].Y * pointsPhi[1].Y,
                        capHeight * pointsPhi[1].X);

                    geometry.Positions.Add(transform.Transform(point));

                    geometry.Positions.Add(topPoint);

                    normal = GetNormal(geometry.Positions[zOffset + 0], geometry.Positions[zOffset + 1], geometry.Positions[zOffset + 2]);
                    geometry.Normals.Add(normal);		// the normals point straight out of the face
                    geometry.Normals.Add(normal);
                    geometry.Normals.Add(normal);

                    geometry.TriangleIndices.Add(zOffset + 0);
                    geometry.TriangleIndices.Add(zOffset + 1);
                    geometry.TriangleIndices.Add(zOffset + 2);

                    zOffset += 3;
                }

                // The last triangle links back to zero
                point = new Point3D(
                    pointsTheta[pointsTheta.Length - 1].X * pointsPhi[1].Y,
                    pointsTheta[pointsTheta.Length - 1].Y * pointsPhi[1].Y,
                    capHeight * pointsPhi[1].X);

                geometry.Positions.Add(transform.Transform(point));

                point = new Point3D(
                    pointsTheta[0].X * pointsPhi[1].Y,
                    pointsTheta[0].Y * pointsPhi[1].Y,
                    capHeight * pointsPhi[1].X);

                geometry.Positions.Add(transform.Transform(point));

                geometry.Positions.Add(topPoint);

                normal = GetNormal(geometry.Positions[zOffset + 0], geometry.Positions[zOffset + 1], geometry.Positions[zOffset + 2]);
                geometry.Normals.Add(normal);		// the normals point straight out of the face
                geometry.Normals.Add(normal);
                geometry.Normals.Add(normal);

                geometry.TriangleIndices.Add(zOffset + 0);
                geometry.TriangleIndices.Add(zOffset + 1);
                geometry.TriangleIndices.Add(zOffset + 2);

                zOffset += 3;

                #endregion

                pointOffset = geometry.Positions.Count;
            }

            private static void EndCap_ConeSoft(ref int pointOffset, ref double[] rotateAnglesForPerp, MeshGeometry3D geometry, Point[] pointsTheta, Transform3D transform, Transform3D normalTransform, TubeRingPoint ring, double capHeight, bool isFirst)
            {
                #region Positions/Normals

                if (isFirst || !ring.MergeNormalWithPrevIfSoft)
                {
                    for (int thetaCntr = 0; thetaCntr < pointsTheta.Length; thetaCntr++)
                    {
                        Point3D point = new Point3D(pointsTheta[thetaCntr].X, pointsTheta[thetaCntr].Y, 0d);
                        geometry.Positions.Add(transform.Transform(point));
                        geometry.Normals.Add(normalTransform.Transform(point).ToVector().ToUnit());		// the normal is the same as the point for a sphere (but no tranlate transform)
                    }
                }

                // Cone tip
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

                // The last triangle links back to zero
                geometry.TriangleIndices.Add(pointOffset + pointsTheta.Length - 1 + 0);
                geometry.TriangleIndices.Add(pointOffset + 0);
                geometry.TriangleIndices.Add(topIndex);

                #endregion

                pointOffset = geometry.Positions.Count;
            }
            private static void EndCap_ConeHard(ref int pointOffset, ref double[] rotateAnglesForPerp, MeshGeometry3D geometry, Point[] pointsTheta, Transform3D transform, TubeRingPoint ring, double capHeight, bool isFirst)
            {
                Point3D tipPosition = transform.Transform(new Point3D(0, 0, capHeight));

                int localOffset = 0;

                for (int cntr = 0; cntr < pointsTheta.Length - 1; cntr++)
                {
                    geometry.Positions.Add(transform.Transform(new Point3D(pointsTheta[cntr].X, pointsTheta[cntr].Y, 0d)));
                    geometry.Positions.Add(transform.Transform(new Point3D(pointsTheta[cntr + 1].X, pointsTheta[cntr + 1].Y, 0d)));
                    geometry.Positions.Add(tipPosition);

                    Vector3D normal = GetNormal(geometry.Positions[pointOffset + localOffset + 0], geometry.Positions[pointOffset + localOffset + 1], geometry.Positions[pointOffset + localOffset + 2]);
                    geometry.Normals.Add(normal);		// the normals point straight out of the face
                    geometry.Normals.Add(normal);
                    geometry.Normals.Add(normal);

                    geometry.TriangleIndices.Add(pointOffset + localOffset + 0);
                    geometry.TriangleIndices.Add(pointOffset + localOffset + 1);
                    geometry.TriangleIndices.Add(pointOffset + localOffset + 2);

                    localOffset += 3;
                }

                // The last triangle links back to zero
                geometry.Positions.Add(transform.Transform(new Point3D(pointsTheta[pointsTheta.Length - 1].X, pointsTheta[pointsTheta.Length - 1].Y, 0d)));
                geometry.Positions.Add(transform.Transform(new Point3D(pointsTheta[0].X, pointsTheta[0].Y, 0d)));
                geometry.Positions.Add(tipPosition);

                Vector3D normal2 = GetNormal(geometry.Positions[pointOffset + localOffset + 0], geometry.Positions[pointOffset + localOffset + 1], geometry.Positions[pointOffset + localOffset + 2]);
                geometry.Normals.Add(normal2);		// the normals point straight out of the face
                geometry.Normals.Add(normal2);
                geometry.Normals.Add(normal2);

                geometry.TriangleIndices.Add(pointOffset + localOffset + 0);
                geometry.TriangleIndices.Add(pointOffset + localOffset + 1);
                geometry.TriangleIndices.Add(pointOffset + localOffset + 2);

                // Update ref param
                pointOffset = geometry.Positions.Count;
            }

            private static void EndCap_PlateSoft(ref int pointOffset, ref double[] rotateAnglesForPerp, MeshGeometry3D geometry, Point[] pointsTheta, Transform3D transform, Transform3D normalTransform, TubeRingBase ring, bool isFirst)
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
                            //normal = point.ToVector();		// this isn't right
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
            private static void EndCap_PlateHard(ref int pointOffset, ref double[] rotateAnglesForPerp, MeshGeometry3D geometry, Point[] pointsTheta, Transform3D transform, Transform3D normalTransform, TubeRingBase ring, bool isFirst)
            {
                Vector3D normal = normalTransform.Transform(new Vector3D(0, 0, 1)).ToUnit();

                // Start with 0,1,2
                geometry.Positions.Add(transform.Transform(new Point3D(pointsTheta[0].X, pointsTheta[0].Y, 0d)));
                geometry.Positions.Add(transform.Transform(new Point3D(pointsTheta[1].X, pointsTheta[1].Y, 0d)));
                geometry.Positions.Add(transform.Transform(new Point3D(pointsTheta[2].X, pointsTheta[2].Y, 0d)));

                geometry.Normals.Add(normal);
                geometry.Normals.Add(normal);
                geometry.Normals.Add(normal);

                geometry.TriangleIndices.Add(pointOffset + 0);
                geometry.TriangleIndices.Add(pointOffset + 1);
                geometry.TriangleIndices.Add(pointOffset + 2);

                int lowerIndex = 2;
                int upperIndex = pointsTheta.Length - 1;
                int lastUsedIndex = 0;
                bool shouldBumpLower = true;

                int localOffset = 3;

                // Do the rest of the triangles
                while (lowerIndex < upperIndex)
                {
                    geometry.Positions.Add(transform.Transform(new Point3D(pointsTheta[lowerIndex].X, pointsTheta[lowerIndex].Y, 0d)));
                    geometry.Positions.Add(transform.Transform(new Point3D(pointsTheta[upperIndex].X, pointsTheta[upperIndex].Y, 0d)));
                    geometry.Positions.Add(transform.Transform(new Point3D(pointsTheta[lastUsedIndex].X, pointsTheta[lastUsedIndex].Y, 0d)));

                    geometry.Normals.Add(normal);
                    geometry.Normals.Add(normal);
                    geometry.Normals.Add(normal);

                    geometry.TriangleIndices.Add(pointOffset + localOffset + 0);
                    geometry.TriangleIndices.Add(pointOffset + localOffset + 1);
                    geometry.TriangleIndices.Add(pointOffset + localOffset + 2);

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

                    localOffset += 3;
                }

                // Update ref param
                pointOffset = geometry.Positions.Count;
            }

            private static void Middle(ref int pointOffset, ref double[] rotateAnglesForPerp, MeshGeometry3D geometry, Transform3D transform, int numSides, TubeRingBase ring1, TubeRingBase ring2, double curZ, bool softSides)
            {
                if (ring1 is TubeRingRegularPolygon && ring2 is TubeRingRegularPolygon)
                {
                    #region Tube

                    if (softSides)
                    {
                        Middle_TubeSoft(ref pointOffset, ref rotateAnglesForPerp, geometry, transform, numSides, (TubeRingRegularPolygon)ring1, (TubeRingRegularPolygon)ring2, curZ);
                    }
                    else
                    {
                        Middle_TubeHard(ref pointOffset, ref rotateAnglesForPerp, geometry, transform, numSides, (TubeRingRegularPolygon)ring1, (TubeRingRegularPolygon)ring2, curZ);
                    }

                    #endregion
                }
                else
                {
                    // There are no other combos that need to show a visual right now (eventually, I'll have a definition for
                    // a non regular polygon - low in fiber)
                }
            }

            private static void Middle_TubeSoft(ref int pointOffset, ref double[] rotateAnglesForPerp, MeshGeometry3D geometry, Transform3D transform, int numSides, TubeRingRegularPolygon ring1, TubeRingRegularPolygon ring2, double curZ)
            {
                if (ring1.MergeNormalWithPrevIfSoft || ring2.MergeNormalWithPrevIfSoft)
                {
                    throw new ApplicationException("finish this");
                }

                Point[] points = Math2D.GetCircle_Cached(numSides);

                #region Points/Normals

                //TODO: Don't add the bottom ring's points, only the top

                // Ring 1
                for (int cntr = 0; cntr < numSides; cntr++)
                {
                    geometry.Positions.Add(transform.Transform(new Point3D(points[cntr].X * ring1.RadiusX, points[cntr].Y * ring1.RadiusY, curZ)));
                    geometry.Normals.Add(transform.Transform(new Vector3D(points[cntr].X * ring1.RadiusX, points[cntr].Y * ring1.RadiusY, 0d).ToUnit()));		// the normals point straight out of the side
                }

                // Ring 2
                for (int cntr = 0; cntr < numSides; cntr++)
                {
                    geometry.Positions.Add(transform.Transform(new Point3D(points[cntr].X * ring2.RadiusX, points[cntr].Y * ring2.RadiusY, curZ + ring2.DistFromPrevRing)));
                    geometry.Normals.Add(transform.Transform(new Vector3D(points[cntr].X * ring2.RadiusX, points[cntr].Y * ring2.RadiusY, 0d).ToUnit()));		// the normals point straight out of the side
                }

                #endregion

                #region Triangles

                int zOffsetBottom = pointOffset;
                int zOffsetTop = zOffsetBottom + numSides;

                for (int cntr = 0; cntr < numSides - 1; cntr++)
                {
                    // Top/Left triangle
                    geometry.TriangleIndices.Add(zOffsetBottom + cntr + 0);
                    geometry.TriangleIndices.Add(zOffsetTop + cntr + 1);
                    geometry.TriangleIndices.Add(zOffsetTop + cntr + 0);

                    // Bottom/Right triangle
                    geometry.TriangleIndices.Add(zOffsetBottom + cntr + 0);
                    geometry.TriangleIndices.Add(zOffsetBottom + cntr + 1);
                    geometry.TriangleIndices.Add(zOffsetTop + cntr + 1);
                }

                // Connecting the last 2 points to the first 2
                // Top/Left triangle
                geometry.TriangleIndices.Add(zOffsetBottom + (numSides - 1) + 0);
                geometry.TriangleIndices.Add(zOffsetTop);		// wrapping back around
                geometry.TriangleIndices.Add(zOffsetTop + (numSides - 1) + 0);

                // Bottom/Right triangle
                geometry.TriangleIndices.Add(zOffsetBottom + (numSides - 1) + 0);
                geometry.TriangleIndices.Add(zOffsetBottom);
                geometry.TriangleIndices.Add(zOffsetTop);

                #endregion

                pointOffset = geometry.Positions.Count;
            }
            private static void Middle_TubeHard(ref int pointOffset, ref double[] rotateAnglesForPerp, MeshGeometry3D geometry, Transform3D transform, int numSides, TubeRingRegularPolygon ring1, TubeRingRegularPolygon ring2, double curZ)
            {
                Point[] points = Math2D.GetCircle_Cached(numSides);

                int zOffset = pointOffset;

                for (int cntr = 0; cntr < numSides - 1; cntr++)
                {
                    // Top/Left triangle (each triangle gets its own 3 points)
                    geometry.Positions.Add(transform.Transform(new Point3D(points[cntr].X * ring1.RadiusX, points[cntr].Y * ring1.RadiusY, curZ)));
                    geometry.Positions.Add(transform.Transform(new Point3D(points[cntr + 1].X * ring2.RadiusX, points[cntr + 1].Y * ring2.RadiusY, curZ + ring2.DistFromPrevRing)));
                    geometry.Positions.Add(transform.Transform(new Point3D(points[cntr].X * ring2.RadiusX, points[cntr].Y * ring2.RadiusY, curZ + ring2.DistFromPrevRing)));

                    Vector3D normal = GetNormal(geometry.Positions[zOffset + 0], geometry.Positions[zOffset + 1], geometry.Positions[zOffset + 2]);
                    geometry.Normals.Add(normal);		// the normals point straight out of the face
                    geometry.Normals.Add(normal);
                    geometry.Normals.Add(normal);

                    geometry.TriangleIndices.Add(zOffset + 0);
                    geometry.TriangleIndices.Add(zOffset + 1);
                    geometry.TriangleIndices.Add(zOffset + 2);

                    zOffset += 3;

                    // Bottom/Right triangle
                    geometry.Positions.Add(transform.Transform(new Point3D(points[cntr].X * ring1.RadiusX, points[cntr].Y * ring1.RadiusY, curZ)));
                    geometry.Positions.Add(transform.Transform(new Point3D(points[cntr + 1].X * ring1.RadiusX, points[cntr + 1].Y * ring1.RadiusY, curZ)));
                    geometry.Positions.Add(transform.Transform(new Point3D(points[cntr + 1].X * ring2.RadiusX, points[cntr + 1].Y * ring2.RadiusY, curZ + ring2.DistFromPrevRing)));

                    normal = GetNormal(geometry.Positions[zOffset + 0], geometry.Positions[zOffset + 1], geometry.Positions[zOffset + 2]);
                    geometry.Normals.Add(normal);		// the normals point straight out of the face
                    geometry.Normals.Add(normal);
                    geometry.Normals.Add(normal);

                    geometry.TriangleIndices.Add(zOffset + 0);
                    geometry.TriangleIndices.Add(zOffset + 1);
                    geometry.TriangleIndices.Add(zOffset + 2);

                    zOffset += 3;
                }

                // Connecting the last 2 points to the first 2

                // Top/Left triangle
                geometry.Positions.Add(transform.Transform(new Point3D(points[numSides - 1].X * ring1.RadiusX, points[numSides - 1].Y * ring1.RadiusY, curZ)));
                geometry.Positions.Add(transform.Transform(new Point3D(points[0].X * ring2.RadiusX, points[0].Y * ring2.RadiusY, curZ + ring2.DistFromPrevRing)));
                geometry.Positions.Add(transform.Transform(new Point3D(points[numSides - 1].X * ring2.RadiusX, points[numSides - 1].Y * ring2.RadiusY, curZ + ring2.DistFromPrevRing)));

                Vector3D normal2 = GetNormal(geometry.Positions[zOffset + 0], geometry.Positions[zOffset + 1], geometry.Positions[zOffset + 2]);
                geometry.Normals.Add(normal2);		// the normals point straight out of the face
                geometry.Normals.Add(normal2);
                geometry.Normals.Add(normal2);

                geometry.TriangleIndices.Add(zOffset + 0);
                geometry.TriangleIndices.Add(zOffset + 1);
                geometry.TriangleIndices.Add(zOffset + 2);

                zOffset += 3;

                // Bottom/Right triangle
                geometry.Positions.Add(transform.Transform(new Point3D(points[numSides - 1].X * ring1.RadiusX, points[numSides - 1].Y * ring1.RadiusY, curZ)));
                geometry.Positions.Add(transform.Transform(new Point3D(points[0].X * ring1.RadiusX, points[0].Y * ring1.RadiusY, curZ)));
                geometry.Positions.Add(transform.Transform(new Point3D(points[0].X * ring2.RadiusX, points[0].Y * ring2.RadiusY, curZ + ring2.DistFromPrevRing)));

                normal2 = GetNormal(geometry.Positions[zOffset + 0], geometry.Positions[zOffset + 1], geometry.Positions[zOffset + 2]);
                geometry.Normals.Add(normal2);		// the normals point straight out of the face
                geometry.Normals.Add(normal2);
                geometry.Normals.Add(normal2);

                geometry.TriangleIndices.Add(zOffset + 0);
                geometry.TriangleIndices.Add(zOffset + 1);
                geometry.TriangleIndices.Add(zOffset + 2);

                // Update ref param
                pointOffset = geometry.Positions.Count;
            }

            //TODO: See if normals should always be unit vectors or not
            private static Vector3D GetNormal(Point3D point0, Point3D point1, Point3D point2)
            {
                Vector3D dir1 = point0 - point1;
                Vector3D dir2 = point2 - point1;

                return Vector3D.CrossProduct(dir2, dir1).ToUnit();
            }

            #endregion
        }

        #endregion

        #region Declaration Section

        private const double INV256 = 1d / 256d;

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

        #region Color

        /// <summary>
        /// This returns a color that is the result of the two colors blended
        /// </summary>
        /// <remarks>
        /// NOTE: This is sort of a mashup (AlphaBlend was written years before OverlayColors and AverageColors)
        /// </remarks>
        /// <param name="alpha">0 is all back color, 1 is all fore color, .5 is half way between</param>
        public static Color AlphaBlend(Color foreColor, Color backColor, double alpha)
        {
            // Figure out the new color
            double a, r, g, b;
            if (foreColor.A == 0)
            {
                // Fore is completely transparent, so only worry about blending the alpha
                a = backColor.A + (((foreColor.A - backColor.A) * INV256) * alpha * 255d);
                r = backColor.R;
                g = backColor.G;
                b = backColor.B;
            }
            else if (backColor.A == 0)
            {
                // Back is completely transparent, so only worry about blending the alpha
                a = backColor.A + (((foreColor.A - backColor.A) * INV256) * alpha * 255d);
                r = foreColor.R;
                g = foreColor.G;
                b = foreColor.B;
            }
            else
            {
                a = backColor.A + (((foreColor.A - backColor.A) * INV256) * alpha * 255d);
                r = backColor.R + (((foreColor.R - backColor.R) * INV256) * alpha * 255d);
                g = backColor.G + (((foreColor.G - backColor.G) * INV256) * alpha * 255d);
                b = backColor.B + (((foreColor.B - backColor.B) * INV256) * alpha * 255d);
            }

            //  Exit Function
            return GetColorCapped(a, r, g, b);
        }
        /// <summary>
        /// Profiling shows that creating a color is 6.5 times slower than a byte array
        /// </summary>
        /// <remarks>
        /// Code is copied for speed reasons
        /// </remarks>
        public static byte[] AlphaBlend(byte[] foreColor, byte[] backColor, double alpha)
        {
            if (backColor.Length == 4)
            {
                #region ARGB

                // Figure out the new color
                if (foreColor[0] == 0)
                {
                    // Fore is completely transparent, so only worry about blending the alpha
                    return new byte[]
                        {
                            GetByteCapped(backColor[0] + (((foreColor[0] - backColor[0]) * INV256) * alpha * 255d)),
                            backColor[1],
                            backColor[2],
                            backColor[3]
                        };
                }
                else if (backColor[0] == 0)
                {
                    // Back is completely transparent, so only worry about blending the alpha
                    return new byte[]
                        {
                            GetByteCapped(backColor[0] + (((foreColor[0] - backColor[0]) * INV256) * alpha * 255d)),
                            foreColor[1],
                            foreColor[2],
                            foreColor[3]
                        };
                }
                else
                {
                    return new byte[]
                        {
                            GetByteCapped(backColor[0] + (((foreColor[0] - backColor[0]) * INV256) * alpha * 255d)),
                            GetByteCapped(backColor[1] + (((foreColor[1] - backColor[1]) * INV256) * alpha * 255d)),
                            GetByteCapped(backColor[2] + (((foreColor[2] - backColor[2]) * INV256) * alpha * 255d)),
                            GetByteCapped(backColor[3] + (((foreColor[3] - backColor[3]) * INV256) * alpha * 255d))
                        };
                }

                #endregion
            }
            else
            {
                #region RGB

                return new byte[]
                    {
                        GetByteCapped(backColor[0] + (((foreColor[0] - backColor[0]) * INV256) * alpha * 255d)),
                        GetByteCapped(backColor[1] + (((foreColor[1] - backColor[1]) * INV256) * alpha * 255d)),
                        GetByteCapped(backColor[2] + (((foreColor[2] - backColor[2]) * INV256) * alpha * 255d))
                    };

                #endregion
            }
        }

        /// <summary>
        /// This lays the colors on top of each other, and returns the result
        /// </summary>
        /// <remarks>
        /// This treats each color like a plate of glass.  So setting a fully opaque plate halfway up the stack will completely block everything
        /// under it.  I tested this in wpf by placing rectangles on top of each other, and got the same color that wpf got.
        /// 
        /// This is a simplified copy of the other overload.  The code was copied for speed reasons
        /// </remarks>
        public static Color OverlayColors(IEnumerable<Color> colors)
        {
            bool isFirst = true;

            //  This represents the running return color (values from 0 to 1)
            double a = 0, r = 0, g = 0, b = 0;

            //  Shoot through the colors, and lay them on top of the running return color
            foreach (var color in colors)
            {
                if (color.A == 0)
                {
                    //  Ignore transparent colors
                    continue;
                }

                if (isFirst)
                {
                    //  Store the first color
                    a = color.A * INV256;
                    r = color.R * INV256;
                    g = color.G * INV256;
                    b = color.B * INV256;

                    isFirst = false;
                    continue;
                }

                double a2 = color.A * INV256;
                double r2 = color.R * INV256;
                double g2 = color.G * INV256;
                double b2 = color.B * INV256;

                //  Alpha is a bit funny, it's a control more than a color
                a = Math.Max(a, a2);

                //  Add the weighted difference between this color and the running color
                r += (r2 - r) * a2;
                g += (g2 - g) * a2;
                b += (b2 - b) * a2;
            }

            if (isFirst)
            {
                //  The list was empty, or all the colors were transparent
                return Colors.Transparent;
            }

            //  Exit Function
            return GetColorCapped(a * 256d, r * 256d, g * 256d, b * 256d);
        }
        /// <summary>
        /// This overload adds an extra dial for transparency (the same result could be achieved by pre multiplying each color by its
        /// corresponding percent)
        /// </summary>
        /// <param name="colors">
        /// Item1=The color
        /// Item2=Percent (0 to 1)
        /// </param>
        public static Color OverlayColors(IEnumerable<Tuple<Color, double>> colors)
        {
            const double INV255 = 1d / 255d;

            bool isFirst = true;

            //  This represents the running return color (values from 0 to 1)
            double a = 0, r = 0, g = 0, b = 0;

            //  Shoot through the colors, and lay them on top of the running return color
            foreach (var color in colors)
            {
                if (color.Item1.A == 0 || color.Item2 == 0d)
                {
                    //  Ignore transparent colors
                    continue;
                }

                if (isFirst)
                {
                    //  Store the first color
                    a = (color.Item1.A * INV255) * color.Item2;
                    r = color.Item1.R * INV255;
                    g = color.Item1.G * INV255;
                    b = color.Item1.B * INV255;

                    isFirst = false;
                    continue;
                }

                double a2 = (color.Item1.A * INV255) * color.Item2;
                double r2 = color.Item1.R * INV255;
                double g2 = color.Item1.G * INV255;
                double b2 = color.Item1.B * INV255;

                //  Alpha is a bit funny, it's a control more than a color
                a = Math.Max(a, a2);

                //  Add the weighted difference between this color and the running color
                r += (r2 - r) * a2;
                g += (g2 - g) * a2;
                b += (b2 - b) * a2;
            }

            if (isFirst)
            {
                //  The list was empty, or all the colors were transparent
                return Colors.Transparent;
            }

            //  Exit Function
            return GetColorCapped(a * 255d, r * 255d, g * 255d, b * 255d);
        }
        /// <summary>
        /// Made an overload for byte array, because byte arrays are faster than the color struct (6.5 times faster)
        /// </summary>
        public static byte[] OverlayColors(IEnumerable<byte[]> colors)
        {
            bool isFirst = true;

            //  This represents the running return color (values from 0 to 1)
            double a = 0, r = 0, g = 0, b = 0;

            //  Shoot through the colors, and lay them on top of the running return color
            foreach (var color in colors)
            {
                if (color[0] == 0)
                {
                    //  Ignore transparent colors
                    continue;
                }

                if (isFirst)
                {
                    //  Store the first color
                    a = color[0] * INV256;
                    r = color[1] * INV256;
                    g = color[2] * INV256;
                    b = color[3] * INV256;

                    isFirst = false;
                    continue;
                }

                double a2 = color[0] * INV256;
                double r2 = color[1] * INV256;
                double g2 = color[2] * INV256;
                double b2 = color[3] * INV256;

                //  Alpha is a bit funny, it's a control more than a color
                a = Math.Max(a, a2);

                //  Add the weighted difference between this color and the running color
                r += (r2 - r) * a2;
                g += (g2 - g) * a2;
                b += (b2 - b) * a2;
            }

            if (isFirst)
            {
                //  The list was empty, or all the colors were transparent
                return new byte[] { 0, 0, 0, 0 };
            }

            //  Exit Function
            return new byte[]
            {
                GetByteCapped(a * 256d),
                GetByteCapped(r * 256d),
                GetByteCapped(g * 256d),
                GetByteCapped(b * 256d)
            };
        }

        /// <summary>
        /// This takes the weighted average of all the colors (using alpha as the weight multiplier)
        /// </summary>
        /// <remarks>
        /// This is a simplified copy of the other overload.  The code was copied for speed reasons
        /// </remarks>
        public static Color AverageColors(IEnumerable<Color> colors)
        {
            const double INV255 = 1d / 255d;
            const double NEARZERO = .001d;

            #region Convert to doubles

            List<Tuple<double, double, double, double>> doubles = new List<Tuple<double, double, double, double>>();

            double minAlpha = double.MaxValue;
            bool isAllTransparent = true;

            //  Convert to doubles from 0 to 1 (throw out fully transparent colors)
            foreach (var color in colors)
            {
                double a = color.A * INV255;

                doubles.Add(Tuple.Create(a, color.R * INV255, color.G * INV255, color.B * INV255));

                if (a > NEARZERO && a < minAlpha)
                {
                    isAllTransparent = false;
                    minAlpha = a;
                }
            }

            #endregion

            if (isAllTransparent)
            {
                return Colors.Transparent;
            }

            #region Weighted sum

            double sumA = 0, sumR = 0, sumG = 0, sumB = 0;
            double sumWeight = 0;

            foreach (var dbl in doubles)
            {
                if (dbl.Item1 <= NEARZERO)
                {
                    //  This is fully transparent.  It doesn't affect sumWeight, but the final alpha is divided by doubles.Count, so it does affect the final alpha (sumWeight
                    //  affects the color, not alpha)
                    continue;
                }

                double multiplier = dbl.Item1 / minAlpha;       //  dividing by min so that multiplier is always greater or equal to 1
                sumWeight += multiplier;

                sumA += dbl.Item1;      //  this one isn't weighted, it's a simple average
                sumR += dbl.Item2 * multiplier;
                sumG += dbl.Item3 * multiplier;
                sumB += dbl.Item4 * multiplier;
            }

            double divisor = 1d / sumWeight;

            #endregion

            //  Exit Function
            return GetColorCapped((sumA / doubles.Count) * 255d, sumR * divisor * 255d, sumG * divisor * 255d, sumB * divisor * 255d);
        }
        /// <summary>
        /// This takes the weighted average of all the colors (using alpha as the weight multiplier)
        /// </summary>
        /// <param name="colors">
        /// Item1=Color
        /// Item2=% (0 to 1)
        /// </param>
        /// <remarks>
        /// The overlay method is what you would normally think of when alpha blending.  This method doesn't care about
        /// the order of the colors, it just averages them (so if several colors are passed in that are different hues, you'll just get gray)
        /// 
        /// http://www.investopedia.com/terms/w/weightedaverage.asp
        /// </remarks>
        public static Color AverageColors(IEnumerable<Tuple<Color, double>> colors)
        {
            const double INV255 = 1d / 255d;
            const double NEARZERO = .001d;

            #region Convert to doubles

            //  A, R, G, B, %
            List<Tuple<double, double, double, double, double>> doubles = new List<Tuple<double, double, double, double, double>>();        //  I hate using such an ugly tuple, but the alternative is linq and anonymous types, and this method needs to be as fast as possible

            double minAlpha = double.MaxValue;
            bool isAllTransparent = true;

            //  Convert to doubles from 0 to 1 (throw out fully transparent colors)
            foreach (var color in colors)
            {
                double a = (color.Item1.A * INV255);
                double a1 = a * color.Item2;

                doubles.Add(Tuple.Create(a, color.Item1.R * INV255, color.Item1.G * INV255, color.Item1.B * INV255, color.Item2));

                if (a1 > NEARZERO && a1 < minAlpha)
                {
                    isAllTransparent = false;
                    minAlpha = a1;
                }
            }

            #endregion

            if (isAllTransparent)
            {
                return Colors.Transparent;
            }

            #region Weighted sum

            double sumA = 0, sumR = 0, sumG = 0, sumB = 0;
            double sumAlphaWeight = 0, sumWeight = 0;

            foreach (var dbl in doubles)
            {
                sumAlphaWeight += dbl.Item5;        //  Item5 should already be from 0 to 1

                if ((dbl.Item1 * dbl.Item5) <= NEARZERO)
                {
                    //  This is fully transparent.  It doesn't affect the sum of the color's weight, but does affect the sum of the alpha's weight
                    continue;
                }

                double multiplier = (dbl.Item1 * dbl.Item5) / minAlpha;       //  dividing by min so that multiplier is always greater or equal to 1
                sumWeight += multiplier;

                sumA += dbl.Item1;      //  alphas have their own weighting
                sumR += dbl.Item2 * multiplier;
                sumG += dbl.Item3 * multiplier;
                sumB += dbl.Item4 * multiplier;
            }

            double divisor = 1d / sumWeight;

            #endregion

            //  Exit Function
            return GetColorCapped((sumA / sumAlphaWeight) * 255d, sumR * divisor * 255d, sumG * divisor * 255d, sumB * divisor * 255d);
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

        public static Color GetColorEGA(int number)
        {
            return GetColorEGA(255, number);
        }
        public static Color GetColorEGA(byte alpha, int number)
        {
            //http://en.wikipedia.org/wiki/Enhanced_Graphics_Adapter

            switch (number)
            {
                case 0:
                    return Color.FromArgb(alpha, 0, 0, 0);      // black

                case 1:
                    return Color.FromArgb(alpha, 0, 0, 170);        // blue

                case 2:
                    return Color.FromArgb(alpha, 0, 170, 0);        // green

                case 3:
                    return Color.FromArgb(alpha, 0, 170, 170);      // cyan

                case 4:
                    return Color.FromArgb(alpha, 170, 0, 0);        // red

                case 5:
                    return Color.FromArgb(alpha, 170, 0, 170);      // magenta

                case 6:
                    return Color.FromArgb(alpha, 170, 85, 0);       // brown

                case 7:
                    return Color.FromArgb(alpha, 170, 170, 170);      // light gray

                case 8:
                    return Color.FromArgb(alpha, 85, 85, 85);      // dark gray

                case 9:
                    return Color.FromArgb(alpha, 85, 85, 255);      // bright blue

                case 10:
                    return Color.FromArgb(alpha, 85, 255, 85);      // bright green

                case 11:
                    return Color.FromArgb(alpha, 85, 255, 255);     // bright cyan

                case 12:
                    return Color.FromArgb(alpha, 255, 85, 85);      // bright red

                case 13:
                    return Color.FromArgb(alpha, 255, 85, 255);     // bright magenta

                case 14:
                    return Color.FromArgb(alpha, 255, 255, 85);     // bright yellow

                case 15:
                    return Color.FromArgb(alpha, 255, 255, 255);        // bright white

                default:
                    throw new ArgumentException("The number must be between 0 and 15: " + number.ToString());
            }
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
            // I think color.ToString does the same thing, but this is explicit
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
        public static ColorHSV RGBtoHSV(Color color)
        {
            // Normalize the RGB values by scaling them to be between 0 and 1
            double red = color.R / 255d;
            double green = color.G / 255d;
            double blue = color.B / 255d;

            double minValue = Math.Min(red, Math.Min(green, blue));
            double maxValue = Math.Max(red, Math.Max(green, blue));		// TODO: don't use math.max, then switch.  Instead, do the if statements manually
            double delta = maxValue - minValue;

            double h, s, v;

            v = maxValue;

            #region Get Hue

            // Calculate the hue (in degrees of a circle, between 0 and 360)
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

            // Calculate the saturation (between 0 and 1)
            if (maxValue == 0d)
            {
                s = 0d;
            }
            else
            {
                s = 1d - (minValue / maxValue);
            }

            // Scale the saturation and value to a percentage between 0 and 100
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

            return new ColorHSV(color.A, h, s, v);
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
            // Scale the Saturation and Value components to be between 0 and 1
            double hue = h;
            double sat = s / 100d;
            double val = v / 100d;

            double r, g, b;		// these go between 0 and 1

            if (sat == 0d)
            {
                #region Gray

                // If the saturation is 0, then all colors are the same.
                // (This is some flavor of gray.)
                r = val;
                g = val;
                b = val;

                #endregion
            }
            else
            {
                #region Color

                // Calculate the appropriate sector of a 6-part color wheel
                double sectorPos = hue / 60d;
                int sectorNumber = Convert.ToInt32(Math.Floor(sectorPos));

                // Get the fractional part of the sector (that is, how many degrees into the sector you are)
                double fractionalSector = sectorPos - sectorNumber;

                // Calculate values for the three axes of the color
                double p = val * (1d - sat);
                double q = val * (1d - (sat * fractionalSector));
                double t = val * (1d - (sat * (1d - fractionalSector)));

                // Assign the fractional colors to red, green, and blue
                // components based on the sector the angle is in
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

            // Scale to 255 (using int to make it easier to handle overflow)
            int rNew = Convert.ToInt32(Math.Round(r * 255d));
            int gNew = Convert.ToInt32(Math.Round(g * 255d));
            int bNew = Convert.ToInt32(Math.Round(b * 255d));

            // Make sure the values are in range
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

            // Exit Function
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

                return !brushCast.GradientStops.Any(o => !IsTransparent(o.Color));		// if any are non-transparent, return false
            }

            // Not sure what it is, probably a bitmap or something, so just assume it's not transparent
            return false;
        }

        #endregion

        #region 3D Geometry

        /// <summary>
        /// ScreenSpaceLines3D makes a line that is the same thickness regardless of zoom/perspective.  This returns two bars in a cross
        /// pattern to make sort of a line.  This isn't meant to look very realistic when viewed up close, but is meant to be cheap
        /// </summary>
        public static MeshGeometry3D GetLine(Point3D from, Point3D to, double thickness)
        {
            double half = thickness / 2d;

            Vector3D line = to - from;
            if (line.X == 0 && line.Y == 0 && line.Z == 0) line.X = 0.000000001d;

            Vector3D orth1 = Math3D.GetArbitraryOrhonganal(line);
            orth1 = Math3D.RotateAroundAxis(orth1, line, StaticRandom.NextDouble() * Math.PI * 2d);		// give it a random rotation so that if many lines are created by this method, they won't all be oriented the same
            orth1 = orth1.ToUnit() * half;

            Vector3D orth2 = Vector3D.CrossProduct(line, orth1);
            orth2 = orth2.ToUnit() * half;

            // Define 3D mesh object
            MeshGeometry3D retVal = new MeshGeometry3D();

            // Plate 1
            retVal.Positions.Add(from + orth1);
            retVal.Positions.Add(to + orth1);
            retVal.Positions.Add(from - orth1);
            retVal.Positions.Add(to - orth1);

            // Plate 2
            retVal.Positions.Add(from + orth2);
            retVal.Positions.Add(to + orth2);
            retVal.Positions.Add(from - orth2);
            retVal.Positions.Add(to - orth2);

            // Face 1
            retVal.TriangleIndices.Add(0);
            retVal.TriangleIndices.Add(3);
            retVal.TriangleIndices.Add(1);
            retVal.TriangleIndices.Add(0);
            retVal.TriangleIndices.Add(2);
            retVal.TriangleIndices.Add(3);

            // Face 2
            retVal.TriangleIndices.Add(4);
            retVal.TriangleIndices.Add(7);
            retVal.TriangleIndices.Add(5);
            retVal.TriangleIndices.Add(4);
            retVal.TriangleIndices.Add(6);
            retVal.TriangleIndices.Add(7);


            // shouldn't I set normals?
            //retVal.Normals

            // Exit Function
            //retVal.Freeze();
            return retVal;
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

            // shouldn't I set normals?
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

            // shouldn't I set normals?
            //retVal.Normals

            // Exit Function
            //retVal.Freeze();
            return retVal;
        }
        public static MeshGeometry3D GetCube(Point3D min, Point3D max)
        {
            // Define 3D mesh object
            MeshGeometry3D retVal = new MeshGeometry3D();

            retVal.Positions.Add(new Point3D(min.X, min.Y, max.Z));		// 0
            retVal.Positions.Add(new Point3D(max.X, min.Y, max.Z));		// 1
            retVal.Positions.Add(new Point3D(max.X, max.Y, max.Z));		// 2
            retVal.Positions.Add(new Point3D(min.X, max.Y, max.Z));		// 3

            retVal.Positions.Add(new Point3D(min.X, min.Y, min.Z));		// 4
            retVal.Positions.Add(new Point3D(max.X, min.Y, min.Z));		// 5
            retVal.Positions.Add(new Point3D(max.X, max.Y, min.Z));		// 6
            retVal.Positions.Add(new Point3D(min.X, max.Y, min.Z));		// 7

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

            // shouldn't I set normals?
            //retVal.Normals

            // Exit Function
            //retVal.Freeze();
            return retVal;
        }
        public static MeshGeometry3D GetCube_IndependentFaces(Vector3D min, Vector3D max)
        {
            return GetCube_IndependentFaces(min.ToPoint(), max.ToPoint());
        }
        /// <summary>
        /// GetCube shares verticies between faces, so light reflects blended and unnatural (but is a bit more efficient)
        /// This looks a bit better, but has a higher vertex count
        /// </summary>
        public static MeshGeometry3D GetCube_IndependentFaces(Point3D min, Point3D max)
        {
            // Define 3D mesh object
            MeshGeometry3D retVal = new MeshGeometry3D();

            //retVal.Positions.Add(new Point3D(min.X, min.Y, max.Z));		// 0
            //retVal.Positions.Add(new Point3D(max.X, min.Y, max.Z));		// 1
            //retVal.Positions.Add(new Point3D(max.X, max.Y, max.Z));		// 2
            //retVal.Positions.Add(new Point3D(min.X, max.Y, max.Z));		// 3

            //retVal.Positions.Add(new Point3D(min.X, min.Y, min.Z));		// 4
            //retVal.Positions.Add(new Point3D(max.X, min.Y, min.Z));		// 5
            //retVal.Positions.Add(new Point3D(max.X, max.Y, min.Z));		// 6
            //retVal.Positions.Add(new Point3D(min.X, max.Y, min.Z));		// 7

            // Front face
            retVal.Positions.Add(new Point3D(min.X, min.Y, max.Z));		// 0
            retVal.Positions.Add(new Point3D(max.X, min.Y, max.Z));		// 1
            retVal.Positions.Add(new Point3D(max.X, max.Y, max.Z));		// 2
            retVal.Positions.Add(new Point3D(min.X, max.Y, max.Z));		// 3
            retVal.TriangleIndices.Add(0);
            retVal.TriangleIndices.Add(1);
            retVal.TriangleIndices.Add(2);
            retVal.TriangleIndices.Add(2);
            retVal.TriangleIndices.Add(3);
            retVal.TriangleIndices.Add(0);

            // Back face
            retVal.Positions.Add(new Point3D(min.X, min.Y, min.Z));		// 4
            retVal.Positions.Add(new Point3D(max.X, min.Y, min.Z));		// 5
            retVal.Positions.Add(new Point3D(max.X, max.Y, min.Z));		// 6
            retVal.Positions.Add(new Point3D(min.X, max.Y, min.Z));		// 7
            retVal.TriangleIndices.Add(6);
            retVal.TriangleIndices.Add(5);
            retVal.TriangleIndices.Add(4);
            retVal.TriangleIndices.Add(4);
            retVal.TriangleIndices.Add(7);
            retVal.TriangleIndices.Add(6);

            // Right face
            retVal.Positions.Add(new Point3D(max.X, min.Y, max.Z));		// 1-8
            retVal.Positions.Add(new Point3D(max.X, max.Y, max.Z));		// 2-9
            retVal.Positions.Add(new Point3D(max.X, min.Y, min.Z));		// 5-10
            retVal.Positions.Add(new Point3D(max.X, max.Y, min.Z));		// 6-11
            retVal.TriangleIndices.Add(8);		// 1
            retVal.TriangleIndices.Add(10);		// 5
            retVal.TriangleIndices.Add(9);		// 2
            retVal.TriangleIndices.Add(10);		// 5
            retVal.TriangleIndices.Add(11);		// 6
            retVal.TriangleIndices.Add(9);		// 2

            // Top face
            retVal.Positions.Add(new Point3D(max.X, max.Y, max.Z));		// 2-12
            retVal.Positions.Add(new Point3D(min.X, max.Y, max.Z));		// 3-13
            retVal.Positions.Add(new Point3D(max.X, max.Y, min.Z));		// 6-14
            retVal.Positions.Add(new Point3D(min.X, max.Y, min.Z));		// 7-15
            retVal.TriangleIndices.Add(12);		// 2
            retVal.TriangleIndices.Add(14);		// 6
            retVal.TriangleIndices.Add(13);		// 3
            retVal.TriangleIndices.Add(13);		// 3
            retVal.TriangleIndices.Add(14);		// 6
            retVal.TriangleIndices.Add(15);		// 7

            // Bottom face
            retVal.Positions.Add(new Point3D(min.X, min.Y, max.Z));		// 0-16
            retVal.Positions.Add(new Point3D(max.X, min.Y, max.Z));		// 1-17
            retVal.Positions.Add(new Point3D(min.X, min.Y, min.Z));		// 4-18
            retVal.Positions.Add(new Point3D(max.X, min.Y, min.Z));		// 5-19
            retVal.TriangleIndices.Add(19);		// 5
            retVal.TriangleIndices.Add(17);		// 1
            retVal.TriangleIndices.Add(16);		// 0
            retVal.TriangleIndices.Add(16);		// 0
            retVal.TriangleIndices.Add(18);		// 4
            retVal.TriangleIndices.Add(19);		// 5

            // Right face
            retVal.Positions.Add(new Point3D(min.X, min.Y, max.Z));		// 0-20
            retVal.Positions.Add(new Point3D(min.X, max.Y, max.Z));		// 3-21
            retVal.Positions.Add(new Point3D(min.X, min.Y, min.Z));		// 4-22
            retVal.Positions.Add(new Point3D(min.X, max.Y, min.Z));		// 7-23
            retVal.TriangleIndices.Add(22);		// 4
            retVal.TriangleIndices.Add(20);		// 0
            retVal.TriangleIndices.Add(21);		// 3
            retVal.TriangleIndices.Add(21);		// 3
            retVal.TriangleIndices.Add(23);		// 7
            retVal.TriangleIndices.Add(22);		// 4

            // shouldn't I set normals?
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

        public static MeshGeometry3D GetCylinder_AlongX(int numSegments, double radius, double height, RotateTransform3D rotateTransform = null)
        {
            //NOTE: All the other geometries in this class are along the x axis, so I want to follow suit, but I think best along the z axis.  So I'll transform the points before commiting them to the geometry
            //TODO: This is so close to GetMultiRingedTube, the only difference is the multi ring tube has "hard" faces, and this has "soft" faces (this one shares points and normals, so the lighting is smoother)

            if (numSegments < 3)
            {
                throw new ArgumentException("numSegments must be at least 3: " + numSegments.ToString(), "numSegments");
            }

            MeshGeometry3D retVal = new MeshGeometry3D();

            #region Initial calculations

            Transform3DGroup transform = new Transform3DGroup();
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90d)));

            if (rotateTransform != null)
            {
                // This is in case they wanted oriented other than along the x axis
                transform.Children.Add(rotateTransform);
            }

            double halfHeight = height / 2d;

            Point[] points = Math2D.GetCircle_Cached(numSegments);

            #endregion

            #region Side

            for (int cntr = 0; cntr < numSegments; cntr++)
            {
                retVal.Positions.Add(transform.Transform(new Point3D(points[cntr].X * radius, points[cntr].Y * radius, -halfHeight)));
                retVal.Positions.Add(transform.Transform(new Point3D(points[cntr].X * radius, points[cntr].Y * radius, halfHeight)));

                retVal.Normals.Add(transform.Transform(new Vector3D(points[cntr].X, points[cntr].Y, 0d)));		// the normals point straight out of the side
                retVal.Normals.Add(transform.Transform(new Vector3D(points[cntr].X, points[cntr].Y, 0d)));
            }

            for (int cntr = 0; cntr < numSegments - 1; cntr++)
            {
                // 0,2,3
                retVal.TriangleIndices.Add((cntr * 2) + 0);
                retVal.TriangleIndices.Add((cntr * 2) + 2);
                retVal.TriangleIndices.Add((cntr * 2) + 3);

                // 0,3,1
                retVal.TriangleIndices.Add((cntr * 2) + 0);
                retVal.TriangleIndices.Add((cntr * 2) + 3);
                retVal.TriangleIndices.Add((cntr * 2) + 1);
            }

            // Connecting the last 2 points to the first 2
            // last,0,1
            int offset = (numSegments - 1) * 2;
            retVal.TriangleIndices.Add(offset + 0);
            retVal.TriangleIndices.Add(0);
            retVal.TriangleIndices.Add(1);

            // last,1,last+1
            retVal.TriangleIndices.Add(offset + 0);
            retVal.TriangleIndices.Add(1);
            retVal.TriangleIndices.Add(offset + 1);

            #endregion

            // Caps
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
            // This is a copy of GetCylinder_AlongX
            //TODO: This is so close to GetMultiRingedTube, the only difference is the multi ring tube has "hard" faces, and this has "soft" faces (this one shares points and normals, so the lighting is smoother)

            if (numSegments < 3)
            {
                throw new ArgumentException("numSegments must be at least 3: " + numSegments.ToString(), "numSegments");
            }

            MeshGeometry3D retVal = new MeshGeometry3D();

            #region Initial calculations

            Transform3D transform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90d));

            double halfHeight = height / 2d;

            Point[] points = Math2D.GetCircle_Cached(numSegments);

            double rotateAngleForPerp = Vector3D.AngleBetween(new Vector3D(1, 0, 0), new Vector3D(radius, 0, height));		// the 2nd vector is perpendicular to line formed from the edge of the cone to the tip

            #endregion

            #region Side

            for (int cntr = 0; cntr < numSegments; cntr++)
            {
                retVal.Positions.Add(transform.Transform(new Point3D(points[cntr].X * radius, points[cntr].Y * radius, -halfHeight)));
                retVal.Positions.Add(transform.Transform(new Point3D(0, 0, halfHeight)));		// creating a unique point for each face so that the lighting will be correct

                // The normal points straight out for a cylinder, but for a cone, it needs to be perpendicular to the slope of the cone
                Vector3D normal = new Vector3D(points[cntr].X, points[cntr].Y, 0d);
                Vector3D rotateAxis = new Vector3D(-points[cntr].Y, points[cntr].X, 0d);
                normal = normal.GetRotatedVector(rotateAxis, rotateAngleForPerp);

                retVal.Normals.Add(transform.Transform(normal));

                // This one just points straight up
                retVal.Normals.Add(transform.Transform(new Vector3D(0, 0, 1d)));
            }

            for (int cntr = 0; cntr < numSegments - 1; cntr++)
            {
                // 0,2,3
                retVal.TriangleIndices.Add((cntr * 2) + 0);
                retVal.TriangleIndices.Add((cntr * 2) + 2);
                retVal.TriangleIndices.Add((cntr * 2) + 3);

                // The cylinder has two triangles per face, but the cone only has one
                //// 0,3,1
                //retVal.TriangleIndices.Add((cntr * 2) + 0);
                //retVal.TriangleIndices.Add((cntr * 2) + 3);
                //retVal.TriangleIndices.Add((cntr * 2) + 1);
            }

            // Connecting the last 2 points to the first 2
            // last,0,1
            int offset = (numSegments - 1) * 2;
            retVal.TriangleIndices.Add(offset + 0);
            retVal.TriangleIndices.Add(0);
            retVal.TriangleIndices.Add(1);

            //// last,1,last+1
            //retVal.TriangleIndices.Add(offset + 0);
            //retVal.TriangleIndices.Add(1);
            //retVal.TriangleIndices.Add(offset + 1);

            #endregion

            // Caps
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

            //Point[] points = new Point[numSegmentsTheta];		// these define a unit circle

            //for (int cntr = 0; cntr < numSegmentsTheta; cntr++)
            //{
            //    points[cntr] = new Point(Math.Cos(theta), Math.Sin(theta));
            //    theta += deltaTheta;
            //}

            Point[] points = Math2D.GetCircle_Cached(numSegmentsTheta);

            #endregion

            #region Side

            for (int cntr = 0; cntr < numSegmentsTheta; cntr++)
            {
                retVal.Positions.Add(transform.Transform(new Point3D(points[cntr].X * radius, points[cntr].Y * radius, -halfHeight)));
                retVal.Positions.Add(transform.Transform(new Point3D(points[cntr].X * radius, points[cntr].Y * radius, halfHeight)));

                retVal.Normals.Add(transform.Transform(new Vector3D(points[cntr].X, points[cntr].Y, 0d)));		// the normals point straight out of the side
                retVal.Normals.Add(transform.Transform(new Vector3D(points[cntr].X, points[cntr].Y, 0d)));
            }

            for (int cntr = 0; cntr < numSegmentsTheta - 1; cntr++)
            {
                // 0,2,3
                retVal.TriangleIndices.Add((cntr * 2) + 0);
                retVal.TriangleIndices.Add((cntr * 2) + 2);
                retVal.TriangleIndices.Add((cntr * 2) + 3);

                // 0,3,1
                retVal.TriangleIndices.Add((cntr * 2) + 0);
                retVal.TriangleIndices.Add((cntr * 2) + 3);
                retVal.TriangleIndices.Add((cntr * 2) + 1);
            }

            // Connecting the last 2 points to the first 2
            // last,0,1
            int offset = (numSegmentsTheta - 1) * 2;
            retVal.TriangleIndices.Add(offset + 0);
            retVal.TriangleIndices.Add(0);
            retVal.TriangleIndices.Add(1);

            // last,1,last+1
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

        public static MeshGeometry3D GetRing(int numSides, double innerRadius, double outerRadius, double height, Transform3D transform, bool includeInnerRingFaces = true, bool includeOuterRingFaces = true)
        {
            MeshGeometry3D retVal = new MeshGeometry3D();

            if (transform == null)
            {
                transform = Transform3D.Identity;
            }

            Point[] points = Math2D.GetCircle_Cached(numSides);
            double halfHeight = height * .5d;

            int pointOffset = 0;
            int zOffsetBottom, zOffsetTop;

            #region Outer Ring

            #region Positions/Normals

            for (int cntr = 0; cntr < numSides; cntr++)
            {
                retVal.Positions.Add(transform.Transform(new Point3D(points[cntr].X * outerRadius, points[cntr].Y * outerRadius, -halfHeight)));
                retVal.Normals.Add(transform.Transform(new Vector3D(points[cntr].X * outerRadius, points[cntr].Y * outerRadius, 0d).ToUnit()));		// the normals point straight out of the side
            }

            for (int cntr = 0; cntr < numSides; cntr++)
            {
                retVal.Positions.Add(transform.Transform(new Point3D(points[cntr].X * outerRadius, points[cntr].Y * outerRadius, halfHeight)));
                retVal.Normals.Add(transform.Transform(new Vector3D(points[cntr].X * outerRadius, points[cntr].Y * outerRadius, 0d).ToUnit()));		// the normals point straight out of the side
            }

            #endregion

            if (includeOuterRingFaces)
            {
                #region Triangles

                zOffsetBottom = pointOffset;
                zOffsetTop = zOffsetBottom + numSides;

                for (int cntr = 0; cntr < numSides - 1; cntr++)
                {
                    // Top/Left triangle
                    retVal.TriangleIndices.Add(zOffsetBottom + cntr + 0);
                    retVal.TriangleIndices.Add(zOffsetTop + cntr + 1);
                    retVal.TriangleIndices.Add(zOffsetTop + cntr + 0);

                    // Bottom/Right triangle
                    retVal.TriangleIndices.Add(zOffsetBottom + cntr + 0);
                    retVal.TriangleIndices.Add(zOffsetBottom + cntr + 1);
                    retVal.TriangleIndices.Add(zOffsetTop + cntr + 1);
                }

                // Connecting the last 2 points to the first 2
                // Top/Left triangle
                retVal.TriangleIndices.Add(zOffsetBottom + (numSides - 1) + 0);
                retVal.TriangleIndices.Add(zOffsetTop);		// wrapping back around
                retVal.TriangleIndices.Add(zOffsetTop + (numSides - 1) + 0);

                // Bottom/Right triangle
                retVal.TriangleIndices.Add(zOffsetBottom + (numSides - 1) + 0);
                retVal.TriangleIndices.Add(zOffsetBottom);
                retVal.TriangleIndices.Add(zOffsetTop);

                #endregion
            }

            pointOffset = retVal.Positions.Count;

            #endregion

            #region Inner Ring

            #region Positions/Normals

            for (int cntr = 0; cntr < numSides; cntr++)
            {
                retVal.Positions.Add(transform.Transform(new Point3D(points[cntr].X * innerRadius, points[cntr].Y * innerRadius, -halfHeight)));
                retVal.Normals.Add(transform.Transform(new Vector3D(points[cntr].X * innerRadius, points[cntr].Y * innerRadius, 0d).ToUnit() * -1d));		// the normals point straight in from the side
            }

            for (int cntr = 0; cntr < numSides; cntr++)
            {
                retVal.Positions.Add(transform.Transform(new Point3D(points[cntr].X * innerRadius, points[cntr].Y * innerRadius, halfHeight)));
                retVal.Normals.Add(transform.Transform(new Vector3D(points[cntr].X * innerRadius, points[cntr].Y * innerRadius, 0d).ToUnit() * -1d));		// the normals point straight in from the side
            }

            #endregion

            if (includeInnerRingFaces)
            {
                #region Triangles

                zOffsetBottom = pointOffset;
                zOffsetTop = zOffsetBottom + numSides;

                for (int cntr = 0; cntr < numSides - 1; cntr++)
                {
                    // Top/Left triangle
                    retVal.TriangleIndices.Add(zOffsetBottom + cntr + 0);
                    retVal.TriangleIndices.Add(zOffsetTop + cntr + 0);
                    retVal.TriangleIndices.Add(zOffsetTop + cntr + 1);

                    // Bottom/Right triangle
                    retVal.TriangleIndices.Add(zOffsetBottom + cntr + 0);
                    retVal.TriangleIndices.Add(zOffsetTop + cntr + 1);
                    retVal.TriangleIndices.Add(zOffsetBottom + cntr + 1);
                }

                // Connecting the last 2 points to the first 2
                // Top/Left triangle
                retVal.TriangleIndices.Add(zOffsetBottom + (numSides - 1) + 0);
                retVal.TriangleIndices.Add(zOffsetTop + (numSides - 1) + 0);
                retVal.TriangleIndices.Add(zOffsetTop);		// wrapping back around

                // Bottom/Right triangle
                retVal.TriangleIndices.Add(zOffsetBottom + (numSides - 1) + 0);
                retVal.TriangleIndices.Add(zOffsetTop);
                retVal.TriangleIndices.Add(zOffsetBottom);

                #endregion
            }

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

            // Exit Function
            return retVal;
        }
        private static void GetRingSprtCap(ref int pointOffset, MeshGeometry3D geometry, Transform3D transform, Point[] points, int numSides, double innerRadius, double outerRadius)
        {
            // Points/Normals
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

            // Triangles
            for (int cntr = 0; cntr < numSides - 1; cntr++)
            {
                // Bottom Right triangle
                geometry.TriangleIndices.Add(zOffsetOuter + cntr + 0);
                geometry.TriangleIndices.Add(zOffsetOuter + cntr + 1);
                geometry.TriangleIndices.Add(zOffsetInner + cntr + 1);

                // Top Left triangle
                geometry.TriangleIndices.Add(zOffsetOuter + cntr + 0);
                geometry.TriangleIndices.Add(zOffsetInner + cntr + 1);
                geometry.TriangleIndices.Add(zOffsetInner + cntr + 0);
            }

            // Connecting the last 2 points to the first 2
            // Bottom/Right triangle
            geometry.TriangleIndices.Add(zOffsetOuter + (numSides - 1) + 0);
            geometry.TriangleIndices.Add(zOffsetOuter);
            geometry.TriangleIndices.Add(zOffsetInner);

            // Top/Left triangle
            geometry.TriangleIndices.Add(zOffsetOuter + (numSides - 1) + 0);
            geometry.TriangleIndices.Add(zOffsetInner);		// wrapping back around
            geometry.TriangleIndices.Add(zOffsetInner + (numSides - 1) + 0);

            pointOffset = geometry.Positions.Count;
        }

        public static MeshGeometry3D GetCircle2D(int numSides, Transform3D transform, Transform3D normalTransform)
        {
            //NOTE: This also sets the texture coordinates

            int pointOffset = 0;
            Point[] pointsTheta = Math2D.GetCircle_Cached(numSides);

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
        /// If you are making something discreet, like dice, or a jewel, you will want each face to reflect like mirrors (not soft).  But if you
        /// are making a cylinder, etc, you don't want the faces to stand out.  Instead, they should blend together, so you will want soft
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
        public static MeshGeometry3D GetMultiRingedTube(int numSides, List<TubeRingBase> rings, bool softSides, bool shouldCenterZ, Transform3D transform = null)
        {
            return BuildTube.Build(numSides, rings, softSides, shouldCenterZ, transform);
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
        public static MeshGeometry3D GetMeshFromTriangles(ITriangleIndexed[] triangles)
        {
            MeshGeometry3D retVal = new MeshGeometry3D();

            // All the triangles in the list of TriangleIndexed should share the same points, so just use the first triangle's list of points
            foreach (Point3D point in triangles[0].AllPoints)
            {
                retVal.Positions.Add(point);
            }

            foreach (ITriangleIndexed triangle in triangles)
            {
                retVal.TriangleIndices.Add(triangle.Index0);
                retVal.TriangleIndices.Add(triangle.Index1);
                retVal.TriangleIndices.Add(triangle.Index2);
            }

            // Exit Function
            //retVal.Freeze();
            return retVal;
        }

        public static TriangleIndexed[] GetTrianglesFromMesh(MeshGeometry3D mesh, Transform3D transform = null, bool dedupePoints = true)
        {
            if (mesh == null)
            {
                return null;
            }

            return GetTrianglesFromMesh(new MeshGeometry3D[] { mesh }, new Transform3D[] { transform }, dedupePoints);
        }
        /// <summary>
        /// This will merge several meshes into a single set of triangles
        /// NOTE: The mesh array and transform array need to be the same size (each mesh could have its own transform)
        /// </summary>
        public static TriangleIndexed[] GetTrianglesFromMesh(MeshGeometry3D[] meshes, Transform3D[] transforms = null, bool dedupePoints = true)
        {
            if (dedupePoints)
            {
                return GetTrianglesFromMesh_Deduped(meshes, transforms);
            }
            else
            {
                return GetTrianglesFromMesh_Raw(meshes, transforms);
            }
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
                // Referenced points
                points = mesh.TriangleIndices.Select(o => mesh.Positions[o]).ToArray();
            }
            else
            {
                // Directly used points
                points = mesh.Positions.ToArray();
            }

            if (transform != null)
            {
                transform.Transform(points);
            }

            // Exit Function
            return points;
        }

        /// <summary>
        /// This returns a material that is the same color regardless of lighting
        /// </summary>
        public static Material GetUnlitMaterial(Color color)
        {
            Color diffuse = Colors.Black;
            diffuse.ScA = color.ScA;

            MaterialGroup retVal = new MaterialGroup();
            retVal.Children.Add(new DiffuseMaterial(new SolidColorBrush(diffuse)));
            retVal.Children.Add(new EmissiveMaterial(new SolidColorBrush(color)));

            retVal.Freeze();

            return retVal;
        }

        #endregion

        #region Misc

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
        /// This will cast a ray from the point (on _viewport) along the direction that the camera is looking, and returns hits
        /// against wpf visuals and the drag shape (sorted by distance from camera)
        /// </summary>
        /// <remarks>
        /// This only looks at the click ray, and won't do anything with offsets.  This method is useful for seeing where they
        /// clicked, then take further action from there
        /// </remarks>
        /// <param name="backgroundVisual">This is the UIElement that contains the viewport (and has the background set to non null)</param>
        public static List<MyHitTestResult> CastRay(out RayHitTestParameters clickRay, Point clickPoint, Visual backgroundVisual, PerspectiveCamera camera, Viewport3D viewport, bool returnAllHits, IEnumerable<Visual3D> ignoreVisuals = null, IEnumerable<Visual3D> onlyVisuals = null)
        {
            List<MyHitTestResult> retVal = new List<MyHitTestResult>();

            // This gets called every time there is a hit
            HitTestResultCallback resultCallback = delegate(HitTestResult result)
            {
                if (result is RayMeshGeometry3DHitTestResult)		// It could also be a RayHitTestResult, which isn't as exact as RayMeshGeometry3DHitTestResult
                {
                    RayMeshGeometry3DHitTestResult resultCast = (RayMeshGeometry3DHitTestResult)result;

                    bool shouldKeep = true;
                    if (ignoreVisuals != null && ignoreVisuals.Any(o => o == resultCast.VisualHit))
                    {
                        shouldKeep = false;
                    }

                    if (onlyVisuals != null && !onlyVisuals.Any(o => o == resultCast.VisualHit))
                    {
                        shouldKeep = false;
                    }

                    if (shouldKeep)
                    {
                        retVal.Add(new MyHitTestResult(resultCast));

                        if (!returnAllHits)
                        {
                            return HitTestResultBehavior.Stop;
                        }
                    }
                }

                return HitTestResultBehavior.Continue;
            };

            // Get hits against existing models
            VisualTreeHelper.HitTest(backgroundVisual, null, resultCallback, new PointHitTestParameters(clickPoint));

            // Also return the click ray
            clickRay = UtilityWPF.RayFromViewportPoint(camera, viewport, clickPoint);

            // Sort by distance
            if (retVal.Count > 1)
            {
                Point3D clickRayOrigin = clickRay.Origin;		// the compiler complains about anonymous methods using out params
                retVal = retVal.OrderBy(o => o.GetDistanceFromPoint(clickRayOrigin)).ToList();
            }

            // Exit Function
            return retVal;
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

        /// <summary>
        /// This projects a single point from 3D to 2D
        /// </summary>
        public static Point? Project3Dto2D(out bool isInFront, Viewport3D viewport, Point3D point)
        {
            // The viewport should always have something in it, even if it's just lights
            if (viewport.Children.Count == 0)
            {
                isInFront = false;
                return null;
            }

            Viewport3DVisual visual = VisualTreeHelper.GetParent(viewport.Children[0]) as Viewport3DVisual;

            bool success;
            Matrix3D matrix = MathUtils.TryWorldToViewportTransform(visual, out success);
            if (!success)
            {
                isInFront = false;
                return null;
            }

            Point3D retVal = matrix.Transform(point);
            isInFront = retVal.Z > 0d;
            return new Point(retVal.X, retVal.Y);
        }
        /// <summary>
        /// This projects a sphere into a circle
        /// NOTE: the return type could be null
        /// </summary>
        public static Tuple<Point, double> Project3Dto2D(out bool isInFront, Viewport3D viewport, Point3D point, double radius)
        {
            // The viewport should always have something in it, even if it's just lights
            if (viewport.Children.Count == 0)
            {
                isInFront = false;
                return null;
            }

            Viewport3DVisual visual = VisualTreeHelper.GetParent(viewport.Children[0]) as Viewport3DVisual;

            bool success;
            Matrix3D matrix = MathUtils.TryWorldToViewportTransform(visual, out success);
            if (!success)
            {
                isInFront = false;
                return null;
            }

            // Transform the center point
            Point3D point2D = matrix.Transform(point);
            Point retVal = new Point(point2D.X, point2D.Y);

            // Cast a ray from that point
            var ray = RayFromViewportPoint(visual.Camera, viewport, retVal);

            // Get an orthogonal to that (this will be a line that is in the plane that the camera is looking at)
            Vector3D orth = Math3D.GetArbitraryOrhonganal(ray.Direction);
            orth = orth.ToUnit() * radius;

            // Now that the direction of the line is known, project the point along that line, the length of radius into 2D
            Point3D point2D_rad = matrix.Transform(point + orth);

            // The distance between the two 2D points is the size of the circle on screen
            double length2D = (new Point(point2D_rad.X, point2D_rad.Y) - retVal).Length;

            // Exit Function
            isInFront = point2D.Z + length2D > 0d;
            return Tuple.Create(retVal, length2D);
        }

        /// <summary>
        /// This sets the attenuation and range of a light so that it's a percent of its intensity at some distance
        /// </summary>
        public static void SetAttenuation(PointLightBase light, double distance, double percentAtDistance)
        {
            // % = 1/max(1, q*d^2)
            // % = 1/(q*d^2)
            // q=1(d^2 * %)
            light.ConstantAttenuation = 0d;
            light.LinearAttenuation = 0d;
            light.QuadraticAttenuation = 1 / (distance * percentAtDistance);

            // Now limit the range
            light.Range = 1 / Math.Sqrt(.01 * light.QuadraticAttenuation);		// stop it at 1% intensity
        }

        /// <summary>
        /// This will tell you what to set top/left to for a window to not straddle monitors
        /// </summary>
        public static Point EnsureWindowIsOnScreen(Point position, Size size)
        {
            //TODO: See if there is a way to do this without using winform dll

            int x = Convert.ToInt32(position.X);
            int y = Convert.ToInt32(position.Y);

            int width = Convert.ToInt32(size.Width);
            int height = Convert.ToInt32(size.Height);

            // See what monitor this is sitting on
            System.Windows.Forms.Screen screen = System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point(x, y));

            if (x + width > screen.WorkingArea.Right)
            {
                x = screen.WorkingArea.Right - width;
            }

            if (x < screen.WorkingArea.Left)
            {
                x = screen.WorkingArea.Left;		// doing this second so that if the window is larger than the screen, it will be the right that overflows
            }

            if (y + height > screen.WorkingArea.Bottom)
            {
                y = screen.WorkingArea.Bottom - height - 2;
            }

            if (y < screen.WorkingArea.Top)
            {
                y = screen.WorkingArea.Top;		// doing this second so the top is always visible when the window is too tall for the monitor
            }

            // Exit Function
            return new Point(x, y);
        }

        public static Rect GetCurrentScreen(Point position)
        {
            int x = Convert.ToInt32(position.X);
            int y = Convert.ToInt32(position.Y);

            // See what monitor this is sitting on
            System.Windows.Forms.Screen screen = System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point(x, y));

            return new Rect(screen.WorkingArea.X, screen.WorkingArea.Y, screen.WorkingArea.Width, screen.WorkingArea.Height);
        }

        /// <summary>
        /// This tells a visual to render itself, and returns a custom class that returns colors at various positions
        /// </summary>
        /// <param name="cacheColorsUpFront">
        /// True:  The entire byte array will be converted into Color structs up front (taking an up front hit, but repeated requests for colors are cheap).
        /// False:  The byte array is stored, and any requests for colors are done on the fly (good if only a subset of the pixels will be looked at, of if you want another thread to take the hit).
        /// </param>
        /// <param name="outOfBoundsColor">If requests for pixels outside of width/height are made, this is the color that should be returned (probably either use transparent or black)</param>
        public static IBitmapCustom RenderControl(FrameworkElement visual, int width, int height, bool cacheColorsUpFront, Color outOfBoundsColor, bool isInVisualTree)
        {
            // Populate a wpf bitmap with a snapshot of the visual
            BitmapSource bitmap = RenderControl(visual, width, height, isInVisualTree);

            // Get a byte array
            int stride = (width * bitmap.Format.BitsPerPixel + 7) / 8;		//http://msdn.microsoft.com/en-us/magazine/cc534995.aspx
            byte[] bytes = new byte[stride * height];

            bitmap.CopyPixels(bytes, stride, 0);

            // Exit Function
            if (cacheColorsUpFront)
            {
                return new BitmapCustomFullyCached(bytes, width, height, stride, bitmap.Format, outOfBoundsColor);
            }
            else
            {
                return new BitmapCustomOnTheFly(bytes, width, height, stride, bitmap.Format, outOfBoundsColor);
            }
        }

        /// <summary>
        /// This tells a visual to render itself to a wpf bitmap.  From there, you can get the bytes (colors), or run it through a converter
        /// to save as jpg, bmp files.
        /// </summary>
        /// <remarks>
        /// This fixes an issue where the rendered image is blank:
        /// http://blogs.msdn.com/b/jaimer/archive/2009/07/03/rendertargetbitmap-tips.aspx
        /// </remarks>
        public static BitmapSource RenderControl(FrameworkElement visual, int width, int height, bool isInVisualTree)
        {
            if (!isInVisualTree)
            {
                // If the visual isn't part of the visual tree, then it needs to be forced to finish its layout
                visual.Width = width;
                visual.Height = height;
                visual.Measure(new Size(width, height));        //  I thought these two statements would be expensive, but profiling shows it's mostly all on Render
                visual.Arrange(new Rect(0, 0, width, height));
            }

            RenderTargetBitmap retVal = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);

            DrawingVisual dv = new DrawingVisual();
            using (DrawingContext ctx = dv.RenderOpen())
            {
                VisualBrush vb = new VisualBrush(visual);
                ctx.DrawRectangle(vb, null, new Rect(new Point(0, 0), new Point(width, height)));
            }

            retVal.Render(dv);      //  profiling shows this is the biggest hit

            return retVal;
        }

        /// <summary>
        /// Gets a single pixel
        /// Got this here: http://stackoverflow.com/questions/14876989/how-to-read-pixels-in-four-corners-of-a-bitmapsource
        /// </summary>
        /// <remarks>
        /// If only a single pixel is needed, this method is a bit easier than RenderControl().GetColor()
        /// </remarks>
        public static Color GetPixelColor(BitmapSource bitmap, int x, int y)
        {
            int bytesPerPixel = (bitmap.Format.BitsPerPixel + 7) / 8;
            byte[] bytes = new byte[bytesPerPixel];
            Int32Rect rect = new Int32Rect(x, y, 1, 1);

            bitmap.CopyPixels(rect, bytes, bytesPerPixel, 0);

            Color color;
            if (bitmap.Format == PixelFormats.Pbgra32)
            {
                color = Color.FromArgb(bytes[3], bytes[2], bytes[1], bytes[0]);
            }
            else if (bitmap.Format == PixelFormats.Bgr32)
            {
                color = Color.FromArgb(0xFF, bytes[2], bytes[1], bytes[0]);
            }
            // handle other required formats
            else
            {
                color = Colors.Black;
            }

            return color;
        }

        /// <summary>
        /// This converts a wpf visual into a mouse cursor (call this.RenderControl to get the bitmapsource)
        /// NOTE: Needs to be a standard size (16x16, 32x32, etc)
        /// TODO: Fix this for semitransparency
        /// </summary>
        /// <remarks>
        /// Got this here:
        /// http://stackoverflow.com/questions/46805/custom-cursor-in-wpf
        /// </remarks>
        public static Cursor ConvertToCursor(BitmapSource bitmapSource, Point hotSpot)
        {
            int width = bitmapSource.PixelWidth;
            int height = bitmapSource.PixelHeight;

            // Get a byte array
            int stride = (width * bitmapSource.Format.BitsPerPixel + 7) / 8;		//http://msdn.microsoft.com/en-us/magazine/cc534995.aspx
            byte[] bytes = new byte[stride * height];

            bitmapSource.CopyPixels(bytes, stride, 0);

            // Convert to System.Drawing.Bitmap
            var bitmap = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            for (int y = 0; y < height; y++)
            {
                int rowOffset = y * stride;
                int yOffset = y * width;

                for (int x = 0; x < width; x++)
                {
                    int offset = rowOffset + (x * 4);		// this is assuming that bitmap.Format.BitsPerPixel is 32, which would be four bytes per pixel

                    bitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(bytes[offset + 3], bytes[offset + 2], bytes[offset + 1], bytes[offset + 0]));
                    //bitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(64, 255, 0, 0));
                }
            }

            // Save to .ico format
            MemoryStream stream = new MemoryStream();
            System.Drawing.Icon.FromHandle(bitmap.GetHicon()).Save(stream);

            // Convert saved file into .cur format
            stream.Seek(2, SeekOrigin.Begin);
            stream.WriteByte(2);        // convert to cur format

            //stream.Seek(8, SeekOrigin.Begin);     // trying to wipe out the color pallete to be able to support transparency, but has no effect
            //stream.WriteByte(0);
            //stream.WriteByte(0);

            stream.Seek(10, SeekOrigin.Begin);
            stream.WriteByte((byte)(int)(hotSpot.X * width));
            stream.WriteByte((byte)(int)(hotSpot.Y * height));
            stream.Seek(0, SeekOrigin.Begin);

            // Construct Cursor
            return new Cursor(stream);
        }

        /// <summary>
        /// This finds the font by name
        /// </summary>
        /// <param name="desiredFonts">Pass in multiple in case the first choice isn't installed</param>
        public static FontFamily GetFont(string[] desiredFonts)
        {
            FontFamily[] sysFonts = Fonts.SystemFontFamilies.ToArray();

            foreach (string desired in desiredFonts)
            {
                string desiredTrim = desired.Trim();

                FontFamily retVal = sysFonts.Where(o => o.Source.Trim().Equals(desiredTrim, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                if (retVal != null)
                {
                    return retVal;
                }
            }

            // Worst case, just return something (should be arial)
            return sysFonts[0];
        }

        /// <summary>
        /// This makes sure that angle stays between 0 and 360
        /// </summary>
        public static double GetCappedAngle(double angle)
        {
            double retVal = angle;

            while (true)
            {
                if (retVal < 0d)
                {
                    retVal += 360d;
                }
                else if (retVal >= 360d)
                {
                    retVal -= 360d;
                }
                else
                {
                    return retVal;
                }
            }
        }

        #endregion

        #region Private Methods

        private static void GetDome(ref int pointOffset, MeshGeometry3D geometry, Point[] pointsTheta, Transform3D transform, int numSegmentsPhi, double radiusX, double radiusY, double radiusZ)
        {
            #region Initial calculations

            // NOTE: There is one more than what the passed in
            Point[] pointsPhi = new Point[numSegmentsPhi + 1];

            pointsPhi[0] = new Point(1d, 0d);		// along the equator
            pointsPhi[numSegmentsPhi] = new Point(0d, 1d);		// north pole

            if (pointsPhi.Length > 2)
            {
                // Need to go from 0 to half pi
                double halfPi = Math.PI * .5d;
                double deltaPhi = halfPi / pointsPhi.Length;		// there is one more point than numSegmentsPhi

                for (int cntr = 1; cntr < numSegmentsPhi; cntr++)
                {
                    double phi = deltaPhi * cntr;		// phi goes from 0 to pi for a full sphere, so start halfway up
                    pointsPhi[cntr] = new Point(Math.Cos(phi), Math.Sin(phi));
                }
            }

            #endregion

            #region Positions/Normals

            // Can't use all of the transform passed in for the normal, because translate portions will skew the normal funny
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

            //for (int phiCntr = 0; phiCntr < numSegmentsPhi; phiCntr++)		// The top point will be added after this loop
            for (int phiCntr = pointsPhi.Length - 1; phiCntr > 0; phiCntr--)
            {
                for (int thetaCntr = 0; thetaCntr < pointsTheta.Length; thetaCntr++)
                {
                    // Phi points are going from bottom to equator.  

                    Point3D point = new Point3D(
                        radiusX * pointsTheta[thetaCntr].X * pointsPhi[phiCntr].Y,
                        radiusY * pointsTheta[thetaCntr].Y * pointsPhi[phiCntr].Y,
                        radiusZ * pointsPhi[phiCntr].X);

                    geometry.Positions.Add(transform.Transform(point));

                    //TODO: For a standalone dome, the bottom rings will point straight out.  But for something like a snow cone, the normal will have to be averaged with the cone
                    geometry.Normals.Add(normalTransform.Transform(point).ToVector().ToUnit());		// the normal is the same as the point for a sphere (but no tranlate transform)
                }
            }

            // This is north pole point
            geometry.Positions.Add(transform.Transform(new Point3D(0, 0, radiusZ)));
            geometry.Normals.Add(transform.Transform(new Vector3D(0, 0, 1)));

            #endregion

            #region Triangles - Rings

            int zOffsetBottom = pointOffset;
            int zOffsetTop;

            for (int phiCntr = 0; phiCntr < numSegmentsPhi - 1; phiCntr++)		// The top cone will be added after this loop
            {
                zOffsetTop = zOffsetBottom + pointsTheta.Length;

                for (int thetaCntr = 0; thetaCntr < pointsTheta.Length - 1; thetaCntr++)
                {
                    // Top/Left triangle
                    geometry.TriangleIndices.Add(zOffsetBottom + thetaCntr + 0);
                    geometry.TriangleIndices.Add(zOffsetTop + thetaCntr + 1);
                    geometry.TriangleIndices.Add(zOffsetTop + thetaCntr + 0);

                    // Bottom/Right triangle
                    geometry.TriangleIndices.Add(zOffsetBottom + thetaCntr + 0);
                    geometry.TriangleIndices.Add(zOffsetBottom + thetaCntr + 1);
                    geometry.TriangleIndices.Add(zOffsetTop + thetaCntr + 1);
                }

                // Connecting the last 2 points to the first 2
                // Top/Left triangle
                geometry.TriangleIndices.Add(zOffsetBottom + (pointsTheta.Length - 1) + 0);
                geometry.TriangleIndices.Add(zOffsetTop);		// wrapping back around
                geometry.TriangleIndices.Add(zOffsetTop + (pointsTheta.Length - 1) + 0);

                // Bottom/Right triangle
                geometry.TriangleIndices.Add(zOffsetBottom + (pointsTheta.Length - 1) + 0);
                geometry.TriangleIndices.Add(zOffsetBottom);
                geometry.TriangleIndices.Add(zOffsetTop);

                // Prep for the next ring
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

            // The last triangle links back to zero
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
            // This will be along the z axis.  It will go from z=0 to z=radiusZ

            if (numSegmentsTheta < 3)
            {
                throw new ArgumentException("numSegments must be at least 3: " + numSegmentsTheta.ToString(), "numSegments");
            }

            MeshGeometry3D retVal = new MeshGeometry3D();

            #region Initial calculations

            //Transform3D transform = Transform3D.Identity;

            double deltaTheta = 2d * Math.PI / numSegmentsTheta;

            Point[] pointsTheta = new Point[numSegmentsTheta];		// these define a unit circle

            for (int cntr = 0; cntr < numSegmentsTheta; cntr++)
            {
                pointsTheta[cntr] = new Point(Math.Cos(deltaTheta * cntr), Math.Sin(deltaTheta * cntr));
            }

            // NOTE: There is one more than what the passed in
            Point[] pointsPhi = new Point[numSegmentsPhi + 1];

            pointsPhi[0] = new Point(1d, 0d);		// along the equator
            pointsPhi[numSegmentsPhi] = new Point(0d, 1d);		// north pole

            if (pointsPhi.Length > 2)
            {
                //double halfPi = Math.PI * .5d;
                ////double deltaPhi = halfPi / numSegmentsPhi;
                //double deltaPhi = halfPi / pointsPhi.Length;		// there is one more point than numSegmentsPhi
                ////double deltaPhi = Math.PI / numSegmentsPhi;

                //for (int cntr = 1; cntr < numSegmentsPhi; cntr++)
                //{
                //    double phi = halfPi + (deltaPhi * cntr);		// phi goes from 0 to pi for a full sphere, so start halfway up
                //    //double phi = deltaPhi * cntr;
                //    pointsPhi[cntr] = new Point(Math.Cos(phi), Math.Sin(phi));
                //}



                // Need to go from 0 to half pi
                double halfPi = Math.PI * .5d;
                double deltaPhi = halfPi / pointsPhi.Length;		// there is one more point than numSegmentsPhi

                for (int cntr = 1; cntr < numSegmentsPhi; cntr++)
                {
                    double phi = deltaPhi * cntr;		// phi goes from 0 to pi for a full sphere, so start halfway up
                    pointsPhi[cntr] = new Point(Math.Cos(phi), Math.Sin(phi));
                }


            }

            #endregion

            #region Positions/Normals

            //for (int phiCntr = 0; phiCntr < numSegmentsPhi; phiCntr++)		// The top point will be added after this loop
            for (int phiCntr = pointsPhi.Length - 1; phiCntr > 0; phiCntr--)
            {
                for (int thetaCntr = 0; thetaCntr < numSegmentsTheta; thetaCntr++)
                {

                    // I think phi points are going from bottom to equator.  

                    Point3D point = new Point3D(
                        radiusX * pointsTheta[thetaCntr].X * pointsPhi[phiCntr].Y,
                        radiusY * pointsTheta[thetaCntr].Y * pointsPhi[phiCntr].Y,
                        radiusZ * pointsPhi[phiCntr].X);

                    //point = transform.Transform(point);

                    retVal.Positions.Add(point);

                    //TODO: For a standalone dome, the bottom ring's will point straight out.  But for something like a snow cone, the normal will have to be averaged with the cone
                    retVal.Normals.Add(point.ToVector().ToUnit());		// the normal is the same as the point for a sphere
                }
            }

            // This is north pole point
            //retVal.Positions.Add(transform.Transform(new Point3D(0, 0, radiusZ)));
            //retVal.Normals.Add(transform.Transform(new Vector3D(0, 0, 1)));
            retVal.Positions.Add(new Point3D(0, 0, radiusZ));
            retVal.Normals.Add(new Vector3D(0, 0, 1));

            #endregion

            #region Triangles - Rings

            int zOffsetBottom = 0;
            int zOffsetTop;

            for (int phiCntr = 0; phiCntr < numSegmentsPhi - 1; phiCntr++)		// The top cone will be added after this loop
            {
                zOffsetTop = zOffsetBottom + numSegmentsTheta;

                for (int thetaCntr = 0; thetaCntr < numSegmentsTheta - 1; thetaCntr++)
                {
                    // Top/Left triangle
                    retVal.TriangleIndices.Add(zOffsetBottom + thetaCntr + 0);
                    retVal.TriangleIndices.Add(zOffsetTop + thetaCntr + 1);
                    retVal.TriangleIndices.Add(zOffsetTop + thetaCntr + 0);

                    // Bottom/Right triangle
                    retVal.TriangleIndices.Add(zOffsetBottom + thetaCntr + 0);
                    retVal.TriangleIndices.Add(zOffsetBottom + thetaCntr + 1);
                    retVal.TriangleIndices.Add(zOffsetTop + thetaCntr + 1);
                }

                // Connecting the last 2 points to the first 2
                // Top/Left triangle
                retVal.TriangleIndices.Add(zOffsetBottom + (numSegmentsTheta - 1) + 0);
                retVal.TriangleIndices.Add(zOffsetTop);		// wrapping back around
                retVal.TriangleIndices.Add(zOffsetTop + (numSegmentsTheta - 1) + 0);

                // Bottom/Right triangle
                retVal.TriangleIndices.Add(zOffsetBottom + (numSegmentsTheta - 1) + 0);
                retVal.TriangleIndices.Add(zOffsetBottom);
                retVal.TriangleIndices.Add(zOffsetTop);

                // Prep for the next ring
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

            // The last triangle links back to zero
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

        /// <summary>
        /// This takes values from 0 to 255
        /// </summary>
        private static Color GetColorCapped(double a, double r, double g, double b)
        {
            if (a < 0)
            {
                a = 0;
            }
            else if (a > 255)
            {
                a = 255;
            }

            if (r < 0)
            {
                r = 0;
            }
            else if (r > 255)
            {
                r = 255;
            }

            if (g < 0)
            {
                g = 0;
            }
            else if (g > 255)
            {
                g = 255;
            }

            if (b < 0)
            {
                b = 0;
            }
            else if (b > 255)
            {
                b = 255;
            }

            // Exit Function
            return Color.FromArgb(Convert.ToByte(a), Convert.ToByte(r), Convert.ToByte(g), Convert.ToByte(b));
        }
        private static Color GetColorCapped(int a, int r, int g, int b)
        {
            if (a < 0)
            {
                a = 0;
            }
            else if (a > 255)
            {
                a = 255;
            }

            if (r < 0)
            {
                r = 0;
            }
            else if (r > 255)
            {
                r = 255;
            }

            if (g < 0)
            {
                g = 0;
            }
            else if (g > 255)
            {
                g = 255;
            }

            if (b < 0)
            {
                b = 0;
            }
            else if (b > 255)
            {
                b = 255;
            }

            // Exit Function
            return Color.FromArgb(Convert.ToByte(a), Convert.ToByte(r), Convert.ToByte(g), Convert.ToByte(b));
        }

        private static byte GetByteCapped(double value)
        {
            if (value < 0)
            {
                return 0;
            }
            else if (value > 255)
            {
                return 255;
            }
            else
            {
                return Convert.ToByte(value);
            }
        }

        // For speed reasons, the code was duplicated
        private static TriangleIndexed[] GetTrianglesFromMesh_Raw(MeshGeometry3D[] meshes, Transform3D[] transforms = null)
        {
            #region Points

            List<Point3D> allPointsList = new List<Point3D>();

            for (int cntr = 0; cntr < meshes.Length; cntr++)
            {
                Point3D[] positions = meshes[cntr].Positions.ToArray();

                if (transforms != null && transforms[cntr] != null)
                {
                    transforms[cntr].Transform(positions);
                }

                allPointsList.AddRange(positions);
            }

            Point3D[] allPoints = allPointsList.ToArray();

            #endregion

            List<TriangleIndexed> retVal = new List<TriangleIndexed>();

            #region Triangles

            int posOffset = 0;

            foreach (MeshGeometry3D mesh in meshes)
            {
                //string report = mesh.ReportGeometry();

                if (mesh.TriangleIndices.Count % 3 != 0)
                {
                    throw new ArgumentException("The mesh's triangle indicies need to be divisible by 3");
                }

                int numTriangles = mesh.TriangleIndices.Count / 3;

                for (int cntr = 0; cntr < numTriangles; cntr++)
                {
                    TriangleIndexed triangle = new TriangleIndexed(
                        posOffset + mesh.TriangleIndices[cntr * 3],
                        posOffset + mesh.TriangleIndices[(cntr * 3) + 1],
                        posOffset + mesh.TriangleIndices[(cntr * 3) + 2],
                        allPoints);

                    double normalLength = triangle.NormalLength;
                    if (!Math3D.IsNearZero(normalLength) && !Math3D.IsInvalid(normalLength))      // don't include bad triangles (the mesh seems to be ok with bad triangles, so just skip them)
                    {
                        retVal.Add(triangle);
                    }
                }

                posOffset += mesh.Positions.Count;
            }

            #endregion

            // Exit Function
            return retVal.ToArray();
        }
        private static TriangleIndexed[] GetTrianglesFromMesh_Deduped(MeshGeometry3D[] meshes, Transform3D[] transforms = null)
        {
            #region Points

            // Key=original index
            // Value=new index
            SortedList<int, int> pointMap = new SortedList<int, int>();

            // Points
            List<Point3D> allPointsList = new List<Point3D>();

            int posOffset = 0;

            for (int m = 0; m < meshes.Length; m++)
            {
                Point3D[] positions = meshes[m].Positions.ToArray();

                if (transforms != null && transforms[m] != null)
                {
                    transforms[m].Transform(positions);
                }

                for (int p = 0; p < positions.Length; p++)
                {
                    int dupeIndex = IndexOfDupe(allPointsList, positions[p]);

                    if (dupeIndex < 0)
                    {
                        allPointsList.Add(positions[p]);
                        pointMap.Add(posOffset + p, allPointsList.Count - 1);
                    }
                    else
                    {
                        pointMap.Add(posOffset + p, dupeIndex);
                    }
                }

                posOffset += meshes[m].Positions.Count;
            }

            Point3D[] allPoints = allPointsList.ToArray();

            #endregion

            List<TriangleIndexed> retVal = new List<TriangleIndexed>();

            #region Triangles

            posOffset = 0;

            foreach (MeshGeometry3D mesh in meshes)
            {
                //string report = mesh.ReportGeometry();

                if (mesh.TriangleIndices.Count % 3 != 0)
                {
                    throw new ArgumentException("The mesh's triangle indicies need to be divisible by 3");
                }

                int numTriangles = mesh.TriangleIndices.Count / 3;

                for (int cntr = 0; cntr < numTriangles; cntr++)
                {
                    TriangleIndexed triangle = new TriangleIndexed(
                        pointMap[posOffset + mesh.TriangleIndices[cntr * 3]],
                        pointMap[posOffset + mesh.TriangleIndices[(cntr * 3) + 1]],
                        pointMap[posOffset + mesh.TriangleIndices[(cntr * 3) + 2]],
                        allPoints);

                    double normalLength = triangle.NormalLength;
                    if (!Math3D.IsNearZero(normalLength) && !Math3D.IsInvalid(normalLength))      // don't include bad triangles (the mesh seems to be ok with bad triangles, so just skip them)
                    {
                        retVal.Add(triangle);
                    }
                }

                posOffset += mesh.Positions.Count;
            }

            #endregion

            // Exit Function
            return retVal.ToArray();
        }

        private static int IndexOfDupe(List<Point3D> points, Point3D test)
        {
            for (int cntr = 0; cntr < points.Count; cntr++)
            {
                if (Math3D.IsNearValue(points[cntr], test))
                {
                    return cntr;
                }
            }

            return -1;
        }

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
        /// Cylinder - You would want false for both caps, because it's 90 degrees between the end cap and side
        /// Pyramid/Cone - You would want false, because it's greater than 90 degrees between the base cap and side
        /// Rings meant to look seamless - When the angle is low between two rings, that's when this should be true
        /// </remarks>
        public bool MergeNormalWithPrevIfSoft
        {
            get;
            private set;
        }

        #endregion
    }

    #endregion

    //TODO: Make a way to define a ring that isn't a regular polygon (like a rectangle), just takes a list of 2D Point.
    //Call it TubeRingPath.  See Game.Newt.AsteroidMiner2.ShipParts.ConverterRadiationToEnergyDesign.GetShape -
    //the generic one will be a lot harder because one could be 3 points, the next could be 4.  QuickHull is too complex,
    //but need something like that to see which points should make triangles from
    #region Class: TubeRingRegularPolygon

    public class TubeRingRegularPolygon : TubeRingBase
    {
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

        // This doesn't need any extra properties
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
                    // If the instance version of this class hasn't been instantiated yet, then do so
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
                    // There is no need to check the static lock, because _instance is only set one time, and that is guaranteed to be
                    // finished before this function gets called
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
                        // NOTE: There is one more than what the passed in
                        Point[] pointsPhi = new Point[numSides + 1];

                        pointsPhi[0] = new Point(1d, 0d);		// along the equator
                        pointsPhi[numSides] = new Point(0d, 1d);		// north pole

                        if (pointsPhi.Length > 2)
                        {
                            // Need to go from 0 to half pi
                            double halfPi = Math.PI * .5d;
                            double deltaPhi = halfPi / pointsPhi.Length;		// there is one more point than numSegmentsPhi

                            for (int cntr = 1; cntr < numSides; cntr++)
                            {
                                double phi = deltaPhi * cntr;		// phi goes from 0 to pi for a full sphere, so start halfway up
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
        /// Cylinder - You would want false for both caps, because it's 90 degrees between the end cap and side
        /// Pyramid/Cone - You would want false, because it's greater than 90 degrees between the base cap and side
        /// Rings meant to look seamless - When the angle is low between two rings, that's when this should be true
        /// </remarks>
        public bool MergeNormalWithPrevIfSoft
        {
            get;
            private set;
        }

        #region Public Methods

        /// <summary>
        /// This creates a new list, and resizes all the items to match the new lengths passed in
        /// </summary>
        /// <remarks>
        /// This is made so that rings can just be defined using friendly units (whatever units the author feels like), then
        /// pass through this method to get final values
        /// </remarks>
        public static List<TubeRingBase> FitNewSize(List<TubeRingBase> rings, double radiusX, double radiusY, double length)
        {
            #region Get sizes of the list passed in

            double origLength = rings.Skip(1).Sum(o => o.DistFromPrevRing);

            Tuple<double, double>[] origRadii = rings.
                Where(o => o is TubeRingRegularPolygon).
                Select(o => (TubeRingRegularPolygon)o).
                Select(o => Tuple.Create(o.RadiusX, o.RadiusY)).
                ToArray();

            double origRadX = origRadii.Max(o => o.Item1);
            double origRadY = origRadii.Max(o => o.Item2);

            #endregion

            List<TubeRingBase> retVal = new List<TubeRingBase>();

            for (int cntr = 0; cntr < rings.Count; cntr++)
            {
                double distance = 0d;
                if (cntr > 0)
                {
                    distance = (rings[cntr].DistFromPrevRing / origLength) * length;
                }

                if (rings[cntr] is TubeRingDome)
                {
                    #region Dome

                    TubeRingDome dome = (TubeRingDome)rings[cntr];

                    retVal.Add(new TubeRingDome(distance, dome.MergeNormalWithPrevIfSoft, dome.NumSegmentsPhi));

                    #endregion
                }
                else if (rings[cntr] is TubeRingPoint)
                {
                    #region TubeRingPoint

                    TubeRingPoint point = (TubeRingPoint)rings[cntr];

                    retVal.Add(new TubeRingPoint(distance, point.MergeNormalWithPrevIfSoft));

                    #endregion
                }
                else if (rings[cntr] is TubeRingRegularPolygon)
                {
                    #region TubeRingRegularPolygon

                    TubeRingRegularPolygon poly = (TubeRingRegularPolygon)rings[cntr];

                    retVal.Add(new TubeRingRegularPolygon(distance, poly.MergeNormalWithPrevIfSoft,
                        (poly.RadiusX / origRadX) * radiusX,
                        (poly.RadiusY / origRadY) * radiusY,
                        poly.IsClosedIfEndCap));

                    #endregion
                }
                else
                {
                    throw new ApplicationException("Unknown type of ring: " + rings[cntr].GetType().ToString());
                }
            }

            return retVal;
        }

        public static double GetTotalHeight(List<TubeRingBase> rings)
        {
            double retVal = 0d;

            for (int cntr = 1; cntr < rings.Count; cntr++)     // the first ring's distance from prev ring is ignored
            {
                // I had a case where I wanted to make an arrow where the end cap comes backward a bit
                //if (rings[cntr].DistFromPrevRing <= 0)
                //{
                //    throw new ArgumentException("DistFromPrevRing must be positive: " + rings[cntr].DistFromPrevRing.ToString());
                //}

                retVal += rings[cntr].DistFromPrevRing;
            }

            return retVal;
        }

        #endregion
    }

    #endregion

    #region Interface: IBitmapCustom

    //NOTE: The classes that implement this should be threadsafe
    //TODO: May want to add methods to set colors, and to populate an arbitrary BitmapSource with the modified pixels - but do so in a treadsafe way (returning a new IBitmapCustom)
    public interface IBitmapCustom
    {
        /// <summary>
        /// This gets the color of a single pixel
        /// NOTE: If the request is outside the bounds of the bitmap, a default color is returned, no exception is thrown
        /// </summary>
        Color GetColor(int x, int y);
        /// <summary>
        /// This returns a rectangle of colors
        /// NOTE: If the request is outside the bounds of the bitmap, a default color is returned, no exception is thrown
        /// </summary>
        /// <remarks>
        /// I was debating whether to return a 1D array or 2D, and read that 2D arrays have slower performance.  So to get
        /// a cell out of the array, it's:
        ///    color[x + (y * width)]
        /// </remarks>
        Color[] GetColors(int x, int y, int width, int height);
    }

    #endregion
    #region Class: BitmapCustomFullyCached

    public class BitmapCustomFullyCached : IBitmapCustom
    {
        #region Declaration Section

        private readonly int _width;
        private readonly int _height;

        private readonly Color[] _colors;

        private readonly Color _outOfBoundsColor;

        #endregion

        #region Constructor

        public BitmapCustomFullyCached(byte[] bytes, int width, int height, int stride, PixelFormat format, Color outOfBoundsColor)
        {
            _width = width;
            _height = height;
            _outOfBoundsColor = outOfBoundsColor;

            _colors = new Color[width * height];

            // Convert to colors
            //NOTE: The entire loop is copied to make this as fast as possible
            if (format == PixelFormats.Pbgra32)		// can't do a switch on format
            {
                #region Pbgra32

                for (int rowCntr = 0; rowCntr < height; rowCntr++)
                {
                    int rowOffset = rowCntr * stride;

                    int yOffset = rowCntr * width;

                    for (int columnCntr = 0; columnCntr < width; columnCntr++)
                    {
                        int offset = rowOffset + (columnCntr * 4);		// this is assuming that bitmap.Format.BitsPerPixel is 32, which would be four bytes per pixel

                        _colors[columnCntr + yOffset] = Color.FromArgb(bytes[offset + 3], bytes[offset + 2], bytes[offset + 1], bytes[offset + 0]);
                    }
                }

                #endregion
            }
            else if (format == PixelFormats.Bgr32)
            {
                #region Bgr32

                for (int rowCntr = 0; rowCntr < height; rowCntr++)
                {
                    int rowOffset = rowCntr * stride;

                    int yOffset = rowCntr * width;

                    for (int columnCntr = 0; columnCntr < width; columnCntr++)
                    {
                        int offset = rowOffset + (columnCntr * 4);		// this is assuming that bitmap.Format.BitsPerPixel is 32, which would be four bytes per pixel

                        _colors[columnCntr + yOffset] = Color.FromArgb(255, bytes[offset + 2], bytes[offset + 1], bytes[offset + 0]);
                    }
                }

                #endregion
            }
            else
            {
                throw new ApplicationException("TODO: Handle more pixel formats: " + format.ToString());
            }
        }

        #endregion

        #region IBitmapCustom Members

        public Color GetColor(int x, int y)
        {
            if (x < 0 || x >= _width || y < 0 || y >= _height)
            {
                return _outOfBoundsColor;
            }
            else
            {
                return _colors[x + (y * _width)];
            }
        }

        public Color[] GetColors(int x, int y, int width, int height)
        {
            if (x == 0 && y == 0 && width == _width && height == _height)
            {
                //  Just return the array (much faster than building a new one, but it assumes that they don't try to manipulate the colors)
                return _colors;
            }

            Color[] retVal = new Color[width * height];

            //NOTE: Copying code for speed reasons

            int yOffsetLeft, yOffsetRight;

            if (x < 0 || x + width >= _width || y < 0 || y + height >= _height)
            {
                #region Some out of bounds

                int x2, y2;

                for (int y1 = 0; y1 < height; y1++)
                {
                    y2 = y + y1;

                    yOffsetLeft = y1 * width;		// offset into the return array
                    yOffsetRight = y2 * _width;		// offset into _colors array

                    for (int x1 = 0; x1 < width; x1++)
                    {
                        x2 = x + x1;

                        if (x2 < 0 || x2 >= _width || y2 < 0 || y2 >= _height)
                        {
                            retVal[x1 + yOffsetLeft] = _outOfBoundsColor;
                        }
                        else
                        {
                            retVal[x1 + yOffsetLeft] = _colors[x2 + yOffsetRight];
                        }
                    }
                }

                #endregion
            }
            else
            {
                #region All in bounds

                for (int y1 = 0; y1 < height; y1++)
                {
                    yOffsetLeft = y1 * width;		// offset into the return array
                    yOffsetRight = (y + y1) * _width;		// offset into _colors array

                    for (int x1 = 0; x1 < width; x1++)
                    {
                        retVal[x1 + yOffsetLeft] = _colors[x + x1 + yOffsetRight];
                    }
                }

                #endregion
            }

            // Exit Function
            return retVal;
        }

        #endregion
    }

    #endregion
    #region Class: BitmapCustomOnTheFly

    public class BitmapCustomOnTheFly : IBitmapCustom
    {
        #region Declaration Section

        private readonly int _width;
        private readonly int _height;

        private readonly byte[] _bytes;

        private readonly PixelFormat _format;
        private readonly int _stride;

        private readonly Color _outOfBoundsColor;

        #endregion

        #region Constructor

        public BitmapCustomOnTheFly(byte[] bytes, int width, int height, int stride, PixelFormat format, Color outOfBoundsColor)
        {
            _bytes = bytes;
            _width = width;
            _height = height;
            _stride = stride;
            _format = format;
            _outOfBoundsColor = outOfBoundsColor;
        }

        #endregion

        #region IBitmapCustom Members

        public Color GetColor(int x, int y)
        {
            if (x < 0 || x >= _width || y < 0 || y >= _height)
            {
                return _outOfBoundsColor;
            }

            if (_format == PixelFormats.Pbgra32)		// can't do a switch on format
            {
                #region Pbgra32

                int offset = (y * _stride) + (x * 4);		// this is assuming that bitmap.Format.BitsPerPixel is 32, which would be four bytes per pixel

                return Color.FromArgb(_bytes[offset + 3], _bytes[offset + 2], _bytes[offset + 1], _bytes[offset + 0]);

                #endregion
            }
            else if (_format == PixelFormats.Bgr32)
            {
                #region Bgr32

                int offset = (y * _stride) + (x * 4);		// this is assuming that bitmap.Format.BitsPerPixel is 32, which would be four bytes per pixel

                return Color.FromArgb(255, _bytes[offset + 2], _bytes[offset + 1], _bytes[offset + 0]);

                #endregion
            }
            else
            {
                throw new ApplicationException("TODO: Handle more pixel formats: " + _format.ToString());
            }
        }

        public Color[] GetColors(int x, int y, int width, int height)
        {
            Color[] retVal = new Color[width * height];

            //NOTE: Copying code for speed reasons

            int yOffsetLeft, yOffsetRight, offset;

            if (x < 0 || x + width >= _width || y < 0 || y + height >= _height)
            {
                #region Some out of bounds

                if (_format == PixelFormats.Pbgra32)		// can't do a switch on format
                {
                    #region Pbgra32

                    int x2, y2;

                    for (int y1 = 0; y1 < height; y1++)
                    {
                        y2 = y + y1;

                        yOffsetLeft = y1 * width;		// offset into the return array
                        yOffsetRight = y2 * _stride;		// offset into _bytes array

                        for (int x1 = 0; x1 < width; x1++)
                        {
                            x2 = x + x1;

                            if (x2 < 0 || x2 >= _width || y2 < 0 || y2 >= _height)
                            {
                                retVal[x1 + yOffsetLeft] = _outOfBoundsColor;
                            }
                            else
                            {
                                offset = yOffsetRight + (x2 * 4);		// this is assuming that bitmap.Format.BitsPerPixel is 32, which would be four bytes per pixel

                                retVal[x1 + yOffsetLeft] = Color.FromArgb(_bytes[offset + 3], _bytes[offset + 2], _bytes[offset + 1], _bytes[offset + 0]);
                            }
                        }
                    }

                    #endregion
                }
                else if (_format == PixelFormats.Bgr32)
                {
                    #region Bgr32

                    int x2, y2;

                    for (int y1 = 0; y1 < height; y1++)
                    {
                        y2 = y + y1;

                        yOffsetLeft = y1 * width;		// offset into the return array
                        yOffsetRight = y2 * _stride;		// offset into _bytes array

                        for (int x1 = 0; x1 < width; x1++)
                        {
                            x2 = x + x1;

                            if (x2 < 0 || x2 >= _width || y2 < 0 || y2 >= _height)
                            {
                                retVal[x1 + yOffsetLeft] = _outOfBoundsColor;
                            }
                            else
                            {
                                offset = yOffsetRight + (x2 * 4);		// this is assuming that bitmap.Format.BitsPerPixel is 32, which would be four bytes per pixel

                                retVal[x1 + yOffsetLeft] = Color.FromArgb(255, _bytes[offset + 2], _bytes[offset + 1], _bytes[offset + 0]);
                            }
                        }
                    }

                    #endregion
                }
                else
                {
                    throw new ApplicationException("TODO: Handle more pixel formats: " + _format.ToString());
                }

                #endregion
            }
            else
            {
                #region All in bounds

                if (_format == PixelFormats.Pbgra32)		// can't do a switch on format
                {
                    #region Pbgra32

                    for (int y1 = 0; y1 < height; y1++)
                    {
                        yOffsetLeft = y1 * width;		// offset into the return array
                        yOffsetRight = (y + y1) * _stride;		// offset into _bytes array

                        for (int x1 = 0; x1 < width; x1++)
                        {
                            offset = yOffsetRight + ((x + x1) * 4);		// this is assuming that bitmap.Format.BitsPerPixel is 32, which would be four bytes per pixel

                            retVal[x1 + yOffsetLeft] = Color.FromArgb(_bytes[offset + 3], _bytes[offset + 2], _bytes[offset + 1], _bytes[offset + 0]);
                        }
                    }

                    #endregion
                }
                else if (_format == PixelFormats.Bgr32)
                {
                    #region Bgr32

                    for (int y1 = 0; y1 < height; y1++)
                    {
                        yOffsetLeft = y1 * width;		// offset into the return array
                        yOffsetRight = (y + y1) * _stride;		// offset into _bytes array

                        for (int x1 = 0; x1 < width; x1++)
                        {
                            offset = yOffsetRight + ((x + x1) * 4);		// this is assuming that bitmap.Format.BitsPerPixel is 32, which would be four bytes per pixel

                            retVal[x1 + yOffsetLeft] = Color.FromArgb(255, _bytes[offset + 2], _bytes[offset + 1], _bytes[offset + 0]);
                        }
                    }

                    #endregion
                }
                else
                {
                    throw new ApplicationException("TODO: Handle more pixel formats: " + _format.ToString());
                }

                #endregion
            }

            // Exit Function
            return retVal;
        }

        #endregion
    }

    #endregion

    #region Class: MyHitTestResult

    // This was copied from the ship editor
    public class MyHitTestResult
    {
        #region Constructor

        public MyHitTestResult(RayMeshGeometry3DHitTestResult modelHit)
        {
            this.ModelHit = modelHit;
        }

        #endregion

        public readonly RayMeshGeometry3DHitTestResult ModelHit;

        /// <summary>
        /// This is a helper property that returns the actual hit point
        /// </summary>
        public Point3D Point
        {
            get
            {
                if (this.ModelHit.VisualHit.Transform != null)
                {
                    return this.ModelHit.VisualHit.Transform.Transform(this.ModelHit.PointHit);
                }
                else
                {
                    return this.ModelHit.PointHit;
                }
            }
        }

        public double GetDistanceFromPoint(Point3D point)
        {
            return (point - this.Point).Length;
        }
    }

    #endregion

    #region Class: MaterialDefinition

    /// <summary>
    /// This defines a material, and is easy to serialize (meant to be put into classes that get serialized
    /// </summary>
    public class MaterialDefinition
    {
        public string DiffuseColor { get; set; }

        public string SpecularColor { get; set; }
        public double? SpecularPower { get; set; }      // required if SpecularColor is populated

        public string EmissiveColor { get; set; }

        public Material CreateMaterial()
        {
            // Detect diffuse only (likely a common case, and a slight optimization)
            if (!string.IsNullOrEmpty(this.DiffuseColor) && string.IsNullOrEmpty(this.SpecularColor) && string.IsNullOrEmpty(this.EmissiveColor))
            {
                return new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex(this.DiffuseColor)));
            }

            MaterialGroup retVal = new MaterialGroup();

            // Diffuse
            if (!string.IsNullOrEmpty(this.DiffuseColor))
            {
                retVal.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex(this.DiffuseColor))));
            }

            // Specular
            if (!string.IsNullOrEmpty(this.SpecularColor))
            {
                if (this.SpecularPower == null)
                {
                    throw new ArgumentException("Specular power is required if specular color is used");
                }

                retVal.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex(this.SpecularColor)), this.SpecularPower.Value));
            }

            // Emissive
            if (!string.IsNullOrEmpty(this.EmissiveColor))
            {
                retVal.Children.Add(new EmissiveMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex(this.EmissiveColor))));
            }

            return retVal;
        }
    }

    #endregion

    #region Struct: ColorHSV

    public struct ColorHSV
    {
        #region Constructor

        public ColorHSV(double h, double s, double v)
            : this(255, h, s, v) { }
        public ColorHSV(byte a, double h, double s, double v)
        {
            this.A = a;
            this.H = h;
            this.S = s;
            this.V = v;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Alpha: 0 to 255
        /// </summary>
        public readonly byte A;

        /// <summary>
        /// Hue: 0 to 360
        /// </summary>
        public readonly double H;

        /// <summary>
        /// Saturation: 0 to 100
        /// </summary>
        public readonly double S;

        /// <summary>
        /// Value: 0 to 100
        /// </summary>
        public readonly double V;

        #endregion

        #region Public Methods

        public Color ToRGB()
        {
            return UtilityWPF.HSVtoRGB(this.A, this.H, this.S, this.V);
        }

        #endregion
    }

    #endregion

    #region Enum: Axis

    public enum Axis
    {
        X,
        Y,
        Z
    }

    #endregion
    #region Struct: AxisFor

    /// <summary>
    /// This helps with running for loops against an axis
    /// </summary>
    public struct AxisFor
    {
        public AxisFor(Axis axis, int start, int stop)
        {
            this.Axis = axis;
            this.Start = start;
            this.Stop = stop;

            this.IsPos = this.Stop > this.Start;
            this.Increment = this.IsPos ? 1 : -1;
        }

        public readonly Axis Axis;
        public readonly int Start;
        public readonly int Stop;
        public readonly int Increment;
        public readonly bool IsPos;

        /// <summary>
        /// This will set one of the output x,y,z to index2D based on this.Axis
        /// </summary>
        public void Set3DIndex(ref int x, ref int y, ref int z, int index2D)
        {
            switch (this.Axis)
            {
                case Axis.X:
                    x = index2D;
                    break;

                case Axis.Y:
                    y = index2D;
                    break;

                case Axis.Z:
                    z = index2D;
                    break;

                default:
                    throw new ApplicationException("Unknown Axis: " + this.Axis.ToString());
            }
        }
        public int GetValueForOffset(int value2D)
        {
            if (this.IsPos)
            {
                return value2D;
            }
            else
            {
                return this.Start - value2D;        // using start, because it's negative, so start is the larger value
            }
        }

        public double GetValue(Point3D point)
        {
            switch (this.Axis)
            {
                case Axis.X:
                    return point.X;

                case Axis.Y:
                    return point.Y;

                case Axis.Z:
                    return point.Z;

                default:
                    throw new ApplicationException("Unknown Axis: " + this.Axis.ToString());
            }
        }
        public double GetValue(Vector3D vector)
        {
            switch (this.Axis)
            {
                case Axis.X:
                    return vector.X;

                case Axis.Y:
                    return vector.Y;

                case Axis.Z:
                    return vector.Z;

                default:
                    throw new ApplicationException("Unknown Axis: " + this.Axis.ToString());
            }
        }

        public IEnumerable<int> Iterate()
        {
            for (int cntr = Start; IsPos ? cntr <= Stop : cntr >= Stop; cntr += Increment)
            {
                yield return cntr;
            }
        }
    }

    #endregion
    #region Struct: Mapping_2D_1D

    /// <summary>
    /// This is a mapping between 2D and 1D (good for bitmaps, or other rectangle grids that are physically stored as 1D arrays)
    /// </summary>
    public struct Mapping_2D_1D
    {
        public Mapping_2D_1D(int x, int y, int offset1D)
        {
            this.X = x;
            this.Y = y;
            this.Offset1D = offset1D;
        }

        public readonly int X;
        public readonly int Y;
        public readonly int Offset1D;
    }

    #endregion
    #region Struct: Mapping_3D_1D

    /// <summary>
    /// This is a mapping between 3D and 1D
    /// </summary>
    public struct Mapping_3D_1D
    {
        public Mapping_3D_1D(int x, int y, int z, int offset1D)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.Offset1D = offset1D;
        }

        public readonly int X;
        public readonly int Y;
        public readonly int Z;
        public readonly int Offset1D;
    }

    #endregion

    #region Class: AnimateRotation

    /// <summary>
    /// This is a way to set up animations on rotate transforms
    /// </summary>
    /// <remarks>
    /// This isn't meant to be used from xaml.  WPF has its own animate classes.  I just figured I'd write my own that is lightweight,
    /// simple to set up (it doesn't have its own timer, and will likely sit next to many other instances)
    /// 
    /// Each of the constructors define a different way to do animation.  Then call Tick on a regular basis
    /// </remarks>
    public class AnimateRotation
    {
        #region Interface: IAnimateRotationWorker

        private interface IAnimateRotationWorker
        {
            void Tick(double elapsedTime);
        }

        #endregion
        #region Class: FixedWorker

        private class FixedWorker : IAnimateRotationWorker
        {
            private readonly bool _isQuat;
            private readonly QuaternionRotation3D _transformQuat;
            private readonly AxisAngleRotation3D _transformAxis;
            private readonly double _delta;

            public FixedWorker(QuaternionRotation3D transform, double delta)
            {
                _isQuat = true;
                _transformQuat = transform;
                _transformAxis = null;
                _delta = delta;
            }
            public FixedWorker(AxisAngleRotation3D transform, double delta)
            {
                _isQuat = false;
                _transformAxis = transform;
                _transformQuat = null;
                _delta = delta;
            }

            public void Tick(double elapsedTime)
            {
                if (_isQuat)
                {
                    _transformQuat.Quaternion = new Quaternion(_transformQuat.Quaternion.Axis, _transformQuat.Quaternion.Angle + (_delta * elapsedTime));
                }
                else
                {
                    _transformAxis.Angle = _transformAxis.Angle + (_delta * elapsedTime);
                }
            }
        }

        #endregion
        #region Class: AnyQuatWorker

        private class AnyQuatWorker : IAnimateRotationWorker
        {
            private readonly QuaternionRotation3D _transform;
            private readonly double _angleDelta;
            private readonly int _numFullRotations;

            private readonly double? _maxTransitionAngle;

            private double _degreesLeft = -1;
            private Quaternion _quatDelta;

            public AnyQuatWorker(QuaternionRotation3D transform, double angleDelta, int numFullRotations, double? maxTransitionAngle = null)
            {
                _transform = transform;
                _angleDelta = angleDelta;
                _numFullRotations = numFullRotations;
                _maxTransitionAngle = maxTransitionAngle;
            }

            public void Tick(double elapsedTime)
            {
                if (_degreesLeft <= 0)
                {
                    // Come up with a new destination
                    Quaternion rotateTo;
                    if (_maxTransitionAngle == null)
                    {
                        rotateTo = Math3D.GetRandomRotation();
                    }
                    else
                    {
                        Vector3D newAxis = Math3D.GetRandomVectorCone(_transform.Quaternion.Axis, _maxTransitionAngle.Value);
                        //NOTE: Once going in one direction, never change, because that would look abrupt (that's why this overload was
                        //used).  Don't want to use random, want to stay under 180, but the larger the angle, the longer it will take to get there, and will be more smooth
                        double newAngle = _transform.Quaternion.Angle + 178;
                        rotateTo = new Quaternion(newAxis, newAngle);
                    }

                    // Figure out how long it will take to get there
                    var delta = GetDelta(_transform.Quaternion, rotateTo, _angleDelta, _numFullRotations);
                    _quatDelta = delta.Item1;
                    _degreesLeft = delta.Item2;
                }

                // Rotate it
                double deltaAngle = _quatDelta.Angle * elapsedTime;
                _transform.Quaternion = _transform.Quaternion.RotateBy(new Quaternion(_quatDelta.Axis, deltaAngle));

                _degreesLeft -= Math.Abs(deltaAngle);
            }
        }

        #endregion
        #region Class: ConeQuatWorker

        private class ConeQuatWorker : IAnimateRotationWorker
        {
            private readonly QuaternionRotation3D _transform;
            private readonly double _angleDelta;
            private readonly int _numFullRotations;

            private readonly Vector3D _centerAxis;
            private readonly double _maxConeAngle;

            private double _degreesLeft = -1;
            private Quaternion _quatDelta;

            public ConeQuatWorker(QuaternionRotation3D transform, Vector3D centerAxis, double maxConeAngle, double angleDelta, int numFullRotations)
            {
                _transform = transform;
                _centerAxis = centerAxis;
                _maxConeAngle = maxConeAngle;
                _angleDelta = angleDelta;
                _numFullRotations = numFullRotations;
            }

            public void Tick(double elapsedTime)
            {
                if (_degreesLeft <= 0)
                {
                    // Come up with a new destination
                    //Quaternion rotateTo = Math3D.GetRandomRotation(_centerAxis, _maxConeAngle);

                    Vector3D newAxis = Math3D.GetRandomVectorCone(_centerAxis, _maxConeAngle);
                    //NOTE: Once going in one direction, never change, because that would look abrupt (that's why this overload was
                    //used).  Don't want to use random, want to stay under 180, but the larger the angle, the longer it will take to get there, and will be more smooth
                    double newAngle = _transform.Quaternion.Angle + 178;
                    Quaternion rotateTo = new Quaternion(newAxis, newAngle);

                    // Figure out how long it will take to get there
                    var delta = GetDelta(_transform.Quaternion, rotateTo, _angleDelta, _numFullRotations);
                    _quatDelta = delta.Item1;
                    _degreesLeft = delta.Item2;
                }

                // Rotate it
                double deltaAngle = _quatDelta.Angle * elapsedTime;
                _transform.Quaternion = _transform.Quaternion.RotateBy(new Quaternion(_quatDelta.Axis, deltaAngle));

                _degreesLeft -= Math.Abs(deltaAngle);
            }
        }

        #endregion
        #region Class: AnyQuatConeWorker

        private class AnyQuatConeWorker : IAnimateRotationWorker
        {
            private const double NEWANGLE = 90;

            private readonly QuaternionRotation3D _transform;

            private readonly double _maxTransitionConeAngle;
            private readonly double _angleBetweenTransitionsAdjusted;

            private double _currentPercent;
            private readonly double _destinationPercent;
            private Quaternion _from;
            private Quaternion _to;

            public AnyQuatConeWorker(QuaternionRotation3D transform, double maxTransitionConeAngle, double anglePerSecond, double angleBetweenTransitions)
            {
                _transform = transform;
                _maxTransitionConeAngle = maxTransitionConeAngle;
                _angleBetweenTransitionsAdjusted = anglePerSecond / NEWANGLE;
                _destinationPercent = angleBetweenTransitions / NEWANGLE;
                _currentPercent = _destinationPercent + 1;      // set it greater so the first time tick is called, a new destination will be chosen
            }

            public void Tick(double elapsedTime)
            {
                if (_currentPercent >= _destinationPercent)
                {
                    _from = _transform.Quaternion;
                    _to = new Quaternion(Math3D.GetRandomVectorCone(_from.Axis, _maxTransitionConeAngle), _from.Angle + NEWANGLE);

                    _currentPercent = 0d;
                }

                _currentPercent += _angleBetweenTransitionsAdjusted * elapsedTime;

                Quaternion newQuat = Quaternion.Slerp(_from, _to, _currentPercent, true);
                if (Vector3D.DotProduct(_from.Axis, newQuat.Axis) < 0)
                {
                    // Once the from,to crosses over 360 degrees, slerp tries to reverse direction.  So fix it so the item continuously rotates
                    // in the same direction
                    newQuat = new Quaternion(newQuat.Axis * -1d, 360d - newQuat.Angle);
                }

                _transform.Quaternion = newQuat.ToUnit();
            }
        }

        #endregion

        #region Declaration Section

        /// <summary>
        /// I decided to go with small dedicated worker classes instead of a switch statement and a mess of semi
        /// common member variables
        /// </summary>
        private IAnimateRotationWorker _worker = null;

        #endregion

        #region Public Factory Methods

        // Use the factory methods to create an instance
        private AnimateRotation() { }

        /// <summary>
        /// This simply rotates the same direction and same speed forever
        /// </summary>
        public static AnimateRotation Create_Constant(AxisAngleRotation3D transform, double delta)
        {
            return new AnimateRotation()
            {
                _worker = new FixedWorker(transform, delta)
            };
        }
        /// <summary>
        /// This simply rotates the same direction and same speed forever
        /// </summary>
        public static AnimateRotation Create_Constant(QuaternionRotation3D transform, double angleDelta)
        {
            if (transform.Quaternion.IsIdentity)
            {
                throw new ArgumentException("The transform passed in is an identity quaternion, so the axis is unpredictable");
            }

            return new AnimateRotation()
            {
                _worker = new FixedWorker(transform, angleDelta)
            };
        }

        /// <summary>
        /// This will rotate to a random rotation.  Once at that destination, choose a new random rotation to
        /// go to, and rotate to that.  Always at a constant speed
        /// </summary>
        /// <remarks>
        /// There are no limits on what axis can be used.  The rotation will always be at the fixed speed
        /// </remarks>
        /// <param name="angleDelta">degrees per elapsed</param>
        /// <param name="numFullRotations">
        /// If 0, this will rotate directly to destination, then choose a new destination.
        /// If 1, this will rotate to the destination, then a full 360, then choose another destination, 2 does 2 rotations, etc
        /// </param>
        public static AnimateRotation Create_AnyOrientation(QuaternionRotation3D transform, double angleDelta, int numFullRotations = 0)
        {
            return new AnimateRotation()
            {
                _worker = new AnyQuatWorker(transform, angleDelta, numFullRotations)
            };
        }
        /// <summary>
        /// This will go from any angle to any angle, but at the time of choosing a new destination, it won't
        /// exceed a cone defined by maxTransitionAngle
        /// </summary>
        /// <remarks>
        /// Without this constraint, the changes in direction are pretty jarring.  This is an attempt to smooth that out
        /// </remarks>
        public static AnimateRotation Create_AnyOrientation_LimitChange(QuaternionRotation3D transform, double maxTransitionConeAngle, double anglePerSecond, double angleBetweenTransitions)
        {
            return new AnimateRotation()
            {
                _worker = new AnyQuatConeWorker(transform, maxTransitionConeAngle, anglePerSecond, angleBetweenTransitions)
            };
        }

        /// <summary>
        /// This limits destination orientation axiis to a cone
        /// </summary>
        public static AnimateRotation Create_LimitedOrientation(QuaternionRotation3D transform, Vector3D centerAxis, double maxConeAngle, double angleDelta, int numFullRotations = 0)
        {
            return new AnimateRotation()
            {
                _worker = new ConeQuatWorker(transform, centerAxis, maxConeAngle, angleDelta, numFullRotations)
            };
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// This is expected to be called on a regular basis
        /// </summary>
        /// <remarks>
        /// This class doesn't have its own timer.  I figured there would be a bunch of these instances, and it would be
        /// inneficient if each had its own timer.  Plus requiring a dispose to turn the timer off would be a headache
        /// </remarks>
        public void Tick(double elapsedTime)
        {
            _worker.Tick(elapsedTime);
        }

        #endregion

        #region Private Methods

        private static Tuple<Quaternion, double> GetDelta(Quaternion current, Quaternion destination, double angleDelta, int numFullRotations)
        {
            Quaternion delta = Math3D.GetRotation(current, destination);

            double degreesLeft = Math.Abs(delta.Angle) + (360d * numFullRotations);

            return Tuple.Create(new Quaternion(delta.Axis, angleDelta), degreesLeft);
        }

        #endregion
    }

    #endregion
}
